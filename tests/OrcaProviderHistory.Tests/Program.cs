using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Code;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;

static class Program
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    static bool Has(string text) { return Output.Lines.Any(s => s.Contains(text)); }
    static OrcaProviderHistoryProbe Create()
    {
        Output.Lines.Clear(); BarsRequest.All.Clear(); BarsRequest.OnCreate = null; DispatcherTimer.All.Clear();
        var probe = new OrcaProviderHistoryProbe();
        probe.Bars.TradingHours.Sessions.Add(new Session { BeginDay = DayOfWeek.Monday, EndDay = DayOfWeek.Monday,
            TradingDay = DayOfWeek.Monday, BeginTime = 90000, EndTime = 170000 });
        probe.SetState(State.SetDefaults); probe.SetState(State.Historical);
        Check(BarsRequest.All.Count == 0, "no request during defaults/historical replay");
        probe.SetState(State.Realtime);
        Check(BarsRequest.All.Count == 0, "request waits for instrument dispatcher");
        return probe;
    }
    static void Released(OrcaProviderHistoryProbe probe, BarsRequest request)
    {
        Check(request.Disposals == 1 && !request.DisposedInsideRequest, "owned request disposed once after Request returns");
        Check(probe.Instrument.Dispatcher.HookCount == 0 && DispatcherTimer.All.All(t => !t.Active), "timer and shutdown hook released");
        Check(Has("requestReleased=True") && Has("published=0; UTC-range-confirmed=false"), "cleanup and noncertification explicit");
        int lines = Output.Lines.Count;
        request.Complete(); probe.SetState(State.Terminated); probe.Instrument.Dispatcher.Drain();
        Check(request.Disposals == 1 && Output.Lines.Count == lines, "late callback/removal idempotent");
    }
    static void Main()
    {
        var probe = Create();
        BarsRequest.OnCreate = r => r.OnRequest = b => { b.Bars.Rows.Add(new Row()); b.Bars.Rows.Add(new Row()); b.Complete(); b.Complete(); };
        probe.Instrument.Dispatcher.Drain(); var request = BarsRequest.All.Single();
        Check(request.Requests == 1 && request.BarsBack == 1000 && request.LookupPolicy == LookupPolicies.Repository
            && request.MergePolicy == MergePolicy.DoNotMerge && request.BarsPeriod.BarsPeriodType == BarsPeriodType.Tick
            && request.BarsPeriod.Value == 1 && request.BarsPeriod.MarketDataType == MarketDataType.Last
            && !request.IsDividendAdjusted && !request.IsSplitAdjusted, "explicit bounded repository-only Last Tick1 request");
        Check(Has("returned=2 inspected=2") && Has("adjacentEqualTimes=1") && Has("volume=2") && Has("deduplicated=0"), "identical rows preserved");
        Check(Has("configurationUnchanged=True") && Has("reason=OBSERVED"), "successful synchronous completion observed");
        probe.SetState(State.Realtime); probe.Instrument.Dispatcher.Drain();
        Check(BarsRequest.All.Count == 1, "no automatic rerun"); Released(probe, request);

        foreach (bool timeout in new[] { false, true })
        {
            probe = Create(); var captured = probe;
            BarsRequest.OnCreate = r => r.OnRequest = b =>
            {
                if (timeout) DispatcherTimer.All.Single().Fire(); else { b.Complete(); captured.Instrument.Dispatcher.Drain(); }
                Check(b.Disposals == 0 && !Has("sample-summary"), "nested dispatcher must not inspect/dispose before Request returns");
            };
            probe.Instrument.Dispatcher.Drain(); request = BarsRequest.All.Single(); Released(probe, request);
        }

        probe = Create(); probe.Instrument.Dispatcher.Drain(); request = BarsRequest.All.Single();
        Task.Run(() => request.Complete()).GetAwaiter().GetResult();
        Check(request.Disposals == 0, "foreign callback only queues owned dispatcher work");
        probe.Instrument.Dispatcher.Drain(); Check(Has("empty=True") && Has("UTC-range-confirmed=false"), "empty is not coverage"); Released(probe, request);

        probe = Create(); BarsRequest.OnCreate = r => r.OnRequest = b =>
        { for (int i = 0; i < 1200; i++) b.Bars.Rows.Add(new Row()); b.Complete(); };
        probe.Instrument.Dispatcher.Drain(); request = BarsRequest.All.Single();
        Check(request.Bars.Reads == 1000 && Has("returned=1200 inspected=1000 capped=True"), "inspection capped even if platform overreturns");
        Check(Output.Lines.Count(s => s.Contains(": sample index=")) == 3, "sample output bounded"); Released(probe, request);

        probe = Create(); BarsRequest.OnCreate = r => r.OnRequest = b =>
        {
            b.Bars.Rows.Add(new Row { Time = new DateTime(2026, 9, 10, 12, 0, 1, DateTimeKind.Utc), Volume = long.MaxValue });
            b.Bars.Rows.Add(new Row { Time = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Local), Volume = long.MaxValue, Bid = 0 });
            b.Bars.Rows.Add(new Row { Close = double.NaN, Volume = -1, Ask = double.PositiveInfinity }); b.Complete();
        };
        probe.Instrument.Dispatcher.Drain(); request = BarsRequest.All.Single();
        Check(Has("kindsUTC/local/unspecified=1/1/1") && Has("backwards=1") && Has("invalidRows=1") && Has("positiveOrderedQuotePairs=1"),
            "timestamp kinds, backwards rows and invalid data remain visible");
        Check(Has("volume=18446744073709551613"), "volume sum does not overflow long"); Released(probe, request);

        probe = Create(); probe.SetState(State.Terminated); probe.Instrument.Dispatcher.Drain();
        Check(BarsRequest.All.Count == 0 && probe.Instrument.Dispatcher.HookCount == 0, "removal before queued start does not request");

        foreach (bool shutdown in new[] { false, true })
        {
            probe = Create(); var captured = probe;
            BarsRequest.OnCreate = r => r.OnRequest = b => { if (shutdown) captured.Instrument.Dispatcher.Shutdown(); else captured.SetState(State.Terminated); b.Complete(); };
            probe.Instrument.Dispatcher.Drain(); request = BarsRequest.All.Single();
            Check(!Has("sample-summary") && Has("CANCELLED_DURING_REQUEST"), "reentrant cancellation prevents inspection"); Released(probe, request);
        }
        foreach (string kind in new[] { "timeout", "remove", "shutdown", "error", "queued-remove", "mutate", "foreign", "read", "read-remove", "queue" })
        {
            probe = Create(); probe.Instrument.Dispatcher.Drain(); request = BarsRequest.All.Single();
            if (kind == "timeout") DispatcherTimer.All.Single().Fire();
            else if (kind == "remove") probe.SetState(State.Terminated);
            else if (kind == "shutdown") probe.Instrument.Dispatcher.Shutdown();
            else if (kind == "error") request.Complete(ErrorCode.Panic);
            else if (kind == "mutate") { request.LookupPolicy = LookupPolicies.Provider; request.Complete(); }
            else if (kind == "foreign") request.Callback(new BarsRequest(), ErrorCode.NoError, "foreign");
            else if (kind == "read") { request.Bars.Rows.Add(new Row()); request.Bars.ThrowAt = 0; request.Complete(); }
            else if (kind == "read-remove")
            { var captured = probe; request.Bars.Rows.Add(new Row()); request.Bars.OnRead = () => captured.SetState(State.Terminated); request.Complete(); }
            else if (kind == "queue")
            { probe.Instrument.Dispatcher.ThrowQueue = true; request.Complete(); probe.Instrument.Dispatcher.ThrowQueue = false; DispatcherTimer.All.Single().Fire(); }
            else { request.Complete(); probe.SetState(State.Terminated); } // cancellation wins over queued completion
            probe.Instrument.Dispatcher.Drain();
            Check(!Has("configurationUnchanged=True"), "incomplete outcome cannot claim successful observation: " + kind); Released(probe, request);
        }
        probe = Create(); BarsRequest.OnCreate = r => r.OnRequest = b => { b.Complete(); throw new Exception("after callback"); };
        probe.Instrument.Dispatcher.Drain(); request = BarsRequest.All.Single();
        Check(Has("REQUEST_FAILED") && !Has("sample-summary"), "Request throws after callback and cleans before queued completion"); Released(probe, request);

        probe = Create(); probe.Instrument.Dispatcher.Drain(); request = BarsRequest.All.Single(); request.ThrowDispose = true;
        DispatcherTimer.All.Single().Fire();
        Check(Has("DISPOSE_FAILED") && Has("requestReleased=False") && probe.Instrument.Dispatcher.HookCount == 1, "failed disposal visible and retry hook retained");
        request.ThrowDispose = false; probe.SetState(State.Terminated); probe.Instrument.Dispatcher.Drain();
        Check(request.Disposals == 2 && Has("requestReleased=True") && probe.Instrument.Dispatcher.HookCount == 0, "removal retries failed disposal");

        probe = Create(); probe.Instrument.MasterInstrument.InstrumentType = InstrumentType.Stock; probe.Instrument.Dispatcher.Drain();
        request = BarsRequest.All.Single(); Check(request.Requests == 0 && Has("REQUEST_FAILED"), "unsupported instrument rejected before Request"); Released(probe, request);
        Console.WriteLine("PASS: " + checks + " linked history-observer checks. Actual platform scheduling and repository results remain unverified.");
    }
}
