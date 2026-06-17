#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors = System.Windows.Media.Colors;
using DxColor4 = SharpDX.Color4;
using DxRectangleF = SharpDX.RectangleF;
using DxVector2 = SharpDX.Vector2;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum OrcaAnchoredVwapPriceSource
	{
		HLC3,
		Close,
		HL2,
		OHLC4
	}

	public enum OrcaAnchoredVwapDeviationMode
	{
		Off,
		AllBands,
		DirectionalSupportResistanceBands,
		ManualSideSelection
	}

	public enum OrcaAnchoredVwapDirectionDetectionMethod
	{
		NetVwapChangeFromAnchor,
		RecentVwapSlope,
		ManualOverride
	}

	public enum OrcaAnchoredVwapFlatDirectionFallback
	{
		ShowBoth,
		ShowNone,
		KeepLastDirection,
		UseManualBandSide
	}

	public enum OrcaAnchoredVwapBandSide
	{
		UpperBandsOnly,
		LowerBandsOnly,
		Both
	}

	public enum OrcaAnchoredVwapCloudMode
	{
		Off,
		VwapToFirstActiveBand,
		BetweenFirstAndSecondActiveBands,
		FullSelectedDeviationZone
	}

	public enum OrcaAnchoredVwapLabelAlignment
	{
		RightEdge,
		Endpoint,
		LastVisibleBar
	}
}

namespace NinjaTrader.NinjaScript.DrawingTools
{
	public class OrcaManualAnchoredVWAP : DrawingTool
	{
		private const double PriceEpsilon = 1E-09;
		private const double CursorSensitivity = 12.0;
		private const float LabelPaddingPx = 4f;

		private enum EditMode
		{
			None,
			Start,
			End,
			MoveAll
		}

		private enum ResolvedBandSide
		{
			None,
			UpperOnly,
			LowerOnly,
			Both
		}

		private struct VwapPoint
		{
			public int BarIndex;
			public int SegmentId;
			public double Vwap;
			public double StdDev;
			public double Upper1;
			public double Upper2;
			public double Upper3;
			public double Lower1;
			public double Lower2;
			public double Lower3;
		}

		private readonly List<VwapPoint> vwapPoints = new List<VwapPoint>();
		private readonly List<DxVector2> cloudUpperPoints = new List<DxVector2>();
		private readonly List<DxVector2> cloudLowerPoints = new List<DxVector2>();

		private ChartAnchor lastMouseMoveDataPoint;
		private EditMode editMode;
		private bool vwapDirty = true;
		private string noDataLabel = string.Empty;
		private ResolvedBandSide lastDirectionalSide = ResolvedBandSide.None;
		private ResolvedBandSide activeBandSide = ResolvedBandSide.Both;

		private DateTime cachedAnchorTime = DateTime.MinValue;
		private DateTime cachedEndTime = DateTime.MinValue;
		private int cachedStartBar = -1;
		private int cachedEndBar = -1;
		private int cachedBarsCount = -1;
		private DateTime cachedLastRangeBarTime = DateTime.MinValue;
		private double cachedLastRangeBarVolume = double.NaN;
		private double cachedAnchorPrice = double.NaN;
		private double cachedTickSize = double.NaN;
		private string cachedSettingsKey = string.Empty;

		private double cachedMinValue = double.NaN;
		private double cachedMaxValue = double.NaN;

		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private SharpDX.Direct2D1.SolidColorBrush supportCloudBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush resistanceCloudBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush neutralCloudBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush labelBrushDx;
		private TextFormat labelTextFormatDx;
		private string lastBrushSignature = string.Empty;
		private int lastCloudOpacity = -1;
		private float lastLabelFontSize = -1f;

		public override object Icon
		{
			get { return Icons.DrawVWAP; }
		}

		public override bool SupportsAlerts
		{
			get { return true; }
		}

		public override IEnumerable<ChartAnchor> Anchors
		{
			get { return new ChartAnchor[] { StartAnchor, EndAnchor }; }
		}

		[Display(Order = 1)]
		public ChartAnchor StartAnchor { get; set; }

		[Display(Order = 2)]
		public ChartAnchor EndAnchor { get; set; }

		public override void OnCalculateMinMax()
		{
			MinValue = double.MaxValue;
			MaxValue = double.MinValue;

			if (!IsVisible || StartAnchor == null || EndAnchor == null)
				return;

			if (!double.IsNaN(cachedMinValue) && !double.IsNaN(cachedMaxValue) && cachedMinValue <= cachedMaxValue)
			{
				MinValue = cachedMinValue;
				MaxValue = cachedMaxValue;
				return;
			}

			MinValue = Math.Min(StartAnchor.Price, EndAnchor.Price);
			MaxValue = Math.Max(StartAnchor.Price, EndAnchor.Price);
		}

		public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
		{
			yield return new AlertConditionItem
			{
				Name = "Orca manual anchored VWAP",
				ShouldOnlyDisplayName = true
			};
		}

		public override IEnumerable<Condition> GetValidAlertConditions()
		{
			return new Condition[] { Condition.CrossAbove, Condition.CrossBelow, Condition.Equals };
		}

		public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
		{
			if (values == null || values.Length == 0 || vwapPoints.Count == 0)
				return false;

			VwapPoint lastPoint = vwapPoints[vwapPoints.Count - 1];
			if (!IsValid(lastPoint.Vwap))
				return false;

			return MathHelper.DidPredicateCross(values, delegate(ChartAlertValue value)
			{
				if (condition == Condition.CrossAbove)
					return value.Value > lastPoint.Vwap;
				if (condition == Condition.CrossBelow)
					return value.Value < lastPoint.Vwap;
				return Math.Abs(value.Value - lastPoint.Vwap) <= PriceEpsilon;
			});
		}

		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			if (DrawingState == DrawingState.Building)
				return true;
			if (StartAnchor == null || EndAnchor == null)
				return false;

			DateTime startTime = StartAnchor.Time;
			DateTime endTime = ExtendRight ? lastTimeOnChart : EndAnchor.Time;
			if (endTime < startTime)
			{
				DateTime swap = startTime;
				startTime = endTime;
				endTime = swap;
			}

			return startTime <= lastTimeOnChart && endTime >= firstTimeOnChart;
		}

		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			if (DrawingState == DrawingState.Building)
				return Cursors.Pen;
			if (DrawingState == DrawingState.Moving)
				return IsLocked ? Cursors.No : Cursors.SizeAll;
			if (DrawingState == DrawingState.Editing && IsLocked)
				return Cursors.No;

			EditMode mode = editMode != EditMode.None ? editMode : GetEditModeForPoint(point, chartControl, chartPanel, chartScale, true);
			switch (mode)
			{
				case EditMode.Start:
				case EditMode.End:
					return IsLocked ? Cursors.Arrow : Cursors.SizeWE;
				case EditMode.MoveAll:
					return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
				default:
					return null;
			}
		}

		public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			if (StartAnchor == null || EndAnchor == null)
				return new Point[0];

			ChartPanel chartPanel = chartControl.ChartPanels[chartScale.PanelIndex];
			return new Point[]
			{
				StartAnchor.GetPoint(chartControl, chartPanel, chartScale),
				EndAnchor.GetPoint(chartControl, chartPanel, chartScale)
			};
		}

		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (dataPoint == null)
				return;

			switch (DrawingState)
			{
				case DrawingState.Building:
					dataPoint.CopyDataValues(StartAnchor);
					dataPoint.CopyDataValues(EndAnchor);
					StartAnchor.IsEditing = false;
					EndAnchor.IsEditing = true;
					MarkVwapDirty();
					break;

				case DrawingState.Normal:
					Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					editMode = GetEditModeForPoint(point, chartControl, chartPanel, chartScale, true);
					if (editMode == EditMode.MoveAll)
						DrawingState = DrawingState.Moving;
					else if (editMode == EditMode.Start || editMode == EditMode.End)
						DrawingState = DrawingState.Editing;
					else
						IsSelected = false;

					if (lastMouseMoveDataPoint == null)
						lastMouseMoveDataPoint = new ChartAnchor();
					dataPoint.CopyDataValues(lastMouseMoveDataPoint);
					break;
			}
		}

		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (dataPoint == null || (IsLocked && DrawingState != DrawingState.Building))
				return;

			if (DrawingState == DrawingState.Building)
			{
				if (EndAnchor != null && EndAnchor.IsEditing)
				{
					dataPoint.CopyDataValues(EndAnchor);
					MarkVwapDirty();
				}
			}
			else if (DrawingState == DrawingState.Editing)
			{
				if (editMode == EditMode.Start)
					dataPoint.CopyDataValues(StartAnchor);
				else if (editMode == EditMode.End)
					dataPoint.CopyDataValues(EndAnchor);

				MarkVwapDirty();
			}
			else if (DrawingState == DrawingState.Moving)
			{
				foreach (ChartAnchor anchor in Anchors)
					anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
				MarkVwapDirty();
			}
		}

		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState == DrawingState.Building)
			{
				if (dataPoint != null)
					dataPoint.CopyDataValues(EndAnchor);
				EndAnchor.IsEditing = false;
				DrawingState = DrawingState.Normal;
				IsSelected = false;
				MarkVwapDirty();
				return;
			}

			if (DrawingState == DrawingState.Editing || DrawingState == DrawingState.Moving)
			{
				lastMouseMoveDataPoint = null;
				DrawingState = DrawingState.Normal;
				editMode = EditMode.None;
				MarkVwapDirty();
			}
		}

		public override void OnKeyDown(ChartControl chartControl, ChartPanel chartPanel, KeyEventArgs e)
		{
			if (e == null)
				return;

			if (e.Key == Key.Escape && DrawingState == DrawingState.Building)
			{
				EndAnchor.IsEditing = false;
				DrawingState = DrawingState.Normal;
				IsSelected = false;
				e.Handled = true;
			}
		}

		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (chartControl == null || chartScale == null || StartAnchor == null || EndAnchor == null || RenderTarget == null)
				return;

			ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
			RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

			if (IsInHitTest)
			{
				DrawHitTest(chartControl, chartPanel, chartScale);
				return;
			}

			EnsureDxResources();
			if (DrawingState == DrawingState.Building)
			{
				DrawAnchorGuide(chartControl, chartPanel, chartScale);
				return;
			}

			EnsureVwap(chartControl, chartScale);
			if (vwapPoints.Count == 0)
			{
				DrawNoDataLabel(chartPanel);
				return;
			}

			if (EnableCloud && CloudMode != OrcaAnchoredVwapCloudMode.Off && DeviationMode != OrcaAnchoredVwapDeviationMode.Off)
				DrawClouds(chartControl, chartPanel, chartScale);

			if (ShowVwapLine)
				DrawSeriesLine(chartControl, chartPanel, chartScale, 0, VwapStroke);

			if (DeviationMode != OrcaAnchoredVwapDeviationMode.Off)
			{
				if (ShouldShowUpperBands())
				{
					if (ShowDeviation1) DrawSeriesLine(chartControl, chartPanel, chartScale, 1, UpperBandStroke);
					if (ShowDeviation2) DrawSeriesLine(chartControl, chartPanel, chartScale, 2, UpperBandStroke);
					if (ShowDeviation3) DrawSeriesLine(chartControl, chartPanel, chartScale, 3, UpperBandStroke);
				}

				if (ShouldShowLowerBands())
				{
					if (ShowDeviation1) DrawSeriesLine(chartControl, chartPanel, chartScale, -1, LowerBandStroke);
					if (ShowDeviation2) DrawSeriesLine(chartControl, chartPanel, chartScale, -2, LowerBandStroke);
					if (ShowDeviation3) DrawSeriesLine(chartControl, chartPanel, chartScale, -3, LowerBandStroke);
				}
			}

			DrawLabels(chartControl, chartPanel, chartScale);
			DrawNoDataLabel(chartPanel);
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaManualAnchoredVWAP";
				Description = "Manual Orca anchored VWAP drawing tool with directional deviation bands and cloud rendering.";
				DrawingState = DrawingState.Building;

				StartAnchor = new ChartAnchor { DisplayName = "Anchor", IsEditing = true, DrawingTool = this };
				EndAnchor = new ChartAnchor { DisplayName = "End", IsEditing = true, DrawingTool = this };

				ExtendRight = true;
				PriceSource = OrcaAnchoredVwapPriceSource.HLC3;
				RespectTradingHoursTemplate = false;

				DeviationMode = OrcaAnchoredVwapDeviationMode.DirectionalSupportResistanceBands;
				ShowDeviation1 = true;
				ShowDeviation2 = true;
				ShowDeviation3 = true;
				DeviationMultiplier1 = 1.0;
				DeviationMultiplier2 = 2.0;
				DeviationMultiplier3 = 3.0;

				DirectionDetectionMethod = OrcaAnchoredVwapDirectionDetectionMethod.NetVwapChangeFromAnchor;
				SlopeLookbackBars = 20;
				DirectionThresholdTicks = 2;
				FlatDirectionFallback = OrcaAnchoredVwapFlatDirectionFallback.KeepLastDirection;
				ManualBandSide = OrcaAnchoredVwapBandSide.Both;

				EnableCloud = true;
				CloudMode = OrcaAnchoredVwapCloudMode.VwapToFirstActiveBand;
				CloudOpacity = 20;
				SupportCloudColor = WpfBrushes.MediumSeaGreen;
				ResistanceCloudColor = WpfBrushes.IndianRed;
				NeutralCloudColor = WpfBrushes.SteelBlue;

				ShowVwapLine = true;
				VwapStroke = new Stroke(WpfBrushes.DeepSkyBlue, DashStyleHelper.Solid, 2f);
				UpperBandStroke = new Stroke(WpfBrushes.IndianRed, DashStyleHelper.Dash, 1.25f);
				LowerBandStroke = new Stroke(WpfBrushes.MediumSeaGreen, DashStyleHelper.Dash, 1.25f);

				ShowVwapLabel = true;
				ShowDeviationLabels = true;
				ShowPriceLabels = true;
				LabelAlignment = OrcaAnchoredVwapLabelAlignment.RightEdge;
				LabelFontSize = 11f;
				LabelTextColor = WpfBrushes.WhiteSmoke;
			}
			else if (State == State.DataLoaded)
			{
				MarkVwapDirty();
			}
			else if (State == State.Terminated)
			{
				DisposeDxResources();
			}
		}

		protected override void Dispose(bool disposing)
		{
			DisposeDxResources();
			base.Dispose(disposing);
		}

		private EditMode GetEditModeForPoint(Point point, ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, bool useSensitivity)
		{
			if (StartAnchor == null || EndAnchor == null)
				return EditMode.None;

			Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			double startDistance = (startPoint - point).Length;
			double endDistance = (endPoint - point).Length;

			if (!useSensitivity || startDistance <= CursorSensitivity || endDistance <= CursorSensitivity)
				return startDistance <= endDistance ? EditMode.Start : EditMode.End;

			Vector lineVector = endPoint - startPoint;
			if (MathHelper.IsPointAlongVector(point, startPoint, lineVector, CursorSensitivity))
				return EditMode.MoveAll;

			return EditMode.None;
		}

		private void EnsureVwap(ChartControl chartControl, ChartScale chartScale)
		{
			noDataLabel = string.Empty;
			ChartBars chartBars = GetAttachedToChartBars();
			if (chartBars == null || chartBars.Bars == null || chartBars.Bars.Count <= 0)
			{
				ClearVwap("No chart bars");
				return;
			}

			Bars bars = chartBars.Bars;
			int chartBarsCount = Math.Min(chartBars.Count, bars.Count);
			if (chartBarsCount <= 0)
			{
				ClearVwap("No chart bars");
				return;
			}

			double tickSize = GetTickSize(bars);
			int startIndex = FindNearestBarIndex(bars, StartAnchor.Time);
			int endIndex = ExtendRight ? chartBarsCount - 1 : FindNearestBarIndex(bars, EndAnchor.Time);
			if (startIndex < 0 || endIndex < 0)
			{
				ClearVwap("No loaded bars in range");
				return;
			}

			double anchorPrice = StartAnchor.Price;
			DateTime anchorTime = StartAnchor.Time;
			DateTime endTime = ExtendRight ? bars.GetTime(endIndex) : EndAnchor.Time;

			if (!ExtendRight && endIndex < startIndex)
			{
				int swapIndex = startIndex;
				startIndex = endIndex;
				endIndex = swapIndex;
				anchorPrice = EndAnchor.Price;
				anchorTime = EndAnchor.Time;
				endTime = StartAnchor.Time;
			}

			startIndex = Math.Max(0, Math.Min(startIndex, chartBarsCount - 1));
			endIndex = Math.Max(0, Math.Min(endIndex, chartBarsCount - 1));
			if (startIndex > endIndex)
			{
				ClearVwap("No loaded bars in range");
				return;
			}

			DateTime lastRangeBarTime = bars.GetTime(endIndex);
			double lastRangeBarVolume = bars.GetVolume(endIndex);
			string settingsKey = BuildSettingsKey();

			if (!NeedsVwapRebuild(anchorTime, endTime, anchorPrice, startIndex, endIndex, chartBarsCount, lastRangeBarTime, lastRangeBarVolume, tickSize, settingsKey))
				return;

			BuildVwapSeries(bars, startIndex, endIndex, anchorPrice, tickSize);
			ResolveBandSide(tickSize);
			UpdateMinMax();

			cachedAnchorTime = anchorTime;
			cachedEndTime = endTime;
			cachedAnchorPrice = anchorPrice;
			cachedStartBar = startIndex;
			cachedEndBar = endIndex;
			cachedBarsCount = chartBarsCount;
			cachedLastRangeBarTime = lastRangeBarTime;
			cachedLastRangeBarVolume = lastRangeBarVolume;
			cachedTickSize = tickSize;
			cachedSettingsKey = settingsKey;
			vwapDirty = false;
		}

		private bool NeedsVwapRebuild(DateTime anchorTime, DateTime endTime, double anchorPrice, int startIndex, int endIndex, int barsCount, DateTime lastRangeBarTime, double lastRangeBarVolume, double tickSize, string settingsKey)
		{
			if (vwapDirty)
				return true;
			if (cachedAnchorTime != anchorTime || cachedEndTime != endTime)
				return true;
			if (Math.Abs(cachedAnchorPrice - anchorPrice) > PriceEpsilon)
				return true;
			if (cachedStartBar != startIndex || cachedEndBar != endIndex || cachedBarsCount != barsCount)
				return true;
			if (cachedLastRangeBarTime != lastRangeBarTime || Math.Abs(cachedLastRangeBarVolume - lastRangeBarVolume) > PriceEpsilon)
				return true;
			if (Math.Abs(cachedTickSize - tickSize) > PriceEpsilon)
				return true;
			if (cachedSettingsKey != settingsKey)
				return true;
			return false;
		}

		private void BuildVwapSeries(Bars bars, int startIndex, int endIndex, double anchorPrice, double tickSize)
		{
			vwapPoints.Clear();
			double sumVol = 0;
			double sumPriceVol = 0;
			double sumPrice2Vol = 0;
			double lastVwap = double.NaN;
			int segmentId = 0;

			for (int barIndex = startIndex; barIndex <= endIndex; barIndex++)
			{
				if (RespectTradingHoursTemplate && barIndex > startIndex && bars.IsFirstBarOfSessionByIndex(barIndex))
				{
					sumVol = 0;
					sumPriceVol = 0;
					sumPrice2Vol = 0;
					lastVwap = double.NaN;
					segmentId++;
				}

				double sourcePrice = GetPrice(bars, barIndex);
				double volume = Math.Max(0, bars.GetVolume(barIndex));

				if (volume > 0)
				{
					sumVol += volume;
					sumPriceVol += sourcePrice * volume;
					sumPrice2Vol += sourcePrice * sourcePrice * volume;
				}

				bool isFirstPoint = barIndex == startIndex || (RespectTradingHoursTemplate && bars.IsFirstBarOfSessionByIndex(barIndex));
				double vwap = double.NaN;
				double stdDev = double.NaN;

				if (isFirstPoint)
				{
					vwap = barIndex == startIndex ? anchorPrice : sourcePrice;
					stdDev = 0;
				}
				else if (sumVol > 0)
				{
					vwap = sumPriceVol / sumVol;
					double variance = (sumPrice2Vol / sumVol) - (vwap * vwap);
					stdDev = Math.Sqrt(Math.Max(0, variance));
				}
				else if (IsValid(lastVwap))
				{
					vwap = lastVwap;
				}

				VwapPoint point = new VwapPoint
				{
					BarIndex = barIndex,
					SegmentId = segmentId,
					Vwap = vwap,
					StdDev = stdDev,
					Upper1 = IsValid(stdDev) ? vwap + DeviationMultiplier1 * stdDev : double.NaN,
					Upper2 = IsValid(stdDev) ? vwap + DeviationMultiplier2 * stdDev : double.NaN,
					Upper3 = IsValid(stdDev) ? vwap + DeviationMultiplier3 * stdDev : double.NaN,
					Lower1 = IsValid(stdDev) ? vwap - DeviationMultiplier1 * stdDev : double.NaN,
					Lower2 = IsValid(stdDev) ? vwap - DeviationMultiplier2 * stdDev : double.NaN,
					Lower3 = IsValid(stdDev) ? vwap - DeviationMultiplier3 * stdDev : double.NaN
				};

				if (IsValid(vwap))
					lastVwap = vwap;

				vwapPoints.Add(point);
			}
		}

		private void ResolveBandSide(double tickSize)
		{
			if (DeviationMode == OrcaAnchoredVwapDeviationMode.Off)
			{
				activeBandSide = ResolvedBandSide.None;
				return;
			}

			if (DeviationMode == OrcaAnchoredVwapDeviationMode.AllBands)
			{
				activeBandSide = ResolvedBandSide.Both;
				return;
			}

			if (DeviationMode == OrcaAnchoredVwapDeviationMode.ManualSideSelection || DirectionDetectionMethod == OrcaAnchoredVwapDirectionDetectionMethod.ManualOverride)
			{
				activeBandSide = ConvertManualSide(ManualBandSide);
				return;
			}

			double threshold = Math.Max(0, DirectionThresholdTicks) * Math.Max(tickSize, PriceEpsilon);
			double current = GetLastValidVwap();
			double compare = double.NaN;

			if (DirectionDetectionMethod == OrcaAnchoredVwapDirectionDetectionMethod.RecentVwapSlope)
				compare = GetLookbackVwap(Math.Max(1, SlopeLookbackBars));
			else
				compare = GetFirstValidVwap();

			ResolvedBandSide resolved = ResolvedBandSide.None;
			if (IsValid(current) && IsValid(compare))
			{
				double delta = current - compare;
				if (delta > threshold)
					resolved = ResolvedBandSide.LowerOnly;
				else if (delta < -threshold)
					resolved = ResolvedBandSide.UpperOnly;
			}

			if (resolved == ResolvedBandSide.None)
				resolved = ResolveFlatFallback();
			else
				lastDirectionalSide = resolved;

			activeBandSide = resolved;
		}

		private ResolvedBandSide ResolveFlatFallback()
		{
			switch (FlatDirectionFallback)
			{
				case OrcaAnchoredVwapFlatDirectionFallback.ShowBoth:
					return ResolvedBandSide.Both;
				case OrcaAnchoredVwapFlatDirectionFallback.ShowNone:
					return ResolvedBandSide.None;
				case OrcaAnchoredVwapFlatDirectionFallback.UseManualBandSide:
					return ConvertManualSide(ManualBandSide);
				default:
					return lastDirectionalSide != ResolvedBandSide.None ? lastDirectionalSide : ResolvedBandSide.Both;
			}
		}

		private ResolvedBandSide ConvertManualSide(OrcaAnchoredVwapBandSide side)
		{
			switch (side)
			{
				case OrcaAnchoredVwapBandSide.UpperBandsOnly:
					return ResolvedBandSide.UpperOnly;
				case OrcaAnchoredVwapBandSide.LowerBandsOnly:
					return ResolvedBandSide.LowerOnly;
				default:
					return ResolvedBandSide.Both;
			}
		}

		private double GetFirstValidVwap()
		{
			for (int index = 0; index < vwapPoints.Count; index++)
				if (IsValid(vwapPoints[index].Vwap))
					return vwapPoints[index].Vwap;
			return double.NaN;
		}

		private double GetLastValidVwap()
		{
			for (int index = vwapPoints.Count - 1; index >= 0; index--)
				if (IsValid(vwapPoints[index].Vwap))
					return vwapPoints[index].Vwap;
			return double.NaN;
		}

		private double GetLookbackVwap(int lookbackBars)
		{
			int validSeen = 0;
			for (int index = vwapPoints.Count - 1; index >= 0; index--)
			{
				if (!IsValid(vwapPoints[index].Vwap))
					continue;
				if (validSeen >= lookbackBars)
					return vwapPoints[index].Vwap;
				validSeen++;
			}
			return GetFirstValidVwap();
		}

		private void UpdateMinMax()
		{
			cachedMinValue = double.NaN;
			cachedMaxValue = double.NaN;

			for (int index = 0; index < vwapPoints.Count; index++)
			{
				IncludeMinMax(vwapPoints[index].Vwap);
				if (DeviationMode == OrcaAnchoredVwapDeviationMode.Off)
					continue;

				if (ShouldShowUpperBands())
				{
					if (ShowDeviation1) IncludeMinMax(vwapPoints[index].Upper1);
					if (ShowDeviation2) IncludeMinMax(vwapPoints[index].Upper2);
					if (ShowDeviation3) IncludeMinMax(vwapPoints[index].Upper3);
				}
				if (ShouldShowLowerBands())
				{
					if (ShowDeviation1) IncludeMinMax(vwapPoints[index].Lower1);
					if (ShowDeviation2) IncludeMinMax(vwapPoints[index].Lower2);
					if (ShowDeviation3) IncludeMinMax(vwapPoints[index].Lower3);
				}
			}
		}

		private void IncludeMinMax(double value)
		{
			if (!IsValid(value))
				return;

			if (double.IsNaN(cachedMinValue) || value < cachedMinValue)
				cachedMinValue = value;
			if (double.IsNaN(cachedMaxValue) || value > cachedMaxValue)
				cachedMaxValue = value;
		}

		private void DrawSeriesLine(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, int lineId, Stroke stroke)
		{
			if (stroke == null || vwapPoints.Count < 1)
				return;

			stroke.RenderTarget = RenderTarget;
			SharpDX.Direct2D1.Brush brush = IsSelected ? chartControl.SelectionBrush : stroke.BrushDX;
			if (brush == null)
				return;

			ChartBars chartBars = GetAttachedToChartBars();
			if (chartBars == null)
				return;

			bool hasPrevious = false;
			VwapPoint previous = new VwapPoint();
			DxVector2 previousPoint = new DxVector2();

			for (int index = 0; index < vwapPoints.Count; index++)
			{
				VwapPoint point = vwapPoints[index];
				double value = GetLineValue(point, lineId);
				if (!IsValid(value))
				{
					hasPrevious = false;
					continue;
				}

				DxVector2 renderPoint = GetRenderPoint(chartControl, chartBars, chartScale, point.BarIndex, value);
				if (hasPrevious && previous.SegmentId == point.SegmentId)
					RenderTarget.DrawLine(previousPoint, renderPoint, brush, stroke.Width, stroke.StrokeStyle);

				previous = point;
				previousPoint = renderPoint;
				hasPrevious = true;
			}
		}

		private void DrawClouds(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
		{
			if (ShouldShowLowerBands())
				DrawSideCloud(chartControl, chartPanel, chartScale, false);
			if (ShouldShowUpperBands())
				DrawSideCloud(chartControl, chartPanel, chartScale, true);
		}

		private void DrawSideCloud(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, bool upper)
		{
			List<int> bandIds = GetActiveBandIds(upper);
			if (bandIds.Count == 0)
				return;

			int firstBand = bandIds[0];
			int secondBand = bandIds.Count > 1 ? bandIds[1] : 0;
			int deepestBand = bandIds[bandIds.Count - 1];
			int lineA;
			int lineB;

			if (CloudMode == OrcaAnchoredVwapCloudMode.BetweenFirstAndSecondActiveBands && secondBand != 0)
			{
				lineA = firstBand;
				lineB = secondBand;
			}
			else if (CloudMode == OrcaAnchoredVwapCloudMode.FullSelectedDeviationZone)
			{
				lineA = 0;
				lineB = deepestBand;
			}
			else
			{
				lineA = 0;
				lineB = firstBand;
			}

			SharpDX.Direct2D1.SolidColorBrush fillBrush = SelectCloudBrush(upper);
			if (fillBrush == null)
				return;

			DrawCloudBetweenLines(chartControl, chartScale, lineA, lineB, fillBrush);
		}

		private void DrawCloudBetweenLines(ChartControl chartControl, ChartScale chartScale, int lineA, int lineB, SharpDX.Direct2D1.SolidColorBrush fillBrush)
		{
			ChartBars chartBars = GetAttachedToChartBars();
			if (chartBars == null)
				return;

			cloudUpperPoints.Clear();
			cloudLowerPoints.Clear();
			int currentSegment = -1;

			for (int index = 0; index < vwapPoints.Count; index++)
			{
				VwapPoint point = vwapPoints[index];
				double valueA = GetLineValue(point, lineA);
				double valueB = GetLineValue(point, lineB);
				if (!IsValid(valueA) || !IsValid(valueB))
				{
					FlushCloud(fillBrush);
					currentSegment = -1;
					continue;
				}

				if (currentSegment != -1 && currentSegment != point.SegmentId)
					FlushCloud(fillBrush);
				currentSegment = point.SegmentId;

				cloudUpperPoints.Add(GetRenderPoint(chartControl, chartBars, chartScale, point.BarIndex, valueA));
				cloudLowerPoints.Add(GetRenderPoint(chartControl, chartBars, chartScale, point.BarIndex, valueB));
			}

			FlushCloud(fillBrush);
		}

		private void FlushCloud(SharpDX.Direct2D1.SolidColorBrush fillBrush)
		{
			if (cloudUpperPoints.Count < 2 || cloudLowerPoints.Count < 2 || fillBrush == null)
			{
				cloudUpperPoints.Clear();
				cloudLowerPoints.Clear();
				return;
			}

			using (PathGeometry geometry = new PathGeometry(RenderTarget.Factory))
			{
				using (GeometrySink sink = geometry.Open())
				{
					sink.BeginFigure(cloudUpperPoints[0], FigureBegin.Filled);
					for (int index = 1; index < cloudUpperPoints.Count; index++)
						sink.AddLine(cloudUpperPoints[index]);
					for (int index = cloudLowerPoints.Count - 1; index >= 0; index--)
						sink.AddLine(cloudLowerPoints[index]);
					sink.EndFigure(FigureEnd.Closed);
					sink.Close();
				}

				RenderTarget.FillGeometry(geometry, fillBrush);
			}

			cloudUpperPoints.Clear();
			cloudLowerPoints.Clear();
		}

		private void DrawLabels(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
		{
			if (labelTextFormatDx == null || labelBrushDx == null)
				return;

			if (ShowVwapLabel)
				DrawLineLabel(chartControl, chartPanel, chartScale, 0, "AVWAP");

			if (DeviationMode == OrcaAnchoredVwapDeviationMode.Off || !ShowDeviationLabels)
				return;

			if (ShouldShowUpperBands())
			{
				if (ShowDeviation1) DrawLineLabel(chartControl, chartPanel, chartScale, 1, "+" + FormatMultiplier(DeviationMultiplier1) + " SD");
				if (ShowDeviation2) DrawLineLabel(chartControl, chartPanel, chartScale, 2, "+" + FormatMultiplier(DeviationMultiplier2) + " SD");
				if (ShowDeviation3) DrawLineLabel(chartControl, chartPanel, chartScale, 3, "+" + FormatMultiplier(DeviationMultiplier3) + " SD");
			}

			if (ShouldShowLowerBands())
			{
				if (ShowDeviation1) DrawLineLabel(chartControl, chartPanel, chartScale, -1, "-" + FormatMultiplier(DeviationMultiplier1) + " SD");
				if (ShowDeviation2) DrawLineLabel(chartControl, chartPanel, chartScale, -2, "-" + FormatMultiplier(DeviationMultiplier2) + " SD");
				if (ShowDeviation3) DrawLineLabel(chartControl, chartPanel, chartScale, -3, "-" + FormatMultiplier(DeviationMultiplier3) + " SD");
			}
		}

		private void DrawLineLabel(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, int lineId, string label)
		{
			VwapPoint point;
			double value;
			if (!TryGetLabelPoint(lineId, out point, out value))
				return;

			ChartBars chartBars = GetAttachedToChartBars();
			if (chartBars == null)
				return;

			float y = chartScale.GetYByValue(value);
			if (y < chartPanel.Y - 20 || y > chartPanel.Y + chartPanel.H + 20)
				return;

			string text = ShowPriceLabels ? label + " " + FormatPrice(value) : label;
			float width = Math.Max(58f, EstimateTextWidth(text, LabelFontSize) + 8f);
			float height = Math.Max(14f, LabelFontSize + 4f);
			float x = ResolveLabelX(chartControl, chartPanel, chartBars, point.BarIndex, width);
			DxRectangleF rect = new DxRectangleF(x, y - (height * 0.5f), width, height);
			RenderTarget.DrawText(text, labelTextFormatDx, rect, labelBrushDx);
		}

		private bool TryGetLabelPoint(int lineId, out VwapPoint point, out double value)
		{
			point = new VwapPoint();
			value = double.NaN;

			int fromIndex = 0;
			int toIndex = int.MaxValue;
			ChartBars chartBars = GetAttachedToChartBars();
			if (chartBars != null && LabelAlignment == OrcaAnchoredVwapLabelAlignment.LastVisibleBar)
			{
				fromIndex = Math.Max(0, chartBars.FromIndex);
				toIndex = Math.Max(fromIndex, chartBars.ToIndex);
			}

			for (int index = vwapPoints.Count - 1; index >= 0; index--)
			{
				VwapPoint candidate = vwapPoints[index];
				if (candidate.BarIndex < fromIndex || candidate.BarIndex > toIndex)
					continue;

				double candidateValue = GetLineValue(candidate, lineId);
				if (!IsValid(candidateValue))
					continue;

				point = candidate;
				value = candidateValue;
				return true;
			}

			for (int index = vwapPoints.Count - 1; index >= 0; index--)
			{
				double candidateValue = GetLineValue(vwapPoints[index], lineId);
				if (!IsValid(candidateValue))
					continue;

				point = vwapPoints[index];
				value = candidateValue;
				return true;
			}

			return false;
		}

		private float ResolveLabelX(ChartControl chartControl, ChartPanel chartPanel, ChartBars chartBars, int barIndex, float labelWidth)
		{
			float panelLeft = chartPanel.X;
			float panelRight = chartPanel.X + chartPanel.W;
			if (LabelAlignment == OrcaAnchoredVwapLabelAlignment.RightEdge)
				return Math.Max(panelLeft, panelRight - labelWidth - LabelPaddingPx);

			float x = chartControl.GetXByBarIndex(chartBars, Math.Max(0, Math.Min(barIndex, chartBars.Count - 1))) + LabelPaddingPx;
			if (x + labelWidth > panelRight)
				x = panelRight - labelWidth - LabelPaddingPx;
			if (x < panelLeft)
				x = panelLeft + LabelPaddingPx;
			return x;
		}

		private void DrawNoDataLabel(ChartPanel chartPanel)
		{
			if (string.IsNullOrEmpty(noDataLabel) || labelTextFormatDx == null || labelBrushDx == null)
				return;

			RenderTarget.DrawText(noDataLabel, labelTextFormatDx, new DxRectangleF(chartPanel.X + 8f, chartPanel.Y + 8f, 220f, 18f), labelBrushDx);
		}

		private void DrawAnchorGuide(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
		{
			if (StartAnchor == null || EndAnchor == null || VwapStroke == null)
				return;

			VwapStroke.RenderTarget = RenderTarget;
			SharpDX.Direct2D1.Brush brush = VwapStroke.BrushDX ?? chartControl.SelectionBrush;
			if (brush == null)
				return;

			Point start = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point end = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			RenderTarget.DrawLine(ToDx(start), ToDx(end), brush, Math.Max(1f, VwapStroke.Width), VwapStroke.StrokeStyle);
		}

		private void DrawHitTest(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
		{
			Point start = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point end = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			RenderTarget.DrawLine(ToDx(start), ToDx(end), chartControl.SelectionBrush, 8f);

			if (DrawingState == DrawingState.Building)
				return;

			EnsureVwap(chartControl, chartScale);
			ChartBars chartBars = GetAttachedToChartBars();
			if (chartBars == null || vwapPoints.Count < 2)
				return;

			bool hasPrevious = false;
			VwapPoint previous = new VwapPoint();
			DxVector2 previousPoint = new DxVector2();
			for (int index = 0; index < vwapPoints.Count; index++)
			{
				VwapPoint point = vwapPoints[index];
				if (!IsValid(point.Vwap))
				{
					hasPrevious = false;
					continue;
				}

				DxVector2 renderPoint = GetRenderPoint(chartControl, chartBars, chartScale, point.BarIndex, point.Vwap);
				if (hasPrevious && previous.SegmentId == point.SegmentId)
					RenderTarget.DrawLine(previousPoint, renderPoint, chartControl.SelectionBrush, 8f);

				previous = point;
				previousPoint = renderPoint;
				hasPrevious = true;
			}
		}

		private DxVector2 GetRenderPoint(ChartControl chartControl, ChartBars chartBars, ChartScale chartScale, int barIndex, double value)
		{
			int safeIndex = Math.Max(0, Math.Min(barIndex, chartBars.Count - 1));
			return new DxVector2(chartControl.GetXByBarIndex(chartBars, safeIndex), chartScale.GetYByValue(value));
		}

		private DxVector2 ToDx(Point point)
		{
			return new DxVector2((float)point.X, (float)point.Y);
		}

		private double GetLineValue(VwapPoint point, int lineId)
		{
			switch (lineId)
			{
				case 1: return point.Upper1;
				case 2: return point.Upper2;
				case 3: return point.Upper3;
				case -1: return point.Lower1;
				case -2: return point.Lower2;
				case -3: return point.Lower3;
				default: return point.Vwap;
			}
		}

		private List<int> GetActiveBandIds(bool upper)
		{
			List<int> ids = new List<int>();
			if (ShowDeviation1) ids.Add(upper ? 1 : -1);
			if (ShowDeviation2) ids.Add(upper ? 2 : -2);
			if (ShowDeviation3) ids.Add(upper ? 3 : -3);
			ids.Sort(delegate(int left, int right)
			{
				return GetMultiplier(Math.Abs(left)).CompareTo(GetMultiplier(Math.Abs(right)));
			});
			return ids;
		}

		private double GetMultiplier(int id)
		{
			switch (id)
			{
				case 1: return DeviationMultiplier1;
				case 2: return DeviationMultiplier2;
				default: return DeviationMultiplier3;
			}
		}

		private SharpDX.Direct2D1.SolidColorBrush SelectCloudBrush(bool upper)
		{
			if (activeBandSide == ResolvedBandSide.Both)
				return neutralCloudBrushDx;
			return upper ? resistanceCloudBrushDx : supportCloudBrushDx;
		}

		private bool ShouldShowUpperBands()
		{
			return activeBandSide == ResolvedBandSide.UpperOnly || activeBandSide == ResolvedBandSide.Both;
		}

		private bool ShouldShowLowerBands()
		{
			return activeBandSide == ResolvedBandSide.LowerOnly || activeBandSide == ResolvedBandSide.Both;
		}

		private int FindNearestBarIndex(Bars bars, DateTime time)
		{
			if (bars == null || bars.Count <= 0)
				return -1;

			if (time <= bars.GetTime(0))
				return 0;
			if (time >= bars.GetTime(bars.Count - 1))
				return bars.Count - 1;

			int low = 0;
			int high = bars.Count - 1;
			int result = bars.Count - 1;
			while (low <= high)
			{
				int mid = low + ((high - low) / 2);
				DateTime barTime = bars.GetTime(mid);
				if (barTime >= time)
				{
					result = mid;
					high = mid - 1;
				}
				else
				{
					low = mid + 1;
				}
			}

			int previous = Math.Max(0, result - 1);
			TimeSpan previousDistance = time - bars.GetTime(previous);
			TimeSpan nextDistance = bars.GetTime(result) - time;
			return previousDistance <= nextDistance ? previous : result;
		}

		private double GetPrice(Bars bars, int barIndex)
		{
			double high = bars.GetHigh(barIndex);
			double low = bars.GetLow(barIndex);
			double close = bars.GetClose(barIndex);
			switch (PriceSource)
			{
				case OrcaAnchoredVwapPriceSource.Close:
					return close;
				case OrcaAnchoredVwapPriceSource.HL2:
					return (high + low) / 2.0;
				case OrcaAnchoredVwapPriceSource.OHLC4:
					return (bars.GetOpen(barIndex) + high + low + close) / 4.0;
				default:
					return (high + low + close) / 3.0;
			}
		}

		private double GetTickSize(Bars bars)
		{
			if (AttachedTo != null && AttachedTo.Instrument != null && AttachedTo.Instrument.MasterInstrument != null && AttachedTo.Instrument.MasterInstrument.TickSize > 0)
				return AttachedTo.Instrument.MasterInstrument.TickSize;
			if (bars != null && bars.Instrument != null && bars.Instrument.MasterInstrument != null && bars.Instrument.MasterInstrument.TickSize > 0)
				return bars.Instrument.MasterInstrument.TickSize;
			return 0.01;
		}

		private string FormatPrice(double price)
		{
			ChartBars chartBars = GetAttachedToChartBars();
			if (chartBars != null && chartBars.Bars != null && chartBars.Bars.Instrument != null && chartBars.Bars.Instrument.MasterInstrument != null)
				return chartBars.Bars.Instrument.MasterInstrument.FormatPrice(price);
			if (AttachedTo != null && AttachedTo.Instrument != null && AttachedTo.Instrument.MasterInstrument != null)
				return AttachedTo.Instrument.MasterInstrument.FormatPrice(price);
			return price.ToString("0.00");
		}

		private string FormatMultiplier(double multiplier)
		{
			return multiplier.ToString("0.##");
		}

		private float EstimateTextWidth(string text, float fontSize)
		{
			if (string.IsNullOrEmpty(text))
				return fontSize;
			return Math.Max(fontSize, text.Length * fontSize * 0.58f);
		}

		private string BuildSettingsKey()
		{
			return ExtendRight.ToString() + "|"
				+ PriceSource.ToString() + "|"
				+ RespectTradingHoursTemplate.ToString() + "|"
				+ DeviationMode.ToString() + "|"
				+ ShowDeviation1.ToString() + "|" + ShowDeviation2.ToString() + "|" + ShowDeviation3.ToString() + "|"
				+ DeviationMultiplier1.ToString("0.########") + "|" + DeviationMultiplier2.ToString("0.########") + "|" + DeviationMultiplier3.ToString("0.########") + "|"
				+ DirectionDetectionMethod.ToString() + "|"
				+ SlopeLookbackBars.ToString() + "|"
				+ DirectionThresholdTicks.ToString() + "|"
				+ FlatDirectionFallback.ToString() + "|"
				+ ManualBandSide.ToString();
		}

		private string BuildBrushSignature()
		{
			return Serialize.BrushToString(SupportCloudColor) + "|"
				+ Serialize.BrushToString(ResistanceCloudColor) + "|"
				+ Serialize.BrushToString(NeutralCloudColor) + "|"
				+ Serialize.BrushToString(LabelTextColor) + "|"
				+ CloudOpacity.ToString() + "|"
				+ LabelFontSize.ToString("0.###");
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null)
				return;

			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxResourceRenderTarget != IntPtr.Zero && dxResourceRenderTarget != currentTarget)
				DisposeDxResources();

			string brushSignature = BuildBrushSignature();
			if (brushSignature != lastBrushSignature || lastCloudOpacity != CloudOpacity || Math.Abs(lastLabelFontSize - LabelFontSize) > 0.0001f)
				DisposeDxResources();

			float cloudOpacity = Math.Max(0, Math.Min(100, CloudOpacity)) / 100f;
			if (supportCloudBrushDx == null) supportCloudBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(SupportCloudColor, cloudOpacity));
			if (resistanceCloudBrushDx == null) resistanceCloudBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(ResistanceCloudColor, cloudOpacity));
			if (neutralCloudBrushDx == null) neutralCloudBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(NeutralCloudColor, cloudOpacity));
			if (labelBrushDx == null) labelBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(LabelTextColor, 1f));
			if (labelTextFormatDx == null)
			{
				labelTextFormatDx = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)Math.Max(6.0, LabelFontSize));
				labelTextFormatDx.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				labelTextFormatDx.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
			}

			lastBrushSignature = brushSignature;
			lastCloudOpacity = CloudOpacity;
			lastLabelFontSize = LabelFontSize;
			dxResourceRenderTarget = currentTarget;
		}

		private DxColor4 ToDxColor(WpfBrush brush, float opacity)
		{
			WpfSolidColorBrush solidBrush = brush as WpfSolidColorBrush;
			System.Windows.Media.Color color = solidBrush != null ? solidBrush.Color : WpfColors.White;
			return new DxColor4(color.R / 255f, color.G / 255f, color.B / 255f, (color.A / 255f) * opacity);
		}

		private void DisposeDxResources()
		{
			if (supportCloudBrushDx != null) { supportCloudBrushDx.Dispose(); supportCloudBrushDx = null; }
			if (resistanceCloudBrushDx != null) { resistanceCloudBrushDx.Dispose(); resistanceCloudBrushDx = null; }
			if (neutralCloudBrushDx != null) { neutralCloudBrushDx.Dispose(); neutralCloudBrushDx = null; }
			if (labelBrushDx != null) { labelBrushDx.Dispose(); labelBrushDx = null; }
			if (labelTextFormatDx != null) { labelTextFormatDx.Dispose(); labelTextFormatDx = null; }
			lastBrushSignature = string.Empty;
			lastCloudOpacity = -1;
			lastLabelFontSize = -1f;
			dxResourceRenderTarget = IntPtr.Zero;
		}

		private void ClearVwap(string message)
		{
			vwapPoints.Clear();
			noDataLabel = message;
			cachedMinValue = double.NaN;
			cachedMaxValue = double.NaN;
			vwapDirty = true;
		}

		private void MarkVwapDirty()
		{
			vwapDirty = true;
		}

		private static bool IsValid(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		[NinjaScriptProperty]
		[Display(Name = "Extend Right", Order = 1, GroupName = "1. Calculation")]
		public bool ExtendRight { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Price Source", Order = 2, GroupName = "1. Calculation")]
		public OrcaAnchoredVwapPriceSource PriceSource { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Respect Trading Hours Template / Session Boundaries", Order = 3, GroupName = "1. Calculation")]
		public bool RespectTradingHoursTemplate { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Deviation Mode", Order = 1, GroupName = "2. Deviations")]
		public OrcaAnchoredVwapDeviationMode DeviationMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Deviation 1", Order = 2, GroupName = "2. Deviations")]
		public bool ShowDeviation1 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Deviation 1 Multiplier", Order = 3, GroupName = "2. Deviations")]
		[Range(0.01, double.MaxValue)]
		public double DeviationMultiplier1 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Deviation 2", Order = 4, GroupName = "2. Deviations")]
		public bool ShowDeviation2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Deviation 2 Multiplier", Order = 5, GroupName = "2. Deviations")]
		[Range(0.01, double.MaxValue)]
		public double DeviationMultiplier2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Deviation 3", Order = 6, GroupName = "2. Deviations")]
		public bool ShowDeviation3 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Deviation 3 Multiplier", Order = 7, GroupName = "2. Deviations")]
		[Range(0.01, double.MaxValue)]
		public double DeviationMultiplier3 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Direction Detection Method", Order = 1, GroupName = "3. Directional Bands")]
		public OrcaAnchoredVwapDirectionDetectionMethod DirectionDetectionMethod { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Slope Lookback Bars", Order = 2, GroupName = "3. Directional Bands")]
		public int SlopeLookbackBars { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000)]
		[Display(Name = "Direction Threshold Ticks", Order = 3, GroupName = "3. Directional Bands")]
		public int DirectionThresholdTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Flat Direction Fallback", Order = 4, GroupName = "3. Directional Bands")]
		public OrcaAnchoredVwapFlatDirectionFallback FlatDirectionFallback { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Manual Band Side", Order = 5, GroupName = "3. Directional Bands")]
		public OrcaAnchoredVwapBandSide ManualBandSide { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Cloud", Order = 1, GroupName = "4. Cloud")]
		public bool EnableCloud { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Cloud Mode", Order = 2, GroupName = "4. Cloud")]
		public OrcaAnchoredVwapCloudMode CloudMode { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Cloud Opacity", Order = 3, GroupName = "4. Cloud")]
		public int CloudOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Support Cloud Color", Order = 4, GroupName = "4. Cloud")]
		public WpfBrush SupportCloudColor { get; set; }

		[Browsable(false)]
		public string SupportCloudColorSerialize
		{
			get { return Serialize.BrushToString(SupportCloudColor); }
			set { SupportCloudColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Resistance Cloud Color", Order = 5, GroupName = "4. Cloud")]
		public WpfBrush ResistanceCloudColor { get; set; }

		[Browsable(false)]
		public string ResistanceCloudColorSerialize
		{
			get { return Serialize.BrushToString(ResistanceCloudColor); }
			set { ResistanceCloudColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Neutral Cloud Color", Order = 6, GroupName = "4. Cloud")]
		public WpfBrush NeutralCloudColor { get; set; }

		[Browsable(false)]
		public string NeutralCloudColorSerialize
		{
			get { return Serialize.BrushToString(NeutralCloudColor); }
			set { NeutralCloudColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "Show VWAP Line", Order = 1, GroupName = "5. Lines")]
		public bool ShowVwapLine { get; set; }

		[Display(Name = "VWAP Stroke", Order = 2, GroupName = "5. Lines")]
		public Stroke VwapStroke { get; set; }

		[Display(Name = "Upper Band Stroke", Order = 3, GroupName = "5. Lines")]
		public Stroke UpperBandStroke { get; set; }

		[Display(Name = "Lower Band Stroke", Order = 4, GroupName = "5. Lines")]
		public Stroke LowerBandStroke { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VWAP Label", Order = 1, GroupName = "6. Labels")]
		public bool ShowVwapLabel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Deviation Labels", Order = 2, GroupName = "6. Labels")]
		public bool ShowDeviationLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Price Labels", Order = 3, GroupName = "6. Labels")]
		public bool ShowPriceLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Label Alignment", Order = 4, GroupName = "6. Labels")]
		public OrcaAnchoredVwapLabelAlignment LabelAlignment { get; set; }

		[NinjaScriptProperty]
		[Range(6.0, 30.0)]
		[Display(Name = "Label Font Size", Order = 5, GroupName = "6. Labels")]
		public float LabelFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Label Text Color", Order = 6, GroupName = "6. Labels")]
		public WpfBrush LabelTextColor { get; set; }

		[Browsable(false)]
		public string LabelTextColorSerialize
		{
			get { return Serialize.BrushToString(LabelTextColor); }
			set { LabelTextColor = Serialize.StringToBrush(value); }
		}
	}
}
