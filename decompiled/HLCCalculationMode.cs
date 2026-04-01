using System.ComponentModel;

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum HLCCalculationMode
{
	CalcFromIntradayData,
	DailyBars,
	UserDefinedValues
}
