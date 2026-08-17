#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

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
	public enum OrcaPriceActionDisplayPreset { CleanCore, BlocksFocused, FullContext, Custom }
	public enum OrcaPriceActionFvgFillMode { RemainingOnly, TwoTone }
	public enum OrcaPriceActionTimedPeriod { Minutes15, Minutes30, Hour1, Hours4 }
	public enum OrcaPriceActionTimedExtension { UntilPeriodEnd, UntilFilled }
	public enum OrcaPriceActionVolumeImbalanceMode { Classic, Advanced }
	public enum OrcaPriceActionSweepQuality { QualifiedLiquidity, MajorOnly, AllConfirmedPivots }
	public enum OrcaPriceActionRejectionPreset { Strict, Balanced, Aggressive, Custom }
	public enum OrcaPriceActionLiquidityScope { ProtectedExternalOnly, ExternalAndInternal, AnyConfirmed }
	public enum OrcaPriceActionPivotImportance { Weak, Standard, Major }
	public enum OrcaPriceActionRejectionConfirmation { FollowThroughRequired, ImmediateRejection }
	public enum OrcaPriceActionOrderBlockPreset { Standard, Strict, Broad, Custom }
	public enum OrcaPriceActionOrderBlockDisplay { Body, FullRange, OpenAndMidpoint }
	public enum OrcaPriceActionBlockInvalidation { CloseBeyondDistal, WickBeyondDistal }
	public enum OrcaPriceActionFvgConfluence { Off, Supporting, Required }
	public enum OrcaPriceActionDashStyle { Solid, Dash, Dot, DashDot }
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaPriceAction : Indicator
	{
		#region Internal models
		private enum Direction { Bearish = -1, None = 0, Bullish = 1 }
		private enum PivotScope { Internal, External }
		private enum PivotFunction { NonSponsor, Sponsor }
		private enum PivotProtection { Neutral, Unprotected, Protected }
		private enum PivotTarget { Inactive, ActiveTrendTarget }
		private enum ZoneState { Candidate, Confirmed, Fresh, Touched, Mitigated, Completed, Failed, Expired, Invalidated }
		private enum BlockType { Rejection, StructuralOrderBlock, ContinuationOrderBlock, PropulsionBlock }
		private enum BlockQuality { Weak, Internal, Standard, Strong }
		private enum StructureEventType { Pivot, Bos, Choch, Sweep, RoleChange }
		private enum ZoneVisualType { Fvg, Ifvg, FvgFilled, IfvgFilled, VolumeImbalance, Rejection, StructuralOb, ContinuationOb, Propulsion }
		private enum LabelVisualType { Structure, Bullish, Bearish, Neutral }

		private sealed class PivotModel
		{
			public string Id = string.Empty;
			public int PivotBar;
			public int ConfirmationBar;
			public DateTime PivotTime;
			public DateTime ConfirmationTime;
			public double Price;
			public bool IsHigh;
			public bool BosEligible = true;
			public string Relation = string.Empty;
			public double ReversalAtr;
			public PivotScope InitialScope;
			public PivotScope Scope;
			public PivotFunction Function;
			public PivotProtection Protection;
			public PivotTarget Target;
			public OrcaPriceActionPivotImportance InitialImportance;
			public OrcaPriceActionPivotImportance Importance;
			public bool Broken;
			public int BreakBar = -1;
			public int LastVisibleSweepBar = -1;
		}

		private sealed class StructureEventModel
		{
			public string Id = string.Empty;
			public StructureEventType Type;
			public int BarIndex;
			public int OriginBar;
			public DateTime Time;
			public double Price;
			public Direction Direction;
			public PivotScope Scope;
			public string PivotId = string.Empty;
			public string Text = string.Empty;
			public bool DisplayLabel = true;
		}

		private sealed class FvgModel
		{
			public string Id = string.Empty;
			public Direction Direction;
			public bool IsIfvg;
			public int OriginBar;
			public int ConfirmationBar;
			public int LastUpdateBar;
			public int TerminalBar = -1;
			public DateTime ConfirmationTime;
			public double OriginalLower;
			public double OriginalUpper;
			public double RemainingLower;
			public double RemainingUpper;
			public ZoneState State;
			public bool IfvgCreated;
			public bool FirstPeriod;
			public bool FirstRth;
			public DateTime PeriodEndEastern = DateTime.MinValue;
			public int PeriodEndBar = -1;
			public double FillPercent;
		}

		private sealed class VolumeImbalanceModel
		{
			public string Id = string.Empty;
			public Direction Direction;
			public int OriginBar;
			public int LastUpdateBar;
			public int TerminalBar = -1;
			public double Lower;
			public double Upper;
			public ZoneState State;
		}

		private sealed class BlockModel
		{
			public string Id = string.Empty;
			public BlockType Type;
			public Direction Direction;
			public BlockQuality Quality;
			public ZoneState State;
			public int OriginBar;
			public int ConfirmationBar = -1;
			public int LastUpdateBar;
			public int TerminalBar = -1;
			public int CandidateExpiryBar = -1;
			public DateTime OriginTime;
			public DateTime ConfirmationTime;
			public double Open;
			public double BodyLower;
			public double BodyUpper;
			public double FullLower;
			public double FullUpper;
			public double DisplacementAtr;
			public bool HasFvgConfluence;
			public bool BreakOnly;
			public bool MeanHold;
			public string ParentId = string.Empty;
			public string StructureEventId = string.Empty;
			public readonly List<string> AttachedStructureEvents = new List<string>();
		}

		private sealed class ZoneRenderItem
		{
			public int StartBar;
			public int EndBar;
			public double Lower;
			public double Upper;
			public ZoneVisualType Type;
			public Direction Direction;
			public ZoneState State;
			public BlockQuality Quality;
			public bool Highlight;
			public bool DrawBorder = true;
			public float Opacity;
			public float LineWidth = 1f;
			public OrcaPriceActionDashStyle DashStyle;
			public string Label = string.Empty;
			public double Open = double.NaN;
			public double Midpoint = double.NaN;
			public bool LinesOnly;
		}

		private sealed class LineRenderItem
		{
			public int StartBar;
			public int EndBar;
			public double Price;
			public Direction Direction;
			public OrcaPriceActionDashStyle DashStyle;
			public float Width;
			public string Label = string.Empty;
		}

		private sealed class LabelRenderItem
		{
			public int BarIndex;
			public double Price;
			public string Text = string.Empty;
			public LabelVisualType Type;
			public bool Above;
			public bool Centered;
			public float PixelOffsetY;
		}

		private sealed class RenderSnapshot
		{
			public static readonly RenderSnapshot Empty = new RenderSnapshot(
				new ZoneRenderItem[0], new LineRenderItem[0], new LabelRenderItem[0], string.Empty);
			public readonly ZoneRenderItem[] Zones;
			public readonly LineRenderItem[] Lines;
			public readonly LabelRenderItem[] Labels;
			public readonly string Diagnostics;

			public RenderSnapshot(ZoneRenderItem[] zones, LineRenderItem[] lines, LabelRenderItem[] labels, string diagnostics)
			{
				Zones = zones ?? new ZoneRenderItem[0];
				Lines = lines ?? new LineRenderItem[0];
				Labels = labels ?? new LabelRenderItem[0];
				Diagnostics = diagnostics ?? string.Empty;
			}
		}
		#endregion

		#region Runtime fields
		private readonly List<PivotModel> pivots = new List<PivotModel>();
		private readonly List<StructureEventModel> structureEvents = new List<StructureEventModel>();
		private readonly List<FvgModel> fvgs = new List<FvgModel>();
		private readonly List<VolumeImbalanceModel> volumeImbalances = new List<VolumeImbalanceModel>();
		private readonly List<BlockModel> blocks = new List<BlockModel>();
		private readonly Dictionary<string, string> firstPeriodFvgIds = new Dictionary<string, string>();
		private readonly Dictionary<string, string> firstRthFvgIds = new Dictionary<string, string>();

		private ATR atr;
		private int lastSeenBar = -1;
		private int nextSequence = 1;
		private Direction trendDirection = Direction.None;
		private string protectedLowId = string.Empty;
		private string protectedHighId = string.Empty;
		private string activeTargetId = string.Empty;
		private double continuationTriggerPrice = double.NaN;
		private TimeZoneInfo easternTimeZone;
		private volatile RenderSnapshot renderSnapshot = RenderSnapshot.Empty;
		private string diagnosticsInstanceId = string.Empty;
		private long lastModelTicks;
		private long lastSnapshotTicks;
		private double lastRenderMilliseconds;

		private IntPtr dxRenderTarget = IntPtr.Zero;
		private DxSolidBrush[] dxBrushes;
		private StrokeStyle[] dxStrokes;
		private DxTextFormat dxTextFormat;
		private DxTextFormat dxSmallFormat;
		private DxTextFormat dxCenteredSmallFormat;
		private bool dxValid;

		private bool applyingDisplayPreset;
		private OrcaPriceActionDisplayPreset displayPreset;
		private bool showFvg;
		private bool showIfvg;
		private bool showVolumeImbalance;
		private bool showStructure;
		private bool showRejectionBlocks;
		private bool showStructuralOrderBlocks;
		private bool showContinuationOrderBlocks;
		private bool showPropulsionBlocks;
		private bool showTimedFirstFvg;
		private bool showFirstRthFvg;

		private const int BrushBull = 0;
		private const int BrushBear = 1;
		private const int BrushStructure = 2;
		private const int BrushHighlight = 3;
		private const int BrushText = 4;
		private const int BrushNeutral = 5;
		private const int BrushPanel = 6;
		private const int BrushFvgBullFill = 7;
		private const int BrushFvgBearFill = 8;
		private const int BrushFvgBullBorder = 9;
		private const int BrushFvgBearBorder = 10;
		private const int BrushIfvgBullFill = 11;
		private const int BrushIfvgBearFill = 12;
		private const int BrushIfvgBullBorder = 13;
		private const int BrushIfvgBearBorder = 14;
		private const int BrushViBullFill = 15;
		private const int BrushViBearFill = 16;
		private const int BrushViBullBorder = 17;
		private const int BrushViBearBorder = 18;
		private const int BrushRejectionBullFill = 19;
		private const int BrushRejectionBearFill = 20;
		private const int BrushRejectionBullBorder = 21;
		private const int BrushRejectionBearBorder = 22;
		private const int BrushStructuralObBullFill = 23;
		private const int BrushStructuralObBearFill = 24;
		private const int BrushStructuralObBullBorder = 25;
		private const int BrushStructuralObBearBorder = 26;
		private const int BrushContinuationObBullFill = 27;
		private const int BrushContinuationObBearFill = 28;
		private const int BrushContinuationObBullBorder = 29;
		private const int BrushContinuationObBearBorder = 30;
		private const int BrushPropulsionBullFill = 31;
		private const int BrushPropulsionBearFill = 32;
		private const int BrushPropulsionBullBorder = 33;
		private const int BrushPropulsionBearBorder = 34;
		private const int BrushCount = 35;
		#endregion

		#region State and update loop
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca Price Action";
				Description = "Causal price-action context: FVG/iFVG, volume imbalance, market structure, rejection blocks, and typed order blocks.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DrawOnPricePanel = true;
				DisplayInDataBox = false;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;

				AtrPeriod = 14;
				MaximumHistoricalBars = 3000;
				MaximumTerminalRecords = 200;
				ShowCandidates = false;
				ShowCompletedZones = false;
				ShowWeakBlocks = false;

				UseMinimumFvgSize = false;
				MinimumFvgSizePoints = 5.0;
				RequireDirectionalMiddleCandle = false;
				RequireFvgDisplacement = false;
				FvgDisplacementAtr = 1.0;
				FvgFillMode = OrcaPriceActionFvgFillMode.RemainingOnly;
				EnableIfvgConversion = true;
				FvgExtensionBars = 30;
				TimedFvgPeriod = OrcaPriceActionTimedPeriod.Hour1;
				TimedFvgExtension = OrcaPriceActionTimedExtension.UntilPeriodEnd;
				RthOpen = new TimeSpan(9, 30, 0);
				RthClose = new TimeSpan(16, 0, 0);

				VolumeImbalanceMode = OrcaPriceActionVolumeImbalanceMode.Classic;

				PivotStrength = 5;
				StructureBreakBufferTicks = 0;
				ShowBos = true;
				ShowChoch = true;
				ShowLiquiditySweeps = true;
				SweepQuality = OrcaPriceActionSweepQuality.QualifiedLiquidity;
				MinimumSweepPenetrationTicks = 1;
				MinimumSweepRestingBars = 3;
				SweepLabelWindowBars = 5;
				ShowProtectedLevels = true;
				ShowDetailedRoleBadges = false;

				RejectionPreset = OrcaPriceActionRejectionPreset.Strict;
				RejectionLiquidityScope = OrcaPriceActionLiquidityScope.ProtectedExternalOnly;
				MinimumRejectionPivotImportance = OrcaPriceActionPivotImportance.Standard;
				RejectionConfirmation = OrcaPriceActionRejectionConfirmation.FollowThroughRequired;
				RejectionConfirmationBars = 3;
				MinimumRejectionWickPercent = 50;
				RejectionSweepTicks = 1;
				ShowStandaloneRejectionCandles = false;

				OrderBlockPreset = OrcaPriceActionOrderBlockPreset.Standard;
				OrderBlockDisplay = OrcaPriceActionOrderBlockDisplay.Body;
				OrderBlockInvalidation = OrcaPriceActionBlockInvalidation.CloseBeyondDistal;
				StructuralObDisplacementAtr = 1.0;
				ContinuationObDisplacementAtr = 0.75;
				PropulsionDisplacementAtr = 0.75;
				StructuralObWindow = 8;
				ContinuationObWindow = 5;
				PropulsionWindow = 5;
				StructuralObFvgMode = OrcaPriceActionFvgConfluence.Supporting;
				ContinuationObFvgMode = OrcaPriceActionFvgConfluence.Supporting;
				PropulsionFvgMode = OrcaPriceActionFvgConfluence.Supporting;
				EnableBreakOnlyBlocks = false;
				RequirePropulsionMeanHold = false;
				ShowOrderBlockOpen = false;
				ShowOrderBlockMidpoint = false;

				FilledZoneOpacity = 10;
				CompletedZoneOpacity = 12;
				TextSize = 10;
				TextFontName = "Segoe UI";
				BullishColor = WpfBrushes.MediumSeaGreen;
				BearishColor = WpfBrushes.IndianRed;
				StructureColor = WpfBrushes.SlateGray;
				HighlightColor = WpfBrushes.Gold;
				TextColor = WpfBrushes.WhiteSmoke;
				BullishFvgFillColor = WpfBrushes.MediumSeaGreen;
				BearishFvgFillColor = WpfBrushes.IndianRed;
				BullishFvgBorderColor = WpfBrushes.MediumSeaGreen;
				BearishFvgBorderColor = WpfBrushes.IndianRed;
				FvgOpacity = 30;
				BullishIfvgFillColor = WpfBrushes.DodgerBlue;
				BearishIfvgFillColor = WpfBrushes.DarkOrange;
				BullishIfvgBorderColor = WpfBrushes.DodgerBlue;
				BearishIfvgBorderColor = WpfBrushes.DarkOrange;
				IfvgOpacity = 30;
				BullishVolumeImbalanceFillColor = WpfBrushes.MediumSeaGreen;
				BearishVolumeImbalanceFillColor = WpfBrushes.IndianRed;
				BullishVolumeImbalanceBorderColor = WpfBrushes.MediumSeaGreen;
				BearishVolumeImbalanceBorderColor = WpfBrushes.IndianRed;
				VolumeImbalanceOpacity = 30;
				BullishRejectionBlockFillColor = WpfBrushes.SeaGreen;
				BearishRejectionBlockFillColor = WpfBrushes.Firebrick;
				BullishRejectionBlockBorderColor = WpfBrushes.SeaGreen;
				BearishRejectionBlockBorderColor = WpfBrushes.Firebrick;
				RejectionBlockOpacity = 24;
				BullishStructuralObFillColor = WpfBrushes.ForestGreen;
				BearishStructuralObFillColor = WpfBrushes.Crimson;
				BullishStructuralObBorderColor = WpfBrushes.ForestGreen;
				BearishStructuralObBorderColor = WpfBrushes.Crimson;
				StructuralObOpacity = 24;
				BullishContinuationObFillColor = WpfBrushes.OliveDrab;
				BearishContinuationObFillColor = WpfBrushes.DarkSalmon;
				BullishContinuationObBorderColor = WpfBrushes.OliveDrab;
				BearishContinuationObBorderColor = WpfBrushes.DarkSalmon;
				ContinuationObOpacity = 18;
				BullishPropulsionBlockFillColor = WpfBrushes.LimeGreen;
				BearishPropulsionBlockFillColor = WpfBrushes.OrangeRed;
				BullishPropulsionBlockBorderColor = WpfBrushes.LimeGreen;
				BearishPropulsionBlockBorderColor = WpfBrushes.OrangeRed;
				PropulsionBlockOpacity = 32;

				ShowDiagnosticsPanel = false;
				ApplyDisplayPreset(OrcaPriceActionDisplayPreset.BlocksFocused);
			}
			else if (State == State.Configure)
			{
				Calculate = Calculate.OnPriceChange;
			}
			else if (State == State.DataLoaded)
			{
				atr = ATR(Math.Max(2, AtrPeriod));
				easternTimeZone = FindEasternTimeZone();
				ResetRuntimeState();
				RegisterDiagnostics();
			}
			else if (State == State.Historical || State == State.Realtime)
			{
				ReportDiagnosticsState();
				if (State == State.Realtime)
					PublishRenderSnapshot();
			}
			else if (State == State.Terminated)
			{
				if (!string.IsNullOrEmpty(diagnosticsInstanceId))
					OrcaDiagnosticsCore.UnregisterInstance(diagnosticsInstanceId);
				DisposeDx();
			}
		}

		private void ResetRuntimeState()
		{
			pivots.Clear();
			structureEvents.Clear();
			fvgs.Clear();
			volumeImbalances.Clear();
			blocks.Clear();
			firstPeriodFvgIds.Clear();
			firstRthFvgIds.Clear();
			lastSeenBar = -1;
			nextSequence = 1;
			trendDirection = Direction.None;
			protectedLowId = string.Empty;
			protectedHighId = string.Empty;
			activeTargetId = string.Empty;
			continuationTriggerPrice = double.NaN;
			renderSnapshot = RenderSnapshot.Empty;
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			long diagnosticsWorkStart = 0;
			if (OrcaDiagnosticsCore.IsEnabled)
			{
				long sequence = OrcaDiagnosticsCore.ReportBarUpdate(diagnosticsInstanceId, 0, Time[0]);
				diagnosticsWorkStart = OrcaDiagnosticsCore.BeginWorkSample(sequence);
			}

			long modelStart = Stopwatch.GetTimestamp();
			try
			{
				bool isNewBar = CurrentBar != lastSeenBar;
				bool modelChanged = false;
				if (isNewBar)
				{
					lastSeenBar = CurrentBar;
					int closedBar = CurrentBar - 1;
					int minimum = Math.Max(AtrPeriod + 2, PivotStrength * 2 + 3);
					int historicalCutoff = State == State.Historical && Bars != null
						? Math.Max(0, Bars.Count - MaximumHistoricalBars - 2)
						: 0;
					if (closedBar >= minimum && closedBar >= historicalCutoff)
					{
						ProcessClosedBar(closedBar);
						modelChanged = true;
					}
				}

				// Historical callbacks need completed-bar model work only. Rebuilding
				// developing FVG geometry and immutable render arrays on every
				// OnPriceChange/Tick Replay event adds no historical correctness.
				if (State == State.Historical)
				{
					if (isNewBar && Bars != null && CurrentBar >= Bars.Count - 1)
						PublishRenderSnapshot();
					return;
				}

				bool intrabarFvgChanged = UpdateIntrabarFvg(CurrentBar);
				if (modelChanged || intrabarFvgChanged)
					PublishRenderSnapshot();
			}
			finally
			{
				lastModelTicks = Stopwatch.GetTimestamp() - modelStart;
				if (OrcaDiagnosticsCore.IsEnabled)
				{
					OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.BarUpdate, 0, diagnosticsWorkStart);
					OrcaDiagnosticsCore.ReportWorkPhaseSample(diagnosticsInstanceId, "PriceAction model", lastModelTicks);
				}
			}
		}

		private void ProcessClosedBar(int barIndex)
		{
			UpdateExistingFvgs(barIndex);
			UpdateVolumeImbalances(barIndex);
			UpdateBlockLifecycles(barIndex);
			ConfirmAvailablePivots(barIndex);
			DetectFairValueGap(barIndex);
			DetectVolumeImbalance(barIndex);

			List<StructureEventModel> sweeps = DetectStructureSweeps(barIndex);
			DetectRejectionBlocks(barIndex, sweeps);
			List<StructureEventModel> breaks = DetectStructureBreaks(barIndex);
			CreateOrderBlocksFromStructure(barIndex, breaks);
			CreateContinuationFromExtension(barIndex, breaks);
			CreatePropulsionCandidates(barIndex);
			AdvancePropulsionCandidates(barIndex);
			PruneTerminalRecords();

			if (OrcaDiagnosticsCore.IsEnabled)
			{
				OrcaDiagnosticsCore.ReportModelUpdate(diagnosticsInstanceId, GetTimeAtBar(barIndex), null);
				int active = fvgs.Count(f => !IsTerminal(f.State))
					+ volumeImbalances.Count(v => !IsTerminal(v.State))
					+ blocks.Count(b => !IsTerminal(b.State));
				OrcaDiagnosticsCore.ReportCacheStatus(diagnosticsInstanceId, "PriceActionState",
					"active=" + active.ToString(CultureInfo.InvariantCulture)
					+ " pivots=" + pivots.Count.ToString(CultureInfo.InvariantCulture)
					+ (active > 500 ? " warning=ActiveRecordPressure" : string.Empty));
			}
		}
		#endregion

		#region Market structure
		private void ConfirmAvailablePivots(int confirmationBar)
		{
			int center = confirmationBar - PivotStrength;
			if (center < PivotStrength || !CanReadBar(center - PivotStrength) || !CanReadBar(center + PivotStrength))
				return;

			if (IsPivotAt(center, true))
				AddConfirmedPivot(center, confirmationBar, true);
			if (IsPivotAt(center, false))
				AddConfirmedPivot(center, confirmationBar, false);
		}

		private bool IsPivotAt(int center, bool isHigh)
		{
			double price = isHigh ? GetHighAtBar(center) : GetLowAtBar(center);
			for (int bar = center - PivotStrength; bar <= center + PivotStrength; bar++)
			{
				if (bar == center)
					continue;
				double comparison = isHigh ? GetHighAtBar(bar) : GetLowAtBar(bar);
				if (isHigh && comparison > price || !isHigh && comparison < price)
					return false;
				// The earliest member of an equal-price cluster is canonical.
				if (bar < center && Math.Abs(comparison - price) <= TickSize * 0.01)
					return false;
			}
			return true;
		}

		private void AddConfirmedPivot(int pivotBar, int confirmationBar, bool isHigh)
		{
			if (pivots.Any(p => p.PivotBar == pivotBar && p.IsHigh == isHigh))
				return;

			double price = isHigh ? GetHighAtBar(pivotBar) : GetLowAtBar(pivotBar);
			PivotModel previous = pivots.Where(p => p.IsHigh == isHigh).OrderByDescending(p => p.PivotBar).FirstOrDefault();
			string relation = previous == null
				? (isHigh ? "H" : "L")
				: isHigh
					? (price > previous.Price ? "HH" : price < previous.Price ? "LH" : "EH")
					: (price > previous.Price ? "HL" : price < previous.Price ? "LL" : "EL");

			double reversal = 0;
			if (isHigh)
			{
				double minimum = price;
				for (int bar = pivotBar + 1; bar <= confirmationBar; bar++)
					minimum = Math.Min(minimum, GetLowAtBar(bar));
				reversal = price - minimum;
			}
			else
			{
				double maximum = price;
				for (int bar = pivotBar + 1; bar <= confirmationBar; bar++)
					maximum = Math.Max(maximum, GetHighAtBar(bar));
				reversal = maximum - price;
			}

			bool external = previous == null || (isHigh ? price > previous.Price : price < previous.Price);
			double reversalAtr = reversal / GetAtrAtBar(confirmationBar);
			OrcaPriceActionPivotImportance importance = reversalAtr < 0.5
				? OrcaPriceActionPivotImportance.Weak : OrcaPriceActionPivotImportance.Standard;
			PivotModel pivot = new PivotModel
			{
				Id = NextId(isHigh ? "PH" : "PL"),
				PivotBar = pivotBar,
				ConfirmationBar = confirmationBar,
				PivotTime = GetTimeAtBar(pivotBar),
				ConfirmationTime = GetTimeAtBar(confirmationBar),
				Price = price,
				IsHigh = isHigh,
				Relation = relation,
				ReversalAtr = reversalAtr,
				InitialScope = external ? PivotScope.External : PivotScope.Internal,
				Scope = external ? PivotScope.External : PivotScope.Internal,
				Function = PivotFunction.NonSponsor,
				Protection = PivotProtection.Neutral,
				Target = PivotTarget.Inactive,
				InitialImportance = importance,
				Importance = importance
			};

			if (trendDirection == Direction.Bullish && isHigh || trendDirection == Direction.Bearish && !isHigh)
			{
				PivotModel oldTarget = FindPivot(activeTargetId);
				if (oldTarget != null)
					oldTarget.Target = PivotTarget.Inactive;
				pivot.Scope = PivotScope.External;
				pivot.Protection = PivotProtection.Unprotected;
				pivot.Target = PivotTarget.ActiveTrendTarget;
				activeTargetId = pivot.Id;
			}

			pivots.Add(pivot);
			structureEvents.Add(new StructureEventModel
			{
				Id = NextId("PIV"),
				Type = StructureEventType.Pivot,
				BarIndex = confirmationBar,
				OriginBar = pivotBar,
				Time = pivot.ConfirmationTime,
				Price = price,
				Direction = isHigh ? Direction.Bearish : Direction.Bullish,
				Scope = pivot.Scope,
				PivotId = pivot.Id,
				Text = relation + " " + (pivot.Scope == PivotScope.External ? "E" : "I")
			});
		}

		private List<StructureEventModel> DetectStructureSweeps(int barIndex)
		{
			List<StructureEventModel> result = new List<StructureEventModel>();
			double high = GetHighAtBar(barIndex);
			double low = GetLowAtBar(barIndex);
			double close = GetCloseAtBar(barIndex);
			double minimumPenetration = Math.Max(0, MinimumSweepPenetrationTicks) * TickSize;

			PivotModel sweptHigh = pivots
				.Where(p => p.IsHigh && !p.Broken && p.ConfirmationBar < barIndex
					&& IsVisibleSweepPivotEligible(p)
					&& IsSweepTimingEligible(p.ConfirmationBar, p.LastVisibleSweepBar, barIndex,
						MinimumSweepRestingBars, SweepLabelWindowBars)
					&& IsSweepPriceAction(high, close, p.Price, minimumPenetration, -1))
				.OrderBy(p => Math.Abs(high - p.Price)).ThenByDescending(p => p.PivotBar).FirstOrDefault();
			PivotModel sweptLow = pivots
				.Where(p => !p.IsHigh && !p.Broken && p.ConfirmationBar < barIndex
					&& IsVisibleSweepPivotEligible(p)
					&& IsSweepTimingEligible(p.ConfirmationBar, p.LastVisibleSweepBar, barIndex,
						MinimumSweepRestingBars, SweepLabelWindowBars)
					&& IsSweepPriceAction(low, close, p.Price, minimumPenetration, 1))
				.OrderBy(p => Math.Abs(low - p.Price)).ThenByDescending(p => p.PivotBar).FirstOrDefault();

			if (sweptHigh != null)
				result.Add(RecordSweep(sweptHigh, barIndex, Direction.Bearish, true));
			if (sweptLow != null)
				result.Add(RecordSweep(sweptLow, barIndex, Direction.Bullish, true));
			return result;
		}

		private bool IsVisibleSweepPivotEligible(PivotModel pivot)
		{
			return pivot != null && IsSweepPivotEligible(SweepQuality, pivot.Importance,
				pivot.Scope == PivotScope.External, pivot.Protection == PivotProtection.Protected,
				pivot.Function == PivotFunction.Sponsor, pivot.Target == PivotTarget.ActiveTrendTarget);
		}

		private StructureEventModel RecordSweep(PivotModel pivot, int barIndex, Direction direction,
			bool displayLabel)
		{
			if (displayLabel)
				pivot.LastVisibleSweepBar = barIndex;
			StructureEventModel model = new StructureEventModel
			{
				Id = NextId("SWP"),
				Type = StructureEventType.Sweep,
				BarIndex = barIndex,
				OriginBar = pivot.PivotBar,
				Time = GetTimeAtBar(barIndex),
				Price = pivot.Price,
				Direction = direction,
				Scope = pivot.Scope,
				PivotId = pivot.Id,
				Text = "Sweep",
				DisplayLabel = displayLabel
			};
			structureEvents.Add(model);
			return model;
		}

		private List<StructureEventModel> DetectStructureBreaks(int barIndex)
		{
			List<StructureEventModel> result = new List<StructureEventModel>();
			double close = GetCloseAtBar(barIndex);
			double buffer = Math.Max(0, StructureBreakBufferTicks) * TickSize;
			List<PivotModel> bullishBreaks = pivots
				.Where(p => p.IsHigh && !p.Broken && p.ConfirmationBar < barIndex
					&& IsStrictBreak(close, p.Price, buffer, 1)).ToList();
			List<PivotModel> bearishBreaks = pivots
				.Where(p => !p.IsHigh && !p.Broken && p.ConfirmationBar < barIndex
					&& IsStrictBreak(close, p.Price, buffer, -1)).ToList();

			Direction breakDirection = bullishBreaks.Count > 0 ? Direction.Bullish
				: bearishBreaks.Count > 0 ? Direction.Bearish : Direction.None;
			List<PivotModel> crossed = breakDirection == Direction.Bullish ? bullishBreaks : bearishBreaks;
			if (crossed.Count == 0)
				return result;

			// The newest crossed pivot is the immediate structure level. A distinct
			// protected boundary can still publish CHoCH for the parent trend. Every
			// older crossed level is retired without another structure annotation.
			PivotModel immediatePivot = crossed.Where(p => p.BosEligible)
				.OrderByDescending(p => p.PivotBar).FirstOrDefault();
			PivotModel protectedBreak = crossed
				.Where(p => p.Protection == PivotProtection.Protected
					&& ((breakDirection == Direction.Bullish && trendDirection == Direction.Bearish)
						|| (breakDirection == Direction.Bearish && trendDirection == Direction.Bullish)))
				.OrderByDescending(p => p.PivotBar).FirstOrDefault();
			HashSet<string> publishedIds = new HashSet<string>();
			if (immediatePivot != null)
				publishedIds.Add(immediatePivot.Id);
			if (protectedBreak != null)
				publishedIds.Add(protectedBreak.Id);
			PivotModel structuralBoundary = immediatePivot ?? protectedBreak;
			if (structuralBoundary != null)
			{
				for (int i = 0; i < pivots.Count; i++)
				{
					PivotModel older = pivots[i];
					if (older.IsHigh == structuralBoundary.IsHigh && !older.Broken
						&& older.PivotBar < structuralBoundary.PivotBar)
						older.BosEligible = false;
				}
			}

			for (int i = 0; i < crossed.Count; i++)
			{
				PivotModel pivot = crossed[i];
				pivot.Broken = true;
				pivot.BreakBar = barIndex;
				pivot.Target = PivotTarget.Inactive;
				if (pivot.Id == activeTargetId)
					activeTargetId = string.Empty;
				if (!publishedIds.Contains(pivot.Id))
					continue;

				bool choch = protectedBreak != null && pivot.Id == protectedBreak.Id;
				if (choch)
				{
					pivot.Protection = PivotProtection.Unprotected;
					if (pivot.Id == protectedLowId) protectedLowId = string.Empty;
					if (pivot.Id == protectedHighId) protectedHighId = string.Empty;
					RecordRoleChange(pivot, barIndex, "Unprotected");
				}
				StructureEventModel model = new StructureEventModel
				{
					Id = NextId(choch ? "CHOCH" : "BOS"),
					Type = choch ? StructureEventType.Choch : StructureEventType.Bos,
					BarIndex = barIndex,
					OriginBar = pivot.PivotBar,
					Time = GetTimeAtBar(barIndex),
					Price = pivot.Price,
					Direction = breakDirection,
					Scope = pivot.Scope,
					PivotId = pivot.Id,
					Text = choch ? "CHoCH" : "BOS"
				};
				structureEvents.Add(model);
				result.Add(model);
			}

			if (result.Count == 0)
				return result;

			StructureEventModel primary = result
				.OrderByDescending(e => e.Type == StructureEventType.Choch ? 1 : 0)
				.ThenByDescending(e => e.OriginBar).First();
			Direction previousTrend = trendDirection;
			trendDirection = primary.Direction;
			PromoteProtectedSponsor(barIndex, trendDirection);
			if (previousTrend != trendDirection || !IsFinite(continuationTriggerPrice))
				continuationTriggerPrice = close;
			return result;
		}

		private void PromoteProtectedSponsor(int barIndex, Direction direction)
		{
			bool sponsorIsHigh = direction == Direction.Bearish;
			PivotModel sponsor = pivots
				.Where(p => p.IsHigh == sponsorIsHigh && !p.Broken && p.ConfirmationBar < barIndex)
				.OrderByDescending(p => p.PivotBar).FirstOrDefault();
			if (sponsor == null)
				return;

			string oldId = direction == Direction.Bullish ? protectedLowId : protectedHighId;
			PivotModel previous = FindPivot(oldId);
			if (previous != null && previous.Id != sponsor.Id)
			{
				previous.Protection = PivotProtection.Unprotected;
				RecordRoleChange(previous, barIndex, "Unprotected");
			}

			sponsor.Function = PivotFunction.Sponsor;
			sponsor.Protection = PivotProtection.Protected;
			sponsor.Scope = PivotScope.External;
			sponsor.Importance = OrcaPriceActionPivotImportance.Major;
			if (direction == Direction.Bullish)
				protectedLowId = sponsor.Id;
			else
				protectedHighId = sponsor.Id;
			RecordRoleChange(sponsor, barIndex, direction == Direction.Bullish ? "Protected Low" : "Protected High");
		}

		private void RecordRoleChange(PivotModel pivot, int barIndex, string text)
		{
			structureEvents.Add(new StructureEventModel
			{
				Id = NextId("ROLE"),
				Type = StructureEventType.RoleChange,
				BarIndex = barIndex,
				OriginBar = pivot.PivotBar,
				Time = GetTimeAtBar(barIndex),
				Price = pivot.Price,
				Direction = pivot.IsHigh ? Direction.Bearish : Direction.Bullish,
				Scope = pivot.Scope,
				PivotId = pivot.Id,
				Text = text
			});
		}
		#endregion

		#region Fair value gaps and volume imbalances
		private void DetectFairValueGap(int barIndex)
		{
			if (barIndex < 2 || !CanReadBar(barIndex - 2))
				return;

			double firstHigh = GetHighAtBar(barIndex - 2);
			double firstLow = GetLowAtBar(barIndex - 2);
			double thirdHigh = GetHighAtBar(barIndex);
			double thirdLow = GetLowAtBar(barIndex);
			Direction direction = (Direction)ClassifyStandardFvg(firstHigh, firstLow, thirdHigh, thirdLow);
			if (direction == Direction.None)
				return;

			double lower = direction == Direction.Bullish ? firstHigh : thirdHigh;
			double upper = direction == Direction.Bullish ? thirdLow : firstLow;
			double size = upper - lower;
			if (!IsFinite(size) || size <= 0 || (UseMinimumFvgSize && size + 1e-10 < MinimumFvgSizePoints))
				return;

			Direction middleDirection = CandleDirection(barIndex - 1);
			if (RequireDirectionalMiddleCandle && middleDirection != direction)
				return;

			if (RequireFvgDisplacement)
			{
				double body = Math.Abs(GetCloseAtBar(barIndex - 1) - GetOpenAtBar(barIndex - 1));
				double ratio = body / GetAtrAtBar(barIndex - 1);
				if (!IsFinite(ratio) || ratio + 1e-10 < FvgDisplacementAtr)
					return;
			}

			FvgModel model = new FvgModel
			{
				Id = NextId("FVG"),
				Direction = direction,
				IsIfvg = false,
				OriginBar = barIndex - 2,
				ConfirmationBar = barIndex,
				LastUpdateBar = barIndex,
				ConfirmationTime = GetTimeAtBar(barIndex),
				OriginalLower = lower,
				OriginalUpper = upper,
				RemainingLower = lower,
				RemainingUpper = upper,
				State = ZoneState.Fresh,
				FillPercent = 0
			};
			QualifyTimedFvg(model);
			fvgs.Add(model);
		}

		private void QualifyTimedFvg(FvgModel model)
		{
			if (model == null || model.IsIfvg)
				return;

			DateTime eastern = ToEastern(model.ConfirmationTime);
			if (ShowTimedFirstFvg)
			{
				int minutes = GetTimedPeriodMinutes();
				DateTime bucketStart = GetClockBucketStart(eastern, minutes);
				string key = bucketStart.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)
					+ "-" + minutes.ToString(CultureInfo.InvariantCulture);
				if (!firstPeriodFvgIds.ContainsKey(key))
				{
					firstPeriodFvgIds[key] = model.Id;
					model.FirstPeriod = true;
					model.PeriodEndEastern = bucketStart.AddMinutes(minutes);
				}
			}

			if (ShowFirstRthFvg && eastern.TimeOfDay > RthOpen && eastern.TimeOfDay < RthClose)
			{
				string key = eastern.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
				if (!firstRthFvgIds.ContainsKey(key))
				{
					firstRthFvgIds[key] = model.Id;
					model.FirstRth = true;
				}
			}
		}

		private int GetTimedPeriodMinutes()
		{
			switch (TimedFvgPeriod)
			{
				case OrcaPriceActionTimedPeriod.Minutes15: return 15;
				case OrcaPriceActionTimedPeriod.Minutes30: return 30;
				case OrcaPriceActionTimedPeriod.Hours4: return 240;
				default: return 60;
			}
		}

		private bool UpdateIntrabarFvg(int barIndex)
		{
			if (!CanReadBar(barIndex))
				return false;
			double high = GetHighAtBar(barIndex);
			double low = GetLowAtBar(barIndex);
			bool changed = false;
			for (int i = 0; i < fvgs.Count; i++)
			{
				FvgModel model = fvgs[i];
				if (model == null || barIndex <= model.ConfirmationBar || model.State == ZoneState.Invalidated)
					continue;
				double oldLower = model.RemainingLower;
				double oldUpper = model.RemainingUpper;
				double oldFill = model.FillPercent;
				ApplyFvgFill(model, barIndex, high, low, false);
				if (oldLower != model.RemainingLower || oldUpper != model.RemainingUpper || oldFill != model.FillPercent)
					changed = true;
			}
			return changed;
		}

		private void UpdateExistingFvgs(int barIndex)
		{
			double high = GetHighAtBar(barIndex);
			double low = GetLowAtBar(barIndex);
			double close = GetCloseAtBar(barIndex);
			DateTime eastern = ToEastern(GetTimeAtBar(barIndex));
			List<FvgModel> inversions = new List<FvgModel>();

			for (int i = 0; i < fvgs.Count; i++)
			{
				FvgModel model = fvgs[i];
				if (model == null || barIndex <= model.ConfirmationBar)
					continue;

				if (model.FirstPeriod && model.PeriodEndBar < 0
					&& model.PeriodEndEastern != DateTime.MinValue && eastern >= model.PeriodEndEastern)
					model.PeriodEndBar = Math.Max(model.ConfirmationBar, barIndex - 1);

				if (!model.IsIfvg && !model.IfvgCreated)
				{
					bool inverted = model.Direction == Direction.Bullish
						? close < model.OriginalLower
						: close > model.OriginalUpper;
					if (inverted)
					{
						model.IfvgCreated = true;
						model.State = ZoneState.Invalidated;
						model.TerminalBar = barIndex;
						if (EnableIfvgConversion)
							inversions.Add(CreateInverseFvg(model, barIndex));
						continue;
					}
				}

				if (model.State != ZoneState.Invalidated)
					ApplyFvgFill(model, barIndex, high, low, true);
			}

			for (int i = 0; i < inversions.Count; i++)
				fvgs.Add(inversions[i]);
		}

		private void ApplyFvgFill(FvgModel model, int barIndex, double high, double low, bool commitState)
		{
			if (model == null || IsTerminal(model.State))
				return;

			double width = model.OriginalUpper - model.OriginalLower;
			if (width <= 0)
				return;

			if (model.Direction == Direction.Bullish && low < model.RemainingUpper)
			{
				model.RemainingUpper = Math.Max(model.OriginalLower, low);
			}
			else if (model.Direction == Direction.Bearish && high > model.RemainingLower)
			{
				model.RemainingLower = Math.Min(model.OriginalUpper, high);
			}

			double remaining = Math.Max(0, model.RemainingUpper - model.RemainingLower);
			model.FillPercent = Clamp(100.0 * (1.0 - remaining / width), 0, 100);
			model.LastUpdateBar = barIndex;
			if (commitState)
			{
				if (remaining <= TickSize * 0.01)
				{
					model.State = ZoneState.Completed;
					if (model.TerminalBar < 0)
						model.TerminalBar = barIndex;
				}
				else if (model.FillPercent > 0)
					model.State = ZoneState.Touched;
			}
		}

		private FvgModel CreateInverseFvg(FvgModel source, int confirmationBar)
		{
			return new FvgModel
			{
				Id = NextId("IFVG"),
				Direction = source.Direction == Direction.Bullish ? Direction.Bearish : Direction.Bullish,
				IsIfvg = true,
				OriginBar = source.OriginBar,
				ConfirmationBar = confirmationBar,
				LastUpdateBar = confirmationBar,
				ConfirmationTime = GetTimeAtBar(confirmationBar),
				OriginalLower = source.OriginalLower,
				OriginalUpper = source.OriginalUpper,
				RemainingLower = source.OriginalLower,
				RemainingUpper = source.OriginalUpper,
				State = ZoneState.Fresh
			};
		}

		private void DetectVolumeImbalance(int barIndex)
		{
			if (barIndex < 1)
				return;

			int previous = barIndex - 1;
			double previousClose = GetCloseAtBar(previous);
			double currentOpen = GetOpenAtBar(barIndex);
			Direction direction = (Direction)ClassifyVolumeImbalance(
				GetOpenAtBar(previous), previousClose, GetHighAtBar(previous), GetLowAtBar(previous),
				currentOpen, GetCloseAtBar(barIndex), GetHighAtBar(barIndex), GetLowAtBar(barIndex),
				VolumeImbalanceMode == OrcaPriceActionVolumeImbalanceMode.Advanced);
			if (direction == Direction.None)
				return;

			volumeImbalances.Add(new VolumeImbalanceModel
			{
				Id = NextId("VI"),
				Direction = direction,
				OriginBar = previous,
				LastUpdateBar = barIndex,
				Lower = Math.Min(previousClose, currentOpen),
				Upper = Math.Max(previousClose, currentOpen),
				State = ZoneState.Fresh
			});
		}

		private void UpdateVolumeImbalances(int barIndex)
		{
			double high = GetHighAtBar(barIndex);
			double low = GetLowAtBar(barIndex);
			double close = GetCloseAtBar(barIndex);
			for (int i = 0; i < volumeImbalances.Count; i++)
			{
				VolumeImbalanceModel model = volumeImbalances[i];
				if (model == null || IsTerminal(model.State) || barIndex <= model.OriginBar)
					continue;
				if (model.Direction == Direction.Bullish && close < model.Lower
					|| model.Direction == Direction.Bearish && close > model.Upper)
				{
					model.State = ZoneState.Invalidated;
					model.TerminalBar = barIndex;
				}
				else if (high >= model.Lower && low <= model.Upper)
				{
					model.State = close >= model.Lower && close <= model.Upper
						? ZoneState.Mitigated : ZoneState.Touched;
				}
				model.LastUpdateBar = barIndex;
			}
		}
		#endregion

		#region Rejection and order blocks
		private void DetectRejectionBlocks(int barIndex, List<StructureEventModel> sweeps)
		{
			List<StructureEventModel> rejectionSweeps = sweeps == null
				? new List<StructureEventModel>() : new List<StructureEventModel>(sweeps);
			HashSet<string> sweptPivotIds = new HashSet<string>(rejectionSweeps.Select(e => e.PivotId));
			double highNow = GetHighAtBar(barIndex);
			double lowNow = GetLowAtBar(barIndex);
			double closeNow = GetCloseAtBar(barIndex);
			double sweepDistance = GetRejectionSweepTicks() * TickSize;
			foreach (bool highSide in new[] { true, false })
			{
				PivotModel eligible = pivots
					.Where(p => p.IsHigh == highSide && !p.Broken && p.ConfirmationBar < barIndex
						&& IsRejectionPivotEligible(p) && !sweptPivotIds.Contains(p.Id)
						&& (highSide
							? highNow >= p.Price + sweepDistance && closeNow <= p.Price
							: lowNow <= p.Price - sweepDistance && closeNow >= p.Price))
					.OrderByDescending(p => p.Importance).ThenByDescending(p => p.Scope)
					.ThenByDescending(p => p.PivotBar).FirstOrDefault();
				if (eligible != null)
				{
					StructureEventModel extra = RecordSweep(eligible, barIndex,
						highSide ? Direction.Bearish : Direction.Bullish, false);
					rejectionSweeps.Add(extra);
					sweptPivotIds.Add(eligible.Id);
				}
			}

			bool created = false;
			if (rejectionSweeps.Count > 0)
			{
				for (int i = 0; i < rejectionSweeps.Count; i++)
				{
					StructureEventModel sweep = rejectionSweeps[i];
					PivotModel pivot = FindPivot(sweep.PivotId);
					if (pivot == null || !IsRejectionPivotEligible(pivot))
						continue;
					double penetration = sweep.Direction == Direction.Bullish
						? pivot.Price - GetLowAtBar(barIndex)
						: GetHighAtBar(barIndex) - pivot.Price;
					if (penetration + 1e-10 < GetRejectionSweepTicks() * TickSize)
						continue;
					if (!PassesRejectionWick(barIndex, sweep.Direction))
						continue;

					double open = GetOpenAtBar(barIndex);
					double close = GetCloseAtBar(barIndex);
					double low = GetLowAtBar(barIndex);
					double high = GetHighAtBar(barIndex);
					double lowerBody = Math.Min(open, close);
					double upperBody = Math.Max(open, close);
					OrcaPriceActionRejectionConfirmation mode = GetRejectionConfirmationMode();
					BlockModel block = new BlockModel
					{
						Id = NextId("RB"),
						Type = BlockType.Rejection,
						Direction = sweep.Direction,
						Quality = GetRejectionQuality(pivot),
						State = mode == OrcaPriceActionRejectionConfirmation.ImmediateRejection ? ZoneState.Fresh : ZoneState.Candidate,
						OriginBar = barIndex,
						ConfirmationBar = mode == OrcaPriceActionRejectionConfirmation.ImmediateRejection ? barIndex : -1,
						LastUpdateBar = barIndex,
						CandidateExpiryBar = barIndex + GetRejectionConfirmationBars(),
						OriginTime = GetTimeAtBar(barIndex),
						ConfirmationTime = mode == OrcaPriceActionRejectionConfirmation.ImmediateRejection ? GetTimeAtBar(barIndex) : DateTime.MinValue,
						Open = open,
						BodyLower = sweep.Direction == Direction.Bullish ? low : upperBody,
						BodyUpper = sweep.Direction == Direction.Bullish ? lowerBody : high,
						FullLower = low,
						FullUpper = high,
						StructureEventId = sweep.Id
					};
					blocks.Add(block);
					created = true;
				}
			}

			if (!created && ShowStandaloneRejectionCandles)
			{
				Direction direction = PassesRejectionWick(barIndex, Direction.Bullish)
					? Direction.Bullish
					: PassesRejectionWick(barIndex, Direction.Bearish) ? Direction.Bearish : Direction.None;
				if (direction != Direction.None)
					structureEvents.Add(new StructureEventModel
					{
						Id = NextId("REJ"), Type = StructureEventType.Sweep, BarIndex = barIndex,
						OriginBar = barIndex, Time = GetTimeAtBar(barIndex),
						Price = direction == Direction.Bullish ? GetLowAtBar(barIndex) : GetHighAtBar(barIndex),
						Direction = direction, Scope = PivotScope.Internal, Text = "Rejection"
					});
			}
		}

		private bool PassesRejectionWick(int barIndex, Direction direction)
		{
			double high = GetHighAtBar(barIndex);
			double low = GetLowAtBar(barIndex);
			double range = high - low;
			if (range <= 0)
				return false;
			double open = GetOpenAtBar(barIndex);
			double close = GetCloseAtBar(barIndex);
			double wick = direction == Direction.Bullish
				? Math.Min(open, close) - low
				: high - Math.Max(open, close);
			return wick / range * 100.0 + 1e-10 >= GetRejectionWickPercent();
		}

		private bool IsRejectionPivotEligible(PivotModel pivot)
		{
			if (pivot == null)
				return false;
			if (RejectionPreset == OrcaPriceActionRejectionPreset.Strict)
				return pivot.Protection == PivotProtection.Protected
					|| pivot.Scope == PivotScope.External
					|| pivot.Importance == OrcaPriceActionPivotImportance.Major;
			if (RejectionPreset == OrcaPriceActionRejectionPreset.Balanced
				|| RejectionPreset == OrcaPriceActionRejectionPreset.Aggressive)
				return pivot.Importance >= OrcaPriceActionPivotImportance.Standard;

			bool scope = RejectionLiquidityScope == OrcaPriceActionLiquidityScope.AnyConfirmed
				|| RejectionLiquidityScope == OrcaPriceActionLiquidityScope.ExternalAndInternal
				|| pivot.Scope == PivotScope.External || pivot.Protection == PivotProtection.Protected;
			return scope && pivot.Importance >= MinimumRejectionPivotImportance;
		}

		private BlockQuality GetRejectionQuality(PivotModel pivot)
		{
			if (pivot.Protection == PivotProtection.Protected || pivot.Scope == PivotScope.External
				|| pivot.Importance == OrcaPriceActionPivotImportance.Major)
				return BlockQuality.Strong;
			return pivot.Importance == OrcaPriceActionPivotImportance.Weak ? BlockQuality.Weak : BlockQuality.Internal;
		}

		private double GetRejectionWickPercent()
		{
			if (RejectionPreset == OrcaPriceActionRejectionPreset.Strict) return 50;
			if (RejectionPreset == OrcaPriceActionRejectionPreset.Balanced) return 45;
			if (RejectionPreset == OrcaPriceActionRejectionPreset.Aggressive) return 40;
			return MinimumRejectionWickPercent;
		}

		private int GetRejectionSweepTicks()
		{
			return RejectionPreset == OrcaPriceActionRejectionPreset.Custom ? RejectionSweepTicks : 1;
		}

		private int GetRejectionConfirmationBars()
		{
			return RejectionPreset == OrcaPriceActionRejectionPreset.Custom ? RejectionConfirmationBars : 3;
		}

		private OrcaPriceActionRejectionConfirmation GetRejectionConfirmationMode()
		{
			if (RejectionPreset == OrcaPriceActionRejectionPreset.Aggressive)
				return OrcaPriceActionRejectionConfirmation.ImmediateRejection;
			if (RejectionPreset == OrcaPriceActionRejectionPreset.Custom)
				return RejectionConfirmation;
			return OrcaPriceActionRejectionConfirmation.FollowThroughRequired;
		}

		private void UpdateBlockLifecycles(int barIndex)
		{
			double high = GetHighAtBar(barIndex);
			double low = GetLowAtBar(barIndex);
			double close = GetCloseAtBar(barIndex);
			for (int i = 0; i < blocks.Count; i++)
			{
				BlockModel block = blocks[i];
				if (block == null || IsTerminal(block.State) || barIndex <= block.OriginBar)
					continue;

				if (block.State == ZoneState.Candidate)
				{
					if (block.Type == BlockType.PropulsionBlock)
						continue;
					bool failed = block.Direction == Direction.Bullish ? close < block.FullLower : close > block.FullUpper;
					if (failed)
					{
						block.State = ZoneState.Failed;
						block.TerminalBar = barIndex;
					}
					else if (barIndex > block.CandidateExpiryBar)
					{
						block.State = ZoneState.Expired;
						block.TerminalBar = barIndex;
					}
					else if (block.Type == BlockType.Rejection
						&& (block.Direction == Direction.Bullish ? close > block.FullUpper : close < block.FullLower))
					{
						block.State = ZoneState.Fresh;
						block.ConfirmationBar = barIndex;
						block.ConfirmationTime = GetTimeAtBar(barIndex);
					}
					block.LastUpdateBar = barIndex;
					continue;
				}

				bool invalid;
				if (block.Type == BlockType.Rejection)
					invalid = block.Direction == Direction.Bullish ? close < block.FullLower : close > block.FullUpper;
				else if (OrderBlockInvalidation == OrcaPriceActionBlockInvalidation.WickBeyondDistal)
					invalid = block.Direction == Direction.Bullish ? low < block.FullLower : high > block.FullUpper;
				else
					invalid = block.Direction == Direction.Bullish ? close < block.FullLower : close > block.FullUpper;

				if (invalid)
				{
					block.State = ZoneState.Invalidated;
					block.TerminalBar = barIndex;
				}
				else if (high >= block.BodyLower && low <= block.BodyUpper)
				{
					bool mitigated = block.Type == BlockType.Rejection
						? close >= block.BodyLower && close <= block.BodyUpper
						: block.Direction == Direction.Bullish ? close < block.Open : close > block.Open;
					block.State = mitigated ? ZoneState.Mitigated : ZoneState.Touched;
				}
				block.LastUpdateBar = barIndex;
			}
		}

		private void CreateOrderBlocksFromStructure(int barIndex, List<StructureEventModel> breaks)
		{
			if (breaks == null)
				return;
			for (int i = 0; i < breaks.Count; i++)
			{
				StructureEventModel structureEvent = breaks[i];
				BlockType type = structureEvent.Type == StructureEventType.Choch || structureEvent.Scope == PivotScope.External
					? BlockType.StructuralOrderBlock : BlockType.ContinuationOrderBlock;
				CreateOrderBlock(type, structureEvent.Direction, barIndex, structureEvent);
			}
		}

		private void CreateContinuationFromExtension(int barIndex, List<StructureEventModel> breaks)
		{
			if (trendDirection == Direction.None || !IsFinite(continuationTriggerPrice))
				return;
			if (breaks != null && breaks.Any(e => e.Scope == PivotScope.Internal && e.Direction == trendDirection))
				return;

			double close = GetCloseAtBar(barIndex);
			double threshold = GetObDisplacement(BlockType.ContinuationOrderBlock) * GetAtrAtBar(barIndex);
			bool extension = trendDirection == Direction.Bullish
				? close > continuationTriggerPrice + threshold
				: close < continuationTriggerPrice - threshold;
			if (!extension)
				return;

			StructureEventModel synthetic = new StructureEventModel
			{
				Id = NextId("CONT"), Type = StructureEventType.Bos, BarIndex = barIndex,
				OriginBar = barIndex, Time = GetTimeAtBar(barIndex), Price = close,
				Direction = trendDirection, Scope = PivotScope.Internal, Text = "Continuation"
			};
			if (CreateOrderBlock(BlockType.ContinuationOrderBlock, trendDirection, barIndex, synthetic))
				continuationTriggerPrice = close;
		}

		private bool CreateOrderBlock(BlockType type, Direction direction, int confirmationBar, StructureEventModel structureEvent)
		{
			int window = GetObWindow(type);
			int origin = FindOpposingCandle(direction, confirmationBar, window);
			if (origin < 0)
				return false;

			BlockModel duplicate = blocks.FirstOrDefault(b => b.Type == type && b.Direction == direction && b.OriginBar == origin);
			if (duplicate != null)
			{
				if (structureEvent != null && !duplicate.AttachedStructureEvents.Contains(structureEvent.Id))
					duplicate.AttachedStructureEvents.Add(structureEvent.Id);
				return true;
			}

			double displacement = direction == Direction.Bullish
				? GetCloseAtBar(confirmationBar) - GetHighAtBar(origin)
				: GetLowAtBar(origin) - GetCloseAtBar(confirmationBar);
			double displacementAtr = displacement / GetAtrAtBar(origin);
			double threshold = GetObDisplacement(type);
			bool displacementPass = displacementAtr + 1e-10 >= threshold;
			bool fvg = HasFvgConfluence(direction, origin + 1, confirmationBar);
			OrcaPriceActionFvgConfluence fvgMode = GetObFvgMode(type);
			bool fvgPass = fvgMode != OrcaPriceActionFvgConfluence.Required || fvg;
			bool scopePass = OrderBlockPreset != OrcaPriceActionOrderBlockPreset.Strict
				|| structureEvent == null || structureEvent.Scope == PivotScope.External;
			bool breakOnly = false;
			if (!displacementPass || !fvgPass || !scopePass)
			{
				if (!GetBreakOnlyEnabled() || type == BlockType.PropulsionBlock || !scopePass)
					return false;
				breakOnly = true;
			}

			double open = GetOpenAtBar(origin);
			double close = GetCloseAtBar(origin);
			BlockModel block = new BlockModel
			{
				Id = NextId(type == BlockType.StructuralOrderBlock ? "SOB" : "COB"),
				Type = type,
				Direction = direction,
				State = ZoneState.Fresh,
				OriginBar = origin,
				ConfirmationBar = confirmationBar,
				LastUpdateBar = confirmationBar,
				OriginTime = GetTimeAtBar(origin),
				ConfirmationTime = GetTimeAtBar(confirmationBar),
				Open = open,
				BodyLower = Math.Min(open, close),
				BodyUpper = Math.Max(open, close),
				FullLower = GetLowAtBar(origin),
				FullUpper = GetHighAtBar(origin),
				DisplacementAtr = displacementAtr,
				HasFvgConfluence = fvg,
				BreakOnly = breakOnly,
				StructureEventId = structureEvent == null ? string.Empty : structureEvent.Id
			};
			if (structureEvent != null)
				block.AttachedStructureEvents.Add(structureEvent.Id);
			block.Quality = GradeOrderBlock(block, structureEvent, threshold);
			blocks.Add(block);
			return true;
		}

		private int FindOpposingCandle(Direction direction, int confirmationBar, int window)
		{
			int earliest = Math.Max(0, confirmationBar - Math.Max(1, window));
			for (int bar = confirmationBar - 1; bar >= earliest; bar--)
			{
				Direction candle = CandleDirection(bar);
				if (direction == Direction.Bullish && candle == Direction.Bearish
					|| direction == Direction.Bearish && candle == Direction.Bullish)
					return bar;
			}
			return -1;
		}

		private bool HasFvgConfluence(Direction direction, int firstBar, int lastBar)
		{
			return fvgs.Any(f => !f.IsIfvg && f.Direction == direction
				&& f.ConfirmationBar >= firstBar && f.ConfirmationBar <= lastBar);
		}

		private BlockQuality GradeOrderBlock(BlockModel block, StructureEventModel structureEvent, double threshold)
		{
			if (block.BreakOnly)
				return BlockQuality.Weak;
			int score = 0;
			PivotModel brokenPivot = structureEvent == null ? null : FindPivot(structureEvent.PivotId);
			if (structureEvent != null && structureEvent.Scope == PivotScope.External)
				score += 2;
			if (brokenPivot != null && (brokenPivot.Function == PivotFunction.Sponsor
				|| brokenPivot.Importance == OrcaPriceActionPivotImportance.Major))
				score++;
			if (structureEvent != null && structureEvent.Type == StructureEventType.Choch)
				score++;
			if (block.DisplacementAtr >= threshold * 1.5)
				score++;
			if (block.HasFvgConfluence)
				score++;
			if (block.Type == BlockType.PropulsionBlock && block.MeanHold)
				score++;
			return score >= 3 ? BlockQuality.Strong : BlockQuality.Standard;
		}

		private double GetObDisplacement(BlockType type)
		{
			if (OrderBlockPreset == OrcaPriceActionOrderBlockPreset.Strict)
				return type == BlockType.StructuralOrderBlock ? 1.5 : 1.0;
			if (OrderBlockPreset == OrcaPriceActionOrderBlockPreset.Broad)
				return type == BlockType.StructuralOrderBlock ? 0.75 : 0.5;
			if (OrderBlockPreset == OrcaPriceActionOrderBlockPreset.Standard)
				return type == BlockType.StructuralOrderBlock ? 1.0 : 0.75;
			return type == BlockType.StructuralOrderBlock ? StructuralObDisplacementAtr
				: type == BlockType.ContinuationOrderBlock ? ContinuationObDisplacementAtr : PropulsionDisplacementAtr;
		}

		private int GetObWindow(BlockType type)
		{
			return type == BlockType.StructuralOrderBlock ? StructuralObWindow
				: type == BlockType.ContinuationOrderBlock ? ContinuationObWindow : PropulsionWindow;
		}

		private OrcaPriceActionFvgConfluence GetObFvgMode(BlockType type)
		{
			if (OrderBlockPreset == OrcaPriceActionOrderBlockPreset.Strict)
				return OrcaPriceActionFvgConfluence.Required;
			if (OrderBlockPreset == OrcaPriceActionOrderBlockPreset.Broad)
				return OrcaPriceActionFvgConfluence.Off;
			if (OrderBlockPreset == OrcaPriceActionOrderBlockPreset.Standard)
				return OrcaPriceActionFvgConfluence.Supporting;
			return type == BlockType.StructuralOrderBlock ? StructuralObFvgMode
				: type == BlockType.ContinuationOrderBlock ? ContinuationObFvgMode : PropulsionFvgMode;
		}

		private bool GetBreakOnlyEnabled()
		{
			return OrderBlockPreset == OrcaPriceActionOrderBlockPreset.Broad
				|| OrderBlockPreset == OrcaPriceActionOrderBlockPreset.Custom && EnableBreakOnlyBlocks;
		}

		private void CreatePropulsionCandidates(int barIndex)
		{
			Direction candle = CandleDirection(barIndex);
			if (candle == Direction.None)
				return;

			for (int i = 0; i < blocks.Count; i++)
			{
				BlockModel parent = blocks[i];
				if (parent == null || (parent.Type != BlockType.StructuralOrderBlock && parent.Type != BlockType.ContinuationOrderBlock)
					|| IsTerminal(parent.State) || parent.State == ZoneState.Candidate || barIndex <= parent.ConfirmationBar)
					continue;
				Direction opposing = parent.Direction == Direction.Bullish ? Direction.Bearish : Direction.Bullish;
				if (candle != opposing || GetHighAtBar(barIndex) < parent.BodyLower || GetLowAtBar(barIndex) > parent.BodyUpper)
					continue;
				if (blocks.Any(b => b.Type == BlockType.PropulsionBlock && b.ParentId == parent.Id && b.OriginBar == barIndex))
					continue;

				double open = GetOpenAtBar(barIndex);
				double close = GetCloseAtBar(barIndex);
				double parentMean = (parent.BodyLower + parent.BodyUpper) * 0.5;
				bool meanHold = parent.Direction == Direction.Bullish ? close >= parentMean : close <= parentMean;
				if (RequirePropulsionMeanHold && !meanHold)
					continue;
				blocks.Add(new BlockModel
				{
					Id = NextId("PB"), Type = BlockType.PropulsionBlock, Direction = parent.Direction,
					Quality = BlockQuality.Standard, State = ZoneState.Candidate, OriginBar = barIndex,
					LastUpdateBar = barIndex, CandidateExpiryBar = barIndex + GetObWindow(BlockType.PropulsionBlock),
					OriginTime = GetTimeAtBar(barIndex), Open = open,
					BodyLower = Math.Min(open, close), BodyUpper = Math.Max(open, close),
					FullLower = GetLowAtBar(barIndex), FullUpper = GetHighAtBar(barIndex),
					ParentId = parent.Id, MeanHold = meanHold
				});
			}
		}

		private void AdvancePropulsionCandidates(int barIndex)
		{
			double close = GetCloseAtBar(barIndex);
			for (int i = 0; i < blocks.Count; i++)
			{
				BlockModel block = blocks[i];
				if (block == null || block.Type != BlockType.PropulsionBlock || block.State != ZoneState.Candidate
					|| barIndex <= block.OriginBar)
					continue;
				BlockModel parent = FindBlock(block.ParentId);
				if (parent == null || IsTerminal(parent.State))
				{
					block.State = ZoneState.Failed;
					block.TerminalBar = barIndex;
					continue;
				}
				if (barIndex > block.CandidateExpiryBar)
				{
					block.State = ZoneState.Expired;
					block.TerminalBar = barIndex;
					continue;
				}
				bool failed = block.Direction == Direction.Bullish ? close < block.FullLower : close > block.FullUpper;
				if (failed)
				{
					block.State = ZoneState.Failed;
					block.TerminalBar = barIndex;
					continue;
				}
				double displacement = block.Direction == Direction.Bullish
					? close - block.FullUpper : block.FullLower - close;
				double ratio = displacement / GetAtrAtBar(block.OriginBar);
				bool fvg = HasFvgConfluence(block.Direction, block.OriginBar + 1, barIndex);
				OrcaPriceActionFvgConfluence mode = GetObFvgMode(BlockType.PropulsionBlock);
				if (ratio + 1e-10 < GetObDisplacement(BlockType.PropulsionBlock)
					|| mode == OrcaPriceActionFvgConfluence.Required && !fvg)
					continue;
				block.State = ZoneState.Fresh;
				block.ConfirmationBar = barIndex;
				block.ConfirmationTime = GetTimeAtBar(barIndex);
				block.DisplacementAtr = ratio;
				block.HasFvgConfluence = fvg;
				block.Quality = GradeOrderBlock(block, null, GetObDisplacement(BlockType.PropulsionBlock));
				block.LastUpdateBar = barIndex;
			}
		}

		private void PruneTerminalRecords()
		{
			int maximum = Math.Max(10, MaximumTerminalRecords);
			int removableFvgCount = fvgs.Count(f => IsTerminal(f.State)
				&& (f.IsIfvg || f.IfvgCreated || !EnableIfvgConversion));
			if (removableFvgCount > maximum)
			{
				List<FvgModel> removableFvgs = fvgs
					.Where(f => IsTerminal(f.State) && (f.IsIfvg || f.IfvgCreated || !EnableIfvgConversion))
					.OrderByDescending(f => f.TerminalBar).Skip(maximum).ToList();
				for (int i = 0; i < removableFvgs.Count; i++)
					fvgs.Remove(removableFvgs[i]);
			}

			int removableViCount = volumeImbalances.Count(v => IsTerminal(v.State));
			if (removableViCount > maximum)
			{
				List<VolumeImbalanceModel> removableVi = volumeImbalances.Where(v => IsTerminal(v.State))
					.OrderByDescending(v => v.TerminalBar).Skip(maximum).ToList();
				for (int i = 0; i < removableVi.Count; i++)
					volumeImbalances.Remove(removableVi[i]);
			}

			foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
			{
				int removableBlockCount = blocks.Count(b => b.Type == type && IsTerminal(b.State));
				if (removableBlockCount > maximum)
				{
					List<BlockModel> removable = blocks.Where(b => b.Type == type && IsTerminal(b.State))
						.OrderByDescending(b => b.TerminalBar).Skip(maximum).ToList();
					for (int i = 0; i < removable.Count; i++)
						blocks.Remove(removable[i]);
				}
			}

			int cutoff = Math.Max(0, CurrentBar - Math.Max(500, MaximumHistoricalBars));
			structureEvents.RemoveAll(e => e.BarIndex < cutoff);
			pivots.RemoveAll(p => p.Broken && p.BreakBar >= 0 && p.BreakBar < cutoff
				&& p.Id != protectedLowId && p.Id != protectedHighId && p.Id != activeTargetId);
		}
		#endregion

		#region Snapshot publication
		private void PublishRenderSnapshot()
		{
			long start = Stopwatch.GetTimestamp();
			List<ZoneRenderItem> zones = new List<ZoneRenderItem>();
			List<LineRenderItem> lines = new List<LineRenderItem>();
			List<LabelRenderItem> labels = new List<LabelRenderItem>();

			BuildFvgRenderItems(zones);
			BuildVolumeImbalanceRenderItems(zones);
			BuildBlockRenderItems(zones);
			BuildStructureRenderItems(lines, labels);

			string diagnostics = ShowDiagnosticsPanel
				? string.Format(CultureInfo.InvariantCulture,
					"Orca Price Action\nFVG {0} | VI {1} | Pivots {2} | Blocks {3}\nModel {4:F2}ms | Snapshot {5:F2}ms | Render {6:F2}ms",
					fvgs.Count, volumeImbalances.Count, pivots.Count, blocks.Count,
					lastModelTicks * 1000.0 / Stopwatch.Frequency,
					lastSnapshotTicks * 1000.0 / Stopwatch.Frequency,
					lastRenderMilliseconds)
				: string.Empty;

			renderSnapshot = new RenderSnapshot(zones.ToArray(), lines.ToArray(), labels.ToArray(), diagnostics);
			lastSnapshotTicks = Stopwatch.GetTimestamp() - start;
		}

		private void BuildFvgRenderItems(List<ZoneRenderItem> result)
		{
			DateTime currentEastern = CurrentBar >= 0 ? ToEastern(Time[0]) : DateTime.MinValue;
			for (int i = 0; i < fvgs.Count; i++)
			{
				FvgModel model = fvgs[i];
				if (model == null)
					continue;
				bool baseVisible = model.IsIfvg ? ShowIfvg : ShowFvg;
				bool periodActive = model.FirstPeriod && ShowTimedFirstFvg
					&& (TimedFvgExtension == OrcaPriceActionTimedExtension.UntilFilled
						|| model.PeriodEndEastern == DateTime.MinValue || currentEastern < model.PeriodEndEastern);
				bool rthVisible = model.FirstRth && ShowFirstRthFvg;
				bool terminal = IsTerminal(model.State);
				bool periodHistorical = model.FirstPeriod && ShowTimedFirstFvg
					&& TimedFvgExtension == OrcaPriceActionTimedExtension.UntilPeriodEnd && model.PeriodEndBar >= 0;
				bool timedKeepCompleted = model.FirstPeriod && ShowTimedFirstFvg
					&& TimedFvgExtension == OrcaPriceActionTimedExtension.UntilPeriodEnd && terminal;
				if (!baseVisible && !periodActive && !rthVisible && !timedKeepCompleted && !periodHistorical)
					continue;
				if (terminal && !ShowCompletedZones && !timedKeepCompleted)
					continue;

				int visualStartBar = model.IsIfvg ? model.ConfirmationBar : model.OriginBar;
				int requestedEndBar = terminal ? Math.Max(visualStartBar, model.TerminalBar) : CurrentBar;
				bool timedUntilFilled = model.FirstPeriod && ShowTimedFirstFvg
					&& TimedFvgExtension == OrcaPriceActionTimedExtension.UntilFilled;
				int endBar = timedUntilFilled ? requestedEndBar
					: CapFvgEndBar(visualStartBar, requestedEndBar, FvgExtensionBars);
				if (timedKeepCompleted)
					endBar = model.PeriodEndBar >= 0 ? model.PeriodEndBar : CurrentBar;
				bool highlight = periodActive || rthVisible || timedKeepCompleted;
				string label = model.IsIfvg ? "iFVG" : "FVG";
				if (model.FirstPeriod && ShowTimedFirstFvg)
					label += " " + TimedPeriodLabel();
				if (model.FirstRth && ShowFirstRthFvg)
					label += " RTH";

				if (periodHistorical && !terminal)
				{
					ZoneVisualType historyType = model.IsIfvg ? ZoneVisualType.Ifvg : ZoneVisualType.Fvg;
					ZoneRenderItem history = CreateZone(visualStartBar, model.PeriodEndBar,
						model.RemainingLower, model.RemainingUpper,
						historyType,
						model.Direction, model.State, BlockQuality.Standard, true,
						Math.Min(0.08f, GetZoneOpacity(historyType, model.State, false)), label);
					result.Add(history);
					if (!baseVisible && !rthVisible)
						continue;
				}

				if (terminal)
				{
					ZoneVisualType terminalType = model.IsIfvg ? ZoneVisualType.Ifvg : ZoneVisualType.Fvg;
					result.Add(CreateZone(visualStartBar, endBar, model.OriginalLower, model.OriginalUpper,
						terminalType,
						model.Direction, model.State, BlockQuality.Standard, highlight,
						GetZoneOpacity(terminalType, model.State, false), label));
					continue;
				}

				if (FvgFillMode == OrcaPriceActionFvgFillMode.TwoTone && model.FillPercent > 0)
				{
					double filledLower = model.Direction == Direction.Bullish ? model.RemainingUpper : model.OriginalLower;
					double filledUpper = model.Direction == Direction.Bullish ? model.OriginalUpper : model.RemainingLower;
					if (filledUpper > filledLower)
					{
						ZoneVisualType filledType = model.IsIfvg ? ZoneVisualType.IfvgFilled : ZoneVisualType.FvgFilled;
						result.Add(CreateZone(visualStartBar, endBar, filledLower, filledUpper,
							filledType, model.Direction, model.State, BlockQuality.Standard,
							highlight, GetZoneOpacity(filledType, model.State, true), string.Empty));
					}
				}

				if (model.RemainingUpper > model.RemainingLower)
				{
					ZoneVisualType activeType = model.IsIfvg ? ZoneVisualType.Ifvg : ZoneVisualType.Fvg;
					result.Add(CreateZone(visualStartBar, endBar, model.RemainingLower, model.RemainingUpper,
						activeType,
						model.Direction, model.State, BlockQuality.Standard, highlight,
						GetZoneOpacity(activeType, model.State, false), label));
				}
			}
		}

		private string TimedPeriodLabel()
		{
			switch (TimedFvgPeriod)
			{
				case OrcaPriceActionTimedPeriod.Minutes15: return "15m";
				case OrcaPriceActionTimedPeriod.Minutes30: return "30m";
				case OrcaPriceActionTimedPeriod.Hours4: return "4h";
				default: return "1h";
			}
		}

		private void BuildVolumeImbalanceRenderItems(List<ZoneRenderItem> result)
		{
			if (!ShowVolumeImbalance)
				return;
			for (int i = 0; i < volumeImbalances.Count; i++)
			{
				VolumeImbalanceModel model = volumeImbalances[i];
				if (model == null || IsTerminal(model.State) && !ShowCompletedZones)
					continue;
				int end = IsTerminal(model.State) ? Math.Max(model.OriginBar, model.TerminalBar) : CurrentBar;
				result.Add(CreateZone(model.OriginBar, end, model.Lower, model.Upper,
					ZoneVisualType.VolumeImbalance, model.Direction, model.State, BlockQuality.Standard,
					false, GetZoneOpacity(ZoneVisualType.VolumeImbalance, model.State, false), "VI"));
			}
		}

		private void BuildBlockRenderItems(List<ZoneRenderItem> result)
		{
			for (int i = 0; i < blocks.Count; i++)
			{
				BlockModel block = blocks[i];
				if (block == null || !ShouldShowBlockType(block.Type)
					|| block.Quality == BlockQuality.Weak && !ShowWeakBlocks
					|| block.State == ZoneState.Candidate && !ShowCandidates
					|| IsTerminal(block.State) && !ShowCompletedZones)
					continue;

				ZoneVisualType visual = block.Type == BlockType.Rejection ? ZoneVisualType.Rejection
					: block.Type == BlockType.StructuralOrderBlock ? ZoneVisualType.StructuralOb
					: block.Type == BlockType.ContinuationOrderBlock ? ZoneVisualType.ContinuationOb
					: ZoneVisualType.Propulsion;
				double lower = block.BodyLower;
				double upper = block.BodyUpper;
				bool linesOnly = false;
				if (block.Type != BlockType.Rejection && OrderBlockDisplay == OrcaPriceActionOrderBlockDisplay.FullRange)
				{
					lower = block.FullLower;
					upper = block.FullUpper;
				}
				else if (block.Type != BlockType.Rejection && OrderBlockDisplay == OrcaPriceActionOrderBlockDisplay.OpenAndMidpoint)
					linesOnly = true;

				int end = IsTerminal(block.State) ? Math.Max(block.OriginBar, block.TerminalBar) : CurrentBar;
				string label = block.Type == BlockType.Rejection ? "RB"
					: block.Type == BlockType.StructuralOrderBlock ? "S-OB"
					: block.Type == BlockType.ContinuationOrderBlock ? "C-OB" : "PB";
				if (block.HasFvgConfluence)
					label += "+FVG";
				if (block.Quality == BlockQuality.Weak)
					label += " W";
				ZoneRenderItem item = CreateZone(block.OriginBar, end, lower, upper, visual,
					block.Direction, block.State, block.Quality, false,
					GetZoneOpacity(visual, block.State, false), label);
				item.Open = block.Open;
				item.Midpoint = (block.BodyLower + block.BodyUpper) * 0.5;
				item.LinesOnly = linesOnly;
				item.DashStyle = block.Type == BlockType.ContinuationOrderBlock
					? OrcaPriceActionDashStyle.Dash : OrcaPriceActionDashStyle.Solid;
				item.LineWidth = block.Type == BlockType.PropulsionBlock ? 2.5f
					: block.Quality == BlockQuality.Strong ? 2f : 1f;
				result.Add(item);
			}
		}

		private bool ShouldShowBlockType(BlockType type)
		{
			return type == BlockType.Rejection ? ShowRejectionBlocks
				: type == BlockType.StructuralOrderBlock ? ShowStructuralOrderBlocks
				: type == BlockType.ContinuationOrderBlock ? ShowContinuationOrderBlocks
				: ShowPropulsionBlocks;
		}

		private ZoneRenderItem CreateZone(int start, int end, double lower, double upper,
			ZoneVisualType type, Direction direction, ZoneState state, BlockQuality quality,
			bool highlight, float opacity, string label)
		{
			return new ZoneRenderItem
			{
				StartBar = start, EndBar = Math.Max(start, end), Lower = lower, Upper = upper,
				Type = type, Direction = direction, State = state, Quality = quality,
				Highlight = highlight, Opacity = (float)Clamp(opacity, 0, 1), Label = label
			};
		}

		private float GetZoneOpacity(ZoneVisualType type, ZoneState state, bool filledPortion)
		{
			int opacity;
			switch (type)
			{
				case ZoneVisualType.Ifvg:
				case ZoneVisualType.IfvgFilled:
					opacity = IfvgOpacity;
					break;
				case ZoneVisualType.VolumeImbalance:
					opacity = VolumeImbalanceOpacity;
					break;
				case ZoneVisualType.Rejection:
					opacity = RejectionBlockOpacity;
					break;
				case ZoneVisualType.StructuralOb:
					opacity = StructuralObOpacity;
					break;
				case ZoneVisualType.ContinuationOb:
					opacity = ContinuationObOpacity;
					break;
				case ZoneVisualType.Propulsion:
					opacity = PropulsionBlockOpacity;
					break;
				default:
					opacity = FvgOpacity;
					break;
			}
			if (filledPortion)
				opacity = Math.Min(opacity, FilledZoneOpacity);
			else if (IsTerminal(state))
				opacity = Math.Min(opacity, CompletedZoneOpacity);
			return (float)Clamp(opacity / 100.0, 0, 1);
		}

		private void BuildStructureRenderItems(List<LineRenderItem> lines, List<LabelRenderItem> labels)
		{
			if (!ShowStructure)
				return;
			StructureEventModel pendingBullishSweep = null;
			StructureEventModel pendingBearishSweep = null;
			for (int i = 0; i < structureEvents.Count; i++)
			{
				StructureEventModel model = structureEvents[i];
				if (model == null)
					continue;
				if (model.Type == StructureEventType.Pivot)
				{
					PivotModel pivot = FindPivot(model.PivotId);
					if (pivot == null)
						continue;
					string text = model.Text;
					if (ShowDetailedRoleBadges)
						text += " " + InitialPivotRoleBadge(pivot);
					labels.Add(new LabelRenderItem
					{
						BarIndex = pivot.PivotBar, Price = model.Price, Text = text,
						Type = LabelVisualType.Structure, Above = pivot.IsHigh, Centered = true
					});
				}
				else if (model.Type == StructureEventType.Bos || model.Type == StructureEventType.Choch)
				{
					bool showBreak = model.Type == StructureEventType.Bos ? ShowBos : ShowChoch;
					if (showBreak)
					{
						lines.Add(new LineRenderItem
						{
							StartBar = model.OriginBar, EndBar = model.BarIndex, Price = model.Price,
							Direction = model.Direction,
							DashStyle = model.Type == StructureEventType.Choch
								? OrcaPriceActionDashStyle.Solid : OrcaPriceActionDashStyle.Dash,
							Width = model.Type == StructureEventType.Choch ? 2f : 1f,
							Label = model.Text
						});
					}
					if (model.Direction == Direction.Bullish && pendingBearishSweep != null)
					{
						AddSweepRenderLabel(labels, pendingBearishSweep);
						pendingBearishSweep = null;
					}
					else if (model.Direction == Direction.Bearish && pendingBullishSweep != null)
					{
						AddSweepRenderLabel(labels, pendingBullishSweep);
						pendingBullishSweep = null;
					}
				}
				else if (model.Type == StructureEventType.Sweep && model.DisplayLabel && ShowLiquiditySweeps)
				{
					if (model.Direction == Direction.Bullish)
					{
						if (pendingBullishSweep != null
							&& !IsSameSweepEpisode(pendingBullishSweep.BarIndex, model.BarIndex, SweepLabelWindowBars))
							AddSweepRenderLabel(labels, pendingBullishSweep);
						pendingBullishSweep = model;
					}
					else
					{
						if (pendingBearishSweep != null
							&& !IsSameSweepEpisode(pendingBearishSweep.BarIndex, model.BarIndex, SweepLabelWindowBars))
							AddSweepRenderLabel(labels, pendingBearishSweep);
						pendingBearishSweep = model;
					}
				}
				else if (model.Type == StructureEventType.RoleChange && ShowDetailedRoleBadges)
				{
					labels.Add(new LabelRenderItem
					{
						BarIndex = model.BarIndex, Price = model.Price, Text = model.Text,
						Type = model.Direction == Direction.Bullish ? LabelVisualType.Bullish : LabelVisualType.Bearish,
						Above = model.Direction == Direction.Bearish
					});
				}
			}
			if (pendingBullishSweep != null)
				AddSweepRenderLabel(labels, pendingBullishSweep);
			if (pendingBearishSweep != null)
				AddSweepRenderLabel(labels, pendingBearishSweep);

			if (ShowProtectedLevels)
			{
				PivotModel protectedLow = FindPivot(protectedLowId);
				PivotModel protectedHigh = FindPivot(protectedHighId);
				AddProtectedLine(lines, protectedLow, Direction.Bullish);
				AddProtectedLabel(labels, protectedLow, Direction.Bullish);
				AddProtectedLine(lines, protectedHigh, Direction.Bearish);
				AddProtectedLabel(labels, protectedHigh, Direction.Bearish);
			}
		}

		private void AddSweepRenderLabel(List<LabelRenderItem> labels, StructureEventModel model)
		{
			if (model == null)
				return;
			bool sweptHigh = model.Direction == Direction.Bearish;
			labels.Add(new LabelRenderItem
			{
				BarIndex = model.BarIndex,
				Price = sweptHigh ? GetHighAtBar(model.BarIndex) : GetLowAtBar(model.BarIndex),
				Text = model.Text,
				Type = model.Direction == Direction.Bullish ? LabelVisualType.Bullish : LabelVisualType.Bearish,
				Above = sweptHigh,
				Centered = true
			});
		}

		private string InitialPivotRoleBadge(PivotModel pivot)
		{
			return (pivot.InitialScope == PivotScope.External ? "E" : "I")
				+ "/NS/N/" + (pivot.InitialImportance == OrcaPriceActionPivotImportance.Standard ? "St" : "W");
		}

		private void AddProtectedLine(List<LineRenderItem> lines, PivotModel pivot, Direction direction)
		{
			if (pivot == null || pivot.Protection != PivotProtection.Protected)
				return;
			StructureEventModel effective = structureEvents
				.Where(e => e.Type == StructureEventType.RoleChange && e.PivotId == pivot.Id && e.Text.StartsWith("Protected", StringComparison.Ordinal))
				.OrderByDescending(e => e.BarIndex).FirstOrDefault();
			int start = effective == null ? pivot.ConfirmationBar : effective.BarIndex;
			lines.Add(new LineRenderItem
			{
				StartBar = start, EndBar = CurrentBar, Price = pivot.Price, Direction = direction,
				DashStyle = OrcaPriceActionDashStyle.Dot, Width = 1f,
				Label = string.Empty
			});
		}

		private void AddProtectedLabel(List<LabelRenderItem> labels, PivotModel pivot, Direction direction)
		{
			if (pivot == null || pivot.Protection != PivotProtection.Protected)
				return;
			bool isHigh = direction == Direction.Bearish;
			labels.Add(new LabelRenderItem
			{
				BarIndex = pivot.PivotBar,
				Price = pivot.Price,
				Text = isHigh ? "Protected High" : "Protected Low",
				Type = isHigh ? LabelVisualType.Bearish : LabelVisualType.Bullish,
				Above = isHigh,
				Centered = true,
				PixelOffsetY = isHigh ? -(TextSize + 4f) : TextSize + 4f
			});
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (chartControl == null || chartScale == null || ChartBars == null || ChartPanel == null || RenderTarget == null)
				return;
			RenderSnapshot snapshot = renderSnapshot;
			if (snapshot == null)
				return;
			EnsureDxResources();
			if (!dxValid)
				return;

			long renderStart = Stopwatch.GetTimestamp();
			AntialiasMode prior = RenderTarget.AntialiasMode;
			try
			{
				RenderTarget.AntialiasMode = AntialiasMode.Aliased;
				for (int i = 0; i < snapshot.Zones.Length; i++) RenderZone(snapshot.Zones[i], chartControl, chartScale);
				for (int i = 0; i < snapshot.Lines.Length; i++) RenderLine(snapshot.Lines[i], chartControl, chartScale);
				for (int i = 0; i < snapshot.Labels.Length; i++) RenderLabel(snapshot.Labels[i], chartControl, chartScale);
				if (ShowDiagnosticsPanel && !string.IsNullOrWhiteSpace(snapshot.Diagnostics))
					RenderDiagnostics(snapshot.Diagnostics);
			}
			catch (Exception exception)
			{
				if (ShowDiagnosticsPanel)
					Print("ORCA_PRICE_ACTION render skipped safely: " + exception.Message);
			}
			finally
			{
				RenderTarget.AntialiasMode = prior;
				lastRenderMilliseconds = (Stopwatch.GetTimestamp() - renderStart) * 1000.0 / Stopwatch.Frequency;
				if (OrcaDiagnosticsCore.IsEnabled)
					OrcaDiagnosticsCore.ReportRenderSample(diagnosticsInstanceId, renderStart);
			}
		}

		private void RenderZone(ZoneRenderItem item, ChartControl chartControl, ChartScale chartScale)
		{
			if (item == null || !IsFinite(item.Lower) || !IsFinite(item.Upper) || item.Upper <= item.Lower)
				return;
			int startBar = Math.Max(item.StartBar, ChartBars.FromIndex);
			int endBar = Math.Min(item.EndBar, ChartBars.ToIndex);
			if (endBar < ChartBars.FromIndex || startBar > ChartBars.ToIndex || endBar < startBar)
				return;

			float startX = chartControl.GetXByBarIndex(ChartBars, startBar);
			float endX = chartControl.GetXByBarIndex(ChartBars, endBar) + Math.Max(1f, (float)chartControl.BarWidth * 0.5f);
			float top = chartScale.GetYByValue(item.Upper);
			float bottom = chartScale.GetYByValue(item.Lower);
			RectangleF rectangle = new RectangleF(Math.Min(startX, endX), Math.Min(top, bottom),
				Math.Max(1f, Math.Abs(endX - startX)), Math.Max(1f, Math.Abs(bottom - top)));
			DxSolidBrush fillBrush = GetZoneFillBrush(item);
			DxSolidBrush borderBrush = item.Highlight ? dxBrushes[BrushHighlight] : GetZoneBorderBrush(item);
			float priorFillOpacity = fillBrush.Opacity;
			float priorBorderOpacity = borderBrush.Opacity;
			fillBrush.Opacity = item.Opacity * (item.State == ZoneState.Candidate ? 0.55f : 1f);

			if (!item.LinesOnly)
				RenderTarget.FillRectangle(rectangle, fillBrush);
			borderBrush.Opacity = Math.Min(1f, Math.Max(0.45f, item.Opacity + 0.25f));
			if (item.DrawBorder && !item.LinesOnly)
				RenderTarget.DrawRectangle(rectangle, borderBrush,
					item.Highlight ? Math.Max(2f, item.LineWidth) : item.LineWidth, GetStroke(item.DashStyle));

			bool isOrderBlock = item.Type == ZoneVisualType.StructuralOb
				|| item.Type == ZoneVisualType.ContinuationOb || item.Type == ZoneVisualType.Propulsion;
			bool drawOpen = isOrderBlock && (item.LinesOnly || ShowOrderBlockOpen);
			bool drawMidpoint = isOrderBlock && (item.LinesOnly || ShowOrderBlockMidpoint);
			if (drawOpen && IsFinite(item.Open))
				RenderTarget.DrawLine(new Vector2(startX, chartScale.GetYByValue(item.Open)),
					new Vector2(endX, chartScale.GetYByValue(item.Open)), borderBrush, Math.Max(1f, item.LineWidth), GetStroke(item.DashStyle));
			if (drawMidpoint && IsFinite(item.Midpoint))
				RenderTarget.DrawLine(new Vector2(startX, chartScale.GetYByValue(item.Midpoint)),
					new Vector2(endX, chartScale.GetYByValue(item.Midpoint)), borderBrush, 1f, GetStroke(OrcaPriceActionDashStyle.Dot));

			if (!string.IsNullOrWhiteSpace(item.Label) && dxSmallFormat != null)
			{
				float labelY = item.Direction == Direction.Bullish ? bottom - TextSize - 3 : top + 1;
				RenderTarget.DrawText(item.Label, dxSmallFormat,
					new RectangleF(Math.Max(startX, endX - 130f), labelY, 128f, TextSize + 5),
					borderBrush);
			}
			fillBrush.Opacity = priorFillOpacity;
			borderBrush.Opacity = priorBorderOpacity;
		}

		private void RenderLine(LineRenderItem item, ChartControl chartControl, ChartScale chartScale)
		{
			if (item == null || item.EndBar < ChartBars.FromIndex || item.StartBar > ChartBars.ToIndex)
				return;
			int startBar = Math.Max(item.StartBar, ChartBars.FromIndex);
			int endBar = Math.Min(item.EndBar, ChartBars.ToIndex);
			float x1 = chartControl.GetXByBarIndex(ChartBars, startBar);
			float x2 = chartControl.GetXByBarIndex(ChartBars, endBar);
			float y = chartScale.GetYByValue(item.Price);
			DxSolidBrush brush = item.Direction == Direction.Bullish ? dxBrushes[BrushBull]
				: item.Direction == Direction.Bearish ? dxBrushes[BrushBear] : dxBrushes[BrushStructure];
			RenderTarget.DrawLine(new Vector2(x1, y), new Vector2(x2, y), brush, item.Width, GetStroke(item.DashStyle));
			if (!string.IsNullOrWhiteSpace(item.Label) && dxSmallFormat != null)
				RenderTarget.DrawText(item.Label, dxSmallFormat,
					new RectangleF(Math.Max(x1, x2 - 100f), y - TextSize - 2, 98f, TextSize + 4), brush);
		}

		private void RenderLabel(LabelRenderItem item, ChartControl chartControl, ChartScale chartScale)
		{
			if (item == null || item.BarIndex < ChartBars.FromIndex || item.BarIndex > ChartBars.ToIndex || dxSmallFormat == null)
				return;
			float x = chartControl.GetXByBarIndex(ChartBars, item.BarIndex);
			float y = chartScale.GetYByValue(item.Price);
			DxSolidBrush brush = item.Type == LabelVisualType.Bullish ? dxBrushes[BrushBull]
				: item.Type == LabelVisualType.Bearish ? dxBrushes[BrushBear]
				: item.Type == LabelVisualType.Structure ? dxBrushes[BrushStructure] : dxBrushes[BrushNeutral];
			float top = (item.Above ? y - TextSize - 4 : y + 2) + item.PixelOffsetY;
			DxTextFormat format = item.Centered && dxCenteredSmallFormat != null ? dxCenteredSmallFormat : dxSmallFormat;
			float width = item.Centered ? 180f : 150f;
			float left = item.Centered ? x - width * 0.5f : x + 3f;
			RenderTarget.DrawText(item.Text, format, new RectangleF(left, top, width, TextSize + 6), brush);
		}

		private void RenderDiagnostics(string text)
		{
			if (dxSmallFormat == null)
				return;
			RectangleF rectangle = new RectangleF(ChartPanel.X + 8, ChartPanel.Y + 8, 330, 64);
			float prior = dxBrushes[BrushPanel].Opacity;
			dxBrushes[BrushPanel].Opacity = 0.72f;
			RenderTarget.FillRectangle(rectangle, dxBrushes[BrushPanel]);
			dxBrushes[BrushPanel].Opacity = prior;
			RenderTarget.DrawText(text, dxSmallFormat, rectangle, dxBrushes[BrushText]);
		}

		private DxSolidBrush GetZoneFillBrush(ZoneRenderItem item)
		{
			bool bullish = item.Direction == Direction.Bullish;
			switch (item.Type)
			{
				case ZoneVisualType.Ifvg:
				case ZoneVisualType.IfvgFilled:
					return dxBrushes[bullish ? BrushIfvgBullFill : BrushIfvgBearFill];
				case ZoneVisualType.VolumeImbalance:
					return dxBrushes[bullish ? BrushViBullFill : BrushViBearFill];
				case ZoneVisualType.Rejection:
					return dxBrushes[bullish ? BrushRejectionBullFill : BrushRejectionBearFill];
				case ZoneVisualType.StructuralOb:
					return dxBrushes[bullish ? BrushStructuralObBullFill : BrushStructuralObBearFill];
				case ZoneVisualType.ContinuationOb:
					return dxBrushes[bullish ? BrushContinuationObBullFill : BrushContinuationObBearFill];
				case ZoneVisualType.Propulsion:
					return dxBrushes[bullish ? BrushPropulsionBullFill : BrushPropulsionBearFill];
				default:
					return dxBrushes[bullish ? BrushFvgBullFill : BrushFvgBearFill];
			}
		}

		private DxSolidBrush GetZoneBorderBrush(ZoneRenderItem item)
		{
			bool bullish = item.Direction == Direction.Bullish;
			switch (item.Type)
			{
				case ZoneVisualType.Ifvg:
				case ZoneVisualType.IfvgFilled:
					return dxBrushes[bullish ? BrushIfvgBullBorder : BrushIfvgBearBorder];
				case ZoneVisualType.VolumeImbalance:
					return dxBrushes[bullish ? BrushViBullBorder : BrushViBearBorder];
				case ZoneVisualType.Rejection:
					return dxBrushes[bullish ? BrushRejectionBullBorder : BrushRejectionBearBorder];
				case ZoneVisualType.StructuralOb:
					return dxBrushes[bullish ? BrushStructuralObBullBorder : BrushStructuralObBearBorder];
				case ZoneVisualType.ContinuationOb:
					return dxBrushes[bullish ? BrushContinuationObBullBorder : BrushContinuationObBearBorder];
				case ZoneVisualType.Propulsion:
					return dxBrushes[bullish ? BrushPropulsionBullBorder : BrushPropulsionBearBorder];
				default:
					return dxBrushes[bullish ? BrushFvgBullBorder : BrushFvgBearBorder];
			}
		}

		private void EnsureDxResources()
		{
			if (dxValid && dxRenderTarget == RenderTarget.NativePointer)
				return;
			DisposeDx();
			if (RenderTarget == null)
				return;
			try
			{
				WpfBrush[] sources = {
					BullishColor, BearishColor, StructureColor, HighlightColor, TextColor,
					WpfBrushes.DimGray, WpfBrushes.Black,
					BullishFvgFillColor, BearishFvgFillColor, BullishFvgBorderColor, BearishFvgBorderColor,
					BullishIfvgFillColor, BearishIfvgFillColor, BullishIfvgBorderColor, BearishIfvgBorderColor,
					BullishVolumeImbalanceFillColor, BearishVolumeImbalanceFillColor,
					BullishVolumeImbalanceBorderColor, BearishVolumeImbalanceBorderColor,
					BullishRejectionBlockFillColor, BearishRejectionBlockFillColor,
					BullishRejectionBlockBorderColor, BearishRejectionBlockBorderColor,
					BullishStructuralObFillColor, BearishStructuralObFillColor,
					BullishStructuralObBorderColor, BearishStructuralObBorderColor,
					BullishContinuationObFillColor, BearishContinuationObFillColor,
					BullishContinuationObBorderColor, BearishContinuationObBorderColor,
					BullishPropulsionBlockFillColor, BearishPropulsionBlockFillColor,
					BullishPropulsionBlockBorderColor, BearishPropulsionBlockBorderColor
				};
				dxBrushes = new DxSolidBrush[BrushCount];
				for (int i = 0; i < BrushCount; i++)
					dxBrushes[i] = new DxSolidBrush(RenderTarget, ToColor4(sources[i]));
				dxStrokes = new StrokeStyle[4];
				dxStrokes[0] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Solid });
				dxStrokes[1] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dash });
				dxStrokes[2] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dot });
				dxStrokes[3] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.DashDot });
				dxTextFormat = new DxTextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, TextFontName,
					FontWeight.SemiBold, SharpDX.DirectWrite.FontStyle.Normal, TextSize);
				dxSmallFormat = new DxTextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, TextFontName,
					FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, Math.Max(8, TextSize - 1))
				{
					WordWrapping = WordWrapping.NoWrap,
					ParagraphAlignment = ParagraphAlignment.Near,
					TextAlignment = TextAlignment.Leading
				};
				dxCenteredSmallFormat = new DxTextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, TextFontName,
					FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, Math.Max(8, TextSize - 1))
				{
					WordWrapping = WordWrapping.NoWrap,
					ParagraphAlignment = ParagraphAlignment.Near,
					TextAlignment = TextAlignment.Center
				};
				dxRenderTarget = RenderTarget.NativePointer;
				dxValid = true;
			}
			catch
			{
				dxValid = false;
			}
		}

		private StrokeStyle GetStroke(OrcaPriceActionDashStyle style)
		{
			int index = Math.Max(0, Math.Min(3, (int)style));
			return dxStrokes != null && index < dxStrokes.Length ? dxStrokes[index] : null;
		}

		private Color4 ToColor4(WpfBrush brush)
		{
			WpfSolidColorBrush solid = brush as WpfSolidColorBrush;
			System.Windows.Media.Color color = solid == null ? WpfColors.White : solid.Color;
			return new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
		}

		private void DisposeDx()
		{
			dxValid = false;
			dxRenderTarget = IntPtr.Zero;
			if (dxBrushes != null)
				for (int i = 0; i < dxBrushes.Length; i++) if (dxBrushes[i] != null) dxBrushes[i].Dispose();
			if (dxStrokes != null)
				for (int i = 0; i < dxStrokes.Length; i++) if (dxStrokes[i] != null) dxStrokes[i].Dispose();
			if (dxTextFormat != null) dxTextFormat.Dispose();
			if (dxSmallFormat != null) dxSmallFormat.Dispose();
			if (dxCenteredSmallFormat != null) dxCenteredSmallFormat.Dispose();
			dxBrushes = null;
			dxStrokes = null;
			dxTextFormat = null;
			dxSmallFormat = null;
			dxCenteredSmallFormat = null;
		}
		#endregion

		#region Bar access and utility
		private int BarsAgo(int absoluteBar)
		{
			return CurrentBar - absoluteBar;
		}

		private bool CanReadBar(int absoluteBar)
		{
			int barsAgo = BarsAgo(absoluteBar);
			return absoluteBar >= 0 && barsAgo >= 0 && barsAgo <= CurrentBar;
		}

		private double GetOpenAtBar(int absoluteBar) { return Open[BarsAgo(absoluteBar)]; }
		private double GetHighAtBar(int absoluteBar) { return High[BarsAgo(absoluteBar)]; }
		private double GetLowAtBar(int absoluteBar) { return Low[BarsAgo(absoluteBar)]; }
		private double GetCloseAtBar(int absoluteBar) { return Close[BarsAgo(absoluteBar)]; }
		private DateTime GetTimeAtBar(int absoluteBar) { return Time[BarsAgo(absoluteBar)]; }

		private double GetAtrAtBar(int absoluteBar)
		{
			if (atr == null || !CanReadBar(absoluteBar))
				return Math.Max(TickSize, 1e-8);
			double value = atr[BarsAgo(absoluteBar)];
			return IsFinite(value) && value > 0 ? value : Math.Max(TickSize, 1e-8);
		}

		private string NextId(string prefix)
		{
			return prefix + "-" + (nextSequence++).ToString(CultureInfo.InvariantCulture);
		}

		private static bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		private static bool IsTerminal(ZoneState state)
		{
			return state == ZoneState.Completed || state == ZoneState.Failed
				|| state == ZoneState.Expired || state == ZoneState.Invalidated;
		}

		private static double Clamp(double value, double minimum, double maximum)
		{
			return Math.Max(minimum, Math.Min(maximum, value));
		}

		internal static int ClassifyStandardFvg(double firstHigh, double firstLow, double thirdHigh, double thirdLow)
		{
			if (thirdLow > firstHigh)
				return 1;
			if (thirdHigh < firstLow)
				return -1;
			return 0;
		}

		internal static int CapFvgEndBar(int confirmationBar, int requestedEndBar, int extensionBars)
		{
			int safeConfirmation = Math.Max(0, confirmationBar);
			int safeRequested = Math.Max(safeConfirmation, requestedEndBar);
			long maximumEnd = (long)safeConfirmation + Math.Max(1, extensionBars);
			return (int)Math.Min(safeRequested, Math.Min(int.MaxValue, maximumEnd));
		}

		internal static int ClassifyVolumeImbalance(
			double previousOpen, double previousClose, double previousHigh, double previousLow,
			double currentOpen, double currentClose, double currentHigh, double currentLow, bool advanced)
		{
			int previousDirection = previousClose > previousOpen ? 1 : previousClose < previousOpen ? -1 : 0;
			int currentDirection = currentClose > currentOpen ? 1 : currentClose < currentOpen ? -1 : 0;
			bool allowBull = advanced ? currentDirection == 1 : previousDirection == 1 && currentDirection == 1;
			bool allowBear = advanced ? currentDirection == -1 : previousDirection == -1 && currentDirection == -1;
			if (allowBull && currentOpen > previousClose && currentLow <= previousHigh)
				return 1;
			if (allowBear && currentOpen < previousClose && currentHigh >= previousLow)
				return -1;
			return 0;
		}

		internal static DateTime GetClockBucketStart(DateTime clock, int periodMinutes)
		{
			int safeMinutes = Math.Max(1, periodMinutes);
			int minuteOfDay = clock.Hour * 60 + clock.Minute;
			return clock.Date.AddMinutes(minuteOfDay - minuteOfDay % safeMinutes);
		}

		internal static bool IsStrictBreak(double close, double level, double buffer, int direction)
		{
			return direction > 0 ? close > level + Math.Max(0, buffer)
				: direction < 0 && close < level - Math.Max(0, buffer);
		}

		internal static bool IsSameSweepEpisode(int previousBar, int currentBar, int windowBars)
		{
			return previousBar >= 0 && currentBar >= previousBar
				&& currentBar - previousBar <= Math.Max(1, windowBars);
		}

		internal static bool IsSweepPivotEligible(OrcaPriceActionSweepQuality quality,
			OrcaPriceActionPivotImportance importance, bool isExternal, bool isProtected,
			bool isSponsor, bool isActiveTarget)
		{
			if (quality == OrcaPriceActionSweepQuality.AllConfirmedPivots)
				return true;
			if (quality == OrcaPriceActionSweepQuality.MajorOnly)
				return isProtected || isSponsor
					|| (isExternal && importance == OrcaPriceActionPivotImportance.Major);
			return importance >= OrcaPriceActionPivotImportance.Standard
				&& (isExternal || isProtected || isSponsor || isActiveTarget);
		}

		internal static bool IsSweepTimingEligible(int confirmationBar, int lastVisibleSweepBar,
			int currentBar, int minimumRestingBars, int episodeWindowBars)
		{
			if (currentBar <= confirmationBar
				|| currentBar - confirmationBar < Math.Max(0, minimumRestingBars))
				return false;
			return lastVisibleSweepBar < 0
				|| (currentBar > lastVisibleSweepBar
					&& currentBar - lastVisibleSweepBar <= Math.Max(1, episodeWindowBars));
		}

		internal static bool IsSweepPriceAction(double extreme, double close, double level,
			double minimumPenetration, int direction)
		{
			double penetration = Math.Max(0, minimumPenetration);
			if (direction < 0)
				return extreme > level && extreme + 1e-10 >= level + penetration && close <= level;
			return direction > 0 && extreme < level
				&& extreme - 1e-10 <= level - penetration && close >= level;
		}

		private Direction CandleDirection(int barIndex)
		{
			double open = GetOpenAtBar(barIndex);
			double close = GetCloseAtBar(barIndex);
			return close > open ? Direction.Bullish : close < open ? Direction.Bearish : Direction.None;
		}

		private PivotModel FindPivot(string id)
		{
			return string.IsNullOrEmpty(id) ? null : pivots.FirstOrDefault(p => p.Id == id);
		}

		private BlockModel FindBlock(string id)
		{
			return string.IsNullOrEmpty(id) ? null : blocks.FirstOrDefault(b => b.Id == id);
		}

		private TimeZoneInfo FindEasternTimeZone()
		{
			try { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
			catch { return TimeZoneInfo.Local; }
		}

		private DateTime ToEastern(DateTime value)
		{
			try
			{
				TimeZoneInfo target = easternTimeZone ?? TimeZoneInfo.Local;
				if (value.Kind == DateTimeKind.Utc)
					return TimeZoneInfo.ConvertTimeFromUtc(value, target);
				return TimeZoneInfo.ConvertTime(value, TimeZoneInfo.Local, target);
			}
			catch { return value; }
		}

		private void RegisterDiagnostics()
		{
			diagnosticsInstanceId = "OrcaPriceAction-" + GetHashCode().ToString(CultureInfo.InvariantCulture);
			OrcaDiagnosticsCore.RegisterInstance(diagnosticsInstanceId, "OrcaPriceAction", this);
			OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsInstanceId, "PrimaryChartOHLC", "Unknown", string.Empty);
			OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 0, "PrimaryChartSeries", "Chart", "Completed-bar price-action state and intrabar FVG fill");
		}

		private void ReportDiagnosticsState()
		{
			if (!string.IsNullOrEmpty(diagnosticsInstanceId))
				OrcaDiagnosticsCore.ReportState(diagnosticsInstanceId, State.ToString());
		}
		#endregion

		#region Display preset behavior
		private void ApplyDisplayPreset(OrcaPriceActionDisplayPreset preset)
		{
			applyingDisplayPreset = true;
			try
			{
				displayPreset = preset;
				showFvg = true;
				showStructure = true;
				showIfvg = preset == OrcaPriceActionDisplayPreset.FullContext;
				showVolumeImbalance = preset == OrcaPriceActionDisplayPreset.FullContext;
				showRejectionBlocks = preset == OrcaPriceActionDisplayPreset.BlocksFocused
					|| preset == OrcaPriceActionDisplayPreset.FullContext;
				showStructuralOrderBlocks = preset == OrcaPriceActionDisplayPreset.BlocksFocused
					|| preset == OrcaPriceActionDisplayPreset.FullContext;
				showContinuationOrderBlocks = preset == OrcaPriceActionDisplayPreset.FullContext;
				showPropulsionBlocks = preset == OrcaPriceActionDisplayPreset.FullContext;
				showTimedFirstFvg = preset == OrcaPriceActionDisplayPreset.FullContext;
				showFirstRthFvg = preset == OrcaPriceActionDisplayPreset.FullContext;
			}
			finally
			{
				applyingDisplayPreset = false;
			}
		}

		private void SetVisibility(ref bool field, bool value)
		{
			if (field == value)
				return;
			field = value;
			if (!applyingDisplayPreset)
				displayPreset = OrcaPriceActionDisplayPreset.Custom;
		}
		#endregion

		#region Properties - General and visibility
		[NinjaScriptProperty]
		[Display(Name = "Display Preset", Order = 1, GroupName = "01. General")]
		public OrcaPriceActionDisplayPreset DisplayPreset
		{
			get { return displayPreset; }
			set
			{
				if (value == OrcaPriceActionDisplayPreset.Custom)
					displayPreset = value;
				else
					ApplyDisplayPreset(value);
			}
		}

		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "ATR Period", Order = 2, GroupName = "01. General")]
		public int AtrPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(100, 50000)]
		[Display(Name = "Maximum Historical Bars", Order = 3, GroupName = "01. General")]
		public int MaximumHistoricalBars { get; set; }

		[NinjaScriptProperty]
		[Range(10, 2000)]
		[Display(Name = "Maximum Terminal Records", Order = 4, GroupName = "01. General")]
		public int MaximumTerminalRecords { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Candidates", Order = 5, GroupName = "01. General")]
		public bool ShowCandidates { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Completed/Invalidated", Order = 6, GroupName = "01. General")]
		public bool ShowCompletedZones { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Weak Blocks", Order = 7, GroupName = "01. General")]
		public bool ShowWeakBlocks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show FVG", Order = 1, GroupName = "02. Visibility")]
		public bool ShowFvg { get { return showFvg; } set { SetVisibility(ref showFvg, value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show iFVG", Order = 2, GroupName = "02. Visibility")]
		public bool ShowIfvg { get { return showIfvg; } set { SetVisibility(ref showIfvg, value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Volume Imbalance", Order = 3, GroupName = "02. Visibility")]
		public bool ShowVolumeImbalance { get { return showVolumeImbalance; } set { SetVisibility(ref showVolumeImbalance, value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Structure", Order = 4, GroupName = "02. Visibility")]
		public bool ShowStructure { get { return showStructure; } set { SetVisibility(ref showStructure, value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Rejection Blocks", Order = 5, GroupName = "02. Visibility")]
		public bool ShowRejectionBlocks { get { return showRejectionBlocks; } set { SetVisibility(ref showRejectionBlocks, value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Structural OB", Order = 6, GroupName = "02. Visibility")]
		public bool ShowStructuralOrderBlocks { get { return showStructuralOrderBlocks; } set { SetVisibility(ref showStructuralOrderBlocks, value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Continuation OB", Order = 7, GroupName = "02. Visibility")]
		public bool ShowContinuationOrderBlocks { get { return showContinuationOrderBlocks; } set { SetVisibility(ref showContinuationOrderBlocks, value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Propulsion Blocks", Order = 8, GroupName = "02. Visibility")]
		public bool ShowPropulsionBlocks { get { return showPropulsionBlocks; } set { SetVisibility(ref showPropulsionBlocks, value); } }
		#endregion

		#region Properties - FVG and timed FVG
		[NinjaScriptProperty]
		[Display(Name = "Use Minimum Size", Order = 1, GroupName = "03. Fair Value Gaps")]
		public bool UseMinimumFvgSize { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100000.0)]
		[Display(Name = "Minimum Size (Points)", Order = 2, GroupName = "03. Fair Value Gaps")]
		public double MinimumFvgSizePoints { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Directional Middle Candle", Order = 3, GroupName = "03. Fair Value Gaps")]
		public bool RequireDirectionalMiddleCandle { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require ATR Displacement", Order = 4, GroupName = "03. Fair Value Gaps")]
		public bool RequireFvgDisplacement { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 20.0)]
		[Display(Name = "Displacement ATR", Order = 5, GroupName = "03. Fair Value Gaps")]
		public double FvgDisplacementAtr { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Fill Display", Order = 6, GroupName = "03. Fair Value Gaps")]
		public OrcaPriceActionFvgFillMode FvgFillMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable iFVG Conversion", Order = 7, GroupName = "03. Fair Value Gaps")]
		public bool EnableIfvgConversion { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10000)]
		[Display(Name = "Extension Bars", Order = 8, GroupName = "03. Fair Value Gaps")]
		public int FvgExtensionBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show First Period FVG", Order = 1, GroupName = "04. Timed FVG")]
		public bool ShowTimedFirstFvg { get { return showTimedFirstFvg; } set { SetVisibility(ref showTimedFirstFvg, value); } }

		[NinjaScriptProperty]
		[Display(Name = "Period", Order = 2, GroupName = "04. Timed FVG")]
		public OrcaPriceActionTimedPeriod TimedFvgPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Extension", Order = 3, GroupName = "04. Timed FVG")]
		public OrcaPriceActionTimedExtension TimedFvgExtension { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show First RTH FVG", Order = 4, GroupName = "04. Timed FVG")]
		public bool ShowFirstRthFvg { get { return showFirstRthFvg; } set { SetVisibility(ref showFirstRthFvg, value); } }

		[NinjaScriptProperty]
		[Display(Name = "RTH Open (New York)", Order = 5, GroupName = "04. Timed FVG")]
		public TimeSpan RthOpen { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "RTH Close (New York)", Order = 6, GroupName = "04. Timed FVG")]
		public TimeSpan RthClose { get; set; }
		#endregion

		#region Properties - VI and structure
		[NinjaScriptProperty]
		[Display(Name = "Detection Mode", Order = 1, GroupName = "05. Volume Imbalance")]
		public OrcaPriceActionVolumeImbalanceMode VolumeImbalanceMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Pivot Strength", Order = 1, GroupName = "06. Market Structure")]
		public int PivotStrength { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Break Buffer (Ticks)", Order = 2, GroupName = "06. Market Structure")]
		public int StructureBreakBufferTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show BOS", Order = 3, GroupName = "06. Market Structure")]
		public bool ShowBos { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show CHoCH/MSS", Order = 4, GroupName = "06. Market Structure")]
		public bool ShowChoch { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Liquidity Sweeps", Order = 5, GroupName = "06. Market Structure")]
		public bool ShowLiquiditySweeps { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Sweep Quality", Order = 6, GroupName = "06. Market Structure")]
		public OrcaPriceActionSweepQuality SweepQuality { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Minimum Sweep Penetration (Ticks)", Order = 7, GroupName = "06. Market Structure")]
		public int MinimumSweepPenetrationTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Minimum Resting Bars", Order = 8, GroupName = "06. Market Structure")]
		public int MinimumSweepRestingBars { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Sweep Label Window Bars", Order = 9, GroupName = "06. Market Structure")]
		public int SweepLabelWindowBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Protected Levels", Order = 10, GroupName = "06. Market Structure")]
		public bool ShowProtectedLevels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Detailed Role Badges", Order = 11, GroupName = "06. Market Structure")]
		public bool ShowDetailedRoleBadges { get; set; }
		#endregion

		#region Properties - Rejection blocks
		[NinjaScriptProperty]
		[Display(Name = "Preset", Order = 1, GroupName = "07. Rejection Blocks")]
		public OrcaPriceActionRejectionPreset RejectionPreset { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Liquidity Scope", Order = 2, GroupName = "07. Rejection Blocks")]
		public OrcaPriceActionLiquidityScope RejectionLiquidityScope { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Minimum Pivot Importance", Order = 3, GroupName = "07. Rejection Blocks")]
		public OrcaPriceActionPivotImportance MinimumRejectionPivotImportance { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Confirmation", Order = 4, GroupName = "07. Rejection Blocks")]
		public OrcaPriceActionRejectionConfirmation RejectionConfirmation { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Confirmation Window", Order = 5, GroupName = "07. Rejection Blocks")]
		public int RejectionConfirmationBars { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 100.0)]
		[Display(Name = "Minimum Wick %", Order = 6, GroupName = "07. Rejection Blocks")]
		public double MinimumRejectionWickPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Minimum Sweep (Ticks)", Order = 7, GroupName = "07. Rejection Blocks")]
		public int RejectionSweepTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Standalone Rejection Candles", Order = 8, GroupName = "07. Rejection Blocks")]
		public bool ShowStandaloneRejectionCandles { get; set; }
		#endregion

		#region Properties - Order blocks
		[NinjaScriptProperty]
		[Display(Name = "Preset", Order = 1, GroupName = "08. Order Blocks")]
		public OrcaPriceActionOrderBlockPreset OrderBlockPreset { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Display", Order = 2, GroupName = "08. Order Blocks")]
		public OrcaPriceActionOrderBlockDisplay OrderBlockDisplay { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Invalidation", Order = 3, GroupName = "08. Order Blocks")]
		public OrcaPriceActionBlockInvalidation OrderBlockInvalidation { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 20.0)]
		[Display(Name = "S-OB Displacement ATR", Order = 4, GroupName = "08. Order Blocks")]
		public double StructuralObDisplacementAtr { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 20.0)]
		[Display(Name = "C-OB Displacement ATR", Order = 5, GroupName = "08. Order Blocks")]
		public double ContinuationObDisplacementAtr { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 20.0)]
		[Display(Name = "PB Displacement ATR", Order = 6, GroupName = "08. Order Blocks")]
		public double PropulsionDisplacementAtr { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "S-OB Window", Order = 7, GroupName = "08. Order Blocks")]
		public int StructuralObWindow { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "C-OB Window", Order = 8, GroupName = "08. Order Blocks")]
		public int ContinuationObWindow { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "PB Window", Order = 9, GroupName = "08. Order Blocks")]
		public int PropulsionWindow { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "S-OB FVG Mode", Order = 10, GroupName = "08. Order Blocks")]
		public OrcaPriceActionFvgConfluence StructuralObFvgMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "C-OB FVG Mode", Order = 11, GroupName = "08. Order Blocks")]
		public OrcaPriceActionFvgConfluence ContinuationObFvgMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "PB FVG Mode", Order = 12, GroupName = "08. Order Blocks")]
		public OrcaPriceActionFvgConfluence PropulsionFvgMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Break-Only Weak Blocks", Order = 13, GroupName = "08. Order Blocks")]
		public bool EnableBreakOnlyBlocks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require PB Parent Mean Hold", Order = 14, GroupName = "08. Order Blocks")]
		public bool RequirePropulsionMeanHold { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Open", Order = 15, GroupName = "08. Order Blocks")]
		public bool ShowOrderBlockOpen { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Body Midpoint", Order = 16, GroupName = "08. Order Blocks")]
		public bool ShowOrderBlockMidpoint { get; set; }
		#endregion

		#region Properties - Visuals and diagnostics
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "FVG Filled-Portion Max Opacity", Order = 1, GroupName = "09. General Rendering")]
		public int FilledZoneOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Completed-Zone Max Opacity", Order = 2, GroupName = "09. General Rendering")]
		public int CompletedZoneOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(6, 36)]
		[Display(Name = "Text Size", Order = 3, GroupName = "09. General Rendering")]
		public int TextSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Text Font", Order = 4, GroupName = "09. General Rendering")]
		public string TextFontName { get; set; }

		[XmlIgnore]
		[Display(Name = "Bullish Fill", Order = 1, GroupName = "10. FVG Style")]
		public WpfBrush BullishFvgFillColor { get; set; }
		[Browsable(false)]
		public string BullishFvgFillColorSerializable { get { return Serialize.BrushToString(BullishFvgFillColor); } set { BullishFvgFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Fill", Order = 2, GroupName = "10. FVG Style")]
		public WpfBrush BearishFvgFillColor { get; set; }
		[Browsable(false)]
		public string BearishFvgFillColorSerializable { get { return Serialize.BrushToString(BearishFvgFillColor); } set { BearishFvgFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bullish Border", Order = 3, GroupName = "10. FVG Style")]
		public WpfBrush BullishFvgBorderColor { get; set; }
		[Browsable(false)]
		public string BullishFvgBorderColorSerializable { get { return Serialize.BrushToString(BullishFvgBorderColor); } set { BullishFvgBorderColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Border", Order = 4, GroupName = "10. FVG Style")]
		public WpfBrush BearishFvgBorderColor { get; set; }
		[Browsable(false)]
		public string BearishFvgBorderColorSerializable { get { return Serialize.BrushToString(BearishFvgBorderColor); } set { BearishFvgBorderColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Opacity", Order = 5, GroupName = "10. FVG Style")]
		public int FvgOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Bullish Fill", Order = 1, GroupName = "11. iFVG Style")]
		public WpfBrush BullishIfvgFillColor { get; set; }
		[Browsable(false)]
		public string BullishIfvgFillColorSerializable { get { return Serialize.BrushToString(BullishIfvgFillColor); } set { BullishIfvgFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Fill", Order = 2, GroupName = "11. iFVG Style")]
		public WpfBrush BearishIfvgFillColor { get; set; }
		[Browsable(false)]
		public string BearishIfvgFillColorSerializable { get { return Serialize.BrushToString(BearishIfvgFillColor); } set { BearishIfvgFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bullish Border", Order = 3, GroupName = "11. iFVG Style")]
		public WpfBrush BullishIfvgBorderColor { get; set; }
		[Browsable(false)]
		public string BullishIfvgBorderColorSerializable { get { return Serialize.BrushToString(BullishIfvgBorderColor); } set { BullishIfvgBorderColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Border", Order = 4, GroupName = "11. iFVG Style")]
		public WpfBrush BearishIfvgBorderColor { get; set; }
		[Browsable(false)]
		public string BearishIfvgBorderColorSerializable { get { return Serialize.BrushToString(BearishIfvgBorderColor); } set { BearishIfvgBorderColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Opacity", Order = 5, GroupName = "11. iFVG Style")]
		public int IfvgOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Bullish Fill", Order = 1, GroupName = "12. Volume Imbalance Style")]
		public WpfBrush BullishVolumeImbalanceFillColor { get; set; }
		[Browsable(false)]
		public string BullishVolumeImbalanceFillColorSerializable { get { return Serialize.BrushToString(BullishVolumeImbalanceFillColor); } set { BullishVolumeImbalanceFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Fill", Order = 2, GroupName = "12. Volume Imbalance Style")]
		public WpfBrush BearishVolumeImbalanceFillColor { get; set; }
		[Browsable(false)]
		public string BearishVolumeImbalanceFillColorSerializable { get { return Serialize.BrushToString(BearishVolumeImbalanceFillColor); } set { BearishVolumeImbalanceFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bullish Border", Order = 3, GroupName = "12. Volume Imbalance Style")]
		public WpfBrush BullishVolumeImbalanceBorderColor { get; set; }
		[Browsable(false)]
		public string BullishVolumeImbalanceBorderColorSerializable { get { return Serialize.BrushToString(BullishVolumeImbalanceBorderColor); } set { BullishVolumeImbalanceBorderColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Border", Order = 4, GroupName = "12. Volume Imbalance Style")]
		public WpfBrush BearishVolumeImbalanceBorderColor { get; set; }
		[Browsable(false)]
		public string BearishVolumeImbalanceBorderColorSerializable { get { return Serialize.BrushToString(BearishVolumeImbalanceBorderColor); } set { BearishVolumeImbalanceBorderColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Opacity", Order = 5, GroupName = "12. Volume Imbalance Style")]
		public int VolumeImbalanceOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Bullish Fill", Order = 1, GroupName = "13. Rejection Block Style")]
		public WpfBrush BullishRejectionBlockFillColor { get; set; }
		[Browsable(false)]
		public string BullishRejectionBlockFillColorSerializable { get { return Serialize.BrushToString(BullishRejectionBlockFillColor); } set { BullishRejectionBlockFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Fill", Order = 2, GroupName = "13. Rejection Block Style")]
		public WpfBrush BearishRejectionBlockFillColor { get; set; }
		[Browsable(false)]
		public string BearishRejectionBlockFillColorSerializable { get { return Serialize.BrushToString(BearishRejectionBlockFillColor); } set { BearishRejectionBlockFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bullish Border", Order = 3, GroupName = "13. Rejection Block Style")]
		public WpfBrush BullishRejectionBlockBorderColor { get; set; }
		[Browsable(false)]
		public string BullishRejectionBlockBorderColorSerializable { get { return Serialize.BrushToString(BullishRejectionBlockBorderColor); } set { BullishRejectionBlockBorderColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Border", Order = 4, GroupName = "13. Rejection Block Style")]
		public WpfBrush BearishRejectionBlockBorderColor { get; set; }
		[Browsable(false)]
		public string BearishRejectionBlockBorderColorSerializable { get { return Serialize.BrushToString(BearishRejectionBlockBorderColor); } set { BearishRejectionBlockBorderColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Opacity", Order = 5, GroupName = "13. Rejection Block Style")]
		public int RejectionBlockOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Bullish Fill", Order = 1, GroupName = "14. Structural OB Style")]
		public WpfBrush BullishStructuralObFillColor { get; set; }
		[Browsable(false)]
		public string BullishStructuralObFillColorSerializable { get { return Serialize.BrushToString(BullishStructuralObFillColor); } set { BullishStructuralObFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Fill", Order = 2, GroupName = "14. Structural OB Style")]
		public WpfBrush BearishStructuralObFillColor { get; set; }
		[Browsable(false)]
		public string BearishStructuralObFillColorSerializable { get { return Serialize.BrushToString(BearishStructuralObFillColor); } set { BearishStructuralObFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bullish Border", Order = 3, GroupName = "14. Structural OB Style")]
		public WpfBrush BullishStructuralObBorderColor { get; set; }
		[Browsable(false)]
		public string BullishStructuralObBorderColorSerializable { get { return Serialize.BrushToString(BullishStructuralObBorderColor); } set { BullishStructuralObBorderColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Border", Order = 4, GroupName = "14. Structural OB Style")]
		public WpfBrush BearishStructuralObBorderColor { get; set; }
		[Browsable(false)]
		public string BearishStructuralObBorderColorSerializable { get { return Serialize.BrushToString(BearishStructuralObBorderColor); } set { BearishStructuralObBorderColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Opacity", Order = 5, GroupName = "14. Structural OB Style")]
		public int StructuralObOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Bullish Fill", Order = 1, GroupName = "15. Continuation OB Style")]
		public WpfBrush BullishContinuationObFillColor { get; set; }
		[Browsable(false)]
		public string BullishContinuationObFillColorSerializable { get { return Serialize.BrushToString(BullishContinuationObFillColor); } set { BullishContinuationObFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Fill", Order = 2, GroupName = "15. Continuation OB Style")]
		public WpfBrush BearishContinuationObFillColor { get; set; }
		[Browsable(false)]
		public string BearishContinuationObFillColorSerializable { get { return Serialize.BrushToString(BearishContinuationObFillColor); } set { BearishContinuationObFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bullish Border", Order = 3, GroupName = "15. Continuation OB Style")]
		public WpfBrush BullishContinuationObBorderColor { get; set; }
		[Browsable(false)]
		public string BullishContinuationObBorderColorSerializable { get { return Serialize.BrushToString(BullishContinuationObBorderColor); } set { BullishContinuationObBorderColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Border", Order = 4, GroupName = "15. Continuation OB Style")]
		public WpfBrush BearishContinuationObBorderColor { get; set; }
		[Browsable(false)]
		public string BearishContinuationObBorderColorSerializable { get { return Serialize.BrushToString(BearishContinuationObBorderColor); } set { BearishContinuationObBorderColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Opacity", Order = 5, GroupName = "15. Continuation OB Style")]
		public int ContinuationObOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Bullish Fill", Order = 1, GroupName = "16. Propulsion Block Style")]
		public WpfBrush BullishPropulsionBlockFillColor { get; set; }
		[Browsable(false)]
		public string BullishPropulsionBlockFillColorSerializable { get { return Serialize.BrushToString(BullishPropulsionBlockFillColor); } set { BullishPropulsionBlockFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Fill", Order = 2, GroupName = "16. Propulsion Block Style")]
		public WpfBrush BearishPropulsionBlockFillColor { get; set; }
		[Browsable(false)]
		public string BearishPropulsionBlockFillColorSerializable { get { return Serialize.BrushToString(BearishPropulsionBlockFillColor); } set { BearishPropulsionBlockFillColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bullish Border", Order = 3, GroupName = "16. Propulsion Block Style")]
		public WpfBrush BullishPropulsionBlockBorderColor { get; set; }
		[Browsable(false)]
		public string BullishPropulsionBlockBorderColorSerializable { get { return Serialize.BrushToString(BullishPropulsionBlockBorderColor); } set { BullishPropulsionBlockBorderColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Border", Order = 4, GroupName = "16. Propulsion Block Style")]
		public WpfBrush BearishPropulsionBlockBorderColor { get; set; }
		[Browsable(false)]
		public string BearishPropulsionBlockBorderColorSerializable { get { return Serialize.BrushToString(BearishPropulsionBlockBorderColor); } set { BearishPropulsionBlockBorderColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Opacity", Order = 5, GroupName = "16. Propulsion Block Style")]
		public int PropulsionBlockOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Bullish Event Color", Order = 1, GroupName = "17. Structure and Text")]
		public WpfBrush BullishColor { get; set; }
		[Browsable(false)]
		public string BullishColorSerializable { get { return Serialize.BrushToString(BullishColor); } set { BullishColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Event Color", Order = 2, GroupName = "17. Structure and Text")]
		public WpfBrush BearishColor { get; set; }
		[Browsable(false)]
		public string BearishColorSerializable { get { return Serialize.BrushToString(BearishColor); } set { BearishColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Pivot / Scope Color", Order = 3, GroupName = "17. Structure and Text")]
		public WpfBrush StructureColor { get; set; }
		[Browsable(false)]
		public string StructureColorSerializable { get { return Serialize.BrushToString(StructureColor); } set { StructureColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Timed Highlight Border", Order = 4, GroupName = "17. Structure and Text")]
		public WpfBrush HighlightColor { get; set; }
		[Browsable(false)]
		public string HighlightColorSerializable { get { return Serialize.BrushToString(HighlightColor); } set { HighlightColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Text Color", Order = 5, GroupName = "17. Structure and Text")]
		public WpfBrush TextColor { get; set; }
		[Browsable(false)]
		public string TextColorSerializable { get { return Serialize.BrushToString(TextColor); } set { TextColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Diagnostics Panel", Order = 1, GroupName = "18. Diagnostics")]
		public bool ShowDiagnosticsPanel { get; set; }
		#endregion
	}
}
