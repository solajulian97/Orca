using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class DailyHigh : MarketAnalyzerColumn
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionDailyHigh;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameDailyHigh;
			base.IsDataSeriesRequired = false;
		}
		else if ((int)((NinjaScript)this).State == 7 && ((NinjaScriptBase)this).Instrument != null && ((NinjaScriptBase)this).Instrument.MarketData != null && ((NinjaScriptBase)this).Instrument.MarketData.DailyHigh != null)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = ((NinjaScriptBase)this).Instrument.MarketData.DailyHigh.Price;
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
		else if ((int)marketDataUpdate.MarketDataType == 3)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = marketDataUpdate.Price;
		}
	}

	public override string Format(double value)
	{
		if (value != double.MinValue)
		{
			return ((NinjaScriptBase)this).Instrument.MasterInstrument.FormatPrice(value, true);
		}
		return string.Empty;
	}
}
