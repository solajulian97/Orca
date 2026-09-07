using System;
using System.Linq;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Code;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;

class Program
{
    static int checks;
    static void Check(bool value, string text) { if (!value) throw new Exception(text); checks++; }
    static OrcaProviderSubscriptionProbe Create(Instrument instrument)
    { Output.Lines.Clear(); var probe = new OrcaProviderSubscriptionProbe { Instrument = instrument }; probe.SetState(State.Realtime); return probe; }
    static void Main()
    {
        var instrument = new Instrument();
        instrument.MarketData.OnAdd = instrument.MarketData.Emit;
        var probe = Create(instrument);
        Check(instrument.MarketData.Count == 0, "attachment must wait for dispatcher");
        instrument.Dispatcher.Drain();
        Check(instrument.MarketData.Count == 1, "exactly one owned subscription");
        for (int i = 0; i < 20; i++) instrument.MarketData.Emit();
        DispatcherTimer.All.Last().Fire();
        Check(instrument.MarketData.Count == 0, "timer unsubscribes");
        Check(Output.Lines.Any(s => s.Contains("callbacksDuringAdd=1 callbacksAfterAdd=20 lastCallbacks=21")), "observes synchronous snapshot separately without asserting freshness");
        Check(Output.Lines.Count(s => s.Contains(": type=")) == 8, "bounded callback detail output");
        Check(Output.Lines.Any(s => s.Contains("published=0")), "observation does not claim publication");
        probe.SetState(State.Terminated); instrument.Dispatcher.Drain();
        Check(Output.Lines.Count(s => s.Contains("summary reason=")) == 1, "cleanup idempotent");

        instrument = new Instrument(); probe = Create(instrument); probe.SetState(State.Terminated); instrument.Dispatcher.Drain();
        Check(instrument.MarketData.Count == 0 && !Output.Lines.Any(s => s.Contains("starting observation")), "removal before queued attach cancels subscription");

        instrument = new Instrument(); probe = Create(instrument);
        instrument.MarketData.OnAdd = () => probe.SetState(State.Terminated);
        instrument.Dispatcher.Drain();
        Check(instrument.MarketData.Count == 0, "removal during add detaches");

        instrument = new Instrument(); probe = Create(instrument); instrument.MarketData.NotifyBeforeAdd = true;
        instrument.MarketData.OnAdd = () => probe.SetState(State.Terminated);
        instrument.Dispatcher.Drain();
        Check(instrument.MarketData.Count == 0, "reentrant removal before handler installation cannot leak subscription");

        instrument = new Instrument(); probe = Create(instrument); instrument.Dispatcher.Drain(); instrument.Dispatcher.Shutdown();
        Check(instrument.MarketData.Count == 0, "shutdown hook releases callback on dispatcher");

        instrument = new Instrument(); instrument.MarketData.ThrowAfterAdd = true; probe = Create(instrument); instrument.Dispatcher.Drain();
        Check(instrument.MarketData.Count == 0 && Output.Lines.Any(s => s.Contains("ATTACH_FAILED")), "partially successful add cleaned up");

        instrument = new Instrument(); probe = Create(instrument); instrument.Dispatcher.Drain(); instrument.MarketData.ThrowOnRemove = true;
        DispatcherTimer.All.Last().Fire();
        Check(Output.Lines.Any(s => s.Contains("DETACH_FAILED")) && Output.Lines.Any(s => s.Contains("detached=False")), "failed detach is explicit");
        instrument.MarketData.ThrowOnRemove = false; probe.SetState(State.Terminated); instrument.Dispatcher.Drain();
        Check(instrument.MarketData.Count == 0, "later cleanup retries failed detach");
        Console.WriteLine("PASS: " + checks + " subscription lifecycle fixture checks. Actual platform callbacks still require observation.");
    }
}
