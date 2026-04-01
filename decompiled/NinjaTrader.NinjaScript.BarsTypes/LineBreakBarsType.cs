using System;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class LineBreakBarsType : BarsType
{
	private double anchorPrice = double.MinValue;

	private bool firstBarOfSession = true;

	private bool newSession;

	private int newSessionIdx;

	private double switchPrice = double.MinValue;

	private int tmpCount;

	private int tmpDayCount;

	private int tmpTickCount;

	private DateTime tmpTime = Globals.MinDate;

	private long tmpVolume;

	private bool upTrend = true;

	public override bool IsRemoveLastBarSupported => true;

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
			((BarsType)this).MonthsToLoad = 1;
			((BarsType)this).YearsToLoad = 0;
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
		period.Value = 3;
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
		return (int)baseBarsPeriodType switch
		{
			5 => ((BarsType)new DayBarsType()).GetInitialLookBackDays(barsPeriod, tradingHours, barsBack), 
			4 => ((BarsType)new MinuteBarsType()).GetInitialLookBackDays(barsPeriod, tradingHours, barsBack), 
			7 => ((BarsType)new MonthBarsType()).GetInitialLookBackDays(barsPeriod, tradingHours, barsBack), 
			3 => ((BarsType)new SecondBarsType()).GetInitialLookBackDays(barsPeriod, tradingHours, barsBack), 
			0 => ((BarsType)new TickBarsType()).GetInitialLookBackDays(barsPeriod, tradingHours, barsBack), 
			1 => ((BarsType)new VolumeBarsType()).GetInitialLookBackDays(barsPeriod, tradingHours, barsBack), 
			6 => ((BarsType)new WeekBarsType()).GetInitialLookBackDays(barsPeriod, tradingHours, barsBack), 
			8 => ((BarsType)new YearBarsType()).GetInitialLookBackDays(barsPeriod, tradingHours, barsBack), 
			_ => ((BarsType)new MinuteBarsType()).GetInitialLookBackDays(barsPeriod, tradingHours, barsBack), 
		};
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
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected I4, but got Unknown
		//IL_05fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0603: Invalid comparison between Unknown and I4
		if (((BarsType)this).SessionIterator == null)
		{
			SessionIterator val = new SessionIterator(bars);
			SessionIterator val2 = val;
			((BarsType)this).SessionIterator = val;
		}
		if (bars.Count == 0 && tmpTime != Globals.MinDate)
		{
			tmpTime = Globals.MinDate;
		}
		bool flag = true;
		if (tmpTime == Globals.MinDate)
		{
			tmpTime = time;
			tmpDayCount = 1;
			tmpTickCount = 1;
		}
		else if (bars.Count < tmpCount && bars.Count == 0)
		{
			tmpTime = Globals.MinDate;
			tmpVolume = 0L;
			tmpDayCount = 0;
			tmpTickCount = 0;
		}
		else if (bars.Count < tmpCount && bars.Count > 0)
		{
			tmpTime = bars.GetTime(bars.Count - 1);
			tmpVolume = bars.GetVolume(bars.Count - 1);
			tmpTickCount = bars.TickCount;
			tmpDayCount = bars.DayCount;
		}
		BarsPeriodType baseBarsPeriodType = ((BarsType)this).BarsPeriod.BaseBarsPeriodType;
		switch ((int)baseBarsPeriodType)
		{
		case 5:
			if (bars.Count == 0 || (bars.Count > 0 && (bars.LastBarTime.Month < time.Month || bars.LastBarTime.Year < time.Year)))
			{
				tmpTime = time.Date;
				bars.LastPrice = close;
				newSession = true;
				break;
			}
			tmpTime = time.Date;
			tmpVolume += volume;
			bars.LastPrice = close;
			tmpDayCount++;
			if (tmpDayCount < ((BarsType)this).BarsPeriod.BaseBarsPeriodValue || (bars.Count > 0 && bars.LastBarTime.Date == time.Date))
			{
				flag = false;
			}
			break;
		case 4:
			if (bars.Count == 0 || (((BarsType)this).SessionIterator.IsNewSession(time, isBar) && bars.IsResetOnNewTradingDay))
			{
				tmpTime = TimeToBarTimeMinute(bars, time, isBar);
				newSession = true;
				tmpVolume = 0L;
				break;
			}
			if ((!isBar && time < bars.LastBarTime) || (isBar && time <= bars.LastBarTime))
			{
				tmpTime = bars.LastBarTime;
				flag = false;
			}
			else
			{
				tmpTime = TimeToBarTimeMinute(bars, time, isBar);
			}
			tmpVolume += volume;
			break;
		case 7:
			if (tmpTime == Globals.MinDate)
			{
				tmpTime = TimeToBarTimeMonth(time, ((BarsType)this).BarsPeriod.BaseBarsPeriodValue);
				if (bars.Count != 0)
				{
					flag = false;
				}
			}
			else if ((time.Month <= tmpTime.Month && time.Year == tmpTime.Year) || time.Year < tmpTime.Year)
			{
				tmpVolume += volume;
				bars.LastPrice = close;
				flag = false;
			}
			break;
		case 3:
			if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
			{
				tmpTime = TimeToBarTimeSecond(bars, time, isBar);
				if (bars.Count != 0)
				{
					flag = false;
					newSession = true;
				}
			}
			else if (time <= tmpTime)
			{
				tmpVolume += volume;
				bars.LastPrice = close;
				flag = false;
			}
			else
			{
				tmpTime = TimeToBarTimeSecond(bars, time, isBar);
			}
			break;
		case 0:
			if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
			{
				((BarsType)this).SessionIterator.GetNextSession(time, isBar);
				newSession = true;
				tmpTime = time;
				tmpTickCount = 1;
				if (bars.Count != 0)
				{
					flag = false;
				}
			}
			else if (((BarsType)this).BarsPeriod.BaseBarsPeriodValue > 1 && tmpTickCount < ((BarsType)this).BarsPeriod.BaseBarsPeriodValue)
			{
				tmpTime = time;
				tmpVolume += volume;
				tmpTickCount++;
				bars.LastPrice = close;
				flag = false;
			}
			else
			{
				tmpTime = time;
			}
			break;
		case 1:
			if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
			{
				((BarsType)this).SessionIterator.GetNextSession(time, isBar);
				newSession = true;
			}
			else
			{
				if (bars.Count == 0 && volume > 0)
				{
					break;
				}
				tmpVolume += volume;
				if (tmpVolume < ((BarsType)this).BarsPeriod.BaseBarsPeriodValue)
				{
					bars.LastPrice = close;
					flag = false;
				}
				else if (tmpVolume == 0L)
				{
					flag = false;
				}
			}
			tmpTime = time;
			break;
		case 6:
			if (tmpTime == Globals.MinDate)
			{
				tmpTime = TimeToBarTimeWeek(time.Date, tmpTime.Date, ((BarsType)this).BarsPeriod.BaseBarsPeriodValue);
				if (bars.Count != 0)
				{
					flag = false;
				}
			}
			else if (time.Date <= tmpTime.Date)
			{
				tmpVolume += volume;
				bars.LastPrice = close;
				flag = false;
			}
			break;
		case 8:
			if (tmpTime == Globals.MinDate)
			{
				tmpTime = TimeToBarTimeYear(time, ((BarsType)this).BarsPeriod.BaseBarsPeriodValue);
				if (bars.Count != 0)
				{
					flag = false;
				}
			}
			else if (time.Year <= tmpTime.Year)
			{
				tmpVolume += volume;
				bars.LastPrice = close;
				flag = false;
			}
			break;
		}
		if (bars.Count > 0 && tmpTime < bars.GetTime(bars.Count - 1) && (int)((BarsType)this).BarsPeriod.BaseBarsPeriodType == 3)
		{
			tmpTime = bars.GetTime(bars.Count - 1);
		}
		if (bars.Count == 0 || (newSession && ((BarsType)this).IsIntraday))
		{
			((BarsType)this).AddBar(bars, open, close, close, close, tmpTime, volume);
			upTrend = open < close;
			newSessionIdx = bars.Count - 1;
			newSession = false;
			firstBarOfSession = true;
			anchorPrice = close;
			switchPrice = open;
		}
		else if (firstBarOfSession && !flag)
		{
			double open2 = bars.GetOpen(bars.Count - 1);
			((BarsType)this).RemoveLastBar(bars);
			if (((BarsType)this).SessionIterator.IsNewSession(tmpTime, true))
			{
				((BarsType)this).SessionIterator.GetNextSession(tmpTime, true);
			}
			((BarsType)this).AddBar(bars, open2, close, close, close, tmpTime, tmpVolume);
			upTrend = open2 < close;
			anchorPrice = close;
		}
		else
		{
			int num = ((BarsType)this).BarsPeriod.Value;
			double num2 = double.MinValue;
			double num3 = double.MaxValue;
			if (firstBarOfSession)
			{
				((BarsType)this).AddBar(bars, anchorPrice, close, close, close, tmpTime, volume);
				firstBarOfSession = false;
				tmpVolume = volume;
				tmpTime = Globals.MinDate;
				return;
			}
			if (bars.Count - newSessionIdx - 1 < num)
			{
				num = bars.Count - (newSessionIdx + 1);
			}
			for (int i = 1; i <= num; i++)
			{
				num2 = Math.Max(num2, bars.GetOpen(bars.Count - i - 1));
				num2 = Math.Max(num2, bars.GetClose(bars.Count - i - 1));
				num3 = Math.Min(num3, bars.GetOpen(bars.Count - i - 1));
				num3 = Math.Min(num3, bars.GetClose(bars.Count - i - 1));
			}
			bars.LastPrice = close;
			if (upTrend)
			{
				if (flag)
				{
					bool flag2 = false;
					if (bars.Instrument.MasterInstrument.Compare(bars.GetClose(bars.Count - 1), anchorPrice) > 0)
					{
						anchorPrice = bars.GetClose(bars.Count - 1);
						switchPrice = bars.GetOpen(bars.Count - 1);
						tmpVolume = volume;
						flag2 = true;
					}
					else if (bars.Instrument.MasterInstrument.Compare(num3, bars.GetClose(bars.Count - 1)) > 0)
					{
						anchorPrice = bars.GetClose(bars.Count - 1);
						switchPrice = bars.GetOpen(bars.Count - 1);
						tmpVolume = volume;
						upTrend = false;
						flag2 = true;
					}
					if (flag2)
					{
						double num4 = (upTrend ? Math.Min(Math.Max(switchPrice, close), anchorPrice) : Math.Max(Math.Min(switchPrice, close), anchorPrice));
						((BarsType)this).AddBar(bars, num4, close, close, close, tmpTime, volume);
					}
					else
					{
						((BarsType)this).RemoveLastBar(bars);
						double num5 = Math.Min(Math.Max(switchPrice, close), anchorPrice);
						if (((BarsType)this).SessionIterator.IsNewSession(tmpTime, true))
						{
							((BarsType)this).SessionIterator.GetNextSession(tmpTime, true);
						}
						((BarsType)this).AddBar(bars, num5, close, close, close, tmpTime, tmpVolume);
					}
				}
				else
				{
					((BarsType)this).RemoveLastBar(bars);
					double num6 = Math.Min(Math.Max(switchPrice, close), anchorPrice);
					if (((BarsType)this).SessionIterator.IsNewSession(tmpTime, true))
					{
						((BarsType)this).SessionIterator.GetNextSession(tmpTime, true);
					}
					((BarsType)this).AddBar(bars, num6, close, close, close, tmpTime, tmpVolume);
				}
			}
			else if (flag)
			{
				bool flag3 = false;
				if (bars.Instrument.MasterInstrument.Compare(bars.GetClose(bars.Count - 1), anchorPrice) < 0)
				{
					anchorPrice = bars.GetClose(bars.Count - 1);
					switchPrice = bars.GetOpen(bars.Count - 1);
					tmpVolume = volume;
					flag3 = true;
				}
				else if (bars.Instrument.MasterInstrument.Compare(num2, bars.GetClose(bars.Count - 1)) < 0)
				{
					anchorPrice = bars.GetClose(bars.Count - 1);
					switchPrice = bars.GetOpen(bars.Count - 1);
					tmpVolume = volume;
					upTrend = true;
					flag3 = true;
				}
				if (flag3)
				{
					double num7 = (upTrend ? Math.Min(Math.Max(switchPrice, close), anchorPrice) : Math.Max(Math.Min(switchPrice, close), anchorPrice));
					((BarsType)this).AddBar(bars, num7, close, close, close, tmpTime, volume);
				}
				else
				{
					((BarsType)this).RemoveLastBar(bars);
					double num8 = Math.Max(Math.Min(switchPrice, close), anchorPrice);
					if (((BarsType)this).SessionIterator.IsNewSession(tmpTime, true))
					{
						((BarsType)this).SessionIterator.GetNextSession(tmpTime, true);
					}
					((BarsType)this).AddBar(bars, num8, close, close, close, tmpTime, tmpVolume);
				}
			}
			else
			{
				((BarsType)this).RemoveLastBar(bars);
				double num9 = Math.Max(Math.Min(switchPrice, close), anchorPrice);
				if (((BarsType)this).SessionIterator.IsNewSession(tmpTime, true))
				{
					((BarsType)this).SessionIterator.GetNextSession(tmpTime, true);
				}
				((BarsType)this).AddBar(bars, num9, close, close, close, tmpTime, tmpVolume);
			}
		}
		if (flag)
		{
			tmpTime = Globals.MinDate;
		}
		tmpCount = bars.Count;
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
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeLineBreak;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)13
			};
			((BarsType)this).DaysToLoad = 5;
			((BarsType)this).WeeksToLoad = 1;
			((BarsType)this).DefaultChartStyle = (ChartStyleType)6;
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
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiDaily : Resource.GuiDay)} LineBreak{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 4:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Min LineBreak{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 7:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiMonthly : Resource.GuiMonth)} LineBreak{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 3:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiSecond : Resource.GuiSeconds)} LineBreak{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 0:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Tick LineBreak{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 1:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Volume LineBreak{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 6:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiWeekly : Resource.GuiWeeks)} LineBreak{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 8:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiYearly : Resource.GuiYears)} LineBreak{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			}
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
			((BarsType)this).SetPropertyName("Value", Resource.NinjaScriptBarsTypeLineBreakLineBreaks);
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
