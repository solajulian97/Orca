using System;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class RangeBarsType : BarsType
{
	public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
	{
	}

	public override void ApplyDefaultValue(BarsPeriod period)
	{
		period.Value = 4;
	}

	public override string ChartLabel(DateTime time)
	{
		return time.ToString("T", Globals.GeneralOptions.CurrentCulture);
	}

	public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
	{
		return 1;
	}

	public override double GetPercentComplete(Bars bars, DateTime now)
	{
		return 0.0;
	}

	protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0016: Expected O, but got Unknown
		if (((BarsType)this).SessionIterator == null)
		{
			SessionIterator val = new SessionIterator(bars);
			SessionIterator val2 = val;
			((BarsType)this).SessionIterator = val;
		}
		bool flag = ((BarsType)this).SessionIterator.IsNewSession(time, isBar);
		if (flag)
		{
			((BarsType)this).SessionIterator.GetNextSession(time, isBar);
		}
		if (bars.Count == 0 || (bars.IsResetOnNewTradingDay && flag))
		{
			((BarsType)this).AddBar(bars, open, high, low, close, time, volume);
		}
		else
		{
			double close2 = bars.GetClose(bars.Count - 1);
			double high2 = bars.GetHigh(bars.Count - 1);
			double low2 = bars.GetLow(bars.Count - 1);
			double tickSize = bars.Instrument.MasterInstrument.TickSize;
			double num = Math.Floor(10000000.0 * (double)bars.BarsPeriod.Value * tickSize) / 10000000.0;
			if (MathExtentions.ApproxCompare(close, low2 + num) > 0)
			{
				double num2 = low2 + num;
				if (MathExtentions.ApproxCompare(num2, close2) > 0)
				{
					((BarsType)this).UpdateBar(bars, num2, low2, num2, time, 0L);
				}
				double num3 = num2 + tickSize;
				while (MathExtentions.ApproxCompare(close, num2) > 0)
				{
					num2 = Math.Min(close, num3 + num);
					((BarsType)this).AddBar(bars, num3, num2, num3, num2, time, (MathExtentions.ApproxCompare(close, num2) > 0) ? 0 : volume);
					num3 = num2 + tickSize;
				}
			}
			else if (MathExtentions.ApproxCompare(high2 - num, close) > 0)
			{
				double num4 = high2 - num;
				if (MathExtentions.ApproxCompare(close2, num4) > 0)
				{
					((BarsType)this).UpdateBar(bars, high2, num4, num4, time, 0L);
				}
				double num5 = num4 - tickSize;
				while (MathExtentions.ApproxCompare(num4, close) > 0)
				{
					num4 = Math.Max(close, num5 - num);
					((BarsType)this).AddBar(bars, num5, num5, num4, num4, time, (MathExtentions.ApproxCompare(num4, close) > 0) ? 0 : volume);
					num5 = num4 - tickSize;
				}
			}
			else
			{
				((BarsType)this).UpdateBar(bars, (close > high2) ? close : high2, (close < low2) ? close : low2, close, time, volume);
			}
		}
		bars.LastPrice = close;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Invalid comparison between Unknown and I4
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeRange;
			((BarsType)this).BuiltFrom = (BarsPeriodType)0;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)2
			};
			((BarsType)this).DaysToLoad = 3;
			((BarsType)this).IsIntraday = true;
			((BarsType)this).IsTimeBased = false;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScript)this).Name = string.Format(Globals.GeneralOptions.CurrentCulture, Resource.DataBarsTypeRange, ((BarsType)this).BarsPeriod.Value, ((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? (" - " + Globals.ToLocalizedObject((object)((BarsType)this).BarsPeriod.MarketDataType, Globals.GeneralOptions.CurrentUICulture)) : string.Empty);
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodValue", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
		}
	}
}
