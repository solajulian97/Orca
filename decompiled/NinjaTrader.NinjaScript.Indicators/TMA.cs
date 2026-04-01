using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The TMA (Triangular Moving Average) is a weighted moving average. Compared to the WMA which puts more weight on the latest price bar, the TMA puts more weight on the data in the middle of the specified period.
/// </summary>
public class TMA : Indicator
{
	private int p1;

	private int p2;

	private SMA sma;

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
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionTMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameTMA;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 15;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameTMA);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			p1 = 0;
			p2 = 0;
			if ((Period & 1) == 0)
			{
				p1 = Period / 2;
				p2 = p1 + 1;
			}
			else
			{
				p1 = (Period + 1) / 2;
				p2 = p1;
			}
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			sma = SMA((ISeries<double>)(object)SMA(((NinjaScriptBase)this).Inputs[0], p1), p2);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)sma)[0];
	}
}
