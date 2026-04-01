using System.ComponentModel;

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum PivotRange
{
	Daily,
	Weekly,
	Monthly
}
