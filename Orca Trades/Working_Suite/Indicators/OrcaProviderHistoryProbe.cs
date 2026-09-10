using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Manually installed, repository-only observation. Never publishes or certifies history.
    public class OrcaProviderHistoryProbe : Indicator
    {
        private readonly object lifecycleSync = new object();
        private Observation observation;
        private bool terminated;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "OrcaProviderHistoryProbe";
                Description = "One repository-only 1000 Tick-1 history observation. No publication or trading.";
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
                    // Retain the owner for a possible repeated cleanup after Dispose failure.
                }
                else if (State == State.Realtime && !terminated && observation == null)
                {
                    observation = new Observation(Instrument, Bars == null ? null : Bars.TradingHours,
                        NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo);
                    observation.Start();
                }
            }
        }

        // No chart/indicator reference. Request, inspection and disposal are dispatcher-owned.
        private sealed class Observation : IDisposable
        {
            private const int SampleLimit = 1000;
            private readonly string id = Guid.NewGuid().ToString("N");
            private readonly Dispatcher dispatcher;
            private readonly TimeZoneInfo eventTimezone, localTimezone;
            private Instrument instrument;
            private TradingHours hours;
            private BarsRequest request;
            private BarsRequest deferredCompletion;
            private ErrorCode deferredError;
            private string deferredMessage;
            private OrcaProviderHistoryConfigurationCapture configuration;
            private DispatcherTimer timer;
            private int stopRequested, completionQueued;
            private bool issuing, inspecting, completionDeferred, hooked, cleaned;
            private long started;

            internal Observation(Instrument instrument, TradingHours hours, TimeZoneInfo eventTimezone)
            {
                this.instrument = instrument; this.hours = hours; this.eventTimezone = eventTimezone;
                localTimezone = TimeZoneInfo.Local;
                dispatcher = instrument == null ? null : instrument.Dispatcher;
            }
            private void Output(string value)
            { NinjaTrader.Code.Output.Process("OrcaProviderHistoryProbe " + id + ": " + value, PrintTo.OutputTab1); }
            private static string Brief(string value)
            {
                value = (value ?? "").Replace('\r', ' ').Replace('\n', ' ');
                return value.Length <= 240 ? value : value.Substring(0, 240);
            }
            internal void Start()
            {
                if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                { Output("UNAVAILABLE instrument dispatcher; requested=0; published=0"); instrument = null; hours = null; return; }
                try { dispatcher.InvokeAsync(Begin); }
                catch (Exception ex) { Output("START_QUEUE_FAILED " + Brief(ex.Message)); instrument = null; hours = null; }
            }
            private void Begin()
            {
                if (Volatile.Read(ref stopRequested) != 0 || dispatcher.HasShutdownStarted)
                { Cleanup("CANCELLED_BEFORE_REQUEST"); return; }
                try
                {
                    dispatcher.ShutdownStarted += OnShutdown; hooked = true;
                    started = Stopwatch.GetTimestamp();
                    request = new BarsRequest(instrument, SampleLimit);
                    request.BarsPeriod = new BarsPeriod { BarsPeriodType = BarsPeriodType.Tick, Value = 1, MarketDataType = MarketDataType.Last };
                    request.TradingHours = hours;
                    request.MergePolicy = MergePolicy.DoNotMerge;
                    request.LookupPolicy = LookupPolicies.Repository;
                    request.IsDividendAdjusted = false; request.IsSplitAdjusted = false;
                    request.IsResetOnNewTradingDay = true;
                    configuration = OrcaProviderHistoryConfigurationCapture.Capture(request, eventTimezone, localTimezone);
                    Output("starting requested=1000 Last-Tick-1; instrument=" + Brief(configuration.FullContract)
                        + "; lookup=Repository; merge=DoNotMerge; timeout=30s; eventClock=" + Brief(eventTimezone.Id)
                        + "; requestClock=" + Brief(localTimezone.Id) + "; published=0; UTC-range-confirmed=false");
                    timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher);
                    timer.Interval = TimeSpan.FromSeconds(30); timer.Tick += OnTimeout; timer.Start();
                    if (Volatile.Read(ref stopRequested) != 0) { Cleanup("CANCELLED_BEFORE_REQUEST"); return; }
                    // Never hold a callback lock around platform Request. Always queue completion,
                    // including synchronous callbacks, so disposal cannot race its setup accessor.
                    issuing = true;
                    try { request.Request(OnCompleted); }
                    finally { issuing = false; }
                    if (Volatile.Read(ref stopRequested) != 0) Cleanup("CANCELLED_DURING_REQUEST");
                    else if (completionDeferred) Complete(deferredCompletion, deferredError, deferredMessage);
                }
                catch (Exception ex) { Output("REQUEST_FAILED " + Brief(ex.Message)); Cleanup("REQUEST_FAILED"); }
            }
            private void OnCompleted(BarsRequest completed, ErrorCode error, string message)
            {
                if (Volatile.Read(ref stopRequested) != 0 || Interlocked.Exchange(ref completionQueued, 1) != 0) return;
                string detail = Brief(message);
                try { dispatcher.InvokeAsync(() => Complete(completed, error, detail)); }
                catch (Exception ex)
                {
                    // The registered shutdown hook or timeout owns disposal, never this arbitrary callback thread.
                    Output("COMPLETION_QUEUE_FAILED " + Brief(ex.Message));
                }
            }
            private void Complete(BarsRequest completed, ErrorCode error, string message)
            {
                if (Volatile.Read(ref stopRequested) != 0 || cleaned) return;
                // A platform call may pump a nested dispatcher frame. Queuing alone
                // is not proof that Request has returned; retain this one completion.
                if (issuing)
                {
                    deferredCompletion = completed; deferredError = error; deferredMessage = message;
                    completionDeferred = true; return;
                }
                string outcome = "ERROR";
                inspecting = true;
                try
                {
                    if (!ReferenceEquals(completed, request)) throw new InvalidOperationException("Completion did not identify the owned request.");
                    if (error != ErrorCode.NoError) throw new InvalidOperationException("Platform error=" + error + "; " + message);
                    configuration.RequireUnchanged(request, eventTimezone, localTimezone);
                    if (request.Bars == null) throw new InvalidOperationException("No Bars result.");
                    Inspect(request.Bars);
                    configuration.RequireUnchanged(request, eventTimezone, localTimezone);
                    if (Volatile.Read(ref stopRequested) != 0) throw new OperationCanceledException("Removed during inspection.");
                    outcome = "OBSERVED";
                    Output("configurationUnchanged=True; observation-only; final bar may be developing; quote provenance and atomic snapshot unverified");
                }
                catch (Exception ex) { Output("OBSERVATION_FAILED " + Brief(ex.Message)); }
                finally { inspecting = false; Cleanup(outcome); }
            }
            private void Inspect(Bars bars)
            {
                int returned = bars.Count, count = Math.Min(SampleLimit, Math.Max(0, returned));
                if (returned < 0) throw new InvalidOperationException("Negative Bars.Count.");
                int utc = 0, local = 0, unspecified = 0, equalTimes = 0, backwards = 0, invalid = 0, quotePairs = 0;
                decimal volume = 0;
                DateTime first = DateTime.MinValue, last = DateTime.MinValue;
                for (int i = 0; i < count; i++)
                {
                    if (Volatile.Read(ref stopRequested) != 0) throw new OperationCanceledException("Removed during inspection.");
                    DateTime time = bars.GetTime(i);
                    double price = bars.GetClose(i); long size = bars.GetVolume(i);
                    if (i == 0) first = time;
                    else { if (time.Ticks == last.Ticks) equalTimes++; if (time.Ticks < last.Ticks) backwards++; }
                    last = time;
                    if (time.Kind == DateTimeKind.Utc) utc++; else if (time.Kind == DateTimeKind.Local) local++; else unspecified++;
                    if (time == DateTime.MinValue || !Finite(price) || size < 0) invalid++;
                    volume += size;
                    // Availability only. Positive ordered values may still be substituted quotes.
                    double bid = bars.GetBid(i), ask = bars.GetAsk(i);
                    if (Finite(bid) && Finite(ask) && bid > 0 && ask >= bid) quotePairs++;
                    if (i < 3) Output("sample index=" + i + " time=" + time.ToString("O", CultureInfo.InvariantCulture)
                        + " kind=" + time.Kind + " close=" + price.ToString("R", CultureInfo.InvariantCulture)
                        + " volume=" + size.ToString(CultureInfo.InvariantCulture));
                }
                Output("sample-summary returned=" + returned + " inspected=" + count + " capped=" + (returned > count)
                    + " countUnchanged=" + (returned == bars.Count) + " empty=" + (count == 0)
                    + " volume=" + volume.ToString(CultureInfo.InvariantCulture) + " invalidRows=" + invalid
                    + " kindsUTC/local/unspecified=" + utc + "/" + local + "/" + unspecified
                    + " adjacentEqualTimes=" + equalTimes + " backwards=" + backwards + " positiveOrderedQuotePairs=" + quotePairs
                    + " first=" + first.ToString("O", CultureInfo.InvariantCulture) + " last=" + last.ToString("O", CultureInfo.InvariantCulture)
                    + "; deduplicated=0; published=0; UTC-range-confirmed=false");
            }
            private static bool Finite(double value)
            { return !double.IsNaN(value) && !double.IsInfinity(value) && value != double.MinValue && value != double.MaxValue; }
            private void OnTimeout(object sender, EventArgs e)
            { Interlocked.Exchange(ref stopRequested, 1); if (!issuing && !inspecting) Cleanup("TIMEOUT"); }
            private void OnShutdown(object sender, EventArgs e)
            { Interlocked.Exchange(ref stopRequested, 1); if (!issuing && !inspecting) Cleanup("DISPATCHER_SHUTDOWN"); }
            private void Cleanup(string reason)
            {
                if (cleaned) return;
                Interlocked.Exchange(ref stopRequested, 1);
                if (timer != null) { timer.Stop(); timer.Tick -= OnTimeout; timer = null; }
                bool released = request == null;
                try { if (request != null) { request.Dispose(); request = null; } released = true; }
                catch (Exception ex) { Output("DISPOSE_FAILED " + Brief(ex.Message)); }
                if (released)
                {
                    if (hooked) { dispatcher.ShutdownStarted -= OnShutdown; hooked = false; }
                    configuration = null; instrument = null; hours = null;
                    deferredCompletion = null; deferredMessage = null; completionDeferred = false; cleaned = true;
                }
                Output("cleanup reason=" + reason + " requestReleased=" + released
                    + " elapsed=" + (started == 0 ? 0 : (Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency).ToString("F2", CultureInfo.InvariantCulture)
                    + "s; published=0; UTC-range-confirmed=false");
            }
            public void Dispose()
            {
                Interlocked.Exchange(ref stopRequested, 1);
                if (dispatcher == null) return;
                if (dispatcher.CheckAccess()) { if (!issuing && !inspecting) Cleanup("INDICATOR_REMOVED"); return; }
                if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;
                try { dispatcher.InvokeAsync(() => Cleanup("INDICATOR_REMOVED")); }
                catch (Exception ex) { Output("CLEANUP_QUEUE_FAILED " + Brief(ex.Message)); }
            }
        }
    }
}
