using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class BuySellVolume : Indicator
{
	private int activeBar;

	private double buys;

	private double sells;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Sells => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Buys => ((NinjaScriptBase)this).Values[0];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Invalid comparison between Unknown and I4
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionBuySellVolume;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameBuySellVolume;
			((NinjaScriptBase)this).BarsRequiredToPlot = 1;
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).IsOverlay = false;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DarkCyan, 2f), (PlotStyle)0, Resource.BuySellVolumeBuys);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Crimson, 2f), (PlotStyle)0, Resource.BuySellVolumeSells);
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
				Sells[1] = sells;
				Buys[1] = buys + sells;
				buys = 0.0;
				sells = 0.0;
				activeBar = ((NinjaScriptBase)this).CurrentBar;
			}
			Sells[0] = sells;
			Buys[0] = buys + sells;
		}
	}
}
