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

    // A pinned reader cannot publish, close a producer, or silently attach to its successor.
    public sealed class OrcaProviderStreamReader
    {
        private readonly OrcaProviderStreamBuffer buffer;
        internal OrcaProviderStreamReader(OrcaProviderStreamBuffer buffer) { this.buffer = buffer; }
        public OrcaStreamCursor FirstAvailable { get { return buffer.FirstAvailable; } }
        public OrcaStreamBatch Read(OrcaStreamCursor cursor, int requestedEvents)
        { return buffer.Read(cursor, requestedEvents); }
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
        private bool closed;

        public OrcaProviderStreamRegistry(int maxStreams, int eventsPerStream, int maxReadEvents)
        {
            if (maxStreams <= 0 || eventsPerStream <= 0 || maxReadEvents <= 0 || maxReadEvents > eventsPerStream)
                throw new ArgumentOutOfRangeException("maxStreams", "Require positive bounds and read limit <= stream capacity.");
            this.maxStreams = maxStreams; this.eventsPerStream = eventsPerStream; this.maxReadEvents = maxReadEvents;
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
        {
            if (key == null) throw new ArgumentNullException("key");
            lock (sync)
            {
                reader = null;
                OrcaProviderPublisherLease lease;
                if (closed || !publishers.TryGetValue(key, out lease)) return false;
                reader = new OrcaProviderStreamReader(lease.Buffer);
                return true;
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
