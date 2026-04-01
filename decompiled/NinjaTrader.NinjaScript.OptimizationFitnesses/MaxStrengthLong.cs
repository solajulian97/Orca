using NinjaTrader.Cbi;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.OptimizationFitnesses;

public class MaxStrengthLong : OptimizationFitness
{
	protected override void OnCalculatePerformanceValue(StrategyBase strategy)
	{
		TradeCollection longTrades = strategy.SystemPerformance.LongTrades;
		((OptimizationFitness)this).Value = (double)(100 * ((!(longTrades.TradesPerformance.ProfitFactor < 1.0)) ? 1 : 0)) * ((longTrades.TradesCount == 0) ? 0.0 : ((double)longTrades.WinningTrades.TradesCount / (double)longTrades.TradesCount)) * longTrades.TradesPerformance.RSquared * (1.0 - 0.25 / (0.25 + (longTrades.TradesPerformance.ProfitFactor - 1.0))) * (1.0 - 25.0 / (25.0 + (double)longTrades.TradesCount));
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptOptimizationFitnessNameMaxStrengthLong;
		}
	}
}
