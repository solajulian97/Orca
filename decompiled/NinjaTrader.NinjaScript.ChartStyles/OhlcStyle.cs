using System;
using System.Collections.Generic;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class OhlcStyle : ChartStyle, ISubModeProvider
{
	private object icon;

	public override object Icon => icon ?? (icon = Icons.ChartOHLC);

	public OhlcMode Mode { get; set; }

	public override IEnumerable<object> SubModes
	{
		get
		{
			foreach (object value in Enum.GetValues(typeof(OhlcMode)))
			{
				yield return value;
			}
		}
	}

	public override int GetBarPaintWidth(int barWidth)
	{
		return 3 * barWidth;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		Bars bars = chartBars.Bars;
		float num = (float)Math.Max(1.0, ((ChartStyle)this).BarWidth);
		Vector2 val = default(Vector2);
		Vector2 val2 = default(Vector2);
		Vector2 val3 = default(Vector2);
		Vector2 val4 = default(Vector2);
		Vector2 val5 = default(Vector2);
		Vector2 val6 = default(Vector2);
		for (int i = chartBars.FromIndex; i <= chartBars.ToIndex; i++)
		{
			object obj = chartControl.GetBarOverrideBrush(chartBars, i);
			double close = bars.GetClose(i);
			double high = bars.GetHigh(i);
			double low = bars.GetLow(i);
			double open = bars.GetOpen(i);
			int yByValue = chartScale.GetYByValue(close);
			int yByValue2 = chartScale.GetYByValue(high);
			int yByValue3 = chartScale.GetYByValue(low);
			int yByValue4 = chartScale.GetYByValue(open);
			float num2 = chartControl.GetXByBarIndex(chartBars, i);
			val.X = (val2.X = num2);
			val.Y = (float)Math.Min(yByValue2, yByValue3) - num * 0.5f;
			val2.Y = (float)Math.Max(yByValue2, yByValue3) + num * 0.5f;
			if (obj == null)
			{
				obj = ((close >= open) ? ((ChartStyle)this).UpBrushDX : ((ChartStyle)this).DownBrushDX);
			}
			Brush val7 = (Brush)obj;
			if (!(val7 is SolidColorBrush))
			{
				ChartStyle.TransformBrush(val7, new RectangleF(val.X - num * 1.5f, val.Y, num * 3f, val2.Y - val.Y));
			}
			((ChartStyle)this).RenderTarget.DrawLine(val, val2, val7, num);
			if (!object.Equals(Mode, OhlcMode.HiLo))
			{
				val3.X = num2 + num * 1.5f;
				val3.Y = yByValue;
				val4.X = num2;
				val4.Y = yByValue;
				((ChartStyle)this).RenderTarget.DrawLine(val3, val4, val7, num);
				if (object.Equals(Mode, OhlcMode.OHLC))
				{
					val5.X = num2 - num * 1.5f;
					val5.Y = yByValue4;
					val6.X = num2;
					val6.Y = yByValue4;
					((ChartStyle)this).RenderTarget.DrawLine(val5, val6, val7, num);
				}
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptChartStyleOHLC;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)3;
			Mode = OhlcMode.OHLC;
			((ChartStyle)this).BarWidth = 2.0;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke", ignoreCase: true));
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke2", ignoreCase: true));
			((ChartStyle)this).SetPropertyName("BarWidth", Resource.NinjaScriptChartStyleBarWidth);
			((ChartStyle)this).SetPropertyName("UpBrush", Resource.NinjaScriptChartStyleOhlcUpBarsColor);
			((ChartStyle)this).SetPropertyName("DownBrush", Resource.NinjaScriptChartStyleOhlcDownBarsColor);
		}
	}

	public void SetSubmode(object mode)
	{
		if (mode is OhlcMode mode2)
		{
			Mode = mode2;
		}
	}
}
