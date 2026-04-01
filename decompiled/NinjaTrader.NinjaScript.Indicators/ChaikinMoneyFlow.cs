using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Chaikin Money Flow.
/// </summary>
public class ChaikinMoneyFlow : Indicator
{
	private Series<double> moneyFlow;

	private SUM sumMoneyFlow;

	private SUM sumVolume;

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
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Invalid comparison between Unknown and I4
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionChaikinMoneyFlow;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameChaikinMoneyFlow;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			Period = 21;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameChaikinMoneyFlow);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			moneyFlow = new Series<double>((NinjaScriptBase)(object)this);
			sumMoneyFlow = SUM((ISeries<double>)(object)moneyFlow, Period);
			sumVolume = SUM((ISeries<double>)(object)((NinjaScriptBase)this).Volume, Period);
		}
		else if ((int)((NinjaScript)this).State == 5 && (int)((NinjaScriptBase)this).Calculate == 2)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), TextPosition.BottomRight);
			NinjaScript.Log(string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), (LogLevel)3);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		double num = ((NinjaScriptBase)this).Close[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		double num3 = ((NinjaScriptBase)this).High[0];
		double num4 = (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)this).Volume[0]) : ((NinjaScriptBase)this).Volume[0]);
		double num5 = (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)sumVolume)[0]) : ((NinjaScriptBase)sumVolume)[0]);
		moneyFlow[0] = num4 * (num - num2 - (num3 - num)) / ((MathExtentions.ApproxCompare(num3 - num2, 0.0) == 0) ? 1.0 : (num3 - num2));
		double num6 = 100.0 * ((NinjaScriptBase)sumMoneyFlow)[0] / num5;
		((NinjaScriptBase)this).Value[0] = (double.IsNaN(num6) ? 0.0 : num6);
	}
}
