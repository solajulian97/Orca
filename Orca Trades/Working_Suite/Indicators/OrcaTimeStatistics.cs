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

	public class OrcaTimeStatistics : Indicator
	{
		private sealed class AverageSummary
		{
			public double VolumeSum;
			public int VolumeCount;
			public double DeltaAbsSum;
			public int DeltaCount;
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
		private DateTime lastProviderWarningUtc = DateTime.MinValue;
		private DateTime lastBarUpdateWarningUtc = DateTime.MinValue;
		private DateTime lastSharedBackfillAttemptUtc = DateTime.MinValue;
		private DateTime lastSharedBackfillSuccessLogUtc = DateTime.MinValue;
		private const int SharedProviderMaxRealtimeLagSeconds = 30;

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
				Description					= "Displays per-bar statistics (Volume, Delta, Delta Efficiency, Range, Time) as a panel below the chart.";
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
				FontSize             = 11;
				ShowCellSeparators   = true;
				CellSeparatorThickness = 1f;
				ShowAverageValues    = true;
				AverageLookbackBars  = 14;

				ShowVolume           = true;
				ShowDelta            = true;
				ShowCumulativeDelta  = false;
				CumulativeDeltaStartMode = OrcaTimeStatisticsCumulativeDeltaStartMode.OneDaySixPmEastern;
				ShowDeltaPercent     = false;
				ShowMaxDelta         = false;
				ShowMinDelta         = false;
				ShowFinishDelta      = true;
				ShowRange            = true;
				ShowTime             = true;
				OrderFlowSourceMode  = OrcaOrderFlowSourceMode.Internal;

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
			}
			else if (State == State.Terminated)
			{
				DisposeDxResources();
			}
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
					int primaryIdx = BarsArray[0].GetBar(e.Time);
					if (primaryIdx >= 0)
					{
						EnsureBarLists(primaryIdx);
						barTickDelta[primaryIdx] += signed;
						barMaxDelta[primaryIdx] = Math.Max(barMaxDelta[primaryIdx], barTickDelta[primaryIdx]);
						barMinDelta[primaryIdx] = Math.Min(barMinDelta[primaryIdx], barTickDelta[primaryIdx]);
						barHasData[primaryIdx] = true;
					}
				}
			}
		}

		protected override void OnBarUpdate()
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
			if (chartControl == null || chartScale == null || Bars == null || ChartBars == null || ChartPanel == null || Instrument == null || Instrument.MasterInstrument == null) return;
			EnsureDeltaStorage();
			if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedProvider && State == State.Realtime)
				TryRefreshFromSharedProvider(false, false, true);
			else if (OrderFlowSourceMode == OrcaOrderFlowSourceMode.SharedHistoricalInternalRealtime && State == State.Realtime && !providerDataActive && ShouldAttemptRealtimeSharedBackfill())
				TryRefreshFromSharedProvider(false, true, false);

			int rowCount = (ShowVolume ? 1 : 0) + (ShowDelta ? 1 : 0) + (ShowCumulativeDelta ? 1 : 0) + (ShowDeltaPercent ? 1 : 0)
						+ (ShowMaxDelta ? 1 : 0) + (ShowMinDelta ? 1 : 0) + (ShowFinishDelta ? 1 : 0)
						+ (ShowRange ? 1 : 0) + (ShowTime ? 1 : 0);
			if (rowCount == 0) return;

			int fromIdx = Math.Max(0, ChartBars.FromIndex);
			int toIdx   = Math.Min(ChartBars.ToIndex, Bars.Count - 1);
			if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx) return;

			EnsureDxResources();
			if (dxVolumeBrush == null) return;

			float panelY = ChartPanel.Y;
			float panelH = ChartPanel.H;
			float rowH = panelH / rowCount;

			var rows = new List<KeyValuePair<string, int>>();
			if (ShowVolume)      rows.Add(new KeyValuePair<string, int>("Volume",       0));
			if (ShowDelta)       rows.Add(new KeyValuePair<string, int>("Delta",        1));
			if (ShowCumulativeDelta) rows.Add(new KeyValuePair<string, int>("Cumulative \u0394", 7));
			if (ShowDeltaPercent) rows.Add(new KeyValuePair<string, int>("\u0394 %",        8));
			if (ShowMaxDelta)    rows.Add(new KeyValuePair<string, int>("Max \u0394",       5));
			if (ShowMinDelta)    rows.Add(new KeyValuePair<string, int>("Min \u0394",       6));
			if (ShowFinishDelta) rows.Add(new KeyValuePair<string, int>("Finish \u0394",  2));
			if (ShowRange)       rows.Add(new KeyValuePair<string, int>("Range",        3));
			if (ShowTime)        rows.Add(new KeyValuePair<string, int>("Time",         4));

			double maxVol = 1, maxDel = 1, maxCumDel = 1, maxDeltaPercent = 1, maxRange = 1;
			double tickSize = Math.Max(0.00000001, Instrument.MasterInstrument.TickSize);
			double[] cumulativeDeltaValues = ShowCumulativeDelta ? BuildCumulativeDeltaValues(toIdx) : null;

			for (int i = fromIdx; i <= toIdx; i++)
			{
				double vol, range;
				if (!TryGetBarStats(i, out vol, out range)) continue;
				if (ShowVolume) maxVol = Math.Max(maxVol, vol);
				if (ShowRange)  maxRange = Math.Max(maxRange, range);
				if (ShowCumulativeDelta && cumulativeDeltaValues != null && i < cumulativeDeltaValues.Length)
					maxCumDel = Math.Max(maxCumDel, Math.Abs(cumulativeDeltaValues[i]));
				if (HasDeltaForBar(i))
				{
					if (ShowDelta) maxDel = Math.Max(maxDel, Math.Abs(barTickDelta[i]));
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
			AverageSummary averageSummary = ShowAverageValues ? CalculateAverageSummary(lastVisibleIdx) : null;

			for (int i = fromIdx; i <= toIdx; i++)
			{
				double vol, range;
				if (!TryGetBarStats(i, out vol, out range)) continue;
				float x = chartControl.GetXByBarIndex(ChartBars, i);
				float barSpacing = (i < toIdx) ? (chartControl.GetXByBarIndex(ChartBars, i + 1) - x) : ((i > fromIdx) ? (x - chartControl.GetXByBarIndex(ChartBars, i - 1)) : (float)chartControl.BarWidth);
				float boxW = Math.Max(2f, barSpacing);

				bool hasDelta = HasDeltaForBar(i);
				double del   = hasDelta ? barTickDelta[i] : 0;

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
						case 1: // Delta
							if (!hasDelta) break;
							var dBrush = del >= 0 ? dxPositiveBrush : dxNegativeBrush;
							dBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * (Math.Abs(del) / maxDel));
							RenderTarget.FillRectangle(rect, dBrush);
							if (boxW >= 20) DrawCenteredText(FormatDelta(del), rect);
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
			{
				DrawAverageColumn(rows, averageSummary, averageLeft, averageWidth, panelY, rowH, tickSize);
				DrawRowLabels(rows, averageLeft + averageWidth + 4f, ChartPanel.X + ChartPanel.W - averageLeft - averageWidth - 8f, panelY, rowH);
			}
			else
			{
				for (int r = 0; r < rows.Count; r++)
					DrawRightLabel(rows[r].Key, ChartPanel.X + ChartPanel.W - 5f, panelY + r * rowH, rowH);
			}

			DrawRightScaleMask(chartControl);

			RenderTarget.AntialiasMode = oldAA;
			RenderTarget.TextAntialiasMode = oldTAA;
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

		private AverageSummary CalculateAverageSummary(int lastVisibleIdx)
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
				double volume, range;
				if (!TryGetBarStats(index, out volume, out range))
					continue;

				if (!double.IsNaN(volume) && !double.IsInfinity(volume))
				{
					summary.VolumeSum += volume;
					summary.VolumeCount++;
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
			if (barTime >= start)
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
		private bool TryGetBarStats(int barIndex, out double volume, out double range)
		{
			volume = 0;
			range = 0;
			try
			{
				if (Bars == null || barIndex < 0 || barIndex >= Bars.Count)
					return false;

				volume = Bars.GetVolume(barIndex);
				double high = Bars.GetHigh(barIndex);
				double low = Bars.GetLow(barIndex);
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

		private void DrawRowLabels(List<KeyValuePair<string, int>> rows, float left, float width, float panelY, float rowH)
		{
			if (rows == null || dxTextFormat == null || dxTextBrush == null || width < 22f)
				return;

			for (int r = 0; r < rows.Count; r++)
			{
				using (var layout = new SharpDX.DirectWrite.TextLayout(dwFactory, GetCompactRowLabel(rows[r].Value, rows[r].Key), dxTextFormat, width, rowH))
				{
					layout.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
					layout.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
					RenderTarget.DrawTextLayout(new Vector2(left, panelY + r * rowH), layout, dxTextBrush);
				}
			}
		}

		private string GetCompactRowLabel(int rowType, string fallback)
		{
			switch (rowType)
			{
				case 0: return "Vol";
				case 1: return "\u0394";
				case 2: return "F\u0394";
				case 3: return "Rng";
				case 4: return "Time";
				case 5: return "Max\u0394";
				case 6: return "Min\u0394";
				case 7: return "C\u0394";
				case 8: return "\u0394%";
				default: return fallback ?? string.Empty;
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
			try {
				dxTextFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, GetTextFontFamily(), SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)FontSize);
			} catch {
				dxTextFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)FontSize);
			}
			dxResourceRenderTarget = currentTarget;
		}

		private string GetTextFontFamily()
		{
			return string.IsNullOrWhiteSpace(FontFamilyName) ? "Segoe UI" : FontFamilyName.Trim();
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

		[Display(Name = "Order Flow Source", Order = 0, GroupName = "Data",
			Description = "Internal keeps the existing local market-data calculation. SharedProvider reads only OrcaProfileDataProvider. SharedHistoricalInternalRealtime backfills from the provider, then uses local real-time market data.")]
		public OrcaOrderFlowSourceMode OrderFlowSourceMode { get; set; }

		[Display(Name = "Show Volume",           Order = 1, GroupName = "Rows")]
		public bool ShowVolume { get; set; }

		[Display(Name = "Show Delta",            Order = 2, GroupName = "Rows")]
		public bool ShowDelta { get; set; }

		[Display(Name = "Show Cumulative Delta", Order = 3, GroupName = "Rows",
			Description = "Shows running cumulative delta across the loaded chart bars.")]
		public bool ShowCumulativeDelta { get; set; }

		[TypeConverter(typeof(OrcaTimeStatisticsCumulativeDeltaStartModeConverter))]
		[Display(Name = "Cumulative Delta Start", Order = 4, GroupName = "Rows",
			Description = "Controls where the cumulative delta row resets.")]
		public OrcaTimeStatisticsCumulativeDeltaStartMode CumulativeDeltaStartMode { get; set; }

		[Display(Name = "Show Delta Percent", Order = 5, GroupName = "Rows",
			Description = "Shows bar delta divided by bar volume as a signed percent.")]
		public bool ShowDeltaPercent { get; set; }

		[Display(Name = "Show Max Delta", Order = 6, GroupName = "Rows")]
		public bool ShowMaxDelta { get; set; }

		[Display(Name = "Show Min Delta", Order = 7, GroupName = "Rows")]
		public bool ShowMinDelta { get; set; }

		[Display(Name = "Show Finish Delta", Order = 8, GroupName = "Rows")]
		public bool ShowFinishDelta { get; set; }

		[Display(Name = "Show Range",            Order = 9, GroupName = "Rows")]
		public bool ShowRange { get; set; }

		[Display(Name = "Show Time",             Order = 10, GroupName = "Rows")]
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

		[Range(6, 24)]
		[Display(Name = "16. Font Size", Order = 16, GroupName = "Visual")]
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
