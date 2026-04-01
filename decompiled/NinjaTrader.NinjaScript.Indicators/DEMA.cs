using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Double Exponential Moving Average
/// </summary>
public class DEMA : Indicator
{
	private EMA ema;

	private EMA emaEma;

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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionDEMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameDEMA;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameDEMA);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			ema = EMA(((NinjaScriptBase)this).Inputs[0], Period);
			emaEma = EMA((ISeries<double>)(object)ema, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = 2.0 * ((NinjaScriptBase)ema)[0] - ((NinjaScriptBase)emaEma)[0];
	}
}
