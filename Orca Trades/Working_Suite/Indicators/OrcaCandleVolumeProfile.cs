#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

	public class OrcaCandleVolumeProfile : Indicator
	{
		#region Fields
		private readonly Guid sharedSourceId = Guid.NewGuid();
		// Per primary-bar volume & delta maps
		private List<Dictionary<double, long>> barVolumeMaps;
		private List<Dictionary<double, long>> barDeltaVolumeMaps;
		private List<Dictionary<double, long>> barDeltaMaps;
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
		private SolidColorBrush[] positiveDeltaIntensityBrushes;
		private SolidColorBrush[] negativeDeltaIntensityBrushes;
		private int lastBuiltDeltaIntensitySteps = -1;
		private float lastBuiltDeltaIntensityMinOpacity = -1f;
		private float lastBuiltDeltaIntensityMaxOpacity = -1f;

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
				Description = "Custom footprint chart: draws candles + per-candle volume profiles with optional delta coloring and Value Area.";
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

				// Layout
				CandleWidthPx       = 14;
				ProfileWidthPx      = 80;
				DeltaProfileWidthPx = 40;
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
				VolumeTextMinThreshold = 1;
				VolumeTextFontSize = 8f;
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
				DeltaTextBrush     = WpfBrushes.White;
				VolumeTextBrush    = WpfBrushes.White;
			}
			else if (State == State.Configure)
			{
				if (TradeSourceMode == CandleProfileTradeSourceMode.SecondaryTickSeries)
					AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				barVolumeMaps = new List<Dictionary<double, long>>(4096);
				barDeltaVolumeMaps = new List<Dictionary<double, long>>(4096);
				barDeltaMaps  = new List<Dictionary<double, long>>(4096);
				sharedVolumeMaps = new List<Dictionary<double, long>>(4096);
				sharedUpVolumeMaps = new List<Dictionary<double, long>>(4096);
				sharedDownVolumeMaps = new List<Dictionary<double, long>>(4096);
				barVACache    = new List<double[]>(4096);
				textWidthCache.Clear();
				lastBid = double.NaN;
				lastAsk = double.NaN;
				prevLast = double.NaN;
				lastDirection = 0;
				sharedCoverageBarCount = 0;
				RegisterSharedProfileSource(true);
			}
			else if (State == State.Historical)
			{
				if (ChartControl != null)
					SetZOrder(9000);
			}
			else if (State == State.Terminated)
			{
				OrcaProfileDataCache.UnregisterSource(sharedSourceId);
				DisposeDx();
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
				DisposeBrushPalette(ref positiveDeltaIntensityBrushes);
				DisposeBrushPalette(ref negativeDeltaIntensityBrushes);
				vaVolBrushDx?.Dispose();
				vaLineBrushDx?.Dispose();
				vaLineStrokeDx?.Dispose();
				deltaTextBrushDx?.Dispose();
				volumeTextBrushDx?.Dispose();
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
				lastBuiltDeltaIntensitySteps = -1;
				lastBuiltDeltaIntensityMinOpacity = -1f;
				lastBuiltDeltaIntensityMaxOpacity = -1f;
				vaVolBrushDx       = null;
				vaLineBrushDx      = null;
				vaLineStrokeDx     = null;
				deltaTextBrushDx   = null;
				volumeTextBrushDx  = null;
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
		}
		#endregion

		#region Market Data / Tick Processing
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e == null)
				return;

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
					ProcessTradeIntoPrimaryBar(tradeTime, e.Price, volume);
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

		private void ProcessTradeIntoPrimaryBar(DateTime tickTime, double last, long vol)
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

				// --- DELTA ---
				long signed = ClassifySignedVolume(last, vol);

				if (signed != 0)
				{
					var dmap = barDeltaMaps[primaryIndex];
					if (dmap.TryGetValue(deltaBucketPrice, out long dExisting))
						dmap[deltaBucketPrice] = dExisting + signed;
					else
						dmap[deltaBucketPrice] = signed;

					if (PublishSharedProfileCache)
					{
						var directionalMap = signed > 0 ? sharedUpVolumeMaps[primaryIndex] : sharedDownVolumeMaps[primaryIndex];
						if (directionalMap.TryGetValue(deltaBucketPrice, out long dirExisting))
							directionalMap[deltaBucketPrice] = dirExisting + vol;
						else
							directionalMap[deltaBucketPrice] = vol;
					}
				}

				if (PublishSharedProfileCache)
				{
					sharedDataRevision++;
					sharedSourceLastUpdatedUtc = DateTime.UtcNow;
				}
			}
		}

		private long ClassifySignedVolume(double price, long volume)
		{
			if (volume <= 0)
				return 0;

			long signed = 0;
			if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
			{
				if (price >= lastAsk)
					signed = +volume;
				else if (price <= lastBid)
					signed = -volume;
				else if (!double.IsNaN(prevLast))
				{
					if (price > prevLast) signed = +volume;
					else if (price < prevLast) signed = -volume;
					else signed = lastDirection * volume;
				}
			}
			else if (!double.IsNaN(prevLast))
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

					// --- Draw Wick ---
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

					// --- Draw Body ---
					try
					{
						RenderTarget.FillRectangle(
							new RectangleF(candleLeft, bodyTop, activeCandleWidth, bodyHeight),
							activeBodyBrush);
					}
					finally
					{
						absorptionDxBrush?.Dispose();
					}

					// --- Draw Profiles ---
					if (profilesVisible && HasRenderableProfile(barIdx))
					{
						bool showVolumeProfile = ShowVolumeProfile;
						bool showDeltaProfile = ShowDeltaProfile;
						if (!showVolumeProfile && !showDeltaProfile)
							continue;

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
			double[] cache;
			lock (barDataSync)
			{
				if (barIdx < 0 || barVolumeMaps == null || barVACache == null || barIdx >= barVolumeMaps.Count || barIdx >= barVACache.Count || barVolumeMaps[barIdx] == null || barVolumeMaps[barIdx].Count == 0)
					return;

				volumeSource = new Dictionary<double, long>(barVolumeMaps[barIdx]);
				if (ShowDelta && barDeltaMaps != null && barIdx < barDeltaMaps.Count && barDeltaMaps[barIdx] != null && barDeltaMaps[barIdx].Count > 0)
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
			if (ShowDelta && deltaSource != null && deltaSource.Count > 0)
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

				// Choose brush: POC > Delta > Gradient/Flat
				SolidColorBrush brush;

				if (ShowPOC && Math.Abs(price - pocPrice) < TickSize * 0.01)
				{
					brush = pocBrushDx;
				}
				else if (ShowDelta && deltaMap != null && deltaMap.TryGetValue(price, out long delta))
				{
					brush = SelectDeltaBrush(delta, maxAbsDelta);
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

				DrawVolumeTextLabel(vol, profileRootX, drawProfileWidth, drawY, rowHeight, flowsRight);
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

		private void DrawVolumeTextLabel(long volume, float profileRootX, float drawProfileWidth, float drawY, float rowHeight, bool flowsRight)
		{
			if (!ShowVolumeText || volumeTextBrushDx == null)
				return;
			if (volume < VolumeTextMinThreshold)
				return;

			float fontSize = ResolveProfileTextFontSize(rowHeight, VolumeTextFontSize);
			if (rowHeight < Math.Max(5f, fontSize - 1f))
				return;

			DrawProfileTextLabel(volume.ToString("N0"), volumeTextBrushDx, profileRootX, drawProfileWidth, drawY, rowHeight, flowsRight, fontSize, TextAlignment.Leading);
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

		private void DrawProfileTextLabel(string label, SolidColorBrush brush, float profileRootX, float drawProfileWidth, float drawY, float rowHeight, bool flowsRight, float fontSize, TextAlignment alignment)
		{
			if (string.IsNullOrEmpty(label) || brush == null)
				return;

			TextFormat format = GetProfileTextFormat(fontSize, alignment);
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

		private TextFormat GetProfileTextFormat(float fontSize, TextAlignment alignment)
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
			int key = sizeKey * 10 + (int)alignment;

			if (textFormatsBySize == null)
				textFormatsBySize = new Dictionary<int, TextFormat>();

			TextFormat format;
			if (textFormatsBySize.TryGetValue(key, out format) && format != null)
				return format;

			format = CreateProfileTextFormat(sizeKey / 10f, alignment);
			if (format == null)
				return null;

			textFormatsBySize[key] = format;
			return format;
		}

		private TextFormat CreateProfileTextFormat(float fontSize, TextAlignment alignment)
		{
			if (Core.Globals.DirectWriteFactory == null)
				return null;

			TextFormat format = null;
			string family = GetProfileTextFontFamily();
			SharpDX.DirectWrite.FontWeight weight = ResolveProfileTextFontWeight();

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
			if (deltaTextBrushDx == null)
				deltaTextBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(DeltaTextBrush, 1f));
			if (volumeTextBrushDx == null)
				volumeTextBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VolumeTextBrush, 1f));
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

		#region Properties

		// --- Data ---
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Tick Compression", GroupName = "Data", Order = 0)]
		public int TickCompression { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Volume Aggregation", Description = "Gently increases volume row height when the visible price range is large.", GroupName = "Data", Order = 1)]
		public bool UseDynamicVolumeAggregation { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "Volume Dynamic Multiplier", Description = "Lower values keep volume rows more granular; higher values aggregate sooner.", GroupName = "Data", Order = 2)]
		public double VolumeDynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Max Dynamic Volume Ticks", Description = "Upper cap for dynamic volume row height.", GroupName = "Data", Order = 3)]
		public int MaxDynamicVolumeTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Delta Tick Compression", Description = "Fixed delta row height when dynamic delta aggregation is off.", GroupName = "Data", Order = 4)]
		public int DeltaTickCompression { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Delta Aggregation", Description = "Dynamically increases delta row height as the visible price range expands.", GroupName = "Data", Order = 5)]
		public bool UseDynamicDeltaAggregation { get; set; }

		[NinjaScriptProperty]
		[Range(2, 40)]
		[Display(Name = "Delta Dynamic Row Min Pixels", Description = "Target minimum delta row height used before applying the multiplier.", GroupName = "Data", Order = 6)]
		public int DeltaDynamicRowMinPixels { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "Delta Dynamic Multiplier", Description = "Lower values keep delta rows more granular; higher values aggregate sooner.", GroupName = "Data", Order = 7)]
		public double DeltaDynamicMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Dynamic Delta Min Compression", GroupName = "Data", Order = 8)]
		public int DynamicDeltaMinCompression { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Dynamic Delta Max Compression", GroupName = "Data", Order = 9)]
		public int DynamicDeltaMaxCompression { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Publish Shared Profile Cache", Description = "Publishes raw 1-tick volume/delta maps for Fixed Range and other Orca tools. Best enabled on one rich/tick-replay CVP per chart key; disable weaker duplicates.", GroupName = "Data", Order = 10)]
		public bool PublishSharedProfileCache { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Trade Source Mode", Description = "Secondary Tick Series is the legacy path. Tick Replay Last Events reads replayed Last events and their trade volume, matching Orca Prints more closely; use it on Tick Replay charts.", GroupName = "Data", Order = 11)]
		public CandleProfileTradeSourceMode TradeSourceMode { get; set; }

		// --- Layout ---
		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "Candle Width (px)", GroupName = "Layout", Order = 1)]
		public int CandleWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Profile Width (px)", GroupName = "Layout", Order = 2)]
		public int ProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Delta Profile Width (px)", GroupName = "Layout", Order = 3)]
		public int DeltaProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Profile Arrangement", Description = "Choose which side of the candle gets volume versus delta.", GroupName = "Layout", Order = 4)]
		public CandleProfileSideArrangement ProfileArrangement { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Profile Width", Description = "Dynamically adjusts profile width to fit between candles", GroupName = "Layout", Order = 5)]
		public bool DynamicProfileWidth { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Profile Width Scale", Description = "Scales profile width after dynamic sizing. 0.90 uses 90% of the available space.", GroupName = "Layout", Order = 6)]
		public double ProfileWidthScale { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Dual Profile Width Scale", Description = "Scales each side when volume and delta profiles are both visible. Lower values leave more room between candles.", GroupName = "Layout", Order = 7)]
		public double DualProfileWidthScale { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Auto Hide Profiles When Compressed", Description = "Hide per-candle profiles when visible bar spacing gets too tight, while still drawing candles.", GroupName = "Layout", Order = 8)]
		public bool AutoHideProfilesWhenCompressed { get; set; }

		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name = "Min Bar Spacing To Show Profiles", Description = "Profiles hide when average visible bar spacing is below this many pixels.", GroupName = "Layout", Order = 9)]
		public int MinBarSpacingToShowProfilesPx { get; set; }

		[NinjaScriptProperty]
		[Range(1, 30)]
		[Display(Name = "Compressed Candle Max Width", Description = "Maximum CVP candle width while profiles are auto-hidden. Actual width follows the chart bar width so Alt+Up/Down still works.", GroupName = "Layout", Order = 10)]
		public int CompressedCandleWidthPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Absorption Colors For Candles", Description = "When OrcaAbsorptionCandles is on the chart, use its delta-intensity colors for CVP candle bodies.", GroupName = "Layout", Order = 11)]
		public bool UseAbsorptionColorsWhenCompressed { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Absorption Candle Min Width", Description = "Minimum CVP candle body width when using absorption colors. Wick width remains controlled by Wick Width.", GroupName = "Layout", Order = 12)]
		public int AbsorptionCandleMinWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Candle-Profile Gap (px)", GroupName = "Layout", Order = 13)]
		public int CandleProfileGapPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Profile Bar Spacing (px)", GroupName = "Layout", Order = 14)]
		public int ProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Range(1, 6)]
		[Display(Name = "Wick Width (px)", GroupName = "Layout", Order = 15)]
		public int WickWidthPx { get; set; }

		// --- Visibility ---
		[NinjaScriptProperty]
		[Display(Name = "Show Volume Profile", GroupName = "Visibility", Order = 9)]
		public bool ShowVolumeProfile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show POC", GroupName = "Visibility", Order = 10)]
		public bool ShowPOC { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Color Volume By Delta", Description = "Colors the volume profile rows by row delta. Turn this off to keep volume in its volume color while the separate delta profile uses delta colors.", GroupName = "Visibility", Order = 11)]
		public bool ShowDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Delta Profile", GroupName = "Visibility", Order = 12)]
		public bool ShowDeltaProfile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Gradient", GroupName = "Visibility", Order = 13)]
		public bool UseGradient { get; set; }

		[NinjaScriptProperty]
		[Range(2, 64)]
		[Display(Name = "Gradient Steps", GroupName = "Visibility", Order = 14)]
		public int GradientSteps { get; set; }

		// --- Text Labels ---
		[NinjaScriptProperty]
		[Display(Name = "Show Delta Text", GroupName = "Text Labels", Order = 1)]
		public bool ShowDeltaText { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000000)]
		[Display(Name = "Delta Text Min Threshold", Description = "Minimum absolute delta needed before drawing the delta label.", GroupName = "Text Labels", Order = 2)]
		public int DeltaTextMinThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(6, 24)]
		[Display(Name = "Delta Text Font Size", Description = "Fixed delta label size, or the base size when dynamic text sizing is enabled.", GroupName = "Text Labels", Order = 3)]
		public float DeltaTextFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Delta Text Color", GroupName = "Text Labels", Order = 4)]
		public WpfBrush DeltaTextBrush { get; set; }
		[Browsable(false)]
		public string DeltaTextBrushSerialize
		{ get { return Serialize.BrushToString(DeltaTextBrush); } set { DeltaTextBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Show Volume Text", GroupName = "Text Labels", Order = 5)]
		public bool ShowVolumeText { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000000)]
		[Display(Name = "Volume Text Min Threshold", Description = "Minimum row volume needed before drawing the volume label.", GroupName = "Text Labels", Order = 6)]
		public int VolumeTextMinThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(6, 24)]
		[Display(Name = "Volume Text Font Size", Description = "Fixed volume label size, or the base size when dynamic text sizing is enabled.", GroupName = "Text Labels", Order = 7)]
		public float VolumeTextFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Volume Text Color", GroupName = "Text Labels", Order = 8)]
		public WpfBrush VolumeTextBrush { get; set; }
		[Browsable(false)]
		public string VolumeTextBrushSerialize
		{ get { return Serialize.BrushToString(VolumeTextBrush); } set { VolumeTextBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[TypeConverter(typeof(CandleProfileTextFontFamilyConverter))]
		[Display(Name = "Text Font Family", Description = "Font used for both delta and volume profile labels.", GroupName = "Text Labels", Order = 9)]
		public string TextFontFamily { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Text Font Weight", Description = "Weight used for both delta and volume profile labels.", GroupName = "Text Labels", Order = 10)]
		public CandleProfileTextFontWeight TextFontWeight { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dynamic Text Size", Description = "Lets volume and delta labels grow with the rendered row height, capped by Dynamic Text Max Size.", GroupName = "Text Labels", Order = 11)]
		public bool UseDynamicTextSizing { get; set; }

		[NinjaScriptProperty]
		[Range(6, 32)]
		[Display(Name = "Dynamic Text Max Size", Description = "Largest font size dynamic labels are allowed to use.", GroupName = "Text Labels", Order = 12)]
		public float DynamicTextMaxFontSize { get; set; }

		// --- Value Area ---
		[NinjaScriptProperty]
		[Display(Name = "Show Value Area", GroupName = "Value Area", Order = 20)]
		public bool ShowValueArea { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Color Mode", Description = "Color rows inside the Value Area differently", GroupName = "Value Area", Order = 21)]
		public bool ShowVAColor { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Boundary Lines", Description = "Draw dashed lines at VAH and VAL", GroupName = "Value Area", Order = 22)]
		public bool ShowVALines { get; set; }

		[NinjaScriptProperty]
		[Range(50, 95)]
		[Display(Name = "VA Percent", GroupName = "Value Area", Order = 23)]
		public int ValueAreaPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 6.0)]
		[Display(Name = "VA Line Thickness", GroupName = "Value Area", Order = 24)]
		public float VALineThickness { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Line Style", GroupName = "Value Area", Order = 25)]
		public VALineStyleEnum VALineStyle { get; set; }

		[XmlIgnore]
		[Display(Name = "VA Color", GroupName = "Value Area", Order = 26)]
		public WpfBrush VABrush { get; set; }
		[Browsable(false)]
		public string VABrushSerialize
		{ get { return Serialize.BrushToString(VABrush); } set { VABrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "VA Line Color", GroupName = "Value Area", Order = 27)]
		public WpfBrush VALineBrush { get; set; }
		[Browsable(false)]
		public string VALineBrushSerialize
		{ get { return Serialize.BrushToString(VALineBrush); } set { VALineBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Candles ---
		[XmlIgnore]
		[Display(Name = "Bullish Body", GroupName = "Colors", Order = 30)]
		public WpfBrush BullishBodyBrush { get; set; }
		[Browsable(false)]
		public string BullishBodyBrushSerialize
		{ get { return Serialize.BrushToString(BullishBodyBrush); } set { BullishBodyBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bearish Body", GroupName = "Colors", Order = 31)]
		public WpfBrush BearishBodyBrush { get; set; }
		[Browsable(false)]
		public string BearishBodyBrushSerialize
		{ get { return Serialize.BrushToString(BearishBodyBrush); } set { BearishBodyBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Compressed Bullish Body", Description = "Body color used only when auto-hide profiles is active and profiles are hidden.", GroupName = "Colors", Order = 32)]
		public WpfBrush CompressedBullishBodyBrush { get; set; }
		[Browsable(false)]
		public string CompressedBullishBodyBrushSerialize
		{ get { return Serialize.BrushToString(CompressedBullishBodyBrush); } set { CompressedBullishBodyBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Compressed Bearish Body", Description = "Body color used only when auto-hide profiles is active and profiles are hidden.", GroupName = "Colors", Order = 33)]
		public WpfBrush CompressedBearishBodyBrush { get; set; }
		[Browsable(false)]
		public string CompressedBearishBodyBrushSerialize
		{ get { return Serialize.BrushToString(CompressedBearishBodyBrush); } set { CompressedBearishBodyBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Profile ---
		[XmlIgnore]
		[Display(Name = "Volume Color", GroupName = "Colors", Order = 34)]
		public WpfBrush VolumeBrush { get; set; }
		[Browsable(false)]
		public string VolumeBrushSerialize
		{ get { return Serialize.BrushToString(VolumeBrush); } set { VolumeBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.05, 1.0)]
		[Display(Name = "Min Brightness", GroupName = "Colors", Order = 35)]
		public float MinBrightness { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Volume Opacity", GroupName = "Colors", Order = 36)]
		public float VolumeOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Color", GroupName = "Colors", Order = 37)]
		public WpfBrush POCBrush { get; set; }
		[Browsable(false)]
		public string POCBrushSerialize
		{ get { return Serialize.BrushToString(POCBrush); } set { POCBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Delta ---
		[XmlIgnore]
		[Display(Name = "Positive Delta", GroupName = "Colors", Order = 38)]
		public WpfBrush PositiveDeltaBrush { get; set; }
		[Browsable(false)]
		public string PositiveDeltaBrushSerialize
		{ get { return Serialize.BrushToString(PositiveDeltaBrush); } set { PositiveDeltaBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Negative Delta", GroupName = "Colors", Order = 39)]
		public WpfBrush NegativeDeltaBrush { get; set; }
		[Browsable(false)]
		public string NegativeDeltaBrushSerialize
		{ get { return Serialize.BrushToString(NegativeDeltaBrush); } set { NegativeDeltaBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Delta Opacity", GroupName = "Colors", Order = 40)]
		public float DeltaOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Delta Intensity Color", Description = "Scales delta row opacity by absolute delta, similar to absorption candle intensity.", GroupName = "Colors", Order = 41)]
		public bool UseDeltaIntensityColoring { get; set; }

		[NinjaScriptProperty]
		[Range(0.05, 1.0)]
		[Display(Name = "Delta Intensity Min Opacity", Description = "Minimum opacity used for the weakest visible delta rows.", GroupName = "Colors", Order = 42)]
		public float DeltaIntensityMinOpacity { get; set; }

		#endregion
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
