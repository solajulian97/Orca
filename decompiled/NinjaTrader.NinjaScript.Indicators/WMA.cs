using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The WMA (Weighted Moving Average) is a Moving Average indicator that shows the average
/// value of a security's price over a period of time with special emphasis on the more recent
/// portions of the time period under analysis as opposed to the earlier.
/// </summary>
public class WMA : Indicator
{
	private int myPeriod;

	private double priorSum;

	private double priorWsum;

	private double sum;

	private double wsum;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionWMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameWMA;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameWMA);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			priorSum = 0.0;
			priorWsum = 0.0;
			sum = 0.0;
			wsum = 0.0;
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
		{
			if (((NinjaScriptBase)this).CurrentBar == 0)
			{
				((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)this).Input[0];
				return;
			}
			int num = Math.Min(Period - 1, ((NinjaScriptBase)this).CurrentBar);
			double num2 = 0.0;
			int num3 = 0;
			for (int num4 = num; num4 >= 0; num4--)
			{
				num2 += (double)(num4 + 1) * ((NinjaScriptBase)this).Input[num - num4];
				num3 += num4 + 1;
			}
			((NinjaScriptBase)this).Value[0] = num2 / (double)num3;
		}
		else
		{
			if (((NinjaScriptBase)this).IsFirstTickOfBar)
			{
				priorWsum = wsum;
				priorSum = sum;
				myPeriod = Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period);
			}
			wsum = priorWsum - ((((NinjaScriptBase)this).CurrentBar >= Period) ? priorSum : 0.0) + (double)myPeriod * ((NinjaScriptBase)this).Input[0];
			sum = priorSum + ((NinjaScriptBase)this).Input[0] - ((((NinjaScriptBase)this).CurrentBar >= Period) ? ((NinjaScriptBase)this).Input[Period] : 0.0);
			((NinjaScriptBase)this).Value[0] = wsum / (0.5 * (double)myPeriod * (double)(myPeriod + 1));
		}
	}
}
