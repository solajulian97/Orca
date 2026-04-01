using System.ComponentModel;

namespace NinjaTrader.NinjaScript.DrawingTools;

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum TextPosition
{
	BottomLeft,
	BottomRight,
	Center,
	TopLeft,
	TopRight
}
