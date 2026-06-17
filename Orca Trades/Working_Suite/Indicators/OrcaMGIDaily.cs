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
using DxBrush = SharpDX.Direct2D1.Brush;
using DxSolidBrush = SharpDX.Direct2D1.SolidColorBrush;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum MgiPlotStyle { Regular, Edge }
	public enum MgiORDuration { Sec30 = -1, Min1 = 1, Min5 = 5, Min15 = 15, Min30 = 30 }
	public enum MgiDashStyle { Solid, Dash, Dot, DashDot }

	/// <summary>
	/// Determines which price is treated as the Prior Day Close (PDC).
	/// Equities4PM  : 4:00 PM — equities market close, common benchmark
	/// CME415PM     : 4:15 PM — CME futures RTH official close
	/// Globex5PM    : 5:00 PM — last price before the 1-hour CME maintenance break
	/// </summary>
	public enum MgiPdcMode { Equities4PM, CME415PM, Globex5PM }
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaMgiFontFamilyConverter : StringConverter
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

			foreach (System.Windows.Media.FontFamily family in System.Windows.Media.Fonts.SystemFontFamilies)
			{
				string name = family != null ? family.Source : null;
				if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
					continue;

				fontNames.Add(name);
			}

			fontNames.Sort(StringComparer.CurrentCultureIgnoreCase);
			return new StandardValuesCollection(fontNames);
		}
	}

	public class OrcaMGIDaily : Indicator
	{
		#region Helper Classes
		private class VwapAccum
		{
			public double SumVol, SumPV, SumP2V;
			public void Add(double p, double v) { SumVol += v; SumPV += p * v; SumP2V += p * p * v; }
			public void Reset() { SumVol = 0; SumPV = 0; SumP2V = 0; }
			public double Value => SumVol > 0 ? SumPV / SumVol : double.NaN;
		}

		private class LevelSet
		{
			public double Open = double.NaN, High = double.NaN, Low = double.NaN, Close = double.NaN, Mid = double.NaN;
			public double VAH = double.NaN, VAL = double.NaN, POC = double.NaN;
			public Dictionary<double, double> VolByPrice = new Dictionary<double, double>();
			public VwapAccum Vwap = new VwapAccum();
			public void ResetPrices() { Open = High = Low = Close = Mid = VAH = VAL = POC = double.NaN; VolByPrice.Clear(); Vwap.Reset(); }
			public void UpdateHL(double h, double l, double c, double openPrice = double.NaN)
			{
				// If open is explicitly provided (crossing bar), capture it; else default to close
				if (double.IsNaN(Open))
					Open = double.IsNaN(openPrice) ? c : openPrice;
				if (double.IsNaN(High) || h > High) High = h;
				if (double.IsNaN(Low) || l < Low) Low = l;
				Close = c;
				if (!double.IsNaN(High) && !double.IsNaN(Low)) Mid = (High + Low) / 2.0;
			}
		}

		private class SessionInfo
		{
			public int RthOpenIdx = -1, OrEndIdx = -1, IbEndIdx = -1, EthOpenIdx = -1, TrueDailyOpenIdx = -1;
			public DateTime RthOpenTime = DateTime.MinValue, OrEndTime = DateTime.MinValue, IbEndTime = DateTime.MinValue, EthOpenTime = DateTime.MinValue, TrueDailyOpenTime = DateTime.MinValue;
			public DateTime Date;
		}
		#endregion

		#region Fields
		// Session data
		private LevelSet curRTH, curETH, priorRTH, priorETH, curWeek, priorWeek;
		private LevelSet overnight;
		private double trueDailyOpen = double.NaN;
		private DateTime trueDailyOpenDate = DateTime.MinValue;
		private double orHigh = double.NaN, orLow = double.NaN, orMid = double.NaN;
		private double ibHigh = double.NaN, ibLow = double.NaN, ibMid = double.NaN;
		private double halfGap = double.NaN;
		private bool orComplete, ibComplete;
		private DateTime rthOpenTime, rthCloseTime, ethOpenTime;
		private DateTime curSessionDate = DateTime.MinValue;
		private DateTime rangeSessionDate = DateTime.MinValue;
		private DateTime orHighTime = DateTime.MinValue, orLowTime = DateTime.MinValue, ibHighTime = DateTime.MinValue, ibLowTime = DateTime.MinValue;
		private DateTime curRthHighTime = DateTime.MinValue, curRthLowTime = DateTime.MinValue;
		private DateTime curEthHighTime = DateTime.MinValue, curEthLowTime = DateTime.MinValue;
		private DateTime curWeekOpenTime = DateTime.MinValue, curWeekHighTime = DateTime.MinValue, curWeekLowTime = DateTime.MinValue;
		private DateTime overnightHighTime = DateTime.MinValue, overnightLowTime = DateTime.MinValue;
		private int orHighIdx = -1, orLowIdx = -1, ibHighIdx = -1, ibLowIdx = -1;
		private int curRthHighIdx = -1, curRthLowIdx = -1, curEthHighIdx = -1, curEthLowIdx = -1, overnightHighIdx = -1, overnightLowIdx = -1;
		private int curWeekOpenIdx = -1, curWeekHighIdx = -1, curWeekLowIdx = -1;
		private double curRthAnchorHigh = double.NaN, curRthAnchorLow = double.NaN;
		private double curEthAnchorHigh = double.NaN, curEthAnchorLow = double.NaN;
		private double overnightAnchorHigh = double.NaN, overnightAnchorLow = double.NaN;
		private DateTime primaryEthAnchorDate = DateTime.MinValue, primaryRthAnchorDate = DateTime.MinValue, primaryOvernightAnchorDate = DateTime.MinValue;
		private bool inRTH, inETH;
		private int orDurationSec, orBarCount;
		private DateTime orStartTime, ibEndTime;

		// Prior day VA (stored separately for stability)
		private double priorRTH_VAH = double.NaN, priorRTH_VAL = double.NaN, priorRTH_POC = double.NaN;
		private double priorETH_VAH = double.NaN, priorETH_VAL = double.NaN, priorETH_POC = double.NaN;
		private double priorWeek_VAH = double.NaN, priorWeek_VAL = double.NaN, priorWeek_POC = double.NaN;

		// DX resources
		private DxSolidBrush[] dxBrushes;
		private SharpDX.Direct2D1.StrokeStyle[] dxStrokes;
		private SharpDX.DirectWrite.TextFormat dxLabelFormat;
		private DxSolidBrush dxLabelBrush, dxRegionBrush;
		private bool dxValid;
		private IntPtr dxResourceRenderTarget = IntPtr.Zero;
		private DateTime lastRenderSkipUtc = DateTime.MinValue;

		// Level rendering cache
		private struct LevelInfo
		{
			public double Price;
			public string Label;
			public int BrushIdx;
			public int StrokeIdx;
			public int Width;
			public DateTime StartTime;
			public int StartIdx;
			public bool Enabled;
		}
		private LevelInfo[] levelCache;
		private const int LVL_COUNT = 82;

		// Level indices
		private const int L_ONH = 0, L_ONL = 1, L_ONMID = 2;
		private const int L_ONVAH = 3, L_ONVAL = 4, L_ONPOC = 5;
		private const int L_ORH = 6, L_ORL = 7, L_ORMID = 8;
		private const int L_IBH = 9, L_IBL = 10, L_IBMID = 11;
		private const int L_CRTH_O = 12, L_CRTH_H = 13, L_CRTH_L = 14;
		private const int L_CETH_O = 15, L_CETH_H = 16, L_CETH_L = 17;
		private const int L_PDH = 18, L_PDL = 19, L_PDC = 20, L_PDO = 21, L_PDMID = 22;
		private const int L_PETH_H = 23, L_PETH_L = 24, L_PETH_C = 25;
		private const int L_CRVAH = 26, L_CRVAL = 27, L_CRPOC = 28;
		private const int L_CEVAH = 29, L_CEVAL = 30, L_CEPOC = 31;
		private const int L_PRVAH = 32, L_PRVAL = 33, L_PRPOC = 34;
		private const int L_PEVAH = 35, L_PEVAL = 36, L_PEPOC = 37;
		private const int L_RVWAP = 38, L_EVWAP = 39;
		private const int L_HGAP = 40;
		private const int L_RTHOPEN_MARK = 41, L_RTHCLOSE_MARK = 42;
		private const int L_IBQ25 = 43, L_IBQ50 = 44, L_IBQ75 = 45;
		private const int L_IBUP25 = 46, L_IBUP50 = 47, L_IBUP75 = 48, L_IBUP100 = 49, L_IBUP150 = 50;
		private const int L_IBUP200 = 51, L_IBUP250 = 52, L_IBUP300 = 53, L_IBUP350 = 54, L_IBUP400 = 55;
		private const int L_IBDN25 = 56, L_IBDN50 = 57, L_IBDN75 = 58, L_IBDN100 = 59, L_IBDN150 = 60;
		private const int L_IBDN200 = 61, L_IBDN250 = 62, L_IBDN300 = 63, L_IBDN350 = 64, L_IBDN400 = 65;
		private const int L_TDO = 66;
		private const int L_CW_O = 67, L_CW_H = 68, L_CW_L = 69, L_CW_MID = 70;
		private const int L_PW_O = 71, L_PW_H = 72, L_PW_L = 73, L_PW_C = 74, L_PW_MID = 75;
		private const int L_CW_VAH = 76, L_CW_VAL = 77, L_CW_POC = 78;
		private const int L_PW_VAH = 79, L_PW_VAL = 80, L_PW_POC = 81;

		private List<SessionInfo> sessionHistory = new List<SessionInfo>();
		private SessionInfo curSessionInfo;
		private int lastBarIdx = -1;
		private Dictionary<DateTime, LevelSet> rthHistoryByDate;

		private Series<double> rthMidSeries, ethMidSeries;

		// Translates PriorDayCloseMode enum to the TimeSpan used for the PDC capture boundary.
		// Independent of RTHCloseTime which controls the RTH session window for H/L/VA tracking.
		private TimeSpan PdcTimeSpan
		{
			get
			{
				if (UseLatestCloseForPdc)
					return new TimeSpan(17, 0, 0);

				switch (PriorDayCloseMode)
				{
					case MgiPdcMode.Equities4PM: return new TimeSpan(16,  0, 0);
					case MgiPdcMode.CME415PM:    return new TimeSpan(16, 15, 0);
					case MgiPdcMode.Globex5PM:   return new TimeSpan(17,  0, 0);
					default:                     return new TimeSpan(16, 15, 0);
				}
			}
		}
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca MGI Daily";
				Description = "Plots structural market levels from current and prior daily sessions including overnight range, value areas, opening range, initial balance, VWAP, and half gap.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;

				// Session times (Eastern)
				RTHOpenTime = new TimeSpan(9, 30, 0);
				RTHCloseTime = new TimeSpan(16, 15, 0);
				ETHOpenTime = new TimeSpan(18, 0, 0);

				// Toggles — all on by default
				ShowONRange = true; ShowONVA = true; ShowOR = true; ShowIB = true;
				ShowCurRTH = true; ShowCurETH = false; ShowDailyOpen = true; ShowTrueDailyOpen = true; ShowPriorRTH = true; ShowPriorETH = false;
				ShowCurRTHVA = true; ShowCurETHVA = false; ShowPriorRTHVA = true; ShowPriorETHVA = false;
				ShowRTHVwap = true; ShowETHVwap = false; ShowHalfGap = true;
				ShowCurrentWeek = false; ShowPriorWeek = false; ShowCurrentWeekVA = false; ShowPriorWeekVA = false;
				ShowSessionMarkers = true; ShowLabels = true;
				ORDuration = MgiORDuration.Min30;
				MgiStyle = MgiPlotStyle.Regular;
				ValueAreaPct = 70;

				// Region fills
				ONRegionOpacity = 15; IBRegionOpacity = 10;

				// Label settings
				LabelFontName = "Segoe UI"; LabelFontSize = 10; LabelXOffset = 20; LabelOpacity = 100;

				// Colors
				ONColor = WpfBrushes.SteelBlue; ONVAColor = WpfBrushes.SlateBlue;
				ORColor = WpfBrushes.Goldenrod; IBColor = WpfBrushes.MediumSeaGreen;
				IBExtensionUpColor = WpfBrushes.MediumAquamarine;
				IBExtensionDownColor = WpfBrushes.IndianRed;
				IBInnerLevelColor = WpfBrushes.NavajoWhite;
				CurRTHColor = WpfBrushes.White; CurETHColor = WpfBrushes.LightGray;
				PriorRTHColor = WpfBrushes.SandyBrown; PriorETHColor = WpfBrushes.DarkGray;
				CurRTHVAColor = WpfBrushes.CornflowerBlue; CurETHVAColor = WpfBrushes.MediumSlateBlue;
				PriorRTHVAColor = WpfBrushes.Peru; PriorETHVAColor = WpfBrushes.RosyBrown;
				CurrentWeekColor = WpfBrushes.MediumPurple; PriorWeekColor = WpfBrushes.DarkSeaGreen;
				CurrentWeekVAColor = WpfBrushes.MediumOrchid; PriorWeekVAColor = WpfBrushes.SeaGreen;
				RTHVwapColor = WpfBrushes.Orchid; ETHVwapColor = WpfBrushes.MediumOrchid;
				HalfGapColor = WpfBrushes.IndianRed;
				TrueDailyOpenColor = WpfBrushes.DeepSkyBlue;
				SessionMarkerColor = WpfBrushes.DimGray;
				LabelColor = WpfBrushes.WhiteSmoke;
				SessionLineColor = WpfBrushes.SkyBlue;

				// Line settings
				MainLineWidth = 2; SecondaryLineWidth = 1; VALineWidth = 1;
				MainDashStyle = MgiDashStyle.Solid; VADashStyle = MgiDashStyle.Dash;
				LineOpacity = 100;
				DrawBehindCandles = false;
				EdgeLineLength = 160;
				ShowIBExtensions = true;
				ShowIBFullExtensions = true;
				ShowIBHalfExtensions = true;
				ShowIBQuarterExtensions = true;

				ShowSessionLine = false;
				AbbreviateLabels = true;
				ShowPriceInLabel = false;

				UseLatestCloseForPdc = true;
				PriorDayCloseMode = MgiPdcMode.Globex5PM; // default: latest close before the 5 PM maintenance break

				ShowRthMid = true; RthMidColor = WpfBrushes.DarkGoldenrod;
				ShowEthMid = true; EthMidColor = WpfBrushes.DarkSlateBlue;

				// Default custom labels (users can rename any of these)
				LblONH = "ONH"; LblONL = "ONL"; LblONM = "ONM";
				LblOVAH = "OVAH"; LblOVAL = "OVAL"; LblOPOC = "OPOC";
				LblORH = "ORH"; LblORL = "ORL"; LblORM = "ORM";
				LblIBH = "IBH"; LblIBL = "IBL"; LblIBM = "IBM";
				LblRTHO = "RTH Open"; LblRTHH = "RTHH"; LblRTHL = "RTHL";
				LblETHO = "Daily Open"; LblETHH = "HOD"; LblETHL = "LOD";
				LblTDO = "TDO";
				LblPDH = "RTH PDH"; LblPDL = "RTH PDL"; LblPDC = "RTH PDC"; LblPDO = "RTH PDO"; LblPDM = "RTH PDM";
				LblPEH = "PDH"; LblPEL = "PDL"; LblPEC = "PDC";
				LblRVAH = "RVAH"; LblRVAL = "RVAL"; LblRPOC = "RPOC";
				LblEVAH = "EVAH"; LblEVAL = "EVAL"; LblEPOC = "EPOC";
				LblPRVAH = "pRVAH"; LblPRVAL = "pRVAL"; LblPRPOC = "pRPOC";
				LblPEVAH = "PDVAH"; LblPEVAL = "PDVAL"; LblPEPOC = "PDPOC";
				LblCWO = "Weekly Open"; LblCWH = "Weekly High"; LblCWL = "Weekly Low"; LblCWM = "Weekly Mid";
				LblPWO = "Prior Weekly Open"; LblPWH = "Prior Weekly High"; LblPWL = "Prior Weekly Low"; LblPWC = "Prior Weekly Close"; LblPWM = "Prior Weekly Mid";
				LblCWVAH = "Weekly VAH"; LblCWVAL = "Weekly VAL"; LblCWPOC = "Weekly POC";
				LblPWVAH = "Prior Weekly VAH"; LblPWVAL = "Prior Weekly VAL"; LblPWPOC = "Prior Weekly POC";
				LblVWAP = "VWAP"; LblEVWAP = "eVWAP"; LblHGAP = "½GAP";

				AddPlot(new Stroke(WpfBrushes.Transparent, 1), PlotStyle.Line, "MGIDummy");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Minute, 1);
				AddDataSeries(BarsPeriodType.Second, 30);
			}
			else if (State == State.DataLoaded)
			{
				NormalizeLegacyLabels();
				curRTH = new LevelSet(); curETH = new LevelSet(); curWeek = new LevelSet();
				priorRTH = new LevelSet(); priorETH = new LevelSet(); priorWeek = new LevelSet();
				overnight = new LevelSet();
				rthHistoryByDate = new Dictionary<DateTime, LevelSet>();
				levelCache = new LevelInfo[LVL_COUNT];
				orComplete = false; ibComplete = false;

				rthMidSeries = new Series<double>(this);
				ethMidSeries = new Series<double>(this);
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

		private void NormalizeLegacyLabels()
		{
			if (LblETHH == "ETHH") LblETHH = "HOD";
			if (LblETHL == "ETHL") LblETHL = "LOD";
			if (LblETHO == "ETH.Open" || LblETHO == "ETHO") LblETHO = "Daily Open";
			if (LblRTHO == "RTHO") LblRTHO = "RTH Open";

			if (LblPDH == "PDH" || LblPDH == "PRTHH") LblPDH = "RTH PDH";
			if (LblPDL == "PDL" || LblPDL == "PRTHL") LblPDL = "RTH PDL";
			if (LblPDC == "PDC" || LblPDC == "PRTHC") LblPDC = "RTH PDC";
			if (LblPDO == "PDO" || LblPDO == "PRTHO") LblPDO = "RTH PDO";
			if (LblPDM == "PDM" || LblPDM == "PRTHM") LblPDM = "RTH PDM";

			if (LblPEH == "PEH") LblPEH = "PDH";
			if (LblPEL == "PEL") LblPEL = "PDL";
			if (LblPEC == "PEC") LblPEC = "PDC";

			if (LblPEVAH == "pEVAH") LblPEVAH = "PDVAH";
			if (LblPEVAL == "pEVAL") LblPEVAL = "PDVAL";
			if (LblPEPOC == "pEPOC") LblPEPOC = "PDPOC";
		}

		#region Value Area Calculation
		private void CalcVA(LevelSet ls)
		{
			if (ls.VolByPrice.Count < 2) return;
			var sorted = ls.VolByPrice.OrderByDescending(kv => kv.Value).ToList();
			ls.POC = sorted[0].Key;
			double totalVol = sorted.Sum(kv => kv.Value);
			if (totalVol <= 0) return;
			double target = totalVol * (ValueAreaPct / 100.0);

			var prices = ls.VolByPrice.Keys.OrderBy(p => p).ToList();
			int pocIdx = prices.IndexOf(ls.POC);
			if (pocIdx < 0) { pocIdx = prices.Count / 2; ls.POC = prices[pocIdx]; }

			double accum = ls.VolByPrice[ls.POC];
			int lo = pocIdx, hi = pocIdx;
			while (accum < target && (lo > 0 || hi < prices.Count - 1))
			{
				double vBelow = lo > 0 ? ls.VolByPrice[prices[lo - 1]] : 0;
				double vAbove = hi < prices.Count - 1 ? ls.VolByPrice[prices[hi + 1]] : 0;
				if (lo <= 0) { hi++; accum += vAbove; }
				else if (hi >= prices.Count - 1) { lo--; accum += vBelow; }
				else if (vAbove >= vBelow) { hi++; accum += vAbove; }
				else { lo--; accum += vBelow; }
			}
			ls.VAL = prices[lo]; ls.VAH = prices[hi];
		}

		private void DistributeVolume(LevelSet ls, double high, double low, double vol)
		{
			if (vol <= 0 || high <= low) return;
			double ts = TickSize;
			int ticks = Math.Max(1, (int)Math.Round((high - low) / ts) + 1);
			double perTick = vol / ticks;
			for (int i = 0; i < ticks; i++)
			{
				double p = Math.Round((low + i * ts) / ts) * ts;
				if (ls.VolByPrice.ContainsKey(p)) ls.VolByPrice[p] += perTick;
				else ls.VolByPrice[p] = perTick;
			}
		}
		#endregion

		#region Session Detection & OnBarUpdate
		private bool IsInTimeWindow(TimeSpan barTime, TimeSpan start, TimeSpan end)
		{
			// Use > start so we don't grab the prior session's closing bar,
			// and <= end so we DO include bars timestamped exactly at the close.
			if (start < end) return barTime > start && barTime <= end;
			return barTime > start || barTime <= end;
		}

		private bool IsInTimeWindowIncludingStart(TimeSpan barTime, TimeSpan start, TimeSpan end)
		{
			if (start < end) return barTime >= start && barTime <= end;
			return barTime >= start || barTime <= end;
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 0)
			{
				if (CurrentBars[0] >= 0)
				{
					UpdatePrimaryOpeningRanges(Times[0][0], Highs[0][0], Lows[0][0], CurrentBars[0]);
					UpdatePrimaryLevelAnchors(Times[0][0], Highs[0][0], Lows[0][0], CurrentBars[0]);
					UpdateDisplaySeries();
					BuildLevelCache();
				}
				return;
			}
			if (BarsInProgress == 2)
			{
				if (CurrentBars.Length > 2 && CurrentBars[2] >= 0)
				{
					UpdateThirtySecondOpeningRange(Times[2][0], Highs[2][0], Lows[2][0]);
					BuildLevelCache();
				}
				return;
			}
			if (BarsInProgress != 1)
				return;

			if (CurrentBars[1] < 1) return;
			int barIndex = CurrentBars[1];
			DateTime t = Times[1][0];
			TimeSpan tod = t.TimeOfDay;
			DateTime prevT = Times[1][1];
			TimeSpan prevTod = prevT.TimeOfDay;
			double o = Opens[1][0], h = Highs[1][0], l = Lows[1][0], c = Closes[1][0], vol = Volumes[1][0];
			double typPrice = (h + l + c) / 3.0;
			bool isRthBar = IsInTimeWindow(tod, RTHOpenTime, RTHCloseTime);
			bool isOvernight = IsInTimeWindow(tod, ETHOpenTime, RTHOpenTime);
			UpdateWeeklyLevels(t, o, h, l, c, typPrice, vol);

			// Detect RTH open crossing
			bool rthCrossed = CrossedSessionOpenTime(prevTod, tod, RTHOpenTime);
			bool ethCrossed = CrossedSessionOpenTime(prevTod, tod, ETHOpenTime);
			bool trueDailyOpenCrossed = CrossedSessionOpenTime(prevTod, tod, TimeSpan.Zero) && tod <= RTHOpenTime;

			if (ethCrossed)
			{
				DateTime ethAnchorTime = GetSessionOpenDateTime(t, ETHOpenTime);
				// At Globex 18:00 boundary, also update priorRTH so PDH reflects today's completed RTH.
				CopyRthToPrior();

				// Snapshot the full-day ETH into priorETH BEFORE resetting
				CopyEthToPrior();

				// Reset curETH for the new full-day session starting now
				curETH.ResetPrices();
				overnight.ResetPrices();
				ResetEthLevelAnchors();
				// Capture exact open price from the crossing bar
				UpdateLevelSetWithAnchors(curETH, h, l, c, o, t, ref curEthHighTime, ref curEthLowTime);
				inETH = true;
				curSessionInfo = GetOrCreateSessionInfo(GetEthTradingDate(ethAnchorTime));
				curSessionInfo.EthOpenTime = ethAnchorTime;
			}

			if (trueDailyOpenCrossed)
			{
				DateTime trueDailyAnchorTime = t.Date;
				trueDailyOpen = o;
				trueDailyOpenDate = trueDailyAnchorTime.Date;
				curSessionInfo = GetOrCreateSessionInfo(trueDailyAnchorTime.Date);
				curSessionInfo.TrueDailyOpenTime = trueDailyAnchorTime;
			}

			if (rthCrossed)
			{
				DateTime rthAnchorTime = GetSessionOpenDateTime(t, RTHOpenTime);
				// Snapshot RTH into priorRTH
				CopyRthToPrior();

				// Reset RTH for the new session
				curRTH.ResetPrices();
				curRthHighTime = curRthLowTime = DateTime.MinValue;
				curRthHighIdx = curRthLowIdx = -1;
				curRthAnchorHigh = curRthAnchorLow = double.NaN;
				BeginRthRangeSession(rthAnchorTime, -1);
				halfGap = double.NaN;
				inRTH = true;
				curSessionDate = rthAnchorTime.Date;

				// Cache Session Info
				curSessionInfo = GetOrCreateSessionInfo(rthAnchorTime.Date);
				curSessionInfo.RthOpenTime = rthAnchorTime;

				// Seed the new curRTH from the first bar whose timestamp is after RTHOpenTime.
				// Time-based NinjaTrader bars are stamped at the close, so the bar stamped
				// exactly at RTHOpenTime still belongs to the prior window.
				UpdateLevelSetWithAnchors(curRTH, h, l, c, o, t, ref curRthHighTime, ref curRthLowTime);
				curRTH.Vwap.Add(typPrice, vol);
				DistributeVolume(curRTH, h, l, vol);

				// Half gap calc
				if (!double.IsNaN(priorRTH.Close))
				{
					double gap = o - priorRTH.Close;
					if (Math.Abs(gap) > TickSize * 2)
						halfGap = priorRTH.Close + gap * 0.5;
				}
			}

			// Determine session state
			inRTH = isRthBar;

			UpdateRthHistory(t, tod, o, h, l, c, typPrice, vol, isRthBar);
			RefreshPriorRthFromHistory(tod >= ETHOpenTime ? t.Date.AddDays(1) : t.Date);

			// PDC uses the latest completed bar close up to the configured close boundary.
			// Do not wait for a crossing bar: range/tick charts may not print exactly at
			// the boundary, and the first bar after it can be materially wrong.
			if (IsInTimeWindowIncludingStart(tod, RTHOpenTime, PdcTimeSpan))
				curRTH.Close = c;

			// Update current levels
			if (inRTH && !rthCrossed)  // rthCrossed bar is already handled above
			{
				UpdateLevelSetWithAnchors(curRTH, h, l, c, double.NaN, t, ref curRthHighTime, ref curRthLowTime);
				curRTH.Vwap.Add(typPrice, vol);
				DistributeVolume(curRTH, h, l, vol);

				// OR/IB ranges are collected from the primary chart bars so the
				// visible chart and the levels use the same candle boundaries.
			}

			// ETH full-day tracking (already updated on ethCrossed above; don't double-count)
			if (!ethCrossed)
			{
				UpdateLevelSetWithAnchors(curETH, h, l, c, double.NaN, t, ref curEthHighTime, ref curEthLowTime);
				curETH.Vwap.Add(typPrice, vol);
				DistributeVolume(curETH, h, l, vol);
			}
			else
			{
				// Already called UpdateHL above for the crossing bar; still add vol
				curETH.Vwap.Add(typPrice, vol);
				DistributeVolume(curETH, h, l, vol);
			}

			// Overnight tracking (ETH open through RTH open). Once RTH begins, freeze these levels for the day.
			if ((isOvernight || ethCrossed) && !rthCrossed)
			{
				UpdateLevelSetWithAnchors(overnight, h, l, c, ethCrossed ? o : double.NaN, t, ref overnightHighTime, ref overnightLowTime);
				DistributeVolume(overnight, h, l, vol);
			}

			// Recalc value areas periodically
			if (barIndex != lastBarIdx)
			{
				lastBarIdx = barIndex;
				if (inRTH && curRTH.VolByPrice.Count > 2) CalcVA(curRTH);
				if (curETH.VolByPrice.Count > 2) CalcVA(curETH);
				if ((isOvernight || rthCrossed) && overnight.VolByPrice.Count > 2) CalcVA(overnight);
			}

			// Build level cache for rendering
			BuildLevelCache();
		}

		private void BeginRthRangeSession(DateTime rthAnchorTime, int primaryIdx)
		{
			if (rangeSessionDate != rthAnchorTime.Date)
			{
				rangeSessionDate = rthAnchorTime.Date;
				orHigh = orLow = orMid = ibHigh = ibLow = ibMid = double.NaN;
				orComplete = false;
				ibComplete = false;
				orStartTime = rthAnchorTime;
				ibEndTime = rthAnchorTime + TimeSpan.FromMinutes(60);
				orHighTime = orLowTime = ibHighTime = ibLowTime = DateTime.MinValue;
				orHighIdx = orLowIdx = ibHighIdx = ibLowIdx = -1;
			}

			curSessionInfo = GetOrCreateSessionInfo(rthAnchorTime.Date);
			curSessionInfo.RthOpenTime = rthAnchorTime;
			if (primaryIdx >= 0 && curSessionInfo.RthOpenIdx < 0)
				curSessionInfo.RthOpenIdx = primaryIdx;
		}

		private void UpdateLevelSetWithAnchors(LevelSet levelSet, double high, double low, double close, double openPrice, DateTime time, ref DateTime highTime, ref DateTime lowTime)
		{
			if (levelSet == null)
				return;

			double oldHigh = levelSet.High;
			double oldLow = levelSet.Low;
			levelSet.UpdateHL(high, low, close, openPrice);

			if (!double.IsNaN(high) && (double.IsNaN(oldHigh) || high > oldHigh))
				highTime = time;
			if (!double.IsNaN(low) && (double.IsNaN(oldLow) || low < oldLow))
				lowTime = time;
		}

		private void UpdatePrimaryLevelAnchors(DateTime time, double high, double low, int primaryIdx)
		{
			if (double.IsNaN(high) || double.IsNaN(low))
				return;

			TimeSpan tod = time.TimeOfDay;
			DateTime ethAnchorTime = GetSessionOpenDateTime(time, ETHOpenTime);
			DateTime ethAnchorDate = ethAnchorTime.Date;
			if (primaryEthAnchorDate != ethAnchorDate)
			{
				primaryEthAnchorDate = ethAnchorDate;
				ResetEthLevelAnchors();
			}

			DateTime ethTradingDate = GetEthTradingDate(ethAnchorTime);
			SessionInfo ethInfo = GetOrCreateSessionInfo(ethTradingDate);
			if (CurrentBars[0] > 0 && CrossedSessionOpenTime(Times[0][1].TimeOfDay, tod, ETHOpenTime))
			{
				ethInfo.EthOpenTime = ethAnchorTime;
				ethInfo.EthOpenIdx = primaryIdx;
				overnightHighIdx = overnightLowIdx = -1;
				overnightAnchorHigh = overnightAnchorLow = double.NaN;
				overnightHighTime = overnightLowTime = DateTime.MinValue;
			}

			UpdateAnchorPair(high, low, primaryIdx, ref curEthAnchorHigh, ref curEthHighIdx, ref curEthAnchorLow, ref curEthLowIdx);

			bool isOvernight = IsInTimeWindow(tod, ETHOpenTime, RTHOpenTime);
			if (isOvernight)
			{
				if (primaryOvernightAnchorDate != ethAnchorDate)
				{
					primaryOvernightAnchorDate = ethAnchorDate;
					overnightHighIdx = overnightLowIdx = -1;
					overnightAnchorHigh = overnightAnchorLow = double.NaN;
					overnightHighTime = overnightLowTime = DateTime.MinValue;
				}
				UpdateAnchorPair(high, low, primaryIdx, ref overnightAnchorHigh, ref overnightHighIdx, ref overnightAnchorLow, ref overnightLowIdx);
			}

			bool isRth = IsInTimeWindow(tod, RTHOpenTime, RTHCloseTime);
			if (isRth)
			{
				DateTime rthAnchorTime = GetSessionOpenDateTime(time, RTHOpenTime);
				if (primaryRthAnchorDate != rthAnchorTime.Date)
				{
					primaryRthAnchorDate = rthAnchorTime.Date;
					curRthHighIdx = curRthLowIdx = -1;
					curRthAnchorHigh = curRthAnchorLow = double.NaN;
					curRthHighTime = curRthLowTime = DateTime.MinValue;
				}

				BeginRthRangeSession(rthAnchorTime, primaryIdx);
				UpdateAnchorPair(high, low, primaryIdx, ref curRthAnchorHigh, ref curRthHighIdx, ref curRthAnchorLow, ref curRthLowIdx);
			}

			if (CurrentBars[0] > 0)
			{
				TimeSpan prevTod = Times[0][1].TimeOfDay;
				if (CrossedSessionOpenTime(prevTod, tod, TimeSpan.Zero) && tod <= RTHOpenTime)
				{
					SessionInfo midnightInfo = GetOrCreateSessionInfo(time.Date);
					midnightInfo.TrueDailyOpenTime = time.Date;
					midnightInfo.TrueDailyOpenIdx = primaryIdx;
				}
			}
		}

		private void ResetEthLevelAnchors()
		{
			curEthHighIdx = curEthLowIdx = -1;
			curEthAnchorHigh = curEthAnchorLow = double.NaN;
			curEthHighTime = curEthLowTime = DateTime.MinValue;
			overnightHighIdx = overnightLowIdx = -1;
			overnightAnchorHigh = overnightAnchorLow = double.NaN;
			overnightHighTime = overnightLowTime = DateTime.MinValue;
		}

		private void UpdateAnchorPair(double high, double low, int primaryIdx, ref double anchorHigh, ref int highIdx, ref double anchorLow, ref int lowIdx)
		{
			if (!double.IsNaN(high) && (double.IsNaN(anchorHigh) || high > anchorHigh))
			{
				anchorHigh = high;
				highIdx = primaryIdx;
			}
			if (!double.IsNaN(low) && (double.IsNaN(anchorLow) || low < anchorLow))
			{
				anchorLow = low;
				lowIdx = primaryIdx;
			}
		}

		private void UpdatePrimaryOpeningRanges(DateTime time, double high, double low, int primaryIdx)
		{
			if (double.IsNaN(high) || double.IsNaN(low))
				return;

			TimeSpan tod = time.TimeOfDay;
			if (!IsInTimeWindow(tod, RTHOpenTime, RTHCloseTime))
				return;

			DateTime rthAnchorTime = GetSessionOpenDateTime(time, RTHOpenTime);
			BeginRthRangeSession(rthAnchorTime, primaryIdx);

			TimeSpan orEnd = RTHOpenTime + GetORDurationTimeSpan();
			if (!orComplete && ORDuration != MgiORDuration.Sec30)
			{
				if (IsInOpeningWindow(tod, RTHOpenTime, orEnd))
					UpdateOrRange(time, high, low, primaryIdx);
				if (tod >= orEnd)
				{
					if (curSessionInfo != null)
					{
						curSessionInfo.OrEndIdx = primaryIdx;
						curSessionInfo.OrEndTime = rthAnchorTime + GetORDurationTimeSpan();
					}
					orComplete = true;
				}
			}

			TimeSpan ibEnd = RTHOpenTime + TimeSpan.FromMinutes(60);
			if (!ibComplete)
			{
				if (IsInOpeningWindow(tod, RTHOpenTime, ibEnd))
					UpdateIbRange(time, high, low, primaryIdx);
				if (tod >= ibEnd)
				{
					if (curSessionInfo != null)
					{
						curSessionInfo.IbEndIdx = primaryIdx;
						curSessionInfo.IbEndTime = rthAnchorTime + TimeSpan.FromMinutes(60);
					}
					ibComplete = true;
				}
			}
		}

		private void UpdateThirtySecondOpeningRange(DateTime time, double high, double low)
		{
			if (ORDuration != MgiORDuration.Sec30 || double.IsNaN(high) || double.IsNaN(low))
				return;

			TimeSpan tod = time.TimeOfDay;
			if (!IsInTimeWindow(tod, RTHOpenTime, RTHCloseTime))
				return;

			DateTime rthAnchorTime = GetSessionOpenDateTime(time, RTHOpenTime);
			BeginRthRangeSession(rthAnchorTime, -1);

			TimeSpan orEnd = RTHOpenTime + GetORDurationTimeSpan();
			if (orComplete)
				return;

			if (IsInOpeningWindow(tod, RTHOpenTime, orEnd))
				UpdateOrRange(time, high, low, -1);

			if (tod >= orEnd)
			{
				if (curSessionInfo != null)
				{
					curSessionInfo.OrEndIdx = -1;
					curSessionInfo.OrEndTime = rthAnchorTime + GetORDurationTimeSpan();
				}
				orComplete = true;
			}
		}

		private bool UpdateOrRange(DateTime time, double high, double low, int primaryIdx)
		{
			bool changed = false;
			if (double.IsNaN(orHigh) || high > orHigh)
			{
				orHigh = high;
				orHighTime = time;
				orHighIdx = primaryIdx;
				changed = true;
			}
			if (double.IsNaN(orLow) || low < orLow)
			{
				orLow = low;
				orLowTime = time;
				orLowIdx = primaryIdx;
				changed = true;
			}
			if (!double.IsNaN(orHigh) && !double.IsNaN(orLow))
				orMid = (orHigh + orLow) / 2.0;

			return changed;
		}

		private bool UpdateIbRange(DateTime time, double high, double low, int primaryIdx)
		{
			if (rangeSessionDate == DateTime.MinValue || time.Date != rangeSessionDate.Date)
				return false;
			if (double.IsNaN(high) || double.IsNaN(low))
				return false;

			TimeSpan tod = time.TimeOfDay;
			TimeSpan ibEnd = RTHOpenTime + TimeSpan.FromMinutes(60);
			if (!IsInOpeningWindow(tod, RTHOpenTime, ibEnd))
				return false;

			bool changed = false;
			if (double.IsNaN(ibHigh) || high > ibHigh)
			{
				ibHigh = high;
				ibHighTime = time;
				ibHighIdx = primaryIdx;
				changed = true;
			}
			if (double.IsNaN(ibLow) || low < ibLow)
			{
				ibLow = low;
				ibLowTime = time;
				ibLowIdx = primaryIdx;
				changed = true;
			}
			if (!double.IsNaN(ibHigh) && !double.IsNaN(ibLow))
				ibMid = (ibHigh + ibLow) / 2.0;

			return changed;
		}

		private TimeSpan GetORDurationTimeSpan()
		{
			return ORDuration == MgiORDuration.Sec30
				? TimeSpan.FromSeconds(30)
				: TimeSpan.FromMinutes((int)ORDuration);
		}

		private bool IsInOpeningWindow(TimeSpan barTime, TimeSpan start, TimeSpan end)
		{
			if (start < end) return barTime > start && barTime <= end;
			return barTime > start || barTime <= end;
		}

		private void UpdateDisplaySeries()
		{
			if (rthMidSeries == null || ethMidSeries == null) return;

			double ethHigh = !double.IsNaN(curEthAnchorHigh) ? curEthAnchorHigh : curETH.High;
			double ethLow = !double.IsNaN(curEthAnchorLow) ? curEthAnchorLow : curETH.Low;
			double rthHigh = !double.IsNaN(curRthAnchorHigh) ? curRthAnchorHigh : curRTH.High;
			double rthLow = !double.IsNaN(curRthAnchorLow) ? curRthAnchorLow : curRTH.Low;
			bool primaryInRth = IsInTimeWindow(Times[0][0].TimeOfDay, RTHOpenTime, RTHCloseTime);

			ethMidSeries[0] = (!double.IsNaN(ethHigh) && !double.IsNaN(ethLow)) ? (ethHigh + ethLow) * 0.5 : double.NaN;
			if (primaryInRth)
				rthMidSeries[0] = (!double.IsNaN(rthHigh) && !double.IsNaN(rthLow)) ? (rthHigh + rthLow) * 0.5 : double.NaN;
			else
				rthMidSeries[0] = double.NaN;
		}

		private void UpdateRthHistory(DateTime time, TimeSpan tod, double open, double high, double low, double close, double typPrice, double volume, bool isRthBar)
		{
			if (rthHistoryByDate == null) return;

			DateTime date = time.Date;
			LevelSet day;
			if (!rthHistoryByDate.TryGetValue(date, out day))
			{
				day = new LevelSet();
				rthHistoryByDate[date] = day;

				if (rthHistoryByDate.Count > 20)
				{
					DateTime oldest = rthHistoryByDate.Keys.OrderBy(d => d).First();
					rthHistoryByDate.Remove(oldest);
				}
			}

			if (isRthBar)
			{
				day.UpdateHL(high, low, close, open);
				day.Vwap.Add(typPrice, volume);
				DistributeVolume(day, high, low, volume);
				if (day.VolByPrice.Count > 2) CalcVA(day);
			}

			if (IsInTimeWindowIncludingStart(tod, RTHOpenTime, PdcTimeSpan))
				day.Close = close;
		}

		private void RefreshPriorRthFromHistory(DateTime currentDate)
		{
			if (rthHistoryByDate == null || rthHistoryByDate.Count == 0) return;

			DateTime priorDate = rthHistoryByDate.Keys
				.Where(d => d < currentDate && !double.IsNaN(rthHistoryByDate[d].Open))
				.OrderByDescending(d => d)
				.FirstOrDefault();

			if (priorDate == DateTime.MinValue) return;

			CopyLevelSet(rthHistoryByDate[priorDate], priorRTH);
			priorRTH_VAH = priorRTH.VAH;
			priorRTH_VAL = priorRTH.VAL;
			priorRTH_POC = priorRTH.POC;
		}

		private void CopyLevelSet(LevelSet source, LevelSet target)
		{
			if (source == null || target == null) return;

			target.Open = source.Open;
			target.High = source.High;
			target.Low = source.Low;
			target.Close = source.Close;
			target.Mid = source.Mid;
			target.VAH = source.VAH;
			target.VAL = source.VAL;
			target.POC = source.POC;
		}

		private bool CrossedTime(TimeSpan prev, TimeSpan cur, TimeSpan target)
		{
			if (target > prev && target <= cur) return true;
			if (prev > cur && (target > prev || target <= cur)) return true;
			return false;
		}

		private bool CrossedSessionOpenTime(TimeSpan prev, TimeSpan cur, TimeSpan target)
		{
			// Minute bars are close-stamped, so the session-opening candle is the
			// first bar after the boundary, not the bar stamped exactly at it.
			if (prev <= cur) return prev <= target && target < cur;
			return target >= prev || target < cur;
		}

		private DateTime GetSessionOpenDateTime(DateTime barTime, TimeSpan sessionOpen)
		{
			DateTime anchor = barTime.Date + sessionOpen;
			return barTime.TimeOfDay < sessionOpen ? anchor.AddDays(-1) : anchor;
		}

		private DateTime GetEthTradingDate(DateTime time)
		{
			return ETHOpenTime > RTHOpenTime ? time.Date.AddDays(1) : time.Date;
		}

		private SessionInfo GetOrCreateSessionInfo(DateTime sessionDate)
		{
			SessionInfo info = sessionHistory.LastOrDefault(s => s.Date == sessionDate);
			if (info == null)
			{
				info = new SessionInfo { Date = sessionDate };
				sessionHistory.Add(info);
				if (sessionHistory.Count > 50) sessionHistory.RemoveAt(0);
			}

			return info;
		}

		// Snapshot RTH only (called at RTH open)
		private void CopyRthToPrior()
		{
			priorRTH.Open = curRTH.Open; priorRTH.High = curRTH.High; priorRTH.Low = curRTH.Low;
			priorRTH.Close = curRTH.Close; priorRTH.Mid = curRTH.Mid;
			priorRTH_VAH = curRTH.VAH; priorRTH_VAL = curRTH.VAL; priorRTH_POC = curRTH.POC;
		}

		// Snapshot ETH only (called at ETH open = start of new full day)
		private void CopyEthToPrior()
		{
			priorETH.Open = curETH.Open; priorETH.High = curETH.High; priorETH.Low = curETH.Low;
			priorETH.Close = curETH.Close; priorETH.Mid = curETH.Mid;
			priorETH_VAH = curETH.VAH; priorETH_VAL = curETH.VAL; priorETH_POC = curETH.POC;
		}

		private void UpdateWeeklyLevels(DateTime time, double open, double high, double low, double close, double typPrice, double volume)
		{
			DateTime weekStart = GetWeekStart(time);
			if (curWeekOpenTime != weekStart)
			{
				if (curWeekOpenTime != DateTime.MinValue && curWeek != null && !double.IsNaN(curWeek.Open))
				{
					CopyLevelSet(curWeek, priorWeek);
					priorWeek_VAH = curWeek.VAH;
					priorWeek_VAL = curWeek.VAL;
					priorWeek_POC = curWeek.POC;
				}

				curWeek.ResetPrices();
				curWeekOpenTime = weekStart;
				curWeekOpenIdx = -1;
				curWeekHighTime = curWeekLowTime = DateTime.MinValue;
				curWeekHighIdx = curWeekLowIdx = -1;
				curWeek.UpdateHL(high, low, close, open);
				curWeekHighTime = curWeekLowTime = time;
			}
			else
			{
				UpdateLevelSetWithAnchors(curWeek, high, low, close, double.NaN, time, ref curWeekHighTime, ref curWeekLowTime);
			}

			curWeek.Vwap.Add(typPrice, volume);
			DistributeVolume(curWeek, high, low, volume);
			if (curWeek.VolByPrice.Count > 2)
				CalcVA(curWeek);
		}

		private DateTime GetWeekStart(DateTime time)
		{
			int daysSinceSunday = (int)time.DayOfWeek;
			DateTime sundayStart = time.Date.AddDays(-daysSinceSunday).Add(ETHOpenTime);
			if (time < sundayStart)
				sundayStart = sundayStart.AddDays(-7);
			return sundayStart;
		}

		private bool IsAfterEthRollover()
		{
			try
			{
				NinjaTrader.Data.Bars primaryBars = PrimaryBars;
				if (primaryBars == null || CurrentBars == null || CurrentBars.Length == 0 || CurrentBars[0] < 0)
					return false;

				DateTime lastTime = primaryBars.GetTime(Math.Min(CurrentBars[0], primaryBars.Count - 1));
				TimeSpan tod = lastTime.TimeOfDay;
				return IsInTimeWindowIncludingStart(tod, ETHOpenTime, RTHOpenTime);
			}
			catch { }

			return false;
		}

		private void BuildLevelCache()
		{
			for (int i = 0; i < LVL_COUNT; i++)
			{
				levelCache[i].Enabled = false;
				levelCache[i].StrokeIdx = -1;
				levelCache[i].Width = 0;
				levelCache[i].StartTime = DateTime.MinValue;
				levelCache[i].StartIdx = -1;
			}

			DateTime rthAnchor = curSessionInfo != null ? curSessionInfo.RthOpenTime : DateTime.MinValue;
			DateTime ethAnchor = curSessionInfo != null ? curSessionInfo.EthOpenTime : DateTime.MinValue;
			DateTime tdoAnchor = curSessionInfo != null ? curSessionInfo.TrueDailyOpenTime : DateTime.MinValue;
			DateTime orAnchor = curSessionInfo != null ? curSessionInfo.OrEndTime : DateTime.MinValue;
			DateTime ibAnchor = curSessionInfo != null ? curSessionInfo.IbEndTime : DateTime.MinValue;
			int rthAnchorIdx = curSessionInfo != null ? SanitizePrimaryIndex(curSessionInfo.RthOpenIdx) : -1;
			int ethAnchorIdx = curSessionInfo != null ? SanitizePrimaryIndex(curSessionInfo.EthOpenIdx) : -1;
			int tdoAnchorIdx = curSessionInfo != null ? SanitizePrimaryIndex(curSessionInfo.TrueDailyOpenIdx) : -1;
			int orAnchorIdx = curSessionInfo != null ? SanitizePrimaryIndex(curSessionInfo.OrEndIdx) : -1;
			int ibAnchorIdx = curSessionInfo != null ? SanitizePrimaryIndex(curSessionInfo.IbEndIdx) : -1;
			if (rthAnchor == DateTime.MinValue && rangeSessionDate != DateTime.MinValue)
				rthAnchor = rangeSessionDate.Date + RTHOpenTime;
			if (tdoAnchor == DateTime.MinValue && trueDailyOpenDate != DateTime.MinValue)
				tdoAnchor = trueDailyOpenDate.Date;
			if (orAnchor == DateTime.MinValue && orStartTime != DateTime.MinValue)
				orAnchor = orStartTime + GetORDurationTimeSpan();
			if (ibAnchor == DateTime.MinValue && ibEndTime != DateTime.MinValue)
				ibAnchor = ibEndTime;
			DateTime onMidAnchor = LaterTime(overnightHighTime, overnightLowTime);
			int onMidAnchorIdx = LaterIdx(overnightHighIdx, overnightLowIdx);
			DateTime orMidAnchor = LaterTime(orHighTime, orLowTime);
			int orMidAnchorIdx = LaterIdx(orHighIdx, orLowIdx);
			DateTime ibMidAnchor = LaterTime(ibHighTime, ibLowTime);
			int ibMidAnchorIdx = LaterIdx(ibHighIdx, ibLowIdx);
			DateTime weekMidAnchor = LaterTime(curWeekHighTime, curWeekLowTime);
			int weekMidAnchorIdx = LaterIdx(curWeekHighIdx, curWeekLowIdx);
			bool priorRthOwnsRthSpace = IsAfterEthRollover();

			if (ShowONRange)  { SetLvl(L_ONH,    overnight.High,    LblONH,    0, null, 0, overnightHighTime, overnightHighIdx); SetLvl(L_ONL,    overnight.Low,    LblONL,    0, null, 0, overnightLowTime, overnightLowIdx); SetLvl(L_ONMID,  overnight.Mid,    LblONM,    0, null, 0, onMidAnchor, onMidAnchorIdx); }
			if (ShowONVA)     { SetLvl(L_ONVAH,  overnight.VAH,    LblOVAH,   1, null, 0, rthAnchor, rthAnchorIdx); SetLvl(L_ONVAL,  overnight.VAL,    LblOVAL,   1, null, 0, rthAnchor, rthAnchorIdx); SetLvl(L_ONPOC,  overnight.POC,    LblOPOC,   1, null, 0, rthAnchor, rthAnchorIdx); }
			if (ShowTrueDailyOpen && trueDailyOpenDate != DateTime.MinValue) SetLvl(L_TDO, trueDailyOpen, LblTDO, 22, null, 0, tdoAnchor, tdoAnchorIdx);
			if (ShowOR && orComplete)       { SetLvl(L_ORH,    orHigh,           FormatORLabel(LblORH),    2, null, 0, orHighTime, orHighIdx); SetLvl(L_ORL,    orLow,            FormatORLabel(LblORL),    2, null, 0, orLowTime, orLowIdx); SetLvl(L_ORMID,  orMid,            FormatORLabel(LblORM),    2, null, 0, orMidAnchor, orMidAnchorIdx); }
			if (ShowIB && ibComplete)
			{
				SetLvl(L_IBH, ibHigh, LblIBH, 19, MgiDashStyle.Solid, 2, ibHighTime, ibHighIdx);
				SetLvl(L_IBL, ibLow, LblIBL, 20, MgiDashStyle.Solid, 2, ibLowTime, ibLowIdx);
				if (!ShowIBExtensions) SetLvl(L_IBMID, ibMid, LblIBM, 21, MgiDashStyle.Solid, 1, ibMidAnchor, ibMidAnchorIdx);
			}
			if (ShowIBExtensions) BuildIBExtensionLevels();
			if (ShowCurRTH && !priorRthOwnsRthSpace) { SetLvl(L_CRTH_O, curRTH.Open, LblRTHO, 4, null, 0, rthAnchor, rthAnchorIdx); SetLvl(L_CRTH_H, curRTH.High, LblRTHH, 4, null, 0, curRthHighTime, curRthHighIdx); SetLvl(L_CRTH_L, curRTH.Low, LblRTHL, 4, null, 0, curRthLowTime, curRthLowIdx); }
			if (ShowDailyOpen) SetLvl(L_CETH_O, curETH.Open, LblETHO, 5, null, 0, ethAnchor, ethAnchorIdx);
			if (ShowCurETH)   { SetLvl(L_CETH_H, curETH.High,      LblETHH,   5, null, 0, curEthHighTime, curEthHighIdx); SetLvl(L_CETH_L, curETH.Low,       LblETHL,   5, null, 0, curEthLowTime, curEthLowIdx); }
			if (ShowPriorRTH)
			{
				if (!SamePrice(priorRTH.High, priorETH.High)) SetLvl(L_PDH, priorRTH.High, LblPDH, 6, null, 0, ethAnchor, ethAnchorIdx);
				if (!SamePrice(priorRTH.Low, priorETH.Low)) SetLvl(L_PDL, priorRTH.Low, LblPDL, 6, null, 0, ethAnchor, ethAnchorIdx);
				SetLvl(L_PDC, priorRTH.Close, LblPDC, 6, null, 0, ethAnchor, ethAnchorIdx);
				SetLvl(L_PDO, priorRTH.Open, LblPDO, 6, null, 0, ethAnchor, ethAnchorIdx);
				SetLvl(L_PDMID, priorRTH.Mid, LblPDM, 6, null, 0, ethAnchor, ethAnchorIdx);
			}
			if (ShowPriorETH) { SetLvl(L_PETH_H, priorETH.High,    LblPEH,    7, null, 0, ethAnchor, ethAnchorIdx); SetLvl(L_PETH_L, priorETH.Low,     LblPEL,    7, null, 0, ethAnchor, ethAnchorIdx); SetLvl(L_PETH_C, priorETH.Close,   LblPEC,    7, null, 0, ethAnchor, ethAnchorIdx); }
			if (ShowCurRTHVA && !priorRthOwnsRthSpace) { SetLvl(L_CRVAH,  curRTH.VAH,       LblRVAH,   8, null, 0, rthAnchor, rthAnchorIdx); SetLvl(L_CRVAL,  curRTH.VAL,       LblRVAL,   8, null, 0, rthAnchor, rthAnchorIdx); SetLvl(L_CRPOC,  curRTH.POC,       LblRPOC,   8, null, 0, rthAnchor, rthAnchorIdx); }
			if (ShowCurETHVA) { SetLvl(L_CEVAH,  curETH.VAH,       LblEVAH,   9, null, 0, ethAnchor, ethAnchorIdx); SetLvl(L_CEVAL,  curETH.VAL,       LblEVAL,   9, null, 0, ethAnchor, ethAnchorIdx); SetLvl(L_CEPOC,  curETH.POC,       LblEPOC,   9, null, 0, ethAnchor, ethAnchorIdx); }
			if (ShowPriorRTHVA) { SetLvl(L_PRVAH, priorRTH_VAH,    LblPRVAH, 10, null, 0, ethAnchor, ethAnchorIdx); SetLvl(L_PRVAL,  priorRTH_VAL,    LblPRVAL, 10, null, 0, ethAnchor, ethAnchorIdx); SetLvl(L_PRPOC,  priorRTH_POC,    LblPRPOC, 10, null, 0, ethAnchor, ethAnchorIdx); }
			if (ShowPriorETHVA) { SetLvl(L_PEVAH, priorETH_VAH,    LblPEVAH, 11, null, 0, ethAnchor, ethAnchorIdx); SetLvl(L_PEVAL,  priorETH_VAL,    LblPEVAL, 11, null, 0, ethAnchor, ethAnchorIdx); SetLvl(L_PEPOC,  priorETH_POC,    LblPEPOC, 11, null, 0, ethAnchor, ethAnchorIdx); }
			if (ShowRTHVwap && !priorRthOwnsRthSpace) SetLvl(L_RVWAP, curRTH.Vwap.Value, LblVWAP,  12, null, 0, rthAnchor, rthAnchorIdx);
			if (ShowETHVwap) SetLvl(L_EVWAP, curETH.Vwap.Value, LblEVWAP, 13, null, 0, ethAnchor, ethAnchorIdx);
			if (ShowHalfGap && !priorRthOwnsRthSpace) SetLvl(L_HGAP,  halfGap,           LblHGAP,  14, null, 0, rthAnchor, rthAnchorIdx);

			if (ShowCurrentWeek)
			{
				SetLvl(L_CW_O, curWeek.Open, LblCWO, 23, null, 0, curWeekOpenTime, curWeekOpenIdx);
				SetLvl(L_CW_H, curWeek.High, LblCWH, 23, null, 0, curWeekHighTime, curWeekHighIdx);
				SetLvl(L_CW_L, curWeek.Low, LblCWL, 23, null, 0, curWeekLowTime, curWeekLowIdx);
				SetLvl(L_CW_MID, curWeek.Mid, LblCWM, 23, null, 0, weekMidAnchor, weekMidAnchorIdx);
			}
			if (ShowPriorWeek)
			{
				SetLvl(L_PW_O, priorWeek.Open, LblPWO, 24, null, 0, curWeekOpenTime, curWeekOpenIdx);
				SetLvl(L_PW_H, priorWeek.High, LblPWH, 24, null, 0, curWeekOpenTime, curWeekOpenIdx);
				SetLvl(L_PW_L, priorWeek.Low, LblPWL, 24, null, 0, curWeekOpenTime, curWeekOpenIdx);
				SetLvl(L_PW_C, priorWeek.Close, LblPWC, 24, null, 0, curWeekOpenTime, curWeekOpenIdx);
				SetLvl(L_PW_MID, priorWeek.Mid, LblPWM, 24, null, 0, curWeekOpenTime, curWeekOpenIdx);
			}
			if (ShowCurrentWeekVA)
			{
				SetLvl(L_CW_VAH, curWeek.VAH, LblCWVAH, 25, null, 0, curWeekOpenTime, curWeekOpenIdx);
				SetLvl(L_CW_VAL, curWeek.VAL, LblCWVAL, 25, null, 0, curWeekOpenTime, curWeekOpenIdx);
				SetLvl(L_CW_POC, curWeek.POC, LblCWPOC, 25, null, 0, curWeekOpenTime, curWeekOpenIdx);
			}
			if (ShowPriorWeekVA)
			{
				SetLvl(L_PW_VAH, priorWeek_VAH, LblPWVAH, 26, null, 0, curWeekOpenTime, curWeekOpenIdx);
				SetLvl(L_PW_VAL, priorWeek_VAL, LblPWVAL, 26, null, 0, curWeekOpenTime, curWeekOpenIdx);
				SetLvl(L_PW_POC, priorWeek_POC, LblPWPOC, 26, null, 0, curWeekOpenTime, curWeekOpenIdx);
			}
		}

		private void BuildIBExtensionLevels()
		{
			if (!ibComplete) return;
			if (double.IsNaN(ibHigh) || double.IsNaN(ibLow) || ibHigh <= ibLow) return;

			double range = ibHigh - ibLow;
			if (ShowIBQuarterExtensions)
			{
				DateTime ibAnchor = curSessionInfo != null ? curSessionInfo.IbEndTime : DateTime.MinValue;
				int ibAnchorIdx = curSessionInfo != null ? SanitizePrimaryIndex(curSessionInfo.IbEndIdx) : -1;
				SetLvl(L_IBQ25, ibLow + range * 0.25, "IB 25%", 21, MgiDashStyle.Dash, 1, ibAnchor, ibAnchorIdx);
				SetLvl(L_IBQ75, ibLow + range * 0.75, "IB 75%", 21, MgiDashStyle.Dash, 1, ibAnchor, ibAnchorIdx);
			}
			if (ShowIBHalfExtensions)
			{
				DateTime ibAnchor = curSessionInfo != null ? curSessionInfo.IbEndTime : DateTime.MinValue;
				int ibAnchorIdx = curSessionInfo != null ? SanitizePrimaryIndex(curSessionInfo.IbEndIdx) : -1;
				SetLvl(L_IBQ50, ibLow + range * 0.50, "IB 50%", 21, MgiDashStyle.Solid, 1, ibAnchor, ibAnchorIdx);
			}

			double[] percentages = { 0.25, 0.50, 0.75, 1.00, 1.50, 2.00, 2.50, 3.00, 3.50, 4.00 };
			int[] upIndices = { L_IBUP25, L_IBUP50, L_IBUP75, L_IBUP100, L_IBUP150, L_IBUP200, L_IBUP250, L_IBUP300, L_IBUP350, L_IBUP400 };
			int[] downIndices = { L_IBDN25, L_IBDN50, L_IBDN75, L_IBDN100, L_IBDN150, L_IBDN200, L_IBDN250, L_IBDN300, L_IBDN350, L_IBDN400 };

			for (int i = 0; i < percentages.Length; i++)
			{
				double pct = percentages[i];
				if (!ShouldShowIBExtensionPercent(pct)) continue;

				MgiDashStyle style = IsMajorIBExtension(pct) || IsHalfStepIBExtension(pct) ? MgiDashStyle.Solid : MgiDashStyle.Dash;
				int width = IsMajorIBExtension(pct) ? 2 : 1;
				string label = "IB +" + FormatPercentLabel(pct);
				DateTime ibAnchor = curSessionInfo != null ? curSessionInfo.IbEndTime : DateTime.MinValue;
				int ibAnchorIdx = curSessionInfo != null ? SanitizePrimaryIndex(curSessionInfo.IbEndIdx) : -1;

				SetLvl(upIndices[i], ibHigh + range * pct, label, 19, style, width, ibAnchor, ibAnchorIdx);
				SetLvl(downIndices[i], ibLow - range * pct, label.Replace("+", "-"), 20, style, width, ibAnchor, ibAnchorIdx);
			}
		}

		private bool ShouldShowIBExtensionPercent(double pct)
		{
			if (IsMajorIBExtension(pct)) return ShowIBFullExtensions;
			if (IsHalfStepIBExtension(pct)) return ShowIBHalfExtensions;
			return ShowIBQuarterExtensions;
		}

		private bool IsMajorIBExtension(double pct)
		{
			return SamePercent(pct, 1.00) || SamePercent(pct, 2.00) || SamePercent(pct, 3.00) || SamePercent(pct, 4.00);
		}

		private bool IsHalfStepIBExtension(double pct)
		{
			double percent = pct * 100.0;
			return Math.Abs(percent % 50.0) < 1E-07;
		}

		private bool SamePercent(double a, double b)
		{
			return Math.Abs(a - b) < 1E-07;
		}

		private string FormatPercentLabel(double pct)
		{
			return (pct * 100.0).ToString("0") + "%";
		}

		private string FormatORLabel(string label)
		{
			return ORDuration == MgiORDuration.Sec30
				? label + " (30S)"
				: label + " (" + (int)ORDuration + "M)";
		}

		private void SetLvl(int idx, double price, string label, int colorGroup)
		{
			SetLvl(idx, price, label, colorGroup, null, 0);
		}

		private void SetLvl(int idx, double price, string label, int colorGroup, DateTime startTime)
		{
			SetLvl(idx, price, label, colorGroup, null, 0, startTime, -1);
		}

		private void SetLvl(int idx, double price, string label, int colorGroup, MgiDashStyle? dashStyle, int width)
		{
			SetLvl(idx, price, label, colorGroup, dashStyle, width, DateTime.MinValue, -1);
		}

		private void SetLvl(int idx, double price, string label, int colorGroup, MgiDashStyle? dashStyle, int width, DateTime startTime, int startIdx)
		{
			if (double.IsNaN(price)) return;
			levelCache[idx].Price = price;
			levelCache[idx].Label = label + (ShowPriceInLabel ? " " + FormatPrice(price) : "");
			levelCache[idx].BrushIdx = colorGroup;
			levelCache[idx].StrokeIdx = dashStyle.HasValue ? (int)dashStyle.Value : -1;
			levelCache[idx].Width = width;
			levelCache[idx].StartTime = startTime;
			levelCache[idx].StartIdx = startIdx;
			levelCache[idx].Enabled = true;
		}

		private DateTime LaterTime(DateTime a, DateTime b)
		{
			if (a == DateTime.MinValue) return b;
			if (b == DateTime.MinValue) return a;
			return a > b ? a : b;
		}

		private int LaterIdx(int a, int b)
		{
			if (a < 0) return b;
			if (b < 0) return a;
			return Math.Max(a, b);
		}

		private NinjaTrader.Data.Bars PrimaryBars
		{
			get { return BarsArray != null && BarsArray.Length > 0 ? BarsArray[0] : Bars; }
		}

		private int SanitizePrimaryIndex(int idx)
		{
			NinjaTrader.Data.Bars primaryBars = PrimaryBars;
			return primaryBars != null && idx >= 0 && idx < primaryBars.Count ? idx : -1;
		}

		private bool SamePrice(double a, double b)
		{
			if (double.IsNaN(a) || double.IsNaN(b)) return false;
			double tolerance = TickSize > 0 ? TickSize * 0.5 : 1E-07;
			return Math.Abs(a - b) <= tolerance;
		}

		private string FormatPrice(double p)
		{
			return Instrument != null ? Instrument.MasterInstrument.FormatPrice(p) : p.ToString("F2");
		}

		private float EstimateLabelWidth(string text)
		{
			if (string.IsNullOrEmpty(text))
				return Math.Max(24f, LabelFontSize * 2f);

			float width = 8f;
			foreach (char c in text)
				width += (c == ' ' || c == '.' || c == ':' || c == '-' ? 0.35f : 0.62f) * LabelFontSize;

			return Math.Max(24f, Math.Min(240f, width));
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl cc, ChartScale cs)
		{
			try
			{
				base.OnRender(cc, cs);
				if (cc == null || cs == null || ChartBars == null || ChartPanel == null || RenderTarget == null || levelCache == null) return;
				EnsureDx();
				if (!dxValid || dxBrushes == null || dxStrokes == null) return;
				BuildLevelCache();

				float pT = ChartPanel.Y, pB = pT + ChartPanel.H, pL = ChartPanel.X;
				float CR = ChartPanel.X + ChartPanel.W;
				bool edgeMode = MgiStyle == MgiPlotStyle.Edge;
				float edgeEndX = CR;
				float edgeStartX = Math.Max(pL, edgeEndX - EdgeLineLength);

				SessionInfo si = curSessionInfo ?? sessionHistory.LastOrDefault();
				int rthPrimaryIdx = si != null ? GetPrimaryBarIndex(si.RthOpenTime, si.RthOpenIdx) : -1;
				int ethPrimaryIdx = si != null ? GetPrimaryBarIndex(si.EthOpenTime, si.EthOpenIdx) : -1;

				float rthX = rthPrimaryIdx != -1 ? cc.GetXByBarIndex(ChartBars, rthPrimaryIdx) : -1;
				float ethX = ethPrimaryIdx != -1 ? cc.GetXByBarIndex(ChartBars, ethPrimaryIdx) : -1;
				float nowX = GetVisibleLevelEndX(cc, pL, CR);
				bool priorRthOwnsRthSpace = IsAfterEthRollover();

				var oldAA = RenderTarget.AntialiasMode;
				RenderTarget.AntialiasMode = AntialiasMode.Aliased;
				try
				{

			// Vert line at session open (if reached)
			if (ShowSessionLine && rthX > 0 && dxBrushes.Length > 16 && dxBrushes[16] != null)
				DrawLineWithOpacity(new Vector2(rthX, pT), new Vector2(rthX, pB), dxBrushes[16], 1f);

			// Draw levels -- Pass 1: lines only, collect labels for stagger pass
			var pendingLabels = ShowLabels && dxLabelFormat != null
				? new System.Collections.Generic.List<(float y, float eX, int bi, string text)>()
				: null;
			for (int i = 0; i < LVL_COUNT; i++)
			{
				if (!levelCache[i].Enabled) continue;
				float y = cs.GetYByValue(levelCache[i].Price);
				if (y < pT - 5 || y > pB + 5) continue;

				int bi = levelCache[i].BrushIdx;
				if (bi >= dxBrushes.Length || dxBrushes[bi] == null) continue;

				float sX = GetLevelStartX(cc, levelCache[i], i, pL);
				if (sX < 0) continue;
				float eX = nowX;

				if (edgeMode)
				{
					sX = edgeStartX;
					eX = edgeEndX;
				}

				if (eX < pL) continue;
				if (sX > CR) sX = pL;
				if (sX < pL) sX = pL;
				if (eX > CR) eX = CR;

				bool isVA = (i >= L_ONVAH && i <= L_ONPOC) || (i >= L_CRVAH && i <= L_CRPOC)
					|| (i >= L_CEVAH && i <= L_CEPOC) || (i >= L_PRVAH && i <= L_PRPOC)
					|| (i >= L_PEVAH && i <= L_PEPOC) || (i >= L_CW_VAH && i <= L_PW_POC);
				int w = levelCache[i].Width > 0 ? levelCache[i].Width : (isVA ? VALineWidth : MainLineWidth);
				MgiDashStyle style = levelCache[i].StrokeIdx >= 0 ? (MgiDashStyle)levelCache[i].StrokeIdx : (isVA ? VADashStyle : MainDashStyle);
				var stroke = GetStroke(style);

				DrawLineWithOpacity(new Vector2(sX, y), new Vector2(eX, y), dxBrushes[bi], w, stroke);

				// Collect label for Pass 2
				float labelAnchorX = edgeMode ? sX + 4f - LabelXOffset : eX;
				pendingLabels?.Add((y, labelAnchorX, bi, levelCache[i].Label));
			}

			// Mids are dynamic levels, so draw the current value from the bar that made the latest high/low input.
			if (ShowRthMid && !priorRthOwnsRthSpace)
			{
				if (edgeMode) DrawMidLevel(cs, curRTH.Mid, edgeStartX, edgeEndX, pL, CR, 17, 1, "RTH MID", pendingLabels);
				else DrawAnchoredMidLevel(cc, cs, curRTH.Mid, LaterTime(curRthHighTime, curRthLowTime), LaterIdx(curRthHighIdx, curRthLowIdx), nowX, pL, CR, 17, 1, "RTH MID", pendingLabels);
			}
			if (ShowEthMid)
			{
				if (edgeMode) DrawMidLevel(cs, curETH.Mid, edgeStartX, edgeEndX, pL, CR, 18, 1, "ETH MID", pendingLabels);
				else DrawAnchoredMidLevel(cc, cs, curETH.Mid, LaterTime(curEthHighTime, curEthLowTime), LaterIdx(curEthHighIdx, curEthLowIdx), nowX, pL, CR, 18, 1, "ETH MID", pendingLabels);
			}

			// Draw levels -- Pass 2: labels with smarter collision handling.
			// Same-price labels spread horizontally; nearby distinct levels stack vertically.
			if (pendingLabels != null && pendingLabels.Count > 0)
			{
				pendingLabels.Sort((a, b) => a.y.CompareTo(b.y));

				float samePriceThreshold = Math.Max(2f, LabelFontSize * 0.2f);
				float colStep = edgeMode ? LabelFontSize * 3.2f : LabelFontSize * 5.0f;
				float labelHeight = LabelFontSize + 4f;
				float halfLabelHeight = labelHeight * 0.5f;
				float lastNaturalY = float.MinValue;
				float lastDrawY = float.MinValue;
				int samePriceCol = 0;

				foreach (var lbl in pendingLabels)
				{
					if (lbl.bi >= dxBrushes.Length || dxBrushes[lbl.bi] == null) continue;

					bool samePrice = Math.Abs(lbl.y - lastNaturalY) <= samePriceThreshold;
					float drawY = lbl.y;
					if (samePrice)
					{
						samePriceCol++;
						drawY = lastDrawY;
					}
					else
					{
						samePriceCol = 0;
						if (lastDrawY != float.MinValue && drawY - lastDrawY < labelHeight)
							drawY = lastDrawY + labelHeight;
					}

					if (drawY - halfLabelHeight < pT)
						drawY = pT + halfLabelHeight;
					if (drawY + halfLabelHeight > pB)
						drawY = pB - halfLabelHeight;

					lastNaturalY = lbl.y;
					lastDrawY = drawY;
					float labelWidth = EstimateLabelWidth(lbl.text);
					float maxTxtX = Math.Max(pL, CR - labelWidth - 2f);
					float txtX = lbl.eX + LabelXOffset + samePriceCol * colStep;
					if (txtX > maxTxtX)
						txtX = samePriceCol > 0 ? maxTxtX - samePriceCol * colStep : maxTxtX;
					if (txtX < pL) txtX = pL;
					float rectWidth = Math.Max(labelWidth + 4f, Math.Min(240f, CR - txtX));
					var rect = new RectangleF(txtX, drawY - halfLabelHeight, rectWidth, labelHeight);
					DrawTextWithOpacity(lbl.text, dxLabelFormat, rect, dxBrushes[lbl.bi]);
				}
			}

				}
				finally
				{
					if (RenderTarget != null)
						RenderTarget.AntialiasMode = oldAA;
				}
			}
			catch (Exception ex)
			{
				DisposeDx();
				PrintRenderSkip(ex);
			}
		}

		private void PrintRenderSkip(Exception ex)
		{
			DateTime now = DateTime.UtcNow;
			if ((now - lastRenderSkipUtc).TotalSeconds < 30) return;
			lastRenderSkipUtc = now;
			Print("OrcaMGIDaily: skipped one render frame: " + ex.Message);
		}

		private bool IsIBProjectionLevel(int idx)
		{
			return idx == L_IBH || idx == L_IBL || idx == L_IBMID || (idx >= L_IBQ25 && idx <= L_IBDN400);
		}

		private float GetVisibleLevelEndX(ChartControl cc, float panelLeft, float panelRight)
		{
			try
			{
				if (cc != null && ChartBars != null && CurrentBars != null && CurrentBars.Length > 0 && CurrentBars[0] >= 0)
				{
					int idx = ChartBars.ToIndex >= 0 ? Math.Min(ChartBars.ToIndex, CurrentBars[0]) : CurrentBars[0];
					float x = cc.GetXByBarIndex(ChartBars, idx);
					if (!float.IsNaN(x) && !float.IsInfinity(x))
					{
						if (x < panelLeft) return panelLeft;
						if (x > panelRight) return panelRight;
						return x;
					}
				}
			}
			catch { }

			return panelRight;
		}

		private void DrawMidLevel(ChartScale cs, double price, float startX, float endX, float panelLeft, float canvasRight, int brushIdx, int width, string label,
			System.Collections.Generic.List<(float y, float eX, int bi, string text)> pendingLabels = null)
		{
			if (double.IsNaN(price) || brushIdx >= dxBrushes.Length || dxBrushes[brushIdx] == null) return;
			if (endX < panelLeft || startX > canvasRight) return;
			if (startX < panelLeft || startX < 0) startX = panelLeft;

			float y = cs.GetYByValue(price);
			DrawLineWithOpacity(new Vector2(startX, y), new Vector2(endX, y), dxBrushes[brushIdx], width, GetStroke(MgiDashStyle.Solid));
			if (ShowLabels && !string.IsNullOrEmpty(label))
				pendingLabels?.Add((y, endX, brushIdx, label));
		}

		private void DrawAnchoredMidLevel(ChartControl cc, ChartScale cs, double price, DateTime startTime, int startIdx, float endX, float panelLeft, float canvasRight, int brushIdx, int width, string label,
			System.Collections.Generic.List<(float y, float eX, int bi, string text)> pendingLabels = null)
		{
			LevelInfo midLevel = new LevelInfo
			{
				Price = price,
				Label = label,
				BrushIdx = brushIdx,
				StrokeIdx = (int)MgiDashStyle.Solid,
				Width = width,
				StartTime = startTime,
				StartIdx = startIdx,
				Enabled = true
			};

			float startX = GetLevelStartX(cc, midLevel, -1, panelLeft);
			if (startX < 0)
				return;

			DrawMidLevel(cs, price, startX, endX, panelLeft, canvasRight, brushIdx, width, label, pendingLabels);
		}

		private float GetLevelStartX(ChartControl cc, LevelInfo level, int levelIdx, float panelLeft)
		{
			try
			{
				NinjaTrader.Data.Bars primaryBars = PrimaryBars;
				if (primaryBars != null && ChartBars != null && ChartBars.FromIndex >= 0 && ChartBars.FromIndex < primaryBars.Count)
				{
					int fromIdx = Math.Max(0, Math.Min(ChartBars.FromIndex, primaryBars.Count - 1));
					int toIdx = Math.Max(0, Math.Min(ChartBars.ToIndex, primaryBars.Count - 1));
					if (toIdx < fromIdx)
					{
						int tmp = fromIdx;
						fromIdx = toIdx;
						toIdx = tmp;
					}

					DateTime firstVisible = primaryBars.GetTime(fromIdx);
					DateTime lastVisible = primaryBars.GetTime(toIdx);
					DateTime startTime = ResolveLevelStartTime(level, levelIdx, lastVisible);

					if (startTime != DateTime.MinValue)
					{
						// If the level was born before the visible window, draw it
						// from the visible edge instead of asking NT to map an
						// off-screen timestamp that may not resolve cleanly.
						if (startTime <= firstVisible)
							return panelLeft;

						int timeIdx = FindVisiblePrimaryBarIndexAtOrAfter(startTime, fromIdx, toIdx);
						if (timeIdx >= 0)
							return ClampStartX(cc.GetXByBarIndex(ChartBars, timeIdx), panelLeft);

						// When NT cannot resolve the timestamp inside the visible
						// range, do not fake an infinite-left anchor.
						return -1;
					}

					if (level.StartIdx >= 0)
					{
						if (level.StartIdx < fromIdx)
							return panelLeft;
						if (level.StartIdx > toIdx)
							return -1;
						return ClampStartX(cc.GetXByBarIndex(ChartBars, level.StartIdx), panelLeft);
					}
				}
			}
			catch { }

			int startIdx = GetPrimaryBarIndex(level.StartTime, level.StartIdx);
			if (startIdx != -1)
				return ClampStartX(cc.GetXByBarIndex(ChartBars, startIdx), panelLeft);

			return -1;
		}

		private float ClampStartX(float x, float panelLeft)
		{
			if (float.IsNaN(x) || float.IsInfinity(x))
				return panelLeft;
			return x < panelLeft ? panelLeft : x;
		}

		private int FindVisiblePrimaryBarIndexAtOrAfter(DateTime time, int fromIdx, int toIdx)
		{
			try
			{
				NinjaTrader.Data.Bars primaryBars = PrimaryBars;
				if (primaryBars == null || time == DateTime.MinValue || fromIdx < 0 || toIdx < fromIdx)
					return -1;

				for (int i = fromIdx; i <= toIdx; i++)
				{
					if (primaryBars.GetTime(i) >= time)
						return i;
				}
			}
			catch { }

			return -1;
		}

		private DateTime ResolveLevelStartTime(LevelInfo level, int levelIdx, DateTime visibleReferenceTime)
		{
			DateTime fallback = GetVisibleSessionStartTimeForLevel(levelIdx, visibleReferenceTime);

			if (level.StartTime == DateTime.MinValue)
				return fallback;

			// Fixed-time levels can always be derived from the visible chart date.
			// This keeps reload/lazy-load bar index quirks from hiding valid levels.
			if (fallback != DateTime.MinValue && !IsVariableAnchorLevel(levelIdx))
				return fallback;

			return level.StartTime;
		}

		private DateTime GetVisibleSessionStartTimeForLevel(int levelIdx, DateTime visibleReferenceTime)
		{
			if (visibleReferenceTime == DateTime.MinValue)
				return DateTime.MinValue;

			DateTime rthOpen = GetSessionOpenDateTime(visibleReferenceTime, RTHOpenTime);
			DateTime ethOpen = GetSessionOpenDateTime(visibleReferenceTime, ETHOpenTime);
			DateTime weekStart = GetWeekStart(visibleReferenceTime);
			DateTime midnight = visibleReferenceTime.Date;

			if (levelIdx == L_TDO)
				return midnight;

			if (levelIdx >= L_ORH && levelIdx <= L_ORMID)
				return rthOpen + GetORDurationTimeSpan();

			if (IsIBProjectionLevel(levelIdx))
				return rthOpen + TimeSpan.FromMinutes(60);

			if (levelIdx == L_CRTH_O || levelIdx == L_CRVAH || levelIdx == L_CRVAL || levelIdx == L_CRPOC
				|| levelIdx == L_RVWAP || levelIdx == L_HGAP || (levelIdx >= L_ONVAH && levelIdx <= L_ONPOC))
				return rthOpen;

			if (levelIdx == L_CETH_O || levelIdx == L_PDH || levelIdx == L_PDL || levelIdx == L_PDC || levelIdx == L_PDO || levelIdx == L_PDMID
				|| levelIdx == L_PETH_H || levelIdx == L_PETH_L || levelIdx == L_PETH_C
				|| levelIdx == L_CEVAH || levelIdx == L_CEVAL || levelIdx == L_CEPOC
				|| levelIdx == L_PRVAH || levelIdx == L_PRVAL || levelIdx == L_PRPOC
				|| levelIdx == L_PEVAH || levelIdx == L_PEVAL || levelIdx == L_PEPOC
				|| levelIdx == L_EVWAP)
				return ethOpen;

			if (levelIdx == L_ONMID)
				return rthOpen;

			if ((levelIdx >= L_CW_O && levelIdx <= L_CW_MID) || (levelIdx >= L_PW_O && levelIdx <= L_PW_POC))
				return weekStart;

			return DateTime.MinValue;
		}

		private bool IsVariableAnchorLevel(int levelIdx)
		{
			return levelIdx == L_ONH || levelIdx == L_ONL || levelIdx == L_ONMID
				|| levelIdx == L_ORH || levelIdx == L_ORL || levelIdx == L_ORMID
				|| levelIdx == L_IBH || levelIdx == L_IBL || levelIdx == L_IBMID
				|| levelIdx == L_CRTH_H || levelIdx == L_CRTH_L
				|| levelIdx == L_CETH_H || levelIdx == L_CETH_L
				|| levelIdx == L_CW_H || levelIdx == L_CW_L || levelIdx == L_CW_MID;
		}

		private int GetPrimaryBarIndex(DateTime time, int fallbackIdx)
		{
			try
			{
				NinjaTrader.Data.Bars primaryBars = PrimaryBars;
				if (primaryBars != null && fallbackIdx >= 0 && fallbackIdx < primaryBars.Count)
				{
					if (time == DateTime.MinValue)
						return fallbackIdx;
					DateTime fallbackTime = primaryBars.GetTime(fallbackIdx);
					DateTime priorFallbackTime = fallbackIdx > 0 ? primaryBars.GetTime(fallbackIdx - 1) : DateTime.MinValue;
					if (fallbackTime >= time && (fallbackIdx == 0 || priorFallbackTime < time))
						return fallbackIdx;
				}

				if (primaryBars != null && time != DateTime.MinValue)
				{
					int idx = primaryBars.GetBar(time);
					if (idx < 0) idx = 0;
					while (idx < primaryBars.Count - 1 && primaryBars.GetTime(idx) < time)
						idx++;
					if (idx >= 0 && idx < primaryBars.Count) return idx;
				}

				if (primaryBars != null && fallbackIdx >= 0 && fallbackIdx < primaryBars.Count)
					return fallbackIdx;
			}
			catch { }

			return -1;
		}

		private void DrawSeriesLine(ChartScale cs, Series<double> series, int startIdx, int endIdx, int brushIdx, int width, string label = null,
			System.Collections.Generic.List<(float y, float eX, int bi, string text)> pendingLabels = null)
		{
			if (startIdx == -1 || brushIdx >= dxBrushes.Length || dxBrushes[brushIdx] == null) return;
			float lastX = -1, lastY = -1;
			float lastValidY = float.NaN;
			var stroke = GetStroke(MgiDashStyle.Solid);

			for (int i = startIdx; i <= endIdx; i++)
			{
				double val = series.GetValueAt(i);
				if (double.IsNaN(val)) { lastX = -1; continue; }
				float x = ChartControl.GetXByBarIndex(ChartBars, i);
				float y = cs.GetYByValue(val);
				if (lastX != -1)
					DrawLineWithOpacity(new Vector2(lastX, lastY), new Vector2(x, y), dxBrushes[brushIdx], width, stroke);
				lastX = x; lastY = y;
				lastValidY = y;
			}

			if (ShowLabels && !string.IsNullOrEmpty(label) && dxLabelFormat != null && !float.IsNaN(lastValidY))
				pendingLabels?.Add((lastValidY, ChartControl.GetXByBarIndex(ChartBars, ChartBars.ToIndex), brushIdx, label));
		}

		private void DrawRegionFill(ChartScale cs, double hi, double lo, int brushIdx, int opacity, float left, float right, float panelTop, float panelBot)
		{
			if (double.IsNaN(hi) || double.IsNaN(lo) || opacity <= 0) return;
			float yHi = cs.GetYByValue(hi);
			float yLo = cs.GetYByValue(lo);
			if (yHi > panelBot || yLo < panelTop) return;
			yHi = Math.Max(panelTop, yHi); yLo = Math.Min(panelBot, yLo);
			if (brushIdx >= dxBrushes.Length || dxBrushes[brushIdx] == null) return;
			float prevOp = dxBrushes[brushIdx].Opacity;
			dxBrushes[brushIdx].Opacity = opacity / 100f;
			RenderTarget.FillRectangle(new RectangleF(left, yHi, right - left, yLo - yHi), dxBrushes[brushIdx]);
			dxBrushes[brushIdx].Opacity = prevOp;
		}

		private void DrawSessionMarkers(ChartControl cc, ChartScale cs, float panelTop, float panelBot)
		{
			if (dxBrushes.Length <= 15 || dxBrushes[15] == null) return;
			NinjaTrader.Data.Bars primaryBars = PrimaryBars;
			if (primaryBars == null) return;

			int from = ChartBars.FromIndex, to = ChartBars.ToIndex;
			for (int i = from; i <= to; i++)
			{
				if (i >= primaryBars.Count || i < 1) continue;
				DateTime t = primaryBars.GetTime(i);
				DateTime pt = primaryBars.GetTime(i - 1);
				if (CrossedTime(pt.TimeOfDay, t.TimeOfDay, RTHOpenTime) || CrossedTime(pt.TimeOfDay, t.TimeOfDay, RTHCloseTime))
				{
					float x = cc.GetXByBarIndex(ChartBars, i);
					DrawLineWithOpacity(new Vector2(x, panelTop), new Vector2(x, panelBot), dxBrushes[15], 1f, null, 0.3f);
				}
			}
		}
		#endregion

		#region DX Resource Management
		private SharpDX.Direct2D1.StrokeStyle GetStroke(MgiDashStyle ds)
		{
			int idx = (int)ds;
			if (dxStrokes == null || idx < 0) return null;
			if (idx < dxStrokes.Length && dxStrokes[idx] != null) return dxStrokes[idx];
			return null;
		}

		private Color4 ToColor4(WpfBrush b, float alpha = 1f)
		{
			var c = (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alpha);
		}

		private float OpacityPercent(int opacity)
		{
			return Math.Max(0, Math.Min(100, opacity)) / 100f;
		}

		private void DrawLineWithOpacity(Vector2 start, Vector2 end, DxSolidBrush brush, float width, SharpDX.Direct2D1.StrokeStyle stroke = null, float opacityMultiplier = 1f)
		{
			if (brush == null) return;
			float prevOp = brush.Opacity;
			brush.Opacity = OpacityPercent(LineOpacity) * Math.Max(0f, Math.Min(1f, opacityMultiplier));
			try
			{
				RenderTarget.DrawLine(start, end, brush, width, stroke);
			}
			finally
			{
				brush.Opacity = prevOp;
			}
		}

		private void DrawTextWithOpacity(string text, SharpDX.DirectWrite.TextFormat format, RectangleF rect, DxSolidBrush brush)
		{
			if (brush == null || format == null) return;
			float prevOp = brush.Opacity;
			brush.Opacity = OpacityPercent(LabelOpacity);
			try
			{
				RenderTarget.DrawText(text, format, rect, brush);
			}
			finally
			{
				brush.Opacity = prevOp;
			}
		}

		private void EnsureDx()
		{
			if (RenderTarget == null) return;
			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxValid && dxResourceRenderTarget == currentTarget) return;
			if (dxValid || dxResourceRenderTarget != IntPtr.Zero)
				DisposeDx();
			try
			{
				WpfBrush[] colorMap = { ONColor, ONVAColor, ORColor, IBColor, CurRTHColor, CurETHColor,
					PriorRTHColor, PriorETHColor, CurRTHVAColor, CurETHVAColor, PriorRTHVAColor, PriorETHVAColor,
					RTHVwapColor, ETHVwapColor, HalfGapColor, SessionMarkerColor, SessionLineColor,
					RthMidColor, EthMidColor, IBExtensionUpColor, IBExtensionDownColor, IBInnerLevelColor,
					TrueDailyOpenColor, CurrentWeekColor, PriorWeekColor, CurrentWeekVAColor, PriorWeekVAColor };
				dxBrushes = new DxSolidBrush[colorMap.Length];
				for (int i = 0; i < colorMap.Length; i++)
					dxBrushes[i] = new DxSolidBrush(RenderTarget, ToColor4(colorMap[i]));

				dxStrokes = new SharpDX.Direct2D1.StrokeStyle[4];
				var factory = RenderTarget.Factory;
				dxStrokes[0] = new SharpDX.Direct2D1.StrokeStyle(factory, new StrokeStyleProperties { DashStyle = DashStyle.Solid });
				dxStrokes[1] = new SharpDX.Direct2D1.StrokeStyle(factory, new StrokeStyleProperties { DashStyle = DashStyle.Dash });
				dxStrokes[2] = new SharpDX.Direct2D1.StrokeStyle(factory, new StrokeStyleProperties { DashStyle = DashStyle.Dot });
				dxStrokes[3] = new SharpDX.Direct2D1.StrokeStyle(factory, new StrokeStyleProperties { DashStyle = DashStyle.DashDot });

				dxLabelFormat = new SharpDX.DirectWrite.TextFormat(
					NinjaTrader.Core.Globals.DirectWriteFactory,
					LabelFontName, FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, (float)LabelFontSize)
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
				dxLabelBrush?.Dispose();
				dxRegionBrush?.Dispose();
			}
			catch { }
			dxBrushes = null; dxStrokes = null; dxLabelFormat = null; dxLabelBrush = null; dxRegionBrush = null;
			dxResourceRenderTarget = IntPtr.Zero;
			dxValid = false;
		}

		public override void OnRenderTargetChanged() { DisposeDx(); base.OnRenderTargetChanged(); }
		#endregion

		#region Properties
		// --- 01. Session Times ---
		[NinjaScriptProperty][Display(Name="RTH Open Time", Description="Regular trading hours open time (Eastern)", Order=1, GroupName="01. Session Times")]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		public TimeSpan RTHOpenTime { get; set; }

		[NinjaScriptProperty][Display(Name="RTH Close Time", Description="Regular trading hours close time (Eastern)", Order=2, GroupName="01. Session Times")]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		public TimeSpan RTHCloseTime { get; set; }

		[NinjaScriptProperty][Display(Name="ETH Open Time", Description="Extended/Globex session open time (Eastern)", Order=3, GroupName="01. Session Times")]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
		public TimeSpan ETHOpenTime { get; set; }

		// --- 02. Overnight Range ---
		[Display(Name="Show Overnight Range", Description="Show ONH, ONL, ON Mid", Order=1, GroupName="02. Overnight Range")]
		public bool ShowONRange { get; set; }
		[XmlIgnore][Display(Name="ON Color", Order=2, GroupName="02. Overnight Range")]
		public WpfBrush ONColor { get; set; }
		[Browsable(false)] public string ONColorS { get { return Serialize.BrushToString(ONColor); } set { ONColor = Serialize.StringToBrush(value); } }
		[Range(0,100)][Display(Name="ON Region Opacity %", Description="0=off", Order=3, GroupName="02. Overnight Range")]
		public int ONRegionOpacity { get; set; }

		// --- 03. Overnight VA ---
		[Display(Name="Show Overnight VA", Description="Show ONVAH, ONVAL, ONPOC", Order=1, GroupName="03. Overnight VA")]
		public bool ShowONVA { get; set; }
		[XmlIgnore][Display(Name="ON VA Color", Order=2, GroupName="03. Overnight VA")]
		public WpfBrush ONVAColor { get; set; }
		[Browsable(false)] public string ONVAColorS { get { return Serialize.BrushToString(ONVAColor); } set { ONVAColor = Serialize.StringToBrush(value); } }

		// --- 04. Opening Range ---
		[Display(Name="Show Opening Range", Description="Show ORH, ORL, OR Mid", Order=1, GroupName="04. Opening Range")]
		public bool ShowOR { get; set; }
		[NinjaScriptProperty][Display(Name="OR Duration", Description="Opening range time window from RTH open", Order=2, GroupName="04. Opening Range")]
		public MgiORDuration ORDuration { get; set; }
		[XmlIgnore][Display(Name="OR Color", Order=3, GroupName="04. Opening Range")]
		public WpfBrush ORColor { get; set; }
		[Browsable(false)] public string ORColorS { get { return Serialize.BrushToString(ORColor); } set { ORColor = Serialize.StringToBrush(value); } }

		// --- 05. Initial Balance ---
		[Display(Name="Show Initial Balance", Description="Show IBH, IBL, IB Mid (first 60 min RTH)", Order=1, GroupName="05. Initial Balance")]
		public bool ShowIB { get; set; }
		[XmlIgnore][Display(Name="IB Color", Order=2, GroupName="05. Initial Balance")]
		public WpfBrush IBColor { get; set; }
		[Browsable(false)] public string IBColorS { get { return Serialize.BrushToString(IBColor); } set { IBColor = Serialize.StringToBrush(value); } }
		[Range(0,100)][Display(Name="IB Region Opacity %", Description="0=off", Order=3, GroupName="05. Initial Balance")]
		public int IBRegionOpacity { get; set; }
		[Display(Name="Show IB Extensions", Order=4, GroupName="05. Initial Balance")]
		public bool ShowIBExtensions { get; set; }
		[Display(Name="Show Full Extensions", Description="Show 100%, 200%, 300%, and 400% extensions", Order=5, GroupName="05. Initial Balance")]
		public bool ShowIBFullExtensions { get; set; }
		[Display(Name="Show 50s", Description="Show 50%, 150%, 250%, and 350% extensions", Order=6, GroupName="05. Initial Balance")]
		public bool ShowIBHalfExtensions { get; set; }
		[Display(Name="Show Quarters", Description="Show 25% and 75% internal levels and extensions", Order=7, GroupName="05. Initial Balance")]
		public bool ShowIBQuarterExtensions { get; set; }
		[XmlIgnore][Display(Name="IB Extension Up Color", Order=8, GroupName="05. Initial Balance")]
		public WpfBrush IBExtensionUpColor { get; set; }
		[Browsable(false)] public string IBExtensionUpColorS { get { return Serialize.BrushToString(IBExtensionUpColor); } set { IBExtensionUpColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="IB Extension Down Color", Order=9, GroupName="05. Initial Balance")]
		public WpfBrush IBExtensionDownColor { get; set; }
		[Browsable(false)] public string IBExtensionDownColorS { get { return Serialize.BrushToString(IBExtensionDownColor); } set { IBExtensionDownColor = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="IB Inner Quarter Color", Order=10, GroupName="05. Initial Balance")]
		public WpfBrush IBInnerLevelColor { get; set; }
		[Browsable(false)] public string IBInnerLevelColorS { get { return Serialize.BrushToString(IBInnerLevelColor); } set { IBInnerLevelColor = Serialize.StringToBrush(value); } }

		// --- 06-07. Current Day ---
		[Display(Name="Show Current RTH", Description="Show current RTH high and low", Order=1, GroupName="06. Current Day RTH")]
		public bool ShowCurRTH { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="06. Current Day RTH")]
		public WpfBrush CurRTHColor { get; set; }
		[Browsable(false)] public string CurRTHColorS { get { return Serialize.BrushToString(CurRTHColor); } set { CurRTHColor = Serialize.StringToBrush(value); } }

		[Display(Name="Show Daily Open", Description="Show the 6:00 PM Globex open", Order=1, GroupName="07. Current Day")]
		public bool ShowDailyOpen { get; set; }
		[Display(Name="Show Current Day", Description="Show current day high and low", Order=2, GroupName="07. Current Day")]
		public bool ShowCurETH { get; set; }
		[XmlIgnore][Display(Name="Color", Order=3, GroupName="07. Current Day")]
		public WpfBrush CurETHColor { get; set; }
		[Browsable(false)] public string CurETHColorS { get { return Serialize.BrushToString(CurETHColor); } set { CurETHColor = Serialize.StringToBrush(value); } }
		[Display(Name="Show True Daily Open", Description="Show the opening print of the midnight Eastern candle", Order=4, GroupName="07. Current Day")]
		public bool ShowTrueDailyOpen { get; set; }
		[XmlIgnore][Display(Name="True Daily Open Color", Order=5, GroupName="07. Current Day")]
		public WpfBrush TrueDailyOpenColor { get; set; }
		[Browsable(false)] public string TrueDailyOpenColorS { get { return Serialize.BrushToString(TrueDailyOpenColor); } set { TrueDailyOpenColor = Serialize.StringToBrush(value); } }

		// --- 08-09. Prior Levels ---
		[Display(Name="Show Prior RTH", Description="Show prior RTH high, low, close, open, and mid", Order=1, GroupName="08. Prior RTH")]
		public bool ShowPriorRTH { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="08. Prior RTH")]
		public WpfBrush PriorRTHColor { get; set; }
		[Browsable(false)] public string PriorRTHColorS { get { return Serialize.BrushToString(PriorRTHColor); } set { PriorRTHColor = Serialize.StringToBrush(value); } }

		[Display(Name="Show Prior Day", Description="Show prior full-day high, low, and close", Order=1, GroupName="09. Prior Day")]
		public bool ShowPriorETH { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="09. Prior Day")]
		public WpfBrush PriorETHColor { get; set; }
		[Browsable(false)] public string PriorETHColorS { get { return Serialize.BrushToString(PriorETHColor); } set { PriorETHColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Prior Day Close Definition",
			Description = "Which close price to use as PDC. Equities4PM = 4:00 PM equities close. CME415PM = 4:15 PM CME RTH futures close. Globex5PM = 5:00 PM, last price before the 1-hour CME maintenance break.",
			Order = 4, GroupName = "08. Prior Day RTH")]
		public MgiPdcMode PriorDayCloseMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Latest 5 PM PDC",
			Description = "Use the latest completed bar close up to 5:00 PM as PDC. Turn off to use Prior Day Close Definition.",
			Order = 3, GroupName = "08. Prior Day RTH")]
		public bool UseLatestCloseForPdc { get; set; }

		// --- 10-13. Value Areas ---
		[Display(Name="Show Current RTH VA", Order=1, GroupName="10. Current RTH VA")]
		public bool ShowCurRTHVA { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="10. Current RTH VA")]
		public WpfBrush CurRTHVAColor { get; set; }
		[Browsable(false)] public string CurRTHVAColorS { get { return Serialize.BrushToString(CurRTHVAColor); } set { CurRTHVAColor = Serialize.StringToBrush(value); } }

		[Display(Name="Show Current ETH VA", Order=1, GroupName="11. Current ETH VA")]
		public bool ShowCurETHVA { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="11. Current ETH VA")]
		public WpfBrush CurETHVAColor { get; set; }
		[Browsable(false)] public string CurETHVAColorS { get { return Serialize.BrushToString(CurETHVAColor); } set { CurETHVAColor = Serialize.StringToBrush(value); } }

		[Display(Name="Show Prior RTH VA", Order=1, GroupName="12. Prior RTH VA")]
		public bool ShowPriorRTHVA { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="12. Prior RTH VA")]
		public WpfBrush PriorRTHVAColor { get; set; }
		[Browsable(false)] public string PriorRTHVAColorS { get { return Serialize.BrushToString(PriorRTHVAColor); } set { PriorRTHVAColor = Serialize.StringToBrush(value); } }

		[Display(Name="Show Prior Day VA", Order=1, GroupName="13. Prior Day VA")]
		public bool ShowPriorETHVA { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="13. Prior Day VA")]
		public WpfBrush PriorETHVAColor { get; set; }
		[Browsable(false)] public string PriorETHVAColorS { get { return Serialize.BrushToString(PriorETHVAColor); } set { PriorETHVAColor = Serialize.StringToBrush(value); } }

		// --- 14. Weekly Levels ---
		[Display(Name="Show Current Week", Description="Show weekly open, high, low, and mid", Order=1, GroupName="14. Weekly Levels")]
		public bool ShowCurrentWeek { get; set; }
		[XmlIgnore][Display(Name="Current Week Color", Order=2, GroupName="14. Weekly Levels")]
		public WpfBrush CurrentWeekColor { get; set; }
		[Browsable(false)] public string CurrentWeekColorS { get { return Serialize.BrushToString(CurrentWeekColor); } set { CurrentWeekColor = Serialize.StringToBrush(value); } }
		[Display(Name="Show Prior Week", Description="Show prior weekly open, high, low, close, and mid", Order=3, GroupName="14. Weekly Levels")]
		public bool ShowPriorWeek { get; set; }
		[XmlIgnore][Display(Name="Prior Week Color", Order=4, GroupName="14. Weekly Levels")]
		public WpfBrush PriorWeekColor { get; set; }
		[Browsable(false)] public string PriorWeekColorS { get { return Serialize.BrushToString(PriorWeekColor); } set { PriorWeekColor = Serialize.StringToBrush(value); } }
		[Display(Name="Show Current Week VA", Description="Show current weekly VAH, VAL, and POC", Order=5, GroupName="14. Weekly Levels")]
		public bool ShowCurrentWeekVA { get; set; }
		[XmlIgnore][Display(Name="Current Week VA Color", Order=6, GroupName="14. Weekly Levels")]
		public WpfBrush CurrentWeekVAColor { get; set; }
		[Browsable(false)] public string CurrentWeekVAColorS { get { return Serialize.BrushToString(CurrentWeekVAColor); } set { CurrentWeekVAColor = Serialize.StringToBrush(value); } }
		[Display(Name="Show Prior Week VA", Description="Show prior weekly VAH, VAL, and POC", Order=7, GroupName="14. Weekly Levels")]
		public bool ShowPriorWeekVA { get; set; }
		[XmlIgnore][Display(Name="Prior Week VA Color", Order=8, GroupName="14. Weekly Levels")]
		public WpfBrush PriorWeekVAColor { get; set; }
		[Browsable(false)] public string PriorWeekVAColorS { get { return Serialize.BrushToString(PriorWeekVAColor); } set { PriorWeekVAColor = Serialize.StringToBrush(value); } }

		// --- 14. VWAP ---
		[Display(Name="Show RTH VWAP", Order=1, GroupName="14. VWAP")]
		public bool ShowRTHVwap { get; set; }
		[XmlIgnore][Display(Name="RTH VWAP Color", Order=2, GroupName="14. VWAP")]
		public WpfBrush RTHVwapColor { get; set; }
		[Browsable(false)] public string RTHVwapColorS { get { return Serialize.BrushToString(RTHVwapColor); } set { RTHVwapColor = Serialize.StringToBrush(value); } }

		[Display(Name="Show ETH VWAP", Order=3, GroupName="14. VWAP")]
		public bool ShowETHVwap { get; set; }
		[XmlIgnore][Display(Name="ETH VWAP Color", Order=4, GroupName="14. VWAP")]
		public WpfBrush ETHVwapColor { get; set; }
		[Browsable(false)] public string ETHVwapColorS { get { return Serialize.BrushToString(ETHVwapColor); } set { ETHVwapColor = Serialize.StringToBrush(value); } }

		// --- 15. Half Gap ---
		[Display(Name="Show Half Gap", Description="50% retracement of RTH open gap from prior close", Order=1, GroupName="15. Half Gap")]
		public bool ShowHalfGap { get; set; }
		[XmlIgnore][Display(Name="Half Gap Color", Order=2, GroupName="15. Half Gap")]
		public WpfBrush HalfGapColor { get; set; }
		[Browsable(false)] public string HalfGapColorS { get { return Serialize.BrushToString(HalfGapColor); } set { HalfGapColor = Serialize.StringToBrush(value); } }

		// --- 16. Session Markers ---
		[Display(Name="Show Session Markers", Description="Vertical lines at RTH open/close", Order=1, GroupName="16. Session Markers")]
		public bool ShowSessionMarkers { get; set; }
		[XmlIgnore][Display(Name="Marker Color", Order=2, GroupName="16. Session Markers")]
		public WpfBrush SessionMarkerColor { get; set; }
		[Browsable(false)] public string SessionMarkerColorS { get { return Serialize.BrushToString(SessionMarkerColor); } set { SessionMarkerColor = Serialize.StringToBrush(value); } }

		// --- 17. Labels ---
		[Display(Name="Show Labels", Description="Show price labels next to session breaks", Order=1, GroupName="17. Labels")]
		public bool ShowLabels { get; set; }
		[NinjaScriptProperty]
		[TypeConverter(typeof(OrcaMgiFontFamilyConverter))]
		[Display(Name="Font Name", Order=2, GroupName="17. Labels")]
		public string LabelFontName { get; set; }
		[Range(6,24)][Display(Name="Font Size", Order=3, GroupName="17. Labels")]
		public int LabelFontSize { get; set; }
		[XmlIgnore][Display(Name="Label Color", Order=4, GroupName="17. Labels")]
		public WpfBrush LabelColor { get; set; }
		[Browsable(false)] public string LabelColorS { get { return Serialize.BrushToString(LabelColor); } set { LabelColor = Serialize.StringToBrush(value); } }
		[Range(0,100)][Display(Name="Label Opacity", Order=5, GroupName="17. Labels")]
		public int LabelOpacity { get; set; }
		[Range(0, 100)][Display(Name="Label X Offset", Order=6, GroupName="17. Labels")]
		public int LabelXOffset { get; set; }
		[Display(Name="Abbreviate Labels", Order=7, GroupName="17. Labels")]
		public bool AbbreviateLabels { get; set; }
		[Display(Name="Show Price in Label", Order=8, GroupName="17. Labels")]
		public bool ShowPriceInLabel { get; set; }

		// --- 18. Plot Style ---
		[Display(Name="Show Session Break Line", Order=1, GroupName="18. Plot Style")]
		public bool ShowSessionLine { get; set; }
		[XmlIgnore][Display(Name="Session Line Color", Order=2, GroupName="18. Plot Style")]
		public WpfBrush SessionLineColor { get; set; }
		[Browsable(false)] public string SessionLineColorS { get { return Serialize.BrushToString(SessionLineColor); } set { SessionLineColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty][Display(Name="Plot Style", Description="Regular = full lines, Edge = right edge only", Order=3, GroupName="18. Plot Style")]
		public MgiPlotStyle MgiStyle { get; set; }
		[NinjaScriptProperty]
		[Range(40, 600)]
		[Display(Name="Edge Line Length", Description="Horizontal pixel length for Edge plot style stubs.", Order=4, GroupName="18. Plot Style")]
		public int EdgeLineLength { get; set; }
		[Range(1,5)][Display(Name="Main Line Width", Order=5, GroupName="18. Plot Style")]
		public int MainLineWidth { get; set; }
		[Range(1,5)][Display(Name="Secondary Line Width", Order=6, GroupName="18. Plot Style")]
		public int SecondaryLineWidth { get; set; }
		[Range(1,5)][Display(Name="VA Line Width", Order=7, GroupName="18. Plot Style")]
		public int VALineWidth { get; set; }
		[Display(Name="Main Dash Style", Order=8, GroupName="18. Plot Style")]
		public MgiDashStyle MainDashStyle { get; set; }
		[Display(Name="VA Dash Style", Order=9, GroupName="18. Plot Style")]
		public MgiDashStyle VADashStyle { get; set; }
		[Range(0,100)][Display(Name="Line Opacity", Order=10, GroupName="18. Plot Style")]
		public int LineOpacity { get; set; }
		[Display(Name="Draw Behind Candles", Description="Attempts to place MGI lines behind chart bars using NinjaTrader z-order.", Order=11, GroupName="18. Plot Style")]
		public bool DrawBehindCandles { get; set; }
		[Range(50,100)][Display(Name="Value Area %", Description="Percentage for VA calculation (default 70)", Order=12, GroupName="18. Plot Style")]
		public int ValueAreaPct { get; set; }

		// --- 19. Mid Levels ---
		[Display(Name="Show RTH Mid", Order=1, GroupName="19. Mid Levels")]
		public bool ShowRthMid { get; set; }
		[XmlIgnore][Display(Name="RTH Mid Color", Order=2, GroupName="19. Mid Levels")]
		public WpfBrush RthMidColor { get; set; }
		[Browsable(false)] public string RthMidColorS { get { return Serialize.BrushToString(RthMidColor); } set { RthMidColor = Serialize.StringToBrush(value); } }

		[Display(Name="Show ETH Mid", Order=3, GroupName="19. Mid Levels")]
		public bool ShowEthMid { get; set; }
		[XmlIgnore][Display(Name="ETH Mid Color", Order=4, GroupName="19. Mid Levels")]
		public WpfBrush EthMidColor { get; set; }
		[Browsable(false)] public string EthMidColorS { get { return Serialize.BrushToString(EthMidColor); } set { EthMidColor = Serialize.StringToBrush(value); } }
		// --- 20. Custom Labels ---
		[Display(Name="ON High",         Order=1,  GroupName="20. Custom Labels")] public string LblONH   { get; set; }
		[Display(Name="ON Low",          Order=2,  GroupName="20. Custom Labels")] public string LblONL   { get; set; }
		[Display(Name="ON Mid",          Order=3,  GroupName="20. Custom Labels")] public string LblONM   { get; set; }
		[Display(Name="ON VAH",          Order=4,  GroupName="20. Custom Labels")] public string LblOVAH  { get; set; }
		[Display(Name="ON VAL",          Order=5,  GroupName="20. Custom Labels")] public string LblOVAL  { get; set; }
		[Display(Name="ON POC",          Order=6,  GroupName="20. Custom Labels")] public string LblOPOC  { get; set; }
		[Display(Name="OR High",         Order=7,  GroupName="20. Custom Labels")] public string LblORH   { get; set; }
		[Display(Name="OR Low",          Order=8,  GroupName="20. Custom Labels")] public string LblORL   { get; set; }
		[Display(Name="OR Mid",          Order=9,  GroupName="20. Custom Labels")] public string LblORM   { get; set; }
		[Display(Name="IB High",         Order=10, GroupName="20. Custom Labels")] public string LblIBH   { get; set; }
		[Display(Name="IB Low",          Order=11, GroupName="20. Custom Labels")] public string LblIBL   { get; set; }
		[Display(Name="IB Mid",          Order=12, GroupName="20. Custom Labels")] public string LblIBM   { get; set; }
		[Display(Name="RTH Open",        Order=13, GroupName="20. Custom Labels")] public string LblRTHO  { get; set; }
		[Display(Name="RTH High",        Order=14, GroupName="20. Custom Labels")] public string LblRTHH  { get; set; }
		[Display(Name="RTH Low",         Order=15, GroupName="20. Custom Labels")] public string LblRTHL  { get; set; }
		[Display(Name="ETH Open",        Order=16, GroupName="20. Custom Labels")] public string LblETHO  { get; set; }
		[Display(Name="High Of Day",     Order=17, GroupName="20. Custom Labels")] public string LblETHH  { get; set; }
		[Display(Name="Low Of Day",      Order=18, GroupName="20. Custom Labels")] public string LblETHL  { get; set; }
		[Display(Name="True Daily Open", Order=19, GroupName="20. Custom Labels")] public string LblTDO   { get; set; }
		[Display(Name="Prior RTH High",  Order=20, GroupName="20. Custom Labels")] public string LblPDH   { get; set; }
		[Display(Name="Prior RTH Low",   Order=21, GroupName="20. Custom Labels")] public string LblPDL   { get; set; }
		[Display(Name="Prior RTH Close", Order=22, GroupName="20. Custom Labels")] public string LblPDC   { get; set; }
		[Display(Name="Prior RTH Open",  Order=23, GroupName="20. Custom Labels")] public string LblPDO   { get; set; }
		[Display(Name="Prior RTH Mid",   Order=24, GroupName="20. Custom Labels")] public string LblPDM   { get; set; }
		[Display(Name="Prior Day High",  Order=25, GroupName="20. Custom Labels")] public string LblPEH   { get; set; }
		[Display(Name="Prior Day Low",   Order=26, GroupName="20. Custom Labels")] public string LblPEL   { get; set; }
		[Display(Name="Prior Day Close", Order=27, GroupName="20. Custom Labels")] public string LblPEC   { get; set; }
		[Display(Name="RTH VAH",         Order=28, GroupName="20. Custom Labels")] public string LblRVAH  { get; set; }
		[Display(Name="RTH VAL",         Order=29, GroupName="20. Custom Labels")] public string LblRVAL  { get; set; }
		[Display(Name="RTH POC",         Order=30, GroupName="20. Custom Labels")] public string LblRPOC  { get; set; }
		[Display(Name="ETH VAH",         Order=31, GroupName="20. Custom Labels")] public string LblEVAH  { get; set; }
		[Display(Name="ETH VAL",         Order=32, GroupName="20. Custom Labels")] public string LblEVAL  { get; set; }
		[Display(Name="ETH POC",         Order=33, GroupName="20. Custom Labels")] public string LblEPOC  { get; set; }
		[Display(Name="Prior RTH VAH",   Order=34, GroupName="20. Custom Labels")] public string LblPRVAH { get; set; }
		[Display(Name="Prior RTH VAL",   Order=35, GroupName="20. Custom Labels")] public string LblPRVAL { get; set; }
		[Display(Name="Prior RTH POC",   Order=36, GroupName="20. Custom Labels")] public string LblPRPOC { get; set; }
		[Display(Name="Prior Day VAH",   Order=37, GroupName="20. Custom Labels")] public string LblPEVAH { get; set; }
		[Display(Name="Prior Day VAL",   Order=38, GroupName="20. Custom Labels")] public string LblPEVAL { get; set; }
		[Display(Name="Prior Day POC",   Order=39, GroupName="20. Custom Labels")] public string LblPEPOC { get; set; }
		[Display(Name="Weekly Open",     Order=40, GroupName="20. Custom Labels")] public string LblCWO   { get; set; }
		[Display(Name="Weekly High",     Order=41, GroupName="20. Custom Labels")] public string LblCWH   { get; set; }
		[Display(Name="Weekly Low",      Order=42, GroupName="20. Custom Labels")] public string LblCWL   { get; set; }
		[Display(Name="Weekly Mid",      Order=43, GroupName="20. Custom Labels")] public string LblCWM   { get; set; }
		[Display(Name="Prior Weekly Open",  Order=44, GroupName="20. Custom Labels")] public string LblPWO { get; set; }
		[Display(Name="Prior Weekly High",  Order=45, GroupName="20. Custom Labels")] public string LblPWH { get; set; }
		[Display(Name="Prior Weekly Low",   Order=46, GroupName="20. Custom Labels")] public string LblPWL { get; set; }
		[Display(Name="Prior Weekly Close", Order=47, GroupName="20. Custom Labels")] public string LblPWC { get; set; }
		[Display(Name="Prior Weekly Mid",   Order=48, GroupName="20. Custom Labels")] public string LblPWM { get; set; }
		[Display(Name="Weekly VAH",      Order=49, GroupName="20. Custom Labels")] public string LblCWVAH { get; set; }
		[Display(Name="Weekly VAL",      Order=50, GroupName="20. Custom Labels")] public string LblCWVAL { get; set; }
		[Display(Name="Weekly POC",      Order=51, GroupName="20. Custom Labels")] public string LblCWPOC { get; set; }
		[Display(Name="Prior Weekly VAH", Order=52, GroupName="20. Custom Labels")] public string LblPWVAH { get; set; }
		[Display(Name="Prior Weekly VAL", Order=53, GroupName="20. Custom Labels")] public string LblPWVAL { get; set; }
		[Display(Name="Prior Weekly POC", Order=54, GroupName="20. Custom Labels")] public string LblPWPOC { get; set; }
		[Display(Name="RTH VWAP",        Order=55, GroupName="20. Custom Labels")] public string LblVWAP  { get; set; }
		[Display(Name="ETH VWAP",        Order=56, GroupName="20. Custom Labels")] public string LblEVWAP { get; set; }
		[Display(Name="Half Gap",        Order=57, GroupName="20. Custom Labels")] public string LblHGAP  { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaMGIDaily[] cacheOrcaMGIDaily;
		public OrcaMGIDaily OrcaMGIDaily(TimeSpan rTHOpenTime, TimeSpan rTHCloseTime, TimeSpan eTHOpenTime, MgiORDuration oRDuration, MgiPdcMode priorDayCloseMode, bool useLatestCloseForPdc, string labelFontName, MgiPlotStyle mgiStyle, int edgeLineLength)
		{
			return OrcaMGIDaily(Input, rTHOpenTime, rTHCloseTime, eTHOpenTime, oRDuration, priorDayCloseMode, useLatestCloseForPdc, labelFontName, mgiStyle, edgeLineLength);
		}

		public OrcaMGIDaily OrcaMGIDaily(ISeries<double> input, TimeSpan rTHOpenTime, TimeSpan rTHCloseTime, TimeSpan eTHOpenTime, MgiORDuration oRDuration, MgiPdcMode priorDayCloseMode, bool useLatestCloseForPdc, string labelFontName, MgiPlotStyle mgiStyle, int edgeLineLength)
		{
			if (cacheOrcaMGIDaily != null)
				for (int idx = 0; idx < cacheOrcaMGIDaily.Length; idx++)
					if (cacheOrcaMGIDaily[idx] != null && cacheOrcaMGIDaily[idx].RTHOpenTime == rTHOpenTime && cacheOrcaMGIDaily[idx].RTHCloseTime == rTHCloseTime && cacheOrcaMGIDaily[idx].ETHOpenTime == eTHOpenTime && cacheOrcaMGIDaily[idx].ORDuration == oRDuration && cacheOrcaMGIDaily[idx].PriorDayCloseMode == priorDayCloseMode && cacheOrcaMGIDaily[idx].UseLatestCloseForPdc == useLatestCloseForPdc && cacheOrcaMGIDaily[idx].LabelFontName == labelFontName && cacheOrcaMGIDaily[idx].MgiStyle == mgiStyle && cacheOrcaMGIDaily[idx].EdgeLineLength == edgeLineLength && cacheOrcaMGIDaily[idx].EqualsInput(input))
						return cacheOrcaMGIDaily[idx];
			return CacheIndicator<OrcaMGIDaily>(new OrcaMGIDaily(){ RTHOpenTime = rTHOpenTime, RTHCloseTime = rTHCloseTime, ETHOpenTime = eTHOpenTime, ORDuration = oRDuration, PriorDayCloseMode = priorDayCloseMode, UseLatestCloseForPdc = useLatestCloseForPdc, LabelFontName = labelFontName, MgiStyle = mgiStyle, EdgeLineLength = edgeLineLength }, input, ref cacheOrcaMGIDaily);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaMGIDaily OrcaMGIDaily(TimeSpan rTHOpenTime, TimeSpan rTHCloseTime, TimeSpan eTHOpenTime, MgiORDuration oRDuration, MgiPdcMode priorDayCloseMode, bool useLatestCloseForPdc, string labelFontName, MgiPlotStyle mgiStyle, int edgeLineLength)
		{
			return indicator.OrcaMGIDaily(Input, rTHOpenTime, rTHCloseTime, eTHOpenTime, oRDuration, priorDayCloseMode, useLatestCloseForPdc, labelFontName, mgiStyle, edgeLineLength);
		}

		public Indicators.OrcaMGIDaily OrcaMGIDaily(ISeries<double> input , TimeSpan rTHOpenTime, TimeSpan rTHCloseTime, TimeSpan eTHOpenTime, MgiORDuration oRDuration, MgiPdcMode priorDayCloseMode, bool useLatestCloseForPdc, string labelFontName, MgiPlotStyle mgiStyle, int edgeLineLength)
		{
			return indicator.OrcaMGIDaily(input, rTHOpenTime, rTHCloseTime, eTHOpenTime, oRDuration, priorDayCloseMode, useLatestCloseForPdc, labelFontName, mgiStyle, edgeLineLength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaMGIDaily OrcaMGIDaily(TimeSpan rTHOpenTime, TimeSpan rTHCloseTime, TimeSpan eTHOpenTime, MgiORDuration oRDuration, MgiPdcMode priorDayCloseMode, bool useLatestCloseForPdc, string labelFontName, MgiPlotStyle mgiStyle, int edgeLineLength)
		{
			return indicator.OrcaMGIDaily(Input, rTHOpenTime, rTHCloseTime, eTHOpenTime, oRDuration, priorDayCloseMode, useLatestCloseForPdc, labelFontName, mgiStyle, edgeLineLength);
		}

		public Indicators.OrcaMGIDaily OrcaMGIDaily(ISeries<double> input , TimeSpan rTHOpenTime, TimeSpan rTHCloseTime, TimeSpan eTHOpenTime, MgiORDuration oRDuration, MgiPdcMode priorDayCloseMode, bool useLatestCloseForPdc, string labelFontName, MgiPlotStyle mgiStyle, int edgeLineLength)
		{
			return indicator.OrcaMGIDaily(input, rTHOpenTime, rTHCloseTime, eTHOpenTime, oRDuration, priorDayCloseMode, useLatestCloseForPdc, labelFontName, mgiStyle, edgeLineLength);
		}
	}
}

#endregion
