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
	public enum TDIDisplayMode
	{
		CumulativeLine,
		BarHistogram,
		RatioLine
	}

	public class OrcaTickDirectionIndex : Indicator
	{
		#region Private Fields
		private double	prevLast;
		private double	runningTickDelta;
		private int		lastPrimaryBarProcessed;

		// Per-bar storage
		private List<double>	barTickDelta;		// net signed volume per bar
		private List<double>	barCumTickDelta;	// running cumulative tick delta snapshot (close)
		private List<double>	barCumOpen;			// cumulative OHLC
		private List<double>	barCumHigh;
		private List<double>	barCumLow;
		private List<double>	barUptickVol;		// total volume on upticks
		private List<double>	barDowntickVol;		// total volume on downticks
		private List<double>	barUnchangedVol;	// volume on unchanged ticks
		private List<int>		barUnchangedCount;	// count of unchanged ticks
		private List<bool>		barHasData;
		private List<bool>		barCumFirstTick;	// whether cumulative OHLC has been initialized

		// SharpDX brushes
		private SharpDX.Direct2D1.Brush	dxUpBrush;
		private SharpDX.Direct2D1.Brush	dxDownBrush;
		private SharpDX.Direct2D1.Brush	dxUpBorderBrush;
		private SharpDX.Direct2D1.Brush	dxDownBorderBrush;
		private SharpDX.Direct2D1.Brush	dxZeroBrush;
		private SharpDX.Direct2D1.Brush	dxNeutralBrush;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "OrcaTickDirectionIndex";
				Description					= "Tick Direction Index (Rewarded Effort Index) — tracks volume classified by tick direction.";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				DisplayInDataBox			= true;
				IsSuspendedWhileInactive	= true;
				BarsRequiredToPlot			= 0;

				// Display mode
				Mode				= TDIDisplayMode.BarHistogram;
				ResetOnSession		= true;

				// Visual parameters
				ColorUp				= Brushes.DodgerBlue;
				ColorDown			= Brushes.Tomato;
				ColorUpBorder		= Brushes.DodgerBlue;
				ColorDownBorder		= Brushes.Tomato;
				NeutralColor		= Brushes.Gray;
				BarOpacity			= 0.5;
				BarWidthPercent		= 90;
				ZeroLineColor		= Brushes.DimGray;
				ZeroLineWidth		= 1;

				// Visible plot drives auto-scaling
				AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "TickDelta");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				barTickDelta		= new List<double>(4096);
				barCumTickDelta		= new List<double>(4096);
				barCumOpen			= new List<double>(4096);
				barCumHigh			= new List<double>(4096);
				barCumLow			= new List<double>(4096);
				barUptickVol		= new List<double>(4096);
				barDowntickVol		= new List<double>(4096);
				barUnchangedVol		= new List<double>(4096);
				barUnchangedCount	= new List<int>(4096);
				barHasData			= new List<bool>(4096);
				barCumFirstTick		= new List<bool>(4096);

				prevLast			= double.NaN;
				runningTickDelta	= 0;
				lastPrimaryBarProcessed = -1;
			}
			else if (State == State.Terminated)
			{
				DisposeDxResources();
			}
		}

		private void EnsureBarLists(int idx)
		{
			while (barTickDelta.Count <= idx)
			{
				barTickDelta.Add(0);
				barCumTickDelta.Add(0);
				barCumOpen.Add(0);
				barCumHigh.Add(0);
				barCumLow.Add(0);
				barUptickVol.Add(0);
				barDowntickVol.Add(0);
				barUnchangedVol.Add(0);
				barUnchangedCount.Add(0);
				barHasData.Add(false);
				barCumFirstTick.Add(false);
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

				if (primaryIdx != lastPrimaryBarProcessed)
					lastPrimaryBarProcessed = primaryIdx;

				// Tick direction classification
				long signed = 0;

				if (!double.IsNaN(prevLast))
				{
					if (price > prevLast)
						signed = +vol;		// uptick
					else if (price < prevLast)
						signed = -vol;		// downtick
					// price == prevLast → unchanged
				}

				prevLast = price;

				// Track unchanged ticks
				if (signed == 0)
				{
					barUnchangedVol[primaryIdx]   += vol;
					barUnchangedCount[primaryIdx] += 1;
					// Still mark bar as having data even for unchanged
					barHasData[primaryIdx] = true;
					return;
				}

				// Accumulate directional volume
				if (signed > 0)
					barUptickVol[primaryIdx] += vol;
				else
					barDowntickVol[primaryIdx] += vol;

				barTickDelta[primaryIdx] += signed;
				runningTickDelta += signed;
				barCumTickDelta[primaryIdx] = runningTickDelta;

				// Track cumulative OHLC
				if (!barCumFirstTick[primaryIdx])
				{
					barCumOpen[primaryIdx] = runningTickDelta;
					barCumHigh[primaryIdx] = runningTickDelta;
					barCumLow[primaryIdx]  = runningTickDelta;
					barCumFirstTick[primaryIdx] = true;
				}
				else
				{
					if (runningTickDelta > barCumHigh[primaryIdx])
						barCumHigh[primaryIdx] = runningTickDelta;
					if (runningTickDelta < barCumLow[primaryIdx])
						barCumLow[primaryIdx] = runningTickDelta;
				}

				barHasData[primaryIdx] = true;
				return;
			}

			// ============================================
			// BarsInProgress == 0 : primary bar
			// ============================================
			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			EnsureBarLists(CurrentBar);

			if (ResetOnSession && Bars.IsFirstBarOfSession)
			{
				runningTickDelta = 0;
				prevLast = double.NaN;
			}

			// Drive auto-scaling via the plot
			if (CurrentBar < barHasData.Count && barHasData[CurrentBar])
			{
				switch (Mode)
				{
					case TDIDisplayMode.CumulativeLine:
						Value[0] = barCumTickDelta[CurrentBar];
						break;
					case TDIDisplayMode.BarHistogram:
						Value[0] = barTickDelta[CurrentBar];
						break;
					case TDIDisplayMode.RatioLine:
						double up   = barUptickVol[CurrentBar];
						double down = barDowntickVol[CurrentBar];
						double total = up + down;
						Value[0] = total > 0 ? up / total : 0.5;
						break;
				}
			}
			else
			{
				Value[0] = double.NaN;
			}
		}

		#region OnRender — Custom histogram / line rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			// Base renders the DimGray line (drives auto-scaling)
			base.OnRender(chartControl, chartScale);

			if (chartControl == null || chartScale == null || Bars == null)
				return;
			if (ChartBars == null || barTickDelta == null)
				return;

			int fromIdx = ChartBars.FromIndex;
			int toIdx   = ChartBars.ToIndex;
			if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx)
				return;

			EnsureDxResources();
			if (dxUpBrush == null)
				return;

			AntialiasMode oldMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = AntialiasMode.Aliased;

			float panelX = ChartPanel.X;
			float panelW = ChartPanel.W;
			float panelY = ChartPanel.Y;
			float panelH = ChartPanel.H;

			// Reference line
			double refValue = (Mode == TDIDisplayMode.RatioLine) ? 0.5 : 0.0;
			float refY = chartScale.GetYByValue(refValue);
			if (refY >= panelY && refY <= panelY + panelH)
			{
				RenderTarget.DrawLine(
					new Vector2(panelX, refY),
					new Vector2(panelX + panelW, refY),
					dxZeroBrush, ZeroLineWidth);
			}

			// Render bars/candles based on mode
			if (Mode == TDIDisplayMode.BarHistogram || Mode == TDIDisplayMode.CumulativeLine)
			{
				for (int barIdx = fromIdx; barIdx <= toIdx; barIdx++)
				{
					if (barIdx < 0 || barIdx >= barTickDelta.Count || !barHasData[barIdx])
						continue;

					// Calculate bar pixel spacing from adjacent bars
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

					if (Mode == TDIDisplayMode.BarHistogram)
					{
						// Simple histogram bar from zero
						double val = barTickDelta[barIdx];
						if (val == 0) continue;

						bool isUp = val > 0;
						float yVal  = chartScale.GetYByValue(val);
						float yZero = chartScale.GetYByValue(0);

						var fillBrush = isUp ? dxUpBrush : dxDownBrush;

						float bTop = Math.Min(yVal, yZero);
						float bBot = Math.Max(yVal, yZero);
						float bH   = bBot - bTop;
						if (bH < 1f) bH = 1f;

						var barRect = new RectangleF(barX - halfW, bTop, halfW * 2, bH);
						RenderTarget.FillRectangle(barRect, fillBrush);
						RenderTarget.DrawRectangle(barRect, fillBrush, 1f);
					}
					else // CumulativeLine → OHLC candles
					{
						if (!barCumFirstTick[barIdx]) continue;

						double dO = barCumOpen[barIdx];
						double dH = barCumHigh[barIdx];
						double dL = barCumLow[barIdx];
						double dC = barCumTickDelta[barIdx];

						bool isUp = dC >= dO;

						float yOpen  = chartScale.GetYByValue(dO);
						float yHigh  = chartScale.GetYByValue(dH);
						float yLow   = chartScale.GetYByValue(dL);
						float yClose = chartScale.GetYByValue(dC);

						var fillBrush   = isUp ? dxUpBrush       : dxDownBrush;
						var borderBrush = isUp ? dxUpBorderBrush : dxDownBorderBrush;

						// Body
						float bTop = Math.Min(yOpen, yClose);
						float bBot = Math.Max(yOpen, yClose);
						float bH   = bBot - bTop;
						if (bH < 1f) bH = 1f;

						// Wicks — color matches candle direction
						if (yHigh < bTop)
							RenderTarget.DrawLine(new Vector2(barX, yHigh), new Vector2(barX, bTop), borderBrush, 1f);
						if (yLow > bBot)
							RenderTarget.DrawLine(new Vector2(barX, bBot), new Vector2(barX, yLow), borderBrush, 1f);

						// Body fill (semi-transparent)
						var bodyRect = new RectangleF(barX - halfW, bTop, halfW * 2, bH);
						RenderTarget.FillRectangle(bodyRect, fillBrush);

						// Body border (full opacity, on top)
						RenderTarget.DrawRectangle(bodyRect, borderBrush, 1f);
					}
				}
			}

			RenderTarget.AntialiasMode = oldMode;
		}
		#endregion

		#region DX Resources
		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			if (dxUpBrush == null)
			{
				float opacity = (float)Math.Max(0.0, Math.Min(1.0, BarOpacity));

				dxUpBrush   = ColorUp.ToDxBrush(RenderTarget);
				dxUpBrush.Opacity = opacity;
				dxDownBrush = ColorDown.ToDxBrush(RenderTarget);
				dxDownBrush.Opacity = opacity;

				dxUpBorderBrush		= ColorUpBorder.ToDxBrush(RenderTarget);
				dxDownBorderBrush	= ColorDownBorder.ToDxBrush(RenderTarget);
				dxZeroBrush			= ZeroLineColor.ToDxBrush(RenderTarget);
				dxNeutralBrush		= NeutralColor.ToDxBrush(RenderTarget);
			}
		}

		private void DisposeDxResources()
		{
			if (dxUpBrush		  != null) { dxUpBrush.Dispose();		  dxUpBrush			= null; }
			if (dxDownBrush		  != null) { dxDownBrush.Dispose();		  dxDownBrush		= null; }
			if (dxUpBorderBrush	  != null) { dxUpBorderBrush.Dispose();	  dxUpBorderBrush	= null; }
			if (dxDownBorderBrush != null) { dxDownBorderBrush.Dispose(); dxDownBorderBrush	= null; }
			if (dxZeroBrush		  != null) { dxZeroBrush.Dispose();		  dxZeroBrush		= null; }
			if (dxNeutralBrush	  != null) { dxNeutralBrush.Dispose();	  dxNeutralBrush	= null; }
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}
		#endregion

		#region Properties

		[Display(Name = "Display Mode", Order = 0, GroupName = "Parameters")]
		public TDIDisplayMode Mode { get; set; }

		[Display(Name = "Reset on Session", Order = 1, GroupName = "Parameters")]
		public bool ResetOnSession { get; set; }

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

		[XmlIgnore]
		[Display(Name = "Neutral Color", Order = 5, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush NeutralColor { get; set; }
		[Browsable(false)]
		public string NeutralColorSerialize { get { return Serialize.BrushToString(NeutralColor); } set { NeutralColor = Serialize.StringToBrush(value); } }

		[Range(0.0, 1.0)]
		[Display(Name = "Bar Opacity", Order = 6, GroupName = "Visual Parameters")]
		public double BarOpacity { get; set; }

		[Range(1, 100)]
		[Display(Name = "Bar Width %", Order = 7, GroupName = "Visual Parameters")]
		public int BarWidthPercent { get; set; }

		[XmlIgnore]
		[Display(Name = "Zero Line Color", Order = 1, GroupName = "Reference Levels")]
		public System.Windows.Media.Brush ZeroLineColor { get; set; }
		[Browsable(false)]
		public string ZeroLineColorSerialize { get { return Serialize.BrushToString(ZeroLineColor); } set { ZeroLineColor = Serialize.StringToBrush(value); } }

		[Range(1, 5)]
		[Display(Name = "Zero Line Width", Order = 2, GroupName = "Reference Levels")]
		public int ZeroLineWidth { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaTickDirectionIndex[] cacheOrcaTickDirectionIndex;
		public OrcaTickDirectionIndex OrcaTickDirectionIndex()
		{
			return OrcaTickDirectionIndex(Input);
		}

		public OrcaTickDirectionIndex OrcaTickDirectionIndex(ISeries<double> input)
		{
			if (cacheOrcaTickDirectionIndex != null)
				for (int idx = 0; idx < cacheOrcaTickDirectionIndex.Length; idx++)
					if (cacheOrcaTickDirectionIndex[idx] != null &&  cacheOrcaTickDirectionIndex[idx].EqualsInput(input))
						return cacheOrcaTickDirectionIndex[idx];
			return CacheIndicator<OrcaTickDirectionIndex>(new OrcaTickDirectionIndex(), input, ref cacheOrcaTickDirectionIndex);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaTickDirectionIndex OrcaTickDirectionIndex()
		{
			return indicator.OrcaTickDirectionIndex(Input);
		}

		public Indicators.OrcaTickDirectionIndex OrcaTickDirectionIndex(ISeries<double> input )
		{
			return indicator.OrcaTickDirectionIndex(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaTickDirectionIndex OrcaTickDirectionIndex()
		{
			return indicator.OrcaTickDirectionIndex(Input);
		}

		public Indicators.OrcaTickDirectionIndex OrcaTickDirectionIndex(ISeries<double> input )
		{
			return indicator.OrcaTickDirectionIndex(input);
		}
	}
}

#endregion
