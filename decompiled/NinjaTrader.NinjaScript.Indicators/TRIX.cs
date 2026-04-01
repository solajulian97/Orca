using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The TRIX (Triple Exponential Average) displays the percentage Rate of Change (ROC)
/// of a triple EMA. Trix oscillates above and below the zero value. The indicator
/// applies triple smoothing in an attempt to eliminate insignificant price movements
/// within the trend that you're trying to isolate.
/// </summary>
public class TRIX : Indicator
{
	private EMA emaDefault;

	private EMA emaTriple;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Default => ((NinjaScriptBase)this).Values[0];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal => ((NinjaScriptBase)this).Values[1];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "SignalPeriod", GroupName = "NinjaScriptParameters", Order = 1)]
	public int SignalPeriod { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionTRIX;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameTRIX;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			SignalPeriod = 3;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DimGray, Resource.NinjaScriptIndicatorDefault);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.TRIXSignal);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			emaTriple = EMA((ISeries<double>)(object)EMA((ISeries<double>)(object)EMA(((NinjaScriptBase)this).Inputs[0], Period), Period), Period);
			emaDefault = EMA((ISeries<double>)(object)Default, SignalPeriod);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)this).Input[0];
			return;
		}
		double num = ((NinjaScriptBase)emaTriple)[0];
		Default[0] = 100.0 * ((num - ((NinjaScriptBase)emaTriple)[1]) / num);
		Signal[0] = ((NinjaScriptBase)emaDefault)[0];
	}
}
