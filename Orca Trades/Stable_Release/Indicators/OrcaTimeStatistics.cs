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
		private double	lastBid;
		private double	lastAsk;
		private double	prevLast;
		private int		lastDirection;

		private List<double>	barTickDelta;
		private List<double>	barMaxDelta;
		private List<double>	barMinDelta;
		private List<bool>		barHasData;

		private SharpDX.Direct2D1.Brush	dxVolumeBrush;
		private SharpDX.Direct2D1.Brush	dxPositiveBrush;
		private SharpDX.Direct2D1.Brush	dxNegativeBrush;
		private SharpDX.Direct2D1.Brush	dxFinPosBrush;
		private SharpDX.Direct2D1.Brush	dxFinNegBrush;
		private SharpDX.Direct2D1.Brush	dxRangeBrush;
		private SharpDX.Direct2D1.Brush	dxTimeBrush;
		private SharpDX.Direct2D1.Brush	dxTextBrush;
		private SharpDX.DirectWrite.TextFormat	dxTextFormat;
		private SharpDX.DirectWrite.Factory dwFactory;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "OrcaTimeStatistics";
				Description					= "Displays per-bar statistics (Volume, Delta, Delta Efficiency, Range, Time) as a panel below the chart.";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= false;
				DisplayInDataBox			= false;
				IsSuspendedWhileInactive	= true;
				BarsRequiredToPlot			= 0;
				DrawHorizontalGridLines		= false;
				DrawVerticalGridLines		= false;
				IsAutoScale					= false;

				VolumeColor          = Brushes.SkyBlue;
				PositiveDeltaColor   = Brushes.LimeGreen;
				NegativeDeltaColor   = Brushes.Crimson;
				FinishDeltaPosColor  = Brushes.MediumOrchid;
				FinishDeltaNegColor  = Brushes.OrangeRed;
				RangeColor           = Brushes.DodgerBlue;
				TimeColor            = Brushes.SlateGray;
				TextColor            = Brushes.Black;
				BaseOpacity          = 0.25;
				FontSize             = 11;

				ShowVolume           = true;
				ShowDelta            = true;
				ShowFinishDelta      = true;
				ShowRange            = true;
				ShowTime             = true;

			}
			else if (State == State.DataLoaded)
			{
				barTickDelta   = new List<double>(4096);
				barMaxDelta    = new List<double>(4096);
				barMinDelta    = new List<double>(4096);
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
				barMaxDelta.Add(0);
				barMinDelta.Add(0);
				barHasData.Add(false);
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid) lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask) lastAsk = e.Price;
			else if (e.MarketDataType == MarketDataType.Last)
			{
				if (e.Ask > 0 && !double.IsNaN(e.Ask)) lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid)) lastBid = e.Bid;

				long vol = e.Volume;
				if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
					vol = (long)NinjaTrader.Core.Globals.ToCryptocurrencyVolume(vol);

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
						barMaxDelta[primaryIdx] = Math.Max(barMaxDelta[primaryIdx], barTickDelta[primaryIdx]);
						barMinDelta[primaryIdx] = Math.Min(barMinDelta[primaryIdx], barTickDelta[primaryIdx]);
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
				if (Bars.IsFirstBarOfSession) { lastBid = double.NaN; lastAsk = double.NaN; prevLast = double.NaN; }
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (chartControl == null || chartScale == null || Bars == null || ChartBars == null) return;
			int rowCount = (ShowVolume ? 1 : 0) + (ShowDelta ? 1 : 0) + (ShowFinishDelta ? 1 : 0)
						+ (ShowRange ? 1 : 0) + (ShowTime ? 1 : 0);
			if (rowCount == 0) return;

			int fromIdx = ChartBars.FromIndex;
			int toIdx   = ChartBars.ToIndex;
			if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx) return;

			EnsureDxResources();
			if (dxVolumeBrush == null) return;

			float panelY = ChartPanel.Y;
			float panelH = ChartPanel.H;
			float rowH = panelH / rowCount;

			var rows = new List<KeyValuePair<string, int>>();
			if (ShowVolume)      rows.Add(new KeyValuePair<string, int>("Volume",       0));
			if (ShowDelta)       rows.Add(new KeyValuePair<string, int>("Delta",        1));
			if (ShowFinishDelta) rows.Add(new KeyValuePair<string, int>("Finish \u0394",  2));
			if (ShowRange)       rows.Add(new KeyValuePair<string, int>("Range",        3));
			if (ShowTime)        rows.Add(new KeyValuePair<string, int>("Time",         4));

			double maxVol = 1, maxDel = 1, maxRange = 1;
			double tickSize = Math.Max(0.00000001, Instrument.MasterInstrument.TickSize);

			for (int i = fromIdx; i <= toIdx; i++)
			{
				if (i >= Bars.Count) continue;
				if (ShowVolume) maxVol = Math.Max(maxVol, Bars.GetVolume(i));
				if (ShowRange)  maxRange = Math.Max(maxRange, Bars.GetHigh(i) - Bars.GetLow(i));
				if (i < barTickDelta.Count && barHasData[i])
				{
					if (ShowDelta) maxDel = Math.Max(maxDel, Math.Abs(barTickDelta[i]));
				}
			}

			SharpDX.Direct2D1.AntialiasMode oldAA = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;
			SharpDX.Direct2D1.TextAntialiasMode oldTAA = RenderTarget.TextAntialiasMode;
			RenderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Cleartype;


			for (int i = fromIdx; i <= toIdx; i++)
			{
				if (i >= Bars.Count) continue;
				float x = chartControl.GetXByBarIndex(ChartBars, i);
				float barSpacing = (i < toIdx) ? (chartControl.GetXByBarIndex(ChartBars, i + 1) - x) : ((i > fromIdx) ? (x - chartControl.GetXByBarIndex(ChartBars, i - 1)) : (float)chartControl.BarWidth);
				float boxW = Math.Max(2f, barSpacing);

				bool hasDelta = (i < barTickDelta.Count && barHasData[i]);
				double vol   = Bars.GetVolume(i);
				double del   = hasDelta ? barTickDelta[i] : 0;
				double range = Bars.GetHigh(i) - Bars.GetLow(i); // points (H-L)
				double open  = Bars.GetOpen(i);
				double close = Bars.GetClose(i);

				for (int r = 0; r < rows.Count; r++)
				{
					float rowY = panelY + r * rowH;
					RectangleF rect = new RectangleF(x - boxW / 2, rowY, boxW, rowH);
					switch (rows[r].Value)
					{
						case 0: // Volume
							dxVolumeBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * (vol / maxVol));
							RenderTarget.FillRectangle(rect, dxVolumeBrush);
							if (boxW >= 20) DrawCenteredText(FormatVolume(vol), rect);
							break;
						case 1: // Delta
							if (!hasDelta) break;
							var dBrush = del >= 0 ? dxPositiveBrush : dxNegativeBrush;
							dBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * (Math.Abs(del) / maxDel));
							RenderTarget.FillRectangle(rect, dBrush);
							if (boxW >= 20) DrawCenteredText(FormatDelta(del), rect);
							break;
						case 2: // Finish Delta = (Current Delta - Extreme Delta)
							if (!hasDelta) break;
							double curDel = barTickDelta[i];
							double extreme = (curDel >= 0) ? barMaxDelta[i] : barMinDelta[i];
							double finDelta = curDel - extreme;
							var fBrush = finDelta >= 0 ? dxFinPosBrush : dxFinNegBrush;
							// Scale opacity based on absolute value relative to absolute max delta (or some fixed scale)
							fBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * Math.Min(1.0, Math.Abs(finDelta) / maxDel));
							RenderTarget.FillRectangle(rect, fBrush);
							if (boxW >= 20) DrawCenteredText(finDelta.ToString("+#;-#;0"), rect);
							break;
						case 3: // Range (H-L in ticks)
							dxRangeBrush.Opacity = (float)(BaseOpacity + (1.0 - BaseOpacity) * (range / maxRange));
							RenderTarget.FillRectangle(rect, dxRangeBrush);
							if (boxW >= 20) DrawCenteredText(range.ToString("0.#"), rect);
							break;
						case 4: // Time — bar duration formatted as "Xm Y" or "Xs"
							dxTimeBrush.Opacity = (float)BaseOpacity;
							RenderTarget.FillRectangle(rect, dxTimeBrush);
							if (boxW >= 28)
							{
								int durationSecs;
								if (i < Bars.Count - 1)
									durationSecs = (int)Math.Abs((Bars.GetTime(i + 1) - Bars.GetTime(i)).TotalSeconds);
								else
									durationSecs = (int)Math.Max(0, (DateTime.Now - Bars.GetTime(i)).TotalSeconds);
								DrawCenteredText(FormatDuration(durationSecs), rect);
							}
							break;
					}
				}
			}

			for (int r = 0; r < rows.Count; r++)
				DrawRightLabel(rows[r].Key, ChartPanel.X + ChartPanel.W - 5f, panelY + r * rowH, rowH);


			RenderTarget.AntialiasMode = oldAA;
			RenderTarget.TextAntialiasMode = oldTAA;
		}

		private string FormatVolume(double vol) { return vol >= 1000 ? (vol / 1000.0).ToString("0.##") + "K" : vol.ToString("0.##"); }
		private string FormatDelta(double delta) { return delta.ToString("#,##0"); }
		private string FormatDuration(int totalSecs)
		{
			if (totalSecs < 60) return totalSecs + "s";
			int m = totalSecs / 60, s = totalSecs % 60;
			return s > 0 ? m + "m " + s : m + "m";
		}

		private void DrawCenteredText(string text, RectangleF rect)
		{
			if (dxTextFormat == null || dxTextBrush == null) return;
			using (var layout = new SharpDX.DirectWrite.TextLayout(dwFactory, text, dxTextFormat, rect.Width, rect.Height))
			{
				layout.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				layout.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
				RenderTarget.DrawTextLayout(new Vector2(rect.X, rect.Y), layout, dxTextBrush);
			}
		}

		private void DrawRightLabel(string text, float x, float y, float h)
		{
			if (dxTextFormat == null || dxTextBrush == null) return;
			using (var layout = new SharpDX.DirectWrite.TextLayout(dwFactory, text, dxTextFormat, 100, h))
			{
				layout.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				layout.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
				RenderTarget.DrawTextLayout(new Vector2(x - 100, y), layout, dxTextBrush);
			}
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null || dxVolumeBrush != null) return;
			dxVolumeBrush   = CreateSolidBrush(VolumeColor, 1.0f);
			dxPositiveBrush = CreateSolidBrush(PositiveDeltaColor, 1.0f);
			dxNegativeBrush = CreateSolidBrush(NegativeDeltaColor, 1.0f);
			dxFinPosBrush   = CreateSolidBrush(FinishDeltaPosColor, 1.0f);
			dxFinNegBrush   = CreateSolidBrush(FinishDeltaNegColor, 1.0f);
			dxRangeBrush    = CreateSolidBrush(RangeColor, 1.0f);
			dxTimeBrush     = CreateSolidBrush(TimeColor, 1.0f);
			dxTextBrush     = CreateSolidBrush(TextColor, 1.0f);
			dwFactory    = new SharpDX.DirectWrite.Factory();
			dxTextFormat = new SharpDX.DirectWrite.TextFormat(dwFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)FontSize);
		}

		private SharpDX.Direct2D1.Brush CreateSolidBrush(System.Windows.Media.Brush wpfBrush, float opacity)
		{
			var color = (wpfBrush as System.Windows.Media.SolidColorBrush)?.Color ?? System.Windows.Media.Colors.White;
			return new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(color.R / 255f, color.G / 255f, color.B / 255f, (color.A / 255f) * opacity));
		}

		private void DisposeDxResources()
		{
			if (dxVolumeBrush != null)   { dxVolumeBrush.Dispose();   dxVolumeBrush = null; }
			if (dxPositiveBrush != null) { dxPositiveBrush.Dispose(); dxPositiveBrush = null; }
			if (dxNegativeBrush != null) { dxNegativeBrush.Dispose(); dxNegativeBrush = null; }
			if (dxFinPosBrush != null)   { dxFinPosBrush.Dispose();   dxFinPosBrush = null; }
			if (dxFinNegBrush != null)   { dxFinNegBrush.Dispose();   dxFinNegBrush = null; }
			if (dxRangeBrush != null)      { dxRangeBrush.Dispose();      dxRangeBrush = null; }
			if (dxTimeBrush != null)       { dxTimeBrush.Dispose();       dxTimeBrush = null; }
			if (dxTextBrush != null)       { dxTextBrush.Dispose();       dxTextBrush = null; }
			if (dxTextFormat != null)      { dxTextFormat.Dispose();      dxTextFormat = null; }
			if (dwFactory != null)         { dwFactory.Dispose();         dwFactory = null; }
		}

		public override void OnRenderTargetChanged() { DisposeDxResources(); base.OnRenderTargetChanged(); }

		[Display(Name = "Show Volume",           Order = 1, GroupName = "Rows")]
		public bool ShowVolume { get; set; }

		[Display(Name = "Show Delta",            Order = 2, GroupName = "Rows")]
		public bool ShowDelta { get; set; }

		[Display(Name = "Show Finish Delta", Order = 3, GroupName = "Rows")]
		public bool ShowFinishDelta { get; set; }

		[Display(Name = "Show Range",            Order = 4, GroupName = "Rows")]
		public bool ShowRange { get; set; }

		[Display(Name = "Show Time",             Order = 5, GroupName = "Rows")]
		public bool ShowTime { get; set; }

		[XmlIgnore]
		[Display(Name = "1. Volume Color", Order = 1, GroupName = "Visual")]
		public System.Windows.Media.Brush VolumeColor { get; set; }
		[Browsable(false)]
		public string VolumeColorSerialize { get { return Serialize.BrushToString(VolumeColor); } set { VolumeColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "2. Positive Delta Color", Order = 2, GroupName = "Visual")]
		public System.Windows.Media.Brush PositiveDeltaColor { get; set; }
		[Browsable(false)]
		public string PositiveDeltaColorSerialize { get { return Serialize.BrushToString(PositiveDeltaColor); } set { PositiveDeltaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "3. Negative Delta Color", Order = 3, GroupName = "Visual")]
		public System.Windows.Media.Brush NegativeDeltaColor { get; set; }
		[Browsable(false)]
		public string NegativeDeltaColorSerialize { get { return Serialize.BrushToString(NegativeDeltaColor); } set { NegativeDeltaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "4. Finish Delta (+) Color", Order = 4, GroupName = "Visual")]
		public System.Windows.Media.Brush FinishDeltaPosColor { get; set; }
		[Browsable(false)]
		public string FinishDeltaPosColorSerialize { get { return Serialize.BrushToString(FinishDeltaPosColor); } set { FinishDeltaPosColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "5. Finish Delta (-) Color", Order = 5, GroupName = "Visual")]
		public System.Windows.Media.Brush FinishDeltaNegColor { get; set; }
		[Browsable(false)]
		public string FinishDeltaNegColorSerialize { get { return Serialize.BrushToString(FinishDeltaNegColor); } set { FinishDeltaNegColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "6. Range Color", Order = 6, GroupName = "Visual")]
		public System.Windows.Media.Brush RangeColor { get; set; }
		[Browsable(false)]
		public string RangeColorSerialize { get { return Serialize.BrushToString(RangeColor); } set { RangeColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "7. Time Color", Order = 7, GroupName = "Visual")]
		public System.Windows.Media.Brush TimeColor { get; set; }
		[Browsable(false)]
		public string TimeColorSerialize { get { return Serialize.BrushToString(TimeColor); } set { TimeColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "8. Text Color", Order = 8, GroupName = "Visual")]
		public System.Windows.Media.Brush TextColor { get; set; }
		[Browsable(false)]
		public string TextColorSerialize { get { return Serialize.BrushToString(TextColor); } set { TextColor = Serialize.StringToBrush(value); } }

		[Range(0.0, 1.0)]
		[Display(Name = "9. Base Opacity", Order = 9, GroupName = "Visual")]
		public double BaseOpacity { get; set; }

		[Range(6, 24)]
		[Display(Name = "10. Font Size", Order = 10, GroupName = "Visual")]
		public int FontSize { get; set; }
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
