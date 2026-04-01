using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Commodity Channel Index (CCI) measures the variation of a security's price
/// from its statistical mean. High values show that prices are unusually high
/// compared to average prices whereas low values indicate that prices are unusually low.
/// </summary>
public class CCI : Indicator
{
	private SMA sma;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionCCI;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameCCI;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameCCI);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 200.0, Resource.CCILevel2);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 100.0, Resource.CCILevel1);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, -100.0, Resource.CCILevelMinus1);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, -200.0, Resource.CCILevelMinus2);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			sma = SMA(((NinjaScriptBase)this).Typical, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			((NinjaScriptBase)this).Value[0] = 0.0;
			return;
		}
		double num = 0.0;
		double num2 = ((NinjaScriptBase)sma)[0];
		for (int num3 = Math.Min(((NinjaScriptBase)this).CurrentBar, Period - 1); num3 >= 0; num3--)
		{
			num += Math.Abs(((NinjaScriptBase)this).Typical[num3] - num2);
		}
		((NinjaScriptBase)this).Value[0] = (((NinjaScriptBase)this).Typical[0] - num2) / ((MathExtentions.ApproxCompare(num, 0.0) == 0) ? 1.0 : (0.015 * (num / (double)Math.Min(Period, ((NinjaScriptBase)this).CurrentBar + 1))));
	}
}
