#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum OrcaTimeStatisticsCumulativeDeltaStartMode
	{
		[Description("Full Day")]
		OneDaySixPmEastern,
		[Description("RTH")]
		OneDayRth,
		[Description("Weekly")]
		WeekSundaySixPmEastern
	}

	public enum OrcaTimeStatisticsFontWeight
	{
		Light = 0,
		Regular = 1,
		Medium = 2,
		SemiBold = 3,
		Bold = 4,
		ExtraBold = 5
	}

	public class OrcaTimeStatisticsCumulativeDeltaStartModeConverter : EnumConverter
	{
		public OrcaTimeStatisticsCumulativeDeltaStartModeConverter() : base(typeof(OrcaTimeStatisticsCumulativeDeltaStartMode))
		{
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is OrcaTimeStatisticsCumulativeDeltaStartMode)
				return GetDescription((OrcaTimeStatisticsCumulativeDeltaStartMode)value);

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (!string.IsNullOrWhiteSpace(text))
			{
				foreach (OrcaTimeStatisticsCumulativeDeltaStartMode mode in Enum.GetValues(typeof(OrcaTimeStatisticsCumulativeDeltaStartMode)))
				{
					if (string.Equals(text, GetDescription(mode), StringComparison.OrdinalIgnoreCase) || string.Equals(text, mode.ToString(), StringComparison.OrdinalIgnoreCase))
						return mode;
				}
			}

			return base.ConvertFrom(context, culture, value);
		}

		private static string GetDescription(OrcaTimeStatisticsCumulativeDeltaStartMode mode)
		{
			var field = typeof(OrcaTimeStatisticsCumulativeDeltaStartMode).GetField(mode.ToString());
			var description = field != null ? Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute : null;
			return description != null ? description.Description : mode.ToString();
		}
	}

	public class OrcaInstalledFontFamilyConverter : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return false;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> fontNames = new List<string>();

			foreach (System.Windows.Media.FontFamily family in Fonts.SystemFontFamilies)
			{
				string name = family != null ? family.Source : null;
				if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
					continue;

				fontNames.Add(name);
			}

			fontNames.Sort(StringComparer.CurrentCultureIgnoreCase);
			return new StandardValuesCollection(fontNames);
		}
	}

	public class OrcaTimeStatistics : Indicator, IOrcaReplayParticipant
	{
		private struct CandleMetrics
		{
			public double Body;
			public double BodyPercent;
			public double CloseLocationPercent;
			public double UpperWickPercent;
			public double LowerWickPercent;
			public bool HasRangePercentages;
		}

		private sealed class AverageSummary
		{
			public double VolumeSum;
			public int VolumeCount;
			public double VolumePerSecondSum;
			public int VolumePerSecondCount;
			public double VolumePerRangeTickSum;
			public int VolumePerRangeTickCount;
			public double DeltaAbsSum;
			public int DeltaCount;
			public double DeltaPerSecondAbsSum;
			public int DeltaPerSecondCount;
			public double CumulativeDeltaAbsSum;
			public int CumulativeDeltaCount;
			public double DeltaPercentAbsSum;
			public int DeltaPercentCount;
			public double MaxDeltaAbsSum;
			public int MaxDeltaCount;
			public double MinDeltaAbsSum;
			public int MinDeltaCount;
			public double FinishDeltaAbsSum;
			public int FinishDeltaCount;
			public double RangeSum;
			public int RangeCount;
			public double BodySum;
			public int BodyCount;
			public double BodyPercentSum;
			public int BodyPercentCount;
			public double CloseLocationPercentSum;
			public int CloseLocationPercentCount;
			public double UpperWickPercentSum;
			public int UpperWickPercentCount;
			public double LowerWickPercentSum;
			public int LowerWickPercentCount;
			public double TimeSecondsSum;
			public int TimeCount;
		}

		private double	lastBid;
		private double	lastAsk;
		private double	prevLast;
		private int		lastDirection;

		private List<double>	barTickDelta;
		private List<double>	barMaxDelta;
		private List<double>	barMinDelta;
		private List<bool>		barHasData;
		private int providerRevision = -1;
		private int providerCurrentBar = -1;
		private bool providerDataActive;
		private int providerOutageWarningLogged;
		private DateTime lastBarUpdateWarningUtc = DateTime.MinValue;
		private DateTime lastSharedBackfillAttemptUtc = DateTime.MinValue;
		private DateTime lastSharedBackfillSuccessLogUtc = DateTime.MinValue;
		private const int SharedProviderMaxRealtimeLagSeconds = 30;
		private readonly string diagnosticsInstanceId = Guid.NewGuid().ToString("N");
		private bool diagnosticsRegistered;
		private OrcaReplayBarHorizon replayBarHorizon;

		private SharpDX.Direct2D1.Brush	dxVolumeBrush;
		private SharpDX.Direct2D1.Brush	dxPositiveBrush;
		private SharpDX.Direct2D1.Brush	dxNegativeBrush;
		private SharpDX.Direct2D1.Brush	dxMaxDeltaBrush;
		private SharpDX.Direct2D1.Brush	dxMinDeltaBrush;
		private SharpDX.Direct2D1.Brush	dxFinPosBrush;
		private SharpDX.Direct2D1.Brush	dxFinNegBrush;
		private SharpDX.Direct2D1.Brush	dxRangeBrush;
		private SharpDX.Direct2D1.Brush	dxTimeBrush;
		private SharpDX.Direct2D1.Brush	dxTextBrush;
		private SharpDX.Direct2D1.Brush	dxSeparatorBrush;
		private SharpDX.Direct2D1.Brush	dxScaleMaskBrush;
		private SharpDX.DirectWrite.TextFormat	dxTextFormat;
		private SharpDX.DirectWrite.Factory dwFactory;
		private IntPtr dxResourceRenderTarget = IntPtr.Zero;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "OrcaTimeStatistics";
				Description					= "Displays optional per-bar activity, order-flow, candle-efficiency, range, and timing statistics.";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= false;
				DisplayInDataBox			= false;
				IsSuspendedWhileInactive	= false;
				BarsRequiredToPlot			= 0;
				DrawHorizontalGridLines		= false;
				DrawVerticalGridLines		= false;
				IsAutoScale					= false;
				PaintPriceMarkers			= false;
				ScaleJustification			= ScaleJustification.Overlay;

				VolumeColor          = Brushes.SkyBlue;
				PositiveDeltaColor   = Brushes.LimeGreen;
				NegativeDeltaColor   = Brushes.Crimson;
				MaxDeltaColor        = Brushes.MediumSeaGreen;
				MinDeltaColor        = Brushes.IndianRed;
				FinishDeltaPosColor  = Brushes.MediumOrchid;
				FinishDeltaNegColor  = Brushes.OrangeRed;
				RangeColor           = Brushes.DodgerBlue;
				TimeColor            = Brushes.SlateGray;
				TextColor            = Brushes.Black;
				CellSeparatorColor   = Brushes.Black;
				BaseOpacity          = 0.25;
				FontFamilyName       = "Segoe UI";
				TextFontWeight       = OrcaTimeStatisticsFontWeight.Bold;
				FontSize             = 11;
				ShowCellSeparators   = true;
				CellSeparatorThickness = 1f;
				ShowAverageValues    = true;
				AverageLookbackBars  = 14;

				ShowVolume           = true;
				ShowVolumePerSecond  = false;
				ShowVolumePerRangeTick = false;
				ShowDelta            = true;
				ShowDeltaPerSecond   = false;
				ShowCumulativeDelta  = false;
				CumulativeDeltaStartMode = OrcaTimeStatisticsCumulativeDeltaStartMode.OneDaySixPmEastern;
				ShowDeltaPercent     = false;
				ShowMaxDelta         = false;
				ShowMinDelta         = false;
				ShowFinishDelta      = true;
				ShowRange            = true;
				ShowBody             = false;
				ShowBodyPercent      = false;
				ShowCloseLocation    = false;
				ShowUpperWick        = false;
				ShowLowerWick        = false;
				ShowTime             = true;
				OrderFlowSourceMode  = OrcaOrderFlowSourceMode.Internal;

			}
			else if (State == State.Configure)
			{
				// Preserve continuous market-data processing after saved settings are applied.
				IsSuspendedWhileInactive = false;
			}
			else if (State == State.DataLoaded)
			{
				barTickDelta   = new List<double>(4096);
				barMaxDelta    = new List<double>(4096);
				barMinDelta    = new List<double>(4096);
				barHasData     = new List<bool>(4096);
				lastBid        = double.NaN;
				lastAsk        = double.NaN;
				prevLast       = double.NaN;
				lastDirection  = 0;
				replayBarHorizon = new OrcaReplayBarHorizon("OrcaTimeStatistics:" + diagnosticsInstanceId);
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				ReportDiagnosticsState();
			}
			else if (State == State.Historical || State == State.Transition || State == State.Realtime)
			{
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				ReportDiagnosticsState();
			}
			else if (State == State.Terminated)
			{
				if (ChartControl != null) OrcaReplayCore.UnregisterParticipant(ChartControl, this);
				if (replayBarHorizon != null) replayBarHorizon.Restore();
				OrcaDiagnosticsCore.UnregisterInstance(diagnosticsInstanceId);
				DisposeDxResources();
			}
		}

		private void EnsureDiagnosticsRegistered()
		{
			if (diagnosticsRegistered)
				return;

			OrcaDiagnosticsCore.RegisterInstance(diagnosticsInstanceId, "OrcaTimeStatistics", this);
			ReportDiagnosticsSourceDeclaration("Unknown");
			OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 0, "PrimaryChartSeries", "Chart", "Time statistics primary bars");
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
				sourceMode = "PrimaryChartMarketData";
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

		private void EnsureBarLists(int idx)
		{
			if (idx < 0)
				return;

			EnsureDeltaStorage();
			while (barTickDelta.Count <= idx)
				barTickDelta.Add(0);
			while (barMaxDelta.Count <= idx)
				barMaxDelta.Add(0);
			while (barMinDelta.Count <= idx)
				barMinDelta.Add(0);
			while (barHasData.Count <= idx)
				barHasData.Add(false);
		}

		private void EnsureDeltaStorage()
		{
			if (barTickDelta == null)
				barTickDelta = new List<double>(4096);
			if (barMaxDelta == null)
				barMaxDelta = new List<double>(4096);
			if (barMinDelta == null)
				barMinDelta = new List<double>(4096);
			if (barHasData == null)
				barHasData = new List<bool>(4096);
		}

		private bool HasDeltaForBar(int barIndex)
		{
			return barIndex >= 0
				&& barTickDelta != null
				&& barMaxDelta != null
				&& barMinDelta != null
				&& barHasData != null
				&& barIndex < barTickDelta.Count
				&& barIndex < barMaxDelta.Count
				&& barIndex < barMinDelta.Count
				&& barIndex < barHasData.Count
				&& barHasData[barIndex];
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

			if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedHistoricalInternalRealtime && State == State.Realtime && !providerDataActive && ShouldAttemptRealtimeSharedBackfill())
				TryRefreshFromSharedProvider(false, true, false);

			if (e.MarketDataType == MarketDataType.Bid) lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask) lastAsk = e.Price;
			else if (e.MarketDataType == MarketDataType.Last)
			{
				if (e.Ask > 0 && !double.IsNaN(e.Ask)) lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid)) lastBid = e.Bid;

				long vol = e.Volume;
				if (Instrument != null && Instrument.MasterInstrument != null && Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
					vol = (long)NinjaTrader.Core.Globals.ToCryptocurrencyVolume(vol);

				long signed = 0;
				if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
				{
					if (e.Price >= lastAsk) signed = vol;
					else if (e.Price <= lastBid) signed = -vol;
					else if (!double.IsNaN(prevLast))
					{
						if (e.Price > prevLast) signed = vol;
						else if (e.Price < prevLast) signed = -vol;
						else signed = lastDirection * vol;
					}
				}
				else if (!double.IsNaN(prevLast))
				{
					if (e.Price > prevLast) signed = vol;
					else if (e.Price < prevLast) signed = -vol;
					else signed = lastDirection * vol;
				}

				if (signed > 0) lastDirection = 1;
				else if (signed < 0) lastDirection = -1;

				prevLast = e.Price;

				if (signed != 0 && BarsArray != null && BarsArray.Length > 0 && BarsArray[0] != null && BarsArray[0].Count > 0)
				{
					// OnEachTick has already assigned this trade's bar before OnMarketData.
					// GetBar(time) chooses the first match when range/volume bars share a timestamp.
					int primaryIdx = Calculate == Calculate.OnEachTick ? CurrentBar : BarsArray[0].GetBar(e.Time);
					if (primaryIdx >= 0 && primaryIdx < BarsArray[0].Count)
					{
						EnsureBarLists(primaryIdx);
						barTickDelta[primaryIdx] += signed;
						barMaxDelta[primaryIdx] = Math.Max(barMaxDelta[primaryIdx], barTickDelta[primaryIdx]);
						barMinDelta[primaryIdx] = Math.Min(barMinDelta[primaryIdx], barTickDelta[primaryIdx]);
						barHasData[primaryIdx] = true;
                        OrcaDiagnosticsCore.ReportModelUpdate(diagnosticsInstanceId, e.Time, null);
					}
				}
			}
			}
			finally
			{
				if (diagnosticsWorkStart > 0)
					OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.MarketData, -1, diagnosticsWorkStart);
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

			if (!IsPrimarySeriesReady())
				return;

			try {
				EnsureBarLists(CurrentBar);
				if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedProvider)
				{
					if (ShouldRefreshSharedHistorical())
						TryRefreshFromSharedProvider(false, false, true);
				}
				else if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedHistoricalInternalRealtime && ((State == State.Realtime && !providerDataActive && ShouldAttemptRealtimeSharedBackfill()) || ShouldRefreshSharedHistorical()))
					TryRefreshFromSharedProvider(false, true, false);
				try {
					if (Bars != null && Bars.IsFirstBarOfSession) { lastBid = double.NaN; lastAsk = double.NaN; prevLast = double.NaN; }
				} catch { }
			} catch (Exception ex) {
				LogBarUpdateWarning(ex);
			}
            }
            finally
            {
                if (diagnosticsWorkStart > 0)
                    OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.BarUpdate, diagnosticsBarsInProgress, diagnosticsWorkStart);
            }
		}

		private bool IsPrimarySeriesReady()
		{
			try {
				return BarsInProgress == 0
					&& CurrentBar >= 0
					&& Bars != null
					&& CurrentBars != null
					&& CurrentBars.Length > 0
					&& CurrentBars[0] >= 0
					&& Bars.Count > 0
					&& CurrentBar < Bars.Count;
			} catch { return false; }
		}

		private void LogBarUpdateWarning(Exception ex)
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastBarUpdateWarningUtc).TotalSeconds < 30)
				return;

			lastBarUpdateWarningUtc = now;
			Print("OrcaTimeStatistics: skipped bar update after startup data error: " + (ex != null ? ex.Message : "unknown error"));
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

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
            long diagnosticsRenderStart = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
			if (chartControl == null || chartScale == null || Bars == null || ChartBars == null || ChartPanel == null || Instrument == null || Instrument.MasterInstrument == null) return;
			EnsureDeltaStorage();
			if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedProvider && State == State.Realtime)
				TryRefreshFromSharedProvider(false, false, true);
			else if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedHistoricalInternalRealtime && State == State.Realtime && !providerDataActive && ShouldAttemptRealtimeSharedBackfill())
				TryRefreshFromSharedProvider(false, true, false);

			int rowCount = (ShowVolume ? 1 : 0) + (ShowVolumePerSecond ? 1 : 0) + (ShowVolumePerRangeTick ? 1 : 0) + (ShowDelta ? 1 : 0) + (ShowDeltaPerSecond ? 1 : 0) + (ShowCumulativeDelta ? 1 : 0) + (ShowDeltaPercent ? 1 : 0)
						+ (ShowMaxDelta ? 1 : 0) + (ShowMinDelta ? 1 : 0) + (ShowFinishDelta ? 1 : 0)
						+ (ShowRange ? 1 : 0) + (ShowBody ? 1 : 0) + (ShowBodyPercent ? 1 : 0) + (ShowCloseLocation ? 1 : 0)
						+ (ShowUpperWick ? 1 : 0) + (ShowLowerWick ? 1 : 0) + (ShowTime ? 1 : 0);
			if (rowCount == 0) return;

			int fromIdx = Math.Max(0, ChartBars.FromIndex);
			int toIdx   = Math.Min(ChartBars.ToIndex, Bars.Count - 1);
			int replayMaxBar = replayBarHorizon == null ? -1 : replayBarHorizon.MaxBarIndex;
			if (replayMaxBar >= 0) toIdx = Math.Min(toIdx, replayMaxBar);
			if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx) return;

			EnsureDxResources();
			if (dxVolumeBrush == null) return;

			float panelY = ChartPanel.Y;
			float panelH = ChartPanel.H;
			float rowH = panelH / rowCount;

			var rows = new List<KeyValuePair<string, int>>();
			if (ShowVolume)      rows.Add(new KeyValuePair<string, int>("Volume",       0));
			if (ShowVolumePerSecond) rows.Add(new KeyValuePair<string, int>("Volume / Sec", 9));
			if (ShowVolumePerRangeTick) rows.Add(new KeyValuePair<string, int>("Volume / Tick", 10));
			if (ShowDelta)       rows.Add(new KeyValuePair<string, int>("Delta",        1));
			if (ShowDeltaPerSecond) rows.Add(new KeyValuePair<string, int>("Delta / Sec", 11));
			if (ShowDeltaPercent) rows.Add(new KeyValuePair<string, int>("\u0394 %",        8));
			if (ShowMaxDelta)    rows.Add(new KeyValuePair<string, int>("Max \u0394",       5));
			if (ShowMinDelta)    rows.Add(new KeyValuePair<string, int>("Min \u0394",       6));
			if (ShowFinishDelta) rows.Add(new KeyValuePair<string, int>("Finish \u0394",  2));
			if (ShowCumulativeDelta) rows.Add(new KeyValuePair<string, int>("Cumulative \u0394", 7));
			if (ShowRange)       rows.Add(new KeyValuePair<string, int>("Range",        3));
			if (ShowBody)        rows.Add(new KeyValuePair<string, int>("Body",         12));
			if (ShowBodyPercent) rows.Add(new KeyValuePair<string, int>("Body %",       13));
			if (ShowCloseLocation) rows.Add(new KeyValuePair<string, int>("Close Location", 14));
			if (ShowUpperWick)   rows.Add(new KeyValuePair<string, int>("Upper Wick",   15));
			if (ShowLowerWick)   rows.Add(new KeyValuePair<string, int>("Lower Wick",   16));
			if (ShowTime)        rows.Add(new KeyValuePair<string, int>("Time",         4));

			double maxVol = 1, maxVolumePerSecond = 1, maxVolumePerRangeTick = 1, maxDel = 1, maxDeltaPerSecond = 1, maxCumDel = 1, maxDeltaPercent = 1, maxRange = 1, maxBody = 1, maxBodyPercent = 1, maxUpperWickPercent = 1, maxLowerWickPercent = 1;
			double tickSize = Math.Max(0.00000001, Instrument.MasterInstrument.TickSize);
			double[] cumulativeDeltaValues = ShowCumulativeDelta ? BuildCumulativeDeltaValues(toIdx) : null;
			bool needsCandleMetrics = ShowBody || ShowBodyPercent || ShowCloseLocation || ShowUpperWick || ShowLowerWick;

			for (int i = fromIdx; i <= toIdx; i++)
			{
				double vol, high, low, range;
				if (!TryGetBarStats(i, out vol, out high, out low, out range)) continue;
				if (ShowVolume) maxVol = Math.Max(maxVol, vol);
				double volumePerSecond;
				if (ShowVolumePerSecond && TryCalculateVolumePerSecond(i, vol, out volumePerSecond))
					maxVolumePerSecond = Math.Max(maxVolumePerSecond, volumePerSecond);
				double volumePerRangeTick;
				if (ShowVolumePerRangeTick && TryCalculateVolumePerRangeTick(vol, range, tickSize, out volumePerRangeTick))
					maxVolumePerRangeTick = Math.Max(maxVolumePerRangeTick, volumePerRangeTick);
				CandleMetrics candleMetrics = new CandleMetrics();
				if (needsCandleMetrics && TryGetCandleMetrics(i, high, low, out candleMetrics))
				{
					if (ShowBody) maxBody = Math.Max(maxBody, candleMetrics.Body);
					if (candleMetrics.HasRangePercentages)
					{
						if (ShowBodyPercent) maxBodyPercent = Math.Max(maxBodyPercent, candleMetrics.BodyPercent);
						if (ShowUpperWick) maxUpperWickPercent = Math.Max(maxUpperWickPercent, candleMetrics.UpperWickPercent);
						if (ShowLowerWick) maxLowerWickPercent = Math.Max(maxLowerWickPercent, candleMetrics.LowerWickPercent);
					}
				}
				if (ShowRange)  maxRange = Math.Max(maxRange, range);
				if (ShowCumulativeDelta && cumulativeDeltaValues != null && i < cumulativeDeltaValues.Length)
					maxCumDel = Math.Max(maxCumDel, Math.Abs(cumulativeDeltaValues[i]));
				if (HasDeltaForBar(i))
				{
					if (ShowDelta) maxDel = Math.Max(maxDel, Math.Abs(barTickDelta[i]));
					if (ShowDeltaPerSecond)
					{
						double deltaPerSecond;
						if (TryCalculateDeltaPerSecond(i, barTickDelta[i], out deltaPerSecond))
							maxDeltaPerSecond = Math.Max(maxDeltaPerSecond, Math.Abs(deltaPerSecond));
					}
					if (ShowDeltaPercent)
						maxDeltaPercent = Math.Max(maxDeltaPercent, Math.Abs(CalculateDeltaPercent(barTickDelta[i], vol)));
					if (ShowMaxDelta) maxDel = Math.Max(maxDel, Math.Abs(barMaxDelta[i]));
					if (ShowMinDelta) maxDel = Math.Max(maxDel, Math.Abs(barMinDelta[i]));
					if (ShowFinishDelta) maxDel = Math.Max(maxDel, Math.Abs(GetFinishDelta(i)));
				}
			}

			SharpDX.Direct2D1.AntialiasMode oldAA = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
			SharpDX.Direct2D1.TextAntialiasMode oldTAA = RenderTarget.TextAntialiasMode;
			RenderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Cleartype;


			int firstVisibleIdx = Math.Max(0, fromIdx);
			int lastVisibleIdx = Math.Min(toIdx, Bars.Count - 1);
			AverageSummary averageSummary = ShowAverageValues ? CalculateAverageSummary(lastVisibleIdx, tickSize) : null;

			for (int i = fromIdx; i <= toIdx; i++)
			{
				double vol, high, low, range;
				if (!TryGetBarStats(i, out vol, out high, out low, out range)) continue;
				float x = chartControl.GetXByBarIndex(ChartBars, i);
				float barSpacing = (i < toIdx) ? (chartControl.GetXByBarIndex(ChartBars, i + 1) - x) : ((i > fromIdx) ? (x - chartControl.GetXByBarIndex(ChartBars, i - 1)) : (float)chartControl.BarWidth);
				float boxW = Math.Max(2f, barSpacing);

				bool hasDelta = HasDeltaForBar(i);
				double del   = hasDelta ? barTickDelta[i] : 0;
				CandleMetrics candleMetrics = new CandleMetrics();
				bool hasCandleMetrics = needsCandleMetrics && TryGetCandleMetrics(i, high, low, out candleMetrics);

				for (int r = 0; r < rows.Count; r++)
				{
					float rowY = panelY + r * rowH;
					RectangleF rect = new RectangleF(x - boxW / 2, rowY, boxW, rowH);
					switch (rows[r].Value)
					{
						case 0: // Volume
							dxVolumeBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * (vol / maxVol));
							RenderTarget.FillRectangle(rect, dxVolumeBrush);
							if (boxW >= 20) DrawCenteredText(FormatVolume(vol), rect);
							break;
						case 9: // Volume per second
							double volumePerSecond;
							if (!TryCalculateVolumePerSecond(i, vol, out volumePerSecond)) break;
							dxVolumeBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * (volumePerSecond / maxVolumePerSecond));
							RenderTarget.FillRectangle(rect, dxVolumeBrush);
							if (boxW >= 20) DrawCenteredText(FormatVolume(volumePerSecond), rect);
							break;
						case 10: // Volume per range tick
							double volumePerRangeTick;
							if (!TryCalculateVolumePerRangeTick(vol, range, tickSize, out volumePerRangeTick)) break;
							dxVolumeBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, volumePerRangeTick / maxVolumePerRangeTick));
							RenderTarget.FillRectangle(rect, dxVolumeBrush);
							if (boxW >= 20) DrawCenteredText(FormatVolume(volumePerRangeTick), rect);
							break;
						case 1: // Delta
							if (!hasDelta) break;
							var dBrush = del >= 0 ? dxPositiveBrush : dxNegativeBrush;
							dBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * (Math.Abs(del) / maxDel));
							RenderTarget.FillRectangle(rect, dBrush);
							if (boxW >= 20) DrawCenteredText(FormatDelta(del), rect);
							break;
						case 11: // Delta per second
							if (!hasDelta) break;
							double deltaPerSecond;
							if (!TryCalculateDeltaPerSecond(i, del, out deltaPerSecond)) break;
							var deltaRateBrush = deltaPerSecond >= 0 ? dxPositiveBrush : dxNegativeBrush;
							deltaRateBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, Math.Abs(deltaPerSecond) / maxDeltaPerSecond));
							RenderTarget.FillRectangle(rect, deltaRateBrush);
							if (boxW >= 20) DrawCenteredText(FormatSignedRate(deltaPerSecond), rect);
							break;
						case 2: // Finish Delta = (Current Delta - Extreme Delta)
							if (!hasDelta) break;
							double finDelta = GetFinishDelta(i);
							var fBrush = finDelta >= 0 ? dxFinPosBrush : dxFinNegBrush;
							// Scale opacity based on absolute value relative to absolute max delta (or some fixed scale)
							fBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, Math.Abs(finDelta) / maxDel));
							RenderTarget.FillRectangle(rect, fBrush);
							if (boxW >= 20) DrawCenteredText(FormatSignedDelta(finDelta), rect);
							break;
						case 3: // Range (H-L in ticks)
							dxRangeBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * (range / maxRange));
							RenderTarget.FillRectangle(rect, dxRangeBrush);
							if (boxW >= 20) DrawCenteredText(FormatRange(range, tickSize), rect);
							break;
						case 12: // Body
							if (!hasCandleMetrics) break;
							dxRangeBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, candleMetrics.Body / maxBody));
							RenderTarget.FillRectangle(rect, dxRangeBrush);
							if (boxW >= 20) DrawCenteredText(FormatRange(candleMetrics.Body, tickSize), rect);
							break;
						case 13: // Body percent
							if (!hasCandleMetrics || !candleMetrics.HasRangePercentages) break;
							dxRangeBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, candleMetrics.BodyPercent / maxBodyPercent));
							RenderTarget.FillRectangle(rect, dxRangeBrush);
							if (boxW >= 28) DrawCenteredText(FormatPercent(candleMetrics.BodyPercent), rect);
							break;
						case 14: // Close location
							if (!hasCandleMetrics || !candleMetrics.HasRangePercentages) break;
							var closeLocationBrush = candleMetrics.CloseLocationPercent >= 50 ? dxPositiveBrush : dxNegativeBrush;
							closeLocationBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, Math.Abs(candleMetrics.CloseLocationPercent - 50.0) / 50.0));
							RenderTarget.FillRectangle(rect, closeLocationBrush);
							if (boxW >= 28) DrawCenteredText(FormatPercent(candleMetrics.CloseLocationPercent), rect);
							break;
						case 15: // Upper wick percent
							if (!hasCandleMetrics || !candleMetrics.HasRangePercentages) break;
							dxNegativeBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, candleMetrics.UpperWickPercent / maxUpperWickPercent));
							RenderTarget.FillRectangle(rect, dxNegativeBrush);
							if (boxW >= 28) DrawCenteredText(FormatPercent(candleMetrics.UpperWickPercent), rect);
							break;
						case 16: // Lower wick percent
							if (!hasCandleMetrics || !candleMetrics.HasRangePercentages) break;
							dxPositiveBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, candleMetrics.LowerWickPercent / maxLowerWickPercent));
							RenderTarget.FillRectangle(rect, dxPositiveBrush);
							if (boxW >= 28) DrawCenteredText(FormatPercent(candleMetrics.LowerWickPercent), rect);
							break;
						case 4: // Time — bar duration formatted as "Xm Y" or "Xs"
							dxTimeBrush.Opacity = (float)BaseOpacity;
							RenderTarget.FillRectangle(rect, dxTimeBrush);
							if (boxW >= 28)
							{
								int durationSecs = GetBarDurationSeconds(i);
								DrawCenteredText(FormatDuration(durationSecs), rect);
							}
							break;
						case 5: // Max Delta
							if (!hasDelta) break;
							double maxDelta = barMaxDelta[i];
							dxMaxDeltaBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, Math.Abs(maxDelta) / maxDel));
							RenderTarget.FillRectangle(rect, dxMaxDeltaBrush);
							if (boxW >= 20) DrawCenteredText(FormatSignedDelta(maxDelta), rect);
							break;
						case 6: // Min Delta
							if (!hasDelta) break;
							double minDelta = barMinDelta[i];
							dxMinDeltaBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, Math.Abs(minDelta) / maxDel));
							RenderTarget.FillRectangle(rect, dxMinDeltaBrush);
							if (boxW >= 20) DrawCenteredText(FormatSignedDelta(minDelta), rect);
							break;
						case 7: // Cumulative Delta
							if (cumulativeDeltaValues == null || i >= cumulativeDeltaValues.Length) break;
							double cumulativeDelta = cumulativeDeltaValues[i];
							var cBrush = cumulativeDelta >= 0 ? dxPositiveBrush : dxNegativeBrush;
							cBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, Math.Abs(cumulativeDelta) / maxCumDel));
							RenderTarget.FillRectangle(rect, cBrush);
							if (boxW >= 20) DrawCenteredText(FormatSignedDelta(cumulativeDelta), rect);
							break;
						case 8: // Delta Percent
							if (!hasDelta) break;
							double deltaPercent = CalculateDeltaPercent(del, vol);
							var pctBrush = deltaPercent >= 0 ? dxPositiveBrush : dxNegativeBrush;
							pctBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, Math.Abs(deltaPercent) / maxDeltaPercent));
							RenderTarget.FillRectangle(rect, pctBrush);
							if (boxW >= 28) DrawCenteredText(FormatSignedPercent(deltaPercent), rect);
							break;
					}

					if (ShowCellSeparators)
					{
						DrawCellSeparator(
							rect,
							i == firstVisibleIdx,
							i == lastVisibleIdx,
							true,
							r == rows.Count - 1);
					}
				}
			}

			float averageLeft, averageWidth;
			if (ShowAverageValues && averageSummary != null && TryGetAverageColumnLayout(chartControl, fromIdx, toIdx, lastVisibleIdx, out averageLeft, out averageWidth))
				DrawAverageColumn(rows, averageSummary, averageLeft, averageWidth, panelY, rowH, tickSize);

			for (int r = 0; r < rows.Count; r++)
				DrawRightLabel(rows[r].Key, ChartPanel.X + ChartPanel.W - 5f, panelY + r * rowH, rowH);

			DrawRightScaleMask(chartControl);

			RenderTarget.AntialiasMode = oldAA;
			RenderTarget.TextAntialiasMode = oldTAA;
            }
            finally
            {
                OrcaDiagnosticsCore.ReportRenderSample(diagnosticsInstanceId, diagnosticsRenderStart);
            }
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

			RebuildDeltaFromOrderFlowSnapshot(snapshot);
            OrcaDiagnosticsCore.ReportCacheStatus(diagnosticsInstanceId, "SharedOrcaProfileDataProvider", "loaded " + snapshot.SourceName + " revision=" + snapshot.Revision);
            OrcaDiagnosticsCore.ReportModelUpdate(diagnosticsInstanceId, GetDiagnosticsEventTime(), null);
			providerRevision = snapshot.Revision;
			providerCurrentBar = CurrentBar;
			providerDataActive = true;
			System.Threading.Interlocked.Exchange(ref providerOutageWarningLogged, 0);
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
			Print("OrcaTimeStatistics: loaded shared historical backfill from " + snapshot.SourceName + " buckets=" + bucketCount + " range=" + firstTime + "-" + lastTime + " revision=" + snapshot.Revision + " bidAsk=" + bidAskPct + " fallback=" + fallbackPct + " unclassified=" + unclassifiedPct);
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
			// One Output warning per continuous outage; diagnostics retain the current reason.
			// Do not use message equality: stale-age text changes while the outage is unchanged.
			if (System.Threading.Interlocked.Exchange(ref providerOutageWarningLogged, 1) != 0)
				return;

			Print("OrcaTimeStatistics: " + message);
		}

		private DateTime GetProviderFromTime()
		{
			DateTime firstBarTime;
			if (!TryGetBarTime(0, out firstBarTime))
				return DateTime.MinValue;

			return firstBarTime.AddDays(-1);
		}

		private DateTime GetProviderToTime()
		{
			DateTime toTime = DateTime.Now;
			DateTime currentBarTime;
			if (CurrentBar >= 0 && TryGetBarTime(CurrentBar, out currentBarTime))
			{
				if (currentBarTime > toTime)
					toTime = currentBarTime;
			}

			return toTime.AddSeconds(2);
		}

		private void RebuildDeltaFromOrderFlowSnapshot(OrcaOrderFlowDataSnapshot snapshot)
		{
			if (snapshot == null || snapshot.Buckets == null || Bars == null || CurrentBar < 0)
				return;

			EnsureBarLists(CurrentBar);
			ClearDeltaLists(CurrentBar);

			for (int index = 0; index < snapshot.Buckets.Count; index++)
			{
				OrcaOrderFlowBucket bucket = snapshot.Buckets[index];
				if (bucket == null)
					continue;

				int primaryIdx = Bars.GetBar(bucket.Time);
				if (primaryIdx < 0 || primaryIdx > CurrentBar)
					continue;

				EnsureBarLists(primaryIdx);
				double barBefore = barTickDelta[primaryIdx];
				barTickDelta[primaryIdx] += bucket.Delta;

				double bucketMax = barBefore + bucket.MaxDelta;
				double bucketMin = barBefore + bucket.MinDelta;
				if (bucketMax > barMaxDelta[primaryIdx])
					barMaxDelta[primaryIdx] = bucketMax;
				if (bucketMin < barMinDelta[primaryIdx])
					barMinDelta[primaryIdx] = bucketMin;

				barHasData[primaryIdx] = true;
			}
		}

		private void ClearDeltaLists(int lastIndex)
		{
			EnsureDeltaStorage();
			int count = Math.Min(lastIndex, barTickDelta.Count - 1);
			for (int index = 0; index <= count; index++)
			{
				if (index < barTickDelta.Count)
					barTickDelta[index] = 0;
				if (index < barMaxDelta.Count)
					barMaxDelta[index] = 0;
				if (index < barMinDelta.Count)
					barMinDelta[index] = 0;
				if (index < barHasData.Count)
					barHasData[index] = false;
			}
		}

		private AverageSummary CalculateAverageSummary(int lastVisibleIdx, double tickSize)
		{
			AverageSummary summary = new AverageSummary();
			if (Bars == null || AverageLookbackBars <= 0 || CurrentBar < 0 || lastVisibleIdx < 0)
				return summary;

			int lastIndex = Math.Min(Math.Min(lastVisibleIdx, CurrentBar), Bars.Count - 1);
			if (State == State.Realtime && lastIndex == CurrentBar && lastIndex > 0)
				lastIndex--;
			if (lastIndex < 0)
				return summary;

			int lookback = Math.Max(1, AverageLookbackBars);
			int firstIndex = Math.Max(0, lastIndex - lookback + 1);
			double[] cumulativeDeltaValues = ShowCumulativeDelta ? BuildCumulativeDeltaValues(lastIndex) : null;
			for (int index = firstIndex; index <= lastIndex; index++)
			{
				double volume, high, low, range;
				if (!TryGetBarStats(index, out volume, out high, out low, out range))
					continue;

				if (!double.IsNaN(volume) && !double.IsInfinity(volume))
				{
					summary.VolumeSum += volume;
					summary.VolumeCount++;
				}

				double volumePerSecond;
				if (ShowVolumePerSecond && TryCalculateVolumePerSecond(index, volume, out volumePerSecond))
				{
					summary.VolumePerSecondSum += volumePerSecond;
					summary.VolumePerSecondCount++;
				}

				double volumePerRangeTick;
				if (ShowVolumePerRangeTick && TryCalculateVolumePerRangeTick(volume, range, tickSize, out volumePerRangeTick))
				{
					summary.VolumePerRangeTickSum += volumePerRangeTick;
					summary.VolumePerRangeTickCount++;
				}

				CandleMetrics candleMetrics = new CandleMetrics();
				if ((ShowBody || ShowBodyPercent || ShowCloseLocation || ShowUpperWick || ShowLowerWick) && TryGetCandleMetrics(index, high, low, out candleMetrics))
				{
					if (ShowBody)
					{
						summary.BodySum += candleMetrics.Body;
						summary.BodyCount++;
					}
					if (candleMetrics.HasRangePercentages)
					{
						if (ShowBodyPercent) { summary.BodyPercentSum += candleMetrics.BodyPercent; summary.BodyPercentCount++; }
						if (ShowCloseLocation) { summary.CloseLocationPercentSum += candleMetrics.CloseLocationPercent; summary.CloseLocationPercentCount++; }
						if (ShowUpperWick) { summary.UpperWickPercentSum += candleMetrics.UpperWickPercent; summary.UpperWickPercentCount++; }
						if (ShowLowerWick) { summary.LowerWickPercentSum += candleMetrics.LowerWickPercent; summary.LowerWickPercentCount++; }
					}
				}

				if (!double.IsNaN(range) && !double.IsInfinity(range))
				{
					summary.RangeSum += Math.Max(0, range);
					summary.RangeCount++;
				}

				int durationSeconds = GetBarDurationSeconds(index);
				if (durationSeconds >= 0)
				{
					summary.TimeSecondsSum += durationSeconds;
					summary.TimeCount++;
				}

				if (cumulativeDeltaValues != null && index < cumulativeDeltaValues.Length)
				{
					summary.CumulativeDeltaAbsSum += Math.Abs(cumulativeDeltaValues[index]);
					summary.CumulativeDeltaCount++;
				}

				if (!HasDeltaForBar(index))
					continue;

				summary.DeltaAbsSum += Math.Abs(barTickDelta[index]);
				summary.DeltaCount++;
				double deltaPerSecond;
				if (ShowDeltaPerSecond && TryCalculateDeltaPerSecond(index, barTickDelta[index], out deltaPerSecond))
				{
					summary.DeltaPerSecondAbsSum += Math.Abs(deltaPerSecond);
					summary.DeltaPerSecondCount++;
				}
				if (volume > 0)
				{
					summary.DeltaPercentAbsSum += Math.Abs(CalculateDeltaPercent(barTickDelta[index], volume));
					summary.DeltaPercentCount++;
				}
				summary.MaxDeltaAbsSum += Math.Abs(barMaxDelta[index]);
				summary.MaxDeltaCount++;
				summary.MinDeltaAbsSum += Math.Abs(barMinDelta[index]);
				summary.MinDeltaCount++;
				summary.FinishDeltaAbsSum += Math.Abs(GetFinishDelta(index));
				summary.FinishDeltaCount++;
			}

			return summary;
		}

		private bool TryGetAverageColumnLayout(ChartControl chartControl, int fromIdx, int toIdx, int lastVisibleIdx, out float left, out float width)
		{
			left = 0;
			width = 0;
			if (chartControl == null || ChartBars == null || ChartPanel == null || lastVisibleIdx < 0)
				return false;

			float lastX = chartControl.GetXByBarIndex(ChartBars, lastVisibleIdx);
			float spacing = (float)chartControl.BarWidth;
			if (lastVisibleIdx < toIdx)
				spacing = chartControl.GetXByBarIndex(ChartBars, lastVisibleIdx + 1) - lastX;
			else if (lastVisibleIdx > fromIdx)
				spacing = lastX - chartControl.GetXByBarIndex(ChartBars, lastVisibleIdx - 1);

			spacing = Math.Max(2f, Math.Abs(spacing));
			float lastCellRight = lastX + spacing / 2f;
			const float labelReserve = 62f;
			float rightLimit = ChartPanel.X + ChartPanel.W - labelReserve - 3f;
			if (chartControl.CanvasRight > 0)
				rightLimit = Math.Min(rightLimit, (float)chartControl.CanvasRight - labelReserve - 3f);

			const float gap = 2f;
			float desiredWidth = Math.Max(34f, Math.Min(62f, spacing * 2.6f));
			left = lastCellRight + gap;
			width = Math.Min(desiredWidth, rightLimit - left);
			if (width >= 28f)
				return true;

			width = desiredWidth;
			left = rightLimit - width;
			if (left <= lastCellRight)
			{
				left = lastCellRight + gap;
				width = rightLimit - left;
			}

			return width >= 24f;
		}

		private void DrawAverageColumn(List<KeyValuePair<string, int>> rows, AverageSummary summary, float left, float width, float panelY, float rowH, double tickSize)
		{
			if (rows == null || summary == null || width <= 0)
				return;

			for (int r = 0; r < rows.Count; r++)
			{
				int rowType = rows[r].Value;
				SharpDX.Direct2D1.Brush brush = GetAverageCellBrush(rowType);
				if (rowType == 14 && summary.CloseLocationPercentCount > 0 && summary.CloseLocationPercentSum / summary.CloseLocationPercentCount < 50.0)
					brush = dxNegativeBrush;
				if (brush == null)
					continue;

				RectangleF rect = new RectangleF(left, panelY + r * rowH, width, rowH);
				brush.Opacity = 0.92f;
				RenderTarget.FillRectangle(rect, brush);
				if (width >= 18f)
					DrawCenteredText(BuildAverageCellValue(rowType, summary, tickSize), rect);

				if (ShowCellSeparators)
					DrawCellSeparator(rect, true, true, true, r == rows.Count - 1);
			}
		}

		private SharpDX.Direct2D1.Brush GetAverageCellBrush(int rowType)
		{
			switch (rowType)
			{
				case 0: return dxVolumeBrush;
				case 1: return dxPositiveBrush;
				case 2: return dxFinPosBrush;
				case 3: return dxRangeBrush;
				case 4: return dxTimeBrush;
				case 5: return dxMaxDeltaBrush;
				case 6: return dxMinDeltaBrush;
				case 7: return dxPositiveBrush;
				case 8: return dxPositiveBrush;
				case 9: return dxVolumeBrush;
				case 10: return dxVolumeBrush;
				case 11: return dxPositiveBrush;
				case 12: return dxRangeBrush;
				case 13: return dxRangeBrush;
				case 14: return dxPositiveBrush;
				case 15: return dxNegativeBrush;
				case 16: return dxPositiveBrush;
				default: return dxTextBrush;
			}
		}

		private string BuildAverageCellValue(int rowType, AverageSummary summary, double tickSize)
		{
			if (summary == null)
				return "--";

			switch (rowType)
			{
				case 0:
					return summary.VolumeCount > 0 ? FormatVolume(summary.VolumeSum / summary.VolumeCount) : "--";
				case 9:
					return summary.VolumePerSecondCount > 0 ? FormatVolume(summary.VolumePerSecondSum / summary.VolumePerSecondCount) : "--";
				case 10:
					return summary.VolumePerRangeTickCount > 0 ? FormatVolume(summary.VolumePerRangeTickSum / summary.VolumePerRangeTickCount) : "--";
				case 11:
					return summary.DeltaPerSecondCount > 0 ? FormatVolume(summary.DeltaPerSecondAbsSum / summary.DeltaPerSecondCount) : "--";
				case 12:
					return summary.BodyCount > 0 ? FormatRange(summary.BodySum / summary.BodyCount, tickSize) : "--";
				case 13:
					return summary.BodyPercentCount > 0 ? FormatPercent(summary.BodyPercentSum / summary.BodyPercentCount) : "--";
				case 14:
					return summary.CloseLocationPercentCount > 0 ? FormatPercent(summary.CloseLocationPercentSum / summary.CloseLocationPercentCount) : "--";
				case 15:
					return summary.UpperWickPercentCount > 0 ? FormatPercent(summary.UpperWickPercentSum / summary.UpperWickPercentCount) : "--";
				case 16:
					return summary.LowerWickPercentCount > 0 ? FormatPercent(summary.LowerWickPercentSum / summary.LowerWickPercentCount) : "--";
				case 1:
					return summary.DeltaCount > 0 ? FormatDelta(summary.DeltaAbsSum / summary.DeltaCount) : "--";
				case 7:
					return summary.CumulativeDeltaCount > 0 ? FormatDelta(summary.CumulativeDeltaAbsSum / summary.CumulativeDeltaCount) : "--";
				case 8:
					return summary.DeltaPercentCount > 0 ? FormatPercent(summary.DeltaPercentAbsSum / summary.DeltaPercentCount) : "--";
				case 2:
					return summary.FinishDeltaCount > 0 ? FormatDelta(summary.FinishDeltaAbsSum / summary.FinishDeltaCount) : "--";
				case 3:
					return summary.RangeCount > 0 ? FormatRange(summary.RangeSum / summary.RangeCount, tickSize) : "--";
				case 4:
					return summary.TimeCount > 0 ? FormatDuration((int)Math.Round(summary.TimeSecondsSum / summary.TimeCount)) : "--";
				case 5:
					return summary.MaxDeltaCount > 0 ? FormatDelta(summary.MaxDeltaAbsSum / summary.MaxDeltaCount) : "--";
				case 6:
					return summary.MinDeltaCount > 0 ? FormatDelta(summary.MinDeltaAbsSum / summary.MinDeltaCount) : "--";
				default:
					return "--";
			}
		}

		private string FormatVolume(double vol) { return vol >= 1000 ? (vol / 1000.0).ToString("0.##") + "K" : vol.ToString("0.##"); }
		private string FormatDelta(double delta) { return delta.ToString("#,##0"); }
		private string FormatSignedRate(double rate)
		{
			double absolute = Math.Abs(rate);
			string value = absolute >= 1000 ? (absolute / 1000.0).ToString("0.##") + "K" : absolute.ToString("0.##");
			return rate > 0 ? "+" + value : rate < 0 ? "-" + value : "0";
		}
		private string FormatSignedDelta(double delta) { return delta.ToString("+#,##0;-#,##0;0"); }
		private string FormatPercent(double percent) { return percent.ToString("#,##0.#") + "%"; }
		private string FormatSignedPercent(double percent) { return percent.ToString("+#,##0.#;-#,##0.#;0") + "%"; }
		private string FormatRange(double range, double tickSize)
		{
			if (tickSize <= 0) return range.ToString("0.########");
			double rounded = Math.Round(range / tickSize, MidpointRounding.AwayFromZero) * tickSize;
			return rounded.ToString("0.########");
		}
		private double CalculateDeltaPercent(double delta, double volume)
		{
			if (volume <= 0 || double.IsNaN(volume) || double.IsInfinity(volume))
				return 0;

			return (delta / volume) * 100.0;
		}
		private bool TryCalculateVolumePerSecond(int barIndex, double volume, out double volumePerSecond)
		{
			volumePerSecond = 0;
			if (volume < 0 || double.IsNaN(volume) || double.IsInfinity(volume))
				return false;

			int durationSeconds = GetBarDurationSeconds(barIndex);
			if (durationSeconds <= 0)
				return false;

			volumePerSecond = volume / durationSeconds;
			return !double.IsNaN(volumePerSecond) && !double.IsInfinity(volumePerSecond);
		}
		private bool TryCalculateVolumePerRangeTick(double volume, double range, double tickSize, out double volumePerRangeTick)
		{
			volumePerRangeTick = 0;
			if (volume < 0 || range <= 0 || tickSize <= 0 || double.IsNaN(volume) || double.IsInfinity(volume))
				return false;

			double rangeTicks = range / tickSize;
			if (rangeTicks <= 0 || double.IsNaN(rangeTicks) || double.IsInfinity(rangeTicks))
				return false;

			volumePerRangeTick = volume / rangeTicks;
			return !double.IsNaN(volumePerRangeTick) && !double.IsInfinity(volumePerRangeTick);
		}
		private bool TryCalculateDeltaPerSecond(int barIndex, double delta, out double deltaPerSecond)
		{
			deltaPerSecond = 0;
			if (double.IsNaN(delta) || double.IsInfinity(delta))
				return false;

			int durationSeconds = GetBarDurationSeconds(barIndex);
			if (durationSeconds <= 0)
				return false;

			deltaPerSecond = delta / durationSeconds;
			return !double.IsNaN(deltaPerSecond) && !double.IsInfinity(deltaPerSecond);
		}
		private double[] BuildCumulativeDeltaValues(int lastIndex)
		{
			if (lastIndex < 0)
				return null;

			int count = lastIndex + 1;
			double[] values = new double[count];
			double running = 0;
			DateTime activeResetStart = DateTime.MinValue;
			for (int index = 0; index < count; index++)
			{
				DateTime barTime;
				if (!TryGetBarTime(index, out barTime))
				{
					values[index] = running;
					continue;
				}

				DateTime resetStart = GetCumulativeDeltaResetStart(barTime);
				if (resetStart != activeResetStart)
				{
					activeResetStart = resetStart;
					running = 0;
				}

				if (barTime >= activeResetStart && HasDeltaForBar(index))
					running += barTickDelta[index];
				values[index] = running;
			}

			return values;
		}
		private DateTime GetCumulativeDeltaResetStart(DateTime barTime)
		{
			switch (CumulativeDeltaStartMode)
			{
				case OrcaTimeStatisticsCumulativeDeltaStartMode.OneDayRth:
					return GetRthStart(barTime);
				case OrcaTimeStatisticsCumulativeDeltaStartMode.WeekSundaySixPmEastern:
					return GetWeekSundaySixPmStart(barTime);
				case OrcaTimeStatisticsCumulativeDeltaStartMode.OneDaySixPmEastern:
				default:
					return GetDailySixPmStart(barTime);
			}
		}
		private DateTime GetDailySixPmStart(DateTime barTime)
		{
			DateTime start = barTime.Date.AddHours(18);
			return barTime >= start ? start : start.AddDays(-1);
		}
		private DateTime GetRthStart(DateTime barTime)
		{
			DateTime start = barTime.Date.AddHours(9).AddMinutes(30);
			if (barTime > start)
				return start;

			DateTime previousDate = GetPreviousWeekday(barTime.Date.AddDays(-1));
			return previousDate.AddHours(9).AddMinutes(30);
		}
		private DateTime GetWeekSundaySixPmStart(DateTime barTime)
		{
			DateTime start = barTime.Date.AddDays(-(int)barTime.DayOfWeek).AddHours(18);
			return barTime >= start ? start : start.AddDays(-7);
		}
		private DateTime GetPreviousWeekday(DateTime date)
		{
			while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
				date = date.AddDays(-1);

			return date;
		}
		private double GetFinishDelta(int barIndex)
		{
			if (!HasDeltaForBar(barIndex))
				return 0;

			double curDel = barTickDelta[barIndex];
			double extreme = (curDel >= 0) ? barMaxDelta[barIndex] : barMinDelta[barIndex];
			return curDel - extreme;
		}
		private bool TryGetBarStats(int barIndex, out double volume, out double high, out double low, out double range)
		{
			volume = 0;
			high = 0;
			low = 0;
			range = 0;
			try
			{
				if (Bars == null || barIndex < 0 || barIndex >= Bars.Count)
					return false;

				volume = Bars.GetVolume(barIndex);
				high = Bars.GetHigh(barIndex);
				low = Bars.GetLow(barIndex);
				if (double.IsNaN(high) || double.IsInfinity(high) || double.IsNaN(low) || double.IsInfinity(low))
					return false;

				range = Math.Max(0, high - low);
				return true;
			}
			catch
			{
				return false;
			}
		}
		private bool TryGetCandleMetrics(int barIndex, double high, double low, out CandleMetrics metrics)
		{
			metrics = new CandleMetrics();
			try
			{
				if (Bars == null || barIndex < 0 || barIndex >= Bars.Count)
					return false;

				double open = Bars.GetOpen(barIndex);
				double close = Bars.GetClose(barIndex);
				if (double.IsNaN(open) || double.IsInfinity(open) || double.IsNaN(close) || double.IsInfinity(close))
					return false;

				double range = Math.Max(0, high - low);
				metrics.Body = Math.Min(range, Math.Abs(close - open));
				if (range <= 0)
					return true;

				metrics.HasRangePercentages = true;
				metrics.BodyPercent = Math.Max(0, Math.Min(100, metrics.Body / range * 100.0));
				metrics.CloseLocationPercent = Math.Max(0, Math.Min(100, (close - low) / range * 100.0));
				metrics.UpperWickPercent = Math.Max(0, Math.Min(100, (high - Math.Max(open, close)) / range * 100.0));
				metrics.LowerWickPercent = Math.Max(0, Math.Min(100, (Math.Min(open, close) - low) / range * 100.0));
				return true;
			}
			catch
			{
				return false;
			}
		}
		private int GetBarDurationSeconds(int barIndex)
		{
			try
			{
				if (Bars == null || barIndex <= 0 || barIndex >= Bars.Count)
					return 0;

				DateTime startTime;
				if (!TryGetBarTime(barIndex - 1, out startTime))
					return 0;

				DateTime endTime;
				if (barIndex == Bars.Count - 1)
					endTime = DateTime.Now;
				else if (!TryGetBarTime(barIndex, out endTime))
					return 0;

				return (int)Math.Max(0, Math.Abs((endTime - startTime).TotalSeconds));
			}
			catch
			{
				return 0;
			}
		}
		private bool TryGetBarTime(int barIndex, out DateTime barTime)
		{
			barTime = DateTime.MinValue;
			try
			{
				if (Bars == null || barIndex < 0 || barIndex >= Bars.Count)
					return false;

				barTime = Bars.GetTime(barIndex);
				return barTime != DateTime.MinValue;
			}
			catch
			{
				return false;
			}
		}
		private string FormatDuration(int totalSecs)
		{
			if (totalSecs < 60) return totalSecs + "s";
			int m = totalSecs / 60, s = totalSecs % 60;
			return s > 0 ? m + "m " + s : m + "m";
		}

		private void DrawCenteredText(string text, RectangleF rect)
		{
			if (dxTextFormat == null || dxTextBrush == null) return;
			using (var layout = new SharpDX.DirectWrite.TextLayout(dwFactory, text, dxTextFormat, rect.Width, rect.Height))
			{
				layout.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				layout.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
				RenderTarget.DrawTextLayout(new Vector2(rect.X, rect.Y), layout, dxTextBrush);
			}
		}

		private void DrawRightLabel(string text, float x, float y, float h)
		{
			if (dxTextFormat == null || dxTextBrush == null) return;
			const float labelWidth = 115f;
			using (var layout = new SharpDX.DirectWrite.TextLayout(dwFactory, text, dxTextFormat, labelWidth, h))
			{
				layout.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				layout.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
				RenderTarget.DrawTextLayout(new Vector2(x - labelWidth, y), layout, dxTextBrush);
			}
		}

		private void DrawRightScaleMask(ChartControl chartControl)
		{
			if (chartControl == null || ChartPanel == null || dxScaleMaskBrush == null)
				return;

			float panelRight = ChartPanel.X + ChartPanel.W;
			float canvasRight = chartControl.CanvasRight > panelRight ? (float)chartControl.CanvasRight : panelRight;
			if (canvasRight <= panelRight)
				return;

			float left = Math.Max(ChartPanel.X, panelRight - 1f);
			RenderTarget.FillRectangle(new RectangleF(left, ChartPanel.Y, canvasRight - left, ChartPanel.H), dxScaleMaskBrush);
		}

		private void DrawCellSeparator(RectangleF rect, bool drawLeft, bool drawRight, bool drawTop, bool drawBottom)
		{
			if (dxSeparatorBrush == null || CellSeparatorThickness <= 0) return;
			float thickness = Math.Max(0.1f, CellSeparatorThickness);
			float left = rect.X;
			float top = rect.Y;
			float right = rect.X + rect.Width;
			float bottom = rect.Y + rect.Height;

			if (drawLeft)
				RenderTarget.DrawLine(new Vector2(left, top), new Vector2(left, bottom), dxSeparatorBrush, thickness);
			RenderTarget.DrawLine(new Vector2(right, top), new Vector2(right, bottom), dxSeparatorBrush, thickness);
			if (drawTop)
				RenderTarget.DrawLine(new Vector2(left, top), new Vector2(right, top), dxSeparatorBrush, thickness);
			if (drawBottom)
				RenderTarget.DrawLine(new Vector2(left, bottom), new Vector2(right, bottom), dxSeparatorBrush, thickness);
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxVolumeBrush != null && dxResourceRenderTarget == currentTarget) return;
			if (dxVolumeBrush != null || dxResourceRenderTarget != IntPtr.Zero)
				DisposeDxResources();
			dxVolumeBrush   = CreateSolidBrush(VolumeColor, 1.0f);
			dxPositiveBrush = CreateSolidBrush(PositiveDeltaColor, 1.0f);
			dxNegativeBrush = CreateSolidBrush(NegativeDeltaColor, 1.0f);
			dxMaxDeltaBrush = CreateSolidBrush(MaxDeltaColor, 1.0f);
			dxMinDeltaBrush = CreateSolidBrush(MinDeltaColor, 1.0f);
			dxFinPosBrush   = CreateSolidBrush(FinishDeltaPosColor, 1.0f);
			dxFinNegBrush   = CreateSolidBrush(FinishDeltaNegColor, 1.0f);
			dxRangeBrush    = CreateSolidBrush(RangeColor, 1.0f);
			dxTimeBrush     = CreateSolidBrush(TimeColor, 1.0f);
			dxTextBrush     = CreateSolidBrush(TextColor, 1.0f);
			dxSeparatorBrush = CreateSolidBrush(CellSeparatorColor, 1.0f);
			dxScaleMaskBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0f, 0f, 0f, 1f));
			dwFactory    = new SharpDX.DirectWrite.Factory();
			SharpDX.DirectWrite.FontWeight textFontWeight = ResolveTextFontWeight();
			try {
				dxTextFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, GetTextFontFamily(), textFontWeight, SharpDX.DirectWrite.FontStyle.Normal, (float)FontSize);
			} catch {
				dxTextFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Segoe UI", textFontWeight, SharpDX.DirectWrite.FontStyle.Normal, (float)FontSize);
			}
			dxResourceRenderTarget = currentTarget;
		}

		private string GetTextFontFamily()
		{
			return string.IsNullOrWhiteSpace(FontFamilyName) ? "Segoe UI" : FontFamilyName.Trim();
		}

		private SharpDX.DirectWrite.FontWeight ResolveTextFontWeight()
		{
			switch (TextFontWeight)
			{
				case OrcaTimeStatisticsFontWeight.Light:
					return SharpDX.DirectWrite.FontWeight.Light;
				case OrcaTimeStatisticsFontWeight.Regular:
					return SharpDX.DirectWrite.FontWeight.Normal;
				case OrcaTimeStatisticsFontWeight.Medium:
					return SharpDX.DirectWrite.FontWeight.Medium;
				case OrcaTimeStatisticsFontWeight.SemiBold:
					return SharpDX.DirectWrite.FontWeight.SemiBold;
				case OrcaTimeStatisticsFontWeight.ExtraBold:
					return SharpDX.DirectWrite.FontWeight.ExtraBold;
				default:
					return SharpDX.DirectWrite.FontWeight.Bold;
			}
		}

		private SharpDX.Direct2D1.Brush CreateSolidBrush(System.Windows.Media.Brush wpfBrush, float opacity)
		{
			var solidBrush = wpfBrush as System.Windows.Media.SolidColorBrush;
			var color = solidBrush != null ? solidBrush.Color : System.Windows.Media.Colors.White;
			return new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(color.R / 255f, color.G / 255f, color.B / 255f, (color.A / 255f) * opacity));
		}

		private void DisposeDxResources()
		{
			if (dxVolumeBrush != null)   { dxVolumeBrush.Dispose();   dxVolumeBrush = null; }
			if (dxPositiveBrush != null) { dxPositiveBrush.Dispose(); dxPositiveBrush = null; }
			if (dxNegativeBrush != null) { dxNegativeBrush.Dispose(); dxNegativeBrush = null; }
			if (dxMaxDeltaBrush != null) { dxMaxDeltaBrush.Dispose(); dxMaxDeltaBrush = null; }
			if (dxMinDeltaBrush != null) { dxMinDeltaBrush.Dispose(); dxMinDeltaBrush = null; }
			if (dxFinPosBrush != null)   { dxFinPosBrush.Dispose();   dxFinPosBrush = null; }
			if (dxFinNegBrush != null)   { dxFinNegBrush.Dispose();   dxFinNegBrush = null; }
			if (dxRangeBrush != null)      { dxRangeBrush.Dispose();      dxRangeBrush = null; }
			if (dxTimeBrush != null)       { dxTimeBrush.Dispose();       dxTimeBrush = null; }
			if (dxTextBrush != null)       { dxTextBrush.Dispose();       dxTextBrush = null; }
			if (dxSeparatorBrush != null)  { dxSeparatorBrush.Dispose();  dxSeparatorBrush = null; }
			if (dxScaleMaskBrush != null)  { dxScaleMaskBrush.Dispose();  dxScaleMaskBrush = null; }
			if (dxTextFormat != null)      { dxTextFormat.Dispose();      dxTextFormat = null; }
			if (dwFactory != null)         { dwFactory.Dispose();         dwFactory = null; }
			dxResourceRenderTarget = IntPtr.Zero;
		}

		public override void OnRenderTargetChanged() { DisposeDxResources(); base.OnRenderTargetChanged(); }

		string IOrcaReplayParticipant.ReplayParticipantId { get { return replayBarHorizon == null ? "OrcaTimeStatistics:" + diagnosticsInstanceId : replayBarHorizon.ParticipantId; } }
		OrcaReplayCapabilities IOrcaReplayParticipant.ReplayCapabilities { get { return replayBarHorizon == null ? new OrcaReplayCapabilities(true, false, true, true, OrcaReplayChartStyleSupport.AllV1) : replayBarHorizon.Capabilities; } }
		OrcaReplayCheckpoint IOrcaReplayParticipant.CaptureReplayCheckpoint(OrcaReplayContext context) { return replayBarHorizon.Capture(context); }
		void IOrcaReplayParticipant.PrepareReplay(OrcaReplayContext context, OrcaReplayCheckpoint checkpoint) { replayBarHorizon.Prepare(context, checkpoint); }
		void IOrcaReplayParticipant.ApplyReplayEvent(OrcaReplayContext context, OrcaReplayTradeEvent tradeEvent) { }
		void IOrcaReplayParticipant.ApplyReplayBar(OrcaReplayContext context, int primaryBarIndex) { replayBarHorizon.ApplyBar(primaryBarIndex); }
		void IOrcaReplayParticipant.PublishReplaySnapshot(OrcaReplayContext context) { }
		void IOrcaReplayParticipant.RestoreLiveState() { if (replayBarHorizon != null) replayBarHorizon.Restore(); }

		[Display(Name = "Order Flow Source", Order = 0, GroupName = "Data",
			Description = "Internal keeps the existing local market-data calculation. SharedProvider reads only OrcaProfileDataProvider. SharedHistoricalInternalRealtime backfills from the provider, then uses local real-time market data.")]
		public OrcaOrderFlowSourceMode OrderFlowSourceMode { get; set; }

		[Display(Name = "Show Volume",           Order = 1, GroupName = "Rows")]
		public bool ShowVolume { get; set; }

		[Display(Name = "Show Volume Per Second", Order = 2, GroupName = "Rows",
			Description = "Shows bar volume divided by the bar's elapsed seconds.")]
		public bool ShowVolumePerSecond { get; set; }

		[Display(Name = "Show Volume Per Range Tick", Order = 3, GroupName = "Rows",
			Description = "Shows bar volume divided by the number of price ticks in the bar range.")]
		public bool ShowVolumePerRangeTick { get; set; }

		[Display(Name = "Show Delta",            Order = 4, GroupName = "Rows")]
		public bool ShowDelta { get; set; }

		[Display(Name = "Show Delta Per Second", Order = 5, GroupName = "Rows",
			Description = "Shows signed bar delta divided by the bar's elapsed seconds.")]
		public bool ShowDeltaPerSecond { get; set; }

		[Display(Name = "Show Delta Percent", Order = 6, GroupName = "Rows",
			Description = "Shows bar delta divided by bar volume as a signed percent.")]
		public bool ShowDeltaPercent { get; set; }

		[Display(Name = "Show Max Delta", Order = 7, GroupName = "Rows")]
		public bool ShowMaxDelta { get; set; }

		[Display(Name = "Show Min Delta", Order = 8, GroupName = "Rows")]
		public bool ShowMinDelta { get; set; }

		[Display(Name = "Show Finish Delta", Order = 9, GroupName = "Rows")]
		public bool ShowFinishDelta { get; set; }

		[Display(Name = "Show Cumulative Delta", Order = 10, GroupName = "Rows",
			Description = "Shows running cumulative delta across the loaded chart bars.")]
		public bool ShowCumulativeDelta { get; set; }

		[TypeConverter(typeof(OrcaTimeStatisticsCumulativeDeltaStartModeConverter))]
		[Display(Name = "Cumulative Delta Start", Order = 11, GroupName = "Rows",
			Description = "Controls where the cumulative delta row resets.")]
		public OrcaTimeStatisticsCumulativeDeltaStartMode CumulativeDeltaStartMode { get; set; }

		[Display(Name = "Show Range",            Order = 12, GroupName = "Rows")]
		public bool ShowRange { get; set; }

		[Display(Name = "Show Body", Order = 13, GroupName = "Rows",
			Description = "Shows the absolute candle body in the same price units as Range.")]
		public bool ShowBody { get; set; }

		[Display(Name = "Show Body Percent", Order = 14, GroupName = "Rows",
			Description = "Shows absolute candle body divided by total range as a percent.")]
		public bool ShowBodyPercent { get; set; }

		[Display(Name = "Show Close Location", Order = 15, GroupName = "Rows",
			Description = "Shows where the close sits inside the bar range: 0% at the low and 100% at the high.")]
		public bool ShowCloseLocation { get; set; }

		[Display(Name = "Show Upper Wick", Order = 16, GroupName = "Rows",
			Description = "Shows the upper wick as a percent of the total bar range.")]
		public bool ShowUpperWick { get; set; }

		[Display(Name = "Show Lower Wick", Order = 17, GroupName = "Rows",
			Description = "Shows the lower wick as a percent of the total bar range.")]
		public bool ShowLowerWick { get; set; }

		[Display(Name = "Show Time",             Order = 18, GroupName = "Rows")]
		public bool ShowTime { get; set; }

		[Display(Name = "Show Averages", Order = 1, GroupName = "Averages",
			Description = "Shows average values as one extra cell to the right of the visible row cells.")]
		public bool ShowAverageValues { get; set; }

		[Range(1, 500)]
		[Display(Name = "Average Lookback Bars", Order = 2, GroupName = "Averages",
			Description = "Number of completed bars used for the right-side average values.")]
		public int AverageLookbackBars { get; set; }

		[XmlIgnore]
		[Display(Name = "1. Volume Color", Order = 1, GroupName = "Visual")]
		public System.Windows.Media.Brush VolumeColor { get; set; }
		[Browsable(false)]
		public string VolumeColorSerialize { get { return Serialize.BrushToString(VolumeColor); } set { VolumeColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "2. Positive Delta Color", Order = 2, GroupName = "Visual")]
		public System.Windows.Media.Brush PositiveDeltaColor { get; set; }
		[Browsable(false)]
		public string PositiveDeltaColorSerialize { get { return Serialize.BrushToString(PositiveDeltaColor); } set { PositiveDeltaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "3. Negative Delta Color", Order = 3, GroupName = "Visual")]
		public System.Windows.Media.Brush NegativeDeltaColor { get; set; }
		[Browsable(false)]
		public string NegativeDeltaColorSerialize { get { return Serialize.BrushToString(NegativeDeltaColor); } set { NegativeDeltaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "4. Max Delta Color", Order = 4, GroupName = "Visual")]
		public System.Windows.Media.Brush MaxDeltaColor { get; set; }
		[Browsable(false)]
		public string MaxDeltaColorSerialize { get { return Serialize.BrushToString(MaxDeltaColor); } set { MaxDeltaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "5. Min Delta Color", Order = 5, GroupName = "Visual")]
		public System.Windows.Media.Brush MinDeltaColor { get; set; }
		[Browsable(false)]
		public string MinDeltaColorSerialize { get { return Serialize.BrushToString(MinDeltaColor); } set { MinDeltaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "6. Finish Delta (+) Color", Order = 6, GroupName = "Visual")]
		public System.Windows.Media.Brush FinishDeltaPosColor { get; set; }
		[Browsable(false)]
		public string FinishDeltaPosColorSerialize { get { return Serialize.BrushToString(FinishDeltaPosColor); } set { FinishDeltaPosColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "7. Finish Delta (-) Color", Order = 7, GroupName = "Visual")]
		public System.Windows.Media.Brush FinishDeltaNegColor { get; set; }
		[Browsable(false)]
		public string FinishDeltaNegColorSerialize { get { return Serialize.BrushToString(FinishDeltaNegColor); } set { FinishDeltaNegColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "8. Range Color", Order = 8, GroupName = "Visual")]
		public System.Windows.Media.Brush RangeColor { get; set; }
		[Browsable(false)]
		public string RangeColorSerialize { get { return Serialize.BrushToString(RangeColor); } set { RangeColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "9. Time Color", Order = 9, GroupName = "Visual")]
		public System.Windows.Media.Brush TimeColor { get; set; }
		[Browsable(false)]
		public string TimeColorSerialize { get { return Serialize.BrushToString(TimeColor); } set { TimeColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "10. Text Color", Order = 10, GroupName = "Visual")]
		public System.Windows.Media.Brush TextColor { get; set; }
		[Browsable(false)]
		public string TextColorSerialize { get { return Serialize.BrushToString(TextColor); } set { TextColor = Serialize.StringToBrush(value); } }

		[Display(Name = "11. Show Cell Separators", Order = 11, GroupName = "Visual")]
		public bool ShowCellSeparators { get; set; }

		[XmlIgnore]
		[Display(Name = "12. Cell Separator Color", Order = 12, GroupName = "Visual")]
		public System.Windows.Media.Brush CellSeparatorColor { get; set; }
		[Browsable(false)]
		public string CellSeparatorColorSerialize { get { return Serialize.BrushToString(CellSeparatorColor); } set { CellSeparatorColor = Serialize.StringToBrush(value); } }

		[Range(0.1, 6.0)]
		[Display(Name = "13. Cell Separator Thickness", Order = 13, GroupName = "Visual")]
		public float CellSeparatorThickness { get; set; }

		[Range(0.0, 1.0)]
		[Display(Name = "14. Base Opacity", Order = 14, GroupName = "Visual")]
		public double BaseOpacity { get; set; }

		[TypeConverter(typeof(OrcaInstalledFontFamilyConverter))]
		[Display(Name = "15. Font Family", Order = 15, GroupName = "Visual")]
		public string FontFamilyName { get; set; }

		[Display(Name = "16. Font Weight", Order = 16, GroupName = "Visual")]
		public OrcaTimeStatisticsFontWeight TextFontWeight { get; set; }

		[Range(6, 24)]
		[Display(Name = "17. Font Size", Order = 17, GroupName = "Visual")]
		public int FontSize { get; set; }
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaTimeStatistics[] cacheOrcaTimeStatistics;
		public OrcaTimeStatistics OrcaTimeStatistics()
		{
			return OrcaTimeStatistics(Input);
		}

		public OrcaTimeStatistics OrcaTimeStatistics(ISeries<double> input)
		{
			if (cacheOrcaTimeStatistics != null)
				for (int idx = 0; idx < cacheOrcaTimeStatistics.Length; idx++)
					if (cacheOrcaTimeStatistics[idx] != null &&  cacheOrcaTimeStatistics[idx].EqualsInput(input))
						return cacheOrcaTimeStatistics[idx];
			return CacheIndicator<OrcaTimeStatistics>(new OrcaTimeStatistics(), input, ref cacheOrcaTimeStatistics);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaTimeStatistics OrcaTimeStatistics()
		{
			return indicator.OrcaTimeStatistics(Input);
		}

		public Indicators.OrcaTimeStatistics OrcaTimeStatistics(ISeries<double> input )
		{
			return indicator.OrcaTimeStatistics(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaTimeStatistics OrcaTimeStatistics()
		{
			return indicator.OrcaTimeStatistics(Input);
		}

		public Indicators.OrcaTimeStatistics OrcaTimeStatistics(ISeries<double> input )
		{
			return indicator.OrcaTimeStatistics(input);
		}
	}
}

#endregion
