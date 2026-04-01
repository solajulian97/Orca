using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The PFE (Polarized Fractal Efficiency) is an indicator that uses fractal
///  geometry to determine how efficiently the price is moving.
/// </summary>
public class PFE : Indicator
{
	private Series<double> div;

	private EMA ema;

	private Series<double> pfeSeries;

	private Series<double> singlePfeSeries;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Smooth { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionPFE;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNamePFE;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			Smooth = 10;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNamePFE);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.PFEZero);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			div = new Series<double>((NinjaScriptBase)(object)this);
			pfeSeries = new Series<double>((NinjaScriptBase)(object)this);
			singlePfeSeries = new Series<double>((NinjaScriptBase)(object)this);
			ema = EMA((ISeries<double>)(object)pfeSeries, Smooth);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).Input[0];
		if (((NinjaScriptBase)this).CurrentBar < Period)
		{
			singlePfeSeries[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? 1.0 : Math.Sqrt(Math.Pow(((NinjaScriptBase)this).Input[1] - num, 2.0) + 1.0));
			div[0] = singlePfeSeries[0] + ((((NinjaScriptBase)this).CurrentBar > 0) ? div[1] : 0.0);
			return;
		}
		double num2 = ((NinjaScriptBase)this).Input[1];
		double num3 = ((NinjaScriptBase)this).Input[Period];
		singlePfeSeries[0] = Math.Sqrt(Math.Pow(num2 - num, 2.0) + 1.0);
		div[0] = singlePfeSeries[0] + div[1] - singlePfeSeries[Period];
		pfeSeries[0] = (double)((!(num < num3)) ? 1 : (-1)) * (Math.Sqrt(Math.Pow(num - num3, 2.0) + Math.Pow(Period, 2.0)) / div[0]);
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)ema)[0];
	}
}
