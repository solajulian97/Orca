#region Using declarations
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum OrcaDiagnosticsMode
	{
		Off = 0,
		Live = 1,
		Verbose = 2
	}

	public enum OrcaDiagnosticsWorkKind
	{
		MarketData = 0,
		BarUpdate = 1
	}

	public sealed class OrcaDiagnosticsSnapshot
	{
		public string InstanceId { get; set; }
		public string ModuleName { get; set; }
		public string StateName { get; set; }
		public string ChartName { get; set; }
		public string Instrument { get; set; }
		public string PrimarySeries { get; set; }
		public string TradingHours { get; set; }
		public string TickReplayState { get; set; }
		public string SourceMode { get; set; }
		public string SourceHealth { get; set; }
		public string SecondarySeries { get; set; }
		public string CacheProvider { get; set; }
		public string CacheStatus { get; set; }
		public DateTime CreatedUtc { get; set; }
		public DateTime LastUpdatedUtc { get; set; }
		public DateTime LastInputUtc { get; set; }
		public DateTime LastInputEventTime { get; set; }
		public DateTime LastModelUpdateUtc { get; set; }
		public long MarketDataLastCount { get; set; }
		public long MarketDataBidCount { get; set; }
		public long MarketDataAskCount { get; set; }
		public string BarUpdateCounts { get; set; }
		public long RenderCount { get; set; }
		public long RenderSampleCount { get; set; }
		public double RenderAverageMs { get; set; }
		public double RenderMaxMs { get; set; }
		public long WorkMarketDataSampleCount { get; set; }
		public double WorkMarketDataAverageMs { get; set; }
		public double WorkMarketDataMaxMs { get; set; }
		public long WorkBarUpdateSampleCount { get; set; }
		public double WorkBarUpdateAverageMs { get; set; }
		public double WorkBarUpdateMaxMs { get; set; }
		public string WorkBarUpdateByBip { get; set; }
		public string WorkPhases { get; set; }
		public DateTime CaptureStartUtc { get; set; }
		public double CaptureElapsedSeconds { get; set; }
		public long CaptureEventCount { get; set; }
		public double CaptureAverageEventsPerSecond { get; set; }
		public double CaptureAverageRendersPerSecond { get; set; }
		public double CaptureRenderAverageMs { get; set; }
		public double CaptureAverageLoadScore { get; set; }
		public double CaptureEstimatedWorkMs { get; set; }
		public double CaptureAverageWorkMsPerSecond { get; set; }
		public long CaptureWorkSampleCount { get; set; }
		public double CaptureWorkMaxMs { get; set; }
		public double FeedAgeSeconds { get; set; }
		public double LagSeconds { get; set; }
        public string LagCluster { get; set; }
		public string Warnings { get; set; }
	}

	public static class OrcaDiagnosticsCore
	{
		private const int MaxTrackedBarsInProgress = 16;
		private const int HotTimestampSampleMask = 31;

		private const int WorkSampleMask = 63;
		private static readonly long RollingWindowStopwatchTicks = Math.Max(1L, System.Diagnostics.Stopwatch.Frequency * 5L);
		private sealed class WorkPhaseStats
		{
			public long SampleCount;
			public double TotalMs;
			public double MaxMs;
		}

		private sealed class InstanceRecord
		{
			public string InstanceId;
			public string ModuleName;
			public string StateName = "Unknown";
			public string ChartName = "Unknown";
			public string Instrument = "Unknown";
			public string PrimarySeries = "Unknown";
			public string TradingHours = "Unknown";
			public string TickReplayState = "Unknown";
			public string SourceMode = "Unknown";
			public string SourceHealth = "Unknown";
			public string CacheProvider = string.Empty;
			public string CacheStatus = string.Empty;
			public readonly HashSet<string> Series = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			public readonly long[] BarUpdatesByBip = new long[MaxTrackedBarsInProgress];
			public readonly ConcurrentDictionary<int, long> OverflowBarUpdatesByBip = new ConcurrentDictionary<int, long>();
			public readonly object RenderSync = new object();
			public DateTime CreatedUtc;
			public readonly object WorkSync = new object();
			public long LastUpdatedUtcTicks;
			public long LastInputUtcTicks;
			public long LastInputEventUtcTicks;
			public long LastModelUpdateUtcTicks;
			public long HotPathSampleSequence;
			public long ModelSampleSequence;
			public long MarketDataLastCount;
			public long MarketDataBidCount;
			public long MarketDataAskCount;
			public long RenderCount;
			public long RenderSampleCount;
			public double RenderTotalMs;
			public double RenderMaxMs;
			public long RenderWindowStartTimestamp;
			public long RenderWindowSampleCount;
			public double RenderWindowTotalMs;
			public double RenderWindowMaxMs;
			public long WorkWindowStartTimestamp;
			public long WorkMarketDataSampleCount;
			public double WorkMarketDataTotalMs;
			public double WorkMarketDataMaxMs;
			public readonly long[] WorkBarUpdateSampleCounts = new long[MaxTrackedBarsInProgress];
			public readonly double[] WorkBarUpdateTotalMs = new double[MaxTrackedBarsInProgress];
			public readonly double[] WorkBarUpdateMaxMs = new double[MaxTrackedBarsInProgress];
			public readonly Dictionary<string, WorkPhaseStats> WorkPhases = new Dictionary<string, WorkPhaseStats>(StringComparer.OrdinalIgnoreCase);
			public long CaptureStartUtcTicks;
			public long CaptureWorkMarketDataSampleCount;
			public double CaptureWorkMarketDataTotalMs;
			public double CaptureWorkMaxMs;
			public readonly long[] CaptureWorkBarUpdateSampleCounts = new long[MaxTrackedBarsInProgress];
			public readonly double[] CaptureWorkBarUpdateTotalMs = new double[MaxTrackedBarsInProgress];
			public readonly double[] CaptureWorkBarUpdateMaxMs = new double[MaxTrackedBarsInProgress];
			public readonly Dictionary<string, WorkPhaseStats> CaptureWorkPhases = new Dictionary<string, WorkPhaseStats>(StringComparer.OrdinalIgnoreCase);

		}


        private sealed class LagGroupStats
        {
            public int InstanceCount;
            public int LaggingCount;
            public int HiddenTickCount;
            public double WorstLagSeconds;
            public readonly HashSet<string> Charts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public void Add(InstanceRecord record, double lagSeconds, bool hasHiddenTickSeries)
            {
                InstanceCount++;
                if (lagSeconds > WorstLagSeconds)
                    WorstLagSeconds = lagSeconds;
                if (lagSeconds > 30)
                    LaggingCount++;
                if (hasHiddenTickSeries)
                    HiddenTickCount++;
                if (record != null && !string.IsNullOrWhiteSpace(record.ChartName))
                    Charts.Add(record.ChartName);
            }
        }

		private static readonly object Sync = new object();
		private static readonly ConcurrentDictionary<string, InstanceRecord> Records = new ConcurrentDictionary<string, InstanceRecord>(StringComparer.OrdinalIgnoreCase);
		private static volatile OrcaDiagnosticsMode mode = OrcaDiagnosticsMode.Off;

		public static OrcaDiagnosticsMode Mode
		{
			get { return mode; }
		}

		public static bool IsEnabled
		{
			get { return mode != OrcaDiagnosticsMode.Off; }
		}

		public static void SetMode(OrcaDiagnosticsMode value)
		{
			mode = value;
		}

		public static void SetEnabled(bool enabled)
		{
			mode = enabled ? OrcaDiagnosticsMode.Live : OrcaDiagnosticsMode.Off;
		}

		public static void RegisterInstance(string instanceId, string moduleName, object owner)
		{
			if (string.IsNullOrEmpty(instanceId))
				return;

			try
			{
				DateTime now = DateTime.UtcNow;
				lock (Sync)
				{
					InstanceRecord record = GetOrCreateRecord(instanceId, moduleName, now);
					if (!string.IsNullOrWhiteSpace(moduleName))
						record.ModuleName = moduleName;

					record.ChartName = ResolveChartName(owner);
					record.Instrument = ResolveInstrument(owner);
					record.PrimarySeries = ResolvePrimarySeries(owner);
					record.TradingHours = ResolveTradingHours(owner);
					record.TickReplayState = ResolveTickReplayState(owner);
					SetTimestamp(ref record.LastUpdatedUtcTicks, now);
				}
			}
			catch { }
		}

		public static void UnregisterInstance(string instanceId)
		{
			if (string.IsNullOrEmpty(instanceId))
				return;

			try
			{
				lock (Sync)
				{
					InstanceRecord removed;
					Records.TryRemove(instanceId, out removed);
				}
			}
			catch { }
		}

		public static void ReportState(string instanceId, string stateName)
		{
			if (string.IsNullOrEmpty(instanceId))
				return;

			try
			{
				DateTime now = DateTime.UtcNow;
				lock (Sync)
				{
					InstanceRecord record = GetOrCreateRecord(instanceId, null, now);
					record.StateName = string.IsNullOrWhiteSpace(stateName) ? "Unknown" : stateName;
					if (string.Equals(record.StateName, "Historical", StringComparison.OrdinalIgnoreCase))
						record.SourceHealth = "HistoricalReplay";
					else if (string.Equals(record.StateName, "Realtime", StringComparison.OrdinalIgnoreCase))
						record.SourceHealth = "Live";
					SetTimestamp(ref record.LastUpdatedUtcTicks, now);
				}
			}
			catch { }
		}

		public static void ReportSourceDeclaration(string instanceId, string sourceMode, string sourceHealth, string cacheProvider)
		{
			if (string.IsNullOrEmpty(instanceId))
				return;

			try
			{
				DateTime now = DateTime.UtcNow;
				lock (Sync)
				{
					InstanceRecord record = GetOrCreateRecord(instanceId, null, now);
					if (!string.IsNullOrWhiteSpace(sourceMode))
						record.SourceMode = sourceMode;
					if (!string.IsNullOrWhiteSpace(sourceHealth))
						record.SourceHealth = sourceHealth;
					if (!string.IsNullOrWhiteSpace(cacheProvider))
						record.CacheProvider = cacheProvider;
					SetTimestamp(ref record.LastUpdatedUtcTicks, now);
				}
			}
			catch { }
		}

		public static void ReportSeriesDeclaration(string instanceId, int barsInProgress, string seriesType, string source, string reason)
		{
			if (string.IsNullOrEmpty(instanceId))
				return;

			try
			{
				string entry = "BIP " + barsInProgress.ToString(CultureInfo.InvariantCulture) + " " + Safe(seriesType, "Unknown");
				if (!string.IsNullOrWhiteSpace(source))
					entry += " source=" + source.Trim();
				if (!string.IsNullOrWhiteSpace(reason))
					entry += " reason=" + reason.Trim();

				DateTime now = DateTime.UtcNow;
				lock (Sync)
				{
					InstanceRecord record = GetOrCreateRecord(instanceId, null, now);
					record.Series.Add(entry);
					SetTimestamp(ref record.LastUpdatedUtcTicks, now);
				}
			}
			catch { }
		}

		public static long ReportMarketData(string instanceId, MarketDataType marketDataType, DateTime eventTime)
		{
			if (!IsEnabled || string.IsNullOrEmpty(instanceId))
				return 0;

			try
			{
				InstanceRecord record;
				if (!Records.TryGetValue(instanceId, out record) || record == null)
					return 0;

				if (marketDataType == MarketDataType.Last)
					Interlocked.Increment(ref record.MarketDataLastCount);
				else if (marketDataType == MarketDataType.Bid)
					Interlocked.Increment(ref record.MarketDataBidCount);
				else if (marketDataType == MarketDataType.Ask)
					Interlocked.Increment(ref record.MarketDataAskCount);
				else
				{
					ReportInputHotPath(record, eventTime);
					return 0;
				}

				ReportInputHotPath(record, eventTime);
				return Interlocked.Read(ref record.MarketDataLastCount)
					+ Interlocked.Read(ref record.MarketDataBidCount)
					+ Interlocked.Read(ref record.MarketDataAskCount);
			}
			catch { return 0; }
		}

		public static long ReportBarUpdate(string instanceId, int barsInProgress, DateTime eventTime)
		{
			if (!IsEnabled || string.IsNullOrEmpty(instanceId))
				return 0;

			try
			{
				InstanceRecord record;
				if (!Records.TryGetValue(instanceId, out record) || record == null)
					return 0;

				long sequence;
				if (barsInProgress >= 0 && barsInProgress < record.BarUpdatesByBip.Length)
					sequence = Interlocked.Increment(ref record.BarUpdatesByBip[barsInProgress]);
				else
					sequence = record.OverflowBarUpdatesByBip.AddOrUpdate(barsInProgress, 1, (key, count) => count + 1);

				ReportInputHotPath(record, eventTime);
				return sequence;
			}
			catch { return 0; }
		}

		public static long BeginWorkSample(long eventSequence)
		{
			if (!IsEnabled || eventSequence <= 0 || ((eventSequence - 1) & WorkSampleMask) != 0)
				return 0;
			try { return System.Diagnostics.Stopwatch.GetTimestamp(); }
			catch { return 0; }
		}


		public static void ReportWorkSample(string instanceId, OrcaDiagnosticsWorkKind workKind, int barsInProgress, long startTimestamp)
		{
			if (!IsEnabled || string.IsNullOrEmpty(instanceId) || startTimestamp <= 0)
				return;

			try
			{
				long endTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
				long elapsedTicks = endTimestamp - startTimestamp;
				if (elapsedTicks < 0)
					return;

				InstanceRecord record;
				if (!Records.TryGetValue(instanceId, out record) || record == null)
					return;

				double elapsedMs = (elapsedTicks * 1000.0) / System.Diagnostics.Stopwatch.Frequency;
				lock (record.WorkSync)
				{
					RotateWorkWindow(record, endTimestamp);
					if (workKind == OrcaDiagnosticsWorkKind.MarketData)
					{
						record.WorkMarketDataSampleCount++;
						record.WorkMarketDataTotalMs += elapsedMs;
						if (elapsedMs > record.WorkMarketDataMaxMs)
							record.WorkMarketDataMaxMs = elapsedMs;
						record.CaptureWorkMarketDataSampleCount++;
						record.CaptureWorkMarketDataTotalMs += elapsedMs;
						if (elapsedMs > record.CaptureWorkMaxMs)
							record.CaptureWorkMaxMs = elapsedMs;
					}
					else if (barsInProgress >= 0 && barsInProgress < record.WorkBarUpdateSampleCounts.Length)
					{
						record.WorkBarUpdateSampleCounts[barsInProgress]++;
						record.WorkBarUpdateTotalMs[barsInProgress] += elapsedMs;
						if (elapsedMs > record.WorkBarUpdateMaxMs[barsInProgress])
							record.WorkBarUpdateMaxMs[barsInProgress] = elapsedMs;
						record.CaptureWorkBarUpdateSampleCounts[barsInProgress]++;
						record.CaptureWorkBarUpdateTotalMs[barsInProgress] += elapsedMs;
						if (elapsedMs > record.CaptureWorkBarUpdateMaxMs[barsInProgress])
							record.CaptureWorkBarUpdateMaxMs[barsInProgress] = elapsedMs;
						if (elapsedMs > record.CaptureWorkMaxMs)
							record.CaptureWorkMaxMs = elapsedMs;
					}
				}
			}
			catch { }
		}
		public static void ReportWorkPhaseSample(string instanceId, string phaseName, long elapsedTicks)
		{
			if (!IsEnabled || string.IsNullOrEmpty(instanceId) || string.IsNullOrWhiteSpace(phaseName) || elapsedTicks <= 0)
				return;

			try
			{
				InstanceRecord record;
				if (!Records.TryGetValue(instanceId, out record) || record == null)
					return;

				double elapsedMs = (elapsedTicks * 1000.0) / System.Diagnostics.Stopwatch.Frequency;
				long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
				lock (record.WorkSync)
				{
					RotateWorkWindow(record, timestamp);
					UpdateWorkPhase(record.WorkPhases, phaseName, elapsedMs);
					UpdateWorkPhase(record.CaptureWorkPhases, phaseName, elapsedMs);
				}
			}
			catch { }
		}

		public static void ReportCacheStatus(string instanceId, string cacheProvider, string cacheStatus)
		{
			if (!IsEnabled || string.IsNullOrEmpty(instanceId))
				return;

			try
			{
				DateTime now = DateTime.UtcNow;
				lock (Sync)
				{
					InstanceRecord record = GetOrCreateRecord(instanceId, null, now);
					if (!string.IsNullOrWhiteSpace(cacheProvider))
						record.CacheProvider = cacheProvider;
					if (!string.IsNullOrWhiteSpace(cacheStatus))
						record.CacheStatus = cacheStatus;
					SetTimestamp(ref record.LastUpdatedUtcTicks, now);
				}
			}
			catch { }
		}

		public static void ReportModelUpdate(string instanceId, DateTime modelTime, string sourceHealth)
		{
			if (!IsEnabled || string.IsNullOrEmpty(instanceId))
				return;

			try
			{
				InstanceRecord record;
				if (!Records.TryGetValue(instanceId, out record) || record == null)
					return;

				bool hasHealthChange = !string.IsNullOrWhiteSpace(sourceHealth);
				long sequence = Interlocked.Increment(ref record.ModelSampleSequence);
				if (!hasHealthChange && (sequence & HotTimestampSampleMask) != 1)
					return;

				DateTime now = DateTime.UtcNow;
				DateTime updateTime = modelTime == DateTime.MinValue ? now : ToUtc(modelTime);
				SetTimestamp(ref record.LastModelUpdateUtcTicks, updateTime);
				SetTimestamp(ref record.LastUpdatedUtcTicks, now);
				if (hasHealthChange)
				{
					lock (Sync)
						record.SourceHealth = sourceHealth;
				}
			}
			catch { }
		}

		public static void ReportRenderSample(string instanceId, long startTimestamp)
		{
			if (!IsEnabled || string.IsNullOrEmpty(instanceId) || startTimestamp <= 0)
				return;

			try
			{
				long endTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
				long elapsedTicks = endTimestamp - startTimestamp;
				if (elapsedTicks < 0)
					return;
				double elapsedMs = (elapsedTicks * 1000.0) / System.Diagnostics.Stopwatch.Frequency;
				InstanceRecord record;
				if (!Records.TryGetValue(instanceId, out record) || record == null)
					return;

				DateTime now = DateTime.UtcNow;
				lock (record.RenderSync)
				{
					RotateRenderWindow(record, endTimestamp);
					record.RenderCount++;
					record.RenderSampleCount++;
					record.RenderTotalMs += elapsedMs;
					if (elapsedMs > record.RenderMaxMs)
						record.RenderMaxMs = elapsedMs;
					record.RenderWindowSampleCount++;
					record.RenderWindowTotalMs += elapsedMs;
					if (elapsedMs > record.RenderWindowMaxMs)
						record.RenderWindowMaxMs = elapsedMs;
					SetTimestamp(ref record.LastUpdatedUtcTicks, now);
				}
			}
			catch { }
		}

		public static void ClearCounters()
		{
			try
			{
				DateTime captureStartUtc = DateTime.UtcNow;
				lock (Sync)
				{
					foreach (InstanceRecord record in Records.Values)
					{
						Interlocked.Exchange(ref record.MarketDataLastCount, 0);
						Interlocked.Exchange(ref record.MarketDataBidCount, 0);
						Interlocked.Exchange(ref record.MarketDataAskCount, 0);
						for (int index = 0; index < record.BarUpdatesByBip.Length; index++)
							Interlocked.Exchange(ref record.BarUpdatesByBip[index], 0);
						record.OverflowBarUpdatesByBip.Clear();
						lock (record.RenderSync)
						{
							record.RenderCount = 0;
							record.RenderSampleCount = 0;
							record.RenderTotalMs = 0;
							record.RenderMaxMs = 0;
							record.RenderWindowStartTimestamp = 0;
							record.RenderWindowSampleCount = 0;
							record.RenderWindowTotalMs = 0;
							record.RenderWindowMaxMs = 0;
						}
						lock (record.WorkSync)
						{
							ResetWorkWindow(record, 0);
							ResetCaptureWork(record, captureStartUtc);
						}
						Interlocked.Exchange(ref record.LastInputUtcTicks, 0);
						Interlocked.Exchange(ref record.LastInputEventUtcTicks, 0);
						Interlocked.Exchange(ref record.LastModelUpdateUtcTicks, 0);
						SetTimestamp(ref record.LastUpdatedUtcTicks, captureStartUtc);
					}
				}
			}
			catch { }
		}

		public static List<OrcaDiagnosticsSnapshot> GetSnapshot()
		{
			List<OrcaDiagnosticsSnapshot> snapshots = new List<OrcaDiagnosticsSnapshot>();
			try
			{
				DateTime now = DateTime.UtcNow;
				long snapshotTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
				Dictionary<string, int> hiddenTickCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, LagGroupStats> instrumentLagStats = new Dictionary<string, LagGroupStats>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, LagGroupStats> chartLagStats = new Dictionary<string, LagGroupStats>(StringComparer.OrdinalIgnoreCase);
				lock (Sync)
				{
					foreach (InstanceRecord record in Records.Values)
					{
						if (record == null)
							continue;
                        bool hasHiddenTick = HasHiddenTickSeries(record);
                        if (hasHiddenTick)
						{
							string key = string.IsNullOrWhiteSpace(record.Instrument) ? "Unknown" : record.Instrument;
							int count;
							hiddenTickCounts.TryGetValue(key, out count);
							hiddenTickCounts[key] = count + 1;
						}

					}

					foreach (InstanceRecord record in Records.Values)
					{
						if (record == null)
							continue;
						bool hasHiddenTick = HasHiddenTickSeries(record);
                        double lagSeconds = CalculateProcessingLagSeconds(record);
                        AddLagStat(instrumentLagStats, Safe(record.Instrument, "Unknown"), record, lagSeconds, hasHiddenTick);
                        AddLagStat(chartLagStats, BuildChartGroupKey(record), record, lagSeconds, hasHiddenTick);
					}

					foreach (InstanceRecord record in Records.Values)
					{
						if (record == null)
							continue;
						long renderCount;
						long renderSampleCount;
						double renderTotalMs;
						double renderMaxMs;
						double captureRenderTotalMs;
						long captureRenderSampleCount;
						lock (record.RenderSync)
						{
							RotateRenderWindow(record, snapshotTimestamp);
							renderCount = record.RenderCount;
							renderSampleCount = record.RenderWindowSampleCount;
							renderTotalMs = record.RenderWindowTotalMs;
							renderMaxMs = record.RenderWindowMaxMs;
							captureRenderTotalMs = record.RenderTotalMs;
							captureRenderSampleCount = record.RenderSampleCount;
						}


						long workMarketDataSampleCount;
						double workMarketDataTotalMs;
						double workMarketDataMaxMs;
						long workBarUpdateSampleCount = 0;
						double workBarUpdateTotalMs = 0;
						double workBarUpdateMaxMs = 0;
						string workBarUpdateByBip;
						string workPhases;
						long marketDataLastCount = Interlocked.Read(ref record.MarketDataLastCount);
						long marketDataBidCount = Interlocked.Read(ref record.MarketDataBidCount);
						long marketDataAskCount = Interlocked.Read(ref record.MarketDataAskCount);
						long marketEventCount = marketDataLastCount + marketDataBidCount + marketDataAskCount;
						long barEventCount = 0;
						for (int index = 0; index < record.BarUpdatesByBip.Length; index++)
							barEventCount += Interlocked.Read(ref record.BarUpdatesByBip[index]);
						foreach (KeyValuePair<int, long> pair in record.OverflowBarUpdatesByBip)
							barEventCount += Math.Max(0, pair.Value);

						DateTime captureStartUtc = ReadTimestamp(ref record.CaptureStartUtcTicks);
						double captureElapsedSeconds = captureStartUtc == DateTime.MinValue ? 0 : Math.Max(0, (now - captureStartUtc).TotalSeconds);
						long captureEventCount = marketEventCount + barEventCount;
						double captureAverageEventsPerSecond = captureElapsedSeconds <= 0 ? 0 : captureEventCount / captureElapsedSeconds;
						double captureAverageRendersPerSecond = captureElapsedSeconds <= 0 ? 0 : renderCount / captureElapsedSeconds;
						double captureRenderAverageMs = captureRenderSampleCount <= 0 ? 0 : captureRenderTotalMs / captureRenderSampleCount;
						double captureAverageLoadScore = captureAverageEventsPerSecond
							+ (captureAverageRendersPerSecond * 5.0)
							+ (captureRenderAverageMs * captureAverageRendersPerSecond);
						long captureWorkSampleCount = 0;
						double captureEstimatedWorkMs = 0;
						double captureWorkMaxMs = 0;
						double captureAverageWorkMsPerSecond = 0;

						lock (record.WorkSync)
						{
							RotateWorkWindow(record, snapshotTimestamp);
							workMarketDataSampleCount = record.WorkMarketDataSampleCount;
							workMarketDataTotalMs = record.WorkMarketDataTotalMs;
							workMarketDataMaxMs = record.WorkMarketDataMaxMs;
							for (int index = 0; index < record.WorkBarUpdateSampleCounts.Length; index++)
							{
								workBarUpdateSampleCount += record.WorkBarUpdateSampleCounts[index];
								workBarUpdateTotalMs += record.WorkBarUpdateTotalMs[index];
								if (record.WorkBarUpdateMaxMs[index] > workBarUpdateMaxMs)
									workBarUpdateMaxMs = record.WorkBarUpdateMaxMs[index];
							}
							workBarUpdateByBip = FormatWorkBarUpdateByBip(record);
							workPhases = FormatWorkPhases(record);
							captureWorkSampleCount = record.CaptureWorkMarketDataSampleCount;
							if (record.CaptureWorkMarketDataSampleCount > 0)
								captureEstimatedWorkMs += marketEventCount
									* (record.CaptureWorkMarketDataTotalMs / record.CaptureWorkMarketDataSampleCount);
							for (int index = 0; index < record.CaptureWorkBarUpdateSampleCounts.Length; index++)
							{
								long sampleCount = record.CaptureWorkBarUpdateSampleCounts[index];
								captureWorkSampleCount += sampleCount;
								if (sampleCount <= 0)
									continue;
								long eventCount = Interlocked.Read(ref record.BarUpdatesByBip[index]);
								captureEstimatedWorkMs += eventCount * (record.CaptureWorkBarUpdateTotalMs[index] / sampleCount);
							}
							captureWorkMaxMs = record.CaptureWorkMaxMs;
							captureAverageWorkMsPerSecond = captureElapsedSeconds <= 0 ? 0 : captureEstimatedWorkMs / captureElapsedSeconds;
						}
						OrcaDiagnosticsSnapshot snapshot = new OrcaDiagnosticsSnapshot
						{
							InstanceId = record.InstanceId,
							ModuleName = Safe(record.ModuleName, "Unknown"),
							StateName = Safe(record.StateName, "Unknown"),
							ChartName = Safe(record.ChartName, "Unknown"),
							Instrument = Safe(record.Instrument, "Unknown"),
							PrimarySeries = Safe(record.PrimarySeries, "Unknown"),
							TradingHours = Safe(record.TradingHours, "Unknown"),
							TickReplayState = Safe(record.TickReplayState, "Unknown"),
							SourceMode = Safe(record.SourceMode, "Unknown"),
							SourceHealth = Safe(record.SourceHealth, "Unknown"),
							SecondarySeries = JoinSeries(record.Series),
							CacheProvider = Safe(record.CacheProvider, string.Empty),
							CacheStatus = Safe(record.CacheStatus, string.Empty),
							CreatedUtc = record.CreatedUtc,
							LastUpdatedUtc = ReadTimestamp(ref record.LastUpdatedUtcTicks),
							LastInputUtc = ReadTimestamp(ref record.LastInputUtcTicks),
							LastInputEventTime = ReadTimestamp(ref record.LastInputEventUtcTicks),
							LastModelUpdateUtc = ReadTimestamp(ref record.LastModelUpdateUtcTicks),
							MarketDataLastCount = marketDataLastCount,
							MarketDataBidCount = marketDataBidCount,
							MarketDataAskCount = marketDataAskCount,
							BarUpdateCounts = FormatBarUpdateCounts(record),
							RenderCount = renderCount,
							RenderSampleCount = renderSampleCount,
							RenderAverageMs = renderSampleCount <= 0 ? 0 : renderTotalMs / renderSampleCount,
							RenderMaxMs = renderMaxMs,
							FeedAgeSeconds = CalculateFeedAgeSeconds(record, now),
							LagSeconds = CalculateProcessingLagSeconds(record),
							WorkMarketDataSampleCount = workMarketDataSampleCount,
							WorkMarketDataAverageMs = workMarketDataSampleCount <= 0 ? 0 : workMarketDataTotalMs / workMarketDataSampleCount,
							WorkMarketDataMaxMs = workMarketDataMaxMs,
							WorkBarUpdateSampleCount = workBarUpdateSampleCount,
							WorkBarUpdateAverageMs = workBarUpdateSampleCount <= 0 ? 0 : workBarUpdateTotalMs / workBarUpdateSampleCount,
							WorkBarUpdateMaxMs = workBarUpdateMaxMs,
							WorkBarUpdateByBip = workBarUpdateByBip,
							WorkPhases = workPhases,
							CaptureStartUtc = captureStartUtc,
							CaptureElapsedSeconds = captureElapsedSeconds,
							CaptureEventCount = captureEventCount,
							CaptureAverageEventsPerSecond = captureAverageEventsPerSecond,
							CaptureAverageRendersPerSecond = captureAverageRendersPerSecond,
							CaptureRenderAverageMs = captureRenderAverageMs,
							CaptureAverageLoadScore = captureAverageLoadScore,
							CaptureEstimatedWorkMs = captureEstimatedWorkMs,
							CaptureAverageWorkMsPerSecond = captureAverageWorkMsPerSecond,
							CaptureWorkSampleCount = captureWorkSampleCount,
							CaptureWorkMaxMs = captureWorkMaxMs,
						};
                        snapshot.LagCluster = BuildLagCluster(record, snapshot.LagSeconds, instrumentLagStats, chartLagStats);

						int duplicateHiddenTickCount = 0;
						hiddenTickCounts.TryGetValue(snapshot.Instrument, out duplicateHiddenTickCount);
                        snapshot.Warnings = BuildWarnings(record, snapshot.LagSeconds, duplicateHiddenTickCount, instrumentLagStats, chartLagStats);
						snapshots.Add(snapshot);
					}
				}
			}
			catch { }

			snapshots.Sort((left, right) =>
			{
                int lagCompare = (right == null ? 0 : right.LagSeconds).CompareTo(left == null ? 0 : left.LagSeconds);
                if (lagCompare != 0)
                    return lagCompare;
				int instrumentCompare = string.Compare(left == null ? null : left.Instrument, right == null ? null : right.Instrument, StringComparison.OrdinalIgnoreCase);
				if (instrumentCompare != 0)
					return instrumentCompare;
				return string.Compare(left == null ? null : left.ModuleName, right == null ? null : right.ModuleName, StringComparison.OrdinalIgnoreCase);
			});
			return snapshots;
		}

        private static void AddLagStat(Dictionary<string, LagGroupStats> statsByKey, string key, InstanceRecord record, double lagSeconds, bool hasHiddenTickSeries)
        {
            if (statsByKey == null || string.IsNullOrWhiteSpace(key))
                return;

            LagGroupStats stats;
            if (!statsByKey.TryGetValue(key, out stats))
            {
                stats = new LagGroupStats();
                statsByKey[key] = stats;
            }
            stats.Add(record, lagSeconds, hasHiddenTickSeries);
        }

        private static string BuildChartGroupKey(InstanceRecord record)
        {
            if (record == null)
                return "Unknown";
            return Safe(record.Instrument, "Unknown") + "|" + Safe(record.ChartName, "Unknown") + "|" + Safe(record.PrimarySeries, "Unknown");
        }

        private static string BuildLagCluster(InstanceRecord record, double lagSeconds, Dictionary<string, LagGroupStats> instrumentLagStats, Dictionary<string, LagGroupStats> chartLagStats)
        {
            if (record == null || lagSeconds <= 30)
                return string.Empty;

            List<string> parts = new List<string>();
            LagGroupStats chartStats;
            if (chartLagStats != null && chartLagStats.TryGetValue(BuildChartGroupKey(record), out chartStats) && chartStats.LaggingCount > 1)
                parts.Add("chart rows=" + chartStats.LaggingCount.ToString(CultureInfo.InvariantCulture) + " worst=" + FormatLag(chartStats.WorstLagSeconds));

            LagGroupStats instrumentStats;
            if (instrumentLagStats != null && instrumentLagStats.TryGetValue(Safe(record.Instrument, "Unknown"), out instrumentStats) && instrumentStats.LaggingCount > 1)
            {
                string instrumentText = "instrument rows=" + instrumentStats.LaggingCount.ToString(CultureInfo.InvariantCulture)
                    + " worst=" + FormatLag(instrumentStats.WorstLagSeconds)
                    + " charts=" + instrumentStats.Charts.Count.ToString(CultureInfo.InvariantCulture);
                if (instrumentStats.HiddenTickCount > 0)
                    instrumentText += " hiddenTick=" + instrumentStats.HiddenTickCount.ToString(CultureInfo.InvariantCulture);
                parts.Add(instrumentText);
            }

            return parts.Count == 0 ? string.Empty : string.Join(" | ", parts.ToArray());
        }

        private static string FormatLag(double lagSeconds)
        {
            if (lagSeconds >= 3600)
                return (lagSeconds / 3600.0).ToString("0.0", CultureInfo.InvariantCulture) + "h";
            if (lagSeconds >= 60)
                return (lagSeconds / 60.0).ToString("0.0", CultureInfo.InvariantCulture) + "m";
            return lagSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        }

		private static void RotateRenderWindow(InstanceRecord record, long timestamp)
		{
			if (record == null || timestamp <= 0)
				return;
			if (record.RenderWindowStartTimestamp <= 0)
			{
				record.RenderWindowStartTimestamp = timestamp;
				return;
			}
			if (timestamp - record.RenderWindowStartTimestamp < RollingWindowStopwatchTicks)
				return;

			record.RenderWindowStartTimestamp = timestamp;
			record.RenderWindowSampleCount = 0;
			record.RenderWindowTotalMs = 0;
			record.RenderWindowMaxMs = 0;
		}

		private static void RotateWorkWindow(InstanceRecord record, long timestamp)
		{
			if (record == null || timestamp <= 0)
				return;
			if (record.WorkWindowStartTimestamp <= 0)
			{
				record.WorkWindowStartTimestamp = timestamp;
				return;
			}
			if (timestamp - record.WorkWindowStartTimestamp < RollingWindowStopwatchTicks)
				return;

			ResetWorkWindow(record, timestamp);
		}

		private static void ResetWorkWindow(InstanceRecord record, long timestamp)
		{
			if (record == null)
				return;

			record.WorkWindowStartTimestamp = timestamp;
			record.WorkMarketDataSampleCount = 0;
			record.WorkMarketDataTotalMs = 0;
			record.WorkMarketDataMaxMs = 0;
			Array.Clear(record.WorkBarUpdateSampleCounts, 0, record.WorkBarUpdateSampleCounts.Length);
			Array.Clear(record.WorkBarUpdateTotalMs, 0, record.WorkBarUpdateTotalMs.Length);
			Array.Clear(record.WorkBarUpdateMaxMs, 0, record.WorkBarUpdateMaxMs.Length);
			record.WorkPhases.Clear();
		}

		private static void ResetCaptureWork(InstanceRecord record, DateTime captureStartUtc)
		{
			if (record == null)
				return;

			SetTimestamp(ref record.CaptureStartUtcTicks, captureStartUtc);
			record.CaptureWorkMarketDataSampleCount = 0;
			record.CaptureWorkMarketDataTotalMs = 0;
			record.CaptureWorkMaxMs = 0;
			Array.Clear(record.CaptureWorkBarUpdateSampleCounts, 0, record.CaptureWorkBarUpdateSampleCounts.Length);
			Array.Clear(record.CaptureWorkBarUpdateTotalMs, 0, record.CaptureWorkBarUpdateTotalMs.Length);
			Array.Clear(record.CaptureWorkBarUpdateMaxMs, 0, record.CaptureWorkBarUpdateMaxMs.Length);
			record.CaptureWorkPhases.Clear();
		}

		private static void UpdateWorkPhase(Dictionary<string, WorkPhaseStats> phases, string phaseName, double elapsedMs)
		{
			if (phases == null || string.IsNullOrWhiteSpace(phaseName))
				return;

			WorkPhaseStats stats;
			if (!phases.TryGetValue(phaseName, out stats))
			{
				stats = new WorkPhaseStats();
				phases[phaseName] = stats;
			}
			stats.SampleCount++;
			stats.TotalMs += elapsedMs;
			if (elapsedMs > stats.MaxMs)
				stats.MaxMs = elapsedMs;
		}

		private static string FormatWorkBarUpdateByBip(InstanceRecord record)
		{
			if (record == null)
				return string.Empty;

			List<string> parts = new List<string>();
			for (int index = 0; index < record.WorkBarUpdateSampleCounts.Length; index++)
			{
				long count = record.WorkBarUpdateSampleCounts[index];
				if (count <= 0)
					continue;

				double averageMs = record.WorkBarUpdateTotalMs[index] / count;
				parts.Add("BIP" + index.ToString(CultureInfo.InvariantCulture)
					+ " avg " + averageMs.ToString("0.000", CultureInfo.InvariantCulture)
					+ " max " + record.WorkBarUpdateMaxMs[index].ToString("0.00", CultureInfo.InvariantCulture));
			}
			return parts.Count == 0 ? string.Empty : string.Join(" ", parts.ToArray());
		}

		private static string FormatWorkPhases(InstanceRecord record)
		{
			if (record == null || (record.WorkPhases.Count == 0 && record.CaptureWorkPhases.Count == 0))
				return string.Empty;

			List<string> names = new List<string>();
			foreach (string name in record.CaptureWorkPhases.Keys)
				if (!names.Contains(name))
					names.Add(name);
			foreach (string name in record.WorkPhases.Keys)
				if (!names.Contains(name))
					names.Add(name);
			names.Sort(StringComparer.OrdinalIgnoreCase);

			List<string> parts = new List<string>();
			foreach (string name in names)
			{
				WorkPhaseStats window;
				WorkPhaseStats capture;
				record.WorkPhases.TryGetValue(name, out window);
				record.CaptureWorkPhases.TryGetValue(name, out capture);
				string part = name;
				if (window != null && window.SampleCount > 0)
					part += " 5s " + (window.TotalMs / window.SampleCount).ToString("0.000", CultureInfo.InvariantCulture)
						+ "/" + window.MaxMs.ToString("0.00", CultureInfo.InvariantCulture);
				if (capture != null && capture.SampleCount > 0)
					part += " cap " + (capture.TotalMs / capture.SampleCount).ToString("0.000", CultureInfo.InvariantCulture)
						+ "/" + capture.MaxMs.ToString("0.00", CultureInfo.InvariantCulture)
						+ " n" + capture.SampleCount.ToString(CultureInfo.InvariantCulture);
				parts.Add(part);
			}
			return string.Join(" | ", parts.ToArray());
		}

		private static InstanceRecord GetOrCreateRecord(string instanceId, string moduleName, DateTime now)
		{
			InstanceRecord record;
			if (!Records.TryGetValue(instanceId, out record))
			{
				InstanceRecord created = new InstanceRecord
				{
					InstanceId = instanceId,
					ModuleName = string.IsNullOrWhiteSpace(moduleName) ? "Unknown" : moduleName,
					CreatedUtc = now
				};
				SetTimestamp(ref created.LastUpdatedUtcTicks, now);
				SetTimestamp(ref created.CaptureStartUtcTicks, now);
				record = Records.GetOrAdd(instanceId, created);
			}
			else if (!string.IsNullOrWhiteSpace(moduleName) && string.Equals(record.ModuleName, "Unknown", StringComparison.OrdinalIgnoreCase))
			{
				record.ModuleName = moduleName;
			}

			return record;
		}

		private static void ReportInputHotPath(InstanceRecord record, DateTime eventTime)
		{
			if (record == null)
				return;

			long sequence = Interlocked.Increment(ref record.HotPathSampleSequence);
			if ((sequence & HotTimestampSampleMask) != 1)
				return;

			DateTime now = DateTime.UtcNow;
			SetTimestamp(ref record.LastInputUtcTicks, now);
			if (eventTime != DateTime.MinValue)
				SetTimestamp(ref record.LastInputEventUtcTicks, ToUtc(eventTime));
			SetTimestamp(ref record.LastUpdatedUtcTicks, now);

			if (string.Equals(record.StateName, "Realtime", StringComparison.OrdinalIgnoreCase))
				record.SourceHealth = "Live";
			else if (eventTime != DateTime.MinValue && Math.Abs((ToUtc(eventTime) - now).TotalMinutes) <= 2)
				record.SourceHealth = "Live";
			else if (eventTime != DateTime.MinValue)
				record.SourceHealth = "HistoricalReplay";
		}

		private static void SetTimestamp(ref long targetTicks, DateTime value)
		{
			long ticks = value == DateTime.MinValue ? 0 : ToUtc(value).Ticks;
			Interlocked.Exchange(ref targetTicks, ticks);
		}

		private static DateTime ReadTimestamp(ref long targetTicks)
		{
			long ticks = Interlocked.Read(ref targetTicks);
			if (ticks <= 0)
				return DateTime.MinValue;
			try { return new DateTime(ticks, DateTimeKind.Utc); }
			catch { return DateTime.MinValue; }
		}

		private static bool HasHiddenTickSeries(InstanceRecord record)
		{
			if (record == null || record.Series == null)
				return false;

			foreach (string series in record.Series)
			{
				if (series != null
					&& series.IndexOf("Tick 1", StringComparison.OrdinalIgnoreCase) >= 0
					&& series.IndexOf("source=Component", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}
			return false;
		}

        private static string BuildWarnings(InstanceRecord record, double lagSeconds, int duplicateHiddenTickCount, Dictionary<string, LagGroupStats> instrumentLagStats, Dictionary<string, LagGroupStats> chartLagStats)
		{
			List<string> warnings = new List<string>();
			DateTime lastInputUtc = ReadTimestamp(ref record.LastInputUtcTicks);
			if (lastInputUtc == DateTime.MinValue)
			{
				if (string.Equals(record.SourceHealth, "HistoricalReplay", StringComparison.OrdinalIgnoreCase))
					warnings.Add("NoHistoricalEvents");
				else
					warnings.Add("NoEvents");
			}
			else if (string.Equals(record.SourceHealth, "Live", StringComparison.OrdinalIgnoreCase) && lagSeconds > 10)
				warnings.Add("StaleData");

			if (string.Equals(record.SourceHealth, "Live", StringComparison.OrdinalIgnoreCase) && lagSeconds > 10)
				warnings.Add("BehindRealtime");

            LagGroupStats instrumentStats;
            if (instrumentLagStats != null && instrumentLagStats.TryGetValue(Safe(record.Instrument, "Unknown"), out instrumentStats) && instrumentStats.LaggingCount >= 3 && lagSeconds > 30)
                warnings.Add("InstrumentLagCluster");

            LagGroupStats chartStats;
            if (chartLagStats != null && chartLagStats.TryGetValue(BuildChartGroupKey(record), out chartStats) && chartStats.LaggingCount >= 2 && lagSeconds > 30)
                warnings.Add("ChartLagCluster");

			if (duplicateHiddenTickCount > 1 && HasHiddenTickSeries(record))
				warnings.Add("DuplicateTickSource");

			double renderMaxMs;
			lock (record.RenderSync)
				renderMaxMs = record.RenderWindowMaxMs;
			if (renderMaxMs > 25)
				warnings.Add("ExcessiveRenderTime");

			if (!string.IsNullOrWhiteSpace(record.SourceHealth)
				&& record.SourceHealth.IndexOf("Fallback", StringComparison.OrdinalIgnoreCase) >= 0)
				warnings.Add("FallbackActive");

			if (!string.IsNullOrWhiteSpace(record.CacheStatus)
				&& record.CacheStatus.IndexOf("wait", StringComparison.OrdinalIgnoreCase) >= 0)
				warnings.Add("CacheWait");

			return warnings.Count == 0 ? string.Empty : string.Join(", ", warnings.ToArray());
		}

		private static double CalculateProcessingLagSeconds(InstanceRecord record)
		{
			if (record == null || !string.Equals(record.SourceHealth, "Live", StringComparison.OrdinalIgnoreCase))
				return 0;

			DateTime lastInputEventTime = ReadTimestamp(ref record.LastInputEventUtcTicks);
			DateTime lastInputUtc = ReadTimestamp(ref record.LastInputUtcTicks);
			if (lastInputEventTime == DateTime.MinValue || lastInputUtc == DateTime.MinValue)
				return 0;

			double seconds = (lastInputUtc - lastInputEventTime).TotalSeconds;
			return seconds < 0 ? 0 : seconds;
		}

		private static double CalculateFeedAgeSeconds(InstanceRecord record, DateTime now)
		{
			if (record == null || !string.Equals(record.SourceHealth, "Live", StringComparison.OrdinalIgnoreCase))
				return 0;

			DateTime lastInputEventTime = ReadTimestamp(ref record.LastInputEventUtcTicks);
			if (lastInputEventTime == DateTime.MinValue)
				return 0;

			double seconds = (now - lastInputEventTime).TotalSeconds;
			return seconds < 0 ? 0 : seconds;
		}

		private static DateTime ToUtc(DateTime value)
		{
			if (value == DateTime.MinValue || value == DateTime.MaxValue)
				return value;
			if (value.Kind == DateTimeKind.Utc)
				return value;
			if (value.Kind == DateTimeKind.Local)
				return value.ToUniversalTime();
			try
			{
				return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
			}
			catch
			{
				return value;
			}
		}

		private static string FormatBarUpdateCounts(InstanceRecord record)
		{
			if (record == null)
				return string.Empty;

			Dictionary<int, long> counts = new Dictionary<int, long>();
			for (int index = 0; index < record.BarUpdatesByBip.Length; index++)
			{
				long count = Interlocked.Read(ref record.BarUpdatesByBip[index]);
				if (count > 0)
					counts[index] = count;
			}
			foreach (KeyValuePair<int, long> item in record.OverflowBarUpdatesByBip)
				if (item.Value > 0)
					counts[item.Key] = item.Value;

			if (counts.Count == 0)
				return string.Empty;

			List<int> keys = new List<int>(counts.Keys);
			keys.Sort();
			List<string> parts = new List<string>();
			foreach (int key in keys)
				parts.Add("BIP" + key.ToString(CultureInfo.InvariantCulture) + "=" + counts[key].ToString(CultureInfo.InvariantCulture));
			return string.Join(" ", parts.ToArray());
		}

		private static string JoinSeries(HashSet<string> series)
		{
			if (series == null || series.Count == 0)
				return string.Empty;
			List<string> values = new List<string>(series);
			values.Sort(StringComparer.OrdinalIgnoreCase);
			return string.Join("; ", values.ToArray());
		}

		private static string ResolveChartName(object owner)
		{
			object chartControl = GetMemberValue(owner, "ChartControl");
			if (chartControl == null)
				return "Unknown";

			object name = GetMemberValue(chartControl, "Name");
			if (name != null && !string.IsNullOrWhiteSpace(name.ToString()))
				return name.ToString();

			return "Chart " + chartControl.GetHashCode().ToString(CultureInfo.InvariantCulture);
		}

		private static string ResolveInstrument(object owner)
		{
			object instrument = GetMemberValue(owner, "Instrument");
			string name = GetStringMember(instrument, "FullName");
			if (!string.IsNullOrWhiteSpace(name))
				return name;

			object bars = GetMemberValue(owner, "Bars");
			object barsInstrument = GetMemberValue(bars, "Instrument");
			name = GetStringMember(barsInstrument, "FullName");
			if (!string.IsNullOrWhiteSpace(name))
				return name;

			return "Unknown";
		}

		private static string ResolvePrimarySeries(object owner)
		{
			object bars = GetMemberValue(owner, "Bars");
			string description = DescribeBars(bars);
			return string.IsNullOrWhiteSpace(description) ? "Unknown" : description;
		}

		private static string ResolveTradingHours(object owner)
		{
			object bars = GetMemberValue(owner, "Bars");
			object tradingHours = GetMemberValue(bars, "TradingHours");
			string name = GetStringMember(tradingHours, "Name");
			return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
		}

		private static string ResolveTickReplayState(object owner)
		{
			object bars = GetMemberValue(owner, "Bars");
			object barsPeriod = GetMemberValue(bars, "BarsPeriod");
			object chartBars = GetMemberValue(owner, "ChartBars");

			bool? enabled = TryGetBoolean(owner, "IsTickReplay")
				?? TryGetBoolean(owner, "TickReplay")
				?? TryGetBoolean(owner, "TickReplayEnabled")
				?? TryGetBoolean(bars, "IsTickReplay")
				?? TryGetBoolean(bars, "TickReplay")
				?? TryGetBoolean(bars, "TickReplayEnabled")
				?? TryGetBoolean(barsPeriod, "IsTickReplay")
				?? TryGetBoolean(chartBars, "IsTickReplay")
				?? TryGetBoolean(chartBars, "TickReplay");

			if (!enabled.HasValue)
				return "Unknown";
			return enabled.Value ? "Enabled" : "Disabled";
		}

		private static string DescribeBars(object bars)
		{
			if (bars == null)
				return string.Empty;

			object barsPeriod = GetMemberValue(bars, "BarsPeriod");
			if (barsPeriod == null)
				return bars.ToString();

			string type = Safe(GetMemberValue(barsPeriod, "BarsPeriodType"), string.Empty);
			string value = Safe(GetMemberValue(barsPeriod, "Value"), string.Empty);
			if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(value))
				return type + " " + value;

			return barsPeriod.ToString();
		}

		private static bool? TryGetBoolean(object target, string memberName)
		{
			object value = GetMemberValue(target, memberName);
			if (value is bool)
				return (bool)value;
			return null;
		}

		private static string GetStringMember(object target, string memberName)
		{
			object value = GetMemberValue(target, memberName);
			return value == null ? string.Empty : value.ToString();
		}

		private static object GetMemberValue(object target, string memberName)
		{
			if (target == null || string.IsNullOrEmpty(memberName))
				return null;

			try
			{
				Type type = target.GetType();
				const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				PropertyInfo property = type.GetProperty(memberName, flags);
				if (property != null && property.GetIndexParameters().Length == 0)
					return property.GetValue(target, null);

				FieldInfo field = type.GetField(memberName, flags);
				if (field != null)
					return field.GetValue(target);
			}
			catch { }

			return null;
		}

		private static string Safe(object value, string fallback)
		{
			if (value == null)
				return fallback;
			string text = value.ToString();
			return string.IsNullOrWhiteSpace(text) ? fallback : text;
		}
	}
}
