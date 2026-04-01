using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Donchian Channel. The Donchian Channel indicator was created by Richard Donchian.
///  It uses the highest high and the lowest low of a period of time to plot the channel.
/// </summary>
public class DonchianChannel : Indicator
{
	private MAX max;

	private MIN min;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Lower => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Mean => ((NinjaScriptBase)this).Values[0];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Upper => ((NinjaScriptBase)this).Values[1];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionDonchianChannel;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameDonchianChannel;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.DonchianChannelMean);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorUpper);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorLower);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			max = MAX(((NinjaScriptBase)this).High, Period);
			min = MIN(((NinjaScriptBase)this).Low, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)max)[0];
		double num2 = ((NinjaScriptBase)min)[0];
		((NinjaScriptBase)this).Value[0] = (num + num2) / 2.0;
		Upper[0] = num;
		Lower[0] = num2;
	}
}
