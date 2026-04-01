using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Dynamic Momentum Index is a variable term RSI. The RSI term varies
///  from 3 to 30. The variable time period makes the RSI more responsive to
/// short-term moves. The more volatile the price is, the shorter the time period is.
///  It is interpreted in the same way as the RSI, but provides signals earlier.
/// </summary>
public class DMIndex : Indicator
{
	private SMA sma;

	private StdDev stdDev;

	[Browsable(false)]
	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Smooth { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionDMIndex;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameDMIndex;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = false;
			Smooth = 3;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameDMIndex);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			stdDev = StdDev(5);
			sma = SMA((ISeries<double>)(object)stdDev, 10);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)this).Input[0];
			return;
		}
		int period = (((int)(14.0 / (((NinjaScriptBase)stdDev)[0] / ((NinjaScriptBase)sma)[0])) < 1) ? 1 : ((int)(14.0 / (((NinjaScriptBase)stdDev)[0] / ((NinjaScriptBase)sma)[0]))));
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)RSI(period, Smooth))[0];
	}
}
