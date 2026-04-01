using System;
using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class PointAndFigureStyle : ChartStyle
{
	private object icon;

	private bool isUp;

	private bool trendDetermined;

	public override object Icon => icon ?? (icon = Icons.ChartPnF);

	public override int GetBarPaintWidth(int barWidth)
	{
		return 1 + 2 * (barWidth - 1) + 2 * (int)Math.Round(((ChartStyle)this).Stroke.Width);
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0453: Unknown result type (might be due to invalid IL or missing references)
		//IL_0597: Unknown result type (might be due to invalid IL or missing references)
		//IL_0599: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0566: Unknown result type (might be due to invalid IL or missing references)
		//IL_0587: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0654: Unknown result type (might be due to invalid IL or missing references)
		//IL_0656: Unknown result type (might be due to invalid IL or missing references)
		//IL_061c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0623: Unknown result type (might be due to invalid IL or missing references)
		//IL_0644: Unknown result type (might be due to invalid IL or missing references)
		double num = Math.Floor(10000000.0 * (double)chartBars.Bars.BarsPeriod.Value * chartBars.Bars.Instrument.MasterInstrument.TickSize) / 10000000.0;
		int num2 = (int)Math.Round((double)ChartingExtensions.ConvertToVerticalPixels(chartScale.Height, chartControl.PresentationSource) / Math.Round(chartScale.MaxMinusMin / num, 0));
		AntialiasMode antialiasMode = ((ChartStyle)this).RenderTarget.AntialiasMode;
		((ChartStyle)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		trendDetermined = false;
		Vector2 val = default(Vector2);
		Vector2 val2 = default(Vector2);
		Ellipse val3 = default(Ellipse);
		Vector2 val4 = default(Vector2);
		Vector2 val5 = default(Vector2);
		for (int i = chartBars.FromIndex; i <= chartBars.ToIndex; i++)
		{
			int barPaintWidth = ((ChartStyle)this).GetBarPaintWidth(((ChartStyle)this).BarWidthUI);
			double close = chartBars.Bars.GetClose(i);
			double open = chartBars.Bars.GetOpen(i);
			int num3 = ((Math.Abs(open - close) < chartBars.Bars.Instrument.MasterInstrument.TickSize * 0.1) ? 1 : ((int)Math.Round(Math.Abs(open - close) / num, 0) + 1));
			float num4 = chartScale.GetYByValue(close);
			float num5 = chartScale.GetYByValue(open);
			float num6 = Math.Min(num5, num4);
			float num7 = chartControl.GetXByBarIndex(chartBars, i);
			float num8 = Math.Abs(num5 - num4) + (float)num2 - (float)(int)Math.Round((double)(num2 * num3));
			if (Math.Abs(close - open) < chartBars.Bars.Instrument.MasterInstrument.TickSize * 0.1)
			{
				if (i == 0)
				{
					((ChartStyle)this).RenderTarget.DrawRectangle(new RectangleF(num7 - (float)barPaintWidth / 2f + 1f, num6 - (float)num2 / 2f + 2f, (float)(barPaintWidth - 1), (float)(num2 - 2)), ((ChartStyle)this).DownBrushDX, ((ChartStyle)this).Stroke.Width);
					val.X = num7 - (float)barPaintWidth / 2f;
					val.Y = num6 - (float)num2 / 2f;
					val2.X = num7 + (float)barPaintWidth / 2f;
					val2.Y = num6 + (float)num2 - (float)num2 / 2f;
					if (!(((ChartStyle)this).UpBrushDX is SolidColorBrush))
					{
						ChartStyle.TransformBrush(((ChartStyle)this).UpBrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke.Width, (float)barPaintWidth, ((ChartStyle)this).Stroke.Width));
					}
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, ((ChartStyle)this).UpBrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
					val.X = num7 - (float)barPaintWidth / 2f;
					val.Y = num6 + (float)num2 - (float)num2 / 2f;
					val2.X = num7 + (float)barPaintWidth / 2f;
					val2.Y = num6 - (float)num2 / 2f;
					if (!(((ChartStyle)this).UpBrushDX is SolidColorBrush))
					{
						ChartStyle.TransformBrush(((ChartStyle)this).UpBrushDX, new RectangleF(val.X, val.Y - ((ChartStyle)this).Stroke.Width, (float)barPaintWidth, ((ChartStyle)this).Stroke.Width));
					}
					((ChartStyle)this).RenderTarget.DrawLine(val, val2, ((ChartStyle)this).UpBrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
					continue;
				}
				if (!trendDetermined)
				{
					if (Math.Abs(chartBars.Bars.GetOpen(i - 1) - chartBars.Bars.GetClose(i - 1)) < chartBars.Bars.Instrument.MasterInstrument.TickSize * 0.1)
					{
						if (chartBars.Bars.GetHigh(i) < chartBars.Bars.GetHigh(i - 1))
						{
							isUp = false;
						}
					}
					else
					{
						isUp = !(chartBars.Bars.GetOpen(i - 1) < chartBars.Bars.GetClose(i - 1));
					}
					trendDetermined = true;
				}
				else
				{
					isUp = !isUp;
				}
			}
			else
			{
				isUp = close > open;
			}
			for (int j = 0; j < num3; j++)
			{
				if (num8 != 0f)
				{
					num6 += (float)((num8 > 0f) ? 1 : (-1));
					num8 += (float)((!(num8 > 0f)) ? 1 : (-1));
				}
				if (!isUp)
				{
					val3.Point = new Vector2(num7, num6);
					val3.RadiusX = (float)barPaintWidth / 2f;
					val3.RadiusY = (float)num2 / 2f - 1f;
					if (!(((ChartStyle)this).DownBrushDX is SolidColorBrush))
					{
						ChartStyle.TransformBrush(((ChartStyle)this).DownBrushDX, new RectangleF(val3.Point.X - val3.RadiusX, val3.Point.Y - val3.RadiusY - ((ChartStyle)this).Stroke.Width, (float)barPaintWidth, ((ChartStyle)this).Stroke.Width));
					}
					((ChartStyle)this).RenderTarget.DrawEllipse(val3, ((ChartStyle)this).DownBrushDX, ((ChartStyle)this).Stroke.Width);
				}
				else
				{
					val4.X = num7 - (float)barPaintWidth / 2f;
					val4.Y = num6 - (float)num2 / 2f;
					val5.X = num7 + (float)barPaintWidth / 2f;
					val5.Y = num6 + (float)num2 - (float)num2 / 2f;
					if (!(((ChartStyle)this).UpBrushDX is SolidColorBrush))
					{
						ChartStyle.TransformBrush(((ChartStyle)this).UpBrushDX, new RectangleF(val4.X, val4.Y - ((ChartStyle)this).Stroke.Width, (float)barPaintWidth, ((ChartStyle)this).Stroke.Width));
					}
					((ChartStyle)this).RenderTarget.DrawLine(val4, val5, ((ChartStyle)this).UpBrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
					val4.X = num7 - (float)barPaintWidth / 2f;
					val4.Y = num6 + (float)num2 - (float)num2 / 2f;
					val5.X = num7 + (float)barPaintWidth / 2f;
					val5.Y = num6 - (float)num2 / 2f;
					if (!(((ChartStyle)this).UpBrushDX is SolidColorBrush))
					{
						ChartStyle.TransformBrush(((ChartStyle)this).UpBrushDX, new RectangleF(val4.X, val4.Y - ((ChartStyle)this).Stroke.Width, (float)barPaintWidth, ((ChartStyle)this).Stroke.Width));
					}
					((ChartStyle)this).RenderTarget.DrawLine(val4, val5, ((ChartStyle)this).UpBrushDX, ((ChartStyle)this).Stroke.Width, ((ChartStyle)this).Stroke.StrokeStyle);
				}
				num6 += (float)num2;
			}
		}
		((ChartStyle)this).RenderTarget.AntialiasMode = antialiasMode;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptChartStylePointAndFigure;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)4;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke", ignoreCase: true));
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke2", ignoreCase: true));
			((ChartStyle)this).SetPropertyName("BarWidth", Resource.NinjaScriptChartStyleBarWidth);
			((ChartStyle)this).SetPropertyName("DownBrush", Resource.NinjaScriptChartStylePointAndFigureDownColor);
			((ChartStyle)this).SetPropertyName("UpBrush", Resource.NinjaScriptChartStylePointAndFigureUpColor);
		}
	}
}
