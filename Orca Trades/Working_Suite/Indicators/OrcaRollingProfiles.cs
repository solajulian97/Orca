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
		private Queue<OrcaProfileBucket> rollingHistory;
		private OrcaProfileBucket developingBucket;
		private OrcaProfileBucket totalProfile;
		private DateTime currentMinuteToken;
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double prevLast = double.NaN;
		private int lastDynamicDeltaComp = -1;

		private SharpDX.Direct2D1.SolidColorBrush volBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush pocBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush posDeltaBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush negDeltaBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush[] volGradientBrushes;
		private int lastBuiltGradientSteps = -1;
		private SharpDX.Direct2D1.SolidColorBrush vaVolBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush[] vaGradientBrushes;
		private int lastBuiltVAGradientSteps = -1;
		private SharpDX.Direct2D1.SolidColorBrush vaLineBrushDx;
		private SharpDX.Direct2D1.StrokeStyle vaLineStrokeDx;
		private SharpDX.Direct2D1.SolidColorBrush deltaTextBrushDx;
		private SharpDX.Direct2D1.SolidColorBrush labelBgBrushDx;
		private TextFormat deltaTextFormatDx;
		private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();

		// ── 1. Data ──────────────────────────────────────────────────────────────
		[NinjaScriptProperty]
		[Display(Name = "1. Rolling Period", Order = 1, GroupName = "1. Data")]
		public RollingProfilePeriod Period { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "2. Operating Mode", Order = 2, GroupName = "1. Data")]
		public ProfileOperatingMode Mode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 3000)]
		[Display(Name = "3. Minutes In Trading Day", Order = 3, GroupName = "1. Data")]
		public int MinutesPerDay { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "4. RTH Start Time", Order = 4, GroupName = "1. Data")]
		public TimeSpan RthStartTime { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "5. RTH End Time", Order = 5, GroupName = "1. Data")]
		public TimeSpan RthEndTime { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Volume Tick Compression", Order = 6, GroupName = "1. Data")]
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
		public float DeltaOpacity { get; set; }

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
		[Display(Name = "Text Color", Order = 4, GroupName = "7. Delta Text")]
		public System.Windows.Media.Brush DeltaTextBrush { get; set; }

		[Browsable(false)]
		public string DeltaTextBrushSerializable
		{
			get { return Serialize.BrushToString(DeltaTextBrush); }
			set { DeltaTextBrush = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Label Background Color", Order = 5, GroupName = "7. Delta Text")]
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
		[Range(1, 100)]
		[Display(Name = "Delta Tick Compression (static)", Order = 3, GroupName = "8. Delta Aggregation",
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
				ShowDeltaText = true;
				ShowDeltaLabelBackground = true;
				DeltaTextMinThreshold = 10;
				DeltaTextBrush = Brushes.White;
				DeltaLabelBgBrush = Brushes.Black;
				DeltaTextFontSize = 10f;
				UseDynamicAggregation = true;
				DynamicAggregationMultiplier = 1.0;
				DeltaTickCompression = 4;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				rollingHistory = new Queue<OrcaProfileBucket>();
				developingBucket = new OrcaProfileBucket();
				totalProfile = new OrcaProfileBucket();
				currentMinuteToken = DateTime.MinValue;
				textWidthCache.Clear();
			}
			else if (State == State.Terminated)
			{
				DisposeDx();
			}
		}

		private void DisposeDx()
		{
			if (volBrushDx != null) volBrushDx.Dispose();
			if (pocBrushDx != null) pocBrushDx.Dispose();
			if (posDeltaBrushDx != null) posDeltaBrushDx.Dispose();
			if (negDeltaBrushDx != null) negDeltaBrushDx.Dispose();
			if (vaVolBrushDx != null) vaVolBrushDx.Dispose();
			if (vaLineBrushDx != null) vaLineBrushDx.Dispose();
			if (vaLineStrokeDx != null) vaLineStrokeDx.Dispose();
			if (volGradientBrushes != null) foreach (var b in volGradientBrushes) if (b != null) b.Dispose();
			if (vaGradientBrushes != null) foreach (var b in vaGradientBrushes) if (b != null) b.Dispose();
			if (deltaTextBrushDx != null) deltaTextBrushDx.Dispose();
			if (labelBgBrushDx != null) labelBgBrushDx.Dispose();

			volBrushDx = null;
			pocBrushDx = null;
			posDeltaBrushDx = null;
			negDeltaBrushDx = null;
			vaVolBrushDx = null;
			vaLineBrushDx = null;
			vaLineStrokeDx = null;
			volGradientBrushes = null;
			vaGradientBrushes = null;
			deltaTextBrushDx = null;
			labelBgBrushDx = null;
			deltaTextFormatDx = null;
			lastBuiltGradientSteps = -1;
			lastBuiltVAGradientSteps = -1;
			lastDynamicDeltaComp = -1;
			textWidthCache.Clear();
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid) lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask) lastAsk = e.Price;
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 1) return;
			DateTime time = Time[0];
			if (Mode == ProfileOperatingMode.RthOnly && (time.TimeOfDay < RthStartTime || time.TimeOfDay > RthEndTime)) return;

			DateTime minute = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
			if (minute > currentMinuteToken)
			{
				if (currentMinuteToken != DateTime.MinValue)
				{
					developingBucket.MinuteToken = currentMinuteToken;
					rollingHistory.Enqueue(developingBucket);
					PruneRollingHistory(minute);
					RebuildTotalProfileFromBuckets();
				}
				currentMinuteToken = minute;
				developingBucket = new OrcaProfileBucket { MinuteToken = minute };
			}

			double price = Close[0];
			long volume = (long)Volume[0];
			if (volume <= 0) return;

			// Volume stored at VolumeTickCompression buckets (1 tick granularity for delta re-grouping at render)
			double tickSize = (double)VolumeTickCompression * TickSize;
			double vKey = Math.Floor(price / tickSize + 1E-06) * tickSize;

			if (developingBucket.VolByPrice.ContainsKey(vKey)) developingBucket.VolByPrice[vKey] += volume;
			else developingBucket.VolByPrice[vKey] = volume;

			if (totalProfile.VolByPrice.ContainsKey(vKey)) totalProfile.VolByPrice[vKey] += volume;
			else totalProfile.VolByPrice[vKey] = volume;

			// Delta stored at 1-tick granularity so dynamic aggregation can re-group at render time
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

			if (delta != 0)
			{
				// Store at 1-tick level so OnRender can re-bucket dynamically
				double rawKey = Math.Floor(price / TickSize + 1E-06) * TickSize;
				if (developingBucket.DeltaByPrice.ContainsKey(rawKey)) developingBucket.DeltaByPrice[rawKey] += delta;
				else developingBucket.DeltaByPrice[rawKey] = delta;

				if (totalProfile.DeltaByPrice.ContainsKey(rawKey)) totalProfile.DeltaByPrice[rawKey] += delta;
				else totalProfile.DeltaByPrice[rawKey] = delta;
			}
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
			if (volBrushDx == null) volBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VolumeBrush, VolumeOpacity));
			if (pocBrushDx == null) pocBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(POCBrush, 1f));
			if (posDeltaBrushDx == null) posDeltaBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(PositiveDeltaBrush, DeltaOpacity));
			if (negDeltaBrushDx == null) negDeltaBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaBrush, DeltaOpacity));
			if (vaVolBrushDx == null) vaVolBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VABrush, VolumeOpacity));
			if (vaLineBrushDx == null) vaLineBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(VALineBrush, 1f));
			if (vaLineStrokeDx == null) vaLineStrokeDx = new SharpDX.Direct2D1.StrokeStyle(RenderTarget.Factory, new SharpDX.Direct2D1.StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dash });
			if (deltaTextBrushDx == null) deltaTextBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaTextBrush, 1f));
			if (labelBgBrushDx == null) labelBgBrushDx = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(DeltaLabelBgBrush, 1f));

			if (deltaTextFormatDx == null)
			{
				deltaTextFormatDx = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, DeltaTextFontSize)
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

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			try
			{
				EnsureDxResources();
				if (totalProfile == null || totalProfile.VolByPrice.Count == 0) return;

				float chartY = ChartPanel.Y;
				float chartH = ChartPanel.H;
				float canvasX = ChartPanel.X + ChartPanel.W - RightOffsetPx;

				// ── Compute dynamic delta bucket size ──────────────────────────
				int dynamicDeltaComp;
				if (UseDynamicAggregation)
				{
					double visibleTicks = (chartScale.MaxValue - chartScale.MinValue) / TickSize;
					double ticksPerPixel = visibleTicks / Math.Max(1, chartH);
					double desiredTicks = ticksPerPixel * (DeltaTextFontSize + 4) * DynamicAggregationMultiplier;

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
				foreach (var kvp in totalProfile.VolByPrice)
				{
					if (kvp.Value > maxVol) { maxVol = kvp.Value; pocPrice = kvp.Key; }
				}

				double vah = 0, val = 0;
				bool vaFound = false;
				if (ShowValueArea && maxVol > 0)
				{
					vaFound = CalculateValueArea(totalProfile.VolByPrice, pocPrice, out vah, out val);
				}

				double vTick = VolumeTickCompression * TickSize;
				if (ShowVolume && maxVol > 0)
				{
					foreach (var kvp in totalProfile.VolByPrice)
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
				if (ShowDelta && totalProfile.DeltaByPrice.Count > 0)
				{
					double deltaComp = dynamicDeltaComp * TickSize;
					HashSet<double> volumeBucketsForDelta = null;
					if (ShowVolume)
					{
						volumeBucketsForDelta = new HashSet<double>();
						foreach (var kvp in totalProfile.VolByPrice)
						{
							double startPrice = Math.Floor(kvp.Key / deltaComp + 1E-06) * deltaComp;
							double endPrice = Math.Floor((kvp.Key + vTick - TickSize) / deltaComp + 1E-06) * deltaComp;
							for (double bPrice = startPrice; bPrice <= endPrice + 1E-07; bPrice += deltaComp)
								volumeBucketsForDelta.Add(bPrice);
						}
					}

					// When volume is visible, keep delta buckets clipped to the volume profile.
					var groupedDelta = new Dictionary<double, long>();
					foreach (var kvp in totalProfile.DeltaByPrice)
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

							SharpDX.Direct2D1.SolidColorBrush brush = kvp.Value >= 0 ? posDeltaBrushDx : negDeltaBrushDx;
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
								RenderTarget.DrawText(lbl, deltaTextFormatDx, new RectangleF(tX, tY, textWidth + 4f, DeltaTextFontSize + 2), deltaTextBrushDx);
							}
						}
					}
				}
			}
			catch { }
		}

		private bool CalculateValueArea(Dictionary<double, long> map, double poc, out double vah, out double val)
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
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaRollingProfiles[] cacheOrcaRollingProfiles;
		public OrcaRollingProfiles OrcaRollingProfiles(RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaTickCompression)
		{
			return OrcaRollingProfiles(Input, period, mode, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaTickCompression);
		}

		public OrcaRollingProfiles OrcaRollingProfiles(ISeries<double> input, RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaTickCompression)
		{
			if (cacheOrcaRollingProfiles != null)
				for (int idx = 0; idx < cacheOrcaRollingProfiles.Length; idx++)
					if (cacheOrcaRollingProfiles[idx] != null && cacheOrcaRollingProfiles[idx].Period == period && cacheOrcaRollingProfiles[idx].Mode == mode && cacheOrcaRollingProfiles[idx].MinutesPerDay == minutesPerDay && cacheOrcaRollingProfiles[idx].RthStartTime == rthStartTime && cacheOrcaRollingProfiles[idx].RthEndTime == rthEndTime && cacheOrcaRollingProfiles[idx].VolumeTickCompression == volumeTickCompression && cacheOrcaRollingProfiles[idx].ProfileWidthPx == profileWidthPx && cacheOrcaRollingProfiles[idx].DeltaWidthPx == deltaWidthPx && cacheOrcaRollingProfiles[idx].RightOffsetPx == rightOffsetPx && cacheOrcaRollingProfiles[idx].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaRollingProfiles[idx].DeltaDirection == deltaDirection && cacheOrcaRollingProfiles[idx].ShowVolume == showVolume && cacheOrcaRollingProfiles[idx].ShowDelta == showDelta && cacheOrcaRollingProfiles[idx].ShowPOC == showPOC && cacheOrcaRollingProfiles[idx].UseGradient == useGradient && cacheOrcaRollingProfiles[idx].GradientSteps == gradientSteps && cacheOrcaRollingProfiles[idx].MinBrightness == minBrightness && cacheOrcaRollingProfiles[idx].ShowValueArea == showValueArea && cacheOrcaRollingProfiles[idx].ShowVAColor == showVAColor && cacheOrcaRollingProfiles[idx].ShowVALines == showVALines && cacheOrcaRollingProfiles[idx].ValueAreaPercent == valueAreaPercent && cacheOrcaRollingProfiles[idx].VALineThickness == vALineThickness && cacheOrcaRollingProfiles[idx].VolumeOpacity == volumeOpacity && cacheOrcaRollingProfiles[idx].DeltaOpacity == deltaOpacity && cacheOrcaRollingProfiles[idx].ShowDeltaText == showDeltaText && cacheOrcaRollingProfiles[idx].ShowDeltaLabelBackground == showDeltaLabelBackground && cacheOrcaRollingProfiles[idx].DeltaTextMinThreshold == deltaTextMinThreshold && cacheOrcaRollingProfiles[idx].DeltaTextFontSize == deltaTextFontSize && cacheOrcaRollingProfiles[idx].UseDynamicAggregation == useDynamicAggregation && cacheOrcaRollingProfiles[idx].DynamicAggregationMultiplier == dynamicAggregationMultiplier && cacheOrcaRollingProfiles[idx].DeltaTickCompression == deltaTickCompression && cacheOrcaRollingProfiles[idx].EqualsInput(input))
						return cacheOrcaRollingProfiles[idx];
			return CacheIndicator<OrcaRollingProfiles>(new OrcaRollingProfiles(){ Period = period, Mode = mode, MinutesPerDay = minutesPerDay, RthStartTime = rthStartTime, RthEndTime = rthEndTime, VolumeTickCompression = volumeTickCompression, ProfileWidthPx = profileWidthPx, DeltaWidthPx = deltaWidthPx, RightOffsetPx = rightOffsetPx, ProfileBarSpacingPx = profileBarSpacingPx, DeltaDirection = deltaDirection, ShowVolume = showVolume, ShowDelta = showDelta, ShowPOC = showPOC, UseGradient = useGradient, GradientSteps = gradientSteps, MinBrightness = minBrightness, ShowValueArea = showValueArea, ShowVAColor = showVAColor, ShowVALines = showVALines, ValueAreaPercent = valueAreaPercent, VALineThickness = vALineThickness, VolumeOpacity = volumeOpacity, DeltaOpacity = deltaOpacity, ShowDeltaText = showDeltaText, ShowDeltaLabelBackground = showDeltaLabelBackground, DeltaTextMinThreshold = deltaTextMinThreshold, DeltaTextFontSize = deltaTextFontSize, UseDynamicAggregation = useDynamicAggregation, DynamicAggregationMultiplier = dynamicAggregationMultiplier, DeltaTickCompression = deltaTickCompression }, input, ref cacheOrcaRollingProfiles);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaRollingProfiles OrcaRollingProfiles(RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaTickCompression)
		{
			return indicator.OrcaRollingProfiles(Input, period, mode, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaTickCompression);
		}

		public Indicators.OrcaRollingProfiles OrcaRollingProfiles(ISeries<double> input , RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaTickCompression)
		{
			return indicator.OrcaRollingProfiles(input, period, mode, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaTickCompression);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaRollingProfiles OrcaRollingProfiles(RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaTickCompression)
		{
			return indicator.OrcaRollingProfiles(Input, period, mode, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaTickCompression);
		}

		public Indicators.OrcaRollingProfiles OrcaRollingProfiles(ISeries<double> input , RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, RollingDeltaDirection deltaDirection, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, bool showDeltaLabelBackground, int deltaTextMinThreshold, float deltaTextFontSize, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaTickCompression)
		{
			return indicator.OrcaRollingProfiles(input, period, mode, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, deltaDirection, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, showDeltaText, showDeltaLabelBackground, deltaTextMinThreshold, deltaTextFontSize, useDynamicAggregation, dynamicAggregationMultiplier, deltaTickCompression);
		}
	}
}

#endregion
