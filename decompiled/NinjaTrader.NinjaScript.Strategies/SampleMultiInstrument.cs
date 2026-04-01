using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies;

public class SampleMultiInstrument : Strategy
{
	private RSI rsi;

	private ADX adx;

	private ADX adx1;

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptStrategyDescriptionSampleMultiInstrument;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptStrategyNameSampleMultiInstrument;
			((StrategyBase)this).IsInstantiatedOnEachOptimizationIteration = false;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries("MSFT", (BarsPeriodType)4, 1);
			((StrategyBase)this).SetTrailStop((CalculationMode)3, 20.0);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			rsi = RSI(14, 1);
			adx = ADX(14);
			adx1 = ADX((ISeries<double>)(object)((NinjaScriptBase)this).BarsArray[1], 14);
			((StrategyBase)this).AddChartIndicator((IndicatorBase)(object)rsi);
			((StrategyBase)this).AddChartIndicator((IndicatorBase)(object)adx);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar >= ((StrategyBase)this).BarsRequiredToTrade && ((NinjaScriptBase)this).CurrentBars[0] >= 0 && ((NinjaScriptBase)this).CurrentBars[1] >= 0 && ((NinjaScriptBase)this).BarsInProgress == 0)
		{
			if (((NinjaScriptBase)adx)[0] > 30.0 && ((NinjaScriptBase)adx1)[0] > 30.0 && ((NinjaScriptBase)this).CrossAbove((ISeries<double>)(object)rsi, 30.0, 1))
			{
				Draw.Square((NinjaScriptBase)(object)this, "My Square" + ((NinjaScriptBase)this).CurrentBar, isAutoScale: false, 0, ((NinjaScriptBase)this).High[0] + ((NinjaScriptBase)this).TickSize, Brushes.DodgerBlue);
				((StrategyBase)this).EnterLongLimit(((NinjaScriptBase)this).GetCurrentAsk(), "RSI");
			}
			if (((NinjaScriptBase)this).CrossBelow((ISeries<double>)(object)rsi, 75.0, 1))
			{
				((StrategyBase)this).ExitLong();
			}
		}
	}
}
