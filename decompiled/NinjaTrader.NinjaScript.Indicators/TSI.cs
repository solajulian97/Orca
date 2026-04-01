using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The TSI (True Strength Index) is a momentum-based indicator, developed by William Blau.
/// Designed to determine both trend and overbought/oversold conditions, the TSI is
/// applicable to intraday time frames as well as long term trading.
/// </summary>
public class TSI : Indicator
{
	private double constant1;

	private double constant2;

	private double constant3;

	private double constant4;

	private Series<double> fastEma;

	private Series<double> fastAbsEma;

	private Series<double> slowEma;

	private Series<double> slowAbsEma;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Fast { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Slow { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionTSI;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameTSI;
			Fast = 3;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Slow = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameTSI);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			constant1 = 2.0 / (double)(1 + Slow);
			constant2 = 1.0 - 2.0 / (double)(1 + Slow);
			constant3 = 2.0 / (double)(1 + Fast);
			constant4 = 1.0 - 2.0 / (double)(1 + Fast);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			fastAbsEma = new Series<double>((NinjaScriptBase)(object)this);
			fastEma = new Series<double>((NinjaScriptBase)(object)this);
			slowAbsEma = new Series<double>((NinjaScriptBase)(object)this);
			slowEma = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			fastAbsEma[0] = 0.0;
			fastEma[0] = 0.0;
			slowAbsEma[0] = 0.0;
			slowEma[0] = 0.0;
			((NinjaScriptBase)this).Value[0] = 0.0;
		}
		else
		{
			double num = ((NinjaScriptBase)this).Input[0] - ((NinjaScriptBase)this).Input[1];
			slowEma[0] = num * constant1 + constant2 * slowEma[1];
			fastEma[0] = slowEma[0] * constant3 + constant4 * fastEma[1];
			slowAbsEma[0] = Math.Abs(num) * constant1 + constant2 * slowAbsEma[1];
			fastAbsEma[0] = slowAbsEma[0] * constant3 + constant4 * fastAbsEma[1];
			((NinjaScriptBase)this).Value[0] = ((fastAbsEma[0] == 0.0) ? 0.0 : (100.0 * fastEma[0] / fastAbsEma[0]));
		}
	}
}
