using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Standard Error shows how near prices go around a linear regression line.
/// </summary>
public class StdError : Indicator
{
	private double avg;

	private double divisor;

	private double intercept;

	private int myPeriod;

	private double priorSumXy;

	private double priorSumY;

	private double slope;

	private double sumX2;

	private double sumX;

	private double sumXy;

	private double sumY;

	private SUM sum;

	private Series<double> y;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Lower => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Middle => ((NinjaScriptBase)this).Values[0];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Upper => ((NinjaScriptBase)this).Values[1];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionStdError;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameStdError;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameLinReg);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorUpper);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorLower);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			avg = (divisor = (intercept = (priorSumXy = (priorSumY = (slope = (sumX = (sumX2 = (sumY = (sumXy = 0.0)))))))));
			myPeriod = 0;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			y = new Series<double>((NinjaScriptBase)(object)this);
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
			y[0] = ((NinjaScriptBase)this).Input[0];
			double num4 = ((double)Period * num3 - num * ((NinjaScriptBase)SUM((ISeries<double>)(object)y, Period))[0]) / num2;
			double num5 = (((NinjaScriptBase)SUM((ISeries<double>)(object)y, Period))[0] - num4 * num) / (double)Period;
			double num6 = num5 + num4 * (double)(Period - 1);
			double num7 = 0.0;
			for (int j = 0; j < Period && ((NinjaScriptBase)this).CurrentBar - j >= 0; j++)
			{
				double num8 = num5 + num4 * (double)(Period - 1 - j);
				double num9 = Math.Abs(((NinjaScriptBase)this).Input[j] - num8);
				num7 += num9 * num9;
			}
			double num10 = Math.Sqrt(num7 / (double)Period);
			Middle[0] = num6;
			Upper[0] = num6 + num10;
			Lower[0] = num6 - num10;
			return;
		}
		if (((NinjaScriptBase)this).IsFirstTickOfBar)
		{
			priorSumY = sumY;
			priorSumXy = sumXy;
			myPeriod = Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period);
			sumX = (double)(myPeriod * (myPeriod - 1)) * 0.5;
			sumX2 = (double)(myPeriod * (myPeriod + 1)) * 0.5;
			divisor = ((myPeriod != 1) ? ((double)(myPeriod * (myPeriod + 1) * (2 * myPeriod + 1)) / 6.0 - sumX2 * sumX2 / (double)myPeriod) : 1.0);
		}
		double num11 = ((NinjaScriptBase)this).Input[0];
		sumXy = priorSumXy - ((((NinjaScriptBase)this).CurrentBar >= Period) ? priorSumY : 0.0) + (double)myPeriod * num11;
		sumY = priorSumY + num11 - ((((NinjaScriptBase)this).CurrentBar >= Period) ? ((NinjaScriptBase)this).Input[Period] : 0.0);
		avg = ((myPeriod != 0) ? (sumY / (double)myPeriod) : 0.0);
		slope = (sumXy - sumX2 * avg) / divisor;
		intercept = ((myPeriod != 0) ? ((((NinjaScriptBase)sum)[0] - slope * sumX) / (double)myPeriod) : 0.0);
		double num12 = intercept + slope * (double)(myPeriod - 1);
		double num13 = 0.0;
		for (int k = 0; k < Period && ((NinjaScriptBase)this).CurrentBar - k >= 0; k++)
		{
			double num14 = intercept + slope * (double)(Period - 1 - k);
			double num15 = Math.Abs(((NinjaScriptBase)this).Input[k] - num14);
			num13 += num15 * num15;
		}
		double num16 = Math.Sqrt(num13 / (double)Period);
		Middle[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? num11 : num12);
		Upper[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? num11 : (num12 + num16));
		Lower[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? num11 : (num12 - num16));
	}
}
