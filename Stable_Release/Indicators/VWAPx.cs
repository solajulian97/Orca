#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class VWAPx : Indicator
	{
		private double iCumVolume = 0;
		private double iCumTypicalVolume = 0;
		private double curVWAP = 0;
		private double deviation = 0;
		private double v2Sum = 0;
		private double hl3 = 0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Volume Weighted Average Price with up to 5 Standard Deviations and filled regions.";
				Name = "VWAPx";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
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
				IsSuspendedWhileInactive = true;
				AddPlot(new Stroke(Brushes.Black, DashStyleHelper.Dot, 1), PlotStyle.Line, "PlotVWAP");
				AddPlot(new Stroke(Brushes.Tan, DashStyleHelper.Dot, 1), PlotStyle.Line, "PlotVWAP1U");
				AddPlot(new Stroke(Brushes.Tan, DashStyleHelper.Dot, 1), PlotStyle.Line, "PlotVWAP1L");
				AddPlot(Brushes.Orange, "PlotVWAP2U");
				AddPlot(Brushes.Orange, "PlotVWAP2L");
				AddPlot(new Stroke(Brushes.Firebrick, 1), PlotStyle.Line, "PlotVWAP3U");
				AddPlot(new Stroke(Brushes.Firebrick, 1), PlotStyle.Line, "PlotVWAP3L");
				AddPlot(Brushes.Red, "PlotVWAP4U");
				AddPlot(Brushes.Red, "PlotVWAP4L");
				AddPlot(Brushes.Black, "PlotVWAP5U");
				AddPlot(Brushes.Black, "PlotVWAP5L");
			}
		}

		protected override void OnBarUpdate()
		{
			hl3 = ((High[0] + Low[0] + Close[0]) / 3);
			if (Bars.IsFirstBarOfSession) {
				iCumVolume = VOL()[0];
				iCumTypicalVolume = VOL()[0] * hl3;
				v2Sum = VOL()[0] * hl3 * hl3;
			} else {
				iCumVolume += VOL()[0];
				iCumTypicalVolume += (VOL()[0] * hl3);
				v2Sum += VOL()[0] * hl3 * hl3;
			}
			if (iCumVolume == 0) return;
			curVWAP = (iCumTypicalVolume / iCumVolume);
			deviation = Math.Sqrt(Math.Max(v2Sum / iCumVolume - curVWAP * curVWAP, 0));
			PlotVWAP[0] = curVWAP;
			switch (NumDeviations) {
				case 1: PlotDevOne(); break;
				case 2: PlotDevTwo(); break;
				case 3: PlotDevThree(); break;
				case 4: PlotDevFour(); break;
				case 5: PlotDevFive(); break;
				default: break;
			}
		}

		private void PlotDevOne() {
			PlotVWAP1U[0] = curVWAP + SD1 * deviation; PlotVWAP1L[0] = curVWAP - SD1 * deviation;
			Draw.Region(this, "dev1", CurrentBar, 0, PlotVWAP1U, PlotVWAP1L, null, SD1AreaBrush, SD1AreaOpacity);
		}
		private void PlotDevTwo() {
			PlotDevOne(); PlotVWAP2U[0] = curVWAP + SD2 * deviation; PlotVWAP2L[0] = curVWAP - SD2 * deviation;
			Draw.Region(this, "dev2", CurrentBar, 0, PlotVWAP1U, PlotVWAP2U, null, SD2AreaBrush, SD2AreaOpacity);
			Draw.Region(this, "dev3", CurrentBar, 0, PlotVWAP1L, PlotVWAP2L, null, SD2AreaBrush, SD2AreaOpacity);
		}
		private void PlotDevThree() {
			PlotDevTwo(); PlotVWAP3U[0] = curVWAP + SD3 * deviation; PlotVWAP3L[0] = curVWAP - SD3 * deviation;
			Draw.Region(this, "dev4", CurrentBar, 0, PlotVWAP2U, PlotVWAP3U, null, SD3AreaBrush, SD3AreaOpacity);
			Draw.Region(this, "dev5", CurrentBar, 0, PlotVWAP2L, PlotVWAP3L, null, SD3AreaBrush, SD3AreaOpacity);
		}
		private void PlotDevFour() {
			PlotDevThree(); PlotVWAP4U[0] = curVWAP + SD4 * deviation; PlotVWAP4L[0] = curVWAP - SD4 * deviation;
			Draw.Region(this, "dev6", CurrentBar, 0, PlotVWAP3U, PlotVWAP4U, null, SD4AreaBrush, SD4AreaOpacity);
			Draw.Region(this, "dev7", CurrentBar, 0, PlotVWAP3L, PlotVWAP4L, null, SD4AreaBrush, SD4AreaOpacity);
		}
		private void PlotDevFive() {
			PlotDevFour(); PlotVWAP5U[0] = curVWAP + SD5 * deviation; PlotVWAP5L[0] = curVWAP - SD5 * deviation;
			Draw.Region(this, "dev8", CurrentBar, 0, PlotVWAP4U, PlotVWAP5U, null, SD5AreaBrush, SD5AreaOpacity);
			Draw.Region(this, "dev9", CurrentBar, 0, PlotVWAP4L, PlotVWAP5L, null, SD5AreaBrush, SD5AreaOpacity);
		}

		[RefreshProperties(RefreshProperties.All)] [Range(0, 5)] [Display(Name = "Number of deviations", Order = 1, GroupName = "Standard Deviations")] public int NumDeviations { get; set; }
		[Display(Name = "Deviation 1", Order = 2, GroupName = "Standard Deviations 1")] public double SD1 { get; set; }
		[Display(Name = "SD1 Fill Opacity", Order = 3, GroupName = "Standard Deviations 1")] public int SD1AreaOpacity { get; set; }
		[XmlIgnore] [Display(Name = "SD1 Fill Color", Order = 4, GroupName = "Standard Deviations 1")] public Brush SD1AreaBrush { get; set; }
		[Display(Name = "Deviation 2", Order = 5, GroupName = "Standard Deviations 2")] public double SD2 { get; set; }
		[Display(Name = "SD2 Fill Opacity", Order = 6, GroupName = "Standard Deviations 2")] public int SD2AreaOpacity { get; set; }
		[XmlIgnore] [Display(Name = "SD2 Fill Color", Order = 7, GroupName = "Standard Deviations 2")] public Brush SD2AreaBrush { get; set; }
		[Display(Name = "Deviation 3", Order = 8, GroupName = "Standard Deviations 3")] public double SD3 { get; set; }
		[Display(Name = "SD3 Fill Opacity", Order = 9, GroupName = "Standard Deviations 3")] public int SD3AreaOpacity { get; set; }
		[XmlIgnore] [Display(Name = "SD3 Fill Color", Order = 10, GroupName = "Standard Deviations 3")] public Brush SD3AreaBrush { get; set; }
		[Display(Name = "Deviation 4", Order = 11, GroupName = "Standard Deviations 4")] public double SD4 { get; set; }
		[Display(Name = "SD4 Fill Opacity", Order = 12, GroupName = "Standard Deviations 4")] public int SD4AreaOpacity { get; set; }
		[XmlIgnore] [Display(Name = "SD4 Fill Color", Order = 13, GroupName = "Standard Deviations 4")] public Brush SD4AreaBrush { get; set; }
		[Display(Name = "Deviation 5", Order = 14, GroupName = "Standard Deviations 5")] public double SD5 { get; set; }
		[Display(Name = "SD5 Fill Opacity", Order = 15, GroupName = "Standard Deviations 5")] public int SD5AreaOpacity { get; set; }
		[XmlIgnore] [Display(Name = "SD5 Fill Color", Order = 16, GroupName = "Standard Deviations 5")] public Brush SD5AreaBrush { get; set; }

		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP { get { return Values[0]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP1U { get { return Values[1]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP1L { get { return Values[2]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP2U { get { return Values[3]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP2L { get { return Values[4]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP3U { get { return Values[5]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP3L { get { return Values[6]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP4U { get { return Values[7]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP4L { get { return Values[8]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP5U { get { return Values[9]; } }
		[Browsable(false)] [XmlIgnore] public Series<double> PlotVWAP5L { get { return Values[10]; } }
	}
}
