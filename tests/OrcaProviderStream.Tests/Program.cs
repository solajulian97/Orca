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
        Console.WriteLine("PASS: " + checks + " checks. Storage/ownership contracts only; no NinjaTrader runtime validation.");
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
