using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class High52Weeks : MarketAnalyzerColumn
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionHigh52Weeks;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameHigh52Weeks;
			base.IsDataSeriesRequired = false;
		}
		else if ((int)((NinjaScript)this).State == 7 && ((NinjaScriptBase)this).Instrument != null && ((NinjaScriptBase)this).Instrument.FundamentalData != null && ((NinjaScriptBase)this).Instrument.FundamentalData.High52Weeks.HasValue)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = ((NinjaScriptBase)this).Instrument.FundamentalData.High52Weeks.Value;
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
		else if ((int)fundamentalDataUpdate.FundamentalDataType == 12)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = fundamentalDataUpdate.DoubleValue;
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
