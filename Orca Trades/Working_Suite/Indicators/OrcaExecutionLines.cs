using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Data;
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
	public enum OrcaOpenTradeRenderMode
	{
		Off,
		ArrowsOnly,
		IndividualLinesAndArrows
	}

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

		private class ExecutionDisplayEvent
		{
			public int Sequence;
			public DateTime Time;
			public double Price;
			public int Quantity;
			public bool IsBuy;
			public bool IsEntry;
			public bool IsPartialExit;
			public bool IsFinalExit;
			public string Tag;
		}

		private class RoundTrip
		{
			public int Number;
			public bool IsLong;
			public bool IsComplete;
			public string AccountName;
			public string InstrumentFullName;
			public List<FillMatch> Matches = new List<FillMatch>();
			public List<ExecutionDisplayEvent> ExecutionEvents = new List<ExecutionDisplayEvent>();
			public List<PendingEntry> OpenLots = new List<PendingEntry>();
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
			public string Note;
			public List<string> Tags = new List<string>();
			public double RealizedPnl;
			public double CurrentUnrealizedPnl;
			public double CurrentTotalPnl;
			public double MfePnl;
			public double MaePnl;
			public double HighestProfitPnl;
			public double HeatTakenPnl;
			public double LastPrice;
			public DateTime LastPriceTime;
			public int ExecutionSequence;

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

		private class OpenRoundTripSnapshot
		{
			public List<FillMatch> Matches = new List<FillMatch>();
			public List<ExecutionDisplayEvent> ExecutionEvents = new List<ExecutionDisplayEvent>();
			public List<PendingEntry> OpenLots = new List<PendingEntry>();
			public double LastPrice;
			public DateTime LastPriceTime;
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
		private ChartScale lastRenderChartScale;
		private Dictionary<string, string> tradeNotes;
		private Dictionary<string, List<string>> tradeTags;
		private HashSet<string> knownTags;
		private bool noteEditorOpen;
		private bool suppressNextRightClickMenu;

		// â”€â”€ properties â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		[NinjaScriptProperty][Display(Name="Show Execution Lines",   GroupName="1. Visibility", Order=0)] public bool ShowExecutionLines  { get; set; }
		[NinjaScriptProperty][Display(Name="Show Labels",            GroupName="1. Visibility", Order=1)] public bool ShowLabels           { get; set; }
		[Display(Name="Hover Individual PnL",                        GroupName="1. Visibility", Order=2)] public bool HoverShowsIndividualPnL { get; set; }
		[NinjaScriptProperty][Display(Name="Show Average Markers",   GroupName="1. Visibility", Order=3)] public bool ShowMarkers          { get; set; }
		[NinjaScriptProperty][Display(Name="Individual Lines",       GroupName="1. Visibility", Order=4)] public bool ShowIndividualLines  { get; set; }
		[NinjaScriptProperty][Display(Name="Individual Fill Markers",GroupName="1. Visibility", Order=5)] public bool ShowIndividualMarkers { get; set; }
		[NinjaScriptProperty][Display(Name="Show MAE/MFE",          GroupName="1. Visibility", Order=6)] public bool ShowMAEMFE           { get; set; }
		[Display(Name="Open Trade Rendering",                        GroupName="1. Visibility", Order=7)] public OrcaOpenTradeRenderMode OpenTradeRendering { get; set; }
		[Display(Name="Show Session Total",                          GroupName="1. Visibility", Order=8)] public bool ShowSessionTotal     { get; set; }
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
				Calculate   = Calculate.OnPriceChange;
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
				OpenTradeRendering  = OrcaOpenTradeRenderMode.Off;
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
				noteEditorOpen   = false;
				suppressNextRightClickMenu = false;
				lastRenderChartScale = null;
				tradeNotes = new Dictionary<string, string>();
				tradeTags = new Dictionary<string, List<string>>();
				knownTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				LoadTradeNotes();
				LoadJournalTradeData();
				HookAllAccounts();
			}
			else if (State == State.Realtime)
			{
				if (ChartControl != null && !mouseHooked)
				{
					ChartControl.MouseMove  += OnChartMouseMove;
					ChartControl.MouseLeave += OnChartMouseLeave;
					ChartControl.PreviewMouseRightButtonDown += OnChartMouseRightButtonDown;
					ChartControl.PreviewMouseRightButtonUp += OnChartMouseRightButtonUp;
					ChartControl.ContextMenuOpening += OnChartContextMenuOpening;
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
					try { ChartControl.PreviewMouseRightButtonDown -= OnChartMouseRightButtonDown; } catch {}
					try { ChartControl.PreviewMouseRightButtonUp -= OnChartMouseRightButtonUp; } catch {}
					try { ChartControl.ContextMenuOpening -= OnChartContextMenuOpening; } catch {}
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
					RoundTrip closingRT = st.CurrentRT;

					while (rem > 0 && st.OpenFills.Count > 0)
					{
						PendingEntry pe  = st.OpenFills[0];
						int filled       = Math.Min(rem, pe.Quantity);
						bool wasLong     = (pe.Side == MarketPosition.Long);
						double ticks     = wasLong ? (price - pe.Price)/TickSize : (pe.Price - price)/TickSize;
						double dollars   = CalculateRealizedPnl(pe, price, filled);

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
							st.CurrentRT.RealizedPnl    += dollars;
						}
						pe.Quantity -= filled;
						rem         -= filled;
						if (pe.Quantity <= 0) st.OpenFills.RemoveAt(0);
					}
					RemoveClosedLots(closingRT);

					if (closingRT != null)
					{
						bool fullyClosed = toClose == Math.Abs(prev);
						AddExecutionEvent(closingRT, isBuy, price, toClose, time, false, !fullyClosed, fullyClosed);
						UpdateRoundTripPnl(closingRT, price, time);
					}

					if (toClose == Math.Abs(prev) && st.CurrentRT != null)
					{
						UpdateRoundTripPnl(st.CurrentRT, price, time);
						st.CurrentRT.IsComplete = true;
						st.CurrentRT.MAEMFECalculated = true;
						ApplyNoteToRoundTrip(st.CurrentRT);
						while (st.RoundTrips.Count > MaxTradesToShow) st.RoundTrips.RemoveAt(0);
						st.CurrentRT = null;
						if (EnableShotClock && shotClockIsLive) { shotClockEnd = DateTime.UtcNow.AddSeconds(ShotClockSeconds); shotClockActive = true; }
					}
					if (toOpen > 0)
					{
						StartNewRoundTrip(st, isBuy, price, toOpen, time, accountName);
						AddOpenLot(st, st.CurrentRT, isBuy, price, toOpen, time);
						AddExecutionEvent(st.CurrentRT, isBuy, price, toOpen, time, true, false, false);
						UpdateRoundTripPnl(st.CurrentRT, price, time);
					}
					needsRedraw = true;
				}
				else
				{
					if (prev == 0) StartNewRoundTrip(st, isBuy, price, quantity, time, accountName);
					else if (st.CurrentRT != null) { st.CurrentRT.EntryPriceSum += price*(double)quantity; st.CurrentRT.EntryQtyTotal += quantity; }
					AddOpenLot(st, st.CurrentRT, isBuy, price, quantity, time);
					if (st.CurrentRT != null)
					{
						AddExecutionEvent(st.CurrentRT, isBuy, price, quantity, time, true, false, false);
						UpdateRoundTripPnl(st.CurrentRT, price, time);
					}
				}
				st.NetPosition = next;
			}
		}

		private void StartNewRoundTrip(AccountState st, bool isBuy, double price, int qty, DateTime time, string acct)
		{
			st.RTCounter++;
			st.OpenFills.Clear();
			st.CurrentRT = new RoundTrip {
				Number=st.RTCounter, IsLong=isBuy, IsComplete=false, AccountName=acct,
				InstrumentFullName=Instrument != null ? Instrument.FullName : "",
				EntryPriceSum=price*(double)qty, EntryQtyTotal=qty, FirstEntryTime=time, LastExitTime=DateTime.MinValue
			};
			st.CurrentRT.LastPrice = price;
			st.CurrentRT.LastPriceTime = time;
			st.RoundTrips.Add(st.CurrentRT);
		}

		private void AddOpenLot(AccountState st, RoundTrip rt, bool isBuy, double price, int quantity, DateTime time)
		{
			if (st == null || rt == null || quantity <= 0) return;
			PendingEntry lot = new PendingEntry {
				Time = time,
				Price = price,
				Quantity = quantity,
				Side = isBuy ? MarketPosition.Long : MarketPosition.Short
			};
			st.OpenFills.Add(lot);
			rt.OpenLots.Add(lot);
		}

		private void RemoveClosedLots(RoundTrip rt)
		{
			if (rt == null || rt.OpenLots == null) return;
			rt.OpenLots.RemoveAll(l => l == null || l.Quantity <= 0);
		}

		private void AddExecutionEvent(RoundTrip rt, bool isBuy, double price, int quantity, DateTime time, bool isEntry, bool isPartialExit, bool isFinalExit)
		{
			if (rt == null || quantity <= 0) return;
			rt.ExecutionSequence++;
			string type = isEntry ? "OpenArrow" : (isPartialExit ? "PartialArrow" : "CloseArrow");
			rt.ExecutionEvents.Add(new ExecutionDisplayEvent {
				Sequence = rt.ExecutionSequence,
				Time = time,
				Price = price,
				Quantity = quantity,
				IsBuy = isBuy,
				IsEntry = isEntry,
				IsPartialExit = isPartialExit,
				IsFinalExit = isFinalExit,
				Tag = BuildExecutionRenderTag(rt, type, rt.ExecutionSequence)
			});
		}

		private string BuildExecutionRenderTag(RoundTrip rt, string type, int sequence)
		{
			string account = rt != null && !string.IsNullOrEmpty(rt.AccountName) ? rt.AccountName : activeAccountName;
			string instrument = rt != null && !string.IsNullOrEmpty(rt.InstrumentFullName) ? rt.InstrumentFullName : (Instrument != null ? Instrument.FullName : "");
			int number = rt != null ? rt.Number : 0;
			return "OrcaExec_" + (account ?? "") + "*" + (instrument ?? "") + "*" + number.ToString(CultureInfo.InvariantCulture) + "*" + type + "*" + sequence.ToString(CultureInfo.InvariantCulture);
		}

		private double GetPointValue()
		{
			try
			{
				if (Instrument != null && Instrument.MasterInstrument != null && Instrument.MasterInstrument.PointValue > 0)
					return Instrument.MasterInstrument.PointValue;
			}
			catch {}
			return 1.0;
		}

		private double CalculateUnrealizedPnl(IEnumerable<PendingEntry> lots, double lastPrice)
		{
			double pv = GetPointValue();
			double total = 0.0;
			if (lots == null) return total;
			foreach (PendingEntry lot in lots)
			{
				if (lot == null || lot.Quantity <= 0) continue;
				if (lot.Side == MarketPosition.Long)
					total += (lastPrice - lot.Price) * lot.Quantity * pv;
				else if (lot.Side == MarketPosition.Short)
					total += (lot.Price - lastPrice) * lot.Quantity * pv;
			}
			return total;
		}

		private double CalculateRealizedPnl(PendingEntry lot, double exitPrice, int closedQty)
		{
			if (lot == null || closedQty <= 0) return 0.0;
			double pv = GetPointValue();
			return lot.Side == MarketPosition.Long
				? (exitPrice - lot.Price) * closedQty * pv
				: (lot.Price - exitPrice) * closedQty * pv;
		}

		private void UpdateRoundTripPnl(RoundTrip rt, double lastPrice, DateTime lastTime)
		{
			if (rt == null || lastPrice <= 0) return;
			rt.LastPrice = lastPrice;
			if (lastTime != DateTime.MinValue)
				rt.LastPriceTime = lastTime;
			rt.CurrentUnrealizedPnl = rt.IsComplete ? 0.0 : CalculateUnrealizedPnl(rt.OpenLots, lastPrice);
			rt.CurrentTotalPnl = rt.RealizedPnl + rt.CurrentUnrealizedPnl;
			rt.MfePnl = Math.Max(rt.MfePnl, rt.CurrentTotalPnl);
			rt.MaePnl = Math.Min(rt.MaePnl, rt.CurrentTotalPnl);
			rt.HighestProfitPnl = Math.Max(0.0, rt.MfePnl);
			rt.HeatTakenPnl = Math.Max(0.0, -rt.MaePnl);
			rt.MaxFavorableExcursion = rt.HighestProfitPnl;
			rt.MaxAdverseExcursion = -rt.HeatTakenPnl;
		}

		// â”€â”€ history loading â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		private void LoadAllHistory()
		{
			if (LoadSqliteHistory) LoadFromSqlite();
			if (LoadTodayHistory)  LoadFromAccountExecutions();
			CalculateAllMAEMFE();
			ApplyNotesToAllRoundTrips();
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
					foreach (var rt in kv.Value.RoundTrips.Where(r => r.IsComplete))
						CalculateMAEMFE(rt);
		}

		private void CalculateMAEMFE(RoundTrip rt)
		{
			try
			{
				if (rt == null) return;
				if (rt.ExecutionEvents == null || rt.ExecutionEvents.Count == 0)
				{
					rt.HighestProfitPnl = Math.Max(0.0, rt.MfePnl);
					rt.HeatTakenPnl = Math.Max(0.0, -rt.MaePnl);
					rt.MaxFavorableExcursion = rt.HighestProfitPnl;
					rt.MaxAdverseExcursion = -rt.HeatTakenPnl;
					rt.MAEMFECalculated = true;
					return;
				}

				List<PendingEntry> lots = new List<PendingEntry>();
				double realized = 0.0;
				double mfe = 0.0;
				double mae = 0.0;
				double pv = GetPointValue();
				int lastBar = -1;

				foreach (ExecutionDisplayEvent ev in rt.ExecutionEvents.OrderBy(e => e.Time).ThenBy(e => e.Sequence))
				{
					int eventBar = Bars != null ? Bars.GetBar(ev.Time) : -1;
					if (Bars != null && Bars.Count > 0 && eventBar >= 0)
					{
						int start = lastBar < 0 ? eventBar : Math.Max(lastBar, 0);
						int end = Math.Min(eventBar, Bars.Count - 1);
						for (int i = start; i <= end; i++)
						{
							double highPnl = realized + CalculateUnrealizedPnl(lots, High.GetValueAt(i));
							double lowPnl = realized + CalculateUnrealizedPnl(lots, Low.GetValueAt(i));
							mfe = Math.Max(mfe, Math.Max(highPnl, lowPnl));
							mae = Math.Min(mae, Math.Min(highPnl, lowPnl));
						}
						lastBar = eventBar + 1;
					}

					if (ev.IsEntry)
					{
						lots.Add(new PendingEntry {
							Time = ev.Time,
							Price = ev.Price,
							Quantity = ev.Quantity,
							Side = ev.IsBuy ? MarketPosition.Long : MarketPosition.Short
						});
					}
					else
					{
						int rem = ev.Quantity;
						while (rem > 0 && lots.Count > 0)
						{
							PendingEntry lot = lots[0];
							int closed = Math.Min(rem, lot.Quantity);
							realized += lot.Side == MarketPosition.Long
								? (ev.Price - lot.Price) * closed * pv
								: (lot.Price - ev.Price) * closed * pv;
							lot.Quantity -= closed;
							rem -= closed;
							if (lot.Quantity <= 0) lots.RemoveAt(0);
						}
					}

					double current = realized + CalculateUnrealizedPnl(lots, ev.Price);
					mfe = Math.Max(mfe, current);
					mae = Math.Min(mae, current);
				}

				rt.RealizedPnl = rt.TotalPnLDollars;
				rt.CurrentUnrealizedPnl = 0.0;
				rt.CurrentTotalPnl = rt.TotalPnLDollars;
				rt.MfePnl = mfe;
				rt.MaePnl = mae;
				rt.HighestProfitPnl = Math.Max(0.0, mfe);
				rt.HeatTakenPnl = Math.Max(0.0, -mae);
				rt.MaxFavorableExcursion = rt.HighestProfitPnl;
				rt.MaxAdverseExcursion   = -rt.HeatTakenPnl;
				rt.MAEMFECalculated      = true;
			}
			catch {}
		}

		private void UpdateLiveMAEMFE()
		{
			if (!ShowMAEMFE) return;
			if (Instrument == null || Instrument.MasterInstrument == null || CurrentBar < 0) return;
			lock (tradeLock)
			{
				if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName)) return;
				AccountState st = accountStates[activeAccountName];
				if (st.CurrentRT == null || st.NetPosition == 0) return;
				RoundTrip rt = st.CurrentRT;
				UpdateRoundTripPnl(rt, Close[0], Time[0]);
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			try
			{
				if (!ShowMAEMFE || e == null || e.MarketDataType != MarketDataType.Last || e.Price <= 0) return;
				lock (tradeLock)
				{
					if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName)) return;
					AccountState st = accountStates[activeAccountName];
					if (st == null || st.CurrentRT == null || st.NetPosition == 0) return;
					UpdateRoundTripPnl(st.CurrentRT, e.Price, e.Time);
				}
				if (ChartControl != null) ChartControl.InvalidateVisual();
			}
			catch {}
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
					ClearOldDrawings();
					RemoveDrawObject("OrcaRT_SessionTotal");
					lastDrawnAccount = activeAccountName;
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
				string.Format("Shot Clock  {0}:{1:D2}", (int)(rem/60), (int)(rem%60)),
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
				if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName))
				{
					ClearOldDrawings();
					RemoveDrawObject("OrcaRT_SessionTotal");
					return;
				}
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
			else
				RemoveDrawObject("OrcaRT_SessionTotal");
			foreach (var rt in list) DrawRoundTrip(rt);
		}

		private void DrawRoundTrip(RoundTrip rt)
		{
			// Line rendering is offloaded entirely to SharpDX in OnRender to eliminate chart lag
		}

		private string Fmt(double d) { return (d >= 0 ? "+$" : "-$") + Math.Abs(d).ToString("N2"); }
		private string FmtAbs(double d) { return "$" + Math.Abs(d).ToString("N2"); }

		private string NotesFilePath
		{
			get
			{
				string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OrcaJournal");
				return System.IO.Path.Combine(dir, "execution_line_notes.tsv");
			}
		}

		private void LoadTradeNotes()
		{
			try
			{
				if (tradeNotes == null) tradeNotes = new Dictionary<string, string>();
				if (tradeTags == null) tradeTags = new Dictionary<string, List<string>>();
				if (knownTags == null) knownTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				tradeNotes.Clear();
				tradeTags.Clear();
				knownTags.Clear();
				if (!System.IO.File.Exists(NotesFilePath)) return;

				foreach (string line in System.IO.File.ReadAllLines(NotesFilePath))
				{
					if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
					string[] parts = line.Split('\t');
					if (parts.Length >= 2 && parts[0] == "TAG")
					{
						string tag = NormalizeTagName(DecodeText(parts[1]));
						if (!string.IsNullOrEmpty(tag)) knownTags.Add(tag);
					}
					else if (parts.Length >= 3 && parts[0] == "TRADE")
					{
						string key = DecodeText(parts[1]);
						string note = DecodeText(parts[2]);
						List<string> tags = parts.Length >= 4 ? DecodeTags(parts[3]) : new List<string>();
						if (!string.IsNullOrEmpty(key))
						{
							if (!string.IsNullOrWhiteSpace(note)) tradeNotes[key] = note;
							if (tags.Count > 0) tradeTags[key] = tags;
							foreach (string tag in tags) knownTags.Add(tag);
						}
					}
					else if (parts.Length == 2)
					{
						string key = DecodeText(parts[0]);
						string note = DecodeText(parts[1]);
						if (!string.IsNullOrEmpty(key) && !string.IsNullOrWhiteSpace(note))
							tradeNotes[key] = note;
					}
				}
			}
			catch (Exception ex) { Print("OrcaExecLines LoadNotes: " + ex.Message); }
		}

		private void SaveTradeNotes()
		{
			try
			{
				string dir = System.IO.Path.GetDirectoryName(NotesFilePath);
				if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

				List<string> lines = new List<string>();
				lines.Add("# OrcaExecutionLines trade journal data. Values are base64 UTF-8.");
				lines.Add("# TAG<TAB>tag");
				lines.Add("# TRADE<TAB>key<TAB>note<TAB>tag1\\u001Ftag2");

				if (knownTags != null)
					foreach (string tag in knownTags.Where(t => !string.IsNullOrWhiteSpace(t)).OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
						lines.Add("TAG\t" + EncodeText(tag));

				HashSet<string> keys = new HashSet<string>();
				if (tradeNotes != null) foreach (string key in tradeNotes.Keys) keys.Add(key);
				if (tradeTags != null) foreach (string key in tradeTags.Keys) keys.Add(key);

				foreach (string key in keys.OrderBy(k => k))
				{
					string note = "";
					List<string> tags = new List<string>();
					if (tradeNotes != null) tradeNotes.TryGetValue(key, out note);
					if (tradeTags != null && tradeTags.ContainsKey(key)) tags = tradeTags[key];
					if (string.IsNullOrWhiteSpace(note) && (tags == null || tags.Count == 0)) continue;
					lines.Add("TRADE\t" + EncodeText(key) + "\t" + EncodeText(note ?? "") + "\t" + EncodeTags(tags));
				}

				System.IO.File.WriteAllLines(NotesFilePath, lines.ToArray(), new UTF8Encoding(false));
			}
			catch (Exception ex) { Print("OrcaExecLines SaveNotes: " + ex.Message); }
		}

		private string JournalDbPath
		{
			get
			{
				return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OrcaJournal", "orca_journal.db");
			}
		}

		private string JournalSqliteDllPath
		{
			get
			{
				return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8", "bin", "System.Data.SQLite.dll");
			}
		}

		private object OpenJournalConnection(bool createIfMissing)
		{
			if (!System.IO.File.Exists(JournalSqliteDllPath)) return null;
			if (!createIfMissing && !System.IO.File.Exists(JournalDbPath)) return null;

			string dir = System.IO.Path.GetDirectoryName(JournalDbPath);
			if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

			Type connT = Assembly.LoadFrom(JournalSqliteDllPath).GetType("System.Data.SQLite.SQLiteConnection");
			object conn = Activator.CreateInstance(connT, "Data Source=" + JournalDbPath + ";Version=3;");
			connT.GetMethod("Open").Invoke(conn, null);
			TryExecuteJournalNonQuery(conn, "PRAGMA busy_timeout = 3000; PRAGMA foreign_keys = ON;");
			return conn;
		}

		private object CreateJournalCommand(object conn, string sql)
		{
			object cmd = conn.GetType().GetMethod("CreateCommand").Invoke(conn, null);
			cmd.GetType().GetProperty("CommandText").SetValue(cmd, sql, null);
			return cmd;
		}

		private void AddJournalParameter(object cmd, string name, object value)
		{
			Type cmdT = cmd.GetType();
			object p = cmdT.GetMethod("CreateParameter").Invoke(cmd, null);
			Type pT = p.GetType();
			pT.GetProperty("ParameterName").SetValue(p, name, null);
			pT.GetProperty("Value").SetValue(p, value ?? DBNull.Value, null);
			object ps = cmdT.GetProperty("Parameters").GetValue(cmd, null);
			ps.GetType().GetMethod("Add", new[] { pT }).Invoke(ps, new[] { p });
		}

		private void AddJournalParameters(object cmd, params object[] values)
		{
			if (values == null) return;
			for (int i = 0; i + 1 < values.Length; i += 2)
				AddJournalParameter(cmd, (string)values[i], values[i + 1]);
		}

		private object ExecuteJournalScalar(object conn, string sql, params object[] values)
		{
			object cmd = CreateJournalCommand(conn, sql);
			try
			{
				AddJournalParameters(cmd, values);
				return cmd.GetType().GetMethod("ExecuteScalar", Type.EmptyTypes).Invoke(cmd, null);
			}
			finally
			{
				IDisposable d = cmd as IDisposable;
				if (d != null) d.Dispose();
			}
		}

		private void ExecuteJournalNonQuery(object conn, string sql, params object[] values)
		{
			object cmd = CreateJournalCommand(conn, sql);
			try
			{
				AddJournalParameters(cmd, values);
				cmd.GetType().GetMethod("ExecuteNonQuery", Type.EmptyTypes).Invoke(cmd, null);
			}
			finally
			{
				IDisposable d = cmd as IDisposable;
				if (d != null) d.Dispose();
			}
		}

		private bool TryExecuteJournalNonQuery(object conn, string sql, params object[] values)
		{
			try
			{
				ExecuteJournalNonQuery(conn, sql, values);
				return true;
			}
			catch { return false; }
		}

		private object ExecuteJournalReader(object conn, string sql, params object[] values)
		{
			object cmd = CreateJournalCommand(conn, sql);
			AddJournalParameters(cmd, values);
			return cmd.GetType().GetMethod("ExecuteReader", Type.EmptyTypes).Invoke(cmd, null);
		}

		private void EnsureJournalAnnotationSchema(object conn)
		{
			TryExecuteJournalNonQuery(conn,
				"CREATE TABLE IF NOT EXISTS tags (" +
				"id INTEGER PRIMARY KEY AUTOINCREMENT, " +
				"name TEXT NOT NULL UNIQUE, " +
				"color TEXT NOT NULL);");

			TryExecuteJournalNonQuery(conn,
				"CREATE TABLE IF NOT EXISTS execution_line_annotations (" +
				"trade_key TEXT PRIMARY KEY, " +
				"account TEXT NOT NULL DEFAULT '', " +
				"instrument_full_name TEXT NOT NULL DEFAULT '', " +
				"direction TEXT NOT NULL DEFAULT '', " +
				"entry_time TEXT, " +
				"exit_time TEXT, " +
				"notes TEXT, " +
				"updated_at TEXT NOT NULL);");

			TryExecuteJournalNonQuery(conn,
				"CREATE TABLE IF NOT EXISTS execution_line_annotation_tags (" +
				"trade_key TEXT NOT NULL, " +
				"tag_name TEXT NOT NULL, " +
				"PRIMARY KEY (trade_key, tag_name));");

			TryExecuteJournalNonQuery(conn, "ALTER TABLE trades ADD COLUMN trade_key TEXT;");
			TryExecuteJournalNonQuery(conn, "ALTER TABLE trades ADD COLUMN instrument_full_name TEXT NOT NULL DEFAULT '';");
			TryExecuteJournalNonQuery(conn, "ALTER TABLE trade_tags ADD COLUMN source TEXT NOT NULL DEFAULT 'manual';");
			TryExecuteJournalNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_trades_trade_key ON trades(trade_key);");
		}

		private void LoadJournalTradeData()
		{
			object conn = null;
			try
			{
				conn = OpenJournalConnection(false);
				if (conn == null) return;
				EnsureJournalAnnotationSchema(conn);
				LoadJournalKnownTags(conn);
				LoadJournalTradeNotes(conn);
				LoadJournalPendingAnnotations(conn);
			}
			catch (Exception ex) { Print("OrcaExecLines LoadJournal: " + ex.Message); }
			finally
			{
				IDisposable d = conn as IDisposable;
				if (d != null) d.Dispose();
			}
		}

		private void LoadJournalKnownTags(object conn)
		{
			object reader = null;
			try
			{
				reader = ExecuteJournalReader(conn, "SELECT name FROM tags ORDER BY name;");
				Type rT = reader.GetType();
				var mRead = rT.GetMethod("Read");
				var mStr = rT.GetMethod("GetString");
				while ((bool)mRead.Invoke(reader, null))
				{
					string tag = NormalizeTagName((string)mStr.Invoke(reader, new object[] { 0 }));
					if (!string.IsNullOrEmpty(tag)) knownTags.Add(tag);
				}
			}
			catch {}
			finally
			{
				IDisposable d = reader as IDisposable;
				if (d != null) d.Dispose();
			}
		}

		private void LoadJournalTradeNotes(object conn)
		{
			object reader = null;
			try
			{
				reader = ExecuteJournalReader(conn,
					"SELECT trade_key, notes FROM trades " +
					"WHERE trade_key IS NOT NULL AND trade_key <> '';");
				Type rT = reader.GetType();
				var mRead = rT.GetMethod("Read");
				var mStr = rT.GetMethod("GetString");
				var mIsDbNull = rT.GetMethod("IsDBNull");
				while ((bool)mRead.Invoke(reader, null))
				{
					string key = (string)mStr.Invoke(reader, new object[] { 0 });
					if (string.IsNullOrEmpty(key)) continue;
					if (!(bool)mIsDbNull.Invoke(reader, new object[] { 1 }))
					{
						string note = (string)mStr.Invoke(reader, new object[] { 1 });
						if (!string.IsNullOrWhiteSpace(note)) tradeNotes[key] = note;
					}
				}
			}
			catch {}
			finally
			{
				IDisposable d = reader as IDisposable;
				if (d != null) d.Dispose();
			}

			reader = null;
			try
			{
				reader = ExecuteJournalReader(conn,
					"SELECT tr.trade_key, t.name " +
					"FROM trades tr " +
					"INNER JOIN trade_tags tt ON tt.trade_id = tr.id " +
					"INNER JOIN tags t ON t.id = tt.tag_id " +
					"WHERE tr.trade_key IS NOT NULL AND tr.trade_key <> '';");
				Type rT = reader.GetType();
				var mRead = rT.GetMethod("Read");
				var mStr = rT.GetMethod("GetString");
				while ((bool)mRead.Invoke(reader, null))
					AddJournalTagForKey((string)mStr.Invoke(reader, new object[] { 0 }), (string)mStr.Invoke(reader, new object[] { 1 }));
			}
			catch {}
			finally
			{
				IDisposable d = reader as IDisposable;
				if (d != null) d.Dispose();
			}
		}

		private void LoadJournalPendingAnnotations(object conn)
		{
			object reader = null;
			try
			{
				reader = ExecuteJournalReader(conn,
					"SELECT trade_key, notes FROM execution_line_annotations;");
				Type rT = reader.GetType();
				var mRead = rT.GetMethod("Read");
				var mStr = rT.GetMethod("GetString");
				var mIsDbNull = rT.GetMethod("IsDBNull");
				while ((bool)mRead.Invoke(reader, null))
				{
					string key = (string)mStr.Invoke(reader, new object[] { 0 });
					if (string.IsNullOrEmpty(key)) continue;
					if (!(bool)mIsDbNull.Invoke(reader, new object[] { 1 }))
					{
						string note = (string)mStr.Invoke(reader, new object[] { 1 });
						if (!string.IsNullOrWhiteSpace(note)) tradeNotes[key] = note;
					}
				}
			}
			catch {}
			finally
			{
				IDisposable d = reader as IDisposable;
				if (d != null) d.Dispose();
			}

			reader = null;
			try
			{
				reader = ExecuteJournalReader(conn,
					"SELECT trade_key, tag_name FROM execution_line_annotation_tags;");
				Type rT = reader.GetType();
				var mRead = rT.GetMethod("Read");
				var mStr = rT.GetMethod("GetString");
				while ((bool)mRead.Invoke(reader, null))
					AddJournalTagForKey((string)mStr.Invoke(reader, new object[] { 0 }), (string)mStr.Invoke(reader, new object[] { 1 }));
			}
			catch {}
			finally
			{
				IDisposable d = reader as IDisposable;
				if (d != null) d.Dispose();
			}
		}

		private void AddJournalTagForKey(string key, string value)
		{
			string tag = NormalizeTagName(value);
			if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(tag)) return;

			knownTags.Add(tag);
			List<string> tags;
			if (!tradeTags.TryGetValue(key, out tags))
			{
				tags = new List<string>();
				tradeTags[key] = tags;
			}
			if (!tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
				tags.Add(tag);
		}

		private void SaveKnownTagToJournal(string tag)
		{
			string cleanTag = NormalizeTagName(tag);
			if (string.IsNullOrEmpty(cleanTag)) return;

			object conn = null;
			try
			{
				conn = OpenJournalConnection(true);
				if (conn == null) return;
				EnsureJournalAnnotationSchema(conn);
				EnsureJournalTag(conn, cleanTag);
			}
			catch (Exception ex) { Print("OrcaExecLines SaveJournalTag: " + ex.Message); }
			finally
			{
				IDisposable d = conn as IDisposable;
				if (d != null) d.Dispose();
			}
		}

		private void SaveRoundTripJournalToDatabase(RoundTrip rt, string key, string note, List<string> tags)
		{
			object conn = null;
			try
			{
				if (rt == null || string.IsNullOrEmpty(key)) return;
				conn = OpenJournalConnection(true);
				if (conn == null) return;
				EnsureJournalAnnotationSchema(conn);

				List<string> cleanTags = tags ?? new List<string>();
				if (string.IsNullOrWhiteSpace(note) && cleanTags.Count == 0)
				{
					ExecuteJournalNonQuery(conn, "DELETE FROM execution_line_annotation_tags WHERE trade_key = @key;", "@key", key);
					ExecuteJournalNonQuery(conn, "DELETE FROM execution_line_annotations WHERE trade_key = @key;", "@key", key);
					object tradeId = FindJournalTradeId(conn, key, rt);
					if (tradeId != null && tradeId != DBNull.Value)
					{
						ExecuteJournalNonQuery(conn, "UPDATE trades SET notes = NULL WHERE id = @id;", "@id", tradeId);
						DeleteJournalSourceTags(conn, tradeId);
					}
					return;
				}

				ExecuteJournalNonQuery(conn,
					"INSERT OR REPLACE INTO execution_line_annotations " +
					"(trade_key, account, instrument_full_name, direction, entry_time, exit_time, notes, updated_at) " +
					"VALUES (@key, @account, @instrument, @direction, @entry, @exit, @note, @updated);",
					"@key", key,
					"@account", rt.AccountName ?? "",
					"@instrument", Instrument != null ? Instrument.FullName : "",
					"@direction", rt.IsLong ? "L" : "S",
					"@entry", rt.FirstEntryTime.ToString("o"),
					"@exit", rt.LastExitTime.ToString("o"),
					"@note", string.IsNullOrWhiteSpace(note) ? null : note,
					"@updated", DateTime.Now.ToString("o"));

				ExecuteJournalNonQuery(conn, "DELETE FROM execution_line_annotation_tags WHERE trade_key = @key;", "@key", key);
				foreach (string tag in cleanTags)
				{
					string cleanTag = NormalizeTagName(tag);
					if (string.IsNullOrEmpty(cleanTag)) continue;
					EnsureJournalTag(conn, cleanTag);
					ExecuteJournalNonQuery(conn,
						"INSERT OR IGNORE INTO execution_line_annotation_tags (trade_key, tag_name) VALUES (@key, @tag);",
						"@key", key,
						"@tag", cleanTag);
				}

				object id = FindJournalTradeId(conn, key, rt);
				if (id != null && id != DBNull.Value)
				{
					ExecuteJournalNonQuery(conn, "UPDATE trades SET notes = @note WHERE id = @id;", "@note", string.IsNullOrWhiteSpace(note) ? null : note, "@id", id);
					ReplaceJournalSourceTags(conn, id, cleanTags);
				}
			}
			catch (Exception ex) { Print("OrcaExecLines SaveJournalDb: " + ex.Message); }
			finally
			{
				IDisposable d = conn as IDisposable;
				if (d != null) d.Dispose();
			}
		}

		private object FindJournalTradeId(object conn, string key)
		{
			try
			{
				return ExecuteJournalScalar(conn, "SELECT id FROM trades WHERE trade_key = @key LIMIT 1;", "@key", key);
			}
			catch { return null; }
		}

		private object FindJournalTradeId(object conn, string key, RoundTrip rt)
		{
			object id = FindJournalTradeId(conn, key);
			if (id != null && id != DBNull.Value) return id;
			return FindLegacyJournalTradeId(conn, key, rt);
		}

		private object FindLegacyJournalTradeId(object conn, string key, RoundTrip rt)
		{
			try
			{
				if (rt == null) return null;

				string fullInstrument = Instrument != null ? Instrument.FullName : "";
				string instrument = GetJournalShortInstrument(fullInstrument);
				string account = !string.IsNullOrEmpty(rt.AccountName) ? rt.AccountName : activeAccountName;
				if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(instrument)) return null;

				object id = ExecuteJournalScalar(conn,
					"SELECT id FROM trades " +
					"WHERE account = @account AND instrument = @instrument AND direction = @direction " +
					"AND entry_time = @entry AND exit_time = @exit AND quantity = @quantity LIMIT 1;",
					"@account", account,
					"@instrument", instrument,
					"@direction", rt.IsLong ? "Long" : "Short",
					"@entry", rt.FirstEntryTime.ToString("o"),
					"@exit", rt.LastExitTime.ToString("o"),
					"@quantity", rt.EntryQtyTotal);

				if (id != null && id != DBNull.Value)
				{
					TryExecuteJournalNonQuery(conn,
						"UPDATE trades SET trade_key = @key, instrument_full_name = @instrument WHERE id = @id;",
						"@key", key,
						"@instrument", fullInstrument,
						"@id", id);
				}

				return id;
			}
			catch { return null; }
		}

		private string GetJournalShortInstrument(string fullInstrument)
		{
			string value = (fullInstrument ?? "").Trim();
			if (string.IsNullOrEmpty(value)) return "";
			int space = value.IndexOf(' ');
			return space > 0 ? value.Substring(0, space) : value;
		}

		private object EnsureJournalTag(object conn, string tag)
		{
			string cleanTag = NormalizeTagName(tag);
			if (string.IsNullOrEmpty(cleanTag)) return null;

			object existing = ExecuteJournalScalar(conn, "SELECT id FROM tags WHERE name = @name COLLATE NOCASE LIMIT 1;", "@name", cleanTag);
			if (existing != null && existing != DBNull.Value) return existing;

			ExecuteJournalNonQuery(conn, "INSERT OR IGNORE INTO tags (name, color) VALUES (@name, @color);", "@name", cleanTag, "@color", "#4285F4");
			return ExecuteJournalScalar(conn, "SELECT id FROM tags WHERE name = @name COLLATE NOCASE LIMIT 1;", "@name", cleanTag);
		}

		private void DeleteJournalSourceTags(object conn, object tradeId)
		{
			try
			{
				ExecuteJournalNonQuery(conn, "DELETE FROM trade_tags WHERE trade_id = @id AND source = @source;", "@id", tradeId, "@source", "execution_lines");
			}
			catch {}
		}

		private void ReplaceJournalSourceTags(object conn, object tradeId, List<string> tags)
		{
			DeleteJournalSourceTags(conn, tradeId);
			if (tags == null) return;

			foreach (string tag in tags)
			{
				string cleanTag = NormalizeTagName(tag);
				if (string.IsNullOrEmpty(cleanTag)) continue;
				object tagId = EnsureJournalTag(conn, cleanTag);
				if (tagId == null || tagId == DBNull.Value) continue;
				ExecuteJournalNonQuery(conn,
					"INSERT OR IGNORE INTO trade_tags (trade_id, tag_id, auto, source) VALUES (@trade_id, @tag_id, 0, @source);",
					"@trade_id", tradeId,
					"@tag_id", tagId,
					"@source", "execution_lines");
			}
		}

		private string EncodeText(string value)
		{
			if (string.IsNullOrEmpty(value)) return "";
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
		}

		private string DecodeText(string value)
		{
			try
			{
				if (string.IsNullOrEmpty(value)) return "";
				return Encoding.UTF8.GetString(Convert.FromBase64String(value));
			}
			catch { return ""; }
		}

		private string EncodeTags(List<string> tags)
		{
			if (tags == null || tags.Count == 0) return "";
			return EncodeText(string.Join("\u001F", tags.Select(t => NormalizeTagName(t)).Where(t => !string.IsNullOrEmpty(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
		}

		private List<string> DecodeTags(string value)
		{
			List<string> result = new List<string>();
			string raw = DecodeText(value);
			if (string.IsNullOrEmpty(raw)) return result;
			foreach (string part in raw.Split('\u001F'))
			{
				string tag = NormalizeTagName(part);
				if (!string.IsNullOrEmpty(tag) && !result.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
					result.Add(tag);
			}
			return result;
		}

		private string NormalizeTagName(string tag)
		{
			return (tag ?? "").Trim();
		}

		private string BuildTradeKey(RoundTrip rt)
		{
			if (rt == null) return "";
			string instrument = Instrument != null ? Instrument.FullName : "";
			string account = !string.IsNullOrEmpty(rt.AccountName) ? rt.AccountName : activeAccountName;
			return string.Join("|", new string[] {
				account ?? "",
				instrument ?? "",
				rt.IsLong ? "L" : "S",
				rt.FirstEntryTime.Ticks.ToString(CultureInfo.InvariantCulture),
				rt.LastExitTime.Ticks.ToString(CultureInfo.InvariantCulture),
				rt.EntryQtyTotal.ToString(CultureInfo.InvariantCulture),
				rt.ExitQtyTotal.ToString(CultureInfo.InvariantCulture),
				rt.AvgEntryPrice.ToString("R", CultureInfo.InvariantCulture),
				rt.AvgExitPrice.ToString("R", CultureInfo.InvariantCulture)
			});
		}

		private string GetRoundTripNote(RoundTrip rt)
		{
			if (rt == null) return "";
			if (!string.IsNullOrWhiteSpace(rt.Note)) return rt.Note;
			if (tradeNotes == null) return "";

			string note;
			if (tradeNotes.TryGetValue(BuildTradeKey(rt), out note))
			{
				rt.Note = note;
				return note;
			}
			return "";
		}

		private List<string> GetRoundTripTags(RoundTrip rt)
		{
			if (rt == null) return new List<string>();
			if (rt.Tags != null && rt.Tags.Count > 0) return rt.Tags;
			if (tradeTags == null) return new List<string>();

			List<string> tags;
			if (tradeTags.TryGetValue(BuildTradeKey(rt), out tags))
			{
				rt.Tags = tags.ToList();
				return rt.Tags;
			}
			return new List<string>();
		}

		private void ApplyNoteToRoundTrip(RoundTrip rt)
		{
			if (rt == null || tradeNotes == null) return;
			string note;
			if (tradeNotes.TryGetValue(BuildTradeKey(rt), out note))
				rt.Note = note;
			if (tradeTags != null)
			{
				List<string> tags;
				if (tradeTags.TryGetValue(BuildTradeKey(rt), out tags))
					rt.Tags = tags.ToList();
			}
		}

		private void ApplyNotesToAllRoundTrips()
		{
			try
			{
				lock (tradeLock)
					foreach (var kv in accountStates)
						foreach (var rt in kv.Value.RoundTrips.Where(r => r.IsComplete))
							ApplyNoteToRoundTrip(rt);
			}
			catch {}
		}

		private void SaveRoundTripJournal(RoundTrip rt, string note, IEnumerable<string> tags)
		{
			if (rt == null) return;
			if (tradeNotes == null) tradeNotes = new Dictionary<string, string>();
			if (tradeTags == null) tradeTags = new Dictionary<string, List<string>>();
			if (knownTags == null) knownTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			string key = BuildTradeKey(rt);
			string clean = (note ?? "").Trim();
			if (string.IsNullOrWhiteSpace(clean))
			{
				tradeNotes.Remove(key);
				rt.Note = "";
			}
			else
			{
				tradeNotes[key] = clean;
				rt.Note = clean;
			}

			List<string> cleanTags = new List<string>();
			if (tags != null)
			{
				foreach (string tagValue in tags)
				{
					string tag = NormalizeTagName(tagValue);
					if (string.IsNullOrEmpty(tag)) continue;
					if (!cleanTags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
						cleanTags.Add(tag);
					knownTags.Add(tag);
				}
			}
			if (cleanTags.Count == 0)
			{
				tradeTags.Remove(key);
				if (rt.Tags == null) rt.Tags = new List<string>();
				rt.Tags.Clear();
			}
			else
			{
				tradeTags[key] = cleanTags;
				rt.Tags = cleanTags.ToList();
			}

			SaveTradeNotes();
			SaveRoundTripJournalToDatabase(rt, key, clean, cleanTags);
			needsRedraw = true;
			if (ChartControl != null) ChartControl.InvalidateVisual();
		}

		private string FormatNotePreview(string note)
		{
			if (string.IsNullOrWhiteSpace(note)) return "";
			string clean = note.Trim().Replace("\r\n", "\n").Replace('\r', '\n');
			if (clean.Length > 180) clean = clean.Substring(0, 180).TrimEnd() + "...";
			return clean;
		}

		private string FormatTagsPreview(List<string> tags)
		{
			if (tags == null || tags.Count == 0) return "";
			List<string> cleanTags = tags.Select(t => NormalizeTagName(t))
				.Where(t => !string.IsNullOrEmpty(t))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Take(6)
				.ToList();
			if (cleanTags.Count == 0) return "";
			string text = string.Join("  ", cleanTags.Select(t => "#" + t).ToArray());
			if (tags.Count > cleanTags.Count) text += " ...";
			return text;
		}

		private string BuildNoteTagHeader(string tag)
		{
			string cleanTag = NormalizeTagName(tag);
			return string.IsNullOrEmpty(cleanTag) ? "" : "# " + cleanTag;
		}

		private bool NoteContainsTagHeader(string note, string tag)
		{
			string header = BuildNoteTagHeader(tag);
			if (string.IsNullOrEmpty(header) || string.IsNullOrWhiteSpace(note)) return false;
			string[] lines = note.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			foreach (string line in lines)
				if (string.Equals(line.Trim(), header, StringComparison.OrdinalIgnoreCase))
					return true;
			return false;
		}

		private void AppendTagSectionToNoteBox(TextBox box, string tag)
		{
			if (box == null) return;
			string header = BuildNoteTagHeader(tag);
			if (string.IsNullOrEmpty(header) || NoteContainsTagHeader(box.Text, tag)) return;

			string text = box.Text ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (!text.EndsWith("\r\n") && !text.EndsWith("\n"))
					text += Environment.NewLine;
				text += Environment.NewLine;
			}

			box.Text = text + header + Environment.NewLine + "- ";
			box.Focus();
			box.CaretIndex = box.Text.Length;
		}

		private void OpenNoteEditor(RoundTrip rt)
		{
			try
			{
				if (rt == null || ChartControl == null || noteEditorOpen) return;
				string existing = GetRoundTripNote(rt);
				List<string> existingTags = GetRoundTripTags(rt).ToList();
				List<string> tagOptions = knownTags != null
					? knownTags.Where(t => !string.IsNullOrWhiteSpace(t)).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList()
					: new List<string>();
				foreach (string tag in existingTags)
				{
					string cleanTag = NormalizeTagName(tag);
					if (!string.IsNullOrEmpty(cleanTag) && !tagOptions.Any(t => string.Equals(t, cleanTag, StringComparison.OrdinalIgnoreCase)))
						tagOptions.Add(cleanTag);
				}

				ChartControl.Dispatcher.BeginInvoke(new Action(() =>
				{
					try
					{
						if (noteEditorOpen) return;
						noteEditorOpen = true;
						HashSet<string> selectedTags = new HashSet<string>(existingTags.Select(t => NormalizeTagName(t)).Where(t => !string.IsNullOrEmpty(t)), StringComparer.OrdinalIgnoreCase);

						Window owner = Window.GetWindow(ChartControl);
						Window win = new Window
						{
							Title = "Orca Trade Note",
							Width = 500,
							Height = 430,
							MinWidth = 360,
							MinHeight = 340,
							ResizeMode = ResizeMode.CanResizeWithGrip,
							WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
						};
						if (owner != null) win.Owner = owner;

						Grid root = new Grid { Margin = new Thickness(12) };
						root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
						root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
						root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

						TextBlock header = new TextBlock
						{
							Text = BuildNoteEditorHeader(rt),
							Margin = new Thickness(0, 0, 0, 8),
							TextWrapping = TextWrapping.Wrap
						};
						Grid.SetRow(header, 0);
						root.Children.Add(header);

						TextBlock tagsHeader = new TextBlock
						{
							Text = "Tags",
							Margin = new Thickness(0, 0, 0, 4)
						};
						Grid.SetRow(tagsHeader, 1);
						root.Children.Add(tagsHeader);

						WrapPanel tagPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
						ScrollViewer tagScroll = new ScrollViewer
						{
							Content = tagPanel,
							MaxHeight = 90,
							VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
							Margin = new Thickness(0, 0, 0, 4)
						};
						Grid.SetRow(tagScroll, 2);
						root.Children.Add(tagScroll);

						Action<string, bool> addTagCheck = null;
						TextBox box = null;
						addTagCheck = delegate(string tagName, bool checkedState)
						{
							string cleanTag = NormalizeTagName(tagName);
							if (string.IsNullOrEmpty(cleanTag)) return;

							foreach (CheckBox existingBox in tagPanel.Children.OfType<CheckBox>())
							{
								string existingTag = existingBox.Tag as string;
								if (string.Equals(existingTag, cleanTag, StringComparison.OrdinalIgnoreCase))
								{
									existingBox.IsChecked = checkedState;
									return;
								}
							}

							CheckBox check = new CheckBox
							{
								Content = cleanTag,
								Tag = cleanTag,
								IsChecked = checkedState,
								Margin = new Thickness(0, 0, 12, 6),
								MinWidth = 72
							};
							check.Checked += delegate
							{
								selectedTags.Add((string)check.Tag);
								AppendTagSectionToNoteBox(box, (string)check.Tag);
							};
							check.Unchecked += delegate { selectedTags.Remove((string)check.Tag); };
							tagPanel.Children.Add(check);
						};

						foreach (string tag in tagOptions.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
							addTagCheck(tag, selectedTags.Contains(tag));

						StackPanel addTagRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
						TextBox newTagBox = new TextBox { MinWidth = 220, Margin = new Thickness(0, 0, 8, 0) };
						Button addTag = new Button { Content = "Add Tag", MinWidth = 72 };
						Action addEnteredTag = delegate
						{
							string cleanTag = NormalizeTagName(newTagBox.Text);
							if (string.IsNullOrEmpty(cleanTag)) return;
							selectedTags.Add(cleanTag);
							if (knownTags == null) knownTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
							knownTags.Add(cleanTag);
							addTagCheck(cleanTag, true);
							AppendTagSectionToNoteBox(box, cleanTag);
							newTagBox.Text = "";
							SaveTradeNotes();
							SaveKnownTagToJournal(cleanTag);
						};
						addTag.Click += delegate { addEnteredTag(); };
						newTagBox.KeyDown += delegate(object sender, KeyEventArgs args)
						{
							if (args.Key == Key.Enter)
							{
								addEnteredTag();
								args.Handled = true;
							}
						};
						addTagRow.Children.Add(newTagBox);
						addTagRow.Children.Add(addTag);
						Grid.SetRow(addTagRow, 3);
						root.Children.Add(addTagRow);

						box = new TextBox
						{
							Text = existing,
							AcceptsReturn = true,
							TextWrapping = TextWrapping.Wrap,
							VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
							Margin = new Thickness(0, 0, 0, 10)
						};
						Grid.SetRow(box, 4);
						root.Children.Add(box);

						StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
						Button delete = new Button { Content = "Delete", MinWidth = 72, Margin = new Thickness(0, 0, 8, 0) };
						Button cancel = new Button { Content = "Cancel", MinWidth = 72, Margin = new Thickness(0, 0, 8, 0) };
						Button save = new Button { Content = "Save", MinWidth = 72 };
						buttons.Children.Add(delete);
						buttons.Children.Add(cancel);
						buttons.Children.Add(save);
						Grid.SetRow(buttons, 5);
						root.Children.Add(buttons);

						save.Click += delegate { SaveRoundTripJournal(rt, box.Text, selectedTags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase)); win.Close(); };
						delete.Click += delegate { SaveRoundTripJournal(rt, "", new string[0]); win.Close(); };
						cancel.Click += delegate { win.Close(); };
						win.KeyDown += delegate(object sender, KeyEventArgs args)
						{
							if (args.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
							{
								SaveRoundTripJournal(rt, box.Text, selectedTags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase));
								args.Handled = true;
								win.Close();
							}
						};
						win.Closed += delegate { noteEditorOpen = false; };
						win.Content = root;
						win.Show();
						box.Focus();
						box.CaretIndex = box.Text != null ? box.Text.Length : 0;
					}
					catch (Exception ex)
					{
						noteEditorOpen = false;
						Print("OrcaExecLines NoteEditor: " + ex.Message);
					}
				}));
			}
			catch (Exception ex) { Print("OrcaExecLines OpenNote: " + ex.Message); }
		}

		private string BuildNoteEditorHeader(RoundTrip rt)
		{
			if (rt == null) return "";
			string side = rt.IsLong ? "Long" : "Short";
			string inst = Instrument != null ? Instrument.FullName : "";
			return "#" + rt.Number + " " + side + " x" + rt.EntryQtyTotal + " " + inst
				+ "  " + rt.FirstEntryTime.ToString("g") + " -> " + rt.LastExitTime.ToString("t")
				+ "  " + Fmt(rt.TotalPnLDollars);
		}

		private bool TryFindHoveredTrade(System.Windows.Point position, ChartControl chartControl, ChartScale chartScale, out RoundTrip hoveredRT, out FillMatch hoveredFill)
		{
			hoveredRT = null;
			hoveredFill = null;
			if (chartControl == null || chartScale == null || ChartBars == null || Bars == null) return false;

			List<RoundTrip> list;
			lock (tradeLock)
			{
				if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName)) return false;
				list = accountStates[activeAccountName].RoundTrips.Where(rt => rt.IsComplete).ToList();
			}

			float mx = (float)position.X - (ChartPanel != null ? (float)ChartPanel.X : 0);
			float my = (float)position.Y - (ChartPanel != null ? (float)ChartPanel.Y : 0);
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
					foreach (var m in rt.Matches)
					{
						float x1,y1,x2,y2;
						if (TryGetXY(m.EntryTime, m.EntryPrice, chartControl, chartScale, out x1, out y1) &&
						    TryGetXY(m.ExitTime,  m.ExitPrice,  chartControl, chartScale, out x2, out y2))
						{
							double d = DistSq(mx,my,x1,y1,x2,y2);
							if (d < best) { best=d; hoveredRT=rt; hoveredFill=null; }
						}
					}

					float ax1,ay1,ax2,ay2;
					if (TryGetXY(rt.FirstEntryTime, rt.AvgEntryPrice, chartControl, chartScale, out ax1, out ay1) &&
					    TryGetXY(rt.LastExitTime,  rt.AvgExitPrice,  chartControl, chartScale, out ax2, out ay2))
					{
						double d = DistSq(mx,my,ax1,ay1,ax2,ay2);
						if (d < best) { best=d; hoveredRT=rt; hoveredFill=null; }
					}
				}
			}
			return hoveredRT != null;
		}

		// â”€â”€ mouse â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		private OpenRoundTripSnapshot CreateOpenRoundTripSnapshot(RoundTrip rt)
		{
			if (rt == null || rt.IsComplete) return null;
			OpenRoundTripSnapshot snapshot = new OpenRoundTripSnapshot {
				LastPrice = rt.LastPrice,
				LastPriceTime = rt.LastPriceTime
			};
			if (rt.Matches != null)
				snapshot.Matches = rt.Matches.ToList();
			if (rt.ExecutionEvents != null)
				snapshot.ExecutionEvents = rt.ExecutionEvents.OrderBy(e => e.Sequence).ToList();
			if (rt.OpenLots != null)
				foreach (PendingEntry lot in rt.OpenLots)
					if (lot != null && lot.Quantity > 0)
						snapshot.OpenLots.Add(new PendingEntry {
							Time = lot.Time,
							Price = lot.Price,
							Quantity = lot.Quantity,
							Side = lot.Side
						});
			return snapshot;
		}

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

		private void OnChartMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (!ShowLabels || ChartControl == null || lastRenderChartScale == null) return;
				mousePosition = e.GetPosition(ChartControl);
				isMouseOverChart = true;

				RoundTrip rt;
				FillMatch fill;
				if (!TryFindHoveredTrade(mousePosition, ChartControl, lastRenderChartScale, out rt, out fill)) return;

				suppressNextRightClickMenu = true;
				e.Handled = true;
				OpenNoteEditor(rt);
				ChartControl.Dispatcher.BeginInvoke(new Action(() => suppressNextRightClickMenu = false), DispatcherPriority.ApplicationIdle);
			}
			catch (Exception ex) { Print("OrcaExecLines NoteClick: " + ex.Message); }
		}

		// â”€â”€ render â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
		private void OnChartMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (suppressNextRightClickMenu)
				e.Handled = true;
		}

		private void OnChartContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
		{
			if (!suppressNextRightClickMenu) return;
			e.Handled = true;
			suppressNextRightClickMenu = false;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (!ShowExecutionLines || RenderTarget == null || ChartBars == null || Bars == null) return;
			lastRenderChartScale = chartScale;
			List<RoundTrip> list;
			OpenRoundTripSnapshot openSnapshot = null;
			lock (tradeLock)
			{
				if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName)) return;
				AccountState st = accountStates[activeAccountName];
				list = st.RoundTrips.Where(rt => rt.IsComplete).ToList();
				if (OpenTradeRendering != OrcaOpenTradeRenderMode.Off && st.CurrentRT != null && !st.CurrentRT.IsComplete)
					openSnapshot = CreateOpenRoundTripSnapshot(st.CurrentRT);
			}
			if (list.Count == 0 && openSnapshot == null) return;

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
						foreach (var m in rt.Matches)
						{
							float x1,y1,x2,y2;
							if (TryGetXY(m.EntryTime, m.EntryPrice, chartControl, chartScale, out x1, out y1) &&
							    TryGetXY(m.ExitTime,  m.ExitPrice,  chartControl, chartScale, out x2, out y2))
							{
								double d = DistSq(mx,my,x1,y1,x2,y2);
								if (d < best) { best=d; hoveredRT=rt; hoveredFill=null; }
							}
						}

						float ax1,ay1,ax2,ay2;
						if (TryGetXY(rt.FirstEntryTime, rt.AvgEntryPrice, chartControl, chartScale, out ax1, out ay1) &&
						    TryGetXY(rt.LastExitTime,  rt.AvgExitPrice,  chartControl, chartScale, out ax2, out ay2))
						{
							double d = DistSq(mx,my,ax1,ay1,ax2,ay2);
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
				DrawOpenRoundTrip(openSnapshot, chartControl, chartScale, lngB, lngBA, shtB, shtBA, prfB, lssB);
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

		private void DrawOpenRoundTrip(
			OpenRoundTripSnapshot snapshot,
			ChartControl chartControl,
			ChartScale chartScale,
			SharpDX.Direct2D1.SolidColorBrush longBrush,
			SharpDX.Direct2D1.SolidColorBrush longFadedBrush,
			SharpDX.Direct2D1.SolidColorBrush shortBrush,
			SharpDX.Direct2D1.SolidColorBrush shortFadedBrush,
			SharpDX.Direct2D1.SolidColorBrush profitBrush,
			SharpDX.Direct2D1.SolidColorBrush lossBrush)
		{
			if (snapshot == null || OpenTradeRendering == OrcaOpenTradeRenderMode.Off) return;

			if (OpenTradeRendering == OrcaOpenTradeRenderMode.IndividualLinesAndArrows)
			{
				foreach (FillMatch m in snapshot.Matches)
				{
					float xa, ya, xb, yb;
					if (TryGetXY(m.EntryTime, m.EntryPrice, chartControl, chartScale, out xa, out ya) &&
					    TryGetXY(m.ExitTime,  m.ExitPrice,  chartControl, chartScale, out xb, out yb))
						RenderTarget.DrawLine(new Vector2(xa, ya), new Vector2(xb, yb), m.PnLDollars >= 0 ? profitBrush : lossBrush, Math.Max(1f, (float)LineWidth - 0.5f));
				}
				foreach (PendingEntry lot in snapshot.OpenLots)
				{
					float xa, ya, xb, yb;
					if (TryGetXY(lot.Time, lot.Price, chartControl, chartScale, out xa, out ya) &&
					    TryGetXY(snapshot.LastPriceTime, snapshot.LastPrice, chartControl, chartScale, out xb, out yb))
					{
						double openPnl = CalculateUnrealizedPnl(new[] { lot }, snapshot.LastPrice);
						RenderTarget.DrawLine(new Vector2(xa, ya), new Vector2(xb, yb), openPnl >= 0 ? profitBrush : lossBrush, Math.Max(1f, (float)LineWidth - 0.5f));
					}
				}
			}

			if (snapshot.ExecutionEvents == null) return;
			foreach (ExecutionDisplayEvent ev in snapshot.ExecutionEvents)
			{
				float x, y;
				if (!TryGetXY(ev.Time, ev.Price, chartControl, chartScale, out x, out y)) continue;
				SharpDX.Direct2D1.SolidColorBrush brush = ev.IsBuy
					? (ev.IsEntry ? longBrush : longFadedBrush)
					: (ev.IsEntry ? shortBrush : shortFadedBrush);
				float size = ev.IsPartialExit ? 6.2f : (ev.IsFinalExit ? 6.8f : 5.2f);
				DrawTri(ev.IsBuy, x, y, size, brush);
			}
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
				double points  = ticks * TickSize;
				double dollars = isFill ? m.PnLDollars  : rt.TotalPnLDollars;
				int    qty     = isFill ? m.Quantity     : rt.EntryQtyTotal;
				string label   = "#" + rt.Number + (isFill?" (Fill)":"") + " " + (rt.IsLong?"Long":"Short") + (qty>1?" x"+qty:"")
				               + "\n" + points.ToString("+0.##;-0.##;0") + " pts | " + Fmt(dollars);
				if (RiskAmount > 0) label += " | " + (dollars/RiskAmount).ToString("+0.##;-0.##;0") + "R";
				if (!isFill && ShowMAEMFE && rt.MAEMFECalculated)
					label += "\nMax Profit Seen: " + FmtAbs(rt.HighestProfitPnl) + "  |  Heat Taken: " + FmtAbs(rt.HeatTakenPnl);
				string tagPreview = FormatTagsPreview(GetRoundTripTags(rt));
				if (!string.IsNullOrEmpty(tagPreview))
					label += "\nTags: " + tagPreview;
				string notePreview = FormatNotePreview(GetRoundTripNote(rt));
				if (!string.IsNullOrEmpty(notePreview))
					label += "\nNote: " + notePreview;

				bool   pos  = dollars >= 0;
				Color4 fg   = pos ? new Color4(0f,1f,0f,1f)       : new Color4(1f,0.5f,0.5f,1f);
				Color4 bg   = pos ? new Color4(0f,0.18f,0f,0.88f) : new Color4(0.32f,0f,0f,0.88f);

				TextFormat tf = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, (float)LabelFontSize);
				try
				{
					TextLayout tl = new TextLayout(Globals.DirectWriteFactory, label, tf, 420f, 240f);
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
