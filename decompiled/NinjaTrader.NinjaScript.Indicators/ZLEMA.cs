using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The ZLEMA (Zero-Lag Exponential Moving Average) is an EMA variant that attempts to adjust for lag.
/// </summary>
public class ZLEMA : Indicator
{
	private double k;

	private int lag;

	private double oneMinusK;

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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionZLEMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameZLEMA;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameZLEMA);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			k = 2.0 / (double)(Period + 1);
			oneMinusK = 1.0 - k;
			lag = (int)Math.Ceiling((double)(Period - 1) / 2.0);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar < lag) ? ((NinjaScriptBase)this).Input[0] : (k * (2.0 * ((NinjaScriptBase)this).Input[0] - ((NinjaScriptBase)this).Input[lag]) + oneMinusK * ((NinjaScriptBase)this).Value[1]));
	}
}
