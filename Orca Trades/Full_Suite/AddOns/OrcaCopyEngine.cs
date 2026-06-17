#region Using declarations
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	public enum OrcaCopyMethod
	{
		ExactQuantity,
		Multiplier,
		FixedQuantity
	}

	public enum OrcaFollowerGuardStatus
	{
		Off,
		Active,
		Warning,
		Disarmed
	}

	[Serializable]
	public sealed class OrcaCopyFollowerSetting
	{
		public string AccountName { get; set; }
		public bool Enabled { get; set; }
		public bool AtmCopy { get; set; }
	}

	[Serializable]
	public sealed class OrcaCopySettings
	{
		public string LeaderAccountName { get; set; }
		public OrcaCopyMethod CopyMethod { get; set; }
		public double Multiplier { get; set; }
		public int FixedQuantity { get; set; }
		public int MaxSlippageTicks { get; set; }
		public int HardSlippageTicks { get; set; }
		public int WarningLatencyMs { get; set; }
		public OrcaCopyNetworkMode NetworkMode { get; set; }
		public string NetworkHost { get; set; }
		public int NetworkPort { get; set; }
		public List<OrcaCopyFollowerSetting> Followers { get; set; }

		public OrcaCopySettings()
		{
			CopyMethod = OrcaCopyMethod.ExactQuantity;
			Multiplier = 1;
			FixedQuantity = 1;
			MaxSlippageTicks = 8;
			HardSlippageTicks = 0;
			WarningLatencyMs = 750;
			NetworkMode = OrcaCopyNetworkMode.Off;
			NetworkHost = "127.0.0.1";
			NetworkPort = 7057;
			Followers = new List<OrcaCopyFollowerSetting>();
		}

		public OrcaCopySettings Clone()
		{
			return new OrcaCopySettings {
				LeaderAccountName = LeaderAccountName,
				CopyMethod = CopyMethod,
				Multiplier = Multiplier,
				FixedQuantity = FixedQuantity,
				MaxSlippageTicks = MaxSlippageTicks,
				HardSlippageTicks = HardSlippageTicks,
				WarningLatencyMs = WarningLatencyMs,
				NetworkMode = NetworkMode,
				NetworkHost = NetworkHost,
				NetworkPort = NetworkPort,
				Followers = Followers == null
					? new List<OrcaCopyFollowerSetting>()
					: Followers.Select(f => new OrcaCopyFollowerSetting { AccountName = f.AccountName, Enabled = f.Enabled, AtmCopy = f.AtmCopy }).ToList()
			};
		}
	}

	public static class OrcaCopySettingsStore
	{
		private static readonly object Sync = new object();

		public static OrcaCopySettings Load()
		{
			lock (Sync) {
				try {
					if (File.Exists(SettingsPath)) {
						using (FileStream stream = File.OpenRead(SettingsPath)) {
							OrcaCopySettings settings = (OrcaCopySettings)new XmlSerializer(typeof(OrcaCopySettings)).Deserialize(stream);
							return Normalize(settings);
						}
					}
				} catch (Exception ex) {
					OrcaCopyDiagnostics.Print("OrcaTradeCopier settings load failed: " + ex.Message, LogLevel.Warning);
				}
				return Normalize(new OrcaCopySettings());
			}
		}

		public static void Save(OrcaCopySettings settings)
		{
			lock (Sync) {
				try {
					OrcaCopySettings normalized = Normalize(settings);
					string directory = Path.GetDirectoryName(SettingsPath);
					if (!Directory.Exists(directory))
						Directory.CreateDirectory(directory);
					using (FileStream stream = File.Create(SettingsPath))
						new XmlSerializer(typeof(OrcaCopySettings)).Serialize(stream, normalized);
				} catch (Exception ex) {
					OrcaCopyDiagnostics.Print("OrcaTradeCopier settings save failed: " + ex.Message, LogLevel.Warning);
				}
			}
		}

		private static string SettingsPath
		{
			get {
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					"NinjaTrader 8",
					"OrcaTradeCopier.xml");
			}
		}

		private static OrcaCopySettings Normalize(OrcaCopySettings input)
		{
			OrcaCopySettings output = input == null ? new OrcaCopySettings() : input.Clone();
			if (output.Multiplier <= 0 || double.IsNaN(output.Multiplier) || double.IsInfinity(output.Multiplier))
				output.Multiplier = 1;
			output.FixedQuantity = Math.Max(1, output.FixedQuantity);
			output.MaxSlippageTicks = Math.Max(0, output.MaxSlippageTicks);
			output.HardSlippageTicks = Math.Max(0, output.HardSlippageTicks);
			output.WarningLatencyMs = Math.Max(0, output.WarningLatencyMs);
			output.NetworkPort = Math.Max(1, Math.Min(65535, output.NetworkPort <= 0 ? 7057 : output.NetworkPort));
			if (string.IsNullOrWhiteSpace(output.NetworkHost))
				output.NetworkHost = "127.0.0.1";
			if (output.Followers == null)
				output.Followers = new List<OrcaCopyFollowerSetting>();
			return output;
		}
	}

	public sealed class OrcaFollowerAccountState : INotifyPropertyChanged
	{
		private readonly object sync = new object();
		private bool enabled;
		private bool atmCopy;
		private bool isDisarmed;
		private OrcaFollowerGuardStatus status;
		private int latencyMs;
		private double averageSlippageTicks;
		private int fillCount;
		private string guardMessage;

		public OrcaFollowerAccountState(Account account)
		{
			Account = account;
			AccountName = account == null ? string.Empty : account.Name;
			DisplayName = account == null ? string.Empty : (string.IsNullOrWhiteSpace(account.DisplayName) ? account.Name : account.DisplayName);
			ConnectionName = GetConnectionName(account);
			AtmCopy = true;
			Status = OrcaFollowerGuardStatus.Off;
			GuardMessage = "Off";
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[XmlIgnore]
		public Account Account { get; private set; }

		public string AccountName { get; private set; }
		public string DisplayName { get; private set; }
		public string ConnectionName { get; private set; }

		public bool Enabled
		{
			get { return enabled; }
			set {
				if (enabled == value)
					return;
				enabled = value;
				if (enabled && !isDisarmed)
					SetStatus(OrcaFollowerGuardStatus.Active, "Armed");
				if (!enabled && !isDisarmed)
					SetStatus(OrcaFollowerGuardStatus.Off, "Off");
				OnPropertyChanged("Enabled");
			}
		}

		public bool AtmCopy
		{
			get { return atmCopy; }
			set {
				if (atmCopy == value)
					return;
				atmCopy = value;
				OnPropertyChanged("AtmCopy");
			}
		}

		public bool IsDisarmed
		{
			get { return isDisarmed; }
			private set {
				if (isDisarmed == value)
					return;
				isDisarmed = value;
				OnPropertyChanged("IsDisarmed");
			}
		}

		public OrcaFollowerGuardStatus Status
		{
			get { return status; }
			private set {
				if (status == value)
					return;
				status = value;
				OnPropertyChanged("Status");
			}
		}

		public int LatencyMs
		{
			get { return latencyMs; }
			private set {
				if (latencyMs == value)
					return;
				latencyMs = value;
				OnPropertyChanged("LatencyMs");
			}
		}

		public double AverageSlippageTicks
		{
			get { return averageSlippageTicks; }
			private set {
				if (Math.Abs(averageSlippageTicks - value) < 0.0001)
					return;
				averageSlippageTicks = value;
				OnPropertyChanged("AverageSlippageTicks");
			}
		}

		public int FillCount
		{
			get { return fillCount; }
			private set {
				if (fillCount == value)
					return;
				fillCount = value;
				OnPropertyChanged("FillCount");
			}
		}

		public string GuardMessage
		{
			get { return guardMessage; }
			private set {
				if (string.Equals(guardMessage, value, StringComparison.Ordinal))
					return;
				guardMessage = value;
				OnPropertyChanged("GuardMessage");
			}
		}

		public void ApplyFollowerSetting(OrcaCopyFollowerSetting setting)
		{
			if (setting == null)
				return;
			AtmCopy = setting.AtmCopy;
			Enabled = setting.Enabled;
		}

		public OrcaCopyFollowerSetting ToSetting()
		{
			return new OrcaCopyFollowerSetting {
				AccountName = AccountName,
				Enabled = Enabled,
				AtmCopy = AtmCopy
			};
		}

		public void MarkArmed()
		{
			IsDisarmed = false;
			if (Enabled)
				SetStatus(OrcaFollowerGuardStatus.Active, "Armed");
		}

		public void MarkStopped()
		{
			if (!IsDisarmed)
				SetStatus(Enabled ? OrcaFollowerGuardStatus.Off : OrcaFollowerGuardStatus.Off, "Off");
		}

		public void MarkWarning(string message)
		{
			if (!IsDisarmed && Enabled)
				SetStatus(OrcaFollowerGuardStatus.Warning, message);
		}

		public void Disarm(string message)
		{
			IsDisarmed = true;
			enabled = false;
			OnPropertyChanged("Enabled");
			SetStatus(OrcaFollowerGuardStatus.Disarmed, message);
		}

		public void Rearm()
		{
			IsDisarmed = false;
			Enabled = true;
			SetStatus(OrcaFollowerGuardStatus.Active, "Rearmed");
		}

		public void AddFillMetric(int latency, double slippageTicks, int warningLatencyMs, int warningSlippageTicks)
		{
			lock (sync) {
				FillCount = FillCount + 1;
				LatencyMs = Math.Max(0, latency);
				AverageSlippageTicks = ((AverageSlippageTicks * (FillCount - 1)) + slippageTicks) / FillCount;
			}

			if (!IsDisarmed && warningSlippageTicks > 0 && slippageTicks > warningSlippageTicks)
				MarkWarning("Slip " + slippageTicks.ToString("0.##") + "t > warn " + warningSlippageTicks + "t");
			else if (!IsDisarmed && warningLatencyMs > 0 && LatencyMs > warningLatencyMs)
				MarkWarning("High latency: " + LatencyMs + " ms");
			else if (!IsDisarmed && Enabled)
				SetStatus(OrcaFollowerGuardStatus.Active, "Synced");
		}

		private void SetStatus(OrcaFollowerGuardStatus newStatus, string message)
		{
			Status = newStatus;
			GuardMessage = string.IsNullOrWhiteSpace(message) ? newStatus.ToString() : message;
		}

		private static string GetConnectionName(Account account)
		{
			try {
				if (account != null && account.Connection != null && account.Connection.Options != null)
					return account.Connection.Options.Name;
			} catch { }
			return string.Empty;
		}

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public sealed class OrcaCopyLogRecord : EventArgs
	{
		public DateTime TimeUtc { get; set; }
		public string EventType { get; set; }
		public string LeaderAccount { get; set; }
		public string FollowerAccount { get; set; }
		public string Instrument { get; set; }
		public string OrderAction { get; set; }
		public string OrderType { get; set; }
		public int Quantity { get; set; }
		public double LeaderPrice { get; set; }
		public double FollowerPrice { get; set; }
		public int LatencyMs { get; set; }
		public double SlippageTicks { get; set; }
		public string Message { get; set; }
	}

	public interface IOrcaCopyTradeLogger
	{
		void Write(OrcaCopyLogRecord record);
	}

	public static class OrcaCopyTradeLogger
	{
		private static IOrcaCopyTradeLogger sink;

		public static event EventHandler<OrcaCopyLogRecord> RecordReady;

		public static void SetSink(IOrcaCopyTradeLogger logger)
		{
			sink = logger;
		}

		public static void Write(OrcaCopyLogRecord record)
		{
			if (record == null)
				return;

			if (record.TimeUtc == DateTime.MinValue)
				record.TimeUtc = DateTime.UtcNow;

			IOrcaCopyTradeLogger currentSink = sink;
			if (currentSink != null)
				currentSink.Write(record);

			EventHandler<OrcaCopyLogRecord> handler = RecordReady;
			if (handler != null)
				handler(null, record);
		}
	}

	public static class OrcaCopyDiagnostics
	{
		public static void Print(string message)
		{
			Print(message, LogLevel.Information);
		}

		public static void Print(string message, LogLevel level)
		{
			try {
				Log.Process(typeof(OrcaCopyDiagnostics), message ?? string.Empty, null, level, LogCategories.NinjaScript);
			} catch { }
			try {
				System.Diagnostics.Trace.WriteLine("[OrcaTradeCopier] " + message);
			} catch { }
		}
	}

	internal sealed class OrcaCopiedOrderState
	{
		public string LeaderOrderKey;
		public string FollowerAccountName;
		public string FollowerOrderKey;
		public string LeaderOco;
		public Order FollowerOrder;
		public Instrument Instrument;
		public OrderAction LeaderAction;
		public OrderType OrderType;
		public int Quantity;
		public bool IsProtective;
		public DateTime LeaderSubmitUtc;
		public DateTime LeaderFillUtc;
		public double LeaderFillPrice;
		public int LeaderFillQuantity;
	}

	internal sealed class OrcaLeaderFillSnapshot
	{
		public string LeaderOrderKey;
		public string LeaderAccountName;
		public Instrument Instrument;
		public OrderAction Action;
		public DateTime FillUtc;
		public double Price;
		public int Quantity;
	}

	public sealed class OrcaCopyEngine : IDisposable
	{
		private readonly ConcurrentDictionary<string, OrcaCopiedOrderState> copiedByFollowerKey = new ConcurrentDictionary<string, OrcaCopiedOrderState>();
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrcaCopiedOrderState>> copiedByLeaderKey = new ConcurrentDictionary<string, ConcurrentDictionary<string, OrcaCopiedOrderState>>();
		private readonly ConcurrentDictionary<string, OrcaLeaderFillSnapshot> leaderFills = new ConcurrentDictionary<string, OrcaLeaderFillSnapshot>();
		private readonly ConcurrentDictionary<string, string> followerOcoByLeaderOco = new ConcurrentDictionary<string, string>();
		private readonly HashSet<string> hookedFollowerAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private readonly object sync = new object();
		private readonly Dispatcher dispatcher;
		private readonly OrcaCopyNetwork network;
		private Account leaderAccount;
		private Dictionary<string, OrcaFollowerAccountState> followersByName = new Dictionary<string, OrcaFollowerAccountState>(StringComparer.OrdinalIgnoreCase);
		private OrcaCopySettings settings = new OrcaCopySettings();
		private volatile bool isRunning;

		public OrcaCopyEngine(Dispatcher dispatcher)
		{
			this.dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
			network = new OrcaCopyNetwork();
			network.MessageReceived += OnNetworkMessageReceived;
			network.StatusChanged += OnNetworkStatusChanged;
		}

		public event EventHandler<string> StatusChanged;

		public bool IsRunning
		{
			get { return isRunning; }
		}

		public void Start(OrcaCopySettings newSettings, IEnumerable<OrcaFollowerAccountState> followerStates)
		{
			lock (sync) {
				StopInternal(false);
				settings = (newSettings ?? new OrcaCopySettings()).Clone();
				ApplyFollowersNoLock(followerStates);
				copiedByFollowerKey.Clear();
				copiedByLeaderKey.Clear();
				leaderFills.Clear();
				followerOcoByLeaderOco.Clear();

				if (settings.NetworkMode != OrcaCopyNetworkMode.RemoteFollower) {
					leaderAccount = ResolveAccount(settings.LeaderAccountName);
					if (leaderAccount == null)
						throw new InvalidOperationException("Select a leader account before arming OrcaTradeCopier.");

					leaderAccount.OrderUpdate += OnLeaderOrderUpdate;
					leaderAccount.ExecutionUpdate += OnLeaderExecutionUpdate;
				}

				foreach (OrcaFollowerAccountState follower in followersByName.Values) {
					if (follower.Enabled && !follower.IsDisarmed)
						HookFollowerNoLock(follower);
					RunOnUi(() => follower.MarkArmed());
				}

				if (settings.NetworkMode == OrcaCopyNetworkMode.LeaderServer)
					network.StartServer(settings.NetworkPort);
				else if (settings.NetworkMode == OrcaCopyNetworkMode.RemoteFollower)
					network.ConnectClient(settings.NetworkHost, settings.NetworkPort);

				isRunning = true;
				RaiseStatus("Armed");
				OrcaCopyDiagnostics.Print("OrcaTradeCopier armed");
			}
		}

		public void Stop()
		{
			lock (sync)
				StopInternal(true);
		}

		public void RefreshFollowers(IEnumerable<OrcaFollowerAccountState> followerStates)
		{
			lock (sync) {
				ApplyFollowersNoLock(followerStates);
				if (!isRunning)
					return;

				foreach (OrcaFollowerAccountState follower in followersByName.Values) {
					if (follower.Enabled && !follower.IsDisarmed)
						HookFollowerNoLock(follower);
					else
						UnhookFollowerNoLock(follower.Account);
				}
			}
		}

		public void RearmFollower(OrcaFollowerAccountState follower)
		{
			if (follower == null)
				return;

			RunOnUi(() => follower.Rearm());
			lock (sync) {
				if (isRunning)
					HookFollowerNoLock(follower);
			}
		}

		public void RearmAll()
		{
			foreach (OrcaFollowerAccountState follower in followersByName.Values.ToArray())
				RearmFollower(follower);
			RaiseStatus("Followers rearmed");
		}

		public void FlattenAll()
		{
			foreach (OrcaFollowerAccountState follower in followersByName.Values.ToArray())
				FlattenFollower(follower, "Manual flatten all");
		}

		public void FlattenFollower(OrcaFollowerAccountState follower, string reason)
		{
			if (follower == null || follower.Account == null)
				return;
			FlattenAccount(follower.Account, reason, false);
		}

		public void Dispose()
		{
			network.MessageReceived -= OnNetworkMessageReceived;
			network.StatusChanged -= OnNetworkStatusChanged;
			Stop();
			network.Dispose();
		}

		private void StopInternal(bool markFollowersStopped)
		{
			isRunning = false;
			try {
				if (leaderAccount != null) {
					leaderAccount.OrderUpdate -= OnLeaderOrderUpdate;
					leaderAccount.ExecutionUpdate -= OnLeaderExecutionUpdate;
				}
			} catch { }
			leaderAccount = null;

			foreach (string accountName in hookedFollowerAccounts.ToArray()) {
				Account account = ResolveAccount(accountName);
				UnhookFollowerNoLock(account);
			}
			hookedFollowerAccounts.Clear();
			network.Stop();

			if (markFollowersStopped) {
				foreach (OrcaFollowerAccountState follower in followersByName.Values.ToArray())
					RunOnUi(() => follower.MarkStopped());
				RaiseStatus("Stopped");
			}
		}

		private void ApplyFollowersNoLock(IEnumerable<OrcaFollowerAccountState> followerStates)
		{
			followersByName = (followerStates ?? new OrcaFollowerAccountState[0])
				.Where(f => f != null && !string.IsNullOrWhiteSpace(f.AccountName))
				.GroupBy(f => f.AccountName, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
		}

		private void HookFollowerNoLock(OrcaFollowerAccountState follower)
		{
			if (follower == null || follower.Account == null || hookedFollowerAccounts.Contains(follower.AccountName))
				return;
			follower.Account.OrderUpdate += OnFollowerOrderUpdate;
			follower.Account.ExecutionUpdate += OnFollowerExecutionUpdate;
			hookedFollowerAccounts.Add(follower.AccountName);
		}

		private void UnhookFollowerNoLock(Account account)
		{
			if (account == null)
				return;
			try {
				account.OrderUpdate -= OnFollowerOrderUpdate;
				account.ExecutionUpdate -= OnFollowerExecutionUpdate;
			} catch { }
			hookedFollowerAccounts.Remove(account.Name);
		}

		private void OnLeaderOrderUpdate(object sender, OrderEventArgs e)
		{
			if (!isRunning || e == null || e.Order == null)
				return;

			try {
				Order order = e.Order;
				string leaderKey = GetLeaderOrderKey(order, null);
				bool protective = IsProtectiveOrder(order);
				BroadcastOrder(order, e, leaderKey, protective);
				ReplicateOrder(order, e.OrderState, e.Error, leaderKey, protective, false);
			} catch (Exception ex) {
				OrcaCopyDiagnostics.Print("Leader order update failed: " + ex.Message, LogLevel.Error);
			}
		}

		private void OnLeaderExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			if (!isRunning || e == null || e.Execution == null)
				return;

			try {
				Execution execution = e.Execution;
				string leaderKey = GetLeaderOrderKey(execution.Order, e.OrderId);
				OrcaLeaderFillSnapshot fill = new OrcaLeaderFillSnapshot {
					LeaderOrderKey = leaderKey,
					LeaderAccountName = execution.Account == null ? settings.LeaderAccountName : execution.Account.Name,
					Instrument = execution.Instrument,
					Action = execution.Order == null ? InferActionFromMarketPosition(e.MarketPosition) : execution.Order.OrderAction,
					FillUtc = ToUtc(e.Time),
					Price = e.Price,
					Quantity = e.Quantity
				};
				leaderFills[leaderKey] = fill;
				UpdateCopiedLeaderFill(leaderKey, fill);
				BroadcastExecution(fill);
				WriteLog("LeaderFill", fill.LeaderAccountName, null, fill.Instrument, fill.Action.ToString(), null, fill.Quantity, fill.Price, 0, 0, 0, "Leader filled");
			} catch (Exception ex) {
				OrcaCopyDiagnostics.Print("Leader execution update failed: " + ex.Message, LogLevel.Error);
			}
		}

		private void OnFollowerOrderUpdate(object sender, OrderEventArgs e)
		{
			if (!isRunning || e == null || e.Order == null)
				return;

			try {
				OrcaCopiedOrderState copied = FindCopiedFollowerOrder(e.Order);
				if (copied == null)
					return;

				RegisterFollowerOrderKeys(e.Order, copied);
				if (e.Error != ErrorCode.NoError || e.OrderState == OrderState.Rejected) {
					OrcaFollowerAccountState follower = GetFollowerState(e.Order.Account);
					string message = "Follower order rejected: " + e.Error + " " + e.Comment;
					TriggerFollowerGuard(follower, message, e.Order.Instrument);
				}
			} catch (Exception ex) {
				OrcaCopyDiagnostics.Print("Follower order update failed: " + ex.Message, LogLevel.Error);
			}
		}

		private void OnFollowerExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			if (!isRunning || e == null || e.Execution == null)
				return;

			try {
				Execution execution = e.Execution;
				OrcaCopiedOrderState copied = FindCopiedFollowerOrder(execution.Order);
				if (copied == null)
					return;

				RegisterFollowerOrderKeys(execution.Order, copied);
				OrcaLeaderFillSnapshot fill;
				if (!leaderFills.TryGetValue(copied.LeaderOrderKey, out fill)) {
					if (copied.LeaderFillUtc != DateTime.MinValue)
						fill = new OrcaLeaderFillSnapshot {
							LeaderOrderKey = copied.LeaderOrderKey,
							LeaderAccountName = settings.LeaderAccountName,
							Instrument = copied.Instrument,
							Action = copied.LeaderAction,
							FillUtc = copied.LeaderFillUtc,
							Price = copied.LeaderFillPrice,
							Quantity = copied.LeaderFillQuantity
						};
					else
						return;
				}

				OrcaFollowerAccountState follower = GetFollowerState(execution.Account);
				if (follower == null)
					return;

				int latency = (int)Math.Max(0, (ToUtc(e.Time) - fill.FillUtc).TotalMilliseconds);
				double slippageTicks = CalculateSlippageTicks(fill, e.Price);
				RunOnUi(() => follower.AddFillMetric(latency, slippageTicks, settings.WarningLatencyMs, settings.MaxSlippageTicks));
				WriteLog("FollowerFill", fill.LeaderAccountName, follower.AccountName, fill.Instrument, fill.Action.ToString(), copied.OrderType.ToString(), e.Quantity, fill.Price, e.Price, latency, slippageTicks, "Follower filled");

				if (settings.MaxSlippageTicks > 0 && slippageTicks > settings.MaxSlippageTicks)
					WriteLog("SlippageWarning", fill.LeaderAccountName, follower.AccountName, fill.Instrument, fill.Action.ToString(), copied.OrderType.ToString(), e.Quantity, fill.Price, e.Price, latency, slippageTicks, "Slippage warning");

				if (settings.HardSlippageTicks > 0 && slippageTicks > settings.HardSlippageTicks)
					TriggerFollowerGuard(follower, "Hard slippage guard: " + slippageTicks.ToString("0.##") + " ticks > " + settings.HardSlippageTicks + " ticks", execution.Instrument);
			} catch (Exception ex) {
				OrcaCopyDiagnostics.Print("Follower execution update failed: " + ex.Message, LogLevel.Error);
			}
		}

		private void OnNetworkMessageReceived(object sender, OrcaCopyNetworkEventArgs e)
		{
			if (!isRunning || e == null || e.Message == null || settings.NetworkMode != OrcaCopyNetworkMode.RemoteFollower)
				return;

			try {
				OrcaCopyNetworkMessage message = e.Message;
				if (string.Equals(message.MessageType, "ExecutionUpdate", StringComparison.OrdinalIgnoreCase))
					ApplyRemoteLeaderExecution(message);
				else if (string.Equals(message.MessageType, "OrderUpdate", StringComparison.OrdinalIgnoreCase))
					ApplyRemoteLeaderOrder(message);
			} catch (Exception ex) {
				OrcaCopyDiagnostics.Print("Remote network message failed: " + ex.Message, LogLevel.Error);
			}
		}

		private void OnNetworkStatusChanged(object sender, OrcaCopyNetworkStatusEventArgs e)
		{
			if (e != null)
				RaiseStatus(e.Status);
		}

		private void ApplyRemoteLeaderOrder(OrcaCopyNetworkMessage message)
		{
			Instrument instrument = ResolveInstrument(message.InstrumentFullName);
			if (instrument == null)
				return;

			OrderState state = ParseEnum(message.OrderState, OrderState.Unknown);
			ErrorCode error = ErrorCode.NoError;
			ReplicateOrderMessage(message, instrument, state, error);
		}

		private void ApplyRemoteLeaderExecution(OrcaCopyNetworkMessage message)
		{
			Instrument instrument = ResolveInstrument(message.InstrumentFullName);
			if (instrument == null)
				return;

			OrderAction action = ParseEnum(message.OrderAction, OrderAction.Buy);
			OrcaLeaderFillSnapshot fill = new OrcaLeaderFillSnapshot {
				LeaderOrderKey = message.LeaderOrderKey,
				LeaderAccountName = message.LeaderAccountName,
				Instrument = instrument,
				Action = action,
				FillUtc = message.TimestampUtcTicks > 0 ? new DateTime(message.TimestampUtcTicks, DateTimeKind.Utc) : DateTime.UtcNow,
				Price = message.ExecutionPrice,
				Quantity = Math.Max(1, message.ExecutionQuantity)
			};
			leaderFills[fill.LeaderOrderKey] = fill;
			UpdateCopiedLeaderFill(fill.LeaderOrderKey, fill);
			WriteLog("RemoteLeaderFill", fill.LeaderAccountName, null, fill.Instrument, fill.Action.ToString(), null, fill.Quantity, fill.Price, 0, 0, 0, "Remote leader filled");
		}

		private void ReplicateOrder(Order leaderOrder, OrderState state, ErrorCode error, string leaderKey, bool protective, bool fromNetwork)
		{
			if (leaderOrder == null || string.IsNullOrWhiteSpace(leaderKey))
				return;

			if (error != ErrorCode.NoError || state == OrderState.Rejected)
				return;

			if (IsCancelState(state)) {
				CancelMappedFollowers(leaderKey);
				return;
			}

			if (!ShouldSubmitOrUpdate(state, leaderOrder.OrderType))
				return;

			foreach (OrcaFollowerAccountState follower in followersByName.Values.ToArray()) {
				if (!CanCopyToFollower(follower, leaderOrder.Account, protective))
					continue;
				SubmitOrUpdateFollowerOrder(
					follower,
					leaderKey,
					leaderOrder.Instrument,
					leaderOrder.OrderAction,
					leaderOrder.OrderType,
					leaderOrder.TimeInForce,
					leaderOrder.Quantity,
					leaderOrder.LimitPrice,
					leaderOrder.StopPrice,
					leaderOrder.Oco,
					leaderOrder.Name,
					leaderOrder.Gtd,
					protective);
			}
		}

		private void ReplicateOrderMessage(OrcaCopyNetworkMessage message, Instrument instrument, OrderState state, ErrorCode error)
		{
			if (message == null || string.IsNullOrWhiteSpace(message.LeaderOrderKey))
				return;

			if (message.IsCancel || IsCancelState(state)) {
				CancelMappedFollowers(message.LeaderOrderKey);
				return;
			}

			if (error != ErrorCode.NoError || !ShouldSubmitOrUpdate(state, ParseEnum(message.OrderType, OrderType.Unknown)))
				return;

			OrderAction action = ParseEnum(message.OrderAction, OrderAction.Buy);
			OrderType orderType = ParseEnum(message.OrderType, OrderType.Market);
			TimeInForce tif = ParseEnum(message.TimeInForce, TimeInForce.Day);
			foreach (OrcaFollowerAccountState follower in followersByName.Values.ToArray()) {
				if (!CanCopyToFollower(follower, null, message.IsProtective))
					continue;
				SubmitOrUpdateFollowerOrder(
					follower,
					message.LeaderOrderKey,
					instrument,
					action,
					orderType,
					tif,
					message.Quantity,
					message.LimitPrice,
					message.StopPrice,
					message.Oco,
					message.Name,
					DateTime.MaxValue,
					message.IsProtective);
			}
		}

		private void SubmitOrUpdateFollowerOrder(
			OrcaFollowerAccountState follower,
			string leaderKey,
			Instrument instrument,
			OrderAction action,
			OrderType orderType,
			TimeInForce tif,
			int leaderQuantity,
			double limitPrice,
			double stopPrice,
			string leaderOco,
			string leaderName,
			DateTime gtd,
			bool protective)
		{
			if (follower == null || follower.Account == null || instrument == null)
				return;

			ConcurrentDictionary<string, OrcaCopiedOrderState> byFollower = copiedByLeaderKey.GetOrAdd(leaderKey, k => new ConcurrentDictionary<string, OrcaCopiedOrderState>(StringComparer.OrdinalIgnoreCase));
			OrcaCopiedOrderState existing;
			if (byFollower.TryGetValue(follower.AccountName, out existing) && existing.FollowerOrder != null) {
				UpdateFollowerOrder(existing, leaderQuantity, limitPrice, stopPrice);
				return;
			}

			int followerQuantity = CalculateFollowerQuantity(leaderQuantity);
			if (followerQuantity <= 0)
				return;

			string followerOco = GetFollowerOco(leaderOco, follower.AccountName);
			string signalName = BuildFollowerSignalName(leaderName, leaderKey, protective);
			try {
				Order followerOrder = follower.Account.CreateOrder(
					instrument,
					action,
					orderType,
					OrderEntry.Manual,
					tif,
					followerQuantity,
					NormalizeLimit(orderType, limitPrice),
					NormalizeStop(orderType, stopPrice),
					followerOco,
					signalName,
					gtd == DateTime.MinValue ? DateTime.MaxValue : gtd,
					null);

				OrcaCopiedOrderState copied = new OrcaCopiedOrderState {
					LeaderOrderKey = leaderKey,
					FollowerAccountName = follower.AccountName,
					LeaderOco = leaderOco,
					FollowerOrder = followerOrder,
					Instrument = instrument,
					LeaderAction = action,
					OrderType = orderType,
					Quantity = followerQuantity,
					IsProtective = protective,
					LeaderSubmitUtc = DateTime.UtcNow
				};
				OrcaLeaderFillSnapshot fill;
				if (leaderFills.TryGetValue(leaderKey, out fill)) {
					copied.LeaderFillUtc = fill.FillUtc;
					copied.LeaderFillPrice = fill.Price;
					copied.LeaderFillQuantity = fill.Quantity;
				}

				byFollower[follower.AccountName] = copied;
				RegisterFollowerOrderKeys(followerOrder, copied);
				follower.Account.Submit(new[] { followerOrder });
				WriteLog("SubmitFollowerOrder", settings.LeaderAccountName, follower.AccountName, instrument, action.ToString(), orderType.ToString(), followerQuantity, 0, 0, 0, 0, "Submitted copied order");
			} catch (Exception ex) {
				TriggerFollowerGuard(follower, "Submit failed: " + ex.Message, instrument);
			}
		}

		private void UpdateFollowerOrder(OrcaCopiedOrderState copied, int leaderQuantity, double limitPrice, double stopPrice)
		{
			if (copied == null || copied.FollowerOrder == null || copied.FollowerOrder.Account == null)
				return;
			if (!IsLiveWorkingState(copied.FollowerOrder.OrderState))
				return;

			int newQuantity = CalculateFollowerQuantity(leaderQuantity);
			if (newQuantity <= 0)
				return;

			bool changed = false;
			if (newQuantity != copied.FollowerOrder.Quantity) {
				copied.FollowerOrder.QuantityChanged = newQuantity;
				copied.Quantity = newQuantity;
				changed = true;
			}
			if (copied.FollowerOrder.IsLimit && Math.Abs(copied.FollowerOrder.LimitPrice - limitPrice) > 0.0000001) {
				copied.FollowerOrder.LimitPriceChanged = limitPrice;
				changed = true;
			}
			if ((copied.FollowerOrder.IsStopMarket || copied.FollowerOrder.IsStopLimit) && Math.Abs(copied.FollowerOrder.StopPrice - stopPrice) > 0.0000001) {
				copied.FollowerOrder.StopPriceChanged = stopPrice;
				changed = true;
			}
			if (!changed)
				return;

			try {
				copied.FollowerOrder.Account.Change(new[] { copied.FollowerOrder });
			} catch (Exception ex) {
				OrcaFollowerAccountState follower = GetFollowerState(copied.FollowerOrder.Account);
				TriggerFollowerGuard(follower, "Change failed: " + ex.Message, copied.FollowerOrder.Instrument);
			}
		}

		private void CancelMappedFollowers(string leaderKey)
		{
			ConcurrentDictionary<string, OrcaCopiedOrderState> byFollower;
			if (!copiedByLeaderKey.TryGetValue(leaderKey, out byFollower))
				return;

			foreach (OrcaCopiedOrderState copied in byFollower.Values.ToArray()) {
				try {
					if (copied.FollowerOrder != null && copied.FollowerOrder.Account != null && IsLiveWorkingState(copied.FollowerOrder.OrderState))
						copied.FollowerOrder.Account.Cancel(new[] { copied.FollowerOrder });
				} catch (Exception ex) {
					OrcaFollowerAccountState follower = copied.FollowerOrder == null ? null : GetFollowerState(copied.FollowerOrder.Account);
					TriggerFollowerGuard(follower, "Cancel failed: " + ex.Message, copied.Instrument);
				}
			}
		}

		private void TriggerFollowerGuard(OrcaFollowerAccountState follower, string message, Instrument preferredInstrument)
		{
			if (follower == null || follower.Account == null)
				return;

			RunOnUi(() => follower.Disarm(message));
			OrcaCopyDiagnostics.Print("Follower guard tripped for " + follower.AccountName + ": " + message, LogLevel.Error);
			WriteLog("FollowerGuard", settings.LeaderAccountName, follower.AccountName, preferredInstrument, null, null, 0, 0, 0, 0, 0, message);
			FlattenAccount(follower.Account, message, true);
			lock (sync)
				UnhookFollowerNoLock(follower.Account);
		}

		private void FlattenAccount(Account account, string reason, bool guardTriggered)
		{
			if (account == null)
				return;

			try {
				List<Order> cancelOrders = account.Orders
					.Where(o => o != null && IsLiveWorkingState(o.OrderState))
					.ToList();
				if (cancelOrders.Count > 0)
					account.Cancel(cancelOrders);
			} catch (Exception ex) {
				OrcaCopyDiagnostics.Print("Cancel pending orders failed for " + account.Name + ": " + ex.Message, LogLevel.Warning);
			}

			try {
				Collection<Instrument> instruments = new Collection<Instrument>();
				foreach (Position position in account.Positions) {
					if (position != null && position.Instrument != null && position.MarketPosition != MarketPosition.Flat && !ContainsInstrument(instruments, position.Instrument))
						instruments.Add(position.Instrument);
				}
				if (instruments.Count > 0)
					account.Flatten(instruments);
				WriteLog(guardTriggered ? "GuardFlatten" : "ManualFlatten", settings.LeaderAccountName, account.Name, instruments.Count > 0 ? instruments[0] : null, null, null, 0, 0, 0, 0, 0, reason);
			} catch (Exception ex) {
				OrcaCopyDiagnostics.Print("Flatten failed for " + account.Name + ": " + ex.Message, LogLevel.Error);
			}
		}

		private bool CanCopyToFollower(OrcaFollowerAccountState follower, Account sourceLeaderAccount, bool protective)
		{
			if (follower == null || follower.Account == null || !follower.Enabled || follower.IsDisarmed)
				return false;
			if (protective && !follower.AtmCopy)
				return false;
			if (sourceLeaderAccount != null && string.Equals(follower.AccountName, sourceLeaderAccount.Name, StringComparison.OrdinalIgnoreCase))
				return false;
			return true;
		}

		private int CalculateFollowerQuantity(int leaderQuantity)
		{
			int sourceQty = Math.Max(1, leaderQuantity);
			switch (settings.CopyMethod) {
				case OrcaCopyMethod.FixedQuantity:
					return Math.Max(1, settings.FixedQuantity);
				case OrcaCopyMethod.Multiplier:
					return Math.Max(1, (int)Math.Round(sourceQty * Math.Max(0.01, settings.Multiplier), MidpointRounding.AwayFromZero));
				default:
					return sourceQty;
			}
		}

		private string GetFollowerOco(string leaderOco, string followerAccountName)
		{
			if (string.IsNullOrWhiteSpace(leaderOco))
				return string.Empty;
			string key = followerAccountName + "|" + leaderOco;
			return followerOcoByLeaderOco.GetOrAdd(key, k => "OrcaCopyOCO_" + Guid.NewGuid().ToString("N"));
		}

		private static string BuildFollowerSignalName(string leaderName, string leaderKey, bool protective)
		{
			string role = protective ? "Protect" : "Entry";
			string shortKey = string.IsNullOrWhiteSpace(leaderKey) ? Guid.NewGuid().ToString("N").Substring(0, 8) : Math.Abs(leaderKey.GetHashCode()).ToString("X");
			string source = string.IsNullOrWhiteSpace(leaderName) ? role : leaderName;
			source = source.Replace(" ", "");
			if (source.Length > 14)
				source = source.Substring(0, 14);
			return "OrcaCopy" + role + "_" + source + "_" + shortKey;
		}

		private static double NormalizeLimit(OrderType orderType, double limitPrice)
		{
			return orderType == OrderType.Limit || orderType == OrderType.StopLimit || orderType == OrderType.MIT ? limitPrice : 0;
		}

		private static double NormalizeStop(OrderType orderType, double stopPrice)
		{
			return orderType == OrderType.StopMarket || orderType == OrderType.StopLimit ? stopPrice : 0;
		}

		private static bool ShouldSubmitOrUpdate(OrderState state, OrderType orderType)
		{
			if (state == OrderState.Submitted || state == OrderState.Accepted || state == OrderState.AcceptedByRisk || state == OrderState.Working || state == OrderState.ChangeSubmitted || state == OrderState.TriggerPending)
				return true;
			return state == OrderState.Filled && orderType == OrderType.Market;
		}

		private static bool IsCancelState(OrderState state)
		{
			return state == OrderState.Cancelled || state == OrderState.CancelPending || state == OrderState.CancelSubmitted;
		}

		private static bool IsLiveWorkingState(OrderState state)
		{
			return state == OrderState.Accepted
				|| state == OrderState.AcceptedByRisk
				|| state == OrderState.Submitted
				|| state == OrderState.Working
				|| state == OrderState.PartFilled
				|| state == OrderState.ChangePending
				|| state == OrderState.ChangeSubmitted
				|| state == OrderState.CancelPending
				|| state == OrderState.CancelSubmitted
				|| state == OrderState.TriggerPending;
		}

		private static bool IsProtectiveOrder(Order order)
		{
			if (order == null)
				return false;
			string name = (order.Name ?? string.Empty).ToLowerInvariant();
			bool namedProtective = name.Contains("stop") || name.Contains("target") || name.Contains("profit") || name.Contains("loss") || name.Contains("sl") || name.Contains("tp");
			bool ocoProtection = !string.IsNullOrWhiteSpace(order.Oco) && (order.OrderType == OrderType.Limit || order.OrderType == OrderType.StopLimit || order.OrderType == OrderType.StopMarket || order.OrderType == OrderType.MIT);
			bool hasEntrySignal = !string.IsNullOrWhiteSpace(order.FromEntrySignal);
			return namedProtective || ocoProtection || hasEntrySignal;
		}

		private void BroadcastOrder(Order order, OrderEventArgs e, string leaderKey, bool protective)
		{
			if (settings.NetworkMode != OrcaCopyNetworkMode.LeaderServer || order == null)
				return;

			network.Broadcast(new OrcaCopyNetworkMessage {
				MessageType = "OrderUpdate",
				LeaderAccountName = order.Account == null ? settings.LeaderAccountName : order.Account.Name,
				LeaderOrderKey = leaderKey,
				InstrumentFullName = order.Instrument == null ? string.Empty : order.Instrument.FullName,
				OrderAction = order.OrderAction.ToString(),
				OrderType = order.OrderType.ToString(),
				OrderState = e == null ? order.OrderState.ToString() : e.OrderState.ToString(),
				TimeInForce = order.TimeInForce.ToString(),
				Oco = order.Oco,
				Name = order.Name,
				Quantity = order.Quantity,
				Filled = e == null ? order.Filled : e.Filled,
				LimitPrice = e == null ? order.LimitPrice : e.LimitPrice,
				StopPrice = e == null ? order.StopPrice : e.StopPrice,
				AverageFillPrice = e == null ? order.AverageFillPrice : e.AverageFillPrice,
				IsProtective = protective,
				IsCancel = e != null && IsCancelState(e.OrderState),
				IsChange = e != null && (e.OrderState == OrderState.ChangePending || e.OrderState == OrderState.ChangeSubmitted),
				TimestampUtcTicks = DateTime.UtcNow.Ticks
			});
		}

		private void BroadcastExecution(OrcaLeaderFillSnapshot fill)
		{
			if (settings.NetworkMode != OrcaCopyNetworkMode.LeaderServer || fill == null)
				return;

			network.Broadcast(new OrcaCopyNetworkMessage {
				MessageType = "ExecutionUpdate",
				LeaderAccountName = fill.LeaderAccountName,
				LeaderOrderKey = fill.LeaderOrderKey,
				InstrumentFullName = fill.Instrument == null ? string.Empty : fill.Instrument.FullName,
				OrderAction = fill.Action.ToString(),
				ExecutionPrice = fill.Price,
				ExecutionQuantity = fill.Quantity,
				TimestampUtcTicks = fill.FillUtc.Ticks
			});
		}

		private void UpdateCopiedLeaderFill(string leaderKey, OrcaLeaderFillSnapshot fill)
		{
			ConcurrentDictionary<string, OrcaCopiedOrderState> byFollower;
			if (!copiedByLeaderKey.TryGetValue(leaderKey, out byFollower))
				return;

			foreach (OrcaCopiedOrderState copied in byFollower.Values.ToArray()) {
				copied.LeaderFillUtc = fill.FillUtc;
				copied.LeaderFillPrice = fill.Price;
				copied.LeaderFillQuantity = fill.Quantity;
			}
		}

		private OrcaCopiedOrderState FindCopiedFollowerOrder(Order order)
		{
			if (order == null || order.Account == null)
				return null;

			foreach (string key in GetFollowerLookupKeys(order)) {
				OrcaCopiedOrderState copied;
				if (copiedByFollowerKey.TryGetValue(key, out copied))
					return copied;
			}
			return null;
		}

		private void RegisterFollowerOrderKeys(Order order, OrcaCopiedOrderState copied)
		{
			if (order == null || order.Account == null || copied == null)
				return;

			foreach (string key in GetFollowerLookupKeys(order)) {
				copiedByFollowerKey[key] = copied;
				copied.FollowerOrderKey = key;
			}
		}

		private static IEnumerable<string> GetFollowerLookupKeys(Order order)
		{
			if (order == null || order.Account == null)
				yield break;
			string prefix = order.Account.Name + "|";
			if (order.Id != 0)
				yield return prefix + "ID:" + order.Id;
			if (!string.IsNullOrWhiteSpace(order.OrderId))
				yield return prefix + "OID:" + order.OrderId;
			if (!string.IsNullOrWhiteSpace(order.Name))
				yield return prefix + "NAME:" + order.Name;
		}

		private static string GetLeaderOrderKey(Order order, string orderId)
		{
			if (order != null && order.Account != null) {
				if (!string.IsNullOrWhiteSpace(order.OrderId))
					return order.Account.Name + "|OID:" + order.OrderId;
				if (order.Id != 0)
					return order.Account.Name + "|ID:" + order.Id;
				if (!string.IsNullOrWhiteSpace(order.Name))
					return order.Account.Name + "|NAME:" + order.Name + "|" + order.Time.Ticks;
			}
			if (!string.IsNullOrWhiteSpace(orderId))
				return "RemoteLeader|OID:" + orderId;
			return "RemoteLeader|ID:" + Guid.NewGuid().ToString("N");
		}

		private double CalculateSlippageTicks(OrcaLeaderFillSnapshot leaderFill, double followerPrice)
		{
			if (leaderFill == null || leaderFill.Instrument == null || leaderFill.Instrument.MasterInstrument == null)
				return 0;
			double tickSize = leaderFill.Instrument.MasterInstrument.TickSize;
			if (tickSize <= 0)
				return 0;
			return Math.Abs(followerPrice - leaderFill.Price) / tickSize;
		}

		private OrcaFollowerAccountState GetFollowerState(Account account)
		{
			if (account == null)
				return null;
			OrcaFollowerAccountState follower;
			return followersByName.TryGetValue(account.Name, out follower) ? follower : null;
		}

		private static Account ResolveAccount(string accountName)
		{
			if (string.IsNullOrWhiteSpace(accountName))
				return null;
			try {
				return Account.All.FirstOrDefault(a => a != null && string.Equals(a.Name, accountName, StringComparison.OrdinalIgnoreCase));
			} catch { return null; }
		}

		private static Instrument ResolveInstrument(string fullName)
		{
			if (string.IsNullOrWhiteSpace(fullName))
				return null;
			try { return Instrument.GetInstrument(fullName, true); }
			catch { return null; }
		}

		private static bool ContainsInstrument(Collection<Instrument> instruments, Instrument instrument)
		{
			foreach (Instrument existing in instruments) {
				if (existing == instrument)
					return true;
				if (existing != null && instrument != null && string.Equals(existing.FullName, instrument.FullName, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		private static DateTime ToUtc(DateTime time)
		{
			if (time.Kind == DateTimeKind.Utc)
				return time;
			if (time == DateTime.MinValue)
				return DateTime.UtcNow;
			try { return time.ToUniversalTime(); }
			catch { return DateTime.UtcNow; }
		}

		private static OrderAction InferActionFromMarketPosition(MarketPosition marketPosition)
		{
			return marketPosition == MarketPosition.Short ? OrderAction.SellShort : OrderAction.Buy;
		}

		private static T ParseEnum<T>(string value, T fallback) where T : struct
		{
			try {
				T parsed;
				if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, true, out parsed))
					return parsed;
			} catch { }
			return fallback;
		}

		private void WriteLog(string eventType, string leaderAccountName, string followerAccountName, Instrument instrument, string action, string orderType, int quantity, double leaderPrice, double followerPrice, int latency, double slippageTicks, string message)
		{
			OrcaCopyTradeLogger.Write(new OrcaCopyLogRecord {
				TimeUtc = DateTime.UtcNow,
				EventType = eventType,
				LeaderAccount = leaderAccountName,
				FollowerAccount = followerAccountName,
				Instrument = instrument == null ? null : instrument.FullName,
				OrderAction = action,
				OrderType = orderType,
				Quantity = quantity,
				LeaderPrice = leaderPrice,
				FollowerPrice = followerPrice,
				LatencyMs = latency,
				SlippageTicks = slippageTicks,
				Message = message
			});
		}

		private void RunOnUi(Action action)
		{
			if (action == null)
				return;
			try {
				if (dispatcher.CheckAccess())
					action();
				else
					dispatcher.BeginInvoke(action);
			} catch { }
		}

		private void RaiseStatus(string status)
		{
			EventHandler<string> handler = StatusChanged;
			if (handler != null)
				RunOnUi(() => handler(this, status));
		}
	}
}
