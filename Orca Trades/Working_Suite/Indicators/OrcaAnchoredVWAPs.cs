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
	[TypeConverter(typeof(OrcaAnchoredVwapTypeConverter))]
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

				ShowVwap1 = true;
				ShowVwap2 = true;
				ShowVwap3 = true;

				UseAtrReversal = false;
				AtrPeriod = 14;
				DevAtrMultiplier = 1.0;
				StdAtrMultiplier = 2.0;
				HtfAtrMultiplier = 3.0;

				ShowVwap1Bands = true;
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
				ShowVwap2Fills = true;

				FillColorHtfCore1 = Brushes.PaleTurquoise;
				FillOpacityHtfCore1 = 20;
				FillColorHtf12 = Brushes.DarkCyan;
				FillOpacityHtf12 = 15;
				FillColorHtf23 = Brushes.Teal;
				FillOpacityHtf23 = 10;
				ShowVwap3Fills = true;

				AddPlot(new Stroke(Brushes.Gray, 2), PlotStyle.Line, "VWAP 1");
				AddPlot(new Stroke(Brushes.Magenta, 2), PlotStyle.Line, "VWAP 2");
				AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "VWAP 3");

				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 2 Upper 1");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 2 Upper 2");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 2 Upper 3");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 2 Lower 1");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 2 Lower 2");
				AddPlot(new Stroke(Brushes.Magenta, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 2 Lower 3");

				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 3 Upper 1");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 3 Upper 2");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 3 Upper 3");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 3 Lower 1");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 3 Lower 2");
				AddPlot(new Stroke(Brushes.Cyan, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 3 Lower 3");

				AddPlot(new Stroke(Brushes.Gray, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 1 Upper 1");
				AddPlot(new Stroke(Brushes.Gray, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 1 Upper 2");
				AddPlot(new Stroke(Brushes.Gray, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 1 Upper 3");
				AddPlot(new Stroke(Brushes.Gray, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 1 Lower 1");
				AddPlot(new Stroke(Brushes.Gray, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 1 Lower 2");
				AddPlot(new Stroke(Brushes.Gray, DashStyleHelper.Dash, 1), PlotStyle.Line, "VWAP 1 Lower 3");
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

			bool drawDev = ShowVwap1 && devTracker.IsActive && (devTracker.ActiveAnchorBar > stdTracker.ActiveAnchorBar);
			bool drawStd = ShowVwap2 && stdTracker.IsActive && (!htfTracker.IsActive || stdTracker.ActiveAnchorBar > htfTracker.ActiveAnchorBar);
			bool drawHtf = ShowVwap3 && htfTracker.IsActive;

			DrawVwap(0, 15,16,17, 18,19,20, devTracker, drawDev, ShowVwap1Bands);
			DrawVwap(1,  3, 4, 5,  6, 7, 8, stdTracker, drawStd, ShowStdBands);
			DrawVwap(2,  9,10,11, 12,13,14, htfTracker, drawHtf, ShowHtfBands);

			DrawRegions(drawStd, drawHtf);
		}

		private void DrawRegions(bool drawStd, bool drawHtf)
		{
			if (ShowVwap2Fills && ShowStdBands && drawStd && stdTracker.ActiveAnchorBar >= 0 && stdTracker.ActiveAnchorBar <= CurrentBar)
			{
				int stdStartBarsAgo = CurrentBar - stdTracker.ActiveAnchorBar;
				bool stdShowUp = ShowAllBands || stdTracker.Direction == -1;
				bool stdShowDn = ShowAllBands || stdTracker.Direction == 1;

				if (stdShowUp)
				{
					if (ShowStdDev1) Draw.Region(this, "StdUpperCore_1", stdStartBarsAgo, 0, Values[1], Values[3], null, FillColorStdCore1, FillOpacityStdCore1);
					else RemoveDrawObject("StdUpperCore_1");
					if (ShowStdDev1 && ShowStdDev2) Draw.Region(this, "StdUpper1_2", stdStartBarsAgo, 0, Values[3], Values[4], null, FillColorStd12, FillOpacityStd12);
					else RemoveDrawObject("StdUpper1_2");
					if (ShowStdDev2 && ShowStdDev3) Draw.Region(this, "StdUpper2_3", stdStartBarsAgo, 0, Values[4], Values[5], null, FillColorStd23, FillOpacityStd23);
					else RemoveDrawObject("StdUpper2_3");
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
					else RemoveDrawObject("StdLowerCore_1");
					if (ShowStdDev1 && ShowStdDev2) Draw.Region(this, "StdLower1_2", stdStartBarsAgo, 0, Values[6], Values[7], null, FillColorStd12, FillOpacityStd12);
					else RemoveDrawObject("StdLower1_2");
					if (ShowStdDev2 && ShowStdDev3) Draw.Region(this, "StdLower2_3", stdStartBarsAgo, 0, Values[7], Values[8], null, FillColorStd23, FillOpacityStd23);
					else RemoveDrawObject("StdLower2_3");
				}
				else
				{
					RemoveDrawObject("StdLowerCore_1");
					RemoveDrawObject("StdLower1_2");
					RemoveDrawObject("StdLower2_3");
				}
			}
			else
			{
				RemoveDrawObject("StdUpperCore_1");
				RemoveDrawObject("StdLowerCore_1");
				RemoveDrawObject("StdUpper1_2");
				RemoveDrawObject("StdLower1_2");
				RemoveDrawObject("StdUpper2_3");
				RemoveDrawObject("StdLower2_3");
			}

			if (ShowVwap3Fills && ShowHtfBands && drawHtf && htfTracker.ActiveAnchorBar >= 0 && htfTracker.ActiveAnchorBar <= CurrentBar)
			{
				int htfStartBarsAgo = CurrentBar - htfTracker.ActiveAnchorBar;
				bool htfShowUp = ShowAllBands || htfTracker.Direction == -1;
				bool htfShowDn = ShowAllBands || htfTracker.Direction == 1;

				if (htfShowUp)
				{
					if (ShowStdDev1) Draw.Region(this, "HtfUpperCore_1", htfStartBarsAgo, 0, Values[2], Values[9], null, FillColorHtfCore1, FillOpacityHtfCore1);
					else RemoveDrawObject("HtfUpperCore_1");
					if (ShowStdDev1 && ShowStdDev2) Draw.Region(this, "HtfUpper1_2", htfStartBarsAgo, 0, Values[9], Values[10], null, FillColorHtf12, FillOpacityHtf12);
					else RemoveDrawObject("HtfUpper1_2");
					if (ShowStdDev2 && ShowStdDev3) Draw.Region(this, "HtfUpper2_3", htfStartBarsAgo, 0, Values[10], Values[11], null, FillColorHtf23, FillOpacityHtf23);
					else RemoveDrawObject("HtfUpper2_3");
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
					else RemoveDrawObject("HtfLowerCore_1");
					if (ShowStdDev1 && ShowStdDev2) Draw.Region(this, "HtfLower1_2", htfStartBarsAgo, 0, Values[12], Values[13], null, FillColorHtf12, FillOpacityHtf12);
					else RemoveDrawObject("HtfLower1_2");
					if (ShowStdDev2 && ShowStdDev3) Draw.Region(this, "HtfLower2_3", htfStartBarsAgo, 0, Values[13], Values[14], null, FillColorHtf23, FillOpacityHtf23);
					else RemoveDrawObject("HtfLower2_3");
				}
				else
				{
					RemoveDrawObject("HtfLowerCore_1");
					RemoveDrawObject("HtfLower1_2");
					RemoveDrawObject("HtfLower2_3");
				}
			}
			else
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

		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Show VWAP 1", Description="Shows VWAP 1, the developing anchored VWAP.", Order=1, GroupName="1. VWAPs")]
		public bool ShowVwap1 { get; set; }

		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Show VWAP 2", Description="Shows VWAP 2, the standard anchored VWAP.", Order=2, GroupName="1. VWAPs")]
		public bool ShowVwap2 { get; set; }

		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Show VWAP 3", Description="Shows VWAP 3, the higher-timeframe anchored VWAP.", Order=3, GroupName="1. VWAPs")]
		public bool ShowVwap3 { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="VWAP 1 Reversal Ticks", Description="Fixed tick threshold for the VWAP 1 pivot.", Order=10, GroupName="2. Anchor Detection")]
		public int DevelopingTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="VWAP 2 Reversal Ticks", Description="Fixed tick threshold for the VWAP 2 pivot.", Order=20, GroupName="2. Anchor Detection")]
		public int StandardTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="VWAP 3 Reversal Ticks", Description="Fixed tick threshold for the VWAP 3 pivot.", Order=30, GroupName="2. Anchor Detection")]
		public int HtfTicks { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Use ATR-Based Reversals", Description="Off uses fixed reversal ticks. On uses ATR period and multipliers.", Order=1, GroupName="2. Anchor Detection")]
		public bool UseAtrReversal { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ATR Period", Description="Lookback period used by ATR-based anchor detection.", Order=40, GroupName="2. Anchor Detection")]
		public int AtrPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name="VWAP 1 ATR Multiplier", Description="ATR reversal multiplier for VWAP 1.", Order=50, GroupName="2. Anchor Detection")]
		public double DevAtrMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name="VWAP 2 ATR Multiplier", Description="ATR reversal multiplier for VWAP 2.", Order=60, GroupName="2. Anchor Detection")]
		public double StdAtrMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name="VWAP 3 ATR Multiplier", Description="ATR reversal multiplier for VWAP 3.", Order=70, GroupName="2. Anchor Detection")]
		public double HtfAtrMultiplier { get; set; }

		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Show VWAP 1 Bands", Description="Shows deviation bands for VWAP 1.", Order=11, GroupName="1. VWAPs")]
		public bool ShowVwap1Bands { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Show VWAP 2 Bands", Description="Shows deviation bands for VWAP 2.", Order=12, GroupName="1. VWAPs")]
		public bool ShowStdBands { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Show VWAP 3 Bands", Description="Shows deviation bands for VWAP 3.", Order=13, GroupName="1. VWAPs")]
		public bool ShowHtfBands { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Both Band Sides", Description="Off shows only the directional support or resistance side. On shows upper and lower bands.", Order=1, GroupName="3. Deviation Bands")]
		public bool ShowAllBands { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Show Band 1", Description="Shows the first standard-deviation band.", Order=10, GroupName="3. Deviation Bands")]
		public bool ShowStdDev1 { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Show Band 2", Description="Shows the second standard-deviation band.", Order=20, GroupName="3. Deviation Bands")]
		public bool ShowStdDev2 { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Show Band 3", Description="Shows the third standard-deviation band.", Order=30, GroupName="3. Deviation Bands")]
		public bool ShowStdDev3 { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name="Band 1 Multiplier", Description="Standard-deviation multiplier for Band 1.", Order=11, GroupName="3. Deviation Bands")]
		public double StdDevMultiplier1 { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name="Band 2 Multiplier", Description="Standard-deviation multiplier for Band 2.", Order=21, GroupName="3. Deviation Bands")]
		public double StdDevMultiplier2 { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name="Band 3 Multiplier", Description="Standard-deviation multiplier for Band 3.", Order=31, GroupName="3. Deviation Bands")]
		public double StdDevMultiplier3 { get; set; }

		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Enable VWAP 2 Fill", Description="Fills the selected deviation zones for VWAP 2.", Order=1, GroupName="4. VWAP 2 Fill")]
		public bool ShowVwap2Fills { get; set; }

		[XmlIgnore]
		[Display(Name="VWAP to Band 1 Color", Order=10, GroupName="4. VWAP 2 Fill")]
		public Brush FillColorStdCore1 { get; set; }

		[Browsable(false)]
		public string FillColorStdCore1Serializable
		{
			get { return Serialize.BrushToString(FillColorStdCore1); }
			set { FillColorStdCore1 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="VWAP to Band 1 Opacity", Order=11, GroupName="4. VWAP 2 Fill")]
		public int FillOpacityStdCore1 { get; set; }

		[XmlIgnore]
		[Display(Name="Band 1 to Band 2 Color", Order=20, GroupName="4. VWAP 2 Fill")]
		public Brush FillColorStd12 { get; set; }

		[Browsable(false)]
		public string FillColorStd12Serializable
		{
			get { return Serialize.BrushToString(FillColorStd12); }
			set { FillColorStd12 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Band 1 to Band 2 Opacity", Order=21, GroupName="4. VWAP 2 Fill")]
		public int FillOpacityStd12 { get; set; }


		[XmlIgnore]
		[Display(Name="Band 2 to Band 3 Color", Order=30, GroupName="4. VWAP 2 Fill")]
		public Brush FillColorStd23 { get; set; }

		[Browsable(false)]
		public string FillColorStd23Serializable
		{
			get { return Serialize.BrushToString(FillColorStd23); }
			set { FillColorStd23 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Band 2 to Band 3 Opacity", Order=31, GroupName="4. VWAP 2 Fill")]
		public int FillOpacityStd23 { get; set; }

		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Enable VWAP 3 Fill", Description="Fills the selected deviation zones for VWAP 3.", Order=1, GroupName="5. VWAP 3 Fill")]
		public bool ShowVwap3Fills { get; set; }


		[XmlIgnore]
		[Display(Name="VWAP to Band 1 Color", Order=10, GroupName="5. VWAP 3 Fill")]
		public Brush FillColorHtfCore1 { get; set; }

		[Browsable(false)]
		public string FillColorHtfCore1Serializable
		{
			get { return Serialize.BrushToString(FillColorHtfCore1); }
			set { FillColorHtfCore1 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="VWAP to Band 1 Opacity", Order=11, GroupName="5. VWAP 3 Fill")]
		public int FillOpacityHtfCore1 { get; set; }

		[XmlIgnore]
		[Display(Name="Band 1 to Band 2 Color", Order=20, GroupName="5. VWAP 3 Fill")]
		public Brush FillColorHtf12 { get; set; }

		[Browsable(false)]
		public string FillColorHtf12Serializable
		{
			get { return Serialize.BrushToString(FillColorHtf12); }
			set { FillColorHtf12 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Band 1 to Band 2 Opacity", Order=21, GroupName="5. VWAP 3 Fill")]
		public int FillOpacityHtf12 { get; set; }


		[XmlIgnore]
		[Display(Name="Band 2 to Band 3 Color", Order=30, GroupName="5. VWAP 3 Fill")]
		public Brush FillColorHtf23 { get; set; }

		[Browsable(false)]
		public string FillColorHtf23Serializable
		{
			get { return Serialize.BrushToString(FillColorHtf23); }
			set { FillColorHtf23 = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Band 2 to Band 3 Opacity", Order=31, GroupName="5. VWAP 3 Fill")]
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

	public class OrcaAnchoredVwapTypeConverter : IndicatorBaseConverter
	{
		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			PropertyDescriptorCollection properties = base.GetPropertiesSupported(context)
				? base.GetProperties(context, value, attributes)
				: TypeDescriptor.GetProperties(value, attributes);

			OrcaAnchoredVWAPs indicator = value as OrcaAnchoredVWAPs;
			if (indicator == null || properties == null)
				return properties;

			HashSet<string> hiddenProperties = new HashSet<string>();

			if (indicator.UseAtrReversal)
			{
				hiddenProperties.Add(nameof(indicator.DevelopingTicks));
				hiddenProperties.Add(nameof(indicator.StandardTicks));
				hiddenProperties.Add(nameof(indicator.HtfTicks));
			}
			else
			{
				hiddenProperties.Add(nameof(indicator.AtrPeriod));
				hiddenProperties.Add(nameof(indicator.DevAtrMultiplier));
				hiddenProperties.Add(nameof(indicator.StdAtrMultiplier));
				hiddenProperties.Add(nameof(indicator.HtfAtrMultiplier));
			}

			if (!indicator.ShowVwap1)
				hiddenProperties.Add(nameof(indicator.ShowVwap1Bands));
			if (!indicator.ShowVwap2)
				hiddenProperties.Add(nameof(indicator.ShowStdBands));
			if (!indicator.ShowVwap3)
				hiddenProperties.Add(nameof(indicator.ShowHtfBands));

			bool showAnyBands = (indicator.ShowVwap1 && indicator.ShowVwap1Bands)
				|| (indicator.ShowVwap2 && indicator.ShowStdBands)
				|| (indicator.ShowVwap3 && indicator.ShowHtfBands);

			if (!showAnyBands)
			{
				hiddenProperties.Add(nameof(indicator.ShowAllBands));
				hiddenProperties.Add(nameof(indicator.ShowStdDev1));
				hiddenProperties.Add(nameof(indicator.ShowStdDev2));
				hiddenProperties.Add(nameof(indicator.ShowStdDev3));
				hiddenProperties.Add(nameof(indicator.StdDevMultiplier1));
				hiddenProperties.Add(nameof(indicator.StdDevMultiplier2));
				hiddenProperties.Add(nameof(indicator.StdDevMultiplier3));
			}
			else
			{
				if (!indicator.ShowStdDev1)
					hiddenProperties.Add(nameof(indicator.StdDevMultiplier1));
				if (!indicator.ShowStdDev2)
					hiddenProperties.Add(nameof(indicator.StdDevMultiplier2));
				if (!indicator.ShowStdDev3)
					hiddenProperties.Add(nameof(indicator.StdDevMultiplier3));
			}

			AddHiddenVwap2FillProperties(indicator, hiddenProperties);
			AddHiddenVwap3FillProperties(indicator, hiddenProperties);

			PropertyDescriptorCollection adjusted = new PropertyDescriptorCollection(null);
			foreach (PropertyDescriptor descriptor in properties)
			{
				adjusted.Add(hiddenProperties.Contains(descriptor.Name)
					? new PropertyDescriptorExtended(descriptor, _ => value, null, new Attribute[] { new BrowsableAttribute(false) })
					: descriptor);
			}

			return adjusted;
		}

		private static void AddHiddenVwap2FillProperties(OrcaAnchoredVWAPs indicator, HashSet<string> hiddenProperties)
		{
			bool canShowFill = indicator.ShowVwap2 && indicator.ShowStdBands;
			if (!canShowFill)
				hiddenProperties.Add(nameof(indicator.ShowVwap2Fills));

			if (!canShowFill || !indicator.ShowVwap2Fills || !indicator.ShowStdDev1)
			{
				hiddenProperties.Add(nameof(indicator.FillColorStdCore1));
				hiddenProperties.Add(nameof(indicator.FillOpacityStdCore1));
			}

			if (!canShowFill || !indicator.ShowVwap2Fills || !indicator.ShowStdDev1 || !indicator.ShowStdDev2)
			{
				hiddenProperties.Add(nameof(indicator.FillColorStd12));
				hiddenProperties.Add(nameof(indicator.FillOpacityStd12));
			}

			if (!canShowFill || !indicator.ShowVwap2Fills || !indicator.ShowStdDev2 || !indicator.ShowStdDev3)
			{
				hiddenProperties.Add(nameof(indicator.FillColorStd23));
				hiddenProperties.Add(nameof(indicator.FillOpacityStd23));
			}
		}

		private static void AddHiddenVwap3FillProperties(OrcaAnchoredVWAPs indicator, HashSet<string> hiddenProperties)
		{
			bool canShowFill = indicator.ShowVwap3 && indicator.ShowHtfBands;
			if (!canShowFill)
				hiddenProperties.Add(nameof(indicator.ShowVwap3Fills));

			if (!canShowFill || !indicator.ShowVwap3Fills || !indicator.ShowStdDev1)
			{
				hiddenProperties.Add(nameof(indicator.FillColorHtfCore1));
				hiddenProperties.Add(nameof(indicator.FillOpacityHtfCore1));
			}

			if (!canShowFill || !indicator.ShowVwap3Fills || !indicator.ShowStdDev1 || !indicator.ShowStdDev2)
			{
				hiddenProperties.Add(nameof(indicator.FillColorHtf12));
				hiddenProperties.Add(nameof(indicator.FillOpacityHtf12));
			}

			if (!canShowFill || !indicator.ShowVwap3Fills || !indicator.ShowStdDev2 || !indicator.ShowStdDev3)
			{
				hiddenProperties.Add(nameof(indicator.FillColorHtf23));
				hiddenProperties.Add(nameof(indicator.FillOpacityHtf23));
			}
		}
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
