using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The PPO (Percentage Price Oscillator) is based on two moving averages expressed as
/// a percentage. The PPO is found by subtracting the longer MA from the shorter MA and
/// then dividing the difference by the longer MA.
/// </summary>
public class PPO : Indicator
{
	private EMA emaFast;

	private EMA emaSlow;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Default => ((NinjaScriptBase)this).Values[0];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Fast { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Slow { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 2)]
	public int Smooth { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Smoothed => ((NinjaScriptBase)this).Values[1];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionPPO;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNamePPO;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Fast = 12;
			Slow = 26;
			Smooth = 9;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DimGray, Resource.NinjaScriptIndicatorDefault);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.PPOSmoothed);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			emaFast = EMA(Fast);
			emaSlow = EMA(Slow);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)emaSlow)[0];
		Default[0] = 100.0 * ((((NinjaScriptBase)emaFast)[0] - num) / num);
		Smoothed[0] = ((NinjaScriptBase)EMA((ISeries<double>)(object)((NinjaScriptBase)this).Values[0], Smooth))[0];
	}
}
