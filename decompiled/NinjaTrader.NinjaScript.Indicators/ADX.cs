using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Average Directional Index measures the strength of a prevailing trend as well as whether movement
/// exists in the market. The ADX is measured on a scale of 0  100. A low ADX value (generally less than 20)
/// can indicate a non-trending market with low volumes whereas a cross above 20 may indicate the start of
///  a trend (either up or down). If the ADX is over 40 and begins to fall, it can indicate the slowdown of a current trend.
/// </summary>
public class ADX : Indicator
{
	private Series<double> dmPlus;

	private Series<double> dmMinus;

	private Series<double> sumDmPlus;

	private Series<double> sumDmMinus;

	private Series<double> sumTr;

	private Series<double> tr;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionADX;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameADX;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameADX);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.SlateBlue, 25.0, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.Goldenrod, 75.0, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			dmPlus = new Series<double>((NinjaScriptBase)(object)this);
			dmMinus = new Series<double>((NinjaScriptBase)(object)this);
			sumDmPlus = new Series<double>((NinjaScriptBase)(object)this);
			sumDmMinus = new Series<double>((NinjaScriptBase)(object)this);
			sumTr = new Series<double>((NinjaScriptBase)(object)this);
			tr = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			tr[0] = num - num2;
			dmPlus[0] = 0.0;
			dmMinus[0] = 0.0;
			sumTr[0] = tr[0];
			sumDmPlus[0] = dmPlus[0];
			sumDmMinus[0] = dmMinus[0];
			((NinjaScriptBase)this).Value[0] = 50.0;
			return;
		}
		double num3 = ((NinjaScriptBase)this).High[1];
		double num4 = ((NinjaScriptBase)this).Low[1];
		double num5 = ((NinjaScriptBase)this).Close[1];
		tr[0] = Math.Max(Math.Abs(num2 - num5), Math.Max(num - num2, Math.Abs(num - num5)));
		dmPlus[0] = ((num - num3 > num4 - num2) ? Math.Max(num - num3, 0.0) : 0.0);
		dmMinus[0] = ((num4 - num2 > num - num3) ? Math.Max(num4 - num2, 0.0) : 0.0);
		if (((NinjaScriptBase)this).CurrentBar < Period)
		{
			sumTr[0] = sumTr[1] + tr[0];
			sumDmPlus[0] = sumDmPlus[1] + dmPlus[0];
			sumDmMinus[0] = sumDmMinus[1] + dmMinus[0];
		}
		else
		{
			double num6 = sumTr[1];
			double num7 = sumDmPlus[1];
			double num8 = sumDmMinus[1];
			sumTr[0] = num6 - num6 / (double)Period + tr[0];
			sumDmPlus[0] = num7 - num7 / (double)Period + dmPlus[0];
			sumDmMinus[0] = num8 - num8 / (double)Period + dmMinus[0];
		}
		double num9 = sumTr[0];
		double num10 = 100.0 * ((MathExtentions.ApproxCompare(num9, 0.0) == 0) ? 0.0 : (sumDmPlus[0] / sumTr[0]));
		double num11 = 100.0 * ((MathExtentions.ApproxCompare(num9, 0.0) == 0) ? 0.0 : (sumDmMinus[0] / sumTr[0]));
		double num12 = Math.Abs(num10 - num11);
		double num13 = num10 + num11;
		((NinjaScriptBase)this).Value[0] = ((MathExtentions.ApproxCompare(num13, 0.0) == 0) ? 50.0 : (((double)(Period - 1) * ((NinjaScriptBase)this).Value[1] + 100.0 * num12 / num13) / (double)Period));
	}
}
