using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Indicates the current buying or selling pressure as a perecentage.
/// This is a tick by tick indicator. If 'Calculate on bar close' is true, the indicator values will always be 100.
/// </summary>
public class BuySellPressure : Indicator
{
	private double buys;

	private double sells;

	private int activeBar = -1;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> BuyPressure => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> SellPressure => ((NinjaScriptBase)this).Values[1];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Invalid comparison between Unknown and I4
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionBuySellPressure;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameBuySellPressure;
			((NinjaScriptBase)this).BarsRequiredToPlot = 1;
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).IsOverlay = false;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.BuySellPressureBuyPressure);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.BuySellPressureSellPressure);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DimGray, 75.0, Resource.NinjaScriptIndicatorUpper);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DimGray, 25.0, Resource.NinjaScriptIndicatorLower);
		}
		else if ((int)((NinjaScript)this).State == 5 && (int)((NinjaScriptBase)this).Calculate != 1)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", string.Format(Resource.NinjaScriptOnBarCloseError, ((NinjaScriptBase)this).Name), TextPosition.BottomRight);
			NinjaScript.Log(string.Format(Resource.NinjaScriptOnBarCloseError, ((NinjaScriptBase)this).Name), (LogLevel)3);
		}
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		if ((int)e.MarketDataType == 2)
		{
			if (e.Price >= e.Ask)
			{
				buys += (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume(e.Volume) : ((double)e.Volume));
			}
			else if (e.Price <= e.Bid)
			{
				sells += (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume(e.Volume) : ((double)e.Volume));
			}
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar >= activeBar && ((NinjaScriptBase)this).CurrentBar > ((NinjaScriptBase)this).BarsRequiredToPlot)
		{
			if (((NinjaScriptBase)this).CurrentBar != activeBar)
			{
				BuyPressure[1] = buys / (buys + sells) * 100.0;
				SellPressure[1] = sells / (buys + sells) * 100.0;
				buys = 1.0;
				sells = 1.0;
				activeBar = ((NinjaScriptBase)this).CurrentBar;
			}
			BuyPressure[0] = buys / (buys + sells) * 100.0;
			SellPressure[0] = sells / (buys + sells) * 100.0;
		}
	}
}
