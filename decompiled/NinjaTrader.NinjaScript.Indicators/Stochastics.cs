using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Stochastic Oscillator is made up of two lines that oscillate between
/// a vertical scale of 0 to 100. The %K is the main line and it is drawn as
/// a solid line. The second is the %D line and is a moving average of %K.
/// The %D line is drawn as a dotted line. Use as a buy/sell signal generator,
/// buying when fast moves above slow and selling when fast moves below slow.
/// </summary>
public class Stochastics : Indicator
{
	private Series<double> den;

	private Series<double> fastK;

	private MIN min;

	private MAX max;

	private Series<double> nom;

	private SMA smaFastK;

	private SMA smaK;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> D => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> K => ((NinjaScriptBase)this).Values[1];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "PeriodD", GroupName = "NinjaScriptParameters", Order = 0)]
	public int PeriodD { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "PeriodK", GroupName = "NinjaScriptParameters", Order = 1)]
	public int PeriodK { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 2)]
	public int Smooth { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionStochastics;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameStochastics;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			PeriodD = 7;
			PeriodK = 14;
			Smooth = 3;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.StochasticsD);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.StochasticsK);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 20.0, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 80.0, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			den = new Series<double>((NinjaScriptBase)(object)this);
			nom = new Series<double>((NinjaScriptBase)(object)this);
			fastK = new Series<double>((NinjaScriptBase)(object)this);
			min = MIN(((NinjaScriptBase)this).Low, PeriodK);
			max = MAX(((NinjaScriptBase)this).High, PeriodK);
			smaFastK = SMA((ISeries<double>)(object)fastK, Smooth);
			smaK = SMA((ISeries<double>)(object)K, PeriodD);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)min)[0];
		nom[0] = ((NinjaScriptBase)this).Close[0] - num;
		den[0] = ((NinjaScriptBase)max)[0] - num;
		fastK[0] = ((MathExtentions.ApproxCompare(den[0], 0.0) != 0) ? Math.Min(100.0, Math.Max(0.0, 100.0 * nom[0] / den[0])) : ((((NinjaScriptBase)this).CurrentBar == 0) ? 50.0 : fastK[1]));
		K[0] = ((NinjaScriptBase)smaFastK)[0];
		D[0] = ((NinjaScriptBase)smaK)[0];
	}
}
