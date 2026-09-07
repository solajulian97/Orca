using System;
using System.Threading.Tasks;
using NinjaTrader.NinjaScript.Indicators;

class Program
{
    static int checks;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    static OrcaStreamTick Tick(long volume)
    {
        return new OrcaStreamTick(new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc),
            100, volume, volume, OrcaStreamClassification.TickDirection);
    }
    static void Main()
    {
        ProviderIdentityTests.Run(Check);
        ProviderSessionCaptureTests.Run(Check);
        ProviderFeedLifetimeTests.Run(Check);
        ProviderReaderComparisonTests.Run(Check);
        using (var buffer = new OrcaProviderStreamBuffer(3, 2))
        {
            var original = buffer.FirstAvailable;
            buffer.Append(Tick(1)); buffer.Append(Tick(1)); buffer.Append(Tick(3));
            var snapshot = buffer.Read(original, int.MaxValue);
            Check(snapshot.Events.Count == 2, "bounded read with int.MaxValue");
            Check(snapshot.Next.Sequence == 2, "distinct same-time identical events preserved");
            Check(snapshot.Events[0].Classification == OrcaStreamClassification.TickDirection, "provenance retained");
            buffer.Append(Tick(4));
            Check(buffer.Read(original, 1).Status == OrcaStreamReadStatus.Evicted, "old cursor explicitly evicted");
            var continuation = buffer.Read(snapshot.Next, int.MaxValue);
            Check(continuation.Events.Count == 2 && continuation.Events[0].Volume == 3 && continuation.Events[1].Volume == 4,
                "absolute cursor survives prefix eviction");
            Check(snapshot.Events[0].Volume == 1, "snapshot independent of overwritten ring");
            Check(buffer.Read(continuation.Next, 1).Events.Count == 0, "caught-up is empty ready");
            Check(buffer.Read(new OrcaStreamCursor(original.Generation, 99), 1).Status == OrcaStreamReadStatus.Ahead, "future cursor");
            using (var replacement = new OrcaProviderStreamBuffer(3, 2))
                Check(replacement.Read(original, 1).Status == OrcaStreamReadStatus.WrongGeneration, "replacement cannot silently reuse cursor");
            buffer.Dispose(); buffer.Dispose();
            Check(buffer.Read(original, 1).Status == OrcaStreamReadStatus.Closed, "closed read");
            bool rejected = false;
            try { buffer.Append(Tick(1)); } catch (ObjectDisposedException) { rejected = true; }
            Check(rejected, "late publication rejected");
        }
        using (var buffer = new OrcaProviderStreamBuffer(128, 16))
        {
            var cursor = buffer.FirstAvailable;
            Parallel.For(0, 10000, i => buffer.Append(Tick(1)));
            Check(buffer.FirstAvailable.Sequence == 9872, "retention bounded under concurrent publishers");
            Check(buffer.Read(cursor, 16).Status == OrcaStreamReadStatus.Evicted, "lagging reader gets gap");
            cursor = buffer.FirstAvailable;
            int total = 0;
            while (true)
            {
                var batch = buffer.Read(cursor, int.MaxValue);
                Check(batch.Status == OrcaStreamReadStatus.Ready && batch.Events.Count <= 16, "bounded batch");
                if (batch.Events.Count == 0) break;
                total += batch.Events.Count; cursor = batch.Next;
            }
            Check(total == 128 && cursor.Sequence == 10000, "no retained-event loss");
            bool rejected = false;
            try { buffer.Append(default(OrcaStreamTick)); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "default event rejected");
        }
        TestRegistry();
        TestCoverage();
        TestReadBudgets();
        TestLifecycleHandoff();
        TestIngestion();
        Console.WriteLine("PASS: " + checks + " checks. Storage/ownership/coverage contracts only; no NinjaTrader runtime validation.");
    }

    static void TestIngestion()
    {
        using (var registry = new OrcaProviderStreamRegistry(1, 16, 16))
        {
            OrcaProviderPublisherLease owner;
            registry.TryAcquire(Key(), out owner);
            var adapter = new OrcaProviderIngestion(owner, OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection, true);
            OrcaProviderStreamReader reader;
            registry.TryOpenReader(Key(), out reader);
            using (reader)
            {
                DateTime time = Tick(1).Time;
                adapter.OnTrade(true, time, 100, 2, 99, 100, true, false);
                adapter.OnTrade(true, time, 99, 3, 99, 100, true, false);
                adapter.CompleteHistoricalPassAndBeginLive();
                // The same quote/price/time as the last historical callback remains a distinct trade.
                adapter.OnTrade(false, time, 99, 3, 99, 100, true, false);
                Reject(() => adapter.OnTrade(true, time, 500, 1, 0, 0, false, true), "rejected lane does not change classifier");
                adapter.OnTrade(false, time, 99, 1, double.NaN, double.NaN, false, false);
                adapter.OnTrade(false, time, 99, 1, 99, 99, true, true);
                adapter.OnTrade(false, time, 100, 1, 101, 100, true, false);
                Reject(() => adapter.OnTrade(false, time, 500, 0, 0, 0, false, true), "invalid volume leaves classifier unchanged");
                adapter.OnTrade(false, time, 100, 1, 0, 0, false, false);
                using (var batch = reader.Read(reader.FirstAvailable, 16))
                {
                    Check(batch.Events.Count == 7 && batch.Coverage.HistoryEndSequenceExclusive == 2, "adapter lane boundary and rejected-event accounting");
                    Check(batch.Events[0].SignedVolume == 2 && batch.Events[1].SignedVolume == -3
                        && batch.Events[2].SignedVolume == -3, "trade-time quote classification and same-time duplicate preservation");
                    Check(batch.Events[3].SignedVolume == -1 && batch.Events[3].Classification == OrcaStreamClassification.TickDirection,
                        "equal-price fallback inherits direction without claiming bidask evidence");
                    Check(batch.Events[4].SignedVolume == 0 && batch.Events[4].Classification == OrcaStreamClassification.Unknown,
                        "explicit session reset and locked quotes yield unknown first trade");
                    Check(batch.Events[5].SignedVolume == 1 && batch.Events[5].Classification == OrcaStreamClassification.TickDirection,
                        "crossed quotes cannot claim bidask classification");
                    Check(batch.Events[6].SignedVolume == 1, "failed event did not mutate classifier");
                }
                adapter.MarkFault(OrcaStreamFault.SourceDisconnected);
                Reject(() => adapter.OnTrade(false, time, 100, 1, 99, 100, true, false), "adapter cannot publish after fault");
            }
        }
        using (var registry = new OrcaProviderStreamRegistry(1, 4, 4))
        {
            OrcaProviderPublisherLease owner;
            registry.TryAcquire(Key(policy: "tickdirection:v1"), out owner);
            Reject(() => new OrcaProviderIngestion(owner, OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection, false),
                "adapter cannot publish under an incompatible policy identity");
            var adapter = new OrcaProviderIngestion(owner, OrcaProviderClassificationPolicy.TickDirection, false);
            adapter.OnTrade(false, Tick(1).Time, 100, 1, 99, 100, true, false);
            adapter.OnTrade(false, Tick(1).Time, 99, 1, 98, 99, true, false);
            OrcaProviderStreamReader reader;
            registry.TryOpenReader(Key(policy: "tickdirection:v1"), out reader);
            using (reader)
            using (var batch = reader.Read(reader.FirstAvailable, 4))
            {
                Check(batch.Events[0].Classification == OrcaStreamClassification.Unknown
                    && batch.Events[1].SignedVolume == -1, "tick-only policy ignores supplied quotes");
                Check(!batch.Coverage.HistoricalPassCompleted, "live-only adapter does not claim history");
            }
        }
    }

    static void TestLifecycleHandoff()
    {
        using (var registry = new OrcaProviderStreamRegistry(1, 4, 4))
        {
            OrcaProviderPublisherLease owner;
            registry.TryAcquire(Key(), out owner);
            OrcaProviderStreamReader reader;
            registry.TryOpenReader(Key(), out reader);
            using (reader)
            {
                var cursor = reader.FirstAvailable;
                owner.BeginHistoricalPass();
                Reject(() => owner.Append(Tick(1)), "lifecycle generic append rejected");
                Reject(() => owner.AppendLive(Tick(1)), "early live lane rejected");
                owner.AppendHistorical(Tick(1)); owner.AppendHistorical(Tick(1));
                Reject(() => owner.ConfirmHistory(), "pass cannot certify arbitrary interval");
                owner.CompleteHistoricalPassAndBeginLive();
                owner.AppendLive(Tick(1)); owner.AppendLive(Tick(1));
                using (var batch = reader.Read(cursor, 4))
                {
                    Check(batch.Events.Count == 4 && batch.Coverage.HistoryEndSequenceExclusive == 2,
                        "identical historical/live ticks preserved across sequence boundary");
                    Check(batch.Coverage.Kind == OrcaStreamCoverageKind.SourceLifecycle
                        && batch.Coverage.HistoricalPassCompleted && batch.Coverage.FullHistoricalPassRetained,
                        "source pass completion explicit");
                    Check(!batch.Coverage.ProducerConfirmedHistory && !batch.Coverage.FullConfirmedHistoryRetained
                        && batch.Coverage.HistoryToUtcExclusive == DateTime.MinValue,
                        "source pass does not claim complete UTC interval");
                }
                Reject(() => owner.AppendHistorical(Tick(1)), "late historical callback cannot become live");
                Reject(() => owner.CompleteHistoricalPassAndBeginLive(), "duplicate handoff rejected");
                // Clock regressions do not reorder or erase source-sequenced events.
                var earlier = new OrcaStreamTick(Tick(1).Time.AddSeconds(-1), 99, 1, 0, OrcaStreamClassification.Unknown);
                owner.AppendLive(earlier);
                using (var batch = reader.Read(reader.FirstAvailable, 4))
                    Check(batch.Events[3].Time == earlier.Time && !batch.Coverage.FullHistoricalPassRetained,
                        "arrival sequence preserved and historical eviction visible");
                owner.MarkFault(OrcaStreamFault.SourceDisconnected);
                Reject(() => owner.AppendLive(Tick(1)), "fault blocks lifecycle lane");
            }
        }
        using (var buffer = new OrcaProviderStreamBuffer(2, 2))
        {
            buffer.BeginLiveOnly(); buffer.AppendLive(Tick(1));
            var batch = buffer.Read(buffer.FirstAvailable, 2);
            Check(batch.Coverage.Phase == OrcaStreamPhase.Live && !batch.Coverage.HistoricalPassCompleted
                && batch.Coverage.HistoryEndSequenceExclusive == -1, "live-only never manufactures empty history confirmation");
            Reject(() => buffer.AppendHistorical(Tick(1)), "live-only rejects history");
            Reject(() => buffer.BeginHistoricalPass(), "cannot backfill an existing live generation in place");
        }
        using (var buffer = new OrcaProviderStreamBuffer(2, 2))
        {
            buffer.BeginHistoricalPass(); buffer.CompleteHistoricalPassAndBeginLive();
            var batch = buffer.Read(buffer.FirstAvailable, 2);
            Check(batch.Coverage.HistoricalPassCompleted && batch.Coverage.HistoryEndSequenceExclusive == 0
                && !batch.Coverage.ProducerConfirmedHistory, "empty completed callback pass is not interval proof");
        }
        for (int run = 0; run < 20; run++)
        {
            using (var buffer = new OrcaProviderStreamBuffer(2, 2))
            {
                buffer.BeginHistoricalPass();
                int accepted = 0;
                Parallel.Invoke(() => { try { buffer.AppendHistorical(Tick(1)); accepted = 1; } catch (InvalidOperationException) { } },
                    () => buffer.CompleteHistoricalPassAndBeginLive());
                buffer.AppendLive(Tick(1));
                var batch = buffer.Read(buffer.FirstAvailable, 2);
                Check(batch.Coverage.HistoryEndSequenceExclusive == accepted && batch.Events.Count == accepted + 1,
                    "racing historical append either precedes boundary or is explicitly rejected");
            }
        }
    }

    static void TestReadBudgets()
    {
        using (var registry = new OrcaProviderStreamRegistry(1, 4, 2, 1, 1))
        {
            OrcaProviderPublisherLease owner;
            registry.TryAcquire(Key(), out owner);
            owner.Append(Tick(1)); owner.Append(Tick(2));
            OrcaProviderStreamReader reader, denied;
            Check(registry.OpenReader(Key(), out reader) == OrcaProviderReaderStatus.Opened, "budgeted reader acquired");
            Check(registry.OpenReader(Key(), out denied) == OrcaProviderReaderStatus.CapacityReached && denied == null,
                "reader capacity refusal distinct from missing source");
            var cursor = reader.FirstAvailable;
            var held = reader.Read(cursor, int.MaxValue);
            var retainedView = held.Events;
            bool refused = false;
            try { reader.Read(cursor, 1); } catch (OrcaProviderBudgetExceededException) { refused = true; }
            Check(refused && held.Events.Count == 2, "outstanding batch budget enforced before another copy");
            held.Dispose(); held.Dispose();
            Reject(() => { var ignored = retainedView[0]; }, "retained view cannot expose disposed payload");
            using (var released = reader.Read(cursor, 1))
                Check(released.Events.Count == 1, "batch disposal releases budget exactly once");
            Reject(() => reader.Read(cursor, 0), "invalid read does not consume budget");
            using (var valid = reader.Read(cursor, 1)) Check(valid.Events.Count == 1, "budget intact after invalid request");
            var inFlight = reader.Read(cursor, 1);
            reader.Dispose(); reader.Dispose();
            Check(registry.OpenReader(Key(), out denied) == OrcaProviderReaderStatus.Opened, "reader slot reusable");
            bool stillReserved = false;
            try { denied.Read(cursor, 1); } catch (OrcaProviderBudgetExceededException) { stillReserved = true; }
            Check(stillReserved, "reader disposal does not release outstanding batch reservation");
            registry.Dispose();
            Check(inFlight.Events[0].Volume == 1, "consumer-owned batch survives source shutdown");
            inFlight.Dispose();
            using (var closed = denied.Read(cursor, 1)) Check(closed.Status == OrcaStreamReadStatus.Closed, "closed generation readable as status");
            denied.Dispose();
            Reject(() => reader.Read(cursor, 1), "disposed reader rejected");
        }
        using (var registry = new OrcaProviderStreamRegistry(1, 4, 1, 64, 1))
        {
            OrcaProviderPublisherLease owner;
            registry.TryAcquire(Key(), out owner); owner.Append(Tick(1));
            var readers = new OrcaProviderStreamReader[32];
            var batches = new OrcaProviderBatchLease[32];
            for (int i = 0; i < readers.Length; i++) registry.TryOpenReader(Key(), out readers[i]);
            Parallel.For(0, 32, i =>
            {
                try { batches[i] = readers[i].Read(readers[i].FirstAvailable, 1); }
                catch (OrcaProviderBudgetExceededException) { }
            });
            int winners = 0;
            foreach (var batch in batches) if (batch != null) { winners++; batch.Dispose(); }
            Check(winners == 1, "one outstanding batch winner across concurrent readers");
            foreach (var reader in readers) reader.Dispose();
        }
    }

    static void Reject(Action action, string name)
    {
        bool rejected = false;
        try { action(); } catch (ArgumentException) { rejected = true; }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, name);
    }

    static void TestCoverage()
    {
        DateTime start = Tick(1).Time;
        DateTime boundary = start.AddSeconds(1);
        var liveTick = new OrcaStreamTick(boundary, 100, 1, 1, OrcaStreamClassification.BidAsk);
        using (var registry = new OrcaProviderStreamRegistry(1, 3, 3))
        {
            OrcaProviderPublisherLease lease;
            registry.TryAcquire(Key(), out lease);
            OrcaProviderStreamReader reader;
            registry.TryOpenReader(Key(), out reader);
            var cursor = reader.FirstAvailable;
            var empty = reader.Read(cursor, 3);
            Check(empty.Coverage.Phase == OrcaStreamPhase.Unverified && !empty.Coverage.ProducerConfirmedHistory,
                "empty registration is not complete history");
            Reject(() => lease.BeginLive(), "cannot become live before history confirmation");
            Reject(() => lease.BeginHistory(DateTime.SpecifyKind(start, DateTimeKind.Unspecified), boundary), "UTC required");
            Reject(() => lease.BeginHistory(start, start), "empty requested interval rejected");
            lease.BeginHistory(start, boundary);
            Reject(() => lease.BeginHistory(start, boundary), "cannot restart history in-place");
            Reject(() => lease.Append(liveTick), "exclusive history boundary enforced");
            lease.Append(Tick(1)); lease.Append(Tick(1));
            var loading = reader.Read(cursor, 3);
            Check(loading.Events.Count == 2 && !loading.Coverage.ProducerConfirmedHistory, "observed events do not prove completeness");
            lease.ConfirmHistory();
            var confirmed = reader.Read(cursor, 3);
            Check(confirmed.Coverage.ProducerConfirmedHistory && confirmed.Coverage.FullConfirmedHistoryRetained
                && confirmed.Coverage.HistoryEndSequenceExclusive == 2, "confirmed range has atomic sequence boundary");
            Check(!loading.Coverage.ProducerConfirmedHistory, "prior snapshot coverage immutable");
            Reject(() => lease.Append(Tick(1)), "handoff barrier rejects unassigned events");
            lease.BeginLive();
            Reject(() => lease.Append(Tick(1)), "overlapping historical event rejected in live phase");
            lease.Append(liveTick); lease.Append(liveTick);
            var live = reader.Read(confirmed.Next, 3);
            Check(live.Events.Count == 2 && live.Events[0].Time == boundary && live.Events[1].Time == boundary,
                "same-time live trades retained without timestamp deduplication");
            Check(live.Coverage.Phase == OrcaStreamPhase.Live && live.Coverage.ProducerConfirmedHistory
                && !live.Coverage.FullConfirmedHistoryRetained, "retention loss is separate from producer confirmation");
            lease.MarkFault(OrcaStreamFault.SourceDisconnected);
            lease.MarkFault(OrcaStreamFault.IngestionFailed);
            var failed = reader.Read(live.Next, 3);
            Check(failed.Status == OrcaStreamReadStatus.SourceFaulted && failed.Events.Count == 0
                && failed.Coverage.Fault == OrcaStreamFault.SourceDisconnected, "fault fails closed and preserves first cause");
            Reject(() => lease.BeginLive(), "fault cannot resume same generation");
            Reject(() => lease.Append(liveTick), "fault blocks publication");
            lease.Dispose();
            Check(reader.Read(cursor, 3).Coverage.Phase == OrcaStreamPhase.Closed, "closed coverage state");
        }
        using (var buffer = new OrcaProviderStreamBuffer(2, 2))
        {
            buffer.BeginHistory(start, boundary);
            buffer.ConfirmHistory(); buffer.BeginLive();
            buffer.Append(liveTick); buffer.Append(liveTick); buffer.Append(liveTick);
            var batch = buffer.Read(buffer.FirstAvailable, 2);
            Check(batch.Coverage.HistoryEndSequenceExclusive == 0 && batch.Coverage.FullConfirmedHistoryRetained,
                "producer-confirmed empty history remains valid after live-only eviction");
        }
        using (var buffer = new OrcaProviderStreamBuffer(2, 2))
        {
            buffer.Append(Tick(1));
            Reject(() => buffer.BeginHistory(start, boundary), "cannot retroactively certify unverified payload");
            buffer.MarkFault(OrcaStreamFault.HistoricalGap);
            Check(!buffer.Read(buffer.FirstAvailable, 1).Coverage.FullConfirmedHistoryRetained, "unverified fault never complete");
        }
        for (int i = 0; i < 20; i++)
        {
            using (var buffer = new OrcaProviderStreamBuffer(2, 2))
            {
                buffer.BeginHistory(start, boundary);
                Parallel.Invoke(() => { try { buffer.Append(Tick(1)); } catch (InvalidOperationException) { } },
                    () => buffer.ConfirmHistory());
                var batch = buffer.Read(buffer.FirstAvailable, 2);
                Check(batch.Coverage.HistoryEndSequenceExclusive == batch.PublishedThroughExclusive
                    && batch.Events.Count == batch.PublishedThroughExclusive,
                    "confirmation atomically seals the accepted historical prefix");
            }
        }
    }

    static OrcaProviderStreamKey Key(string contract = "ES SEP26", string environment = "historical-live:feedA",
        string session = "CME-ETH:v1", string time = "UTC", string policy = "bidask-fallback:v1")
    { return new OrcaProviderStreamKey(contract, environment, session, time, policy); }

    static void TestRegistry()
    {
        using (var registry = new OrcaProviderStreamRegistry(1, 8, 4))
        {
            var leases = new OrcaProviderPublisherLease[64];
            var statuses = new OrcaProviderAcquireStatus[64];
            Parallel.For(0, 64, i => statuses[i] = registry.TryAcquire(Key(), out leases[i]));
            int wins = 0;
            OrcaProviderPublisherLease owner = null;
            for (int i = 0; i < 64; i++)
            {
                if (statuses[i] == OrcaProviderAcquireStatus.Acquired) { wins++; owner = leases[i]; }
                else Check(statuses[i] == OrcaProviderAcquireStatus.Occupied && leases[i] == null, "losing claimant gets no writer");
            }
            Check(wins == 1 && registry.ActiveStreams == 1, "one winner under contention");
            OrcaProviderPublisherLease refused;
            Check(registry.TryAcquire(Key("NQ SEP26"), out refused) == OrcaProviderAcquireStatus.CapacityReached, "registry capacity enforced");
            OrcaProviderStreamReader reader;
            Check(registry.TryOpenReader(Key(), out reader), "reader attaches by equivalent key");
            var cursor = reader.FirstAvailable;
            owner.Append(Tick(2));
            Check(reader.Read(cursor, 4).Events[0].Volume == 2, "owner publishes to pinned reader");
            owner.Dispose();
            Check(reader.Read(cursor, 4).Status == OrcaStreamReadStatus.Closed && registry.ActiveStreams == 0, "release closes readers and removes source");
            bool rejected = false;
            try { owner.Append(Tick(3)); } catch (ObjectDisposedException) { rejected = true; }
            Check(rejected, "released writer cannot publish");
            OrcaProviderPublisherLease replacement;
            Check(registry.TryAcquire(Key(), out replacement) == OrcaProviderAcquireStatus.Acquired, "released capacity reusable");
            owner.Dispose(); // delayed duplicate shutdown must not remove the replacement
            Check(registry.ActiveStreams == 1, "stale shutdown cannot remove successor");
            OrcaProviderStreamReader nextReader;
            Check(registry.TryOpenReader(Key(), out nextReader), "replacement reader");
            Check(nextReader.Read(cursor, 1).Status == OrcaStreamReadStatus.WrongGeneration, "replacement generation isolated");
            Check(reader.Read(cursor, 1).Status == OrcaStreamReadStatus.Closed, "old reader does not silently switch");
            replacement.Append(Tick(4));
            registry.Dispose(); registry.Dispose(); replacement.Dispose();
            Check(registry.ActiveStreams == 0 && registry.TryAcquire(Key(), out refused) == OrcaProviderAcquireStatus.Closed, "service shutdown permanent");
            Check(!registry.TryOpenReader(Key(), out nextReader), "no readers after shutdown");
        }
        using (var registry = new OrcaProviderStreamRegistry(6, 2, 1))
        {
            var keys = new[] { Key(), Key("MES SEP26"), Key(environment: "playback:sessionB"),
                Key(session: "CME-RTH:v1"), Key(time: "America/New_York"), Key(policy: "tickdirection:v1") };
            foreach (var key in keys)
            {
                OrcaProviderPublisherLease lease;
                Check(registry.TryAcquire(key, out lease) == OrcaProviderAcquireStatus.Acquired, "incompatible identity isolated");
            }
            Check(registry.ActiveStreams == 6, "all key dimensions participate in equality");
        }
        for (int run = 0; run < 20; run++)
        {
            using (var registry = new OrcaProviderStreamRegistry(1, 8, 4))
            {
                OrcaProviderPublisherLease lease;
                registry.TryAcquire(Key(), out lease);
                OrcaProviderStreamReader reader;
                registry.TryOpenReader(Key(), out reader);
                var cursor = reader.FirstAvailable;
                Parallel.Invoke(
                    () => { for (int i = 0; i < 1000; i++) { try { lease.Append(Tick(1)); } catch (ObjectDisposedException) { break; } } },
                    () => lease.Dispose(),
                    () => registry.Dispose());
                Check(reader.Read(cursor, 4).Status == OrcaStreamReadStatus.Closed && registry.ActiveStreams == 0,
                    "concurrent publication/owner release/service close converges");
            }
        }
    }
}
