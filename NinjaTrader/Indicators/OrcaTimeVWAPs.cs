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

public enum RollingVwapPeriod 
{
	Min15 = 15,
	Min30 = 30,
	Hour1 = 60,
	Hour2 = 120,
	Hour4 = 240,
	Hour8 = 480,
	Day1 = 1440,
	Day5 = 7200,
	Day20 = 28800
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaVwapSession 
	{
		public double SumVol;
		public double SumPriceVol;
		public double SumPrice2Vol;

		public void Add(double price, double vol) 
		{
			SumVol += vol;
			SumPriceVol += price * vol;
			SumPrice2Vol += price * price * vol;
		}

		public void Reset() 
		{
			SumVol = 0;
			SumPriceVol = 0;
			SumPrice2Vol = 0;
		}

		public double Vwap => SumVol > 0 ? SumPriceVol / SumVol : 0;
		public double MathVariance => SumVol > 0 ? Math.Max(0, (SumPrice2Vol / SumVol) - (Vwap * Vwap)) : 0;
		public double StdDev => Math.Sqrt(MathVariance);
	}

	public class OrcaVwapBucket 
	{
		public double SumVol;
		public double SumPriceVol;
		public double SumPrice2Vol;
	}

	public class OrcaTimeVWAPs : Indicator
	{
		private OrcaVwapSession globexSession;
		private OrcaVwapSession rthSession;
		private OrcaVwapSession weeklySession;

		private Queue<OrcaVwapBucket> rollingHistory;
		private OrcaVwapBucket rollingDeveloping;
		private OrcaVwapSession rollingTotal;
		private DateTime currentMinuteToken;

		private double lastBarVolume;
		private int lastBarIndex;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "All-in-one Anchored Time and Rolling VWAP with Deviation bands.";
				Name = "Orca Time VWAPs";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive = true;

				// Globex Defaults
				GlobexShowVWAP = true;
				GlobexStartTime = new TimeSpan(18, 00, 00);
				GlobexShowDev1 = true;
				GlobexDev1Mult = 1.0;
				GlobexShowDev2 = true;
				GlobexDev2Mult = 2.0;
				GlobexShowDev3 = true;
				GlobexDev3Mult = 3.0;
				GlobexFillColorCore = Brushes.DodgerBlue;
				GlobexFillOpacityCore = 0;
				GlobexFillColor12 = Brushes.DodgerBlue;
				GlobexFillOpacity12 = 0;
				GlobexFillColor23 = Brushes.DodgerBlue;
				GlobexFillOpacity23 = 0;

				// Rth Defaults
				RthShowVWAP = true;
				RthStartTime = new TimeSpan(09, 30, 00);
				RthShowDev1 = true;
				RthDev1Mult = 1.0;
				RthShowDev2 = true;
				RthDev2Mult = 2.0;
				RthShowDev3 = true;
				RthDev3Mult = 3.0;
				RthFillColorCore = Brushes.Orange;
				RthFillOpacityCore = 0;
				RthFillColor12 = Brushes.Orange;
				RthFillOpacity12 = 0;
				RthFillColor23 = Brushes.Orange;
				RthFillOpacity23 = 0;

				// === 3. Rolling Defaults ===
				RollingShowVWAP = true;
				RollingPeriod = RollingVwapPeriod.Day1;
				MinutesPerDay = 1380;
				RollingShowDev1 = true;
				RollingDev1Mult = 1.0;
				RollingShowDev2 = true;
				RollingDev2Mult = 2.0;
				RollingShowDev3 = true;
				RollingDev3Mult = 3.0;
				RollingFillColorCore = Brushes.LimeGreen;
				RollingFillOpacityCore = 0;
				RollingFillColor12 = Brushes.LimeGreen;
				RollingFillOpacity12 = 0;
				RollingFillColor23 = Brushes.LimeGreen;
				RollingFillOpacity23 = 0;

				// Weekly Defaults
				WeeklyShowVWAP = true;
				WeeklyStartTime = new TimeSpan(18, 00, 00);
				WeeklyShowDev1 = true;
				WeeklyDev1Mult = 1.0;
				WeeklyShowDev2 = true;
				WeeklyDev2Mult = 2.0;
				WeeklyShowDev3 = true;
				WeeklyDev3Mult = 3.0;
				WeeklyFillColorCore = Brushes.Plum;
				WeeklyFillOpacityCore = 0;
				WeeklyFillColor12 = Brushes.Plum;
				WeeklyFillOpacity12 = 0;
				WeeklyFillColor23 = Brushes.Plum;
				WeeklyFillOpacity23 = 0;

				// === Plots ===
				AddPlot(new Stroke(Brushes.DodgerBlue, DashStyleHelper.Solid, 2), PlotStyle.Line, "Globex VWAP");
				AddPlot(new Stroke(Brushes.DodgerBlue, DashStyleHelper.Dash, 1), PlotStyle.Line, "Globex Dev 1 Upper");
				AddPlot(new Stroke(Brushes.DodgerBlue, DashStyleHelper.Dash, 1), PlotStyle.Line, "Globex Dev 1 Lower");
				AddPlot(new Stroke(Brushes.DodgerBlue, DashStyleHelper.Dot, 1), PlotStyle.Line, "Globex Dev 2 Upper");
				AddPlot(new Stroke(Brushes.DodgerBlue, DashStyleHelper.Dot, 1), PlotStyle.Line, "Globex Dev 2 Lower");
				AddPlot(new Stroke(Brushes.DodgerBlue, DashStyleHelper.DashDot, 1), PlotStyle.Line, "Globex Dev 3 Upper");
				AddPlot(new Stroke(Brushes.DodgerBlue, DashStyleHelper.DashDot, 1), PlotStyle.Line, "Globex Dev 3 Lower");

				AddPlot(new Stroke(Brushes.Orange, DashStyleHelper.Solid, 2), PlotStyle.Line, "Rth VWAP");
				AddPlot(new Stroke(Brushes.Orange, DashStyleHelper.Dash, 1), PlotStyle.Line, "Rth Dev 1 Upper");
				AddPlot(new Stroke(Brushes.Orange, DashStyleHelper.Dash, 1), PlotStyle.Line, "Rth Dev 1 Lower");
				AddPlot(new Stroke(Brushes.Orange, DashStyleHelper.Dot, 1), PlotStyle.Line, "Rth Dev 2 Upper");
				AddPlot(new Stroke(Brushes.Orange, DashStyleHelper.Dot, 1), PlotStyle.Line, "Rth Dev 2 Lower");
				AddPlot(new Stroke(Brushes.Orange, DashStyleHelper.DashDot, 1), PlotStyle.Line, "Rth Dev 3 Upper");
				AddPlot(new Stroke(Brushes.Orange, DashStyleHelper.DashDot, 1), PlotStyle.Line, "Rth Dev 3 Lower");

				AddPlot(new Stroke(Brushes.LimeGreen, DashStyleHelper.Solid, 2), PlotStyle.Line, "Rolling VWAP");
				AddPlot(new Stroke(Brushes.LimeGreen, DashStyleHelper.Dash, 1), PlotStyle.Line, "Rolling Dev 1 Upper");
				AddPlot(new Stroke(Brushes.LimeGreen, DashStyleHelper.Dash, 1), PlotStyle.Line, "Rolling Dev 1 Lower");
				AddPlot(new Stroke(Brushes.LimeGreen, DashStyleHelper.Dot, 1), PlotStyle.Line, "Rolling Dev 2 Upper");
				AddPlot(new Stroke(Brushes.LimeGreen, DashStyleHelper.Dot, 1), PlotStyle.Line, "Rolling Dev 2 Lower");
				AddPlot(new Stroke(Brushes.LimeGreen, DashStyleHelper.DashDot, 1), PlotStyle.Line, "Rolling Dev 3 Upper");
				AddPlot(new Stroke(Brushes.LimeGreen, DashStyleHelper.DashDot, 1), PlotStyle.Line, "Rolling Dev 3 Lower");

				AddPlot(new Stroke(Brushes.Plum, DashStyleHelper.Solid, 2), PlotStyle.Line, "Weekly VWAP");
				AddPlot(new Stroke(Brushes.Plum, DashStyleHelper.Dash, 1), PlotStyle.Line, "Weekly Dev 1 Upper");
				AddPlot(new Stroke(Brushes.Plum, DashStyleHelper.Dash, 1), PlotStyle.Line, "Weekly Dev 1 Lower");
				AddPlot(new Stroke(Brushes.Plum, DashStyleHelper.Dot, 1), PlotStyle.Line, "Weekly Dev 2 Upper");
				AddPlot(new Stroke(Brushes.Plum, DashStyleHelper.Dot, 1), PlotStyle.Line, "Weekly Dev 2 Lower");
				AddPlot(new Stroke(Brushes.Plum, DashStyleHelper.DashDot, 1), PlotStyle.Line, "Weekly Dev 3 Upper");
				AddPlot(new Stroke(Brushes.Plum, DashStyleHelper.DashDot, 1), PlotStyle.Line, "Weekly Dev 3 Lower");
			}
			else if (State == State.Configure)
			{
				globexSession = new OrcaVwapSession();
				rthSession = new OrcaVwapSession();
				weeklySession = new OrcaVwapSession();
				rollingHistory = new Queue<OrcaVwapBucket>();
				rollingDeveloping = new OrcaVwapBucket();
				rollingTotal = new OrcaVwapSession();
				currentMinuteToken = DateTime.MinValue;
				lastBarVolume = 0;
				lastBarIndex = -1;
			}
		}

		private bool CrossedTime(DateTime start, DateTime end, TimeSpan target)
		{
			if (start >= end) return false;
			
			DateTime targetTime;
			if (end.TimeOfDay >= target) 
			{
				targetTime = end.Date + target;
			}
			else 
			{
				targetTime = end.Date.AddDays(-1) + target;
			}
			
			return start < targetTime && end >= targetTime;
		}

		private bool CrossedWeekly(DateTime start, DateTime end, TimeSpan target)
		{
			if (start >= end) return false;
			
			int diff = (int)end.DayOfWeek - (int)DayOfWeek.Sunday;
			if (diff < 0) diff += 7;
			DateTime sunday = end.Date.AddDays(-diff);
			
			if (end.DayOfWeek == DayOfWeek.Sunday && end.TimeOfDay < target)
			{
				sunday = sunday.AddDays(-7);
			}
			
			DateTime targetTime = sunday.Date + target;
			return start < targetTime && end >= targetTime;
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 0) return;

			double currentVolume = Volume[0];
			double tickVol = 0;

			if (CurrentBar != lastBarIndex) 
			{
				tickVol = currentVolume;
			}
			else 
			{
				tickVol = currentVolume - lastBarVolume;
			}
			
			lastBarVolume = currentVolume;
			lastBarIndex = CurrentBar;

			if (tickVol <= 0) return;

			double price = (State == State.Historical) ? Typical[0] : Close[0];
			DateTime time0 = Time[0];
			DateTime time1 = (CurrentBar > 0) ? Time[1] : time0;

			if (GlobexShowVWAP)
			{
				if (CrossedTime(time1, time0, GlobexStartTime)) globexSession.Reset();
				globexSession.Add(price, tickVol);
				if (globexSession.SumVol > 0)
				{
					double vwap = globexSession.Vwap;
					double sd = globexSession.StdDev;
					Values[0][0] = vwap;
					if (GlobexShowDev1) { Values[1][0] = vwap + sd * GlobexDev1Mult; Values[2][0] = vwap - sd * GlobexDev1Mult; }
					if (GlobexShowDev2) { Values[3][0] = vwap + sd * GlobexDev2Mult; Values[4][0] = vwap - sd * GlobexDev2Mult; }
					if (GlobexShowDev3) { Values[5][0] = vwap + sd * GlobexDev3Mult; Values[6][0] = vwap - sd * GlobexDev3Mult; }
					if (GlobexFillOpacityCore > 0) {
						Draw.Region(this, "GlobexR_CoreU", CurrentBar, 0, Values[0], Values[1], null, GlobexFillColorCore, GlobexFillOpacityCore);
						Draw.Region(this, "GlobexR_CoreD", CurrentBar, 0, Values[0], Values[2], null, GlobexFillColorCore, GlobexFillOpacityCore);
					}
					if (GlobexFillOpacity12 > 0) {
						Draw.Region(this, "GlobexR_12U", CurrentBar, 0, Values[1], Values[3], null, GlobexFillColor12, GlobexFillOpacity12);
						Draw.Region(this, "GlobexR_12D", CurrentBar, 0, Values[2], Values[4], null, GlobexFillColor12, GlobexFillOpacity12);
					}
					if (GlobexFillOpacity23 > 0) {
						Draw.Region(this, "GlobexR_23U", CurrentBar, 0, Values[3], Values[5], null, GlobexFillColor23, GlobexFillOpacity23);
						Draw.Region(this, "GlobexR_23D", CurrentBar, 0, Values[4], Values[6], null, GlobexFillColor23, GlobexFillOpacity23);
					}
				}
			}

			if (RthShowVWAP)
			{
				if (CrossedTime(time1, time0, RthStartTime)) rthSession.Reset();
				rthSession.Add(price, tickVol);
				if (rthSession.SumVol > 0)
				{
					double vwap = rthSession.Vwap;
					double sd = rthSession.StdDev;
					Values[7][0] = vwap;
					if (RthShowDev1) { Values[8][0] = vwap + sd * RthDev1Mult; Values[9][0] = vwap - sd * RthDev1Mult; }
					if (RthShowDev2) { Values[10][0] = vwap + sd * RthDev2Mult; Values[11][0] = vwap - sd * RthDev2Mult; }
					if (RthShowDev3) { Values[12][0] = vwap + sd * RthDev3Mult; Values[13][0] = vwap - sd * RthDev3Mult; }
					if (RthFillOpacityCore > 0) {
						Draw.Region(this, "RthR_CoreU", CurrentBar, 0, Values[7], Values[8], null, RthFillColorCore, RthFillOpacityCore);
						Draw.Region(this, "RthR_CoreD", CurrentBar, 0, Values[7], Values[9], null, RthFillColorCore, RthFillOpacityCore);
					}
					if (RthFillOpacity12 > 0) {
						Draw.Region(this, "RthR_12U", CurrentBar, 0, Values[8], Values[10], null, RthFillColor12, RthFillOpacity12);
						Draw.Region(this, "RthR_12D", CurrentBar, 0, Values[9], Values[11], null, RthFillColor12, RthFillOpacity12);
					}
					if (RthFillOpacity23 > 0) {
						Draw.Region(this, "RthR_23U", CurrentBar, 0, Values[10], Values[12], null, RthFillColor23, RthFillOpacity23);
						Draw.Region(this, "RthR_23D", CurrentBar, 0, Values[11], Values[13], null, RthFillColor23, RthFillOpacity23);
					}
				}
			}
			if (WeeklyShowVWAP)
			{
				if (CrossedWeekly(time1, time0, WeeklyStartTime)) weeklySession.Reset();
				weeklySession.Add(price, tickVol);
				if (weeklySession.SumVol > 0)
				{
					double vwap = weeklySession.Vwap;
					double sd = weeklySession.StdDev;
					Values[21][0] = vwap;
					if (WeeklyShowDev1) { Values[22][0] = vwap + sd * WeeklyDev1Mult; Values[23][0] = vwap - sd * WeeklyDev1Mult; }
					if (WeeklyShowDev2) { Values[24][0] = vwap + sd * WeeklyDev2Mult; Values[25][0] = vwap - sd * WeeklyDev2Mult; }
					if (WeeklyShowDev3) { Values[26][0] = vwap + sd * WeeklyDev3Mult; Values[27][0] = vwap - sd * WeeklyDev3Mult; }
					if (WeeklyFillOpacityCore > 0) {
						Draw.Region(this, "WeeklyR_CoreU", CurrentBar, 0, Values[21], Values[22], null, WeeklyFillColorCore, WeeklyFillOpacityCore);
						Draw.Region(this, "WeeklyR_CoreD", CurrentBar, 0, Values[21], Values[23], null, WeeklyFillColorCore, WeeklyFillOpacityCore);
					}
					if (WeeklyFillOpacity12 > 0) {
						Draw.Region(this, "WeeklyR_12U", CurrentBar, 0, Values[22], Values[24], null, WeeklyFillColor12, WeeklyFillOpacity12);
						Draw.Region(this, "WeeklyR_12D", CurrentBar, 0, Values[23], Values[25], null, WeeklyFillColor12, WeeklyFillOpacity12);
					}
					if (WeeklyFillOpacity23 > 0) {
						Draw.Region(this, "WeeklyR_23U", CurrentBar, 0, Values[24], Values[26], null, WeeklyFillColor23, WeeklyFillOpacity23);
						Draw.Region(this, "WeeklyR_23D", CurrentBar, 0, Values[25], Values[27], null, WeeklyFillColor23, WeeklyFillOpacity23);
					}
				}
			}

			if (RollingShowVWAP)
			{
				DateTime minuteToken = new DateTime(time0.Year, time0.Month, time0.Day, time0.Hour, time0.Minute, 0);
				if (minuteToken > currentMinuteToken) 
				{
					if (currentMinuteToken != DateTime.MinValue) 
					{
						rollingHistory.Enqueue(rollingDeveloping);
						
						int maxBuckets;
						if (RollingPeriod == RollingVwapPeriod.Day1) maxBuckets = MinutesPerDay;
						else if (RollingPeriod == RollingVwapPeriod.Day5) maxBuckets = MinutesPerDay * 5;
						else if (RollingPeriod == RollingVwapPeriod.Day20) maxBuckets = MinutesPerDay * 20;
						else maxBuckets = (int)RollingPeriod;
						
						int missedMinutes = (int)(minuteToken - currentMinuteToken).TotalMinutes;

						if (missedMinutes > 1 && missedMinutes <= 720) 
						{
							int emptyBuckets = Math.Min(missedMinutes - 1, maxBuckets);
							for (int i = 0; i < emptyBuckets; i++) 
							{
								rollingHistory.Enqueue(new OrcaVwapBucket());
							}
						}

						rollingDeveloping = new OrcaVwapBucket();
						
						while (rollingHistory.Count >= maxBuckets) rollingHistory.Dequeue();

						rollingTotal.Reset();
						foreach(var b in rollingHistory) 
						{
							rollingTotal.SumVol += b.SumVol;
							rollingTotal.SumPriceVol += b.SumPriceVol;
							rollingTotal.SumPrice2Vol += b.SumPrice2Vol;
						}
					}
					currentMinuteToken = minuteToken;
				}
				else if (minuteToken < currentMinuteToken)
				{
					rollingHistory.Clear();
					rollingDeveloping = new OrcaVwapBucket();
					rollingTotal.Reset();
					currentMinuteToken = minuteToken;
				}

				rollingDeveloping.SumVol += tickVol;
				rollingDeveloping.SumPriceVol += price * tickVol;
				rollingDeveloping.SumPrice2Vol += price * price * tickVol;

				double currentSumVol = rollingTotal.SumVol + rollingDeveloping.SumVol;
				if (currentSumVol > 0) 
				{
					double currentSumPriceVol = rollingTotal.SumPriceVol + rollingDeveloping.SumPriceVol;
					double currentSumPrice2Vol = rollingTotal.SumPrice2Vol + rollingDeveloping.SumPrice2Vol;

					double vwap = currentSumPriceVol / currentSumVol;
					double variance = Math.Max(0, (currentSumPrice2Vol / currentSumVol) - (vwap * vwap));
					double sd = Math.Sqrt(variance);

					Values[14][0] = vwap;
					if (RollingShowDev1) { Values[15][0] = vwap + sd * RollingDev1Mult; Values[16][0] = vwap - sd * RollingDev1Mult; }
					if (RollingShowDev2) { Values[17][0] = vwap + sd * RollingDev2Mult; Values[18][0] = vwap - sd * RollingDev2Mult; }
					if (RollingShowDev3) { Values[19][0] = vwap + sd * RollingDev3Mult; Values[20][0] = vwap - sd * RollingDev3Mult; }

					if (RollingFillOpacityCore > 0) {
						Draw.Region(this, "RollR_CoreU", CurrentBar, 0, Values[14], Values[15], null, RollingFillColorCore, RollingFillOpacityCore);
						Draw.Region(this, "RollR_CoreD", CurrentBar, 0, Values[14], Values[16], null, RollingFillColorCore, RollingFillOpacityCore);
					}
					if (RollingFillOpacity12 > 0) {
						Draw.Region(this, "RollR_12U", CurrentBar, 0, Values[15], Values[17], null, RollingFillColor12, RollingFillOpacity12);
						Draw.Region(this, "RollR_12D", CurrentBar, 0, Values[16], Values[18], null, RollingFillColor12, RollingFillOpacity12);
					}
					if (RollingFillOpacity23 > 0) {
						Draw.Region(this, "RollR_23U", CurrentBar, 0, Values[17], Values[19], null, RollingFillColor23, RollingFillOpacity23);
						Draw.Region(this, "RollR_23D", CurrentBar, 0, Values[18], Values[20], null, RollingFillColor23, RollingFillOpacity23);
					}
				}
			}
		}

		#region Properties

		// ------------------ 1. Globex VWAP ------------------
		[NinjaScriptProperty]
		[Display(Name="1. Show VWAP", Order=1, GroupName="1. Globex VWAP")]
		public bool GlobexShowVWAP { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name="2. Start Time", Order=2, GroupName="1. Globex VWAP")]
		public TimeSpan GlobexStartTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 1", Order=3, GroupName="1. Globex VWAP")]
		public bool GlobexShowDev1 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 1 Multiplier", Order=4, GroupName="1. Globex VWAP")]
		public double GlobexDev1Mult { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 2", Order=5, GroupName="1. Globex VWAP")]
		public bool GlobexShowDev2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 2 Multiplier", Order=6, GroupName="1. Globex VWAP")]
		public double GlobexDev2Mult { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 3", Order=7, GroupName="1. Globex VWAP")]
		public bool GlobexShowDev3 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 3 Multiplier", Order=8, GroupName="1. Globex VWAP")]
		public double GlobexDev3Mult { get; set; }

		[XmlIgnore]
		[Display(Name="Core-Dev1 Fill Color", Order=9, GroupName="1. Globex VWAP")]
		public Brush GlobexFillColorCore { get; set; }
		[Browsable(false)]
		public string GlobexFillColorCoreSerializable
		{
			get { return Serialize.BrushToString(GlobexFillColorCore); }
			set { GlobexFillColorCore = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Core-Dev1 Fill Opacity (0 = Off)", Order=10, GroupName="1. Globex VWAP")]
		public int GlobexFillOpacityCore { get; set; }

		[XmlIgnore]
		[Display(Name="Dev1-Dev2 Fill Color", Order=11, GroupName="1. Globex VWAP")]
		public Brush GlobexFillColor12 { get; set; }
		[Browsable(false)]
		public string GlobexFillColor12Serializable
		{
			get { return Serialize.BrushToString(GlobexFillColor12); }
			set { GlobexFillColor12 = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Dev1-Dev2 Fill Opacity (0 = Off)", Order=12, GroupName="1. Globex VWAP")]
		public int GlobexFillOpacity12 { get; set; }

		[XmlIgnore]
		[Display(Name="Dev2-Dev3 Fill Color", Order=13, GroupName="1. Globex VWAP")]
		public Brush GlobexFillColor23 { get; set; }
		[Browsable(false)]
		public string GlobexFillColor23Serializable
		{
			get { return Serialize.BrushToString(GlobexFillColor23); }
			set { GlobexFillColor23 = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Dev2-Dev3 Fill Opacity (0 = Off)", Order=14, GroupName="1. Globex VWAP")]
		public int GlobexFillOpacity23 { get; set; }


		// ------------------ 2. RTH VWAP ------------------
		[NinjaScriptProperty]
		[Display(Name="1. Show VWAP", Order=1, GroupName="2. RTH VWAP")]
		public bool RthShowVWAP { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name="2. Start Time", Order=2, GroupName="2. RTH VWAP")]
		public TimeSpan RthStartTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 1", Order=3, GroupName="2. RTH VWAP")]
		public bool RthShowDev1 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 1 Multiplier", Order=4, GroupName="2. RTH VWAP")]
		public double RthDev1Mult { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 2", Order=5, GroupName="2. RTH VWAP")]
		public bool RthShowDev2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 2 Multiplier", Order=6, GroupName="2. RTH VWAP")]
		public double RthDev2Mult { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 3", Order=7, GroupName="2. RTH VWAP")]
		public bool RthShowDev3 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 3 Multiplier", Order=8, GroupName="2. RTH VWAP")]
		public double RthDev3Mult { get; set; }

		[XmlIgnore]
		[Display(Name="Core-Dev1 Fill Color", Order=9, GroupName="2. RTH VWAP")]
		public Brush RthFillColorCore { get; set; }
		[Browsable(false)]
		public string RthFillColorCoreSerializable
		{
			get { return Serialize.BrushToString(RthFillColorCore); }
			set { RthFillColorCore = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Core-Dev1 Fill Opacity (0 = Off)", Order=10, GroupName="2. RTH VWAP")]
		public int RthFillOpacityCore { get; set; }

		[XmlIgnore]
		[Display(Name="Dev1-Dev2 Fill Color", Order=11, GroupName="2. RTH VWAP")]
		public Brush RthFillColor12 { get; set; }
		[Browsable(false)]
		public string RthFillColor12Serializable
		{
			get { return Serialize.BrushToString(RthFillColor12); }
			set { RthFillColor12 = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Dev1-Dev2 Fill Opacity (0 = Off)", Order=12, GroupName="2. RTH VWAP")]
		public int RthFillOpacity12 { get; set; }

		[XmlIgnore]
		[Display(Name="Dev2-Dev3 Fill Color", Order=13, GroupName="2. RTH VWAP")]
		public Brush RthFillColor23 { get; set; }
		[Browsable(false)]
		public string RthFillColor23Serializable
		{
			get { return Serialize.BrushToString(RthFillColor23); }
			set { RthFillColor23 = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Dev2-Dev3 Fill Opacity (0 = Off)", Order=14, GroupName="2. RTH VWAP")]
		public int RthFillOpacity23 { get; set; }


		// ------------------ 3. Rolling VWAP ------------------
		[NinjaScriptProperty]
		[Display(Name="1. Show VWAP", Order=1, GroupName="3. Rolling VWAP")]
		public bool RollingShowVWAP { get; set; }

		[NinjaScriptProperty]
		[Display(Name="2. Rolling Period", Order=2, GroupName="3. Rolling VWAP")]
		public RollingVwapPeriod RollingPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, 3000)]
		[Display(Name="3. Minutes In Trading Day", Order=3, GroupName="3. Rolling VWAP", Description="1380 for Globex, 390 for RTH. Used to calculate Day1, Day5, Day20.")]
		public int MinutesPerDay { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 1", Order=4, GroupName="3. Rolling VWAP")]
		public bool RollingShowDev1 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 1 Multiplier", Order=5, GroupName="3. Rolling VWAP")]
		public double RollingDev1Mult { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 2", Order=6, GroupName="3. Rolling VWAP")]
		public bool RollingShowDev2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 2 Multiplier", Order=7, GroupName="3. Rolling VWAP")]
		public double RollingDev2Mult { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 3", Order=8, GroupName="3. Rolling VWAP")]
		public bool RollingShowDev3 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 3 Multiplier", Order=9, GroupName="3. Rolling VWAP")]
		public double RollingDev3Mult { get; set; }

		[XmlIgnore]
		[Display(Name="Core-Dev1 Fill Color", Order=10, GroupName="3. Rolling VWAP")]
		public Brush RollingFillColorCore { get; set; }
		[Browsable(false)]
		public string RollingFillColorCoreSerializable
		{
			get { return Serialize.BrushToString(RollingFillColorCore); }
			set { RollingFillColorCore = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Core-Dev1 Fill Opacity (0 = Off)", Order=11, GroupName="3. Rolling VWAP")]
		public int RollingFillOpacityCore { get; set; }

		[XmlIgnore]
		[Display(Name="Dev1-Dev2 Fill Color", Order=12, GroupName="3. Rolling VWAP")]
		public Brush RollingFillColor12 { get; set; }
		[Browsable(false)]
		public string RollingFillColor12Serializable
		{
			get { return Serialize.BrushToString(RollingFillColor12); }
			set { RollingFillColor12 = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Dev1-Dev2 Fill Opacity (0 = Off)", Order=13, GroupName="3. Rolling VWAP")]
		public int RollingFillOpacity12 { get; set; }

		[XmlIgnore]
		[Display(Name="Dev2-Dev3 Fill Color", Order=14, GroupName="3. Rolling VWAP")]
		public Brush RollingFillColor23 { get; set; }
		[Browsable(false)]
		public string RollingFillColor23Serializable
		{
			get { return Serialize.BrushToString(RollingFillColor23); }
			set { RollingFillColor23 = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Dev2-Dev3 Fill Opacity (0 = Off)", Order=15, GroupName="3. Rolling VWAP")]
		public int RollingFillOpacity23 { get; set; }


		// ------------------ 4. Weekly VWAP ------------------
		[NinjaScriptProperty]
		[Display(Name="1. Show VWAP", Order=1, GroupName="4. Weekly VWAP")]
		public bool WeeklyShowVWAP { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name="2. Start Time", Order=2, GroupName="4. Weekly VWAP")]
		public TimeSpan WeeklyStartTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 1", Order=3, GroupName="4. Weekly VWAP")]
		public bool WeeklyShowDev1 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 1 Multiplier", Order=4, GroupName="4. Weekly VWAP")]
		public double WeeklyDev1Mult { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 2", Order=5, GroupName="4. Weekly VWAP")]
		public bool WeeklyShowDev2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 2 Multiplier", Order=6, GroupName="4. Weekly VWAP")]
		public double WeeklyDev2Mult { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Show Dev 3", Order=7, GroupName="4. Weekly VWAP")]
		public bool WeeklyShowDev3 { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Dev 3 Multiplier", Order=8, GroupName="4. Weekly VWAP")]
		public double WeeklyDev3Mult { get; set; }

		[XmlIgnore]
		[Display(Name="Core-Dev1 Fill Color", Order=9, GroupName="4. Weekly VWAP")]
		public Brush WeeklyFillColorCore { get; set; }
		[Browsable(false)]
		public string WeeklyFillColorCoreSerializable
		{
			get { return Serialize.BrushToString(WeeklyFillColorCore); }
			set { WeeklyFillColorCore = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Core-Dev1 Fill Opacity (0 = Off)", Order=10, GroupName="4. Weekly VWAP")]
		public int WeeklyFillOpacityCore { get; set; }

		[XmlIgnore]
		[Display(Name="Dev1-Dev2 Fill Color", Order=11, GroupName="4. Weekly VWAP")]
		public Brush WeeklyFillColor12 { get; set; }
		[Browsable(false)]
		public string WeeklyFillColor12Serializable
		{
			get { return Serialize.BrushToString(WeeklyFillColor12); }
			set { WeeklyFillColor12 = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Dev1-Dev2 Fill Opacity (0 = Off)", Order=12, GroupName="4. Weekly VWAP")]
		public int WeeklyFillOpacity12 { get; set; }

		[XmlIgnore]
		[Display(Name="Dev2-Dev3 Fill Color", Order=13, GroupName="4. Weekly VWAP")]
		public Brush WeeklyFillColor23 { get; set; }
		[Browsable(false)]
		public string WeeklyFillColor23Serializable
		{
			get { return Serialize.BrushToString(WeeklyFillColor23); }
			set { WeeklyFillColor23 = Serialize.StringToBrush(value); }
		}
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Dev2-Dev3 Fill Opacity (0 = Off)", Order=14, GroupName="4. Weekly VWAP")]
		public int WeeklyFillOpacity23 { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaTimeVWAPs[] cacheOrcaTimeVWAPs;
		public OrcaTimeVWAPs OrcaTimeVWAPs(bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
		{
			return OrcaTimeVWAPs(Input, globexShowVWAP, globexStartTime, globexShowDev1, globexDev1Mult, globexShowDev2, globexDev2Mult, globexShowDev3, globexDev3Mult, globexFillOpacityCore, globexFillOpacity12, globexFillOpacity23, rthShowVWAP, rthStartTime, rthShowDev1, rthDev1Mult, rthShowDev2, rthDev2Mult, rthShowDev3, rthDev3Mult, rthFillOpacityCore, rthFillOpacity12, rthFillOpacity23, rollingShowVWAP, rollingPeriod, minutesPerDay, rollingShowDev1, rollingDev1Mult, rollingShowDev2, rollingDev2Mult, rollingShowDev3, rollingDev3Mult, rollingFillOpacityCore, rollingFillOpacity12, rollingFillOpacity23, weeklyShowVWAP, weeklyStartTime, weeklyShowDev1, weeklyDev1Mult, weeklyShowDev2, weeklyDev2Mult, weeklyShowDev3, weeklyDev3Mult, weeklyFillOpacityCore, weeklyFillOpacity12, weeklyFillOpacity23);
		}

		public OrcaTimeVWAPs OrcaTimeVWAPs(ISeries<double> input, bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
		{
			if (cacheOrcaTimeVWAPs != null)
				for (int idx = 0; idx < cacheOrcaTimeVWAPs.Length; idx++)
					if (cacheOrcaTimeVWAPs[idx] != null && cacheOrcaTimeVWAPs[idx].GlobexShowVWAP == globexShowVWAP && cacheOrcaTimeVWAPs[idx].GlobexStartTime == globexStartTime && cacheOrcaTimeVWAPs[idx].GlobexShowDev1 == globexShowDev1 && cacheOrcaTimeVWAPs[idx].GlobexDev1Mult == globexDev1Mult && cacheOrcaTimeVWAPs[idx].GlobexShowDev2 == globexShowDev2 && cacheOrcaTimeVWAPs[idx].GlobexDev2Mult == globexDev2Mult && cacheOrcaTimeVWAPs[idx].GlobexShowDev3 == globexShowDev3 && cacheOrcaTimeVWAPs[idx].GlobexDev3Mult == globexDev3Mult && cacheOrcaTimeVWAPs[idx].GlobexFillOpacityCore == globexFillOpacityCore && cacheOrcaTimeVWAPs[idx].GlobexFillOpacity12 == globexFillOpacity12 && cacheOrcaTimeVWAPs[idx].GlobexFillOpacity23 == globexFillOpacity23 && cacheOrcaTimeVWAPs[idx].RthShowVWAP == rthShowVWAP && cacheOrcaTimeVWAPs[idx].RthStartTime == rthStartTime && cacheOrcaTimeVWAPs[idx].RthShowDev1 == rthShowDev1 && cacheOrcaTimeVWAPs[idx].RthDev1Mult == rthDev1Mult && cacheOrcaTimeVWAPs[idx].RthShowDev2 == rthShowDev2 && cacheOrcaTimeVWAPs[idx].RthDev2Mult == rthDev2Mult && cacheOrcaTimeVWAPs[idx].RthShowDev3 == rthShowDev3 && cacheOrcaTimeVWAPs[idx].RthDev3Mult == rthDev3Mult && cacheOrcaTimeVWAPs[idx].RthFillOpacityCore == rthFillOpacityCore && cacheOrcaTimeVWAPs[idx].RthFillOpacity12 == rthFillOpacity12 && cacheOrcaTimeVWAPs[idx].RthFillOpacity23 == rthFillOpacity23 && cacheOrcaTimeVWAPs[idx].RollingShowVWAP == rollingShowVWAP && cacheOrcaTimeVWAPs[idx].RollingPeriod == rollingPeriod && cacheOrcaTimeVWAPs[idx].MinutesPerDay == minutesPerDay && cacheOrcaTimeVWAPs[idx].RollingShowDev1 == rollingShowDev1 && cacheOrcaTimeVWAPs[idx].RollingDev1Mult == rollingDev1Mult && cacheOrcaTimeVWAPs[idx].RollingShowDev2 == rollingShowDev2 && cacheOrcaTimeVWAPs[idx].RollingDev2Mult == rollingDev2Mult && cacheOrcaTimeVWAPs[idx].RollingShowDev3 == rollingShowDev3 && cacheOrcaTimeVWAPs[idx].RollingDev3Mult == rollingDev3Mult && cacheOrcaTimeVWAPs[idx].RollingFillOpacityCore == rollingFillOpacityCore && cacheOrcaTimeVWAPs[idx].RollingFillOpacity12 == rollingFillOpacity12 && cacheOrcaTimeVWAPs[idx].RollingFillOpacity23 == rollingFillOpacity23 && cacheOrcaTimeVWAPs[idx].WeeklyShowVWAP == weeklyShowVWAP && cacheOrcaTimeVWAPs[idx].WeeklyStartTime == weeklyStartTime && cacheOrcaTimeVWAPs[idx].WeeklyShowDev1 == weeklyShowDev1 && cacheOrcaTimeVWAPs[idx].WeeklyDev1Mult == weeklyDev1Mult && cacheOrcaTimeVWAPs[idx].WeeklyShowDev2 == weeklyShowDev2 && cacheOrcaTimeVWAPs[idx].WeeklyDev2Mult == weeklyDev2Mult && cacheOrcaTimeVWAPs[idx].WeeklyShowDev3 == weeklyShowDev3 && cacheOrcaTimeVWAPs[idx].WeeklyDev3Mult == weeklyDev3Mult && cacheOrcaTimeVWAPs[idx].WeeklyFillOpacityCore == weeklyFillOpacityCore && cacheOrcaTimeVWAPs[idx].WeeklyFillOpacity12 == weeklyFillOpacity12 && cacheOrcaTimeVWAPs[idx].WeeklyFillOpacity23 == weeklyFillOpacity23 && cacheOrcaTimeVWAPs[idx].EqualsInput(input))
						return cacheOrcaTimeVWAPs[idx];
			return CacheIndicator<OrcaTimeVWAPs>(new OrcaTimeVWAPs(){ GlobexShowVWAP = globexShowVWAP, GlobexStartTime = globexStartTime, GlobexShowDev1 = globexShowDev1, GlobexDev1Mult = globexDev1Mult, GlobexShowDev2 = globexShowDev2, GlobexDev2Mult = globexDev2Mult, GlobexShowDev3 = globexShowDev3, GlobexDev3Mult = globexDev3Mult, GlobexFillOpacityCore = globexFillOpacityCore, GlobexFillOpacity12 = globexFillOpacity12, GlobexFillOpacity23 = globexFillOpacity23, RthShowVWAP = rthShowVWAP, RthStartTime = rthStartTime, RthShowDev1 = rthShowDev1, RthDev1Mult = rthDev1Mult, RthShowDev2 = rthShowDev2, RthDev2Mult = rthDev2Mult, RthShowDev3 = rthShowDev3, RthDev3Mult = rthDev3Mult, RthFillOpacityCore = rthFillOpacityCore, RthFillOpacity12 = rthFillOpacity12, RthFillOpacity23 = rthFillOpacity23, RollingShowVWAP = rollingShowVWAP, RollingPeriod = rollingPeriod, MinutesPerDay = minutesPerDay, RollingShowDev1 = rollingShowDev1, RollingDev1Mult = rollingDev1Mult, RollingShowDev2 = rollingShowDev2, RollingDev2Mult = rollingDev2Mult, RollingShowDev3 = rollingShowDev3, RollingDev3Mult = rollingDev3Mult, RollingFillOpacityCore = rollingFillOpacityCore, RollingFillOpacity12 = rollingFillOpacity12, RollingFillOpacity23 = rollingFillOpacity23, WeeklyShowVWAP = weeklyShowVWAP, WeeklyStartTime = weeklyStartTime, WeeklyShowDev1 = weeklyShowDev1, WeeklyDev1Mult = weeklyDev1Mult, WeeklyShowDev2 = weeklyShowDev2, WeeklyDev2Mult = weeklyDev2Mult, WeeklyShowDev3 = weeklyShowDev3, WeeklyDev3Mult = weeklyDev3Mult, WeeklyFillOpacityCore = weeklyFillOpacityCore, WeeklyFillOpacity12 = weeklyFillOpacity12, WeeklyFillOpacity23 = weeklyFillOpacity23 }, input, ref cacheOrcaTimeVWAPs);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaTimeVWAPs OrcaTimeVWAPs(bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
		{
			return indicator.OrcaTimeVWAPs(Input, globexShowVWAP, globexStartTime, globexShowDev1, globexDev1Mult, globexShowDev2, globexDev2Mult, globexShowDev3, globexDev3Mult, globexFillOpacityCore, globexFillOpacity12, globexFillOpacity23, rthShowVWAP, rthStartTime, rthShowDev1, rthDev1Mult, rthShowDev2, rthDev2Mult, rthShowDev3, rthDev3Mult, rthFillOpacityCore, rthFillOpacity12, rthFillOpacity23, rollingShowVWAP, rollingPeriod, minutesPerDay, rollingShowDev1, rollingDev1Mult, rollingShowDev2, rollingDev2Mult, rollingShowDev3, rollingDev3Mult, rollingFillOpacityCore, rollingFillOpacity12, rollingFillOpacity23, weeklyShowVWAP, weeklyStartTime, weeklyShowDev1, weeklyDev1Mult, weeklyShowDev2, weeklyDev2Mult, weeklyShowDev3, weeklyDev3Mult, weeklyFillOpacityCore, weeklyFillOpacity12, weeklyFillOpacity23);
		}

		public Indicators.OrcaTimeVWAPs OrcaTimeVWAPs(ISeries<double> input , bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
		{
			return indicator.OrcaTimeVWAPs(input, globexShowVWAP, globexStartTime, globexShowDev1, globexDev1Mult, globexShowDev2, globexDev2Mult, globexShowDev3, globexDev3Mult, globexFillOpacityCore, globexFillOpacity12, globexFillOpacity23, rthShowVWAP, rthStartTime, rthShowDev1, rthDev1Mult, rthShowDev2, rthDev2Mult, rthShowDev3, rthDev3Mult, rthFillOpacityCore, rthFillOpacity12, rthFillOpacity23, rollingShowVWAP, rollingPeriod, minutesPerDay, rollingShowDev1, rollingDev1Mult, rollingShowDev2, rollingDev2Mult, rollingShowDev3, rollingDev3Mult, rollingFillOpacityCore, rollingFillOpacity12, rollingFillOpacity23, weeklyShowVWAP, weeklyStartTime, weeklyShowDev1, weeklyDev1Mult, weeklyShowDev2, weeklyDev2Mult, weeklyShowDev3, weeklyDev3Mult, weeklyFillOpacityCore, weeklyFillOpacity12, weeklyFillOpacity23);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaTimeVWAPs OrcaTimeVWAPs(bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
		{
			return indicator.OrcaTimeVWAPs(Input, globexShowVWAP, globexStartTime, globexShowDev1, globexDev1Mult, globexShowDev2, globexDev2Mult, globexShowDev3, globexDev3Mult, globexFillOpacityCore, globexFillOpacity12, globexFillOpacity23, rthShowVWAP, rthStartTime, rthShowDev1, rthDev1Mult, rthShowDev2, rthDev2Mult, rthShowDev3, rthDev3Mult, rthFillOpacityCore, rthFillOpacity12, rthFillOpacity23, rollingShowVWAP, rollingPeriod, minutesPerDay, rollingShowDev1, rollingDev1Mult, rollingShowDev2, rollingDev2Mult, rollingShowDev3, rollingDev3Mult, rollingFillOpacityCore, rollingFillOpacity12, rollingFillOpacity23, weeklyShowVWAP, weeklyStartTime, weeklyShowDev1, weeklyDev1Mult, weeklyShowDev2, weeklyDev2Mult, weeklyShowDev3, weeklyDev3Mult, weeklyFillOpacityCore, weeklyFillOpacity12, weeklyFillOpacity23);
		}

		public Indicators.OrcaTimeVWAPs OrcaTimeVWAPs(ISeries<double> input , bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
		{
			return indicator.OrcaTimeVWAPs(input, globexShowVWAP, globexStartTime, globexShowDev1, globexDev1Mult, globexShowDev2, globexDev2Mult, globexShowDev3, globexDev3Mult, globexFillOpacityCore, globexFillOpacity12, globexFillOpacity23, rthShowVWAP, rthStartTime, rthShowDev1, rthDev1Mult, rthShowDev2, rthDev2Mult, rthShowDev3, rthDev3Mult, rthFillOpacityCore, rthFillOpacity12, rthFillOpacity23, rollingShowVWAP, rollingPeriod, minutesPerDay, rollingShowDev1, rollingDev1Mult, rollingShowDev2, rollingDev2Mult, rollingShowDev3, rollingDev3Mult, rollingFillOpacityCore, rollingFillOpacity12, rollingFillOpacity23, weeklyShowVWAP, weeklyStartTime, weeklyShowDev1, weeklyDev1Mult, weeklyShowDev2, weeklyDev2Mult, weeklyShowDev3, weeklyDev3Mult, weeklyFillOpacityCore, weeklyFillOpacity12, weeklyFillOpacity23);
		}
	}
}

#endregion
