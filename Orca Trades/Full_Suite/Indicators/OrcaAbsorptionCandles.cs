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
#endregion

public enum DeltaCalculationMode
{
	BidAsk,
	UpDownTick
}

public enum AbsorptionThresholdMode
{
	None,
	FixedValue,
	PercentageOfAverage
}

public enum DivergenceColorMode
{
	MultiColorGradient,
	TwoColorOpacity
}

public enum DivergenceColorBasis
{
	DeltaDirection,
	CloseDirection
}

namespace NinjaTrader.NinjaScript.Indicators
{

	public class OrcaAbsorptionCandles : Indicator
	{
		private double	lastBid;
		private double	lastAsk;
		private double	prevLast;
		private int		lastDirection;

		private List<double>	barTickDelta;
		private List<bool>		barHasData;
		private List<double>	barSyntheticDelta; // OHLC-derived fallback for historical bars

		private Brush[] positiveBrushes;
		private Brush[] negativeBrushes;
		private const int NUM_BRUSHES = 20;

		private Brush pos100Brush;
		private Brush neg100Brush;
		private Brush pos50Brush;
		private Brush neg50Brush;
		private Brush divOutlineBrush;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "OrcaAbsorptionCandles";
				Description					= "Paints standard candlesticks based on volume delta intensity (absorption).";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= true;
				DisplayInDataBox			= false;
				IsSuspendedWhileInactive	= true;
				BarsRequiredToPlot			= 0;
				PaintPriceMarkers			= false;

				PositiveColor               = Brushes.DodgerBlue;
				NegativeColor               = Brushes.Crimson;
				NeutralColor                = Brushes.Gray;
				BaseOpacity                 = 0.45;
				IntensityLookback           = 50;
				DeltaMode                   = DeltaCalculationMode.BidAsk;
				ShowHistoricalColor         = true;

				HighlightDivergence         = true;
				BullishDivergenceColor      = Brushes.Cyan;
				BearishDivergenceColor      = Brushes.Fuchsia;
				DivergenceOutlineColor      = Brushes.White;
				DivColorMode                = DivergenceColorMode.MultiColorGradient;
				DivColorBasis               = DivergenceColorBasis.CloseDirection;
				DivergenceOutlineOpacity    = 1.0;
				DivergenceOutlineOnly       = false;

				ThresholdMode               = AbsorptionThresholdMode.None;
				FixedThreshold              = 1000;
				AvgLookback                 = 14;
				PercentageThreshold         = 150;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				barTickDelta      = new List<double>(4096);
				barHasData        = new List<bool>(4096);
				barSyntheticDelta = new List<double>(4096);

				lastBid        = double.NaN;
				lastAsk        = double.NaN;
				prevLast       = double.NaN;
				lastDirection  = 0;

				InitializeBrushes();
			}
		}

		private void InitializeBrushes()
		{
			positiveBrushes = new Brush[NUM_BRUSHES];
			negativeBrushes = new Brush[NUM_BRUSHES];

			Color posColor = ((SolidColorBrush)PositiveColor).Color;
			Color negColor = ((SolidColorBrush)NegativeColor).Color;

			SolidColorBrush p100 = new SolidColorBrush(Color.FromArgb(255, posColor.R, posColor.G, posColor.B));
			p100.Freeze();
			pos100Brush = p100;
			SolidColorBrush n100 = new SolidColorBrush(Color.FromArgb(255, negColor.R, negColor.G, negColor.B));
			n100.Freeze();
			neg100Brush = n100;
			
			SolidColorBrush p50 = new SolidColorBrush(Color.FromArgb(127, posColor.R, posColor.G, posColor.B));
			p50.Freeze();
			pos50Brush = p50;
			SolidColorBrush n50 = new SolidColorBrush(Color.FromArgb(127, negColor.R, negColor.G, negColor.B));
			n50.Freeze();
			neg50Brush = n50;

			Color outColor = ((SolidColorBrush)DivergenceOutlineColor).Color;
			SolidColorBrush oBrush = new SolidColorBrush(Color.FromArgb((byte)(DivergenceOutlineOpacity * 255), outColor.R, outColor.G, outColor.B));
			oBrush.Freeze();
			divOutlineBrush = oBrush;

			for (int i = 0; i < NUM_BRUSHES; i++)
			{
				double intensity = (double)i / (NUM_BRUSHES - 1); // 0.0 to 1.0
				double opacity = BaseOpacity + (1.0 - BaseOpacity) * intensity;
				
				byte posA = (byte)(opacity * posColor.A);
				byte negA = (byte)(opacity * negColor.A);

				SolidColorBrush pBrush = new SolidColorBrush(Color.FromArgb(posA, posColor.R, posColor.G, posColor.B));
				pBrush.Freeze();
				positiveBrushes[i] = pBrush;

				SolidColorBrush nBrush = new SolidColorBrush(Color.FromArgb(negA, negColor.R, negColor.G, negColor.B));
				nBrush.Freeze();
				negativeBrushes[i] = nBrush;
			}
		}

		private void EnsureBarLists(int idx)
		{
			while (barTickDelta.Count <= idx)
			{
				barTickDelta.Add(0);
				barHasData.Add(false);
				barSyntheticDelta.Add(double.NaN);
			}
		}

		/// <summary>
		/// Computes synthetic delta for a historical bar using OHLC.
		/// Formula: (Close - Open) / Range * Volume — directional and magnitude-scaled.
		/// Returns 0 if the bar has zero range.
		/// </summary>
		private double ComputeSyntheticDelta(int barIdx)
		{
			if (double.IsNaN(barSyntheticDelta[barIdx]))
			{
				double o = Open.GetValueAt(barIdx);
				double c = Close.GetValueAt(barIdx);
				double h = High.GetValueAt(barIdx);
				double l = Low.GetValueAt(barIdx);
				double range = h - l;
				long vol = (long)Volume.GetValueAt(barIdx);
				if (vol <= 0) vol = 1;
				// Normalize: ratio of -1 to +1, scaled by volume
				double ratio = (range > 0) ? ((c - o) / range) : (c >= o ? 1.0 : -1.0);
				barSyntheticDelta[barIdx] = ratio * vol;
			}
			return barSyntheticDelta[barIdx];
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (DeltaMode == DeltaCalculationMode.BidAsk)
			{
				if (e.MarketDataType == MarketDataType.Bid) lastBid = e.Price;
				else if (e.MarketDataType == MarketDataType.Ask) lastAsk = e.Price;
				else if (e.MarketDataType == MarketDataType.Last)
				{
					if (e.Ask > 0 && !double.IsNaN(e.Ask)) lastAsk = e.Ask;
					if (e.Bid > 0 && !double.IsNaN(e.Bid)) lastBid = e.Bid;
				}
			}
		}

		protected override void OnBarUpdate()
		{
			// ============================================
			// BarsInProgress == 1 : hidden tick processing
			// ============================================
			if (BarsInProgress == 1)
			{
				double price = Close[0];
				long vol = (long)Volume[0];
				if (vol <= 0) return;

				if (Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
					vol = (long)Core.Globals.ToCryptocurrencyVolume(vol);

				long signed = 0;
				
				if (DeltaMode == DeltaCalculationMode.BidAsk && !double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
				{
					if (price >= lastAsk) signed = vol;
					else if (price <= lastBid) signed = -vol;
					else if (!double.IsNaN(prevLast))
					{
						if (price > prevLast) signed = vol;
						else if (price < prevLast) signed = -vol;
						else signed = lastDirection * vol;
					}
				}
				else if (!double.IsNaN(prevLast))
				{
					// UpDownTick fallback or explicitly chosen calculation mode
					if (price > prevLast) signed = vol;
					else if (price < prevLast) signed = -vol;
					else signed = lastDirection * vol;
				}

				if (signed > 0) lastDirection = 1;
				else if (signed < 0) lastDirection = -1;

				prevLast = price;
				
				if (signed == 0) return;

				int primaryIdx = BarsArray[0].GetBar(Time[0]);
				if (primaryIdx < 0) return;

				EnsureBarLists(primaryIdx);
				barTickDelta[primaryIdx] += signed;
				barHasData[primaryIdx] = true;
				return;
			}

			// ============================================
			// BarsInProgress == 0 : primary bar painting
			// ============================================
			if (BarsInProgress == 0)
			{
				EnsureBarLists(CurrentBar);
				
				if (Bars.IsFirstBarOfSession && IsFirstTickOfBar)
				{
					lastBid = double.NaN;
					lastAsk = double.NaN;
					prevLast = double.NaN;
				}

				// Determine the delta value to use: real tick data or synthetic OHLC fallback
				bool hasReal = CurrentBar < barHasData.Count && barHasData[CurrentBar];
				bool useSynthetic = !hasReal && ShowHistoricalColor;

				if (hasReal || useSynthetic)
				{
					double delta = hasReal ? barTickDelta[CurrentBar] : ComputeSyntheticDelta(CurrentBar);
					
					bool isBullishDivergence = delta < 0 && Close[0] > Open[0];
					bool isBearishDivergence = delta > 0 && Close[0] < Open[0];
					bool isDivergent = isBullishDivergence || isBearishDivergence;

					bool passesThreshold = true;
					if (ThresholdMode == AbsorptionThresholdMode.FixedValue)
					{
						passesThreshold = Math.Abs(delta) >= FixedThreshold;
					}
					else if (ThresholdMode == AbsorptionThresholdMode.PercentageOfAverage)
					{
						double sum = 0;
						int count = 0;
						int avgStartIdx = Math.Max(0, CurrentBar - AvgLookback);
						for (int i = CurrentBar - 1; i >= avgStartIdx; i--)
						{
							double absD = 0;
							if (i < barHasData.Count && barHasData[i])
								absD = Math.Abs(barTickDelta[i]);
							else if (ShowHistoricalColor && i < barSyntheticDelta.Count)
								absD = Math.Abs(ComputeSyntheticDelta(i));
							else
								continue;
							sum += absD;
							count++;
						}
						double avg = count > 0 ? (sum / count) : 0;
						passesThreshold = Math.Abs(delta) >= (avg * (PercentageThreshold / 100.0));
					}

					bool enforceThreshold = ThresholdMode != AbsorptionThresholdMode.None && !passesThreshold;

					if (DivColorMode == DivergenceColorMode.TwoColorOpacity)
					{
						Brush standardBody;
						if (enforceThreshold) standardBody = NeutralColor;
						else standardBody = delta >= 0 ? pos50Brush : neg50Brush;

						if (HighlightDivergence && isDivergent)
						{
							if (DivergenceOutlineOnly)
							{
								Brush b;
								if (DivColorBasis == DivergenceColorBasis.DeltaDirection)
									b = delta >= 0 ? pos50Brush : neg50Brush;
								else
									b = Close[0] >= Open[0] ? pos50Brush : neg50Brush;
									
								BarBrush = b;
								CandleOutlineBrush = divOutlineBrush;
							}
							else
							{
								Brush b;
								if (DivColorBasis == DivergenceColorBasis.DeltaDirection)
									b = delta >= 0 ? pos100Brush : neg100Brush;
								else
									b = Close[0] >= Open[0] ? pos100Brush : neg100Brush;
									
								BarBrush = b;
								CandleOutlineBrush = divOutlineBrush;
							}
						}
						else
						{
							BarBrush = standardBody;
							CandleOutlineBrush = standardBody;
						}
					}
					else
					{
						// MultiColorGradient Mode
						int brushIdx = 0;
						Brush standardBody;
						
						if (enforceThreshold)
						{
							standardBody = NeutralColor;
						}
						else
						{
							double maxDelta = 0;
							int startIdx = Math.Max(0, CurrentBar - IntensityLookback);
							
							for (int i = CurrentBar; i >= startIdx; i--)
							{
								double absD;
								if (i < barHasData.Count && barHasData[i])
									absD = Math.Abs(barTickDelta[i]);
								else if (ShowHistoricalColor && i < barSyntheticDelta.Count)
									absD = Math.Abs(ComputeSyntheticDelta(i));
								else
									continue;
								if (absD > maxDelta) maxDelta = absD;
							}

							if (maxDelta == 0) maxDelta = 1;

							double intensity = Math.Abs(delta) / maxDelta;
							brushIdx = (int)Math.Round(intensity * (NUM_BRUSHES - 1));
							if (brushIdx < 0) brushIdx = 0;
							if (brushIdx >= NUM_BRUSHES) brushIdx = NUM_BRUSHES - 1;

							standardBody = delta >= 0 ? positiveBrushes[brushIdx] : negativeBrushes[brushIdx];
						}

						if (HighlightDivergence && isDivergent)
						{
							if (DivergenceOutlineOnly)
							{
								Brush b;
								if (enforceThreshold) b = NeutralColor;
								else if (DivColorBasis == DivergenceColorBasis.DeltaDirection)
									b = delta >= 0 ? positiveBrushes[brushIdx] : negativeBrushes[brushIdx];
								else
									b = Close[0] >= Open[0] ? positiveBrushes[brushIdx] : negativeBrushes[brushIdx];
									
								BarBrush = b;
								CandleOutlineBrush = divOutlineBrush;
							}
							else
							{
								Brush divBrush = isBullishDivergence ? BullishDivergenceColor : BearishDivergenceColor;
								BarBrush = divBrush;
								CandleOutlineBrush = divOutlineBrush;
							}
						}
						else
						{
							BarBrush = standardBody;
							CandleOutlineBrush = standardBody;
						}
					}
				}
			}
		}

		#region Properties
		[XmlIgnore]
		[Display(Name = "1. Positive Delta Color", Order = 1, GroupName = "1. Visuals")]
		public System.Windows.Media.Brush PositiveColor { get; set; }
		[Browsable(false)]
		public string PositiveColorSerialize { get { return Serialize.BrushToString(PositiveColor); } set { PositiveColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "2. Negative Delta Color", Order = 2, GroupName = "1. Visuals")]
		public System.Windows.Media.Brush NegativeColor { get; set; }
		[Browsable(false)]
		public string NegativeColorSerialize { get { return Serialize.BrushToString(NegativeColor); } set { NegativeColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "3. Neutral Color", Order = 3, GroupName = "1. Visuals", Description = "Color for candles that do not pass the threshold.")]
		public System.Windows.Media.Brush NeutralColor { get; set; }
		[Browsable(false)]
		public string NeutralColorSerialize { get { return Serialize.BrushToString(NeutralColor); } set { NeutralColor = Serialize.StringToBrush(value); } }

		[Range(0.0, 1.0)]
		[Display(Name = "4. Base Opacity", Order = 4, GroupName = "1. Visuals", Description = "Minimum opacity for lowest intensity values.")]
		public double BaseOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "1. Intensity Lookback", Order = 1, GroupName = "2. Parameters", Description = "Number of bars to look back for calculating max delta intensity.")]
		public int IntensityLookback { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "2. Delta Calculation Mode", Order = 2, GroupName = "2. Parameters", Description = "Choose whether delta calculates via real Bid/Ask spread hits, or simple Up/Down tick direction.")]
		public DeltaCalculationMode DeltaMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "3. Show Historical Color", Order = 3, GroupName = "2. Parameters",
			Description = "Paint historical bars using synthetic delta ((Close-Open)/Range × Volume) when real tick data is unavailable.")]
		public bool ShowHistoricalColor { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "4. Threshold Mode", Order = 4, GroupName = "2. Parameters", Description = "Method used to filter which candles light up with absorption colors.")]
		public AbsorptionThresholdMode ThresholdMode { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, double.MaxValue)]
		[Display(Name = "5. Fixed Threshold", Order = 5, GroupName = "2. Parameters", Description = "Delta must be >= this value when mode is FixedValue.")]
		public double FixedThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "6. Average Lookback", Order = 6, GroupName = "2. Parameters", Description = "Number of preceding bars to average when mode is PercentageOfAverage.")]
		public int AvgLookback { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, double.MaxValue)]
		[Display(Name = "7. Percentage Threshold", Order = 7, GroupName = "2. Parameters", Description = "Required percentage of the average (e.g. 150 for 150%) when mode is PercentageOfAverage.")]
		public double PercentageThreshold { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "1. Highlight Divergence", Order = 1, GroupName = "3. Divergence", Description = "Highlights candles where price closes opposite to delta direction.")]
		public bool HighlightDivergence { get; set; }

		[XmlIgnore]
		[Display(Name = "2. Bullish Divergence (Negative Delta, Positive Close)", Order = 2, GroupName = "3. Divergence")]
		public System.Windows.Media.Brush BullishDivergenceColor { get; set; }
		[Browsable(false)]
		public string BullishDivergenceColorSerialize { get { return Serialize.BrushToString(BullishDivergenceColor); } set { BullishDivergenceColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "3. Bearish Divergence (Positive Delta, Negative Close)", Order = 3, GroupName = "3. Divergence")]
		public System.Windows.Media.Brush BearishDivergenceColor { get; set; }
		[Browsable(false)]
		public string BearishDivergenceColorSerialize { get { return Serialize.BrushToString(BearishDivergenceColor); } set { BearishDivergenceColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "5. Main Candle Painting Style", Order = 5, GroupName = "1. Visuals", Description = "MultiColorGradient applies volume delta intensity shading to normal candles. TwoColorOpacity ignores gradients and uses a flat 50% opacity for normal candles.")]
		public DivergenceColorMode DivColorMode { get; set; }

		[XmlIgnore]
		[Display(Name = "5. Divergence Outline Color", Order = 5, GroupName = "3. Divergence", Description = "Border color specifically applied to highlight divergent candles.")]
		public System.Windows.Media.Brush DivergenceOutlineColor { get; set; }
		[Browsable(false)]
		public string DivergenceOutlineColorSerialize { get { return Serialize.BrushToString(DivergenceOutlineColor); } set { DivergenceOutlineColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "6. Divergence Color Basis", Order = 6, GroupName = "3. Divergence", Description = "Dictates whether the divergent candle base hue derives from the Delta direction or the Close direction.")]
		public DivergenceColorBasis DivColorBasis { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "7. Divergence Outline Opacity", Order = 7, GroupName = "3. Divergence", Description = "Opacity level for the Divergence Outline Color (0.0 for invisible, 1.0 for solid).")]
		public double DivergenceOutlineOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "8. Divergence Outline Only", Order = 8, GroupName = "3. Divergence", Description = "If true, divergence highlighting will only override the candle border. The inner body will retain its normal standard delta volume gradient or opacity.")]
		public bool DivergenceOutlineOnly { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaAbsorptionCandles[] cacheOrcaAbsorptionCandles;
		public OrcaAbsorptionCandles OrcaAbsorptionCandles(bool showHistoricalColor)
		{
			return OrcaAbsorptionCandles(Input, showHistoricalColor);
		}

		public OrcaAbsorptionCandles OrcaAbsorptionCandles(ISeries<double> input, bool showHistoricalColor)
		{
			if (cacheOrcaAbsorptionCandles != null)
				for (int idx = 0; idx < cacheOrcaAbsorptionCandles.Length; idx++)
					if (cacheOrcaAbsorptionCandles[idx] != null && cacheOrcaAbsorptionCandles[idx].ShowHistoricalColor == showHistoricalColor && cacheOrcaAbsorptionCandles[idx].EqualsInput(input))
						return cacheOrcaAbsorptionCandles[idx];
			return CacheIndicator<OrcaAbsorptionCandles>(new OrcaAbsorptionCandles(){ ShowHistoricalColor = showHistoricalColor }, input, ref cacheOrcaAbsorptionCandles);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaAbsorptionCandles OrcaAbsorptionCandles(bool showHistoricalColor)
		{
			return indicator.OrcaAbsorptionCandles(Input, showHistoricalColor);
		}

		public Indicators.OrcaAbsorptionCandles OrcaAbsorptionCandles(ISeries<double> input , bool showHistoricalColor)
		{
			return indicator.OrcaAbsorptionCandles(input, showHistoricalColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaAbsorptionCandles OrcaAbsorptionCandles(bool showHistoricalColor)
		{
			return indicator.OrcaAbsorptionCandles(Input, showHistoricalColor);
		}

		public Indicators.OrcaAbsorptionCandles OrcaAbsorptionCandles(ISeries<double> input , bool showHistoricalColor)
		{
			return indicator.OrcaAbsorptionCandles(input, showHistoricalColor);
		}
	}
}

#endregion
