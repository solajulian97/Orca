using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Chaikin Volatility
/// </summary>
public class ChaikinVolatility : Indicator
{
	private EMA ema;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "MAPeriod", GroupName = "NinjaScriptParameters", Order = 0)]
	public int MAPeriod { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "ROCPeriod", GroupName = "NinjaScriptParameters", Order = 1)]
	public int ROCPeriod { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionChaikinVolatility;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameChaikinVolatility;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			MAPeriod = 10;
			ROCPeriod = 10;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.NinjaScriptIndicatorNameChaikinVolatility);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			ema = EMA((ISeries<double>)(object)Range(), MAPeriod);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)ema)[Math.Min(((NinjaScriptBase)this).CurrentBar, ROCPeriod)];
		((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? ((NinjaScriptBase)ema)[0] : ((((NinjaScriptBase)ema)[0] - num) / num * 100.0));
	}
}
