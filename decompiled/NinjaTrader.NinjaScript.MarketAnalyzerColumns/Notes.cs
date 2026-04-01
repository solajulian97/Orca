using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class Notes : MarketAnalyzerColumnBase
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionNotes;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameNotes;
			((NinjaScriptBase)this).IsDataSeriesRequired = false;
			((MarketAnalyzerColumnBase)this).DataType = typeof(string);
			((MarketAnalyzerColumnBase)this).IsEditable = true;
		}
	}
}
