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
using WpfColors = System.Windows.Media.Colors;
using DxSolidBrush = SharpDX.Direct2D1.SolidColorBrush;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum MgiPanelPosition { TopLeft, TopRight, BottomLeft, BottomRight }
	public enum MgiHarmonicSource { IBRange, OvernightRange, ORRange, RTHRange }
	public enum MgiHarmonicAnchor { IBHigh, IBLow, ONHigh, ONLow, ORHigh, ORLow }
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaMGIStatistics : Indicator
	{
		#region Session Record
		private class SessionRecord
		{
			public DateTime Date;
			public double ONRange, IBRange, RTHRange, ETHRange, ORRange;
			public double ONVAWidth, RTHVAWidth, ETHVAWidth;
			public double ONVolume, IBVolume, RTHVolume, ETHVolume;
			public double TotalRange;
			public List<double> PeriodRanges = new List<double>();
			// Cumulative volume by elapsed minutes from RTH open for RVOL
			public Dictionary<int, double> CumVolByMinute = new Dictionary<int, double>();
			public double WeeklyRange, WeeklyVolume;
			public bool IsWeekEnd;
		}
		#endregion

		#region Fields
		private List<SessionRecord> history;
		private SessionRecord curSession;
		private DateTime curSessionDate = DateTime.MinValue;
		private bool inRTH;

		// Current developing session data for live calc
		private double curONH = double.NaN, curONL = double.NaN;
		private double curIBH = double.NaN, curIBL = double.NaN;
		private double curORH = double.NaN, curORL = double.NaN;
		private double curRTHH = double.NaN, curRTHL = double.NaN;
		private double curETHH = double.NaN, curETHL = double.NaN;
		private double curONVol, curIBVol, curRTHVol, curETHVol;
		private bool ibDone, orDone;
		private double curRTHCumVol;
		private int curRTHElapsedMin;

		// Weekly tracking
		private double curWeekH = double.NaN, curWeekL = double.NaN, curWeekVol;
		private DateTime curWeekStart = DateTime.MinValue;
		private List<double> weeklyRanges;
		private List<double> weeklyVolumes;

		// 30-min period tracking
		private double periodH = double.NaN, periodL = double.NaN;
		private int periodIdx;
		private DateTime periodEnd;

		// Harmonic calc cache
		private double harmonicRefRange = double.NaN;
		private double harmonicAnchorPrice = double.NaN;

		// Panel rows
		private List<KeyValuePair<string, string>> displayRows;

		// DX
		private bool dxValid;
		private DxSolidBrush dxBgBrush, dxHeaderBrush, dxLabelBrush, dxValueBrush, dxBorderBrush;
		private SharpDX.DirectWrite.TextFormat dxHeaderFmt, dxBodyFmt;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca MGI Statistics";
				Description = "On-chart statistics panel displaying average ranges, volumes, value area widths, relative volume, and harmonic rotation levels.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;

				RTHOpenTime = new TimeSpan(9, 30, 0);
				RTHCloseTime = new TimeSpan(16, 15, 0);
				ETHOpenTime = new TimeSpan(18, 0, 0);
				LookbackPeriod = 20;
				ORDuration = MgiORDuration.Min30;

				ShowDailyRangeStats = true; ShowVAStats = true;
				ShowVolumeStats = true; ShowWeeklyStats = true;
				ShowHarmonicRotation = true; ShowRelativeVolume = true;
				NumPeriodsToShow = 4;

				HarmonicSource = MgiHarmonicSource.IBRange;
				HarmonicAnchor = MgiHarmonicAnchor.IBHigh;

				PanelPosition = MgiPanelPosition.TopRight;
				PanelWidth = 280; RowHeight = 18; PanelPadding = 10;
				PanelBgOpacity = 80;

				PanelBgColor = WpfBrushes.Black;
				HeaderColor = WpfBrushes.Gold;
				LabelTextColor = WpfBrushes.Silver;
				ValueTextColor = WpfBrushes.White;
				BorderColor = WpfBrushes.DimGray;
				FontName = "Segoe UI"; FontSize = 11; HeaderFontSize = 12;

				AddPlot(new Stroke(WpfBrushes.Transparent, 1), PlotStyle.Line, "StatsDummy");
			}
			else if (State == State.DataLoaded)
			{
				history = new List<SessionRecord>();
				displayRows = new List<KeyValuePair<string, string>>();
				weeklyRanges = new List<double>();
				weeklyVolumes = new List<double>();
			}
			else if (State == State.Terminated) { DisposeDx(); }
		}

		#region Session Detection
		private bool CrossedTime(TimeSpan prev, TimeSpan cur, TimeSpan target)
		{
			if (target > prev && target <= cur) return true;
			if (prev > cur && (target > prev || target <= cur)) return true;
			return false;
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) return;
			DateTime t = Time[0]; TimeSpan tod = t.TimeOfDay;
			DateTime prevT = Time[1]; TimeSpan prevTod = prevT.TimeOfDay;
			double h = High[0], l = Low[0], c = Close[0], vol = Volume[0];

			bool rthCrossed = CrossedTime(prevTod, tod, RTHOpenTime);

			if (rthCrossed)
			{
				// Finalize prior session
				if (curSession != null && curSessionDate != DateTime.MinValue)
				{
					curSession.ONRange = SafeRange(curONH, curONL);
					curSession.IBRange = SafeRange(curIBH, curIBL);
					curSession.RTHRange = SafeRange(curRTHH, curRTHL);
					curSession.ETHRange = SafeRange(curETHH, curETHL);
					curSession.ORRange = SafeRange(curORH, curORL);
					curSession.ONVolume = curONVol; curSession.IBVolume = curIBVol;
					curSession.RTHVolume = curRTHVol; curSession.ETHVolume = curETHVol;
					if (curSession.TotalRange > 0)
					{
						double vaWidth = curSession.RTHVAWidth;
						// VA% of range already set inline
					}
					history.Add(curSession);
					if (history.Count > LookbackPeriod + 5) history.RemoveAt(0);
				}

				// New session
				curSession = new SessionRecord { Date = t.Date };
				curSessionDate = t.Date;
				curONH = curONL = curIBH = curIBL = curORH = curORL = curRTHH = curRTHL = double.NaN;
				curETHH = curETHL = double.NaN;
				curONVol = curIBVol = curRTHVol = curETHVol = curRTHCumVol = 0;
				ibDone = orDone = false;
				periodH = periodL = double.NaN;
				periodIdx = 0;
				periodEnd = t.Date + RTHOpenTime + TimeSpan.FromMinutes(30);
			}

			inRTH = tod >= RTHOpenTime && tod < RTHCloseTime;

			if (inRTH)
			{
				if (double.IsNaN(curRTHH) || h > curRTHH) curRTHH = h;
				if (double.IsNaN(curRTHL) || l < curRTHL) curRTHL = l;
				curRTHVol += vol;
				curRTHCumVol += vol;
				curRTHElapsedMin = (int)(tod - RTHOpenTime).TotalMinutes;
				if (curSession != null && curRTHElapsedMin > 0)
					curSession.CumVolByMinute[curRTHElapsedMin] = curRTHCumVol;

				// OR
				if (!orDone)
				{
					TimeSpan orEnd = RTHOpenTime + TimeSpan.FromMinutes((int)ORDuration);
					if (tod < orEnd) { if (double.IsNaN(curORH) || h > curORH) curORH = h; if (double.IsNaN(curORL) || l < curORL) curORL = l; }
					else orDone = true;
				}
				// IB
				if (!ibDone)
				{
					TimeSpan ibEnd = RTHOpenTime + TimeSpan.FromMinutes(60);
					if (tod < ibEnd) { if (double.IsNaN(curIBH) || h > curIBH) curIBH = h; if (double.IsNaN(curIBL) || l < curIBL) curIBL = l; curIBVol += vol; }
					else ibDone = true;
				}
				// 30-min periods
				if (t >= periodEnd && periodIdx < 30)
				{
					if (curSession != null && !double.IsNaN(periodH))
						curSession.PeriodRanges.Add(periodH - periodL);
					periodIdx++;
					periodEnd = periodEnd.AddMinutes(30);
					periodH = h; periodL = l;
				}
				else { if (double.IsNaN(periodH) || h > periodH) periodH = h; if (double.IsNaN(periodL) || l < periodL) periodL = l; }
			}
			else
			{
				// Overnight
				if (double.IsNaN(curONH) || h > curONH) curONH = h;
				if (double.IsNaN(curONL) || l < curONL) curONL = l;
				curONVol += vol;
			}

			// ETH
			if (double.IsNaN(curETHH) || h > curETHH) curETHH = h;
			if (double.IsNaN(curETHL) || l < curETHL) curETHL = l;
			curETHVol += vol;

			// Weekly
			bool newWeek = (t.DayOfWeek == DayOfWeek.Monday && prevT.DayOfWeek != DayOfWeek.Monday && prevT.DayOfWeek != DayOfWeek.Sunday)
				|| (t.DayOfWeek == DayOfWeek.Sunday && CrossedTime(prevTod, tod, ETHOpenTime));
			if (newWeek && t.Date != curWeekStart)
			{
				if (!double.IsNaN(curWeekH) && !double.IsNaN(curWeekL))
				{
					weeklyRanges.Add(curWeekH - curWeekL);
					weeklyVolumes.Add(curWeekVol);
					if (weeklyRanges.Count > LookbackPeriod) { weeklyRanges.RemoveAt(0); weeklyVolumes.RemoveAt(0); }
				}
				curWeekH = curWeekL = double.NaN; curWeekVol = 0; curWeekStart = t.Date;
			}
			if (double.IsNaN(curWeekH) || h > curWeekH) curWeekH = h;
			if (double.IsNaN(curWeekL) || l < curWeekL) curWeekL = l;
			curWeekVol += vol;

			BuildDisplayRows();
		}

		private double SafeRange(double h, double l) => (!double.IsNaN(h) && !double.IsNaN(l)) ? h - l : 0;
		#endregion

		#region Build Display
		private void BuildDisplayRows()
		{
			displayRows.Clear();
			int n = Math.Min(history.Count, LookbackPeriod);
			if (n < 1) return;
			var recent = history.Skip(Math.Max(0, history.Count - n)).ToList();
			double ts = TickSize > 0 ? TickSize : 0.25;

			if (ShowDailyRangeStats)
			{
				displayRows.Add(KV("── Daily Ranges ──", ""));
				displayRows.Add(KV("Avg ON Range", Pts(recent.Average(s => s.ONRange), ts)));
				displayRows.Add(KV("Avg IB Range", Pts(recent.Average(s => s.IBRange), ts)));
				displayRows.Add(KV("Avg RTH Range", Pts(recent.Average(s => s.RTHRange), ts)));
				displayRows.Add(KV("Avg ETH Range", Pts(recent.Average(s => s.ETHRange), ts)));
				displayRows.Add(KV("Avg OR Range (" + (int)ORDuration + "m)", Pts(recent.Average(s => s.ORRange), ts)));

				// 30-min period averages
				if (NumPeriodsToShow > 0)
				{
					int maxPeriods = Math.Min(NumPeriodsToShow, 14);
					for (int p = 0; p < maxPeriods; p++)
					{
						var vals = recent.Where(s => s.PeriodRanges.Count > p).Select(s => s.PeriodRanges[p]).ToList();
						if (vals.Count > 0)
						{
							TimeSpan pStart = RTHOpenTime + TimeSpan.FromMinutes(p * 30);
							TimeSpan pEnd = pStart + TimeSpan.FromMinutes(30);
							displayRows.Add(KV($"{pStart:hh\\:mm}-{pEnd:hh\\:mm}", Pts(vals.Average(), ts)));
						}
					}
				}
			}

			if (ShowVAStats)
			{
				displayRows.Add(KV("── Value Area ──", ""));
				displayRows.Add(KV("Avg ON VA Width", Pts(recent.Average(s => s.ONVAWidth), ts)));
				displayRows.Add(KV("Avg RTH VA Width", Pts(recent.Average(s => s.RTHVAWidth), ts)));
				displayRows.Add(KV("Avg ETH VA Width", Pts(recent.Average(s => s.ETHVAWidth), ts)));
				var vaRatios = recent.Where(s => s.TotalRange > 0).Select(s => s.RTHVAWidth / s.TotalRange * 100).ToList();
				if (vaRatios.Count > 0) displayRows.Add(KV("Avg VA % of Range", vaRatios.Average().ToString("F1") + "%"));
			}

			if (ShowVolumeStats)
			{
				displayRows.Add(KV("── Volume ──", ""));
				displayRows.Add(KV("Avg ON Volume", FmtVol(recent.Average(s => s.ONVolume))));
				displayRows.Add(KV("Avg IB Volume", FmtVol(recent.Average(s => s.IBVolume))));
				displayRows.Add(KV("Avg RTH Volume", FmtVol(recent.Average(s => s.RTHVolume))));
				displayRows.Add(KV("Avg ETH Volume", FmtVol(recent.Average(s => s.ETHVolume))));

				if (ShowRelativeVolume && curRTHElapsedMin > 0)
				{
					var avgAtTime = recent.Where(s => s.CumVolByMinute.ContainsKey(curRTHElapsedMin))
						.Select(s => s.CumVolByMinute[curRTHElapsedMin]).ToList();
					if (avgAtTime.Count > 0)
					{
						double avgV = avgAtTime.Average();
						double rvol = avgV > 0 ? curRTHCumVol / avgV : 0;
						displayRows.Add(KV("Relative Volume", rvol.ToString("F2") + "x"));
					}
				}
			}

			if (ShowWeeklyStats && weeklyRanges.Count > 0)
			{
				displayRows.Add(KV("── Weekly ──", ""));
				displayRows.Add(KV("Avg Weekly Range", Pts(weeklyRanges.Average(), ts)));
				displayRows.Add(KV("Avg Weekly Volume", FmtVol(weeklyVolumes.Average())));
			}

			if (ShowHarmonicRotation)
			{
				double refRange = GetHarmonicRef(recent);
				double anchor = GetHarmonicAnchorPrice();
				if (!double.IsNaN(refRange) && refRange > 0 && !double.IsNaN(anchor))
				{
					displayRows.Add(KV("── Harmonics ──", ""));
					double[] mults = { 0.5, 1.0, 1.5, 2.0, 2.618 };
					foreach (double m in mults)
					{
						double up = anchor + refRange * m;
						double dn = anchor - refRange * m;
						string fp = Instrument != null ? Instrument.MasterInstrument.FormatPrice(up) : up.ToString("F2");
						string fpd = Instrument != null ? Instrument.MasterInstrument.FormatPrice(dn) : dn.ToString("F2");
						displayRows.Add(KV(m + "x", fpd + " / " + fp));
					}
				}
			}
		}

		private double GetHarmonicRef(List<SessionRecord> recent)
		{
			switch (HarmonicSource)
			{
				case MgiHarmonicSource.IBRange: return SafeRange(curIBH, curIBL);
				case MgiHarmonicSource.OvernightRange: return SafeRange(curONH, curONL);
				case MgiHarmonicSource.ORRange: return SafeRange(curORH, curORL);
				case MgiHarmonicSource.RTHRange: return SafeRange(curRTHH, curRTHL);
				default: return double.NaN;
			}
		}

		private double GetHarmonicAnchorPrice()
		{
			switch (HarmonicAnchor)
			{
				case MgiHarmonicAnchor.IBHigh: return curIBH;
				case MgiHarmonicAnchor.IBLow: return curIBL;
				case MgiHarmonicAnchor.ONHigh: return curONH;
				case MgiHarmonicAnchor.ONLow: return curONL;
				case MgiHarmonicAnchor.ORHigh: return curORH;
				case MgiHarmonicAnchor.ORLow: return curORL;
				default: return double.NaN;
			}
		}

		private KeyValuePair<string, string> KV(string k, string v) => new KeyValuePair<string, string>(k, v);
		private string Pts(double v, double ts) => ts > 0 ? (v / ts).ToString("F1") + " tks (" + v.ToString("F2") + " pts)" : v.ToString("F2");
		private string FmtVol(double v) => v >= 1000000 ? (v / 1000000.0).ToString("F2") + "M" : v >= 1000 ? (v / 1000.0).ToString("F1") + "K" : v.ToString("F0");
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl cc, ChartScale cs)
		{
			base.OnRender(cc, cs);
			if (cc == null || cs == null || displayRows == null || displayRows.Count == 0) return;
			EnsureDx();
			if (!dxValid) return;

			float pW = PanelWidth, rH = RowHeight, pad = PanelPadding;
			float totalH = pad * 2 + displayRows.Count * rH;
			float totalW = pW + pad * 2;

			float px, py;
			switch (PanelPosition)
			{
				case MgiPanelPosition.TopLeft: px = ChartPanel.X + 10; py = ChartPanel.Y + 10; break;
				case MgiPanelPosition.BottomLeft: px = ChartPanel.X + 10; py = ChartPanel.Y + ChartPanel.H - totalH - 10; break;
				case MgiPanelPosition.BottomRight: px = ChartPanel.X + ChartPanel.W - totalW - 10; py = ChartPanel.Y + ChartPanel.H - totalH - 10; break;
				default: px = ChartPanel.X + ChartPanel.W - totalW - 10; py = ChartPanel.Y + 10; break;
			}

			var oldAA = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

			// Background
			var bgRect = new RoundedRectangle { Rect = new RectangleF(px, py, totalW, totalH), RadiusX = 6, RadiusY = 6 };
			RenderTarget.FillRoundedRectangle(bgRect, dxBgBrush);
			RenderTarget.DrawRoundedRectangle(bgRect, dxBorderBrush, 1f);

			// Rows
			float curY = py + pad;
			float labelX = px + pad;
			float valueX = px + totalW * 0.55f;
			float valueW = totalW * 0.45f - pad;

			for (int i = 0; i < displayRows.Count; i++)
			{
				var row = displayRows[i];
				bool isHeader = row.Value == "" && row.Key.Contains("──");
				var fmt = isHeader ? dxHeaderFmt : dxBodyFmt;
				var brush = isHeader ? dxHeaderBrush : dxLabelBrush;

				if (isHeader)
				{
					var hRect = new RectangleF(labelX, curY, totalW - pad * 2, rH);
					RenderTarget.DrawText(row.Key, fmt, hRect, brush);
				}
				else
				{
					var lRect = new RectangleF(labelX, curY, valueX - labelX - 4, rH);
					RenderTarget.DrawText(row.Key, fmt, lRect, brush);
					var vRect = new RectangleF(valueX, curY, valueW, rH);
					RenderTarget.DrawText(row.Value, fmt, vRect, dxValueBrush);
				}
				curY += rH;
			}

			RenderTarget.AntialiasMode = oldAA;
		}
		#endregion

		#region DX Resources
		private Color4 ToC4(WpfBrush b, float a = 1f)
		{
			var c = (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * a);
		}

		private void EnsureDx()
		{
			if (dxValid || RenderTarget == null) return;
			try
			{
				dxBgBrush = new DxSolidBrush(RenderTarget, ToC4(PanelBgColor, PanelBgOpacity / 100f));
				dxHeaderBrush = new DxSolidBrush(RenderTarget, ToC4(HeaderColor));
				dxLabelBrush = new DxSolidBrush(RenderTarget, ToC4(LabelTextColor));
				dxValueBrush = new DxSolidBrush(RenderTarget, ToC4(ValueTextColor));
				dxBorderBrush = new DxSolidBrush(RenderTarget, ToC4(BorderColor, 0.5f));

				var dwf = NinjaTrader.Core.Globals.DirectWriteFactory;
				dxHeaderFmt = new SharpDX.DirectWrite.TextFormat(dwf, FontName, FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)HeaderFontSize)
				{ TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Center };
				dxBodyFmt = new SharpDX.DirectWrite.TextFormat(dwf, FontName, FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, (float)FontSize)
				{ TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Center };
				dxValid = true;
			}
			catch { dxValid = false; }
		}

		private void DisposeDx()
		{
			try { dxBgBrush?.Dispose(); dxHeaderBrush?.Dispose(); dxLabelBrush?.Dispose(); dxValueBrush?.Dispose(); dxBorderBrush?.Dispose(); dxHeaderFmt?.Dispose(); dxBodyFmt?.Dispose(); }
			catch { }
			dxBgBrush = dxHeaderBrush = dxLabelBrush = dxValueBrush = dxBorderBrush = null;
			dxHeaderFmt = dxBodyFmt = null; dxValid = false;
		}

		public override void OnRenderTargetChanged() { DisposeDx(); base.OnRenderTargetChanged(); }
		#endregion

		#region Properties
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name="RTH Open Time", Order=1, GroupName="01. Session Times")] public TimeSpan RTHOpenTime { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name="RTH Close Time", Order=2, GroupName="01. Session Times")] public TimeSpan RTHCloseTime { get; set; }
		[NinjaScriptProperty][PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		[Display(Name="ETH Open Time", Order=3, GroupName="01. Session Times")] public TimeSpan ETHOpenTime { get; set; }

		[Range(5,100)][Display(Name="Lookback Period", Description="Number of sessions for averaging", Order=1, GroupName="02. General")]
		public int LookbackPeriod { get; set; }
		[NinjaScriptProperty][Display(Name="OR Duration", Description="Opening range timeframe", Order=2, GroupName="02. General")]
		public MgiORDuration ORDuration { get; set; }

		// Row visibility
		[Display(Name="Show Daily Range Stats", Order=1, GroupName="03. Rows")] public bool ShowDailyRangeStats { get; set; }
		[Display(Name="Show VA Stats", Order=2, GroupName="03. Rows")] public bool ShowVAStats { get; set; }
		[Display(Name="Show Volume Stats", Order=3, GroupName="03. Rows")] public bool ShowVolumeStats { get; set; }
		[Display(Name="Show Relative Volume", Order=4, GroupName="03. Rows")] public bool ShowRelativeVolume { get; set; }
		[Display(Name="Show Weekly Stats", Order=5, GroupName="03. Rows")] public bool ShowWeeklyStats { get; set; }
		[Display(Name="Show Harmonic Rotation", Order=6, GroupName="03. Rows")] public bool ShowHarmonicRotation { get; set; }
		[Range(0,14)][Display(Name="30-min Periods to Show", Order=7, GroupName="03. Rows")] public int NumPeriodsToShow { get; set; }

		// Harmonic
		[Display(Name="Harmonic Reference", Description="Range used for harmonic calc", Order=1, GroupName="04. Harmonic Rotation")]
		public MgiHarmonicSource HarmonicSource { get; set; }
		[Display(Name="Harmonic Anchor", Description="Price anchor for harmonic projections", Order=2, GroupName="04. Harmonic Rotation")]
		public MgiHarmonicAnchor HarmonicAnchor { get; set; }

		// Display
		[Display(Name="Panel Position", Order=1, GroupName="05. Display")] public MgiPanelPosition PanelPosition { get; set; }
		[Range(150,500)][Display(Name="Panel Width", Order=2, GroupName="05. Display")] public int PanelWidth { get; set; }
		[Range(14,30)][Display(Name="Row Height", Order=3, GroupName="05. Display")] public int RowHeight { get; set; }
		[Range(2,30)][Display(Name="Panel Padding", Order=4, GroupName="05. Display")] public int PanelPadding { get; set; }
		[Range(0,100)][Display(Name="Background Opacity %", Order=5, GroupName="05. Display")] public int PanelBgOpacity { get; set; }

		// Colors
		[XmlIgnore][Display(Name="Background Color", Order=1, GroupName="06. Colors")] public WpfBrush PanelBgColor { get; set; }
		[Browsable(false)] public string PanelBgColorS { get { return Serialize.BrushToString(PanelBgColor); } set { PanelBgColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Header Color", Order=2, GroupName="06. Colors")] public WpfBrush HeaderColor { get; set; }
		[Browsable(false)] public string HeaderColorS { get { return Serialize.BrushToString(HeaderColor); } set { HeaderColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Label Text Color", Order=3, GroupName="06. Colors")] public WpfBrush LabelTextColor { get; set; }
		[Browsable(false)] public string LabelTextColorS { get { return Serialize.BrushToString(LabelTextColor); } set { LabelTextColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Value Text Color", Order=4, GroupName="06. Colors")] public WpfBrush ValueTextColor { get; set; }
		[Browsable(false)] public string ValueTextColorS { get { return Serialize.BrushToString(ValueTextColor); } set { ValueTextColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Border Color", Order=5, GroupName="06. Colors")] public WpfBrush BorderColor { get; set; }
		[Browsable(false)] public string BorderColorS { get { return Serialize.BrushToString(BorderColor); } set { BorderColor = Serialize.StringToBrush(value); } }

		// Font
		[NinjaScriptProperty][Display(Name="Font Name", Order=1, GroupName="07. Font")] public string FontName { get; set; }
		[Range(8,20)][Display(Name="Font Size", Order=2, GroupName="07. Font")] public int FontSize { get; set; }
		[Range(8,24)][Display(Name="Header Font Size", Order=3, GroupName="07. Font")] public int HeaderFontSize { get; set; }
		#endregion
	}
}
