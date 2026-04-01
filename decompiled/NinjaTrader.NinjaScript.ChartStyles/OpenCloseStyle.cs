using System;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class OpenCloseStyle : ChartStyle
{
	private object icon;

	public override object Icon => icon ?? (icon = Icons.ChartOpenClose);

	public override int GetBarPaintWidth(int barWidth)
	{
		return 1 + 2 * (barWidth - 1) + 2 * (int)Math.Round(((ChartStyle)this).Stroke.Width);
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		Bars bars = chartBars.Bars;
		float num = ((ChartStyle)this).GetBarPaintWidth(((ChartStyle)this).BarWidthUI);
		RectangleF val = default(RectangleF);
		for (int i = chartBars.FromIndex; i <= chartBars.ToIndex; i++)
		{
			Brush obj = chartControl.GetBarOverrideBrush(chartBars, i);
			Brush candleOutlineOverrideBrush = chartControl.GetCandleOutlineOverrideBrush(chartBars, i);
			double close = bars.GetClose(i);
			float val2 = chartScale.GetYByValue(close);
			double open = bars.GetOpen(i);
			float val3 = chartScale.GetYByValue(open);
			float num2 = chartControl.GetXByBarIndex(chartBars, i);
			Stroke val4 = ((close >= open) ? ((ChartStyle)this).Stroke : ((ChartStyle)this).Stroke2);
			((RectangleF)(ref val)).X = num2 - num * 0.5f + 0.5f;
			((RectangleF)(ref val)).Y = Math.Min(val3, val2);
			((RectangleF)(ref val)).Width = num - 1f;
			((RectangleF)(ref val)).Height = Math.Max(val3, val2) - Math.Min(val3, val2);
			Brush val5 = obj ?? ((close >= open) ? ((ChartStyle)this).UpBrushDX : ((ChartStyle)this).DownBrushDX);
			if (!(val5 is SolidColorBrush))
			{
				ChartStyle.TransformBrush(val5, val);
			}
			((ChartStyle)this).RenderTarget.FillRectangle(val, val5);
			if (obj == null)
			{
				obj = val4.BrushDX;
			}
			val5 = obj;
			if (!(val5 is SolidColorBrush))
			{
				ChartStyle.TransformBrush(val5, val);
			}
			((ChartStyle)this).RenderTarget.DrawRectangle(val, candleOutlineOverrideBrush ?? val4.BrushDX, val4.Width, val4.StrokeStyle);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptChartStyleOpenClose;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)6;
			((ChartStyle)this).BarWidth = 3.0;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).SetPropertyName("BarWidth", Resource.NinjaScriptChartStyleBarWidth);
			((ChartStyle)this).SetPropertyName("DownBrush", Resource.NinjaScriptChartStyleOpenCloseDownBarsColor);
			((ChartStyle)this).SetPropertyName("UpBrush", Resource.NinjaScriptChartStyleOpenCloseUpBarsColor);
			((ChartStyle)this).SetPropertyName("Stroke", Resource.NinjaScriptChartStyleOpenCloseUpBarsOutline);
			((ChartStyle)this).SetPropertyName("Stroke2", Resource.NinjaScriptChartStyleOpenCloseDownBarsOutline);
		}
	}
}
