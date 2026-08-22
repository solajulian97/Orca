#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using DxBrush = SharpDX.Direct2D1.Brush;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum OrcaHTFTimeframe
	{
		Minutes5,
		Minutes15,
		Minutes30,
		Hour1,
		Hours2,
		Hours4,
		Day1,
		Week1
	}

	public class OrcaHTFTimeframeConverter : EnumConverter
	{
		public OrcaHTFTimeframeConverter() : base(typeof(OrcaHTFTimeframe))
		{
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is OrcaHTFTimeframe)
				return GetDisplayName((OrcaHTFTimeframe)value);

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (!string.IsNullOrWhiteSpace(text))
			{
				foreach (OrcaHTFTimeframe timeframe in Enum.GetValues(typeof(OrcaHTFTimeframe)))
				{
					if (string.Equals(text, GetDisplayName(timeframe), StringComparison.OrdinalIgnoreCase)
						|| string.Equals(text, timeframe.ToString(), StringComparison.OrdinalIgnoreCase))
						return timeframe;
				}
			}

			return base.ConvertFrom(context, culture, value);
		}

		private static string GetDisplayName(OrcaHTFTimeframe timeframe)
		{
			switch (timeframe)
			{
				case OrcaHTFTimeframe.Minutes5:  return "5 Minutes";
				case OrcaHTFTimeframe.Minutes15: return "15 Minutes";
				case OrcaHTFTimeframe.Minutes30: return "30 Minutes";
				case OrcaHTFTimeframe.Hour1:      return "1 Hour";
				case OrcaHTFTimeframe.Hours2:     return "2 Hours";
				case OrcaHTFTimeframe.Hours4:     return "4 Hours";
				case OrcaHTFTimeframe.Day1:       return "1 Day";
				case OrcaHTFTimeframe.Week1:      return "1 Week";
				default:                          return timeframe.ToString();
			}
		}
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaHTFCandles : Indicator
	{
		private struct HtfCandleSnapshot
		{
			public int SourceBarIndex;
			public DateTime StartTime;
			public DateTime MidTime;
			public DateTime EndTime;
			public double Open;
			public double High;
			public double Low;
			public double Close;
			public int StartPrimaryIndex;
			public int MidPrimaryIndex;
			public int EndPrimaryIndex;
		}

		private static readonly HtfCandleSnapshot[] EmptySnapshot = new HtfCandleSnapshot[0];

		private readonly object activeSync = new object();
		private Queue<HtfCandleSnapshot> completedCandles;
		private volatile HtfCandleSnapshot[] completedRenderSnapshot;
		private HtfCandleSnapshot activeCandle;
		private bool hasActiveCandle;
		private int historicalBarsSincePublish;
		private bool timeframeIsHigherThanPrimary;
		private SessionIterator htfSessionIterator;

		private DxBrush dxBullBodyBrush;
		private DxBrush dxBearBodyBrush;
		private DxBrush dxBorderBrush;
		private DxBrush dxBullWickBrush;
		private DxBrush dxBearWickBrush;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca HTF Candles";
				Description = "Overlays native higher-timeframe candles behind the primary chart bars.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DrawOnPricePanel = true;
				IsAutoScale = false;
				DisplayInDataBox = false;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;

				Timeframe = OrcaHTFTimeframe.Hour1;
				CandleLookback = 200;

				BullBody = CreateFrozenBrush(76, 175, 80);
				BearBody = CreateFrozenBrush(255, 82, 82);
				Border = CreateFrozenBrush(46, 46, 46);
				BullWick = CreateFrozenBrush(46, 46, 46);
				BearWick = CreateFrozenBrush(46, 46, 46);
				Transparency = 85;
				BorderWidth = 1;
				WickWidth = 1;
			}
			else if (State == State.Configure)
			{
				AddSelectedTimeframeSeries();
			}
			else if (State == State.DataLoaded)
			{
				completedCandles = new Queue<HtfCandleSnapshot>(Math.Max(1, CandleLookback));
				completedRenderSnapshot = EmptySnapshot;
				historicalBarsSincePublish = 0;
				hasActiveCandle = false;
				timeframeIsHigherThanPrimary = IsSelectedTimeframeHigherThanPrimary();

				if (BarsArray != null && BarsArray.Length > 1)
					htfSessionIterator = new SessionIterator(BarsArray[1]);
			}
			else if (State == State.Historical)
			{
				if (ChartControl != null)
					SetZOrder(-1000);
			}
			else if (State == State.Transition)
			{
				PublishCompletedSnapshot();
			}
			else if (State == State.Terminated)
			{
				DisposeDxResources();
				completedRenderSnapshot = EmptySnapshot;
				completedCandles = null;
				htfSessionIterator = null;
				lock (activeSync)
				{
					hasActiveCandle = false;
					activeCandle = default(HtfCandleSnapshot);
				}
			}
		}

		private static WpfBrush CreateFrozenBrush(byte red, byte green, byte blue)
		{
			WpfSolidColorBrush brush = new WpfSolidColorBrush(WpfColor.FromRgb(red, green, blue));
			brush.Freeze();
			return brush;
		}

		private void AddSelectedTimeframeSeries()
		{
			switch (Timeframe)
			{
				case OrcaHTFTimeframe.Minutes5:
					AddDataSeries(BarsPeriodType.Minute, 5);
					break;
				case OrcaHTFTimeframe.Minutes15:
					AddDataSeries(BarsPeriodType.Minute, 15);
					break;
				case OrcaHTFTimeframe.Minutes30:
					AddDataSeries(BarsPeriodType.Minute, 30);
					break;
				case OrcaHTFTimeframe.Hours2:
					AddDataSeries(BarsPeriodType.Minute, 120);
					break;
				case OrcaHTFTimeframe.Hours4:
					AddDataSeries(BarsPeriodType.Minute, 240);
					break;
				case OrcaHTFTimeframe.Day1:
					AddDataSeries(BarsPeriodType.Day, 1);
					break;
				case OrcaHTFTimeframe.Week1:
					AddDataSeries(BarsPeriodType.Week, 1);
					break;
				default:
					AddDataSeries(BarsPeriodType.Minute, 60);
					break;
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 1 || !timeframeIsHigherThanPrimary || CurrentBars == null || CurrentBars.Length < 2 || CurrentBars[1] < 0)
				return;

			HtfCandleSnapshot next;
			if (!TryBuildCurrentCandle(out next))
				return;

			HtfCandleSnapshot previous;
			bool hadPrevious;
			lock (activeSync)
			{
				hadPrevious = hasActiveCandle;
				previous = activeCandle;
			}

			bool startedNewCandle = !hadPrevious || previous.SourceBarIndex != next.SourceBarIndex;
			if (startedNewCandle && hadPrevious)
			{
				int completedAdded = 0;
				if (next.SourceBarIndex > previous.SourceBarIndex)
				{
					int firstCompletedIndex = Math.Max(previous.SourceBarIndex, next.SourceBarIndex - CandleLookback);
					for (int sourceIndex = firstCompletedIndex; sourceIndex < next.SourceBarIndex; sourceIndex++)
					{
						HtfCandleSnapshot completed;
						if (!TryBuildCandleFromSeriesIndex(sourceIndex, out completed))
							continue;

						MapCompletedCandleToPrimaryBars(ref completed);
						completedCandles.Enqueue(completed);
						completedAdded++;
					}

					while (completedCandles.Count > CandleLookback)
						completedCandles.Dequeue();
				}
				else
				{
					completedCandles.Clear();
					completedRenderSnapshot = EmptySnapshot;
					historicalBarsSincePublish = 0;
				}

				if (State == State.Realtime)
				{
					PublishCompletedSnapshot();
				}
				else
				{
					historicalBarsSincePublish += completedAdded;
					if (completedCandles.Count == 1 || historicalBarsSincePublish >= 64)
					{
						PublishCompletedSnapshot();
						historicalBarsSincePublish = 0;
					}
				}
			}

			lock (activeSync)
			{
				activeCandle = next;
				hasActiveCandle = true;
			}
		}

		private bool TryBuildCurrentCandle(out HtfCandleSnapshot candle)
		{
			return TryBuildCandleFromSeriesIndex(CurrentBars[1], out candle);
		}

		private bool TryBuildCandleFromSeriesIndex(int sourceIndex, out HtfCandleSnapshot candle)
		{
			candle = default(HtfCandleSnapshot);
			if (BarsArray == null || BarsArray.Length < 2 || sourceIndex < 0 || sourceIndex >= BarsArray[1].Count)
				return false;

			DateTime sourceTime = BarsArray[1].GetTime(sourceIndex);
			double open = BarsArray[1].GetOpen(sourceIndex);
			double high = BarsArray[1].GetHigh(sourceIndex);
			double low = BarsArray[1].GetLow(sourceIndex);
			double close = BarsArray[1].GetClose(sourceIndex);

			if (!IsFinite(open) || !IsFinite(high) || !IsFinite(low) || !IsFinite(close))
				return false;

			DateTime startTime;
			DateTime endTime;
			if (!TryGetCandleBounds(sourceIndex, sourceTime, out startTime, out endTime) || endTime <= startTime)
				return false;

			candle.SourceBarIndex = sourceIndex;
			candle.StartTime = startTime;
			candle.EndTime = endTime;
			candle.MidTime = Midpoint(startTime, endTime);
			candle.Open = open;
			candle.High = high;
			candle.Low = low;
			candle.Close = close;
			candle.StartPrimaryIndex = -1;
			candle.MidPrimaryIndex = -1;
			candle.EndPrimaryIndex = -1;
			return true;
		}

		private bool TryGetCandleBounds(int sourceIndex, DateTime sourceTime, out DateTime startTime, out DateTime endTime)
		{
			startTime = DateTime.MinValue;
			endTime = DateTime.MinValue;

			if (Timeframe == OrcaHTFTimeframe.Day1)
				return TryGetDailyBounds(sourceTime, out startTime, out endTime);

			if (Timeframe == OrcaHTFTimeframe.Week1)
				return TryGetWeeklyBounds(sourceTime, out startTime, out endTime);

			endTime = sourceTime;
			DateTime sessionBegin;
			DateTime sessionEnd;
			bool hasSessionBounds = TryGetIntradaySessionBounds(endTime, out sessionBegin, out sessionEnd);

			if (sourceIndex > 0 && BarsArray != null && BarsArray.Length > 1)
			{
				DateTime previousEnd = BarsArray[1].GetTime(sourceIndex - 1);
				if (previousEnd < endTime && (!hasSessionBounds || previousEnd >= sessionBegin))
					startTime = previousEnd;
			}

			if (startTime == DateTime.MinValue || startTime >= endTime)
			{
				TimeSpan duration = TimeSpan.FromMinutes(GetSelectedMinutes());
				startTime = endTime > DateTime.MinValue + duration ? endTime - duration : DateTime.MinValue;
				if (hasSessionBounds && startTime < sessionBegin)
					startTime = sessionBegin;
			}

			return startTime != DateTime.MinValue && endTime > startTime;
		}

		private bool TryGetIntradaySessionBounds(DateTime barEndTime, out DateTime sessionBegin, out DateTime sessionEnd)
		{
			sessionBegin = DateTime.MinValue;
			sessionEnd = DateTime.MinValue;
			if (htfSessionIterator == null || barEndTime == DateTime.MinValue)
				return false;

			try
			{
				DateTime lookupTime = barEndTime.AddTicks(-1);
				DateTime tradingDay = htfSessionIterator.GetTradingDay(lookupTime);
				sessionBegin = htfSessionIterator.GetTradingDayBeginLocal(tradingDay);
				sessionEnd = htfSessionIterator.GetTradingDayEndLocal(tradingDay);
				return sessionEnd > sessionBegin;
			}
			catch
			{
				sessionBegin = DateTime.MinValue;
				sessionEnd = DateTime.MinValue;
				return false;
			}
		}

		private bool TryGetDailyBounds(DateTime barTime, out DateTime startTime, out DateTime endTime)
		{
			startTime = DateTime.MinValue;
			endTime = DateTime.MinValue;

			if (!TryGetTradingDayBegin(barTime.Date, out startTime))
				startTime = barTime.Date;

			try
			{
				endTime = startTime.AddDays(1);
			}
			catch
			{
				return false;
			}

			return endTime > startTime;
		}

		private bool TryGetWeeklyBounds(DateTime barTime, out DateTime startTime, out DateTime endTime)
		{
			DateTime weekDate = barTime.Date;
			int daysSinceMonday = ((int)weekDate.DayOfWeek + 6) % 7;
			DateTime monday = weekDate.AddDays(-daysSinceMonday);

			if (!TryFindTradingDayBegin(monday, 7, out startTime))
				startTime = monday;

			DateTime nextMonday = monday.AddDays(7);
			if (!TryFindTradingDayBegin(nextMonday, 7, out endTime) || endTime <= startTime)
				endTime = startTime.AddDays(7);

			return endTime > startTime;
		}

		private bool TryFindTradingDayBegin(DateTime firstDate, int maximumDays, out DateTime beginTime)
		{
			beginTime = DateTime.MinValue;
			if (htfSessionIterator == null)
				return false;

			for (int dayOffset = 0; dayOffset < maximumDays; dayOffset++)
			{
				DateTime tradingDay = firstDate.Date.AddDays(dayOffset);
				try
				{
					if (!htfSessionIterator.IsTradingDayDefined(tradingDay))
						continue;

					beginTime = htfSessionIterator.GetTradingDayBeginLocal(tradingDay);
					return beginTime != DateTime.MinValue;
				}
				catch
				{
					beginTime = DateTime.MinValue;
				}
			}

			return false;
		}

		private bool TryGetTradingDayBegin(DateTime tradingDay, out DateTime beginTime)
		{
			beginTime = DateTime.MinValue;
			if (htfSessionIterator == null)
				return false;

			try
			{
				beginTime = htfSessionIterator.GetTradingDayBeginLocal(tradingDay.Date);
				return beginTime != DateTime.MinValue;
			}
			catch
			{
				beginTime = DateTime.MinValue;
				return false;
			}
		}

		private static DateTime Midpoint(DateTime startTime, DateTime endTime)
		{
			long halfTicks = (endTime.Ticks - startTime.Ticks) / 2;
			return startTime.AddTicks(halfTicks);
		}

		private static bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		private void MapCompletedCandleToPrimaryBars(ref HtfCandleSnapshot candle)
		{
			candle.StartPrimaryIndex = FindFirstPrimaryBarAfter(candle.StartTime);
			candle.MidPrimaryIndex = FindFirstPrimaryBarAfter(candle.MidTime);
			candle.EndPrimaryIndex = FindFirstPrimaryBarAfter(candle.EndTime);
		}

		private int FindFirstPrimaryBarAfter(DateTime boundary)
		{
			if (BarsArray == null || BarsArray.Length == 0 || BarsArray[0] == null)
				return -1;

			int count = BarsArray[0].Count;
			int low = 0;
			int high = count;
			while (low < high)
			{
				int middle = low + ((high - low) >> 1);
				if (BarsArray[0].GetTime(middle) > boundary)
					high = middle;
				else
					low = middle + 1;
			}

			return low < count ? low : -1;
		}

		private void PublishCompletedSnapshot()
		{
			completedRenderSnapshot = completedCandles == null || completedCandles.Count == 0
				? EmptySnapshot
				: completedCandles.ToArray();
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (!timeframeIsHigherThanPrimary || chartControl == null || chartScale == null || ChartBars == null
				|| ChartPanel == null || RenderTarget == null || BarsArray == null || BarsArray.Length == 0)
				return;

			HtfCandleSnapshot[] completed = completedRenderSnapshot ?? EmptySnapshot;
			HtfCandleSnapshot active;
			bool renderActive;
			lock (activeSync)
			{
				active = activeCandle;
				renderActive = hasActiveCandle;
			}

			if (completed.Length == 0 && !renderActive)
				return;

			EnsureDxResources();
			if (dxBullBodyBrush == null || dxBearBodyBrush == null || dxBorderBrush == null
				|| dxBullWickBrush == null || dxBearWickBrush == null)
				return;

			RectangleF panelBounds = new RectangleF(ChartPanel.X, ChartPanel.Y, ChartPanel.W, ChartPanel.H);
			AntialiasMode previousAntialiasMode = RenderTarget.AntialiasMode;
			bool clipPushed = false;
			try
			{
				RenderTarget.AntialiasMode = AntialiasMode.Aliased;
				RenderTarget.PushAxisAlignedClip(panelBounds, AntialiasMode.Aliased);
				clipPushed = true;

				for (int index = 0; index < completed.Length; index++)
					RenderCandle(chartControl, chartScale, completed[index], panelBounds);

				if (renderActive)
					RenderCandle(chartControl, chartScale, active, panelBounds);
			}
			finally
			{
				if (clipPushed)
					RenderTarget.PopAxisAlignedClip();
				RenderTarget.AntialiasMode = previousAntialiasMode;
			}
		}

		private void RenderCandle(ChartControl chartControl, ChartScale chartScale, HtfCandleSnapshot candle, RectangleF panelBounds)
		{
			float leftX;
			float rightX;
			float wickX;
			if (!TryResolveBoundaryX(chartControl, candle.StartTime, candle.StartPrimaryIndex, out leftX)
				|| !TryResolveBoundaryX(chartControl, candle.EndTime, candle.EndPrimaryIndex, out rightX)
				|| !TryResolveBoundaryX(chartControl, candle.MidTime, candle.MidPrimaryIndex, out wickX))
				return;

			if (rightX < leftX)
			{
				float swap = leftX;
				leftX = rightX;
				rightX = swap;
			}

			if (rightX < panelBounds.Left || leftX > panelBounds.Right)
				return;

			if (rightX - leftX < 1f)
				rightX = leftX + 1f;

			bool isBull = candle.Close >= candle.Open;
			DxBrush bodyBrush = isBull ? dxBullBodyBrush : dxBearBodyBrush;
			DxBrush wickBrush = isBull ? dxBullWickBrush : dxBearWickBrush;

			double bodyHigh = Math.Max(candle.Open, candle.Close);
			double bodyLow = Math.Min(candle.Open, candle.Close);
			float bodyHighY = chartScale.GetYByValue(bodyHigh);
			float bodyLowY = chartScale.GetYByValue(bodyLow);
			float highY = chartScale.GetYByValue(candle.High);
			float lowY = chartScale.GetYByValue(candle.Low);

			if (highY < bodyHighY)
				RenderTarget.DrawLine(new Vector2(wickX, highY), new Vector2(wickX, bodyHighY), wickBrush, WickWidth);
			if (lowY > bodyLowY)
				RenderTarget.DrawLine(new Vector2(wickX, bodyLowY), new Vector2(wickX, lowY), wickBrush, WickWidth);

			float bodyTop = Math.Min(bodyHighY, bodyLowY);
			float bodyHeight = Math.Abs(bodyLowY - bodyHighY);
			if (bodyHeight < 1f)
			{
				bodyTop -= 0.5f;
				bodyHeight = 1f;
			}

			RectangleF bodyRectangle = new RectangleF(leftX, bodyTop, rightX - leftX, bodyHeight);
			RenderTarget.FillRectangle(bodyRectangle, bodyBrush);
			RenderTarget.DrawRectangle(bodyRectangle, dxBorderBrush, BorderWidth);
		}

		private bool TryResolveBoundaryX(ChartControl chartControl, DateTime boundary, int mappedIndex, out float x)
		{
			x = 0f;
			try
			{
				int primaryCount = BarsArray[0].Count;
				int index = mappedIndex;
				if (index < 0 || index >= primaryCount)
					index = FindFirstPrimaryBarAfter(boundary);

				if (index >= 0 && index < primaryCount)
				{
					x = chartControl.GetXByBarIndex(ChartBars, index);
					return !float.IsNaN(x) && !float.IsInfinity(x);
				}

				x = chartControl.GetXByTime(boundary);
				return !float.IsNaN(x) && !float.IsInfinity(x);
			}
			catch
			{
				x = 0f;
				return false;
			}
		}

		private bool IsSelectedTimeframeHigherThanPrimary()
		{
			if (BarsPeriod == null)
				return true;

			long selectedSeconds = GetSelectedSeconds();
			long primarySeconds;
			switch (BarsPeriod.BarsPeriodType)
			{
				case BarsPeriodType.Second:
					primarySeconds = Math.Max(1, BarsPeriod.Value);
					break;
				case BarsPeriodType.Minute:
					primarySeconds = Math.Max(1, BarsPeriod.Value) * 60L;
					break;
				case BarsPeriodType.Day:
					primarySeconds = Math.Max(1, BarsPeriod.Value) * 86400L;
					break;
				case BarsPeriodType.Week:
					primarySeconds = Math.Max(1, BarsPeriod.Value) * 604800L;
					break;
				case BarsPeriodType.Month:
				case BarsPeriodType.Year:
					return false;
				default:
					return true;
			}

			return selectedSeconds > primarySeconds;
		}

		private int GetSelectedMinutes()
		{
			switch (Timeframe)
			{
				case OrcaHTFTimeframe.Minutes5:  return 5;
				case OrcaHTFTimeframe.Minutes15: return 15;
				case OrcaHTFTimeframe.Minutes30: return 30;
				case OrcaHTFTimeframe.Hours2:     return 120;
				case OrcaHTFTimeframe.Hours4:     return 240;
				case OrcaHTFTimeframe.Day1:       return 1440;
				case OrcaHTFTimeframe.Week1:      return 10080;
				default:                          return 60;
			}
		}

		private long GetSelectedSeconds()
		{
			return GetSelectedMinutes() * 60L;
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null)
				return;

			if (dxBullBodyBrush != null && dxBearBodyBrush != null && dxBorderBrush != null
				&& dxBullWickBrush != null && dxBearWickBrush != null)
				return;

			DisposeDxResources();
			try
			{
				dxBullBodyBrush = ToDxBrush(BullBody);
				dxBearBodyBrush = ToDxBrush(BearBody);
				dxBorderBrush = ToDxBrush(Border);
				dxBullWickBrush = ToDxBrush(BullWick);
				dxBearWickBrush = ToDxBrush(BearWick);

				float bodyOpacity = Math.Max(0f, Math.Min(1f, (100f - Transparency) / 100f));
				if (dxBullBodyBrush != null)
					dxBullBodyBrush.Opacity = bodyOpacity;
				if (dxBearBodyBrush != null)
					dxBearBodyBrush.Opacity = bodyOpacity;
			}
			catch
			{
				DisposeDxResources();
			}
		}

		private DxBrush ToDxBrush(WpfBrush brush)
		{
			return (brush ?? System.Windows.Media.Brushes.Transparent).ToDxBrush(RenderTarget);
		}

		private void DisposeDxResources()
		{
			DisposeBrush(ref dxBullBodyBrush);
			DisposeBrush(ref dxBearBodyBrush);
			DisposeBrush(ref dxBorderBrush);
			DisposeBrush(ref dxBullWickBrush);
			DisposeBrush(ref dxBearWickBrush);
		}

		private static void DisposeBrush(ref DxBrush brush)
		{
			if (brush == null)
				return;

			brush.Dispose();
			brush = null;
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}

		#region Properties
		[NinjaScriptProperty]
		[TypeConverter(typeof(OrcaHTFTimeframeConverter))]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Timeframe", Description = "Higher timeframe used to build the candle overlay. Changing it reloads the indicator with one matching secondary series.", Order = 1, GroupName = "General")]
		public OrcaHTFTimeframe Timeframe { get; set; }

		[NinjaScriptProperty]
		[Range(50, 400)]
		[Display(Name = "Candle Lookback", Description = "Maximum number of completed higher-timeframe candles retained. The developing candle is displayed in addition to this count.", Order = 2, GroupName = "General")]
		public int CandleLookback { get; set; }

		[XmlIgnore]
		[Display(Name = "Bull Body", Description = "Fill color for higher-timeframe candles whose close is greater than or equal to their open.", Order = 1, GroupName = "Appearance")]
		public WpfBrush BullBody { get; set; }
		[Browsable(false)]
		public string BullBodySerializable { get { return Serialize.BrushToString(BullBody); } set { BullBody = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bear Body", Description = "Fill color for higher-timeframe candles whose close is below their open.", Order = 2, GroupName = "Appearance")]
		public WpfBrush BearBody { get; set; }
		[Browsable(false)]
		public string BearBodySerializable { get { return Serialize.BrushToString(BearBody); } set { BearBody = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Border", Description = "Outline color for every higher-timeframe candle body.", Order = 3, GroupName = "Appearance")]
		public WpfBrush Border { get; set; }
		[Browsable(false)]
		public string BorderSerializable { get { return Serialize.BrushToString(Border); } set { Border = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bull Wick", Description = "Wick color for higher-timeframe candles whose close is greater than or equal to their open.", Order = 4, GroupName = "Appearance")]
		public WpfBrush BullWick { get; set; }
		[Browsable(false)]
		public string BullWickSerializable { get { return Serialize.BrushToString(BullWick); } set { BullWick = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Bear Wick", Description = "Wick color for higher-timeframe candles whose close is below their open.", Order = 5, GroupName = "Appearance")]
		public WpfBrush BearWick { get; set; }
		[Browsable(false)]
		public string BearWickSerializable { get { return Serialize.BrushToString(BearWick); } set { BearWick = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Transparency", Description = "Body transparency using TradingView semantics: 0 is opaque and 100 is invisible. Borders and wicks remain opaque.", Order = 6, GroupName = "Appearance")]
		public int Transparency { get; set; }

		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Border Width", Description = "Width in pixels of the candle body border.", Order = 7, GroupName = "Appearance")]
		public int BorderWidth { get; set; }

		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Wick Width", Description = "Width in pixels of the upper and lower candle wicks.", Order = 8, GroupName = "Appearance")]
		public int WickWidth { get; set; }
		#endregion
	}
}
