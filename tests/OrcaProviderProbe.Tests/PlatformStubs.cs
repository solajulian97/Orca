using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Data;

// Shape-only callbacks; real API binding is checked by OrcaProvider.PlatformCheck.
namespace NinjaTrader.Cbi
{
    public enum ConnectionStatus { Connected, Connecting, ConnectionLost, Disconnected }
    public class Connection { public ConnectionStatus PriceStatus; }
    public class ConnectionStatusEventArgs
    {
        public ConnectionStatus PriceStatus, PreviousPriceStatus, Status, PreviousStatus;
        public Connection Connection;
        public string Error = "NoError";
    }
    public class Instrument { public string FullName = "ES SEP26"; }
}
namespace NinjaTrader.Data
{
    public enum MarketDataType { Last, Bid, Ask }
    public class MarketDataEventArgs
    {
        public MarketDataType MarketDataType;
        public DateTime Time = new DateTime(2026, 9, 7, 20, 0, 0, DateTimeKind.Utc);
        public bool IsReset;
        public double Price = 100, Bid = 99, Ask = 100;
        public long Volume = 1;
    }
    public class Bars
    {
        public bool IsTickReplay, IsInReplayMode, IsFirstBarOfSession;
        public TradingHours TradingHours = new TradingHours();
    }
}
namespace NinjaTrader.Core
{
    public static class Globals { public static Options GeneralOptions = new Options(); }
    public class Options { public TimeZoneInfo TimeZoneInfo = TimeZoneInfo.Utc; }
}
namespace NinjaTrader.NinjaScript
{
    public enum State { SetDefaults, DataLoaded, Historical, Transition, Realtime, Terminated }
    public enum Calculate { OnEachTick }
}
namespace NinjaTrader.NinjaScript.Indicators
{
    public class Indicator
    {
        public string Name, Description;
        public bool IsOverlay, IsAutoScale, DisplayInDataBox, PaintPriceMarkers, IsSuspendedWhileInactive;
        public int BarsRequiredToPlot, BarsInProgress, CurrentBar;
        public Calculate Calculate;
        public Instrument Instrument = new Instrument();
        public Bars Bars = new Bars();
        public State State;
        public readonly List<string> Output = new List<string>();
        protected virtual void OnStateChange() { }
        protected virtual void OnBarUpdate() { }
        protected virtual void OnMarketData(MarketDataEventArgs e) { }
        protected virtual void OnConnectionStatusUpdate(ConnectionStatusEventArgs e) { }
        protected void Print(string text) { Output.Add(text); }
        public void SetState(State state) { State = state; OnStateChange(); }
        public void Emit(MarketDataEventArgs e) { OnMarketData(e); }
        public void Connection(ConnectionStatus status) { OnConnectionStatusUpdate(new ConnectionStatusEventArgs { PriceStatus = status }); }
    }
    public enum OrcaDiagnosticsWorkKind { MarketData }
    public static class OrcaDiagnosticsCore
    {
        public static bool IsEnabled { get { return false; } }
        public static void RegisterInstance(string id, string name, object owner) { }
        public static void UnregisterInstance(string id) { }
        public static void ReportSeriesDeclaration(string id, int bip, string kind, string owner, string reason) { }
        public static void ReportState(string id, string state) { }
        public static long ReportMarketData(string id, MarketDataType type, DateTime time) { return 0; }
        public static long BeginWorkSample(long sample) { return 0; }
        public static void ReportWorkSample(string id, OrcaDiagnosticsWorkKind kind, int bip, long start) { }
        public static void ReportSourceDeclaration(string id, string source, string health, string cache) { }
        public static void ReportCacheStatus(string id, string cache, string status) { }
    }
}
