#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum FixedRangeProfileCacheTradeSourceMode
	{
		TickReplayLastEvents = 0,
		SecondaryTickSeries = 1
	}

	public class OrcaFixedRangeProfileDataCache : Indicator
	{
		private readonly Guid sourceId = Guid.NewGuid();
		private readonly object mapSync = new object();
		private List<Dictionary<double, long>> volumeByBar;
		private List<Dictionary<double, long>> upVolumeByBar;
		private List<Dictionary<double, long>> downVolumeByBar;
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double previousLast = double.NaN;
		private int lastDirection;
		private int dataRevision;
		private int coverageBarCount;
		private DateTime lastUpdatedUtc = DateTime.MinValue;
		private DateTime lastRegistrationUtc = DateTime.MinValue;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaFixedRangeProfileDataCache";
				Description = "Invisible same-chart Tick Replay/local trade cache for Orca Fixed Range Profile.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DrawOnPricePanel = true;
				DisplayInDataBox = false;
				IsAutoScale = false;
				IsSuspendedWhileInactive = false;
				PaintPriceMarkers = false;
				BarsRequiredToPlot = 0;
				TradeSourceMode = FixedRangeProfileCacheTradeSourceMode.TickReplayLastEvents;
			}
			else if (State == State.Configure)
			{
				if (TradeSourceMode == FixedRangeProfileCacheTradeSourceMode.SecondaryTickSeries)
					AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				volumeByBar = new List<Dictionary<double, long>>(4096);
				upVolumeByBar = new List<Dictionary<double, long>>(4096);
				downVolumeByBar = new List<Dictionary<double, long>>(4096);
				RegisterSource();
			}
			else if (State == State.Historical || State == State.Realtime)
			{
				RegisterSource();
				if (State == State.Historical && TradeSourceMode == FixedRangeProfileCacheTradeSourceMode.TickReplayLastEvents && !IsPrimaryTickReplayEnabled())
					Print("OrcaFixedRangeProfileDataCache: Tick Replay Last Events requires Tick Replay enabled on the chart's primary Data Series. Historical fixed-range data will begin when Last events are available.");
			}
			else if (State == State.Terminated)
			{
				OrcaProfileDataCache.UnregisterSource(sourceId);
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e == null)
				return;

			if (e.MarketDataType == MarketDataType.Bid)
			{
				lastBid = e.Price;
				return;
			}
			if (e.MarketDataType == MarketDataType.Ask)
			{
				lastAsk = e.Price;
				return;
			}
			if (e.MarketDataType != MarketDataType.Last)
				return;

			if (e.Bid > 0 && !double.IsNaN(e.Bid))
				lastBid = e.Bid;
			if (e.Ask > 0 && !double.IsNaN(e.Ask))
				lastAsk = e.Ask;

			if (TradeSourceMode == FixedRangeProfileCacheTradeSourceMode.TickReplayLastEvents)
			{
				DateTime time = e.Time == DateTime.MinValue ? GetCurrentPrimaryTime() : e.Time;
				ProcessTrade(time, e.Price, NormalizeVolume(e.Volume), e.Bid, e.Ask);
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				if (TradeSourceMode == FixedRangeProfileCacheTradeSourceMode.SecondaryTickSeries && CurrentBars != null && CurrentBars.Length > 1 && CurrentBars[1] >= 0)
					ProcessTrade(Times[1][0], Closes[1][0], NormalizeVolume(Volumes[1][0]), double.NaN, double.NaN);
				return;
			}

			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			lock (mapSync)
				EnsureBarMaps(CurrentBar);
			if ((DateTime.UtcNow - lastRegistrationUtc).TotalSeconds >= 5)
				RegisterSource();
		}

		private void ProcessTrade(DateTime time, double price, long volume, double bidAtTrade, double askAtTrade)
		{
			if (time == DateTime.MinValue || volume <= 0 || double.IsNaN(price) || double.IsInfinity(price))
				return;

			int primaryBar = ResolvePrimaryBarIndex(time, price);
			if (primaryBar < 0)
				return;

			price = NormalizeToTick(price);
			long signedVolume = ClassifySignedVolume(price, volume, bidAtTrade, askAtTrade);
			lock (mapSync)
			{
				EnsureBarMaps(primaryBar);
				Dictionary<double, long> volumeMap = volumeByBar[primaryBar];
				if (volumeMap.Count == 0)
					coverageBarCount++;
				AddToMap(volumeMap, price, volume);
				if (signedVolume > 0)
					AddToMap(upVolumeByBar[primaryBar], price, volume);
				else if (signedVolume < 0)
					AddToMap(downVolumeByBar[primaryBar], price, volume);
				dataRevision++;
				lastUpdatedUtc = DateTime.UtcNow;
			}
		}

		private void RegisterSource()
		{
			lastRegistrationUtc = DateTime.UtcNow;
			RegisterSourceForKey(OrcaProfileDataCache.BuildKey(Bars));
			string chartKey = OrcaProfileDataCache.BuildKey(Bars, ChartControl);
			if (!string.IsNullOrEmpty(chartKey))
				RegisterSourceForKey(chartKey);
		}

		private void RegisterSourceForKey(string key)
		{
			if (string.IsNullOrEmpty(key))
				return;

			OrcaProfileDataCache.RegisterSource(new OrcaProfileDataSource
			{
				SourceId = sourceId,
				Key = key,
				SourceName = TradeSourceMode == FixedRangeProfileCacheTradeSourceMode.TickReplayLastEvents
					? "OrcaFixedRangeProfileDataCache.TickReplay"
					: "OrcaFixedRangeProfileDataCache.SecondaryTick",
				SyncRoot = mapSync,
				VolumeByBar = volumeByBar,
				UpVolumeByBar = upVolumeByBar,
				DownVolumeByBar = downVolumeByBar,
				RevisionProvider = () => dataRevision,
				LastUpdatedUtcProvider = () => lastUpdatedUtc,
				CoverageProvider = () => coverageBarCount
			});
		}

		private void EnsureBarMaps(int primaryBar)
		{
			while (volumeByBar.Count <= primaryBar)
				volumeByBar.Add(new Dictionary<double, long>());
			while (upVolumeByBar.Count <= primaryBar)
				upVolumeByBar.Add(new Dictionary<double, long>());
			while (downVolumeByBar.Count <= primaryBar)
				downVolumeByBar.Add(new Dictionary<double, long>());
		}

		private int ResolvePrimaryBarIndex(DateTime time, double price)
		{
			if (BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null)
				return -1;

			int primaryBar = BarsArray[0].GetBar(time);
			if (primaryBar < 0)
				return -1;

			if (IsPriceInsidePrimaryBar(primaryBar, price))
				return primaryBar;

			int count = BarsArray[0].Count;
			int first = Math.Max(0, primaryBar - 64);
			int last = Math.Min(count - 1, primaryBar + 64);
			int bestBar = -1;
			long bestScore = long.MaxValue;
			for (int index = first; index <= last; index++)
			{
				if (!IsPriceInsidePrimaryBar(index, price))
					continue;

				long timeDistance = GetTimeDistanceTicks(index, time);
				if (timeDistance > TimeSpan.TicksPerSecond * 2L)
					continue;

				long score = timeDistance + Math.Abs(index - primaryBar);
				if (score < bestScore)
				{
					bestScore = score;
					bestBar = index;
				}
			}

			return bestBar >= 0 ? bestBar : primaryBar;
		}

		private bool IsPriceInsidePrimaryBar(int barIndex, double price)
		{
			try
			{
				if (barIndex < 0 || BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null || barIndex >= BarsArray[0].Count)
					return false;
				double tolerance = Math.Max(TickSize * 0.01, 0.0000001);
				return price >= BarsArray[0].GetLow(barIndex) - tolerance && price <= BarsArray[0].GetHigh(barIndex) + tolerance;
			}
			catch { return false; }
		}

		private long GetTimeDistanceTicks(int barIndex, DateTime time)
		{
			try
			{
				long distance = BarsArray[0].GetTime(barIndex).Ticks - time.Ticks;
				return distance < 0 ? -distance : distance;
			}
			catch { return long.MaxValue; }
		}

		private long ClassifySignedVolume(double price, long volume, double bidAtTrade, double askAtTrade)
		{
			double bid = bidAtTrade > 0 && !double.IsNaN(bidAtTrade) ? bidAtTrade : lastBid;
			double ask = askAtTrade > 0 && !double.IsNaN(askAtTrade) ? askAtTrade : lastAsk;
			long signed = 0;
			if (!double.IsNaN(bid) && !double.IsNaN(ask) && bid > 0 && ask >= bid)
			{
				if (price >= ask)
					signed = volume;
				else if (price <= bid)
					signed = -volume;
			}

			if (signed == 0 && !double.IsNaN(previousLast))
			{
				if (price > previousLast)
					signed = volume;
				else if (price < previousLast)
					signed = -volume;
				else
					signed = lastDirection * volume;
			}

			previousLast = price;
			if (signed > 0)
				lastDirection = 1;
			else if (signed < 0)
				lastDirection = -1;
			return signed;
		}

		private long NormalizeVolume(double volume)
		{
			return volume > 0 ? (long)Math.Round(volume) : 0;
		}

		private double NormalizeToTick(double price)
		{
			return TickSize > 0 ? Math.Round(price / TickSize) * TickSize : price;
		}

		private DateTime GetCurrentPrimaryTime()
		{
			try { return CurrentBar >= 0 ? Times[0][0] : DateTime.MinValue; }
			catch { return DateTime.MinValue; }
		}

		private static void AddToMap(Dictionary<double, long> map, double price, long volume)
		{
			long existing;
			if (map.TryGetValue(price, out existing))
				map[price] = existing + volume;
			else
				map[price] = volume;
		}

		private bool IsPrimaryTickReplayEnabled()
		{
			try { return IsTickReplays != null && IsTickReplays.Length > 0 && IsTickReplays[0] == true; }
			catch { return false; }
		}

		[NinjaScriptProperty]
		[Display(Name = "Trade Source Mode", Description = "Tick Replay Last Events uses the primary chart's replayed Last events and adds no hidden Tick 1 series. Secondary Tick Series is an alternative local source.", GroupName = "1. Data", Order = 1)]
		public FixedRangeProfileCacheTradeSourceMode TradeSourceMode { get; set; }
	}
}
