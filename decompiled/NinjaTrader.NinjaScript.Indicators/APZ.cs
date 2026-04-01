using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The APZ (Adaptive Prize Zone) forms a steady channel based on double smoothed
/// exponential moving averages around the average price. See S/C, September 2006, p.28.
/// </summary>
public class APZ : Indicator
{
	private EMA emaEma;

	private EMA emaRange;

	private int newPeriod;

	private int period;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "BandPct", GroupName = "NinjaScriptParameters", Order = 0)]
	public double BandPct { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Lower => ((NinjaScriptBase)this).Values[0];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Period
	{
		get
		{
			return period;
		}
		set
		{
			period = value;
			newPeriod = Convert.ToInt32(Math.Sqrt(Convert.ToDouble(value)));
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Upper => ((NinjaScriptBase)this).Values[1];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionAPZ;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameAPZ;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			BandPct = 2.0;
			Period = 20;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			emaEma = EMA((ISeries<double>)(object)EMA(newPeriod), newPeriod);
			emaRange = EMA((ISeries<double>)(object)Range(), Period);
			newPeriod = 0;
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar < Period)
		{
			Lower[0] = ((NinjaScriptBase)this).Input[0];
			Upper[0] = ((NinjaScriptBase)this).Input[0];
			return;
		}
		double num = BandPct * ((NinjaScriptBase)emaRange)[0];
		double num2 = ((NinjaScriptBase)emaEma)[0];
		Lower[0] = num2 - num;
		Upper[0] = num2 + num;
	}
}
