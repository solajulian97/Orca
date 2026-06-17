#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

using DxBrush = SharpDX.Direct2D1.Brush;
using DxSolidBrush = SharpDX.Direct2D1.SolidColorBrush;
using DxTextFormat = SharpDX.DirectWrite.TextFormat;
using WpfBrush = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColors = System.Windows.Media.Colors;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum OrcaSessionTimeMode
	{
		ChartTime,
		EasternTime,
		LocalComputerTime
	}

	public enum OrcaSessionCarryForwardMode
	{
		None,
		NextSessionOnly,
		RestOfDay,
		UntilTouched,
		CustomBars
	}

	public enum OrcaSessionProfileSide
	{
		LeftOfSession,
		RightOfSession,
		AnchoredToSessionStart,
		AnchoredToSessionEnd
	}

	public enum OrcaSessionProfileWidthMode
	{
		FixedPixels,
		PercentOfSessionWidth
	}

	public enum OrcaSessionProfileGradientMode
	{
		OrcaDefaultGradient,
		SingleColor
	}

	public enum OrcaSessionSweepScope
	{
		PreviousSessionOnly,
		AllPriorSessionsFromSameTradingDay,
		AsiaLondonIntoNY
	}

	public enum OrcaSessionReclaimConfirmationMode
	{
		IntrabarTouch,
		BarClose,
		NBarsClose
	}

	public enum OrcaSessionProjectionSource
	{
		PriorSessionRange,
		OpeningRange,
		Both
	}

	public enum OrcaSessionProjectionCarryForwardMode
	{
		NextSessionOnly,
		RestOfDay
	}

	public enum OrcaSessionStatsPanelPosition
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public enum OrcaSessionClassification
	{
		Unknown,
		Balanced,
		TrendUp,
		TrendDown,
		TransitioningUp,
		TransitioningDown
	}

	public enum OrcaSessionDeltaMode
	{
		ExistingOrcaDelta,
		BidAsk,
		UpDownTickApproximation,
		Disabled
	}

	public enum OrcaSessionUpdateMode
	{
		PriceChange,
		EachTick
	}

	public enum OrcaSessionDashStyle
	{
		Solid,
		Dash,
		Dot,
		DashDot
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaSessionContextMap : Indicator
	{
		#region Models
		private class SessionDefinition
		{
			public int Index;
			public string Name;
			public bool Enabled;
			public TimeSpan StartTime;
			public TimeSpan EndTime;
			public WpfBrush FillBrush;
			public int FillOpacity;
			public bool ShowShading = true;
			public bool ShowHighLow = true;
			public bool ShowMidpoint = true;
			public bool ShowVWAP = true;
			public bool ShowOpeningRange = true;
			public bool ShowVolumeProfile = true;
			public bool ShowCarryForwardLevels = true;
			public bool ShowRangeProjections = true;

			public bool CrossesMidnight
			{
				get { return EndTime <= StartTime; }
			}
		}

		private class ProfileRow
		{
			public int PriceTick;
			public double Price;
			public double Volume;
			public double BidVolume;
			public double AskVolume;
			public double Delta;
		}

		private class SessionEvent
		{
			public string EventType;
			public string RelatedLevel;
			public DateTime Time;
			public int BarIndex = -1;
			public double Price = double.NaN;
			public int Direction;
			public string LabelText;
		}

		private class VwapPoint
		{
			public int BarIndex;
			public double Price;
		}

		private class SweepTracker
		{
			public bool Swept;
			public bool Reclaimed;
			public bool Accepted;
			public int ConsecutiveReclaimCloses;
			public int ConsecutiveAcceptanceCloses;
		}

		private class SessionState
		{
			public SessionDefinition Definition;
			public DateTime SessionStart;
			public DateTime SessionEnd;
			public DateTime TradingDate;
			public string SessionKey;
			public bool IsActive;
			public bool IsComplete;
			public bool SnapshotCreated;
			public int FirstBarIndex = -1;
			public int LastBarIndex = -1;
			public double Open = double.NaN;
			public double High = double.NaN;
			public double Low = double.NaN;
			public double Close = double.NaN;
			public double Midpoint = double.NaN;
			public double Range = double.NaN;
			public double SessionVWAP = double.NaN;
			public double CumulativeVolume;
			public double CumulativeDelta;
			public double SumPV;
			public double SumVolume;
			public double OpeningRangeHigh = double.NaN;
			public double OpeningRangeLow = double.NaN;
			public bool OpeningRangeComplete;
			public DateTime OpeningRangeEndTime;
			public Dictionary<int, ProfileRow> ProfileRows = new Dictionary<int, ProfileRow>();
			public List<SessionEvent> Events = new List<SessionEvent>();
			public List<VwapPoint> VwapPoints = new List<VwapPoint>(512);
			public Dictionary<string, SweepTracker> SweepTrackers = new Dictionary<string, SweepTracker>();
			public OrcaSessionClassification Classification = OrcaSessionClassification.Unknown;
			public string OpenLocationText = string.Empty;
			public string StatusText = string.Empty;
			public bool OpenLocationClassified;
			public double Poc = double.NaN;
			public double Vah = double.NaN;
			public double Val = double.NaN;
			public double MaxProfileVolume;
			public int BarsProcessed;
			public int BarsAboveVwap;
			public int BarsBelowVwap;
			public int VwapCrossCount;
			public int MidpointCrossCount;
			public int OpeningRangeAboveCount;
			public int OpeningRangeBelowCount;
			public bool? LastAboveVwap;
			public bool? LastAboveMidpoint;
			public double FirstVwap = double.NaN;
			public double LastVwap = double.NaN;
		}

		private class PriorSessionSnapshot
		{
			public string Name;
			public int DefinitionIndex;
			public DateTime SessionStart;
			public DateTime SessionEnd;
			public DateTime TradingDate;
			public int StartBarIndex;
			public int EndBarIndex;
			public double High = double.NaN;
			public double Low = double.NaN;
			public double Midpoint = double.NaN;
			public double VWAP = double.NaN;
			public double OpeningRangeHigh = double.NaN;
			public double OpeningRangeLow = double.NaN;
			public double Range = double.NaN;
			public double Volume;
			public double Delta;
			public double POC = double.NaN;
			public double VAH = double.NaN;
			public double VAL = double.NaN;
			public int FirstTouchBarIndex = -1;
			public int NextSessionEndBarIndex = -1;
			public DateTime NextSessionEndTime = DateTime.MinValue;
		}

		private class PendingLabel
		{
			public float X;
			public float Y;
			public int BrushIndex;
			public string Text;
		}

		private class RenderProfileRow
		{
			public int PriceTick;
			public int SpanTicks = 1;
			public double Price;
			public double Volume;
			public double Delta;
		}
		#endregion

		#region Fields
		private readonly List<SessionDefinition> sessionDefinitions = new List<SessionDefinition>(3);
		private readonly Dictionary<string, SessionState> sessionsByKey = new Dictionary<string, SessionState>();
		private readonly List<SessionState> sessions = new List<SessionState>(64);
		private readonly List<PriorSessionSnapshot> snapshots = new List<PriorSessionSnapshot>(64);
		private readonly List<PendingLabel> pendingLabels = new List<PendingLabel>(256);
		private readonly List<RenderProfileRow> renderRows = new List<RenderProfileRow>(1024);
		private readonly Dictionary<int, RenderProfileRow> renderRowMap = new Dictionary<int, RenderProfileRow>();
		private readonly Dictionary<double, long> valueAreaScratch = new Dictionary<double, long>();

		private TimeZoneInfo easternTimeZone;
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double previousTradePrice = double.NaN;
		private int lastTradeDirection;
		private int lastVolumeBarIndex = -1;
		private double lastBarVolume;
		private DateTime lastPruneClock = DateTime.MinValue;

		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private DxSolidBrush[] dxBrushes;
		private DxSolidBrush dxPanelBackgroundBrush;
		private DxSolidBrush dxPanelBorderBrush;
		private StrokeStyle[] dxStrokes;
		private DxTextFormat dxLabelFormat;
		private DxTextFormat dxPanelFormat;
		private bool dxValid;

		private const int BrushAsia = 0;
		private const int BrushLondon = 1;
		private const int BrushNewYork = 2;
		private const int BrushHigh = 3;
		private const int BrushLow = 4;
		private const int BrushMidpoint = 5;
		private const int BrushVwap = 6;
		private const int BrushOpeningRange = 7;
		private const int BrushProjection = 8;
		private const int BrushCarry = 9;
		private const int BrushProfile = 10;
		private const int BrushPoc = 11;
		private const int BrushValueArea = 12;
		private const int BrushLabel = 13;
		private const int BrushEventBullish = 14;
		private const int BrushEventBearish = 15;
		private const int BrushEventNeutral = 16;
		private const int BrushCount = 17;
		#endregion

		#region State
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "ORCA Session Context Map";
				Description = "Session-based ORCA market context map for Asia, London, and New York sessions.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DrawOnPricePanel = true;
				DisplayInDataBox = false;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;

				EnableAsia = true;
				AsiaStartTime = new TimeSpan(18, 0, 0);
				AsiaEndTime = new TimeSpan(3, 0, 0);
				EnableLondon = true;
				LondonStartTime = new TimeSpan(3, 0, 0);
				LondonEndTime = new TimeSpan(9, 30, 0);
				EnableNewYork = true;
				NewYorkStartTime = new TimeSpan(9, 30, 0);
				NewYorkEndTime = new TimeSpan(16, 0, 0);
				SessionTimeMode = OrcaSessionTimeMode.ChartTime;
				LookbackDays = 5;

				ShowSessionShading = true;
				SessionOpacity = 8;
				AsiaOpacity = 8;
				LondonOpacity = 8;
				NewYorkOpacity = 8;
				ShowSessionHighLow = true;
				ShowSessionMidpoint = true;
				ShowSessionVWAP = true;
				ShowOpeningRange = true;
				OpeningRangeSeconds = 30;
				ShowCarryForwardLevels = true;
				CarryForwardMode = OrcaSessionCarryForwardMode.RestOfDay;
				CarryForwardCustomBars = 200;

				ShowSessionVolumeProfile = true;
				ProfileSide = OrcaSessionProfileSide.RightOfSession;
				ProfileWidthMode = OrcaSessionProfileWidthMode.PercentOfSessionWidth;
				ProfileWidthPixels = 120;
				ProfileWidthPercent = 22;
				ProfileOpacity = 34;
				ProfileGradientMode = OrcaSessionProfileGradientMode.OrcaDefaultGradient;
				ShowProfilePOC = true;
				ShowProfileValueArea = true;
				ValueAreaPercent = 70;
				MinimumProfileRowWidth = 1;
				MinProfileRowHeight = 2;
				MaxProfileRows = 240;
				UseDynamicAggregation = true;

				EnableSweepDetection = true;
				SweepTickBuffer = 1;
				DetectSweepsAgainst = OrcaSessionSweepScope.AsiaLondonIntoNY;
				EnableReclaimDetection = true;
				ReclaimTickBuffer = 1;
				ReclaimConfirmationMode = OrcaSessionReclaimConfirmationMode.BarClose;
				ReclaimConfirmationBars = 1;
				ShowReclaimLabels = true;
				EnableAcceptanceDetection = true;
				AcceptanceBars = 3;
				AcceptanceTickBuffer = 1;
				ShowAcceptance = true;
				ShowOpenLocationClassification = true;
				NearVWAPTicks = 4;
				NearMidpointTicks = 4;
				NearLevelTicks = 4;

				ShowRangeProjections = true;
				ProjectionMultiplier1 = 0.5;
				ProjectionMultiplier2 = 1.0;
				ProjectionSource = OrcaSessionProjectionSource.PriorSessionRange;
				ProjectionCarryForwardMode = OrcaSessionProjectionCarryForwardMode.NextSessionOnly;

				ShowStatsPanel = true;
				StatsPanelPosition = OrcaSessionStatsPanelPosition.TopRight;
				StatsPanelOpacity = 72;
				StatsPanelFontSize = 11;
				CompactStatsPanel = true;
				ShowOnlyCurrentSessionStats = false;

				EnableTrendBalanceClassification = true;
				VWAPDominancePercent = 65;
				TrendScoreThreshold = 3;
				BalanceCrossCountThreshold = 3;
				OpeningRangeAcceptanceBars = 3;
				DeltaAlignmentEnabled = true;
				MinimumMinutesBeforeClassification = 5;
				DeltaMode = OrcaSessionDeltaMode.BidAsk;

				AsiaColor = WpfBrushes.SteelBlue;
				LondonColor = WpfBrushes.MediumSeaGreen;
				NewYorkColor = WpfBrushes.DarkOrange;
				HighLineColor = WpfBrushes.LightSkyBlue;
				LowLineColor = WpfBrushes.LightCoral;
				MidpointLineColor = WpfBrushes.Goldenrod;
				VWAPLineColor = WpfBrushes.Orchid;
				OpeningRangeLineColor = WpfBrushes.WhiteSmoke;
				ProjectionLineColor = WpfBrushes.MediumPurple;
				CarryForwardLineColor = WpfBrushes.DarkGray;
				ProfileColor = WpfBrushes.CornflowerBlue;
				POCLineColor = WpfBrushes.White;
				ValueAreaColor = WpfBrushes.SlateBlue;
				LabelColor = WpfBrushes.WhiteSmoke;
				EventBullishColor = WpfBrushes.MediumSeaGreen;
				EventBearishColor = WpfBrushes.IndianRed;
				EventNeutralColor = WpfBrushes.Gold;
				CarryForwardLineOpacity = 58;
				LabelFontSize = 10;
				ShowLabels = true;
				LabelOffsetTicks = 2;

				MaxHistoricalDaysToProcess = 5;
				UpdateMode = OrcaSessionUpdateMode.PriceChange;
				RenderOnlyVisibleSessions = true;
				EnableDebugLogging = false;
			}
			else if (State == State.Configure)
			{
				Calculate = UpdateMode == OrcaSessionUpdateMode.EachTick ? Calculate.OnEachTick : Calculate.OnPriceChange;
				AddDataSeries(BarsPeriodType.Second, 30);
			}
			else if (State == State.DataLoaded)
			{
				ResetRuntimeState();
				BuildSessionDefinitions();
				easternTimeZone = FindEasternTimeZone();
			}
			else if (State == State.Terminated)
			{
				DisposeDx();
			}
		}

		private void ResetRuntimeState()
		{
			sessionsByKey.Clear();
			sessions.Clear();
			snapshots.Clear();
			pendingLabels.Clear();
			lastBid = double.NaN;
			lastAsk = double.NaN;
			previousTradePrice = double.NaN;
			lastTradeDirection = 0;
			lastVolumeBarIndex = -1;
			lastBarVolume = 0;
			lastPruneClock = DateTime.MinValue;
		}

		private TimeZoneInfo FindEasternTimeZone()
		{
			try
			{
				return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
			}
			catch
			{
				return TimeZoneInfo.Local;
			}
		}

		private void BuildSessionDefinitions()
		{
			sessionDefinitions.Clear();
			sessionDefinitions.Add(new SessionDefinition
			{
				Index = 0,
				Name = "Asia",
				Enabled = EnableAsia,
				StartTime = AsiaStartTime,
				EndTime = AsiaEndTime,
				FillBrush = AsiaColor,
				FillOpacity = AsiaOpacity,
				ShowShading = ShowSessionShading,
				ShowHighLow = ShowSessionHighLow,
				ShowMidpoint = ShowSessionMidpoint,
				ShowVWAP = ShowSessionVWAP,
				ShowOpeningRange = ShowOpeningRange,
				ShowVolumeProfile = ShowSessionVolumeProfile,
				ShowCarryForwardLevels = ShowCarryForwardLevels,
				ShowRangeProjections = ShowRangeProjections
			});
			sessionDefinitions.Add(new SessionDefinition
			{
				Index = 1,
				Name = "London",
				Enabled = EnableLondon,
				StartTime = LondonStartTime,
				EndTime = LondonEndTime,
				FillBrush = LondonColor,
				FillOpacity = LondonOpacity,
				ShowShading = ShowSessionShading,
				ShowHighLow = ShowSessionHighLow,
				ShowMidpoint = ShowSessionMidpoint,
				ShowVWAP = ShowSessionVWAP,
				ShowOpeningRange = ShowOpeningRange,
				ShowVolumeProfile = ShowSessionVolumeProfile,
				ShowCarryForwardLevels = ShowCarryForwardLevels,
				ShowRangeProjections = ShowRangeProjections
			});
			sessionDefinitions.Add(new SessionDefinition
			{
				Index = 2,
				Name = "NY",
				Enabled = EnableNewYork,
				StartTime = NewYorkStartTime,
				EndTime = NewYorkEndTime,
				FillBrush = NewYorkColor,
				FillOpacity = NewYorkOpacity,
				ShowShading = ShowSessionShading,
				ShowHighLow = ShowSessionHighLow,
				ShowMidpoint = ShowSessionMidpoint,
				ShowVWAP = ShowSessionVWAP,
				ShowOpeningRange = ShowOpeningRange,
				ShowVolumeProfile = ShowSessionVolumeProfile,
				ShowCarryForwardLevels = ShowCarryForwardLevels,
				ShowRangeProjections = ShowRangeProjections
			});
		}
		#endregion

		#region Market data and updates
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
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				UpdateOpeningRangeFromThirtySecondSeries();
				return;
			}

			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			DateTime barTime = Time[0];
			if (MaxHistoricalDaysToProcess > 0 && State == State.Historical)
			{
				DateTime cutoff = DateTime.Now.Date.AddDays(-Math.Max(1, MaxHistoricalDaysToProcess) - 2);
				if (barTime.Date < cutoff)
					return;
			}

			DateTime sessionClock = ConvertToSessionClock(barTime);
			CloseExpiredSessions(sessionClock, CurrentBar);

			SessionDefinition activeDefinition = GetActiveDefinition(sessionClock);
			if (activeDefinition == null)
			{
				UpdateLastBarVolume();
				return;
			}

			DateTime sessionStart;
			DateTime sessionEnd;
			GetSessionBounds(activeDefinition, sessionClock, out sessionStart, out sessionEnd);
			string key = BuildSessionKey(activeDefinition, sessionStart);
			SessionState state = GetOrCreateSession(activeDefinition, key, sessionStart, sessionEnd);

			double tickVolume = GetIncrementalVolume();
			double open = Open[0];
			double high = High[0];
			double low = Low[0];
			double close = Close[0];
			double typicalPrice = (high + low + close) / 3.0;
			double signedDelta = CalculateSignedDelta(close, tickVolume);

			UpdateSessionState(state, sessionClock, CurrentBar, open, high, low, close, typicalPrice, tickVolume, signedDelta);
			UpdateLastBarVolume();
			PruneOldState(sessionClock);
		}

		private void UpdateOpeningRangeFromThirtySecondSeries()
		{
			if (CurrentBars == null || CurrentBars.Length <= 1 || CurrentBars[1] < 0)
				return;

			DateTime barTime = Times[1][0];
			if (MaxHistoricalDaysToProcess > 0 && State == State.Historical)
			{
				DateTime cutoff = DateTime.Now.Date.AddDays(-Math.Max(1, MaxHistoricalDaysToProcess) - 2);
				if (barTime.Date < cutoff)
					return;
			}

			DateTime sessionClock = ConvertToSessionClock(barTime);
			CloseExpiredSessions(sessionClock, CurrentBars[0] >= 0 ? CurrentBars[0] : -1);

			SessionDefinition activeDefinition = GetActiveDefinition(sessionClock);
			if (activeDefinition == null)
				return;

			DateTime sessionStart;
			DateTime sessionEnd;
			GetSessionBounds(activeDefinition, sessionClock, out sessionStart, out sessionEnd);
			string key = BuildSessionKey(activeDefinition, sessionStart);
			SessionState state = GetOrCreateSession(activeDefinition, key, sessionStart, sessionEnd);

			UpdateOpeningRange(state, sessionClock, Highs[1][0], Lows[1][0]);
			PruneOldState(sessionClock);
		}

		private void UpdateLastBarVolume()
		{
			lastVolumeBarIndex = CurrentBar;
			lastBarVolume = Volume[0];
		}

		private double GetIncrementalVolume()
		{
			double currentVolume = Volume[0];
			if (CurrentBar != lastVolumeBarIndex)
				return Math.Max(0, currentVolume);

			double diff = currentVolume - lastBarVolume;
			return diff > 0 ? diff : 0;
		}

		private double CalculateSignedDelta(double price, double volume)
		{
			if (volume <= 0 || DeltaMode == OrcaSessionDeltaMode.Disabled)
				return 0;

			int direction = 0;
			bool bidAskAllowed = DeltaMode == OrcaSessionDeltaMode.BidAsk || DeltaMode == OrcaSessionDeltaMode.ExistingOrcaDelta;

			if (bidAskAllowed && !double.IsNaN(lastBid) && !double.IsNaN(lastAsk) && lastBid > 0 && lastAsk > 0 && lastAsk >= lastBid)
			{
				if (price >= lastAsk)
					direction = 1;
				else if (price <= lastBid)
					direction = -1;
			}

			if (direction == 0 && !double.IsNaN(previousTradePrice))
			{
				if (price > previousTradePrice)
					direction = 1;
				else if (price < previousTradePrice)
					direction = -1;
				else
					direction = lastTradeDirection;
			}

			previousTradePrice = price;
			if (direction != 0)
				lastTradeDirection = direction;

			return direction * volume;
		}

		private DateTime ConvertToSessionClock(DateTime barTime)
		{
			if (SessionTimeMode == OrcaSessionTimeMode.ChartTime)
				return barTime;
			if (SessionTimeMode == OrcaSessionTimeMode.LocalComputerTime)
				return barTime.Kind == DateTimeKind.Utc ? TimeZoneInfo.ConvertTimeFromUtc(barTime, TimeZoneInfo.Local) : barTime;

			try
			{
				TimeZoneInfo tz = easternTimeZone ?? TimeZoneInfo.Local;
				if (barTime.Kind == DateTimeKind.Utc)
					return TimeZoneInfo.ConvertTimeFromUtc(barTime, tz);
				return TimeZoneInfo.ConvertTime(barTime, TimeZoneInfo.Local, tz);
			}
			catch
			{
				return barTime;
			}
		}

		private SessionDefinition GetActiveDefinition(DateTime sessionClock)
		{
			TimeSpan tod = sessionClock.TimeOfDay;
			for (int i = 0; i < sessionDefinitions.Count; i++)
			{
				SessionDefinition definition = sessionDefinitions[i];
				if (definition == null || !definition.Enabled)
					continue;
				if (definition.StartTime == definition.EndTime)
					continue;
				if (IsInSessionWindow(tod, definition.StartTime, definition.EndTime))
					return definition;
			}
			return null;
		}

		private bool IsInSessionWindow(TimeSpan tod, TimeSpan start, TimeSpan end)
		{
			if (start < end)
				return tod >= start && tod < end;
			return tod >= start || tod < end;
		}

		private void GetSessionBounds(SessionDefinition definition, DateTime sessionClock, out DateTime start, out DateTime end)
		{
			DateTime baseDate = sessionClock.Date;
			if (definition.CrossesMidnight && sessionClock.TimeOfDay < definition.EndTime)
				baseDate = baseDate.AddDays(-1);

			start = baseDate + definition.StartTime;
			end = baseDate + definition.EndTime;
			if (definition.CrossesMidnight)
				end = end.AddDays(1);
		}

		private string BuildSessionKey(SessionDefinition definition, DateTime start)
		{
			return definition.Name + "|" + start.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		}

		private DateTime GetTradingDate(SessionDefinition definition, DateTime sessionStart, DateTime sessionEnd)
		{
			if (definition != null && definition.CrossesMidnight)
				return sessionEnd.Date;
			return sessionStart.Date;
		}

		private SessionState GetOrCreateSession(SessionDefinition definition, string key, DateTime start, DateTime end)
		{
			SessionState state;
			if (sessionsByKey.TryGetValue(key, out state))
			{
				state.IsActive = true;
				return state;
			}

			state = new SessionState
			{
				Definition = definition,
				SessionStart = start,
				SessionEnd = end,
				TradingDate = GetTradingDate(definition, start, end),
				SessionKey = key,
				IsActive = true,
				OpeningRangeEndTime = start.AddSeconds(Math.Max(1, OpeningRangeSeconds))
			};
			sessionsByKey[key] = state;
			sessions.Add(state);
			AssignNextSessionEndForSnapshots(state);
			return state;
		}

		private void UpdateSessionState(SessionState state, DateTime sessionClock, int barIndex, double open, double high, double low, double close, double typicalPrice, double volume, double signedDelta)
		{
			if (state == null)
				return;

			if (state.FirstBarIndex < 0)
				state.FirstBarIndex = barIndex;
			state.LastBarIndex = barIndex;

			if (double.IsNaN(state.Open))
				state.Open = open;
			if (double.IsNaN(state.High) || high > state.High)
				state.High = high;
			if (double.IsNaN(state.Low) || low < state.Low)
				state.Low = low;
			state.Close = close;
			state.Midpoint = !double.IsNaN(state.High) && !double.IsNaN(state.Low) ? (state.High + state.Low) * 0.5 : double.NaN;
			state.Range = !double.IsNaN(state.High) && !double.IsNaN(state.Low) ? state.High - state.Low : double.NaN;

			if (!state.OpenLocationClassified && !double.IsNaN(state.Open))
				ClassifyOpenLocation(state);

			if (volume > 0)
			{
				state.CumulativeVolume += volume;
				state.CumulativeDelta += signedDelta;
				state.SumPV += typicalPrice * volume;
				state.SumVolume += volume;
				state.SessionVWAP = state.SumVolume > 0 ? state.SumPV / state.SumVolume : double.NaN;
				AddProfileVolume(state, high, low, volume, signedDelta);
				UpdateProfileSummary(state);
			}

			if (!double.IsNaN(state.SessionVWAP))
			{
				if (double.IsNaN(state.FirstVwap))
					state.FirstVwap = state.SessionVWAP;
				state.LastVwap = state.SessionVWAP;
				if (state.VwapPoints.Count == 0 || state.VwapPoints[state.VwapPoints.Count - 1].BarIndex != barIndex)
					state.VwapPoints.Add(new VwapPoint { BarIndex = barIndex, Price = state.SessionVWAP });
				else
					state.VwapPoints[state.VwapPoints.Count - 1].Price = state.SessionVWAP;
			}

			UpdateTrendCounters(state, close);
			EvaluateContextEvents(state, high, low, close, barIndex, sessionClock);
			ClassifyTrendBalance(state, sessionClock);
		}

		private void UpdateOpeningRange(SessionState state, DateTime sessionClock, double high, double low)
		{
			if (state == null || state.OpeningRangeComplete)
				return;

			if (sessionClock <= state.OpeningRangeEndTime)
			{
				if (double.IsNaN(state.OpeningRangeHigh) || high > state.OpeningRangeHigh)
					state.OpeningRangeHigh = high;
				if (double.IsNaN(state.OpeningRangeLow) || low < state.OpeningRangeLow)
					state.OpeningRangeLow = low;
			}
			else
			{
				state.OpeningRangeComplete = true;
			}
		}

		private void AddProfileVolume(SessionState state, double high, double low, double volume, double signedDelta)
		{
			if (state == null || volume <= 0)
				return;

			double tickSize = GetSafeTickSize();
			int lowTick = PriceToTick(low);
			int highTick = PriceToTick(high);
			if (highTick < lowTick)
			{
				int tmp = highTick;
				highTick = lowTick;
				lowTick = tmp;
			}

			int tickCount = Math.Max(1, highTick - lowTick + 1);
			double perTickVolume = volume / tickCount;
			double perTickDelta = signedDelta / tickCount;

			for (int tick = lowTick; tick <= highTick; tick++)
			{
				ProfileRow row;
				if (!state.ProfileRows.TryGetValue(tick, out row))
				{
					row = new ProfileRow { PriceTick = tick, Price = tick * tickSize };
					state.ProfileRows[tick] = row;
				}

				row.Volume += perTickVolume;
				row.Delta += perTickDelta;
				if (perTickDelta >= 0)
					row.AskVolume += perTickVolume;
				else
					row.BidVolume += perTickVolume;
			}
		}

		private void UpdateProfileSummary(SessionState state)
		{
			if (state == null || state.ProfileRows.Count == 0)
				return;

			double maxVolume = 0;
			double poc = double.NaN;
			valueAreaScratch.Clear();
			foreach (ProfileRow row in state.ProfileRows.Values)
			{
				if (row.Volume > maxVolume)
				{
					maxVolume = row.Volume;
					poc = row.Price;
				}

				long roundedVolume = (long)Math.Max(1, Math.Round(row.Volume));
				valueAreaScratch[row.Price] = roundedVolume;
			}

			state.MaxProfileVolume = maxVolume;
			state.Poc = poc;
			if (!double.IsNaN(poc) && valueAreaScratch.Count > 1)
			{
				double vah;
				double val;
				if (OrcaVolumeProfileCore.TryCalculateValueArea(valueAreaScratch, poc, ValueAreaPercent, out vah, out val))
				{
					state.Vah = vah;
					state.Val = val;
				}
			}
			valueAreaScratch.Clear();
		}

		private void UpdateTrendCounters(SessionState state, double close)
		{
			if (state == null)
				return;

			state.BarsProcessed++;
			if (!double.IsNaN(state.SessionVWAP))
			{
				bool above = close > state.SessionVWAP;
				if (above)
					state.BarsAboveVwap++;
				else if (close < state.SessionVWAP)
					state.BarsBelowVwap++;

				if (state.LastAboveVwap.HasValue && state.LastAboveVwap.Value != above)
					state.VwapCrossCount++;
				state.LastAboveVwap = above;
			}

			if (!double.IsNaN(state.Midpoint))
			{
				bool aboveMid = close > state.Midpoint;
				if (state.LastAboveMidpoint.HasValue && state.LastAboveMidpoint.Value != aboveMid)
					state.MidpointCrossCount++;
				state.LastAboveMidpoint = aboveMid;
			}

			if (state.OpeningRangeComplete && !double.IsNaN(state.OpeningRangeHigh) && !double.IsNaN(state.OpeningRangeLow))
			{
				if (close > state.OpeningRangeHigh + AcceptanceTickBuffer * GetSafeTickSize())
				{
					state.OpeningRangeAboveCount++;
					state.OpeningRangeBelowCount = 0;
				}
				else if (close < state.OpeningRangeLow - AcceptanceTickBuffer * GetSafeTickSize())
				{
					state.OpeningRangeBelowCount++;
					state.OpeningRangeAboveCount = 0;
				}
				else
				{
					state.OpeningRangeAboveCount = 0;
					state.OpeningRangeBelowCount = 0;
				}
			}
		}

		private void CloseExpiredSessions(DateTime sessionClock, int currentBarIndex)
		{
			for (int i = 0; i < sessions.Count; i++)
			{
				SessionState state = sessions[i];
				if (state == null || state.IsComplete)
					continue;

				if (sessionClock >= state.SessionEnd)
					CompleteSession(state, Math.Max(0, currentBarIndex - 1));
			}
		}

		private void CompleteSession(SessionState state, int endBarIndex)
		{
			if (state == null || state.IsComplete)
				return;

			state.IsActive = false;
			state.IsComplete = true;
			if (state.LastBarIndex < 0)
				state.LastBarIndex = endBarIndex;
			if (!state.OpeningRangeComplete)
				state.OpeningRangeComplete = true;
			FillNextSessionEndpoints(state);
			CreateSnapshot(state);
		}

		private void FillNextSessionEndpoints(SessionState completedSession)
		{
			if (completedSession == null)
				return;

			for (int i = 0; i < snapshots.Count; i++)
			{
				PriorSessionSnapshot snapshot = snapshots[i];
				if (snapshot.NextSessionEndBarIndex < 0 && snapshot.NextSessionEndTime == completedSession.SessionEnd)
					snapshot.NextSessionEndBarIndex = completedSession.LastBarIndex;
			}
		}

		private void CreateSnapshot(SessionState state)
		{
			if (state == null || state.SnapshotCreated || double.IsNaN(state.High) || double.IsNaN(state.Low))
				return;

			PriorSessionSnapshot snapshot = new PriorSessionSnapshot
			{
				Name = state.Definition.Name,
				DefinitionIndex = state.Definition.Index,
				SessionStart = state.SessionStart,
				SessionEnd = state.SessionEnd,
				TradingDate = state.TradingDate,
				StartBarIndex = state.FirstBarIndex,
				EndBarIndex = state.LastBarIndex,
				High = state.High,
				Low = state.Low,
				Midpoint = state.Midpoint,
				VWAP = state.SessionVWAP,
				OpeningRangeHigh = state.OpeningRangeHigh,
				OpeningRangeLow = state.OpeningRangeLow,
				Range = state.Range,
				Volume = state.CumulativeVolume,
				Delta = state.CumulativeDelta,
				POC = state.Poc,
				VAH = state.Vah,
				VAL = state.Val
			};
			snapshots.Add(snapshot);
			AssignNextSessionEndForSnapshot(snapshot);
			state.SnapshotCreated = true;
		}

		private void AssignNextSessionEndForSnapshot(PriorSessionSnapshot snapshot)
		{
			if (snapshot == null)
				return;

			SessionState next = null;
			for (int i = 0; i < sessions.Count; i++)
			{
				SessionState candidate = sessions[i];
				if (candidate == null || candidate.SessionStart < snapshot.SessionEnd)
					continue;
				if (next == null || candidate.SessionStart < next.SessionStart)
					next = candidate;
			}

			if (next == null)
				return;

			snapshot.NextSessionEndTime = next.SessionEnd;
			if (next.IsComplete && next.LastBarIndex >= 0)
				snapshot.NextSessionEndBarIndex = next.LastBarIndex;
		}

		private void AssignNextSessionEndForSnapshots(SessionState newSession)
		{
			if (newSession == null)
				return;

			for (int i = snapshots.Count - 1; i >= 0; i--)
			{
				PriorSessionSnapshot snapshot = snapshots[i];
				if (snapshot.NextSessionEndTime != DateTime.MinValue)
					continue;
				if (snapshot.SessionEnd <= newSession.SessionStart)
				{
					snapshot.NextSessionEndTime = newSession.SessionEnd;
					snapshot.NextSessionEndBarIndex = newSession.LastBarIndex >= 0 ? newSession.LastBarIndex : -1;
				}
			}
		}

		private void PruneOldState(DateTime sessionClock)
		{
			if (LookbackDays <= 0)
				return;
			if (lastPruneClock != DateTime.MinValue && (sessionClock - lastPruneClock).TotalMinutes < 15)
				return;

			lastPruneClock = sessionClock;
			DateTime cutoff = sessionClock.Date.AddDays(-LookbackDays - 2);

			for (int i = sessions.Count - 1; i >= 0; i--)
			{
				SessionState state = sessions[i];
				if (state != null && state.IsComplete && state.SessionEnd < cutoff)
				{
					sessionsByKey.Remove(state.SessionKey);
					sessions.RemoveAt(i);
				}
			}

			for (int i = snapshots.Count - 1; i >= 0; i--)
			{
				if (snapshots[i].SessionEnd < cutoff)
					snapshots.RemoveAt(i);
			}
		}
		#endregion

		#region Context logic
		private void ClassifyOpenLocation(SessionState state)
		{
			state.OpenLocationClassified = true;
			if (!ShowOpenLocationClassification)
				return;

			List<PriorSessionSnapshot> priors = GetEligiblePriorSnapshots(state, true);
			if (priors.Count == 0)
				return;

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < priors.Count; i++)
			{
				PriorSessionSnapshot prior = priors[i];
				string relation = GetRangeRelation(state.Open, prior, NearVWAPTicks, NearMidpointTicks);
				if (sb.Length > 0)
					sb.Append(" / ");
				sb.Append(relation);
			}

			if (sb.Length == 0)
				return;

			state.OpenLocationText = state.Definition.Name + " Open: " + sb;
			state.StatusText = state.OpenLocationText;
			AddEvent(state, "Open", "Open", state.SessionStart, state.FirstBarIndex, state.Open, 0, state.OpenLocationText);
		}

		private string GetRangeRelation(double price, PriorSessionSnapshot prior, int nearVwapTicks, int nearMidTicks)
		{
			string name = prior.Name;
			if (!double.IsNaN(prior.High) && price > prior.High)
				return "Above " + name + " High";
			if (!double.IsNaN(prior.Low) && price < prior.Low)
				return "Below " + name + " Low";
			if (!double.IsNaN(prior.High) && !double.IsNaN(prior.Low) && price >= prior.Low && price <= prior.High)
			{
				if (!double.IsNaN(prior.VWAP) && IsNear(price, prior.VWAP, nearVwapTicks))
					return "Near " + name + " VWAP";
				if (!double.IsNaN(prior.Midpoint) && IsNear(price, prior.Midpoint, nearMidTicks))
					return "Near " + name + " EQ";
				return "Inside " + name;
			}
			return "Outside " + name;
		}

		private void EvaluateContextEvents(SessionState state, double high, double low, double close, int barIndex, DateTime sessionClock)
		{
			if (state == null)
				return;

			List<PriorSessionSnapshot> priors = GetEligiblePriorSnapshots(state, false);
			if (priors.Count == 0)
				return;

			if (EnableSweepDetection || EnableAcceptanceDetection)
			{
				for (int i = 0; i < priors.Count; i++)
				{
					PriorSessionSnapshot prior = priors[i];
					EvaluatePriorLevel(state, prior, prior.High, true, prior.Name + " High", high, low, close, barIndex, sessionClock);
					EvaluatePriorLevel(state, prior, prior.Low, false, prior.Name + " Low", high, low, close, barIndex, sessionClock);
					EvaluatePriorLevel(state, prior, prior.OpeningRangeHigh, true, prior.Name + " ORH", high, low, close, barIndex, sessionClock);
					EvaluatePriorLevel(state, prior, prior.OpeningRangeLow, false, prior.Name + " ORL", high, low, close, barIndex, sessionClock);
				}
			}

			UpdateCarryForwardTouches(high, low, barIndex);
		}

		private void EvaluatePriorLevel(SessionState state, PriorSessionSnapshot prior, double level, bool highLevel, string levelName, double high, double low, double close, int barIndex, DateTime sessionClock)
		{
			if (double.IsNaN(level) || double.IsInfinity(level))
				return;

			string key = prior.Name + "|" + levelName + "|" + highLevel.ToString(CultureInfo.InvariantCulture);
			SweepTracker tracker;
			if (!state.SweepTrackers.TryGetValue(key, out tracker))
			{
				tracker = new SweepTracker();
				state.SweepTrackers[key] = tracker;
			}

			double tickSize = GetSafeTickSize();
			double sweepBuffer = Math.Max(0, SweepTickBuffer) * tickSize;
			double reclaimBuffer = Math.Max(0, ReclaimTickBuffer) * tickSize;
			double acceptanceBuffer = Math.Max(0, AcceptanceTickBuffer) * tickSize;

			if (EnableSweepDetection && !tracker.Swept)
			{
				bool swept = highLevel ? high > level + sweepBuffer : low < level - sweepBuffer;
				if (swept)
				{
					tracker.Swept = true;
					string text = state.Definition.Name + " swept " + levelName;
					AddEvent(state, "Sweep", levelName, sessionClock, barIndex, level, highLevel ? 1 : -1, text);
					state.StatusText = text;
				}
			}

			if (tracker.Swept && EnableReclaimDetection && !tracker.Reclaimed)
			{
				bool reclaimNow;
				if (ReclaimConfirmationMode == OrcaSessionReclaimConfirmationMode.IntrabarTouch)
					reclaimNow = highLevel ? low <= level - reclaimBuffer : high >= level + reclaimBuffer;
				else
					reclaimNow = highLevel ? close <= level - reclaimBuffer : close >= level + reclaimBuffer;

				if (reclaimNow)
					tracker.ConsecutiveReclaimCloses++;
				else if (ReclaimConfirmationMode == OrcaSessionReclaimConfirmationMode.NBarsClose)
					tracker.ConsecutiveReclaimCloses = 0;

				int needed = ReclaimConfirmationMode == OrcaSessionReclaimConfirmationMode.NBarsClose ? Math.Max(1, ReclaimConfirmationBars) : 1;
				if (tracker.ConsecutiveReclaimCloses >= needed)
				{
					tracker.Reclaimed = true;
					string text = levelName + " Swept + Reclaimed";
					if (ShowReclaimLabels)
						AddEvent(state, "Reclaim", levelName, sessionClock, barIndex, level, highLevel ? -1 : 1, text);
					state.StatusText = text;
				}
			}

			if (tracker.Swept && EnableAcceptanceDetection && !tracker.Reclaimed && !tracker.Accepted)
			{
				bool acceptedClose = highLevel ? close > level + acceptanceBuffer : close < level - acceptanceBuffer;
				if (acceptedClose)
					tracker.ConsecutiveAcceptanceCloses++;
				else
					tracker.ConsecutiveAcceptanceCloses = 0;

				if (tracker.ConsecutiveAcceptanceCloses >= Math.Max(1, AcceptanceBars))
				{
					tracker.Accepted = true;
					string text = "Accepted " + (highLevel ? "Above " : "Below ") + levelName;
					if (ShowAcceptance)
						AddEvent(state, "Acceptance", levelName, sessionClock, barIndex, level, highLevel ? 1 : -1, text);
					state.StatusText = text;
				}
			}
		}

		private void UpdateCarryForwardTouches(double high, double low, int barIndex)
		{
			for (int i = 0; i < snapshots.Count; i++)
			{
				PriorSessionSnapshot snapshot = snapshots[i];
				if (snapshot.FirstTouchBarIndex >= 0)
					continue;

				if (TouchesLevel(high, low, snapshot.High) || TouchesLevel(high, low, snapshot.Low)
					|| TouchesLevel(high, low, snapshot.Midpoint) || TouchesLevel(high, low, snapshot.VWAP)
					|| TouchesLevel(high, low, snapshot.OpeningRangeHigh) || TouchesLevel(high, low, snapshot.OpeningRangeLow))
				{
					snapshot.FirstTouchBarIndex = barIndex;
				}
			}
		}

		private bool TouchesLevel(double high, double low, double level)
		{
			return !double.IsNaN(level) && high >= level && low <= level;
		}

		private void AddEvent(SessionState state, string type, string relatedLevel, DateTime time, int barIndex, double price, int direction, string label)
		{
			if (state == null || string.IsNullOrEmpty(label))
				return;

			state.Events.Add(new SessionEvent
			{
				EventType = type,
				RelatedLevel = relatedLevel,
				Time = time,
				BarIndex = barIndex,
				Price = price,
				Direction = direction,
				LabelText = label
			});
		}

		private List<PriorSessionSnapshot> GetEligiblePriorSnapshots(SessionState state, bool forOpen)
		{
			List<PriorSessionSnapshot> result = new List<PriorSessionSnapshot>(3);
			if (state == null)
				return result;

			if (DetectSweepsAgainst == OrcaSessionSweepScope.PreviousSessionOnly || forOpen)
			{
				PriorSessionSnapshot previous = FindPreviousSnapshot(state);
				if (previous != null)
					result.Add(previous);

				if (forOpen && state.Definition.Index == 2)
				{
					PriorSessionSnapshot asia = FindSnapshotByName(state, "Asia");
					if (asia != null && previous != asia)
						result.Add(asia);
				}
				return result;
			}

			for (int i = snapshots.Count - 1; i >= 0; i--)
			{
				PriorSessionSnapshot snapshot = snapshots[i];
				if (snapshot.SessionEnd > state.SessionStart)
					continue;

				if (DetectSweepsAgainst == OrcaSessionSweepScope.AsiaLondonIntoNY)
				{
					if (state.Definition.Index == 1 && snapshot.Name == "Asia" && snapshot.TradingDate == state.TradingDate)
						AddUniqueSnapshot(result, snapshot);
					else if (state.Definition.Index == 2 && (snapshot.Name == "Asia" || snapshot.Name == "London") && snapshot.TradingDate == state.TradingDate)
						AddUniqueSnapshot(result, snapshot);
				}
				else if (snapshot.TradingDate == state.TradingDate)
				{
					AddUniqueSnapshot(result, snapshot);
				}

				if (result.Count >= 4)
					break;
			}

			if (result.Count == 0)
			{
				PriorSessionSnapshot previous = FindPreviousSnapshot(state);
				if (previous != null)
					result.Add(previous);
			}
			return result;
		}

		private void AddUniqueSnapshot(List<PriorSessionSnapshot> list, PriorSessionSnapshot snapshot)
		{
			for (int i = 0; i < list.Count; i++)
				if (list[i] == snapshot)
					return;
			list.Add(snapshot);
		}

		private PriorSessionSnapshot FindPreviousSnapshot(SessionState state)
		{
			for (int i = snapshots.Count - 1; i >= 0; i--)
			{
				if (snapshots[i].SessionEnd <= state.SessionStart)
					return snapshots[i];
			}
			return null;
		}

		private PriorSessionSnapshot FindSnapshotByName(SessionState state, string name)
		{
			for (int i = snapshots.Count - 1; i >= 0; i--)
			{
				PriorSessionSnapshot snapshot = snapshots[i];
				if (snapshot.SessionEnd <= state.SessionStart && snapshot.TradingDate == state.TradingDate && snapshot.Name == name)
					return snapshot;
			}
			return null;
		}

		private void ClassifyTrendBalance(SessionState state, DateTime sessionClock)
		{
			if (state == null || !EnableTrendBalanceClassification)
			{
				if (state != null)
					state.Classification = OrcaSessionClassification.Unknown;
				return;
			}

			if ((sessionClock - state.SessionStart).TotalMinutes < Math.Max(1, MinimumMinutesBeforeClassification) || state.BarsProcessed < 3)
			{
				state.Classification = OrcaSessionClassification.Unknown;
				return;
			}

			int bullish = 0;
			int bearish = 0;
			int balance = 0;

			double total = Math.Max(1, state.BarsProcessed);
			double abovePct = state.BarsAboveVwap * 100.0 / total;
			double belowPct = state.BarsBelowVwap * 100.0 / total;
			if (abovePct >= VWAPDominancePercent)
				bullish++;
			if (belowPct >= VWAPDominancePercent)
				bearish++;

			if (!double.IsNaN(state.FirstVwap) && !double.IsNaN(state.LastVwap))
			{
				double slopeTicks = (state.LastVwap - state.FirstVwap) / GetSafeTickSize();
				if (slopeTicks >= 2)
					bullish++;
				else if (slopeTicks <= -2)
					bearish++;
			}

			if (!double.IsNaN(state.Range) && state.Range > 0 && !double.IsNaN(state.Close))
			{
				double location = (state.Close - state.Low) / state.Range;
				if (location >= 0.75)
					bullish++;
				else if (location <= 0.25)
					bearish++;
				else if (Math.Abs(location - 0.5) <= 0.15)
					balance++;
			}

			if (state.OpeningRangeAboveCount >= Math.Max(1, OpeningRangeAcceptanceBars))
				bullish++;
			if (state.OpeningRangeBelowCount >= Math.Max(1, OpeningRangeAcceptanceBars))
				bearish++;

			if (DeltaAlignmentEnabled)
			{
				if (state.CumulativeDelta > 0 && !double.IsNaN(state.SessionVWAP) && state.Close > state.SessionVWAP)
					bullish++;
				else if (state.CumulativeDelta < 0 && !double.IsNaN(state.SessionVWAP) && state.Close < state.SessionVWAP)
					bearish++;
			}

			if (state.VwapCrossCount >= BalanceCrossCountThreshold || state.MidpointCrossCount >= BalanceCrossCountThreshold)
				balance++;

			bool highSweep = false;
			bool lowSweep = false;
			foreach (SessionEvent ev in state.Events)
			{
				if (ev.EventType == "Sweep")
				{
					if (ev.Direction > 0)
						highSweep = true;
					if (ev.Direction < 0)
						lowSweep = true;
				}
			}
			if (highSweep && lowSweep)
				balance++;

			int threshold = Math.Max(1, TrendScoreThreshold);
			if (bullish >= threshold && bearish <= 1)
				state.Classification = state.Classification == OrcaSessionClassification.Balanced ? OrcaSessionClassification.TransitioningUp : OrcaSessionClassification.TrendUp;
			else if (bearish >= threshold && bullish <= 1)
				state.Classification = state.Classification == OrcaSessionClassification.Balanced ? OrcaSessionClassification.TransitioningDown : OrcaSessionClassification.TrendDown;
			else if ((bullish >= threshold - 1 && bearish >= threshold - 1) || balance > 0)
				state.Classification = OrcaSessionClassification.Balanced;
			else
				state.Classification = OrcaSessionClassification.Unknown;
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (chartControl == null || chartScale == null || ChartBars == null || RenderTarget == null)
				return;

			EnsureDx();
			if (!dxValid)
				return;

			float panelLeft = ChartPanel.X;
			float panelTop = ChartPanel.Y;
			float panelRight = ChartPanel.X + ChartPanel.W;
			float panelBottom = ChartPanel.Y + ChartPanel.H;
			int fromIndex = Math.Max(0, ChartBars.FromIndex);
			int toIndex = Math.Max(fromIndex, ChartBars.ToIndex);

			pendingLabels.Clear();

			for (int i = 0; i < sessions.Count; i++)
			{
				SessionState state = sessions[i];
				if (!ShouldRenderSession(state, fromIndex, toIndex))
					continue;

				DrawSessionShading(chartControl, state, fromIndex, toIndex, panelLeft, panelTop, panelRight, panelBottom);
			}

			for (int i = 0; i < sessions.Count; i++)
			{
				SessionState state = sessions[i];
				if (!ShouldRenderSession(state, fromIndex, toIndex))
					continue;

				DrawSessionVolumeProfile(chartControl, chartScale, state, fromIndex, toIndex, panelLeft, panelTop, panelRight, panelBottom);
			}

			for (int i = 0; i < sessions.Count; i++)
			{
				SessionState state = sessions[i];
				if (!ShouldRenderSession(state, fromIndex, toIndex))
					continue;

				DrawSessionLevels(chartControl, chartScale, state, fromIndex, toIndex, panelLeft, panelTop, panelRight, panelBottom);
				DrawSessionEvents(chartControl, chartScale, state, fromIndex, toIndex, panelLeft, panelTop, panelRight, panelBottom);
			}

			DrawCarryForwardLevels(chartControl, chartScale, fromIndex, toIndex, panelLeft, panelTop, panelRight, panelBottom);
			DrawRangeProjections(chartControl, chartScale, fromIndex, toIndex, panelLeft, panelTop, panelRight, panelBottom);
			DrawPendingLabels(panelLeft, panelTop, panelRight, panelBottom);

			if (ShowStatsPanel)
				DrawStatsPanel(panelLeft, panelTop, panelRight, panelBottom);
		}

		private bool ShouldRenderSession(SessionState state, int fromIndex, int toIndex)
		{
			if (state == null || state.FirstBarIndex < 0)
				return false;
			if (!RenderOnlyVisibleSessions)
				return true;
			int endIndex = state.LastBarIndex >= 0 ? state.LastBarIndex : toIndex;
			return endIndex >= fromIndex && state.FirstBarIndex <= toIndex;
		}

		private void DrawSessionShading(ChartControl chartControl, SessionState state, int fromIndex, int toIndex, float panelLeft, float panelTop, float panelRight, float panelBottom)
		{
			if (!ShowSessionShading || !state.Definition.ShowShading)
				return;

			float startX;
			float endX;
			if (!TryGetSessionXRange(chartControl, state, fromIndex, toIndex, panelLeft, panelRight, out startX, out endX))
				return;

			int brushIndex = GetSessionBrushIndex(state.Definition);
			DxSolidBrush brush = GetBrush(brushIndex);
			if (brush == null)
				return;

			float oldOpacity = brush.Opacity;
			brush.Opacity = PercentToOpacity(state.Definition.FillOpacity > 0 ? state.Definition.FillOpacity : SessionOpacity);
			RenderTarget.FillRectangle(new RectangleF(startX, panelTop, Math.Max(1, endX - startX), panelBottom - panelTop), brush);
			brush.Opacity = oldOpacity;
		}

		private void DrawSessionVolumeProfile(ChartControl chartControl, ChartScale chartScale, SessionState state, int fromIndex, int toIndex, float panelLeft, float panelTop, float panelRight, float panelBottom)
		{
			if (!ShowSessionVolumeProfile || !state.Definition.ShowVolumeProfile || state.ProfileRows.Count == 0 || state.MaxProfileVolume <= 0)
				return;

			float sessionStartX;
			float sessionEndX;
			if (!TryGetSessionXRange(chartControl, state, fromIndex, toIndex, panelLeft, panelRight, out sessionStartX, out sessionEndX))
				return;

			float sessionWidth = Math.Max(1, sessionEndX - sessionStartX);
			float profileWidth = ProfileWidthMode == OrcaSessionProfileWidthMode.FixedPixels
				? Math.Max(10, ProfileWidthPixels)
				: Math.Max(10, sessionWidth * Math.Max(1, ProfileWidthPercent) / 100f);
			float maxWidth = Math.Min(profileWidth, sessionWidth);
			float anchorX;

			if (ProfileSide == OrcaSessionProfileSide.LeftOfSession || ProfileSide == OrcaSessionProfileSide.AnchoredToSessionStart)
				anchorX = sessionStartX;
			else
				anchorX = sessionEndX;

			bool drawLeftToRight = ProfileSide == OrcaSessionProfileSide.LeftOfSession || ProfileSide == OrcaSessionProfileSide.AnchoredToSessionStart;
			BuildRenderableProfileRows(state, chartScale, panelTop, panelBottom);
			if (renderRows.Count == 0)
				return;

			double maxVolume = 0;
			for (int i = 0; i < renderRows.Count; i++)
				if (renderRows[i].Volume > maxVolume)
					maxVolume = renderRows[i].Volume;
			if (maxVolume <= 0)
				return;

			DxSolidBrush profileBrush = GetBrush(BrushProfile);
			DxSolidBrush valueAreaBrush = GetBrush(BrushValueArea);
			DxSolidBrush pocBrush = GetBrush(BrushPoc);
			if (profileBrush == null)
				return;

			for (int i = 0; i < renderRows.Count; i++)
			{
				RenderProfileRow row = renderRows[i];
				double rowTopPrice = row.Price + Math.Max(1, row.SpanTicks) * GetSafeTickSize();
				int y1 = chartScale.GetYByValue(rowTopPrice);
				int y2 = chartScale.GetYByValue(row.Price);
				float rowTop = Math.Min(y1, y2);
				float rowHeight = Math.Max(1f, Math.Abs(y2 - y1));
				if (rowTop > panelBottom || rowTop + rowHeight < panelTop)
					continue;

				float width = (float)(maxWidth * (row.Volume / maxVolume));
				if (width < MinimumProfileRowWidth)
					continue;

				DxSolidBrush brush = profileBrush;
				if (ShowProfileValueArea && valueAreaBrush != null && !double.IsNaN(state.Vah) && !double.IsNaN(state.Val)
					&& row.Price >= state.Val - GetSafeTickSize() * 0.5 && row.Price <= state.Vah + GetSafeTickSize() * 0.5)
					brush = valueAreaBrush;
				if (ShowProfilePOC && pocBrush != null && !double.IsNaN(state.Poc) && Math.Abs(row.Price - state.Poc) <= GetSafeTickSize() * 0.5)
					brush = pocBrush;

				float oldOpacity = brush.Opacity;
				float opacity = PercentToOpacity(ProfileOpacity);
				if (ProfileGradientMode == OrcaSessionProfileGradientMode.OrcaDefaultGradient)
					opacity = Math.Min(1f, opacity * (0.35f + 0.65f * (float)(row.Volume / maxVolume)));
				brush.Opacity = opacity;

				float x = drawLeftToRight ? anchorX : anchorX - width;
				RenderTarget.FillRectangle(new RectangleF(x, rowTop, width, rowHeight), brush);
				brush.Opacity = oldOpacity;
			}

			if (ShowProfilePOC && !double.IsNaN(state.Poc))
				DrawPriceLine(chartScale, state.Poc, sessionStartX, sessionEndX, BrushPoc, 1f, OrcaSessionDashStyle.Dot, "POC", true, panelTop, panelBottom);
			if (ShowProfileValueArea)
			{
				if (!double.IsNaN(state.Vah))
					DrawPriceLine(chartScale, state.Vah, sessionStartX, sessionEndX, BrushValueArea, 1f, OrcaSessionDashStyle.Dash, "VAH", false, panelTop, panelBottom);
				if (!double.IsNaN(state.Val))
					DrawPriceLine(chartScale, state.Val, sessionStartX, sessionEndX, BrushValueArea, 1f, OrcaSessionDashStyle.Dash, "VAL", false, panelTop, panelBottom);
			}
		}

		private void BuildRenderableProfileRows(SessionState state, ChartScale chartScale, float panelTop, float panelBottom)
		{
			renderRows.Clear();
			renderRowMap.Clear();
			if (state == null || state.ProfileRows.Count == 0)
				return;

			int compressionTicks = 1;
			if (UseDynamicAggregation && chartScale != null)
			{
				double visibleTicks = Math.Max(1, (chartScale.MaxValue - chartScale.MinValue) / GetSafeTickSize());
				double pixels = Math.Max(1, panelBottom - panelTop);
				double ticksPerPixel = visibleTicks / pixels;
				compressionTicks = Math.Max(1, (int)Math.Ceiling(ticksPerPixel * Math.Max(1, MinProfileRowHeight)));
			}

			if (MaxProfileRows > 0 && state.ProfileRows.Count / compressionTicks > MaxProfileRows)
				compressionTicks = Math.Max(compressionTicks, (int)Math.Ceiling(state.ProfileRows.Count / (double)MaxProfileRows));

			foreach (ProfileRow row in state.ProfileRows.Values)
			{
				int bucketTick = (int)Math.Floor(row.PriceTick / (double)compressionTicks) * compressionTicks;
				RenderProfileRow renderRow;
				if (!renderRowMap.TryGetValue(bucketTick, out renderRow))
				{
					renderRow = new RenderProfileRow { PriceTick = bucketTick, SpanTicks = compressionTicks, Price = bucketTick * GetSafeTickSize() };
					renderRowMap[bucketTick] = renderRow;
					renderRows.Add(renderRow);
				}
				renderRow.Volume += row.Volume;
				renderRow.Delta += row.Delta;
			}

			renderRows.Sort(delegate(RenderProfileRow a, RenderProfileRow b) { return a.PriceTick.CompareTo(b.PriceTick); });
		}

		private void DrawSessionLevels(ChartControl chartControl, ChartScale chartScale, SessionState state, int fromIndex, int toIndex, float panelLeft, float panelTop, float panelRight, float panelBottom)
		{
			float startX;
			float endX;
			if (!TryGetSessionXRange(chartControl, state, fromIndex, toIndex, panelLeft, panelRight, out startX, out endX))
				return;

			if (ShowSessionHighLow && state.Definition.ShowHighLow)
			{
				DrawPriceLine(chartScale, state.High, startX, endX, BrushHigh, 1.5f, OrcaSessionDashStyle.Solid, state.Definition.Name + " High", true, panelTop, panelBottom);
				DrawPriceLine(chartScale, state.Low, startX, endX, BrushLow, 1.5f, OrcaSessionDashStyle.Solid, state.Definition.Name + " Low", true, panelTop, panelBottom);
			}

			if (ShowSessionMidpoint && state.Definition.ShowMidpoint)
				DrawPriceLine(chartScale, state.Midpoint, startX, endX, BrushMidpoint, 1f, OrcaSessionDashStyle.Dash, state.Definition.Name + " EQ", true, panelTop, panelBottom);

			if (ShowOpeningRange && state.Definition.ShowOpeningRange)
			{
				DrawPriceLine(chartScale, state.OpeningRangeHigh, startX, endX, BrushOpeningRange, 1f, OrcaSessionDashStyle.DashDot, state.Definition.Name + " 30s ORH", true, panelTop, panelBottom);
				DrawPriceLine(chartScale, state.OpeningRangeLow, startX, endX, BrushOpeningRange, 1f, OrcaSessionDashStyle.DashDot, state.Definition.Name + " 30s ORL", true, panelTop, panelBottom);
			}

			if (ShowSessionVWAP && state.Definition.ShowVWAP)
				DrawVwapPath(chartControl, chartScale, state, fromIndex, toIndex, panelLeft, panelRight, panelTop, panelBottom);
		}

		private void DrawVwapPath(ChartControl chartControl, ChartScale chartScale, SessionState state, int fromIndex, int toIndex, float panelLeft, float panelRight, float panelTop, float panelBottom)
		{
			if (state == null || state.VwapPoints.Count < 1)
				return;

			DxSolidBrush brush = GetBrush(BrushVwap);
			if (brush == null)
				return;

			Vector2? previous = null;
			for (int i = 0; i < state.VwapPoints.Count; i++)
			{
				VwapPoint point = state.VwapPoints[i];
				if (point.BarIndex < fromIndex - 1 || point.BarIndex > toIndex + 1 || double.IsNaN(point.Price))
					continue;

				float x = ClampX(chartControl.GetXByBarIndex(ChartBars, point.BarIndex), panelLeft, panelRight);
				float y = chartScale.GetYByValue(point.Price);
				if (y < panelTop - 20 || y > panelBottom + 20)
					continue;

				Vector2 current = new Vector2(x, y);
				if (previous.HasValue)
					RenderTarget.DrawLine(previous.Value, current, brush, 1.6f, GetStroke(OrcaSessionDashStyle.Solid));
				previous = current;
			}

			if (!double.IsNaN(state.SessionVWAP))
			{
				float endX = GetSessionEndX(chartControl, state, fromIndex, toIndex, panelLeft, panelRight);
				AddPendingLabel(endX, chartScale.GetYByValue(state.SessionVWAP), BrushVwap, state.Definition.Name + " VWAP");
			}
		}

		private void DrawSessionEvents(ChartControl chartControl, ChartScale chartScale, SessionState state, int fromIndex, int toIndex, float panelLeft, float panelTop, float panelRight, float panelBottom)
		{
			if (!ShowLabels || state == null || state.Events.Count == 0)
				return;

			for (int i = 0; i < state.Events.Count; i++)
			{
				SessionEvent ev = state.Events[i];
				if (ev.BarIndex < fromIndex || ev.BarIndex > toIndex || double.IsNaN(ev.Price))
					continue;

				float x = ClampX(chartControl.GetXByBarIndex(ChartBars, ev.BarIndex), panelLeft, panelRight);
				float y = chartScale.GetYByValue(ev.Price + ev.Direction * LabelOffsetTicks * GetSafeTickSize());
				int brushIndex = ev.Direction > 0 ? BrushEventBullish : ev.Direction < 0 ? BrushEventBearish : BrushEventNeutral;
				AddPendingLabel(x, y, brushIndex, ev.LabelText);
			}
		}

		private void DrawCarryForwardLevels(ChartControl chartControl, ChartScale chartScale, int fromIndex, int toIndex, float panelLeft, float panelTop, float panelRight, float panelBottom)
		{
			if (!ShowCarryForwardLevels || CarryForwardMode == OrcaSessionCarryForwardMode.None)
				return;

			for (int i = 0; i < snapshots.Count; i++)
			{
				PriorSessionSnapshot snapshot = snapshots[i];
				int startBar = Math.Max(0, snapshot.EndBarIndex);
				int endBar = ResolveCarryForwardEndBar(snapshot, toIndex);
				if (endBar < fromIndex || startBar > toIndex)
					continue;

				float startX = startBar <= fromIndex ? panelLeft : ClampX(chartControl.GetXByBarIndex(ChartBars, startBar), panelLeft, panelRight);
				float endX = endBar >= toIndex ? panelRight : ClampX(chartControl.GetXByBarIndex(ChartBars, endBar), panelLeft, panelRight);
				DrawCarryLine(chartScale, snapshot.High, startX, endX, snapshot.Name + " High", panelTop, panelBottom);
				DrawCarryLine(chartScale, snapshot.Low, startX, endX, snapshot.Name + " Low", panelTop, panelBottom);
				DrawCarryLine(chartScale, snapshot.Midpoint, startX, endX, snapshot.Name + " EQ", panelTop, panelBottom);
				DrawCarryLine(chartScale, snapshot.VWAP, startX, endX, snapshot.Name + " VWAP", panelTop, panelBottom);
				DrawCarryLine(chartScale, snapshot.OpeningRangeHigh, startX, endX, snapshot.Name + " ORH", panelTop, panelBottom);
				DrawCarryLine(chartScale, snapshot.OpeningRangeLow, startX, endX, snapshot.Name + " ORL", panelTop, panelBottom);
			}
		}

		private int ResolveCarryForwardEndBar(PriorSessionSnapshot snapshot, int visibleToIndex)
		{
			if (snapshot == null)
				return visibleToIndex;
			if (CarryForwardMode == OrcaSessionCarryForwardMode.CustomBars)
				return snapshot.EndBarIndex + Math.Max(1, CarryForwardCustomBars);
			if (CarryForwardMode == OrcaSessionCarryForwardMode.UntilTouched && snapshot.FirstTouchBarIndex >= 0)
				return snapshot.FirstTouchBarIndex;
			if (CarryForwardMode == OrcaSessionCarryForwardMode.NextSessionOnly && snapshot.NextSessionEndBarIndex >= 0)
				return snapshot.NextSessionEndBarIndex;
			return visibleToIndex;
		}

		private void DrawCarryLine(ChartScale chartScale, double price, float startX, float endX, string label, float panelTop, float panelBottom)
		{
			if (double.IsNaN(price) || endX <= startX)
				return;

			DxSolidBrush brush = GetBrush(BrushCarry);
			if (brush == null)
				return;

			float oldOpacity = brush.Opacity;
			brush.Opacity = PercentToOpacity(CarryForwardLineOpacity);
			float y = chartScale.GetYByValue(price);
			if (y >= panelTop - 5 && y <= panelBottom + 5)
			{
				RenderTarget.DrawLine(new Vector2(startX, y), new Vector2(endX, y), brush, 1f, GetStroke(OrcaSessionDashStyle.Dash));
				AddPendingLabel(endX, y, BrushCarry, label);
			}
			brush.Opacity = oldOpacity;
		}

		private void DrawRangeProjections(ChartControl chartControl, ChartScale chartScale, int fromIndex, int toIndex, float panelLeft, float panelTop, float panelRight, float panelBottom)
		{
			if (!ShowRangeProjections)
				return;

			for (int i = 0; i < snapshots.Count; i++)
			{
				PriorSessionSnapshot snapshot = snapshots[i];
				int startBar = Math.Max(0, snapshot.EndBarIndex);
				int endBar = ProjectionCarryForwardMode == OrcaSessionProjectionCarryForwardMode.NextSessionOnly && snapshot.NextSessionEndBarIndex >= 0
					? snapshot.NextSessionEndBarIndex
					: toIndex;

				if (endBar < fromIndex || startBar > toIndex)
					continue;

				float startX = startBar <= fromIndex ? panelLeft : ClampX(chartControl.GetXByBarIndex(ChartBars, startBar), panelLeft, panelRight);
				float endX = endBar >= toIndex ? panelRight : ClampX(chartControl.GetXByBarIndex(ChartBars, endBar), panelLeft, panelRight);

				if (ProjectionSource == OrcaSessionProjectionSource.PriorSessionRange || ProjectionSource == OrcaSessionProjectionSource.Both)
					DrawProjectionSet(chartScale, snapshot.Name, snapshot.High, snapshot.Low, snapshot.Range, startX, endX, panelTop, panelBottom);
				if (ProjectionSource == OrcaSessionProjectionSource.OpeningRange || ProjectionSource == OrcaSessionProjectionSource.Both)
				{
					double orRange = !double.IsNaN(snapshot.OpeningRangeHigh) && !double.IsNaN(snapshot.OpeningRangeLow)
						? snapshot.OpeningRangeHigh - snapshot.OpeningRangeLow
						: double.NaN;
					DrawProjectionSet(chartScale, snapshot.Name + " OR", snapshot.OpeningRangeHigh, snapshot.OpeningRangeLow, orRange, startX, endX, panelTop, panelBottom);
				}
			}
		}

		private void DrawProjectionSet(ChartScale chartScale, string name, double high, double low, double range, float startX, float endX, float panelTop, float panelBottom)
		{
			if (double.IsNaN(high) || double.IsNaN(low) || double.IsNaN(range) || range <= 0)
				return;

			DrawProjectionLine(chartScale, high + range * ProjectionMultiplier1, startX, endX, name + " +" + ProjectionMultiplier1.ToString("0.##", CultureInfo.InvariantCulture) + "R", panelTop, panelBottom);
			DrawProjectionLine(chartScale, high + range * ProjectionMultiplier2, startX, endX, name + " +" + ProjectionMultiplier2.ToString("0.##", CultureInfo.InvariantCulture) + "R", panelTop, panelBottom);
			DrawProjectionLine(chartScale, low - range * ProjectionMultiplier1, startX, endX, name + " -" + ProjectionMultiplier1.ToString("0.##", CultureInfo.InvariantCulture) + "R", panelTop, panelBottom);
			DrawProjectionLine(chartScale, low - range * ProjectionMultiplier2, startX, endX, name + " -" + ProjectionMultiplier2.ToString("0.##", CultureInfo.InvariantCulture) + "R", panelTop, panelBottom);
		}

		private void DrawProjectionLine(ChartScale chartScale, double price, float startX, float endX, string label, float panelTop, float panelBottom)
		{
			DrawPriceLine(chartScale, price, startX, endX, BrushProjection, 1f, OrcaSessionDashStyle.Dot, label, true, panelTop, panelBottom);
		}

		private void DrawPriceLine(ChartScale chartScale, double price, float startX, float endX, int brushIndex, float width, OrcaSessionDashStyle dashStyle, string label, bool showLabel, float panelTop, float panelBottom)
		{
			if (double.IsNaN(price) || double.IsInfinity(price) || endX <= startX)
				return;

			float y = chartScale.GetYByValue(price);
			if (y < panelTop - 6 || y > panelBottom + 6)
				return;

			DxSolidBrush brush = GetBrush(brushIndex);
			if (brush == null)
				return;

			RenderTarget.DrawLine(new Vector2(startX, y), new Vector2(endX, y), brush, width, GetStroke(dashStyle));
			if (showLabel && ShowLabels && !string.IsNullOrEmpty(label))
				AddPendingLabel(endX, y, brushIndex, label);
		}

		private void DrawPendingLabels(float panelLeft, float panelTop, float panelRight, float panelBottom)
		{
			if (!ShowLabels || dxLabelFormat == null || pendingLabels.Count == 0)
				return;

			pendingLabels.Sort(delegate(PendingLabel a, PendingLabel b)
			{
				int yCompare = a.Y.CompareTo(b.Y);
				return yCompare != 0 ? yCompare : a.X.CompareTo(b.X);
			});

			float lastY = float.MinValue;
			float labelHeight = Math.Max(12f, LabelFontSize + 5f);
			for (int i = 0; i < pendingLabels.Count; i++)
			{
				PendingLabel label = pendingLabels[i];
				if (string.IsNullOrEmpty(label.Text))
					continue;

				float drawY = label.Y;
				if (lastY != float.MinValue && drawY - lastY < labelHeight)
					drawY = lastY + labelHeight;
				if (drawY + labelHeight > panelBottom)
					drawY = panelBottom - labelHeight;
				if (drawY < panelTop)
					drawY = panelTop;
				lastY = drawY;

				float width = EstimateTextWidth(label.Text, LabelFontSize);
				float x = label.X + 4f;
				if (x + width > panelRight)
					x = panelRight - width;
				if (x < panelLeft)
					x = panelLeft;

				DxSolidBrush brush = GetBrush(label.BrushIndex);
				if (brush == null)
					brush = GetBrush(BrushLabel);
				if (brush == null)
					continue;

				RenderTarget.DrawText(label.Text, dxLabelFormat, new RectangleF(x, drawY - labelHeight * 0.5f, width, labelHeight + 2f), brush);
			}
		}

		private void AddPendingLabel(float x, float y, int brushIndex, string text)
		{
			if (!ShowLabels || string.IsNullOrEmpty(text))
				return;

			pendingLabels.Add(new PendingLabel
			{
				X = x,
				Y = y,
				BrushIndex = brushIndex,
				Text = text
			});
		}

		private void DrawStatsPanel(float panelLeft, float panelTop, float panelRight, float panelBottom)
		{
			if (dxPanelFormat == null)
				return;

			string text = BuildStatsPanelText();
			if (string.IsNullOrEmpty(text))
				return;

			float width = CompactStatsPanel ? 245f : 330f;
			float height = CompactStatsPanel ? 112f : 168f;
			float margin = 12f;
			float x = StatsPanelPosition == OrcaSessionStatsPanelPosition.TopLeft || StatsPanelPosition == OrcaSessionStatsPanelPosition.BottomLeft
				? panelLeft + margin
				: panelRight - width - margin;
			float y = StatsPanelPosition == OrcaSessionStatsPanelPosition.TopLeft || StatsPanelPosition == OrcaSessionStatsPanelPosition.TopRight
				? panelTop + margin
				: panelBottom - height - margin;

			if (dxPanelBackgroundBrush != null)
			{
				float old = dxPanelBackgroundBrush.Opacity;
				dxPanelBackgroundBrush.Opacity = PercentToOpacity(StatsPanelOpacity);
				RenderTarget.FillRectangle(new RectangleF(x, y, width, height), dxPanelBackgroundBrush);
				dxPanelBackgroundBrush.Opacity = old;
			}
			if (dxPanelBorderBrush != null)
				RenderTarget.DrawRectangle(new RectangleF(x, y, width, height), dxPanelBorderBrush, 1f);

			DxSolidBrush textBrush = GetBrush(BrushLabel);
			if (textBrush != null)
				RenderTarget.DrawText(text, dxPanelFormat, new RectangleF(x + 8f, y + 7f, width - 14f, height - 10f), textBrush);
		}

		private string BuildStatsPanelText()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("ORCA Session Context Map");

			for (int i = 0; i < sessionDefinitions.Count; i++)
			{
				SessionDefinition definition = sessionDefinitions[i];
				if (definition == null || !definition.Enabled)
					continue;

				SessionState state = GetLatestSessionByDefinition(definition.Index);
				if (state == null)
					continue;
				if (ShowOnlyCurrentSessionStats && !state.IsActive)
					continue;

				if (CompactStatsPanel)
				{
					sb.Append(definition.Name);
					sb.Append(": R ");
					sb.Append(FormatPriceDistance(state.Range));
					sb.Append(" V ");
					sb.Append(FormatVolume(state.CumulativeVolume));
					sb.Append(" D ");
					sb.Append(FormatSigned(state.CumulativeDelta));
					sb.Append(" ");
					sb.Append(ClassificationToText(state.Classification));
					if (!string.IsNullOrEmpty(state.StatusText))
					{
						sb.Append(" | ");
						sb.Append(CompactStatus(state.StatusText));
					}
					sb.AppendLine();
				}
				else
				{
					sb.AppendLine(definition.Name + ":");
					sb.AppendLine("Range: " + FormatPriceDistance(state.Range) + "  Vol: " + FormatVolume(state.CumulativeVolume) + "  Delta: " + FormatSigned(state.CumulativeDelta));
					sb.AppendLine("VWAP: " + PriceVsLevelText(state.Close, state.SessionVWAP, "VWAP") + "  EQ: " + PriceVsLevelText(state.Close, state.Midpoint, "EQ"));
					sb.AppendLine("Type: " + ClassificationToText(state.Classification));
					if (!string.IsNullOrEmpty(state.StatusText))
						sb.AppendLine("Status: " + state.StatusText);
				}
			}

			return sb.ToString();
		}

		private SessionState GetLatestSessionByDefinition(int definitionIndex)
		{
			for (int i = sessions.Count - 1; i >= 0; i--)
			{
				SessionState state = sessions[i];
				if (state != null && state.Definition != null && state.Definition.Index == definitionIndex)
					return state;
			}
			return null;
		}
		#endregion

		#region Rendering helpers and DX
		private bool TryGetSessionXRange(ChartControl chartControl, SessionState state, int fromIndex, int toIndex, float panelLeft, float panelRight, out float startX, out float endX)
		{
			startX = panelLeft;
			endX = panelRight;
			if (state == null || state.FirstBarIndex < 0)
				return false;

			int startIndex = state.FirstBarIndex;
			int endIndex = state.LastBarIndex >= 0 ? state.LastBarIndex : toIndex;
			if (endIndex < fromIndex || startIndex > toIndex)
				return false;

			startX = startIndex <= fromIndex ? panelLeft : ClampX(chartControl.GetXByBarIndex(ChartBars, startIndex), panelLeft, panelRight);
			endX = endIndex >= toIndex ? panelRight : ClampX(chartControl.GetXByBarIndex(ChartBars, endIndex), panelLeft, panelRight);
			if (endX <= startX)
				endX = startX + 1;
			return true;
		}

		private float GetSessionEndX(ChartControl chartControl, SessionState state, int fromIndex, int toIndex, float panelLeft, float panelRight)
		{
			int endIndex = state.LastBarIndex >= 0 ? state.LastBarIndex : toIndex;
			if (endIndex >= toIndex)
				return panelRight;
			if (endIndex <= fromIndex)
				return panelLeft;
			return ClampX(chartControl.GetXByBarIndex(ChartBars, endIndex), panelLeft, panelRight);
		}

		private float ClampX(float x, float left, float right)
		{
			if (float.IsNaN(x) || float.IsInfinity(x))
				return left;
			if (x < left)
				return left;
			if (x > right)
				return right;
			return x;
		}

		private int GetSessionBrushIndex(SessionDefinition definition)
		{
			if (definition == null)
				return BrushAsia;
			if (definition.Index == 1)
				return BrushLondon;
			if (definition.Index == 2)
				return BrushNewYork;
			return BrushAsia;
		}

		private DxSolidBrush GetBrush(int index)
		{
			if (dxBrushes == null || index < 0 || index >= dxBrushes.Length)
				return null;
			return dxBrushes[index];
		}

		private StrokeStyle GetStroke(OrcaSessionDashStyle style)
		{
			int idx = (int)style;
			if (dxStrokes == null || idx < 0 || idx >= dxStrokes.Length)
				return null;
			return dxStrokes[idx];
		}

		private void EnsureDx()
		{
			if (RenderTarget == null)
				return;

			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxValid && dxResourceRenderTarget == currentTarget)
				return;

			if (dxValid || dxResourceRenderTarget != IntPtr.Zero)
				DisposeDx();

			try
			{
				dxBrushes = new DxSolidBrush[BrushCount];
				dxBrushes[BrushAsia] = new DxSolidBrush(RenderTarget, ToColor4(AsiaColor, 1f));
				dxBrushes[BrushLondon] = new DxSolidBrush(RenderTarget, ToColor4(LondonColor, 1f));
				dxBrushes[BrushNewYork] = new DxSolidBrush(RenderTarget, ToColor4(NewYorkColor, 1f));
				dxBrushes[BrushHigh] = new DxSolidBrush(RenderTarget, ToColor4(HighLineColor, 1f));
				dxBrushes[BrushLow] = new DxSolidBrush(RenderTarget, ToColor4(LowLineColor, 1f));
				dxBrushes[BrushMidpoint] = new DxSolidBrush(RenderTarget, ToColor4(MidpointLineColor, 1f));
				dxBrushes[BrushVwap] = new DxSolidBrush(RenderTarget, ToColor4(VWAPLineColor, 1f));
				dxBrushes[BrushOpeningRange] = new DxSolidBrush(RenderTarget, ToColor4(OpeningRangeLineColor, 1f));
				dxBrushes[BrushProjection] = new DxSolidBrush(RenderTarget, ToColor4(ProjectionLineColor, 1f));
				dxBrushes[BrushCarry] = new DxSolidBrush(RenderTarget, ToColor4(CarryForwardLineColor, 1f));
				dxBrushes[BrushProfile] = new DxSolidBrush(RenderTarget, ToColor4(ProfileColor, 1f));
				dxBrushes[BrushPoc] = new DxSolidBrush(RenderTarget, ToColor4(POCLineColor, 1f));
				dxBrushes[BrushValueArea] = new DxSolidBrush(RenderTarget, ToColor4(ValueAreaColor, 1f));
				dxBrushes[BrushLabel] = new DxSolidBrush(RenderTarget, ToColor4(LabelColor, 1f));
				dxBrushes[BrushEventBullish] = new DxSolidBrush(RenderTarget, ToColor4(EventBullishColor, 1f));
				dxBrushes[BrushEventBearish] = new DxSolidBrush(RenderTarget, ToColor4(EventBearishColor, 1f));
				dxBrushes[BrushEventNeutral] = new DxSolidBrush(RenderTarget, ToColor4(EventNeutralColor, 1f));

				dxPanelBackgroundBrush = new DxSolidBrush(RenderTarget, new Color4(0.02f, 0.025f, 0.03f, 1f));
				dxPanelBorderBrush = new DxSolidBrush(RenderTarget, new Color4(1f, 1f, 1f, 0.18f));

				dxStrokes = new StrokeStyle[4];
				dxStrokes[(int)OrcaSessionDashStyle.Solid] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = DashStyle.Solid });
				dxStrokes[(int)OrcaSessionDashStyle.Dash] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = DashStyle.Dash });
				dxStrokes[(int)OrcaSessionDashStyle.Dot] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = DashStyle.Dot });
				dxStrokes[(int)OrcaSessionDashStyle.DashDot] = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = DashStyle.DashDot });

				dxLabelFormat = new DxTextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, Math.Max(8f, (float)LabelFontSize))
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
					ParagraphAlignment = ParagraphAlignment.Center
				};
				dxPanelFormat = new DxTextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, Math.Max(8f, (float)StatsPanelFontSize))
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
					ParagraphAlignment = ParagraphAlignment.Near
				};

				dxResourceRenderTarget = currentTarget;
				dxValid = true;
			}
			catch
			{
				DisposeDx();
				dxValid = false;
			}
		}

		private void DisposeDx()
		{
			try
			{
				if (dxBrushes != null)
				{
					for (int i = 0; i < dxBrushes.Length; i++)
						if (dxBrushes[i] != null)
							dxBrushes[i].Dispose();
				}
				if (dxStrokes != null)
				{
					for (int i = 0; i < dxStrokes.Length; i++)
						if (dxStrokes[i] != null)
							dxStrokes[i].Dispose();
				}
				if (dxPanelBackgroundBrush != null)
					dxPanelBackgroundBrush.Dispose();
				if (dxPanelBorderBrush != null)
					dxPanelBorderBrush.Dispose();
				if (dxLabelFormat != null)
					dxLabelFormat.Dispose();
				if (dxPanelFormat != null)
					dxPanelFormat.Dispose();
			}
			catch { }

			dxBrushes = null;
			dxStrokes = null;
			dxPanelBackgroundBrush = null;
			dxPanelBorderBrush = null;
			dxLabelFormat = null;
			dxPanelFormat = null;
			dxResourceRenderTarget = IntPtr.Zero;
			dxValid = false;
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}

		private Color4 ToColor4(WpfBrush brush, float alpha)
		{
			WpfSolidColorBrush solid = brush as WpfSolidColorBrush;
			System.Windows.Media.Color c = solid != null ? solid.Color : WpfColors.White;
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alpha);
		}

		private float PercentToOpacity(int value)
		{
			if (value <= 0)
				return 0f;
			if (value >= 100)
				return 1f;
			return value / 100f;
		}

		private float EstimateTextWidth(string text, double fontSize)
		{
			if (string.IsNullOrEmpty(text))
				return 24f;
			return Math.Max(32f, Math.Min(260f, (float)(text.Length * Math.Max(6, fontSize) * 0.58 + 8)));
		}
		#endregion

		#region Utility
		private double GetSafeTickSize()
		{
			if (TickSize > 0 && !double.IsNaN(TickSize) && !double.IsInfinity(TickSize))
				return TickSize;
			return 0.01;
		}

		private int PriceToTick(double price)
		{
			double tickSize = GetSafeTickSize();
			return (int)Math.Round(price / tickSize, MidpointRounding.AwayFromZero);
		}

		private bool IsNear(double price, double level, int ticks)
		{
			if (double.IsNaN(price) || double.IsNaN(level))
				return false;
			return Math.Abs(price - level) <= Math.Max(0, ticks) * GetSafeTickSize();
		}

		private string FormatPriceDistance(double value)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
				return "--";
			return value.ToString("0.##", CultureInfo.InvariantCulture);
		}

		private string FormatVolume(double value)
		{
			double abs = Math.Abs(value);
			if (abs >= 1000000)
				return (value / 1000000.0).ToString("0.##", CultureInfo.InvariantCulture) + "m";
			if (abs >= 1000)
				return (value / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) + "k";
			return value.ToString("0", CultureInfo.InvariantCulture);
		}

		private string FormatSigned(double value)
		{
			if (Math.Abs(value) >= 1000)
				return (value / 1000.0).ToString("+#.#k;-#.#k;0", CultureInfo.InvariantCulture);
			return value.ToString("+#;-#;0", CultureInfo.InvariantCulture);
		}

		private string PriceVsLevelText(double price, double level, string label)
		{
			if (double.IsNaN(price) || double.IsNaN(level))
				return "--";
			if (IsNear(price, level, NearLevelTicks))
				return "At " + label;
			return price > level ? "Above" : "Below";
		}

		private string ClassificationToText(OrcaSessionClassification classification)
		{
			switch (classification)
			{
				case OrcaSessionClassification.Balanced: return "Balanced";
				case OrcaSessionClassification.TrendUp: return "Trend Up";
				case OrcaSessionClassification.TrendDown: return "Trend Down";
				case OrcaSessionClassification.TransitioningUp: return "Transitioning Up";
				case OrcaSessionClassification.TransitioningDown: return "Transitioning Down";
				default: return "Unknown";
			}
		}

		private string CompactStatus(string status)
		{
			if (string.IsNullOrEmpty(status))
				return string.Empty;
			if (status.Length <= 32)
				return status;
			return status.Substring(0, 31) + "...";
		}

		private void DebugLog(string message)
		{
			if (EnableDebugLogging)
				Print("OrcaSessionContextMap: " + message);
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Enable Asia", Order = 1, GroupName = "01. Sessions")]
		public bool EnableAsia { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "Asia Start Time", Order = 2, GroupName = "01. Sessions")]
		public TimeSpan AsiaStartTime { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "Asia End Time", Order = 3, GroupName = "01. Sessions")]
		public TimeSpan AsiaEndTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable London", Order = 4, GroupName = "01. Sessions")]
		public bool EnableLondon { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "London Start Time", Order = 5, GroupName = "01. Sessions")]
		public TimeSpan LondonStartTime { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "London End Time", Order = 6, GroupName = "01. Sessions")]
		public TimeSpan LondonEndTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable New York", Order = 7, GroupName = "01. Sessions")]
		public bool EnableNewYork { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "New York Start Time", Order = 8, GroupName = "01. Sessions")]
		public TimeSpan NewYorkStartTime { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name = "New York End Time", Order = 9, GroupName = "01. Sessions")]
		public TimeSpan NewYorkEndTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Session Time Mode", Order = 10, GroupName = "01. Sessions")]
		public OrcaSessionTimeMode SessionTimeMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 30)]
		[Display(Name = "Lookback Days", Order = 11, GroupName = "01. Sessions")]
		public int LookbackDays { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Session Shading", Order = 1, GroupName = "02. Core Visuals")]
		public bool ShowSessionShading { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Session Opacity", Order = 2, GroupName = "02. Core Visuals")]
		public int SessionOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Asia Opacity", Order = 3, GroupName = "02. Core Visuals")]
		public int AsiaOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "London Opacity", Order = 4, GroupName = "02. Core Visuals")]
		public int LondonOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "New York Opacity", Order = 5, GroupName = "02. Core Visuals")]
		public int NewYorkOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Session High/Low", Order = 6, GroupName = "02. Core Visuals")]
		public bool ShowSessionHighLow { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Session Midpoint", Order = 7, GroupName = "02. Core Visuals")]
		public bool ShowSessionMidpoint { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Session VWAP", Order = 8, GroupName = "02. Core Visuals")]
		public bool ShowSessionVWAP { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Opening Range", Order = 9, GroupName = "02. Core Visuals")]
		public bool ShowOpeningRange { get; set; }

		[NinjaScriptProperty]
		[Range(1, 3600)]
		[Display(Name = "Opening Range Seconds", Order = 10, GroupName = "02. Core Visuals")]
		public int OpeningRangeSeconds { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Carry Forward Levels", Order = 11, GroupName = "02. Core Visuals")]
		public bool ShowCarryForwardLevels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Carry Forward Mode", Order = 12, GroupName = "02. Core Visuals")]
		public OrcaSessionCarryForwardMode CarryForwardMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 5000)]
		[Display(Name = "Carry Forward Custom Bars", Order = 13, GroupName = "02. Core Visuals")]
		public int CarryForwardCustomBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Session Volume Profile", Order = 1, GroupName = "03. Volume Profile")]
		public bool ShowSessionVolumeProfile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Profile Side", Order = 2, GroupName = "03. Volume Profile")]
		public OrcaSessionProfileSide ProfileSide { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Profile Width Mode", Order = 3, GroupName = "03. Volume Profile")]
		public OrcaSessionProfileWidthMode ProfileWidthMode { get; set; }

		[NinjaScriptProperty]
		[Range(10, 800)]
		[Display(Name = "Profile Width Pixels", Order = 4, GroupName = "03. Volume Profile")]
		public int ProfileWidthPixels { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Profile Width Percent", Order = 5, GroupName = "03. Volume Profile")]
		public int ProfileWidthPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Profile Opacity", Order = 6, GroupName = "03. Volume Profile")]
		public int ProfileOpacity { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Profile Gradient Mode", Order = 7, GroupName = "03. Volume Profile")]
		public OrcaSessionProfileGradientMode ProfileGradientMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show POC", Order = 8, GroupName = "03. Volume Profile")]
		public bool ShowProfilePOC { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Value Area", Order = 9, GroupName = "03. Volume Profile")]
		public bool ShowProfileValueArea { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Value Area Percent", Order = 10, GroupName = "03. Volume Profile")]
		public int ValueAreaPercent { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Minimum Row Width", Order = 11, GroupName = "03. Volume Profile")]
		public int MinimumProfileRowWidth { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Minimum Row Height", Order = 12, GroupName = "03. Volume Profile")]
		public int MinProfileRowHeight { get; set; }

		[NinjaScriptProperty]
		[Range(20, 2000)]
		[Display(Name = "Max Profile Rows", Order = 13, GroupName = "03. Volume Profile")]
		public int MaxProfileRows { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Dynamic Aggregation", Order = 14, GroupName = "03. Volume Profile")]
		public bool UseDynamicAggregation { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Sweep Detection", Order = 1, GroupName = "04. Context Logic")]
		public bool EnableSweepDetection { get; set; }

		[NinjaScriptProperty]
		[Range(0, 20)]
		[Display(Name = "Sweep Tick Buffer", Order = 2, GroupName = "04. Context Logic")]
		public int SweepTickBuffer { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Detect Sweeps Against", Order = 3, GroupName = "04. Context Logic")]
		public OrcaSessionSweepScope DetectSweepsAgainst { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Reclaim Detection", Order = 4, GroupName = "04. Context Logic")]
		public bool EnableReclaimDetection { get; set; }

		[NinjaScriptProperty]
		[Range(0, 20)]
		[Display(Name = "Reclaim Tick Buffer", Order = 5, GroupName = "04. Context Logic")]
		public int ReclaimTickBuffer { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Reclaim Confirmation Mode", Order = 6, GroupName = "04. Context Logic")]
		public OrcaSessionReclaimConfirmationMode ReclaimConfirmationMode { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Reclaim Confirmation Bars", Order = 7, GroupName = "04. Context Logic")]
		public int ReclaimConfirmationBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Reclaim Labels", Order = 8, GroupName = "04. Context Logic")]
		public bool ShowReclaimLabels { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Acceptance Detection", Order = 9, GroupName = "04. Context Logic")]
		public bool EnableAcceptanceDetection { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Acceptance Bars", Order = 10, GroupName = "04. Context Logic")]
		public int AcceptanceBars { get; set; }

		[NinjaScriptProperty]
		[Range(0, 20)]
		[Display(Name = "Acceptance Tick Buffer", Order = 11, GroupName = "04. Context Logic")]
		public int AcceptanceTickBuffer { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Acceptance", Order = 12, GroupName = "04. Context Logic")]
		public bool ShowAcceptance { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Open Location", Order = 13, GroupName = "04. Context Logic")]
		public bool ShowOpenLocationClassification { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Near VWAP Ticks", Order = 14, GroupName = "04. Context Logic")]
		public int NearVWAPTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Near Midpoint Ticks", Order = 15, GroupName = "04. Context Logic")]
		public int NearMidpointTicks { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Near Level Ticks", Order = 16, GroupName = "04. Context Logic")]
		public int NearLevelTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Range Projections", Order = 1, GroupName = "05. Projections")]
		public bool ShowRangeProjections { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "Projection Multiplier 1", Order = 2, GroupName = "05. Projections")]
		public double ProjectionMultiplier1 { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name = "Projection Multiplier 2", Order = 3, GroupName = "05. Projections")]
		public double ProjectionMultiplier2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Projection Source", Order = 4, GroupName = "05. Projections")]
		public OrcaSessionProjectionSource ProjectionSource { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Projection Carry Forward Mode", Order = 5, GroupName = "05. Projections")]
		public OrcaSessionProjectionCarryForwardMode ProjectionCarryForwardMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Stats Panel", Order = 1, GroupName = "06. Stats Panel")]
		public bool ShowStatsPanel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Panel Position", Order = 2, GroupName = "06. Stats Panel")]
		public OrcaSessionStatsPanelPosition StatsPanelPosition { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Panel Opacity", Order = 3, GroupName = "06. Stats Panel")]
		public int StatsPanelOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(8, 30)]
		[Display(Name = "Panel Font Size", Order = 4, GroupName = "06. Stats Panel")]
		public int StatsPanelFontSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Compact Panel", Order = 5, GroupName = "06. Stats Panel")]
		public bool CompactStatsPanel { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Only Current Session", Order = 6, GroupName = "06. Stats Panel")]
		public bool ShowOnlyCurrentSessionStats { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Classification", Order = 1, GroupName = "07. Trend Balance")]
		public bool EnableTrendBalanceClassification { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "VWAP Dominance Percent", Order = 2, GroupName = "07. Trend Balance")]
		public int VWAPDominancePercent { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Trend Score Threshold", Order = 3, GroupName = "07. Trend Balance")]
		public int TrendScoreThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Balance Cross Count", Order = 4, GroupName = "07. Trend Balance")]
		public int BalanceCrossCountThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "OR Acceptance Bars", Order = 5, GroupName = "07. Trend Balance")]
		public int OpeningRangeAcceptanceBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Delta Alignment Enabled", Order = 6, GroupName = "07. Trend Balance")]
		public bool DeltaAlignmentEnabled { get; set; }

		[NinjaScriptProperty]
		[Range(0, 60)]
		[Display(Name = "Minimum Minutes", Order = 7, GroupName = "07. Trend Balance")]
		public int MinimumMinutesBeforeClassification { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Delta Mode", Order = 8, GroupName = "07. Trend Balance")]
		public OrcaSessionDeltaMode DeltaMode { get; set; }

		[XmlIgnore]
		[Display(Name = "Asia Color", Order = 1, GroupName = "08. Styling")]
		public WpfBrush AsiaColor { get; set; }
		[Browsable(false)]
		public string AsiaColorSerializable { get { return Serialize.BrushToString(AsiaColor); } set { AsiaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "London Color", Order = 2, GroupName = "08. Styling")]
		public WpfBrush LondonColor { get; set; }
		[Browsable(false)]
		public string LondonColorSerializable { get { return Serialize.BrushToString(LondonColor); } set { LondonColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "New York Color", Order = 3, GroupName = "08. Styling")]
		public WpfBrush NewYorkColor { get; set; }
		[Browsable(false)]
		public string NewYorkColorSerializable { get { return Serialize.BrushToString(NewYorkColor); } set { NewYorkColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "High Line Color", Order = 4, GroupName = "08. Styling")]
		public WpfBrush HighLineColor { get; set; }
		[Browsable(false)]
		public string HighLineColorSerializable { get { return Serialize.BrushToString(HighLineColor); } set { HighLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Low Line Color", Order = 5, GroupName = "08. Styling")]
		public WpfBrush LowLineColor { get; set; }
		[Browsable(false)]
		public string LowLineColorSerializable { get { return Serialize.BrushToString(LowLineColor); } set { LowLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Midpoint Line Color", Order = 6, GroupName = "08. Styling")]
		public WpfBrush MidpointLineColor { get; set; }
		[Browsable(false)]
		public string MidpointLineColorSerializable { get { return Serialize.BrushToString(MidpointLineColor); } set { MidpointLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "VWAP Line Color", Order = 7, GroupName = "08. Styling")]
		public WpfBrush VWAPLineColor { get; set; }
		[Browsable(false)]
		public string VWAPLineColorSerializable { get { return Serialize.BrushToString(VWAPLineColor); } set { VWAPLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Opening Range Line Color", Order = 8, GroupName = "08. Styling")]
		public WpfBrush OpeningRangeLineColor { get; set; }
		[Browsable(false)]
		public string OpeningRangeLineColorSerializable { get { return Serialize.BrushToString(OpeningRangeLineColor); } set { OpeningRangeLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Projection Line Color", Order = 9, GroupName = "08. Styling")]
		public WpfBrush ProjectionLineColor { get; set; }
		[Browsable(false)]
		public string ProjectionLineColorSerializable { get { return Serialize.BrushToString(ProjectionLineColor); } set { ProjectionLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Carry Forward Line Color", Order = 10, GroupName = "08. Styling")]
		public WpfBrush CarryForwardLineColor { get; set; }
		[Browsable(false)]
		public string CarryForwardLineColorSerializable { get { return Serialize.BrushToString(CarryForwardLineColor); } set { CarryForwardLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Profile Color", Order = 11, GroupName = "08. Styling")]
		public WpfBrush ProfileColor { get; set; }
		[Browsable(false)]
		public string ProfileColorSerializable { get { return Serialize.BrushToString(ProfileColor); } set { ProfileColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "POC Line Color", Order = 12, GroupName = "08. Styling")]
		public WpfBrush POCLineColor { get; set; }
		[Browsable(false)]
		public string POCLineColorSerializable { get { return Serialize.BrushToString(POCLineColor); } set { POCLineColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Value Area Color", Order = 13, GroupName = "08. Styling")]
		public WpfBrush ValueAreaColor { get; set; }
		[Browsable(false)]
		public string ValueAreaColorSerializable { get { return Serialize.BrushToString(ValueAreaColor); } set { ValueAreaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Label Color", Order = 14, GroupName = "08. Styling")]
		public WpfBrush LabelColor { get; set; }
		[Browsable(false)]
		public string LabelColorSerializable { get { return Serialize.BrushToString(LabelColor); } set { LabelColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Event Bullish Color", Order = 15, GroupName = "08. Styling")]
		public WpfBrush EventBullishColor { get; set; }
		[Browsable(false)]
		public string EventBullishColorSerializable { get { return Serialize.BrushToString(EventBullishColor); } set { EventBullishColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Event Bearish Color", Order = 16, GroupName = "08. Styling")]
		public WpfBrush EventBearishColor { get; set; }
		[Browsable(false)]
		public string EventBearishColorSerializable { get { return Serialize.BrushToString(EventBearishColor); } set { EventBearishColor = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Event Neutral Color", Order = 17, GroupName = "08. Styling")]
		public WpfBrush EventNeutralColor { get; set; }
		[Browsable(false)]
		public string EventNeutralColorSerializable { get { return Serialize.BrushToString(EventNeutralColor); } set { EventNeutralColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Carry Forward Line Opacity", Order = 18, GroupName = "08. Styling")]
		public int CarryForwardLineOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(6, 30)]
		[Display(Name = "Label Font Size", Order = 19, GroupName = "08. Styling")]
		public int LabelFontSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Labels", Order = 20, GroupName = "08. Styling")]
		public bool ShowLabels { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Label Offset Ticks", Order = 21, GroupName = "08. Styling")]
		public int LabelOffsetTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, 30)]
		[Display(Name = "Max Historical Days", Order = 1, GroupName = "09. Performance")]
		public int MaxHistoricalDaysToProcess { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Update Mode", Order = 2, GroupName = "09. Performance")]
		public OrcaSessionUpdateMode UpdateMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Render Only Visible Sessions", Order = 3, GroupName = "09. Performance")]
		public bool RenderOnlyVisibleSessions { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Debug Logging", Order = 4, GroupName = "09. Performance")]
		public bool EnableDebugLogging { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaSessionContextMap[] cacheOrcaSessionContextMap;
		public OrcaSessionContextMap OrcaSessionContextMap(bool enableAsia, TimeSpan asiaStartTime, TimeSpan asiaEndTime, bool enableLondon, TimeSpan londonStartTime, TimeSpan londonEndTime, bool enableNewYork, TimeSpan newYorkStartTime, TimeSpan newYorkEndTime, OrcaSessionTimeMode sessionTimeMode, int lookbackDays, bool showSessionShading, int sessionOpacity, int asiaOpacity, int londonOpacity, int newYorkOpacity, bool showSessionHighLow, bool showSessionMidpoint, bool showSessionVWAP, bool showOpeningRange, int openingRangeSeconds, bool showCarryForwardLevels, OrcaSessionCarryForwardMode carryForwardMode, int carryForwardCustomBars, bool showSessionVolumeProfile, OrcaSessionProfileSide profileSide, OrcaSessionProfileWidthMode profileWidthMode, int profileWidthPixels, int profileWidthPercent, int profileOpacity, OrcaSessionProfileGradientMode profileGradientMode, bool showProfilePOC, bool showProfileValueArea, int valueAreaPercent, int minimumProfileRowWidth, int minProfileRowHeight, int maxProfileRows, bool useDynamicAggregation, bool enableSweepDetection, int sweepTickBuffer, OrcaSessionSweepScope detectSweepsAgainst, bool enableReclaimDetection, int reclaimTickBuffer, OrcaSessionReclaimConfirmationMode reclaimConfirmationMode, int reclaimConfirmationBars, bool showReclaimLabels, bool enableAcceptanceDetection, int acceptanceBars, int acceptanceTickBuffer, bool showAcceptance, bool showOpenLocationClassification, int nearVWAPTicks, int nearMidpointTicks, int nearLevelTicks, bool showRangeProjections, double projectionMultiplier1, double projectionMultiplier2, OrcaSessionProjectionSource projectionSource, OrcaSessionProjectionCarryForwardMode projectionCarryForwardMode, bool showStatsPanel, OrcaSessionStatsPanelPosition statsPanelPosition, int statsPanelOpacity, int statsPanelFontSize, bool compactStatsPanel, bool showOnlyCurrentSessionStats, bool enableTrendBalanceClassification, int vWAPDominancePercent, int trendScoreThreshold, int balanceCrossCountThreshold, int openingRangeAcceptanceBars, bool deltaAlignmentEnabled, int minimumMinutesBeforeClassification, OrcaSessionDeltaMode deltaMode, int carryForwardLineOpacity, int labelFontSize, bool showLabels, int labelOffsetTicks, int maxHistoricalDaysToProcess, OrcaSessionUpdateMode updateMode, bool renderOnlyVisibleSessions, bool enableDebugLogging)
		{
			return OrcaSessionContextMap(Input, enableAsia, asiaStartTime, asiaEndTime, enableLondon, londonStartTime, londonEndTime, enableNewYork, newYorkStartTime, newYorkEndTime, sessionTimeMode, lookbackDays, showSessionShading, sessionOpacity, asiaOpacity, londonOpacity, newYorkOpacity, showSessionHighLow, showSessionMidpoint, showSessionVWAP, showOpeningRange, openingRangeSeconds, showCarryForwardLevels, carryForwardMode, carryForwardCustomBars, showSessionVolumeProfile, profileSide, profileWidthMode, profileWidthPixels, profileWidthPercent, profileOpacity, profileGradientMode, showProfilePOC, showProfileValueArea, valueAreaPercent, minimumProfileRowWidth, minProfileRowHeight, maxProfileRows, useDynamicAggregation, enableSweepDetection, sweepTickBuffer, detectSweepsAgainst, enableReclaimDetection, reclaimTickBuffer, reclaimConfirmationMode, reclaimConfirmationBars, showReclaimLabels, enableAcceptanceDetection, acceptanceBars, acceptanceTickBuffer, showAcceptance, showOpenLocationClassification, nearVWAPTicks, nearMidpointTicks, nearLevelTicks, showRangeProjections, projectionMultiplier1, projectionMultiplier2, projectionSource, projectionCarryForwardMode, showStatsPanel, statsPanelPosition, statsPanelOpacity, statsPanelFontSize, compactStatsPanel, showOnlyCurrentSessionStats, enableTrendBalanceClassification, vWAPDominancePercent, trendScoreThreshold, balanceCrossCountThreshold, openingRangeAcceptanceBars, deltaAlignmentEnabled, minimumMinutesBeforeClassification, deltaMode, carryForwardLineOpacity, labelFontSize, showLabels, labelOffsetTicks, maxHistoricalDaysToProcess, updateMode, renderOnlyVisibleSessions, enableDebugLogging);
		}

		public OrcaSessionContextMap OrcaSessionContextMap(ISeries<double> input, bool enableAsia, TimeSpan asiaStartTime, TimeSpan asiaEndTime, bool enableLondon, TimeSpan londonStartTime, TimeSpan londonEndTime, bool enableNewYork, TimeSpan newYorkStartTime, TimeSpan newYorkEndTime, OrcaSessionTimeMode sessionTimeMode, int lookbackDays, bool showSessionShading, int sessionOpacity, int asiaOpacity, int londonOpacity, int newYorkOpacity, bool showSessionHighLow, bool showSessionMidpoint, bool showSessionVWAP, bool showOpeningRange, int openingRangeSeconds, bool showCarryForwardLevels, OrcaSessionCarryForwardMode carryForwardMode, int carryForwardCustomBars, bool showSessionVolumeProfile, OrcaSessionProfileSide profileSide, OrcaSessionProfileWidthMode profileWidthMode, int profileWidthPixels, int profileWidthPercent, int profileOpacity, OrcaSessionProfileGradientMode profileGradientMode, bool showProfilePOC, bool showProfileValueArea, int valueAreaPercent, int minimumProfileRowWidth, int minProfileRowHeight, int maxProfileRows, bool useDynamicAggregation, bool enableSweepDetection, int sweepTickBuffer, OrcaSessionSweepScope detectSweepsAgainst, bool enableReclaimDetection, int reclaimTickBuffer, OrcaSessionReclaimConfirmationMode reclaimConfirmationMode, int reclaimConfirmationBars, bool showReclaimLabels, bool enableAcceptanceDetection, int acceptanceBars, int acceptanceTickBuffer, bool showAcceptance, bool showOpenLocationClassification, int nearVWAPTicks, int nearMidpointTicks, int nearLevelTicks, bool showRangeProjections, double projectionMultiplier1, double projectionMultiplier2, OrcaSessionProjectionSource projectionSource, OrcaSessionProjectionCarryForwardMode projectionCarryForwardMode, bool showStatsPanel, OrcaSessionStatsPanelPosition statsPanelPosition, int statsPanelOpacity, int statsPanelFontSize, bool compactStatsPanel, bool showOnlyCurrentSessionStats, bool enableTrendBalanceClassification, int vWAPDominancePercent, int trendScoreThreshold, int balanceCrossCountThreshold, int openingRangeAcceptanceBars, bool deltaAlignmentEnabled, int minimumMinutesBeforeClassification, OrcaSessionDeltaMode deltaMode, int carryForwardLineOpacity, int labelFontSize, bool showLabels, int labelOffsetTicks, int maxHistoricalDaysToProcess, OrcaSessionUpdateMode updateMode, bool renderOnlyVisibleSessions, bool enableDebugLogging)
		{
			if (cacheOrcaSessionContextMap != null)
				for (int idx = 0; idx < cacheOrcaSessionContextMap.Length; idx++)
					if (cacheOrcaSessionContextMap[idx] != null && cacheOrcaSessionContextMap[idx].EnableAsia == enableAsia && cacheOrcaSessionContextMap[idx].AsiaStartTime == asiaStartTime && cacheOrcaSessionContextMap[idx].AsiaEndTime == asiaEndTime && cacheOrcaSessionContextMap[idx].EnableLondon == enableLondon && cacheOrcaSessionContextMap[idx].LondonStartTime == londonStartTime && cacheOrcaSessionContextMap[idx].LondonEndTime == londonEndTime && cacheOrcaSessionContextMap[idx].EnableNewYork == enableNewYork && cacheOrcaSessionContextMap[idx].NewYorkStartTime == newYorkStartTime && cacheOrcaSessionContextMap[idx].NewYorkEndTime == newYorkEndTime && cacheOrcaSessionContextMap[idx].SessionTimeMode == sessionTimeMode && cacheOrcaSessionContextMap[idx].LookbackDays == lookbackDays && cacheOrcaSessionContextMap[idx].ShowSessionShading == showSessionShading && cacheOrcaSessionContextMap[idx].SessionOpacity == sessionOpacity && cacheOrcaSessionContextMap[idx].AsiaOpacity == asiaOpacity && cacheOrcaSessionContextMap[idx].LondonOpacity == londonOpacity && cacheOrcaSessionContextMap[idx].NewYorkOpacity == newYorkOpacity && cacheOrcaSessionContextMap[idx].ShowSessionHighLow == showSessionHighLow && cacheOrcaSessionContextMap[idx].ShowSessionMidpoint == showSessionMidpoint && cacheOrcaSessionContextMap[idx].ShowSessionVWAP == showSessionVWAP && cacheOrcaSessionContextMap[idx].ShowOpeningRange == showOpeningRange && cacheOrcaSessionContextMap[idx].OpeningRangeSeconds == openingRangeSeconds && cacheOrcaSessionContextMap[idx].ShowCarryForwardLevels == showCarryForwardLevels && cacheOrcaSessionContextMap[idx].CarryForwardMode == carryForwardMode && cacheOrcaSessionContextMap[idx].CarryForwardCustomBars == carryForwardCustomBars && cacheOrcaSessionContextMap[idx].ShowSessionVolumeProfile == showSessionVolumeProfile && cacheOrcaSessionContextMap[idx].ProfileSide == profileSide && cacheOrcaSessionContextMap[idx].ProfileWidthMode == profileWidthMode && cacheOrcaSessionContextMap[idx].ProfileWidthPixels == profileWidthPixels && cacheOrcaSessionContextMap[idx].ProfileWidthPercent == profileWidthPercent && cacheOrcaSessionContextMap[idx].ProfileOpacity == profileOpacity && cacheOrcaSessionContextMap[idx].ProfileGradientMode == profileGradientMode && cacheOrcaSessionContextMap[idx].ShowProfilePOC == showProfilePOC && cacheOrcaSessionContextMap[idx].ShowProfileValueArea == showProfileValueArea && cacheOrcaSessionContextMap[idx].ValueAreaPercent == valueAreaPercent && cacheOrcaSessionContextMap[idx].MinimumProfileRowWidth == minimumProfileRowWidth && cacheOrcaSessionContextMap[idx].MinProfileRowHeight == minProfileRowHeight && cacheOrcaSessionContextMap[idx].MaxProfileRows == maxProfileRows && cacheOrcaSessionContextMap[idx].UseDynamicAggregation == useDynamicAggregation && cacheOrcaSessionContextMap[idx].EnableSweepDetection == enableSweepDetection && cacheOrcaSessionContextMap[idx].SweepTickBuffer == sweepTickBuffer && cacheOrcaSessionContextMap[idx].DetectSweepsAgainst == detectSweepsAgainst && cacheOrcaSessionContextMap[idx].EnableReclaimDetection == enableReclaimDetection && cacheOrcaSessionContextMap[idx].ReclaimTickBuffer == reclaimTickBuffer && cacheOrcaSessionContextMap[idx].ReclaimConfirmationMode == reclaimConfirmationMode && cacheOrcaSessionContextMap[idx].ReclaimConfirmationBars == reclaimConfirmationBars && cacheOrcaSessionContextMap[idx].ShowReclaimLabels == showReclaimLabels && cacheOrcaSessionContextMap[idx].EnableAcceptanceDetection == enableAcceptanceDetection && cacheOrcaSessionContextMap[idx].AcceptanceBars == acceptanceBars && cacheOrcaSessionContextMap[idx].AcceptanceTickBuffer == acceptanceTickBuffer && cacheOrcaSessionContextMap[idx].ShowAcceptance == showAcceptance && cacheOrcaSessionContextMap[idx].ShowOpenLocationClassification == showOpenLocationClassification && cacheOrcaSessionContextMap[idx].NearVWAPTicks == nearVWAPTicks && cacheOrcaSessionContextMap[idx].NearMidpointTicks == nearMidpointTicks && cacheOrcaSessionContextMap[idx].NearLevelTicks == nearLevelTicks && cacheOrcaSessionContextMap[idx].ShowRangeProjections == showRangeProjections && cacheOrcaSessionContextMap[idx].ProjectionMultiplier1 == projectionMultiplier1 && cacheOrcaSessionContextMap[idx].ProjectionMultiplier2 == projectionMultiplier2 && cacheOrcaSessionContextMap[idx].ProjectionSource == projectionSource && cacheOrcaSessionContextMap[idx].ProjectionCarryForwardMode == projectionCarryForwardMode && cacheOrcaSessionContextMap[idx].ShowStatsPanel == showStatsPanel && cacheOrcaSessionContextMap[idx].StatsPanelPosition == statsPanelPosition && cacheOrcaSessionContextMap[idx].StatsPanelOpacity == statsPanelOpacity && cacheOrcaSessionContextMap[idx].StatsPanelFontSize == statsPanelFontSize && cacheOrcaSessionContextMap[idx].CompactStatsPanel == compactStatsPanel && cacheOrcaSessionContextMap[idx].ShowOnlyCurrentSessionStats == showOnlyCurrentSessionStats && cacheOrcaSessionContextMap[idx].EnableTrendBalanceClassification == enableTrendBalanceClassification && cacheOrcaSessionContextMap[idx].VWAPDominancePercent == vWAPDominancePercent && cacheOrcaSessionContextMap[idx].TrendScoreThreshold == trendScoreThreshold && cacheOrcaSessionContextMap[idx].BalanceCrossCountThreshold == balanceCrossCountThreshold && cacheOrcaSessionContextMap[idx].OpeningRangeAcceptanceBars == openingRangeAcceptanceBars && cacheOrcaSessionContextMap[idx].DeltaAlignmentEnabled == deltaAlignmentEnabled && cacheOrcaSessionContextMap[idx].MinimumMinutesBeforeClassification == minimumMinutesBeforeClassification && cacheOrcaSessionContextMap[idx].DeltaMode == deltaMode && cacheOrcaSessionContextMap[idx].CarryForwardLineOpacity == carryForwardLineOpacity && cacheOrcaSessionContextMap[idx].LabelFontSize == labelFontSize && cacheOrcaSessionContextMap[idx].ShowLabels == showLabels && cacheOrcaSessionContextMap[idx].LabelOffsetTicks == labelOffsetTicks && cacheOrcaSessionContextMap[idx].MaxHistoricalDaysToProcess == maxHistoricalDaysToProcess && cacheOrcaSessionContextMap[idx].UpdateMode == updateMode && cacheOrcaSessionContextMap[idx].RenderOnlyVisibleSessions == renderOnlyVisibleSessions && cacheOrcaSessionContextMap[idx].EnableDebugLogging == enableDebugLogging && cacheOrcaSessionContextMap[idx].EqualsInput(input))
						return cacheOrcaSessionContextMap[idx];
			return CacheIndicator<OrcaSessionContextMap>(new OrcaSessionContextMap(){ EnableAsia = enableAsia, AsiaStartTime = asiaStartTime, AsiaEndTime = asiaEndTime, EnableLondon = enableLondon, LondonStartTime = londonStartTime, LondonEndTime = londonEndTime, EnableNewYork = enableNewYork, NewYorkStartTime = newYorkStartTime, NewYorkEndTime = newYorkEndTime, SessionTimeMode = sessionTimeMode, LookbackDays = lookbackDays, ShowSessionShading = showSessionShading, SessionOpacity = sessionOpacity, AsiaOpacity = asiaOpacity, LondonOpacity = londonOpacity, NewYorkOpacity = newYorkOpacity, ShowSessionHighLow = showSessionHighLow, ShowSessionMidpoint = showSessionMidpoint, ShowSessionVWAP = showSessionVWAP, ShowOpeningRange = showOpeningRange, OpeningRangeSeconds = openingRangeSeconds, ShowCarryForwardLevels = showCarryForwardLevels, CarryForwardMode = carryForwardMode, CarryForwardCustomBars = carryForwardCustomBars, ShowSessionVolumeProfile = showSessionVolumeProfile, ProfileSide = profileSide, ProfileWidthMode = profileWidthMode, ProfileWidthPixels = profileWidthPixels, ProfileWidthPercent = profileWidthPercent, ProfileOpacity = profileOpacity, ProfileGradientMode = profileGradientMode, ShowProfilePOC = showProfilePOC, ShowProfileValueArea = showProfileValueArea, ValueAreaPercent = valueAreaPercent, MinimumProfileRowWidth = minimumProfileRowWidth, MinProfileRowHeight = minProfileRowHeight, MaxProfileRows = maxProfileRows, UseDynamicAggregation = useDynamicAggregation, EnableSweepDetection = enableSweepDetection, SweepTickBuffer = sweepTickBuffer, DetectSweepsAgainst = detectSweepsAgainst, EnableReclaimDetection = enableReclaimDetection, ReclaimTickBuffer = reclaimTickBuffer, ReclaimConfirmationMode = reclaimConfirmationMode, ReclaimConfirmationBars = reclaimConfirmationBars, ShowReclaimLabels = showReclaimLabels, EnableAcceptanceDetection = enableAcceptanceDetection, AcceptanceBars = acceptanceBars, AcceptanceTickBuffer = acceptanceTickBuffer, ShowAcceptance = showAcceptance, ShowOpenLocationClassification = showOpenLocationClassification, NearVWAPTicks = nearVWAPTicks, NearMidpointTicks = nearMidpointTicks, NearLevelTicks = nearLevelTicks, ShowRangeProjections = showRangeProjections, ProjectionMultiplier1 = projectionMultiplier1, ProjectionMultiplier2 = projectionMultiplier2, ProjectionSource = projectionSource, ProjectionCarryForwardMode = projectionCarryForwardMode, ShowStatsPanel = showStatsPanel, StatsPanelPosition = statsPanelPosition, StatsPanelOpacity = statsPanelOpacity, StatsPanelFontSize = statsPanelFontSize, CompactStatsPanel = compactStatsPanel, ShowOnlyCurrentSessionStats = showOnlyCurrentSessionStats, EnableTrendBalanceClassification = enableTrendBalanceClassification, VWAPDominancePercent = vWAPDominancePercent, TrendScoreThreshold = trendScoreThreshold, BalanceCrossCountThreshold = balanceCrossCountThreshold, OpeningRangeAcceptanceBars = openingRangeAcceptanceBars, DeltaAlignmentEnabled = deltaAlignmentEnabled, MinimumMinutesBeforeClassification = minimumMinutesBeforeClassification, DeltaMode = deltaMode, CarryForwardLineOpacity = carryForwardLineOpacity, LabelFontSize = labelFontSize, ShowLabels = showLabels, LabelOffsetTicks = labelOffsetTicks, MaxHistoricalDaysToProcess = maxHistoricalDaysToProcess, UpdateMode = updateMode, RenderOnlyVisibleSessions = renderOnlyVisibleSessions, EnableDebugLogging = enableDebugLogging }, input, ref cacheOrcaSessionContextMap);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaSessionContextMap OrcaSessionContextMap(bool enableAsia, TimeSpan asiaStartTime, TimeSpan asiaEndTime, bool enableLondon, TimeSpan londonStartTime, TimeSpan londonEndTime, bool enableNewYork, TimeSpan newYorkStartTime, TimeSpan newYorkEndTime, OrcaSessionTimeMode sessionTimeMode, int lookbackDays, bool showSessionShading, int sessionOpacity, int asiaOpacity, int londonOpacity, int newYorkOpacity, bool showSessionHighLow, bool showSessionMidpoint, bool showSessionVWAP, bool showOpeningRange, int openingRangeSeconds, bool showCarryForwardLevels, OrcaSessionCarryForwardMode carryForwardMode, int carryForwardCustomBars, bool showSessionVolumeProfile, OrcaSessionProfileSide profileSide, OrcaSessionProfileWidthMode profileWidthMode, int profileWidthPixels, int profileWidthPercent, int profileOpacity, OrcaSessionProfileGradientMode profileGradientMode, bool showProfilePOC, bool showProfileValueArea, int valueAreaPercent, int minimumProfileRowWidth, int minProfileRowHeight, int maxProfileRows, bool useDynamicAggregation, bool enableSweepDetection, int sweepTickBuffer, OrcaSessionSweepScope detectSweepsAgainst, bool enableReclaimDetection, int reclaimTickBuffer, OrcaSessionReclaimConfirmationMode reclaimConfirmationMode, int reclaimConfirmationBars, bool showReclaimLabels, bool enableAcceptanceDetection, int acceptanceBars, int acceptanceTickBuffer, bool showAcceptance, bool showOpenLocationClassification, int nearVWAPTicks, int nearMidpointTicks, int nearLevelTicks, bool showRangeProjections, double projectionMultiplier1, double projectionMultiplier2, OrcaSessionProjectionSource projectionSource, OrcaSessionProjectionCarryForwardMode projectionCarryForwardMode, bool showStatsPanel, OrcaSessionStatsPanelPosition statsPanelPosition, int statsPanelOpacity, int statsPanelFontSize, bool compactStatsPanel, bool showOnlyCurrentSessionStats, bool enableTrendBalanceClassification, int vWAPDominancePercent, int trendScoreThreshold, int balanceCrossCountThreshold, int openingRangeAcceptanceBars, bool deltaAlignmentEnabled, int minimumMinutesBeforeClassification, OrcaSessionDeltaMode deltaMode, int carryForwardLineOpacity, int labelFontSize, bool showLabels, int labelOffsetTicks, int maxHistoricalDaysToProcess, OrcaSessionUpdateMode updateMode, bool renderOnlyVisibleSessions, bool enableDebugLogging)
		{
			return indicator.OrcaSessionContextMap(Input, enableAsia, asiaStartTime, asiaEndTime, enableLondon, londonStartTime, londonEndTime, enableNewYork, newYorkStartTime, newYorkEndTime, sessionTimeMode, lookbackDays, showSessionShading, sessionOpacity, asiaOpacity, londonOpacity, newYorkOpacity, showSessionHighLow, showSessionMidpoint, showSessionVWAP, showOpeningRange, openingRangeSeconds, showCarryForwardLevels, carryForwardMode, carryForwardCustomBars, showSessionVolumeProfile, profileSide, profileWidthMode, profileWidthPixels, profileWidthPercent, profileOpacity, profileGradientMode, showProfilePOC, showProfileValueArea, valueAreaPercent, minimumProfileRowWidth, minProfileRowHeight, maxProfileRows, useDynamicAggregation, enableSweepDetection, sweepTickBuffer, detectSweepsAgainst, enableReclaimDetection, reclaimTickBuffer, reclaimConfirmationMode, reclaimConfirmationBars, showReclaimLabels, enableAcceptanceDetection, acceptanceBars, acceptanceTickBuffer, showAcceptance, showOpenLocationClassification, nearVWAPTicks, nearMidpointTicks, nearLevelTicks, showRangeProjections, projectionMultiplier1, projectionMultiplier2, projectionSource, projectionCarryForwardMode, showStatsPanel, statsPanelPosition, statsPanelOpacity, statsPanelFontSize, compactStatsPanel, showOnlyCurrentSessionStats, enableTrendBalanceClassification, vWAPDominancePercent, trendScoreThreshold, balanceCrossCountThreshold, openingRangeAcceptanceBars, deltaAlignmentEnabled, minimumMinutesBeforeClassification, deltaMode, carryForwardLineOpacity, labelFontSize, showLabels, labelOffsetTicks, maxHistoricalDaysToProcess, updateMode, renderOnlyVisibleSessions, enableDebugLogging);
		}

		public Indicators.OrcaSessionContextMap OrcaSessionContextMap(ISeries<double> input , bool enableAsia, TimeSpan asiaStartTime, TimeSpan asiaEndTime, bool enableLondon, TimeSpan londonStartTime, TimeSpan londonEndTime, bool enableNewYork, TimeSpan newYorkStartTime, TimeSpan newYorkEndTime, OrcaSessionTimeMode sessionTimeMode, int lookbackDays, bool showSessionShading, int sessionOpacity, int asiaOpacity, int londonOpacity, int newYorkOpacity, bool showSessionHighLow, bool showSessionMidpoint, bool showSessionVWAP, bool showOpeningRange, int openingRangeSeconds, bool showCarryForwardLevels, OrcaSessionCarryForwardMode carryForwardMode, int carryForwardCustomBars, bool showSessionVolumeProfile, OrcaSessionProfileSide profileSide, OrcaSessionProfileWidthMode profileWidthMode, int profileWidthPixels, int profileWidthPercent, int profileOpacity, OrcaSessionProfileGradientMode profileGradientMode, bool showProfilePOC, bool showProfileValueArea, int valueAreaPercent, int minimumProfileRowWidth, int minProfileRowHeight, int maxProfileRows, bool useDynamicAggregation, bool enableSweepDetection, int sweepTickBuffer, OrcaSessionSweepScope detectSweepsAgainst, bool enableReclaimDetection, int reclaimTickBuffer, OrcaSessionReclaimConfirmationMode reclaimConfirmationMode, int reclaimConfirmationBars, bool showReclaimLabels, bool enableAcceptanceDetection, int acceptanceBars, int acceptanceTickBuffer, bool showAcceptance, bool showOpenLocationClassification, int nearVWAPTicks, int nearMidpointTicks, int nearLevelTicks, bool showRangeProjections, double projectionMultiplier1, double projectionMultiplier2, OrcaSessionProjectionSource projectionSource, OrcaSessionProjectionCarryForwardMode projectionCarryForwardMode, bool showStatsPanel, OrcaSessionStatsPanelPosition statsPanelPosition, int statsPanelOpacity, int statsPanelFontSize, bool compactStatsPanel, bool showOnlyCurrentSessionStats, bool enableTrendBalanceClassification, int vWAPDominancePercent, int trendScoreThreshold, int balanceCrossCountThreshold, int openingRangeAcceptanceBars, bool deltaAlignmentEnabled, int minimumMinutesBeforeClassification, OrcaSessionDeltaMode deltaMode, int carryForwardLineOpacity, int labelFontSize, bool showLabels, int labelOffsetTicks, int maxHistoricalDaysToProcess, OrcaSessionUpdateMode updateMode, bool renderOnlyVisibleSessions, bool enableDebugLogging)
		{
			return indicator.OrcaSessionContextMap(input, enableAsia, asiaStartTime, asiaEndTime, enableLondon, londonStartTime, londonEndTime, enableNewYork, newYorkStartTime, newYorkEndTime, sessionTimeMode, lookbackDays, showSessionShading, sessionOpacity, asiaOpacity, londonOpacity, newYorkOpacity, showSessionHighLow, showSessionMidpoint, showSessionVWAP, showOpeningRange, openingRangeSeconds, showCarryForwardLevels, carryForwardMode, carryForwardCustomBars, showSessionVolumeProfile, profileSide, profileWidthMode, profileWidthPixels, profileWidthPercent, profileOpacity, profileGradientMode, showProfilePOC, showProfileValueArea, valueAreaPercent, minimumProfileRowWidth, minProfileRowHeight, maxProfileRows, useDynamicAggregation, enableSweepDetection, sweepTickBuffer, detectSweepsAgainst, enableReclaimDetection, reclaimTickBuffer, reclaimConfirmationMode, reclaimConfirmationBars, showReclaimLabels, enableAcceptanceDetection, acceptanceBars, acceptanceTickBuffer, showAcceptance, showOpenLocationClassification, nearVWAPTicks, nearMidpointTicks, nearLevelTicks, showRangeProjections, projectionMultiplier1, projectionMultiplier2, projectionSource, projectionCarryForwardMode, showStatsPanel, statsPanelPosition, statsPanelOpacity, statsPanelFontSize, compactStatsPanel, showOnlyCurrentSessionStats, enableTrendBalanceClassification, vWAPDominancePercent, trendScoreThreshold, balanceCrossCountThreshold, openingRangeAcceptanceBars, deltaAlignmentEnabled, minimumMinutesBeforeClassification, deltaMode, carryForwardLineOpacity, labelFontSize, showLabels, labelOffsetTicks, maxHistoricalDaysToProcess, updateMode, renderOnlyVisibleSessions, enableDebugLogging);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaSessionContextMap OrcaSessionContextMap(bool enableAsia, TimeSpan asiaStartTime, TimeSpan asiaEndTime, bool enableLondon, TimeSpan londonStartTime, TimeSpan londonEndTime, bool enableNewYork, TimeSpan newYorkStartTime, TimeSpan newYorkEndTime, OrcaSessionTimeMode sessionTimeMode, int lookbackDays, bool showSessionShading, int sessionOpacity, int asiaOpacity, int londonOpacity, int newYorkOpacity, bool showSessionHighLow, bool showSessionMidpoint, bool showSessionVWAP, bool showOpeningRange, int openingRangeSeconds, bool showCarryForwardLevels, OrcaSessionCarryForwardMode carryForwardMode, int carryForwardCustomBars, bool showSessionVolumeProfile, OrcaSessionProfileSide profileSide, OrcaSessionProfileWidthMode profileWidthMode, int profileWidthPixels, int profileWidthPercent, int profileOpacity, OrcaSessionProfileGradientMode profileGradientMode, bool showProfilePOC, bool showProfileValueArea, int valueAreaPercent, int minimumProfileRowWidth, int minProfileRowHeight, int maxProfileRows, bool useDynamicAggregation, bool enableSweepDetection, int sweepTickBuffer, OrcaSessionSweepScope detectSweepsAgainst, bool enableReclaimDetection, int reclaimTickBuffer, OrcaSessionReclaimConfirmationMode reclaimConfirmationMode, int reclaimConfirmationBars, bool showReclaimLabels, bool enableAcceptanceDetection, int acceptanceBars, int acceptanceTickBuffer, bool showAcceptance, bool showOpenLocationClassification, int nearVWAPTicks, int nearMidpointTicks, int nearLevelTicks, bool showRangeProjections, double projectionMultiplier1, double projectionMultiplier2, OrcaSessionProjectionSource projectionSource, OrcaSessionProjectionCarryForwardMode projectionCarryForwardMode, bool showStatsPanel, OrcaSessionStatsPanelPosition statsPanelPosition, int statsPanelOpacity, int statsPanelFontSize, bool compactStatsPanel, bool showOnlyCurrentSessionStats, bool enableTrendBalanceClassification, int vWAPDominancePercent, int trendScoreThreshold, int balanceCrossCountThreshold, int openingRangeAcceptanceBars, bool deltaAlignmentEnabled, int minimumMinutesBeforeClassification, OrcaSessionDeltaMode deltaMode, int carryForwardLineOpacity, int labelFontSize, bool showLabels, int labelOffsetTicks, int maxHistoricalDaysToProcess, OrcaSessionUpdateMode updateMode, bool renderOnlyVisibleSessions, bool enableDebugLogging)
		{
			return indicator.OrcaSessionContextMap(Input, enableAsia, asiaStartTime, asiaEndTime, enableLondon, londonStartTime, londonEndTime, enableNewYork, newYorkStartTime, newYorkEndTime, sessionTimeMode, lookbackDays, showSessionShading, sessionOpacity, asiaOpacity, londonOpacity, newYorkOpacity, showSessionHighLow, showSessionMidpoint, showSessionVWAP, showOpeningRange, openingRangeSeconds, showCarryForwardLevels, carryForwardMode, carryForwardCustomBars, showSessionVolumeProfile, profileSide, profileWidthMode, profileWidthPixels, profileWidthPercent, profileOpacity, profileGradientMode, showProfilePOC, showProfileValueArea, valueAreaPercent, minimumProfileRowWidth, minProfileRowHeight, maxProfileRows, useDynamicAggregation, enableSweepDetection, sweepTickBuffer, detectSweepsAgainst, enableReclaimDetection, reclaimTickBuffer, reclaimConfirmationMode, reclaimConfirmationBars, showReclaimLabels, enableAcceptanceDetection, acceptanceBars, acceptanceTickBuffer, showAcceptance, showOpenLocationClassification, nearVWAPTicks, nearMidpointTicks, nearLevelTicks, showRangeProjections, projectionMultiplier1, projectionMultiplier2, projectionSource, projectionCarryForwardMode, showStatsPanel, statsPanelPosition, statsPanelOpacity, statsPanelFontSize, compactStatsPanel, showOnlyCurrentSessionStats, enableTrendBalanceClassification, vWAPDominancePercent, trendScoreThreshold, balanceCrossCountThreshold, openingRangeAcceptanceBars, deltaAlignmentEnabled, minimumMinutesBeforeClassification, deltaMode, carryForwardLineOpacity, labelFontSize, showLabels, labelOffsetTicks, maxHistoricalDaysToProcess, updateMode, renderOnlyVisibleSessions, enableDebugLogging);
		}

		public Indicators.OrcaSessionContextMap OrcaSessionContextMap(ISeries<double> input , bool enableAsia, TimeSpan asiaStartTime, TimeSpan asiaEndTime, bool enableLondon, TimeSpan londonStartTime, TimeSpan londonEndTime, bool enableNewYork, TimeSpan newYorkStartTime, TimeSpan newYorkEndTime, OrcaSessionTimeMode sessionTimeMode, int lookbackDays, bool showSessionShading, int sessionOpacity, int asiaOpacity, int londonOpacity, int newYorkOpacity, bool showSessionHighLow, bool showSessionMidpoint, bool showSessionVWAP, bool showOpeningRange, int openingRangeSeconds, bool showCarryForwardLevels, OrcaSessionCarryForwardMode carryForwardMode, int carryForwardCustomBars, bool showSessionVolumeProfile, OrcaSessionProfileSide profileSide, OrcaSessionProfileWidthMode profileWidthMode, int profileWidthPixels, int profileWidthPercent, int profileOpacity, OrcaSessionProfileGradientMode profileGradientMode, bool showProfilePOC, bool showProfileValueArea, int valueAreaPercent, int minimumProfileRowWidth, int minProfileRowHeight, int maxProfileRows, bool useDynamicAggregation, bool enableSweepDetection, int sweepTickBuffer, OrcaSessionSweepScope detectSweepsAgainst, bool enableReclaimDetection, int reclaimTickBuffer, OrcaSessionReclaimConfirmationMode reclaimConfirmationMode, int reclaimConfirmationBars, bool showReclaimLabels, bool enableAcceptanceDetection, int acceptanceBars, int acceptanceTickBuffer, bool showAcceptance, bool showOpenLocationClassification, int nearVWAPTicks, int nearMidpointTicks, int nearLevelTicks, bool showRangeProjections, double projectionMultiplier1, double projectionMultiplier2, OrcaSessionProjectionSource projectionSource, OrcaSessionProjectionCarryForwardMode projectionCarryForwardMode, bool showStatsPanel, OrcaSessionStatsPanelPosition statsPanelPosition, int statsPanelOpacity, int statsPanelFontSize, bool compactStatsPanel, bool showOnlyCurrentSessionStats, bool enableTrendBalanceClassification, int vWAPDominancePercent, int trendScoreThreshold, int balanceCrossCountThreshold, int openingRangeAcceptanceBars, bool deltaAlignmentEnabled, int minimumMinutesBeforeClassification, OrcaSessionDeltaMode deltaMode, int carryForwardLineOpacity, int labelFontSize, bool showLabels, int labelOffsetTicks, int maxHistoricalDaysToProcess, OrcaSessionUpdateMode updateMode, bool renderOnlyVisibleSessions, bool enableDebugLogging)
		{
			return indicator.OrcaSessionContextMap(input, enableAsia, asiaStartTime, asiaEndTime, enableLondon, londonStartTime, londonEndTime, enableNewYork, newYorkStartTime, newYorkEndTime, sessionTimeMode, lookbackDays, showSessionShading, sessionOpacity, asiaOpacity, londonOpacity, newYorkOpacity, showSessionHighLow, showSessionMidpoint, showSessionVWAP, showOpeningRange, openingRangeSeconds, showCarryForwardLevels, carryForwardMode, carryForwardCustomBars, showSessionVolumeProfile, profileSide, profileWidthMode, profileWidthPixels, profileWidthPercent, profileOpacity, profileGradientMode, showProfilePOC, showProfileValueArea, valueAreaPercent, minimumProfileRowWidth, minProfileRowHeight, maxProfileRows, useDynamicAggregation, enableSweepDetection, sweepTickBuffer, detectSweepsAgainst, enableReclaimDetection, reclaimTickBuffer, reclaimConfirmationMode, reclaimConfirmationBars, showReclaimLabels, enableAcceptanceDetection, acceptanceBars, acceptanceTickBuffer, showAcceptance, showOpenLocationClassification, nearVWAPTicks, nearMidpointTicks, nearLevelTicks, showRangeProjections, projectionMultiplier1, projectionMultiplier2, projectionSource, projectionCarryForwardMode, showStatsPanel, statsPanelPosition, statsPanelOpacity, statsPanelFontSize, compactStatsPanel, showOnlyCurrentSessionStats, enableTrendBalanceClassification, vWAPDominancePercent, trendScoreThreshold, balanceCrossCountThreshold, openingRangeAcceptanceBars, deltaAlignmentEnabled, minimumMinutesBeforeClassification, deltaMode, carryForwardLineOpacity, labelFontSize, showLabels, labelOffsetTicks, maxHistoricalDaysToProcess, updateMode, renderOnlyVisibleSessions, enableDebugLogging);
		}
	}
}

#endregion
