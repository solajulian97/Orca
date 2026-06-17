using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NinjaTrader.Core;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;

namespace OrcaTradeCopier
{
    public enum CopyMethod
    {
        ExactQuantity,
        Multiplier,
        FixedQuantity
    }

    public class FollowerAccountSettings
    {
        public string AccountName { get; set; }
        public bool IsEnabled { get; set; }
        public double Multiplier { get; set; } = 1.0;
        public int FixedQty { get; set; } = 1;
        public bool CopyAtm { get; set; } = true;

        // Metrics
        public long LastLatencyMs { get; set; }
        public double LastSlippageTicks { get; set; }
        public bool IsDisarmed { get; set; }
        public string DisarmReason { get; set; }
    }

    public class OrcaTradeCopierEngine : IDisposable
    {
        private Account _leaderAccount;
        private readonly ConcurrentDictionary<string, FollowerAccountSettings> _followers = new ConcurrentDictionary<string, FollowerAccountSettings>();
        private readonly ConcurrentDictionary<string, Order> _copiedOrders = new ConcurrentDictionary<string, Order>(); // Key: LeaderOrderId, Value: Follower Replicated Order
        private readonly ConcurrentDictionary<string, string> _leaderOrderMap = new ConcurrentDictionary<string, string>(); // Key: FollowerOrderId, Value: LeaderOrderId

        // Risk Settings (Follower Guard)
        public bool CancelOnUnfilledEntry { get; set; } = false;
        public int CancelEntryTimeoutSeconds { get; set; } = 5;
        public bool AutoFlattenOnOutOfSync { get; set; } = true;
        public int SyncTimeoutSeconds { get; set; } = 10;
        public bool FlattenOnReject { get; set; } = true;
        public double MaxAllowedSlippageTicks { get; set; } = 4.0;

        public CopyMethod DefaultCopyMethod { get; set; } = CopyMethod.ExactQuantity;
        public bool IsCopyingActive { get; private set; }

        // External Network
        private OrcaTradeCopierNetwork _network;

        // Events
        public event Action<string> OnLogMessage;
        public event Action OnStateUpdated;

        public OrcaTradeCopierEngine(OrcaTradeCopierNetwork network = null)
        {
            _network = network;
            if (_network != null)
            {
                _network.OnTradeMessageReceived += HandleNetworkTradeMessage;
            }
        }

        public void Initialize(Account leader, List<FollowerAccountSettings> followersList)
        {
            StopCopying();

            _leaderAccount = leader;
            _followers.Clear();
            foreach (var f in followersList)
            {
                _followers[f.AccountName] = f;
            }

            Log($"[Engine] Initialized with Leader: {leader.Name} and {followersList.Count} followers.");
        }

        public void StartCopying()
        {
            if (IsCopyingActive || _leaderAccount == null) return;

            IsCopyingActive = true;

            // Wire native NinjaTrader event loops
            _leaderAccount.OrderUpdate += OnLeaderOrderUpdate;
            _leaderAccount.ExecutionUpdate += OnLeaderExecutionUpdate;

            // Wire all Follower Account event loops for health auditing and risk checks
            foreach (var followerName in _followers.Keys)
            {
                var fAccount = GetNinjaTraderAccount(followerName);
                if (fAccount != null)
                {
                    fAccount.OrderUpdate += OnFollowerOrderUpdate;
                    fAccount.ExecutionUpdate += OnFollowerExecutionUpdate;
                }
            }

            Log("[Engine] Trade copying successfully armed and active.");
            OnStateUpdated?.Invoke();
        }

        public void StopCopying()
        {
            if (!IsCopyingActive) return;

            IsCopyingActive = false;

            if (_leaderAccount != null)
            {
                _leaderAccount.OrderUpdate -= OnLeaderOrderUpdate;
                _leaderAccount.ExecutionUpdate -= OnLeaderExecutionUpdate;
            }

            foreach (var followerName in _followers.Keys)
            {
                var fAccount = GetNinjaTraderAccount(followerName);
                if (fAccount != null)
                {
                    fAccount.OrderUpdate -= OnFollowerOrderUpdate;
                    fAccount.ExecutionUpdate -= OnFollowerExecutionUpdate;
                }
            }

            Log("[Engine] Trade copying disarmed.");
            OnStateUpdated?.Invoke();
        }

        public void FlattenAllFollowers()
        {
            Log("[EMERGENCY] Initiating Flatten All Followers command...");
            foreach (var pair in _followers)
            {
                var fSettings = pair.Value;
                if (fSettings.IsEnabled)
                {
                    FlattenFollowerAccount(fSettings.AccountName, "Emergency manual flatten request");
                }
            }
        }

        public void RearmAllFollowers()
        {
            Log("[Engine] Rearming and resetting all Follower Guard safeguards...");
            foreach (var pair in _followers)
            {
                pair.Value.IsDisarmed = false;
                pair.Value.DisarmReason = string.Empty;
            }
            OnStateUpdated?.Invoke();
        }

        private void OnLeaderOrderUpdate(object sender, OrderEventArgs e)
        {
            if (!IsCopyingActive) return;

            // Intercept only new orders, modifications, or cancellations
            if (e.OrderState == OrderState.Working || e.OrderState == OrderState.Accepted)
            {
                // If we haven't replicated this order yet, replicate it
                string linkKey = e.Order.OrderId + "_";
                bool alreadyReplicated = _copiedOrders.Keys.Any(k => k.StartsWith(linkKey));

                if (!alreadyReplicated)
                {
                    ReplicateOrderToFollowers(e.Order, "SUBMIT");
                }
                else
                {
                    ModifyFollowerOrders(e.Order);
                }
            }
            else if (e.OrderState == OrderState.Cancelled)
            {
                CancelFollowerOrders(e.Order);
            }
        }

        private void OnLeaderExecutionUpdate(object sender, ExecutionEventArgs e)
        {
            if (!IsCopyingActive) return;

            // Broadcast to LAN network followers if server mode is active
            if (_network != null && _network.IsRunning)
            {
                var msg = new TradeMessage
                {
                    InstrumentName = e.Execution.Instrument.FullName,
                    OrderAction = e.Execution.Order.OrderAction == OrderAction.Buy ? "BUY" : "SELL",
                    OrderType = e.Execution.Order.OrderType.ToString().ToUpper(),
                    ActionType = "SUBMIT",
                    LeaderOrderId = e.Execution.Order.OrderId,
                    Quantity = e.Execution.Quantity,
                    LimitPrice = e.Execution.Price,
                    TimestampTicks = DateTime.UtcNow.Ticks
                };
                _network.BroadcastTrade(msg);
            }
        }

        private void ReplicateOrderToFollowers(Order leaderOrder, string actionType)
        {
            // Do not copy child bracket orders if ATM copy is handled automatically by local templates
            if (leaderOrder.Name.Contains("Stop") || leaderOrder.Name.Contains("Target")) return;

            foreach (var pair in _followers)
            {
                var fSettings = pair.Value;
                if (!fSettings.IsEnabled || fSettings.IsDisarmed) continue;

                var fAccount = GetNinjaTraderAccount(fSettings.AccountName);
                if (fAccount == null) continue;

                int targetQty = CalculateTargetQuantity(leaderOrder.Quantity, fSettings);

                Log($"[Engine] Replicating Leader Order: {leaderOrder.OrderAction} {leaderOrder.Quantity} {leaderOrder.Instrument.FullName} -> Follower: {fSettings.AccountName} Qty: {targetQty}");

                try
                {
                    // Low-latency async execution mapping
                    Order followerOrder = fAccount.CreateOrder(
                        leaderOrder.Instrument,
                        leaderOrder.OrderAction,
                        leaderOrder.OrderType,
                        leaderOrder.TimeInForce,
                        targetQty,
                        leaderOrder.LimitPrice,
                        leaderOrder.StopPrice,
                        Guid.NewGuid().ToString(), // Unique string to tag
                        leaderOrder.Name,
                        null
                    );

                    // Track replication link
                    _copiedOrders[leaderOrder.OrderId + "_" + fSettings.AccountName] = followerOrder;
                    _leaderOrderMap[followerOrder.OrderId] = leaderOrder.OrderId;
                }
                catch (Exception ex)
                {
                    Log($"[Engine Error] Failed replicating to {fSettings.AccountName}: {ex.Message}");
                    DisarmFollower(fSettings, $"Submission execution failed: {ex.Message}");
                }
            }
        }

        private void CancelFollowerOrders(Order leaderOrder)
        {
            foreach (var pair in _followers)
            {
                var fSettings = pair.Value;
                string linkKey = leaderOrder.OrderId + "_" + fSettings.AccountName;
                if (_copiedOrders.TryGetValue(linkKey, out var fOrder))
                {
                    if (fOrder.OrderState == OrderState.Submitted || fOrder.OrderState == OrderState.Working)
                    {
                        var fAccount = GetNinjaTraderAccount(fSettings.AccountName);
                        fAccount?.Cancel(new[] { fOrder });
                        Log($"[Engine] Replicated Cancel for Leader Order: {leaderOrder.OrderId} -> Follower: {fSettings.AccountName}");
                    }
                }
            }
        }

        private void ModifyFollowerOrders(Order leaderOrder)
        {
            foreach (var pair in _followers)
            {
                var fSettings = pair.Value;
                string linkKey = leaderOrder.OrderId + "_" + fSettings.AccountName;
                if (_copiedOrders.TryGetValue(linkKey, out var fOrder))
                {
                    if (fOrder.OrderState == OrderState.Working)
                    {
                        var fAccount = GetNinjaTraderAccount(fSettings.AccountName);
                        if (fAccount != null)
                        {
                            // NT8 AddOn API does not expose Account.Change() — use cancel + resubmit.
                            // Cancel the stale follower working order; the next leader Working event
                            // will trigger a fresh replication via ReplicateOrderToFollowers.
                            fAccount.Cancel(new[] { fOrder });
                            Log($"[Engine] Cancelled stale follower order to re-sync: Leader {leaderOrder.OrderId} -> {fSettings.AccountName}");
                        }
                    }
                }
            }
        }

        private void OnFollowerOrderUpdate(object sender, OrderEventArgs e)
        {
            // Health Audit & Rejection Risk Checks
            if (e.OrderState == OrderState.Rejected)
            {
                if (_leaderOrderMap.TryGetValue(e.Order.OrderId, out string leaderId))
                {
                    string accountName = e.Order.Account.Name;
                    if (_followers.TryGetValue(accountName, out var fSettings))
                    {
                        Log($"[Follower Guard] Follower order {e.Order.OrderId} REJECTED in {accountName}. Reason: {e.Order.OrderState}");
                        if (FlattenOnReject)
                        {
                            DisarmFollower(fSettings, $"Order Rejected: {e.Order.OrderState}");
                            FlattenFollowerAccount(accountName, "Emergency Liquidation on Order Rejection");
                        }
                    }
                }
            }
        }

        private void OnFollowerExecutionUpdate(object sender, ExecutionEventArgs e)
        {
            // Auditing Fill Latency and Slippage in real-time
            if (_leaderOrderMap.TryGetValue(e.Execution.Order.OrderId, out string leaderId))
            {
                string accountName = e.Execution.Account.Name;
                if (_followers.TryGetValue(accountName, out var fSettings))
                {
                    // Retrieve corresponding leader execution to calculate slippage
                    double leaderPrice = _leaderAccount.Orders.FirstOrDefault(o => o.OrderId == leaderId)?.AverageFillPrice ?? 0;

                    if (leaderPrice > 0)
                    {
                        double slippageTicks = Math.Abs(e.Execution.Price - leaderPrice) / e.Execution.Instrument.MasterInstrument.TickSize;
                        long latencyMs = (DateTime.UtcNow.Ticks - e.Execution.Time.Ticks) / TimeSpan.TicksPerMillisecond;

                        fSettings.LastLatencyMs = Math.Max(0, latencyMs);
                        fSettings.LastSlippageTicks = Math.Round(slippageTicks, 1);

                        Log($"[Auditor] Follower {accountName} Filled. Latency: {fSettings.LastLatencyMs}ms, Slippage: {fSettings.LastSlippageTicks} ticks");

                        // Emergency check for excessive slippage
                        if (slippageTicks > MaxAllowedSlippageTicks)
                        {
                            Log($"[Follower Guard] Follower {accountName} experienced severe slippage of {fSettings.LastSlippageTicks} ticks (Limit: {MaxAllowedSlippageTicks} ticks). Triggering safeguards.");
                            DisarmFollower(fSettings, $"Excessive slippage: {fSettings.LastSlippageTicks} ticks");
                            if (AutoFlattenOnOutOfSync)
                            {
                                FlattenFollowerAccount(accountName, "Excessive Slippage Safety Trigger");
                            }
                        }
                    }
                    OnStateUpdated?.Invoke();
                }
            }
        }

        private void HandleNetworkTradeMessage(TradeMessage msg)
        {
            // Replicate incoming trade messages from LAN server node
            if (!IsCopyingActive) return;

            foreach (var pair in _followers)
            {
                var fSettings = pair.Value;
                if (!fSettings.IsEnabled || fSettings.IsDisarmed) continue;

                var fAccount = GetNinjaTraderAccount(fSettings.AccountName);
                if (fAccount == null) continue;

                var instrument = Instrument.GetInstrument(msg.InstrumentName);
                if (instrument == null) continue;

                int targetQty = CalculateTargetQuantity(msg.Quantity, fSettings);

                OrderAction action = msg.OrderAction == "BUY" ? OrderAction.Buy : OrderAction.Sell;
                OrderType oType = (OrderType)Enum.Parse(typeof(OrderType), msg.OrderType, true);

                Log($"[Engine] LAN Replicating: {action} {targetQty} {instrument.FullName} -> Follower: {fSettings.AccountName}");

                try
                {
                    fAccount.CreateOrder(
                        instrument,
                        action,
                        oType,
                        TimeInForce.Gtc,
                        targetQty,
                        msg.LimitPrice,
                        msg.StopPrice,
                        Guid.NewGuid().ToString(),
                        "LAN_REPLICATED",
                        null
                    );
                }
                catch (Exception ex)
                {
                    Log($"[Engine Error] LAN Replication failed for {fSettings.AccountName}: {ex.Message}");
                }
            }
        }

        private int CalculateTargetQuantity(int leaderQty, FollowerAccountSettings fSettings)
        {
            switch (DefaultCopyMethod)
            {
                case CopyMethod.Multiplier:
                    return (int)Math.Max(1, Math.Round(leaderQty * fSettings.Multiplier));
                case CopyMethod.FixedQuantity:
                    return fSettings.FixedQty;
                case CopyMethod.ExactQuantity:
                default:
                    return leaderQty;
            }
        }

        private void DisarmFollower(FollowerAccountSettings fSettings, string reason)
        {
            fSettings.IsDisarmed = true;
            fSettings.DisarmReason = reason;
            Log($"[Follower Guard] Account {fSettings.AccountName} DISARMED. Reason: {reason}");
            OnStateUpdated?.Invoke();
        }

        private void FlattenFollowerAccount(string accountName, string reason)
        {
            var fAccount = GetNinjaTraderAccount(accountName);
            if (fAccount != null)
            {
                Log($"[Engine Execution] Flattening account: {accountName}. Reason: {reason}");

                // NT8 Account.Flatten() takes System.Collections.ObjectModel.Collection<Instrument>
                lock (fAccount.Positions)
                {
                    foreach (Position pos in fAccount.Positions)
                    {
                        if (pos.MarketPosition != MarketPosition.Flat)
                        {
                            var instruments = new System.Collections.ObjectModel.Collection<Instrument>();
                            instruments.Add(pos.Instrument);
                            fAccount.Flatten(instruments);
                        }
                    }
                }
            }
        }

        private Account GetNinjaTraderAccount(string name)
        {
            lock (NinjaTrader.Cbi.Account.All)
            {
                return NinjaTrader.Cbi.Account.All.FirstOrDefault(a => a.Name == name);
            }
        }

        private void Log(string msg)
        {
            OnLogMessage?.Invoke(msg);
        }

        public void Dispose()
        {
            StopCopying();
            if (_network != null)
            {
                _network.OnTradeMessageReceived -= HandleNetworkTradeMessage;
            }
        }
    }
}
