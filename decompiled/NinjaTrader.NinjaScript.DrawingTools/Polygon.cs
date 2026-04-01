using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
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
/// Represents an interface that exposes information regarding a Polygon IDrawingTool.
/// </summary>
public class Polygon : DrawingTool
{
	private Brush areaBrush;

	private readonly DeviceBrush areaBrushDevice = new DeviceBrush();

	private int areaOpacity;

	private const double cursorSensitivity = 15.0;

	private ChartAnchor editingAnchor;

	private DateTime lastClick;

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 0)]
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
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 1)]
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

	public override object Icon => Icons.DrawPolygon;

	[Browsable(false)]
	[SkipOnCopyTo(true)]
	[ExcludeFromTemplate]
	public List<ChartAnchor> ChartAnchors { get; set; }

	[Display(Order = 0)]
	[SkipOnCopyTo(true)]
	[ExcludeFromTemplate]
	public ChartAnchor StartAnchor
	{
		get
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Expected O, but got Unknown
			if (ChartAnchors == null || ChartAnchors.Count == 0)
			{
				return new ChartAnchor
				{
					DisplayName = Resource.NinjaScriptDrawingToolAnchorStart,
					IsEditing = true,
					DrawingTool = (IDrawingTool)(object)this
				};
			}
			return ChartAnchors[0];
		}
		set
		{
			if (ChartAnchors != null)
			{
				if (ChartAnchors.Count == 0)
				{
					ChartAnchors.Add(value);
				}
				else
				{
					ChartAnchors[0] = value;
				}
			}
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 2)]
	public Stroke OutlineStroke { get; set; }

	public override IEnumerable<ChartAnchor> Anchors
	{
		get
		{
			if (ChartAnchors == null || ChartAnchors.Count == 0)
			{
				return (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { StartAnchor };
			}
			return ChartAnchors.ToArray();
		}
	}

	public override bool SupportsAlerts => true;

	public override void CopyTo(NinjaScript ninjaScript)
	{
		((DrawingTool)this).CopyTo(ninjaScript);
		if (ninjaScript is PathTool pathTool)
		{
			if (ChartAnchors == null)
			{
				return;
			}
			pathTool.ChartAnchors.Clear();
			{
				foreach (ChartAnchor chartAnchor in ChartAnchors)
				{
					List<ChartAnchor> chartAnchors = pathTool.ChartAnchors;
					object obj = chartAnchor.Clone();
					chartAnchors.Add((ChartAnchor)((obj is ChartAnchor) ? obj : null));
				}
				return;
			}
		}
		PropertyInfo property = ((object)ninjaScript).GetType().GetProperty("ChartAnchors");
		if (property == null || !(property.GetValue(ninjaScript) is IList list))
		{
			return;
		}
		list.Clear();
		foreach (ChartAnchor chartAnchor2 in ChartAnchors)
		{
			try
			{
				object obj2 = chartAnchor2.Clone();
				ChartAnchor val = (ChartAnchor)((obj2 is ChartAnchor) ? obj2 : null);
				if (val != null)
				{
					list.Add(val);
				}
			}
			catch
			{
			}
		}
	}

	private PathGeometry CreatePolygonGeometry(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, double pixelAdjust)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		List<Vector2> list = new List<Vector2>();
		Vector vector = new Vector(pixelAdjust, pixelAdjust);
		for (int i = 0; i < ChartAnchors.Count; i++)
		{
			Point point = ChartAnchors[i].GetPoint(chartControl, chartPanel, chartScale, true);
			list.Add(DxExtensions.ToVector2(point + vector));
			if (i + 1 < ChartAnchors.Count)
			{
				Point point2 = ChartAnchors[i + 1].GetPoint(chartControl, chartPanel, chartScale, true);
				list.Add(DxExtensions.ToVector2(point2 + vector));
			}
			else if ((int)((DrawingTool)this).DrawingState != 0)
			{
				list.Add(list[0]);
			}
		}
		PathGeometry val = new PathGeometry(Globals.D2DFactory);
		GeometrySink obj = val.Open();
		((SimplifiedGeometrySink)obj).BeginFigure(list[0], (FigureBegin)0);
		((SimplifiedGeometrySink)obj).AddLines(list.ToArray());
		((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)0);
		((SimplifiedGeometrySink)obj).Close();
		return val;
	}

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		if (areaBrushDevice != null)
		{
			areaBrushDevice.RenderTarget = null;
		}
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		yield return new AlertConditionItem
		{
			Name = "Polygon",
			ShouldOnlyDisplayName = true
		};
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Invalid comparison between Unknown and I4
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (ChartAnchors.Count > 2)
			{
				Point point2 = ChartAnchors[0].GetPoint(chartControl, chartPanel, chartScale, true);
				if (point.X >= point2.X - 15.0 && point.X <= point2.X + 15.0 && point.Y >= point2.Y - 15.0 && point.Y <= point2.Y + 15.0)
				{
					return Cursors.Cross;
				}
			}
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
		if (((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point) == null)
		{
			Point[] polygonAnchorPoints = GetPolygonAnchorPoints(chartControl, chartScale, includeCentroid: true);
			if (polygonAnchorPoints.Length != 0 && (polygonAnchorPoints.Last() - point).Length <= 15.0)
			{
				if (!((DrawingTool)this).IsLocked)
				{
					return Cursors.SizeAll;
				}
				return Cursors.Arrow;
			}
			for (int i = 0; i < ChartAnchors.Count; i++)
			{
				Point point3 = ChartAnchors[i].GetPoint(chartControl, chartPanel, chartScale, true);
				if (i + 1 < ChartAnchors.Count)
				{
					Point point4 = ChartAnchors[i + 1].GetPoint(chartControl, chartPanel, chartScale, true);
					if (MathHelper.IsPointAlongVector(point, point3, point4 - point3, 15.0))
					{
						if (!((DrawingTool)this).IsLocked)
						{
							return Cursors.SizeAll;
						}
						return Cursors.Arrow;
					}
					continue;
				}
				Point point5 = ChartAnchors[0].GetPoint(chartControl, chartPanel, chartScale, true);
				if (MathHelper.IsPointAlongVector(point, point5, point3 - point5, 15.0))
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

	private Point[] GetPolygonAnchorPoints(ChartControl chartControl, ChartScale chartScale, bool includeCentroid)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		if (includeCentroid)
		{
			return GetCentroid(chartControl, val, chartScale);
		}
		Point[] array = new Point[ChartAnchors.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ChartAnchors[i].GetPoint(chartControl, val, chartScale, true);
		}
		return array;
	}

	private Point[] GetCentroid(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		Point point = default(Point);
		int num4 = 0;
		int index = ChartAnchors.Count - 1;
		while (num4 < ChartAnchors.Count)
		{
			Point point2 = ChartAnchors[num4].GetPoint(chartControl, chartPanel, chartScale, true);
			Point point3 = ChartAnchors[index].GetPoint(chartControl, chartPanel, chartScale, true);
			double num5 = point2.X * point3.Y - point3.X * point2.Y;
			num += num5;
			num2 += (point2.X + point3.X) * num5;
			num3 += (point2.Y + point3.Y) * num5;
			index = num4++;
		}
		if (Math.Abs(num) < 1.0000000116860974E-07)
		{
			return GetPolygonAnchorPoints(chartControl, chartScale, includeCentroid: false);
		}
		num *= 3.0;
		point.X = num2 / num;
		point.Y = num3 / num;
		if (!IsPointInsidePolygon(chartControl, chartPanel, chartScale, point))
		{
			return GetPolygonAnchorPoints(chartControl, chartScale, includeCentroid: false);
		}
		Point[] array = new Point[ChartAnchors.Count + 1];
		for (int i = 0; i < array.Length; i++)
		{
			if (i < ChartAnchors.Count)
			{
				array[i] = ChartAnchors[i].GetPoint(chartControl, chartPanel, chartScale, true);
			}
		}
		array[array.Length - 1] = point;
		return array;
	}

	public override IEnumerable<Condition> GetValidAlertConditions()
	{
		return (IEnumerable<Condition>)(object)new Condition[2]
		{
			(Condition)8,
			(Condition)9
		};
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		return GetPolygonAnchorPoints(chartControl, chartScale, (int)((DrawingTool)this).DrawingState > 0);
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel chartPanel = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
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
		bool Predicate(ChartAlertValue v)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Invalid comparison between Unknown and I4
			bool flag = IsPointInsidePolygon(chartControl, chartPanel, chartScale, GetBarPoint(v));
			if ((int)condition != 8)
			{
				return !flag;
			}
			return flag;
		}
	}

	private bool IsPointInsidePolygon(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point p)
	{
		bool flag = false;
		int num = 0;
		int index = ChartAnchors.Count - 1;
		while (num < ChartAnchors.Count)
		{
			Point point = ChartAnchors[num].GetPoint(chartControl, chartPanel, chartScale, true);
			Point point2 = ChartAnchors[index].GetPoint(chartControl, chartPanel, chartScale, true);
			if (point.Y > p.Y != point2.Y > p.Y && p.X < (point2.X - point.X) * (p.Y - point.Y) / (point2.Y - point.Y) + point.X)
			{
				flag = !flag;
			}
			index = num++;
		}
		return flag;
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
		foreach (Point item in ChartAnchors.Select((ChartAnchor a) => a.GetPoint(chartControl, chartPanel, chartScale, true)))
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

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible || !ChartAnchors.Any((ChartAnchor a) => !a.IsEditing))
		{
			return;
		}
		foreach (ChartAnchor chartAnchor in ChartAnchors)
		{
			((ChartObject)this).MinValue = Math.Min(((ChartObject)this).MinValue, chartAnchor.Price);
			((ChartObject)this).MaxValue = Math.Max(((ChartObject)this).MaxValue, chartAnchor.Price);
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Expected O, but got Unknown
		Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState == 2)
			{
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
				if (editingAnchor != null)
				{
					editingAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
				else if (!IsPointInsidePolygon(chartControl, chartPanel, chartScale, point))
				{
					((ChartObject)this).IsSelected = false;
				}
			}
			return;
		}
		if (ChartAnchors.Count == 0)
		{
			ChartAnchors.Add(new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchor,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			});
		}
		foreach (ChartAnchor chartAnchor in ChartAnchors)
		{
			if (chartAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(chartAnchor);
				chartAnchor.IsEditing = false;
			}
		}
		if (ChartAnchors.Count > 2 && (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.Cross || DateTime.Now.Subtract(lastClick).TotalMilliseconds <= 200.0))
		{
			ChartAnchors.Remove(ChartAnchors[ChartAnchors.Count - 1]);
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
		else
		{
			ChartAnchor val = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchor,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			dataPoint.CopyDataValues(val);
			ChartAnchors.Add(val);
		}
		lastClick = DateTime.Now;
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected I4, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		switch ((int)drawingState)
		{
		case 0:
		{
			foreach (ChartAnchor chartAnchor in ChartAnchors)
			{
				if (chartAnchor.IsEditing)
				{
					dataPoint.CopyDataValues(chartAnchor);
				}
			}
			break;
		}
		case 1:
			if (editingAnchor != null)
			{
				dataPoint.CopyDataValues(editingAnchor);
			}
			break;
		case 3:
		{
			foreach (ChartAnchor chartAnchor2 in ChartAnchors)
			{
				chartAnchor2.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			break;
		}
		case 2:
			break;
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = false;
				editingAnchor = null;
			}
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		Stroke outlineStroke = OutlineStroke;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		ChartPanel chartPanel = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
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
		double pixelAdjust = ((outlineStroke.Width % 2f == 0f) ? 0.5 : 0.0);
		PathGeometry val = CreatePolygonGeometry(chartControl, chartPanel, chartScale, pixelAdjust);
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			if (!((ChartObject)this).IsInHitTest && areaBrushDevice.BrushDX != null)
			{
				((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val, areaBrushDevice.BrushDX);
			}
			else
			{
				Point[] polygonAnchorPoints = GetPolygonAnchorPoints(chartControl, chartScale, includeCentroid: true);
				Point point = ((polygonAnchorPoints.Length != 0) ? polygonAnchorPoints.Last() : default(Point));
				((ChartObject)this).RenderTarget.FillRectangle(new RectangleF((float)point.X - 5f, (float)point.Y - 5f, 15f, 15f), chartControl.SelectionBrush);
			}
		}
		Brush val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)val, val2, outlineStroke.Width, outlineStroke.StrokeStyle);
		((DisposeBase)val).Dispose();
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Invalid comparison between Unknown and I4
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			AreaBrush = Brushes.CornflowerBlue;
			AreaOpacity = 40;
			((DrawingTool)this).DrawingState = (DrawingState)0;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolPolygon;
			OutlineStroke = new Stroke((Brush)Brushes.CornflowerBlue, (DashStyleHelper)0, 2f, 100);
			ChartAnchors = new List<ChartAnchor>();
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}
}
