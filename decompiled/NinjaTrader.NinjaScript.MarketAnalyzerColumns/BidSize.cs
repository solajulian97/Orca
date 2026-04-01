using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class BidSize : MarketAnalyzerColumn
{
	private InstrumentType instrumentType = (InstrumentType)99;

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionBidSize;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameBidSize;
			base.IsDataSeriesRequired = false;
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		if (marketDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
		}
		else if ((int)marketDataUpdate.MarketDataType == 1)
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
