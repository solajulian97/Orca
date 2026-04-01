using System;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class BoxStyle : ChartStyle
{
	private object icon;

	public override object Icon => icon ?? (icon = Icons.ChartBox2);

	public override int GetBarPaintWidth(int barWidth)
	{
		return 10 - 2 * (int)Math.Round(((ChartStyle)this).Stroke.Width);
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		Bars bars = chartBars.Bars;
		float num = ((ChartStyle)this).ConvertToHorizontalPixels(chartControl, (double)(chartControl.CanvasLeft + chartControl.Properties.BarMarginRight));
		RectangleF val = default(RectangleF);
		int num2 = chartBars.ToIndex;
		if (num2 >= 0 && num2 < bars.Count - 1)
		{
			num2++;
		}
		for (int i = chartBars.FromIndex; i <= num2; i++)
		{
			double close = bars.GetClose(i);
			float num3 = chartScale.GetYByValue(bars.GetHigh(i));
			float num4 = chartScale.GetYByValue(bars.GetLow(i));
			double open = bars.GetOpen(i);
			Brush barOverrideBrush = chartControl.GetBarOverrideBrush(chartBars, i);
			Brush candleOutlineOverrideBrush = chartControl.GetCandleOutlineOverrideBrush(chartBars, i);
			float num5 = chartControl.GetXByBarIndex(chartBars, i);
			float num6 = ((i != chartBars.FromIndex || (num2 != 0 && i != 0)) ? ((float)chartControl.GetXByBarIndex(chartBars, i - 1)) : ((num2 != 0) ? (2f * num5 - (float)chartControl.GetXByBarIndex(chartBars, i + 1)) : num));
			if (!((double)Math.Abs(num5 - num6) < 0.2))
			{
				float num7 = Math.Max(2f, Math.Abs(num5 - num6));
				if (close > open)
				{
					num7 -= ((ChartStyle)this).Stroke.Width;
					((RectangleF)(ref val)).X = num6;
					((RectangleF)(ref val)).Y = num3;
					((RectangleF)(ref val)).Width = num7;
					((RectangleF)(ref val)).Height = num4 - num3;
					ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).UpBrushDX, val);
					ChartStyle.TransformBrush(candleOutlineOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, val);
					((ChartStyle)this).RenderTarget.FillRectangle(val, barOverrideBrush ?? ((ChartStyle)this).UpBrushDX);
					((ChartStyle)this).RenderTarget.DrawRectangle(val, candleOutlineOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
				}
				else
				{
					num7 -= ((ChartStyle)this).Stroke2.Width;
					((RectangleF)(ref val)).X = num6;
					((RectangleF)(ref val)).Y = num3;
					((RectangleF)(ref val)).Width = num7;
					((RectangleF)(ref val)).Height = num4 - num3;
					ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).DownBrushDX, val);
					ChartStyle.TransformBrush(candleOutlineOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, val);
					((ChartStyle)this).RenderTarget.FillRectangle(val, barOverrideBrush ?? ((ChartStyle)this).DownBrushDX);
					((ChartStyle)this).RenderTarget.DrawRectangle(val, candleOutlineOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, ((ChartStyle)this).Stroke2.Width, ((ChartStyle)this).Stroke2.StrokeStyle);
				}
			}
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
			((NinjaScript)this).Name = Resource.NinjaScriptChartStyleBox;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)0;
			((ChartStyle)this).BarWidth = 1.0;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("BarWidthUI", ignoreCase: true));
			((ChartStyle)this).SetPropertyName("DownBrush", Resource.NinjaScriptChartStyleBoxDownBarsColor);
			((ChartStyle)this).SetPropertyName("UpBrush", Resource.NinjaScriptChartStyleBoxUpBarsColor);
			((ChartStyle)this).SetPropertyName("Stroke", Resource.NinjaScriptChartStyleBoxUpBarsOutline);
			((ChartStyle)this).SetPropertyName("Stroke2", Resource.NinjaScriptChartStyleBoxDownBarsOutline);
		}
	}
}
