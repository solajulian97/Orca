using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The VMA (Variable Moving Average, also known as VIDYA or Variable Index Dynamic Average)
///  is an exponential moving average that automatically adjusts the smoothing weight based
/// on the volatility of the data series. VMA solves a problem with most moving averages.
/// In times of low volatility, such as when the price is trending, the moving average time
///  period should be shorter to be sensitive to the inevitable break in the trend. Whereas,
/// in more volatile non-trending times, the moving average time period should be longer to
/// filter out the choppiness. VIDYA uses the CMO indicator for it's internal volatility calculations.
/// Both the VMA and the CMO period are adjustable.
/// </summary>
public class VMA : Indicator
{
	private CMO cmo;

	private double sc;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "VolatilityPeriod", GroupName = "NinjaScriptParameters", Order = 1)]
	public int VolatilityPeriod { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionVMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameVMA;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			Period = 9;
			VolatilityPeriod = 9;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameVMA);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			sc = 2.0 / (double)(Period + 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			cmo = CMO(((NinjaScriptBase)this).Inputs[0], VolatilityPeriod);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)this).Input[0];
			return;
		}
		double num = Math.Abs(((NinjaScriptBase)cmo)[0]) / 100.0;
		((NinjaScriptBase)this).Value[0] = sc * num * ((NinjaScriptBase)this).Input[0] + (1.0 - sc * num) * ((NinjaScriptBase)this).Value[1];
	}
}
