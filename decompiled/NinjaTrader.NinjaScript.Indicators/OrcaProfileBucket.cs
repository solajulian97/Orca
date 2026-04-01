using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.Indicators;

public class OrcaProfileBucket
{
	public Dictionary<double, long> VolByPrice = new Dictionary<double, long>();

	public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();
}
