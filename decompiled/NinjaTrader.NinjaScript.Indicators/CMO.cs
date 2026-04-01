using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The CMO differs from other momentum oscillators such as Relative Strength Index (RSI) and Stochastics.
/// It uses both up and down days data in the numerator of the calculation to measure momentum directly.
/// Primarily used to look for extreme overbought and oversold conditions, CMO can also be used to look for trends.
/// </summary>
public class CMO : Indicator
{
	private Series<double> down;

	private Series<double> up;

	private SUM sumDown;

	private SUM sumUp;

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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionCMO;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameCMO;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = false;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameCMO);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			down = new Series<double>((NinjaScriptBase)(object)this);
			up = new Series<double>((NinjaScriptBase)(object)this);
			sumDown = SUM((ISeries<double>)(object)down, Period);
			sumUp = SUM((ISeries<double>)(object)up, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			down[0] = 0.0;
			up[0] = 0.0;
			return;
		}
		double num = ((NinjaScriptBase)this).Input[0];
		double num2 = ((NinjaScriptBase)this).Input[1];
		down[0] = Math.Max(num2 - num, 0.0);
		up[0] = Math.Max(num - num2, 0.0);
		double num3 = ((NinjaScriptBase)sumDown)[0];
		double num4 = ((NinjaScriptBase)sumUp)[0];
		if (MathExtentions.ApproxCompare(num4, num3) == 0)
		{
			((NinjaScriptBase)this).Value[0] = 0.0;
		}
		else
		{
			((NinjaScriptBase)this).Value[0] = 100.0 * ((num4 - num3) / (num4 + num3));
		}
	}
}
