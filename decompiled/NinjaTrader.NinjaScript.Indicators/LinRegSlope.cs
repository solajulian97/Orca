using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Linear Regression Slope
/// </summary>
public class LinRegSlope : Indicator
{
	private double avg;

	private double divisor;

	private double myPeriod;

	private double priorSumXy;

	private double priorSumY;

	private double sumX2;

	private double sumXy;

	private double sumY;

	[Range(2, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionLinRegSlope;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameLinRegSlope;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameLinRegSlope);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			avg = (divisor = (myPeriod = (priorSumXy = (priorSumY = (sumX2 = (sumY = (sumXy = 0.0)))))));
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
		{
			double num = (double)Period * (double)(Period - 1) * 0.5;
			double num2 = num * num - (double)Period * (double)Period * (double)(Period - 1) * (double)(2 * Period - 1) / 6.0;
			double num3 = 0.0;
			for (int i = 0; i < Period && ((NinjaScriptBase)this).CurrentBar - i >= 0; i++)
			{
				num3 += (double)i * ((NinjaScriptBase)this).Input[i];
			}
			((NinjaScriptBase)this).Value[0] = ((double)Period * num3 - num * ((NinjaScriptBase)SUM(((NinjaScriptBase)this).Inputs[0], Period))[0]) / num2;
			return;
		}
		if (((NinjaScriptBase)this).IsFirstTickOfBar)
		{
			priorSumY = sumY;
			priorSumXy = sumXy;
			myPeriod = Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period);
			sumX2 = myPeriod * (myPeriod + 1.0) * 0.5;
			divisor = myPeriod * (myPeriod + 1.0) * (2.0 * myPeriod + 1.0) / 6.0 - sumX2 * sumX2 / myPeriod;
		}
		double num4 = ((NinjaScriptBase)this).Input[0];
		sumXy = priorSumXy - ((((NinjaScriptBase)this).CurrentBar >= Period) ? priorSumY : 0.0) + myPeriod * num4;
		sumY = priorSumY + num4 - ((((NinjaScriptBase)this).CurrentBar >= Period) ? ((NinjaScriptBase)this).Input[Period] : 0.0);
		avg = sumY / myPeriod;
		((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar <= Period) ? 0.0 : ((sumXy - sumX2 * avg) / divisor));
	}
}
