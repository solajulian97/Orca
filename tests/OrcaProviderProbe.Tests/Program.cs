using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;

static class Program
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    static T Field<T>(OrcaProviderProbe probe, string name)
    { return (T)typeof(OrcaProviderProbe).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(probe); }
    static OrcaProviderProbe Start(bool replay = true)
    {
        var probe = new OrcaProviderProbe();
        probe.Bars.IsTickReplay = replay;
        probe.Bars.TradingHours.Sessions.Add(new Session { BeginDay = DayOfWeek.Monday, EndDay = DayOfWeek.Monday, TradingDay = DayOfWeek.Monday, BeginTime = 90000, EndTime = 170000 });
        probe.SetState(State.SetDefaults);
        probe.SetState(State.DataLoaded);
        probe.SetState(State.Historical);
        Check(!Field<bool>(probe, "faulted"), "setup must succeed");
        return probe;
    }
    static void Main()
    {
        var probe = Start();
        probe.Emit(new MarketDataEventArgs());
        probe.SetState(State.Transition);
        probe.SetState(State.Realtime);
        probe.Emit(new MarketDataEventArgs()); // Identical values are separate callbacks, not deduplicated.
        probe.Emit(new MarketDataEventArgs());
        Check(Field<long>(probe, "historicalCount") == 1 && Field<long>(probe, "liveCount") == 2, "preserve identical boundary events");
        Check(probe.Output.Any(s => s.Contains("live=1") && s.Contains("readerComparison=PASS")), "first live evidence printed without diagnostics");
        var owner = Field<OrcaProviderFeedLifetime>(probe, "feedLifetime");
        probe.SetState(State.Terminated);
        Check(probe.Output.Any(s => s.Contains("live=2") && s.Contains("verified=3")), "final live tail drained and printed");
        Check(probe.Output.Last().Contains("connection-monitor-detached=True"), "normal cleanup reports owned handler removal");
        Check(owner.ActiveStreams == 0, "termination releases owner");
        Check(Connection.HandlerCount == 0, "termination detaches direct handler");
        int lines = probe.Output.Count;
        probe.SetState(State.Terminated); probe.Emit(new MarketDataEventArgs()); probe.Connection(ConnectionStatus.Connected);
        Check(lines == probe.Output.Count, "termination idempotent and late callbacks ignored");

        foreach (State phase in new[] { State.DataLoaded, State.Historical, State.Transition, State.Realtime })
        foreach (ConnectionStatus status in Enum.GetValues(typeof(ConnectionStatus)))
        {
            probe = Start();
            if (phase == State.Realtime) probe.SetState(phase); else probe.State = phase;
            owner = Field<OrcaProviderFeedLifetime>(probe, "feedLifetime");
            probe.Connection(status);
            Check(!Field<bool>(probe, "faulted") && owner.ActiveStreams == 1, "script notification does not impersonate direct lifetime change");
            Connection.Emit(status);
            Check(owner.ActiveStreams == 0, "direct notification closes core without waiting for a chart callback");
            probe.SetState(State.Realtime); probe.Emit(new MarketDataEventArgs()); probe.Connection(ConnectionStatus.Connected);
            Check(Field<long>(probe, "liveCount") == 0 && probe.Output.Count(s => s.Contains(": FAULT ")) == 1, "no reconnect resurrection or repeated fault");
            probe.SetState(State.Terminated);
            Check(Connection.HandlerCount == 0, "fault path detaches direct handler");
        }
        foreach (MarketDataType type in Enum.GetValues(typeof(MarketDataType)))
        {
            probe = Start(); probe.SetState(State.Realtime);
            probe.Emit(new MarketDataEventArgs { MarketDataType = type, IsReset = true });
            Check(Field<bool>(probe, "faulted") && Field<long>(probe, "liveCount") == 0, "reset of any type is not a trade");
            probe.SetState(State.Terminated);
        }
        probe = Start(); probe.SetState(State.Transition); probe.Emit(new MarketDataEventArgs());
        Check(Field<bool>(probe, "faulted") && Field<long>(probe, "historicalCount") == 0, "transition Last cannot disappear silently");
        probe.SetState(State.Terminated);
        probe = Start(false); probe.Emit(new MarketDataEventArgs()); probe.SetState(State.Realtime);
        probe.Emit(new MarketDataEventArgs { MarketDataType = MarketDataType.Bid });
        probe.Emit(new MarketDataEventArgs());
        Check(Field<long>(probe, "historicalCount") == 0 && Field<long>(probe, "liveCount") == 1, "live-only ignores historical and quote callbacks");
        probe.SetState(State.Terminated);
        probe = Start(false); probe.SetState(State.Realtime);
        for (int i = 0; i < 20; i++) probe.Connection(ConnectionStatus.Connecting);
        Check(probe.Output.Count(s => s.Contains("connection-observation")) == 8, "script observations bounded");
        Check(!Field<bool>(probe, "faulted"), "script notification history alone does not invalidate direct guard");
        probe.Emit(new MarketDataEventArgs());
        Check(Field<long>(probe, "liveCount") == 1, "live input continues after script notifications when direct lifetime is unchanged");
        probe.SetState(State.Terminated);
        MonitorChecks();
        Check(Connection.HandlerCount == 0, "all monitor tests release subscriptions");
        Console.WriteLine("PASS: " + checks + " linked-probe checks; platform callback scheduling remains unverified.");
    }

    static OrcaProviderFeedLifetime Owner()
    { return new OrcaProviderFeedLifetime(OrcaProviderEnvironment.LiveFeed, 1, 8, 2, 2, 2); }

    static void MonitorChecks()
    {
        var owner = Owner(); var monitor = new OrcaProviderConnectionMonitor(owner);
        Check(!monitor.IsArmed, "unattached monitor cannot admit");
        monitor.Attach(); Check(monitor.IsArmed && Connection.HandlerCount == 1, "exactly one handler attached");
        bool rejected = false;
        try { monitor.Attach(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && Connection.HandlerCount == 1, "duplicate attach rejected");
        var captured = Connection.CaptureHandlers();
        monitor.Dispose(); monitor.Dispose();
        Check(!monitor.IsArmed && Connection.HandlerCount == 0, "monitor cleanup idempotent");
        rejected = false; try { monitor.Attach(); } catch (ObjectDisposedException) { rejected = true; }
        Check(rejected, "disposed monitor cannot reattach");
        using (var replacement = new OrcaProviderConnectionMonitor(Owner()))
        {
            replacement.Attach();
            captured(null, new ConnectionStatusEventArgs());
            Check(!replacement.IsInvalidated, "queued old handler cannot invalidate replacement");
        }

        monitor = new OrcaProviderConnectionMonitor(Owner());
        Connection.DuringAdd = () => Connection.Emit(ConnectionStatus.Connected);
        try { monitor.Attach(); } finally { Connection.DuringAdd = null; }
        Check(monitor.IsInvalidated, "notification during attachment fails closed");
        monitor.Dispose();

        Connection.ThrowAfterAdd = true;
        var failedProbe = new OrcaProviderProbe();
        failedProbe.Bars.TradingHours.Sessions.Add(new Session());
        try { failedProbe.SetState(State.DataLoaded); } finally { Connection.ThrowAfterAdd = false; }
        Check(Field<bool>(failedProbe, "faulted") && Connection.HandlerCount == 0, "partial add failure closes and detaches stored monitor");
        failedProbe.SetState(State.Terminated);

        monitor = new OrcaProviderConnectionMonitor(Owner()); monitor.Attach();
        Connection.ThrowOnRemove = true;
        rejected = false; try { monitor.Dispose(); } catch (InvalidOperationException) { rejected = true; }
        finally { Connection.ThrowOnRemove = false; }
        Check(rejected && !monitor.IsArmed && Connection.HandlerCount == 1, "detach failure reported without admission");
        monitor.Dispose(); Check(Connection.HandlerCount == 0, "detach failure can retry");

        var cleanupProbe = Start(false); cleanupProbe.SetState(State.Realtime);
        Connection.ThrowOnRemove = true;
        try { cleanupProbe.SetState(State.Terminated); } finally { Connection.ThrowOnRemove = false; }
        Check(cleanupProbe.Output.Any(s => s.Contains("CONNECTION_MONITOR_DETACH_FAILED"))
            && Connection.HandlerCount == 1, "probe exposes failed cleanup and retains retry handle");
        cleanupProbe.SetState(State.Terminated);
        Check(Connection.HandlerCount == 0, "repeated probe removal retries exact failed handler");

        using (var first = new OrcaProviderConnectionMonitor(Owner()))
        using (var second = new OrcaProviderConnectionMonitor(Owner()))
        {
            first.Attach(); second.Attach(); first.Dispose();
            Check(Connection.HandlerCount == 1 && second.IsArmed, "one monitor disposal preserves another handler");
            Connection.CaptureHandlers()(null, null);
            Check(second.IsInvalidated, "unclassified direct notification fails closed");
        }

        using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
        {
            monitor = new OrcaProviderConnectionMonitor(Owner());
            Connection.DuringAdd = () => { entered.Set(); if (!release.Wait(3000)) throw new TimeoutException(); };
            var attach = Task.Run(() => monitor.Attach());
            if (!entered.Wait(3000)) throw new TimeoutException();
            var dispose = Task.Run(() => monitor.Dispose());
            release.Set();
            try { Task.WaitAll(attach, dispose); } finally { Connection.DuringAdd = null; }
            Check(Connection.HandlerCount == 0 && !monitor.IsArmed, "dispose racing attach leaves no handler");
        }

        var probe = Start(false); probe.SetState(State.Realtime);
        Task.Run(() => Connection.Emit(ConnectionStatus.Connecting)).GetAwaiter().GetResult();
        Check(!Field<bool>(probe, "faulted") && Field<OrcaProviderFeedLifetime>(probe, "feedLifetime").ActiveStreams == 0,
            "direct callback closes core without calling chart logging/locking");
        probe.SetState(State.Terminated);
        Check(probe.Output.Any(s => s.Contains("Direct platform connection notification"))
            && !probe.Output.Last().Contains("readerComparison=PASS"), "removal reports quiet-feed invalidation, not successful tail");

        for (int i = 0; i < 10; i++)
        {
            probe = Start(false); probe.SetState(State.Realtime);
            Parallel.Invoke(() => probe.Emit(new MarketDataEventArgs()), () => Connection.Emit(ConnectionStatus.Disconnected));
            long accepted = Field<long>(probe, "liveCount");
            probe.Emit(new MarketDataEventArgs());
            Check(Field<long>(probe, "liveCount") == accepted && Field<bool>(probe, "faulted"), "concurrent invalidation rejects subsequent input");
            probe.SetState(State.Terminated);
        }
    }
}
