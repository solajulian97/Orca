using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators;

public class OrcaTimeStatistics : Indicator
{
	private double lastBid;

	private double lastAsk;

	private double prevLast;

	private int lastDirection;

	private List<double> barTickDelta;

	private List<bool> barHasData;

	private Brush dxVolumeBrush;

	private Brush dxPositiveBrush;

	private Brush dxNegativeBrush;

	private Brush dxEffPosBrush;

	private Brush dxEffNegBrush;

	private Brush dxTextBrush;

	private TextFormat dxTextFormat;

	private Factory dwFactory;

	[Display(Name = "Show Volume", Order = 1, GroupName = "Rows")]
	public bool ShowVolume { get; set; }

	[Display(Name = "Show Delta", Order = 2, GroupName = "Rows")]
	public bool ShowDelta { get; set; }

	[Display(Name = "Show Delta Efficiency", Order = 3, GroupName = "Rows", Description = "Delta / Range (ticks). Measures net delta per tick of price range.")]
	public bool ShowDeltaEfficiency { get; set; }

	[XmlIgnore]
	[Display(Name = "1. Volume Color", Order = 1, GroupName = "Visual")]
	public Brush VolumeColor { get; set; }

	[Browsable(false)]
	public string VolumeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(VolumeColor);
		}
		set
		{
			VolumeColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "2. Positive Delta Color", Order = 2, GroupName = "Visual")]
	public Brush PositiveDeltaColor { get; set; }

	[Browsable(false)]
	public string PositiveDeltaColorSerialize
	{
		get
		{
			return Serialize.BrushToString(PositiveDeltaColor);
		}
		set
		{
			PositiveDeltaColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "3. Negative Delta Color", Order = 3, GroupName = "Visual")]
	public Brush NegativeDeltaColor { get; set; }

	[Browsable(false)]
	public string NegativeDeltaColorSerialize
	{
		get
		{
			return Serialize.BrushToString(NegativeDeltaColor);
		}
		set
		{
			NegativeDeltaColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "4. Efficiency (+) Color", Order = 4, GroupName = "Visual", Description = "Color for bullish Delta Efficiency bars.")]
	public Brush EfficiencyPosColor { get; set; }

	[Browsable(false)]
	public string EfficiencyPosColorSerialize
	{
		get
		{
			return Serialize.BrushToString(EfficiencyPosColor);
		}
		set
		{
			EfficiencyPosColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "5. Efficiency (-) Color", Order = 5, GroupName = "Visual", Description = "Color for bearish Delta Efficiency bars.")]
	public Brush EfficiencyNegColor { get; set; }

	[Browsable(false)]
	public string EfficiencyNegColorSerialize
	{
		get
		{
			return Serialize.BrushToString(EfficiencyNegColor);
		}
		set
		{
			EfficiencyNegColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "6. Text Color", Order = 6, GroupName = "Visual")]
	public Brush TextColor { get; set; }

	[Browsable(false)]
	public string TextColorSerialize
	{
		get
		{
			return Serialize.BrushToString(TextColor);
		}
		set
		{
			TextColor = Serialize.StringToBrush(value);
		}
	}

	[Range(0.0, 1.0)]
	[Display(Name = "7. Base Opacity", Order = 7, GroupName = "Visual", Description = "Minimum opacity for lowest-intensity values.")]
	public double BaseOpacity { get; set; }

	[Range(6, 24)]
	[Display(Name = "8. Font Size", Order = 8, GroupName = "Visual")]
	public int FontSize { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Invalid comparison between Unknown and I4
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Invalid comparison between Unknown and I4
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "OrcaTimeStatistics";
			((NinjaScript)this).Description = "Displays Time Statistics (Volume, Delta, Delta Efficiency) at the bottom of the chart.";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = false;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).BarsRequiredToPlot = 0;
			VolumeColor = Brushes.SkyBlue;
			PositiveDeltaColor = Brushes.LimeGreen;
			NegativeDeltaColor = Brushes.Crimson;
			EfficiencyPosColor = Brushes.MediumOrchid;
			EfficiencyNegColor = Brushes.OrangeRed;
			TextColor = Brushes.WhiteSmoke;
			BaseOpacity = 0.25;
			FontSize = 11;
			ShowVolume = true;
			ShowDelta = true;
			ShowDeltaEfficiency = true;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Transparent, 1f), (PlotStyle)6, "TimeStatsDummy");
		}
		else if ((int)((NinjaScript)this).State != 2)
		{
			if ((int)((NinjaScript)this).State == 4)
			{
				barTickDelta = new List<double>(4096);
				barHasData = new List<bool>(4096);
				lastBid = double.NaN;
				lastAsk = double.NaN;
				prevLast = double.NaN;
				lastDirection = 0;
			}
			else if ((int)((NinjaScript)this).State == 8)
			{
				DisposeDxResources();
			}
		}
	}

	private void EnsureBarLists(int idx)
	{
		while (barTickDelta.Count <= idx)
		{
			barTickDelta.Add(0.0);
			barHasData.Add(item: false);
		}
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Invalid comparison between Unknown and I4
		if ((int)e.MarketDataType == 1)
		{
			lastBid = e.Price;
		}
		else if ((int)e.MarketDataType == 0)
		{
			lastAsk = e.Price;
		}
		else
		{
			if ((int)e.MarketDataType != 2)
			{
				return;
			}
			if (e.Ask > 0.0 && !double.IsNaN(e.Ask))
			{
				lastAsk = e.Ask;
			}
			if (e.Bid > 0.0 && !double.IsNaN(e.Bid))
			{
				lastBid = e.Bid;
			}
			long num = e.Volume;
			if ((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7)
			{
				num = (long)Globals.ToCryptocurrencyVolume(num);
			}
			long num2 = 0L;
			if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0.0 && lastBid > 0.0 && lastAsk >= lastBid)
			{
				if (e.Price >= lastAsk)
				{
					num2 = num;
				}
				else if (e.Price <= lastBid)
				{
					num2 = -num;
				}
				else if (!double.IsNaN(prevLast))
				{
					num2 = ((e.Price > prevLast) ? num : ((!(e.Price < prevLast)) ? (lastDirection * num) : (-num)));
				}
			}
			else if (!double.IsNaN(prevLast))
			{
				num2 = ((e.Price > prevLast) ? num : ((!(e.Price < prevLast)) ? (lastDirection * num) : (-num)));
			}
			if (num2 > 0)
			{
				lastDirection = 1;
			}
			else if (num2 < 0)
			{
				lastDirection = -1;
			}
			prevLast = e.Price;
			if (num2 != 0L && ((NinjaScriptBase)this).BarsArray[0].Count > 0)
			{
				int bar = ((NinjaScriptBase)this).BarsArray[0].GetBar(e.Time);
				if (bar >= 0)
				{
					EnsureBarLists(bar);
					barTickDelta[bar] += num2;
					barHasData[bar] = true;
				}
			}
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).BarsInProgress == 0)
		{
			EnsureBarLists(((NinjaScriptBase)this).CurrentBar);
			if (((NinjaScriptBase)this).Bars.IsFirstBarOfSession)
			{
				lastBid = double.NaN;
				lastAsk = double.NaN;
				prevLast = double.NaN;
			}
			if (((NinjaScriptBase)this).CurrentBar < barHasData.Count && barHasData[((NinjaScriptBase)this).CurrentBar])
			{
				((NinjaScriptBase)this).Value[0] = barTickDelta[((NinjaScriptBase)this).CurrentBar];
			}
			else
			{
				((NinjaScriptBase)this).Value[0] = double.NaN;
			}
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_061e: Unknown result type (might be due to invalid IL or missing references)
		//IL_062b: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04db: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0595: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		if (chartControl == null || chartScale == null || ((NinjaScriptBase)this).Bars == null || ((IndicatorRenderBase)this).ChartBars == null)
		{
			return;
		}
		int num = 0;
		if (ShowVolume)
		{
			num++;
		}
		if (ShowDelta)
		{
			num++;
		}
		if (ShowDeltaEfficiency)
		{
			num++;
		}
		if (num == 0)
		{
			return;
		}
		int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
		int toIndex = ((IndicatorRenderBase)this).ChartBars.ToIndex;
		if (fromIndex < 0 || toIndex < 0 || fromIndex > toIndex)
		{
			return;
		}
		EnsureDxResources();
		if (dxVolumeBrush == null)
		{
			return;
		}
		float num2 = ((IndicatorRenderBase)this).ChartPanel.X;
		float num3 = ((IndicatorRenderBase)this).ChartPanel.W;
		float num4 = ((IndicatorRenderBase)this).ChartPanel.Y;
		float num5 = (float)((IndicatorRenderBase)this).ChartPanel.H / (float)num;
		List<(string, int)> list = new List<(string, int)>();
		if (ShowVolume)
		{
			list.Add(("Volume", 0));
		}
		if (ShowDelta)
		{
			list.Add(("Delta", 1));
		}
		if (ShowDeltaEfficiency)
		{
			list.Add(("Δ Efficiency", 2));
		}
		double num6 = 0.0;
		double num7 = 0.0;
		double num8 = 0.0;
		double num9 = ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
		if (num9 <= 0.0)
		{
			num9 = 0.25;
		}
		for (int i = fromIndex; i <= toIndex; i++)
		{
			if (i >= ((NinjaScriptBase)this).Bars.Count)
			{
				continue;
			}
			if (ShowVolume)
			{
				double num10 = ((NinjaScriptBase)this).Bars.GetVolume(i);
				if (num10 > num6)
				{
					num6 = num10;
				}
			}
			if (i >= barTickDelta.Count || !barHasData[i])
			{
				continue;
			}
			if (ShowDelta)
			{
				double num11 = Math.Abs(barTickDelta[i]);
				if (num11 > num7)
				{
					num7 = num11;
				}
			}
			if (!ShowDeltaEfficiency)
			{
				continue;
			}
			double value = barTickDelta[i];
			double num12 = (((NinjaScriptBase)this).Bars.GetHigh(i) - ((NinjaScriptBase)this).Bars.GetLow(i)) / num9;
			if (num12 > 0.0)
			{
				double num13 = Math.Abs(value) / num12;
				if (num13 > num8)
				{
					num8 = num13;
				}
			}
		}
		if (num6 == 0.0)
		{
			num6 = 1.0;
		}
		if (num7 == 0.0)
		{
			num7 = 1.0;
		}
		if (num8 == 0.0)
		{
			num8 = 1.0;
		}
		AntialiasMode antialiasMode = ((IndicatorRenderBase)this).RenderTarget.AntialiasMode;
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)1;
		TextAntialiasMode textAntialiasMode = ((IndicatorRenderBase)this).RenderTarget.TextAntialiasMode;
		((IndicatorRenderBase)this).RenderTarget.TextAntialiasMode = (TextAntialiasMode)1;
		RectangleF val3 = default(RectangleF);
		RectangleF val5 = default(RectangleF);
		RectangleF val2 = default(RectangleF);
		for (int j = fromIndex; j <= toIndex; j++)
		{
			if (j >= ((NinjaScriptBase)this).Bars.Count)
			{
				continue;
			}
			bool flag = j < barTickDelta.Count && barHasData[j];
			float num14 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, j);
			float num15 = GetBarSpacing(chartControl, j, fromIndex, toIndex) * 0.9f;
			if (num15 < 2f)
			{
				num15 = 2f;
			}
			double num16 = ((NinjaScriptBase)this).Bars.GetVolume(j);
			double num17 = (flag ? barTickDelta[j] : 0.0);
			double num18 = (((NinjaScriptBase)this).Bars.GetHigh(j) - ((NinjaScriptBase)this).Bars.GetLow(j)) / num9;
			double num19 = ((flag && num18 > 0.0) ? (num17 / num18) : 0.0);
			for (int k = 0; k < list.Count; k++)
			{
				float num20 = num4 + (float)k * num5;
				switch (list[k].Item2)
				{
				case 0:
				{
					double num22 = num16 / num6;
					float opacity2 = (float)(BaseOpacity + (1.0 - BaseOpacity) * num22);
					dxVolumeBrush.Opacity = opacity2;
					((RectangleF)(ref val3))._002Ector(num14 - num15 / 2f, num20 + 1f, num15, num5 - 2f);
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(val3, dxVolumeBrush);
					if (num15 >= 20f)
					{
						DrawCenteredText(FormatVolume(num16), val3);
					}
					break;
				}
				case 1:
					if (flag)
					{
						double num23 = Math.Abs(num17) / num7;
						float opacity3 = (float)(BaseOpacity + (1.0 - BaseOpacity) * num23);
						Brush val4 = ((num17 >= 0.0) ? dxPositiveBrush : dxNegativeBrush);
						val4.Opacity = opacity3;
						((RectangleF)(ref val5))._002Ector(num14 - num15 / 2f, num20 + 1f, num15, num5 - 2f);
						((IndicatorRenderBase)this).RenderTarget.FillRectangle(val5, val4);
						if (num15 >= 20f)
						{
							DrawCenteredText(FormatDelta(num17), val5);
						}
					}
					break;
				case 2:
					if (flag && !(num18 <= 0.0))
					{
						double num21 = Math.Abs(num19) / num8;
						float opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * num21);
						Brush val = ((num19 >= 0.0) ? dxEffPosBrush : dxEffNegBrush);
						val.Opacity = opacity;
						((RectangleF)(ref val2))._002Ector(num14 - num15 / 2f, num20 + 1f, num15, num5 - 2f);
						((IndicatorRenderBase)this).RenderTarget.FillRectangle(val2, val);
						if (num15 >= 20f)
						{
							DrawCenteredText(FormatEfficiency(num19), val2);
						}
					}
					break;
				}
			}
		}
		for (int l = 0; l < list.Count; l++)
		{
			DrawRightLabel(list[l].Item1, num2 + num3 - 5f, num4 + (float)l * num5, num5);
		}
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = antialiasMode;
		((IndicatorRenderBase)this).RenderTarget.TextAntialiasMode = textAntialiasMode;
	}

	private float GetBarSpacing(ChartControl chartControl, int barIdx, int fromIdx, int toIdx)
	{
		float num = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, barIdx);
		if (barIdx < toIdx)
		{
			return (float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, barIdx + 1) - num;
		}
		if (barIdx > fromIdx)
		{
			return num - (float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, barIdx - 1);
		}
		return (float)chartControl.BarWidth;
	}

	private string FormatVolume(double vol)
	{
		if (vol >= 1000.0)
		{
			return (vol / 1000.0).ToString("0.##") + "K";
		}
		return vol.ToString("0.##");
	}

	private string FormatDelta(double delta)
	{
		return delta.ToString("#,##0");
	}

	private string FormatEfficiency(double eff)
	{
		return eff.ToString("+0.00;-0.00;0.00");
	}

	private void DrawCenteredText(string text, RectangleF rect)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (dxTextFormat == null || dxTextBrush == null)
		{
			return;
		}
		TextLayout val = new TextLayout(dwFactory, text, dxTextFormat, ((RectangleF)(ref rect)).Width, ((RectangleF)(ref rect)).Height);
		try
		{
			((TextFormat)val).TextAlignment = (TextAlignment)2;
			((TextFormat)val).ParagraphAlignment = (ParagraphAlignment)2;
			((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(new Vector2(((RectangleF)(ref rect)).X, ((RectangleF)(ref rect)).Y), val, dxTextBrush);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void DrawRightLabel(string text, float x, float y, float h)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (dxTextFormat == null || dxTextBrush == null)
		{
			return;
		}
		TextLayout val = new TextLayout(dwFactory, text, dxTextFormat, 100f, h);
		try
		{
			((TextFormat)val).TextAlignment = (TextAlignment)1;
			((TextFormat)val).ParagraphAlignment = (ParagraphAlignment)2;
			((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(new Vector2(x - 100f, y), val, dxTextBrush);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void EnsureDxResources()
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		if (((IndicatorRenderBase)this).RenderTarget != null && dxVolumeBrush == null)
		{
			dxVolumeBrush = DxExtensions.ToDxBrush(VolumeColor, ((IndicatorRenderBase)this).RenderTarget);
			dxPositiveBrush = DxExtensions.ToDxBrush(PositiveDeltaColor, ((IndicatorRenderBase)this).RenderTarget);
			dxNegativeBrush = DxExtensions.ToDxBrush(NegativeDeltaColor, ((IndicatorRenderBase)this).RenderTarget);
			dxEffPosBrush = DxExtensions.ToDxBrush(EfficiencyPosColor, ((IndicatorRenderBase)this).RenderTarget);
			dxEffNegBrush = DxExtensions.ToDxBrush(EfficiencyNegColor, ((IndicatorRenderBase)this).RenderTarget);
			dxTextBrush = DxExtensions.ToDxBrush(TextColor, ((IndicatorRenderBase)this).RenderTarget);
			dwFactory = new Factory();
			dxTextFormat = new TextFormat(dwFactory, "Segoe UI", (FontWeight)700, (FontStyle)0, (float)FontSize);
		}
	}

	private void DisposeDxResources()
	{
		if (dxVolumeBrush != null)
		{
			((DisposeBase)dxVolumeBrush).Dispose();
			dxVolumeBrush = null;
		}
		if (dxPositiveBrush != null)
		{
			((DisposeBase)dxPositiveBrush).Dispose();
			dxPositiveBrush = null;
		}
		if (dxNegativeBrush != null)
		{
			((DisposeBase)dxNegativeBrush).Dispose();
			dxNegativeBrush = null;
		}
		if (dxEffPosBrush != null)
		{
			((DisposeBase)dxEffPosBrush).Dispose();
			dxEffPosBrush = null;
		}
		if (dxEffNegBrush != null)
		{
			((DisposeBase)dxEffNegBrush).Dispose();
			dxEffNegBrush = null;
		}
		if (dxTextBrush != null)
		{
			((DisposeBase)dxTextBrush).Dispose();
			dxTextBrush = null;
		}
		if (dxTextFormat != null)
		{
			((DisposeBase)dxTextFormat).Dispose();
			dxTextFormat = null;
		}
		if (dwFactory != null)
		{
			((DisposeBase)dwFactory).Dispose();
			dwFactory = null;
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDxResources();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}
}
