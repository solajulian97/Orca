using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class BidAskSpread : MarketAnalyzerColumn
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionBidAskSpread;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameBidAskSpread;
			base.IsDataSeriesRequired = false;
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		if (marketDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
		}
		else if (((int)marketDataUpdate.MarketDataType == 0 || (int)marketDataUpdate.MarketDataType == 1) && ((NinjaScriptBase)this).Instrument.MarketData.Ask != null && ((NinjaScriptBase)this).Instrument.MarketData.Bid != null)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = ((NinjaScriptBase)this).Instrument.MarketData.Ask.Price - ((NinjaScriptBase)this).Instrument.MarketData.Bid.Price;
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
