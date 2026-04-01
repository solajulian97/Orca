using System;
using System.Globalization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class DayBarsType : BarsType
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
		return time.ToString(DateTimeFormatInfo.CurrentInfo.ShortDatePattern);
	}

	public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
	{
		return (int)Math.Ceiling((double)(barsPeriod.Value * barsBack) * 7.0 / 4.5);
	}

	public override double GetPercentComplete(Bars bars, DateTime now)
	{
		if (((BarsType)this).SessionIterator == null || ((BarsType)this).SessionIterator.ActualTradingDayExchange == Globals.MinDate)
		{
			return 1.0;
		}
		DateTime tradingDayBeginLocal = ((BarsType)this).SessionIterator.GetTradingDayBeginLocal(((BarsType)this).SessionIterator.ActualTradingDayExchange);
		if (!(now > tradingDayBeginLocal) || !(now < ((BarsType)this).SessionIterator.ActualTradingDayEndLocal))
		{
			return 1.0;
		}
		return now.Subtract(tradingDayBeginLocal).TotalSeconds / ((BarsType)this).SessionIterator.ActualTradingDayEndLocal.Subtract(tradingDayBeginLocal).TotalSeconds;
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
				((BarsType)this).AddBar(bars, open, high, low, close, time.Date, volume);
				return;
			}
			((BarsType)this).SessionIterator.CalculateTradingDay(time, false);
			((BarsType)this).AddBar(bars, open, high, low, close, ((BarsType)this).SessionIterator.ActualTradingDayExchange, volume);
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
		if (bars.DayCount < bars.BarsPeriod.Value || (isBar && bars.Count > 0 && dateTime == bars.LastBarTime.Date) || (!isBar && bars.Count > 0 && dateTime <= bars.LastBarTime.Date))
		{
			((BarsType)this).UpdateBar(bars, high, low, close, dateTime, volume);
		}
		else
		{
			((BarsType)this).AddBar(bars, open, high, low, close, dateTime, volume);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Invalid comparison between Unknown and I4
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeDay;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)5
			};
			((BarsType)this).BuiltFrom = (BarsPeriodType)5;
			((BarsType)this).DaysToLoad = 365;
			((BarsType)this).WeeksToLoad = 52;
			((BarsType)this).MonthsToLoad = 12;
			((BarsType)this).YearsToLoad = 1;
			((BarsType)this).IsIntraday = false;
			((BarsType)this).IsTimeBased = true;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScript)this).Name = ((((BarsType)this).BarsPeriod.Value == 1) ? Resource.DataBarsTypeDaily : string.Format(Globals.GeneralOptions.CurrentCulture, Resource.DataBarsTypeDay, ((BarsType)this).BarsPeriod.Value)) + (((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? (" - " + Globals.ToLocalizedObject((object)((BarsType)this).BarsPeriod.MarketDataType, Globals.GeneralOptions.CurrentUICulture)) : string.Empty);
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodValue", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
		}
	}
}
