#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaExecutionLines : Indicator
	{
		private class PendingEntry { public DateTime Time; public double Price; public int Quantity; public MarketPosition Side; }
		private class FillMatch { public DateTime EntryTime; public double EntryPrice; public DateTime ExitTime; public double ExitPrice; public int Quantity; public bool IsLong; public double PnLTicks; public double PnLDollars; }
		private class RoundTrip { public int Number; public bool IsLong, IsComplete, MAEMFECalculated; public string AccountName; public List<FillMatch> Matches = new List<FillMatch>(); public double EntryPriceSum, ExitPriceSum, TotalPnLDollars, TotalPnLTicks, MaxAdverseExcursion, MaxFavorableExcursion; public int EntryQtyTotal, ExitQtyTotal; public DateTime FirstEntryTime, LastExitTime; public double AvgEntryPrice => EntryQtyTotal <= 0 ? 0 : EntryPriceSum / EntryQtyTotal; public double AvgExitPrice => ExitQtyTotal <= 0 ? 0 : ExitPriceSum / ExitQtyTotal; }
		private class AccountState { public string AccountName; public List<PendingEntry> OpenFills = new List<PendingEntry>(); public List<RoundTrip> RoundTrips = new List<RoundTrip>(); public RoundTrip CurrentRT; public int RTCounter, NetPosition; }

		private Dictionary<string, AccountState> accountStates = new Dictionary<string, AccountState>();
		private List<Account> hookedAccounts = new List<Account>();
		private string activeAccountName = "", lastDrawnAccount = "";
		private object tradeLock = new object();
		private bool needsRedraw = false, historyLoaded = false, shotClockActive = false, shotClockIsLive = false;
		private System.Windows.Point mousePosition = new System.Windows.Point(-1, -1);
		private bool isMouseOverChart = false;
		private DateTime lastAccountCheck = DateTime.MinValue, shotClockEnd = DateTime.MinValue;

		[NinjaScriptProperty] [Display(Name = "Show Execution Lines", GroupName = "1. Visibility", Order = 0)] public bool ShowExecutionLines { get; set; }
		[NinjaScriptProperty] [Display(Name = "Show Labels", GroupName = "1. Visibility", Order = 1)] public bool ShowLabels { get; set; }
		[Display(Name = "Hover Individual PnL", GroupName = "1. Visibility", Order = 2)] public bool HoverShowsIndividualPnL { get; set; }
		[NinjaScriptProperty] [Display(Name = "Show Average Markers", GroupName = "1. Visibility", Order = 3)] public bool ShowMarkers { get; set; }
		[NinjaScriptProperty] [Display(Name = "Individual Lines (vs Averaged)", GroupName = "1. Visibility", Order = 4)] public bool ShowIndividualLines { get; set; }
		[NinjaScriptProperty] [Display(Name = "Individual Fill Markers", GroupName = "1. Visibility", Order = 5)] public bool ShowIndividualMarkers { get; set; }
		[NinjaScriptProperty] [Display(Name = "Show MAE/MFE", GroupName = "1. Visibility", Order = 6)] public bool ShowMAEMFE { get; set; }
		[Display(Name = "Show Session Total", GroupName = "1. Visibility", Order = 7)] public bool ShowSessionTotal { get; set; }
		[Display(Name = "Session Total Position", GroupName = "3. Appearance", Order = 2)] public TextPosition SessionTotalPosition { get; set; }
		[NinjaScriptProperty] [Display(Name = "Enable Shot Clock", GroupName = "5. Shot Clock", Order = 0)] public bool EnableShotClock { get; set; }
		[NinjaScriptProperty] [Range(5, 3600)] [Display(Name = "Cooldown Duration (s)", GroupName = "5. Shot Clock", Order = 1)] public int ShotClockSeconds { get; set; }
		[Display(Name = "Label Position", GroupName = "5. Shot Clock", Order = 2)] public TextPosition ShotClockPosition { get; set; }
		[XmlIgnore] [Display(Name = "Countdown Color", GroupName = "5. Shot Clock", Order = 3)] public System.Windows.Media.Brush ShotClockColor { get; set; }
		[Browsable(false)] public string ShotClockColorSerializable { get { return Serialize.BrushToString(ShotClockColor); } set { ShotClockColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Warning Color", GroupName = "5. Shot Clock", Order = 4)] public System.Windows.Media.Brush ShotClockWarningColor { get; set; }
		[Browsable(false)] public string ShotClockWarningColorSerializable { get { return Serialize.BrushToString(ShotClockWarningColor); } set { ShotClockWarningColor = Serialize.StringToBrush(value); } }
		[NinjaScriptProperty] [Display(Name = "Load Live Session", GroupName = "2. Data", Order = 0)] public bool LoadTodayHistory { get; set; }
		[NinjaScriptProperty] [Display(Name = "Load SQLite History", GroupName = "2. Data", Order = 1)] public bool LoadSqliteHistory { get; set; }
		[NinjaScriptProperty] [Range(0, 100000)] [Display(Name = "Risk $ (R-Mult)", GroupName = "2. Data", Order = 2)] public double RiskAmount { get; set; }
		[NinjaScriptProperty] [Range(1, 5)] [Display(Name = "Line Width", GroupName = "3. Appearance", Order = 0)] public int LineWidth { get; set; }
		[NinjaScriptProperty] [Range(8, 20)] [Display(Name = "Label Font Size", GroupName = "3. Appearance", Order = 1)] public int LabelFontSize { get; set; }
		[Range(5, 200)] [Display(Name = "Max Trades", GroupName = "2. Data", Order = 3)] public int MaxTradesToShow { get; set; }
		[XmlIgnore] [Display(Name = "Profit Color", GroupName = "4. Colors", Order = 0)] public System.Windows.Media.Brush ProfitColor { get; set; }
		[Browsable(false)] public string ProfitColorSerializable { get { return Serialize.BrushToString(ProfitColor); } set { ProfitColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Loss Color", GroupName = "4. Colors", Order = 1)] public System.Windows.Media.Brush LossColor { get; set; }
		[Browsable(false)] public string LossColorSerializable { get { return Serialize.BrushToString(LossColor); } set { LossColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Long Marker Color", GroupName = "4. Colors", Order = 2)] public System.Windows.Media.Brush LongMarkerColor { get; set; }
		[Browsable(false)] public string LongMarkerColorSerializable { get { return Serialize.BrushToString(LongMarkerColor); } set { LongMarkerColor = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name = "Short Marker Color", GroupName = "4. Colors", Order = 3)] public System.Windows.Media.Brush ShortMarkerColor { get; set; }
		[Browsable(false)] public string ShortMarkerColorSerializable { get { return Serialize.BrushToString(ShortMarkerColor); } set { ShortMarkerColor = Serialize.StringToBrush(value); } }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Orca Execution History Lines and R-Multiple Tracking";
				Name = "OrcaExecutionLines";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = false;
				ShowExecutionLines = true; ShowLabels = true; ShowMarkers = true; ShowIndividualLines = true; ShowIndividualMarkers = true; ShowMAEMFE = true; ShowSessionTotal = true; SessionTotalPosition = TextPosition.TopRight;
				LoadTodayHistory = true; LoadSqliteHistory = true; EnableShotClock = true; ShotClockSeconds = 300; ShotClockPosition = TextPosition.BottomRight; ShotClockColor = System.Windows.Media.Brushes.Orange; ShotClockWarningColor = System.Windows.Media.Brushes.Red;
				LineWidth = 2; LabelFontSize = 11; MaxTradesToShow = 50; RiskAmount = 200; ProfitColor = System.Windows.Media.Brushes.DodgerBlue; LossColor = System.Windows.Media.Brushes.Tomato; LongMarkerColor = System.Windows.Media.Brushes.Lime; ShortMarkerColor = System.Windows.Media.Brushes.Red;
			}
			else if (State == State.DataLoaded)
			{
				accountStates.Clear(); hookedAccounts.Clear(); needsRedraw = false; historyLoaded = false; shotClockActive = false; shotClockIsLive = false; lastAccountCheck = DateTime.MinValue; HookAll();
				if (ChartControl != null) { ChartControl.Dispatcher.InvokeAsync(() => { ChartControl.MouseMove += OnChartMouseMove; ChartControl.MouseLeave += OnChartMouseLeave; }); }
			}
			else if (State == State.Terminated)
			{
				UnhookAll();
				if (ChartControl != null) { ChartControl.Dispatcher.InvokeAsync(() => { ChartControl.MouseMove -= OnChartMouseMove; ChartControl.MouseLeave -= OnChartMouseLeave; }); }
			}
		}

		private void HookAll() { try { foreach (Account a in Account.All) { a.ExecutionUpdate += OnExecUpdate; hookedAccounts.Add(a); } } catch { } }
		private void UnhookAll() { foreach (Account a in hookedAccounts) { try { a.ExecutionUpdate -= OnExecUpdate; } catch { } } hookedAccounts.Clear(); }
		private void OnExecUpdate(object sender, ExecutionEventArgs e) { try { if (e.Execution != null && e.Execution.Instrument != null && Instrument != null && e.Execution.Instrument.FullName == Instrument.FullName && e.Execution.Order != null) { bool isBuy = e.Execution.Order.OrderAction == OrderAction.Buy || e.Execution.Order.OrderAction == OrderAction.BuyToCover; ProcessExecution(isBuy, e.Execution.Price, e.Execution.Quantity, e.Execution.Time, e.Execution.Account?.Name ?? "Unknown"); } } catch { } }
		private void ProcessExecution(bool isBuy, double price, int qty, DateTime time, string acc) {
			lock (tradeLock) {
				if (!accountStates.ContainsKey(acc)) accountStates[acc] = new AccountState { AccountName = acc };
				AccountState s = accountStates[acc]; int net = s.NetPosition, next = s.NetPosition + (isBuy? qty : -qty);
				if ((net > 0 && !isBuy) || (net < 0 && isBuy)) {
					int closeQty = Math.Min(qty, Math.Abs(net)), rem = qty - closeQty, toClose = closeQty;
					while (toClose > 0 && s.OpenFills.Count > 0) {
						PendingEntry p = s.OpenFills[0]; int take = Math.Min(toClose, p.Quantity); bool isL = p.Side == MarketPosition.Long;
						double ticks = isL? (price - p.Price)/TickSize : (p.Price - price)/TickSize, pnl = ticks * TickSize * Instrument.MasterInstrument.PointValue * take;
						if (s.CurrentRT != null) {
							s.CurrentRT.Matches.Add(new FillMatch { EntryTime = p.Time, EntryPrice = p.Price, ExitTime = time, ExitPrice = price, Quantity = take, IsLong = isL, PnLTicks = ticks, PnLDollars = pnl });
							s.CurrentRT.ExitPriceSum += price * take; s.CurrentRT.ExitQtyTotal += take; s.CurrentRT.LastExitTime = time; s.CurrentRT.TotalPnLDollars += pnl; s.CurrentRT.TotalPnLTicks += ticks * take;
						}
						p.Quantity -= take; toClose -= take; if (p.Quantity <= 0) s.OpenFills.RemoveAt(0);
					}
					if (next == 0 && s.CurrentRT != null) { s.CurrentRT.IsComplete = true; while (s.RoundTrips.Count > MaxTradesToShow) s.RoundTrips.RemoveAt(0); s.CurrentRT = null; if (EnableShotClock && shotClockIsLive) { shotClockEnd = DateTime.UtcNow.AddSeconds(ShotClockSeconds); shotClockActive = true; } }
					if (rem > 0) { StartNewRT(s, isBuy, price, rem, time, acc); s.OpenFills.Add(new PendingEntry { Time = time, Price = price, Quantity = rem, Side = isBuy? MarketPosition.Long : MarketPosition.Short }); }
					needsRedraw = true;
				} else {
					if (net == 0) StartNewRT(s, isBuy, price, qty, time, acc);
					else if (s.CurrentRT != null) { s.CurrentRT.EntryPriceSum += price * qty; s.CurrentRT.EntryQtyTotal += qty; }
					s.OpenFills.Add(new PendingEntry { Time = time, Price = price, Quantity = qty, Side = isBuy? MarketPosition.Long : MarketPosition.Short });
				}
				s.NetPosition = next;
			}
		}
		private void StartNewRT(AccountState s, bool isB, double p, int q, DateTime t, string acc) { s.RTCounter++; s.CurrentRT = new RoundTrip { Number = s.RTCounter, IsLong = isB, AccountName = acc, EntryPriceSum = p * q, EntryQtyTotal = q, FirstEntryTime = t }; s.RoundTrips.Add(s.CurrentRT); }

		protected override void OnBarUpdate() {
			if (!historyLoaded && CurrentBar > 10) { historyLoaded = true; LoadAllHistory(); shotClockIsLive = true; }
			if (DateTime.UtcNow - lastAccountCheck > TimeSpan.FromSeconds(1)) {
				lastAccountCheck = DateTime.UtcNow; string ct = GetCTAcc();
				if (!string.IsNullOrEmpty(ct) && ct != activeAccountName) { activeAccountName = ct; needsRedraw = true; }
			}
			UpdateLiveMAE(); if (needsRedraw) { needsRedraw = false; DrawAll(); }
			if (EnableShotClock && shotClockActive) {
				double rem = (shotClockEnd - DateTime.UtcNow).TotalSeconds;
				if (rem <= 0) { shotClockActive = false; RemoveDrawObject("OrcaShotClock"); } else {
					Draw.TextFixed(this, "OrcaShotClock", $"⏱ Shot Clock {(int)rem/60}:{(int)rem%60:D2}", ShotClockPosition, rem <= 30? ShotClockWarningColor : ShotClockColor, new SimpleFont("Arial", 14){ Bold = true }, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Transparent, 0);
				}
			}
		}
		private string GetCTAcc() { string r = ""; ChartControl?.Dispatcher.InvokeAsync(() => { try { Window w = Window.GetWindow(ChartControl); if (w is Chart ch && ch.ChartTrader?.Account != null) r = ch.ChartTrader.Account.Name; } catch {} }).Wait(100); return r; }
		private void LoadAllHistory() { if (LoadSqliteHistory) LoadFromSqlite(); if (LoadTodayHistory) LoadFromAccount(); lock (tradeLock) { foreach (var s in accountStates.Values) foreach (var rt in s.RoundTrips) if (rt.IsComplete && !rt.MAEMFECalculated) CalcMAE(rt); } needsRedraw = true; }
		private void CalcMAE(RoundTrip rt) { try { int s = Bars.GetBar(rt.FirstEntryTime), e = Bars.GetBar(rt.LastExitTime); if (s<0) s=0; if (e>=Bars.Count) e=Bars.Count-1; if (s>e) return; double pv = Instrument.MasterInstrument.PointValue, mx=0, mn=0; for (int i=s; i<=e; i++) { double h=High.GetValueAt(i), l=Low.GetValueAt(i), p1=(h-rt.AvgEntryPrice)*pv*rt.EntryQtyTotal, p2=(l-rt.AvgEntryPrice)*pv*rt.EntryQtyTotal; if (!rt.IsLong) { p1 = -p1; p2 = -p2; } mx=Math.Max(mx, Math.Max(p1,p2)); mn=Math.Min(mn, Math.Min(p1,p2)); } rt.MaxFavorableExcursion=mx; rt.MaxAdverseExcursion=mn; rt.MAEMFECalculated=true; } catch{} }
		private void UpdateLiveMAE() { if (!ShowMAEMFE) return; lock (tradeLock) { if (!accountStates.ContainsKey(activeAccountName)) return; var s = accountStates[activeAccountName]; if (s.CurrentRT != null && s.NetPosition != 0) { double pv = Instrument.MasterInstrument.PointValue, p1=(High[0]-s.CurrentRT.AvgEntryPrice)*pv*s.CurrentRT.EntryQtyTotal, p2=(Low[0]-s.CurrentRT.AvgEntryPrice)*pv*s.CurrentRT.EntryQtyTotal; if (!s.CurrentRT.IsLong) { p1=-p1; p2=-p2; } s.CurrentRT.MaxFavorableExcursion=Math.Max(s.CurrentRT.MaxFavorableExcursion, Math.Max(p1,p2)); s.CurrentRT.MaxAdverseExcursion=Math.Min(s.CurrentRT.MaxAdverseExcursion, Math.Min(p1,p2)); } } }
		private void LoadFromSqlite() { try { string db = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NinjaTrader 8", "db", "NinjaTrader.sqlite"), dll = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8", "bin", "System.Data.SQLite.dll"); if (!File.Exists(db) || !File.Exists(dll)) return; Type connT = Assembly.LoadFrom(dll).GetType("System.Data.SQLite.SQLiteConnection"); object conn = Activator.CreateInstance(connT, "Data Source="+db+";Read Only=True"); try { connT.GetMethod("Open").Invoke(conn, null); object cmd = connT.GetMethod("CreateCommand").Invoke(conn, null); Type cmdT = cmd.GetType(); cmdT.GetProperty("CommandText").SetValue(cmd, "SELECT e.Time, a.Name, e.MarketPosition, e.Price, e.Quantity FROM Executions e INNER JOIN Accounts a ON e.Account = a.Id INNER JOIN Instruments i ON e.Instrument = i.Id INNER JOIN MasterInstruments mi ON i.MasterInstrument = mi.Id WHERE mi.Name = @n ORDER BY e.Time ASC", null); object p = cmdT.GetMethod("CreateParameter").Invoke(cmd, null); p.GetType().GetProperty("ParameterName").SetValue(p, "@n", null); p.GetType().GetProperty("Value").SetValue(p, Instrument.MasterInstrument.Name, null); object ps = cmdT.GetProperty("Parameters").GetValue(cmd, null); ps.GetType().GetMethod("Add", new[] { p.GetType() }).Invoke(ps, new[] { p }); object r = cmdT.GetMethod("ExecuteReader", Type.EmptyTypes).Invoke(cmd, null); Type rT = r.GetType(); while ((bool)rT.GetMethod("Read").Invoke(r, null)) { ProcessExecution((int)rT.GetMethod("GetInt32").Invoke(r, new object[] { 2 }) == 1, (double)rT.GetMethod("GetDouble").Invoke(r, new object[] { 3 }), (int)rT.GetMethod("GetInt32").Invoke(r, new object[] { 4 }), new DateTime((long)rT.GetMethod("GetInt64").Invoke(r, new object[] { 0 })), (string)rT.GetMethod("GetString").Invoke(r, new object[] { 1 })); } rT.GetMethod("Close").Invoke(r, null); } finally { connT.GetMethod("Close").Invoke(conn, null); } } catch{} }
		private void LoadFromAccount() { try { string ni = Instrument.FullName; HashSet<string> seen = new HashSet<string>(); lock (tradeLock) { foreach (var s in accountStates.Values) foreach (var rt in s.RoundTrips) foreach (var m in rt.Matches) { seen.Add(m.EntryTime.Ticks+"_"+m.EntryPrice+"_"+m.Quantity); seen.Add(m.ExitTime.Ticks+"_"+m.ExitPrice+"_"+m.Quantity); } } foreach (var a in hookedAccounts) { foreach (var e in a.Executions.Where(x => x.Instrument != null && x.Instrument.FullName == ni).OrderBy(x => x.Time)) { if (!seen.Contains(e.Time.Ticks+"_"+e.Price+"_"+e.Quantity)) { ProcessExecution(e.Order.OrderAction == OrderAction.Buy || e.Order.OrderAction == OrderAction.BuyToCover, e.Price, e.Quantity, e.Time, a.Name); } } } } catch{} }

		private void DrawAll() { if (!ShowExecutionLines) return; List<RoundTrip> list; lock (tradeLock) { if (!accountStates.ContainsKey(activeAccountName)) return; list = accountStates[activeAccountName].RoundTrips.Where(rt => rt.IsComplete).ToList(); } if (lastDrawnAccount != activeAccountName) { foreach (var d in DrawObjects.ToList()) if (d.Tag.StartsWith("OrcaRT_")) RemoveDrawObject(d.Tag); lastDrawnAccount = activeAccountName; } if (list.Count > 0) { double p = list.Sum(rt => rt.TotalPnLDollars); if (ShowSessionTotal) Draw.TextFixed(this, "OrcaRT_Total", $"Session: {p:C2}{(RiskAmount>0?" | "+(p/RiskAmount).ToString("N1")+"R":"")} ({list.Count} trades)", SessionTotalPosition, p>=0? System.Windows.Media.Brushes.Lime: System.Windows.Media.Brushes.Salmon, new SimpleFont("Arial", 13){Bold=true}, System.Windows.Media.Brushes.Transparent, p>=0? System.Windows.Media.Brushes.DarkGreen: System.Windows.Media.Brushes.DarkRed, 80); else RemoveDrawObject("OrcaRT_Total"); } foreach (var rt in list) { string tag = "OrcaRT_"+rt.Number+"_"; System.Windows.Media.Brush b = rt.TotalPnLDollars >= 0? ProfitColor : LossColor; if (ShowIndividualLines) { for (int i=0; i<rt.Matches.Count; i++) { var m = rt.Matches[i]; Draw.Line(this, tag+"L"+i, false, m.EntryTime, m.EntryPrice, m.ExitTime, m.ExitPrice, m.PnLDollars>=0? ProfitColor : LossColor, DashStyleHelper.Solid, LineWidth); } } else Draw.Line(this, tag+"L", false, rt.FirstEntryTime, rt.AvgEntryPrice, rt.LastExitTime, rt.AvgExitPrice, b, DashStyleHelper.Solid, LineWidth); } }

		private void OnChartMouseMove(object s, MouseEventArgs e) { mousePosition = e.GetPosition(ChartControl); isMouseOverChart = true; ChartControl?.InvalidateVisual(); }
		private void OnChartMouseLeave(object s, MouseEventArgs e) { isMouseOverChart = false; mousePosition = new System.Windows.Point(-1,-1); ChartControl?.InvalidateVisual(); }

		protected override void OnRender(ChartControl cc, ChartScale cs) {
			if (!ShowExecutionLines || RenderTarget == null || ChartBars == null || Bars == null) return;
			List<RoundTrip> list; lock (tradeLock) { if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName)) return; list = accountStates[activeAccountName].RoundTrips.Where(rt => rt.IsComplete).ToList(); }
			if (list.Count == 0) return; SharpDX.Direct2D1.SolidColorBrush brL = CreateSolidBrush(LongMarkerColor), brLA = CreateSolidBrush(LongMarkerColor, 0.65f), brS = CreateSolidBrush(ShortMarkerColor), brSA = CreateSolidBrush(ShortMarkerColor, 0.65f);
			foreach (var rt in list) {
				var b = rt.IsLong? brL : brS; var ba = rt.IsLong? brLA : brSA; if (ShowMarkers && TryGetXY(rt.FirstEntryTime, rt.AvgEntryPrice, out float x1, out float y1) && TryGetXY(rt.LastExitTime, rt.AvgExitPrice, out float x2, out float y2)) { DrawTri(rt.IsLong, x1, y1, 8, b); DrawTri(!rt.IsLong, x2, y2, 8, b); }
				if (ShowIndividualMarkers && rt.Matches.Count > 1) { foreach (var m in rt.Matches) { if (TryGetXY(m.EntryTime, m.EntryPrice, out float x3, out float y3)) DrawTri(rt.IsLong, x3, y3, 4.4f, ba); if (TryGetXY(m.ExitTime, m.ExitPrice, out float x4, out float y4)) DrawTri(!rt.IsLong, x4, y4, 4.4f, ba); } }
			}
			if (ShowLabels && isMouseOverChart) {
				float mx = (float)mousePosition.X - (float)(ChartPanel?.X ?? 0), my = (float)mousePosition.Y - (float)(ChartPanel?.Y ?? 0); double best = 625; RoundTrip bestRT = null; FillMatch bestM = null;
				foreach (var rt in list) {
					if (HoverShowsIndividualPnL) { foreach (var m in rt.Matches) { if (TryGetXY(m.EntryTime, m.EntryPrice, out float xE, out float yE) && TryGetXY(m.ExitTime, m.ExitPrice, out float xX, out float yX)) { double d = DistToSeg(mx, my, xE, yE, xX, yX); if (d < best) { best=d; bestRT=rt; bestM=m; } } } }
					else if (TryGetXY(rt.FirstEntryTime, rt.AvgEntryPrice, out float xE, out float yE) && TryGetXY(rt.LastExitTime, rt.AvgExitPrice, out float xX, out float yX)) { double d = DistToSeg(mx, my, xE, yE, xX, yX); if (d < best) { best=d; bestRT=rt; bestM=null; } }
				}
				if (bestRT != null) { if (bestM != null && TryGetXY(bestM.ExitTime, bestM.ExitPrice, out float lx, out float ly)) DrawHover(bestRT, bestM, lx, ly); else if (TryGetXY(bestRT.LastExitTime, bestRT.AvgExitPrice, out float lx2, out float ly2)) DrawHover(bestRT, null, lx2, ly2); }
			}
		}
		private bool TryGetXY(DateTime t, double p, out float x, out float y) { x=0; y=0; int b = Bars.GetBar(t); if (b<0 || b>=Bars.Count) return false; x = ChartControl.GetXByBarIndex(ChartBars, b); y = ChartScreenLottery.GetYByPrice(this, p); return true; }
		private void DrawTri(bool up, float cx, float cy, float s, SharpDX.Direct2D1.SolidColorBrush b) {
			using (var g = new SharpDX.Direct2D1.PathGeometry(RenderTarget.Factory)) using (var sk = g.Open()) {
				sk.BeginFigure(new Vector2(cx, up? cy-s*1.4f : cy+s*1.4f), FigureBegin.Filled);
				sk.AddLines(new[] { new Vector2(cx+s, up? cy+s*0.7f : cy-s*0.7f), new Vector2(cx-s, up? cy+s*0.7f : cy-s*0.7f) });
				sk.EndFigure(FigureEnd.Closed); sk.Close(); RenderTarget.FillGeometry(g, b);
			}
		}
		private void DrawHover(RoundTrip rt, FillMatch m, float x, float y) {
			bool isM = m != null; int q = isM? m.Quantity : rt.EntryQtyTotal; double tks = isM? m.PnLTicks : (rt.EntryQtyTotal>0? rt.TotalPnLTicks/rt.EntryQtyTotal : 0), dls = isM? m.PnLDollars : rt.TotalPnLDollars;
			string txt = $"#{rt.Number}{(isM?" (Fill)":"")} {(rt.IsLong?"Long":"Short")}{(q>1?" x"+q:"")}\n{tks:+#.##;-#.##;0} ticks | {dls:C2}"; if (RiskAmount>0) txt += $" | {(dls/RiskAmount):+#.##;-#.##;0}R";
			if (!isM && ShowMAEMFE && rt.MAEMFECalculated) txt += $"\n{(dls>=0?"MDD: ":"Peak: ")}{(dls>=0?rt.MaxAdverseExcursion:rt.MaxFavorableExcursion):C2}";
			using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, LabelFontSize))
			using (var l = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, txt, fmt, 320, 120))
			using (var brBg = CreateSolidBrush(dls>=0? System.Windows.Media.Colors.DarkGreen : System.Windows.Media.Colors.Maroon, 0.88f))
			using (var brTx = CreateSolidBrush(dls>=0? System.Windows.Media.Colors.Lime : System.Windows.Media.Colors.LightPink)) {
				var mtr = l.Metrics; float rx = x+10, ry = y-mtr.Height/2; if(rx+mtr.Width+14 > RenderTarget.Size.Width) rx = x-mtr.Width-24; RenderTarget.FillRectangle(new RectangleF(rx-7, ry-7, mtr.Width+14, mtr.Height+14), brBg); RenderTarget.DrawTextLayout(new Vector2(rx, ry), l, brTx);
			}
		}
		private double DistToSeg(float px, float py, float x1, float y1, float x2, float y2) { float l2 = (x1-x2)*(x1-x2)+(y1-y2)*(y1-y2); if(l2==0) return (px-x1)*(px-x1)+(py-y1)*(py-y1); float t = Math.Max(0, Math.Min(1, ((px-x1)*(x2-x1)+(py-y1)*(y2-y1))/l2)); return (px-(x1+t*(x2-x1)))*(px-(x1+t*(x2-x1))) + (py-(y1+t*(y2-y1)))*(py-(y1+t*(y2-y1))); }
		private SharpDX.Direct2D1.SolidColorBrush CreateSolidBrush(System.Windows.Media.Brush wpfBrush, float alpha = 1f) => new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(wpfBrush, alpha));
		private SharpDX.Direct2D1.SolidColorBrush CreateSolidBrush(System.Windows.Media.Color color, float alpha = 1f) => new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(color.R/255f, color.G/255f, color.B/255f, alpha));
		private SharpDX.Color4 ToDxColor(System.Windows.Media.Brush b, float a = 1f) { var s = b as System.Windows.Media.SolidColorBrush; return s != null ? new Color4(s.Color.R/255f, s.Color.G/255f, s.Color.B/255f, a) : SharpDX.Color.Gray; }
	}
	public static class ChartScreenLottery { public static float GetYByPrice(Indicator i, double p) { var s = i.ChartPanel.Scales.FirstOrDefault(x => x.ScaleJustification == i.ScaleJustification); return s != null ? s.GetYByValue(p) : 0; } }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaExecutionLines[] cacheOrcaExecutionLines;
		public OrcaExecutionLines OrcaExecutionLines(bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
		{
			return OrcaExecutionLines(Input, showExecutionLines, showLabels, showMarkers, showIndividualLines, showIndividualMarkers, showMAEMFE, enableShotClock, shotClockSeconds, loadTodayHistory, loadSqliteHistory, riskAmount, lineWidth, labelFontSize);
		}

		public OrcaExecutionLines OrcaExecutionLines(ISeries<double> input, bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
		{
			if (cacheOrcaExecutionLines != null)
				for (int idx = 0; idx < cacheOrcaExecutionLines.Length; idx++)
					if (cacheOrcaExecutionLines[idx] != null && cacheOrcaExecutionLines[idx].ShowExecutionLines == showExecutionLines && cacheOrcaExecutionLines[idx].ShowLabels == showLabels && cacheOrcaExecutionLines[idx].ShowMarkers == showMarkers && cacheOrcaExecutionLines[idx].ShowIndividualLines == showIndividualLines && cacheOrcaExecutionLines[idx].ShowIndividualMarkers == showIndividualMarkers && cacheOrcaExecutionLines[idx].ShowMAEMFE == showMAEMFE && cacheOrcaExecutionLines[idx].EnableShotClock == enableShotClock && cacheOrcaExecutionLines[idx].ShotClockSeconds == shotClockSeconds && cacheOrcaExecutionLines[idx].LoadTodayHistory == loadTodayHistory && cacheOrcaExecutionLines[idx].LoadSqliteHistory == loadSqliteHistory && cacheOrcaExecutionLines[idx].RiskAmount == riskAmount && cacheOrcaExecutionLines[idx].LineWidth == lineWidth && cacheOrcaExecutionLines[idx].LabelFontSize == labelFontSize && cacheOrcaExecutionLines[idx].EqualsInput(input))
						return cacheOrcaExecutionLines[idx];
			return CacheIndicator<OrcaExecutionLines>(new OrcaExecutionLines(){ ShowExecutionLines = showExecutionLines, ShowLabels = showLabels, ShowMarkers = showMarkers, ShowIndividualLines = showIndividualLines, ShowIndividualMarkers = showIndividualMarkers, ShowMAEMFE = showMAEMFE, EnableShotClock = enableShotClock, ShotClockSeconds = shotClockSeconds, LoadTodayHistory = loadTodayHistory, LoadSqliteHistory = loadSqliteHistory, RiskAmount = riskAmount, LineWidth = lineWidth, LabelFontSize = labelFontSize }, input, ref cacheOrcaExecutionLines);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaExecutionLines OrcaExecutionLines(bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
		{
			return indicator.OrcaExecutionLines(Input, showExecutionLines, showLabels, showMarkers, showIndividualLines, showIndividualMarkers, showMAEMFE, enableShotClock, shotClockSeconds, loadTodayHistory, loadSqliteHistory, riskAmount, lineWidth, labelFontSize);
		}

		public Indicators.OrcaExecutionLines OrcaExecutionLines(ISeries<double> input , bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
		{
			return indicator.OrcaExecutionLines(input, showExecutionLines, showLabels, showMarkers, showIndividualLines, showIndividualMarkers, showMAEMFE, enableShotClock, shotClockSeconds, loadTodayHistory, loadSqliteHistory, riskAmount, lineWidth, labelFontSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaExecutionLines OrcaExecutionLines(bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
		{
			return indicator.OrcaExecutionLines(Input, showExecutionLines, showLabels, showMarkers, showIndividualLines, showIndividualMarkers, showMAEMFE, enableShotClock, shotClockSeconds, loadTodayHistory, loadSqliteHistory, riskAmount, lineWidth, labelFontSize);
		}

		public Indicators.OrcaExecutionLines OrcaExecutionLines(ISeries<double> input , bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
		{
			return indicator.OrcaExecutionLines(input, showExecutionLines, showLabels, showMarkers, showIndividualLines, showIndividualMarkers, showMAEMFE, enableShotClock, shotClockSeconds, loadTodayHistory, loadSqliteHistory, riskAmount, lineWidth, labelFontSize);
		}
	}
}

#endregion
