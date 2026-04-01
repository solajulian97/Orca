using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Standard Deviation is a statistical measure of volatility.
/// Standard Deviation is typically used as a component of other indicators,
/// rather than as a stand-alone indicator. For example, Bollinger Bands are
/// calculated by adding a security's Standard Deviation to a moving average.
/// </summary>
public class StdDev : Indicator
{
	private Series<double> sumSeries;

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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionStdDev;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameStdDev;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameStdDev);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			sumSeries = new Series<double>((NinjaScriptBase)(object)this, (MaximumBarsLookBack)(Period > 256));
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar < 1)
		{
			((NinjaScriptBase)this).Value[0] = 0.0;
			sumSeries[0] = ((NinjaScriptBase)this).Input[0];
			return;
		}
		sumSeries[0] = ((NinjaScriptBase)this).Input[0] + sumSeries[1] - ((((NinjaScriptBase)this).CurrentBar >= Period) ? ((NinjaScriptBase)this).Input[Period] : 0.0);
		double num = sumSeries[0] / (double)Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period);
		double num2 = 0.0;
		for (int num3 = Math.Min(((NinjaScriptBase)this).CurrentBar, Period - 1); num3 >= 0; num3--)
		{
			num2 += (((NinjaScriptBase)this).Input[num3] - num) * (((NinjaScriptBase)this).Input[num3] - num);
		}
		((NinjaScriptBase)this).Value[0] = Math.Sqrt(num2 / (double)Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period));
	}
}
