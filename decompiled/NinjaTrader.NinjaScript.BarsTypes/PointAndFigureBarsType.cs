using System;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class PointAndFigureBarsType : BarsType
{
	private enum Trend
	{
		Up,
		Down,
		Undetermined
	}

	private double anchorPrice = double.MinValue;

	private double boxSize = double.MinValue;

	private bool endOfBar;

	private DateTime prevTime = Globals.MinDate;

	private DateTime prevTimeD = Globals.MinDate;

	private double reversalSize = double.MinValue;

	private int tmpCount;

	private int tmpDayCount;

	private double tmpHigh = double.MinValue;

	private double tmpLow = double.MinValue;

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
		period.Value2 = 3;
	}

	private void CalculatePfBar(Bars bars, double h, double l, double c, DateTime barTime, DateTime tTime)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((BarsType)this).BarsPeriod.PointAndFigurePriceType == 0)
		{
			switch (trend)
			{
			case Trend.Up:
				if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice - reversalSize) <= 0)
				{
					double num4 = anchorPrice - boxSize;
					double num5 = anchorPrice - reversalSize;
					while (bars.Instrument.MasterInstrument.Compare(num5 - boxSize, bars.LastPrice) >= 0)
					{
						num5 -= boxSize;
					}
					num4 = bars.Instrument.MasterInstrument.RoundToTickSize(num4);
					num5 = (anchorPrice = bars.Instrument.MasterInstrument.RoundToTickSize(num5));
					trend = Trend.Down;
					((BarsType)this).AddBar(bars, num4, num4, num5, num5, barTime, volumeCount);
				}
				else if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice + boxSize) >= 0)
				{
					double num6;
					for (num6 = anchorPrice + boxSize; bars.Instrument.MasterInstrument.Compare(bars.LastPrice, num6 + boxSize) >= 0; num6 += boxSize)
					{
					}
					num6 = (anchorPrice = bars.Instrument.MasterInstrument.RoundToTickSize(num6));
					((BarsType)this).UpdateBar(bars, num6, l, num6, barTime, volumeCount);
				}
				else
				{
					((BarsType)this).UpdateBar(bars, h, l, c, barTime, volumeCount);
				}
				return;
			case Trend.Down:
				if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice + reversalSize) >= 0)
				{
					double num = anchorPrice + boxSize;
					double num2;
					for (num2 = anchorPrice + reversalSize; bars.Instrument.MasterInstrument.Compare(bars.LastPrice, num2 + boxSize) >= 0; num2 += boxSize)
					{
					}
					num2 = bars.Instrument.MasterInstrument.RoundToTickSize(num2);
					num = bars.Instrument.MasterInstrument.RoundToTickSize(num);
					anchorPrice = num2;
					trend = Trend.Up;
					((BarsType)this).AddBar(bars, num, num2, num, num2, barTime, volumeCount);
				}
				else if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice - boxSize) <= 0)
				{
					double num3 = anchorPrice - boxSize;
					while (bars.Instrument.MasterInstrument.Compare(num3 - boxSize, bars.LastPrice) >= 0)
					{
						num3 -= boxSize;
					}
					num3 = (anchorPrice = bars.Instrument.MasterInstrument.RoundToTickSize(num3));
					((BarsType)this).UpdateBar(bars, h, num3, num3, barTime, volumeCount);
				}
				else
				{
					((BarsType)this).UpdateBar(bars, h, l, c, barTime, volumeCount);
				}
				return;
			}
			if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice + boxSize) >= 0)
			{
				double num7;
				for (num7 = anchorPrice + boxSize; bars.Instrument.MasterInstrument.Compare(bars.LastPrice, num7 + boxSize) >= 0; num7 += boxSize)
				{
				}
				num7 = (anchorPrice = bars.Instrument.MasterInstrument.RoundToTickSize(num7));
				trend = Trend.Up;
				((BarsType)this).UpdateBar(bars, num7, l, num7, barTime, volumeCount);
			}
			else if (bars.Instrument.MasterInstrument.Compare(anchorPrice - boxSize, bars.LastPrice) >= 0)
			{
				double num8 = anchorPrice - boxSize;
				while (bars.Instrument.MasterInstrument.Compare(num8 - boxSize, bars.LastPrice) >= 0)
				{
					num8 -= boxSize;
				}
				num8 = (anchorPrice = bars.Instrument.MasterInstrument.RoundToTickSize(num8));
				trend = Trend.Down;
				((BarsType)this).UpdateBar(bars, h, num8, num8, barTime, volumeCount);
			}
			else
			{
				((BarsType)this).UpdateBar(bars, anchorPrice, anchorPrice, anchorPrice, barTime, volumeCount);
			}
			return;
		}
		switch (trend)
		{
		case Trend.Up:
		{
			bool flag2 = false;
			if (bars.Instrument.MasterInstrument.Compare(tmpHigh, anchorPrice + boxSize) >= 0)
			{
				double num13;
				for (num13 = anchorPrice; bars.Instrument.MasterInstrument.Compare(tmpHigh, num13 + boxSize) >= 0; num13 += boxSize)
				{
				}
				num13 = bars.Instrument.MasterInstrument.RoundToTickSize(num13);
				flag2 = true;
				anchorPrice = num13;
				long num14 = ((bars.Instrument.MasterInstrument.Compare(anchorPrice - reversalSize, tmpLow) >= 0) ? 0 : volumeCount);
				DateTime dateTime2 = ((bars.Instrument.MasterInstrument.Compare(anchorPrice - reversalSize, tmpLow) >= 0) ? tTime : barTime);
				((BarsType)this).UpdateBar(bars, num13, l, num13, dateTime2, num14);
			}
			if (bars.Instrument.MasterInstrument.Compare(anchorPrice - reversalSize, tmpLow) >= 0)
			{
				double num15 = anchorPrice - boxSize;
				double num16 = anchorPrice - reversalSize;
				while (bars.Instrument.MasterInstrument.Compare(num16 - boxSize, tmpLow) >= 0)
				{
					num16 -= boxSize;
				}
				num15 = bars.Instrument.MasterInstrument.RoundToTickSize(num15);
				num16 = bars.Instrument.MasterInstrument.RoundToTickSize(num16);
				flag2 = true;
				anchorPrice = num16;
				trend = Trend.Down;
				((BarsType)this).AddBar(bars, num15, num15, num16, num16, barTime, volumeCount);
			}
			if (!flag2)
			{
				((BarsType)this).UpdateBar(bars, h, l, c, barTime, volumeCount);
				anchorPrice = h;
			}
			return;
		}
		case Trend.Down:
		{
			bool flag = false;
			if (bars.Instrument.MasterInstrument.Compare(tmpLow, anchorPrice - boxSize) <= 0)
			{
				double num9 = anchorPrice;
				while (bars.Instrument.MasterInstrument.Compare(num9 - boxSize, tmpLow) >= 0)
				{
					num9 -= boxSize;
				}
				num9 = bars.Instrument.MasterInstrument.RoundToTickSize(num9);
				flag = true;
				anchorPrice = num9;
				long num10 = ((bars.Instrument.MasterInstrument.Compare(tmpHigh, anchorPrice + reversalSize) >= 0) ? 0 : volumeCount);
				DateTime dateTime = ((bars.Instrument.MasterInstrument.Compare(anchorPrice - reversalSize, tmpLow) >= 0) ? tTime : barTime);
				((BarsType)this).UpdateBar(bars, h, num9, num9, dateTime, num10);
			}
			if (bars.Instrument.MasterInstrument.Compare(tmpHigh, anchorPrice + reversalSize) >= 0)
			{
				double num11 = anchorPrice + boxSize;
				double num12;
				for (num12 = anchorPrice + reversalSize; bars.Instrument.MasterInstrument.Compare(tmpHigh, num12 + boxSize) >= 0; num12 += boxSize)
				{
				}
				num12 = bars.Instrument.MasterInstrument.RoundToTickSize(num12);
				num11 = bars.Instrument.MasterInstrument.RoundToTickSize(num11);
				flag = true;
				anchorPrice = num12;
				trend = Trend.Up;
				((BarsType)this).AddBar(bars, num11, num12, num11, num12, barTime, volumeCount);
			}
			if (!flag)
			{
				((BarsType)this).UpdateBar(bars, h, l, c, barTime, volumeCount);
				anchorPrice = l;
			}
			return;
		}
		}
		if (bars.Instrument.MasterInstrument.Compare(bars.LastPrice, anchorPrice + boxSize) >= 0)
		{
			double num17;
			for (num17 = anchorPrice + boxSize; bars.Instrument.MasterInstrument.Compare(bars.LastPrice, num17 + boxSize) >= 0; num17 += boxSize)
			{
			}
			num17 = (anchorPrice = bars.Instrument.MasterInstrument.RoundToTickSize(num17));
			trend = Trend.Up;
			((BarsType)this).UpdateBar(bars, num17, l, num17, barTime, volumeCount);
		}
		else if (bars.Instrument.MasterInstrument.Compare(anchorPrice - boxSize, bars.LastPrice) >= 0)
		{
			double num18 = anchorPrice - boxSize;
			while (bars.Instrument.MasterInstrument.Compare(num18 - boxSize, bars.LastPrice) >= 0)
			{
				num18 -= boxSize;
			}
			num18 = (anchorPrice = bars.Instrument.MasterInstrument.RoundToTickSize(num18));
			trend = Trend.Down;
			((BarsType)this).UpdateBar(bars, h, num18, num18, barTime, volumeCount);
		}
		else
		{
			((BarsType)this).UpdateBar(bars, anchorPrice, anchorPrice, anchorPrice, barTime, volumeCount);
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
		//IL_0012: Expected O, but got Unknown
		//IL_0017: Expected O, but got Unknown
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected I4, but got Unknown
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
				tmpTickCount = bars.TickCount;
				tmpDayCount = bars.DayCount;
				bars.LastPrice = (anchorPrice = bars.GetClose(bars.Count - 1));
			}
		}
		BarsPeriodType baseBarsPeriodType = ((BarsType)this).BarsPeriod.BaseBarsPeriodType;
		switch ((int)baseBarsPeriodType)
		{
		case 5:
			tmpTime = time.Date;
			if (!isBar)
			{
				tmpDayCount++;
				if (tmpTime < time.Date)
				{
					tmpTime = time.Date;
				}
			}
			if (isBar && prevTimeD != tmpTime)
			{
				tmpDayCount++;
			}
			if ((isBar && bars.Count > 0 && tmpTime == bars.LastBarTime.Date) || (!isBar && bars.Count > 0 && tmpTime <= bars.LastBarTime.Date) || tmpDayCount < ((BarsType)this).BarsPeriod.BaseBarsPeriodValue)
			{
				endOfBar = false;
				break;
			}
			prevTime = ((prevTimeD == Globals.MinDate) ? tmpTime : prevTimeD);
			prevTimeD = tmpTime;
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
				endOfBar = tmpVolume >= ((BarsType)this).BarsPeriod.BaseBarsPeriodValue;
				prevTime = (tmpTime = time);
				if (endOfBar)
				{
					tmpVolume = 0L;
				}
				break;
			}
			tmpVolume += volume;
			endOfBar = tmpVolume >= ((BarsType)this).BarsPeriod.BaseBarsPeriodValue;
			if (endOfBar)
			{
				prevTime = tmpTime;
				tmpVolume = 0L;
				tmpTime = time;
			}
			break;
		case 7:
			if (tmpTime == Globals.MinDate)
			{
				prevTime = (tmpTime = TimeToBarTimeMonth(time, ((BarsType)this).BarsPeriod.BaseBarsPeriodValue));
			}
			if ((time.Month <= tmpTime.Month && time.Year == tmpTime.Year) || time.Year < tmpTime.Year)
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			endOfBar = true;
			tmpTime = TimeToBarTimeMonth(time, ((BarsType)this).BarsPeriod.BaseBarsPeriodValue);
			break;
		case 3:
			if (tmpTime == Globals.MinDate)
			{
				prevTime = (tmpTime = TimeToBarTimeSecond(bars, time, isBar));
			}
			if (time <= tmpTime)
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			tmpTime = TimeToBarTimeSecond(bars, time, isBar);
			endOfBar = true;
			break;
		case 0:
			if (tmpTime == Globals.MinDate || ((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1)
			{
				prevTime = tmpTime;
				if (prevTime == Globals.MinDate)
				{
					prevTime = time;
				}
				tmpTime = time;
				endOfBar = ((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1;
			}
			else if (tmpTickCount < ((BarsType)this).BarsPeriod.BaseBarsPeriodValue)
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
		case 6:
			if (tmpTime == Globals.MinDate)
			{
				prevTime = (tmpTime = TimeToBarTimeWeek(time.Date, tmpTime.Date, ((BarsType)this).BarsPeriod.BaseBarsPeriodValue));
			}
			if (time.Date <= tmpTime.Date)
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			endOfBar = true;
			tmpTime = TimeToBarTimeWeek(time.Date, tmpTime.Date, ((BarsType)this).BarsPeriod.BaseBarsPeriodValue);
			break;
		case 8:
			if (tmpTime == Globals.MinDate)
			{
				prevTime = (tmpTime = TimeToBarTimeYear(time, ((BarsType)this).BarsPeriod.BaseBarsPeriodValue));
			}
			if (time.Year <= tmpTime.Year)
			{
				endOfBar = false;
				break;
			}
			prevTime = tmpTime;
			endOfBar = true;
			tmpTime = TimeToBarTimeYear(time, ((BarsType)this).BarsPeriod.BaseBarsPeriodValue);
			break;
		}
		double tickSize = bars.Instrument.MasterInstrument.TickSize;
		boxSize = Math.Floor(10000000.0 * (double)((BarsType)this).BarsPeriod.Value * tickSize) / 10000000.0;
		reversalSize = (double)((BarsType)this).BarsPeriod.Value2 * boxSize;
		if (bars.Count == 0 || (((BarsType)this).IsIntraday && bars.IsResetOnNewTradingDay && flag))
		{
			if (bars.Count > 0)
			{
				double high2 = bars.GetHigh(bars.Count - 1);
				double low2 = bars.GetLow(bars.Count - 1);
				double close2 = bars.GetClose(bars.Count - 1);
				DateTime time2 = bars.GetTime(bars.Count - 1);
				bars.LastPrice = (anchorPrice = close2);
				if (bars.Count == tmpCount)
				{
					CalculatePfBar(bars, high2, low2, close2, (prevTime == Globals.MinDate) ? time : prevTime, time2);
				}
			}
			((BarsType)this).AddBar(bars, close, close, close, close, tmpTime, volume);
			anchorPrice = close;
			trend = Trend.Undetermined;
			prevTime = tmpTime;
			volumeCount = 0L;
			bars.LastPrice = close;
			tmpCount = bars.Count;
			tmpHigh = high;
			tmpLow = low;
		}
		else
		{
			double close3 = bars.GetClose(bars.Count - 1);
			double high3 = bars.GetHigh(bars.Count - 1);
			double low3 = bars.GetLow(bars.Count - 1);
			DateTime time3 = bars.GetTime(bars.Count - 1);
			if (endOfBar)
			{
				CalculatePfBar(bars, high3, low3, close3, prevTime, time3);
				volumeCount = volume;
				tmpHigh = high;
				tmpLow = low;
			}
			else
			{
				tmpHigh = ((high > tmpHigh) ? high : tmpHigh);
				tmpLow = ((low < tmpLow) ? low : tmpLow);
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
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypePointAndFigure;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)12
			};
			((BarsType)this).DaysToLoad = 5;
			((BarsType)this).WeeksToLoad = 1;
			((BarsType)this).DefaultChartStyle = (ChartStyleType)4;
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
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiDaily : Resource.GuiDay)} PointAndFigure{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 4:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Min PointAndFigure{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 7:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiMonthly : Resource.GuiMonth)} PointAndFigure{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 3:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiSecond : Resource.GuiSeconds)} PointAndFigure{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 0:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Tick PointAndFigure{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 1:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} Volume PointAndFigure{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 6:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiWeekly : Resource.GuiWeeks)} PointAndFigure{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			case 8:
				((NinjaScript)this).Name = $"{((BarsType)this).BarsPeriod.BaseBarsPeriodValue} {((((BarsType)this).BarsPeriod.BaseBarsPeriodValue == 1) ? Resource.GuiYearly : Resource.GuiYears)} PointAndFigure{(((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {((BarsType)this).BarsPeriod.MarketDataType}" : string.Empty)}";
				break;
			}
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).SetPropertyName("Value", Resource.NinjaScriptBarsTypePointAndFigureBoxSize);
			((BarsType)this).SetPropertyName("Value2", Resource.NinjaScriptBarsTypePointAndFigureReversal);
		}
	}

	private DateTime TimeToBarTimeMinute(Bars bars, DateTime time, bool isBar)
	{
		if (((BarsType)this).SessionIterator.IsNewSession(time, isBar))
		{
			((BarsType)this).SessionIterator.GetNextSession(time, isBar);
		}
		DateTime dateTime = ((!isBar) ? ((BarsType)this).SessionIterator.ActualSessionBegin.AddMinutes((double)bars.BarsPeriod.BaseBarsPeriodValue + Math.Floor(Math.Floor(Math.Max(0.0, time.Subtract(((BarsType)this).SessionIterator.ActualSessionBegin).TotalMinutes)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue) : ((BarsType)this).SessionIterator.ActualSessionBegin.AddMinutes(Math.Ceiling(Math.Ceiling(Math.Max(0.0, time.Subtract(((BarsType)this).SessionIterator.ActualSessionBegin).TotalMinutes)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue));
		if (bars.TradingHours.Sessions.Count > 0 && dateTime > ((BarsType)this).SessionIterator.ActualSessionEnd)
		{
			dateTime = ((((BarsType)this).SessionIterator.ActualSessionEnd <= Globals.MinDate) ? dateTime : ((BarsType)this).SessionIterator.ActualSessionEnd);
		}
		return dateTime;
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
		DateTime dateTime = ((BarsType)this).SessionIterator.ActualSessionBegin.AddSeconds(Math.Ceiling(Math.Ceiling(Math.Max(0.0, time.Subtract(((BarsType)this).SessionIterator.ActualSessionBegin).TotalSeconds)) / (double)bars.BarsPeriod.BaseBarsPeriodValue) * (double)bars.BarsPeriod.BaseBarsPeriodValue);
		if (bars.TradingHours.Sessions.Count > 0 && dateTime > ((BarsType)this).SessionIterator.ActualSessionEnd)
		{
			dateTime = ((((BarsType)this).SessionIterator.ActualSessionEnd <= Globals.MinDate) ? dateTime : ((BarsType)this).SessionIterator.ActualSessionEnd);
		}
		return dateTime;
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
