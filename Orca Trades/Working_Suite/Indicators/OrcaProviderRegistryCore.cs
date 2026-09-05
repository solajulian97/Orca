using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Exact-tick capability is implicit in this registry. All IDs are supplied by the
    // future platform adapter; never infer equivalence from instrument root alone.
    public sealed class OrcaProviderStreamKey : IEquatable<OrcaProviderStreamKey>
    {
        public readonly string Contract;
        public readonly string DataEnvironment;
        public readonly string SessionDefinition;
        public readonly string TimeBasis;
        public readonly string ClassificationPolicy;

        public OrcaProviderStreamKey(string contract, string dataEnvironment,
            string sessionDefinition, string timeBasis, string classificationPolicy)
        {
            Contract = Required(contract); DataEnvironment = Required(dataEnvironment);
            SessionDefinition = Required(sessionDefinition); TimeBasis = Required(timeBasis);
            ClassificationPolicy = Required(classificationPolicy);
        }
        private static string Required(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
                throw new ArgumentException("A nonempty canonical stream identity is required.");
            return value;
        }
        public bool Equals(OrcaProviderStreamKey other)
        {
            return other != null && Contract == other.Contract && DataEnvironment == other.DataEnvironment
                && SessionDefinition == other.SessionDefinition && TimeBasis == other.TimeBasis
                && ClassificationPolicy == other.ClassificationPolicy;
        }
        public override bool Equals(object other) { return Equals(other as OrcaProviderStreamKey); }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Contract.GetHashCode();
                hash = hash * 31 + DataEnvironment.GetHashCode();
                hash = hash * 31 + SessionDefinition.GetHashCode();
                hash = hash * 31 + TimeBasis.GetHashCode();
                return hash * 31 + ClassificationPolicy.GetHashCode();
            }
        }
    }

    public enum OrcaProviderAcquireStatus { Acquired, Occupied, CapacityReached, Closed }
    public enum OrcaProviderReaderStatus { Opened, SourceUnavailable, CapacityReached, Closed }

    // A pinned reader cannot publish, close a producer, or silently attach to its successor.
    public sealed class OrcaProviderStreamReader : IDisposable
    {
        private readonly object sync = new object();
        private readonly OrcaProviderStreamBuffer buffer;
        private readonly OrcaProviderReadBudget budget;
        private bool closed;
        internal OrcaProviderStreamReader(OrcaProviderStreamBuffer buffer, OrcaProviderReadBudget budget)
        { this.buffer = buffer; this.budget = budget; }
        public OrcaStreamCursor FirstAvailable
        { get { lock (sync) { RequireOpen(); return buffer.FirstAvailable; } } }
        public OrcaProviderBatchLease Read(OrcaStreamCursor cursor, int requestedEvents)
        {
            lock (sync)
            {
                RequireOpen();
                if (requestedEvents <= 0) throw new ArgumentOutOfRangeException("requestedEvents");
                if (!budget.TryBatch()) throw new OrcaProviderBudgetExceededException();
                try { return new OrcaProviderBatchLease(buffer.Read(cursor, requestedEvents), budget); }
                catch { budget.ReleaseBatch(); throw; }
            }
        }
        private void RequireOpen() { if (closed) throw new ObjectDisposedException("OrcaProviderStreamReader"); }
        public void Dispose()
        { lock (sync) { if (closed) return; closed = true; budget.ReleaseReader(); } }
    }

    public sealed class OrcaProviderPublisherLease : IDisposable
    {
        private OrcaProviderStreamRegistry registry;
        internal readonly OrcaProviderStreamKey Key;
        internal readonly OrcaProviderStreamBuffer Buffer;
        internal OrcaProviderPublisherLease(OrcaProviderStreamRegistry registry,
            OrcaProviderStreamKey key, OrcaProviderStreamBuffer buffer)
        { this.registry = registry; Key = key; Buffer = buffer; }
        public long Append(OrcaStreamTick tick) { return Buffer.Append(tick); }
        public void BeginHistory(DateTime fromUtc, DateTime toUtcExclusive) { Buffer.BeginHistory(fromUtc, toUtcExclusive); }
        public void ConfirmHistory() { Buffer.ConfirmHistory(); }
        public void BeginLive() { Buffer.BeginLive(); }
        public void MarkFault(OrcaStreamFault reason) { Buffer.MarkFault(reason); }
        public void Dispose()
        {
            // Keep the reference until Release completes so every concurrent Dispose
            // returns only after closure, including callers racing the first release.
            var owner = System.Threading.Interlocked.CompareExchange(ref registry, null, null);
            if (owner == null) return;
            owner.Release(this);
            DetachRegistry();
        }
        internal void DetachRegistry() { System.Threading.Interlocked.Exchange(ref registry, null); }
    }

    // Explicit service lifetime, not a global static registry. No platform objects,
    // timers, user callbacks or market subscriptions are retained or invoked here.
    public sealed class OrcaProviderStreamRegistry : IDisposable
    {
        private readonly object sync = new object();
        private readonly Dictionary<OrcaProviderStreamKey, OrcaProviderPublisherLease> publishers
            = new Dictionary<OrcaProviderStreamKey, OrcaProviderPublisherLease>();
        private readonly int maxStreams;
        private readonly int eventsPerStream;
        private readonly int maxReadEvents;
        private readonly OrcaProviderReadBudget readBudget;
        private bool closed;

        public OrcaProviderStreamRegistry(int maxStreams, int eventsPerStream, int maxReadEvents)
            : this(maxStreams, eventsPerStream, maxReadEvents, 128, 256) { }

        public OrcaProviderStreamRegistry(int maxStreams, int eventsPerStream, int maxReadEvents,
            int maxReaders, int maxOutstandingBatches)
        {
            if (maxStreams <= 0 || eventsPerStream <= 0 || maxReadEvents <= 0 || maxReadEvents > eventsPerStream
                || maxReaders <= 0 || maxOutstandingBatches <= 0)
                throw new ArgumentOutOfRangeException("maxStreams", "Require positive bounds and read limit <= stream capacity.");
            this.maxStreams = maxStreams; this.eventsPerStream = eventsPerStream; this.maxReadEvents = maxReadEvents;
            readBudget = new OrcaProviderReadBudget(maxReaders, maxOutstandingBatches);
        }
        public int ActiveStreams { get { lock (sync) return publishers.Count; } }

        public OrcaProviderAcquireStatus TryAcquire(OrcaProviderStreamKey key, out OrcaProviderPublisherLease lease)
        {
            if (key == null) throw new ArgumentNullException("key");
            lock (sync)
            {
                lease = null;
                if (closed) return OrcaProviderAcquireStatus.Closed;
                if (publishers.ContainsKey(key)) return OrcaProviderAcquireStatus.Occupied;
                if (publishers.Count >= maxStreams) return OrcaProviderAcquireStatus.CapacityReached;
                lease = new OrcaProviderPublisherLease(this, key, new OrcaProviderStreamBuffer(eventsPerStream, maxReadEvents));
                publishers.Add(key, lease);
                return OrcaProviderAcquireStatus.Acquired;
            }
        }

        public bool TryOpenReader(OrcaProviderStreamKey key, out OrcaProviderStreamReader reader)
        { return OpenReader(key, out reader) == OrcaProviderReaderStatus.Opened; }

        public OrcaProviderReaderStatus OpenReader(OrcaProviderStreamKey key, out OrcaProviderStreamReader reader)
        {
            if (key == null) throw new ArgumentNullException("key");
            lock (sync)
            {
                reader = null;
                OrcaProviderPublisherLease lease;
                if (closed) return OrcaProviderReaderStatus.Closed;
                if (!publishers.TryGetValue(key, out lease)) return OrcaProviderReaderStatus.SourceUnavailable;
                if (!readBudget.TryReader()) return OrcaProviderReaderStatus.CapacityReached;
                try { reader = new OrcaProviderStreamReader(lease.Buffer, readBudget); }
                catch { readBudget.ReleaseReader(); throw; }
                return OrcaProviderReaderStatus.Opened;
            }
        }

        internal void Release(OrcaProviderPublisherLease lease)
        {
            lock (sync)
            {
                OrcaProviderPublisherLease current;
                if (!publishers.TryGetValue(lease.Key, out current) || !ReferenceEquals(current, lease)) return;
                // Close before making this key available: no overlapping active generations.
                // Lock order is registry -> buffer; append/read never enter the registry.
                lease.Buffer.Dispose();
                publishers.Remove(lease.Key);
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (closed) return;
                closed = true;
                foreach (var lease in publishers.Values)
                {
                    lease.Buffer.Dispose();
                    lease.DetachRegistry();
                }
                publishers.Clear();
            }
        }
    }
}
