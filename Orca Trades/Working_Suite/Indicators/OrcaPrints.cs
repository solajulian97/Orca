#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Windows.Media;
using System.Xml.Serialization;

using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;

using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class OrcaPrints : Indicator, IOrcaReplayParticipant
	{
		private readonly Guid sharedProfileSourceId = Guid.NewGuid();
		private readonly object sharedProfileSync = new object();
		private List<OrcaPrintTick> tickBuffer;
		private List<PrintEvent> printEvents;
		private Dictionary<string, DateTime> clusterCooldowns;
		private Dictionary<double, PriceLevelAccumulator> priceLevelAccumulators;
		private Dictionary<long, HighDeltaWindowAccumulator> highDeltaWindowAccumulators;
		private List<Dictionary<double, long>> sharedProfileVolumeMaps;
		private List<Dictionary<double, long>> sharedProfileUpVolumeMaps;
		private List<Dictionary<double, long>> sharedProfileDownVolumeMaps;
		private ReaderWriterLockSlim printLock;
		private double currentBid = double.NaN;
		private double currentAsk = double.NaN;
		private double sharedProfilePrevLast = double.NaN;
		private int lastSessionResetBar = -1;
		private int priceLevelAccumulatorBarIndex = -1;
		private int sharedProfileLastDirection;
		private int sharedProfileRevision;
		private int sharedProfileCoverageBarCount;
		private DateTime lastSharedProfileRegistrationUtc = DateTime.MinValue;
		private DateTime sharedProfileLastUpdatedUtc = DateTime.MinValue;
		private readonly string diagnosticsInstanceId = Guid.NewGuid().ToString("N");
		private bool diagnosticsRegistered;
		private OrcaReplayBarHorizon replayBarHorizon;
		private TimeZoneInfo orcaPrintChartTimeZone;
		private TimeZoneInfo orcaPrintEasternTimeZone;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaPrints";
				Description = "Detects standalone large prints and clustered aggressive participation. Tick Replay must be enabled on the data series for accurate historical replay.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = true;

				MinTradeSize = 5;
				ResetOnNewSession = true;
				PublishSharedProfileCache = true;
				ExcludeMocPrints = false;
				MocStartTimeEt = new TimeSpan(15, 50, 0);
				MocEndTimeEt = new TimeSpan(16, 0, 0);

				EnableSinglePrints = true;
				SinglePrintMinSize = 100;
				ShowSinglePrintSizeText = false;
				SinglePrintTextFontSize = 9.0f;
				SinglePrintTextFontFamily = "Figtree";
				HideSinglePrintsMatchingClusters = true;
				AggregateSinglePrintsWhenCompact = true;
				CompactAggregatePriceRangeTicks = 4;
				CompactAggregateMinPrints = 3;

				EnableClusters = true;
				ClusterTimeWindowSec = 3.0;
				ClusterMinVolume = 500;
				ClusterMaxPriceTicks = 4;
				MinAggressorPercent = 70;
				ClusterCooldownSec = 1.0;
				AggregateClustersWhenCompact = true;
				CompactClusterAggregatePriceRangeTicks = 4;
				CompactClusterAggregateMinClusters = 3;

				EnablePriceLevelAccumulation = false;
				PriceLevelHighlightMode = NinjaTrader.NinjaScript.Indicators.PriceLevelHighlightMode.VolumeAndDominance;
				PriceLevelMinVolume = 75;
				PriceLevelRequireMinDominance = true;
				PriceLevelMinDominancePercent = 60;
				PriceLevelMinDelta = 200;
				PriceLevelDeltaDirection = NinjaTrader.NinjaScript.Indicators.PriceLevelDeltaDirection.Both;
				PriceLevelDeltaWindowTicks = 4;

				ParentConfidenceMode = NinjaTrader.NinjaScript.Indicators.ParentConfidenceMode.Score;
				MinParentConfidence = 60;
				WeightAggressorConsistency = 0.30;
				WeightSizeUniformity = 0.25;
				WeightPriceTightness = 0.20;
				WeightTimingRegularity = 0.25;

				MinDotSize = 6;
				MaxDotSize = 30;
				SinglePrintMinDotSize = 7;
				SinglePrintMaxDotSize = 22;
				PriceLevelMinDotSize = 12;
				PriceLevelMaxDotSize = 26;
				ClusterMinDotSize = 18;
				ClusterMaxDotSize = 34;
				DotSizeScale = NinjaTrader.NinjaScript.Indicators.DotSizeScale.Logarithmic;
				BuyAggressorColor = WpfBrushes.LimeGreen;
				SellAggressorColor = WpfBrushes.OrangeRed;
				PriceLevelBuyColor = WpfBrushes.DeepSkyBlue;
				PriceLevelSellColor = WpfBrushes.Magenta;
				UseVariableIntensity = true;
				MinIntensityPct = 35;
				BorderEnabled = true;
				BorderColor = WpfBrushes.Black;
				TransparencyPct = 20;
				ShapeMode = NinjaTrader.NinjaScript.Indicators.ShapeMode.DistinguishClusters;
				HorizontalAnchor = OrcaPrintHorizontalAnchor.ExactPrintTime;
				HorizontalOffsetPx = 0;
				AutoCompactLayout = true;
				CompactLayoutEnterSpacingPx = 36;
				DetailLayoutEnterSpacingPx = 46;
				HidePrintsWhenZoomedOut = false;
				MinimumBarSpacingToShowPrintsPx = 4;
			}
			else if (State == State.DataLoaded)
			{
				InitializeOrcaPrintTimeZones();
				InitializeOrcaPrintsEngine();
				InitializeOrcaPrintsRendering();
				replayBarHorizon = new OrcaReplayBarHorizon("OrcaPrints:" + diagnosticsInstanceId);
				RegisterSharedProfileSource(true);
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				ReportDiagnosticsState();
			}
			else if (State == State.Historical)
			{
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				AttachOrcaPrintsMouseHandlers();
				ReportDiagnosticsState();
			}
			else if (State == State.Transition || State == State.Realtime)
			{
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				ReportDiagnosticsState();
			}
			else if (State == State.Terminated)
			{
				if (ChartControl != null) OrcaReplayCore.UnregisterParticipant(ChartControl, this);
				if (replayBarHorizon != null) replayBarHorizon.Restore();
				OrcaDiagnosticsCore.UnregisterInstance(diagnosticsInstanceId);
				DetachOrcaPrintsMouseHandlers();
				TerminateOrcaPrintsEngine();
				DisposeDxBrushCache();
			}
		}

		protected override void OnBarUpdate()
		{
			long diagnosticsWorkStart = 0;
			int diagnosticsBarsInProgress = BarsInProgress;
			if (OrcaDiagnosticsCore.IsEnabled)
			{
				EnsureDiagnosticsRegistered();
				long diagnosticsSequence = OrcaDiagnosticsCore.ReportBarUpdate(diagnosticsInstanceId, BarsInProgress, GetDiagnosticsEventTime());
				diagnosticsWorkStart = OrcaDiagnosticsCore.BeginWorkSample(diagnosticsSequence);
			}
			try
			{

			if (CurrentBar < 0 || Bars == null)
				return;

			if (ResetOnNewSession && Bars.IsFirstBarOfSession && lastSessionResetBar != CurrentBar)
			{
				lastSessionResetBar = CurrentBar;
				ClearOrcaPrintsState();
			}

			RefreshSharedProfileRegistrationIfNeeded();
			}
			finally
			{
				if (diagnosticsWorkStart > 0)
					OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.BarUpdate, diagnosticsBarsInProgress, diagnosticsWorkStart);
			}
		}

		private void EnsureDiagnosticsRegistered()
		{
			if (diagnosticsRegistered)
				return;

			OrcaDiagnosticsCore.RegisterInstance(diagnosticsInstanceId, "OrcaPrints", this);
			OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsInstanceId, "TickReplayLastEvents", "Unknown", PublishSharedProfileCache ? "SharedChartCache" : string.Empty);
			OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 0, "PrimaryChartSeries", "Chart", "Primary chart market-data events");
			diagnosticsRegistered = true;
		}

		private void ReportDiagnosticsState()
		{

			EnsureDiagnosticsRegistered();
			OrcaDiagnosticsCore.ReportState(diagnosticsInstanceId, State.ToString());
		}

		private DateTime GetDiagnosticsEventTime()
		{
			try
			{
				if (Times != null && CurrentBars != null && BarsInProgress >= 0 && BarsInProgress < Times.Length && BarsInProgress < CurrentBars.Length && CurrentBars[BarsInProgress] >= 0)
					return Times[BarsInProgress][0];
			}
			catch { }

			try
			{
				if (CurrentBar >= 0)
					return Time[0];
			}
			catch { }

			return DateTime.MinValue;
		}

		#region 01. General
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Min Trade Size", Order = 1, GroupName = "01. General")]
		public int MinTradeSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Reset On New Session", Order = 2, GroupName = "01. General")]
		public bool ResetOnNewSession { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Publish Shared Profile Cache", Order = 3, GroupName = "01. General",
			Description = "Publishes live volume-at-price and classified delta maps for Fixed Range and other Orca tools on the same chart.")]
		public bool PublishSharedProfileCache { get; set; }

		string IOrcaReplayParticipant.ReplayParticipantId { get { return replayBarHorizon == null ? "OrcaPrints:" + diagnosticsInstanceId : replayBarHorizon.ParticipantId; } }
		OrcaReplayCapabilities IOrcaReplayParticipant.ReplayCapabilities { get { return replayBarHorizon == null ? new OrcaReplayCapabilities(true, false, true, true, OrcaReplayChartStyleSupport.AllV1) : replayBarHorizon.Capabilities; } }
		OrcaReplayCheckpoint IOrcaReplayParticipant.CaptureReplayCheckpoint(OrcaReplayContext context) { return replayBarHorizon.Capture(context); }
		void IOrcaReplayParticipant.PrepareReplay(OrcaReplayContext context, OrcaReplayCheckpoint checkpoint) { replayBarHorizon.Prepare(context, checkpoint); }
		void IOrcaReplayParticipant.ApplyReplayEvent(OrcaReplayContext context, OrcaReplayTradeEvent tradeEvent) { }
		void IOrcaReplayParticipant.ApplyReplayBar(OrcaReplayContext context, int primaryBarIndex) { replayBarHorizon.ApplyBar(primaryBarIndex); }
		void IOrcaReplayParticipant.PublishReplaySnapshot(OrcaReplayContext context) { }
		void IOrcaReplayParticipant.RestoreLiveState() { if (replayBarHorizon != null) replayBarHorizon.Restore(); }

		[NinjaScriptProperty]
		[Display(Name = "Exclude MOC Prints", Order = 4, GroupName = "01. General", Description = "Excludes all Orca print detection inside the configured Eastern market-on-close window while preserving candle-profile volume.")]
		public bool ExcludeMocPrints { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "MOC Start Time ET", Order = 5, GroupName = "01. General")]
		public TimeSpan MocStartTimeEt { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "MOC End Time ET", Order = 6, GroupName = "01. General")]
		public TimeSpan MocEndTimeEt { get; set; }
		#endregion

		#region 02. Single Prints
		[NinjaScriptProperty]
		[Display(Name = "Enable Single Prints", Order = 1, GroupName = "02. Single Prints")]
		public bool EnableSinglePrints { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Single Print Min Size", Order = 2, GroupName = "02. Single Prints")]
		public int SinglePrintMinSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Size Text", Order = 3, GroupName = "02. Single Prints", Description = "Shows the contract size inside single-print circles when the selected text fits without clipping.")]
		public bool ShowSinglePrintSizeText { get; set; }

		[NinjaScriptProperty]
		[Range(6.0, 30.0)]
		[Display(Name = "Text Size", Order = 4, GroupName = "02. Single Prints")]
		public float SinglePrintTextFontSize { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(CandleProfileTextFontFamilyConverter))]
		[Display(Name = "Text Font", Order = 5, GroupName = "02. Single Prints")]
		public string SinglePrintTextFontFamily { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Hide Singles Matching Clusters", Order = 6, GroupName = "02. Single Prints", Description = "Hides a single print when an accepted cluster has the same timestamp and price row.")]
		public bool HideSinglePrintsMatchingClusters { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Aggregate Singles When Compact", Order = 7, GroupName = "02. Single Prints", Description = "Combines nearby same-side single prints into display-only bubbles while Auto Compact Layout is active.")]
		public bool AggregateSinglePrintsWhenCompact { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Aggregate Price Range Ticks", Order = 8, GroupName = "02. Single Prints")]
		public int CompactAggregatePriceRangeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(2, 1000)]
		[Display(Name = "Aggregate Min Prints", Order = 9, GroupName = "02. Single Prints")]
		public int CompactAggregateMinPrints { get; set; }
		#endregion

		#region 03. Clusters
		[NinjaScriptProperty]
		[Display(Name = "Enable Clusters", Order = 1, GroupName = "03. Clusters")]
		public bool EnableClusters { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 60.0)]
		[Display(Name = "Cluster Time Window Sec", Order = 2, GroupName = "03. Clusters")]
		public double ClusterTimeWindowSec { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000000000)]
		[Display(Name = "Cluster Min Volume", Order = 3, GroupName = "03. Clusters")]
		public long ClusterMinVolume { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Cluster Max Price Ticks", Order = 4, GroupName = "03. Clusters")]
		public int ClusterMaxPriceTicks { get; set; }

		[NinjaScriptProperty]
		[Range(50, 100)]
		[Display(Name = "Min Aggressor Percent", Order = 5, GroupName = "03. Clusters")]
		public int MinAggressorPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 60.0)]
		[Display(Name = "Cluster Cooldown Sec", Order = 6, GroupName = "03. Clusters")]
		public double ClusterCooldownSec { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Aggregate Clusters When Compact", Order = 7, GroupName = "03. Clusters", Description = "Combines nearby same-side detected clusters into display-only super clusters while Auto Compact Layout is active.")]
		public bool AggregateClustersWhenCompact { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Aggregate Price Range Ticks", Order = 8, GroupName = "03. Clusters")]
		public int CompactClusterAggregatePriceRangeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(2, 1000)]
		[Display(Name = "Aggregate Min Clusters", Order = 9, GroupName = "03. Clusters")]
		public int CompactClusterAggregateMinClusters { get; set; }
		#endregion

		#region 03B. Price Levels
		[NinjaScriptProperty]
		[Display(Name = "Enable Price Levels", Order = 1, GroupName = "03B. Price Levels")]
		public bool EnablePriceLevelAccumulation { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Highlight Mode", Order = 2, GroupName = "03B. Price Levels", Description = "Uses the existing volume/dominance trigger, signed high delta, or either trigger independently.")]
		public PriceLevelHighlightMode PriceLevelHighlightMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000000000)]
		[Display(Name = "Price Level Min Volume", Order = 3, GroupName = "03B. Price Levels")]
		public long PriceLevelMinVolume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Min Dominance", Order = 4, GroupName = "03B. Price Levels")]
		public bool PriceLevelRequireMinDominance { get; set; }

		[NinjaScriptProperty]
		[Range(50, 100)]
		[Display(Name = "Min Dominance Percent", Order = 5, GroupName = "03B. Price Levels")]
		public int PriceLevelMinDominancePercent { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000000000)]
		[Display(Name = "Minimum Absolute Delta", Order = 6, GroupName = "03B. Price Levels")]
		public long PriceLevelMinDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Delta Direction", Order = 7, GroupName = "03B. Price Levels")]
		public PriceLevelDeltaDirection PriceLevelDeltaDirection { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Delta Window Ticks", Order = 8, GroupName = "03B. Price Levels", Description = "Calculates net signed delta across this many consecutive price rows inside the current candle.")]
		public int PriceLevelDeltaWindowTicks { get; set; }
		#endregion

		#region 04. Parent Confidence
		[NinjaScriptProperty]
		[Display(Name = "Parent Confidence Mode", Order = 1, GroupName = "04. Parent Confidence")]
		public ParentConfidenceMode ParentConfidenceMode { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Min Parent Confidence", Order = 2, GroupName = "04. Parent Confidence")]
		public int MinParentConfidence { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Weight Aggressor Consistency", Order = 3, GroupName = "04. Parent Confidence")]
		public double WeightAggressorConsistency { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Weight Size Uniformity", Order = 4, GroupName = "04. Parent Confidence")]
		public double WeightSizeUniformity { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Weight Price Tightness", Order = 5, GroupName = "04. Parent Confidence")]
		public double WeightPriceTightness { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Weight Timing Regularity", Order = 6, GroupName = "04. Parent Confidence")]
		public double WeightTimingRegularity { get; set; }
		#endregion

		#region 05. Rendering
		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "Min Dot Size", Order = 1, GroupName = "05. Rendering")]
		public int MinDotSize { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Max Dot Size", Order = 2, GroupName = "05. Rendering")]
		public int MaxDotSize { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Single Min Dot Size", Order = 3, GroupName = "05. Rendering")]
		public int SinglePrintMinDotSize { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Single Max Dot Size", Order = 4, GroupName = "05. Rendering")]
		public int SinglePrintMaxDotSize { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Price Level Min Dot Size", Order = 5, GroupName = "05. Rendering")]
		public int PriceLevelMinDotSize { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Price Level Max Dot Size", Order = 6, GroupName = "05. Rendering")]
		public int PriceLevelMaxDotSize { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Cluster Min Dot Size", Order = 7, GroupName = "05. Rendering")]
		public int ClusterMinDotSize { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Cluster Max Dot Size", Order = 8, GroupName = "05. Rendering")]
		public int ClusterMaxDotSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dot Size Scale", Order = 9, GroupName = "05. Rendering")]
		public DotSizeScale DotSizeScale { get; set; }

		[XmlIgnore]
		[Display(Name = "Buy Aggressor Color", Order = 10, GroupName = "05. Rendering")]
		public WpfBrush BuyAggressorColor { get; set; }

		[Browsable(false)]
		public string BuyAggressorColorSerializable
		{
			get { return Serialize.BrushToString(BuyAggressorColor); }
			set { BuyAggressorColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Sell Aggressor Color", Order = 11, GroupName = "05. Rendering")]
		public WpfBrush SellAggressorColor { get; set; }

		[Browsable(false)]
		public string SellAggressorColorSerializable
		{
			get { return Serialize.BrushToString(SellAggressorColor); }
			set { SellAggressorColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Price Level Buy Color", Order = 12, GroupName = "05. Rendering")]
		public WpfBrush PriceLevelBuyColor { get; set; }

		[Browsable(false)]
		public string PriceLevelBuyColorSerializable
		{
			get { return Serialize.BrushToString(PriceLevelBuyColor); }
			set { PriceLevelBuyColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Price Level Sell Color", Order = 13, GroupName = "05. Rendering")]
		public WpfBrush PriceLevelSellColor { get; set; }

		[Browsable(false)]
		public string PriceLevelSellColorSerializable
		{
			get { return Serialize.BrushToString(PriceLevelSellColor); }
			set { PriceLevelSellColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "Use Variable Intensity", Order = 14, GroupName = "05. Rendering")]
		public bool UseVariableIntensity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Min Intensity Pct", Order = 15, GroupName = "05. Rendering")]
		public int MinIntensityPct { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Border Enabled", Order = 16, GroupName = "05. Rendering")]
		public bool BorderEnabled { get; set; }

		[XmlIgnore]
		[Display(Name = "Border Color", Order = 17, GroupName = "05. Rendering")]
		public WpfBrush BorderColor { get; set; }

		[Browsable(false)]
		public string BorderColorSerializable
		{
			get { return Serialize.BrushToString(BorderColor); }
			set { BorderColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 99)]
		[Display(Name = "Transparency Pct", Order = 18, GroupName = "05. Rendering")]
		public int TransparencyPct { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Shape Mode", Order = 19, GroupName = "05. Rendering")]
		public ShapeMode ShapeMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Horizontal Anchor", Order = 20, GroupName = "05. Rendering")]
		public OrcaPrintHorizontalAnchor HorizontalAnchor { get; set; }

		[NinjaScriptProperty]
		[Range(-500, 500)]
		[Display(Name = "Horizontal Offset Px", Order = 21, GroupName = "05. Rendering")]
		public int HorizontalOffsetPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Auto Compact Layout", Order = 22, GroupName = "05. Rendering")]
		public bool AutoCompactLayout { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Compact Below Bar Spacing", Order = 23, GroupName = "05. Rendering")]
		public int CompactLayoutEnterSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Detail Above Bar Spacing", Order = 24, GroupName = "05. Rendering")]
		public int DetailLayoutEnterSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Hide Prints When Zoomed Out", Order = 25, GroupName = "05. Rendering", Description = "Hides every Orca print when average visible bar spacing falls below the configured pixel threshold.")]
		public bool HidePrintsWhenZoomedOut { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Minimum Bar Spacing To Show Prints", Order = 26, GroupName = "05. Rendering", Description = "Minimum average pixels per bar required to show Orca prints. Higher values hide prints sooner while zooming out.")]
		public int MinimumBarSpacingToShowPrintsPx { get; set; }
		#endregion
	}
}
