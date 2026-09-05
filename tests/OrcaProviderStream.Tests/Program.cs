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
        Console.WriteLine("PASS: " + checks + " checks. Storage contract only; no NinjaTrader runtime validation.");
    }
}
