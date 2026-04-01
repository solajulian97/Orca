using System;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class HeikinAshiBarsType : BarsType
{
	public override void ApplyDefaultValue(BarsPeriod period)
	{
	}

	public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected I4, but got Unknown
		BarsPeriodType baseBarsPeriodType = period.BaseBarsPeriodType;
		switch ((int)baseBarsPeriodType)
		{
		case 5:
			period.BaseBarsPeriodValue = 1;
			((BarsType)this).DaysToLoad = 365;
			((BarsType)this).WeeksToLoad = 52;
			((BarsType)this).MonthsToLoad = 12;
			((BarsType)this).YearsToLoad = 1;
			break;
		case 4:
			period.BaseBarsPeriodValue = 1;
			((BarsType)this).DaysToLoad = 5;
			((BarsType)this).WeeksToLoad = 1;
			((BarsType)this).MonthsToLoad = 0;
			((BarsType)this).YearsToLoad = 0;
			break;
		case 7:
			period.BaseBarsPeriodValue = 1;
			((BarsType)this).DaysToLoad = 5475;
			((BarsType)this).WeeksToLoad = 780;
			((BarsType)this).MonthsToLoad = 180;
			((BarsType)this).YearsToLoad = 15;
			break;
		case 3:
			period.BaseBarsPeriodValue = 30;
			((BarsType)this).DaysToLoad = 3;
			((BarsType)this).WeeksToLoad = 0;
			((BarsType)this).MonthsToLoad = 0;
			((BarsType)this).YearsToLoad = 0;
			break;
		case 0:
			period.BaseBarsPeriodValue = 150;
			((BarsType)this).DaysToLoad = 3;
			((BarsType)this).WeeksToLoad = 0;
			((BarsType)this).MonthsToLoad = 0;
			((BarsType)this).YearsToLoad = 0;
			break;
		case 1:
			period.BaseBarsPeriodValue = 1000;
			((BarsType)this).DaysToLoad = 3;
			((BarsType)this).WeeksToLoad = 0;
			((BarsType)this).MonthsToLoad = 0;
			((BarsType)this).YearsToLoad = 0;
			break;
		case 6:
			period.BaseBarsPeriodValue = 1;
			((BarsType)this).DaysToLoad = 1825;
			((BarsType)this).WeeksToLoad = 260;
			((BarsType)this).MonthsToLoad = 60;
			((BarsType)this).YearsToLoad = 5;
			break;
		case 8:
			period.BaseBarsPeriodValue = 1;
			((BarsType)this).DaysToLoad = 15000;
			((BarsType)this).WeeksToLoad = 780;
			((BarsType)this).MonthsToLoad = 180;
			((BarsType)this).YearsToLoad = 15;
			break;
		case 2:
			break;
		}
	}

	public override string ChartLabel(DateTime time)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected I4, but got Unknown
		BarsPeriodType baseBarsPeriodType = ((BarsType)this).BarsPeriod.BaseBarsPeriodType;
		return (int)baseBarsPeriodType switch
		{
			5 => BarsType.BarsTypeDay.ChartLabel(time), 
			4 => BarsType.BarsTypeMinute.ChartLabel(time), 
			7 => BarsType.BarsTypeMonth.ChartLabel(time), 
			3 => BarsType.BarsTypeSecond.ChartLabel(time), 
			0 => BarsType.BarsTypeTick.ChartLabel(time), 
			1 => BarsType.BarsTypeTick.ChartLabel(time), 
			6 => BarsType.BarsTypeDay.ChartLabel(time), 
			8 => BarsType.BarsTypeYear.ChartLabel(time), 
			_ => BarsType.BarsTypeDay.ChartLabel(time), 
		};
	}

	public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected I4, but got Unknown
		BarsPeriodType baseBarsPeriodType = ((BarsType)this).BarsPeriod.BaseBarsPeriodType;
		switch ((int)baseBarsPeriodType)
		{
		case 5:
			return (int)Math.Ceiling((double)(barsPeriod.BaseBarsPeriodValue * barsBack) * 7.0 / 4.5);
		case 4:
		{
			int num = 0;
			lock (tradingHours.Sessions)
			{
				foreach (Session session in tradingHours.Sessions)
				{
					int beginDay = (int)session.BeginDay;
					int num2 = (int)session.EndDay;
					if (beginDay > num2)
					{
						num2 += 7;
					}
					num += (num2 - beginDay) * 1440 + session.EndTime / 100 * 60 + session.EndTime % 100 - (session.BeginTime / 100 * 60 + session.BeginTime % 100);
				}
			}
			return (int)Math.Max(1.0, Math.Ceiling((double)barsBack / Math.Max(1.0, (double)num / 7.0 / (double)barsPeriod.BaseBarsPeriodValue) * 1.05));
		}
		case 7:
			return barsPeriod.BaseBarsPeriodValue * barsBack * 31;
		case 3:
			return (int)Math.Max(1.0, Math.Ceiling((double)barsBack / Math.Max(1.0, 28800.0 / (double)barsPeriod.BaseBarsPeriodValue)) * 7.0 / 5.0);
		case 0:
			return 1;
		case 1:
			return 1;
		case 6:
			return barsPeriod.BaseBarsPeriodValue * barsBack * 7;
		case 8:
			return barsPeriod.BaseBarsPeriodValue * barsBack * 365;
		default:
			return 1;
		}
	}

	public override double GetPercentComplete(Bars bars, DateTime now)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected I4, but got Unknown
		BarsPeriodType baseBarsPeriodType = ((BarsType)this).BarsPeriod.BaseBarsPeriodType;
		switch ((int)baseBarsPeriodType)
		{
		case 5:
			if (!(now.Date <= bars.LastBarTime.Date))
			{
				return 1.0;
			}
			return 1.0 - bars.LastBarTime.AddDays(1.0).Subtract(now).TotalDays / (double)bars.BarsPeriod.BaseBarsPeriodValue;
		case 4:
			if (!(now <= bars.LastBarTime))
			{
				return 1.0;
			}
			return 1.0 - bars.LastBarTime.Subtract(now).TotalMinutes / (double)bars.BarsPeriod.BaseBarsPeriodValue;
		case 7:
			if (now.Date <= bars.LastBarTime.Date)
			{
				int num2;
				switch (now.Month)
				{
				default:
					num2 = 30;
					break;
				case 1:
				case 3:
				case 5:
				case 7:
				case 8:
				case 10:
				case 12:
					num2 = 31;
					break;
				case 2:
					num2 = (DateTime.IsLeapYear(now.Year) ? 29 : 28);
					break;
				}
				int num3 = num2;
				return ((double)num3 - bars.LastBarTime.Date.AddDays(1.0).Subtract(now).TotalDays / (double)bars.BarsPeriod.BaseBarsPeriodValue) / (double)num3;
			}
			return 1.0;
		case 3:
			if (!(now <= bars.LastBarTime))
			{
				return 1.0;
			}
			return 1.0 - bars.LastBarTime.Subtract(now).TotalSeconds / (double)bars.BarsPeriod.BaseBarsPeriodValue;
		case 0:
			return (double)bars.TickCount / (double)bars.BarsPeriod.BaseBarsPeriodValue;
		case 1:
			if (bars.Count != 0)
			{
				return (double)bars.GetVolume(bars.Count - 1) / (double)bars.BarsPeriod.BaseBarsPeriodValue;
			}
			return 0.0;
		case 6:
			if (!(now.Date <= bars.LastBarTime.Date))
			{
				return 1.0;
			}
			return (7.0 - bars.LastBarTime.AddDays(1.0).Subtract(now).TotalDays / (double)bars.BarsPeriod.BaseBarsPeriodValue) / 7.0;
		case 8:
			if (now.Date <= bars.LastBarTime.Date)
			{
				double num = (DateTime.IsLeapYear(now.Year) ? 366 : 365);
				return (num - bars.LastBarTime.Date.AddDays(1.0).Subtract(now).TotalDays / (double)bars.BarsPeriod.BaseBarsPeriodValue) / num;
			}
			return 1.0;
		default:
			return 1.0;
		}
	}

	protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected I4, but got Unknown
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
		double num = 0.0;
		double num2 = 0.0;
		BarsPeriodType baseBarsPeriodType = ((BarsType)this).BarsPeriod.BaseBarsPeriodType;
		switch ((int)baseBarsPeriodType)
		{
		case 5:
		{
			if (bars.Count == 0)
			{
				if (isBar || bars.TradingHours.Sessions.Count == 0)
				{
					((BarsType)this).AddBar(bars, open, high, low, close, time.Date, volume);
					break;
				}
				((BarsType)this).SessionIterator.CalculateTradingDay(time, false);
				((BarsType)this).AddBar(bars, open, high, low, close, ((BarsType)this).SessionIterator.ActualTradingDayExchange, volume);
				break;
			}
			DateTime dateTime2;
			if (isBar)
			{
				dateTime2 = time.Date;
			}
			else if (bars.TradingHours.Sessions.Count > 0 && ((BarsType)this).SessionIterator.IsNewSession(time, false))
			{
				((BarsType)this).SessionIterator.CalculateTradingDay(time, false);
				dateTime2 = ((BarsType)this).SessionIterator.ActualTradingDayExchange;
				if (dateTime2 < bars.LastBarTime.Date)
				{
					dateTime2 = bars.LastBarTime.Date;
				}
			}
			else
			{
				dateTime2 = bars.LastBarTime.Date;
			}
			if (bars.DayCount < bars.BarsPeriod.BaseBarsPeriodValue || (isBar && bars.Count > 0 && dateTime2 == bars.LastBarTime.Date) || (!isBar && bars.Count > 0 && dateTime2 <= bars.LastBarTime.Date))
			{
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, bars.GetOpen(bars.Count - 1)));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, bars.GetOpen(bars.Count - 1)));
				((BarsType)this).UpdateBar(bars, num3, num4, num, dateTime2, volume);
			}
			else
			{
				num2 = bars.Instrument.MasterInstrument.RoundToTickSize((bars.GetOpen(bars.Count - 1) + bars.GetClose(bars.Count - 1)) / 2.0);
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
				((BarsType)this).AddBar(bars, num2, num3, num4, num, dateTime2, volume);
			}
			break;
		}
		case 4:
			if (bars.Count == 0)
			{
				((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTimeMinute(bars, time, isBar), volume);
			}
			else if (!isBar && time < bars.LastBarTime)
			{
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, bars.GetOpen(bars.Count - 1)));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, bars.GetOpen(bars.Count - 1)));
				((BarsType)this).UpdateBar(bars, num3, num4, num, bars.LastBarTime, volume);
			}
			else if (isBar && time <= bars.LastBarTime)
			{
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, bars.GetOpen(bars.Count - 1)));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, bars.GetOpen(bars.Count - 1)));
				((BarsType)this).UpdateBar(bars, num3, num4, num, bars.LastBarTime, volume);
			}
			else
			{
				num2 = bars.Instrument.MasterInstrument.RoundToTickSize((bars.GetOpen(bars.Count - 1) + bars.GetClose(bars.Count - 1)) / 2.0);
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
				time = TimeToBarTimeMinute(bars, time, isBar);
				((BarsType)this).AddBar(bars, num2, num3, num4, num, time, volume);
			}
			break;
		case 7:
			if (bars.Count == 0)
			{
				((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTimeMonth(time, bars.BarsPeriod.BaseBarsPeriodValue), volume);
			}
			else if ((time.Month <= bars.LastBarTime.Month && time.Year == bars.LastBarTime.Year) || time.Year < bars.LastBarTime.Year)
			{
				if (MathExtentions.ApproxCompare(high, bars.GetHigh(bars.Count - 1)) != 0 || MathExtentions.ApproxCompare(low, bars.GetLow(bars.Count - 1)) != 0 || MathExtentions.ApproxCompare(close, bars.GetClose(bars.Count - 1)) != 0 || volume > 0)
				{
					num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
					double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, bars.GetOpen(bars.Count - 1)));
					double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, bars.GetOpen(bars.Count - 1)));
					((BarsType)this).UpdateBar(bars, num3, num4, num, bars.LastBarTime, volume);
				}
			}
			else
			{
				num2 = bars.Instrument.MasterInstrument.RoundToTickSize((bars.GetOpen(bars.Count - 1) + bars.GetClose(bars.Count - 1)) / 2.0);
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
				((BarsType)this).AddBar(bars, num2, num3, num4, num, TimeToBarTimeMonth(time, bars.BarsPeriod.BaseBarsPeriodValue), volume);
			}
			break;
		case 3:
			if (bars.Count == 0)
			{
				DateTime dateTime = TimeToBarTimeSecond(bars, time, isBar);
				((BarsType)this).AddBar(bars, open, high, low, close, dateTime, volume);
			}
			else if ((bars.BarsPeriod.BaseBarsPeriodValue > 1 && time < bars.LastBarTime) || (bars.BarsPeriod.BaseBarsPeriodValue == 1 && time <= bars.LastBarTime))
			{
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, bars.GetOpen(bars.Count - 1)));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, bars.GetOpen(bars.Count - 1)));
				((BarsType)this).UpdateBar(bars, num3, num4, num, bars.LastBarTime, volume);
			}
			else
			{
				num2 = bars.Instrument.MasterInstrument.RoundToTickSize((bars.GetOpen(bars.Count - 1) + bars.GetClose(bars.Count - 1)) / 2.0);
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
				time = TimeToBarTimeSecond(bars, time, isBar);
				((BarsType)this).AddBar(bars, num2, num3, num4, num, time, volume);
			}
			break;
		case 0:
		{
			bool flag = ((BarsType)this).SessionIterator.IsNewSession(time, isBar);
			if (flag)
			{
				((BarsType)this).SessionIterator.GetNextSession(time, isBar);
			}
			if (bars.BarsPeriod.BaseBarsPeriodValue == 1)
			{
				num2 = ((MathExtentions.ApproxCompare(num2, 0.0) == 0) ? open : ((num2 + num) / 2.0));
				num = ((MathExtentions.ApproxCompare(num, 0.0) == 0) ? close : bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0));
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
				((BarsType)this).AddBar(bars, num2, num3, num4, num, time, volume);
			}
			else if (bars.Count == 0)
			{
				((BarsType)this).AddBar(bars, open, high, low, close, time, volume);
			}
			else if (bars.Count > 0 && (!flag || !bars.IsResetOnNewTradingDay) && bars.BarsPeriod.BaseBarsPeriodValue > 1 && bars.TickCount < bars.BarsPeriod.BaseBarsPeriodValue)
			{
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, bars.GetOpen(bars.Count - 1)));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, bars.GetOpen(bars.Count - 1)));
				((BarsType)this).UpdateBar(bars, num3, num4, num, time, volume);
			}
			else
			{
				num2 = bars.Instrument.MasterInstrument.RoundToTickSize((bars.GetOpen(bars.Count - 1) + bars.GetClose(bars.Count - 1)) / 2.0);
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
				((BarsType)this).AddBar(bars, num2, num3, num4, num, time, volume);
			}
			break;
		}
		case 1:
		{
			if (bars.Count == 0)
			{
				while (volume > bars.BarsPeriod.BaseBarsPeriodValue)
				{
					num2 = ((MathExtentions.ApproxCompare(num2, 0.0) == 0) ? open : ((num2 + num) / 2.0));
					num = ((MathExtentions.ApproxCompare(num, 0.0) == 0) ? close : bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0));
					double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
					double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
					((BarsType)this).AddBar(bars, num2, num3, num4, num, time, (long)bars.BarsPeriod.BaseBarsPeriodValue);
					volume -= bars.BarsPeriod.BaseBarsPeriodValue;
				}
				if (volume > 0)
				{
					num2 = ((MathExtentions.ApproxCompare(num2, 0.0) == 0) ? open : bars.Instrument.MasterInstrument.RoundToTickSize((num2 + num) / 2.0));
					num = ((MathExtentions.ApproxCompare(num, 0.0) == 0) ? close : bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0));
					double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
					double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
					((BarsType)this).AddBar(bars, num2, num3, num4, num, time, volume);
				}
				break;
			}
			long num5 = 0L;
			bool flag2 = ((BarsType)this).SessionIterator.IsNewSession(time, isBar);
			if (!bars.IsResetOnNewTradingDay || !flag2)
			{
				num5 = Math.Min(bars.BarsPeriod.BaseBarsPeriodValue - bars.GetVolume(bars.Count - 1), volume);
				if (num5 > 0)
				{
					num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
					double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, bars.GetOpen(bars.Count - 1)));
					double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, bars.GetOpen(bars.Count - 1)));
					((BarsType)this).UpdateBar(bars, num3, num4, num, time, num5);
				}
			}
			if (flag2)
			{
				((BarsType)this).SessionIterator.GetNextSession(time, isBar);
			}
			for (num5 = volume - num5; num5 > 0; num5 -= bars.BarsPeriod.BaseBarsPeriodValue)
			{
				num2 = bars.Instrument.MasterInstrument.RoundToTickSize((bars.GetOpen(bars.Count - 1) + bars.GetClose(bars.Count - 1)) / 2.0);
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
				((BarsType)this).AddBar(bars, num2, num3, num4, num, time, Math.Min(num5, bars.BarsPeriod.BaseBarsPeriodValue));
			}
			break;
		}
		case 6:
			if (bars.Count == 0)
			{
				((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTimeWeek(time, time.AddDays(6 - (int)(time.DayOfWeek + 1) % 7 + (bars.BarsPeriod.BaseBarsPeriodValue - 1) * 7), bars.BarsPeriod.BaseBarsPeriodValue), volume);
			}
			else if (time.Date <= bars.LastBarTime.Date)
			{
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, bars.GetOpen(bars.Count - 1)));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, bars.GetOpen(bars.Count - 1)));
				((BarsType)this).UpdateBar(bars, num3, num4, num, bars.LastBarTime, volume);
			}
			else
			{
				num2 = bars.Instrument.MasterInstrument.RoundToTickSize((bars.GetOpen(bars.Count - 1) + bars.GetClose(bars.Count - 1)) / 2.0);
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
				((BarsType)this).AddBar(bars, num2, num3, num4, num, TimeToBarTimeWeek(time.Date, bars.LastBarTime.Date, bars.BarsPeriod.BaseBarsPeriodValue), volume);
			}
			break;
		case 8:
			if (bars.Count == 0)
			{
				((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTimeYear(time, bars.BarsPeriod.BaseBarsPeriodValue), volume);
			}
			else if (time.Year <= bars.LastBarTime.Year)
			{
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, bars.GetOpen(bars.Count - 1)));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, bars.GetOpen(bars.Count - 1)));
				((BarsType)this).UpdateBar(bars, num3, num4, num, bars.LastBarTime, volume);
			}
			else
			{
				num2 = bars.Instrument.MasterInstrument.RoundToTickSize((bars.GetOpen(bars.Count - 1) + bars.GetClose(bars.Count - 1)) / 2.0);
				num = bars.Instrument.MasterInstrument.RoundToTickSize((open + high + low + close) / 4.0);
				double num3 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Max(high, num2));
				double num4 = bars.Instrument.MasterInstrument.RoundToTickSize(Math.Min(low, num2));
				((BarsType)this).AddBar(bars, num2, num3, num4, num, TimeToBarTimeYear(time.Date, bars.BarsPeriod.BaseBarsPeriodValue), volume);
			}
			break;
		}
		bars.LastPrice = num;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected I4, but got Unknown
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Expected I4, but got Unknown
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Invalid comparison between Unknown and I4
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Invalid comparison between Unknown and I4
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Invalid comparison between Unknown and I4
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Invalid comparison between Unknown and I4
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Invalid comparison between Unknown and I4
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Invalid comparison between Unknown and I4
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Invalid comparison between Unknown and I4
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Invalid comparison between Unknown and I4
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f9: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeHeikenAshi;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)9
			};
			((BarsType)this).DaysToLoad = 3;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			BarsPeriodType baseBarsPeriodType = ((BarsType)this).BarsPeriod.BaseBarsPeriodType;
			switch ((int)baseBarsPeriodType)
			{
			case 4:
				((BarsType)this).BuiltFrom = (BarsPeriodType)4;
				((BarsType)this).IsIntraday = true;
				((BarsType)this).IsTimeBased = true;
				break;
			case 3:
				((BarsType)this).BuiltFrom = (BarsPeriodType)0;
				((BarsType)this).IsIntraday = true;
				((BarsType)this).IsTimeBased = true;
				break;
			case 0:
			case 1:
				((BarsType)this).BuiltFrom = (BarsPeriodType)0;
				((BarsType)this).IsIntraday = true;
				((BarsType)this).IsTimeBased = false;
				break;
			default:
				((BarsType)this).BuiltFrom = (BarsPeriodType)5;
				((BarsType)this).IsIntraday = false;
				((BarsType)this).IsTimeBased = true;
				break;
			}
			baseBarsPeriodType = ((BarsType)this).BarsPeriod.BaseBarsPeriodType;
			switch ((int)baseBarsPeriodType)
			{
			case 5:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiDaily : Resource.GuiDay)} Heiken-Ashi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 4:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Min Heiken-Ashi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 7:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiMonthly : Resource.GuiMonth)} Heiken-Ashi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 3:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiSecond : Resource.GuiSeconds)} Heiken-Ashi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 0:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Tick Heiken-Ashi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 1:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Volume Heiken-Ashi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 6:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiWeekly : Resource.GuiWeeks)} Heiken-Ashi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 8:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiYearly : Resource.GuiYears)} Heiken-Ashi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			}
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
		}
	}

	private DateTime TimeToBarTimeMinute(Bars bars, DateTime time, bool isBar)
	{
		if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
		{
			((BarsType)this).SessionIterator.GetNextSession(time, isBar);
		}
		if (bars.IsResetOnNewTradingDay || (!bars.IsResetOnNewTradingDay && bars.Count == 0))
		{
			DateTime dateTime = (isBar ? ((BarsType)this).SessionIterator.ActualSessionBegin.AddMinutes(Math.Ceiling(Math.Ceiling(Math.Max(0.0, time.Subtract(((BarsType)this).SessionIterator.ActualSessionBegin).TotalMinutes)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue) : ((BarsType)this).SessionIterator.ActualSessionBegin.AddMinutes((double)bars.BarsPeriod.BaseBarsPeriodValue + Math.Floor(Math.Floor(Math.Max(0.0, time.Subtract(((BarsType)this).SessionIterator.ActualSessionBegin).TotalMinutes)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue));
			if (bars.TradingHours.Sessions.Count > 0 && dateTime > ((BarsType)this).SessionIterator.ActualSessionEnd)
			{
				dateTime = ((BarsType)this).SessionIterator.ActualSessionEnd;
			}
			return dateTime;
		}
		DateTime time2 = bars.GetTime(bars.Count - 1);
		DateTime dateTime2 = (isBar ? time2.AddMinutes(Math.Ceiling(Math.Ceiling(Math.Max(0.0, time.Subtract(time2).TotalMinutes)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue) : time2.AddMinutes((double)bars.BarsPeriod.BaseBarsPeriodValue + Math.Floor(Math.Floor(Math.Max(0.0, time.Subtract(time2).TotalMinutes)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue));
		if (bars.TradingHours.Sessions.Count > 0 && dateTime2 > ((BarsType)this).SessionIterator.ActualSessionEnd)
		{
			DateTime actualSessionEnd = ((BarsType)this).SessionIterator.ActualSessionEnd;
			((BarsType)this).SessionIterator.GetNextSession(((BarsType)this).SessionIterator.ActualSessionEnd.AddSeconds(1.0), isBar);
			dateTime2 = ((BarsType)this).SessionIterator.ActualSessionBegin.AddMinutes((int)dateTime2.Subtract(actualSessionEnd).TotalMinutes);
		}
		return dateTime2;
	}

	private static DateTime TimeToBarTimeMonth(DateTime time, int periodValue)
	{
		DateTime dateTime = new DateTime(time.Year, time.Month, 1);
		for (int i = 0; i < periodValue; i++)
		{
			dateTime = dateTime.AddMonths(1);
		}
		return dateTime.AddDays(-1.0);
	}

	private DateTime TimeToBarTimeSecond(Bars bars, DateTime time, bool isBar)
	{
		if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
		{
			((BarsType)this).SessionIterator.GetNextSession(time, isBar);
		}
		if (bars.IsResetOnNewTradingDay || (!bars.IsResetOnNewTradingDay && bars.Count == 0))
		{
			DateTime dateTime = (isBar ? ((BarsType)this).SessionIterator.ActualSessionBegin.AddSeconds(Math.Ceiling(Math.Ceiling(Math.Max(0.0, time.Subtract(((BarsType)this).SessionIterator.ActualSessionBegin).TotalSeconds)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue) : ((BarsType)this).SessionIterator.ActualSessionBegin.AddSeconds((double)bars.BarsPeriod.BaseBarsPeriodValue + Math.Floor(Math.Floor(Math.Max(0.0, time.Subtract(((BarsType)this).SessionIterator.ActualSessionBegin).TotalSeconds)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue));
			if (bars.TradingHours.Sessions.Count > 0 && dateTime > ((BarsType)this).SessionIterator.ActualSessionEnd)
			{
				dateTime = ((BarsType)this).SessionIterator.ActualSessionEnd;
			}
			return dateTime;
		}
		DateTime time2 = bars.GetTime(bars.Count - 1);
		DateTime dateTime2 = (isBar ? time2.AddSeconds(Math.Ceiling(Math.Ceiling(Math.Max(0.0, time.Subtract(time2).TotalSeconds)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue) : time2.AddSeconds((double)bars.BarsPeriod.BaseBarsPeriodValue + Math.Floor(Math.Floor(Math.Max(0.0, time.Subtract(time2).TotalSeconds)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue));
		if (bars.TradingHours.Sessions.Count > 0 && dateTime2 > ((BarsType)this).SessionIterator.ActualSessionEnd)
		{
			DateTime actualSessionEnd = ((BarsType)this).SessionIterator.ActualSessionEnd;
			((BarsType)this).SessionIterator.GetNextSession(((BarsType)this).SessionIterator.ActualSessionEnd.AddSeconds(1.0), isBar);
			dateTime2 = ((BarsType)this).SessionIterator.ActualSessionBegin.AddSeconds((int)dateTime2.Subtract(actualSessionEnd).TotalSeconds);
		}
		return dateTime2;
	}

	private static DateTime TimeToBarTimeWeek(DateTime time, DateTime periodStart, int periodValue)
	{
		return periodStart.Date.AddDays(Math.Ceiling(Math.Ceiling(time.Date.Subtract(periodStart.Date).TotalDays) / (double)(periodValue * 7)) * (double)(periodValue * 7)).Date;
	}

	private static DateTime TimeToBarTimeYear(DateTime time, int periodValue)
	{
		DateTime dateTime = new DateTime(time.Year, 1, 1);
		for (int i = 0; i < periodValue; i++)
		{
			dateTime = dateTime.AddYears(1);
		}
		return dateTime.AddDays(-1.0);
	}
}
