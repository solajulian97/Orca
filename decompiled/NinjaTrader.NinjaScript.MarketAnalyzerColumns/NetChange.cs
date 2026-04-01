using System;
using System.Globalization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class NetChange : MarketAnalyzerColumn
{
	private Account account;

	public PerformanceUnit Unit { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionNetChange;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameNetChange;
			base.IsDataSeriesRequired = false;
			Unit = (PerformanceUnit)1;
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
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected I4, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Invalid comparison between Unknown and I4
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Invalid comparison between Unknown and I4
		if (marketDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
		}
		else if ((int)marketDataUpdate.MarketDataType == 2 && marketDataUpdate.Instrument.MarketData.LastClose != null)
		{
			double num = 0.0;
			if (account != null)
			{
				bool flag = default(bool);
				num = marketDataUpdate.Instrument.GetConversionRate((MarketDataType)1, account.Denomination, ref flag);
			}
			PerformanceUnit unit = Unit;
			switch ((int)unit)
			{
			case 1:
				((MarketAnalyzerColumnBase)this).CurrentValue = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / marketDataUpdate.Instrument.MarketData.LastClose.Price;
				break;
			case 2:
				((MarketAnalyzerColumnBase)this).CurrentValue = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize * (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 4) ? 0.1 : 1.0);
				break;
			case 4:
				((MarketAnalyzerColumnBase)this).CurrentValue = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
				break;
			case 0:
				((MarketAnalyzerColumnBase)this).CurrentValue = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) * ((NinjaScriptBase)this).Instrument.MasterInstrument.PointValue * num * (double)(((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType != 4) ? 1 : ((account != null) ? account.ForexLotSize : 1000));
				break;
			case 3:
				((MarketAnalyzerColumnBase)this).CurrentValue = marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price;
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
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if (value <= double.MinValue)
		{
			return string.Empty;
		}
		PerformanceUnit unit = Unit;
		switch ((int)unit)
		{
		case 0:
		{
			Account obj = account;
			Currency val = ((obj != null) ? obj.Denomination : ((NinjaScriptBase)this).Instrument.MasterInstrument.Currency);
			return Globals.FormatCurrency(value, val);
		}
		case 3:
			if (!(value <= double.MinValue))
			{
				return ((NinjaScriptBase)this).Instrument.MasterInstrument.FormatPrice(value, true);
			}
			return string.Empty;
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
