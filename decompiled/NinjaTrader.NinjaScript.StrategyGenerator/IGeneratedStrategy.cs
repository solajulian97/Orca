using NinjaTrader.Cbi;

namespace NinjaTrader.NinjaScript.StrategyGenerator;

public interface IGeneratedStrategy
{
	Order OnEnterLong();

	Order OnEnterShort();

	Order OnExitLong();

	Order OnExitShort();
}
