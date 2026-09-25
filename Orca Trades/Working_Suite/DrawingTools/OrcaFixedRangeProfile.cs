#region Using declarations
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
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
	public enum OrcaFixedRangeProfileSide
	{
		Left,
		Right
	}

	public enum OrcaFixedRangeAggregationMode
	{
		RowCount,
		TicksPerRow,
		Dynamic
	}

	public enum OrcaFixedRangeProfileDataMode
	{
		TrueVolumeAtPrice,
		EstimatedFromBars
	}

	public enum OrcaFixedRangeProfileDataSourcePreference
	{
		ChartLocalOnly,
		ChartLocalThenMaster,
		MasterThenChartLocal
	}

	public enum OrcaFixedRangeProfilePlacement
	{
		InsideSelectedBox,
		OutsideRightEdge,
		OutsideLeftEdge
	}

	public enum OrcaFixedRangeProfileSideArrangement
	{
		ManualSideSettings,
		VolumeRightDeltaLeft,
		VolumeLeftDeltaRight
	}

	public enum OrcaFixedRangeVALineStyle
	{
		Solid,
		Dash,
		Dot,
		DashDot
	}

	public enum OrcaFixedRangeStatisticsPosition
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public enum OrcaFixedRangeFontWeight
	{
		Light,
		Normal,
		Medium,
		SemiBold,
		Bold
	}
}

namespace NinjaTrader.NinjaScript.DrawingTools
{
	public class OrcaFixedRangeProfile : DrawingTool
	{
		private const double PriceEpsilon = 1E-09;
		private const double CursorSensitivity = 15.0;
		private const float BoxPaddingPx = 3f;
		private const float OutsideProfileGapPx = 6f;
		private const float StatisticsOutsideGapPx = 6f;
		private const float TrackGapPx = 3f;

		private enum ResizeMode
		{
			None,
			TopLeft,
			TopRight,
			BottomLeft,
			BottomRight,
			MoveAll
		}

		private struct ProfileTrack
		{
			public bool IsVisible;
			public float Left;
			public float Right;
			public bool DrawFromRight;
		}

		private ChartAnchor editingLeftAnchor;
		private ChartAnchor editingTopAnchor;
		private ChartAnchor editingBottomAnchor;
		private ChartAnchor editingRightAnchor;
		private ChartAnchor lastMouseMoveDataPoint;
		private ChartAnchor lastBuildEndDataPoint;
		private ResizeMode resizeMode;

		private OrcaVolumeProfileResult profileResult;
		private OrcaVolumeProfileResult deltaResult;
		private bool profileDirty = true;
		private string noDataLabel = string.Empty;
		private string totalVolumeLabel = string.Empty;
		private string dataSourceLabel = string.Empty;
		private string statisticsLabel = string.Empty;
		private long statisticsTotalVolume;
		private long statisticsTotalDelta;
		private long statisticsFinishDelta;
		private bool statisticsHasRealDelta;
		private bool statisticsFinishKnown;
		private double statisticsDeltaPercent;
		private double statisticsPointRange;
		private TimeSpan statisticsDuration;

		private DateTime cachedStartTime = DateTime.MinValue;
		private DateTime cachedEndTime = DateTime.MinValue;
		private double cachedLowPrice = double.NaN;
		private double cachedHighPrice = double.NaN;
		private int cachedFirstBar = -1;
		private int cachedLastBar = -1;
		private int cachedBarsCount = -1;
		private DateTime cachedLastRangeBarTime = DateTime.MinValue;
		private double cachedLastRangeBarVolume = double.NaN;
		private string cachedDataKey = string.Empty;
		private int cachedTrueDataRevision = -1;
		private int cachedRowCount = -1;
		private int cachedTicksPerRow = -1;
		private int cachedResolvedTicksPerRow = -1;
		private int cachedDynamicRowMinPixels = -1;
		private int cachedDeltaRowCount = -1;
		private int cachedDeltaTicksPerRow = -1;
		private int cachedResolvedDeltaTicksPerRow = -1;
		private int cachedDeltaDynamicRowMinPixels = -1;
		private int cachedDeltaDynamicMinCompression = -1;
		private int cachedDeltaDynamicMaxCompression = -1;
		private double cachedValueAreaPercent = double.NaN;
		private double cachedDynamicAggregationMultiplier = double.NaN;
		private double cachedDeltaDynamicAggregationMultiplier = double.NaN;
		private double cachedTickSize = double.NaN;
		private OrcaFixedRangeAggregationMode cachedAggregationMode = (OrcaFixedRangeAggregationMode)(-1);
		private OrcaFixedRangeAggregationMode cachedDeltaAggregationMode = (OrcaFixedRangeAggregationMode)(-1);
		private OrcaFixedRangeProfileDataMode cachedProfileDataMode = (OrcaFixedRangeProfileDataMode)(-1);
		private OrcaFixedRangeProfileDataSourcePreference cachedDataSourcePreference = (OrcaFixedRangeProfileDataSourcePreference)(-1);
		private bool volumetricAccessResolved;
		private Type volumetricResolvedType;
		private bool volumetricAccessAvailable;
		private PropertyInfo volumetricVolumesProperty;
		private FieldInfo volumetricVolumesField;
		private MethodInfo volumetricAskMethod;
		private MethodInfo volumetricBidMethod;
		private MethodInfo volumetricTotalAtPriceMethod;
		private PropertyInfo volumetricBuyingProperty;
		private PropertyInfo volumetricSellingProperty;
		private PropertyInfo volumetricBarVolumeProperty;
		private readonly object[] volumetricInvokeArgs = new object[1];
		private bool cachedAllowEstimatedChartFallback;

		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private SharpDX.Direct2D1.SolidColorBrush pocBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush vaFillBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush vaLineBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush upBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush downBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaPositiveBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaNegativeBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaNeutralBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush[] positiveDeltaIntensityBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] negativeDeltaIntensityBrushes;
		private SharpDX.Direct2D1.SolidColorBrush deltaPositiveLabelBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaNegativeLabelBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush textBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush boxFillBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush statisticsTextBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush statisticsBackgroundBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush trendLineBrushDx;
		private StrokeStyle vaLineStrokeDx;
		private StrokeStyle trendLineStrokeDx;
		private SharpDX.Direct2D1.SolidColorBrush[] upGradientBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] downGradientBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] vaGradientBrushes;
		private TextFormat textFormatDx;
		private TextFormat volumeLabelTextFormatDx;
		private TextFormat deltaLabelTextFormatDx;
		private TextFormat statisticsTextFormatDx;
		private int lastBuiltGradientSteps = -1;
		private float lastBuiltMinBrightness = -1f;
		private int lastBuiltProfileOpacity = -1;
		private int lastBuiltDeltaIntensitySteps = -1;
		private int lastBuiltDeltaIntensityProfileOpacity = -1;
		private int lastBuiltBoxFillOpacity = -1;
		private int lastBuiltStatisticsBackgroundOpacity = -1;
		private int lastBuiltTrendLineOpacity = -1;
		private float lastBuiltStatisticsFontSize = -1f;
		private string lastBuiltStatisticsFontFamily = string.Empty;
		private OrcaFixedRangeFontWeight lastBuiltStatisticsFontWeight = (OrcaFixedRangeFontWeight)(-1);
		private OrcaFixedRangeVALineStyle lastBuiltVALineStyle = (OrcaFixedRangeVALineStyle)(-1);
		private OrcaFixedRangeVALineStyle lastBuiltTrendLineStyle = (OrcaFixedRangeVALineStyle)(-1);
		private string lastBrushSignature = string.Empty;

		public override object Icon
		{
			get { return Icons.DrawRectangle; }
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

			if (!IsVisible || StartAnchor == null || EndAnchor == null || (StartAnchor.IsEditing && EndAnchor.IsEditing))
				return;

			MinValue = Math.Min(StartAnchor.Price, EndAnchor.Price);
			MaxValue = Math.Max(StartAnchor.Price, EndAnchor.Price);
		}

		public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
		{
			yield return new AlertConditionItem
			{
				Name = "Orca fixed range profile",
				ShouldOnlyDisplayName = true
			};
		}

		public override IEnumerable<Condition> GetValidAlertConditions()
		{
			return new Condition[] { Condition.CrossInside, Condition.CrossOutside };
		}

		public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
		{
			if (values == null || values.Length == 0 || StartAnchor == null || EndAnchor == null)
				return false;

			double minPrice = Math.Min(StartAnchor.Price, EndAnchor.Price);
			double maxPrice = Math.Max(StartAnchor.Price, EndAnchor.Price);
			DateTime minTime = StartAnchor.Time <= EndAnchor.Time ? StartAnchor.Time : EndAnchor.Time;
			DateTime maxTime = StartAnchor.Time <= EndAnchor.Time ? EndAnchor.Time : StartAnchor.Time;

			return MathHelper.DidPredicateCross(values, delegate(ChartAlertValue value)
			{
				bool isInside = value.Value >= minPrice && value.Value <= maxPrice && value.Time >= minTime && value.Time <= maxTime;
				return condition == Condition.CrossInside ? isInside : !isInside;
			});
		}

		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			if (DrawingState == DrawingState.Building)
				return true;
			if (StartAnchor == null || EndAnchor == null)
				return false;

			DateTime minTime = StartAnchor.Time <= EndAnchor.Time ? StartAnchor.Time : EndAnchor.Time;
			DateTime maxTime = StartAnchor.Time <= EndAnchor.Time ? EndAnchor.Time : StartAnchor.Time;
			if (minTime > lastTimeOnChart || maxTime < firstTimeOnChart)
				return false;

			double minPrice = Math.Min(StartAnchor.Price, EndAnchor.Price);
			double maxPrice = Math.Max(StartAnchor.Price, EndAnchor.Price);
			return maxPrice >= chartScale.MinValue && minPrice <= chartScale.MaxValue;
		}

		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			if (DrawingState == DrawingState.Building)
				return Cursors.Pen;
			if (DrawingState == DrawingState.Moving)
				return IsLocked ? Cursors.No : Cursors.SizeAll;
			if (DrawingState == DrawingState.Editing && IsLocked)
				return Cursors.No;

			ResizeMode mode = resizeMode != ResizeMode.None ? resizeMode : GetResizeModeForPoint(point, chartControl, chartScale, DrawingState == DrawingState.Normal);
			switch (mode)
			{
				case ResizeMode.TopLeft:
				case ResizeMode.BottomRight:
					return IsLocked ? Cursors.Arrow : Cursors.SizeNWSE;
				case ResizeMode.TopRight:
				case ResizeMode.BottomLeft:
					return IsLocked ? Cursors.Arrow : Cursors.SizeNESW;
				case ResizeMode.MoveAll:
					return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
				default:
					return null;
			}
		}

		public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			Rect rect = GetAnchorsRect(chartControl, chartScale);
			return new Point[] { rect.TopLeft, rect.TopRight, rect.BottomLeft, rect.BottomRight };
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
					lastBuildEndDataPoint = null;
					break;

				case DrawingState.Normal:
					Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
					editingLeftAnchor = startPoint.X <= endPoint.X ? StartAnchor : EndAnchor;
					editingTopAnchor = startPoint.Y <= endPoint.Y ? StartAnchor : EndAnchor;
					editingBottomAnchor = startPoint.Y <= endPoint.Y ? EndAnchor : StartAnchor;
					editingRightAnchor = startPoint.X <= endPoint.X ? EndAnchor : StartAnchor;

					Cursor clickedCursor = GetCursor(chartControl, chartPanel, chartScale, point);
					if (clickedCursor == Cursors.SizeAll || clickedCursor == Cursors.No)
					{
						DrawingState = DrawingState.Moving;
					}
					else
					{
						resizeMode = GetResizeModeForPoint(point, chartControl, chartScale, true);
						if (resizeMode != ResizeMode.None)
							DrawingState = resizeMode == ResizeMode.MoveAll ? DrawingState.Moving : DrawingState.Editing;
						else if (!GetAnchorsRect(chartControl, chartScale).IntersectsWith(new Rect(point.X, point.Y, 1, 1)))
							IsSelected = false;
					}

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
					if (lastBuildEndDataPoint == null)
						lastBuildEndDataPoint = new ChartAnchor();
					dataPoint.CopyDataValues(lastBuildEndDataPoint);
					MarkProfileDirty();
				}
			}
			else if (DrawingState == DrawingState.Editing)
			{
				if (lastMouseMoveDataPoint == null)
					lastMouseMoveDataPoint = new ChartAnchor();

				switch (resizeMode)
				{
					case ResizeMode.TopLeft:
						editingTopAnchor.Price = lastMouseMoveDataPoint.Price;
						editingLeftAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
						editingLeftAnchor.Time = lastMouseMoveDataPoint.Time;
						dataPoint.CopyDataValues(lastMouseMoveDataPoint);
						break;
					case ResizeMode.BottomRight:
						editingBottomAnchor.Price = lastMouseMoveDataPoint.Price;
						editingRightAnchor.Time = lastMouseMoveDataPoint.Time;
						editingRightAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
						dataPoint.CopyDataValues(lastMouseMoveDataPoint);
						break;
					case ResizeMode.TopRight:
						editingRightAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
						editingRightAnchor.Time = lastMouseMoveDataPoint.Time;
						editingTopAnchor.Price = lastMouseMoveDataPoint.Price;
						dataPoint.CopyDataValues(lastMouseMoveDataPoint);
						break;
					case ResizeMode.BottomLeft:
						editingLeftAnchor.Time = lastMouseMoveDataPoint.Time;
						editingLeftAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
						editingBottomAnchor.Price = lastMouseMoveDataPoint.Price;
						dataPoint.CopyDataValues(lastMouseMoveDataPoint);
						break;
				}
				MarkProfileDirty();
			}
			else if (DrawingState == DrawingState.Moving)
			{
				foreach (ChartAnchor anchor in Anchors)
					anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
				MarkProfileDirty();
			}
		}

		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState == DrawingState.Building)
			{
				if (lastBuildEndDataPoint != null)
					lastBuildEndDataPoint.CopyDataValues(EndAnchor);
				else if (dataPoint != null)
					dataPoint.CopyDataValues(EndAnchor);
				lastBuildEndDataPoint = null;
				EndAnchor.IsEditing = false;
				DrawingState = DrawingState.Normal;
				IsSelected = false;
				MarkProfileDirty();
				return;
			}

			if (DrawingState == DrawingState.Editing || DrawingState == DrawingState.Moving)
			{
				lastMouseMoveDataPoint = null;
				DrawingState = DrawingState.Normal;
				editingLeftAnchor = null;
				editingTopAnchor = null;
				editingRightAnchor = null;
				editingBottomAnchor = null;
				resizeMode = ResizeMode.None;
				MarkProfileDirty();
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
			DxRectangleF boxRect = GetBoxRect(chartControl, chartPanel, chartScale);
			if (boxRect.Width < 1f || boxRect.Height < 1f)
				return;

			RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
			EnsureDxResources();
			DrawSelectionBox(chartControl, boxRect);
			DrawTrendLine(chartControl, chartScale);

			if (IsInHitTest || DrawingState == DrawingState.Building)
				return;

			EnsureProfiles(chartControl, chartScale, boxRect);

			ProfileTrack volumeTrack;
			ProfileTrack deltaTrack;
			ResolveProfileTracks(boxRect, chartPanel, out volumeTrack, out deltaTrack);

			if (ShowVolumeProfile && profileResult != null && profileResult.HasProfile && volumeTrack.IsVisible)
				DrawVolumeRows(chartScale, chartPanel, profileResult, volumeTrack);

			if (ShowDeltaProfile && deltaResult != null && deltaResult.MaxDelta > 0 && deltaTrack.IsVisible)
				DrawDeltaRows(chartScale, chartPanel, deltaResult, deltaTrack);

			ProfileTrack referenceTrack = volumeTrack.IsVisible ? volumeTrack : deltaTrack;
			if (referenceTrack.IsVisible && profileResult != null && profileResult.HasProfile)
				DrawReferenceLines(chartScale, chartPanel, referenceTrack);

			DrawStatisticsBox(chartPanel, boxRect);
			if (!ShowProfileStatistics)
				DrawTotalVolumeLabel(boxRect, volumeTrack, deltaTrack);
			DrawDataSourceLabel(boxRect, volumeTrack, deltaTrack);
			DrawNoDataLabel(boxRect);
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaFixedRangeProfile";
				Description = "Manual time-and-price fixed range volume/delta profile using Orca profile rows and aggregation.";
				DrawingState = DrawingState.Building;

				StartAnchor = new ChartAnchor { DisplayName = "Start", IsEditing = true, DrawingTool = this };
				EndAnchor = new ChartAnchor { DisplayName = "End", IsEditing = true, DrawingTool = this };

				ProfileDataMode = OrcaFixedRangeProfileDataMode.TrueVolumeAtPrice;
				DataSourcePreference = OrcaFixedRangeProfileDataSourcePreference.ChartLocalOnly;
				AllowEstimatedChartFallback = true;
				ShowDataSourceLabel = true;
				RowCount = 100;
				VolumeAggregationMode = OrcaFixedRangeAggregationMode.TicksPerRow;
				TicksPerRow = 1;
				DynamicAggregationMultiplier = 1.0;
				DynamicRowMinPixels = 6;
				DeltaAggregationMode = OrcaFixedRangeAggregationMode.Dynamic;
				DeltaRowCount = 100;
				DeltaTicksPerRow = 4;
				DeltaDynamicAggregationMultiplier = 1.0;
				DeltaDynamicRowMinPixels = 10;
				DeltaDynamicMinCompression = 1;
				DeltaDynamicMaxCompression = 100;
				ValueAreaPercent = 70;

				ShowVolumeProfile = true;
				ShowDeltaProfile = true;
				ProfileSideArrangement = OrcaFixedRangeProfileSideArrangement.ManualSideSettings;
				VolumeSide = OrcaFixedRangeProfileSide.Right;
				DeltaSide = OrcaFixedRangeProfileSide.Left;
				ProfilePlacement = OrcaFixedRangeProfilePlacement.InsideSelectedBox;
				MaxProfileWidthPx = 160;
				VolumeProfileWidthPx = 100;
				DeltaProfileWidthPx = 60;
				VolumeProfileBarSpacingPx = 0;
				DeltaProfileBarSpacingPx = 1;
				ShowVolumeLabels = false;
				ShowDeltaLabels = true;
				ShowPOC = true;
				ShowValueArea = true;
				ShowVAColor = true;
				ShowVALines = true;
				ShowVAH = true;
				ShowVAL = true;
				ShowTotalVolume = true;
				ShowProfileStatistics = true;
				ShowTotalDelta = true;
				ShowFinishDelta = true;
				ShowDeltaPercent = true;
				ShowPointRange = true;
				ShowDuration = true;
				StatisticsPosition = OrcaFixedRangeStatisticsPosition.TopLeft;
				StatisticsFontFamily = "Segoe UI";
				StatisticsFontWeight = OrcaFixedRangeFontWeight.Bold;
				StatisticsFontSize = 11f;
				StatisticsBackgroundOpacity = 70;
				StatisticsCornerRadius = 6f;
				StatisticsTextColor = WpfBrushes.WhiteSmoke;
				StatisticsBackgroundColor = WpfBrushes.Black;
				ShowTrendLine = true;
				TrendLineStyle = OrcaFixedRangeVALineStyle.Solid;
				TrendLineThickness = 1.5f;
				TrendLineOpacity = 80;
				TrendLineColor = WpfBrushes.DodgerBlue;
				DeltaLabelFontSize = 10f;
				VolumeLabelFontSize = 10f;
				VALineThickness = 1.5f;
				VALineStyle = OrcaFixedRangeVALineStyle.Dash;
				ShowBoxBorder = true;
				BoxFillOpacity = 30;
				ProfileOpacity = 180;
				UseGradient = true;
				GradientSteps = 16;
				MinBrightness = 0.2f;

				BoxFillColor = WpfBrushes.SteelBlue;
				BoxBorderStroke = new Stroke(WpfBrushes.SteelBlue, 1.5f);
				POCColor = WpfBrushes.DodgerBlue;
				VAColor = WpfBrushes.CornflowerBlue;
				ProfileUpColor = WpfBrushes.MediumSeaGreen;
				ProfileDownColor = WpfBrushes.Crimson;
				DeltaPositiveColor = WpfBrushes.SteelBlue;
				DeltaNegativeColor = WpfBrushes.IndianRed;
				DeltaNeutralColor = WpfBrushes.Gray;
				UseDeltaIntensityColoring = true;
				DeltaIntensityMinOpacity = 0.35f;
				DeltaPositiveLabelColor = WpfBrushes.LightGreen;
				DeltaNegativeLabelColor = WpfBrushes.LightCoral;
				TextColor = WpfBrushes.WhiteSmoke;
			}
			else if (State == State.DataLoaded)
			{
				profileResult = new OrcaVolumeProfileResult();
				deltaResult = new OrcaVolumeProfileResult();
				MarkProfileDirty();
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

		private Rect GetAnchorsRect(ChartControl chartControl, ChartScale chartScale)
		{
			if (StartAnchor == null || EndAnchor == null)
				return new Rect();

			ChartPanel chartPanel = chartControl.ChartPanels[chartScale.PanelIndex];
			Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			double left = Math.Min(startPoint.X, endPoint.X);
			double top = Math.Min(startPoint.Y, endPoint.Y);
			double width = Math.Abs(endPoint.X - startPoint.X);
			double height = Math.Abs(endPoint.Y - startPoint.Y);
			return new Rect(left, top, width, height);
		}

		private DxRectangleF GetBoxRect(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
		{
			Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			float left = (float)Math.Min(startPoint.X, endPoint.X);
			float right = (float)Math.Max(startPoint.X, endPoint.X);
			float top = (float)Math.Min(startPoint.Y, endPoint.Y);
			float bottom = (float)Math.Max(startPoint.Y, endPoint.Y);
			return new DxRectangleF(left, top, Math.Max(1f, right - left), Math.Max(1f, bottom - top));
		}

		private ResizeMode GetResizeModeForPoint(Point point, ChartControl chartControl, ChartScale chartScale, bool useCursorSensitivity)
		{
			Rect rect = GetAnchorsRect(chartControl, chartScale);
			Point[] points = new Point[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft };
			Point? closest = GetClosestPoint(points, point, useCursorSensitivity);
			if (closest != null)
			{
				if (closest.Value == rect.TopLeft)
					return ResizeMode.TopLeft;
				if (closest.Value == rect.TopRight)
					return ResizeMode.TopRight;
				if (closest.Value == rect.BottomRight)
					return ResizeMode.BottomRight;
				if (closest.Value == rect.BottomLeft)
					return ResizeMode.BottomLeft;
			}

			for (int index = 0; index < 4; index++)
			{
				Point nextPoint = points[index == 3 ? 0 : index + 1];
				Vector vector = points[index] - nextPoint;
				if (MathHelper.IsPointAlongVector(point, nextPoint, vector, CursorSensitivity))
					return ResizeMode.MoveAll;
			}

			if (rect.Contains(point))
				return ResizeMode.MoveAll;

			return ResizeMode.None;
		}

		private static Point? GetClosestPoint(Point[] points, Point desired, bool useSensitivity)
		{
			if (points == null || points.Length == 0)
				return null;

			Point closest = points[0];
			double closestDistance = (closest - desired).Length;
			for (int index = 1; index < points.Length; index++)
			{
				double distance = (points[index] - desired).Length;
				if (distance < closestDistance)
				{
					closest = points[index];
					closestDistance = distance;
				}
			}

			if (useSensitivity && closestDistance > CursorSensitivity)
				return null;

			return closest;
		}

		private void EnsureProfiles(ChartControl chartControl, ChartScale chartScale, DxRectangleF boxRect)
		{
			if (profileResult == null)
				profileResult = new OrcaVolumeProfileResult();
			if (deltaResult == null)
				deltaResult = new OrcaVolumeProfileResult();

			noDataLabel = string.Empty;
			dataSourceLabel = string.Empty;
			ChartBars chartBars = GetAttachedToChartBars();
			if (chartBars == null || chartBars.Bars == null || chartBars.Bars.Count <= 0)
			{
				profileResult.Clear();
				deltaResult.Clear();
				ClearStatistics();
				noDataLabel = "No chart bars";
				return;
			}

			Bars bars = chartBars.Bars;
			DateTime startTime = StartAnchor.Time <= EndAnchor.Time ? StartAnchor.Time : EndAnchor.Time;
			DateTime endTime = StartAnchor.Time <= EndAnchor.Time ? EndAnchor.Time : StartAnchor.Time;
			double lowPrice = Math.Min(StartAnchor.Price, EndAnchor.Price);
			double highPrice = Math.Max(StartAnchor.Price, EndAnchor.Price);
			if (endTime < startTime || highPrice <= lowPrice + PriceEpsilon)
			{
				profileResult.Clear();
				deltaResult.Clear();
				ClearStatistics();
				noDataLabel = "Range too small";
				return;
			}

			int firstBar = FindFirstBarIndexAtOrAfter(bars, startTime);
			int lastBar = FindLastBarIndexAtOrBefore(bars, endTime);
			if (firstBar < 0 || lastBar < 0 || firstBar > lastBar)
			{
				profileResult.Clear();
				deltaResult.Clear();
				ClearStatistics();
				noDataLabel = "No loaded bars in range";
				return;
			}

			double tickSize = GetTickSize(bars);
			int resolvedTicksPerRow = ResolveTicksPerRow(VolumeAggregationMode, TicksPerRow, DynamicRowMinPixels, DynamicAggregationMultiplier, 1, 1000, lowPrice, highPrice, tickSize, boxRect.Height);
			int resolvedDeltaTicksPerRow = ResolveTicksPerRow(DeltaAggregationMode, DeltaTicksPerRow, DeltaDynamicRowMinPixels, DeltaDynamicAggregationMultiplier, DeltaDynamicMinCompression, DeltaDynamicMaxCompression, lowPrice, highPrice, tickSize, boxRect.Height);
			bool useVolumeTicksPerRow = VolumeAggregationMode != OrcaFixedRangeAggregationMode.RowCount;
			bool useDeltaTicksPerRow = DeltaAggregationMode != OrcaFixedRangeAggregationMode.RowCount;
			DateTime lastRangeBarTime = bars.GetTime(lastBar);
			double lastRangeBarVolume = bars.GetVolume(lastBar);
			string dataKey = OrcaProfileDataCache.BuildKey(bars);
			string chartDataKey = OrcaProfileDataCache.BuildKey(bars, chartControl);
			string instrumentKey = OrcaProfileDataCache.BuildInstrumentKey(bars);
			string effectiveDataKey = dataKey;
			int trueDataRevision = -1;
			OrcaProfileDataSnapshot trueDataSnapshot = null;
			bool useTrueProfileData = false;

			if (ProfileDataMode == OrcaFixedRangeProfileDataMode.TrueVolumeAtPrice)
			{
				int sharedBucketSeconds = -1;
				string sharedSourceName = string.Empty;
				bool tryChartFirst = DataSourcePreference != OrcaFixedRangeProfileDataSourcePreference.MasterThenChartLocal;
				bool tryMaster = DataSourcePreference != OrcaFixedRangeProfileDataSourcePreference.ChartLocalOnly;

				if (tryChartFirst)
				{
					string matchedDataKey;
					if (TrySnapshotChartProfile(chartDataKey, dataKey, firstBar, lastBar, out trueDataSnapshot, out matchedDataKey))
					{
						effectiveDataKey = matchedDataKey + "|chart|" + (trueDataSnapshot != null && trueDataSnapshot.SourceName != null ? trueDataSnapshot.SourceName : string.Empty);
						dataSourceLabel = BuildChartTrueDataLabel(trueDataSnapshot);
						useTrueProfileData = true;
					}
				}

				if (!useTrueProfileData && tryMaster)
				{
					bool gotSharedSnapshot = OrcaProfileDataCache.TrySnapshotOrderFlowPriceMaps(instrumentKey, startTime, endTime, out trueDataSnapshot, out sharedBucketSeconds, out sharedSourceName);
					if (gotSharedSnapshot)
					{
						effectiveDataKey = instrumentKey + "|orderflow|" + (sharedSourceName ?? string.Empty) + "|bucket0";
						dataSourceLabel = "Source: master tick";
						useTrueProfileData = true;
					}
				}

				if (!useTrueProfileData && !tryChartFirst)
				{
					string matchedDataKey;
					if (TrySnapshotChartProfile(chartDataKey, dataKey, firstBar, lastBar, out trueDataSnapshot, out matchedDataKey))
					{
						effectiveDataKey = matchedDataKey + "|chart|" + (trueDataSnapshot != null && trueDataSnapshot.SourceName != null ? trueDataSnapshot.SourceName : string.Empty);
						dataSourceLabel = BuildChartTrueDataLabel(trueDataSnapshot);
						useTrueProfileData = true;
					}
				}

				if (!useTrueProfileData)
				{
					if (EnsureVolumetricAccess(bars))
					{
						effectiveDataKey = dataKey + "|volumetric-bars";
						trueDataRevision = -1;
						dataSourceLabel = "Source: volumetric bars";
					}
					else if (AllowEstimatedChartFallback)
					{
						effectiveDataKey = dataKey + "|estimated-bars";
						trueDataRevision = -1;
						dataSourceLabel = BuildEstimatedFallbackLabel(sharedBucketSeconds, instrumentKey, chartDataKey, dataKey);
					}
					else
					{
						profileResult.Clear();
						deltaResult.Clear();
						ClearStatistics();
						dataSourceLabel = string.Empty;
						if (DataSourcePreference == OrcaFixedRangeProfileDataSourcePreference.ChartLocalOnly)
							noDataLabel = "No local Tick Replay cache";
						else if (sharedBucketSeconds > 0)
							noDataLabel = "Set master provider bucket to 0";
						else if (OrcaProfileDataCache.HasOrderFlowSource(instrumentKey))
							noDataLabel = "Waiting for master provider data";
						else
							noDataLabel = HasChartProfileSource(chartDataKey, dataKey) ? "Waiting for true profile data" : BuildMissingProviderLabel(instrumentKey);
						profileDirty = true;
						return;
					}
				}

				if (useTrueProfileData && trueDataSnapshot != null)
					trueDataRevision = trueDataSnapshot.Revision;
			}
			else
			{
				dataSourceLabel = "Source: chart estimate";
			}

			if (!NeedsProfileRebuild(startTime, endTime, lowPrice, highPrice, firstBar, lastBar, bars.Count, lastRangeBarTime, lastRangeBarVolume, effectiveDataKey, trueDataRevision, tickSize, resolvedTicksPerRow, resolvedDeltaTicksPerRow))
				return;

			bool volumeOk = false;
			bool deltaOk = false;
			bool deltaFromVolumeSnapshot = false;
			OrcaProfileDataSnapshot deltaSnapshot = null;
			OrcaProfileDataSnapshot volumetricSnapshot = null;
			if (useTrueProfileData && trueDataSnapshot != null)
			{
				volumeOk = OrcaVolumeProfileCore.BuildFixedRangeFromPriceMaps(trueDataSnapshot.VolumeByBar, trueDataSnapshot.UpVolumeByBar, trueDataSnapshot.DownVolumeByBar, 0, trueDataSnapshot.ToIndex, lowPrice, highPrice, RowCount, resolvedTicksPerRow, useVolumeTicksPerRow, ValueAreaPercent, tickSize, profileResult);
				deltaSnapshot = FilterToBidAskSnapshot(trueDataSnapshot, lowPrice, highPrice);
				deltaFromVolumeSnapshot = deltaSnapshot != null;
			}

			// The volume source can be a chart-local map with no ask/bid. Delta may live on another same-chart publisher. A bar with no bid/ask is omitted; it does not clear the bars that have it.
			if (deltaSnapshot == null)
				deltaSnapshot = TryResolveChartDeltaSnapshot(chartDataKey, dataKey, firstBar, lastBar, lowPrice, highPrice);

			if (deltaSnapshot == null)
			{
				volumetricSnapshot = trueDataSnapshot != null && trueDataSnapshot.SourceName == "VolumetricBars"
					? trueDataSnapshot
					: TryCreateVolumetricSnapshot(bars, firstBar, lastBar, lowPrice, highPrice, tickSize);
				if (volumetricSnapshot != null)
				{
					deltaSnapshot = FilterToBidAskSnapshot(volumetricSnapshot, lowPrice, highPrice);
					if (!volumeOk && volumetricSnapshot.HasAnyVolume)
					{
						volumeOk = OrcaVolumeProfileCore.BuildFixedRangeFromPriceMaps(volumetricSnapshot.VolumeByBar, volumetricSnapshot.UpVolumeByBar, volumetricSnapshot.DownVolumeByBar, 0, volumetricSnapshot.ToIndex, lowPrice, highPrice, RowCount, resolvedTicksPerRow, useVolumeTicksPerRow, ValueAreaPercent, tickSize, profileResult);
						if (volumeOk && (IsEstimatedSourceLabel(dataSourceLabel) || string.IsNullOrEmpty(dataSourceLabel)))
							dataSourceLabel = deltaSnapshot != null ? "Source: volumetric bid/ask" : "Source: volumetric bars";
					}
				}
			}

			if (deltaSnapshot == null)
				deltaSnapshot = TryBuildOrderFlowDeltaSnapshot(instrumentKey, startTime, endTime, lowPrice, highPrice);

			if (deltaSnapshot != null)
			{
				deltaOk = OrcaVolumeProfileCore.BuildFixedRangeFromPriceMaps(deltaSnapshot.VolumeByBar, deltaSnapshot.UpVolumeByBar, deltaSnapshot.DownVolumeByBar, 0, deltaSnapshot.ToIndex, lowPrice, highPrice, DeltaRowCount, resolvedDeltaTicksPerRow, useDeltaTicksPerRow, ValueAreaPercent, tickSize, deltaResult);
				if (deltaOk && (!deltaFromVolumeSnapshot || dataSourceLabel == "Source: volumetric bars" || IsEstimatedSourceLabel(dataSourceLabel) || string.IsNullOrEmpty(dataSourceLabel)))
					dataSourceLabel = DescribeDeltaSource(deltaSnapshot);
			}

			bool allowGeometricVolume = ProfileDataMode == OrcaFixedRangeProfileDataMode.EstimatedFromBars || AllowEstimatedChartFallback;
			if (!volumeOk && allowGeometricVolume)
			{
				if (dataSourceLabel == "Source: volumetric bars" || dataSourceLabel == "Source: volumetric bid/ask")
					dataSourceLabel = "Source: chart estimate";
				volumeOk = OrcaVolumeProfileCore.BuildFixedRangeFromBars(bars, firstBar, lastBar, lowPrice, highPrice, RowCount, resolvedTicksPerRow, useVolumeTicksPerRow, ValueAreaPercent, tickSize, profileResult);
				// BuildFixedRangeFromBars stores the whole bar on the up or down side. That is not bid/ask delta.
				if (volumeOk)
					ClearDirectionalVolume(profileResult);
			}

			if (!deltaOk)
				deltaResult.Clear();

			bool hasRealDelta = deltaOk && deltaSnapshot != null;
			totalVolumeLabel = volumeOk ? "Vol " + FormatVolume(profileResult.TotalVolume) : string.Empty;
			UpdateStatistics(startTime, endTime, lowPrice, highPrice, volumeOk, hasRealDelta, deltaSnapshot, volumetricSnapshot, instrumentKey);
			if (!volumeOk && !deltaOk)
				noDataLabel = "No volume in range";

			cachedStartTime = startTime;
			cachedEndTime = endTime;
			cachedLowPrice = lowPrice;
			cachedHighPrice = highPrice;
			cachedFirstBar = firstBar;
			cachedLastBar = lastBar;
			cachedBarsCount = bars.Count;
			cachedLastRangeBarTime = lastRangeBarTime;
			cachedLastRangeBarVolume = lastRangeBarVolume;
			cachedDataKey = effectiveDataKey;
			cachedTrueDataRevision = trueDataRevision;
			cachedTickSize = tickSize;
			cachedRowCount = RowCount;
			cachedTicksPerRow = TicksPerRow;
			cachedResolvedTicksPerRow = resolvedTicksPerRow;
			cachedDynamicRowMinPixels = DynamicRowMinPixels;
			cachedDeltaRowCount = DeltaRowCount;
			cachedDeltaTicksPerRow = DeltaTicksPerRow;
			cachedResolvedDeltaTicksPerRow = resolvedDeltaTicksPerRow;
			cachedDeltaDynamicRowMinPixels = DeltaDynamicRowMinPixels;
			cachedDeltaDynamicMinCompression = DeltaDynamicMinCompression;
			cachedDeltaDynamicMaxCompression = DeltaDynamicMaxCompression;
			cachedValueAreaPercent = ValueAreaPercent;
			cachedDynamicAggregationMultiplier = DynamicAggregationMultiplier;
			cachedDeltaDynamicAggregationMultiplier = DeltaDynamicAggregationMultiplier;
			cachedAggregationMode = VolumeAggregationMode;
			cachedDeltaAggregationMode = DeltaAggregationMode;
			cachedProfileDataMode = ProfileDataMode;
			cachedDataSourcePreference = DataSourcePreference;
			cachedAllowEstimatedChartFallback = AllowEstimatedChartFallback;
			profileDirty = false;
		}

		private bool TrySnapshotChartProfile(string chartDataKey, string dataKey, int firstBar, int lastBar, out OrcaProfileDataSnapshot snapshot, out string matchedDataKey)
		{
			snapshot = null;
			matchedDataKey = string.Empty;

			if (!string.IsNullOrEmpty(chartDataKey) && OrcaProfileDataCache.TrySnapshot(chartDataKey, firstBar, lastBar, out snapshot))
			{
				matchedDataKey = chartDataKey;
				return true;
			}

			if (!string.IsNullOrEmpty(dataKey) && dataKey != chartDataKey && OrcaProfileDataCache.TrySnapshot(dataKey, firstBar, lastBar, out snapshot))
			{
				matchedDataKey = dataKey;
				return true;
			}

			return false;
		}

		private bool HasChartProfileSource(string chartDataKey, string dataKey)
		{
			if (!string.IsNullOrEmpty(chartDataKey) && OrcaProfileDataCache.HasSource(chartDataKey))
				return true;
			return !string.IsNullOrEmpty(dataKey) && dataKey != chartDataKey && OrcaProfileDataCache.HasSource(dataKey);
		}

		private string BuildEstimatedFallbackLabel(int sharedBucketSeconds, string instrumentKey, string chartDataKey, string dataKey)
		{
			if (ProfileDataMode == OrcaFixedRangeProfileDataMode.EstimatedFromBars)
				return "Source: chart estimate";
			if (DataSourcePreference == OrcaFixedRangeProfileDataSourcePreference.ChartLocalOnly)
				return HasChartProfileSource(chartDataKey, dataKey)
					? "Source: chart estimate (waiting local cache)"
					: "Source: chart estimate (no local cache)";

			if (sharedBucketSeconds > 0)
				return "Source: chart estimate (master bucket != 0)";

			if (OrcaProfileDataCache.HasOrderFlowSource(instrumentKey))
				return "Source: chart estimate (waiting master)";

			if (HasChartProfileSource(chartDataKey, dataKey))
				return "Source: chart estimate (waiting chart cache)";

			return "Source: chart estimate (no master)";
		}

		private string BuildMissingProviderLabel(string instrumentKey)
		{
			string key = string.IsNullOrEmpty(instrumentKey) ? "instrument" : instrumentKey;
			string sources = OrcaProfileDataCache.DescribeOrderFlowSources();
			if (string.IsNullOrEmpty(sources) || sources == "none")
				return "No master provider for " + key + " (sources=none)";

			if (sources.Length > 96)
				sources = sources.Substring(0, 96) + "...";
			return "No master provider for " + key + " (" + sources + ")";
		}

		private string BuildChartTrueDataLabel(OrcaProfileDataSnapshot snapshot)
		{
			if (snapshot == null || string.IsNullOrEmpty(snapshot.SourceName))
				return "Source: local chart VAP";

			if (snapshot.SourceName.IndexOf("FixedRangeProfileDataCache", StringComparison.OrdinalIgnoreCase) >= 0)
				return snapshot.SourceName.IndexOf("SecondaryTick", StringComparison.OrdinalIgnoreCase) >= 0
					? "Source: local secondary tick cache"
					: "Source: local Tick Replay cache";
			if (snapshot.SourceName.IndexOf("OrcaPrints", StringComparison.OrdinalIgnoreCase) >= 0)
				return "Source: chart live prints";
			if (snapshot.SourceName.IndexOf("Candle", StringComparison.OrdinalIgnoreCase) >= 0)
				return "Source: local candle VAP";

			return "Source: local chart VAP";
		}

		private static void ClearDirectionalVolume(OrcaVolumeProfileResult result)
		{
			if (result == null || result.Rows == null)
				return;

			int limit = Math.Min(result.RowCount, result.Rows.Length);
			for (int index = 0; index < limit; index++)
			{
				result.Rows[index].UpVolume = 0;
				result.Rows[index].DownVolume = 0;
			}
		}

		private static bool IsEstimatedSourceLabel(string label)
		{
			return !string.IsNullOrEmpty(label) && label.StartsWith("Source: chart estimate", StringComparison.Ordinal);
		}

		// Ask is the up map and bid is the down map. Bars with neither are left empty so they cannot clear bars that have a split.
		// Row volume is ask+bid, so an unclassified remainder is not assigned to a side.
		private static OrcaProfileDataSnapshot FilterToBidAskSnapshot(OrcaProfileDataSnapshot source, double lowPrice, double highPrice)
		{
			if (source == null)
				return null;

			int count = source.UpVolumeByBar != null ? source.UpVolumeByBar.Count : 0;
			if (source.DownVolumeByBar != null && source.DownVolumeByBar.Count > count)
				count = source.DownVolumeByBar.Count;
			if (count <= 0)
				return null;

			List<Dictionary<double, long>> volume = new List<Dictionary<double, long>>(count);
			List<Dictionary<double, long>> up = new List<Dictionary<double, long>>(count);
			List<Dictionary<double, long>> down = new List<Dictionary<double, long>>(count);
			bool any = false;
			for (int index = 0; index < count; index++)
			{
				Dictionary<double, long> ask = CopyPositiveInRange(GetMap(source.UpVolumeByBar, index), lowPrice, highPrice);
				Dictionary<double, long> bid = CopyPositiveInRange(GetMap(source.DownVolumeByBar, index), lowPrice, highPrice);
				if (ask == null && bid == null)
				{
					volume.Add(null);
					up.Add(null);
					down.Add(null);
					continue;
				}

				Dictionary<double, long> classified = new Dictionary<double, long>();
				AddIntoMap(classified, ask);
				AddIntoMap(classified, bid);
				volume.Add(classified);
				up.Add(ask);
				down.Add(bid);
				any = true;
			}

			if (!any)
				return null;

			return new OrcaProfileDataSnapshot
			{
				FromIndex = 0,
				ToIndex = count - 1,
				Revision = source.Revision,
				SourceName = source.SourceName,
				VolumeByBar = volume,
				UpVolumeByBar = up,
				DownVolumeByBar = down,
				HasAnyVolume = true
			};
		}

		private static Dictionary<double, long> CopyPositiveInRange(Dictionary<double, long> map, double lowPrice, double highPrice)
		{
			if (map == null || map.Count == 0)
				return null;

			Dictionary<double, long> copy = null;
			foreach (KeyValuePair<double, long> kvp in map)
			{
				if (kvp.Value <= 0 || double.IsNaN(kvp.Key) || double.IsInfinity(kvp.Key))
					continue;
				if (kvp.Key < lowPrice - PriceEpsilon || kvp.Key > highPrice + PriceEpsilon)
					continue;
				if (copy == null)
					copy = new Dictionary<double, long>();
				copy[kvp.Key] = kvp.Value;
			}

			return copy;
		}

		private static void AddIntoMap(Dictionary<double, long> target, Dictionary<double, long> source)
		{
			if (target == null || source == null)
				return;

			foreach (KeyValuePair<double, long> kvp in source)
			{
				long existing;
				if (target.TryGetValue(kvp.Key, out existing))
					target[kvp.Key] = existing + kvp.Value;
				else
					target[kvp.Key] = kvp.Value;
			}
		}

		private OrcaProfileDataSnapshot TryResolveChartDeltaSnapshot(string chartDataKey, string dataKey, int firstBar, int lastBar, double lowPrice, double highPrice)
		{
			OrcaProfileDataSnapshot filtered = TryPreferChartDelta(chartDataKey, firstBar, lastBar, lowPrice, highPrice);
			if (filtered != null)
				return filtered;
			if (string.IsNullOrEmpty(dataKey) || dataKey == chartDataKey)
				return null;
			return TryPreferChartDelta(dataKey, firstBar, lastBar, lowPrice, highPrice);
		}

		private static OrcaProfileDataSnapshot TryPreferChartDelta(string key, int firstBar, int lastBar, double lowPrice, double highPrice)
		{
			OrcaProfileDataSnapshot snapshot;
			if (string.IsNullOrEmpty(key) || !OrcaProfileDataCache.TrySnapshotPreferDelta(key, firstBar, lastBar, out snapshot))
				return null;
			return FilterToBidAskSnapshot(snapshot, lowPrice, highPrice);
		}

		private static OrcaProfileDataSnapshot TryBuildOrderFlowDeltaSnapshot(string instrumentKey, DateTime startTime, DateTime endTime, double lowPrice, double highPrice)
		{
			if (string.IsNullOrEmpty(instrumentKey))
				return null;

			OrcaProfileDataSnapshot snapshot;
			int bucketSeconds;
			string sourceName;
			if (!OrcaProfileDataCache.TrySnapshotOrderFlowPriceMaps(instrumentKey, startTime, endTime, out snapshot, out bucketSeconds, out sourceName) || bucketSeconds != 0)
				return null;
			if (snapshot != null && string.IsNullOrEmpty(snapshot.SourceName))
				snapshot.SourceName = sourceName;
			return FilterToBidAskSnapshot(snapshot, lowPrice, highPrice);
		}

		private string DescribeDeltaSource(OrcaProfileDataSnapshot snapshot)
		{
			if (snapshot == null)
				return "Source: bid/ask";
			if (string.Equals(snapshot.SourceName, "VolumetricBars", StringComparison.Ordinal))
				return "Source: volumetric bid/ask";
			if (!string.IsNullOrEmpty(snapshot.SourceName)
				&& (snapshot.SourceName.IndexOf("ProfileDataProvider", StringComparison.OrdinalIgnoreCase) >= 0
					|| snapshot.SourceName.IndexOf("OrderFlow", StringComparison.OrdinalIgnoreCase) >= 0))
				return "Source: master tick";
			return BuildChartTrueDataLabel(snapshot);
		}

		private OrcaProfileDataSnapshot TryCreateVolumetricSnapshot(Bars bars, int firstBar, int lastBar, double lowPrice, double highPrice, double tickSize)
		{
			if (bars == null || firstBar < 0 || lastBar < firstBar || lastBar >= bars.Count)
				return null;
			if (tickSize <= 0 || double.IsNaN(tickSize) || double.IsInfinity(tickSize))
				return null;
			if (!EnsureVolumetricAccess(bars))
				return null;

			try
			{
				int count = lastBar - firstBar + 1;
				OrcaProfileDataSnapshot snapshot = new OrcaProfileDataSnapshot
				{
					FromIndex = 0,
					ToIndex = count - 1,
					Revision = 0,
					SourceName = "VolumetricBars",
					VolumeByBar = new List<Dictionary<double, long>>(count),
					UpVolumeByBar = new List<Dictionary<double, long>>(count),
					DownVolumeByBar = new List<Dictionary<double, long>>(count)
				};

				for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
				{
					Dictionary<double, long> volumeMap = new Dictionary<double, long>();
					Dictionary<double, long> upMap = new Dictionary<double, long>();
					Dictionary<double, long> downMap = new Dictionary<double, long>();
					object volumetricBar = GetVolumetricBar(bars, barIndex);
					if (volumetricBar != null)
					{
						double high = bars.GetHigh(barIndex);
						double low = bars.GetLow(barIndex);
						if (high < low)
						{
							double tmp = high;
							high = low;
							low = tmp;
						}

						if (!double.IsNaN(high) && !double.IsNaN(low) && high >= lowPrice - PriceEpsilon && low <= highPrice + PriceEpsilon)
							AppendVolumetricBar(volumetricBar, low, high, lowPrice, highPrice, tickSize, volumeMap, upMap, downMap);
					}

					if (volumeMap.Count > 0)
						snapshot.HasAnyVolume = true;
					snapshot.VolumeByBar.Add(volumeMap);
					snapshot.UpVolumeByBar.Add(upMap);
					snapshot.DownVolumeByBar.Add(downMap);
				}

				return snapshot.HasAnyVolume ? snapshot : null;
			}
			catch
			{
				return null;
			}
		}

		private void AppendVolumetricBar(object volumetricBar, double barLow, double barHigh, double rangeLow, double rangeHigh, double tickSize, Dictionary<double, long> volumeMap, Dictionary<double, long> upMap, Dictionary<double, long> downMap)
		{
			long scannedVolume = 0;
			if (volumetricAskMethod != null && volumetricBidMethod != null)
			{
				long firstTick = (long)Math.Round(barLow / tickSize);
				long lastTick = (long)Math.Round(barHigh / tickSize);
				if (lastTick < firstTick)
				{
					long tmp = firstTick;
					firstTick = lastTick;
					lastTick = tmp;
				}

				long rangeLowTick = (long)Math.Ceiling((rangeLow / tickSize) - 1E-8);
				long rangeHighTick = (long)Math.Floor((rangeHigh / tickSize) + 1E-8);
				int guard = 0;
				for (long tick = firstTick; tick <= lastTick && guard < 20000; tick++, guard++)
				{
					double price = tick * tickSize;
					long ask = InvokeVolumetricVolume(volumetricAskMethod, volumetricBar, price);
					long bid = InvokeVolumetricVolume(volumetricBidMethod, volumetricBar, price);
					long total = volumetricTotalAtPriceMethod != null ? InvokeVolumetricVolume(volumetricTotalAtPriceMethod, volumetricBar, price) : ask + bid;
					if (total <= 0)
						total = ask + bid;
					if (total <= 0 && ask <= 0 && bid <= 0)
						continue;

					scannedVolume += total > 0 ? total : ask + bid;
					if (tick < rangeLowTick || tick > rangeHighTick)
						continue;

					if (total > 0)
						AddVolumetricMap(volumeMap, price, total);
					if (ask > 0)
						AddVolumetricMap(upMap, price, ask);
					if (bid > 0)
						AddVolumetricMap(downMap, price, bid);
				}
			}

			if (scannedVolume > 0 || volumeMap.Count > 0)
				return;

			long buying = ReadVolumetricProperty(volumetricBuyingProperty, volumetricBar);
			long selling = ReadVolumetricProperty(volumetricSellingProperty, volumetricBar);
			long barVolume = ReadVolumetricProperty(volumetricBarVolumeProperty, volumetricBar);
			if (barVolume <= 0)
				barVolume = buying + selling;
			if (barVolume <= 0 && buying <= 0 && selling <= 0)
				return;

			double overlapLow = Math.Max(barLow, rangeLow);
			double overlapHigh = Math.Min(barHigh, rangeHigh);
			double fullRange = Math.Max(tickSize, barHigh - barLow);
			double overlap = Math.Max(0.0, overlapHigh - overlapLow);
			if (overlap <= PriceEpsilon)
				return;

			double fraction = Math.Min(1.0, overlap / fullRange);
			long askInRange = (long)Math.Round(buying * fraction);
			long bidInRange = (long)Math.Round(selling * fraction);
			long totalInRange = (long)Math.Round(barVolume * fraction);
			if (totalInRange < askInRange + bidInRange)
				totalInRange = askInRange + bidInRange;
			if (totalInRange <= 0 && askInRange <= 0 && bidInRange <= 0)
				return;

			DistributeVolumetricRange(volumeMap, upMap, downMap, overlapLow, overlapHigh, tickSize, totalInRange, askInRange, bidInRange);
		}

		private static void DistributeVolumetricRange(Dictionary<double, long> volumeMap, Dictionary<double, long> upMap, Dictionary<double, long> downMap, double overlapLow, double overlapHigh, double tickSize, long total, long ask, long bid)
		{
			long firstTick = (long)Math.Ceiling((overlapLow / tickSize) - 1E-8);
			long lastTick = (long)Math.Floor((overlapHigh / tickSize) + 1E-8);
			if (lastTick < firstTick)
			{
				long mid = (long)Math.Round(((overlapLow + overlapHigh) * 0.5) / tickSize);
				firstTick = mid;
				lastTick = mid;
			}

			long buckets = lastTick - firstTick + 1;
			if (buckets < 1)
				buckets = 1;
			if (buckets > 20000)
			{
				lastTick = firstTick + 19999;
				buckets = 20000;
			}

			bool splitBidAsk = ask > 0 || bid > 0;
			long totalLeft = total;
			long askLeft = ask;
			long bidLeft = bid;
			for (long tick = firstTick; tick <= lastTick; tick++)
			{
				long remainingBuckets = lastTick - tick + 1;
				long askShare = remainingBuckets <= 1 ? askLeft : askLeft / remainingBuckets;
				long bidShare = remainingBuckets <= 1 ? bidLeft : bidLeft / remainingBuckets;
				long totalShare = splitBidAsk
					? askShare + bidShare
					: (remainingBuckets <= 1 ? totalLeft : totalLeft / remainingBuckets);
				totalLeft -= splitBidAsk ? totalShare : totalShare;
				askLeft -= askShare;
				bidLeft -= bidShare;
				double price = tick * tickSize;
				if (totalShare > 0)
					AddVolumetricMap(volumeMap, price, totalShare);
				if (askShare > 0)
					AddVolumetricMap(upMap, price, askShare);
				if (bidShare > 0)
					AddVolumetricMap(downMap, price, bidShare);
			}
		}

		private bool EnsureVolumetricAccess(Bars bars)
		{
			if (bars == null || bars.BarsType == null)
				return false;

			Type barsType = bars.BarsType.GetType();
			if (volumetricAccessResolved && volumetricResolvedType == barsType)
				return volumetricAccessAvailable;

			volumetricAccessResolved = true;
			volumetricResolvedType = barsType;
			volumetricAccessAvailable = false;
			volumetricVolumesProperty = null;
			volumetricVolumesField = null;
			volumetricAskMethod = null;
			volumetricBidMethod = null;
			volumetricTotalAtPriceMethod = null;
			volumetricBuyingProperty = null;
			volumetricSellingProperty = null;
			volumetricBarVolumeProperty = null;
			if (barsType.Name.IndexOf("Volumetric", StringComparison.Ordinal) < 0)
				return false;

			try
			{
				const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
				volumetricVolumesProperty = barsType.GetProperty("Volumes", flags);
				volumetricVolumesField = barsType.GetField("Volumes", flags);
				if (volumetricVolumesProperty == null && volumetricVolumesField == null)
					return false;

				Type volumeCollectionType = volumetricVolumesProperty != null ? volumetricVolumesProperty.PropertyType : volumetricVolumesField.FieldType;
				Type elementType = volumeCollectionType;
				if (elementType == null)
					return false;
				if (elementType.IsArray)
					elementType = elementType.GetElementType();
				else if (elementType.IsGenericType)
				{
					Type[] arguments = elementType.GetGenericArguments();
					if (arguments != null && arguments.Length > 0)
						elementType = arguments[0];
				}

				if (elementType == null)
					return false;

				Type[] priceArgs = new Type[] { typeof(double) };
				volumetricAskMethod = elementType.GetMethod("GetAskVolumeForPrice", flags, null, priceArgs, null);
				volumetricBidMethod = elementType.GetMethod("GetBidVolumeForPrice", flags, null, priceArgs, null);
				volumetricTotalAtPriceMethod = elementType.GetMethod("GetTotalVolumeForPrice", flags, null, priceArgs, null);
				volumetricBuyingProperty = elementType.GetProperty("TotalBuyingVolume", flags);
				volumetricSellingProperty = elementType.GetProperty("TotalSellingVolume", flags);
				volumetricBarVolumeProperty = elementType.GetProperty("TotalVolume", flags);
				volumetricAccessAvailable = volumetricAskMethod != null || volumetricBuyingProperty != null;
				return volumetricAccessAvailable;
			}
			catch
			{
				volumetricAccessAvailable = false;
				return false;
			}
		}

		private object GetVolumetricBar(Bars bars, int barIndex)
		{
			if (!EnsureVolumetricAccess(bars) || bars == null || bars.BarsType == null || barIndex < 0)
				return null;

			object volumes = null;
			try
			{
				if (volumetricVolumesProperty != null)
					volumes = volumetricVolumesProperty.GetValue(bars.BarsType, null);
				else if (volumetricVolumesField != null)
					volumes = volumetricVolumesField.GetValue(bars.BarsType);
			}
			catch
			{
				return null;
			}

			if (volumes == null)
				return null;

			Array array = volumes as Array;
			if (array != null)
				return barIndex < array.Length ? array.GetValue(barIndex) : null;

			IList list = volumes as IList;
			if (list != null)
				return barIndex < list.Count ? list[barIndex] : null;

			return null;
		}

		private long InvokeVolumetricVolume(MethodInfo method, object volumetricBar, double price)
		{
			if (method == null || volumetricBar == null)
				return 0;

			try
			{
				volumetricInvokeArgs[0] = price;
				return CoerceVolume(method.Invoke(volumetricBar, volumetricInvokeArgs));
			}
			catch
			{
				return 0;
			}
		}

		private static long ReadVolumetricProperty(PropertyInfo property, object volumetricBar)
		{
			if (property == null || volumetricBar == null)
				return 0;

			try
			{
				return CoerceVolume(property.GetValue(volumetricBar, null));
			}
			catch
			{
				return 0;
			}
		}

		private static long CoerceVolume(object value)
		{
			if (value == null)
				return 0;
			if (value is long)
				return (long)value < 0 ? 0 : (long)value;
			if (value is int)
				return (int)value < 0 ? 0 : (int)value;
			if (value is double)
			{
				double number = (double)value;
				if (double.IsNaN(number) || double.IsInfinity(number) || number <= 0)
					return 0;
				return (long)Math.Round(number);
			}
			if (value is float)
			{
				float number = (float)value;
				if (float.IsNaN(number) || float.IsInfinity(number) || number <= 0)
					return 0;
				return (long)Math.Round(number);
			}
			if (value is decimal)
			{
				decimal number = (decimal)value;
				return number <= 0 ? 0 : (long)Math.Round(number);
			}

			try
			{
				long number = Convert.ToInt64(value, CultureInfo.InvariantCulture);
				return number < 0 ? 0 : number;
			}
			catch
			{
				return 0;
			}
		}

		private static void AddVolumetricMap(Dictionary<double, long> map, double price, long volume)
		{
			if (map == null || volume <= 0 || double.IsNaN(price) || double.IsInfinity(price))
				return;

			long existing;
			if (map.TryGetValue(price, out existing))
				map[price] = existing + volume;
			else
				map[price] = volume;
		}

		private bool NeedsProfileRebuild(DateTime startTime, DateTime endTime, double lowPrice, double highPrice, int firstBar, int lastBar, int barsCount, DateTime lastRangeBarTime, double lastRangeBarVolume, string dataKey, int trueDataRevision, double tickSize, int resolvedTicksPerRow, int resolvedDeltaTicksPerRow)
		{
			if (profileDirty)
				return true;
			if (cachedStartTime != startTime || cachedEndTime != endTime)
				return true;
			if (Math.Abs(cachedLowPrice - lowPrice) > PriceEpsilon || Math.Abs(cachedHighPrice - highPrice) > PriceEpsilon)
				return true;
			if (cachedFirstBar != firstBar || cachedLastBar != lastBar || cachedBarsCount != barsCount)
				return true;
			if (cachedLastRangeBarTime != lastRangeBarTime || Math.Abs(cachedLastRangeBarVolume - lastRangeBarVolume) > PriceEpsilon)
				return true;
			if (cachedDataKey != dataKey || cachedTrueDataRevision != trueDataRevision)
				return true;
			if (Math.Abs(cachedTickSize - tickSize) > PriceEpsilon)
				return true;
			if (cachedRowCount != RowCount || cachedTicksPerRow != TicksPerRow || cachedResolvedTicksPerRow != resolvedTicksPerRow || cachedDynamicRowMinPixels != DynamicRowMinPixels)
				return true;
			if (cachedDeltaRowCount != DeltaRowCount || cachedDeltaTicksPerRow != DeltaTicksPerRow || cachedResolvedDeltaTicksPerRow != resolvedDeltaTicksPerRow || cachedDeltaDynamicRowMinPixels != DeltaDynamicRowMinPixels)
				return true;
			if (cachedDeltaDynamicMinCompression != DeltaDynamicMinCompression || cachedDeltaDynamicMaxCompression != DeltaDynamicMaxCompression)
				return true;
			if (Math.Abs(cachedValueAreaPercent - ValueAreaPercent) > PriceEpsilon || Math.Abs(cachedDynamicAggregationMultiplier - DynamicAggregationMultiplier) > PriceEpsilon || Math.Abs(cachedDeltaDynamicAggregationMultiplier - DeltaDynamicAggregationMultiplier) > PriceEpsilon)
				return true;
			if (cachedAggregationMode != VolumeAggregationMode || cachedDeltaAggregationMode != DeltaAggregationMode || cachedProfileDataMode != ProfileDataMode)
				return true;
			if (cachedDataSourcePreference != DataSourcePreference)
				return true;
			if (cachedAllowEstimatedChartFallback != AllowEstimatedChartFallback)
				return true;
			return false;
		}

		private int FindFirstBarIndexAtOrAfter(Bars bars, DateTime time)
		{
			int low = 0;
			int high = bars.Count - 1;
			int result = -1;
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
			return result;
		}

		private int FindLastBarIndexAtOrBefore(Bars bars, DateTime time)
		{
			int low = 0;
			int high = bars.Count - 1;
			int result = -1;
			while (low <= high)
			{
				int mid = low + ((high - low) / 2);
				DateTime barTime = bars.GetTime(mid);
				if (barTime <= time)
				{
					result = mid;
					low = mid + 1;
				}
				else
				{
					high = mid - 1;
				}
			}
			return result;
		}

		private double GetTickSize(Bars bars)
		{
			if (AttachedTo != null && AttachedTo.Instrument != null && AttachedTo.Instrument.MasterInstrument != null && AttachedTo.Instrument.MasterInstrument.TickSize > 0)
				return AttachedTo.Instrument.MasterInstrument.TickSize;
			if (bars != null && bars.Instrument != null && bars.Instrument.MasterInstrument != null && bars.Instrument.MasterInstrument.TickSize > 0)
				return bars.Instrument.MasterInstrument.TickSize;
			return 0.01;
		}

		private int ResolveTicksPerRow(OrcaFixedRangeAggregationMode mode, int requestedTicksPerRow, int dynamicMinPixels, double multiplier, int minCompression, int maxCompression, double lowPrice, double highPrice, double tickSize, float boxHeight)
		{
			if (mode == OrcaFixedRangeAggregationMode.TicksPerRow)
				return Math.Max(1, requestedTicksPerRow);
			if (mode == OrcaFixedRangeAggregationMode.RowCount)
				return Math.Max(1, requestedTicksPerRow);

			double safeTickSize = tickSize > 0 ? tickSize : 0.01;
			double priceRange = Math.Max(safeTickSize, highPrice - lowPrice);
			int ticksInRange = Math.Max(1, (int)Math.Ceiling(priceRange / safeTickSize));
			int targetRows = Math.Max(1, (int)Math.Floor(Math.Max(1f, boxHeight) / Math.Max(1, dynamicMinPixels)));
			int resolved = Math.Max(1, (int)Math.Ceiling((ticksInRange / (double)targetRows) * Math.Max(0.1, multiplier)));
			resolved = Math.Max(Math.Max(1, minCompression), resolved);
			resolved = Math.Min(Math.Max(1, maxCompression), resolved);
			return resolved;
		}

		private void ResolveProfileTracks(DxRectangleF boxRect, ChartPanel chartPanel, out ProfileTrack volumeTrack, out ProfileTrack deltaTrack)
		{
			volumeTrack = new ProfileTrack();
			deltaTrack = new ProfileTrack();
			volumeTrack.IsVisible = ShowVolumeProfile;
			deltaTrack.IsVisible = ShowDeltaProfile;
			OrcaFixedRangeProfileSide volumeSide;
			OrcaFixedRangeProfileSide deltaSide;
			bool useArrangement = ResolveArrangementSides(out volumeSide, out deltaSide);

			float panelLeft = chartPanel.X;
			float panelRight = chartPanel.X + chartPanel.W;
			float volumeWidth = ResolveTrackWidth(VolumeProfileWidthPx);
			float deltaWidth = ResolveTrackWidth(DeltaProfileWidthPx);

			if (useArrangement && ProfilePlacement == OrcaFixedRangeProfilePlacement.InsideSelectedBox && ShowVolumeProfile && ShowDeltaProfile)
			{
				AssignInsideEdgeArrangementTracks(boxRect, volumeWidth, deltaWidth, volumeSide, deltaSide, ref volumeTrack, ref deltaTrack);
				return;
			}

			float bandLeft;
			float bandRight;
			float requestedBandWidth;
			if (ShowVolumeProfile && ShowDeltaProfile)
				requestedBandWidth = volumeWidth + TrackGapPx + deltaWidth;
			else if (ShowVolumeProfile)
				requestedBandWidth = volumeWidth;
			else
				requestedBandWidth = deltaWidth;
			requestedBandWidth = Math.Max(10f, requestedBandWidth);

			if (ProfilePlacement == OrcaFixedRangeProfilePlacement.OutsideRightEdge)
			{
				bandLeft = boxRect.Right + OutsideProfileGapPx;
				bandRight = Math.Min(panelRight, bandLeft + requestedBandWidth);
			}
			else if (ProfilePlacement == OrcaFixedRangeProfilePlacement.OutsideLeftEdge)
			{
				bandRight = boxRect.Left - OutsideProfileGapPx;
				bandLeft = Math.Max(panelLeft, bandRight - requestedBandWidth);
			}
			else
			{
				float innerWidth = Math.Max(1f, boxRect.Width - (BoxPaddingPx * 2f));
				float bandWidth = Math.Min(requestedBandWidth, innerWidth);
				bandLeft = boxRect.Left + BoxPaddingPx;
				bandRight = bandLeft + bandWidth;
				if (volumeSide == OrcaFixedRangeProfileSide.Right || deltaSide == OrcaFixedRangeProfileSide.Right)
				{
					bandRight = boxRect.Right - BoxPaddingPx;
					bandLeft = bandRight - bandWidth;
				}
			}

			if (bandRight <= bandLeft + 2f)
			{
				volumeTrack.IsVisible = false;
				deltaTrack.IsVisible = false;
				return;
			}

			if (ShowVolumeProfile && ShowDeltaProfile)
			{
				if (volumeSide != deltaSide)
				{
					float available = Math.Max(1f, bandRight - bandLeft - TrackGapPx);
					float scaledVolume = volumeWidth;
					float scaledDelta = deltaWidth;
					float needed = scaledVolume + scaledDelta;
					if (needed > available && needed > 0.1f)
					{
						float scale = available / needed;
						scaledVolume *= scale;
						scaledDelta *= scale;
					}

					float mid = volumeSide == OrcaFixedRangeProfileSide.Left
						? bandLeft + scaledVolume + (TrackGapPx * 0.5f)
						: bandRight - scaledVolume - (TrackGapPx * 0.5f);
					AssignSideTrack(ref volumeTrack, volumeSide, bandLeft, mid - (TrackGapPx * 0.5f), mid + (TrackGapPx * 0.5f), bandRight, useArrangement);
					AssignSideTrack(ref deltaTrack, deltaSide, bandLeft, mid - (TrackGapPx * 0.5f), mid + (TrackGapPx * 0.5f), bandRight, useArrangement);

					// Clamp each track to its requested width when the band is wider than needed.
					ClampTrackWidth(ref volumeTrack, volumeSide, scaledVolume);
					ClampTrackWidth(ref deltaTrack, deltaSide, scaledDelta);
				}
				else
				{
					float available = Math.Max(1f, bandRight - bandLeft - TrackGapPx);
					float scaledVolume = volumeWidth;
					float scaledDelta = deltaWidth;
					float needed = scaledVolume + scaledDelta;
					if (needed > available && needed > 0.1f)
					{
						float scale = available / needed;
						scaledVolume *= scale;
						scaledDelta *= scale;
					}

					volumeTrack.Left = bandLeft;
					volumeTrack.Right = Math.Max(bandLeft, bandLeft + scaledVolume);
					deltaTrack.Left = Math.Min(bandRight, volumeTrack.Right + TrackGapPx);
					deltaTrack.Right = Math.Min(bandRight, deltaTrack.Left + scaledDelta);
					volumeTrack.DrawFromRight = ShouldDrawFromRight(volumeSide, useArrangement);
					deltaTrack.DrawFromRight = ShouldDrawFromRight(deltaSide, useArrangement);
				}
			}
			else if (ShowVolumeProfile)
			{
				AssignSingleTrack(ref volumeTrack, volumeSide, bandLeft, bandRight, volumeWidth, useArrangement);
			}
			else if (ShowDeltaProfile)
			{
				AssignSingleTrack(ref deltaTrack, deltaSide, bandLeft, bandRight, deltaWidth, useArrangement);
			}

			volumeTrack.IsVisible = volumeTrack.IsVisible && volumeTrack.Right > volumeTrack.Left + 1f;
			deltaTrack.IsVisible = deltaTrack.IsVisible && deltaTrack.Right > deltaTrack.Left + 1f;
		}

		private float ResolveTrackWidth(int requestedWidthPx)
		{
			int capped = Math.Min(Math.Max(10, requestedWidthPx), Math.Max(10, MaxProfileWidthPx));
			return capped;
		}

		private void AssignSingleTrack(ref ProfileTrack track, OrcaFixedRangeProfileSide side, float bandLeft, float bandRight, float requestedWidth, bool pointInward)
		{
			float available = Math.Max(1f, bandRight - bandLeft);
			float width = Math.Min(requestedWidth, available);
			if (side == OrcaFixedRangeProfileSide.Right)
			{
				track.Right = bandRight;
				track.Left = bandRight - width;
			}
			else
			{
				track.Left = bandLeft;
				track.Right = bandLeft + width;
			}
			track.DrawFromRight = ShouldDrawFromRight(side, pointInward);
		}

		private static void ClampTrackWidth(ref ProfileTrack track, OrcaFixedRangeProfileSide side, float width)
		{
			float available = Math.Max(1f, track.Right - track.Left);
			float clamped = Math.Min(Math.Max(1f, width), available);
			if (side == OrcaFixedRangeProfileSide.Right)
				track.Left = track.Right - clamped;
			else
				track.Right = track.Left + clamped;
		}

		private bool ResolveArrangementSides(out OrcaFixedRangeProfileSide volumeSide, out OrcaFixedRangeProfileSide deltaSide)
		{
			volumeSide = VolumeSide;
			deltaSide = DeltaSide;

			if (ProfileSideArrangement == OrcaFixedRangeProfileSideArrangement.VolumeRightDeltaLeft)
			{
				volumeSide = OrcaFixedRangeProfileSide.Right;
				deltaSide = OrcaFixedRangeProfileSide.Left;
				return true;
			}

			if (ProfileSideArrangement == OrcaFixedRangeProfileSideArrangement.VolumeLeftDeltaRight)
			{
				volumeSide = OrcaFixedRangeProfileSide.Left;
				deltaSide = OrcaFixedRangeProfileSide.Right;
				return true;
			}

			return false;
		}

		private void AssignInsideEdgeArrangementTracks(DxRectangleF boxRect, float volumeWidth, float deltaWidth, OrcaFixedRangeProfileSide volumeSide, OrcaFixedRangeProfileSide deltaSide, ref ProfileTrack volumeTrack, ref ProfileTrack deltaTrack)
		{
			float innerLeft = boxRect.Left + BoxPaddingPx;
			float innerRight = boxRect.Right - BoxPaddingPx;
			float innerWidth = Math.Max(1f, innerRight - innerLeft);
			float scaledVolume = Math.Min(Math.Max(4f, volumeWidth), Math.Max(1f, innerWidth - TrackGapPx));
			float scaledDelta = Math.Min(Math.Max(4f, deltaWidth), Math.Max(1f, innerWidth - TrackGapPx));
			if (volumeSide != deltaSide)
			{
				float available = Math.Max(1f, innerWidth - TrackGapPx);
				float needed = scaledVolume + scaledDelta;
				if (needed > available && needed > 0.1f)
				{
					float scale = available / needed;
					scaledVolume *= scale;
					scaledDelta *= scale;
				}
			}

			AssignInsideEdgeTrack(ref volumeTrack, volumeSide, innerLeft, innerRight, scaledVolume);
			AssignInsideEdgeTrack(ref deltaTrack, deltaSide, innerLeft, innerRight, scaledDelta);

			volumeTrack.IsVisible = volumeTrack.IsVisible && volumeTrack.Right > volumeTrack.Left + 1f;
			deltaTrack.IsVisible = deltaTrack.IsVisible && deltaTrack.Right > deltaTrack.Left + 1f;
		}

		private void AssignInsideEdgeTrack(ref ProfileTrack track, OrcaFixedRangeProfileSide side, float innerLeft, float innerRight, float trackWidth)
		{
			if (side == OrcaFixedRangeProfileSide.Left)
			{
				track.Left = innerLeft;
				track.Right = Math.Min(innerRight, innerLeft + trackWidth);
			}
			else
			{
				track.Right = innerRight;
				track.Left = Math.Max(innerLeft, innerRight - trackWidth);
			}

			track.DrawFromRight = ShouldDrawFromRight(side, true);
		}

		private void AssignSideTrack(ref ProfileTrack track, OrcaFixedRangeProfileSide side, float bandLeft, float leftRight, float rightLeft, float bandRight, bool pointInward)
		{
			if (side == OrcaFixedRangeProfileSide.Left)
			{
				track.Left = bandLeft;
				track.Right = leftRight;
			}
			else
			{
				track.Left = rightLeft;
				track.Right = bandRight;
			}

			track.DrawFromRight = ShouldDrawFromRight(side, pointInward);
		}

		private bool ShouldDrawFromRight(OrcaFixedRangeProfileSide side, bool pointInward)
		{
			return pointInward ? side == OrcaFixedRangeProfileSide.Right : side == OrcaFixedRangeProfileSide.Left;
		}

		private void DrawSelectionBox(ChartControl chartControl, DxRectangleF boxRect)
		{
			if (IsInHitTest)
			{
				RenderTarget.FillRectangle(boxRect, chartControl.SelectionBrush);
				return;
			}

			if (boxFillBrushDx != null && BoxFillOpacity > 0)
				RenderTarget.FillRectangle(boxRect, boxFillBrushDx);

			if (ShowBoxBorder && BoxBorderStroke != null)
			{
				BoxBorderStroke.RenderTarget = RenderTarget;
				SharpDX.Direct2D1.Brush borderBrush = IsSelected ? chartControl.SelectionBrush : BoxBorderStroke.BrushDX;
				if (borderBrush != null)
					RenderTarget.DrawRectangle(boxRect, borderBrush, BoxBorderStroke.Width, BoxBorderStroke.StrokeStyle);
			}
		}

		private void DrawTrendLine(ChartControl chartControl, ChartScale chartScale)
		{
			if (!ShowTrendLine || IsInHitTest || chartControl == null || chartScale == null || StartAnchor == null || EndAnchor == null)
				return;
			if (trendLineBrushDx == null || trendLineStrokeDx == null)
				return;
			if (chartControl.ChartPanels == null || PanelIndex < 0 || PanelIndex >= chartControl.ChartPanels.Count)
				return;

			ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
			Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			RenderTarget.DrawLine(
				new DxVector2((float)startPoint.X, (float)startPoint.Y),
				new DxVector2((float)endPoint.X, (float)endPoint.Y),
				trendLineBrushDx,
				Math.Max(0.5f, TrendLineThickness),
				trendLineStrokeDx);
		}

		private void DrawVolumeRows(ChartScale chartScale, ChartPanel chartPanel, OrcaVolumeProfileResult result, ProfileTrack track)
		{
			if (result == null || result.Rows == null || result.MaxVolume <= 0)
				return;

			float width = Math.Max(1f, track.Right - track.Left);
			int rowLimit = Math.Min(result.RowCount, result.Rows.Length);
			for (int rowIndex = 0; rowIndex < rowLimit; rowIndex++)
			{
				OrcaVolumeProfileRow row = result.Rows[rowIndex];
				if (row.Volume <= 0)
					continue;

				float yTop = chartScale.GetYByValue(row.HighPrice);
				float yBottom = chartScale.GetYByValue(row.LowPrice);
				if (yBottom < chartPanel.Y - 2 || yTop > chartPanel.Y + chartPanel.H + 2)
					continue;

				float rowSpacing = Math.Max(0f, VolumeProfileBarSpacingPx);
				float rowHeightPx = Math.Max(1f, Math.Abs(yBottom - yTop) - rowSpacing);
				float drawY = Math.Min(yTop, yBottom) + (rowSpacing / 2f);
				float barWidth = (float)(width * (row.Volume / result.MaxVolume));
				if (barWidth < 0.5f)
					continue;

				SharpDX.Direct2D1.Brush brush = SelectRowBrush(result, rowIndex, row);
				float drawX = track.DrawFromRight ? track.Right - barWidth : track.Left;
				RenderTarget.FillRectangle(new DxRectangleF(drawX, drawY, barWidth, rowHeightPx), brush);
				DrawVolumeLabel(row.Volume, drawX, drawY, barWidth, rowHeightPx, track);
			}
		}

		private void DrawDeltaRows(ChartScale chartScale, ChartPanel chartPanel, OrcaVolumeProfileResult result, ProfileTrack track)
		{
			if (result == null || result.Rows == null || result.MaxDelta <= 0)
				return;

			float width = Math.Max(1f, track.Right - track.Left);
			int rowLimit = Math.Min(result.RowCount, result.Rows.Length);
			for (int rowIndex = 0; rowIndex < rowLimit; rowIndex++)
			{
				OrcaVolumeProfileRow row = result.Rows[rowIndex];
				double delta = row.UpVolume - row.DownVolume;
				if (Math.Abs(delta) <= PriceEpsilon)
					continue;

				float yTop = chartScale.GetYByValue(row.HighPrice);
				float yBottom = chartScale.GetYByValue(row.LowPrice);
				if (yBottom < chartPanel.Y - 2 || yTop > chartPanel.Y + chartPanel.H + 2)
					continue;

				float rowSpacing = Math.Max(0f, DeltaProfileBarSpacingPx);
				float rowHeightPx = Math.Max(1f, Math.Abs(yBottom - yTop) - rowSpacing);
				float drawY = Math.Min(yTop, yBottom) + (rowSpacing / 2f);
				float barWidth = (float)(width * (Math.Abs(delta) / result.MaxDelta));
				if (barWidth < 0.5f)
					continue;

				SharpDX.Direct2D1.SolidColorBrush brush = SelectDeltaBrush(delta, result.MaxDelta);
				if (brush == null)
					brush = deltaNeutralBrushDx;
				float drawX = track.DrawFromRight ? track.Right - barWidth : track.Left;
				RenderTarget.FillRectangle(new DxRectangleF(drawX, drawY, barWidth, rowHeightPx), brush);
				DrawDeltaLabel(delta, drawX, drawY, barWidth, rowHeightPx, track);
			}
		}

		private SharpDX.Direct2D1.Brush SelectRowBrush(OrcaVolumeProfileResult result, int rowIndex, OrcaVolumeProfileRow row)
		{
			if (ShowPOC && rowIndex == result.PocIndex)
				return pocBrushDx;

			bool insideValueArea = ShowValueArea && result.HasValueArea && rowIndex >= result.ValIndex && rowIndex <= result.VahIndex;
			if (insideValueArea && ShowVAColor)
				return SelectGradientBrush(result, row, vaGradientBrushes, vaFillBrushDx);

			if (row.UpVolume <= PriceEpsilon && row.DownVolume <= PriceEpsilon)
				return deltaNeutralBrushDx ?? upBrushDx;

			bool upDominant = row.UpVolume >= row.DownVolume;
			return SelectGradientBrush(result, row, upDominant ? upGradientBrushes : downGradientBrushes, upDominant ? upBrushDx : downBrushDx);
		}

		private SharpDX.Direct2D1.Brush SelectGradientBrush(OrcaVolumeProfileResult result, OrcaVolumeProfileRow row, SharpDX.Direct2D1.SolidColorBrush[] palette, SharpDX.Direct2D1.SolidColorBrush fallback)
		{
			if (!UseGradient || palette == null || palette.Length == 0 || result == null || result.MaxVolume <= 0)
				return fallback;

			int gradientIndex = (int)((row.Volume / result.MaxVolume) * (palette.Length - 1));
			if (gradientIndex < 0) gradientIndex = 0;
			if (gradientIndex >= palette.Length) gradientIndex = palette.Length - 1;
			return palette[gradientIndex];
		}

		private void DrawReferenceLines(ChartScale chartScale, ChartPanel chartPanel, ProfileTrack track)
		{
			if (profileResult == null)
				return;

			if (ShowPOC && profileResult.PocIndex >= 0)
				DrawHorizontalProfileLine(chartScale.GetYByValue(profileResult.PocPrice), chartPanel, track.Left, track.Right, pocBrushDx, 2f, null);

			if (ShowValueArea && ShowVALines && profileResult.HasValueArea)
			{
				if (ShowVAH)
					DrawHorizontalProfileLine(chartScale.GetYByValue(profileResult.VahPrice), chartPanel, track.Left, track.Right, vaLineBrushDx, VALineThickness, vaLineStrokeDx);
				if (ShowVAL)
					DrawHorizontalProfileLine(chartScale.GetYByValue(profileResult.ValPrice), chartPanel, track.Left, track.Right, vaLineBrushDx, VALineThickness, vaLineStrokeDx);
			}
		}

		private void DrawHorizontalProfileLine(float y, ChartPanel chartPanel, float left, float right, SharpDX.Direct2D1.SolidColorBrush brush, float thickness, StrokeStyle strokeStyle)
		{
			if (brush == null || y < chartPanel.Y - 3 || y > chartPanel.Y + chartPanel.H + 3)
				return;

			if (strokeStyle != null)
				RenderTarget.DrawLine(new DxVector2(left, y), new DxVector2(right, y), brush, thickness, strokeStyle);
			else
				RenderTarget.DrawLine(new DxVector2(left, y), new DxVector2(right, y), brush, thickness);
		}

		private void DrawVolumeLabel(double volume, float drawX, float drawY, float barWidth, float rowHeightPx, ProfileTrack track)
		{
			if (!ShowVolumeLabels || volumeLabelTextFormatDx == null || textBrushDx == null)
				return;

			float fontSize = (float)Clamp(VolumeLabelFontSize, 6.0, 30.0);
			if (rowHeightPx < fontSize + 2f)
				return;

			string label = FormatVolume(volume);
			float labelWidth = EstimateTextWidth(label, fontSize);
			if (barWidth < labelWidth + 4f)
				return;

			float textLeft = Math.Max(track.Left, drawX + 1f);
			float textRight = Math.Min(track.Right, drawX + barWidth - 2f);
			if (textRight <= textLeft)
				return;

			RenderTarget.DrawText(label, volumeLabelTextFormatDx, new DxRectangleF(textLeft, drawY, textRight - textLeft, rowHeightPx), textBrushDx);
		}

		private void DrawDeltaLabel(double delta, float drawX, float drawY, float barWidth, float rowHeightPx, ProfileTrack track)
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

			SharpDX.Direct2D1.SolidColorBrush labelBrush = delta >= 0 ? deltaPositiveLabelBrushDx : deltaNegativeLabelBrushDx;
			if (labelBrush == null)
				return;

			// Keep delta labels beside the fixed spine instead of following each bar endpoint.
			float textLeft;
			float textRight;
			if (track.DrawFromRight)
			{
				textRight = track.Right - 1f;
				textLeft = Math.Max(track.Left, textRight - labelWidth - 1f);
			}
			else
			{
				textLeft = track.Left + 1f;
				textRight = Math.Min(track.Right, textLeft + labelWidth + 1f);
			}
			if (textRight <= textLeft)
				return;

			RenderTarget.DrawText(label, deltaLabelTextFormatDx, new DxRectangleF(textLeft, drawY, textRight - textLeft, rowHeightPx), labelBrush);
		}

		private void DrawTotalVolumeLabel(DxRectangleF boxRect, ProfileTrack volumeTrack, ProfileTrack deltaTrack)
		{
			if (!ShowTotalVolume || string.IsNullOrEmpty(totalVolumeLabel) || textBrushDx == null || textFormatDx == null)
				return;

			float left = volumeTrack.IsVisible ? volumeTrack.Left : (deltaTrack.IsVisible ? deltaTrack.Left : boxRect.Left);
			float right = volumeTrack.IsVisible ? volumeTrack.Right : (deltaTrack.IsVisible ? deltaTrack.Right : boxRect.Right);
			float width = Math.Max(60f, right - left);
			RenderTarget.DrawText(totalVolumeLabel, textFormatDx, new DxRectangleF(left, boxRect.Top + 4f, width, 18f), textBrushDx);
		}

		private void DrawStatisticsBox(ChartPanel chartPanel, DxRectangleF boxRect)
		{
			if (!ShowProfileStatistics || statisticsTextFormatDx == null || statisticsTextBrushDx == null)
				return;

			string label = BuildStatisticsLabel();
			if (string.IsNullOrEmpty(label))
				return;

			float fontSize = Math.Max(8f, StatisticsFontSize);
			float paddingX = 8f;
			float paddingY = 5f;
			float maxWidth = Math.Max(80f, boxRect.Width - 8f);
			float textWidth;
			float textHeight;
			using (TextLayout layout = new TextLayout(Core.Globals.DirectWriteFactory, label, statisticsTextFormatDx, maxWidth, 400f))
			{
				textWidth = Math.Max(fontSize, layout.Metrics.Width);
				textHeight = Math.Max(fontSize, layout.Metrics.Height);
			}

			float boxWidth = textWidth + (paddingX * 2f);
			float boxHeight = textHeight + (paddingY * 2f);
			bool placeAbove = StatisticsPosition == OrcaFixedRangeStatisticsPosition.TopLeft
				|| StatisticsPosition == OrcaFixedRangeStatisticsPosition.TopRight;
			bool alignRight = StatisticsPosition == OrcaFixedRangeStatisticsPosition.TopRight
				|| StatisticsPosition == OrcaFixedRangeStatisticsPosition.BottomRight;
			float left = alignRight ? boxRect.Right - boxWidth : boxRect.Left;
			float top = placeAbove
				? boxRect.Top - boxHeight - StatisticsOutsideGapPx
				: boxRect.Bottom + StatisticsOutsideGapPx;
			if (chartPanel != null)
			{
				float panelLeft = chartPanel.X;
				float panelRight = chartPanel.X + chartPanel.W;
				float panelTop = chartPanel.Y;
				float panelBottom = chartPanel.Y + chartPanel.H;
				if (left < panelLeft)
					left = panelLeft;
				if (left + boxWidth > panelRight)
					left = Math.Max(panelLeft, panelRight - boxWidth);

				float clampedTop = top;
				if (clampedTop < panelTop)
					clampedTop = panelTop;
				if (clampedTop + boxHeight > panelBottom)
					clampedTop = panelBottom - boxHeight;
				bool overlapsRange = clampedTop < boxRect.Bottom && (clampedTop + boxHeight) > boxRect.Top;
				if (!overlapsRange)
					top = clampedTop;
			}

			DxRectangleF rect = new DxRectangleF(left, top, boxWidth, boxHeight);
			float radius = Math.Max(0f, StatisticsCornerRadius);
			if (statisticsBackgroundBrushDx != null && StatisticsBackgroundOpacity > 0)
			{
				RoundedRectangle rounded = new RoundedRectangle
				{
					Rect = rect,
					RadiusX = radius,
					RadiusY = radius
				};
				RenderTarget.FillRoundedRectangle(rounded, statisticsBackgroundBrushDx);
			}

			RenderTarget.DrawText(
				label,
				statisticsTextFormatDx,
				new DxRectangleF(left + paddingX, top + paddingY, Math.Max(1f, textWidth + 1f), Math.Max(1f, textHeight + 1f)),
				statisticsTextBrushDx);
		}

		private void ClearStatistics()
		{
			statisticsLabel = string.Empty;
			statisticsTotalVolume = 0;
			statisticsTotalDelta = 0;
			statisticsFinishDelta = 0;
			statisticsHasRealDelta = false;
			statisticsFinishKnown = false;
			statisticsDeltaPercent = 0;
			statisticsPointRange = 0;
			statisticsDuration = TimeSpan.Zero;
			totalVolumeLabel = string.Empty;
		}

		private void UpdateStatistics(DateTime startTime, DateTime endTime, double lowPrice, double highPrice, bool volumeOk, bool hasRealDelta, OrcaProfileDataSnapshot snapshot, OrcaProfileDataSnapshot volumetricSnapshot, string instrumentKey)
		{
			statisticsPointRange = Math.Max(0.0, highPrice - lowPrice);
			statisticsDuration = endTime >= startTime ? endTime - startTime : TimeSpan.Zero;

			statisticsTotalVolume = volumeOk && profileResult != null ? (long)Math.Round(profileResult.TotalVolume) : 0;
			statisticsHasRealDelta = hasRealDelta;
			statisticsTotalDelta = hasRealDelta ? SumProfileDelta(deltaResult) : 0;
			long finishDelta = 0;
			statisticsFinishKnown = hasRealDelta && TryComputeFinishDelta(snapshot, volumetricSnapshot, lowPrice, highPrice, startTime, endTime, instrumentKey, out finishDelta);
			statisticsFinishDelta = statisticsFinishKnown ? finishDelta : 0;
			statisticsDeltaPercent = hasRealDelta && statisticsTotalVolume > 0 ? statisticsTotalDelta / (double)statisticsTotalVolume * 100.0 : 0.0;
			statisticsLabel = BuildStatisticsLabel();
		}

		private static long SumProfileDelta(OrcaVolumeProfileResult result)
		{
			if (result == null || result.Rows == null || result.RowCount <= 0)
				return 0;

			double total = 0;
			int rowLimit = Math.Min(result.RowCount, result.Rows.Length);
			for (int index = 0; index < rowLimit; index++)
				total += result.Rows[index].UpVolume - result.Rows[index].DownVolume;
			return (long)Math.Round(total);
		}

		private bool TryComputeFinishDelta(OrcaProfileDataSnapshot snapshot, OrcaProfileDataSnapshot volumetricSnapshot, double lowPrice, double highPrice, DateTime startTime, DateTime endTime, string instrumentKey, out long finishDelta)
		{
			finishDelta = 0;
			if (TryComputeFinishDeltaFromPriceMaps(snapshot, lowPrice, highPrice, out finishDelta))
				return true;

			if (TryComputeFinishDeltaFromPriceMaps(volumetricSnapshot, lowPrice, highPrice, out finishDelta))
				return true;

			if (TryComputeFinishDeltaFromOrderFlowBuckets(instrumentKey, startTime, endTime, lowPrice, highPrice, out finishDelta))
				return true;

			// Without a chronological bid/ask path, finish delta is unknown. Do not invent it from bar volume.
			finishDelta = 0;
			return false;
		}

		private static bool TryComputeFinishDeltaFromPriceMaps(OrcaProfileDataSnapshot snapshot, double lowPrice, double highPrice, out long finishDelta)
		{
			finishDelta = 0;
			if (snapshot == null || snapshot.UpVolumeByBar == null || snapshot.DownVolumeByBar == null)
				return false;

			// Chart snapshots store maps in list order 0..Count-1 (BuildFixedRange uses the same).
			// Do not use snapshot.FromIndex here — that field retains the original chart bar index.
			int count = Math.Min(snapshot.UpVolumeByBar.Count, snapshot.DownVolumeByBar.Count);
			if (count <= 0)
				return false;

			// A single collapsed map (master order-flow aggregate) has no chronology for finish delta.
			if (count == 1)
				return false;

			long running = 0;
			long maxCumulative = 0;
			long minCumulative = 0;
			bool sawAny = false;
			for (int index = 0; index < count; index++)
			{
				long ask = SumMapInRange(GetMap(snapshot.UpVolumeByBar, index), lowPrice, highPrice);
				long bid = SumMapInRange(GetMap(snapshot.DownVolumeByBar, index), lowPrice, highPrice);
				// A volume-only bar is not a known zero. Skip it so it cannot freeze finish delta at 0.
				if (ask <= 0 && bid <= 0)
					continue;

				long barDelta = ask - bid;

				running += barDelta;
				if (!sawAny)
				{
					maxCumulative = running;
					minCumulative = running;
					sawAny = true;
				}
				else
				{
					if (running > maxCumulative) maxCumulative = running;
					if (running < minCumulative) minCumulative = running;
				}
			}

			if (!sawAny)
				return false;

			finishDelta = CalculateFinishDelta(running, maxCumulative, minCumulative);
			return true;
		}

		private static bool TryComputeFinishDeltaFromOrderFlowBuckets(string instrumentKey, DateTime startTime, DateTime endTime, double lowPrice, double highPrice, out long finishDelta)
		{
			finishDelta = 0;
			if (string.IsNullOrEmpty(instrumentKey))
				return false;

			OrcaOrderFlowDataSnapshot orderFlowSnapshot;
			if (!OrcaProfileDataCache.TrySnapshotOrderFlow(instrumentKey, startTime, endTime, out orderFlowSnapshot)
				|| orderFlowSnapshot == null
				|| orderFlowSnapshot.Buckets == null
				|| orderFlowSnapshot.Buckets.Count == 0)
				return false;

			long running = 0;
			long maxCumulative = 0;
			long minCumulative = 0;
			bool sawAny = false;
			for (int index = 0; index < orderFlowSnapshot.Buckets.Count; index++)
			{
				OrcaOrderFlowBucket bucket = orderFlowSnapshot.Buckets[index];
				if (bucket == null || bucket.Volume <= 0 || double.IsNaN(bucket.Price) || double.IsInfinity(bucket.Price))
					continue;
				if (bucket.Price < lowPrice - PriceEpsilon || bucket.Price > highPrice + PriceEpsilon)
					continue;

				long askVolume = bucket.AskVolume;
				long bidVolume = bucket.BidVolume;
				if (askVolume <= 0 && bidVolume <= 0 && bucket.Delta == 0)
					continue;

				long barDelta;
				if (askVolume > 0 || bidVolume > 0)
					barDelta = askVolume - bidVolume;
				else
					barDelta = bucket.Delta;

				running += barDelta;
				if (!sawAny)
				{
					maxCumulative = running;
					minCumulative = running;
					sawAny = true;
				}
				else
				{
					if (running > maxCumulative) maxCumulative = running;
					if (running < minCumulative) minCumulative = running;
				}
			}

			if (!sawAny)
				return false;

			finishDelta = CalculateFinishDelta(running, maxCumulative, minCumulative);
			return true;
		}

		private static Dictionary<double, long> GetMap(IList<Dictionary<double, long>> maps, int index)
		{
			if (maps == null || index < 0 || index >= maps.Count)
				return null;
			return maps[index];
		}

		private static long SumMapInRange(Dictionary<double, long> map, double lowPrice, double highPrice)
		{
			if (map == null || map.Count == 0)
				return 0;

			long total = 0;
			foreach (KeyValuePair<double, long> kvp in map)
			{
				if (kvp.Value > 0 && kvp.Key >= lowPrice - PriceEpsilon && kvp.Key <= highPrice + PriceEpsilon)
					total += kvp.Value;
			}
			return total;
		}

		private static long CalculateFinishDelta(long currentDelta, long maxCumulativeDelta, long minCumulativeDelta)
		{
			return currentDelta - (currentDelta >= 0 ? maxCumulativeDelta : minCumulativeDelta);
		}

		private string BuildStatisticsLabel()
		{
			string text = string.Empty;
			if (ShowTotalDelta && statisticsHasRealDelta)
				text = AppendStatisticsToken(text, "D " + FormatSignedValue(statisticsTotalDelta));
			if (ShowFinishDelta && statisticsFinishKnown)
				text = AppendStatisticsToken(text, "FD " + FormatSignedValue(statisticsFinishDelta));
			if (ShowDeltaPercent && statisticsHasRealDelta)
				text = AppendStatisticsToken(text, statisticsDeltaPercent.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "%");
			if (ShowTotalVolume)
				text = AppendStatisticsToken(text, "V " + FormatCompactVolume(statisticsTotalVolume));
			if (ShowPointRange)
				text = AppendStatisticsToken(text, "Pts " + statisticsPointRange.ToString("0.##", CultureInfo.InvariantCulture));
			if (ShowDuration)
				text = AppendStatisticsToken(text, FormatDurationHms(statisticsDuration));
			return text;
		}

		private static string AppendStatisticsToken(string text, string token)
		{
			return string.IsNullOrEmpty(text) ? token : text + " | " + token;
		}

		private static string FormatSignedValue(long value)
		{
			return value.ToString("+#;-#;0", CultureInfo.InvariantCulture);
		}

		private static string FormatCompactVolume(long volume)
		{
			long absoluteVolume = volume == long.MinValue ? long.MaxValue : Math.Abs(volume);
			if (absoluteVolume >= 1000000)
				return (volume / 1000000.0).ToString("0.0", CultureInfo.InvariantCulture) + "M";
			if (absoluteVolume >= 1000)
				return (volume / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + "K";
			return volume.ToString(CultureInfo.InvariantCulture);
		}

		private static string FormatDurationHms(TimeSpan duration)
		{
			int totalSeconds = (int)Math.Max(0, Math.Round(Math.Abs(duration.TotalSeconds)));
			int hours = totalSeconds / 3600;
			int minutes = (totalSeconds % 3600) / 60;
			int seconds = totalSeconds % 60;
			if (hours > 0)
				return hours.ToString(CultureInfo.InvariantCulture) + "h " + minutes.ToString(CultureInfo.InvariantCulture) + "m " + seconds.ToString(CultureInfo.InvariantCulture) + "s";
			if (minutes > 0)
				return minutes.ToString(CultureInfo.InvariantCulture) + "m " + seconds.ToString(CultureInfo.InvariantCulture) + "s";
			return seconds.ToString(CultureInfo.InvariantCulture) + "s";
		}

		private void DrawNoDataLabel(DxRectangleF boxRect)
		{
			if (string.IsNullOrEmpty(noDataLabel) || textBrushDx == null || textFormatDx == null)
				return;

			RenderTarget.DrawText(noDataLabel, textFormatDx, new DxRectangleF(boxRect.Left + 6f, boxRect.Top + 6f, Math.Max(60f, boxRect.Width - 12f), 18f), textBrushDx);
		}

		private void DrawDataSourceLabel(DxRectangleF boxRect, ProfileTrack volumeTrack, ProfileTrack deltaTrack)
		{
			if (!ShowDataSourceLabel || string.IsNullOrEmpty(dataSourceLabel) || textBrushDx == null || textFormatDx == null)
				return;

			float left = boxRect.Left + 6f;
			float width = Math.Max(80f, boxRect.Width - 12f);
			float y = Math.Max(boxRect.Top + 4f, boxRect.Top + boxRect.Height - 24f);
			RenderTarget.DrawText(dataSourceLabel, textFormatDx, new DxRectangleF(left, y, width, 18f), textBrushDx);
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null)
				return;

			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxResourceRenderTarget != IntPtr.Zero && dxResourceRenderTarget != currentTarget)
				DisposeDxResources();

			string brushSignature = BuildBrushSignature();
			int steps = Math.Max(2, GradientSteps);
			if (brushSignature != lastBrushSignature
				|| lastBuiltProfileOpacity != ProfileOpacity
				|| lastBuiltBoxFillOpacity != BoxFillOpacity
				|| lastBuiltStatisticsBackgroundOpacity != StatisticsBackgroundOpacity
				|| lastBuiltTrendLineOpacity != TrendLineOpacity)
				DisposeDxResources();

			float alpha = ProfileOpacity / 255f;
			if (pocBrushDx == null) pocBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(POCColor, 1f));
			if (vaFillBrushDx == null) vaFillBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VAColor, alpha));
			if (vaLineBrushDx == null) vaLineBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VAColor, 1f));
			if (upBrushDx == null) upBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(ProfileUpColor, alpha));
			if (downBrushDx == null) downBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(ProfileDownColor, alpha));
			if (deltaPositiveBrushDx == null) deltaPositiveBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaPositiveColor, alpha));
			if (deltaNegativeBrushDx == null) deltaNegativeBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaNegativeColor, alpha));
			if (deltaNeutralBrushDx == null) deltaNeutralBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaNeutralColor, alpha));
			if (deltaPositiveLabelBrushDx == null) deltaPositiveLabelBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaPositiveLabelColor, 1f));
			if (deltaNegativeLabelBrushDx == null) deltaNegativeLabelBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaNegativeLabelColor, 1f));
			if (textBrushDx == null) textBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(TextColor, 1f));
			if (boxFillBrushDx == null) boxFillBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(BoxFillColor, BoxFillOpacity / 100f));
			if (statisticsTextBrushDx == null) statisticsTextBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(StatisticsTextColor, 1f));
			if (statisticsBackgroundBrushDx == null) statisticsBackgroundBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(StatisticsBackgroundColor, StatisticsBackgroundOpacity / 100f));
			if (trendLineBrushDx == null) trendLineBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(TrendLineColor, TrendLineOpacity / 100f));

			if (vaLineStrokeDx == null || lastBuiltVALineStyle != VALineStyle)
			{
				if (vaLineStrokeDx != null) vaLineStrokeDx.Dispose();
				vaLineStrokeDx = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = ToDxDashStyle(VALineStyle) });
				lastBuiltVALineStyle = VALineStyle;
			}

			if (trendLineStrokeDx == null || lastBuiltTrendLineStyle != TrendLineStyle)
			{
				if (trendLineStrokeDx != null) trendLineStrokeDx.Dispose();
				trendLineStrokeDx = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = ToDxDashStyle(TrendLineStyle) });
				lastBuiltTrendLineStyle = TrendLineStyle;
			}

			if (textFormatDx == null)
			{
				textFormatDx = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 11f);
				textFormatDx.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				textFormatDx.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
				textFormatDx.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
			}
			if (volumeLabelTextFormatDx == null)
			{
				volumeLabelTextFormatDx = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)Clamp(VolumeLabelFontSize, 6.0, 30.0));
				volumeLabelTextFormatDx.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				volumeLabelTextFormatDx.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
			}
			if (deltaLabelTextFormatDx == null)
			{
				deltaLabelTextFormatDx = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)Clamp(DeltaLabelFontSize, 6.0, 30.0));
				deltaLabelTextFormatDx.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				deltaLabelTextFormatDx.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
			}

			float statisticsFontSize = (float)Clamp(StatisticsFontSize, 8.0, 36.0);
			string statisticsFontFamily = string.IsNullOrWhiteSpace(StatisticsFontFamily) ? "Segoe UI" : StatisticsFontFamily.Trim();
			if (statisticsTextFormatDx == null
				|| Math.Abs(lastBuiltStatisticsFontSize - statisticsFontSize) > 0.001f
				|| !string.Equals(lastBuiltStatisticsFontFamily, statisticsFontFamily, StringComparison.OrdinalIgnoreCase)
				|| lastBuiltStatisticsFontWeight != StatisticsFontWeight)
			{
				if (statisticsTextFormatDx != null) statisticsTextFormatDx.Dispose();
				statisticsTextFormatDx = new TextFormat(
					Core.Globals.DirectWriteFactory,
					statisticsFontFamily,
					ToDxFontWeight(StatisticsFontWeight),
					SharpDX.DirectWrite.FontStyle.Normal,
					statisticsFontSize);
				statisticsTextFormatDx.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				statisticsTextFormatDx.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Near;
				statisticsTextFormatDx.WordWrapping = SharpDX.DirectWrite.WordWrapping.Wrap;
				lastBuiltStatisticsFontSize = statisticsFontSize;
				lastBuiltStatisticsFontFamily = statisticsFontFamily;
				lastBuiltStatisticsFontWeight = StatisticsFontWeight;
			}

			if (UseGradient && (upGradientBrushes == null || downGradientBrushes == null || vaGradientBrushes == null || lastBuiltGradientSteps != steps || Math.Abs(lastBuiltMinBrightness - MinBrightness) > 0.0001f))
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
			if (UseDeltaIntensityColoring && (positiveDeltaIntensityBrushes == null || negativeDeltaIntensityBrushes == null || lastBuiltDeltaIntensitySteps != steps || lastBuiltDeltaIntensityProfileOpacity != ProfileOpacity))
			{
				DisposePalette(ref positiveDeltaIntensityBrushes);
				DisposePalette(ref negativeDeltaIntensityBrushes);
				positiveDeltaIntensityBrushes = BuildDeltaIntensityPalette(DeltaPositiveColor, steps, alpha);
				negativeDeltaIntensityBrushes = BuildDeltaIntensityPalette(DeltaNegativeColor, steps, alpha);
				lastBuiltDeltaIntensitySteps = steps;
				lastBuiltDeltaIntensityProfileOpacity = ProfileOpacity;
			}
			else if (!UseDeltaIntensityColoring && (positiveDeltaIntensityBrushes != null || negativeDeltaIntensityBrushes != null))
			{
				DisposePalette(ref positiveDeltaIntensityBrushes);
				DisposePalette(ref negativeDeltaIntensityBrushes);
				lastBuiltDeltaIntensitySteps = -1;
				lastBuiltDeltaIntensityProfileOpacity = -1;
			}

			lastBuiltProfileOpacity = ProfileOpacity;
			lastBuiltBoxFillOpacity = BoxFillOpacity;
			lastBuiltStatisticsBackgroundOpacity = StatisticsBackgroundOpacity;
			lastBuiltTrendLineOpacity = TrendLineOpacity;
			lastBrushSignature = brushSignature;
			dxResourceRenderTarget = currentTarget;
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
			if (deltaNeutralBrushDx != null) { deltaNeutralBrushDx.Dispose(); deltaNeutralBrushDx = null; }
			DisposePalette(ref positiveDeltaIntensityBrushes);
			DisposePalette(ref negativeDeltaIntensityBrushes);
			if (deltaPositiveLabelBrushDx != null) { deltaPositiveLabelBrushDx.Dispose(); deltaPositiveLabelBrushDx = null; }
			if (deltaNegativeLabelBrushDx != null) { deltaNegativeLabelBrushDx.Dispose(); deltaNegativeLabelBrushDx = null; }
			if (textBrushDx != null) { textBrushDx.Dispose(); textBrushDx = null; }
			if (boxFillBrushDx != null) { boxFillBrushDx.Dispose(); boxFillBrushDx = null; }
			if (statisticsTextBrushDx != null) { statisticsTextBrushDx.Dispose(); statisticsTextBrushDx = null; }
			if (statisticsBackgroundBrushDx != null) { statisticsBackgroundBrushDx.Dispose(); statisticsBackgroundBrushDx = null; }
			if (trendLineBrushDx != null) { trendLineBrushDx.Dispose(); trendLineBrushDx = null; }
			if (vaLineStrokeDx != null) { vaLineStrokeDx.Dispose(); vaLineStrokeDx = null; }
			if (trendLineStrokeDx != null) { trendLineStrokeDx.Dispose(); trendLineStrokeDx = null; }
			DisposePalette(ref upGradientBrushes);
			DisposePalette(ref downGradientBrushes);
			DisposePalette(ref vaGradientBrushes);
			if (textFormatDx != null) { textFormatDx.Dispose(); textFormatDx = null; }
			if (volumeLabelTextFormatDx != null) { volumeLabelTextFormatDx.Dispose(); volumeLabelTextFormatDx = null; }
			if (deltaLabelTextFormatDx != null) { deltaLabelTextFormatDx.Dispose(); deltaLabelTextFormatDx = null; }
			if (statisticsTextFormatDx != null) { statisticsTextFormatDx.Dispose(); statisticsTextFormatDx = null; }
			lastBuiltGradientSteps = -1;
			lastBuiltMinBrightness = -1f;
			lastBuiltProfileOpacity = -1;
			lastBuiltDeltaIntensitySteps = -1;
			lastBuiltDeltaIntensityProfileOpacity = -1;
			lastBuiltBoxFillOpacity = -1;
			lastBuiltStatisticsBackgroundOpacity = -1;
			lastBuiltTrendLineOpacity = -1;
			lastBuiltStatisticsFontSize = -1f;
			lastBuiltStatisticsFontFamily = string.Empty;
			lastBuiltStatisticsFontWeight = (OrcaFixedRangeFontWeight)(-1);
			lastBuiltVALineStyle = (OrcaFixedRangeVALineStyle)(-1);
			lastBuiltTrendLineStyle = (OrcaFixedRangeVALineStyle)(-1);
			lastBrushSignature = string.Empty;
			dxResourceRenderTarget = IntPtr.Zero;
		}

		private void DisposePalette(ref SharpDX.Direct2D1.SolidColorBrush[] palette)
		{
			if (palette != null)
			{
				for (int index = 0; index < palette.Length; index++)
					if (palette[index] != null)
						palette[index].Dispose();
			}
			palette = null;
		}

		private string BuildBrushSignature()
		{
			return Serialize.BrushToString(BoxFillColor) + "|"
				+ Serialize.BrushToString(POCColor) + "|"
				+ Serialize.BrushToString(VAColor) + "|"
				+ Serialize.BrushToString(ProfileUpColor) + "|"
				+ Serialize.BrushToString(ProfileDownColor) + "|"
				+ Serialize.BrushToString(DeltaPositiveColor) + "|"
				+ Serialize.BrushToString(DeltaNegativeColor) + "|"
				+ Serialize.BrushToString(DeltaNeutralColor) + "|"
				+ Serialize.BrushToString(DeltaPositiveLabelColor) + "|"
				+ Serialize.BrushToString(DeltaNegativeLabelColor) + "|"
				+ Serialize.BrushToString(TextColor) + "|"
				+ Serialize.BrushToString(StatisticsTextColor) + "|"
				+ Serialize.BrushToString(StatisticsBackgroundColor) + "|"
				+ Serialize.BrushToString(TrendLineColor) + "|"
				+ DeltaLabelFontSize.ToString("0.###") + "|"
				+ VolumeLabelFontSize.ToString("0.###") + "|"
				+ StatisticsFontSize.ToString("0.###") + "|"
				+ (StatisticsFontFamily ?? string.Empty) + "|"
				+ StatisticsFontWeight.ToString() + "|"
				+ UseDeltaIntensityColoring.ToString() + "|"
				+ DeltaIntensityMinOpacity.ToString("0.###") + "|"
				+ ProfileOpacity.ToString() + "|"
				+ BoxFillOpacity.ToString() + "|"
				+ StatisticsBackgroundOpacity.ToString() + "|"
				+ TrendLineOpacity.ToString() + "|"
				+ TrendLineStyle.ToString();
		}

		private DxColor4 ToDxColor(WpfBrush brush, float opacity)
		{
			WpfSolidColorBrush solidBrush = brush as WpfSolidColorBrush;
			System.Windows.Media.Color color = solidBrush != null ? solidBrush.Color : WpfColors.White;
			return new DxColor4(color.R / 255f, color.G / 255f, color.B / 255f, (color.A / 255f) * opacity);
		}

		private SharpDX.Direct2D1.SolidColorBrush[] BuildGradientPalette(WpfBrush brush, int steps, float opacity)
		{
			SharpDX.Direct2D1.SolidColorBrush[] palette = new SharpDX.Direct2D1.SolidColorBrush[steps];
			DxColor4 baseColor = ToDxColor(brush, opacity);
			float minBrightness = (float)Clamp(MinBrightness, 0.01, 1.0);
			for (int index = 0; index < steps; index++)
			{
				float ratio = index / (float)(steps - 1);
				float brightness = minBrightness + ((1f - minBrightness) * ratio);
				palette[index] = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new DxColor4(baseColor.Red * brightness, baseColor.Green * brightness, baseColor.Blue * brightness, opacity));
			}
			return palette;
		}

		private SharpDX.Direct2D1.SolidColorBrush[] BuildDeltaIntensityPalette(WpfBrush brush, int steps, float maxOpacity)
		{
			SharpDX.Direct2D1.SolidColorBrush[] palette = new SharpDX.Direct2D1.SolidColorBrush[steps];
			DxColor4 baseColor = ToDxColor(brush, 1f);
			float minOpacity = (float)Clamp(DeltaIntensityMinOpacity, 0.0, 1.0);
			for (int index = 0; index < steps; index++)
			{
				float ratio = index / (float)(steps - 1);
				float opacity = maxOpacity * (minOpacity + ((1f - minOpacity) * ratio));
				palette[index] = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new DxColor4(baseColor.Red, baseColor.Green, baseColor.Blue, baseColor.Alpha * opacity));
			}
			return palette;
		}

		private SharpDX.Direct2D1.SolidColorBrush SelectDeltaBrush(double delta, double maxAbsDelta)
		{
			if (!UseDeltaIntensityColoring || maxAbsDelta <= 0)
				return delta > 0 ? deltaPositiveBrushDx : deltaNegativeBrushDx;

			SharpDX.Direct2D1.SolidColorBrush[] palette = delta > 0 ? positiveDeltaIntensityBrushes : negativeDeltaIntensityBrushes;
			if (palette == null || palette.Length == 0)
				return delta > 0 ? deltaPositiveBrushDx : deltaNegativeBrushDx;

			double intensity = Math.Abs(delta) / Math.Max(1.0, maxAbsDelta);
			int index = (int)Math.Round(intensity * (palette.Length - 1));
			if (index < 0) index = 0;
			if (index >= palette.Length) index = palette.Length - 1;
			return palette[index];
		}

		private DashStyle ToDxDashStyle(OrcaFixedRangeVALineStyle lineStyle)
		{
			switch (lineStyle)
			{
				case OrcaFixedRangeVALineStyle.Solid:
					return DashStyle.Solid;
				case OrcaFixedRangeVALineStyle.Dot:
					return DashStyle.Dot;
				case OrcaFixedRangeVALineStyle.DashDot:
					return DashStyle.DashDot;
				default:
					return DashStyle.Dash;
			}
		}

		private static SharpDX.DirectWrite.FontWeight ToDxFontWeight(OrcaFixedRangeFontWeight weight)
		{
			switch (weight)
			{
				case OrcaFixedRangeFontWeight.Light:
					return SharpDX.DirectWrite.FontWeight.Light;
				case OrcaFixedRangeFontWeight.Medium:
					return SharpDX.DirectWrite.FontWeight.Medium;
				case OrcaFixedRangeFontWeight.SemiBold:
					return SharpDX.DirectWrite.FontWeight.SemiBold;
				case OrcaFixedRangeFontWeight.Bold:
					return SharpDX.DirectWrite.FontWeight.Bold;
				default:
					return SharpDX.DirectWrite.FontWeight.Normal;
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

		private void MarkProfileDirty()
		{
			profileDirty = true;
		}

		[NinjaScriptProperty]
		[Display(Name = "Data Mode", Order = 1, GroupName = "1. Data")]
		public OrcaFixedRangeProfileDataMode ProfileDataMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "True Data Source", Order = 2, GroupName = "1. Data",
			Description = "Chart Local Only uses same-chart Tick Replay/local profile maps and never waits for a master provider. The other modes expose the master only as an explicit fallback.")]
		public OrcaFixedRangeProfileDataSourcePreference DataSourcePreference { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Fallback To Chart Estimate", Order = 3, GroupName = "1. Data",
			Description = "When true tick/provider data is unavailable, draw an estimated volume profile from chart bar volume and label it as estimated. Delta is included only when the bars or a local cache provide bid/ask volume.")]
		public bool AllowEstimatedChartFallback { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Data Source Label", Order = 4, GroupName = "1. Data")]
		public bool ShowDataSourceLabel { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Row Count", Order = 4, GroupName = "1. Data")]
		public int RowCount { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Volume Aggregation Mode", Order = 5, GroupName = "1. Data")]
		public OrcaFixedRangeAggregationMode VolumeAggregationMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "Ticks Per Row", Order = 6, GroupName = "1. Data")]
		public int TicksPerRow { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "Dynamic Aggregation Multiplier", Order = 7, GroupName = "1. Data")]
		public double DynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, 40)]
		[Display(Name = "Dynamic Row Min Pixels", Order = 8, GroupName = "1. Data")]
		public int DynamicRowMinPixels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Delta Aggregation Mode", Order = 9, GroupName = "1. Data")]
		public OrcaFixedRangeAggregationMode DeltaAggregationMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Delta Row Count", Order = 10, GroupName = "1. Data")]
		public int DeltaRowCount { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "Delta Ticks Per Row", Order = 11, GroupName = "1. Data")]
		public int DeltaTicksPerRow { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "Delta Dynamic Aggregation Multiplier", Order = 12, GroupName = "1. Data")]
		public double DeltaDynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, 40)]
		[Display(Name = "Delta Dynamic Row Min Pixels", Order = 13, GroupName = "1. Data")]
		public int DeltaDynamicRowMinPixels { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Delta Dynamic Min Compression", Order = 14, GroupName = "1. Data")]
		public int DeltaDynamicMinCompression { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Delta Dynamic Max Compression", Order = 15, GroupName = "1. Data")]
		public int DeltaDynamicMaxCompression { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Value Area Percent", Order = 16, GroupName = "1. Data")]
		public double ValueAreaPercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Volume Profile", Order = 1, GroupName = "2. Display")]
		public bool ShowVolumeProfile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta Profile", Order = 2, GroupName = "2. Display")]
		public bool ShowDeltaProfile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Profile Arrangement", Order = 3, GroupName = "2. Display",
			Description = "Manual uses Volume Side and Delta Side. The fixed arrangements place volume and delta on opposite box edges and point them inward.")]
		public OrcaFixedRangeProfileSideArrangement ProfileSideArrangement { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Volume Side", Order = 4, GroupName = "2. Display")]
		public OrcaFixedRangeProfileSide VolumeSide { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Delta Side", Order = 5, GroupName = "2. Display")]
		public OrcaFixedRangeProfileSide DeltaSide { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Profile Placement", Order = 6, GroupName = "2. Display")]
		public OrcaFixedRangeProfilePlacement ProfilePlacement { get; set; }

		[NinjaScriptProperty]
		[Range(10, 600)]
		[Display(Name = "Max Profile Width Px", Description = "Hard cap applied to each of the volume and delta profile widths.", Order = 7, GroupName = "2. Display")]
		public int MaxProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 600)]
		[Display(Name = "Volume Profile Width Px", Order = 8, GroupName = "2. Display")]
		public int VolumeProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 600)]
		[Display(Name = "Delta Profile Width Px", Order = 9, GroupName = "2. Display")]
		public int DeltaProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Volume Profile Bar Spacing Px", Order = 10, GroupName = "2. Display")]
		public int VolumeProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Delta Profile Bar Spacing Px", Order = 11, GroupName = "2. Display")]
		public int DeltaProfileBarSpacingPx { get; set; }

		[Browsable(false)]
		public int ProfileBarSpacingPx
		{
			get { return VolumeProfileBarSpacingPx; }
			set
			{
				VolumeProfileBarSpacingPx = value;
				DeltaProfileBarSpacingPx = value;
			}
		}

		[NinjaScriptProperty]
		[Display(Name = "Show Volume Labels", Order = 12, GroupName = "2. Display")]
		public bool ShowVolumeLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta Labels", Order = 13, GroupName = "2. Display")]
		public bool ShowDeltaLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Total Volume", Order = 14, GroupName = "2. Display")]
		public bool ShowTotalVolume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Profile Statistics", Description = "Shows the statistics box for the fixed range.", Order = 15, GroupName = "2. Display")]
		public bool ShowProfileStatistics { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Total Delta", Order = 16, GroupName = "2. Display")]
		public bool ShowTotalDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Finish Delta", Order = 17, GroupName = "2. Display")]
		public bool ShowFinishDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta Percent", Order = 18, GroupName = "2. Display")]
		public bool ShowDeltaPercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Point Range", Order = 19, GroupName = "2. Display")]
		public bool ShowPointRange { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Duration", Order = 20, GroupName = "2. Display")]
		public bool ShowDuration { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Statistics Position", Order = 21, GroupName = "2. Display")]
		public OrcaFixedRangeStatisticsPosition StatisticsPosition { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Trend Line", Description = "Draws a line between the start and finish anchors so the range stays visible without the box border.", Order = 22, GroupName = "2. Display")]
		public bool ShowTrendLine { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Trend Line Style", Order = 23, GroupName = "2. Display")]
		public OrcaFixedRangeVALineStyle TrendLineStyle { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 10.0)]
		[Display(Name = "Trend Line Thickness", Order = 24, GroupName = "2. Display")]
		public float TrendLineThickness { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Trend Line Opacity", Order = 25, GroupName = "2. Display")]
		public int TrendLineOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show POC", Order = 1, GroupName = "3. References")]
		public bool ShowPOC { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Value Area", Order = 2, GroupName = "3. References")]
		public bool ShowValueArea { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Value Area Color", Order = 3, GroupName = "3. References")]
		public bool ShowVAColor { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Value Area Lines", Order = 4, GroupName = "3. References")]
		public bool ShowVALines { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VAH", Order = 5, GroupName = "3. References")]
		public bool ShowVAH { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VAL", Order = 6, GroupName = "3. References")]
		public bool ShowVAL { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 8.0)]
		[Display(Name = "VA Line Thickness", Order = 7, GroupName = "3. References")]
		public float VALineThickness { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Line Style", Order = 8, GroupName = "3. References")]
		public OrcaFixedRangeVALineStyle VALineStyle { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Box Border", Order = 1, GroupName = "4. Box")]
		public bool ShowBoxBorder { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Box Fill Opacity", Order = 2, GroupName = "4. Box")]
		public int BoxFillOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 255)]
		[Display(Name = "Profile Opacity", Order = 1, GroupName = "5. Style")]
		public int ProfileOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Gradient", Order = 2, GroupName = "5. Style")]
		public bool UseGradient { get; set; }

		[NinjaScriptProperty]
		[Range(2, 64)]
		[Display(Name = "Gradient Steps", Order = 3, GroupName = "5. Style")]
		public int GradientSteps { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 1.0)]
		[Display(Name = "Min Brightness", Order = 4, GroupName = "5. Style")]
		public float MinBrightness { get; set; }

		[NinjaScriptProperty]
		[Range(6.0, 30.0)]
		[Display(Name = "Volume Label Font Size", Order = 5, GroupName = "5. Style")]
		public float VolumeLabelFontSize { get; set; }

		[NinjaScriptProperty]
		[Range(6.0, 30.0)]
		[Display(Name = "Delta Label Font Size", Order = 6, GroupName = "5. Style")]
		public float DeltaLabelFontSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Statistics Font Family", Order = 7, GroupName = "5. Style")]
		public string StatisticsFontFamily { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Statistics Font Weight", Order = 8, GroupName = "5. Style")]
		public OrcaFixedRangeFontWeight StatisticsFontWeight { get; set; }

		[NinjaScriptProperty]
		[Range(8.0, 36.0)]
		[Display(Name = "Statistics Font Size", Order = 9, GroupName = "5. Style")]
		public float StatisticsFontSize { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Statistics Background Opacity", Order = 10, GroupName = "5. Style")]
		public int StatisticsBackgroundOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 24.0)]
		[Display(Name = "Statistics Corner Radius", Order = 11, GroupName = "5. Style")]
		public float StatisticsCornerRadius { get; set; }

		[XmlIgnore]
		[Display(Name = "Box Fill Color", Order = 1, GroupName = "6. Colors")]
		public WpfBrush BoxFillColor { get; set; }

		[Browsable(false)]
		public string BoxFillColorSerialize
		{
			get { return Serialize.BrushToString(BoxFillColor); }
			set { BoxFillColor = Serialize.StringToBrush(value); }
		}

		[Display(Name = "Box Border Stroke", Order = 2, GroupName = "6. Colors")]
		public Stroke BoxBorderStroke { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Color", Order = 3, GroupName = "6. Colors")]
		public WpfBrush POCColor { get; set; }

		[Browsable(false)]
		public string POCColorSerialize
		{
			get { return Serialize.BrushToString(POCColor); }
			set { POCColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Value Area Color", Order = 4, GroupName = "6. Colors")]
		public WpfBrush VAColor { get; set; }

		[Browsable(false)]
		public string VAColorSerialize
		{
			get { return Serialize.BrushToString(VAColor); }
			set { VAColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Profile Up Color", Order = 5, GroupName = "6. Colors")]
		public WpfBrush ProfileUpColor { get; set; }

		[Browsable(false)]
		public string ProfileUpColorSerialize
		{
			get { return Serialize.BrushToString(ProfileUpColor); }
			set { ProfileUpColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Profile Down Color", Order = 6, GroupName = "6. Colors")]
		public WpfBrush ProfileDownColor { get; set; }

		[Browsable(false)]
		public string ProfileDownColorSerialize
		{
			get { return Serialize.BrushToString(ProfileDownColor); }
			set { ProfileDownColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Delta Positive Color", Order = 7, GroupName = "6. Colors")]
		public WpfBrush DeltaPositiveColor { get; set; }

		[Browsable(false)]
		public string DeltaPositiveColorSerialize
		{
			get { return Serialize.BrushToString(DeltaPositiveColor); }
			set { DeltaPositiveColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Delta Negative Color", Order = 8, GroupName = "6. Colors")]
		public WpfBrush DeltaNegativeColor { get; set; }

		[Browsable(false)]
		public string DeltaNegativeColorSerialize
		{
			get { return Serialize.BrushToString(DeltaNegativeColor); }
			set { DeltaNegativeColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Delta Neutral Color", Order = 9, GroupName = "6. Colors")]
		public WpfBrush DeltaNeutralColor { get; set; }

		[Browsable(false)]
		public string DeltaNeutralColorSerialize
		{
			get { return Serialize.BrushToString(DeltaNeutralColor); }
			set { DeltaNeutralColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "Use Delta Intensity Color", Order = 10, GroupName = "6. Colors")]
		public bool UseDeltaIntensityColoring { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Delta Intensity Min Opacity", Order = 11, GroupName = "6. Colors")]
		public float DeltaIntensityMinOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Delta Positive Label Color", Order = 12, GroupName = "6. Colors")]
		public WpfBrush DeltaPositiveLabelColor { get; set; }

		[Browsable(false)]
		public string DeltaPositiveLabelColorSerialize
		{
			get { return Serialize.BrushToString(DeltaPositiveLabelColor); }
			set { DeltaPositiveLabelColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Delta Negative Label Color", Order = 13, GroupName = "6. Colors")]
		public WpfBrush DeltaNegativeLabelColor { get; set; }

		[Browsable(false)]
		public string DeltaNegativeLabelColorSerialize
		{
			get { return Serialize.BrushToString(DeltaNegativeLabelColor); }
			set { DeltaNegativeLabelColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Text Color", Order = 14, GroupName = "6. Colors")]
		public WpfBrush TextColor { get; set; }

		[Browsable(false)]
		public string TextColorSerialize
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Statistics Text Color", Order = 15, GroupName = "6. Colors")]
		public WpfBrush StatisticsTextColor { get; set; }

		[Browsable(false)]
		public string StatisticsTextColorSerialize
		{
			get { return Serialize.BrushToString(StatisticsTextColor); }
			set { StatisticsTextColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Statistics Background Color", Order = 16, GroupName = "6. Colors")]
		public WpfBrush StatisticsBackgroundColor { get; set; }

		[Browsable(false)]
		public string StatisticsBackgroundColorSerialize
		{
			get { return Serialize.BrushToString(StatisticsBackgroundColor); }
			set { StatisticsBackgroundColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Trend Line Color", Order = 17, GroupName = "6. Colors")]
		public WpfBrush TrendLineColor { get; set; }

		[Browsable(false)]
		public string TrendLineColorSerialize
		{
			get { return Serialize.BrushToString(TrendLineColor); }
			set { TrendLineColor = Serialize.StringToBrush(value); }
		}
	}
}
