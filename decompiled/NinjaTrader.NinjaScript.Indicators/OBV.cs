using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// OBV (On Balance Volume) is a running total of volume. It shows if volume is flowing into
/// or out of a security. When the security closes higher than the previous close, all
/// of the day's volume is considered up-volume. When the security closes lower than the
/// previous close, all of the day's volume is considered down-volume.
/// </summary>
public class OBV : Indicator
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Invalid comparison between Unknown and I4
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionOBV;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameOBV;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameOBV);
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
			((NinjaScriptBase)this).Value[0] = 0.0;
			return;
		}
		double num = ((NinjaScriptBase)this).Close[0];
		double num2 = ((NinjaScriptBase)this).Close[1];
		double num3 = (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)this).Volume[0]) : ((NinjaScriptBase)this).Volume[0]);
		((NinjaScriptBase)this).Value[0] = ((num > num2) ? (((NinjaScriptBase)this).Value[1] + num3) : ((num < num2) ? (((NinjaScriptBase)this).Value[1] - num3) : ((NinjaScriptBase)this).Value[1]));
	}
}
