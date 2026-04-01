using System.ComponentModel;

namespace NinjaTrader.NinjaScript.SuperDomColumns;

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum RecentDisplayType
{
	Ask,
	Bid,
	BidAsk
}
