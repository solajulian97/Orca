using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.OptimizationFitnesses;

public class MaxSortinoRatioShort : OptimizationFitness
{
	protected override void OnCalculatePerformanceValue(StrategyBase strategy)
	{
		((OptimizationFitness)this).Value = strategy.SystemPerformance.ShortTrades.TradesPerformance.SortinoRatio;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptOptimizationFitnessNameMaxSortinoRatioShort;
		}
	}
}
