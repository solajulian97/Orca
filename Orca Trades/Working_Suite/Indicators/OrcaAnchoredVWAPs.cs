#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaAnchoredVWAPs : Indicator
	{
		private VwapTracker devTracker;
		private VwapTracker stdTracker;
		private VwapTracker htfTracker;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Anchored VWAP indicator with 3 reversal thresholds, standard deviation bands, and region fills.";
				Name = "Orca Anchored VWAPs";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive = true;

				DevelopingTicks = 120;
				StandardTicks = 250;
				HtfTicks = 500;
                
				UseAtrReversal = false;
				AtrPeriod = 14;
				DevAtrMultiplier = 1.0;
				StdAtrMultiplier = 2.0;
				HtfAtrMultiplier = 3.0;
                
				ShowStdBands = true;
				ShowHtfBands = true;
				ShowAllBands = false;
				ShowStdDev1 = true;
				ShowStdDev2 = true;
				ShowStdDev3 = true;

				StdDevMultiplier1 = 1.0;
				StdDevMultiplier2 = 2.0;
				StdDevMultiplier3 = 3.0;

				FillColorStdCore1 = Brushes.LightBlue;
				FillOpacityStdCore1 = 20;
				FillColorStd12 = Brushes.DodgerBlue;
				FillOpacityStd12 = 15;
				FillColorStd23 = Brushes.RoyalBlue;
				FillOpacityStd23 = 10;
                
				FillColorHtfCore1 = Brushes.PaleTurquoise;
				FillOpacityHtfCore1 = 20;
				FillColorHtf12 = Brushes.DarkCyan;
				FillOpacityHtf12 = 15;
				FillColorHtf23 = Brushes.Teal;
				FillOpacityHtf23 = 10;

				AddPlot(new Stroke(Brushes.Gray, 2), PlotStyle.Line, "Dev VWAP");
				AddPlot(new Stroke(Brushes.Magenta, 2), PlotStyle.Line, "Std VWAP");
				AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "HTF VWAP");

				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "Std VWAP Upper 1");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "Std VWAP Upper 2");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "Std VWAP Upper 3");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "Std VWAP Lower 1");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "Std VWAP Lower 2");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "Std VWAP Lower 3");

				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "HTF VWAP Upper 1");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "HTF VWAP Upper 2");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "HTF VWAP Upper 3");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "HTF VWAP Lower 1");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "HTF VWAP Lower 2");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "HTF VWAP Lower 3");
			}
			else if (State == State.Configure)
			{
				devTracker = new VwapTracker(TickSize, DevelopingTicks);
				stdTracker = new VwapTracker(TickSize, StandardTicks);
				htfTracker = new VwapTracker(TickSize, HtfTicks);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) return;

			if (UseAtrReversal)
			{
				double atrValue = ATR(AtrPeriod)[0];
				devTracker.ReversalTicks = Math.Max(1.0, (atrValue * DevAtrMultiplier) / TickSize);
				stdTracker.ReversalTicks = Math.Max(1.0, (atrValue * StdAtrMultiplier) / TickSize);
				htfTracker.ReversalTicks = Math.Max(1.0, (atrValue * HtfAtrMultiplier) / TickSize);
			}
			else
			{
				devTracker.ReversalTicks = DevelopingTicks;
				stdTracker.ReversalTicks = StandardTicks;
				htfTracker.ReversalTicks = HtfTicks;
			}

			devTracker.Process(High[0], Low[0], Close[0], Volume[0], CurrentBar);
			stdTracker.Process(High[0], Low[0], Close[0], Volume[0], CurrentBar);
			htfTracker.Process(High[0], Low[0], Close[0], Volume[0], CurrentBar);

			bool drawDev = devTracker.IsActive && (devTracker.ActiveAnchorBar > stdTracker.ActiveAnchorBar);
			bool drawStd = stdTracker.IsActive && (!htfTracker.IsActive || stdTracker.ActiveAnchorBar > htfTracker.ActiveAnchorBar);
			bool drawHtf = htfTracker.IsActive;

			DrawVwap(0, -1,-1,-1, -1,-1,-1, devTracker, drawDev, false);
			DrawVwap(1,  3, 4, 5,  6, 7, 8, stdTracker, drawStd, ShowStdBands);
			DrawVwap(2,  9,10,11, 12,13,14, htfTracker, drawHtf, ShowHtfBands);

			DrawRegions(drawStd, drawHtf);
		}

		private void DrawRegions(bool drawStd, bool drawHtf)
		{
			if (ShowStdBands && drawStd && stdTracker.ActiveAnchorBar >= 0 && stdTracker.ActiveAnchorBar <= CurrentBar)
			{
				int stdStartBarsAgo = CurrentBar - stdTracker.ActiveAnchorBar;
				bool stdShowUp = ShowAllBands || stdTracker.Direction == -1;
				bool stdShowDn = ShowAllBands || stdTracker.Direction == 1;

				if (stdShowUp)
				{
					if (ShowStdDev1) Draw.Region(this, "StdUpperCore_1", stdStartBarsAgo, 0, Values[1], Values[3], null, FillColorStdCore1, FillOpacityStdCore1);
					if (ShowStdDev1 && ShowStdDev2) Draw.Region(this, "StdUpper1_2", stdStartBarsAgo, 0, Values[3], Values[4], null, FillColorStd12, FillOpacityStd12);
					if (ShowStdDev2 && ShowStdDev3) Draw.Region(this, "StdUpper2_3", stdStartBarsAgo, 0, Values[4], Values[5], null, FillColorStd23, FillOpacityStd23);
				}
				else
				{
					RemoveDrawObject("StdUpperCore_1");
					RemoveDrawObject("StdUpper1_2");
					RemoveDrawObject("StdUpper2_3");
				}

				if (stdShowDn)
				{
					if (ShowStdDev1) Draw.Region(this, "StdLowerCore_1", stdStartBarsAgo, 0, Values[1], Values[6], null, FillColorStdCore1, FillOpacityStdCore1);
					if (ShowStdDev1 && ShowStdDev2) Draw.Region(this, "StdLower1_2", stdStartBarsAgo, 0, Values[6], Values[7], null, FillColorStd12, FillOpacityStd12);
					if (ShowStdDev2 && ShowStdDev3) Draw.Region(this, "StdLower2_3", stdStartBarsAgo, 0, Values[7], Values[8], null, FillColorStd23, FillOpacityStd23);
				}
				else
				{
					RemoveDrawObject("StdLowerCore_1");
					RemoveDrawObject("StdLower1_2");
					RemoveDrawObject("StdLower2_3");
				}
			}
			else if (!drawStd)
			{
				RemoveDrawObject("StdUpperCore_1");
				RemoveDrawObject("StdLowerCore_1");
				RemoveDrawObject("StdUpper1_2");
				RemoveDrawObject("StdLower1_2");
				RemoveDrawObject("StdUpper2_3");
				RemoveDrawObject("StdLower2_3");
			}

			if (ShowHtfBands && drawHtf && htfTracker.ActiveAnchorBar >= 0 && htfTracker.ActiveAnchorBar <= CurrentBar)
			{
				int htfStartBarsAgo = CurrentBar - htfTracker.ActiveAnchorBar;
				bool htfShowUp = ShowAllBands || htfTracker.Direction == -1;
				bool htfShowDn = ShowAllBands || htfTracker.Direction == 1;

				if (htfShowUp)
				{
					if (ShowStdDev1) Draw.Region(this, "HtfUpperCore_1", htfStartBarsAgo, 0, Values[2], Values[9], null, FillColorHtfCore1, FillOpacityHtfCore1);
					if (ShowStdDev1 && ShowStdDev2) Draw.Region(this, "HtfUpper1_2", htfStartBarsAgo, 0, Values[9], Values[10], null, FillColorHtf12, FillOpacityHtf12);
					if (ShowStdDev2 && ShowStdDev3) Draw.Region(this, "HtfUpper2_3", htfStartBarsAgo, 0, Values[10], Values[11], null, FillColorHtf23, FillOpacityHtf23);
				}
				else
				{
					RemoveDrawObject("HtfUpperCore_1");
					RemoveDrawObject("HtfUpper1_2");
					RemoveDrawObject("HtfUpper2_3");
				}

				if (htfShowDn)
				{
					if (ShowStdDev1) Draw.Region(this, "HtfLowerCore_1", htfStartBarsAgo, 0, Values[2], Values[12], null, FillColorHtfCore1, FillOpacityHtfCore1);
					if (ShowStdDev1 && ShowStdDev2) Draw.Region(this, "HtfLower1_2", htfStartBarsAgo, 0, Values[12], Values[13], null, FillColorHtf12, FillOpacityHtf12);
					if (ShowStdDev2 && ShowStdDev3) Draw.Region(this, "HtfLower2_3", htfStartBarsAgo, 0, Values[13], Values[14], null, FillColorHtf23, FillOpacityHtf23);
				}
				else
				{
					RemoveDrawObject("HtfLowerCore_1");
					RemoveDrawObject("HtfLower1_2");
					RemoveDrawObject("HtfLower2_3");
				}
			}
			else if (!drawHtf)
			{
				RemoveDrawObject("HtfUpperCore_1");
				RemoveDrawObject("HtfLowerCore_1");
				RemoveDrawObject("HtfUpper1_2");
				RemoveDrawObject("HtfLower1_2");
				RemoveDrawObject("HtfUpper2_3");
				RemoveDrawObject("HtfLower2_3");
			}
		}

		private void DrawVwap(int coreIdx, int u1, int u2, int u3, int l1, int l2, int l3, VwapTracker tracker, bool visible, bool showBands)
		{
			if (!visible || !tracker.IsActive || tracker.ActiveAnchorBar < 0 || CurrentBar < tracker.ActiveAnchorBar)
			{
				Values[coreIdx].Reset();
				if (u1 >= 0) {
					Values[u1].Reset(); Values[u2].Reset(); Values[u3].Reset();
					Values[l1].Reset(); Values[l2].Reset(); Values[l3].Reset();
				}
				return;
			}

			if (tracker.JustReversed)
			{
				// NUCLEAR: Wipe entire plot history clean so no old points exist to connect to
				for (int i = 0; i <= CurrentBar; i++)
				{
					Values[coreIdx].Reset(i);
					if (u1 >= 0) {
						Values[u1].Reset(i); Values[u2].Reset(i); Values[u3].Reset(i);
						Values[l1].Reset(i); Values[l2].Reset(i); Values[l3].Reset(i);
					}
				}

				double priorCumVol = 0;
				double priorCumPV = 0;
				double priorCumP2V = 0;

				for (int barsAgo = CurrentBar - tracker.ActiveAnchorBar; barsAgo > 0; barsAgo--)
				{
					double tp = (High[barsAgo] + Low[barsAgo] + Close[barsAgo]) / 3.0;
					bool isAnchorHistory = (barsAgo == CurrentBar - tracker.ActiveAnchorBar);

					priorCumVol += Volume[barsAgo];
					priorCumPV += tp * Volume[barsAgo];
					priorCumP2V += tp * tp * Volume[barsAgo];

					double vwapHistory = 0;
					if (isAnchorHistory)
						vwapHistory = tracker.AnchorPrice;
					else if (priorCumVol > 0)
						vwapHistory = priorCumPV / priorCumVol;
					
					Values[coreIdx][barsAgo] = vwapHistory;

					if (u1 >= 0)
					{
						double stdDevHistory = 0;
						if (isAnchorHistory) stdDevHistory = 0;
						else if (priorCumVol > 0) stdDevHistory = Math.Sqrt(Math.Max(0, (priorCumP2V / priorCumVol) - (vwapHistory * vwapHistory)));
						SetBands(barsAgo, vwapHistory, stdDevHistory, tracker.Direction, u1, u2, u3, l1, l2, l3, showBands);
					}
				}

				tracker.PriorCumVol = priorCumVol;
				tracker.PriorCumPV = priorCumPV;
				tracker.PriorCumP2V = priorCumP2V;
				tracker.LastBarSeen = CurrentBar;

				// Now process the current bar (barsAgo = 0)
				bool isAnchor = (0 == CurrentBar - tracker.ActiveAnchorBar);
				double tp0 = (High[0] + Low[0] + Close[0]) / 3.0;
				double currentCumVol = priorCumVol + Volume[0];
				double currentCumPV = priorCumPV + tp0 * Volume[0];
				double currentCumP2V = priorCumP2V + tp0 * tp0 * Volume[0];

				double vwap = 0;
				if (isAnchor)
					vwap = tracker.AnchorPrice;
				else if (currentCumVol > 0)
					vwap = currentCumPV / currentCumVol;
				
				Values[coreIdx][0] = vwap;
				
				if (u1 >= 0)
				{
					double stdDev = 0;
					if (isAnchor) stdDev = 0;
					else if (currentCumVol > 0) stdDev = Math.Sqrt(Math.Max(0, (currentCumP2V / currentCumVol) - (vwap * vwap)));
					SetBands(0, vwap, stdDev, tracker.Direction, u1, u2, u3, l1, l2, l3, showBands);
				}
			}
			else
			{
				bool isAnchor = (CurrentBar == tracker.ActiveAnchorBar);

				if (tracker.LastBarSeen != CurrentBar && tracker.LastBarSeen >= 0)
				{
					double tp1 = (High[1] + Low[1] + Close[1]) / 3.0;
					tracker.PriorCumVol += Volume[1];
					tracker.PriorCumPV += tp1 * Volume[1];
					tracker.PriorCumP2V += tp1 * tp1 * Volume[1];
				}
				tracker.LastBarSeen = CurrentBar;
				
				// Append current bar to running VWAP
				double tp0 = (High[0] + Low[0] + Close[0]) / 3.0;
				double currentCumVol = tracker.PriorCumVol + Volume[0];
				double currentCumPV = tracker.PriorCumPV + tp0 * Volume[0];
				double currentCumP2V = tracker.PriorCumP2V + tp0 * tp0 * Volume[0];

				double vwap = 0;
				if (isAnchor)
					vwap = tracker.AnchorPrice;
				else if (currentCumVol > 0)
					vwap = currentCumPV / currentCumVol;
					
				Values[coreIdx][0] = vwap;

				if (u1 >= 0)
				{
					double stdDev = 0;
					if (isAnchor)
					{
						stdDev = 0;
					}
					else if (currentCumVol > 0)
					{
						double variance = (currentCumP2V / currentCumVol) - (vwap * vwap);
						stdDev = Math.Sqrt(Math.Max(0, variance));
					}
					SetBands(0, vwap, stdDev, tracker.Direction, u1, u2, u3, l1, l2, l3, showBands);
				}
			}

			// ALWAYS maintain the gap: reset the bar just before the anchor on every single update.
			int gapBarsAgo = CurrentBar - tracker.ActiveAnchorBar + 1;
			if (gapBarsAgo >= 0 && gapBarsAgo <= CurrentBar)
			{
				Values[coreIdx].Reset(gapBarsAgo);
				if (u1 >= 0) {
					Values[u1].Reset(gapBarsAgo); Values[u2].Reset(gapBarsAgo); Values[u3].Reset(gapBarsAgo);
					Values[l1].Reset(gapBarsAgo); Values[l2].Reset(gapBarsAgo); Values[l3].Reset(gapBarsAgo);
				}
			}
		}

		private void SetBands(int barsAgo, double vwap, double stdDev, int anchorDirection, int u1, int u2, int u3, int l1, int l2, int l3, bool showBands)
		{
			if (!showBands)
			{
				Values[u1].Reset(barsAgo); Values[u2].Reset(barsAgo); Values[u3].Reset(barsAgo);
				Values[l1].Reset(barsAgo); Values[l2].Reset(barsAgo); Values[l3].Reset(barsAgo);
				return;
			}

			bool showUp = ShowAllBands || anchorDirection == -1; // -1 means anchored from High (going down)  -> Resistance
			bool showDn = ShowAllBands || anchorDirection == 1;  // 1 means anchored from Low (going up)     -> Support

			if (showUp)
			{
				if (ShowStdDev1) Values[u1][barsAgo] = vwap + StdDevMultiplier1 * stdDev; else Values[u1].Reset(barsAgo);
				if (ShowStdDev2) Values[u2][barsAgo] = vwap + StdDevMultiplier2 * stdDev; else Values[u2].Reset(barsAgo);
				if (ShowStdDev3) Values[u3][barsAgo] = vwap + StdDevMultiplier3 * stdDev; else Values[u3].Reset(barsAgo);
			}
			else
			{
				Values[u1].Reset(barsAgo); Values[u2].Reset(barsAgo); Values[u3].Reset(barsAgo);
			}

			if (showDn)
			{
				if (ShowStdDev1) Values[l1][barsAgo] = vwap - StdDevMultiplier1 * stdDev; else Values[l1].Reset(barsAgo);
				if (ShowStdDev2) Values[l2][barsAgo] = vwap - StdDevMultiplier2 * stdDev; else Values[l2].Reset(barsAgo);
				if (ShowStdDev3) Values[l3][barsAgo] = vwap - StdDevMultiplier3 * stdDev; else Values[l3].Reset(barsAgo);
			}
			else
			{
				Values[l1].Reset(barsAgo); Values[l2].Reset(barsAgo); Values[l3].Reset(barsAgo);
			}
		}

		#region Properties

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Developing Reversal Ticks", Description="Tick threshold for the Developing VWAP pivot", Order=1, GroupName="Parameters")]
		public int DevelopingTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Standard Reversal Ticks", Description="Tick threshold for the Standard VWAP pivot", Order=2, GroupName="Parameters")]
		public int StandardTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="HTF Reversal Ticks", Description="Tick threshold for the HTF VWAP pivot", Order=3, GroupName="Parameters")]
		public int HtfTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Use ATR Reversal", Description="Toggle ATR-based Reversal", Order=4, GroupName="Parameters")]
		public bool UseAtrReversal { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ATR Period", Description="Lookback period for ATR", Order=5, GroupName="Parameters")]
		public int AtrPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name="Dev ATR Multiplier", Description="ATR Multiplier for Developing VWAP", Order=6, GroupName="Parameters")]
		public double DevAtrMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name="Std ATR Multiplier", Description="ATR Multiplier for Standard VWAP", Order=7, GroupName="Parameters")]
		public double StdAtrMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name="HTF ATR Multiplier", Description="ATR Multiplier for HTF VWAP", Order=8, GroupName="Parameters")]
		public double HtfAtrMultiplier { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Standard VWAP Bands", Order=1, GroupName="Standard Deviation")]
		public bool ShowStdBands { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show HTF VWAP Bands", Order=2, GroupName="Standard Deviation")]
		public bool ShowHtfBands { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show All Bands (Override Filter)", Order=3, GroupName="Standard Deviation")]
		public bool ShowAllBands { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Std Dev 1", Order=4, GroupName="Standard Deviation")]
		public bool ShowStdDev1 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Std Dev 2", Order=5, GroupName="Standard Deviation")]
		public bool ShowStdDev2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Std Dev 3", Order=6, GroupName="Standard Deviation")]
		public bool ShowStdDev3 { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name="Std Dev Multiplier 1", Order=7, GroupName="Standard Deviation")]
		public double StdDevMultiplier1 { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name="Std Dev Multiplier 2", Order=8, GroupName="Standard Deviation")]
		public double StdDevMultiplier2 { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name="Std Dev Multiplier 3", Order=9, GroupName="Standard Deviation")]
		public double StdDevMultiplier3 { get; set; }

		[XmlIgnore]
		[Display(Name="Fill Color Core-1 (Std)", Order=7, GroupName="Standard Deviation Regions")]
		public Brush FillColorStdCore1 { get; set; }

		[Browsable(false)]
		public string FillColorStdCore1Serializable
		{
			get { return Serialize.BrushToString(FillColorStdCore1); }
			set { FillColorStdCore1 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Fill Opacity Core-1 (Std)", Order=8, GroupName="Standard Deviation Regions")]
		public int FillOpacityStdCore1 { get; set; }

		[XmlIgnore]
		[Display(Name="Fill Color 1-2 (Std)", Order=9, GroupName="Standard Deviation Regions")]
		public Brush FillColorStd12 { get; set; }

		[Browsable(false)]
		public string FillColorStd12Serializable
		{
			get { return Serialize.BrushToString(FillColorStd12); }
			set { FillColorStd12 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Fill Opacity 1-2 (Std)", Order=10, GroupName="Standard Deviation Regions")]
		public int FillOpacityStd12 { get; set; }


		[XmlIgnore]
		[Display(Name="Fill Color 2-3 (Std)", Order=11, GroupName="Standard Deviation Regions")]
		public Brush FillColorStd23 { get; set; }

		[Browsable(false)]
		public string FillColorStd23Serializable
		{
			get { return Serialize.BrushToString(FillColorStd23); }
			set { FillColorStd23 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Fill Opacity 2-3 (Std)", Order=12, GroupName="Standard Deviation Regions")]
		public int FillOpacityStd23 { get; set; }


		[XmlIgnore]
		[Display(Name="Fill Color Core-1 (HTF)", Order=13, GroupName="Standard Deviation Regions")]
		public Brush FillColorHtfCore1 { get; set; }

		[Browsable(false)]
		public string FillColorHtfCore1Serializable
		{
			get { return Serialize.BrushToString(FillColorHtfCore1); }
			set { FillColorHtfCore1 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Fill Opacity Core-1 (HTF)", Order=14, GroupName="Standard Deviation Regions")]
		public int FillOpacityHtfCore1 { get; set; }

		[XmlIgnore]
		[Display(Name="Fill Color 1-2 (HTF)", Order=15, GroupName="Standard Deviation Regions")]
		public Brush FillColorHtf12 { get; set; }

		[Browsable(false)]
		public string FillColorHtf12Serializable
		{
			get { return Serialize.BrushToString(FillColorHtf12); }
			set { FillColorHtf12 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Fill Opacity 1-2 (HTF)", Order=16, GroupName="Standard Deviation Regions")]
		public int FillOpacityHtf12 { get; set; }


		[XmlIgnore]
		[Display(Name="Fill Color 2-3 (HTF)", Order=17, GroupName="Standard Deviation Regions")]
		public Brush FillColorHtf23 { get; set; }

		[Browsable(false)]
		public string FillColorHtf23Serializable
		{
			get { return Serialize.BrushToString(FillColorHtf23); }
			set { FillColorHtf23 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Fill Opacity 2-3 (HTF)", Order=18, GroupName="Standard Deviation Regions")]
		public int FillOpacityHtf23 { get; set; }


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DevVWAP => Values[0];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StdVWAP => Values[1];

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HtfVWAP => Values[2];

		#endregion
	}

	public class VwapTracker
	{
		private double tickSize;
		public double ReversalTicks;

		public int Direction = 0;
		public double ExtremePrice;
		public int ExtremeBar = -1;

		public double AnchorPrice;
		public int ActiveAnchorBar = -1;
		public bool IsActive = false;
		public bool JustReversed = false;

		public double PriorCumVol;
		public double PriorCumPV;
		public double PriorCumP2V;
		public int LastBarSeen = -1;

		public VwapTracker(double tickSize, double reversalTicks)
		{
			this.tickSize = tickSize;
			this.ReversalTicks = reversalTicks;
		}

		public void Process(double high, double low, double close, double volume, int currentBar)
		{
			JustReversed = false;

			if (Direction == 0)
			{
				Direction = 1;
				ExtremePrice = high;
				ExtremeBar = currentBar;
				return;
			}

			// Step 1: Extend the trend if applicable
			bool extended = false;
			if ((Direction == 1 && high >= ExtremePrice) || (Direction == -1 && low <= ExtremePrice))
			{
				ExtremePrice = (Direction == 1) ? high : low;
				ExtremeBar = currentBar;
				extended = true;
			}

			// Step 2: Check for reversal using (possibly updated) extreme
			bool reversed = (Direction == 1 && (ExtremePrice - low) / tickSize >= ReversalTicks) ||
							(Direction == -1 && (high - ExtremePrice) / tickSize >= ReversalTicks);

			if (reversed)
			{
				// Save anchor price BEFORE flipping direction
				AnchorPrice = ExtremePrice;
				ActiveAnchorBar = ExtremeBar;

				// Flip direction and start tracking new extreme
				Direction = (Direction == 1) ? -1 : 1;
				ExtremePrice = (Direction == 1) ? high : low;
				ExtremeBar = currentBar;

				IsActive = true;
				JustReversed = true;
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaAnchoredVWAPs[] cacheOrcaAnchoredVWAPs;
		public OrcaAnchoredVWAPs OrcaAnchoredVWAPs(int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
		{
			return OrcaAnchoredVWAPs(Input, developingTicks, standardTicks, htfTicks, useAtrReversal, atrPeriod, devAtrMultiplier, stdAtrMultiplier, htfAtrMultiplier, showStdBands, showHtfBands, showAllBands, showStdDev1, showStdDev2, showStdDev3, stdDevMultiplier1, stdDevMultiplier2, stdDevMultiplier3, fillOpacityStdCore1, fillOpacityStd12, fillOpacityStd23, fillOpacityHtfCore1, fillOpacityHtf12, fillOpacityHtf23);
		}

		public OrcaAnchoredVWAPs OrcaAnchoredVWAPs(ISeries<double> input, int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
		{
			if (cacheOrcaAnchoredVWAPs != null)
				for (int idx = 0; idx < cacheOrcaAnchoredVWAPs.Length; idx++)
					if (cacheOrcaAnchoredVWAPs[idx] != null && cacheOrcaAnchoredVWAPs[idx].DevelopingTicks == developingTicks && cacheOrcaAnchoredVWAPs[idx].StandardTicks == standardTicks && cacheOrcaAnchoredVWAPs[idx].HtfTicks == htfTicks && cacheOrcaAnchoredVWAPs[idx].UseAtrReversal == useAtrReversal && cacheOrcaAnchoredVWAPs[idx].AtrPeriod == atrPeriod && cacheOrcaAnchoredVWAPs[idx].DevAtrMultiplier == devAtrMultiplier && cacheOrcaAnchoredVWAPs[idx].StdAtrMultiplier == stdAtrMultiplier && cacheOrcaAnchoredVWAPs[idx].HtfAtrMultiplier == htfAtrMultiplier && cacheOrcaAnchoredVWAPs[idx].ShowStdBands == showStdBands && cacheOrcaAnchoredVWAPs[idx].ShowHtfBands == showHtfBands && cacheOrcaAnchoredVWAPs[idx].ShowAllBands == showAllBands && cacheOrcaAnchoredVWAPs[idx].ShowStdDev1 == showStdDev1 && cacheOrcaAnchoredVWAPs[idx].ShowStdDev2 == showStdDev2 && cacheOrcaAnchoredVWAPs[idx].ShowStdDev3 == showStdDev3 && cacheOrcaAnchoredVWAPs[idx].StdDevMultiplier1 == stdDevMultiplier1 && cacheOrcaAnchoredVWAPs[idx].StdDevMultiplier2 == stdDevMultiplier2 && cacheOrcaAnchoredVWAPs[idx].StdDevMultiplier3 == stdDevMultiplier3 && cacheOrcaAnchoredVWAPs[idx].FillOpacityStdCore1 == fillOpacityStdCore1 && cacheOrcaAnchoredVWAPs[idx].FillOpacityStd12 == fillOpacityStd12 && cacheOrcaAnchoredVWAPs[idx].FillOpacityStd23 == fillOpacityStd23 && cacheOrcaAnchoredVWAPs[idx].FillOpacityHtfCore1 == fillOpacityHtfCore1 && cacheOrcaAnchoredVWAPs[idx].FillOpacityHtf12 == fillOpacityHtf12 && cacheOrcaAnchoredVWAPs[idx].FillOpacityHtf23 == fillOpacityHtf23 && cacheOrcaAnchoredVWAPs[idx].EqualsInput(input))
						return cacheOrcaAnchoredVWAPs[idx];
			return CacheIndicator<OrcaAnchoredVWAPs>(new OrcaAnchoredVWAPs(){ DevelopingTicks = developingTicks, StandardTicks = standardTicks, HtfTicks = htfTicks, UseAtrReversal = useAtrReversal, AtrPeriod = atrPeriod, DevAtrMultiplier = devAtrMultiplier, StdAtrMultiplier = stdAtrMultiplier, HtfAtrMultiplier = htfAtrMultiplier, ShowStdBands = showStdBands, ShowHtfBands = showHtfBands, ShowAllBands = showAllBands, ShowStdDev1 = showStdDev1, ShowStdDev2 = showStdDev2, ShowStdDev3 = showStdDev3, StdDevMultiplier1 = stdDevMultiplier1, StdDevMultiplier2 = stdDevMultiplier2, StdDevMultiplier3 = stdDevMultiplier3, FillOpacityStdCore1 = fillOpacityStdCore1, FillOpacityStd12 = fillOpacityStd12, FillOpacityStd23 = fillOpacityStd23, FillOpacityHtfCore1 = fillOpacityHtfCore1, FillOpacityHtf12 = fillOpacityHtf12, FillOpacityHtf23 = fillOpacityHtf23 }, input, ref cacheOrcaAnchoredVWAPs);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaAnchoredVWAPs OrcaAnchoredVWAPs(int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
		{
			return indicator.OrcaAnchoredVWAPs(Input, developingTicks, standardTicks, htfTicks, useAtrReversal, atrPeriod, devAtrMultiplier, stdAtrMultiplier, htfAtrMultiplier, showStdBands, showHtfBands, showAllBands, showStdDev1, showStdDev2, showStdDev3, stdDevMultiplier1, stdDevMultiplier2, stdDevMultiplier3, fillOpacityStdCore1, fillOpacityStd12, fillOpacityStd23, fillOpacityHtfCore1, fillOpacityHtf12, fillOpacityHtf23);
		}

		public Indicators.OrcaAnchoredVWAPs OrcaAnchoredVWAPs(ISeries<double> input , int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
		{
			return indicator.OrcaAnchoredVWAPs(input, developingTicks, standardTicks, htfTicks, useAtrReversal, atrPeriod, devAtrMultiplier, stdAtrMultiplier, htfAtrMultiplier, showStdBands, showHtfBands, showAllBands, showStdDev1, showStdDev2, showStdDev3, stdDevMultiplier1, stdDevMultiplier2, stdDevMultiplier3, fillOpacityStdCore1, fillOpacityStd12, fillOpacityStd23, fillOpacityHtfCore1, fillOpacityHtf12, fillOpacityHtf23);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaAnchoredVWAPs OrcaAnchoredVWAPs(int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
		{
			return indicator.OrcaAnchoredVWAPs(Input, developingTicks, standardTicks, htfTicks, useAtrReversal, atrPeriod, devAtrMultiplier, stdAtrMultiplier, htfAtrMultiplier, showStdBands, showHtfBands, showAllBands, showStdDev1, showStdDev2, showStdDev3, stdDevMultiplier1, stdDevMultiplier2, stdDevMultiplier3, fillOpacityStdCore1, fillOpacityStd12, fillOpacityStd23, fillOpacityHtfCore1, fillOpacityHtf12, fillOpacityHtf23);
		}

		public Indicators.OrcaAnchoredVWAPs OrcaAnchoredVWAPs(ISeries<double> input , int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
		{
			return indicator.OrcaAnchoredVWAPs(input, developingTicks, standardTicks, htfTicks, useAtrReversal, atrPeriod, devAtrMultiplier, stdAtrMultiplier, htfAtrMultiplier, showStdBands, showHtfBands, showAllBands, showStdDev1, showStdDev2, showStdDev3, stdDevMultiplier1, stdDevMultiplier2, stdDevMultiplier3, fillOpacityStdCore1, fillOpacityStd12, fillOpacityStd23, fillOpacityHtfCore1, fillOpacityHtf12, fillOpacityHtf23);
		}
	}
}

#endregion
