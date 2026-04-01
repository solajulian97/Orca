using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The ROC (Rate-of-Change) indicator displays the percent change between the current price and the price x-time periods ago.
/// </summary>
public class ROC : Indicator
{
	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionROC;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameROC;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameROC);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).Input[Math.Min(((NinjaScriptBase)this).CurrentBar, Period)];
		if (!(num <= 0.0))
		{
			((NinjaScriptBase)this).Value[0] = (((NinjaScriptBase)this).Input[0] - num) / num * 100.0;
		}
	}
}
