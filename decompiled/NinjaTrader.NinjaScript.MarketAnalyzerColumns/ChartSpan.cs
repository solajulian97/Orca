using System.ComponentModel;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum ChartSpan
{
	Min1,
	Min5,
	Min15,
	Min30,
	Min60,
	Min240,
	Day,
	Week,
	Month,
	Year
}
