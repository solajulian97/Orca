#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;

using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum OrcaEnvelopePriceSource
	{
		Close,
		MedianPrice,
		TypicalPrice,
		OhlcAverage
	}

	public enum OrcaEnvelopeRegime
	{
		Unclassified,
		Range,
		Rising,
		Falling,
		Transition
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaAdaptiveEnvelope : Indicator
	{
		private sealed class RegressionState
		{
			public bool Initialized;
			public DateTime OriginTime;
			public DateTime LastTime;
			public double Weight;
			public double SumX;
			public double SumY;
			public double SumXX;
			public double SumXY;
			public double UpperResidualWeight;
			public double UpperResidualSquares;
			public double LowerResidualWeight;
			public double LowerResidualSquares;
			public double UpperHalfWidth;
			public double LowerHalfWidth;
			public int SampleCount;

			public RegressionState Clone()
			{
				return new RegressionState
				{
					Initialized = Initialized,
					OriginTime = OriginTime,
					LastTime = LastTime,
					Weight = Weight,
					SumX = SumX,
					SumY = SumY,
					SumXX = SumXX,
					SumXY = SumXY,
					UpperResidualWeight = UpperResidualWeight,
					UpperResidualSquares = UpperResidualSquares,
					LowerResidualWeight = LowerResidualWeight,
					LowerResidualSquares = LowerResidualSquares,
					UpperHalfWidth = UpperHalfWidth,
					LowerHalfWidth = LowerHalfWidth,
					SampleCount = SampleCount
				};
			}
		}

		private sealed class EnvelopeResult
		{
			public double Center;
			public double Upper;
			public double Lower;
			public double SlopePerMinute;
			public double UpperHalfWidth;
			public double LowerHalfWidth;
			public int SampleCount;
		}

		private sealed class ZoneRegionSegment
		{
			public int Id;
			public int StartBar;
			public int EndBar;
			public OrcaEnvelopeRegime Regime;
		}

		private const string FillTag = "OAE-Fill";
		private const string ZoneTagPrefix = "OAE-Zone-";
		private const string DiagnosticsTag = "OAE-Diagnostics";

		private RegressionState committedState;
		private EnvelopeResult lastCommittedResult;
		private Queue<double> trueRangeHistory;
		private Queue<double> efficiencyPriceHistory;
		private int workingBar;
		private DateTime workingTime;
		private double workingPrice;
		private double workingHigh;
		private double workingLow;
		private double workingClose;
		private double workingTrueRange;
		private double workingMedianTrueRange;
		private double lastCommittedClose;
		private OrcaEnvelopeRegime currentRegime;
		private OrcaEnvelopeRegime pendingRegime;
		private int pendingRegimeBars;
		private double currentDirectionalEfficiency;
		private double currentTrendStrength;
		private List<ZoneRegionSegment> zoneRegionSegments;
		private ZoneRegionSegment activeZoneRegionSegment;
		private int nextZoneRegionSegmentId;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca Adaptive Envelope";
				Description = "Continuous, non-candidate adaptive regression envelope with time-decayed trend, residual-deviation width, and regime-aware trade zones.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DrawOnPricePanel = true;
				DisplayInDataBox = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 2;

				PriceSource = OrcaEnvelopePriceSource.TypicalPrice;
				TrendMemoryMinutes = 15;
				DeviationMultiplier = 2.5;
				ExpansionRate = 0.30;
				ContractionRate = 0.05;
				OutlierClamp = 4.0;
				TrueRangeLookback = 20;
				MinimumWidthMultiple = 0.75;
				ResetAtTradingSession = true;

				EfficiencyLookback = 20;
				MinimumDirectionalEfficiency = 0.28;
				TrendStrengthThreshold = 0.60;
				RangeStrengthThreshold = 0.20;
				ClassificationConfirmationBars = 3;
				MinimumClassificationSamples = 12;

				ShowTradeZones = true;
				ZoneWidthPercentage = 15;
				FillOpacity = 7;
				ZoneOpacity = 14;
				MaximumFillBars = 5000;
				FillColor = Brushes.SteelBlue;
				LongZoneColor = Brushes.MediumSeaGreen;
				ShortZoneColor = Brushes.IndianRed;
				RangeZoneColor = Brushes.Goldenrod;
				ShowDiagnostics = false;

				AddPlot(new Stroke(Brushes.Gainsboro, DashStyleHelper.Solid, 1), PlotStyle.Line, "Centerline");
				AddPlot(new Stroke(Brushes.DimGray, DashStyleHelper.Solid, 3), PlotStyle.Line, "UpperEnvelope");
				AddPlot(new Stroke(Brushes.DimGray, DashStyleHelper.Solid, 3), PlotStyle.Line, "LowerEnvelope");
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1), PlotStyle.Line, "LongZoneInner");
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1), PlotStyle.Line, "ShortZoneInner");
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1), PlotStyle.Line, "RangeLongZoneInner");
				AddPlot(new Stroke(Brushes.Transparent, DashStyleHelper.Solid, 1), PlotStyle.Line, "RangeShortZoneInner");
			}
			else if (State == State.DataLoaded)
			{
				committedState = new RegressionState();
				trueRangeHistory = new Queue<double>();
				efficiencyPriceHistory = new Queue<double>();
				workingBar = -1;
				lastCommittedClose = double.NaN;
				currentRegime = OrcaEnvelopeRegime.Unclassified;
				pendingRegime = OrcaEnvelopeRegime.Unclassified;
				pendingRegimeBars = 0;
				zoneRegionSegments = new List<ZoneRegionSegment>();
				activeZoneRegionSegment = null;
				nextZoneRegionSegmentId = 1;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 0 || committedState == null)
				return;

			bool isNewBar = CurrentBar != workingBar;
			bool isSessionStart = isNewBar
				&& ResetAtTradingSession
				&& Bars != null
				&& Bars.IsFirstBarOfSession;

			if (isNewBar)
			{
				if (workingBar >= 0)
				{
					RefreshClosedWorkingBar();
					CommitWorkingBar();
				}

				if (isSessionStart)
					ResetForTradingSession();

				workingBar = CurrentBar;
			}

			CaptureWorkingBar();
			RegressionState provisionalState = committedState.Clone();
			EnvelopeResult result = AddSample(
				provisionalState,
				workingTime,
				workingPrice,
				workingMedianTrueRange);

			WritePlots(result);

			if (isNewBar)
				UpdateRegions();

			UpdateDiagnostics(result);
		}

		private void RefreshClosedWorkingBar()
		{
			int barsAgo = CurrentBar - workingBar;
			if (barsAgo < 1 || barsAgo > CurrentBar)
				return;

			workingTime = Time[barsAgo];
			workingPrice = GetPrice(barsAgo);
			workingHigh = High[barsAgo];
			workingLow = Low[barsAgo];
			workingClose = Close[barsAgo];

			double previousClose = IsFinite(lastCommittedClose)
				? lastCommittedClose
				: workingClose;
			workingTrueRange = Math.Max(
				workingHigh - workingLow,
				Math.Max(
					Math.Abs(workingHigh - previousClose),
					Math.Abs(workingLow - previousClose)));
			workingMedianTrueRange = CalculateMedianTrueRange(workingTrueRange);
		}

		private void CommitWorkingBar()
		{
			RegressionState nextState = committedState.Clone();
			lastCommittedResult = AddSample(
				nextState,
				workingTime,
				workingPrice,
				workingMedianTrueRange);
			committedState = nextState;

			if (IsFinite(workingTrueRange) && workingTrueRange > 0)
			{
				trueRangeHistory.Enqueue(workingTrueRange);
				while (trueRangeHistory.Count > Math.Max(5, TrueRangeLookback))
					trueRangeHistory.Dequeue();
			}

			if (IsFinite(workingPrice))
			{
				efficiencyPriceHistory.Enqueue(workingPrice);
				while (efficiencyPriceHistory.Count > Math.Max(5, EfficiencyLookback))
					efficiencyPriceHistory.Dequeue();
			}

			lastCommittedClose = workingClose;
			currentDirectionalEfficiency = CalculateDirectionalEfficiency();
			UpdateRegime(lastCommittedResult);
		}

		private void ResetForTradingSession()
		{
			committedState = new RegressionState();
			lastCommittedResult = null;
			trueRangeHistory.Clear();
			efficiencyPriceHistory.Clear();
			lastCommittedClose = double.NaN;
			currentRegime = OrcaEnvelopeRegime.Unclassified;
			pendingRegime = OrcaEnvelopeRegime.Unclassified;
			pendingRegimeBars = 0;
			currentDirectionalEfficiency = 0;
			currentTrendStrength = 0;

			if (CurrentBar > 0)
			{
				for (int plotIndex = 0; plotIndex < Values.Length; plotIndex++)
					Values[plotIndex].Reset(1);
			}
		}

		private void CaptureWorkingBar()
		{
			workingTime = Time[0];
			workingPrice = GetPrice(0);
			workingHigh = High[0];
			workingLow = Low[0];
			workingClose = Close[0];

			double previousClose = IsFinite(lastCommittedClose)
				? lastCommittedClose
				: workingClose;
			workingTrueRange = Math.Max(
				workingHigh - workingLow,
				Math.Max(
					Math.Abs(workingHigh - previousClose),
					Math.Abs(workingLow - previousClose)));
			workingMedianTrueRange = CalculateMedianTrueRange(workingTrueRange);
		}

		private EnvelopeResult AddSample(
			RegressionState state,
			DateTime sampleTime,
			double samplePrice,
			double medianTrueRange)
		{
			double minimumHalfWidth = Math.Max(
				TickSize * 2,
				Math.Max(TickSize, medianTrueRange) * MinimumWidthMultiple);

			if (!state.Initialized
				|| sampleTime < state.LastTime
				|| !IsFinite(samplePrice))
			{
				InitializeState(state, sampleTime, samplePrice, minimumHalfWidth);
				return BuildResult(state, sampleTime);
			}

			double decay = CalculateDecay(state.LastTime, sampleTime);
			DecayState(state, decay);

			double x = Math.Max(0, (sampleTime - state.OriginTime).TotalMinutes);
			double priorSlope;
			double priorIntercept;
			FitRegression(state, out priorSlope, out priorIntercept);
			double priorCenter = priorIntercept + priorSlope * x;
			double averageHalfWidth = Math.Max(
				minimumHalfWidth,
				(state.UpperHalfWidth + state.LowerHalfWidth) * 0.5);
			double fitPrice = samplePrice;

			if (state.SampleCount >= 3 && OutlierClamp > 0)
			{
				double maximumResidual = Math.Max(TickSize, OutlierClamp * averageHalfWidth);
				fitPrice = priorCenter + Clamp(
					samplePrice - priorCenter,
					-maximumResidual,
					maximumResidual);
			}

			state.Weight += 1.0;
			state.SumX += x;
			state.SumY += fitPrice;
			state.SumXX += x * x;
			state.SumXY += x * fitPrice;
			state.LastTime = sampleTime;
			state.SampleCount++;

			double slope;
			double intercept;
			FitRegression(state, out slope, out intercept);
			double center = intercept + slope * x;
			double residual = samplePrice - center;
			double residualClamp = Math.Max(TickSize, OutlierClamp * averageHalfWidth);
			double widthResidual = Clamp(residual, -residualClamp, residualClamp);

			state.UpperResidualWeight += 1.0;
			state.LowerResidualWeight += 1.0;

			if (widthResidual >= 0)
			{
				state.UpperResidualSquares += widthResidual * widthResidual;
			}
			else
			{
				state.LowerResidualSquares += widthResidual * widthResidual;
			}

			double upperSigma = state.UpperResidualWeight > 0
				? Math.Sqrt(Math.Max(0, 2.0 * state.UpperResidualSquares / state.UpperResidualWeight))
				: 0;
			double lowerSigma = state.LowerResidualWeight > 0
				? Math.Sqrt(Math.Max(0, 2.0 * state.LowerResidualSquares / state.LowerResidualWeight))
				: 0;
			double upperTarget = Math.Max(minimumHalfWidth, DeviationMultiplier * upperSigma);
			double lowerTarget = Math.Max(minimumHalfWidth, DeviationMultiplier * lowerSigma);

			state.UpperHalfWidth = SmoothWidth(state.UpperHalfWidth, upperTarget, minimumHalfWidth);
			state.LowerHalfWidth = SmoothWidth(state.LowerHalfWidth, lowerTarget, minimumHalfWidth);

			return new EnvelopeResult
			{
				Center = center,
				Upper = center + state.UpperHalfWidth,
				Lower = center - state.LowerHalfWidth,
				SlopePerMinute = slope,
				UpperHalfWidth = state.UpperHalfWidth,
				LowerHalfWidth = state.LowerHalfWidth,
				SampleCount = state.SampleCount
			};
		}

		private void InitializeState(
			RegressionState state,
			DateTime sampleTime,
			double samplePrice,
			double minimumHalfWidth)
		{
			double safePrice = IsFinite(samplePrice) ? samplePrice : 0;
			state.Initialized = true;
			state.OriginTime = sampleTime;
			state.LastTime = sampleTime;
			state.Weight = 1;
			state.SumX = 0;
			state.SumY = safePrice;
			state.SumXX = 0;
			state.SumXY = 0;
			state.UpperResidualWeight = 0;
			state.UpperResidualSquares = 0;
			state.LowerResidualWeight = 0;
			state.LowerResidualSquares = 0;
			state.UpperHalfWidth = minimumHalfWidth;
			state.LowerHalfWidth = minimumHalfWidth;
			state.SampleCount = 1;
		}

		private EnvelopeResult BuildResult(RegressionState state, DateTime sampleTime)
		{
			double slope;
			double intercept;
			FitRegression(state, out slope, out intercept);
			double x = Math.Max(0, (sampleTime - state.OriginTime).TotalMinutes);
			double center = intercept + slope * x;
			return new EnvelopeResult
			{
				Center = center,
				Upper = center + state.UpperHalfWidth,
				Lower = center - state.LowerHalfWidth,
				SlopePerMinute = slope,
				UpperHalfWidth = state.UpperHalfWidth,
				LowerHalfWidth = state.LowerHalfWidth,
				SampleCount = state.SampleCount
			};
		}

		private double CalculateDecay(DateTime previousTime, DateTime currentTime)
		{
			double elapsedMinutes = Math.Max(0, (currentTime - previousTime).TotalMinutes);
			double memory = Math.Max(0.25, TrendMemoryMinutes);
			double decay = Math.Exp(Math.Log(0.05) * elapsedMinutes / memory);
			return Clamp(decay, 1e-8, 1.0);
		}

		private static void DecayState(RegressionState state, double decay)
		{
			state.Weight *= decay;
			state.SumX *= decay;
			state.SumY *= decay;
			state.SumXX *= decay;
			state.SumXY *= decay;
			state.UpperResidualWeight *= decay;
			state.UpperResidualSquares *= decay;
			state.LowerResidualWeight *= decay;
			state.LowerResidualSquares *= decay;
		}

		private static void FitRegression(
			RegressionState state,
			out double slope,
			out double intercept)
		{
			if (state == null || state.Weight <= 1e-12)
			{
				slope = 0;
				intercept = 0;
				return;
			}

			double meanX = state.SumX / state.Weight;
			double meanY = state.SumY / state.Weight;
			double centeredXX = state.SumXX - state.SumX * meanX;
			double centeredXY = state.SumXY - state.SumX * meanY;

			if (centeredXX <= 1e-10)
			{
				slope = 0;
				intercept = meanY;
				return;
			}

			slope = centeredXY / centeredXX;
			intercept = meanY - slope * meanX;
		}

		private double SmoothWidth(double current, double target, double minimum)
		{
			if (!IsFinite(current) || current <= 0)
				return Math.Max(minimum, target);

			double rate = target > current ? ExpansionRate : ContractionRate;
			double next = current + Clamp(rate, 0.001, 1.0) * (target - current);
			return Math.Max(minimum, next);
		}

		private void UpdateRegime(EnvelopeResult result)
		{
			if (result == null
				|| result.SampleCount < Math.Max(3, MinimumClassificationSamples))
			{
				ProposeRegime(OrcaEnvelopeRegime.Unclassified);
				return;
			}

			double averageHalfWidth = Math.Max(
				TickSize,
				(result.UpperHalfWidth + result.LowerHalfWidth) * 0.5);
			currentTrendStrength = Math.Abs(result.SlopePerMinute)
				* Math.Max(1, TrendMemoryMinutes)
				/ averageHalfWidth;

			OrcaEnvelopeRegime proposed;
			if (currentTrendStrength >= TrendStrengthThreshold
				&& currentDirectionalEfficiency >= MinimumDirectionalEfficiency)
			{
				proposed = result.SlopePerMinute >= 0
					? OrcaEnvelopeRegime.Rising
					: OrcaEnvelopeRegime.Falling;
			}
			else if (currentTrendStrength <= RangeStrengthThreshold
				|| currentDirectionalEfficiency < MinimumDirectionalEfficiency * 0.70)
			{
				proposed = OrcaEnvelopeRegime.Range;
			}
			else
			{
				proposed = OrcaEnvelopeRegime.Transition;
			}

			ProposeRegime(proposed);
		}

		private void ProposeRegime(OrcaEnvelopeRegime proposed)
		{
			if (proposed == currentRegime)
			{
				pendingRegime = proposed;
				pendingRegimeBars = 0;
				return;
			}

			if (proposed != pendingRegime)
			{
				pendingRegime = proposed;
				pendingRegimeBars = 1;
				return;
			}

			pendingRegimeBars++;
			if (pendingRegimeBars >= Math.Max(1, ClassificationConfirmationBars))
			{
				currentRegime = proposed;
				pendingRegimeBars = 0;
			}
		}

		private double CalculateDirectionalEfficiency()
		{
			if (efficiencyPriceHistory == null || efficiencyPriceHistory.Count < 2)
				return 0;

			double[] prices = efficiencyPriceHistory.ToArray();
			double path = 0;
			for (int i = 1; i < prices.Length; i++)
				path += Math.Abs(prices[i] - prices[i - 1]);
			if (path <= TickSize * 0.25)
				return 0;
			return Clamp(
				Math.Abs(prices[prices.Length - 1] - prices[0]) / path,
				0,
				1);
		}

		private double CalculateMedianTrueRange(double currentTrueRange)
		{
			List<double> values = trueRangeHistory == null
				? new List<double>()
				: trueRangeHistory.Where(v => IsFinite(v) && v > 0).ToList();
			if (IsFinite(currentTrueRange) && currentTrueRange > 0)
				values.Add(currentTrueRange);
			if (values.Count == 0)
				return Math.Max(TickSize, currentTrueRange);

			values.Sort();
			int middle = values.Count / 2;
			return values.Count % 2 == 0
				? (values[middle - 1] + values[middle]) * 0.5
				: values[middle];
		}

		private void WritePlots(EnvelopeResult result)
		{
			if (result == null
				|| !IsFinite(result.Center)
				|| !IsFinite(result.Upper)
				|| !IsFinite(result.Lower))
			{
				for (int plotIndex = 0; plotIndex < Values.Length; plotIndex++)
					Values[plotIndex].Reset(0);
				return;
			}

			Values[0][0] = result.Center;
			Values[1][0] = result.Upper;
			Values[2][0] = result.Lower;
			for (int plotIndex = 3; plotIndex < Values.Length; plotIndex++)
				Values[plotIndex].Reset(0);

			if (!ShowTradeZones)
				return;

			double percentage = Clamp(ZoneWidthPercentage / 100.0, 0.01, 0.50);
			double longInner = result.Lower + result.LowerHalfWidth * percentage;
			double shortInner = result.Upper - result.UpperHalfWidth * percentage;

			if (currentRegime == OrcaEnvelopeRegime.Rising)
			{
				Values[3][0] = longInner;
			}
			else if (currentRegime == OrcaEnvelopeRegime.Falling)
			{
				Values[4][0] = shortInner;
			}
			else if (currentRegime == OrcaEnvelopeRegime.Range)
			{
				Values[5][0] = longInner;
				Values[6][0] = shortInner;
			}
		}

		private void UpdateRegions()
		{
			int startBarsAgo = Math.Min(CurrentBar, Math.Max(50, MaximumFillBars));

			if (FillOpacity > 0)
			{
				Draw.Region(
					this,
					FillTag,
					startBarsAgo,
					0,
					Values[1],
					Values[2],
					null,
					FillColor,
					FillOpacity);
			}

			if (!ShowTradeZones || ZoneOpacity <= 0)
				return;

			UpdateZoneRegionSegment();
			RemoveExpiredZoneRegionSegments();
			DrawActiveZoneRegionSegment();
		}

		private void UpdateZoneRegionSegment()
		{
			OrcaEnvelopeRegime drawableRegime =
				currentRegime == OrcaEnvelopeRegime.Rising
				|| currentRegime == OrcaEnvelopeRegime.Falling
				|| currentRegime == OrcaEnvelopeRegime.Range
					? currentRegime
					: OrcaEnvelopeRegime.Unclassified;

			if (activeZoneRegionSegment != null
				&& activeZoneRegionSegment.Regime == drawableRegime)
			{
				activeZoneRegionSegment.EndBar = CurrentBar;
				return;
			}

			activeZoneRegionSegment = null;
			if (drawableRegime == OrcaEnvelopeRegime.Unclassified)
				return;

			activeZoneRegionSegment = new ZoneRegionSegment
			{
				Id = nextZoneRegionSegmentId++,
				StartBar = CurrentBar,
				EndBar = CurrentBar,
				Regime = drawableRegime
			};
			zoneRegionSegments.Add(activeZoneRegionSegment);
		}

		private void DrawActiveZoneRegionSegment()
		{
			if (activeZoneRegionSegment == null)
				return;

			int segmentStartBarsAgo = Math.Min(
				CurrentBar - activeZoneRegionSegment.StartBar,
				Math.Max(50, MaximumFillBars));
			string baseTag = ZoneTagPrefix + activeZoneRegionSegment.Id;

			if (activeZoneRegionSegment.Regime == OrcaEnvelopeRegime.Rising)
			{
				Draw.Region(
					this,
					baseTag + "-Long",
					segmentStartBarsAgo,
					0,
					Values[3],
					Values[2],
					null,
					LongZoneColor,
					ZoneOpacity);
			}
			else if (activeZoneRegionSegment.Regime == OrcaEnvelopeRegime.Falling)
			{
				Draw.Region(
					this,
					baseTag + "-Short",
					segmentStartBarsAgo,
					0,
					Values[1],
					Values[4],
					null,
					ShortZoneColor,
					ZoneOpacity);
			}
			else if (activeZoneRegionSegment.Regime == OrcaEnvelopeRegime.Range)
			{
				Draw.Region(
					this,
					baseTag + "-RangeLong",
					segmentStartBarsAgo,
					0,
					Values[5],
					Values[2],
					null,
					RangeZoneColor,
					ZoneOpacity);
				Draw.Region(
					this,
					baseTag + "-RangeShort",
					segmentStartBarsAgo,
					0,
					Values[1],
					Values[6],
					null,
					RangeZoneColor,
					ZoneOpacity);
			}
		}

		private void RemoveExpiredZoneRegionSegments()
		{
			int oldestBarToKeep = CurrentBar - Math.Max(50, MaximumFillBars);
			for (int index = zoneRegionSegments.Count - 1; index >= 0; index--)
			{
				ZoneRegionSegment segment = zoneRegionSegments[index];
				if (segment == activeZoneRegionSegment || segment.EndBar >= oldestBarToKeep)
					continue;

				string baseTag = ZoneTagPrefix + segment.Id;
				try { RemoveDrawObject(baseTag + "-Long"); }
				catch { }
				try { RemoveDrawObject(baseTag + "-Short"); }
				catch { }
				try { RemoveDrawObject(baseTag + "-RangeLong"); }
				catch { }
				try { RemoveDrawObject(baseTag + "-RangeShort"); }
				catch { }
				zoneRegionSegments.RemoveAt(index);
			}
		}

		private void UpdateDiagnostics(EnvelopeResult result)
		{
			if (!ShowDiagnostics || result == null)
			{
				if (CurrentBar == 0 || (CurrentBar != workingBar))
				{
					try { RemoveDrawObject(DiagnosticsTag); }
					catch { }
				}
				return;
			}

			string text = string.Format(
				System.Globalization.CultureInfo.InvariantCulture,
				"Orca Adaptive Envelope\nRegime {0} | Pending {1} ({2})\nSlope {3:F3}/min | Strength {4:F2} | Efficiency {5:F2}\nWidth +{6:F2} / -{7:F2} | Samples {8} | Memory {9:F1}m",
				currentRegime,
				pendingRegime,
				pendingRegimeBars,
				result.SlopePerMinute,
				currentTrendStrength,
				currentDirectionalEfficiency,
				result.UpperHalfWidth,
				result.LowerHalfWidth,
				result.SampleCount,
				TrendMemoryMinutes);

			Draw.TextFixed(
				this,
				DiagnosticsTag,
				text,
				TextPosition.TopLeft,
				Brushes.WhiteSmoke,
				new SimpleFont("Segoe UI", 11),
				Brushes.Transparent,
				Brushes.Black,
				70);
		}

		private double GetPrice(int barsAgo)
		{
			switch (PriceSource)
			{
				case OrcaEnvelopePriceSource.Close:
					return Close[barsAgo];
				case OrcaEnvelopePriceSource.MedianPrice:
					return (High[barsAgo] + Low[barsAgo]) * 0.5;
				case OrcaEnvelopePriceSource.OhlcAverage:
					return (Open[barsAgo] + High[barsAgo] + Low[barsAgo] + Close[barsAgo]) * 0.25;
				default:
					return (High[barsAgo] + Low[barsAgo] + Close[barsAgo]) / 3.0;
			}
		}

		private static bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		private static double Clamp(double value, double minimum, double maximum)
		{
			return value < minimum ? minimum : value > maximum ? maximum : value;
		}

		#region Properties - Engine
		[NinjaScriptProperty]
		[Display(Name = "Price Source", Order = 1, GroupName = "01. Engine")]
		public OrcaEnvelopePriceSource PriceSource { get; set; }

		[NinjaScriptProperty]
		[Range(0.25, 240)]
		[Display(Name = "Trend Memory Minutes", Description = "Elapsed trading-session time after which an observation retains five percent of its original regression weight.", Order = 2, GroupName = "01. Engine")]
		public double TrendMemoryMinutes { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 6)]
		[Display(Name = "Deviation Multiplier", Description = "Upper and lower residual-deviation multiplier.", Order = 3, GroupName = "01. Engine")]
		public double DeviationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 1)]
		[Display(Name = "Expansion Rate", Description = "Per completed-bar smoothing rate when the target envelope becomes wider.", Order = 4, GroupName = "01. Engine")]
		public double ExpansionRate { get; set; }

		[NinjaScriptProperty]
		[Range(0.001, 0.50)]
		[Display(Name = "Contraction Rate", Description = "Per completed-bar smoothing rate when the target envelope becomes narrower.", Order = 5, GroupName = "01. Engine")]
		public double ContractionRate { get; set; }

		[NinjaScriptProperty]
		[Range(1.5, 10)]
		[Display(Name = "Outlier Clamp", Description = "Maximum residual, in current half-widths, allowed to influence one regression/width update.", Order = 6, GroupName = "01. Engine")]
		public double OutlierClamp { get; set; }

		[NinjaScriptProperty]
		[Range(5, 100)]
		[Display(Name = "True Range Lookback", Order = 7, GroupName = "01. Engine")]
		public int TrueRangeLookback { get; set; }

		[NinjaScriptProperty]
		[Range(0.10, 5)]
		[Display(Name = "Minimum Width Multiple", Description = "Minimum upper and lower half-width as a multiple of rolling median true range.", Order = 8, GroupName = "01. Engine")]
		public double MinimumWidthMultiple { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Reset at Trading Session", Order = 9, GroupName = "01. Engine")]
		public bool ResetAtTradingSession { get; set; }
		#endregion

		#region Properties - Classification
		[NinjaScriptProperty]
		[Range(5, 100)]
		[Display(Name = "Efficiency Lookback", Order = 1, GroupName = "02. Classification")]
		public int EfficiencyLookback { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name = "Minimum Directional Efficiency", Order = 2, GroupName = "02. Classification")]
		public double MinimumDirectionalEfficiency { get; set; }

		[NinjaScriptProperty]
		[Range(0.05, 5)]
		[Display(Name = "Trend Strength Threshold", Description = "Memory-horizon slope displacement divided by average envelope half-width.", Order = 3, GroupName = "02. Classification")]
		public double TrendStrengthThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0, 2)]
		[Display(Name = "Range Strength Threshold", Order = 4, GroupName = "02. Classification")]
		public double RangeStrengthThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Classification Confirmation Bars", Order = 5, GroupName = "02. Classification")]
		public int ClassificationConfirmationBars { get; set; }

		[NinjaScriptProperty]
		[Range(3, 200)]
		[Display(Name = "Minimum Classification Samples", Order = 6, GroupName = "02. Classification")]
		public int MinimumClassificationSamples { get; set; }
		#endregion

		#region Properties - Zones and visual
		[NinjaScriptProperty]
		[Display(Name = "Show Trade Zones", Order = 1, GroupName = "03. Zones")]
		public bool ShowTradeZones { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Zone Width Percentage", Order = 2, GroupName = "03. Zones")]
		public double ZoneWidthPercentage { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Envelope Fill Opacity", Order = 1, GroupName = "04. Visual")]
		public int FillOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Zone Opacity", Order = 2, GroupName = "04. Visual")]
		public int ZoneOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(50, 20000)]
		[Display(Name = "Maximum Fill Bars", Order = 3, GroupName = "04. Visual")]
		public int MaximumFillBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Diagnostics", Order = 4, GroupName = "04. Visual")]
		public bool ShowDiagnostics { get; set; }

		[XmlIgnore]
		[Display(Name = "Envelope Fill Color", Order = 5, GroupName = "04. Visual")]
		public Brush FillColor { get; set; }

		[Browsable(false)]
		public string FillColorSerializable
		{
			get { return Serialize.BrushToString(FillColor); }
			set { FillColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Long Zone Color", Order = 6, GroupName = "04. Visual")]
		public Brush LongZoneColor { get; set; }

		[Browsable(false)]
		public string LongZoneColorSerializable
		{
			get { return Serialize.BrushToString(LongZoneColor); }
			set { LongZoneColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Short Zone Color", Order = 7, GroupName = "04. Visual")]
		public Brush ShortZoneColor { get; set; }

		[Browsable(false)]
		public string ShortZoneColorSerializable
		{
			get { return Serialize.BrushToString(ShortZoneColor); }
			set { ShortZoneColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Range Zone Color", Order = 8, GroupName = "04. Visual")]
		public Brush RangeZoneColor { get; set; }

		[Browsable(false)]
		public string RangeZoneColorSerializable
		{
			get { return Serialize.BrushToString(RangeZoneColor); }
			set { RangeZoneColor = Serialize.StringToBrush(value); }
		}
		#endregion

		#region Plot accessors
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Centerline
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> UpperEnvelope
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> LowerEnvelope
		{
			get { return Values[2]; }
		}
		#endregion
	}
}
