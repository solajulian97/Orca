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
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class ShapeBase : DrawingTool
{
	protected enum ChartShapeType
	{
		Unset,
		Ellipse,
		Rectangle,
		Triangle
	}

	protected enum ResizeMode
	{
		None,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight,
		MoveAll
	}

	private int areaOpacity;

	private Brush areaBrush;

	private readonly DeviceBrush areaBrushDevice = new DeviceBrush();

	private const double cursorSensitivity = 15.0;

	private ChartAnchor editingAnchor;

	private ChartAnchor editingLeftAnchor;

	private ChartAnchor editingTopAnchor;

	private ChartAnchor editingBottomAnchor;

	private ChartAnchor editingRightAnchor;

	private ChartAnchor lastMouseMoveDataPoint;

	private ResizeMode resizeMode;

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
			areaBrush = value;
			if (areaBrush != null)
			{
				if (areaBrush.IsFrozen)
				{
					areaBrush = areaBrush.Clone();
				}
				areaBrush.Freeze();
			}
			areaBrushDevice.Brush = null;
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
			areaOpacity = Math.Max(0, Math.Min(100, value));
			areaBrushDevice.Brush = null;
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 3)]
	public Stroke OutlineStroke { get; set; }

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Display(Order = 3)]
	public ChartAnchor MiddleAnchor { get; set; }

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	[Browsable(false)]
	protected ChartShapeType ShapeType { get; set; }

	public override bool SupportsAlerts => true;

	public override IEnumerable<ChartAnchor> Anchors
	{
		get
		{
			if (ShapeType == ChartShapeType.Triangle)
			{
				return (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { StartAnchor, MiddleAnchor, EndAnchor };
			}
			return (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };
		}
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

	private PathGeometry CreateTriangleGeometry(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, double pixelAdjust)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		Point point = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Point point2 = MiddleAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Vector vector = new Vector(pixelAdjust, pixelAdjust);
		Vector2 val = DxExtensions.ToVector2(point + vector);
		Vector2 val2 = DxExtensions.ToVector2(point2 + vector);
		Vector2 val3 = DxExtensions.ToVector2(point3 + vector);
		PathGeometry val4 = new PathGeometry(Globals.D2DFactory);
		GeometrySink val5 = val4.Open();
		((SimplifiedGeometrySink)val5).BeginFigure(val, (FigureBegin)0);
		((SimplifiedGeometrySink)val5).AddLines((Vector2[])(object)new Vector2[6] { val, val2, val2, val3, val3, val });
		((SimplifiedGeometrySink)val5).EndFigure((FigureEnd)0);
		((SimplifiedGeometrySink)val5).Close();
		return val4;
	}

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		if (areaBrushDevice != null)
		{
			areaBrushDevice.RenderTarget = null;
		}
	}

	private Rect GetAnchorsRect(ChartControl chartControl, ChartScale chartScale)
	{
		if (StartAnchor == null || EndAnchor == null)
		{
			return default(Rect);
		}
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double x = Math.Min(point2.X, point.X);
		double y = Math.Min(point2.Y, point.Y);
		double width = Math.Abs(point2.X - point.X);
		double height = Math.Abs(point2.Y - point.Y);
		return new Rect(x, y, width, height);
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return Cursors.Pen;
		}
		if ((int)((DrawingTool)this).DrawingState == 3)
		{
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		}
		if ((int)((DrawingTool)this).DrawingState == 1 && ((DrawingTool)this).IsLocked)
		{
			return Cursors.No;
		}
		if (ShapeType == ChartShapeType.Triangle)
		{
			if (((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point) == null)
			{
				Point[] triangleAnchorPoints = GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: true);
				if ((triangleAnchorPoints.Last() - point).Length <= 15.0)
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
				for (int i = 0; i < 3; i++)
				{
					Point point2 = triangleAnchorPoints[(i != 2) ? (i + 1) : 0];
					Vector vector = triangleAnchorPoints[i] - point2;
					if (MathHelper.IsPointAlongVector(point, point2, vector, 10.0))
					{
						if (!((DrawingTool)this).IsLocked)
						{
							return Cursors.SizeAll;
						}
						return Cursors.Arrow;
					}
				}
				return null;
			}
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeNESW;
			}
			return null;
		}
		bool flag = ShapeType == ChartShapeType.Rectangle;
		switch ((resizeMode != ResizeMode.None) ? resizeMode : GetResizeModeForPoint(point, chartControl, chartScale, (int)((DrawingTool)this).DrawingState == 2))
		{
		case ResizeMode.TopLeft:
		case ResizeMode.BottomRight:
			return ((DrawingTool)this).IsLocked ? Cursors.Arrow : (flag ? Cursors.SizeNWSE : Cursors.SizeNS);
		case ResizeMode.TopRight:
		case ResizeMode.BottomLeft:
			return ((DrawingTool)this).IsLocked ? Cursors.Arrow : (flag ? Cursors.SizeNESW : Cursors.SizeWE);
		case ResizeMode.MoveAll:
			return ((DrawingTool)this).IsLocked ? Cursors.Arrow : Cursors.SizeAll;
		default:
			return null;
		}
	}

	private static Point? GetClosestPoint(IEnumerable<Point> inputPoints, Point desired, bool useSensitivity)
	{
		Point point = inputPoints.OrderBy((Point pt) => (pt - desired).Length).First();
		if (!useSensitivity || !((point - desired).Length > 15.0))
		{
			return point;
		}
		return null;
	}

	private Point[] GetEllipseAnchorPoints(ChartControl chartControl, ChartScale chartScale)
	{
		Rect anchorsRect = GetAnchorsRect(chartControl, chartScale);
		Point point = new Point(anchorsRect.Left + anchorsRect.Width / 2.0, anchorsRect.Top + anchorsRect.Height / 2.0);
		return new Point[5]
		{
			new Point(anchorsRect.TopLeft.X + anchorsRect.Width / 2.0, anchorsRect.Top),
			new Point(anchorsRect.Right, anchorsRect.TopRight.Y + anchorsRect.Height / 2.0),
			new Point(anchorsRect.Right - anchorsRect.Width / 2.0, anchorsRect.Bottom),
			new Point(anchorsRect.Left, anchorsRect.Top + anchorsRect.Height / 2.0),
			point
		};
	}

	private ResizeMode GetResizeModeForPoint(Point pt, ChartControl chartControl, ChartScale chartScale, bool useCursorSens)
	{
		switch (ShapeType)
		{
		case ChartShapeType.Ellipse:
		{
			Point[] ellipseAnchorPoints = GetEllipseAnchorPoints(chartControl, chartScale);
			Point point2 = ellipseAnchorPoints.Last();
			Point? closestPoint2 = GetClosestPoint(ellipseAnchorPoints, pt, useCursorSens);
			if (closestPoint2.HasValue)
			{
				int k;
				for (k = 0; k < ellipseAnchorPoints.Length && !(ellipseAnchorPoints[k] == closestPoint2.Value); k++)
				{
				}
				switch (k)
				{
				case 0:
					return ResizeMode.TopLeft;
				case 1:
					return ResizeMode.TopRight;
				case 2:
					return ResizeMode.BottomRight;
				case 3:
					return ResizeMode.BottomLeft;
				}
			}
			if ((point2 - pt).Length < 15.0)
			{
				return ResizeMode.MoveAll;
			}
			for (int l = 0; l < 4; l++)
			{
				Point point3 = ellipseAnchorPoints[(l != 3) ? (l + 1) : 0];
				Vector vector2 = ellipseAnchorPoints[l] - point3;
				if (MathHelper.IsPointAlongVector(pt, point3, vector2, 25.0))
				{
					return ResizeMode.MoveAll;
				}
			}
			break;
		}
		case ChartShapeType.Rectangle:
		{
			Rect anchorsRect = GetAnchorsRect(chartControl, chartScale);
			Point[] array = new Point[4] { anchorsRect.TopLeft, anchorsRect.TopRight, anchorsRect.BottomRight, anchorsRect.BottomLeft };
			Point? closestPoint = GetClosestPoint(array, pt, useCursorSens);
			if (closestPoint.HasValue)
			{
				int i;
				for (i = 0; i < array.Length && !(array[i] == closestPoint.Value); i++)
				{
				}
				return i switch
				{
					0 => ResizeMode.TopLeft, 
					1 => ResizeMode.TopRight, 
					2 => ResizeMode.BottomRight, 
					3 => ResizeMode.BottomLeft, 
					_ => ResizeMode.MoveAll, 
				};
			}
			for (int j = 0; j < 4; j++)
			{
				Point point = array[(j != 3) ? (j + 1) : 0];
				Vector vector = array[j] - point;
				if (MathHelper.IsPointAlongVector(pt, point, vector, 15.0))
				{
					return ResizeMode.MoveAll;
				}
			}
			break;
		}
		}
		return ResizeMode.None;
	}

	public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		switch (ShapeType)
		{
		case ChartShapeType.Ellipse:
			return GetEllipseAnchorPoints(chartControl, chartScale);
		case ChartShapeType.Rectangle:
		{
			Rect anchorsRect = GetAnchorsRect(chartControl, chartScale);
			return new Point[4] { anchorsRect.TopLeft, anchorsRect.TopRight, anchorsRect.BottomLeft, anchorsRect.BottomRight };
		}
		case ChartShapeType.Triangle:
			return GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: true);
		default:
			return new Point[0];
		}
	}

	private Point[] GetTriangleAnchorPoints(ChartControl chartControl, ChartScale chartScale, bool includeCentroid)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = MiddleAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		if (includeCentroid)
		{
			return new Point[4]
			{
				point2,
				point,
				point3,
				new Point((point2.X + point.X + point3.X) / 3.0, (point2.Y + point.Y + point3.Y) / 3.0)
			};
		}
		return new Point[3] { point2, point, point3 };
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		yield return new AlertConditionItem
		{
			Name = "Shape area",
			ShouldOnlyDisplayName = true
		};
	}

	public override IEnumerable<Condition> GetValidAlertConditions()
	{
		return (IEnumerable<Condition>)(object)new Condition[2]
		{
			(Condition)8,
			(Condition)9
		};
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		double minPrice = ((DrawingTool)this).Anchors.Min((ChartAnchor val2) => val2.Price);
		double maxPrice = ((DrawingTool)this).Anchors.Max((ChartAnchor val2) => val2.Price);
		DateTime minTime = ((DrawingTool)this).Anchors.Min((ChartAnchor val2) => val2.Time);
		DateTime maxTime = ((DrawingTool)this).Anchors.Max((ChartAnchor val2) => val2.Time);
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point centerPoint = point + (point2 - point) * 0.5;
		Predicate<ChartAlertValue> predicate;
		switch (ShapeType)
		{
		case ChartShapeType.Rectangle:
			predicate = delegate(ChartAlertValue v)
			{
				//IL_0045: Unknown result type (might be due to invalid IL or missing references)
				//IL_004b: Invalid comparison between Unknown and I4
				bool flag = v.Value >= minPrice && v.Value <= maxPrice && v.Time >= minTime && v.Time <= maxTime;
				return ((int)condition != 8) ? (!flag) : flag;
			};
			break;
		case ChartShapeType.Ellipse:
		{
			double a = Math.Abs(point2.X - point.X) / 2.0;
			double b = Math.Abs(point2.Y - point.Y) / 2.0;
			predicate = delegate(ChartAlertValue v)
			{
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0035: Invalid comparison between Unknown and I4
				bool flag = MathHelper.IsPointInsideEllipse(centerPoint, GetBarPoint(v), a, b);
				return ((int)condition != 8) ? (!flag) : flag;
			};
			break;
		}
		case ChartShapeType.Triangle:
		{
			Point[] trianglePoints = GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: false);
			predicate = delegate(ChartAlertValue v)
			{
				//IL_003c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0042: Invalid comparison between Unknown and I4
				bool flag = MathHelper.IsPointInsideTriangle(GetBarPoint(v), trianglePoints[0], trianglePoints[1], trianglePoints[2]);
				return ((int)condition != 8) ? (!flag) : flag;
			};
			break;
		}
		default:
			return false;
		}
		return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, predicate);
		Point GetBarPoint(ChartAlertValue v)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Invalid comparison between Unknown and I4
			if ((int)v.ValueType == 12)
			{
				return new Point(0.0, 0.0);
			}
			return new Point(chartControl.GetXByTime(v.Time), chartScale.GetYByValue(v.Value));
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		float num = float.MaxValue;
		float num2 = float.MinValue;
		ChartPanel chartPanel = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		foreach (Point item in ((DrawingTool)this).Anchors.Select((ChartAnchor a) => a.GetPoint(chartControl, chartPanel, chartScale, true)))
		{
			num = (float)Math.Min(num, item.X);
			num2 = (float)Math.Max(num2, item.X);
		}
		DateTime timeByX = chartControl.GetTimeByX((int)num);
		DateTime timeByX2 = chartControl.GetTimeByX((int)num2);
		if (timeByX <= lastTimeOnChart)
		{
			return timeByX2 >= firstTimeOnChart;
		}
		return false;
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Expected O, but got Unknown
		if (ShapeType == ChartShapeType.Unset)
		{
			return;
		}
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState != 2)
			{
				return;
			}
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			switch (ShapeType)
			{
			case ChartShapeType.Triangle:
			{
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
				if (editingAnchor != null)
				{
					editingAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
					break;
				}
				if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
					break;
				}
				Point[] triangleAnchorPoints = GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: true);
				if (!MathHelper.IsPointInsideTriangle(point, triangleAnchorPoints[0], triangleAnchorPoints[1], triangleAnchorPoints[2]))
				{
					((ChartObject)this).IsSelected = false;
				}
				break;
			}
			case ChartShapeType.Ellipse:
			case ChartShapeType.Rectangle:
			{
				Point point2 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
				Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
				editingLeftAnchor = ((point2.X <= point3.X) ? StartAnchor : EndAnchor);
				editingTopAnchor = ((point2.Y <= point3.Y) ? StartAnchor : EndAnchor);
				editingBottomAnchor = ((point2.Y <= point3.Y) ? EndAnchor : StartAnchor);
				editingRightAnchor = ((point2.X <= point3.X) ? EndAnchor : StartAnchor);
				Cursor cursor = ((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point);
				if (cursor == Cursors.SizeAll || cursor == Cursors.No)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
					break;
				}
				resizeMode = GetResizeModeForPoint(point, chartControl, chartScale, useCursorSens: true);
				if (resizeMode != ResizeMode.None)
				{
					((DrawingTool)this).DrawingState = (DrawingState)((resizeMode != ResizeMode.MoveAll) ? 1 : 3);
				}
				else if (!GetAnchorsRect(chartControl, chartScale).IntersectsWith(new Rect(point.X, point.Y, 1.0, 1.0)))
				{
					((ChartObject)this).IsSelected = false;
				}
				break;
			}
			}
			if (lastMouseMoveDataPoint == null)
			{
				lastMouseMoveDataPoint = new ChartAnchor();
			}
			dataPoint.CopyDataValues(lastMouseMoveDataPoint);
		}
		else
		{
			if (StartAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(StartAnchor);
				dataPoint.CopyDataValues(MiddleAnchor);
				dataPoint.CopyDataValues(EndAnchor);
				StartAnchor.IsEditing = false;
			}
			else if (ShapeType == ChartShapeType.Triangle && MiddleAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(MiddleAnchor);
				MiddleAnchor.IsEditing = false;
			}
			else if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
				EndAnchor.IsEditing = false;
			}
			if (!StartAnchor.IsEditing && !EndAnchor.IsEditing && (ShapeType != ChartShapeType.Triangle || !MiddleAnchor.IsEditing))
			{
				((DrawingTool)this).DrawingState = (DrawingState)2;
				((ChartObject)this).IsSelected = false;
			}
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Invalid comparison between Unknown and I4
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		if (ShapeType == ChartShapeType.Unset || (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0))
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (MiddleAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(MiddleAnchor);
			}
			if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1)
		{
			if (ShapeType == ChartShapeType.Triangle && editingAnchor != null)
			{
				dataPoint.CopyDataValues(editingAnchor);
				return;
			}
			if (lastMouseMoveDataPoint == null)
			{
				lastMouseMoveDataPoint = new ChartAnchor();
			}
			switch (resizeMode)
			{
			case ResizeMode.TopLeft:
				editingTopAnchor.Price = lastMouseMoveDataPoint.Price;
				if (ShapeType != ChartShapeType.Ellipse)
				{
					editingLeftAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
					editingLeftAnchor.Time = lastMouseMoveDataPoint.Time;
					dataPoint.CopyDataValues(lastMouseMoveDataPoint);
				}
				else
				{
					lastMouseMoveDataPoint.Price = dataPoint.Price;
				}
				break;
			case ResizeMode.BottomRight:
				editingBottomAnchor.Price = lastMouseMoveDataPoint.Price;
				if (ShapeType != ChartShapeType.Ellipse)
				{
					editingRightAnchor.Time = lastMouseMoveDataPoint.Time;
					editingRightAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
					dataPoint.CopyDataValues(lastMouseMoveDataPoint);
				}
				else
				{
					lastMouseMoveDataPoint.Price = dataPoint.Price;
				}
				break;
			case ResizeMode.TopRight:
				editingRightAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
				editingRightAnchor.Time = lastMouseMoveDataPoint.Time;
				if (ShapeType != ChartShapeType.Ellipse)
				{
					editingTopAnchor.Price = lastMouseMoveDataPoint.Price;
					dataPoint.CopyDataValues(lastMouseMoveDataPoint);
				}
				else
				{
					lastMouseMoveDataPoint.Time = dataPoint.Time;
					lastMouseMoveDataPoint.SlotIndex = dataPoint.SlotIndex;
				}
				break;
			case ResizeMode.BottomLeft:
				editingLeftAnchor.Time = lastMouseMoveDataPoint.Time;
				editingLeftAnchor.SlotIndex = lastMouseMoveDataPoint.SlotIndex;
				if (ShapeType != ChartShapeType.Ellipse)
				{
					editingBottomAnchor.Price = lastMouseMoveDataPoint.Price;
					dataPoint.CopyDataValues(lastMouseMoveDataPoint);
				}
				else
				{
					lastMouseMoveDataPoint.Time = dataPoint.Time;
					lastMouseMoveDataPoint.SlotIndex = dataPoint.SlotIndex;
				}
				break;
			}
		}
		else
		{
			if ((int)((DrawingTool)this).DrawingState != 3)
			{
				return;
			}
			foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
			{
				anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			lastMouseMoveDataPoint = null;
			((DrawingTool)this).DrawingState = (DrawingState)2;
			editingAnchor = null;
			editingLeftAnchor = null;
			editingTopAnchor = null;
			editingRightAnchor = null;
			editingBottomAnchor = null;
			resizeMode = ResizeMode.None;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		if (ShapeType == ChartShapeType.Unset)
		{
			return;
		}
		Stroke outlineStroke = OutlineStroke;
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = point2.X - point.X;
		double num2 = point2.Y - point.Y;
		Vector2 val2 = DxExtensions.ToVector2(point + (point2 - point) / 2.0);
		if (!((ChartObject)this).IsInHitTest && AreaBrush != null)
		{
			if (areaBrushDevice.Brush == null)
			{
				Brush brush = areaBrush.Clone();
				brush.Opacity = (double)areaOpacity / 100.0;
				areaBrushDevice.Brush = brush;
			}
			areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		}
		else
		{
			areaBrushDevice.RenderTarget = null;
			areaBrushDevice.Brush = null;
		}
		double num3 = ((outlineStroke.Width % 2f == 0f) ? 0.5 : 0.0);
		switch (ShapeType)
		{
		case ChartShapeType.Ellipse:
		{
			Ellipse val7 = default(Ellipse);
			((Ellipse)(ref val7))._002Ector(val2, (float)(num / 2.0 + num3), (float)(num2 / 2.0 + num3));
			if (!((ChartObject)this).IsInHitTest && areaBrushDevice.BrushDX != null)
			{
				((ChartObject)this).RenderTarget.FillEllipse(val7, areaBrushDevice.BrushDX);
			}
			else
			{
				((ChartObject)this).RenderTarget.FillRectangle(new RectangleF(val2.X - 5f, val2.Y - 5f, 15f, 15f), chartControl.SelectionBrush);
			}
			Brush val8 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawEllipse(val7, val8, outlineStroke.Width, outlineStroke.StrokeStyle);
			break;
		}
		case ChartShapeType.Rectangle:
		{
			RectangleF val5 = default(RectangleF);
			((RectangleF)(ref val5))._002Ector((float)(point.X + num3), (float)(point.Y + num3), (float)num, (float)num2);
			if (!((ChartObject)this).IsInHitTest && areaBrushDevice.BrushDX != null)
			{
				((ChartObject)this).RenderTarget.FillRectangle(val5, areaBrushDevice.BrushDX);
			}
			Brush val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawRectangle(val5, val6, outlineStroke.Width, outlineStroke.StrokeStyle);
			break;
		}
		case ChartShapeType.Triangle:
		{
			PathGeometry val3 = CreateTriangleGeometry(chartControl, val, chartScale, num3);
			if (!((ChartObject)this).IsInHitTest && areaBrushDevice.BrushDX != null)
			{
				((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val3, areaBrushDevice.BrushDX);
			}
			else
			{
				Point point3 = GetTriangleAnchorPoints(chartControl, chartScale, includeCentroid: true).Last();
				((ChartObject)this).RenderTarget.FillRectangle(new RectangleF((float)point3.X - 5f, (float)point3.Y - 5f, 15f, 15f), chartControl.SelectionBrush);
			}
			Brush val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)val3, val4, outlineStroke.Width, outlineStroke.StrokeStyle);
			((DisposeBase)val3).Dispose();
			break;
		}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Invalid comparison between Unknown and I4
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			StartAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorStart,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			MiddleAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorMiddle,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			EndAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			((DrawingTool)this).DrawingState = (DrawingState)0;
			AreaBrush = Brushes.CornflowerBlue;
			AreaOpacity = 40;
			OutlineStroke = new Stroke((Brush)Brushes.CornflowerBlue, 2f);
			ShapeType = ChartShapeType.Unset;
			MiddleAnchor.IsBrowsable = false;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}
}
