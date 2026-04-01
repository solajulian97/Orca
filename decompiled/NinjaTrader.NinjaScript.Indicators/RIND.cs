using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// RIND (Range Indicator) compares the intraday range (high - low) to the
/// inter-day (close - previous close) range. When the intraday range is greater
/// than the inter-day range, the Range Indicator will be a high value. This
/// signals an end to the current trend. When the Range Indicator is at a low
/// level, a new trend is about to start.
/// </summary>
public class RIND : Indicator
{
	private EMA ema;

	private MIN min;

	private MAX max;

	private Series<double> stochRange;

	private Series<double> val1;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "PeriodQ", GroupName = "NinjaScriptParameters", Order = 0)]
	public int PeriodQ { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Smooth { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionRIND;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameRIND;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			PeriodQ = 3;
			Smooth = 10;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameRIND);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			stochRange = new Series<double>((NinjaScriptBase)(object)this);
			val1 = new Series<double>((NinjaScriptBase)(object)this);
			ema = EMA((ISeries<double>)(object)stochRange, Smooth);
			min = MIN((ISeries<double>)(object)val1, PeriodQ);
			max = MAX((ISeries<double>)(object)val1, PeriodQ);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			stochRange[0] = 50.0;
			return;
		}
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		double num3 = ((NinjaScriptBase)this).Close[0];
		double num4 = ((NinjaScriptBase)this).Close[1];
		double num5 = Math.Max(num, num4) - Math.Min(num2, num4);
		if (num3 > num4)
		{
			val1[0] = num5 / (num3 - num4);
		}
		else
		{
			val1[0] = num5;
		}
		double num6 = ((NinjaScriptBase)min)[0];
		double num7 = ((NinjaScriptBase)max)[0];
		double num8 = val1[0];
		if (num7 - num6 > 0.0)
		{
			stochRange[0] = 100.0 * ((num8 - num6) / (num7 - num6));
		}
		else
		{
			stochRange[0] = 100.0 * (num8 - num6);
		}
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)ema)[0];
	}
}
