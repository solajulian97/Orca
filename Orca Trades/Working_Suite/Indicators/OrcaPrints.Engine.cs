#region Using declarations
using System;
using System.Collections.Generic;

using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
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
			highDeltaWindowAccumulators = new Dictionary<long, HighDeltaWindowAccumulator>();
			sharedProfileVolumeMaps = new List<Dictionary<double, long>>(4096);
			sharedProfileUpVolumeMaps = new List<Dictionary<double, long>>(4096);
			sharedProfileDownVolumeMaps = new List<Dictionary<double, long>>(4096);
			printLock = new System.Threading.ReaderWriterLockSlim();
			currentBid = double.NaN;
			currentAsk = double.NaN;
			sharedProfilePrevLast = double.NaN;
			sharedProfileLastDirection = 0;
			sharedProfileRevision = 0;
			sharedProfileCoverageBarCount = 0;
			sharedProfileLastUpdatedUtc = DateTime.MinValue;
			lastSharedProfileRegistrationUtc = DateTime.MinValue;
			lastSessionResetBar = -1;
			priceLevelAccumulatorBarIndex = -1;
		}

		private void TerminateOrcaPrintsEngine()
		{
			OrcaProfileDataCache.UnregisterSource(sharedProfileSourceId);

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
					if (highDeltaWindowAccumulators != null) highDeltaWindowAccumulators.Clear();
					if (sharedProfileVolumeMaps != null) sharedProfileVolumeMaps.Clear();
					if (sharedProfileUpVolumeMaps != null) sharedProfileUpVolumeMaps.Clear();
					if (sharedProfileDownVolumeMaps != null) sharedProfileDownVolumeMaps.Clear();
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
			highDeltaWindowAccumulators = null;
			sharedProfileVolumeMaps = null;
			sharedProfileUpVolumeMaps = null;
			sharedProfileDownVolumeMaps = null;
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
				if (highDeltaWindowAccumulators != null) highDeltaWindowAccumulators.Clear();
				currentBid = double.NaN;
				currentAsk = double.NaN;
				priceLevelAccumulatorBarIndex = -1;
				ClearSharedProfileCache();
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
			long diagnosticsWorkStart = 0;

			if (OrcaDiagnosticsCore.IsEnabled)
			{
				EnsureDiagnosticsRegistered();
				long diagnosticsSequence = OrcaDiagnosticsCore.ReportMarketData(diagnosticsInstanceId, e.MarketDataType, e.Time == DateTime.MinValue ? GetDiagnosticsEventTime() : e.Time);
				diagnosticsWorkStart = OrcaDiagnosticsCore.BeginWorkSample(diagnosticsSequence);
			}
			try
			{

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

			DateTime time = e.Time == DateTime.MinValue ? Time[0] : e.Time;
			UpdateSharedProfileCache(time, e.Price, size, side);

			if (side == AggressorSide.Unknown)
				return;
			if (IsInsideExcludedMocWindow(time))
				return;

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
			finally
			{
				if (diagnosticsWorkStart > 0)
					OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.MarketData, -1, diagnosticsWorkStart);
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

		private void InitializeOrcaPrintTimeZones()
		{
			try
			{
				orcaPrintChartTimeZone = NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo ?? TimeZoneInfo.Local;
			}
			catch
			{
				orcaPrintChartTimeZone = TimeZoneInfo.Local;
			}

			try
			{
				orcaPrintEasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
			}
			catch
			{
				orcaPrintEasternTimeZone = TimeZoneInfo.Local;
			}
		}

		private bool IsInsideExcludedMocWindow(DateTime chartTime)
		{
			if (!ExcludeMocPrints)
				return false;

			DateTime easternTime = ConvertOrcaPrintTimeToEastern(chartTime);
			TimeSpan start = MocStartTimeEt;
			TimeSpan end = MocEndTimeEt;
			TimeSpan timeOfDay = easternTime.TimeOfDay;

			if (start == end)
				return false;
			if (start < end)
				return timeOfDay >= start && timeOfDay < end;

			return timeOfDay >= start || timeOfDay < end;
		}

		private DateTime ConvertOrcaPrintTimeToEastern(DateTime chartTime)
		{
			TimeZoneInfo source = orcaPrintChartTimeZone ?? TimeZoneInfo.Local;
			TimeZoneInfo destination = orcaPrintEasternTimeZone ?? TimeZoneInfo.Local;
			if (string.Equals(source.Id, destination.Id, StringComparison.OrdinalIgnoreCase))
				return DateTime.SpecifyKind(chartTime, DateTimeKind.Unspecified);

			try
			{
				return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(chartTime, DateTimeKind.Unspecified), source, destination);
			}
			catch
			{
				return DateTime.SpecifyKind(chartTime, DateTimeKind.Unspecified);
			}
		}

		private void RefreshSharedProfileRegistrationIfNeeded()
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastSharedProfileRegistrationUtc).TotalSeconds < 5)
				return;

			RegisterSharedProfileSource(false);
		}

		private void RegisterSharedProfileSource(bool announce)
		{
			lastSharedProfileRegistrationUtc = DateTime.UtcNow;
			if (!PublishSharedProfileCache)
			{
				OrcaProfileDataCache.UnregisterSource(sharedProfileSourceId);
				return;
			}

			string key = OrcaProfileDataCache.BuildKey(Bars);
			RegisterSharedProfileSourceForKey(key);

			string chartKey = OrcaProfileDataCache.BuildKey(Bars, ChartControl);
			if (!string.IsNullOrEmpty(chartKey) && chartKey != key)
				RegisterSharedProfileSourceForKey(chartKey);
		}

		private void RegisterSharedProfileSourceForKey(string key)
		{
			if (string.IsNullOrEmpty(key))
				return;

			OrcaProfileDataCache.RegisterSource(new OrcaProfileDataSource
			{
				SourceId = sharedProfileSourceId,
				Key = key,
				SourceName = "OrcaPrints live VAP",
				SyncRoot = sharedProfileSync,
				VolumeByBar = sharedProfileVolumeMaps,
				UpVolumeByBar = sharedProfileUpVolumeMaps,
				DownVolumeByBar = sharedProfileDownVolumeMaps,
				RevisionProvider = () => sharedProfileRevision,
				LastUpdatedUtcProvider = () => sharedProfileLastUpdatedUtc,
				CoverageProvider = () => sharedProfileCoverageBarCount
			});

			if (OrcaDiagnosticsCore.IsEnabled)
			{
				EnsureDiagnosticsRegistered();
				OrcaDiagnosticsCore.ReportCacheStatus(diagnosticsInstanceId, "SharedChartCache", "registered " + key);
			}
		}

		private void ClearSharedProfileCache()
		{
			lock (sharedProfileSync)
			{
				if (sharedProfileVolumeMaps != null) sharedProfileVolumeMaps.Clear();
				if (sharedProfileUpVolumeMaps != null) sharedProfileUpVolumeMaps.Clear();
				if (sharedProfileDownVolumeMaps != null) sharedProfileDownVolumeMaps.Clear();
				sharedProfilePrevLast = double.NaN;
				sharedProfileLastDirection = 0;
				sharedProfileCoverageBarCount = 0;
				sharedProfileRevision++;
				sharedProfileLastUpdatedUtc = DateTime.UtcNow;
			}

		}

		private void UpdateSharedProfileCache(DateTime tickTime, double price, long volume, AggressorSide side)
		{
			if (!PublishSharedProfileCache)
				return;
			if (volume <= 0 || tickTime == DateTime.MinValue || double.IsNaN(price) || double.IsInfinity(price))
				return;

			int primaryIndex = GetSharedProfilePrimaryIndex(tickTime);
			if (primaryIndex < 0)
				return;

			double priceKey = NormalizePriceToTick(price);

			lock (sharedProfileSync)
			{
				EnsureSharedProfileBarMaps(primaryIndex);
				long signed = ResolveSharedProfileSignedVolume(price, volume, side);

				Dictionary<double, long> volumeMap = sharedProfileVolumeMaps[primaryIndex];
				bool wasEmptyBar = volumeMap.Count == 0;
				AddSharedProfileVolume(volumeMap, priceKey, volume);

				if (signed > 0)
					AddSharedProfileVolume(sharedProfileUpVolumeMaps[primaryIndex], priceKey, volume);
				else if (signed < 0)
					AddSharedProfileVolume(sharedProfileDownVolumeMaps[primaryIndex], priceKey, volume);

				if (wasEmptyBar)
					sharedProfileCoverageBarCount++;

				sharedProfileRevision++;
				sharedProfileLastUpdatedUtc = DateTime.UtcNow;
			}

			if (OrcaDiagnosticsCore.IsEnabled)
				OrcaDiagnosticsCore.ReportModelUpdate(diagnosticsInstanceId, tickTime, null);
		}

		private int GetSharedProfilePrimaryIndex(DateTime tickTime)
		{
			int primaryIndex = -1;
			try
			{
				if (BarsArray != null && BarsArray.Length > 0 && BarsArray[0] != null)
					primaryIndex = BarsArray[0].GetBar(tickTime);
			}
			catch { }

			if (primaryIndex < 0 && CurrentBar >= 0)
				primaryIndex = CurrentBar;

			return primaryIndex;
		}

		private void EnsureSharedProfileBarMaps(int primaryIndex)
		{
			while (sharedProfileVolumeMaps.Count <= primaryIndex)
				sharedProfileVolumeMaps.Add(new Dictionary<double, long>());
			while (sharedProfileUpVolumeMaps.Count <= primaryIndex)
				sharedProfileUpVolumeMaps.Add(new Dictionary<double, long>());
			while (sharedProfileDownVolumeMaps.Count <= primaryIndex)
				sharedProfileDownVolumeMaps.Add(new Dictionary<double, long>());
		}

		private long ResolveSharedProfileSignedVolume(double price, long volume, AggressorSide side)
		{
			long signed = 0;
			if (side == AggressorSide.Buy)
				signed = volume;
			else if (side == AggressorSide.Sell)
				signed = -volume;
			else if (!double.IsNaN(sharedProfilePrevLast))
			{
				if (price > sharedProfilePrevLast)
					signed = volume;
				else if (price < sharedProfilePrevLast)
					signed = -volume;
				else
					signed = sharedProfileLastDirection * volume;
			}

			sharedProfilePrevLast = price;
			if (signed > 0)
				sharedProfileLastDirection = 1;
			else if (signed < 0)
				sharedProfileLastDirection = -1;

			return signed;
		}

		private void AddSharedProfileVolume(Dictionary<double, long> map, double price, long volume)
		{
			if (map == null || volume <= 0 || double.IsNaN(price) || double.IsInfinity(price))
				return;

			long existing;
			if (map.TryGetValue(price, out existing))
				map[price] = existing + volume;
			else
				map[price] = volume;
		}

		private void UpdatePriceLevelAccumulation(OrcaPrintTick tick)
		{
			if (priceLevelAccumulators == null || highDeltaWindowAccumulators == null || CurrentBar < 0)
				return;

			if (priceLevelAccumulatorBarIndex != CurrentBar)
			{
				priceLevelAccumulators.Clear();
				highDeltaWindowAccumulators.Clear();
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

			UpdateVolumeDominancePriceLevel(accumulator);
			if (UsesHighDeltaPriceLevels())
				UpdateHighDeltaWindows(tick);
		}

		private bool UsesVolumeDominancePriceLevels()
		{
			return PriceLevelHighlightMode == NinjaTrader.NinjaScript.Indicators.PriceLevelHighlightMode.VolumeAndDominance
				|| PriceLevelHighlightMode == NinjaTrader.NinjaScript.Indicators.PriceLevelHighlightMode.Either;
		}

		private bool UsesHighDeltaPriceLevels()
		{
			return PriceLevelHighlightMode == NinjaTrader.NinjaScript.Indicators.PriceLevelHighlightMode.HighDelta
				|| PriceLevelHighlightMode == NinjaTrader.NinjaScript.Indicators.PriceLevelHighlightMode.Either;
		}

		private void UpdateVolumeDominancePriceLevel(PriceLevelAccumulator accumulator)
		{
			if (accumulator == null)
				return;

			bool passesVolume = accumulator.TotalVolume >= PriceLevelMinVolume;
			bool passesDominance = !PriceLevelRequireMinDominance || GetPriceLevelDominantPercent(accumulator) >= PriceLevelMinDominancePercent;
			if (!UsesVolumeDominancePriceLevels() || !passesVolume || !passesDominance)
			{
				RemovePriceLevelEvent(accumulator);
				return;
			}

			if (accumulator.Event == null)
			{
				accumulator.Event = new PriceLevelEvent();
				AddPrintEvent(accumulator.Event);
			}

			UpdatePriceLevelEvent(accumulator);
		}

		private void RemovePriceLevelEvent(PriceLevelAccumulator accumulator)
		{
			if (accumulator == null || accumulator.Event == null)
				return;

			RemovePrintEvent(accumulator.Event);
			accumulator.Event = null;
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
			printEvent.MinPrice = accumulator.Price;
			printEvent.MaxPrice = accumulator.Price;
			printEvent.Volume = accumulator.TotalVolume;
			printEvent.BuyVolume = accumulator.BuyVolume;
			printEvent.SellVolume = accumulator.SellVolume;
			printEvent.Delta = accumulator.BuyVolume - accumulator.SellVolume;
			printEvent.ChildCount = accumulator.ChildCount;
			printEvent.WindowTicks = 1;
			printEvent.IsHighDelta = false;
			printEvent.Side = printEvent.Delta >= 0 ? AggressorSide.Buy : AggressorSide.Sell;
		}

		private void UpdateHighDeltaWindows(OrcaPrintTick tick)
		{
			if (TickSize <= 0 || highDeltaWindowAccumulators == null)
				return;

			int windowTicks = Math.Max(1, PriceLevelDeltaWindowTicks);
			long tickIndex = GetPriceLevelTickIndex(tick.Price);
			for (int offset = 0; offset < windowTicks; offset++)
			{
				long startTickIndex = tickIndex - offset;
				HighDeltaWindowAccumulator window;
				if (!highDeltaWindowAccumulators.TryGetValue(startTickIndex, out window) || window == null)
				{
					window = new HighDeltaWindowAccumulator
					{
						StartTime = tick.Time,
						EndTime = tick.Time,
						StartTickIndex = startTickIndex
					};
					highDeltaWindowAccumulators[startTickIndex] = window;
				}

				if (tick.Time < window.StartTime)
					window.StartTime = tick.Time;
				if (tick.Time > window.EndTime)
					window.EndTime = tick.Time;
				window.ChildCount++;
				if (tick.Side == AggressorSide.Buy)
					window.BuyVolume += tick.Size;
				else if (tick.Side == AggressorSide.Sell)
					window.SellVolume += tick.Size;
			}

			long reevaluateStart = tickIndex - (2L * (windowTicks - 1));
			long reevaluateEnd = tickIndex + (windowTicks - 1);
			for (long startTickIndex = reevaluateStart; startTickIndex <= reevaluateEnd; startTickIndex++)
				RefreshHighDeltaWindowEvent(startTickIndex, windowTicks);
		}

		private void RefreshHighDeltaWindowEvent(long startTickIndex, int windowTicks)
		{
			HighDeltaWindowAccumulator window;
			if (!highDeltaWindowAccumulators.TryGetValue(startTickIndex, out window) || window == null)
				return;

			if (!QualifiesAsHighDelta(window.Delta) || !IsStrongestOverlappingDeltaWindow(window, windowTicks))
			{
				RemoveHighDeltaWindowEvent(window);
				return;
			}

			if (window.Event == null)
			{
				window.Event = new PriceLevelEvent();
				AddPrintEvent(window.Event);
			}

			UpdateHighDeltaWindowEvent(window, windowTicks);
		}

		private bool QualifiesAsHighDelta(long delta)
		{
			long magnitude = GetAbsoluteDelta(delta);
			if (magnitude < Math.Max(1L, PriceLevelMinDelta))
				return false;

			if (PriceLevelDeltaDirection == NinjaTrader.NinjaScript.Indicators.PriceLevelDeltaDirection.PositiveOnly)
				return delta > 0;
			if (PriceLevelDeltaDirection == NinjaTrader.NinjaScript.Indicators.PriceLevelDeltaDirection.NegativeOnly)
				return delta < 0;
			return delta != 0;
		}

		private bool IsStrongestOverlappingDeltaWindow(HighDeltaWindowAccumulator window, int windowTicks)
		{
			long magnitude = GetAbsoluteDelta(window.Delta);
			long firstOverlappingStart = window.StartTickIndex - (windowTicks - 1);
			long lastOverlappingStart = window.StartTickIndex + (windowTicks - 1);
			for (long otherStart = firstOverlappingStart; otherStart <= lastOverlappingStart; otherStart++)
			{
				if (otherStart == window.StartTickIndex)
					continue;

				HighDeltaWindowAccumulator other;
				if (!highDeltaWindowAccumulators.TryGetValue(otherStart, out other) || other == null || !QualifiesAsHighDelta(other.Delta))
					continue;

				long otherMagnitude = GetAbsoluteDelta(other.Delta);
				if (otherMagnitude > magnitude || (otherMagnitude == magnitude && otherStart < window.StartTickIndex))
					return false;
			}

			return true;
		}

		private void RemoveHighDeltaWindowEvent(HighDeltaWindowAccumulator window)
		{
			if (window == null || window.Event == null)
				return;

			RemovePrintEvent(window.Event);
			window.Event = null;
		}

		private void UpdateHighDeltaWindowEvent(HighDeltaWindowAccumulator window, int windowTicks)
		{
			if (window == null || window.Event == null)
				return;

			PriceLevelEvent printEvent = window.Event;
			printEvent.StartTime = window.StartTime;
			printEvent.EndTime = window.EndTime;
			printEvent.Time = window.EndTime;
			printEvent.Price = GetHighDeltaRepresentativePrice(window, windowTicks);
			printEvent.MinPrice = window.StartTickIndex * TickSize;
			printEvent.MaxPrice = (window.StartTickIndex + windowTicks - 1) * TickSize;
			printEvent.Volume = GetAbsoluteDelta(window.Delta);
			printEvent.BuyVolume = window.BuyVolume;
			printEvent.SellVolume = window.SellVolume;
			printEvent.Delta = window.Delta;
			printEvent.ChildCount = window.ChildCount;
			printEvent.WindowTicks = windowTicks;
			printEvent.IsHighDelta = true;
			printEvent.Side = window.Delta > 0 ? AggressorSide.Buy : AggressorSide.Sell;
		}

		private double GetHighDeltaRepresentativePrice(HighDeltaWindowAccumulator window, int windowTicks)
		{
			long representativeTick = window.StartTickIndex;
			long strongestContribution = 0;
			bool positiveWindow = window.Delta > 0;

			for (int offset = 0; offset < windowTicks; offset++)
			{
				long tickIndex = window.StartTickIndex + offset;
				double price = tickIndex * TickSize;
				PriceLevelAccumulator level;
				if (!priceLevelAccumulators.TryGetValue(price, out level) || level == null)
					continue;

				long contribution = level.BuyVolume - level.SellVolume;
				if ((positiveWindow && contribution > strongestContribution) || (!positiveWindow && contribution < strongestContribution))
				{
					strongestContribution = contribution;
					representativeTick = tickIndex;
				}
			}

			return representativeTick * TickSize;
		}

		private long GetPriceLevelTickIndex(double price)
		{
			return TickSize > 0 ? (long)Math.Round(price / TickSize) : (long)Math.Round(price * 100000000.0);
		}

		private long GetAbsoluteDelta(long delta)
		{
			return delta >= 0 ? delta : -delta;
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

			MarkMatchingSinglePrintsSuppressed(cluster);
			AddPrintEvent(cluster);
		}

		private void MarkMatchingSinglePrintsSuppressed(ClusterEvent cluster)
		{
			if (cluster == null || printEvents == null || printEvents.Count == 0)
				return;

			double clusterPrice = NormalizePriceToTick(cluster.Price);
			double priceTolerance = TickSize > 0 ? TickSize * 0.001 : 0.0000001;

			for (int i = printEvents.Count - 1; i >= 0; i--)
			{
				PrintEvent candidate = printEvents[i];
				if (candidate == null)
					continue;
				if (candidate.Time < cluster.Time)
					break;
				if (candidate.Time != cluster.Time || candidate.Kind != OrcaPrintEventKind.Single)
					continue;

				double candidatePrice = NormalizePriceToTick(candidate.Price);
				if (Math.Abs(candidatePrice - clusterPrice) <= priceTolerance)
					candidate.SuppressedByCluster = true;
			}
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
