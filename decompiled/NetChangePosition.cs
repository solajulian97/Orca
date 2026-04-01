using System.ComponentModel;

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum NetChangePosition
{
	BottomLeft,
	BottomRight,
	TopLeft,
	TopRight
}
