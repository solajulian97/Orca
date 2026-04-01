using System;
using System.Globalization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class NetChangeMaxUp : MarketAnalyzerColumn
{
	private Account account;

	private Instrument instrument;

	private bool isInitialCalculation = true;

	public PerformanceUnit Unit { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionNetChangeMaxUp;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameNetChangeMaxUp;
			base.IsDataSeriesRequired = false;
			Unit = (PerformanceUnit)1;
		}
		if ((int)((NinjaScript)this).State == 2)
		{
			instrument = ((NinjaScriptBase)this).Instruments[0];
		}
	}

	protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		if ((int)connectionStatusUpdate.PriceStatus == 3 && (int)connectionStatusUpdate.PreviousStatus == 4 && connectionStatusUpdate.Connection.Accounts.Count > 0 && account == null)
		{
			account = connectionStatusUpdate.Connection.Accounts[0];
		}
		else if ((int)connectionStatusUpdate.Status == 0 && (int)connectionStatusUpdate.PreviousStatus == 1 && account != null && account.Connection == connectionStatusUpdate.Connection)
		{
			account = null;
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Invalid comparison between Unknown and I4
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Expected I4, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Invalid comparison between Unknown and I4
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Invalid comparison between Unknown and I4
		if (marketDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
			return;
		}
		double num = double.MinValue;
		double num2 = double.MinValue;
		if ((int)marketDataUpdate.MarketDataType == 3 && marketDataUpdate.Instrument.MarketData.LastClose != null)
		{
			num = marketDataUpdate.Price;
			num2 = marketDataUpdate.Instrument.MarketData.LastClose.Price;
		}
		else if ((int)marketDataUpdate.MarketDataType == 6 && marketDataUpdate.Instrument.MarketData.DailyHigh != null)
		{
			num = marketDataUpdate.Instrument.MarketData.DailyHigh.Price;
			num2 = marketDataUpdate.Price;
		}
		else if (isInitialCalculation)
		{
			if (marketDataUpdate.Instrument.MarketData.DailyHigh != null && marketDataUpdate.Instrument.MarketData.LastClose != null)
			{
				num = marketDataUpdate.Instrument.MarketData.DailyHigh.Price;
				num2 = marketDataUpdate.Instrument.MarketData.LastClose.Price;
			}
			isInitialCalculation = false;
		}
		if (num != double.MinValue && num2 != double.MinValue)
		{
			double num3 = 0.0;
			if (account != null)
			{
				bool flag = default(bool);
				num3 = marketDataUpdate.Instrument.GetConversionRate((MarketDataType)1, account.Denomination, ref flag);
			}
			PerformanceUnit unit = Unit;
			switch ((int)unit)
			{
			case 1:
				((MarketAnalyzerColumnBase)this).CurrentValue = (num - num2) / num2;
				break;
			case 2:
				((MarketAnalyzerColumnBase)this).CurrentValue = (num - num2) / ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize * (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 4) ? 0.1 : 1.0);
				break;
			case 4:
				((MarketAnalyzerColumnBase)this).CurrentValue = (num - num2) / ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
				break;
			case 0:
				((MarketAnalyzerColumnBase)this).CurrentValue = (num - num2) * ((NinjaScriptBase)this).Instrument.MasterInstrument.PointValue * num3 * (double)(((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType != 4) ? 1 : ((account != null) ? account.ForexLotSize : 1000));
				break;
			case 3:
				((MarketAnalyzerColumnBase)this).CurrentValue = num - num2;
				break;
			}
		}
	}

	public override string Format(double value)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected I4, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		if (value == double.MinValue)
		{
			return string.Empty;
		}
		PerformanceUnit unit = Unit;
		switch ((int)unit)
		{
		case 0:
		{
			Currency val = ((account == null) ? ((NinjaScriptBase)this).Instrument.MasterInstrument.Currency : account.Denomination);
			return Globals.FormatCurrency(value, val);
		}
		case 3:
			return value.ToString(Globals.GetTickFormatString(((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize), Globals.GeneralOptions.CurrentCulture);
		case 1:
			return value.ToString("P", Globals.GeneralOptions.CurrentCulture);
		case 2:
		{
			CultureInfo cultureInfo = Globals.GeneralOptions.CurrentCulture.Clone() as CultureInfo;
			if (cultureInfo != null)
			{
				cultureInfo.NumberFormat.NumberDecimalSeparator = "'";
			}
			return (Math.Round(value * 10.0) / 10.0).ToString("0.0", cultureInfo);
		}
		case 4:
			return Math.Round(value).ToString(Globals.GeneralOptions.CurrentCulture);
		default:
			return "0";
		}
	}
}
