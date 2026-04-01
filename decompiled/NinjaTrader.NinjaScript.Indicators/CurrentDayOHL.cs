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
/// Plots the open, high, and low values from the session starting on the current day.
/// </summary>
public class CurrentDayOHL : Indicator
{
	private DateTime currentDate = Globals.MinDate;

	private double currentOpen = double.MinValue;

	private double currentHigh = double.MinValue;

	private double currentLow = double.MaxValue;

	private DateTime lastDate = Globals.MinDate;

	private SessionIterator sessionIterator;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> CurrentOpen => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> CurrentHigh => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> CurrentLow => ((NinjaScriptBase)this).Values[2];

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
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Invalid comparison between Unknown and I4
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected O, but got Unknown
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Invalid comparison between Unknown and I4
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Invalid comparison between Unknown and I4
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionCurrentDayOHL;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameCurrentDayOHL;
			((NinjaScriptBase)this).IsAutoScale = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			ShowLow = true;
			ShowHigh = true;
			ShowOpen = true;
			((NinjaScriptBase)this).BarsRequiredToPlot = 0;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Goldenrod, (DashStyleHelper)1, 2f), (PlotStyle)7, Resource.CurrentDayOHLOpen);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.SeaGreen, (DashStyleHelper)1, 2f), (PlotStyle)7, Resource.CurrentDayOHLHigh);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Red, (DashStyleHelper)1, 2f), (PlotStyle)7, Resource.CurrentDayOHLLow);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			currentDate = Globals.MinDate;
			currentOpen = double.MinValue;
			currentHigh = double.MinValue;
			currentLow = double.MaxValue;
			lastDate = Globals.MinDate;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			sessionIterator = new SessionIterator(((NinjaScriptBase)this).Bars);
		}
		else if ((int)((NinjaScript)this).State == 5 && !((NinjaScriptBase)this).Bars.BarsType.IsIntraday)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.CurrentDayOHLError, TextPosition.BottomRight);
			NinjaScript.Log(Resource.CurrentDayOHLError, (LogLevel)3);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).Bars.BarsType.IsIntraday)
		{
			lastDate = currentDate;
			currentDate = sessionIterator.GetTradingDay(((NinjaScriptBase)this).Time[0]);
			if (lastDate != currentDate || currentOpen <= double.MinValue)
			{
				currentOpen = ((NinjaScriptBase)this).Open[0];
				currentHigh = ((NinjaScriptBase)this).High[0];
				currentLow = ((NinjaScriptBase)this).Low[0];
			}
			currentHigh = Math.Max(currentHigh, ((NinjaScriptBase)this).High[0]);
			currentLow = Math.Min(currentLow, ((NinjaScriptBase)this).Low[0]);
			if (ShowOpen)
			{
				CurrentOpen[0] = currentOpen;
			}
			if (ShowHigh)
			{
				CurrentHigh[0] = currentHigh;
			}
			if (ShowLow)
			{
				CurrentLow[0] = currentLow;
			}
		}
	}

	public override string FormatPriceMarker(double price)
	{
		return ((NinjaScriptBase)this).Instrument.MasterInstrument.FormatPrice(price, true);
	}
}
