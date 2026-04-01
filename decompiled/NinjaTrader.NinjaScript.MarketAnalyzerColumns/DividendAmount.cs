using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class DividendAmount : MarketAnalyzerColumn
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionDividendAmount;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameDividendAmount;
			base.IsDataSeriesRequired = false;
		}
		else if ((int)((NinjaScript)this).State == 7 && ((NinjaScriptBase)this).Instrument != null && ((NinjaScriptBase)this).Instrument.FundamentalData != null && ((NinjaScriptBase)this).Instrument.FundamentalData.DividendAmount.HasValue)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = ((NinjaScriptBase)this).Instrument.FundamentalData.DividendAmount.Value;
		}
	}

	protected override void OnFundamentalData(FundamentalDataEventArgs fundamentalDataUpdate)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (fundamentalDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
		}
		else if ((int)fundamentalDataUpdate.FundamentalDataType == 7)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = fundamentalDataUpdate.DoubleValue;
		}
	}

	public override string Format(double value)
	{
		if (value != double.MinValue)
		{
			return Globals.FormatCurrency(value);
		}
		return string.Empty;
	}
}
