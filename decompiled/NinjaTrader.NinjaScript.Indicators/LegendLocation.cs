using System.ComponentModel;

namespace NinjaTrader.NinjaScript.Indicators;

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum LegendLocation
{
	Disabled,
	TopLeft,
	TopRight,
	BottomLeft,
	BottomRight
}
