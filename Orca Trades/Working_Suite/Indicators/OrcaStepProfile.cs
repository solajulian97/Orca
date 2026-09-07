#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Core.FloatingPoint;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

using WpfBrush  = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors  = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum StepHistoricalDeltaTextMode { FollowGlobal, Filtered, HoverOnly, Hidden }

	public enum StepIntervalType
	{
		Minutes5  = 5,
		Minutes10 = 10,
		Minutes15 = 15,
		Minutes30 = 30,
		Hour1     = 60,
		Hours2    = 120,
		Hours4    = 240,
		Daily     = 1440
	}

	public enum StepProfileBasis
	{
		Time = 0,
		Volume = 1,
		Session = 2
	}

	public class StepProfileBasisConverter : EnumConverter
	{
		public StepProfileBasisConverter() : base(typeof(StepProfileBasis)) { }

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is StepProfileBasis)
			{
				switch ((StepProfileBasis)value)
				{
					case StepProfileBasis.Volume: return "Volume Amount";
					case StepProfileBasis.Session: return "Session";
					default: return "Time";
				}
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (string.Equals(text, "Volume Amount", StringComparison.OrdinalIgnoreCase))
				return StepProfileBasis.Volume;
			if (string.Equals(text, "Session", StringComparison.OrdinalIgnoreCase))
				return StepProfileBasis.Session;
			if (string.Equals(text, "Time", StringComparison.OrdinalIgnoreCase))
				return StepProfileBasis.Time;

			return base.ConvertFrom(context, culture, value);
		}
	}

	public enum StepSessionConfiguration
	{
		OvernightAndRTH = 0,
		AsianLondonNewYork = 1
	}

	public class StepSessionConfigurationConverter : EnumConverter
	{
		public StepSessionConfigurationConverter() : base(typeof(StepSessionConfiguration)) { }

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is StepSessionConfiguration)
				return (StepSessionConfiguration)value == StepSessionConfiguration.AsianLondonNewYork
					? "Asian / London / New York"
					: "Overnight / RTH";

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (string.Equals(text, "Asian / London / New York", StringComparison.OrdinalIgnoreCase))
				return StepSessionConfiguration.AsianLondonNewYork;
			if (string.Equals(text, "Overnight / RTH", StringComparison.OrdinalIgnoreCase))
				return StepSessionConfiguration.OvernightAndRTH;

			return base.ConvertFrom(context, culture, value);
		}
	}

	public enum StepTradeSourceMode
	{
		SecondaryTickSeries = 0,
		TickReplayLastEvents = 1
	}

	public class StepTradeSourceModeConverter : EnumConverter
	{
		public StepTradeSourceModeConverter() : base(typeof(StepTradeSourceMode)) { }

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is StepTradeSourceMode)
				return (StepTradeSourceMode)value == StepTradeSourceMode.TickReplayLastEvents
					? "Tick Replay Last Events"
					: "Secondary Tick Series";

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (string.Equals(text, "Tick Replay Last Events", StringComparison.OrdinalIgnoreCase))
				return StepTradeSourceMode.TickReplayLastEvents;
			if (string.Equals(text, "Secondary Tick Series", StringComparison.OrdinalIgnoreCase))
				return StepTradeSourceMode.SecondaryTickSeries;

			return base.ConvertFrom(context, culture, value);
		}
	}

	public enum StepVolumeAmountType
	{
		Contracts1000 = 1000,
		Contracts2000 = 2000,
		Contracts5000 = 5000,
		Contracts10000 = 10000,
		Contracts20000 = 20000,
		Contracts50000 = 50000,
		Contracts100000 = 100000,
		Contracts200000 = 200000,
		Contracts500000 = 500000
	}

	public class StepVolumeAmountConverter : EnumConverter
	{
		public StepVolumeAmountConverter() : base(typeof(StepVolumeAmountType)) { }

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			return new StandardValuesCollection(new[]
			{
				StepVolumeAmountType.Contracts1000,
				StepVolumeAmountType.Contracts2000,
				StepVolumeAmountType.Contracts5000,
				StepVolumeAmountType.Contracts10000,
				StepVolumeAmountType.Contracts20000,
				StepVolumeAmountType.Contracts50000,
				StepVolumeAmountType.Contracts100000,
				StepVolumeAmountType.Contracts200000,
				StepVolumeAmountType.Contracts500000
			});
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is StepVolumeAmountType)
				return ((int)(StepVolumeAmountType)value).ToString("N0", CultureInfo.InvariantCulture);

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			int amount;
			if (!string.IsNullOrWhiteSpace(text)
				&& int.TryParse(text.Replace(",", string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out amount)
				&& Enum.IsDefined(typeof(StepVolumeAmountType), amount))
				return (StepVolumeAmountType)amount;

			return base.ConvertFrom(context, culture, value);
		}
	}

	public enum StepVolumeResetAnchor
	{
		NewDay6PM = 0,
		RTHOpen930AM = 1
	}

	public class StepVolumeResetAnchorConverter : EnumConverter
	{
		public StepVolumeResetAnchorConverter() : base(typeof(StepVolumeResetAnchor)) { }

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is StepVolumeResetAnchor)
				return (StepVolumeResetAnchor)value == StepVolumeResetAnchor.RTHOpen930AM
					? "9:30 AM (RTH Open)"
					: "6:00 PM (New Day)";

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (string.Equals(text, "9:30 AM (RTH Open)", StringComparison.OrdinalIgnoreCase))
				return StepVolumeResetAnchor.RTHOpen930AM;
			if (string.Equals(text, "6:00 PM (New Day)", StringComparison.OrdinalIgnoreCase))
				return StepVolumeResetAnchor.NewDay6PM;

			return base.ConvertFrom(context, culture, value);
		}
	}

	public enum StepVALineStyleEnum
	{
		Solid   = 0,
		Dash    = 1,
		Dot     = 2,
		DashDot = 3
	}

	public enum StepMirrorArrangement
	{
		DeltaLeft_VolumeRight,
		VolumeLeft_DeltaRight
	}

	public enum StepActiveUnmirroredLayout
	{
		Overlay = 0,
		SideBySideBothLeft = 1
	}

	public class StepActiveUnmirroredLayoutConverter : EnumConverter
	{
		public StepActiveUnmirroredLayoutConverter() : base(typeof(StepActiveUnmirroredLayout)) { }

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is StepActiveUnmirroredLayout)
				return (StepActiveUnmirroredLayout)value == StepActiveUnmirroredLayout.SideBySideBothLeft
					? "Side by Side (Both Left)"
					: "Overlay";

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (string.Equals(text, "Side by Side (Both Left)", StringComparison.OrdinalIgnoreCase))
				return StepActiveUnmirroredLayout.SideBySideBothLeft;
			if (string.Equals(text, "Overlay", StringComparison.OrdinalIgnoreCase))
				return StepActiveUnmirroredLayout.Overlay;

			return base.ConvertFrom(context, culture, value);
		}
	}

	public class StepVisibilityHotkeyConverter : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			return new StandardValuesCollection(new[]
			{
				"Alt+S", "Alt+H", "Ctrl+Alt+S", "Ctrl+Alt+H",
				"Ctrl+Shift+S", "Ctrl+Shift+H", "Alt+Shift+S", "Alt+Shift+H"
			});
		}
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaStepProfile : Indicator
	{
		#region Inner Types
		private class StepBlock
		{
			public object   SyncObj = new object();
			public DateTime StartTime;
			public DateTime EndTime;
			public int      StartBarIndex;
			public int      EndBarIndex = -1;      // -1 = active/in-progress
			public double   HighPrice;
			public double   LowPrice;
			public Dictionary<double, long> VolByPrice   = new Dictionary<double, long>();
			public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();
			public bool IsVACalculated = false;
			public double POCPrice = double.NaN;
			public double VAHPrice = double.NaN;
			public double VALPrice = double.NaN;
			public long MaxVol = 0;
			public int VACalculationCompressionTicks = -1;
			public long TotalVolume;
			public long CumulativeDelta;
			public long MaxCumulativeDelta;
			public long MinCumulativeDelta;
			public DateTime VolumeSequenceAnchor = DateTime.MinValue;
		}

		private struct VisibilityHotkeyGesture
		{
			public Key Key;
			public ModifierKeys Modifiers;
			public bool IsValid;
		}
		#endregion

		#region Fields
		private List<StepBlock> stepBlocks;
		private DateTime        previousBarTime = DateTime.MinValue;

		// Bid/Ask cache for delta classification
		private double lastBid  = double.NaN;
		private double lastAsk  = double.NaN;
		private double prevLast = double.NaN;

		// SharpDX rendering resources
		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private SharpDX.Direct2D1.SolidColorBrush volBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush histVolBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush pocBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush posDeltaBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush negDeltaBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush histPosDeltaBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush histNegDeltaBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush[] activePositiveDeltaIntensityBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] activeNegativeDeltaIntensityBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] historicalPositiveDeltaIntensityBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] historicalNegativeDeltaIntensityBrushes;
		private SharpDX.Direct2D1.SolidColorBrush blockSepBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush sessionLabelBrushDx;

		// Volume gradient palette (outside VA)
		private SharpDX.Direct2D1.SolidColorBrush[] volGradientBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] histVolGradientBrushes;
		private int lastBuiltGradientSteps = -1;

		// Value Area gradient palette (inside VA)
		private SharpDX.Direct2D1.SolidColorBrush   vaVolBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush   histVaVolBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush[] vaGradientBrushes;
		private SharpDX.Direct2D1.SolidColorBrush[] histVaGradientBrushes;
		private int lastBuiltVAGradientSteps = -1;

		// VA line resources
		private SharpDX.Direct2D1.SolidColorBrush vaLineBrushDx;
		private SharpDX.Direct2D1.StrokeStyle     vaLineStrokeDx;
		private int lastBuiltDeltaIntensitySteps = -1;
		private float lastBuiltDeltaIntensityMinOpacity = -1f;
		private float lastBuiltActiveDeltaIntensityOpacity = -1f;
		private float lastBuiltHistoricalDeltaIntensityOpacity = -1f;

		// Text resources
		private SharpDX.Direct2D1.SolidColorBrush deltaTextBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush negativeDeltaTextBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush statisticsTextBrushDx;
		private SharpDX.DirectWrite.TextFormat    deltaTextFormatDx;
		private SharpDX.DirectWrite.TextFormat    statisticsTextFormatDx;
		private SharpDX.DirectWrite.TextFormat    sessionLabelTextFormatDx;
		private float lastBuiltStatisticsFontSize = -1f;
		private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();
		private int lastDynamicDeltaComp = -1;
		private DateTime lastRenderSkipUtc = DateTime.MinValue;
		private readonly string diagnosticsInstanceId = Guid.NewGuid().ToString("N");
		private bool diagnosticsRegistered;
		private ChartControl visibilityHotkeyChartControl;
		private KeyEventHandler visibilityHotkeyHandler;
		private bool visibilityHotkeyAttached;
		private volatile bool visibilityHotkeyHidden;
		private ChartControl deltaHoverChart;
		private float deltaMouseX = float.NaN, deltaMouseY = float.NaN;
		private long deltaHoverValue;
		private bool deltaHoverHit;
		private float historicalDeltaClipLeft, historicalDeltaClipRight;
		private float historicalTextBarSpacing;
		private DateTime lastDeltaHoverRefresh;
		private SharpDX.Direct2D1.SolidColorBrush deltaHoverBackgroundDx;
		private SharpDX.DirectWrite.TextFormat deltaHoverFormatDx;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name        = "OrcaStepProfile";
				Description = "Displays volume and delta profiles by time interval, traded volume, or market session. Includes active and historical profiles, point of control, value area, delta labels, and profile statistics.";
				Calculate   = Calculate.OnPriceChange;
				IsOverlay   = true;

				// Data
				StepBasis              = StepProfileBasis.Time;
				SessionConfiguration   = StepSessionConfiguration.OvernightAndRTH;
				StepInterval           = StepIntervalType.Minutes30;
				StepVolumeAmount       = StepVolumeAmountType.Contracts100000;
				VolumeResetAnchor      = StepVolumeResetAnchor.NewDay6PM;
				VolumeTickCompression  = 4;
				UseDynamicVolumeAggregation = false;
				VolumeDynamicAggregationMultiplier = 1.0;
				MaxDynamicVolumeTicks = 8;
				DeltaTickCompression   = 10;
				UseDynamicAggregation  = false;
				DynamicAggregationMultiplier = 1.0;
				DeltaDynamicRowMinPixels = 10;
				DynamicDeltaMinCompression = 1;
				DynamicDeltaMaxCompression = 100;
				TradeSourceMode         = StepTradeSourceMode.SecondaryTickSeries;
				RTHOnly                = false;
				RTHStart               = DateTime.Parse("09:30:00", System.Globalization.CultureInfo.InvariantCulture);
				RTHEnd                 = DateTime.Parse("16:00:00", System.Globalization.CultureInfo.InvariantCulture);

				// Layout
				DynamicProfileWidth      = true;
				FillSpacePercent         = 80;
				OverrideNonRthWidthOnVolumeCharts = false;
				NonRthFillSpacePercent   = 100;
				OverrideRthWidthOnTimeCharts = true;
				RthTimeFillSpacePercent = 100;
				HistoricalProfileWidthPx = 100;
				ActiveProfileWidthPx     = 150;
				DynamicHistoricalDeltaWidth = true;
				HistoricalDeltaFillPercent = 10;
				HistoricalDeltaWidthPx   = 60;
				ActiveDeltaWidthPx       = 60;
				RightOffsetPx            = 60;
				ProfileBarSpacingPx      = 0;
				MirrorProfiles           = false;
				MirrorArrangement        = StepMirrorArrangement.DeltaLeft_VolumeRight;
				ActiveMirrorArrangement  = StepMirrorArrangement.DeltaLeft_VolumeRight;
				ActiveUnmirroredLayout   = StepActiveUnmirroredLayout.Overlay;
				DrawBehindCandles        = false;

				// Visibility
				ShowActiveVolume    = true;
				ShowHistoricalVolume= true;
				ShowActiveDelta     = true;
				ShowHistoricalDelta = true;
				ShowPOC             = true;
				ShowBlockSeparators = true;
				ShowSessionLabels   = true;
				EnableVisibilityHotkey = false;
				VisibilityHotkey = "Alt+S";

				// Gradient
				UseGradient   = true;
				GradientSteps = 16;
				MinBrightness = 0.20f;

				// Value Area
				ShowValueArea    = true;
				ShowVAColor      = true;
				ShowVALines      = true;
				ValueAreaPercent = 70;
				VALineThickness  = 1.5f;
				VALineStyle      = StepVALineStyleEnum.Dash;

				// Colors — profile
				VolumeBrush             = WpfBrushes.RoyalBlue;
				ActiveVolumeOpacity     = 0.85f;
				HistoricalVolumeOpacity = 0.50f;
				POCBrush                = WpfBrushes.DodgerBlue;

				// Colors — Value Area
				VABrush     = WpfBrushes.CornflowerBlue;
				VALineBrush = WpfBrushes.White;

				// Colors — delta
				PositiveDeltaBrush     = WpfBrushes.Lime;
				NegativeDeltaBrush     = WpfBrushes.Red;
				ActiveDeltaOpacity     = 0.85f;
				HistoricalDeltaOpacity = 0.50f;
				UseDeltaIntensityColoring = true;
				DeltaIntensityMinOpacity = 0.35f;

				// Colors — separators
				BlockSeparatorBrush = WpfBrushes.DimGray;

				// Delta Text
				ShowDeltaText = true;
				DeltaTextMinThreshold = 10;
				DeltaTextBrush = WpfBrushes.LightGreen;
				NegativeDeltaTextBrush = WpfBrushes.LightCoral;
				DeltaTextFontSize = 11f;
				HistoricalDeltaTextMode = StepHistoricalDeltaTextMode.FollowGlobal;
				HistoricalDeltaTextMinValue = 500;
				HistoricalDeltaTextMinRowHeight = 6;
				HistoricalDeltaTextMinBarSpacing = 0;
				RevealHistoricalDeltaOnHover = false;

				// Profile Statistics
				ShowProfileStatistics = false;
				ShowActiveStatistics = true;
				ShowHistoricalStatistics = true;
				ShowTotalDelta = true;
				ShowFinishDelta = true;
				ShowDeltaPercent = true;
				ShowTotalVolume = true;
				StatisticsFontSize = 12;
				StatisticsTextBrush = WpfBrushes.White;
			}
			else if (State == State.Configure)
			{
				if (TradeSourceMode == StepTradeSourceMode.SecondaryTickSeries)
					AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				stepBlocks      = new List<StepBlock>(256);
				previousBarTime = DateTime.MinValue;
				textWidthCache.Clear();
				ReportDiagnosticsState();
			}
			else if (State == State.Historical)
			{
				ApplyProfileZOrder();
				QueueAttachVisibilityHotkey();
				QueueAttachDeltaHover();
				ReportDiagnosticsState();
				if (TradeSourceMode == StepTradeSourceMode.TickReplayLastEvents && !IsPrimaryTickReplayEnabled())
					Print("OrcaStepProfile: Tick Replay Last Events requires Tick Replay enabled on the chart's primary Data Series. Historical profiles will begin when Last events become available.");
			}
			else if (State == State.Realtime)
			{
				QueueAttachVisibilityHotkey();
				QueueAttachDeltaHover();
				ReportDiagnosticsState();
			}
			else if (State == State.Terminated)
			{
				DetachVisibilityHotkey();
				DetachDeltaHover();
				OrcaDiagnosticsCore.UnregisterInstance(diagnosticsInstanceId);
				DisposeDx();
			}
		}

		private void ApplyProfileZOrder()
		{
			if (ChartControl == null) return;

			int desiredZOrder = DrawBehindCandles ? -1 : 0;
			if (ZOrder == desiredZOrder) return;

			SetZOrder(desiredZOrder);
		}

		#region Dispose
		private void DisposeDx()
		{
			try
			{
				if (volBrushDx != null) volBrushDx.Dispose();
				if (histVolBrushDx != null) histVolBrushDx.Dispose();
				if (pocBrushDx != null) pocBrushDx.Dispose();
				if (posDeltaBrushDx != null) posDeltaBrushDx.Dispose();
				if (negDeltaBrushDx != null) negDeltaBrushDx.Dispose();
				if (histPosDeltaBrushDx != null) histPosDeltaBrushDx.Dispose();
				if (histNegDeltaBrushDx != null) histNegDeltaBrushDx.Dispose();
				DisposePalette(ref activePositiveDeltaIntensityBrushes);
				DisposePalette(ref activeNegativeDeltaIntensityBrushes);
				DisposePalette(ref historicalPositiveDeltaIntensityBrushes);
				DisposePalette(ref historicalNegativeDeltaIntensityBrushes);
				if (blockSepBrushDx != null) blockSepBrushDx.Dispose();
				if (sessionLabelBrushDx != null) sessionLabelBrushDx.Dispose();
				if (vaVolBrushDx != null) vaVolBrushDx.Dispose();
				if (histVaVolBrushDx != null) histVaVolBrushDx.Dispose();
				if (vaLineBrushDx != null) vaLineBrushDx.Dispose();
				if (vaLineStrokeDx != null) vaLineStrokeDx.Dispose();
				if (deltaTextBrushDx != null) deltaTextBrushDx.Dispose();
				if (negativeDeltaTextBrushDx != null) negativeDeltaTextBrushDx.Dispose();
				if (statisticsTextBrushDx != null) statisticsTextBrushDx.Dispose();
				if (deltaTextFormatDx != null) deltaTextFormatDx.Dispose();
				if (deltaHoverBackgroundDx != null) deltaHoverBackgroundDx.Dispose();
				if (deltaHoverFormatDx != null) deltaHoverFormatDx.Dispose();
				if (statisticsTextFormatDx != null) statisticsTextFormatDx.Dispose();
				if (sessionLabelTextFormatDx != null) sessionLabelTextFormatDx.Dispose();

				if (volGradientBrushes != null)
					for (int i = 0; i < volGradientBrushes.Length; i++)
						if (volGradientBrushes[i] != null) volGradientBrushes[i].Dispose();
				if (histVolGradientBrushes != null)
					for (int i = 0; i < histVolGradientBrushes.Length; i++)
						if (histVolGradientBrushes[i] != null) histVolGradientBrushes[i].Dispose();

				if (vaGradientBrushes != null)
					for (int i = 0; i < vaGradientBrushes.Length; i++)
						if (vaGradientBrushes[i] != null) vaGradientBrushes[i].Dispose();
				if (histVaGradientBrushes != null)
					for (int i = 0; i < histVaGradientBrushes.Length; i++)
						if (histVaGradientBrushes[i] != null) histVaGradientBrushes[i].Dispose();
			}
			catch { }
			finally
			{
				volBrushDx         = null;
				histVolBrushDx     = null;
				pocBrushDx         = null;
				posDeltaBrushDx    = null;
				negDeltaBrushDx    = null;
				histPosDeltaBrushDx = null;
				histNegDeltaBrushDx = null;
				activePositiveDeltaIntensityBrushes = null;
				activeNegativeDeltaIntensityBrushes = null;
				historicalPositiveDeltaIntensityBrushes = null;
				historicalNegativeDeltaIntensityBrushes = null;
				blockSepBrushDx    = null;
				sessionLabelBrushDx = null;
				vaVolBrushDx       = null;
				histVaVolBrushDx   = null;
				vaLineBrushDx      = null;
				vaLineStrokeDx     = null;
				volGradientBrushes = null;
				histVolGradientBrushes = null;
				vaGradientBrushes  = null;
				histVaGradientBrushes = null;
				deltaTextBrushDx   = null;
				negativeDeltaTextBrushDx = null;
				statisticsTextBrushDx = null;
				deltaTextFormatDx  = null;
				deltaHoverBackgroundDx = null;
				deltaHoverFormatDx = null;
				statisticsTextFormatDx = null;
				sessionLabelTextFormatDx = null;
				dxResourceRenderTarget = IntPtr.Zero;
				lastBuiltGradientSteps   = -1;
				lastBuiltVAGradientSteps = -1;
				lastBuiltDeltaIntensitySteps = -1;
				lastBuiltDeltaIntensityMinOpacity = -1f;
				lastBuiltActiveDeltaIntensityOpacity = -1f;
				lastBuiltHistoricalDeltaIntensityOpacity = -1f;
				lastBuiltStatisticsFontSize = -1f;
				textWidthCache.Clear();
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}
		#endregion

		#region Market Data / Block Boundary / Tick Processing
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e == null)
				return;
			long diagnosticsWorkStart = 0;

			if (OrcaDiagnosticsCore.IsEnabled)
			{
				EnsureDiagnosticsRegistered();
				long diagnosticsSequence = OrcaDiagnosticsCore.ReportMarketData(diagnosticsInstanceId, e.MarketDataType, e.Time == DateTime.MinValue ? GetDiagnosticsEventTime() : e.Time);
				diagnosticsWorkStart = OrcaDiagnosticsCore.BeginWorkSample(diagnosticsSequence);
			}
			try
			{
				if (e.MarketDataType == MarketDataType.Bid)
					lastBid = e.Price;
				else if (e.MarketDataType == MarketDataType.Ask)
					lastAsk = e.Price;
				else if (e.MarketDataType == MarketDataType.Last)
				{
					if (e.Bid > 0 && !double.IsNaN(e.Bid))
						lastBid = e.Bid;
					if (e.Ask > 0 && !double.IsNaN(e.Ask))
						lastAsk = e.Ask;

					if (TradeSourceMode == StepTradeSourceMode.TickReplayLastEvents)
					{
						DateTime tradeTime = e.Time == DateTime.MinValue ? GetCurrentPrimaryTime() : e.Time;
						ProcessTradeTick(tradeTime, e.Price, (long)e.Volume, e.Bid, e.Ask);
					}
				}
			}
			finally
			{
				if (diagnosticsWorkStart > 0)
					OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.MarketData, -1, diagnosticsWorkStart);
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
				if (TradeSourceMode == StepTradeSourceMode.SecondaryTickSeries && BarsInProgress == 1)
					ProcessTradeTick(Time[0], Close[0], (long)Volume[0], double.NaN, double.NaN);
			}
			finally
			{
				if (diagnosticsWorkStart > 0)
					OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.BarUpdate, diagnosticsBarsInProgress, diagnosticsWorkStart);
			}
		}

		private bool HistoricalDeltaHoverEnabled
		{
			get { return HistoricalDeltaTextMode != StepHistoricalDeltaTextMode.Hidden
				&& (RevealHistoricalDeltaOnHover || HistoricalDeltaTextMode == StepHistoricalDeltaTextMode.HoverOnly); }
		}

		private void QueueAttachDeltaHover()
		{
			ChartControl chart = ChartControl;
			if (chart == null) return;
			chart.Dispatcher.InvokeAsync(() =>
			{
				if (State == State.Terminated || deltaHoverChart != null || !ReferenceEquals(chart, ChartControl)) return;
				deltaHoverChart = chart;
				chart.MouseMove += OnDeltaHoverMove;
				chart.MouseLeave += OnDeltaHoverLeave;
			});
		}

		private void DetachDeltaHover()
		{
			ChartControl chart = deltaHoverChart;
			deltaHoverChart = null;
			deltaMouseX = deltaMouseY = float.NaN;
			if (chart == null) return;
			chart.Dispatcher.InvokeAsync(() =>
			{
				chart.MouseMove -= OnDeltaHoverMove;
				chart.MouseLeave -= OnDeltaHoverLeave;
			});
		}

		private void OnDeltaHoverMove(object sender, MouseEventArgs e)
		{
			if (!HistoricalDeltaHoverEnabled || !IsVisible || visibilityHotkeyHidden || !ShowHistoricalDelta || deltaHoverChart == null) return;
			var point = e.GetPosition(deltaHoverChart);
			var source = System.Windows.PresentationSource.FromVisual(deltaHoverChart);
			var scale = source == null || source.CompositionTarget == null ? System.Windows.Media.Matrix.Identity : source.CompositionTarget.TransformToDevice;
			deltaMouseX = (float)(point.X * scale.M11);
			deltaMouseY = (float)(point.Y * scale.M22);
			if ((DateTime.UtcNow - lastDeltaHoverRefresh).TotalMilliseconds < 33) return;
			lastDeltaHoverRefresh = DateTime.UtcNow;
			deltaHoverChart.InvalidateVisual();
		}

		private void OnDeltaHoverLeave(object sender, MouseEventArgs e)
		{
			deltaMouseX = deltaMouseY = float.NaN;
			if (deltaHoverChart != null && HistoricalDeltaHoverEnabled) deltaHoverChart.InvalidateVisual();
		}

		private void QueueAttachVisibilityHotkey()
		{
			if (!EnableVisibilityHotkey || visibilityHotkeyAttached || ChartControl == null)
				return;

			VisibilityHotkeyGesture gesture;
			if (!TryParseVisibilityHotkey(VisibilityHotkey, out gesture))
				return;

			ChartControl chart = ChartControl;
			try
			{
				chart.Dispatcher.InvokeAsync(() =>
				{
					if (State == State.Terminated || visibilityHotkeyAttached || ChartControl == null || !ReferenceEquals(chart, ChartControl))
						return;

					visibilityHotkeyHandler = OnVisibilityHotkeyPreviewKeyDown;
					visibilityHotkeyChartControl = chart;
					chart.AddHandler(Keyboard.PreviewKeyDownEvent, visibilityHotkeyHandler, true);
					visibilityHotkeyAttached = true;
				});
			}
			catch { }
		}

		private void DetachVisibilityHotkey()
		{
			ChartControl chart = visibilityHotkeyChartControl;
			KeyEventHandler handler = visibilityHotkeyHandler;
			visibilityHotkeyAttached = false;
			visibilityHotkeyChartControl = null;
			visibilityHotkeyHandler = null;
			if (chart == null || handler == null) return;

			Action detach = () =>
			{
				try { chart.RemoveHandler(Keyboard.PreviewKeyDownEvent, handler); }
				catch { }
			};

			try
			{
				if (chart.Dispatcher.CheckAccess()) detach();
				else chart.Dispatcher.InvokeAsync(detach);
			}
			catch { }
		}

		private void OnVisibilityHotkeyPreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e == null || e.IsRepeat || !EnableVisibilityHotkey || !visibilityHotkeyAttached || IsTextInputFocused())
				return;

			VisibilityHotkeyGesture gesture;
			if (!TryParseVisibilityHotkey(VisibilityHotkey, out gesture) || !VisibilityHotkeyMatches(e, gesture))
				return;

			e.Handled = true;
			visibilityHotkeyHidden = !visibilityHotkeyHidden;
			ChartControl chart = visibilityHotkeyChartControl;
			if (chart != null)
			{
				try { chart.InvalidateVisual(); }
				catch { }
			}
		}

		private static bool IsTextInputFocused()
		{
			object focused = Keyboard.FocusedElement;
			return focused is System.Windows.Controls.Primitives.TextBoxBase
				|| focused is System.Windows.Controls.PasswordBox
				|| focused is System.Windows.Controls.ComboBox;
		}

		private static bool TryParseVisibilityHotkey(string configured, out VisibilityHotkeyGesture gesture)
		{
			gesture = new VisibilityHotkeyGesture();
			if (string.IsNullOrWhiteSpace(configured)) return false;

			ModifierKeys modifiers = ModifierKeys.None;
			Key key = Key.None;
			foreach (string rawPart in configured.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string part = rawPart.Trim();
				if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase)) modifiers |= ModifierKeys.Control;
				else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= ModifierKeys.Alt;
				else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= ModifierKeys.Shift;
				else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) || part.Equals("Windows", StringComparison.OrdinalIgnoreCase)) modifiers |= ModifierKeys.Windows;
				else
				{
					Key parsed;
					if (key != Key.None || !Enum.TryParse(part, true, out parsed) || parsed == Key.None) return false;
					key = parsed;
				}
			}

			if (key == Key.None) return false;
			gesture = new VisibilityHotkeyGesture { Key = key, Modifiers = modifiers, IsValid = true };
			return true;
		}

		private static bool VisibilityHotkeyMatches(KeyEventArgs e, VisibilityHotkeyGesture gesture)
		{
			if (e == null || !gesture.IsValid) return false;
			Key eventKey = e.Key == Key.System ? e.SystemKey : e.Key;
			return eventKey == gesture.Key && Keyboard.Modifiers == gesture.Modifiers;
		}

		private void ProcessTradeTick(DateTime tickTime, double last, long volume, double bidAtTrade, double askAtTrade)
		{
			if (stepBlocks == null || tickTime == DateTime.MinValue || volume <= 0 || double.IsNaN(last) || last <= 0)
				return;

			if (RTHOnly && StepBasis != StepProfileBasis.Session && !IsWithinConfiguredRTH(tickTime))
				return;

			previousBarTime = tickTime;
			int deltaDirection = ClassifyTickDirection(last, bidAtTrade, askAtTrade);
			switch (StepBasis)
			{
				case StepProfileBasis.Volume:
					ProcessVolumeStepTick(tickTime, last, volume, deltaDirection);
					break;
				case StepProfileBasis.Session:
					ProcessSessionStepTick(tickTime, last, volume, deltaDirection);
					break;
				default:
					ProcessTimeStepTick(tickTime, last, volume, deltaDirection);
					break;
			}
		}

		private bool IsWithinConfiguredRTH(DateTime time)
		{
			TimeSpan tod = time.TimeOfDay;
			TimeSpan startTod = RTHStart.TimeOfDay;
			TimeSpan endTod = RTHEnd.TimeOfDay;

			return startTod < endTod
				? tod >= startTod && tod < endTod
				: tod >= startTod || tod < endTod;
		}

		private DateTime GetCurrentPrimaryTime()
		{
			try
			{
				if (BarsArray != null && BarsArray.Length > 0 && BarsArray[0] != null && BarsArray[0].CurrentBar >= 0)
					return BarsArray[0].GetTime(BarsArray[0].CurrentBar);
			}
			catch { }

			return DateTime.MinValue;
		}

		private bool IsPrimaryTickReplayEnabled()
		{
			try
			{
				return IsTickReplays != null && IsTickReplays.Length > 0 && IsTickReplays[0] == true;
			}
			catch { return false; }
		}

		private DateTime GetAlignedBlockStart(DateTime time, int intervalMinutes)
		{
			if (intervalMinutes >= 1440)
			{
				if (time.Hour < 18)
					return time.Date.AddDays(-1).AddHours(18);
				else
					return time.Date.AddHours(18);
			}
			if (intervalMinutes == 240)
			{
				DateTime sessionStart = time.Date.AddHours(18);
				if (time < sessionStart)
					sessionStart = sessionStart.AddDays(-1);

				int elapsedMinutes = (int)(time - sessionStart).TotalMinutes;
				return sessionStart.AddMinutes((elapsedMinutes / intervalMinutes) * intervalMinutes);
			}
			int totalMins = time.Hour * 60 + time.Minute;
			int blockStart = (totalMins / intervalMinutes) * intervalMinutes;
			return time.Date.AddMinutes(blockStart);
		}

		private DateTime GetAlignedBlockEnd(DateTime blockStart, int intervalMinutes)
		{
			if (intervalMinutes >= 1440)
				return blockStart.AddDays(1);
			return blockStart.AddMinutes(intervalMinutes);
		}

		private void ProcessTimeStepTick(DateTime tickTime, double last, long volume, int deltaDirection)
		{
			if (stepBlocks.Count == 0)
			{
				DateTime alignedStart = GetAlignedBlockStart(tickTime, (int)StepInterval);
				StartNewTimeBlock(alignedStart, ResolveBoundaryStartBarIndex(alignedStart));
			}
			else
			{
				StepBlock active = stepBlocks[stepBlocks.Count - 1];
				if (tickTime >= active.EndTime)
				{
					int newBlockStartIdx = ResolveBoundaryStartBarIndex(active.EndTime);
					FinalizeBlockBeforeIndex(active, newBlockStartIdx);

					DateTime newStart = GetAlignedBlockStart(tickTime, (int)StepInterval);
					StartNewTimeBlock(newStart, newBlockStartIdx);
				}
			}

			UpdateActiveEndBarIndex();
			ProcessTickIntoActiveBlock(last, volume, deltaDirection);
		}

		private void StartNewTimeBlock(DateTime startTime, int startBarIndex)
		{
			int intervalMins = (int)StepInterval;
			DateTime alignedStart = GetAlignedBlockStart(startTime, intervalMins);

			stepBlocks.Add(new StepBlock
			{
				StartTime     = alignedStart,
				EndTime       = GetAlignedBlockEnd(alignedStart, intervalMins),
				StartBarIndex = startBarIndex,
				EndBarIndex   = -1,
				HighPrice     = double.MinValue,
				LowPrice      = double.MaxValue
			});
		}

		private void ProcessSessionStepTick(DateTime tickTime, double last, long volume, int deltaDirection)
		{
			DateTime sessionStart;
			DateTime sessionEnd;
			if (!TryGetSessionWindow(tickTime, out sessionStart, out sessionEnd))
			{
				FinalizeSessionBlockOutsideWindow(tickTime);
				return;
			}

			if (stepBlocks.Count == 0)
			{
				StartNewSessionBlock(sessionStart, sessionEnd, ResolveBoundaryStartBarIndex(sessionStart));
			}
			else
			{
				StepBlock active = stepBlocks[stepBlocks.Count - 1];
				bool activeIsEmpty = IsBlockEmpty(active);
				if (active.StartTime != sessionStart || active.EndTime != sessionEnd)
				{
					if (activeIsEmpty)
						stepBlocks.RemoveAt(stepBlocks.Count - 1);
					else
						FinalizeBlockBeforeIndex(active, ResolveBoundaryStartBarIndex(active.EndTime));

					StartNewSessionBlock(sessionStart, sessionEnd, ResolveBoundaryStartBarIndex(sessionStart));
				}
				else if (activeIsEmpty)
				{
					// A post-close placeholder is re-anchored when the first actual session tick arrives.
					active.StartBarIndex = ResolveBoundaryStartBarIndex(sessionStart);
				}
			}

			UpdateActiveEndBarIndex();
			ProcessTickIntoActiveBlock(last, volume, deltaDirection);
		}

		private bool TryGetSessionWindow(DateTime time, out DateTime sessionStart, out DateTime sessionEnd)
		{
			DateTime tradingDayStart = GetSessionTradingDayStart(time);
			DateTime threeAm = tradingDayStart.AddHours(9);
			DateTime rthStart = tradingDayStart.AddHours(15.5);
			DateTime rthEnd = tradingDayStart.Date.AddDays(1).Add(RTHEnd.TimeOfDay);

			if (SessionConfiguration == StepSessionConfiguration.AsianLondonNewYork)
			{
				if (time >= tradingDayStart && time < threeAm)
				{
					sessionStart = tradingDayStart;
					sessionEnd = threeAm;
					return true;
				}
				if (time >= threeAm && time < rthStart)
				{
					sessionStart = threeAm;
					sessionEnd = rthStart;
					return true;
				}
			}
			else if (time >= tradingDayStart && time < rthStart)
			{
				sessionStart = tradingDayStart;
				sessionEnd = rthStart;
				return true;
			}

			if (time >= rthStart && time < rthEnd)
			{
				sessionStart = rthStart;
				sessionEnd = rthEnd;
				return true;
			}

			sessionStart = DateTime.MinValue;
			sessionEnd = DateTime.MinValue;
			return false;
		}

		private DateTime GetSessionTradingDayStart(DateTime time)
		{
			DateTime tradingDayStart = time.Date.AddHours(18);
			return time < tradingDayStart ? tradingDayStart.AddDays(-1) : tradingDayStart;
		}

		private void StartNewSessionBlock(DateTime startTime, DateTime endTime, int startBarIndex)
		{
			stepBlocks.Add(new StepBlock
			{
				StartTime = startTime,
				EndTime = endTime,
				StartBarIndex = startBarIndex,
				EndBarIndex = -1,
				HighPrice = double.MinValue,
				LowPrice = double.MaxValue
			});
		}

		private void FinalizeSessionBlockOutsideWindow(DateTime tickTime)
		{
			if (stepBlocks.Count == 0)
				return;

			StepBlock active = stepBlocks[stepBlocks.Count - 1];
			if (tickTime < active.EndTime || IsBlockEmpty(active))
				return;

			FinalizeBlockBeforeIndex(active, ResolveBoundaryStartBarIndex(active.EndTime));

			DateTime nextSessionStart = GetSessionTradingDayStart(active.StartTime).AddDays(1);
			DateTime nextSessionEnd = SessionConfiguration == StepSessionConfiguration.AsianLondonNewYork
				? nextSessionStart.AddHours(9)
				: nextSessionStart.AddHours(15.5);
			StartNewSessionBlock(nextSessionStart, nextSessionEnd, ResolveBoundaryStartBarIndex(nextSessionStart));
		}

		private bool IsBlockEmpty(StepBlock block)
		{
			if (block == null)
				return true;

			lock (block.SyncObj)
				return block.TotalVolume <= 0 && block.VolByPrice.Count == 0 && block.DeltaByPrice.Count == 0;
		}

		private void ProcessVolumeStepTick(DateTime tickTime, double last, long volume, int deltaDirection)
		{
			long remainingVolume = volume;
			if (remainingVolume <= 0)
				return;

			int currentPrimaryBar = BarsArray[0].CurrentBar;
			DateTime sequenceAnchor = GetVolumeSequenceAnchor(tickTime);
			EnsureVolumeSequence(sequenceAnchor, currentPrimaryBar);

			long targetVolume = Math.Max(1L, (long)StepVolumeAmount);

			while (remainingVolume > 0)
			{
				StepBlock active = stepBlocks[stepBlocks.Count - 1];
				long capacity = Math.Max(0L, targetVolume - active.TotalVolume);
				if (capacity == 0)
				{
					CompleteVolumeBlock(active, tickTime, currentPrimaryBar);
					StartNewVolumeBlock(tickTime, currentPrimaryBar, sequenceAnchor);
					continue;
				}

				long allocatedVolume = Math.Min(remainingVolume, capacity);
				AddTickToBlock(active, last, allocatedVolume, deltaDirection);
				remainingVolume -= allocatedVolume;

				if (active.EndBarIndex < 0 || active.EndBarIndex < currentPrimaryBar)
					active.EndBarIndex = currentPrimaryBar;

				if (active.TotalVolume >= targetVolume)
				{
					CompleteVolumeBlock(active, tickTime, currentPrimaryBar);
					StartNewVolumeBlock(tickTime, currentPrimaryBar, sequenceAnchor);
				}
			}

			if (OrcaDiagnosticsCore.IsEnabled)
				OrcaDiagnosticsCore.ReportModelUpdate(diagnosticsInstanceId, previousBarTime, null);
		}

		private void EnsureVolumeSequence(DateTime sequenceAnchor, int currentPrimaryBar)
		{
			if (stepBlocks.Count == 0)
			{
				StartNewVolumeBlock(sequenceAnchor, currentPrimaryBar, sequenceAnchor);
				return;
			}

			StepBlock active = stepBlocks[stepBlocks.Count - 1];
			if (active.VolumeSequenceAnchor == sequenceAnchor)
				return;

			bool activeIsEmpty;
			lock (active.SyncObj)
				activeIsEmpty = active.TotalVolume <= 0 && active.VolByPrice.Count == 0 && active.DeltaByPrice.Count == 0;

			int newBlockStartIdx = ResolveBoundaryStartBarIndex(sequenceAnchor);
			if (activeIsEmpty)
				stepBlocks.RemoveAt(stepBlocks.Count - 1);
			else
			{
				active.EndTime = sequenceAnchor;
				FinalizeBlockBeforeIndex(active, newBlockStartIdx);
			}

			StartNewVolumeBlock(sequenceAnchor, newBlockStartIdx, sequenceAnchor);
		}

		private void StartNewVolumeBlock(DateTime startTime, int startBarIndex, DateTime sequenceAnchor)
		{
			stepBlocks.Add(new StepBlock
			{
				StartTime           = startTime,
				EndTime             = sequenceAnchor.AddDays(1),
				StartBarIndex       = startBarIndex,
				EndBarIndex         = -1,
				HighPrice           = double.MinValue,
				LowPrice            = double.MaxValue,
				VolumeSequenceAnchor = sequenceAnchor
			});
		}

		private void CompleteVolumeBlock(StepBlock block, DateTime endTime, int endBarIndex)
		{
			block.EndTime = endTime;
			block.EndBarIndex = Math.Max(block.StartBarIndex, endBarIndex);
		}

		private DateTime GetVolumeSequenceAnchor(DateTime time)
		{
			TimeSpan anchorTime = VolumeResetAnchor == StepVolumeResetAnchor.RTHOpen930AM
				? new TimeSpan(9, 30, 0)
				: new TimeSpan(18, 0, 0);
			DateTime anchor = time.Date.Add(anchorTime);
			return time < anchor ? anchor.AddDays(-1) : anchor;
		}

		private int ResolveBoundaryStartBarIndex(DateTime boundaryTime)
		{
			if (BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null || BarsArray[0].Count <= 0)
				return 0;

			// Time-based primary bars are stamped at their close. The block beginning at
			// 6:00 PM therefore belongs on the 7:00 PM hourly bar, not the 5:00 PM bar.
			int low = 0;
			int high = BarsArray[0].Count;
			while (low < high)
			{
				int middle = low + ((high - low) / 2);
				if (BarsArray[0].GetTime(middle) <= boundaryTime)
					low = middle + 1;
				else
					high = middle;
			}

			return low;
		}

		private void FinalizeBlockBeforeIndex(StepBlock block, int newBlockStartIdx)
		{
			block.EndBarIndex = newBlockStartIdx - 1;
			if (block.EndBarIndex < block.StartBarIndex)
				block.EndBarIndex = block.StartBarIndex;
		}

		private void UpdateActiveEndBarIndex()
		{
			if (stepBlocks.Count == 0)
				return;

			StepBlock active = stepBlocks[stepBlocks.Count - 1];
			if (active.EndBarIndex < 0 || active.EndBarIndex < BarsArray[0].CurrentBar)
				active.EndBarIndex = BarsArray[0].CurrentBar;
		}

		private void ProcessTickIntoActiveBlock(double last, long volume, int deltaDirection)
		{
			if (stepBlocks == null || stepBlocks.Count == 0)
				return;

			if (volume <= 0)
				return;

			AddTickToBlock(stepBlocks[stepBlocks.Count - 1], last, volume, deltaDirection);

			if (OrcaDiagnosticsCore.IsEnabled)
				OrcaDiagnosticsCore.ReportModelUpdate(diagnosticsInstanceId, previousBarTime, null);
		}

		private int ClassifyTickDirection(double last, double bidAtTrade, double askAtTrade)
		{
			int direction = 0;
			double effectiveBid = bidAtTrade > 0 && !double.IsNaN(bidAtTrade) ? bidAtTrade : lastBid;
			double effectiveAsk = askAtTrade > 0 && !double.IsNaN(askAtTrade) ? askAtTrade : lastAsk;
			if (!double.IsNaN(effectiveAsk) && !double.IsNaN(effectiveBid) && effectiveAsk > 0 && effectiveBid > 0 && effectiveAsk >= effectiveBid)
			{
				if (last >= effectiveAsk)
					direction = 1;
				else if (last <= effectiveBid)
					direction = -1;
				else if (!double.IsNaN(prevLast))
					direction = last > prevLast ? 1 : (last < prevLast ? -1 : 0);
			}
			else if (!double.IsNaN(prevLast))
			{
				direction = last > prevLast ? 1 : (last < prevLast ? -1 : 0);
			}

			prevLast = last;
			return direction;
		}

		private void AddTickToBlock(StepBlock block, double last, long volume, int deltaDirection)
		{
			if (block == null || volume <= 0)
				return;

			if (last > block.HighPrice) block.HighPrice = last;
			if (last < block.LowPrice) block.LowPrice = last;

			double volumeCompression = VolumeTickCompression * TickSize;
			double volumeBucketPrice = Math.Floor(last / volumeCompression + 0.000001) * volumeCompression;
			double deltaBucketPrice = volumeBucketPrice;
			long signedVolume = deltaDirection * volume;

			lock (block.SyncObj)
			{
				long existingVolume;
				if (block.VolByPrice.TryGetValue(volumeBucketPrice, out existingVolume))
					block.VolByPrice[volumeBucketPrice] = existingVolume + volume;
				else
					block.VolByPrice[volumeBucketPrice] = volume;

				if (signedVolume != 0)
				{
					long existingDelta;
					if (block.DeltaByPrice.TryGetValue(deltaBucketPrice, out existingDelta))
						block.DeltaByPrice[deltaBucketPrice] = existingDelta + signedVolume;
					else
						block.DeltaByPrice[deltaBucketPrice] = signedVolume;
				}

				block.CumulativeDelta += signedVolume;
				block.MaxCumulativeDelta = Math.Max(block.MaxCumulativeDelta, block.CumulativeDelta);
				block.MinCumulativeDelta = Math.Min(block.MinCumulativeDelta, block.CumulativeDelta);
				block.TotalVolume += volume;
			}
		}
		#endregion

		#region Value Area Calculation
		private bool CalcValueArea(Dictionary<double, long> volMap, double pocPrice, out double vahPrice, out double valPrice)
		{
			vahPrice = pocPrice;
			valPrice = pocPrice;
			if (volMap.Count <= 1) return false;

			var sortedPrices = new List<double>(volMap.Keys);
			sortedPrices.Sort();
			long totalVol = 0;
			foreach (var kv in volMap) totalVol += kv.Value;
			if (totalVol <= 0) return false;

			double targetVol = totalVol * (ValueAreaPercent / 100.0);
			int pocIdx = sortedPrices.IndexOf(pocPrice);
			if (pocIdx < 0) return false;

			long accumulatedVol = volMap[pocPrice];
			int lo = pocIdx;
			int hi = pocIdx;

			while (accumulatedVol < targetVol && (lo > 0 || hi < sortedPrices.Count - 1))
			{
				long volBelow = (lo > 0) ? volMap[sortedPrices[lo - 1]] : 0;
				long volAbove = (hi < sortedPrices.Count - 1) ? volMap[sortedPrices[hi + 1]] : 0;

				if (lo <= 0) { hi++; accumulatedVol += volAbove; }
				else if (hi >= sortedPrices.Count - 1) { lo--; accumulatedVol += volBelow; }
				else if (volAbove >= volBelow) { hi++; accumulatedVol += volAbove; }
				else { lo--; accumulatedVol += volBelow; }
			}

			valPrice = sortedPrices[lo];
			vahPrice = sortedPrices[hi];
			return true;
		}
		private void EnsureDiagnosticsRegistered()
		{
			if (diagnosticsRegistered)
				return;

			OrcaDiagnosticsCore.RegisterInstance(diagnosticsInstanceId, "OrcaStepProfile", this);
			OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 0, "PrimaryChartSeries", "Chart", "Primary bars");
			if (TradeSourceMode == StepTradeSourceMode.SecondaryTickSeries)
			{
				OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsInstanceId, "HiddenSecondaryTickSeries", "Unknown", "LocalBarSeries");
				OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 1, "Tick 1 Last", "Component", "Step profile tick aggregation");
			}
			else
			{
				OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsInstanceId, "TickReplayLastEvents", "Unknown", "MarketDataEvents");
			}
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

		#endregion

		#region Rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			long diagnosticsRenderStart = 0;
			if (OrcaDiagnosticsCore.IsEnabled)
			{
				EnsureDiagnosticsRegistered();
				diagnosticsRenderStart = System.Diagnostics.Stopwatch.GetTimestamp();
			}

			try
			{
				RenderStepProfiles(chartControl, chartScale);
				DrawHistoricalDeltaHover();
			}
			catch (Exception ex)
			{
				PrintRenderSkip(ex);
			}
			finally
			{
				if (diagnosticsRenderStart != 0)
					OrcaDiagnosticsCore.ReportRenderSample(diagnosticsInstanceId, diagnosticsRenderStart);
			}
		}

		private void RenderStepProfiles(ChartControl chartControl, ChartScale chartScale)
		{
			deltaHoverHit = false;
			ApplyProfileZOrder();
			base.OnRender(chartControl, chartScale);
			if (visibilityHotkeyHidden) return;
			if (stepBlocks == null || stepBlocks.Count == 0 || ChartBars == null || BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null) return;

			EnsureDxResources();

			int volumeCompressionTicks = ResolveVolumeCompressionTicks(chartScale);
			int dynamicDeltaComp = DeltaTickCompression;
			if (UseDynamicAggregation)
			{
				var panel = chartControl.ChartPanels[chartScale.PanelIndex];
				double visibleTicks = (chartScale.MaxValue - chartScale.MinValue) / TickSize;
				double ticksPerPixel = visibleTicks / Math.Max(1, panel.H);
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

				if (lastDynamicDeltaComp > 0 && Math.Abs(dynamicDeltaComp - lastDynamicDeltaComp) < Math.Max(2, dynamicDeltaComp * 0.15))
					dynamicDeltaComp = lastDynamicDeltaComp;
				else
					lastDynamicDeltaComp = dynamicDeltaComp;
			}

			int maxBarIdx = Math.Min(BarsArray[0].Count - 1, ChartBars.Count - 1);
			if (maxBarIdx < 0) return;
			int fromIdx = Math.Max(0, ChartBars.FromIndex);
			int toIdx   = Math.Min(ChartBars.ToIndex, maxBarIdx);
			if (fromIdx > toIdx) return;
			float panelTop    = ChartPanel.Y;
			float panelBottom = ChartPanel.Y + ChartPanel.H;
			historicalTextBarSpacing = HistoricalDeltaTextMode == StepHistoricalDeltaTextMode.Filtered && HistoricalDeltaTextMinBarSpacing > 0 && toIdx > fromIdx
				? Math.Abs(chartControl.GetXByBarIndex(ChartBars, toIdx) - chartControl.GetXByBarIndex(ChartBars, fromIdx)) / (toIdx - fromIdx) : 0f;

			for (int i = 0; i < stepBlocks.Count - 1; i++)
			{
				StepBlock block = stepBlocks[i];
				int startIndex;
				int endIndex;
				float startX;
				float endX;
				if (!TryGetBlockRenderBounds(chartControl, block, fromIdx, toIdx, out startIndex, out endIndex, out startX, out endX))
					continue;

				float spineX = startX;
				int drawWidth = GetHistoricalProfileDrawWidth(block, startX, endX);
				int deltaWidth = GetHistoricalDeltaDrawWidth(startX, endX);
				float clipStartX = startX;
				if (MirrorProfiles)
					clipStartX -= MirrorArrangement == StepMirrorArrangement.VolumeLeft_DeltaRight
						? drawWidth
						: deltaWidth;

				if (PushPeriodClip(clipStartX, endX, panelTop, panelBottom))
				{
					historicalDeltaClipLeft = Math.Max(ChartPanel.X, clipStartX);
					historicalDeltaClipRight = Math.Min(ChartPanel.X + ChartPanel.W, endX);
					try
					{
						if (ShowHistoricalVolume && block.VolByPrice.Count > 0)
							DrawBlockProfile(chartControl, chartScale, block, spineX, drawWidth, deltaWidth, panelTop, panelBottom, true, false, MirrorArrangement, volumeCompressionTicks);

						if (ShowHistoricalDelta && block.DeltaByPrice.Count > 0)
							DrawBlockDelta(chartControl, chartScale, block, spineX, drawWidth, deltaWidth, panelTop, panelBottom, true, false, MirrorArrangement, dynamicDeltaComp);

					}
					finally
					{
						RenderTarget.PopAxisAlignedClip();
					}
				}

				if (ShowBlockSeparators && blockSepBrushDx != null && startX >= ChartPanel.X && startX <= ChartPanel.X + ChartPanel.W)
					RenderTarget.DrawLine(new Vector2(startX, panelTop), new Vector2(startX, panelBottom), blockSepBrushDx, 1f);

				// Start statistics on this block's side of the separator.
				if (ShowProfileStatistics && ShowHistoricalStatistics && spineX > ChartPanel.X && spineX <= ChartPanel.X + ChartPanel.W)
				{
					DrawProfileStatistics(block, spineX + 4f, endX, panelTop + 4f, true);
				}
				DrawSessionLabel(block, startX, endX, panelBottom);
			}

			if (stepBlocks.Count > 0)
			{
				StepBlock active = stepBlocks[stepBlocks.Count - 1];
				bool hasActiveVolume;
				bool hasActiveDelta;
				lock (active.SyncObj)
				{
					hasActiveVolume = ShowActiveVolume && active.VolByPrice.Count > 0;
					hasActiveDelta = ShowActiveDelta && active.DeltaByPrice.Count > 0;
				}
				float activeSpineX = chartControl.CanvasRight - RightOffsetPx;
				if (hasActiveVolume)
				{
					DrawBlockProfile(chartControl, chartScale, active, activeSpineX, ActiveProfileWidthPx, ActiveDeltaWidthPx, panelTop, panelBottom, false, true, ActiveMirrorArrangement, volumeCompressionTicks);
				}
				if (hasActiveDelta)
				{
					DrawBlockDelta(chartControl, chartScale, active, activeSpineX, ActiveProfileWidthPx, ActiveDeltaWidthPx, panelTop, panelBottom, false, true, ActiveMirrorArrangement, dynamicDeltaComp);
				}
				if (ShowProfileStatistics && ShowActiveStatistics)
				{
					float statisticsLeft;
					float statisticsRight;
					if (TryGetActiveStatisticsBounds(activeSpineX, hasActiveVolume, hasActiveDelta, out statisticsLeft, out statisticsRight))
						DrawProfileStatistics(active, statisticsLeft, statisticsRight,
							panelTop + 4f + (ShowHistoricalStatistics ? Math.Max(16f, StatisticsFontSize + 6f) + 2f : 0f), false);
				}
				if (ShowBlockSeparators && blockSepBrushDx != null && active.StartBarIndex >= fromIdx && active.StartBarIndex <= toIdx)
				{
					float sepX;
					if (TryGetXByBarIndex(chartControl, active.StartBarIndex, out sepX))
						RenderTarget.DrawLine(new Vector2(sepX, panelTop), new Vector2(sepX, panelBottom), blockSepBrushDx, 1f);
				}

				if (StepBasis == StepProfileBasis.Session && !IsBlockEmpty(active))
				{
					int activeStartIndex;
					int activeEndIndex;
					float activeStartX;
					float activeEndX;
					if (TryGetBlockRenderBounds(chartControl, active, fromIdx, toIdx, out activeStartIndex, out activeEndIndex, out activeStartX, out activeEndX))
						DrawSessionLabel(active, activeStartX, activeEndX, panelBottom);
				}
			}
		}

		private void DrawHistoricalDeltaHover()
		{
			if (!deltaHoverHit || !HistoricalDeltaHoverEnabled || deltaHoverFormatDx == null || deltaHoverBackgroundDx == null) return;
			float width = Math.Min(220f, ChartPanel.W - 8f);
			float height = 24f;
			if (width <= 0 || ChartPanel.H < height + 8f) return;
			float x = Math.Max(ChartPanel.X + 4f, Math.Min(deltaMouseX + 12f, ChartPanel.X + ChartPanel.W - width - 4f));
			float y = Math.Max(ChartPanel.Y + 4f, Math.Min(deltaMouseY + 16f, ChartPanel.Y + ChartPanel.H - height - 4f));
			RenderTarget.FillRectangle(new RectangleF(x, y, width, height), deltaHoverBackgroundDx);
			RenderTarget.DrawText("Delta " + FormatSignedValue(deltaHoverValue), deltaHoverFormatDx,
				new RectangleF(x + 5f, y, width - 10f, height), deltaHoverValue >= 0 ? deltaTextBrushDx : negativeDeltaTextBrushDx, DrawTextOptions.Clip);
		}

		private void DrawSessionLabel(StepBlock block, float startX, float endX, float panelBottom)
		{
			if (!ShowSessionLabels || StepBasis != StepProfileBasis.Session || block == null || sessionLabelBrushDx == null || sessionLabelTextFormatDx == null)
				return;

			string label = GetSessionLabel(block.StartTime);
			if (string.IsNullOrEmpty(label))
				return;

			float centerX = (startX + endX) * 0.5f;
			float panelLeft = ChartPanel.X;
			float panelRight = ChartPanel.X + ChartPanel.W;
			if (centerX < panelLeft || centerX > panelRight)
				return;

			float labelWidth = Math.Max(24f, Math.Min(120f, endX - startX));
			RectangleF labelRect = new RectangleF(centerX - labelWidth * 0.5f, panelBottom - 24f, labelWidth, 20f);
			RenderTarget.DrawText(label, sessionLabelTextFormatDx, labelRect, sessionLabelBrushDx);
		}

		private bool TryGetActiveStatisticsBounds(float baseSpineX, bool hasVolume, bool hasDelta, out float left, out float right)
		{
			left = float.MaxValue;
			right = float.MinValue;
			if (!hasVolume && !hasDelta)
				return false;

			float volumeWidth = Math.Max(1f, ActiveProfileWidthPx);
			float deltaWidth = Math.Max(1f, ActiveDeltaWidthPx);
			if (MirrorProfiles)
			{
				if (ActiveMirrorArrangement == StepMirrorArrangement.VolumeLeft_DeltaRight)
				{
					if (hasVolume)
					{
						left = Math.Min(left, baseSpineX - deltaWidth - volumeWidth);
						right = Math.Max(right, baseSpineX - deltaWidth);
					}
					if (hasDelta)
					{
						left = Math.Min(left, baseSpineX - deltaWidth);
						right = Math.Max(right, baseSpineX);
					}
				}
				else
				{
					if (hasVolume)
					{
						left = Math.Min(left, baseSpineX - volumeWidth);
						right = Math.Max(right, baseSpineX);
					}
					if (hasDelta)
					{
						left = Math.Min(left, baseSpineX - volumeWidth - deltaWidth);
						right = Math.Max(right, baseSpineX - volumeWidth);
					}
				}
			}
			else if (ActiveUnmirroredLayout == StepActiveUnmirroredLayout.SideBySideBothLeft)
			{
				if (hasVolume)
				{
					left = Math.Min(left, baseSpineX - volumeWidth);
					right = Math.Max(right, baseSpineX);
				}
				if (hasDelta)
				{
					left = Math.Min(left, baseSpineX - volumeWidth - deltaWidth);
					right = Math.Max(right, baseSpineX - volumeWidth);
				}
			}
			else
			{
				if (hasVolume)
					left = Math.Min(left, baseSpineX - volumeWidth);
				if (hasDelta)
					left = Math.Min(left, baseSpineX - deltaWidth);
				right = baseSpineX;
			}

			return right > left;
		}

		private void DrawProfileStatistics(StepBlock block, float logicalLeft, float logicalRight, float y, bool constrainToLogicalLeft)
		{
			if (block == null || statisticsTextBrushDx == null || statisticsTextFormatDx == null || ChartPanel == null)
				return;

			long totalVolume;
			long totalDelta;
			long maxCumulativeDelta;
			long minCumulativeDelta;
			lock (block.SyncObj)
			{
				totalVolume = block.TotalVolume;
				totalDelta = block.CumulativeDelta;
				maxCumulativeDelta = block.MaxCumulativeDelta;
				minCumulativeDelta = block.MinCumulativeDelta;
			}

			string text = BuildProfileStatisticsText(totalVolume, totalDelta, maxCumulativeDelta, minCumulativeDelta);
			if (string.IsNullOrEmpty(text))
				return;

			float panelLeft = ChartPanel.X + 4f;
			float panelRight = ChartPanel.X + ChartPanel.W - 4f;
			float rectLeft = (float)Math.Ceiling(constrainToLogicalLeft ? Math.Max(panelLeft, logicalLeft) : panelLeft);
			float rectRight = (float)Math.Floor(Math.Min(panelRight, logicalRight - 4f));
			float rowHeight = Math.Max(16f, StatisticsFontSize + 6f);
			float alignedY = (float)Math.Round(y);
			if (rectRight <= rectLeft || alignedY + rowHeight > ChartPanel.Y + ChartPanel.H)
				return;

			statisticsTextFormatDx.TextAlignment = constrainToLogicalLeft ? TextAlignment.Leading : TextAlignment.Trailing;
			RenderTarget.DrawText(text, statisticsTextFormatDx, new RectangleF(rectLeft, alignedY, rectRight - rectLeft, rowHeight), statisticsTextBrushDx, DrawTextOptions.Clip);
		}

		private string BuildProfileStatisticsText(long totalVolume, long totalDelta, long maxCumulativeDelta, long minCumulativeDelta)
		{
			string text = string.Empty;
			if (ShowTotalDelta)
				text = AppendStatisticsToken(text, "T " + FormatSignedValue(totalDelta));
			if (ShowFinishDelta)
				text = AppendStatisticsToken(text, "F " + FormatSignedValue(CalculateFinishDelta(totalDelta, maxCumulativeDelta, minCumulativeDelta)));
			if (ShowDeltaPercent)
			{
				double percent = totalVolume > 0 ? totalDelta / (double)totalVolume * 100.0 : 0.0;
				text = AppendStatisticsToken(text, percent.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "%");
			}
			if (ShowTotalVolume)
				text = AppendStatisticsToken(text, "V " + FormatCompactVolume(totalVolume));
			return text;
		}

		private static long CalculateFinishDelta(long currentDelta, long maxCumulativeDelta, long minCumulativeDelta)
		{
			return currentDelta - (currentDelta >= 0 ? maxCumulativeDelta : minCumulativeDelta);
		}

		private static string FormatSignedValue(long value)
		{
			return value.ToString("+#;-#;0", CultureInfo.InvariantCulture);
		}

		private static string FormatCompactVolume(long volume)
		{
			long absoluteVolume = volume == long.MinValue ? long.MaxValue : Math.Abs(volume);
			if (absoluteVolume >= 1000000)
				return (volume / 1000000.0).ToString("0.0", CultureInfo.InvariantCulture) + "M";
			if (absoluteVolume >= 1000)
				return (volume / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + "K";
			return volume.ToString(CultureInfo.InvariantCulture);
		}

		private static string AppendStatisticsToken(string text, string token)
		{
			return string.IsNullOrEmpty(text) ? token : text + " | " + token;
		}

		private string GetSessionLabel(DateTime sessionStart)
		{
			DateTime tradingDayStart = GetSessionTradingDayStart(sessionStart);
			if (SessionConfiguration == StepSessionConfiguration.AsianLondonNewYork)
			{
				if (sessionStart == tradingDayStart) return "Asia";
				if (sessionStart == tradingDayStart.AddHours(9)) return "London";
				if (sessionStart == tradingDayStart.AddHours(15.5)) return "RTH";
			}
			else
			{
				if (sessionStart == tradingDayStart) return "Overnight";
				if (sessionStart == tradingDayStart.AddHours(15.5)) return "RTH";
			}

			return null;
		}

		private void PrintRenderSkip(Exception ex)
		{
			try
			{
				DateTime now = DateTime.UtcNow;
				if ((now - lastRenderSkipUtc).TotalSeconds < 30) return;
				lastRenderSkipUtc = now;
				Print("OrcaStepProfile: skipped one render frame: " + ex.Message);
			}
			catch { }
		}

		private bool TryGetBlockRenderBounds(ChartControl chartControl, StepBlock block, int fromIdx, int toIdx, out int startIndex, out int endIndex, out float startX, out float endX)
		{
			startIndex = -1;
			endIndex = -1;
			startX = 0f;
			endX = 0f;

			if (block == null || chartControl == null || ChartBars == null || ChartBars.Count <= 0 || BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null || BarsArray[0].Count <= 0)
				return false;

			int lastIndex = Math.Min(BarsArray[0].Count - 1, ChartBars.Count - 1);
			if (lastIndex < 0)
				return false;

			startIndex = Math.Max(0, Math.Min(block.StartBarIndex, lastIndex));
			endIndex = block.EndBarIndex >= 0 ? block.EndBarIndex : lastIndex;
			endIndex = Math.Max(startIndex, Math.Min(endIndex, lastIndex));

			if (endIndex < fromIdx || startIndex > toIdx)
				return false;

			if (!TryGetXByBarIndex(chartControl, startIndex, out startX) || !TryGetXByBarIndex(chartControl, endIndex, out endX))
				return false;

			if (endX < startX)
			{
				float temp = startX;
				startX = endX;
				endX = temp;
			}
			if (endX - startX < 2f)
				endX = startX + 2f;

			return true;
		}

		private bool TryGetXByBarIndex(ChartControl chartControl, int barIndex, out float x)
		{
			x = 0f;
			if (chartControl == null || ChartBars == null || barIndex < 0 || barIndex >= ChartBars.Count)
				return false;

			try
			{
				x = chartControl.GetXByBarIndex(ChartBars, barIndex);
				return !float.IsNaN(x) && !float.IsInfinity(x);
			}
			catch (Exception ex)
			{
				PrintRenderSkip(ex);
				return false;
			}
		}

		private int GetHistoricalProfileDrawWidth(StepBlock block, float startX, float endX)
		{
			int periodWidth = GetPeriodWidthPx(startX, endX);
			if (DynamicProfileWidth)
			{
				int fillPercent = GetHistoricalProfileFillPercent(block);
				return Math.Max(2, Math.Min(periodWidth, (int)(periodWidth * (fillPercent / 100.0))));
			}
			return Math.Max(2, Math.Min(HistoricalProfileWidthPx, periodWidth));
		}

		private int GetHistoricalProfileFillPercent(StepBlock block)
		{
			if (ShouldUseRthTimeWidthOverride(block))
				return RthTimeFillSpacePercent;
			if (ShouldUseNonRthVolumeWidthOverride(block))
				return NonRthFillSpacePercent;
			return FillSpacePercent;
		}

		private bool ShouldUseRthTimeWidthOverride(StepBlock block)
		{
			if (!OverrideRthWidthOnTimeCharts
				|| StepBasis != StepProfileBasis.Session
				|| BarsPeriod == null
				|| BarsPeriod.BarsPeriodType != BarsPeriodType.Minute
				|| block == null)
				return false;

			DateTime tradingDayStart = GetSessionTradingDayStart(block.StartTime);
			return block.StartTime == tradingDayStart.AddHours(15.5);
		}

		private bool ShouldUseNonRthVolumeWidthOverride(StepBlock block)
		{
			if (!OverrideNonRthWidthOnVolumeCharts
				|| StepBasis != StepProfileBasis.Session
				|| BarsPeriod == null
				|| BarsPeriod.BarsPeriodType != BarsPeriodType.Volume
				|| block == null)
				return false;

			DateTime tradingDayStart = GetSessionTradingDayStart(block.StartTime);
			DateTime rthStart = tradingDayStart.AddHours(15.5);
			return block.StartTime >= tradingDayStart && block.StartTime < rthStart;
		}

		private int GetHistoricalDeltaDrawWidth(float startX, float endX)
		{
			int periodWidth = GetPeriodWidthPx(startX, endX);
			if (DynamicHistoricalDeltaWidth)
				return Math.Max(2, Math.Min(periodWidth, (int)(periodWidth * (HistoricalDeltaFillPercent / 100.0))));
			return Math.Max(2, Math.Min(HistoricalDeltaWidthPx, periodWidth));
		}

		private int GetPeriodWidthPx(float startX, float endX)
		{
			return Math.Max(2, (int)Math.Abs(endX - startX));
		}

		private bool PushPeriodClip(float startX, float endX, float panelTop, float panelBottom)
		{
			float panelLeft = ChartPanel.X;
			float panelRight = ChartPanel.X + ChartPanel.W;
			float clipLeft = Math.Max(panelLeft, Math.Min(startX, endX));
			float clipRight = Math.Min(panelRight, Math.Max(startX, endX));
			float clipHeight = panelBottom - panelTop;

			if (clipRight <= clipLeft || clipHeight <= 0f)
				return false;

			RenderTarget.PushAxisAlignedClip(new RectangleF(clipLeft, panelTop, clipRight - clipLeft, clipHeight), SharpDX.Direct2D1.AntialiasMode.PerPrimitive);
			return true;
		}

		private void DrawBlockProfile(ChartControl chartControl, ChartScale chartScale, StepBlock block, float baseSpineX, int profileWidthPx, int deltaWidthPx, float panelTop, float panelBottom, bool facingRight, bool isActiveProfile, StepMirrorArrangement mirrorArrangement, int volumeCompressionTicks)
		{
			Dictionary<double, long> volumeSource;
			lock (block.SyncObj)
			{
				if (block.VolByPrice.Count == 0) return;
				volumeSource = isActiveProfile ? new Dictionary<double, long>(block.VolByPrice) : block.VolByPrice;
			}
			Dictionary<double, long> volMap = BuildAggregatedMap(volumeSource, volumeCompressionTicks, Math.Max(1, VolumeTickCompression));

			long   maxVol   = 0;
			double pocPrice = double.NaN;
			double vahPrice = double.NaN, valPrice = double.NaN;
			bool haveVA = false;

			if (!block.IsVACalculated || isActiveProfile || block.VACalculationCompressionTicks != volumeCompressionTicks)
			{
				foreach (var kvp in volMap) { if (kvp.Value > maxVol) { maxVol = kvp.Value; pocPrice = kvp.Key; } }
				if (maxVol > 0 && ShowValueArea && (ShowVAColor || ShowVALines))
					haveVA = CalcValueArea(volMap, pocPrice, out vahPrice, out valPrice);

				if (!isActiveProfile) { block.MaxVol = maxVol; block.POCPrice = pocPrice; block.VAHPrice = vahPrice; block.VALPrice = valPrice; block.IsVACalculated = true; block.VACalculationCompressionTicks = volumeCompressionTicks; }
			}
			else { maxVol = block.MaxVol; pocPrice = block.POCPrice; vahPrice = block.VAHPrice; valPrice = block.VALPrice; haveVA = !double.IsNaN(vahPrice); }

			if (maxVol <= 0) return;
			double volCompHeight = volumeCompressionTicks * TickSize;

			if ((isActiveProfile && ShowActiveVolume) || (!isActiveProfile && ShowHistoricalVolume))
			{
				foreach (var kvp in volMap)
				{
					double price = kvp.Key;
					long   vol   = kvp.Value;
					int yTop = chartScale.GetYByValue(price + volCompHeight);
					int yBot = chartScale.GetYByValue(price);
					if (yBot < panelTop - 20 || yTop > panelBottom + 20) continue;

					int   rowHeight = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
					float drawY     = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;
					float barWidth = (float)(profileWidthPx * (vol / (double)maxVol));
					if (barWidth < 0.5f) continue;

					RectangleF rect;
					if (facingRight)
					{
						if (MirrorProfiles && mirrorArrangement == StepMirrorArrangement.VolumeLeft_DeltaRight)
							rect = new RectangleF(baseSpineX - barWidth, drawY, barWidth, rowHeight);
						else
							rect = new RectangleF(baseSpineX, drawY, barWidth, rowHeight);
					}
					else
					{
						if (MirrorProfiles)
						{
							if (mirrorArrangement == StepMirrorArrangement.VolumeLeft_DeltaRight)
								rect = new RectangleF(baseSpineX - deltaWidthPx - barWidth, drawY, barWidth, rowHeight);
							else
								rect = new RectangleF(baseSpineX - profileWidthPx, drawY, barWidth, rowHeight);
						}
						else rect = new RectangleF(baseSpineX - barWidth, drawY, barWidth, rowHeight);
					}

					bool insideVA = haveVA && price >= valPrice - TickSize * 0.01 && price <= vahPrice + TickSize * 0.01;
					SharpDX.Direct2D1.SolidColorBrush brush;

					if (ShowPOC && Math.Abs(price - pocPrice) < TickSize * 0.01) brush = pocBrushDx;
					else if (UseGradient)
					{
						var palette = (ShowValueArea && ShowVAColor && insideVA) ? (isActiveProfile ? vaGradientBrushes : histVaGradientBrushes) : (isActiveProfile ? volGradientBrushes : histVolGradientBrushes);
						if (palette != null)
						{
							int gradIdx = (int)((vol / (double)maxVol) * (palette.Length - 1));
							brush = palette[Math.Min(palette.Length - 1, Math.Max(0, gradIdx))];
						}
						else brush = isActiveProfile ? volBrushDx : histVolBrushDx;
					}
					else
					{
						if (ShowValueArea && ShowVAColor && insideVA) brush = isActiveProfile ? vaVolBrushDx : histVaVolBrushDx;
						else brush = isActiveProfile ? volBrushDx : histVolBrushDx;
					}
					RenderTarget.FillRectangle(rect, brush);
				}

				if (haveVA && ShowValueArea && ShowVALines && vaLineBrushDx != null)
				{
					float lineLeft, lineRight;
					int usedDeltaWidth = deltaWidthPx;
					if (facingRight)
					{
						if (MirrorProfiles)
						{
							if (mirrorArrangement == StepMirrorArrangement.VolumeLeft_DeltaRight)
							{
								lineLeft = baseSpineX - profileWidthPx - 2;
								lineRight = baseSpineX + usedDeltaWidth + 2;
							}
							else
							{
								lineLeft = baseSpineX - usedDeltaWidth - 2;
								lineRight = baseSpineX + profileWidthPx + 2;
							}
						}
						else { lineLeft = baseSpineX - 2; lineRight = baseSpineX + profileWidthPx + 2; }
					}
					else
					{
						if (MirrorProfiles)
						{
							if (mirrorArrangement == StepMirrorArrangement.VolumeLeft_DeltaRight)
							{
								float splitX = baseSpineX - usedDeltaWidth;
								lineLeft = splitX - profileWidthPx - 2;
								lineRight = splitX + usedDeltaWidth + 2;
							}
							else
							{
								float splitX = baseSpineX - profileWidthPx;
								lineLeft = splitX - usedDeltaWidth - 2;
								lineRight = splitX + profileWidthPx + 2;
							}
						}
						else if (isActiveProfile && ActiveUnmirroredLayout == StepActiveUnmirroredLayout.SideBySideBothLeft)
						{
							lineLeft = baseSpineX - profileWidthPx - usedDeltaWidth - 2;
							lineRight = baseSpineX + 2;
						}
						else { lineLeft = baseSpineX - profileWidthPx - 2; lineRight = baseSpineX + 2; }
					}
					float yVAH = chartScale.GetYByValue(vahPrice + volCompHeight);
					if (yVAH >= panelTop - 5 && yVAH <= panelBottom + 5) RenderTarget.DrawLine(new Vector2(lineLeft, yVAH), new Vector2(lineRight, yVAH), vaLineBrushDx, VALineThickness, vaLineStrokeDx);
					float yVAL = chartScale.GetYByValue(valPrice);
					if (yVAL >= panelTop - 5 && yVAL <= panelBottom + 5) RenderTarget.DrawLine(new Vector2(lineLeft, yVAL), new Vector2(lineRight, yVAL), vaLineBrushDx, VALineThickness, vaLineStrokeDx);
				}
			}
		}

		private void DrawBlockDelta(ChartControl chartControl, ChartScale chartScale, StepBlock block, float baseSpineX, int profileWidthPx, int deltaWidthPx, float panelTop, float panelBottom, bool facingRight, bool isActiveProfile, StepMirrorArrangement mirrorArrangement, int deltaCompTicks)
		{
			Dictionary<double, long> deltaMap;
			lock (block.SyncObj) { if (block.DeltaByPrice.Count == 0) return; deltaMap = isActiveProfile ? new Dictionary<double, long>(block.DeltaByPrice) : block.DeltaByPrice; }

			double deltaComp = deltaCompTicks * TickSize;
			var groupedDelta = new Dictionary<double, long>();
			foreach (var kvp in deltaMap) { double bucketPrice = Math.Floor(kvp.Key / deltaComp + 0.000001) * deltaComp; if (groupedDelta.TryGetValue(bucketPrice, out long existing)) groupedDelta[bucketPrice] = existing + kvp.Value; else groupedDelta[bucketPrice] = kvp.Value; }

			long maxAbsDelta = 0;
			foreach (var kvp in groupedDelta) { long absVal = Math.Abs(kvp.Value); if (absVal > maxAbsDelta) maxAbsDelta = absVal; }
			if (maxAbsDelta <= 0) return;

			foreach (var kvp in groupedDelta)
			{
				int yTop = chartScale.GetYByValue(kvp.Key + deltaComp);
				int yBot = chartScale.GetYByValue(kvp.Key);
				if (yBot < panelTop - 20 || yTop > panelBottom + 20) continue;

				int   height = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
				float drawY  = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;
				float w = (float)(deltaWidthPx * (Math.Abs(kvp.Value) / (double)maxAbsDelta));
				if (w < 0.5f && !ShowDeltaText) continue;
				w = Math.Max(w, 0.5f);

				SharpDX.Direct2D1.SolidColorBrush deltaBrush = SelectDeltaBrush(kvp.Value, maxAbsDelta, isActiveProfile);

				float deltaRootX;
				bool deltaFlowsRight;

				if (facingRight)
				{
					if (MirrorProfiles && mirrorArrangement == StepMirrorArrangement.VolumeLeft_DeltaRight)
					{ deltaRootX = baseSpineX; deltaFlowsRight = true; }
					else if (MirrorProfiles)
					{ deltaRootX = baseSpineX; deltaFlowsRight = false; }
					else
					{ deltaRootX = baseSpineX; deltaFlowsRight = true; }
				}
				else
				{
					if (MirrorProfiles)
					{
						if (mirrorArrangement == StepMirrorArrangement.VolumeLeft_DeltaRight)
						{ deltaRootX = baseSpineX - deltaWidthPx; deltaFlowsRight = true; }
						else
						{ deltaRootX = baseSpineX - profileWidthPx; deltaFlowsRight = false; }
					}
					else if (isActiveProfile && ActiveUnmirroredLayout == StepActiveUnmirroredLayout.SideBySideBothLeft)
					{ deltaRootX = baseSpineX - profileWidthPx; deltaFlowsRight = false; }
					else
					{ deltaRootX = baseSpineX; deltaFlowsRight = false; }
				}

				RectangleF rect;
				if (deltaFlowsRight) rect = new RectangleF(deltaRootX, drawY, w, height);
				else rect = new RectangleF(deltaRootX - w, drawY, w, height);

				RenderTarget.FillRectangle(rect, deltaBrush);

				if (!isActiveProfile && HistoricalDeltaHoverEnabled && kvp.Value != 0 && chartControl.IsMouseOver
					&& Mouse.LeftButton == MouseButtonState.Released && Mouse.RightButton == MouseButtonState.Released
					&& deltaMouseX >= Math.Max(rect.Left, historicalDeltaClipLeft)
					&& deltaMouseX < Math.Min(rect.Right, historicalDeltaClipRight)
					&& deltaMouseY >= Math.Max(rect.Top, panelTop) && deltaMouseY < Math.Min(rect.Bottom, panelBottom))
				{
					deltaHoverHit = true;
					deltaHoverValue = kvp.Value;
				}

				SharpDX.Direct2D1.SolidColorBrush labelBrush = kvp.Value >= 0 ? deltaTextBrushDx : negativeDeltaTextBrushDx;
				bool showRowText = ShowDeltaText && rect.Height >= 6 && Math.Abs(kvp.Value) >= DeltaTextMinThreshold;
				if (!isActiveProfile && HistoricalDeltaTextMode != StepHistoricalDeltaTextMode.FollowGlobal)
					showRowText = HistoricalDeltaTextMode == StepHistoricalDeltaTextMode.Filtered
						&& rect.Height >= HistoricalDeltaTextMinRowHeight
						&& historicalTextBarSpacing >= HistoricalDeltaTextMinBarSpacing
						&& Math.Abs(kvp.Value) >= HistoricalDeltaTextMinValue;
				if (showRowText && deltaTextFormatDx != null && labelBrush != null)
				{
					string text = kvp.Value > 0 ? $"+{kvp.Value}" : kvp.Value.ToString();

					// Dynamically toggle the DirectX bounding box text alignment to stick left or right depending on the flow direction
					deltaTextFormatDx.TextAlignment = deltaFlowsRight ? SharpDX.DirectWrite.TextAlignment.Leading : SharpDX.DirectWrite.TextAlignment.Trailing;

					float maxW = 200f; // Excessively wide boundary avoids truncating labels extending beyond tiny delta bars
					RectangleF textRect;
					if (deltaFlowsRight)
						textRect = new RectangleF(deltaRootX + 2, drawY, maxW, height);
					else
						textRect = new RectangleF(deltaRootX - maxW - 2, drawY, maxW, height);

					RenderTarget.DrawText(text, deltaTextFormatDx, textRect, labelBrush);
				}
			}
		}

		#endregion

		#region DX Resources
		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxResourceRenderTarget != IntPtr.Zero && dxResourceRenderTarget != currentTarget)
				DisposeDx();

			if (volBrushDx == null) volBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VolumeBrush, ActiveVolumeOpacity));
			if (histVolBrushDx == null) histVolBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VolumeBrush, HistoricalVolumeOpacity));
			if (pocBrushDx == null) pocBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(POCBrush, 1f));
			if (posDeltaBrushDx == null) posDeltaBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(PositiveDeltaBrush, ActiveDeltaOpacity));
			if (negDeltaBrushDx == null) negDeltaBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaBrush, ActiveDeltaOpacity));
			if (histPosDeltaBrushDx == null) histPosDeltaBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(PositiveDeltaBrush, HistoricalDeltaOpacity));
			if (histNegDeltaBrushDx == null) histNegDeltaBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaBrush, HistoricalDeltaOpacity));
			if (blockSepBrushDx == null) blockSepBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(BlockSeparatorBrush, 0.4f));
			if (sessionLabelBrushDx == null) sessionLabelBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(WpfBrushes.LightGray, 0.9f));
			if (vaVolBrushDx == null) vaVolBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VABrush, ActiveVolumeOpacity));
			if (histVaVolBrushDx == null) histVaVolBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VABrush, HistoricalVolumeOpacity));
			if (vaLineBrushDx == null) vaLineBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VALineBrush, 1f));
			if (vaLineStrokeDx == null)
			{
				DashStyle ds;
				switch (VALineStyle)
				{
					case StepVALineStyleEnum.Solid: ds = DashStyle.Solid; break;
					case StepVALineStyleEnum.Dot: ds = DashStyle.Dot; break;
					case StepVALineStyleEnum.DashDot: ds = DashStyle.DashDot; break;
					default: ds = DashStyle.Dash; break;
				}
				vaLineStrokeDx = new SharpDX.Direct2D1.StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = ds });
			}
			if (deltaTextBrushDx == null) deltaTextBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaTextBrush, 1f));
			if (negativeDeltaTextBrushDx == null) negativeDeltaTextBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaTextBrush, 1f));
			if (HistoricalDeltaHoverEnabled && deltaHoverBackgroundDx == null)
				deltaHoverBackgroundDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(0.04f, 0.04f, 0.04f, 0.96f));
			if (HistoricalDeltaHoverEnabled && deltaHoverFormatDx == null)
				deltaHoverFormatDx = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", 12f)
				{ WordWrapping = WordWrapping.NoWrap, ParagraphAlignment = ParagraphAlignment.Center };
			if (statisticsTextBrushDx == null) statisticsTextBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(StatisticsTextBrush, 1f));
			if (deltaTextFormatDx == null)
			{
				deltaTextFormatDx = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, FontStretch.Normal, DeltaTextFontSize)
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing, ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center
				};
			}
			float statisticsFontSize = Math.Max(8f, StatisticsFontSize);
			if (statisticsTextFormatDx == null || Math.Abs(lastBuiltStatisticsFontSize - statisticsFontSize) > 0.001f)
			{
				if (statisticsTextFormatDx != null) statisticsTextFormatDx.Dispose();
				statisticsTextFormatDx = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, FontStretch.Normal, statisticsFontSize)
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing,
					WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap,
					ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center
				};
				lastBuiltStatisticsFontSize = statisticsFontSize;
			}
			if (sessionLabelTextFormatDx == null)
			{
				sessionLabelTextFormatDx = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, FontStretch.Normal, 12f)
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Center,
					ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center
				};
			}

			int steps = Math.Max(2, GradientSteps);
			if (UseGradient && (volGradientBrushes == null || lastBuiltGradientSteps != steps))
			{
				volGradientBrushes = BuildGradientPalette(VolumeBrush, steps, ActiveVolumeOpacity);
				histVolGradientBrushes = BuildGradientPalette(VolumeBrush, steps, HistoricalVolumeOpacity);
				lastBuiltGradientSteps = steps;
			}
			if (UseGradient && ShowValueArea && ShowVAColor && (vaGradientBrushes == null || lastBuiltVAGradientSteps != steps))
			{
				vaGradientBrushes = BuildGradientPalette(VABrush, steps, ActiveVolumeOpacity);
				histVaGradientBrushes = BuildGradientPalette(VABrush, steps, HistoricalVolumeOpacity);
				lastBuiltVAGradientSteps = steps;
			}
			int deltaIntensitySteps = Math.Max(2, GradientSteps);
			if (UseDeltaIntensityColoring && (activePositiveDeltaIntensityBrushes == null || activeNegativeDeltaIntensityBrushes == null || historicalPositiveDeltaIntensityBrushes == null || historicalNegativeDeltaIntensityBrushes == null || lastBuiltDeltaIntensitySteps != deltaIntensitySteps || Math.Abs(lastBuiltDeltaIntensityMinOpacity - DeltaIntensityMinOpacity) > 0.0001f || Math.Abs(lastBuiltActiveDeltaIntensityOpacity - ActiveDeltaOpacity) > 0.0001f || Math.Abs(lastBuiltHistoricalDeltaIntensityOpacity - HistoricalDeltaOpacity) > 0.0001f))
			{
				DisposePalette(ref activePositiveDeltaIntensityBrushes);
				DisposePalette(ref activeNegativeDeltaIntensityBrushes);
				DisposePalette(ref historicalPositiveDeltaIntensityBrushes);
				DisposePalette(ref historicalNegativeDeltaIntensityBrushes);
				activePositiveDeltaIntensityBrushes = BuildDeltaIntensityPalette(PositiveDeltaBrush, deltaIntensitySteps, ActiveDeltaOpacity);
				activeNegativeDeltaIntensityBrushes = BuildDeltaIntensityPalette(NegativeDeltaBrush, deltaIntensitySteps, ActiveDeltaOpacity);
				historicalPositiveDeltaIntensityBrushes = BuildDeltaIntensityPalette(PositiveDeltaBrush, deltaIntensitySteps, HistoricalDeltaOpacity);
				historicalNegativeDeltaIntensityBrushes = BuildDeltaIntensityPalette(NegativeDeltaBrush, deltaIntensitySteps, HistoricalDeltaOpacity);
				lastBuiltDeltaIntensitySteps = deltaIntensitySteps;
				lastBuiltDeltaIntensityMinOpacity = DeltaIntensityMinOpacity;
				lastBuiltActiveDeltaIntensityOpacity = ActiveDeltaOpacity;
				lastBuiltHistoricalDeltaIntensityOpacity = HistoricalDeltaOpacity;
			}
			else if (!UseDeltaIntensityColoring && (activePositiveDeltaIntensityBrushes != null || activeNegativeDeltaIntensityBrushes != null || historicalPositiveDeltaIntensityBrushes != null || historicalNegativeDeltaIntensityBrushes != null))
			{
				DisposePalette(ref activePositiveDeltaIntensityBrushes);
				DisposePalette(ref activeNegativeDeltaIntensityBrushes);
				DisposePalette(ref historicalPositiveDeltaIntensityBrushes);
				DisposePalette(ref historicalNegativeDeltaIntensityBrushes);
				lastBuiltDeltaIntensitySteps = -1;
				lastBuiltDeltaIntensityMinOpacity = -1f;
				lastBuiltActiveDeltaIntensityOpacity = -1f;
				lastBuiltHistoricalDeltaIntensityOpacity = -1f;
			}
			dxResourceRenderTarget = currentTarget;
		}

		private int ResolveVolumeCompressionTicks(ChartScale chartScale)
		{
			int baseTicks = Math.Max(1, VolumeTickCompression);
			if (!UseDynamicVolumeAggregation || chartScale == null || ChartPanel == null || TickSize <= 0)
				return baseTicks;

			double visibleTicks = Math.Max(1.0, (chartScale.MaxValue - chartScale.MinValue) / TickSize);
			double ticksPerPixel = visibleTicks / Math.Max(1.0, ChartPanel.H);
			double desiredTicks = ticksPerPixel * 3.0 * Math.Max(0.1, VolumeDynamicAggregationMultiplier);
			int resolved = RoundToGentleVolumeTicks(desiredTicks);
			int maxTicks = Math.Max(baseTicks, MaxDynamicVolumeTicks);

			return Math.Max(baseTicks, Math.Min(maxTicks, resolved));
		}

		private int RoundToGentleVolumeTicks(double desiredTicks)
		{
			if (desiredTicks <= 1.25) return 1;
			if (desiredTicks <= 2.25) return 2;
			if (desiredTicks <= 3.25) return 3;
			if (desiredTicks <= 4.50) return 4;
			if (desiredTicks <= 6.50) return 6;
			if (desiredTicks <= 8.50) return 8;
			if (desiredTicks <= 10.50) return 10;
			return Math.Max(10, (int)(Math.Round(desiredTicks / 5.0) * 5));
		}

		private Dictionary<double, long> BuildAggregatedMap(Dictionary<double, long> source, int targetTicks, int sourceBaseTicks)
		{
			int baseTicks = Math.Max(1, sourceBaseTicks);
			targetTicks = Math.Max(baseTicks, targetTicks);
			if (targetTicks == baseTicks)
				return source;

			double comp = targetTicks * TickSize;
			var aggregated = new Dictionary<double, long>(source.Count);
			foreach (var kvp in source)
			{
				double bucketPrice = Math.Floor(kvp.Key / comp + 0.000001) * comp;
				if (aggregated.TryGetValue(bucketPrice, out long existing))
					aggregated[bucketPrice] = existing + kvp.Value;
				else
					aggregated[bucketPrice] = kvp.Value;
			}

			return aggregated;
		}

		private int ClampDeltaCompression(int compression)
		{
			int min = Math.Max(1, DynamicDeltaMinCompression);
			int max = Math.Max(min, DynamicDeltaMaxCompression);
			if (compression < min) return min;
			if (compression > max) return max;
			return compression;
		}

		private SharpDX.Direct2D1.SolidColorBrush[] BuildGradientPalette(WpfBrush baseBrush, int steps, float opacity)
		{
			var baseColor = (baseBrush as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			var palette = new SharpDX.Direct2D1.SolidColorBrush[steps];
			for (int i = 0; i < steps; i++)
			{
				float b = MinBrightness + (i / (float)(steps - 1)) * (1f - MinBrightness);
				var c = new Color4((baseColor.R / 255f) * b, (baseColor.G / 255f) * b, (baseColor.B / 255f) * b, (baseColor.A / 255f) * opacity);
				palette[i] = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, c);
			}
			return palette;
		}

		private SharpDX.Direct2D1.SolidColorBrush[] BuildDeltaIntensityPalette(WpfBrush baseBrush, int steps, float maxOpacity)
		{
			var baseColor = (baseBrush as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			var palette = new SharpDX.Direct2D1.SolidColorBrush[steps];
			float minOpacity = Math.Max(0f, Math.Min(1f, DeltaIntensityMinOpacity));
			for (int i = 0; i < steps; i++)
			{
				float ratio = i / (float)(steps - 1);
				float opacity = maxOpacity * (minOpacity + ((1f - minOpacity) * ratio));
				var c = new Color4(baseColor.R / 255f, baseColor.G / 255f, baseColor.B / 255f, (baseColor.A / 255f) * opacity);
				palette[i] = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, c);
			}
			return palette;
		}

		private SharpDX.Direct2D1.SolidColorBrush SelectDeltaBrush(long delta, long maxAbsDelta, bool isActiveProfile)
		{
			SharpDX.Direct2D1.SolidColorBrush fallback = isActiveProfile
				? (delta >= 0 ? posDeltaBrushDx : negDeltaBrushDx)
				: (delta >= 0 ? histPosDeltaBrushDx : histNegDeltaBrushDx);
			if (!UseDeltaIntensityColoring || maxAbsDelta <= 0)
				return fallback;

			SharpDX.Direct2D1.SolidColorBrush[] palette = isActiveProfile
				? (delta >= 0 ? activePositiveDeltaIntensityBrushes : activeNegativeDeltaIntensityBrushes)
				: (delta >= 0 ? historicalPositiveDeltaIntensityBrushes : historicalNegativeDeltaIntensityBrushes);
			if (palette == null || palette.Length == 0)
				return fallback;

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
				for (int i = 0; i < palette.Length; i++)
					if (palette[i] != null) palette[i].Dispose();
			}
			palette = null;
		}

		private Color4 ToDxColor(WpfBrush b, float alphaMult)
		{
			var c = (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alphaMult);
		}
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[TypeConverter(typeof(StepProfileBasisConverter))]
		[Display(Name = "Step Basis", Description = "Builds each profile by elapsed time, traded contract amount, or market session.", GroupName = "01 Display", Order = 0)]
		public StepProfileBasis StepBasis { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(StepSessionConfigurationConverter))]
		[Display(Name = "Session Configuration", Description = "Uses Overnight/RTH or Asian/London/New York profiles when Step Basis is Session. Times follow Eastern-aligned chart timestamps; RTH End Time defines the close.", GroupName = "01 Display", Order = 4)]
		public StepSessionConfiguration SessionConfiguration { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Step Interval", Description = "Time interval used when Step Basis is Time.", GroupName = "01 Display", Order = 1)]
		public StepIntervalType StepInterval { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(StepVolumeAmountConverter))]
		[Display(Name = "Step Volume Amount", Description = "Contracts per completed profile when Step Basis is Volume.", GroupName = "01 Display", Order = 2)]
		public StepVolumeAmountType StepVolumeAmount { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(StepVolumeResetAnchorConverter))]
		[Display(Name = "Volume Step Start", Description = "Resets volume-step counting daily at 6:00 PM or 9:30 AM. This does not filter trades; use RTH Only to exclude overnight volume.", GroupName = "01 Display", Order = 3)]
		public StepVolumeResetAnchor VolumeResetAnchor { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Volume Row Size (ticks)", Description = "Fixed volume row height and minimum row size when dynamic volume aggregation is enabled.", GroupName = "04 Rows & Scaling", Order = 0)]
		public int VolumeTickCompression { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Volume Aggregation", Description = "Gently increases volume row height when the visible price range is large.", GroupName = "04 Rows & Scaling", Order = 1)]
		public bool UseDynamicVolumeAggregation { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "Volume Dynamic Multiplier", Description = "Lower values keep volume rows more granular; higher values aggregate sooner.", GroupName = "04 Rows & Scaling", Order = 2)]
		public double VolumeDynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Max Dynamic Volume Ticks", Description = "Upper cap for dynamic volume row height. The fixed volume row size remains the minimum.", GroupName = "04 Rows & Scaling", Order = 3)]
		public int MaxDynamicVolumeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Order Flow Row Size (ticks)", Description = "Fixed Delta row height when dynamic order-flow aggregation is off.", GroupName = "04 Rows & Scaling", Order = 4)]
		public int DeltaTickCompression { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Order Flow Aggregation", Description = "Dynamically increases Delta row height as the visible price range expands.", GroupName = "04 Rows & Scaling", Order = 5)]
		public bool UseDynamicAggregation { get; set; }

		[NinjaScriptProperty]
		[Range(2, 40)]
		[Display(Name = "Order Flow Row Min Pixels", Description = "Target minimum Delta row height before applying the multiplier.", GroupName = "04 Rows & Scaling", Order = 6)]
		public int DeltaDynamicRowMinPixels { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "Order Flow Dynamic Multiplier", Description = "Lower values keep Delta rows more granular; higher values aggregate sooner.", GroupName = "04 Rows & Scaling", Order = 7)]
		public double DynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Dynamic Order Flow Min Ticks", GroupName = "04 Rows & Scaling", Order = 8)]
		public int DynamicDeltaMinCompression { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Dynamic Order Flow Max Ticks", GroupName = "04 Rows & Scaling", Order = 9)]
		public int DynamicDeltaMaxCompression { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(StepTradeSourceModeConverter))]
		[Display(Name = "Trade Source Mode", Description = "Secondary Tick Series is the fast estimated-Delta path. Tick Replay Last Events uses trade-time Bid/Ask and requires Tick Replay on the chart.", GroupName = "11 Advanced - Data", Order = 0)]
		public StepTradeSourceMode TradeSourceMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "RTH Only", Description = "Filters Time and Volume modes to the configured RTH window. Ignored in Session mode.", GroupName = "03 Sessions", Order = 0)]
		public bool RTHOnly { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "RTH Start Time", Description = "Defines the RTH filter start for Time and Volume modes. Session mode uses a fixed 9:30 AM New York open.", GroupName = "03 Sessions", Order = 1)]
		public DateTime RTHStart { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "RTH End Time", Description = "Defines the RTH close and the end of the New York session.", GroupName = "03 Sessions", Order = 2)]
		public DateTime RTHEnd { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Historical Volume Width", Description = "Automatically size completed volume profiles to their own step period. Historical Volume Fill controls the percentage of available width.", GroupName = "02 Profile Layout", Order = 7)]
		public bool DynamicProfileWidth { get; set; }

		[NinjaScriptProperty]
		[Range(10, 100)]
		[Display(Name = "Historical Volume Fill (%)", Description = "Maximum historical volume width as a percentage of its step period when dynamic width is enabled. Session-specific overrides can replace this percentage.", GroupName = "02 Profile Layout", Order = 8)]
		public int FillSpacePercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Override Non-RTH Width on Volume Charts", Description = "Uses a separate width percentage for completed Overnight, Asia, and London profiles on native Volume charts.", GroupName = "02 Profile Layout", Order = 13)]
		public bool OverrideNonRthWidthOnVolumeCharts { get; set; }

		[NinjaScriptProperty]
		[Range(10, 100)]
		[Display(Name = "Non-RTH Volume Fill (%)", Description = "Completed Overnight, Asia, and London profile width on native Volume charts when the override is enabled.", GroupName = "02 Profile Layout", Order = 14)]
		public int NonRthFillSpacePercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Override RTH Width on Time Charts", Description = "Uses a separate width percentage for completed RTH profiles on Minute-based time charts when Session basis is selected.", GroupName = "02 Profile Layout", Order = 15)]
		public bool OverrideRthWidthOnTimeCharts { get; set; }

		[NinjaScriptProperty]
		[Range(10, 100)]
		[Display(Name = "RTH Volume Fill on Time Charts (%)", Description = "Completed RTH profile width on Minute-based time charts when the RTH override is enabled.", GroupName = "02 Profile Layout", Order = 16)]
		public int RthTimeFillSpacePercent { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Historical Volume Width (px)", Description = "Fixed width in pixels for completed volume profiles when Dynamic Historical Volume Width is off.", GroupName = "02 Profile Layout", Order = 9)]
		public int HistoricalProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Active Volume Width (px)", Description = "Maximum width in pixels for the developing volume profile.", GroupName = "02 Profile Layout", Order = 4)]
		public int ActiveProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Active Delta Width (px)", Description = "Maximum width in pixels for the developing delta profile.", GroupName = "02 Profile Layout", Order = 5)]
		public int ActiveDeltaWidthPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Historical Delta Width", Description = "Sizes completed delta profiles as a percentage of their own step period width.", GroupName = "02 Profile Layout", Order = 10)]
		public bool DynamicHistoricalDeltaWidth { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Historical Delta Fill (%)", Description = "Maximum completed delta width as a percentage of its own step period when dynamic width is enabled.", GroupName = "02 Profile Layout", Order = 11)]
		public int HistoricalDeltaFillPercent { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Historical Delta Width (px)", Description = "Fixed completed delta width used when dynamic historical delta width is disabled.", GroupName = "02 Profile Layout", Order = 12)]
		public int HistoricalDeltaWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 500)]
		[Display(Name = "Right Offset (px)", Description = "Horizontal distance in pixels from the chart right edge to the active profile anchor.", GroupName = "02 Profile Layout", Order = 6)]
		public int RightOffsetPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Profile Row Spacing (px)", Description = "Gap in pixels between horizontal price rows within the volume and delta profiles.", GroupName = "02 Profile Layout", Order = 17)]
		public int ProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Mirror Profiles", Description = "Place volume and delta on opposite sides of their shared anchor. Use the separate historical and active arrangements below.", GroupName = "02 Profile Layout", Order = 0)]
		public bool MirrorProfiles { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Historical Mirror Arrangement", Description = "When Mirror Profiles is checked, configures the historical profile arrangement. Delta left / Volume right keeps historical Volume facing right.", GroupName = "02 Profile Layout", Order = 1)]
		public StepMirrorArrangement MirrorArrangement { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Active Mirror Arrangement", Description = "When Mirror Profiles is checked, configures the active profile independently from the historical profile arrangement.", GroupName = "02 Profile Layout", Order = 2)]
		public StepMirrorArrangement ActiveMirrorArrangement { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(StepActiveUnmirroredLayoutConverter))]
		[Display(Name = "Active Unmirrored Layout", Description = "When Mirror Profiles is off, keeps the active profiles overlaid or places Delta left of Volume with both facing left.", GroupName = "02 Profile Layout", Order = 3)]
		public StepActiveUnmirroredLayout ActiveUnmirroredLayout { get; set; }

		[Display(Name = "Draw Behind Candles", Description = "Places profile rendering below the native chart bars so candles remain fully visible.", GroupName = "02 Profile Layout", Order = 18)]
		public bool DrawBehindCandles { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Active Volume", Description = "Show the volume histogram for the currently developing Step Profile.", GroupName = "01 Display", Order = 5)]
		public bool ShowActiveVolume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Historical Volume", Description = "Show volume histograms for completed Step Profiles.", GroupName = "01 Display", Order = 7)]
		public bool ShowHistoricalVolume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Active Delta", Description = "Show the delta histogram for the currently developing Step Profile.", GroupName = "01 Display", Order = 6)]
		public bool ShowActiveDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Historical Delta", Description = "Show delta histograms for completed Step Profiles.", GroupName = "01 Display", Order = 8)]
		public bool ShowHistoricalDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show POC", Description = "Highlight each volume profile point of control, the price row with the highest volume.", GroupName = "06 POC & Value Area", Order = 0)]
		public bool ShowPOC { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Block Separators", Description = "Show vertical separators between Step Profile periods.", GroupName = "10 Display Controls", Order = 0)]
		public bool ShowBlockSeparators { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Gradient", Description = "Vary volume-row brightness with its volume relative to the largest row in the profile.", GroupName = "05 Profile Colors", Order = 3)]
		public bool UseGradient { get; set; }

		[NinjaScriptProperty]
		[Range(2, 64)]
		[Display(Name = "Gradient Steps", GroupName = "05 Profile Colors", Order = 5)]
		public int GradientSteps { get; set; }

		[NinjaScriptProperty]
		[Range(0.05, 1.0)]
		[Display(Name = "Minimum Profile Brightness", Description = "Minimum brightness for lower-volume profile rows when the gradient is enabled.", GroupName = "05 Profile Colors", Order = 4)]
		public float MinBrightness { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Value Area", Description = "Enable value-area calculation and display for each volume profile. Shading and boundary lines are controlled separately below.", GroupName = "06 POC & Value Area", Order = 2)]
		public bool ShowValueArea { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Shade Value Area", Description = "Use Value Area Color for volume rows inside the value area when Show Value Area is enabled.", GroupName = "06 POC & Value Area", Order = 4)]
		public bool ShowVAColor { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Value Area Boundaries", Description = "Draw the upper and lower value-area boundaries using the selected line color, width and style.", GroupName = "06 POC & Value Area", Order = 6)]
		public bool ShowVALines { get; set; }

		[NinjaScriptProperty]
		[Range(50, 95)]
		[Display(Name = "Value Area (%)", Description = "Percentage of each profile volume included in its value area.", GroupName = "06 POC & Value Area", Order = 3)]
		public int ValueAreaPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 6.0)]
		[Display(Name = "Boundary Line Width (px)", GroupName = "06 POC & Value Area", Order = 8)]
		public float VALineThickness { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Boundary Line Style", GroupName = "06 POC & Value Area", Order = 9)]
		public StepVALineStyleEnum VALineStyle { get; set; }

		[XmlIgnore]
		[Display(Name = "Value Area Color", GroupName = "06 POC & Value Area", Order = 5)]
		public WpfBrush VABrush { get; set; }
		[Browsable(false)]
		public string VABrushSerialize { get { return Serialize.BrushToString(VABrush); } set { VABrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Boundary Line Color", GroupName = "06 POC & Value Area", Order = 7)]
		public WpfBrush VALineBrush { get; set; }
		[Browsable(false)]
		public string VALineBrushSerialize { get { return Serialize.BrushToString(VALineBrush); } set { VALineBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Volume Color", GroupName = "05 Profile Colors", Order = 0)]
		public WpfBrush VolumeBrush { get; set; }
		[Browsable(false)]
		public string VolumeBrushSerialize { get { return Serialize.BrushToString(VolumeBrush); } set { VolumeBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Active Volume Opacity", GroupName = "05 Profile Colors", Order = 1)]
		public float ActiveVolumeOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Historical Volume Opacity", GroupName = "05 Profile Colors", Order = 2)]
		public float HistoricalVolumeOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Color", GroupName = "06 POC & Value Area", Order = 1)]
		public WpfBrush POCBrush { get; set; }
		[Browsable(false)]
		public string POCBrushSerialize { get { return Serialize.BrushToString(POCBrush); } set { POCBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Positive Delta", GroupName = "05 Profile Colors", Order = 6)]
		public WpfBrush PositiveDeltaBrush { get; set; }
		[Browsable(false)]
		public string PositiveDeltaBrushSerialize { get { return Serialize.BrushToString(PositiveDeltaBrush); } set { PositiveDeltaBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Negative Delta", GroupName = "05 Profile Colors", Order = 7)]
		public WpfBrush NegativeDeltaBrush { get; set; }
		[Browsable(false)]
		public string NegativeDeltaBrushSerialize { get { return Serialize.BrushToString(NegativeDeltaBrush); } set { NegativeDeltaBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Active Delta Opacity", GroupName = "05 Profile Colors", Order = 8)]
		public float ActiveDeltaOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Historical Delta Opacity", GroupName = "05 Profile Colors", Order = 9)]
		public float HistoricalDeltaOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Delta Intensity Color", Description = "Scale delta-row opacity by absolute delta relative to the strongest row in the profile.", GroupName = "05 Profile Colors", Order = 10)]
		public bool UseDeltaIntensityColoring { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Delta Intensity Min Opacity", Description = "Minimum opacity used for the weakest visible delta rows when intensity coloring is enabled.", GroupName = "05 Profile Colors", Order = 11)]
		public float DeltaIntensityMinOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Block Separator Color", GroupName = "10 Display Controls", Order = 1)]
		public WpfBrush BlockSeparatorBrush { get; set; }
		[Browsable(false)]
		public string BlockSeparatorBrushSerialize { get { return Serialize.BrushToString(BlockSeparatorBrush); } set { BlockSeparatorBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta Text", Description = "Show active delta labels. Also controls historical labels when Historical Label Mode is FollowGlobal.", GroupName = "07 Text - Delta", Order = 0)]
		public bool ShowDeltaText { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Historical Label Mode", Description = "FollowGlobal uses Text - Delta settings. Filtered uses all three historical thresholds below independently. HoverOnly reveals the hovered delta row. Hidden disables historical labels and hover.", GroupName = "08 Text - Historical Delta", Order = 0)]
		public StepHistoricalDeltaTextMode HistoricalDeltaTextMode { get; set; }
		[NinjaScriptProperty, Range(0, int.MaxValue)]
		[Display(Name = "Minimum Absolute Delta to Show Text", Description = "Filtered mode: minimum absolute displayed row delta; zero includes every value.", GroupName = "08 Text - Historical Delta", Order = 1)]
		public int HistoricalDeltaTextMinValue { get; set; }
		[NinjaScriptProperty, Range(0, 100)]
		[Display(Name = "Minimum Row Height (px)", Description = "Filtered mode: hides text below this rendered row height. Dynamic aggregation can keep rows tall even when zoomed out.", GroupName = "08 Text - Historical Delta", Order = 2)]
		public int HistoricalDeltaTextMinRowHeight { get; set; }
		[NinjaScriptProperty, Range(0, 100)]
		[Display(Name = "Minimum Bar Spacing (px)", Description = "Filtered mode: horizontal zoom cutoff measured between chart bars; zero disables this cutoff.", GroupName = "08 Text - Historical Delta", Order = 3)]
		public int HistoricalDeltaTextMinBarSpacing { get; set; }
		[NinjaScriptProperty]
		[Display(Name = "Reveal Delta on Hover", Description = "Reveals the displayed historical delta bar value regardless of label thresholds. HoverOnly enables this automatically; Hidden disables it.", GroupName = "08 Text - Historical Delta", Order = 4)]
		public bool RevealHistoricalDeltaOnHover { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Minimum Absolute Delta to Show Text", Description = "Minimum absolute row delta for active labels and historical labels using FollowGlobal. Filtered historical labels use their separate threshold.", GroupName = "07 Text - Delta", Order = 1)]
		public int DeltaTextMinThreshold { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Delta Text Font Size", Description = "Font size used for ordinary active and historical delta labels. Hover text keeps its existing independent size.", GroupName = "07 Text - Delta", Order = 2)]
		public float DeltaTextFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Positive Text Color", GroupName = "07 Text - Delta", Order = 3)]
		public WpfBrush DeltaTextBrush { get; set; }
		[Browsable(false)]
		public string DeltaTextBrushSerialize { get { return Serialize.BrushToString(DeltaTextBrush); } set { DeltaTextBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Negative Text Color", GroupName = "07 Text - Delta", Order = 4)]
		public WpfBrush NegativeDeltaTextBrush { get; set; }
		[Browsable(false)]
		public string NegativeDeltaTextBrushSerialize { get { return Serialize.BrushToString(NegativeDeltaTextBrush); } set { NegativeDeltaTextBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Profile Statistics", Description = "Shows the selected statistics row for Step Profiles.", GroupName = "09 Profile Statistics", Order = 0)]
		public bool ShowProfileStatistics { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Active Statistics", Description = "Shows statistics for the active Step Profile when profile statistics are enabled.", GroupName = "09 Profile Statistics", Order = 1)]
		public bool ShowActiveStatistics { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Historical Statistics", Description = "Shows statistics for completed Step Profiles when profile statistics are enabled.", GroupName = "09 Profile Statistics", Order = 2)]
		public bool ShowHistoricalStatistics { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Total Delta", Description = "Includes cumulative signed trade volume for the individual Step Profile.", GroupName = "09 Profile Statistics", Order = 3)]
		public bool ShowTotalDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Finish Delta", Description = "Includes delta gained or surrendered after the strongest same-side cumulative-delta excursion.", GroupName = "09 Profile Statistics", Order = 4)]
		public bool ShowFinishDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta Percent", Description = "Includes Total Delta divided by Total Volume as a percentage.", GroupName = "09 Profile Statistics", Order = 5)]
		public bool ShowDeltaPercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Total Volume", Description = "Includes the Step Profile's total assigned trade volume.", GroupName = "09 Profile Statistics", Order = 6)]
		public bool ShowTotalVolume { get; set; }

		[NinjaScriptProperty]
		[Range(8, 30)]
		[Display(Name = "Statistics Font Size", Description = "Controls the Step Profile statistics text size.", GroupName = "09 Profile Statistics", Order = 7)]
		public int StatisticsFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Statistics Text Color", Description = "Controls the Step Profile statistics text color.", GroupName = "09 Profile Statistics", Order = 8)]
		public WpfBrush StatisticsTextBrush { get; set; }
		[Browsable(false)]
		public string StatisticsTextBrushSerialize { get { return Serialize.BrushToString(StatisticsTextBrush); } set { StatisticsTextBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Session Labels", Description = "Shows Asia, London, RTH, or Overnight labels along the bottom when Step Basis is Session.", GroupName = "03 Sessions", Order = 3)]
		public bool ShowSessionLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Visibility Hotkey", Description = "Lets the configured chart-focused hotkey show or hide this Step Profile without stopping its calculations.", GroupName = "10 Display Controls", Order = 2)]
		public bool EnableVisibilityHotkey { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(StepVisibilityHotkeyConverter))]
		[Display(Name = "Visibility Hotkey", Description = "Select or enter an exact chord such as Alt+S or Ctrl+Alt+S. Assign the same nonreserved chord to HTF Candles to toggle both together; NinjaTrader reserves Alt+A.", GroupName = "10 Display Controls", Order = 3)]
		public string VisibilityHotkey { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaStepProfile[] cacheOrcaStepProfile;
		public OrcaStepProfile OrcaStepProfile(StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, bool dynamicProfileWidth, int fillSpacePercent, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, StepMirrorArrangement mirrorArrangement, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return OrcaStepProfile(Input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, rTHOnly, rTHStart, rTHEnd, dynamicProfileWidth, fillSpacePercent, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, mirrorArrangement, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}

		public OrcaStepProfile OrcaStepProfile(ISeries<double> input, StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, bool dynamicProfileWidth, int fillSpacePercent, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, StepMirrorArrangement mirrorArrangement, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			if (cacheOrcaStepProfile != null)
				for (int idx = 0; idx < cacheOrcaStepProfile.Length; idx++)
					if (cacheOrcaStepProfile[idx] != null && cacheOrcaStepProfile[idx].StepInterval == stepInterval && cacheOrcaStepProfile[idx].VolumeTickCompression == volumeTickCompression && cacheOrcaStepProfile[idx].DeltaTickCompression == deltaTickCompression && cacheOrcaStepProfile[idx].UseDynamicAggregation == useDynamicAggregation && cacheOrcaStepProfile[idx].DynamicAggregationMultiplier == dynamicAggregationMultiplier && cacheOrcaStepProfile[idx].DeltaDynamicRowMinPixels == deltaDynamicRowMinPixels && cacheOrcaStepProfile[idx].DynamicDeltaMinCompression == dynamicDeltaMinCompression && cacheOrcaStepProfile[idx].DynamicDeltaMaxCompression == dynamicDeltaMaxCompression && cacheOrcaStepProfile[idx].RTHOnly == rTHOnly && cacheOrcaStepProfile[idx].RTHStart == rTHStart && cacheOrcaStepProfile[idx].RTHEnd == rTHEnd && cacheOrcaStepProfile[idx].DynamicProfileWidth == dynamicProfileWidth && cacheOrcaStepProfile[idx].FillSpacePercent == fillSpacePercent && cacheOrcaStepProfile[idx].HistoricalProfileWidthPx == historicalProfileWidthPx && cacheOrcaStepProfile[idx].ActiveProfileWidthPx == activeProfileWidthPx && cacheOrcaStepProfile[idx].ActiveDeltaWidthPx == activeDeltaWidthPx && cacheOrcaStepProfile[idx].HistoricalDeltaWidthPx == historicalDeltaWidthPx && cacheOrcaStepProfile[idx].RightOffsetPx == rightOffsetPx && cacheOrcaStepProfile[idx].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaStepProfile[idx].MirrorProfiles == mirrorProfiles && cacheOrcaStepProfile[idx].MirrorArrangement == mirrorArrangement && cacheOrcaStepProfile[idx].ShowActiveVolume == showActiveVolume && cacheOrcaStepProfile[idx].ShowHistoricalVolume == showHistoricalVolume && cacheOrcaStepProfile[idx].ShowActiveDelta == showActiveDelta && cacheOrcaStepProfile[idx].ShowHistoricalDelta == showHistoricalDelta && cacheOrcaStepProfile[idx].ShowPOC == showPOC && cacheOrcaStepProfile[idx].ShowBlockSeparators == showBlockSeparators && cacheOrcaStepProfile[idx].UseGradient == useGradient && cacheOrcaStepProfile[idx].GradientSteps == gradientSteps && cacheOrcaStepProfile[idx].MinBrightness == minBrightness && cacheOrcaStepProfile[idx].ShowValueArea == showValueArea && cacheOrcaStepProfile[idx].ShowVAColor == showVAColor && cacheOrcaStepProfile[idx].ShowVALines == showVALines && cacheOrcaStepProfile[idx].ValueAreaPercent == valueAreaPercent && cacheOrcaStepProfile[idx].VALineThickness == vALineThickness && cacheOrcaStepProfile[idx].VALineStyle == vALineStyle && cacheOrcaStepProfile[idx].ActiveVolumeOpacity == activeVolumeOpacity && cacheOrcaStepProfile[idx].HistoricalVolumeOpacity == historicalVolumeOpacity && cacheOrcaStepProfile[idx].ActiveDeltaOpacity == activeDeltaOpacity && cacheOrcaStepProfile[idx].HistoricalDeltaOpacity == historicalDeltaOpacity && cacheOrcaStepProfile[idx].UseDeltaIntensityColoring == useDeltaIntensityColoring && cacheOrcaStepProfile[idx].DeltaIntensityMinOpacity == deltaIntensityMinOpacity && cacheOrcaStepProfile[idx].ShowDeltaText == showDeltaText && cacheOrcaStepProfile[idx].DeltaTextMinThreshold == deltaTextMinThreshold && cacheOrcaStepProfile[idx].DeltaTextFontSize == deltaTextFontSize && cacheOrcaStepProfile[idx].EqualsInput(input))
						return cacheOrcaStepProfile[idx];
			return CacheIndicator<OrcaStepProfile>(new OrcaStepProfile(){ StepInterval = stepInterval, VolumeTickCompression = volumeTickCompression, DeltaTickCompression = deltaTickCompression, UseDynamicAggregation = useDynamicAggregation, DynamicAggregationMultiplier = dynamicAggregationMultiplier, DeltaDynamicRowMinPixels = deltaDynamicRowMinPixels, DynamicDeltaMinCompression = dynamicDeltaMinCompression, DynamicDeltaMaxCompression = dynamicDeltaMaxCompression, RTHOnly = rTHOnly, RTHStart = rTHStart, RTHEnd = rTHEnd, DynamicProfileWidth = dynamicProfileWidth, FillSpacePercent = fillSpacePercent, HistoricalProfileWidthPx = historicalProfileWidthPx, ActiveProfileWidthPx = activeProfileWidthPx, ActiveDeltaWidthPx = activeDeltaWidthPx, HistoricalDeltaWidthPx = historicalDeltaWidthPx, RightOffsetPx = rightOffsetPx, ProfileBarSpacingPx = profileBarSpacingPx, MirrorProfiles = mirrorProfiles, MirrorArrangement = mirrorArrangement, ShowActiveVolume = showActiveVolume, ShowHistoricalVolume = showHistoricalVolume, ShowActiveDelta = showActiveDelta, ShowHistoricalDelta = showHistoricalDelta, ShowPOC = showPOC, ShowBlockSeparators = showBlockSeparators, UseGradient = useGradient, GradientSteps = gradientSteps, MinBrightness = minBrightness, ShowValueArea = showValueArea, ShowVAColor = showVAColor, ShowVALines = showVALines, ValueAreaPercent = valueAreaPercent, VALineThickness = vALineThickness, VALineStyle = vALineStyle, ActiveVolumeOpacity = activeVolumeOpacity, HistoricalVolumeOpacity = historicalVolumeOpacity, ActiveDeltaOpacity = activeDeltaOpacity, HistoricalDeltaOpacity = historicalDeltaOpacity, UseDeltaIntensityColoring = useDeltaIntensityColoring, DeltaIntensityMinOpacity = deltaIntensityMinOpacity, ShowDeltaText = showDeltaText, DeltaTextMinThreshold = deltaTextMinThreshold, DeltaTextFontSize = deltaTextFontSize }, input, ref cacheOrcaStepProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaStepProfile OrcaStepProfile(StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, bool dynamicProfileWidth, int fillSpacePercent, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, StepMirrorArrangement mirrorArrangement, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return indicator.OrcaStepProfile(Input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, rTHOnly, rTHStart, rTHEnd, dynamicProfileWidth, fillSpacePercent, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, mirrorArrangement, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}

		public Indicators.OrcaStepProfile OrcaStepProfile(ISeries<double> input , StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, bool dynamicProfileWidth, int fillSpacePercent, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, StepMirrorArrangement mirrorArrangement, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return indicator.OrcaStepProfile(input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, rTHOnly, rTHStart, rTHEnd, dynamicProfileWidth, fillSpacePercent, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, mirrorArrangement, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaStepProfile OrcaStepProfile(StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, bool dynamicProfileWidth, int fillSpacePercent, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, StepMirrorArrangement mirrorArrangement, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return indicator.OrcaStepProfile(Input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, rTHOnly, rTHStart, rTHEnd, dynamicProfileWidth, fillSpacePercent, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, mirrorArrangement, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}

		public Indicators.OrcaStepProfile OrcaStepProfile(ISeries<double> input , StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, bool dynamicProfileWidth, int fillSpacePercent, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, StepMirrorArrangement mirrorArrangement, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return indicator.OrcaStepProfile(input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, rTHOnly, rTHStart, rTHEnd, dynamicProfileWidth, fillSpacePercent, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, mirrorArrangement, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}
	}
}

#endregion
