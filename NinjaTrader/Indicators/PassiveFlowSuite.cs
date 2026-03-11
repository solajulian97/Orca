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
		#region Private Fields — Depth tracking (Panel 1 + Panel 3)
		private readonly object depthLock = new object();

		// Bid/Ask depth books keyed by position → volume
		private Dictionary<int, long> bidDepthByPos;
		private Dictionary<int, long> askDepthByPos;

		// Panel 1 — Cumulative Book Delta
		private double prevTotalBidSize;
		private double prevTotalAskSize;
		private bool   hasPrevDepthSnapshot;
		private double cumulativeBookDelta;

		// Panel 3 — Cumulative OBI (rolling queue)
		private Queue<KeyValuePair<DateTime, double>> obiQueue;
		private double cobiSum;

		// Per-bar storage for all 3 sections
		private List<double> barBookDelta;
		private List<double> barAbsorption;
		private List<double> barAbsorptionSmoothed;
		private List<double> barCOBI;
		private List<bool>   barHasData;
		#endregion

		#region Private Fields — Tape tracking (Panel 2)
		private double lastBid;
		private double lastAsk;
		private double aggressiveDelta;

		// SMA ring buffer for absorption smoothing
		private Queue<double> absorptionBuffer;
		private double        absorptionBufSum;
		#endregion

		#region SharpDX Resources
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
		private SharpDX.DirectWrite.Factory    dwFactory;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "PassiveFlowSuite";
				Description					= "3-section passive flow indicator: Cumulative Book Delta, Absorption Ratio, and Cumulative OBI.";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				DisplayInDataBox			= true;
				IsSuspendedWhileInactive	= true;
				BarsRequiredToPlot			= 0;

				// Parameters
				DepthLevels			= 5;
				OBILevels			= 3;
				AbsorptionPeriod	= 20;
				COBIWindowMinutes	= 120;

				// Single invisible plot to drive auto-scaling (we custom-render everything)
				AddPlot(new Stroke(Brushes.Transparent, 0), PlotStyle.Line, "PassiveFlowData");

				// Visual colors
				HistogramUpColor	= Brushes.LimeGreen;
				HistogramDownColor	= Brushes.Crimson;
				AbsorptionUpColor	= Brushes.LimeGreen;
				AbsorptionDownColor	= Brushes.Crimson;
				AbsorptionLineColor	= Brushes.DodgerBlue;
				COBIBullColor		= Brushes.DodgerBlue;
				COBIBearColor		= Brushes.Orange;
				ZeroLineColor		= Brushes.DimGray;
				SeparatorColor		= Brushes.Gray;
				LabelColor			= Brushes.WhiteSmoke;
			}
			else if (State == State.Configure)
			{
				// Add 1-tick data series for tape reads
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				bidDepthByPos		= new Dictionary<int, long>();
				askDepthByPos		= new Dictionary<int, long>();
				obiQueue			= new Queue<KeyValuePair<DateTime, double>>();

				barBookDelta			= new List<double>(4096);
				barAbsorption			= new List<double>(4096);
				barAbsorptionSmoothed	= new List<double>(4096);
				barCOBI					= new List<double>(4096);
				barHasData				= new List<bool>(4096);

				absorptionBuffer	= new Queue<double>();
				absorptionBufSum	= 0;

				prevTotalBidSize	= 0;
				prevTotalAskSize	= 0;
				hasPrevDepthSnapshot = false;
				cumulativeBookDelta	= 0;
				cobiSum				= 0;
				aggressiveDelta		= 0;
				lastBid				= double.NaN;
				lastAsk				= double.NaN;
			}
			else if (State == State.Terminated)
			{
				DisposeDxResources();
			}
		}

		#region OnMarketDepth — Level 2 data (runs on separate thread)
		protected override void OnMarketDepth(MarketDepthEventArgs e)
		{
			lock (depthLock)
			{
				// Update the depth book
				var book = (e.MarketDataType == MarketDataType.Ask) ? askDepthByPos : bidDepthByPos;

				if (e.Operation == Operation.Add || e.Operation == Operation.Update)
				{
					book[e.Position] = e.Volume;
				}
				else if (e.Operation == Operation.Remove)
				{
					book.Remove(e.Position);
				}

				// --- Section 1: Cumulative Book Delta ---
				double totalBid = 0;
				double totalAsk = 0;
				int levelsForDelta = Math.Min(DepthLevels, 10);

				foreach (var kvp in bidDepthByPos)
				{
					if (kvp.Key < levelsForDelta)
						totalBid += kvp.Value;
				}
				foreach (var kvp in askDepthByPos)
				{
					if (kvp.Key < levelsForDelta)
						totalAsk += kvp.Value;
				}

				if (hasPrevDepthSnapshot)
				{
					double deltaBid = totalBid - prevTotalBidSize;
					double deltaAsk = totalAsk - prevTotalAskSize;
					cumulativeBookDelta += (deltaBid - deltaAsk);
				}
				else
				{
					hasPrevDepthSnapshot = true;
				}

				prevTotalBidSize = totalBid;
				prevTotalAskSize = totalAsk;

				// --- Section 3: OBI calculation ---
				int levelsForOBI = Math.Min(OBILevels, 10);
				double obiBid = 0;
				double obiAsk = 0;

				foreach (var kvp in bidDepthByPos)
				{
					if (kvp.Key < levelsForOBI)
						obiBid += kvp.Value;
				}
				foreach (var kvp in askDepthByPos)
				{
					if (kvp.Key < levelsForOBI)
						obiAsk += kvp.Value;
				}

				double denom = obiBid + obiAsk;
				if (denom > 0)
				{
					double obi = (obiBid - obiAsk) / denom;
					DateTime now = DateTime.UtcNow;

					obiQueue.Enqueue(new KeyValuePair<DateTime, double>(now, obi));
					cobiSum += obi;

					// Prune entries older than the rolling window
					DateTime cutoff = now.AddMinutes(-COBIWindowMinutes);
					while (obiQueue.Count > 0 && obiQueue.Peek().Key < cutoff)
					{
						cobiSum -= obiQueue.Dequeue().Value;
					}
				}
			}
		}
		#endregion

		#region OnMarketData — Tape reads for aggressive delta
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid)
			{
				lastBid = e.Price;
			}
			else if (e.MarketDataType == MarketDataType.Ask)
			{
				lastAsk = e.Price;
			}
			else if (e.MarketDataType == MarketDataType.Last)
			{
				// Sync bid/ask from tick replay events
				if (e.Ask > 0 && !double.IsNaN(e.Ask)) lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid)) lastBid = e.Bid;

				long vol = e.Volume;
				if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
					vol = (long)Core.Globals.ToCryptocurrencyVolume(vol);

				if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid))
				{
					if (e.Price >= lastAsk)
						aggressiveDelta += vol;   // Aggressive buy
					else if (e.Price <= lastBid)
						aggressiveDelta -= vol;   // Aggressive sell
				}
			}
		}
		#endregion

		#region EnsureBarLists
		private void EnsureBarLists(int idx)
		{
			while (barBookDelta.Count <= idx)
			{
				barBookDelta.Add(0);
				barAbsorption.Add(0);
				barAbsorptionSmoothed.Add(0);
				barCOBI.Add(0);
				barHasData.Add(false);
			}
		}
		#endregion

		#region OnBarUpdate
		protected override void OnBarUpdate()
		{
			// Process only primary bars
			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			EnsureBarLists(CurrentBar);

			// --- Session reset for Sections 1 and 2 ---
			if (Bars.IsFirstBarOfSession)
			{
				lock (depthLock)
				{
					cumulativeBookDelta  = 0;
					hasPrevDepthSnapshot = false;
				}
				aggressiveDelta  = 0;
				absorptionBuffer.Clear();
				absorptionBufSum = 0;
			}

			// --- Section 1: Cumulative Book Delta ---
			double bookDeltaSnapshot;
			double cobiSnapshot;
			lock (depthLock)
			{
				bookDeltaSnapshot = cumulativeBookDelta;
				cobiSnapshot      = cobiSum;
			}

			barBookDelta[CurrentBar] = bookDeltaSnapshot;

			// --- Section 2: Absorption Ratio ---
			double tickMove = TickSize > 0 ? Math.Abs(Close[0] - Open[0]) / TickSize : 0;
			double absorptionRaw = tickMove > 0 ? Math.Abs(aggressiveDelta) / tickMove : 0;

			// Rolling SMA
			absorptionBuffer.Enqueue(absorptionRaw);
			absorptionBufSum += absorptionRaw;
			while (absorptionBuffer.Count > AbsorptionPeriod)
			{
				absorptionBufSum -= absorptionBuffer.Dequeue();
			}

			double absorptionSmoothed = absorptionBuffer.Count > 0
				? absorptionBufSum / absorptionBuffer.Count
				: 0;

			barAbsorption[CurrentBar]         = absorptionRaw;
			barAbsorptionSmoothed[CurrentBar]  = absorptionSmoothed;

			// --- Section 3: Cumulative OBI ---
			barCOBI[CurrentBar] = cobiSnapshot;

			barHasData[CurrentBar] = true;

			// Drive panel auto-scaling — set to a dummy value so the panel doesn't collapse
			Value[0] = 0;
		}
		#endregion

		#region OnRender — Custom drawing: 3 visual sections in one panel
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (chartControl == null || chartScale == null || Bars == null || ChartBars == null)
				return;

			int fromIdx = ChartBars.FromIndex;
			int toIdx   = ChartBars.ToIndex;
			if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx)
				return;

			EnsureDxResources();
			if (dxGreenBrush == null) return;

			AntialiasMode oldMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = AntialiasMode.Aliased;

			float panelX = ChartPanel.X;
			float panelW = ChartPanel.W;
			float panelY = ChartPanel.Y;
			float panelH = ChartPanel.H;

			// Divide panel into 3 equal sections with 1px separators
			float sectionH = (panelH - 2f) / 3f;  // 2 separator lines
			float sec1Top  = panelY;
			float sec1Bot  = sec1Top + sectionH;
			float sec2Top  = sec1Bot + 1f;          // 1px separator
			float sec2Bot  = sec2Top + sectionH;
			float sec3Top  = sec2Bot + 1f;          // 1px separator
			float sec3Bot  = panelY + panelH;

			// Draw separator lines
			RenderTarget.DrawLine(
				new Vector2(panelX, sec1Bot),
				new Vector2(panelX + panelW, sec1Bot),
				dxSepBrush, 1f);
			RenderTarget.DrawLine(
				new Vector2(panelX, sec2Bot),
				new Vector2(panelX + panelW, sec2Bot),
				dxSepBrush, 1f);

			// --- Compute per-section min/max for Y scaling ---
			double s1Min = 0, s1Max = 0;
			double s2Min = double.MaxValue, s2Max = double.MinValue;
			double s3Min = 0, s3Max = 0;

			for (int i = fromIdx; i <= toIdx; i++)
			{
				if (i < 0 || i >= barBookDelta.Count || !barHasData[i]) continue;

				double v1 = barBookDelta[i];
				if (v1 < s1Min) s1Min = v1;
				if (v1 > s1Max) s1Max = v1;

				double v2 = barAbsorptionSmoothed[i];
				if (v2 < s2Min) s2Min = v2;
				if (v2 > s2Max) s2Max = v2;

				double v3 = barCOBI[i];
				if (v3 < s3Min) s3Min = v3;
				if (v3 > s3Max) s3Max = v3;
			}

			// Add padding so data doesn't touch edges
			double s1Pad = Math.Max(1, (s1Max - s1Min) * 0.1);
			s1Min -= s1Pad; s1Max += s1Pad;
			if (s1Max == s1Min) { s1Max = 1; s1Min = -1; }

			if (s2Min == double.MaxValue) { s2Min = 0; s2Max = 1; }
			double s2Pad = Math.Max(0.1, (s2Max - s2Min) * 0.1);
			s2Min -= s2Pad; s2Max += s2Pad;
			if (s2Max == s2Min) { s2Max = s2Min + 1; }

			double s3Pad = Math.Max(0.1, (s3Max - s3Min) * 0.1);
			s3Min -= s3Pad; s3Max += s3Pad;
			if (s3Max == s3Min) { s3Max = 1; s3Min = -1; }

			// Draw section labels
			DrawLabel("Cumulative Book Delta", panelX + 5, sec1Top + 2);
			DrawLabel("Absorption Ratio (" + AbsorptionPeriod + ")", panelX + 5, sec2Top + 2);
			DrawLabel("Cumulative OBI (" + COBIWindowMinutes + "m Rolling)", panelX + 5, sec3Top + 2);

			// ========== Section 1: Cumulative Book Delta — Histogram ==========
			float s1ZeroY = MapY(0, s1Min, s1Max, sec1Top, sec1Bot);
			// Zero line
			if (s1ZeroY >= sec1Top && s1ZeroY <= sec1Bot)
			{
				RenderTarget.DrawLine(
					new Vector2(panelX, s1ZeroY),
					new Vector2(panelX + panelW, s1ZeroY),
					dxZeroBrush, 1f);
			}

			for (int barIdx = fromIdx; barIdx <= toIdx; barIdx++)
			{
				if (barIdx < 0 || barIdx >= barBookDelta.Count || !barHasData[barIdx])
					continue;

				double val = barBookDelta[barIdx];
				float barX = chartControl.GetXByBarIndex(ChartBars, barIdx);
				float valY = MapY(val, s1Min, s1Max, sec1Top, sec1Bot);

				float barSpacing = GetBarSpacing(chartControl, barIdx, fromIdx, toIdx);
				float halfW = (float)(barSpacing * 0.8 / 2.0);
				if (halfW < 1f) halfW = 1f;

				float top    = Math.Min(s1ZeroY, valY);
				float bottom = Math.Max(s1ZeroY, valY);
				float height = bottom - top;
				if (height < 1f) height = 1f;

				// Clamp to section bounds
				top    = Math.Max(top, sec1Top);
				bottom = Math.Min(bottom, sec1Bot);
				if (bottom <= top) continue;

				var rect  = new RectangleF(barX - halfW, top, halfW * 2, bottom - top);
				var brush = val >= 0 ? dxGreenBrush : dxRedBrush;
				RenderTarget.FillRectangle(rect, brush);
			}

			// ========== Section 2: Absorption Ratio — Color-coded Line ==========
			float s2ZeroY = MapY(0, s2Min, s2Max, sec2Top, sec2Bot);

			RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
			for (int barIdx = fromIdx + 1; barIdx <= toIdx; barIdx++)
			{
				if (barIdx < 1 || barIdx >= barAbsorptionSmoothed.Count
					|| !barHasData[barIdx] || !barHasData[barIdx - 1])
					continue;

				float x1 = chartControl.GetXByBarIndex(ChartBars, barIdx - 1);
				float y1 = ClampY(MapY(barAbsorptionSmoothed[barIdx - 1], s2Min, s2Max, sec2Top, sec2Bot), sec2Top, sec2Bot);
				float x2 = chartControl.GetXByBarIndex(ChartBars, barIdx);
				float y2 = ClampY(MapY(barAbsorptionSmoothed[barIdx], s2Min, s2Max, sec2Top, sec2Bot), sec2Top, sec2Bot);

				// Color: green when raw >= smoothed (strong absorption), red when below
				bool strong = barAbsorption[barIdx] >= barAbsorptionSmoothed[barIdx];
				var lineBrush = strong ? dxAbsGreenBrush : dxAbsRedBrush;

				RenderTarget.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), lineBrush, 2f);
			}
			RenderTarget.AntialiasMode = AntialiasMode.Aliased;

			// ========== Section 3: Cumulative OBI — Line + Color Fill ==========
			float s3ZeroY = MapY(0, s3Min, s3Max, sec3Top, sec3Bot);
			// Zero line
			if (s3ZeroY >= sec3Top && s3ZeroY <= sec3Bot)
			{
				RenderTarget.DrawLine(
					new Vector2(panelX, s3ZeroY),
					new Vector2(panelX + panelW, s3ZeroY),
					dxZeroBrush, 1f);
			}

			// Color fill between COBI line and zero
			for (int barIdx = fromIdx; barIdx <= toIdx; barIdx++)
			{
				if (barIdx < 0 || barIdx >= barCOBI.Count || !barHasData[barIdx])
					continue;

				double val = barCOBI[barIdx];
				float barX = chartControl.GetXByBarIndex(ChartBars, barIdx);
				float valY = MapY(val, s3Min, s3Max, sec3Top, sec3Bot);

				float barSpacing = GetBarSpacing(chartControl, barIdx, fromIdx, toIdx);
				float halfW = barSpacing / 2f;
				if (halfW < 1f) halfW = 1f;

				float top    = Math.Min(s3ZeroY, valY);
				float bottom = Math.Max(s3ZeroY, valY);
				top    = Math.Max(top, sec3Top);
				bottom = Math.Min(bottom, sec3Bot);
				if (bottom - top < 0.5f) continue;

				var rect = new RectangleF(barX - halfW, top, halfW * 2, bottom - top);
				var fillBrush = val >= 0 ? dxBlueFillBrush : dxOrangeFillBrush;
				RenderTarget.FillRectangle(rect, fillBrush);
			}

			// COBI line on top
			RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
			for (int barIdx = fromIdx + 1; barIdx <= toIdx; barIdx++)
			{
				if (barIdx < 1 || barIdx >= barCOBI.Count
					|| !barHasData[barIdx] || !barHasData[barIdx - 1])
					continue;

				float x1 = chartControl.GetXByBarIndex(ChartBars, barIdx - 1);
				float y1 = ClampY(MapY(barCOBI[barIdx - 1], s3Min, s3Max, sec3Top, sec3Bot), sec3Top, sec3Bot);
				float x2 = chartControl.GetXByBarIndex(ChartBars, barIdx);
				float y2 = ClampY(MapY(barCOBI[barIdx], s3Min, s3Max, sec3Top, sec3Bot), sec3Top, sec3Bot);

				bool isPos = barCOBI[barIdx] >= 0;
				var lineBrush = isPos ? dxBlueFillBrush : dxOrangeFillBrush;
				RenderTarget.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), lineBrush, 2f);
			}
			RenderTarget.AntialiasMode = oldMode;
		}
		#endregion

		#region Render Helpers
		/// <summary>Maps a data value to a Y pixel within [secTop, secBot].</summary>
		private float MapY(double value, double dataMin, double dataMax, float secTop, float secBot)
		{
			if (dataMax == dataMin) return (secTop + secBot) / 2f;
			// Y is inverted: higher values → smaller pixel Y
			double pct = (value - dataMin) / (dataMax - dataMin);
			return secBot - (float)(pct * (secBot - secTop));
		}

		private float ClampY(float y, float top, float bot)
		{
			if (y < top) return top;
			if (y > bot) return bot;
			return y;
		}

		private float GetBarSpacing(ChartControl chartControl, int barIdx, int fromIdx, int toIdx)
		{
			float barX = chartControl.GetXByBarIndex(ChartBars, barIdx);
			if (barIdx < toIdx)
				return chartControl.GetXByBarIndex(ChartBars, barIdx + 1) - barX;
			else if (barIdx > fromIdx)
				return barX - chartControl.GetXByBarIndex(ChartBars, barIdx - 1);
			else
				return (float)chartControl.BarWidth;
		}

		private void DrawLabel(string text, float x, float y)
		{
			if (dxLabelFormat == null || dxLabelBrush == null) return;
			var layout = new SharpDX.DirectWrite.TextLayout(dwFactory, text, dxLabelFormat, 400, 20);
			RenderTarget.DrawTextLayout(new Vector2(x, y), layout, dxLabelBrush);
			layout.Dispose();
		}
		#endregion

		#region DX Resources
		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			if (dxGreenBrush == null)
			{
				dxGreenBrush          = HistogramUpColor.ToDxBrush(RenderTarget);
				dxGreenBrush.Opacity  = 0.85f;
				dxRedBrush            = HistogramDownColor.ToDxBrush(RenderTarget);
				dxRedBrush.Opacity    = 0.85f;

				dxBlueFillBrush           = COBIBullColor.ToDxBrush(RenderTarget);
				dxBlueFillBrush.Opacity   = 0.35f;
				dxOrangeFillBrush         = COBIBearColor.ToDxBrush(RenderTarget);
				dxOrangeFillBrush.Opacity = 0.35f;

				dxZeroBrush = ZeroLineColor.ToDxBrush(RenderTarget);
				dxSepBrush  = SeparatorColor.ToDxBrush(RenderTarget);

				dxAbsLineBrush  = AbsorptionLineColor.ToDxBrush(RenderTarget);
				dxAbsGreenBrush = AbsorptionUpColor.ToDxBrush(RenderTarget);
				dxAbsRedBrush   = AbsorptionDownColor.ToDxBrush(RenderTarget);

				dxLabelBrush = LabelColor.ToDxBrush(RenderTarget);

				dwFactory    = new SharpDX.DirectWrite.Factory();
				dxLabelFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Segoe UI", 11f);
			}
		}

		private void DisposeDxResources()
		{
			if (dxGreenBrush      != null) { dxGreenBrush.Dispose();      dxGreenBrush      = null; }
			if (dxRedBrush        != null) { dxRedBrush.Dispose();        dxRedBrush        = null; }
			if (dxBlueFillBrush   != null) { dxBlueFillBrush.Dispose();   dxBlueFillBrush   = null; }
			if (dxOrangeFillBrush != null) { dxOrangeFillBrush.Dispose(); dxOrangeFillBrush = null; }
			if (dxZeroBrush       != null) { dxZeroBrush.Dispose();       dxZeroBrush       = null; }
			if (dxSepBrush        != null) { dxSepBrush.Dispose();        dxSepBrush        = null; }
			if (dxAbsLineBrush    != null) { dxAbsLineBrush.Dispose();    dxAbsLineBrush    = null; }
			if (dxAbsGreenBrush   != null) { dxAbsGreenBrush.Dispose();   dxAbsGreenBrush   = null; }
			if (dxAbsRedBrush     != null) { dxAbsRedBrush.Dispose();     dxAbsRedBrush     = null; }
			if (dxLabelBrush      != null) { dxLabelBrush.Dispose();      dxLabelBrush      = null; }
			if (dxLabelFormat     != null) { dxLabelFormat.Dispose();     dxLabelFormat     = null; }
			if (dwFactory         != null) { dwFactory.Dispose();         dwFactory         = null; }
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}
		#endregion

		#region Properties — Parameters

		[Range(1, 10)]
		[Display(Name = "Depth Levels", Order = 1, GroupName = "Parameters",
			Description = "Number of book levels to track for Cumulative Book Delta (Section 1).")]
		public int DepthLevels { get; set; }

		[Range(1, 10)]
		[Display(Name = "OBI Levels", Order = 2, GroupName = "Parameters",
			Description = "Number of book levels used for OBI calculation (Section 3).")]
		public int OBILevels { get; set; }

		[Range(1, 100)]
		[Display(Name = "Absorption Period", Order = 3, GroupName = "Parameters",
			Description = "SMA smoothing period for Absorption Ratio (Section 2).")]
		public int AbsorptionPeriod { get; set; }

		[Range(1, 1440)]
		[Display(Name = "COBI Window (min)", Order = 4, GroupName = "Parameters",
			Description = "Rolling window in minutes for Cumulative OBI (Section 3).")]
		public int COBIWindowMinutes { get; set; }

		#endregion

		#region Properties — Visual

		[XmlIgnore]
		[Display(Name = "Histogram Up Color", Order = 1, GroupName = "Visual")]
		public System.Windows.Media.Brush HistogramUpColor { get; set; }
		[Browsable(false)]
		public string HistogramUpColorSerialize
		{ get { return Serialize.BrushToString(HistogramUpColor); } set { HistogramUpColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Histogram Down Color", Order = 2, GroupName = "Visual")]
		public System.Windows.Media.Brush HistogramDownColor { get; set; }
		[Browsable(false)]
		public string HistogramDownColorSerialize
		{ get { return Serialize.BrushToString(HistogramDownColor); } set { HistogramDownColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Absorption Up Color", Order = 3, GroupName = "Visual")]
		public System.Windows.Media.Brush AbsorptionUpColor { get; set; }
		[Browsable(false)]
		public string AbsorptionUpColorSerialize
		{ get { return Serialize.BrushToString(AbsorptionUpColor); } set { AbsorptionUpColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Absorption Down Color", Order = 4, GroupName = "Visual")]
		public System.Windows.Media.Brush AbsorptionDownColor { get; set; }
		[Browsable(false)]
		public string AbsorptionDownColorSerialize
		{ get { return Serialize.BrushToString(AbsorptionDownColor); } set { AbsorptionDownColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Absorption Line Color", Order = 5, GroupName = "Visual")]
		public System.Windows.Media.Brush AbsorptionLineColor { get; set; }
		[Browsable(false)]
		public string AbsorptionLineColorSerialize
		{ get { return Serialize.BrushToString(AbsorptionLineColor); } set { AbsorptionLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "COBI Bull Color", Order = 6, GroupName = "Visual")]
		public System.Windows.Media.Brush COBIBullColor { get; set; }
		[Browsable(false)]
		public string COBIBullColorSerialize
		{ get { return Serialize.BrushToString(COBIBullColor); } set { COBIBullColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "COBI Bear Color", Order = 7, GroupName = "Visual")]
		public System.Windows.Media.Brush COBIBearColor { get; set; }
		[Browsable(false)]
		public string COBIBearColorSerialize
		{ get { return Serialize.BrushToString(COBIBearColor); } set { COBIBearColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Zero Line Color", Order = 8, GroupName = "Visual")]
		public System.Windows.Media.Brush ZeroLineColor { get; set; }
		[Browsable(false)]
		public string ZeroLineColorSerialize
		{ get { return Serialize.BrushToString(ZeroLineColor); } set { ZeroLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Separator Color", Order = 9, GroupName = "Visual")]
		public System.Windows.Media.Brush SeparatorColor { get; set; }
		[Browsable(false)]
		public string SeparatorColorSerialize
		{ get { return Serialize.BrushToString(SeparatorColor); } set { SeparatorColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Label Color", Order = 10, GroupName = "Visual")]
		public System.Windows.Media.Brush LabelColor { get; set; }
		[Browsable(false)]
		public string LabelColorSerialize
		{ get { return Serialize.BrushToString(LabelColor); } set { LabelColor = Serialize.StringToBrush(value); } }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PassiveFlowSuite[] cachePassiveFlowSuite;
		public PassiveFlowSuite PassiveFlowSuite()
		{
			return PassiveFlowSuite(Input);
		}

		public PassiveFlowSuite PassiveFlowSuite(ISeries<double> input)
		{
			if (cachePassiveFlowSuite != null)
				for (int idx = 0; idx < cachePassiveFlowSuite.Length; idx++)
					if (cachePassiveFlowSuite[idx] != null &&  cachePassiveFlowSuite[idx].EqualsInput(input))
						return cachePassiveFlowSuite[idx];
			return CacheIndicator<PassiveFlowSuite>(new PassiveFlowSuite(), input, ref cachePassiveFlowSuite);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PassiveFlowSuite PassiveFlowSuite()
		{
			return indicator.PassiveFlowSuite(Input);
		}

		public Indicators.PassiveFlowSuite PassiveFlowSuite(ISeries<double> input )
		{
			return indicator.PassiveFlowSuite(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PassiveFlowSuite PassiveFlowSuite()
		{
			return indicator.PassiveFlowSuite(Input);
		}

		public Indicators.PassiveFlowSuite PassiveFlowSuite(ISeries<double> input )
		{
			return indicator.PassiveFlowSuite(input);
		}
	}
}

#endregion
