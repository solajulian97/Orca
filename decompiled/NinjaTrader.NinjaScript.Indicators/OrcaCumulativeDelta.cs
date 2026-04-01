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

namespace NinjaTrader.NinjaScript.Indicators;

public class OrcaCumulativeDelta : Indicator
{
	private double lastBid;

	private double lastAsk;

	private double prevLast;

	private double runningDelta;

	private int lastPrimaryBarProcessed;

	private int lastDirection;

	private List<double> barDeltaOpen;

	private List<double> barDeltaHigh;

	private List<double> barDeltaLow;

	private List<double> barDeltaClose;

	private List<bool> barHasData;

	private Brush dxUpFillBrush;

	private Brush dxDownFillBrush;

	private Brush dxUpBorderBrush;

	private Brush dxDownBorderBrush;

	private Brush dxWickBrush;

	private Brush dxZeroBrush;

	private Brush dxPriceLineBrush;

	[XmlIgnore]
	[Display(Name = "Color Up", Order = 1, GroupName = "Visual Parameters")]
	public Brush ColorUp { get; set; }

	[Browsable(false)]
	public string ColorUpSerialize
	{
		get
		{
			return Serialize.BrushToString(ColorUp);
		}
		set
		{
			ColorUp = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Color Down", Order = 2, GroupName = "Visual Parameters")]
	public Brush ColorDown { get; set; }

	[Browsable(false)]
	public string ColorDownSerialize
	{
		get
		{
			return Serialize.BrushToString(ColorDown);
		}
		set
		{
			ColorDown = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Color Up Border", Order = 3, GroupName = "Visual Parameters")]
	public Brush ColorUpBorder { get; set; }

	[Browsable(false)]
	public string ColorUpBorderSerialize
	{
		get
		{
			return Serialize.BrushToString(ColorUpBorder);
		}
		set
		{
			ColorUpBorder = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Color Down Border", Order = 4, GroupName = "Visual Parameters")]
	public Brush ColorDownBorder { get; set; }

	[Browsable(false)]
	public string ColorDownBorderSerialize
	{
		get
		{
			return Serialize.BrushToString(ColorDownBorder);
		}
		set
		{
			ColorDownBorder = Serialize.StringToBrush(value);
		}
	}

	[Range(0.0, 1.0)]
	[Display(Name = "Bar Opacity", Order = 5, GroupName = "Visual Parameters")]
	public double BarOpacity { get; set; }

	[Range(0.0, 1.0)]
	[Display(Name = "Border Opacity", Order = 6, GroupName = "Visual Parameters")]
	public double BorderOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Wick Color", Order = 6, GroupName = "Visual Parameters")]
	public Brush WickColor { get; set; }

	[Browsable(false)]
	public string WickColorSerialize
	{
		get
		{
			return Serialize.BrushToString(WickColor);
		}
		set
		{
			WickColor = Serialize.StringToBrush(value);
		}
	}

	[Range(1, 100)]
	[Display(Name = "Bar Width %", Order = 8, GroupName = "Visual Parameters")]
	public int BarWidthPercent { get; set; }

	[Display(Name = "Show Price Line", Order = 1, GroupName = "Price Line")]
	public bool ShowPriceLine { get; set; }

	[Range(1, 5)]
	[Display(Name = "Price Line Width", Order = 2, GroupName = "Price Line")]
	public int PriceLineWidth { get; set; }

	[XmlIgnore]
	[Display(Name = "Zero Line Color", Order = 1, GroupName = "Reference Levels")]
	public Brush ZeroLineColor { get; set; }

	[Browsable(false)]
	public string ZeroLineColorSerialize
	{
		get
		{
			return Serialize.BrushToString(ZeroLineColor);
		}
		set
		{
			ZeroLineColor = Serialize.StringToBrush(value);
		}
	}

	[Range(1, 5)]
	[Display(Name = "Zero Line Width", Order = 2, GroupName = "Reference Levels")]
	public int ZeroLineWidth { get; set; }

	[Display(Name = "Delta Mode", Order = 1, GroupName = "Delta Calculation", Description = "BidAsk: classifies each trade against the bid/ask spread (most accurate live). TickDirection: classifies by whether price moved up or down tick-to-tick (works historically and live).")]
	public CumulativeDeltaMode DeltaMode { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Invalid comparison between Unknown and I4
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Invalid comparison between Unknown and I4
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "OrcaCumulativeDelta";
			((NinjaScript)this).Description = "Cumulative delta OHLC candles on a separate panel.";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).BarsRequiredToPlot = 0;
			ColorUp = Brushes.DodgerBlue;
			ColorDown = Brushes.Tomato;
			ColorUpBorder = Brushes.DodgerBlue;
			ColorDownBorder = Brushes.Tomato;
			BarOpacity = 0.5;
			BorderOpacity = 1.0;
			WickColor = Brushes.White;
			ZeroLineColor = Brushes.DimGray;
			ZeroLineWidth = 1;
			BarWidthPercent = 90;
			ShowPriceLine = true;
			PriceLineWidth = 1;
			DeltaMode = CumulativeDeltaMode.BidAsk;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DimGray, 1f), (PlotStyle)6, "DeltaClose");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DimGray, 1f), (PlotStyle)6, "DeltaHigh");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DimGray, 1f), (PlotStyle)6, "DeltaLow");
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			barDeltaOpen = new List<double>(4096);
			barDeltaHigh = new List<double>(4096);
			barDeltaLow = new List<double>(4096);
			barDeltaClose = new List<double>(4096);
			barHasData = new List<bool>(4096);
			lastBid = double.NaN;
			lastAsk = double.NaN;
			prevLast = double.NaN;
			runningDelta = 0.0;
			lastPrimaryBarProcessed = -1;
			lastDirection = 0;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			DisposeDxResources();
		}
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if ((int)e.MarketDataType == 1)
		{
			lastBid = e.Price;
		}
		else if ((int)e.MarketDataType == 0)
		{
			lastAsk = e.Price;
		}
		else if ((int)e.MarketDataType == 2)
		{
			if (e.Ask > 0.0 && !double.IsNaN(e.Ask))
			{
				lastAsk = e.Ask;
			}
			if (e.Bid > 0.0 && !double.IsNaN(e.Bid))
			{
				lastBid = e.Bid;
			}
		}
	}

	private void EnsureBarLists(int idx)
	{
		while (barDeltaOpen.Count <= idx)
		{
			barDeltaOpen.Add(0.0);
			barDeltaHigh.Add(0.0);
			barDeltaLow.Add(0.0);
			barDeltaClose.Add(0.0);
			barHasData.Add(item: false);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).BarsInProgress == 1)
		{
			double num = ((NinjaScriptBase)this).Close[0];
			long num2 = (long)((NinjaScriptBase)this).Volume[0];
			if (num2 <= 0)
			{
				return;
			}
			if ((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7)
			{
				num2 = (long)Globals.ToCryptocurrencyVolume(num2);
			}
			int bar = ((NinjaScriptBase)this).BarsArray[0].GetBar(((NinjaScriptBase)this).Time[0]);
			if (bar < 0)
			{
				return;
			}
			EnsureBarLists(bar);
			if (bar != lastPrimaryBarProcessed)
			{
				lastPrimaryBarProcessed = bar;
			}
			long num3 = 0L;
			if (DeltaMode == CumulativeDeltaMode.BidAsk && !double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0.0 && lastBid > 0.0 && lastAsk >= lastBid)
			{
				if (num >= lastAsk)
				{
					num3 = num2;
				}
				else if (num <= lastBid)
				{
					num3 = -num2;
				}
				else if (!double.IsNaN(prevLast))
				{
					num3 = ((num > prevLast) ? num2 : ((!(num < prevLast)) ? (lastDirection * num2) : (-num2)));
				}
			}
			else if (!double.IsNaN(prevLast))
			{
				num3 = ((num > prevLast) ? num2 : ((!(num < prevLast)) ? (lastDirection * num2) : (-num2)));
			}
			prevLast = num;
			if (num3 > 0)
			{
				lastDirection = 1;
			}
			else if (num3 < 0)
			{
				lastDirection = -1;
			}
			if (num3 == 0L)
			{
				return;
			}
			runningDelta += num3;
			if (!barHasData[bar])
			{
				barDeltaOpen[bar] = runningDelta;
				barDeltaHigh[bar] = runningDelta;
				barDeltaLow[bar] = runningDelta;
				barDeltaClose[bar] = runningDelta;
				barHasData[bar] = true;
				return;
			}
			barDeltaClose[bar] = runningDelta;
			if (runningDelta > barDeltaHigh[bar])
			{
				barDeltaHigh[bar] = runningDelta;
			}
			if (runningDelta < barDeltaLow[bar])
			{
				barDeltaLow[bar] = runningDelta;
			}
		}
		else if (((NinjaScriptBase)this).BarsInProgress == 0 && ((NinjaScriptBase)this).CurrentBar >= 0)
		{
			EnsureBarLists(((NinjaScriptBase)this).CurrentBar);
			if (((NinjaScriptBase)this).Bars.IsFirstBarOfSession)
			{
				runningDelta = 0.0;
				prevLast = double.NaN;
			}
			if (((NinjaScriptBase)this).CurrentBar < barDeltaClose.Count && barHasData[((NinjaScriptBase)this).CurrentBar])
			{
				((NinjaScriptBase)this).Values[0][0] = barDeltaClose[((NinjaScriptBase)this).CurrentBar];
				((NinjaScriptBase)this).Values[1][0] = barDeltaHigh[((NinjaScriptBase)this).CurrentBar];
				((NinjaScriptBase)this).Values[2][0] = barDeltaLow[((NinjaScriptBase)this).CurrentBar];
			}
			else
			{
				((NinjaScriptBase)this).Values[0][0] = double.NaN;
				((NinjaScriptBase)this).Values[1][0] = double.NaN;
				((NinjaScriptBase)this).Values[2][0] = double.NaN;
			}
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		if (chartControl == null || chartScale == null || ((NinjaScriptBase)this).Bars == null || ((IndicatorRenderBase)this).ChartBars == null || barDeltaOpen == null)
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
		if (dxUpFillBrush == null)
		{
			return;
		}
		AntialiasMode antialiasMode = ((IndicatorRenderBase)this).RenderTarget.AntialiasMode;
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)1;
		float num = ((IndicatorRenderBase)this).ChartPanel.X;
		float num2 = ((IndicatorRenderBase)this).ChartPanel.W;
		float num3 = ((IndicatorRenderBase)this).ChartPanel.Y;
		float num4 = ((IndicatorRenderBase)this).ChartPanel.H;
		float num5 = chartScale.GetYByValue(0.0);
		if (num5 >= num3 && num5 <= num3 + num4)
		{
			((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num, num5), new Vector2(num + num2, num5), dxZeroBrush, (float)ZeroLineWidth);
		}
		RectangleF val5 = default(RectangleF);
		for (int i = fromIndex; i <= toIndex; i++)
		{
			if (i >= 0 && i < barDeltaOpen.Count && barHasData[i])
			{
				double num6 = barDeltaOpen[i];
				double num7 = barDeltaHigh[i];
				double num8 = barDeltaLow[i];
				double num9 = barDeltaClose[i];
				bool num10 = num9 >= num6;
				float val = chartScale.GetYByValue(num6);
				float num11 = chartScale.GetYByValue(num7);
				float num12 = chartScale.GetYByValue(num8);
				float val2 = chartScale.GetYByValue(num9);
				float num13 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i);
				float num14 = ((i < toIndex) ? ((float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i + 1) - num13) : ((i <= fromIndex) ? ((float)chartControl.BarWidth) : (num13 - (float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i - 1))));
				float num15 = (float)((double)(num14 * (float)BarWidthPercent) / 100.0 / 2.0);
				if (num15 < 1f)
				{
					num15 = 1f;
				}
				Brush val3 = (num10 ? dxUpFillBrush : dxDownFillBrush);
				Brush val4 = (num10 ? dxUpBorderBrush : dxDownBorderBrush);
				float num16 = Math.Min(val, val2);
				float num17 = Math.Max(val, val2);
				float num18 = num17 - num16;
				if (num18 < 1f)
				{
					num18 = 1f;
				}
				if (num11 < num16)
				{
					((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num13, num11), new Vector2(num13, num16), val4, 1f);
				}
				if (num12 > num17)
				{
					((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num13, num17), new Vector2(num13, num12), val4, 1f);
				}
				((RectangleF)(ref val5))._002Ector(num13 - num15, num16, num15 * 2f, num18);
				((IndicatorRenderBase)this).RenderTarget.FillRectangle(val5, val3);
				((IndicatorRenderBase)this).RenderTarget.DrawRectangle(val5, val4, 1f);
			}
		}
		if (ShowPriceLine)
		{
			int num19 = toIndex;
			while (num19 >= fromIndex && (num19 >= barDeltaClose.Count || !barHasData[num19]))
			{
				num19--;
			}
			if (num19 >= fromIndex)
			{
				double num20 = barDeltaClose[num19];
				double num21 = barDeltaOpen[num19];
				Brush val6 = ((num20 >= num21) ? dxUpBorderBrush : dxDownBorderBrush);
				float num22 = chartScale.GetYByValue(num20);
				float num23 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, num19);
				float num24 = num + num2;
				if (num22 >= num3 && num22 <= num3 + num4 && num23 < num24)
				{
					((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num23, num22), new Vector2(num24, num22), val6, (float)PriceLineWidth);
				}
			}
		}
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = antialiasMode;
	}

	private void EnsureDxResources()
	{
		if (((IndicatorRenderBase)this).RenderTarget != null && dxUpFillBrush == null)
		{
			float opacity = (float)Math.Max(0.0, Math.Min(1.0, BarOpacity));
			float opacity2 = (float)Math.Max(0.0, Math.Min(1.0, BorderOpacity));
			dxUpFillBrush = DxExtensions.ToDxBrush(ColorUp, ((IndicatorRenderBase)this).RenderTarget);
			dxUpFillBrush.Opacity = opacity;
			dxDownFillBrush = DxExtensions.ToDxBrush(ColorDown, ((IndicatorRenderBase)this).RenderTarget);
			dxDownFillBrush.Opacity = opacity;
			dxUpBorderBrush = DxExtensions.ToDxBrush(ColorUpBorder, ((IndicatorRenderBase)this).RenderTarget);
			dxUpBorderBrush.Opacity = opacity2;
			dxDownBorderBrush = DxExtensions.ToDxBrush(ColorDownBorder, ((IndicatorRenderBase)this).RenderTarget);
			dxDownBorderBrush.Opacity = opacity2;
			dxWickBrush = DxExtensions.ToDxBrush(WickColor, ((IndicatorRenderBase)this).RenderTarget);
			dxWickBrush.Opacity = opacity2;
			dxZeroBrush = DxExtensions.ToDxBrush(ZeroLineColor, ((IndicatorRenderBase)this).RenderTarget);
			dxPriceLineBrush = DxExtensions.ToDxBrush((Brush)Brushes.White, ((IndicatorRenderBase)this).RenderTarget);
		}
	}

	private void DisposeDxResources()
	{
		if (dxUpFillBrush != null)
		{
			((DisposeBase)dxUpFillBrush).Dispose();
			dxUpFillBrush = null;
		}
		if (dxDownFillBrush != null)
		{
			((DisposeBase)dxDownFillBrush).Dispose();
			dxDownFillBrush = null;
		}
		if (dxUpBorderBrush != null)
		{
			((DisposeBase)dxUpBorderBrush).Dispose();
			dxUpBorderBrush = null;
		}
		if (dxDownBorderBrush != null)
		{
			((DisposeBase)dxDownBorderBrush).Dispose();
			dxDownBorderBrush = null;
		}
		if (dxWickBrush != null)
		{
			((DisposeBase)dxWickBrush).Dispose();
			dxWickBrush = null;
		}
		if (dxZeroBrush != null)
		{
			((DisposeBase)dxZeroBrush).Dispose();
			dxZeroBrush = null;
		}
		if (dxPriceLineBrush != null)
		{
			((DisposeBase)dxPriceLineBrush).Dispose();
			dxPriceLineBrush = null;
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDxResources();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}
}
