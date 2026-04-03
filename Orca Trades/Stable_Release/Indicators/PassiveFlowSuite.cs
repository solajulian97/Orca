#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class PassiveFlowSuite : Indicator
	{
		private readonly object depthLock = new object();
		private Dictionary<int, long> bidDepthByPos;
		private Dictionary<int, long> askDepthByPos;
		private double prevTotalBidSize;
		private double prevTotalAskSize;
		private bool hasPrevDepthSnapshot;
		private double cumulativeBookDelta;
		private Queue<KeyValuePair<DateTime, double>> obiQueue;
		private double cobiSum;
		private List<double> barBookDelta;
		private List<double> barAbsorption;
		private List<double> barAbsorptionSmoothed;
		private List<double> barCOBI;
		private List<bool>   barHasData;
		private double lastBid;
		private double lastAsk;
		private double aggressiveDelta;
		private Queue<double> absorptionBuffer;
		private double absorptionBufSum;

		private SharpDX.Direct2D1.Brush dxGreenBrush;
		private SharpDX.Direct2D1.Brush dxRedBrush;
		private SharpDX.Direct2D1.Brush dxBlueFillBrush;
		private SharpDX.Direct2D1.Brush dxOrangeFillBrush;
		private SharpDX.Direct2D1.Brush dxZeroBrush;
		private SharpDX.Direct2D1.Brush dxSepBrush;
		private SharpDX.Direct2D1.Brush dxAbsLineBrush;
		private SharpDX.Direct2D1.Brush dxAbsGreenBrush;
		private SharpDX.Direct2D1.Brush dxAbsRedBrush;
		private SharpDX.Direct2D1.Brush dxLabelBrush;
		private SharpDX.DirectWrite.TextFormat dxLabelFormat;
		private SharpDX.DirectWrite.Factory dwFactory;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "PassiveFlowSuite";
				Description = "3-section passive flow indicator: Cumulative Book Delta, Absorption Ratio, and Cumulative OBI.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = false;
				DrawOnPricePanel = false;
				DisplayInDataBox = true;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;
				DepthLevels = 5;
				OBILevels = 3;
				AbsorptionPeriod = 20;
				COBIWindowMinutes = 120;
				AddPlot(new Stroke(System.Windows.Media.Brushes.Transparent, 0), PlotStyle.Line, "PassiveFlowData");
				HistogramUpColor = System.Windows.Media.Brushes.LimeGreen;
				HistogramDownColor = System.Windows.Media.Brushes.Crimson;
				AbsorptionUpColor = System.Windows.Media.Brushes.LimeGreen;
				AbsorptionDownColor = System.Windows.Media.Brushes.Crimson;
				AbsorptionLineColor = System.Windows.Media.Brushes.DodgerBlue;
				COBIBullColor = System.Windows.Media.Brushes.DodgerBlue;
				COBIBearColor = System.Windows.Media.Brushes.Orange;
				ZeroLineColor = System.Windows.Media.Brushes.DimGray;
				SeparatorColor = System.Windows.Media.Brushes.Gray;
				LabelColor = System.Windows.Media.Brushes.WhiteSmoke;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				bidDepthByPos = new Dictionary<int, long>();
				askDepthByPos = new Dictionary<int, long>();
				obiQueue = new Queue<KeyValuePair<DateTime, double>>();
				barBookDelta = new List<double>(4096);
				barAbsorption = new List<double>(4096);
				barAbsorptionSmoothed = new List<double>(4096);
				barCOBI = new List<double>(4096);
				barHasData = new List<bool>(4096);
				absorptionBuffer = new Queue<double>();
				absorptionBufSum = 0;
				prevTotalBidSize = 0; prevTotalAskSize = 0;
				hasPrevDepthSnapshot = false; cumulativeBookDelta = 0;
				cobiSum = 0; aggressiveDelta = 0;
				lastBid = double.NaN; lastAsk = double.NaN;
			}
			else if (State == State.Terminated)
			{
				DisposeDxResources();
			}
		}

		protected override void OnMarketDepth(MarketDepthEventArgs e)
		{
			lock (depthLock)
			{
				var book = (e.MarketDataType == MarketDataType.Ask) ? askDepthByPos : bidDepthByPos;
				if (e.Operation == Operation.Add || e.Operation == Operation.Update) book[e.Position] = e.Volume;
				else if (e.Operation == Operation.Remove) book.Remove(e.Position);

				double totalBid = 0; double totalAsk = 0; int levelsForDelta = Math.Min(DepthLevels, 10);
				foreach (var kvp in bidDepthByPos) if (kvp.Key < levelsForDelta) totalBid += kvp.Value;
				foreach (var kvp in askDepthByPos) if (kvp.Key < levelsForDelta) totalAsk += kvp.Value;

				if (hasPrevDepthSnapshot) { double deltaBid = totalBid - prevTotalBidSize; double deltaAsk = totalAsk - prevTotalAskSize; cumulativeBookDelta += (deltaBid - deltaAsk); }
				else hasPrevDepthSnapshot = true;
				prevTotalBidSize = totalBid; prevTotalAskSize = totalAsk;

				int levelsForOBI = Math.Min(OBILevels, 10); double obiBid = 0; double obiAsk = 0;
				foreach (var kvp in bidDepthByPos) if (kvp.Key < levelsForOBI) obiBid += kvp.Value;
				foreach (var kvp in askDepthByPos) if (kvp.Key < levelsForOBI) obiAsk += kvp.Value;

				double denom = obiBid + obiAsk;
				if (denom > 0) {
					double obi = (obiBid - obiAsk) / denom; DateTime now = DateTime.UtcNow;
					obiQueue.Enqueue(new KeyValuePair<DateTime, double>(now, obi));
					cobiSum += obi;
					DateTime cutoff = now.AddMinutes(-COBIWindowMinutes);
					while (obiQueue.Count > 0 && obiQueue.Peek().Key < cutoff) cobiSum -= obiQueue.Dequeue().Value;
				}
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid) lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask) lastAsk = e.Price;
			else if (e.MarketDataType == MarketDataType.Last) {
				if (e.Ask > 0 && !double.IsNaN(e.Ask)) lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid)) lastBid = e.Bid;
				long vol = e.Volume;
				if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency) vol = (long)NinjaTrader.Core.Globals.ToCryptocurrencyVolume(vol);
				if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid)) {
					if (e.Price >= lastAsk) aggressiveDelta += vol;
					else if (e.Price <= lastBid) aggressiveDelta -= vol;
				}
			}
		}

		private void EnsureBarLists(int idx) { while (barBookDelta.Count <= idx) { barBookDelta.Add(0); barAbsorption.Add(0); barAbsorptionSmoothed.Add(0); barCOBI.Add(0); barHasData.Add(false); } }

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0 || CurrentBar < 0) return;
			EnsureBarLists(CurrentBar);
			if (Bars.IsFirstBarOfSession) { lock (depthLock) { cumulativeBookDelta = 0; hasPrevDepthSnapshot = false; } aggressiveDelta = 0; absorptionBuffer.Clear(); absorptionBufSum = 0; }
			double bookDeltaSnapshot; double cobiSnapshot;
			lock (depthLock) { bookDeltaSnapshot = cumulativeBookDelta; cobiSnapshot = cobiSum; }
			barBookDelta[CurrentBar] = bookDeltaSnapshot;
			double tickMove = TickSize > 0 ? Math.Abs(Close[0] - Open[0]) / TickSize : 0;
			double absorptionRaw = tickMove > 0 ? Math.Abs(aggressiveDelta) / tickMove : 0;
			absorptionBuffer.Enqueue(absorptionRaw); absorptionBufSum += absorptionRaw;
			while (absorptionBuffer.Count > AbsorptionPeriod) absorptionBufSum -= absorptionBuffer.Dequeue();
			double absorptionSmoothed = absorptionBuffer.Count > 0 ? absorptionBufSum / absorptionBuffer.Count : 0;
			barAbsorption[CurrentBar] = absorptionRaw; barAbsorptionSmoothed[CurrentBar] = absorptionSmoothed;
			barCOBI[CurrentBar] = cobiSnapshot; barHasData[CurrentBar] = true;
			Value[0] = 0;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (chartControl == null || chartScale == null || Bars == null || ChartBars == null) return;
			int fromIdx = ChartBars.FromIndex; int toIdx = ChartBars.ToIndex;
			if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx) return;
			EnsureDxResources();
			if (dxGreenBrush == null) return;
			SharpDX.Direct2D1.AntialiasMode oldMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
			float panelX = ChartPanel.X; float panelW = ChartPanel.W; float panelY = ChartPanel.Y; float panelH = ChartPanel.H;
			float sectionH = (panelH - 2f) / 3f;
			float sec1Top = panelY; float sec1Bot = sec1Top + sectionH;
			float sec2Top = sec1Bot + 1f; float sec2Bot = sec2Top + sectionH;
			float sec3Top = sec2Bot + 1f; float sec3Bot = panelY + panelH;
			RenderTarget.DrawLine(new Vector2(panelX, sec1Bot), new Vector2(panelX + panelW, sec1Bot), dxSepBrush, 1f);
			RenderTarget.DrawLine(new Vector2(panelX, sec2Bot), new Vector2(panelX + panelW, sec2Bot), dxSepBrush, 1f);

			double s1Min = 0, s1Max = 0, s2Min = double.MaxValue, s2Max = double.MinValue, s3Min = 0, s3Max = 0;
			for (int i = fromIdx; i <= toIdx; i++) {
				if (i < 0 || i >= barBookDelta.Count || !barHasData[i]) continue;
				s1Min = Math.Min(s1Min, barBookDelta[i]); s1Max = Math.Max(s1Max, barBookDelta[i]);
				s2Min = Math.Min(s2Min, barAbsorptionSmoothed[i]); s2Max = Math.Max(s2Max, barAbsorptionSmoothed[i]);
				s3Min = Math.Min(s3Min, barCOBI[i]); s3Max = Math.Max(s3Max, barCOBI[i]);
			}
			double s1Pad = Math.Max(1, (s1Max - s1Min) * 0.1); s1Min -= s1Pad; s1Max += s1Pad; if (s1Max == s1Min) { s1Max = 1; s1Min = -1; }
			if (s2Min == double.MaxValue) { s2Min = 0; s2Max = 1; } double s2Pad = Math.Max(0.1, (s2Max - s2Min) * 0.1); s2Min -= s2Pad; s2Max += s2Pad; if (s2Max == s2Min) s2Max = s2Min + 1;
			double s3Pad = Math.Max(0.1, (s3Max - s3Min) * 0.1); s3Min -= s3Pad; s3Max += s3Pad; if (s3Max == s3Min) { s3Max = 1; s3Min = -1; }

			DrawLabel("Cumulative Book Delta", panelX + 5, sec1Top + 2);
			DrawLabel("Absorption Ratio (" + AbsorptionPeriod + ")", panelX + 5, sec2Top + 2);
			DrawLabel("Cumulative OBI (" + COBIWindowMinutes + "m Rolling)", panelX + 5, sec3Top + 2);

			float s1ZeroY = MapY(0, s1Min, s1Max, sec1Top, sec1Bot);
			if (s1ZeroY >= sec1Top && s1ZeroY <= sec1Bot) RenderTarget.DrawLine(new Vector2(panelX, s1ZeroY), new Vector2(panelX + panelW, s1ZeroY), dxZeroBrush, 1f);
			for (int i = fromIdx; i <= toIdx; i++) {
				if (i < 0 || i >= barBookDelta.Count || !barHasData[i]) continue;
				float bX = chartControl.GetXByBarIndex(ChartBars, i); float vY = MapY(barBookDelta[i], s1Min, s1Max, sec1Top, sec1Bot);
				float hW = Math.Max(1f, GetBarSpacing(chartControl, i, fromIdx, toIdx) * 0.4f);
				float t = Math.Max(sec1Top, Math.Min(s1ZeroY, vY)), b = Math.Min(sec1Bot, Math.Max(s1ZeroY, vY));
				if (b > t) RenderTarget.FillRectangle(new RectangleF(bX - hW, t, hW * 2, b - t), barBookDelta[i] >= 0 ? dxGreenBrush : dxRedBrush);
			}

			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
			for (int i = fromIdx + 1; i <= toIdx; i++) {
				if (i < 1 || i >= barAbsorptionSmoothed.Count || !barHasData[i] || !barHasData[i - 1]) continue;
				float x1 = chartControl.GetXByBarIndex(ChartBars, i - 1), x2 = chartControl.GetXByBarIndex(ChartBars, i);
				float y1 = MapY(barAbsorptionSmoothed[i - 1], s2Min, s2Max, sec2Top, sec2Bot), y2 = MapY(barAbsorptionSmoothed[i], s2Min, s2Max, sec2Top, sec2Bot);
				RenderTarget.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), barAbsorption[i] >= barAbsorptionSmoothed[i] ? dxAbsGreenBrush : dxAbsRedBrush, 2f);
			}

			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
			float s3ZeroY = MapY(0, s3Min, s3Max, sec3Top, sec3Bot);
			if (s3ZeroY >= sec3Top && s3ZeroY <= sec3Bot) RenderTarget.DrawLine(new Vector2(panelX, s3ZeroY), new Vector2(panelX + panelW, s3ZeroY), dxZeroBrush, 1f);
			for (int i = fromIdx; i <= toIdx; i++) {
				if (i < 0 || i >= barCOBI.Count || !barHasData[i]) continue;
				float bX = chartControl.GetXByBarIndex(ChartBars, i); float vY = MapY(barCOBI[i], s3Min, s3Max, sec3Top, sec3Bot);
				float hW = GetBarSpacing(chartControl, i, fromIdx, toIdx) * 0.5f;
				float t = Math.Max(sec3Top, Math.Min(s3ZeroY, vY)), b = Math.Min(sec3Bot, Math.Max(s3ZeroY, vY));
				if (b - t >= 0.5f) RenderTarget.FillRectangle(new RectangleF(bX - hW, t, hW * 2, b - t), barCOBI[i] >= 0 ? dxBlueFillBrush : dxOrangeFillBrush);
			}
			RenderTarget.AntialiasMode = oldMode;
		}

		private float MapY(double val, double min, double max, float top, float bot) { if (max == min) return (top + bot) / 2f; double pct = (val - min) / (max - min); return bot - (float)(pct * (bot - top)); }
		private float GetBarSpacing(ChartControl chartControl, int idx, int from, int to) { float bX = chartControl.GetXByBarIndex(ChartBars, idx); if (idx < to) return chartControl.GetXByBarIndex(ChartBars, idx + 1) - bX; else if (idx > from) return bX - chartControl.GetXByBarIndex(ChartBars, idx - 1); return (float)chartControl.BarWidth; }
		private void DrawLabel(string text, float x, float y) { if (dxLabelFormat == null || dxLabelBrush == null) return; using (var l = new SharpDX.DirectWrite.TextLayout(dwFactory, text, dxLabelFormat, 400, 20)) RenderTarget.DrawTextLayout(new Vector2(x, y), l, dxLabelBrush); }

		private void EnsureDxResources()
		{
			if (RenderTarget == null || dxGreenBrush != null) return;
			dxGreenBrush = CreateSolidBrush(HistogramUpColor, 0.85f);
			dxRedBrush = CreateSolidBrush(HistogramDownColor, 0.85f);
			dxBlueFillBrush = CreateSolidBrush(COBIBullColor, 0.35f);
			dxOrangeFillBrush = CreateSolidBrush(COBIBearColor, 0.35f);
			dxZeroBrush = CreateSolidBrush(ZeroLineColor, 1.0f);
			dxSepBrush = CreateSolidBrush(SeparatorColor, 1.0f);
			dxAbsLineBrush = CreateSolidBrush(AbsorptionLineColor, 1.0f);
			dxAbsGreenBrush = CreateSolidBrush(AbsorptionUpColor, 1.0f);
			dxAbsRedBrush = CreateSolidBrush(AbsorptionDownColor, 1.0f);
			dxLabelBrush = CreateSolidBrush(LabelColor, 1.0f);
			dwFactory = new SharpDX.DirectWrite.Factory();
			dxLabelFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Segoe UI", 11f);
		}

		private SharpDX.Direct2D1.Brush CreateSolidBrush(System.Windows.Media.Brush wpfBrush, float opacity)
		{
			var color = (wpfBrush as System.Windows.Media.SolidColorBrush)?.Color ?? System.Windows.Media.Colors.White;
			return new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(color.R / 255f, color.G / 255f, color.B / 255f, (color.A / 255f) * opacity));
		}

		private void DisposeDxResources()
		{
			if (dxGreenBrush != null) { dxGreenBrush.Dispose(); dxGreenBrush = null; }
			if (dxRedBrush != null) { dxRedBrush.Dispose(); dxRedBrush = null; }
			if (dxBlueFillBrush != null) { dxBlueFillBrush.Dispose(); dxBlueFillBrush = null; }
			if (dxOrangeFillBrush != null) { dxOrangeFillBrush.Dispose(); dxOrangeFillBrush = null; }
			if (dxZeroBrush != null) { dxZeroBrush.Dispose(); dxZeroBrush = null; }
			if (dxSepBrush != null) { dxSepBrush.Dispose(); dxSepBrush = null; }
			if (dxAbsLineBrush != null) { dxAbsLineBrush.Dispose(); dxAbsLineBrush = null; }
			if (dxAbsGreenBrush != null) { dxAbsGreenBrush.Dispose(); dxAbsGreenBrush = null; }
			if (dxAbsRedBrush != null) { dxAbsRedBrush.Dispose(); dxAbsRedBrush = null; }
			if (dxLabelBrush != null) { dxLabelBrush.Dispose(); dxLabelBrush = null; }
			if (dxLabelFormat != null) { dxLabelFormat.Dispose(); dxLabelFormat = null; }
			if (dwFactory != null) { dwFactory.Dispose(); dwFactory = null; }
		}

		public override void OnRenderTargetChanged() { DisposeDxResources(); base.OnRenderTargetChanged(); }

		[Range(1, 10)] [Display(Name = "Depth Levels", Order = 1, GroupName = "Parameters")] public int DepthLevels { get; set; }
		[Range(1, 10)] [Display(Name = "OBI Levels", Order = 2, GroupName = "Parameters")] public int OBILevels { get; set; }
		[Range(1, 100)] [Display(Name = "Absorption Period", Order = 3, GroupName = "Parameters")] public int AbsorptionPeriod { get; set; }
		[Range(1, 1440)] [Display(Name = "COBI Window (min)", Order = 4, GroupName = "Parameters")] public int COBIWindowMinutes { get; set; }
		[XmlIgnore] [Display(Name = "Histogram Up Color", Order = 1, GroupName = "Visual")] public System.Windows.Media.Brush HistogramUpColor { get; set; }
		[Browsable(false)] public string HistogramUpColorSerialize { get { return Serialize.BrushToString(HistogramUpColor); } set { HistogramUpColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Histogram Down Color", Order = 2, GroupName = "Visual")] public System.Windows.Media.Brush HistogramDownColor { get; set; }
		[Browsable(false)] public string HistogramDownColorSerialize { get { return Serialize.BrushToString(HistogramDownColor); } set { HistogramDownColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Absorption Up Color", Order = 3, GroupName = "Visual")] public System.Windows.Media.Brush AbsorptionUpColor { get; set; }
		[Browsable(false)] public string AbsorptionUpColorSerialize { get { return Serialize.BrushToString(AbsorptionUpColor); } set { AbsorptionUpColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Absorption Down Color", Order = 4, GroupName = "Visual")] public System.Windows.Media.Brush AbsorptionDownColor { get; set; }
		[Browsable(false)] public string AbsorptionDownColorSerialize { get { return Serialize.BrushToString(AbsorptionDownColor); } set { AbsorptionDownColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Absorption Line Color", Order = 5, GroupName = "Visual")] public System.Windows.Media.Brush AbsorptionLineColor { get; set; }
		[Browsable(false)] public string AbsorptionLineColorSerialize { get { return Serialize.BrushToString(AbsorptionLineColor); } set { AbsorptionLineColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "COBI Bull Color", Order = 6, GroupName = "Visual")] public System.Windows.Media.Brush COBIBullColor { get; set; }
		[Browsable(false)] public string COBIBullColorSerialize { get { return Serialize.BrushToString(COBIBullColor); } set { COBIBullColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "COBI Bear Color", Order = 7, GroupName = "Visual")] public System.Windows.Media.Brush COBIBearColor { get; set; }
		[Browsable(false)] public string COBIBearColorSerialize { get { return Serialize.BrushToString(COBIBearColor); } set { COBIBearColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Zero Line Color", Order = 8, GroupName = "Visual")] public System.Windows.Media.Brush ZeroLineColor { get; set; }
		[Browsable(false)] public string ZeroLineColorSerialize { get { return Serialize.BrushToString(ZeroLineColor); } set { ZeroLineColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Separator Color", Order = 9, GroupName = "Visual")] public System.Windows.Media.Brush SeparatorColor { get; set; }
		[Browsable(false)] public string SeparatorColorSerialize { get { return Serialize.BrushToString(SeparatorColor); } set { SeparatorColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Label Color", Order = 10, GroupName = "Visual")] public System.Windows.Media.Brush LabelColor { get; set; }
		[Browsable(false)] public string LabelColorSerialize { get { return Serialize.BrushToString(LabelColor); } set { LabelColor = Serialize.StringToBrush(value); } }
	}
}
