using System;
using System.Collections.Generic;

enum MarketDataType { Bid, Ask, Last }
enum Calculate { OnEachTick, OnPriceChange, OnBarClose }
enum State { Historical, Realtime }
enum InstrumentType { Future, CryptoCurrency }
enum OrcaOrderFlowSourceMode { Internal, SharedProvider, SharedHistoricalInternalRealtime }
enum OrcaDiagnosticsWorkKind { MarketData }
sealed class MarketDataEventArgs { public MarketDataType MarketDataType; public DateTime Time; public double Price, Bid, Ask; public long Volume; }
sealed class MasterInstrument { public InstrumentType InstrumentType = InstrumentType.Future; }
sealed class Instrument { public MasterInstrument MasterInstrument = new MasterInstrument(); }
sealed class Bars
{
    public DateTime[] Times;
    public int Count { get { return Times.Length; } }
    // Documented GetBar contract: first matching timestamp. This models timestamp
    // ambiguity; it does not emulate NinjaTrader's scheduling or bar construction.
    public int GetBar(DateTime time) { for (int i = 0; i < Count; i++) if (Times[i] >= time) return i; return Count - 1; }
}
class Indicator { protected virtual void OnMarketData(MarketDataEventArgs e) { } }
namespace NinjaTrader.Core { static class Globals { public static long ToCryptocurrencyVolume(long value) { return value; } } }
static class OrcaDiagnosticsCore
{
    public static bool IsEnabled = false;
    public static long ReportMarketData(string id, MarketDataType type, DateTime time) { return 0; }
    public static long BeginWorkSample(long sequence) { return 0; }
    public static void ReportModelUpdate(string id, DateTime time, object unused) { }
    public static void ReportWorkSample(string id, OrcaDiagnosticsWorkKind kind, int bip, long start) { }
}
sealed class Subject : Indicator
{
    double lastBid = double.NaN, lastAsk = double.NaN, prevLast = double.NaN;
    int lastDirection;
    List<double> barTickDelta, barMaxDelta, barMinDelta;
    List<bool> barHasData;
    bool providerDataActive = true;
    string diagnosticsInstanceId = "fixture";
    public OrcaOrderFlowSourceMode OrderFlowSourceMode = OrcaOrderFlowSourceMode.Internal;
    public Calculate Calculate = Calculate.OnEachTick;
    public State State = State.Realtime;
    public Instrument Instrument = new Instrument();
    public Bars[] BarsArray;
    public int CurrentBar;
    void EnsureDiagnosticsRegistered() { }
    DateTime GetDiagnosticsEventTime() { return DateTime.MinValue; }
    bool ShouldAttemptRealtimeSharedBackfill() { return false; }
    void TryRefreshFromSharedProvider(bool force, bool lag, bool clear) { throw new Exception("Unexpected provider access"); }
    public Subject(params DateTime[] times) { BarsArray = new[] { new Bars { Times = times } }; }
    public void Trade(int bar, DateTime time, double price, long volume, double bid = 100, double ask = 101)
    {
        CurrentBar = bar;
        OnMarketData(new MarketDataEventArgs { MarketDataType = MarketDataType.Last, Time = time, Price = price, Volume = volume, Bid = bid, Ask = ask });
    }
    public void Quote(MarketDataType type, double price) { OnMarketData(new MarketDataEventArgs { MarketDataType = type, Price = price }); }
    public double Delta(int bar) { return HasDeltaForBar(bar) ? barTickDelta[bar] : 0; }
    public double Max(int bar) { return HasDeltaForBar(bar) ? barMaxDelta[bar] : 0; }
    public double Min(int bar) { return HasDeltaForBar(bar) ? barMinDelta[bar] : 0; }
    public double Finish(int bar) { return GetFinishDelta(bar); }
    public bool Has(int bar) { return HasDeltaForBar(bar); }
    /* ACTUAL_METHODS */
}
static class Program
{
    static int checks;
    static void Equal(double expected, double actual, string label) { checks++; if (expected != actual) throw new Exception(label + ": expected " + expected + ", got " + actual); }
    static void True(bool value, string label) { checks++; if (!value) throw new Exception(label); }
    public static int Main()
    {
        try
        {
            DateTime t = new DateTime(2026, 9, 9, 10, 0, 0);
            // Three distinct assigned range/volume bars sharing one timestamp.
            // No render callbacks are involved in collecting any of these trades.
            foreach (State state in new[] { State.Historical, State.Realtime })
            {
                var s = new Subject(t, t, t) { State = state };
                s.Trade(0, t, 101, 5);
                s.Trade(1, t, 100, 7);
                s.Trade(2, t, 101, 11);
                s.Trade(2, t, 100, 4);
                Equal(5, s.Delta(0), state + " first same-time bar");
                Equal(-7, s.Delta(1), state + " second same-time bar");
                Equal(7, s.Delta(2), state + " third same-time bar");
                Equal(11, s.Max(2), "intrabar maximum preserved");
                Equal(0, s.Min(2), "intrabar minimum preserved");
                Equal(-4, s.Finish(2), "finish delta preserved");
                Equal(5, s.Delta(0) + s.Delta(1) + s.Delta(2), "total signed volume conserved");
            }
            var minutes = new Subject(t.AddMinutes(1), t.AddMinutes(2));
            minutes.Trade(0, t.AddSeconds(30), 101, 4);
            minutes.Trade(1, t.AddSeconds(90), 100, 3);
            Equal(4, minutes.Delta(0), "ordinary close-stamped first time bar");
            Equal(-3, minutes.Delta(1), "ordinary close-stamped second time bar");

            var developing = new Subject(t.AddMinutes(1), t.AddMinutes(2));
            developing.Trade(0, t.AddSeconds(90), 101, 9);
            Equal(9, developing.Delta(0), "assigned developing bar overrides timestamp lookup");
            True(!developing.Has(1), "no lookahead write to a loaded future bar");

            var missing = new Subject(t, t);
            missing.Trade(-1, t, 101, 8);
            missing.Trade(2, t, 101, 8);
            True(!missing.Has(0) && !missing.Has(1), "invalid assigned bars do not corrupt edge bars");

            var quotes = new Subject(t);
            quotes.Quote(MarketDataType.Bid, 100);
            quotes.Quote(MarketDataType.Ask, 101);
            quotes.Trade(0, t, 101, 6, 0, 0);
            quotes.Trade(0, t, 100, 2, 0, 0);
            Equal(4, quotes.Delta(0), "remembered quote classification");

            var fallback = new Subject(t);
            fallback.Trade(0, t, 100, 2, 0, 0);
            fallback.Trade(0, t, 101, 5, 0, 0);
            fallback.Trade(0, t, 101, 3, 0, 0);
            fallback.Trade(0, t, 100, 4, 0, 0);
            Equal(4, fallback.Delta(0), "tick-direction fallback including equal price");

            var provider = new Subject(t) { OrderFlowSourceMode = OrcaOrderFlowSourceMode.SharedProvider };
            provider.Trade(0, t, 101, 8);
            True(!provider.Has(0), "shared-only mode does not ingest local trade volume");
            var hybrid = new Subject(t, t) { OrderFlowSourceMode = OrcaOrderFlowSourceMode.SharedHistoricalInternalRealtime };
            hybrid.Trade(1, t, 101, 8);
            Equal(8, hybrid.Delta(1), "hybrid local callback also honors assigned bar");

            foreach (Calculate mode in new[] { Calculate.OnBarClose, Calculate.OnPriceChange })
            {
                var legacy = new Subject(t, t) { Calculate = mode };
                legacy.Trade(1, t, 101, 3);
                Equal(3, legacy.Delta(0), "existing timestamp fallback retained for " + mode);
            }
            Console.WriteLine("PASS: " + checks + " assertions using actual source OnMarketData/storage/extrema methods; modeled event inputs only, not a NinjaTrader tab-switch reproduction.");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine("FAIL: " + ex.Message); return 1; }
    }
}
