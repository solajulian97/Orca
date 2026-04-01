using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Fisher Transform. The Fisher Transform has sharp and distinct turning points
/// that occur in a timely fashion. The resulting peak swings are used to identify price reversals.
/// </summary>
public class FisherTransform : Indicator
{
	private MAX max;

	private MIN min;

	private Series<double> tmpSeries;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionFisherTransform;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameFisherTransform;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 10;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, 2f), (PlotStyle)0, Resource.NinjaScriptIndicatorNameFisherTransform);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			max = MAX(((NinjaScriptBase)this).Input, Period);
			min = MIN(((NinjaScriptBase)this).Input, Period);
			tmpSeries = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = 0.0;
		double num2 = 0.0;
		if (((NinjaScriptBase)this).CurrentBar > 0)
		{
			num = ((NinjaScriptBase)this).Value[1];
			num2 = tmpSeries[1];
		}
		double num3 = ((NinjaScriptBase)min)[0];
		double num4 = ((NinjaScriptBase)max)[0] - num3;
		num4 = ((num4 < ((NinjaScriptBase)this).TickSize / 10.0) ? (((NinjaScriptBase)this).TickSize / 10.0) : num4);
		double num5 = 0.66 * ((((NinjaScriptBase)this).Input[0] - num3) / num4 - 0.5) + 0.67 * num2;
		if (num5 > 0.99)
		{
			num5 = 0.999;
		}
		else if (num5 < -0.99)
		{
			num5 = -0.999;
		}
		tmpSeries[0] = num5;
		((NinjaScriptBase)this).Value[0] = 0.5 * Math.Log((1.0 + num5) / (1.0 - num5)) + 0.5 * num;
	}
}
