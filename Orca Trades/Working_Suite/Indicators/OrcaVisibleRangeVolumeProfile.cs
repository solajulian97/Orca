#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors = System.Windows.Media.Colors;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum VisibleRangeProfileSide
	{
		Left,
		Right
	}

	public enum VisibleRangeRowSizingMode
	{
		RowCount,
		TicksPerRow,
		Dynamic
	}

	public enum VisibleRangeProfileDataMode
	{
		TrueVolumeAtPrice,
		EstimatedFromBars
	}

	public enum VisibleRangeDeltaDirection
	{
		TowardPriceScale,
		TowardCandles
	}

	public enum VisibleRangeVALineStyle
	{
		Solid,
		Dash,
		Dot,
		DashDot
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaVisibleRangeVolumeProfile : Indicator
	{
		private OrcaVolumeProfileResult profileResult;
		private OrcaVolumeProfileResult deltaResult;
		private bool profileDirty;
		private int cachedFromIndex = -1;
		private int cachedToIndex = -1;
		private int cachedBarsCount = -1;
		private int cachedRowCount = -1;
		private int cachedTicksPerRow = -1;
		private int cachedResolvedTicksPerRow = -1;
		private int cachedDynamicRowMinPixels = -1;
		private int cachedDeltaRowCount = -1;
		private int cachedDeltaTicksPerRow = -1;
		private int cachedResolvedDeltaTicksPerRow = -1;
		private int cachedDeltaDynamicRowMinPixels = -1;
		private double cachedValueAreaPercent = double.NaN;
		private double cachedDynamicAggregationMultiplier = double.NaN;
		private double cachedDeltaDynamicAggregationMultiplier = double.NaN;
		private double cachedTickSize = double.NaN;
		private VisibleRangeRowSizingMode cachedRowSizingMode = (VisibleRangeRowSizingMode)(-1);
		private VisibleRangeRowSizingMode cachedDeltaRowSizingMode = (VisibleRangeRowSizingMode)(-1);
		private VisibleRangeProfileDataMode cachedProfileDataMode = (VisibleRangeProfileDataMode)(-1);
		private int cachedDataRevision = -1;
		private int lastSeenCurrentBar = -1;
		private string totalVolumeLabel = string.Empty;

		private List<Dictionary<double, long>> barVolumeMaps;
		private List<Dictionary<double, long>> barUpVolumeMaps;
		private List<Dictionary<double, long>> barDownVolumeMaps;
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double prevLast = double.NaN;
		private int lastDirection;
		private int dataRevision;

		private INotifyPropertyChanged subscribedChartControl;
		private ChartControl subscribedChartControlRef;

		private SharpDX.Direct2D1.SolidColorBrush pocBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush vaFillBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush vaLineBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush upBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush downBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaPositiveBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaNegativeBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaPositiveLabelBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaNegativeLabelBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush textBrushDx;
		private SharpDX.Direct2D1.StrokeStyle vaLineStrokeDx;
		private SharpDX.Direct2D1.SolidColorBrush[] upGradientBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] downGradientBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] vaGradientBrushes;
		private TextFormat textFormatDx;
		private TextFormat deltaLabelTextFormatDx;
		private int lastBuiltGradientSteps = -1;
		private float lastBuiltMinBrightness = -1f;
		private byte lastBuiltOpacity = 0;
		private VisibleRangeVALineStyle lastBuiltVALineStyle = (VisibleRangeVALineStyle)(-1);
		private string lastBrushSignature = string.Empty;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaVisibleRangeVolumeProfile";
				Description = "Visible-range volume profile that recalculates from the bars currently shown in the chart viewport.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DrawOnPricePanel = true;
				DisplayInDataBox = false;
				IsAutoScale = false;
				IsSuspendedWhileInactive = true;
				PaintPriceMarkers = false;
				BarsRequiredToPlot = 0;

				ProfileDataMode = VisibleRangeProfileDataMode.TrueVolumeAtPrice;
				RowCount = 100;
				RowSizingMode = VisibleRangeRowSizingMode.TicksPerRow;
				TicksPerRow = 1;
				DynamicAggregationMultiplier = 1.0;
				DynamicRowMinPixels = 6;
				DeltaRowSizingMode = VisibleRangeRowSizingMode.Dynamic;
				DeltaRowCount = 100;
				DeltaTicksPerRow = 4;
				DeltaDynamicAggregationMultiplier = 1.0;
				DeltaDynamicRowMinPixels = 10;
				ValueAreaPercent = 70;
				ProfileSide = VisibleRangeProfileSide.Right;
				ProfileWidthPercent = 25;
				DeltaWidthPercent = 12;
				DeltaDirection = VisibleRangeDeltaDirection.TowardPriceScale;
				ProfileBarSpacingPx = 0;
				ShowVolume = true;
				ShowDelta = true;
				ShowPOC = true;
				ShowValueArea = true;
				ShowVAColor = true;
				ShowVALines = true;
				ShowVAH = true;
				ShowVAL = true;
				ShowTotalVolume = true;
				ShowDeltaLabels = true;
				DeltaLabelFontSize = 10f;
				VALineThickness = 1.5f;
				VALineStyle = VisibleRangeVALineStyle.Dash;
				UseGradient = true;
				GradientSteps = 16;
				MinBrightness = 0.2f;

				POCColor = WpfBrushes.DodgerBlue;
				VAColor = WpfBrushes.CornflowerBlue;
				ProfileUpColor = WpfBrushes.MediumSeaGreen;
				ProfileDownColor = WpfBrushes.Crimson;
				DeltaPositiveColor = WpfBrushes.SteelBlue;
				DeltaNegativeColor = WpfBrushes.IndianRed;
				DeltaPositiveLabelColor = WpfBrushes.LightGreen;
				DeltaNegativeLabelColor = WpfBrushes.LightCoral;
				Opacity = 180;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				profileResult = new OrcaVolumeProfileResult();
				deltaResult = new OrcaVolumeProfileResult();
				barVolumeMaps = new List<Dictionary<double, long>>(4096);
				barUpVolumeMaps = new List<Dictionary<double, long>>(4096);
				barDownVolumeMaps = new List<Dictionary<double, long>>(4096);
				ResetTradeClassification();
				ResetCache();
			}
			else if (State == State.Terminated)
			{
				DetachChartControl();
				DisposeDxResources();
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
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

			if (CurrentBar != lastSeenCurrentBar)
			{
				lastSeenCurrentBar = CurrentBar;
				EnsureBarMaps(CurrentBar);
				if (Bars != null && Bars.IsFirstBarOfSession)
					ResetTradeClassification();
				profileDirty = true;
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);

			if (chartControl == null || chartScale == null || RenderTarget == null || ChartBars == null || Bars == null)
				return;

			AttachChartControl(chartControl);

			int fromIndex;
			int toIndex;
			if (!TryGetVisibleBarRange(out fromIndex, out toIndex))
				return;

			NinjaTrader.Gui.Chart.ChartPanel panel;
			if (!TryGetRenderPanel(chartControl, chartScale, out panel))
				return;

			int resolvedTicksPerRow = ResolveTicksPerRow(RowSizingMode, TicksPerRow, DynamicRowMinPixels, DynamicAggregationMultiplier, chartScale, panel);
			int resolvedDeltaTicksPerRow = ResolveTicksPerRow(DeltaRowSizingMode, DeltaTicksPerRow, DeltaDynamicRowMinPixels, DeltaDynamicAggregationMultiplier, chartScale, panel);
			if (NeedsRecalculate(fromIndex, toIndex, resolvedTicksPerRow, resolvedDeltaTicksPerRow))
				RecalculateProfile(fromIndex, toIndex, resolvedTicksPerRow, resolvedDeltaTicksPerRow);

			if ((profileResult == null || !profileResult.HasProfile) && (deltaResult == null || !deltaResult.HasProfile))
				return;

			EnsureDxResources();
			if (pocBrushDx == null || vaFillBrushDx == null || vaLineBrushDx == null || upBrushDx == null || downBrushDx == null || deltaPositiveBrushDx == null || deltaNegativeBrushDx == null
				|| (ShowDeltaLabels && (deltaPositiveLabelBrushDx == null || deltaNegativeLabelBrushDx == null || deltaLabelTextFormatDx == null)))
				return;

			SharpDX.Direct2D1.AntialiasMode oldAntialias = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
			try
			{
				DrawProfile(chartScale, panel);
			}
			finally
			{
				RenderTarget.AntialiasMode = oldAntialias;
			}
		}

		private void ResetCache()
		{
			cachedFromIndex = -1;
			cachedToIndex = -1;
			cachedBarsCount = -1;
			cachedRowCount = -1;
			cachedTicksPerRow = -1;
			cachedResolvedTicksPerRow = -1;
			cachedDynamicRowMinPixels = -1;
			cachedDeltaRowCount = -1;
			cachedDeltaTicksPerRow = -1;
			cachedResolvedDeltaTicksPerRow = -1;
			cachedDeltaDynamicRowMinPixels = -1;
			cachedValueAreaPercent = double.NaN;
			cachedDynamicAggregationMultiplier = double.NaN;
			cachedDeltaDynamicAggregationMultiplier = double.NaN;
			cachedTickSize = double.NaN;
			cachedRowSizingMode = (VisibleRangeRowSizingMode)(-1);
			cachedDeltaRowSizingMode = (VisibleRangeRowSizingMode)(-1);
			cachedProfileDataMode = (VisibleRangeProfileDataMode)(-1);
			cachedDataRevision = -1;
			totalVolumeLabel = string.Empty;
			profileDirty = true;
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
			int primaryIndex = BarsArray[0].GetBar(tickTime);
			if (primaryIndex < 0)
				return;

			EnsureBarMaps(primaryIndex);

			double price = NormalizeToTick(Closes[1][0]);
			long volume = (long)Volumes[1][0];
			if (volume <= 0 || double.IsNaN(price) || double.IsInfinity(price))
				return;

			AddToMap(barVolumeMaps[primaryIndex], price, volume);

			long signedVolume = ClassifySignedVolume(price, volume);
			if (signedVolume > 0)
				AddToMap(barUpVolumeMaps[primaryIndex], price, volume);
			else if (signedVolume < 0)
				AddToMap(barDownVolumeMaps[primaryIndex], price, volume);

			dataRevision++;
			if (cachedFromIndex < 0 || (primaryIndex >= cachedFromIndex && primaryIndex <= cachedToIndex))
				profileDirty = true;
		}

		private long ClassifySignedVolume(double price, long volume)
		{
			long signedVolume = 0;
			if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
			{
				if (price >= lastAsk)
					signedVolume = volume;
				else if (price <= lastBid)
					signedVolume = -volume;
				else
					signedVolume = ClassifyByTickDirection(price, volume);
			}
			else if (!double.IsNaN(prevLast))
			{
				signedVolume = ClassifyByTickDirection(price, volume);
			}

			prevLast = price;
			if (signedVolume > 0)
				lastDirection = 1;
			else if (signedVolume < 0)
				lastDirection = -1;

			return signedVolume;
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

		private bool TryGetVisibleBarRange(out int fromIndex, out int toIndex)
		{
			fromIndex = -1;
			toIndex = -1;
			if (ChartBars == null || Bars == null || Bars.Count <= 0)
				return false;

			fromIndex = Math.Max(0, ChartBars.FromIndex);
			toIndex = Math.Min(ChartBars.ToIndex, Bars.Count - 1);
			return fromIndex >= 0 && toIndex >= fromIndex;
		}

		private bool NeedsRecalculate(int fromIndex, int toIndex, int resolvedTicksPerRow, int resolvedDeltaTicksPerRow)
		{
			VisibleRangeRowSizingMode effectiveMode = RowSizingMode;
			VisibleRangeRowSizingMode effectiveDeltaMode = DeltaRowSizingMode;
			int rowCount = Math.Max(1, RowCount);
			int ticksPerRow = Math.Max(1, TicksPerRow);
			int deltaRowCount = Math.Max(1, DeltaRowCount);
			int deltaTicksPerRow = Math.Max(1, DeltaTicksPerRow);
			return profileDirty
				|| fromIndex != cachedFromIndex
				|| toIndex != cachedToIndex
				|| Bars.Count != cachedBarsCount
				|| rowCount != cachedRowCount
				|| ticksPerRow != cachedTicksPerRow
				|| resolvedTicksPerRow != cachedResolvedTicksPerRow
				|| DynamicRowMinPixels != cachedDynamicRowMinPixels
				|| effectiveMode != cachedRowSizingMode
				|| deltaRowCount != cachedDeltaRowCount
				|| deltaTicksPerRow != cachedDeltaTicksPerRow
				|| resolvedDeltaTicksPerRow != cachedResolvedDeltaTicksPerRow
				|| DeltaDynamicRowMinPixels != cachedDeltaDynamicRowMinPixels
				|| effectiveDeltaMode != cachedDeltaRowSizingMode
				|| ProfileDataMode != cachedProfileDataMode
				|| dataRevision != cachedDataRevision
				|| Math.Abs(ValueAreaPercent - cachedValueAreaPercent) > 1E-09
				|| Math.Abs(DynamicAggregationMultiplier - cachedDynamicAggregationMultiplier) > 1E-09
				|| Math.Abs(DeltaDynamicAggregationMultiplier - cachedDeltaDynamicAggregationMultiplier) > 1E-09
				|| Math.Abs(TickSize - cachedTickSize) > 1E-12;
		}

		private void RecalculateProfile(int fromIndex, int toIndex, int resolvedTicksPerRow, int resolvedDeltaTicksPerRow)
		{
			if (profileResult == null)
				profileResult = new OrcaVolumeProfileResult();
			if (deltaResult == null)
				deltaResult = new OrcaVolumeProfileResult();

			VisibleRangeRowSizingMode effectiveMode = RowSizingMode;
			VisibleRangeRowSizingMode effectiveDeltaMode = DeltaRowSizingMode;
			bool useTicksPerRow = effectiveMode != VisibleRangeRowSizingMode.RowCount;
			bool useDeltaTicksPerRow = effectiveDeltaMode != VisibleRangeRowSizingMode.RowCount;
			int rowCount = Math.Max(1, RowCount);
			int ticksPerRow = useTicksPerRow ? Math.Max(1, resolvedTicksPerRow) : Math.Max(1, TicksPerRow);
			int deltaRowCount = Math.Max(1, DeltaRowCount);
			int deltaTicksPerRow = useDeltaTicksPerRow ? Math.Max(1, resolvedDeltaTicksPerRow) : Math.Max(1, DeltaTicksPerRow);
			if (ProfileDataMode == VisibleRangeProfileDataMode.TrueVolumeAtPrice)
			{
				OrcaVolumeProfileCore.BuildVisibleRangeFromPriceMaps(barVolumeMaps, barUpVolumeMaps, barDownVolumeMaps, fromIndex, toIndex, rowCount, ticksPerRow, useTicksPerRow, ValueAreaPercent, TickSize, profileResult);
				OrcaVolumeProfileCore.BuildVisibleRangeFromPriceMaps(barVolumeMaps, barUpVolumeMaps, barDownVolumeMaps, fromIndex, toIndex, deltaRowCount, deltaTicksPerRow, useDeltaTicksPerRow, ValueAreaPercent, TickSize, deltaResult);
			}
			else
			{
				OrcaVolumeProfileCore.BuildVisibleRangeFromBars(Bars, fromIndex, toIndex, rowCount, ticksPerRow, useTicksPerRow, ValueAreaPercent, TickSize, profileResult);
				OrcaVolumeProfileCore.BuildVisibleRangeFromBars(Bars, fromIndex, toIndex, deltaRowCount, deltaTicksPerRow, useDeltaTicksPerRow, ValueAreaPercent, TickSize, deltaResult);
			}

			cachedFromIndex = fromIndex;
			cachedToIndex = toIndex;
			cachedBarsCount = Bars != null ? Bars.Count : -1;
			cachedRowCount = rowCount;
			cachedTicksPerRow = Math.Max(1, TicksPerRow);
			cachedResolvedTicksPerRow = effectiveMode == VisibleRangeRowSizingMode.RowCount ? -1 : ticksPerRow;
			cachedDynamicRowMinPixels = DynamicRowMinPixels;
			cachedDeltaRowCount = deltaRowCount;
			cachedDeltaTicksPerRow = Math.Max(1, DeltaTicksPerRow);
			cachedResolvedDeltaTicksPerRow = effectiveDeltaMode == VisibleRangeRowSizingMode.RowCount ? -1 : deltaTicksPerRow;
			cachedDeltaDynamicRowMinPixels = DeltaDynamicRowMinPixels;
			cachedValueAreaPercent = ValueAreaPercent;
			cachedDynamicAggregationMultiplier = DynamicAggregationMultiplier;
			cachedDeltaDynamicAggregationMultiplier = DeltaDynamicAggregationMultiplier;
			cachedTickSize = TickSize;
			cachedRowSizingMode = effectiveMode;
			cachedDeltaRowSizingMode = effectiveDeltaMode;
			cachedProfileDataMode = ProfileDataMode;
			cachedDataRevision = dataRevision;
			totalVolumeLabel = profileResult.HasProfile ? FormatVolume(profileResult.TotalVolume) : string.Empty;
			profileDirty = false;
		}

		private int ResolveTicksPerRow(VisibleRangeRowSizingMode rowSizingMode, int ticksPerRow, int dynamicRowMinPixels, double dynamicAggregationMultiplier, ChartScale chartScale, NinjaTrader.Gui.Chart.ChartPanel panel)
		{
			if (rowSizingMode == VisibleRangeRowSizingMode.RowCount)
				return -1;

			if (rowSizingMode == VisibleRangeRowSizingMode.TicksPerRow || chartScale == null || panel == null || TickSize <= 0)
				return Math.Max(1, ticksPerRow);

			double visibleTicks = Math.Max(1.0, (chartScale.MaxValue - chartScale.MinValue) / TickSize);
			double ticksPerPixel = visibleTicks / Math.Max(1.0, panel.H);
			double desiredTicks = ticksPerPixel * Math.Max(1, dynamicRowMinPixels) * Math.Max(0.1, dynamicAggregationMultiplier);
			return RoundToProfileTicks(desiredTicks);
		}

		private int RoundToProfileTicks(double desiredTicks)
		{
			if (desiredTicks <= 1) return 1;
			if (desiredTicks <= 2) return 2;
			if (desiredTicks <= 4) return 4;
			if (desiredTicks <= 5) return 5;
			if (desiredTicks <= 8) return 8;
			if (desiredTicks <= 10) return 10;
			if (desiredTicks <= 15) return 15;
			if (desiredTicks <= 20) return 20;
			if (desiredTicks <= 25) return 25;
			if (desiredTicks <= 30) return 30;
			if (desiredTicks <= 40) return 40;
			if (desiredTicks <= 50) return 50;
			if (desiredTicks <= 100) return Math.Max(50, (int)(Math.Round(desiredTicks / 20.0) * 20));
			return Math.Max(100, (int)(Math.Round(desiredTicks / 50.0) * 50));
		}

		private bool TryGetRenderPanel(ChartControl chartControl, ChartScale chartScale, out NinjaTrader.Gui.Chart.ChartPanel panel)
		{
			panel = null;
			try
			{
				if (chartControl == null || chartScale == null)
					return false;

				int panelIndex = chartScale.PanelIndex;
				if (chartControl.ChartPanels != null && panelIndex >= 0 && panelIndex < chartControl.ChartPanels.Count)
					panel = chartControl.ChartPanels[panelIndex];
				else
					panel = ChartPanel;

				return panel != null && panel.W > 0 && panel.H > 0;
			}
			catch
			{
				return false;
			}
		}

		private void DrawProfile(ChartScale chartScale, NinjaTrader.Gui.Chart.ChartPanel panel)
		{
			float panelLeft = panel.X;
			float panelRight = panel.X + panel.W;
			float panelTop = panel.Y;
			float panelBottom = panel.Y + panel.H;
			float volumeWidth = ShowVolume ? (float)(panel.W * (Clamp(ProfileWidthPercent, 5, 50) / 100.0)) : 0f;
			float deltaWidth = ShowDelta ? (float)(panel.W * (Clamp(DeltaWidthPercent, 3, 50) / 100.0)) : 0f;
			if (ShowVolume) volumeWidth = Math.Max(10f, volumeWidth);
			if (ShowDelta) deltaWidth = Math.Max(8f, deltaWidth);

			float totalWidth = volumeWidth + deltaWidth;
			if (totalWidth <= 0)
				return;

			float maxTotalWidth = Math.Max(20f, panel.W * 0.85f);
			if (totalWidth > maxTotalWidth)
			{
				float scale = maxTotalWidth / totalWidth;
				volumeWidth *= scale;
				deltaWidth *= scale;
				totalWidth = maxTotalWidth;
			}

			bool profileOnRight = ProfileSide == VisibleRangeProfileSide.Right;
			bool deltaTowardOuterEdge = DeltaDirection == VisibleRangeDeltaDirection.TowardPriceScale;
			bool deltaBeforeVolume = ShowDelta && (!ShowVolume || (profileOnRight ? !deltaTowardOuterEdge : deltaTowardOuterEdge));
			float bandLeft = profileOnRight ? panelRight - 2f - totalWidth : panelLeft + 2f;
			float bandRight = bandLeft + totalWidth;

			float volumeLeft;
			float volumeRight;
			float deltaLeft;
			float deltaRight;

			if (ShowVolume && ShowDelta)
			{
				if (deltaBeforeVolume)
				{
					deltaLeft = bandLeft;
					deltaRight = deltaLeft + deltaWidth;
					volumeLeft = deltaRight;
					volumeRight = bandRight;
				}
				else
				{
					volumeLeft = bandLeft;
					volumeRight = volumeLeft + volumeWidth;
					deltaLeft = volumeRight;
					deltaRight = bandRight;
				}
			}
			else if (ShowVolume)
			{
				volumeLeft = bandLeft;
				volumeRight = bandRight;
				deltaLeft = deltaRight = bandRight;
			}
			else
			{
				deltaLeft = bandLeft;
				deltaRight = bandRight;
				volumeLeft = volumeRight = bandLeft;
			}

			bool volumeDrawFromRight = ShowDelta ? DrawFromRightForDirection(!deltaTowardOuterEdge) : DrawFromRightForDirection(false);
			bool deltaDrawFromRight = DrawFromRightForDirection(deltaTowardOuterEdge);

			if (ShowVolume && profileResult.MaxVolume > 0)
				DrawVolumeRows(chartScale, panelTop, panelBottom, volumeLeft, volumeRight, volumeDrawFromRight);

			if (ShowDelta && deltaResult != null && deltaResult.MaxDelta > 0)
				DrawDeltaRows(chartScale, panelTop, panelBottom, deltaLeft, deltaRight, deltaDrawFromRight);

			float referenceLeft = ShowVolume ? volumeLeft : deltaLeft;
			float referenceRight = ShowVolume ? volumeRight : deltaRight;
			DrawReferenceLines(chartScale, panelTop, panelBottom, referenceLeft, referenceRight);
			DrawTotalVolume(panelTop, bandLeft, bandRight);
		}

		private bool DrawFromRightForDirection(bool towardOuterEdge)
		{
			return towardOuterEdge ? ProfileSide == VisibleRangeProfileSide.Left : ProfileSide == VisibleRangeProfileSide.Right;
		}

		private void DrawVolumeRows(ChartScale chartScale, float panelTop, float panelBottom, float profileLeft, float profileRight, bool drawFromRight)
		{
			float width = Math.Max(1f, profileRight - profileLeft);
			for (int rowIndex = 0; rowIndex < profileResult.RowCount; rowIndex++)
			{
				OrcaVolumeProfileRow row = profileResult.Rows[rowIndex];
				if (row.Volume <= 0)
					continue;

				float yTop = chartScale.GetYByValue(row.HighPrice);
				float yBottom = chartScale.GetYByValue(row.LowPrice);
				if (yBottom < panelTop - 2 || yTop > panelBottom + 2)
					continue;

				float rowHeightPx = Math.Max(1f, Math.Abs(yBottom - yTop) - ProfileBarSpacingPx);
				float drawY = Math.Min(yTop, yBottom) + (ProfileBarSpacingPx / 2f);
				float barWidth = (float)(width * (row.Volume / profileResult.MaxVolume));
				if (barWidth < 0.5f)
					continue;

				SharpDX.Direct2D1.Brush brush = SelectRowBrush(rowIndex, row);
				float drawX = drawFromRight ? profileRight - barWidth : profileLeft;
				RenderTarget.FillRectangle(new RectangleF(drawX, drawY, barWidth, rowHeightPx), brush);
			}
		}

		private void DrawDeltaRows(ChartScale chartScale, float panelTop, float panelBottom, float profileLeft, float profileRight, bool drawFromRight)
		{
			float width = Math.Max(1f, profileRight - profileLeft);
			for (int rowIndex = 0; rowIndex < deltaResult.RowCount; rowIndex++)
			{
				OrcaVolumeProfileRow row = deltaResult.Rows[rowIndex];
				double delta = row.UpVolume - row.DownVolume;
				if (Math.Abs(delta) <= 1E-09)
					continue;

				float yTop = chartScale.GetYByValue(row.HighPrice);
				float yBottom = chartScale.GetYByValue(row.LowPrice);
				if (yBottom < panelTop - 2 || yTop > panelBottom + 2)
					continue;

				float rowHeightPx = Math.Max(1f, Math.Abs(yBottom - yTop) - ProfileBarSpacingPx);
				float drawY = Math.Min(yTop, yBottom) + (ProfileBarSpacingPx / 2f);
				float barWidth = (float)(width * (Math.Abs(delta) / deltaResult.MaxDelta));
				if (barWidth < 0.5f)
					continue;

				SharpDX.Direct2D1.SolidColorBrush brush = delta >= 0 ? deltaPositiveBrushDx : deltaNegativeBrushDx;
				float drawX = drawFromRight ? profileRight - barWidth : profileLeft;
				RenderTarget.FillRectangle(new RectangleF(drawX, drawY, barWidth, rowHeightPx), brush);
				DrawDeltaLabel(delta, drawX, drawY, barWidth, rowHeightPx, profileLeft, profileRight, drawFromRight);
			}
		}

		private void DrawDeltaLabel(double delta, float drawX, float drawY, float barWidth, float rowHeightPx, float profileLeft, float profileRight, bool drawFromRight)
		{
			if (!ShowDeltaLabels || deltaLabelTextFormatDx == null)
				return;

			float fontSize = (float)Clamp(DeltaLabelFontSize, 6.0, 30.0);
			if (rowHeightPx < fontSize + 2f)
				return;

			string label = FormatDelta(delta);
			float labelWidth = EstimateTextWidth(label, fontSize);
			if (barWidth < labelWidth + 4f)
				return;

			float textX = drawFromRight ? drawX + barWidth - labelWidth - 2f : drawX + 2f;
			if (textX < profileLeft)
				textX = profileLeft + 1f;
			if (textX + labelWidth + 2f > profileRight)
				textX = profileRight - labelWidth - 2f;

			SharpDX.Direct2D1.SolidColorBrush labelBrush = delta >= 0 ? deltaPositiveLabelBrushDx : deltaNegativeLabelBrushDx;
			if (labelBrush == null)
				return;

			RenderTarget.DrawText(label, deltaLabelTextFormatDx, new RectangleF(textX, drawY, labelWidth + 4f, rowHeightPx), labelBrush);
		}

		private SharpDX.Direct2D1.Brush SelectRowBrush(int rowIndex, OrcaVolumeProfileRow row)
		{
			if (ShowPOC && rowIndex == profileResult.PocIndex)
				return pocBrushDx;

			bool insideValueArea = ShowValueArea && profileResult.HasValueArea && rowIndex >= profileResult.ValIndex && rowIndex <= profileResult.VahIndex;
			if (insideValueArea && ShowVAColor)
				return SelectGradientBrush(row, vaGradientBrushes, vaFillBrushDx);

			bool upDominant = row.UpVolume >= row.DownVolume;
			return SelectGradientBrush(row, upDominant ? upGradientBrushes : downGradientBrushes, upDominant ? upBrushDx : downBrushDx);
		}

		private SharpDX.Direct2D1.Brush SelectGradientBrush(OrcaVolumeProfileRow row, SharpDX.Direct2D1.SolidColorBrush[] palette, SharpDX.Direct2D1.SolidColorBrush fallback)
		{
			if (!UseGradient || palette == null || palette.Length == 0 || profileResult == null || profileResult.MaxVolume <= 0)
				return fallback;

			int gradientIndex = (int)((row.Volume / profileResult.MaxVolume) * (palette.Length - 1));
			if (gradientIndex < 0) gradientIndex = 0;
			if (gradientIndex >= palette.Length) gradientIndex = palette.Length - 1;
			return palette[gradientIndex];
		}

		private void DrawReferenceLines(ChartScale chartScale, float panelTop, float panelBottom, float profileLeft, float profileRight)
		{
			if (ShowPOC && profileResult.PocIndex >= 0)
				DrawHorizontalProfileLine(chartScale.GetYByValue(profileResult.PocPrice), panelTop, panelBottom, profileLeft, profileRight, pocBrushDx, 2f);

			if (ShowValueArea && ShowVALines && profileResult.HasValueArea)
			{
				if (ShowVAH)
					DrawHorizontalProfileLine(chartScale.GetYByValue(profileResult.VahPrice), panelTop, panelBottom, profileLeft, profileRight, vaLineBrushDx, VALineThickness);
				if (ShowVAL)
					DrawHorizontalProfileLine(chartScale.GetYByValue(profileResult.ValPrice), panelTop, panelBottom, profileLeft, profileRight, vaLineBrushDx, VALineThickness);
			}
		}

		private void DrawHorizontalProfileLine(float y, float panelTop, float panelBottom, float left, float right, SharpDX.Direct2D1.SolidColorBrush brush, float thickness)
		{
			if (brush == null || y < panelTop - 3 || y > panelBottom + 3)
				return;

			if (brush == vaLineBrushDx && vaLineStrokeDx != null)
				RenderTarget.DrawLine(new Vector2(left, y), new Vector2(right, y), brush, thickness, vaLineStrokeDx);
			else
				RenderTarget.DrawLine(new Vector2(left, y), new Vector2(right, y), brush, thickness);
		}

		private void DrawTotalVolume(float panelTop, float profileLeft, float profileRight)
		{
			if (!ShowTotalVolume || string.IsNullOrEmpty(totalVolumeLabel) || textBrushDx == null || textFormatDx == null)
				return;

			float textWidth = Math.Max(60f, profileRight - profileLeft);
			RectangleF textRect = new RectangleF(profileLeft, panelTop + 4f, textWidth, 18f);
			RenderTarget.DrawText(totalVolumeLabel, textFormatDx, textRect, textBrushDx);
		}

		private void AttachChartControl(ChartControl chartControl)
		{
			if (chartControl == null || ReferenceEquals(chartControl, subscribedChartControlRef))
				return;

			DetachChartControl();
			subscribedChartControlRef = chartControl;
			subscribedChartControl = chartControl as INotifyPropertyChanged;
			if (subscribedChartControl != null)
				subscribedChartControl.PropertyChanged += OnChartControlPropertyChanged;
		}

		private void DetachChartControl()
		{
			if (subscribedChartControl != null)
				subscribedChartControl.PropertyChanged -= OnChartControlPropertyChanged;

			subscribedChartControl = null;
			subscribedChartControlRef = null;
		}

		private void OnChartControlPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e == null || string.IsNullOrEmpty(e.PropertyName))
			{
				profileDirty = true;
				return;
			}

			string propertyName = e.PropertyName;
			if (propertyName.IndexOf("Bar", StringComparison.OrdinalIgnoreCase) >= 0
				|| propertyName.IndexOf("Canvas", StringComparison.OrdinalIgnoreCase) >= 0)
				profileDirty = true;
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null)
				return;

			int steps = Math.Max(2, GradientSteps);
			string brushSignature = BuildBrushSignature();
			if (lastBuiltOpacity != Opacity || brushSignature != lastBrushSignature)
				DisposeDxResources();

			float alpha = Opacity / 255f;
			if (pocBrushDx == null) pocBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(POCColor, 1f));
			if (vaFillBrushDx == null) vaFillBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VAColor, alpha));
			if (vaLineBrushDx == null) vaLineBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VAColor, 1f));
			if (upBrushDx == null) upBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(ProfileUpColor, alpha));
			if (downBrushDx == null) downBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(ProfileDownColor, alpha));
			if (deltaPositiveBrushDx == null) deltaPositiveBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaPositiveColor, alpha));
			if (deltaNegativeBrushDx == null) deltaNegativeBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaNegativeColor, alpha));
			if (deltaPositiveLabelBrushDx == null) deltaPositiveLabelBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaPositiveLabelColor, 1f));
			if (deltaNegativeLabelBrushDx == null) deltaNegativeLabelBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaNegativeLabelColor, 1f));
			if (textBrushDx == null) textBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(POCColor, 1f));
			if (vaLineStrokeDx == null || lastBuiltVALineStyle != VALineStyle)
			{
				if (vaLineStrokeDx != null) vaLineStrokeDx.Dispose();
				vaLineStrokeDx = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = ToDxDashStyle(VALineStyle) });
				lastBuiltVALineStyle = VALineStyle;
			}
			if (textFormatDx == null)
			{
				textFormatDx = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 11f)
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
					ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center
				};
			}
			if (deltaLabelTextFormatDx == null)
			{
				deltaLabelTextFormatDx = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)Clamp(DeltaLabelFontSize, 6.0, 30.0))
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
					ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center
				};
			}

			if (UseGradient && (upGradientBrushes == null || downGradientBrushes == null || vaGradientBrushes == null || lastBuiltGradientSteps != steps || Math.Abs(lastBuiltMinBrightness - MinBrightness) > 0.0001f || lastBuiltOpacity != Opacity))
			{
				DisposePalette(ref upGradientBrushes);
				DisposePalette(ref downGradientBrushes);
				DisposePalette(ref vaGradientBrushes);
				upGradientBrushes = BuildGradientPalette(ProfileUpColor, steps, alpha);
				downGradientBrushes = BuildGradientPalette(ProfileDownColor, steps, alpha);
				vaGradientBrushes = BuildGradientPalette(VAColor, steps, alpha);
				lastBuiltGradientSteps = steps;
				lastBuiltMinBrightness = MinBrightness;
			}
			lastBuiltOpacity = Opacity;
			lastBrushSignature = brushSignature;
		}

		private void DisposeDxResources()
		{
			if (pocBrushDx != null) { pocBrushDx.Dispose(); pocBrushDx = null; }
			if (vaFillBrushDx != null) { vaFillBrushDx.Dispose(); vaFillBrushDx = null; }
			if (vaLineBrushDx != null) { vaLineBrushDx.Dispose(); vaLineBrushDx = null; }
			if (upBrushDx != null) { upBrushDx.Dispose(); upBrushDx = null; }
			if (downBrushDx != null) { downBrushDx.Dispose(); downBrushDx = null; }
			if (deltaPositiveBrushDx != null) { deltaPositiveBrushDx.Dispose(); deltaPositiveBrushDx = null; }
			if (deltaNegativeBrushDx != null) { deltaNegativeBrushDx.Dispose(); deltaNegativeBrushDx = null; }
			if (deltaPositiveLabelBrushDx != null) { deltaPositiveLabelBrushDx.Dispose(); deltaPositiveLabelBrushDx = null; }
			if (deltaNegativeLabelBrushDx != null) { deltaNegativeLabelBrushDx.Dispose(); deltaNegativeLabelBrushDx = null; }
			if (textBrushDx != null) { textBrushDx.Dispose(); textBrushDx = null; }
			if (vaLineStrokeDx != null) { vaLineStrokeDx.Dispose(); vaLineStrokeDx = null; }
			DisposePalette(ref upGradientBrushes);
			DisposePalette(ref downGradientBrushes);
			DisposePalette(ref vaGradientBrushes);
			if (textFormatDx != null) { textFormatDx.Dispose(); textFormatDx = null; }
			if (deltaLabelTextFormatDx != null) { deltaLabelTextFormatDx.Dispose(); deltaLabelTextFormatDx = null; }
			lastBuiltGradientSteps = -1;
			lastBuiltMinBrightness = -1f;
			lastBuiltOpacity = 0;
			lastBuiltVALineStyle = (VisibleRangeVALineStyle)(-1);
			lastBrushSignature = string.Empty;
		}

		private string BuildBrushSignature()
		{
			return NinjaTrader.Gui.Serialize.BrushToString(POCColor) + "|"
				+ NinjaTrader.Gui.Serialize.BrushToString(VAColor) + "|"
				+ NinjaTrader.Gui.Serialize.BrushToString(ProfileUpColor) + "|"
				+ NinjaTrader.Gui.Serialize.BrushToString(ProfileDownColor) + "|"
				+ NinjaTrader.Gui.Serialize.BrushToString(DeltaPositiveColor) + "|"
				+ NinjaTrader.Gui.Serialize.BrushToString(DeltaNegativeColor) + "|"
				+ NinjaTrader.Gui.Serialize.BrushToString(DeltaPositiveLabelColor) + "|"
				+ NinjaTrader.Gui.Serialize.BrushToString(DeltaNegativeLabelColor) + "|"
				+ DeltaLabelFontSize.ToString("0.###") + "|"
				+ Opacity.ToString();
		}

		private Color4 ToDxColor(WpfBrush brush, float opacity)
		{
			WpfSolidColorBrush solidBrush = brush as WpfSolidColorBrush;
			System.Windows.Media.Color color = solidBrush != null ? solidBrush.Color : WpfColors.White;
			return new Color4(color.R / 255f, color.G / 255f, color.B / 255f, (color.A / 255f) * opacity);
		}

		private SharpDX.Direct2D1.SolidColorBrush[] BuildGradientPalette(WpfBrush brush, int steps, float opacity)
		{
			SharpDX.Direct2D1.SolidColorBrush[] palette = new SharpDX.Direct2D1.SolidColorBrush[steps];
			Color4 baseColor = ToDxColor(brush, opacity);
			float minBrightness = (float)Clamp(MinBrightness, 0.01, 1.0);
			for (int index = 0; index < steps; index++)
			{
				float ratio = index / (float)(steps - 1);
				float brightness = minBrightness + ((1f - minBrightness) * ratio);
				palette[index] = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(baseColor.Red * brightness, baseColor.Green * brightness, baseColor.Blue * brightness, opacity));
			}
			return palette;
		}

		private void DisposePalette(ref SharpDX.Direct2D1.SolidColorBrush[] palette)
		{
			if (palette != null)
			{
				foreach (SharpDX.Direct2D1.SolidColorBrush brush in palette)
					if (brush != null) brush.Dispose();
			}
			palette = null;
		}

		private DashStyle ToDxDashStyle(VisibleRangeVALineStyle lineStyle)
		{
			switch (lineStyle)
			{
				case VisibleRangeVALineStyle.Solid: return DashStyle.Solid;
				case VisibleRangeVALineStyle.Dot: return DashStyle.Dot;
				case VisibleRangeVALineStyle.DashDot: return DashStyle.DashDot;
				default: return DashStyle.Dash;
			}
		}

		private string FormatVolume(double volume)
		{
			double absVolume = Math.Abs(volume);
			if (absVolume >= 1000000)
				return (volume / 1000000.0).ToString("0.##") + "M";
			if (absVolume >= 1000)
				return (volume / 1000.0).ToString("0.#") + "K";
			return volume.ToString("0");
		}

		private string FormatDelta(double delta)
		{
			long roundedDelta = (long)Math.Round(delta);
			return roundedDelta.ToString("+#,0;-#,0;0");
		}

		private float EstimateTextWidth(string text, float fontSize)
		{
			if (string.IsNullOrEmpty(text))
				return fontSize;

			return Math.Max(fontSize, text.Length * fontSize * 0.62f);
		}

		private double Clamp(double value, double min, double max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		[NinjaScriptProperty]
		[Display(Name = "Data Mode", Order = 1, GroupName = "1. Profile")]
		public VisibleRangeProfileDataMode ProfileDataMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Row Count", Order = 2, GroupName = "1. Profile")]
		public int RowCount { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Row Sizing Mode", Order = 3, GroupName = "1. Profile")]
		public VisibleRangeRowSizingMode RowSizingMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "Ticks Per Row", Order = 4, GroupName = "1. Profile")]
		public int TicksPerRow { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "Dynamic Aggregation Multiplier", Order = 5, GroupName = "1. Profile")]
		public double DynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(2, 40)]
		[Display(Name = "Dynamic Row Min Pixels", Order = 6, GroupName = "1. Profile")]
		public int DynamicRowMinPixels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Delta Row Sizing Mode", Order = 7, GroupName = "1. Profile")]
		public VisibleRangeRowSizingMode DeltaRowSizingMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Delta Row Count", Order = 8, GroupName = "1. Profile")]
		public int DeltaRowCount { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "Delta Ticks Per Row", Order = 9, GroupName = "1. Profile")]
		public int DeltaTicksPerRow { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "Delta Dynamic Multiplier", Order = 10, GroupName = "1. Profile")]
		public double DeltaDynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(2, 40)]
		[Display(Name = "Delta Dynamic Row Min Pixels", Order = 11, GroupName = "1. Profile")]
		public int DeltaDynamicRowMinPixels { get; set; }

		[NinjaScriptProperty]
		[Range(1.0, 100.0)]
		[Display(Name = "Value Area Percent", Order = 12, GroupName = "1. Profile")]
		public double ValueAreaPercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Profile Side", Order = 1, GroupName = "2. Display")]
		public VisibleRangeProfileSide ProfileSide { get; set; }

		[NinjaScriptProperty]
		[Range(5.0, 50.0)]
		[Display(Name = "Profile Width Percent", Order = 2, GroupName = "2. Display")]
		public double ProfileWidthPercent { get; set; }

		[NinjaScriptProperty]
		[Range(3.0, 50.0)]
		[Display(Name = "Delta Width Percent", Order = 3, GroupName = "2. Display")]
		public double DeltaWidthPercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Delta Direction", Order = 4, GroupName = "2. Display")]
		public VisibleRangeDeltaDirection DeltaDirection { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Profile Bar Spacing", Order = 5, GroupName = "2. Display")]
		public int ProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Volume", Order = 6, GroupName = "2. Display")]
		public bool ShowVolume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta", Order = 7, GroupName = "2. Display")]
		public bool ShowDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show POC", Order = 8, GroupName = "2. Display")]
		public bool ShowPOC { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Value Area", Order = 9, GroupName = "2. Display")]
		public bool ShowValueArea { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VA Color", Order = 10, GroupName = "2. Display")]
		public bool ShowVAColor { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VA Lines", Order = 11, GroupName = "2. Display")]
		public bool ShowVALines { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VAH", Order = 12, GroupName = "2. Display")]
		public bool ShowVAH { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VAL", Order = 13, GroupName = "2. Display")]
		public bool ShowVAL { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Total Volume", Order = 14, GroupName = "2. Display")]
		public bool ShowTotalVolume { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 8.0)]
		[Display(Name = "VA Line Thickness", Order = 15, GroupName = "2. Display")]
		public float VALineThickness { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Line Style", Order = 16, GroupName = "2. Display")]
		public VisibleRangeVALineStyle VALineStyle { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Gradient", Order = 1, GroupName = "3. Gradient")]
		public bool UseGradient { get; set; }

		[NinjaScriptProperty]
		[Range(2, 64)]
		[Display(Name = "Gradient Steps", Order = 2, GroupName = "3. Gradient")]
		public int GradientSteps { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 1.0)]
		[Display(Name = "Min Brightness", Order = 3, GroupName = "3. Gradient")]
		public float MinBrightness { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Color", Order = 1, GroupName = "4. Colors")]
		public WpfBrush POCColor { get; set; }

		[Browsable(false)]
		public string POCColorSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(POCColor); }
			set { POCColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Value Area Color", Order = 2, GroupName = "4. Colors")]
		public WpfBrush VAColor { get; set; }

		[Browsable(false)]
		public string VAColorSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(VAColor); }
			set { VAColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Profile Up Color", Order = 3, GroupName = "4. Colors")]
		public WpfBrush ProfileUpColor { get; set; }

		[Browsable(false)]
		public string ProfileUpColorSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(ProfileUpColor); }
			set { ProfileUpColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Profile Down Color", Order = 4, GroupName = "4. Colors")]
		public WpfBrush ProfileDownColor { get; set; }

		[Browsable(false)]
		public string ProfileDownColorSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(ProfileDownColor); }
			set { ProfileDownColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Delta Positive Color", Order = 5, GroupName = "4. Colors")]
		public WpfBrush DeltaPositiveColor { get; set; }

		[Browsable(false)]
		public string DeltaPositiveColorSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(DeltaPositiveColor); }
			set { DeltaPositiveColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Delta Negative Color", Order = 6, GroupName = "4. Colors")]
		public WpfBrush DeltaNegativeColor { get; set; }

		[Browsable(false)]
		public string DeltaNegativeColorSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(DeltaNegativeColor); }
			set { DeltaNegativeColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 255)]
		[Display(Name = "Opacity", Order = 7, GroupName = "4. Colors")]
		public byte Opacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta Labels", Order = 1, GroupName = "5. Delta Labels")]
		public bool ShowDeltaLabels { get; set; }

		[NinjaScriptProperty]
		[Range(6.0, 30.0)]
		[Display(Name = "Delta Label Font Size", Order = 2, GroupName = "5. Delta Labels")]
		public float DeltaLabelFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Positive Label Color", Order = 3, GroupName = "5. Delta Labels")]
		public WpfBrush DeltaPositiveLabelColor { get; set; }

		[Browsable(false)]
		public string DeltaPositiveLabelColorSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(DeltaPositiveLabelColor); }
			set { DeltaPositiveLabelColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Negative Label Color", Order = 4, GroupName = "5. Delta Labels")]
		public WpfBrush DeltaNegativeLabelColor { get; set; }

		[Browsable(false)]
		public string DeltaNegativeLabelColorSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(DeltaNegativeLabelColor); }
			set { DeltaNegativeLabelColor = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}
	}
}
