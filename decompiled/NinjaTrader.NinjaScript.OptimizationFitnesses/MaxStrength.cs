using NinjaTrader.Cbi;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.OptimizationFitnesses;

public class MaxStrength : OptimizationFitness
{
	protected override void OnCalculatePerformanceValue(StrategyBase strategy)
	{
		TradeCollection allTrades = strategy.SystemPerformance.AllTrades;
		((OptimizationFitness)this).Value = (double)(100 * ((!(allTrades.TradesPerformance.ProfitFactor < 1.0)) ? 1 : 0)) * ((allTrades.TradesCount == 0) ? 0.0 : ((double)allTrades.WinningTrades.TradesCount / (double)allTrades.TradesCount)) * allTrades.TradesPerformance.RSquared * (1.0 - 0.25 / (0.25 + (allTrades.TradesPerformance.ProfitFactor - 1.0))) * (1.0 - 25.0 / (25.0 + (double)allTrades.TradesCount));
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptOptimizationFitnessNameMaxStrength;
		}
	}
}
