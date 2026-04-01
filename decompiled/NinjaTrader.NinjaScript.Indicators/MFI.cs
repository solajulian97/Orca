using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The MFI (Money Flow Index) is a momentum indicator that measures the strength of money flowing in and out of a security.
/// </summary>
public class MFI : Indicator
{
	private Series<double> negative;

	private Series<double> positive;

	private SUM sumNegative;

	private SUM sumPositive;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Invalid comparison between Unknown and I4
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Invalid comparison between Unknown and I4
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionMFI;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameMFI;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameMFI);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.SlateBlue, 20.0, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.Goldenrod, 80.0, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			negative = new Series<double>((NinjaScriptBase)(object)this);
			positive = new Series<double>((NinjaScriptBase)(object)this);
			sumNegative = SUM((ISeries<double>)(object)negative, Period);
			sumPositive = SUM((ISeries<double>)(object)positive, Period);
		}
		else if ((int)((NinjaScript)this).State == 5 && (int)((NinjaScriptBase)this).Calculate == 2)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), TextPosition.BottomRight);
			NinjaScript.Log(string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), (LogLevel)3);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			((NinjaScriptBase)this).Value[0] = 50.0;
			return;
		}
		double num = ((NinjaScriptBase)this).Typical[0];
		double num2 = ((NinjaScriptBase)this).Typical[1];
		double num3 = (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)this).Volume[0]) : ((NinjaScriptBase)this).Volume[0]);
		negative[0] = ((num < num2) ? (num * num3) : 0.0);
		positive[0] = ((num > num2) ? (num * num3) : 0.0);
		double num4 = ((NinjaScriptBase)sumNegative)[0];
		((NinjaScriptBase)this).Value[0] = ((num4 == 0.0) ? 50.0 : (100.0 - 100.0 / (1.0 + ((NinjaScriptBase)sumPositive)[0] / num4)));
	}
}
