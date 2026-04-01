using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Chaikin Oscillator.
/// </summary>
public class ChaikinOscillator : Indicator
{
	private Series<double> cummulative;

	private EMA emaFast;

	private EMA emaSlow;

	private Series<double> moneyFlow;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Fast { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Slow { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Invalid comparison between Unknown and I4
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionChaikinOscillator;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameChaikinOscillator;
			Fast = 3;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Slow = 10;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameChaikinOscillator);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			cummulative = new Series<double>((NinjaScriptBase)(object)this);
			moneyFlow = new Series<double>((NinjaScriptBase)(object)this);
			emaFast = EMA((ISeries<double>)(object)cummulative, Fast);
			emaSlow = EMA((ISeries<double>)(object)cummulative, Slow);
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
		double num = ((NinjaScriptBase)this).Close[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		double num3 = ((NinjaScriptBase)this).High[0];
		double num4 = (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)this).Volume[0]) : ((NinjaScriptBase)this).Volume[0]);
		moneyFlow[0] = num4 * (num - num2 - (num3 - num)) / ((MathExtentions.ApproxCompare(num3 - num2, 0.0) == 0) ? 1.0 : (num3 - num2));
		cummulative[0] = moneyFlow[0] + ((((NinjaScriptBase)this).CurrentBar == 0) ? 0.0 : cummulative[1]);
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)emaFast)[0] - ((NinjaScriptBase)emaSlow)[0];
	}
}
