using System;
using System.Threading.Tasks;
using NinjaTrader.NinjaScript.Indicators;

static class ProviderFeedLifetimeTests
{
    static OrcaProviderSourceIdentity Identity(OrcaProviderFeedLifetime owner, string contract = "ES SEP26", OrcaProviderEnvironment? environment = null)
    {
        return new OrcaProviderSourceIdentity(contract, environment ?? owner.Environment, owner.Epoch,
            "effective-session", "effective-timezone", true, new Guid("56565656-5656-5656-5656-565656565656"),
            OrcaProviderClassificationPolicy.TickDirection, "none");
    }
    static OrcaProviderFeedLifetime Owner()
    { return new OrcaProviderFeedLifetime(OrcaProviderEnvironment.LiveFeed, 2, 8, 2, 4, 4); }
    public static void Run(Action<bool, string> check)
    {
        using (var first = Owner()) using (var second = Owner())
        {
            var identity = Identity(first);
            check(first.Epoch != second.Epoch, "replacement has fresh epoch");
            OrcaProviderPublisherLease publisher;
            check(first.TryAcquire(identity, out publisher) == OrcaProviderAcquireStatus.Acquired, "owner accepts matching identity");
            bool rejected = false;
            try { OrcaProviderPublisherLease other; second.TryAcquire(identity, out other); } catch (ArgumentException) { rejected = true; }
            check(rejected, "old identity cannot acquire in replacement");
            rejected = false;
            try { OrcaProviderStreamReader other; first.OpenReader(Identity(first, environment: OrcaProviderEnvironment.MarketReplay), out other); } catch (ArgumentException) { rejected = true; }
            check(rejected, "environment mismatch cannot read");
            publisher.BeginLiveOnly();
            publisher.AppendLive(new OrcaStreamTick(DateTime.UtcNow, 100, 1, 1, OrcaStreamClassification.TickDirection));
            OrcaProviderStreamReader reader; first.OpenReader(identity, out reader);
            using (reader) using (var held = reader.Read(reader.FirstAvailable, 1))
            {
                first.Dispose(); first.Dispose();
                check(first.ActiveStreams == 0, "closure releases all stream ownership");
                using (var closed = reader.Read(held.Next, 1)) check(closed.Status == OrcaStreamReadStatus.Closed, "pinned reader observes terminal closure");
                check(held.Events.Count == 1, "already delivered batch remains consumer owned");
                OrcaProviderPublisherLease late;
                check(first.TryAcquire(identity, out late) == OrcaProviderAcquireStatus.Closed, "old owner cannot reopen");
                rejected = false;
                try { publisher.AppendLive(new OrcaStreamTick(DateTime.UtcNow, 100, 1, 1, OrcaStreamClassification.TickDirection)); } catch (ObjectDisposedException) { rejected = true; }
                check(rejected, "late source callbacks rejected after closure");
                check(second.TryAcquire(Identity(second), out late) == OrcaProviderAcquireStatus.Acquired, "replacement can start independently");
                publisher.Dispose(); check(second.ActiveStreams == 1, "stale lease disposal cannot close replacement");
            }
        }
        for (int i = 0; i < 20; i++)
        {
            using (var owner = Owner())
            {
                var identity = Identity(owner);
                Parallel.Invoke(() => { OrcaProviderPublisherLease publisher; owner.TryAcquire(identity, out publisher); }, owner.Dispose);
                check(owner.ActiveStreams == 0, "acquisition racing closure cannot resurrect source");
            }
        }
    }
}
