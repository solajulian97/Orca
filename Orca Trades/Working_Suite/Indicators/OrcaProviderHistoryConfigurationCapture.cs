using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NinjaTrader.Cbi;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Configuration of an explicitly owned future BarsRequest, NOT retrospective
    // provenance for chart/cached ticks. Never calls Request, subscribes, or certifies history.
    // Caller serializes initialization and must keep these settings stable through loading.
    public sealed class OrcaProviderHistoryConfigurationCapture
    {
        private const int MaxRollovers = 4096;
        public readonly string FullContract;
        public readonly string Definition;
        public readonly string SessionDefinition;
        public readonly string EventTimezone;

        private OrcaProviderHistoryConfigurationCapture(string contract, string definition, OrcaProviderSessionCapture session)
        { FullContract = contract; Definition = definition; SessionDefinition = session.Definition; EventTimezone = session.EventTimezone; }

        public static OrcaProviderHistoryConfigurationCapture Capture(BarsRequest request,
            TimeZoneInfo eventTimezone, TimeZoneInfo requestLocalTimezone)
        {
            var first = Encode(request, eventTimezone, requestLocalTimezone);
            var second = Encode(request, eventTimezone, requestLocalTimezone);
            if (!string.Equals(first.Definition, second.Definition, StringComparison.Ordinal))
                throw new InvalidOperationException("Historical request configuration changed during capture.");
            return first;
        }

        public void RequireUnchanged(BarsRequest request, TimeZoneInfo eventTimezone, TimeZoneInfo requestLocalTimezone)
        {
            if (!string.Equals(Definition, Capture(request, eventTimezone, requestLocalTimezone).Definition, StringComparison.Ordinal))
                throw new InvalidOperationException("Historical request configuration no longer matches its captured identity.");
        }

        public OrcaProviderSourceIdentity CreateIdentity(OrcaProviderFeedLifetime owner, Guid historicalSnapshotId,
            bool resetClassificationOnSessionBreak, Guid classifierOrigin,
            OrcaProviderClassificationPolicy policy, string quoteSemantics)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            return new OrcaProviderSourceIdentity(FullContract, owner.Environment, owner.Epoch, SessionDefinition,
                EventTimezone, resetClassificationOnSessionBreak, classifierOrigin, policy, quoteSemantics,
                historicalSnapshotId, Definition);
        }

        private static OrcaProviderHistoryConfigurationCapture Encode(BarsRequest request,
            TimeZoneInfo eventTimezone, TimeZoneInfo requestLocalTimezone)
        {
            if (request == null || request.Instrument == null || request.Instrument.MasterInstrument == null
                || request.BarsPeriod == null || eventTimezone == null || requestLocalTimezone == null)
                throw new ArgumentException("Resolved request, instrument, period and timezone metadata required.");
            var instrument = request.Instrument;
            var master = instrument.MasterInstrument;
            string contract = instrument.FullName;
            if (string.IsNullOrWhiteSpace(contract) || contract != contract.Trim() || instrument.Expiry == DateTime.MinValue)
                throw new ArgumentException("An explicit full futures contract and expiry are required.");
            if (master.InstrumentType != InstrumentType.Future)
                throw new ArgumentException("This capture supports futures only; corporate-action schedules are not implemented.");
            if (request.BarsPeriod.BarsPeriodType != BarsPeriodType.Tick || request.BarsPeriod.Value != 1
                || request.BarsPeriod.MarketDataType != MarketDataType.Last)
                throw new ArgumentException("Only explicit Last Tick-1 requests match the exact-event contract.");
            if (request.IsDividendAdjusted || request.IsSplitAdjusted)
                throw new ArgumentException("Adjusted corporate-action history requires a separate verified schedule contract.");
            MergePolicy merge = request.MergePolicy;
            if (merge != MergePolicy.DoNotMerge && merge != MergePolicy.MergeBackAdjusted && merge != MergePolicy.MergeNonBackAdjusted)
                throw new ArgumentException("Resolve an explicit request merge policy; global/default inheritance is not a snapshot.");
            int lookup = (int)request.LookupPolicy;
            int allowedLookup = (int)(LookupPolicies.Provider | LookupPolicies.Repository);
            if (lookup == 0 || (lookup & ~allowedLookup) != 0)
                throw new ArgumentException("An explicit supported lookup policy is required.");
            if (request.BarsBack < 0 || (request.BarsBack == 0
                && (request.FromLocal == DateTime.MinValue || request.ToLocal == DateTime.MinValue || request.ToLocal < request.FromLocal)))
                throw new ArgumentException("An explicit valid count-back or local trading-day request is required.");
            if (!PositiveFinite(master.TickSize) || !PositiveFinite(master.PointValue))
                throw new ArgumentException("Resolved futures tick size and point value required.");

            var session = OrcaProviderSessionCapture.Capture(request.TradingHours, eventTimezone);
            var text = new StringBuilder("nt-futures-history-config-v1:");
            Add(text, contract); AddDate(text, instrument.Expiry);
            Add(text, (int)master.InstrumentType); AddDouble(text, master.TickSize); AddDouble(text, master.PointValue);
            Add(text, "Last-Tick-1"); Add(text, (int)merge); Add(text, lookup);
            Add(text, request.IsDividendAdjusted); Add(text, request.IsSplitAdjusted); Add(text, request.IsResetOnNewTradingDay);
            // Preserve the actual inputs and their kind. Do not reinterpret full trading-day
            // requests as exact UTC coverage or infer a count-back range from today's clock.
            Add(text, request.BarsBack > 0 ? "count-back" : "local-trading-days"); Add(text, request.BarsBack);
            AddDate(text, request.FromLocal); AddDate(text, request.ToLocal);
            Add(text, requestLocalTimezone.ToSerializedString()); Add(text, session.Definition); Add(text, session.EventTimezone);
            AddRollovers(text, master, merge);
            return new OrcaProviderHistoryConfigurationCapture(contract, text.ToString(), session);
        }

        private static void AddRollovers(StringBuilder text, MasterInstrument master, MergePolicy merge)
        {
            if (merge == MergePolicy.DoNotMerge) { Add(text, "rollovers-not-applied"); return; }
            if (master.RolloverCollection == null || master.RolloverCollection.Count == 0 || master.RolloverCollection.Count > MaxRollovers)
                throw new ArgumentException("Merged history requires a bounded nonempty rollover schedule.");
            var rows = new List<RolloverValue>();
            var contracts = new HashSet<long>();
            foreach (var rollover in master.RolloverCollection)
            {
                if (rollover == null || rows.Count >= MaxRollovers)
                    throw new ArgumentException("Incomplete or oversized rollover schedule.");
                var row = new RolloverValue(rollover.ContractMonth, rollover.Date, rollover.Offset);
                if (row.Month == DateTime.MinValue || row.Date == DateTime.MinValue
                    || double.IsNaN(row.Offset) || double.IsInfinity(row.Offset)
                    || row.Offset == double.MinValue || row.Offset == double.MaxValue
                    || !contracts.Add(row.Month.Year * 12L + row.Month.Month))
                    throw new ArgumentException("Unresolved or duplicate rollover entry.");
                rows.Add(row);
            }
            Add(text, rows.Count);
            foreach (var row in rows.OrderBy(r => r.Month.Ticks))
            { AddDate(text, row.Month); AddDate(text, row.Date); AddDouble(text, row.Offset); }
        }

        private struct RolloverValue
        {
            public readonly DateTime Month, Date;
            public readonly double Offset;
            public RolloverValue(DateTime month, DateTime date, double offset) { Month = month; Date = date; Offset = offset; }
        }
        private static bool PositiveFinite(double value) { return value > 0 && !double.IsInfinity(value); }
        private static void AddDate(StringBuilder text, DateTime value) { Add(text, value.Ticks); Add(text, (int)value.Kind); }
        private static void AddDouble(StringBuilder text, double value) { Add(text, BitConverter.DoubleToInt64Bits(value)); }
        private static void Add(StringBuilder text, object value)
        {
            string valueText = Convert.ToString(value, CultureInfo.InvariantCulture);
            text.Append(valueText.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(valueText);
        }
    }
}
