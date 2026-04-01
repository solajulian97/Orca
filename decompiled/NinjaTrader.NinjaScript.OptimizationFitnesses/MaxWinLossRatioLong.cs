using System;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.OptimizationFitnesses;

public class MaxWinLossRatioLong : OptimizationFitness
{
	protected override void OnCalculatePerformanceValue(StrategyBase strategy)
	{
		((OptimizationFitness)this).Value = ((strategy.SystemPerformance.LongTrades.LosingTrades.TradesPerformance.Percent.AverageProfit == 0.0) ? 1.0 : (strategy.SystemPerformance.LongTrades.WinningTrades.TradesPerformance.Percent.AverageProfit / Math.Abs(strategy.SystemPerformance.LongTrades.LosingTrades.TradesPerformance.Percent.AverageProfit)));
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptOptimizationFitnessNameMaxWinLossRatioLong;
		}
	}
}
