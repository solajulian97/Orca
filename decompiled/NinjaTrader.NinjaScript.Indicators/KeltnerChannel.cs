using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Keltner Channel. The Keltner Channel is a similar indicator to Bollinger Bands.
/// Here the midline is a standard moving average with the upper and lower bands offset
/// by the SMA of the difference between the high and low of the previous bars.
/// The offset multiplier as well as the SMA period is configurable.
/// </summary>
public class KeltnerChannel : Indicator
{
	private Series<double> diff;

	private SMA smaDiff;

	private SMA smaTypical;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Lower => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Midline => ((NinjaScriptBase)this).Values[0];

	[Range(0.01, 2147483647.0)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "OffsetMultiplier", GroupName = "NinjaScriptParameters", Order = 0)]
	public double OffsetMultiplier { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Upper => ((NinjaScriptBase)this).Values[1];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionKeltnerChannel;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameKelterChannel;
			Period = 10;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			OffsetMultiplier = 1.5;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkGray, Resource.KeltnerChannelMidline);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorUpper);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorLower);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			diff = new Series<double>((NinjaScriptBase)(object)this);
			smaDiff = SMA((ISeries<double>)(object)diff, Period);
			smaTypical = SMA(((NinjaScriptBase)this).Typical, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		diff[0] = ((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Low[0];
		double num = ((NinjaScriptBase)smaTypical)[0];
		double num2 = ((NinjaScriptBase)smaDiff)[0] * OffsetMultiplier;
		double num3 = num + num2;
		double num4 = num - num2;
		Midline[0] = num;
		Upper[0] = num3;
		Lower[0] = num4;
	}
}
