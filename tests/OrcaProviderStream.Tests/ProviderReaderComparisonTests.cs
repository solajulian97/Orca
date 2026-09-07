using System;
using NinjaTrader.NinjaScript.Indicators;

static class ProviderReaderComparisonTests
{
    static OrcaProviderSourceIdentity Identity(OrcaProviderFeedLifetime owner)
    { return new OrcaProviderSourceIdentity("ES SEP26", owner.Environment, owner.Epoch, "session", "timezone", true, Guid.NewGuid(), OrcaProviderClassificationPolicy.TickDirection, "none"); }
    static OrcaStreamTick Tick(long volume)
    { return new OrcaStreamTick(new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc), 100, volume, volume, OrcaStreamClassification.TickDirection); }
    public static void Run(Action<bool, string> check)
    {
        using (var owner = new OrcaProviderFeedLifetime(OrcaProviderEnvironment.LiveFeed, 1, 512, 256, 3, 2))
        {
            var identity = Identity(owner); OrcaProviderPublisherLease publisher; owner.TryAcquire(identity, out publisher);
            using (var comparison = OrcaProviderReaderComparison.Create(owner, identity))
            {
                check(comparison.DrainThrough(0) && comparison.VerifiedEvents == 0, "empty comparison does not manufacture events");
                for (int i = 0; i < 300; i++) publisher.Append(Tick(1));
                check(!comparison.DrainThrough(300) && comparison.VerifiedEvents == 256, "comparison work bounded to 256 per reader");
                check(comparison.DrainThrough(300) && comparison.Volume == 300 && comparison.SignedVolume == 300, "tail drained exactly");
                ulong digest = comparison.Digest;
                check(comparison.DrainThrough(300) && comparison.Digest == digest, "caught-up read does not duplicate totals");
                for (int i = 0; i < 1000; i++)
                {
                    publisher.Append(Tick(2));
                    if ((i + 1) % 200 == 0) check(comparison.DrainThrough(301 + i), "incremental comparison survives prefix eviction");
                }
                check(comparison.VerifiedEvents == 1300 && comparison.Volume == 2300, "independent readers preserve same-time duplicates");
                publisher.Dispose(); bool rejected = false;
                try { comparison.DrainThrough(1301); } catch (InvalidOperationException) { rejected = true; }
                check(rejected && comparison.Failed, "closed stream comparison fails explicitly");
            }
        }
        using (var owner = new OrcaProviderFeedLifetime(OrcaProviderEnvironment.LiveFeed, 1, 256, 256, 2, 2))
        {
            var identity = Identity(owner); OrcaProviderPublisherLease publisher; owner.TryAcquire(identity, out publisher);
            using (var comparison = OrcaProviderReaderComparison.Create(owner, identity))
            {
                for (int i = 0; i < 257; i++) publisher.Append(Tick(1));
                bool rejected = false; try { comparison.DrainThrough(257); } catch (InvalidOperationException) { rejected = true; }
                check(rejected && comparison.Failed && comparison.VerifiedEvents == 0, "eviction fails without silent cursor reset");
            }
        }
        using (var owner = new OrcaProviderFeedLifetime(OrcaProviderEnvironment.LiveFeed, 1, 256, 256, 1, 2))
        {
            var identity = Identity(owner); OrcaProviderPublisherLease publisher; owner.TryAcquire(identity, out publisher);
            bool rejected = false; try { OrcaProviderReaderComparison.Create(owner, identity); } catch (InvalidOperationException) { rejected = true; }
            OrcaProviderStreamReader reader = null;
            check(rejected && owner.OpenReader(identity, out reader) == OrcaProviderReaderStatus.Opened, "partial admission releases acquired reader");
            if (reader != null) reader.Dispose();
        }
    }
}
