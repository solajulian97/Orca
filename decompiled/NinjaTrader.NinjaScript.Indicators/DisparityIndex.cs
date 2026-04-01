using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Disparity Index measures the difference between the price and an exponential moving average. A value greater could suggest bullish momentum, while a value less than zero could suggest bearish momentum.
/// </summary>
public class DisparityIndex : Indicator
{
	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> DisparityLine => ((NinjaScriptBase)this).Values[0];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionDisparityIndex;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameDisparityIndex;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 25;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorDisparityLine);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
	}

	protected override void OnBarUpdate()
	{
		if (!(((NinjaScriptBase)this).Close[0] <= 0.0))
		{
			DisparityLine[0] = 100.0 * (((NinjaScriptBase)this).Close[0] - ((NinjaScriptBase)EMA(((NinjaScriptBase)this).Close, Period))[0]) / ((NinjaScriptBase)this).Close[0];
		}
	}
}
