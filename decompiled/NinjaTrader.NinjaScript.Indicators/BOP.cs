using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The balance of power indicator measures the strength of the bulls vs. bears by
///  assessing the ability of each to push price to an extreme level.
/// </summary>
public class BOP : Indicator
{
	private Series<double> bop;

	private SMA sma;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Smooth { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionBOP;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameBOP;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Smooth = 14;
			((NinjaScriptBase)this).IsOverlay = false;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, 2f), (PlotStyle)0, Resource.NinjaScriptIndicatorNameBOP);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			bop = new Series<double>((NinjaScriptBase)(object)this);
			sma = SMA((ISeries<double>)(object)bop, Smooth);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		if (MathExtentions.ApproxCompare(num - num2, 0.0) == 0)
		{
			bop[0] = 0.0;
		}
		else
		{
			bop[0] = (((NinjaScriptBase)this).Close[0] - ((NinjaScriptBase)this).Open[0]) / (num - num2);
		}
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)sma)[0];
	}
}
