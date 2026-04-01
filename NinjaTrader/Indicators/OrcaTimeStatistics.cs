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
using SharpDX.DirectWrite;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaTimeStatistics : Indicator
	{
		#region Private Fields
		private double	lastBid;
		private double	lastAsk;
		private double	prevLast;
		private int		lastDirection;

		private List<double>	barTickDelta;
		private List<bool>		barHasData;

		// DX Resources
		private SharpDX.Direct2D1.Brush	dxVolumeBrush;
		private SharpDX.Direct2D1.Brush	dxPositiveBrush;
		private SharpDX.Direct2D1.Brush	dxNegativeBrush;
		private SharpDX.Direct2D1.Brush	dxEffPosBrush;
		private SharpDX.Direct2D1.Brush	dxEffNegBrush;
		private SharpDX.Direct2D1.Brush	dxTextBrush;
		private TextFormat				dxTextFormat;
		private SharpDX.DirectWrite.Factory dwFactory;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "OrcaTimeStatistics";
				Description					= "Displays Time Statistics (Volume, Delta, Delta Efficiency) at the bottom of the chart.";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= false;
				DisplayInDataBox			= true;
				IsSuspendedWhileInactive	= true;
				BarsRequiredToPlot			= 0;

				VolumeColor          = Brushes.SkyBlue;
				PositiveDeltaColor   = Brushes.LimeGreen;
				NegativeDeltaColor   = Brushes.Crimson;
				EfficiencyPosColor   = Brushes.MediumOrchid;
				EfficiencyNegColor   = Brushes.OrangeRed;
				TextColor            = Brushes.Black;
				BaseOpacity          = 0.25;
				FontSize             = 11;

				ShowVolume           = true;
				ShowDelta            = true;
				ShowDeltaEfficiency  = true;

				AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "TimeStatsDummy");
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				barTickDelta   = new List<double>(4096);
				barHasData     = new List<bool>(4096);

				lastBid        = double.NaN;
				lastAsk        = double.NaN;
				prevLast       = double.NaN;
				lastDirection  = 0;
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
				barHasData.Add(false);
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid)
				lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask)
				lastAsk = e.Price;
			else if (e.MarketDataType == MarketDataType.Last)
			{
				if (e.Ask > 0 && !double.IsNaN(e.Ask)) lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid)) lastBid = e.Bid;

				long vol = e.Volume;
				if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
					vol = (long)Core.Globals.ToCryptocurrencyVolume(vol);

				long signed = 0;
				if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
				{
					if (e.Price >= lastAsk) signed = vol;
					else if (e.Price <= lastBid) signed = -vol;
					else if (!double.IsNaN(prevLast))
					{
						if (e.Price > prevLast) signed = vol;
						else if (e.Price < prevLast) signed = -vol;
						else signed = lastDirection * vol;
					}
				}
				else if (!double.IsNaN(prevLast))
				{
					if (e.Price > prevLast) signed = vol;
					else if (e.Price < prevLast) signed = -vol;
					else signed = lastDirection * vol;
				}

				if (signed > 0) lastDirection = 1;
				else if (signed < 0) lastDirection = -1;

				prevLast = e.Price;

				if (signed != 0 && BarsArray[0].Count > 0)
				{
					int primaryIdx = BarsArray[0].GetBar(e.Time);
					if (primaryIdx >= 0)
					{
						EnsureBarLists(primaryIdx);
						barTickDelta[primaryIdx] += signed;
						barHasData[primaryIdx] = true;
					}
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 0)
			{
				EnsureBarLists(CurrentBar);

				if (Bars.IsFirstBarOfSession)
				{
					lastBid = double.NaN;
					lastAsk = double.NaN;
					prevLast = double.NaN;
				}

				if (CurrentBar < barHasData.Count && barHasData[CurrentBar])
					Value[0] = barTickDelta[CurrentBar];
				else
					Value[0] = double.NaN;
			}
		}

		#region Drawing & Rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (chartControl == null || chartScale == null || Bars == null || ChartBars == null)
				return;

			// Count how many rows are active
			int rowCount = 0;
			if (ShowVolume)           rowCount++;
			if (ShowDelta)            rowCount++;
			if (ShowDeltaEfficiency)  rowCount++;
			if (rowCount == 0) return;

			int fromIdx = ChartBars.FromIndex;
			int toIdx   = ChartBars.ToIndex;
			if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx)
				return;

			EnsureDxResources();
			if (dxVolumeBrush == null) return;

			float panelX = ChartPanel.X;
			float panelW = ChartPanel.W;
			float panelY = ChartPanel.Y;
			float panelH = ChartPanel.H;

			float rowH = panelH / rowCount;

			// Build ordered list of active rows  (kind: 0=vol, 1=delta, 2=eff)
			var rows = new List<(string label, int kind)>();
			if (ShowVolume)           rows.Add(("Volume",       0));
			if (ShowDelta)            rows.Add(("Delta",        1));
			if (ShowDeltaEfficiency)  rows.Add(("Δ Efficiency", 2));

			// Find per-row maximums over visible bars
			double maxVol = 0, maxDel = 0, maxEff = 0;
			double tickSize = Instrument.MasterInstrument.TickSize;
			if (tickSize <= 0) tickSize = 0.25;

			for (int i = fromIdx; i <= toIdx; i++)
			{
				if (i >= Bars.Count) continue;

				if (ShowVolume)
				{
					double v = Bars.GetVolume(i);
					if (v > maxVol) maxVol = v;
				}

				if (i < barTickDelta.Count && barHasData[i])
				{
					if (ShowDelta)
					{
						double d = Math.Abs(barTickDelta[i]);
						if (d > maxDel) maxDel = d;
					}

					if (ShowDeltaEfficiency)
					{
						double bdel  = barTickDelta[i];
						double range = (Bars.GetHigh(i) - Bars.GetLow(i)) / tickSize;
						if (range > 0)
						{
							double eff = Math.Abs(bdel) / range;
							if (eff > maxEff) maxEff = eff;
						}
					}
				}
			}

			if (maxVol == 0) maxVol = 1;
			if (maxDel == 0) maxDel = 1;
			if (maxEff == 0) maxEff = 1;

			AntialiasMode oldAA = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = AntialiasMode.Aliased;
			var oldTAA = RenderTarget.TextAntialiasMode;
			RenderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Cleartype;

			for (int i = fromIdx; i <= toIdx; i++)
			{
				if (i >= Bars.Count) continue;

				bool hasDelta = (i < barTickDelta.Count && barHasData[i]);

				float x          = chartControl.GetXByBarIndex(ChartBars, i);
				float barSpacing = GetBarSpacing(chartControl, i, fromIdx, toIdx);
				float boxW       = barSpacing * 0.9f;
				if (boxW < 2f) boxW = 2f;

				double vol   = Bars.GetVolume(i);
				double del   = hasDelta ? barTickDelta[i] : 0;
				double range = (Bars.GetHigh(i) - Bars.GetLow(i)) / tickSize;
				double eff   = (hasDelta && range > 0) ? del / range : 0;

				for (int r = 0; r < rows.Count; r++)
				{
					float rowY = panelY + r * rowH;
					int   kind = rows[r].kind;

					switch (kind)
					{
						case 0: // Volume
						{
							double intens  = vol / maxVol;
							float  opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * intens);
							dxVolumeBrush.Opacity = opacity;
							var rect = new RectangleF(x - boxW / 2, rowY + 1f, boxW, rowH - 2f);
							RenderTarget.FillRectangle(rect, dxVolumeBrush);
							if (boxW >= 20) DrawCenteredText(FormatVolume(vol), rect);
							break;
						}
						case 1: // Delta
						{
							if (!hasDelta) break;
							double intens  = Math.Abs(del) / maxDel;
							float  opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * intens);
							var dBrush = del >= 0 ? dxPositiveBrush : dxNegativeBrush;
							dBrush.Opacity = opacity;
							var rect = new RectangleF(x - boxW / 2, rowY + 1f, boxW, rowH - 2f);
							RenderTarget.FillRectangle(rect, dBrush);
							if (boxW >= 20) DrawCenteredText(FormatDelta(del), rect);
							break;
						}
						case 2: // Delta Efficiency
						{
							if (!hasDelta || range <= 0) break;
							double absEff  = Math.Abs(eff);
							double intens  = absEff / maxEff;
							float  opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * intens);
							var eBrush = eff >= 0 ? dxEffPosBrush : dxEffNegBrush;
							eBrush.Opacity = opacity;
							var rect = new RectangleF(x - boxW / 2, rowY + 1f, boxW, rowH - 2f);
							RenderTarget.FillRectangle(rect, eBrush);
							if (boxW >= 20) DrawCenteredText(FormatEfficiency(eff), rect);
							break;
						}
					}
				}
			}

			// Row labels on the right edge
			for (int r = 0; r < rows.Count; r++)
				DrawRightLabel(rows[r].label, panelX + panelW - 5f, panelY + r * rowH, rowH);

			RenderTarget.AntialiasMode     = oldAA;
			RenderTarget.TextAntialiasMode = oldTAA;
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

		private string FormatVolume(double vol)
		{
			if (vol >= 1000)
				return (vol / 1000.0).ToString("0.##") + "K";
			return vol.ToString("0.##");
		}

		private string FormatDelta(double delta)
		{
			return delta.ToString("#,##0");
		}

		// No need to scale by 100 anymore since it's now Delta per tick of range.
		private string FormatEfficiency(double eff)
		{
			return eff.ToString("+0.00;-0.00;0.00");
		}

		private void DrawCenteredText(string text, RectangleF rect)
		{
			if (dxTextFormat == null || dxTextBrush == null) return;
			using (var layout = new TextLayout(dwFactory, text, dxTextFormat, rect.Width, rect.Height))
			{
				layout.TextAlignment      = TextAlignment.Center;
				layout.ParagraphAlignment = ParagraphAlignment.Center;
				RenderTarget.DrawTextLayout(new Vector2(rect.X, rect.Y), layout, dxTextBrush);
			}
		}

		private void DrawRightLabel(string text, float x, float y, float h)
		{
			if (dxTextFormat == null || dxTextBrush == null) return;
			using (var layout = new TextLayout(dwFactory, text, dxTextFormat, 100, h))
			{
				layout.TextAlignment      = TextAlignment.Trailing;
				layout.ParagraphAlignment = ParagraphAlignment.Center;
				RenderTarget.DrawTextLayout(new Vector2(x - 100, y), layout, dxTextBrush);
			}
		}
		#endregion

		#region DX Resources
		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			if (dxVolumeBrush != null) return;

			dxVolumeBrush   = VolumeColor.ToDxBrush(RenderTarget);
			dxPositiveBrush = PositiveDeltaColor.ToDxBrush(RenderTarget);
			dxNegativeBrush = NegativeDeltaColor.ToDxBrush(RenderTarget);
			dxEffPosBrush   = EfficiencyPosColor.ToDxBrush(RenderTarget);
			dxEffNegBrush   = EfficiencyNegColor.ToDxBrush(RenderTarget);
			dxTextBrush     = TextColor.ToDxBrush(RenderTarget);

			dwFactory    = new SharpDX.DirectWrite.Factory();
			dxTextFormat = new TextFormat(dwFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold,
			                              SharpDX.DirectWrite.FontStyle.Normal, (float)FontSize);
		}

		private void DisposeDxResources()
		{
			if (dxVolumeBrush   != null) { dxVolumeBrush.Dispose();   dxVolumeBrush   = null; }
			if (dxPositiveBrush != null) { dxPositiveBrush.Dispose(); dxPositiveBrush = null; }
			if (dxNegativeBrush != null) { dxNegativeBrush.Dispose(); dxNegativeBrush = null; }
			if (dxEffPosBrush   != null) { dxEffPosBrush.Dispose();   dxEffPosBrush   = null; }
			if (dxEffNegBrush   != null) { dxEffNegBrush.Dispose();   dxEffNegBrush   = null; }
			if (dxTextBrush     != null) { dxTextBrush.Dispose();     dxTextBrush     = null; }
			if (dxTextFormat    != null) { dxTextFormat.Dispose();    dxTextFormat    = null; }
			if (dwFactory       != null) { dwFactory.Dispose();       dwFactory       = null; }
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}
		#endregion

		#region Properties
		// ── Visibility Toggles ────────────────────────────────────────────────
		[Display(Name = "Show Volume", Order = 1, GroupName = "Rows")]
		public bool ShowVolume { get; set; }

		[Display(Name = "Show Delta", Order = 2, GroupName = "Rows")]
		public bool ShowDelta { get; set; }

		[Display(Name = "Show Delta Efficiency", Order = 3, GroupName = "Rows",
			Description = "Delta / Range (ticks). Measures net delta per tick of price range.")]
		public bool ShowDeltaEfficiency { get; set; }

		// ── Colors ────────────────────────────────────────────────────────────
		[XmlIgnore]
		[Display(Name = "1. Volume Color", Order = 1, GroupName = "Visual")]
		public System.Windows.Media.Brush VolumeColor { get; set; }
		[Browsable(false)]
		public string VolumeColorSerialize
		{
			get { return Serialize.BrushToString(VolumeColor); }
			set { VolumeColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "2. Positive Delta Color", Order = 2, GroupName = "Visual")]
		public System.Windows.Media.Brush PositiveDeltaColor { get; set; }
		[Browsable(false)]
		public string PositiveDeltaColorSerialize
		{
			get { return Serialize.BrushToString(PositiveDeltaColor); }
			set { PositiveDeltaColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "3. Negative Delta Color", Order = 3, GroupName = "Visual")]
		public System.Windows.Media.Brush NegativeDeltaColor { get; set; }
		[Browsable(false)]
		public string NegativeDeltaColorSerialize
		{
			get { return Serialize.BrushToString(NegativeDeltaColor); }
			set { NegativeDeltaColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "4. Efficiency (+) Color", Order = 4, GroupName = "Visual",
			Description = "Color for bullish Delta Efficiency bars.")]
		public System.Windows.Media.Brush EfficiencyPosColor { get; set; }
		[Browsable(false)]
		public string EfficiencyPosColorSerialize
		{
			get { return Serialize.BrushToString(EfficiencyPosColor); }
			set { EfficiencyPosColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "5. Efficiency (-) Color", Order = 5, GroupName = "Visual",
			Description = "Color for bearish Delta Efficiency bars.")]
		public System.Windows.Media.Brush EfficiencyNegColor { get; set; }
		[Browsable(false)]
		public string EfficiencyNegColorSerialize
		{
			get { return Serialize.BrushToString(EfficiencyNegColor); }
			set { EfficiencyNegColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "6. Text Color", Order = 6, GroupName = "Visual")]
		public System.Windows.Media.Brush TextColor { get; set; }
		[Browsable(false)]
		public string TextColorSerialize
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}

		[Range(0.0, 1.0)]
		[Display(Name = "7. Base Opacity", Order = 7, GroupName = "Visual",
			Description = "Minimum opacity for lowest-intensity values.")]
		public double BaseOpacity { get; set; }

		[Range(6, 24)]
		[Display(Name = "8. Font Size", Order = 8, GroupName = "Visual")]
		public int FontSize { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaTimeStatistics[] cacheOrcaTimeStatistics;
		public OrcaTimeStatistics OrcaTimeStatistics()
		{
			return OrcaTimeStatistics(Input);
		}

		public OrcaTimeStatistics OrcaTimeStatistics(ISeries<double> input)
		{
			if (cacheOrcaTimeStatistics != null)
				for (int idx = 0; idx < cacheOrcaTimeStatistics.Length; idx++)
					if (cacheOrcaTimeStatistics[idx] != null &&  cacheOrcaTimeStatistics[idx].EqualsInput(input))
						return cacheOrcaTimeStatistics[idx];
			return CacheIndicator<OrcaTimeStatistics>(new OrcaTimeStatistics(), input, ref cacheOrcaTimeStatistics);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaTimeStatistics OrcaTimeStatistics()
		{
			return indicator.OrcaTimeStatistics(Input);
		}

		public Indicators.OrcaTimeStatistics OrcaTimeStatistics(ISeries<double> input )
		{
			return indicator.OrcaTimeStatistics(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaTimeStatistics OrcaTimeStatistics()
		{
			return indicator.OrcaTimeStatistics(Input);
		}

		public Indicators.OrcaTimeStatistics OrcaTimeStatistics(ISeries<double> input )
		{
			return indicator.OrcaTimeStatistics(input);
		}
	}
}

#endregion
