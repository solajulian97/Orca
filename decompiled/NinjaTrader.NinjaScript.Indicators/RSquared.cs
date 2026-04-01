using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// R-squared indicator
/// </summary>
public class RSquared : Indicator
{
	private double myPeriod;

	private double priorSumXy;

	private double priorSumY;

	private double priorSumY2;

	private double sumX;

	private double sumX2;

	private double sumXy;

	private double sumY;

	private double sumY2;

	private double denominator;

	private double r;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionRSquared;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameRSquared;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 8;
			((NinjaScriptBase)this).IsOverlay = false;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.NinjaScriptIndicatorNameRSquared);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.SlateBlue, 0.2, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.Goldenrod, 0.75, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			priorSumXy = (priorSumY = (priorSumY2 = (sumX = (sumXy = (sumX2 = (sumY2 = (denominator = 0.0)))))));
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
		{
			sumX = (double)Period * (double)(Period - 1) * 0.5;
			sumXy = 0.0;
			sumX2 = 0.0;
			sumY2 = 0.0;
			for (int i = 0; i < Period && ((NinjaScriptBase)this).CurrentBar - i >= 0; i++)
			{
				sumXy += (double)i * ((NinjaScriptBase)this).Input[i];
				sumX2 += i * i;
				sumY2 += ((NinjaScriptBase)this).Input[i] * ((NinjaScriptBase)this).Input[i];
			}
			double num = (double)Period * sumXy - sumX * ((NinjaScriptBase)SUM(((NinjaScriptBase)this).Inputs[0], Period))[0];
			denominator = ((double)Period * sumX2 - sumX * sumX) * ((double)Period * sumY2 - ((NinjaScriptBase)SUM(((NinjaScriptBase)this).Inputs[0], Period))[0] * ((NinjaScriptBase)SUM(((NinjaScriptBase)this).Inputs[0], Period))[0]);
			r = ((denominator > 0.0) ? Math.Pow(num / Math.Sqrt(denominator), 2.0) : 0.0);
			((NinjaScriptBase)this).Value[0] = r;
			return;
		}
		if (((NinjaScriptBase)this).IsFirstTickOfBar)
		{
			priorSumXy = sumXy;
			priorSumY = sumY;
			priorSumY2 = sumY2;
			myPeriod = Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period);
			sumX = myPeriod * (myPeriod + 1.0) * 0.5;
			sumX2 = sumX * (2.0 * myPeriod + 1.0) / 3.0;
		}
		double num2 = ((NinjaScriptBase)this).Input[0];
		double num3 = ((NinjaScriptBase)this).Input[Math.Min(Period, ((NinjaScriptBase)this).CurrentBar)];
		sumXy = priorSumXy - ((((NinjaScriptBase)this).CurrentBar >= Period) ? priorSumY : 0.0) + myPeriod * num2;
		sumY = priorSumY + num2 - ((((NinjaScriptBase)this).CurrentBar >= Period) ? num3 : 0.0);
		sumY2 = priorSumY2 + num2 * num2 - ((((NinjaScriptBase)this).CurrentBar >= Period) ? (num3 * num3) : 0.0);
		denominator = (myPeriod * sumX2 - sumX * sumX) * (myPeriod * sumY2 - sumY * sumY);
		r = ((denominator > 0.0) ? ((myPeriod * sumXy - sumX * sumY) / Math.Sqrt(denominator)) : 0.0);
		((NinjaScriptBase)this).Value[0] = r * r;
	}
}
