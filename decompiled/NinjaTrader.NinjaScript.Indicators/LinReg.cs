using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Linear Regression. The Linear Regression is an indicator that 'predicts' the value of a security's price.
/// </summary>
public class LinReg : Indicator
{
	private double avg;

	private double divisor;

	private double intercept;

	private double myPeriod;

	private double priorSumXy;

	private double priorSumY;

	private double slope;

	private double sumX2;

	private double sumX;

	private double sumXy;

	private double sumY;

	private SUM sum;

	[Range(2, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionLinReg;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameLinReg;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameLinReg);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			avg = (divisor = (intercept = (myPeriod = (priorSumXy = (priorSumY = (slope = (sumX = (sumX2 = (sumY = (sumXy = 0.0))))))))));
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			sum = SUM(((NinjaScriptBase)this).Inputs[0], Period);
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
			double num4 = ((double)Period * num3 - num * ((NinjaScriptBase)SUM(((NinjaScriptBase)this).Inputs[0], Period))[0]) / num2;
			double num5 = (((NinjaScriptBase)SUM(((NinjaScriptBase)this).Inputs[0], Period))[0] - num4 * num) / (double)Period;
			((NinjaScriptBase)this).Value[0] = num5 + num4 * (double)(Period - 1);
			return;
		}
		if (((NinjaScriptBase)this).IsFirstTickOfBar)
		{
			priorSumY = sumY;
			priorSumXy = sumXy;
			myPeriod = Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period);
			sumX = myPeriod * (myPeriod - 1.0) * 0.5;
			sumX2 = myPeriod * (myPeriod + 1.0) * 0.5;
			divisor = myPeriod * (myPeriod + 1.0) * (2.0 * myPeriod + 1.0) / 6.0 - sumX2 * sumX2 / myPeriod;
		}
		double num6 = ((NinjaScriptBase)this).Input[0];
		sumXy = priorSumXy - ((((NinjaScriptBase)this).CurrentBar >= Period) ? priorSumY : 0.0) + myPeriod * num6;
		sumY = priorSumY + num6 - ((((NinjaScriptBase)this).CurrentBar >= Period) ? ((NinjaScriptBase)this).Input[Period] : 0.0);
		avg = sumY / myPeriod;
		slope = (sumXy - sumX2 * avg) / divisor;
		intercept = (((NinjaScriptBase)sum)[0] - slope * sumX) / myPeriod;
		((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? num6 : (intercept + slope * (myPeriod - 1.0)));
	}
}
