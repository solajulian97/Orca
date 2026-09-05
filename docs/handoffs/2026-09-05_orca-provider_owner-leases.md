# Exact-stream registry and publisher leases

## Objective

Continue the approved provider foundation with compatible identities, exclusive logical publisher ownership and explicit service shutdown. This is isolated core work; it does not yet register NinjaTrader subscriptions or replace the old provider.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderRegistryCore.cs`: new identity, registry, publisher lease and pinned reader API.
- `Orca Trades/Working_Suite/Indicators/OrcaProviderStreamCore.cs`: release ring payload on close instead of retaining a cleared array.
- `tests/OrcaProviderStream.Tests/Program.cs` and project: linked registry plus ownership/race tests.
- This handoff.

## Architecture / behavior

The non-static registry requires explicit service lifetime. Exact, ordinal keys contain full contract, data environment, session definition, time basis and classification policy. Empty/whitespace-padded fields are rejected; adapters must supply canonical versioned values and separate Playback runs/connections where semantics differ. This class cannot validate whether those supplied identities accurately describe platform data. Tick capability is implicit; aggregates must not use this registry as if interchangeable.

One key grants one publisher lease, not one subscription per chart. Competing claims return Occupied with no writer. Registry constructor bounds active streams, event slots per stream and each read batch. These are not a process-wide byte or outstanding-snapshot budget. Subscription count and in-flight snapshot budgets remain integration work.

Readers have no publishing or producer-disposal authority. They pin a single generation, which returns Closed after source disposal rather than switching silently to a replacement. Publisher release closes the buffer before releasing the key. Reference identity guards against stale lease removal of a successor. Idempotent service shutdown closes all active buffers, detaches lease references and permanently rejects acquisition.

Append/read use only the per-buffer lock. Lifecycle operations use registry -> buffer lock order, with no reverse path, external callbacks, chart references, account references, timers or dispatcher calls. One logical publisher can receive serialized concurrent calls; adapters still own correct source-event ordering. No finalizers or automatic failover: owner/service disposal is mandatory. Closed buffers release ring arrays even if consumers retain old handles; existing immutable returned batches remain consumer-owned allocations.

## Settings, series and platform implications

No user-facing settings, defaults, secondary series, Tick/Bid/Ask/Last subscriptions, OnMarketData, Calculate mode or Tick Replay behavior changed. No chart rendering, execution/trading, history loading, cache persistence or existing provider/consumer integration changes. Full_Suite untouched. No deployment, restart or user F5 needed for this isolated slice.

## Tests / compile / validation

Linked-source harness passes 127 checks, including the existing 24 storage checks. New tests cover 64 parallel publisher claims, maximum stream capacity, compatible identity lookup, isolation by each key dimension, release/reacquire, stale disposal, generation mismatch, pinned old readers, registry shutdown, and 20 concurrent append/release/shutdown trials. Passing concurrency trials are evidence for exercised interleavings, not an exhaustive proof of all platform races.

Standalone installed .NET Framework compilation of both core files passed. No NinjaTrader compile/load or manual chart tests performed; the classes have no NinjaTrader runtime dependencies. This is not runtime or performance validation. No live speedup claimed.

## Remaining work and promotion

Next: truthful coverage and historical/live state contract, memory/subscriber budgets, then an adapter with explicit subscription ownership and canonical quote/trade ingestion. Prove replay/history/live handoff before opt-in RollingProfiles migration. The old provider's known persistence, session and positional-cursor defects remain until that migration or separately scoped fixes.

No automatic subscriber reference-counted acquisition, stalled-owner recovery, persistence, or historical hydration implemented here. These must not be inferred from the registry API. Not eligible for Full_Suite; manual validation follows future integration.
