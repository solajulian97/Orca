using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Hull Moving Average (HMA) employs weighted MA calculations to offer superior
/// smoothing, and much less lag, over traditional SMA indicators.
/// This indicator is based on the reference article found here:
/// http://www.justdata.com.au/Journals/AlanHull/hull_ma.htm
/// </summary>
public class HMA : Indicator
{
	private Series<double> diffSeries;

	private WMA wma1;

	private WMA wma2;

	private WMA wmaDiffSeries;

	[Range(2, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionHMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameHMA;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameHMA);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			diffSeries = new Series<double>((NinjaScriptBase)(object)this);
			wma1 = WMA(((NinjaScriptBase)this).Inputs[0], Period / 2);
			wma2 = WMA(((NinjaScriptBase)this).Inputs[0], Period);
			wmaDiffSeries = WMA((ISeries<double>)(object)diffSeries, (int)Math.Sqrt(Period));
		}
	}

	protected override void OnBarUpdate()
	{
		diffSeries[0] = 2.0 * ((NinjaScriptBase)wma1)[0] - ((NinjaScriptBase)wma2)[0];
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)wmaDiffSeries)[0];
	}
}
