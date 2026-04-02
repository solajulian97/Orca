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
	public class PAX30OpeningRange : Indicator
	{
		private const int DAYS_TO_DISPLAY = 8;
		private DateTime cutoffStartDate = DateTime.MinValue;
		private const int EXTRA_DAYS = 2;
		
		private double orbHigh;
		private double orbLow;
		private double orbMid;
		private int ORBSeconds;
		private bool inOrbPeriod = false;
		private DateTime currentOrbDate = DateTime.MinValue;
		private DateTime lastProcessTime = DateTime.MinValue;
		private DateTime lastOrDate = DateTime.MinValue;

		private Dictionary<DateTime, Dictionary<int, DateTime>> upperLevelStartTimes = new Dictionary<DateTime, Dictionary<int, DateTime>>();
		private Dictionary<DateTime, Dictionary<int, DateTime>> lowerLevelStartTimes = new Dictionary<DateTime, Dictionary<int, DateTime>>();
		private DateTime realtimeOrbDate = DateTime.MinValue;
		private Dictionary<string, DateTime> activeLabels = new Dictionary<string, DateTime>();
		
		private struct OrbData
		{
			public double High;
			public double Low;
			public double Mid;
			public bool IsToday;
			public OrbData(double high, double low, double mid, bool isToday) { High = high; Low = low; Mid = mid; IsToday = isToday; }
		}
		
		private Dictionary<DateTime, OrbData> orbValues = new Dictionary<DateTime, OrbData>();
		private Dictionary<DateTime, List<double>> upperLevels = new Dictionary<DateTime, List<double>>();
		private Dictionary<DateTime, List<double>> lowerLevels = new Dictionary<DateTime, List<double>>();
		private HashSet<string> drawnLevels = new HashSet<string>();
		private bool isValidTimeframe = true;
		
		[XmlIgnore]
		private SimpleFont cachedFont;
		private DateTime lastCleanupTime = DateTime.MinValue;

		[XmlIgnore]
		[Browsable(false)]
		public TimeSpan ORBStart { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ORB LocaL Start Time", Order = 2, GroupName = "ORB Parameters")]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditor")]
		[XmlElement("ORBStart")]
		public string ORBStartSerialize
		{
			get { return ORBStart.ToString(); }
			set { ORBStart = TimeSpan.Parse(value); }
		}

		[XmlIgnore]
		[Browsable(false)]
		public TimeSpan ORBEndPlot { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "ORB LocaL Line End Time", Order = 5, GroupName = "ORB Parameters")]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditor")]
		[XmlElement("ORBEndPlot")]
		public string ORBEndPlotSerialize
		{
			get { return ORBEndPlot.ToString(); }
			set { ORBEndPlot = TimeSpan.Parse(value); }
		}
		
		[NinjaScriptProperty]
		[Display(Name = "Text Vert Offset ", Order = 7, GroupName = "xyDisplay Settings")]
		[Range(-50, 50)]
		public int TextvertPixels  { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Text Horz Offset", Order = 8, GroupName = "xyDisplay Settings")]
		[Range(-100, 100)]
		public int TextHorzOffset { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Font Size", Order = 9, GroupName = "xyDisplay Settings")]
		[Range(6, 36)]
		public int FontSize { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Bold Font", Order = 10, GroupName = "xyDisplay Settings")]
		public bool BoldFont { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Price Label Prefix", Order = 11, GroupName = "xyDisplay Parameters")]
		public string LabelPrefix { get; set; }
		
		[XmlIgnore] [NinjaScriptProperty] [Display(Name = "High Line Color", Order = 12, GroupName = "xyORB Colors")] public Brush HighLineColor { get; set; }
		[Browsable(false)] [XmlElement("HighLineColorSerializable")] public string HighLineColorSerializable { get { return Serialize.BrushToString(HighLineColor); } set { HighLineColor = Serialize.StringToBrush(value); } }
		
		[XmlIgnore] [NinjaScriptProperty] [Display(Name = "Low Line Color", Order = 13, GroupName = "xyORB Colors")] public Brush LowLineColor { get; set; }
		[Browsable(false)] [XmlElement("LowLineColorSerializable")] public string LowLineColorSerializable { get { return Serialize.BrushToString(LowLineColor); } set { LowLineColor = Serialize.StringToBrush(value); } }
		
		[XmlIgnore] [NinjaScriptProperty] [Display(Name = "Mid Line Color", Order = 14, GroupName = "xyORB Colors")] public Brush MidLineColor { get; set; }
		[Browsable(false)] [XmlElement("MidLineColorSerializable")] public string MidLineColorSerializable { get { return Serialize.BrushToString(MidLineColor); } set { MidLineColor = Serialize.StringToBrush(value); } }
		
		[NinjaScriptProperty] [Display(Name = "High/Low Line Width", Order = 15, GroupName = "xyDisplay Parameters")] [Range(1, 10)] public int MainLineWidth { get; set; }
		[NinjaScriptProperty] [Display(Name = "Mid Line Width", Order = 16, GroupName = "xyDisplay Parameters")] [Range(1, 10)] public int MidLineWidth { get; set; }
		[NinjaScriptProperty] [Display(Name = "Levels Line Width", Order = 17, GroupName = "xyDisplay Parameters")] [Range(1, 10)] public int LevelsLineWidth { get; set; }
		[NinjaScriptProperty] [Display(Name = "Show Mid Line", Order = 18, GroupName = "xyDisplay Parameters")] public bool ShowMid { get; set; }

		private double GetMarketLevelFactor()
		{
			if (Instrument == null || Instrument.MasterInstrument == null) return 0;
			string sym = Instrument.MasterInstrument.Name.ToUpper();
			if (sym.Contains("ES") || sym.Contains("MES")) return 15;
			else if (sym.Contains("NQ") || sym.Contains("MNQ")) return 65;
			return 0;
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Multi-timeframe 30 second ORB with dynamic levels. Symbol-specific level points.";
				Name = "PAX30OpeningRange";
				IsOverlay = true;
				Calculate = Calculate.OnBarClose;
				ORBStart = new TimeSpan(9, 30, 0);
				ORBSeconds = 30;
				ORBEndPlot = new TimeSpan(17, 0, 0);
				TextvertPixels = 17;
				TextHorzOffset = 5;
				FontSize = 13;
				BoldFont = false;
				LabelPrefix = "PAXOR";
				HighLineColor = Brushes.DeepSkyBlue;
				LowLineColor = Brushes.OrangeRed;
				MidLineColor = Brushes.Gold;
				MainLineWidth = 3;
				MidLineWidth = 2;
				LevelsLineWidth = 3;
				ShowMid = false;
			}
			else if (State == State.Configure)
			{
				if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute && BarsPeriod.BarsPeriodType != BarsPeriodType.Second) { isValidTimeframe = false; return; }
				AddDataSeries(BarsPeriodType.Second, 30);
			}
			else if (State == State.DataLoaded)
			{
				if (activeLabels == null) activeLabels = new Dictionary<string, DateTime>();
				if (orbValues == null) orbValues = new Dictionary<DateTime, OrbData>();
				if (upperLevels == null) upperLevels = new Dictionary<DateTime, List<double>>();
				if (lowerLevels == null) lowerLevels = new Dictionary<DateTime, List<double>>();
				if (drawnLevels == null) drawnLevels = new HashSet<string>();
				cachedFont = new SimpleFont("Arial", FontSize) { Bold = BoldFont };
				if (!isValidTimeframe)
				{
					Draw.TextFixed(this, "PAXORBWarning", " PAX30OR only supports Minute and Second charts ", TextPosition.Center, Brushes.White, new SimpleFont("Arial", 16) { Bold = true }, Brushes.Transparent, Brushes.DimGray, 100);
				}
				DateTime anchor = BarsArray[0].GetTime(BarsArray[0].Count - 1).Date;
				cutoffStartDate = anchor.AddDays(-(DAYS_TO_DISPLAY + EXTRA_DAYS - 1));
			}
			else if (State == State.Terminated)
			{
				if (activeLabels != null) activeLabels.Clear();
				if (orbValues != null) orbValues.Clear();
				if (upperLevels != null) { foreach (var l in upperLevels.Values) l.Clear(); upperLevels.Clear(); }
				if (lowerLevels != null) { foreach (var l in lowerLevels.Values) l.Clear(); lowerLevels.Clear(); }
				if (drawnLevels != null) drawnLevels.Clear();
			}
		}

		private double RoundToNearestTick(double value) { if (double.IsNaN(value) || double.IsInfinity(value)) return double.NaN; return Math.Round(value / TickSize) * TickSize; }

		private DateTime GetLabelTimeWithOffset(DateTime baseTime, bool isRealtime)
		{
			if (TextHorzOffset == 0) return baseTime;
			try {
				var chartPeriod = (BarsArray != null && BarsArray.Length > 0 && BarsArray[0] != null) ? BarsArray[0].BarsPeriod : BarsPeriod;
				TimeSpan barInterval = TimeSpan.Zero;
				if (chartPeriod.BarsPeriodType == BarsPeriodType.Minute) barInterval = TimeSpan.FromMinutes(chartPeriod.Value * TextHorzOffset);
				else if (chartPeriod.BarsPeriodType == BarsPeriodType.Second) barInterval = TimeSpan.FromSeconds(chartPeriod.Value * TextHorzOffset);
				else return baseTime;
				return baseTime.Add(barInterval);
			} catch { return baseTime; }
		}

		protected override void OnBarUpdate()
		{
			if (!isValidTimeframe || CurrentBar < 1) return;
			if (BarsInProgress == 0) { if (IsFirstTickOfBar && activeLabels.Count > 0) { MoveActiveLabels(); if (orbValues.ContainsKey(realtimeOrbDate)) DrawOrbForDay(realtimeOrbDate, true); } return; }
			if (BarsInProgress != 1) return;
			DateTime currentTime = Time[0];
			if (currentTime == lastProcessTime) return;
			lastProcessTime = currentTime;
			DateTime currentDate = currentTime.Date;
			if (currentDate < cutoffStartDate) return;
			TimeSpan tOfDay = currentTime.TimeOfDay;
			TimeSpan orbEnd = ORBStart.Add(TimeSpan.FromSeconds(30));
			bool isOrbBar = (tOfDay == orbEnd);

			if (isOrbBar && !orbValues.ContainsKey(currentDate))
			{
				currentOrbDate = currentDate; orbHigh = High[0]; orbLow = Low[0]; orbMid = RoundToNearestTick(orbLow + ((orbHigh - orbLow) * 0.5));
				bool isToday = (currentDate == NinjaTrader.Core.Globals.Now.Date);
				orbValues[currentDate] = new OrbData(orbHigh, orbLow, orbMid, isToday);
				upperLevels[currentDate] = new List<double>(); lowerLevels[currentDate] = new List<double>();
				double factor = GetMarketLevelFactor();
				if (factor > 0) { upperLevels[currentDate].Add(RoundToNearestTick(orbHigh + factor)); lowerLevels[currentDate].Add(RoundToNearestTick(orbLow - factor)); }
				DrawOrbForDay(currentDate, isToday);
				if (isToday) realtimeOrbDate = currentDate;
			}
			else if (currentDate != currentOrbDate && !orbValues.ContainsKey(currentDate))
			{
				if (tOfDay > orbEnd && tOfDay <= ORBEndPlot)
				{
					currentOrbDate = currentDate; orbHigh = High[0]; orbLow = Low[0]; orbMid = RoundToNearestTick(orbLow + ((orbHigh - orbLow) * 0.5));
					bool isToday = (currentDate == NinjaTrader.Core.Globals.Now.Date);
					orbValues[currentDate] = new OrbData(orbHigh, orbLow, orbMid, isToday);
					upperLevels[currentDate] = new List<double>(); lowerLevels[currentDate] = new List<double>();
					double factor = GetMarketLevelFactor();
					if (factor > 0) { upperLevels[currentDate].Add(RoundToNearestTick(orbHigh + factor)); lowerLevels[currentDate].Add(RoundToNearestTick(orbLow - factor)); }
					DrawOrbForDay(currentDate, isToday);
					if (isToday) realtimeOrbDate = currentDate;
				}
			}

			if (orbValues.ContainsKey(currentDate) && tOfDay > orbEnd && tOfDay <= ORBEndPlot) CheckAndAddDynamicLevels(currentDate, currentTime);
			if (currentTime.Subtract(lastCleanupTime).TotalHours >= 1) { CleanupOldData(currentDate); lastCleanupTime = currentTime; }
		}

		private void DrawOrbForDay(DateTime orbDate, bool isRealtime)
		{
			if (!orbValues.ContainsKey(orbDate)) return;
			var d = orbValues[orbDate]; string ds = orbDate.ToString("yyyyMMdd");
			DateTime lStart = orbDate.Add(ORBStart.Add(TimeSpan.FromSeconds(ORBSeconds)));
			DateTime maxEnd = orbDate.Add(ORBEndPlot);
			DateTime lEnd = isRealtime ? (Times[0].Count > 0 ? (Times[0][0] < maxEnd ? Times[0][0] : maxEnd) : lStart) : maxEnd;
			DateTime labTime = GetLabelTimeWithOffset(lEnd, isRealtime);

			Draw.Line(this, "PAX_HighLine_" + ds, true, lStart, d.High, lEnd, d.High, HighLineColor, DashStyleHelper.Solid, MainLineWidth);
			Draw.Text(this, "PAX_HighLabel_" + ds, true, LabelPrefix + " " + d.High.ToString("F2"), labTime, d.High, TextvertPixels, HighLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
			if (isRealtime) activeLabels["PAX_HighLabel_" + ds] = labTime;

			Draw.Line(this, "PAX_LowLine_" + ds, true, lStart, d.Low, lEnd, d.Low, LowLineColor, DashStyleHelper.Solid, MainLineWidth);
			Draw.Text(this, "PAX_LowLabel_" + ds, true, LabelPrefix + " " + d.Low.ToString("F2"), labTime, d.Low, TextvertPixels, LowLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
			if (isRealtime) activeLabels["PAX_LowLabel_" + ds] = labTime;

			if (ShowMid) {
				Draw.Line(this, "PAX_MidLine_" + ds, true, lStart, d.Mid, lEnd, d.Mid, MidLineColor, DashStyleHelper.Solid, MidLineWidth);
				Draw.Text(this, "PAX_MidLabel_" + ds, true, LabelPrefix + " MID " + d.Mid.ToString("F2"), labTime, d.Mid, TextvertPixels, MidLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
				if (isRealtime) activeLabels["PAX_MidLabel_" + ds] = labTime;
			}

			double factor = GetMarketLevelFactor();
			if (factor > 0 && upperLevels.ContainsKey(orbDate) && lowerLevels.ContainsKey(orbDate))
			{
				if (upperLevels[orbDate].Count > 0) {
					double u = upperLevels[orbDate][0];
					Draw.Line(this, "PAX_UpperLevel_" + ds + "_0", true, lStart, u, lEnd, u, HighLineColor, DashStyleHelper.Dash, LevelsLineWidth);
					Draw.Text(this, "PAX_UpperLabel_" + ds + "_0", true, LabelPrefix + " " + u.ToString("F2"), labTime, u, TextvertPixels, HighLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
					if (isRealtime) activeLabels["PAX_UpperLabel_" + ds + "_0"] = labTime;
				}
				if (lowerLevels[orbDate].Count > 0) {
					double lo = lowerLevels[orbDate][0];
					Draw.Line(this, "PAX_LowerLevel_" + ds + "_0", true, lStart, lo, lEnd, lo, LowLineColor, DashStyleHelper.Dash, LevelsLineWidth);
					Draw.Text(this, "PAX_LowerLabel_" + ds + "_0", true, LabelPrefix + " " + lo.ToString("F2"), labTime, lo, TextvertPixels, LowLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
					if (isRealtime) activeLabels["PAX_LowerLabel_" + ds + "_0"] = labTime;
				}
			}
		}

		private void CheckAndAddDynamicLevels(DateTime currentDate, DateTime currentTime)
		{
			double factor = GetMarketLevelFactor();
			if (!orbValues.ContainsKey(currentDate) || factor <= 0 || !upperLevels.ContainsKey(currentDate) || !lowerLevels.ContainsKey(currentDate)) return;
			var uls = upperLevels[currentDate]; var lls = lowerLevels[currentDate];
			if (uls.Count > 0 && lls.Count > 0)
			{
				double hU = uls[uls.Count - 1], lL = lls[lls.Count - 1];
				string ds = currentDate.ToString("yyyyMMdd");
				DateTime maxEnd = currentDate.Add(ORBEndPlot); bool isR = orbValues[currentDate].IsToday;
				DateTime lEnd = isR ? currentTime : maxEnd;
				if (High[0] > hU) {
					double nU = RoundToNearestTick(hU + factor); int idx = uls.Count; string key = "PAX_UpperLevel_" + ds + "_" + idx;
					if (!drawnLevels.Contains(key)) {
						uls.Add(nU); drawnLevels.Add(key);
						if (!upperLevelStartTimes.ContainsKey(currentDate)) upperLevelStartTimes[currentDate] = new Dictionary<int, DateTime>();
						upperLevelStartTimes[currentDate][idx] = currentTime;
						Draw.Line(this, key, true, currentTime, nU, lEnd, nU, HighLineColor, DashStyleHelper.Dash, LevelsLineWidth);
						DateTime labT = GetLabelTimeWithOffset(isR ? currentTime : maxEnd, isR);
						Draw.Text(this, "PAX_UpperLabel_" + ds + "_" + idx, true, LabelPrefix + " " + nU.ToString("F2"), labT, nU, TextvertPixels, HighLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
						if (isR) activeLabels["PAX_UpperLabel_" + ds + "_" + idx] = labT;
					}
				}
				if (Low[0] < lL) {
					double nL = RoundToNearestTick(lL - factor); int idx = lls.Count; string key = "PAX_LowerLevel_" + ds + "_" + idx;
					if (!drawnLevels.Contains(key)) {
						lls.Add(nL); drawnLevels.Add(key);
						if (!lowerLevelStartTimes.ContainsKey(currentDate)) lowerLevelStartTimes[currentDate] = new Dictionary<int, DateTime>();
						lowerLevelStartTimes[currentDate][idx] = currentTime;
						Draw.Line(this, key, true, currentTime, nL, lEnd, nL, LowLineColor, DashStyleHelper.Dash, LevelsLineWidth);
						DateTime labT = GetLabelTimeWithOffset(isR ? currentTime : maxEnd, isR);
						Draw.Text(this, "PAX_LowerLabel_" + ds + "_" + idx, true, LabelPrefix + " " + nL.ToString("F2"), labT, nL, TextvertPixels, LowLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
						if (isR) activeLabels["PAX_LowerLabel_" + ds + "_" + idx] = labT;
					}
				}
			}
		}

		private void MoveActiveLabels()
		{
			if (Times[0].Count < 1 || !orbValues.ContainsKey(realtimeOrbDate)) return;
			DateTime cT = Times[0][0]; DateTime oET = realtimeOrbDate.Add(ORBEndPlot);
			if (cT >= oET) return;
			DateTime lEnd = cT < oET ? cT : oET; string ds = realtimeOrbDate.ToString("yyyyMMdd");
			var d = orbValues[realtimeOrbDate]; DateTime lStart = realtimeOrbDate.Add(ORBStart.Add(TimeSpan.FromSeconds(ORBSeconds)));
			Draw.Line(this, "PAX_HighLine_" + ds, true, lStart, d.High, lEnd, d.High, HighLineColor, DashStyleHelper.Solid, MainLineWidth);
			Draw.Line(this, "PAX_LowLine_" + ds, true, lStart, d.Low, lEnd, d.Low, LowLineColor, DashStyleHelper.Solid, MainLineWidth);
			if (ShowMid) Draw.Line(this, "PAX_MidLine_" + ds, true, lStart, d.Mid, lEnd, d.Mid, MidLineColor, DashStyleHelper.Solid, MidLineWidth);
			if (upperLevels.ContainsKey(realtimeOrbDate)) {
				for (int i = 0; i < upperLevels[realtimeOrbDate].Count; i++) {
					DateTime sT = (i > 0 && upperLevelStartTimes.ContainsKey(realtimeOrbDate) && upperLevelStartTimes[realtimeOrbDate].ContainsKey(i)) ? upperLevelStartTimes[realtimeOrbDate][i] : lStart;
					Draw.Line(this, "PAX_UpperLevel_" + ds + "_" + i, true, sT, upperLevels[realtimeOrbDate][i], lEnd, upperLevels[realtimeOrbDate][i], HighLineColor, DashStyleHelper.Dash, LevelsLineWidth);
				}
			}
			if (lowerLevels.ContainsKey(realtimeOrbDate)) {
				for (int i = 0; i < lowerLevels[realtimeOrbDate].Count; i++) {
					DateTime sT = (i > 0 && lowerLevelStartTimes.ContainsKey(realtimeOrbDate) && lowerLevelStartTimes[realtimeOrbDate].ContainsKey(i)) ? lowerLevelStartTimes[realtimeOrbDate][i] : lStart;
					Draw.Line(this, "PAX_LowerLevel_" + ds + "_" + i, true, sT, lowerLevels[realtimeOrbDate][i], lEnd, lowerLevels[realtimeOrbDate][i], LowLineColor, DashStyleHelper.Dash, LevelsLineWidth);
				}
			}
			DateTime nLT = GetLabelTimeWithOffset(cT, true); var keys = activeLabels.Keys.ToList();
			foreach (string k in keys) {
				double p = 0; string txt = ""; Brush clr = Brushes.White;
				if (k.Contains("HighLabel")) { p = d.High; txt = LabelPrefix + " " + p.ToString("F2"); clr = HighLineColor; }
				else if (k.Contains("LowLabel")) { p = d.Low; txt = LabelPrefix + " " + p.ToString("F2"); clr = LowLineColor; }
				else if (k.Contains("MidLabel")) { p = d.Mid; txt = LabelPrefix + " MID " + p.ToString("F2"); clr = MidLineColor; }
				else if (k.Contains("UpperLabel")) {
					string[] pts = k.Split('_'); if (pts.Length > 1 && int.TryParse(pts[pts.Length - 1], out int idx)) {
						if (upperLevels.ContainsKey(realtimeOrbDate) && idx < upperLevels[realtimeOrbDate].Count) { p = upperLevels[realtimeOrbDate][idx]; txt = LabelPrefix + " " + p.ToString("F2"); clr = HighLineColor; }
					}
				}
				else if (k.Contains("LowerLabel")) {
					string[] pts = k.Split('_'); if (pts.Length > 1 && int.TryParse(pts[pts.Length - 1], out int idx)) {
						if (lowerLevels.ContainsKey(realtimeOrbDate) && idx < lowerLevels[realtimeOrbDate].Count) { p = lowerLevels[realtimeOrbDate][idx]; txt = LabelPrefix + " " + p.ToString("F2"); clr = LowLineColor; }
					}
				}
				if (p > 0) { Draw.Text(this, k, true, txt, nLT, p, TextvertPixels, clr, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0); activeLabels[k] = nLT; }
			}
		}

		private void CleanupOldData(DateTime currentDate)
		{
			var toRem = orbValues.Keys.Where(date => (currentDate - date).Days >= DAYS_TO_DISPLAY).ToList();
			foreach (var k in toRem) {
				string ds = k.ToString("yyyyMMdd");
				if (k == realtimeOrbDate) { realtimeOrbDate = DateTime.MinValue; activeLabels.Clear(); }
				drawnLevels.RemoveWhere(x => x.Contains(ds)); orbValues.Remove(k);
				if (upperLevels.ContainsKey(k)) { upperLevels[k].Clear(); upperLevels.Remove(k); }
				if (lowerLevels.ContainsKey(k)) { lowerLevels[k].Clear(); lowerLevels.Remove(k); }
				if (upperLevelStartTimes.ContainsKey(k)) { upperLevelStartTimes[k].Clear(); upperLevelStartTimes.Remove(k); }
				if (lowerLevelStartTimes.ContainsKey(k)) { lowerLevelStartTimes[k].Clear(); lowerLevelStartTimes.Remove(k); }
			}
		}
	}
}
