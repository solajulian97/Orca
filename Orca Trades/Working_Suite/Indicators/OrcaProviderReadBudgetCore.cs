using System;
using System.Collections;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.Indicators
{
    public sealed class OrcaProviderBudgetExceededException : InvalidOperationException
    {
        public OrcaProviderBudgetExceededException() : base("Outstanding provider read budget exhausted; dispose a batch before retrying.") { }
    }

    internal sealed class OrcaProviderReadBudget
    {
        private readonly object sync = new object();
        private readonly int maxReaders;
        private readonly int maxBatches;
        private int readers;
        private int batches;
        internal OrcaProviderReadBudget(int maxReaders, int maxBatches)
        { this.maxReaders = maxReaders; this.maxBatches = maxBatches; }
        internal bool TryReader() { lock (sync) { if (readers >= maxReaders) return false; readers++; return true; } }
        internal void ReleaseReader() { lock (sync) readers--; }
        internal bool TryBatch() { lock (sync) { if (batches >= maxBatches) return false; batches++; return true; } }
        internal void ReleaseBatch() { lock (sync) batches--; }
    }

    // The array never escapes. A retained Events wrapper cannot keep disposed payload alive.
    // Values explicitly copied out by a consumer belong to that consumer's memory budget.
    public sealed class OrcaProviderBatchLease : IDisposable
    {
        private readonly object sync = new object();
        private OrcaStreamBatch batch;
        private readonly OrcaProviderReadBudget budget;
        public readonly OrcaStreamReadStatus Status;
        public readonly OrcaStreamCursor FirstAvailable;
        public readonly OrcaStreamCursor Next;
        public readonly long PublishedThroughExclusive;
        public readonly OrcaStreamCoverage Coverage;
        public readonly IReadOnlyList<OrcaStreamTick> Events;

        internal OrcaProviderBatchLease(OrcaStreamBatch batch, OrcaProviderReadBudget budget)
        {
            this.batch = batch; this.budget = budget;
            Status = batch.Status; FirstAvailable = batch.FirstAvailable; Next = batch.Next;
            PublishedThroughExclusive = batch.PublishedThroughExclusive; Coverage = batch.Coverage;
            Events = new EventView(this);
        }
        private int Count { get { lock (sync) { RequireOpen(); return batch.Events.Count; } } }
        private OrcaStreamTick At(int index) { lock (sync) { RequireOpen(); return batch.Events[index]; } }
        private void RequireOpen() { if (batch == null) throw new ObjectDisposedException("OrcaProviderBatchLease"); }
        public void Dispose()
        {
            lock (sync)
            {
                if (batch == null) return;
                batch = null;
                budget.ReleaseBatch();
            }
        }
        private sealed class EventView : IReadOnlyList<OrcaStreamTick>
        {
            private readonly OrcaProviderBatchLease owner;
            internal EventView(OrcaProviderBatchLease owner) { this.owner = owner; }
            public int Count { get { return owner.Count; } }
            public OrcaStreamTick this[int index] { get { return owner.At(index); } }
            public IEnumerator<OrcaStreamTick> GetEnumerator()
            { for (int i = 0; i < Count; i++) yield return this[i]; }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
    }
}
