using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies;

public class SampleMultiTimeFrame : Strategy
{
	private SMA sma50B0;

	private SMA sma50B1;

	private SMA sma50B2;

	private SMA sma5B0;

	private SMA sma5B1;

	private SMA sma5B2;

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptStrategyDescriptionSampleMultiTimeFrame;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptStrategyNameSampleMultiTimeFrame;
			((StrategyBase)this).IsInstantiatedOnEachOptimizationIteration = false;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)4, 5);
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)4, 15);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			sma50B0 = SMA(50);
			sma5B0 = SMA(5);
			sma50B1 = SMA((ISeries<double>)(object)((NinjaScriptBase)this).BarsArray[1], 50);
			sma50B2 = SMA((ISeries<double>)(object)((NinjaScriptBase)this).BarsArray[2], 50);
			sma5B1 = SMA((ISeries<double>)(object)((NinjaScriptBase)this).BarsArray[1], 5);
			sma5B2 = SMA((ISeries<double>)(object)((NinjaScriptBase)this).BarsArray[2], 5);
			((StrategyBase)this).AddChartIndicator((IndicatorBase)(object)sma5B0);
			((StrategyBase)this).AddChartIndicator((IndicatorBase)(object)sma50B0);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar >= ((StrategyBase)this).BarsRequiredToTrade && ((NinjaScriptBase)this).CurrentBars[0] >= 1 && ((NinjaScriptBase)this).CurrentBars[1] >= 1 && ((NinjaScriptBase)this).CurrentBars[2] >= 1 && ((NinjaScriptBase)this).BarsInProgress == 0)
		{
			if (((NinjaScriptBase)sma5B1)[0] > ((NinjaScriptBase)sma50B1)[0] && ((NinjaScriptBase)sma5B2)[0] > ((NinjaScriptBase)sma50B2)[0] && ((NinjaScriptBase)this).CrossAbove((ISeries<double>)(object)sma5B0, (ISeries<double>)(object)sma50B0, 1))
			{
				((StrategyBase)this).EnterLong(1000, "SMA");
			}
			if (((NinjaScriptBase)this).CrossBelow((ISeries<double>)(object)sma5B2, (ISeries<double>)(object)sma50B2, 1))
			{
				((StrategyBase)this).ExitLong(1000);
			}
		}
	}
}
