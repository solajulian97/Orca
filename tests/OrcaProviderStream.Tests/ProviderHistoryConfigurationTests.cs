using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.Indicators;

// Shape-only fixtures. Installed-platform semantic compilation checks the real public APIs.
namespace NinjaTrader.Cbi
{
    public enum InstrumentType { Future, Stock }
    public enum LookupPolicies { Provider = 1, Repository = 2 }
    public enum MergePolicy { DoNotMerge, MergeBackAdjusted, MergeNonBackAdjusted, UseGlobalSettings, UseDefault }
    public class Rollover { public DateTime ContractMonth, Date; public double Offset; }
    public class MasterInstrument
    {
        public InstrumentType InstrumentType;
        public double TickSize = 0.25, PointValue = 50;
        public Collection<Rollover> RolloverCollection = new Collection<Rollover>();
    }
    public class Instrument
    {
        public string FullName = "ES SEP26";
        public DateTime Expiry = new DateTime(2026, 9, 1);
        public MasterInstrument MasterInstrument = new MasterInstrument();
    }
}
namespace NinjaTrader.Data
{
    public enum BarsPeriodType { Tick, Minute }
    public enum MarketDataType { Last, Bid, Ask }
    public class BarsPeriod { public BarsPeriodType BarsPeriodType; public int Value = 1; public MarketDataType MarketDataType; }
    public class BarsRequest
    {
        public Instrument Instrument = new Instrument();
        public BarsPeriod BarsPeriod = new BarsPeriod();
        public bool IsDividendAdjusted, IsSplitAdjusted, IsResetOnNewTradingDay = true;
        public int BarsBack;
        public DateTime FromLocal = new DateTime(2026, 9, 8), ToLocal = new DateTime(2026, 9, 9);
        public TradingHours TradingHours = new TradingHours();
        public LookupPolicies LookupPolicy = LookupPolicies.Provider | LookupPolicies.Repository;
        private MergePolicy merge;
        public Action OnMergeRead;
        public MergePolicy MergePolicy { get { if (OnMergeRead != null) OnMergeRead(); return merge; } set { merge = value; } }
    }
}
static class ProviderHistoryConfigurationTests
{
    static readonly Guid HistoryId = new Guid("11111111-2222-3333-4444-555555555555");
    static readonly Guid ClassifierId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    static BarsRequest Request()
    {
        var request = new BarsRequest { MergePolicy = MergePolicy.MergeBackAdjusted };
        request.TradingHours.Sessions.Add(new Session { BeginDay = DayOfWeek.Monday, EndDay = DayOfWeek.Monday,
            TradingDay = DayOfWeek.Monday, BeginTime = 90000, EndTime = 170000 });
        request.Instrument.MasterInstrument.RolloverCollection.Add(new Rollover
        { ContractMonth = new DateTime(2026, 9, 1), Date = new DateTime(2026, 6, 18), Offset = 10.5 });
        request.Instrument.MasterInstrument.RolloverCollection.Add(new Rollover
        { ContractMonth = new DateTime(2026, 12, 1), Date = new DateTime(2026, 9, 17), Offset = -2.25 });
        return request;
    }
    static OrcaProviderHistoryConfigurationCapture Capture(BarsRequest request)
    { return OrcaProviderHistoryConfigurationCapture.Capture(request, TimeZoneInfo.Utc, TimeZoneInfo.Utc); }
    static OrcaProviderSourceIdentity Identity(OrcaProviderHistoryConfigurationCapture capture, OrcaProviderFeedLifetime owner, Guid? history = null)
    { return capture.CreateIdentity(owner, history ?? HistoryId, true, ClassifierId, OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection, "platform-unverified:v1"); }
    public static void Run(Action<bool, string> check)
    {
        var request = Request(); var frozen = Capture(request);
        check(frozen.Definition == Capture(Request()).Definition, "equivalent request configurations match");
        frozen.RequireUnchanged(request, TimeZoneInfo.Utc, TimeZoneInfo.Utc);
        check(frozen.FullContract == "ES SEP26", "capture retains exact contract");
        var reordered = Request(); var rows = reordered.Instrument.MasterInstrument.RolloverCollection;
        var first = rows[0]; rows.RemoveAt(0); rows.Add(first);
        check(frozen.Definition == Capture(reordered).Definition, "rollover collection enumeration order is not identity");
        var renamed = Request(); renamed.TradingHours.Name = "Same schedule, different label";
        check(frozen.Definition == Capture(renamed).Definition, "session template label is not history semantics");
        var culture = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA"); check(frozen.Definition == Capture(Request()).Definition, "request capture is culture invariant"); }
        finally { CultureInfo.CurrentCulture = culture; }

        var mutations = new Action<BarsRequest>[]
        {
            r => r.Instrument.FullName = "MES SEP26",
            r => r.Instrument.Expiry = new DateTime(2026, 12, 1),
            r => r.Instrument.MasterInstrument.TickSize = 0.5,
            r => r.Instrument.MasterInstrument.PointValue = 5,
            r => r.MergePolicy = MergePolicy.MergeNonBackAdjusted,
            r => r.MergePolicy = MergePolicy.DoNotMerge,
            r => r.LookupPolicy = LookupPolicies.Provider,
            r => r.LookupPolicy = LookupPolicies.Repository,
            r => r.IsResetOnNewTradingDay = false,
            r => r.FromLocal = r.FromLocal.AddDays(-1),
            r => r.ToLocal = r.ToLocal.AddDays(1),
            r => r.FromLocal = DateTime.SpecifyKind(r.FromLocal, DateTimeKind.Utc),
            r => r.BarsBack = 100,
            r => r.TradingHours.Sessions[0].EndTime = 160000,
            r => r.TradingHours.Holidays.Add(new DateTime(2026, 12, 25), "Closed"),
            r => r.Instrument.MasterInstrument.RolloverCollection[0].Date = new DateTime(2026, 6, 19),
            r => r.Instrument.MasterInstrument.RolloverCollection[0].ContractMonth = new DateTime(2026, 6, 1),
            r => r.Instrument.MasterInstrument.RolloverCollection[0].Offset = 10.500000000000002,
            r => r.Instrument.MasterInstrument.RolloverCollection.RemoveAt(1)
        };
        using (var owner = new OrcaProviderFeedLifetime(OrcaProviderEnvironment.LiveFeed, 1, 4, 2, 2, 2))
        {
            var originalIdentity = Identity(frozen, owner);
            check(originalIdentity.HasHistoricalConfiguration && originalIdentity.HistoricalSnapshotId == HistoryId,
                "history-aware identity records explicit load ownership");
            check(originalIdentity.Key.Equals(Identity(Capture(Request()), owner).Key), "same lineage/configuration creates compatible key");
            check(!originalIdentity.Key.Equals(Identity(frozen, owner, Guid.NewGuid()).Key), "separate history loads do not alias");
            var legacy = new OrcaProviderSourceIdentity(frozen.FullContract, owner.Environment, owner.Epoch,
                frozen.SessionDefinition, frozen.EventTimezone, true, ClassifierId,
                OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection, "platform-unverified:v1");
            check(!legacy.HasHistoricalConfiguration && legacy.HistoricalSnapshotId == Guid.Empty && !legacy.Key.Equals(originalIdentity.Key),
                "legacy unverified identity remains isolated from history-aware identity");
            OrcaProviderPublisherLease publisher; owner.TryAcquire(originalIdentity, out publisher);
            foreach (var mutate in mutations)
            {
                var other = Request(); mutate(other); var changed = Capture(other);
                check(frozen.Definition != changed.Definition, "effective history configuration dimension changes snapshot");
                var otherIdentity = Identity(changed, owner);
                check(!originalIdentity.Key.Equals(otherIdentity.Key), "configuration mismatch changes stream identity");
                OrcaProviderStreamReader rejected;
                check(owner.OpenReader(otherIdentity, out rejected) == OrcaProviderReaderStatus.SourceUnavailable,
                    "incompatible history reader cannot attach");
            }
            publisher.BeginHistoricalPass(); publisher.CompleteHistoricalPassAndBeginLive();
            OrcaProviderStreamReader reader; owner.OpenReader(originalIdentity, out reader);
            using (reader) using (var batch = reader.Read(reader.FirstAvailable, 1))
                check(OrcaProviderHistoryAdmission.Evaluate(batch, new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc)) == OrcaProviderHistoryAdmissionStatus.UtcRangeUnverified,
                    "configuration/lineage capture never certifies an empty platform callback pass");
            Reject(check, () => Identity(frozen, owner, Guid.Empty));
            Reject(check, () => new OrcaProviderSourceIdentity(frozen.FullContract, owner.Environment, owner.Epoch,
                frozen.SessionDefinition, frozen.EventTimezone, true, ClassifierId,
                OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection, "platform-unverified:v1", HistoryId, null));
            Reject(check, () => new OrcaProviderSourceIdentity(frozen.FullContract, owner.Environment, owner.Epoch,
                frozen.SessionDefinition, frozen.EventTimezone, true, ClassifierId,
                OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection, "platform-unverified:v1", HistoryId, " trailing "));
        }
        var shifted = TimeZoneInfo.CreateCustomTimeZone("request-clock", TimeSpan.FromHours(1), "test", "test");
        check(frozen.Definition != OrcaProviderHistoryConfigurationCapture.Capture(Request(), TimeZoneInfo.Utc, shifted).Definition,
            "request-local clock rules participate independently");
        check(frozen.Definition != OrcaProviderHistoryConfigurationCapture.Capture(Request(), shifted, TimeZoneInfo.Utc).Definition,
            "event clock rules participate independently");
        request.Instrument.MasterInstrument.RolloverCollection[0].Offset++;
        check(frozen.Definition != Capture(request).Definition && frozen.Definition == Capture(Request()).Definition,
            "captured strings do not retain mutable request/rollover objects");
        Reject(check, () => frozen.RequireUnchanged(request, TimeZoneInfo.Utc, TimeZoneInfo.Utc));
        var noMerge = Request(); noMerge.MergePolicy = MergePolicy.DoNotMerge; noMerge.Instrument.MasterInstrument.RolloverCollection = null;
        check(Capture(noMerge).Definition != frozen.Definition, "explicit unmerged request does not apply rollover tables");
        var countBack = Request(); countBack.BarsBack = 100; countBack.FromLocal = countBack.ToLocal = DateTime.MinValue;
        check(Capture(countBack).Definition.Contains("count-back"), "count-back request recorded without inventing a UTC interval");
        var sameDay = Request(); sameDay.ToLocal = sameDay.FromLocal;
        check(Capture(sameDay).Definition.Contains("local-trading-days"), "same local day is not rejected as an empty UTC range");
        var invalid = new Action<BarsRequest>[]
        {
            r => r.Instrument = null, r => r.Instrument.MasterInstrument = null, r => r.BarsPeriod = null,
            r => r.Instrument.FullName = " ES SEP26", r => r.Instrument.Expiry = DateTime.MinValue,
            r => r.Instrument.MasterInstrument.InstrumentType = InstrumentType.Stock,
            r => r.BarsPeriod.BarsPeriodType = BarsPeriodType.Minute, r => r.BarsPeriod.Value = 2,
            r => r.BarsPeriod.MarketDataType = MarketDataType.Bid, r => r.IsSplitAdjusted = true, r => r.IsDividendAdjusted = true,
            r => r.MergePolicy = MergePolicy.UseGlobalSettings, r => r.MergePolicy = MergePolicy.UseDefault, r => r.MergePolicy = (MergePolicy)99,
            r => r.LookupPolicy = 0, r => r.LookupPolicy = (LookupPolicies)8, r => r.BarsBack = -1,
            r => r.FromLocal = DateTime.MinValue, r => r.ToLocal = r.FromLocal.AddDays(-1),
            r => r.Instrument.MasterInstrument.TickSize = 0, r => r.Instrument.MasterInstrument.PointValue = double.NaN,
            r => r.Instrument.MasterInstrument.TickSize = double.PositiveInfinity, r => r.TradingHours = null,
            r => r.TradingHours.Name = TradingHours.UseInstrumentSettings,
            r => r.Instrument.MasterInstrument.RolloverCollection.Clear(), r => r.Instrument.MasterInstrument.RolloverCollection = null,
            r => r.Instrument.MasterInstrument.RolloverCollection.Add(r.Instrument.MasterInstrument.RolloverCollection[0]),
            r => r.Instrument.MasterInstrument.RolloverCollection.Add(new Rollover { ContractMonth = new DateTime(2026, 9, 2), Date = new DateTime(2026, 6, 19), Offset = 1 }),
            r => r.Instrument.MasterInstrument.RolloverCollection.Add(null),
            r => r.Instrument.MasterInstrument.RolloverCollection[0].Offset = double.NaN,
            r => r.Instrument.MasterInstrument.RolloverCollection[0].Offset = double.MaxValue,
            r => r.Instrument.MasterInstrument.RolloverCollection[0].ContractMonth = DateTime.MinValue,
            r => r.Instrument.MasterInstrument.RolloverCollection[0].Date = DateTime.MinValue
        };
        foreach (var mutate in invalid) { var bad = Request(); mutate(bad); Reject(check, () => Capture(bad)); }
        var oversized = Request(); for (int i = 0; i < 4096; i++) oversized.Instrument.MasterInstrument.RolloverCollection.Add(new Rollover());
        Reject(check, () => Capture(oversized));
        var changing = Request(); int reads = 0;
        changing.OnMergeRead = () => { if (++reads == 2) changing.MergePolicy = MergePolicy.MergeNonBackAdjusted; };
        Reject(check, () => Capture(changing));
        Reject(check, () => Capture(null));
        Reject(check, () => OrcaProviderHistoryConfigurationCapture.Capture(Request(), null, TimeZoneInfo.Utc));
        Reject(check, () => OrcaProviderHistoryConfigurationCapture.Capture(Request(), TimeZoneInfo.Utc, null));
        Reject(check, () => Identity(frozen, null));
    }
    static void Reject(Action<bool, string> check, Action action)
    {
        bool rejected = false;
        try { action(); } catch (ArgumentException) { rejected = true; } catch (InvalidOperationException) { rejected = true; }
        check(rejected, "incomplete/unresolved/mutated history configuration rejected");
    }
}
