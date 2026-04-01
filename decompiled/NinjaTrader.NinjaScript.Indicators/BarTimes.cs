using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using CustomBsEnumNamespace;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.Indicators;

public class BarTimes : Indicator
{
	private DateTime temp;

	public override string DisplayName => ((NinjaScriptBase)this).Name + " in: " + TimeUnits;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> BarTime => ((NinjaScriptBase)this).Values[0];

	[NinjaScriptProperty]
	[Display(GroupName = "Time Selections", Description = "Choose how to display time")]
	public TimeSelector TimeUnits { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Provides bar times in milliseconds or seconds or minutes or hours,  displayed as bar, intended for non time based bar types.";
			((NinjaScriptBase)this).Name = "BarTimes";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = false;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.OrangeRed, 2f), (PlotStyle)0, "BarTime");
			((NinjaScriptBase)this).AddLine(new Stroke((Brush)Brushes.Gray, 1f), 0.0, "zeroLine");
			TimeUnits = TimeSelector.Seconds;
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar < 1)
		{
			return;
		}
		if (((NinjaScriptBase)this).Bars.IsFirstBarOfSession)
		{
			if (((NinjaScriptBase)this).IsFirstTickOfBar)
			{
				temp = ((NinjaScriptBase)this).Time[0];
				return;
			}
			switch (TimeUnits)
			{
			case TimeSelector.Milliseconds:
				BarTime[0] = (((NinjaScriptBase)this).Time[0] - temp).TotalMilliseconds;
				break;
			case TimeSelector.Seconds:
				BarTime[0] = (((NinjaScriptBase)this).Time[0] - temp).TotalSeconds;
				break;
			case TimeSelector.Minutes:
				BarTime[0] = (((NinjaScriptBase)this).Time[0] - temp).TotalMinutes;
				break;
			case TimeSelector.Hours:
				BarTime[0] = (((NinjaScriptBase)this).Time[0] - temp).TotalHours;
				break;
			}
		}
		else
		{
			switch (TimeUnits)
			{
			case TimeSelector.Milliseconds:
				BarTime[0] = (((NinjaScriptBase)this).Time[0] - ((NinjaScriptBase)this).Time[1]).TotalMilliseconds;
				break;
			case TimeSelector.Seconds:
				BarTime[0] = (((NinjaScriptBase)this).Time[0] - ((NinjaScriptBase)this).Time[1]).TotalSeconds;
				break;
			case TimeSelector.Minutes:
				BarTime[0] = (((NinjaScriptBase)this).Time[0] - ((NinjaScriptBase)this).Time[1]).TotalMinutes;
				break;
			case TimeSelector.Hours:
				BarTime[0] = (((NinjaScriptBase)this).Time[0] - ((NinjaScriptBase)this).Time[1]).TotalHours;
				break;
			}
		}
	}
}
