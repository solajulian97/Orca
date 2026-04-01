using System.ComponentModel;
using NinjaTrader.Core;

namespace NinjaTrader.NinjaScript.DrawingTools;

[TypeConverter(typeof(CoreEnumConverter))]
public enum ChartMarkerSize
{
	ExtraSmall,
	Small,
	Medium,
	Large,
	ExtraLarge
}
