using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Triple Exponential Moving Average
/// </summary>
public class TEMA : Indicator
{
	private EMA ema1;

	private EMA ema2;

	private EMA ema3;

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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionTEMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameTEMA;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameTEMA);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			ema1 = EMA(((NinjaScriptBase)this).Inputs[0], Period);
			ema2 = EMA((ISeries<double>)(object)ema1, Period);
			ema3 = EMA((ISeries<double>)(object)ema2, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = 3.0 * ((NinjaScriptBase)ema1)[0] - 3.0 * ((NinjaScriptBase)ema2)[0] + ((NinjaScriptBase)ema3)[0];
	}
}
