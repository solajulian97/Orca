using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

public class OrcaAbsorptionCandles : Indicator
{
	private double lastBid;

	private double lastAsk;

	private double prevLast;

	private int lastDirection;

	private List<double> barTickDelta;

	private List<bool> barHasData;

	private List<double> barSyntheticDelta;

	private Brush[] positiveBrushes;

	private Brush[] negativeBrushes;

	private const int NUM_BRUSHES = 20;

	[XmlIgnore]
	[Display(Name = "1. Positive Delta Color", Order = 1, GroupName = "1. Visuals")]
	public Brush PositiveColor { get; set; }

	[Browsable(false)]
	public string PositiveColorSerialize
	{
		get
		{
			return Serialize.BrushToString(PositiveColor);
		}
		set
		{
			PositiveColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "2. Negative Delta Color", Order = 2, GroupName = "1. Visuals")]
	public Brush NegativeColor { get; set; }

	[Browsable(false)]
	public string NegativeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(NegativeColor);
		}
		set
		{
			NegativeColor = Serialize.StringToBrush(value);
		}
	}

	[Range(0.0, 1.0)]
	[Display(Name = "3. Base Opacity", Order = 3, GroupName = "1. Visuals", Description = "Minimum opacity for lowest intensity values.")]
	public double BaseOpacity { get; set; }

	[Range(1, int.MaxValue)]
	[Display(Name = "1. Intensity Lookback", Order = 1, GroupName = "2. Parameters", Description = "Number of bars to look back for calculating max delta intensity.")]
	public int IntensityLookback { get; set; }

	[Display(Name = "2. Delta Calculation Mode", Order = 2, GroupName = "2. Parameters", Description = "Choose whether delta calculates via real Bid/Ask spread hits, or simple Up/Down tick direction.")]
	public DeltaCalculationMode DeltaMode { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "3. Show Historical Color", Order = 3, GroupName = "2. Parameters", Description = "Paint historical bars using synthetic delta ((Close-Open)/Range × Volume) when real tick data is unavailable.")]
	public bool ShowHistoricalColor { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Invalid comparison between Unknown and I4
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "OrcaAbsorptionCandles";
			((NinjaScript)this).Description = "Paints standard candlesticks based on volume delta intensity (absorption).";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).BarsRequiredToPlot = 0;
			((IndicatorBase)this).PaintPriceMarkers = false;
			PositiveColor = Brushes.DodgerBlue;
			NegativeColor = Brushes.Crimson;
			BaseOpacity = 0.45;
			IntensityLookback = 50;
			DeltaMode = DeltaCalculationMode.BidAsk;
			ShowHistoricalColor = true;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			barTickDelta = new List<double>(4096);
			barHasData = new List<bool>(4096);
			barSyntheticDelta = new List<double>(4096);
			lastBid = double.NaN;
			lastAsk = double.NaN;
			prevLast = double.NaN;
			lastDirection = 0;
			InitializeBrushes();
		}
	}

	private void InitializeBrushes()
	{
		positiveBrushes = new Brush[20];
		negativeBrushes = new Brush[20];
		Color color = ((SolidColorBrush)PositiveColor).Color;
		Color color2 = ((SolidColorBrush)NegativeColor).Color;
		for (int i = 0; i < 20; i++)
		{
			double num = (double)i / 19.0;
			double num2 = BaseOpacity + (1.0 - BaseOpacity) * num;
			byte a = (byte)(num2 * (double)(int)color.A);
			byte a2 = (byte)(num2 * (double)(int)color2.A);
			SolidColorBrush solidColorBrush = new SolidColorBrush(Color.FromArgb(a, color.R, color.G, color.B));
			solidColorBrush.Freeze();
			positiveBrushes[i] = solidColorBrush;
			SolidColorBrush solidColorBrush2 = new SolidColorBrush(Color.FromArgb(a2, color2.R, color2.G, color2.B));
			solidColorBrush2.Freeze();
			negativeBrushes[i] = solidColorBrush2;
		}
	}

	private void EnsureBarLists(int idx)
	{
		while (barTickDelta.Count <= idx)
		{
			barTickDelta.Add(0.0);
			barHasData.Add(item: false);
			barSyntheticDelta.Add(double.NaN);
		}
	}

	/// <summary>
	/// Computes synthetic delta for a historical bar using OHLC.
	/// Formula: (Close - Open) / Range * Volume — directional and magnitude-scaled.
	/// Returns 0 if the bar has zero range.
	/// </summary>
	private double ComputeSyntheticDelta(int barIdx)
	{
		if (double.IsNaN(barSyntheticDelta[barIdx]))
		{
			double valueAt = ((NinjaScriptBase)this).Open.GetValueAt(barIdx);
			double valueAt2 = ((NinjaScriptBase)this).Close.GetValueAt(barIdx);
			double valueAt3 = ((NinjaScriptBase)this).High.GetValueAt(barIdx);
			double valueAt4 = ((NinjaScriptBase)this).Low.GetValueAt(barIdx);
			double num = valueAt3 - valueAt4;
			long num2 = (long)((NinjaScriptBase)this).Volume.GetValueAt(barIdx);
			if (num2 <= 0)
			{
				num2 = 1L;
			}
			double num3 = ((num > 0.0) ? ((valueAt2 - valueAt) / num) : ((valueAt2 >= valueAt) ? 1.0 : (-1.0)));
			barSyntheticDelta[barIdx] = num3 * (double)num2;
		}
		return barSyntheticDelta[barIdx];
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Invalid comparison between Unknown and I4
		if (DeltaMode != DeltaCalculationMode.BidAsk)
		{
			return;
		}
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
			long num3 = 0L;
			if (DeltaMode == DeltaCalculationMode.BidAsk && !double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0.0 && lastBid > 0.0 && lastAsk >= lastBid)
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
			if (num3 > 0)
			{
				lastDirection = 1;
			}
			else if (num3 < 0)
			{
				lastDirection = -1;
			}
			prevLast = num;
			if (num3 != 0L)
			{
				int num4 = ((NinjaScriptBase)this).CurrentBars[0];
				if (num4 >= 0)
				{
					EnsureBarLists(num4);
					barTickDelta[num4] += num3;
					barHasData[num4] = true;
				}
			}
		}
		else
		{
			if (((NinjaScriptBase)this).BarsInProgress != 0)
			{
				return;
			}
			EnsureBarLists(((NinjaScriptBase)this).CurrentBar);
			if (((NinjaScriptBase)this).Bars.IsFirstBarOfSession && ((NinjaScriptBase)this).IsFirstTickOfBar)
			{
				lastBid = double.NaN;
				lastAsk = double.NaN;
				prevLast = double.NaN;
			}
			bool flag = ((NinjaScriptBase)this).CurrentBar < barHasData.Count && barHasData[((NinjaScriptBase)this).CurrentBar];
			bool flag2 = !flag && ShowHistoricalColor;
			if (!(flag || flag2))
			{
				return;
			}
			double num5 = (flag ? barTickDelta[((NinjaScriptBase)this).CurrentBar] : ComputeSyntheticDelta(((NinjaScriptBase)this).CurrentBar));
			double num6 = 0.0;
			int num7 = Math.Max(0, ((NinjaScriptBase)this).CurrentBar - IntensityLookback);
			for (int num8 = ((NinjaScriptBase)this).CurrentBar; num8 >= num7; num8--)
			{
				double num9;
				if (num8 < barHasData.Count && barHasData[num8])
				{
					num9 = Math.Abs(barTickDelta[num8]);
				}
				else
				{
					if (!ShowHistoricalColor || num8 >= barSyntheticDelta.Count)
					{
						continue;
					}
					num9 = Math.Abs(ComputeSyntheticDelta(num8));
				}
				if (num9 > num6)
				{
					num6 = num9;
				}
			}
			if (num6 == 0.0)
			{
				num6 = 1.0;
			}
			int num10 = (int)Math.Round(Math.Abs(num5) / num6 * 19.0);
			if (num10 < 0)
			{
				num10 = 0;
			}
			if (num10 >= 20)
			{
				num10 = 19;
			}
			Brush candleOutlineBrush = (((NinjaScriptBase)this).BarBrush = ((num5 >= 0.0) ? positiveBrushes[num10] : negativeBrushes[num10]));
			((NinjaScriptBase)this).CandleOutlineBrush = candleOutlineBrush;
		}
	}
}
