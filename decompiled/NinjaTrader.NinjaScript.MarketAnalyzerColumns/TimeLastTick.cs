using System;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class TimeLastTick : MarketAnalyzerColumn
{
	private DateTime reference = new DateTime(2000, 1, 1);

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionTimeLastTick;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameTimeLastTick;
			base.IsDataSeriesRequired = false;
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (marketDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
		}
		else if ((int)marketDataUpdate.MarketDataType == 2)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = marketDataUpdate.Time.Subtract(reference).TotalSeconds;
		}
	}

	public override string Format(double value)
	{
		if (value == double.MinValue)
		{
			return string.Empty;
		}
		if (((MarketAnalyzerColumnBase)this).CurrentValue != double.MinValue)
		{
			return reference.AddSeconds(((MarketAnalyzerColumnBase)this).CurrentValue).ToString(Globals.GeneralOptions.CurrentCulture.DateTimeFormat.LongTimePattern, Globals.GeneralOptions.CurrentCulture);
		}
		return string.Empty;
	}
}
