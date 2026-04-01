using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

public class ChoppinessIndex : Indicator
{
	[Range(2, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionChoppinessIndex;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameChoppinessIndex;
			((NinjaScriptBase)this).IsOverlay = false;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameChoppinessIndex);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 38.2, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 62.8, Resource.NinjaScriptIndicatorUpper);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = ((MathExtentions.ApproxCompare(((NinjaScriptBase)MAX(((NinjaScriptBase)this).High, Period))[0] - ((NinjaScriptBase)MIN(((NinjaScriptBase)this).Low, Period))[0], 0.0) == 0 || MathExtentions.ApproxCompare(((NinjaScriptBase)SUM((ISeries<double>)(object)ATR(1), Period))[0], 0.0) == 0) ? 0.0 : (100.0 * Math.Log10(((NinjaScriptBase)SUM((ISeries<double>)(object)ATR(1), Period))[0] / (((NinjaScriptBase)MAX(((NinjaScriptBase)this).High, Period))[0] - ((NinjaScriptBase)MIN(((NinjaScriptBase)this).Low, Period))[0])) / Math.Log10(Period)));
	}
}
