using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Plots the open, high, low and close values from the session starting on the prior day.
/// </summary>
public class PriorDayOHLC : Indicator
{
	private DateTime currentDate = Globals.MinDate;

	private double currentClose;

	private double currentHigh;

	private double currentLow;

	private double currentOpen;

	private double priorDayClose;

	private double priorDayHigh;

	private double priorDayLow;

	private double priorDayOpen;

	private SessionIterator sessionIterator;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PriorOpen => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PriorHigh => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PriorLow => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PriorClose => ((NinjaScriptBase)this).Values[3];

	[Display(ResourceType = typeof(Resource), Name = "ShowClose", GroupName = "NinjaScriptParameters", Order = 0)]
	public bool ShowClose { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "ShowHigh", GroupName = "NinjaScriptParameters", Order = 1)]
	public bool ShowHigh { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "ShowLow", GroupName = "NinjaScriptParameters", Order = 2)]
	public bool ShowLow { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "ShowOpen", GroupName = "NinjaScriptParameters", Order = 3)]
	public bool ShowOpen { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Invalid comparison between Unknown and I4
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Invalid comparison between Unknown and I4
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Invalid comparison between Unknown and I4
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionPriorDayOHLC;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNamePriorDayOHLC;
			((NinjaScriptBase)this).IsAutoScale = false;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			ShowClose = true;
			ShowLow = true;
			ShowHigh = true;
			ShowOpen = true;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.SteelBlue, (DashStyleHelper)1, 2f), (PlotStyle)4, Resource.PriorDayOHLCOpen);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DarkCyan, 2f), (PlotStyle)4, Resource.PriorDayOHLCHigh);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Crimson, 2f), (PlotStyle)4, Resource.PriorDayOHLCLow);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.SlateBlue, (DashStyleHelper)1, 2f), (PlotStyle)4, Resource.PriorDayOHLCClose);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			currentDate = Globals.MinDate;
			currentClose = 0.0;
			currentHigh = 0.0;
			currentLow = 0.0;
			currentOpen = 0.0;
			priorDayClose = 0.0;
			priorDayHigh = 0.0;
			priorDayLow = 0.0;
			priorDayOpen = 0.0;
			sessionIterator = null;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			sessionIterator = new SessionIterator(((NinjaScriptBase)this).Bars);
		}
		else if ((int)((NinjaScript)this).State == 5 && !((NinjaScriptBase)this).Bars.BarsType.IsIntraday)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.PriorDayOHLCIntradayError, TextPosition.BottomRight);
			NinjaScript.Log(Resource.PriorDayOHLCIntradayError, (LogLevel)3);
		}
	}

	protected override void OnBarUpdate()
	{
		if (!((NinjaScriptBase)this).Bars.BarsType.IsIntraday)
		{
			return;
		}
		if (currentDate != sessionIterator.GetTradingDay(((NinjaScriptBase)this).Time[0]) || currentOpen == 0.0)
		{
			priorDayOpen = currentOpen;
			priorDayHigh = currentHigh;
			priorDayLow = currentLow;
			priorDayClose = currentClose;
			if (ShowOpen)
			{
				PriorOpen[0] = priorDayOpen;
			}
			if (ShowHigh)
			{
				PriorHigh[0] = priorDayHigh;
			}
			if (ShowLow)
			{
				PriorLow[0] = priorDayLow;
			}
			if (ShowClose)
			{
				PriorClose[0] = priorDayClose;
			}
			currentOpen = ((NinjaScriptBase)this).Open[0];
			currentHigh = ((NinjaScriptBase)this).High[0];
			currentLow = ((NinjaScriptBase)this).Low[0];
			currentClose = ((NinjaScriptBase)this).Close[0];
			currentDate = sessionIterator.GetTradingDay(((NinjaScriptBase)this).Time[0]);
		}
		else
		{
			currentHigh = Math.Max(currentHigh, ((NinjaScriptBase)this).High[0]);
			currentLow = Math.Min(currentLow, ((NinjaScriptBase)this).Low[0]);
			currentClose = ((NinjaScriptBase)this).Close[0];
			if (ShowOpen)
			{
				PriorOpen[0] = priorDayOpen;
			}
			if (ShowHigh)
			{
				PriorHigh[0] = priorDayHigh;
			}
			if (ShowLow)
			{
				PriorLow[0] = priorDayLow;
			}
			if (ShowClose)
			{
				PriorClose[0] = priorDayClose;
			}
		}
	}

	public override string FormatPriceMarker(double price)
	{
		return ((NinjaScriptBase)this).Instrument.MasterInstrument.FormatPrice(price, true);
	}
}
