#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaProfileDataProvider : Indicator
	{
		private readonly Guid sourceId = Guid.NewGuid();
		private readonly object priceMapSync = new object();
		private List<Dictionary<double, long>> barVolumeMaps;
		private List<Dictionary<double, long>> barUpVolumeMaps;
		private List<Dictionary<double, long>> barDownVolumeMaps;
		private List<OrcaOrderFlowBucket> orderFlowBuckets;
		private Dictionary<DateTime, int> orderFlowBucketIndexes;
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double prevLast = double.NaN;
		private int lastDirection;
		private int lastSeenCurrentBar = -1;
		private int dataRevision;
		private DateTime sourceLastUpdatedUtc = DateTime.MinValue;
		private bool orderFlowCacheDirty;
		private DateTime lastOrderFlowSaveUtc = DateTime.MinValue;
		private DateTime lastOrderFlowPurgeUtc = DateTime.MinValue;
		private DateTime lastRegistrationRefreshUtc = DateTime.MinValue;
		private DateTime lastForcedRegistrationLogUtc = DateTime.MinValue;
		private DateTime lastRegistrationWarningUtc = DateTime.MinValue;
		private bool registrationAnnounced;
		private DispatcherTimer registrationRefreshTimer;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaProfileDataProvider";
				Description = "Shared Orca true volume-at-price/delta data provider for drawing tools and profile components.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DrawOnPricePanel = true;
				DisplayInDataBox = false;
				IsAutoScale = false;
				IsSuspendedWhileInactive = false;
				PaintPriceMarkers = false;
				BarsRequiredToPlot = 0;

				ResetClassificationOnSessionBreak = true;
				PublishChartProfileCache = false;
				OrderFlowBucketSeconds = 1;
				TradingSessionsToKeep = 2;
				MaxTickEventsToKeep = 1000000;
				PersistOrderFlowCache = false;
				AutoSaveIntervalSeconds = 30;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				barVolumeMaps = PublishChartProfileCache ? new List<Dictionary<double, long>>(4096) : null;
				barUpVolumeMaps = PublishChartProfileCache ? new List<Dictionary<double, long>>(4096) : null;
				barDownVolumeMaps = PublishChartProfileCache ? new List<Dictionary<double, long>>(4096) : null;
				orderFlowBuckets = new List<OrcaOrderFlowBucket>(16384);
				orderFlowBucketIndexes = new Dictionary<DateTime, int>(16384);
				ResetTradeClassification();
				LoadOrderFlowCache();
				RegisterDataSource(true);
				StartRegistrationRefreshTimer();
			}
			else if (State == State.Terminated)
			{
				StopRegistrationRefreshTimer();
				SaveOrderFlowCache();
				OrcaProfileDataCache.UnregisterSource(sourceId);
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			RefreshRegistrationIfNeeded();

			if (e.MarketDataType == MarketDataType.Bid)
				lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask)
				lastAsk = e.Price;
			else if (e.MarketDataType == MarketDataType.Last)
			{
				if (e.Ask > 0 && !double.IsNaN(e.Ask))
					lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid))
					lastBid = e.Bid;
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				ProcessTickIntoPrimaryBar();
				return;
			}

			if (BarsInProgress != 0)
				return;

			if (PublishChartProfileCache && CurrentBar != lastSeenCurrentBar)
			{
				lastSeenCurrentBar = CurrentBar;
				lock (priceMapSync)
				{
					EnsureBarMaps(CurrentBar);
				}

				if (ResetClassificationOnSessionBreak && Bars != null && Bars.IsFirstBarOfSession)
					ResetTradeClassification();
			}

			RefreshRegistrationIfNeeded();
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			RefreshRegistrationIfNeeded();
		}

		private void StartRegistrationRefreshTimer()
		{
			StopRegistrationRefreshTimer();
			registrationRefreshTimer = new DispatcherTimer();
			registrationRefreshTimer.Interval = TimeSpan.FromSeconds(2);
			registrationRefreshTimer.Tick += OnRegistrationRefreshTimerTick;
			registrationRefreshTimer.Start();
		}

		private void StopRegistrationRefreshTimer()
		{
			if (registrationRefreshTimer == null)
				return;

			registrationRefreshTimer.Stop();
			registrationRefreshTimer.Tick -= OnRegistrationRefreshTimerTick;
			registrationRefreshTimer = null;
		}

		private void OnRegistrationRefreshTimerTick(object sender, EventArgs e)
		{
			RefreshRegistrationIfNeeded();
		}

		private void RefreshRegistrationIfNeeded()
		{
			DateTime now = DateTime.UtcNow;
			string instrumentKey = OrcaProfileDataCache.BuildInstrumentKey(Bars);
			bool missingOrderFlowRegistration = !string.IsNullOrEmpty(instrumentKey) && !OrcaProfileDataCache.HasOrderFlowSource(instrumentKey);
			if (!missingOrderFlowRegistration && (now - lastRegistrationRefreshUtc).TotalSeconds < 5)
				return;

			RegisterDataSource(missingOrderFlowRegistration && ShouldLogForcedRegistration(now));
		}

		private bool ShouldLogForcedRegistration(DateTime now)
		{
			if ((now - lastForcedRegistrationLogUtc).TotalSeconds < 30)
				return false;

			lastForcedRegistrationLogUtc = now;
			return true;
		}

		private void RegisterDataSource(bool announce)
		{
			lastRegistrationRefreshUtc = DateTime.UtcNow;

			string key = PublishChartProfileCache ? OrcaProfileDataCache.BuildKey(Bars) : string.Empty;
			if (PublishChartProfileCache && !string.IsNullOrEmpty(key))
			{
				RegisterChartProfileSourceForKey(key);

				string chartKey = OrcaProfileDataCache.BuildKey(Bars, ChartControl);
				if (!string.IsNullOrEmpty(chartKey) && chartKey != key)
					RegisterChartProfileSourceForKey(chartKey);
			}
			else if (PublishChartProfileCache)
			{
				PrintRegistrationWarning("OrcaProfileDataProvider: could not build chart profile cache key; instrument master registration will still be attempted.");
			}

			string instrumentKey = OrcaProfileDataCache.BuildInstrumentKey(Bars);
			if (!string.IsNullOrEmpty(instrumentKey))
			{
				OrcaProfileDataCache.RegisterOrderFlowSource(new OrcaOrderFlowDataSource
				{
					SourceId = sourceId,
					Key = instrumentKey,
					SourceName = "OrcaProfileDataProvider",
					SyncRoot = priceMapSync,
					Buckets = orderFlowBuckets,
					RevisionProvider = () => dataRevision,
					LastUpdatedUtcProvider = () => sourceLastUpdatedUtc,
					BucketSecondsProvider = () => Math.Max(0, OrderFlowBucketSeconds)
				});
				if (announce || !registrationAnnounced)
				{
					Print("[" + DateTime.Now.ToString("HH:mm:ss") + "] OrcaProfileDataProvider: registered order-flow source for " + instrumentKey + " bucketSeconds=" + Math.Max(0, OrderFlowBucketSeconds) + " sessionsToKeep=" + TradingSessionsToKeep);
					registrationAnnounced = true;
				}
			}
			else
			{
				PrintRegistrationWarning("OrcaProfileDataProvider: could not register order-flow source because instrument key is empty.");
			}
		}

		private void PrintRegistrationWarning(string message)
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastRegistrationWarningUtc).TotalSeconds < 30)
				return;

			lastRegistrationWarningUtc = now;
			Print("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
		}

		private void RegisterChartProfileSourceForKey(string key)
		{
			if (string.IsNullOrEmpty(key))
				return;

			OrcaProfileDataCache.RegisterSource(new OrcaProfileDataSource
			{
				SourceId = sourceId,
				Key = key,
				SourceName = "OrcaProfileDataProvider",
				SyncRoot = priceMapSync,
				VolumeByBar = barVolumeMaps,
				UpVolumeByBar = barUpVolumeMaps,
				DownVolumeByBar = barDownVolumeMaps,
				RevisionProvider = () => dataRevision,
				LastUpdatedUtcProvider = () => sourceLastUpdatedUtc
			});
		}

		private void EnsureBarMaps(int primaryBarIndex)
		{
			if (primaryBarIndex < 0)
				return;

			while (barVolumeMaps.Count <= primaryBarIndex)
				barVolumeMaps.Add(new Dictionary<double, long>());
			while (barUpVolumeMaps.Count <= primaryBarIndex)
				barUpVolumeMaps.Add(new Dictionary<double, long>());
			while (barDownVolumeMaps.Count <= primaryBarIndex)
				barDownVolumeMaps.Add(new Dictionary<double, long>());
		}

		private void ProcessTickIntoPrimaryBar()
		{
			if (BarsArray == null || BarsArray.Length < 2 || CurrentBars == null || CurrentBars.Length < 2 || CurrentBars[1] < 0)
				return;

			DateTime tickTime = Times[1][0];
			int primaryIndex = -1;
			if (PublishChartProfileCache)
			{
				primaryIndex = BarsArray[0].GetBar(tickTime);
				if (primaryIndex < 0)
					return;
			}

			double price = NormalizeToTick(Closes[1][0]);
			long volume = (long)Volumes[1][0];
				if (volume <= 0 || double.IsNaN(price) || double.IsInfinity(price))
				return;

			bool usedBidAsk;
			bool usedFallback;
			long signedVolume = ClassifySignedVolume(price, volume, out usedBidAsk, out usedFallback);
			lock (priceMapSync)
			{
				if (PublishChartProfileCache)
				{
					EnsureBarMaps(primaryIndex);
					AddToMap(barVolumeMaps[primaryIndex], price, volume);

					if (signedVolume > 0)
						AddToMap(barUpVolumeMaps[primaryIndex], price, volume);
					else if (signedVolume < 0)
						AddToMap(barDownVolumeMaps[primaryIndex], price, volume);
				}

				AddToOrderFlowBucket(tickTime, price, volume, signedVolume, usedBidAsk, usedFallback);
				TryPurgeOldOrderFlowBuckets();
				dataRevision++;
				sourceLastUpdatedUtc = DateTime.UtcNow;
				orderFlowCacheDirty = true;
			}

			TryAutoSaveOrderFlowCache();
		}

		private long ClassifySignedVolume(double price, long volume, out bool usedBidAsk, out bool usedFallback)
		{
			usedBidAsk = false;
			usedFallback = false;
			long signedVolume = 0;
			if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
			{
				if (price >= lastAsk)
				{
					signedVolume = volume;
					usedBidAsk = true;
				}
				else if (price <= lastBid)
				{
					signedVolume = -volume;
					usedBidAsk = true;
				}
				else
				{
					signedVolume = ClassifyByTickDirection(price, volume);
					usedFallback = signedVolume != 0;
				}
			}
			else if (!double.IsNaN(prevLast))
			{
				signedVolume = ClassifyByTickDirection(price, volume);
				usedFallback = signedVolume != 0;
			}

			prevLast = price;
			if (signedVolume > 0)
				lastDirection = 1;
			else if (signedVolume < 0)
				lastDirection = -1;

			return signedVolume;
		}

		private void AddToOrderFlowBucket(DateTime tickTime, double price, long volume, long signedVolume, bool usedBidAsk, bool usedFallback)
		{
			if (orderFlowBuckets == null || orderFlowBucketIndexes == null)
				return;

			if (OrderFlowBucketSeconds <= 0)
			{
				OrcaOrderFlowBucket tickBucket = new OrcaOrderFlowBucket { Time = tickTime };
				tickBucket.Add(price, volume, signedVolume, usedBidAsk, usedFallback);
				orderFlowBuckets.Add(tickBucket);
				return;
			}

			DateTime bucketTime = GetBucketTime(tickTime);
			int bucketIndex;
			OrcaOrderFlowBucket bucket;
			if (!orderFlowBucketIndexes.TryGetValue(bucketTime, out bucketIndex))
			{
				bucket = new OrcaOrderFlowBucket { Time = bucketTime };
				orderFlowBucketIndexes[bucketTime] = orderFlowBuckets.Count;
				orderFlowBuckets.Add(bucket);
			}
			else
			{
				bucket = orderFlowBuckets[bucketIndex];
			}

			bucket.Add(price, volume, signedVolume, usedBidAsk, usedFallback);
		}

		private void TryPurgeOldOrderFlowBuckets()
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastOrderFlowPurgeUtc).TotalMinutes < 1)
				return;

			PurgeOldOrderFlowBuckets();
			lastOrderFlowPurgeUtc = now;
		}

		private DateTime GetBucketTime(DateTime time)
		{
			int seconds = Math.Max(1, OrderFlowBucketSeconds);
			long ticksPerBucket = TimeSpan.TicksPerSecond * (long)seconds;
			long ticks = (time.Ticks / ticksPerBucket) * ticksPerBucket;
			return new DateTime(ticks, time.Kind);
		}

		private void PurgeOldOrderFlowBuckets()
		{
			if (orderFlowBuckets == null || orderFlowBuckets.Count == 0 || TradingSessionsToKeep <= 0)
				return;

			DateTime cutoffSession = GetRetentionCutoffSession();
			bool removedAny = false;

			if (cutoffSession != DateTime.MinValue)
			{
				for (int index = orderFlowBuckets.Count - 1; index >= 0; index--)
				{
					OrcaOrderFlowBucket bucket = orderFlowBuckets[index];
					if (bucket != null && GetTradingSessionDate(bucket.Time) < cutoffSession)
					{
						orderFlowBuckets.RemoveAt(index);
						removedAny = true;
					}
				}
			}

			if (OrderFlowBucketSeconds <= 0 && MaxTickEventsToKeep > 0 && orderFlowBuckets.Count > MaxTickEventsToKeep)
			{
				int removeCount = orderFlowBuckets.Count - MaxTickEventsToKeep;
				orderFlowBuckets.RemoveRange(0, removeCount);
				removedAny = true;
			}

			if (removedAny)
			{
				RebuildOrderFlowBucketIndex();
				orderFlowCacheDirty = true;
			}
		}

		private DateTime GetRetentionCutoffSession()
		{
			List<DateTime> sessions = new List<DateTime>();
			for (int index = 0; index < orderFlowBuckets.Count; index++)
			{
				OrcaOrderFlowBucket bucket = orderFlowBuckets[index];
				if (bucket == null)
					continue;

				DateTime sessionDate = GetTradingSessionDate(bucket.Time);
				if (sessionDate.DayOfWeek == DayOfWeek.Saturday || sessionDate.DayOfWeek == DayOfWeek.Sunday)
					continue;

				if (!sessions.Contains(sessionDate))
					sessions.Add(sessionDate);
			}

			if (sessions.Count == 0)
				return DateTime.MinValue;

			sessions.Sort();
			int keep = Math.Max(1, TradingSessionsToKeep);
			int cutoffIndex = Math.Max(0, sessions.Count - keep);
			return sessions[cutoffIndex];
		}

		private DateTime GetTradingSessionDate(DateTime time)
		{
			DateTime sessionDate = time.TimeOfDay >= new TimeSpan(18, 0, 0) ? time.Date.AddDays(1) : time.Date;
			if (sessionDate.DayOfWeek == DayOfWeek.Saturday)
				return sessionDate.AddDays(2);
			if (sessionDate.DayOfWeek == DayOfWeek.Sunday)
				return sessionDate.AddDays(1);
			return sessionDate;
		}

		private void RebuildOrderFlowBucketIndex()
		{
			orderFlowBucketIndexes.Clear();
			for (int index = 0; index < orderFlowBuckets.Count; index++)
			{
				if (orderFlowBuckets[index] != null)
					orderFlowBucketIndexes[orderFlowBuckets[index].Time] = index;
			}
		}

		private long ClassifyByTickDirection(double price, long volume)
		{
			if (double.IsNaN(prevLast))
				return 0;
			if (price > prevLast)
				return volume;
			if (price < prevLast)
				return -volume;
			return lastDirection * volume;
		}

		private void ResetTradeClassification()
		{
			lastBid = double.NaN;
			lastAsk = double.NaN;
			prevLast = double.NaN;
			lastDirection = 0;
		}

		private void AddToMap(Dictionary<double, long> map, double price, long volume)
		{
			long existing;
			if (map.TryGetValue(price, out existing))
				map[price] = existing + volume;
			else
				map[price] = volume;
		}

		private double NormalizeToTick(double price)
		{
			if (TickSize <= 0 || double.IsNaN(TickSize) || double.IsInfinity(TickSize))
				return price;
			return Math.Round(price / TickSize, MidpointRounding.AwayFromZero) * TickSize;
		}

		private void TryAutoSaveOrderFlowCache()
		{
			if (!PersistOrderFlowCache || AutoSaveIntervalSeconds <= 0 || !orderFlowCacheDirty)
				return;

			DateTime now = DateTime.UtcNow;
			if ((now - lastOrderFlowSaveUtc).TotalSeconds < AutoSaveIntervalSeconds)
				return;

			SaveOrderFlowCache();
		}

		private void LoadOrderFlowCache()
		{
			if (!PersistOrderFlowCache)
				return;

			string path = GetOrderFlowCachePath();
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				return;

			try
			{
				using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					string header = reader.ReadString();
					if (header != "ORCA_ORDER_FLOW_CACHE_V1" && header != "ORCA_ORDER_FLOW_CACHE_V2")
						return;
					bool hasStoredPrice = header == "ORCA_ORDER_FLOW_CACHE_V2";

					string instrumentName = reader.ReadString();
					int bucketSeconds = reader.ReadInt32();
					if (instrumentName != Instrument.FullName || bucketSeconds != Math.Max(0, OrderFlowBucketSeconds))
						return;

					int count = reader.ReadInt32();
					for (int index = 0; index < count; index++)
					{
						OrcaOrderFlowBucket bucket = new OrcaOrderFlowBucket
						{
							Time = new DateTime(reader.ReadInt64(), DateTimeKind.Unspecified),
							Price = hasStoredPrice ? reader.ReadDouble() : double.NaN,
							Volume = reader.ReadInt64(),
							AskVolume = reader.ReadInt64(),
							BidVolume = reader.ReadInt64(),
							Delta = reader.ReadInt64(),
							MaxDelta = reader.ReadInt64(),
							MinDelta = reader.ReadInt64(),
							BidAskClassifiedVolume = reader.ReadInt64(),
							FallbackClassifiedVolume = reader.ReadInt64(),
							UnclassifiedVolume = reader.ReadInt64(),
							RunningDelta = reader.ReadInt64()
						};

						orderFlowBuckets.Add(bucket);
					}
				}

				PurgeOldOrderFlowBuckets();
				RebuildOrderFlowBucketIndex();
				dataRevision++;
				sourceLastUpdatedUtc = DateTime.UtcNow;
				orderFlowCacheDirty = false;
			}
			catch (Exception ex)
			{
				Print("OrcaProfileDataProvider: could not load order-flow cache: " + ex.Message);
			}
		}

		private void SaveOrderFlowCache()
		{
			if (!PersistOrderFlowCache || orderFlowBuckets == null || !orderFlowCacheDirty)
				return;

			string path = GetOrderFlowCachePath();
			if (string.IsNullOrEmpty(path))
				return;

			List<OrcaOrderFlowBucket> bucketsToWrite;
			lock (priceMapSync)
			{
				PurgeOldOrderFlowBuckets();
				bucketsToWrite = new List<OrcaOrderFlowBucket>(orderFlowBuckets.Count);
				for (int index = 0; index < orderFlowBuckets.Count; index++)
				{
					if (orderFlowBuckets[index] != null)
						bucketsToWrite.Add(orderFlowBuckets[index].Clone());
				}
			}

			try
			{
				string directory = Path.GetDirectoryName(path);
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write("ORCA_ORDER_FLOW_CACHE_V2");
					writer.Write(Instrument.FullName);
					writer.Write(Math.Max(0, OrderFlowBucketSeconds));
					writer.Write(bucketsToWrite.Count);

					for (int index = 0; index < bucketsToWrite.Count; index++)
					{
						OrcaOrderFlowBucket bucket = bucketsToWrite[index];
						writer.Write(bucket.Time.Ticks);
						writer.Write(bucket.Price);
						writer.Write(bucket.Volume);
						writer.Write(bucket.AskVolume);
						writer.Write(bucket.BidVolume);
						writer.Write(bucket.Delta);
						writer.Write(bucket.MaxDelta);
						writer.Write(bucket.MinDelta);
						writer.Write(bucket.BidAskClassifiedVolume);
						writer.Write(bucket.FallbackClassifiedVolume);
						writer.Write(bucket.UnclassifiedVolume);
						writer.Write(bucket.RunningDelta);
					}
				}

				orderFlowCacheDirty = false;
				lastOrderFlowSaveUtc = DateTime.UtcNow;
			}
			catch (Exception ex)
			{
				Print("OrcaProfileDataProvider: could not save order-flow cache: " + ex.Message);
			}
		}

		private string GetOrderFlowCachePath()
		{
			if (Instrument == null)
				return string.Empty;

			string root = Path.Combine(NinjaTrader.Core.Globals.UserDataDir, "cache", "Orca", "OrderFlow");
			string instrumentName = SanitizeFileName(Instrument.FullName);
			string bucketLabel = OrderFlowBucketSeconds <= 0 ? "ticks" : Math.Max(1, OrderFlowBucketSeconds) + "s";
			return Path.Combine(root, instrumentName + "_" + bucketLabel + ".ofd");
		}

		private string SanitizeFileName(string value)
		{
			if (string.IsNullOrEmpty(value))
				return "Unknown";

			char[] invalid = Path.GetInvalidFileNameChars();
			char[] chars = value.ToCharArray();
			for (int index = 0; index < chars.Length; index++)
			{
				for (int invalidIndex = 0; invalidIndex < invalid.Length; invalidIndex++)
				{
					if (chars[index] == invalid[invalidIndex])
					{
						chars[index] = '_';
						break;
					}
				}
			}

			return new string(chars);
		}

		[NinjaScriptProperty]
		[Display(Name = "Reset Classification On Session Break", Order = 1, GroupName = "1. Data")]
		public bool ResetClassificationOnSessionBreak { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Publish Chart Profile Cache", Order = 2, GroupName = "1. Data",
			Description = "Leave off for master-provider charts. Turn on only when same-chart drawing tools need chart-specific true volume-at-price maps.")]
		public bool PublishChartProfileCache { get; set; }

		[NinjaScriptProperty]
		[Range(0, 60)]
		[Display(Name = "Order Flow Bucket Seconds", Order = 3, GroupName = "1. Data",
			Description = "0 stores compact individual print events. 1 or higher stores compact time buckets for lighter cache size.")]
		public int OrderFlowBucketSeconds { get; set; }

		[NinjaScriptProperty]
		[Range(1, 14)]
		[Display(Name = "Trading Sessions To Keep", Order = 4, GroupName = "1. Data")]
		public int TradingSessionsToKeep { get; set; }

		[NinjaScriptProperty]
		[Range(0, 5000000)]
		[Display(Name = "Max Tick Events To Keep", Order = 5, GroupName = "1. Data",
			Description = "Only applies when bucket seconds is 0. 0 disables this cap.")]
		public int MaxTickEventsToKeep { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Persist Order Flow Cache", Order = 6, GroupName = "1. Data",
			Description = "Experimental. Leave off until provider cache de-duplication is validated.")]
		public bool PersistOrderFlowCache { get; set; }

		[NinjaScriptProperty]
		[Range(5, 300)]
		[Display(Name = "Auto Save Interval Seconds", Order = 7, GroupName = "1. Data")]
		public int AutoSaveIntervalSeconds { get; set; }
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaProfileDataProvider[] cacheOrcaProfileDataProvider;
		public OrcaProfileDataProvider OrcaProfileDataProvider(bool resetClassificationOnSessionBreak, bool publishChartProfileCache, int orderFlowBucketSeconds, int tradingSessionsToKeep, int maxTickEventsToKeep, bool persistOrderFlowCache, int autoSaveIntervalSeconds)
		{
			return OrcaProfileDataProvider(Input, resetClassificationOnSessionBreak, publishChartProfileCache, orderFlowBucketSeconds, tradingSessionsToKeep, maxTickEventsToKeep, persistOrderFlowCache, autoSaveIntervalSeconds);
		}

		public OrcaProfileDataProvider OrcaProfileDataProvider(ISeries<double> input, bool resetClassificationOnSessionBreak, bool publishChartProfileCache, int orderFlowBucketSeconds, int tradingSessionsToKeep, int maxTickEventsToKeep, bool persistOrderFlowCache, int autoSaveIntervalSeconds)
		{
			if (cacheOrcaProfileDataProvider != null)
				for (int idx = 0; idx < cacheOrcaProfileDataProvider.Length; idx++)
					if (cacheOrcaProfileDataProvider[idx] != null && cacheOrcaProfileDataProvider[idx].ResetClassificationOnSessionBreak == resetClassificationOnSessionBreak && cacheOrcaProfileDataProvider[idx].PublishChartProfileCache == publishChartProfileCache && cacheOrcaProfileDataProvider[idx].OrderFlowBucketSeconds == orderFlowBucketSeconds && cacheOrcaProfileDataProvider[idx].TradingSessionsToKeep == tradingSessionsToKeep && cacheOrcaProfileDataProvider[idx].MaxTickEventsToKeep == maxTickEventsToKeep && cacheOrcaProfileDataProvider[idx].PersistOrderFlowCache == persistOrderFlowCache && cacheOrcaProfileDataProvider[idx].AutoSaveIntervalSeconds == autoSaveIntervalSeconds && cacheOrcaProfileDataProvider[idx].EqualsInput(input))
						return cacheOrcaProfileDataProvider[idx];
			return CacheIndicator<OrcaProfileDataProvider>(new OrcaProfileDataProvider(){ ResetClassificationOnSessionBreak = resetClassificationOnSessionBreak, PublishChartProfileCache = publishChartProfileCache, OrderFlowBucketSeconds = orderFlowBucketSeconds, TradingSessionsToKeep = tradingSessionsToKeep, MaxTickEventsToKeep = maxTickEventsToKeep, PersistOrderFlowCache = persistOrderFlowCache, AutoSaveIntervalSeconds = autoSaveIntervalSeconds }, input, ref cacheOrcaProfileDataProvider);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaProfileDataProvider OrcaProfileDataProvider(bool resetClassificationOnSessionBreak, bool publishChartProfileCache, int orderFlowBucketSeconds, int tradingSessionsToKeep, int maxTickEventsToKeep, bool persistOrderFlowCache, int autoSaveIntervalSeconds)
		{
			return indicator.OrcaProfileDataProvider(Input, resetClassificationOnSessionBreak, publishChartProfileCache, orderFlowBucketSeconds, tradingSessionsToKeep, maxTickEventsToKeep, persistOrderFlowCache, autoSaveIntervalSeconds);
		}

		public Indicators.OrcaProfileDataProvider OrcaProfileDataProvider(ISeries<double> input , bool resetClassificationOnSessionBreak, bool publishChartProfileCache, int orderFlowBucketSeconds, int tradingSessionsToKeep, int maxTickEventsToKeep, bool persistOrderFlowCache, int autoSaveIntervalSeconds)
		{
			return indicator.OrcaProfileDataProvider(input, resetClassificationOnSessionBreak, publishChartProfileCache, orderFlowBucketSeconds, tradingSessionsToKeep, maxTickEventsToKeep, persistOrderFlowCache, autoSaveIntervalSeconds);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaProfileDataProvider OrcaProfileDataProvider(bool resetClassificationOnSessionBreak, bool publishChartProfileCache, int orderFlowBucketSeconds, int tradingSessionsToKeep, int maxTickEventsToKeep, bool persistOrderFlowCache, int autoSaveIntervalSeconds)
		{
			return indicator.OrcaProfileDataProvider(Input, resetClassificationOnSessionBreak, publishChartProfileCache, orderFlowBucketSeconds, tradingSessionsToKeep, maxTickEventsToKeep, persistOrderFlowCache, autoSaveIntervalSeconds);
		}

		public Indicators.OrcaProfileDataProvider OrcaProfileDataProvider(ISeries<double> input , bool resetClassificationOnSessionBreak, bool publishChartProfileCache, int orderFlowBucketSeconds, int tradingSessionsToKeep, int maxTickEventsToKeep, bool persistOrderFlowCache, int autoSaveIntervalSeconds)
		{
			return indicator.OrcaProfileDataProvider(input, resetClassificationOnSessionBreak, publishChartProfileCache, orderFlowBucketSeconds, tradingSessionsToKeep, maxTickEventsToKeep, persistOrderFlowCache, autoSaveIntervalSeconds);
		}
	}
}

#endregion
