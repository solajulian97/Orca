using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Williams %R is a momentum indicator that is designed to identify overbought and oversold areas in a nontrending market.
/// </summary>
public class WilliamsR : Indicator
{
	private MAX max;

	private MIN min;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionWilliamsR;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameWilliamsR;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, -25.0, Resource.NinjaScriptIndicatorUpper);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, -75.0, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.WilliamsPercentR);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			max = MAX(((NinjaScriptBase)this).High, Period);
			min = MIN(((NinjaScriptBase)this).Low, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)max)[0];
		double num2 = ((NinjaScriptBase)min)[0];
		((NinjaScriptBase)this).Value[0] = -100.0 * (num - ((NinjaScriptBase)this).Close[0]) / ((num - num2 == 0.0) ? 1.0 : (num - num2));
	}
}
