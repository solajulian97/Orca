#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Core.FloatingPoint;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors = System.Windows.Media.Colors;
using DxSolidBrush = SharpDX.Direct2D1.SolidColorBrush;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaMGIWeekly : Indicator
	{
		#region Helper Classes
		private class VwapAccum
		{
			public double SumVol, SumPV;
			public void Add(double p, double v) { SumVol += v; SumPV += p * v; }
			public void Reset() { SumVol = 0; SumPV = 0; }
			public double Value => SumVol > 0 ? SumPV / SumVol : double.NaN;
		}

		private class WeekData
		{
			public double Open = double.NaN, High = double.NaN, Low = double.NaN, Close = double.NaN, Mid = double.NaN;
			public double VAH = double.NaN, VAL = double.NaN, POC = double.NaN;
			public double IBH = double.NaN, IBL = double.NaN, IBMid = double.NaN;
			public Dictionary<double, double> VolByPrice = new Dictionary<double, double>();
			public VwapAccum Vwap = new VwapAccum();
			public bool IBComplete;
			public void ResetAll()
			{
				Open = High = Low = Close = Mid = VAH = VAL = POC = IBH = IBL = IBMid = double.NaN;
				IBComplete = false; VolByPrice.Clear(); Vwap.Reset();
			}
			public void UpdateHL(double h, double l, double c)
			{
				if (double.IsNaN(Open)) Open = c;
				if (double.IsNaN(High) || h > High) High = h;
				if (double.IsNaN(Low) || l < Low) Low = l;
				Close = c;
				if (!double.IsNaN(High) && !double.IsNaN(Low)) Mid = (High + Low) / 2.0;
			}
		}
		#endregion

		#region Fields
		private WeekData curWeek, priorWeek;
		private DateTime curWeekStart = DateTime.MinValue;
		private bool dxValid;
		private DxSolidBrush[] dxBrushes;
		private SharpDX.Direct2D1.StrokeStyle[] dxStrokes;
		private SharpDX.DirectWrite.TextFormat dxLabelFormat;
		private int lastBarIdx = -1;

		// Level indices
		private const int LVL_COUNT = 20;
		private const int L_WIBH = 0, L_WIBL = 1, L_WIBMID = 2;
		private const int L_CWH = 3, L_CWL = 4, L_CWMID = 5;
		private const int L_CWVAH = 6, L_CWVAL = 7, L_CWPOC = 8;
		private const int L_PWH = 9, L_PWL = 10, L_PWC = 11, L_PWO = 12, L_PWMID = 13;
		private const int L_PWVAH = 14, L_PWVAL = 15, L_PWPOC = 16;
		private const int L_WVWAP = 17;

		private struct LevelInfo
		{
			public double Price; public string Label; public int BrushIdx; public bool Enabled;
		}
		private LevelInfo[] lvls;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca MGI Weekly";
				Description = "Plots structural levels from current and prior weekly sessions including weekly IB, range, value area, and VWAP.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;

				RTHOpenTime = new TimeSpan(9, 30, 0);
				ETHOpenTime = new TimeSpan(18, 0, 0);

				ShowWeeklyIB = true; ShowCurWeekRange = true; ShowCurWeekVA = true;
				ShowPriorWeekRange = true; ShowPriorWeekVA = true; ShowWeeklyVwap = true;
				ShowLabels = true; ValueAreaPct = 70;
				MgiStyle = MgiPlotStyle.Regular;

				WIBRegionOpacity = 10; CWRegionOpacity = 8; PWRegionOpacity = 8;

				WIBColor = WpfBrushes.MediumSeaGreen;
				CWRangeColor = WpfBrushes.CornflowerBlue;
				CWVAColor = WpfBrushes.SteelBlue;
				PWRangeColor = WpfBrushes.SandyBrown;
				PWVAColor = WpfBrushes.Peru;
				WVwapColor = WpfBrushes.Orchid;

				MainLineWidth = 2; VALineWidth = 1;
				MainDashStyle = MgiDashStyle.Solid; VADashStyle = MgiDashStyle.Dash;
				LabelFontName = "Segoe UI"; LabelFontSize = 10;

				AddPlot(new Stroke(WpfBrushes.Transparent, 1), PlotStyle.Line, "WklyDummy");
			}
			else if (State == State.DataLoaded)
			{
				curWeek = new WeekData(); priorWeek = new WeekData();
				lvls = new LevelInfo[LVL_COUNT];
			}
			else if (State == State.Terminated) { DisposeDx(); }
		}

		#region VA Calculation
		private void CalcVA(WeekData w)
		{
			if (w.VolByPrice.Count < 2) return;
			var sorted = w.VolByPrice.OrderByDescending(kv => kv.Value).ToList();
			w.POC = sorted[0].Key;
			double total = sorted.Sum(kv => kv.Value);
			if (total <= 0) return;
			double target = total * (ValueAreaPct / 100.0);
			var prices = w.VolByPrice.Keys.OrderBy(p => p).ToList();
			int pi = prices.IndexOf(w.POC);
			if (pi < 0) { pi = prices.Count / 2; w.POC = prices[pi]; }
			double acc = w.VolByPrice[w.POC]; int lo = pi, hi = pi;
			while (acc < target && (lo > 0 || hi < prices.Count - 1))
			{
				double vB = lo > 0 ? w.VolByPrice[prices[lo - 1]] : 0;
				double vA = hi < prices.Count - 1 ? w.VolByPrice[prices[hi + 1]] : 0;
				if (lo <= 0) { hi++; acc += vA; }
				else if (hi >= prices.Count - 1) { lo--; acc += vB; }
				else if (vA >= vB) { hi++; acc += vA; }
				else { lo--; acc += vB; }
			}
			w.VAL = prices[lo]; w.VAH = prices[hi];
		}

		private void DistVol(WeekData w, double high, double low, double vol)
		{
			if (vol <= 0 || high <= low) return;
			double ts = TickSize;
			int ticks = Math.Max(1, (int)Math.Round((high - low) / ts) + 1);
			double per = vol / ticks;
			for (int i = 0; i < ticks; i++)
			{
				double p = Math.Round((low + i * ts) / ts) * ts;
				if (w.VolByPrice.ContainsKey(p)) w.VolByPrice[p] += per;
				else w.VolByPrice[p] = per;
			}
		}
		#endregion

		#region OnBarUpdate
		private bool CrossedTime(TimeSpan prev, TimeSpan cur, TimeSpan target)
		{
			if (target > prev && target <= cur) return true;
			if (prev > cur && (target > prev || target <= cur)) return true;
			return false;
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) return;
			DateTime t = Time[0];
			TimeSpan tod = t.TimeOfDay;
			DateTime prevT = Time[1];
			double h = High[0], l = Low[0], c = Close[0], vol = Volume[0];
			double typ = (h + l + c) / 3.0;

			// Detect new week: Sunday ETH open (18:00) crossing
			bool isNewWeek = false;
			if (t.DayOfWeek == DayOfWeek.Sunday && prevT.DayOfWeek != DayOfWeek.Sunday && prevT.DayOfWeek != DayOfWeek.Saturday)
				isNewWeek = true;
			else if (t.DayOfWeek == DayOfWeek.Sunday && CrossedTime(prevT.TimeOfDay, tod, ETHOpenTime))
				isNewWeek = true;
			else if (prevT.DayOfWeek == DayOfWeek.Saturday && t.DayOfWeek == DayOfWeek.Sunday)
				isNewWeek = true;
			// Also handle Monday crossing if no Sunday data
			else if (t.DayOfWeek == DayOfWeek.Monday && curWeekStart == DateTime.MinValue)
				isNewWeek = true;

			if (isNewWeek && t.Date != curWeekStart)
			{
				// Copy current to prior
				priorWeek.Open = curWeek.Open; priorWeek.High = curWeek.High;
				priorWeek.Low = curWeek.Low; priorWeek.Close = curWeek.Close; priorWeek.Mid = curWeek.Mid;
				priorWeek.VAH = curWeek.VAH; priorWeek.VAL = curWeek.VAL; priorWeek.POC = curWeek.POC;
				curWeek.ResetAll();
				curWeekStart = t.Date;
			}

			// Update current week
			curWeek.UpdateHL(h, l, c);
			DistVol(curWeek, h, l, vol);

			// Weekly IB: first 60 min of Monday RTH
			if (t.DayOfWeek == DayOfWeek.Monday && !curWeek.IBComplete)
			{
				bool inRTH = tod >= RTHOpenTime && tod < RTHOpenTime + TimeSpan.FromMinutes(60);
				if (inRTH)
				{
					if (double.IsNaN(curWeek.IBH) || h > curWeek.IBH) curWeek.IBH = h;
					if (double.IsNaN(curWeek.IBL) || l < curWeek.IBL) curWeek.IBL = l;
					curWeek.IBMid = (curWeek.IBH + curWeek.IBL) / 2.0;
				}
				else if (tod >= RTHOpenTime + TimeSpan.FromMinutes(60))
					curWeek.IBComplete = true;
			}
			else if (t.DayOfWeek != DayOfWeek.Monday && !curWeek.IBComplete)
				curWeek.IBComplete = true;

			// Weekly VWAP (anchored to Monday RTH open)
			if (t.DayOfWeek == DayOfWeek.Monday && CrossedTime(prevT.TimeOfDay, tod, RTHOpenTime))
				curWeek.Vwap.Reset();
			bool rthActive = tod >= RTHOpenTime;
			if (rthActive || t.DayOfWeek != DayOfWeek.Monday)
				curWeek.Vwap.Add(typ, vol);

			// Recalc VA
			if (CurrentBar != lastBarIdx)
			{
				lastBarIdx = CurrentBar;
				if (curWeek.VolByPrice.Count > 2) CalcVA(curWeek);
			}

			BuildLevelCache();
		}

		private void BuildLevelCache()
		{
			for (int i = 0; i < LVL_COUNT; i++) lvls[i].Enabled = false;

			if (ShowWeeklyIB) { SL(L_WIBH, curWeek.IBH, "WIBH", 0); SL(L_WIBL, curWeek.IBL, "WIBL", 0); SL(L_WIBMID, curWeek.IBMid, "WIBM", 0); }
			if (ShowCurWeekRange) { SL(L_CWH, curWeek.High, "CWH", 1); SL(L_CWL, curWeek.Low, "CWL", 1); SL(L_CWMID, curWeek.Mid, "CWM", 1); }
			if (ShowCurWeekVA) { SL(L_CWVAH, curWeek.VAH, "CWVAH", 2); SL(L_CWVAL, curWeek.VAL, "CWVAL", 2); SL(L_CWPOC, curWeek.POC, "CWPOC", 2); }
			if (ShowPriorWeekRange) { SL(L_PWH, priorWeek.High, "PWH", 3); SL(L_PWL, priorWeek.Low, "PWL", 3); SL(L_PWC, priorWeek.Close, "PWC", 3); SL(L_PWO, priorWeek.Open, "PWO", 3); SL(L_PWMID, priorWeek.Mid, "PWM", 3); }
			if (ShowPriorWeekVA) { SL(L_PWVAH, priorWeek.VAH, "PWVAH", 4); SL(L_PWVAL, priorWeek.VAL, "PWVAL", 4); SL(L_PWPOC, priorWeek.POC, "PWPOC", 4); }
			if (ShowWeeklyVwap) SL(L_WVWAP, curWeek.Vwap.Value, "WVWAP", 5);
		}

		private void SL(int idx, double price, string label, int brushIdx)
		{
			if (double.IsNaN(price)) return;
			lvls[idx].Price = price;
			lvls[idx].Label = label + (ShowPriceInLabel ? " " + FormatPrice(price) : "");
			lvls[idx].BrushIdx = brushIdx;
			lvls[idx].Enabled = true;
		}

		private string FormatPrice(double p)
		{
			return Instrument != null ? Instrument.MasterInstrument.FormatPrice(p) : p.ToString("F2");
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl cc, ChartScale cs)
		{
			base.OnRender(cc, cs);
			if (cc == null || cs == null || ChartBars == null || lvls == null) return;
			EnsureDx();
			if (!dxValid) return;

			float chartRight = (float)cc.CanvasRight;
			float pTop = ChartPanel.Y, pBot = pTop + ChartPanel.H, pLeft = ChartPanel.X;
			bool edge = MgiStyle == MgiPlotStyle.Edge;
			float lineLeft = edge ? chartRight - 60f : pLeft;

			var oldAA = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = AntialiasMode.Aliased;

			// Region fills
			DrawRegion(cs, curWeek.IBH, curWeek.IBL, 0, WIBRegionOpacity, pLeft, chartRight, pTop, pBot);
			DrawRegion(cs, curWeek.High, curWeek.Low, 1, CWRegionOpacity, pLeft, chartRight, pTop, pBot);
			DrawRegion(cs, priorWeek.High, priorWeek.Low, 3, PWRegionOpacity, pLeft, chartRight, pTop, pBot);

			for (int i = 0; i < LVL_COUNT; i++)
			{
				if (!lvls[i].Enabled) continue;
				float y = cs.GetYByValue(lvls[i].Price);
				if (y < pTop - 5 || y > pBot + 5) continue;
				int bi = lvls[i].BrushIdx;
				if (bi >= dxBrushes.Length || dxBrushes[bi] == null) continue;

				bool isVA = i == L_CWVAH || i == L_CWVAL || i == L_CWPOC || i == L_PWVAH || i == L_PWVAL || i == L_PWPOC;
				int w = isVA ? VALineWidth : MainLineWidth;
				var stroke = GetStroke(isVA ? VADashStyle : MainDashStyle);
				RenderTarget.DrawLine(new Vector2(lineLeft, y), new Vector2(chartRight, y), dxBrushes[bi], w, stroke);

				if (ShowLabels && dxLabelFormat != null)
				{
					var rect = new RectangleF(chartRight - 150, y - LabelFontSize - 1, 145, LabelFontSize + 4);
					RenderTarget.DrawText(lvls[i].Label, dxLabelFormat, rect, dxBrushes[bi]);
				}
			}
			RenderTarget.AntialiasMode = oldAA;
		}

		private void DrawRegion(ChartScale cs, double hi, double lo, int bi, int opacity, float left, float right, float pTop, float pBot)
		{
			if (double.IsNaN(hi) || double.IsNaN(lo) || opacity <= 0) return;
			float yH = cs.GetYByValue(hi), yL = cs.GetYByValue(lo);
			if (yH > pBot || yL < pTop) return;
			yH = Math.Max(pTop, yH); yL = Math.Min(pBot, yL);
			if (bi >= dxBrushes.Length || dxBrushes[bi] == null) return;
			float prev = dxBrushes[bi].Opacity;
			dxBrushes[bi].Opacity = opacity / 100f;
			RenderTarget.FillRectangle(new RectangleF(left, yH, right - left, yL - yH), dxBrushes[bi]);
			dxBrushes[bi].Opacity = prev;
		}
		#endregion

		#region DX Resources
		private Color4 ToC4(WpfBrush b, float a = 1f)
		{
			var c = (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * a);
		}

		private SharpDX.Direct2D1.StrokeStyle GetStroke(MgiDashStyle ds)
		{
			int idx = (int)ds;
			return (idx < dxStrokes.Length && dxStrokes[idx] != null) ? dxStrokes[idx] : null;
		}

		private void EnsureDx()
		{
			if (dxValid || RenderTarget == null) return;
			try
			{
				WpfBrush[] cm = { WIBColor, CWRangeColor, CWVAColor, PWRangeColor, PWVAColor, WVwapColor };
				dxBrushes = new DxSolidBrush[cm.Length];
				for (int i = 0; i < cm.Length; i++) dxBrushes[i] = new DxSolidBrush(RenderTarget, ToC4(cm[i]));

				var f = RenderTarget.Factory;
				dxStrokes = new SharpDX.Direct2D1.StrokeStyle[4];
				dxStrokes[0] = new SharpDX.Direct2D1.StrokeStyle(f, new StrokeStyleProperties { DashStyle = DashStyle.Solid });
				dxStrokes[1] = new SharpDX.Direct2D1.StrokeStyle(f, new StrokeStyleProperties { DashStyle = DashStyle.Dash });
				dxStrokes[2] = new SharpDX.Direct2D1.StrokeStyle(f, new StrokeStyleProperties { DashStyle = DashStyle.Dot });
				dxStrokes[3] = new SharpDX.Direct2D1.StrokeStyle(f, new StrokeStyleProperties { DashStyle = DashStyle.DashDot });

				dxLabelFormat = new SharpDX.DirectWrite.TextFormat(
					NinjaTrader.Core.Globals.DirectWriteFactory, LabelFontName,
					FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, (float)LabelFontSize)
				{ TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Near };
				dxValid = true;
			}
			catch { dxValid = false; }
		}

		private void DisposeDx()
		{
			try
			{
				if (dxBrushes != null) foreach (var b in dxBrushes) b?.Dispose();
				if (dxStrokes != null) foreach (var s in dxStrokes) s?.Dispose();
				dxLabelFormat?.Dispose();
			}
			catch { }
			dxBrushes = null; dxStrokes = null; dxLabelFormat = null; dxValid = false;
		}

		public override void OnRenderTargetChanged() { DisposeDx(); base.OnRenderTargetChanged(); }
		#endregion

		#region Properties
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name="RTH Open Time", Order=1, GroupName="01. Session Times")]
		public TimeSpan RTHOpenTime { get; set; }

		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name="ETH Open Time", Order=2, GroupName="01. Session Times")]
		public TimeSpan ETHOpenTime { get; set; }

		// Weekly IB
		[Display(Name="Show Weekly IB", Description="First 60 min of Monday RTH", Order=1, GroupName="02. Weekly IB")]
		public bool ShowWeeklyIB { get; set; }
		[XmlIgnore][Display(Name="WIB Color", Order=2, GroupName="02. Weekly IB")]
		public WpfBrush WIBColor { get; set; }
		[Browsable(false)] public string WIBColorS { get { return Serialize.BrushToString(WIBColor); } set { WIBColor = Serialize.StringToBrush(value); } }
		[Range(0,100)][Display(Name="WIB Region Opacity %", Order=3, GroupName="02. Weekly IB")]
		public int WIBRegionOpacity { get; set; }

		// Current Week Range
		[Display(Name="Show Current Week Range", Order=1, GroupName="03. Current Week Range")]
		public bool ShowCurWeekRange { get; set; }
		[XmlIgnore][Display(Name="CW Range Color", Order=2, GroupName="03. Current Week Range")]
		public WpfBrush CWRangeColor { get; set; }
		[Browsable(false)] public string CWRangeColorS { get { return Serialize.BrushToString(CWRangeColor); } set { CWRangeColor = Serialize.StringToBrush(value); } }
		[Range(0,100)][Display(Name="CW Region Opacity %", Order=3, GroupName="03. Current Week Range")]
		public int CWRegionOpacity { get; set; }

		// Current Week VA
		[Display(Name="Show Current Week VA", Order=1, GroupName="04. Current Week VA")]
		public bool ShowCurWeekVA { get; set; }
		[XmlIgnore][Display(Name="CW VA Color", Order=2, GroupName="04. Current Week VA")]
		public WpfBrush CWVAColor { get; set; }
		[Browsable(false)] public string CWVAColorS { get { return Serialize.BrushToString(CWVAColor); } set { CWVAColor = Serialize.StringToBrush(value); } }

		// Prior Week Range
		[Display(Name="Show Prior Week Range", Order=1, GroupName="05. Prior Week Range")]
		public bool ShowPriorWeekRange { get; set; }
		[XmlIgnore][Display(Name="PW Range Color", Order=2, GroupName="05. Prior Week Range")]
		public WpfBrush PWRangeColor { get; set; }
		[Browsable(false)] public string PWRangeColorS { get { return Serialize.BrushToString(PWRangeColor); } set { PWRangeColor = Serialize.StringToBrush(value); } }
		[Range(0,100)][Display(Name="PW Region Opacity %", Order=3, GroupName="05. Prior Week Range")]
		public int PWRegionOpacity { get; set; }

		// Prior Week VA
		[Display(Name="Show Prior Week VA", Order=1, GroupName="06. Prior Week VA")]
		public bool ShowPriorWeekVA { get; set; }
		[XmlIgnore][Display(Name="PW VA Color", Order=2, GroupName="06. Prior Week VA")]
		public WpfBrush PWVAColor { get; set; }
		[Browsable(false)] public string PWVAColorS { get { return Serialize.BrushToString(PWVAColor); } set { PWVAColor = Serialize.StringToBrush(value); } }

		// Weekly VWAP
		[Display(Name="Show Weekly VWAP", Description="Anchored to Monday RTH open", Order=1, GroupName="07. Weekly VWAP")]
		public bool ShowWeeklyVwap { get; set; }
		[XmlIgnore][Display(Name="WVWAP Color", Order=2, GroupName="07. Weekly VWAP")]
		public WpfBrush WVwapColor { get; set; }
		[Browsable(false)] public string WVwapColorS { get { return Serialize.BrushToString(WVwapColor); } set { WVwapColor = Serialize.StringToBrush(value); } }

		// Labels & Style
		[Display(Name="Show Labels", Order=1, GroupName="08. Labels")]
		public bool ShowLabels { get; set; }
		[NinjaScriptProperty][Display(Name="Font Name", Order=2, GroupName="08. Labels")]
		public string LabelFontName { get; set; }
		[Range(6,24)][Display(Name="Font Size", Order=3, GroupName="08. Labels")]
		public int LabelFontSize { get; set; }
		[Display(Name="Abbreviate Labels", Order=4, GroupName="08. Labels")]
		public bool AbbreviateLabels { get; set; }
		[Display(Name="Show Price in Label", Order=5, GroupName="08. Labels")]
		public bool ShowPriceInLabel { get; set; }

		[NinjaScriptProperty][Display(Name="Plot Style", Order=1, GroupName="09. Plot Style")]
		public MgiPlotStyle MgiStyle { get; set; }
		[Range(1,5)][Display(Name="Main Line Width", Order=2, GroupName="09. Plot Style")]
		public int MainLineWidth { get; set; }
		[Range(1,5)][Display(Name="VA Line Width", Order=3, GroupName="09. Plot Style")]
		public int VALineWidth { get; set; }
		[Display(Name="Main Dash Style", Order=4, GroupName="09. Plot Style")]
		public MgiDashStyle MainDashStyle { get; set; }
		[Display(Name="VA Dash Style", Order=5, GroupName="09. Plot Style")]
		public MgiDashStyle VADashStyle { get; set; }
		[Range(50,100)][Display(Name="Value Area %", Order=6, GroupName="09. Plot Style")]
		public int ValueAreaPct { get; set; }
		#endregion
	}
}
