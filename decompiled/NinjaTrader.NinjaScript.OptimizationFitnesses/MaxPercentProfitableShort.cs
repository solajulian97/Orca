using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.OptimizationFitnesses;

public class MaxPercentProfitableShort : OptimizationFitness
{
	protected override void OnCalculatePerformanceValue(StrategyBase strategy)
	{
		((OptimizationFitness)this).Value = ((strategy.SystemPerformance.ShortTrades.TradesCount == 0) ? 0.0 : ((double)strategy.SystemPerformance.ShortTrades.WinningTrades.TradesCount / (double)strategy.SystemPerformance.ShortTrades.TradesCount));
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptOptimizationFitnessNameMaxPercentProfitableShort;
		}
	}
}
