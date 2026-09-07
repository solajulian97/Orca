using System;
using System.Globalization;
using NinjaTrader.NinjaScript.Indicators;

static class ProviderIdentityTests
{
    static readonly Guid Connection = new Guid("12121212-1212-1212-1212-121212121212");
    static readonly Guid Origin = new Guid("34343434-3434-3434-3434-343434343434");
    static OrcaProviderStreamKey Key(string contract = "ES SEP26", OrcaProviderEnvironment environment = OrcaProviderEnvironment.LiveFeed,
        Guid? connection = null, string session = "schedule+holidays:v1", string zone = "timezone+adjustments:v1", bool reset = true,
        Guid? origin = null, OrcaProviderClassificationPolicy policy = OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection,
        string quotes = "platform-supplied-unverified:v1")
    {
        return new OrcaProviderSourceIdentity(contract, environment, connection ?? Connection, session, zone, reset,
            origin ?? Origin, policy, quotes).Key;
    }
    public static void Run(Action<bool, string> check)
    {
        var key = Key();
        check(key.Equals(Key()) && key.GetHashCode() == Key().GetHashCode(), "equivalent immutable descriptors match");
        var incompatible = new[] { Key(contract: "MES SEP26"), Key(contract: "ES DEC26"),
            Key(environment: OrcaProviderEnvironment.MarketReplay), Key(environment: OrcaProviderEnvironment.HistoricalOnly),
            Key(connection: Guid.NewGuid()), Key(session: "schedule+holidays:v2"), Key(zone: "timezone+adjustments:v2"),
            Key(reset: false), Key(origin: Guid.NewGuid()), Key(policy: OrcaProviderClassificationPolicy.TickDirection),
            Key(quotes: "verified-exchange:v1") };
        using (var registry = new OrcaProviderStreamRegistry(12, 2, 1))
        {
            OrcaProviderPublisherLease publisher;
            registry.TryAcquire(key, out publisher);
            foreach (var other in incompatible)
            {
                check(!key.Equals(other), "incompatible semantic dimension does not alias");
                OrcaProviderStreamReader reader;
                check(!registry.TryOpenReader(other, out reader), "incompatible source cannot attach");
            }
            OrcaProviderPublisherLease duplicate;
            check(registry.TryAcquire(Key(), out duplicate) == OrcaProviderAcquireStatus.Occupied, "compatible producer still has exclusive ownership");
        }
        check(!Key(session: "a", zone: "bc").Equals(Key(session: "ab", zone: "c")), "field boundaries cannot alias");
        check(!Key(session: "a:3", zone: "b").Equals(Key(session: "a", zone: "3:b")), "embedded delimiters cannot alias");
        var culture = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA"); check(key.Equals(Key()), "identity is culture independent"); }
        finally { CultureInfo.CurrentCulture = culture; }
        Action<Action> rejects = action => { bool rejected = false; try { action(); } catch (ArgumentException) { rejected = true; } check(rejected, "missing or invalid identity rejected"); };
        rejects(() => Key(connection: Guid.Empty)); rejects(() => Key(origin: Guid.Empty));
        rejects(() => Key(session: "")); rejects(() => Key(zone: " ")); rejects(() => Key(quotes: null));
        rejects(() => Key(contract: " ES SEP26")); rejects(() => Key(environment: (OrcaProviderEnvironment)99));
        rejects(() => Key(policy: (OrcaProviderClassificationPolicy)99));
        using (var registry = new OrcaProviderStreamRegistry(1, 2, 1))
        {
            OrcaProviderPublisherLease publisher; registry.TryAcquire(key, out publisher);
            var ingestion = new OrcaProviderIngestion(publisher, OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection, false);
            ingestion.OnTrade(false, DateTime.UtcNow, 100, 1, 99, 100, true, true);
            OrcaProviderStreamReader reader; registry.TryOpenReader(Key(), out reader);
            using (reader) using (var batch = reader.Read(reader.FirstAvailable, 1))
                check(batch.Events.Count == 1 && !batch.Coverage.ProducerConfirmedHistory, "identity compatibility is not historical coverage proof");
        }
    }
}
