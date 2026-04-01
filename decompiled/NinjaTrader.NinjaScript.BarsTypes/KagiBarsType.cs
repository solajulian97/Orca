using System;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class KagiBarsType : BarsType
{
	private enum Trend
	{
		Up,
		Down,
		Undetermined
	}

	private double anchorPrice = double.MinValue;

	private DateTime cacheSessionEnd = Globals.MinDate;

	private bool endOfBar;

	private DateTime prevTime = Globals.MinDate;

	private double reversalPoint = double.MinValue;

	private int tmpCount;

	private int tmpDayCount;

	private int tmpTickCount;

	private DateTime tmpTime = Globals.MinDate;

	private long tmpVolume;

	private Trend trend = Trend.Undetermined;

	private long volumeCount;

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

	public override void ApplyDefaultValue(BarsPeriod period)
	{
		period.Value = 2;
	}

	private void CalculateKagiBar(Bars bars, double o, double h, double l, double c, DateTime barTime, long volume)
	{
		switch (trend)
		{
		case Trend.Up:
			if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice - reversalPoint) <= 0)
			{
				((BarsType)this).AddBar(bars, anchorPrice, anchorPrice, bars.LastPrice, bars.LastPrice, barTime, volumeCount);
				anchorPrice = bars.LastPrice;
				trend = Trend.Down;
			}
			else if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice) > 0)
			{
				((BarsType)this).UpdateBar(bars, bars.LastPrice, l, bars.LastPrice, barTime, volumeCount);
				anchorPrice = bars.LastPrice;
			}
			else
			{
				((BarsType)this).UpdateBar(bars, h, l, c, barTime, volumeCount);
			}
			break;
		case Trend.Down:
			if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice + reversalPoint) >= 0)
			{
				((BarsType)this).AddBar(bars, anchorPrice, bars.LastPrice, anchorPrice, bars.LastPrice, barTime, volumeCount);
				anchorPrice = bars.LastPrice;
				trend = Trend.Up;
			}
			else if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice) < 0)
			{
				((BarsType)this).UpdateBar(bars, h, bars.LastPrice, bars.LastPrice, barTime, volumeCount);
				anchorPrice = bars.LastPrice;
			}
			else
			{
				((BarsType)this).UpdateBar(bars, h, l, c, barTime, volumeCount);
			}
			break;
		default:
			((BarsType)this).UpdateBar(bars, bars.LastPrice, bars.LastPrice, bars.LastPrice, barTime, volumeCount);
			anchorPrice = bars.LastPrice;
			trend = ((bars.Instrument.MasterInstrument.Compare(bars.LastPrice, o) < 0) ? Trend.Down : ((bars.Instrument.MasterInstrument.Compare(bars.LastPrice, o) <= 0) ? Trend.Undetermined : Trend.Up));
			break;
		}
		volumeCount = volume;
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

	public override object Clone()
	{
		return new KagiBarsType();
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
		return 0.0;
	}

	protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0017: Expected O, but got Unknown
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Expected I4, but got Unknown
		//IL_06a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06aa: Invalid comparison between Unknown and I4
		if (((BarsType)this).SessionIterator == null)
		{
			SessionIterator val = new SessionIterator(bars);
			SessionIterator val2 = val;
			((BarsType)this).SessionIterator = val;
		}
		if (bars.Count != tmpCount)
		{
			if (bars.Count == 0)
			{
				tmpTime = Globals.MinDate;
				tmpVolume = 0L;
				tmpDayCount = 0;
				tmpTickCount = 0;
			}
			else
			{
				tmpTime = bars.GetTime(bars.Count - 1);
				tmpVolume = bars.GetVolume(bars.Count - 1);
				tmpDayCount = bars.DayCount;
				tmpTickCount = bars.BarsPeriod.BaseBarsPeriodValue;
				bars.LastPrice = bars.GetClose(bars.Count - 1);
				anchorPrice = bars.LastPrice;
			}
		}
		bool flag = ((BarsType)this).SessionIterator.IsNewSession(time, isBar);
		bool flag2 = false;
		BarsPeriodType baseBarsPeriodType = bars.BarsPeriod.BaseBarsPeriodType;
		switch ((int)baseBarsPeriodType)
		{
		case 5:
			tmpTime = time.Date;
			if (!isBar && time >= cacheSessionEnd)
			{
				if (flag)
				{
					((BarsType)this).SessionIterator.GetNextSession(time, false);
					flag2 = true;
				}
				cacheSessionEnd = ((BarsType)this).SessionIterator.ActualSessionEnd;
				if (tmpTime < time.Date)
				{
					tmpTime = time.Date;
				}
			}
			if (prevTime != tmpTime)
			{
				tmpDayCount++;
			}
			if (tmpDayCount < bars.BarsPeriod.BaseBarsPeriodValue || (isBar && bars.Count > 0 && tmpTime == bars.LastBarTime.Date) || (!isBar && bars.Count > 0 && tmpTime <= bars.LastBarTime.Date))
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			endOfBar = true;
			break;
		case 4:
			if (tmpTime == Globals.MinDate)
			{
				prevTime = (tmpTime = TimeToBarTimeMinute(bars, time, isBar));
			}
			if ((isBar && time <= tmpTime) || (!isBar && time < tmpTime))
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			tmpTime = TimeToBarTimeMinute(bars, time, isBar);
			endOfBar = true;
			break;
		case 1:
			if (tmpTime == Globals.MinDate)
			{
				tmpVolume = volume;
				endOfBar = tmpVolume >= bars.BarsPeriod.BaseBarsPeriodValue;
				prevTime = (tmpTime = time);
				if (endOfBar)
				{
					tmpVolume = 0L;
				}
				break;
			}
			tmpVolume += volume;
			endOfBar = tmpVolume >= bars.BarsPeriod.BaseBarsPeriodValue;
			if (endOfBar)
			{
				prevTime = tmpTime;
				tmpVolume = 0L;
			}
			tmpTime = time;
			break;
		case 0:
			if (tmpTime == Globals.MinDate || bars.BarsPeriod.BaseBarsPeriodValue == 1)
			{
				prevTime = ((tmpTime == Globals.MinDate) ? time : tmpTime);
				tmpTime = time;
				tmpTickCount = ((bars.BarsPeriod.BaseBarsPeriodValue != 1) ? 1 : 0);
				endOfBar = bars.BarsPeriod.BaseBarsPeriodValue == 1;
			}
			else if (tmpTickCount < bars.BarsPeriod.BaseBarsPeriodValue)
			{
				tmpTime = time;
				endOfBar = false;
				tmpTickCount++;
			}
			else
			{
				prevTime = tmpTime;
				tmpTime = time;
				endOfBar = true;
				tmpTickCount = 1;
			}
			break;
		case 7:
			if (tmpTime == Globals.MinDate)
			{
				prevTime = (tmpTime = TimeToBarTimeMonth(time, bars.BarsPeriod.BaseBarsPeriodValue));
			}
			if ((time.Month <= tmpTime.Month && time.Year == tmpTime.Year) || time.Year < tmpTime.Year)
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			endOfBar = true;
			tmpTime = TimeToBarTimeMonth(time, bars.BarsPeriod.BaseBarsPeriodValue);
			break;
		case 3:
			if (tmpTime == Globals.MinDate)
			{
				prevTime = (tmpTime = TimeToBarTimeSecond(bars, time, isBar));
			}
			if ((bars.BarsPeriod.BaseBarsPeriodValue > 1 && time < tmpTime) || (bars.BarsPeriod.BaseBarsPeriodValue == 1 && time <= tmpTime))
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			tmpTime = TimeToBarTimeSecond(bars, time, isBar);
			endOfBar = true;
			break;
		case 6:
			if (tmpTime == Globals.MinDate)
			{
				prevTime = (tmpTime = TimeToBarTimeWeek(time.Date, tmpTime.Date, bars.BarsPeriod.BaseBarsPeriodValue));
			}
			if (time.Date <= tmpTime.Date)
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			endOfBar = true;
			tmpTime = TimeToBarTimeWeek(time.Date, tmpTime.Date, bars.BarsPeriod.BaseBarsPeriodValue);
			break;
		case 8:
			if (tmpTime == Globals.MinDate)
			{
				prevTime = (tmpTime = TimeToBarTimeYear(time, bars.BarsPeriod.Value));
			}
			if (time.Year <= tmpTime.Year)
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			endOfBar = true;
			tmpTime = TimeToBarTimeYear(time, bars.BarsPeriod.Value);
			break;
		}
		reversalPoint = (((int)bars.BarsPeriod.ReversalType == 1) ? ((double)bars.BarsPeriod.Value * bars.Instrument.MasterInstrument.TickSize) : ((double)bars.BarsPeriod.Value / 100.0 * anchorPrice));
		if (bars.Count == 0 || (((BarsType)this).IsIntraday && bars.IsResetOnNewTradingDay && flag))
		{
			if (flag && !flag2)
			{
				((BarsType)this).SessionIterator.GetNextSession(tmpTime, isBar);
			}
			tmpTickCount = 0;
			if (bars.Count > 0)
			{
				double open2 = bars.GetOpen(bars.Count - 1);
				double high2 = bars.GetHigh(bars.Count - 1);
				double low2 = bars.GetLow(bars.Count - 1);
				double close2 = bars.GetClose(bars.Count - 1);
				if (bars.Count == tmpCount)
				{
					CalculateKagiBar(bars, open2, high2, low2, close2, prevTime, volume);
				}
			}
			((BarsType)this).AddBar(bars, close, close, close, close, tmpTime, volume);
			anchorPrice = close;
			trend = Trend.Undetermined;
			prevTime = tmpTime;
			volumeCount = 0L;
			bars.LastPrice = close;
			tmpCount = bars.Count;
		}
		else
		{
			double close3 = bars.GetClose(bars.Count - 1);
			double open3 = bars.GetOpen(bars.Count - 1);
			double high3 = bars.GetHigh(bars.Count - 1);
			double low3 = bars.GetLow(bars.Count - 1);
			if (endOfBar)
			{
				CalculateKagiBar(bars, open3, high3, low3, close3, prevTime, volume);
			}
			else
			{
				volumeCount += volume;
			}
			bars.LastPrice = close;
			tmpCount = bars.Count;
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected I4, but got Unknown
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Expected I4, but got Unknown
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Invalid comparison between Unknown and I4
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Invalid comparison between Unknown and I4
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Invalid comparison between Unknown and I4
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Invalid comparison between Unknown and I4
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Invalid comparison between Unknown and I4
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Invalid comparison between Unknown and I4
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Invalid comparison between Unknown and I4
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Invalid comparison between Unknown and I4
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeKagi;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)10
			};
			((BarsType)this).DaysToLoad = 5;
			((BarsType)this).WeeksToLoad = 1;
			((BarsType)this).DefaultChartStyle = (ChartStyleType)5;
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
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiDaily : Resource.GuiDay)} Kagi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 4:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Min Kagi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 7:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiMonthly : Resource.GuiMonth)} Kagi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 3:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiSecond : Resource.GuiSeconds)} Kagi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 0:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Tick Kagi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 1:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Volume Kagi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 6:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiWeekly : Resource.GuiWeeks)} Kagi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 8:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiYearly : Resource.GuiYears)} Kagi{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			}
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
			((BarsType)this).SetPropertyName("Value", Resource.NinjaScriptBarsTypeKagiReversal);
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

	private static DateTime TimeToBarTimeYear(DateTime time, double periodValue)
	{
		DateTime dateTime = new DateTime(time.Year, 1, 1);
		for (int i = 0; (double)i < periodValue; i++)
		{
			dateTime = dateTime.AddYears(1);
		}
		return dateTime.AddDays(-1.0);
	}
}
