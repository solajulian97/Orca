#region Using declarations
using System;
using System.Collections.Generic;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum AggressorSide
	{
		Unknown = 0,
		Buy = 1,
		Sell = -1
	}

	public enum ParentConfidenceMode
	{
		Off,
		Score,
		ScoreAndFilter
	}

	public enum DotSizeScale
	{
		Linear,
		Logarithmic
	}

	public enum ShapeMode
	{
		UniformCircles,
		DistinguishClusters
	}

	internal enum OrcaPrintEventKind
	{
		Single,
		Cluster
	}

	internal struct OrcaPrintTick
	{
		public DateTime Time;
		public double Price;
		public long Size;
		public AggressorSide Side;

		public OrcaPrintTick(DateTime time, double price, long size, AggressorSide side)
		{
			Time = time;
			Price = price;
			Size = size;
			Side = side;
		}
	}

	internal class PrintEvent
	{
		public DateTime Time;
		public double Price;
		public long Volume;
		public AggressorSide Side;
		public OrcaPrintEventKind Kind;

		public bool IsCluster
		{
			get { return Kind == OrcaPrintEventKind.Cluster; }
		}
	}

	internal class ClusterEvent : PrintEvent
	{
		public DateTime StartTime;
		public DateTime EndTime;
		public double VwapPrice;
		public double MinPrice;
		public double MaxPrice;
		public long TotalVolume;
		public long BuyVolume;
		public long SellVolume;
		public int ChildCount;
		public AggressorSide DominantSide;
		public List<long> ChildSizes;
		public List<long> InterTradeGapsMs;
		public double ParentConfidenceScore;
		public double AggressorConsistencyScore;
		public double SizeUniformityScore;
		public double PriceTightnessScore;
		public double TimingRegularityScore;

		public ClusterEvent()
		{
			Kind = OrcaPrintEventKind.Cluster;
			ChildSizes = new List<long>();
			InterTradeGapsMs = new List<long>();
		}
	}
}
