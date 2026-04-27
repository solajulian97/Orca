#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
	public enum CumulativeDeltaMode
	{
		BidAsk,
		TickDirection
	}

	public class OrcaCumulativeDelta : Indicator
	{
		#region Private Fields
		private double	lastBid;
		private double	lastAsk;
		private double	prevLast;
		private double	runningDelta;
		private int		lastPrimaryBarProcessed;
		private int		lastDirection;  // +1 or -1, carries forward for unchanged ticks

		private List<double>	barDeltaOpen;
		private List<double>	barDeltaHigh;
		private List<double>	barDeltaLow;
		private List<double>	barDeltaClose;
		private List<bool>		barHasData;

		// SharpDX brushes
		private SharpDX.Direct2D1.Brush	dxUpFillBrush;
		private SharpDX.Direct2D1.Brush	dxDownFillBrush;
		private SharpDX.Direct2D1.Brush	dxUpBorderBrush;
		private SharpDX.Direct2D1.Brush	dxDownBorderBrush;
		private SharpDX.Direct2D1.Brush	dxWickBrush;
		private SharpDX.Direct2D1.Brush	dxZeroBrush;
		private SharpDX.Direct2D1.Brush	dxPriceLineBrush;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "OrcaCumulativeDelta";
				Description					= "Cumulative delta OHLC candles on a separate panel.";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				DisplayInDataBox			= true;
				IsSuspendedWhileInactive	= true;
				BarsRequiredToPlot			= 0;

				// Visual parameters — OTM style
				ColorUp				= Brushes.DodgerBlue;
				ColorDown			= Brushes.Tomato;
				ColorUpBorder		= Brushes.DodgerBlue;
				ColorDownBorder		= Brushes.Tomato;
				BarOpacity			= 0.5;
				BorderOpacity		= 1.0;
				WickColor			= Brushes.White;
				ZeroLineColor		= Brushes.DimGray;
				ZeroLineWidth		= 1;
				BarWidthPercent		= 90;
				ShowPriceLine		= true;
				PriceLineWidth		= 1;
				DeltaMode			= CumulativeDeltaMode.BidAsk;

				// DeltaClose is Values[0] so NT's right-side live label tracks the current delta close.
				// DeltaHigh / DeltaLow are Values[1]/[2] to ensure the scale covers the full range.
				// Lines are never drawn because base.OnRender() is not called.
				AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "DeltaClose");
				AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "DeltaHigh");
				AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "DeltaLow");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				barDeltaOpen	= new List<double>(4096);
				barDeltaHigh	= new List<double>(4096);
				barDeltaLow		= new List<double>(4096);
				barDeltaClose	= new List<double>(4096);
				barHasData		= new List<bool>(4096);

				lastBid			= double.NaN;
				lastAsk			= double.NaN;
				prevLast		= double.NaN;
				runningDelta	= 0;
				lastPrimaryBarProcessed = -1;
				lastDirection	= 0;
			}
			else if (State == State.Terminated)
			{
				DisposeDxResources();
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			// Track bid/ask for real-time classification
			if (e.MarketDataType == MarketDataType.Bid)
				lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask)
				lastAsk = e.Price;
			else if (e.MarketDataType == MarketDataType.Last)
			{
				// During tick replay, e.Ask/e.Bid on Last events carry bid/ask
				if (e.Ask > 0 && !double.IsNaN(e.Ask)) lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid)) lastBid = e.Bid;
			}
		}

		private void EnsureBarLists(int idx)
		{
			while (barDeltaOpen.Count <= idx)
			{
				barDeltaOpen.Add(0);
				barDeltaHigh.Add(0);
				barDeltaLow.Add(0);
				barDeltaClose.Add(0);
				barHasData.Add(false);
			}
		}

		protected override void OnBarUpdate()
		{
			// ============================================
			// BarsInProgress == 1 : each tick
			// ============================================
			if (BarsInProgress == 1)
			{
				double price = Close[0];
				long   vol   = (long)Volume[0];
				if (vol <= 0) return;

				if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
					vol = (long)Core.Globals.ToCryptocurrencyVolume(vol);

				int primaryIdx = BarsArray[0].GetBar(Time[0]);
				if (primaryIdx < 0) return;

				EnsureBarLists(primaryIdx);

				// Track bar transitions
				if (primaryIdx != lastPrimaryBarProcessed)
					lastPrimaryBarProcessed = primaryIdx;

				long signed = 0;

				if (DeltaMode == CumulativeDeltaMode.BidAsk
					&& !double.IsNaN(lastAsk) && !double.IsNaN(lastBid)
					&& lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
				{
					// Bid/Ask mode: classify against the spread, fall back to tick direction mid-spread
					if (price >= lastAsk)
						signed = +vol;
					else if (price <= lastBid)
						signed = -vol;
					else if (!double.IsNaN(prevLast))
					{
						if (price > prevLast) signed = +vol;
						else if (price < prevLast) signed = -vol;
						else signed = lastDirection * vol;
					}
				}
				else if (!double.IsNaN(prevLast))
				{
					// TickDirection mode (or BidAsk fallback when no quote data yet)
					if (price > prevLast) signed = +vol;
					else if (price < prevLast) signed = -vol;
					else signed = lastDirection * vol;
				}

				prevLast = price;
				if (signed > 0) lastDirection = 1;
				else if (signed < 0) lastDirection = -1;
				// if signed == 0 (first tick ever), keep lastDirection as-is
				if (signed == 0) return;

				runningDelta += signed;

				if (!barHasData[primaryIdx])
				{
					barDeltaOpen[primaryIdx]  = runningDelta;
					barDeltaHigh[primaryIdx]  = runningDelta;
					barDeltaLow[primaryIdx]   = runningDelta;
					barDeltaClose[primaryIdx] = runningDelta;
					barHasData[primaryIdx]    = true;
				}
				else
				{
					barDeltaClose[primaryIdx] = runningDelta;
					if (runningDelta > barDeltaHigh[primaryIdx])
						barDeltaHigh[primaryIdx] = runningDelta;
					if (runningDelta < barDeltaLow[primaryIdx])
						barDeltaLow[primaryIdx] = runningDelta;
				}
				return;
			}

			// ============================================
			// BarsInProgress == 0 : primary bar
			// ============================================
			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			EnsureBarLists(CurrentBar);

			// Session reset using NinjaTrader's proper session detection
			if (Bars.IsFirstBarOfSession)
			{
				runningDelta = 0;
				prevLast = double.NaN;
			}

			// Values[0]=DeltaClose drives the live right-axis label; [1]=High, [2]=Low drive scale range.
			if (CurrentBar < barDeltaClose.Count && barHasData[CurrentBar])
			{
				Values[0][0] = barDeltaClose[CurrentBar];  // DeltaClose — primary live label
				Values[1][0] = barDeltaHigh[CurrentBar];   // DeltaHigh  — scale max
				Values[2][0] = barDeltaLow[CurrentBar];    // DeltaLow   — scale min
			}
			else
			{
				Values[0][0] = double.NaN;
				Values[1][0] = double.NaN;
				Values[2][0] = double.NaN;
			}
		}

		#region OnRender — OTM-style OHLC delta candles
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (chartControl == null || chartScale == null || Bars == null)
				return;
			if (ChartBars == null || barDeltaOpen == null)
				return;

			int fromIdx = ChartBars.FromIndex;
			int toIdx   = ChartBars.ToIndex;
			if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx)
				return;

			EnsureDxResources();
			if (dxUpFillBrush == null)
				return;

			AntialiasMode oldMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = AntialiasMode.Aliased;

			float panelX = ChartPanel.X;
			float panelW = ChartPanel.W;
			float panelY = ChartPanel.Y;
			float panelH = ChartPanel.H;

			// Zero line
			float zeroY = chartScale.GetYByValue(0);
			if (zeroY >= panelY && zeroY <= panelY + panelH)
			{
				RenderTarget.DrawLine(
					new Vector2(panelX, zeroY),
					new Vector2(panelX + panelW, zeroY),
					dxZeroBrush, ZeroLineWidth);
			}

			// Delta candles
			for (int barIdx = fromIdx; barIdx <= toIdx; barIdx++)
			{
				if (barIdx < 0 || barIdx >= barDeltaOpen.Count || !barHasData[barIdx])
					continue;

				double dO = barDeltaOpen[barIdx];
				double dH = barDeltaHigh[barIdx];
				double dL = barDeltaLow[barIdx];
				double dC = barDeltaClose[barIdx];

				bool isUp = dC >= dO;

				float yOpen  = chartScale.GetYByValue(dO);
				float yHigh  = chartScale.GetYByValue(dH);
				float yLow   = chartScale.GetYByValue(dL);
				float yClose = chartScale.GetYByValue(dC);

			// Calculate bar pixel spacing from adjacent bars (independent of chart bar width)
				float barX = chartControl.GetXByBarIndex(ChartBars, barIdx);
				float barSpacing;
				if (barIdx < toIdx)
					barSpacing = chartControl.GetXByBarIndex(ChartBars, barIdx + 1) - barX;
				else if (barIdx > fromIdx)
					barSpacing = barX - chartControl.GetXByBarIndex(ChartBars, barIdx - 1);
				else
					barSpacing = (float)chartControl.BarWidth;

				float halfW = (float)(barSpacing * BarWidthPercent / 100.0 / 2.0);
				if (halfW < 1f) halfW = 1f;

				var fillBrush   = isUp ? dxUpFillBrush   : dxDownFillBrush;
				var borderBrush = isUp ? dxUpBorderBrush  : dxDownBorderBrush;

				// Body rect
				float bTop = Math.Min(yOpen, yClose);
				float bBot = Math.Max(yOpen, yClose);
				float bH   = bBot - bTop;
				if (bH < 1f) bH = 1f;

				// 1) Wick — draw ONLY above and below body, color matches candle direction
				if (yHigh < bTop)
					RenderTarget.DrawLine(new Vector2(barX, yHigh), new Vector2(barX, bTop), borderBrush, 1f);
				if (yLow > bBot)
					RenderTarget.DrawLine(new Vector2(barX, bBot), new Vector2(barX, yLow), borderBrush, 1f);

				// 2) Body fill (semi-transparent)
				var bodyRect = new RectangleF(barX - halfW, bTop, halfW * 2, bH);
				RenderTarget.FillRectangle(bodyRect, fillBrush);

				// 3) Body border
				RenderTarget.DrawRectangle(bodyRect, borderBrush, 1f);
			}

			// Price line — walk backward from toIdx to find the last bar with real data
			// (toIdx may point to an empty future slot when there's blank space on the right)
			if (ShowPriceLine)
			{
				int lastData = toIdx;
				while (lastData >= fromIdx && (lastData >= barDeltaClose.Count || !barHasData[lastData]))
					lastData--;

				if (lastData >= fromIdx)
				{
					double lastClose = barDeltaClose[lastData];
					double lastOpen  = barDeltaOpen[lastData];
					bool   lineIsUp  = lastClose >= lastOpen;
					var    plBrush   = lineIsUp ? dxUpBorderBrush : dxDownBorderBrush;

					float lineY    = chartScale.GetYByValue(lastClose);
					float lastBarX = chartControl.GetXByBarIndex(ChartBars, lastData);
					float rightX   = panelX + panelW;

					if (lineY >= panelY && lineY <= panelY + panelH && lastBarX < rightX)
						RenderTarget.DrawLine(
							new Vector2(lastBarX, lineY),
							new Vector2(rightX,   lineY),
							plBrush, (float)PriceLineWidth);
				}
			}

			RenderTarget.AntialiasMode = oldMode;
		}
		#endregion

		#region DX Resources
		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			if (dxUpFillBrush == null)
			{
				float fillOpacity   = (float)Math.Max(0.0, Math.Min(1.0, BarOpacity));
				float borderOpacity = (float)Math.Max(0.0, Math.Min(1.0, BorderOpacity));

				dxUpFillBrush         = ColorUp.ToDxBrush(RenderTarget);
				dxUpFillBrush.Opacity = fillOpacity;
				dxDownFillBrush         = ColorDown.ToDxBrush(RenderTarget);
				dxDownFillBrush.Opacity = fillOpacity;

				dxUpBorderBrush           = ColorUpBorder.ToDxBrush(RenderTarget);
				dxUpBorderBrush.Opacity   = borderOpacity;
				dxDownBorderBrush           = ColorDownBorder.ToDxBrush(RenderTarget);
				dxDownBorderBrush.Opacity   = borderOpacity;
				dxWickBrush			= WickColor.ToDxBrush(RenderTarget);
				dxWickBrush.Opacity = borderOpacity;
				dxZeroBrush			= ZeroLineColor.ToDxBrush(RenderTarget);
				dxPriceLineBrush    = Brushes.White.ToDxBrush(RenderTarget);
			}
		}

		private void DisposeDxResources()
		{
			if (dxUpFillBrush	  != null) { dxUpFillBrush.Dispose();	  dxUpFillBrush		= null; }
			if (dxDownFillBrush	  != null) { dxDownFillBrush.Dispose();	  dxDownFillBrush	= null; }
			if (dxUpBorderBrush	  != null) { dxUpBorderBrush.Dispose();	  dxUpBorderBrush	= null; }
			if (dxDownBorderBrush != null) { dxDownBorderBrush.Dispose(); dxDownBorderBrush	= null; }
			if (dxWickBrush		    != null) { dxWickBrush.Dispose();		    dxWickBrush		    = null; }
			if (dxZeroBrush		    != null) { dxZeroBrush.Dispose();		    dxZeroBrush		    = null; }
			if (dxPriceLineBrush    != null) { dxPriceLineBrush.Dispose();     dxPriceLineBrush    = null; }
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}
		#endregion

		#region Properties

		[XmlIgnore]
		[Display(Name = "Color Up", Order = 1, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorUp { get; set; }
		[Browsable(false)]
		public string ColorUpSerialize { get { return Serialize.BrushToString(ColorUp); } set { ColorUp = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Color Down", Order = 2, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorDown { get; set; }
		[Browsable(false)]
		public string ColorDownSerialize { get { return Serialize.BrushToString(ColorDown); } set { ColorDown = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Color Up Border", Order = 3, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorUpBorder { get; set; }
		[Browsable(false)]
		public string ColorUpBorderSerialize { get { return Serialize.BrushToString(ColorUpBorder); } set { ColorUpBorder = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Color Down Border", Order = 4, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorDownBorder { get; set; }
		[Browsable(false)]
		public string ColorDownBorderSerialize { get { return Serialize.BrushToString(ColorDownBorder); } set { ColorDownBorder = Serialize.StringToBrush(value); } }

		[Range(0.0, 1.0)]
		[Display(Name = "Bar Opacity", Order = 5, GroupName = "Visual Parameters")]
		public double BarOpacity { get; set; }

		[Range(0.0, 1.0)]
		[Display(Name = "Border Opacity", Order = 6, GroupName = "Visual Parameters")]
		public double BorderOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Wick Color", Order = 6, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush WickColor { get; set; }
		[Browsable(false)]
		public string WickColorSerialize { get { return Serialize.BrushToString(WickColor); } set { WickColor = Serialize.StringToBrush(value); } }

		[Range(1, 100)]
		[Display(Name = "Bar Width %", Order = 8, GroupName = "Visual Parameters")]
		public int BarWidthPercent { get; set; }

		[Display(Name = "Show Price Line", Order = 1, GroupName = "Price Line")]
		public bool ShowPriceLine { get; set; }

		[Range(1, 5)]
		[Display(Name = "Price Line Width", Order = 2, GroupName = "Price Line")]
		public int PriceLineWidth { get; set; }

		[XmlIgnore]
		[Display(Name = "Zero Line Color", Order = 1, GroupName = "Reference Levels")]
		public System.Windows.Media.Brush ZeroLineColor { get; set; }
		[Browsable(false)]
		public string ZeroLineColorSerialize { get { return Serialize.BrushToString(ZeroLineColor); } set { ZeroLineColor = Serialize.StringToBrush(value); } }

		[Range(1, 5)]
		[Display(Name = "Zero Line Width", Order = 2, GroupName = "Reference Levels")]
		public int ZeroLineWidth { get; set; }

		[Display(Name = "Delta Mode", Order = 1, GroupName = "Delta Calculation",
			Description = "BidAsk: classifies each trade against the bid/ask spread (most accurate live). TickDirection: classifies by whether price moved up or down tick-to-tick (works historically and live).")]
		public CumulativeDeltaMode DeltaMode { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaCumulativeDelta[] cacheOrcaCumulativeDelta;
		public OrcaCumulativeDelta OrcaCumulativeDelta()
		{
			return OrcaCumulativeDelta(Input);
		}

		public OrcaCumulativeDelta OrcaCumulativeDelta(ISeries<double> input)
		{
			if (cacheOrcaCumulativeDelta != null)
				for (int idx = 0; idx < cacheOrcaCumulativeDelta.Length; idx++)
					if (cacheOrcaCumulativeDelta[idx] != null &&  cacheOrcaCumulativeDelta[idx].EqualsInput(input))
						return cacheOrcaCumulativeDelta[idx];
			return CacheIndicator<OrcaCumulativeDelta>(new OrcaCumulativeDelta(), input, ref cacheOrcaCumulativeDelta);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaCumulativeDelta OrcaCumulativeDelta()
		{
			return indicator.OrcaCumulativeDelta(Input);
		}

		public Indicators.OrcaCumulativeDelta OrcaCumulativeDelta(ISeries<double> input )
		{
			return indicator.OrcaCumulativeDelta(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaCumulativeDelta OrcaCumulativeDelta()
		{
			return indicator.OrcaCumulativeDelta(Input);
		}

		public Indicators.OrcaCumulativeDelta OrcaCumulativeDelta(ISeries<double> input )
		{
			return indicator.OrcaCumulativeDelta(input);
		}
	}
}

#endregion
