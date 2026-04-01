using System;
using System.Globalization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class WeekBarsType : BarsType
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
		return time.ToString(DateTimeFormatInfo.CurrentInfo.MonthDayPattern);
	}

	public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
	{
		return barsPeriod.Value * barsBack * 7;
	}

	public override double GetPercentComplete(Bars bars, DateTime now)
	{
		if (!(now.Date <= bars.LastBarTime.Date))
		{
			return 1.0;
		}
		return (7.0 - bars.LastBarTime.AddDays(1.0).Subtract(now).TotalDays / (double)bars.BarsPeriod.Value) / 7.0;
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
				((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTime(time, time.AddDays(6 - (int)(time.DayOfWeek + 1) % 7 + (bars.BarsPeriod.Value - 1) * 7), bars.BarsPeriod.Value), volume);
				return;
			}
			((BarsType)this).SessionIterator.CalculateTradingDay(time, false);
			DateTime actualTradingDayExchange = ((BarsType)this).SessionIterator.ActualTradingDayExchange;
			((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTime(actualTradingDayExchange, actualTradingDayExchange.AddDays(6 - (int)(actualTradingDayExchange.DayOfWeek + 1) % 7 + (bars.BarsPeriod.Value - 1) * 7), bars.BarsPeriod.Value), volume);
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
		if (dateTime <= bars.LastBarTime.Date)
		{
			((BarsType)this).UpdateBar(bars, high, low, close, bars.LastBarTime, volume);
		}
		else
		{
			((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTime(dateTime, bars.LastBarTime.Date, bars.BarsPeriod.Value), volume);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Invalid comparison between Unknown and I4
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeWeek;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)6
			};
			((BarsType)this).BuiltFrom = (BarsPeriodType)5;
			((BarsType)this).DaysToLoad = 1825;
			((BarsType)this).WeeksToLoad = 620;
			((BarsType)this).MonthsToLoad = 60;
			((BarsType)this).YearsToLoad = 5;
			((BarsType)this).IsIntraday = false;
			((BarsType)this).IsTimeBased = true;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScript)this).Name = ((((BarsType)this).BarsPeriod.Value == 1) ? Resource.DataBarsTypeWeekly : string.Format(Globals.GeneralOptions.CurrentCulture, Resource.DataBarsTypeWeek, ((BarsType)this).BarsPeriod.Value)) + (((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {Globals.ToLocalizedObject((object)((BarsType)this).BarsPeriod.MarketDataType, Globals.GeneralOptions.CurrentUICulture)}" : string.Empty);
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodValue", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
		}
	}

	private DateTime TimeToBarTime(DateTime time, DateTime periodStart, int periodValue)
	{
		return periodStart.Date.AddDays(Math.Ceiling(Math.Ceiling(time.Date.Subtract(periodStart.Date).TotalDays) / (double)(periodValue * 7)) * (double)(periodValue * 7)).Date;
	}
}
