using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Price Oscillator indicator shows the variation among two moving averages for the price of a security.
/// </summary>
public class PriceOscillator : Indicator
{
	private EMA emaFast;

	private EMA emaSlow;

	private EMA emaSmooth;

	private Series<double> smoothEma;

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

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionPriceOscillator;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNamePriceOscillator;
			Fast = 12;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Slow = 26;
			Smooth = 9;
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNamePriceOscillator);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			smoothEma = new Series<double>((NinjaScriptBase)(object)this);
			emaFast = EMA(Fast);
			emaSlow = EMA(Slow);
			emaSmooth = EMA((ISeries<double>)(object)smoothEma, Smooth);
		}
	}

	protected override void OnBarUpdate()
	{
		smoothEma[0] = ((NinjaScriptBase)emaFast)[0] - ((NinjaScriptBase)emaSlow)[0];
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)emaSmooth)[0];
	}
}
