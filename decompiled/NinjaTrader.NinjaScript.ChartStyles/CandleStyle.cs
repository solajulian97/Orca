using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NTRes.NinjaTrader.Gui.Chart;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class CandleStyle : ChartStyle
{
	private Brush dojiBrush;

	private Brush dojiBrushDX;

	private object icon;

	public override object Icon => icon ?? (icon = Icons.ChartChartStyle);

	[RefreshProperties(RefreshProperties.All)]
	[Display(ResourceType = typeof(ChartResources), Name = "GuiChartStyleWickMatchesBody", Order = 2)]
	[DefaultIfMissing(false)]
	public bool WickMatchesBody { get; set; } = true;

	[Display(ResourceType = typeof(ChartResources), Name = "GuiChartStyleDojiBrush", Order = 5)]
	[XmlIgnore]
	public Brush DojiBrush
	{
		get
		{
			return dojiBrush ?? (dojiBrush = (Application.Current.FindResource("ChartControl.Stroke") as Pen)?.Brush);
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
			Brush obj = dojiBrushDX;
			if (obj == null || ((DisposeBase)obj).IsDisposed)
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

	[Display(ResourceType = typeof(ChartResources), Name = "GuiChartStyleWickStyle", Order = 6)]
	public DashStyleHelper WickStyle { get; set; }

	[Display(ResourceType = typeof(ChartResources), Name = "GuiChartStyleWickWidth", Order = 7)]
	public int WickWidth { get; set; } = 1;

	public override int GetBarPaintWidth(int barWidth)
	{
		return 1 + 2 * (barWidth - 1) + 2 * (int)Math.Round(((ChartStyle)this).Stroke.Width);
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		Bars bars = chartBars.Bars;
		float num = ((ChartStyle)this).GetBarPaintWidth(((ChartStyle)this).BarWidthUI);
		Vector2 val = default(Vector2);
		Vector2 val2 = default(Vector2);
		RectangleF val3 = default(RectangleF);
		Stroke val4 = (Stroke)((!WickMatchesBody) ? ((object)((ChartStyle)this).Stroke) : ((object)new Stroke(((ChartStyle)this).Stroke.Brush, WickStyle, (float)WickWidth)));
		Stroke val5 = (Stroke)((!WickMatchesBody) ? ((object)((ChartStyle)this).Stroke2) : ((object)new Stroke(((ChartStyle)this).Stroke2.Brush, WickStyle, (float)WickWidth)));
		for (int i = chartBars.FromIndex; i <= chartBars.ToIndex; i++)
		{
			Brush barOverrideBrush = chartControl.GetBarOverrideBrush(chartBars, i);
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
			bool flag = Math.Abs(open - close) < 1E-10;
			if (flag)
			{
				val.X = (float)xByBarIndex - num * 0.5f;
				val.Y = yByValue;
				val2.X = (float)xByBarIndex + num * 0.5f;
				val2.Y = yByValue;
				Brush val6 = candleOutlineOverrideBrush ?? (WickMatchesBody ? DojiBrushDX : val4.BrushDX);
				if (!(val6 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val6, new RectangleF(val.X, val.Y - val4.Width, num, val4.Width));
				}
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, val6, val4.Width, val4.StrokeStyle);
			}
			else
			{
				((RectangleF)(ref val3)).X = (float)xByBarIndex - num * 0.5f + 0.5f;
				((RectangleF)(ref val3)).Y = Math.Min(yByValue, yByValue4);
				((RectangleF)(ref val3)).Width = num - 1f;
				((RectangleF)(ref val3)).Height = Math.Max(yByValue4, yByValue) - Math.Min(yByValue, yByValue4);
				Brush val7 = barOverrideBrush ?? ((close >= open) ? ((ChartStyle)this).UpBrushDX : ((ChartStyle)this).DownBrushDX);
				if (!(val7 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val7, val3);
				}
				((ChartStyle)this).RenderTarget.FillRectangle(val3, val7);
				Brush val8 = candleOutlineOverrideBrush ?? ((!WickMatchesBody) ? val4.BrushDX : ((close >= open) ? ((ChartStyle)this).UpBrushDX : ((ChartStyle)this).DownBrushDX));
				if (!(val8 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val8, val3);
				}
				((ChartStyle)this).RenderTarget.DrawRectangle(val3, val8 ?? val4.BrushDX, val4.Width, val4.StrokeStyle);
			}
			Brush val9 = candleOutlineOverrideBrush ?? ((!WickMatchesBody) ? val5.BrushDX : (flag ? DojiBrushDX : ((close >= open) ? ((ChartStyle)this).UpBrushDX : ((ChartStyle)this).DownBrushDX)));
			if (high > Math.Max(open, close))
			{
				val.X = xByBarIndex;
				val.Y = yByValue2;
				val2.X = xByBarIndex;
				val2.Y = ((open > close) ? yByValue4 : yByValue);
				if (!(val9 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val9, new RectangleF(val.X - val5.Width, val.Y, val5.Width, val2.Y - val.Y));
				}
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, val9, val5.Width, val5.StrokeStyle);
			}
			if (low < Math.Min(open, close))
			{
				val.X = xByBarIndex;
				val.Y = yByValue3;
				val2.X = xByBarIndex;
				val2.Y = ((open < close) ? yByValue4 : yByValue);
				if (!(val9 is SolidColorBrush))
				{
					ChartStyle.TransformBrush(val9, new RectangleF(val2.X - val5.Width, val2.Y, val5.Width, val.Y - val2.Y));
				}
				((ChartStyle)this).RenderTarget.DrawLine(val, val2, val9, val5.Width, val5.StrokeStyle);
			}
		}
	}

	public override void OnRenderTargetChanged()
	{
		((ChartStyle)this).OnRenderTargetChanged();
		Brush obj = dojiBrushDX;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		dojiBrushDX = null;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptChartStyleCandlestick;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)1;
			DojiBrush = (Application.Current.FindResource("ChartControl.Stroke") as Pen)?.Brush;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).SetPropertyName("BarWidth", Resource.NinjaScriptChartStyleBarWidth);
			((ChartStyle)this).SetPropertyName("DownBrush", Resource.NinjaScriptChartStyleCandleDownBarsColor);
			((ChartStyle)this).SetPropertyName("UpBrush", Resource.NinjaScriptChartStyleCandleUpBarsColor);
			((ChartStyle)this).SetPropertyName("Stroke", Resource.NinjaScriptChartStyleCandleOutline);
			((ChartStyle)this).SetPropertyName("Stroke2", Resource.NinjaScriptChartStyleCandleWick);
			((ChartStyle)this).SetPropertyOrder("BarWidth", 1);
			((ChartStyle)this).SetPropertyOrder("UpBrush", 3);
			((ChartStyle)this).SetPropertyOrder("DownBrush", 4);
			((ChartStyle)this).SetPropertyOrder("Stroke", 5);
			((ChartStyle)this).SetPropertyOrder("Stroke2", 6);
		}
	}

	public override object Clone()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		CandleStyle candleStyle = ((ChartStyle)this).Clone() as CandleStyle;
		if (candleStyle != null)
		{
			candleStyle.WickMatchesBody = WickMatchesBody;
			candleStyle.DojiBrush = DojiBrush?.Clone();
			candleStyle.WickStyle = WickStyle;
			candleStyle.WickWidth = WickWidth;
		}
		return candleStyle ?? new CandleStyle();
	}
}
