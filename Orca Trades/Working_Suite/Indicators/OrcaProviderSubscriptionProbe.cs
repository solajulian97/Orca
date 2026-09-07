using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Observation only. This indicator never publishes into an Orca provider.
    public class OrcaProviderSubscriptionProbe : Indicator
    {
        private readonly object lifecycleSync = new object();
        private Observation observation;
        private bool terminated;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "OrcaProviderSubscriptionProbe";
                Description = "Five-second subscription-boundary observation. No provider publication or trading.";
                IsOverlay = true; IsAutoScale = false; DisplayInDataBox = false;
                PaintPriceMarkers = false; IsSuspendedWhileInactive = false;
                return;
            }
            lock (lifecycleSync)
            {
                if (State == State.Terminated)
                {
                    terminated = true;
                    if (observation != null) observation.Dispose();
                    observation = null;
                }
                else if (State == State.Realtime && !terminated && observation == null)
                {
                    observation = new Observation(Instrument);
                    observation.Start();
                }
            }
        }

        // Owns only its instrument callback and dispatcher cleanup hooks, not its chart.
        private sealed class Observation : IDisposable
        {
            private readonly string id = Guid.NewGuid().ToString("N");
            private readonly Instrument instrument;
            private readonly Dispatcher dispatcher;
            private readonly object sampleSync = new object();
            private readonly List<string> firstCallbacks = new List<string>(8);
            private DispatcherTimer timer;
            private int stopRequested, stage;
            private bool attachmentAttempted, shutdownHooked, cleaned;
            private long duringAdd, afterAdd, last, reset, started;

            internal Observation(Instrument instrument)
            {
                this.instrument = instrument;
                dispatcher = instrument == null ? null : instrument.Dispatcher;
            }
            private void Output(string text)
            {
                NinjaTrader.Code.Output.Process("OrcaProviderSubscriptionProbe " + id + ": " + text, PrintTo.OutputTab1);
            }
            internal void Start()
            {
                if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                { Output("UNAVAILABLE instrument dispatcher; nothing subscribed."); return; }
                try { dispatcher.InvokeAsync(Attach); }
                catch (Exception ex) { Output("ATTACH_QUEUE_FAILED " + ex.Message); }
            }
            private void Attach()
            {
                if (Volatile.Read(ref stopRequested) != 0 || dispatcher.HasShutdownStarted) return;
                try
                {
                    dispatcher.ShutdownStarted += OnShutdown;
                    shutdownHooked = true;
                    started = Stopwatch.GetTimestamp();
                    Output("starting observation; instrument=" + instrument.FullName + "; no provider publication; snapshot provenance is unverified");
                    Volatile.Write(ref stage, 1);
                    attachmentAttempted = true;
                    instrument.MarketData.Update += OnData;
                    Volatile.Write(ref stage, 2);
                    if (Volatile.Read(ref stopRequested) != 0) { Cleanup("removed during attachment"); return; }
                    timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher);
                    timer.Interval = TimeSpan.FromSeconds(5);
                    timer.Tick += OnTimer;
                    timer.Start();
                }
                catch (Exception ex)
                {
                    Volatile.Write(ref stage, 2);
                    Output("ATTACH_FAILED " + ex.Message);
                    Cleanup("attachment failure");
                }
            }
            private void OnData(object sender, MarketDataEventArgs e)
            {
                if (e == null || Volatile.Read(ref stopRequested) != 0) return;
                int callbackStage = Volatile.Read(ref stage);
                lock (sampleSync)
                {
                    if (Volatile.Read(ref stopRequested) != 0) return;
                    if (callbackStage == 1) duringAdd++; else afterAdd++;
                    if (e.MarketDataType == MarketDataType.Last) last++;
                    if (e.IsReset) reset++;
                    if (firstCallbacks.Count < 8)
                        firstCallbacks.Add("type=" + e.MarketDataType + " phase=" + (callbackStage == 1 ? "during-add" : "after-add")
                            + " time=" + e.Time.ToString("O", CultureInfo.InvariantCulture) + " kind=" + e.Time.Kind
                            + " reset=" + e.IsReset + " volume=" + e.Volume.ToString(CultureInfo.InvariantCulture));
                }
            }
            private void OnTimer(object sender, EventArgs e) { Cleanup("observation complete"); }
            private void OnShutdown(object sender, EventArgs e)
            {
                Interlocked.Exchange(ref stopRequested, 1);
                if (Volatile.Read(ref stage) != 1) Cleanup("dispatcher shutdown");
            }
            private void Cleanup(string reason)
            {
                // Attach, timer, shutdown and queued removal all execute on the instrument dispatcher.
                if (cleaned) return;
                Interlocked.Exchange(ref stopRequested, 1);
                if (timer != null) { timer.Stop(); timer.Tick -= OnTimer; timer = null; }
                bool detachSucceeded = !attachmentAttempted;
                try
                {
                    // A throwing add accessor may have installed our handler before throwing.
                    // Attempt removal of that exact handler and surface errors, never claim success silently.
                    if (attachmentAttempted) { instrument.MarketData.Update -= OnData; attachmentAttempted = false; }
                    detachSucceeded = true;
                }
                catch (Exception ex) { Output("DETACH_FAILED " + ex.Message); }
                if (shutdownHooked && detachSucceeded) { dispatcher.ShutdownStarted -= OnShutdown; shutdownHooked = false; }
                cleaned = detachSucceeded;
                lock (sampleSync)
                {
                    Output("summary reason=" + reason + " detached=" + detachSucceeded + " callbacksDuringAdd=" + duringAdd
                        + " callbacksAfterAdd=" + afterAdd + " lastCallbacks=" + last + " resetCallbacks=" + reset
                        + " elapsed=" + (started == 0 ? 0 : (Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency).ToString("F2", CultureInfo.InvariantCulture)
                        + "s; published=0; after-add does NOT prove a fresh trade");
                    foreach (string item in firstCallbacks) Output(item);
                    firstCallbacks.Clear();
                }
            }
            public void Dispose()
            {
                Interlocked.Exchange(ref stopRequested, 1);
                if (dispatcher == null) return;
                if (dispatcher.CheckAccess())
                {
                    // A reentrant snapshot callback may remove the indicator before += returns.
                    // Defer cleanup to Attach so an add accessor cannot install a handler after removal.
                    if (Volatile.Read(ref stage) != 1) Cleanup("indicator removed");
                    return;
                }
                // If shutdown has started, its registered handler performs cleanup on that dispatcher.
                if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;
                try { dispatcher.InvokeAsync(() => Cleanup("indicator removed")); }
                catch (Exception ex) { Output("DETACH_QUEUE_FAILED " + ex.Message); }
            }
        }
    }
}
