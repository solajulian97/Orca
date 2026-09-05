using System;
using System.Collections.ObjectModel;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Platform-independent exact-event storage. No chart, account or subscription ownership.
    public enum OrcaStreamReadStatus { Ready, Evicted, WrongGeneration, Ahead, Closed }
    public enum OrcaStreamClassification { Unknown, TickDirection, BidAsk }

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
        internal OrcaStreamBatch(OrcaStreamReadStatus status, OrcaStreamCursor first,
            OrcaStreamCursor next, long published, OrcaStreamTick[] events)
        {
            Status = status; FirstAvailable = first; Next = next;
            PublishedThroughExclusive = published; Events = Array.AsReadOnly(events);
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
        {
            // Reject default(struct), which bypasses the event constructor.
            if (tick.Volume <= 0 || tick.Time == DateTime.MinValue)
                throw new ArgumentException("Uninitialized event.");
            lock (sync)
            {
                if (closed) throw new ObjectDisposedException("OrcaProviderStreamBuffer");
                if (nextSequence == long.MaxValue) throw new InvalidOperationException("Sequence exhausted; replace generation.");
                long sequence = nextSequence;
                ring[(int)(sequence % ring.Length)] = tick;
                nextSequence++;
                if (nextSequence - firstSequence > ring.Length) firstSequence++;
                return sequence;
            }
        }

        public OrcaStreamBatch Read(OrcaStreamCursor cursor, int requestedEvents)
        {
            if (requestedEvents <= 0) throw new ArgumentOutOfRangeException("requestedEvents");
            lock (sync)
            {
                var first = new OrcaStreamCursor(generation, firstSequence);
                OrcaStreamReadStatus status = closed ? OrcaStreamReadStatus.Closed
                    : cursor.Generation != generation ? OrcaStreamReadStatus.WrongGeneration
                    : cursor.Sequence < firstSequence ? OrcaStreamReadStatus.Evicted
                    : cursor.Sequence > nextSequence ? OrcaStreamReadStatus.Ahead
                    : OrcaStreamReadStatus.Ready;
                // Fail closed. Recovery must be an explicit consumer decision, never silent skipping.
                if (status != OrcaStreamReadStatus.Ready)
                    return new OrcaStreamBatch(status, first, cursor, nextSequence, Empty);
                int count = (int)Math.Min(nextSequence - cursor.Sequence, Math.Min(requestedEvents, maxReadEvents));
                var events = new OrcaStreamTick[count];
                for (int i = 0; i < count; i++)
                    events[i] = ring[(int)((cursor.Sequence + i) % ring.Length)];
                return new OrcaStreamBatch(status, first,
                    new OrcaStreamCursor(generation, cursor.Sequence + count), nextSequence, events);
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (closed) return;
                closed = true;
                // Closed reader handles must not retain the large payload allocation.
                ring = null;
                firstSequence = nextSequence;
            }
        }
    }
}
