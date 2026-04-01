using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NTRes.NinjaTrader.Gui.Chart;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Time Cycles IDrawingTool.
/// </summary>
public class TimeCycles : DrawingTool
{
	private Brush areaBrush;

	private readonly DeviceBrush areaBrushDevice = new DeviceBrush();

	private int areaOpacity;

	private List<int> anchorBars;

	private const int cursorSensitivity = 15;

	private int diameter;

	private int radius;

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

	public override object Icon => Icons.DrawTimeCycles;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 2)]
	public Stroke OutlineStroke { get; set; }

	[Browsable(false)]
	public ChartAnchor StartAnchor { get; set; }

	[Browsable(false)]
	public ChartAnchor EndAnchor { get; set; }

	[PropertyEditor("NinjaTrader.Gui.Tools.ChartAnchorTimeEditor")]
	[Display(ResourceType = typeof(ChartResources), GroupName = "GuiChartsCategoryData", Name = "GuiChartsChartAnchorStartTime", Order = 0)]
	public DateTime StartTime
	{
		get
		{
			return StartAnchor.Time;
		}
		set
		{
			StartAnchor.Time = value;
		}
	}

	[PropertyEditor("NinjaTrader.Gui.Tools.ChartAnchorTimeEditor")]
	[Display(ResourceType = typeof(ChartResources), GroupName = "GuiChartsCategoryData", Name = "GuiChartsChartAnchorEndTime", Order = 1)]
	public DateTime EndTime
	{
		get
		{
			return EndAnchor.Time;
		}
		set
		{
			EndAnchor.Time = value;
		}
	}

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };

	public override bool SupportsAlerts => true;

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
			Name = "Time cycles",
			ShouldOnlyDisplayName = true
		};
	}

	private ChartBars GetChartBars()
	{
		if (((DrawingTool)this).AttachedTo != null)
		{
			IChartObject chartObject = ((DrawingTool)this).AttachedTo.ChartObject;
			ChartBars val = (ChartBars)(object)((chartObject is ChartBars) ? chartObject : null);
			if (val == null)
			{
				IChartObject chartObject2 = ((DrawingTool)this).AttachedTo.ChartObject;
				IChartBars val2 = (IChartBars)(object)((chartObject2 is IChartBars) ? chartObject2 : null);
				if (val2 != null)
				{
					val = val2.ChartBars;
				}
			}
			return val;
		}
		return null;
	}

	private int GetClosestBarAnchor(ChartControl chartControl, Point p, bool ignoreHitTest)
	{
		if (!ignoreHitTest && p.Y < (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H - 15))
		{
			return int.MinValue;
		}
		int num = chartControl.GetXByTime(GetChartBars().GetTimeByBarIdx(chartControl, 0)) - diameter;
		if (anchorBars != null)
		{
			for (int i = 0; i < anchorBars.Count - 1; i++)
			{
				if (anchorBars[i] > num && ((!ignoreHitTest && (double)anchorBars[i] > p.X - 15.0 && (double)anchorBars[i] < p.X + 15.0 && anchorBars[i] > num) || (ignoreHitTest && i > 0 && (double)anchorBars[i] > p.X && (double)anchorBars[i - 1] < p.X)))
				{
					return anchorBars[i];
				}
			}
		}
		return int.MinValue;
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
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
		if ((int)((DrawingTool)this).DrawingState == 1)
		{
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeWE;
			}
			return Cursors.No;
		}
		if (GetClosestBarAnchor(chartControl, point, ignoreHitTest: false) != int.MinValue)
		{
			return Cursors.SizeWE;
		}
		if (IsPointOnTimeCyclesOutline(chartControl, chartPanel, point))
		{
			return Cursors.SizeAll;
		}
		return Cursors.Arrow;
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
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		int num = chartControl.GetXByTime(GetChartBars().GetTimeByBarIdx(chartControl, 0)) - diameter;
		List<Point> list = new List<Point>();
		if (anchorBars != null)
		{
			for (int i = 0; i < anchorBars.Count - 1; i++)
			{
				if (anchorBars[i] > num)
				{
					list.Add(new Point(anchorBars[i], val.Y + val.H));
				}
			}
		}
		return list.ToArray();
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
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Invalid comparison between Unknown and I4
			bool flag = IsPointInsideTimeCycles(chartPanel, GetBarPoint(v));
			if ((int)condition != 8)
			{
				return !flag;
			}
			return flag;
		}
	}

	private bool IsPointInsideTimeCycles(ChartPanel chartPanel, Point p)
	{
		if (radius < 0)
		{
			return false;
		}
		for (int i = 0; i < anchorBars.Count - 1; i++)
		{
			if (MathHelper.IsPointInsideEllipse(new Point(anchorBars[i] + radius, chartPanel.Y + chartPanel.H), p, (double)radius, (double)radius))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsPointOnTimeCyclesOutline(ChartControl chartControl, ChartPanel chartPanel, Point p)
	{
		if (radius < 0)
		{
			return false;
		}
		int num = chartControl.GetXByTime(GetChartBars().GetTimeByBarIdx(chartControl, 0)) - diameter;
		for (int i = 0; i < anchorBars.Count - 1; i++)
		{
			if (anchorBars[i] >= num)
			{
				double num2 = anchorBars[i] + radius;
				double num3 = chartPanel.Y + chartPanel.H;
				double num4 = Math.Atan2(p.Y - num3, p.X - num2);
				double num5 = num4 * (180.0 / Math.PI);
				double num6 = Math.Atan((double)radius * Math.Tan(num4) / (double)radius) + ((num5 > 90.0) ? Math.PI : ((num5 < -90.0) ? (-Math.PI) : 0.0));
				double num7 = num2 + (double)radius * Math.Cos(num6);
				double num8 = num3 + (double)radius * Math.Sin(num6);
				if (p.X < num7 + 15.0 && p.X > num7 - 15.0 && p.Y < num8 + 15.0 && p.Y > num8 - 15.0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		return true;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MaxValue = double.MinValue;
		((ChartObject)this).MinValue = double.MaxValue;
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
			if ((int)drawingState != 2)
			{
				return;
			}
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			Cursor cursor = ((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point);
			if (cursor == Cursors.SizeWE)
			{
				int closestBarAnchor = GetClosestBarAnchor(chartControl, point, ignoreHitTest: false);
				int num = anchorBars.IndexOf(closestBarAnchor);
				if (closestBarAnchor != int.MinValue && num > -1)
				{
					StartAnchor.UpdateXFromPoint(new Point(anchorBars[(num == 0) ? num : (num - 1)], chartPanel.Y + chartPanel.H), chartControl, chartScale);
					EndAnchor.UpdateXFromPoint(new Point(anchorBars[(num == 0) ? 1 : num], chartPanel.Y + chartPanel.H), chartControl, chartScale);
					EndAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
			}
			else if (cursor == Cursors.SizeAll)
			{
				int closestBarAnchor2 = GetClosestBarAnchor(chartControl, point, ignoreHitTest: true);
				int num2 = anchorBars.IndexOf(closestBarAnchor2);
				if (closestBarAnchor2 != int.MinValue && num2 > -1)
				{
					StartAnchor.UpdateXFromPoint(new Point(anchorBars[num2 - 1], chartPanel.Y + chartPanel.H), chartControl, chartScale);
					EndAnchor.UpdateXFromPoint(new Point(anchorBars[num2], chartPanel.Y + chartPanel.H), chartControl, chartScale);
					_003F val = this;
					object obj = dataPoint.Clone();
					((DrawingTool)val).InitialMouseDownAnchor = (ChartAnchor)((obj is ChartAnchor) ? obj : null);
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
			}
			else
			{
				((ChartObject)this).IsSelected = false;
			}
		}
		else if (StartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(StartAnchor);
			dataPoint.CopyDataValues(EndAnchor);
			StartAnchor.IsEditing = false;
		}
		else if (EndAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(EndAnchor);
			EndAnchor.IsEditing = false;
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
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
			if (StartAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(StartAnchor);
			}
			if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
			}
			break;
		case 1:
			dataPoint.CopyDataValues(EndAnchor);
			break;
		case 3:
			StartAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			EndAnchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			break;
		case 2:
			break;
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			if ((int)((DrawingTool)this).DrawingState == 1)
			{
				EndAnchor.IsEditing = false;
			}
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		int num = Convert.ToInt32(StartAnchor.GetPoint(chartControl, val, chartScale, true).X);
		diameter = Math.Abs(num - Convert.ToInt32(EndAnchor.GetPoint(chartControl, val, chartScale, true).X));
		radius = Convert.ToInt32((double)diameter / 2.0);
		if (radius <= 0)
		{
			return;
		}
		UpdateAnchors(chartControl, num);
		if (anchorBars.Count <= 2)
		{
			return;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		Stroke outlineStroke = OutlineStroke;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		bool flag = false;
		if (!((ChartObject)this).IsInHitTest && AreaBrush != null)
		{
			if (areaBrushDevice.Brush == null)
			{
				Brush brush = areaBrush.Clone();
				brush.Opacity = (double)areaOpacity / 100.0;
				areaBrushDevice.Brush = brush;
			}
			areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
			flag = true;
		}
		else
		{
			areaBrushDevice.RenderTarget = null;
			areaBrushDevice.Brush = null;
		}
		Ellipse val2 = default(Ellipse);
		for (int i = 0; i < anchorBars.Count - 1; i++)
		{
			((Ellipse)(ref val2))._002Ector(new Vector2((float)(anchorBars[i] + radius), (float)(val.Y + val.H)), (float)radius, (float)radius);
			if (flag)
			{
				((ChartObject)this).RenderTarget.FillEllipse(val2, areaBrushDevice.BrushDX);
			}
			Brush val3 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawEllipse(val2, val3);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			AreaBrush = Brushes.CornflowerBlue;
			AreaOpacity = 40;
			((DrawingTool)this).DrawingState = (DrawingState)0;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolTimeCycles;
			OutlineStroke = new Stroke((Brush)Brushes.CornflowerBlue, (DashStyleHelper)0, 2f, 100);
			StartAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorStart,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this,
				IsYPropertyVisible = false
			};
			EndAnchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this,
				IsYPropertyVisible = false
			};
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	private void UpdateAnchors(ChartControl chartControl, int startX)
	{
		List<int> list = new List<int>();
		if (!StartAnchor.IsEditing && diameter > 0)
		{
			int num = chartControl.GetXByTime(chartControl.FirstTimePainted) - diameter;
			int num2 = chartControl.GetXByTime(chartControl.LastTimePainted) + diameter;
			if (startX <= num2 && startX >= num)
			{
				list.Add(startX);
			}
			int num3 = startX;
			do
			{
				num3 -= diameter;
				if (num3 <= num2 && num3 >= num)
				{
					list.Add(num3);
				}
			}
			while (num3 >= num);
			list.Add(num3);
			num3 = ((list.Count == 0) ? int.MinValue : list[list.Count - 1]);
			int num4 = ((num3 == int.MinValue) ? startX : list[0]);
			do
			{
				num4 += diameter;
				if (num4 <= num2 && num4 >= num)
				{
					list.Add(num4);
				}
			}
			while (num4 <= num2);
			list.Add(num4);
		}
		else
		{
			list.Add(startX);
		}
		anchorBars = list.OrderBy((int x) => x).ToList();
	}
}
