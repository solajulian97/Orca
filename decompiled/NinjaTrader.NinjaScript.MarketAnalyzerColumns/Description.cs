using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class Description : MarketAnalyzerColumnBase
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionDescription;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameDescription;
			((NinjaScriptBase)this).IsDataSeriesRequired = false;
			((MarketAnalyzerColumnBase)this).DataType = typeof(string);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((MarketAnalyzerColumnBase)this).CurrentText = ((NinjaScriptBase)this).Instrument.MasterInstrument.Description;
		}
	}
}
