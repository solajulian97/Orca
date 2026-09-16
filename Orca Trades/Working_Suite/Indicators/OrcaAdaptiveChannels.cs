#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

using DxSolidBrush = SharpDX.Direct2D1.SolidColorBrush;
using DxTextFormat = SharpDX.DirectWrite.TextFormat;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColors = System.Windows.Media.Colors;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum OrcaChannelDetectionMode
	{
		SwingPivot,
		LinearRegression,
		RepeatedTouches,
		Hybrid
	}

	public enum OrcaChannelDisplayMode
	{
		SelectedMethodOnly,
		HybridWinner,
		CompareMethods
	}

	public enum OrcaChannelPriceSource
	{
		Close,
		MedianPrice,
		TypicalPrice,
		OhlcAverage
	}

	public enum OrcaChannelPreset
	{
		UniversalAdaptive,
		RangeChartTight,
		RangeChartBalanced,
		TimeChartBalanced,
		Manual
	}

	public enum OrcaChannelPivotMethod
	{
		BarStrength,
		BarStrengthAndReversal
	}

	public enum OrcaChannelRegressionBoundaryModel
	{
		RobustResidualEnvelope,
		ResidualPercentile,
		StandardDeviations,
		MaximumResidualEnvelope
	}

	public enum OrcaChannelOutlierHandling
	{
		None,
		Winsorize,
		TrimExtremeResiduals
	}

	public enum OrcaChannelClassification
	{
		Unclassified,
		Range,
		Uptrend,
		Downtrend,
		Transition
	}

	public enum OrcaChannelLifecycleState
	{
		Candidate,
		Confirmed,
		Mature,
		Broken,
		Retest,
		Archived
	}

	public enum OrcaChannelConfidenceState
	{
		Low,
		Moderate,
		High
	}

	public enum OrcaChannelBreakoutMode
	{
		IntrabarPenetration,
		CloseOutsideBuffer,
		ConsecutiveCloses,
		CloseAndFollowThrough
	}

	public enum OrcaChannelZoneCalculationMode
	{
		PercentOfChannelWidth,
		Ticks,
		NormalizedUnits,
		RangeBarMultiple
	}

	public enum OrcaChannelRejectionMode
	{
		OneBar,
		TwoBar,
		CloseBackInside
	}

	public enum OrcaChannelPreferredPriceMode
	{
		ExactBoundary,
		ZoneCenter,
		ZoneNearEdge
	}

	public enum OrcaChannelUpdateMode
	{
		OnBarClose,
		OnPriceChange,
		OnEachTick
	}

	public enum OrcaChannelDashStyle
	{
		Solid,
		Dash,
		Dot,
		DashDot
	}

	public enum OrcaChannelLabelPlacement
	{
		UpperLeft,
		UpperRight,
		LowerLeft,
		LowerRight
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaAdaptiveChannels : Indicator
	{
		#region Models
		private sealed class PivotPoint
		{
			public int BarIndex;
			public DateTime Time;
			public double Price;
			public bool IsHigh;
		}

		private sealed class TouchPoint
		{
			public int BarIndex;
			public double Price;
			public bool IsUpper;
		}

		private sealed class CandidateLine
		{
			public int FirstBar;
			public int LastBar;
			public double Slope;
			public double Intercept;
			public bool IsUpper;
		}

		private sealed class ChannelCandidate
		{
			public OrcaChannelDetectionMode Method;
			public string ChannelId = string.Empty;
			public int StartBar;
			public int EndBar;
			public int CreatedBar;
			public int ConfirmationBar = -1;
			public int BreakBar = -1;
			public int LastTouchBar = -1;
			public int UpperTouches;
			public int LowerTouches;
			public int TotalTouches;
			public int BreakoutCount;
			public int ViolationCount;
			public int AgeBars;
			public int HoldBars;
			public int BreakDirection;
			public double UpperSlope;
			public double UpperIntercept;
			public double LowerSlope;
			public double LowerIntercept;
			public double CenterSlope;
			public double CenterIntercept;
			public double CurrentUpper;
			public double CurrentLower;
			public double ChannelWidth;
			public double NormalizedSlope;
			public double NormalizedWidth;
			public double ContainmentPercent;
			public double AverageFitError;
			public double MaximumFitError;
			public double ParallelismScore;
			public double WidthStabilityScore;
			public double DirectionalEfficiency;
			public double CenterlineCrossingRate;
			public double AlternationScore;
			public double ReactionScore;
			public double ConsensusScore;
			public double QualityScore;
			public OrcaChannelClassification Classification;
			public OrcaChannelLifecycleState Lifecycle;
			public OrcaChannelConfidenceState Confidence;
			public List<TouchPoint> Touches = new List<TouchPoint>();

			public double UpperAt(int barIndex)
			{
				return UpperSlope * barIndex + UpperIntercept;
			}

			public double LowerAt(int barIndex)
			{
				return LowerSlope * barIndex + LowerIntercept;
			}

			public double CenterAt(int barIndex)
			{
				return CenterSlope * barIndex + CenterIntercept;
			}

			public ChannelCandidate Clone()
			{
				ChannelCandidate clone = (ChannelCandidate)MemberwiseClone();
				clone.Touches = new List<TouchPoint>();
				for (int i = 0; i < Touches.Count; i++)
				{
					TouchPoint touch = Touches[i];
					clone.Touches.Add(new TouchPoint
					{
						BarIndex = touch.BarIndex,
						Price = touch.Price,
						IsUpper = touch.IsUpper
					});
				}
				return clone;
			}
		}

		private sealed class SignalMarker
		{
			public int BarIndex;
			public DateTime Time;
			public double Price;
			public bool IsLong;
			public string Label = string.Empty;
			public string ChannelId = string.Empty;
		}

		private sealed class ChannelRenderItem
		{
			public ChannelCandidate Candidate;
			public bool IsPrimary;
			public bool IsHistorical;
			public int MethodIndex;
		}

		private sealed class RenderStateSnapshot
		{
			public static readonly RenderStateSnapshot Empty = new RenderStateSnapshot(
				new ChannelRenderItem[0],
				new SignalMarker[0],
				string.Empty,
				string.Empty,
				double.NaN,
				double.NaN);

			public readonly ChannelRenderItem[] Channels;
			public readonly SignalMarker[] Signals;
			public readonly string PrimaryLabel;
			public readonly string DiagnosticsText;
			public readonly double PreferredLongPrice;
			public readonly double PreferredShortPrice;

			public RenderStateSnapshot(
				ChannelRenderItem[] channels,
				SignalMarker[] signals,
				string primaryLabel,
				string diagnosticsText,
				double preferredLongPrice,
				double preferredShortPrice)
			{
				Channels = channels;
				Signals = signals;
				PrimaryLabel = primaryLabel ?? string.Empty;
				DiagnosticsText = diagnosticsText ?? string.Empty;
				PreferredLongPrice = preferredLongPrice;
				PreferredShortPrice = preferredShortPrice;
			}
		}
		#endregion

		#region Fields
		private readonly List<PivotPoint> confirmedPivots = new List<PivotPoint>();
		private readonly List<ChannelCandidate> cachedTouchGeometries = new List<ChannelCandidate>();
		private readonly List<ChannelCandidate> lastCandidates = new List<ChannelCandidate>();
		private readonly List<ChannelCandidate> secondaryChannels = new List<ChannelCandidate>();
		private readonly List<ChannelCandidate> archivedChannels = new List<ChannelCandidate>();
		private readonly List<SignalMarker> signalMarkers = new List<SignalMarker>();

		private ChannelCandidate activeChannel;
		private int lastSeenBar = -1;
		private int lastStructuralEvaluationBar = -1;
		private int pivotVersion;
		private int cachedTouchPivotVersion = -1;
		private int nextChannelSequence = 1;
		private int consecutiveBreakCloses;
		private int lastBreakDirection;
		private int lastBreakCloseBar = -1;
		private bool longZoneArmed = true;
		private bool shortZoneArmed = true;
		private int longZoneEntryBar = -1;
		private int shortZoneEntryBar = -1;
		private string signalChannelId = string.Empty;
		private double normalizationUnit = double.NaN;
		private double lastPivotMilliseconds;
		private double lastCandidateMilliseconds;
		private double lastScoringMilliseconds;
		private double lastRenderMilliseconds;
		private volatile RenderStateSnapshot renderSnapshot = RenderStateSnapshot.Empty;

		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private DxSolidBrush[] dxBrushes;
		private StrokeStyle[] dxStrokes;
		private DxTextFormat dxLabelFormat;
		private DxTextFormat dxSmallFormat;
		private bool dxValid;

		private const int BrushUpper = 0;
		private const int BrushLower = 1;
		private const int BrushCenter = 2;
		private const int BrushFill = 3;
		private const int BrushLong = 4;
		private const int BrushShort = 5;
		private const int BrushRange = 6;
		private const int BrushCandidate = 7;
		private const int BrushBroken = 8;
		private const int BrushText = 9;
		private const int BrushSwing = 10;
		private const int BrushRegression = 11;
		private const int BrushTouches = 12;
		private const int BrushPanel = 13;
		private const int BrushCount = 14;

		private static readonly object ExportSync = new object();
		#endregion

		#region State and updates
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca Adaptive Channels";
				Description = "Multi-engine, non-lookahead adaptive market channels with shared scoring, classification, lifecycle, and trade-location guidance.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DrawOnPricePanel = true;
				DisplayInDataBox = false;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;

				DetectionMode = OrcaChannelDetectionMode.Hybrid;
				DisplayMode = OrcaChannelDisplayMode.HybridWinner;
				CalculationSource = OrcaChannelPriceSource.TypicalPrice;
				MinimumBarsRequired = 60;
				MaximumLookback = 240;
				MaximumActiveChannels = 1;
				ShowHistoricalChannels = true;
				MaximumHistoricalChannels = 6;
				ShowProvisionalChannels = false;
				Preset = OrcaChannelPreset.UniversalAdaptive;
				AutomaticChartTypeNormalization = true;
				ManualNormalizationOverride = 0;
				VolatilityLookback = 20;
				UpdateMode = OrcaChannelUpdateMode.OnPriceChange;

				PivotMethod = OrcaChannelPivotMethod.BarStrengthAndReversal;
				PivotStrength = 3;
				MinimumReversalTicks = 4;
				MinimumReversalNormalizedUnits = 0.35;
				MaximumStoredPivots = 48;
				MinimumPivotSeparation = 4;

				MinimumUpperPivots = 2;
				MinimumLowerPivots = 2;
				ParallelismTolerance = 0.12;
				SwingCandidateLookback = 180;
				MaximumBoundaryError = 0.30;

				MinimumRegressionLength = 40;
				MaximumRegressionLength = 200;
				RegressionLengthStep = 20;
				RegressionSource = OrcaChannelPriceSource.TypicalPrice;
				BoundaryModel = OrcaChannelRegressionBoundaryModel.RobustResidualEnvelope;
				StandardDeviationMultiplier = 2.0;
				ResidualPercentile = 90;
				OutlierHandling = OrcaChannelOutlierHandling.Winsorize;

				MinimumTouches = 4;
				MinimumTouchesPerSide = 2;
				TouchTolerance = 0.20;
				MinimumBarsBetweenTouches = 6;
				MaximumCandidateLines = 36;
				MaximumAllowedViolations = 5;

				EnableSwingEngine = true;
				EnableRegressionEngine = true;
				EnableTouchEngine = true;
				FitWeight = 15;
				TouchWeight = 25;
				ContainmentWeight = 20;
				ParallelismWeight = 10;
				StabilityWeight = 10;
				RecencyWeight = 10;
				ConsensusWeight = 10;
				ViolationPenalty = 12;
				ReplacementScoreMargin = 7;
				MinimumHoldBars = 8;

				RangeSlopeThreshold = 0.05;
				TrendSlopeThreshold = 0.10;
				MinimumDirectionalEfficiency = 0.28;
				MinimumChannelScore = 45;
				ConfirmationScore = 58;
				MatureScore = 75;

				EnableTradeLocations = true;
				EnableTrendLocations = true;
				EnableRangeLocations = true;
				ZoneCalculationMode = OrcaChannelZoneCalculationMode.PercentOfChannelWidth;
				ZoneWidthPercentage = 15;
				ZoneWidthTicks = 8;
				ZoneWidthNormalizedUnits = 0.25;
				RangeBarZoneMultiple = 0.20;
				ShowPreferredPrice = true;
				PreferredPriceMode = OrcaChannelPreferredPriceMode.ZoneCenter;
				EnableCountertrendLocations = false;
				EnableLocationSignals = true;
				EnableRejectionSignals = true;
				RejectionConfirmationMode = OrcaChannelRejectionMode.OneBar;
				RejectionClosePercent = 60;
				RequireDirectionalRejectionBar = false;
				SignalRearmDistance = 0.20;

				BreakoutMode = OrcaChannelBreakoutMode.ConsecutiveCloses;
				BreakoutBuffer = 0.18;
				RequiredConfirmationCloses = 2;
				AllowWickPenetration = true;
				MaximumWickPenetration = 0.35;
				EnableRetestDetection = true;
				RetestTolerance = 0.20;
				ExtendBrokenChannels = true;

				ShowUpperBoundary = true;
				ShowLowerBoundary = true;
				ShowCenterline = true;
				ShowFill = true;
				ShowTradeZones = true;
				ShowLabels = true;
				ShowTouchMarkers = false;
				ShowScore = true;
				ShowMethod = false;
				ShowLifecycleState = false;
				ShowRightSideProjection = true;
				ProjectionBars = 12;
				HistoricalChannelOpacity = 32;
				UpperLineOpacity = 100;
				LowerLineOpacity = 100;
				CenterlineOpacity = 60;
				CandidateLineStyle = OrcaChannelDashStyle.Dash;
				BrokenLineStyle = OrcaChannelDashStyle.DashDot;
				UpperLineColor = WpfBrushes.DeepSkyBlue;
				LowerLineColor = WpfBrushes.DeepSkyBlue;
				CenterlineColor = WpfBrushes.SlateGray;
				FillColor = WpfBrushes.SteelBlue;
				LongZoneColor = WpfBrushes.MediumSeaGreen;
				ShortZoneColor = WpfBrushes.IndianRed;
				RangeZoneColor = WpfBrushes.Goldenrod;
				CandidateChannelColor = WpfBrushes.DarkGray;
				BrokenChannelColor = WpfBrushes.DimGray;
				TextColor = WpfBrushes.WhiteSmoke;
				SwingMethodColor = WpfBrushes.DeepSkyBlue;
				RegressionMethodColor = WpfBrushes.Gold;
				TouchMethodColor = WpfBrushes.MediumOrchid;
				UpperLineWidth = 2;
				LowerLineWidth = 2;
				CenterlineWidth = 1;
				UpperLineStyle = OrcaChannelDashStyle.Solid;
				LowerLineStyle = OrcaChannelDashStyle.Solid;
				CenterlineStyle = OrcaChannelDashStyle.Dash;
				FillOpacity = 8;
				ZoneOpacity = 16;
				TextFontName = "Segoe UI";
				TextSize = 11;
				TextOpacity = 90;
				LabelPlacement = OrcaChannelLabelPlacement.UpperLeft;

				AlertOnLocation = false;
				AlertOnRejection = false;
				AlertOnBreak = false;
				AlertOnRetest = false;
				AlertSound = "Alert1.wav";

				EnableDiagnostics = false;
				ShowDiagnosticsPanel = false;
				LogCandidateScores = false;
				LogChannelChanges = false;
				EnableEvaluationExport = false;
				ExportPath = string.Empty;
				PerformanceTiming = false;
			}
			else if (State == State.Configure)
			{
				Calculate = UpdateMode == OrcaChannelUpdateMode.OnBarClose
					? Calculate.OnBarClose
					: UpdateMode == OrcaChannelUpdateMode.OnEachTick
						? Calculate.OnEachTick
						: Calculate.OnPriceChange;
			}
			else if (State == State.DataLoaded)
			{
				ResetRuntimeState();
			}
			else if (State == State.Terminated)
			{
				DisposeDx();
			}
		}

		private void ResetRuntimeState()
		{
			confirmedPivots.Clear();
			cachedTouchGeometries.Clear();
			lastCandidates.Clear();
			secondaryChannels.Clear();
			archivedChannels.Clear();
			signalMarkers.Clear();
			activeChannel = null;
			lastSeenBar = -1;
			lastStructuralEvaluationBar = -1;
			pivotVersion = 0;
			cachedTouchPivotVersion = -1;
			nextChannelSequence = 1;
			consecutiveBreakCloses = 0;
			lastBreakDirection = 0;
			lastBreakCloseBar = -1;
			longZoneArmed = true;
			shortZoneArmed = true;
			longZoneEntryBar = -1;
			shortZoneEntryBar = -1;
			signalChannelId = string.Empty;
			normalizationUnit = double.NaN;
			renderSnapshot = RenderStateSnapshot.Empty;
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			bool isNewBar = CurrentBar != lastSeenBar;
			bool pivotConfirmed = false;
			if (isNewBar)
			{
				lastSeenBar = CurrentBar;
				normalizationUnit = CalculateNormalizationUnit();
				long pivotStart = TimingStart();
				pivotConfirmed = ConfirmAvailablePivots();
				lastPivotMilliseconds = TimingElapsedMilliseconds(pivotStart);
			}

			int minimumBars = Math.Max(MinimumBarsRequired, Math.Max(PivotStrength * 2 + 2, MinimumRegressionLength));
			if (CurrentBar >= minimumBars)
			{
				int cadence = GetStructuralCadence();
				bool structuralDue = isNewBar
					&& (pivotConfirmed
						|| lastStructuralEvaluationBar < 0
						|| CurrentBar - lastStructuralEvaluationBar >= cadence);
				if (structuralDue)
				{
					EvaluateStructure();
					lastStructuralEvaluationBar = CurrentBar;
				}

				UpdateActiveProjectionAndLifecycle();
				UpdateTradeLocationSignals();
			}

			PublishRenderSnapshot();
		}
		#endregion

		#region Normalization and pivots
		private int GetStructuralCadence()
		{
			switch (Preset)
			{
				case OrcaChannelPreset.RangeChartTight:
				case OrcaChannelPreset.RangeChartBalanced:
					return 1;
				case OrcaChannelPreset.TimeChartBalanced:
					return 3;
				default:
					return 2;
			}
		}

		private double GetPresetToleranceFactor()
		{
			switch (Preset)
			{
				case OrcaChannelPreset.RangeChartTight:
					return 0.75;
				case OrcaChannelPreset.RangeChartBalanced:
					return 0.90;
				case OrcaChannelPreset.TimeChartBalanced:
					return 1.10;
				default:
					return 1.0;
			}
		}

		private double CalculateNormalizationUnit()
		{
			if (ManualNormalizationOverride > 0)
				return Math.Max(TickSize, ManualNormalizationOverride);

			if (AutomaticChartTypeNormalization
				&& BarsPeriod != null
				&& BarsPeriod.BarsPeriodType == BarsPeriodType.Range
				&& BarsPeriod.Value > 0)
			{
				return Math.Max(TickSize, BarsPeriod.Value * TickSize);
			}

			int count = Math.Min(Math.Max(5, VolatilityLookback), CurrentBar);
			if (count <= 0)
				return Math.Max(TickSize, 1e-8);

			List<double> trueRanges = new List<double>(count);
			for (int barsAgo = 0; barsAgo < count; barsAgo++)
			{
				double high = High[barsAgo];
				double low = Low[barsAgo];
				double previousClose = barsAgo + 1 <= CurrentBar ? Close[barsAgo + 1] : Close[barsAgo];
				double tr = Math.Max(high - low, Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose)));
				if (IsFinite(tr) && tr > 0)
					trueRanges.Add(tr);
			}

			if (trueRanges.Count == 0)
				return Math.Max(TickSize, 1e-8);

			trueRanges.Sort();
			int middle = trueRanges.Count / 2;
			double median = trueRanges.Count % 2 == 0
				? (trueRanges[middle - 1] + trueRanges[middle]) * 0.5
				: trueRanges[middle];
			return Math.Max(Math.Max(TickSize, 1e-8), median);
		}

		private bool ConfirmAvailablePivots()
		{
			if (CurrentBar < PivotStrength * 2)
				return false;

			int pivotBarsAgo = PivotStrength;
			int absoluteBar = CurrentBar - pivotBarsAgo;
			bool isHigh = true;
			bool isLow = true;
			double candidateHigh = High[pivotBarsAgo];
			double candidateLow = Low[pivotBarsAgo];

			for (int offset = 1; offset <= PivotStrength; offset++)
			{
				if (candidateHigh <= High[pivotBarsAgo - offset] || candidateHigh < High[pivotBarsAgo + offset])
					isHigh = false;
				if (candidateLow >= Low[pivotBarsAgo - offset] || candidateLow > Low[pivotBarsAgo + offset])
					isLow = false;
			}

			double requiredReversal = Math.Max(
				MinimumReversalTicks * TickSize,
				MinimumReversalNormalizedUnits * GetPresetToleranceFactor() * SafeNormalizationUnit());
			if (PivotMethod == OrcaChannelPivotMethod.BarStrengthAndReversal)
			{
				double localLow = candidateLow;
				double localHigh = candidateHigh;
				for (int offset = -PivotStrength; offset <= PivotStrength; offset++)
				{
					int barsAgo = pivotBarsAgo + offset;
					localLow = Math.Min(localLow, Low[barsAgo]);
					localHigh = Math.Max(localHigh, High[barsAgo]);
				}
				if (candidateHigh - localLow < requiredReversal)
					isHigh = false;
				if (localHigh - candidateLow < requiredReversal)
					isLow = false;
			}

			bool added = false;
			if (isHigh)
				added |= AddConfirmedPivot(absoluteBar, Time[pivotBarsAgo], candidateHigh, true);
			if (isLow)
				added |= AddConfirmedPivot(absoluteBar, Time[pivotBarsAgo], candidateLow, false);
			return added;
		}

		private bool AddConfirmedPivot(int barIndex, DateTime time, double price, bool isHigh)
		{
			for (int i = confirmedPivots.Count - 1; i >= 0; i--)
			{
				PivotPoint existing = confirmedPivots[i];
				if (existing.BarIndex == barIndex && existing.IsHigh == isHigh)
					return false;
				if (existing.IsHigh == isHigh
					&& Math.Abs(existing.BarIndex - barIndex) < MinimumPivotSeparation)
				{
					return false;
				}
			}

			confirmedPivots.Add(new PivotPoint
			{
				BarIndex = barIndex,
				Time = time,
				Price = price,
				IsHigh = isHigh
			});
			confirmedPivots.Sort((a, b) => a.BarIndex.CompareTo(b.BarIndex));
			while (confirmedPivots.Count > MaximumStoredPivots)
				confirmedPivots.RemoveAt(0);
			pivotVersion++;
			return true;
		}

		private double SafeNormalizationUnit()
		{
			return IsFinite(normalizationUnit) && normalizationUnit > 0
				? normalizationUnit
				: Math.Max(TickSize, 1e-8);
		}
		#endregion

		#region Engine orchestration
		private void EvaluateStructure()
		{
			long candidateStart = TimingStart();
			List<ChannelCandidate> candidates = new List<ChannelCandidate>();

			bool compare = DisplayMode == OrcaChannelDisplayMode.CompareMethods;
			bool runSwing = compare ? EnableSwingEngine : ShouldRunEngine(OrcaChannelDetectionMode.SwingPivot);
			bool runRegression = compare ? EnableRegressionEngine : ShouldRunEngine(OrcaChannelDetectionMode.LinearRegression);
			bool runTouches = compare ? EnableTouchEngine : ShouldRunEngine(OrcaChannelDetectionMode.RepeatedTouches);

			if (runSwing)
				candidates.AddRange(BuildSwingPivotCandidates());
			if (runRegression)
				candidates.AddRange(BuildRegressionCandidates());
			if (runTouches)
				candidates.AddRange(BuildRepeatedTouchCandidates());

			lastCandidateMilliseconds = TimingElapsedMilliseconds(candidateStart);
			long scoringStart = TimingStart();
			ApplyConsensusAndScore(candidates);
			List<ChannelCandidate> ranked = DeduplicateAndRank(candidates);
			lastScoringMilliseconds = TimingElapsedMilliseconds(scoringStart);

			lastCandidates.Clear();
			lastCandidates.AddRange(ranked);
			ApplyPrimarySelection(ranked);
			BuildSecondarySelection(ranked);

			if (EnableDiagnostics && LogCandidateScores)
				LogCandidateSummary(ranked);
		}

		private bool ShouldRunEngine(OrcaChannelDetectionMode engine)
		{
			if (DetectionMode == engine)
				return true;
			if (DetectionMode != OrcaChannelDetectionMode.Hybrid)
				return false;
			if (engine == OrcaChannelDetectionMode.SwingPivot)
				return EnableSwingEngine;
			if (engine == OrcaChannelDetectionMode.LinearRegression)
				return EnableRegressionEngine;
			return EnableTouchEngine;
		}

		private List<ChannelCandidate> BuildSwingPivotCandidates()
		{
			List<PivotPoint> highs = RecentPivots(true, 12);
			List<PivotPoint> lows = RecentPivots(false, 12);
			List<ChannelCandidate> result = new List<ChannelCandidate>();
			if (highs.Count < Math.Max(2, MinimumUpperPivots) || lows.Count < Math.Max(2, MinimumLowerPivots))
				return result;

			int maxCandidates = Math.Max(24, MaximumCandidateLines * 3);
			for (int hi1 = 0; hi1 < highs.Count - 1 && result.Count < maxCandidates; hi1++)
			{
				for (int hi2 = hi1 + 1; hi2 < highs.Count && result.Count < maxCandidates; hi2++)
				{
					double upperSlope = Slope(highs[hi1], highs[hi2]);
					for (int lo1 = 0; lo1 < lows.Count - 1 && result.Count < maxCandidates; lo1++)
					{
						for (int lo2 = lo1 + 1; lo2 < lows.Count && result.Count < maxCandidates; lo2++)
						{
							double lowerSlope = Slope(lows[lo1], lows[lo2]);
							if (!SlopesAreParallel(upperSlope, lowerSlope))
								continue;

							int start = Math.Max(CurrentBar - SwingCandidateLookback + 1,
								Math.Min(Math.Min(highs[hi1].BarIndex, highs[hi2].BarIndex),
									Math.Min(lows[lo1].BarIndex, lows[lo2].BarIndex)));
							if (CurrentBar - start + 1 < MinimumRegressionLength)
								continue;

							ChannelCandidate candidate = CreateCandidate(
								OrcaChannelDetectionMode.SwingPivot,
								start,
								CurrentBar,
								upperSlope,
								AverageIntercept(highs[hi1], highs[hi2], upperSlope),
								lowerSlope,
								AverageIntercept(lows[lo1], lows[lo2], lowerSlope));
							if (EvaluateCandidate(candidate))
								result.Add(candidate);
						}
					}
				}
			}
			return result;
		}

		private List<ChannelCandidate> BuildRegressionCandidates()
		{
			List<ChannelCandidate> result = new List<ChannelCandidate>();
			int maximum = Math.Min(MaximumRegressionLength, Math.Min(MaximumLookback, CurrentBar + 1));
			int minimum = Math.Min(MinimumRegressionLength, maximum);
			int step = Math.Max(1, RegressionLengthStep);

			for (int length = minimum; length <= maximum; length += step)
			{
				int startBar = CurrentBar - length + 1;
				double slope;
				double intercept;
				if (!TryLinearRegression(startBar, CurrentBar, RegressionSource, out slope, out intercept))
					continue;

				List<double> residuals = new List<double>(length);
				for (int bar = startBar; bar <= CurrentBar; bar++)
					residuals.Add(GetPriceAtBar(bar, RegressionSource) - (slope * bar + intercept));

				double upperOffset;
				double lowerOffset;
				if (!TryGetRegressionOffsets(residuals, out upperOffset, out lowerOffset))
					continue;

				double minimumWidth = Math.Max(TickSize * 2, SafeNormalizationUnit() * 0.20);
				if (upperOffset - lowerOffset < minimumWidth)
				{
					double half = minimumWidth * 0.5;
					double middle = (upperOffset + lowerOffset) * 0.5;
					upperOffset = middle + half;
					lowerOffset = middle - half;
				}

				ChannelCandidate candidate = CreateCandidate(
					OrcaChannelDetectionMode.LinearRegression,
					startBar,
					CurrentBar,
					slope,
					intercept + upperOffset,
					slope,
					intercept + lowerOffset);
				if (EvaluateCandidate(candidate))
					result.Add(candidate);
			}
			return result;
		}

		private List<ChannelCandidate> BuildRepeatedTouchCandidates()
		{
			if (cachedTouchPivotVersion != pivotVersion)
			{
				cachedTouchGeometries.Clear();
				List<CandidateLine> upperLines = BuildPivotPairLines(true);
				List<CandidateLine> lowerLines = BuildPivotPairLines(false);
				int maximum = Math.Max(12, MaximumCandidateLines * 2);

				for (int upperIndex = 0; upperIndex < upperLines.Count && cachedTouchGeometries.Count < maximum; upperIndex++)
				{
					CandidateLine upper = upperLines[upperIndex];
					for (int lowerIndex = 0; lowerIndex < lowerLines.Count && cachedTouchGeometries.Count < maximum; lowerIndex++)
					{
						CandidateLine lower = lowerLines[lowerIndex];
						if (!SlopesAreParallel(upper.Slope, lower.Slope))
							continue;

						int start = Math.Max(
							CurrentBar - Math.Min(MaximumLookback, SwingCandidateLookback) + 1,
							Math.Min(upper.FirstBar, lower.FirstBar));
						ChannelCandidate geometry = CreateCandidate(
							OrcaChannelDetectionMode.RepeatedTouches,
							start,
							CurrentBar,
							upper.Slope,
							upper.Intercept,
							lower.Slope,
							lower.Intercept);
						cachedTouchGeometries.Add(geometry);
					}
				}
				cachedTouchPivotVersion = pivotVersion;
			}

			List<ChannelCandidate> result = new List<ChannelCandidate>();
			for (int i = 0; i < cachedTouchGeometries.Count; i++)
			{
				ChannelCandidate candidate = CopyGeometry(cachedTouchGeometries[i]);
				candidate.EndBar = CurrentBar;
				if (EvaluateCandidate(candidate))
					result.Add(candidate);
			}
			return result;
		}

		private List<CandidateLine> BuildPivotPairLines(bool isUpper)
		{
			List<PivotPoint> pivots = RecentPivots(isUpper, 14);
			List<CandidateLine> lines = new List<CandidateLine>();
			for (int first = 0; first < pivots.Count - 1; first++)
			{
				for (int second = first + 1; second < pivots.Count; second++)
				{
					if (pivots[second].BarIndex - pivots[first].BarIndex < MinimumBarsBetweenTouches)
						continue;
					double slope = Slope(pivots[first], pivots[second]);
					lines.Add(new CandidateLine
					{
						FirstBar = pivots[first].BarIndex,
						LastBar = pivots[second].BarIndex,
						Slope = slope,
						Intercept = (pivots[first].Price - slope * pivots[first].BarIndex
							+ pivots[second].Price - slope * pivots[second].BarIndex) * 0.5,
						IsUpper = isUpper
					});
				}
			}

			lines.Sort((a, b) => b.LastBar.CompareTo(a.LastBar));
			if (lines.Count > MaximumCandidateLines)
				lines.RemoveRange(MaximumCandidateLines, lines.Count - MaximumCandidateLines);
			return lines;
		}

		private List<PivotPoint> RecentPivots(bool highs, int maximum)
		{
			int earliest = Math.Max(0, CurrentBar - MaximumLookback + 1);
			List<PivotPoint> result = confirmedPivots
				.Where(p => p.IsHigh == highs && p.BarIndex >= earliest)
				.OrderByDescending(p => p.BarIndex)
				.Take(Math.Max(2, maximum))
				.OrderBy(p => p.BarIndex)
				.ToList();
			return result;
		}
		#endregion

		#region Candidate evaluation and scoring
		private ChannelCandidate CreateCandidate(
			OrcaChannelDetectionMode method,
			int startBar,
			int endBar,
			double upperSlope,
			double upperIntercept,
			double lowerSlope,
			double lowerIntercept)
		{
			return new ChannelCandidate
			{
				Method = method,
				StartBar = Math.Max(0, startBar),
				EndBar = Math.Max(startBar, endBar),
				CreatedBar = CurrentBar,
				UpperSlope = upperSlope,
				UpperIntercept = upperIntercept,
				LowerSlope = lowerSlope,
				LowerIntercept = lowerIntercept,
				CenterSlope = (upperSlope + lowerSlope) * 0.5,
				CenterIntercept = (upperIntercept + lowerIntercept) * 0.5,
				Lifecycle = OrcaChannelLifecycleState.Candidate,
				Classification = OrcaChannelClassification.Unclassified,
				Confidence = OrcaChannelConfidenceState.Low
			};
		}

		private ChannelCandidate CopyGeometry(ChannelCandidate source)
		{
			return CreateCandidate(
				source.Method,
				source.StartBar,
				source.EndBar,
				source.UpperSlope,
				source.UpperIntercept,
				source.LowerSlope,
				source.LowerIntercept);
		}

		private bool EvaluateCandidate(ChannelCandidate candidate)
		{
			if (candidate == null
				|| candidate.StartBar < 0
				|| candidate.EndBar > CurrentBar
				|| candidate.EndBar <= candidate.StartBar
				|| !IsFinite(candidate.UpperSlope)
				|| !IsFinite(candidate.LowerSlope)
				|| !IsFinite(candidate.UpperIntercept)
				|| !IsFinite(candidate.LowerIntercept))
			{
				return false;
			}

			candidate.Touches.Clear();
			candidate.UpperTouches = 0;
			candidate.LowerTouches = 0;
			candidate.TotalTouches = 0;
			candidate.ViolationCount = 0;
			candidate.BreakoutCount = 0;
			candidate.LastTouchBar = -1;

			double unit = SafeNormalizationUnit();
			double tolerance = Math.Max(TickSize, TouchTolerance * GetPresetToleranceFactor() * unit);
			double boundaryLimit = Math.Max(tolerance, MaximumBoundaryError * unit);
			int contained = 0;
			int observations = 0;
			double widthSum = 0;
			double minimumWidth = double.MaxValue;
			double maximumWidth = 0;
			double fitErrorSum = 0;
			double fitErrorMaximum = 0;
			int fitObservations = 0;
			double path = 0;
			double firstPrice = GetPriceAtBar(candidate.StartBar, CalculationSource);
			double previousPrice = firstPrice;
			int centerCrossings = 0;
			int previousCenterSide = 0;

			List<TouchPoint> touchEvents = new List<TouchPoint>();
			int lastUpperTouch = int.MinValue / 2;
			int lastLowerTouch = int.MinValue / 2;

			for (int bar = candidate.StartBar; bar <= candidate.EndBar; bar++)
			{
				double upper = candidate.UpperAt(bar);
				double lower = candidate.LowerAt(bar);
				if (!IsFinite(upper) || !IsFinite(lower) || upper <= lower + TickSize * 0.25)
					return false;

				double high = GetHighAtBar(bar);
				double low = GetLowAtBar(bar);
				double close = GetCloseAtBar(bar);
				double width = upper - lower;
				widthSum += width;
				minimumWidth = Math.Min(minimumWidth, width);
				maximumWidth = Math.Max(maximumWidth, width);
				observations++;

				if (close <= upper && close >= lower)
					contained++;
				else
				{
					double outside = close > upper ? close - upper : lower - close;
					if (outside > BreakoutBuffer * unit)
						candidate.ViolationCount++;
					if (outside > Math.Max(BreakoutBuffer, MaximumWickPenetration) * unit)
						candidate.BreakoutCount++;
				}

				double currentPrice = GetPriceAtBar(bar, CalculationSource);
				if (bar > candidate.StartBar)
					path += Math.Abs(currentPrice - previousPrice);
				previousPrice = currentPrice;

				double centerDistance = currentPrice - candidate.CenterAt(bar);
				int centerSide = centerDistance > TickSize * 0.25 ? 1 : centerDistance < -TickSize * 0.25 ? -1 : 0;
				if (centerSide != 0 && previousCenterSide != 0 && centerSide != previousCenterSide)
					centerCrossings++;
				if (centerSide != 0)
					previousCenterSide = centerSide;

				if (candidate.Method == OrcaChannelDetectionMode.LinearRegression)
				{
					double upperError = Math.Abs(high - upper);
					double lowerError = Math.Abs(low - lower);
					if (upperError <= tolerance && bar - lastUpperTouch >= MinimumBarsBetweenTouches)
					{
						AddTouch(touchEvents, bar, high, true);
						lastUpperTouch = bar;
					}
					if (lowerError <= tolerance && bar - lastLowerTouch >= MinimumBarsBetweenTouches)
					{
						AddTouch(touchEvents, bar, low, false);
						lastLowerTouch = bar;
					}
					double nearestError = Math.Min(upperError, lowerError);
					if (nearestError <= boundaryLimit)
					{
						fitErrorSum += nearestError;
						fitErrorMaximum = Math.Max(fitErrorMaximum, nearestError);
						fitObservations++;
					}
				}
			}

			if (candidate.Method != OrcaChannelDetectionMode.LinearRegression)
			{
				List<PivotPoint> pivots = confirmedPivots
					.Where(p => p.BarIndex >= candidate.StartBar && p.BarIndex <= candidate.EndBar)
					.OrderBy(p => p.BarIndex)
					.ToList();
				for (int i = 0; i < pivots.Count; i++)
				{
					PivotPoint pivot = pivots[i];
					double boundary = pivot.IsHigh ? candidate.UpperAt(pivot.BarIndex) : candidate.LowerAt(pivot.BarIndex);
					double error = Math.Abs(pivot.Price - boundary);
					fitErrorSum += error;
					fitErrorMaximum = Math.Max(fitErrorMaximum, error);
					fitObservations++;
					if (error > tolerance)
						continue;

					if (pivot.IsHigh && pivot.BarIndex - lastUpperTouch >= MinimumBarsBetweenTouches)
					{
						AddTouch(touchEvents, pivot.BarIndex, pivot.Price, true);
						lastUpperTouch = pivot.BarIndex;
					}
					else if (!pivot.IsHigh && pivot.BarIndex - lastLowerTouch >= MinimumBarsBetweenTouches)
					{
						AddTouch(touchEvents, pivot.BarIndex, pivot.Price, false);
						lastLowerTouch = pivot.BarIndex;
					}
				}
			}

			touchEvents.Sort((a, b) => a.BarIndex.CompareTo(b.BarIndex));
			int alternations = 0;
			int reactions = 0;
			for (int i = 0; i < touchEvents.Count; i++)
			{
				TouchPoint touch = touchEvents[i];
				candidate.Touches.Add(touch);
				if (touch.IsUpper)
					candidate.UpperTouches++;
				else
					candidate.LowerTouches++;
				candidate.LastTouchBar = Math.Max(candidate.LastTouchBar, touch.BarIndex);
				if (i > 0 && touchEvents[i - 1].IsUpper != touch.IsUpper)
					alternations++;

				int reactionBar = touch.BarIndex + 1;
				if (reactionBar <= candidate.EndBar)
				{
					double reactionClose = GetCloseAtBar(reactionBar);
					double touchClose = GetCloseAtBar(touch.BarIndex);
					if (touch.IsUpper && touchClose - reactionClose >= tolerance * 0.5)
						reactions++;
					else if (!touch.IsUpper && reactionClose - touchClose >= tolerance * 0.5)
						reactions++;
				}
			}

			candidate.TotalTouches = candidate.UpperTouches + candidate.LowerTouches;
			candidate.AgeBars = candidate.EndBar - candidate.StartBar + 1;
			candidate.CurrentUpper = candidate.UpperAt(CurrentBar);
			candidate.CurrentLower = candidate.LowerAt(CurrentBar);
			candidate.ChannelWidth = observations > 0 ? widthSum / observations : double.NaN;
			candidate.NormalizedWidth = candidate.ChannelWidth / unit;
			candidate.NormalizedSlope = candidate.CenterSlope / unit;
			candidate.ContainmentPercent = observations > 0 ? 100.0 * contained / observations : 0;
			candidate.AverageFitError = fitObservations > 0 ? fitErrorSum / fitObservations : boundaryLimit * 2;
			candidate.MaximumFitError = fitErrorMaximum;
			candidate.WidthStabilityScore = candidate.ChannelWidth > 0
				? Clamp01(1.0 - (maximumWidth - minimumWidth) / candidate.ChannelWidth)
				: 0;
			candidate.ParallelismScore = CalculateParallelismScore(candidate.UpperSlope, candidate.LowerSlope);
			candidate.DirectionalEfficiency = path > 0
				? Clamp01(Math.Abs(previousPrice - firstPrice) / path)
				: 0;
			candidate.CenterlineCrossingRate = observations > 1
				? Clamp01((double)centerCrossings / (observations - 1))
				: 0;
			candidate.AlternationScore = touchEvents.Count > 1
				? Clamp01((double)alternations / (touchEvents.Count - 1))
				: 0;
			candidate.ReactionScore = touchEvents.Count > 0
				? Clamp01((double)reactions / touchEvents.Count)
				: 0;

			if (!IsFinite(candidate.ChannelWidth)
				|| candidate.ChannelWidth <= TickSize
				|| candidate.NormalizedWidth > 20
				|| candidate.ViolationCount > MaximumAllowedViolations)
			{
				return false;
			}

			candidate.Classification = ClassifyCandidate(candidate);
			ScoreCandidate(candidate);
			AssignLifecycle(candidate);
			return true;
		}

		private void AddTouch(List<TouchPoint> touches, int barIndex, double price, bool isUpper)
		{
			touches.Add(new TouchPoint
			{
				BarIndex = barIndex,
				Price = price,
				IsUpper = isUpper
			});
		}

		private void ScoreCandidate(ChannelCandidate candidate)
		{
			double unit = SafeNormalizationUnit();
			double tolerance = Math.Max(TickSize, TouchTolerance * GetPresetToleranceFactor() * unit);
			double fit = Clamp01(1.0 - candidate.AverageFitError / Math.Max(TickSize, tolerance * 2));
			double touchVolume = Clamp01(candidate.TotalTouches / 6.0);
			double touchBalance = candidate.TotalTouches > 0
				? 1.0 - Math.Abs(candidate.UpperTouches - candidate.LowerTouches) / (double)candidate.TotalTouches
				: 0;
			double touch = Clamp01(touchVolume * 0.55
				+ touchBalance * 0.20
				+ candidate.AlternationScore * 0.15
				+ candidate.ReactionScore * 0.10);
			double containment = Clamp01(candidate.ContainmentPercent / 100.0);
			double stability = Clamp01(candidate.WidthStabilityScore * 0.60
				+ Clamp01(candidate.AgeBars / 120.0) * 0.25
				+ (1.0 - Clamp01(candidate.CenterlineCrossingRate * 0.5)) * 0.15);
			int barsSinceTouch = candidate.LastTouchBar >= 0 ? CurrentBar - candidate.LastTouchBar : candidate.AgeBars;
			double recency = Clamp01(1.0 - barsSinceTouch / Math.Max(12.0, candidate.AgeBars * 0.75));
			double totalWeight = Math.Max(1,
				FitWeight + TouchWeight + ContainmentWeight + ParallelismWeight
				+ StabilityWeight + RecencyWeight + ConsensusWeight);
			double positive = FitWeight * fit
				+ TouchWeight * touch
				+ ContainmentWeight * containment
				+ ParallelismWeight * candidate.ParallelismScore
				+ StabilityWeight * stability
				+ RecencyWeight * recency
				+ ConsensusWeight * candidate.ConsensusScore;
			double violationRate = candidate.AgeBars > 0
				? Clamp01(candidate.ViolationCount / (double)candidate.AgeBars)
				: 1;
			double oneSidedPenalty = candidate.UpperTouches == 0 || candidate.LowerTouches == 0 ? 8 : 0;
			candidate.QualityScore = Clamp(100.0 * positive / totalWeight
				- ViolationPenalty * violationRate
				- oneSidedPenalty, 0, 100);
			candidate.Confidence = candidate.QualityScore >= MatureScore
				? OrcaChannelConfidenceState.High
				: candidate.QualityScore >= ConfirmationScore
					? OrcaChannelConfidenceState.Moderate
					: OrcaChannelConfidenceState.Low;
		}

		private void ApplyConsensusAndScore(List<ChannelCandidate> candidates)
		{
			for (int i = 0; i < candidates.Count; i++)
			{
				HashSet<OrcaChannelDetectionMode> agreeingMethods = new HashSet<OrcaChannelDetectionMode>();
				agreeingMethods.Add(candidates[i].Method);
				for (int j = 0; j < candidates.Count; j++)
				{
					if (i == j || candidates[i].Method == candidates[j].Method)
						continue;
					if (AreSimilar(candidates[i], candidates[j]))
						agreeingMethods.Add(candidates[j].Method);
				}
				candidates[i].ConsensusScore = Clamp01((agreeingMethods.Count - 1) / 2.0);
				ScoreCandidate(candidates[i]);
				AssignLifecycle(candidates[i]);
			}
		}

		private List<ChannelCandidate> DeduplicateAndRank(List<ChannelCandidate> candidates)
		{
			List<ChannelCandidate> ordered = candidates
				.Where(c => c != null && c.QualityScore >= MinimumChannelScore)
				.OrderByDescending(c => c.QualityScore)
				.ThenByDescending(c => c.TotalTouches)
				.ThenByDescending(c => c.AgeBars)
				.ToList();
			List<ChannelCandidate> result = new List<ChannelCandidate>();
			for (int i = 0; i < ordered.Count; i++)
			{
				bool duplicate = false;
				for (int j = 0; j < result.Count; j++)
				{
					if (ordered[i].Method == result[j].Method && AreSimilar(ordered[i], result[j]))
					{
						duplicate = true;
						break;
					}
				}
				if (!duplicate)
					result.Add(ordered[i]);
				if (result.Count >= 24)
					break;
			}
			return result;
		}

		private OrcaChannelClassification ClassifyCandidate(ChannelCandidate candidate)
		{
			double slope = candidate.NormalizedSlope;
			double efficiency = candidate.DirectionalEfficiency;
			double containment = candidate.ContainmentPercent / 100.0;
			double rotation = candidate.CenterlineCrossingRate;

			if (Math.Abs(slope) <= RangeSlopeThreshold
				&& efficiency < MinimumDirectionalEfficiency
				&& containment >= 0.65
				&& rotation >= 0.04)
			{
				return OrcaChannelClassification.Range;
			}
			if (slope >= TrendSlopeThreshold
				&& efficiency >= MinimumDirectionalEfficiency
				&& containment >= 0.55)
			{
				return OrcaChannelClassification.Uptrend;
			}
			if (slope <= -TrendSlopeThreshold
				&& efficiency >= MinimumDirectionalEfficiency
				&& containment >= 0.55)
			{
				return OrcaChannelClassification.Downtrend;
			}
			return OrcaChannelClassification.Transition;
		}

		private void AssignLifecycle(ChannelCandidate candidate)
		{
			bool enoughTouches = candidate.TotalTouches >= MinimumTouches
				&& candidate.UpperTouches >= MinimumTouchesPerSide
				&& candidate.LowerTouches >= MinimumTouchesPerSide;
			if (candidate.QualityScore >= MatureScore
				&& enoughTouches
				&& candidate.AgeBars >= MinimumRegressionLength * 2)
			{
				candidate.Lifecycle = OrcaChannelLifecycleState.Mature;
			}
			else if (candidate.QualityScore >= ConfirmationScore
				&& enoughTouches
				&& candidate.AgeBars >= MinimumRegressionLength)
			{
				candidate.Lifecycle = OrcaChannelLifecycleState.Confirmed;
			}
			else
			{
				candidate.Lifecycle = OrcaChannelLifecycleState.Candidate;
			}
		}
		#endregion

		#region Selection, lifecycle, and stability
		private void ApplyPrimarySelection(List<ChannelCandidate> ranked)
		{
			if (activeChannel != null
				&& activeChannel.Lifecycle != OrcaChannelLifecycleState.Archived
				&& activeChannel.BreakBar < 0)
			{
				ChannelCandidate refreshed = CopyGeometry(activeChannel);
				refreshed.ChannelId = activeChannel.ChannelId;
				refreshed.CreatedBar = activeChannel.CreatedBar;
				refreshed.ConfirmationBar = activeChannel.ConfirmationBar;
				refreshed.HoldBars = activeChannel.HoldBars;
				refreshed.EndBar = CurrentBar;
				if (EvaluateCandidate(refreshed))
					MergeActiveState(activeChannel, refreshed);
			}

			if (ranked == null || ranked.Count == 0)
				return;

			ChannelCandidate challenger = ranked[0];
			if (activeChannel == null)
			{
				ActivateCandidate(challenger);
				return;
			}

			if (AreSimilar(activeChannel, challenger))
			{
				AdoptCandidateGeometry(activeChannel, challenger);
				return;
			}

			bool activeInvalid = activeChannel.Lifecycle == OrcaChannelLifecycleState.Broken
				|| activeChannel.Lifecycle == OrcaChannelLifecycleState.Retest
				|| activeChannel.BreakBar >= 0;
			bool holdComplete = CurrentBar - activeChannel.CreatedBar >= MinimumHoldBars;
			bool challengerClearlyBetter = challenger.QualityScore
				>= activeChannel.QualityScore + ReplacementScoreMargin;

			if ((activeInvalid && challenger.Lifecycle != OrcaChannelLifecycleState.Candidate)
				|| (holdComplete && challengerClearlyBetter))
			{
				ArchiveActiveChannel();
				ActivateCandidate(challenger);
			}
		}

		private void BuildSecondarySelection(List<ChannelCandidate> ranked)
		{
			secondaryChannels.Clear();
			if (MaximumActiveChannels <= 1 || ranked == null)
				return;

			for (int i = 0; i < ranked.Count && secondaryChannels.Count < MaximumActiveChannels - 1; i++)
			{
				ChannelCandidate candidate = ranked[i];
				if (activeChannel != null && AreSimilar(activeChannel, candidate))
					continue;
				bool duplicate = false;
				for (int j = 0; j < secondaryChannels.Count; j++)
				{
					if (AreSimilar(secondaryChannels[j], candidate))
					{
						duplicate = true;
						break;
					}
				}
				if (!duplicate)
					secondaryChannels.Add(candidate.Clone());
			}
		}

		private void ActivateCandidate(ChannelCandidate candidate)
		{
			activeChannel = candidate.Clone();
			activeChannel.ChannelId = string.Format(
				CultureInfo.InvariantCulture,
				"OAC-{0:D6}",
				nextChannelSequence++);
			activeChannel.CreatedBar = CurrentBar;
			activeChannel.HoldBars = 0;
			if (activeChannel.Lifecycle != OrcaChannelLifecycleState.Candidate)
				activeChannel.ConfirmationBar = CurrentBar;
			ResetSignalState(activeChannel.ChannelId);
			if (EnableDiagnostics && LogChannelChanges)
				Print(string.Format(
					CultureInfo.InvariantCulture,
					"ORCA_CHANNEL activated id={0} method={1} score={2:F1} class={3}",
					activeChannel.ChannelId,
					activeChannel.Method,
					activeChannel.QualityScore,
					activeChannel.Classification));
			QueueEvaluationExport("Activated", activeChannel);
		}

		private void AdoptCandidateGeometry(ChannelCandidate target, ChannelCandidate source)
		{
			OrcaChannelLifecycleState oldLifecycle = target.Lifecycle;
			int oldConfirmationBar = target.ConfirmationBar;
			int oldCreatedBar = target.CreatedBar;
			string id = target.ChannelId;
			MergeActiveState(target, source);
			target.ChannelId = id;
			target.CreatedBar = oldCreatedBar;
			target.HoldBars = Math.Max(0, CurrentBar - oldCreatedBar);

			if (oldLifecycle == OrcaChannelLifecycleState.Mature
				&& target.Lifecycle != OrcaChannelLifecycleState.Broken
				&& target.Lifecycle != OrcaChannelLifecycleState.Retest)
			{
				target.Lifecycle = OrcaChannelLifecycleState.Mature;
			}
			else if (oldLifecycle == OrcaChannelLifecycleState.Confirmed
				&& target.Lifecycle == OrcaChannelLifecycleState.Candidate)
			{
				target.Lifecycle = OrcaChannelLifecycleState.Confirmed;
			}

			if (oldConfirmationBar >= 0)
				target.ConfirmationBar = oldConfirmationBar;
			else if (target.Lifecycle == OrcaChannelLifecycleState.Confirmed
				|| target.Lifecycle == OrcaChannelLifecycleState.Mature)
				target.ConfirmationBar = CurrentBar;
		}

		private void MergeActiveState(ChannelCandidate target, ChannelCandidate source)
		{
			string id = target.ChannelId;
			int created = target.CreatedBar;
			int confirmed = target.ConfirmationBar;
			int breakBar = target.BreakBar;
			int breakDirection = target.BreakDirection;
			OrcaChannelLifecycleState lifecycle = target.Lifecycle;

			target.Method = source.Method;
			target.StartBar = source.StartBar;
			target.EndBar = source.EndBar;
			target.UpperSlope = source.UpperSlope;
			target.UpperIntercept = source.UpperIntercept;
			target.LowerSlope = source.LowerSlope;
			target.LowerIntercept = source.LowerIntercept;
			target.CenterSlope = source.CenterSlope;
			target.CenterIntercept = source.CenterIntercept;
			target.CurrentUpper = source.CurrentUpper;
			target.CurrentLower = source.CurrentLower;
			target.ChannelWidth = source.ChannelWidth;
			target.NormalizedSlope = source.NormalizedSlope;
			target.NormalizedWidth = source.NormalizedWidth;
			target.ContainmentPercent = source.ContainmentPercent;
			target.AverageFitError = source.AverageFitError;
			target.MaximumFitError = source.MaximumFitError;
			target.ParallelismScore = source.ParallelismScore;
			target.WidthStabilityScore = source.WidthStabilityScore;
			target.DirectionalEfficiency = source.DirectionalEfficiency;
			target.CenterlineCrossingRate = source.CenterlineCrossingRate;
			target.AlternationScore = source.AlternationScore;
			target.ReactionScore = source.ReactionScore;
			target.ConsensusScore = source.ConsensusScore;
			target.QualityScore = source.QualityScore;
			target.Classification = source.Classification;
			target.Confidence = source.Confidence;
			target.UpperTouches = source.UpperTouches;
			target.LowerTouches = source.LowerTouches;
			target.TotalTouches = source.TotalTouches;
			target.ViolationCount = source.ViolationCount;
			target.BreakoutCount = source.BreakoutCount;
			target.LastTouchBar = source.LastTouchBar;
			target.AgeBars = source.AgeBars;
			target.Touches = source.Touches.Select(t => new TouchPoint
			{
				BarIndex = t.BarIndex,
				Price = t.Price,
				IsUpper = t.IsUpper
			}).ToList();

			target.ChannelId = id;
			target.CreatedBar = created;
			target.ConfirmationBar = confirmed;
			target.BreakBar = breakBar;
			target.BreakDirection = breakDirection;
			target.Lifecycle = breakBar >= 0 ? lifecycle : source.Lifecycle;
		}

		private void ArchiveActiveChannel()
		{
			if (activeChannel == null)
				return;
			ChannelCandidate archived = activeChannel.Clone();
			archived.Lifecycle = OrcaChannelLifecycleState.Archived;
			archivedChannels.Add(archived);
			while (archivedChannels.Count > MaximumHistoricalChannels)
				archivedChannels.RemoveAt(0);
			QueueEvaluationExport("Archived", archived);
		}

		private void UpdateActiveProjectionAndLifecycle()
		{
			if (activeChannel == null)
				return;

			activeChannel.EndBar = CurrentBar;
			activeChannel.CurrentUpper = activeChannel.UpperAt(CurrentBar);
			activeChannel.CurrentLower = activeChannel.LowerAt(CurrentBar);
			activeChannel.HoldBars = Math.Max(0, CurrentBar - activeChannel.CreatedBar);

			if (activeChannel.BreakBar >= 0)
			{
				if (EnableRetestDetection)
					UpdateRetestState();
				return;
			}

			double unit = SafeNormalizationUnit();
			double upper = activeChannel.CurrentUpper;
			double lower = activeChannel.CurrentLower;
			double close = Close[0];
			double high = High[0];
			double low = Low[0];
			double buffer = Math.Max(TickSize, BreakoutBuffer * unit);
			bool upperCloseBreak = close > upper + buffer;
			bool lowerCloseBreak = close < lower - buffer;
			bool upperWickBreak = high > upper + Math.Max(buffer, MaximumWickPenetration * unit);
			bool lowerWickBreak = low < lower - Math.Max(buffer, MaximumWickPenetration * unit);
			bool breakNow = false;
			int direction = 0;

			switch (BreakoutMode)
			{
				case OrcaChannelBreakoutMode.IntrabarPenetration:
					if (upperWickBreak) direction = 1;
					else if (lowerWickBreak) direction = -1;
					breakNow = direction != 0;
					break;

				case OrcaChannelBreakoutMode.CloseOutsideBuffer:
					if (upperCloseBreak) direction = 1;
					else if (lowerCloseBreak) direction = -1;
					breakNow = direction != 0;
					break;

				case OrcaChannelBreakoutMode.CloseAndFollowThrough:
					if (upperCloseBreak && Close[0] > Open[0] && (CurrentBar < 1 || Close[0] > Close[1]))
						direction = 1;
					else if (lowerCloseBreak && Close[0] < Open[0] && (CurrentBar < 1 || Close[0] < Close[1]))
						direction = -1;
					breakNow = direction != 0;
					break;

				default:
					int currentDirection = upperCloseBreak ? 1 : lowerCloseBreak ? -1 : 0;
					if (CurrentBar != lastBreakCloseBar)
					{
						if (currentDirection != 0 && currentDirection == lastBreakDirection)
							consecutiveBreakCloses++;
						else if (currentDirection != 0)
							consecutiveBreakCloses = 1;
						else
							consecutiveBreakCloses = 0;
						lastBreakDirection = currentDirection;
						lastBreakCloseBar = CurrentBar;
					}
					if (currentDirection != 0
						&& consecutiveBreakCloses >= Math.Max(1, RequiredConfirmationCloses))
					{
						direction = currentDirection;
						breakNow = true;
					}
					break;
			}

			if (AllowWickPenetration
				&& !upperCloseBreak
				&& !lowerCloseBreak
				&& BreakoutMode == OrcaChannelBreakoutMode.IntrabarPenetration)
			{
				breakNow = false;
				direction = 0;
			}

			if (breakNow)
			{
				activeChannel.Lifecycle = OrcaChannelLifecycleState.Broken;
				activeChannel.BreakBar = CurrentBar;
				activeChannel.BreakDirection = direction;
				AddSignalMarker(
					CurrentBar,
					Time[0],
					direction > 0 ? upper : lower,
					direction > 0,
					"Channel Break",
					activeChannel.ChannelId);
				FireConfiguredAlert(
					AlertOnBreak,
					"break",
					string.Format(CultureInfo.InvariantCulture, "{0} channel break", Name));
				QueueEvaluationExport("Broken", activeChannel);
			}
		}

		private void UpdateRetestState()
		{
			if (activeChannel == null || activeChannel.BreakBar < 0 || CurrentBar <= activeChannel.BreakBar)
				return;

			double tolerance = Math.Max(TickSize, RetestTolerance * SafeNormalizationUnit());
			double boundary = activeChannel.BreakDirection > 0
				? activeChannel.UpperAt(CurrentBar)
				: activeChannel.LowerAt(CurrentBar);
			bool retest = activeChannel.BreakDirection > 0
				? Low[0] <= boundary + tolerance && Close[0] >= boundary
				: High[0] >= boundary - tolerance && Close[0] <= boundary;
			if (!retest || activeChannel.Lifecycle == OrcaChannelLifecycleState.Retest)
				return;

			activeChannel.Lifecycle = OrcaChannelLifecycleState.Retest;
			AddSignalMarker(
				CurrentBar,
				Time[0],
				boundary,
				activeChannel.BreakDirection > 0,
				"Channel Retest",
				activeChannel.ChannelId);
			FireConfiguredAlert(
				AlertOnRetest,
				"retest",
				string.Format(CultureInfo.InvariantCulture, "{0} channel retest", Name));
			QueueEvaluationExport("Retest", activeChannel);
		}

		private bool AreSimilar(ChannelCandidate first, ChannelCandidate second)
		{
			if (first == null || second == null)
				return false;
			double unit = SafeNormalizationUnit();
			double slopeDistance = Math.Abs(first.CenterSlope - second.CenterSlope) / unit;
			double upperDistance = Math.Abs(first.UpperAt(CurrentBar) - second.UpperAt(CurrentBar)) / unit;
			double lowerDistance = Math.Abs(first.LowerAt(CurrentBar) - second.LowerAt(CurrentBar)) / unit;
			int overlapStart = Math.Max(first.StartBar, second.StartBar);
			int overlapEnd = Math.Min(first.EndBar, second.EndBar);
			int minimumAge = Math.Max(1, Math.Min(first.AgeBars, second.AgeBars));
			double overlap = Math.Max(0, overlapEnd - overlapStart + 1) / (double)minimumAge;
			double widthDistance = Math.Abs(first.ChannelWidth - second.ChannelWidth)
				/ Math.Max(unit, Math.Max(first.ChannelWidth, second.ChannelWidth));
			return slopeDistance <= 0.08
				&& upperDistance <= 0.35
				&& lowerDistance <= 0.35
				&& widthDistance <= 0.30
				&& overlap >= 0.35;
		}
		#endregion

		#region Trade locations and signals
		private void ResetSignalState(string channelId)
		{
			signalChannelId = channelId ?? string.Empty;
			longZoneArmed = true;
			shortZoneArmed = true;
			longZoneEntryBar = -1;
			shortZoneEntryBar = -1;
		}

		private void UpdateTradeLocationSignals()
		{
			if (!EnableTradeLocations
				|| activeChannel == null
				|| activeChannel.Lifecycle == OrcaChannelLifecycleState.Candidate
				|| activeChannel.Lifecycle == OrcaChannelLifecycleState.Broken
				|| activeChannel.Lifecycle == OrcaChannelLifecycleState.Retest)
			{
				return;
			}

			if (!string.Equals(signalChannelId, activeChannel.ChannelId, StringComparison.Ordinal))
				ResetSignalState(activeChannel.ChannelId);

			double zoneWidth = GetZoneWidth(activeChannel);
			if (!IsFinite(zoneWidth) || zoneWidth <= 0)
				return;

			double upper = activeChannel.UpperAt(CurrentBar);
			double lower = activeChannel.LowerAt(CurrentBar);
			bool allowLong = (activeChannel.Classification == OrcaChannelClassification.Uptrend && EnableTrendLocations)
				|| (activeChannel.Classification == OrcaChannelClassification.Range && EnableRangeLocations)
				|| (activeChannel.Classification == OrcaChannelClassification.Downtrend && EnableCountertrendLocations);
			bool allowShort = (activeChannel.Classification == OrcaChannelClassification.Downtrend && EnableTrendLocations)
				|| (activeChannel.Classification == OrcaChannelClassification.Range && EnableRangeLocations)
				|| (activeChannel.Classification == OrcaChannelClassification.Uptrend && EnableCountertrendLocations);
			bool inLongZone = Low[0] <= lower + zoneWidth && High[0] >= lower - TickSize;
			bool inShortZone = High[0] >= upper - zoneWidth && Low[0] <= upper + TickSize;
			double rearmDistance = Math.Max(TickSize, SignalRearmDistance * SafeNormalizationUnit());

			if (allowLong)
			{
				if (inLongZone && longZoneArmed)
				{
					longZoneArmed = false;
					longZoneEntryBar = CurrentBar;
					if (EnableLocationSignals)
					{
						string label = activeChannel.Classification == OrcaChannelClassification.Range
							? "Range Long Location"
							: "Long Location";
						AddSignalMarker(CurrentBar, Time[0], lower + zoneWidth * 0.5, true, label, activeChannel.ChannelId);
						FireConfiguredAlert(AlertOnLocation, "long-location", label);
						QueueEvaluationExport(label, activeChannel);
					}
				}
				else if (!inLongZone && Close[0] > lower + zoneWidth + rearmDistance)
				{
					longZoneArmed = true;
					longZoneEntryBar = -1;
				}

				if (EnableRejectionSignals
					&& longZoneEntryBar >= 0
					&& IsLongRejection(lower, zoneWidth))
				{
					string label = activeChannel.Classification == OrcaChannelClassification.Range
						? "Range Long Rejection"
						: "Long Rejection";
					AddSignalMarker(CurrentBar, Time[0], Close[0], true, label, activeChannel.ChannelId);
					FireConfiguredAlert(AlertOnRejection, "long-rejection", label);
					QueueEvaluationExport(label, activeChannel);
					longZoneEntryBar = -1;
				}
			}

			if (allowShort)
			{
				if (inShortZone && shortZoneArmed)
				{
					shortZoneArmed = false;
					shortZoneEntryBar = CurrentBar;
					if (EnableLocationSignals)
					{
						string label = activeChannel.Classification == OrcaChannelClassification.Range
							? "Range Short Location"
							: "Short Location";
						AddSignalMarker(CurrentBar, Time[0], upper - zoneWidth * 0.5, false, label, activeChannel.ChannelId);
						FireConfiguredAlert(AlertOnLocation, "short-location", label);
						QueueEvaluationExport(label, activeChannel);
					}
				}
				else if (!inShortZone && Close[0] < upper - zoneWidth - rearmDistance)
				{
					shortZoneArmed = true;
					shortZoneEntryBar = -1;
				}

				if (EnableRejectionSignals
					&& shortZoneEntryBar >= 0
					&& IsShortRejection(upper, zoneWidth))
				{
					string label = activeChannel.Classification == OrcaChannelClassification.Range
						? "Range Short Rejection"
						: "Short Rejection";
					AddSignalMarker(CurrentBar, Time[0], Close[0], false, label, activeChannel.ChannelId);
					FireConfiguredAlert(AlertOnRejection, "short-rejection", label);
					QueueEvaluationExport(label, activeChannel);
					shortZoneEntryBar = -1;
				}
			}
		}

		private bool IsLongRejection(double lower, double zoneWidth)
		{
			double barRange = Math.Max(TickSize, High[0] - Low[0]);
			bool closeAway = (Close[0] - Low[0]) / barRange * 100.0 >= RejectionClosePercent;
			bool directional = !RequireDirectionalRejectionBar || Close[0] > Open[0];
			if (!directional)
				return false;

			switch (RejectionConfirmationMode)
			{
				case OrcaChannelRejectionMode.TwoBar:
					return CurrentBar > longZoneEntryBar
						&& CurrentBar >= 1
						&& Close[0] > Close[1]
						&& Close[0] > lower + zoneWidth
						&& closeAway;
				case OrcaChannelRejectionMode.CloseBackInside:
					return Low[0] <= lower && Close[0] > lower + TickSize;
				default:
					return CurrentBar >= longZoneEntryBar
						&& Low[0] <= lower + zoneWidth
						&& Close[0] > lower + zoneWidth
						&& closeAway;
			}
		}

		private bool IsShortRejection(double upper, double zoneWidth)
		{
			double barRange = Math.Max(TickSize, High[0] - Low[0]);
			bool closeAway = (High[0] - Close[0]) / barRange * 100.0 >= RejectionClosePercent;
			bool directional = !RequireDirectionalRejectionBar || Close[0] < Open[0];
			if (!directional)
				return false;

			switch (RejectionConfirmationMode)
			{
				case OrcaChannelRejectionMode.TwoBar:
					return CurrentBar > shortZoneEntryBar
						&& CurrentBar >= 1
						&& Close[0] < Close[1]
						&& Close[0] < upper - zoneWidth
						&& closeAway;
				case OrcaChannelRejectionMode.CloseBackInside:
					return High[0] >= upper && Close[0] < upper - TickSize;
				default:
					return CurrentBar >= shortZoneEntryBar
						&& High[0] >= upper - zoneWidth
						&& Close[0] < upper - zoneWidth
						&& closeAway;
			}
		}

		private double GetZoneWidth(ChannelCandidate candidate)
		{
			if (candidate == null || candidate.ChannelWidth <= 0)
				return 0;
			double width;
			switch (ZoneCalculationMode)
			{
				case OrcaChannelZoneCalculationMode.Ticks:
					width = ZoneWidthTicks * TickSize;
					break;
				case OrcaChannelZoneCalculationMode.NormalizedUnits:
					width = ZoneWidthNormalizedUnits * SafeNormalizationUnit();
					break;
				case OrcaChannelZoneCalculationMode.RangeBarMultiple:
					double rangeUnit = BarsPeriod != null
						&& BarsPeriod.BarsPeriodType == BarsPeriodType.Range
						&& BarsPeriod.Value > 0
							? BarsPeriod.Value * TickSize
							: SafeNormalizationUnit();
					width = RangeBarZoneMultiple * rangeUnit;
					break;
				default:
					width = candidate.ChannelWidth * ZoneWidthPercentage / 100.0;
					break;
			}
			return Clamp(width, TickSize, candidate.ChannelWidth * 0.45);
		}

		private double GetPreferredPrice(ChannelCandidate candidate, bool isLong)
		{
			if (candidate == null)
				return double.NaN;
			double boundary = isLong ? candidate.CurrentLower : candidate.CurrentUpper;
			double width = GetZoneWidth(candidate);
			if (PreferredPriceMode == OrcaChannelPreferredPriceMode.ExactBoundary)
				return boundary;
			if (PreferredPriceMode == OrcaChannelPreferredPriceMode.ZoneNearEdge)
				return isLong ? boundary + width : boundary - width;
			return isLong ? boundary + width * 0.5 : boundary - width * 0.5;
		}

		private void AddSignalMarker(
			int barIndex,
			DateTime time,
			double price,
			bool isLong,
			string label,
			string channelId)
		{
			if (!IsFinite(price))
				return;
			for (int i = signalMarkers.Count - 1; i >= 0; i--)
			{
				SignalMarker existing = signalMarkers[i];
				if (existing.BarIndex == barIndex
					&& existing.Label == label
					&& existing.ChannelId == channelId)
					return;
			}
			signalMarkers.Add(new SignalMarker
			{
				BarIndex = barIndex,
				Time = time,
				Price = price,
				IsLong = isLong,
				Label = label,
				ChannelId = channelId
			});
			while (signalMarkers.Count > 300)
				signalMarkers.RemoveAt(0);
		}

		private void FireConfiguredAlert(bool enabled, string eventKey, string message)
		{
			if (!enabled || State != State.Realtime)
				return;
			try
			{
				string sound = string.IsNullOrWhiteSpace(AlertSound)
					? string.Empty
					: Path.Combine(NinjaTrader.Core.Globals.InstallDir, "sounds", AlertSound);
				Alert(
					string.Format(CultureInfo.InvariantCulture, "OAC-{0}-{1}-{2}", eventKey, CurrentBar, activeChannel == null ? "none" : activeChannel.ChannelId),
					Priority.Medium,
					message,
					sound,
					0,
					WpfBrushes.Black,
					WpfBrushes.White);
			}
			catch (Exception exception)
			{
				if (EnableDiagnostics)
					Print("ORCA_CHANNEL alert failed: " + exception.Message);
			}
		}
		#endregion
		#region Snapshot publication and diagnostics
		private void PublishRenderSnapshot()
		{
			List<ChannelRenderItem> renderItems = new List<ChannelRenderItem>();
			if (ShowHistoricalChannels)
			{
				for (int i = 0; i < archivedChannels.Count; i++)
				{
					renderItems.Add(new ChannelRenderItem
					{
						Candidate = archivedChannels[i].Clone(),
						IsPrimary = false,
						IsHistorical = true,
						MethodIndex = MethodIndex(archivedChannels[i].Method)
					});
				}
			}

			if (DisplayMode == OrcaChannelDisplayMode.CompareMethods)
			{
				foreach (OrcaChannelDetectionMode method in new[]
				{
					OrcaChannelDetectionMode.SwingPivot,
					OrcaChannelDetectionMode.LinearRegression,
					OrcaChannelDetectionMode.RepeatedTouches
				})
				{
					ChannelCandidate best = lastCandidates
						.Where(c => c.Method == method)
						.OrderByDescending(c => c.QualityScore)
						.FirstOrDefault();
					if (best == null
						|| (!ShowProvisionalChannels && best.Lifecycle == OrcaChannelLifecycleState.Candidate))
						continue;
					renderItems.Add(new ChannelRenderItem
					{
						Candidate = best.Clone(),
						IsPrimary = activeChannel != null && AreSimilar(activeChannel, best),
						IsHistorical = false,
						MethodIndex = MethodIndex(method)
					});
				}
			}
			else
			{
				if (activeChannel != null
					&& (ShowProvisionalChannels || activeChannel.Lifecycle != OrcaChannelLifecycleState.Candidate))
				{
					renderItems.Add(new ChannelRenderItem
					{
						Candidate = activeChannel.Clone(),
						IsPrimary = true,
						IsHistorical = false,
						MethodIndex = MethodIndex(activeChannel.Method)
					});
				}
				for (int i = 0; i < secondaryChannels.Count; i++)
				{
					ChannelCandidate secondary = secondaryChannels[i];
					if (!ShowProvisionalChannels && secondary.Lifecycle == OrcaChannelLifecycleState.Candidate)
						continue;
					renderItems.Add(new ChannelRenderItem
					{
						Candidate = secondary.Clone(),
						IsPrimary = false,
						IsHistorical = false,
						MethodIndex = MethodIndex(secondary.Method)
					});
				}
			}

			int visibleStart = Math.Max(0, CurrentBar - Math.Max(MaximumLookback * 2, 500));
			SignalMarker[] markers = signalMarkers
				.Where(s => s.BarIndex >= visibleStart)
				.Select(s => new SignalMarker
				{
					BarIndex = s.BarIndex,
					Time = s.Time,
					Price = s.Price,
					IsLong = s.IsLong,
					Label = s.Label,
					ChannelId = s.ChannelId
				})
				.ToArray();

			double longPrice = double.NaN;
			double shortPrice = double.NaN;
			if (ShowPreferredPrice && activeChannel != null)
			{
				if (activeChannel.Classification == OrcaChannelClassification.Uptrend
					|| activeChannel.Classification == OrcaChannelClassification.Range)
					longPrice = GetPreferredPrice(activeChannel, true);
				if (activeChannel.Classification == OrcaChannelClassification.Downtrend
					|| activeChannel.Classification == OrcaChannelClassification.Range)
					shortPrice = GetPreferredPrice(activeChannel, false);
			}

			renderSnapshot = new RenderStateSnapshot(
				renderItems.ToArray(),
				markers,
				BuildPrimaryLabel(activeChannel),
				BuildDiagnosticsText(activeChannel),
				longPrice,
				shortPrice);
		}

		private string BuildPrimaryLabel(ChannelCandidate candidate)
		{
			if (candidate == null)
				return "No confirmed channel";
			string classification = candidate.Classification == OrcaChannelClassification.Range
				? "Range Channel"
				: candidate.Classification == OrcaChannelClassification.Uptrend
					? "Rising Channel"
					: candidate.Classification == OrcaChannelClassification.Downtrend
						? "Falling Channel"
						: candidate.Classification == OrcaChannelClassification.Transition
							? "Transitional Structure"
							: "Unclassified Structure";
			StringBuilder builder = new StringBuilder(classification);
			if (ShowScore)
				builder.AppendFormat(CultureInfo.InvariantCulture, " | Quality {0:F0}", candidate.QualityScore);
			builder.AppendFormat(CultureInfo.InvariantCulture, " | {0}U / {1}L touches", candidate.UpperTouches, candidate.LowerTouches);
			if (ShowMethod)
				builder.Append(" | ").Append(MethodName(candidate.Method));
			if (ShowLifecycleState)
				builder.Append(" | ").Append(candidate.Lifecycle);
			return builder.ToString();
		}

		private string BuildDiagnosticsText(ChannelCandidate candidate)
		{
			if (!ShowDiagnosticsPanel)
				return string.Empty;
			if (candidate == null)
				return "Orca Adaptive Channels\nNo candidate above the minimum score";
			return string.Format(
				CultureInfo.InvariantCulture,
				"Method {0} | State {1}\nScore {2:F1} | Age {3} | Touches {4}U/{5}L\nContain {6:F1}% | Slope {7:F3} | Width {8:F2}N\nFit {9:F3}N | Efficiency {10:F2}\nPivot {11:F2}ms | Candidate {12:F2}ms | Score {13:F2}ms | Render {14:F2}ms",
				MethodName(candidate.Method),
				candidate.Lifecycle,
				candidate.QualityScore,
				candidate.AgeBars,
				candidate.UpperTouches,
				candidate.LowerTouches,
				candidate.ContainmentPercent,
				candidate.NormalizedSlope,
				candidate.NormalizedWidth,
				candidate.AverageFitError / SafeNormalizationUnit(),
				candidate.DirectionalEfficiency,
				lastPivotMilliseconds,
				lastCandidateMilliseconds,
				lastScoringMilliseconds,
				lastRenderMilliseconds);
		}

		private void LogCandidateSummary(List<ChannelCandidate> ranked)
		{
			int limit = Math.Min(6, ranked.Count);
			for (int i = 0; i < limit; i++)
			{
				ChannelCandidate candidate = ranked[i];
				Print(string.Format(
					CultureInfo.InvariantCulture,
					"ORCA_CHANNEL candidate method={0} score={1:F1} class={2} state={3} age={4} touches={5}/{6} contain={7:F1} slopeN={8:F3} widthN={9:F2}",
					candidate.Method,
					candidate.QualityScore,
					candidate.Classification,
					candidate.Lifecycle,
					candidate.AgeBars,
					candidate.UpperTouches,
					candidate.LowerTouches,
					candidate.ContainmentPercent,
					candidate.NormalizedSlope,
					candidate.NormalizedWidth));
			}
		}

		private void QueueEvaluationExport(string eventType, ChannelCandidate candidate)
		{
			if (!EnableEvaluationExport || candidate == null)
				return;

			string path = ResolveExportPath();
			string instrument = Instrument == null ? string.Empty : Instrument.FullName;
			string barType = BarsPeriod == null ? string.Empty : BarsPeriod.BarsPeriodType.ToString();
			int barValue = BarsPeriod == null ? 0 : BarsPeriod.Value;
			DateTime eventTime = Time[0];
			string line = string.Join(",",
				Csv(eventTime.ToString("O", CultureInfo.InvariantCulture)),
				Csv(instrument),
				Csv(barType),
				barValue.ToString(CultureInfo.InvariantCulture),
				Csv(eventType),
				Csv(candidate.Method.ToString()),
				Csv(candidate.ChannelId),
				candidate.StartBar.ToString(CultureInfo.InvariantCulture),
				candidate.ConfirmationBar.ToString(CultureInfo.InvariantCulture),
				candidate.BreakBar.ToString(CultureInfo.InvariantCulture),
				Csv(candidate.Classification.ToString()),
				candidate.QualityScore.ToString("F4", CultureInfo.InvariantCulture),
				candidate.TotalTouches.ToString(CultureInfo.InvariantCulture),
				candidate.AgeBars.ToString(CultureInfo.InvariantCulture),
				candidate.NormalizedSlope.ToString("F6", CultureInfo.InvariantCulture),
				candidate.NormalizedWidth.ToString("F6", CultureInfo.InvariantCulture),
				candidate.ContainmentPercent.ToString("F4", CultureInfo.InvariantCulture));
			ThreadPool.QueueUserWorkItem(delegate
			{
				try
				{
					lock (ExportSync)
					{
						string directory = Path.GetDirectoryName(path);
						if (!string.IsNullOrWhiteSpace(directory))
							Directory.CreateDirectory(directory);
						bool writeHeader = !File.Exists(path);
						using (StreamWriter writer = new StreamWriter(path, true, Encoding.UTF8))
						{
							if (writeHeader)
								writer.WriteLine("Timestamp,Instrument,BarType,BarPeriod,Event,DetectionMethod,ChannelId,StartBar,ConfirmationBar,BreakBar,Classification,Score,TouchCount,DurationBars,NormalizedSlope,NormalizedWidth,ContainmentPercent");
							writer.WriteLine(line);
						}
					}
				}
				catch (Exception exception)
				{
					if (EnableDiagnostics)
						Print("ORCA_CHANNEL export failed: " + exception.Message);
				}
			});
		}

		private string ResolveExportPath()
		{
			if (!string.IsNullOrWhiteSpace(ExportPath))
			{
				if (Path.HasExtension(ExportPath))
					return ExportPath;
				return Path.Combine(ExportPath, "orca-adaptive-channels.csv");
			}
			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"NinjaTrader 8",
				"orca-adaptive-channels",
				"orca-adaptive-channels.csv");
		}

		private static string Csv(string value)
		{
			string safe = value ?? string.Empty;
			return "\"" + safe.Replace("\"", "\"\"") + "\"";
		}
		#endregion

		#region Math and data helpers
		private bool TryLinearRegression(
			int startBar,
			int endBar,
			OrcaChannelPriceSource source,
			out double slope,
			out double intercept)
		{
			slope = 0;
			intercept = 0;
			int count = endBar - startBar + 1;
			if (count < 2)
				return false;

			double sumX = 0;
			double sumY = 0;
			double sumXY = 0;
			double sumXX = 0;
			for (int bar = startBar; bar <= endBar; bar++)
			{
				double x = bar;
				double y = GetPriceAtBar(bar, source);
				if (!IsFinite(y))
					return false;
				sumX += x;
				sumY += y;
				sumXY += x * y;
				sumXX += x * x;
			}
			double denominator = count * sumXX - sumX * sumX;
			if (Math.Abs(denominator) < 1e-12)
				return false;
			slope = (count * sumXY - sumX * sumY) / denominator;
			intercept = (sumY - slope * sumX) / count;
			return IsFinite(slope) && IsFinite(intercept);
		}

		private bool TryGetRegressionOffsets(
			List<double> residuals,
			out double upperOffset,
			out double lowerOffset)
		{
			upperOffset = 0;
			lowerOffset = 0;
			if (residuals == null || residuals.Count < 5)
				return false;

			List<double> working = residuals.Where(IsFinite).OrderBy(value => value).ToList();
			if (working.Count < 5)
				return false;

			if (OutlierHandling == OrcaChannelOutlierHandling.Winsorize)
			{
				double lowFence = Percentile(working, 5);
				double highFence = Percentile(working, 95);
				for (int i = 0; i < working.Count; i++)
					working[i] = Clamp(working[i], lowFence, highFence);
				working.Sort();
			}
			else if (OutlierHandling == OrcaChannelOutlierHandling.TrimExtremeResiduals && working.Count >= 20)
			{
				int trim = Math.Max(1, working.Count / 20);
				working = working.Skip(trim).Take(working.Count - trim * 2).ToList();
			}

			switch (BoundaryModel)
			{
				case OrcaChannelRegressionBoundaryModel.StandardDeviations:
					double mean = working.Average();
					double variance = working.Sum(value => (value - mean) * (value - mean)) / Math.Max(1, working.Count - 1);
					double standardDeviation = Math.Sqrt(Math.Max(0, variance));
					upperOffset = mean + StandardDeviationMultiplier * standardDeviation;
					lowerOffset = mean - StandardDeviationMultiplier * standardDeviation;
					break;

				case OrcaChannelRegressionBoundaryModel.MaximumResidualEnvelope:
					upperOffset = working[working.Count - 1];
					lowerOffset = working[0];
					break;

				case OrcaChannelRegressionBoundaryModel.ResidualPercentile:
					upperOffset = Percentile(working, ResidualPercentile);
					lowerOffset = Percentile(working, 100 - ResidualPercentile);
					break;

				default:
					List<double> positive = working.Where(value => value >= 0).ToList();
					List<double> negative = working.Where(value => value <= 0).ToList();
					upperOffset = positive.Count > 0
						? Percentile(positive, ResidualPercentile)
						: Percentile(working, ResidualPercentile);
					lowerOffset = negative.Count > 0
						? Percentile(negative, 100 - ResidualPercentile)
						: Percentile(working, 100 - ResidualPercentile);
					break;
			}
			return IsFinite(upperOffset) && IsFinite(lowerOffset) && upperOffset > lowerOffset;
		}

		private static double Percentile(List<double> sorted, double percentile)
		{
			if (sorted == null || sorted.Count == 0)
				return double.NaN;
			double position = Clamp(percentile, 0, 100) / 100.0 * (sorted.Count - 1);
			int lower = (int)Math.Floor(position);
			int upper = (int)Math.Ceiling(position);
			if (lower == upper)
				return sorted[lower];
			double fraction = position - lower;
			return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
		}

		private double GetPriceAtBar(int absoluteBar, OrcaChannelPriceSource source)
		{
			int barsAgo = CurrentBar - absoluteBar;
			if (barsAgo < 0 || barsAgo > CurrentBar)
				return double.NaN;
			switch (source)
			{
				case OrcaChannelPriceSource.MedianPrice:
					return (High[barsAgo] + Low[barsAgo]) * 0.5;
				case OrcaChannelPriceSource.TypicalPrice:
					return (High[barsAgo] + Low[barsAgo] + Close[barsAgo]) / 3.0;
				case OrcaChannelPriceSource.OhlcAverage:
					return (Open[barsAgo] + High[barsAgo] + Low[barsAgo] + Close[barsAgo]) * 0.25;
				default:
					return Close[barsAgo];
			}
		}

		private double GetHighAtBar(int absoluteBar)
		{
			int barsAgo = CurrentBar - absoluteBar;
			return barsAgo >= 0 && barsAgo <= CurrentBar ? High[barsAgo] : double.NaN;
		}

		private double GetLowAtBar(int absoluteBar)
		{
			int barsAgo = CurrentBar - absoluteBar;
			return barsAgo >= 0 && barsAgo <= CurrentBar ? Low[barsAgo] : double.NaN;
		}

		private double GetCloseAtBar(int absoluteBar)
		{
			int barsAgo = CurrentBar - absoluteBar;
			return barsAgo >= 0 && barsAgo <= CurrentBar ? Close[barsAgo] : double.NaN;
		}

		private static double Slope(PivotPoint first, PivotPoint second)
		{
			int distance = second.BarIndex - first.BarIndex;
			return distance == 0 ? 0 : (second.Price - first.Price) / distance;
		}

		private static double AverageIntercept(PivotPoint first, PivotPoint second, double slope)
		{
			return (first.Price - slope * first.BarIndex
				+ second.Price - slope * second.BarIndex) * 0.5;
		}

		private bool SlopesAreParallel(double first, double second)
		{
			return Math.Abs(first - second) / SafeNormalizationUnit() <= ParallelismTolerance;
		}

		private double CalculateParallelismScore(double first, double second)
		{
			double normalizedDifference = Math.Abs(first - second) / SafeNormalizationUnit();
			return Clamp01(1.0 - normalizedDifference / Math.Max(0.001, ParallelismTolerance * 2));
		}

		private static bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		private static double Clamp01(double value)
		{
			return Clamp(value, 0, 1);
		}

		private static double Clamp(double value, double minimum, double maximum)
		{
			return value < minimum ? minimum : value > maximum ? maximum : value;
		}

		private int MethodIndex(OrcaChannelDetectionMode method)
		{
			if (method == OrcaChannelDetectionMode.SwingPivot)
				return 0;
			if (method == OrcaChannelDetectionMode.LinearRegression)
				return 1;
			return 2;
		}

		private string MethodName(OrcaChannelDetectionMode method)
		{
			if (method == OrcaChannelDetectionMode.SwingPivot)
				return "Swing";
			if (method == OrcaChannelDetectionMode.LinearRegression)
				return "Regression";
			if (method == OrcaChannelDetectionMode.RepeatedTouches)
				return "Touches";
			return "Hybrid";
		}

		private long TimingStart()
		{
			return PerformanceTiming || EnableDiagnostics ? Stopwatch.GetTimestamp() : 0;
		}

		private double TimingElapsedMilliseconds(long start)
		{
			if (start == 0)
				return 0;
			return (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
		}
		#endregion
		#region Rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (chartControl == null
				|| chartScale == null
				|| ChartBars == null
				|| ChartPanel == null
				|| RenderTarget == null)
			{
				return;
			}

			RenderStateSnapshot snapshot = renderSnapshot;
			if (snapshot == null)
				return;
			EnsureDxResources();
			if (!dxValid)
				return;

			long renderStart = TimingStart();
			AntialiasMode previousAntialias = RenderTarget.AntialiasMode;
			try
			{
				RenderTarget.AntialiasMode = AntialiasMode.Aliased;
				for (int i = 0; i < snapshot.Channels.Length; i++)
					RenderChannel(snapshot.Channels[i], chartControl, chartScale);
				RenderSignals(snapshot.Signals, chartControl, chartScale);
				if (ShowPreferredPrice)
				{
					RenderPreferredPrice(snapshot.PreferredLongPrice, true, chartControl, chartScale);
					RenderPreferredPrice(snapshot.PreferredShortPrice, false, chartControl, chartScale);
				}
				if (ShowLabels && !string.IsNullOrWhiteSpace(snapshot.PrimaryLabel))
					RenderPrimaryLabel(snapshot.PrimaryLabel, chartControl);
				if (ShowDiagnosticsPanel && !string.IsNullOrWhiteSpace(snapshot.DiagnosticsText))
					RenderDiagnosticsPanel(snapshot.DiagnosticsText, chartControl);
			}
			catch (Exception exception)
			{
				if (EnableDiagnostics)
					Print("ORCA_CHANNEL render skipped safely: " + exception.Message);
			}
			finally
			{
				RenderTarget.AntialiasMode = previousAntialias;
				lastRenderMilliseconds = TimingElapsedMilliseconds(renderStart);
			}
		}

		private void RenderChannel(ChannelRenderItem item, ChartControl chartControl, ChartScale chartScale)
		{
			if (item == null || item.Candidate == null)
				return;
			ChannelCandidate candidate = item.Candidate;
			int chartFrom = ChartBars.FromIndex;
			int chartTo = ChartBars.ToIndex;
			int startBar = Math.Max(candidate.StartBar, chartFrom);
			int logicalEnd = candidate.EndBar;
			if (candidate.BreakBar >= 0 && !ExtendBrokenChannels)
				logicalEnd = candidate.BreakBar;
			int endBar = Math.Min(Math.Max(startBar, logicalEnd), chartTo);
			if (endBar < chartFrom || startBar > chartTo)
				return;

			float startX = chartControl.GetXByBarIndex(ChartBars, startBar);
			float endX = chartControl.GetXByBarIndex(ChartBars, endBar);
			int projectedBars = item.IsHistorical || !ShowRightSideProjection ? 0 : Math.Max(0, ProjectionBars);
			float pixelsPerBar = 0;
			if (endBar > chartFrom)
			{
				float previousX = chartControl.GetXByBarIndex(ChartBars, endBar - 1);
				pixelsPerBar = Math.Max(1f, endX - previousX);
			}
			else
			{
				pixelsPerBar = Math.Max(1f, (float)chartControl.BarWidth + 1f);
			}
			float projectedX = Math.Min((float)chartControl.CanvasRight, endX + pixelsPerBar * projectedBars);
			int projectedBarIndex = logicalEnd + projectedBars;

			double upperStartPrice = candidate.UpperAt(startBar);
			double lowerStartPrice = candidate.LowerAt(startBar);
			double upperEndPrice = candidate.UpperAt(projectedBarIndex);
			double lowerEndPrice = candidate.LowerAt(projectedBarIndex);
			if (!IsFinite(upperStartPrice)
				|| !IsFinite(lowerStartPrice)
				|| !IsFinite(upperEndPrice)
				|| !IsFinite(lowerEndPrice))
			{
				return;
			}

			float upperStartY = chartScale.GetYByValue(upperStartPrice);
			float lowerStartY = chartScale.GetYByValue(lowerStartPrice);
			float upperEndY = chartScale.GetYByValue(upperEndPrice);
			float lowerEndY = chartScale.GetYByValue(lowerEndPrice);
			DxSolidBrush upperBrush = GetChannelBrush(item, true);
			DxSolidBrush lowerBrush = GetChannelBrush(item, false);
			DxSolidBrush fillBrush = dxBrushes[BrushFill];
			float opacity = item.IsHistorical
				? HistoricalChannelOpacity / 100f
				: item.IsPrimary ? 1f : 0.58f;
			if (candidate.Lifecycle == OrcaChannelLifecycleState.Candidate)
				opacity *= 0.65f;

			if (ShowFill && item.IsPrimary && !item.IsHistorical && FillOpacity > 0)
			{
				float previousOpacity = fillBrush.Opacity;
				fillBrush.Opacity = FillOpacity / 100f;
				FillQuadrilateral(
					new Vector2(startX, upperStartY),
					new Vector2(projectedX, upperEndY),
					new Vector2(projectedX, lowerEndY),
					new Vector2(startX, lowerStartY),
					fillBrush);
				fillBrush.Opacity = previousOpacity;
			}

			if (ShowTradeZones && item.IsPrimary && !item.IsHistorical && EnableTradeLocations)
				RenderTradeZones(candidate, startX, projectedX, startBar, projectedBarIndex, chartScale);

			float previousUpperOpacity = upperBrush.Opacity;
			float previousLowerOpacity = lowerBrush.Opacity;
			upperBrush.Opacity = opacity * UpperLineOpacity / 100f;
			lowerBrush.Opacity = opacity * LowerLineOpacity / 100f;
			bool brokenStyle = candidate.Lifecycle == OrcaChannelLifecycleState.Broken
				|| candidate.Lifecycle == OrcaChannelLifecycleState.Retest
				|| candidate.Lifecycle == OrcaChannelLifecycleState.Archived;
			OrcaChannelDashStyle upperStyle = candidate.Lifecycle == OrcaChannelLifecycleState.Candidate
				? CandidateLineStyle
				: brokenStyle ? BrokenLineStyle : UpperLineStyle;
			OrcaChannelDashStyle lowerStyle = candidate.Lifecycle == OrcaChannelLifecycleState.Candidate
				? CandidateLineStyle
				: brokenStyle ? BrokenLineStyle : LowerLineStyle;
			float widthScale = item.IsPrimary ? 1f : 0.75f;
			if (ShowUpperBoundary)
				RenderTarget.DrawLine(
					new Vector2(startX, upperStartY),
					new Vector2(projectedX, upperEndY),
					upperBrush,
					Math.Max(1f, UpperLineWidth * widthScale),
					GetStroke(upperStyle));
			if (ShowLowerBoundary)
				RenderTarget.DrawLine(
					new Vector2(startX, lowerStartY),
					new Vector2(projectedX, lowerEndY),
					lowerBrush,
					Math.Max(1f, LowerLineWidth * widthScale),
					GetStroke(lowerStyle));
			if (ShowCenterline)
			{
				double centerStartPrice = candidate.CenterAt(startBar);
				double centerEndPrice = candidate.CenterAt(projectedBarIndex);
				if (IsFinite(centerStartPrice) && IsFinite(centerEndPrice))
				{
					DxSolidBrush centerBrush = dxBrushes[BrushCenter];
					float previousCenterOpacity = centerBrush.Opacity;
					centerBrush.Opacity = opacity * CenterlineOpacity / 100f;
					RenderTarget.DrawLine(
						new Vector2(startX, chartScale.GetYByValue(centerStartPrice)),
						new Vector2(projectedX, chartScale.GetYByValue(centerEndPrice)),
						centerBrush,
						Math.Max(1f, CenterlineWidth * widthScale),
						GetStroke(CenterlineStyle));
					centerBrush.Opacity = previousCenterOpacity;
				}
			}
			upperBrush.Opacity = previousUpperOpacity;
			lowerBrush.Opacity = previousLowerOpacity;

			if (ShowTouchMarkers)
				RenderTouches(candidate, chartControl, chartScale, opacity);
			if (DisplayMode == OrcaChannelDisplayMode.CompareMethods && dxSmallFormat != null)
			{
				string methodLabel = string.Format(
					CultureInfo.InvariantCulture,
					"{0} {1:F0}",
					MethodName(candidate.Method),
					candidate.QualityScore);
				RectangleF labelRect = new RectangleF(startX + 4, upperStartY - TextSize - 4, 130, TextSize + 6);
				RenderTarget.DrawText(methodLabel, dxSmallFormat, labelRect, upperBrush);
			}
		}

		private DxSolidBrush GetChannelBrush(ChannelRenderItem item, bool upper)
		{
			if (item.Candidate.Lifecycle == OrcaChannelLifecycleState.Broken
				|| item.Candidate.Lifecycle == OrcaChannelLifecycleState.Retest
				|| item.Candidate.Lifecycle == OrcaChannelLifecycleState.Archived)
				return dxBrushes[BrushBroken];
			if (item.Candidate.Lifecycle == OrcaChannelLifecycleState.Candidate)
				return dxBrushes[BrushCandidate];
			if (DisplayMode == OrcaChannelDisplayMode.CompareMethods)
				return dxBrushes[BrushSwing + Math.Max(0, Math.Min(2, item.MethodIndex))];
			return dxBrushes[upper ? BrushUpper : BrushLower];
		}

		private void RenderTradeZones(
			ChannelCandidate candidate,
			float startX,
			float endX,
			int startBar,
			int endBar,
			ChartScale chartScale)
		{
			double width = GetZoneWidth(candidate);
			if (!IsFinite(width) || width <= 0)
				return;
			bool allowLong = (candidate.Classification == OrcaChannelClassification.Uptrend && EnableTrendLocations)
				|| (candidate.Classification == OrcaChannelClassification.Range && EnableRangeLocations)
				|| (candidate.Classification == OrcaChannelClassification.Downtrend && EnableCountertrendLocations);
			bool allowShort = (candidate.Classification == OrcaChannelClassification.Downtrend && EnableTrendLocations)
				|| (candidate.Classification == OrcaChannelClassification.Range && EnableRangeLocations)
				|| (candidate.Classification == OrcaChannelClassification.Uptrend && EnableCountertrendLocations);

			if (allowLong)
			{
				DxSolidBrush brush = candidate.Classification == OrcaChannelClassification.Range
					? dxBrushes[BrushRange]
					: dxBrushes[BrushLong];
				FillPriceBand(
					startX,
					endX,
					candidate.LowerAt(startBar) + width,
					candidate.LowerAt(startBar),
					candidate.LowerAt(endBar) + width,
					candidate.LowerAt(endBar),
					chartScale,
					brush,
					ZoneOpacity / 100f);
			}
			if (allowShort)
			{
				DxSolidBrush brush = candidate.Classification == OrcaChannelClassification.Range
					? dxBrushes[BrushRange]
					: dxBrushes[BrushShort];
				FillPriceBand(
					startX,
					endX,
					candidate.UpperAt(startBar),
					candidate.UpperAt(startBar) - width,
					candidate.UpperAt(endBar),
					candidate.UpperAt(endBar) - width,
					chartScale,
					brush,
					ZoneOpacity / 100f);
			}
		}

		private void FillPriceBand(
			float startX,
			float endX,
			double startUpper,
			double startLower,
			double endUpper,
			double endLower,
			ChartScale chartScale,
			DxSolidBrush brush,
			float opacity)
		{
			if (!IsFinite(startUpper) || !IsFinite(startLower) || !IsFinite(endUpper) || !IsFinite(endLower))
				return;
			float previousOpacity = brush.Opacity;
			brush.Opacity = (float)Clamp(opacity, 0, 1);
			FillQuadrilateral(
				new Vector2(startX, chartScale.GetYByValue(startUpper)),
				new Vector2(endX, chartScale.GetYByValue(endUpper)),
				new Vector2(endX, chartScale.GetYByValue(endLower)),
				new Vector2(startX, chartScale.GetYByValue(startLower)),
				brush);
			brush.Opacity = previousOpacity;
		}

		private void FillQuadrilateral(Vector2 first, Vector2 second, Vector2 third, Vector2 fourth, DxSolidBrush brush)
		{
			using (PathGeometry geometry = new PathGeometry(RenderTarget.Factory))
			using (GeometrySink sink = geometry.Open())
			{
				sink.BeginFigure(first, FigureBegin.Filled);
				sink.AddLine(second);
				sink.AddLine(third);
				sink.AddLine(fourth);
				sink.EndFigure(FigureEnd.Closed);
				sink.Close();
				RenderTarget.FillGeometry(geometry, brush);
			}
		}

		private void RenderTouches(ChannelCandidate candidate, ChartControl chartControl, ChartScale chartScale, float opacity)
		{
			DxSolidBrush brush = dxBrushes[BrushText];
			float previousOpacity = brush.Opacity;
			brush.Opacity = opacity;
			for (int i = 0; i < candidate.Touches.Count; i++)
			{
				TouchPoint touch = candidate.Touches[i];
				if (touch.BarIndex < ChartBars.FromIndex || touch.BarIndex > ChartBars.ToIndex)
					continue;
				float x = chartControl.GetXByBarIndex(ChartBars, touch.BarIndex);
				float y = chartScale.GetYByValue(touch.Price);
				RenderTarget.DrawEllipse(new Ellipse(new Vector2(x, y), 3f, 3f), brush, 1f);
			}
			brush.Opacity = previousOpacity;
		}

		private void RenderSignals(SignalMarker[] markers, ChartControl chartControl, ChartScale chartScale)
		{
			if (markers == null)
				return;
			for (int i = 0; i < markers.Length; i++)
			{
				SignalMarker marker = markers[i];
				if (marker.BarIndex < ChartBars.FromIndex || marker.BarIndex > ChartBars.ToIndex)
					continue;
				float x = chartControl.GetXByBarIndex(ChartBars, marker.BarIndex);
				float y = chartScale.GetYByValue(marker.Price);
				DxSolidBrush brush = marker.IsLong ? dxBrushes[BrushLong] : dxBrushes[BrushShort];
				float direction = marker.IsLong ? 1f : -1f;
				RenderTarget.DrawLine(new Vector2(x - 4, y + direction * 5), new Vector2(x, y), brush, 2f);
				RenderTarget.DrawLine(new Vector2(x, y), new Vector2(x + 4, y + direction * 5), brush, 2f);
				if (ShowLabels && dxSmallFormat != null)
				{
					float labelY = marker.IsLong ? y + 6 : y - TextSize - 8;
					RenderTarget.DrawText(
						marker.Label,
						dxSmallFormat,
						new RectangleF(x + 5, labelY, 150, TextSize + 5),
						brush);
				}
			}
		}

		private void RenderPreferredPrice(double price, bool isLong, ChartControl chartControl, ChartScale chartScale)
		{
			if (!IsFinite(price) || dxSmallFormat == null)
				return;
			float y = chartScale.GetYByValue(price);
			float panelTop = ChartPanel.Y;
			float panelBottom = ChartPanel.Y + ChartPanel.H;
			if (y < panelTop - 10 || y > panelBottom + 10)
				return;
			DxSolidBrush brush = isLong ? dxBrushes[BrushLong] : dxBrushes[BrushShort];
			string prefix = isLong ? "Long " : "Short ";
			string text = prefix + FormatPrice(price);
			float width = 96f;
			float right = (float)chartControl.CanvasRight;
			RectangleF rectangle = new RectangleF(right - width, y - TextSize * 0.65f, width - 2, TextSize + 5);
			float previousOpacity = brush.Opacity;
			brush.Opacity = 0.82f;
			RenderTarget.FillRectangle(rectangle, brush);
			brush.Opacity = previousOpacity;
			RenderTarget.DrawText(text, dxSmallFormat, rectangle, dxBrushes[BrushText]);
		}

		private void RenderPrimaryLabel(string text, ChartControl chartControl)
		{
			if (dxLabelFormat == null)
				return;
			float left = ChartPanel.X + 8;
			float top = ChartPanel.Y + 8;
			float width = Math.Min(430f, Math.Max(180f, ChartPanel.W * 0.55f));
			float height = TextSize + 10;
			if (LabelPlacement == OrcaChannelLabelPlacement.UpperRight
				|| LabelPlacement == OrcaChannelLabelPlacement.LowerRight)
				left = ChartPanel.X + ChartPanel.W - width - 8;
			if (LabelPlacement == OrcaChannelLabelPlacement.LowerLeft
				|| LabelPlacement == OrcaChannelLabelPlacement.LowerRight)
				top = ChartPanel.Y + ChartPanel.H - height - 8;
			RectangleF rectangle = new RectangleF(left, top, width, height);
			DxSolidBrush panelBrush = dxBrushes[BrushPanel];
			float previousOpacity = panelBrush.Opacity;
			panelBrush.Opacity = 0.62f;
			RenderTarget.FillRectangle(rectangle, panelBrush);
			panelBrush.Opacity = previousOpacity;
			RenderTarget.DrawText(text, dxLabelFormat, rectangle, dxBrushes[BrushText]);
		}

		private void RenderDiagnosticsPanel(string text, ChartControl chartControl)
		{
			if (dxSmallFormat == null)
				return;
			float width = 330f;
			float height = 92f;
			RectangleF rectangle = new RectangleF(
				ChartPanel.X + ChartPanel.W - width - 8,
				ChartPanel.Y + ChartPanel.H - height - 8,
				width,
				height);
			DxSolidBrush panelBrush = dxBrushes[BrushPanel];
			float previousOpacity = panelBrush.Opacity;
			panelBrush.Opacity = 0.76f;
			RenderTarget.FillRectangle(rectangle, panelBrush);
			panelBrush.Opacity = previousOpacity;
			RenderTarget.DrawText(text, dxSmallFormat, rectangle, dxBrushes[BrushText]);
		}

		private string FormatPrice(double price)
		{
			return Instrument != null
				? Instrument.MasterInstrument.FormatPrice(price)
				: price.ToString("F2", CultureInfo.InvariantCulture);
		}
		#endregion

		#region SharpDX resources
		private void EnsureDxResources()
		{
			if (dxValid && dxResourceRenderTarget == RenderTarget.NativePointer)
				return;
			DisposeDx();
			if (RenderTarget == null)
				return;
			try
			{
				WpfBrush[] sourceBrushes =
				{
					UpperLineColor,
					LowerLineColor,
					CenterlineColor,
					FillColor,
					LongZoneColor,
					ShortZoneColor,
					RangeZoneColor,
					CandidateChannelColor,
					BrokenChannelColor,
					TextColor,
					SwingMethodColor,
					RegressionMethodColor,
					TouchMethodColor,
					WpfBrushes.Black
				};
				dxBrushes = new DxSolidBrush[BrushCount];
				for (int i = 0; i < sourceBrushes.Length; i++)
					dxBrushes[i] = new DxSolidBrush(RenderTarget, ToColor4(sourceBrushes[i]));
				dxBrushes[BrushText].Opacity = TextOpacity / 100f;

				dxStrokes = new StrokeStyle[4];
				dxStrokes[0] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Solid });
				dxStrokes[1] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dash });
				dxStrokes[2] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dot });
				dxStrokes[3] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.DashDot });
				dxLabelFormat = new DxTextFormat(
					NinjaTrader.Core.Globals.DirectWriteFactory,
					TextFontName,
					FontWeight.SemiBold,
					SharpDX.DirectWrite.FontStyle.Normal,
					TextSize)
				{
					TextAlignment = TextAlignment.Leading,
					ParagraphAlignment = ParagraphAlignment.Center
				};
				dxSmallFormat = new DxTextFormat(
					NinjaTrader.Core.Globals.DirectWriteFactory,
					TextFontName,
					FontWeight.Normal,
					SharpDX.DirectWrite.FontStyle.Normal,
					Math.Max(8, TextSize - 1))
				{
					TextAlignment = TextAlignment.Leading,
					ParagraphAlignment = ParagraphAlignment.Near,
					WordWrapping = WordWrapping.NoWrap
				};
				dxResourceRenderTarget = RenderTarget.NativePointer;
				dxValid = true;
			}
			catch (Exception exception)
			{
				dxValid = false;
				if (EnableDiagnostics)
					Print("ORCA_CHANNEL resource creation failed: " + exception.Message);
			}
		}

		private Color4 ToColor4(WpfBrush brush)
		{
			WpfSolidColorBrush solid = brush as WpfSolidColorBrush;
			System.Windows.Media.Color color = solid == null ? WpfColors.White : solid.Color;
			return new Color4(
				color.R / 255f,
				color.G / 255f,
				color.B / 255f,
				color.A / 255f);
		}

		private StrokeStyle GetStroke(OrcaChannelDashStyle style)
		{
			int index = Math.Max(0, Math.Min(3, (int)style));
			return dxStrokes != null && index < dxStrokes.Length ? dxStrokes[index] : null;
		}

		private void DisposeDx()
		{
			try
			{
				if (dxBrushes != null)
					for (int i = 0; i < dxBrushes.Length; i++)
						if (dxBrushes[i] != null)
							dxBrushes[i].Dispose();
				if (dxStrokes != null)
					for (int i = 0; i < dxStrokes.Length; i++)
						if (dxStrokes[i] != null)
							dxStrokes[i].Dispose();
				if (dxLabelFormat != null)
					dxLabelFormat.Dispose();
				if (dxSmallFormat != null)
					dxSmallFormat.Dispose();
			}
			catch
			{
			}
			dxBrushes = null;
			dxStrokes = null;
			dxLabelFormat = null;
			dxSmallFormat = null;
			dxResourceRenderTarget = IntPtr.Zero;
			dxValid = false;
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}
		#endregion
		#region Properties - General
		[NinjaScriptProperty]
		[Display(Name = "Detection Mode", Order = 1, GroupName = "01. General")]
		public OrcaChannelDetectionMode DetectionMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Display Mode", Order = 2, GroupName = "01. General")]
		public OrcaChannelDisplayMode DisplayMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Calculation Source", Order = 3, GroupName = "01. General")]
		public OrcaChannelPriceSource CalculationSource { get; set; }

		[NinjaScriptProperty]
		[Range(20, 1000)]
		[Display(Name = "Bars Required", Order = 4, GroupName = "01. General")]
		public int MinimumBarsRequired { get; set; }

		[NinjaScriptProperty]
		[Range(40, 2000)]
		[Display(Name = "Maximum Lookback", Order = 5, GroupName = "01. General")]
		public int MaximumLookback { get; set; }

		[NinjaScriptProperty]
		[Range(1, 3)]
		[Display(Name = "Maximum Active Channels", Order = 6, GroupName = "01. General")]
		public int MaximumActiveChannels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Historical Channels", Order = 7, GroupName = "01. General")]
		public bool ShowHistoricalChannels { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Maximum Historical Channels", Order = 8, GroupName = "01. General")]
		public int MaximumHistoricalChannels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Provisional Channels", Order = 9, GroupName = "01. General")]
		public bool ShowProvisionalChannels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Preset", Order = 10, GroupName = "01. General")]
		public OrcaChannelPreset Preset { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Automatic Chart-Type Normalization", Order = 11, GroupName = "01. General")]
		public bool AutomaticChartTypeNormalization { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1000000.0)]
		[Display(Name = "Manual Normalization Override", Description = "Price-unit normalization value. Zero uses automatic normalization.", Order = 12, GroupName = "01. General")]
		public double ManualNormalizationOverride { get; set; }

		[NinjaScriptProperty]
		[Range(5, 200)]
		[Display(Name = "Volatility Lookback", Order = 13, GroupName = "01. General")]
		public int VolatilityLookback { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Update Mode", Order = 14, GroupName = "01. General")]
		public OrcaChannelUpdateMode UpdateMode { get; set; }
		#endregion

		#region Properties - Pivot detection
		[NinjaScriptProperty]
		[Display(Name = "Pivot Method", Order = 1, GroupName = "02. Pivot Detection")]
		public OrcaChannelPivotMethod PivotMethod { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Pivot Strength", Order = 2, GroupName = "02. Pivot Detection")]
		public int PivotStrength { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000)]
		[Display(Name = "Minimum Reversal Ticks", Order = 3, GroupName = "02. Pivot Detection")]
		public int MinimumReversalTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 10.0)]
		[Display(Name = "Minimum Reversal Normalized Units", Order = 4, GroupName = "02. Pivot Detection")]
		public double MinimumReversalNormalizedUnits { get; set; }

		[NinjaScriptProperty]
		[Range(8, 300)]
		[Display(Name = "Maximum Stored Pivots", Order = 5, GroupName = "02. Pivot Detection")]
		public int MaximumStoredPivots { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Minimum Pivot Separation", Order = 6, GroupName = "02. Pivot Detection")]
		public int MinimumPivotSeparation { get; set; }
		#endregion

		#region Properties - Swing engine
		[NinjaScriptProperty]
		[Range(2, 10)]
		[Display(Name = "Minimum Upper Pivots", Order = 1, GroupName = "03. Swing-Pivot Engine")]
		public int MinimumUpperPivots { get; set; }

		[NinjaScriptProperty]
		[Range(2, 10)]
		[Display(Name = "Minimum Lower Pivots", Order = 2, GroupName = "03. Swing-Pivot Engine")]
		public int MinimumLowerPivots { get; set; }

		[NinjaScriptProperty]
		[Range(0.001, 2.0)]
		[Display(Name = "Parallelism Tolerance", Description = "Maximum normalized slope difference.", Order = 3, GroupName = "03. Swing-Pivot Engine")]
		public double ParallelismTolerance { get; set; }

		[NinjaScriptProperty]
		[Range(20, 2000)]
		[Display(Name = "Candidate Lookback", Order = 4, GroupName = "03. Swing-Pivot Engine")]
		public int SwingCandidateLookback { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 5.0)]
		[Display(Name = "Maximum Boundary Error", Description = "Maximum normalized fit error used by scoring.", Order = 5, GroupName = "03. Swing-Pivot Engine")]
		public double MaximumBoundaryError { get; set; }
		#endregion

		#region Properties - Regression engine
		[NinjaScriptProperty]
		[Range(10, 1000)]
		[Display(Name = "Minimum Regression Length", Order = 1, GroupName = "04. Regression Engine")]
		public int MinimumRegressionLength { get; set; }

		[NinjaScriptProperty]
		[Range(20, 2000)]
		[Display(Name = "Maximum Regression Length", Order = 2, GroupName = "04. Regression Engine")]
		public int MaximumRegressionLength { get; set; }

		[NinjaScriptProperty]
		[Range(1, 250)]
		[Display(Name = "Regression Length Step", Order = 3, GroupName = "04. Regression Engine")]
		public int RegressionLengthStep { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Regression Source", Order = 4, GroupName = "04. Regression Engine")]
		public OrcaChannelPriceSource RegressionSource { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Boundary Model", Order = 5, GroupName = "04. Regression Engine")]
		public OrcaChannelRegressionBoundaryModel BoundaryModel { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "Standard-Deviation Multiplier", Order = 6, GroupName = "04. Regression Engine")]
		public double StandardDeviationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(51, 100)]
		[Display(Name = "Residual Percentile", Order = 7, GroupName = "04. Regression Engine")]
		public int ResidualPercentile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Outlier Handling", Order = 8, GroupName = "04. Regression Engine")]
		public OrcaChannelOutlierHandling OutlierHandling { get; set; }
		#endregion

		#region Properties - Repeated-touch engine
		[NinjaScriptProperty]
		[Range(2, 20)]
		[Display(Name = "Minimum Touches", Order = 1, GroupName = "05. Repeated-Touch Engine")]
		public int MinimumTouches { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Minimum Touches Per Side", Order = 2, GroupName = "05. Repeated-Touch Engine")]
		public int MinimumTouchesPerSide { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 5.0)]
		[Display(Name = "Touch Tolerance", Description = "Normalized boundary distance.", Order = 3, GroupName = "05. Repeated-Touch Engine")]
		public double TouchTolerance { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "Minimum Bars Between Touches", Order = 4, GroupName = "05. Repeated-Touch Engine")]
		public int MinimumBarsBetweenTouches { get; set; }

		[NinjaScriptProperty]
		[Range(4, 300)]
		[Display(Name = "Maximum Candidate Lines", Order = 5, GroupName = "05. Repeated-Touch Engine")]
		public int MaximumCandidateLines { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Maximum Allowed Violations", Order = 6, GroupName = "05. Repeated-Touch Engine")]
		public int MaximumAllowedViolations { get; set; }
		#endregion

		#region Properties - Hybrid scoring
		[NinjaScriptProperty]
		[Display(Name = "Enable Swing Engine", Order = 1, GroupName = "06. Hybrid Scoring")]
		public bool EnableSwingEngine { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Regression Engine", Order = 2, GroupName = "06. Hybrid Scoring")]
		public bool EnableRegressionEngine { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Touch Engine", Order = 3, GroupName = "06. Hybrid Scoring")]
		public bool EnableTouchEngine { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Fit Weight", Order = 4, GroupName = "06. Hybrid Scoring")]
		public double FitWeight { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Touch Weight", Order = 5, GroupName = "06. Hybrid Scoring")]
		public double TouchWeight { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Containment Weight", Order = 6, GroupName = "06. Hybrid Scoring")]
		public double ContainmentWeight { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Parallelism Weight", Order = 7, GroupName = "06. Hybrid Scoring")]
		public double ParallelismWeight { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Stability Weight", Order = 8, GroupName = "06. Hybrid Scoring")]
		public double StabilityWeight { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Recency Weight", Order = 9, GroupName = "06. Hybrid Scoring")]
		public double RecencyWeight { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Consensus Weight", Order = 10, GroupName = "06. Hybrid Scoring")]
		public double ConsensusWeight { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Violation Penalty", Order = 11, GroupName = "06. Hybrid Scoring")]
		public double ViolationPenalty { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 50.0)]
		[Display(Name = "Replacement Score Margin", Order = 12, GroupName = "06. Hybrid Scoring")]
		public double ReplacementScoreMargin { get; set; }

		[NinjaScriptProperty]
		[Range(0, 200)]
		[Display(Name = "Minimum Hold Bars", Order = 13, GroupName = "06. Hybrid Scoring")]
		public int MinimumHoldBars { get; set; }
		#endregion

		#region Properties - Classification
		[NinjaScriptProperty]
		[Range(0.0, 2.0)]
		[Display(Name = "Range Slope Threshold", Order = 1, GroupName = "07. Classification")]
		public double RangeSlopeThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 5.0)]
		[Display(Name = "Trend Slope Threshold", Order = 2, GroupName = "07. Classification")]
		public double TrendSlopeThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Minimum Directional Efficiency", Order = 3, GroupName = "07. Classification")]
		public double MinimumDirectionalEfficiency { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Minimum Channel Score", Order = 4, GroupName = "07. Classification")]
		public double MinimumChannelScore { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Confirmation Score", Order = 5, GroupName = "07. Classification")]
		public double ConfirmationScore { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Mature Score", Order = 6, GroupName = "07. Classification")]
		public double MatureScore { get; set; }
		#endregion
		#region Properties - Trade location
		[NinjaScriptProperty]
		[Display(Name = "Enable Trade Locations", Order = 1, GroupName = "08. Trade Location")]
		public bool EnableTradeLocations { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Trend Locations", Order = 2, GroupName = "08. Trade Location")]
		public bool EnableTrendLocations { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Range Locations", Order = 3, GroupName = "08. Trade Location")]
		public bool EnableRangeLocations { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Zone Calculation Mode", Order = 4, GroupName = "08. Trade Location")]
		public OrcaChannelZoneCalculationMode ZoneCalculationMode { get; set; }

		[NinjaScriptProperty]
		[Range(1.0, 45.0)]
		[Display(Name = "Zone Width Percentage", Order = 5, GroupName = "08. Trade Location")]
		public double ZoneWidthPercentage { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Zone Width Ticks", Order = 6, GroupName = "08. Trade Location")]
		public int ZoneWidthTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 10.0)]
		[Display(Name = "Zone Width Normalized Units", Order = 7, GroupName = "08. Trade Location")]
		public double ZoneWidthNormalizedUnits { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 5.0)]
		[Display(Name = "Range-Bar Zone Multiple", Order = 8, GroupName = "08. Trade Location")]
		public double RangeBarZoneMultiple { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Preferred Price", Order = 9, GroupName = "08. Trade Location")]
		public bool ShowPreferredPrice { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Preferred Price Mode", Order = 10, GroupName = "08. Trade Location")]
		public OrcaChannelPreferredPriceMode PreferredPriceMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Countertrend Locations", Order = 11, GroupName = "08. Trade Location")]
		public bool EnableCountertrendLocations { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Location Signals", Order = 12, GroupName = "08. Trade Location")]
		public bool EnableLocationSignals { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Rejection Signals", Order = 13, GroupName = "08. Trade Location")]
		public bool EnableRejectionSignals { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Rejection Confirmation Mode", Order = 14, GroupName = "08. Trade Location")]
		public OrcaChannelRejectionMode RejectionConfirmationMode { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Rejection Close Percent", Order = 15, GroupName = "08. Trade Location")]
		public double RejectionClosePercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Directional Rejection Bar", Order = 16, GroupName = "08. Trade Location")]
		public bool RequireDirectionalRejectionBar { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 10.0)]
		[Display(Name = "Signal Rearm Distance", Description = "Normalized distance outside a zone before the signal rearms.", Order = 17, GroupName = "08. Trade Location")]
		public double SignalRearmDistance { get; set; }
		#endregion

		#region Properties - Breakout and retest
		[NinjaScriptProperty]
		[Display(Name = "Breakout Mode", Order = 1, GroupName = "09. Breakout and Retest")]
		public OrcaChannelBreakoutMode BreakoutMode { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 10.0)]
		[Display(Name = "Breakout Buffer", Description = "Normalized distance beyond a boundary.", Order = 2, GroupName = "09. Breakout and Retest")]
		public double BreakoutBuffer { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Required Confirmation Closes", Order = 3, GroupName = "09. Breakout and Retest")]
		public int RequiredConfirmationCloses { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Allow Wick Penetration", Order = 4, GroupName = "09. Breakout and Retest")]
		public bool AllowWickPenetration { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 10.0)]
		[Display(Name = "Maximum Wick Penetration", Description = "Normalized wick distance permitted before an intrabar break.", Order = 5, GroupName = "09. Breakout and Retest")]
		public double MaximumWickPenetration { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Retest Detection", Order = 6, GroupName = "09. Breakout and Retest")]
		public bool EnableRetestDetection { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 10.0)]
		[Display(Name = "Retest Tolerance", Order = 7, GroupName = "09. Breakout and Retest")]
		public double RetestTolerance { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Extend Broken Channels", Order = 8, GroupName = "09. Breakout and Retest")]
		public bool ExtendBrokenChannels { get; set; }
		#endregion

		#region Properties - Visual
		[NinjaScriptProperty]
		[Display(Name = "Show Upper Boundary", Order = 1, GroupName = "10. Visual")]
		public bool ShowUpperBoundary { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Lower Boundary", Order = 2, GroupName = "10. Visual")]
		public bool ShowLowerBoundary { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Centerline", Order = 3, GroupName = "10. Visual")]
		public bool ShowCenterline { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Fill", Order = 4, GroupName = "10. Visual")]
		public bool ShowFill { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Trade Zones", Order = 5, GroupName = "10. Visual")]
		public bool ShowTradeZones { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Labels", Order = 6, GroupName = "10. Visual")]
		public bool ShowLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Touch Markers", Order = 7, GroupName = "10. Visual")]
		public bool ShowTouchMarkers { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Score", Order = 8, GroupName = "10. Visual")]
		public bool ShowScore { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Method", Order = 9, GroupName = "10. Visual")]
		public bool ShowMethod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Lifecycle State", Order = 10, GroupName = "10. Visual")]
		public bool ShowLifecycleState { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Right-Side Projection", Order = 11, GroupName = "10. Visual")]
		public bool ShowRightSideProjection { get; set; }

		[NinjaScriptProperty]
		[Range(0, 500)]
		[Display(Name = "Projection Bars", Order = 12, GroupName = "10. Visual")]
		public int ProjectionBars { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Historical Channel Opacity", Order = 13, GroupName = "10. Visual")]
		public int HistoricalChannelOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Upper Line Opacity", Order = 14, GroupName = "10. Visual")]
		public int UpperLineOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Lower Line Opacity", Order = 15, GroupName = "10. Visual")]
		public int LowerLineOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Centerline Opacity", Order = 16, GroupName = "10. Visual")]
		public int CenterlineOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Candidate Line Style", Order = 17, GroupName = "10. Visual")]
		public OrcaChannelDashStyle CandidateLineStyle { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Broken Line Style", Order = 18, GroupName = "10. Visual")]
		public OrcaChannelDashStyle BrokenLineStyle { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Upper Line Width", Order = 19, GroupName = "10. Visual")]
		public int UpperLineWidth { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Lower Line Width", Order = 15, GroupName = "10. Visual")]
		public int LowerLineWidth { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Centerline Width", Order = 16, GroupName = "10. Visual")]
		public int CenterlineWidth { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Upper Line Style", Order = 17, GroupName = "10. Visual")]
		public OrcaChannelDashStyle UpperLineStyle { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Lower Line Style", Order = 18, GroupName = "10. Visual")]
		public OrcaChannelDashStyle LowerLineStyle { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Centerline Style", Order = 19, GroupName = "10. Visual")]
		public OrcaChannelDashStyle CenterlineStyle { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Fill Opacity", Order = 20, GroupName = "10. Visual")]
		public int FillOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Zone Opacity", Order = 21, GroupName = "10. Visual")]
		public int ZoneOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Text Font", Order = 22, GroupName = "10. Visual")]
		public string TextFontName { get; set; }

		[NinjaScriptProperty]
		[Range(6, 36)]
		[Display(Name = "Text Size", Order = 23, GroupName = "10. Visual")]
		public int TextSize { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Text Opacity", Order = 24, GroupName = "10. Visual")]
		public int TextOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Label Placement", Order = 25, GroupName = "10. Visual")]
		public OrcaChannelLabelPlacement LabelPlacement { get; set; }
		#endregion

		#region Properties - Visual brushes
		[XmlIgnore]
		[Display(Name = "Upper Line Color", Order = 1, GroupName = "11. Visual Colors")]
		public WpfBrush UpperLineColor { get; set; }
		[Browsable(false)]
		public string UpperLineColorSerializable { get { return Serialize.BrushToString(UpperLineColor); } set { UpperLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Lower Line Color", Order = 2, GroupName = "11. Visual Colors")]
		public WpfBrush LowerLineColor { get; set; }
		[Browsable(false)]
		public string LowerLineColorSerializable { get { return Serialize.BrushToString(LowerLineColor); } set { LowerLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Centerline Color", Order = 3, GroupName = "11. Visual Colors")]
		public WpfBrush CenterlineColor { get; set; }
		[Browsable(false)]
		public string CenterlineColorSerializable { get { return Serialize.BrushToString(CenterlineColor); } set { CenterlineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Fill Color", Order = 4, GroupName = "11. Visual Colors")]
		public WpfBrush FillColor { get; set; }
		[Browsable(false)]
		public string FillColorSerializable { get { return Serialize.BrushToString(FillColor); } set { FillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Long Zone Color", Order = 5, GroupName = "11. Visual Colors")]
		public WpfBrush LongZoneColor { get; set; }
		[Browsable(false)]
		public string LongZoneColorSerializable { get { return Serialize.BrushToString(LongZoneColor); } set { LongZoneColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Short Zone Color", Order = 6, GroupName = "11. Visual Colors")]
		public WpfBrush ShortZoneColor { get; set; }
		[Browsable(false)]
		public string ShortZoneColorSerializable { get { return Serialize.BrushToString(ShortZoneColor); } set { ShortZoneColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Range Zone Color", Order = 7, GroupName = "11. Visual Colors")]
		public WpfBrush RangeZoneColor { get; set; }
		[Browsable(false)]
		public string RangeZoneColorSerializable { get { return Serialize.BrushToString(RangeZoneColor); } set { RangeZoneColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Candidate Channel Color", Order = 8, GroupName = "11. Visual Colors")]
		public WpfBrush CandidateChannelColor { get; set; }
		[Browsable(false)]
		public string CandidateChannelColorSerializable { get { return Serialize.BrushToString(CandidateChannelColor); } set { CandidateChannelColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Broken Channel Color", Order = 9, GroupName = "11. Visual Colors")]
		public WpfBrush BrokenChannelColor { get; set; }
		[Browsable(false)]
		public string BrokenChannelColorSerializable { get { return Serialize.BrushToString(BrokenChannelColor); } set { BrokenChannelColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Text Color", Order = 10, GroupName = "11. Visual Colors")]
		public WpfBrush TextColor { get; set; }
		[Browsable(false)]
		public string TextColorSerializable { get { return Serialize.BrushToString(TextColor); } set { TextColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Swing Method Color", Order = 11, GroupName = "11. Visual Colors")]
		public WpfBrush SwingMethodColor { get; set; }
		[Browsable(false)]
		public string SwingMethodColorSerializable { get { return Serialize.BrushToString(SwingMethodColor); } set { SwingMethodColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Regression Method Color", Order = 12, GroupName = "11. Visual Colors")]
		public WpfBrush RegressionMethodColor { get; set; }
		[Browsable(false)]
		public string RegressionMethodColorSerializable { get { return Serialize.BrushToString(RegressionMethodColor); } set { RegressionMethodColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Touch Method Color", Order = 13, GroupName = "11. Visual Colors")]
		public WpfBrush TouchMethodColor { get; set; }
		[Browsable(false)]
		public string TouchMethodColorSerializable { get { return Serialize.BrushToString(TouchMethodColor); } set { TouchMethodColor = Serialize.StringToBrush(value); } }
		#endregion

		#region Properties - Alerts and diagnostics
		[NinjaScriptProperty]
		[Display(Name = "Alert on Location", Order = 1, GroupName = "12. Alerts")]
		public bool AlertOnLocation { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Alert on Rejection", Order = 2, GroupName = "12. Alerts")]
		public bool AlertOnRejection { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Alert on Break", Order = 3, GroupName = "12. Alerts")]
		public bool AlertOnBreak { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Alert on Retest", Order = 4, GroupName = "12. Alerts")]
		public bool AlertOnRetest { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Alert Sound", Order = 5, GroupName = "12. Alerts")]
		public string AlertSound { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Diagnostics", Order = 1, GroupName = "13. Diagnostics")]
		public bool EnableDiagnostics { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Diagnostics Panel", Order = 2, GroupName = "13. Diagnostics")]
		public bool ShowDiagnosticsPanel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Log Candidate Scores", Order = 3, GroupName = "13. Diagnostics")]
		public bool LogCandidateScores { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Log Channel Changes", Order = 4, GroupName = "13. Diagnostics")]
		public bool LogChannelChanges { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Evaluation Export", Order = 5, GroupName = "13. Diagnostics")]
		public bool EnableEvaluationExport { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Export Path", Order = 6, GroupName = "13. Diagnostics")]
		public string ExportPath { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Performance Timing", Order = 7, GroupName = "13. Diagnostics")]
		public bool PerformanceTiming { get; set; }
		#endregion
	}
}