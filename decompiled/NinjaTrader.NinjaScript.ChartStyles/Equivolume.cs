using System;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class Equivolume : ChartStyle
{
	private object icon;

	public override object Icon => icon ?? (icon = Icons.ChartEquivolume);

	public override int GetBarPaintWidth(int barWidth)
	{
		return 1 + 2 * (barWidth - 1) + 2 * (int)Math.Round(((ChartStyle)this).Stroke.Width);
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		Bars bars = chartBars.Bars;
		Vector2 val = default(Vector2);
		Vector2 val2 = default(Vector2);
		RectangleF val3 = default(RectangleF);
		float num = (float)(((ChartStyle)this).GetBarPaintWidth(((ChartStyle)this).BarWidthUI) - 1) / 2f;
		float num2 = 0f;
		for (int i = chartBars.FromIndex; i < chartBars.ToIndex; i++)
		{
			float num3 = (float)(bars.GetVolume(i) + bars.GetVolume(i + 1)) / 2f;
			if (num3 > num2)
			{
				num2 = num3;
			}
		}
		for (int j = chartBars.FromIndex; j <= chartBars.ToIndex; j++)
		{
			Brush barOverrideBrush = chartControl.GetBarOverrideBrush(chartBars, j);
			Brush candleOutlineOverrideBrush = chartControl.GetCandleOutlineOverrideBrush(chartBars, j);
			double close = bars.GetClose(j);
			double high = bars.GetHigh(j);
			double low = bars.GetLow(j);
			double open = bars.GetOpen(j);
			int yByValue = chartScale.GetYByValue(close);
			int yByValue2 = chartScale.GetYByValue(high);
			int yByValue3 = chartScale.GetYByValue(low);
			int yByValue4 = chartScale.GetYByValue(open);
			float num4 = 1f + 2f * (float)Math.Round((float)bars.GetVolume(j) / num2 * num);
			int xByBarIndex = chartControl.GetXByBarIndex(chartBars, j);
			if ((double)Math.Abs(yByValue4 - yByValue) < 1E-07)
			{
				val.X = (float)xByBarIndex - num4 * 0.5f;
				val.Y = yByValue;
				val2.X = (float)xByBarIndex + num4 * 0.5f;
				val2.Y = yByValue;
				Brush val4 = candleOutlineOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX;
				if (!(val4 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(candleOutlineOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke.Width, num4, ((ChartStyle)this).Stroke.Width));
				}
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, val4, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
			}
			else
			{
				((RectangleF)(ref val3)).X = (float)xByBarIndex - num4 * 0.5f + 0.5f;
				((RectangleF)(ref val3)).Y = Math.Min(yByValue, yByValue4);
				((RectangleF)(ref val3)).Width = num4 - 1f;
				((RectangleF)(ref val3)).Height = Math.Max(yByValue4, yByValue) - Math.Min(yByValue, yByValue4);
				Brush val5 = barOverrideBrush ?? ((close >= open) ? ((ChartStyle)this).UpBrushDX : ((ChartStyle)this).DownBrushDX);
				if (!(val5 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val5, val3);
				}
				((ChartStyle)this).RenderTarget.FillRectangle(val3, val5);
				val5 = candleOutlineOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX;
				if (!(val5 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val5, val3);
				}
				((ChartStyle)this).RenderTarget.DrawRectangle(val3, candleOutlineOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
			}
			Brush val6 = candleOutlineOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX;
			if (high > Math.Min(open, close))
			{
				val.X = xByBarIndex;
				val.Y = yByValue2;
				val2.X = xByBarIndex;
				val2.Y = ((open > close) ? yByValue4 : yByValue);
				if (!(val6 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val6, new RectangleF(val.X - ((ChartStyle)this).Stroke2.Width, val.Y, ((ChartStyle)this).Stroke2.Width, val2.Y - val.Y));
				}
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, val6, ((ChartStyle)this).Stroke2.Width, ((ChartStyle)this).Stroke2.StrokeStyle);
			}
			if (low < Math.Min(open, close))
			{
				val.X = xByBarIndex;
				val.Y = yByValue3;
				val2.X = xByBarIndex;
				val2.Y = ((open < close) ? yByValue4 : yByValue);
				if (!(val6 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val6, new RectangleF(val2.X - ((ChartStyle)this).Stroke2.Width, val2.Y, ((ChartStyle)this).Stroke2.Width, val.Y - val2.Y));
				}
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, val6, ((ChartStyle)this).Stroke2.Width, ((ChartStyle)this).Stroke2.StrokeStyle);
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptChartStyleEquivolume;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)10;
			((ChartStyle)this).BarWidth = 5.0;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).SetPropertyName("BarWidth", Resource.NinjaScriptChartStyleBarWidth);
			((ChartStyle)this).SetPropertyName("DownBrush", Resource.NinjaScriptChartStyleCandleDownBarsColor);
			((ChartStyle)this).SetPropertyName("UpBrush", Resource.NinjaScriptChartStyleCandleUpBarsColor);
			((ChartStyle)this).SetPropertyName("Stroke", Resource.NinjaScriptChartStyleCandleOutline);
			((ChartStyle)this).SetPropertyName("Stroke2", Resource.NinjaScriptChartStyleCandleWick);
		}
	}
}
