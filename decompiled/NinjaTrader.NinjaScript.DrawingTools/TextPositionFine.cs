using System.ComponentModel;

namespace NinjaTrader.NinjaScript.DrawingTools;

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum TextPositionFine
{
	BottomLeft,
	BottomMiddle,
	BottomRight,
	MiddleLeft,
	MiddleRight,
	TopLeft,
	TopMiddle,
	TopRight
}
