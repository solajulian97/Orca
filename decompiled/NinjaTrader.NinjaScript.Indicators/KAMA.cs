using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Kaufman's Adaptive Moving Average. Developed by Perry Kaufman, this indicator is an
/// EMA using an Efficiency Ratio to modify the smoothing constant, which ranges from
/// a minimum of Fast Length to a maximum of Slow Length. Since this moving average is
/// adaptive it tends to follow prices more closely than other MA's.
/// </summary>
public class KAMA : Indicator
{
	private Series<double> diffSeries;

	private double fastCf;

	private double slowCf;

	private SUM sum;

	[Range(1, 125)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Fast { get; set; }

	[Range(5, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Period { get; set; }

	[Range(1, 125)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 2)]
	public int Slow { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionKAMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameKAMA;
			Fast = 2;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			Period = 10;
			Slow = 30;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameKAMA);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			fastCf = 2.0 / (double)(Fast + 1);
			slowCf = 2.0 / (double)(Slow + 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			diffSeries = new Series<double>((NinjaScriptBase)(object)this);
			sum = SUM((ISeries<double>)(object)diffSeries, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).Input[0];
		diffSeries[0] = ((((NinjaScriptBase)this).CurrentBar > 0) ? Math.Abs(num - ((NinjaScriptBase)this).Input[1]) : num);
		if (((NinjaScriptBase)this).CurrentBar < Period)
		{
			((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)this).Input[0];
			return;
		}
		double num2 = Math.Abs(num - ((NinjaScriptBase)this).Input[Period]);
		double num3 = ((NinjaScriptBase)sum)[0];
		if (num3 == 0.0)
		{
			((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)this).Value[1];
			return;
		}
		double num4 = ((NinjaScriptBase)this).Value[1];
		((NinjaScriptBase)this).Value[0] = num4 + Math.Pow(num2 / num3 * (fastCf - slowCf) + slowCf, 2.0) * (num - num4);
	}
}
