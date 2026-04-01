using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Momentum indicator measures the amount that a security's price has changed over a given time span.
/// </summary>
public class Momentum : Indicator
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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionMomentum;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameMomentum;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameMomentum);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.SlateBlue, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? 0.0 : (((NinjaScriptBase)this).Input[0] - ((NinjaScriptBase)this).Input[Math.Min(((NinjaScriptBase)this).CurrentBar, Period)]));
	}
}
