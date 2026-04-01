using System;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class RenkoBarsType : BarsType
{
	private double offset;

	private double renkoHigh;

	private double renkoLow;

	public override bool IsRemoveLastBarSupported => true;

	public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
	{
	}

	public override void ApplyDefaultValue(BarsPeriod period)
	{
		period.Value = 2;
	}

	public override string ChartLabel(DateTime time)
	{
		return time.ToString("T", Globals.GeneralOptions.CurrentCulture);
	}

	public override int GetInitialLookBackDays(BarsPeriod period, TradingHours tradingHours, int barsBack)
	{
		return 3;
	}

	public override double GetPercentComplete(Bars bars, DateTime now)
	{
		return 0.0;
	}

	protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0017: Expected O, but got Unknown
		if (((BarsType)this).SessionIterator == null)
		{
			SessionIterator val = new SessionIterator(bars);
			SessionIterator val2 = val;
			((BarsType)this).SessionIterator = val;
		}
		offset = (double)bars.BarsPeriod.Value * bars.Instrument.MasterInstrument.TickSize;
		bool flag = ((BarsType)this).SessionIterator.IsNewSession(time, isBar);
		if (flag)
		{
			((BarsType)this).SessionIterator.GetNextSession(time, isBar);
		}
		if (bars.Count == 0 || (bars.IsResetOnNewTradingDay && flag))
		{
			if (bars.Count > 0)
			{
				double close2 = bars.GetClose(bars.Count - 1);
				DateTime time2 = bars.GetTime(bars.Count - 1);
				long volume2 = bars.GetVolume(bars.Count - 1);
				((BarsType)this).RemoveLastBar(bars);
				((BarsType)this).AddBar(bars, close2, close2, close2, close2, time2, volume2);
			}
			renkoHigh = close + offset;
			renkoLow = close - offset;
			if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
			{
				((BarsType)this).SessionIterator.GetNextSession(time, isBar);
			}
			((BarsType)this).AddBar(bars, close, close, close, close, time, volume);
			bars.LastPrice = close;
			return;
		}
		double open2 = bars.GetOpen(bars.Count - 1);
		double high2 = bars.GetHigh(bars.Count - 1);
		double low2 = bars.GetLow(bars.Count - 1);
		long volume3 = bars.GetVolume(bars.Count - 1);
		DateTime time3 = bars.GetTime(bars.Count - 1);
		if (MathExtentions.ApproxCompare(renkoHigh, 0.0) == 0 || MathExtentions.ApproxCompare(renkoLow, 0.0) == 0)
		{
			if (bars.Count == 1)
			{
				renkoHigh = open2 + offset;
				renkoLow = open2 - offset;
			}
			else if (bars.GetClose(bars.Count - 2) > bars.GetOpen(bars.Count - 2))
			{
				renkoHigh = bars.GetClose(bars.Count - 2) + offset;
				renkoLow = bars.GetClose(bars.Count - 2) - offset * 2.0;
			}
			else
			{
				renkoHigh = bars.GetClose(bars.Count - 2) + offset * 2.0;
				renkoLow = bars.GetClose(bars.Count - 2) - offset;
			}
		}
		if (MathExtentions.ApproxCompare(close, renkoHigh) >= 0)
		{
			if (MathExtentions.ApproxCompare(open2, renkoHigh - offset) != 0 || MathExtentions.ApproxCompare(high2, Math.Max(renkoHigh - offset, renkoHigh)) != 0 || MathExtentions.ApproxCompare(low2, Math.Min(renkoHigh - offset, renkoHigh)) != 0)
			{
				((BarsType)this).RemoveLastBar(bars);
				((BarsType)this).AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, renkoHigh), Math.Min(renkoHigh - offset, renkoHigh), renkoHigh, time3, volume3);
			}
			renkoLow = renkoHigh - 2.0 * offset;
			renkoHigh += offset;
			if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
			{
				((BarsType)this).SessionIterator.GetNextSession(time, isBar);
			}
			while (MathExtentions.ApproxCompare(close, renkoHigh) >= 0)
			{
				((BarsType)this).AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, renkoHigh), Math.Min(renkoHigh - offset, renkoHigh), renkoHigh, time, 0L);
				renkoLow = renkoHigh - 2.0 * offset;
				renkoHigh += offset;
			}
			((BarsType)this).AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, close), Math.Min(renkoHigh - offset, close), close, time, volume);
		}
		else if (MathExtentions.ApproxCompare(close, renkoLow) <= 0)
		{
			if (MathExtentions.ApproxCompare(open2, renkoLow + offset) != 0 || MathExtentions.ApproxCompare(high2, Math.Max(renkoLow + offset, renkoLow)) != 0 || MathExtentions.ApproxCompare(low2, Math.Min(renkoLow + offset, renkoLow)) != 0)
			{
				((BarsType)this).RemoveLastBar(bars);
				((BarsType)this).AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, renkoLow), Math.Min(renkoLow + offset, renkoLow), renkoLow, time3, volume3);
			}
			renkoHigh = renkoLow + 2.0 * offset;
			renkoLow -= offset;
			if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
			{
				((BarsType)this).SessionIterator.GetNextSession(time, isBar);
			}
			while (MathExtentions.ApproxCompare(close, renkoLow) <= 0)
			{
				((BarsType)this).AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, renkoLow), Math.Min(renkoLow + offset, renkoLow), renkoLow, time, 0L);
				renkoHigh = renkoLow + 2.0 * offset;
				renkoLow -= offset;
			}
			((BarsType)this).AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, close), Math.Min(renkoLow + offset, close), close, time, volume);
		}
		else
		{
			((BarsType)this).UpdateBar(bars, close, close, close, time, volume);
		}
		bars.LastPrice = close;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeRenko;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)11
			};
			((BarsType)this).BuiltFrom = (BarsPeriodType)0;
			((BarsType)this).DaysToLoad = 3;
			((BarsType)this).DefaultChartStyle = (ChartStyleType)6;
			((BarsType)this).IsIntraday = true;
			((BarsType)this).IsTimeBased = false;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScript)this).Name = string.Format(Globals.GeneralOptions.CurrentCulture, Resource.DataBarsTypeRenko, ((BarsType)this).BarsPeriod.Value);
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodValue", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
			((BarsType)this).SetPropertyName("Value", Resource.NinjaScriptBarsTypeRenkoBrickSize);
		}
	}
}
