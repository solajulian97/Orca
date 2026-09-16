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

using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using DxSolidBrush = SharpDX.Direct2D1.SolidColorBrush;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum OrcaOrDisplayStyle { Lines, Shaded }
	public enum OrcaOrTimeZoneMode { NewYork, ChartTime }
	public enum OrcaOrExtensionStep { Auto, ES15, NQ65, Custom }
	public enum OrcaOrExpansionTrigger { BothSides, EachSideIndividually }
	public enum OrcaOrLineStyle { Solid, Dashed, Dotted }
	public enum OrcaOrBoxEnd { UntilNextOpeningRange, UntilDayEnd }
}

namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Orca Opening Ranges: 30-second opening ranges for the CME session opens
	/// (RTH, PM, Globex, Tokyo, Midnight, London, Gold, CL) with progressive RTH extensions.
	/// Port of Julian's TradingView "30s OR" study.
	/// </summary>
	public class OrcaOpeningRanges : Indicator
	{
		#region Helper Classes
		private class OrRange
		{
			// Chart-time-zone instant of the 30s OR bar open; used for X placement.
			public DateTime AnchorTime;
			// New York date + session open; identifies one range per session per day.
			public DateTime NyOpen;
			public double High = double.NaN;
			public double Low = double.NaN;
			public int ExtUp;
			public int ExtDn;
			public double Mid => (!double.IsNaN(High) && !double.IsNaN(Low)) ? (High + Low) / 2.0 : double.NaN;
			public bool IsValid => !double.IsNaN(High) && !double.IsNaN(Low);
		}

		private class OrSession
		{
			public string Label;
			public TimeSpan OpenTime;
			public int HlBrush;
			public int MidBrush;
			public List<OrRange> Ranges = new List<OrRange>(64);
			public OrRange Latest => Ranges.Count > 0 ? Ranges[Ranges.Count - 1] : null;
		}
		#endregion

		#region Fields
		private const int SessionRth = 0;
		private const int SessionPm = 1;
		private const int SessionGlobex = 2;
		private const int SessionTokyo = 3;
		private const int SessionMidnight = 4;
		private const int SessionLondon = 5;
		private const int SessionGold = 6;
		private const int SessionCl = 7;
		private const int SessionCount = 8;

		private const int BrushExtUp = SessionCount * 2;
		private const int BrushExtDn = SessionCount * 2 + 1;
		private const int BrushCount = SessionCount * 2 + 2;

		private const int MaxRangesPerSession = 400;
		// Extensions are unbounded by design (touch level N -> reveal N+1 forever). This is only a
		// safety guard against a degenerate step (e.g. tiny Custom step on a huge move); rendering is
		// done in OnRender, so NinjaTrader draw-object limits do not apply.
		private const int MaxSupportedExtensionLevels = 250;
		private const int SecondarySeriesIndex = 1;
		private static readonly TimeSpan OrWindow = TimeSpan.FromSeconds(30);

		// Price-scale markers are NinjaTrader plot price markers. Plot lines are never drawn (base.OnRender is
		// skipped); the plots exist only so NT paints native tags on the right price scale.
		private const int PlotsPerSession = 3; // High, Low, Mid
		private const int MaxScaleMarkerLevels = 10; // extension levels per side that get a scale tag
		private const int ExtUpPlotBase = SessionCount * PlotsPerSession;
		private const int ExtDnPlotBase = ExtUpPlotBase + MaxScaleMarkerLevels;
		private const int PlotCount = ExtDnPlotBase + MaxScaleMarkerLevels;

		private OrSession[] sessions;
		private TimeZoneInfo easternTimeZone;
		private TimeZoneInfo chartTimeZone;
		private double resolvedExtensionStep;
		private DateTime lastPrimaryTime = DateTime.MinValue;

		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private DxSolidBrush[] dxBrushes;
		private SharpDX.Direct2D1.StrokeStyle[] dxStrokes;
		private SharpDX.DirectWrite.TextFormat dxLabelFormat;
		private bool dxValid;
		#endregion

		#region State
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca Opening Ranges";
				Description = "30-second opening ranges for RTH, PM, Globex, Tokyo, Midnight, London, Gold and CL opens (New York time) with progressive RTH extensions.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;
				// Julian's rule: ranges/extensions never pull the chart scale. Levels off-screen stay off-screen.
				IsAutoScale = false;
				// Native right-scale price tags (plot price markers); the plots themselves are never drawn.
				PaintPriceMarkers = true;

				// Display
				DisplayStyle = OrcaOrDisplayStyle.Shaded;
				FillOpacity = 15;
				ShowMidpoint = false;
				ShowWordLabels = false;
				ShowPriceLabels = true;
				ConnectRanges = false;
				BoxEndMode = OrcaOrBoxEnd.UntilNextOpeningRange;
				DayEndTime = new TimeSpan(17, 0, 0);
				TimeZoneMode = OrcaOrTimeZoneMode.NewYork;
				LineWidth = 1;
				MidLineWidth = 2;
				DrawBehindCandles = false;
				LabelFontSize = 10;
				LabelXOffset = 6;

				// Sessions (defaults follow the TradingView study; times are New York)
				RthOpenTime = new TimeSpan(9, 30, 0);
				RthCloseTime = new TimeSpan(16, 0, 0);
				PmOpenTime = new TimeSpan(13, 30, 0);
				GlobexOpenTime = new TimeSpan(18, 0, 0);
				TokyoOpenTime = new TimeSpan(20, 0, 0);
				MidnightOpenTime = new TimeSpan(0, 0, 0);
				LondonOpenTime = new TimeSpan(3, 0, 0);
				GoldOpenTime = new TimeSpan(8, 20, 0);
				ClOpenTime = new TimeSpan(9, 0, 0);

				RthEnabled = true;      RthColor = WpfBrushes.Aqua;                          RthMidColor = WpfBrushes.Yellow;
				PmEnabled = true;       PmColor = MakeBrush(0x20, 0xB2, 0xAA);               PmMidColor = MakeBrush(0x40, 0xE0, 0xD0);
				GlobexEnabled = true;   GlobexColor = WpfBrushes.Orange;                     GlobexMidColor = WpfBrushes.Fuchsia;
				TokyoEnabled = false;   TokyoColor = MakeBrush(0x7B, 0x68, 0xEE);            TokyoMidColor = MakeBrush(0xDA, 0x70, 0xD6);
				MidnightEnabled = true; MidnightColor = MakeBrush(0xA9, 0xA9, 0xA9);         MidnightMidColor = MakeBrush(0xF5, 0xF5, 0xF5);
				LondonEnabled = true;   LondonColor = WpfBrushes.Olive;                      LondonMidColor = WpfBrushes.Lime;
				GoldEnabled = false;    GoldColor = MakeBrush(0xDA, 0xA5, 0x20);             GoldMidColor = MakeBrush(0xFF, 0xD7, 0x00);
				ClEnabled = false;      ClColor = MakeBrush(0xD2, 0x69, 0x1E);               ClMidColor = MakeBrush(0xF4, 0xA4, 0x60);

				// RTH extensions
				ExtensionsEnabled = true;
				ExtensionStepMode = OrcaOrExtensionStep.Auto;
				CustomExtensionStep = 15.0;
				ExpansionTrigger = OrcaOrExpansionTrigger.BothSides;
				InitialExtensionLevels = 2;
				ExtensionLineStyle = OrcaOrLineStyle.Dashed;
				ExtensionLineWidth = 1;
				ExtensionUpColor = MakeBrush(0x00, 0xE6, 0x76);
				ExtensionDownColor = MakeBrush(0xFF, 0x52, 0x52);

				AddScaleMarkerPlots();
			}
			else if (State == State.Configure)
			{
				// 30-second series drives the opening range capture regardless of the chart's own bar type.
				AddDataSeries(BarsPeriodType.Second, 30);
			}
			else if (State == State.DataLoaded)
			{
				easternTimeZone = FindEasternTimeZone();
				chartTimeZone = FindChartTimeZone();
				BuildSessions();
				resolvedExtensionStep = ResolveExtensionStep();
				ApplyScaleMarkerBrushes();
			}
			else if (State == State.Historical)
			{
				if (DrawBehindCandles && ChartControl != null)
					SetZOrder(-1000);
			}
			else if (State == State.Terminated)
			{
				DisposeDx();
			}
		}

		private static WpfBrush MakeBrush(byte r, byte g, byte b)
		{
			WpfSolidColorBrush brush = new WpfSolidColorBrush(WpfColor.FromRgb(r, g, b));
			brush.Freeze();
			return brush;
		}

		private TimeZoneInfo FindEasternTimeZone()
		{
			try { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
			catch { return TimeZoneInfo.Local; }
		}

		private TimeZoneInfo FindChartTimeZone()
		{
			// NinjaTrader stamps bars in the Tools > Options > General time zone.
			try { return NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo ?? TimeZoneInfo.Local; }
			catch { return TimeZoneInfo.Local; }
		}

		private void BuildSessions()
		{
			sessions = new OrSession[SessionCount];
			sessions[SessionRth]      = new OrSession { Label = "RTH",      OpenTime = NormalizeOpenTime(RthOpenTime),      HlBrush = SessionRth * 2,      MidBrush = SessionRth * 2 + 1 };
			sessions[SessionPm]       = new OrSession { Label = "PM",       OpenTime = NormalizeOpenTime(PmOpenTime),       HlBrush = SessionPm * 2,       MidBrush = SessionPm * 2 + 1 };
			sessions[SessionGlobex]   = new OrSession { Label = "GLOBEX",   OpenTime = NormalizeOpenTime(GlobexOpenTime),   HlBrush = SessionGlobex * 2,   MidBrush = SessionGlobex * 2 + 1 };
			sessions[SessionTokyo]    = new OrSession { Label = "TOKYO",    OpenTime = NormalizeOpenTime(TokyoOpenTime),    HlBrush = SessionTokyo * 2,    MidBrush = SessionTokyo * 2 + 1 };
			sessions[SessionMidnight] = new OrSession { Label = "MIDNIGHT", OpenTime = NormalizeOpenTime(MidnightOpenTime), HlBrush = SessionMidnight * 2, MidBrush = SessionMidnight * 2 + 1 };
			sessions[SessionLondon]   = new OrSession { Label = "LONDON",   OpenTime = NormalizeOpenTime(LondonOpenTime),   HlBrush = SessionLondon * 2,   MidBrush = SessionLondon * 2 + 1 };
			sessions[SessionGold]     = new OrSession { Label = "GOLD",     OpenTime = NormalizeOpenTime(GoldOpenTime),     HlBrush = SessionGold * 2,     MidBrush = SessionGold * 2 + 1 };
			sessions[SessionCl]       = new OrSession { Label = "CL",       OpenTime = NormalizeOpenTime(ClOpenTime),       HlBrush = SessionCl * 2,       MidBrush = SessionCl * 2 + 1 };
		}

		private static readonly string[] SessionPlotNames = { "RTH", "PM", "Globex", "Tokyo", "Midnight", "London", "Gold", "CL" };

		private void AddScaleMarkerPlots()
		{
			for (int i = 0; i < SessionCount; i++)
			{
				AddPlot(new Stroke(WpfBrushes.Transparent, 1), PlotStyle.Line, SessionPlotNames[i] + " OR High");
				AddPlot(new Stroke(WpfBrushes.Transparent, 1), PlotStyle.Line, SessionPlotNames[i] + " OR Low");
				AddPlot(new Stroke(WpfBrushes.Transparent, 1), PlotStyle.Line, SessionPlotNames[i] + " OR Mid");
			}
			for (int k = 1; k <= MaxScaleMarkerLevels; k++)
				AddPlot(new Stroke(WpfBrushes.Transparent, 1), PlotStyle.Line, "RTH Ext +" + k);
			for (int k = 1; k <= MaxScaleMarkerLevels; k++)
				AddPlot(new Stroke(WpfBrushes.Transparent, 1), PlotStyle.Line, "RTH Ext -" + k);
		}

		private void ApplyScaleMarkerBrushes()
		{
			if (Plots == null || Plots.Length < PlotCount)
				return;

			WpfBrush[] hl = { RthColor, PmColor, GlobexColor, TokyoColor, MidnightColor, LondonColor, GoldColor, ClColor };
			WpfBrush[] mid = { RthMidColor, PmMidColor, GlobexMidColor, TokyoMidColor, MidnightMidColor, LondonMidColor, GoldMidColor, ClMidColor };
			for (int i = 0; i < SessionCount; i++)
			{
				Plots[i * PlotsPerSession].Brush = hl[i] ?? WpfBrushes.White;
				Plots[i * PlotsPerSession + 1].Brush = hl[i] ?? WpfBrushes.White;
				Plots[i * PlotsPerSession + 2].Brush = mid[i] ?? WpfBrushes.White;
			}
			for (int k = 0; k < MaxScaleMarkerLevels; k++)
			{
				Plots[ExtUpPlotBase + k].Brush = ExtensionUpColor ?? WpfBrushes.White;
				Plots[ExtDnPlotBase + k].Brush = ExtensionDownColor ?? WpfBrushes.White;
			}
		}

		private static TimeSpan NormalizeOpenTime(TimeSpan value)
		{
			// Keep the open inside a single day and drop sub-second parts so it lines up with 30s bar boundaries.
			long ticks = value.Ticks % TimeSpan.TicksPerDay;
			if (ticks < 0) ticks += TimeSpan.TicksPerDay;
			ticks -= ticks % TimeSpan.TicksPerSecond;
			return TimeSpan.FromTicks(ticks);
		}

		private bool IsSessionEnabled(int idx)
		{
			switch (idx)
			{
				case SessionRth: return RthEnabled;
				case SessionPm: return PmEnabled;
				case SessionGlobex: return GlobexEnabled;
				case SessionTokyo: return TokyoEnabled;
				case SessionMidnight: return MidnightEnabled;
				case SessionLondon: return LondonEnabled;
				case SessionGold: return GoldEnabled;
				case SessionCl: return ClEnabled;
			}
			return false;
		}
		#endregion

		#region Extension Step
		private double ResolveExtensionStep()
		{
			switch (ExtensionStepMode)
			{
				case OrcaOrExtensionStep.ES15: return 15.0;
				case OrcaOrExtensionStep.NQ65: return 65.0;
				case OrcaOrExtensionStep.Custom: return Math.Max(TickSize > 0 ? TickSize : 0.25, CustomExtensionStep);
			}
			return IsNasdaqSymbol() ? 65.0 : 15.0;
		}

		private bool IsNasdaqSymbol()
		{
			// Mirrors the Pine detection: NQ / MNQ / NAS100 / US100 / NDX -> 65 pts, everything else (ES / MES) -> 15 pts.
			string name = Instrument != null && Instrument.MasterInstrument != null ? Instrument.MasterInstrument.Name : string.Empty;
			string full = Instrument != null ? Instrument.FullName : string.Empty;
			string[] keys = { "NQ", "NAS100", "US100", "NDX" };
			foreach (string key in keys)
			{
				if (ContainsIgnoreCase(name, key) || ContainsIgnoreCase(full, key))
					return true;
			}
			return false;
		}

		private static bool ContainsIgnoreCase(string source, string value)
		{
			return !string.IsNullOrEmpty(source) && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private int ClampedInitialLevels => Math.Max(1, Math.Min(InitialExtensionLevels, MaxSupportedExtensionLevels));
		#endregion

		#region Time Helpers
		private DateTime ToNewYork(DateTime chartTime)
		{
			if (TimeZoneMode == OrcaOrTimeZoneMode.ChartTime || easternTimeZone == null || chartTimeZone == null)
				return chartTime;
			if (easternTimeZone.Id == chartTimeZone.Id)
				return chartTime;
			try
			{
				DateTime unspecified = DateTime.SpecifyKind(chartTime, DateTimeKind.Unspecified);
				return TimeZoneInfo.ConvertTime(unspecified, chartTimeZone, easternTimeZone);
			}
			catch
			{
				return chartTime;
			}
		}

		private static DateTime SessionOpenFor(DateTime nyBarOpen, TimeSpan sessionOpen)
		{
			DateTime anchor = nyBarOpen.Date + sessionOpen;
			return nyBarOpen.TimeOfDay < sessionOpen ? anchor.AddDays(-1) : anchor;
		}

		/// <summary>
		/// RTH close for a given RTH range, in chart time. Extensions (lines, unlocks, scale tags) stop here
		/// so they never stretch across Globex.
		/// </summary>
		/// <summary>
		/// End of an OR box in chart time for the UntilDayEnd mode: the first DayEndTime (NY) at or after the
		/// range's open. Globex/Tokyo ranges opened after 17:00 therefore end at the next day's 17:00.
		/// </summary>
		private DateTime GetBoxDayEndChartTime(OrRange range)
		{
			TimeSpan dayEnd = NormalizeOpenTime(DayEndTime);
			DateTime dayEndNy = range.NyOpen.Date + dayEnd;
			if (dayEndNy <= range.NyOpen)
				dayEndNy = dayEndNy.AddDays(1);
			return range.AnchorTime + (dayEndNy - range.NyOpen);
		}

		/// <summary>True while the box is still drawn at barChartTime (always true in UntilNextOpeningRange mode).</summary>
		private bool IsBoxActive(OrRange range, DateTime barChartTime)
		{
			if (BoxEndMode != OrcaOrBoxEnd.UntilDayEnd)
				return true;
			return barChartTime <= GetBoxDayEndChartTime(range);
		}

		private DateTime GetRthCloseChartTime(OrRange rth)
		{
			TimeSpan open = sessions[SessionRth].OpenTime;
			TimeSpan close = NormalizeOpenTime(RthCloseTime);
			TimeSpan duration = close - open;
			if (duration <= TimeSpan.Zero)
				duration += TimeSpan.FromDays(1);
			return rth.AnchorTime + duration;
		}
		#endregion

		#region OnBarUpdate
		protected override void OnBarUpdate()
		{
			if (sessions == null)
				return;

			if (BarsInProgress == SecondarySeriesIndex)
			{
				if (CurrentBars.Length > SecondarySeriesIndex && CurrentBars[SecondarySeriesIndex] >= 0)
					CaptureOpeningRange(Times[SecondarySeriesIndex][0], Highs[SecondarySeriesIndex][0], Lows[SecondarySeriesIndex][0]);
				return;
			}

			if (BarsInProgress != 0 || CurrentBars[0] < 0)
				return;

			lastPrimaryTime = Time[0];
			UpdateExtensionUnlocks(Time[0], High[0], Low[0]);
			UpdateScaleMarkers(Time[0]);
		}

		/// <summary>
		/// Feeds the marker plots so NinjaTrader paints native price tags on the right scale. Values are
		/// written per primary bar, so the tags follow whatever level was active at the visible bar.
		/// </summary>
		private void UpdateScaleMarkers(DateTime barChartTime)
		{
			if (Values == null || Values.Length < PlotCount)
				return;

			for (int i = 0; i < SessionCount; i++)
			{
				int b = i * PlotsPerSession;
				OrRange range = sessions[i].Latest;
				bool show = ShowPriceLabels && IsSessionEnabled(i) && range != null && range.IsValid && IsBoxActive(range, barChartTime);
				if (show)
				{
					Values[b][0] = range.High;
					Values[b + 1][0] = range.Low;
					if (ShowMidpoint) Values[b + 2][0] = range.Mid; else Values[b + 2].Reset();
				}
				else
				{
					Values[b].Reset(); Values[b + 1].Reset(); Values[b + 2].Reset();
				}
			}

			OrRange rth = sessions[SessionRth].Latest;
			double step = resolvedExtensionStep;
			bool extActive = ShowPriceLabels && ExtensionsEnabled && RthEnabled && rth != null && rth.IsValid && step > 0
				&& barChartTime > rth.AnchorTime && barChartTime <= GetRthCloseChartTime(rth);
			for (int k = 0; k < MaxScaleMarkerLevels; k++)
			{
				if (extActive && k < rth.ExtUp) Values[ExtUpPlotBase + k][0] = rth.High + (k + 1) * step; else Values[ExtUpPlotBase + k].Reset();
				if (extActive && k < rth.ExtDn) Values[ExtDnPlotBase + k][0] = rth.Low - (k + 1) * step; else Values[ExtDnPlotBase + k].Reset();
			}
		}

		/// <summary>
		/// The 30s series is close-stamped, so the bar whose open (Time - 30s) lands on a session open
		/// is that session's opening range bar. High/Low are assigned (not accumulated) so the developing
		/// realtime bar and the finished historical bar produce the same result.
		/// </summary>
		private void CaptureOpeningRange(DateTime barCloseChartTime, double high, double low)
		{
			if (double.IsNaN(high) || double.IsNaN(low))
				return;

			DateTime barOpenChartTime = barCloseChartTime - OrWindow;
			DateTime barOpenNy = ToNewYork(barOpenChartTime);

			for (int i = 0; i < SessionCount; i++)
			{
				OrSession session = sessions[i];
				DateTime sessionOpenNy = SessionOpenFor(barOpenNy, session.OpenTime);
				TimeSpan offset = barOpenNy - sessionOpenNy;
				// Tolerate sub-second jitter on the open side; the bar must start inside the first 30s window.
				if (offset < TimeSpan.FromSeconds(-1) || offset >= OrWindow)
					continue;

				OrRange range = session.Latest;
				if (range == null || range.NyOpen != sessionOpenNy)
				{
					range = new OrRange
					{
						AnchorTime = barOpenChartTime,
						NyOpen = sessionOpenNy,
						ExtUp = ClampedInitialLevels,
						ExtDn = ClampedInitialLevels
					};
					session.Ranges.Add(range);
					if (session.Ranges.Count > MaxRangesPerSession)
						session.Ranges.RemoveAt(0);
				}

				range.High = high;
				range.Low = low;
			}
		}

		/// <summary>
		/// Progressive reveal: touching the highest currently revealed level (level N) reveals level N+1,
		/// indefinitely (2 -> 3 -> 4 -> ...). BothSides shares one count across both sides (a touch on
		/// either side reveals the next level on both); EachSideIndividually tracks the sides separately.
		/// </summary>
		private void UpdateExtensionUnlocks(DateTime barChartTime, double high, double low)
		{
			if (!ExtensionsEnabled || !RthEnabled)
				return;

			OrRange rth = sessions[SessionRth].Latest;
			if (rth == null || !rth.IsValid || barChartTime <= rth.AnchorTime)
				return;
			// Touches after the RTH close (Globex) do not unlock levels; the extensions end at the close.
			if (barChartTime > GetRthCloseChartTime(rth))
				return;

			double step = resolvedExtensionStep;
			if (step <= 0)
				return;

			int max = MaxSupportedExtensionLevels;
			if (ExpansionTrigger == OrcaOrExpansionTrigger.BothSides)
			{
				int count = Math.Max(rth.ExtUp, rth.ExtDn);
				while (count < max && (high >= rth.High + count * step || low <= rth.Low - count * step))
					count++;
				rth.ExtUp = count;
				rth.ExtDn = count;
			}
			else
			{
				while (rth.ExtUp < max && high >= rth.High + rth.ExtUp * step)
					rth.ExtUp++;
				while (rth.ExtDn < max && low <= rth.Low - rth.ExtDn * step)
					rth.ExtDn++;
			}
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl cc, ChartScale cs)
		{
			try
			{
				// base.OnRender is intentionally skipped: the plots exist only for native price-scale tags
				// and must not be drawn as lines in the pane.
				if (cc == null || cs == null || ChartBars == null || ChartPanel == null || RenderTarget == null || sessions == null)
					return;
				EnsureDx();
				if (!dxValid || dxBrushes == null || dxStrokes == null)
					return;

				float panelTop = ChartPanel.Y;
				float panelBottom = panelTop + ChartPanel.H;
				float panelLeft = ChartPanel.X;
				float panelRight = ChartPanel.X + ChartPanel.W;
				float labelX = GetLabelAnchorX(cc, panelLeft, panelRight);

				var oldAA = RenderTarget.AntialiasMode;
				RenderTarget.AntialiasMode = AntialiasMode.Aliased;
				try
				{
					for (int i = 0; i < SessionCount; i++)
					{
						if (!IsSessionEnabled(i))
							continue;
						RenderSession(cc, cs, sessions[i], panelLeft, panelRight, panelTop, panelBottom, labelX);
					}

					if (ExtensionsEnabled && RthEnabled)
						RenderExtensions(cc, cs, sessions[SessionRth], panelLeft, panelRight, panelTop, panelBottom, labelX);
				}
				finally
				{
					RenderTarget.AntialiasMode = oldAA;
				}
			}
			catch (Exception ex)
			{
				Print("OrcaOpeningRanges OnRender error: " + ex.Message);
			}
		}

		private void RenderSession(ChartControl cc, ChartScale cs, OrSession session, float panelLeft, float panelRight, float panelTop, float panelBottom, float labelX)
		{
			DxSolidBrush hlBrush = dxBrushes[session.HlBrush];
			DxSolidBrush midBrush = dxBrushes[session.MidBrush];
			if (hlBrush == null)
				return;

			List<OrRange> ranges = session.Ranges;
			int last = ranges.Count - 1;
			OrRange prev = null;

			for (int i = 0; i <= last; i++)
			{
				OrRange range = ranges[i];
				if (!range.IsValid) { prev = null; continue; }

				float startX = cc.GetXByTime(range.AnchorTime);
				float endX = i < last ? cc.GetXByTime(ranges[i + 1].AnchorTime) : panelRight;
				bool boxClosed = false;
				if (BoxEndMode == OrcaOrBoxEnd.UntilDayEnd)
				{
					DateTime dayEnd = GetBoxDayEndChartTime(range);
					boxClosed = lastPrimaryTime != DateTime.MinValue && dayEnd <= lastPrimaryTime;
					if (boxClosed)
					{
						float dayEndX = cc.GetXByTime(dayEnd);
						if (!float.IsNaN(dayEndX))
							endX = Math.Min(endX, dayEndX);
					}
				}
				if (float.IsNaN(startX) || float.IsNaN(endX)) { prev = null; continue; }

				bool visible = endX >= panelLeft && startX <= panelRight;
				float sX = Math.Max(panelLeft, startX);
				float eX = Math.Min(panelRight, endX);

				if (visible && eX > sX)
				{
					float yHigh = cs.GetYByValue(range.High);
					float yLow = cs.GetYByValue(range.Low);

					if (DisplayStyle == OrcaOrDisplayStyle.Shaded && FillOpacity > 0)
					{
						float top = Math.Max(panelTop, Math.Min(yHigh, yLow));
						float bottom = Math.Min(panelBottom, Math.Max(yHigh, yLow));
						if (bottom > top)
							FillRectWithOpacity(new RectangleF(sX, top, eX - sX, bottom - top), hlBrush, FillOpacity / 100f);
					}

					if (IsYVisible(yHigh, panelTop, panelBottom))
						RenderTarget.DrawLine(new Vector2(sX, yHigh), new Vector2(eX, yHigh), hlBrush, LineWidth, dxStrokes[0]);
					if (IsYVisible(yLow, panelTop, panelBottom))
						RenderTarget.DrawLine(new Vector2(sX, yLow), new Vector2(eX, yLow), hlBrush, LineWidth, dxStrokes[0]);

					if (ShowMidpoint && midBrush != null)
					{
						float yMid = cs.GetYByValue(range.Mid);
						if (IsYVisible(yMid, panelTop, panelBottom))
							RenderTarget.DrawLine(new Vector2(sX, yMid), new Vector2(eX, yMid), midBrush, MidLineWidth, dxStrokes[0]);
					}

					// Stepline join between the prior range and this one (TradingView "Connect successive ranges").
					// Only meaningful when boxes run into each other; day-ended boxes leave a gap instead.
					if (ConnectRanges && BoxEndMode == OrcaOrBoxEnd.UntilNextOpeningRange && prev != null && prev.IsValid && startX >= panelLeft && startX <= panelRight)
					{
						DrawVerticalJoin(startX, cs.GetYByValue(prev.High), yHigh, hlBrush, LineWidth, panelTop, panelBottom);
						DrawVerticalJoin(startX, cs.GetYByValue(prev.Low), yLow, hlBrush, LineWidth, panelTop, panelBottom);
						if (ShowMidpoint && midBrush != null)
							DrawVerticalJoin(startX, cs.GetYByValue(prev.Mid), cs.GetYByValue(range.Mid), midBrush, MidLineWidth, panelTop, panelBottom);
					}

					if (i == last && !boxClosed && ShowWordLabels && dxLabelFormat != null)
					{
						DrawLabel(session.Label + " OR-H", yHigh, hlBrush, labelX, panelLeft, panelRight, panelTop, panelBottom);
						DrawLabel(session.Label + " OR-L", yLow, hlBrush, labelX, panelLeft, panelRight, panelTop, panelBottom);
						if (ShowMidpoint && midBrush != null)
							DrawLabel(session.Label + " OR-M", cs.GetYByValue(range.Mid), midBrush, labelX, panelLeft, panelRight, panelTop, panelBottom);
					}
				}

				prev = range;
			}
		}

		private void RenderExtensions(ChartControl cc, ChartScale cs, OrSession rth, float panelLeft, float panelRight, float panelTop, float panelBottom, float labelX)
		{
			double step = resolvedExtensionStep;
			if (step <= 0)
				return;

			DxSolidBrush upBrush = dxBrushes[BrushExtUp];
			DxSolidBrush dnBrush = dxBrushes[BrushExtDn];
			if (upBrush == null || dnBrush == null)
				return;

			SharpDX.Direct2D1.StrokeStyle stroke = GetStroke(ExtensionLineStyle);
			List<OrRange> ranges = rth.Ranges;
			int last = ranges.Count - 1;

			for (int i = 0; i <= last; i++)
			{
				OrRange range = ranges[i];
				if (!range.IsValid)
					continue;

				// Extensions live inside RTH only: end at the RTH close (or the next RTH range if that comes first).
				DateTime closeTime = GetRthCloseChartTime(range);
				bool closed = lastPrimaryTime != DateTime.MinValue && closeTime <= lastPrimaryTime;
				float startX = cc.GetXByTime(range.AnchorTime);
				float endX = closed ? cc.GetXByTime(closeTime) : panelRight;
				if (i < last)
				{
					float nextX = cc.GetXByTime(ranges[i + 1].AnchorTime);
					if (!float.IsNaN(nextX))
						endX = Math.Min(endX, nextX);
				}
				if (float.IsNaN(startX) || float.IsNaN(endX))
					continue;
				if (endX < panelLeft || startX > panelRight)
					continue;

				float sX = Math.Max(panelLeft, startX);
				float eX = Math.Min(panelRight, endX);
				if (eX <= sX)
					continue;

				bool isLatest = i == last && !closed;
				for (int level = 1; level <= range.ExtUp; level++)
				{
					double price = range.High + level * step;
					float y = cs.GetYByValue(price);
					if (IsYVisible(y, panelTop, panelBottom))
						RenderTarget.DrawLine(new Vector2(sX, y), new Vector2(eX, y), upBrush, ExtensionLineWidth, stroke);
					if (isLatest && ShowWordLabels && dxLabelFormat != null)
						DrawLabel("RTH EXT +" + level, y, upBrush, labelX, panelLeft, panelRight, panelTop, panelBottom);
				}
				for (int level = 1; level <= range.ExtDn; level++)
				{
					double price = range.Low - level * step;
					float y = cs.GetYByValue(price);
					if (IsYVisible(y, panelTop, panelBottom))
						RenderTarget.DrawLine(new Vector2(sX, y), new Vector2(eX, y), dnBrush, ExtensionLineWidth, stroke);
					if (isLatest && ShowWordLabels && dxLabelFormat != null)
						DrawLabel("RTH EXT -" + level, y, dnBrush, labelX, panelLeft, panelRight, panelTop, panelBottom);
				}
			}
		}

		private static bool IsYVisible(float y, float panelTop, float panelBottom)
		{
			return !float.IsNaN(y) && y >= panelTop - 2f && y <= panelBottom + 2f;
		}

		private void DrawVerticalJoin(float x, float yFrom, float yTo, DxSolidBrush brush, float width, float panelTop, float panelBottom)
		{
			if (float.IsNaN(yFrom) || float.IsNaN(yTo) || Math.Abs(yFrom - yTo) < 0.5f)
				return;
			float top = Math.Max(panelTop, Math.Min(yFrom, yTo));
			float bottom = Math.Min(panelBottom, Math.Max(yFrom, yTo));
			if (bottom <= top)
				return;
			RenderTarget.DrawLine(new Vector2(x, top), new Vector2(x, bottom), brush, width, dxStrokes[0]);
		}

		private void FillRectWithOpacity(RectangleF rect, DxSolidBrush brush, float opacity)
		{
			float prevOpacity = brush.Opacity;
			brush.Opacity = Math.Max(0f, Math.Min(1f, opacity));
			try { RenderTarget.FillRectangle(rect, brush); }
			finally { brush.Opacity = prevOpacity; }
		}

		private float GetLabelAnchorX(ChartControl cc, float panelLeft, float panelRight)
		{
			// Mirrors the Pine labels anchored at bar_index + 1: sit just right of the last visible bar.
			try
			{
				if (ChartBars != null && CurrentBars != null && CurrentBars.Length > 0 && CurrentBars[0] >= 0)
				{
					int idx = ChartBars.ToIndex >= 0 ? Math.Min(ChartBars.ToIndex, CurrentBars[0]) : CurrentBars[0];
					float x = cc.GetXByBarIndex(ChartBars, idx);
					if (!float.IsNaN(x) && !float.IsInfinity(x))
						return Math.Max(panelLeft, Math.Min(panelRight, x + (float)cc.BarWidth + LabelXOffset));
				}
			}
			catch { }
			return panelRight;
		}

		// Word labels carry the level name only; prices live on the native right price scale.
		private void DrawLabel(string text, float y, DxSolidBrush brush, float anchorX, float panelLeft, float panelRight, float panelTop, float panelBottom)
		{
			if (brush == null || dxLabelFormat == null || string.IsNullOrEmpty(text) || float.IsNaN(y))
				return;
			if (y < panelTop || y > panelBottom)
				return;

			float width = EstimateLabelWidth(text);
			float height = LabelFontSize + 4f;
			float x = anchorX;
			if (x + width > panelRight)
				x = Math.Max(panelLeft, panelRight - width);

			RectangleF rect = new RectangleF(x, y - height, width, height);
			RenderTarget.DrawText(text, dxLabelFormat, rect, brush);
		}

		private float EstimateLabelWidth(string text)
		{
			if (string.IsNullOrEmpty(text))
				return Math.Max(24f, LabelFontSize * 2f);

			float width = 8f;
			foreach (char c in text)
				width += (c == ' ' || c == '.' || c == ':' || c == '-' || c == '+' ? 0.35f : 0.62f) * LabelFontSize;

			return Math.Max(24f, Math.Min(260f, width));
		}
		#endregion

		#region DX Resource Management
		private SharpDX.Direct2D1.StrokeStyle GetStroke(OrcaOrLineStyle style)
		{
			int idx = (int)style;
			if (dxStrokes == null || idx < 0 || idx >= dxStrokes.Length)
				return null;
			return dxStrokes[idx];
		}

		private static Color4 ToColor4(WpfBrush b)
		{
			WpfColor c = (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
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
				WpfBrush[] colorMap = new WpfBrush[BrushCount];
				colorMap[SessionRth * 2] = RthColor;           colorMap[SessionRth * 2 + 1] = RthMidColor;
				colorMap[SessionPm * 2] = PmColor;             colorMap[SessionPm * 2 + 1] = PmMidColor;
				colorMap[SessionGlobex * 2] = GlobexColor;     colorMap[SessionGlobex * 2 + 1] = GlobexMidColor;
				colorMap[SessionTokyo * 2] = TokyoColor;       colorMap[SessionTokyo * 2 + 1] = TokyoMidColor;
				colorMap[SessionMidnight * 2] = MidnightColor; colorMap[SessionMidnight * 2 + 1] = MidnightMidColor;
				colorMap[SessionLondon * 2] = LondonColor;     colorMap[SessionLondon * 2 + 1] = LondonMidColor;
				colorMap[SessionGold * 2] = GoldColor;         colorMap[SessionGold * 2 + 1] = GoldMidColor;
				colorMap[SessionCl * 2] = ClColor;             colorMap[SessionCl * 2 + 1] = ClMidColor;
				colorMap[BrushExtUp] = ExtensionUpColor;
				colorMap[BrushExtDn] = ExtensionDownColor;

				dxBrushes = new DxSolidBrush[colorMap.Length];
				for (int i = 0; i < colorMap.Length; i++)
					dxBrushes[i] = new DxSolidBrush(RenderTarget, ToColor4(colorMap[i] ?? WpfBrushes.White));

				var factory = RenderTarget.Factory;
				dxStrokes = new SharpDX.Direct2D1.StrokeStyle[3];
				dxStrokes[(int)OrcaOrLineStyle.Solid] = new SharpDX.Direct2D1.StrokeStyle(factory, new StrokeStyleProperties { DashStyle = DashStyle.Solid });
				dxStrokes[(int)OrcaOrLineStyle.Dashed] = new SharpDX.Direct2D1.StrokeStyle(factory, new StrokeStyleProperties { DashStyle = DashStyle.Dash });
				dxStrokes[(int)OrcaOrLineStyle.Dotted] = new SharpDX.Direct2D1.StrokeStyle(factory, new StrokeStyleProperties { DashStyle = DashStyle.Dot });

				dxLabelFormat = new SharpDX.DirectWrite.TextFormat(
					NinjaTrader.Core.Globals.DirectWriteFactory,
					"Segoe UI", FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, (float)LabelFontSize)
				{ TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Center };

				dxResourceRenderTarget = currentTarget;
				dxValid = true;
			}
			catch
			{
				DisposeDx();
			}
		}

		private void DisposeDx()
		{
			try
			{
				if (dxBrushes != null) foreach (var b in dxBrushes) b?.Dispose();
				if (dxStrokes != null) foreach (var s in dxStrokes) s?.Dispose();
				dxLabelFormat?.Dispose();
			}
			catch { }
			dxBrushes = null; dxStrokes = null; dxLabelFormat = null;
			dxResourceRenderTarget = IntPtr.Zero;
			dxValid = false;
		}

		public override void OnRenderTargetChanged() { DisposeDx(); base.OnRenderTargetChanged(); }
		#endregion

		#region Autoscale
		/// <summary>
		/// Autoscale is off by design. Reporting an inverted min/max tells NinjaTrader this indicator
		/// contributes nothing to the price scale, so OR levels and extensions never expand the chart.
		/// </summary>
		public override void OnCalculateMinMax()
		{
			MinValue = double.MaxValue;
			MaxValue = double.MinValue;
		}
		#endregion

		#region Properties
		// --- 01. Display ---
		[NinjaScriptProperty][Display(Name="Style", Description="Lines draws OR high/low only; Shaded also fills between them.", Order=1, GroupName="01. Display")]
		public OrcaOrDisplayStyle DisplayStyle { get; set; }

		[NinjaScriptProperty][Range(0, 100)][Display(Name="Fill Opacity", Description="Used when Style is Shaded. Higher = more visible.", Order=2, GroupName="01. Display")]
		public int FillOpacity { get; set; }

		[NinjaScriptProperty][Display(Name="Show Midpoint", Order=3, GroupName="01. Display")]
		public bool ShowMidpoint { get; set; }

		[NinjaScriptProperty][Display(Name="Show Price Labels", Description="Price tags on the native right price scale (like Orca MGI Daily). Requires the indicator's Price markers setting to stay on.", Order=4, GroupName="01. Display")]
		public bool ShowPriceLabels { get; set; }

		[NinjaScriptProperty][Display(Name="Show Word Labels", Description="Level names (e.g. RTH OR-H, RTH EXT +1) drawn in the chart pane right of the last bar. Names only; prices come from Show Price Labels.", Order=5, GroupName="01. Display")]
		public bool ShowWordLabels { get; set; }

		[NinjaScriptProperty][Display(Name="Connect Successive Ranges", Description="When off, each day's opening range is drawn on its own with no vertical join to the prior range. Only applies to the Until Next Opening Range box end mode.", Order=6, GroupName="01. Display")]
		public bool ConnectRanges { get; set; }

		[NinjaScriptProperty][Display(Name="OR Box End", Description="UntilNextOpeningRange: each session's box runs until that session's next OR (TradingView behavior). UntilDayEnd: every box ends at Day End Time; ranges opened after it (Globex, Tokyo) end at the next day's Day End Time.", Order=7, GroupName="01. Display")]
		public OrcaOrBoxEnd BoxEndMode { get; set; }

		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="Day End Time (New York)", Description="Used by the UntilDayEnd box end mode. Default 17:00 (Globex day boundary).", Order=8, GroupName="01. Display")]
		public TimeSpan DayEndTime { get; set; }

		[NinjaScriptProperty][Display(Name="Session Time Zone", Description="NewYork converts bar times from the NinjaTrader time zone to America/New_York. ChartTime uses bar times as-is.", Order=9, GroupName="01. Display")]
		public OrcaOrTimeZoneMode TimeZoneMode { get; set; }

		[NinjaScriptProperty][Range(1, 6)][Display(Name="Line Width", Order=10, GroupName="01. Display")]
		public int LineWidth { get; set; }

		[NinjaScriptProperty][Range(1, 6)][Display(Name="Midpoint Line Width", Order=11, GroupName="01. Display")]
		public int MidLineWidth { get; set; }

		[NinjaScriptProperty][Display(Name="Draw Behind Candles", Order=12, GroupName="01. Display")]
		public bool DrawBehindCandles { get; set; }

		[NinjaScriptProperty][Range(6, 24)][Display(Name="Word Label Font Size", Order=13, GroupName="01. Display")]
		public int LabelFontSize { get; set; }

		[NinjaScriptProperty][Range(0, 200)][Display(Name="Word Label X Offset", Order=14, GroupName="01. Display")]
		public int LabelXOffset { get; set; }

		// --- 02. RTH Open (default 09:30 NY) ---
		[NinjaScriptProperty][Display(Name="Enable", Order=1, GroupName="02. RTH Open")]
		public bool RthEnabled { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="Open Time (New York)", Description="Session open in America/New_York; the 30-second bar starting at this time is the opening range. Default 09:30.", Order=2, GroupName="02. RTH Open")]
		public TimeSpan RthOpenTime { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="RTH Close Time (New York)", Description="RTH extensions (lines, unlocks, scale tags) end here and never stretch into Globex. Default 16:00.", Order=3, GroupName="02. RTH Open")]
		public TimeSpan RthCloseTime { get; set; }
		[XmlIgnore][Display(Name="High/Low Color", Order=4, GroupName="02. RTH Open")]
		public WpfBrush RthColor { get; set; }
		[Browsable(false)] public string RthColorSerializable { get { return Serialize.BrushToString(RthColor); } set { RthColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Mid Color", Order=5, GroupName="02. RTH Open")]
		public WpfBrush RthMidColor { get; set; }
		[Browsable(false)] public string RthMidColorSerializable { get { return Serialize.BrushToString(RthMidColor); } set { RthMidColor = Serialize.StringToBrush(value); } }

		// --- 03. PM Open Open (default 13:30 NY) ---
		[NinjaScriptProperty][Display(Name="Enable", Order=1, GroupName="03. PM Open Open")]
		public bool PmEnabled { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="Open Time (New York)", Description="Session open in America/New_York; the 30-second bar starting at this time is the opening range. Default 13:30.", Order=2, GroupName="03. PM Open Open")]
		public TimeSpan PmOpenTime { get; set; }
		[XmlIgnore][Display(Name="High/Low Color", Order=3, GroupName="03. PM Open Open")]
		public WpfBrush PmColor { get; set; }
		[Browsable(false)] public string PmColorSerializable { get { return Serialize.BrushToString(PmColor); } set { PmColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Mid Color", Order=4, GroupName="03. PM Open Open")]
		public WpfBrush PmMidColor { get; set; }
		[Browsable(false)] public string PmMidColorSerializable { get { return Serialize.BrushToString(PmMidColor); } set { PmMidColor = Serialize.StringToBrush(value); } }

		// --- 04. Globex Open (default 18:00 NY) ---
		[NinjaScriptProperty][Display(Name="Enable", Order=1, GroupName="04. Globex Open")]
		public bool GlobexEnabled { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="Open Time (New York)", Description="Session open in America/New_York; the 30-second bar starting at this time is the opening range. Default 18:00.", Order=2, GroupName="04. Globex Open")]
		public TimeSpan GlobexOpenTime { get; set; }
		[XmlIgnore][Display(Name="High/Low Color", Order=3, GroupName="04. Globex Open")]
		public WpfBrush GlobexColor { get; set; }
		[Browsable(false)] public string GlobexColorSerializable { get { return Serialize.BrushToString(GlobexColor); } set { GlobexColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Mid Color", Order=4, GroupName="04. Globex Open")]
		public WpfBrush GlobexMidColor { get; set; }
		[Browsable(false)] public string GlobexMidColorSerializable { get { return Serialize.BrushToString(GlobexMidColor); } set { GlobexMidColor = Serialize.StringToBrush(value); } }

		// --- 05. Tokyo Open (default 20:00 NY) ---
		[NinjaScriptProperty][Display(Name="Enable", Order=1, GroupName="05. Tokyo Open")]
		public bool TokyoEnabled { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="Open Time (New York)", Description="Session open in America/New_York; the 30-second bar starting at this time is the opening range. Default 20:00.", Order=2, GroupName="05. Tokyo Open")]
		public TimeSpan TokyoOpenTime { get; set; }
		[XmlIgnore][Display(Name="High/Low Color", Order=3, GroupName="05. Tokyo Open")]
		public WpfBrush TokyoColor { get; set; }
		[Browsable(false)] public string TokyoColorSerializable { get { return Serialize.BrushToString(TokyoColor); } set { TokyoColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Mid Color", Order=4, GroupName="05. Tokyo Open")]
		public WpfBrush TokyoMidColor { get; set; }
		[Browsable(false)] public string TokyoMidColorSerializable { get { return Serialize.BrushToString(TokyoMidColor); } set { TokyoMidColor = Serialize.StringToBrush(value); } }

		// --- 06. Midnight Open (default 00:00 NY) ---
		[NinjaScriptProperty][Display(Name="Enable", Order=1, GroupName="06. Midnight Open")]
		public bool MidnightEnabled { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="Open Time (New York)", Description="Session open in America/New_York; the 30-second bar starting at this time is the opening range. Default 00:00.", Order=2, GroupName="06. Midnight Open")]
		public TimeSpan MidnightOpenTime { get; set; }
		[XmlIgnore][Display(Name="High/Low Color", Order=3, GroupName="06. Midnight Open")]
		public WpfBrush MidnightColor { get; set; }
		[Browsable(false)] public string MidnightColorSerializable { get { return Serialize.BrushToString(MidnightColor); } set { MidnightColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Mid Color", Order=4, GroupName="06. Midnight Open")]
		public WpfBrush MidnightMidColor { get; set; }
		[Browsable(false)] public string MidnightMidColorSerializable { get { return Serialize.BrushToString(MidnightMidColor); } set { MidnightMidColor = Serialize.StringToBrush(value); } }

		// --- 07. London Open (default 03:00 NY) ---
		[NinjaScriptProperty][Display(Name="Enable", Order=1, GroupName="07. London Open")]
		public bool LondonEnabled { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="Open Time (New York)", Description="Session open in America/New_York; the 30-second bar starting at this time is the opening range. Default 03:00.", Order=2, GroupName="07. London Open")]
		public TimeSpan LondonOpenTime { get; set; }
		[XmlIgnore][Display(Name="High/Low Color", Order=3, GroupName="07. London Open")]
		public WpfBrush LondonColor { get; set; }
		[Browsable(false)] public string LondonColorSerializable { get { return Serialize.BrushToString(LondonColor); } set { LondonColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Mid Color", Order=4, GroupName="07. London Open")]
		public WpfBrush LondonMidColor { get; set; }
		[Browsable(false)] public string LondonMidColorSerializable { get { return Serialize.BrushToString(LondonMidColor); } set { LondonMidColor = Serialize.StringToBrush(value); } }

		// --- 08. Gold Open (default 08:20 NY) ---
		[NinjaScriptProperty][Display(Name="Enable", Order=1, GroupName="08. Gold Open")]
		public bool GoldEnabled { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="Open Time (New York)", Description="Session open in America/New_York; the 30-second bar starting at this time is the opening range. Default 08:20.", Order=2, GroupName="08. Gold Open")]
		public TimeSpan GoldOpenTime { get; set; }
		[XmlIgnore][Display(Name="High/Low Color", Order=3, GroupName="08. Gold Open")]
		public WpfBrush GoldColor { get; set; }
		[Browsable(false)] public string GoldColorSerializable { get { return Serialize.BrushToString(GoldColor); } set { GoldColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Mid Color", Order=4, GroupName="08. Gold Open")]
		public WpfBrush GoldMidColor { get; set; }
		[Browsable(false)] public string GoldMidColorSerializable { get { return Serialize.BrushToString(GoldMidColor); } set { GoldMidColor = Serialize.StringToBrush(value); } }

		// --- 09. CL Open (default 09:00 NY) ---
		[NinjaScriptProperty][Display(Name="Enable", Order=1, GroupName="09. CL Open")]
		public bool ClEnabled { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")][Display(Name="Open Time (New York)", Description="Session open in America/New_York; the 30-second bar starting at this time is the opening range. Default 09:00.", Order=2, GroupName="09. CL Open")]
		public TimeSpan ClOpenTime { get; set; }
		[XmlIgnore][Display(Name="High/Low Color", Order=3, GroupName="09. CL Open")]
		public WpfBrush ClColor { get; set; }
		[Browsable(false)] public string ClColorSerializable { get { return Serialize.BrushToString(ClColor); } set { ClColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Mid Color", Order=4, GroupName="09. CL Open")]
		public WpfBrush ClMidColor { get; set; }
		[Browsable(false)] public string ClMidColorSerializable { get { return Serialize.BrushToString(ClMidColor); } set { ClMidColor = Serialize.StringToBrush(value); } }

		// --- 10. RTH Extensions ---
		[NinjaScriptProperty][Display(Name="Enable RTH Extensions", Order=1, GroupName="10. RTH Extensions (ES: 15 pts / NQ: 65 pts)")]
		public bool ExtensionsEnabled { get; set; }

		[NinjaScriptProperty][Display(Name="Extension Step", Description="Auto detects NQ/MNQ (65 pts) vs ES/MES and everything else (15 pts). Can be overridden.", Order=2, GroupName="10. RTH Extensions (ES: 15 pts / NQ: 65 pts)")]
		public OrcaOrExtensionStep ExtensionStepMode { get; set; }

		[NinjaScriptProperty][Range(0.25, 100000)][Display(Name="Custom Step", Description="Points per extension level when Extension Step is Custom.", Order=3, GroupName="10. RTH Extensions (ES: 15 pts / NQ: 65 pts)")]
		public double CustomExtensionStep { get; set; }

		[NinjaScriptProperty][Display(Name="Expansion Trigger", Description="Both Sides: touching the last revealed level on either side reveals the next level on both sides. Each Side Individually: each side unlocks on its own touches.", Order=4, GroupName="10. RTH Extensions (ES: 15 pts / NQ: 65 pts)")]
		public OrcaOrExpansionTrigger ExpansionTrigger { get; set; }

		[NinjaScriptProperty][Range(1, 20)][Display(Name="Initial Levels Per Side", Description="Extension levels shown as soon as the RTH opening range prints. Touching the highest revealed level always reveals the next one; there is no upper limit.", Order=5, GroupName="10. RTH Extensions (ES: 15 pts / NQ: 65 pts)")]
		public int InitialExtensionLevels { get; set; }

		[NinjaScriptProperty][Display(Name="Line Style", Order=6, GroupName="10. RTH Extensions (ES: 15 pts / NQ: 65 pts)")]
		public OrcaOrLineStyle ExtensionLineStyle { get; set; }

		[NinjaScriptProperty][Range(1, 4)][Display(Name="Line Width", Order=7, GroupName="10. RTH Extensions (ES: 15 pts / NQ: 65 pts)")]
		public int ExtensionLineWidth { get; set; }

		[XmlIgnore][Display(Name="Upper Extensions", Order=8, GroupName="10. RTH Extensions (ES: 15 pts / NQ: 65 pts)")]
		public WpfBrush ExtensionUpColor { get; set; }
		[Browsable(false)] public string ExtensionUpColorSerializable { get { return Serialize.BrushToString(ExtensionUpColor); } set { ExtensionUpColor = Serialize.StringToBrush(value); } }

		[XmlIgnore][Display(Name="Lower Extensions", Order=9, GroupName="10. RTH Extensions (ES: 15 pts / NQ: 65 pts)")]
		public WpfBrush ExtensionDownColor { get; set; }
		[Browsable(false)] public string ExtensionDownColorSerializable { get { return Serialize.BrushToString(ExtensionDownColor); } set { ExtensionDownColor = Serialize.StringToBrush(value); } }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaOpeningRanges[] cacheOrcaOpeningRanges;
		public OrcaOpeningRanges OrcaOpeningRanges(OrcaOrDisplayStyle displayStyle, int fillOpacity, bool showMidpoint, bool showPriceLabels, bool showWordLabels, bool connectRanges, OrcaOrBoxEnd boxEndMode, TimeSpan dayEndTime, OrcaOrTimeZoneMode timeZoneMode, int lineWidth, int midLineWidth, bool drawBehindCandles, int labelFontSize, int labelXOffset, bool rthEnabled, TimeSpan rthOpenTime, TimeSpan rthCloseTime, bool pmEnabled, TimeSpan pmOpenTime, bool globexEnabled, TimeSpan globexOpenTime, bool tokyoEnabled, TimeSpan tokyoOpenTime, bool midnightEnabled, TimeSpan midnightOpenTime, bool londonEnabled, TimeSpan londonOpenTime, bool goldEnabled, TimeSpan goldOpenTime, bool clEnabled, TimeSpan clOpenTime, bool extensionsEnabled, OrcaOrExtensionStep extensionStepMode, double customExtensionStep, OrcaOrExpansionTrigger expansionTrigger, int initialExtensionLevels, OrcaOrLineStyle extensionLineStyle, int extensionLineWidth)
		{
			return OrcaOpeningRanges(Input, displayStyle, fillOpacity, showMidpoint, showPriceLabels, showWordLabels, connectRanges, boxEndMode, dayEndTime, timeZoneMode, lineWidth, midLineWidth, drawBehindCandles, labelFontSize, labelXOffset, rthEnabled, rthOpenTime, rthCloseTime, pmEnabled, pmOpenTime, globexEnabled, globexOpenTime, tokyoEnabled, tokyoOpenTime, midnightEnabled, midnightOpenTime, londonEnabled, londonOpenTime, goldEnabled, goldOpenTime, clEnabled, clOpenTime, extensionsEnabled, extensionStepMode, customExtensionStep, expansionTrigger, initialExtensionLevels, extensionLineStyle, extensionLineWidth);
		}

		public OrcaOpeningRanges OrcaOpeningRanges(ISeries<double> input, OrcaOrDisplayStyle displayStyle, int fillOpacity, bool showMidpoint, bool showPriceLabels, bool showWordLabels, bool connectRanges, OrcaOrBoxEnd boxEndMode, TimeSpan dayEndTime, OrcaOrTimeZoneMode timeZoneMode, int lineWidth, int midLineWidth, bool drawBehindCandles, int labelFontSize, int labelXOffset, bool rthEnabled, TimeSpan rthOpenTime, TimeSpan rthCloseTime, bool pmEnabled, TimeSpan pmOpenTime, bool globexEnabled, TimeSpan globexOpenTime, bool tokyoEnabled, TimeSpan tokyoOpenTime, bool midnightEnabled, TimeSpan midnightOpenTime, bool londonEnabled, TimeSpan londonOpenTime, bool goldEnabled, TimeSpan goldOpenTime, bool clEnabled, TimeSpan clOpenTime, bool extensionsEnabled, OrcaOrExtensionStep extensionStepMode, double customExtensionStep, OrcaOrExpansionTrigger expansionTrigger, int initialExtensionLevels, OrcaOrLineStyle extensionLineStyle, int extensionLineWidth)
		{
			if (cacheOrcaOpeningRanges != null)
				for (int idx = 0; idx < cacheOrcaOpeningRanges.Length; idx++)
					if (cacheOrcaOpeningRanges[idx] != null && cacheOrcaOpeningRanges[idx].DisplayStyle == displayStyle && cacheOrcaOpeningRanges[idx].FillOpacity == fillOpacity && cacheOrcaOpeningRanges[idx].ShowMidpoint == showMidpoint && cacheOrcaOpeningRanges[idx].ShowPriceLabels == showPriceLabels && cacheOrcaOpeningRanges[idx].ShowWordLabels == showWordLabels && cacheOrcaOpeningRanges[idx].ConnectRanges == connectRanges && cacheOrcaOpeningRanges[idx].BoxEndMode == boxEndMode && cacheOrcaOpeningRanges[idx].DayEndTime == dayEndTime && cacheOrcaOpeningRanges[idx].TimeZoneMode == timeZoneMode && cacheOrcaOpeningRanges[idx].LineWidth == lineWidth && cacheOrcaOpeningRanges[idx].MidLineWidth == midLineWidth && cacheOrcaOpeningRanges[idx].DrawBehindCandles == drawBehindCandles && cacheOrcaOpeningRanges[idx].LabelFontSize == labelFontSize && cacheOrcaOpeningRanges[idx].LabelXOffset == labelXOffset && cacheOrcaOpeningRanges[idx].RthEnabled == rthEnabled && cacheOrcaOpeningRanges[idx].RthOpenTime == rthOpenTime && cacheOrcaOpeningRanges[idx].RthCloseTime == rthCloseTime && cacheOrcaOpeningRanges[idx].PmEnabled == pmEnabled && cacheOrcaOpeningRanges[idx].PmOpenTime == pmOpenTime && cacheOrcaOpeningRanges[idx].GlobexEnabled == globexEnabled && cacheOrcaOpeningRanges[idx].GlobexOpenTime == globexOpenTime && cacheOrcaOpeningRanges[idx].TokyoEnabled == tokyoEnabled && cacheOrcaOpeningRanges[idx].TokyoOpenTime == tokyoOpenTime && cacheOrcaOpeningRanges[idx].MidnightEnabled == midnightEnabled && cacheOrcaOpeningRanges[idx].MidnightOpenTime == midnightOpenTime && cacheOrcaOpeningRanges[idx].LondonEnabled == londonEnabled && cacheOrcaOpeningRanges[idx].LondonOpenTime == londonOpenTime && cacheOrcaOpeningRanges[idx].GoldEnabled == goldEnabled && cacheOrcaOpeningRanges[idx].GoldOpenTime == goldOpenTime && cacheOrcaOpeningRanges[idx].ClEnabled == clEnabled && cacheOrcaOpeningRanges[idx].ClOpenTime == clOpenTime && cacheOrcaOpeningRanges[idx].ExtensionsEnabled == extensionsEnabled && cacheOrcaOpeningRanges[idx].ExtensionStepMode == extensionStepMode && cacheOrcaOpeningRanges[idx].CustomExtensionStep == customExtensionStep && cacheOrcaOpeningRanges[idx].ExpansionTrigger == expansionTrigger && cacheOrcaOpeningRanges[idx].InitialExtensionLevels == initialExtensionLevels && cacheOrcaOpeningRanges[idx].ExtensionLineStyle == extensionLineStyle && cacheOrcaOpeningRanges[idx].ExtensionLineWidth == extensionLineWidth && cacheOrcaOpeningRanges[idx].EqualsInput(input))
						return cacheOrcaOpeningRanges[idx];
			return CacheIndicator<OrcaOpeningRanges>(new OrcaOpeningRanges(){ DisplayStyle = displayStyle, FillOpacity = fillOpacity, ShowMidpoint = showMidpoint, ShowPriceLabels = showPriceLabels, ShowWordLabels = showWordLabels, ConnectRanges = connectRanges, BoxEndMode = boxEndMode, DayEndTime = dayEndTime, TimeZoneMode = timeZoneMode, LineWidth = lineWidth, MidLineWidth = midLineWidth, DrawBehindCandles = drawBehindCandles, LabelFontSize = labelFontSize, LabelXOffset = labelXOffset, RthEnabled = rthEnabled, RthOpenTime = rthOpenTime, RthCloseTime = rthCloseTime, PmEnabled = pmEnabled, PmOpenTime = pmOpenTime, GlobexEnabled = globexEnabled, GlobexOpenTime = globexOpenTime, TokyoEnabled = tokyoEnabled, TokyoOpenTime = tokyoOpenTime, MidnightEnabled = midnightEnabled, MidnightOpenTime = midnightOpenTime, LondonEnabled = londonEnabled, LondonOpenTime = londonOpenTime, GoldEnabled = goldEnabled, GoldOpenTime = goldOpenTime, ClEnabled = clEnabled, ClOpenTime = clOpenTime, ExtensionsEnabled = extensionsEnabled, ExtensionStepMode = extensionStepMode, CustomExtensionStep = customExtensionStep, ExpansionTrigger = expansionTrigger, InitialExtensionLevels = initialExtensionLevels, ExtensionLineStyle = extensionLineStyle, ExtensionLineWidth = extensionLineWidth }, input, ref cacheOrcaOpeningRanges);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaOpeningRanges OrcaOpeningRanges(OrcaOrDisplayStyle displayStyle, int fillOpacity, bool showMidpoint, bool showPriceLabels, bool showWordLabels, bool connectRanges, OrcaOrBoxEnd boxEndMode, TimeSpan dayEndTime, OrcaOrTimeZoneMode timeZoneMode, int lineWidth, int midLineWidth, bool drawBehindCandles, int labelFontSize, int labelXOffset, bool rthEnabled, TimeSpan rthOpenTime, TimeSpan rthCloseTime, bool pmEnabled, TimeSpan pmOpenTime, bool globexEnabled, TimeSpan globexOpenTime, bool tokyoEnabled, TimeSpan tokyoOpenTime, bool midnightEnabled, TimeSpan midnightOpenTime, bool londonEnabled, TimeSpan londonOpenTime, bool goldEnabled, TimeSpan goldOpenTime, bool clEnabled, TimeSpan clOpenTime, bool extensionsEnabled, OrcaOrExtensionStep extensionStepMode, double customExtensionStep, OrcaOrExpansionTrigger expansionTrigger, int initialExtensionLevels, OrcaOrLineStyle extensionLineStyle, int extensionLineWidth)
		{
			return indicator.OrcaOpeningRanges(Input, displayStyle, fillOpacity, showMidpoint, showPriceLabels, showWordLabels, connectRanges, boxEndMode, dayEndTime, timeZoneMode, lineWidth, midLineWidth, drawBehindCandles, labelFontSize, labelXOffset, rthEnabled, rthOpenTime, rthCloseTime, pmEnabled, pmOpenTime, globexEnabled, globexOpenTime, tokyoEnabled, tokyoOpenTime, midnightEnabled, midnightOpenTime, londonEnabled, londonOpenTime, goldEnabled, goldOpenTime, clEnabled, clOpenTime, extensionsEnabled, extensionStepMode, customExtensionStep, expansionTrigger, initialExtensionLevels, extensionLineStyle, extensionLineWidth);
		}

		public Indicators.OrcaOpeningRanges OrcaOpeningRanges(ISeries<double> input , OrcaOrDisplayStyle displayStyle, int fillOpacity, bool showMidpoint, bool showPriceLabels, bool showWordLabels, bool connectRanges, OrcaOrBoxEnd boxEndMode, TimeSpan dayEndTime, OrcaOrTimeZoneMode timeZoneMode, int lineWidth, int midLineWidth, bool drawBehindCandles, int labelFontSize, int labelXOffset, bool rthEnabled, TimeSpan rthOpenTime, TimeSpan rthCloseTime, bool pmEnabled, TimeSpan pmOpenTime, bool globexEnabled, TimeSpan globexOpenTime, bool tokyoEnabled, TimeSpan tokyoOpenTime, bool midnightEnabled, TimeSpan midnightOpenTime, bool londonEnabled, TimeSpan londonOpenTime, bool goldEnabled, TimeSpan goldOpenTime, bool clEnabled, TimeSpan clOpenTime, bool extensionsEnabled, OrcaOrExtensionStep extensionStepMode, double customExtensionStep, OrcaOrExpansionTrigger expansionTrigger, int initialExtensionLevels, OrcaOrLineStyle extensionLineStyle, int extensionLineWidth)
		{
			return indicator.OrcaOpeningRanges(input, displayStyle, fillOpacity, showMidpoint, showPriceLabels, showWordLabels, connectRanges, boxEndMode, dayEndTime, timeZoneMode, lineWidth, midLineWidth, drawBehindCandles, labelFontSize, labelXOffset, rthEnabled, rthOpenTime, rthCloseTime, pmEnabled, pmOpenTime, globexEnabled, globexOpenTime, tokyoEnabled, tokyoOpenTime, midnightEnabled, midnightOpenTime, londonEnabled, londonOpenTime, goldEnabled, goldOpenTime, clEnabled, clOpenTime, extensionsEnabled, extensionStepMode, customExtensionStep, expansionTrigger, initialExtensionLevels, extensionLineStyle, extensionLineWidth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaOpeningRanges OrcaOpeningRanges(OrcaOrDisplayStyle displayStyle, int fillOpacity, bool showMidpoint, bool showPriceLabels, bool showWordLabels, bool connectRanges, OrcaOrBoxEnd boxEndMode, TimeSpan dayEndTime, OrcaOrTimeZoneMode timeZoneMode, int lineWidth, int midLineWidth, bool drawBehindCandles, int labelFontSize, int labelXOffset, bool rthEnabled, TimeSpan rthOpenTime, TimeSpan rthCloseTime, bool pmEnabled, TimeSpan pmOpenTime, bool globexEnabled, TimeSpan globexOpenTime, bool tokyoEnabled, TimeSpan tokyoOpenTime, bool midnightEnabled, TimeSpan midnightOpenTime, bool londonEnabled, TimeSpan londonOpenTime, bool goldEnabled, TimeSpan goldOpenTime, bool clEnabled, TimeSpan clOpenTime, bool extensionsEnabled, OrcaOrExtensionStep extensionStepMode, double customExtensionStep, OrcaOrExpansionTrigger expansionTrigger, int initialExtensionLevels, OrcaOrLineStyle extensionLineStyle, int extensionLineWidth)
		{
			return indicator.OrcaOpeningRanges(Input, displayStyle, fillOpacity, showMidpoint, showPriceLabels, showWordLabels, connectRanges, boxEndMode, dayEndTime, timeZoneMode, lineWidth, midLineWidth, drawBehindCandles, labelFontSize, labelXOffset, rthEnabled, rthOpenTime, rthCloseTime, pmEnabled, pmOpenTime, globexEnabled, globexOpenTime, tokyoEnabled, tokyoOpenTime, midnightEnabled, midnightOpenTime, londonEnabled, londonOpenTime, goldEnabled, goldOpenTime, clEnabled, clOpenTime, extensionsEnabled, extensionStepMode, customExtensionStep, expansionTrigger, initialExtensionLevels, extensionLineStyle, extensionLineWidth);
		}

		public Indicators.OrcaOpeningRanges OrcaOpeningRanges(ISeries<double> input , OrcaOrDisplayStyle displayStyle, int fillOpacity, bool showMidpoint, bool showPriceLabels, bool showWordLabels, bool connectRanges, OrcaOrBoxEnd boxEndMode, TimeSpan dayEndTime, OrcaOrTimeZoneMode timeZoneMode, int lineWidth, int midLineWidth, bool drawBehindCandles, int labelFontSize, int labelXOffset, bool rthEnabled, TimeSpan rthOpenTime, TimeSpan rthCloseTime, bool pmEnabled, TimeSpan pmOpenTime, bool globexEnabled, TimeSpan globexOpenTime, bool tokyoEnabled, TimeSpan tokyoOpenTime, bool midnightEnabled, TimeSpan midnightOpenTime, bool londonEnabled, TimeSpan londonOpenTime, bool goldEnabled, TimeSpan goldOpenTime, bool clEnabled, TimeSpan clOpenTime, bool extensionsEnabled, OrcaOrExtensionStep extensionStepMode, double customExtensionStep, OrcaOrExpansionTrigger expansionTrigger, int initialExtensionLevels, OrcaOrLineStyle extensionLineStyle, int extensionLineWidth)
		{
			return indicator.OrcaOpeningRanges(input, displayStyle, fillOpacity, showMidpoint, showPriceLabels, showWordLabels, connectRanges, boxEndMode, dayEndTime, timeZoneMode, lineWidth, midLineWidth, drawBehindCandles, labelFontSize, labelXOffset, rthEnabled, rthOpenTime, rthCloseTime, pmEnabled, pmOpenTime, globexEnabled, globexOpenTime, tokyoEnabled, tokyoOpenTime, midnightEnabled, midnightOpenTime, londonEnabled, londonOpenTime, goldEnabled, goldOpenTime, clEnabled, clOpenTime, extensionsEnabled, extensionStepMode, customExtensionStep, expansionTrigger, initialExtensionLevels, extensionLineStyle, extensionLineWidth);
		}
	}
}

#endregion
