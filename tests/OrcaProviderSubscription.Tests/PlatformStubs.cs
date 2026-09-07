using System;
using System.Collections.Generic;

namespace System.Windows.Threading
{
    public enum DispatcherPriority { Background }
    public class Dispatcher
    {
        public bool HasShutdownStarted, HasShutdownFinished;
        public bool OnDispatcher;
        public event EventHandler ShutdownStarted;
        private readonly Queue<Action> queue = new Queue<Action>();
        public void InvokeAsync(Action action) { queue.Enqueue(action); }
        public bool CheckAccess() { return OnDispatcher; }
        public void Drain() { OnDispatcher = true; try { while (queue.Count > 0) queue.Dequeue()(); } finally { OnDispatcher = false; } }
        public void Shutdown() { OnDispatcher = true; HasShutdownStarted = true; ShutdownStarted?.Invoke(this, EventArgs.Empty); HasShutdownFinished = true; OnDispatcher = false; }
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
        public void Fire() { if (Active) { dispatcher.OnDispatcher = true; Tick?.Invoke(this, EventArgs.Empty); dispatcher.OnDispatcher = false; } }
    }
}
namespace NinjaTrader.Cbi
{
    public class Instrument
    {
        public string FullName = "ES SEP26";
        public System.Windows.Threading.Dispatcher Dispatcher = new System.Windows.Threading.Dispatcher();
        public NinjaTrader.Data.MarketData MarketData = new NinjaTrader.Data.MarketData();
    }
}
namespace NinjaTrader.Data
{
    public enum MarketDataType { Last, Bid, Ask }
    public class MarketDataEventArgs : EventArgs { public MarketDataType MarketDataType; public DateTime Time = DateTime.UtcNow; public bool IsReset; public long Volume = 1; }
    public class MarketData
    {
        private EventHandler<MarketDataEventArgs> handlers;
        public Action OnAdd;
        public bool ThrowAfterAdd, ThrowOnRemove, NotifyBeforeAdd;
        public int Count { get { return handlers == null ? 0 : handlers.GetInvocationList().Length; } }
        public event EventHandler<MarketDataEventArgs> Update
        {
            add { if (NotifyBeforeAdd) OnAdd?.Invoke(); handlers += value; if (!NotifyBeforeAdd) OnAdd?.Invoke(); if (ThrowAfterAdd) throw new Exception("injected add failure"); }
            remove { if (ThrowOnRemove) throw new Exception("injected remove failure"); handlers -= value; }
        }
        public void Emit() { handlers?.Invoke(this, new MarketDataEventArgs()); }
    }
}
namespace NinjaTrader.NinjaScript
{
    public enum State { SetDefaults, Realtime, Terminated }
    public enum PrintTo { OutputTab1 }
}
namespace NinjaTrader.NinjaScript.Indicators
{
    public class Indicator
    {
        public string Name, Description;
        public bool IsOverlay, IsAutoScale, DisplayInDataBox, PaintPriceMarkers, IsSuspendedWhileInactive;
        public NinjaTrader.Cbi.Instrument Instrument;
        public State State;
        protected virtual void OnStateChange() { }
        public void SetState(State state) { State = state; OnStateChange(); }
    }
}
namespace NinjaTrader.Code
{
    public static class Output
    {
        public static readonly List<string> Lines = new List<string>();
        public static void Process(string text, NinjaTrader.NinjaScript.PrintTo tab) { Lines.Add(text); }
    }
}
