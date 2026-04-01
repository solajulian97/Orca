using System;
using System.Globalization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class YearBarsType : BarsType
{
	public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
	{
	}

	public override void ApplyDefaultValue(BarsPeriod period)
	{
		period.Value = 1;
	}

	public override string ChartLabel(DateTime time)
	{
		return time.ToString("yyyy", CultureInfo.InvariantCulture);
	}

	public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
	{
		return barsPeriod.Value * barsBack * 365;
	}

	public override double GetPercentComplete(Bars bars, DateTime now)
	{
		if (now.Date <= bars.LastBarTime.Date)
		{
			double num = (DateTime.IsLeapYear(now.Year) ? 366 : 365);
			return (num - bars.LastBarTime.Date.AddDays(1.0).Subtract(now).TotalDays / (double)bars.BarsPeriod.Value) / num;
		}
		return 1.0;
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
		if (bars.Count == 0)
		{
			if (isBar || bars.TradingHours.Sessions.Count == 0)
			{
				((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTime(time, bars.BarsPeriod.Value), volume);
				return;
			}
			((BarsType)this).SessionIterator.CalculateTradingDay(time, false);
			((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTime(((BarsType)this).SessionIterator.ActualTradingDayExchange, bars.BarsPeriod.Value), volume);
			return;
		}
		DateTime dateTime;
		if (isBar)
		{
			dateTime = time.Date;
		}
		else if (((BarsType)this).SessionIterator.IsNewSession(time, false))
		{
			((BarsType)this).SessionIterator.CalculateTradingDay(time, false);
			dateTime = ((BarsType)this).SessionIterator.ActualTradingDayExchange;
			if (dateTime < bars.LastBarTime.Date)
			{
				dateTime = bars.LastBarTime.Date;
			}
		}
		else
		{
			dateTime = bars.LastBarTime.Date;
		}
		if (dateTime.Year <= bars.LastBarTime.Year)
		{
			((BarsType)this).UpdateBar(bars, high, low, close, bars.LastBarTime, volume);
		}
		else
		{
			((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTime(dateTime, bars.BarsPeriod.Value), volume);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Invalid comparison between Unknown and I4
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.BarsPeriodTypeNameYear;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)8
			};
			((BarsType)this).BuiltFrom = (BarsPeriodType)5;
			((BarsType)this).DaysToLoad = 15000;
			((BarsType)this).WeeksToLoad = 780;
			((BarsType)this).MonthsToLoad = 180;
			((BarsType)this).YearsToLoad = 15;
			((BarsType)this).IsIntraday = false;
			((BarsType)this).IsTimeBased = true;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScript)this).Name = ((((BarsType)this).BarsPeriod.Value == 1) ? Resource.DataBarsTypeYearly : string.Format(Globals.GeneralOptions.CurrentCulture, Resource.DataBarsTypeYear, ((BarsType)this).BarsPeriod.Value)) + (((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? (" - " + Globals.ToLocalizedObject((object)((BarsType)this).BarsPeriod.MarketDataType, Globals.GeneralOptions.CurrentUICulture)) : string.Empty);
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodValue", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
		}
	}

	private static DateTime TimeToBarTime(DateTime time, int periodValue)
	{
		DateTime dateTime = new DateTime(time.Year, 1, 1);
		for (int i = 0; i < periodValue; i++)
		{
			dateTime = dateTime.AddYears(1);
		}
		return dateTime.AddDays(-1.0);
	}
}
