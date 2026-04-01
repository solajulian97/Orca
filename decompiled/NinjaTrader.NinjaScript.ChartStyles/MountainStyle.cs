using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class MountainStyle : ChartStyle
{
	private object icon;

	public override object Icon => icon ?? (icon = Icons.ChartMountainChart);

	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral")]
	public int Opacity { get; set; }

	public override int GetBarPaintWidth(int barWidth)
	{
		return 1 + 2 * (barWidth - 1) + 2 * barWidth;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		Bars bars = chartBars.Bars;
		if (chartBars.FromIndex > 0)
		{
			int fromIndex = chartBars.FromIndex;
			chartBars.FromIndex = fromIndex - 1;
		}
		PathGeometry val = new PathGeometry(Globals.D2DFactory);
		AntialiasMode antialiasMode = ((ChartStyle)this).RenderTarget.AntialiasMode;
		GeometrySink val2 = val.Open();
		((SimplifiedGeometrySink)val2).BeginFigure(new Vector2((float)chartControl.GetXByBarIndex(chartBars, (chartBars.FromIndex > -1) ? chartBars.FromIndex : 0), (float)chartScale.GetYByValue(bars.GetClose((chartBars.FromIndex > -1) ? chartBars.FromIndex : 0))), (FigureBegin)0);
		for (int i = chartBars.FromIndex + 1; i <= chartBars.ToIndex; i++)
		{
			double close = bars.GetClose(i);
			float num = chartScale.GetYByValue(close);
			float num2 = chartControl.GetXByBarIndex(chartBars, i);
			val2.AddLine(new Vector2(num2, num));
		}
		((SimplifiedGeometrySink)val2).EndFigure((FigureEnd)0);
		((SimplifiedGeometrySink)val2).Close();
		((ChartStyle)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		((ChartStyle)this).RenderTarget.DrawGeometry((Geometry)(object)val, ((ChartStyle)this).UpBrushDX, (float)Math.Max(1.0, chartBars.Properties.ChartStyle.BarWidth));
		((DisposeBase)val).Dispose();
		SolidColorBrush val3 = new SolidColorBrush(((ChartStyle)this).RenderTarget, Color.op_Implicit(Color.Transparent));
		PathGeometry val4 = new PathGeometry(Globals.D2DFactory);
		GeometrySink val5 = val4.Open();
		((SimplifiedGeometrySink)val5).BeginFigure(new Vector2((float)chartControl.GetXByBarIndex(chartBars, (chartBars.FromIndex > -1) ? chartBars.FromIndex : 0), (float)chartScale.GetYByValue(chartScale.MinValue)), (FigureBegin)0);
		float num3 = float.NaN;
		for (int j = chartBars.FromIndex; j <= chartBars.ToIndex; j++)
		{
			double close2 = bars.GetClose(j);
			float num4 = chartScale.GetYByValue(close2);
			num3 = chartControl.GetXByBarIndex(chartBars, j);
			val5.AddLine(new Vector2(num3, num4));
		}
		if (!double.IsNaN(num3))
		{
			val5.AddLine(new Vector2(num3, (float)chartScale.GetYByValue(chartScale.MinValue)));
		}
		((SimplifiedGeometrySink)val5).EndFigure((FigureEnd)0);
		((SimplifiedGeometrySink)val5).Close();
		((ChartStyle)this).DownBrushDX.Opacity = (float)Opacity / 100f;
		if (!(((ChartStyle)this).DownBrushDX is SolidColorBrush))
		{
			ChartStyle.TransformBrush(((ChartStyle)this).DownBrushDX, new RectangleF(0f, 0f, (float)chartScale.Width, (float)chartScale.Height));
		}
		((ChartStyle)this).RenderTarget.FillGeometry((Geometry)(object)val4, ((ChartStyle)this).DownBrushDX);
		((ChartStyle)this).RenderTarget.DrawGeometry((Geometry)(object)val4, (Brush)(object)val3, (float)chartBars.Properties.ChartStyle.BarWidth);
		((DisposeBase)val3).Dispose();
		((ChartStyle)this).RenderTarget.AntialiasMode = antialiasMode;
		((DisposeBase)val4).Dispose();
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptChartStyleMountain;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)7;
			((ChartStyle)this).UpBrush = Brushes.DimGray;
			((ChartStyle)this).DownBrush = Brushes.DimGray;
			((ChartStyle)this).BarWidth = 1.0;
			Opacity = 50;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke", ignoreCase: true));
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke2", ignoreCase: true));
			((ChartStyle)this).SetPropertyName("UpBrush", Resource.NinjaScriptChartStyleMountainOutline);
			((ChartStyle)this).SetPropertyName("DownBrush", Resource.NinjaScriptChartStyleMountainColor);
		}
	}
}
