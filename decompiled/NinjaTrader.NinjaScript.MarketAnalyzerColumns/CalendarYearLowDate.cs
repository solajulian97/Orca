using System;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class CalendarYearLowDate : MarketAnalyzerColumn
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionCalendarYearLowDate;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameCalendarYearLowDate;
			base.IsDataSeriesRequired = false;
			((MarketAnalyzerColumnBase)this).DataType = typeof(string);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((MarketAnalyzerColumnBase)this).CurrentText = string.Empty;
		}
		else if ((int)((NinjaScript)this).State == 7 && ((NinjaScriptBase)this).Instrument != null && ((NinjaScriptBase)this).Instrument.FundamentalData != null && ((NinjaScriptBase)this).Instrument.FundamentalData.CalendarYearLowDate.HasValue)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = ((NinjaScriptBase)this).Instrument.FundamentalData.CalendarYearLowDate.Value.Subtract(Globals.MinDate).TotalDays;
			((MarketAnalyzerColumnBase)this).CurrentText = Format(((MarketAnalyzerColumnBase)this).CurrentValue);
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
		else if ((int)fundamentalDataUpdate.FundamentalDataType == 5)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = fundamentalDataUpdate.DateTimeValue.Subtract(Globals.MinDate).TotalDays;
			((MarketAnalyzerColumnBase)this).CurrentText = Format(((MarketAnalyzerColumnBase)this).CurrentValue);
		}
	}

	public string Format(double value)
	{
		DateTime minDate = Globals.MinDate;
		return minDate.AddDays(value).ToString(Globals.GeneralOptions.CurrentCulture.DateTimeFormat.ShortDatePattern, Globals.GeneralOptions.CurrentCulture);
	}
}
