// Compiled by Program.cs into a .NET Framework executable. Real source methods are
// inserted at markers; accounts/instruments are fixtures and never call NinjaTrader.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

public sealed class MasterInstrument { public string Name; public double TickSize = 0.25; }
public sealed class Instrument {
    public string FullName;
    public MasterInstrument MasterInstrument;
    public static readonly Dictionary<string, Instrument> Catalog = new Dictionary<string, Instrument>(StringComparer.OrdinalIgnoreCase);
    public static readonly List<string> Lookups = new List<string>();
    public static bool ThrowLookup;
    public static Instrument Make(string root, string expiry = "09-26") {
        return new Instrument { FullName = root + (expiry == null ? "" : " " + expiry), MasterInstrument = new MasterInstrument { Name = root } };
    }
    public static Instrument GetInstrument(string name, bool create) {
        Lookups.Add(name);
        if (ThrowLookup) throw new InvalidOperationException("fixture lookup unavailable");
        Instrument instrument;
        return Catalog.TryGetValue(name, out instrument) ? instrument : null;
    }
}
public enum OrderAction { Buy, Sell, SellShort, BuyToCover }
public enum OrderType { Market, Limit, StopMarket }
public enum OrderEntry { Manual }
public enum TimeInForce { Day }
public sealed class Order {
    public Instrument Instrument;
    public OrderAction Action;
    public OrderType Type;
    public int Quantity;
    public double Limit, Stop;
    public string Name;
}
public sealed class Account {
    public static readonly List<Order> Submitted = new List<Order>();
    public Order CreateOrder(Instrument instrument, OrderAction action, OrderType type, OrderEntry entry,
        TimeInForce tif, int quantity, double limit, double stop, string oco, string name, DateTime expiry, object custom) {
        return new Order { Instrument = instrument, Action = action, Type = type, Quantity = quantity, Limit = limit, Stop = stop, Name = name };
    }
    public void Submit(Order[] orders) { Submitted.AddRange(orders); }
}
public sealed class ChartTrader { public Account Account; }
public sealed class ChartControl { public Instrument Instrument; }
public sealed class ChartTab : UserControl { public ChartControl ChartControl; }
public sealed class Chart : Window {
    public TabControl MainTabControl = new TabControl();
    public ChartTrader ChartTrader = new ChartTrader();
    public new bool IsLoaded = true;
}
public static class OrcaReplayCore {
    public static bool Locked;
    public static bool IsChartLocked(ChartControl chart) { return Locked; }
}
public sealed class RouterSettings { public bool Enabled = true, RouteEsToMes = true, RouteNqToMnq = true; }
public static class OrcaExecutionRouter {
    private static readonly object Sync = new object();
    public static RouterSettings settings = new RouterSettings();
    private static void EnsureLoaded() { }
    /* ENTRY_ROUTER */
}
public sealed class OrcaRiskManagerAddOn {
    public sealed class ChartWindowBinding {
        public Chart ChartWindow;
        public bool IsDetached;
        public int RefreshQueued;
    }
    public int Refreshes;
    public ChartTab RefreshedTab;
    private bool IsBindingActive(ChartWindowBinding binding) { return binding != null && !binding.IsDetached; }
    private void AttachSelectionChangedHandler(ChartWindowBinding binding) { }
    private void RefreshChartWindowPanels(Chart chart) { Refreshes++; RefreshedTab = GetSelectedChartTab(chart); }
    public void Queue(ChartWindowBinding binding) { QueueChartWindowRefresh(binding); }
    /* SELECTED_TAB */
    /* QUEUED_REFRESH */
}
public sealed class Anchor { public double Price; }
public sealed class Drawing { public Anchor StartAnchor = new Anchor(); }
public sealed class OrcaRiskPanel : UserControl {
    private ChartTab attachedTab;
    public bool isCleanedUp;
    public bool isPanelRuntimeActive = true;
    private long entryTabBindingVersion;
    public string LastBlock;
    public Action DuringConfirmation;
    public bool ConfirmationResult = true;
    private TextBox txtContracts = new TextBox { Text = "3" };
    private bool isLongSelected = true;
    private Drawing hEntry = new Drawing { StartAnchor = new Anchor { Price = 6000 } };
    private Drawing hStop = new Drawing { StartAnchor = new Anchor { Price = 5995 } };
    private Drawing hTarget = new Drawing { StartAnchor = new Anchor { Price = 6010 } };
    private double pendingStopPrice, pendingTargetPrice;
    private int pendingContracts;
    private string pendingEntryName;
    private double stagedEntryPrice = 6000, stagedStopPrice = 5995, stagedTargetPrice = 6010;
    private int stagedQuantity = 3;
    private OrderType stagedEntryOrderType = OrderType.Limit;
    private OrderAction stagedEntryAction = OrderAction.Buy;
    private string dragOrderType = "BuyLimit";
    public void Bind(ChartTab tab) { if (!ReferenceEquals(attachedTab, tab)) { attachedTab = tab; entryTabBindingVersion++; } }
    private bool BlockEntry(string reason) { LastBlock = reason; return false; }
    private bool ConfirmLiveOrderIfNeeded(Account account, string text) { if (DuringConfirmation != null) DuringConfirmation(); return ConfirmationResult; }
    private bool CanSubmitHotkeyOrder(Account account, string text = "Submit staged bracket") { return ConfirmLiveOrderIfNeeded(account, text); }
    private int ParseQuantity() { return 3; }
    private double RoundToTick(double price) { return price; }
    private double GetActivePrice() { return 6000; }
    private void ResolveEntryOrderAtPrice(double price, MouseButton button, out OrderAction action, out OrderType type) {
        action = button == MouseButton.Left ? OrderAction.Buy : OrderAction.Sell;
        type = OrderType.Limit;
    }
    private void RemoveStagedBracketOverlay() { }
    public void Send(int path) {
        switch (path) {
            case 0: ExecuteTrade(OrderType.Limit); break;
            case 1: ExecuteFastCommand("BuyMkt"); break;
            case 2: PlaceDragOrderAt(6000); break;
            case 3: SubmitStagedBracket(); break;
            case 4: SubmitAltSpaceQuickEntryAt(6000, MouseButton.Left); break;
        }
    }
    /* ENTRY_CONTEXT */
    /* ENTRY_METHODS */
}
public sealed class Scenario {
    public Chart Window = new Chart();
    public ChartTab A = new ChartTab { ChartControl = new ChartControl { Instrument = Instrument.Make("ES") } };
    public ChartTab B = new ChartTab { ChartControl = new ChartControl { Instrument = Instrument.Make("MES") } };
    public OrcaRiskPanel Panel = new OrcaRiskPanel();
    public Scenario() {
        Instrument.Catalog.Clear(); Instrument.Lookups.Clear(); Instrument.ThrowLookup = false;
        Instrument.Catalog["MES 09-26"] = Instrument.Make("MES");
        Instrument.Catalog["MNQ 09-26"] = Instrument.Make("MNQ");
        OrcaExecutionRouter.settings = new RouterSettings(); OrcaReplayCore.Locked = false; Account.Submitted.Clear();
        Window.ChartTrader.Account = new Account();
        Window.MainTabControl.Items.Add(new TabItem { Header = "A", Content = A });
        Window.MainTabControl.Items.Add(new TabItem { Header = "B", Content = B });
        var host = new Grid(); host.Children.Add(Window.MainTabControl); host.Children.Add(Panel); Window.Content = host;
        Window.MainTabControl.SelectedIndex = 0; Panel.Bind(A);
    }
}
public static class Program {
    private static int checks;
    private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    private static void Drain() {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    [STAThread]
    public static int Main(string[] args) {
        try {
            bool legacy = args[0] == "legacy";
            AppContext.SetSwitch("Switch.System.Windows.Controls.TabControl.SelectionPropertiesCanLagBehindSelectionChangedEvent", legacy);
            var s = new Scenario();
            int staleContent = 0, wrongSelected = 0;
            s.Window.MainTabControl.SelectionChanged += (sender, e) => {
                var tabs = s.Window.MainTabControl;
                ChartTab expected = ((TabItem)tabs.SelectedItem).Content as ChartTab;
                if (!ReferenceEquals(tabs.SelectedContent, expected)) staleContent++;
                if (!ReferenceEquals(OrcaRiskManagerAddOn.GetSelectedChartTab(s.Window), expected)) wrongSelected++;
            };
            for (int i = 0; i < 100; i++) s.Window.MainTabControl.SelectedIndex = 1 - s.Window.MainTabControl.SelectedIndex;
            Check(wrongSelected == 0, "Selected-item resolver followed stale content");
            Check(legacy ? staleContent == 100 : staleContent == 0, "WPF event ordering fixture mismatch");
            int staleDuringSwitches = staleContent;
            Check(OrcaRiskManagerAddOn.GetSelectedChartTab(null) == null, "Null window resolved a tab");
            var direct = new Chart(); direct.MainTabControl.Items.Add(new ChartTab()); direct.MainTabControl.SelectedIndex = 0;
            Check(ReferenceEquals(OrcaRiskManagerAddOn.GetSelectedChartTab(direct), direct.MainTabControl.SelectedItem), "Direct chart tab item unsupported");
            direct.MainTabControl.SelectedIndex = -1;
            Check(OrcaRiskManagerAddOn.GetSelectedChartTab(direct) == null, "No selection picked a tab");
            var coordinator = new OrcaRiskManagerAddOn();
            var binding = new OrcaRiskManagerAddOn.ChartWindowBinding { ChartWindow = s.Window };
            for (int i = 0; i < 50; i++) coordinator.Queue(binding);
            s.Window.MainTabControl.SelectedIndex = 1;
            Drain();
            Check(coordinator.Refreshes == 1 && ReferenceEquals(coordinator.RefreshedTab, s.B), "Queued refresh did not coalesce/use latest tab");
            coordinator.Queue(binding); binding.IsDetached = true; Drain();
            Check(coordinator.Refreshes == 1, "Detached owner refreshed a chart");

            Instrument resolved; string reason;
            Check(OrcaExecutionRouter.TryResolveEntryInstrument(Instrument.Make("NQ"), out resolved, out reason) && resolved.FullName == "MNQ 09-26", "NQ route failed");
            Check(!OrcaExecutionRouter.TryResolveEntryInstrument(null, out resolved, out reason), "Null instrument allowed");
            Check(!OrcaExecutionRouter.TryResolveEntryInstrument(Instrument.Make("ES", null), out resolved, out reason), "Expiry-free ES allowed");
            Check(OrcaExecutionRouter.TryResolveEntryInstrument(Instrument.Make("RTY"), out resolved, out reason) && resolved.FullName == "RTY 09-26", "Unmapped instrument changed");
            Instrument.Catalog["MES 09-26"] = Instrument.Make("MES", "12-26");
            Check(!OrcaExecutionRouter.TryResolveEntryInstrument(Instrument.Make("ES"), out resolved, out reason), "Wrong expiry allowed");
            Instrument.ThrowLookup = true;
            Check(!OrcaExecutionRouter.TryResolveEntryInstrument(Instrument.Make("ES"), out resolved, out reason), "Lookup exception allowed fallback");
            Instrument.ThrowLookup = false;
            OrcaExecutionRouter.settings.RouteEsToMes = false;
            Check(OrcaExecutionRouter.TryResolveEntryInstrument(Instrument.Make("ES"), out resolved, out reason) && resolved.FullName == "ES 09-26", "Disabled ES mapping changed");

            for (int path = 0; path < 5; path++) {
                s = new Scenario(); s.Panel.Send(path);
                Check(Account.Submitted.Count == 1 && Account.Submitted[0].Instrument.FullName == "MES 09-26", "ES entry did not route: " + path);
                Check(Account.Submitted[0].Quantity == 3 && Account.Submitted[0].Action == OrderAction.Buy, "Quantity/side changed: " + path);
                Check(path == 1 ? Account.Submitted[0].Type == OrderType.Market : Account.Submitted[0].Type == OrderType.Limit && Account.Submitted[0].Limit == 6000, "Price/type changed: " + path);
                s = new Scenario(); s.Window.MainTabControl.SelectedIndex = 1; s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Stale tab submitted: " + path);
                s.Panel.Bind(s.B); s.Panel.Send(path);
                Check(Account.Submitted.Count == 1 && Account.Submitted[0].Instrument.FullName == "MES 09-26", "Rebound micro tab failed: " + path);
                s = new Scenario(); s.Window.ChartTrader.Account = null; s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Missing account submitted: " + path);
                s = new Scenario(); Instrument.Catalog.Clear(); s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null && Instrument.Lookups.SequenceEqual(new[] { "MES 09-26" }), "Failed mapping fell back: " + path);
                s = new Scenario(); s.Panel.ConfirmationResult = false; s.Panel.Send(path);
                Check(Account.Submitted.Count == 0, "Cancelled confirmation submitted: " + path);
                s = new Scenario(); s.Panel.DuringConfirmation = () => s.Window.ChartTrader.Account = new Account(); s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Changed account submitted: " + path);
                s = new Scenario(); s.Panel.DuringConfirmation = () => OrcaExecutionRouter.settings.Enabled = false; s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Changed route submitted: " + path);
                s = new Scenario(); s.Panel.DuringConfirmation = () => { s.Window.MainTabControl.SelectedIndex = 1; s.Panel.Bind(s.B); }; s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Changed tab submitted: " + path);
                s = new Scenario(); s.B.ChartControl.Instrument = Instrument.Make("ES"); s.Panel.DuringConfirmation = () => { s.Window.MainTabControl.SelectedIndex = 1; s.Panel.Bind(s.B); }; s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Same-symbol different tab submitted: " + path);
                s = new Scenario(); s.Panel.DuringConfirmation = () => s.A.ChartControl.Instrument = Instrument.Make("NQ"); s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Changed chart instrument submitted: " + path);
                s = new Scenario(); s.Panel.DuringConfirmation = () => { s.Panel.Bind(s.B); s.Panel.Bind(s.A); }; s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Round-trip tab binding submitted stale plan: " + path);
                s = new Scenario(); s.Panel.DuringConfirmation = () => OrcaReplayCore.Locked = true; s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Replay lock after dialog submitted: " + path);
                s = new Scenario(); s.Panel.isPanelRuntimeActive = false; s.Panel.Send(path);
                Check(Account.Submitted.Count == 0 && s.Panel.LastBlock != null, "Hidden panel submitted: " + path);
                s = new Scenario(); OrcaExecutionRouter.settings.Enabled = false; s.Panel.Send(path);
                Check(Account.Submitted.Count == 1 && Account.Submitted[0].Instrument.FullName == "ES 09-26", "Intentionally disabled routing changed: " + path);
            }
            Console.WriteLine("PASS " + args[0] + ": " + checks + " checks; stale SelectedContent observed " + staleDuringSwitches + "/100 switches; correct selected-tab resolution 100/100; five entry paths verified.");
            return 0;
        } catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
}
