#region Using declarations
using System;
using System.Collections.Generic;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class OrcaPrints
	{
		private void ScoreCluster(ClusterEvent cluster)
		{
			if (cluster == null || cluster.TotalVolume <= 0)
				return;

			cluster.AggressorConsistencyScore = CalculateAggressorConsistency(cluster);
			cluster.SizeUniformityScore = CalculateSizeUniformity(cluster.ChildSizes);
			cluster.PriceTightnessScore = CalculatePriceTightness(cluster);
			cluster.TimingRegularityScore = CalculateTimingRegularity(cluster.InterTradeGapsMs);

			double wAggressor = Math.Max(0.0, WeightAggressorConsistency);
			double wSize = Math.Max(0.0, WeightSizeUniformity);
			double wPrice = Math.Max(0.0, WeightPriceTightness);
			double wTiming = Math.Max(0.0, WeightTimingRegularity);
			double weightSum = wAggressor + wSize + wPrice + wTiming;

			if (weightSum <= 0.0)
			{
				wAggressor = 0.30;
				wSize = 0.25;
				wPrice = 0.20;
				wTiming = 0.25;
				weightSum = 1.0;
			}

			double composite =
				wAggressor * cluster.AggressorConsistencyScore +
				wSize * cluster.SizeUniformityScore +
				wPrice * cluster.PriceTightnessScore +
				wTiming * cluster.TimingRegularityScore;

			cluster.ParentConfidenceScore = Clamp01(composite / weightSum);
		}

		private double CalculateAggressorConsistency(ClusterEvent cluster)
		{
			long maxSideVolume = Math.Max(cluster.BuyVolume, cluster.SellVolume);
			if (cluster.TotalVolume <= 0)
				return 0.0;

			double oneSideShare = (double)maxSideVolume / (double)cluster.TotalVolume;
			return Clamp01((oneSideShare - 0.5) * 2.0);
		}

		private double CalculateSizeUniformity(List<long> childSizes)
		{
			if (childSizes == null || childSizes.Count == 0)
				return 0.0;
			if (childSizes.Count == 1)
				return 1.0;

			double mean = 0.0;
			for (int i = 0; i < childSizes.Count; i++)
				mean += childSizes[i];
			mean /= childSizes.Count;

			if (mean <= 0.0)
				return 0.0;

			double variance = 0.0;
			Dictionary<long, int> countsBySize = new Dictionary<long, int>();
			int modeCount = 0;

			for (int i = 0; i < childSizes.Count; i++)
			{
				double diff = childSizes[i] - mean;
				variance += diff * diff;

				int count;
				if (!countsBySize.TryGetValue(childSizes[i], out count))
					count = 0;
				count++;
				countsBySize[childSizes[i]] = count;
				if (count > modeCount)
					modeCount = count;
			}

			variance /= childSizes.Count;
			double stdDev = Math.Sqrt(variance);
			double coeffVariation = stdDev / mean;
			double cvScore = 1.0 - Clamp01(coeffVariation);
			double modeScore = (double)modeCount / (double)childSizes.Count;

			return Clamp01(Math.Max(cvScore, modeScore));
		}

		private double CalculatePriceTightness(ClusterEvent cluster)
		{
			if (TickSize <= 0 || ClusterMaxPriceTicks <= 0)
				return 1.0;

			double priceRangeTicks = (cluster.MaxPrice - cluster.MinPrice) / TickSize;
			double score = 1.0 - (priceRangeTicks / ClusterMaxPriceTicks);
			return Clamp01(score);
		}

		private double CalculateTimingRegularity(List<long> interTradeGapsMs)
		{
			if (interTradeGapsMs == null || interTradeGapsMs.Count == 0)
				return 1.0;
			if (interTradeGapsMs.Count == 1)
				return 1.0;

			double mean = 0.0;
			for (int i = 0; i < interTradeGapsMs.Count; i++)
				mean += interTradeGapsMs[i];
			mean /= interTradeGapsMs.Count;

			if (mean <= 0.0)
				return 1.0;

			double variance = 0.0;
			for (int i = 0; i < interTradeGapsMs.Count; i++)
			{
				double diff = interTradeGapsMs[i] - mean;
				variance += diff * diff;
			}
			variance /= interTradeGapsMs.Count;

			double stdDev = Math.Sqrt(variance);
			double coeffVariation = stdDev / mean;
			return Clamp01(1.0 - Clamp01(coeffVariation));
		}

		private double Clamp01(double value)
		{
			if (value < 0.0) return 0.0;
			if (value > 1.0) return 1.0;
			return value;
		}
	}
}
