namespace NinjaTrader.NinjaScript.Indicators;

public class VwapTracker
{
	private double tickSize;

	public double ReversalTicks;

	public int Direction;

	public double ExtremePrice;

	public int ExtremeBar = -1;

	public double AnchorPrice;

	public int ActiveAnchorBar = -1;

	public bool IsActive;

	public bool JustReversed;

	public double PriorCumVol;

	public double PriorCumPV;

	public double PriorCumP2V;

	public int LastBarSeen = -1;

	public VwapTracker(double tickSize, double reversalTicks)
	{
		this.tickSize = tickSize;
		ReversalTicks = reversalTicks;
	}

	public void Process(double high, double low, double close, double volume, int currentBar)
	{
		JustReversed = false;
		if (Direction == 0)
		{
			Direction = 1;
			ExtremePrice = high;
			ExtremeBar = currentBar;
			return;
		}
		if ((Direction == 1 && high >= ExtremePrice) || (Direction == -1 && low <= ExtremePrice))
		{
			ExtremePrice = ((Direction == 1) ? high : low);
			ExtremeBar = currentBar;
		}
		if ((Direction == 1 && (ExtremePrice - low) / tickSize >= ReversalTicks) || (Direction == -1 && (high - ExtremePrice) / tickSize >= ReversalTicks))
		{
			AnchorPrice = ExtremePrice;
			ActiveAnchorBar = ExtremeBar;
			Direction = ((Direction != 1) ? 1 : (-1));
			ExtremePrice = ((Direction == 1) ? high : low);
			ExtremeBar = currentBar;
			IsActive = true;
			JustReversed = true;
		}
	}
}
