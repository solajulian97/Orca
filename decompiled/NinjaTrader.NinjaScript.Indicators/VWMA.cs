using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The VWMA (Volume-Weighted Moving Average) returns the volume-weighted moving average
/// for the specified price series and period. VWMA is similar to a Simple Moving Average
/// (SMA), but each bar of data is weighted by the bar's Volume. VWMA places more significance
/// on the days with the largest volume and the least for the days with lowest volume for the period specified.
/// </summary>
public class VWMA : Indicator
{
	private double priorVolPriceSum;

	private double volPriceSum;

	private Series<double> volSum;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Invalid comparison between Unknown and I4
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionVWMA;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameVWMA;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameVWMA);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			volSum = new Series<double>((NinjaScriptBase)(object)this);
		}
		else if ((int)((NinjaScript)this).State == 5 && (int)((NinjaScriptBase)this).Calculate == 2)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), TextPosition.BottomRight);
			NinjaScript.Log(string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), (LogLevel)3);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Invalid comparison between Unknown and I4
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
		{
			int num = Math.Min(((NinjaScriptBase)this).CurrentBar, Period);
			double num2 = 0.0;
			double num3 = 0.0;
			for (int i = 0; i < num; i++)
			{
				double num4 = (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)this).Volume[i]) : ((NinjaScriptBase)this).Volume[i]);
				num2 += ((NinjaScriptBase)this).Input[i] * num4;
				num3 += num4;
			}
			((NinjaScriptBase)this).Value[0] = ((num3 <= double.Epsilon) ? num2 : (num2 / num3));
			return;
		}
		if (((NinjaScriptBase)this).IsFirstTickOfBar)
		{
			priorVolPriceSum = volPriceSum;
		}
		double num5 = ((NinjaScriptBase)this).Volume[0];
		double num6 = ((NinjaScriptBase)this).Volume[Math.Min(Period, ((NinjaScriptBase)this).CurrentBar)];
		if ((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7)
		{
			num5 = Globals.ToCryptocurrencyVolume((long)num5);
			num6 = Globals.ToCryptocurrencyVolume((long)num6);
		}
		volPriceSum = priorVolPriceSum + ((NinjaScriptBase)this).Input[0] * num5 - ((((NinjaScriptBase)this).CurrentBar >= Period) ? (((NinjaScriptBase)this).Input[Period] * num6) : 0.0);
		volSum[0] = num5 + ((((NinjaScriptBase)this).CurrentBar > 0) ? volSum[1] : 0.0) - ((((NinjaScriptBase)this).CurrentBar >= Period) ? num6 : 0.0);
		((NinjaScriptBase)this).Value[0] = ((MathExtentions.ApproxCompare(volSum[0], 0.0) == 0) ? volPriceSum : (volPriceSum / volSum[0]));
	}
}
