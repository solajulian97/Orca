using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class HollowCandleStyle : ChartStyle
{
	private object icon;

	private Brush dojiBrush;

	private Brush dojiBrushDX;

	public override object Icon => icon ?? (icon = Icons.ChartChartStyleHollow);

	[Range(1, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptChartStyleLineWidth", GroupName = "NinjaScriptGeneral")]
	public int LineWidth { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "GuiChartStyleDojiBrush", GroupName = "NinjaScriptGeneral")]
	[XmlIgnore]
	public Brush DojiBrush
	{
		get
		{
			return dojiBrush ?? (dojiBrush = Brushes.DimGray);
		}
		set
		{
			dojiBrush = value;
			Brush brush = dojiBrush;
			if (brush != null && brush.CanFreeze)
			{
				dojiBrush.Freeze();
			}
			dojiBrushDX = null;
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	[CLSCompliant(false)]
	public Brush DojiBrushDX
	{
		get
		{
			if (dojiBrushDX == null || ((DisposeBase)dojiBrushDX).IsDisposed)
			{
				dojiBrushDX = DxExtensions.ToDxBrush(DojiBrush, ((ChartStyle)this).RenderTarget);
			}
			return dojiBrushDX;
		}
	}

	[Browsable(false)]
	public string DojiBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(DojiBrush);
		}
		set
		{
			DojiBrush = Serialize.StringToBrush(value);
		}
	}

	public override int GetBarPaintWidth(int barWidth)
	{
		return 1 + 2 * (barWidth - 1) + 2 * (int)Math.Round(((ChartStyle)this).Stroke.Width);
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		Bars bars = chartBars.Bars;
		float num = ((ChartStyle)this).GetBarPaintWidth(((ChartStyle)this).BarWidthUI);
		Vector2 val = default(Vector2);
		Vector2 val2 = default(Vector2);
		RectangleF val3 = default(RectangleF);
		for (int i = chartBars.FromIndex; i <= chartBars.ToIndex; i++)
		{
			Brush candleOutlineOverrideBrush = chartControl.GetCandleOutlineOverrideBrush(chartBars, i);
			double close = bars.GetClose(i);
			double high = bars.GetHigh(i);
			double low = bars.GetLow(i);
			double open = bars.GetOpen(i);
			int yByValue = chartScale.GetYByValue(close);
			int yByValue2 = chartScale.GetYByValue(high);
			int yByValue3 = chartScale.GetYByValue(low);
			int yByValue4 = chartScale.GetYByValue(open);
			int xByBarIndex = chartControl.GetXByBarIndex(chartBars, i);
			Brush val4 = candleOutlineOverrideBrush ?? ((close > open) ? ((ChartStyle)this).UpBrushDX : ((close < open) ? ((ChartStyle)this).DownBrushDX : DojiBrushDX));
			if ((double)Math.Abs(yByValue4 - yByValue) < 1E-07)
			{
				val.X = (float)xByBarIndex - num * 0.5f;
				val.Y = yByValue;
				val2.X = (float)xByBarIndex + num * 0.5f;
				val2.Y = yByValue;
				if (!(val4 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(candleOutlineOverrideBrush ?? DojiBrushDX, new RectangleF(val.X, val.Y - (float)LineWidth, num, (float)LineWidth));
				}
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, val4, (float)LineWidth);
			}
			else
			{
				((RectangleF)(ref val3)).X = (float)xByBarIndex - num * 0.5f + 0.5f;
				((RectangleF)(ref val3)).Y = Math.Min(yByValue, yByValue4);
				((RectangleF)(ref val3)).Width = num - 1f;
				((RectangleF)(ref val3)).Height = Math.Max(yByValue4, yByValue) - Math.Min(yByValue, yByValue4);
				if (!(val4 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val4, val3);
				}
				((ChartStyle)this).RenderTarget.DrawRectangle(val3, val4, (float)LineWidth);
				if (((ChartObject)chartBars).IsInHitTest)
				{
					((ChartStyle)this).RenderTarget.FillRectangle(val3, chartControl.SelectionBrush);
				}
			}
			if (high > Math.Max(open, close))
			{
				val.X = xByBarIndex;
				val.Y = yByValue2;
				val2.X = xByBarIndex;
				val2.Y = ((open > close) ? yByValue4 : yByValue);
				if (!(val4 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val4, new RectangleF(val.X - ((ChartStyle)this).Stroke2.Width, val.Y, (float)LineWidth, val2.Y - val.Y));
				}
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, val4, (float)LineWidth);
			}
			if (low < Math.Min(open, close))
			{
				val.X = xByBarIndex;
				val.Y = yByValue3;
				val2.X = xByBarIndex;
				val2.Y = ((open < close) ? yByValue4 : yByValue);
				if (!(val4 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val4, new RectangleF(val2.X - ((ChartStyle)this).Stroke2.Width, val2.Y, (float)LineWidth, val.Y - val2.Y));
				}
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, val4, (float)LineWidth);
			}
		}
	}

	public override void OnRenderTargetChanged()
	{
		Brush obj = dojiBrushDX;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		dojiBrushDX = null;
		((ChartStyle)this).OnRenderTargetChanged();
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptChartStyleCandlestickHollow;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)9;
			LineWidth = 1;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).SetPropertyName("BarWidth", Resource.NinjaScriptChartStyleBarWidth);
			((ChartStyle)this).SetPropertyName("DownBrush", Resource.NinjaScriptChartStyleCandleDownBarsColor);
			((ChartStyle)this).SetPropertyName("UpBrush", Resource.NinjaScriptChartStyleCandleUpBarsColor);
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke", ignoreCase: true));
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke2", ignoreCase: true));
		}
	}
}
