using System;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class MinuteBarsType : BarsType
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
		return time.ToString("HH:mm");
	}

	public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
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
		return (int)Math.Max(1.0, Math.Ceiling((double)barsBack / Math.Max(1.0, (double)num / 7.0 / (double)barsPeriod.Value) * 1.05));
	}

	public override double GetPercentComplete(Bars bars, DateTime now)
	{
		if (!(now <= bars.LastBarTime))
		{
			return 1.0;
		}
		return 1.0 - bars.LastBarTime.Subtract(now).TotalMinutes / (double)bars.BarsPeriod.Value;
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
			((BarsType)this).AddBar(bars, open, high, low, close, TimeToBarTime(bars, time, isBar), volume);
			return;
		}
		if (!isBar && time < bars.LastBarTime)
		{
			((BarsType)this).UpdateBar(bars, high, low, close, bars.LastBarTime, volume);
			return;
		}
		if (isBar && time <= bars.LastBarTime)
		{
			((BarsType)this).UpdateBar(bars, high, low, close, bars.LastBarTime, volume);
			return;
		}
		time = TimeToBarTime(bars, time, isBar);
		((BarsType)this).AddBar(bars, open, high, low, close, time, volume);
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Invalid comparison between Unknown and I4
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeMinute;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)4,
				Value = 1
			};
			((BarsType)this).BuiltFrom = (BarsPeriodType)4;
			((BarsType)this).DaysToLoad = 5;
			((BarsType)this).WeeksToLoad = 1;
			((BarsType)this).IsIntraday = true;
			((BarsType)this).IsTimeBased = true;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScript)this).Name = string.Format(Globals.GeneralOptions.CurrentCulture, Resource.DataBarsTypeMinute, ((BarsType)this).BarsPeriod.Value, ((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? (" - " + Globals.ToLocalizedObject((object)((BarsType)this).BarsPeriod.MarketDataType, Globals.GeneralOptions.CurrentUICulture)) : string.Empty);
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodValue", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
		}
	}

	private DateTime TimeToBarTime(Bars bars, DateTime time, bool isBar)
	{
		if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
		{
			((BarsType)this).SessionIterator.GetNextSession(time, isBar);
		}
		if (bars.IsResetOnNewTradingDay || (!bars.IsResetOnNewTradingDay && bars.Count == 0))
		{
			DateTime dateTime = (isBar ? ((BarsType)this).SessionIterator.ActualSessionBegin.AddMinutes(Math.Ceiling(Math.Ceiling(Math.Max(0.0, time.Subtract(((BarsType)this).SessionIterator.ActualSessionBegin).TotalMinutes)) / (double)bars.BarsPeriod.Value) * (double)bars.BarsPeriod.Value) : ((BarsType)this).SessionIterator.ActualSessionBegin.AddMinutes((double)bars.BarsPeriod.Value + Math.Floor(Math.Floor(Math.Max(0.0, time.Subtract(((BarsType)this).SessionIterator.ActualSessionBegin).TotalMinutes)) / (double)bars.BarsPeriod.Value) * (double)bars.BarsPeriod.Value));
			if (bars.TradingHours.Sessions.Count > 0 && dateTime > ((BarsType)this).SessionIterator.ActualSessionEnd)
			{
				dateTime = ((BarsType)this).SessionIterator.ActualSessionEnd;
			}
			return dateTime;
		}
		DateTime time2 = bars.GetTime(bars.Count - 1);
		DateTime dateTime2 = (isBar ? time2.AddMinutes(Math.Ceiling(Math.Ceiling(Math.Max(0.0, time.Subtract(time2).TotalMinutes)) / (double)bars.BarsPeriod.Value) * (double)bars.BarsPeriod.Value) : time2.AddMinutes((double)bars.BarsPeriod.Value + Math.Floor(Math.Floor(Math.Max(0.0, time.Subtract(time2).TotalMinutes)) / (double)bars.BarsPeriod.Value) * (double)bars.BarsPeriod.Value));
		if (bars.TradingHours.Sessions.Count > 0 && dateTime2 > ((BarsType)this).SessionIterator.ActualSessionEnd)
		{
			DateTime actualSessionEnd = ((BarsType)this).SessionIterator.ActualSessionEnd;
			((BarsType)this).SessionIterator.GetNextSession(((BarsType)this).SessionIterator.ActualSessionEnd.AddSeconds(1.0), isBar);
			dateTime2 = ((BarsType)this).SessionIterator.ActualSessionBegin.AddMinutes((int)dateTime2.Subtract(actualSessionEnd).TotalMinutes);
		}
		return dateTime2;
	}
}
