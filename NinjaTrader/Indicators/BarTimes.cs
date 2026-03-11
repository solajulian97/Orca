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

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class BarTimes : Indicator
	{
		private DateTime temp;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Provides bar times in milliseconds or seconds or minutes or hours,  displayed as bar, intended for non time based bar types.";
				Name										= "BarTimes";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AddPlot(new Stroke(Brushes.OrangeRed, 2), PlotStyle.Bar, "BarTime");
				AddLine(new Stroke(Brushes.Gray, 1f),  0.0,"zeroLine");				
				TimeUnits									= CustomBsEnumNamespace.TimeSelector.Seconds;
			}
		}
		
		public override string DisplayName
		{
			get {return Name +" in: "+TimeUnits;}
		}		

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1)
				return;
			
			if (Bars.IsFirstBarOfSession)
			{
				if (IsFirstTickOfBar)
				{
					temp = Time[0];
				}
				else
				{
					switch (TimeUnits)
					{
						case CustomBsEnumNamespace.TimeSelector.Milliseconds:
						{
							BarTime[0] = (Time[0] - temp).TotalMilliseconds;
							break;
						}
						case CustomBsEnumNamespace.TimeSelector.Seconds:
						{
							BarTime[0] = (Time[0] - temp).TotalSeconds;
							break;
						}
						case CustomBsEnumNamespace.TimeSelector.Minutes:
						{
							BarTime[0] = (Time[0] - temp).TotalMinutes;
							break;
						}
						case CustomBsEnumNamespace.TimeSelector.Hours:
						{
							BarTime[0] = (Time[0] - temp).TotalHours;
							break;
						}
					}
				}
			}
			
			else
			{
				switch (TimeUnits)
				{
					case CustomBsEnumNamespace.TimeSelector.Milliseconds:
					{
						BarTime[0] = (Time[0] - Time[1]).TotalMilliseconds;
						break;
					}
					case CustomBsEnumNamespace.TimeSelector.Seconds:
					{
						BarTime[0] = (Time[0] - Time[1]).TotalSeconds;
						break;
					}
					case CustomBsEnumNamespace.TimeSelector.Minutes:
					{
						BarTime[0] = (Time[0] - Time[1]).TotalMinutes;
						break;
					}
					case CustomBsEnumNamespace.TimeSelector.Hours:
					{
						BarTime[0] = (Time[0] - Time[1]).TotalHours;
						break;
					}
				}
			}
		}

		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> BarTime
		{
			get { return Values[0]; }
		}
		
		// Creates the user definable parameter for the moving average type.
		[NinjaScriptProperty]
		[Display(GroupName = "Time Selections", Description="Choose how to display time")]
		public CustomBsEnumNamespace.TimeSelector TimeUnits
		{ get; set; }		
		
		#endregion

	}
}

namespace CustomBsEnumNamespace
{
	public enum TimeSelector
	{
		Milliseconds,
		Seconds,
		Minutes,
		Hours,
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BarTimes[] cacheBarTimes;
		public BarTimes BarTimes(CustomBsEnumNamespace.TimeSelector timeUnits)
		{
			return BarTimes(Input, timeUnits);
		}

		public BarTimes BarTimes(ISeries<double> input, CustomBsEnumNamespace.TimeSelector timeUnits)
		{
			if (cacheBarTimes != null)
				for (int idx = 0; idx < cacheBarTimes.Length; idx++)
					if (cacheBarTimes[idx] != null && cacheBarTimes[idx].TimeUnits == timeUnits && cacheBarTimes[idx].EqualsInput(input))
						return cacheBarTimes[idx];
			return CacheIndicator<BarTimes>(new BarTimes(){ TimeUnits = timeUnits }, input, ref cacheBarTimes);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BarTimes BarTimes(CustomBsEnumNamespace.TimeSelector timeUnits)
		{
			return indicator.BarTimes(Input, timeUnits);
		}

		public Indicators.BarTimes BarTimes(ISeries<double> input , CustomBsEnumNamespace.TimeSelector timeUnits)
		{
			return indicator.BarTimes(input, timeUnits);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BarTimes BarTimes(CustomBsEnumNamespace.TimeSelector timeUnits)
		{
			return indicator.BarTimes(Input, timeUnits);
		}

		public Indicators.BarTimes BarTimes(ISeries<double> input , CustomBsEnumNamespace.TimeSelector timeUnits)
		{
			return indicator.BarTimes(input, timeUnits);
		}
	}
}

#endregion
