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

		public double AvgEntryPrice
		{
			get
			{
				if (EntryQtyTotal <= 0)
				{
					return 0.0;
				}
				return EntryPriceSum / (double)EntryQtyTotal;
			}
		}

		public double AvgExitPrice
		{
			get
			{
				if (ExitQtyTotal <= 0)
				{
					return 0.0;
				}
				return ExitPriceSum / (double)ExitQtyTotal;
			}
		}
	}

	private class AccountState
	{
		public string AccountName;

		public List<PendingEntry> OpenFills = new List<PendingEntry>();

		public List<RoundTrip> RoundTrips = new List<RoundTrip>();

		public RoundTrip CurrentRT;

		public int RTCounter;

		public int NetPosition;
	}

	private Dictionary<string, AccountState> accountStates;

	private List<Account> hookedAccounts;

	private string activeAccountName;

	private string lastDrawnAccount;

	private object tradeLock = new object();

	private bool needsRedraw;

	private bool historyLoaded;

	private DateTime shotClockEnd = DateTime.MinValue;

	private bool shotClockActive;

	private bool shotClockIsLive;

	private Point mousePosition = new Point(-1.0, -1.0);
	private bool isMouseOverChart;
	private DateTime lastAccountCheck = DateTime.MinValue;

	[NinjaScriptProperty]
	[Display(Name = "Show Execution Lines", GroupName = "1. Visibility", Order = 0)]
	public bool ShowExecutionLines { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Labels", GroupName = "1. Visibility", Order = 1)]
	public bool ShowLabels { get; set; }

	[Display(Name = "Hover Individual PnL", GroupName = "1. Visibility", Order = 2)]
	public bool HoverShowsIndividualPnL { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Average Markers", GroupName = "1. Visibility", Order = 3)]
	public bool ShowMarkers { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Individual Lines (vs Averaged)", GroupName = "1. Visibility", Order = 4)]
	public bool ShowIndividualLines { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Individual Fill Markers", GroupName = "1. Visibility", Order = 5)]
	public bool ShowIndividualMarkers { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show MAE/MFE", Description = "Winners show max drawdown, losers show peak unrealized gain", GroupName = "1. Visibility", Order = 6)]
	public bool ShowMAEMFE { get; set; }

	[Display(Name = "Show Session Total", GroupName = "1. Visibility", Order = 7)]
	public bool ShowSessionTotal { get; set; }

	[Display(Name = "Session Total Position", GroupName = "3. Appearance", Order = 2)]
	public TextPosition SessionTotalPosition { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Enable Shot Clock", Description = "Show a cooldown countdown after each completed trade", GroupName = "5. Shot Clock", Order = 0)]
	public bool EnableShotClock { get; set; }

	[NinjaScriptProperty]
	[Range(5, 3600)]
	[Display(Name = "Cooldown Duration (seconds)", Description = "How long the shot clock counts down (default 300 = 5 min)", GroupName = "5. Shot Clock", Order = 1)]
	public int ShotClockSeconds { get; set; }

	[Display(Name = "Label Position", GroupName = "5. Shot Clock", Order = 2)]
	public TextPosition ShotClockPosition { get; set; }

	[XmlIgnore]
	[Display(Name = "Countdown Color", GroupName = "5. Shot Clock", Order = 3)]
	public Brush ShotClockColor { get; set; }

	[Browsable(false)]
	public string ShotClockColorSerializable
	{
		get
		{
			return Serialize.BrushToString(ShotClockColor);
		}
		set
		{
			ShotClockColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Warning Color (≤ 30 s)", GroupName = "5. Shot Clock", Order = 4)]
	public Brush ShotClockWarningColor { get; set; }

	[Browsable(false)]
	public string ShotClockWarningColorSerializable
	{
		get
		{
			return Serialize.BrushToString(ShotClockWarningColor);
		}
		set
		{
			ShotClockWarningColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Load from Account (live session)", GroupName = "2. Data", Order = 0)]
	public bool LoadTodayHistory { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Load from SQLite (all history)", GroupName = "2. Data", Order = 1)]
	public bool LoadSqliteHistory { get; set; }

	[NinjaScriptProperty]
	[Range(0, 100000)]
	[Display(Name = "Risk Amount ($)", Description = "Dollar risk per trade for R-multiple calc. 0 = disabled", GroupName = "2. Data", Order = 2)]
	public double RiskAmount { get; set; }

	[NinjaScriptProperty]
	[Range(1, 5)]
	[Display(Name = "Line Width", GroupName = "3. Appearance", Order = 0)]
	public int LineWidth { get; set; }

	[NinjaScriptProperty]
	[Range(8, 20)]
	[Display(Name = "Label Font Size", GroupName = "3. Appearance", Order = 1)]
	public int LabelFontSize { get; set; }

	[Range(5, 200)]
	[Display(Name = "Max Trades To Show", GroupName = "2. Data", Order = 3)]
	public int MaxTradesToShow { get; set; }

	[XmlIgnore]
	[Display(Name = "Profit Color", GroupName = "4. Colors", Order = 0)]
	public Brush ProfitColor { get; set; }

	[Browsable(false)]
	public string ProfitColorSerializable
	{
		get
		{
			return Serialize.BrushToString(ProfitColor);
		}
		set
		{
			ProfitColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Loss Color", GroupName = "4. Colors", Order = 1)]
	public Brush LossColor { get; set; }

	[Browsable(false)]
	public string LossColorSerializable
	{
		get
		{
			return Serialize.BrushToString(LossColor);
		}
		set
		{
			LossColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Long Marker Color", GroupName = "4. Colors", Order = 2)]
	public Brush LongMarkerColor { get; set; }

	[Browsable(false)]
	public string LongMarkerColorSerializable
	{
		get
		{
			return Serialize.BrushToString(LongMarkerColor);
		}
		set
		{
			LongMarkerColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Short Marker Color", GroupName = "4. Colors", Order = 3)]
	public Brush ShortMarkerColor { get; set; }

	[Browsable(false)]
	public string ShortMarkerColorSerializable
	{
		get
		{
			return Serialize.BrushToString(ShortMarkerColor);
		}
		set
		{
			ShortMarkerColor = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Invalid comparison between Unknown and I4
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Invalid comparison between Unknown and I4
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Automatic execution lines with FIFO round-trip matching, SQLite history, and R-multiple tracking";
			((NinjaScriptBase)this).Name = "OrcaExecutionLines";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = false;
			ShowExecutionLines = true;
			ShowLabels = true;
			HoverShowsIndividualPnL = false;
			ShowMarkers = true;
			ShowIndividualLines = true;
			ShowIndividualMarkers = true;
			ShowMAEMFE = true;
			ShowSessionTotal = true;
			SessionTotalPosition = TextPosition.TopRight;
			LoadTodayHistory = true;
			LoadSqliteHistory = true;
			EnableShotClock = true;
			ShotClockSeconds = 300;
			ShotClockPosition = TextPosition.BottomRight;
			ShotClockColor = Brushes.Orange;
			ShotClockWarningColor = Brushes.Red;
			LineWidth = 2;
			LabelFontSize = 11;
			MaxTradesToShow = 50;
			RiskAmount = 200.0;
			ProfitColor = Brushes.DodgerBlue;
			LossColor = Brushes.Tomato;
			LongMarkerColor = Brushes.Lime;
			ShortMarkerColor = Brushes.Red;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			accountStates = new Dictionary<string, AccountState>();
			hookedAccounts = new List<Account>();
			needsRedraw = false;
			historyLoaded = false;
			activeAccountName = "";
			lastDrawnAccount = "";
			shotClockActive = false;
			shotClockIsLive = false;
			HookAllAccounts();
		}
		else if ((int)((NinjaScript)this).State == 5)
		{
			if (((IndicatorRenderBase)this).ChartControl != null)
			{
				((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseMove += OnChartMouseMove;
				((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseLeave += OnChartMouseLeave;
			}
		}
		else
		{
			if ((int)((NinjaScript)this).State != 8)
			{
				return;
			}
			UnhookAllAccounts();
			if (((IndicatorRenderBase)this).ChartControl != null)
			{
				try
				{
					((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseMove -= OnChartMouseMove;
				}
				catch
				{
				}
				try
				{
					((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseLeave -= OnChartMouseLeave;
				}
				catch
				{
				}
			}
			try
			{
				((IndicatorRenderBase)this).RemoveDrawObject("OrcaShotClock");
			}
			catch
			{
			}
		}
	}

	private void HookAllAccounts()
	{
		try
		{
			foreach (Account item in Account.All)
			{
				item.ExecutionUpdate += OnExecutionUpdate;
				hookedAccounts.Add(item);
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)("OrcaExecLines HookAll error: " + ex.Message));
		}
	}

	private void UnhookAllAccounts()
	{
		try
		{
			foreach (Account hookedAccount in hookedAccounts)
			{
				try
				{
					hookedAccount.ExecutionUpdate -= OnExecutionUpdate;
				}
				catch
				{
				}
			}
			hookedAccounts.Clear();
		}
		catch
		{
		}
	}

	private AccountState GetOrCreateState(string accountName)
	{
		if (!accountStates.ContainsKey(accountName))
		{
			accountStates[accountName] = new AccountState
			{
				AccountName = accountName
			};
		}
		return accountStates[accountName];
	}

	private string GetChartTraderAccount()
	{
		try
		{
			if (((IndicatorRenderBase)this).ChartControl == null)
			{
				return "";
			}
			string result = "";
			((DispatcherObject)(object)((IndicatorRenderBase)this).ChartControl).Dispatcher.InvokeAsync(delegate
			{
				try
				{
					Window window = Window.GetWindow(((FrameworkElement)(object)((IndicatorRenderBase)this).ChartControl).Parent);
					Chart val = (Chart)(object)((window is Chart) ? window : null);
					if (val != null && val.ChartTrader != null && val.ChartTrader.Account != null)
					{
						result = val.ChartTrader.Account.Name;
					}
				}
				catch
				{
				}
			}).Wait(TimeSpan.FromMilliseconds(100.0));
			return result;
		}
		catch
		{
			return "";
		}
	}

	private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Invalid comparison between Unknown and I4
		try
		{
			if (e.Execution != null && e.Execution.Instrument != null && ((NinjaScriptBase)this).Instrument != null && !(e.Execution.Instrument.FullName != ((NinjaScriptBase)this).Instrument.FullName) && e.Execution.Order != null)
			{
				string accountName = ((e.Execution.Account != null) ? e.Execution.Account.Name : "Unknown");
				bool isBuy = (int)e.Execution.Order.OrderAction == 0 || (int)e.Execution.Order.OrderAction == 1;
				ProcessExecution(isBuy, e.Execution.Price, e.Execution.Quantity, e.Execution.Time, accountName);
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)("OrcaExecLines OnExecUpdate error: " + ex.Message));
		}
	}

	private void ProcessExecution(bool isBuy, double price, int quantity, DateTime time, string accountName)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		lock (tradeLock)
		{
			AccountState orCreateState = GetOrCreateState(accountName);
			int netPosition = orCreateState.NetPosition;
			int num = orCreateState.NetPosition + (isBuy ? quantity : (-quantity));
			if ((orCreateState.NetPosition > 0 && !isBuy) || (orCreateState.NetPosition < 0 && isBuy))
			{
				int num2 = Math.Min(quantity, Math.Abs(orCreateState.NetPosition));
				int num3 = quantity - num2;
				int num4 = num2;
				while (num4 > 0 && orCreateState.OpenFills.Count > 0)
				{
					PendingEntry pendingEntry = orCreateState.OpenFills[0];
					int num5 = Math.Min(num4, pendingEntry.Quantity);
					bool flag = (int)pendingEntry.Side == 0;
					double num6 = (flag ? ((price - pendingEntry.Price) / ((NinjaScriptBase)this).TickSize) : ((pendingEntry.Price - price) / ((NinjaScriptBase)this).TickSize));
					double num7 = num6 * ((NinjaScriptBase)this).TickSize * ((NinjaScriptBase)this).Instrument.MasterInstrument.PointValue * (double)num5;
					if (orCreateState.CurrentRT != null)
					{
						orCreateState.CurrentRT.Matches.Add(new FillMatch
						{
							EntryTime = pendingEntry.Time,
							EntryPrice = pendingEntry.Price,
							ExitTime = time,
							ExitPrice = price,
							Quantity = num5,
							IsLong = flag,
							PnLTicks = num6,
							PnLDollars = num7
						});
						orCreateState.CurrentRT.ExitPriceSum += price * (double)num5;
						orCreateState.CurrentRT.ExitQtyTotal += num5;
						orCreateState.CurrentRT.LastExitTime = time;
						orCreateState.CurrentRT.TotalPnLDollars += num7;
						orCreateState.CurrentRT.TotalPnLTicks += num6 * (double)num5;
					}
					pendingEntry.Quantity -= num5;
					num4 -= num5;
					if (pendingEntry.Quantity <= 0)
					{
						orCreateState.OpenFills.RemoveAt(0);
					}
				}
				if (num == 0 && orCreateState.CurrentRT != null)
				{
					orCreateState.CurrentRT.IsComplete = true;
					while (orCreateState.RoundTrips.Count > MaxTradesToShow)
					{
						orCreateState.RoundTrips.RemoveAt(0);
					}
					orCreateState.CurrentRT = null;
					if (EnableShotClock && shotClockIsLive)
					{
						shotClockEnd = DateTime.UtcNow.AddSeconds(ShotClockSeconds);
						shotClockActive = true;
					}
				}
				if (num3 > 0)
				{
					StartNewRoundTrip(orCreateState, isBuy, price, num3, time, accountName);
					orCreateState.OpenFills.Add(new PendingEntry
					{
						Time = time,
						Price = price,
						Quantity = num3,
						Side = (MarketPosition)(!isBuy)
					});
				}
				needsRedraw = true;
			}
			else
			{
				if (netPosition == 0)
				{
					StartNewRoundTrip(orCreateState, isBuy, price, quantity, time, accountName);
				}
				else if (orCreateState.CurrentRT != null)
				{
					orCreateState.CurrentRT.EntryPriceSum += price * (double)quantity;
					orCreateState.CurrentRT.EntryQtyTotal += quantity;
				}
				orCreateState.OpenFills.Add(new PendingEntry
				{
					Time = time,
					Price = price,
					Quantity = quantity,
					Side = (MarketPosition)(!isBuy)
				});
			}
			orCreateState.NetPosition = num;
		}
	}

	private void StartNewRoundTrip(AccountState state, bool isBuy, double price, int quantity, DateTime time, string accountName)
	{
		state.RTCounter++;
		state.CurrentRT = new RoundTrip
		{
			Number = state.RTCounter,
			IsLong = isBuy,
			IsComplete = false,
			AccountName = accountName,
			EntryPriceSum = price * (double)quantity,
			EntryQtyTotal = quantity,
			FirstEntryTime = time,
			LastExitTime = DateTime.MinValue
		};
		state.RoundTrips.Add(state.CurrentRT);
	}

	private void LoadAllHistory()
	{
		if (LoadSqliteHistory)
		{
			LoadFromSqlite();
		}
		if (LoadTodayHistory)
		{
			LoadFromAccountExecutions();
		}
		CalculateAllMAEMFE();
		int num = 0;
		lock (tradeLock)
		{
			foreach (KeyValuePair<string, AccountState> accountState in accountStates)
			{
				num += accountState.Value.RoundTrips.Count((RoundTrip rt) => rt.IsComplete);
			}
		}
		((NinjaScript)this).Print((object)("OrcaExecLines: Total " + num + " completed round trips across all accounts"));
		if (num > 0)
		{
			needsRedraw = true;
		}
	}

	private void LoadFromSqlite()
	{
		try
		{
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NinjaTrader 8", "db", "NinjaTrader.sqlite");
			if (!File.Exists(text))
			{
				((NinjaScript)this).Print((object)"OrcaExecLines: SQLite DB not found");
				return;
			}
			string text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NinjaTrader 8", "bin", "System.Data.SQLite.dll");
			if (!File.Exists(text2))
			{
				((NinjaScript)this).Print((object)"OrcaExecLines: SQLite DLL not found");
				return;
			}
			Type type = Assembly.LoadFrom(text2).GetType("System.Data.SQLite.SQLiteConnection");
			string text3 = "Data Source=" + text + ";Read Only=True";
			object obj = Activator.CreateInstance(type, text3);
			try
			{
				type.GetMethod("Open").Invoke(obj, null);
				string name = ((NinjaScriptBase)this).Instrument.MasterInstrument.Name;
				object obj2 = type.GetMethod("CreateCommand").Invoke(obj, null);
				Type type2 = obj2.GetType();
				type2.GetProperty("CommandText").SetValue(obj2, "SELECT e.Time, a.Name, e.MarketPosition, e.Price, e.Quantity FROM Executions e INNER JOIN Accounts a ON e.Account = a.Id INNER JOIN Instruments i ON e.Instrument = i.Id INNER JOIN MasterInstruments mi ON i.MasterInstrument = mi.Id WHERE mi.Name = @instName ORDER BY e.Time ASC", null);
				object obj3 = type2.GetMethod("CreateParameter").Invoke(obj2, null);
				Type type3 = obj3.GetType();
				type3.GetProperty("ParameterName").SetValue(obj3, "@instName", null);
				type3.GetProperty("Value").SetValue(obj3, name, null);
				object value = type2.GetProperty("Parameters").GetValue(obj2, null);
				value.GetType().GetMethod("Add", new Type[1] { type3 }).Invoke(value, new object[1] { obj3 });
				object obj4 = type2.GetMethod("ExecuteReader", Type.EmptyTypes).Invoke(obj2, null);
				Type type4 = obj4.GetType();
				MethodInfo method = type4.GetMethod("Read");
				MethodInfo method2 = type4.GetMethod("GetInt64");
				MethodInfo method3 = type4.GetMethod("GetString");
				MethodInfo method4 = type4.GetMethod("GetInt32");
				MethodInfo method5 = type4.GetMethod("GetDouble");
				int num = 0;
				while ((bool)method.Invoke(obj4, null))
				{
					long ticks = (long)method2.Invoke(obj4, new object[1] { 0 });
					string accountName = (string)method3.Invoke(obj4, new object[1] { 1 });
					int num2 = (int)method4.Invoke(obj4, new object[1] { 2 });
					double price = (double)method5.Invoke(obj4, new object[1] { 3 });
					int quantity = (int)method4.Invoke(obj4, new object[1] { 4 });
					DateTime time = new DateTime(ticks);
					bool isBuy = num2 == 1;
					ProcessExecution(isBuy, price, quantity, time, accountName);
					num++;
				}
				type4.GetMethod("Close").Invoke(obj4, null);
				((NinjaScript)this).Print((object)("OrcaExecLines: Loaded " + num + " executions from SQLite for " + name));
			}
			finally
			{
				try
				{
					type.GetMethod("Close").Invoke(obj, null);
				}
				catch
				{
				}
				try
				{
					type.GetMethod("Dispose").Invoke(obj, null);
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)("OrcaExecLines SQLite error: " + ex.Message));
		}
	}

	private void LoadFromAccountExecutions()
	{
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Invalid comparison between Unknown and I4
		try
		{
			if (((NinjaScriptBase)this).Instrument == null)
			{
				return;
			}
			string chartInstrument = ((NinjaScriptBase)this).Instrument.FullName;
			HashSet<string> hashSet = new HashSet<string>();
			lock (tradeLock)
			{
				foreach (KeyValuePair<string, AccountState> accountState in accountStates)
				{
					foreach (RoundTrip roundTrip in accountState.Value.RoundTrips)
					{
						foreach (FillMatch match in roundTrip.Matches)
						{
							hashSet.Add(match.EntryTime.Ticks + "_" + match.EntryPrice + "_" + match.Quantity);
							hashSet.Add(match.ExitTime.Ticks + "_" + match.ExitPrice + "_" + match.Quantity);
						}
					}
				}
			}
			foreach (Account hookedAccount in hookedAccounts)
			{
				try
				{
					List<Execution> list = (from val in hookedAccount.Executions
						where val.Instrument != null && val.Instrument.FullName == chartInstrument && val.Order != null
						orderby val.Time
						select val).ToList();
					int num = 0;
					foreach (Execution item2 in list)
					{
						string item = item2.Time.Ticks + "_" + item2.Price + "_" + item2.Quantity;
						if (!hashSet.Contains(item))
						{
							bool isBuy = (int)item2.Order.OrderAction == 0 || (int)item2.Order.OrderAction == 1;
							ProcessExecution(isBuy, item2.Price, item2.Quantity, item2.Time, hookedAccount.Name);
							num++;
						}
					}
					if (num > 0)
					{
						((NinjaScript)this).Print((object)("OrcaExecLines: Loaded " + num + " new executions from account " + hookedAccount.Name));
					}
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)("OrcaExecLines AccountExec error: " + ex.Message));
		}
	}

	private void CalculateAllMAEMFE()
	{
		if (!ShowMAEMFE)
		{
			return;
		}
		lock (tradeLock)
		{
			foreach (KeyValuePair<string, AccountState> accountState in accountStates)
			{
				foreach (RoundTrip item in accountState.Value.RoundTrips.Where((RoundTrip r) => r.IsComplete && !r.MAEMFECalculated))
				{
					CalculateMAEMFE(item);
				}
			}
		}
	}

	private void CalculateMAEMFE(RoundTrip rt)
	{
		try
		{
			if (((NinjaScriptBase)this).Bars == null || ((NinjaScriptBase)this).Bars.Count < 2)
			{
				return;
			}
			int num = ((NinjaScriptBase)this).Bars.GetBar(rt.FirstEntryTime);
			int num2 = ((NinjaScriptBase)this).Bars.GetBar(rt.LastExitTime);
			if (num < 0)
			{
				num = 0;
			}
			if (num2 >= ((NinjaScriptBase)this).Bars.Count)
			{
				num2 = ((NinjaScriptBase)this).Bars.Count - 1;
			}
			if (num > num2)
			{
				return;
			}
			double avgEntryPrice = rt.AvgEntryPrice;
			int entryQtyTotal = rt.EntryQtyTotal;
			double pointValue = ((NinjaScriptBase)this).Instrument.MasterInstrument.PointValue;
			double num3 = 0.0;
			double num4 = 0.0;
			for (int i = num; i <= num2; i++)
			{
				double valueAt = ((NinjaScriptBase)this).High.GetValueAt(i);
				double valueAt2 = ((NinjaScriptBase)this).Low.GetValueAt(i);
				double val;
				double val2;
				if (rt.IsLong)
				{
					val = (valueAt - avgEntryPrice) * pointValue * (double)entryQtyTotal;
					val2 = (valueAt2 - avgEntryPrice) * pointValue * (double)entryQtyTotal;
				}
				else
				{
					val = (avgEntryPrice - valueAt) * pointValue * (double)entryQtyTotal;
					val2 = (avgEntryPrice - valueAt2) * pointValue * (double)entryQtyTotal;
				}
				if (Math.Max(val, val2) > num4)
				{
					num4 = Math.Max(val, val2);
				}
				if (Math.Min(val, val2) < num3)
				{
					num3 = Math.Min(val, val2);
				}
			}
			rt.MaxFavorableExcursion = num4;
			rt.MaxAdverseExcursion = num3;
			rt.MAEMFECalculated = true;
		}
		catch
		{
		}
	}

	private void UpdateLiveMAEMFE()
	{
		if (!ShowMAEMFE)
		{
			return;
		}
		lock (tradeLock)
		{
			if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName))
				return;

			AccountState value = accountStates[activeAccountName];
			if (value.CurrentRT != null && value.NetPosition != 0)
			{
				RoundTrip currentRT = value.CurrentRT;
				double avgEntryPrice = currentRT.AvgEntryPrice;
				int entryQtyTotal = currentRT.EntryQtyTotal;
				double pointValue = ((NinjaScriptBase)this).Instrument.MasterInstrument.PointValue;
				double val;
				double val2;
				if (currentRT.IsLong)
				{
					val = (((NinjaScriptBase)this).High[0] - avgEntryPrice) * pointValue * (double)entryQtyTotal;
					val2 = (((NinjaScriptBase)this).Low[0] - avgEntryPrice) * pointValue * (double)entryQtyTotal;
				}
				else
				{
					val = (avgEntryPrice - ((NinjaScriptBase)this).High[0]) * pointValue * (double)entryQtyTotal;
					val2 = (avgEntryPrice - ((NinjaScriptBase)this).Low[0]) * pointValue * (double)entryQtyTotal;
				}
				if (Math.Max(val, val2) > currentRT.MaxFavorableExcursion)
				{
					currentRT.MaxFavorableExcursion = Math.Max(val, val2);
				}
				if (Math.Min(val, val2) < currentRT.MaxAdverseExcursion)
				{
					currentRT.MaxAdverseExcursion = Math.Min(val, val2);
				}
			}
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Expected O, but got Unknown
		if (!historyLoaded && ((NinjaScriptBase)this).CurrentBar > 10)
		{
			historyLoaded = true;
			LoadAllHistory();
			shotClockIsLive = true;
		}
		if (DateTime.UtcNow - lastAccountCheck > TimeSpan.FromSeconds(1))
		{
			lastAccountCheck = DateTime.UtcNow;
			string chartTraderAccount = GetChartTraderAccount();
			if (!string.IsNullOrEmpty(chartTraderAccount) && chartTraderAccount != activeAccountName)
			{
				activeAccountName = chartTraderAccount;
				needsRedraw = true;
			}
		}
		UpdateLiveMAEMFE();
		if (needsRedraw)
		{
			needsRedraw = false;
			DrawAllTrades();
		}
		if (!EnableShotClock || !shotClockActive)
		{
			return;
		}
		double totalSeconds = (shotClockEnd - DateTime.UtcNow).TotalSeconds;
		if (totalSeconds <= 0.0)
		{
			shotClockActive = false;
			try
			{
				((IndicatorRenderBase)this).RemoveDrawObject("OrcaShotClock");
				return;
			}
			catch
			{
				return;
			}
		}
		int num = (int)(totalSeconds / 60.0);
		int num2 = (int)(totalSeconds % 60.0);
		string text = $"⏱ Shot Clock  {num}:{num2:D2}";
		Brush textBrush = ((totalSeconds <= 30.0) ? ShotClockWarningColor : ShotClockColor);
		Draw.TextFixed((NinjaScriptBase)(object)this, "OrcaShotClock", text, ShotClockPosition, textBrush, new SimpleFont("Arial", 14)
		{
			Bold = true
		}, Brushes.Transparent, Brushes.Transparent, 0);
	}

	private void ClearOldDrawings()
	{
		try
		{
			foreach (string item in (from d in (IEnumerable<IDrawingTool>)((IndicatorRenderBase)this).DrawObjects
				where d.Tag.StartsWith("OrcaRT_")
				select d.Tag).ToList())
			{
				((IndicatorRenderBase)this).RemoveDrawObject(item);
			}
		}
		catch
		{
		}
	}

	private void DrawAllTrades()
	{
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Expected O, but got Unknown
		if (!ShowExecutionLines)
		{
			return;
		}
		List<RoundTrip> list;
		lock (tradeLock)
		{
			if (!accountStates.ContainsKey(activeAccountName))
			{
				return;
			}
			list = accountStates[activeAccountName].RoundTrips.Where((RoundTrip rt) => rt.IsComplete).ToList();
		}
		if (lastDrawnAccount != activeAccountName)
		{
			ClearOldDrawings();
			lastDrawnAccount = activeAccountName;
		}
		if (list.Count > 0)
		{
			double num = list.Sum((RoundTrip rt) => rt.TotalPnLDollars);
			string text = ((RiskAmount > 0.0) ? (" | " + (num / RiskAmount).ToString("+0.##;-0.##;0") + "R") : "");
			string text2 = "Session: " + Fmt(num) + text + " (" + list.Count + " trades)";
			if (ShowSessionTotal)
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "OrcaRT_SessionTotal", text2, SessionTotalPosition, (num >= 0.0) ? Brushes.Lime : Brushes.Salmon, new SimpleFont("Arial", 13)
				{
					Bold = true
				}, Brushes.Transparent, (num >= 0.0) ? Brushes.DarkGreen : Brushes.DarkRed, 80);
			}
			else
			{
				((IndicatorRenderBase)this).RemoveDrawObject("OrcaRT_SessionTotal");
			}
		}
		foreach (RoundTrip item in list)
		{
			DrawRoundTrip(item);
		}
	}

	private void DrawRoundTrip(RoundTrip rt)
	{
		try
		{
			string text = "OrcaRT_" + rt.Number + "_";
			double avgEntryPrice = rt.AvgEntryPrice;
			double avgExitPrice = rt.AvgExitPrice;
			Brush brush = ((rt.TotalPnLDollars >= 0.0) ? ProfitColor : LossColor);
			if (ShowIndividualLines)
			{
				for (int i = 0; i < rt.Matches.Count; i++)
				{
					FillMatch fillMatch = rt.Matches[i];
					DateTime dateTime = fillMatch.EntryTime;
					DateTime dateTime2 = fillMatch.ExitTime;
					if (((NinjaScriptBase)this).Bars != null && ((NinjaScriptBase)this).Bars.Count > 0)
					{
						int bar = ((NinjaScriptBase)this).Bars.GetBar(dateTime);
						if (bar >= 0 && bar < ((NinjaScriptBase)this).Bars.Count)
						{
							dateTime = ((NinjaScriptBase)this).Bars.GetTime(bar);
						}
						int bar2 = ((NinjaScriptBase)this).Bars.GetBar(dateTime2);
						if (bar2 >= 0 && bar2 < ((NinjaScriptBase)this).Bars.Count)
						{
							dateTime2 = ((NinjaScriptBase)this).Bars.GetTime(bar2);
						}
					}
					Draw.Line((NinjaScriptBase)(object)this, text + "L" + i, isAutoScale: false, dateTime, fillMatch.EntryPrice, dateTime2, fillMatch.ExitPrice, (fillMatch.PnLDollars >= 0.0) ? ProfitColor : LossColor, (DashStyleHelper)0, LineWidth);
				}
				return;
			}
			DateTime dateTime3 = rt.FirstEntryTime;
			DateTime dateTime4 = rt.LastExitTime;
			if (((NinjaScriptBase)this).Bars != null && ((NinjaScriptBase)this).Bars.Count > 0)
			{
				int bar3 = ((NinjaScriptBase)this).Bars.GetBar(dateTime3);
				if (bar3 >= 0 && bar3 < ((NinjaScriptBase)this).Bars.Count)
				{
					dateTime3 = ((NinjaScriptBase)this).Bars.GetTime(bar3);
				}
				int bar4 = ((NinjaScriptBase)this).Bars.GetBar(dateTime4);
				if (bar4 >= 0 && bar4 < ((NinjaScriptBase)this).Bars.Count)
				{
					dateTime4 = ((NinjaScriptBase)this).Bars.GetTime(bar4);
				}
			}
			Draw.Line((NinjaScriptBase)(object)this, text + "L", isAutoScale: false, dateTime3, avgEntryPrice, dateTime4, avgExitPrice, brush, (DashStyleHelper)0, LineWidth);
		}
		catch (Exception ex)
		{
			((NinjaScript)this).Print((object)("OrcaExecLines DrawRT error: " + ex.Message));
		}
	}

	private string Fmt(double d)
	{
		return ((d >= 0.0) ? "+$" : "-$") + Math.Abs(d).ToString("N2");
	}

	private void OnChartMouseMove(object sender, MouseEventArgs e)
	{
		mousePosition = e.GetPosition((IInputElement)((IndicatorRenderBase)this).ChartControl);
		isMouseOverChart = true;
		ChartControl chartControl = ((IndicatorRenderBase)this).ChartControl;
		if (chartControl != null)
		{
			chartControl.InvalidateVisual();
		}
	}

	private void OnChartMouseLeave(object sender, MouseEventArgs e)
	{
		isMouseOverChart = false;
		mousePosition = new Point(-1.0, -1.0);
		ChartControl chartControl = ((IndicatorRenderBase)this).ChartControl;
		if (chartControl != null)
		{
			chartControl.InvalidateVisual();
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (!ShowExecutionLines || ((IndicatorRenderBase)this).RenderTarget == null || ((IndicatorRenderBase)this).ChartBars == null || ((NinjaScriptBase)this).Bars == null)
		{
			return;
		}
		List<RoundTrip> list;
		lock (tradeLock)
		{
			if (string.IsNullOrEmpty(activeAccountName) || !accountStates.ContainsKey(activeAccountName))
			{
				return;
			}
			list = accountStates[activeAccountName].RoundTrips.Where((RoundTrip rt) => rt.IsComplete).ToList();
		}
		if (list.Count == 0)
		{
			return;
		}
		RoundTrip roundTrip = null;
		FillMatch fillMatch = null;
		if (ShowLabels && isMouseOverChart)
		{
			float num = (float)mousePosition.X;
			float num2 = (float)mousePosition.Y;
			if (((IndicatorRenderBase)this).ChartPanel != null)
			{
				num2 -= (float)((IndicatorRenderBase)this).ChartPanel.Y;
				num -= (float)((IndicatorRenderBase)this).ChartPanel.X;
			}
			double num3 = 625.0;
			foreach (RoundTrip item in list)
			{
				float x3;
				float y3;
				float x4;
				float y4;
				if (HoverShowsIndividualPnL)
				{
					foreach (FillMatch match in item.Matches)
					{
						if (TryGetXY(match.EntryTime, match.EntryPrice, chartControl, chartScale, out var x, out var y) && TryGetXY(match.ExitTime, match.ExitPrice, chartControl, chartScale, out var x2, out var y2))
						{
							double num4 = DistToSegmentSquared(num, num2, x, y, x2, y2);
							if (num4 < num3)
							{
								num3 = num4;
								roundTrip = item;
								fillMatch = match;
							}
						}
					}
				}
				else if (TryGetXY(item.FirstEntryTime, item.AvgEntryPrice, chartControl, chartScale, out x3, out y3) && TryGetXY(item.LastExitTime, item.AvgExitPrice, chartControl, chartScale, out x4, out y4))
				{
					double num5 = DistToSegmentSquared(num, num2, x3, y3, x4, y4);
					if (num5 < num3)
					{
						num3 = num5;
						roundTrip = item;
						fillMatch = null;
					}
				}
			}
		}

		SolidColorBrush longMarkerBrush = ToD2DBrush(LongMarkerColor);
		SolidColorBrush longMarkerBrushAlpha = ToD2DBrush(LongMarkerColor, 0.65f);
		SolidColorBrush shortMarkerBrush = ToD2DBrush(ShortMarkerColor);
		SolidColorBrush shortMarkerBrushAlpha = ToD2DBrush(ShortMarkerColor, 0.65f);

		try
		{
			foreach (RoundTrip item2 in list)
			{
				SolidColorBrush markerBrush = (item2.IsLong ? longMarkerBrush : shortMarkerBrush);
				SolidColorBrush markerBrushAlpha = (item2.IsLong ? longMarkerBrushAlpha : shortMarkerBrushAlpha);

				if (ShowMarkers && TryGetXY(item2.FirstEntryTime, item2.AvgEntryPrice, chartControl, chartScale, out var x5, out var y5) && TryGetXY(item2.LastExitTime, item2.AvgExitPrice, chartControl, chartScale, out var x6, out var y6))
				{
					DrawTriangle(item2.IsLong, x5, y5, 8f, markerBrush);
					DrawTriangle(!item2.IsLong, x6, y6, 8f, markerBrush);
				}

				if (!ShowIndividualMarkers || (item2.Matches.Count <= 1 && ShowMarkers))
				{
					continue;
				}

				foreach (FillMatch match2 in item2.Matches)
				{
					if (TryGetXY(match2.EntryTime, match2.EntryPrice, chartControl, chartScale, out var x7, out var y7))
					{
						DrawTriangle(item2.IsLong, x7, y7, 4.4f, markerBrushAlpha);
					}
					if (TryGetXY(match2.ExitTime, match2.ExitPrice, chartControl, chartScale, out var x8, out var y8))
					{
						DrawTriangle(!item2.IsLong, x8, y8, 4.4f, markerBrushAlpha);
					}
				}
			}
		}
		finally
		{
			longMarkerBrush?.Dispose();
			longMarkerBrushAlpha?.Dispose();
			shortMarkerBrush?.Dispose();
			shortMarkerBrushAlpha?.Dispose();
		}
		if (roundTrip == null)
		{
			return;
		}
		float x9;
		float y9;
		if (HoverShowsIndividualPnL && fillMatch != null)
		{
			if (TryGetXY(fillMatch.ExitTime, fillMatch.ExitPrice, chartControl, chartScale, out x9, out y9))
			{
				DrawHoverLabel(roundTrip, fillMatch, x9, y9);
			}
		}
		else if (TryGetXY(roundTrip.LastExitTime, roundTrip.AvgExitPrice, chartControl, chartScale, out x9, out y9))
		{
			DrawHoverLabel(roundTrip, null, x9, y9);
		}
	}

	private bool TryGetXY(DateTime time, double price, ChartControl cc, ChartScale cs, out float x, out float y)
	{
		x = (y = 0f);
		try
		{
			int bar = ((NinjaScriptBase)this).Bars.GetBar(time);
			if (bar < 0 || bar >= ((NinjaScriptBase)this).Bars.Count)
			{
				return false;
			}
			x = cc.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, bar);
			y = cs.GetYByValue(price);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void DrawTriangle(bool pointUp, float cx, float cy, float size, SolidColorBrush brush)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			PathGeometry val = new PathGeometry(Globals.D2DFactory);
			GeometrySink val2 = val.Open();
			((SimplifiedGeometrySink)val2).SetFillMode((FillMode)1);
			if (pointUp)
			{
				((SimplifiedGeometrySink)val2).BeginFigure(new Vector2(cx, cy - size * 1.4f), (FigureBegin)0);
				((SimplifiedGeometrySink)val2).AddLines((Vector2[])(object)new Vector2[2]
				{
					new Vector2(cx + size, cy + size * 0.7f),
					new Vector2(cx - size, cy + size * 0.7f)
				});
			}
			else
			{
				((SimplifiedGeometrySink)val2).BeginFigure(new Vector2(cx, cy + size * 1.4f), (FigureBegin)0);
				((SimplifiedGeometrySink)val2).AddLines((Vector2[])(object)new Vector2[2]
				{
					new Vector2(cx + size, cy - size * 0.7f),
					new Vector2(cx - size, cy - size * 0.7f)
				});
			}
			((SimplifiedGeometrySink)val2).EndFigure((FigureEnd)1);
			((SimplifiedGeometrySink)val2).Close();
			((IndicatorRenderBase)this).RenderTarget.FillGeometry((Geometry)(object)val, (Brush)(object)brush);
			((DisposeBase)val).Dispose();
			((IDisposable)val2).Dispose();
		}
		catch
		{
		}
	}

	private void DrawHoverLabel(RoundTrip rt, FillMatch match, float exitX, float exitY)
	{
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Expected O, but got Unknown
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Expected O, but got Unknown
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Expected O, but got Unknown
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Expected O, but got Unknown
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			bool flag = match != null;
			string text = (rt.IsLong ? "Long" : "Short");
			int num = (flag ? match.Quantity : rt.EntryQtyTotal);
			string text2 = ((num > 1) ? (" x" + num) : "");
			double num2 = (flag ? match.PnLTicks : ((rt.EntryQtyTotal > 0) ? (rt.TotalPnLTicks / (double)rt.EntryQtyTotal) : 0.0));
			double num3 = (flag ? match.PnLDollars : rt.TotalPnLDollars);
			string text3 = "#" + rt.Number + (flag ? " (Fill)" : "") + " " + text + text2 + "\n" + num2.ToString("+0.##;-0.##;0") + " ticks | " + Fmt(num3);
			if (RiskAmount > 0.0)
			{
				text3 = text3 + " | " + (num3 / RiskAmount).ToString("+0.##;-0.##;0") + "R";
			}
			if (!flag && ShowMAEMFE && rt.MAEMFECalculated)
			{
				text3 = text3 + "\n" + ((num3 >= 0.0) ? ("MDD: " + Fmt(rt.MaxAdverseExcursion)) : ("Peak: " + Fmt(rt.MaxFavorableExcursion)));
			}
			bool num4 = num3 >= 0.0;
			Color4 val = (num4 ? new Color4(0f, 1f, 0f, 1f) : new Color4(1f, 0.5f, 0.5f, 1f));
			Color4 val2 = (num4 ? new Color4(0f, 0.18f, 0f, 0.88f) : new Color4(0.32f, 0f, 0f, 0.88f));
			TextFormat val3 = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", (FontWeight)700, (FontStyle)0, (float)LabelFontSize);
			try
			{
				TextLayout val4 = new TextLayout(Globals.DirectWriteFactory, text3, val3, 320f, 120f);
				try
				{
					TextMetrics metrics = val4.Metrics;
					float num5 = exitX + 10f;
					float num6 = exitY - metrics.Height / 2f;
					if (num5 + metrics.Width + 14f > ((IndicatorRenderBase)this).RenderTarget.Size.Width)
					{
						num5 = exitX - metrics.Width - 14f - 10f;
					}
					if (num6 < 2f)
					{
						num6 = 2f;
					}
					SolidColorBrush val5 = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, val2);
					try
					{
						SolidColorBrush val6 = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, val);
						try
						{
							((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num5 - 7f, num6 - 7f, metrics.Width + 14f, metrics.Height + 14f), (Brush)(object)val5);
							((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(new Vector2(num5, num6), val4, (Brush)(object)val6);
						}
						finally
						{
							((IDisposable)val6)?.Dispose();
						}
					}
					finally
					{
						((IDisposable)val5)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val4)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val3)?.Dispose();
			}
		}
		catch
		{
		}
	}

	private SolidColorBrush ToD2DBrush(Brush wpfBrush, float alpha = 1f)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		if (wpfBrush is SolidColorBrush { Color: var color })
		{
			return new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, alpha));
		}
		return new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4(1f, 1f, 0f, alpha));
	}

	private static double DistToSegmentSquared(float px, float py, float x1, float y1, float x2, float y2)
	{
		float num = (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
		if (num == 0f)
		{
			return Dist2(px, py, x1, y1);
		}
		float val = ((px - x1) * (x2 - x1) + (py - y1) * (y2 - y1)) / num;
		val = Math.Max(0f, Math.Min(1f, val));
		return Dist2(px, py, x1 + val * (x2 - x1), y1 + val * (y2 - y1));
	}

	private static double Dist2(float ax, float ay, float bx, float by)
	{
		return (double)(ax - bx) * (double)(ax - bx) + (double)(ay - by) * (double)(ay - by);
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
