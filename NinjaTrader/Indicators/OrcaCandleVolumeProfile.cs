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

using WpfBrush  = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors  = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum VALineStyleEnum
	{
		Solid = 0,
		Dash = 1,
		Dot = 2,
		DashDot = 3
	}

	public class OrcaCandleVolumeProfile : Indicator
	{
		#region Fields
		// Per primary-bar volume & delta maps
		private List<Dictionary<double, long>> barVolumeMaps;
		private List<Dictionary<double, long>> barDeltaMaps;
		private List<double[]> barVACache; // [0]=VAH, [1]=VAL, [2]=POC, [3]=MaxVol

		// Bid/Ask cache for delta classification
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double prevLast = double.NaN;

		// SharpDX rendering resources
		private SolidColorBrush bullBodyBrushDx;
		private SolidColorBrush bearBodyBrushDx;
		private SolidColorBrush bullWickBrushDx;
		private SolidColorBrush bearWickBrushDx;
		private SolidColorBrush volBrushDx;
		private SolidColorBrush pocBrushDx;
		private SolidColorBrush posDeltaBrushDx;
		private SolidColorBrush negDeltaBrushDx;

		// Volume gradient palette (dark → bright) — outside VA
		private SolidColorBrush[] volGradientBrushes;
		private int lastBuiltGradientSteps = -1;

		// Value Area gradient palette (dark → bright) — inside VA
		private SolidColorBrush vaVolBrushDx;
		private SolidColorBrush[] vaGradientBrushes;
		private int lastBuiltVAGradientSteps = -1;

		// VA line resources
		private SolidColorBrush vaLineBrushDx;
		private StrokeStyle vaLineStrokeDx;

		// Text resources
		private SolidColorBrush deltaTextBrushDx;
		private TextFormat      textFormatDx;
		private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name        = "OrcaCandleVolumeProfile";
				Description = "Custom footprint chart: draws candles + per-candle volume profiles with optional delta coloring and Value Area.";
				Calculate   = Calculate.OnPriceChange;
				IsOverlay   = true;

				// Data
				TickCompression = 4;

				// Layout
				CandleWidthPx       = 14;
				ProfileWidthPx      = 80;
				DynamicProfileWidth = true;
				CandleProfileGapPx  = 2;
				ProfileBarSpacingPx = 0;
				WickWidthPx         = 2;

				// Visibility
				ShowPOC       = true;
				ShowDelta     = false;
				UseGradient   = true;
				GradientSteps = 16;

				// Value Area
				ShowValueArea    = true;
				ShowVAColor      = true;
				ShowVALines      = true;
				ValueAreaPercent = 70;
				VALineThickness  = 1.5f;
				VALineStyle      = VALineStyleEnum.Dash;

				// Colors — candles
				BullishBodyBrush = WpfBrushes.MediumSeaGreen;
				BearishBodyBrush = WpfBrushes.Crimson;

				// Colors — profile
				VolumeBrush    = WpfBrushes.RoyalBlue;
				VolumeOpacity  = 0.85f;
				MinBrightness  = 0.20f;
				POCBrush       = WpfBrushes.DodgerBlue;

				// Colors — Value Area
				VABrush     = WpfBrushes.CornflowerBlue;
				VALineBrush = WpfBrushes.White;

				// Colors — delta
				PositiveDeltaBrush = WpfBrushes.Lime;
				NegativeDeltaBrush = WpfBrushes.Red;
				DeltaOpacity       = 0.85f;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				barVolumeMaps = new List<Dictionary<double, long>>(4096);
				barDeltaMaps  = new List<Dictionary<double, long>>(4096);
				barVACache    = new List<double[]>(4096);
				textWidthCache.Clear();
			}
			else if (State == State.Terminated)
			{
				DisposeDx();
			}
		}

		#region Dispose
		private void DisposeDx()
		{
			try
			{
				bullBodyBrushDx?.Dispose();
				bearBodyBrushDx?.Dispose();
				bullWickBrushDx?.Dispose();
				bearWickBrushDx?.Dispose();
				volBrushDx?.Dispose();
				pocBrushDx?.Dispose();
				posDeltaBrushDx?.Dispose();
				negDeltaBrushDx?.Dispose();
				vaVolBrushDx?.Dispose();
				vaLineBrushDx?.Dispose();
				vaLineStrokeDx?.Dispose();
				deltaTextBrushDx?.Dispose();
				textFormatDx?.Dispose();

				if (volGradientBrushes != null)
					for (int i = 0; i < volGradientBrushes.Length; i++)
						volGradientBrushes[i]?.Dispose();

				if (vaGradientBrushes != null)
					for (int i = 0; i < vaGradientBrushes.Length; i++)
						vaGradientBrushes[i]?.Dispose();
			}
			catch { }
			finally
			{
				bullBodyBrushDx    = null;
				bearBodyBrushDx    = null;
				bullWickBrushDx    = null;
				bearWickBrushDx    = null;
				volBrushDx         = null;
				pocBrushDx         = null;
				posDeltaBrushDx    = null;
				negDeltaBrushDx    = null;
				vaVolBrushDx       = null;
				vaLineBrushDx      = null;
				vaLineStrokeDx     = null;
				deltaTextBrushDx   = null;
				textFormatDx       = null;
				volGradientBrushes = null;
				vaGradientBrushes  = null;
				lastBuiltGradientSteps   = -1;
				lastBuiltVAGradientSteps = -1;
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}
		#endregion

		#region Market Data / Tick Processing
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid)
				lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask)
				lastAsk = e.Price;
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				ProcessTickIntoPrimaryBar();

				// Removed ForceRefresh() to fix UI Thread lagging

				return;
			}

			if (BarsInProgress == 0 && CurrentBar >= 0)
			{
				EnsureBarMaps(CurrentBar);
			}
		}

		private void EnsureBarMaps(int primaryBarIndex)
		{
			while (barVolumeMaps.Count <= primaryBarIndex)
				barVolumeMaps.Add(new Dictionary<double, long>());

			while (barDeltaMaps.Count <= primaryBarIndex)
				barDeltaMaps.Add(new Dictionary<double, long>());
				
			while (barVACache.Count <= primaryBarIndex)
				barVACache.Add(new double[] { double.NaN, double.NaN, double.NaN, 0 });
		}

		private void ProcessTickIntoPrimaryBar()
		{
			int primaryIndex = BarsArray[0].GetBar(Time[0]);
			if (primaryIndex < 0) return;

			EnsureBarMaps(primaryIndex);

			double last = Close[0];
			long   vol  = (long)Volume[0];
			if (vol <= 0) return;

			double comp        = TickCompression * TickSize;
			double bucketPrice = Math.Floor(last / comp + 0.000001) * comp;

			// --- VOLUME ---
			var vmap = barVolumeMaps[primaryIndex];
			if (vmap.TryGetValue(bucketPrice, out long vExisting))
				vmap[bucketPrice] = vExisting + vol;
			else
				vmap[bucketPrice] = vol;

			// --- DELTA ---
			long signed = 0;
			if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
			{
				if (last >= lastAsk)       signed = +vol;
				else if (last <= lastBid)  signed = -vol;
				else if (!double.IsNaN(prevLast))
					signed = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
			}
			else if (!double.IsNaN(prevLast))
			{
				signed = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
			}
			prevLast = last;

			if (signed != 0)
			{
				var dmap = barDeltaMaps[primaryIndex];
				if (dmap.TryGetValue(bucketPrice, out long dExisting))
					dmap[bucketPrice] = dExisting + signed;
				else
					dmap[bucketPrice] = signed;
			}
		}
		#endregion

		#region Value Area Calculation
		/// <summary>
		/// Calculates Value Area boundaries for a given volume map.
		/// Returns true if valid, with vahPrice and valPrice set.
		/// VA = price range covering ValueAreaPercent% of total volume, expanding outward from POC.
		/// </summary>
		private bool CalcValueArea(Dictionary<double, long> volMap, double pocPrice, out double vahPrice, out double valPrice)
		{
			vahPrice = pocPrice;
			valPrice = pocPrice;

			if (volMap.Count <= 1) return false;

			// Sort all price levels
			var sortedPrices = new List<double>(volMap.Keys);
			sortedPrices.Sort();

			long totalVol = 0;
			foreach (var kv in volMap) totalVol += kv.Value;
			if (totalVol <= 0) return false;

			double targetVol = totalVol * (ValueAreaPercent / 100.0);

			// Find POC index in sorted list
			int pocIdx = sortedPrices.IndexOf(pocPrice);
			if (pocIdx < 0) return false;

			long accumulatedVol = volMap[pocPrice];
			int lo = pocIdx;
			int hi = pocIdx;

			// Expand outward from POC: pick the side with more volume at the next level
			while (accumulatedVol < targetVol && (lo > 0 || hi < sortedPrices.Count - 1))
			{
				long volBelow = (lo > 0) ? volMap[sortedPrices[lo - 1]] : 0;
				long volAbove = (hi < sortedPrices.Count - 1) ? volMap[sortedPrices[hi + 1]] : 0;

				if (lo <= 0)
				{
					hi++;
					accumulatedVol += volAbove;
				}
				else if (hi >= sortedPrices.Count - 1)
				{
					lo--;
					accumulatedVol += volBelow;
				}
				else if (volAbove >= volBelow)
				{
					hi++;
					accumulatedVol += volAbove;
				}
				else
				{
					lo--;
					accumulatedVol += volBelow;
				}
			}

			valPrice = sortedPrices[lo];
			vahPrice = sortedPrices[hi];
			return true;
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);

			if (barVolumeMaps == null || ChartBars == null) return;

			EnsureDxResources();

			int fromIdx = ChartBars.FromIndex;
			int toIdx   = ChartBars.ToIndex;

			float panelTop    = ChartPanel.Y;
			float panelBottom = ChartPanel.Y + ChartPanel.H;

			for (int barIdx = fromIdx; barIdx <= toIdx; barIdx++)
			{
				if (barIdx < 0 || barIdx >= BarsArray[0].Count) continue;

				float barCenterX = chartControl.GetXByBarIndex(ChartBars, barIdx);

				// --- OHLC ---
				double o = BarsArray[0].GetOpen(barIdx);
				double h = BarsArray[0].GetHigh(barIdx);
				double l = BarsArray[0].GetLow(barIdx);
				double c = BarsArray[0].GetClose(barIdx);

				float yOpen  = chartScale.GetYByValue(o);
				float yHigh  = chartScale.GetYByValue(h);
				float yLow   = chartScale.GetYByValue(l);
				float yClose = chartScale.GetYByValue(c);

				bool isBullish = c >= o;

				float bodyTop    = Math.Min(yOpen, yClose);
				float bodyBottom = Math.Max(yOpen, yClose);
				float bodyHeight = Math.Max(1f, bodyBottom - bodyTop);

				float halfCandle = CandleWidthPx / 2f;
				float candleLeft  = barCenterX - halfCandle;
				float candleRight = barCenterX + halfCandle;

				var bodyBrush = isBullish ? bullBodyBrushDx : bearBodyBrushDx;
				var wickBrush = isBullish ? bullWickBrushDx : bearWickBrushDx;

				// --- Draw Wick ---
				float wickX    = barCenterX;
				float halfWick = WickWidthPx / 2f;

				if (yHigh < bodyTop)
				{
					RenderTarget.FillRectangle(
						new RectangleF(wickX - halfWick, yHigh, WickWidthPx, bodyTop - yHigh),
						wickBrush);
				}
				if (yLow > bodyBottom)
				{
					RenderTarget.FillRectangle(
						new RectangleF(wickX - halfWick, bodyBottom, WickWidthPx, yLow - bodyBottom),
						wickBrush);
				}

				// --- Draw Body ---
				RenderTarget.FillRectangle(
					new RectangleF(candleLeft, bodyTop, CandleWidthPx, bodyHeight),
					bodyBrush);

				// --- Draw Volume Profile ---
				if (barIdx < barVolumeMaps.Count && barVolumeMaps[barIdx].Count > 0)
				{
					float drawProfileWidth = ProfileWidthPx;

					if (DynamicProfileWidth)
					{
						float nextBarCenterX;
						if (barIdx + 1 < ChartBars.Count)
						{
							nextBarCenterX = chartControl.GetXByBarIndex(ChartBars, barIdx + 1);
						}
						else if (barIdx > 0)
						{
							nextBarCenterX = barCenterX + (barCenterX - chartControl.GetXByBarIndex(ChartBars, barIdx - 1));
						}
						else
						{
							nextBarCenterX = barCenterX + ProfileWidthPx;
						}

						float nextCandleLeft = nextBarCenterX - halfCandle;
						float availableWidth = nextCandleLeft - (candleRight + CandleProfileGapPx);
						
						// Keep at least 2px width, and subtract 1px padding so it doesn't strictly touch the next candle
						drawProfileWidth = Math.Max(2f, availableWidth - 1f);
					}

					DrawBarVolumeProfile(chartScale, barIdx, candleRight + CandleProfileGapPx, panelTop, panelBottom, drawProfileWidth);
				}
			}
		}

		private void DrawBarVolumeProfile(ChartScale chartScale, int barIdx, float profileLeftX, float panelTop, float panelBottom, float drawProfileWidth)
		{
			var volMap = barVolumeMaps[barIdx];
			if (volMap.Count == 0) return;

			long maxVol = 0;
			double pocPrice = double.NaN;
			double vahPrice = double.NaN, valPrice = double.NaN;
			bool haveVA = false;

			bool isActive = barIdx == BarsArray[0].Count - 1;
			double[] cache = barVACache[barIdx];
			bool needsCalc = double.IsNaN(cache[0]) || isActive;

			if (needsCalc)
			{
				foreach (var kvp in volMap)
				{
					if (kvp.Value > maxVol)
					{
						maxVol   = kvp.Value;
						pocPrice = kvp.Key;
					}
				}
				if (maxVol > 0 && ShowValueArea && (ShowVAColor || ShowVALines))
				{
					haveVA = CalcValueArea(volMap, pocPrice, out vahPrice, out valPrice);
				}
				
				if (!isActive)
				{
					cache[0] = vahPrice;
					cache[1] = valPrice;
					cache[2] = pocPrice;
					cache[3] = maxVol;
				}
			}
			else
			{
				vahPrice = cache[0];
			valPrice = cache[1];
				pocPrice = cache[2];
				maxVol   = (long)cache[3];
				haveVA = !double.IsNaN(vahPrice);
			}

			if (maxVol <= 0) return;

			// Get delta map if needed
			Dictionary<double, long> deltaMap = null;
			if (ShowDelta && barIdx < barDeltaMaps.Count && barDeltaMaps[barIdx].Count > 0)
				deltaMap = barDeltaMaps[barIdx];

			double compHeight = TickCompression * TickSize;

			foreach (var kvp in volMap)
			{
				double price = kvp.Key;
				long   vol   = kvp.Value;

				int yTop = chartScale.GetYByValue(price + compHeight);
				int yBot = chartScale.GetYByValue(price);

				if (yBot < panelTop - 20 || yTop > panelBottom + 20) continue;

				int rowHeight = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
				float drawY   = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;

				float barWidth = (float)(drawProfileWidth * (vol / (double)maxVol));
				if (barWidth < 0.5f) continue;

				RectangleF rect = new RectangleF(profileLeftX, drawY, barWidth, rowHeight);

				// Determine if this row is inside the Value Area
				bool insideVA = haveVA && price >= valPrice - TickSize * 0.01 && price <= vahPrice + TickSize * 0.01;

				// Choose brush: POC > Delta > Gradient/Flat
				SolidColorBrush brush;

				if (ShowPOC && Math.Abs(price - pocPrice) < TickSize * 0.01)
				{
					brush = pocBrushDx;
				}
				else if (ShowDelta && deltaMap != null && deltaMap.TryGetValue(price, out long delta))
				{
					brush = delta >= 0 ? posDeltaBrushDx : negDeltaBrushDx;
				}
				else if (UseGradient)
				{
					// Pick gradient palette based on VA membership
					var palette = (ShowValueArea && ShowVAColor && insideVA && vaGradientBrushes != null)
						? vaGradientBrushes
						: volGradientBrushes;

					if (palette != null)
					{
						double ratio = vol / (double)maxVol;
						int steps = palette.Length;
						int gradIdx = (int)(ratio * (steps - 1));
						if (gradIdx < 0) gradIdx = 0;
						if (gradIdx >= steps) gradIdx = steps - 1;
						brush = palette[gradIdx];
					}
					else
					{
						brush = volBrushDx;
					}
				}
				else
				{
					// Flat color: VA color or regular
					brush = (ShowValueArea && ShowVAColor && insideVA) ? vaVolBrushDx : volBrushDx;
				}

				RenderTarget.FillRectangle(rect, brush);
			}

			// --- Draw VA boundary lines ---
			if (haveVA && ShowValueArea && ShowVALines && vaLineBrushDx != null)
			{
				float profileRightX = profileLeftX + drawProfileWidth;

				// VAH line (top of value area)
				float yVAH = chartScale.GetYByValue(vahPrice + compHeight);
				if (yVAH >= panelTop - 5 && yVAH <= panelBottom + 5)
				{
					RenderTarget.DrawLine(
						new Vector2(profileLeftX - 2, yVAH),
						new Vector2(profileRightX + 2, yVAH),
						vaLineBrushDx, VALineThickness, vaLineStrokeDx);
				}

				// VAL line (bottom of value area)
				float yVAL = chartScale.GetYByValue(valPrice);
				if (yVAL >= panelTop - 5 && yVAL <= panelBottom + 5)
				{
					RenderTarget.DrawLine(
						new Vector2(profileLeftX - 2, yVAL),
						new Vector2(profileRightX + 2, yVAL),
						vaLineBrushDx, VALineThickness, vaLineStrokeDx);
				}
			}
		}

		private float MeasureTextWidth(string text)
		{
			if (textFormatDx == null) return 0f;
			if (textWidthCache.TryGetValue(text, out float width))
				return width;

			using (var layout = new TextLayout(Core.Globals.DirectWriteFactory, text, textFormatDx, 1000, 100))
			{
				width = layout.Metrics.Width;
				textWidthCache[text] = width;
				return width;
			}
		}

		private void EnsureDxResources()
		{
			if (bullBodyBrushDx == null)
				bullBodyBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BullishBodyBrush, 1f));
			if (bearBodyBrushDx == null)
				bearBodyBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BearishBodyBrush, 1f));
			if (bullWickBrushDx == null)
				bullWickBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BullishBodyBrush, 1f));
			if (bearWickBrushDx == null)
				bearWickBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BearishBodyBrush, 1f));
			if (volBrushDx == null)
				volBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VolumeBrush, VolumeOpacity));
			if (pocBrushDx == null)
				pocBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(POCBrush, 1f));
			if (posDeltaBrushDx == null)
				posDeltaBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(PositiveDeltaBrush, DeltaOpacity));
			if (negDeltaBrushDx == null)
				negDeltaBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaBrush, DeltaOpacity));

			// VA flat brush
			if (vaVolBrushDx == null)
				vaVolBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VABrush, VolumeOpacity));

			// VA line brush + dashed stroke
			if (vaLineBrushDx == null)
				vaLineBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VALineBrush, 1f));
			if (vaLineStrokeDx == null)
			{
				DashStyle ds;
				switch (VALineStyle)
				{
					case VALineStyleEnum.Solid:  ds = DashStyle.Solid;   break;
					case VALineStyleEnum.Dot:    ds = DashStyle.Dot;     break;
					case VALineStyleEnum.DashDot:ds = DashStyle.DashDot; break;
					default:                     ds = DashStyle.Dash;    break;
				}
				vaLineStrokeDx = new StrokeStyle(RenderTarget.Factory,
					new StrokeStyleProperties { DashStyle = ds });
			}

			// Build gradient palettes
			int steps = Math.Max(2, GradientSteps);

			// Outside-VA gradient
			if (UseGradient && (volGradientBrushes == null || lastBuiltGradientSteps != steps))
			{
				if (volGradientBrushes != null)
					for (int i = 0; i < volGradientBrushes.Length; i++)
						volGradientBrushes[i]?.Dispose();

				volGradientBrushes = BuildGradientPalette(VolumeBrush, steps);
				lastBuiltGradientSteps = steps;
			}

			// Inside-VA gradient
			if (UseGradient && ShowValueArea && ShowVAColor && (vaGradientBrushes == null || lastBuiltVAGradientSteps != steps))
			{
				if (vaGradientBrushes != null)
					for (int i = 0; i < vaGradientBrushes.Length; i++)
						vaGradientBrushes[i]?.Dispose();

				vaGradientBrushes = BuildGradientPalette(VABrush, steps);
				lastBuiltVAGradientSteps = steps;
			}
		}

		private SolidColorBrush[] BuildGradientPalette(WpfBrush baseBrush, int steps)
		{
			var baseColor = BrushToMediaColor(baseBrush);
			var palette = new SolidColorBrush[steps];

			for (int i = 0; i < steps; i++)
			{
				float t = i / (float)(steps - 1);
				float brightness = MinBrightness + t * (1f - MinBrightness);

				var c = new Color4(
					(baseColor.R / 255f) * brightness,
					(baseColor.G / 255f) * brightness,
					(baseColor.B / 255f) * brightness,
					(baseColor.A / 255f) * VolumeOpacity);

				palette[i] = new SolidColorBrush(RenderTarget, c);
			}

			return palette;
		}

		private static System.Windows.Media.Color BrushToMediaColor(WpfBrush b)
		{
			return (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
		}

		private Color4 ToDxColor(WpfBrush b, float alphaMult)
		{
			var c = BrushToMediaColor(b);
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alphaMult);
		}
		#endregion

		#region Properties

		// --- Data ---
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Tick Compression", GroupName = "Data", Order = 0)]
		public int TickCompression { get; set; }

		// --- Layout ---
		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "Candle Width (px)", GroupName = "Layout", Order = 1)]
		public int CandleWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Profile Width (px)", GroupName = "Layout", Order = 2)]
		public int ProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Profile Width", Description = "Dynamically adjusts profile width to fit between candles", GroupName = "Layout", Order = 3)]
		public bool DynamicProfileWidth { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Candle-Profile Gap (px)", GroupName = "Layout", Order = 4)]
		public int CandleProfileGapPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Profile Bar Spacing (px)", GroupName = "Layout", Order = 5)]
		public int ProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Range(1, 6)]
		[Display(Name = "Wick Width (px)", GroupName = "Layout", Order = 6)]
		public int WickWidthPx { get; set; }

		// --- Visibility ---
		[NinjaScriptProperty]
		[Display(Name = "Show POC", GroupName = "Visibility", Order = 10)]
		public bool ShowPOC { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta", GroupName = "Visibility", Order = 11)]
		public bool ShowDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Gradient", GroupName = "Visibility", Order = 12)]
		public bool UseGradient { get; set; }

		[NinjaScriptProperty]
		[Range(2, 64)]
		[Display(Name = "Gradient Steps", GroupName = "Visibility", Order = 13)]
		public int GradientSteps { get; set; }

		// --- Value Area ---
		[NinjaScriptProperty]
		[Display(Name = "Show Value Area", GroupName = "Value Area", Order = 20)]
		public bool ShowValueArea { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Color Mode", Description = "Color rows inside the Value Area differently", GroupName = "Value Area", Order = 21)]
		public bool ShowVAColor { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Boundary Lines", Description = "Draw dashed lines at VAH and VAL", GroupName = "Value Area", Order = 22)]
		public bool ShowVALines { get; set; }

		[NinjaScriptProperty]
		[Range(50, 95)]
		[Display(Name = "VA Percent", GroupName = "Value Area", Order = 23)]
		public int ValueAreaPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 6.0)]
		[Display(Name = "VA Line Thickness", GroupName = "Value Area", Order = 24)]
		public float VALineThickness { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Line Style", GroupName = "Value Area", Order = 25)]
		public VALineStyleEnum VALineStyle { get; set; }

		[XmlIgnore]
		[Display(Name = "VA Color", GroupName = "Value Area", Order = 26)]
		public WpfBrush VABrush { get; set; }
		[Browsable(false)]
		public string VABrushSerialize
		{ get { return Serialize.BrushToString(VABrush); } set { VABrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "VA Line Color", GroupName = "Value Area", Order = 27)]
		public WpfBrush VALineBrush { get; set; }
		[Browsable(false)]
		public string VALineBrushSerialize
		{ get { return Serialize.BrushToString(VALineBrush); } set { VALineBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Candles ---
		[XmlIgnore]
		[Display(Name = "Bullish Body", GroupName = "Colors", Order = 30)]
		public WpfBrush BullishBodyBrush { get; set; }
		[Browsable(false)]
		public string BullishBodyBrushSerialize
		{ get { return Serialize.BrushToString(BullishBodyBrush); } set { BullishBodyBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Body", GroupName = "Colors", Order = 31)]
		public WpfBrush BearishBodyBrush { get; set; }
		[Browsable(false)]
		public string BearishBodyBrushSerialize
		{ get { return Serialize.BrushToString(BearishBodyBrush); } set { BearishBodyBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Profile ---
		[XmlIgnore]
		[Display(Name = "Volume Color", GroupName = "Colors", Order = 32)]
		public WpfBrush VolumeBrush { get; set; }
		[Browsable(false)]
		public string VolumeBrushSerialize
		{ get { return Serialize.BrushToString(VolumeBrush); } set { VolumeBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.05, 1.0)]
		[Display(Name = "Min Brightness", GroupName = "Colors", Order = 33)]
		public float MinBrightness { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Volume Opacity", GroupName = "Colors", Order = 34)]
		public float VolumeOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Color", GroupName = "Colors", Order = 35)]
		public WpfBrush POCBrush { get; set; }
		[Browsable(false)]
		public string POCBrushSerialize
		{ get { return Serialize.BrushToString(POCBrush); } set { POCBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Delta ---
		[XmlIgnore]
		[Display(Name = "Positive Delta", GroupName = "Colors", Order = 36)]
		public WpfBrush PositiveDeltaBrush { get; set; }
		[Browsable(false)]
		public string PositiveDeltaBrushSerialize
		{ get { return Serialize.BrushToString(PositiveDeltaBrush); } set { PositiveDeltaBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Negative Delta", GroupName = "Colors", Order = 37)]
		public WpfBrush NegativeDeltaBrush { get; set; }
		[Browsable(false)]
		public string NegativeDeltaBrushSerialize
		{ get { return Serialize.BrushToString(NegativeDeltaBrush); } set { NegativeDeltaBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Delta Opacity", GroupName = "Colors", Order = 38)]
		public float DeltaOpacity { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaCandleVolumeProfile[] cacheOrcaCandleVolumeProfile;
		public OrcaCandleVolumeProfile OrcaCandleVolumeProfile(int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return OrcaCandleVolumeProfile(Input, tickCompression, candleWidthPx, profileWidthPx, dynamicProfileWidth, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showPOC, showDelta, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}

		public OrcaCandleVolumeProfile OrcaCandleVolumeProfile(ISeries<double> input, int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			if (cacheOrcaCandleVolumeProfile != null)
				for (int idx = 0; idx < cacheOrcaCandleVolumeProfile.Length; idx++)
					if (cacheOrcaCandleVolumeProfile[idx] != null && cacheOrcaCandleVolumeProfile[idx].TickCompression == tickCompression && cacheOrcaCandleVolumeProfile[idx].CandleWidthPx == candleWidthPx && cacheOrcaCandleVolumeProfile[idx].ProfileWidthPx == profileWidthPx && cacheOrcaCandleVolumeProfile[idx].DynamicProfileWidth == dynamicProfileWidth && cacheOrcaCandleVolumeProfile[idx].CandleProfileGapPx == candleProfileGapPx && cacheOrcaCandleVolumeProfile[idx].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaCandleVolumeProfile[idx].WickWidthPx == wickWidthPx && cacheOrcaCandleVolumeProfile[idx].ShowPOC == showPOC && cacheOrcaCandleVolumeProfile[idx].ShowDelta == showDelta && cacheOrcaCandleVolumeProfile[idx].UseGradient == useGradient && cacheOrcaCandleVolumeProfile[idx].GradientSteps == gradientSteps && cacheOrcaCandleVolumeProfile[idx].ShowValueArea == showValueArea && cacheOrcaCandleVolumeProfile[idx].ShowVAColor == showVAColor && cacheOrcaCandleVolumeProfile[idx].ShowVALines == showVALines && cacheOrcaCandleVolumeProfile[idx].ValueAreaPercent == valueAreaPercent && cacheOrcaCandleVolumeProfile[idx].VALineThickness == vALineThickness && cacheOrcaCandleVolumeProfile[idx].VALineStyle == vALineStyle && cacheOrcaCandleVolumeProfile[idx].MinBrightness == minBrightness && cacheOrcaCandleVolumeProfile[idx].VolumeOpacity == volumeOpacity && cacheOrcaCandleVolumeProfile[idx].DeltaOpacity == deltaOpacity && cacheOrcaCandleVolumeProfile[idx].EqualsInput(input))
						return cacheOrcaCandleVolumeProfile[idx];
			return CacheIndicator<OrcaCandleVolumeProfile>(new OrcaCandleVolumeProfile(){ TickCompression = tickCompression, CandleWidthPx = candleWidthPx, ProfileWidthPx = profileWidthPx, DynamicProfileWidth = dynamicProfileWidth, CandleProfileGapPx = candleProfileGapPx, ProfileBarSpacingPx = profileBarSpacingPx, WickWidthPx = wickWidthPx, ShowPOC = showPOC, ShowDelta = showDelta, UseGradient = useGradient, GradientSteps = gradientSteps, ShowValueArea = showValueArea, ShowVAColor = showVAColor, ShowVALines = showVALines, ValueAreaPercent = valueAreaPercent, VALineThickness = vALineThickness, VALineStyle = vALineStyle, MinBrightness = minBrightness, VolumeOpacity = volumeOpacity, DeltaOpacity = deltaOpacity }, input, ref cacheOrcaCandleVolumeProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaCandleVolumeProfile OrcaCandleVolumeProfile(int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return indicator.OrcaCandleVolumeProfile(Input, tickCompression, candleWidthPx, profileWidthPx, dynamicProfileWidth, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showPOC, showDelta, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}

		public Indicators.OrcaCandleVolumeProfile OrcaCandleVolumeProfile(ISeries<double> input , int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return indicator.OrcaCandleVolumeProfile(input, tickCompression, candleWidthPx, profileWidthPx, dynamicProfileWidth, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showPOC, showDelta, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaCandleVolumeProfile OrcaCandleVolumeProfile(int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return indicator.OrcaCandleVolumeProfile(Input, tickCompression, candleWidthPx, profileWidthPx, dynamicProfileWidth, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showPOC, showDelta, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}

		public Indicators.OrcaCandleVolumeProfile OrcaCandleVolumeProfile(ISeries<double> input , int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return indicator.OrcaCandleVolumeProfile(input, tickCompression, candleWidthPx, profileWidthPx, dynamicProfileWidth, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showPOC, showDelta, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}
	}
}

#endregion
