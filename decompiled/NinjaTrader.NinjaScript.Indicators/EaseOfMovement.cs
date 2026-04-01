using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Ease of Movement (EMV) indicator emphasizes days in which the stock is moving
///  easily and minimizes the days in which the stock is finding it difficult to move.
/// A buy signal is generated when the EMV crosses above zero, a sell signal when it
///  crosses below zero. When the EMV hovers around zero, then there are small price
/// movements and/or high volume, which is to say, the price is not moving easily.
/// </summary>
public class EaseOfMovement : Indicator
{
	private EMA ema;

	private Series<double> emv;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Smoothing", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Smoothing { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "VolumeDivisor", GroupName = "NinjaScriptParameters", Order = 1)]
	public int VolumeDivisor { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Invalid comparison between Unknown and I4
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Invalid comparison between Unknown and I4
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionEaseOfMovement;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameEaseOfMovement;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			Smoothing = 14;
			VolumeDivisor = 10000;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameEaseOfMovement);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			emv = new Series<double>((NinjaScriptBase)(object)this);
			ema = EMA((ISeries<double>)(object)emv, Smoothing);
		}
		else if ((int)((NinjaScript)this).State == 5 && (int)((NinjaScriptBase)this).Calculate == 2)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), TextPosition.BottomRight);
			NinjaScript.Log(string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), (LogLevel)3);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).CurrentBar != 0)
		{
			double num = (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)this).Volume[0]) : ((NinjaScriptBase)this).Volume[0]);
			double num2 = ((NinjaScriptBase)this).Median[0] - ((NinjaScriptBase)this).Median[1];
			double num3 = num / (double)VolumeDivisor / (((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Low[0]);
			emv[0] = ((MathExtentions.ApproxCompare(num3, 0.0) == 0) ? 0.0 : (num2 / num3));
			((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)ema)[0];
		}
	}
}
