using System;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.BarsTypes;

public class VolumeBarsType : BarsType
{
	public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
	{
	}

	public override void ApplyDefaultValue(BarsPeriod period)
	{
		period.Value = 1000;
	}

	public override string ChartLabel(DateTime time)
	{
		return time.ToString("T", Globals.GeneralOptions.CurrentCulture);
	}

	public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
	{
		return 1;
	}

	public override double GetPercentComplete(Bars bars, DateTime now)
	{
		if (bars.Count != 0)
		{
			return (double)bars.GetVolume(bars.Count - 1) / (double)bars.BarsPeriod.Value;
		}
		return 0.0;
	}

	protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0016: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Invalid comparison between Unknown and I4
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
		long num = bars.BarsPeriod.Value;
		if ((int)bars.Instrument.MasterInstrument.InstrumentType == 7)
		{
			num = Globals.FromCryptocurrencyVolume((double)bars.BarsPeriod.Value);
		}
		if (bars.Count == 0)
		{
			while (volume > num)
			{
				((BarsType)this).AddBar(bars, open, high, low, close, time, num);
				volume -= num;
			}
			if (volume > 0)
			{
				((BarsType)this).AddBar(bars, open, high, low, close, time, volume);
			}
			return;
		}
		long num2 = 0L;
		if (!bars.IsResetOnNewTradingDay || !flag)
		{
			num2 = Math.Min(num - bars.GetVolume(bars.Count - 1), volume);
			if (num2 > 0)
			{
				((BarsType)this).UpdateBar(bars, high, low, close, time, num2);
			}
		}
		for (num2 = volume - num2; num2 > 0; num2 -= num)
		{
			((BarsType)this).AddBar(bars, open, high, low, close, time, Math.Min(num2, num));
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Invalid comparison between Unknown and I4
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptBarsTypeVolume;
			((BarsType)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)1,
				Value = 1000
			};
			((BarsType)this).BuiltFrom = (BarsPeriodType)0;
			((BarsType)this).DaysToLoad = 3;
			((BarsType)this).IsIntraday = true;
			((BarsType)this).IsTimeBased = false;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScript)this).Name = string.Format(Globals.GeneralOptions.CurrentCulture, Resource.DataBarsTypeVolume, ((BarsType)this).BarsPeriod.Value, ((int)((BarsType)this).BarsPeriod.MarketDataType != 2) ? (" - " + Globals.ToLocalizedObject((object)((BarsType)this).BarsPeriod.MarketDataType, Globals.GeneralOptions.CurrentUICulture)) : string.Empty);
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("BaseBarsPeriodValue", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("PointAndFigurePriceType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("ReversalType", ignoreCase: true));
			((BarsType)this).Properties.Remove(((BarsType)this).Properties.Find("Value2", ignoreCase: true));
		}
	}
}
