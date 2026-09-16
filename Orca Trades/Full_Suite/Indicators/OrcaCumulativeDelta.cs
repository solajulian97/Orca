#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;

using SharpDX;
using SharpDX.Direct2D1;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum CumulativeDeltaMode
	{
		BidAsk,
		TickDirection
	}

	public enum CumulativeDeltaDisplayMode
	{
		Cumulative,
		BarByBar
	}

	public enum BarDeltaHistogramStyle
	{
		Mirrored,
		SameFloor
	}

	public enum CumulativeDeltaResetPeriod
	{
		ETHDaily,
		RTHDaily,
		Weekly,
		Monthly,
		FullRange
	}

	public class OrcaCumulativeDelta : Indicator, IOrcaReplayParticipant
	{
		#region Private Fields
		private sealed class ReplayDeltaBaseline
		{
			public int SelectionBarIndex;
			public double RunningDelta;
			public int ResetKey;
			public double[] Open;
			public double[] High;
			public double[] Low;
			public double[] Close;
			public double[] Value;
			public double[] MaxValue;
			public double[] MinValue;
			public bool[] HasData;
		}

		private struct ReplayDeltaBar
		{
			public double Open;
			public double High;
			public double Low;
			public double Close;
			public double Value;
			public double MaxValue;
			public double MinValue;
			public bool HasData;
		}

		private struct BarDeltaRun
		{
			public int StartIndex;
			public int EndIndex;
			public int Count;
			public double Value;
			public double MaxValue;
			public double MinValue;
		}

		private sealed class ReplayDeltaSnapshot
		{
			public ReplayDeltaBaseline Baseline;
			public Dictionary<int, ReplayDeltaBar> Bars;
			public int MaxIndex;
		}

		private double	lastBid;
		private double	lastAsk;
		private double	prevLast;
		private double	runningDelta;
		private int		lastPrimaryBarProcessed;
		private int		lastResetKey;
		private int		lastDirection;  // +1 or -1, carries forward for unchanged ticks

		private List<double>	barDeltaOpen;
		private List<double>	barDeltaHigh;
		private List<double>	barDeltaLow;
		private List<double>	barDeltaClose;
		private List<double>	barDeltaValue;
		private List<double>	barDeltaMaxValue;
		private List<double>	barDeltaMinValue;
		private List<double>	barDeltaBundledValue;
		private List<bool>		barHasData;
		private readonly object deltaDataSync = new object();
		private DateTime lastRenderSkipUtc = DateTime.MinValue;
		private int providerRevision = -1;
		private int providerCurrentBar = -1;
		private bool providerDataActive;
		private DateTime lastProviderWarningUtc = DateTime.MinValue;
		private DateTime lastSharedBackfillAttemptUtc = DateTime.MinValue;
		private DateTime lastSharedBackfillSuccessLogUtc = DateTime.MinValue;
		private const int SharedProviderMaxRealtimeLagSeconds = 30;
        private readonly string diagnosticsInstanceId = Guid.NewGuid().ToString("N");
        private bool diagnosticsRegistered;

		// SharpDX brushes
		private SharpDX.Direct2D1.Brush	dxUpFillBrush;
		private SharpDX.Direct2D1.Brush	dxDownFillBrush;
		private SharpDX.Direct2D1.Brush	dxUpBorderBrush;
		private SharpDX.Direct2D1.Brush	dxDownBorderBrush;
		private SharpDX.Direct2D1.Brush	dxWickBrush;
		private SharpDX.Direct2D1.Brush	dxZeroBrush;
		private SharpDX.Direct2D1.Brush	dxPriceLineBrush;
		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private readonly OrcaReplayTradeClassifier replayTradeClassifier = new OrcaReplayTradeClassifier();
		private ReplayDeltaBaseline replayBaseline;
		private Dictionary<int, ReplayDeltaBar> replayBars;
		private volatile ReplayDeltaSnapshot replayDeltaSnapshot;
		private double replayRunningDelta;
		private int replayLastResetKey;
		private int replayLastPrimaryBar;
		private bool replayPaintPriceMarkersCaptured;
		private bool replayPaintPriceMarkers;
		private volatile bool sameSignBundleZoomActive;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "OrcaCumulativeDelta";
				Description					= "Cumulative delta OHLC candles on a separate panel.";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				DisplayInDataBox			= true;
				IsSuspendedWhileInactive	= false;
				BarsRequiredToPlot			= 0;

				// Visual parameters — OTM style
				ColorUp				= Brushes.DodgerBlue;
				ColorDown			= Brushes.Tomato;
				ColorUpBorder		= Brushes.DodgerBlue;
				ColorDownBorder		= Brushes.Tomato;
				BarOpacity			= 0.5;
				BorderOpacity		= 1.0;
				WickColor			= Brushes.White;
				ZeroLineColor		= Brushes.DimGray;
				ZeroLineWidth		= 1;
				BarWidthPercent		= 90;
				ShowPriceLine		= true;
				PriceLineWidth		= 1;
				IncludeZeroInAutoScale = false;
				DeltaMode			= CumulativeDeltaMode.BidAsk;
				DeltaDisplayMode	= CumulativeDeltaDisplayMode.Cumulative;
				BarHistogramStyle	= BarDeltaHistogramStyle.Mirrored;
				BundleSameSignBars	= false;
				BundleOnlyWhenZoomedOut = false;
				BundleAtBarSpacingPx = 6;
				ShowBarDeltaWicks	= true;
				BarDeltaGapPx		= 1;
				ResetPeriod			= CumulativeDeltaResetPeriod.ETHDaily;
				OrderFlowSourceMode	= OrcaOrderFlowSourceMode.Internal;

				// DeltaClose is Values[0] so NT's right-side live label tracks the current delta close.
				// DeltaHigh / DeltaLow are Values[1]/[2] to ensure the scale covers the full range.
				// Lines are never drawn because base.OnRender() is not called.
				AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "DeltaClose");
				AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "DeltaHigh");
				AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "DeltaLow");
			}
			else if (State == State.Configure)
			{
				// Delta depends on continuous tick/quote state, including on background tabs.
				// Enforce this after saved settings are applied as well as for new instances.
				IsSuspendedWhileInactive = false;
				if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.Internal)
					AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
                ReportDiagnosticsState();
				barDeltaOpen	= new List<double>(4096);
				barDeltaHigh	= new List<double>(4096);
				barDeltaLow		= new List<double>(4096);
				barDeltaClose	= new List<double>(4096);
				barDeltaValue	= new List<double>(4096);
				barDeltaMaxValue = new List<double>(4096);
				barDeltaMinValue = new List<double>(4096);
				barDeltaBundledValue = new List<double>(4096);
				barHasData		= new List<bool>(4096);

				lastBid			= double.NaN;
				lastAsk			= double.NaN;
				prevLast		= double.NaN;
				runningDelta	= 0;
				lastPrimaryBarProcessed = -1;
				lastResetKey	= int.MinValue;
				lastDirection	= 0;
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
			}
			else if (State == State.Historical || State == State.Transition || State == State.Realtime)
			{
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				ReportDiagnosticsState();
			}
			else if (State == State.Terminated)
			{
				if (ChartControl != null) OrcaReplayCore.UnregisterParticipant(ChartControl, this);
				RestoreLiveState();
				OrcaDiagnosticsCore.UnregisterInstance(diagnosticsInstanceId);
				DisposeDxResources();
			}
		}


        private void EnsureDiagnosticsRegistered()
        {
            if (diagnosticsRegistered)
                return;

            OrcaDiagnosticsCore.RegisterInstance(diagnosticsInstanceId, "OrcaCumulativeDelta", this);
            ReportDiagnosticsSourceDeclaration("Unknown");
            OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 0, "PrimaryChartSeries", "Chart", "Cumulative delta primary bars");
            if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.Internal)
                OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 1, "Tick 1 Last", "Hidden", "Internal cumulative-delta tick series");
            diagnosticsRegistered = true;
        }

        private void ReportDiagnosticsState()
        {
            EnsureDiagnosticsRegistered();
            OrcaDiagnosticsCore.ReportState(diagnosticsInstanceId, State.ToString());
            ReportDiagnosticsSourceDeclaration(null);
        }

        private void ReportDiagnosticsSourceDeclaration(string sourceHealth)
        {
            string sourceMode;
            string cacheProvider;
            if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedProvider)
            {
                sourceMode = "SharedOrcaProfileDataProvider";
                cacheProvider = "SharedOrcaProfileDataProvider";
            }
            else if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedHistoricalInternalRealtime)
            {
                sourceMode = "SharedHistoricalInternalRealtime";
                cacheProvider = "SharedOrcaProfileDataProvider";
            }
            else
            {
                sourceMode = "HiddenSecondaryTickSeries";
                cacheProvider = "LocalBarSeries";
            }

            OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsInstanceId, sourceMode, sourceHealth, cacheProvider);
        }

        private DateTime GetDiagnosticsEventTime()
        {
            try
            {
                if (Times != null && CurrentBars != null && BarsInProgress >= 0 && BarsInProgress < Times.Length && BarsInProgress < CurrentBars.Length && CurrentBars[BarsInProgress] >= 0)
                    return Times[BarsInProgress][0];
            }
            catch { }

            try
            {
                if (CurrentBar >= 0)
                    return Time[0];
            }
            catch { }

            return DateTime.MinValue;
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

			if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedProvider)
				return;

			// Track bid/ask for real-time classification
			if (e.MarketDataType == MarketDataType.Bid)
				lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask)
				lastAsk = e.Price;
			else if (e.MarketDataType == MarketDataType.Last)
			{
				// During tick replay, e.Ask/e.Bid on Last events carry bid/ask
				if (e.Ask > 0 && !double.IsNaN(e.Ask)) lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid)) lastBid = e.Bid;

				if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedHistoricalInternalRealtime && State == State.Realtime)
				{
					if (!providerDataActive && ShouldAttemptRealtimeSharedBackfill())
						TryRefreshFromSharedProvider(false, true, false);

					long volume = e.Volume;
					if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
						volume = (long)Core.Globals.ToCryptocurrencyVolume(volume);

					long signed = ClassifySignedVolume(e.Price, volume);
					if (signed != 0)
						ApplySignedDeltaToBar(e.Time, signed);
				}
			}
            }
            finally
            {
                if (diagnosticsWorkStart > 0)
                    OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.MarketData, -1, diagnosticsWorkStart);
            }
		}

		private void EnsureBarLists(int idx)
		{
			while (barDeltaOpen.Count <= idx)
			{
				barDeltaOpen.Add(0);
				barDeltaHigh.Add(0);
				barDeltaLow.Add(0);
				barDeltaClose.Add(0);
				barDeltaValue.Add(0);
				barDeltaMaxValue.Add(0);
				barDeltaMinValue.Add(0);
				barDeltaBundledValue.Add(0);
				barHasData.Add(false);
			}
		}

		protected override void OnBarUpdate()
		{
            long diagnosticsWorkStart = 0;
            int diagnosticsBarsInProgress = BarsInProgress;
            if (OrcaDiagnosticsCore.IsEnabled)
            {
                EnsureDiagnosticsRegistered();
                long diagnosticsSequence = OrcaDiagnosticsCore.ReportBarUpdate(diagnosticsInstanceId, BarsInProgress, GetDiagnosticsEventTime());
                diagnosticsWorkStart = OrcaDiagnosticsCore.BeginWorkSample(diagnosticsSequence);
            }
            try
            {

			if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedProvider)
			{
				if (BarsInProgress == 0 && CurrentBar >= 0)
				{
					EnsureBarLists(CurrentBar);
					if (ShouldRefreshSharedHistorical())
						TryRefreshFromSharedProvider(false, false, true);
					UpdateCurrentValuesFromArrays();
				}
				return;
			}

			if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedHistoricalInternalRealtime)
			{
				if (BarsInProgress == 0 && CurrentBar >= 0)
				{
					EnsureBarLists(CurrentBar);
					if ((State == State.Realtime && !providerDataActive && ShouldAttemptRealtimeSharedBackfill()) || ShouldRefreshSharedHistorical())
						TryRefreshFromSharedProvider(false, true, false);
					UpdateCurrentValuesFromArrays();
				}
				return;
			}

			// ============================================
			// BarsInProgress == 1 : each tick
			// ============================================
			if (BarsInProgress == 1)
			{
				double price = Closes[1][0];
				long   vol   = (long)Volumes[1][0];
				if (vol <= 0) return;

				if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
					vol = (long)Core.Globals.ToCryptocurrencyVolume(vol);

				DateTime tradeTime = Times[1][0];
				int primaryIdx = BarsArray[0].GetBar(tradeTime);
				if (primaryIdx < 0) return;

				EnsureBarLists(primaryIdx);

				// Track bar transitions
				if (primaryIdx != lastPrimaryBarProcessed)
				{
					ApplyResetIfNeeded(primaryIdx);
					lastPrimaryBarProcessed = primaryIdx;
				}

				long signed = ClassifySignedVolume(price, vol);
				if (signed == 0) return;

				ApplySignedDeltaToBar(tradeTime, signed);
				return;
			}

			// ============================================
			// BarsInProgress == 0 : primary bar
			// ============================================
			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			EnsureBarLists(CurrentBar);

			UpdateCurrentValuesFromArrays();
            }
            finally
            {
                if (diagnosticsWorkStart > 0)
                    OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.BarUpdate, diagnosticsBarsInProgress, diagnosticsWorkStart);
            }
		}

		private bool ShouldRefreshSharedHistorical()
		{
			if (State == State.Realtime)
				return false;
			if (Bars == null || CurrentBar < 0)
				return false;
			return CurrentBar >= Math.Max(0, Bars.Count - 2);
		}

		private bool ShouldAttemptRealtimeSharedBackfill()
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastSharedBackfillAttemptUtc).TotalSeconds < 3)
				return false;

			lastSharedBackfillAttemptUtc = now;
			return true;
		}

		private void UpdateCurrentValuesFromArrays()
		{
			// Values[0]=DeltaClose drives the live right-axis label; [1]=High, [2]=Low drive scale range.
			if (CurrentBar >= 0 && CurrentBar < barDeltaClose.Count && barHasData[CurrentBar])
			{
				if (DeltaDisplayMode == CumulativeDeltaDisplayMode.BarByBar)
				{
					bool bundleValues = BundleSameSignBars && (!BundleOnlyWhenZoomedOut || sameSignBundleZoomActive);
					double value = bundleValues ? barDeltaBundledValue[CurrentBar] : barDeltaValue[CurrentBar];
					double renderedValue = GetRenderedBarDeltaValue(value);
					if (bundleValues)
					{
						Values[0][0] = renderedValue;
						Values[1][0] = Math.Max(0, renderedValue);
						Values[2][0] = BarHistogramStyle == BarDeltaHistogramStyle.SameFloor ? 0 : Math.Min(0, renderedValue);
						return;
					}
					if (ShowBarDeltaWicks)
					{
						Values[0][0] = renderedValue;
						Values[1][0] = Math.Max(renderedValue, barDeltaMaxValue[CurrentBar]);
						Values[2][0] = Math.Min(0, barDeltaMinValue[CurrentBar]);
					}
					else
					{
						if (BarHistogramStyle == BarDeltaHistogramStyle.SameFloor)
						{
							double absValue = Math.Abs(value);
							Values[0][0] = absValue;
							Values[1][0] = absValue;
							Values[2][0] = 0;
						}
						else
						{
							Values[0][0] = value;
							Values[1][0] = Math.Max(0, value);
							Values[2][0] = Math.Min(0, value);
						}
					}
				}
				else
				{
					Values[0][0] = barDeltaClose[CurrentBar];
					Values[1][0] = barDeltaHigh[CurrentBar];
					Values[2][0] = barDeltaLow[CurrentBar];
				}
			}
			else
			{
				Values[0][0] = double.NaN;
				Values[1][0] = double.NaN;
				Values[2][0] = double.NaN;
			}
		}

		private long ClassifySignedVolume(double price, long volume)
		{
			if (volume <= 0)
				return 0;

			long signed = 0;
			if (DeltaMode == CumulativeDeltaMode.BidAsk
				&& !double.IsNaN(lastAsk) && !double.IsNaN(lastBid)
				&& lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
			{
				if (price >= lastAsk)
					signed = +volume;
				else if (price <= lastBid)
					signed = -volume;
				else if (!double.IsNaN(prevLast))
				{
					if (price > prevLast) signed = +volume;
					else if (price < prevLast) signed = -volume;
					else signed = lastDirection * volume;
				}
			}
			else if (!double.IsNaN(prevLast))
			{
				if (price > prevLast) signed = +volume;
				else if (price < prevLast) signed = -volume;
				else signed = lastDirection * volume;
			}

			prevLast = price;
			if (signed > 0) lastDirection = 1;
			else if (signed < 0) lastDirection = -1;
			return signed;
		}

		private void ApplySignedDeltaToBar(DateTime tradeTime, long signed)
		{
			int primaryIdx = BarsArray[0].GetBar(tradeTime);
			if (primaryIdx < 0)
				return;

			EnsureBarLists(primaryIdx);
			if (primaryIdx != lastPrimaryBarProcessed)
			{
				ApplyResetIfNeeded(primaryIdx);
				lastPrimaryBarProcessed = primaryIdx;
			}

			runningDelta += signed;
			barDeltaValue[primaryIdx] += signed;
			double currentBarDelta = barDeltaValue[primaryIdx];

			if (!barHasData[primaryIdx])
			{
				barDeltaOpen[primaryIdx]  = runningDelta;
				barDeltaHigh[primaryIdx]  = runningDelta;
				barDeltaLow[primaryIdx]   = runningDelta;
				barDeltaClose[primaryIdx] = runningDelta;
				barDeltaMaxValue[primaryIdx] = Math.Max(0, currentBarDelta);
				barDeltaMinValue[primaryIdx] = Math.Min(0, currentBarDelta);
				barHasData[primaryIdx]    = true;
			}
			else
			{
				barDeltaClose[primaryIdx] = runningDelta;
				if (runningDelta > barDeltaHigh[primaryIdx])
					barDeltaHigh[primaryIdx] = runningDelta;
				if (runningDelta < barDeltaLow[primaryIdx])
					barDeltaLow[primaryIdx] = runningDelta;
				if (currentBarDelta > barDeltaMaxValue[primaryIdx])
					barDeltaMaxValue[primaryIdx] = currentBarDelta;
				if (currentBarDelta < barDeltaMinValue[primaryIdx])
					barDeltaMinValue[primaryIdx] = currentBarDelta;
			}
			UpdateBundledValueAt(primaryIdx);
            OrcaDiagnosticsCore.ReportModelUpdate(diagnosticsInstanceId, tradeTime, null);
		}

		private bool TryRefreshFromSharedProvider(bool force, bool allowRealtimeLag, bool clearOnUnavailable)
		{
			if (Bars == null || CurrentBar < 0)
				return false;

			string key = OrcaProfileDataCache.BuildInstrumentKey(Bars);
			int revision;
			DateTime updatedUtc;
			string sourceName;
			if (!OrcaProfileDataCache.TryGetOrderFlowStatus(key, out revision, out updatedUtc, out sourceName))
			{
				MarkSharedProviderUnavailable("no OrcaProfileDataProvider order-flow source is registered for " + key + ". Registered order-flow sources: " + OrcaProfileDataCache.DescribeOrderFlowSources(), clearOnUnavailable);
				return false;
			}

			if (!force && providerDataActive && revision == providerRevision && CurrentBar == providerCurrentBar)
				return true;

			OrcaOrderFlowDataSnapshot snapshot;
			if (!OrcaProfileDataCache.TrySnapshotOrderFlow(key, GetProviderFromTime(), GetProviderToTime(), out snapshot))
			{
				MarkSharedProviderUnavailable("OrcaProfileDataProvider has no order-flow buckets for the loaded chart range yet", clearOnUnavailable);
				return false;
			}

			string staleReason;
			if (!IsSharedProviderSnapshotUsable(snapshot, allowRealtimeLag, out staleReason))
			{
				MarkSharedProviderUnavailable(staleReason, clearOnUnavailable);
				return false;
			}

			RebuildFromOrderFlowSnapshot(snapshot);
            OrcaDiagnosticsCore.ReportCacheStatus(diagnosticsInstanceId, "SharedOrcaProfileDataProvider", "loaded " + snapshot.SourceName + " revision=" + snapshot.Revision);
            OrcaDiagnosticsCore.ReportModelUpdate(diagnosticsInstanceId, GetDiagnosticsEventTime(), null);
			providerRevision = snapshot.Revision;
			providerCurrentBar = CurrentBar;
			providerDataActive = true;
			LogSharedBackfillSuccess(snapshot);
			return true;
		}

		private void LogSharedBackfillSuccess(OrcaOrderFlowDataSnapshot snapshot)
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastSharedBackfillSuccessLogUtc).TotalSeconds < 30)
				return;

			lastSharedBackfillSuccessLogUtc = now;
			int bucketCount = snapshot != null && snapshot.Buckets != null ? snapshot.Buckets.Count : 0;
			string firstTime = bucketCount > 0 ? snapshot.Buckets[0].Time.ToString("HH:mm:ss") : "n/a";
			string lastTime = bucketCount > 0 ? snapshot.Buckets[bucketCount - 1].Time.ToString("HH:mm:ss") : "n/a";
			long totalVolume = snapshot != null ? snapshot.Volume : 0;
			string bidAskPct = FormatPercent(snapshot != null ? snapshot.BidAskClassifiedVolume : 0, totalVolume);
			string fallbackPct = FormatPercent(snapshot != null ? snapshot.FallbackClassifiedVolume : 0, totalVolume);
			string unclassifiedPct = FormatPercent(snapshot != null ? snapshot.UnclassifiedVolume : 0, totalVolume);
			Print("OrcaCumulativeDelta: loaded shared historical backfill from " + snapshot.SourceName + " buckets=" + bucketCount + " range=" + firstTime + "-" + lastTime + " revision=" + snapshot.Revision + " bidAsk=" + bidAskPct + " fallback=" + fallbackPct + " unclassified=" + unclassifiedPct);
		}

		private string FormatPercent(long value, long total)
		{
			if (total <= 0)
				return "0%";

			return (100.0 * value / total).ToString("0.0") + "%";
		}

		private void MarkSharedProviderUnavailable(string reason, bool clearExistingData)
		{
			if (clearExistingData && CurrentBar >= 0)
			{
				EnsureBarLists(CurrentBar);
				ClearDeltaLists(CurrentBar);
			}

			providerDataActive = false;
			providerRevision = -1;
			providerCurrentBar = -1;
            ReportDiagnosticsSourceDeclaration("Unavailable");
            OrcaDiagnosticsCore.ReportCacheStatus(diagnosticsInstanceId, "SharedOrcaProfileDataProvider", reason);
			LogProviderWarning(reason);
		}

		private bool IsSharedProviderSnapshotUsable(OrcaOrderFlowDataSnapshot snapshot, bool allowRealtimeLag, out string reason)
		{
			reason = null;
			if (snapshot == null || snapshot.Buckets == null || snapshot.Buckets.Count == 0)
			{
				reason = "OrcaProfileDataProvider has no order-flow buckets for the loaded chart range yet";
				return false;
			}

			DateTime lastBucketTime = snapshot.Buckets[snapshot.Buckets.Count - 1].Time;
			if (!allowRealtimeLag && State == State.Realtime)
			{
				double secondsBehind = (DateTime.Now - lastBucketTime).TotalSeconds;
				if (secondsBehind > SharedProviderMaxRealtimeLagSeconds)
				{
					reason = "shared provider data is behind real time by about " + Math.Round(secondsBehind) + " seconds. Last provider bucket: " + lastBucketTime.ToString("HH:mm:ss");
					return false;
				}
			}

			return true;
		}

		private void LogProviderWarning(string message)
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastProviderWarningUtc).TotalSeconds < 30)
				return;

			lastProviderWarningUtc = now;
			Print("OrcaCumulativeDelta: " + message);
		}

		private DateTime GetProviderFromTime()
		{
			if (Bars == null || Bars.Count <= 0)
				return DateTime.MinValue;

			return Bars.GetTime(0).AddDays(-1);
		}

		private DateTime GetProviderToTime()
		{
			DateTime toTime = DateTime.Now;
			if (Bars != null && CurrentBar >= 0 && CurrentBar < Bars.Count)
			{
				DateTime currentBarTime = Bars.GetTime(CurrentBar);
				if (currentBarTime > toTime)
					toTime = currentBarTime;
			}

			return toTime.AddSeconds(2);
		}

		private void RebuildFromOrderFlowSnapshot(OrcaOrderFlowDataSnapshot snapshot)
		{
			if (snapshot == null || snapshot.Buckets == null || Bars == null || CurrentBar < 0)
				return;

			EnsureBarLists(CurrentBar);
			ClearDeltaLists(CurrentBar);

			double providerRunningDelta = 0;
			int activeResetKey = int.MinValue;

			for (int index = 0; index < snapshot.Buckets.Count; index++)
			{
				OrcaOrderFlowBucket bucket = snapshot.Buckets[index];
				if (bucket == null)
					continue;

				int primaryIdx = Bars.GetBar(bucket.Time);
				if (primaryIdx < 0 || primaryIdx > CurrentBar)
					continue;

				EnsureBarLists(primaryIdx);
				int resetKey = GetResetKey(bucket.Time);
				if (activeResetKey == int.MinValue)
					activeResetKey = resetKey;
				else if (resetKey != activeResetKey)
				{
					providerRunningDelta = 0;
					activeResetKey = resetKey;
				}

				if (!barHasData[primaryIdx])
				{
					barDeltaOpen[primaryIdx] = providerRunningDelta;
					barDeltaHigh[primaryIdx] = providerRunningDelta;
					barDeltaLow[primaryIdx] = providerRunningDelta;
					barDeltaClose[primaryIdx] = providerRunningDelta;
					barDeltaValue[primaryIdx] = 0;
					barDeltaMaxValue[primaryIdx] = 0;
					barDeltaMinValue[primaryIdx] = 0;
					barHasData[primaryIdx] = true;
				}

				double barBefore = barDeltaValue[primaryIdx];
				double runningBefore = providerRunningDelta;
				double cumulativeMax = runningBefore + bucket.MaxDelta;
				double cumulativeMin = runningBefore + bucket.MinDelta;

				if (cumulativeMax > barDeltaHigh[primaryIdx])
					barDeltaHigh[primaryIdx] = cumulativeMax;
				if (cumulativeMin < barDeltaLow[primaryIdx])
					barDeltaLow[primaryIdx] = cumulativeMin;

				barDeltaValue[primaryIdx] += bucket.Delta;
				double barMax = barBefore + bucket.MaxDelta;
				double barMin = barBefore + bucket.MinDelta;
				if (barMax > barDeltaMaxValue[primaryIdx])
					barDeltaMaxValue[primaryIdx] = barMax;
				if (barMin < barDeltaMinValue[primaryIdx])
					barDeltaMinValue[primaryIdx] = barMin;

				providerRunningDelta += bucket.Delta;
				barDeltaClose[primaryIdx] = providerRunningDelta;
				if (providerRunningDelta > barDeltaHigh[primaryIdx])
					barDeltaHigh[primaryIdx] = providerRunningDelta;
				if (providerRunningDelta < barDeltaLow[primaryIdx])
					barDeltaLow[primaryIdx] = providerRunningDelta;
			}

			CarryForwardMissingCumulativeBars(CurrentBar);
			RebuildBundledValues(CurrentBar);
			runningDelta = providerRunningDelta;
			lastResetKey = activeResetKey;
			lastPrimaryBarProcessed = CurrentBar;
		}

		private void CarryForwardMissingCumulativeBars(int lastIndex)
		{
			if (BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null || lastIndex < 0)
				return;

			int count = Math.Min(lastIndex, barDeltaClose.Count - 1);
			bool hasCarry = false;
			double carry = 0;
			int carryResetKey = int.MinValue;

			for (int index = 0; index <= count; index++)
			{
				int resetKey = GetResetKey(BarsArray[0].GetTime(index));
				if (carryResetKey == int.MinValue)
					carryResetKey = resetKey;
				else if (resetKey != carryResetKey)
				{
					hasCarry = false;
					carry = 0;
					carryResetKey = resetKey;
				}

				if (barHasData[index])
				{
					carry = barDeltaClose[index];
					hasCarry = true;
					continue;
				}

				if (!hasCarry)
					continue;

				barDeltaOpen[index] = carry;
				barDeltaHigh[index] = carry;
				barDeltaLow[index] = carry;
				barDeltaClose[index] = carry;
				barDeltaValue[index] = 0;
				barDeltaMaxValue[index] = 0;
				barDeltaMinValue[index] = 0;
				barHasData[index] = true;
			}
		}

		private void ClearDeltaLists(int lastIndex)
		{
			int count = Math.Min(lastIndex, barDeltaClose.Count - 1);
			for (int index = 0; index <= count; index++)
			{
				barDeltaOpen[index] = 0;
				barDeltaHigh[index] = 0;
				barDeltaLow[index] = 0;
				barDeltaClose[index] = 0;
				barDeltaValue[index] = 0;
				barDeltaMaxValue[index] = 0;
				barDeltaMinValue[index] = 0;
				barDeltaBundledValue[index] = 0;
				barHasData[index] = false;
			}
		}

		private void RebuildBundledValues(int lastIndex)
		{
			int count = Math.Min(lastIndex, barDeltaValue.Count - 1);
			for (int index = 0; index <= count; index++)
				UpdateBundledValueAt(index);
		}

		private void UpdateBundledValueAt(int index)
		{
			if (index < 0 || index >= barDeltaValue.Count || index >= barDeltaBundledValue.Count || !barHasData[index])
				return;

			double rawValue = barDeltaValue[index];
			int sign = GetDeltaSign(rawValue);
			double bundledValue = rawValue;
			if (sign != 0 && index > 0 && barHasData[index - 1]
				&& GetDeltaSign(barDeltaValue[index - 1]) == sign
				&& IsSameResetPeriod(index - 1, index))
				bundledValue += barDeltaBundledValue[index - 1];

			barDeltaBundledValue[index] = bundledValue;
		}

		private string ReplayParticipantKey { get { return "OrcaCumulativeDelta:" + diagnosticsInstanceId; } }

		string IOrcaReplayParticipant.ReplayParticipantId { get { return ReplayParticipantKey; } }

		OrcaReplayCapabilities IOrcaReplayParticipant.ReplayCapabilities
		{
			get
			{
				return new OrcaReplayCapabilities(true, true, true, false,
					OrcaReplayChartStyleSupport.AllV1);
			}
		}

		public OrcaReplayCheckpoint CaptureReplayCheckpoint(OrcaReplayContext context)
		{
			int selectionBar = context == null ? -1 : context.PrimaryBarIndex;
			int available = Math.Min(
				Math.Min(Math.Min(barDeltaOpen.Count, barDeltaHigh.Count), Math.Min(barDeltaLow.Count, barDeltaClose.Count)),
				Math.Min(Math.Min(barDeltaValue.Count, barHasData.Count), Math.Min(barDeltaMaxValue.Count, barDeltaMinValue.Count)));
			selectionBar = Math.Min(selectionBar, available - 1);
			if (selectionBar < 0)
				throw new InvalidOperationException("Cumulative Delta has no completed replay baseline at the selected bar.");

			int count = selectionBar + 1;
			var baseline = new ReplayDeltaBaseline
			{
				SelectionBarIndex = selectionBar,
				Open = barDeltaOpen.GetRange(0, count).ToArray(),
				High = barDeltaHigh.GetRange(0, count).ToArray(),
				Low = barDeltaLow.GetRange(0, count).ToArray(),
				Close = barDeltaClose.GetRange(0, count).ToArray(),
				Value = barDeltaValue.GetRange(0, count).ToArray(),
				MaxValue = barDeltaMaxValue.GetRange(0, count).ToArray(),
				MinValue = barDeltaMinValue.GetRange(0, count).ToArray(),
				HasData = barHasData.GetRange(0, count).ToArray()
			};

			int lastData = selectionBar;
			while (lastData >= 0 && !baseline.HasData[lastData]) lastData--;
			baseline.RunningDelta = lastData >= 0 ? baseline.Close[lastData] : 0;
			DateTime baselineTime = context.CurrentTime;
			try { baselineTime = BarsArray[0].GetTime(selectionBar); } catch { }
			baseline.ResetKey = GetResetKey(baselineTime);
			return new OrcaReplayCheckpoint(baselineTime, 0, selectionBar, ReplayParticipantKey, baseline);
		}

		public void PrepareReplay(OrcaReplayContext context, OrcaReplayCheckpoint checkpoint)
		{
			replayBaseline = checkpoint == null ? null : checkpoint.State as ReplayDeltaBaseline;
			if (replayBaseline == null)
				throw new InvalidOperationException("Cumulative Delta replay baseline is unavailable.");

			replayBars = new Dictionary<int, ReplayDeltaBar>();
			replayRunningDelta = replayBaseline.RunningDelta;
			replayLastResetKey = replayBaseline.ResetKey;
			replayLastPrimaryBar = replayBaseline.SelectionBarIndex;
			replayTradeClassifier.Reset();
			replayDeltaSnapshot = null;
			if (!replayPaintPriceMarkersCaptured)
			{
				replayPaintPriceMarkers = PaintPriceMarkers;
				replayPaintPriceMarkersCaptured = true;
			}
			PaintPriceMarkers = false;
		}

		public void ApplyReplayEvent(OrcaReplayContext context, OrcaReplayTradeEvent tradeEvent)
		{
			if (tradeEvent == null || tradeEvent.Volume <= 0 || replayBaseline == null || replayBars == null)
				return;

			long signed = replayTradeClassifier.Classify(tradeEvent, DeltaMode == CumulativeDeltaMode.BidAsk);
			// Prefix events rebuild only the tick-rule classifier. The immutable completed-bar
			// baseline already contains their delta and prevents double counting.
			if (tradeEvent.PrimaryBarIndex <= replayBaseline.SelectionBarIndex || signed == 0)
				return;

			if (tradeEvent.PrimaryBarIndex != replayLastPrimaryBar)
			{
				int resetKey = GetResetKey(tradeEvent.Time);
				if (resetKey != replayLastResetKey)
				{
					replayRunningDelta = 0;
					replayLastResetKey = resetKey;
				}
				replayLastPrimaryBar = tradeEvent.PrimaryBarIndex;
			}

			ReplayDeltaBar bar;
			if (!replayBars.TryGetValue(tradeEvent.PrimaryBarIndex, out bar))
				bar = new ReplayDeltaBar();
			replayRunningDelta += signed;
			bar.Value += signed;
			if (!bar.HasData)
			{
				bar.Open = replayRunningDelta;
				bar.High = replayRunningDelta;
				bar.Low = replayRunningDelta;
				bar.Close = replayRunningDelta;
				bar.MaxValue = Math.Max(0, bar.Value);
				bar.MinValue = Math.Min(0, bar.Value);
				bar.HasData = true;
			}
			else
			{
				bar.Close = replayRunningDelta;
				bar.High = Math.Max(bar.High, replayRunningDelta);
				bar.Low = Math.Min(bar.Low, replayRunningDelta);
				bar.MaxValue = Math.Max(bar.MaxValue, bar.Value);
				bar.MinValue = Math.Min(bar.MinValue, bar.Value);
			}
			replayBars[tradeEvent.PrimaryBarIndex] = bar;
		}

		public void ApplyReplayBar(OrcaReplayContext context, int primaryBarIndex)
		{
			replayLastPrimaryBar = Math.Max(replayLastPrimaryBar, primaryBarIndex);
		}

		public void PublishReplaySnapshot(OrcaReplayContext context)
		{
			if (replayBaseline == null || replayBars == null) return;
			replayDeltaSnapshot = new ReplayDeltaSnapshot
			{
				Baseline = replayBaseline,
				Bars = new Dictionary<int, ReplayDeltaBar>(replayBars),
				MaxIndex = Math.Max(replayBaseline.SelectionBarIndex, replayLastPrimaryBar)
			};
		}

		public void RestoreLiveState()
		{
			replayDeltaSnapshot = null;
			replayBaseline = null;
			replayBars = null;
			replayTradeClassifier.Reset();
			if (replayPaintPriceMarkersCaptured)
			{
				PaintPriceMarkers = replayPaintPriceMarkers;
				replayPaintPriceMarkersCaptured = false;
			}
		}

		private bool TryGetRenderBar(ReplayDeltaSnapshot snapshot, int barIndex, out ReplayDeltaBar bar)
		{
			bar = new ReplayDeltaBar();
			if (barIndex < 0) return false;
			if (snapshot != null)
			{
				if (snapshot.Bars != null && snapshot.Bars.TryGetValue(barIndex, out bar))
					return bar.HasData;
				ReplayDeltaBaseline baseline = snapshot.Baseline;
				if (baseline == null || barIndex >= baseline.HasData.Length || !baseline.HasData[barIndex])
					return false;
				bar.Open = baseline.Open[barIndex];
				bar.High = baseline.High[barIndex];
				bar.Low = baseline.Low[barIndex];
				bar.Close = baseline.Close[barIndex];
				bar.Value = baseline.Value[barIndex];
				bar.MaxValue = baseline.MaxValue[barIndex];
				bar.MinValue = baseline.MinValue[barIndex];
				bar.HasData = true;
				return true;
			}

			if (barHasData == null || barIndex >= barHasData.Count || !barHasData[barIndex]) return false;
			bar.Open = barDeltaOpen[barIndex];
			bar.High = barDeltaHigh[barIndex];
			bar.Low = barDeltaLow[barIndex];
			bar.Close = barDeltaClose[barIndex];
			bar.Value = barDeltaValue[barIndex];
			bar.MaxValue = barDeltaMaxValue[barIndex];
			bar.MinValue = barDeltaMinValue[barIndex];
			bar.HasData = true;
			return true;
		}

		#region OnRender — OTM-style OHLC delta candles
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
            long diagnosticsRenderStart = System.Diagnostics.Stopwatch.GetTimestamp();
			try
			{
				RenderCumulativeDelta(chartControl, chartScale);
			}
			catch (Exception ex)
			{
				DisposeDxResources();
				LogRenderSkip(ex);
			}
            finally
            {
                OrcaDiagnosticsCore.ReportRenderSample(diagnosticsInstanceId, diagnosticsRenderStart);
            }
        }

		private void RenderCumulativeDelta(ChartControl chartControl, ChartScale chartScale)
		{
			if (chartControl == null || chartScale == null || Bars == null || RenderTarget == null)
				return;
			ReplayDeltaSnapshot renderSnapshot = replayDeltaSnapshot;
			if (renderSnapshot == null) TryEnsureSharedBackfillFromRender();
			if (ChartBars == null || barDeltaOpen == null || barDeltaHigh == null || barDeltaLow == null || barDeltaClose == null || barDeltaValue == null || barDeltaMaxValue == null || barDeltaMinValue == null || barHasData == null)
				return;

			int fromIdx = ChartBars.FromIndex;
			int toIdx   = ChartBars.ToIndex;
			int dataMax = renderSnapshot != null ? renderSnapshot.MaxIndex : Math.Min(
				Math.Min(Math.Min(barDeltaOpen.Count, barDeltaHigh.Count), Math.Min(barDeltaLow.Count, barDeltaClose.Count)),
				Math.Min(Math.Min(barDeltaValue.Count, barHasData.Count), Math.Min(barDeltaMaxValue.Count, barDeltaMinValue.Count))) - 1;
			int chartMax = Math.Min(ChartBars.Count - 1, Bars.Count - 1);
			int maxIdx = Math.Min(dataMax, chartMax);
			if (maxIdx < 0)
				return;
			fromIdx = Math.Max(0, fromIdx);
			toIdx = Math.Min(toIdx, maxIdx);
			if (fromIdx > toIdx)
				return;
			bool bundleBarsForCurrentRender = ShouldBundleSameSignBars(chartControl);

			EnsureDxResources();
			if (dxUpFillBrush == null)
				return;

			AntialiasMode oldMode = RenderTarget.AntialiasMode;
			try
			{
				RenderTarget.AntialiasMode = AntialiasMode.Aliased;

			float panelX = ChartPanel.X;
			float panelW = ChartPanel.W;
			float panelY = ChartPanel.Y;
			float panelH = ChartPanel.H;

			// Zero line
			float zeroY = chartScale.GetYByValue(0);
			if (zeroY >= panelY && zeroY <= panelY + panelH)
			{
				RenderTarget.DrawLine(
					new Vector2(panelX, zeroY),
					new Vector2(panelX + panelW, zeroY),
					dxZeroBrush, ZeroLineWidth);
			}

			// Delta candles
			if (DeltaDisplayMode == CumulativeDeltaDisplayMode.BarByBar && bundleBarsForCurrentRender)
			{
				int runIndex = FindBarDeltaRunStart(renderSnapshot, fromIdx);
				while (runIndex <= toIdx && runIndex <= maxIdx)
				{
					BarDeltaRun run;
					if (!TryBuildBarDeltaRun(renderSnapshot, runIndex, maxIdx, out run))
					{
						runIndex++;
						continue;
					}

					if (run.EndIndex >= fromIdx)
						DrawBarDeltaRunHistogram(chartControl, chartScale, run, fromIdx, toIdx);
					runIndex = run.EndIndex + 1;
				}
			}
			else
			for (int barIdx = fromIdx; barIdx <= toIdx; barIdx++)
			{
				ReplayDeltaBar renderBar;
				if (barIdx < 0 || barIdx > maxIdx || !TryGetRenderBar(renderSnapshot, barIdx, out renderBar))
					continue;

				if (DeltaDisplayMode == CumulativeDeltaDisplayMode.BarByBar)
				{
					DrawBarDeltaHistogram(chartControl, chartScale, barIdx, fromIdx, toIdx, renderBar);
					continue;
				}

				double dO = renderBar.Open;
				double dH = renderBar.High;
				double dL = renderBar.Low;
				double dC = renderBar.Close;

				bool isUp = dC >= dO;

				float yOpen  = chartScale.GetYByValue(dO);
				float yHigh  = chartScale.GetYByValue(dH);
				float yLow   = chartScale.GetYByValue(dL);
				float yClose = chartScale.GetYByValue(dC);

			// Calculate bar pixel spacing from adjacent bars (independent of chart bar width)
				float barX = chartControl.GetXByBarIndex(ChartBars, barIdx);
				float barSpacing;
				if (barIdx < toIdx)
					barSpacing = chartControl.GetXByBarIndex(ChartBars, barIdx + 1) - barX;
				else if (barIdx > fromIdx)
					barSpacing = barX - chartControl.GetXByBarIndex(ChartBars, barIdx - 1);
				else
					barSpacing = (float)chartControl.BarWidth;

				float halfW = (float)(barSpacing * BarWidthPercent / 100.0 / 2.0);
				if (halfW < 1f) halfW = 1f;

				var fillBrush   = isUp ? dxUpFillBrush   : dxDownFillBrush;
				var borderBrush = isUp ? dxUpBorderBrush  : dxDownBorderBrush;

				// Body rect
				float bTop = Math.Min(yOpen, yClose);
				float bBot = Math.Max(yOpen, yClose);
				float bH   = bBot - bTop;
				if (bH < 1f) bH = 1f;

				// 1) Wick — draw ONLY above and below body, color matches candle direction
				if (yHigh < bTop)
					RenderTarget.DrawLine(new Vector2(barX, yHigh), new Vector2(barX, bTop), borderBrush, 1f);
				if (yLow > bBot)
					RenderTarget.DrawLine(new Vector2(barX, bBot), new Vector2(barX, yLow), borderBrush, 1f);

				// 2) Body fill (semi-transparent)
				var bodyRect = new RectangleF(barX - halfW, bTop, halfW * 2, bH);
				RenderTarget.FillRectangle(bodyRect, fillBrush);

				// 3) Body border
				RenderTarget.DrawRectangle(bodyRect, borderBrush, 1f);
			}

			// Price line — walk backward from toIdx to find the last bar with real data
			// (toIdx may point to an empty future slot when there's blank space on the right)
			if (ShowPriceLine)
			{
				int lastData = toIdx;
				ReplayDeltaBar lastRenderBar;
				while (lastData >= fromIdx && (lastData > maxIdx || !TryGetRenderBar(renderSnapshot, lastData, out lastRenderBar))) lastData--;

				if (lastData >= fromIdx)
				{
					TryGetRenderBar(renderSnapshot, lastData, out lastRenderBar);
					double lastRawValue = DeltaDisplayMode == CumulativeDeltaDisplayMode.BarByBar
						? lastRenderBar.Value
						: lastRenderBar.Close - lastRenderBar.Open;
					if (DeltaDisplayMode == CumulativeDeltaDisplayMode.BarByBar && bundleBarsForCurrentRender)
					{
						BarDeltaRun lastRun;
						int lastRunStart = FindBarDeltaRunStart(renderSnapshot, lastData);
						if (TryBuildBarDeltaRun(renderSnapshot, lastRunStart, lastData, out lastRun))
							lastRawValue = lastRun.Value;
					}
					double lastClose = DeltaDisplayMode == CumulativeDeltaDisplayMode.BarByBar
						? GetRenderedBarDeltaValue(lastRawValue)
						: lastRenderBar.Close;
					bool   lineIsUp  = lastRawValue >= 0;
					var    plBrush   = lineIsUp ? dxUpBorderBrush : dxDownBorderBrush;

					float lineY    = chartScale.GetYByValue(lastClose);
					float lastBarX = chartControl.GetXByBarIndex(ChartBars, lastData);
					float rightX   = panelX + panelW;

					if (lineY >= panelY && lineY <= panelY + panelH && lastBarX < rightX)
						RenderTarget.DrawLine(
							new Vector2(lastBarX, lineY),
							new Vector2(rightX,   lineY),
							plBrush, (float)PriceLineWidth);
				}
			}
			}
			finally
			{
				RenderTarget.AntialiasMode = oldMode;
			}
		}

		private void LogRenderSkip(Exception ex)
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastRenderSkipUtc).TotalSeconds < 5)
				return;

			lastRenderSkipUtc = now;
			Print("OrcaCumulativeDelta: skipped one render frame: " + ex.Message);
		}
		#endregion

		private void TryEnsureSharedBackfillFromRender()
		{
			if (providerDataActive || CurrentBar < 0)
				return;

			if (!ShouldAttemptRealtimeSharedBackfill())
				return;

			if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedProvider)
				TryRefreshFromSharedProvider(false, false, true);
			else if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedHistoricalInternalRealtime)
				TryRefreshFromSharedProvider(false, true, false);
		}

		private void DrawBarDeltaHistogram(ChartControl chartControl, ChartScale chartScale, int barIdx, int fromIdx, int toIdx, ReplayDeltaBar renderBar)
		{
			if (barIdx < 0) return;

			double rawValue = renderBar.Value;
			double renderValue = GetRenderedBarDeltaValue(rawValue);

			float yBase = chartScale.GetYByValue(0);
			float yValue = chartScale.GetYByValue(renderValue);

			float barX = chartControl.GetXByBarIndex(ChartBars, barIdx);
			float barSpacing;
			if (barIdx < toIdx)
				barSpacing = chartControl.GetXByBarIndex(ChartBars, barIdx + 1) - barX;
			else if (barIdx > fromIdx)
				barSpacing = barX - chartControl.GetXByBarIndex(ChartBars, barIdx - 1);
			else
				barSpacing = (float)chartControl.BarWidth;

			float halfW = (float)(barSpacing * BarWidthPercent / 100.0 / 2.0);
			if (halfW < 1f) halfW = 1f;
			float gapPx = Math.Max(0f, BarDeltaGapPx);
			float bodyWidth = Math.Max(1f, halfW * 2f - gapPx);

			float top = Math.Min(yBase, yValue);
			float height = Math.Abs(yBase - yValue);
			if (height < 1f) height = 1f;

			var fillBrush = rawValue >= 0 ? dxUpFillBrush : dxDownFillBrush;
			var borderBrush = rawValue >= 0 ? dxUpBorderBrush : dxDownBorderBrush;
			var rect = new RectangleF(barX - bodyWidth / 2f, top, bodyWidth, height);

			if (ShowBarDeltaWicks)
			{
				float yMax = chartScale.GetYByValue(renderBar.MaxValue);
				float yMin = chartScale.GetYByValue(renderBar.MinValue);
				float bodyTop = rect.Y;
				float bodyBottom = rect.Y + rect.Height;
				if (yMax < bodyTop)
					RenderTarget.DrawLine(new Vector2(barX, yMax), new Vector2(barX, bodyTop), borderBrush, 1f);
				if (yMin > bodyBottom)
					RenderTarget.DrawLine(new Vector2(barX, bodyBottom), new Vector2(barX, yMin), borderBrush, 1f);
			}

			RenderTarget.FillRectangle(rect, fillBrush);
			RenderTarget.DrawRectangle(rect, borderBrush, 1f);
		}

		private int FindBarDeltaRunStart(ReplayDeltaSnapshot snapshot, int seedIndex)
		{
			int start = Math.Max(0, seedIndex);
			ReplayDeltaBar current;
			if (!TryGetRenderBar(snapshot, start, out current))
				return start;

			int sign = GetDeltaSign(current.Value);
			if (sign == 0)
				return start;

			while (start > 0)
			{
				ReplayDeltaBar previous;
				if (!TryGetRenderBar(snapshot, start - 1, out previous)
					|| GetDeltaSign(previous.Value) != sign
					|| !IsSameResetPeriod(start - 1, start))
					break;
				start--;
			}

			return start;
		}

		private bool TryBuildBarDeltaRun(ReplayDeltaSnapshot snapshot, int startIndex, int maxIndex, out BarDeltaRun run)
		{
			run = new BarDeltaRun { StartIndex = startIndex, EndIndex = startIndex, MaxValue = 0, MinValue = 0 };
			ReplayDeltaBar firstBar;
			if (startIndex < 0 || startIndex > maxIndex || !TryGetRenderBar(snapshot, startIndex, out firstBar))
				return false;

			int sign = GetDeltaSign(firstBar.Value);
			double runningValue = 0;
			for (int index = startIndex; index <= maxIndex; index++)
			{
				ReplayDeltaBar bar;
				if (!TryGetRenderBar(snapshot, index, out bar))
					break;
				if (index > startIndex && (sign == 0 || GetDeltaSign(bar.Value) != sign || !IsSameResetPeriod(index - 1, index)))
					break;

				run.MaxValue = Math.Max(run.MaxValue, runningValue + bar.MaxValue);
				run.MinValue = Math.Min(run.MinValue, runningValue + bar.MinValue);
				runningValue += bar.Value;
				run.MaxValue = Math.Max(run.MaxValue, runningValue);
				run.MinValue = Math.Min(run.MinValue, runningValue);
				run.EndIndex = index;
				run.Count++;
				if (sign == 0)
					break;
			}

			run.Value = runningValue;
			return run.Count > 0;
		}

		private void DrawBarDeltaRunHistogram(ChartControl chartControl, ChartScale chartScale, BarDeltaRun run, int fromIdx, int toIdx)
		{
			if (run.Count <= 0)
				return;

			double renderValue = GetRenderedBarDeltaValue(run.Value);
			float yBase = chartScale.GetYByValue(0);
			float yValue = chartScale.GetYByValue(renderValue);
			float startX = chartControl.GetXByBarIndex(ChartBars, run.StartIndex);
			float endX = chartControl.GetXByBarIndex(ChartBars, run.EndIndex);
			float startHalfWidth = GetHistogramHalfWidth(chartControl, run.StartIndex, fromIdx, toIdx);
			float endHalfWidth = GetHistogramHalfWidth(chartControl, run.EndIndex, fromIdx, toIdx);
			float gapHalf = Math.Max(0f, BarDeltaGapPx) / 2f;
			float left = startX - startHalfWidth + gapHalf;
			float right = endX + endHalfWidth - gapHalf;
			float width = Math.Max(1f, right - left);
			float top = Math.Min(yBase, yValue);
			float height = Math.Max(1f, Math.Abs(yBase - yValue));
			float centerX = left + width / 2f;

			var fillBrush = run.Value >= 0 ? dxUpFillBrush : dxDownFillBrush;
			var borderBrush = run.Value >= 0 ? dxUpBorderBrush : dxDownBorderBrush;
			var rect = new RectangleF(left, top, width, height);

			if (ShowBarDeltaWicks)
			{
				float yMax = chartScale.GetYByValue(run.MaxValue);
				float yMin = chartScale.GetYByValue(run.MinValue);
				float bodyTop = rect.Y;
				float bodyBottom = rect.Y + rect.Height;
				if (yMax < bodyTop)
					RenderTarget.DrawLine(new Vector2(centerX, yMax), new Vector2(centerX, bodyTop), borderBrush, 1f);
				if (yMin > bodyBottom)
					RenderTarget.DrawLine(new Vector2(centerX, bodyBottom), new Vector2(centerX, yMin), borderBrush, 1f);
			}

			RenderTarget.FillRectangle(rect, fillBrush);
			RenderTarget.DrawRectangle(rect, borderBrush, 1f);
		}

		private float GetHistogramHalfWidth(ChartControl chartControl, int barIndex, int fromIdx, int toIdx)
		{
			float barX = chartControl.GetXByBarIndex(ChartBars, barIndex);
			float spacing;
			if (barIndex < Math.Min(toIdx, ChartBars.Count - 1))
				spacing = chartControl.GetXByBarIndex(ChartBars, barIndex + 1) - barX;
			else if (barIndex > Math.Max(0, fromIdx))
				spacing = barX - chartControl.GetXByBarIndex(ChartBars, barIndex - 1);
			else
				spacing = (float)chartControl.BarWidth;

			return Math.Max(1f, (float)(spacing * BarWidthPercent / 100.0 / 2.0));
		}

		private bool ShouldBundleSameSignBars(ChartControl chartControl)
		{
			if (!BundleSameSignBars)
			{
				sameSignBundleZoomActive = false;
				return false;
			}

			if (!BundleOnlyWhenZoomedOut)
			{
				sameSignBundleZoomActive = true;
				return true;
			}

			float spacing = GetAverageVisibleBarSpacing(chartControl);
			if (spacing <= 0.0f)
			{
				sameSignBundleZoomActive = false;
				return false;
			}

			float bundleAt = Math.Max(1.0f, BundleAtBarSpacingPx);
			float showDetailsAt = bundleAt + 1.0f;
			if (sameSignBundleZoomActive)
			{
				if (spacing >= showDetailsAt)
					sameSignBundleZoomActive = false;
			}
			else if (spacing <= bundleAt)
			{
				sameSignBundleZoomActive = true;
			}

			return sameSignBundleZoomActive;
		}

		private float GetAverageVisibleBarSpacing(ChartControl chartControl)
		{
			if (chartControl == null || ChartBars == null)
				return 0.0f;

			try
			{
				int fromIndex = Math.Max(0, ChartBars.FromIndex);
				int toIndex = Math.Min(ChartBars.ToIndex, ChartBars.Count - 1);
				if (toIndex <= fromIndex)
					return GetVisibleBarSpacing(chartControl, fromIndex);

				float totalSpacing = 0.0f;
				int sampleCount = 0;
				int step = Math.Max(1, (toIndex - fromIndex) / 24);
				for (int barIndex = fromIndex; barIndex + step <= toIndex; barIndex += step)
				{
					float x1 = chartControl.GetXByBarIndex(ChartBars, barIndex);
					float x2 = chartControl.GetXByBarIndex(ChartBars, barIndex + step);
					totalSpacing += Math.Abs(x2 - x1) / step;
					sampleCount++;
				}

				return sampleCount > 0 ? totalSpacing / sampleCount : 0.0f;
			}
			catch { }

			return 0.0f;
		}

		private float GetVisibleBarSpacing(ChartControl chartControl, int barIndex)
		{
			if (chartControl == null || ChartBars == null || barIndex < 0)
				return 0.0f;

			try
			{
				if (barIndex + 1 < ChartBars.Count)
					return Math.Abs(chartControl.GetXByBarIndex(ChartBars, barIndex + 1) - chartControl.GetXByBarIndex(ChartBars, barIndex));
				if (barIndex > 0)
					return Math.Abs(chartControl.GetXByBarIndex(ChartBars, barIndex) - chartControl.GetXByBarIndex(ChartBars, barIndex - 1));
			}
			catch { }

			return 0.0f;
		}

		private bool IsSameResetPeriod(int firstIndex, int secondIndex)
		{
			if (BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null
				|| firstIndex < 0 || secondIndex < 0
				|| firstIndex >= BarsArray[0].Count || secondIndex >= BarsArray[0].Count)
				return false;

			return GetResetKey(BarsArray[0].GetTime(firstIndex)) == GetResetKey(BarsArray[0].GetTime(secondIndex));
		}

		private int GetDeltaSign(double value)
		{
			return value > 0 ? 1 : value < 0 ? -1 : 0;
		}

		public override void OnCalculateMinMax()
		{
			if (ChartBars == null || barDeltaHigh == null || barDeltaLow == null || barDeltaValue == null || barDeltaMaxValue == null || barDeltaMinValue == null || barHasData == null)
				return;

			ReplayDeltaSnapshot renderSnapshot = replayDeltaSnapshot;
			int dataMax = renderSnapshot != null ? renderSnapshot.MaxIndex : Math.Min(
				Math.Min(barHasData.Count, barDeltaValue.Count),
				Math.Min(barDeltaHigh.Count, Math.Min(barDeltaLow.Count, Math.Min(barDeltaMaxValue.Count, barDeltaMinValue.Count)))) - 1;
			if (dataMax < 0)
				return;

			int first = Math.Max(0, ChartBars.FromIndex);
			int last = Math.Min(ChartBars.ToIndex, dataMax);
			if (first > last)
				return;

			double min = double.MaxValue;
			double max = double.MinValue;
			bool bundleBarsForCurrentScale = ShouldBundleSameSignBars(ChartControl);
			if (DeltaDisplayMode == CumulativeDeltaDisplayMode.BarByBar && bundleBarsForCurrentScale)
			{
				int runIndex = FindBarDeltaRunStart(renderSnapshot, first);
				while (runIndex <= last && runIndex <= dataMax)
				{
					BarDeltaRun run;
					if (!TryBuildBarDeltaRun(renderSnapshot, runIndex, dataMax, out run))
					{
						runIndex++;
						continue;
					}

					if (run.EndIndex >= first)
					{
						double value = GetRenderedBarDeltaValue(run.Value);
						double high = ShowBarDeltaWicks ? Math.Max(value, run.MaxValue) : Math.Max(0, value);
						double low = ShowBarDeltaWicks ? Math.Min(0, run.MinValue) : Math.Min(0, value);
						if (high > max) max = high;
						if (low < min) min = low;
					}
					runIndex = run.EndIndex + 1;
				}
			}
			else
			for (int index = first; index <= last; index++)
			{
				ReplayDeltaBar renderBar;
				if (!TryGetRenderBar(renderSnapshot, index, out renderBar))
					continue;

				if (DeltaDisplayMode == CumulativeDeltaDisplayMode.BarByBar)
				{
					double value = GetRenderedBarDeltaValue(renderBar.Value);
					double high = ShowBarDeltaWicks ? Math.Max(value, renderBar.MaxValue) : Math.Max(0, value);
					double low = ShowBarDeltaWicks ? Math.Min(0, renderBar.MinValue) : Math.Min(0, value);
					if (high > max) max = high;
					if (low < min) min = low;
				}
				else
				{
					if (renderBar.High > max) max = renderBar.High;
					if (renderBar.Low < min) min = renderBar.Low;
				}
			}

			if (min == double.MaxValue || max == double.MinValue)
				return;

			if (IncludeZeroInAutoScale)
			{
				if (min > 0) min = 0;
				if (max < 0) max = 0;
			}

			double range = max - min;
			double padding = range <= 0 ? 10 : range * 0.08;
			MinValue = min - padding;
			MaxValue = max + padding;
		}

		private double GetRenderedBarDeltaValue(int barIdx)
		{
			if (barIdx < 0 || barIdx >= barDeltaValue.Count)
				return 0;

			return GetRenderedBarDeltaValue(barDeltaValue[barIdx]);
		}

		private double GetRenderedBarDeltaValue(double value)
		{
			return BarHistogramStyle == BarDeltaHistogramStyle.SameFloor ? Math.Abs(value) : value;
		}

		private void ApplyResetIfNeeded(int primaryIdx)
		{
			if (primaryIdx < 0 || primaryIdx >= BarsArray[0].Count)
				return;

			int resetKey = GetResetKey(BarsArray[0].GetTime(primaryIdx));
			if (lastResetKey == int.MinValue)
			{
				lastResetKey = resetKey;
				return;
			}

			if (resetKey == lastResetKey)
				return;

			runningDelta = 0;
			prevLast = double.NaN;
			lastDirection = 0;
			lastResetKey = resetKey;
		}

		private int GetResetKey(DateTime time)
		{
			switch (ResetPeriod)
			{
				case CumulativeDeltaResetPeriod.RTHDaily:
					return DateToKey(GetAnchoredDate(time, new TimeSpan(9, 30, 0)));
				case CumulativeDeltaResetPeriod.Weekly:
					return DateToKey(GetEthWeekStartDate(time));
				case CumulativeDeltaResetPeriod.Monthly:
					return time.Year * 100 + time.Month;
				case CumulativeDeltaResetPeriod.FullRange:
					return 0;
				case CumulativeDeltaResetPeriod.ETHDaily:
				default:
					return DateToKey(GetAnchoredDate(time, new TimeSpan(18, 0, 0)));
			}
		}

		private DateTime GetAnchoredDate(DateTime time, TimeSpan sessionStart)
		{
			return time.TimeOfDay >= sessionStart ? time.Date : time.Date.AddDays(-1);
		}

		private DateTime GetEthWeekStartDate(DateTime time)
		{
			int daysSinceSunday = (int)time.DayOfWeek;
			DateTime sunday = time.Date.AddDays(-daysSinceSunday);
			if (time.DayOfWeek == DayOfWeek.Sunday && time.TimeOfDay < new TimeSpan(18, 0, 0))
				sunday = sunday.AddDays(-7);
			return sunday;
		}

		private int DateToKey(DateTime date)
		{
			return date.Year * 10000 + date.Month * 100 + date.Day;
		}

		#region DX Resources
		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxResourceRenderTarget != IntPtr.Zero && dxResourceRenderTarget != currentTarget)
				DisposeDxResources();

			if (dxUpFillBrush == null)
			{
				dxResourceRenderTarget = currentTarget;
				float fillOpacity   = (float)Math.Max(0.0, Math.Min(1.0, BarOpacity));
				float borderOpacity = (float)Math.Max(0.0, Math.Min(1.0, BorderOpacity));

				dxUpFillBrush         = ColorUp.ToDxBrush(RenderTarget);
				dxUpFillBrush.Opacity = fillOpacity;
				dxDownFillBrush         = ColorDown.ToDxBrush(RenderTarget);
				dxDownFillBrush.Opacity = fillOpacity;

				dxUpBorderBrush           = ColorUpBorder.ToDxBrush(RenderTarget);
				dxUpBorderBrush.Opacity   = borderOpacity;
				dxDownBorderBrush           = ColorDownBorder.ToDxBrush(RenderTarget);
				dxDownBorderBrush.Opacity   = borderOpacity;
				dxWickBrush			= WickColor.ToDxBrush(RenderTarget);
				dxWickBrush.Opacity = borderOpacity;
				dxZeroBrush			= ZeroLineColor.ToDxBrush(RenderTarget);
				dxPriceLineBrush    = Brushes.White.ToDxBrush(RenderTarget);
			}
		}

		private void DisposeDxResources()
		{
			if (dxUpFillBrush	  != null) { dxUpFillBrush.Dispose();	  dxUpFillBrush		= null; }
			if (dxDownFillBrush	  != null) { dxDownFillBrush.Dispose();	  dxDownFillBrush	= null; }
			if (dxUpBorderBrush	  != null) { dxUpBorderBrush.Dispose();	  dxUpBorderBrush	= null; }
			if (dxDownBorderBrush != null) { dxDownBorderBrush.Dispose(); dxDownBorderBrush	= null; }
			if (dxWickBrush		    != null) { dxWickBrush.Dispose();		    dxWickBrush		    = null; }
			if (dxZeroBrush		    != null) { dxZeroBrush.Dispose();		    dxZeroBrush		    = null; }
			if (dxPriceLineBrush    != null) { dxPriceLineBrush.Dispose();     dxPriceLineBrush    = null; }
			dxResourceRenderTarget = IntPtr.Zero;
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}
		#endregion

		#region Properties

		[XmlIgnore]
		[Display(Name = "Color Up", Order = 1, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorUp { get; set; }
		[Browsable(false)]
		public string ColorUpSerialize { get { return Serialize.BrushToString(ColorUp); } set { ColorUp = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Color Down", Order = 2, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorDown { get; set; }
		[Browsable(false)]
		public string ColorDownSerialize { get { return Serialize.BrushToString(ColorDown); } set { ColorDown = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Color Up Border", Order = 3, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorUpBorder { get; set; }
		[Browsable(false)]
		public string ColorUpBorderSerialize { get { return Serialize.BrushToString(ColorUpBorder); } set { ColorUpBorder = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Color Down Border", Order = 4, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorDownBorder { get; set; }
		[Browsable(false)]
		public string ColorDownBorderSerialize { get { return Serialize.BrushToString(ColorDownBorder); } set { ColorDownBorder = Serialize.StringToBrush(value); } }

		[Range(0.0, 1.0)]
		[Display(Name = "Bar Opacity", Order = 5, GroupName = "Visual Parameters")]
		public double BarOpacity { get; set; }

		[Range(0.0, 1.0)]
		[Display(Name = "Border Opacity", Order = 6, GroupName = "Visual Parameters")]
		public double BorderOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Wick Color", Order = 6, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush WickColor { get; set; }
		[Browsable(false)]
		public string WickColorSerialize { get { return Serialize.BrushToString(WickColor); } set { WickColor = Serialize.StringToBrush(value); } }

		[Range(1, 100)]
		[Display(Name = "Bar Width %", Order = 8, GroupName = "Visual Parameters")]
		public int BarWidthPercent { get; set; }

		[Display(Name = "Show Price Line", Order = 1, GroupName = "Price Line")]
		public bool ShowPriceLine { get; set; }

		[Range(1, 5)]
		[Display(Name = "Price Line Width", Order = 2, GroupName = "Price Line")]
		public int PriceLineWidth { get; set; }

		[XmlIgnore]
		[Display(Name = "Zero Line Color", Order = 1, GroupName = "Reference Levels")]
		public System.Windows.Media.Brush ZeroLineColor { get; set; }
		[Browsable(false)]
		public string ZeroLineColorSerialize { get { return Serialize.BrushToString(ZeroLineColor); } set { ZeroLineColor = Serialize.StringToBrush(value); } }

		[Range(1, 5)]
		[Display(Name = "Zero Line Width", Order = 2, GroupName = "Reference Levels")]
		public int ZeroLineWidth { get; set; }

		[Display(Name = "Include Zero In Auto Scale", Order = 3, GroupName = "Reference Levels",
			Description = "When false, autoscale follows the visible cumulative delta range instead of forcing the zero line into view.")]
		public bool IncludeZeroInAutoScale { get; set; }

		[Display(Name = "Delta Display Mode", Order = 1, GroupName = "Delta Display",
			Description = "Cumulative: running session delta candles. BarByBar: each candle's net delta as a histogram.")]
		public CumulativeDeltaDisplayMode DeltaDisplayMode { get; set; }

		[Display(Name = "Bar Delta Histogram Style", Order = 2, GroupName = "Delta Display",
			Description = "Mirrored: positive bars above zero and negative bars below zero. SameFloor: both signs draw upward from zero, with color denoting sign.")]
		public BarDeltaHistogramStyle BarHistogramStyle { get; set; }

		[Display(Name = "Bundle Same-Sign Bars", Order = 3, GroupName = "Delta Display",
			Description = "BarByBar only: combines each uninterrupted run of two or more positive or negative bars into one wider block whose height is the run's summed net delta. Zero-delta bars and reset boundaries end a run.")]
		public bool BundleSameSignBars { get; set; }

		[Display(Name = "Bundle Only When Zoomed Out", Order = 4, GroupName = "Delta Display",
			Description = "When enabled, same-sign bundles appear only when average visible bar spacing is at or below the configured pixel threshold; zooming back in restores individual bar detail.")]
		public bool BundleOnlyWhenZoomedOut { get; set; }

		[Range(1, 50)]
		[Display(Name = "Bundle At Bar Spacing (px)", Order = 5, GroupName = "Delta Display",
			Description = "Average visible pixels per bar at or below which same-sign bundling activates. Individual bars return one pixel above this value to prevent flicker near the cutoff.")]
		public int BundleAtBarSpacingPx { get; set; }

		[Display(Name = "Show Bar Delta Wicks", Order = 6, GroupName = "Delta Display",
			Description = "BarByBar only: draws each bar's intrabar max/min delta as a wick while the body ends at final net delta.")]
		public bool ShowBarDeltaWicks { get; set; }

		[Range(0.0, 10.0)]
		[Display(Name = "Bar Delta Gap (px)", Order = 7, GroupName = "Delta Display",
			Description = "BarByBar only: subtracts this many pixels from histogram body width to separate adjacent bars.")]
		public float BarDeltaGapPx { get; set; }

		[Display(Name = "Order Flow Source", Order = 0, GroupName = "Delta Calculation",
			Description = "Internal keeps the existing local tick-series calculation. SharedProvider reads only OrcaProfileDataProvider. SharedHistoricalInternalRealtime backfills from the provider, then uses local real-time market data.")]
		public OrcaOrderFlowSourceMode OrderFlowSourceMode { get; set; }

		[Display(Name = "Delta Mode", Order = 1, GroupName = "Delta Calculation",
			Description = "BidAsk: classifies each trade against the bid/ask spread (most accurate live). TickDirection: classifies by whether price moved up or down tick-to-tick (works historically and live).")]
		public CumulativeDeltaMode DeltaMode { get; set; }

		[Display(Name = "Reset Period", Order = 2, GroupName = "Delta Calculation",
			Description = "Controls where cumulative delta starts over: ETH day, RTH day, week, month, or full loaded range.")]
		public CumulativeDeltaResetPeriod ResetPeriod { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaCumulativeDelta[] cacheOrcaCumulativeDelta;
		public OrcaCumulativeDelta OrcaCumulativeDelta()
		{
			return OrcaCumulativeDelta(Input);
		}

		public OrcaCumulativeDelta OrcaCumulativeDelta(ISeries<double> input)
		{
			if (cacheOrcaCumulativeDelta != null)
				for (int idx = 0; idx < cacheOrcaCumulativeDelta.Length; idx++)
					if (cacheOrcaCumulativeDelta[idx] != null &&  cacheOrcaCumulativeDelta[idx].EqualsInput(input))
						return cacheOrcaCumulativeDelta[idx];
			return CacheIndicator<OrcaCumulativeDelta>(new OrcaCumulativeDelta(), input, ref cacheOrcaCumulativeDelta);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaCumulativeDelta OrcaCumulativeDelta()
		{
			return indicator.OrcaCumulativeDelta(Input);
		}

		public Indicators.OrcaCumulativeDelta OrcaCumulativeDelta(ISeries<double> input )
		{
			return indicator.OrcaCumulativeDelta(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaCumulativeDelta OrcaCumulativeDelta()
		{
			return indicator.OrcaCumulativeDelta(Input);
		}

		public Indicators.OrcaCumulativeDelta OrcaCumulativeDelta(ISeries<double> input )
		{
			return indicator.OrcaCumulativeDelta(input);
		}
	}
}

#endregion
