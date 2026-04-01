using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class RevenuePerShare : MarketAnalyzerColumn
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionRevenuePerShare;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameRevenuePerShare;
			base.IsDataSeriesRequired = false;
		}
		else if ((int)((NinjaScript)this).State == 7 && ((NinjaScriptBase)this).Instrument != null && ((NinjaScriptBase)this).Instrument.FundamentalData != null && ((NinjaScriptBase)this).Instrument.FundamentalData.RevenuePerShare.HasValue)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = ((NinjaScriptBase)this).Instrument.FundamentalData.RevenuePerShare.Value;
		}
	}

	protected override void OnFundamentalData(FundamentalDataEventArgs fundamentalDataUpdate)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		if (fundamentalDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
		}
		else if ((int)fundamentalDataUpdate.FundamentalDataType == 21)
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
