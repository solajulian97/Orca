using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class VWAP : Indicator
{
	private double iCumVolume;

	private double iCumTypicalVolume;

	private double curVWAP;

	private double deviation;

	private double v2Sum;

	private double hl3;

	[RefreshProperties(RefreshProperties.All)]
	[Range(0, 5)]
	[Display(Name = "Number of deviations", Order = 1, GroupName = "Standard Deviations")]
	public int NumDeviations { get; set; }

	[Display(Name = "Deviation 1", Order = 2, GroupName = "Standard Deviations 1")]
	public double SD1 { get; set; }

	[Display(Name = "SD1 Fill Opacity", Order = 3, GroupName = "Standard Deviations 1")]
	public int SD1AreaOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "SD1 Fill Color", Order = 4, GroupName = "Standard Deviations 1")]
	public Brush SD1AreaBrush { get; set; }

	[Display(Name = "Deviation 2", Order = 5, GroupName = "Standard Deviations 2")]
	public double SD2 { get; set; }

	[Display(Name = "SD2 Fill Opacity", Order = 6, GroupName = "Standard Deviations 2")]
	public int SD2AreaOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "SD2 Fill Color", Order = 7, GroupName = "Standard Deviations 2")]
	public Brush SD2AreaBrush { get; set; }

	[Display(Name = "Deviation 3", Order = 8, GroupName = "Standard Deviations 3")]
	public double SD3 { get; set; }

	[Display(Name = "SD3 Fill Opacity", Order = 9, GroupName = "Standard Deviations 3")]
	public int SD3AreaOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "SD3 Fill Color", Order = 10, GroupName = "Standard Deviations 3")]
	public Brush SD3AreaBrush { get; set; }

	[Display(Name = "Deviation 4", Order = 11, GroupName = "Standard Deviations 4")]
	public double SD4 { get; set; }

	[Display(Name = "SD4 Fill Opacity", Order = 12, GroupName = "Standard Deviations 4")]
	public int SD4AreaOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "SD4 Fill Color", Order = 13, GroupName = "Standard Deviations 4")]
	public Brush SD4AreaBrush { get; set; }

	[Display(Name = "Deviation 5", Order = 14, GroupName = "Standard Deviations 5")]
	public double SD5 { get; set; }

	[Display(Name = "SD5 Fill Opacity", Order = 15, GroupName = "Standard Deviations 5")]
	public int SD5AreaOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "SD5 Fill Color", Order = 16, GroupName = "Standard Deviations 5")]
	public Brush SD5AreaBrush { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP1U => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP1L => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP2U => ((NinjaScriptBase)this).Values[3];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP2L => ((NinjaScriptBase)this).Values[4];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP3U => ((NinjaScriptBase)this).Values[5];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP3L => ((NinjaScriptBase)this).Values[6];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP4U => ((NinjaScriptBase)this).Values[7];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP4L => ((NinjaScriptBase)this).Values[8];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP5U => ((NinjaScriptBase)this).Values[9];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PlotVWAP5L => ((NinjaScriptBase)this).Values[10];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected O, but got Unknown
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Expected O, but got Unknown
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Expected O, but got Unknown
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Volume Weighted Average Price";
			((NinjaScriptBase)this).Name = "VWAPx";
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			NumDeviations = 4;
			SD1 = 1.28;
			SD2 = 2.01;
			SD3 = 2.51;
			SD4 = 3.1;
			SD5 = 4.0;
			SD1AreaBrush = Brushes.CornflowerBlue;
			SD1AreaOpacity = 6;
			SD2AreaBrush = Brushes.CornflowerBlue;
			SD2AreaOpacity = 2;
			SD3AreaBrush = Brushes.DarkOrange;
			SD3AreaOpacity = 4;
			SD4AreaBrush = Brushes.Brown;
			SD4AreaOpacity = 4;
			SD5AreaBrush = Brushes.Red;
			SD5AreaOpacity = 5;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Black, (DashStyleHelper)4, 1f), (PlotStyle)6, "PlotVWAP");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Tan, (DashStyleHelper)4, 1f), (PlotStyle)6, "PlotVWAP1U");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Tan, (DashStyleHelper)4, 1f), (PlotStyle)6, "PlotVWAP1L");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Orange, "PlotVWAP2U");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Orange, "PlotVWAP2L");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Firebrick, 1f), (PlotStyle)6, "PlotVWAP3U");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Firebrick, 1f), (PlotStyle)6, "PlotVWAP3L");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Red, "PlotVWAP4U");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Red, "PlotVWAP4L");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Black, "PlotVWAP5U");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Black, "PlotVWAP5L");
		}
	}

	protected override void OnBarUpdate()
	{
		hl3 = (((NinjaScriptBase)this).High[0] + ((NinjaScriptBase)this).Low[0] + ((NinjaScriptBase)this).Close[0]) / 3.0;
		if (((NinjaScriptBase)this).Bars.IsFirstBarOfSession)
		{
			iCumVolume = ((NinjaScriptBase)VOL())[0];
			iCumTypicalVolume = ((NinjaScriptBase)VOL())[0] * hl3;
			v2Sum = ((NinjaScriptBase)VOL())[0] * hl3 * hl3;
		}
		else
		{
			iCumVolume += ((NinjaScriptBase)VOL())[0];
			iCumTypicalVolume += ((NinjaScriptBase)VOL())[0] * hl3;
			v2Sum += ((NinjaScriptBase)VOL())[0] * hl3 * hl3;
		}
		curVWAP = iCumTypicalVolume / iCumVolume;
		deviation = Math.Sqrt(Math.Max(v2Sum / iCumVolume - curVWAP * curVWAP, 0.0));
		PlotVWAP[0] = curVWAP;
		switch (NumDeviations)
		{
		case 1:
			PlotDevOne();
			break;
		case 2:
			PlotDevTwo();
			break;
		case 3:
			PlotDevThree();
			break;
		case 4:
			PlotDevFour();
			break;
		case 5:
			PlotDevFive();
			break;
		default:
			PlotVWAP[0] = curVWAP;
			break;
		}
	}

	private void PlotDevOne()
	{
		PlotVWAP1U[0] = curVWAP + SD1 * deviation;
		PlotVWAP1L[0] = curVWAP - SD1 * deviation;
		Draw.Region((NinjaScriptBase)(object)this, "dev1", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)PlotVWAP1U, (ISeries<double>)(object)PlotVWAP1L, null, SD1AreaBrush, SD1AreaOpacity);
	}

	private void PlotDevTwo()
	{
		PlotDevOne();
		PlotVWAP2U[0] = curVWAP + SD2 * deviation;
		PlotVWAP2L[0] = curVWAP - SD2 * deviation;
		Draw.Region((NinjaScriptBase)(object)this, "dev2", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)PlotVWAP1U, (ISeries<double>)(object)PlotVWAP2U, null, SD2AreaBrush, SD2AreaOpacity);
		Draw.Region((NinjaScriptBase)(object)this, "dev3", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)PlotVWAP1L, (ISeries<double>)(object)PlotVWAP2L, null, SD2AreaBrush, SD2AreaOpacity);
	}

	private void PlotDevThree()
	{
		PlotDevTwo();
		PlotVWAP3U[0] = curVWAP + SD3 * deviation;
		PlotVWAP3L[0] = curVWAP - SD3 * deviation;
		Draw.Region((NinjaScriptBase)(object)this, "dev4", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)PlotVWAP2U, (ISeries<double>)(object)PlotVWAP3U, null, SD3AreaBrush, SD3AreaOpacity);
		Draw.Region((NinjaScriptBase)(object)this, "dev5", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)PlotVWAP2L, (ISeries<double>)(object)PlotVWAP3L, null, SD3AreaBrush, SD3AreaOpacity);
	}

	private void PlotDevFour()
	{
		PlotDevThree();
		PlotVWAP4U[0] = curVWAP + SD4 * deviation;
		PlotVWAP4L[0] = curVWAP - SD4 * deviation;
		Draw.Region((NinjaScriptBase)(object)this, "dev6", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)PlotVWAP3U, (ISeries<double>)(object)PlotVWAP4U, null, SD4AreaBrush, SD4AreaOpacity);
		Draw.Region((NinjaScriptBase)(object)this, "dev7", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)PlotVWAP3L, (ISeries<double>)(object)PlotVWAP4L, null, SD4AreaBrush, SD4AreaOpacity);
	}

	private void PlotDevFive()
	{
		PlotDevFour();
		PlotVWAP5U[0] = curVWAP + SD5 * deviation;
		PlotVWAP5L[0] = curVWAP - SD5 * deviation;
		Draw.Region((NinjaScriptBase)(object)this, "dev8", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)PlotVWAP4U, (ISeries<double>)(object)PlotVWAP5U, null, SD5AreaBrush, SD5AreaOpacity);
		Draw.Region((NinjaScriptBase)(object)this, "dev9", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)PlotVWAP4L, (ISeries<double>)(object)PlotVWAP5L, null, SD5AreaBrush, SD5AreaOpacity);
	}
}
