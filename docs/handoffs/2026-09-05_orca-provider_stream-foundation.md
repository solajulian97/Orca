# Provider exact-stream foundation: architecture and first slice

## Objective

Julian authorized the architecture pass followed by implementation. Establish a testable, bounded exact-event storage contract without changing existing chart calculations. This follows the provider hardening review, not a wholesale rewrite or default-on deployment.

## Architecture decisions

- Exact tick data is the canonical lane. Aggregates are explicitly derived capabilities, not replacements for ordered ticks. Arrival sequence is authoritative within one generation; identical timestamps and identical tick values remain distinct. Reordered source timestamps are not sorted here.
- The planned owner registry must distinguish full contract, data environment, session/time basis and classification semantics. It grants a single writer lease per compatible stream. No chart/account references belong in shared data storage. Registry/lease implementation is the next slice, not completed here.
- A source generation plus absolute sequence identifies a cursor. Eviction, replacement, future cursors and closure are explicit results with no automatic catch-up or implicit source switching.
- Initial storage uses a fixed-capacity struct ring and bounded copied batches. This is a deliberately smaller implementation than sealed zero-copy segments. Batch contents are read-only value copies. A later segment implementation must preserve this API's semantics and justify itself with allocation measurements.
- Capacity bounds retained event payload, and maxReadEvents bounds one allocation/lock-held copy. It does NOT cap unlimited consumer-retained snapshots, concurrent readers' total memory, or service-wide allocations. Subscriber/in-flight budgets are required before multi-chart integration.
- Coverage completeness, historical/live reconciliation, gap intervals and cache-overlap resolution are separate producer contracts. An empty Ready read only means caught up to current publication, not complete requested history. No timestamp-based deduplication is allowed.
- No market-data ingestion, classification algorithm, subscription or rendering API is called by the core. Producer adapters will supply classification provenance and validated event data.
- Persistence stays out of this slice. Existing provider stays opt-in and unchanged. First migration remains RollingProfiles with local-mode parity gates.

## Files changed

- Working_Suite/Indicators/OrcaProviderStreamCore.cs: pure C# event, cursor, batch and bounded buffer.
- tests/OrcaProviderStream.Tests/OrcaProviderStream.Tests.csproj and Program.cs: linked-source executable regression harness.
- This handoff.

## Behavior and settings

New isolated storage supports thread-safe append/read/dispose, generation identity, absolute sequence eviction, bounded reads, immutable value snapshots and invalid/default event rejection. It has no static source registry, owner delegates or platform references. Dispose is idempotent and rejects future publication.

No user-facing settings/defaults, secondary series, Tick/Bid/Ask/Last subscriptions, Tick Replay behavior, existing cache, historical load, drawings, trading functionality or existing provider/consumer code changed. No OnMarketData, OnEachTick, OnPriceChange or OnRender work added. No Full_Suite changes or live deployment.

## Verification

`dotnet run --project tests/OrcaProviderStream.Tests`: 24 checks passed. Covers repeated identical-time ticks, provenance, bounded int.MaxValue requests, eviction gap, surviving cursor continuity after prefix eviction, copied snapshot independence, caught-up empty read, future cursor, source-generation replacement, idempotent disposal, rejected late append/default event and 10,000 concurrent appends into a 128-event capacity.

The source also compiles independently with the installed .NET Framework compiler. Initial shell invocation mispassed the space-containing source path; rerun from its directory succeeded. These are source/storage checks, not NinjaTrader F5/runtime validation or a performance benchmark.

## Remaining work / risks

Next: compatible-stream key, sole-owner leases and teardown race tests; total memory/subscriber budgeting; coverage publication. Then canonical historical/live ingestion and one opt-in consumer. Concurrent append tests establish serialization, not exchange order across competing publishers; the owner layer must enforce the single-writer ingestion rule. Session/DST, quote alignment, persistence overlap/corruption and actual chart parity remain untested pending integration. This source is not yet used by the legacy provider, so its known defects remain unchanged.

NinjaTrader tests: none. Manual validation: pending future integration. Compile: standalone passed, platform F5 not applicable until targeted deployment. Promotion: not eligible for Full_Suite. No runtime speedup claimed.
