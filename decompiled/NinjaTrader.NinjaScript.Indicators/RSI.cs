using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The RSI (Relative Strength Index) is a price-following oscillator that ranges between 0 and 100.
/// </summary>
public class RSI : Indicator
{
	private Series<double> avgDown;

	private Series<double> avgUp;

	private double constant1;

	private double constant2;

	private double constant3;

	private Series<double> down;

	private SMA smaDown;

	private SMA smaUp;

	private Series<double> up;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Avg => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Default => ((NinjaScriptBase)this).Values[0];

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
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Invalid comparison between Unknown and I4
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionRSI;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameRSI;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).BarsRequiredToPlot = 20;
			Period = 14;
			Smooth = 3;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameRSI);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorAvg);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 30.0, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 70.0, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			constant1 = 2.0 / (double)(1 + Smooth);
			constant2 = 1.0 - 2.0 / (double)(1 + Smooth);
			constant3 = Period - 1;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			avgUp = new Series<double>((NinjaScriptBase)(object)this);
			avgDown = new Series<double>((NinjaScriptBase)(object)this);
			down = new Series<double>((NinjaScriptBase)(object)this);
			up = new Series<double>((NinjaScriptBase)(object)this);
			smaDown = SMA((ISeries<double>)(object)down, Period);
			smaUp = SMA((ISeries<double>)(object)up, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			down[0] = 0.0;
			up[0] = 0.0;
			if (Period < 3)
			{
				Avg[0] = 50.0;
			}
			return;
		}
		double num = ((NinjaScriptBase)this).Input[0];
		double num2 = ((NinjaScriptBase)this).Input[1];
		down[0] = Math.Max(num2 - num, 0.0);
		up[0] = Math.Max(num - num2, 0.0);
		if (((NinjaScriptBase)this).CurrentBar + 1 < Period)
		{
			if (((NinjaScriptBase)this).CurrentBar + 1 == Period - 1)
			{
				Avg[0] = 50.0;
			}
			return;
		}
		if (((NinjaScriptBase)this).CurrentBar + 1 == Period)
		{
			avgDown[0] = ((NinjaScriptBase)smaDown)[0];
			avgUp[0] = ((NinjaScriptBase)smaUp)[0];
		}
		else
		{
			avgDown[0] = (avgDown[1] * constant3 + down[0]) / (double)Period;
			avgUp[0] = (avgUp[1] * constant3 + up[0]) / (double)Period;
		}
		double num3 = avgDown[0];
		double num4 = ((num3 == 0.0) ? 100.0 : (100.0 - 100.0 / (1.0 + avgUp[0] / num3)));
		Default[0] = num4;
		Avg[0] = constant1 * num4 + constant2 * Avg[1];
	}
}
