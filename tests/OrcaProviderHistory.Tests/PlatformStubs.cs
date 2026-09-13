using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

// Shape-only scheduling/failure fixtures, not NinjaTrader callback-order proof.
namespace System.Windows.Threading
{
    public enum DispatcherPriority { Background }
    public class Dispatcher
    {
        public bool HasShutdownStarted, HasShutdownFinished, OnDispatcher, ThrowQueue;
        private EventHandler shutdown;
        public event EventHandler ShutdownStarted { add { shutdown += value; } remove { shutdown -= value; } }
        public int HookCount { get { return shutdown == null ? 0 : shutdown.GetInvocationList().Length; } }
        private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
        public void InvokeAsync(Action action) { if (ThrowQueue) throw new Exception("queue failure"); queue.Enqueue(action); }
        public bool CheckAccess() { return OnDispatcher; }
        public void Drain()
        { bool prior = OnDispatcher; OnDispatcher = true; try { Action action; while (queue.TryDequeue(out action)) action(); } finally { OnDispatcher = prior; } }
        public void Shutdown()
        { OnDispatcher = true; HasShutdownStarted = true; shutdown?.Invoke(this, EventArgs.Empty); HasShutdownFinished = true; OnDispatcher = false; }
    }
    public class DispatcherTimer
    {
        public static readonly List<DispatcherTimer> All = new List<DispatcherTimer>();
        public bool Active;
        public TimeSpan Interval;
        public event EventHandler Tick;
        private readonly Dispatcher dispatcher;
        public DispatcherTimer(DispatcherPriority priority, Dispatcher dispatcher) { this.dispatcher = dispatcher; All.Add(this); }
        public void Start() { Active = true; }
        public void Stop() { Active = false; }
        public void Fire() { if (Active) { dispatcher.OnDispatcher = true; try { Tick?.Invoke(this, EventArgs.Empty); } finally { dispatcher.OnDispatcher = false; } } }
    }
}
namespace NinjaTrader.Cbi
{
    public enum ErrorCode { NoError, Panic }
    public partial class Instrument
    { public System.Windows.Threading.Dispatcher Dispatcher = new System.Windows.Threading.Dispatcher(); }
}
namespace NinjaTrader.Data
{
    public class Row
    {
        public DateTime Time = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Unspecified);
        public double Close = 5000, Bid = 4999.75, Ask = 5000;
        public long Volume = 1;
    }
    public class Bars
    {
        public TradingHours TradingHours = new TradingHours();
        public List<Row> Rows = new List<Row>();
        public Action OnRead;
        public int Reads, ThrowAt = -1;
        public int Count { get { return Rows.Count; } }
        public DateTime GetTime(int i) { Reads++; OnRead?.Invoke(); if (i == ThrowAt) throw new Exception("row failure"); return Rows[i].Time; }
        public double GetClose(int i) { return Rows[i].Close; }
        public long GetVolume(int i) { return Rows[i].Volume; }
        public double GetBid(int i) { return Rows[i].Bid; }
        public double GetAsk(int i) { return Rows[i].Ask; }
    }
    public partial class BarsRequest
    {
        public static Action<BarsRequest> OnCreate;
        public static readonly List<BarsRequest> All = new List<BarsRequest>();
        public Bars Bars = new Bars();
        public Action<BarsRequest> OnRequest;
        public Action<NinjaTrader.Data.BarsRequest, NinjaTrader.Cbi.ErrorCode, string> Callback;
        public int Requests, Disposals;
        public bool InsideRequest, DisposedInsideRequest, ThrowDispose;
        public BarsRequest() { }
        public BarsRequest(NinjaTrader.Cbi.Instrument instrument, int count)
        { Instrument = instrument; BarsBack = count; ToLocal = new DateTime(2099, 12, 1); All.Add(this); OnCreate?.Invoke(this); }
        public void Request(Action<BarsRequest, NinjaTrader.Cbi.ErrorCode, string> callback)
        {
            Requests++; Callback = callback; InsideRequest = true;
            try { OnRequest?.Invoke(this); } finally { InsideRequest = false; }
        }
        public void Complete(NinjaTrader.Cbi.ErrorCode error = NinjaTrader.Cbi.ErrorCode.NoError)
        { Callback?.Invoke(this, error, "fixture"); }
        public void Dispose()
        { Disposals++; DisposedInsideRequest |= InsideRequest; if (ThrowDispose) throw new Exception("dispose failure"); }
    }
}
namespace NinjaTrader.NinjaScript
{
    public enum State { SetDefaults, Historical, Realtime, Terminated }
    public enum PrintTo { OutputTab1 }
}
namespace NinjaTrader.NinjaScript.Indicators
{
    public class Indicator
    {
        public string Name, Description;
        public bool IsOverlay, IsAutoScale, DisplayInDataBox, PaintPriceMarkers, IsSuspendedWhileInactive;
        public NinjaTrader.Cbi.Instrument Instrument = new NinjaTrader.Cbi.Instrument();
        public NinjaTrader.Data.Bars Bars = new NinjaTrader.Data.Bars();
        public State State;
        protected virtual void OnStateChange() { }
        public void SetState(State state) { State = state; OnStateChange(); }
    }
}
namespace NinjaTrader.Core
{
    public static class Globals { public static Options GeneralOptions = new Options(); }
    public class Options { public TimeZoneInfo TimeZoneInfo = TimeZoneInfo.Utc; }
}
namespace NinjaTrader.Code
{
    public static class Output
    {
        public static readonly List<string> Lines = new List<string>();
        public static void Process(string text, NinjaTrader.NinjaScript.PrintTo tab) { lock (Lines) Lines.Add(text); }
    }
}
