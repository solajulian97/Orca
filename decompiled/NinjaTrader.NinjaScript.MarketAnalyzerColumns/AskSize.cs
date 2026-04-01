using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class AskSize : MarketAnalyzerColumn
{
	private InstrumentType instrumentType = (InstrumentType)99;

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionAskSize;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameAskSize;
			base.IsDataSeriesRequired = false;
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		if (marketDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
		}
		else if ((int)marketDataUpdate.MarketDataType == 0)
		{
			instrumentType = marketDataUpdate.Instrument.MasterInstrument.InstrumentType;
			((MarketAnalyzerColumnBase)this).CurrentValue = (((int)instrumentType == 7) ? Globals.ToCryptocurrencyVolume(marketDataUpdate.Volume) : ((double)marketDataUpdate.Volume));
		}
	}

	public override string Format(double value)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		if (value != double.MinValue)
		{
			if ((int)instrumentType != 7)
			{
				return Globals.FormatQuantity((long)value, false);
			}
			return Globals.FormatCryptocurrencyQuantity(value, false);
		}
		return string.Empty;
	}
}
