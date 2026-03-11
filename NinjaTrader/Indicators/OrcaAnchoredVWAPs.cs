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
				Description = "Anchored VWAP indicator with 3 reversal thresholds.";
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

				AddPlot(new Stroke(Brushes.Gray, 2), PlotStyle.Line, "Dev VWAP");
				AddPlot(new Stroke(Brushes.Magenta, 2), PlotStyle.Line, "Std VWAP");
				AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "HTF VWAP");
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

			devTracker.Process(High[0], Low[0], Close[0], Volume[0], CurrentBar);
			stdTracker.Process(High[0], Low[0], Close[0], Volume[0], CurrentBar);
			htfTracker.Process(High[0], Low[0], Close[0], Volume[0], CurrentBar);

			bool drawDev = devTracker.IsActive && (devTracker.ActiveAnchorBar > stdTracker.ActiveAnchorBar);
			bool drawStd = stdTracker.IsActive && (!htfTracker.IsActive || stdTracker.ActiveAnchorBar > htfTracker.ActiveAnchorBar);
			bool drawHtf = htfTracker.IsActive;

			DrawVwap(0, devTracker, drawDev);
			DrawVwap(1, stdTracker, drawStd);
			DrawVwap(2, htfTracker, drawHtf);
		}

		private void DrawVwap(int plotIdx, VwapTracker tracker, bool visible)
		{
			if (!visible || !tracker.IsActive || tracker.ActiveAnchorBar < 0 || CurrentBar < tracker.ActiveAnchorBar)
			{
				Values[plotIdx].Reset();
				return;
			}

			if (tracker.JustReversed)
			{
				// NUCLEAR: Wipe entire plot history clean so no old points exist to connect to
				for (int i = 0; i <= CurrentBar; i++)
					Values[plotIdx].Reset(i);

				// Calculate VWAP from anchor bar forward to current bar
				double cumVol = 0;
				double cumPV = 0;

				for (int barsAgo = CurrentBar - tracker.ActiveAnchorBar; barsAgo >= 0; barsAgo--)
				{
					double tp = (High[barsAgo] + Low[barsAgo] + Close[barsAgo]) / 3.0;
					cumVol += Volume[barsAgo];
					cumPV += tp * Volume[barsAgo];

					if (barsAgo == CurrentBar - tracker.ActiveAnchorBar)
						Values[plotIdx][barsAgo] = tracker.AnchorPrice;
					else if (cumVol > 0)
						Values[plotIdx][barsAgo] = cumPV / cumVol;
				}

				tracker.CumVol = cumVol;
				tracker.CumPV = cumPV;
			}
			else
			{
				// Append current bar to running VWAP
				double tp = (High[0] + Low[0] + Close[0]) / 3.0;
				tracker.CumVol += Volume[0];
				tracker.CumPV += tp * Volume[0];

				if (tracker.CumVol > 0)
					Values[plotIdx][0] = tracker.CumPV / tracker.CumVol;
			}

			// ALWAYS maintain the gap: reset the bar just before the anchor on every single update.
			// This is the key fix — barsAgo shifts by 1 each bar, so we must re-assert the gap every bar.
			int gapBarsAgo = CurrentBar - tracker.ActiveAnchorBar + 1;
			if (gapBarsAgo >= 0 && gapBarsAgo <= CurrentBar)
				Values[plotIdx].Reset(gapBarsAgo);
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

		public double CumVol;
		public double CumPV;

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
				CumVol = 0;
				CumPV = 0;
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
		public OrcaAnchoredVWAPs OrcaAnchoredVWAPs(int developingTicks, int standardTicks, int htfTicks)
		{
			return OrcaAnchoredVWAPs(Input, developingTicks, standardTicks, htfTicks);
		}

		public OrcaAnchoredVWAPs OrcaAnchoredVWAPs(ISeries<double> input, int developingTicks, int standardTicks, int htfTicks)
		{
			if (cacheOrcaAnchoredVWAPs != null)
				for (int idx = 0; idx < cacheOrcaAnchoredVWAPs.Length; idx++)
					if (cacheOrcaAnchoredVWAPs[idx] != null && cacheOrcaAnchoredVWAPs[idx].DevelopingTicks == developingTicks && cacheOrcaAnchoredVWAPs[idx].StandardTicks == standardTicks && cacheOrcaAnchoredVWAPs[idx].HtfTicks == htfTicks && cacheOrcaAnchoredVWAPs[idx].EqualsInput(input))
						return cacheOrcaAnchoredVWAPs[idx];
			return CacheIndicator<OrcaAnchoredVWAPs>(new OrcaAnchoredVWAPs(){ DevelopingTicks = developingTicks, StandardTicks = standardTicks, HtfTicks = htfTicks }, input, ref cacheOrcaAnchoredVWAPs);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaAnchoredVWAPs OrcaAnchoredVWAPs(int developingTicks, int standardTicks, int htfTicks)
		{
			return indicator.OrcaAnchoredVWAPs(Input, developingTicks, standardTicks, htfTicks);
		}

		public Indicators.OrcaAnchoredVWAPs OrcaAnchoredVWAPs(ISeries<double> input , int developingTicks, int standardTicks, int htfTicks)
		{
			return indicator.OrcaAnchoredVWAPs(input, developingTicks, standardTicks, htfTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaAnchoredVWAPs OrcaAnchoredVWAPs(int developingTicks, int standardTicks, int htfTicks)
		{
			return indicator.OrcaAnchoredVWAPs(Input, developingTicks, standardTicks, htfTicks);
		}

		public Indicators.OrcaAnchoredVWAPs OrcaAnchoredVWAPs(ISeries<double> input , int developingTicks, int standardTicks, int htfTicks)
		{
			return indicator.OrcaAnchoredVWAPs(input, developingTicks, standardTicks, htfTicks);
		}
	}
}

#endregion
