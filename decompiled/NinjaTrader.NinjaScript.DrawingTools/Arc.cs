using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
/// Represents an interface that exposes information regarding an Arc IDrawingTool.
/// </summary>
public class Arc : Line
{
	private PathGeometry arcGeometry;

	private Brush areaBrush;

	private readonly DeviceBrush areaBrushDevice = new DeviceBrush();

	private int areaOpacity;

	private Point cachedStartPoint;

	private Point cachedEndPoint;

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptGeneral", Name = "NinjaScriptDrawingToolArc", Order = 99)]
	public Stroke ArcStroke { get; set; }

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

	public override object Icon => Icons.DrawArc;

	public override bool SupportsAlerts => true;

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		if (areaBrushDevice != null)
		{
			areaBrushDevice.RenderTarget = null;
		}
		PathGeometry obj = arcGeometry;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		yield return new AlertConditionItem
		{
			Name = ((NinjaScript)this).Name,
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
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Invalid comparison between Unknown and I4
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		double num = chartControl.GetXByTime(values[0].Time);
		double y = chartScale.GetYByValue(values[0].Value);
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = new Point(num, y);
		if (arcGeometry == null || ((DisposeBase)arcGeometry).IsDisposed)
		{
			UpdateArcGeometry(chartControl, val, chartScale);
		}
		if (num < Math.Min(point.X, point2.X))
		{
			return false;
		}
		if ((int)MathHelper.GetPointLineLocation(point, point2, point3) != 1)
		{
			return false;
		}
		return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)ArcPredicate);
		bool ArcPredicate(ChartAlertValue v)
		{
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Invalid comparison between Unknown and I4
			if (v.Time == Globals.MinDate || v.Time == Globals.MaxDate)
			{
				return false;
			}
			if (v.Value <= double.MinValue)
			{
				return false;
			}
			double x = chartControl.GetXByTime(v.Time);
			double y2 = chartScale.GetYByValue(v.Value);
			Point point4 = new Point(x, y2);
			bool flag = ((Geometry)arcGeometry).FillContainsPoint(DxExtensions.ToVector2(point4));
			if ((int)condition != 8)
			{
				return !flag;
			}
			return flag;
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		Cursor cursor = base.GetCursor(chartControl, chartPanel, chartScale, point);
		if (cursor != null)
		{
			return cursor;
		}
		if (arcGeometry == null || ((DisposeBase)arcGeometry).IsDisposed)
		{
			UpdateArcGeometry(chartControl, chartPanel, chartScale);
		}
		PathGeometry obj = arcGeometry;
		if (obj == null || !((Geometry)obj).StrokeContainsPoint(DxExtensions.ToVector2(point), 15f))
		{
			return null;
		}
		if (!((DrawingTool)this).IsLocked || (int)((DrawingTool)this).DrawingState == 2)
		{
			return Cursors.SizeAll;
		}
		return Cursors.No;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel chartPanel = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		UpdateArcGeometry(chartControl, chartPanel, chartScale);
		base.OnRender(chartControl, chartScale);
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		if (AreaBrush != null && !((ChartObject)this).IsInHitTest)
		{
			areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
			if (areaBrushDevice.Brush == null)
			{
				Brush brush = AreaBrush.Clone();
				brush.Opacity = (double)areaOpacity / 100.0;
				areaBrushDevice.Brush = brush;
			}
			((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)arcGeometry, areaBrushDevice.BrushDX);
		}
		ArcStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		Brush val = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : ArcStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)arcGeometry, val, ArcStroke.Width, ArcStroke.StrokeStyle);
	}

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Invalid comparison between Unknown and I4
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		base.OnStateChange();
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolArc;
			base.LineType = ChartLineType.Line;
			base.Stroke = new Stroke((Brush)Brushes.CornflowerBlue, (DashStyleHelper)0, 2f, 50);
			ArcStroke = new Stroke((Brush)Brushes.CornflowerBlue, (DashStyleHelper)0, 2f);
			AreaBrush = Brushes.CornflowerBlue;
			AreaOpacity = 40;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			PathGeometry obj = arcGeometry;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			arcGeometry = null;
			if (areaBrushDevice != null)
			{
				areaBrushDevice.RenderTarget = null;
			}
		}
	}

	private void UpdateArcGeometry(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Expected O, but got Unknown
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		Point point = base.StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		if (arcGeometry == null || !(point == cachedStartPoint) || !(point2 == cachedEndPoint))
		{
			cachedEndPoint = point2;
			cachedStartPoint = point;
			PathGeometry val = arcGeometry;
			if (val != null && !((DisposeBase)val).IsDisposed)
			{
				((DisposeBase)arcGeometry).Dispose();
			}
			Vector vector = point2 - point;
			float num = Math.Abs((float)vector.X);
			float num2 = Math.Abs((float)vector.Y);
			ArcSegment val2 = new ArcSegment
			{
				ArcSize = (ArcSize)0,
				Point = new Vector2((float)point2.X, (float)point2.Y),
				SweepDirection = (SweepDirection)0,
				Size = new Size2F(num, num2)
			};
			arcGeometry = new PathGeometry(Globals.D2DFactory);
			GeometrySink obj = arcGeometry.Open();
			((SimplifiedGeometrySink)obj).BeginFigure(new Vector2((float)point.X, (float)point.Y), (FigureBegin)0);
			obj.AddArc(val2);
			((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)0);
			((SimplifiedGeometrySink)obj).Close();
		}
	}
}
