using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript
{
	public class OrcaProfileBucket
	{
		public DateTime MinuteToken { get; set; }
		public Dictionary<double, long> VolByPrice { get; set; } = new Dictionary<double, long>();
		public Dictionary<double, long> DeltaByPrice { get; set; } = new Dictionary<double, long>();
	}

	public class OrcaRollingProfileTick
	{
		public DateTime Time { get; set; }
		public double VolumePrice { get; set; }
		public double DeltaPrice { get; set; }
		public long Volume { get; set; }
		public long Delta { get; set; }
	}

	public enum ProfileOperatingMode
	{
		FullSession,
		RthOnly
	}

	public enum RollingProfilePeriod
	{
		Minutes15 = 15,
		Minutes30 = 30,
		Hour1 = 60,
		Hours4 = 240,
		Hours8 = 480,
		Day1 = 1,
		Days2 = 2,
		Days5 = 5,
		Days10 = 10,
		Days20 = 20
	}

	public enum RollingDeltaDirection
	{
		TowardPriceScale,
		TowardCandles
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaRollingProfiles : Indicator
	{
		private readonly object profileDataSync = new object();
		private Queue<OrcaProfileBucket> rollingHistory;
		private SortedDictionary<DateTime, List<OrcaRollingProfileTick>> activeProfileTicksByTime;
		private int activeProfileTickCount;
		private OrcaProfileBucket developingBucket;
		private OrcaProfileBucket totalProfile;
		private DateTime currentMinuteToken;
		private DateTime currentWindowStartTime = DateTime.MinValue;
		private DateTime currentWindowEndTime = DateTime.MinValue;
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double prevLast = double.NaN;
		private int lastDynamicDeltaComp = -1;
		private bool addLocalTickSeries;
		private bool localTickSeriesHydrating;
		private bool localTickSeriesReady;
		private bool sharedProviderHydrating;
		private Guid sharedOrderFlowSourceId = Guid.Empty;
		private int sharedOrderFlowNextIndex;
		private int sharedOrderFlowBucketSeconds = int.MinValue;
		private int sharedOrderFlowRevision = int.MinValue;
		private string dataSourceLabel = string.Empty;
		private DateTime lastMissingProviderPrintUtc = DateTime.MinValue;
		private DateTime lastSharedProviderUpdateUtc = DateTime.MinValue;
		private int lastPrunedActiveTicks;
		private int lastDroppedStaleTicks;
		private DateTime lastDebugPrintUtc = DateTime.MinValue;
		private long profileBuildSequence;
		private string lastDebugProfileSignature = string.Empty;

		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private SharpDX.Direct2D1.SolidColorBrush volBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush pocBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush posDeltaBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush negDeltaBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush[] positiveDeltaIntensityBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] negativeDeltaIntensityBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] volGradientBrushes;
		private int lastBuiltGradientSteps = -1;
		private SharpDX.Direct2D1.SolidColorBrush vaVolBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush[] vaGradientBrushes;
		private int lastBuiltVAGradientSteps = -1;
		private int lastBuiltDeltaIntensitySteps = -1;
		private float lastBuiltDeltaIntensityMinOpacity = -1f;
		private float lastBuiltDeltaIntensityMaxOpacity = -1f;
		private SharpDX.Direct2D1.SolidColorBrush vaLineBrushDx;
		private SharpDX.Direct2D1.StrokeStyle vaLineStrokeDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaTextBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush negativeDeltaTextBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush labelBgBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush sourceLabelBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush sourceLabelBgBrushDx;
		private TextFormat deltaTextFormatDx;
		private TextFormat sourceLabelTextFormatDx;
		private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();

		// ── 1. Data ──────────────────────────────────────────────────────────────
		[NinjaScriptProperty]
		[Display(Name = "1. Rolling Period", Order = 1, GroupName = "1. Data")]
		public RollingProfilePeriod Period { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "2. Operating Mode", Order = 2, GroupName = "1. Data")]
		public ProfileOperatingMode Mode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Master Data Provider", Order = 3, GroupName = "1. Data")]
		public bool UseSharedProfileDataProvider { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Master Historical Backfill", Order = 4, GroupName = "1. Data")]
		public bool EnableSharedProviderHistoricalBackfill { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Local Tick Cache (Reload)", Order = 5, GroupName = "1. Data")]
		public bool UseLocalTickSeriesCache { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Data Source Label", Order = 6, GroupName = "1. Data")]
		public bool ShowDataSourceLabel { get; set; }

		[NinjaScriptProperty]
		[Range(100, 50000)]
		[Display(Name = "Master Ticks Per Render", Order = 7, GroupName = "1. Data")]
		public int SharedProviderMaxTicksPerRender { get; set; }

		[NinjaScriptProperty]
		[Range(5, 1440)]
		[Display(Name = "Max Master Backfill Minutes", Order = 8, GroupName = "1. Data")]
		public int SharedProviderMaxBackfillMinutes { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Debug Profile Signatures", Order = 9, GroupName = "1. Data")]
		public bool DebugProfileSignatures { get; set; }

		[NinjaScriptProperty]
		[Range(1, 3000)]
		[Display(Name = "3. Minutes In Trading Day", Order = 10, GroupName = "1. Data")]
		public int MinutesPerDay { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "4. RTH Start Time", Order = 11, GroupName = "1. Data")]
		public TimeSpan RthStartTime { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "5. RTH End Time", Order = 12, GroupName = "1. Data")]
		public TimeSpan RthEndTime { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Volume Tick Compression", Order = 13, GroupName = "1. Data")]
		public int VolumeTickCompression { get; set; }

		// ── 2. Layout ────────────────────────────────────────────────────────────
		[NinjaScriptProperty]
		[Range(10, 1000)]
		[Display(Name = "Profile Width (px)", Order = 1, GroupName = "2. Layout")]
		public int ProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Delta Width (px)", Order = 2, GroupName = "2. Layout")]
		public int DeltaWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000)]
		[Display(Name = "Right Canvas Offset (px)", Order = 3, GroupName = "2. Layout")]
		public int RightOffsetPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Bar Spacing (px)", Order = 4, GroupName = "2. Layout")]
		public int ProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Delta Direction", Order = 5, GroupName = "2. Layout")]
		public RollingDeltaDirection DeltaDirection { get; set; }

		// ── 3. Visibility ────────────────────────────────────────────────────────
		[NinjaScriptProperty]
		[Display(Name = "Show Volume", Order = 1, GroupName = "3. Visibility")]
		public bool ShowVolume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta", Order = 2, GroupName = "3. Visibility")]
		public bool ShowDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show POC", Order = 3, GroupName = "3. Visibility")]
		public bool ShowPOC { get; set; }

		// ── 4. Gradient ──────────────────────────────────────────────────────────
		[NinjaScriptProperty]
		[Display(Name = "Use Gradient", Order = 1, GroupName = "4. Gradient")]
		public bool UseGradient { get; set; }

		[NinjaScriptProperty]
		[Range(2, 64)]
		[Display(Name = "Gradient Steps", Order = 2, GroupName = "4. Gradient")]
		public int GradientSteps { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 1.0)]
		public float MinBrightness { get; set; }

		// ── 5. Value Area ────────────────────────────────────────────────────────
		[NinjaScriptProperty]
		[Display(Name = "Show Value Area", Order = 1, GroupName = "5. Value Area")]
		public bool ShowValueArea { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VA Color", Order = 2, GroupName = "5. Value Area")]
		public bool ShowVAColor { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show VA Lines", Order = 3, GroupName = "5. Value Area")]
		public bool ShowVALines { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		public int ValueAreaPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		public float VALineThickness { get; set; }

		// ── 6. Colors ────────────────────────────────────────────────────────────
		[XmlIgnore]
		[Display(Name = "Volume Background", Order = 1, GroupName = "6. Colors")]
		public System.Windows.Media.Brush VolumeBrush { get; set; }

		[Browsable(false)]
		public string VolumeBrushSerializable
		{
			get { return Serialize.BrushToString(VolumeBrush); }
			set { VolumeBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		public float VolumeOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Color", Order = 3, GroupName = "6. Colors")]
		public System.Windows.Media.Brush POCBrush { get; set; }

		[Browsable(false)]
		public string POCBrushSerializable
		{
			get { return Serialize.BrushToString(POCBrush); }
			set { POCBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Value Area Background", Order = 4, GroupName = "6. Colors")]
		public System.Windows.Media.Brush VABrush { get; set; }

		[Browsable(false)]
		public string VABrushSerializable
		{
			get { return Serialize.BrushToString(VABrush); }
			set { VABrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Value Area Lines", Order = 5, GroupName = "6. Colors")]
		public System.Windows.Media.Brush VALineBrush { get; set; }

		[Browsable(false)]
		public string VALineBrushSerializable
		{
			get { return Serialize.BrushToString(VALineBrush); }
			set { VALineBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Positive Delta", Order = 6, GroupName = "6. Colors")]
		public System.Windows.Media.Brush PositiveDeltaBrush { get; set; }

		[Browsable(false)]
		public string PositiveDeltaBrushSerializable
		{
			get { return Serialize.BrushToString(PositiveDeltaBrush); }
			set { PositiveDeltaBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Negative Delta", Order = 7, GroupName = "6. Colors")]
		public System.Windows.Media.Brush NegativeDeltaBrush { get; set; }

		[Browsable(false)]
		public string NegativeDeltaBrushSerializable
		{
			get { return Serialize.BrushToString(NegativeDeltaBrush); }
			set { NegativeDeltaBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Delta Opacity", Order = 8, GroupName = "6. Colors")]
		public float DeltaOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Delta Intensity Color", Order = 9, GroupName = "6. Colors")]
		public bool UseDeltaIntensityColoring { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Delta Intensity Min Opacity", Order = 10, GroupName = "6. Colors")]
		public float DeltaIntensityMinOpacity { get; set; }

		// ── 7. Delta Text ────────────────────────────────────────────────────────
		[NinjaScriptProperty]
		[Display(Name = "Show Text", Order = 1, GroupName = "7. Delta Text")]
		public bool ShowDeltaText { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Label Background", Order = 2, GroupName = "7. Delta Text")]
		public bool ShowDeltaLabelBackground { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000000)]
		public int DeltaTextMinThreshold { get; set; }

		[XmlIgnore]
		[Display(Name = "Positive Text Color", Order = 4, GroupName = "7. Delta Text")]
		public System.Windows.Media.Brush DeltaTextBrush { get; set; }

		[Browsable(false)]
		public string DeltaTextBrushSerializable
		{
			get { return Serialize.BrushToString(DeltaTextBrush); }
			set { DeltaTextBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Negative Text Color", Order = 5, GroupName = "7. Delta Text")]
		public System.Windows.Media.Brush NegativeDeltaTextBrush { get; set; }

		[Browsable(false)]
		public string NegativeDeltaTextBrushSerializable
		{
			get { return Serialize.BrushToString(NegativeDeltaTextBrush); }
			set { NegativeDeltaTextBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Label Background Color", Order = 6, GroupName = "7. Delta Text")]
		public System.Windows.Media.Brush DeltaLabelBgBrush { get; set; }

		[Browsable(false)]
		public string DeltaLabelBgBrushSerializable
		{
			get { return Serialize.BrushToString(DeltaLabelBgBrush); }
			set { DeltaLabelBgBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(6.0, 36.0)]
		public float DeltaTextFontSize { get; set; }

		// ── 8. Dynamic Delta Aggregation ─────────────────────────────────────────
		[NinjaScriptProperty]
		[Display(Name = "Use Dynamic Aggregation", Order = 1, GroupName = "8. Delta Aggregation",
			Description = "Automatically adjusts delta bar height to stay readable at any zoom level")]
		public bool UseDynamicAggregation { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "Aggregation Multiplier", Order = 2, GroupName = "8. Delta Aggregation",
			Description = "Lower = thinner/more granular bars (e.g. 0.8). Higher = thicker bars (e.g. 1.5)")]
		public double DynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(2, 40)]
		[Display(Name = "Delta Dynamic Row Min Pixels", Order = 3, GroupName = "8. Delta Aggregation",
			Description = "Target minimum row height used before applying the aggregation multiplier")]
		public int DeltaDynamicRowMinPixels { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Dynamic Delta Min Compression", Order = 4, GroupName = "8. Delta Aggregation",
			Description = "Minimum ticks per delta row when Dynamic Aggregation is ON")]
		public int DynamicDeltaMinCompression { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Dynamic Delta Max Compression", Order = 5, GroupName = "8. Delta Aggregation",
			Description = "Maximum ticks per delta row when Dynamic Aggregation is ON")]
		public int DynamicDeltaMaxCompression { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Delta Tick Compression (static)", Order = 6, GroupName = "8. Delta Aggregation",
			Description = "Used when Dynamic Aggregation is OFF")]
		public int DeltaTickCompression { get; set; }

		// ─────────────────────────────────────────────────────────────────────────

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca Rolling Profiles";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				Period = RollingProfilePeriod.Day1;
				Mode = ProfileOperatingMode.FullSession;
				UseSharedProfileDataProvider = false;
				EnableSharedProviderHistoricalBackfill = false;
				UseLocalTickSeriesCache = true;
				ShowDataSourceLabel = true;
				SharedProviderMaxTicksPerRender = 500;
				SharedProviderMaxBackfillMinutes = 90;
				DebugProfileSignatures = false;
				MinutesPerDay = 1380;
				RthStartTime = new TimeSpan(9, 30, 0);
				RthEndTime = new TimeSpan(16, 0, 0);
				VolumeTickCompression = 4;
				ProfileWidthPx = 150;
				DeltaWidthPx = 60;
				RightOffsetPx = 60;
				ProfileBarSpacingPx = 0;
				DeltaDirection = RollingDeltaDirection.TowardPriceScale;
				ShowVolume = true;
				ShowDelta = true;
				ShowPOC = true;
				UseGradient = true;
				GradientSteps = 16;
				MinBrightness = 0.2f;
				ShowValueArea = true;
				ShowVAColor = true;
				ShowVALines = true;
				ValueAreaPercent = 70;
				VALineThickness = 1.5f;
				VolumeBrush = Brushes.RoyalBlue;
				VolumeOpacity = 0.85f;
				POCBrush = Brushes.DodgerBlue;
				VABrush = Brushes.CornflowerBlue;
				VALineBrush = Brushes.White;
				PositiveDeltaBrush = Brushes.Lime;
				NegativeDeltaBrush = Brushes.Red;
				DeltaOpacity = 0.85f;
				UseDeltaIntensityColoring = true;
				DeltaIntensityMinOpacity = 0.35f;
				ShowDeltaText = true;
				ShowDeltaLabelBackground = true;
				DeltaTextMinThreshold = 10;
				DeltaTextBrush = Brushes.LightGreen;
				NegativeDeltaTextBrush = Brushes.LightCoral;
				DeltaLabelBgBrush = Brushes.Black;
				DeltaTextFontSize = 10f;
				UseDynamicAggregation = true;
				DynamicAggregationMultiplier = 1.0;
				DeltaDynamicRowMinPixels = 10;
				DynamicDeltaMinCompression = 1;
				DynamicDeltaMaxCompression = 100;
				DeltaTickCompression = 4;
			}
			else if (State == State.Configure)
			{
				addLocalTickSeries = UseLocalTickSeriesCache && !UseSharedProfileDataProvider;
				if (addLocalTickSeries)
					AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				ResetRollingProfiles();
				textWidthCache.Clear();
			}
			else if (State == State.Historical)
			{
				if (addLocalTickSeries)
				{
					localTickSeriesHydrating = true;
					localTickSeriesReady = false;
					dataSourceLabel = "Source: local tick cache hydrating";
				}
			}
			else if (State == State.Transition || State == State.Realtime)
			{
				if (addLocalTickSeries)
					MarkLocalTickSeriesReady();
			}
			else if (State == State.Terminated)
			{
				DisposeDx();
			}
		}

		private void ResetRollingProfiles()
		{
			lock (profileDataSync)
			{
				ClearProfileDataUnsafe();
				currentMinuteToken = DateTime.MinValue;
				currentWindowStartTime = DateTime.MinValue;
				currentWindowEndTime = DateTime.MinValue;
				prevLast = double.NaN;
				sharedOrderFlowNextIndex = 0;
				sharedOrderFlowRevision = int.MinValue;
				sharedOrderFlowSourceId = Guid.Empty;
				sharedOrderFlowBucketSeconds = int.MinValue;
				sharedProviderHydrating = false;
				localTickSeriesHydrating = addLocalTickSeries;
				localTickSeriesReady = !addLocalTickSeries;
				lastSharedProviderUpdateUtc = DateTime.MinValue;
				lastPrunedActiveTicks = 0;
				lastDroppedStaleTicks = 0;
				lastDebugPrintUtc = DateTime.MinValue;
				profileBuildSequence = 0;
				lastDebugProfileSignature = string.Empty;
			}
		}

		private void ClearProfileDataUnsafe()
		{
			rollingHistory = new Queue<OrcaProfileBucket>();
			activeProfileTicksByTime = new SortedDictionary<DateTime, List<OrcaRollingProfileTick>>();
			activeProfileTickCount = 0;
			developingBucket = new OrcaProfileBucket();
			totalProfile = new OrcaProfileBucket();
		}

		private void DisposeDx()
		{
			if (volBrushDx != null) volBrushDx.Dispose();
			if (pocBrushDx != null) pocBrushDx.Dispose();
			if (posDeltaBrushDx != null) posDeltaBrushDx.Dispose();
			if (negDeltaBrushDx != null) negDeltaBrushDx.Dispose();
			if (positiveDeltaIntensityBrushes != null) foreach (var b in positiveDeltaIntensityBrushes) if (b != null) b.Dispose();
			if (negativeDeltaIntensityBrushes != null) foreach (var b in negativeDeltaIntensityBrushes) if (b != null) b.Dispose();
			if (vaVolBrushDx != null) vaVolBrushDx.Dispose();
			if (vaLineBrushDx != null) vaLineBrushDx.Dispose();
			if (vaLineStrokeDx != null) vaLineStrokeDx.Dispose();
			if (volGradientBrushes != null) foreach (var b in volGradientBrushes) if (b != null) b.Dispose();
			if (vaGradientBrushes != null) foreach (var b in vaGradientBrushes) if (b != null) b.Dispose();
			if (deltaTextBrushDx != null) deltaTextBrushDx.Dispose();
			if (negativeDeltaTextBrushDx != null) negativeDeltaTextBrushDx.Dispose();
			if (labelBgBrushDx != null) labelBgBrushDx.Dispose();
			if (sourceLabelBrushDx != null) sourceLabelBrushDx.Dispose();
			if (sourceLabelBgBrushDx != null) sourceLabelBgBrushDx.Dispose();
			if (sourceLabelTextFormatDx != null) sourceLabelTextFormatDx.Dispose();

			volBrushDx = null;
			pocBrushDx = null;
			posDeltaBrushDx = null;
			negDeltaBrushDx = null;
			positiveDeltaIntensityBrushes = null;
			negativeDeltaIntensityBrushes = null;
			vaVolBrushDx = null;
			vaLineBrushDx = null;
			vaLineStrokeDx = null;
			volGradientBrushes = null;
			vaGradientBrushes = null;
			deltaTextBrushDx = null;
			negativeDeltaTextBrushDx = null;
			labelBgBrushDx = null;
			sourceLabelBrushDx = null;
			sourceLabelBgBrushDx = null;
			deltaTextFormatDx = null;
			sourceLabelTextFormatDx = null;
			dxResourceRenderTarget = IntPtr.Zero;
			lastBuiltGradientSteps = -1;
			lastBuiltVAGradientSteps = -1;
			lastBuiltDeltaIntensitySteps = -1;
			lastBuiltDeltaIntensityMinOpacity = -1f;
			lastBuiltDeltaIntensityMaxOpacity = -1f;
			lastDynamicDeltaComp = -1;
			textWidthCache.Clear();
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid) lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask) lastAsk = e.Price;
		}

		protected override void OnBarUpdate()
		{
			if (UseSharedProfileDataProvider && BarsInProgress == 0)
			{
				UpdateFromSharedProvider();
				return;
			}

			if (!addLocalTickSeries || BarsInProgress != 1) return;
			if (CurrentBars == null || CurrentBars.Length < 2 || CurrentBars[1] < 0)
				return;

			DateTime time = Times[1][0];
			if (Mode == ProfileOperatingMode.RthOnly && (time.TimeOfDay < RthStartTime || time.TimeOfDay > RthEndTime)) return;

			double price = Closes[1][0];
			long volume = (long)Volumes[1][0];
			if (volume <= 0) return;

			lock (profileDataSync)
			{
				long delta = 0;
				if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
				{
					if (price >= lastAsk) delta = volume;
					else if (price <= lastBid) delta = -volume;
					else if (!double.IsNaN(prevLast)) delta = (price > prevLast) ? volume : (price < prevLast ? -volume : 0);
				}
				else if (!double.IsNaN(prevLast))
				{
					delta = (price > prevLast) ? volume : (price < prevLast ? -volume : 0);
				}
				prevLast = price;

				AddTradeToRollingProfilesUnsafe(time, price, volume, delta);
				dataSourceLabel = "Source: local tick cache";
			}
		}

		private void UpdateFromSharedProvider()
		{
			if (!UseSharedProfileDataProvider || Bars == null)
				return;

			if (!EnableSharedProviderHistoricalBackfill)
			{
				dataSourceLabel = "Source: master disabled";
				return;
			}

			DateTime now = DateTime.UtcNow;
			if (State == State.Realtime && (now - lastSharedProviderUpdateUtc).TotalMilliseconds < 500)
				return;
			lastSharedProviderUpdateUtc = now;

			string instrumentKey = OrcaProfileDataCache.BuildInstrumentKey(Bars);
			if (string.IsNullOrEmpty(instrumentKey))
				return;

			DateTime fromTime = GetSharedProviderStartTime();
			if (fromTime == DateTime.MinValue)
			{
				dataSourceLabel = "Source: master waiting";
				return;
			}

			OrcaOrderFlowDataSnapshot snapshot;
			Guid sourceId;
			int nextIndex;
			int totalBucketCount;
			int batchSize = Math.Max(100, Math.Min(SharedProviderMaxTicksPerRender, 5000));
			bool hasData = OrcaProfileDataCache.TrySnapshotOrderFlowSinceIndex(instrumentKey, sharedOrderFlowSourceId, sharedOrderFlowNextIndex, fromTime, batchSize, out snapshot, out sourceId, out nextIndex, out totalBucketCount);
			if (sourceId == Guid.Empty || snapshot == null)
			{
				dataSourceLabel = addLocalTickSeries ? "Source: local tick cache" : "Source: no master (reload provider)";
				PrintMissingProviderIfNeeded(instrumentKey);
				return;
			}

			bool reset = sharedOrderFlowSourceId == Guid.Empty || sharedOrderFlowSourceId != sourceId || sharedOrderFlowNextIndex < 0 || sharedOrderFlowNextIndex > totalBucketCount || snapshot.BucketSeconds != sharedOrderFlowBucketSeconds;
			if (reset)
			{
				lock (profileDataSync)
				{
					ClearProfileDataUnsafe();
					currentMinuteToken = DateTime.MinValue;
					currentWindowStartTime = DateTime.MinValue;
					currentWindowEndTime = DateTime.MinValue;
				}
			}

			sharedOrderFlowSourceId = sourceId;
			sharedOrderFlowNextIndex = nextIndex;
			sharedOrderFlowBucketSeconds = snapshot.BucketSeconds;
			sharedOrderFlowRevision = snapshot.Revision;
			if (snapshot.BucketSeconds != 0)
			{
				dataSourceLabel = "Source: master bucket " + snapshot.BucketSeconds + "s (set 0)";
				return;
			}

			sharedProviderHydrating = nextIndex < totalBucketCount;
			dataSourceLabel = sharedProviderHydrating ? "Source: master tick loading" : "Source: master tick";
			if (!hasData || snapshot.Buckets == null || snapshot.Buckets.Count == 0)
				return;

			snapshot.Buckets.Sort(CompareOrderFlowBuckets);
			lock (profileDataSync)
			{
				for (int index = 0; index < snapshot.Buckets.Count; index++)
				{
					OrcaOrderFlowBucket bucket = snapshot.Buckets[index];
					if (bucket == null || bucket.Volume <= 0 || double.IsNaN(bucket.Price) || double.IsInfinity(bucket.Price))
						continue;

					long delta = bucket.Delta;
					if (delta == 0 && (bucket.AskVolume > 0 || bucket.BidVolume > 0))
						delta = bucket.AskVolume - bucket.BidVolume;

					AddTradeToRollingProfilesUnsafe(bucket.Time, bucket.Price, bucket.Volume, delta);
				}
			}
		}

		private int CompareOrderFlowBuckets(OrcaOrderFlowBucket left, OrcaOrderFlowBucket right)
		{
			if (left == null && right == null) return 0;
			if (left == null) return -1;
			if (right == null) return 1;

			int result = left.Time.CompareTo(right.Time);
			if (result != 0) return result;
			result = left.Price.CompareTo(right.Price);
			if (result != 0) return result;
			result = left.Volume.CompareTo(right.Volume);
			if (result != 0) return result;
			return left.Delta.CompareTo(right.Delta);
		}

		private void MarkLocalTickSeriesReady()
		{
			lock (profileDataSync)
			{
				if (localTickSeriesReady)
					return;

				localTickSeriesHydrating = false;
				localTickSeriesReady = true;
				dataSourceLabel = "Source: local tick cache";
				RebuildTotalProfileFromActiveTicksUnsafe();
				PrintProfileDebugUnsafe("ready");
			}
		}

		private void PrintMissingProviderIfNeeded(string instrumentKey)
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastMissingProviderPrintUtc).TotalSeconds < 30)
				return;

			lastMissingProviderPrintUtc = now;
			Print("[" + DateTime.Now.ToString("HH:mm:ss") + "] OrcaRollingProfiles: no OrcaProfileDataProvider order-flow source is registered for " + instrumentKey + ". Registered order-flow sources: " + OrcaProfileDataCache.DescribeOrderFlowSources());
		}

		private DateTime GetSharedProviderStartTime()
		{
			int periodMins = Math.Max(1, GetPeriodMinutes());
			int lookbackMins = Math.Max(5, Math.Min(Math.Max(5, SharedProviderMaxBackfillMinutes), periodMins + 2));
			if (currentMinuteToken != DateTime.MinValue)
				return currentMinuteToken.AddMinutes(-2);

			DateTime anchorTime = GetSharedProviderLastBucketTime();
			if (anchorTime != DateTime.MinValue)
				return anchorTime.AddMinutes(-lookbackMins);

			if (Bars != null && Bars.Count > 0)
				return Bars.GetTime(Bars.Count - 1).AddMinutes(-lookbackMins);

			return DateTime.MinValue;
		}

		private DateTime GetSharedProviderLastBucketTime()
		{
			if (Bars == null)
				return DateTime.MinValue;

			string instrumentKey = OrcaProfileDataCache.BuildInstrumentKey(Bars);
			if (string.IsNullOrEmpty(instrumentKey))
				return DateTime.MinValue;

			int revision;
			DateTime lastUpdatedUtc;
			string sourceName;
			int bucketSeconds;
			int bucketCount;
			DateTime firstBucketTime;
			DateTime lastBucketTime;
			if (OrcaProfileDataCache.TryGetOrderFlowStatus(instrumentKey, out revision, out lastUpdatedUtc, out sourceName, out bucketSeconds, out bucketCount, out firstBucketTime, out lastBucketTime))
				return lastBucketTime;

			return DateTime.MinValue;
		}

		private void AddTradeToRollingProfilesUnsafe(DateTime time, double price, long volume, long delta)
		{
			if (volume <= 0 || double.IsNaN(price) || double.IsInfinity(price))
				return;

			if (Mode == ProfileOperatingMode.RthOnly && (time.TimeOfDay < RthStartTime || time.TimeOfDay > RthEndTime))
				return;

			DateTime minute = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
			if (minute > currentMinuteToken)
				currentMinuteToken = minute;

			double vKey = NormalizeToBucketStart(price, VolumeTickCompression);
			double rawKey = NormalizeToBucketStart(price, 1);
			if (currentWindowEndTime == DateTime.MinValue || time > currentWindowEndTime)
			{
				currentWindowEndTime = time;
				currentWindowStartTime = currentWindowEndTime.AddMinutes(-Math.Max(1, GetPeriodMinutes()));
				lastPrunedActiveTicks = PruneActiveTicksUnsafe(currentWindowStartTime);
			}
			else
			{
				lastPrunedActiveTicks = 0;
			}

			if (currentWindowStartTime != DateTime.MinValue && time <= currentWindowStartTime)
			{
				lastDroppedStaleTicks++;
				profileBuildSequence++;
				return;
			}

			OrcaRollingProfileTick tick = new OrcaRollingProfileTick
			{
				Time = time,
				VolumePrice = vKey,
				DeltaPrice = rawKey,
				Volume = volume,
				Delta = delta
			};

			AddActiveTickUnsafe(tick);
			if (totalProfile.VolByPrice.ContainsKey(vKey)) totalProfile.VolByPrice[vKey] += volume;
			else totalProfile.VolByPrice[vKey] = volume;

			if (delta != 0)
			{
				if (totalProfile.DeltaByPrice.ContainsKey(rawKey)) totalProfile.DeltaByPrice[rawKey] += delta;
				else totalProfile.DeltaByPrice[rawKey] = delta;
			}

			profileBuildSequence++;
		}

		private int GetPeriodMinutes()
		{
			if (Period == RollingProfilePeriod.Day1) return MinutesPerDay;
			if (Period == RollingProfilePeriod.Days2) return MinutesPerDay * 2;
			if (Period == RollingProfilePeriod.Days5) return MinutesPerDay * 5;
			if (Period == RollingProfilePeriod.Days10) return MinutesPerDay * 10;
			if (Period == RollingProfilePeriod.Days20) return MinutesPerDay * 20;
			return (int)Period;
		}

		private void PruneRollingHistory(DateTime currentMinute)
		{
			int periodMins = Math.Max(1, GetPeriodMinutes());
			DateTime cutoff = currentMinute.AddMinutes(-(periodMins - 1));
			while (rollingHistory.Count > 0)
			{
				OrcaProfileBucket oldest = rollingHistory.Peek();
				if (oldest.MinuteToken != DateTime.MinValue && oldest.MinuteToken >= cutoff) break;
				SubtractBucketFromTotal(rollingHistory.Dequeue());
			}
		}

		private int PruneActiveTicksUnsafe(DateTime windowStartTime)
		{
			if (activeProfileTicksByTime == null)
				return 0;

			int pruned = 0;
			while (activeProfileTicksByTime.Count > 0)
			{
				DateTime oldestTime = DateTime.MinValue;
				List<OrcaRollingProfileTick> ticksAtTime = null;
				using (IEnumerator<KeyValuePair<DateTime, List<OrcaRollingProfileTick>>> enumerator = activeProfileTicksByTime.GetEnumerator())
				{
					if (!enumerator.MoveNext())
						break;

					oldestTime = enumerator.Current.Key;
					ticksAtTime = enumerator.Current.Value;
				}

				if (oldestTime > windowStartTime)
					break;

				if (ticksAtTime != null)
				{
					for (int index = 0; index < ticksAtTime.Count; index++)
						SubtractTickFromTotalUnsafe(ticksAtTime[index]);

					pruned += ticksAtTime.Count;
					activeProfileTickCount -= ticksAtTime.Count;
				}

				activeProfileTicksByTime.Remove(oldestTime);
			}
			return pruned;
		}

		private void AddActiveTickUnsafe(OrcaRollingProfileTick tick)
		{
			if (tick == null)
				return;

			if (activeProfileTicksByTime == null)
				activeProfileTicksByTime = new SortedDictionary<DateTime, List<OrcaRollingProfileTick>>();

			List<OrcaRollingProfileTick> ticksAtTime;
			if (!activeProfileTicksByTime.TryGetValue(tick.Time, out ticksAtTime))
			{
				ticksAtTime = new List<OrcaRollingProfileTick>();
				activeProfileTicksByTime[tick.Time] = ticksAtTime;
			}
			ticksAtTime.Add(tick);
			activeProfileTickCount++;
		}

		private void RebuildTotalProfileFromActiveTicksUnsafe()
		{
			totalProfile = new OrcaProfileBucket();
			if (activeProfileTicksByTime == null)
				return;

			foreach (KeyValuePair<DateTime, List<OrcaRollingProfileTick>> kvp in activeProfileTicksByTime)
			{
				List<OrcaRollingProfileTick> ticksAtTime = kvp.Value;
				if (ticksAtTime == null)
					continue;

				for (int index = 0; index < ticksAtTime.Count; index++)
				{
					OrcaRollingProfileTick tick = ticksAtTime[index];
					if (tick == null || tick.Volume <= 0)
						continue;

					if (totalProfile.VolByPrice.ContainsKey(tick.VolumePrice)) totalProfile.VolByPrice[tick.VolumePrice] += tick.Volume;
					else totalProfile.VolByPrice[tick.VolumePrice] = tick.Volume;

					if (tick.Delta != 0)
					{
						if (totalProfile.DeltaByPrice.ContainsKey(tick.DeltaPrice)) totalProfile.DeltaByPrice[tick.DeltaPrice] += tick.Delta;
						else totalProfile.DeltaByPrice[tick.DeltaPrice] = tick.Delta;
					}
				}
			}
		}

		private void GetActiveTickDiagnosticsUnsafe(DateTime windowStartTime, SortedDictionary<double, long> volumeByPrice, out DateTime minTime, out DateTime maxTime, out double minPrice, out double maxPrice, out int staleTickCount)
		{
			minTime = DateTime.MinValue;
			maxTime = DateTime.MinValue;
			minPrice = double.NaN;
			maxPrice = double.NaN;
			staleTickCount = 0;

			if (activeProfileTicksByTime != null && activeProfileTicksByTime.Count > 0)
			{
				using (IEnumerator<KeyValuePair<DateTime, List<OrcaRollingProfileTick>>> enumerator = activeProfileTicksByTime.GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						minTime = enumerator.Current.Key;
						if (minTime <= windowStartTime && enumerator.Current.Value != null)
							staleTickCount = enumerator.Current.Value.Count;
					}
				}
				maxTime = currentWindowEndTime;
			}

			if (volumeByPrice == null || volumeByPrice.Count == 0)
				return;

			using (IEnumerator<KeyValuePair<double, long>> enumerator = volumeByPrice.GetEnumerator())
			{
				if (enumerator.MoveNext())
					minPrice = enumerator.Current.Key;
			}

			foreach (double key in volumeByPrice.Keys)
				maxPrice = key;
		}

		private void SubtractTickFromTotalUnsafe(OrcaRollingProfileTick tick)
		{
			if (tick == null || totalProfile == null)
				return;

			SubtractFromMap(totalProfile.VolByPrice, tick.VolumePrice, tick.Volume, true);
			if (tick.Delta != 0)
				SubtractFromMap(totalProfile.DeltaByPrice, tick.DeltaPrice, tick.Delta, false);
		}

		private void SubtractFromMap(Dictionary<double, long> map, double key, long value, bool removeWhenNonPositive)
		{
			if (map == null || !map.ContainsKey(key))
				return;

			map[key] -= value;
			if ((removeWhenNonPositive && map[key] <= 0) || (!removeWhenNonPositive && map[key] == 0))
				map.Remove(key);
		}

		private double NormalizeToBucketStart(double price, int compressionTicks)
		{
			double tickSize = TickSize > 0 ? TickSize : 0.01;
			double normalized = Math.Round(price / tickSize, MidpointRounding.AwayFromZero) * tickSize;
			double bucketSize = Math.Max(1, compressionTicks) * tickSize;
			return Math.Floor(normalized / bucketSize + 1E-06) * bucketSize;
		}

		private void SubtractBucketFromTotal(OrcaProfileBucket removed)
		{
			if (removed == null || totalProfile == null) return;
			foreach (var kvp in removed.VolByPrice)
			{
				if (!totalProfile.VolByPrice.ContainsKey(kvp.Key)) continue;
				totalProfile.VolByPrice[kvp.Key] -= kvp.Value;
				if (totalProfile.VolByPrice[kvp.Key] <= 0) totalProfile.VolByPrice.Remove(kvp.Key);
			}
			foreach (var kvp in removed.DeltaByPrice)
			{
				if (!totalProfile.DeltaByPrice.ContainsKey(kvp.Key)) continue;
				totalProfile.DeltaByPrice[kvp.Key] -= kvp.Value;
				if (totalProfile.DeltaByPrice[kvp.Key] == 0) totalProfile.DeltaByPrice.Remove(kvp.Key);
			}
		}

		private void RebuildTotalProfileFromBuckets()
		{
			totalProfile = new OrcaProfileBucket();
			foreach (OrcaProfileBucket bucket in rollingHistory)
				AddBucketToTotal(bucket);
		}

		private void AddBucketToTotal(OrcaProfileBucket bucket)
		{
			if (bucket == null || totalProfile == null) return;
			foreach (var kvp in bucket.VolByPrice)
			{
				if (totalProfile.VolByPrice.ContainsKey(kvp.Key)) totalProfile.VolByPrice[kvp.Key] += kvp.Value;
				else totalProfile.VolByPrice[kvp.Key] = kvp.Value;
			}
			foreach (var kvp in bucket.DeltaByPrice)
			{
				if (totalProfile.DeltaByPrice.ContainsKey(kvp.Key)) totalProfile.DeltaByPrice[kvp.Key] += kvp.Value;
				else totalProfile.DeltaByPrice[kvp.Key] = kvp.Value;
			}
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxResourceRenderTarget != IntPtr.Zero && dxResourceRenderTarget != currentTarget)
				DisposeDx();

			if (volBrushDx == null) volBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VolumeBrush, VolumeOpacity));
			if (pocBrushDx == null) pocBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(POCBrush, 1f));
			if (posDeltaBrushDx == null) posDeltaBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(PositiveDeltaBrush, DeltaOpacity));
			if (negDeltaBrushDx == null) negDeltaBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaBrush, DeltaOpacity));
			if (vaVolBrushDx == null) vaVolBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VABrush, VolumeOpacity));
			if (vaLineBrushDx == null) vaLineBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VALineBrush, 1f));
			if (vaLineStrokeDx == null) vaLineStrokeDx = new SharpDX.Direct2D1.StrokeStyle(RenderTarget.Factory, new SharpDX.Direct2D1.StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dash });
			if (deltaTextBrushDx == null) deltaTextBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaTextBrush, 1f));
			if (negativeDeltaTextBrushDx == null) negativeDeltaTextBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaTextBrush, 1f));
			if (labelBgBrushDx == null) labelBgBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaLabelBgBrush, 1f));
			if (sourceLabelBrushDx == null) sourceLabelBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.92f, 0.95f, 1.0f, 1.0f));
			if (sourceLabelBgBrushDx == null) sourceLabelBgBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.02f, 0.02f, 0.02f, 0.78f));

			if (deltaTextFormatDx == null)
			{
				deltaTextFormatDx = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, DeltaTextFontSize)
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
					ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center
				};
			}
			if (sourceLabelTextFormatDx == null)
			{
				sourceLabelTextFormatDx = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 11f)
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
					ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center
				};
			}

			if (UseGradient && (volGradientBrushes == null || lastBuiltGradientSteps != GradientSteps))
			{
				BuildGradient(VolumeBrush, VolumeOpacity, ref volGradientBrushes);
				lastBuiltGradientSteps = GradientSteps;
			}
			if (UseGradient && ShowValueArea && ShowVAColor && (vaGradientBrushes == null || lastBuiltVAGradientSteps != GradientSteps))
			{
				BuildGradient(VABrush, VolumeOpacity, ref vaGradientBrushes);
				lastBuiltVAGradientSteps = GradientSteps;
			}
			int deltaIntensitySteps = Math.Max(2, GradientSteps);
			if (UseDeltaIntensityColoring && (positiveDeltaIntensityBrushes == null || negativeDeltaIntensityBrushes == null || lastBuiltDeltaIntensitySteps != deltaIntensitySteps || Math.Abs(lastBuiltDeltaIntensityMinOpacity - DeltaIntensityMinOpacity) > 0.0001f || Math.Abs(lastBuiltDeltaIntensityMaxOpacity - DeltaOpacity) > 0.0001f))
			{
				DisposePalette(ref positiveDeltaIntensityBrushes);
				DisposePalette(ref negativeDeltaIntensityBrushes);
				positiveDeltaIntensityBrushes = BuildDeltaIntensityPalette(PositiveDeltaBrush, deltaIntensitySteps, DeltaOpacity);
				negativeDeltaIntensityBrushes = BuildDeltaIntensityPalette(NegativeDeltaBrush, deltaIntensitySteps, DeltaOpacity);
				lastBuiltDeltaIntensitySteps = deltaIntensitySteps;
				lastBuiltDeltaIntensityMinOpacity = DeltaIntensityMinOpacity;
				lastBuiltDeltaIntensityMaxOpacity = DeltaOpacity;
			}
			else if (!UseDeltaIntensityColoring && (positiveDeltaIntensityBrushes != null || negativeDeltaIntensityBrushes != null))
			{
				DisposePalette(ref positiveDeltaIntensityBrushes);
				DisposePalette(ref negativeDeltaIntensityBrushes);
				lastBuiltDeltaIntensitySteps = -1;
				lastBuiltDeltaIntensityMinOpacity = -1f;
				lastBuiltDeltaIntensityMaxOpacity = -1f;
			}
			dxResourceRenderTarget = currentTarget;
		}

		private void BuildGradient(System.Windows.Media.Brush wpfBrush, float opacity, ref SharpDX.Direct2D1.SolidColorBrush[] palette)
		{
			if (palette != null) foreach (var b in palette) if (b != null) b.Dispose();
			palette = new SharpDX.Direct2D1.SolidColorBrush[GradientSteps];
			Color4 baseCol = ToDxColor(wpfBrush, opacity);
			for (int i = 0; i < GradientSteps; i++)
			{
				float ratio = (float)i / (float)(GradientSteps - 1);
				float b = MinBrightness + (1f - MinBrightness) * ratio;
				palette[i] = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(baseCol.Red * b, baseCol.Green * b, baseCol.Blue * b, opacity));
			}
		}

		private SharpDX.Direct2D1.SolidColorBrush[] BuildDeltaIntensityPalette(System.Windows.Media.Brush wpfBrush, int steps, float maxOpacity)
		{
			SharpDX.Direct2D1.SolidColorBrush[] palette = new SharpDX.Direct2D1.SolidColorBrush[steps];
			Color4 baseCol = ToDxColor(wpfBrush, 1f);
			float minOpacity = Math.Max(0f, Math.Min(1f, DeltaIntensityMinOpacity));
			for (int i = 0; i < steps; i++)
			{
				float ratio = (float)i / (float)(steps - 1);
				float opacity = maxOpacity * (minOpacity + ((1f - minOpacity) * ratio));
				palette[i] = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(baseCol.Red, baseCol.Green, baseCol.Blue, baseCol.Alpha * opacity));
			}
			return palette;
		}

		private SharpDX.Direct2D1.SolidColorBrush SelectDeltaBrush(long delta, long maxAbsDelta)
		{
			if (!UseDeltaIntensityColoring || maxAbsDelta <= 0)
				return delta >= 0 ? posDeltaBrushDx : negDeltaBrushDx;

			SharpDX.Direct2D1.SolidColorBrush[] palette = delta >= 0 ? positiveDeltaIntensityBrushes : negativeDeltaIntensityBrushes;
			if (palette == null || palette.Length == 0)
				return delta >= 0 ? posDeltaBrushDx : negDeltaBrushDx;

			double intensity = Math.Abs((double)delta) / Math.Max(1.0, (double)maxAbsDelta);
			int index = (int)Math.Round(intensity * (palette.Length - 1));
			if (index < 0) index = 0;
			if (index >= palette.Length) index = palette.Length - 1;
			return palette[index];
		}

		private void DisposePalette(ref SharpDX.Direct2D1.SolidColorBrush[] palette)
		{
			if (palette != null)
			{
				foreach (var brush in palette)
					if (brush != null) brush.Dispose();
			}
			palette = null;
		}

		private Color4 ToDxColor(System.Windows.Media.Brush wpfBrush, float opacity)
		{
			var scb = wpfBrush as System.Windows.Media.SolidColorBrush;
			if (scb != null) return new Color4((float)scb.Color.R / 255f, (float)scb.Color.G / 255f, (float)scb.Color.B / 255f, (float)scb.Color.A / 255f * opacity);
			return new Color4(1f, 1f, 1f, opacity);
		}

		private float MeasureTextWidth(string text)
		{
			if (deltaTextFormatDx == null) return 0f;
			if (textWidthCache.TryGetValue(text, out float width)) return width;
			using (var layout = new TextLayout(Core.Globals.DirectWriteFactory, text, deltaTextFormatDx, 1000, 100))
			{
				width = layout.Metrics.Width;
				textWidthCache[text] = width;
				return width;
			}
		}

		private void DrawDataSourceLabel()
		{
			if (!ShowDataSourceLabel || string.IsNullOrEmpty(dataSourceLabel) || RenderTarget == null || sourceLabelTextFormatDx == null || sourceLabelBrushDx == null || sourceLabelBgBrushDx == null || ChartPanel == null)
				return;

			float width = Math.Max(132f, EstimateSourceLabelWidth(dataSourceLabel) + 12f);
			float height = 18f;
			float left = (float)ChartPanel.X + 8f;
			float top = (float)(ChartPanel.Y + ChartPanel.H) - height - 8f;
			if (top < ChartPanel.Y + 4f)
				top = (float)ChartPanel.Y + 4f;

			RectangleF bgRect = new RectangleF(left, top, width, height);
			RenderTarget.FillRectangle(bgRect, sourceLabelBgBrushDx);
			RenderTarget.DrawText(dataSourceLabel, sourceLabelTextFormatDx, new RectangleF(left + 6f, top, width - 8f, height), sourceLabelBrushDx);
		}

		private float EstimateSourceLabelWidth(string text)
		{
			if (string.IsNullOrEmpty(text))
				return 0f;
			return Math.Max(80f, text.Length * 6.2f);
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				EnsureDxResources();
				SortedDictionary<double, long> volumeByPrice;
				SortedDictionary<double, long> deltaByPrice;
				DateTime windowStartTime;
				DateTime windowEndTime;
				DateTime activeMinTime;
				DateTime activeMaxTime;
				double activeMinPrice;
				double activeMaxPrice;
				int tickCount;
				int staleTickCount;
				int prunedTickCount;
				long buildSequence;
				bool isHydrating;
				lock (profileDataSync)
				{
					isHydrating = (addLocalTickSeries && localTickSeriesHydrating && !localTickSeriesReady) || (UseSharedProfileDataProvider && sharedProviderHydrating);
					if (isHydrating)
					{
						if (addLocalTickSeries)
							dataSourceLabel = "Source: local tick cache hydrating";
						DrawDataSourceLabel();
						return;
					}

					if (totalProfile == null || totalProfile.VolByPrice.Count == 0)
					{
						DrawDataSourceLabel();
						return;
					}
					volumeByPrice = new SortedDictionary<double, long>(totalProfile.VolByPrice);
					deltaByPrice = new SortedDictionary<double, long>(totalProfile.DeltaByPrice);
					windowStartTime = currentWindowStartTime;
					windowEndTime = currentWindowEndTime;
					tickCount = activeProfileTickCount;
					GetActiveTickDiagnosticsUnsafe(windowStartTime, volumeByPrice, out activeMinTime, out activeMaxTime, out activeMinPrice, out activeMaxPrice, out staleTickCount);
					prunedTickCount = lastPrunedActiveTicks;
					buildSequence = profileBuildSequence;
				}

				float chartY = ChartPanel.Y;
				float chartH = ChartPanel.H;
				float canvasX = ChartPanel.X + ChartPanel.W - RightOffsetPx;

				// ── Compute dynamic delta bucket size ──────────────────────────
				int dynamicDeltaComp;
				if (UseDynamicAggregation)
				{
					double visibleTicks = (chartScale.MaxValue - chartScale.MinValue) / TickSize;
					double ticksPerPixel = visibleTicks / Math.Max(1, chartH);
					double desiredTicks = ticksPerPixel * Math.Max(1, DeltaDynamicRowMinPixels) * DynamicAggregationMultiplier;

					if (desiredTicks <= 1) dynamicDeltaComp = 1;
					else if (desiredTicks <= 2) dynamicDeltaComp = 2;
					else if (desiredTicks <= 4) dynamicDeltaComp = 4;
					else if (desiredTicks <= 5) dynamicDeltaComp = 5;
					else if (desiredTicks <= 8) dynamicDeltaComp = 8;
					else if (desiredTicks <= 10) dynamicDeltaComp = 10;
					else if (desiredTicks <= 15) dynamicDeltaComp = 15;
					else if (desiredTicks <= 20) dynamicDeltaComp = 20;
					else if (desiredTicks <= 25) dynamicDeltaComp = 25;
					else if (desiredTicks <= 30) dynamicDeltaComp = 30;
					else if (desiredTicks <= 40) dynamicDeltaComp = 40;
					else if (desiredTicks <= 50) dynamicDeltaComp = 50;
					else if (desiredTicks <= 100) dynamicDeltaComp = (int)(Math.Round(desiredTicks / 20.0) * 20);
					else dynamicDeltaComp = (int)(Math.Round(desiredTicks / 50.0) * 50);
					dynamicDeltaComp = ClampDeltaCompression(dynamicDeltaComp);

					// Hysteresis: only change if deviation is significant
					if (lastDynamicDeltaComp > 0 && Math.Abs(dynamicDeltaComp - lastDynamicDeltaComp) < Math.Max(2, dynamicDeltaComp * 0.15))
						dynamicDeltaComp = lastDynamicDeltaComp;
					else
						lastDynamicDeltaComp = dynamicDeltaComp;
				}
				else
				{
					dynamicDeltaComp = DeltaTickCompression;
				}

				// ── Volume profile ─────────────────────────────────────────────
				long maxVol = 0;
				double pocPrice = 0;
				long totalVolume = 0;
				foreach (var kvp in volumeByPrice)
				{
					totalVolume += kvp.Value;
					if (kvp.Value > maxVol) { maxVol = kvp.Value; pocPrice = kvp.Key; }
				}

				double vah = 0, val = 0;
				bool vaFound = false;
				if (ShowValueArea && maxVol > 0)
				{
					vaFound = CalculateValueArea(volumeByPrice, pocPrice, out vah, out val);
				}
				long totalDelta = 0;
				foreach (long deltaValue in deltaByPrice.Values)
					totalDelta += deltaValue;
				PrintProfileDebug("render", windowStartTime, windowEndTime, activeMinTime, activeMaxTime, activeMinPrice, activeMaxPrice, tickCount, staleTickCount, prunedTickCount, volumeByPrice.Count, totalVolume, totalDelta, pocPrice, vah, val, maxVol, buildSequence, volumeByPrice, deltaByPrice);

				double vTick = VolumeTickCompression * TickSize;
				if (ShowVolume && maxVol > 0)
				{
					foreach (var kvp in volumeByPrice)
					{
						int y1 = chartScale.GetYByValue(kvp.Key + vTick);
						int y2 = chartScale.GetYByValue(kvp.Key);
						if (y2 < chartY || y1 > chartY + chartH) continue;

						float h = Math.Max(1, Math.Abs(y2 - y1) - ProfileBarSpacingPx);
						float y = Math.Min(y1, y2) + (float)ProfileBarSpacingPx / 2f;
						float w = (float)(ProfileWidthPx * ((double)kvp.Value / (double)maxVol));
						if (w < 1) continue;

						bool inVA = vaFound && kvp.Key >= val - 1E-07 && kvp.Key <= vah + 1E-07;
						SharpDX.Direct2D1.Brush brush = inVA && ShowVAColor ? vaVolBrushDx : volBrushDx;
						if (UseGradient)
						{
							var palette = inVA && ShowVAColor ? vaGradientBrushes : volGradientBrushes;
							if (palette != null)
							{
								int idx = (int)((double)kvp.Value / (double)maxVol * (palette.Length - 1));
								brush = palette[idx];
							}
						}
						if (ShowPOC && Math.Abs(kvp.Key - pocPrice) < 1E-07) brush = pocBrushDx;

						RenderTarget.FillRectangle(new RectangleF(canvasX - w, y, w, h), brush);
					}
				}

				// ── Delta profile (dynamically re-grouped at render time) ───────
				if (ShowDelta && deltaByPrice.Count > 0)
				{
					double deltaComp = dynamicDeltaComp * TickSize;
					HashSet<double> volumeBucketsForDelta = null;
					if (ShowVolume)
					{
						volumeBucketsForDelta = new HashSet<double>();
						foreach (var kvp in volumeByPrice)
						{
							double startPrice = Math.Floor(kvp.Key / deltaComp + 1E-06) * deltaComp;
							double endPrice = Math.Floor((kvp.Key + vTick - TickSize) / deltaComp + 1E-06) * deltaComp;
							for (double bPrice = startPrice; bPrice <= endPrice + 1E-07; bPrice += deltaComp)
								volumeBucketsForDelta.Add(bPrice);
						}
					}

					// When volume is visible, keep delta buckets clipped to the volume profile.
					var groupedDelta = new SortedDictionary<double, long>();
					foreach (var kvp in deltaByPrice)
					{
						double bPrice = Math.Floor(kvp.Key / deltaComp + 1E-06) * deltaComp;
						if (volumeBucketsForDelta != null && !volumeBucketsForDelta.Contains(bPrice)) continue;

						if (groupedDelta.ContainsKey(bPrice)) groupedDelta[bPrice] += kvp.Value;
						else groupedDelta[bPrice] = kvp.Value;
					}

					long maxDelta = 0;
					foreach (var d in groupedDelta.Values) if (Math.Abs(d) > maxDelta) maxDelta = Math.Abs(d);

					if (maxDelta > 0)
					{
						foreach (var kvp in groupedDelta)
						{
							int y1 = chartScale.GetYByValue(kvp.Key + deltaComp);
							int y2 = chartScale.GetYByValue(kvp.Key);
							if (y2 < chartY || y1 > chartY + chartH) continue;

							float h = Math.Max(1, Math.Abs(y2 - y1) - ProfileBarSpacingPx);
							float y = Math.Min(y1, y2) + (float)ProfileBarSpacingPx / 2f;
							float w = (float)(DeltaWidthPx * ((double)Math.Abs(kvp.Value) / (double)maxDelta));
							if (w < 1f) continue;

							SharpDX.Direct2D1.SolidColorBrush brush = SelectDeltaBrush(kvp.Value, maxDelta);
							bool deltaTowardCandles = DeltaDirection == RollingDeltaDirection.TowardCandles;
							float deltaX = deltaTowardCandles ? canvasX - w : canvasX;
							RenderTarget.FillRectangle(new RectangleF(deltaX, y, w, h), brush);

							if (ShowDeltaText && Math.Abs(kvp.Value) >= DeltaTextMinThreshold && h >= DeltaTextFontSize + 2)
							{
								string lbl = kvp.Value.ToString("+#;-#;0");
								float textWidth = MeasureTextWidth(lbl);
								if (w < textWidth + 4f) continue;

								float tX = deltaTowardCandles ? canvasX - textWidth - 2f : canvasX + 2f;
								float tY = y + (h / 2f) - (DeltaTextFontSize / 2f);
								if (ShowDeltaLabelBackground)
									RenderTarget.FillRectangle(new RectangleF(tX - 1, tY - 1, textWidth + 2, DeltaTextFontSize + 2), labelBgBrushDx);
								SharpDX.Direct2D1.SolidColorBrush labelBrush = kvp.Value >= 0 ? deltaTextBrushDx : negativeDeltaTextBrushDx;
								if (labelBrush != null)
									RenderTarget.DrawText(lbl, deltaTextFormatDx, new RectangleF(tX, tY, textWidth + 4f, DeltaTextFontSize + 2), labelBrush);
							}
						}
					}
				}
				DrawDataSourceLabel();
			}
			catch { }
		}

		private bool CalculateValueArea(IDictionary<double, long> map, double poc, out double vah, out double val)
		{
			vah = poc; val = poc;
			if (map.Count <= 1) return false;
			List<double> prices = new List<double>(map.Keys);
			prices.Sort();
			long total = 0;
			foreach (var v in map.Values) total += v;
			double target = total * ((double)ValueAreaPercent / 100.0);
			long current = map[poc];
			int iH = prices.IndexOf(poc);
			int iL = iH;
			while (current < target && (iH < prices.Count - 1 || iL > 0))
			{
				long vH = iH < prices.Count - 1 ? map[prices[iH + 1]] : 0;
				long vL = iL > 0 ? map[prices[iL - 1]] : 0;
				if (vH >= vL) { iH++; current += vH; }
				else { iL--; current += vL; }
			}
			vah = prices[iH];
			val = prices[iL];
			return true;
		}

		private int ClampDeltaCompression(int compression)
		{
			int min = Math.Max(1, DynamicDeltaMinCompression);
			int max = Math.Max(min, DynamicDeltaMaxCompression);
			if (compression < min) return min;
			if (compression > max) return max;
			return compression;
		}

		private void PrintProfileDebugUnsafe(string reason)
		{
			if (totalProfile == null)
				return;

			SortedDictionary<double, long> volumeByPrice = new SortedDictionary<double, long>(totalProfile.VolByPrice);
			SortedDictionary<double, long> deltaByPrice = new SortedDictionary<double, long>(totalProfile.DeltaByPrice);
			long totalVolume = 0;
			long maxVol = 0;
			double pocPrice = 0;
			foreach (var kvp in volumeByPrice)
			{
				totalVolume += kvp.Value;
				if (kvp.Value > maxVol)
				{
					maxVol = kvp.Value;
					pocPrice = kvp.Key;
				}
			}

			double vah = 0;
			double val = 0;
			if (ShowValueArea && maxVol > 0)
				CalculateValueArea(volumeByPrice, pocPrice, out vah, out val);

			long totalDelta = 0;
			foreach (long value in deltaByPrice.Values)
				totalDelta += value;

			DateTime activeMinTime;
			DateTime activeMaxTime;
			double activeMinPrice;
			double activeMaxPrice;
			int staleTickCount;
			GetActiveTickDiagnosticsUnsafe(currentWindowStartTime, volumeByPrice, out activeMinTime, out activeMaxTime, out activeMinPrice, out activeMaxPrice, out staleTickCount);
			PrintProfileDebug(reason, currentWindowStartTime, currentWindowEndTime, activeMinTime, activeMaxTime, activeMinPrice, activeMaxPrice, activeProfileTickCount, staleTickCount, lastPrunedActiveTicks, volumeByPrice.Count, totalVolume, totalDelta, pocPrice, vah, val, maxVol, profileBuildSequence, volumeByPrice, deltaByPrice);
		}

		private void PrintProfileDebug(string reason, DateTime windowStartTime, DateTime windowEndTime, DateTime activeMinTime, DateTime activeMaxTime, double activeMinPrice, double activeMaxPrice, int tickCount, int staleTickCount, int prunedTickCount, int rowCount, long totalVolume, long totalDelta, double pocPrice, double vah, double val, long maxRowVolume, long buildSequence, SortedDictionary<double, long> volumeByPrice, SortedDictionary<double, long> deltaByPrice)
		{
			if (!DebugProfileSignatures)
				return;

			DateTime now = DateTime.UtcNow;
			if (lastDebugPrintUtc != DateTime.MinValue && (now - lastDebugPrintUtc).TotalSeconds < 1.0)
				return;

			string signature = BuildProfileSignature(windowStartTime, windowEndTime, totalVolume, totalDelta, rowCount, pocPrice, vah, val, volumeByPrice, deltaByPrice);
			if (signature == lastDebugProfileSignature)
				return;

			lastDebugPrintUtc = now;
			lastDebugProfileSignature = signature;
			Print("[" + DateTime.Now.ToString("HH:mm:ss") + "] OrcaRollingProfiles "
				+ reason
				+ " seq=" + buildSequence
				+ " period=" + GetPeriodMinutes() + "m"
				+ " source=" + dataSourceLabel
				+ " ready=" + (!localTickSeriesHydrating && !sharedProviderHydrating)
				+ " windowStart=" + FormatDebugTime(windowStartTime)
				+ " windowEnd=" + FormatDebugTime(windowEndTime)
				+ " activeMinTime=" + FormatDebugTime(activeMinTime)
				+ " activeMaxTime=" + FormatDebugTime(activeMaxTime)
				+ " activeMinPrice=" + FormatDebugPrice(activeMinPrice)
				+ " activeMaxPrice=" + FormatDebugPrice(activeMaxPrice)
				+ " ticks=" + tickCount
				+ " staleTicks=" + staleTickCount
				+ " pruned=" + prunedTickCount
				+ " droppedStale=" + lastDroppedStaleTicks
				+ " rows=" + rowCount
				+ " totalVol=" + totalVolume
				+ " totalDelta=" + totalDelta
				+ " poc=" + FormatDebugPrice(pocPrice)
				+ " vah=" + FormatDebugPrice(vah)
				+ " val=" + FormatDebugPrice(val)
				+ " maxRowVol=" + maxRowVolume
				+ " signature=" + signature);
		}

		private string BuildProfileSignature(DateTime windowStartTime, DateTime windowEndTime, long totalVolume, long totalDelta, int rowCount, double pocPrice, double vah, double val, SortedDictionary<double, long> volumeByPrice, SortedDictionary<double, long> deltaByPrice)
		{
			unchecked
			{
				ulong hash = 14695981039346656037UL;
				AddHash(ref hash, windowStartTime.Ticks);
				AddHash(ref hash, windowEndTime.Ticks);
				AddHash(ref hash, totalVolume);
				AddHash(ref hash, totalDelta);
				AddHash(ref hash, rowCount);
				AddHash(ref hash, PriceToTicks(pocPrice));
				AddHash(ref hash, PriceToTicks(vah));
				AddHash(ref hash, PriceToTicks(val));

				foreach (var kvp in volumeByPrice)
				{
					AddHash(ref hash, PriceToTicks(kvp.Key));
					AddHash(ref hash, kvp.Value);
				}

				foreach (var kvp in deltaByPrice)
				{
					AddHash(ref hash, PriceToTicks(kvp.Key));
					AddHash(ref hash, kvp.Value);
				}

				return hash.ToString("X16");
			}
		}

		private void AddHash(ref ulong hash, long value)
		{
			unchecked
			{
				ulong data = (ulong)value;
				for (int i = 0; i < 8; i++)
				{
					hash ^= (byte)(data & 0xFF);
					hash *= 1099511628211UL;
					data >>= 8;
				}
			}
		}

		private long PriceToTicks(double price)
		{
			double tickSize = TickSize > 0 ? TickSize : 0.01;
			return (long)Math.Round(price / tickSize, MidpointRounding.AwayFromZero);
		}

		private string FormatDebugTime(DateTime time)
		{
			return time == DateTime.MinValue ? "n/a" : time.ToString("yyyy-MM-dd HH:mm:ss.fff");
		}

		private string FormatDebugPrice(double price)
		{
			if (double.IsNaN(price) || double.IsInfinity(price))
				return "n/a";
			return price.ToString("0.########");
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaRollingProfiles[] cacheOrcaRollingProfiles;
		public OrcaRollingProfiles OrcaRollingProfiles(RollingProfilePeriod period, ProfileOperatingMode mode, bool useSharedProfileDataProvider, bool enableSharedProviderHistoricalBackfill, bool useLocalTickSeriesCache, bool showDataSourceLabel, int sharedProviderMaxTicksPerRender, int sharedProviderMaxBackfillMinutes, bool debugProfileSignatures, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, int deltaTickCompression)
		{
			return OrcaRollingProfiles(Input, period, mode, useSharedProfileDataProvider, enableSharedProviderHistoricalBackfill, useLocalTickSeriesCache, showDataSourceLabel, sharedProviderMaxTicksPerRender, sharedProviderMaxBackfillMinutes, debugProfileSignatures, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, deltaTickCompression);
		}

		public OrcaRollingProfiles OrcaRollingProfiles(ISeries<double> input, RollingProfilePeriod period, ProfileOperatingMode mode, bool useSharedProfileDataProvider, bool enableSharedProviderHistoricalBackfill, bool useLocalTickSeriesCache, bool showDataSourceLabel, int sharedProviderMaxTicksPerRender, int sharedProviderMaxBackfillMinutes, bool debugProfileSignatures, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, int deltaTickCompression)
		{
			if (cacheOrcaRollingProfiles != null)
				for (int idx = 0; idx < cacheOrcaRollingProfiles.Length; idx++)
					if (cacheOrcaRollingProfiles[idx] != null && cacheOrcaRollingProfiles[idx].Period == period && cacheOrcaRollingProfiles[idx].Mode == mode && cacheOrcaRollingProfiles[idx].UseSharedProfileDataProvider == useSharedProfileDataProvider && cacheOrcaRollingProfiles[idx].EnableSharedProviderHistoricalBackfill == enableSharedProviderHistoricalBackfill && cacheOrcaRollingProfiles[idx].UseLocalTickSeriesCache == useLocalTickSeriesCache && cacheOrcaRollingProfiles[idx].ShowDataSourceLabel == showDataSourceLabel && cacheOrcaRollingProfiles[idx].SharedProviderMaxTicksPerRender == sharedProviderMaxTicksPerRender && cacheOrcaRollingProfiles[idx].SharedProviderMaxBackfillMinutes == sharedProviderMaxBackfillMinutes && cacheOrcaRollingProfiles[idx].DebugProfileSignatures == debugProfileSignatures && cacheOrcaRollingProfiles[idx].MinutesPerDay == minutesPerDay && cacheOrcaRollingProfiles[idx].RthStartTime == rthStartTime && cacheOrcaRollingProfiles[idx].RthEndTime == rthEndTime && cacheOrcaRollingProfiles[idx].VolumeTickCompression == volumeTickCompression && cacheOrcaRollingProfiles[idx].ProfileWidthPx == profileWidthPx && cacheOrcaRollingProfiles[idx].DeltaWidthPx == deltaWidthPx && cacheOrcaRollingProfiles[idx].RightOffsetPx == rightOffsetPx && cacheOrcaRollingProfiles[idx].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaRollingProfiles[idx].DeltaDirection == deltaDirection && cacheOrcaRollingProfiles[idx].ShowVolume == showVolume && cacheOrcaRollingProfiles[idx].ShowDelta == showDelta && cacheOrcaRollingProfiles[idx].ShowPOC == showPOC && cacheOrcaRollingProfiles[idx].UseGradient == useGradient && cacheOrcaRollingProfiles[idx].GradientSteps == gradientSteps && cacheOrcaRollingProfiles[idx].MinBrightness == minBrightness && cacheOrcaRollingProfiles[idx].ShowValueArea == showValueArea && cacheOrcaRollingProfiles[idx].ShowVAColor == showVAColor && cacheOrcaRollingProfiles[idx].ShowVALines == showVALines && cacheOrcaRollingProfiles[idx].ValueAreaPercent == valueAreaPercent && cacheOrcaRollingProfiles[idx].VALineThickness == vALineThickness && cacheOrcaRollingProfiles[idx].VolumeOpacity == volumeOpacity && cacheOrcaRollingProfiles[idx].DeltaOpacity == deltaOpacity && cacheOrcaRollingProfiles[idx].UseDeltaIntensityColoring == useDeltaIntensityColoring && cacheOrcaRollingProfiles[idx].DeltaIntensityMinOpacity == deltaIntensityMinOpacity && cacheOrcaRollingProfiles[idx].ShowDeltaText == showDeltaText && cacheOrcaRollingProfiles[idx].ShowDeltaLabelBackground == showDeltaLabelBackground && cacheOrcaRollingProfiles[idx].DeltaTextMinThreshold == deltaTextMinThreshold && cacheOrcaRollingProfiles[idx].DeltaTextFontSize == deltaTextFontSize && cacheOrcaRollingProfiles[idx].UseDynamicAggregation == useDynamicAggregation && cacheOrcaRollingProfiles[idx].DynamicAggregationMultiplier == dynamicAggregationMultiplier && cacheOrcaRollingProfiles[idx].DeltaDynamicRowMinPixels == deltaDynamicRowMinPixels && cacheOrcaRollingProfiles[idx].DynamicDeltaMinCompression == dynamicDeltaMinCompression && cacheOrcaRollingProfiles[idx].DynamicDeltaMaxCompression == dynamicDeltaMaxCompression && cacheOrcaRollingProfiles[idx].DeltaTickCompression == deltaTickCompression && cacheOrcaRollingProfiles[idx].EqualsInput(input))
						return cacheOrcaRollingProfiles[idx];
			return CacheIndicator<OrcaRollingProfiles>(new OrcaRollingProfiles(){ Period = period, Mode = mode, UseSharedProfileDataProvider = useSharedProfileDataProvider, EnableSharedProviderHistoricalBackfill = enableSharedProviderHistoricalBackfill, UseLocalTickSeriesCache = useLocalTickSeriesCache, ShowDataSourceLabel = showDataSourceLabel, SharedProviderMaxTicksPerRender = sharedProviderMaxTicksPerRender, SharedProviderMaxBackfillMinutes = sharedProviderMaxBackfillMinutes, DebugProfileSignatures = debugProfileSignatures, MinutesPerDay = minutesPerDay, RthStartTime = rthStartTime, RthEndTime = rthEndTime, VolumeTickCompression = volumeTickCompression, ProfileWidthPx = profileWidthPx, DeltaWidthPx = deltaWidthPx, RightOffsetPx = rightOffsetPx, ProfileBarSpacingPx = profileBarSpacingPx, DeltaDirection = deltaDirection, ShowVolume = showVolume, ShowDelta = showDelta, ShowPOC = showPOC, UseGradient = useGradient, GradientSteps = gradientSteps, MinBrightness = minBrightness, ShowValueArea = showValueArea, ShowVAColor = showVAColor, ShowVALines = showVALines, ValueAreaPercent = valueAreaPercent, VALineThickness = vALineThickness, VolumeOpacity = volumeOpacity, DeltaOpacity = deltaOpacity, UseDeltaIntensityColoring = useDeltaIntensityColoring, DeltaIntensityMinOpacity = deltaIntensityMinOpacity, ShowDeltaText = showDeltaText, ShowDeltaLabelBackground = showDeltaLabelBackground, DeltaTextMinThreshold = deltaTextMinThreshold, DeltaTextFontSize = deltaTextFontSize, UseDynamicAggregation = useDynamicAggregation, DynamicAggregationMultiplier = dynamicAggregationMultiplier, DeltaDynamicRowMinPixels = deltaDynamicRowMinPixels, DynamicDeltaMinCompression = dynamicDeltaMinCompression, DynamicDeltaMaxCompression = dynamicDeltaMaxCompression, DeltaTickCompression = deltaTickCompression }, input, ref cacheOrcaRollingProfiles);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaRollingProfiles OrcaRollingProfiles(RollingProfilePeriod period, ProfileOperatingMode mode, bool useSharedProfileDataProvider, bool enableSharedProviderHistoricalBackfill, bool useLocalTickSeriesCache, bool showDataSourceLabel, int sharedProviderMaxTicksPerRender, int sharedProviderMaxBackfillMinutes, bool debugProfileSignatures, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, int deltaTickCompression)
		{
			return indicator.OrcaRollingProfiles(Input, period, mode, useSharedProfileDataProvider, enableSharedProviderHistoricalBackfill, useLocalTickSeriesCache, showDataSourceLabel, sharedProviderMaxTicksPerRender, sharedProviderMaxBackfillMinutes, debugProfileSignatures, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, deltaTickCompression);
		}

		public Indicators.OrcaRollingProfiles OrcaRollingProfiles(ISeries<double> input , RollingProfilePeriod period, ProfileOperatingMode mode, bool useSharedProfileDataProvider, bool enableSharedProviderHistoricalBackfill, bool useLocalTickSeriesCache, bool showDataSourceLabel, int sharedProviderMaxTicksPerRender, int sharedProviderMaxBackfillMinutes, bool debugProfileSignatures, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, int deltaTickCompression)
		{
			return indicator.OrcaRollingProfiles(input, period, mode, useSharedProfileDataProvider, enableSharedProviderHistoricalBackfill, useLocalTickSeriesCache, showDataSourceLabel, sharedProviderMaxTicksPerRender, sharedProviderMaxBackfillMinutes, debugProfileSignatures, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, deltaTickCompression);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaRollingProfiles OrcaRollingProfiles(RollingProfilePeriod period, ProfileOperatingMode mode, bool useSharedProfileDataProvider, bool enableSharedProviderHistoricalBackfill, bool useLocalTickSeriesCache, bool showDataSourceLabel, int sharedProviderMaxTicksPerRender, int sharedProviderMaxBackfillMinutes, bool debugProfileSignatures, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, int deltaTickCompression)
		{
			return indicator.OrcaRollingProfiles(Input, period, mode, useSharedProfileDataProvider, enableSharedProviderHistoricalBackfill, useLocalTickSeriesCache, showDataSourceLabel, sharedProviderMaxTicksPerRender, sharedProviderMaxBackfillMinutes, debugProfileSignatures, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, deltaTickCompression);
		}

		public Indicators.OrcaRollingProfiles OrcaRollingProfiles(ISeries<double> input , RollingProfilePeriod period, ProfileOperatingMode mode, bool useSharedProfileDataProvider, bool enableSharedProviderHistoricalBackfill, bool useLocalTickSeriesCache, bool showDataSourceLabel, int sharedProviderMaxTicksPerRender, int sharedProviderMaxBackfillMinutes, bool debugProfileSignatures, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, int deltaTickCompression)
		{
			return indicator.OrcaRollingProfiles(input, period, mode, useSharedProfileDataProvider, enableSharedProviderHistoricalBackfill, useLocalTickSeriesCache, showDataSourceLabel, sharedProviderMaxTicksPerRender, sharedProviderMaxBackfillMinutes, debugProfileSignatures, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, deltaTickCompression);
		}
	}
}

#endregion
