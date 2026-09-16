using System;
using System.Collections.ObjectModel;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Platform-independent exact-event storage. No chart, account or subscription ownership.
    public enum OrcaStreamReadStatus { Ready, Evicted, WrongGeneration, Ahead, Closed, SourceFaulted }
    public enum OrcaStreamClassification { Unknown, TickDirection, BidAsk }
    public enum OrcaStreamPhase { Unverified, Historical, HistoryConfirmed, Live, Faulted, Closed }
    public enum OrcaStreamFault { None, HistoricalGap, SourceDisconnected, InvalidEvent, IngestionFailed }
    public enum OrcaStreamCoverageKind { Unverified, RequestedUtcRange, SourceLifecycle }

    public sealed class OrcaStreamCoverage
    {
        public readonly OrcaStreamPhase Phase;
        public readonly OrcaStreamFault Fault;
        public readonly OrcaStreamCoverageKind Kind;
        public readonly DateTime HistoryFromUtc;
        public readonly DateTime HistoryToUtcExclusive;
        public readonly bool ProducerConfirmedHistory;
        public readonly bool FullConfirmedHistoryRetained;
        public readonly long HistoryEndSequenceExclusive;
        public readonly bool HistoricalPassCompleted;
        public readonly bool FullHistoricalPassRetained;
        internal OrcaStreamCoverage(OrcaStreamPhase phase, OrcaStreamFault fault, DateTime from, DateTime to,
            bool confirmed, long historyEnd, long firstSequence, OrcaStreamCoverageKind kind)
        {
            Phase = phase; Fault = fault; HistoryFromUtc = from; HistoryToUtcExclusive = to;
            Kind = kind;
            HistoricalPassCompleted = confirmed;
            ProducerConfirmedHistory = confirmed && kind == OrcaStreamCoverageKind.RequestedUtcRange;
            HistoryEndSequenceExclusive = historyEnd;
            FullHistoricalPassRetained = confirmed && (historyEnd == 0 || firstSequence == 0)
                && phase != OrcaStreamPhase.Closed && phase != OrcaStreamPhase.Faulted;
            FullConfirmedHistoryRetained = FullHistoricalPassRetained && ProducerConfirmedHistory;
        }
    }

    public struct OrcaStreamTick
    {
        public readonly DateTime Time;
        public readonly double Price;
        public readonly long Volume;
        public readonly long SignedVolume;
        public readonly OrcaStreamClassification Classification;

        public OrcaStreamTick(DateTime time, double price, long volume, long signedVolume,
            OrcaStreamClassification classification)
        {
            if (time == DateTime.MinValue || double.IsNaN(price) || double.IsInfinity(price)
                || volume <= 0 || signedVolume > volume || signedVolume < -volume
                || classification < OrcaStreamClassification.Unknown || classification > OrcaStreamClassification.BidAsk
                || (classification == OrcaStreamClassification.Unknown && signedVolume != 0))
                throw new ArgumentException("Invalid exact-stream event.");
            Time = time; Price = price; Volume = volume; SignedVolume = signedVolume;
            Classification = classification;
        }
    }

    public struct OrcaStreamCursor
    {
        public readonly Guid Generation;
        public readonly long Sequence;
        public OrcaStreamCursor(Guid generation, long sequence) { Generation = generation; Sequence = sequence; }
    }

    public sealed class OrcaStreamBatch
    {
        public readonly OrcaStreamReadStatus Status;
        public readonly OrcaStreamCursor FirstAvailable;
        public readonly OrcaStreamCursor Next;
        public readonly long PublishedThroughExclusive;
        public readonly ReadOnlyCollection<OrcaStreamTick> Events;
        public readonly OrcaStreamCoverage Coverage;
        internal OrcaStreamBatch(OrcaStreamReadStatus status, OrcaStreamCursor first,
            OrcaStreamCursor next, long published, OrcaStreamTick[] events, OrcaStreamCoverage coverage)
        {
            Status = status; FirstAvailable = first; Next = next;
            PublishedThroughExclusive = published; Events = Array.AsReadOnly(events);
            Coverage = coverage;
        }
    }

    // A fixed-size ring is the initial bounded implementation. Reads copy immutable values;
    // it is NOT a zero-copy segment store, a history completeness claim, or a deduplicator.
    public sealed class OrcaProviderStreamBuffer : IDisposable
    {
        private static readonly OrcaStreamTick[] Empty = new OrcaStreamTick[0];
        private readonly object sync = new object();
        private readonly Guid generation = Guid.NewGuid();
        private OrcaStreamTick[] ring;
        private readonly int maxReadEvents;
        private long firstSequence;
        private long nextSequence;
        private bool closed;
        private OrcaStreamPhase phase;
        private OrcaStreamFault fault;
        private OrcaStreamCoverageKind coverageKind;
        private DateTime historyFromUtc;
        private DateTime historyToUtc;
        private bool historyConfirmed;
        private long historyEndSequence = -1;

        public OrcaProviderStreamBuffer(int capacity, int maxReadEvents)
        {
            if (capacity <= 0 || maxReadEvents <= 0 || maxReadEvents > capacity)
                throw new ArgumentOutOfRangeException("capacity", "Require 0 < maxReadEvents <= capacity.");
            ring = new OrcaStreamTick[capacity];
            this.maxReadEvents = maxReadEvents;
        }

        public OrcaStreamCursor FirstAvailable
        {
            get { lock (sync) return new OrcaStreamCursor(generation, firstSequence); }
        }

        public long Append(OrcaStreamTick tick)
        { return AppendCore(tick, null); }

        public long AppendHistorical(OrcaStreamTick tick)
        { return AppendCore(tick, OrcaStreamPhase.Historical); }

        public long AppendLive(OrcaStreamTick tick)
        { return AppendCore(tick, OrcaStreamPhase.Live); }

        private long AppendCore(OrcaStreamTick tick, OrcaStreamPhase? expectedPhase)
        {
            // Reject default(struct), which bypasses the event constructor.
            if (tick.Volume <= 0 || tick.Time == DateTime.MinValue)
                throw new ArgumentException("Uninitialized event.");
            lock (sync)
            {
                if (closed) throw new ObjectDisposedException("OrcaProviderStreamBuffer");
                if (coverageKind == OrcaStreamCoverageKind.SourceLifecycle && !expectedPhase.HasValue)
                    throw new InvalidOperationException("Lifecycle ingestion requires an explicit historical/live lane.");
                if (expectedPhase.HasValue && phase != expectedPhase.Value)
                    throw new InvalidOperationException("Callback lane does not match the source lifecycle.");
                if (phase == OrcaStreamPhase.Faulted || phase == OrcaStreamPhase.HistoryConfirmed)
                    throw new InvalidOperationException("Stream is not accepting events in this phase.");
                if ((phase == OrcaStreamPhase.Historical || phase == OrcaStreamPhase.Live) && tick.Time.Kind != DateTimeKind.Utc)
                    throw new ArgumentException("Verified ingestion lanes require UTC event timestamps.");
                if (coverageKind == OrcaStreamCoverageKind.RequestedUtcRange && phase == OrcaStreamPhase.Historical
                    && (tick.Time < historyFromUtc || tick.Time >= historyToUtc))
                    throw new ArgumentException("Historical event is outside the declared UTC half-open interval.");
                if (coverageKind == OrcaStreamCoverageKind.RequestedUtcRange && phase == OrcaStreamPhase.Live && tick.Time < historyToUtc)
                    throw new ArgumentException("Live event precedes the declared handoff boundary.");
                if (nextSequence == long.MaxValue) throw new InvalidOperationException("Sequence exhausted; replace generation.");
                long sequence = nextSequence;
                ring[(int)(sequence % ring.Length)] = tick;
                nextSequence++;
                if (nextSequence - firstSequence > ring.Length) firstSequence++;
                return sequence;
            }
        }

        public void BeginHistory(DateTime fromUtc, DateTime toUtcExclusive)
        {
            if (fromUtc.Kind != DateTimeKind.Utc || toUtcExclusive.Kind != DateTimeKind.Utc
                || fromUtc == DateTime.MinValue || toUtcExclusive <= fromUtc)
                throw new ArgumentException("History requires a nonempty UTC half-open interval.");
            lock (sync)
            {
                RequirePhase(OrcaStreamPhase.Unverified);
                if (nextSequence != 0) throw new InvalidOperationException("Cannot certify preexisting unverified events.");
                historyFromUtc = fromUtc; historyToUtc = toUtcExclusive;
                coverageKind = OrcaStreamCoverageKind.RequestedUtcRange;
                phase = OrcaStreamPhase.Historical;
            }
        }

        // This is a producer assertion, never inferred from event counts or timestamps.
        // The platform adapter must establish complete source coverage before calling it.
        public void ConfirmHistory()
        {
            lock (sync)
            {
                RequirePhase(OrcaStreamPhase.Historical);
                if (coverageKind != OrcaStreamCoverageKind.RequestedUtcRange)
                    throw new InvalidOperationException("Lifecycle completion cannot certify a clock-time range.");
                historyEndSequence = nextSequence; historyConfirmed = true;
                phase = OrcaStreamPhase.HistoryConfirmed;
            }
        }

        public void BeginLive()
        {
            lock (sync)
            {
                RequirePhase(OrcaStreamPhase.HistoryConfirmed);
                phase = OrcaStreamPhase.Live;
            }
        }

        public void BeginHistoricalPass()
        {
            lock (sync)
            {
                RequireEmptyUnverified();
                coverageKind = OrcaStreamCoverageKind.SourceLifecycle;
                phase = OrcaStreamPhase.Historical;
            }
        }

        // Adapter must serialize its callbacks and call this only after its historical
        // lane is drained. No time cutoff, sorting, boundary suppression or cache merge.
        public void CompleteHistoricalPassAndBeginLive()
        {
            lock (sync)
            {
                RequirePhase(OrcaStreamPhase.Historical);
                if (coverageKind != OrcaStreamCoverageKind.SourceLifecycle)
                    throw new InvalidOperationException("Wrong handoff contract.");
                historyEndSequence = nextSequence;
                historyConfirmed = true;
                phase = OrcaStreamPhase.Live;
            }
        }

        public void BeginLiveOnly()
        {
            lock (sync)
            {
                RequireEmptyUnverified();
                coverageKind = OrcaStreamCoverageKind.SourceLifecycle;
                phase = OrcaStreamPhase.Live;
                // No historical pass or range has been confirmed.
            }
        }

        private void RequireEmptyUnverified()
        {
            RequirePhase(OrcaStreamPhase.Unverified);
            if (nextSequence != 0) throw new InvalidOperationException("Cannot certify preexisting unverified events.");
        }

        public void MarkFault(OrcaStreamFault reason)
        {
            if (reason <= OrcaStreamFault.None || reason > OrcaStreamFault.IngestionFailed)
                throw new ArgumentOutOfRangeException("reason");
            lock (sync)
            {
                if (closed) throw new ObjectDisposedException("OrcaProviderStreamBuffer");
                if (phase == OrcaStreamPhase.Faulted) return; // retain the first cause
                fault = reason; phase = OrcaStreamPhase.Faulted;
            }
        }

        private void RequirePhase(OrcaStreamPhase expected)
        {
            if (closed) throw new ObjectDisposedException("OrcaProviderStreamBuffer");
            if (phase != expected) throw new InvalidOperationException("Invalid stream phase transition.");
        }

        public OrcaStreamBatch Read(OrcaStreamCursor cursor, int requestedEvents)
        {
            if (requestedEvents <= 0) throw new ArgumentOutOfRangeException("requestedEvents");
            lock (sync)
            {
                var first = new OrcaStreamCursor(generation, firstSequence);
                var coverage = new OrcaStreamCoverage(phase, fault, historyFromUtc, historyToUtc,
                    historyConfirmed, historyEndSequence, firstSequence, coverageKind);
                OrcaStreamReadStatus status = closed ? OrcaStreamReadStatus.Closed
                    : cursor.Generation != generation ? OrcaStreamReadStatus.WrongGeneration
                    : phase == OrcaStreamPhase.Faulted ? OrcaStreamReadStatus.SourceFaulted
                    : cursor.Sequence < firstSequence ? OrcaStreamReadStatus.Evicted
                    : cursor.Sequence > nextSequence ? OrcaStreamReadStatus.Ahead
                    : OrcaStreamReadStatus.Ready;
                // Fail closed. Recovery must be an explicit consumer decision, never silent skipping.
                if (status != OrcaStreamReadStatus.Ready)
                    return new OrcaStreamBatch(status, first, cursor, nextSequence, Empty, coverage);
                int count = (int)Math.Min(nextSequence - cursor.Sequence, Math.Min(requestedEvents, maxReadEvents));
                var events = new OrcaStreamTick[count];
                for (int i = 0; i < count; i++)
                    events[i] = ring[(int)((cursor.Sequence + i) % ring.Length)];
                return new OrcaStreamBatch(status, first,
                    new OrcaStreamCursor(generation, cursor.Sequence + count), nextSequence, events, coverage);
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (closed) return;
                closed = true;
                phase = OrcaStreamPhase.Closed;
                // Closed reader handles must not retain the large payload allocation.
                ring = null;
                firstSequence = nextSequence;
            }
        }
    }
}
