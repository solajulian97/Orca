using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Trend Channel IDrawingTool.
/// </summary>
public class TrendChannel : PriceLevelContainer
{
	private int areaOpacity;

	private Brush areaBrush;

	private readonly DeviceBrush areaDeviceBrush = new DeviceBrush();

	private const double cursorSensitivity = 15.0;

	private ChartAnchor editingAnchor;

	private PathGeometry fillMainGeometry;

	private Vector2[] fillMainFig;

	private PathGeometry fillLeftGeometry;

	private Vector2[] fillLeftFig;

	private PathGeometry fillRightGeometry;

	private Vector2[] fillRightFig;

	private bool isReadyForMovingSecondLeg;

	private bool updateEndAnc;

	public override object Icon => Icons.DrawTrendChannel;

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
	public Brush AreaBrush
	{
		get
		{
			return areaBrush;
		}
		set
		{
			areaBrush = BrushExtensions.ToFrozenBrush(value);
		}
	}

	[Browsable(false)]
	public string AreaBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(AreaBrush);
		}
		set
		{
			AreaBrush = Serialize.StringToBrush(value);
		}
	}

	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 2)]
	public int AreaOpacity
	{
		get
		{
			return areaOpacity;
		}
		set
		{
			int num = Math.Max(0, Math.Min(100, value));
			if (num != areaOpacity)
			{
				areaOpacity = num;
				areaDeviceBrush.Brush = null;
			}
		}
	}

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { TrendStartAnchor, TrendEndAnchor, ParallelStartAnchor };

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", GroupName = "NinjaScriptLines")]
	public bool IsExtendedLinesRight { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", GroupName = "NinjaScriptLines")]
	public bool IsExtendedLinesLeft { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTrendChannelTrendStroke", GroupName = "NinjaScriptLines", Order = 1)]
	public Stroke Stroke { get; set; }

	[Display(Order = 10)]
	[ExcludeFromTemplate]
	public ChartAnchor TrendEndAnchor { get; set; }

	[Display(Order = 0)]
	[ExcludeFromTemplate]
	public ChartAnchor TrendStartAnchor { get; set; }

	[Display(Order = 20)]
	[ExcludeFromTemplate]
	public ChartAnchor ParallelStartAnchor { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTrendChannelParallelStroke", GroupName = "NinjaScriptLines", Order = 2)]
	public Stroke ParallelStroke { get; set; }

	public override bool SupportsAlerts => true;

	public override void CopyTo(NinjaScript ninjaScript)
	{
		base.CopyTo(ninjaScript);
		if (ninjaScript is TrendChannel trendChannel)
		{
			trendChannel.isReadyForMovingSecondLeg = isReadyForMovingSecondLeg;
		}
	}

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		if (areaDeviceBrush != null)
		{
			areaDeviceBrush.RenderTarget = null;
		}
		PathGeometry obj = fillLeftGeometry;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		PathGeometry obj2 = fillMainGeometry;
		if (obj2 != null)
		{
			((DisposeBase)obj2).Dispose();
		}
		PathGeometry obj3 = fillRightGeometry;
		if (obj3 != null)
		{
			((DisposeBase)obj3).Dispose();
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		State state = ((NinjaScript)this).State;
		if ((int)state != 1)
		{
			if ((int)state != 2)
			{
				if ((int)state == 8)
				{
					((DrawingTool)this).Dispose();
				}
			}
			else if (base.PriceLevels.Count == 0)
			{
				base.PriceLevels.Add(new PriceLevel(0.0, Brushes.Transparent));
				base.PriceLevels.Add(new PriceLevel(100.0, Brushes.Transparent));
			}
			return;
		}
		((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolTrendChannelDescription;
		((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolTrendChannel;
		((DrawingTool)this).DrawingState = (DrawingState)0;
		TrendStartAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this,
			IsBrowsable = true,
			DisplayName = Resource.NinjaScriptDrawingToolTrendChannelStart1AnchorDisplayName
		};
		TrendEndAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this,
			IsBrowsable = true,
			DisplayName = Resource.NinjaScriptDrawingToolTrendChannelEnd1AnchorDisplayName
		};
		ParallelStartAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this,
			IsBrowsable = true,
			DisplayName = Resource.NinjaScriptDrawingToolTrendChannelStart2AnchorDisplayName,
			Time = DateTime.MinValue
		};
		ParallelStroke = new Stroke((Brush)Brushes.SeaGreen, 2f);
		Stroke = new Stroke((Brush)Brushes.SeaGreen, 2f);
		AreaBrush = Brushes.SeaGreen;
		AreaOpacity = 0;
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		if (base.PriceLevels == null || base.PriceLevels.Count == 0)
		{
			yield break;
		}
		foreach (PriceLevel priceLevel in base.PriceLevels)
		{
			yield return new AlertConditionItem
			{
				Name = priceLevel.Name,
				ShouldOnlyDisplayName = true,
				Tag = priceLevel
			};
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		switch ((int)drawingState)
		{
		case 0:
			return Cursors.Pen;
		case 3:
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		case 1:
			if (editingAnchor == null)
			{
				return null;
			}
			if (!((DrawingTool)this).IsLocked)
			{
				if (editingAnchor != TrendStartAnchor)
				{
					return Cursors.SizeNWSE;
				}
				return Cursors.SizeNESW;
			}
			return Cursors.No;
		default:
		{
			Point point2 = TrendStartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point3 = ParallelStartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (closestAnchor != null)
			{
				if (!((DrawingTool)this).IsLocked)
				{
					if (closestAnchor != TrendStartAnchor)
					{
						return Cursors.SizeNWSE;
					}
					return Cursors.SizeNESW;
				}
				return Cursors.Arrow;
			}
			Point point4 = TrendEndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point5 = point3 + (point4 - point2);
			Vector vector = point4 - point2;
			Vector vector2 = point5 - point3;
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(point2, point4);
			Point extendedPoint2 = ((DrawingTool)this).GetExtendedPoint(point3, point5);
			Point extendedPoint3 = ((DrawingTool)this).GetExtendedPoint(point4, point2);
			Point extendedPoint4 = ((DrawingTool)this).GetExtendedPoint(point5, point3);
			if (IsExtendedLinesLeft)
			{
				Vector vector3 = extendedPoint3 - point2;
				Vector vector4 = extendedPoint4 - point3;
				if (MathHelper.IsPointAlongVector(point, point2, vector3, 15.0) || MathHelper.IsPointAlongVector(point, point3, vector4, 15.0))
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
			}
			if (IsExtendedLinesRight)
			{
				Vector vector5 = extendedPoint - point4;
				Vector vector6 = extendedPoint2 - point5;
				if (MathHelper.IsPointAlongVector(point, point4, vector5, 15.0) || MathHelper.IsPointAlongVector(point, point5, vector6, 15.0))
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
			}
			if (MathHelper.IsPointAlongVector(point, point2, vector, 15.0) || MathHelper.IsPointAlongVector(point, point3, vector2, 15.0))
			{
				if (!((DrawingTool)this).IsLocked)
				{
					return Cursors.SizeAll;
				}
				return Cursors.Arrow;
			}
			return null;
		}
		}
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = TrendStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = TrendEndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = new Point((point.X + point2.X) / 2.0, (point.Y + point2.Y) / 2.0);
		Point point4 = ParallelStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point5 = point4 + (point2 - point);
		Point point6 = new Point((point4.X + point5.X) / 2.0, (point4.Y + point5.Y) / 2.0);
		if ((int)((DrawingTool)this).DrawingState == 0 && !isReadyForMovingSecondLeg)
		{
			return new Point[3] { point, point3, point2 };
		}
		return new Point[6] { point, point3, point2, point4, point6, point5 };
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Expected I4, but got Unknown
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Invalid comparison between Unknown and I4
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Invalid comparison between Unknown and I4
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Invalid comparison between Unknown and I4
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Invalid comparison between Unknown and I4
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Invalid comparison between Unknown and I4
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Invalid comparison between Unknown and I4
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Invalid comparison between Unknown and I4
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = TrendStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = TrendEndAnchor.GetPoint(chartControl, val, chartScale, true);
		Vector vector = ((conditionItem.Tag as PriceLevel)?.Value ?? 0.0) / 100.0 * (ParallelStartAnchor.GetPoint(chartControl, val, chartScale, true) - point);
		Vector vector2 = point2 - point;
		Point point3 = new Point(point.X + vector.X, point.Y + vector.Y);
		Point point4 = new Point(point3.X + vector2.X, point3.Y + vector2.Y);
		double num = chartControl.GetXByTime(values[0].Time);
		double y = chartScale.GetYByValue(values[0].Value);
		Point alertStartPoint = ((point3.X <= point4.X) ? point3 : point4);
		Point alertEndPoint = ((point4.X >= point3.X) ? point4 : point3);
		Point point5 = new Point(num, y);
		if (IsExtendedLinesLeft)
		{
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(alertEndPoint, alertStartPoint);
			if (extendedPoint.X > -1.0 || extendedPoint.Y > -1.0)
			{
				alertStartPoint = extendedPoint;
			}
		}
		if (IsExtendedLinesRight)
		{
			Point extendedPoint2 = ((DrawingTool)this).GetExtendedPoint(alertStartPoint, alertEndPoint);
			if (extendedPoint2.X > -1.0 || extendedPoint2.Y > -1.0)
			{
				alertEndPoint = extendedPoint2;
			}
		}
		if (num < alertStartPoint.X || num > alertEndPoint.X)
		{
			return false;
		}
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, point5);
		Condition val2 = condition;
		switch ((int)val2)
		{
		case 3:
			return (int)pointLineLocation == 0;
		case 4:
			if ((int)pointLineLocation != 0)
			{
				return (int)pointLineLocation == 2;
			}
			return true;
		case 5:
			return (int)pointLineLocation == 1;
		case 6:
			if ((int)pointLineLocation != 1)
			{
				return (int)pointLineLocation == 2;
			}
			return true;
		case 2:
			return (int)pointLineLocation == 2;
		case 7:
			return (int)pointLineLocation != 2;
		case 0:
		case 1:
			return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
		default:
			return false;
		}
		bool Predicate(ChartAlertValue v)
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Invalid comparison between Unknown and I4
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Invalid comparison between Unknown and I4
			double x = chartControl.GetXByTime(v.Time);
			double y2 = chartScale.GetYByValue(v.Value);
			Point point6 = new Point(x, y2);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, point6);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		if (((DrawingTool)this).Anchors.Any((ChartAnchor a) => a.Time >= firstTimeOnChart && a.Time <= lastTimeOnChart))
		{
			return true;
		}
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = TrendStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = TrendEndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = ParallelStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point4 = point3 + (point2 - point);
		Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(point, point2);
		Point extendedPoint2 = ((DrawingTool)this).GetExtendedPoint(point3, point4);
		Point extendedPoint3 = ((DrawingTool)this).GetExtendedPoint(point2, point);
		Point extendedPoint4 = ((DrawingTool)this).GetExtendedPoint(point4, point3);
		Point[] source = new Point[4] { extendedPoint, extendedPoint2, extendedPoint3, extendedPoint4 };
		double num = source.Select((Point p) => p.X).Min();
		double num2 = source.Select((Point p) => p.X).Max();
		DateTime timeByX = chartControl.GetTimeByX((int)num);
		DateTime timeByX2 = chartControl.GetTimeByX((int)point.X);
		DateTime timeByX3 = chartControl.GetTimeByX((int)point2.X);
		DateTime timeByX4 = chartControl.GetTimeByX((int)num2);
		DateTime[] array = new DateTime[4] { timeByX, timeByX2, timeByX3, timeByX4 };
		foreach (DateTime dateTime in array)
		{
			if (dateTime >= firstTimeOnChart && dateTime <= lastTimeOnChart)
			{
				return true;
			}
		}
		if ((timeByX <= firstTimeOnChart && timeByX4 >= lastTimeOnChart) || (timeByX2 <= firstTimeOnChart && timeByX3 >= lastTimeOnChart) || (timeByX3 <= firstTimeOnChart && timeByX2 >= lastTimeOnChart))
		{
			return true;
		}
		return false;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible || !((DrawingTool)this).Anchors.Any((ChartAnchor a) => !a.IsEditing))
		{
			return;
		}
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			((ChartObject)this).MinValue = Math.Min(anchor.Price, ((ChartObject)this).MinValue);
			((ChartObject)this).MaxValue = Math.Max(anchor.Price, ((ChartObject)this).MaxValue);
		}
	}

	public override void OnEdited(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, DrawingTool oldinstance)
	{
		SetParallelLine(chartControl, initialSet: false);
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if (drawingState - 2 > 1)
			{
				return;
			}
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = true;
				((DrawingTool)this).DrawingState = (DrawingState)1;
			}
			else if (editingAnchor == null || ((DrawingTool)this).IsLocked)
			{
				if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == null)
				{
					((ChartObject)this).IsSelected = false;
				}
				else
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
			}
			return;
		}
		if (TrendStartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(TrendStartAnchor);
			dataPoint.CopyDataValues(TrendEndAnchor);
			TrendStartAnchor.IsEditing = false;
		}
		else if (TrendEndAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(TrendEndAnchor);
			TrendEndAnchor.IsEditing = false;
		}
		if (!TrendStartAnchor.IsEditing && !TrendEndAnchor.IsEditing)
		{
			SetParallelLine(chartControl, ParallelStartAnchor.IsEditing);
		}
		if (!isReadyForMovingSecondLeg)
		{
			if (!ParallelStartAnchor.IsEditing)
			{
				isReadyForMovingSecondLeg = true;
			}
		}
		else
		{
			isReadyForMovingSecondLeg = false;
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Invalid comparison between Unknown and I4
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (TrendEndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(TrendEndAnchor);
			}
			else if (isReadyForMovingSecondLeg)
			{
				ParallelStartAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1)
		{
			if (!TrendStartAnchor.IsEditing && !ParallelStartAnchor.IsEditing && TrendEndAnchor.IsEditing)
			{
				TrendEndAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			if (!TrendEndAnchor.IsEditing && !ParallelStartAnchor.IsEditing && TrendStartAnchor.IsEditing)
			{
				TrendStartAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			if (!TrendStartAnchor.IsEditing && !TrendEndAnchor.IsEditing && ParallelStartAnchor.IsEditing)
			{
				ParallelStartAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			if (!TrendStartAnchor.IsEditing && !ParallelStartAnchor.IsEditing && !TrendEndAnchor.IsEditing)
			{
				((DrawingTool)this).DrawingState = (DrawingState)3;
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 3)
		{
			ChartAnchor[] array = (ChartAnchor[])(object)new ChartAnchor[2] { TrendStartAnchor, TrendEndAnchor };
			for (int i = 0; i < array.Length; i++)
			{
				array[i].MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			ParallelStartAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			if ((int)((DrawingTool)this).DrawingState == 1 && updateEndAnc)
			{
				updateEndAnc = false;
			}
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = false;
			}
			editingAnchor = null;
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Expected O, but got Unknown
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1232: Unknown result type (might be due to invalid IL or missing references)
		//IL_1236: Unknown result type (might be due to invalid IL or missing references)
		//IL_128b: Unknown result type (might be due to invalid IL or missing references)
		//IL_128f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_2188: Unknown result type (might be due to invalid IL or missing references)
		//IL_218f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2209: Unknown result type (might be due to invalid IL or missing references)
		//IL_2210: Unknown result type (might be due to invalid IL or missing references)
		//IL_2272: Unknown result type (might be due to invalid IL or missing references)
		//IL_2279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0543: Unknown result type (might be due to invalid IL or missing references)
		//IL_0548: Unknown result type (might be due to invalid IL or missing references)
		//IL_0556: Unknown result type (might be due to invalid IL or missing references)
		//IL_055b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0569: Unknown result type (might be due to invalid IL or missing references)
		//IL_056e: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_058e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_073f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0744: Unknown result type (might be due to invalid IL or missing references)
		//IL_0752: Unknown result type (might be due to invalid IL or missing references)
		//IL_0757: Unknown result type (might be due to invalid IL or missing references)
		//IL_0765: Unknown result type (might be due to invalid IL or missing references)
		//IL_076a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0778: Unknown result type (might be due to invalid IL or missing references)
		//IL_077d: Unknown result type (might be due to invalid IL or missing references)
		//IL_078a: Unknown result type (might be due to invalid IL or missing references)
		//IL_078f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1408: Unknown result type (might be due to invalid IL or missing references)
		//IL_140d: Unknown result type (might be due to invalid IL or missing references)
		//IL_141b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1420: Unknown result type (might be due to invalid IL or missing references)
		//IL_142e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1433: Unknown result type (might be due to invalid IL or missing references)
		//IL_1441: Unknown result type (might be due to invalid IL or missing references)
		//IL_1446: Unknown result type (might be due to invalid IL or missing references)
		//IL_1453: Unknown result type (might be due to invalid IL or missing references)
		//IL_1458: Unknown result type (might be due to invalid IL or missing references)
		//IL_05af: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b9: Expected O, but got Unknown
		//IL_05d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_1605: Unknown result type (might be due to invalid IL or missing references)
		//IL_160a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1618: Unknown result type (might be due to invalid IL or missing references)
		//IL_161d: Unknown result type (might be due to invalid IL or missing references)
		//IL_162b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1630: Unknown result type (might be due to invalid IL or missing references)
		//IL_163e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1643: Unknown result type (might be due to invalid IL or missing references)
		//IL_1650: Unknown result type (might be due to invalid IL or missing references)
		//IL_1655: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc5: Unknown result type (might be due to invalid IL or missing references)
		//IL_097a: Unknown result type (might be due to invalid IL or missing references)
		//IL_097f: Unknown result type (might be due to invalid IL or missing references)
		//IL_098d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0992: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b5: Expected O, but got Unknown
		//IL_07d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1474: Unknown result type (might be due to invalid IL or missing references)
		//IL_147e: Expected O, but got Unknown
		//IL_149a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c89: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c8e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1841: Unknown result type (might be due to invalid IL or missing references)
		//IL_1846: Unknown result type (might be due to invalid IL or missing references)
		//IL_1854: Unknown result type (might be due to invalid IL or missing references)
		//IL_1859: Unknown result type (might be due to invalid IL or missing references)
		//IL_1867: Unknown result type (might be due to invalid IL or missing references)
		//IL_186c: Unknown result type (might be due to invalid IL or missing references)
		//IL_187a: Unknown result type (might be due to invalid IL or missing references)
		//IL_187f: Unknown result type (might be due to invalid IL or missing references)
		//IL_188c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1891: Unknown result type (might be due to invalid IL or missing references)
		//IL_1671: Unknown result type (might be due to invalid IL or missing references)
		//IL_167b: Expected O, but got Unknown
		//IL_1697: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f0: Expected O, but got Unknown
		//IL_0a0c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c08: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c16: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c41: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c53: Unknown result type (might be due to invalid IL or missing references)
		//IL_18ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_18b7: Expected O, but got Unknown
		//IL_18d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f85: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f8a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1acb: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ad0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ade: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ae3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1af1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1af6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b04: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b09: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b16: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b1b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ea3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ea8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c79: Expected O, but got Unknown
		//IL_0c95: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e53: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cfe: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d6c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d71: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b37: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b41: Expected O, but got Unknown
		//IL_1b5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f70: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f75: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dda: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ddf: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e39: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e3e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1147: Unknown result type (might be due to invalid IL or missing references)
		//IL_114c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1159: Unknown result type (might be due to invalid IL or missing references)
		//IL_115e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1068: Unknown result type (might be due to invalid IL or missing references)
		//IL_106d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2013: Unknown result type (might be due to invalid IL or missing references)
		//IL_2018: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ec3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ec8: Unknown result type (might be due to invalid IL or missing references)
		//IL_10c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_10cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_2025: Unknown result type (might be due to invalid IL or missing references)
		//IL_202a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f31: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f36: Unknown result type (might be due to invalid IL or missing references)
		//IL_117a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1184: Expected O, but got Unknown
		//IL_11a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1132: Unknown result type (might be due to invalid IL or missing references)
		//IL_1137: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f9f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fa4: Unknown result type (might be due to invalid IL or missing references)
		//IL_2046: Unknown result type (might be due to invalid IL or missing references)
		//IL_2050: Expected O, but got Unknown
		//IL_206c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ffe: Unknown result type (might be due to invalid IL or missing references)
		//IL_2003: Unknown result type (might be due to invalid IL or missing references)
		Stroke.RenderTarget = ((ChartObject)this).RenderTarget;
		ParallelStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		if (!((ChartObject)this).IsInHitTest && AreaBrush != null)
		{
			if (areaDeviceBrush.Brush == null)
			{
				Brush brush = areaBrush.Clone();
				brush.Opacity = (double)areaOpacity / 100.0;
				areaDeviceBrush.Brush = brush;
			}
			areaDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
		}
		else
		{
			areaDeviceBrush.RenderTarget = null;
			areaDeviceBrush.Brush = null;
		}
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = TrendStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = TrendEndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = ParallelStartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point4 = point3 + (point2 - point);
		Vector2 val2 = DxExtensions.ToVector2(point);
		Vector2 val3 = DxExtensions.ToVector2(point2);
		Vector2 val4 = DxExtensions.ToVector2(point3);
		Vector2 val5 = DxExtensions.ToVector2(point4);
		Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(chartControl, val, chartScale, TrendStartAnchor, TrendEndAnchor);
		Point extendedPoint2 = ((DrawingTool)this).GetExtendedPoint(chartControl, val, chartScale, TrendEndAnchor, TrendStartAnchor);
		Point point5 = ((ParallelStartAnchor.Time > DateTime.MinValue) ? (point3 + (extendedPoint - extendedPoint2)) : new Point(double.NaN, double.NaN));
		Point point6 = ((ParallelStartAnchor.Time > DateTime.MinValue) ? (point3 + (extendedPoint2 - extendedPoint)) : new Point(double.NaN, double.NaN));
		Brush val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : Stroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(val2, val3, val6, Stroke.Width, Stroke.StrokeStyle);
		if ((int)((DrawingTool)this).DrawingState == 0 && !isReadyForMovingSecondLeg)
		{
			return;
		}
		val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : ParallelStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(val4, val5, val6, ParallelStroke.Width, ParallelStroke.StrokeStyle);
		fillMainFig = (Vector2[])(object)new Vector2[4];
		fillMainFig[0] = DxExtensions.ToVector2(point3);
		fillMainFig[1] = DxExtensions.ToVector2(point4);
		fillMainFig[2] = DxExtensions.ToVector2(point2);
		fillMainFig[3] = DxExtensions.ToVector2(point);
		fillMainGeometry = new PathGeometry(Globals.D2DFactory);
		GeometrySink obj = fillMainGeometry.Open();
		((SimplifiedGeometrySink)obj).BeginFigure(new Vector2((float)point.X, (float)point.Y), (FigureBegin)0);
		((SimplifiedGeometrySink)obj).AddLines(fillMainFig);
		((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)1);
		((SimplifiedGeometrySink)obj).Close();
		DeviceBrush val7 = areaDeviceBrush;
		if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
		{
			((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillMainGeometry, areaDeviceBrush.BrushDX);
		}
		if (IsExtendedLinesLeft)
		{
			if (extendedPoint2.X > -1.0 || extendedPoint2.Y > -1.0)
			{
				((ChartObject)this).RenderTarget.DrawLine(val2, DxExtensions.ToVector2(extendedPoint2), Stroke.BrushDX, Stroke.Width, Stroke.StrokeStyle);
			}
			if (!double.IsNaN(point6.X) && !double.IsNaN(point6.Y))
			{
				((ChartObject)this).RenderTarget.DrawLine(val4, DxExtensions.ToVector2(point6), ParallelStroke.BrushDX, ParallelStroke.Width, ParallelStroke.StrokeStyle);
			}
			if ((point6.Y > 0.0 && point6.X < (double)((ChartObject)this).ChartPanel.X && point6.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && extendedPoint2.X > (double)((ChartObject)this).ChartPanel.X && extendedPoint2.Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (extendedPoint2.Y > 0.0 && extendedPoint2.X < (double)((ChartObject)this).ChartPanel.X && extendedPoint2.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && point6.X > (double)((ChartObject)this).ChartPanel.X && point6.Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point point7 = new Point(((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y);
				fillLeftFig = (Vector2[])(object)new Vector2[5];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				fillLeftFig[1] = DxExtensions.ToVector2(point6);
				fillLeftFig[2] = DxExtensions.ToVector2(point7);
				fillLeftFig[3] = DxExtensions.ToVector2(extendedPoint2);
				fillLeftFig[4] = DxExtensions.ToVector2(point);
				PathGeometry obj2 = fillLeftGeometry;
				if (obj2 != null)
				{
					((DisposeBase)obj2).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj3 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj3).BeginFigure(new Vector2((float)point.X, (float)point.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj3).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj3).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj3).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((point6.X > (double)((ChartObject)this).ChartPanel.X && point6.Y < (double)((ChartObject)this).ChartPanel.Y && extendedPoint2.X < (double)((ChartObject)this).ChartPanel.X && extendedPoint2.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (extendedPoint2.X > (double)((ChartObject)this).ChartPanel.X && extendedPoint2.Y < (double)((ChartObject)this).ChartPanel.Y && point6.X < (double)((ChartObject)this).ChartPanel.X && point6.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point point8 = new Point(((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.Y);
				fillLeftFig = (Vector2[])(object)new Vector2[5];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				fillLeftFig[1] = DxExtensions.ToVector2(point6);
				fillLeftFig[2] = DxExtensions.ToVector2(point8);
				fillLeftFig[3] = DxExtensions.ToVector2(extendedPoint2);
				fillLeftFig[4] = DxExtensions.ToVector2(point);
				PathGeometry obj4 = fillLeftGeometry;
				if (obj4 != null)
				{
					((DisposeBase)obj4).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj5 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj5).BeginFigure(new Vector2((float)point.X, (float)point.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj5).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj5).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj5).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((point6.X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && point6.Y < (double)((ChartObject)this).ChartPanel.Y && extendedPoint2.X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && extendedPoint2.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (extendedPoint2.X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && extendedPoint2.Y < (double)((ChartObject)this).ChartPanel.Y && point6.X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && point6.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point point9 = new Point(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.Y);
				fillLeftFig = (Vector2[])(object)new Vector2[5];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				fillLeftFig[1] = DxExtensions.ToVector2(point6);
				fillLeftFig[2] = DxExtensions.ToVector2(point9);
				fillLeftFig[3] = DxExtensions.ToVector2(extendedPoint2);
				fillLeftFig[4] = DxExtensions.ToVector2(point);
				PathGeometry obj6 = fillLeftGeometry;
				if (obj6 != null)
				{
					((DisposeBase)obj6).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj7 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj7).BeginFigure(new Vector2((float)point.X, (float)point.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj7).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj7).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj7).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((point6.Y > 0.0 && point6.X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && point6.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && extendedPoint2.X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && extendedPoint2.Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (extendedPoint2.Y > 0.0 && extendedPoint2.X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && extendedPoint2.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && point6.X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && point6.Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point point10 = new Point(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y);
				fillLeftFig = (Vector2[])(object)new Vector2[5];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				fillLeftFig[1] = DxExtensions.ToVector2(point6);
				fillLeftFig[2] = DxExtensions.ToVector2(point10);
				fillLeftFig[3] = DxExtensions.ToVector2(extendedPoint2);
				fillLeftFig[4] = DxExtensions.ToVector2(point);
				PathGeometry obj8 = fillLeftGeometry;
				if (obj8 != null)
				{
					((DisposeBase)obj8).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj9 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj9).BeginFigure(new Vector2((float)point.X, (float)point.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj9).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj9).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj9).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else
			{
				Point point11 = new Point(((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.Y);
				Point point12 = new Point(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.Y);
				Point point13 = new Point(((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y);
				Point point14 = new Point(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y);
				fillLeftFig = (Vector2[])(object)new Vector2[4];
				fillLeftFig[0] = DxExtensions.ToVector2(point3);
				if (point.Y < point2.Y && point.X < point2.X && point4.Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H) && point3.X < (double)((ChartObject)this).ChartPanel.X)
				{
					fillLeftFig[1] = DxExtensions.ToVector2(point11);
				}
				else if (point.Y < point2.Y && point.X > point2.X && point4.Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H) && point3.X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W))
				{
					fillLeftFig[1] = DxExtensions.ToVector2(point12);
				}
				else if (point.Y > point2.Y && point.X < point2.X && point4.Y < (double)((ChartObject)this).ChartPanel.Y && point3.X < (double)((ChartObject)this).ChartPanel.X)
				{
					fillLeftFig[1] = DxExtensions.ToVector2(point13);
				}
				else if (point.Y > point2.Y && point.X > point2.X && point4.Y < (double)((ChartObject)this).ChartPanel.Y && point3.X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W))
				{
					fillLeftFig[1] = DxExtensions.ToVector2(point14);
				}
				else
				{
					fillLeftFig[1] = DxExtensions.ToVector2(point6);
				}
				if (point.Y < point2.Y && point.X < point2.X && point2.Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H) && point.X < (double)((ChartObject)this).ChartPanel.X)
				{
					fillLeftFig[2] = DxExtensions.ToVector2(point11);
				}
				else if (point.Y < point2.Y && point.X > point2.X && point2.Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H) && point.X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W))
				{
					fillLeftFig[2] = DxExtensions.ToVector2(point12);
				}
				else if (point.Y > point2.Y && point.X < point2.X && point2.Y < 0.0 && point.X < (double)((ChartObject)this).ChartPanel.X)
				{
					fillLeftFig[2] = DxExtensions.ToVector2(point13);
				}
				else if (point.Y > point2.Y && point.X > point2.X && point2.Y < (double)((ChartObject)this).ChartPanel.Y && point.X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W))
				{
					fillLeftFig[2] = DxExtensions.ToVector2(point14);
				}
				else
				{
					fillLeftFig[2] = DxExtensions.ToVector2(extendedPoint2);
				}
				fillLeftFig[3] = DxExtensions.ToVector2(point);
				PathGeometry obj10 = fillLeftGeometry;
				if (obj10 != null)
				{
					((DisposeBase)obj10).Dispose();
				}
				fillLeftGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj11 = fillLeftGeometry.Open();
				((SimplifiedGeometrySink)obj11).BeginFigure(new Vector2((float)point.X, (float)point.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj11).AddLines(fillLeftFig);
				((SimplifiedGeometrySink)obj11).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj11).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillLeftGeometry, areaDeviceBrush.BrushDX);
				}
			}
		}
		if (IsExtendedLinesRight)
		{
			if (extendedPoint.X > -1.0 || extendedPoint.Y > -1.0)
			{
				((ChartObject)this).RenderTarget.DrawLine(val3, DxExtensions.ToVector2(extendedPoint), Stroke.BrushDX, Stroke.Width, Stroke.StrokeStyle);
			}
			if (point5.X > -1.0 || point5.Y > -1.0)
			{
				((ChartObject)this).RenderTarget.DrawLine(val5, DxExtensions.ToVector2(point5), ParallelStroke.BrushDX, ParallelStroke.Width, ParallelStroke.StrokeStyle);
			}
			if ((point5.Y > 0.0 && point5.X < (double)((ChartObject)this).ChartPanel.X && point5.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && extendedPoint.X > (double)((ChartObject)this).ChartPanel.X && extendedPoint.Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (extendedPoint.Y > 0.0 && extendedPoint.X < (double)((ChartObject)this).ChartPanel.X && extendedPoint.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && point5.X > (double)((ChartObject)this).ChartPanel.X && point5.Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point point15 = new Point(((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y);
				fillRightFig = (Vector2[])(object)new Vector2[5];
				fillRightFig[0] = DxExtensions.ToVector2(point4);
				fillRightFig[1] = DxExtensions.ToVector2(point5);
				fillRightFig[2] = DxExtensions.ToVector2(point15);
				fillRightFig[3] = DxExtensions.ToVector2(extendedPoint);
				fillRightFig[4] = DxExtensions.ToVector2(point2);
				PathGeometry obj12 = fillRightGeometry;
				if (obj12 != null)
				{
					((DisposeBase)obj12).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj13 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj13).BeginFigure(new Vector2((float)point2.X, (float)point2.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj13).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj13).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj13).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((point5.X > (double)((ChartObject)this).ChartPanel.X && point5.Y < (double)((ChartObject)this).ChartPanel.Y && extendedPoint.X < (double)((ChartObject)this).ChartPanel.X && extendedPoint.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (extendedPoint.X > (double)((ChartObject)this).ChartPanel.X && extendedPoint.Y < (double)((ChartObject)this).ChartPanel.Y && point5.X < (double)((ChartObject)this).ChartPanel.X && point5.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point point16 = new Point(((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.Y);
				fillRightFig = (Vector2[])(object)new Vector2[5];
				fillRightFig[0] = DxExtensions.ToVector2(point4);
				fillRightFig[1] = DxExtensions.ToVector2(point5);
				fillRightFig[2] = DxExtensions.ToVector2(point16);
				fillRightFig[3] = DxExtensions.ToVector2(extendedPoint);
				fillRightFig[4] = DxExtensions.ToVector2(point2);
				PathGeometry obj14 = fillRightGeometry;
				if (obj14 != null)
				{
					((DisposeBase)obj14).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj15 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj15).BeginFigure(new Vector2((float)point2.X, (float)point2.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj15).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj15).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj15).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((point5.X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && point5.Y < (double)((ChartObject)this).ChartPanel.Y && extendedPoint.X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && extendedPoint.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (extendedPoint.X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && extendedPoint.Y < (double)((ChartObject)this).ChartPanel.Y && point5.X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && point5.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point point17 = new Point(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.Y);
				fillRightFig = (Vector2[])(object)new Vector2[5];
				fillRightFig[0] = DxExtensions.ToVector2(point4);
				fillRightFig[1] = DxExtensions.ToVector2(point5);
				fillRightFig[2] = DxExtensions.ToVector2(point17);
				fillRightFig[3] = DxExtensions.ToVector2(extendedPoint);
				fillRightFig[4] = DxExtensions.ToVector2(point2);
				PathGeometry obj16 = fillRightGeometry;
				if (obj16 != null)
				{
					((DisposeBase)obj16).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj17 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj17).BeginFigure(new Vector2((float)point2.X, (float)point2.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj17).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj17).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj17).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else if ((point5.Y > 0.0 && point5.X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && point5.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && extendedPoint.X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && extendedPoint.Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)) || (extendedPoint.Y > 0.0 && extendedPoint.X > (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && extendedPoint.Y < (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y) && point5.X < (double)(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X) && point5.Y > (double)(((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y)))
			{
				Point point18 = new Point(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y);
				fillRightFig = (Vector2[])(object)new Vector2[5];
				fillRightFig[0] = DxExtensions.ToVector2(point4);
				fillRightFig[1] = DxExtensions.ToVector2(point5);
				fillRightFig[2] = DxExtensions.ToVector2(point18);
				fillRightFig[3] = DxExtensions.ToVector2(extendedPoint);
				fillRightFig[4] = DxExtensions.ToVector2(point2);
				PathGeometry obj18 = fillRightGeometry;
				if (obj18 != null)
				{
					((DisposeBase)obj18).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj19 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj19).BeginFigure(new Vector2((float)point2.X, (float)point2.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj19).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj19).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj19).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
			else
			{
				Point point19 = new Point(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.Y);
				Point point20 = new Point(((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.Y);
				Point point21 = new Point(((ChartObject)this).ChartPanel.W + ((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y);
				Point point22 = new Point(((ChartObject)this).ChartPanel.X, ((ChartObject)this).ChartPanel.H + ((ChartObject)this).ChartPanel.Y);
				fillRightFig = (Vector2[])(object)new Vector2[4];
				fillRightFig[0] = DxExtensions.ToVector2(point4);
				if (point.Y > point2.Y && point.X < point2.X && point4.X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W) && point3.Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
				{
					fillRightFig[1] = DxExtensions.ToVector2(point19);
				}
				else if (point.Y > point2.Y && point.X > point2.X && point4.X < (double)((ChartObject)this).ChartPanel.X && point3.Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
				{
					fillRightFig[1] = DxExtensions.ToVector2(point20);
				}
				else if (point.Y < point2.Y && point.X < point2.X && point4.X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W) && point3.Y < (double)((ChartObject)this).ChartPanel.Y)
				{
					fillRightFig[1] = DxExtensions.ToVector2(point21);
				}
				else if (point.Y < point2.Y && point.X > point2.X && point4.X < (double)((ChartObject)this).ChartPanel.X && point3.Y < (double)((ChartObject)this).ChartPanel.Y)
				{
					fillRightFig[1] = DxExtensions.ToVector2(point22);
				}
				else
				{
					fillRightFig[1] = DxExtensions.ToVector2(point5);
				}
				if (point.Y > point2.Y && point.X < point2.X && point2.X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W) && point.Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
				{
					fillRightFig[2] = DxExtensions.ToVector2(point19);
				}
				else if (point.Y > point2.Y && point.X > point2.X && point2.X < (double)((ChartObject)this).ChartPanel.X && point.Y > (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
				{
					fillRightFig[2] = DxExtensions.ToVector2(point20);
				}
				else if (point.Y < point2.Y && point.X < point2.X && point2.X > (double)(((ChartObject)this).ChartPanel.X + ((ChartObject)this).ChartPanel.W) && point.Y < (double)((ChartObject)this).ChartPanel.Y)
				{
					fillRightFig[2] = DxExtensions.ToVector2(point21);
				}
				else if (point.Y < point2.Y && point.X > point2.X && point2.X < (double)((ChartObject)this).ChartPanel.X && point.Y < (double)((ChartObject)this).ChartPanel.Y)
				{
					fillRightFig[2] = DxExtensions.ToVector2(point22);
				}
				else
				{
					fillRightFig[2] = DxExtensions.ToVector2(extendedPoint);
				}
				fillRightFig[3] = DxExtensions.ToVector2(point2);
				PathGeometry obj20 = fillRightGeometry;
				if (obj20 != null)
				{
					((DisposeBase)obj20).Dispose();
				}
				fillRightGeometry = new PathGeometry(Globals.D2DFactory);
				GeometrySink obj21 = fillRightGeometry.Open();
				((SimplifiedGeometrySink)obj21).BeginFigure(new Vector2((float)point2.X, (float)point2.Y), (FigureBegin)0);
				((SimplifiedGeometrySink)obj21).AddLines(fillRightFig);
				((SimplifiedGeometrySink)obj21).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)obj21).Close();
				val7 = areaDeviceBrush;
				if (val7 != null && val7.RenderTarget != null && val7.BrushDX != null)
				{
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)fillRightGeometry, areaDeviceBrush.BrushDX);
				}
			}
		}
		SetAllPriceLevelsRenderTarget();
		foreach (PriceLevel item in base.PriceLevels.Where((PriceLevel tl) => tl.IsVisible && tl.Stroke != null))
		{
			Vector vector = item.Value / 100.0 * (point3 - point);
			Vector vector2 = point2 - point;
			Point point23 = new Point(point.X + vector.X, point.Y + vector.Y);
			Point point24 = new Point(point23.X + vector2.X, point23.Y + vector2.Y);
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point23), DxExtensions.ToVector2(point24), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			Point extendedPoint3 = ((DrawingTool)this).GetExtendedPoint(point23, point24);
			Point extendedPoint4 = ((DrawingTool)this).GetExtendedPoint(point24, point23);
			if (IsExtendedLinesLeft && (extendedPoint4.X > -1.0 || extendedPoint4.Y > -1.0))
			{
				((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point23), DxExtensions.ToVector2(extendedPoint4), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			}
			if (IsExtendedLinesRight && (extendedPoint3.X > -1.0 || extendedPoint3.Y > -1.0))
			{
				((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point24), DxExtensions.ToVector2(extendedPoint3), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			}
		}
	}

	private void SetParallelLine(ChartControl chartControl, bool initialSet)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		if (initialSet)
		{
			if ((int)chartControl.BarSpacingType != 3)
			{
				ParallelStartAnchor.SlotIndex = TrendEndAnchor.SlotIndex;
				ParallelStartAnchor.Time = chartControl.GetTimeBySlotIndex(ParallelStartAnchor.SlotIndex);
			}
			else
			{
				ParallelStartAnchor.Time = TrendEndAnchor.Time;
			}
			ParallelStartAnchor.Price = TrendEndAnchor.Price;
			ParallelStartAnchor.StartAnchor = ((DrawingTool)this).InitialMouseDownAnchor;
		}
		else
		{
			double num = TrendStartAnchor.Price - ParallelStartAnchor.Price;
			ParallelStartAnchor.Price = TrendStartAnchor.Price - num;
		}
		ParallelStartAnchor.IsEditing = false;
	}
}
