using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The TSF (Time Series Forecast) calculates probable future values for the price
/// by fitting a linear regression line over a given number of price bars and following
///  that line forward into the future. A linear regression line is a straight line which
///  is as close to all of the given price points as possible. Also see the Linear Regression indicator.
/// </summary>
public class TSF : Indicator
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

	private Series<double> y;

	[Range(-10, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Forecast", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Forecast { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionTSF;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameTSF;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			Period = 14;
			Forecast = 3;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameTSF);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			avg = (divisor = (intercept = (myPeriod = (priorSumXy = (priorSumY = (slope = (sumX = (sumX2 = (sumY = (sumXy = 0.0))))))))));
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			sum = SUM(((NinjaScriptBase)this).Inputs[0], Period);
			y = new Series<double>((NinjaScriptBase)(object)this);
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
			y[0] = ((NinjaScriptBase)this).Input[0];
			double num4 = ((double)Period * num3 - num * ((NinjaScriptBase)SUM((ISeries<double>)(object)y, Period))[0]) / num2;
			double num5 = (((NinjaScriptBase)SUM((ISeries<double>)(object)y, Period))[0] - num4 * num) / (double)Period;
			((NinjaScriptBase)this).Value[0] = num5 + num4 * (double)(Period - 1 + Forecast);
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
		((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? num6 : (intercept + slope * (myPeriod - 1.0 + (double)Forecast)));
	}
}
