using System;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class TickBarsType : BarsType
{
	public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
	{
	}

	public override void ApplyDefaultValue(BarsPeriod period)
	{
		period.Value = 150;
	}

	public override string ChartLabel(DateTime time)
	{
		return time.ToString("HH:mm:ss");
	}

	public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
	{
		return 1;
	}

	public override double GetPercentComplete(Bars bars, DateTime now)
	{
		return (double)bars.TickCount / (double)bars.BarsPeriod.Value;
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
		if (bars.BarsPeriod.Value == 1)
		{
			((BarsType)this).AddBar(bars, open, high, low, close, time, volume, bid, ask);
		}
		else if (bars.Count == 0)
		{
			((BarsType)this).AddBar(bars, open, high, low, close, time, volume);
		}
		else if (bars.Count > 0 && (!bars.IsResetOnNewTradingDay || !flag) && bars.BarsPeriod.Value > 1 && bars.TickCount < bars.BarsPeriod.Value)
		{
			((BarsType)this).UpdateBar(bars, high, low, close, time, volume);
		}
		else
		{
			((BarsType)this).AddBar(bars, open, high, low, close, time, volume);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Invalid comparison between Unknown and I4
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeTick;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)0
			};
			((BarsType)this).BuiltFrom = (BarsPeriodType)0;
			((BarsType)this).DaysToLoad = 3;
			((BarsType)this).IsIntraday = true;
			((BarsType)this).IsTimeBased = false;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScript)this).Name = string.Format(Globals.GeneralOptions.CurrentCulture, Resource.DataBarsTypeTick, ((BarsType)this).BarsPeriod.Value, ((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? $" - {Globals.ToLocalizedObject((object)((BarsType)this).BarsPeriod.MarketDataType, Globals.GeneralOptions.CurrentUICulture)}" : string.Empty);
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodValue", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
		}
	}
}
