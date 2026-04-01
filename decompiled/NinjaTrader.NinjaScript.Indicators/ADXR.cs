using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Average Directional Movement Rating quantifies momentum change in the ADX.
/// It is calculated by adding two values of ADX (the current value and a value n periods back),
/// then dividing by two. This additional smoothing makes the ADXR slightly less responsive than ADX.
/// The interpretation is the same as the ADX; the higher the value, the stronger the trend.
/// </summary>
public class ADXR : Indicator
{
	private ADX adx;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Interval", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Interval { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionADXR;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameADXR;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			Interval = 10;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameADXR);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.SlateBlue, 25.0, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.Goldenrod, 75.0, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			adx = ADX(Period);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar < Interval) ? ((((NinjaScriptBase)adx)[0] + ((NinjaScriptBase)adx)[((NinjaScriptBase)this).CurrentBar]) / 2.0) : ((((NinjaScriptBase)adx)[0] + ((NinjaScriptBase)adx)[Interval]) / 2.0));
	}
}
