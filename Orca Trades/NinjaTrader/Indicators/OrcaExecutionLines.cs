using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
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

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaExecutionLines : Indicator
	{
		private class PendingEntry
		{
			public DateTime Time;
			public double Price;
			public int Quantity;
			public MarketPosition Side;
		}

		private class FillMatch
		{
			public DateTime EntryTime;
			public double EntryPrice;
			public DateTime ExitTime;
			public double ExitPrice;
			public int Quantity;
			public bool IsLong;
			public double PnLTicks;
			public double PnLDollars;
		}

		private class RoundTrip
		{
			public int Number;
			public bool IsLong;
			public bool IsComplete;
			public string AccountName;
			public List<FillMatch> Matches = new List<FillMatch>();
			public double EntryPriceSum;
			public int EntryQtyTotal;
			public DateTime FirstEntryTime;
			public double ExitPriceSum;
			public int ExitQtyTotal;
			public DateTime LastExitTime;
			public double TotalPnLDollars;
			public double TotalPnLTicks;
			public double MaxAdverseExcursion;
			public double MaxFavorableExcursion;
			public bool MAEMFECalculated;

			public double AvgEntryPrice { get { return EntryQtyTotal <= 0 ? 0.0 : EntryPriceSum / (double)EntryQtyTotal; } }
			public double AvgExitPrice  { get { return ExitQtyTotal  <= 0 ? 0.0 : ExitPriceSum  / (double)ExitQtyTotal;  } }
		}

		private class AccountState
		{
			public string AccountName;
			public List<PendingEntry> OpenFills  = new List<PendingEntry>();
			public List<RoundTrip>   RoundTrips  = new List<RoundTrip>();
			public RoundTrip CurrentRT;
			public int RTCounter;
			public int NetPosition;
		}

		// â”€â”€ fields â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		private Dictionary<string, AccountState> accountStates;
		private List<Account> hookedAccounts;
		private string activeAccountName;
		private string lastDrawnAccount;
		private readonly object tradeLock = new object();
		private bool needsRedraw;
		private bool historyLoaded;
		private DateTime shotClockEnd = DateTime.MinValue;
		private bool shotClockActive;
		private bool shotClockIsLive;
		private System.Windows.Point mousePosition = new System.Windows.Point(-1, -1);
		private bool isMouseOverChart;
		private DateTime lastAccountCheck = DateTime.MinValue;
		private bool mouseHooked;

		// â”€â”€ properties â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		[NinjaScriptProperty][Display(Name="Show Execution Lines",   GroupName="1. Visibility", Order=0)] public bool ShowExecutionLines  { get; set; }
		[NinjaScriptProperty][Display(Name="Show Labels",            GroupName="1. Visibility", Order=1)] public bool ShowLabels           { get; set; }
		[Display(Name="Hover Individual PnL",                        GroupName="1. Visibility", Order=2)] public bool HoverShowsIndividualPnL { get; set; }
		[NinjaScriptProperty][Display(Name="Show Average Markers",   GroupName="1. Visibility", Order=3)] public bool ShowMarkers          { get; set; }
		[NinjaScriptProperty][Display(Name="Individual Lines",       GroupName="1. Visibility", Order=4)] public bool ShowIndividualLines  { get; set; }
		[NinjaScriptProperty][Display(Name="Individual Fill Markers",GroupName="1. Visibility", Order=5)] public bool ShowIndividualMarkers { get; set; }
		[NinjaScriptProperty][Display(Name="Show MAE/MFE",          GroupName="1. Visibility", Order=6)] public bool ShowMAEMFE           { get; set; }
		[Display(Name="Show Session Total",                          GroupName="1. Visibility", Order=7)] public bool ShowSessionTotal     { get; set; }
		[Display(Name="Session Total Position",                      GroupName="3. Appearance", Order=2)] public TextPosition SessionTotalPosition { get; set; }

		[NinjaScriptProperty][Display(Name="Enable Shot Clock",      GroupName="5. Shot Clock", Order=0)] public bool EnableShotClock     { get; set; }
		[NinjaScriptProperty][Range(5,3600)][Display(Name="Cooldown (seconds)", GroupName="5. Shot Clock", Order=1)] public int ShotClockSeconds { get; set; }
		[Display(Name="Label Position",                              GroupName="5. Shot Clock", Order=2)] public TextPosition ShotClockPosition { get; set; }

		[XmlIgnore][Display(Name="Countdown Color",  GroupName="5. Shot Clock", Order=3)] public System.Windows.Media.Brush ShotClockColor        { get; set; }
		[Browsable(false)] public string ShotClockColorSerializable        { get { return Serialize.BrushToString(ShotClockColor);        } set { ShotClockColor        = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Warning Color (â‰¤30s)", GroupName="5. Shot Clock", Order=4)] public System.Windows.Media.Brush ShotClockWarningColor { get; set; }
		[Browsable(false)] public string ShotClockWarningColorSerializable { get { return Serialize.BrushToString(ShotClockWarningColor); } set { ShotClockWarningColor = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty][Display(Name="Load from Account",      GroupName="2. Data", Order=0)] public bool LoadTodayHistory   { get; set; }
		[NinjaScriptProperty][Display(Name="Load from SQLite",       GroupName="2. Data", Order=1)] public bool LoadSqliteHistory  { get; set; }
		[NinjaScriptProperty][Range(0,100000)][Display(Name="Risk Amount ($)", GroupName="2. Data", Order=2)] public double RiskAmount { get; set; }
		[Range(5,200)][Display(Name="Max Trades To Show",            GroupName="2. Data", Order=3)] public int MaxTradesToShow     { get; set; }

		[NinjaScriptProperty][Range(1,5)][Display(Name="Line Width", GroupName="3. Appearance", Order=0)] public int LineWidth      { get; set; }
		[NinjaScriptProperty][Range(8,20)][Display(Name="Label Font Size", GroupName="3. Appearance", Order=1)] public int LabelFontSize { get; set; }

		[XmlIgnore][Display(Name="Profit Color", GroupName="4. Colors", Order=0)] public System.Windows.Media.Brush ProfitColor      { get; set; }
		[Browsable(false)] public string ProfitColorSerializable      { get { return Serialize.BrushToString(ProfitColor);      } set { ProfitColor      = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Loss Color",   GroupName="4. Colors", Order=1)] public System.Windows.Media.Brush LossColor        { get; set; }
		[Browsable(false)] public string LossColorSerializable        { get { return Serialize.BrushToString(LossColor);        } set { LossColor        = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Long Marker Color",  GroupName="4. Colors", Order=2)] public System.Windows.Media.Brush LongMarkerColor  { get; set; }
		[Browsable(false)] public string LongMarkerColorSerializable  { get { return Serialize.BrushToString(LongMarkerColor);  } set { LongMarkerColor  = Serialize.StringToBrush(value); } }
		[XmlIgnore][Display(Name="Short Marker Color", GroupName="4. Colors", Order=3)] public System.Windows.Media.Brush ShortMarkerColor { get; set; }
		[Browsable(false)] public string ShortMarkerColorSerializable { get { return Serialize.BrushToString(ShortMarkerColor); } set { ShortMarkerColor = Serialize.StringToBrush(value); } }

		// â”€â”€ lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Automatic execution lines with FIFO round-trip matching, SQLite history, and R-multiple tracking";
				Name        = "OrcaExecutionLines";
				Calculate   = Calculate.OnEachTick;
				IsOverlay   = true;
				DisplayInDataBox      = false;
				DrawOnPricePanel      = true;
				ScaleJustification    = ScaleJustification.Right;
				IsSuspendedWhileInactive = false;

				ShowExecutionLines   = true;
				ShowLabels           = true;
				HoverShowsIndividualPnL = false;
				ShowMarkers          = true;
				ShowIndividualLines  = true;
				ShowIndividualMarkers= true;
				ShowMAEMFE           = true;
				ShowSessionTotal     = true;
				SessionTotalPosition = TextPosition.TopRight;
				LoadTodayHistory     = true;
				LoadSqliteHistory    = true;
				EnableShotClock      = true;
				ShotClockSeconds     = 300;
				ShotClockPosition    = TextPosition.BottomRight;
				ShotClockColor        = System.Windows.Media.Brushes.Orange;
				ShotClockWarningColor = System.Windows.Media.Brushes.Red;
				LineWidth            = 2;
				LabelFontSize        = 11;
				MaxTradesToShow      = 50;
				RiskAmount           = 200.0;
				ProfitColor          = System.Windows.Media.Brushes.DodgerBlue;
				LossColor            = System.Windows.Media.Brushes.Tomato;
				LongMarkerColor      = System.Windows.Media.Brushes.Lime;
				ShortMarkerColor     = System.Windows.Media.Brushes.Red;
			}
			else if (State == State.DataLoaded)
			{
				accountStates    = new Dictionary<string, AccountState>();
				hookedAccounts   = new List<Account>();
				needsRedraw      = false;
				historyLoaded    = false;
				activeAccountName= "";
				lastDrawnAccount = "";
				shotClockActive  = false;
				shotClockIsLive  = false;
				mouseHooked      = false;
				HookAllAccounts();
			}
			else if (State == State.Realtime)
			{
				if (ChartControl != null && !mouseHooked)
				{
					ChartControl.MouseMove  += OnChartMouseMove;
					ChartControl.MouseLeave += OnChartMouseLeave;
					mouseHooked = true;
				}
			}
			else if (State == State.Terminated)
			{
				UnhookAllAccounts();
				if (ChartControl != null && mouseHooked)
				{
					try { ChartControl.MouseMove  -= OnChartMouseMove; } catch {}
					try { ChartControl.MouseLeave -= OnChartMouseLeave; } catch {}
				}
				try { RemoveDrawObject("OrcaShotClock"); } catch {}
			}
		}

		private void HookAllAccounts()
		{
			try
			{
				foreach (Account a in Account.All)
				{
					a.ExecutionUpdate += OnExecutionUpdate;
					hookedAccounts.Add(a);
				}
			}
			catch (Exception ex) { Print("OrcaExecLines HookAll: " + ex.Message); }
		}

		private void UnhookAllAccounts()
		{
			try
			{
				foreach (Account a in hookedAccounts)
					try { a.ExecutionUpdate -= OnExecutionUpdate; } catch {}
				hookedAccounts.Clear();
			}
			catch {}
		}

		private AccountState GetOrCreateState(string name)
		{
			if (!accountStates.ContainsKey(name))
				accountStates[name] = new AccountState { AccountName = name };
			return accountStates[name];
		}

		private string GetChartTraderAccount()
		{
			try
			{
				if (ChartControl == null) return "";
				string result = "";
				ChartControl.Dispatcher.InvokeAsync(() =>
				{
					try
					{
						Window w = Window.GetWindow(ChartControl);
						Chart ch = w as Chart;
						if (ch != null && ch.ChartTrader != null && ch.ChartTrader.Account != null)
							result = ch.ChartTrader.Account.Name;
					}
					catch {}
				}).Wait(TimeSpan.FromMilliseconds(100));
				return result;
			}
			catch { return ""; }
		}

		private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			try
			{
				if (e.Execution == null || e.Execution.Instrument == null || Instrument == null) return;
				if (e.Execution.Instrument.FullName != Instrument.FullName) return;
				if (e.Execution.Order == null) return;
				string acct = e.Execution.Account != null ? e.Execution.Account.Name : "Unknown";
				bool isBuy  = e.Execution.Order.OrderAction == OrderAction.Buy || e.Execution.Order.OrderAction == OrderAction.BuyToCover;
				ProcessExecution(isBuy, e.Execution.Price, e.Execution.Quantity, e.Execution.Time, acct);
			}
			catch (Exception ex) { Print("OrcaExecLines OnExec: " + ex.Message); }
		}

		private void ProcessExecution(bool isBuy, double price, int quantity, DateTime time, string accountName)
		{
			lock (tradeLock)
			{
				AccountState st  = GetOrCreateState(accountName);
				int prev = st.NetPosition;
				int next = st.NetPosition + (isBuy ? quantity : -quantity);

				bool isReducing = (st.NetPosition > 0 && !isBuy) || (st.NetPosition < 0 && isBuy);
				if (isReducing)
				{
					int toClose = Math.Min(quantity, Math.Abs(st.NetPosition));
					int toOpen  = quantity - toClose;
					int rem     = toClose;

					while (rem > 0 && st.OpenFills.Count > 0)
					{
						PendingEntry pe  = st.OpenFills[0];
						int filled       = Math.Min(rem, pe.Quantity);
						bool wasLong     = (pe.Side == MarketPosition.Long);
						double ticks     = wasLong ? (price - pe.Price)/TickSize : (pe.Price - price)/TickSize;
						double dollars   = ticks * TickSize * Instrument.MasterInstrument.PointValue * filled;

						if (st.CurrentRT != null)
						{
							st.CurrentRT.Matches.Add(new FillMatch {
								EntryTime = pe.Time, EntryPrice = pe.Price,
								ExitTime  = time,    ExitPrice  = price,
								Quantity  = filled,  IsLong     = wasLong,
								PnLTicks  = ticks,   PnLDollars = dollars
							});
							st.CurrentRT.ExitPriceSum  += price  * filled;
							st.CurrentRT.ExitQtyTotal  += filled;
							st.CurrentRT.LastExitTime   = time;
							st.CurrentRT.TotalPnLDollars+= dollars;
							st.CurrentRT.TotalPnLTicks  += ticks * filled;
						}
						pe.Quantity -= filled;
						rem         -= filled;
						if (pe.Quantity <= 0) st.OpenFills.RemoveAt(0);
					}

					if (next == 0 && st.CurrentRT != null)
					{
						st.CurrentRT.IsComplete = true;
						while (st.RoundTrips.Count > MaxTradesToShow) st.RoundTrips.RemoveAt(0);
						st.CurrentRT = null;
						if (EnableShotClock && shotClockIsLive) { shotClockEnd = DateTime.UtcNow.AddSeconds(ShotClockSeconds); shotClockActive = true; }
					}
					if (toOpen > 0)
					{
						StartNewRoundTrip(st, isBuy, price, toOpen, time, accountName);
						st.OpenFills.Add(new PendingEntry { Time=time, Price=price, Quantity=toOpen, Side=isBuy?MarketPosition.Long:MarketPosition.Short });
					}
					needsRedraw = true;
				}
				else
				{
					if (prev == 0) StartNewRoundTrip(st, isBuy, price, quantity, time, accountName);
					else if (st.CurrentRT != null) { st.CurrentRT.EntryPriceSum += price*(double)quantity; st.CurrentRT.EntryQtyTotal += quantity; }
					st.OpenFills.Add(new PendingEntry { Time=time, Price=price, Quantity=quantity, Side=isBuy?MarketPosition.Long:MarketPosition.Short });
				}
				st.NetPosition = next;
			}
		}

		private void StartNewRoundTrip(AccountState st, bool isBuy, double price, int qty, DateTime time, string acct)
		{
			st.RTCounter++;
			st.CurrentRT = new RoundTrip {
				Number=st.RTCounter, IsLong=isBuy, IsComplete=false, AccountName=acct,
				EntryPriceSum=price*(double)qty, EntryQtyTotal=qty, FirstEntryTime=time, LastExitTime=DateTime.MinValue
			};
			st.RoundTrips.Add(st.CurrentRT);
		}

		// â”€â”€ history loading â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		private void LoadAllHistory()
		{
			if (LoadSqliteHistory) LoadFromSqlite();
			if (LoadTodayHistory)  LoadFromAccountExecutions();
			CalculateAllMAEMFE();
			int total = 0;
			lock (tradeLock)
				foreach (var kv in accountStates)
					total += kv.Value.RoundTrips.Count(r => r.IsComplete);
			Print("OrcaExecLines: " + total + " completed round-trips loaded");
			if (total > 0) needsRedraw = true;
		}

		private void LoadFromSqlite()
		{
			try
			{
				string db  = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NinjaTrader 8", "db", "NinjaTrader.sqlite");
				string dll = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8", "bin", "System.Data.SQLite.dll");
				if (!System.IO.File.Exists(db) || !System.IO.File.Exists(dll)) { Print("OrcaExecLines: SQLite db/dll not found"); return; }

				Type connT = Assembly.LoadFrom(dll).GetType("System.Data.SQLite.SQLiteConnection");
				object conn = Activator.CreateInstance(connT, "Data Source=" + db + ";Read Only=True");
				try
				{
					connT.GetMethod("Open").Invoke(conn, null);
					string instName = Instrument.MasterInstrument.Name;
					object cmd   = connT.GetMethod("CreateCommand").Invoke(conn, null);
					Type   cmdT  = cmd.GetType();
					cmdT.GetProperty("CommandText").SetValue(cmd,
						"SELECT e.Time, a.Name, e.MarketPosition, e.Price, e.Quantity " +
						"FROM Executions e " +
						"INNER JOIN Accounts a ON e.Account = a.Id " +
						"INNER JOIN Instruments i ON e.Instrument = i.Id " +
						"INNER JOIN MasterInstruments mi ON i.MasterInstrument = mi.Id " +
						"WHERE mi.Name = @n ORDER BY e.Time ASC", null);
					object p  = cmdT.GetMethod("CreateParameter").Invoke(cmd, null);
					Type   pT = p.GetType();
					pT.GetProperty("ParameterName").SetValue(p, "@n", null);
					pT.GetProperty("Value").SetValue(p, instName, null);
					object ps = cmdT.GetProperty("Parameters").GetValue(cmd, null);
					ps.GetType().GetMethod("Add", new[] { pT }).Invoke(ps, new[] { p });

					object r  = cmdT.GetMethod("ExecuteReader", Type.EmptyTypes).Invoke(cmd, null);
					Type   rT = r.GetType();
					var mRead   = rT.GetMethod("Read");
					var mI64    = rT.GetMethod("GetInt64");
					var mStr    = rT.GetMethod("GetString");
					var mI32    = rT.GetMethod("GetInt32");
					var mDbl    = rT.GetMethod("GetDouble");
					int count = 0;
					while ((bool)mRead.Invoke(r, null))
					{
						long   ticks = (long)mI64.Invoke(r, new object[]{0});
						string acct  = (string)mStr.Invoke(r, new object[]{1});
						int    mp    = (int)mI32.Invoke(r, new object[]{2});
						double price = (double)mDbl.Invoke(r, new object[]{3});
						int    qty   = (int)mI32.Invoke(r, new object[]{4});
						ProcessExecution(mp == (int)MarketPosition.Long, price, qty, new DateTime(ticks), acct);
						count++;
					}
					rT.GetMethod("Close").Invoke(r, null);
					Print("OrcaExecLines: " + count + " SQLite rows for " + instName);
				}
				finally { try { connT.GetMethod("Close").Invoke(conn, null); } catch {} }
			}
			catch (Exception ex) { Print("OrcaExecLines SQLite: " + ex.Message); }
		}

		private void LoadFromAccountExecutions()
		{
			try
			{
				if (Instrument == null) return;
				string chartInst = Instrument.FullName;
				HashSet<string> seen = new HashSet<string>();
				lock (tradeLock)
					foreach (var kv in accountStates)
						foreach (var rt in kv.Value.RoundTrips)
							foreach (var m in rt.Matches)
							{
								seen.Add(m.EntryTime.Ticks+"_"+m.EntryPrice+"_"+m.Quantity);
								seen.Add(m.ExitTime.Ticks +"_"+m.ExitPrice +"_"+m.Quantity);
							}

				foreach (Account a in hookedAccounts)
				{
					try
					{
						var execs = a.Executions.Where(e => e.Instrument != null && e.Instrument.FullName == chartInst && e.Order != null).OrderBy(e => e.Time).ToList();
						int count = 0;
						foreach (var e in execs)
						{
							string key = e.Time.Ticks+"_"+e.Price+"_"+e.Quantity;
							if (!seen.Contains(key))
							{
								bool isBuy = e.Order.OrderAction == OrderAction.Buy || e.Order.OrderAction == OrderAction.BuyToCover;
								ProcessExecution(isBuy, e.Price, e.Quantity, e.Time, a.Name);
								count++;
							}
						}
						if (count > 0) Print("OrcaExecLines: " + count + " executions from account " + a.Name);
					}
					catch {}
				}
			}
			catch (Exception ex) { Print("OrcaExecLines AcctExec: " + ex.Message); }
		}

		private void CalculateAllMAEMFE()
		{
			if (!ShowMAEMFE) return;
			lock (tradeLock)
				foreach (var kv in accountStates)
					foreach (var rt in kv.Value.RoundTrips.Where(r => r.IsComplete && !r.MAEMFECalculated))
						CalculateMAEMFE(rt);
		}

		private void CalculateMAEMFE(RoundTrip rt)
		{
			try
			{
				if (Bars == null || Bars.Count < 2) return;
				int s = Bars.GetBar(rt.FirstEntryTime), e = Bars.GetBar(rt.LastExitTime);
				if (s < 0) s = 0; if (e >= Bars.Count) e = Bars.Count - 1; if (s > e) return;
				double ep = rt.AvgEntryPrice, pv = Instrument.MasterInstrument.PointValue;
				double mfe = 0, mae = 0;
				for (int i = s; i <= e; i++)
				{
					double h = High.GetValueAt(i), l = Low.GetValueAt(i);
					double vh = rt.IsLong ? (h-ep)*pv*rt.EntryQtyTotal : (ep-h)*pv*rt.EntryQtyTotal;
					double vl = rt.IsLong ? (l-ep)*pv*rt.EntryQtyTotal : (ep-l)*pv*rt.EntryQtyTotal;
					mfe = Math.Max(mfe, Math.Max(vh, vl));
					mae = Math.Min(mae, Math.Min(vh, vl));
				}
				rt.MaxFavorableExcursion = mfe;
				rt.MaxAdverseExcursion   = mae;
				rt.MAEMFECalculated      = true;
			}
			catch {}
		}

		private void UpdateLiveMAEMFE()
		{
			if (!ShowMAEMFE) return;
			lock (tradeLock)
			{
				if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName)) return;
				AccountState st = accountStates[activeAccountName];
				if (st.CurrentRT == null || st.NetPosition == 0) return;
				RoundTrip rt = st.CurrentRT;
				double ep = rt.AvgEntryPrice, pv = Instrument.MasterInstrument.PointValue;
				double vh = rt.IsLong ? (High[0]-ep)*pv*rt.EntryQtyTotal : (ep-High[0])*pv*rt.EntryQtyTotal;
				double vl = rt.IsLong ? (Low[0] -ep)*pv*rt.EntryQtyTotal : (ep-Low[0] )*pv*rt.EntryQtyTotal;
				rt.MaxFavorableExcursion = Math.Max(rt.MaxFavorableExcursion, Math.Max(vh, vl));
				rt.MaxAdverseExcursion   = Math.Min(rt.MaxAdverseExcursion,   Math.Min(vh, vl));
			}
		}

		// â”€â”€ bar update â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		protected override void OnBarUpdate()
		{
			if (!historyLoaded && CurrentBar > 10)
			{
				historyLoaded   = true;
				shotClockIsLive = true;
				LoadAllHistory();
			}

			if (DateTime.UtcNow - lastAccountCheck > TimeSpan.FromSeconds(1))
			{
				lastAccountCheck = DateTime.UtcNow;
				string acct = GetChartTraderAccount();
				if (!string.IsNullOrEmpty(acct) && acct != activeAccountName)
				{
					activeAccountName = acct;
					needsRedraw = true;
				}
			}

			UpdateLiveMAEMFE();

			if (needsRedraw) { needsRedraw = false; DrawAllTrades(); }

			if (!EnableShotClock || !shotClockActive) return;
			double rem = (shotClockEnd - DateTime.UtcNow).TotalSeconds;
			if (rem <= 0)
			{
				shotClockActive = false;
				try { RemoveDrawObject("OrcaShotClock"); } catch {}
				return;
			}
			System.Windows.Media.Brush clk = rem <= 30 ? ShotClockWarningColor : ShotClockColor;
			Draw.TextFixed(this, "OrcaShotClock",
				string.Format("â± Shot Clock  {0}:{1:D2}", (int)(rem/60), (int)(rem%60)),
				ShotClockPosition, clk,
				new SimpleFont("Arial", 14){ Bold=true },
				System.Windows.Media.Brushes.Transparent,
				System.Windows.Media.Brushes.Transparent, 0);
		}

		// â”€â”€ drawing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		private void ClearOldDrawings()
		{
			try
			{
				foreach (string tag in DrawObjects.Where(d => d.Tag.StartsWith("OrcaRT_")).Select(d => d.Tag).ToList())
					RemoveDrawObject(tag);
			}
			catch {}
		}

		private void DrawAllTrades()
		{
			if (!ShowExecutionLines) return;
			List<RoundTrip> list;
			lock (tradeLock)
			{
				if (!accountStates.ContainsKey(activeAccountName)) return;
				list = accountStates[activeAccountName].RoundTrips.Where(rt => rt.IsComplete).ToList();
			}
			if (lastDrawnAccount != activeAccountName) { ClearOldDrawings(); lastDrawnAccount = activeAccountName; }

			if (list.Count > 0)
			{
				double total = list.Sum(rt => rt.TotalPnLDollars);
				string rStr  = RiskAmount > 0 ? " | " + (total/RiskAmount).ToString("+0.##;-0.##;0") + "R" : "";
				if (ShowSessionTotal)
					Draw.TextFixed(this, "OrcaRT_SessionTotal",
						"Session: " + Fmt(total) + rStr + " (" + list.Count + " trades)",
						SessionTotalPosition,
						total >= 0 ? System.Windows.Media.Brushes.Lime : System.Windows.Media.Brushes.Salmon,
						new SimpleFont("Arial",13){Bold=true},
						System.Windows.Media.Brushes.Transparent,
						total >= 0 ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkRed, 80);
				else
					RemoveDrawObject("OrcaRT_SessionTotal");
			}
			foreach (var rt in list) DrawRoundTrip(rt);
		}

		private void DrawRoundTrip(RoundTrip rt)
		{
			// Line rendering is offloaded entirely to SharpDX in OnRender to eliminate chart lag
		}

		private string Fmt(double d) { return (d >= 0 ? "+$" : "-$") + Math.Abs(d).ToString("N2"); }

		// â”€â”€ mouse â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		private void OnChartMouseMove(object sender, MouseEventArgs e)
		{
			mousePosition   = e.GetPosition(ChartControl);
			isMouseOverChart = true;
			if (ChartControl != null) ChartControl.InvalidateVisual();
		}
		private void OnChartMouseLeave(object sender, MouseEventArgs e)
		{
			isMouseOverChart = false;
			mousePosition    = new System.Windows.Point(-1, -1);
			if (ChartControl != null) ChartControl.InvalidateVisual();
		}

		// â”€â”€ render â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (!ShowExecutionLines || RenderTarget == null || ChartBars == null || Bars == null) return;
			List<RoundTrip> list;
			lock (tradeLock)
			{
				if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName)) return;
				list = accountStates[activeAccountName].RoundTrips.Where(rt => rt.IsComplete).ToList();
			}
			if (list.Count == 0) return;

			RoundTrip hoveredRT     = null;
			FillMatch hoveredFill   = null;
			if (ShowLabels && isMouseOverChart)
			{
				float mx = (float)mousePosition.X - (ChartPanel != null ? (float)ChartPanel.X : 0);
				float my = (float)mousePosition.Y - (ChartPanel != null ? (float)ChartPanel.Y : 0);
				double best = 625.0;
				foreach (var rt in list)
				{
					if (HoverShowsIndividualPnL)
					{
						foreach (var m in rt.Matches)
						{
							float x1,y1,x2,y2;
							if (TryGetXY(m.EntryTime, m.EntryPrice, chartControl, chartScale, out x1, out y1) &&
							    TryGetXY(m.ExitTime,  m.ExitPrice,  chartControl, chartScale, out x2, out y2))
							{
								double d = DistSq(mx,my,x1,y1,x2,y2);
								if (d < best) { best=d; hoveredRT=rt; hoveredFill=m; }
							}
						}
					}
					else
					{
						float x1,y1,x2,y2;
						if (TryGetXY(rt.FirstEntryTime, rt.AvgEntryPrice, chartControl, chartScale, out x1, out y1) &&
						    TryGetXY(rt.LastExitTime,  rt.AvgExitPrice,  chartControl, chartScale, out x2, out y2))
						{
							double d = DistSq(mx,my,x1,y1,x2,y2);
							if (d < best) { best=d; hoveredRT=rt; hoveredFill=null; }
						}
					}
				}
			}

			SharpDX.Direct2D1.SolidColorBrush lngB  = ToD2D(LongMarkerColor);
			SharpDX.Direct2D1.SolidColorBrush lngBA = ToD2D(LongMarkerColor, 0.65f);
			SharpDX.Direct2D1.SolidColorBrush shtB  = ToD2D(ShortMarkerColor);
			SharpDX.Direct2D1.SolidColorBrush shtBA = ToD2D(ShortMarkerColor, 0.65f);
			SharpDX.Direct2D1.SolidColorBrush prfB  = ToD2D(ProfitColor);
			SharpDX.Direct2D1.SolidColorBrush lssB  = ToD2D(LossColor);

			try
			{
				foreach (var rt in list)
				{
					SharpDX.Direct2D1.SolidColorBrush mb  = rt.IsLong ? lngB  : shtB;
					SharpDX.Direct2D1.SolidColorBrush mba = rt.IsLong ? lngBA : shtBA;

					if (ShowIndividualLines)
					{
						foreach (var m in rt.Matches)
						{
							float xa,ya,xb,yb;
							if (TryGetXY(m.EntryTime, m.EntryPrice, chartControl, chartScale, out xa, out ya) && TryGetXY(m.ExitTime, m.ExitPrice, chartControl, chartScale, out xb, out yb))
								RenderTarget.DrawLine(new Vector2(xa, ya), new Vector2(xb, yb), m.PnLDollars >= 0 ? prfB : lssB, (float)LineWidth);
						}
					}
					else
					{
						float xa,ya,xb,yb;
						if (TryGetXY(rt.FirstEntryTime, rt.AvgEntryPrice, chartControl, chartScale, out xa, out ya) && TryGetXY(rt.LastExitTime, rt.AvgExitPrice, chartControl, chartScale, out xb, out yb))
							RenderTarget.DrawLine(new Vector2(xa, ya), new Vector2(xb, yb), rt.TotalPnLDollars >= 0 ? prfB : lssB, (float)LineWidth);
					}

					float x1,y1,x2,y2;
					if (ShowMarkers &&
					    TryGetXY(rt.FirstEntryTime, rt.AvgEntryPrice, chartControl, chartScale, out x1, out y1) &&
					    TryGetXY(rt.LastExitTime,  rt.AvgExitPrice,  chartControl, chartScale, out x2, out y2))
					{
						DrawTri( rt.IsLong, x1, y1, 8f, mb);
						DrawTri(!rt.IsLong, x2, y2, 8f, mb);
					}
					if (!ShowIndividualMarkers || (rt.Matches.Count <= 1 && ShowMarkers)) continue;
					foreach (var m in rt.Matches)
					{
						float xa,ya,xb,yb;
						if (TryGetXY(m.EntryTime, m.EntryPrice, chartControl, chartScale, out xa, out ya))  DrawTri( rt.IsLong, xa, ya, 4.4f, mba);
						if (TryGetXY(m.ExitTime,  m.ExitPrice,  chartControl, chartScale, out xb, out yb))  DrawTri(!rt.IsLong, xb, yb, 4.4f, mba);
					}
				}
			}
			finally { lngB?.Dispose(); lngBA?.Dispose(); shtB?.Dispose(); shtBA?.Dispose(); prfB?.Dispose(); lssB?.Dispose(); }

			if (hoveredRT == null) return;
			float hx, hy;
			if (HoverShowsIndividualPnL && hoveredFill != null)
			{
				if (TryGetXY(hoveredFill.ExitTime, hoveredFill.ExitPrice, chartControl, chartScale, out hx, out hy))
					DrawHover(hoveredRT, hoveredFill, hx, hy);
			}
			else if (TryGetXY(hoveredRT.LastExitTime, hoveredRT.AvgExitPrice, chartControl, chartScale, out hx, out hy))
				DrawHover(hoveredRT, null, hx, hy);
		}

		private bool TryGetXY(DateTime time, double price, ChartControl cc, ChartScale cs, out float x, out float y)
		{
			x = y = 0f;
			try
			{
				int bar = Bars.GetBar(time);
				if (bar < 0 || bar >= Bars.Count) return false;
				x = cc.GetXByBarIndex(ChartBars, bar);
				y = cs.GetYByValue(price);
				return true;
			}
			catch { return false; }
		}

		private void DrawTri(bool up, float cx, float cy, float sz, SharpDX.Direct2D1.SolidColorBrush brush)
		{
			try
			{
				SharpDX.Direct2D1.PathGeometry pg = new SharpDX.Direct2D1.PathGeometry(Globals.D2DFactory);
				GeometrySink gs = pg.Open();
				gs.SetFillMode(FillMode.Winding);
				if (up)
				{
					gs.BeginFigure(new Vector2(cx, cy - sz*1.4f), FigureBegin.Filled);
					gs.AddLines(new[] { new Vector2(cx+sz, cy+sz*0.7f), new Vector2(cx-sz, cy+sz*0.7f) });
				}
				else
				{
					gs.BeginFigure(new Vector2(cx, cy + sz*1.4f), FigureBegin.Filled);
					gs.AddLines(new[] { new Vector2(cx+sz, cy-sz*0.7f), new Vector2(cx-sz, cy-sz*0.7f) });
				}
				gs.EndFigure(FigureEnd.Closed);
				gs.Close();
				RenderTarget.FillGeometry(pg, brush);
				pg.Dispose(); gs.Dispose();
			}
			catch {}
		}

		private void DrawHover(RoundTrip rt, FillMatch m, float ex, float ey)
		{
			try
			{
				bool isFill = m != null;
				double ticks   = isFill ? m.PnLTicks   : (rt.EntryQtyTotal>0 ? rt.TotalPnLTicks/(double)rt.EntryQtyTotal : 0);
				double dollars = isFill ? m.PnLDollars  : rt.TotalPnLDollars;
				int    qty     = isFill ? m.Quantity     : rt.EntryQtyTotal;
				string label   = "#" + rt.Number + (isFill?" (Fill)":"") + " " + (rt.IsLong?"Long":"Short") + (qty>1?" x"+qty:"")
				               + "\n" + ticks.ToString("+0.##;-0.##;0") + " ticks | " + Fmt(dollars);
				if (RiskAmount > 0) label += " | " + (dollars/RiskAmount).ToString("+0.##;-0.##;0") + "R";
				if (!isFill && ShowMAEMFE && rt.MAEMFECalculated)
					label += "\n" + (dollars>=0 ? "MDD: "+Fmt(rt.MaxAdverseExcursion) : "Peak: "+Fmt(rt.MaxFavorableExcursion));

				bool   pos  = dollars >= 0;
				Color4 fg   = pos ? new Color4(0f,1f,0f,1f)       : new Color4(1f,0.5f,0.5f,1f);
				Color4 bg   = pos ? new Color4(0f,0.18f,0f,0.88f) : new Color4(0.32f,0f,0f,0.88f);

				TextFormat tf = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)LabelFontSize);
				try
				{
					TextLayout tl = new TextLayout(Globals.DirectWriteFactory, label, tf, 320f, 120f);
					try
					{
						TextMetrics tm = tl.Metrics;
						float lx = ex + 10f, ly = ey - tm.Height/2f;
						if (lx + tm.Width + 14f > RenderTarget.Size.Width) lx = ex - tm.Width - 24f;
						if (ly < 2f) ly = 2f;
						SharpDX.Direct2D1.SolidColorBrush bgB = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, bg);
						SharpDX.Direct2D1.SolidColorBrush fgB = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, fg);
						try
						{
							RenderTarget.FillRectangle(new RectangleF(lx-7f, ly-7f, tm.Width+14f, tm.Height+14f), bgB);
							RenderTarget.DrawTextLayout(new Vector2(lx, ly), tl, fgB);
						}
						finally { bgB?.Dispose(); fgB?.Dispose(); }
					}
					finally { tl?.Dispose(); }
				}
				finally { tf?.Dispose(); }
			}
			catch {}
		}

		private SharpDX.Direct2D1.SolidColorBrush ToD2D(System.Windows.Media.Brush wpf, float a = 1f)
		{
			System.Windows.Media.SolidColorBrush scb = wpf as System.Windows.Media.SolidColorBrush;
			if (scb != null)
			{
				System.Windows.Media.Color c = scb.Color;
				return new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(c.R/255f, c.G/255f, c.B/255f, a));
			}
			return new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new Color4(1f,1f,0f,a));
		}

		private static double DistSq(float px, float py, float x1, float y1, float x2, float y2)
		{
			float dx = x2-x1, dy = y2-y1, len2 = dx*dx+dy*dy;
			if (len2 == 0f) return (px-x1)*(px-x1)+(py-y1)*(py-y1);
			float t = Math.Max(0f, Math.Min(1f, ((px-x1)*dx+(py-y1)*dy)/len2));
			float qx = x1+t*dx, qy = y1+t*dy;
			return (px-qx)*(px-qx)+(py-qy)*(py-qy);
		}
	}
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
