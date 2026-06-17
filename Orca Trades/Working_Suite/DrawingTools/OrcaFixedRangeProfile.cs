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
}

namespace NinjaTrader.NinjaScript.DrawingTools
{
	public class OrcaFixedRangeProfile : DrawingTool
	{
		private const double PriceEpsilon = 1E-09;
		private const double CursorSensitivity = 15.0;
		private const float BoxPaddingPx = 3f;
		private const float OutsideProfileGapPx = 6f;
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
		private StrokeStyle vaLineStrokeDx;
		private SharpDX.Direct2D1.SolidColorBrush[] upGradientBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] downGradientBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] vaGradientBrushes;
		private TextFormat textFormatDx;
		private TextFormat volumeLabelTextFormatDx;
		private TextFormat deltaLabelTextFormatDx;
		private int lastBuiltGradientSteps = -1;
		private float lastBuiltMinBrightness = -1f;
		private int lastBuiltProfileOpacity = -1;
		private int lastBuiltDeltaIntensitySteps = -1;
		private int lastBuiltDeltaIntensityProfileOpacity = -1;
		private int lastBuiltBoxFillOpacity = -1;
		private OrcaFixedRangeVALineStyle lastBuiltVALineStyle = (OrcaFixedRangeVALineStyle)(-1);
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
				noDataLabel = "Range too small";
				return;
			}

			int firstBar = FindFirstBarIndexAtOrAfter(bars, startTime);
			int lastBar = FindLastBarIndexAtOrBefore(bars, endTime);
			if (firstBar < 0 || lastBar < 0 || firstBar > lastBar)
			{
				profileResult.Clear();
				deltaResult.Clear();
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
				int sharedBucketSeconds;
				string sharedSourceName;
				bool gotSharedSnapshot = OrcaProfileDataCache.TrySnapshotOrderFlowPriceMaps(instrumentKey, startTime, endTime, out trueDataSnapshot, out sharedBucketSeconds, out sharedSourceName);
				if (gotSharedSnapshot)
				{
					effectiveDataKey = instrumentKey + "|orderflow|" + (sharedSourceName ?? string.Empty) + "|bucket0";
					dataSourceLabel = "Source: master tick";
					useTrueProfileData = true;
				}
				else
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
					if (AllowEstimatedChartFallback)
					{
						effectiveDataKey = dataKey + "|estimated-bars";
						trueDataRevision = -1;
						dataSourceLabel = BuildEstimatedFallbackLabel(sharedBucketSeconds, instrumentKey, chartDataKey, dataKey);
					}
					else
					{
						profileResult.Clear();
						deltaResult.Clear();
						dataSourceLabel = string.Empty;
						if (sharedBucketSeconds > 0)
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

			bool volumeOk;
			bool deltaOk;
			if (useTrueProfileData && trueDataSnapshot != null)
			{
				volumeOk = OrcaVolumeProfileCore.BuildFixedRangeFromPriceMaps(trueDataSnapshot.VolumeByBar, trueDataSnapshot.UpVolumeByBar, trueDataSnapshot.DownVolumeByBar, 0, trueDataSnapshot.ToIndex, lowPrice, highPrice, RowCount, resolvedTicksPerRow, useVolumeTicksPerRow, ValueAreaPercent, tickSize, profileResult);
				deltaOk = OrcaVolumeProfileCore.BuildFixedRangeFromPriceMaps(trueDataSnapshot.VolumeByBar, trueDataSnapshot.UpVolumeByBar, trueDataSnapshot.DownVolumeByBar, 0, trueDataSnapshot.ToIndex, lowPrice, highPrice, DeltaRowCount, resolvedDeltaTicksPerRow, useDeltaTicksPerRow, ValueAreaPercent, tickSize, deltaResult);
			}
			else
			{
				volumeOk = OrcaVolumeProfileCore.BuildFixedRangeFromBars(bars, firstBar, lastBar, lowPrice, highPrice, RowCount, resolvedTicksPerRow, useVolumeTicksPerRow, ValueAreaPercent, tickSize, profileResult);
				deltaOk = OrcaVolumeProfileCore.BuildFixedRangeFromBars(bars, firstBar, lastBar, lowPrice, highPrice, DeltaRowCount, resolvedDeltaTicksPerRow, useDeltaTicksPerRow, ValueAreaPercent, tickSize, deltaResult);
			}

			totalVolumeLabel = volumeOk ? "Vol " + FormatVolume(profileResult.TotalVolume) : string.Empty;
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
				return "Source: chart true VAP";

			if (snapshot.SourceName.IndexOf("OrcaPrints", StringComparison.OrdinalIgnoreCase) >= 0)
				return "Source: chart live prints";
			if (snapshot.SourceName.IndexOf("Candle", StringComparison.OrdinalIgnoreCase) >= 0)
				return "Source: chart candle VAP";

			return "Source: chart true VAP";
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
			float bandLeft;
			float bandRight;
			float requestedWidth = Math.Max(10f, MaxProfileWidthPx);

			if (useArrangement && ProfilePlacement == OrcaFixedRangeProfilePlacement.InsideSelectedBox && ShowVolumeProfile && ShowDeltaProfile)
			{
				AssignInsideEdgeArrangementTracks(boxRect, requestedWidth, volumeSide, deltaSide, ref volumeTrack, ref deltaTrack);
				return;
			}

			if (ProfilePlacement == OrcaFixedRangeProfilePlacement.OutsideRightEdge)
			{
				bandLeft = boxRect.Right + OutsideProfileGapPx;
				bandRight = Math.Min(panelRight, bandLeft + requestedWidth);
			}
			else if (ProfilePlacement == OrcaFixedRangeProfilePlacement.OutsideLeftEdge)
			{
				bandRight = boxRect.Left - OutsideProfileGapPx;
				bandLeft = Math.Max(panelLeft, bandRight - requestedWidth);
			}
			else
			{
				float innerWidth = Math.Max(1f, boxRect.Width - (BoxPaddingPx * 2f));
				float bandWidth = Math.Min(requestedWidth, innerWidth);
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
					float mid = bandLeft + ((bandRight - bandLeft) * 0.5f);
					AssignSideTrack(ref volumeTrack, volumeSide, bandLeft, mid - (TrackGapPx * 0.5f), mid + (TrackGapPx * 0.5f), bandRight, useArrangement);
					AssignSideTrack(ref deltaTrack, deltaSide, bandLeft, mid - (TrackGapPx * 0.5f), mid + (TrackGapPx * 0.5f), bandRight, useArrangement);
				}
				else
				{
					float mid = bandLeft + ((bandRight - bandLeft) * 0.5f);
					volumeTrack.Left = bandLeft;
					volumeTrack.Right = Math.Max(bandLeft, mid - (TrackGapPx * 0.5f));
					deltaTrack.Left = Math.Min(bandRight, mid + (TrackGapPx * 0.5f));
					deltaTrack.Right = bandRight;
					volumeTrack.DrawFromRight = ShouldDrawFromRight(volumeSide, useArrangement);
					deltaTrack.DrawFromRight = ShouldDrawFromRight(deltaSide, useArrangement);
				}
			}
			else if (ShowVolumeProfile)
			{
				volumeTrack.Left = bandLeft;
				volumeTrack.Right = bandRight;
				volumeTrack.DrawFromRight = ShouldDrawFromRight(volumeSide, useArrangement);
			}
			else if (ShowDeltaProfile)
			{
				deltaTrack.Left = bandLeft;
				deltaTrack.Right = bandRight;
				deltaTrack.DrawFromRight = ShouldDrawFromRight(deltaSide, useArrangement);
			}

			volumeTrack.IsVisible = volumeTrack.IsVisible && volumeTrack.Right > volumeTrack.Left + 1f;
			deltaTrack.IsVisible = deltaTrack.IsVisible && deltaTrack.Right > deltaTrack.Left + 1f;
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

		private void AssignInsideEdgeArrangementTracks(DxRectangleF boxRect, float requestedWidth, OrcaFixedRangeProfileSide volumeSide, OrcaFixedRangeProfileSide deltaSide, ref ProfileTrack volumeTrack, ref ProfileTrack deltaTrack)
		{
			float innerLeft = boxRect.Left + BoxPaddingPx;
			float innerRight = boxRect.Right - BoxPaddingPx;
			float innerWidth = Math.Max(1f, innerRight - innerLeft);
			float trackWidth = Math.Min(Math.Max(4f, (requestedWidth - TrackGapPx) * 0.5f), Math.Max(1f, (innerWidth - TrackGapPx) * 0.5f));

			AssignInsideEdgeTrack(ref volumeTrack, volumeSide, innerLeft, innerRight, trackWidth);
			AssignInsideEdgeTrack(ref deltaTrack, deltaSide, innerLeft, innerRight, trackWidth);

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

			float textLeft = Math.Max(track.Left, drawX + 1f);
			float textRight = Math.Min(track.Right, drawX + barWidth - 2f);
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
			if (brushSignature != lastBrushSignature || lastBuiltProfileOpacity != ProfileOpacity || lastBuiltBoxFillOpacity != BoxFillOpacity)
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

			if (vaLineStrokeDx == null || lastBuiltVALineStyle != VALineStyle)
			{
				if (vaLineStrokeDx != null) vaLineStrokeDx.Dispose();
				vaLineStrokeDx = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = ToDxDashStyle(VALineStyle) });
				lastBuiltVALineStyle = VALineStyle;
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
				deltaLabelTextFormatDx.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				deltaLabelTextFormatDx.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
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
			if (vaLineStrokeDx != null) { vaLineStrokeDx.Dispose(); vaLineStrokeDx = null; }
			DisposePalette(ref upGradientBrushes);
			DisposePalette(ref downGradientBrushes);
			DisposePalette(ref vaGradientBrushes);
			if (textFormatDx != null) { textFormatDx.Dispose(); textFormatDx = null; }
			if (volumeLabelTextFormatDx != null) { volumeLabelTextFormatDx.Dispose(); volumeLabelTextFormatDx = null; }
			if (deltaLabelTextFormatDx != null) { deltaLabelTextFormatDx.Dispose(); deltaLabelTextFormatDx = null; }
			lastBuiltGradientSteps = -1;
			lastBuiltMinBrightness = -1f;
			lastBuiltProfileOpacity = -1;
			lastBuiltDeltaIntensitySteps = -1;
			lastBuiltDeltaIntensityProfileOpacity = -1;
			lastBuiltBoxFillOpacity = -1;
			lastBuiltVALineStyle = (OrcaFixedRangeVALineStyle)(-1);
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
				+ DeltaLabelFontSize.ToString("0.###") + "|"
				+ VolumeLabelFontSize.ToString("0.###") + "|"
				+ UseDeltaIntensityColoring.ToString() + "|"
				+ DeltaIntensityMinOpacity.ToString("0.###") + "|"
				+ ProfileOpacity.ToString() + "|"
				+ BoxFillOpacity.ToString();
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
		[Display(Name = "Fallback To Chart Estimate", Order = 2, GroupName = "1. Data",
			Description = "When true tick/provider data is unavailable, draw an estimated profile from the chart bars and label it as estimated.")]
		public bool AllowEstimatedChartFallback { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Data Source Label", Order = 3, GroupName = "1. Data")]
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
		[Display(Name = "Max Profile Width Px", Order = 7, GroupName = "2. Display")]
		public int MaxProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Volume Profile Bar Spacing Px", Order = 8, GroupName = "2. Display")]
		public int VolumeProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Delta Profile Bar Spacing Px", Order = 9, GroupName = "2. Display")]
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
		[Display(Name = "Show Volume Labels", Order = 10, GroupName = "2. Display")]
		public bool ShowVolumeLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta Labels", Order = 11, GroupName = "2. Display")]
		public bool ShowDeltaLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Total Volume", Order = 12, GroupName = "2. Display")]
		public bool ShowTotalVolume { get; set; }

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
	}
}
