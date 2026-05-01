#region Using declarations
using System;
using System.Collections.Generic;

using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class OrcaPrints
	{
		private const int MaxStoredPrintEvents = 50000;

		private void InitializeOrcaPrintsEngine()
		{
			tickBuffer = new List<OrcaPrintTick>(4096);
			printEvents = new List<PrintEvent>(2048);
			clusterCooldowns = new Dictionary<string, DateTime>();
			priceLevelAccumulators = new Dictionary<double, PriceLevelAccumulator>();
			printLock = new System.Threading.ReaderWriterLockSlim();
			currentBid = double.NaN;
			currentAsk = double.NaN;
			lastSessionResetBar = -1;
			priceLevelAccumulatorBarIndex = -1;
		}

		private void TerminateOrcaPrintsEngine()
		{
			System.Threading.ReaderWriterLockSlim localLock = printLock;
			if (localLock != null)
			{
				try
				{
					localLock.EnterWriteLock();
					if (tickBuffer != null) tickBuffer.Clear();
					if (printEvents != null) printEvents.Clear();
					if (clusterCooldowns != null) clusterCooldowns.Clear();
					if (priceLevelAccumulators != null) priceLevelAccumulators.Clear();
				}
				finally
				{
					if (localLock.IsWriteLockHeld) localLock.ExitWriteLock();
				}

				localLock.Dispose();
			}

			printLock = null;
			tickBuffer = null;
			printEvents = null;
			clusterCooldowns = null;
			priceLevelAccumulators = null;
		}

		private void ClearOrcaPrintsState()
		{
			System.Threading.ReaderWriterLockSlim localLock = printLock;
			if (localLock == null)
				return;

			localLock.EnterWriteLock();
			try
			{
				if (tickBuffer != null) tickBuffer.Clear();
				if (printEvents != null) printEvents.Clear();
				if (clusterCooldowns != null) clusterCooldowns.Clear();
				if (priceLevelAccumulators != null) priceLevelAccumulators.Clear();
				currentBid = double.NaN;
				currentAsk = double.NaN;
				priceLevelAccumulatorBarIndex = -1;
			}
			finally
			{
				localLock.ExitWriteLock();
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e == null)
				return;

			if (e.MarketDataType == MarketDataType.Bid)
			{
				if (e.Price > 0 && !double.IsNaN(e.Price))
					currentBid = e.Price;
				return;
			}

			if (e.MarketDataType == MarketDataType.Ask)
			{
				if (e.Price > 0 && !double.IsNaN(e.Price))
					currentAsk = e.Price;
				return;
			}

			if (e.MarketDataType != MarketDataType.Last)
				return;

			if (e.Ask > 0 && !double.IsNaN(e.Ask))
				currentAsk = e.Ask;
			if (e.Bid > 0 && !double.IsNaN(e.Bid))
				currentBid = e.Bid;

			long size = e.Volume;
			if (size <= 0)
				return;

			AggressorSide side = AggressorSide.Unknown;
			if (!double.IsNaN(currentAsk) && currentAsk > 0 && e.Price >= currentAsk)
				side = AggressorSide.Buy;
			else if (!double.IsNaN(currentBid) && currentBid > 0 && e.Price <= currentBid)
				side = AggressorSide.Sell;

			if (side == AggressorSide.Unknown)
				return;

			DateTime time = e.Time == DateTime.MinValue ? Time[0] : e.Time;
			OrcaPrintTick tick = new OrcaPrintTick(time, e.Price, size, side);
			bool includeInSingleAndCluster = size >= MinTradeSize;

			System.Threading.ReaderWriterLockSlim localLock = printLock;
			if (localLock == null || tickBuffer == null || printEvents == null)
				return;

			localLock.EnterWriteLock();
			try
			{
				ProcessIncomingPrintTick(tick, includeInSingleAndCluster);
			}
			finally
			{
				localLock.ExitWriteLock();
			}
		}

		private void ProcessIncomingPrintTick(OrcaPrintTick tick, bool includeInSingleAndCluster)
		{
			if (EnablePriceLevelAccumulation)
				UpdatePriceLevelAccumulation(tick);

			if (includeInSingleAndCluster)
			{
				tickBuffer.Add(tick);
				EvictOldTicks(tick.Time);

				if (EnableSinglePrints && tick.Size >= SinglePrintMinSize)
				{
					PrintEvent printEvent = new PrintEvent
					{
						Time = tick.Time,
						Price = tick.Price,
						Volume = tick.Size,
						Side = tick.Side,
						Kind = OrcaPrintEventKind.Single
					};
					AddPrintEvent(printEvent);
				}

				if (EnableClusters)
					TryEmitCluster(tick);
			}

			TrimStoredEvents();
		}

		private void UpdatePriceLevelAccumulation(OrcaPrintTick tick)
		{
			if (priceLevelAccumulators == null || CurrentBar < 0)
				return;

			if (priceLevelAccumulatorBarIndex != CurrentBar)
			{
				priceLevelAccumulators.Clear();
				priceLevelAccumulatorBarIndex = CurrentBar;
			}

			double priceKey = NormalizePriceToTick(tick.Price);
			PriceLevelAccumulator accumulator;
			if (!priceLevelAccumulators.TryGetValue(priceKey, out accumulator) || accumulator == null)
			{
				accumulator = new PriceLevelAccumulator
				{
					StartTime = tick.Time,
					EndTime = tick.Time,
					Price = priceKey
				};
				priceLevelAccumulators[priceKey] = accumulator;
			}

			accumulator.EndTime = tick.Time;
			accumulator.ChildCount++;
			if (tick.Side == AggressorSide.Buy)
				accumulator.BuyVolume += tick.Size;
			else if (tick.Side == AggressorSide.Sell)
				accumulator.SellVolume += tick.Size;

			bool passesVolume = accumulator.TotalVolume >= PriceLevelMinVolume;
			bool passesDominance = !PriceLevelRequireMinDominance || GetPriceLevelDominantPercent(accumulator) >= PriceLevelMinDominancePercent;
			if (!passesVolume || !passesDominance)
			{
				if (accumulator.Event != null)
				{
					RemovePrintEvent(accumulator.Event);
					accumulator.Event = null;
				}
				return;
			}

			if (accumulator.Event == null)
			{
				accumulator.Event = new PriceLevelEvent();
				AddPrintEvent(accumulator.Event);
			}

			UpdatePriceLevelEvent(accumulator);
		}

		private double GetPriceLevelDominantPercent(PriceLevelAccumulator accumulator)
		{
			if (accumulator == null || accumulator.TotalVolume <= 0)
				return 0.0;

			return 100.0 * Math.Max(accumulator.BuyVolume, accumulator.SellVolume) / accumulator.TotalVolume;
		}

		private void UpdatePriceLevelEvent(PriceLevelAccumulator accumulator)
		{
			if (accumulator == null || accumulator.Event == null)
				return;

			PriceLevelEvent printEvent = accumulator.Event;
			printEvent.StartTime = accumulator.StartTime;
			printEvent.EndTime = accumulator.EndTime;
			printEvent.Time = accumulator.EndTime;
			printEvent.Price = accumulator.Price;
			printEvent.Volume = accumulator.TotalVolume;
			printEvent.BuyVolume = accumulator.BuyVolume;
			printEvent.SellVolume = accumulator.SellVolume;
			printEvent.ChildCount = accumulator.ChildCount;
			printEvent.Side = accumulator.BuyVolume >= accumulator.SellVolume ? AggressorSide.Buy : AggressorSide.Sell;
		}

		private double NormalizePriceToTick(double price)
		{
			if (TickSize <= 0)
				return price;

			return Math.Round(price / TickSize) * TickSize;
		}

		private void EvictOldTicks(DateTime currentTime)
		{
			if (tickBuffer == null || tickBuffer.Count == 0)
				return;

			double windowSec = Math.Max(0.1, ClusterTimeWindowSec);
			int removeCount = 0;
			for (int i = 0; i < tickBuffer.Count; i++)
			{
				if ((currentTime - tickBuffer[i].Time).TotalSeconds <= windowSec)
					break;
				removeCount++;
			}

			if (removeCount > 0)
				tickBuffer.RemoveRange(0, removeCount);
		}

		private void TryEmitCluster(OrcaPrintTick latestTick)
		{
			if (tickBuffer == null || tickBuffer.Count < 2 || TickSize <= 0)
				return;

			double maxPriceRange = Math.Max(TickSize, ClusterMaxPriceTicks * TickSize);
			AggressorSide dominantSide = latestTick.Side;
			long totalVolume = 0;
			long buyVolume = 0;
			long sellVolume = 0;
			int childCount = 0;
			double minPrice = latestTick.Price;
			double maxPrice = latestTick.Price;
			int firstIndex = tickBuffer.Count - 1;
			int lastIndex = tickBuffer.Count - 1;

			for (int i = lastIndex; i >= 0; i--)
			{
				OrcaPrintTick candidate = tickBuffer[i];
				double spanSec = (latestTick.Time - candidate.Time).TotalSeconds;
				if (spanSec > ClusterTimeWindowSec)
					break;

				double proposedMin = Math.Min(minPrice, candidate.Price);
				double proposedMax = Math.Max(maxPrice, candidate.Price);
				if ((proposedMax - proposedMin) > maxPriceRange + 0.0000001)
					break;

				long proposedTotal = totalVolume + candidate.Size;
				long proposedBuy = buyVolume + (candidate.Side == AggressorSide.Buy ? candidate.Size : 0);
				long proposedSell = sellVolume + (candidate.Side == AggressorSide.Sell ? candidate.Size : 0);
				long proposedDominant = dominantSide == AggressorSide.Buy ? proposedBuy : proposedSell;
				double proposedConsistencyPct = proposedTotal > 0 ? (100.0 * proposedDominant / proposedTotal) : 0.0;

				if (candidate.Side != dominantSide && proposedConsistencyPct < MinAggressorPercent)
					break;

				firstIndex = i;
				totalVolume = proposedTotal;
				buyVolume = proposedBuy;
				sellVolume = proposedSell;
				minPrice = proposedMin;
				maxPrice = proposedMax;
				childCount++;
			}

			if (childCount < 2 || totalVolume < ClusterMinVolume)
				return;

			long dominantVolume = dominantSide == AggressorSide.Buy ? buyVolume : sellVolume;
			double consistencyPct = totalVolume > 0 ? (100.0 * dominantVolume / totalVolume) : 0.0;
			if (consistencyPct < MinAggressorPercent)
				return;

			ClusterEvent cluster = BuildClusterEvent(firstIndex, lastIndex, dominantSide, totalVolume, buyVolume, sellVolume, minPrice, maxPrice);
			if (cluster == null)
				return;

			if (ParentConfidenceMode != NinjaTrader.NinjaScript.Indicators.ParentConfidenceMode.Off)
			{
				ScoreCluster(cluster);
				if (ParentConfidenceMode == NinjaTrader.NinjaScript.Indicators.ParentConfidenceMode.ScoreAndFilter && cluster.ParentConfidenceScore * 100.0 < MinParentConfidence)
					return;
			}

			if (IsClusterInCooldown(cluster))
				return;

			AddPrintEvent(cluster);
		}

		private ClusterEvent BuildClusterEvent(int firstIndex, int lastIndex, AggressorSide dominantSide, long totalVolume, long buyVolume, long sellVolume, double minPrice, double maxPrice)
		{
			if (tickBuffer == null || firstIndex < 0 || lastIndex >= tickBuffer.Count || firstIndex > lastIndex || totalVolume <= 0)
				return null;

			double priceVolume = 0.0;
			DateTime startTime = tickBuffer[firstIndex].Time;
			DateTime endTime = tickBuffer[lastIndex].Time;
			DateTime previousTime = DateTime.MinValue;
			ClusterEvent cluster = new ClusterEvent
			{
				StartTime = startTime,
				EndTime = endTime,
				MinPrice = minPrice,
				MaxPrice = maxPrice,
				TotalVolume = totalVolume,
				BuyVolume = buyVolume,
				SellVolume = sellVolume,
				ChildCount = lastIndex - firstIndex + 1,
				DominantSide = dominantSide
			};

			for (int i = firstIndex; i <= lastIndex; i++)
			{
				OrcaPrintTick tick = tickBuffer[i];
				priceVolume += tick.Price * tick.Size;
				cluster.ChildSizes.Add(tick.Size);

				if (previousTime != DateTime.MinValue)
				{
					long gapMs = (long)Math.Max(0.0, (tick.Time - previousTime).TotalMilliseconds);
					cluster.InterTradeGapsMs.Add(gapMs);
				}
				previousTime = tick.Time;
			}

			cluster.VwapPrice = priceVolume / totalVolume;
			cluster.Time = endTime;
			cluster.Price = cluster.VwapPrice;
			cluster.Volume = totalVolume;
			cluster.Side = dominantSide;
			return cluster;
		}

		private bool IsClusterInCooldown(ClusterEvent cluster)
		{
			if (clusterCooldowns == null || cluster == null)
				return false;

			double cooldownSec = Math.Max(0.0, ClusterCooldownSec);
			double bandWidth = Math.Max(TickSize, ClusterMaxPriceTicks * TickSize);
			long bandIndex = (long)Math.Floor(cluster.VwapPrice / bandWidth);
			string key = ((int)cluster.DominantSide).ToString() + ":" + bandIndex.ToString();

			DateTime lastEmission;
			if (clusterCooldowns.TryGetValue(key, out lastEmission))
			{
				if ((cluster.EndTime - lastEmission).TotalSeconds < cooldownSec)
					return true;
			}

			clusterCooldowns[key] = cluster.EndTime;
			PruneClusterCooldowns(cluster.EndTime, Math.Max(cooldownSec * 4.0, ClusterTimeWindowSec * 2.0));
			return false;
		}

		private void PruneClusterCooldowns(DateTime currentTime, double maxAgeSec)
		{
			if (clusterCooldowns == null || clusterCooldowns.Count == 0)
				return;

			List<string> keysToRemove = null;
			foreach (KeyValuePair<string, DateTime> kvp in clusterCooldowns)
			{
				if ((currentTime - kvp.Value).TotalSeconds > maxAgeSec)
				{
					if (keysToRemove == null)
						keysToRemove = new List<string>();
					keysToRemove.Add(kvp.Key);
				}
			}

			if (keysToRemove == null)
				return;

			for (int i = 0; i < keysToRemove.Count; i++)
				clusterCooldowns.Remove(keysToRemove[i]);
		}

		private void AddPrintEvent(PrintEvent printEvent)
		{
			if (printEvents != null && printEvent != null)
				printEvents.Add(printEvent);
		}

		private void RemovePrintEvent(PrintEvent printEvent)
		{
			if (printEvents != null && printEvent != null)
				printEvents.Remove(printEvent);
		}

		private void TrimStoredEvents()
		{
			if (printEvents == null || printEvents.Count <= MaxStoredPrintEvents)
				return;

			int excess = printEvents.Count - MaxStoredPrintEvents;
			printEvents.RemoveRange(0, excess);
		}

		private List<PrintEvent> CopyPrintEventsSnapshot()
		{
			List<PrintEvent> snapshot = new List<PrintEvent>();
			System.Threading.ReaderWriterLockSlim localLock = printLock;
			if (localLock == null || printEvents == null)
				return snapshot;

			localLock.EnterReadLock();
			try
			{
				snapshot.Capacity = printEvents.Count;
				for (int i = 0; i < printEvents.Count; i++)
					snapshot.Add(printEvents[i]);
			}
			finally
			{
				localLock.ExitReadLock();
			}

			return snapshot;
		}
	}
}
