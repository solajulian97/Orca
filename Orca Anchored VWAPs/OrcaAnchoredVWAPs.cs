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
		private VwapTracker savedDevTracker;
		private VwapTracker savedStdTracker;
		private VwapTracker savedHtfTracker;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Efficient Anchored VWAP indicator calculating 3 simultaneous anchors based on reversal thresholds.";
				Name = "Orca Anchored VWAPs";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive = true;

				DevelopingTicks = 10;
				StandardTicks = 20;
				HtfTicks = 40;

				AddPlot(new Stroke(Brushes.Gray, 2), PlotStyle.Line, "Dev VWAP");
				AddPlot(new Stroke(Brushes.Magenta, 2), PlotStyle.Line, "Std VWAP");
				AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "HTF VWAP");
			}
			else if (State == State.Configure)
			{
				savedDevTracker = new VwapTracker(TickSize, DevelopingTicks);
				savedStdTracker = new VwapTracker(TickSize, StandardTicks);
				savedHtfTracker = new VwapTracker(TickSize, HtfTicks);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 0) return;

			if (IsFirstTickOfBar)
			{
				if (CurrentBar > 0)
				{
					savedDevTracker.Process(Open[1], High[1], Low[1], Close[1], Volume[1]);
					savedStdTracker.Process(Open[1], High[1], Low[1], Close[1], Volume[1]);
					savedHtfTracker.Process(Open[1], High[1], Low[1], Close[1], Volume[1]);
				}
			}

			VwapTracker liveDev = savedDevTracker.Clone();
			liveDev.Process(Open[0], High[0], Low[0], Close[0], Volume[0]);
			Values[0][0] = liveDev.Value;

			VwapTracker liveStd = savedStdTracker.Clone();
			liveStd.Process(Open[0], High[0], Low[0], Close[0], Volume[0]);
			Values[1][0] = liveStd.Value;

			VwapTracker liveHtf = savedHtfTracker.Clone();
			liveHtf.Process(Open[0], High[0], Low[0], Close[0], Volume[0]);
			Values[2][0] = liveHtf.Value;
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
		
		public int Direction = 0; // 1 for Up, -1 for Down
		public double ExtremePrice;
		
		public double ConfirmedVol;
		public double ConfirmedPriceVol;
		
		public double PendingVol;
		public double PendingPriceVol;
		
		public double Value;
		
		public VwapTracker(double tickSize, double reversalTicks)
		{
			this.tickSize = tickSize;
			this.ReversalTicks = reversalTicks;
			Direction = 0;
		}
		
		public VwapTracker Clone()
		{
			return (VwapTracker)this.MemberwiseClone();
		}
		
		public void Process(double open, double high, double low, double close, double volume)
		{
			double typicalPrice = (high + low + close) / 3.0;
			
			if (Direction == 0)
			{
				Direction = 1;
				ExtremePrice = high;
				ConfirmedVol = 0;
				ConfirmedPriceVol = 0;
				PendingVol = volume;
				PendingPriceVol = typicalPrice * volume;
				Value = typicalPrice;
				return;
			}

			bool extendsTrend = (Direction == 1 && high >= ExtremePrice) || (Direction == -1 && low <= ExtremePrice);
			bool reversesTrend = (Direction == 1 && (ExtremePrice - low) / tickSize >= ReversalTicks) ||
								 (Direction == -1 && (high - ExtremePrice) / tickSize >= ReversalTicks);
								 
			if (extendsTrend && reversesTrend)
			{
				if (Direction == 1)
				{
					// New high first
					ExtremePrice = high;
					ConfirmedVol += PendingVol;
					ConfirmedPriceVol += PendingPriceVol;
					PendingVol = volume;
					PendingPriceVol = typicalPrice * volume;
					
					// Then reverse
					Direction = -1;
					ExtremePrice = low;
					ConfirmedVol = PendingVol;
					ConfirmedPriceVol = PendingPriceVol;
					PendingVol = 0;
					PendingPriceVol = 0;
				}
				else
				{
					ExtremePrice = low;
					ConfirmedVol += PendingVol;
					ConfirmedPriceVol += PendingPriceVol;
					PendingVol = volume;
					PendingPriceVol = typicalPrice * volume;
					
					Direction = 1;
					ExtremePrice = high;
					ConfirmedVol = PendingVol;
					ConfirmedPriceVol = PendingPriceVol;
					PendingVol = 0;
					PendingPriceVol = 0;
				}
			}
			else if (extendsTrend)
			{
				ExtremePrice = (Direction == 1) ? high : low;
				ConfirmedVol += PendingVol;
				ConfirmedPriceVol += PendingPriceVol;
				PendingVol = volume;
				PendingPriceVol = typicalPrice * volume;
			}
			else if (reversesTrend)
			{
				 Direction = (Direction == 1) ? -1 : 1;
				 ExtremePrice = (Direction == 1) ? high : low;
				 
				 ConfirmedVol = PendingVol;
				 ConfirmedPriceVol = PendingPriceVol;
				 PendingVol = volume;
				 PendingPriceVol = typicalPrice * volume;
			}
			else
			{
				PendingVol += volume;
				PendingPriceVol += typicalPrice * volume;
			}
			
			double totalVol = ConfirmedVol + PendingVol;
			if (totalVol > 0)
			{
				Value = (ConfirmedPriceVol + PendingPriceVol) / totalVol;
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
