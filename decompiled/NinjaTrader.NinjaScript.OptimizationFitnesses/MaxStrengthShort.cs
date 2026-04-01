using NinjaTrader.Cbi;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.OptimizationFitnesses;

public class MaxStrengthShort : OptimizationFitness
{
	protected override void OnCalculatePerformanceValue(StrategyBase strategy)
	{
		TradeCollection shortTrades = strategy.SystemPerformance.ShortTrades;
		((OptimizationFitness)this).Value = (double)(100 * ((!(shortTrades.TradesPerformance.ProfitFactor < 1.0)) ? 1 : 0)) * ((shortTrades.TradesCount == 0) ? 0.0 : ((double)shortTrades.WinningTrades.TradesCount / (double)shortTrades.TradesCount)) * shortTrades.TradesPerformance.RSquared * (1.0 - 0.25 / (0.25 + (shortTrades.TradesPerformance.ProfitFactor - 1.0))) * (1.0 - 25.0 / (25.0 + (double)shortTrades.TradesCount));
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptOptimizationFitnessNameMaxStrengthShort;
		}
	}
}
