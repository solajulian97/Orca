using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The MAMA (MESA Adaptive Moving Average) was developed by John Ehlers.
/// It adapts to price movement in a new and unique way. The adaptation is
/// based on the Hilbert Transform Discriminator. The adavantage of this method
/// features fast attack average and a slow decay average. The MAMA + the FAMA
/// (Following Adaptive Moving Average) lines only cross at major market reversals.
/// </summary>
public class MAMA : Indicator
{
	private Series<double> detrender;

	private Series<double> i1;

	private Series<double> i2;

	private Series<double> im;

	private Series<double> period;

	private Series<double> phase;

	private Series<double> q1;

	private Series<double> q2;

	private Series<double> re;

	private Series<double> smooth;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Default => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Fama => ((NinjaScriptBase)this).Values[1];

	[Range(0.05, 2147483647.0)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "FastLimit", GroupName = "NinjaScriptParameters", Order = 0)]
	public double FastLimit { get; set; }

	[Range(0.005, 2147483647.0)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "SlowLimit", GroupName = "NinjaScriptParameters", Order = 1)]
	public double SlowLimit { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionMAMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameMAMA;
			FastLimit = 0.5;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			SlowLimit = 0.05;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorDefault);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.MAMAFAMA);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			detrender = new Series<double>((NinjaScriptBase)(object)this);
			period = new Series<double>((NinjaScriptBase)(object)this);
			smooth = new Series<double>((NinjaScriptBase)(object)this);
			i1 = new Series<double>((NinjaScriptBase)(object)this);
			i2 = new Series<double>((NinjaScriptBase)(object)this);
			im = new Series<double>((NinjaScriptBase)(object)this);
			q1 = new Series<double>((NinjaScriptBase)(object)this);
			q2 = new Series<double>((NinjaScriptBase)(object)this);
			re = new Series<double>((NinjaScriptBase)(object)this);
			phase = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar < 6)
		{
			return;
		}
		if (((NinjaScriptBase)this).CurrentBar == 6)
		{
			Default[0] = (((NinjaScriptBase)this).High[0] + ((NinjaScriptBase)this).Low[0]) / 2.0;
			Fama[0] = ((NinjaScriptBase)this).Input[0];
			return;
		}
		double num = period[1];
		smooth[0] = (4.0 * ((NinjaScriptBase)this).Median[0] + 3.0 * ((NinjaScriptBase)this).Median[1] + 2.0 * ((NinjaScriptBase)this).Median[2] + ((NinjaScriptBase)this).Median[3]) / 10.0;
		detrender[0] = (0.0962 * smooth[0] + 0.5769 * smooth[2] - 0.5769 * smooth[4] - 0.0962 * smooth[6]) * (0.075 * num + 0.54);
		q1[0] = (0.0962 * detrender[0] + 0.5769 * detrender[2] - 0.5769 * detrender[4] - 0.0962 * detrender[6]) * (0.075 * num + 0.54);
		i1[0] = detrender[3];
		double num2 = i1[0];
		double num3 = (0.0962 * num2 + 0.5769 * i1[2] - 0.5769 * i1[4] - 0.0962 * i1[6]) * (0.075 * num + 0.54);
		double num4 = (0.0962 * q1[0] + 0.5769 * q1[2] - 0.5769 * q1[4] - 0.0962 * q1[6]) * (0.075 * num + 0.54);
		i2[0] = num2 - num4;
		q2[0] = q1[0] + num3;
		i2[0] = 0.2 * i2[0] + 0.8 * i2[1];
		q2[0] = 0.2 * q2[0] + 0.8 * q2[1];
		double num5 = i2[0];
		double num6 = q2[1];
		double num7 = i2[1];
		double num8 = q2[0];
		double num9 = period[0];
		re[0] = num5 * num7 + num8 * num6;
		im[0] = num5 * num6 - num8 * num7;
		re[0] = 0.2 * re[0] + 0.8 * re[1];
		im[0] = 0.2 * im[0] + 0.8 * im[1];
		if (im[0] != 0.0 && re[0] != 0.0)
		{
			num9 = 360.0 / (180.0 / Math.PI * Math.Atan(im[0] / re[0]));
		}
		if (num9 > 1.5 * num)
		{
			num9 = 1.5 * num;
		}
		if (num9 < 0.67 * num)
		{
			num9 = 0.67 * num;
		}
		if (num9 < 6.0)
		{
			num9 = 6.0;
		}
		if (num9 > 50.0)
		{
			num9 = 50.0;
		}
		period[0] = 0.2 * num9 + 0.8 * num;
		if (i1[0] != 0.0)
		{
			phase[0] = 180.0 / Math.PI * Math.Atan(q1[0] / i1[0]);
		}
		double num10 = phase[1] - phase[0];
		if (num10 < 1.0)
		{
			num10 = 1.0;
		}
		double num11 = FastLimit / num10;
		if (num11 < SlowLimit)
		{
			num11 = SlowLimit;
		}
		Default[0] = num11 * ((NinjaScriptBase)this).Median[0] + (1.0 - num11) * Default[1];
		Fama[0] = 0.5 * num11 * Default[0] + (1.0 - 0.5 * num11) * Fama[1];
	}
}
