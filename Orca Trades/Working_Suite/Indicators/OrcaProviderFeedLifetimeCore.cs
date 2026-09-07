using System;

namespace NinjaTrader.NinjaScript.Indicators
{
    // An explicitly owned connection/replay continuity interval. No automatic reconnect,
    // platform references, subscription ownership or process-global discovery is implied.
    public sealed class OrcaProviderFeedLifetime : IDisposable
    {
        private readonly OrcaProviderStreamRegistry registry;
        public readonly Guid Epoch = Guid.NewGuid();
        public readonly OrcaProviderEnvironment Environment;

        public OrcaProviderFeedLifetime(OrcaProviderEnvironment environment, int maxStreams,
            int eventsPerStream, int maxReadEvents, int maxReaders, int maxOutstandingBatches)
        {
            if (!Enum.IsDefined(typeof(OrcaProviderEnvironment), environment))
                throw new ArgumentOutOfRangeException("environment");
            Environment = environment;
            registry = new OrcaProviderStreamRegistry(maxStreams, eventsPerStream, maxReadEvents, maxReaders, maxOutstandingBatches);
        }

        public int ActiveStreams { get { return registry.ActiveStreams; } }

        public OrcaProviderAcquireStatus TryAcquire(OrcaProviderSourceIdentity identity, out OrcaProviderPublisherLease publisher)
        {
            RequireIdentity(identity);
            return registry.TryAcquire(identity.Key, out publisher);
        }

        public OrcaProviderReaderStatus OpenReader(OrcaProviderSourceIdentity identity, out OrcaProviderStreamReader reader)
        {
            RequireIdentity(identity);
            return registry.OpenReader(identity.Key, out reader);
        }

        private void RequireIdentity(OrcaProviderSourceIdentity identity)
        {
            if (identity == null) throw new ArgumentNullException("identity");
            if (identity.ConnectionEpoch != Epoch || identity.Environment != Environment)
                throw new ArgumentException("Identity belongs to another feed lifetime or environment.", "identity");
        }

        // Closure is terminal. A new connection/replay run requires a new owner and epoch.
        // Existing publisher/read handles are closed by the registry before Dispose returns.
        // Already delivered immutable batch leases remain consumer-owned until disposed.
        public void Dispose() { registry.Dispose(); }
    }
}
