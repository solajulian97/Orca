using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Accumulation/Distribution (AD) study attempts to quantify the amount of volume flowing into or
/// out of an instrument by identifying the position of the close of the period in relation to that period's high/low range.
/// </summary>
public class ADL : Indicator
{
	[Browsable(false)]
	[XmlIgnore]
	public Series<double> AD => ((NinjaScriptBase)this).Values[0];

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
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionADL;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameADL;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.ADLAD);
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
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		double num3 = ((NinjaScriptBase)this).Close[0];
		double num4 = (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)this).Volume[0]) : ((NinjaScriptBase)this).Volume[0]);
		AD[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? 0.0 : AD[1]) + ((MathExtentions.ApproxCompare(num, num2) != 0) ? ((num3 - num2 - (num - num3)) / (num - num2) * num4) : 0.0);
	}
}
