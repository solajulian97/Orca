#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using DxSolidColorBrush = SharpDX.Direct2D1.SolidColorBrush;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum OrcaHistoricalReplayCoverage
	{
		LiveOnly = 0,
		CurrentTradingSession = 1,
		LoadedChartHistory = 2
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Chart-only coordinator for bounded, same-chart instant replay. Live NinjaScript
	/// processing is never paused; replay publishes isolated view state and masks later bars.
	/// </summary>
	public class OrcaInstantReplay : Indicator
	{
		private const long EstimatedEventBytes = 64;
		private const long Megabyte = 1024L * 1024L;
		private const int VisualFrameMilliseconds = 33;

		private sealed class TapeSegment
		{
			public DateTime StartTime;
			public DateTime EndTime;
			public long EstimatedBytes;
			public bool IsSealed;
			public bool IsHistorical;
			public readonly List<OrcaReplayTradeEvent> Events = new List<OrcaReplayTradeEvent>();
		}

		private sealed class SyntheticBar
		{
			public int BarIndex = -1;
			public double Open;
			public double High;
			public double Low;
			public double Close;
			public long Volume;
			public double ReferenceVolume;

			public SyntheticBar Copy()
			{
				return new SyntheticBar { BarIndex = BarIndex, Open = Open, High = High,
					Low = Low, Close = Close, Volume = Volume, ReferenceVolume = ReferenceVolume };
			}
		}

		private readonly object tapeSync = new object();
		private readonly string diagnosticsInstanceId = Guid.NewGuid().ToString("N");
		private bool diagnosticsRegistered;
		private readonly List<TapeSegment> tapeSegments = new List<TapeSegment>();
		private readonly List<TapeSegment> historicalBuilderSegments = new List<TapeSegment>();
		private long tapeBytes;
		private long historicalBuilderBytes;
		private long nextSequence;
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private SessionIterator replaySessionIterator;
		private bool historicalSeedFinalized;
		private bool historicalTickReplayAvailable;
		private int historicalSeedEventCount;
		private OrcaReplayTradeEvent transitionBoundaryEvent;
		private bool transitionDedupPending;
		private string historicalSeedStatus = string.Empty;

		private OrcaReplayLifecycle lifecycle = OrcaReplayLifecycle.Idle;
		private OrcaReplayMode replayMode = OrcaReplayMode.Bar;
		private int selectionBarIndex = -1;
		private int replayBarIndex = -1;
		private DateTime selectionTime = DateTime.MinValue;
		private DateTime replayTime = DateTime.MinValue;
		private long replaySequence;
		private double replaySpeed = 1.0;
		private bool tickModeAvailable;
		private string replayStatus = "Capture active";
		private List<OrcaReplayTradeEvent> playbackEvents = new List<OrcaReplayTradeEvent>();
		private int playbackPosition = -1;
		private SyntheticBar syntheticBar;
		private DispatcherTimer playbackTimer;
		private DateTime lastTimerWallUtc;
		private double accumulatedBarSeconds;
		private bool coordinatorRegistered;
		private bool chartTraderWasEnabled;
		private bool chartTraderStateCaptured;
		private CancellationTokenSource replayCancellation;
		private readonly Dictionary<IChartObject, bool> replaySurfaceVisibility = new Dictionary<IChartObject, bool>();

		private Grid toolbar;
		private Button replayButton;
		private Button previousButton;
		private Button playPauseButton;
		private Button stepButton;
		private Button jumpLiveButton;
		private ComboBox modeCombo;
		private ComboBox speedCombo;
		private TextBlock statusText;
		private Border lockBanner;

		private DxSolidColorBrush maskBrushDx;
		private DxSolidColorBrush bullishBrushDx;
		private DxSolidColorBrush bearishBrushDx;
		private DxSolidColorBrush wickBrushDx;
		private DxSolidColorBrush selectionBrushDx;
		private TextFormat bannerFormatDx;

		#region Properties
		[NinjaScriptProperty]
		[Range(5, 480)]
		[Display(Name = "Retention minutes", GroupName = "Replay", Order = 0)]
		public int RetentionMinutes { get; set; }

		[NinjaScriptProperty]
		[Range(16, 512)]
		[Display(Name = "Persistent memory cap MB", GroupName = "Replay", Order = 1)]
		public int MemoryCapMb { get; set; }

		[NinjaScriptProperty]
		[Range(16, 256)]
		[Display(Name = "Replay working headroom MB", GroupName = "Replay", Order = 2)]
		public int WorkingHeadroomMb { get; set; }

		[NinjaScriptProperty]
		[Range(1, 30)]
		[Display(Name = "Checkpoint segment minutes", GroupName = "Replay", Order = 3)]
		public int CheckpointMinutes { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Historical replay coverage", GroupName = "Replay", Order = 4,
			Description = "Seeds replay from NinjaTrader Tick Replay while the chart loads. Current trading session is recommended; Loaded chart history remains bounded by the memory cap.")]
		public OrcaHistoricalReplayCoverage HistoricalCoverage { get; set; }

		[XmlIgnore]
		[Display(Name = "Replay mask", GroupName = "Style", Order = 0)]
		public System.Windows.Media.Brush ReplayMaskBrush { get; set; }

		[Browsable(false)]
		public string ReplayMaskBrushSerializable
		{
			get { return Serialize.BrushToString(ReplayMaskBrush); }
			set { ReplayMaskBrush = Serialize.StringToBrush(value); }
		}
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaInstantReplay";
				Description = "Live-safe, chart-local instant market replay coordinator.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				IsChartOnly = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = false;
				RetentionMinutes = 60;
				MemoryCapMb = 128;
				WorkingHeadroomMb = 64;
				CheckpointMinutes = 5;
				HistoricalCoverage = OrcaHistoricalReplayCoverage.CurrentTradingSession;
				ReplayMaskBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(12, 12, 14));
			}
			else if (State == State.DataLoaded)
			{
				replaySessionIterator = BarsArray != null && BarsArray.Length > 0 && BarsArray[0] != null
					? new SessionIterator(BarsArray[0]) : null;
				historicalTickReplayAvailable = IsPrimaryTickReplayEnabled();
				historicalSeedFinalized = false;
				historicalSeedEventCount = 0;
				historicalSeedStatus = string.Empty;
				lastBid = lastAsk = double.NaN;
				nextSequence = 0;
				lock (tapeSync)
				{
					tapeSegments.Clear();
					tapeBytes = 0;
					historicalBuilderSegments.Clear();
					historicalBuilderBytes = 0;
				}
				transitionBoundaryEvent = null;
				transitionDedupPending = false;
			}
			else if (State == State.Historical)
			{
				if (ChartControl == null) return;
				historicalTickReplayAvailable = IsPrimaryTickReplayEnabled();
				EnsureDiagnosticsRegistered();
				OrcaDiagnosticsCore.ReportState(diagnosticsInstanceId, State.ToString());
				replayStatus = HistoricalCoverage == OrcaHistoricalReplayCoverage.LiveOnly
					? "Historical seeding off — live capture begins at realtime"
					: historicalTickReplayAvailable
						? "Loading historical Tick Replay coverage…"
						: "Historical seeding unavailable — enable Tick Replay";
				SetZOrder(int.MaxValue - 32);
				ChartControl.Dispatcher.InvokeAsync(AttachChartUi);
			}
			else if (State == State.Transition)
			{
				FinalizeHistoricalTape();
			}
			else if (State == State.Realtime)
			{
				if (!historicalSeedFinalized) FinalizeHistoricalTape();
				if (ChartControl != null) ChartControl.Dispatcher.InvokeAsync(UpdateUiAndPublish);
			}
			else if (State == State.Terminated)
			{
				CancelReplayAndReturnLive(false);
				if (ChartControl != null)
					ChartControl.Dispatcher.InvokeAsync(DetachChartUi);
				DisposeDxResources();
				historicalBuilderSegments.Clear();
				replaySessionIterator = null;
				OrcaDiagnosticsCore.UnregisterInstance(diagnosticsInstanceId);
			}
		}

		protected override void OnBarUpdate()
		{
			// Primary bars continue to update normally behind the replay view. Exact trade-to-bar
			// mapping is captured in OnMarketData; no secondary series is added here.
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			long diagnosticsWorkStart = 0;
			if (e != null && OrcaDiagnosticsCore.IsEnabled)
			{
				EnsureDiagnosticsRegistered();
				long diagnosticsSequence = OrcaDiagnosticsCore.ReportMarketData(diagnosticsInstanceId,
					e.MarketDataType, e.Time == DateTime.MinValue ? DateTime.Now : e.Time);
				diagnosticsWorkStart = OrcaDiagnosticsCore.BeginWorkSample(diagnosticsSequence);
			}
			try
			{
				CaptureMarketData(e);
			}
			finally
			{
				if (diagnosticsWorkStart > 0)
					OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId,
						OrcaDiagnosticsWorkKind.MarketData, -1, diagnosticsWorkStart);
			}
		}

		private void CaptureMarketData(MarketDataEventArgs e)
		{
			if (e == null) return;
			bool historicalCapture = State == State.Historical
				&& HistoricalCoverage != OrcaHistoricalReplayCoverage.LiveOnly
				&& historicalTickReplayAvailable;
			bool liveCapture = State == State.Realtime;
			if (!historicalCapture && !liveCapture) return;
			if (e.MarketDataType == MarketDataType.Bid)
			{
				lastBid = e.Price;
				return;
			}
			if (e.MarketDataType == MarketDataType.Ask)
			{
				lastAsk = e.Price;
				return;
			}
			if (e.MarketDataType != MarketDataType.Last || e.Volume <= 0
				|| double.IsNaN(e.Price) || double.IsInfinity(e.Price)) return;

			if (e.Bid > 0 && !double.IsNaN(e.Bid)) lastBid = e.Bid;
			if (e.Ask > 0 && !double.IsNaN(e.Ask)) lastAsk = e.Ask;
			DateTime eventTime = e.Time == DateTime.MinValue ? DateTime.Now : e.Time;
			int exactBarIndex = CurrentBar;
			if (exactBarIndex < 0 && BarsArray != null && BarsArray.Length > 0 && BarsArray[0] != null)
				exactBarIndex = BarsArray[0].GetBar(eventTime);
			if (exactBarIndex < 0) return;

			long normalizedVolume = NormalizeVolume(e.Volume);
			if (normalizedVolume <= 0) return;
			var tradeEvent = new OrcaReplayTradeEvent(eventTime, e.Price, normalizedVolume,
				lastBid, lastAsk, 0, exactBarIndex);

			if (liveCapture && transitionDedupPending)
			{
				transitionDedupPending = false;
				if (OrcaReplayEventIdentity.IsSameBoundaryTrade(transitionBoundaryEvent, tradeEvent))
				{
					transitionBoundaryEvent = null;
					return;
				}
				transitionBoundaryEvent = null;
			}

			long sequence = Interlocked.Increment(ref nextSequence);
			tradeEvent = new OrcaReplayTradeEvent(eventTime, e.Price, normalizedVolume,
				lastBid, lastAsk, sequence, exactBarIndex);
			if (historicalCapture) AppendHistoricalTradeEvent(tradeEvent);
			else AppendTradeEvent(tradeEvent, false);
			if (liveCapture && (sequence & 255L) == 0 && ChartControl != null)
				ChartControl.Dispatcher.InvokeAsync(UpdateUiAndPublish, DispatcherPriority.Background);
		}

		private bool IsPrimaryTickReplayEnabled()
		{
			try { return IsTickReplays != null && IsTickReplays.Length > 0 && IsTickReplays[0] == true; }
			catch { return false; }
		}

		private void EnsureDiagnosticsRegistered()
		{
			if (diagnosticsRegistered) return;
			OrcaDiagnosticsCore.RegisterInstance(diagnosticsInstanceId, "OrcaInstantReplay", this);
			OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsInstanceId,
				"LiveMarketDataTape", "CaptureActive", "None");
			OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 0,
				"PrimaryChartSeries", "Chart", "Exact live bar mapping; no replay series added");
			diagnosticsRegistered = true;
		}

		private long NormalizeVolume(long volume)
		{
			try
			{
				if (Instrument != null && Instrument.MasterInstrument != null
					&& Instrument.MasterInstrument.InstrumentType == InstrumentType.CryptoCurrency)
					return (long)NinjaTrader.Core.Globals.ToCryptocurrencyVolume(volume);
			}
			catch { }
			return volume;
		}

		private void AppendHistoricalTradeEvent(OrcaReplayTradeEvent tradeEvent)
		{
			lock (tapeSync)
			{
				AppendToSegments(historicalBuilderSegments, tradeEvent, true, ref historicalBuilderBytes);
				historicalSeedEventCount++;
				long builderCap = (Math.Max(16, MemoryCapMb) + Math.Max(16, WorkingHeadroomMb)) * Megabyte;
				while (historicalBuilderSegments.Count > 1 && historicalBuilderBytes > builderCap)
				{
					TapeSegment oldest = historicalBuilderSegments[0];
					if (!oldest.IsSealed) break;
					historicalBuilderSegments.RemoveAt(0);
					historicalBuilderBytes = Math.Max(0, historicalBuilderBytes - oldest.EstimatedBytes);
				}
			}
		}

		private void AppendTradeEvent(OrcaReplayTradeEvent tradeEvent, bool historical)
		{
			lock (tapeSync)
			{
				AppendToSegments(tapeSegments, tradeEvent, historical, ref tapeBytes);
				EvictCompleteSegments(tradeEvent.Time);
			}
		}

		private void AppendToSegments(List<TapeSegment> segments, OrcaReplayTradeEvent tradeEvent,
			bool historical, ref long estimatedBytes)
		{
			TapeSegment active = segments.Count > 0 ? segments[segments.Count - 1] : null;
			if (active == null || active.IsSealed
				|| active.IsHistorical != historical
				|| tradeEvent.Time >= active.StartTime.AddMinutes(Math.Max(1, CheckpointMinutes)))
			{
				if (active != null) active.IsSealed = true;
				active = new TapeSegment { StartTime = tradeEvent.Time, EndTime = tradeEvent.Time,
					IsHistorical = historical };
				segments.Add(active);
			}

			active.Events.Add(tradeEvent);
			active.EndTime = tradeEvent.Time;
			active.EstimatedBytes += EstimatedEventBytes;
			estimatedBytes += EstimatedEventBytes;
		}

		private void FinalizeHistoricalTape()
		{
			if (historicalSeedFinalized) return;
			historicalSeedFinalized = true;
			lock (tapeSync)
			{
				foreach (TapeSegment segment in historicalBuilderSegments) segment.IsSealed = true;
				if (HistoricalCoverage == OrcaHistoricalReplayCoverage.LiveOnly)
				{
					historicalSeedStatus = "Historical seeding off";
					historicalBuilderSegments.Clear();
					historicalBuilderBytes = 0;
					replayStatus = GetIdleCaptureStatus();
					return;
				}
				if (!historicalTickReplayAvailable)
				{
					historicalSeedStatus = "Historical seeding unavailable — enable Tick Replay";
					historicalBuilderSegments.Clear();
					historicalBuilderBytes = 0;
					replayStatus = GetIdleCaptureStatus();
					return;
				}

				List<OrcaReplayTradeEvent> retained = historicalBuilderSegments
					.SelectMany(s => s.Events).OrderBy(e => e.Sequence).ToList();
				if (retained.Count == 0)
				{
					historicalSeedStatus = "Tick Replay produced no historical Last events";
					historicalBuilderSegments.Clear();
					historicalBuilderBytes = 0;
					replayStatus = GetIdleCaptureStatus();
					return;
				}

				if (HistoricalCoverage == OrcaHistoricalReplayCoverage.CurrentTradingSession)
				{
					DateTime sessionBegin, sessionEnd;
					if (!TryResolveSessionWindow(retained[retained.Count - 1].Time, out sessionBegin, out sessionEnd))
					{
						historicalSeedStatus = "Historical seed rejected — trading-session boundary unavailable";
						historicalBuilderSegments.Clear();
						historicalBuilderBytes = 0;
						replayStatus = GetIdleCaptureStatus();
						return;
					}
					retained = retained.Where(e => e.Time >= sessionBegin && e.Time <= sessionEnd).ToList();
				}

				tapeSegments.Clear();
				tapeBytes = 0;
				foreach (OrcaReplayTradeEvent tradeEvent in retained)
					AppendToSegments(tapeSegments, tradeEvent, true, ref tapeBytes);
				foreach (TapeSegment segment in tapeSegments) segment.IsSealed = true;
				if (retained.Count > 0)
				{
					transitionBoundaryEvent = retained[retained.Count - 1];
					transitionDedupPending = true;
					EvictCompleteSegments(transitionBoundaryEvent.Time);
					int actualRetained = tapeSegments.Sum(s => s.Events.Count);
					historicalSeedStatus = "Historical Tick Replay seeded " + actualRetained.ToString("N0") + " trades";
					if (actualRetained < historicalSeedEventCount)
						historicalSeedStatus += " (coverage shortened by memory/session limits)";
				}
				else historicalSeedStatus = "No trades remained inside the selected historical coverage";

				historicalBuilderSegments.Clear();
				historicalBuilderBytes = 0;
			}

			replayStatus = GetIdleCaptureStatus();
			EnsureDiagnosticsRegistered();
			OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsInstanceId,
				"HistoricalTickReplayAndLiveTape", RetainedCountForDiagnostics(), "None");
		}

		private string RetainedCountForDiagnostics()
		{
			return string.IsNullOrEmpty(historicalSeedStatus) ? "Unknown" : historicalSeedStatus;
		}

		private bool TryResolveSessionWindow(DateTime time, out DateTime sessionBegin, out DateTime sessionEnd)
		{
			sessionBegin = sessionEnd = DateTime.MinValue;
			try
			{
				if (replaySessionIterator == null) return false;
				DateTime tradingDay = replaySessionIterator.GetTradingDay(time);
				sessionBegin = replaySessionIterator.GetTradingDayBeginLocal(tradingDay);
				sessionEnd = replaySessionIterator.GetTradingDayEndLocal(tradingDay);
				return sessionEnd > sessionBegin;
			}
			catch { return false; }
		}

		private string GetIdleCaptureStatus()
		{
			if (!string.IsNullOrEmpty(historicalSeedStatus)) return historicalSeedStatus + " — live capture active";
			return "Capture active";
		}

		private void EvictCompleteSegments(DateTime newestTime)
		{
			long capBytes = Math.Max(16, MemoryCapMb) * Megabyte;
			DateTime retentionFloor = newestTime.AddMinutes(-Math.Max(5, RetentionMinutes));
			DateTime currentSessionBegin = DateTime.MinValue;
			DateTime currentSessionEnd;
			if (HistoricalCoverage == OrcaHistoricalReplayCoverage.CurrentTradingSession)
				TryResolveSessionWindow(newestTime, out currentSessionBegin, out currentSessionEnd);
			while (tapeSegments.Count > 1)
			{
				TapeSegment oldest = tapeSegments[0];
				bool outsideTime = !oldest.IsHistorical && oldest.EndTime < retentionFloor;
				bool outsideHistoricalSession = oldest.IsHistorical
					&& HistoricalCoverage == OrcaHistoricalReplayCoverage.CurrentTradingSession
					&& currentSessionBegin != DateTime.MinValue && oldest.EndTime < currentSessionBegin;
				bool outsideMemory = tapeBytes > capBytes;
				if (!oldest.IsSealed || (!outsideTime && !outsideHistoricalSession && !outsideMemory)) break;
				tapeSegments.RemoveAt(0);
				tapeBytes = Math.Max(0, tapeBytes - oldest.EstimatedBytes);
			}
		}

		private void AttachChartUi()
		{
			if (ChartControl == null || State >= State.Terminated) return;
			coordinatorRegistered = OrcaReplayCore.TryRegisterCoordinator(ChartControl, this);
			if (!coordinatorRegistered)
			{
				Draw.TextFixed(this, "OrcaInstantReplayDuplicate",
					"Orca Instant Replay is already enabled on this chart.",
					TextPosition.BottomRight, System.Windows.Media.Brushes.OrangeRed,
					new NinjaTrader.Gui.Tools.SimpleFont("Segoe UI", 12), System.Windows.Media.Brushes.Transparent,
					System.Windows.Media.Brushes.Transparent, 0);
				return;
			}

			ChartControl.MouseLeftButtonDown += ChartControlMouseLeftButtonDown;
			ChartControl.PreviewKeyDown += ChartControlPreviewKeyDown;
			CreateToolbar();
			if (toolbar != null && !UserControlCollection.Contains(toolbar)) UserControlCollection.Add(toolbar);
			if (lockBanner != null && !UserControlCollection.Contains(lockBanner)) UserControlCollection.Add(lockBanner);
			UpdateUiAndPublish();
		}

		private void DetachChartUi()
		{
			if (playbackTimer != null)
			{
				playbackTimer.Stop();
				playbackTimer.Tick -= PlaybackTimerTick;
				playbackTimer = null;
			}
			if (ChartControl != null)
			{
				ChartControl.MouseLeftButtonDown -= ChartControlMouseLeftButtonDown;
				ChartControl.PreviewKeyDown -= ChartControlPreviewKeyDown;
				if (toolbar != null) UserControlCollection.Remove(toolbar);
				if (lockBanner != null) UserControlCollection.Remove(lockBanner);
				if (coordinatorRegistered) OrcaReplayCore.UnregisterCoordinator(ChartControl, this);
			}
			coordinatorRegistered = false;
			toolbar = null;
			lockBanner = null;
		}

		private void CreateToolbar()
		{
			if (toolbar != null) return;
			System.Windows.Media.Brush background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 24, 24, 27));
			System.Windows.Media.Brush foreground = System.Windows.Media.Brushes.WhiteSmoke;
			toolbar = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(0, 0, 0, 24),
				Background = background,
				Height = 34
			};
			for (int i = 0; i < 9; i++) toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			replayButton = CreateButton("Replay", 72, ReplayButtonClick);
			previousButton = CreateButton("|<", 34, PreviousButtonClick);
			playPauseButton = CreateButton("Play", 50, PlayPauseButtonClick);
			stepButton = CreateButton(">|", 34, StepButtonClick);
			jumpLiveButton = CreateButton("Jump Live", 74, JumpLiveButtonClick);
			modeCombo = new ComboBox { Width = 68, Height = 25, Margin = new Thickness(3), ItemsSource = new[] { "Bar", "Tick" }, SelectedIndex = 0 };
			speedCombo = new ComboBox { Width = 58, Height = 25, Margin = new Thickness(3), ItemsSource = new[] { "0.5x", "1x", "2x", "5x", "10x" }, SelectedIndex = 1 };
			statusText = new TextBlock { Foreground = foreground, Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, MinWidth = 180 };
			modeCombo.SelectionChanged += ModeSelectionChanged;
			speedCombo.SelectionChanged += SpeedSelectionChanged;

			FrameworkElement[] controls = { replayButton, previousButton, playPauseButton, stepButton, modeCombo, speedCombo, statusText, jumpLiveButton };
			for (int i = 0; i < controls.Length; i++)
			{
				Grid.SetColumn(controls[i], i);
				toolbar.Children.Add(controls[i]);
			}

			lockBanner = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0, 28, 0, 0),
				Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(232, 133, 28, 28)),
				CornerRadius = new CornerRadius(3),
				Padding = new Thickness(12, 5, 12, 5),
				Visibility = Visibility.Collapsed,
				Child = new TextBlock
				{
					Text = "ORCA REPLAY — CHART ORDER ENTRY LOCKED — LIVE DATA CONTINUES",
					Foreground = System.Windows.Media.Brushes.White,
					FontWeight = FontWeights.Bold
				}
			};
		}

		private Button CreateButton(string text, double width, RoutedEventHandler handler)
		{
			var button = new Button { Content = text, Width = width, Height = 25, Margin = new Thickness(3), Padding = new Thickness(4, 0, 4, 0) };
			button.Click += handler;
			return button;
		}

		private void ReplayButtonClick(object sender, RoutedEventArgs e)
		{
			if (lifecycle == OrcaReplayLifecycle.Idle)
			{
				DateTime earliest, latest;
				if (!TryGetCoverage(out earliest, out latest))
				{
					replayStatus = historicalSeedFinalized
						? "No replayable trades are currently retained" : replayStatus;
					UpdateUiAndPublish();
					return;
				}
				lifecycle = OrcaReplayLifecycle.Selecting;
				replayStatus = "Select the last visible bar (" + earliest.ToString("HH:mm:ss") + "+)";
			}
			else CancelReplayAndReturnLive(true);
			UpdateUiAndPublish();
		}

		private void ChartControlMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (lifecycle != OrcaReplayLifecycle.Selecting || ChartControl == null || Bars == null) return;
			try
			{
				IInputElement inputElement = ChartPanel != null ? (IInputElement)ChartPanel : (IInputElement)ChartControl;
				System.Windows.Point point = e.GetPosition(inputElement);
				DateTime clickedTime = ChartControl.GetTimeByX((int)point.X);
				int barIndex = Bars.GetBar(clickedTime);
				if (!PrepareReplay(barIndex)) return;
				e.Handled = true;
			}
			catch (Exception ex)
			{
				replayStatus = "Selection failed: " + ex.Message;
				UpdateUiAndPublish();
			}
		}

		private bool PrepareReplay(int barIndex)
		{
			if (Bars == null || barIndex < 0 || barIndex >= Bars.Count - 1) return RejectSelection("Choose an earlier completed bar");
			DateTime selected = Bars.GetTime(barIndex);
			DateTime earliest, latest;
			if (!TryGetCoverage(out earliest, out latest) || selected < earliest || selected >= latest)
				return RejectSelection("Selection is outside retained coverage (earliest " + earliest.ToString("HH:mm:ss") + ")");

			string incompatible;
			if (!TryValidateParticipants(out incompatible))
				return RejectSelection("Replay blocked: " + incompatible);

			lifecycle = OrcaReplayLifecycle.Preparing;
			replayStatus = "Preparing replay…";
			UpdateUiAndPublish();
			replayCancellation = new CancellationTokenSource();
			selectionBarIndex = barIndex;
			replayBarIndex = barIndex;
			selectionTime = selected;
			replayTime = selected;
			replaySequence = 0;
			playbackPosition = -1;
			syntheticBar = null;

			lock (tapeSync)
			{
				playbackEvents = tapeSegments.SelectMany(s => s.Events)
					.Where(t => t.PrimaryBarIndex > barIndex)
					.OrderBy(t => t.Sequence).ToList();
			}
			long workingBytes = playbackEvents.Count * EstimatedEventBytes;
			if (workingBytes > Math.Max(16, WorkingHeadroomMb) * Megabyte)
			{
				playbackEvents.Clear();
				CancelReplayAndReturnLive(false);
				return RejectSelection("Replay slice exceeds working-memory headroom");
			}

			tickModeAvailable = playbackEvents.Count > 0 && IsTickChartStyleSupported()
				&& AreVisibleParticipantsCompatibleWith(OrcaReplayMode.Tick);
			if (replayMode == OrcaReplayMode.Tick && !tickModeAvailable) replayMode = OrcaReplayMode.Bar;
			string surfaceFailure;
			if (!TryHideNonReplaySurfaces(out surfaceFailure))
			{
				CancelReplayAndReturnLive(false);
				return RejectSelection(surfaceFailure);
			}
			SetChartOrderLock(true);
			if (!PrepareParticipants()) return false;
			lifecycle = OrcaReplayLifecycle.Paused;
			replayStatus = "Paused at " + selected.ToString("HH:mm:ss");
			EnsurePlaybackTimer();
			UpdateUiAndPublish();
			return true;
		}

		private bool RejectSelection(string reason)
		{
			replayStatus = reason;
			UpdateUiAndPublish();
			return false;
		}

		private bool TryValidateParticipants(out string incompatibleSummary)
		{
			incompatibleSummary = string.Empty;
			if (ChartControl == null) return false;
			var participants = OrcaReplayCore.GetParticipants(ChartControl);
			var incompatible = new List<string>();
			try
			{
				foreach (IndicatorRenderBase indicator in ChartControl.Indicators)
				{
					if (indicator == null || !indicator.IsVisible || ReferenceEquals(indicator, this)) continue;
					string name = indicator.GetType().Name;
					if (!name.StartsWith("Orca", StringComparison.OrdinalIgnoreCase)) continue;
					if (IsInteractionOnlyComponent(name)) continue;
					IOrcaReplayParticipant participant = indicator as IOrcaReplayParticipant;
					if (participant == null || !participants.Any(p => ReferenceEquals(p, participant)))
					{
						incompatible.Add(name);
						continue;
					}
					OrcaReplayCapabilities capabilities = participant.ReplayCapabilities;
					if (capabilities == null
						|| (replayMode == OrcaReplayMode.Bar && !capabilities.SupportsBarReplay)
						|| (replayMode == OrcaReplayMode.Tick && !capabilities.SupportsTickReplay))
						incompatible.Add(name + " (" + replayMode + " unsupported)");
				}
			}
			catch (Exception ex)
			{
				incompatibleSummary = "could not inspect chart indicators: " + ex.Message;
				return false;
			}
			if (incompatible.Count == 0) return true;
			incompatibleSummary = string.Join(", ", incompatible.Distinct().OrderBy(n => n));
			return false;
		}

		private bool AreVisibleParticipantsCompatibleWith(OrcaReplayMode mode)
		{
			if (ChartControl == null) return false;
			var participants = OrcaReplayCore.GetParticipants(ChartControl);
			try
			{
				foreach (IndicatorRenderBase indicator in ChartControl.Indicators)
				{
					if (indicator == null || !indicator.IsVisible || ReferenceEquals(indicator, this)) continue;
					string name = indicator.GetType().Name;
					if (!name.StartsWith("Orca", StringComparison.OrdinalIgnoreCase) || IsInteractionOnlyComponent(name)) continue;
					IOrcaReplayParticipant participant = indicator as IOrcaReplayParticipant;
					if (participant == null || !participants.Any(p => ReferenceEquals(p, participant))) return false;
					OrcaReplayCapabilities capabilities = participant.ReplayCapabilities;
					if (capabilities == null) return false;
					if (mode == OrcaReplayMode.Bar && !capabilities.SupportsBarReplay) return false;
					if (mode == OrcaReplayMode.Tick && !capabilities.SupportsTickReplay) return false;
				}
				return true;
			}
			catch { return false; }
		}

		private bool IsInteractionOnlyComponent(string name)
		{
			return string.Equals(name, "OrcaExecutionLines", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(name, "OrcaExecutionLines2", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(name, "OrcaVisualOrders", StringComparison.OrdinalIgnoreCase);
		}

		private bool TryHideNonReplaySurfaces(out string failure)
		{
			failure = string.Empty;
			if (ChartControl == null) { failure = "Replay chart is unavailable"; return false; }
			replaySurfaceVisibility.Clear();
			var participants = OrcaReplayCore.GetParticipants(ChartControl);
			try
			{
				foreach (IChartObject chartObject in ChartControl.ChartObjects.ToList())
				{
					if (chartObject == null || ReferenceEquals(chartObject, this) || ReferenceEquals(chartObject, ChartBars))
						continue;
					IndicatorRenderBase indicator = chartObject as IndicatorRenderBase;
					bool hide = chartObject is NinjaTrader.NinjaScript.DrawingTools.DrawingTool;
					if (indicator != null)
					{
						string name = indicator.GetType().Name;
						hide = IsInteractionOnlyComponent(name)
							|| !name.StartsWith("Orca", StringComparison.OrdinalIgnoreCase)
							|| !(indicator is IOrcaReplayParticipant)
							|| !participants.Any(p => ReferenceEquals(p, indicator));
					}
					if (!hide || !chartObject.IsVisible) continue;
					replaySurfaceVisibility[chartObject] = true;
					chartObject.IsVisible = false;
					if (chartObject.IsVisible)
						throw new InvalidOperationException(chartObject.Name + " did not accept visibility change");
				}
				return true;
			}
			catch (Exception ex)
			{
				RestoreReplaySurfaceVisibility();
				failure = "Replay blocked: could not safely hide/restore chart surface — " + ex.Message;
				return false;
			}
		}

		private void RestoreReplaySurfaceVisibility()
		{
			foreach (KeyValuePair<IChartObject, bool> item in replaySurfaceVisibility.ToList())
			{
				try { item.Key.IsVisible = item.Value; }
				catch { }
			}
			replaySurfaceVisibility.Clear();
		}

		private bool PrepareParticipants()
		{
			if (ChartControl == null) return false;
			CancellationToken token = replayCancellation == null ? CancellationToken.None : replayCancellation.Token;
			List<OrcaReplayTradeEvent> prefixEvents;
			lock (tapeSync)
			{
				prefixEvents = tapeSegments.SelectMany(s => s.Events)
					.Where(t => t.PrimaryBarIndex <= selectionBarIndex)
					.OrderBy(t => t.Sequence).ToList();
			}
			long selectionSequence = prefixEvents.Count == 0 ? 0 : prefixEvents[prefixEvents.Count - 1].Sequence;
			var context = new OrcaReplayContext(ChartControl, replayMode, selectionTime, replayTime,
				replayBarIndex, selectionSequence, token);
			foreach (IOrcaReplayParticipant participant in OrcaReplayCore.GetParticipants(ChartControl))
			{
				try
				{
					OrcaReplayCheckpoint checkpoint = participant.CaptureReplayCheckpoint(context);
					participant.PrepareReplay(context, checkpoint);
					long checkpointSequence = checkpoint == null ? 0 : checkpoint.Sequence;
					foreach (OrcaReplayTradeEvent prefixEvent in prefixEvents)
					{
						if (prefixEvent.Sequence <= checkpointSequence) continue;
						var prefixContext = new OrcaReplayContext(ChartControl, replayMode, selectionTime,
							prefixEvent.Time, prefixEvent.PrimaryBarIndex, prefixEvent.Sequence, token);
						participant.ApplyReplayEvent(prefixContext, prefixEvent);
					}
					participant.PublishReplaySnapshot(context);
				}
				catch (Exception ex)
				{
					replayStatus = participant.ReplayParticipantId + " replay preparation failed: " + ex.Message;
					CancelReplayAndReturnLive(false);
					UpdateUiAndPublish();
					return false;
				}
			}
			return true;
		}

		private void EnsurePlaybackTimer()
		{
			if (playbackTimer != null) return;
			playbackTimer = new DispatcherTimer(DispatcherPriority.Render)
			{
				Interval = TimeSpan.FromMilliseconds(VisualFrameMilliseconds)
			};
			playbackTimer.Tick += PlaybackTimerTick;
			playbackTimer.Start();
		}

		private void PlayPauseButtonClick(object sender, RoutedEventArgs e)
		{
			if (lifecycle == OrcaReplayLifecycle.Paused || lifecycle == OrcaReplayLifecycle.Complete)
			{
				if (lifecycle == OrcaReplayLifecycle.Complete) return;
				lifecycle = OrcaReplayLifecycle.Playing;
				lastTimerWallUtc = DateTime.UtcNow;
				accumulatedBarSeconds = 0;
			}
			else if (lifecycle == OrcaReplayLifecycle.Playing) lifecycle = OrcaReplayLifecycle.Paused;
			UpdateUiAndPublish();
		}

		private void PlaybackTimerTick(object sender, EventArgs e)
		{
			if (lifecycle != OrcaReplayLifecycle.Playing) return;
			DateTime now = DateTime.UtcNow;
			double elapsed = Math.Max(0, (now - lastTimerWallUtc).TotalSeconds);
			lastTimerWallUtc = now;
			if (replayMode == OrcaReplayMode.Bar)
			{
				accumulatedBarSeconds += elapsed * replaySpeed;
				while (accumulatedBarSeconds >= 1.0 && lifecycle == OrcaReplayLifecycle.Playing)
				{
					accumulatedBarSeconds -= 1.0;
					StepBarForward();
				}
			}
			else AdvanceTicksByElapsed(elapsed * replaySpeed);
			UpdateUiAndPublish();
		}

		private void AdvanceTicksByElapsed(double replayElapsedSeconds)
		{
			if (playbackPosition + 1 >= playbackEvents.Count) { CompleteReplay(); return; }
			DateTime target = replayTime.AddSeconds(replayElapsedSeconds);
			bool applied = false;
			while (playbackPosition + 1 < playbackEvents.Count && playbackEvents[playbackPosition + 1].Time <= target)
			{
				ApplyNextTick(false);
				applied = true;
			}
			if (!applied) replayTime = target;
			else PublishParticipantSnapshots();
			if (playbackPosition + 1 >= playbackEvents.Count) CompleteReplay();
		}

		private void StepButtonClick(object sender, RoutedEventArgs e)
		{
			if (!IsReplayControllable()) return;
			lifecycle = OrcaReplayLifecycle.Paused;
			if (replayMode == OrcaReplayMode.Bar) StepBarForward();
			else ApplyNextTick(true);
			UpdateUiAndPublish();
		}

		private void PreviousButtonClick(object sender, RoutedEventArgs e)
		{
			if (!IsReplayControllable()) return;
			lifecycle = OrcaReplayLifecycle.Paused;
			if (replayMode == OrcaReplayMode.Bar) RebuildToBar(Math.Max(selectionBarIndex, replayBarIndex - 1));
			else RebuildToEvent(Math.Max(-1, playbackPosition - 1));
			UpdateUiAndPublish();
		}

		private bool IsReplayControllable()
		{
			return lifecycle == OrcaReplayLifecycle.Paused || lifecycle == OrcaReplayLifecycle.Playing
				|| lifecycle == OrcaReplayLifecycle.Complete;
		}

		private void StepBarForward()
		{
			int lastCapturedBar = playbackEvents.Count == 0 ? selectionBarIndex : playbackEvents.Max(t => t.PrimaryBarIndex);
			if (replayBarIndex >= lastCapturedBar) { CompleteReplay(); return; }
			RebuildToBar(replayBarIndex + 1);
		}

		private void RebuildToBar(int targetBar)
		{
			targetBar = Math.Max(selectionBarIndex, targetBar);
			int targetEvent = -1;
			for (int i = 0; i < playbackEvents.Count; i++)
			{
				if (playbackEvents[i].PrimaryBarIndex > targetBar) break;
				targetEvent = i;
			}
			RebuildToEvent(targetEvent);
			replayBarIndex = targetBar;
			try { replayTime = Bars.GetTime(Math.Min(targetBar, Bars.Count - 1)); } catch { }
			PublishBarToParticipants(targetBar);
		}

		private void RebuildToEvent(int targetPosition)
		{
			playbackPosition = -1;
			replayBarIndex = selectionBarIndex;
			replayTime = selectionTime;
			replaySequence = 0;
			syntheticBar = null;
			if (!PrepareParticipants()) return;
			for (int i = 0; i <= targetPosition && i < playbackEvents.Count; i++) ApplyNextTick(false);
			PublishParticipantSnapshots();
		}

		private void ApplyNextTick(bool publishSnapshot)
		{
			if (playbackPosition + 1 >= playbackEvents.Count) { CompleteReplay(); return; }
			OrcaReplayTradeEvent tradeEvent = playbackEvents[++playbackPosition];
			replayTime = tradeEvent.Time;
			replaySequence = tradeEvent.Sequence;
			replayBarIndex = tradeEvent.PrimaryBarIndex;
			if (syntheticBar == null || syntheticBar.BarIndex != replayBarIndex)
			{
				syntheticBar = new SyntheticBar { BarIndex = replayBarIndex, Open = tradeEvent.Price,
					High = tradeEvent.Price, Low = tradeEvent.Price, Close = tradeEvent.Price,
					Volume = tradeEvent.Volume, ReferenceVolume = GetReplayVolumeReference(replayBarIndex) };
			}
			else
			{
				syntheticBar.High = Math.Max(syntheticBar.High, tradeEvent.Price);
				syntheticBar.Low = Math.Min(syntheticBar.Low, tradeEvent.Price);
				syntheticBar.Close = tradeEvent.Price;
				syntheticBar.Volume += tradeEvent.Volume;
			}
			PublishEventToParticipants(tradeEvent, publishSnapshot);
		}

		private double GetReplayVolumeReference(int barIndex)
		{
			if (Bars == null || barIndex <= 0) return 1;
			double maxVolume = 1;
			int first = Math.Max(0, barIndex - 50);
			int last = Math.Min(barIndex - 1, Bars.Count - 1);
			for (int i = first; i <= last; i++)
			{
				try { maxVolume = Math.Max(maxVolume, Bars.GetVolume(i)); }
				catch { }
			}
			return maxVolume;
		}

		private void PublishEventToParticipants(OrcaReplayTradeEvent tradeEvent, bool publishSnapshot)
		{
			if (ChartControl == null) return;
			CancellationToken token = replayCancellation == null ? CancellationToken.None : replayCancellation.Token;
			var context = new OrcaReplayContext(ChartControl, replayMode, selectionTime, replayTime,
				replayBarIndex, replaySequence, token);
			foreach (IOrcaReplayParticipant participant in OrcaReplayCore.GetParticipants(ChartControl))
			{
				participant.ApplyReplayEvent(context, tradeEvent);
				if (publishSnapshot) participant.PublishReplaySnapshot(context);
			}
		}

		private void PublishParticipantSnapshots()
		{
			if (ChartControl == null) return;
			CancellationToken token = replayCancellation == null ? CancellationToken.None : replayCancellation.Token;
			var context = new OrcaReplayContext(ChartControl, replayMode, selectionTime, replayTime,
				replayBarIndex, replaySequence, token);
			foreach (IOrcaReplayParticipant participant in OrcaReplayCore.GetParticipants(ChartControl))
				participant.PublishReplaySnapshot(context);
		}

		private void PublishBarToParticipants(int barIndex)
		{
			if (ChartControl == null) return;
			CancellationToken token = replayCancellation == null ? CancellationToken.None : replayCancellation.Token;
			var context = new OrcaReplayContext(ChartControl, replayMode, selectionTime, replayTime,
				barIndex, replaySequence, token);
			foreach (IOrcaReplayParticipant participant in OrcaReplayCore.GetParticipants(ChartControl))
			{
				participant.ApplyReplayBar(context, barIndex);
				participant.PublishReplaySnapshot(context);
			}
		}

		private void CompleteReplay()
		{
			lifecycle = OrcaReplayLifecycle.Complete;
			replayStatus = "Replay complete — Jump Live to unlock";
		}

		private void ModeSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (modeCombo == null) return;
			OrcaReplayMode requested = modeCombo.SelectedIndex == 1 ? OrcaReplayMode.Tick : OrcaReplayMode.Bar;
			if (requested == OrcaReplayMode.Tick && IsReplayControllable() && !tickModeAvailable)
			{
				modeCombo.SelectedIndex = 0;
				replayStatus = "Tick mode unavailable for this replay slice";
				return;
			}
			replayMode = requested;
			if (IsReplayControllable()) RebuildToBar(replayBarIndex);
			UpdateUiAndPublish();
		}

		private bool IsTickChartStyleSupported()
		{
			try
			{
				if (ChartBars == null || ChartBars.Properties == null) return false;
				int style = (int)ChartBars.Properties.ChartStyleType;
				return style == 1 || style == 2 || style == 3 || style == 0x4F564331;
			}
			catch { return false; }
		}

		private void SpeedSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			double[] speeds = { 0.5, 1.0, 2.0, 5.0, 10.0 };
			int index = speedCombo == null ? 1 : Math.Max(0, Math.Min(speeds.Length - 1, speedCombo.SelectedIndex));
			replaySpeed = speeds[index];
			lastTimerWallUtc = DateTime.UtcNow;
			UpdateUiAndPublish();
		}

		private void JumpLiveButtonClick(object sender, RoutedEventArgs e)
		{
			CancelReplayAndReturnLive(true);
		}

		private void ChartControlPreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape && lifecycle != OrcaReplayLifecycle.Idle)
			{
				CancelReplayAndReturnLive(true);
				e.Handled = true;
				return;
			}
			if (OrcaReplayCore.IsChartLocked(ChartControl) && IsLikelyOrderHotkey(e)) e.Handled = true;
		}

		private bool IsLikelyOrderHotkey(KeyEventArgs e)
		{
			ModifierKeys modifiers = Keyboard.Modifiers;
			return e.Key == Key.Enter || e.Key == Key.Space || e.Key == Key.F1 || e.Key == Key.F2
				|| modifiers.HasFlag(ModifierKeys.Alt) || modifiers.HasFlag(ModifierKeys.Control);
		}

		private void CancelReplayAndReturnLive(bool updateUi)
		{
			if (lifecycle != OrcaReplayLifecycle.Idle) lifecycle = OrcaReplayLifecycle.ReturningLive;
			if (replayCancellation != null)
			{
				try { replayCancellation.Cancel(); } catch { }
				replayCancellation.Dispose();
				replayCancellation = null;
			}
			if (ChartControl != null)
			{
				foreach (IOrcaReplayParticipant participant in OrcaReplayCore.GetParticipants(ChartControl))
				{
					try { participant.RestoreLiveState(); } catch { }
				}
			}
			SetChartOrderLock(false);
			RestoreReplaySurfaceVisibility();
			playbackEvents.Clear();
			playbackPosition = -1;
			syntheticBar = null;
			selectionBarIndex = replayBarIndex = -1;
			selectionTime = replayTime = DateTime.MinValue;
			replaySequence = 0;
			tickModeAvailable = false;
			lifecycle = OrcaReplayLifecycle.Idle;
			replayStatus = GetIdleCaptureStatus();
			if (updateUi) UpdateUiAndPublish();
		}

		private void SetChartOrderLock(bool locked)
		{
			if (ChartControl == null) return;
			try
			{
				NinjaTrader.Gui.Chart.Chart chart = Window.GetWindow(ChartControl) as NinjaTrader.Gui.Chart.Chart;
				if (chart == null || chart.ChartTrader == null) return;
				if (locked)
				{
					chartTraderWasEnabled = chart.ChartTrader.IsEnabled;
					chartTraderStateCaptured = true;
					chart.ChartTrader.IsEnabled = false;
				}
				else if (chartTraderStateCaptured)
				{
					chart.ChartTrader.IsEnabled = chartTraderWasEnabled;
					chartTraderStateCaptured = false;
				}
			}
			catch { }
		}

		private bool TryGetCoverage(out DateTime earliest, out DateTime latest)
		{
			earliest = latest = DateTime.MinValue;
			lock (tapeSync)
			{
				TapeSegment first = tapeSegments.FirstOrDefault(s => s.Events.Count > 0);
				TapeSegment last = tapeSegments.LastOrDefault(s => s.Events.Count > 0);
				if (first == null || last == null) return false;
				earliest = first.Events[0].Time;
				latest = last.Events[last.Events.Count - 1].Time;
				return latest > earliest;
			}
		}

		private void UpdateUiAndPublish()
		{
			if (ChartControl == null) return;
			if (!ChartControl.Dispatcher.CheckAccess())
			{
				ChartControl.Dispatcher.InvokeAsync(UpdateUiAndPublish);
				return;
			}

			bool active = lifecycle == OrcaReplayLifecycle.Preparing || lifecycle == OrcaReplayLifecycle.Paused
				|| lifecycle == OrcaReplayLifecycle.Playing || lifecycle == OrcaReplayLifecycle.Complete;
			string availability = string.Empty;
			bool replayAvailable = true;
			if (lifecycle == OrcaReplayLifecycle.Idle)
			{
				DateTime availabilityEarliest, availabilityLatest;
				string incompatible = string.Empty;
				replayAvailable = TryGetCoverage(out availabilityEarliest, out availabilityLatest)
					&& TryValidateParticipants(out incompatible);
				if (!string.IsNullOrEmpty(incompatible)) availability = "Replay unavailable: " + incompatible;
				else if (!replayAvailable)
					availability = !historicalSeedFinalized ? replayStatus : "Waiting for replay coverage";
			}
			if (replayButton != null)
			{
				replayButton.Content = lifecycle == OrcaReplayLifecycle.Idle ? "Replay" : "Cancel";
				replayButton.IsEnabled = lifecycle != OrcaReplayLifecycle.Idle || replayAvailable;
			}
			if (playPauseButton != null) playPauseButton.Content = lifecycle == OrcaReplayLifecycle.Playing ? "Pause" : "Play";
			if (previousButton != null) previousButton.IsEnabled = active;
			if (playPauseButton != null) playPauseButton.IsEnabled = active && lifecycle != OrcaReplayLifecycle.Complete;
			if (stepButton != null) stepButton.IsEnabled = active;
			if (jumpLiveButton != null) jumpLiveButton.IsEnabled = active || lifecycle == OrcaReplayLifecycle.Selecting;
			if (lockBanner != null) lockBanner.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
			if (statusText != null)
			{
				DateTime earliest, latest;
				string coverage = TryGetCoverage(out earliest, out latest) ? " | " + earliest.ToString("HH:mm") + "–" + latest.ToString("HH:mm") : string.Empty;
				statusText.Text = (string.IsNullOrEmpty(availability) ? replayStatus : availability) + coverage;
			}

			OrcaReplayCore.PublishSnapshot(ChartControl, this,
				new OrcaReplayViewSnapshot(lifecycle, replayMode, selectionTime, replayTime,
					selectionBarIndex, replayBarIndex, replaySequence, replaySpeed,
					tickModeAvailable, replayStatus));
			ChartControl.InvalidateVisual();
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (RenderTarget == null || chartControl == null || chartScale == null || ChartBars == null) return;
			OrcaReplayViewSnapshot snapshot;
			if (!OrcaReplayCore.TryGetSnapshot(chartControl, out snapshot)) return;

			if (snapshot.Lifecycle == OrcaReplayLifecycle.Selecting)
			{
				EnsureDxResources();
				return;
			}
			if (!snapshot.IsReplayActive || snapshot.CurrentBarIndex < 0) return;
			EnsureDxResources();
			int maskFromBar = snapshot.Mode == OrcaReplayMode.Tick ? snapshot.CurrentBarIndex : snapshot.CurrentBarIndex + 1;
			float maskLeft = (float)chartControl.CanvasRight;
			try
			{
				if (maskFromBar <= ChartBars.ToIndex)
				{
					float center = chartControl.GetXByBarIndex(ChartBars, maskFromBar);
					float spacing = maskFromBar > 0 ? center - chartControl.GetXByBarIndex(ChartBars, maskFromBar - 1) : (float)chartControl.BarWidth * 2f;
					maskLeft = center - Math.Max(1f, spacing / 2f);
				}
			}
			catch { }

			float panelTop = ChartPanel == null ? 0 : ChartPanel.Y;
			float panelBottom = ChartPanel == null ? (float)chartControl.ActualHeight : ChartPanel.Y + ChartPanel.H;
			if (maskLeft < chartControl.CanvasRight)
				RenderTarget.FillRectangle(new RectangleF(maskLeft, panelTop,
					(float)chartControl.CanvasRight - maskLeft, Math.Max(1, panelBottom - panelTop)), maskBrushDx);

			SyntheticBar bar = syntheticBar == null ? null : syntheticBar.Copy();
			if (snapshot.Mode == OrcaReplayMode.Tick && bar != null && bar.BarIndex == snapshot.CurrentBarIndex)
				DrawSyntheticPrice(chartControl, chartScale, bar);
		}

		private void DrawSyntheticPrice(ChartControl chartControl, ChartScale chartScale, SyntheticBar bar)
		{
			float x;
			try { x = chartControl.GetXByBarIndex(ChartBars, bar.BarIndex); }
			catch { return; }
			float openY = chartScale.GetYByValue(bar.Open);
			float highY = chartScale.GetYByValue(bar.High);
			float lowY = chartScale.GetYByValue(bar.Low);
			float closeY = chartScale.GetYByValue(bar.Close);
			int style = 1;
			try { style = (int)ChartBars.Properties.ChartStyleType; } catch { }
			float width = Math.Max(3f, (float)chartControl.BarWidth * 1.6f);
			if (style == 0x4F564331)
			{
				double ratio = bar.ReferenceVolume <= 0 ? 1 : Math.Max(0.15, Math.Min(1.0, bar.Volume / bar.ReferenceVolume));
				width = Math.Max(1f, width * (float)ratio);
			}
			DxSolidColorBrush body = bar.Close >= bar.Open ? bullishBrushDx : bearishBrushDx;
			if (style == 2)
			{
				float previousX = x;
				float previousY = closeY;
				try
				{
					if (bar.BarIndex > 0)
					{
						previousX = chartControl.GetXByBarIndex(ChartBars, bar.BarIndex - 1);
						previousY = chartScale.GetYByValue(Bars.GetClose(bar.BarIndex - 1));
					}
				}
				catch { }
				RenderTarget.DrawLine(new Vector2(previousX, previousY), new Vector2(x, closeY), body, 2f);
				return;
			}
			if (style == 3)
			{
				float tickWidth = Math.Max(3f, width / 2f);
				RenderTarget.DrawLine(new Vector2(x, highY), new Vector2(x, lowY), wickBrushDx, 1f);
				RenderTarget.DrawLine(new Vector2(x - tickWidth, openY), new Vector2(x, openY), body, 2f);
				RenderTarget.DrawLine(new Vector2(x, closeY), new Vector2(x + tickWidth, closeY), body, 2f);
				return;
			}
			RenderTarget.DrawLine(new Vector2(x, highY), new Vector2(x, lowY), wickBrushDx, 1f);
			float top = Math.Min(openY, closeY);
			float height = Math.Max(1f, Math.Abs(closeY - openY));
			RenderTarget.FillRectangle(new RectangleF(x - width / 2f, top, width, height), body);
		}

		private void EnsureDxResources()
		{
			if (maskBrushDx == null) maskBrushDx = new DxSolidColorBrush(RenderTarget, ToDxColor(ReplayMaskBrush, 1.0));
			if (bullishBrushDx == null) bullishBrushDx = new DxSolidColorBrush(RenderTarget, new Color4(0.25f, 0.67f, 0.30f, 1f));
			if (bearishBrushDx == null) bearishBrushDx = new DxSolidColorBrush(RenderTarget, new Color4(0.84f, 0.20f, 0.20f, 1f));
			if (wickBrushDx == null) wickBrushDx = new DxSolidColorBrush(RenderTarget, new Color4(0.80f, 0.80f, 0.82f, 1f));
			if (selectionBrushDx == null) selectionBrushDx = new DxSolidColorBrush(RenderTarget, new Color4(0.15f, 0.47f, 1f, 1f));
			if (bannerFormatDx == null) bannerFormatDx = new TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", 12f);
		}

		private SharpDX.Color4 ToDxColor(System.Windows.Media.Brush brush, double opacity)
		{
			var solid = brush as System.Windows.Media.SolidColorBrush;
			System.Windows.Media.Color color = solid == null ? System.Windows.Media.Colors.Black : solid.Color;
			return new SharpDX.Color4(color.R / 255f, color.G / 255f, color.B / 255f,
				(float)Math.Max(0, Math.Min(1, opacity * color.A / 255.0)));
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxResources();
			base.OnRenderTargetChanged();
		}

		private void DisposeDxResources()
		{
			if (maskBrushDx != null) { maskBrushDx.Dispose(); maskBrushDx = null; }
			if (bullishBrushDx != null) { bullishBrushDx.Dispose(); bullishBrushDx = null; }
			if (bearishBrushDx != null) { bearishBrushDx.Dispose(); bearishBrushDx = null; }
			if (wickBrushDx != null) { wickBrushDx.Dispose(); wickBrushDx = null; }
			if (selectionBrushDx != null) { selectionBrushDx.Dispose(); selectionBrushDx = null; }
			if (bannerFormatDx != null) { bannerFormatDx.Dispose(); bannerFormatDx = null; }
		}
	}
}
