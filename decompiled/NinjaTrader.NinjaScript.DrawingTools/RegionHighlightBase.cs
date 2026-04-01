using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

[CLSCompliant(false)]
public abstract class RegionHighlightBase : DrawingTool
{
	private int areaOpacity;

	private Brush areaBrush;

	private readonly DeviceBrush areaBrushDevice = new DeviceBrush();

	private const double cursorSensitivity = 15.0;

	private ChartAnchor editingAnchor;

	private bool hasSetZOrder;

	public override bool SupportsAlerts => true;

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRiskRewardAnchorLineStroke", GroupName = "NinjaScriptGeneral", Order = 5)]
	public Stroke AnchorLineStroke { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 3)]
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
				areaBrush.Opacity = (double)areaOpacity / 100.0;
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
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 4)]
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

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	internal RegionHighlightMode Mode { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 6)]
	public Stroke OutlineStroke { get; set; }

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

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
			Name = Resource.NinjaScriptDrawingToolRegion,
			ShouldOnlyDisplayName = true
		};
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected I4, but got Unknown
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		switch ((int)drawingState)
		{
		case 0:
			return Cursors.Pen;
		case 1:
			if (!((DrawingTool)this).IsLocked)
			{
				if (Mode != RegionHighlightMode.Time)
				{
					return Cursors.SizeNS;
				}
				return Cursors.SizeWE;
			}
			return Cursors.No;
		case 3:
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		default:
		{
			Point point2 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			if (((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point) != null)
			{
				if (((DrawingTool)this).IsLocked)
				{
					return Cursors.Arrow;
				}
				if (Mode != RegionHighlightMode.Time)
				{
					return Cursors.SizeNS;
				}
				return Cursors.SizeWE;
			}
			Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Vector vector = point3 - point2;
			if (MathHelper.IsPointAlongVector(point, point2, vector, 15.0))
			{
				if (!((DrawingTool)this).IsLocked)
				{
					return Cursors.SizeAll;
				}
				return Cursors.Arrow;
			}
			Point[] array = new Point[2] { point2, point3 };
			for (int i = 0; i < array.Length; i++)
			{
				Point point4 = array[i];
				if (Mode == RegionHighlightMode.Price && Math.Abs(point4.Y - point.Y) <= 15.0)
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
				if (Mode == RegionHighlightMode.Time && Math.Abs(point4.X - point.X) <= 15.0)
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
		}
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double middleX = (double)val.X + (double)val.W / 2.0;
		double middleY = (double)val.Y + (double)val.H / 2.0;
		Point point3 = new Point((point.X + point2.X) / 2.0, (point.Y + point2.Y) / 2.0);
		return new Point[3] { point, point3, point2 }.Select((Point p) => (Mode != RegionHighlightMode.Time) ? new Point(middleX, p.Y) : new Point(p.X, middleY)).ToArray();
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
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Invalid comparison between Unknown and I4
		double minPrice = ((DrawingTool)this).Anchors.Min((ChartAnchor a) => a.Price);
		double maxPrice = ((DrawingTool)this).Anchors.Max((ChartAnchor a) => a.Price);
		DateTime minTime = ((DrawingTool)this).Anchors.Min((ChartAnchor a) => a.Time);
		DateTime maxTime = ((DrawingTool)this).Anchors.Max((ChartAnchor a) => a.Time);
		if (Mode == RegionHighlightMode.Time)
		{
			DateTime time = values[0].Time;
			if ((int)condition != 8)
			{
				if (time > minTime)
				{
					return time < maxTime;
				}
				return false;
			}
			if (time > minTime)
			{
				return time <= maxTime;
			}
			return false;
		}
		return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
		bool Predicate(ChartAlertValue v)
		{
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Invalid comparison between Unknown and I4
			bool flag = ((Mode != RegionHighlightMode.Time) ? (v.Value >= minPrice && v.Value <= maxPrice) : (v.Time >= minTime && v.Time <= maxTime));
			if ((int)condition != 8)
			{
				return !flag;
			}
			return flag;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		if (Mode == RegionHighlightMode.Time)
		{
			if (((DrawingTool)this).Anchors.Any((ChartAnchor a) => a.Time >= firstTimeOnChart && a.Time <= lastTimeOnChart))
			{
				return true;
			}
			if (StartAnchor.Time <= firstTimeOnChart && EndAnchor.Time >= lastTimeOnChart)
			{
				return true;
			}
			if (EndAnchor.Time <= firstTimeOnChart && StartAnchor.Time >= lastTimeOnChart)
			{
				return true;
			}
			return false;
		}
		if (((DrawingTool)this).Anchors.Any((ChartAnchor a) => a.Price <= chartScale.MaxValue && a.Price >= chartScale.MinValue))
		{
			return true;
		}
		if (!(StartAnchor.Price <= chartScale.MinValue) || !(EndAnchor.Price >= chartScale.MaxValue))
		{
			if (EndAnchor.Price <= chartScale.MinValue)
			{
				return StartAnchor.Price >= chartScale.MaxValue;
			}
			return false;
		}
		return true;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible)
		{
			return;
		}
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			((ChartObject)this).MinValue = Math.Min(anchor.Price, ((ChartObject)this).MinValue);
			((ChartObject)this).MaxValue = Math.Max(anchor.Price, ((ChartObject)this).MaxValue);
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState == 2)
			{
				Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
				if (editingAnchor != null)
				{
					editingAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeAll)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeWE || ((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNS)
				{
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.Arrow)
				{
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == null)
				{
					((ChartObject)this).IsSelected = false;
				}
			}
			return;
		}
		if (Mode == RegionHighlightMode.Price)
		{
			dataPoint.Time = chartControl.FirstTimePainted.AddSeconds((chartControl.LastTimePainted - chartControl.FirstTimePainted).TotalSeconds / 2.0);
		}
		else
		{
			dataPoint.Price = chartScale.MinValue + chartScale.MaxMinusMin / 2.0;
		}
		if (StartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(StartAnchor);
			StartAnchor.IsEditing = false;
			dataPoint.CopyDataValues(EndAnchor);
		}
		else if (EndAnchor.IsEditing)
		{
			if (Mode == RegionHighlightMode.Price)
			{
				dataPoint.Time = StartAnchor.Time;
				dataPoint.SlotIndex = StartAnchor.SlotIndex;
			}
			else
			{
				dataPoint.Price = StartAnchor.Price;
			}
			dataPoint.CopyDataValues(EndAnchor);
			EndAnchor.IsEditing = false;
		}
		if (!StartAnchor.IsEditing && !EndAnchor.IsEditing)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Invalid comparison between Unknown and I4
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0 && EndAnchor.IsEditing)
		{
			if (Mode == RegionHighlightMode.Price)
			{
				dataPoint.Time = chartControl.FirstTimePainted.AddSeconds((chartControl.LastTimePainted - chartControl.FirstTimePainted).TotalSeconds / 2.0);
			}
			else
			{
				dataPoint.Price = chartScale.MinValue + chartScale.MaxMinusMin / 2.0;
			}
			dataPoint.CopyDataValues(EndAnchor);
		}
		else if ((int)((DrawingTool)this).DrawingState == 1 && editingAnchor != null)
		{
			dataPoint.CopyDataValues(editingAnchor);
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
			((DrawingTool)this).DrawingState = (DrawingState)2;
			editingAnchor = null;
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			AnchorLineStroke = new Stroke((Brush)Brushes.DarkGray, (DashStyleHelper)1, 1f);
			AreaBrush = Brushes.Goldenrod;
			AreaOpacity = 25;
			((DrawingTool)this).DrawingState = (DrawingState)0;
			EndAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			OutlineStroke = new Stroke((Brush)Brushes.Goldenrod, 2f);
			StartAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorStart,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			((DrawingTool)this).ZOrderType = (DrawingToolZOrder)1;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		if (!hasSetZOrder && !StartAnchor.IsNinjaScriptDrawn)
		{
			((DrawingTool)this).ZOrderType = (DrawingToolZOrder)0;
			((ChartObject)this).ZOrder = ((ChartObject)this).ChartPanel.ChartObjects.Min((IChartObject z) => z.ZOrder) - 1;
			hasSetZOrder = true;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		Stroke outlineStroke = OutlineStroke;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		double x = (double)val.X + (double)val.W / 2.0;
		double y = (double)val.Y + (double)val.H / 2.0;
		if (Mode == RegionHighlightMode.Price)
		{
			StartAnchor.UpdateXFromPoint(new Point(x, 0.0), chartControl, chartScale);
			EndAnchor.UpdateXFromPoint(new Point(x, 0.0), chartControl, chartScale);
		}
		else
		{
			StartAnchor.UpdateYFromDevicePoint(new Point(0.0, y), chartScale);
			EndAnchor.UpdateYFromDevicePoint(new Point(0.0, y), chartScale);
		}
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = point2.X - point.X;
		AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
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
		float num2 = ((MathExtentions.ApproxCompare(Math.Abs((double)outlineStroke.Width % 2.0), 0.0) == 0) ? 0.5f : 0f);
		RectangleF val2 = ((Mode == RegionHighlightMode.Time) ? new RectangleF((float)point.X + num2, (float)((ChartObject)this).ChartPanel.Y - outlineStroke.Width + num2, (float)num, (float)(val.Y + val.H) + outlineStroke.Width * 2f) : new RectangleF((float)val.X - outlineStroke.Width + num2, (float)point.Y + num2, (float)(val.X + val.W) + outlineStroke.Width * 2f, (float)(point2.Y - point.Y)));
		if (!((ChartObject)this).IsInHitTest && areaBrushDevice.BrushDX != null)
		{
			((ChartObject)this).RenderTarget.FillRectangle(val2, areaBrushDevice.BrushDX);
		}
		Brush val3 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawRectangle(val2, val3, outlineStroke.Width, outlineStroke.StrokeStyle);
		if (((ChartObject)this).IsSelected)
		{
			val3 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point), DxExtensions.ToVector2(point2), val3, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
		}
	}
}
