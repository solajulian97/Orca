using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.
/// </summary>
public class SMA : Indicator
{
	private double priorSum;

	private double sum;

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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionSMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameSMA;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameSMA);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			priorSum = 0.0;
			sum = 0.0;
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
			double num = ((NinjaScriptBase)this).Value[1] * (double)Math.Min(((NinjaScriptBase)this).CurrentBar, Period);
			((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar >= Period) ? ((num + ((NinjaScriptBase)this).Input[0] - ((NinjaScriptBase)this).Input[Period]) / (double)Math.Min(((NinjaScriptBase)this).CurrentBar, Period)) : ((num + ((NinjaScriptBase)this).Input[0]) / (double)(Math.Min(((NinjaScriptBase)this).CurrentBar, Period) + 1)));
			return;
		}
		if (((NinjaScriptBase)this).IsFirstTickOfBar)
		{
			priorSum = sum;
		}
		sum = priorSum + ((NinjaScriptBase)this).Input[0] - ((((NinjaScriptBase)this).CurrentBar >= Period) ? ((NinjaScriptBase)this).Input[Period] : 0.0);
		((NinjaScriptBase)this).Value[0] = sum / (double)((((NinjaScriptBase)this).CurrentBar < Period) ? (((NinjaScriptBase)this).CurrentBar + 1) : Period);
	}
}
