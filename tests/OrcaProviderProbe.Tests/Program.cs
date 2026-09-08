using System;
using System.Linq;
using System.Reflection;
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
        Check(probe.Output.Last().Contains("live=2") && probe.Output.Last().Contains("verified=3"), "final live tail drained and printed");
        Check(owner.ActiveStreams == 0, "termination releases owner");
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
            Check(Field<bool>(probe, "faulted") && owner.ActiveStreams == 0, "every owner-lifetime connection notification closes continuity");
            probe.SetState(State.Realtime); probe.Emit(new MarketDataEventArgs()); probe.Connection(ConnectionStatus.Connected);
            Check(Field<long>(probe, "liveCount") == 0 && probe.Output.Count(s => s.Contains(": FAULT ")) == 1, "no reconnect resurrection or repeated fault");
            probe.SetState(State.Terminated);
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
        Console.WriteLine("PASS: " + checks + " linked-probe checks; platform callback scheduling remains unverified.");
    }
}
