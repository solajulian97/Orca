using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Calculates the range of a bar.
/// </summary>
public class Range : Indicator
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionRange;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameRange;
			((NinjaScriptBase)this).BarsRequiredToPlot = 0;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Goldenrod, 2f), (PlotStyle)0, Resource.RangeValue);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Low[0];
	}
}
