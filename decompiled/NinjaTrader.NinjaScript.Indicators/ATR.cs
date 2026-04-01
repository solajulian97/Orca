using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Average True Range (ATR) is a measure of volatility. It was introduced by Welles Wilder
/// in his book 'New Concepts in Technical Trading Systems' and has since been used as a component
/// of many indicators and trading systems.
/// </summary>
public class ATR : Indicator
{
	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionATR;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameATR;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameATR);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			((NinjaScriptBase)this).Value[0] = num - num2;
			return;
		}
		double num3 = ((NinjaScriptBase)this).Close[1];
		double num4 = Math.Max(Math.Abs(num2 - num3), Math.Max(num - num2, Math.Abs(num - num3)));
		((NinjaScriptBase)this).Value[0] = ((double)(Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period) - 1) * ((NinjaScriptBase)this).Value[1] + num4) / (double)Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period);
	}
}
