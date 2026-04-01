using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Aroon Oscillator is based upon his Aroon Indicator. Much like the Aroon Indicator,
///  the Aroon Oscillator measures the strength of a trend.
/// </summary>
public class AroonOscillator : Indicator
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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionAroonOscillator;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameAroonOscillator;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorUp);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			((NinjaScriptBase)this).Value[0] = 0.0;
			return;
		}
		int num = Math.Min(Period, ((NinjaScriptBase)this).CurrentBar);
		int num2 = -1;
		int num3 = -1;
		double num4 = double.MinValue;
		double num5 = double.MaxValue;
		for (int num6 = num; num6 >= 0; num6--)
		{
			if (MathExtentions.ApproxCompare(((NinjaScriptBase)this).High[num - num6], num4) >= 0)
			{
				num4 = ((NinjaScriptBase)this).High[num - num6];
				num2 = ((NinjaScriptBase)this).CurrentBar - num + num6;
			}
			if (MathExtentions.ApproxCompare(((NinjaScriptBase)this).Low[num - num6], num5) <= 0)
			{
				num5 = ((NinjaScriptBase)this).Low[num - num6];
				num3 = ((NinjaScriptBase)this).CurrentBar - num + num6;
			}
		}
		((NinjaScriptBase)this).Value[0] = 100.0 * ((double)(num - (((NinjaScriptBase)this).CurrentBar - num2)) / (double)num) - 100.0 * ((double)(num - (((NinjaScriptBase)this).CurrentBar - num3)) / (double)num);
	}
}
