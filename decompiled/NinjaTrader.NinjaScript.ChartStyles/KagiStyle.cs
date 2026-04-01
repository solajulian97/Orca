using System;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class KagiStyle : ChartStyle
{
	private object icon;

	private bool thickLine;

	public override object Icon => icon ?? (icon = Icons.ChartKagiLine);

	public override int GetBarPaintWidth(int barWidth)
	{
		return 1 + 2 * barWidth + 2 * (int)Math.Round(((ChartStyle)this).Stroke2.Width);
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b01: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b08: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b27: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b37: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b39: Unknown result type (might be due to invalid IL or missing references)
		//IL_09be: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_054e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0555: Unknown result type (might be due to invalid IL or missing references)
		//IL_0574: Unknown result type (might be due to invalid IL or missing references)
		//IL_0584: Unknown result type (might be due to invalid IL or missing references)
		//IL_0586: Unknown result type (might be due to invalid IL or missing references)
		//IL_040b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0443: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b9c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ba3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bc2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a60: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a8f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a91: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_060f: Unknown result type (might be due to invalid IL or missing references)
		//IL_061f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0621: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cbc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ccc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0774: Unknown result type (might be due to invalid IL or missing references)
		//IL_077b: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d9b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0da2: Unknown result type (might be due to invalid IL or missing references)
		//IL_088d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0894: Unknown result type (might be due to invalid IL or missing references)
		//IL_0deb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dfb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dfd: Unknown result type (might be due to invalid IL or missing references)
		//IL_08dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eaa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eb1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ed0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ee0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ee2: Unknown result type (might be due to invalid IL or missing references)
		if (chartBars.FromIndex > chartBars.ToIndex)
		{
			return;
		}
		if (chartBars.FromIndex > 0)
		{
			int fromIndex = chartBars.FromIndex;
			chartBars.FromIndex = fromIndex - 1;
		}
		Bars bars = chartBars.Bars;
		float num = ((ChartStyle)this).GetBarPaintWidth(((ChartStyle)this).BarWidthUI);
		int num2 = chartBars.FromIndex;
		int num3 = (int)((double)((ChartStyle)this).Stroke.Width * 0.5);
		int num4 = (int)((double)((ChartStyle)this).Stroke2.Width * 0.5);
		Vector2 val = default(Vector2);
		Vector2 val2 = default(Vector2);
		while (num2 > 0 && bars.BarsType.IsIntraday && !bars.BarsSeries.GetIsFirstBarOfSession(num2))
		{
			num2--;
		}
		if (num2 < 0)
		{
			return;
		}
		thickLine = bars.GetClose(num2) > bars.GetOpen(num2);
		for (int i = num2 + 1; i < chartBars.FromIndex; i++)
		{
			double close = bars.GetClose(i);
			if (close > bars.GetOpen(i))
			{
				if (Math.Max(bars.GetOpen(i - 1), bars.GetClose(i - 1)) < close)
				{
					thickLine = true;
				}
			}
			else if (close < Math.Min(bars.GetOpen(i - 1), bars.GetClose(i - 1)))
			{
				thickLine = false;
			}
		}
		for (int j = chartBars.FromIndex; j <= chartBars.ToIndex; j++)
		{
			Brush barOverrideBrush = chartControl.GetBarOverrideBrush(chartBars, j);
			double open = bars.GetOpen(j);
			float num5 = chartScale.GetYByValue(open);
			double close2 = bars.GetClose(j);
			float num6 = chartScale.GetYByValue(close2);
			float x = chartControl.GetXByBarIndex(chartBars, j);
			double val3 = ((j == 0) ? open : bars.GetOpen(j - 1));
			double val4 = ((j == 0) ? close2 : bars.GetClose(j - 1));
			float num10;
			if (j == 0 && chartBars.ToIndex >= 1)
			{
				float num7 = chartControl.GetXByBarIndex(chartBars, 0);
				float num8 = chartControl.GetXByBarIndex(chartBars, 1);
				float num9 = Math.Max(1f, num8 - num7);
				num10 = num7 - num9;
			}
			else
			{
				num10 = ((j == chartBars.FromIndex) ? chartControl.GetXByBarIndex(chartBars, j) : chartControl.GetXByBarIndex(chartBars, j - 1));
			}
			num10 = ((num10 < 0f) ? 0f : num10);
			if (bars.BarsType.IsIntraday && bars.IsResetOnNewTradingDay && bars.BarsSeries.GetIsFirstBarOfSession(j))
			{
				if (close2 > open)
				{
					val.X = x;
					val.Y = num5;
					val2.X = x;
					val2.Y = num6;
					ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke.Width, num, ((ChartStyle)this).Stroke.Width));
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
					thickLine = true;
				}
				else
				{
					val.X = x;
					val.Y = num5;
					val2.X = x;
					val2.Y = num6;
					ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke2.Width, num, ((ChartStyle)this).Stroke2.Width));
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, ((ChartStyle)this).Stroke2.Width, ((ChartStyle)this).Stroke2.StrokeStyle);
					thickLine = false;
				}
			}
			else if (close2 > open)
			{
				if (close2 <= Math.Max(val4, val3))
				{
					if (thickLine)
					{
						val.X = x;
						val.Y = num5 + (float)num3;
						val2.X = x;
						val2.Y = num6 - (float)num3;
						ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke.Width, num, ((ChartStyle)this).Stroke.Width));
						((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
						val.X = num10;
						val.Y = num5;
						val2.X = x;
						val2.Y = num5;
						ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke.Width, num, ((ChartStyle)this).Stroke.Width));
						((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
					}
					else
					{
						val.X = x;
						val.Y = num5 + (float)num4;
						val2.X = x;
						val2.Y = num6 - (float)num4;
						ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke2.Width, num, ((ChartStyle)this).Stroke2.Width));
						((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, ((ChartStyle)this).Stroke2.Width, ((ChartStyle)this).Stroke2.StrokeStyle);
						val.X = num10;
						val.Y = num5;
						val2.X = x;
						val2.Y = num5;
						ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke2.Width, num, ((ChartStyle)this).Stroke2.Width));
						((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, ((ChartStyle)this).Stroke2.Width, ((ChartStyle)this).Stroke2.StrokeStyle);
					}
				}
				else if (close2 > Math.Max(val4, val3))
				{
					double num11 = Math.Max(val4, val3);
					val.X = x;
					val.Y = num6 - (float)num3;
					val2.X = x;
					val2.Y = chartScale.GetYByValue(num11);
					ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke.Width, num, ((ChartStyle)this).Stroke.Width));
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
					val.X = x;
					val.Y = chartScale.GetYByValue(num11);
					val2.X = x;
					val2.Y = num5 + (float)(thickLine ? num3 : num4);
					ChartStyle.TransformBrush(barOverrideBrush ?? (thickLine ? ((ChartStyle)this).Stroke.BrushDX : ((ChartStyle)this).Stroke2.BrushDX), new RectangleF(val.X, val.Y - (thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width), num, thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width));
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? (thickLine ? ((ChartStyle)this).Stroke.BrushDX : ((ChartStyle)this).Stroke2.BrushDX), thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width, thickLine ? ((ChartStyle)this).Stroke.StrokeStyle : ((ChartStyle)this).Stroke2.StrokeStyle);
					val.X = num10;
					val.Y = num5;
					val2.X = x;
					val2.Y = num5;
					ChartStyle.TransformBrush(barOverrideBrush ?? (thickLine ? ((ChartStyle)this).Stroke.BrushDX : ((ChartStyle)this).Stroke2.BrushDX), new RectangleF(val.X, val.Y - (thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width), num, thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width));
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? (thickLine ? ((ChartStyle)this).Stroke.BrushDX : ((ChartStyle)this).Stroke2.BrushDX), thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width, thickLine ? ((ChartStyle)this).Stroke.StrokeStyle : ((ChartStyle)this).Stroke2.StrokeStyle);
					thickLine = true;
				}
			}
			else if (Math.Min(val4, val3) <= close2)
			{
				if (thickLine)
				{
					val.X = x;
					val.Y = num5 - (float)num3;
					val2.X = x;
					val2.Y = num6 + (float)num3;
					ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke.Width, num, ((ChartStyle)this).Stroke.Width));
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
					val.X = num10;
					val.Y = num5;
					val2.X = x;
					val2.Y = num5;
					ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke.Width, num, ((ChartStyle)this).Stroke.Width));
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke.BrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
				}
				else
				{
					val.X = x;
					val.Y = num5 - (float)num4;
					val2.X = x;
					val2.Y = num6 + (float)num4;
					ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke2.Width, num, ((ChartStyle)this).Stroke2.Width));
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, ((ChartStyle)this).Stroke2.Width, ((ChartStyle)this).Stroke2.StrokeStyle);
					val.X = num10;
					val.Y = num5;
					val2.X = x;
					val2.Y = num5;
					ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke2.Width, num, ((ChartStyle)this).Stroke2.Width));
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, ((ChartStyle)this).Stroke2.Width, ((ChartStyle)this).Stroke2.StrokeStyle);
				}
			}
			else if (close2 < Math.Min(val4, val3))
			{
				double num12 = Math.Min(val4, val3);
				val.X = num10;
				val.Y = num5;
				val2.X = x;
				val2.Y = num5;
				ChartStyle.TransformBrush(barOverrideBrush ?? (thickLine ? ((ChartStyle)this).Stroke.BrushDX : ((ChartStyle)this).Stroke2.BrushDX), new RectangleF(val.X, val.Y - (thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width), num, thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width));
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? (thickLine ? ((ChartStyle)this).Stroke.BrushDX : ((ChartStyle)this).Stroke2.BrushDX), thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width, thickLine ? ((ChartStyle)this).Stroke.StrokeStyle : ((ChartStyle)this).Stroke2.StrokeStyle);
				val.X = x;
				val.Y = num5 - (float)(thickLine ? num3 : num4);
				val2.X = x;
				val2.Y = chartScale.GetYByValue(num12);
				ChartStyle.TransformBrush(barOverrideBrush ?? (thickLine ? ((ChartStyle)this).Stroke.BrushDX : ((ChartStyle)this).Stroke2.BrushDX), new RectangleF(val.X, val.Y - (thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width), num, thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width));
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? (thickLine ? ((ChartStyle)this).Stroke.BrushDX : ((ChartStyle)this).Stroke2.BrushDX), thickLine ? ((ChartStyle)this).Stroke.Width : ((ChartStyle)this).Stroke2.Width, thickLine ? ((ChartStyle)this).Stroke.StrokeStyle : ((ChartStyle)this).Stroke2.StrokeStyle);
				val.X = x;
				val.Y = chartScale.GetYByValue(num12);
				val2.X = x;
				val2.Y = num6 + (float)num4;
				ChartStyle.TransformBrush(barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke2.Width, num, ((ChartStyle)this).Stroke2.Width));
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, barOverrideBrush ?? ((ChartStyle)this).Stroke2.BrushDX, ((ChartStyle)this).Stroke2.Width, ((ChartStyle)this).Stroke2.StrokeStyle);
				thickLine = false;
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptChartStyleKagi;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)5;
			((ChartStyle)this).Stroke = new Stroke(Globals.GeneralOptions.BrushUpPrimary, 3f)
			{
				IsOpacityVisible = false
			};
			((ChartStyle)this).Stroke2 = new Stroke(Globals.GeneralOptions.BrushDownPrimary, 1f)
			{
				IsOpacityVisible = false
			};
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("BarWidthUI", ignoreCase: true));
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("DownBrush", ignoreCase: true));
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("UpBrush", ignoreCase: true));
			((ChartStyle)this).SetPropertyName("Stroke", Resource.NinjaScriptChartStyleKagiThickLine);
			((ChartStyle)this).SetPropertyName("Stroke2", Resource.NinjaScriptChartStyleKagiThinLine);
		}
	}
}
