#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
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

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum VALineStyleEnum
	{
		Solid = 0,
		Dash = 1,
		Dot = 2,
		DashDot = 3
	}

	public enum CandleProfileSideArrangement
	{
		DeltaLeft_VolumeRight = 0,
		VolumeLeft_DeltaRight = 1
	}

	public enum CandleProfileTradeSourceMode
	{
		SecondaryTickSeries = 0,
		TickReplayLastEvents = 1
	}

	public enum CandleProfileDisplayMode
	{
		LegacyVisibility = -1,
		Off = 0,
		Volume = 1,
		Delta = 2,
		VolumeAndDelta = 3,
		BidAsk = 4
	}

	public enum CandleProfileBidAskStyle
	{
		Cluster = 0,
		Histogram = 1
	}

	public class CandleProfileDisplayModeConverter : EnumConverter
	{
		public CandleProfileDisplayModeConverter() : base(typeof(CandleProfileDisplayMode))
		{
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			return new StandardValuesCollection(new[]
			{
				CandleProfileDisplayMode.Off,
				CandleProfileDisplayMode.Volume,
				CandleProfileDisplayMode.Delta,
				CandleProfileDisplayMode.VolumeAndDelta,
				CandleProfileDisplayMode.BidAsk
			});
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is CandleProfileDisplayMode)
			{
				switch ((CandleProfileDisplayMode)value)
				{
					case CandleProfileDisplayMode.Off: return "Off";
					case CandleProfileDisplayMode.Delta: return "Delta";
					case CandleProfileDisplayMode.VolumeAndDelta: return "Volume + Delta";
					case CandleProfileDisplayMode.BidAsk: return "Bid x Ask";
					default: return "Volume";
				}
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (string.Equals(text, "Off", StringComparison.OrdinalIgnoreCase)) return CandleProfileDisplayMode.Off;
				if (string.Equals(text, "Volume", StringComparison.OrdinalIgnoreCase)) return CandleProfileDisplayMode.Volume;
				if (string.Equals(text, "Delta", StringComparison.OrdinalIgnoreCase)) return CandleProfileDisplayMode.Delta;
				if (string.Equals(text, "Volume + Delta", StringComparison.OrdinalIgnoreCase)) return CandleProfileDisplayMode.VolumeAndDelta;
				if (string.Equals(text, "Bid x Ask", StringComparison.OrdinalIgnoreCase)) return CandleProfileDisplayMode.BidAsk;
			}

			return base.ConvertFrom(context, culture, value);
		}
	}

	public enum CandleProfileTextFontWeight
	{
		Regular = 0,
		Medium = 1,
		SemiBold = 2,
		Bold = 3,
		ExtraBold = 4
	}

	public class CandleProfileTextFontFamilyConverter : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return false;
		}

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> fontNames = new List<string>();
			AddFontName(fontNames, seen, "Figtree");
			AddFontName(fontNames, seen, "Segoe UI");
			AddFontName(fontNames, seen, "Arial");
			AddFontName(fontNames, seen, "Consolas");
			AddFontName(fontNames, seen, "Tahoma");
			AddFontName(fontNames, seen, "Verdana");

			List<string> installedFontNames = new List<string>();
			foreach (System.Windows.Media.FontFamily family in System.Windows.Media.Fonts.SystemFontFamilies)
			{
				string name = family != null ? family.Source : null;
				if (!string.IsNullOrWhiteSpace(name))
					installedFontNames.Add(name);
			}

			installedFontNames.Sort(StringComparer.CurrentCultureIgnoreCase);
			foreach (string name in installedFontNames)
				AddFontName(fontNames, seen, name);

			return new StandardValuesCollection(fontNames);
		}

		private static void AddFontName(List<string> fontNames, HashSet<string> seen, string name)
		{
			if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
				return;

			fontNames.Add(name);
		}
	}

	[TypeConverter(typeof(OrcaFootprintSettingsConverter))]
	public partial class OrcaCandleVolumeProfile : Indicator, IOrcaReplayParticipant
	{
		#region Fields
		private readonly Guid sharedSourceId = Guid.NewGuid();
		private OrcaReplayBarHorizon replayBarHorizon;
		// Per primary-bar volume & delta maps
		private List<Dictionary<double, long>> barVolumeMaps;
		private List<Dictionary<double, long>> barDeltaVolumeMaps;
		private List<Dictionary<double, long>> barDeltaMaps;
		private List<Dictionary<double, long>> barAskVolumeMaps;
		private List<Dictionary<double, long>> barBidVolumeMaps;
		private List<Dictionary<double, long>> barUnclassifiedVolumeMaps;
		private List<Dictionary<double, long>> sharedVolumeMaps;
		private List<Dictionary<double, long>> sharedUpVolumeMaps;
		private List<Dictionary<double, long>> sharedDownVolumeMaps;
		private List<double[]> barVACache; // [0]=VAH, [1]=VAL, [2]=POC, [3]=MaxVol
		private readonly object barDataSync = new object();
		private DateTime lastRenderSkipUtc = DateTime.MinValue;
		private DateTime lastSharedRegistrationUtc = DateTime.MinValue;
		private DateTime sharedSourceLastUpdatedUtc = DateTime.MinValue;
		private int sharedDataRevision;
		private int sharedCoverageBarCount;
		private bool sharedRegistrationAnnounced;
		private int lastDynamicDeltaComp = -1;

		// Bid/Ask cache for delta classification
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double prevLast = double.NaN;
		private int lastDirection;

		// SharpDX rendering resources
		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private SolidColorBrush bullBodyBrushDx;
		private SolidColorBrush bearBodyBrushDx;
		private SolidColorBrush compressedBullBodyBrushDx;
		private SolidColorBrush compressedBearBodyBrushDx;
		private SolidColorBrush bullWickBrushDx;
		private SolidColorBrush bearWickBrushDx;
		private SolidColorBrush volBrushDx;
		private SolidColorBrush pocBrushDx;
		private SolidColorBrush posDeltaBrushDx;
		private SolidColorBrush negDeltaBrushDx;
		private SolidColorBrush[] volumeTextIntensityBrushes;
		private int lastBuiltVolumeTextIntensitySteps = -1;
		private float lastBuiltVolumeTextMinBrightness = -1f;
		private SolidColorBrush[] positiveDeltaIntensityBrushes;
		private SolidColorBrush[] negativeDeltaIntensityBrushes;
		private int lastBuiltDeltaIntensitySteps = -1;
		private float lastBuiltDeltaIntensityMinOpacity = -1f;
		private float lastBuiltDeltaIntensityMaxOpacity = -1f;
		private SolidColorBrush[] bidAskPositiveIntensityBrushes;
		private SolidColorBrush[] bidAskNegativeIntensityBrushes;
		private SolidColorBrush[] bidAskNeutralIntensityBrushes;
		private int lastBuiltBidAskIntensitySteps = -1;
		private float lastBuiltBidAskMinOpacity = -1f;
		private float lastBuiltBidAskMaxOpacity = -1f;

		// Volume gradient palette (dark → bright) — outside VA
		private SolidColorBrush[] volGradientBrushes;
		private int lastBuiltGradientSteps = -1;

		// Value Area gradient palette (dark → bright) — inside VA
		private SolidColorBrush vaVolBrushDx;
		private SolidColorBrush[] vaGradientBrushes;
		private int lastBuiltVAGradientSteps = -1;

		// VA line resources
		private SolidColorBrush vaLineBrushDx;
		private StrokeStyle vaLineStrokeDx;

		// Text resources
		private SolidColorBrush deltaTextBrushDx;
		private SolidColorBrush volumeTextBrushDx;
		private SolidColorBrush bidAskTextBrushDx;
		private TextFormat      textFormatDx;
		private Dictionary<int, TextFormat> textFormatsBySize = new Dictionary<int, TextFormat>();
		private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();
		private float lastBuiltDeltaTextFontSize = -1f;
		private string lastBuiltDeltaTextFormatSignature = string.Empty;
		private string lastProfileTextFormatSignature = string.Empty;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name        = "OrcaCandleVolumeProfile";
				Description = "Displays volume and delta at each candle's price levels, with Volume, Delta, combined, and Bid x Ask footprint views. Includes point of control, value area, adjustable row sizing, and customizable colors and text.";
				Calculate   = Calculate.OnPriceChange;
				IsOverlay   = true;

				// Data
				TickCompression = 4;
				UseDynamicVolumeAggregation = false;
				VolumeDynamicAggregationMultiplier = 1.0;
				MaxDynamicVolumeTicks = 8;
				DeltaTickCompression = 4;
				UseDynamicDeltaAggregation = false;
				DeltaDynamicRowMinPixels = 10;
				DeltaDynamicMultiplier = 1.0;
				DynamicDeltaMinCompression = 1;
				DynamicDeltaMaxCompression = 100;
				PublishSharedProfileCache = true;
				TradeSourceMode = CandleProfileTradeSourceMode.SecondaryTickSeries;
				ProfileDisplayMode = CandleProfileDisplayMode.LegacyVisibility;
				BidAskStyle = CandleProfileBidAskStyle.Cluster;
				EnhancedFootprint = false;
				FootprintSettingsVersion = 0;
				FootprintAnalysisTicks = 0;
				FootprintScale = FootprintScaleMode.PerBar;
				FootprintFixedVolume = 0;
				FootprintScaffold = FootprintScaffoldMode.OhlcSpine;
				FootprintNumbers = FootprintNumberFormat.Auto;
				FootprintValues = FootprintCellView.BidAsk;
				FootprintGutterPx = 8;
				FootprintShowHealth = false;
				FootprintEmphasizeWinner = true;
				FootprintWinnerRatio = 1.5;

				// Layout
				CandleWidthPx       = 14;
				ProfileWidthPx      = 80;
				DeltaProfileWidthPx = 40;
				BidAskWidthPx = 96;
				ProfileArrangement  = CandleProfileSideArrangement.DeltaLeft_VolumeRight;
				DynamicProfileWidth = true;
				ProfileWidthScale   = 1.0;
				DualProfileWidthScale = 0.45;
				AutoHideProfilesWhenCompressed = false;
				MinBarSpacingToShowProfilesPx = 18;
				CompressedCandleWidthPx = 12;
				UseAbsorptionColorsWhenCompressed = true;
				AbsorptionCandleMinWidthPx = 5;
				CandleProfileGapPx  = 2;
				ProfileBarSpacingPx = 0;
				WickWidthPx         = 2;

				// Visibility
				ShowPOC       = true;
				ShowVolumeProfile = true;
				ShowDelta     = false;
				ShowDeltaProfile = false;
				ShowDeltaText = true;
				DeltaTextMinThreshold = 1;
				DeltaTextFontSize = 8f;
				ShowVolumeText = false;
				ScaleVolumeTextBrightnessByVolume = false;
				VolumeTextMinBrightness = 0.35f;
				ColorVolumeTextByDelta = false;
				ScaleVolumeTextColorByDeltaPercent = true;
				VolumeTextDeltaMinAbsolute = 150;
				VolumeTextDeltaMinPercent = 15.0;
				BoldQualifiedVolumeText = true;
				VolumeTextMinThreshold = 1;
				VolumeTextFontSize = 8f;
				ShowBidAskText = true;
				BidAskTextMinThreshold = 0;
				BidAskTextFontSize = 8f;
				TextFontFamily = "Segoe UI";
				TextFontWeight = CandleProfileTextFontWeight.Bold;
				UseDynamicTextSizing = false;
				DynamicTextMaxFontSize = 18f;
				UseGradient   = true;
				GradientSteps = 16;

				// Value Area
				ShowValueArea    = true;
				ShowVAColor      = true;
				ShowVALines      = true;
				ValueAreaPercent = 70;
				VALineThickness  = 1.5f;
				VALineStyle      = VALineStyleEnum.Dash;

				// Colors — candles
				BullishBodyBrush = WpfBrushes.MediumSeaGreen;
				BearishBodyBrush = WpfBrushes.Crimson;
				CompressedBullishBodyBrush = WpfBrushes.DodgerBlue;
				CompressedBearishBodyBrush = WpfBrushes.Crimson;

				// Colors — profile
				VolumeBrush    = WpfBrushes.RoyalBlue;
				VolumeOpacity  = 0.85f;
				MinBrightness  = 0.20f;
				POCBrush       = WpfBrushes.DodgerBlue;

				// Colors — Value Area
				VABrush     = WpfBrushes.CornflowerBlue;
				VALineBrush = WpfBrushes.White;

				// Colors — delta
				PositiveDeltaBrush = WpfBrushes.Lime;
				NegativeDeltaBrush = WpfBrushes.Red;
				DeltaOpacity       = 0.85f;
				UseDeltaIntensityColoring = true;
				DeltaIntensityMinOpacity = 0.35f;
				BidAskPositiveBrush = WpfBrushes.DodgerBlue;
				BidAskNegativeBrush = WpfBrushes.Crimson;
				BidAskNeutralBrush = WpfBrushes.DimGray;
				BidAskMinOpacity = 0.20f;
				BidAskMaxOpacity = 0.90f;
				BidAskTextBrush = WpfBrushes.White;
				DeltaTextBrush     = WpfBrushes.White;
				VolumeTextBrush    = WpfBrushes.White;
			}
			else if (State == State.Configure)
			{
				ResolveLegacyProfileDisplayMode();
				if (IsEnhancedFootprintActive && FootprintAnalysisTicks < 1)
					FootprintAnalysisTicks = Math.Max(1, DeltaTickCompression);
				FootprintSettingsVersion = 2;
				if (TradeSourceMode == CandleProfileTradeSourceMode.SecondaryTickSeries)
					AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				barVolumeMaps = new List<Dictionary<double, long>>(4096);
				barDeltaVolumeMaps = new List<Dictionary<double, long>>(4096);
				barDeltaMaps  = new List<Dictionary<double, long>>(4096);
				sharedVolumeMaps = new List<Dictionary<double, long>>(4096);
				barAskVolumeMaps = new List<Dictionary<double, long>>(4096);
				barBidVolumeMaps = new List<Dictionary<double, long>>(4096);
				barUnclassifiedVolumeMaps = new List<Dictionary<double, long>>(4096);
				sharedUpVolumeMaps = new List<Dictionary<double, long>>(4096);
				sharedDownVolumeMaps = new List<Dictionary<double, long>>(4096);
				barVACache    = new List<double[]>(4096);
				textWidthCache.Clear();
				lastBid = double.NaN;
				lastAsk = double.NaN;
				prevLast = double.NaN;
				lastDirection = 0;
				sharedCoverageBarCount = 0;
				replayBarHorizon = new OrcaReplayBarHorizon("OrcaCandleVolumeProfile:" + sharedSourceId.ToString("N"));
				RegisterSharedProfileSource(true);
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				InitializeEnhancedFootprint();
			}
			else if (State == State.Historical)
			{
				if (ChartControl != null)
				{
					OrcaReplayCore.RegisterParticipant(ChartControl, this);
					SetZOrder(9000);
				}
				StartEnhancedFootprintObserver();
			}
			else if (State == State.Transition || State == State.Realtime)
			{
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				FlushEnhancedFootprint();
			}
			else if (State == State.Terminated)
			{
				if (ChartControl != null) OrcaReplayCore.UnregisterParticipant(ChartControl, this);
				if (replayBarHorizon != null) replayBarHorizon.Restore();
				OrcaProfileDataCache.UnregisterSource(sharedSourceId);
				StopEnhancedFootprint();
				DisposeDx();
			}
		}

		private void ResolveLegacyProfileDisplayMode()
		{
			if (ProfileDisplayMode == CandleProfileDisplayMode.LegacyVisibility)
			{
				if (ShowVolumeProfile && ShowDeltaProfile)
					ProfileDisplayMode = CandleProfileDisplayMode.VolumeAndDelta;
				else if (ShowDeltaProfile)
					ProfileDisplayMode = CandleProfileDisplayMode.Delta;
				else if (ShowVolumeProfile)
					ProfileDisplayMode = CandleProfileDisplayMode.Volume;
				else
					ProfileDisplayMode = CandleProfileDisplayMode.Off;
			}

			ShowVolumeProfile = ProfileDisplayMode == CandleProfileDisplayMode.Volume || ProfileDisplayMode == CandleProfileDisplayMode.VolumeAndDelta;
			ShowDeltaProfile = ProfileDisplayMode == CandleProfileDisplayMode.Delta || ProfileDisplayMode == CandleProfileDisplayMode.VolumeAndDelta;
		}

		private bool IsDeltaColoredVolumeTextActive
		{
			get
			{
				return ProfileDisplayMode == CandleProfileDisplayMode.Volume
					&& ColorVolumeTextByDelta;
			}
		}

		private bool IsVolumeTextBrightnessActive
		{
			get
			{
				return ProfileDisplayMode == CandleProfileDisplayMode.VolumeAndDelta
					&& ShowVolumeText
					&& ScaleVolumeTextBrightnessByVolume;
			}
		}

		private bool ShouldCollectStrictBidAskEvidence
		{
			get
			{
				return (ProfileDisplayMode == CandleProfileDisplayMode.BidAsk && !IsEnhancedFootprintActive)
					|| IsDeltaColoredVolumeTextActive;
			}
		}

		#region Dispose
		private void DisposeBrushPalette(ref SolidColorBrush[] brushes)
		{
			if (brushes == null)
				return;

			for (int i = 0; i < brushes.Length; i++)
				brushes[i]?.Dispose();

			brushes = null;
		}

		private void DisposeDx()
		{
			try
			{
				bullBodyBrushDx?.Dispose();
				bearBodyBrushDx?.Dispose();
				compressedBullBodyBrushDx?.Dispose();
				compressedBearBodyBrushDx?.Dispose();
				bullWickBrushDx?.Dispose();
				bearWickBrushDx?.Dispose();
				volBrushDx?.Dispose();
				pocBrushDx?.Dispose();
				posDeltaBrushDx?.Dispose();
				negDeltaBrushDx?.Dispose();
				DisposeBrushPalette(ref volumeTextIntensityBrushes);
				DisposeBrushPalette(ref positiveDeltaIntensityBrushes);
				DisposeBrushPalette(ref negativeDeltaIntensityBrushes);
				DisposeBrushPalette(ref bidAskPositiveIntensityBrushes);
				DisposeBrushPalette(ref bidAskNegativeIntensityBrushes);
				DisposeBrushPalette(ref bidAskNeutralIntensityBrushes);
				vaVolBrushDx?.Dispose();
				vaLineBrushDx?.Dispose();
				vaLineStrokeDx?.Dispose();
				deltaTextBrushDx?.Dispose();
				volumeTextBrushDx?.Dispose();
				bidAskTextBrushDx?.Dispose();
				textFormatDx?.Dispose();
				DisposeTextFormats();

				if (volGradientBrushes != null)
					for (int i = 0; i < volGradientBrushes.Length; i++)
						volGradientBrushes[i]?.Dispose();

				if (vaGradientBrushes != null)
					for (int i = 0; i < vaGradientBrushes.Length; i++)
						vaGradientBrushes[i]?.Dispose();
			}
			catch { }
			finally
			{
				bullBodyBrushDx    = null;
				bearBodyBrushDx    = null;
				compressedBullBodyBrushDx = null;
				compressedBearBodyBrushDx = null;
				bullWickBrushDx    = null;
				bearWickBrushDx    = null;
				volBrushDx         = null;
				pocBrushDx         = null;
				posDeltaBrushDx    = null;
				negDeltaBrushDx    = null;
				lastBuiltVolumeTextIntensitySteps = -1;
				lastBuiltVolumeTextMinBrightness = -1f;
				lastBuiltDeltaIntensitySteps = -1;
				lastBuiltDeltaIntensityMinOpacity = -1f;
				lastBuiltDeltaIntensityMaxOpacity = -1f;
				lastBuiltBidAskIntensitySteps = -1;
				lastBuiltBidAskMinOpacity = -1f;
				lastBuiltBidAskMaxOpacity = -1f;
				vaVolBrushDx       = null;
				vaLineBrushDx      = null;
				vaLineStrokeDx     = null;
				deltaTextBrushDx   = null;
				volumeTextBrushDx  = null;
				bidAskTextBrushDx = null;
				textFormatDx       = null;
				volGradientBrushes = null;
				vaGradientBrushes  = null;
				dxResourceRenderTarget = IntPtr.Zero;
				lastBuiltGradientSteps   = -1;
				lastBuiltVAGradientSteps = -1;
				lastBuiltDeltaTextFontSize = -1f;
				lastBuiltDeltaTextFormatSignature = string.Empty;
				lastProfileTextFormatSignature = string.Empty;
			}
		}

		private void DisposeTextFormats()
		{
			if (textFormatsBySize == null)
				return;

			foreach (TextFormat format in textFormatsBySize.Values)
			{
				if (format != null)
					format.Dispose();
			}

			textFormatsBySize.Clear();
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
			ResetEnhancedRenderTarget();
		}
		#endregion

		#region Market Data / Tick Processing
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e == null)
				return;
			if (IsEnhancedFootprintActive)
				ObserveEnhancedQuoteTime(e);

			if (e.MarketDataType == MarketDataType.Bid)
				lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask)
				lastAsk = e.Price;
			else if (e.MarketDataType == MarketDataType.Last)
			{
				if (e.Ask > 0 && !double.IsNaN(e.Ask))
					lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid))
					lastBid = e.Bid;

				if (TradeSourceMode == CandleProfileTradeSourceMode.TickReplayLastEvents)
				{
					long volume = NormalizeTradeVolume(e.Volume);
					DateTime tradeTime = e.Time == DateTime.MinValue ? GetCurrentPrimaryTime() : e.Time;
					ProcessTradeIntoPrimaryBar(tradeTime, e.Price, volume,
						e.Bid > 0 && e.Ask >= e.Bid && !double.IsInfinity(e.Ask)
							? FootprintDataQuality.TradeQuoteUnverified : FootprintDataQuality.CachedQuote);
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				ProcessTickIntoPrimaryBar();

				// Removed ForceRefresh() to fix UI Thread lagging

				return;
			}

			if (BarsInProgress == 0 && CurrentBar >= 0)
			{
				lock (barDataSync)
					EnsureBarMaps(CurrentBar);
				RefreshSharedProfileRegistrationIfNeeded();
			}
		}

		private void EnsureBarMaps(int primaryBarIndex)
		{
			while (barVolumeMaps.Count <= primaryBarIndex)
				barVolumeMaps.Add(new Dictionary<double, long>());

			while (barDeltaVolumeMaps.Count <= primaryBarIndex)
				barDeltaVolumeMaps.Add(new Dictionary<double, long>());

			while (barDeltaMaps.Count <= primaryBarIndex)
				barDeltaMaps.Add(new Dictionary<double, long>());

			if (ShouldCollectStrictBidAskEvidence)
			{
				while (barAskVolumeMaps.Count <= primaryBarIndex)
					barAskVolumeMaps.Add(new Dictionary<double, long>());
				while (barBidVolumeMaps.Count <= primaryBarIndex)
					barBidVolumeMaps.Add(new Dictionary<double, long>());
				while (barUnclassifiedVolumeMaps.Count <= primaryBarIndex)
					barUnclassifiedVolumeMaps.Add(new Dictionary<double, long>());
			}

			while (sharedVolumeMaps.Count <= primaryBarIndex)
				sharedVolumeMaps.Add(new Dictionary<double, long>());

			while (sharedUpVolumeMaps.Count <= primaryBarIndex)
				sharedUpVolumeMaps.Add(new Dictionary<double, long>());

			while (sharedDownVolumeMaps.Count <= primaryBarIndex)
				sharedDownVolumeMaps.Add(new Dictionary<double, long>());

			while (barVACache.Count <= primaryBarIndex)
				barVACache.Add(new double[] { double.NaN, double.NaN, double.NaN, 0, 0 });
		}

		private void RefreshSharedProfileRegistrationIfNeeded()
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastSharedRegistrationUtc).TotalSeconds < 5)
				return;

			RegisterSharedProfileSource(false);
		}

		private void RegisterSharedProfileSource(bool announce)
		{
			lastSharedRegistrationUtc = DateTime.UtcNow;
			if (!PublishSharedProfileCache)
			{
				OrcaProfileDataCache.UnregisterSource(sharedSourceId);
				return;
			}

			string key = OrcaProfileDataCache.BuildKey(Bars);
			RegisterSharedProfileSourceForKey(key);

			string chartKey = OrcaProfileDataCache.BuildKey(Bars, ChartControl);
			if (!string.IsNullOrEmpty(chartKey) && chartKey != key)
				RegisterSharedProfileSourceForKey(chartKey);

			if (announce && !sharedRegistrationAnnounced)
				sharedRegistrationAnnounced = true;
		}

		private void RegisterSharedProfileSourceForKey(string key)
		{
			if (string.IsNullOrEmpty(key))
				return;

			OrcaProfileDataCache.RegisterSource(new OrcaProfileDataSource
			{
				SourceId = sharedSourceId,
				Key = key,
				SourceName = "OrcaCandleVolumeProfile",
				SyncRoot = barDataSync,
				VolumeByBar = sharedVolumeMaps,
				UpVolumeByBar = sharedUpVolumeMaps,
				DownVolumeByBar = sharedDownVolumeMaps,
				RevisionProvider = () => sharedDataRevision,
				LastUpdatedUtcProvider = () => sharedSourceLastUpdatedUtc,
				CoverageProvider = () => sharedCoverageBarCount
			});
		}

		private void ProcessTickIntoPrimaryBar()
		{
			if (TradeSourceMode != CandleProfileTradeSourceMode.SecondaryTickSeries)
				return;

			if (BarsArray == null || BarsArray.Length < 2 || CurrentBars == null || CurrentBars.Length < 2 || CurrentBars[1] < 0)
				return;

			DateTime tickTime = Times[1][0];
			double last = Closes[1][0];
			long   vol  = NormalizeTradeVolume((long)Volumes[1][0]);

			ProcessTradeIntoPrimaryBar(tickTime, last, vol);
		}

		private DateTime GetCurrentPrimaryTime()
		{
			try
			{
				if (Times != null && Times.Length > 0 && CurrentBar >= 0)
					return Times[0][0];
			}
			catch { }

			return DateTime.MinValue;
		}

		private long NormalizeTradeVolume(long volume)
		{
			if (volume <= 0)
				return 0;

			try
			{
				if (Instrument != null && Instrument.MasterInstrument != null && Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
					return (long)Core.Globals.ToCryptocurrencyVolume(volume);
			}
			catch { }

			return volume;
		}

		private void ProcessTradeIntoPrimaryBar(DateTime tickTime, double last, long vol,
			FootprintDataQuality quoteQuality = FootprintDataQuality.SecondarySeries | FootprintDataQuality.CachedQuote)
		{
			if (BarsArray == null || BarsArray.Length < 1 || BarsArray[0] == null || tickTime == DateTime.MinValue)
				return;

			if (vol <= 0 || double.IsNaN(last) || double.IsInfinity(last))
				return;

			int primaryIndex = ResolvePrimaryBarIndex(tickTime, last);
			if (primaryIndex < 0) return;

			double volumeComp        = Math.Max(1, TickCompression) * TickSize;
			double volumeBucketPrice = Math.Floor(last / volumeComp + 0.000001) * volumeComp;
			double deltaComp         = TickSize;
			double deltaBucketPrice  = Math.Floor(last / deltaComp + 0.000001) * deltaComp;

			lock (barDataSync)
			{
				EnsureBarMaps(primaryIndex);

				// --- VOLUME ---
				var vmap = barVolumeMaps[primaryIndex];
				if (vmap.TryGetValue(volumeBucketPrice, out long vExisting))
					vmap[volumeBucketPrice] = vExisting + vol;
				else
					vmap[volumeBucketPrice] = vol;

				var deltaVolMap = barDeltaVolumeMaps[primaryIndex];
				if (deltaVolMap.TryGetValue(deltaBucketPrice, out long deltaVolExisting))
					deltaVolMap[deltaBucketPrice] = deltaVolExisting + vol;
				else
					deltaVolMap[deltaBucketPrice] = vol;

				if (PublishSharedProfileCache)
				{
					var sharedVolMap = sharedVolumeMaps[primaryIndex];
					bool wasEmptySharedBar = sharedVolMap.Count == 0;
					if (sharedVolMap.TryGetValue(deltaBucketPrice, out long sharedExisting))
						sharedVolMap[deltaBucketPrice] = sharedExisting + vol;
					else
						sharedVolMap[deltaBucketPrice] = vol;
					if (wasEmptySharedBar)
						sharedCoverageBarCount++;
				}

				// --- DELTA + STRICT BID/ASK ---
				bool usedBidAsk;
				long signed = ClassifySignedVolume(last, vol, out usedBidAsk);
				if (IsEnhancedFootprintActive)
					ObserveEnhancedTrade(primaryIndex, tickTime, last, vol, usedBidAsk ? Math.Sign(signed) : 0, quoteQuality);

				if (ShouldCollectStrictBidAskEvidence)
				{
					if (usedBidAsk)
					{
						Dictionary<double, long> strictMap = signed > 0 ? barAskVolumeMaps[primaryIndex] : barBidVolumeMaps[primaryIndex];
						AddVolumeToMap(strictMap, deltaBucketPrice, vol);
					}
					else
						AddVolumeToMap(barUnclassifiedVolumeMaps[primaryIndex], deltaBucketPrice, vol);
				}

				if (signed != 0)
				{
					AddVolumeToMap(barDeltaMaps[primaryIndex], deltaBucketPrice, signed);

					if (PublishSharedProfileCache)
					{
						Dictionary<double, long> directionalMap = signed > 0 ? sharedUpVolumeMaps[primaryIndex] : sharedDownVolumeMaps[primaryIndex];
						AddVolumeToMap(directionalMap, deltaBucketPrice, vol);
					}
				}

				if (PublishSharedProfileCache)
				{
					sharedDataRevision++;
					sharedSourceLastUpdatedUtc = DateTime.UtcNow;
				}
			}
		}

		private long ClassifySignedVolume(double price, long volume, out bool usedBidAsk)
		{
			usedBidAsk = false;
			if (volume <= 0)
				return 0;

			long signed = 0;
			bool haveUsableQuotes = !double.IsNaN(lastAsk) && !double.IsNaN(lastBid)
				&& lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid;
			if (haveUsableQuotes)
			{
				if (price >= lastAsk)
				{
					signed = +volume;
					usedBidAsk = true;
				}
				else if (price <= lastBid)
				{
					signed = -volume;
					usedBidAsk = true;
				}
			}

			if (!usedBidAsk && !double.IsNaN(prevLast))
			{
				if (price > prevLast) signed = +volume;
				else if (price < prevLast) signed = -volume;
				else signed = lastDirection * volume;
			}

			prevLast = price;
			if (signed > 0)
				lastDirection = 1;
			else if (signed < 0)
				lastDirection = -1;

			return signed;
		}

		private static void AddVolumeToMap(Dictionary<double, long> map, double price, long volume)
		{
			long existing;
			if (map.TryGetValue(price, out existing))
				map[price] = existing + volume;
			else
				map[price] = volume;
		}

		private int ResolvePrimaryBarIndex(DateTime tickTime, double price)
		{
			if (BarsArray == null || BarsArray.Length < 1 || BarsArray[0] == null || tickTime == DateTime.MinValue)
				return -1;

			int primaryIndex = BarsArray[0].GetBar(tickTime);
			if (primaryIndex < 0)
				return -1;

			if (IsPriceInsidePrimaryBar(primaryIndex, price))
				return primaryIndex;

			int count = BarsArray[0].Count;
			int searchRadius = 64;
			int first = Math.Max(0, primaryIndex - searchRadius);
			int last = Math.Min(count - 1, primaryIndex + searchRadius);
			int bestIndex = -1;
			long bestScore = long.MaxValue;

			for (int index = first; index <= last; index++)
			{
				if (!IsPriceInsidePrimaryBar(index, price))
					continue;

				long timeDistance = GetTimeDistanceTicks(index, tickTime);
				if (timeDistance > TimeSpan.TicksPerSecond * 2L)
					continue;

				long score = timeDistance + Math.Abs(index - primaryIndex);
				if (score < bestScore)
				{
					bestScore = score;
					bestIndex = index;
				}
			}

			return bestIndex >= 0 ? bestIndex : primaryIndex;
		}

		private bool IsPriceInsidePrimaryBar(int barIndex, double price)
		{
			try
			{
				if (BarsArray == null || BarsArray.Length < 1 || BarsArray[0] == null || barIndex < 0 || barIndex >= BarsArray[0].Count)
					return false;

				double high = BarsArray[0].GetHigh(barIndex);
				double low = BarsArray[0].GetLow(barIndex);
				double tolerance = Math.Max(TickSize * 0.01, 0.0000001);
				return price >= low - tolerance && price <= high + tolerance;
			}
			catch { return false; }
		}

		private long GetTimeDistanceTicks(int barIndex, DateTime tickTime)
		{
			try
			{
				DateTime barTime = BarsArray[0].GetTime(barIndex);
				long diff = barTime.Ticks - tickTime.Ticks;
				return diff < 0 ? -diff : diff;
			}
			catch { return long.MaxValue; }
		}
		#endregion

		#region Value Area Calculation
		/// <summary>
		/// Calculates Value Area boundaries for a given volume map.
		/// Returns true if valid, with vahPrice and valPrice set.
		/// VA = price range covering ValueAreaPercent% of total volume, expanding outward from POC.
		/// </summary>
		private bool CalcValueArea(Dictionary<double, long> volMap, double pocPrice, out double vahPrice, out double valPrice)
		{
			vahPrice = pocPrice;
			valPrice = pocPrice;

			if (volMap.Count <= 1) return false;

			// Sort all price levels
			var sortedPrices = new List<double>(volMap.Keys);
			sortedPrices.Sort();

			long totalVol = 0;
			foreach (var kv in volMap) totalVol += kv.Value;
			if (totalVol <= 0) return false;

			double targetVol = totalVol * (ValueAreaPercent / 100.0);

			// Find POC index in sorted list
			int pocIdx = sortedPrices.IndexOf(pocPrice);
			if (pocIdx < 0) return false;

			long accumulatedVol = volMap[pocPrice];
			int lo = pocIdx;
			int hi = pocIdx;

			// Expand outward from POC: pick the side with more volume at the next level
			while (accumulatedVol < targetVol && (lo > 0 || hi < sortedPrices.Count - 1))
			{
				long volBelow = (lo > 0) ? volMap[sortedPrices[lo - 1]] : 0;
				long volAbove = (hi < sortedPrices.Count - 1) ? volMap[sortedPrices[hi + 1]] : 0;

				if (lo <= 0)
				{
					hi++;
					accumulatedVol += volAbove;
				}
				else if (hi >= sortedPrices.Count - 1)
				{
					lo--;
					accumulatedVol += volBelow;
				}
				else if (volAbove >= volBelow)
				{
					hi++;
					accumulatedVol += volAbove;
				}
				else
				{
					lo--;
					accumulatedVol += volBelow;
				}
			}

			valPrice = sortedPrices[lo];
			vahPrice = sortedPrices[hi];
			return true;
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				base.OnRender(chartControl, chartScale);
				if (IsEnhancedFootprintActive)
				{
					RenderEnhancedFootprint(chartControl, chartScale);
					return;
				}
				RefreshSharedProfileRegistrationIfNeeded();

				if (barVolumeMaps == null || ChartBars == null || BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null) return;

				EnsureDxResources();

				int maxBarIdx = Math.Min(BarsArray[0].Count - 1, ChartBars.Count - 1);
				if (maxBarIdx < 0) return;
				int fromIdx = Math.Max(0, ChartBars.FromIndex);
				int toIdx   = Math.Min(ChartBars.ToIndex, maxBarIdx);
				if (fromIdx > toIdx) return;

				float panelTop    = ChartPanel.Y;
				float panelBottom = ChartPanel.Y + ChartPanel.H;
				int volumeCompressionTicks = ResolveVolumeCompressionTicks(chartScale);
				int deltaCompressionTicks = ResolveDeltaCompressionTicks(chartScale);
				float averageBarSpacing = GetAverageVisibleBarSpacing(chartControl, fromIdx, toIdx);
				bool profilesVisible = !AutoHideProfilesWhenCompressed || averageBarSpacing <= 0 || averageBarSpacing >= MinBarSpacingToShowProfilesPx;
				float chartCandleWidth = ResolveChartCandleWidth(chartControl, averageBarSpacing);
				OrcaAbsorptionCandles absorptionSource = UseAbsorptionColorsWhenCompressed
					? FindAbsorptionColorSource(chartControl)
					: null;
				bool useAbsorptionCandleRender = absorptionSource != null;
				float activeCandleWidth = profilesVisible
					? (useAbsorptionCandleRender ? Math.Max(AbsorptionCandleMinWidthPx, CandleWidthPx) : CandleWidthPx)
					: Math.Max(useAbsorptionCandleRender ? AbsorptionCandleMinWidthPx : 1f, Math.Min(CompressedCandleWidthPx, chartCandleWidth));
				float activeWickWidth = profilesVisible
					? Math.Max(1f, WickWidthPx)
					: Math.Min(WickWidthPx, Math.Max(1f, activeCandleWidth));

				for (int barIdx = fromIdx; barIdx <= toIdx; barIdx++)
				{
					if (barIdx < 0 || barIdx >= BarsArray[0].Count) continue;

					float barCenterX = chartControl.GetXByBarIndex(ChartBars, barIdx);

					// --- OHLC ---
					double o = BarsArray[0].GetOpen(barIdx);
					double h = BarsArray[0].GetHigh(barIdx);
					double l = BarsArray[0].GetLow(barIdx);
					double c = BarsArray[0].GetClose(barIdx);

					float yOpen  = chartScale.GetYByValue(o);
					float yHigh  = chartScale.GetYByValue(h);
					float yLow   = chartScale.GetYByValue(l);
					float yClose = chartScale.GetYByValue(c);

					bool isBullish = c >= o;

					float bodyTop    = Math.Min(yOpen, yClose);
					float bodyBottom = Math.Max(yOpen, yClose);
					float bodyHeight = Math.Max(1f, bodyBottom - bodyTop);

					float halfCandle = activeCandleWidth / 2f;
					float candleLeft  = barCenterX - halfCandle;
					float candleRight = barCenterX + halfCandle;

					WpfBrush absorptionBodyBrush = absorptionSource != null ? absorptionSource.GetBodyBrushForBar(barIdx) : null;
					var bodyBrush = profilesVisible
						? (isBullish ? bullBodyBrushDx : bearBodyBrushDx)
						: (isBullish ? compressedBullBodyBrushDx : compressedBearBodyBrushDx);
					var wickBrush = isBullish ? bullWickBrushDx : bearWickBrushDx;
					SolidColorBrush absorptionDxBrush = null;
					if (absorptionBodyBrush != null)
						absorptionDxBrush = new SolidColorBrush(RenderTarget, ToDxColor(absorptionBodyBrush, 1f));
					var activeBodyBrush = absorptionDxBrush ?? bodyBrush;
					var activeWickBrush = absorptionDxBrush ?? wickBrush;
					bool hasRenderableProfile = profilesVisible && HasRenderableProfile(barIdx);
					bool hollowBodyDelta = hasRenderableProfile
						&& ProfileDisplayMode == CandleProfileDisplayMode.BidAsk
						&& FootprintScaffold == FootprintScaffoldMode.HollowBodyDelta;

					// --- Draw Wick ---
					if (!hollowBodyDelta)
					{
						float wickX    = barCenterX;
						float halfWick = activeWickWidth / 2f;

						if (yHigh < bodyTop)
						{
							RenderTarget.FillRectangle(
								new RectangleF(wickX - halfWick, yHigh, activeWickWidth, bodyTop - yHigh),
								activeWickBrush);
						}
						if (yLow > bodyBottom)
						{
							RenderTarget.FillRectangle(
								new RectangleF(wickX - halfWick, bodyBottom, activeWickWidth, yLow - bodyBottom),
								activeWickBrush);
						}
					}

					// Keep an absorption brush alive through the optional foreground redraw.
					try
					{
						// --- Draw Body ---
						if (!hollowBodyDelta)
							RenderTarget.FillRectangle(
								new RectangleF(candleLeft, bodyTop, activeCandleWidth, bodyHeight),
								activeBodyBrush);

						// --- Draw Profiles ---
						if (hasRenderableProfile)
						{
						bool showBidAsk = ProfileDisplayMode == CandleProfileDisplayMode.BidAsk;
						bool showVolumeProfile = ProfileDisplayMode == CandleProfileDisplayMode.Volume || ProfileDisplayMode == CandleProfileDisplayMode.VolumeAndDelta;
						bool showDeltaProfile = ProfileDisplayMode == CandleProfileDisplayMode.Delta || ProfileDisplayMode == CandleProfileDisplayMode.VolumeAndDelta;

							if (showBidAsk)
							{
								float bidAskWidth = ResolveCenteredProfileWidth(chartControl, barIdx, barCenterX, averageBarSpacing);
								float bodyDeltaWidth = hollowBodyDelta ? ResolveBodyDeltaColumnWidth(activeCandleWidth) : 0f;
								float centerGap = FootprintScaffold == FootprintScaffoldMode.OhlcSpineAndBody
									? activeCandleWidth + 2f * Math.Max(0, CandleProfileGapPx)
									: hollowBodyDelta ? bodyDeltaWidth + 2f * Math.Max(0, CandleProfileGapPx) : 0f;
								DrawBarBidAskProfile(chartScale, barIdx, barCenterX, panelTop, panelBottom, bidAskWidth,
									deltaCompressionTicks, centerGap, bodyDeltaWidth, o, c);
								if (FootprintScaffold == FootprintScaffoldMode.OhlcSpine)
									DrawCandleSpine(barCenterX, yHigh, yLow, bodyTop, bodyHeight, activeBodyBrush, activeWickBrush);
								else if (FootprintScaffold == FootprintScaffoldMode.OhlcSpineAndBody)
									DrawForegroundCandle(barCenterX, yHigh, yLow, bodyTop, bodyHeight, activeCandleWidth, activeWickWidth, activeBodyBrush, activeWickBrush);
							}
						else if (showVolumeProfile || showDeltaProfile)
						{
							float widthScale = (float)Math.Max(0.1, Math.Min(1.0, ProfileWidthScale));
							float dualWidthScale = showVolumeProfile && showDeltaProfile ? (float)Math.Max(0.1, Math.Min(1.0, DualProfileWidthScale)) : 1f;
							bool volumeOnRight = ProfileArrangement == CandleProfileSideArrangement.DeltaLeft_VolumeRight;
							bool deltaOnRight = !showVolumeProfile ? true : !volumeOnRight;
							float availableRightWidth = ResolveAvailableProfileWidth(chartControl, barIdx, barCenterX, halfCandle, activeCandleWidth, true);
							float availableLeftWidth = ResolveAvailableProfileWidth(chartControl, barIdx, barCenterX, halfCandle, activeCandleWidth, false);
							float volumeAvailableWidth = (volumeOnRight ? availableRightWidth : availableLeftWidth) * dualWidthScale;
							float deltaAvailableWidth = (deltaOnRight ? availableRightWidth : availableLeftWidth) * dualWidthScale;

							float drawVolumeWidth = ResolveSideProfileWidth(volumeAvailableWidth, ProfileWidthPx, widthScale, false);
							float drawDeltaWidth = ResolveSideProfileWidth(deltaAvailableWidth, DeltaProfileWidthPx, widthScale, true);

							if (showVolumeProfile)
							{
								float volumeRootX = volumeOnRight ? candleRight + CandleProfileGapPx : candleLeft - CandleProfileGapPx;
								DrawBarVolumeProfile(chartScale, barIdx, volumeRootX, panelTop, panelBottom, drawVolumeWidth, volumeCompressionTicks, volumeOnRight);
							}

							if (showDeltaProfile)
							{
								float deltaRootX = deltaOnRight ? candleRight + CandleProfileGapPx : candleLeft - CandleProfileGapPx;
								TextAlignment deltaTextAlignment = showVolumeProfile ? TextAlignment.Trailing : TextAlignment.Leading;
								DrawBarDeltaProfile(chartScale, barIdx, deltaRootX, panelTop, panelBottom, drawDeltaWidth, deltaCompressionTicks, deltaOnRight, deltaTextAlignment, !showVolumeProfile);
							}
							}
						}
					}
					finally
					{
						absorptionDxBrush?.Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				PrintRenderSkip(ex);
			}
		}

		private bool HasRenderableProfile(int barIdx)
		{
			lock (barDataSync)
				return barVolumeMaps != null && barIdx >= 0 && barIdx < barVolumeMaps.Count && barVolumeMaps[barIdx] != null && barVolumeMaps[barIdx].Count > 0 && barIdx < barVACache.Count;
		}


		private void DrawCandleSpine(float barCenterX, float yHigh, float yLow, float bodyTop, float bodyHeight, SolidColorBrush bodyBrush, SolidColorBrush wickBrush)
		{
			if (bodyBrush == null || wickBrush == null)
				return;

			float spineWidth = Math.Max(1f, Math.Min(2f, WickWidthPx));
			RenderTarget.FillRectangle(new RectangleF(barCenterX - spineWidth / 2f, yHigh, spineWidth, Math.Max(1f, yLow - yHigh)), wickBrush);
			RenderTarget.FillRectangle(new RectangleF(barCenterX - spineWidth / 2f, bodyTop, spineWidth, Math.Max(1f, bodyHeight)), bodyBrush);
		}

		private void DrawForegroundCandle(float barCenterX, float yHigh, float yLow, float bodyTop, float bodyHeight,
			float candleWidth, float wickWidth, SolidColorBrush bodyBrush, SolidColorBrush wickBrush)
		{
			if (bodyBrush == null || wickBrush == null)
				return;

			float resolvedCandleWidth = Math.Max(1f, candleWidth);
			float resolvedWickWidth = Math.Max(1f, Math.Min(wickWidth, resolvedCandleWidth));
			RenderTarget.FillRectangle(new RectangleF(barCenterX - resolvedWickWidth / 2f, yHigh,
				resolvedWickWidth, Math.Max(1f, yLow - yHigh)), wickBrush);
			RenderTarget.FillRectangle(new RectangleF(barCenterX - resolvedCandleWidth / 2f, bodyTop,
				resolvedCandleWidth, Math.Max(1f, bodyHeight)), bodyBrush);
		}

		private float ResolveBodyDeltaColumnWidth(float configuredCandleWidth)
		{
			// Five ungrouped glyphs cover a signed four-digit delta without reserving a broad candle lane.
			float readableMinimum = Math.Max(24f, BidAskTextFontSize * 3f + 4f);
			return Math.Max(2f, Math.Max(configuredCandleWidth, readableMinimum));
		}

		private float ResolveCenteredProfileWidth(ChartControl chartControl, int barIdx, float barCenterX, float averageBarSpacing)
		{
			float availableWidth = averageBarSpacing > 0f ? averageBarSpacing - 2f : BidAskWidthPx;

			try
			{
				if (chartControl != null && ChartBars != null)
				{
					float nearest = float.MaxValue;
					if (barIdx > 0)
						nearest = Math.Min(nearest, Math.Abs(barCenterX - chartControl.GetXByBarIndex(ChartBars, barIdx - 1)));
					if (barIdx + 1 < ChartBars.Count)
						nearest = Math.Min(nearest, Math.Abs(chartControl.GetXByBarIndex(ChartBars, barIdx + 1) - barCenterX));

					if (nearest < float.MaxValue && nearest > 0f)
						availableWidth = nearest - 2f;
				}
			}
			catch { }

			if (float.IsNaN(availableWidth) || float.IsInfinity(availableWidth) || availableWidth <= 0f)
				availableWidth = BidAskWidthPx;

			return Math.Max(2f, Math.Min(BidAskWidthPx, availableWidth));
		}
		private float GetAverageVisibleBarSpacing(ChartControl chartControl, int fromIdx, int toIdx)
		{
			try
			{
				if (chartControl == null || ChartBars == null || toIdx <= fromIdx)
					return 0f;

				float firstX = chartControl.GetXByBarIndex(ChartBars, fromIdx);
				float lastX = chartControl.GetXByBarIndex(ChartBars, toIdx);
				float spacing = Math.Abs(lastX - firstX) / Math.Max(1, toIdx - fromIdx);
				return float.IsNaN(spacing) || float.IsInfinity(spacing) ? 0f : spacing;
			}
			catch { }

			return 0f;
		}

		private float ResolveChartCandleWidth(ChartControl chartControl, float averageBarSpacing)
		{
			float width = 0f;
			try
			{
				if (chartControl != null)
					width = (float)chartControl.BarWidth;
			}
			catch { width = 0f; }

			if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f)
				width = averageBarSpacing > 0f ? averageBarSpacing * 0.7f : CompressedCandleWidthPx;

			if (averageBarSpacing > 0f)
				width = Math.Min(width, Math.Max(1f, averageBarSpacing - 1f));

			return Math.Max(1f, width);
		}

		private float ResolveAvailableProfileWidth(ChartControl chartControl, int barIdx, float barCenterX, float halfCandle, float activeCandleWidth, bool rightSide)
		{
			try
			{
				if (chartControl == null || ChartBars == null)
					return ProfileWidthPx;

				if (rightSide)
				{
					float nextBarCenterX;
					if (barIdx + 1 < ChartBars.Count)
						nextBarCenterX = chartControl.GetXByBarIndex(ChartBars, barIdx + 1);
					else if (barIdx > 0)
						nextBarCenterX = barCenterX + (barCenterX - chartControl.GetXByBarIndex(ChartBars, barIdx - 1));
					else
						nextBarCenterX = barCenterX + ProfileWidthPx;

					float nextCandleLeft = nextBarCenterX - halfCandle;
					float currentCandleRight = barCenterX + activeCandleWidth / 2f;
					return Math.Max(2f, nextCandleLeft - (currentCandleRight + CandleProfileGapPx) - 1f);
				}
				else
				{
					float prevBarCenterX;
					if (barIdx > 0)
						prevBarCenterX = chartControl.GetXByBarIndex(ChartBars, barIdx - 1);
					else if (barIdx + 1 < ChartBars.Count)
						prevBarCenterX = barCenterX - (chartControl.GetXByBarIndex(ChartBars, barIdx + 1) - barCenterX);
					else
						prevBarCenterX = barCenterX - ProfileWidthPx;

					float prevCandleRight = prevBarCenterX + halfCandle;
					float currentCandleLeft = barCenterX - activeCandleWidth / 2f;
					return Math.Max(2f, (currentCandleLeft - CandleProfileGapPx) - prevCandleRight - 1f);
				}
			}
			catch { }

			return ProfileWidthPx;
		}

		private float ResolveSideProfileWidth(float availableWidth, int fixedWidthPx, float widthScale, bool capDynamicWidth)
		{
			if (DynamicProfileWidth)
			{
				float dynamicWidth = availableWidth * widthScale;
				if (capDynamicWidth)
					dynamicWidth = Math.Min(fixedWidthPx * widthScale, dynamicWidth);
				return Math.Max(2f, dynamicWidth);
			}

			return Math.Max(2f, Math.Min(fixedWidthPx * widthScale, availableWidth));
		}

		private OrcaAbsorptionCandles FindAbsorptionColorSource(ChartControl chartControl)
		{
			try
			{
				if (chartControl == null || chartControl.Indicators == null)
					return null;

				foreach (object indicator in chartControl.Indicators)
				{
					OrcaAbsorptionCandles absorption = indicator as OrcaAbsorptionCandles;
					if (absorption != null)
						return absorption;
				}
			}
			catch { }

			return null;
		}

		private void PrintRenderSkip(Exception ex)
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastRenderSkipUtc).TotalSeconds < 30) return;
			lastRenderSkipUtc = now;
			Print("OrcaCandleVolumeProfile: skipped one render frame: " + ex.Message);
		}

		private void DrawBarVolumeProfile(ChartScale chartScale, int barIdx, float profileRootX, float panelTop, float panelBottom, float drawProfileWidth, int volumeCompressionTicks, bool flowsRight)
		{
			Dictionary<double, long> volumeSource;
			Dictionary<double, long> deltaSource = null;
			bool emphasizeVolumeRowsByDelta = IsDeltaColoredVolumeTextActive;
			double[] cache;
			lock (barDataSync)
			{
				if (barIdx < 0 || barVolumeMaps == null || barVACache == null || barIdx >= barVolumeMaps.Count || barIdx >= barVACache.Count || barVolumeMaps[barIdx] == null || barVolumeMaps[barIdx].Count == 0)
					return;

				volumeSource = new Dictionary<double, long>(barVolumeMaps[barIdx]);
				if ((ShowDelta || emphasizeVolumeRowsByDelta) && barDeltaMaps != null && barIdx < barDeltaMaps.Count && barDeltaMaps[barIdx] != null && barDeltaMaps[barIdx].Count > 0)
					deltaSource = new Dictionary<double, long>(barDeltaMaps[barIdx]);

				cache = barVACache[barIdx];
				if (cache == null || cache.Length < 5)
				{
					cache = new double[] { double.NaN, double.NaN, double.NaN, 0, 0 };
					barVACache[barIdx] = cache;
				}
			}

			var volMap = BuildAggregatedMap(volumeSource, volumeCompressionTicks, Math.Max(1, TickCompression));
			if (volMap.Count == 0) return;

			long maxVol = 0;
			double pocPrice = double.NaN;
			double vahPrice = double.NaN, valPrice = double.NaN;
			bool haveVA = false;

			bool isActive = barIdx == BarsArray[0].Count - 1;
			bool needsCalc = double.IsNaN(cache[0]) || isActive || Math.Abs(cache[4] - volumeCompressionTicks) > 0.5;

			if (needsCalc)
			{
				foreach (var kvp in volMap)
				{
					if (kvp.Value > maxVol)
					{
						maxVol   = kvp.Value;
						pocPrice = kvp.Key;
					}
				}
				if (maxVol > 0 && ShowValueArea && (ShowVAColor || ShowVALines))
				{
					haveVA = CalcValueArea(volMap, pocPrice, out vahPrice, out valPrice);
				}

				if (!isActive)
				{
					cache[0] = vahPrice;
					cache[1] = valPrice;
					cache[2] = pocPrice;
					cache[3] = maxVol;
					cache[4] = volumeCompressionTicks;
					lock (barDataSync)
						if (barIdx >= 0 && barVACache != null && barIdx < barVACache.Count)
							barVACache[barIdx] = cache;
				}
			}
			else
			{
				vahPrice = cache[0];
				valPrice = cache[1];
				pocPrice = cache[2];
				maxVol   = (long)cache[3];
				haveVA = !double.IsNaN(vahPrice);
			}

			if (maxVol <= 0) return;

			// Get delta map if needed
			Dictionary<double, long> deltaMap = null;
			long maxAbsDelta = 0;
			if ((ShowDelta || emphasizeVolumeRowsByDelta) && deltaSource != null && deltaSource.Count > 0)
			{
				deltaMap = BuildAggregatedMap(deltaSource, volumeCompressionTicks, 1);
				foreach (var kvp in deltaMap)
				{
					long absVal = Math.Abs(kvp.Value);
					if (absVal > maxAbsDelta) maxAbsDelta = absVal;
				}
			}

			double compHeight = volumeCompressionTicks * TickSize;

			foreach (var kvp in volMap)
			{
				double price = kvp.Key;
				long   vol   = kvp.Value;
				long rowDelta = 0;
				bool hasRowDelta = deltaMap != null && deltaMap.TryGetValue(price, out rowDelta);
				bool emphasizeRow = emphasizeVolumeRowsByDelta
					&& hasRowDelta
					&& FootprintFormatting.IsDeltaEmphasis(rowDelta, vol, VolumeTextDeltaMinAbsolute, VolumeTextDeltaMinPercent);

				int yTop = chartScale.GetYByValue(price + compHeight);
				int yBot = chartScale.GetYByValue(price);

				if (yBot < panelTop - 20 || yTop > panelBottom + 20) continue;

				int rowHeight = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
				float drawY   = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;

				float barWidth = (float)(drawProfileWidth * (vol / (double)maxVol));
				if (barWidth < 0.5f) continue;

				RectangleF rect = flowsRight
					? new RectangleF(profileRootX, drawY, barWidth, rowHeight)
					: new RectangleF(profileRootX - barWidth, drawY, barWidth, rowHeight);

				// Determine if this row is inside the Value Area
				bool insideVA = haveVA && price >= valPrice - TickSize * 0.01 && price <= vahPrice + TickSize * 0.01;

				// Choose brush: POC > thresholded row emphasis > continuous delta > Gradient/Flat
				SolidColorBrush brush;

				if (ShowPOC && Math.Abs(price - pocPrice) < TickSize * 0.01)
				{
					brush = pocBrushDx;
				}
				else if (emphasizeRow)
				{
					brush = rowDelta > 0 ? posDeltaBrushDx : negDeltaBrushDx;
				}
				else if (ShowDelta && hasRowDelta)
				{
					brush = SelectDeltaBrush(rowDelta, maxAbsDelta);
				}
				else if (UseGradient)
				{
					// Pick gradient palette based on VA membership
					var palette = (ShowValueArea && ShowVAColor && insideVA && vaGradientBrushes != null)
						? vaGradientBrushes
						: volGradientBrushes;

					if (palette != null)
					{
						double ratio = vol / (double)maxVol;
						int steps = palette.Length;
						int gradIdx = (int)(ratio * (steps - 1));
						if (gradIdx < 0) gradIdx = 0;
						if (gradIdx >= steps) gradIdx = steps - 1;
						brush = palette[gradIdx];
					}
					else
					{
						brush = volBrushDx;
					}
				}
				else
				{
					// Flat color: VA color or regular
					brush = (ShowValueArea && ShowVAColor && insideVA) ? vaVolBrushDx : volBrushDx;
				}

				RenderTarget.FillRectangle(rect, brush);

				DrawVolumeTextLabel(vol, maxVol, profileRootX, drawProfileWidth, drawY, rowHeight, flowsRight);
			}

			// --- Draw VA boundary lines ---
			if (haveVA && ShowValueArea && ShowVALines && vaLineBrushDx != null)
			{
				float lineLeft = flowsRight ? profileRootX - 2 : profileRootX - drawProfileWidth - 2;
				float lineRight = flowsRight ? profileRootX + drawProfileWidth + 2 : profileRootX + 2;

				// VAH line (top of value area)
				float yVAH = chartScale.GetYByValue(vahPrice + compHeight);
				if (yVAH >= panelTop - 5 && yVAH <= panelBottom + 5)
				{
					RenderTarget.DrawLine(
						new Vector2(lineLeft, yVAH),
						new Vector2(lineRight, yVAH),
						vaLineBrushDx, VALineThickness, vaLineStrokeDx);
				}

				// VAL line (bottom of value area)
				float yVAL = chartScale.GetYByValue(valPrice);
				if (yVAL >= panelTop - 5 && yVAL <= panelBottom + 5)
				{
					RenderTarget.DrawLine(
						new Vector2(lineLeft, yVAL),
						new Vector2(lineRight, yVAL),
						vaLineBrushDx, VALineThickness, vaLineStrokeDx);
				}
			}
		}

		private void DrawBarDeltaProfile(ChartScale chartScale, int barIdx, float profileRootX, float panelTop, float panelBottom, float drawProfileWidth, int deltaCompressionTicks, bool flowsRight, TextAlignment textAlignment, bool includeZeroVolumeRows)
		{
			Dictionary<double, long> deltaSource = null;
			Dictionary<double, long> volumeSource = null;
			lock (barDataSync)
			{
				if (barIdx < 0 || barDeltaMaps == null || barIdx >= barDeltaMaps.Count)
					return;

				if (barDeltaMaps[barIdx] != null && barDeltaMaps[barIdx].Count > 0)
					deltaSource = new Dictionary<double, long>(barDeltaMaps[barIdx]);

				if (includeZeroVolumeRows && barDeltaVolumeMaps != null && barIdx < barDeltaVolumeMaps.Count && barDeltaVolumeMaps[barIdx] != null && barDeltaVolumeMaps[barIdx].Count > 0)
					volumeSource = new Dictionary<double, long>(barDeltaVolumeMaps[barIdx]);
			}

			var deltaMap = deltaSource != null ? BuildAggregatedMap(deltaSource, deltaCompressionTicks, 1) : new Dictionary<double, long>();
			Dictionary<double, long> renderMap = new Dictionary<double, long>(deltaMap);
			if (includeZeroVolumeRows && volumeSource != null)
			{
				var volumeRows = BuildAggregatedMap(volumeSource, deltaCompressionTicks, 1);
				foreach (var price in volumeRows.Keys)
				{
					if (!renderMap.ContainsKey(price))
						renderMap[price] = 0;
				}
			}

			if (renderMap.Count == 0) return;

			long maxAbsDelta = 0;
			foreach (var kvp in renderMap)
			{
				long absVal = Math.Abs(kvp.Value);
				if (absVal > maxAbsDelta) maxAbsDelta = absVal;
			}
			if (maxAbsDelta <= 0)
			{
				if (!includeZeroVolumeRows)
					return;

				maxAbsDelta = 1;
			}

			double compHeight = Math.Max(1, deltaCompressionTicks) * TickSize;
			var prices = new List<double>(renderMap.Keys);
			prices.Sort();

			foreach (var price in prices)
			{
				long delta = renderMap[price];

				int yTop = chartScale.GetYByValue(price + compHeight);
				int yBot = chartScale.GetYByValue(price);

				if (yBot < panelTop - 20 || yTop > panelBottom + 20) continue;

				int rowHeight = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
				float drawY = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;

				float barWidth = (float)(drawProfileWidth * (Math.Abs(delta) / (double)maxAbsDelta));
				if (delta != 0)
				{
					if (barWidth < 0.5f) continue;

					RectangleF rect = flowsRight
						? new RectangleF(profileRootX, drawY, barWidth, rowHeight)
						: new RectangleF(profileRootX - barWidth, drawY, barWidth, rowHeight);

					SolidColorBrush brush = SelectDeltaBrush(delta, maxAbsDelta);
					RenderTarget.FillRectangle(rect, brush);
				}

				DrawDeltaTextLabel(delta, profileRootX, drawProfileWidth, drawY, rowHeight, flowsRight, textAlignment, includeZeroVolumeRows && delta == 0);
			}
		}


		private void DrawBarBidAskProfile(ChartScale chartScale, int barIdx, float barCenterX, float panelTop, float panelBottom,
			float drawProfileWidth, int compressionTicks, float requestedCenterGap, float requestedBodyDeltaWidth, double open, double close)
		{
			Dictionary<double, long> askSource;
			Dictionary<double, long> bidSource;
			Dictionary<double, long> unclassifiedSource;
			lock (barDataSync)
			{
				if (barIdx < 0 || barAskVolumeMaps == null || barBidVolumeMaps == null || barUnclassifiedVolumeMaps == null
					|| barIdx >= barAskVolumeMaps.Count || barIdx >= barBidVolumeMaps.Count || barIdx >= barUnclassifiedVolumeMaps.Count)
					return;

				askSource = new Dictionary<double, long>(barAskVolumeMaps[barIdx]);
				bidSource = new Dictionary<double, long>(barBidVolumeMaps[barIdx]);
				unclassifiedSource = new Dictionary<double, long>(barUnclassifiedVolumeMaps[barIdx]);
			}

			Dictionary<double, long> askMap = BuildAggregatedMap(askSource, compressionTicks, 1);
			Dictionary<double, long> bidMap = BuildAggregatedMap(bidSource, compressionTicks, 1);
			Dictionary<double, long> unclassifiedMap = BuildAggregatedMap(unclassifiedSource, compressionTicks, 1);
			HashSet<double> priceSet = new HashSet<double>(askMap.Keys);
			priceSet.UnionWith(bidMap.Keys);
			priceSet.UnionWith(unclassifiedMap.Keys);
			if (priceSet.Count == 0)
				return;

			List<double> prices = new List<double>(priceSet);
			prices.Sort();
			long maxRowTotal = 0;
			long maxSideVolume = 0;
			double pocPrice = double.NaN;
			foreach (double price in prices)
			{
				long ask = GetMapVolume(askMap, price);
				long bid = GetMapVolume(bidMap, price);
				long unclassified = GetMapVolume(unclassifiedMap, price);
				long rowTotal = ask + bid + unclassified;
				if (rowTotal > maxRowTotal)
				{
					maxRowTotal = rowTotal;
					pocPrice = price;
				}
				maxSideVolume = Math.Max(maxSideVolume, Math.Max(ask, bid));
			}

			if (maxRowTotal <= 0)
				return;
			if (maxSideVolume <= 0)
				maxSideVolume = 1;

			float fullWidth = Math.Max(2f, drawProfileWidth);
			float centerGap = Math.Max(0f, Math.Min(requestedCenterGap, Math.Max(0f, fullWidth - 2f)));
			float halfWidth = Math.Max(1f, (fullWidth - centerGap) / 2f);
			float bidRoot = barCenterX - centerGap / 2f;
			float askRoot = barCenterX + centerGap / 2f;
			float left = bidRoot - halfWidth;
			double compHeight = Math.Max(1, compressionTicks) * TickSize;
			bool drawBodyDelta = FootprintScaffold == FootprintScaffoldMode.HollowBodyDelta && centerGap > 0f;
			float bodyDeltaWidth = drawBodyDelta
				? Math.Min(requestedBodyDeltaWidth, Math.Max(1f, centerGap - 2f * Math.Max(0, CandleProfileGapPx)))
				: 0f;
			long maxCenterAbsDelta = 0;
			if (drawBodyDelta)
			{
				foreach (double price in prices)
				{
					long centerDelta = GetMapVolume(askMap, price) - GetMapVolume(bidMap, price);
					maxCenterAbsDelta = Math.Max(maxCenterAbsDelta, FootprintFormatting.Magnitude(centerDelta));
				}
			}

			foreach (double price in prices)
			{
				long ask = GetMapVolume(askMap, price);
				long bid = GetMapVolume(bidMap, price);
				long unclassified = GetMapVolume(unclassifiedMap, price);
				long rowTotal = ask + bid + unclassified;
				long rowDelta = ask - bid;
				if (rowTotal <= 0)
					continue;

				int yTop = chartScale.GetYByValue(price + compHeight);
				int yBot = chartScale.GetYByValue(price);
				if (yBot < panelTop - 20 || yTop > panelBottom + 20)
					continue;

				int rowHeight = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
				float drawY = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;
				RectangleF rowRect = new RectangleF(left, drawY, fullWidth, rowHeight);

				if (BidAskStyle == CandleProfileBidAskStyle.Cluster)
				{
					SolidColorBrush brush = SelectBidAskBrush(rowDelta, rowTotal, maxRowTotal);
					if (brush != null)
					{
						if (centerGap > 0)
						{
							RenderTarget.FillRectangle(new RectangleF(left, drawY, halfWidth, rowHeight), brush);
							RenderTarget.FillRectangle(new RectangleF(askRoot, drawY, halfWidth, rowHeight), brush);
						}
						else RenderTarget.FillRectangle(rowRect, brush);
					}
					DrawBidAskClusterText(bid, ask, rowTotal, rowRect, centerGap);
				}
				else
				{
					if (unclassified > 0)
					{
						SolidColorBrush neutralBrush = SelectBidAskBrush(0, unclassified, maxRowTotal);
						if (neutralBrush != null)
							RenderTarget.FillRectangle(rowRect, neutralBrush);
					}

					float bidWidth = (float)(halfWidth * (bid / (double)maxSideVolume));
					float askWidth = (float)(halfWidth * (ask / (double)maxSideVolume));
					if (bidWidth >= 0.5f)
					{
						SolidColorBrush bidBrush = SelectBidAskBrush(-1, bid, maxSideVolume);
						if (bidBrush != null)
							RenderTarget.FillRectangle(new RectangleF(bidRoot - bidWidth, drawY, bidWidth, rowHeight), bidBrush);
					}
					if (askWidth >= 0.5f)
					{
						SolidColorBrush askBrush = SelectBidAskBrush(1, ask, maxSideVolume);
						if (askBrush != null)
							RenderTarget.FillRectangle(new RectangleF(askRoot, drawY, askWidth, rowHeight), askBrush);
					}
					DrawBidAskHistogramText(bid, ask, rowTotal, left, askRoot, halfWidth, drawY, rowHeight);
				}

				if (drawBodyDelta)
				{
					bool bodyRow = FootprintFormatting.IntersectsBody(price, price + compHeight, open, close);
					DrawHollowBodyDeltaRow(barCenterX, bodyDeltaWidth, drawY, rowHeight, bid, ask,
						unclassified, rowDelta, maxCenterAbsDelta, bodyRow);
				}

				if (ShowPOC && Math.Abs(price - pocPrice) < TickSize * 0.01 && pocBrushDx != null)
				{
					if (centerGap > 0)
					{
						RenderTarget.DrawRectangle(new RectangleF(left, drawY, halfWidth, rowHeight), pocBrushDx, 1f);
						RenderTarget.DrawRectangle(new RectangleF(askRoot, drawY, halfWidth, rowHeight), pocBrushDx, 1f);
					}
					else RenderTarget.DrawRectangle(rowRect, pocBrushDx, 1f);
				}
			}
		}

		private void DrawHollowBodyDeltaRow(float barCenterX, float width, float drawY, float rowHeight,
			long bid, long ask, long unclassified, long rowDelta, long maxCenterAbsDelta, bool bodyRow)
		{
			if (width < 2f || rowHeight < 1f)
				return;

			SolidColorBrush brush = SelectBidAskBrush(rowDelta, FootprintFormatting.Magnitude(rowDelta), maxCenterAbsDelta);
			if (brush == null)
				return;

			RectangleF rectangle = new RectangleF(barCenterX - width / 2f, drawY, width, rowHeight);
			if (bodyRow)
				RenderTarget.DrawRectangle(rectangle, brush, 1f);
			else
			{
				SolidColorBrush separator = SelectBidAskBrush(0, 0, 1);
				if (separator != null && width >= 6f)
					RenderTarget.DrawLine(new Vector2(rectangle.Left + 2f, rectangle.Bottom - 0.5f),
						new Vector2(rectangle.Right - 2f, rectangle.Bottom - 0.5f), separator, 0.5f);
			}

			float fontSize = ResolveProfileTextFontSize(rowHeight, BidAskTextFontSize);
			if (rowHeight < Math.Max(5f, fontSize - 1f))
				return;

			string label = bid == 0 && ask == 0 && unclassified > 0
				? "N/A"
				: FootprintFormatting.SignedNumber(rowDelta, false);
			DrawTextInRectangle(label, brush,
				new RectangleF(rectangle.Left + 1f, rectangle.Top - 1f, Math.Max(2f, rectangle.Width - 2f), rectangle.Height + 2f),
				fontSize, TextAlignment.Center);
		}

		private static long GetMapVolume(Dictionary<double, long> map, double price)
		{
			long value;
			return map != null && map.TryGetValue(price, out value) ? value : 0;
		}

		private long GetDisplayBucketVolume(Dictionary<double, long> map, double bucketPrice, int compressionTicks)
		{
			if (map == null || map.Count == 0)
				return 0;

			double compression = Math.Max(1, compressionTicks) * TickSize;
			long total = 0;
			foreach (KeyValuePair<double, long> entry in map)
			{
				double entryBucket = Math.Floor(entry.Key / compression + 0.000001) * compression;
				if (Math.Abs(entryBucket - bucketPrice) < TickSize * 0.01)
					total += entry.Value;
			}

			return total;
		}

		private bool TryGetQualifiedVolumeTextEvidence(int barIndex, double bucketPrice, int compressionTicks,
			out long bid, out long ask, out long unclassified)
		{
			bid = 0;
			ask = 0;
			unclassified = 0;
			if (!IsDeltaColoredVolumeTextActive || barIndex < 0)
				return false;

			lock (barDataSync)
			{
				if (barVolumeMaps == null || barDeltaMaps == null || barBidVolumeMaps == null
					|| barAskVolumeMaps == null || barUnclassifiedVolumeMaps == null
					|| barIndex >= barVolumeMaps.Count || barIndex >= barDeltaMaps.Count
					|| barIndex >= barBidVolumeMaps.Count || barIndex >= barAskVolumeMaps.Count
					|| barIndex >= barUnclassifiedVolumeMaps.Count)
					return false;

				long volume = GetDisplayBucketVolume(barVolumeMaps[barIndex], bucketPrice, compressionTicks);
				long delta = GetDisplayBucketVolume(barDeltaMaps[barIndex], bucketPrice, compressionTicks);
				if (volume < VolumeTextMinThreshold
					|| !FootprintFormatting.IsDeltaEmphasis(delta, volume, VolumeTextDeltaMinAbsolute, VolumeTextDeltaMinPercent))
					return false;

				bid = GetDisplayBucketVolume(barBidVolumeMaps[barIndex], bucketPrice, compressionTicks);
				ask = GetDisplayBucketVolume(barAskVolumeMaps[barIndex], bucketPrice, compressionTicks);
				unclassified = GetDisplayBucketVolume(barUnclassifiedVolumeMaps[barIndex], bucketPrice, compressionTicks);
				return true;
			}
		}

		private SolidColorBrush SelectBidAskBrush(long deltaSign, long intensityValue, long maxIntensity)
		{
			SolidColorBrush[] palette = deltaSign > 0
				? bidAskPositiveIntensityBrushes
				: deltaSign < 0 ? bidAskNegativeIntensityBrushes : bidAskNeutralIntensityBrushes;
			if (palette == null || palette.Length == 0)
				return deltaSign > 0 ? posDeltaBrushDx : deltaSign < 0 ? negDeltaBrushDx : volBrushDx;

			double intensity = intensityValue / Math.Max(1.0, maxIntensity);
			intensity = Math.Max(0.0, Math.Min(1.0, intensity));
			int brushIdx = (int)Math.Round(intensity * (palette.Length - 1));
			brushIdx = Math.Max(0, Math.Min(palette.Length - 1, brushIdx));
			return palette[brushIdx];
		}

		private void DrawBidAskClusterText(long bid, long ask, long rowTotal, RectangleF rowRect, float centerGap)
		{
			if (!ShowBidAskText || bidAskTextBrushDx == null || bid + ask < BidAskTextMinThreshold)
				return;

			float fontSize = ResolveProfileTextFontSize(rowRect.Height, BidAskTextFontSize);
			if (rowRect.Height < Math.Max(5f, fontSize - 1f) || rowRect.Width < 16f)
				return;

			bool bidWinner = FootprintEmphasizeWinner && FootprintFormatting.IsWinner(bid, ask, FootprintWinnerRatio);
			bool askWinner = FootprintEmphasizeWinner && FootprintFormatting.IsWinner(ask, bid, FootprintWinnerRatio);
			float separatorWidth = centerGap > 0
				? Math.Max(0f, Math.Min(centerGap, Math.Max(0f, rowRect.Width - 8f)))
				: Math.Min(8f, Math.Max(4f, rowRect.Width * 0.12f));
			float sideWidth = Math.Max(4f, (rowRect.Width - separatorWidth) / 2f);
			DrawTextInRectangle(bid.ToString("N0"), bidAskTextBrushDx,
				new RectangleF(rowRect.Left, rowRect.Top, sideWidth, rowRect.Height), fontSize, TextAlignment.Trailing, bidWinner);
			if (centerGap <= 0)
				DrawTextInRectangle("x", bidAskTextBrushDx,
					new RectangleF(rowRect.Left + sideWidth, rowRect.Top, separatorWidth, rowRect.Height), fontSize, TextAlignment.Center);
			DrawTextInRectangle(ask.ToString("N0"), bidAskTextBrushDx,
				new RectangleF(rowRect.Right - sideWidth, rowRect.Top, sideWidth, rowRect.Height), fontSize, TextAlignment.Leading, askWinner);
		}

		private void DrawBidAskHistogramText(long bid, long ask, long rowTotal, float left, float center, float halfWidth, float drawY, float rowHeight)
		{
			if (!ShowBidAskText || bidAskTextBrushDx == null || bid + ask < BidAskTextMinThreshold)
				return;

			float fontSize = ResolveProfileTextFontSize(rowHeight, BidAskTextFontSize);
			if (rowHeight < Math.Max(5f, fontSize - 1f) || halfWidth < 8f)
				return;

			bool bidWinner = FootprintEmphasizeWinner && FootprintFormatting.IsWinner(bid, ask, FootprintWinnerRatio);
			bool askWinner = FootprintEmphasizeWinner && FootprintFormatting.IsWinner(ask, bid, FootprintWinnerRatio);
			DrawTextInRectangle(bid.ToString("N0"), bidAskTextBrushDx, new RectangleF(left + 1f, drawY - 1f, Math.Max(4f, halfWidth - 3f), rowHeight + 2f), fontSize, TextAlignment.Trailing, bidWinner);
			DrawTextInRectangle(ask.ToString("N0"), bidAskTextBrushDx, new RectangleF(center + 2f, drawY - 1f, Math.Max(4f, halfWidth - 3f), rowHeight + 2f), fontSize, TextAlignment.Leading, askWinner);
		}

		private void DrawTextInRectangle(string label, SolidColorBrush brush, RectangleF rectangle, float fontSize, TextAlignment alignment, bool winner = false)
		{
			if (string.IsNullOrEmpty(label) || brush == null)
				return;

			TextFormat format = GetProfileTextFormat(fontSize, alignment, winner);
			if (format != null)
				RenderTarget.DrawText(label, format, rectangle, brush);
		}
		private SolidColorBrush SelectDeltaBrush(long delta, long maxAbsDelta)
		{
			if (!UseDeltaIntensityColoring || maxAbsDelta <= 0 || positiveDeltaIntensityBrushes == null || negativeDeltaIntensityBrushes == null)
				return delta >= 0 ? posDeltaBrushDx : negDeltaBrushDx;

			SolidColorBrush[] palette = delta >= 0 ? positiveDeltaIntensityBrushes : negativeDeltaIntensityBrushes;
			if (palette == null || palette.Length == 0)
				return delta >= 0 ? posDeltaBrushDx : negDeltaBrushDx;

			double intensity = Math.Abs((double)delta) / Math.Max(1.0, maxAbsDelta);
			if (intensity < 0.0) intensity = 0.0;
			if (intensity > 1.0) intensity = 1.0;

			int brushIdx = (int)Math.Round(intensity * (palette.Length - 1));
			if (brushIdx < 0) brushIdx = 0;
			if (brushIdx >= palette.Length) brushIdx = palette.Length - 1;

			return palette[brushIdx] ?? (delta >= 0 ? posDeltaBrushDx : negDeltaBrushDx);
		}

		private SolidColorBrush SelectVolumeTextBrush(long volume, long candleMaximum)
		{
			if (!IsVolumeTextBrightnessActive || volumeTextIntensityBrushes == null || volumeTextIntensityBrushes.Length == 0)
				return volumeTextBrushDx;

			double intensity = FootprintFormatting.VolumeTextIntensity(volume, candleMaximum);
			int brushIndex = (int)Math.Round(intensity * (volumeTextIntensityBrushes.Length - 1));
			if (brushIndex < 0) brushIndex = 0;
			if (brushIndex >= volumeTextIntensityBrushes.Length) brushIndex = volumeTextIntensityBrushes.Length - 1;
			return volumeTextIntensityBrushes[brushIndex] ?? volumeTextBrushDx;
		}

		private void DrawVolumeTextLabel(long volume, long candleMaximum, float profileRootX, float drawProfileWidth, float drawY, float rowHeight, bool flowsRight)
		{
			if (!ShowVolumeText || volumeTextBrushDx == null)
				return;
			if (volume < VolumeTextMinThreshold)
				return;

			float fontSize = ResolveProfileTextFontSize(rowHeight, VolumeTextFontSize);
			if (rowHeight < Math.Max(5f, fontSize - 1f))
				return;

			SolidColorBrush textBrush = SelectVolumeTextBrush(volume, candleMaximum) ?? volumeTextBrushDx;
			DrawProfileTextLabel(volume.ToString("N0"), textBrush, profileRootX, drawProfileWidth, drawY, rowHeight, flowsRight, fontSize, TextAlignment.Leading);
		}

		private void DrawDeltaTextLabel(long delta, float profileRootX, float drawProfileWidth, float drawY, float rowHeight, bool flowsRight, TextAlignment textAlignment, bool forceZeroLabel)
		{
			if (!ShowDeltaText || deltaTextBrushDx == null)
				return;
			if (!forceZeroLabel && Math.Abs(delta) < DeltaTextMinThreshold)
				return;

			float fontSize = ResolveProfileTextFontSize(rowHeight, DeltaTextFontSize);
			if (rowHeight < Math.Max(5f, fontSize - 1f))
				return;

			DrawProfileTextLabel(delta.ToString("+#;-#;0"), deltaTextBrushDx, profileRootX, drawProfileWidth, drawY, rowHeight, flowsRight, fontSize, textAlignment);
		}

		private void DrawProfileTextLabel(string label, SolidColorBrush brush, float profileRootX, float drawProfileWidth, float drawY, float rowHeight, bool flowsRight, float fontSize, TextAlignment alignment, bool emphasized = false)
		{
			if (string.IsNullOrEmpty(label) || brush == null)
				return;

			TextFormat format = GetProfileTextFormat(fontSize, alignment, emphasized);
			if (format == null)
				return;

			float textLeft = flowsRight ? profileRootX : profileRootX - drawProfileWidth;
			float textWidth = Math.Max(8f, drawProfileWidth - 2f);
			RenderTarget.DrawText(
				label,
				format,
				new RectangleF(textLeft, drawY - 1f, textWidth, rowHeight + 2f),
				brush);
		}

		private float ResolveProfileTextFontSize(float rowHeight, float fixedFontSize)
		{
			float baseSize = Math.Max(6f, Math.Min(24f, fixedFontSize));
			if (!UseDynamicTextSizing)
				return baseSize;

			float maxSize = Math.Max(baseSize, Math.Min(32f, DynamicTextMaxFontSize));
			float rowSize = Math.Max(baseSize, rowHeight - 2f);
			return Math.Min(maxSize, rowSize);
		}

		private int ResolveVolumeCompressionTicks(ChartScale chartScale)
		{
			int baseTicks = Math.Max(1, TickCompression);
			if (!UseDynamicVolumeAggregation || chartScale == null || ChartPanel == null || TickSize <= 0)
				return baseTicks;

			double visibleTicks = Math.Max(1.0, (chartScale.MaxValue - chartScale.MinValue) / TickSize);
			double ticksPerPixel = visibleTicks / Math.Max(1.0, ChartPanel.H);
			double desiredTicks = ticksPerPixel * 3.0 * Math.Max(0.1, VolumeDynamicAggregationMultiplier);
			int resolved = RoundToGentleVolumeTicks(desiredTicks);
			int maxTicks = Math.Max(baseTicks, MaxDynamicVolumeTicks);

			return Math.Max(baseTicks, Math.Min(maxTicks, resolved));
		}

		private int ResolveDeltaCompressionTicks(ChartScale chartScale)
		{
			int baseTicks = Math.Max(1, DeltaTickCompression);
			if (!UseDynamicDeltaAggregation || chartScale == null || ChartPanel == null || TickSize <= 0)
				return baseTicks;

			double visibleTicks = Math.Max(1.0, (chartScale.MaxValue - chartScale.MinValue) / TickSize);
			double ticksPerPixel = visibleTicks / Math.Max(1.0, ChartPanel.H);
			double desiredTicks = ticksPerPixel * Math.Max(1, DeltaDynamicRowMinPixels) * Math.Max(0.1, DeltaDynamicMultiplier);
			int resolved = RoundToDeltaTicks(desiredTicks);
			resolved = ClampDeltaCompression(resolved);

			if (lastDynamicDeltaComp > 0 && Math.Abs(resolved - lastDynamicDeltaComp) < Math.Max(2, resolved * 0.15))
				resolved = lastDynamicDeltaComp;
			else
				lastDynamicDeltaComp = resolved;

			return resolved;
		}

		private int RoundToDeltaTicks(double desiredTicks)
		{
			if (desiredTicks <= 1) return 1;
			if (desiredTicks <= 2) return 2;
			if (desiredTicks <= 4) return 4;
			if (desiredTicks <= 5) return 5;
			if (desiredTicks <= 8) return 8;
			if (desiredTicks <= 10) return 10;
			if (desiredTicks <= 15) return 15;
			if (desiredTicks <= 20) return 20;
			if (desiredTicks <= 25) return 25;
			if (desiredTicks <= 30) return 30;
			if (desiredTicks <= 40) return 40;
			if (desiredTicks <= 50) return 50;
			if (desiredTicks <= 100) return (int)(Math.Round(desiredTicks / 20.0) * 20);
			return (int)(Math.Round(desiredTicks / 50.0) * 50);
		}

		private int ClampDeltaCompression(int compression)
		{
			int min = Math.Max(1, DynamicDeltaMinCompression);
			int max = Math.Max(min, DynamicDeltaMaxCompression);
			if (compression < min) return min;
			if (compression > max) return max;
			return compression;
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

		private float MeasureTextWidth(string text)
		{
			if (textFormatDx == null) return 0f;
			if (textWidthCache.TryGetValue(text, out float width))
				return width;

			using (var layout = new TextLayout(Core.Globals.DirectWriteFactory, text, textFormatDx, 1000, 100))
			{
				width = layout.Metrics.Width;
				textWidthCache[text] = width;
				return width;
			}
		}

		private TextFormat GetProfileTextFormat(float fontSize, TextAlignment alignment, bool winner = false)
		{
			if (Core.Globals.DirectWriteFactory == null)
				return null;

			string signature = GetProfileTextFormatSignature();
			if (!string.Equals(lastProfileTextFormatSignature, signature, StringComparison.Ordinal))
			{
				DisposeTextFormats();
				textWidthCache.Clear();
				lastProfileTextFormatSignature = signature;
			}

			float resolvedSize = Math.Max(6f, Math.Min(32f, fontSize));
			int sizeKey = (int)Math.Round(resolvedSize * 10f);
			if (sizeKey < 1)
				sizeKey = 1;
			int key = sizeKey * 100 + (int)alignment * 10 + (winner ? 1 : 0);

			if (textFormatsBySize == null)
				textFormatsBySize = new Dictionary<int, TextFormat>();

			TextFormat format;
			if (textFormatsBySize.TryGetValue(key, out format) && format != null)
				return format;

			format = CreateProfileTextFormat(sizeKey / 10f, alignment, winner);
			if (format == null)
				return null;

			textFormatsBySize[key] = format;
			return format;
		}

		private TextFormat CreateProfileTextFormat(float fontSize, TextAlignment alignment, bool winner = false)
		{
			if (Core.Globals.DirectWriteFactory == null)
				return null;

			TextFormat format = null;
			string family = GetProfileTextFontFamily();
			SharpDX.DirectWrite.FontWeight weight = winner ? ResolveFootprintWinnerWeight() : ResolveProfileTextFontWeight();

			try
			{
				format = new TextFormat(Core.Globals.DirectWriteFactory, family, weight, SharpDX.DirectWrite.FontStyle.Normal, fontSize);
			}
			catch
			{
				try
				{
					format = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", weight, SharpDX.DirectWrite.FontStyle.Normal, fontSize);
				}
				catch
				{
					return null;
				}
			}

			format.TextAlignment = alignment;
			format.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;
			format.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap;
			return format;
		}

		private string GetProfileTextFontFamily()
		{
			return string.IsNullOrWhiteSpace(TextFontFamily) ? "Segoe UI" : TextFontFamily.Trim();
		}

		private SharpDX.DirectWrite.FontWeight ResolveProfileTextFontWeight()
		{
			switch (TextFontWeight)
			{
				case CandleProfileTextFontWeight.Regular:
					return SharpDX.DirectWrite.FontWeight.Normal;
				case CandleProfileTextFontWeight.Medium:
					return SharpDX.DirectWrite.FontWeight.Medium;
				case CandleProfileTextFontWeight.SemiBold:
					return SharpDX.DirectWrite.FontWeight.SemiBold;
				case CandleProfileTextFontWeight.ExtraBold:
					return SharpDX.DirectWrite.FontWeight.ExtraBold;
				default:
					return SharpDX.DirectWrite.FontWeight.Bold;
			}
		}

		private string GetProfileTextFormatSignature()
		{
			return GetProfileTextFontFamily() + "|" + TextFontWeight;
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxResourceRenderTarget != IntPtr.Zero && dxResourceRenderTarget != currentTarget)
				DisposeDx();

			if (bullBodyBrushDx == null)
				bullBodyBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BullishBodyBrush, 1f));
			if (bearBodyBrushDx == null)
				bearBodyBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BearishBodyBrush, 1f));
			if (compressedBullBodyBrushDx == null)
				compressedBullBodyBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(CompressedBullishBodyBrush, 1f));
			if (compressedBearBodyBrushDx == null)
				compressedBearBodyBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(CompressedBearishBodyBrush, 1f));
			if (bullWickBrushDx == null)
				bullWickBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BullishBodyBrush, 1f));
			if (bearWickBrushDx == null)
				bearWickBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BearishBodyBrush, 1f));
			if (volBrushDx == null)
				volBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VolumeBrush, VolumeOpacity));
			if (pocBrushDx == null)
				pocBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(POCBrush, 1f));
			if (posDeltaBrushDx == null)
				posDeltaBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(PositiveDeltaBrush, DeltaOpacity));
			if (negDeltaBrushDx == null)
				negDeltaBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaBrush, DeltaOpacity));
			int volumeTextIntensitySteps = Math.Max(2, GradientSteps);
			float volumeTextMinBrightness = (float)Math.Max(0.05, Math.Min(1.0, VolumeTextMinBrightness));
			if (!IsVolumeTextBrightnessActive)
				DisposeBrushPalette(ref volumeTextIntensityBrushes);
			else if (volumeTextIntensityBrushes == null
				|| lastBuiltVolumeTextIntensitySteps != volumeTextIntensitySteps
				|| Math.Abs(lastBuiltVolumeTextMinBrightness - volumeTextMinBrightness) > 0.001f)
			{
				DisposeBrushPalette(ref volumeTextIntensityBrushes);
				volumeTextIntensityBrushes = BuildOpacityPalette(VolumeTextBrush, volumeTextIntensitySteps, volumeTextMinBrightness, 1f);
				lastBuiltVolumeTextIntensitySteps = volumeTextIntensitySteps;
				lastBuiltVolumeTextMinBrightness = volumeTextMinBrightness;
			}
			int deltaIntensitySteps = Math.Max(2, GradientSteps);
			float deltaIntensityMinOpacity = (float)Math.Max(0.05, Math.Min(1.0, DeltaIntensityMinOpacity));
			float deltaIntensityMaxOpacity = (float)Math.Max(0.1, Math.Min(1.0, DeltaOpacity));
			if (!UseDeltaIntensityColoring)
			{
				DisposeBrushPalette(ref positiveDeltaIntensityBrushes);
				DisposeBrushPalette(ref negativeDeltaIntensityBrushes);
			}
			else if (positiveDeltaIntensityBrushes == null
				|| negativeDeltaIntensityBrushes == null
				|| lastBuiltDeltaIntensitySteps != deltaIntensitySteps
				|| Math.Abs(lastBuiltDeltaIntensityMinOpacity - deltaIntensityMinOpacity) > 0.001f
				|| Math.Abs(lastBuiltDeltaIntensityMaxOpacity - deltaIntensityMaxOpacity) > 0.001f)
			{
				DisposeBrushPalette(ref positiveDeltaIntensityBrushes);
				DisposeBrushPalette(ref negativeDeltaIntensityBrushes);
				positiveDeltaIntensityBrushes = BuildDeltaIntensityPalette(PositiveDeltaBrush, deltaIntensitySteps, deltaIntensityMinOpacity, deltaIntensityMaxOpacity);
				negativeDeltaIntensityBrushes = BuildDeltaIntensityPalette(NegativeDeltaBrush, deltaIntensitySteps, deltaIntensityMinOpacity, deltaIntensityMaxOpacity);
				lastBuiltDeltaIntensitySteps = deltaIntensitySteps;
				lastBuiltDeltaIntensityMinOpacity = deltaIntensityMinOpacity;
				lastBuiltDeltaIntensityMaxOpacity = deltaIntensityMaxOpacity;
			}

			int bidAskIntensitySteps = Math.Max(2, GradientSteps);
			float bidAskMinOpacity = (float)Math.Max(0.05, Math.Min(1.0, BidAskMinOpacity));
			float bidAskMaxOpacity = (float)Math.Max(bidAskMinOpacity, Math.Min(1.0, BidAskMaxOpacity));
			if (ProfileDisplayMode != CandleProfileDisplayMode.BidAsk)
			{
				DisposeBrushPalette(ref bidAskPositiveIntensityBrushes);
				DisposeBrushPalette(ref bidAskNegativeIntensityBrushes);
				DisposeBrushPalette(ref bidAskNeutralIntensityBrushes);
			}
			else if (bidAskPositiveIntensityBrushes == null
				|| bidAskNegativeIntensityBrushes == null
				|| bidAskNeutralIntensityBrushes == null
				|| lastBuiltBidAskIntensitySteps != bidAskIntensitySteps
				|| Math.Abs(lastBuiltBidAskMinOpacity - bidAskMinOpacity) > 0.001f
				|| Math.Abs(lastBuiltBidAskMaxOpacity - bidAskMaxOpacity) > 0.001f)
			{
				DisposeBrushPalette(ref bidAskPositiveIntensityBrushes);
				DisposeBrushPalette(ref bidAskNegativeIntensityBrushes);
				DisposeBrushPalette(ref bidAskNeutralIntensityBrushes);
				bidAskPositiveIntensityBrushes = BuildOpacityPalette(BidAskPositiveBrush, bidAskIntensitySteps, bidAskMinOpacity, bidAskMaxOpacity);
				bidAskNegativeIntensityBrushes = BuildOpacityPalette(BidAskNegativeBrush, bidAskIntensitySteps, bidAskMinOpacity, bidAskMaxOpacity);
				bidAskNeutralIntensityBrushes = BuildOpacityPalette(BidAskNeutralBrush, bidAskIntensitySteps, bidAskMinOpacity, bidAskMaxOpacity);
				lastBuiltBidAskIntensitySteps = bidAskIntensitySteps;
				lastBuiltBidAskMinOpacity = bidAskMinOpacity;
				lastBuiltBidAskMaxOpacity = bidAskMaxOpacity;
			}
			if (deltaTextBrushDx == null)
				deltaTextBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(DeltaTextBrush, 1f));
			if (volumeTextBrushDx == null)
				volumeTextBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VolumeTextBrush, 1f));
			if (bidAskTextBrushDx == null)
				bidAskTextBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BidAskTextBrush, 1f));
			string textFormatSignature = GetProfileTextFormatSignature();
			float deltaTextSize = Math.Max(6f, Math.Min(20f, DeltaTextFontSize));
			if (textFormatDx == null || Math.Abs(lastBuiltDeltaTextFontSize - deltaTextSize) > 0.001f || !string.Equals(lastBuiltDeltaTextFormatSignature, textFormatSignature, StringComparison.Ordinal))
			{
				textFormatDx?.Dispose();
				textFormatDx = CreateProfileTextFormat(deltaTextSize, SharpDX.DirectWrite.TextAlignment.Trailing);
				lastBuiltDeltaTextFontSize = deltaTextSize;
				lastBuiltDeltaTextFormatSignature = textFormatSignature;
				textWidthCache.Clear();
			}

			// VA flat brush
			if (vaVolBrushDx == null)
				vaVolBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VABrush, VolumeOpacity));

			// VA line brush + dashed stroke
			if (vaLineBrushDx == null)
				vaLineBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VALineBrush, 1f));
			if (vaLineStrokeDx == null)
			{
				DashStyle ds;
				switch (VALineStyle)
				{
					case VALineStyleEnum.Solid:  ds = DashStyle.Solid;   break;
					case VALineStyleEnum.Dot:    ds = DashStyle.Dot;     break;
					case VALineStyleEnum.DashDot:ds = DashStyle.DashDot; break;
					default:                     ds = DashStyle.Dash;    break;
				}
				vaLineStrokeDx = new StrokeStyle(RenderTarget.Factory,
					new StrokeStyleProperties { DashStyle = ds });
			}

			// Build gradient palettes
			int steps = Math.Max(2, GradientSteps);

			// Outside-VA gradient
			if (UseGradient && (volGradientBrushes == null || lastBuiltGradientSteps != steps))
			{
				if (volGradientBrushes != null)
					for (int i = 0; i < volGradientBrushes.Length; i++)
						volGradientBrushes[i]?.Dispose();

				volGradientBrushes = BuildGradientPalette(VolumeBrush, steps);
				lastBuiltGradientSteps = steps;
			}

			// Inside-VA gradient
			if (UseGradient && ShowValueArea && ShowVAColor && (vaGradientBrushes == null || lastBuiltVAGradientSteps != steps))
			{
				if (vaGradientBrushes != null)
					for (int i = 0; i < vaGradientBrushes.Length; i++)
						vaGradientBrushes[i]?.Dispose();

				vaGradientBrushes = BuildGradientPalette(VABrush, steps);
				lastBuiltVAGradientSteps = steps;
			}
			dxResourceRenderTarget = currentTarget;
		}

		private SolidColorBrush[] BuildGradientPalette(WpfBrush baseBrush, int steps)
		{
			var baseColor = BrushToMediaColor(baseBrush);
			var palette = new SolidColorBrush[steps];

			for (int i = 0; i < steps; i++)
			{
				float t = i / (float)(steps - 1);
				float brightness = MinBrightness + t * (1f - MinBrightness);

				var c = new Color4(
					(baseColor.R / 255f) * brightness,
					(baseColor.G / 255f) * brightness,
					(baseColor.B / 255f) * brightness,
					(baseColor.A / 255f) * VolumeOpacity);

				palette[i] = new SolidColorBrush(RenderTarget, c);
			}

			return palette;
		}

		private SolidColorBrush[] BuildDeltaIntensityPalette(WpfBrush baseBrush, int steps, float minOpacity, float maxOpacity)
		{
			var baseColor = BrushToMediaColor(baseBrush);
			var palette = new SolidColorBrush[steps];

			for (int i = 0; i < steps; i++)
			{
				float t = i / (float)(steps - 1);
				float opacity = minOpacity + t * (1f - minOpacity);

				var c = new Color4(
					baseColor.R / 255f,
					baseColor.G / 255f,
					baseColor.B / 255f,
					(baseColor.A / 255f) * maxOpacity * opacity);

				palette[i] = new SolidColorBrush(RenderTarget, c);
			}

			return palette;
		}

		private SolidColorBrush[] BuildOpacityPalette(WpfBrush baseBrush, int steps, float minOpacity, float maxOpacity)
		{
			var baseColor = BrushToMediaColor(baseBrush);
			var palette = new SolidColorBrush[steps];

			for (int i = 0; i < steps; i++)
			{
				float t = i / (float)(steps - 1);
				float opacity = minOpacity + t * (maxOpacity - minOpacity);

				var c = new Color4(
					baseColor.R / 255f,
					baseColor.G / 255f,
					baseColor.B / 255f,
					(baseColor.A / 255f) * opacity);

				palette[i] = new SolidColorBrush(RenderTarget, c);
			}

			return palette;
		}

		private static System.Windows.Media.Color BrushToMediaColor(WpfBrush b)
		{
			return (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
		}

		private Color4 ToDxColor(WpfBrush b, float alphaMult)
		{
			var c = BrushToMediaColor(b);
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alphaMult);
		}
		#endregion

		string IOrcaReplayParticipant.ReplayParticipantId { get { return replayBarHorizon == null ? "OrcaCandleVolumeProfile:" + sharedSourceId.ToString("N") : replayBarHorizon.ParticipantId; } }
		OrcaReplayCapabilities IOrcaReplayParticipant.ReplayCapabilities { get { return replayBarHorizon == null ? new OrcaReplayCapabilities(true, false, true, true, OrcaReplayChartStyleSupport.AllV1) : replayBarHorizon.Capabilities; } }
		OrcaReplayCheckpoint IOrcaReplayParticipant.CaptureReplayCheckpoint(OrcaReplayContext context) { return replayBarHorizon.Capture(context); }
		void IOrcaReplayParticipant.PrepareReplay(OrcaReplayContext context, OrcaReplayCheckpoint checkpoint) { replayBarHorizon.Prepare(context, checkpoint); }
		void IOrcaReplayParticipant.ApplyReplayEvent(OrcaReplayContext context, OrcaReplayTradeEvent tradeEvent) { }
		void IOrcaReplayParticipant.ApplyReplayBar(OrcaReplayContext context, int primaryBarIndex) { replayBarHorizon.ApplyBar(primaryBarIndex); }
		void IOrcaReplayParticipant.PublishReplaySnapshot(OrcaReplayContext context) { }
		void IOrcaReplayParticipant.RestoreLiveState() { if (replayBarHorizon != null) replayBarHorizon.Restore(); }

		#region Properties
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Enhanced Footprint", Description = "Opt-in Bid x Ask renderer. Disable to restore the legacy appearance. Other display modes are unchanged.", GroupName = "01 Display", Order = 2)]
		public bool EnhancedFootprint { get; set; }

		[Browsable(false)]
		public int FootprintSettingsVersion { get; set; }

		[Range(0, 500)]
		[Display(Name = "Analysis Row Size (ticks)", Description = "Independent of display zoom. Zero initializes from Order Flow Row Size on first enable.", GroupName = "04 Rows & Scaling", Order = 10)]
		public int FootprintAnalysisTicks { get; set; }

		[RefreshProperties(RefreshProperties.All)]
		[TypeConverter(typeof(FootprintScaleModeConverter))]
		[Display(Name = "Normalization", Description = "Enhanced Bid x Ask scaling: compare rows within each bar, across visible bars, within the session, or against a fixed contract count.", GroupName = "04 Rows & Scaling", Order = 11)]
		public FootprintScaleMode FootprintScale { get; set; }

		[Range(0, long.MaxValue)]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Fixed Scale Volume (contracts)", Description = "Positive common bid/ask denominator; zero leaves Fixed unavailable. Cluster uses the same count for total-row intensity.", GroupName = "04 Rows & Scaling", Order = 12)]
		public long FootprintFixedVolume { get; set; }

		[TypeConverter(typeof(FootprintScaffoldModeConverter))]
		[Display(Name = "Candle Display", Description = "Off hides the center candle, OHLC Spine draws a narrow spine, Full Candle draws the configured candle, and Hollow Body Delta shows every row delta with boxed body rows and free-floating wick rows.", GroupName = "03 Candles", Order = 0)]
		public FootprintScaffoldMode FootprintScaffold { get; set; }

		[TypeConverter(typeof(FootprintCellViewConverter))]
		[Display(Name = "Cell Values", Description = "Choose the numbers shown in enhanced Bid x Ask cells: Bid x Ask, total volume, strict delta, or strict delta percentage.", GroupName = "10 Text - Bid x Ask", Order = 1)]
		public FootprintCellView FootprintValues { get; set; }

		[Display(Name = "Number Format", Description = "Enhanced Bid x Ask number format. Auto uses full numbers where they fit, then compact notation.", GroupName = "10 Text - Bid x Ask", Order = 2)]
		public FootprintNumberFormat FootprintNumbers { get; set; }

		[Range(4, 40)]
		[Display(Name = "Bid/Ask Column Gap (px)", Description = "Space in pixels between the two number columns in enhanced Bid x Ask mode.", GroupName = "10 Text - Bid x Ask", Order = 6)]
		public int FootprintGutterPx { get; set; }

		[Display(Name = "Show Data Health", Description = "Coverage, classification and scaling status. Exact quality remains available on hover.", GroupName = "11 Advanced - Data", Order = 2)]
		public bool FootprintShowHealth { get; set; }

		[Display(Name = "Emphasize Dominant Bid/Ask", Description = "Uses a heavier font weight for the Bid or Ask side that meets Minimum Dominance Ratio.", GroupName = "10 Text - Bid x Ask", Order = 7)]
		public bool FootprintEmphasizeWinner { get; set; }

		[Range(1.0, 10.0)]
		[Display(Name = "Minimum Dominance Ratio", Description = "Same-row Bid/Ask ratio required to emphasize the dominant side. Example: 1.5 means 150% of the opposing side.", GroupName = "10 Text - Bid x Ask", Order = 8)]
		public double FootprintWinnerRatio { get; set; }

		// --- Profile Setup ---
		[NinjaScriptProperty]
		[TypeConverter(typeof(CandleProfileDisplayModeConverter))]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Profile Display", Description = "Choose Volume, Delta, both profiles, or the Bid x Ask footprint.", GroupName = "01 Display", Order = 0)]
		public CandleProfileDisplayMode ProfileDisplayMode { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(CandleProfileBidAskStyleConverter))]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Bid x Ask Style", Description = "Cluster draws fixed-width Bid x Ask cells. Histogram draws executed sells left and buys right.", GroupName = "01 Display", Order = 1)]
		public CandleProfileBidAskStyle BidAskStyle { get; set; }

		// --- Data ---
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Volume Row Size (ticks)", Description = "Fixed volume row height when dynamic volume aggregation is off.", GroupName = "04 Rows & Scaling", Order = 0)]
		public int TickCompression { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Volume Aggregation", Description = "Gently increases volume row height when the visible price range is large.", GroupName = "04 Rows & Scaling", Order = 1)]
		public bool UseDynamicVolumeAggregation { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "Volume Dynamic Multiplier", Description = "Lower values keep volume rows more granular; higher values aggregate sooner.", GroupName = "04 Rows & Scaling", Order = 2)]
		public double VolumeDynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Max Dynamic Volume Ticks", Description = "Upper cap for dynamic volume row height.", GroupName = "04 Rows & Scaling", Order = 3)]
		public int MaxDynamicVolumeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Order Flow Row Size (ticks)", Description = "Fixed row height for Delta and Bid x Ask when dynamic order-flow aggregation is off.", GroupName = "04 Rows & Scaling", Order = 4)]
		public int DeltaTickCompression { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Dynamic Order Flow Aggregation", Description = "Dynamically increases Delta and Bid x Ask row height as the visible price range expands.", GroupName = "04 Rows & Scaling", Order = 5)]
		public bool UseDynamicDeltaAggregation { get; set; }

		[NinjaScriptProperty]
		[Range(2, 40)]
		[Display(Name = "Order Flow Row Min Pixels", Description = "Target minimum Delta and Bid x Ask row height before applying the multiplier.", GroupName = "04 Rows & Scaling", Order = 6)]
		public int DeltaDynamicRowMinPixels { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "Order Flow Dynamic Multiplier", Description = "Lower values keep order-flow rows more granular; higher values aggregate sooner.", GroupName = "04 Rows & Scaling", Order = 7)]
		public double DeltaDynamicMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Dynamic Order Flow Min Ticks", GroupName = "04 Rows & Scaling", Order = 8)]
		public int DynamicDeltaMinCompression { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Dynamic Order Flow Max Ticks", GroupName = "04 Rows & Scaling", Order = 9)]
		public int DynamicDeltaMaxCompression { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Publish Shared Profile Cache", Description = "Publishes raw 1-tick volume/delta maps for Fixed Range and other Orca tools. Best enabled on one rich/tick-replay CVP per chart key; disable weaker duplicates.", GroupName = "11 Advanced - Data", Order = 1)]
		public bool PublishSharedProfileCache { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Trade Source Mode", Description = "Secondary Tick Series is the fast legacy path but may lack historical quotes. For historical Bid x Ask, use Tick Replay Last Events on a chart with Tick Replay enabled.", GroupName = "11 Advanced - Data", Order = 0)]
		public CandleProfileTradeSourceMode TradeSourceMode { get; set; }

		// --- Layout ---
		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "Candle Width (px)", GroupName = "03 Candles", Order = 1)]
		public int CandleWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Volume Profile Width (px)", GroupName = "02 Profile Layout", Order = 0)]
		public int ProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Delta Profile Width (px)", GroupName = "02 Profile Layout", Order = 1)]
		public int DeltaProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(24, 500)]
		[Display(Name = "Bid x Ask Width (px)", Description = "Total centered width of Bid x Ask cells; capped to avoid overlapping neighboring candles.", GroupName = "02 Profile Layout", Order = 2)]
		public int BidAskWidthPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Volume + Delta Arrangement", Description = "Choose which side of the candle gets volume versus delta when both are displayed.", GroupName = "01 Display", Order = 3)]
		public CandleProfileSideArrangement ProfileArrangement { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Volume/Delta Width", Description = "Dynamically adjusts side-profile width to fit between candles.", GroupName = "02 Profile Layout", Order = 3)]
		public bool DynamicProfileWidth { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Volume/Delta Width Scale", Description = "Scales side-profile width after dynamic sizing. 0.90 uses 90% of the available space.", GroupName = "02 Profile Layout", Order = 4)]
		public double ProfileWidthScale { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Volume + Delta Width Scale", Description = "Scales each side when volume and delta are both visible. Lower values leave more room between candles.", GroupName = "02 Profile Layout", Order = 5)]
		public double DualProfileWidthScale { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Auto Hide Profiles When Compressed", Description = "Hide per-candle profiles when visible bar spacing gets too tight, while still drawing candles.", GroupName = "02 Profile Layout", Order = 8)]
		public bool AutoHideProfilesWhenCompressed { get; set; }

		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "Minimum Bar Spacing for Profiles (px)", Description = "Profiles hide when average visible bar spacing is below this many pixels.", GroupName = "02 Profile Layout", Order = 9)]
		public int MinBarSpacingToShowProfilesPx { get; set; }

		[NinjaScriptProperty]
		[Range(1, 30)]
		[Display(Name = "Compressed Candle Maximum Width (px)", Description = "Maximum CVP candle width while profiles are auto-hidden. Actual width follows the chart bar width so Alt+Up/Down still works.", GroupName = "03 Candles", Order = 5)]
		public int CompressedCandleWidthPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Absorption Colors For Candles", Description = "When OrcaAbsorptionCandles is on the chart, use its delta-intensity colors for CVP candle bodies.", GroupName = "03 Candles", Order = 8)]
		public bool UseAbsorptionColorsWhenCompressed { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Absorption Candle Minimum Width (px)", Description = "Minimum CVP candle body width when using absorption colors. Wick width remains controlled by Wick Width.", GroupName = "03 Candles", Order = 9)]
		public int AbsorptionCandleMinWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Candle-Profile Gap (px)", GroupName = "02 Profile Layout", Order = 6)]
		public int CandleProfileGapPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Profile Row Spacing (px)", GroupName = "02 Profile Layout", Order = 7)]
		public int ProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Range(1, 6)]
		[Display(Name = "Wick Width (px)", GroupName = "03 Candles", Order = 2)]
		public int WickWidthPx { get; set; }

		// --- Visibility ---
		[NinjaScriptProperty]
		[Browsable(false)]
		public bool ShowVolumeProfile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show POC", Description = "Highlight the point of control, the highest-volume price row. Enhanced Bid x Ask uses an outline based on independent analysis rows.", GroupName = "06 POC & Value Area", Order = 0)]
		public bool ShowPOC { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Color Volume By Delta", Description = "Colors the volume profile rows by row delta. Turn this off to keep volume in its volume color while the separate delta profile uses delta colors.", GroupName = "05 Profile Colors", Order = 2)]
		public bool ShowDelta { get; set; }

		[NinjaScriptProperty]
		[Browsable(false)]
		public bool ShowDeltaProfile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Gradient", GroupName = "05 Profile Colors", Order = 3)]
		public bool UseGradient { get; set; }

		[NinjaScriptProperty]
		[Range(2, 64)]
		[Display(Name = "Gradient Steps", GroupName = "05 Profile Colors", Order = 5)]
		public int GradientSteps { get; set; }

		// --- Text Labels ---
		[NinjaScriptProperty]
		[Display(Name = "Show Delta Text", GroupName = "09 Text - Delta", Order = 0)]
		public bool ShowDeltaText { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000000)]
		[Display(Name = "Minimum Absolute Delta to Show Text", Description = "Minimum absolute delta needed before drawing the delta label.", GroupName = "09 Text - Delta", Order = 1)]
		public int DeltaTextMinThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(6, 24)]
		[Display(Name = "Delta Text Font Size", Description = "Fixed delta label size, or the base size when dynamic text sizing is enabled.", GroupName = "09 Text - Delta", Order = 2)]
		public float DeltaTextFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Delta Text Color", GroupName = "09 Text - Delta", Order = 3)]
		public WpfBrush DeltaTextBrush { get; set; }
		[Browsable(false)]
		public string DeltaTextBrushSerialize
		{ get { return Serialize.BrushToString(DeltaTextBrush); } set { DeltaTextBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Volume Text", GroupName = "08 Text - Volume", Order = 0)]
		public bool ShowVolumeText { get; set; }

		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Scale Volume Text Brightness", Description = "In Volume + Delta mode, makes the largest displayed volume row brightest and fades smaller volume numbers within each candle.", GroupName = "08 Text - Volume", Order = 4)]
		public bool ScaleVolumeTextBrightnessByVolume { get; set; }

		[Range(0.05, 1.0)]
		[Display(Name = "Volume Text Minimum Brightness", Description = "Minimum opacity used for lower-volume numbers when volume text brightness scaling is enabled.", GroupName = "08 Text - Volume", Order = 5)]
		public float VolumeTextMinBrightness { get; set; }

		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Emphasize Volume Rows by Delta", Description = "In Volume-only mode, colors profile rows that meet both the absolute-delta and delta-percent thresholds while keeping volume text in its selected color.", GroupName = "08 Text - Volume", Order = 6)]
		public bool ColorVolumeTextByDelta { get; set; }

		[Browsable(false)]
		public bool ScaleVolumeTextColorByDeltaPercent { get; set; }

		[Range(0, 1000000)]
		[Display(Name = "Minimum Absolute Delta", Description = "Minimum absolute row delta required before the volume bar is emphasized.", GroupName = "08 Text - Volume", Order = 7)]
		public int VolumeTextDeltaMinAbsolute { get; set; }

		[Range(0.0, 100.0)]
		[Display(Name = "Minimum Delta %", Description = "Minimum absolute row delta as a percentage of row volume required for emphasis.", GroupName = "08 Text - Volume", Order = 8)]
		public double VolumeTextDeltaMinPercent { get; set; }

		[Browsable(false)]
		[Display(Name = "Bold Qualified Text", Description = "Legacy saved value retained for template compatibility.", GroupName = "08 Text - Volume", Order = 9)]
		public bool BoldQualifiedVolumeText { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000000)]
		[Display(Name = "Minimum Volume to Show Text", Description = "Minimum row volume needed before drawing the volume label.", GroupName = "08 Text - Volume", Order = 1)]
		public int VolumeTextMinThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(6, 24)]
		[Display(Name = "Volume Text Font Size", Description = "Fixed volume label size, or the base size when dynamic text sizing is enabled.", GroupName = "08 Text - Volume", Order = 2)]
		public float VolumeTextFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Volume Text Color", GroupName = "08 Text - Volume", Order = 3)]
		public WpfBrush VolumeTextBrush { get; set; }
		[Browsable(false)]
		public string VolumeTextBrushSerialize
		{ get { return Serialize.BrushToString(VolumeTextBrush); } set { VolumeTextBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Bid x Ask Text", GroupName = "10 Text - Bid x Ask", Order = 0)]
		public bool ShowBidAskText { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000000)]
		[Display(Name = "Minimum Classified Volume to Show Text", Description = "Minimum classified row volume needed before drawing Bid x Ask values.", GroupName = "10 Text - Bid x Ask", Order = 3)]
		public int BidAskTextMinThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(6, 24)]
		[Display(Name = "Bid x Ask Text Font Size", Description = "Fixed footprint label size, or the base size when dynamic text sizing is enabled.", GroupName = "10 Text - Bid x Ask", Order = 4)]
		public float BidAskTextFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Bid x Ask Text Color", GroupName = "10 Text - Bid x Ask", Order = 5)]
		public WpfBrush BidAskTextBrush { get; set; }
		[Browsable(false)]
		public string BidAskTextBrushSerialize
		{ get { return Serialize.BrushToString(BidAskTextBrush); } set { BidAskTextBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[TypeConverter(typeof(CandleProfileTextFontFamilyConverter))]
		[Display(Name = "Text Font Family", Description = "Font used for Volume, Delta, and Bid x Ask labels.", GroupName = "07 Text - General", Order = 0)]
		public string TextFontFamily { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Text Font Weight", Description = "Weight used for Volume, Delta, and Bid x Ask labels.", GroupName = "07 Text - General", Order = 1)]
		public CandleProfileTextFontWeight TextFontWeight { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Auto-Size Text", Description = "Lets Volume, Delta and Bid x Ask labels grow with row height, up to Maximum Text Size.", GroupName = "07 Text - General", Order = 2)]
		public bool UseDynamicTextSizing { get; set; }

		[NinjaScriptProperty]
		[Range(6, 32)]
		[Display(Name = "Maximum Text Size", Description = "Largest font size dynamic labels are allowed to use.", GroupName = "07 Text - General", Order = 3)]
		public float DynamicTextMaxFontSize { get; set; }

		// --- Value Area ---
		[NinjaScriptProperty]
		[Display(Name = "Show Value Area", GroupName = "06 POC & Value Area", Order = 2)]
		public bool ShowValueArea { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Shade Value Area", Description = "Color rows inside the Value Area differently", GroupName = "06 POC & Value Area", Order = 4)]
		public bool ShowVAColor { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Value Area Boundaries", Description = "Draw the upper and lower value-area boundaries using the selected line color, width and style.", GroupName = "06 POC & Value Area", Order = 6)]
		public bool ShowVALines { get; set; }

		[NinjaScriptProperty]
		[Range(50, 95)]
		[Display(Name = "Value Area (%)", Description = "Percentage of each candle profile volume included in its value area.", GroupName = "06 POC & Value Area", Order = 3)]
		public int ValueAreaPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 6.0)]
		[Display(Name = "Boundary Line Width (px)", GroupName = "06 POC & Value Area", Order = 8)]
		public float VALineThickness { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Boundary Line Style", GroupName = "06 POC & Value Area", Order = 9)]
		public VALineStyleEnum VALineStyle { get; set; }

		[XmlIgnore]
		[Display(Name = "Value Area Color", GroupName = "06 POC & Value Area", Order = 5)]
		public WpfBrush VABrush { get; set; }
		[Browsable(false)]
		public string VABrushSerialize
		{ get { return Serialize.BrushToString(VABrush); } set { VABrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Boundary Line Color", GroupName = "06 POC & Value Area", Order = 7)]
		public WpfBrush VALineBrush { get; set; }
		[Browsable(false)]
		public string VALineBrushSerialize
		{ get { return Serialize.BrushToString(VALineBrush); } set { VALineBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Candles ---
		[XmlIgnore]
		[Display(Name = "Bullish Body", GroupName = "03 Candles", Order = 3)]
		public WpfBrush BullishBodyBrush { get; set; }
		[Browsable(false)]
		public string BullishBodyBrushSerialize
		{ get { return Serialize.BrushToString(BullishBodyBrush); } set { BullishBodyBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Body", GroupName = "03 Candles", Order = 4)]
		public WpfBrush BearishBodyBrush { get; set; }
		[Browsable(false)]
		public string BearishBodyBrushSerialize
		{ get { return Serialize.BrushToString(BearishBodyBrush); } set { BearishBodyBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Compressed Bullish Body", Description = "Body color used only when auto-hide profiles is active and profiles are hidden.", GroupName = "03 Candles", Order = 6)]
		public WpfBrush CompressedBullishBodyBrush { get; set; }
		[Browsable(false)]
		public string CompressedBullishBodyBrushSerialize
		{ get { return Serialize.BrushToString(CompressedBullishBodyBrush); } set { CompressedBullishBodyBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Compressed Bearish Body", Description = "Body color used only when auto-hide profiles is active and profiles are hidden.", GroupName = "03 Candles", Order = 7)]
		public WpfBrush CompressedBearishBodyBrush { get; set; }
		[Browsable(false)]
		public string CompressedBearishBodyBrushSerialize
		{ get { return Serialize.BrushToString(CompressedBearishBodyBrush); } set { CompressedBearishBodyBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Profile ---
		[XmlIgnore]
		[Display(Name = "Volume Color", GroupName = "05 Profile Colors", Order = 0)]
		public WpfBrush VolumeBrush { get; set; }
		[Browsable(false)]
		public string VolumeBrushSerialize
		{ get { return Serialize.BrushToString(VolumeBrush); } set { VolumeBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.05, 1.0)]
		[Display(Name = "Minimum Profile Brightness", GroupName = "05 Profile Colors", Order = 4)]
		public float MinBrightness { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Volume Opacity", GroupName = "05 Profile Colors", Order = 1)]
		public float VolumeOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Color", GroupName = "06 POC & Value Area", Order = 1)]
		public WpfBrush POCBrush { get; set; }
		[Browsable(false)]
		public string POCBrushSerialize
		{ get { return Serialize.BrushToString(POCBrush); } set { POCBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Delta ---
		[XmlIgnore]
		[Display(Name = "Positive Delta", GroupName = "05 Profile Colors", Order = 6)]
		public WpfBrush PositiveDeltaBrush { get; set; }
		[Browsable(false)]
		public string PositiveDeltaBrushSerialize
		{ get { return Serialize.BrushToString(PositiveDeltaBrush); } set { PositiveDeltaBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Negative Delta", GroupName = "05 Profile Colors", Order = 7)]
		public WpfBrush NegativeDeltaBrush { get; set; }
		[Browsable(false)]
		public string NegativeDeltaBrushSerialize
		{ get { return Serialize.BrushToString(NegativeDeltaBrush); } set { NegativeDeltaBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Delta Opacity", GroupName = "05 Profile Colors", Order = 8)]
		public float DeltaOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Delta Intensity Color", Description = "Scales delta row opacity by absolute delta, similar to absorption candle intensity.", GroupName = "05 Profile Colors", Order = 9)]
		public bool UseDeltaIntensityColoring { get; set; }

		[NinjaScriptProperty]
		[Range(0.05, 1.0)]
		[Display(Name = "Delta Intensity Min Opacity", Description = "Minimum opacity used for the weakest visible delta rows.", GroupName = "05 Profile Colors", Order = 10)]
		public float DeltaIntensityMinOpacity { get; set; }

		// --- Colors: Bid x Ask ---
		[XmlIgnore]
		[Display(Name = "Bid x Ask Positive", GroupName = "05 Profile Colors", Order = 11)]
		public WpfBrush BidAskPositiveBrush { get; set; }
		[Browsable(false)]
		public string BidAskPositiveBrushSerialize
		{ get { return Serialize.BrushToString(BidAskPositiveBrush); } set { BidAskPositiveBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bid x Ask Negative", GroupName = "05 Profile Colors", Order = 12)]
		public WpfBrush BidAskNegativeBrush { get; set; }
		[Browsable(false)]
		public string BidAskNegativeBrushSerialize
		{ get { return Serialize.BrushToString(BidAskNegativeBrush); } set { BidAskNegativeBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bid x Ask Neutral", GroupName = "05 Profile Colors", Order = 13)]
		public WpfBrush BidAskNeutralBrush { get; set; }
		[Browsable(false)]
		public string BidAskNeutralBrushSerialize
		{ get { return Serialize.BrushToString(BidAskNeutralBrush); } set { BidAskNeutralBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.05, 1.0)]
		[Display(Name = "Bid x Ask Min Opacity", Description = "Opacity used for the weakest footprint rows.", GroupName = "05 Profile Colors", Order = 14)]
		public float BidAskMinOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Bid x Ask Max Opacity", Description = "Opacity used for the strongest footprint rows.", GroupName = "05 Profile Colors", Order = 15)]
		public float BidAskMaxOpacity { get; set; }

		#endregion
	}

    // Keep the IndicatorBaseConverter with the actual indicator, not in a helper partial file.
    public class OrcaFootprintSettingsConverter : IndicatorBaseConverter
    {
        private static readonly HashSet<string> applicable = new HashSet<string>(StringComparer.Ordinal)
        {
            "ProfileDisplayMode", "EnhancedFootprint", "BidAskStyle", "BidAskWidthPx", "TradeSourceMode", "PublishSharedProfileCache",
            "DeltaTickCompression", "UseDynamicDeltaAggregation", "DeltaDynamicRowMinPixels", "DeltaDynamicMultiplier",
            "DynamicDeltaMinCompression", "DynamicDeltaMaxCompression", "ProfileBarSpacingPx", "ShowPOC", "ShowBidAskText",
            "BidAskTextMinThreshold", "BidAskTextFontSize", "BidAskTextBrush", "TextFontFamily", "TextFontWeight",
            "UseDynamicTextSizing", "DynamicTextMaxFontSize", "BidAskPositiveBrush", "BidAskNegativeBrush", "BidAskNeutralBrush",
            "BidAskMinOpacity", "BidAskMaxOpacity", "FootprintEmphasizeWinner", "FootprintWinnerRatio"
        };
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) { return true; }
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            PropertyDescriptorCollection properties = base.GetPropertiesSupported(context) ? base.GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes);
            var indicator = value as OrcaCandleVolumeProfile;
            if (indicator == null || properties == null) return properties;
            var output = new List<PropertyDescriptor>();
            bool bidAsk = indicator.ProfileDisplayMode == CandleProfileDisplayMode.BidAsk;
            bool enhanced = indicator.EnhancedFootprint && bidAsk;
            bool volume = indicator.ProfileDisplayMode == CandleProfileDisplayMode.Volume
                || indicator.ProfileDisplayMode == CandleProfileDisplayMode.VolumeAndDelta;
            bool delta = indicator.ProfileDisplayMode == CandleProfileDisplayMode.Delta
                || indicator.ProfileDisplayMode == CandleProfileDisplayMode.VolumeAndDelta;
			foreach (PropertyDescriptor property in properties)
			{
				string name = property.Name;
                var settingDisplay = property.Attributes[typeof(DisplayAttribute)] as DisplayAttribute;
                string settingGroup = settingDisplay == null ? null : settingDisplay.GetGroupName();
                if (settingGroup == "08 Text - Volume" && !volume) continue;
                if (settingGroup == "09 Text - Delta" && !delta) continue;
                if (settingGroup == "10 Text - Bid x Ask" && !bidAsk) continue;
                if ((name == "BidAskStyle" || name == "EnhancedFootprint" || name == "BidAskWidthPx") && !bidAsk) continue;
                if (name == "ProfileArrangement" && indicator.ProfileDisplayMode != CandleProfileDisplayMode.VolumeAndDelta) continue;
				if (name == "ScaleVolumeTextColorByDeltaPercent") continue;
				bool volumeAndDeltaTextSetting = name == "ScaleVolumeTextBrightnessByVolume" || name == "VolumeTextMinBrightness";
				if (volumeAndDeltaTextSetting && indicator.ProfileDisplayMode != CandleProfileDisplayMode.VolumeAndDelta) continue;
				if (name == "VolumeTextMinBrightness" && !indicator.ScaleVolumeTextBrightnessByVolume) continue;
				bool volumeOnlyTextSetting = name == "ColorVolumeTextByDelta"
					|| name == "VolumeTextDeltaMinAbsolute"
					|| name == "VolumeTextDeltaMinPercent";
				if (volumeOnlyTextSetting && indicator.ProfileDisplayMode != CandleProfileDisplayMode.Volume) continue;
				if (name != "ColorVolumeTextByDelta" && volumeOnlyTextSetting && !indicator.ColorVolumeTextByDelta) continue;
                bool sharedBidAskSetting = name == "FootprintEmphasizeWinner" || name == "FootprintWinnerRatio" || name == "FootprintScaffold";
                if (!enhanced && name.StartsWith("Footprint", StringComparison.Ordinal) && !(bidAsk && sharedBidAskSetting)) continue;
                if (enhanced)
                {
                    DisplayAttribute display = property.Attributes[typeof(DisplayAttribute)] as DisplayAttribute;
                    var info = typeof(OrcaCandleVolumeProfile).GetProperty(name);
                    bool owned = info != null && info.DeclaringType == typeof(OrcaCandleVolumeProfile);
                    if (!owned || display == null) { output.Add(property); continue; }
                    if (!name.StartsWith("Footprint", StringComparison.Ordinal) && !applicable.Contains(name)) continue;
                    if (name == "BidAskMinOpacity" && indicator.BidAskStyle == CandleProfileBidAskStyle.Histogram) continue;
                    if (!indicator.UseDynamicDeltaAggregation && (name.StartsWith("DynamicDelta", StringComparison.Ordinal) || name.StartsWith("DeltaDynamic", StringComparison.Ordinal))) continue;
                    string group = display.GetGroupName(), label = display.GetName();
                    if (name == "ShowPOC") label = "POC Outline";
                    if (name == "DeltaTickCompression") label = "Display Row Size (ticks)";
                    if (name == "BidAskPositiveBrush") label = "Ask / Positive Delta Color";
                    if (name == "BidAskNegativeBrush") label = "Bid / Negative Delta Color";
                    if (name == "BidAskTextMinThreshold") label = "Minimum Total Row Volume for Text";
                    output.Add(TypeDescriptor.CreateProperty(property.ComponentType, property,
                        new DisplayAttribute { Name = label, Description = display.GetDescription(), GroupName = group, Order = display.GetOrder() ?? 0 }));
                }
                else output.Add(property);
            }
            return new PropertyDescriptorCollection(output.ToArray(), true);
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaCandleVolumeProfile[] cacheOrcaCandleVolumeProfile;
		public OrcaCandleVolumeProfile OrcaCandleVolumeProfile(int tickCompression, bool useDynamicVolumeAggregation, double volumeDynamicAggregationMultiplier, int maxDynamicVolumeTicks, int deltaTickCompression, bool useDynamicDeltaAggregation, int deltaDynamicRowMinPixels, double deltaDynamicMultiplier, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool publishSharedProfileCache, CandleProfileTradeSourceMode tradeSourceMode, int candleWidthPx, int profileWidthPx, int deltaProfileWidthPx, CandleProfileSideArrangement profileArrangement, bool dynamicProfileWidth, double profileWidthScale, double dualProfileWidthScale, bool autoHideProfilesWhenCompressed, int minBarSpacingToShowProfilesPx, int compressedCandleWidthPx, bool useAbsorptionColorsWhenCompressed, int absorptionCandleMinWidthPx, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showVolumeProfile, bool showPOC, bool showDelta, bool showDeltaProfile, bool useGradient, int gradientSteps, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize, bool showVolumeText, int volumeTextMinThreshold, float volumeTextFontSize, string textFontFamily, CandleProfileTextFontWeight textFontWeight, bool useDynamicTextSizing, float dynamicTextMaxFontSize, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return OrcaCandleVolumeProfile(Input, tickCompression, useDynamicVolumeAggregation, volumeDynamicAggregationMultiplier, maxDynamicVolumeTicks, deltaTickCompression, useDynamicDeltaAggregation, deltaDynamicRowMinPixels, deltaDynamicMultiplier, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, publishSharedProfileCache, tradeSourceMode, candleWidthPx, profileWidthPx, deltaProfileWidthPx, profileArrangement, dynamicProfileWidth, profileWidthScale, dualProfileWidthScale, autoHideProfilesWhenCompressed, minBarSpacingToShowProfilesPx, compressedCandleWidthPx, useAbsorptionColorsWhenCompressed, absorptionCandleMinWidthPx, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showVolumeProfile, showPOC, showDelta, showDeltaProfile, useGradient, gradientSteps, showDeltaText, deltaTextMinThreshold, deltaTextFontSize, showVolumeText, volumeTextMinThreshold, volumeTextFontSize, textFontFamily, textFontWeight, useDynamicTextSizing, dynamicTextMaxFontSize, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}

		public OrcaCandleVolumeProfile OrcaCandleVolumeProfile(ISeries<double> input, int tickCompression, bool useDynamicVolumeAggregation, double volumeDynamicAggregationMultiplier, int maxDynamicVolumeTicks, int deltaTickCompression, bool useDynamicDeltaAggregation, int deltaDynamicRowMinPixels, double deltaDynamicMultiplier, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool publishSharedProfileCache, CandleProfileTradeSourceMode tradeSourceMode, int candleWidthPx, int profileWidthPx, int deltaProfileWidthPx, CandleProfileSideArrangement profileArrangement, bool dynamicProfileWidth, double profileWidthScale, double dualProfileWidthScale, bool autoHideProfilesWhenCompressed, int minBarSpacingToShowProfilesPx, int compressedCandleWidthPx, bool useAbsorptionColorsWhenCompressed, int absorptionCandleMinWidthPx, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showVolumeProfile, bool showPOC, bool showDelta, bool showDeltaProfile, bool useGradient, int gradientSteps, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize, bool showVolumeText, int volumeTextMinThreshold, float volumeTextFontSize, string textFontFamily, CandleProfileTextFontWeight textFontWeight, bool useDynamicTextSizing, float dynamicTextMaxFontSize, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			if (cacheOrcaCandleVolumeProfile != null)
				for (int idx = 0; idx < cacheOrcaCandleVolumeProfile.Length; idx++)
					if (cacheOrcaCandleVolumeProfile[idx] != null && cacheOrcaCandleVolumeProfile[idx].TickCompression == tickCompression && cacheOrcaCandleVolumeProfile[idx].UseDynamicVolumeAggregation == useDynamicVolumeAggregation && cacheOrcaCandleVolumeProfile[idx].VolumeDynamicAggregationMultiplier == volumeDynamicAggregationMultiplier && cacheOrcaCandleVolumeProfile[idx].MaxDynamicVolumeTicks == maxDynamicVolumeTicks && cacheOrcaCandleVolumeProfile[idx].DeltaTickCompression == deltaTickCompression && cacheOrcaCandleVolumeProfile[idx].UseDynamicDeltaAggregation == useDynamicDeltaAggregation && cacheOrcaCandleVolumeProfile[idx].DeltaDynamicRowMinPixels == deltaDynamicRowMinPixels && cacheOrcaCandleVolumeProfile[idx].DeltaDynamicMultiplier == deltaDynamicMultiplier && cacheOrcaCandleVolumeProfile[idx].DynamicDeltaMinCompression == dynamicDeltaMinCompression && cacheOrcaCandleVolumeProfile[idx].DynamicDeltaMaxCompression == dynamicDeltaMaxCompression && cacheOrcaCandleVolumeProfile[idx].PublishSharedProfileCache == publishSharedProfileCache && cacheOrcaCandleVolumeProfile[idx].TradeSourceMode == tradeSourceMode && cacheOrcaCandleVolumeProfile[idx].CandleWidthPx == candleWidthPx && cacheOrcaCandleVolumeProfile[idx].ProfileWidthPx == profileWidthPx && cacheOrcaCandleVolumeProfile[idx].DeltaProfileWidthPx == deltaProfileWidthPx && cacheOrcaCandleVolumeProfile[idx].ProfileArrangement == profileArrangement && cacheOrcaCandleVolumeProfile[idx].DynamicProfileWidth == dynamicProfileWidth && cacheOrcaCandleVolumeProfile[idx].ProfileWidthScale == profileWidthScale && cacheOrcaCandleVolumeProfile[idx].DualProfileWidthScale == dualProfileWidthScale && cacheOrcaCandleVolumeProfile[idx].AutoHideProfilesWhenCompressed == autoHideProfilesWhenCompressed && cacheOrcaCandleVolumeProfile[idx].MinBarSpacingToShowProfilesPx == minBarSpacingToShowProfilesPx && cacheOrcaCandleVolumeProfile[idx].CompressedCandleWidthPx == compressedCandleWidthPx && cacheOrcaCandleVolumeProfile[idx].UseAbsorptionColorsWhenCompressed == useAbsorptionColorsWhenCompressed && cacheOrcaCandleVolumeProfile[idx].AbsorptionCandleMinWidthPx == absorptionCandleMinWidthPx && cacheOrcaCandleVolumeProfile[idx].CandleProfileGapPx == candleProfileGapPx && cacheOrcaCandleVolumeProfile[idx].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaCandleVolumeProfile[idx].WickWidthPx == wickWidthPx && cacheOrcaCandleVolumeProfile[idx].ShowVolumeProfile == showVolumeProfile && cacheOrcaCandleVolumeProfile[idx].ShowPOC == showPOC && cacheOrcaCandleVolumeProfile[idx].ShowDelta == showDelta && cacheOrcaCandleVolumeProfile[idx].ShowDeltaProfile == showDeltaProfile && cacheOrcaCandleVolumeProfile[idx].UseGradient == useGradient && cacheOrcaCandleVolumeProfile[idx].GradientSteps == gradientSteps && cacheOrcaCandleVolumeProfile[idx].ShowDeltaText == showDeltaText && cacheOrcaCandleVolumeProfile[idx].DeltaTextMinThreshold == deltaTextMinThreshold && cacheOrcaCandleVolumeProfile[idx].DeltaTextFontSize == deltaTextFontSize && cacheOrcaCandleVolumeProfile[idx].ShowVolumeText == showVolumeText && cacheOrcaCandleVolumeProfile[idx].VolumeTextMinThreshold == volumeTextMinThreshold && cacheOrcaCandleVolumeProfile[idx].VolumeTextFontSize == volumeTextFontSize && cacheOrcaCandleVolumeProfile[idx].TextFontFamily == textFontFamily && cacheOrcaCandleVolumeProfile[idx].TextFontWeight == textFontWeight && cacheOrcaCandleVolumeProfile[idx].UseDynamicTextSizing == useDynamicTextSizing && cacheOrcaCandleVolumeProfile[idx].DynamicTextMaxFontSize == dynamicTextMaxFontSize && cacheOrcaCandleVolumeProfile[idx].ShowValueArea == showValueArea && cacheOrcaCandleVolumeProfile[idx].ShowVAColor == showVAColor && cacheOrcaCandleVolumeProfile[idx].ShowVALines == showVALines && cacheOrcaCandleVolumeProfile[idx].ValueAreaPercent == valueAreaPercent && cacheOrcaCandleVolumeProfile[idx].VALineThickness == vALineThickness && cacheOrcaCandleVolumeProfile[idx].VALineStyle == vALineStyle && cacheOrcaCandleVolumeProfile[idx].MinBrightness == minBrightness && cacheOrcaCandleVolumeProfile[idx].VolumeOpacity == volumeOpacity && cacheOrcaCandleVolumeProfile[idx].DeltaOpacity == deltaOpacity && cacheOrcaCandleVolumeProfile[idx].UseDeltaIntensityColoring == useDeltaIntensityColoring && cacheOrcaCandleVolumeProfile[idx].DeltaIntensityMinOpacity == deltaIntensityMinOpacity && cacheOrcaCandleVolumeProfile[idx].EqualsInput(input))
						return cacheOrcaCandleVolumeProfile[idx];
			return CacheIndicator<OrcaCandleVolumeProfile>(new OrcaCandleVolumeProfile(){ TickCompression = tickCompression, UseDynamicVolumeAggregation = useDynamicVolumeAggregation, VolumeDynamicAggregationMultiplier = volumeDynamicAggregationMultiplier, MaxDynamicVolumeTicks = maxDynamicVolumeTicks, DeltaTickCompression = deltaTickCompression, UseDynamicDeltaAggregation = useDynamicDeltaAggregation, DeltaDynamicRowMinPixels = deltaDynamicRowMinPixels, DeltaDynamicMultiplier = deltaDynamicMultiplier, DynamicDeltaMinCompression = dynamicDeltaMinCompression, DynamicDeltaMaxCompression = dynamicDeltaMaxCompression, PublishSharedProfileCache = publishSharedProfileCache, TradeSourceMode = tradeSourceMode, CandleWidthPx = candleWidthPx, ProfileWidthPx = profileWidthPx, DeltaProfileWidthPx = deltaProfileWidthPx, ProfileArrangement = profileArrangement, DynamicProfileWidth = dynamicProfileWidth, ProfileWidthScale = profileWidthScale, DualProfileWidthScale = dualProfileWidthScale, AutoHideProfilesWhenCompressed = autoHideProfilesWhenCompressed, MinBarSpacingToShowProfilesPx = minBarSpacingToShowProfilesPx, CompressedCandleWidthPx = compressedCandleWidthPx, UseAbsorptionColorsWhenCompressed = useAbsorptionColorsWhenCompressed, AbsorptionCandleMinWidthPx = absorptionCandleMinWidthPx, CandleProfileGapPx = candleProfileGapPx, ProfileBarSpacingPx = profileBarSpacingPx, WickWidthPx = wickWidthPx, ShowVolumeProfile = showVolumeProfile, ShowPOC = showPOC, ShowDelta = showDelta, ShowDeltaProfile = showDeltaProfile, UseGradient = useGradient, GradientSteps = gradientSteps, ShowDeltaText = showDeltaText, DeltaTextMinThreshold = deltaTextMinThreshold, DeltaTextFontSize = deltaTextFontSize, ShowVolumeText = showVolumeText, VolumeTextMinThreshold = volumeTextMinThreshold, VolumeTextFontSize = volumeTextFontSize, TextFontFamily = textFontFamily, TextFontWeight = textFontWeight, UseDynamicTextSizing = useDynamicTextSizing, DynamicTextMaxFontSize = dynamicTextMaxFontSize, ShowValueArea = showValueArea, ShowVAColor = showVAColor, ShowVALines = showVALines, ValueAreaPercent = valueAreaPercent, VALineThickness = vALineThickness, VALineStyle = vALineStyle, MinBrightness = minBrightness, VolumeOpacity = volumeOpacity, DeltaOpacity = deltaOpacity, UseDeltaIntensityColoring = useDeltaIntensityColoring, DeltaIntensityMinOpacity = deltaIntensityMinOpacity }, input, ref cacheOrcaCandleVolumeProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaCandleVolumeProfile OrcaCandleVolumeProfile(int tickCompression, bool useDynamicVolumeAggregation, double volumeDynamicAggregationMultiplier, int maxDynamicVolumeTicks, int deltaTickCompression, bool useDynamicDeltaAggregation, int deltaDynamicRowMinPixels, double deltaDynamicMultiplier, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool publishSharedProfileCache, CandleProfileTradeSourceMode tradeSourceMode, int candleWidthPx, int profileWidthPx, int deltaProfileWidthPx, CandleProfileSideArrangement profileArrangement, bool dynamicProfileWidth, double profileWidthScale, double dualProfileWidthScale, bool autoHideProfilesWhenCompressed, int minBarSpacingToShowProfilesPx, int compressedCandleWidthPx, bool useAbsorptionColorsWhenCompressed, int absorptionCandleMinWidthPx, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showVolumeProfile, bool showPOC, bool showDelta, bool showDeltaProfile, bool useGradient, int gradientSteps, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize, bool showVolumeText, int volumeTextMinThreshold, float volumeTextFontSize, string textFontFamily, CandleProfileTextFontWeight textFontWeight, bool useDynamicTextSizing, float dynamicTextMaxFontSize, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return indicator.OrcaCandleVolumeProfile(Input, tickCompression, useDynamicVolumeAggregation, volumeDynamicAggregationMultiplier, maxDynamicVolumeTicks, deltaTickCompression, useDynamicDeltaAggregation, deltaDynamicRowMinPixels, deltaDynamicMultiplier, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, publishSharedProfileCache, tradeSourceMode, candleWidthPx, profileWidthPx, deltaProfileWidthPx, profileArrangement, dynamicProfileWidth, profileWidthScale, dualProfileWidthScale, autoHideProfilesWhenCompressed, minBarSpacingToShowProfilesPx, compressedCandleWidthPx, useAbsorptionColorsWhenCompressed, absorptionCandleMinWidthPx, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showVolumeProfile, showPOC, showDelta, showDeltaProfile, useGradient, gradientSteps, showDeltaText, deltaTextMinThreshold, deltaTextFontSize, showVolumeText, volumeTextMinThreshold, volumeTextFontSize, textFontFamily, textFontWeight, useDynamicTextSizing, dynamicTextMaxFontSize, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}

		public Indicators.OrcaCandleVolumeProfile OrcaCandleVolumeProfile(ISeries<double> input , int tickCompression, bool useDynamicVolumeAggregation, double volumeDynamicAggregationMultiplier, int maxDynamicVolumeTicks, int deltaTickCompression, bool useDynamicDeltaAggregation, int deltaDynamicRowMinPixels, double deltaDynamicMultiplier, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool publishSharedProfileCache, CandleProfileTradeSourceMode tradeSourceMode, int candleWidthPx, int profileWidthPx, int deltaProfileWidthPx, CandleProfileSideArrangement profileArrangement, bool dynamicProfileWidth, double profileWidthScale, double dualProfileWidthScale, bool autoHideProfilesWhenCompressed, int minBarSpacingToShowProfilesPx, int compressedCandleWidthPx, bool useAbsorptionColorsWhenCompressed, int absorptionCandleMinWidthPx, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showVolumeProfile, bool showPOC, bool showDelta, bool showDeltaProfile, bool useGradient, int gradientSteps, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize, bool showVolumeText, int volumeTextMinThreshold, float volumeTextFontSize, string textFontFamily, CandleProfileTextFontWeight textFontWeight, bool useDynamicTextSizing, float dynamicTextMaxFontSize, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return indicator.OrcaCandleVolumeProfile(input, tickCompression, useDynamicVolumeAggregation, volumeDynamicAggregationMultiplier, maxDynamicVolumeTicks, deltaTickCompression, useDynamicDeltaAggregation, deltaDynamicRowMinPixels, deltaDynamicMultiplier, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, publishSharedProfileCache, tradeSourceMode, candleWidthPx, profileWidthPx, deltaProfileWidthPx, profileArrangement, dynamicProfileWidth, profileWidthScale, dualProfileWidthScale, autoHideProfilesWhenCompressed, minBarSpacingToShowProfilesPx, compressedCandleWidthPx, useAbsorptionColorsWhenCompressed, absorptionCandleMinWidthPx, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showVolumeProfile, showPOC, showDelta, showDeltaProfile, useGradient, gradientSteps, showDeltaText, deltaTextMinThreshold, deltaTextFontSize, showVolumeText, volumeTextMinThreshold, volumeTextFontSize, textFontFamily, textFontWeight, useDynamicTextSizing, dynamicTextMaxFontSize, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaCandleVolumeProfile OrcaCandleVolumeProfile(int tickCompression, bool useDynamicVolumeAggregation, double volumeDynamicAggregationMultiplier, int maxDynamicVolumeTicks, int deltaTickCompression, bool useDynamicDeltaAggregation, int deltaDynamicRowMinPixels, double deltaDynamicMultiplier, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool publishSharedProfileCache, CandleProfileTradeSourceMode tradeSourceMode, int candleWidthPx, int profileWidthPx, int deltaProfileWidthPx, CandleProfileSideArrangement profileArrangement, bool dynamicProfileWidth, double profileWidthScale, double dualProfileWidthScale, bool autoHideProfilesWhenCompressed, int minBarSpacingToShowProfilesPx, int compressedCandleWidthPx, bool useAbsorptionColorsWhenCompressed, int absorptionCandleMinWidthPx, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showVolumeProfile, bool showPOC, bool showDelta, bool showDeltaProfile, bool useGradient, int gradientSteps, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize, bool showVolumeText, int volumeTextMinThreshold, float volumeTextFontSize, string textFontFamily, CandleProfileTextFontWeight textFontWeight, bool useDynamicTextSizing, float dynamicTextMaxFontSize, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return indicator.OrcaCandleVolumeProfile(Input, tickCompression, useDynamicVolumeAggregation, volumeDynamicAggregationMultiplier, maxDynamicVolumeTicks, deltaTickCompression, useDynamicDeltaAggregation, deltaDynamicRowMinPixels, deltaDynamicMultiplier, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, publishSharedProfileCache, tradeSourceMode, candleWidthPx, profileWidthPx, deltaProfileWidthPx, profileArrangement, dynamicProfileWidth, profileWidthScale, dualProfileWidthScale, autoHideProfilesWhenCompressed, minBarSpacingToShowProfilesPx, compressedCandleWidthPx, useAbsorptionColorsWhenCompressed, absorptionCandleMinWidthPx, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showVolumeProfile, showPOC, showDelta, showDeltaProfile, useGradient, gradientSteps, showDeltaText, deltaTextMinThreshold, deltaTextFontSize, showVolumeText, volumeTextMinThreshold, volumeTextFontSize, textFontFamily, textFontWeight, useDynamicTextSizing, dynamicTextMaxFontSize, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}

		public Indicators.OrcaCandleVolumeProfile OrcaCandleVolumeProfile(ISeries<double> input , int tickCompression, bool useDynamicVolumeAggregation, double volumeDynamicAggregationMultiplier, int maxDynamicVolumeTicks, int deltaTickCompression, bool useDynamicDeltaAggregation, int deltaDynamicRowMinPixels, double deltaDynamicMultiplier, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, bool publishSharedProfileCache, CandleProfileTradeSourceMode tradeSourceMode, int candleWidthPx, int profileWidthPx, int deltaProfileWidthPx, CandleProfileSideArrangement profileArrangement, bool dynamicProfileWidth, double profileWidthScale, double dualProfileWidthScale, bool autoHideProfilesWhenCompressed, int minBarSpacingToShowProfilesPx, int compressedCandleWidthPx, bool useAbsorptionColorsWhenCompressed, int absorptionCandleMinWidthPx, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showVolumeProfile, bool showPOC, bool showDelta, bool showDeltaProfile, bool useGradient, int gradientSteps, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize, bool showVolumeText, int volumeTextMinThreshold, float volumeTextFontSize, string textFontFamily, CandleProfileTextFontWeight textFontWeight, bool useDynamicTextSizing, float dynamicTextMaxFontSize, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return indicator.OrcaCandleVolumeProfile(input, tickCompression, useDynamicVolumeAggregation, volumeDynamicAggregationMultiplier, maxDynamicVolumeTicks, deltaTickCompression, useDynamicDeltaAggregation, deltaDynamicRowMinPixels, deltaDynamicMultiplier, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, publishSharedProfileCache, tradeSourceMode, candleWidthPx, profileWidthPx, deltaProfileWidthPx, profileArrangement, dynamicProfileWidth, profileWidthScale, dualProfileWidthScale, autoHideProfilesWhenCompressed, minBarSpacingToShowProfilesPx, compressedCandleWidthPx, useAbsorptionColorsWhenCompressed, absorptionCandleMinWidthPx, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showVolumeProfile, showPOC, showDelta, showDeltaProfile, useGradient, gradientSteps, showDeltaText, deltaTextMinThreshold, deltaTextFontSize, showVolumeText, volumeTextMinThreshold, volumeTextFontSize, textFontFamily, textFontWeight, useDynamicTextSizing, dynamicTextMaxFontSize, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}
	}
}

#endregion
