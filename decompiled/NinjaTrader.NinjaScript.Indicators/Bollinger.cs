using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Bollinger Bands are plotted at standard deviation levels above and below a moving average.
/// Since standard deviation is a measure of volatility, the bands are self-adjusting:
/// widening during volatile markets and contracting during calmer periods.
/// </summary>
public class Bollinger : Indicator
{
	private SMA sma;

	private StdDev stdDev;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Lower => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Middle => ((NinjaScriptBase)this).Values[1];

	[Range(0, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "NumStdDev", GroupName = "NinjaScriptParameters", Order = 0)]
	public double NumStdDev { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Upper => ((NinjaScriptBase)this).Values[0];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionBollinger;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameBollinger;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			NumStdDev = 2.0;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.BollingerUpperBand);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.BollingerMiddleBand);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.BollingerLowerBand);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			sma = SMA(Period);
			stdDev = StdDev(Period);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)sma)[0];
		double num2 = ((NinjaScriptBase)stdDev)[0];
		Upper[0] = num + NumStdDev * num2;
		Middle[0] = num;
		Lower[0] = num - NumStdDev * num2;
	}
}
