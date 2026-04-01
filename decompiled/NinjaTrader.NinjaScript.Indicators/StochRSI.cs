using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The StochRSI is an oscillator similar in computation to the stochastic measure,
/// except instead of price values as input, the StochRSI uses RSI values.
/// The StochRSI computes the current position of the RSI relative to the high and
/// low RSI values over a specified number of days. The intent of this measure,
/// designed by Tushard Chande and Stanley Kroll, is to provide further information
/// about the overbought/oversold nature of the RSI. The StochRSI ranges between 0.0 and 1.0.
/// Values above 0.8 are generally seen to identify overbought levels and values below 0.2 are
/// considered to indicate oversold conditions.
/// </summary>
public class StochRSI : Indicator
{
	private MAX max;

	private MIN min;

	private RSI rsi;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionStochRSI;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameStochRSI;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = false;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameStochRSI);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.Crimson, 0.8, Resource.NinjaScriptIndicatorOverbought);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DodgerBlue, 0.5, Resource.NinjaScriptIndicatorNeutral);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.Crimson, 0.2, Resource.NinjaScriptIndicatorOversold);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			rsi = RSI(((NinjaScriptBase)this).Inputs[0], Period, 1);
			min = MIN((ISeries<double>)(object)rsi, Period);
			max = MAX((ISeries<double>)(object)rsi, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)rsi)[0];
		double num2 = ((NinjaScriptBase)min)[0];
		double num3 = ((NinjaScriptBase)max)[0];
		((NinjaScriptBase)this).Value[0] = ((Math.Abs(num - num2) > double.Epsilon && Math.Abs(num3 - num2) > double.Epsilon) ? ((num - num2) / (num3 - num2)) : 0.0);
	}
}
