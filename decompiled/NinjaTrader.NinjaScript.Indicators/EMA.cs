using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Exponential Moving Average. The Exponential Moving Average is an indicator that
/// shows the average value of a security's price over a period of time. When calculating
/// a moving average. The EMA applies more weight to recent prices than the SMA.
/// </summary>
public class EMA : Indicator
{
	private double constant1;

	private double constant2;

	[Range(1, int.MaxValue)]
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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionEMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameEMA;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameEMA);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			constant1 = 2.0 / (double)(1 + Period);
			constant2 = 1.0 - 2.0 / (double)(1 + Period);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? ((NinjaScriptBase)this).Input[0] : (((NinjaScriptBase)this).Input[0] * constant1 + constant2 * ((NinjaScriptBase)this).Value[1]));
	}
}
