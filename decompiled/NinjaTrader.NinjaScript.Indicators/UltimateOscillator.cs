using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Ultimate Oscillator is the weighted sum of three oscillators of different time periods.
/// The typical time periods are 7, 14 and 28. The values of the Ultimate Oscillator range
/// from zero to 100. Values over 70 indicate overbought conditions, and values under 30 indicate
/// oversold conditions. Also look for agreement/divergence with the price to confirm a trend or signal the end of a trend.
/// </summary>
public class UltimateOscillator : Indicator
{
	private Series<double> buyingPressure;

	private double constant1;

	private double constant2;

	private double constant3;

	private SUM sumBpFast;

	private SUM sumBpIntermediate;

	private SUM sumBpSlow;

	private SUM sumTrFast;

	private SUM sumTrIntermediate;

	private SUM sumTrSlow;

	private Series<double> trueRange;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Fast { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Intermediate", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Intermediate { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 2)]
	public int Slow { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Invalid comparison between Unknown and I4
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionUltimateOscillator;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameUltimateOscillator;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Fast = 7;
			Intermediate = 14;
			Slow = 28;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameUltimateOscillator);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 30.0, Resource.NinjaScriptIndicatorOversold);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 50.0, Resource.NinjaScriptIndicatorNeutral);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 70.0, Resource.NinjaScriptIndicatorOverbought);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			constant1 = Slow / Fast;
			constant2 = Slow / Intermediate;
			constant3 = constant1 + constant2 + 1.0;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			buyingPressure = new Series<double>((NinjaScriptBase)(object)this);
			trueRange = new Series<double>((NinjaScriptBase)(object)this);
			sumBpFast = SUM((ISeries<double>)(object)buyingPressure, Fast);
			sumBpIntermediate = SUM((ISeries<double>)(object)buyingPressure, Intermediate);
			sumBpSlow = SUM((ISeries<double>)(object)buyingPressure, Slow);
			sumTrFast = SUM((ISeries<double>)(object)trueRange, Fast);
			sumTrIntermediate = SUM((ISeries<double>)(object)trueRange, Intermediate);
			sumTrSlow = SUM((ISeries<double>)(object)trueRange, Slow);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			((NinjaScriptBase)this).Value[0] = 0.0;
			return;
		}
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		double num3 = ((NinjaScriptBase)this).Close[0];
		double num4 = ((NinjaScriptBase)this).Close[1];
		buyingPressure[0] = num3 - Math.Min(num2, num4);
		trueRange[0] = Math.Max(Math.Max(num - num2, num - num4), num4 - num2);
		if (((NinjaScriptBase)sumTrFast)[0] == 0.0 || ((NinjaScriptBase)sumTrIntermediate)[0] == 0.0 || ((NinjaScriptBase)sumTrSlow)[0] == 0.0)
		{
			((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)this).Value[1];
		}
		else
		{
			((NinjaScriptBase)this).Value[0] = (((NinjaScriptBase)sumBpFast)[0] / ((NinjaScriptBase)sumTrFast)[0] * constant1 + ((NinjaScriptBase)sumBpIntermediate)[0] / ((NinjaScriptBase)sumTrIntermediate)[0] * constant2 + ((NinjaScriptBase)sumBpSlow)[0] / ((NinjaScriptBase)sumTrSlow)[0]) / constant3 * 100.0;
		}
	}
}
