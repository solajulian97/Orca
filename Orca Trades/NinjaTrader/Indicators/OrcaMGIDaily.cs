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
	public enum MgiORDuration { Min1 = 1, Min5 = 5, Min15 = 15, Min30 = 30 }
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
			public int RthOpenIdx = -1, IbEndIdx = -1, EthOpenIdx = -1;
			public DateTime Date;
		}
		#endregion

		#region Fields
		// Session data
		private LevelSet curRTH, curETH, priorRTH, priorETH;
		private LevelSet overnight;
		private double orHigh = double.NaN, orLow = double.NaN, orMid = double.NaN;
		private double ibHigh = double.NaN, ibLow = double.NaN, ibMid = double.NaN;
		private double halfGap = double.NaN;
		private bool orComplete, ibComplete;
		private DateTime rthOpenTime, rthCloseTime, ethOpenTime;
		private DateTime curSessionDate = DateTime.MinValue;
		private bool inRTH, inETH;
		private int orDurationSec, orBarCount;
		private DateTime orStartTime, ibEndTime;

		// Prior day VA (stored separately for stability)
		private double priorRTH_VAH = double.NaN, priorRTH_VAL = double.NaN, priorRTH_POC = double.NaN;
		private double priorETH_VAH = double.NaN, priorETH_VAL = double.NaN, priorETH_POC = double.NaN;

		// DX resources
		private DxSolidBrush[] dxBrushes;
		private SharpDX.Direct2D1.StrokeStyle[] dxStrokes;
		private SharpDX.DirectWrite.TextFormat dxLabelFormat;
		private DxSolidBrush dxLabelBrush, dxRegionBrush;
		private bool dxValid;

		// Level rendering cache
		private struct LevelInfo
		{
			public double Price;
			public string Label;
			public int BrushIdx;
			public int StrokeIdx;
			public int Width;
			public bool Enabled;
		}
		private LevelInfo[] levelCache;
		private const int LVL_COUNT = 45;

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
		// 43-44 spare

		private List<SessionInfo> sessionHistory = new List<SessionInfo>();
		private SessionInfo curSessionInfo;
		private int lastBarIdx = -1;

		private Series<double> rthMidSeries, ethMidSeries;

		// Translates PriorDayCloseMode enum to the TimeSpan used for the PDC capture boundary.
		// Independent of RTHCloseTime which controls the RTH session window for H/L/VA tracking.
		private TimeSpan PdcTimeSpan
		{
			get
			{
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
				ShowCurRTH = true; ShowCurETH = false; ShowPriorRTH = true; ShowPriorETH = false;
				ShowCurRTHVA = true; ShowCurETHVA = false; ShowPriorRTHVA = true; ShowPriorETHVA = false;
				ShowRTHVwap = true; ShowETHVwap = false; ShowHalfGap = true;
				ShowSessionMarkers = true; ShowLabels = true;
				ORDuration = MgiORDuration.Min30;
				MgiStyle = MgiPlotStyle.Regular;
				ValueAreaPct = 70;

				// Region fills
				ONRegionOpacity = 15; IBRegionOpacity = 10;

				// Label settings
				LabelFontName = "Segoe UI"; LabelFontSize = 10; LabelXOffset = 20;

				// Colors
				ONColor = WpfBrushes.SteelBlue; ONVAColor = WpfBrushes.SlateBlue;
				ORColor = WpfBrushes.Goldenrod; IBColor = WpfBrushes.MediumSeaGreen;
				CurRTHColor = WpfBrushes.White; CurETHColor = WpfBrushes.LightGray;
				PriorRTHColor = WpfBrushes.SandyBrown; PriorETHColor = WpfBrushes.DarkGray;
				CurRTHVAColor = WpfBrushes.CornflowerBlue; CurETHVAColor = WpfBrushes.MediumSlateBlue;
				PriorRTHVAColor = WpfBrushes.Peru; PriorETHVAColor = WpfBrushes.RosyBrown;
				RTHVwapColor = WpfBrushes.Orchid; ETHVwapColor = WpfBrushes.MediumOrchid;
				HalfGapColor = WpfBrushes.IndianRed;
				SessionMarkerColor = WpfBrushes.DimGray;
				LabelColor = WpfBrushes.WhiteSmoke;
				SessionLineColor = WpfBrushes.SkyBlue;

				// Line settings
				MainLineWidth = 2; SecondaryLineWidth = 1; VALineWidth = 1;
				MainDashStyle = MgiDashStyle.Solid; VADashStyle = MgiDashStyle.Dash;
				
				ShowSessionLine = false;
				AbbreviateLabels = true;
				ShowPriceInLabel = false;

				PriorDayCloseMode = MgiPdcMode.CME415PM; // default: CME futures RTH close at 4:15 PM
				
				ShowRthMid = true; RthMidColor = WpfBrushes.DarkGoldenrod;
				ShowEthMid = true; EthMidColor = WpfBrushes.DarkSlateBlue;

				// Default custom labels (users can rename any of these)
				LblONH = "ONH"; LblONL = "ONL"; LblONM = "ONM";
				LblOVAH = "OVAH"; LblOVAL = "OVAL"; LblOPOC = "OPOC";
				LblORH = "ORH"; LblORL = "ORL"; LblORM = "ORM";
				LblIBH = "IBH"; LblIBL = "IBL"; LblIBM = "IBM";
				LblRTHO = "RTHO"; LblRTHH = "RTHH"; LblRTHL = "RTHL";
				LblETHO = "ETH.Open"; LblETHH = "ETHH"; LblETHL = "ETHL";
				LblPDH = "PDH"; LblPDL = "PDL"; LblPDC = "PDC"; LblPDO = "PDO"; LblPDM = "PDM";
				LblPEH = "PEH"; LblPEL = "PEL"; LblPEC = "PEC";
				LblRVAH = "RVAH"; LblRVAL = "RVAL"; LblRPOC = "RPOC";
				LblEVAH = "EVAH"; LblEVAL = "EVAL"; LblEPOC = "EPOC";
				LblPRVAH = "pRVAH"; LblPRVAL = "pRVAL"; LblPRPOC = "pRPOC";
				LblPEVAH = "pEVAH"; LblPEVAL = "pEVAL"; LblPEPOC = "pEPOC";
				LblVWAP = "VWAP"; LblEVWAP = "eVWAP"; LblHGAP = "½GAP";

				AddPlot(new Stroke(WpfBrushes.Transparent, 1), PlotStyle.Line, "MGIDummy");
			}
			else if (State == State.DataLoaded)
			{
				curRTH = new LevelSet(); curETH = new LevelSet();
				priorRTH = new LevelSet(); priorETH = new LevelSet();
				overnight = new LevelSet();
				levelCache = new LevelInfo[LVL_COUNT];
				orComplete = false; ibComplete = false;
				
				rthMidSeries = new Series<double>(this);
				ethMidSeries = new Series<double>(this);
			}
			else if (State == State.Terminated)
			{
				DisposeDx();
			}
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

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) return;
			DateTime t = Time[0];
			TimeSpan tod = t.TimeOfDay;
			DateTime prevT = Time[1];
			TimeSpan prevTod = prevT.TimeOfDay;
			double h = High[0], l = Low[0], c = Close[0], vol = Volume[0];
			double typPrice = (h + l + c) / 3.0;

			// Detect RTH open crossing
			bool rthCrossed = CrossedTime(prevTod, tod, RTHOpenTime);
			bool ethCrossed = CrossedTime(prevTod, tod, ETHOpenTime);

			if (ethCrossed)
			{
				// At Globex 18:00 boundary, also update priorRTH so PDH reflects today's completed RTH.
				CopyRthToPrior();

				// Snapshot the full-day ETH into priorETH BEFORE resetting
				CopyEthToPrior();

				// Reset curETH for the new full-day session starting now
				curETH.ResetPrices();
				// Capture exact open price from the crossing bar
				curETH.UpdateHL(h, l, c, Open[0]);
				inETH = true;
				if (curSessionInfo == null || curSessionInfo.Date != t.Date)
				{
					curSessionInfo = new SessionInfo { Date = t.Date, EthOpenIdx = CurrentBar };
					sessionHistory.Add(curSessionInfo);
				}
				else curSessionInfo.EthOpenIdx = CurrentBar;
			}

			if (rthCrossed)
			{
				// Snapshot RTH into priorRTH
				CopyRthToPrior();

				// Reset RTH for the new session
				curRTH.ResetPrices(); overnight.ResetPrices();
				orHigh = orLow = orMid = ibHigh = ibLow = ibMid = double.NaN;
				orComplete = false; ibComplete = false;
				orStartTime = t;
				ibEndTime = t.Date + RTHOpenTime + TimeSpan.FromMinutes(60);
				halfGap = double.NaN;
				inRTH = true;
				curSessionDate = t.Date;

				// Cache Session Info
				if (curSessionInfo == null || curSessionInfo.Date != t.Date)
				{
					curSessionInfo = new SessionInfo { Date = t.Date, RthOpenIdx = CurrentBar };
					sessionHistory.Add(curSessionInfo);
				}
				else curSessionInfo.RthOpenIdx = CurrentBar;
				if (sessionHistory.Count > 50) sessionHistory.RemoveAt(0);

				// Seed the new curRTH with the crossing bar's exact Open price.
				// NinjaTrader timestamps bars at their CLOSE, so this bar's Open[0] is
				// the actual first trade of the RTH session.
				curRTH.UpdateHL(h, l, c, Open[0]);
				curRTH.Vwap.Add(typPrice, vol);
				DistributeVolume(curRTH, h, l, vol);

				// Half gap calc
				if (!double.IsNaN(priorRTH.Close))
				{
					double gap = c - priorRTH.Close;
					if (Math.Abs(gap) > TickSize * 2)
						halfGap = priorRTH.Close + gap * 0.5;
				}
			}
			
			// Detect IB End (10:30)
			if (inRTH && !ibComplete && tod >= (RTHOpenTime + TimeSpan.FromMinutes(60)))
			{
				if (curSessionInfo != null) curSessionInfo.IbEndIdx = CurrentBar;
				ibComplete = true;
			}

			// Determine session state
			inRTH = IsInTimeWindow(tod, RTHOpenTime, RTHCloseTime);
			bool isOvernight = !inRTH;

			// RTH close crossing — use PdcTimeSpan (independent of RTHCloseTime which defines the session window)
			if (CrossedTime(prevTod, tod, PdcTimeSpan))
			{
				curRTH.Close = c;
			}

			// Update current levels
			if (inRTH && !rthCrossed)  // rthCrossed bar is already handled above
			{
				curRTH.UpdateHL(h, l, c);
				curRTH.Vwap.Add(typPrice, vol);
				DistributeVolume(curRTH, h, l, vol);

				// Opening Range
				if (!orComplete)
				{
					TimeSpan orEnd = RTHOpenTime + TimeSpan.FromMinutes((int)ORDuration);
					if (tod <= orEnd)
					{
						if (double.IsNaN(orHigh) || h > orHigh) orHigh = h;
						if (double.IsNaN(orLow) || l < orLow) orLow = l;
						orMid = (orHigh + orLow) / 2.0;
					}
					else orComplete = true;
				}

				// Initial Balance (first 60 min)
				if (!ibComplete)
				{
					TimeSpan ibEnd = RTHOpenTime + TimeSpan.FromMinutes(60);
					if (tod <= ibEnd)
					{
						if (double.IsNaN(ibHigh) || h > ibHigh) ibHigh = h;
						if (double.IsNaN(ibLow) || l < ibLow) ibLow = l;
						ibMid = (ibHigh + ibLow) / 2.0;
					}
					else ibComplete = true;
				}
			}

			// ETH full-day tracking (already updated on ethCrossed above; don't double-count)
			if (!ethCrossed)
			{
				curETH.UpdateHL(h, l, c);
				curETH.Vwap.Add(typPrice, vol);
				DistributeVolume(curETH, h, l, vol);
			}
			else
			{
				// Already called UpdateHL above for the crossing bar; still add vol
				curETH.Vwap.Add(typPrice, vol);
				DistributeVolume(curETH, h, l, vol);
			}

			// Overnight tracking (between prior RTH close and current RTH open)
			if (isOvernight)
			{
				overnight.UpdateHL(h, l, c);
				DistributeVolume(overnight, h, l, vol);
			}

			// Dynamic Mids
			ethMidSeries[0] = (!double.IsNaN(curETH.High) && !double.IsNaN(curETH.Low)) ? (curETH.High + curETH.Low) * 0.5 : double.NaN;
			if (inRTH)
				rthMidSeries[0] = (!double.IsNaN(curRTH.High) && !double.IsNaN(curRTH.Low)) ? (curRTH.High + curRTH.Low) * 0.5 : double.NaN;
			else
				rthMidSeries[0] = double.NaN;

			// Recalc value areas periodically
			if (CurrentBar != lastBarIdx)
			{
				lastBarIdx = CurrentBar;
				if (inRTH && curRTH.VolByPrice.Count > 2) CalcVA(curRTH);
				if (curETH.VolByPrice.Count > 2) CalcVA(curETH);
				if (isOvernight && overnight.VolByPrice.Count > 2) CalcVA(overnight);
			}

			// Build level cache for rendering
			BuildLevelCache();
		}

		private bool CrossedTime(TimeSpan prev, TimeSpan cur, TimeSpan target)
		{
			if (target > prev && target <= cur) return true;
			if (prev > cur && (target > prev || target <= cur)) return true;
			return false;
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

		private void BuildLevelCache()
		{
			for (int i = 0; i < LVL_COUNT; i++) levelCache[i].Enabled = false;

			if (ShowONRange)  { SetLvl(L_ONH,    overnight.High,    LblONH,    0); SetLvl(L_ONL,    overnight.Low,    LblONL,    0); SetLvl(L_ONMID,  overnight.Mid,    LblONM,    0); }
			if (ShowONVA)     { SetLvl(L_ONVAH,  overnight.VAH,    LblOVAH,   1); SetLvl(L_ONVAL,  overnight.VAL,    LblOVAL,   1); SetLvl(L_ONPOC,  overnight.POC,    LblOPOC,   1); }
			if (ShowOR)       { SetLvl(L_ORH,    orHigh,           LblORH,    2); SetLvl(L_ORL,    orLow,            LblORL,    2); SetLvl(L_ORMID,  orMid,            LblORM,    2); }
			if (ShowIB)       { SetLvl(L_IBH,    ibHigh,           LblIBH,    3); SetLvl(L_IBL,    ibLow,            LblIBL,    3); SetLvl(L_IBMID,  ibMid,            LblIBM,    3); }
			if (ShowCurRTH)   { SetLvl(L_CRTH_O, curRTH.Open,      LblRTHO,   4); SetLvl(L_CRTH_H, curRTH.High,      LblRTHH,   4); SetLvl(L_CRTH_L, curRTH.Low,       LblRTHL,   4); }
			if (ShowCurETH)   { SetLvl(L_CETH_O, curETH.Open,      LblETHO,   5); SetLvl(L_CETH_H, curETH.High,      LblETHH,   5); SetLvl(L_CETH_L, curETH.Low,       LblETHL,   5); }
			if (ShowPriorRTH) { SetLvl(L_PDH,    priorRTH.High,    LblPDH,    6); SetLvl(L_PDL,    priorRTH.Low,     LblPDL,    6); SetLvl(L_PDC,    priorRTH.Close,   LblPDC,    6); SetLvl(L_PDO,    priorRTH.Open,    LblPDO,    6); SetLvl(L_PDMID,  priorRTH.Mid,    LblPDM,    6); }
			if (ShowPriorETH) { SetLvl(L_PETH_H, priorETH.High,    LblPEH,    7); SetLvl(L_PETH_L, priorETH.Low,     LblPEL,    7); SetLvl(L_PETH_C, priorETH.Close,   LblPEC,    7); }
			if (ShowCurRTHVA) { SetLvl(L_CRVAH,  curRTH.VAH,       LblRVAH,   8); SetLvl(L_CRVAL,  curRTH.VAL,       LblRVAL,   8); SetLvl(L_CRPOC,  curRTH.POC,       LblRPOC,   8); }
			if (ShowCurETHVA) { SetLvl(L_CEVAH,  curETH.VAH,       LblEVAH,   9); SetLvl(L_CEVAL,  curETH.VAL,       LblEVAL,   9); SetLvl(L_CEPOC,  curETH.POC,       LblEPOC,   9); }
			if (ShowPriorRTHVA) { SetLvl(L_PRVAH, priorRTH_VAH,    LblPRVAH, 10); SetLvl(L_PRVAL,  priorRTH_VAL,    LblPRVAL, 10); SetLvl(L_PRPOC,  priorRTH_POC,    LblPRPOC, 10); }
			if (ShowPriorETHVA) { SetLvl(L_PEVAH, priorETH_VAH,    LblPEVAH, 11); SetLvl(L_PEVAL,  priorETH_VAL,    LblPEVAL, 11); SetLvl(L_PEPOC,  priorETH_POC,    LblPEPOC, 11); }
			if (ShowRTHVwap) SetLvl(L_RVWAP, curRTH.Vwap.Value, LblVWAP,  12);
			if (ShowETHVwap) SetLvl(L_EVWAP, curETH.Vwap.Value, LblEVWAP, 13);
			if (ShowHalfGap) SetLvl(L_HGAP,  halfGap,           LblHGAP,  14);
		}

		private void SetLvl(int idx, double price, string label, int colorGroup)
		{
			if (double.IsNaN(price)) return;
			levelCache[idx].Price = price;
			levelCache[idx].Label = label + (ShowPriceInLabel ? " " + FormatPrice(price) : "");
			levelCache[idx].BrushIdx = colorGroup;
			levelCache[idx].Enabled = true;
		}

		private string FormatPrice(double p)
		{
			return Instrument != null ? Instrument.MasterInstrument.FormatPrice(p) : p.ToString("F2");
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl cc, ChartScale cs)
		{
			base.OnRender(cc, cs);
			if (cc == null || cs == null || ChartBars == null || levelCache == null) return;
			EnsureDx();
			if (!dxValid) return;

			float CR = (float)cc.CanvasRight;
			float pT = ChartPanel.Y, pB = pT + ChartPanel.H, pL = ChartPanel.X;
			
			// ONLY draw for the LATEST session in history
			SessionInfo si = sessionHistory.LastOrDefault();
			if (si == null) return;

			float rthX = si.RthOpenIdx != -1 ? cc.GetXByBarIndex(ChartBars, si.RthOpenIdx) : -1;
			float ethX = si.EthOpenIdx != -1 ? cc.GetXByBarIndex(ChartBars, si.EthOpenIdx) : -1;
			float ibEX = si.IbEndIdx != -1 ? cc.GetXByBarIndex(ChartBars, si.IbEndIdx) : -1;
			float nowX = cc.GetXByBarIndex(ChartBars, ChartBars.ToIndex);

			// Fallback: If no RTHOpen, use EthOpen or PanelLeft
			float effectiveStartX = rthX > 0 ? rthX : (ethX > 0 ? ethX : pL);

			var oldAA = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = AntialiasMode.Aliased;

			// Vert line at session open (if reached)
			if (ShowSessionLine && rthX > 0 && dxBrushes.Length > 16 && dxBrushes[16] != null)
				RenderTarget.DrawLine(new Vector2(rthX, pT), new Vector2(rthX, pB), dxBrushes[16], 1f);

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

				float sX = -1, eX = nowX;

				// 1. Overnight Range / VA
				if (i >= L_ONH && i <= L_ONPOC)
				{ sX = ethX > 0 ? ethX : pL; eX = nowX; }
				// 2. IB Levels
				else if (i == L_IBH || i == L_IBL || i == L_IBMID)
				{ if (si.IbEndIdx == -1) continue; sX = ibEX; eX = nowX; }
				// 3. Opening Range
				else if (i >= L_ORH && i <= L_ORMID)
				{ sX = rthX > 0 ? rthX : -1; eX = nowX; }
				// 4. Everything else (RTH, Prior Day, VWAP, Half Gap)
				else { sX = effectiveStartX; eX = nowX; }

				if (sX < 0 || sX > CR) continue;

				bool isVA = (i >= L_ONVAH && i <= L_ONPOC) || (i >= L_CRVAH && i <= L_CRPOC)
					|| (i >= L_CEVAH && i <= L_CEPOC) || (i >= L_PRVAH && i <= L_PRPOC)
					|| (i >= L_PEVAH && i <= L_PEPOC);
				int w = isVA ? VALineWidth : MainLineWidth;
				var stroke = GetStroke(isVA ? VADashStyle : MainDashStyle);

				RenderTarget.DrawLine(new Vector2(sX, y), new Vector2(eX, y), dxBrushes[bi], w, stroke);

				// Collect label for Pass 2
				pendingLabels?.Add((y, eX, bi, levelCache[i].Label));
			}

			// Draw levels -- Pass 2: labels with horizontal stagger for overlaps.
			// Sort by Y pixel position then cascade nearby labels to the right.
			if (pendingLabels != null && pendingLabels.Count > 0)
			{
				pendingLabels.Sort((a, b) => a.y.CompareTo(b.y));

				float overlapThreshold = LabelFontSize * 1.5f;
				float colStep = LabelFontSize * 5.5f;
				float lastY = float.MinValue;
				int col = 0;

				foreach (var lbl in pendingLabels)
				{
					if (lbl.bi >= dxBrushes.Length || dxBrushes[lbl.bi] == null) continue;

					if (Math.Abs(lbl.y - lastY) < overlapThreshold)
						col++;
					else
						col = 0;

					lastY = lbl.y;
					float txtX = lbl.eX + LabelXOffset + col * colStep;
					var rect = new RectangleF(txtX, lbl.y - LabelFontSize - 1, 200, LabelFontSize + 4);
					RenderTarget.DrawText(lbl.text, dxLabelFormat, rect, dxBrushes[lbl.bi]);
				}
			}

			// Draw Dynamic Mids (Squiggly)
			if (ShowRthMid) DrawSeriesLine(cs, rthMidSeries, si.RthOpenIdx, ChartBars.ToIndex, 17, 1);
			if (ShowEthMid) DrawSeriesLine(cs, ethMidSeries, si.EthOpenIdx, ChartBars.ToIndex, 18, 1);

			RenderTarget.AntialiasMode = oldAA;
		}

		private void DrawSeriesLine(ChartScale cs, Series<double> series, int startIdx, int endIdx, int brushIdx, int width)
		{
			if (startIdx == -1 || brushIdx >= dxBrushes.Length || dxBrushes[brushIdx] == null) return;
			float lastX = -1, lastY = -1;
			var stroke = GetStroke(MgiDashStyle.Solid);

			for (int i = startIdx; i <= endIdx; i++)
			{
				double val = series.GetValueAt(i);
				if (double.IsNaN(val)) { lastX = -1; continue; }
				float x = ChartControl.GetXByBarIndex(ChartBars, i);
				float y = cs.GetYByValue(val);
				if (lastX != -1)
					RenderTarget.DrawLine(new Vector2(lastX, lastY), new Vector2(x, y), dxBrushes[brushIdx], width, stroke);
				lastX = x; lastY = y;
			}
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
			int from = ChartBars.FromIndex, to = ChartBars.ToIndex;
			for (int i = from; i <= to; i++)
			{
				if (i >= Bars.Count || i < 1) continue;
				DateTime t = Bars.GetTime(i);
				DateTime pt = Bars.GetTime(i - 1);
				if (CrossedTime(pt.TimeOfDay, t.TimeOfDay, RTHOpenTime) || CrossedTime(pt.TimeOfDay, t.TimeOfDay, RTHCloseTime))
				{
					float x = cc.GetXByBarIndex(ChartBars, i);
					float prevOp = dxBrushes[15].Opacity;
					dxBrushes[15].Opacity = 0.3f;
					RenderTarget.DrawLine(new Vector2(x, panelTop), new Vector2(x, panelBot), dxBrushes[15], 1f);
					dxBrushes[15].Opacity = prevOp;
				}
			}
		}
		#endregion

		#region DX Resource Management
		private SharpDX.Direct2D1.StrokeStyle GetStroke(MgiDashStyle ds)
		{
			int idx = (int)ds;
			if (idx < dxStrokes.Length && dxStrokes[idx] != null) return dxStrokes[idx];
			return null;
		}

		private Color4 ToColor4(WpfBrush b, float alpha = 1f)
		{
			var c = (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alpha);
		}

		private void EnsureDx()
		{
			if (dxValid || RenderTarget == null) return;
			try
			{
				WpfBrush[] colorMap = { ONColor, ONVAColor, ORColor, IBColor, CurRTHColor, CurETHColor,
					PriorRTHColor, PriorETHColor, CurRTHVAColor, CurETHVAColor, PriorRTHVAColor, PriorETHVAColor,
					RTHVwapColor, ETHVwapColor, HalfGapColor, SessionMarkerColor, SessionLineColor,
					RthMidColor, EthMidColor };
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
				{ TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Near };

				dxValid = true;
			}
			catch { dxValid = false; }
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

		// --- 06-07. Current Day ---
		[Display(Name="Show Current RTH", Description="Show current day RTH Open/High/Low", Order=1, GroupName="06. Current Day RTH")]
		public bool ShowCurRTH { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="06. Current Day RTH")]
		public WpfBrush CurRTHColor { get; set; }
		[Browsable(false)] public string CurRTHColorS { get { return Serialize.BrushToString(CurRTHColor); } set { CurRTHColor = Serialize.StringToBrush(value); } }

		[Display(Name="Show Current ETH", Description="Show current day ETH Open/High/Low", Order=1, GroupName="07. Current Day ETH")]
		public bool ShowCurETH { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="07. Current Day ETH")]
		public WpfBrush CurETHColor { get; set; }
		[Browsable(false)] public string CurETHColorS { get { return Serialize.BrushToString(CurETHColor); } set { CurETHColor = Serialize.StringToBrush(value); } }

		// --- 08-09. Prior Day ---
		[Display(Name="Show Prior Day RTH", Description="Show PDH, PDL, PDC, PDO, PD Mid", Order=1, GroupName="08. Prior Day RTH")]
		public bool ShowPriorRTH { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="08. Prior Day RTH")]
		public WpfBrush PriorRTHColor { get; set; }
		[Browsable(false)] public string PriorRTHColorS { get { return Serialize.BrushToString(PriorRTHColor); } set { PriorRTHColor = Serialize.StringToBrush(value); } }

		[Display(Name="Show Prior Day ETH", Description="Show prior ETH High/Low/Close", Order=1, GroupName="09. Prior Day ETH")]
		public bool ShowPriorETH { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="09. Prior Day ETH")]
		public WpfBrush PriorETHColor { get; set; }
		[Browsable(false)] public string PriorETHColorS { get { return Serialize.BrushToString(PriorETHColor); } set { PriorETHColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Display(Name = "Prior Day Close Definition",
			Description = "Which close price to use as PDC. Equities4PM = 4:00 PM equities close. CME415PM = 4:15 PM CME RTH futures close. Globex5PM = 5:00 PM, last price before the 1-hour CME maintenance break.",
			Order = 3, GroupName = "08. Prior Day RTH")]
		public MgiPdcMode PriorDayCloseMode { get; set; }

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

		[Display(Name="Show Prior ETH VA", Order=1, GroupName="13. Prior ETH VA")]
		public bool ShowPriorETHVA { get; set; }
		[XmlIgnore][Display(Name="Color", Order=2, GroupName="13. Prior ETH VA")]
		public WpfBrush PriorETHVAColor { get; set; }
		[Browsable(false)] public string PriorETHVAColorS { get { return Serialize.BrushToString(PriorETHVAColor); } set { PriorETHVAColor = Serialize.StringToBrush(value); } }

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
		[NinjaScriptProperty][Display(Name="Font Name", Order=2, GroupName="17. Labels")]
		public string LabelFontName { get; set; }
		[Range(6,24)][Display(Name="Font Size", Order=3, GroupName="17. Labels")]
		public int LabelFontSize { get; set; }
		[XmlIgnore][Display(Name="Label Color", Order=4, GroupName="17. Labels")]
		public WpfBrush LabelColor { get; set; }
		[Browsable(false)] public string LabelColorS { get { return Serialize.BrushToString(LabelColor); } set { LabelColor = Serialize.StringToBrush(value); } }
		[Range(0, 100)][Display(Name="Label X Offset", Order=5, GroupName="17. Labels")]
		public int LabelXOffset { get; set; }
		[Display(Name="Abbreviate Labels", Order=6, GroupName="17. Labels")]
		public bool AbbreviateLabels { get; set; }
		[Display(Name="Show Price in Label", Order=7, GroupName="17. Labels")]
		public bool ShowPriceInLabel { get; set; }

		// --- 18. Plot Style ---
		[Display(Name="Show Session Break Line", Order=1, GroupName="18. Plot Style")]
		public bool ShowSessionLine { get; set; }
		[XmlIgnore][Display(Name="Session Line Color", Order=2, GroupName="18. Plot Style")]
		public WpfBrush SessionLineColor { get; set; }
		[Browsable(false)] public string SessionLineColorS { get { return Serialize.BrushToString(SessionLineColor); } set { SessionLineColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty][Display(Name="Plot Style", Description="Regular = full lines, Edge = right edge only", Order=3, GroupName="18. Plot Style")]
		public MgiPlotStyle MgiStyle { get; set; }
		[Range(1,5)][Display(Name="Main Line Width", Order=2, GroupName="18. Plot Style")]
		public int MainLineWidth { get; set; }
		[Range(1,5)][Display(Name="Secondary Line Width", Order=3, GroupName="18. Plot Style")]
		public int SecondaryLineWidth { get; set; }
		[Range(1,5)][Display(Name="VA Line Width", Order=4, GroupName="18. Plot Style")]
		public int VALineWidth { get; set; }
		[Display(Name="Main Dash Style", Order=5, GroupName="18. Plot Style")]
		public MgiDashStyle MainDashStyle { get; set; }
		[Display(Name="VA Dash Style", Order=6, GroupName="18. Plot Style")]
		public MgiDashStyle VADashStyle { get; set; }
		[Range(50,100)][Display(Name="Value Area %", Description="Percentage for VA calculation (default 70)", Order=7, GroupName="18. Plot Style")]
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
		[Display(Name="ETH High",        Order=17, GroupName="20. Custom Labels")] public string LblETHH  { get; set; }
		[Display(Name="ETH Low",         Order=18, GroupName="20. Custom Labels")] public string LblETHL  { get; set; }
		[Display(Name="Prior Day High",  Order=19, GroupName="20. Custom Labels")] public string LblPDH   { get; set; }
		[Display(Name="Prior Day Low",   Order=20, GroupName="20. Custom Labels")] public string LblPDL   { get; set; }
		[Display(Name="Prior Day Close", Order=21, GroupName="20. Custom Labels")] public string LblPDC   { get; set; }
		[Display(Name="Prior Day Open",  Order=22, GroupName="20. Custom Labels")] public string LblPDO   { get; set; }
		[Display(Name="Prior Day Mid",   Order=23, GroupName="20. Custom Labels")] public string LblPDM   { get; set; }
		[Display(Name="Prior ETH High",  Order=24, GroupName="20. Custom Labels")] public string LblPEH   { get; set; }
		[Display(Name="Prior ETH Low",   Order=25, GroupName="20. Custom Labels")] public string LblPEL   { get; set; }
		[Display(Name="Prior ETH Close", Order=26, GroupName="20. Custom Labels")] public string LblPEC   { get; set; }
		[Display(Name="RTH VAH",         Order=27, GroupName="20. Custom Labels")] public string LblRVAH  { get; set; }
		[Display(Name="RTH VAL",         Order=28, GroupName="20. Custom Labels")] public string LblRVAL  { get; set; }
		[Display(Name="RTH POC",         Order=29, GroupName="20. Custom Labels")] public string LblRPOC  { get; set; }
		[Display(Name="ETH VAH",         Order=30, GroupName="20. Custom Labels")] public string LblEVAH  { get; set; }
		[Display(Name="ETH VAL",         Order=31, GroupName="20. Custom Labels")] public string LblEVAL  { get; set; }
		[Display(Name="ETH POC",         Order=32, GroupName="20. Custom Labels")] public string LblEPOC  { get; set; }
		[Display(Name="Prior RTH VAH",   Order=33, GroupName="20. Custom Labels")] public string LblPRVAH { get; set; }
		[Display(Name="Prior RTH VAL",   Order=34, GroupName="20. Custom Labels")] public string LblPRVAL { get; set; }
		[Display(Name="Prior RTH POC",   Order=35, GroupName="20. Custom Labels")] public string LblPRPOC { get; set; }
		[Display(Name="Prior ETH VAH",   Order=36, GroupName="20. Custom Labels")] public string LblPEVAH { get; set; }
		[Display(Name="Prior ETH VAL",   Order=37, GroupName="20. Custom Labels")] public string LblPEVAL { get; set; }
		[Display(Name="Prior ETH POC",   Order=38, GroupName="20. Custom Labels")] public string LblPEPOC { get; set; }
		[Display(Name="RTH VWAP",        Order=39, GroupName="20. Custom Labels")] public string LblVWAP  { get; set; }
		[Display(Name="ETH VWAP",        Order=40, GroupName="20. Custom Labels")] public string LblEVWAP { get; set; }
		[Display(Name="Half Gap",        Order=41, GroupName="20. Custom Labels")] public string LblHGAP  { get; set; }
		#endregion
	}
}
