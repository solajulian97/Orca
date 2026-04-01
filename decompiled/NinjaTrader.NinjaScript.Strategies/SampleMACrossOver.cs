using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies;

public class SampleMACrossOver : Strategy
{
	private SMA smaFast;

	private SMA smaSlow;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Fast", GroupName = "NinjaScriptStrategyParameters", Order = 0)]
	public int Fast { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Slow", GroupName = "NinjaScriptStrategyParameters", Order = 1)]
	public int Slow { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptStrategyDescriptionSampleMACrossOver;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptStrategyNameSampleMACrossOver;
			Fast = 10;
			Slow = 25;
			((StrategyBase)this).IsInstantiatedOnEachOptimizationIteration = false;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			smaFast = SMA(Fast);
			smaSlow = SMA(Slow);
			((Stroke)((NinjaScriptBase)smaFast).Plots[0]).Brush = Brushes.Goldenrod;
			((Stroke)((NinjaScriptBase)smaSlow).Plots[0]).Brush = Brushes.SeaGreen;
			((StrategyBase)this).AddChartIndicator((IndicatorBase)(object)smaFast);
			((StrategyBase)this).AddChartIndicator((IndicatorBase)(object)smaSlow);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar >= ((StrategyBase)this).BarsRequiredToTrade)
		{
			if (((NinjaScriptBase)this).CrossAbove((ISeries<double>)(object)smaFast, (ISeries<double>)(object)smaSlow, 1))
			{
				((StrategyBase)this).EnterLong();
			}
			else if (((NinjaScriptBase)this).CrossBelow((ISeries<double>)(object)smaFast, (ISeries<double>)(object)smaSlow, 1))
			{
				((StrategyBase)this).EnterShort();
			}
		}
	}
}
