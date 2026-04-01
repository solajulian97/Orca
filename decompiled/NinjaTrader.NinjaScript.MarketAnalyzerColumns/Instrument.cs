using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class Instrument : MarketAnalyzerColumnBase
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionInstrument;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameInstrument;
			((NinjaScriptBase)this).IsDataSeriesRequired = false;
			((MarketAnalyzerColumnBase)this).DataType = typeof(string);
			((MarketAnalyzerColumnBase)this).IsEditable = true;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((MarketAnalyzerColumnBase)this).CurrentText = ((NinjaScriptBase)this).Instrument.FullName;
		}
	}
}
