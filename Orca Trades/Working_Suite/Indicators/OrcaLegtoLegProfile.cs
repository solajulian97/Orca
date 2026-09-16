#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Serialization;
using System.Windows.Input;

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

using WpfBrush = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	internal enum OrcaLegDirection { Unknown = 0, Up = 1, Down = -1 }
	public enum LegProfileTradeSourceMode
	{
		SecondaryTickSeries = 0,
		TickReplayLastEvents = 1
	}

	public enum LegProfileResetDisplayMode
	{
		[Description("Reset Only")]
		ResetOnly = 0,
		Overlay = 1,
		[Description("Side-by-Side")]
		SideBySide = 2
	}

	public enum LegProfileResetAggregationMode
	{
		[Description("Follow Full Leg")]
		FollowFullLeg = 0,
		[Description("Fixed Ticks")]
		FixedTicks = 1,
		[Description("Full-Leg Ratio")]
		FullLegRatio = 2
	}

	public enum LegProfileTextFontWeight
	{
		Thin = 0,
		[Description("Extra Light")]
		ExtraLight = 1,
		Light = 2,
		[Description("Normal / Regular")]
		Regular = 3,
		Medium = 4,
		[Description("Semi-Bold")]
		SemiBold = 5,
		Bold = 6,
		[Description("Extra Bold")]
		ExtraBold = 7,
		Black = 8
	}

	public class LegProfileTextFontFamilyConverter : StringConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> fontNames = new List<string>();
			List<string> installedFontNames = new List<string>();
			foreach (System.Windows.Media.FontFamily family in System.Windows.Media.Fonts.SystemFontFamilies)
			{
				string name = family != null ? family.Source : null;
				if (!string.IsNullOrWhiteSpace(name)) installedFontNames.Add(name);
			}

			string[] preferredFonts = { "Segoe UI", "Figtree", "Arial", "Consolas", "Tahoma", "Verdana" };
			foreach (string preferred in preferredFonts)
			{
				string installed = installedFontNames.FirstOrDefault(name => string.Equals(name, preferred, StringComparison.OrdinalIgnoreCase));
				if (installed != null) AddFontName(fontNames, seen, installed);
			}
			if (fontNames.Count == 0) AddFontName(fontNames, seen, "Segoe UI");

			installedFontNames.Sort(StringComparer.CurrentCultureIgnoreCase);
			foreach (string name in installedFontNames) AddFontName(fontNames, seen, name);

			return new StandardValuesCollection(fontNames);
		}

		private static void AddFontName(List<string> fontNames, HashSet<string> seen, string name)
		{
			if (!string.IsNullOrWhiteSpace(name) && seen.Add(name)) fontNames.Add(name);
		}
	}

	public class OrcaLegtoLegProfile : Indicator, IOrcaReplayParticipant
	{
		private class PriceLeg
		{
			public object SyncObj = new object();
			public long LegId;
			public int StartIndex;
			public int EndIndex;
			public DateTime StartTime;
			public DateTime EndTime;
			public double HighPrice;
			public double LowPrice;
			public OrcaLegDirection Direction;
			public Dictionary<double, long> VolByPrice = new Dictionary<double, long>();
			public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();
			public long TotalTradeVolume;
			public long CumulativeDelta;
			public long MaxCumulativeDelta;
			public long MinCumulativeDelta;
			public long LastTradeSequence;
			public DateTime LastTradeTime;
			public bool IsVACalculated = false;
			public double POCPrice = double.NaN;
			public double VAHPrice = double.NaN;
			public double VALPrice = double.NaN;
			public long MaxVol = 0;
			public int LastVolComp = -1;
		}

		private class ActiveDeltaReset
		{
			public long OwnerLegId;
			public DateTime AnchorCommandTimeUtc;
			public DateTime AnchorMarketTime;
			public long AnchorTradeSequence;
			public long BaselineTotalVolume;
			public long BaselineTotalDelta;
			public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();
			public long ResetCumulativeDelta;
			public long ResetMaxCumulativeDelta;
			public long ResetMinCumulativeDelta;
		}

		private struct ActiveDeltaResetSnapshot
		{
			public bool IsActive;
			public DateTime AnchorMarketTime;
			public long BaselineTotalVolume;
			public Dictionary<double, long> DeltaByPrice;
			public long ResetCumulativeDelta;
			public long ResetMaxCumulativeDelta;
			public long ResetMinCumulativeDelta;
		}

		private struct HotkeyGesture
		{
			public Key Key;
			public ModifierKeys Modifiers;
			public bool IsValid;
		}

		private LegTracker currentTracker;
		private LegTracker pastTracker;
		private LegTracker replayCurrentTracker;
		private LegTracker replayPastTracker;
		private readonly OrcaReplayTradeClassifier replayTradeClassifier = new OrcaReplayTradeClassifier();
		private volatile bool replaySnapshotPublished;
		private ATR atrIndicator;
		private int lastDynamicDeltaComp = -1;
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double prevLast = double.NaN;
		private int lastDirection;
		private long nextLegId;
		private long nextTradeSequence;
		private readonly object resetStateSync = new object();
		private ActiveDeltaReset activeDeltaReset;
		private string resetActionStatus;
		private DateTime resetActionStatusUntilUtc;
		private ChartControl resetHotkeyChartControl;
		private KeyEventHandler resetHotkeyHandler;
		private bool resetHotkeyAttached;
		private static readonly object ResetHotkeyRegistrySync = new object();
		private static readonly List<WeakReference> ResetHotkeyInstances = new List<WeakReference>();

		private TextFormat textFormat;
		private TextFormat statisticsTextFormat;
		private float lastBuiltTextFontSize = -1f;
		private float lastBuiltStatisticsFontSize = -1f;
		private string lastBuiltTextFormatSignature;
		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private SolidColorBrush posBrushDx, negBrushDx, textBrushDx, negativeTextBrushDx, statisticsTextBrushDx, volBrushDx, labelBgBrushDx, legBoxBrushDx;
		private SolidColorBrush pocBrushDx, vaVolBrushDx, vaLineBrushDx;
		private StrokeStyle vaLineStrokeDx;
		private SolidColorBrush[] volGradientBrushes;
		private SolidColorBrush[] vaGradientBrushes;
		private SolidColorBrush[] positiveDeltaIntensityBrushes;
		private SolidColorBrush[] negativeDeltaIntensityBrushes;
		private int lastBuiltGradientSteps = -1;
		private int lastBuiltVAGradientSteps = -1;
		private int lastBuiltDeltaIntensitySteps = -1;
		private float lastBuiltDeltaIntensityMinOpacity = -1f;
		private float lastBuiltDeltaIntensityMaxOpacity = -1f;
		private DateTime lastRenderSkipUtc = DateTime.MinValue;
		private int lastAppliedZOrder = int.MinValue;
		private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();
		private readonly string diagnosticsInstanceId = Guid.NewGuid().ToString("N");
		private bool diagnosticsRegistered;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaLegtoLegProfile";
				Description = "Displays volume and delta across price swings, with tick- or ATR-based leg detection. Includes active and historical profiles, point of control, value area, and optional active-leg delta resets with statistics.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;

				ReversalTicks = 20;
				PastReversalTicks = 40;
				UseAtrReversal = false;
				AtrPeriod = 14;
				AtrMultiplier = 1.0;
				PastAtrMultiplier = 2.0;
				MinimumBarsPerLeg = 1;
				MinimumDurationMinutes = 0;
				LegsToDisplay = 3;
				UseDynamicAggregation = false;
				DynamicAggregationMultiplier = 1.0;
				DeltaDynamicRowMinPixels = 10;
				DynamicDeltaMinCompression = 1;
				DynamicDeltaMaxCompression = 100;
				TradeSourceMode = LegProfileTradeSourceMode.SecondaryTickSeries;
				VolumeTickCompression = 6;
				DeltaTickCompression = 6;
				VolumeProfileWidthPx = 150;
				DeltaProfileWidthPx = 100;
				PastVolumeWidthPx = 60;
				PastDeltaWidthPx = 40;
				RightOffsetPx = 60;
				ProfileSeparationPx = 20;
				ProfileBarSpacingPx = 0;
				DrawBehindCandles = false;
				ShowVolume = true;
				ShowDelta = true;
				ShowPastDelta = true;
				ShowCurrentLegBox = false;
				MirrorProfile = false;

				EnableActiveDeltaReset = false;
				ResetDisplayMode = LegProfileResetDisplayMode.SideBySide;
				ResetNowHotkey = "Ctrl+Alt+R";
				ClearResetHotkey = "Ctrl+Alt+C";
				FullLegOverlayOpacity = 0.25f;
				ResetSideBySideGapPx = 0;
				ShowResetStatus = true;
				ResetAggregationMode = LegProfileResetAggregationMode.FollowFullLeg;
				ResetDeltaAggregationTicks = 4;
				ResetAggregationRatio = 0.50;
				ShowFullLegTotalDelta = false;
				ShowFullLegFinishDelta = false;
				ShowFullLegDeltaPercent = false;
				ShowResetTotalDelta = true;
				ShowResetFinishDelta = true;
				ShowResetDeltaPercent = true;

				DeltaLabelFontSize = 10;
				TextFontFamily = "Segoe UI";
				TextFontWeight = LegProfileTextFontWeight.Bold;
				StatisticsFontSize = 12;
				ShowDeltaLabelBackground = true;
				VolumeOpacity = 0.6f;
				DeltaOpacity = 0.85f;
				UseDeltaIntensityColoring = true;
				DeltaIntensityMinOpacity = 0.35f;

				PositiveBrush = WpfBrushes.Lime;
				NegativeBrush = WpfBrushes.Red;
				VolumeBrush = WpfBrushes.RoyalBlue;
				TextBrush = WpfBrushes.LightGreen;
				NegativeTextBrush = WpfBrushes.LightCoral;
				StatisticsTextBrush = WpfBrushes.White;
				LabelBgBrush = WpfBrushes.Black;
				LegBoxBrush = WpfBrushes.Yellow;

				ShowPOC = true;
				POCBrush = WpfBrushes.DodgerBlue;
				UseGradient = true;
				GradientSteps = 16;
				MinBrightness = 0.20f;

				ShowValueArea = true;
				ShowVAColor = true;
				ShowVALines = true;
				ValueAreaPercent = 70;
				VALineThickness = 1.5f;
				VALineStyle = VALineStyleEnum.Dash;
				VABrush = WpfBrushes.CornflowerBlue;
				VALineBrush = WpfBrushes.White;
			}
			else if (State == State.Configure)
			{
				if (TradeSourceMode == LegProfileTradeSourceMode.SecondaryTickSeries)
					AddDataSeries(BarsPeriodType.Tick, 1);
				currentTracker = new LegTracker(this, ReversalTicks, AtrMultiplier, true);
				pastTracker = new LegTracker(this, PastReversalTicks > 0 ? PastReversalTicks : ReversalTicks, PastAtrMultiplier, false);
			}
			else if (State == State.DataLoaded)
			{
				lastBid = double.NaN;
				lastAsk = double.NaN;
				prevLast = double.NaN;
				lastDirection = 0;
				nextLegId = 0;
				nextTradeSequence = 0;
				ClearActiveResetState(null);
				if (UseAtrReversal)
				{
					atrIndicator = ATR(AtrPeriod);
				}
				ReportDiagnosticsState();
			}
			else if (State == State.Historical)
			{
				ApplyProfileZOrder();
				QueueAttachResetHotkeys();
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				ReportDiagnosticsState();
			}
			else if (State == State.Realtime)
			{
				QueueAttachResetHotkeys();
				if (ChartControl != null) OrcaReplayCore.RegisterParticipant(ChartControl, this);
				ReportDiagnosticsState();
			}
			else if (State == State.Terminated)
			{
				if (ChartControl != null) OrcaReplayCore.UnregisterParticipant(ChartControl, this);
				RestoreLiveState();
				DetachResetHotkeys();
				ClearActiveResetState(null);
				OrcaDiagnosticsCore.UnregisterInstance(diagnosticsInstanceId);
				DisposeDx();
			}
		}

		private long NextLegId()
		{
			return System.Threading.Interlocked.Increment(ref nextLegId);
		}

		private void OnActiveLegChanged(PriceLeg oldLeg, PriceLeg newLeg)
		{
			if (oldLeg == null || newLeg == null || oldLeg.LegId == newLeg.LegId)
				return;

			bool cleared = false;
			lock (resetStateSync)
			{
				if (activeDeltaReset != null)
				{
					activeDeltaReset = null;
					resetActionStatus = "Reset cleared - new leg";
					resetActionStatusUntilUtc = DateTime.UtcNow.AddSeconds(3);
					cleared = true;
				}
			}
			if (cleared)
				RequestResetVisualRefresh();
		}

		private void ObserveResetTradeUnderLegLock(PriceLeg leg, double price, long signedVolume)
		{
			lock (resetStateSync)
			{
				if (activeDeltaReset == null || leg == null || activeDeltaReset.OwnerLegId != leg.LegId)
					return;

				if (signedVolume != 0)
				{
					int resetAggregationTicks;
					if (ResetAggregationMode == LegProfileResetAggregationMode.FullLegRatio)
						resetAggregationTicks = 1;
					else if (ResetAggregationMode == LegProfileResetAggregationMode.FixedTicks)
						resetAggregationTicks = Math.Max(1, ResetDeltaAggregationTicks);
					else
						resetAggregationTicks = UseDynamicAggregation ? 1 : Math.Max(1, DeltaTickCompression);
					double resetAggregation = resetAggregationTicks * TickSize;
					double resetPrice = Math.Floor(price / resetAggregation + 0.000001) * resetAggregation;
					activeDeltaReset.DeltaByPrice[resetPrice] = activeDeltaReset.DeltaByPrice.TryGetValue(resetPrice, out long delta)
						? delta + signedVolume
						: signedVolume;
				}
				activeDeltaReset.ResetCumulativeDelta += signedVolume;
				activeDeltaReset.ResetMaxCumulativeDelta = Math.Max(activeDeltaReset.ResetMaxCumulativeDelta, activeDeltaReset.ResetCumulativeDelta);
				activeDeltaReset.ResetMinCumulativeDelta = Math.Min(activeDeltaReset.ResetMinCumulativeDelta, activeDeltaReset.ResetCumulativeDelta);
			}
		}

		private ActiveDeltaResetSnapshot GetActiveResetSnapshotUnderLegLock(PriceLeg leg)
		{
			lock (resetStateSync)
			{
				if (activeDeltaReset == null || leg == null || activeDeltaReset.OwnerLegId != leg.LegId)
					return new ActiveDeltaResetSnapshot();

				return new ActiveDeltaResetSnapshot
				{
					IsActive = true,
					AnchorMarketTime = activeDeltaReset.AnchorMarketTime,
					BaselineTotalVolume = activeDeltaReset.BaselineTotalVolume,
					DeltaByPrice = new Dictionary<double, long>(activeDeltaReset.DeltaByPrice),
					ResetCumulativeDelta = activeDeltaReset.ResetCumulativeDelta,
					ResetMaxCumulativeDelta = activeDeltaReset.ResetMaxCumulativeDelta,
					ResetMinCumulativeDelta = activeDeltaReset.ResetMinCumulativeDelta
				};
			}
		}

		private void ApplyResetCommand()
		{
			if (State != State.Realtime)
			{
				SetResetActionStatus("Reset is available in real-time only");
				return;
			}
			if (!EnableActiveDeltaReset)
			{
				SetResetActionStatus("Active delta reset is disabled");
				return;
			}
			if (!ShowDelta)
			{
				SetResetActionStatus("Enable Show Delta before resetting");
				return;
			}

			PriceLeg leg = currentTracker != null ? currentTracker.GetCurrentLegSnapshot() : null;
			if (leg == null)
			{
				SetResetActionStatus("No active leg to reset");
				return;
			}

			bool replaced;
			lock (leg.SyncObj)
			{
				lock (resetStateSync)
				{
					replaced = activeDeltaReset != null && activeDeltaReset.OwnerLegId == leg.LegId;
					activeDeltaReset = new ActiveDeltaReset
					{
						OwnerLegId = leg.LegId,
						AnchorCommandTimeUtc = DateTime.UtcNow,
						AnchorMarketTime = leg.LastTradeTime != DateTime.MinValue ? leg.LastTradeTime : leg.StartTime,
						AnchorTradeSequence = leg.LastTradeSequence,
						BaselineTotalVolume = leg.TotalTradeVolume,
						BaselineTotalDelta = leg.CumulativeDelta,
						ResetCumulativeDelta = 0,
						ResetMaxCumulativeDelta = 0,
						ResetMinCumulativeDelta = 0
					};
					resetActionStatus = replaced ? "Reset anchor replaced" : "Reset active";
					resetActionStatusUntilUtc = DateTime.UtcNow.AddSeconds(3);
				}
			}
			RequestResetVisualRefresh();
		}

		private void ClearResetCommand()
		{
			bool hadReset;
			lock (resetStateSync)
			{
				hadReset = activeDeltaReset != null;
				activeDeltaReset = null;
				resetActionStatus = hadReset ? "Reset cleared" : "No active reset";
				resetActionStatusUntilUtc = DateTime.UtcNow.AddSeconds(3);
			}
			RequestResetVisualRefresh();
		}

		private void ClearActiveResetState(string status)
		{
			lock (resetStateSync)
			{
				activeDeltaReset = null;
				resetActionStatus = status;
				resetActionStatusUntilUtc = string.IsNullOrEmpty(status) ? DateTime.MinValue : DateTime.UtcNow.AddSeconds(3);
			}
		}

		private void SetResetActionStatus(string status)
		{
			lock (resetStateSync)
			{
				resetActionStatus = status;
				resetActionStatusUntilUtc = DateTime.UtcNow.AddSeconds(3);
			}
			RequestResetVisualRefresh();
		}

		private string GetResetActionStatus()
		{
			lock (resetStateSync)
			{
				return !string.IsNullOrEmpty(resetActionStatus) && DateTime.UtcNow <= resetActionStatusUntilUtc ? resetActionStatus : null;
			}
		}

		private void RequestResetVisualRefresh()
		{
			ChartControl chart = ChartControl;
			if (chart == null) return;
			try
			{
				chart.Dispatcher.InvokeAsync(() =>
				{
					try { chart.InvalidateVisual(); }
					catch { }
				});
			}
			catch { }
		}

		private void QueueAttachResetHotkeys()
		{
			if (!EnableActiveDeltaReset || resetHotkeyAttached || ChartControl == null)
				return;

			ChartControl chart = ChartControl;
			try
			{
				chart.Dispatcher.InvokeAsync(() =>
				{
					if (State == State.Terminated || resetHotkeyAttached || ChartControl == null || !ReferenceEquals(chart, ChartControl))
						return;

					resetHotkeyHandler = OnResetHotkeyPreviewKeyDown;
					resetHotkeyChartControl = chart;
					chart.AddHandler(Keyboard.PreviewKeyDownEvent, resetHotkeyHandler, true);
					resetHotkeyAttached = true;
					RegisterResetHotkeyInstance();

					HotkeyGesture resetGesture, clearGesture;
					if (!TryParseHotkey(ResetNowHotkey, out resetGesture) || !TryParseHotkey(ClearResetHotkey, out clearGesture))
						SetResetActionStatus("Invalid delta reset hotkey setting");
				});
			}
			catch { }
		}

		private void DetachResetHotkeys()
		{
			ChartControl chart = resetHotkeyChartControl;
			KeyEventHandler handler = resetHotkeyHandler;
			resetHotkeyAttached = false;
			resetHotkeyChartControl = null;
			resetHotkeyHandler = null;
			UnregisterResetHotkeyInstance();
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

		private void OnResetHotkeyPreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e == null || e.Handled || e.IsRepeat || !EnableActiveDeltaReset || !resetHotkeyAttached)
				return;
			if (OrcaReplayCore.IsChartLocked(resetHotkeyChartControl))
			{
				e.Handled = true;
				return;
			}
			if (IsTextInputFocused())
				return;

			HotkeyGesture resetGesture, clearGesture;
			bool resetValid = TryParseHotkey(ResetNowHotkey, out resetGesture);
			bool clearValid = TryParseHotkey(ClearResetHotkey, out clearGesture);
			bool isReset = resetValid && HotkeyMatches(e, resetGesture);
			bool isClear = clearValid && HotkeyMatches(e, clearGesture);
			if (!isReset && !isClear)
				return;

			e.Handled = true;
			HotkeyGesture matchedGesture = isReset ? resetGesture : clearGesture;
			if ((isReset && isClear) || CountResetHotkeyOwners(resetHotkeyChartControl, matchedGesture) > 1)
			{
				SetResetActionStatus("Delta reset hotkey conflict - no action taken");
				return;
			}

			try
			{
				TriggerCustomEvent(o =>
				{
					if ((bool)o) ApplyResetCommand();
					else ClearResetCommand();
				}, isReset);
			}
			catch (Exception ex)
			{
				SetResetActionStatus("Delta reset command failed: " + ex.Message);
			}
		}

		private static bool IsTextInputFocused()
		{
			object focused = Keyboard.FocusedElement;
			return focused is System.Windows.Controls.Primitives.TextBoxBase
				|| focused is System.Windows.Controls.PasswordBox
				|| focused is System.Windows.Controls.ComboBox;
		}

		private static bool TryParseHotkey(string configured, out HotkeyGesture gesture)
		{
			gesture = new HotkeyGesture();
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
					if (key != Key.None || !Enum.TryParse(part, true, out parsed) || parsed == Key.None)
						return false;
					key = parsed;
				}
			}

			if (key == Key.None) return false;
			gesture = new HotkeyGesture { Key = key, Modifiers = modifiers, IsValid = true };
			return true;
		}

		private static bool HotkeyMatches(KeyEventArgs e, HotkeyGesture gesture)
		{
			if (e == null || !gesture.IsValid) return false;
			Key eventKey = e.Key == Key.System ? e.SystemKey : e.Key;
			return eventKey == gesture.Key && Keyboard.Modifiers == gesture.Modifiers;
		}

		private static bool HotkeysEqual(HotkeyGesture left, HotkeyGesture right)
		{
			return left.IsValid && right.IsValid && left.Key == right.Key && left.Modifiers == right.Modifiers;
		}

		private void RegisterResetHotkeyInstance()
		{
			lock (ResetHotkeyRegistrySync)
			{
				PruneResetHotkeyInstances();
				foreach (WeakReference reference in ResetHotkeyInstances)
					if (ReferenceEquals(reference.Target, this)) return;
				ResetHotkeyInstances.Add(new WeakReference(this));
			}
		}

		private void UnregisterResetHotkeyInstance()
		{
			lock (ResetHotkeyRegistrySync)
			{
				for (int i = ResetHotkeyInstances.Count - 1; i >= 0; i--)
				{
					object target = ResetHotkeyInstances[i].Target;
					if (target == null || ReferenceEquals(target, this)) ResetHotkeyInstances.RemoveAt(i);
				}
			}
		}

		private static void PruneResetHotkeyInstances()
		{
			for (int i = ResetHotkeyInstances.Count - 1; i >= 0; i--)
				if (!ResetHotkeyInstances[i].IsAlive || ResetHotkeyInstances[i].Target == null) ResetHotkeyInstances.RemoveAt(i);
		}

		private static int CountResetHotkeyOwners(ChartControl chart, HotkeyGesture gesture)
		{
			if (chart == null || !gesture.IsValid) return 0;
			int count = 0;
			lock (ResetHotkeyRegistrySync)
			{
				PruneResetHotkeyInstances();
				foreach (WeakReference reference in ResetHotkeyInstances)
				{
					OrcaLegtoLegProfile indicator = reference.Target as OrcaLegtoLegProfile;
					if (indicator == null || !indicator.EnableActiveDeltaReset || !indicator.resetHotkeyAttached || !ReferenceEquals(indicator.resetHotkeyChartControl, chart))
						continue;

					HotkeyGesture resetGesture, clearGesture;
					bool ownsReset = TryParseHotkey(indicator.ResetNowHotkey, out resetGesture) && HotkeysEqual(resetGesture, gesture);
					bool ownsClear = TryParseHotkey(indicator.ClearResetHotkey, out clearGesture) && HotkeysEqual(clearGesture, gesture);
					if (ownsReset || ownsClear) count++;
				}
			}
			return count;
		}

		private void ApplyProfileZOrder()
		{
			if (ChartControl == null) return;

			int desiredZOrder = replaySnapshotPublished ? int.MaxValue - 16 : (DrawBehindCandles ? -1000 : 0);
			if (lastAppliedZOrder == desiredZOrder) return;

			SetZOrder(desiredZOrder);
			lastAppliedZOrder = desiredZOrder;
		}

		private void EnsureDiagnosticsRegistered()
		{
			if (diagnosticsRegistered)
				return;

			string sourceMode = TradeSourceMode == LegProfileTradeSourceMode.TickReplayLastEvents ? "TickReplayLastEvents" : "HiddenSecondaryTickSeries";
			OrcaDiagnosticsCore.RegisterInstance(diagnosticsInstanceId, "OrcaLegtoLegProfile", this);
			OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsInstanceId, sourceMode, "Unknown", "LocalBarSeries");
			OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 0, "PrimaryChartSeries", "Chart", "Primary bars");
			if (TradeSourceMode == LegProfileTradeSourceMode.SecondaryTickSeries)
				OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsInstanceId, 1, "Tick 1 Last", "Component", "Leg profile trade aggregation");
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

		private void DisposeDx()
		{
			try
			{
				textFormat?.Dispose();
				statisticsTextFormat?.Dispose();
				posBrushDx?.Dispose();
				negBrushDx?.Dispose();
				textBrushDx?.Dispose();
				negativeTextBrushDx?.Dispose();
				statisticsTextBrushDx?.Dispose();
				volBrushDx?.Dispose();
				labelBgBrushDx?.Dispose();
				legBoxBrushDx?.Dispose();
				pocBrushDx?.Dispose();
				vaVolBrushDx?.Dispose();
				vaLineBrushDx?.Dispose();
				vaLineStrokeDx?.Dispose();
				if (volGradientBrushes != null) foreach (var b in volGradientBrushes) b?.Dispose();
				if (vaGradientBrushes != null) foreach (var b in vaGradientBrushes) b?.Dispose();
				if (positiveDeltaIntensityBrushes != null) foreach (var b in positiveDeltaIntensityBrushes) b?.Dispose();
				if (negativeDeltaIntensityBrushes != null) foreach (var b in negativeDeltaIntensityBrushes) b?.Dispose();
			}
			catch { }
			finally
			{
				textFormat = null; statisticsTextFormat = null; posBrushDx = null; negBrushDx = null; textBrushDx = null; negativeTextBrushDx = null; statisticsTextBrushDx = null;
				volBrushDx = null; labelBgBrushDx = null; legBoxBrushDx = null;
				pocBrushDx = null; vaVolBrushDx = null; vaLineBrushDx = null; vaLineStrokeDx = null;
				volGradientBrushes = null; vaGradientBrushes = null;
				positiveDeltaIntensityBrushes = null; negativeDeltaIntensityBrushes = null;
				dxResourceRenderTarget = IntPtr.Zero;
				lastBuiltGradientSteps = -1; lastBuiltVAGradientSteps = -1;
				lastBuiltDeltaIntensitySteps = -1; lastBuiltDeltaIntensityMinOpacity = -1f; lastBuiltDeltaIntensityMaxOpacity = -1f;
				lastBuiltTextFontSize = -1f; lastBuiltStatisticsFontSize = -1f; lastBuiltTextFormatSignature = null;
				textWidthCache.Clear();
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}

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
				if (e.Ask > 0 && !double.IsNaN(e.Ask))
					lastAsk = e.Ask;
				if (e.Bid > 0 && !double.IsNaN(e.Bid))
					lastBid = e.Bid;

				if (TradeSourceMode == LegProfileTradeSourceMode.TickReplayLastEvents)
				{
					DateTime tradeTime = e.Time == DateTime.MinValue ? GetCurrentPrimaryTime() : e.Time;
					ProcessTradeEvent(e.Price, NormalizeTradeVolume(e.Volume), tradeTime);
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

			if (BarsInProgress == 1)
			{
				ProcessSecondaryTickSeriesEvent();
				// Removed ForceRefresh() to fix UI Thread lagging
			}
			else if (BarsInProgress == 0)
			{
				if (CurrentBar > 0)
				{
					PriceLeg activeLeg = currentTracker != null ? currentTracker.GetCurrentLegSnapshot() : null;
					PriceLeg pastLeg = pastTracker != null ? pastTracker.GetCurrentLegSnapshot() : null;
					if (activeLeg != null) lock (activeLeg.SyncObj) activeLeg.EndIndex = CurrentBar;
					if (pastLeg != null) lock (pastLeg.SyncObj) pastLeg.EndIndex = CurrentBar;
				}
			}
			}
			finally
			{
				if (diagnosticsWorkStart > 0)
					OrcaDiagnosticsCore.ReportWorkSample(diagnosticsInstanceId, OrcaDiagnosticsWorkKind.BarUpdate, diagnosticsBarsInProgress, diagnosticsWorkStart);
			}
		}

		private void ProcessSecondaryTickSeriesEvent()
		{
			if (TradeSourceMode != LegProfileTradeSourceMode.SecondaryTickSeries)
				return;

			if (BarsArray == null || BarsArray.Length < 2 || CurrentBars == null || CurrentBars.Length < 2 || CurrentBars[1] < 0)
				return;

			ProcessTradeEvent(Closes[1][0], NormalizeTradeVolume((long)Volumes[1][0]), Times[1][0]);
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

		private void ProcessTradeEvent(double last, long vol, DateTime time)
		{
			if (BarsArray == null || BarsArray.Length < 1 || BarsArray[0] == null || time == DateTime.MinValue)
				return;

			if (vol <= 0 || double.IsNaN(last) || double.IsInfinity(last))
				return;

			int primaryBarIndex = BarsArray[0].GetBar(time);
			if (primaryBarIndex < 0)
				return;

			long tradeSequence = System.Threading.Interlocked.Increment(ref nextTradeSequence);
			long signedVol = ClassifySignedVolume(last, vol);
			currentTracker?.ProcessBarUpdate(last, vol, signedVol, time, primaryBarIndex, tradeSequence);
			pastTracker?.ProcessBarUpdate(last, vol, signedVol, time, primaryBarIndex, tradeSequence);
			if (OrcaDiagnosticsCore.IsEnabled)
				OrcaDiagnosticsCore.ReportModelUpdate(diagnosticsInstanceId, time, null);
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

		private class LegTracker
		{
			private OrcaLegtoLegProfile parent;
			private readonly bool isActiveDisplayTracker;
			public readonly object SyncObj = new object();
			public int TickReversalThreshold;
			public List<PriceLeg> CompletedLegs = new List<PriceLeg>();
			public PriceLeg CurrentLeg;

			private double currentExtremePrice = double.NaN;
			private int currentExtremeBar = -1;
			private DateTime currentExtremeTime;
			private OrcaLegDirection legDir = OrcaLegDirection.Unknown;
			private readonly bool isReplayTracker;
			private long replayNextLegId;

			private struct TickRecord { public double Price; public long Volume; public long SignedVolume; public DateTime Time; public long Sequence; }
			private List<TickRecord> ticksSinceExtreme = new List<TickRecord>();

			public double AtrMultiplier;

			public LegTracker(OrcaLegtoLegProfile indicator, int reversalTicks, double atrMultiplier, bool activeDisplayTracker, bool replayTracker = false)
			{
				parent = indicator;
				TickReversalThreshold = reversalTicks;
				AtrMultiplier = atrMultiplier;
				isActiveDisplayTracker = activeDisplayTracker;
				isReplayTracker = replayTracker;
			}

			private long AllocateLegId()
			{
				return isReplayTracker ? ++replayNextLegId : parent.NextLegId();
			}

			public PriceLeg GetCurrentLegSnapshot()
			{
				lock (SyncObj)
					return CurrentLeg;
			}

			private void PublishCurrentLeg(PriceLeg newLeg)
			{
				PriceLeg oldLeg;
				lock (SyncObj)
				{
					oldLeg = CurrentLeg;
					CurrentLeg = newLeg;
				}
				if (isActiveDisplayTracker)
					parent.OnActiveLegChanged(oldLeg, newLeg);
			}

			public void ProcessBarUpdate(double last, long vol, long signedVol, DateTime time, int primaryBarIndex, long tradeSequence)
			{
				PriceLeg currentLeg = GetCurrentLegSnapshot();
				if (currentLeg == null)
				{
					StartNewLegAtCurrentTick(OrcaLegDirection.Up, last, time, primaryBarIndex);
					return;
				}

				bool newExtremeFound = false;
				if (legDir == OrcaLegDirection.Up || legDir == OrcaLegDirection.Unknown)
				{
					if (double.IsNaN(currentExtremePrice) || last >= currentExtremePrice)
					{
						currentExtremePrice = last; currentExtremeTime = time; newExtremeFound = true;
					}
				}
				if (legDir == OrcaLegDirection.Down || legDir == OrcaLegDirection.Unknown)
				{
					if (double.IsNaN(currentExtremePrice) || last <= currentExtremePrice)
					{
						currentExtremePrice = last; currentExtremeTime = time; newExtremeFound = true;
					}
				}

				if (newExtremeFound) ticksSinceExtreme.Clear();
				ticksSinceExtreme.Add(new TickRecord { Price = last, Volume = vol, SignedVolume = signedVol, Time = time, Sequence = tradeSequence });

				if (!newExtremeFound)
				{
					double reversalThreshold = (TickReversalThreshold * parent.TickSize);
					if (parent.UseAtrReversal && parent.atrIndicator != null && parent.CurrentBars[0] >= parent.AtrPeriod)
					{
						try
						{
							double atrVal = parent.atrIndicator[0];
							if (atrVal > 0)
							{
								reversalThreshold = atrVal * AtrMultiplier;
							}
						}
						catch { }
					}

					reversalThreshold = Math.Max(reversalThreshold, parent.TickSize);

					bool durationMet = parent.MinimumDurationMinutes == 0 || (time - currentLeg.StartTime).TotalMinutes >= parent.MinimumDurationMinutes;
					if (durationMet)
					{
						if (legDir == OrcaLegDirection.Up && (currentExtremePrice - last) >= reversalThreshold)
						{
							HandleReversalTick(OrcaLegDirection.Down, last, time, primaryBarIndex); return;
						}
						else if (legDir == OrcaLegDirection.Down && (last - currentExtremePrice) >= reversalThreshold)
						{
							HandleReversalTick(OrcaLegDirection.Up, last, time, primaryBarIndex); return;
						}
					}
				}

				ProcessTickToLeg(currentLeg, last, vol, signedVol, time, tradeSequence);
				lock (currentLeg.SyncObj)
				{
					currentLeg.HighPrice = Math.Max(currentLeg.HighPrice, last);
					currentLeg.LowPrice = Math.Min(currentLeg.LowPrice, last);
					currentLeg.EndTime = time;
				}
			}

			private void HandleReversalTick(OrcaLegDirection newDir, double currentTickPrice, DateTime time, int primaryBarIndex)
			{
				PriceLeg oldLeg = GetCurrentLegSnapshot();
				if (oldLeg == null) return;
				if (Math.Abs(oldLeg.HighPrice - oldLeg.LowPrice) / parent.TickSize >= parent.MinimumLegTicks)
				{
					lock (SyncObj)
					{
						CompletedLegs.Add(oldLeg);
						while (CompletedLegs.Count > parent.LegsToDisplay) CompletedLegs.RemoveAt(0);
					}
				}

				legDir = newDir;
				PriceLeg newLeg = new PriceLeg { LegId = AllocateLegId(), StartIndex = currentExtremeBar > -1 ? currentExtremeBar : primaryBarIndex, StartTime = currentExtremeTime, EndIndex = primaryBarIndex, EndTime = time, HighPrice = currentExtremePrice, LowPrice = currentExtremePrice, Direction = newDir };

				foreach (var t in ticksSinceExtreme)
				{
					ProcessTickToLeg(newLeg, t.Price, t.Volume, t.SignedVolume, t.Time, t.Sequence);
					newLeg.HighPrice = Math.Max(newLeg.HighPrice, t.Price);
					newLeg.LowPrice = Math.Min(newLeg.LowPrice, t.Price);
					newLeg.EndTime = t.Time;
				}
				ticksSinceExtreme.Clear();
				currentExtremePrice = currentTickPrice; currentExtremeBar = primaryBarIndex; currentExtremeTime = time;
				PublishCurrentLeg(newLeg);
			}

			private void StartNewLegAtCurrentTick(OrcaLegDirection dir, double last, DateTime time, int primaryBarIndex)
			{
				legDir = dir; currentExtremePrice = last; currentExtremeBar = primaryBarIndex; currentExtremeTime = time;
				PublishCurrentLeg(new PriceLeg { LegId = AllocateLegId(), StartIndex = primaryBarIndex, StartTime = time, EndIndex = primaryBarIndex, EndTime = time, HighPrice = last, LowPrice = last, Direction = dir });
			}

			private void ProcessTickToLeg(PriceLeg targetLeg, double price, long vol, long signedVol, DateTime time, long tradeSequence)
			{
				if (targetLeg == null || vol <= 0) return;
				double volComp = parent.VolumeTickCompression * parent.TickSize;
				double roundedVolPrice = Math.Floor(price / volComp + 0.000001) * volComp;
				lock (targetLeg.SyncObj)
				{
					targetLeg.TotalTradeVolume += vol;
					targetLeg.VolByPrice[roundedVolPrice] = targetLeg.VolByPrice.TryGetValue(roundedVolPrice, out long v) ? v + vol : vol;
					if (signedVol != 0)
					{
						int baseTicks = parent.UseDynamicAggregation ? 1 : parent.DeltaTickCompression;
						double deltaComp = baseTicks * parent.TickSize;
						double roundedDeltaPrice = Math.Floor(price / deltaComp + 0.000001) * deltaComp;
						targetLeg.DeltaByPrice[roundedDeltaPrice] = targetLeg.DeltaByPrice.TryGetValue(roundedDeltaPrice, out long d) ? d + signedVol : signedVol;
					}
					targetLeg.CumulativeDelta += signedVol;
					targetLeg.MaxCumulativeDelta = Math.Max(targetLeg.MaxCumulativeDelta, targetLeg.CumulativeDelta);
					targetLeg.MinCumulativeDelta = Math.Min(targetLeg.MinCumulativeDelta, targetLeg.CumulativeDelta);
					targetLeg.LastTradeSequence = tradeSequence;
					targetLeg.LastTradeTime = time;
					if (isActiveDisplayTracker)
						parent.ObserveResetTradeUnderLegLock(targetLeg, price, signedVol);
				}
			}
		}

		private bool CalcValueArea(Dictionary<double, long> volMap, double pocPrice, out double vahPrice, out double valPrice)
		{
			vahPrice = pocPrice; valPrice = pocPrice;
			if (volMap.Count <= 1) return false;

			var sortedPrices = new List<double>(volMap.Keys);
			sortedPrices.Sort();

			long totalVol = volMap.Values.Sum();
			if (totalVol <= 0) return false;

			double targetVol = totalVol * (ValueAreaPercent / 100.0);
			int pocIdx = sortedPrices.IndexOf(pocPrice);
			if (pocIdx < 0) return false;

			long accumulatedVol = volMap[pocPrice];
			int lo = pocIdx, hi = pocIdx;

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

		private string ReplayParticipantKey { get { return "OrcaLegtoLegProfile:" + diagnosticsInstanceId; } }

		string IOrcaReplayParticipant.ReplayParticipantId { get { return ReplayParticipantKey; } }

		OrcaReplayCapabilities IOrcaReplayParticipant.ReplayCapabilities
		{
			get
			{
				return new OrcaReplayCapabilities(true, true, true, false,
					OrcaReplayChartStyleSupport.AllV1);
			}
		}

		public OrcaReplayCheckpoint CaptureReplayCheckpoint(OrcaReplayContext context)
		{
			if (UseAtrReversal)
				throw new InvalidOperationException("ATR-based Leg-to-Leg replay requires the bar/ATR checkpoint adapter.");
			// V1 reference adapter rebuilds from the retained tape floor. A future measured
			// optimization can replace this clean baseline with immutable five-minute clones.
			return new OrcaReplayCheckpoint(DateTime.MinValue, 0, -1, ReplayParticipantKey, null);
		}

		public void PrepareReplay(OrcaReplayContext context, OrcaReplayCheckpoint checkpoint)
		{
			replayCurrentTracker = new LegTracker(this, ReversalTicks, AtrMultiplier, false, true);
			replayPastTracker = new LegTracker(this, PastReversalTicks > 0 ? PastReversalTicks : ReversalTicks,
				PastAtrMultiplier, false, true);
			replayTradeClassifier.Reset();
			replaySnapshotPublished = false;
		}

		public void ApplyReplayEvent(OrcaReplayContext context, OrcaReplayTradeEvent tradeEvent)
		{
			if (tradeEvent == null || tradeEvent.Volume <= 0 || replayCurrentTracker == null || replayPastTracker == null)
				return;
			long signedVolume = replayTradeClassifier.Classify(tradeEvent);
			replayCurrentTracker.ProcessBarUpdate(tradeEvent.Price, tradeEvent.Volume, signedVolume,
				tradeEvent.Time, tradeEvent.PrimaryBarIndex, tradeEvent.Sequence);
			replayPastTracker.ProcessBarUpdate(tradeEvent.Price, tradeEvent.Volume, signedVolume,
				tradeEvent.Time, tradeEvent.PrimaryBarIndex, tradeEvent.Sequence);
		}

		public void ApplyReplayBar(OrcaReplayContext context, int primaryBarIndex)
		{
			PriceLeg current = replayCurrentTracker != null ? replayCurrentTracker.GetCurrentLegSnapshot() : null;
			PriceLeg past = replayPastTracker != null ? replayPastTracker.GetCurrentLegSnapshot() : null;
			if (current != null) lock (current.SyncObj) current.EndIndex = primaryBarIndex;
			if (past != null) lock (past.SyncObj) past.EndIndex = primaryBarIndex;
		}

		public void PublishReplaySnapshot(OrcaReplayContext context)
		{
			replaySnapshotPublished = replayCurrentTracker != null && replayPastTracker != null;
		}

		public void RestoreLiveState()
		{
			replaySnapshotPublished = false;
			replayCurrentTracker = null;
			replayPastTracker = null;
			replayTradeClassifier.Reset();
		}

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
				ApplyProfileZOrder();
				base.OnRender(chartControl, chartScale);
				LegTracker renderCurrentTracker = replaySnapshotPublished ? replayCurrentTracker : currentTracker;
				LegTracker renderPastTracker = replaySnapshotPublished ? replayPastTracker : pastTracker;
				PriceLeg activeLeg = renderCurrentTracker != null ? renderCurrentTracker.GetCurrentLegSnapshot() : null;
				if (RenderTarget == null || chartControl == null || chartScale == null || activeLeg == null) return;
				NinjaTrader.Gui.Chart.ChartPanel panel;
				if (!TryGetRenderPanel(chartControl, chartScale, out panel)) return;
				EnsureDxResources();

			int dynamicVolComp = VolumeTickCompression;
			int dynamicDeltaComp = DeltaTickCompression;
			if (UseDynamicAggregation)
			{
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
				else if (desiredTicks <= 100) dynamicDeltaComp = (int)(Math.Round(desiredTicks / 20.0) * 20); // 60, 80, 100
				else dynamicDeltaComp = (int)(Math.Round(desiredTicks / 50.0) * 50);
				dynamicDeltaComp = ClampDeltaCompression(dynamicDeltaComp);

				if (lastDynamicDeltaComp > 0 && Math.Abs(dynamicDeltaComp - lastDynamicDeltaComp) < Math.Max(2, dynamicDeltaComp * 0.15))
				{
					dynamicDeltaComp = lastDynamicDeltaComp;
				}
				else
				{
					lastDynamicDeltaComp = dynamicDeltaComp;
				}
			}

				float rightmostEdge = chartControl.CanvasRight - RightOffsetPx - VolumeProfileWidthPx;
				DrawLegProfiles(chartControl, chartScale, panel, activeLeg, rightmostEdge, VolumeProfileWidthPx, DeltaProfileWidthPx, true, false, dynamicVolComp, dynamicDeltaComp);

				List<PriceLeg> completedLegs = GetCompletedLegSnapshot(renderPastTracker);
				if (LegsToDisplay > 0 && completedLegs.Count > 0)
				{
					for (int i = completedLegs.Count - 1; i >= 0; i--)
					{
						var leg = completedLegs[i];
						float originX;
						if (!TryGetXByTime(chartControl, leg.StartTime, out originX)) continue;
						DrawLegProfiles(chartControl, chartScale, panel, leg, originX, PastVolumeWidthPx, PastDeltaWidthPx, false, !ShowPastDelta, dynamicVolComp, dynamicDeltaComp);
					}
				}
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

		private void PrintRenderSkip(Exception ex)
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastRenderSkipUtc).TotalSeconds < 30) return;
			lastRenderSkipUtc = now;
			Print("OrcaLegtoLegProfile: skipped one render frame: " + ex.Message);
		}

		private int ClampDeltaCompression(int compression)
		{
			int min = Math.Max(1, DynamicDeltaMinCompression);
			int max = Math.Max(min, DynamicDeltaMaxCompression);
			if (compression < min) return min;
			if (compression > max) return max;
			return compression;
		}

		private int ResolveResetAggregationTicks(int fullLegAggregationTicks)
		{
			int fullLegTicks = Math.Max(1, fullLegAggregationTicks);
			if (ResetAggregationMode == LegProfileResetAggregationMode.FixedTicks)
				return Math.Max(1, ResetDeltaAggregationTicks);
			if (ResetAggregationMode != LegProfileResetAggregationMode.FullLegRatio)
				return fullLegTicks;

			double configuredRatio = ResetAggregationRatio;
			if (double.IsNaN(configuredRatio) || double.IsInfinity(configuredRatio))
				configuredRatio = 0.50;
			configuredRatio = Math.Max(0.05, Math.Min(1.00, configuredRatio));
			decimal scaledTicks = Convert.ToDecimal(fullLegTicks) * Convert.ToDecimal(configuredRatio);
			return Math.Max(1, Convert.ToInt32(Math.Ceiling(scaledTicks)));
		}

		private bool TryGetRenderPanel(ChartControl chartControl, ChartScale chartScale, out NinjaTrader.Gui.Chart.ChartPanel panel)
		{
			panel = null;
			try
			{
				if (chartControl == null || chartScale == null) return false;
				int panelIndex = chartScale.PanelIndex;
				if (chartControl.ChartPanels != null && panelIndex >= 0 && panelIndex < chartControl.ChartPanels.Count)
					panel = chartControl.ChartPanels[panelIndex];
				else
					panel = this.ChartPanel;

				return panel != null && panel.W > 0 && panel.H > 0;
			}
			catch { return false; }
		}

		private List<PriceLeg> GetCompletedLegSnapshot(LegTracker tracker)
		{
			if (tracker == null) return new List<PriceLeg>();
			lock (tracker.SyncObj)
				return tracker.CompletedLegs != null ? new List<PriceLeg>(tracker.CompletedLegs) : new List<PriceLeg>();
		}

		private bool TryGetXByTime(ChartControl chartControl, DateTime time, out float x)
		{
			x = 0f;
			try
			{
				if (chartControl == null || time == DateTime.MinValue) return false;
				x = chartControl.GetXByTime(time);
				return !float.IsNaN(x) && !float.IsInfinity(x);
			}
			catch { return false; }
		}

		private void DrawLegProfiles(ChartControl chartControl, ChartScale chartScale, NinjaTrader.Gui.Chart.ChartPanel panel, PriceLeg leg, float originX, int vWidth, int dWidth, bool isCurrent, bool forceHideDelta, int volCompTicks, int deltaCompTicks)
		{
			if (chartControl == null || chartScale == null || panel == null || leg == null || TickSize <= 0) return;
			volCompTicks = Math.Max(1, volCompTicks);
			deltaCompTicks = Math.Max(1, deltaCompTicks);
			vWidth = Math.Max(1, vWidth);
			dWidth = Math.Max(1, dWidth);

			double highPrice, lowPrice;
			Dictionary<double, long> volSnapshot = null;
			Dictionary<double, long> deltaSnapshot = null;
			ActiveDeltaResetSnapshot resetSnapshot = new ActiveDeltaResetSnapshot();
			long fullTotalVolume = 0, fullTotalDelta = 0, fullMaxCumulativeDelta = 0, fullMinCumulativeDelta = 0;
			lock (leg.SyncObj)
			{
				highPrice = leg.HighPrice;
				lowPrice = leg.LowPrice;
				fullTotalVolume = leg.TotalTradeVolume;
				fullTotalDelta = leg.CumulativeDelta;
				fullMaxCumulativeDelta = leg.MaxCumulativeDelta;
				fullMinCumulativeDelta = leg.MinCumulativeDelta;
				if (ShowVolume && leg.VolByPrice.Count > 0)
					volSnapshot = new Dictionary<double, long>(leg.VolByPrice);
				if (ShowDelta && !forceHideDelta && leg.DeltaByPrice.Count > 0)
					deltaSnapshot = new Dictionary<double, long>(leg.DeltaByPrice);
				if (isCurrent && EnableActiveDeltaReset && !replaySnapshotPublished)
					resetSnapshot = GetActiveResetSnapshotUnderLegLock(leg);
			}

			if (ShowCurrentLegBox && isCurrent && !double.IsNaN(highPrice) && !double.IsNaN(lowPrice))
			{
				int topY = chartScale.GetYByValue(highPrice), bottomY = chartScale.GetYByValue(lowPrice);
				RenderTarget.DrawRectangle(new RectangleF(originX - dWidth - 5, topY - 5, vWidth + dWidth + 10, (bottomY - topY) + 10), legBoxBrushDx, 1f);
			}

			bool deltaVisibleForProfile = ShowDelta && !forceHideDelta;
			bool mirrorThisProfile = isCurrent ? !deltaVisibleForProfile : MirrorProfile;
			float spineX = mirrorThisProfile ? originX + vWidth : originX;
			if (ShowVolume && volSnapshot != null && volSnapshot.Count > 0)
			{
				Dictionary<double, long> targetVolMap;
				targetVolMap = volSnapshot;
				long maxVol = 0; double pocPrice = double.NaN;
				bool haveVA = false; double vahPrice = double.NaN, valPrice = double.NaN;

				if (!leg.IsVACalculated || isCurrent)
				{
					foreach (var kvp in targetVolMap) { if (kvp.Value > maxVol) { maxVol = kvp.Value; pocPrice = kvp.Key; } }
					if (maxVol > 0 && ShowValueArea && (ShowVAColor || ShowVALines))
						haveVA = CalcValueArea(targetVolMap, pocPrice, out vahPrice, out valPrice);

					if (!isCurrent)
					{
						leg.MaxVol = maxVol; leg.POCPrice = pocPrice;
						leg.VAHPrice = vahPrice; leg.VALPrice = valPrice;
						leg.IsVACalculated = true;
					}
				}
				else
				{
					maxVol = leg.MaxVol; pocPrice = leg.POCPrice;
					vahPrice = leg.VAHPrice; valPrice = leg.VALPrice;
					haveVA = !double.IsNaN(vahPrice);
				}

				if (maxVol > 0)
				{
					foreach (var kvp in targetVolMap)
					{
						int yTop = chartScale.GetYByValue(kvp.Key);
						if (yTop < panel.Y - 50 || yTop > panel.Y + panel.H + 50) continue;
						int yBot = chartScale.GetYByValue(kvp.Key - (volCompTicks * TickSize));
						int height = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
						float w = (float)(vWidth * (kvp.Value / (double)maxVol));
						if (w > 0.5f)
						{
							bool insideVA = haveVA && kvp.Key >= valPrice - TickSize * 0.01 && kvp.Key <= vahPrice + TickSize * 0.01;
							SolidColorBrush brush = volBrushDx;
							if (ShowPOC && Math.Abs(kvp.Key - pocPrice) < TickSize * 0.01) brush = pocBrushDx;
							else if (UseGradient)
							{
								var palette = (ShowValueArea && ShowVAColor && insideVA && vaGradientBrushes != null) ? vaGradientBrushes : volGradientBrushes;
								if (palette != null && palette.Length > 0)
								{
									int gradIdx = Math.Min(palette.Length - 1, Math.Max(0, (int)((kvp.Value / (double)maxVol) * (palette.Length - 1))));
									brush = palette[gradIdx];
								}
							}
							else brush = (ShowValueArea && ShowVAColor && insideVA) ? vaVolBrushDx : volBrushDx;

							float barX = mirrorThisProfile ? spineX - w : spineX;
							RenderTarget.FillRectangle(new RectangleF(barX, Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f, w, height), brush);
						}
					}
					if (haveVA && ShowValueArea && ShowVALines && vaLineBrushDx != null)
					{
						float lineLeft = mirrorThisProfile ? spineX - vWidth - 2 : spineX - 2;
						float lineRight = mirrorThisProfile ? spineX + 2 : spineX + vWidth + 2;
						float yVAH = chartScale.GetYByValue(vahPrice);
						float yVAL = chartScale.GetYByValue(valPrice);
						if (yVAH >= panel.Y - 5 && yVAH <= panel.Y + panel.H + 5)
							RenderTarget.DrawLine(new Vector2(lineLeft, yVAH), new Vector2(lineRight, yVAH), vaLineBrushDx, VALineThickness, vaLineStrokeDx);
						if (yVAL >= panel.Y - 5 && yVAL <= panel.Y + panel.H + 5)
							RenderTarget.DrawLine(new Vector2(lineLeft, yVAL), new Vector2(lineRight, yVAL), vaLineBrushDx, VALineThickness, vaLineStrokeDx);
					}
				}
			}

			if (ShowDelta && !forceHideDelta)
			{
				Dictionary<double, long> fullDeltaMap = deltaSnapshot ?? new Dictionary<double, long>();
				if (!isCurrent || !resetSnapshot.IsActive)
				{
					DrawDeltaRows(chartScale, panel, fullDeltaMap, spineX, dWidth, mirrorThisProfile, deltaCompTicks, 1f, true);
					if (isCurrent)
						DrawFullLegStatistics(panel, spineX - dWidth, dWidth, fullTotalVolume, fullTotalDelta, fullMaxCumulativeDelta, fullMinCumulativeDelta, panel.Y + 4f);
				}
				else
				{
					Dictionary<double, long> resetDeltaMap = resetSnapshot.DeltaByPrice ?? new Dictionary<double, long>();
					int resetDeltaCompTicks = ResolveResetAggregationTicks(deltaCompTicks);
					float fullLaneLeft = spineX - dWidth;
					float resetSpineX = spineX;
					float resetLaneLeft = fullLaneLeft;
					if (ResetDisplayMode == LegProfileResetDisplayMode.SideBySide)
					{
						resetSpineX = spineX - dWidth - Math.Max(0, ResetSideBySideGapPx);
						resetLaneLeft = resetSpineX - dWidth;
						DrawDeltaRows(chartScale, panel, fullDeltaMap, spineX, dWidth, false, deltaCompTicks, 1f, true);
						DrawDeltaRows(chartScale, panel, resetDeltaMap, resetSpineX, dWidth, false, resetDeltaCompTicks, 1f, true);
						float nextY = DrawFullLegStatistics(panel, fullLaneLeft, dWidth, fullTotalVolume, fullTotalDelta, fullMaxCumulativeDelta, fullMinCumulativeDelta, panel.Y + 4f);
						DrawResetStatistics(panel, resetLaneLeft, dWidth, fullTotalVolume, resetSnapshot, nextY);
					}
					else if (ResetDisplayMode == LegProfileResetDisplayMode.Overlay)
					{
						DrawDeltaRows(chartScale, panel, fullDeltaMap, spineX, dWidth, false, deltaCompTicks, FullLegOverlayOpacity, false);
						DrawDeltaRows(chartScale, panel, resetDeltaMap, spineX, dWidth, false, resetDeltaCompTicks, 1f, true);
						float nextY = DrawFullLegStatistics(panel, fullLaneLeft, dWidth, fullTotalVolume, fullTotalDelta, fullMaxCumulativeDelta, fullMinCumulativeDelta, panel.Y + 4f);
						DrawResetStatistics(panel, fullLaneLeft, dWidth, fullTotalVolume, resetSnapshot, nextY);
					}
					else
					{
						DrawDeltaRows(chartScale, panel, resetDeltaMap, spineX, dWidth, false, resetDeltaCompTicks, 1f, true);
						float nextY = DrawFullLegStatistics(panel, fullLaneLeft, dWidth, fullTotalVolume, fullTotalDelta, fullMaxCumulativeDelta, fullMinCumulativeDelta, panel.Y + 4f);
						DrawResetStatistics(panel, fullLaneLeft, dWidth, fullTotalVolume, resetSnapshot, nextY);
					}
				}
			}

			if (isCurrent && ShowResetStatus)
			{
				string actionStatus = GetResetActionStatus();
				if (!string.IsNullOrEmpty(actionStatus))
					DrawStatisticsRow(panel, originX - dWidth, dWidth, panel.Y + panel.H - GetStatisticsRowHeight(), actionStatus, 1f);
			}
		}

		private void DrawDeltaRows(ChartScale chartScale, NinjaTrader.Gui.Chart.ChartPanel panel, Dictionary<double, long> deltaMap, float spineX, int dWidth, bool mirror, int deltaCompTicks, float opacityMultiplier, bool drawLabels)
		{
			if (deltaMap == null || deltaMap.Count == 0 || chartScale == null || panel == null) return;
			double deltaComp = Math.Max(1, deltaCompTicks) * TickSize;
			var groupedDelta = new Dictionary<double, long>();
			foreach (var kvp in deltaMap)
			{
				double bPrice = Math.Floor(kvp.Key / deltaComp + 0.000001) * deltaComp;
				groupedDelta[bPrice] = groupedDelta.TryGetValue(bPrice, out long existing) ? existing + kvp.Value : kvp.Value;
			}
			long maxAbsDelta = groupedDelta.Values.Select(v => Math.Abs(v)).DefaultIfEmpty(0).Max();
			if (maxAbsDelta <= 0) return;

			float effectiveOpacity = Math.Max(0.05f, Math.Min(1f, opacityMultiplier));
			foreach (var kvp in groupedDelta)
			{
				int yTop = chartScale.GetYByValue(kvp.Key + deltaComp);
				if (yTop < panel.Y - 50 || yTop > panel.Y + panel.H + 50) continue;
				int yBot = chartScale.GetYByValue(kvp.Key);
				int height = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
				float drawY = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;
				float width = (float)(dWidth * (Math.Abs(kvp.Value) / (double)maxAbsDelta));
				if (width <= 0.5f) continue;

				float barX = mirror ? spineX : spineX - width;
				FillRectangleWithOpacity(new RectangleF(barX, drawY, width, height), SelectDeltaBrush(kvp.Value, maxAbsDelta), effectiveOpacity);
				if (!drawLabels || height < DeltaLabelFontSize + 2) continue;

				string label = kvp.Value.ToString("+#;-#;0");
				float textWidth = MeasureTextWidth(label);
				float textX = mirror ? spineX + 2 : spineX - textWidth - 2;
				float textY = drawY + (height / 2f) - (DeltaLabelFontSize / 2f);
				if (ShowDeltaLabelBackground) RenderTarget.FillRectangle(new RectangleF(textX - 1, textY - 1, textWidth + 2, DeltaLabelFontSize + 2), labelBgBrushDx);
				SolidColorBrush labelBrush = kvp.Value >= 0 ? textBrushDx : negativeTextBrushDx;
				if (labelBrush != null)
					RenderTarget.DrawText(label, textFormat, new RectangleF(textX, textY, textWidth, DeltaLabelFontSize + 2), labelBrush);
			}
		}

		private void FillRectangleWithOpacity(RectangleF rectangle, SolidColorBrush brush, float opacityMultiplier)
		{
			if (brush == null) return;
			float priorOpacity = brush.Opacity;
			try
			{
				brush.Opacity = priorOpacity * Math.Max(0.05f, Math.Min(1f, opacityMultiplier));
				RenderTarget.FillRectangle(rectangle, brush);
			}
			finally { brush.Opacity = priorOpacity; }
		}

		private static long CalculateFinishDelta(long currentDelta, long maxCumulativeDelta, long minCumulativeDelta)
		{
			return currentDelta - (currentDelta >= 0 ? maxCumulativeDelta : minCumulativeDelta);
		}

		private float DrawFullLegStatistics(NinjaTrader.Gui.Chart.ChartPanel panel, float laneLeft, float laneWidth, long totalVolume, long totalDelta, long maxCumulativeDelta, long minCumulativeDelta, float startY)
		{
			if (!ShowFullLegTotalDelta && !ShowFullLegFinishDelta && !ShowFullLegDeltaPercent) return startY;
			double percent = totalVolume > 0 ? totalDelta / (double)totalVolume * 100.0 : 0.0;
			string statistics = BuildDeltaStatisticsText(
				ShowFullLegTotalDelta,
				ShowFullLegFinishDelta,
				ShowFullLegDeltaPercent,
				totalDelta,
				CalculateFinishDelta(totalDelta, maxCumulativeDelta, minCumulativeDelta),
				percent);
			return DrawStatisticsRow(panel, laneLeft, laneWidth, startY, statistics, 1f);
		}

		private float DrawResetStatistics(NinjaTrader.Gui.Chart.ChartPanel panel, float laneLeft, float laneWidth, long fullTotalVolume, ActiveDeltaResetSnapshot resetSnapshot, float startY)
		{
			bool showAnyStatistic = ShowResetTotalDelta || ShowResetFinishDelta || ShowResetDeltaPercent;
			if (!ShowResetStatus && !showAnyStatistic) return startY;
			float y = startY;
			if (ShowResetStatus)
			{
				string anchor = resetSnapshot.AnchorMarketTime != DateTime.MinValue ? resetSnapshot.AnchorMarketTime.ToString("HH:mm:ss") : "NOW";
				y = DrawStatisticsRow(panel, laneLeft, laneWidth, y, "RESET " + anchor, 1f);
			}
			long resetVolume = Math.Max(0, fullTotalVolume - resetSnapshot.BaselineTotalVolume);
			double percent = resetVolume > 0 ? resetSnapshot.ResetCumulativeDelta / (double)resetVolume * 100.0 : 0.0;
			string statistics = BuildDeltaStatisticsText(
				ShowResetTotalDelta,
				ShowResetFinishDelta,
				ShowResetDeltaPercent,
				resetSnapshot.ResetCumulativeDelta,
				CalculateFinishDelta(resetSnapshot.ResetCumulativeDelta, resetSnapshot.ResetMaxCumulativeDelta, resetSnapshot.ResetMinCumulativeDelta),
				percent);
			if (!string.IsNullOrEmpty(statistics))
				y = DrawStatisticsRow(panel, laneLeft, laneWidth, y, statistics, 1f);
			return y;
		}

		private static string BuildDeltaStatisticsText(bool showTotal, bool showFinish, bool showPercent, long totalDelta, long finishDelta, double deltaPercent)
		{
			string text = string.Empty;
			if (showTotal)
				text = "D " + totalDelta.ToString("+#;-#;0");
			if (showFinish)
				text = AppendStatisticsToken(text, "FD " + finishDelta.ToString("+#;-#;0"));
			if (showPercent)
				text = AppendStatisticsToken(text, deltaPercent.ToString("+0.0;-0.0;0.0") + "%");
			return text;
		}

		private static string AppendStatisticsToken(string text, string token)
		{
			return string.IsNullOrEmpty(text) ? token : text + " | " + token;
		}

		private float DrawStatisticsRow(NinjaTrader.Gui.Chart.ChartPanel panel, float laneLeft, float laneWidth, float y, string text, float opacity)
		{
			if (panel == null || statisticsTextFormat == null || string.IsNullOrEmpty(text)) return y;
			float rowHeight = GetStatisticsRowHeight();
			float alignedY = (float)Math.Round(y);
			if (alignedY + rowHeight > panel.Y + panel.H) return y;

			float panelLeft = panel.X + 4f;
			float panelRight = panel.X + panel.W - 4f;
			float laneRight = Math.Min(panelRight, laneLeft + Math.Max(20f, laneWidth));
			if (laneRight <= panelLeft) return y;

			float backgroundWidth = Math.Min(Math.Max(20f, laneWidth), laneRight - panelLeft);
			float backgroundLeft = laneRight - backgroundWidth;
			FillRectangleWithOpacity(new RectangleF(backgroundLeft, alignedY, backgroundWidth, rowHeight), labelBgBrushDx, Math.Max(0.2f, opacity));

			SolidColorBrush brush = statisticsTextBrushDx ?? textBrushDx ?? negativeTextBrushDx;
			if (brush != null)
				RenderTarget.DrawText(text, statisticsTextFormat, new RectangleF(panelLeft, alignedY, Math.Max(1f, laneRight - panelLeft - 2f), rowHeight), brush);
			return y + rowHeight + 1f;
		}

		private float GetStatisticsRowHeight()
		{
			return Math.Max(16f, StatisticsFontSize + 6f);
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null) return;
			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxResourceRenderTarget != IntPtr.Zero && dxResourceRenderTarget != currentTarget)
				DisposeDx();

			if (posBrushDx == null) posBrushDx = new SolidColorBrush(RenderTarget, ToDx(PositiveBrush, DeltaOpacity));
			if (negBrushDx == null) negBrushDx = new SolidColorBrush(RenderTarget, ToDx(NegativeBrush, DeltaOpacity));
			if (textBrushDx == null) textBrushDx = new SolidColorBrush(RenderTarget, ToDx(TextBrush, 1f));
			if (negativeTextBrushDx == null) negativeTextBrushDx = new SolidColorBrush(RenderTarget, ToDx(NegativeTextBrush, 1f));
			if (statisticsTextBrushDx == null) statisticsTextBrushDx = new SolidColorBrush(RenderTarget, ToDx(StatisticsTextBrush, 1f));
			if (volBrushDx == null) volBrushDx = new SolidColorBrush(RenderTarget, ToDx(VolumeBrush, VolumeOpacity));
			if (labelBgBrushDx == null) labelBgBrushDx = new SolidColorBrush(RenderTarget, ToDx(LabelBgBrush, 1f));
			if (legBoxBrushDx == null) legBoxBrushDx = new SolidColorBrush(RenderTarget, ToDx(LegBoxBrush, 1f));
			if (pocBrushDx == null) pocBrushDx = new SolidColorBrush(RenderTarget, ToDx(POCBrush, 1f));
			if (vaVolBrushDx == null) vaVolBrushDx = new SolidColorBrush(RenderTarget, ToDx(VABrush, VolumeOpacity));
			if (vaLineBrushDx == null) vaLineBrushDx = new SolidColorBrush(RenderTarget, ToDx(VALineBrush, 1f));
			if (vaLineStrokeDx == null)
			{
				DashStyle ds = VALineStyle == VALineStyleEnum.Solid ? DashStyle.Solid :
							   VALineStyle == VALineStyleEnum.Dot ? DashStyle.Dot :
							   VALineStyle == VALineStyleEnum.DashDot ? DashStyle.DashDot : DashStyle.Dash;
				vaLineStrokeDx = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = ds });
			}
			float textFontSize = Math.Max(5f, DeltaLabelFontSize);
			float statisticsFontSize = Math.Max(8f, StatisticsFontSize);
			string textFormatSignature = GetTextFormatSignature();
			if (textFormat == null || statisticsTextFormat == null || Math.Abs(lastBuiltTextFontSize - textFontSize) > 0.001f || Math.Abs(lastBuiltStatisticsFontSize - statisticsFontSize) > 0.001f || !string.Equals(lastBuiltTextFormatSignature, textFormatSignature, StringComparison.Ordinal))
			{
				textFormat?.Dispose();
				statisticsTextFormat?.Dispose();
				textFormat = CreateTextFormat(textFontSize);
				statisticsTextFormat = CreateTextFormat(statisticsFontSize);
				if (statisticsTextFormat != null)
					statisticsTextFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				lastBuiltTextFontSize = textFontSize;
				lastBuiltStatisticsFontSize = statisticsFontSize;
				lastBuiltTextFormatSignature = textFormatSignature;
				textWidthCache.Clear();
			}

			int steps = Math.Max(2, GradientSteps);
			if (UseGradient && (volGradientBrushes == null || lastBuiltGradientSteps != steps))
			{
				if (volGradientBrushes != null) foreach (var b in volGradientBrushes) b?.Dispose();
				volGradientBrushes = BuildGradientPalette(VolumeBrush, steps);
				lastBuiltGradientSteps = steps;
			}
			if (UseGradient && ShowValueArea && ShowVAColor && (vaGradientBrushes == null || lastBuiltVAGradientSteps != steps))
			{
				if (vaGradientBrushes != null) foreach (var b in vaGradientBrushes) b?.Dispose();
				vaGradientBrushes = BuildGradientPalette(VABrush, steps);
				lastBuiltVAGradientSteps = steps;
			}
			if (UseDeltaIntensityColoring && (positiveDeltaIntensityBrushes == null || negativeDeltaIntensityBrushes == null || lastBuiltDeltaIntensitySteps != steps || Math.Abs(lastBuiltDeltaIntensityMinOpacity - DeltaIntensityMinOpacity) > 0.0001f || Math.Abs(lastBuiltDeltaIntensityMaxOpacity - DeltaOpacity) > 0.0001f))
			{
				if (positiveDeltaIntensityBrushes != null) foreach (var b in positiveDeltaIntensityBrushes) b?.Dispose();
				if (negativeDeltaIntensityBrushes != null) foreach (var b in negativeDeltaIntensityBrushes) b?.Dispose();
				positiveDeltaIntensityBrushes = BuildDeltaIntensityPalette(PositiveBrush, steps, DeltaOpacity);
				negativeDeltaIntensityBrushes = BuildDeltaIntensityPalette(NegativeBrush, steps, DeltaOpacity);
				lastBuiltDeltaIntensitySteps = steps;
				lastBuiltDeltaIntensityMinOpacity = DeltaIntensityMinOpacity;
				lastBuiltDeltaIntensityMaxOpacity = DeltaOpacity;
			}
			else if (!UseDeltaIntensityColoring && (positiveDeltaIntensityBrushes != null || negativeDeltaIntensityBrushes != null))
			{
				if (positiveDeltaIntensityBrushes != null) foreach (var b in positiveDeltaIntensityBrushes) b?.Dispose();
				if (negativeDeltaIntensityBrushes != null) foreach (var b in negativeDeltaIntensityBrushes) b?.Dispose();
				positiveDeltaIntensityBrushes = null; negativeDeltaIntensityBrushes = null;
				lastBuiltDeltaIntensitySteps = -1; lastBuiltDeltaIntensityMinOpacity = -1f; lastBuiltDeltaIntensityMaxOpacity = -1f;
			}
			dxResourceRenderTarget = currentTarget;
		}

		private TextFormat CreateTextFormat(float fontSize)
		{
			if (Core.Globals.DirectWriteFactory == null) return null;
			TextFormat format = null;
			try
			{
				format = new TextFormat(Core.Globals.DirectWriteFactory, GetTextFontFamily(), ResolveTextFontWeight(), FontStyle.Normal, fontSize);
			}
			catch
			{
				try { format = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", ResolveTextFontWeight(), FontStyle.Normal, fontSize); }
				catch { return null; }
			}
			format.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
			format.ParagraphAlignment = ParagraphAlignment.Center;
			format.WordWrapping = WordWrapping.NoWrap;
			return format;
		}

		private string GetTextFontFamily()
		{
			return string.IsNullOrWhiteSpace(TextFontFamily) ? "Segoe UI" : TextFontFamily.Trim();
		}

		private FontWeight ResolveTextFontWeight()
		{
			switch (TextFontWeight)
			{
				case LegProfileTextFontWeight.Thin: return FontWeight.Thin;
				case LegProfileTextFontWeight.ExtraLight: return FontWeight.ExtraLight;
				case LegProfileTextFontWeight.Light: return FontWeight.Light;
				case LegProfileTextFontWeight.Regular: return FontWeight.Normal;
				case LegProfileTextFontWeight.Medium: return FontWeight.Medium;
				case LegProfileTextFontWeight.SemiBold: return FontWeight.SemiBold;
				case LegProfileTextFontWeight.ExtraBold: return FontWeight.ExtraBold;
				case LegProfileTextFontWeight.Black: return FontWeight.Black;
				default: return FontWeight.Bold;
			}
		}

		private string GetTextFormatSignature()
		{
			return GetTextFontFamily() + "|" + TextFontWeight;
		}

		private SolidColorBrush[] BuildGradientPalette(WpfBrush baseBrush, int steps)
		{
			var baseColor = (baseBrush as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			var palette = new SolidColorBrush[steps];
			for (int i = 0; i < steps; i++)
			{
				float t = i / (float)(steps - 1), brightness = MinBrightness + t * (1f - MinBrightness);
				palette[i] = new SolidColorBrush(RenderTarget, new Color4((baseColor.R / 255f) * brightness, (baseColor.G / 255f) * brightness, (baseColor.B / 255f) * brightness, (baseColor.A / 255f) * VolumeOpacity));
			}
			return palette;
		}

		private SolidColorBrush[] BuildDeltaIntensityPalette(WpfBrush baseBrush, int steps, float maxOpacity)
		{
			var baseColor = (baseBrush as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			var palette = new SolidColorBrush[steps];
			float minOpacity = Math.Max(0f, Math.Min(1f, DeltaIntensityMinOpacity));
			for (int i = 0; i < steps; i++)
			{
				float t = i / (float)(steps - 1);
				float opacity = maxOpacity * (minOpacity + t * (1f - minOpacity));
				palette[i] = new SolidColorBrush(RenderTarget, new Color4(baseColor.R / 255f, baseColor.G / 255f, baseColor.B / 255f, (baseColor.A / 255f) * opacity));
			}
			return palette;
		}

		private SolidColorBrush SelectDeltaBrush(long delta, long maxAbsDelta)
		{
			if (!UseDeltaIntensityColoring || maxAbsDelta <= 0)
				return delta >= 0 ? posBrushDx : negBrushDx;

			SolidColorBrush[] palette = delta >= 0 ? positiveDeltaIntensityBrushes : negativeDeltaIntensityBrushes;
			if (palette == null || palette.Length == 0)
				return delta >= 0 ? posBrushDx : negBrushDx;

			double intensity = Math.Abs((double)delta) / Math.Max(1.0, (double)maxAbsDelta);
			int index = (int)Math.Round(intensity * (palette.Length - 1));
			if (index < 0) index = 0;
			if (index >= palette.Length) index = palette.Length - 1;
			return palette[index];
		}

		private float MeasureTextWidth(string text)
		{
			if (textFormat == null) return 0f;
			if (textWidthCache.TryGetValue(text, out float width)) return width;
			using (var l = new TextLayout(Core.Globals.DirectWriteFactory, text, textFormat, 1000, 100))
			{
				width = l.Metrics.Width;
				textWidthCache[text] = width;
				return width;
			}
		}
		private Color4 ToDx(WpfBrush b, float alphaMult) { var c = (b as WpfSolidColorBrush)?.Color ?? WpfColors.White; return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alphaMult); }

		#region Properties
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Active Reversal (ticks)", GroupName="03 Leg Detection", Order=1, Description="Fixed reversal threshold for the active leg tracker. ATR-based reversal replaces this threshold when enabled and available.")] public int ReversalTicks { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Historical Reversal (ticks)", GroupName="03 Leg Detection", Order=2, Description="Fixed reversal threshold for the historical leg tracker. It remains independent of the active-leg threshold.")] public int PastReversalTicks { get; set; }
		[NinjaScriptProperty] [Display(Name="Use ATR Reversal", GroupName="03 Leg Detection", Order=0, Description="Use ATR-based reversal thresholds when ATR data is available, with separate active and historical multipliers.")] public bool UseAtrReversal { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="ATR Period", GroupName="03 Leg Detection", Order=3, Description="ATR lookback used when ATR-based reversal is enabled.")] public int AtrPeriod { get; set; }
		[NinjaScriptProperty] [Range(0.1, double.MaxValue)] [Display(Name="Active ATR Multiplier", GroupName="03 Leg Detection", Order=4, Description="ATR multiplier for active-leg reversal detection.")] public double AtrMultiplier { get; set; }
		[NinjaScriptProperty] [Range(0.1, double.MaxValue)] [Display(Name="Historical ATR Multiplier", GroupName="03 Leg Detection", Order=5, Description="ATR multiplier for historical-leg reversal detection.")] public double PastAtrMultiplier { get; set; }
		[NinjaScriptProperty] [Range(0, int.MaxValue)] [Display(Name="Minimum Leg Size (ticks)", GroupName="03 Leg Detection", Order=6)] public int MinimumLegTicks { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Minimum Bars per Leg", GroupName="03 Leg Detection", Order=7)] public int MinimumBarsPerLeg { get; set; }
		[NinjaScriptProperty] [Range(0, 1440)] [Display(Name="Minimum Leg Duration (minutes)", GroupName="03 Leg Detection", Order=8)] public int MinimumDurationMinutes { get; set; }
		[NinjaScriptProperty] [Range(0, 50)] [Display(Name="Historical Legs to Display", GroupName="01 Display", Order=3, Description="Number of completed historical legs retained for display. Zero hides completed legs while preserving the active profile.")] public int LegsToDisplay { get; set; }
		[NinjaScriptProperty] [Display(Name="Dynamic Order Flow Aggregation", Description="Automatically adjust delta row height with chart zoom. Volume uses its separate fixed row size.", GroupName="04 Rows & Scaling", Order=2)] public bool UseDynamicAggregation { get; set; }
		[NinjaScriptProperty] [Range(0.1, 10.0)] [Display(Name="Order Flow Dynamic Multiplier", Description="Lower values keep delta rows more granular; higher values create taller rows.", GroupName="04 Rows & Scaling", Order=4)] public double DynamicAggregationMultiplier { get; set; }
		[NinjaScriptProperty] [Range(2, 40)] [Display(Name="Order Flow Row Min Pixels", Description="Target minimum delta row height used before applying the aggregation multiplier", GroupName="04 Rows & Scaling", Order=3)] public int DeltaDynamicRowMinPixels { get; set; }
		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Dynamic Order Flow Min Ticks", GroupName="04 Rows & Scaling", Order=5, Description="Minimum ticks per delta row when Dynamic Order Flow Aggregation is on.")] public int DynamicDeltaMinCompression { get; set; }
		[NinjaScriptProperty] [Range(1, 500)] [Display(Name="Dynamic Order Flow Max Ticks", GroupName="04 Rows & Scaling", Order=6, Description="Maximum ticks per delta row when Dynamic Order Flow Aggregation is on.")] public int DynamicDeltaMaxCompression { get; set; }
		[NinjaScriptProperty] [Display(Name="Trade Source Mode", Description="Secondary Tick Series is the legacy path; Tick Replay Last Events reads replayed Last event volume like CVP/Orca Prints.", GroupName="11 Advanced - Data", Order=0)] public LegProfileTradeSourceMode TradeSourceMode { get; set; }
		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Volume Row Size (ticks)", GroupName="04 Rows & Scaling", Order=0, Description="Number of price ticks grouped into each volume row.")] public int VolumeTickCompression { get; set; }
		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Order Flow Row Size (ticks)", GroupName="04 Rows & Scaling", Order=1, Description="Fixed delta row height in ticks when Dynamic Order Flow Aggregation is off.")] public int DeltaTickCompression { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Active Volume Width (px)", GroupName="02 Profile Layout", Order=0)] public int VolumeProfileWidthPx { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Active Delta Width (px)", GroupName="02 Profile Layout", Order=1)] public int DeltaProfileWidthPx { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Historical Volume Width (px)", GroupName="02 Profile Layout", Order=2)] public int PastVolumeWidthPx { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Historical Delta Width (px)", GroupName="02 Profile Layout", Order=3)] public int PastDeltaWidthPx { get; set; }
		[NinjaScriptProperty] [Range(-500, 500)] [Display(Name="Right Offset (px)", GroupName="02 Profile Layout", Order=4)] public int RightOffsetPx { get; set; }
		[NinjaScriptProperty] [Range(0, 500)] [Display(Name="Profile Separation (px)", GroupName="02 Profile Layout", Order=5)] public int ProfileSeparationPx { get; set; }
		[NinjaScriptProperty] [Range(0, 10)] [Display(Name="Profile Row Spacing (px)", GroupName="02 Profile Layout", Order=6, Description="Gap in pixels between horizontal price rows within the profiles.")] public int ProfileBarSpacingPx { get; set; }
		[NinjaScriptProperty] [Display(Name="Mirror Historical Profiles", Description="Flip historical profiles so their spines are on the right. Active orientation follows the Show Delta setting automatically.", GroupName="02 Profile Layout", Order=7)] public bool MirrorProfile { get; set; }
		[Display(Name="Draw Behind Candles", Description="Attempts to place the profile behind chart bars using NinjaTrader z-order.", GroupName="02 Profile Layout", Order=8)] public bool DrawBehindCandles { get; set; }
		[NinjaScriptProperty] [Display(Name="Enable Active Delta Reset", Description="Enables chart-focused hotkeys for a live, non-destructive reset of the active leg delta profile.", GroupName="10 Active Delta Reset", Order=0)] public bool EnableActiveDeltaReset { get; set; }
		[NinjaScriptProperty] [Display(Name="Reset Display Mode", Description="Reset Only hides full-leg delta; Overlay fades it behind reset delta; Side-by-Side adds reset delta immediately to the left.", GroupName="10 Active Delta Reset", Order=1)] public LegProfileResetDisplayMode ResetDisplayMode { get; set; }
		[NinjaScriptProperty] [Display(Name="Reset Now Hotkey", Description="Exact chart-focused key chord, for example Ctrl+Alt+R.", GroupName="10 Active Delta Reset", Order=7)] public string ResetNowHotkey { get; set; }
		[NinjaScriptProperty] [Display(Name="Clear Reset Hotkey", Description="Exact chart-focused key chord, for example Ctrl+Alt+C.", GroupName="10 Active Delta Reset", Order=8)] public string ClearResetHotkey { get; set; }
		[NinjaScriptProperty] [Range(0.05, 1.0)] [Display(Name="Full-Leg Overlay Opacity", GroupName="10 Active Delta Reset", Order=2)] public float FullLegOverlayOpacity { get; set; }
		[NinjaScriptProperty] [Range(0, 100)] [Display(Name="Side-by-Side Gap (px)", GroupName="10 Active Delta Reset", Order=3)] public int ResetSideBySideGapPx { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Reset Status", Description="Shows the active reset anchor time and short hotkey-action messages.", GroupName="10 Active Delta Reset", Order=9)] public bool ShowResetStatus { get; set; }
		[NinjaScriptProperty] [Display(Name="Reset Aggregation Mode", Description="Follow Full Leg uses the rendered full-leg aggregation; Fixed Ticks uses the fixed reset value; Full-Leg Ratio multiplies the rendered full-leg aggregation by the configured ratio and rounds up.", GroupName="10 Active Delta Reset", Order=4)] public LegProfileResetAggregationMode ResetAggregationMode { get; set; }
		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Reset Delta Row Size (ticks)", Description="Used only when Reset Aggregation Mode is Fixed Ticks.", GroupName="10 Active Delta Reset", Order=5)] public int ResetDeltaAggregationTicks { get; set; }
		[NinjaScriptProperty] [Range(0.05, 1.00)] [Display(Name="Reset Aggregation Ratio", Description="Used only in Full-Leg Ratio mode. Reset ticks equal the rendered full-leg ticks multiplied by this ratio, rounded up.", GroupName="10 Active Delta Reset", Order=6)] public double ResetAggregationRatio { get; set; }
		[NinjaScriptProperty]
		[Browsable(false)]
		public bool UseSeparateResetAggregation
		{
			get { return ResetAggregationMode != LegProfileResetAggregationMode.FollowFullLeg; }
			set
			{
				if (!value)
					ResetAggregationMode = LegProfileResetAggregationMode.FollowFullLeg;
				else if (ResetAggregationMode == LegProfileResetAggregationMode.FollowFullLeg)
					ResetAggregationMode = LegProfileResetAggregationMode.FixedTicks;
			}
		}
		[NinjaScriptProperty] [Display(Name="Show Full-Leg Total Delta", GroupName="09 Profile Statistics", Order=0)] public bool ShowFullLegTotalDelta { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Full-Leg Finish Delta", Description="Current delta minus its same-sign cumulative extreme.", GroupName="09 Profile Statistics", Order=1)] public bool ShowFullLegFinishDelta { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Full-Leg Delta Percent", GroupName="09 Profile Statistics", Order=2)] public bool ShowFullLegDeltaPercent { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Reset Total Delta", GroupName="09 Profile Statistics", Order=3)] public bool ShowResetTotalDelta { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Reset Finish Delta", Description="Reset delta minus its same-sign post-reset cumulative extreme.", GroupName="09 Profile Statistics", Order=4)] public bool ShowResetFinishDelta { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Reset Delta Percent", GroupName="09 Profile Statistics", Order=5)] public bool ShowResetDeltaPercent { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Volume", GroupName="01 Display", Order=0)] public bool ShowVolume { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Delta", GroupName="01 Display", Order=1, Description="Show delta profiles. Historical delta also requires Show Historical Delta; active orientation continues to follow this setting.")] public bool ShowDelta { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Historical Delta", GroupName="01 Display", Order=2, Description="Show completed-leg delta profiles when Show Delta is enabled.")] public bool ShowPastDelta { get; set; }
		[NinjaScriptProperty] [Browsable(false)] [Display(Name="Show Current Leg Box", GroupName="01 Display", Order=4)] public bool ShowCurrentLegBox { get; set; }
		[NinjaScriptProperty] [Range(5, 50)] [Display(Name="Delta Text Font Size", GroupName="08 Text - Delta", Order=0, Description="Size of the delta-row labels. Full-leg and reset statistics use Statistics Font Size.")] public int DeltaLabelFontSize { get; set; }
		[NinjaScriptProperty] [TypeConverter(typeof(LegProfileTextFontFamilyConverter))] [Display(Name="Text Font Family", Description="Selects the font family used for Leg-to-Leg Profile labels and annotations.", GroupName="07 Text - General", Order=0)] public string TextFontFamily { get; set; }
		[NinjaScriptProperty] [Display(Name="Text Font Weight", Description="Selects the text weight used for Leg-to-Leg Profile labels and annotations.", GroupName="07 Text - General", Order=1)] public LegProfileTextFontWeight TextFontWeight { get; set; }
		[NinjaScriptProperty] [Range(8, 30)] [Display(Name="Statistics Font Size", Description="Controls only the full-leg and reset statistics text size.", GroupName="09 Profile Statistics", Order=6)] public int StatisticsFontSize { get; set; }
		[XmlIgnore] [Display(Name="Statistics Text Color", Description="Controls only the full-leg, reset, and reset-status text color.", GroupName="09 Profile Statistics", Order=7)] public WpfBrush StatisticsTextBrush { get; set; }
		[Browsable(false)] public string StatisticsTextBrushSerialize { get { return Serialize.BrushToString(StatisticsTextBrush); } set { StatisticsTextBrush = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty] [Display(Name="Show Label Background", GroupName="08 Text - Delta", Order=3)] public bool ShowDeltaLabelBackground { get; set; }
		[NinjaScriptProperty] [Display(Name="Show POC", GroupName="06 POC & Value Area", Order=0, Description="Highlight the highest-volume price row in each volume profile.")] public bool ShowPOC { get; set; }
		[NinjaScriptProperty] [Display(Name="Use Gradient", GroupName="05 Profile Colors", Order=2)] public bool UseGradient { get; set; }
		[NinjaScriptProperty] [Range(2, 64)] [Display(Name="Gradient Steps", GroupName="05 Profile Colors", Order=4)] public int GradientSteps { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Value Area", GroupName="06 POC & Value Area", Order=2)] public bool ShowValueArea { get; set; }
		[NinjaScriptProperty] [Display(Name="Shade Value Area", GroupName="06 POC & Value Area", Order=4, Description="Use Value Area Color for volume rows inside the value area.")] public bool ShowVAColor { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Value Area Boundaries", GroupName="06 POC & Value Area", Order=6, Description="Draw the upper and lower value-area boundaries using the selected line color, width and style.")] public bool ShowVALines { get; set; }
		[NinjaScriptProperty] [Range(50, 95)] [Display(Name="Value Area (%)", GroupName="06 POC & Value Area", Order=3, Description="Percentage of each leg profile volume included in its value area.")] public int ValueAreaPercent { get; set; }
		[NinjaScriptProperty] [Range(0.5, 6.0)] [Display(Name="Boundary Line Width (px)", GroupName="06 POC & Value Area", Order=8)] public float VALineThickness { get; set; }
		[NinjaScriptProperty] [Display(Name="Boundary Line Style", GroupName="06 POC & Value Area", Order=9)] public VALineStyleEnum VALineStyle { get; set; }
		[NinjaScriptProperty] [Range(0.05, 1.0)] [Display(Name="Minimum Profile Brightness", GroupName="05 Profile Colors", Order=3, Description="Minimum brightness for lower-volume rows when the gradient is enabled.")] public float MinBrightness { get; set; }
		[NinjaScriptProperty] [Range(0.1, 1.0)] [Display(Name="Volume Opacity", GroupName="05 Profile Colors", Order=1)] public float VolumeOpacity { get; set; }
		[NinjaScriptProperty] [Range(0.1, 1.0)] [Display(Name="Delta Opacity", GroupName="05 Profile Colors", Order=7)] public float DeltaOpacity { get; set; }
		[NinjaScriptProperty] [Display(Name="Use Delta Intensity Color", GroupName="05 Profile Colors", Order=8)] public bool UseDeltaIntensityColoring { get; set; }
		[NinjaScriptProperty] [Range(0.0, 1.0)] [Display(Name="Delta Intensity Min Opacity", GroupName="05 Profile Colors", Order=9)] public float DeltaIntensityMinOpacity { get; set; }
		[XmlIgnore] [Display(Name="Positive Delta", GroupName="05 Profile Colors", Order=5)] public WpfBrush PositiveBrush { get; set; }
		[Browsable(false)] public string PositiveBrushSerialize { get { return Serialize.BrushToString(PositiveBrush); } set { PositiveBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Negative Delta", GroupName="05 Profile Colors", Order=6)] public WpfBrush NegativeBrush { get; set; }
		[Browsable(false)] public string NegativeBrushSerialize { get { return Serialize.BrushToString(NegativeBrush); } set { NegativeBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Volume Color", GroupName="05 Profile Colors", Order=0)] public WpfBrush VolumeBrush { get; set; }
		[Browsable(false)] public string VolumeBrushSerialize { get { return Serialize.BrushToString(VolumeBrush); } set { VolumeBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Positive Text Color", GroupName="08 Text - Delta", Order=1)] public WpfBrush TextBrush { get; set; }
		[Browsable(false)] public string TextBrushSerialize { get { return Serialize.BrushToString(TextBrush); } set { TextBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Negative Text Color", GroupName="08 Text - Delta", Order=2)] public WpfBrush NegativeTextBrush { get; set; }
		[Browsable(false)] public string NegativeTextBrushSerialize { get { return Serialize.BrushToString(NegativeTextBrush); } set { NegativeTextBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Label Background Color", GroupName="08 Text - Delta", Order=4)] public WpfBrush LabelBgBrush { get; set; }
		[Browsable(false)] public string LabelBgBrushSerialize { get { return Serialize.BrushToString(LabelBgBrush); } set { LabelBgBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Browsable(false)] [Display(Name="Leg Box Color", GroupName="01 Display", Order=5)] public WpfBrush LegBoxBrush { get; set; }
		[Browsable(false)] public string LegBoxBrushSerialize { get { return Serialize.BrushToString(LegBoxBrush); } set { LegBoxBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="POC Color", GroupName="06 POC & Value Area", Order=1)] public WpfBrush POCBrush { get; set; }
		[Browsable(false)] public string POCBrushSerialize { get { return Serialize.BrushToString(POCBrush); } set { POCBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Value Area Color", GroupName="06 POC & Value Area", Order=5)] public WpfBrush VABrush { get; set; }
		[Browsable(false)] public string VABrushSerialize { get { return Serialize.BrushToString(VABrush); } set { VABrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Boundary Line Color", GroupName="06 POC & Value Area", Order=7)] public WpfBrush VALineBrush { get; set; }
		[Browsable(false)] public string VALineBrushSerialize { get { return Serialize.BrushToString(VALineBrush); } set { VALineBrush = Serialize.StringToBrush(value); } }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaLegtoLegProfile[] cacheOrcaLegtoLegProfile;
		public OrcaLegtoLegProfile OrcaLegtoLegProfile(int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, LegProfileTradeSourceMode tradeSourceMode, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool enableActiveDeltaReset, LegProfileResetDisplayMode resetDisplayMode, string resetNowHotkey, string clearResetHotkey, float fullLegOverlayOpacity, int resetSideBySideGapPx, bool showResetStatus, LegProfileResetAggregationMode resetAggregationMode, int resetDeltaAggregationTicks, double resetAggregationRatio, bool useSeparateResetAggregation, bool showFullLegTotalDelta, bool showFullLegFinishDelta, bool showFullLegDeltaPercent, bool showResetTotalDelta, bool showResetFinishDelta, bool showResetDeltaPercent, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, string textFontFamily, LegProfileTextFontWeight textFontWeight, int statisticsFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return OrcaLegtoLegProfile(Input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, tradeSourceMode, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, enableActiveDeltaReset, resetDisplayMode, resetNowHotkey, clearResetHotkey, fullLegOverlayOpacity, resetSideBySideGapPx, showResetStatus, resetAggregationMode, resetDeltaAggregationTicks, resetAggregationRatio, useSeparateResetAggregation, showFullLegTotalDelta, showFullLegFinishDelta, showFullLegDeltaPercent, showResetTotalDelta, showResetFinishDelta, showResetDeltaPercent, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, textFontFamily, textFontWeight, statisticsFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}

		public OrcaLegtoLegProfile OrcaLegtoLegProfile(ISeries<double> input, int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, LegProfileTradeSourceMode tradeSourceMode, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool enableActiveDeltaReset, LegProfileResetDisplayMode resetDisplayMode, string resetNowHotkey, string clearResetHotkey, float fullLegOverlayOpacity, int resetSideBySideGapPx, bool showResetStatus, LegProfileResetAggregationMode resetAggregationMode, int resetDeltaAggregationTicks, double resetAggregationRatio, bool useSeparateResetAggregation, bool showFullLegTotalDelta, bool showFullLegFinishDelta, bool showFullLegDeltaPercent, bool showResetTotalDelta, bool showResetFinishDelta, bool showResetDeltaPercent, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, string textFontFamily, LegProfileTextFontWeight textFontWeight, int statisticsFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			if (cacheOrcaLegtoLegProfile != null)
				for (int idx = 0; idx < cacheOrcaLegtoLegProfile.Length; idx++)
					if (cacheOrcaLegtoLegProfile[idx] != null && cacheOrcaLegtoLegProfile[idx].ReversalTicks == reversalTicks && cacheOrcaLegtoLegProfile[idx].PastReversalTicks == pastReversalTicks && cacheOrcaLegtoLegProfile[idx].UseAtrReversal == useAtrReversal && cacheOrcaLegtoLegProfile[idx].AtrPeriod == atrPeriod && cacheOrcaLegtoLegProfile[idx].AtrMultiplier == atrMultiplier && cacheOrcaLegtoLegProfile[idx].PastAtrMultiplier == pastAtrMultiplier && cacheOrcaLegtoLegProfile[idx].MinimumLegTicks == minimumLegTicks && cacheOrcaLegtoLegProfile[idx].MinimumBarsPerLeg == minimumBarsPerLeg && cacheOrcaLegtoLegProfile[idx].MinimumDurationMinutes == minimumDurationMinutes && cacheOrcaLegtoLegProfile[idx].LegsToDisplay == legsToDisplay && cacheOrcaLegtoLegProfile[idx].UseDynamicAggregation == useDynamicAggregation && cacheOrcaLegtoLegProfile[idx].DynamicAggregationMultiplier == dynamicAggregationMultiplier && cacheOrcaLegtoLegProfile[idx].DeltaDynamicRowMinPixels == deltaDynamicRowMinPixels && cacheOrcaLegtoLegProfile[idx].DynamicDeltaMinCompression == dynamicDeltaMinCompression && cacheOrcaLegtoLegProfile[idx].DynamicDeltaMaxCompression == dynamicDeltaMaxCompression && cacheOrcaLegtoLegProfile[idx].TradeSourceMode == tradeSourceMode && cacheOrcaLegtoLegProfile[idx].VolumeTickCompression == volumeTickCompression && cacheOrcaLegtoLegProfile[idx].DeltaTickCompression == deltaTickCompression && cacheOrcaLegtoLegProfile[idx].VolumeProfileWidthPx == volumeProfileWidthPx && cacheOrcaLegtoLegProfile[idx].DeltaProfileWidthPx == deltaProfileWidthPx && cacheOrcaLegtoLegProfile[idx].PastVolumeWidthPx == pastVolumeWidthPx && cacheOrcaLegtoLegProfile[idx].PastDeltaWidthPx == pastDeltaWidthPx && cacheOrcaLegtoLegProfile[idx].RightOffsetPx == rightOffsetPx && cacheOrcaLegtoLegProfile[idx].ProfileSeparationPx == profileSeparationPx && cacheOrcaLegtoLegProfile[idx].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaLegtoLegProfile[idx].MirrorProfile == mirrorProfile && cacheOrcaLegtoLegProfile[idx].EnableActiveDeltaReset == enableActiveDeltaReset && cacheOrcaLegtoLegProfile[idx].ResetDisplayMode == resetDisplayMode && cacheOrcaLegtoLegProfile[idx].ResetNowHotkey == resetNowHotkey && cacheOrcaLegtoLegProfile[idx].ClearResetHotkey == clearResetHotkey && cacheOrcaLegtoLegProfile[idx].FullLegOverlayOpacity == fullLegOverlayOpacity && cacheOrcaLegtoLegProfile[idx].ResetSideBySideGapPx == resetSideBySideGapPx && cacheOrcaLegtoLegProfile[idx].ShowResetStatus == showResetStatus && cacheOrcaLegtoLegProfile[idx].ResetAggregationMode == resetAggregationMode && cacheOrcaLegtoLegProfile[idx].ResetDeltaAggregationTicks == resetDeltaAggregationTicks && cacheOrcaLegtoLegProfile[idx].ResetAggregationRatio == resetAggregationRatio && cacheOrcaLegtoLegProfile[idx].UseSeparateResetAggregation == useSeparateResetAggregation && cacheOrcaLegtoLegProfile[idx].ShowFullLegTotalDelta == showFullLegTotalDelta && cacheOrcaLegtoLegProfile[idx].ShowFullLegFinishDelta == showFullLegFinishDelta && cacheOrcaLegtoLegProfile[idx].ShowFullLegDeltaPercent == showFullLegDeltaPercent && cacheOrcaLegtoLegProfile[idx].ShowResetTotalDelta == showResetTotalDelta && cacheOrcaLegtoLegProfile[idx].ShowResetFinishDelta == showResetFinishDelta && cacheOrcaLegtoLegProfile[idx].ShowResetDeltaPercent == showResetDeltaPercent && cacheOrcaLegtoLegProfile[idx].ShowVolume == showVolume && cacheOrcaLegtoLegProfile[idx].ShowDelta == showDelta && cacheOrcaLegtoLegProfile[idx].ShowPastDelta == showPastDelta && cacheOrcaLegtoLegProfile[idx].ShowCurrentLegBox == showCurrentLegBox && cacheOrcaLegtoLegProfile[idx].DeltaLabelFontSize == deltaLabelFontSize && cacheOrcaLegtoLegProfile[idx].TextFontFamily == textFontFamily && cacheOrcaLegtoLegProfile[idx].TextFontWeight == textFontWeight && cacheOrcaLegtoLegProfile[idx].StatisticsFontSize == statisticsFontSize && cacheOrcaLegtoLegProfile[idx].ShowDeltaLabelBackground == showDeltaLabelBackground && cacheOrcaLegtoLegProfile[idx].ShowPOC == showPOC && cacheOrcaLegtoLegProfile[idx].UseGradient == useGradient && cacheOrcaLegtoLegProfile[idx].GradientSteps == gradientSteps && cacheOrcaLegtoLegProfile[idx].ShowValueArea == showValueArea && cacheOrcaLegtoLegProfile[idx].ShowVAColor == showVAColor && cacheOrcaLegtoLegProfile[idx].ShowVALines == showVALines && cacheOrcaLegtoLegProfile[idx].ValueAreaPercent == valueAreaPercent && cacheOrcaLegtoLegProfile[idx].VALineThickness == vALineThickness && cacheOrcaLegtoLegProfile[idx].VALineStyle == vALineStyle && cacheOrcaLegtoLegProfile[idx].MinBrightness == minBrightness && cacheOrcaLegtoLegProfile[idx].VolumeOpacity == volumeOpacity && cacheOrcaLegtoLegProfile[idx].DeltaOpacity == deltaOpacity && cacheOrcaLegtoLegProfile[idx].UseDeltaIntensityColoring == useDeltaIntensityColoring && cacheOrcaLegtoLegProfile[idx].DeltaIntensityMinOpacity == deltaIntensityMinOpacity && cacheOrcaLegtoLegProfile[idx].EqualsInput(input))
						return cacheOrcaLegtoLegProfile[idx];
			return CacheIndicator<OrcaLegtoLegProfile>(new OrcaLegtoLegProfile(){ ReversalTicks = reversalTicks, PastReversalTicks = pastReversalTicks, UseAtrReversal = useAtrReversal, AtrPeriod = atrPeriod, AtrMultiplier = atrMultiplier, PastAtrMultiplier = pastAtrMultiplier, MinimumLegTicks = minimumLegTicks, MinimumBarsPerLeg = minimumBarsPerLeg, MinimumDurationMinutes = minimumDurationMinutes, LegsToDisplay = legsToDisplay, UseDynamicAggregation = useDynamicAggregation, DynamicAggregationMultiplier = dynamicAggregationMultiplier, DeltaDynamicRowMinPixels = deltaDynamicRowMinPixels, DynamicDeltaMinCompression = dynamicDeltaMinCompression, DynamicDeltaMaxCompression = dynamicDeltaMaxCompression, TradeSourceMode = tradeSourceMode, VolumeTickCompression = volumeTickCompression, DeltaTickCompression = deltaTickCompression, VolumeProfileWidthPx = volumeProfileWidthPx, DeltaProfileWidthPx = deltaProfileWidthPx, PastVolumeWidthPx = pastVolumeWidthPx, PastDeltaWidthPx = pastDeltaWidthPx, RightOffsetPx = rightOffsetPx, ProfileSeparationPx = profileSeparationPx, ProfileBarSpacingPx = profileBarSpacingPx, MirrorProfile = mirrorProfile, EnableActiveDeltaReset = enableActiveDeltaReset, ResetDisplayMode = resetDisplayMode, ResetNowHotkey = resetNowHotkey, ClearResetHotkey = clearResetHotkey, FullLegOverlayOpacity = fullLegOverlayOpacity, ResetSideBySideGapPx = resetSideBySideGapPx, ShowResetStatus = showResetStatus, ResetAggregationMode = resetAggregationMode, ResetDeltaAggregationTicks = resetDeltaAggregationTicks, ResetAggregationRatio = resetAggregationRatio, UseSeparateResetAggregation = useSeparateResetAggregation, ShowFullLegTotalDelta = showFullLegTotalDelta, ShowFullLegFinishDelta = showFullLegFinishDelta, ShowFullLegDeltaPercent = showFullLegDeltaPercent, ShowResetTotalDelta = showResetTotalDelta, ShowResetFinishDelta = showResetFinishDelta, ShowResetDeltaPercent = showResetDeltaPercent, ShowVolume = showVolume, ShowDelta = showDelta, ShowPastDelta = showPastDelta, ShowCurrentLegBox = showCurrentLegBox, DeltaLabelFontSize = deltaLabelFontSize, TextFontFamily = textFontFamily, TextFontWeight = textFontWeight, StatisticsFontSize = statisticsFontSize, ShowDeltaLabelBackground = showDeltaLabelBackground, ShowPOC = showPOC, UseGradient = useGradient, GradientSteps = gradientSteps, ShowValueArea = showValueArea, ShowVAColor = showVAColor, ShowVALines = showVALines, ValueAreaPercent = valueAreaPercent, VALineThickness = vALineThickness, VALineStyle = vALineStyle, MinBrightness = minBrightness, VolumeOpacity = volumeOpacity, DeltaOpacity = deltaOpacity, UseDeltaIntensityColoring = useDeltaIntensityColoring, DeltaIntensityMinOpacity = deltaIntensityMinOpacity }, input, ref cacheOrcaLegtoLegProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaLegtoLegProfile OrcaLegtoLegProfile(int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, LegProfileTradeSourceMode tradeSourceMode, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool enableActiveDeltaReset, LegProfileResetDisplayMode resetDisplayMode, string resetNowHotkey, string clearResetHotkey, float fullLegOverlayOpacity, int resetSideBySideGapPx, bool showResetStatus, LegProfileResetAggregationMode resetAggregationMode, int resetDeltaAggregationTicks, double resetAggregationRatio, bool useSeparateResetAggregation, bool showFullLegTotalDelta, bool showFullLegFinishDelta, bool showFullLegDeltaPercent, bool showResetTotalDelta, bool showResetFinishDelta, bool showResetDeltaPercent, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, string textFontFamily, LegProfileTextFontWeight textFontWeight, int statisticsFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return indicator.OrcaLegtoLegProfile(Input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, tradeSourceMode, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, enableActiveDeltaReset, resetDisplayMode, resetNowHotkey, clearResetHotkey, fullLegOverlayOpacity, resetSideBySideGapPx, showResetStatus, resetAggregationMode, resetDeltaAggregationTicks, resetAggregationRatio, useSeparateResetAggregation, showFullLegTotalDelta, showFullLegFinishDelta, showFullLegDeltaPercent, showResetTotalDelta, showResetFinishDelta, showResetDeltaPercent, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, textFontFamily, textFontWeight, statisticsFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}

		public Indicators.OrcaLegtoLegProfile OrcaLegtoLegProfile(ISeries<double> input , int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, LegProfileTradeSourceMode tradeSourceMode, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool enableActiveDeltaReset, LegProfileResetDisplayMode resetDisplayMode, string resetNowHotkey, string clearResetHotkey, float fullLegOverlayOpacity, int resetSideBySideGapPx, bool showResetStatus, LegProfileResetAggregationMode resetAggregationMode, int resetDeltaAggregationTicks, double resetAggregationRatio, bool useSeparateResetAggregation, bool showFullLegTotalDelta, bool showFullLegFinishDelta, bool showFullLegDeltaPercent, bool showResetTotalDelta, bool showResetFinishDelta, bool showResetDeltaPercent, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, string textFontFamily, LegProfileTextFontWeight textFontWeight, int statisticsFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return indicator.OrcaLegtoLegProfile(input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, tradeSourceMode, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, enableActiveDeltaReset, resetDisplayMode, resetNowHotkey, clearResetHotkey, fullLegOverlayOpacity, resetSideBySideGapPx, showResetStatus, resetAggregationMode, resetDeltaAggregationTicks, resetAggregationRatio, useSeparateResetAggregation, showFullLegTotalDelta, showFullLegFinishDelta, showFullLegDeltaPercent, showResetTotalDelta, showResetFinishDelta, showResetDeltaPercent, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, textFontFamily, textFontWeight, statisticsFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaLegtoLegProfile OrcaLegtoLegProfile(int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, LegProfileTradeSourceMode tradeSourceMode, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool enableActiveDeltaReset, LegProfileResetDisplayMode resetDisplayMode, string resetNowHotkey, string clearResetHotkey, float fullLegOverlayOpacity, int resetSideBySideGapPx, bool showResetStatus, LegProfileResetAggregationMode resetAggregationMode, int resetDeltaAggregationTicks, double resetAggregationRatio, bool useSeparateResetAggregation, bool showFullLegTotalDelta, bool showFullLegFinishDelta, bool showFullLegDeltaPercent, bool showResetTotalDelta, bool showResetFinishDelta, bool showResetDeltaPercent, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, string textFontFamily, LegProfileTextFontWeight textFontWeight, int statisticsFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return indicator.OrcaLegtoLegProfile(Input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, tradeSourceMode, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, enableActiveDeltaReset, resetDisplayMode, resetNowHotkey, clearResetHotkey, fullLegOverlayOpacity, resetSideBySideGapPx, showResetStatus, resetAggregationMode, resetDeltaAggregationTicks, resetAggregationRatio, useSeparateResetAggregation, showFullLegTotalDelta, showFullLegFinishDelta, showFullLegDeltaPercent, showResetTotalDelta, showResetFinishDelta, showResetDeltaPercent, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, textFontFamily, textFontWeight, statisticsFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}

		public Indicators.OrcaLegtoLegProfile OrcaLegtoLegProfile(ISeries<double> input , int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int deltaDynamicRowMinPixels, int dynamicDeltaMinCompression, int dynamicDeltaMaxCompression, LegProfileTradeSourceMode tradeSourceMode, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool enableActiveDeltaReset, LegProfileResetDisplayMode resetDisplayMode, string resetNowHotkey, string clearResetHotkey, float fullLegOverlayOpacity, int resetSideBySideGapPx, bool showResetStatus, LegProfileResetAggregationMode resetAggregationMode, int resetDeltaAggregationTicks, double resetAggregationRatio, bool useSeparateResetAggregation, bool showFullLegTotalDelta, bool showFullLegFinishDelta, bool showFullLegDeltaPercent, bool showResetTotalDelta, bool showResetFinishDelta, bool showResetDeltaPercent, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, string textFontFamily, LegProfileTextFontWeight textFontWeight, int statisticsFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity, bool useDeltaIntensityColoring, float deltaIntensityMinOpacity)
		{
			return indicator.OrcaLegtoLegProfile(input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, deltaDynamicRowMinPixels, dynamicDeltaMinCompression, dynamicDeltaMaxCompression, tradeSourceMode, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, enableActiveDeltaReset, resetDisplayMode, resetNowHotkey, clearResetHotkey, fullLegOverlayOpacity, resetSideBySideGapPx, showResetStatus, resetAggregationMode, resetDeltaAggregationTicks, resetAggregationRatio, useSeparateResetAggregation, showFullLegTotalDelta, showFullLegFinishDelta, showFullLegDeltaPercent, showResetTotalDelta, showResetFinishDelta, showResetDeltaPercent, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, textFontFamily, textFontWeight, statisticsFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity, useDeltaIntensityColoring, deltaIntensityMinOpacity);
		}
	}
}

#endregion
