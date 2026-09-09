using System;

namespace NinjaTrader.NinjaScript.Indicators
{
    public enum OrcaProviderHistoryAdmissionStatus
    {
        ReadUnavailable,
        UtcRangeUnverified,
        HistoricalPassPending,
        RequiredRangeNotCovered,
        ConfirmedHistoryEvicted,
        Ready
    }

    // A necessary history-availability gate for a future consumer, not source authentication,
    // completed consumer initialization, or permission to drop the consumer's local series.
    // Evaluates one immutable read snapshot. Recheck each subsequent read's generation/status;
    // this result cannot reserve history, recover an evicted cursor or certify future continuity.
    public static class OrcaProviderHistoryAdmission
    {
        public static OrcaProviderHistoryAdmissionStatus Evaluate(OrcaProviderBatchLease batch,
            DateTime requiredFromUtc, DateTime requiredToUtcExclusive)
        {
            if (batch == null) throw new ArgumentNullException("batch");
            // Disposed leases retain metadata for diagnostics, not an active read entitlement.
            _ = batch.Events.Count;
            return EvaluateCore(batch.Status, batch.Coverage, requiredFromUtc, requiredToUtcExclusive);
        }

        public static OrcaProviderHistoryAdmissionStatus Evaluate(OrcaStreamBatch batch,
            DateTime requiredFromUtc, DateTime requiredToUtcExclusive)
        {
            if (batch == null) throw new ArgumentNullException("batch");
            return EvaluateCore(batch.Status, batch.Coverage, requiredFromUtc, requiredToUtcExclusive);
        }

        private static OrcaProviderHistoryAdmissionStatus EvaluateCore(OrcaStreamReadStatus readStatus,
            OrcaStreamCoverage coverage, DateTime requiredFromUtc, DateTime requiredToUtcExclusive)
        {
            if (requiredFromUtc.Kind != DateTimeKind.Utc || requiredToUtcExclusive.Kind != DateTimeKind.Utc
                || requiredFromUtc == DateTime.MinValue || requiredToUtcExclusive <= requiredFromUtc)
                throw new ArgumentException("Required history must be a nonempty UTC half-open interval.");
            if (readStatus != OrcaStreamReadStatus.Ready || coverage == null
                || coverage.Phase == OrcaStreamPhase.Closed || coverage.Phase == OrcaStreamPhase.Faulted)
                return OrcaProviderHistoryAdmissionStatus.ReadUnavailable;
            // Event counts, first/last trade timestamps, Tick Replay settings and completion
            // of a callback lifecycle cannot prove a requested clock-time interval.
            if (coverage.Kind != OrcaStreamCoverageKind.RequestedUtcRange)
                return OrcaProviderHistoryAdmissionStatus.UtcRangeUnverified;
            if (!coverage.ProducerConfirmedHistory)
                return OrcaProviderHistoryAdmissionStatus.HistoricalPassPending;
            if (requiredFromUtc < coverage.HistoryFromUtc || requiredToUtcExclusive > coverage.HistoryToUtcExclusive)
                return OrcaProviderHistoryAdmissionStatus.RequiredRangeNotCovered;
            // Conservative: no timestamp-based attempt to salvage a retained subset after
            // prefix eviction. A genuinely confirmed empty no-trade interval is still valid.
            if (!coverage.FullConfirmedHistoryRetained)
                return OrcaProviderHistoryAdmissionStatus.ConfirmedHistoryEvicted;
            return OrcaProviderHistoryAdmissionStatus.Ready;
        }
    }
}
