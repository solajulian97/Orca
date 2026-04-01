using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Double stochastics
/// </summary>
public class DoubleStochastics : Indicator
{
	private EMA emaP1;

	private EMA emaP3;

	private MIN minLow;

	private MIN minP2;

	private MAX maxHigh;

	private MAX maxP2;

	private Series<double> p1;

	private Series<double> p2;

	private Series<double> p3;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> K => ((NinjaScriptBase)this).Values[0];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Invalid comparison between Unknown and I4
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionDoubleStochastics;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameDoubleStochastics;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 10;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.StochasticsK);
			((NinjaScriptBase)this).AddLine(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)1, 1f), 90.0, Resource.NinjaScriptIndicatorUpper);
			((NinjaScriptBase)this).AddLine(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)1, 1f), 10.0, Resource.NinjaScriptIndicatorLower);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			p1 = new Series<double>((NinjaScriptBase)(object)this);
			p2 = new Series<double>((NinjaScriptBase)(object)this);
			p3 = new Series<double>((NinjaScriptBase)(object)this);
			emaP1 = EMA((ISeries<double>)(object)p1, 3);
			emaP3 = EMA((ISeries<double>)(object)p3, 3);
			maxHigh = MAX(((NinjaScriptBase)this).High, Period);
			maxP2 = MAX((ISeries<double>)(object)p2, Period);
			minLow = MIN(((NinjaScriptBase)this).Low, Period);
			minP2 = MIN((ISeries<double>)(object)p2, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)maxHigh)[0];
		double num2 = ((NinjaScriptBase)minLow)[0];
		double num3 = num - num2;
		num3 = ((MathExtentions.ApproxCompare(num3, 0.0) == 0) ? 0.0 : num3);
		if (num3 == 0.0)
		{
			p1[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? 50.0 : p1[1]);
		}
		else
		{
			p1[0] = Math.Min(100.0, Math.Max(0.0, 100.0 * (((NinjaScriptBase)this).Close[0] - num2) / num3));
		}
		p2[0] = ((NinjaScriptBase)emaP1)[0];
		double num4 = ((NinjaScriptBase)minP2)[0];
		double num5 = ((NinjaScriptBase)maxP2)[0] - num4;
		num5 = ((MathExtentions.ApproxCompare(num5, 0.0) == 0) ? 0.0 : num5);
		if (num5 == 0.0)
		{
			p3[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? 50.0 : p3[1]);
		}
		else
		{
			p3[0] = Math.Min(100.0, Math.Max(0.0, 100.0 * (p2[0] - num4) / num5));
		}
		K[0] = ((NinjaScriptBase)emaP3)[0];
	}
}
