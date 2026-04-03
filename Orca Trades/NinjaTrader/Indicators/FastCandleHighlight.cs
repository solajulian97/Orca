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

public enum HighlightingMode
{
	FixedSeconds,
	DynamicAverage
}

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class FastCandleHighlight : Indicator
	{
		private Series<double> barDurations;
		private SMA smaBarDuration;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Highlights candles that complete under a certain threshold.";
				Name										= "FastCandleHighlight";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
				
				HighlightColor								= Brushes.Yellow;
				Mode										= HighlightingMode.FixedSeconds;
				MaxSeconds									= 10;
				AveragePeriod								= 20;
				PercentageThreshold							= 50;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				barDurations = new Series<double>(this);
				smaBarDuration = SMA(barDurations, AveragePeriod);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1)
				return;
				
			TimeSpan durationSpan = Time[0] - Time[1];
			double durationSeconds = durationSpan.TotalSeconds;

			// Store the duration for the SMA calculation
			barDurations[0] = durationSeconds;

			if (Mode == HighlightingMode.FixedSeconds)
			{
				if (durationSeconds <= MaxSeconds)
				{
					BarBrush = HighlightColor;
				}
			}
			else if (Mode == HighlightingMode.DynamicAverage)
			{
				// Need enough bars to calculate the average
				if (CurrentBar < AveragePeriod)
					return;

				// We compare against the average of the PREVIOUS bars (smaBarDuration[1]) 
				// to avoid the current unusually fast bar dragging the average down.
				double averageDuration = smaBarDuration[1];
				double thresholdSeconds = averageDuration * (PercentageThreshold / 100.0);

				if (durationSeconds <= thresholdSeconds)
				{
					BarBrush = HighlightColor;
				}
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="Calculation Mode", Description="Mode to determine fast candles", Order=1, GroupName="Parameters")]
		public HighlightingMode Mode
		{ get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Highlight Color", Description="Color to highlight fast candles", Order=2, GroupName="Parameters")]
		public Brush HighlightColor
		{ get; set; }

		[Browsable(false)]
		public string HighlightColorSerializable
		{
			get { return Serialize.BrushToString(HighlightColor); }
			set { HighlightColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Max Seconds (Fixed Mode)", Description="Maximum seconds for a candle to be highlighted in Fixed Seconds mode", Order=3, GroupName="Parameters")]
		public int MaxSeconds
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Average Period (Dynamic Mode)", Description="Number of bars for the moving average in Dynamic Average mode", Order=4, GroupName="Parameters")]
		public int AveragePeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name="Percentage Threshold (Dynamic)", Description="Percentage of the average duration under which a candle is highlighted (e.g., 50 means < 50% of the average time)", Order=5, GroupName="Parameters")]
		public int PercentageThreshold
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FastCandleHighlight[] cacheFastCandleHighlight;
		public FastCandleHighlight FastCandleHighlight(HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
		{
			return FastCandleHighlight(Input, mode, highlightColor, maxSeconds, averagePeriod, percentageThreshold);
		}

		public FastCandleHighlight FastCandleHighlight(ISeries<double> input, HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
		{
			if (cacheFastCandleHighlight != null)
				for (int idx = 0; idx < cacheFastCandleHighlight.Length; idx++)
					if (cacheFastCandleHighlight[idx] != null && cacheFastCandleHighlight[idx].Mode == mode && cacheFastCandleHighlight[idx].HighlightColor == highlightColor && cacheFastCandleHighlight[idx].MaxSeconds == maxSeconds && cacheFastCandleHighlight[idx].AveragePeriod == averagePeriod && cacheFastCandleHighlight[idx].PercentageThreshold == percentageThreshold && cacheFastCandleHighlight[idx].EqualsInput(input))
						return cacheFastCandleHighlight[idx];
			return CacheIndicator<FastCandleHighlight>(new FastCandleHighlight(){ Mode = mode, HighlightColor = highlightColor, MaxSeconds = maxSeconds, AveragePeriod = averagePeriod, PercentageThreshold = percentageThreshold }, input, ref cacheFastCandleHighlight);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FastCandleHighlight FastCandleHighlight(HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
		{
			return indicator.FastCandleHighlight(Input, mode, highlightColor, maxSeconds, averagePeriod, percentageThreshold);
		}

		public Indicators.FastCandleHighlight FastCandleHighlight(ISeries<double> input , HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
		{
			return indicator.FastCandleHighlight(input, mode, highlightColor, maxSeconds, averagePeriod, percentageThreshold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FastCandleHighlight FastCandleHighlight(HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
		{
			return indicator.FastCandleHighlight(Input, mode, highlightColor, maxSeconds, averagePeriod, percentageThreshold);
		}

		public Indicators.FastCandleHighlight FastCandleHighlight(ISeries<double> input , HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
		{
			return indicator.FastCandleHighlight(input, mode, highlightColor, maxSeconds, averagePeriod, percentageThreshold);
		}
	}
}

#endregion
