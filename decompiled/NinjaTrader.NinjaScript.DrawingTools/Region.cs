using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Region IDrawingTool.
/// </summary>
public class Region : DrawingTool
{
	private int areaOpacity;

	private Brush areaBrush;

	private readonly DeviceBrush areaBrushDevice = new DeviceBrush();

	public ChartAnchor StartAnchor { get; set; }

	public ChartAnchor EndAnchor { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public ISeries<double> Series1 { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public ISeries<double> Series2 { get; set; }

	[Browsable(false)]
	public double Price { get; set; }

	[Browsable(false)]
	public int Displacement { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 4)]
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
				areaBrushDevice.Brush = null;
			}
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
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 5)]
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

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 6)]
	public Stroke OutlineStroke { get; set; }

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		if (areaBrushDevice != null)
		{
			areaBrushDevice.RenderTarget = null;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (!(((DrawingTool)this).AttachedTo.ChartObject is IChartBars) || ((IChartBars)((DrawingTool)this).AttachedTo.ChartObject).ChartBars == null)
		{
			return false;
		}
		if (!StartAnchor.IsNinjaScriptDrawn || !EndAnchor.IsNinjaScriptDrawn)
		{
			return false;
		}
		DateTime time = StartAnchor.Time;
		DateTime time2 = EndAnchor.Time;
		if (!(time >= firstTimeOnChart) && !(time2 <= lastTimeOnChart))
		{
			if (time < firstTimeOnChart)
			{
				return time2 > lastTimeOnChart;
			}
			return false;
		}
		return true;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolRegion;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolRegion;
			((DrawingTool)this).DisplayOnChartsMenus = false;
			((DrawingTool)this).IgnoresUserInput = true;
			StartAnchor = new ChartAnchor
			{
				IsYPropertyVisible = false,
				IsXPropertiesVisible = false
			};
			EndAnchor = new ChartAnchor
			{
				IsYPropertyVisible = false,
				IsXPropertiesVisible = false
			};
			StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorStart;
			EndAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd;
			AreaBrush = Brushes.DarkCyan;
			OutlineStroke = new Stroke((Brush)Brushes.Goldenrod);
			AreaOpacity = 40;
			((DrawingTool)this).ZOrderType = (DrawingToolZOrder)1;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I4
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Invalid comparison between Unknown and I4
		//IL_0459: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Invalid comparison between Unknown and I4
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Invalid comparison between Unknown and I4
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_069b: Expected O, but got Unknown
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Invalid comparison between Unknown and I4
		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Invalid comparison between Unknown and I4
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Invalid comparison between Unknown and I4
		//IL_04ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Invalid comparison between Unknown and I4
		//IL_073d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Invalid comparison between Unknown and I4
		//IL_075b: Unknown result type (might be due to invalid IL or missing references)
		//IL_077f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0572: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ed: Unknown result type (might be due to invalid IL or missing references)
		if (Series1 == null)
		{
			return;
		}
		ChartBars chartBars = ((IChartBars)((DrawingTool)this).AttachedTo.ChartObject).ChartBars;
		IChartObject chartObject = ((DrawingTool)this).AttachedTo.ChartObject;
		NinjaScriptBase val = (NinjaScriptBase)(object)((chartObject is NinjaScriptBase) ? chartObject : null);
		if (val == null || chartBars == null || Math.Abs(Series1.Count - chartBars.Count) > 1)
		{
			return;
		}
		int num;
		int num2;
		if ((int)chartControl.BarSpacingType == 3)
		{
			num = chartBars.GetBarIdxByTime(chartControl, StartAnchor.Time);
			num2 = chartBars.GetBarIdxByTime(chartControl, EndAnchor.Time);
		}
		else
		{
			num = StartAnchor.DrawnOnBar - StartAnchor.BarsAgo;
			num2 = EndAnchor.DrawnOnBar - EndAnchor.BarsAgo;
			if (num == num2)
			{
				num = chartBars.GetBarIdxByTime(chartControl, StartAnchor.Time);
				num2 = chartBars.GetBarIdxByTime(chartControl, EndAnchor.Time);
			}
		}
		int num3 = Math.Min(num, num2);
		int num4 = Math.Max(num, num2);
		int num5 = Math.Max(val.BarsRequiredToPlot + Displacement, chartBars.GetBarIdxByTime(chartControl, chartControl.GetTimeByX(0)) - 1);
		int num6 = Math.Max(chartBars.ToIndex, chartBars.GetBarIdxByTime(chartControl, chartControl.LastTimePainted)) + 1;
		num3 = Math.Max(0, Math.Max(num5, num3 + Displacement));
		num4 = Math.Max(0, Math.Min(num4 + Displacement, num6));
		if (num3 > num6 || num4 < num5)
		{
			return;
		}
		ISeries<double> val2 = Series1;
		ISeries<double> val3 = Series2;
		ISeries<double> series = Series1;
		NinjaScriptBase val4 = (NinjaScriptBase)(object)((series is NinjaScriptBase) ? series : null);
		if (val4 != null)
		{
			val2 = (ISeries<double>)(object)val4.Value;
		}
		if (val2 == null)
		{
			return;
		}
		ISeries<double> series2 = Series2;
		NinjaScriptBase val5 = (NinjaScriptBase)(object)((series2 is NinjaScriptBase) ? series2 : null);
		if (val5 != null)
		{
			val3 = (ISeries<double>)(object)val5.Value;
		}
		Vector2[] array = Array.Empty<Vector2>();
		int num7 = 0;
		int num8 = 0;
		Vector2[] array2;
		if (val3 == null)
		{
			array2 = (Vector2[])(object)new Vector2[num4 - num3 + 1 + 2];
			for (int i = num3; i <= num4; i++)
			{
				if (i >= Math.Max(0, Displacement) && i <= Math.Max(chartBars.Count - (((int)val.Calculate != 0) ? 1 : 2) + Displacement, num4))
				{
					int num9 = Math.Min(chartBars.Count - (((int)val.Calculate != 0) ? 1 : 2), Math.Max(0, i - Displacement));
					double valueAt = val2.GetValueAt(num9);
					float num10 = chartScale.GetYByValue(valueAt);
					float num11 = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && i >= chartBars.Count)) ? chartControl.GetXByTime(chartBars.GetTimeByBarIdx(chartControl, i)) : chartControl.GetXByBarIndex(chartBars, i));
					double x = ((num11 % 1f != 0f) ? 0.0 : 0.5);
					double y = ((num10 % 1f != 0f) ? 0.0 : 0.5);
					Vector vector = new Vector(x, y);
					Point point = new Point(num11, num10) + vector;
					array2[num7] = DxExtensions.ToVector2(point);
					num7++;
				}
			}
			array2[num7].X = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && num4 >= chartBars.Count)) ? chartControl.GetXByTime(chartBars.GetTimeByBarIdx(chartControl, num4)) : chartControl.GetXByBarIndex(chartBars, num4));
			array2[num7++].Y = chartScale.GetYByValue(Math.Max(chartScale.MinValue, Math.Min(chartScale.MaxValue, Price)));
			array2[num7].X = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && num3 >= chartBars.Count)) ? chartControl.GetXByTime(chartBars.GetTimeByBarIdx(chartControl, num3)) : chartControl.GetXByBarIndex(chartBars, num3));
			array2[num7++].Y = chartScale.GetYByValue(Math.Max(chartScale.MinValue, Math.Min(chartScale.MaxValue, Price)));
		}
		else
		{
			array2 = (Vector2[])(object)new Vector2[num4 - num3 + 1];
			array = (Vector2[])(object)new Vector2[num4 - num3 + 1];
			for (int j = num3; j <= num4; j++)
			{
				if (j < Math.Max(0, Displacement) || j > Math.Max(chartBars.Count - (((int)val.Calculate != 0) ? 1 : 2) + Displacement, num4))
				{
					continue;
				}
				int num12 = Math.Min(chartBars.Count - (((int)val.Calculate != 0) ? 1 : 2), Math.Max(0, j - Displacement));
				float num13 = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && j >= chartBars.Count)) ? chartControl.GetXByTime(chartBars.GetTimeByBarIdx(chartControl, j)) : chartControl.GetXByBarIndex(chartBars, j));
				if (val2.IsValidDataPointAt(num12))
				{
					double valueAt2 = val2.GetValueAt(num12);
					float num14 = chartScale.GetYByValue(valueAt2);
					double x2 = ((num13 % 1f != 0f) ? 0.0 : 0.5);
					double y2 = ((num14 % 1f != 0f) ? 0.0 : 0.5);
					Vector vector2 = new Vector(x2, y2);
					Point point2 = new Point(num13, num14) + vector2;
					array2[num7] = DxExtensions.ToVector2(point2);
					num7++;
					if (val3.IsValidDataPointAt(num12))
					{
						valueAt2 = val3.GetValueAt(num12);
						num14 = chartScale.GetYByValue(valueAt2);
						y2 = ((num14 % 1f != 0f) ? 0.0 : 0.5);
						vector2 = new Vector(x2, y2);
						point2 = new Point(num13, num14) + vector2;
						array[num8] = DxExtensions.ToVector2(point2);
						num8++;
					}
				}
			}
		}
		if (num7 + num8 <= 2)
		{
			return;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		if (OutlineStroke != null)
		{
			OutlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		}
		if (AreaBrush != null)
		{
			if (areaBrushDevice.Brush == null)
			{
				Brush brush = areaBrush.Clone();
				brush.Opacity = (double)areaOpacity / 100.0;
				areaBrushDevice.Brush = brush;
			}
			areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		}
		PathGeometry val6 = new PathGeometry(Globals.D2DFactory);
		GeometrySink val7 = val6.Open();
		double x3 = ((array2[0].X % 1f != 0f) ? 0.0 : 0.5);
		double y3 = ((array2[0].Y % 1f != 0f) ? 0.0 : 0.5);
		Vector vector3 = new Vector(x3, y3);
		Point point3 = new Point(array2[0].X, array2[0].Y) + vector3;
		((SimplifiedGeometrySink)val7).BeginFigure(DxExtensions.ToVector2(point3), (FigureBegin)0);
		((SimplifiedGeometrySink)val7).SetFillMode((FillMode)1);
		for (int k = 1; k < num7; k++)
		{
			val7.AddLine(array2[k]);
		}
		for (int num15 = num8 - 1; num15 >= 0; num15--)
		{
			val7.AddLine(array[num15]);
		}
		((SimplifiedGeometrySink)val7).EndFigure((FigureEnd)1);
		((SimplifiedGeometrySink)val7).Close();
		object obj2;
		if (!((ChartObject)this).IsInHitTest)
		{
			DeviceBrush obj = areaBrushDevice;
			obj2 = ((obj != null) ? obj.BrushDX : null);
		}
		else
		{
			obj2 = chartControl.SelectionBrush;
		}
		Brush val8 = (Brush)obj2;
		if (val8 != null)
		{
			((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val6, val8);
		}
		object obj3;
		if (!((ChartObject)this).IsInHitTest)
		{
			Stroke outlineStroke = OutlineStroke;
			obj3 = ((outlineStroke != null) ? outlineStroke.BrushDX : null);
		}
		else
		{
			obj3 = chartControl.SelectionBrush;
		}
		val8 = (Brush)obj3;
		if (val8 != null)
		{
			((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)val6, val8, OutlineStroke.Width);
		}
		((DisposeBase)val6).Dispose();
	}
}
