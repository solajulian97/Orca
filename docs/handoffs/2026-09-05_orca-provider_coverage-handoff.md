# Provider coverage and declared historical/live handoff

## Objective

Continue the approved isolated provider foundation. Make empty/caught-up reads distinct from producer-confirmed history, expose retention loss and faults, and publish coverage atomically with event batches. No platform integration in this slice.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderStreamCore.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaProviderRegistryCore.cs`
- `tests/OrcaProviderStream.Tests/Program.cs`
- This handoff.

## Behavior and architecture

New immutable per-batch coverage reports phase, first fault reason, requested UTC history interval, producer confirmation, confirmed-history sequence boundary and whether the entire confirmed history remains retained. Reads capture these fields under the same per-buffer lock as payload/cursors. Past snapshots do not change when the producer advances.

Lifecycle is Unverified -> Historical -> HistoryConfirmed -> Live. Faulted and Closed are terminal for ingestion in that generation; replacement requires release/acquisition. History can only begin before any unverified events have been appended. Direct storage writes may remain Unverified for low-level use, but can never be retroactively labeled confirmed history.

`BeginHistory(fromUtc, toUtcExclusive)` declares a nonempty UTC half-open interval. Historical appends must fall inside it. `ConfirmHistory` is explicitly a producer assertion, not an inference from tick counts or timestamps. It seals the accepted sequence prefix and pauses ingestion until `BeginLive`. Live events must be UTC and at/after the declared boundary. Every valid same-time event is preserved; no timestamp deduplication or silent overlap suppression.

This boundary contract does NOT yet reconcile NinjaTrader historical/live callbacks with overlapping timestamps. The future adapter must stage/reconcile source data and establish a valid cut, preserving distinct executions/events with identical visible attributes. Do not wire it by blindly rejecting legitimate platform callbacks. Rejected events throw to the publisher; the adapter must treat ingestion failure/gaps explicitly rather than catching and continuing as if coverage were complete.

Reader status Ready means the cursor is readable, not that history is complete or that a live feed is fresh. SourceFaulted returns no payload and preserves the first typed fault cause. Coverage ProducerConfirmedHistory records the assertion, while FullConfirmedHistoryRetained is false after any historical prefix eviction, fault or close. An explicitly confirmed zero-event history interval remains retained despite later live-event eviction; skipped requested live sequences still report Evicted through the cursor API.

No interval-gap ledger, current live freshness guarantee, completeness verification against a data source, per-subscriber baseline accounting or reconnect recovery is implemented here. Consumers that build incrementally must eventually track their own incorporated baseline rather than interpreting retained-store coverage as their entire model's coverage.

## Settings / series / platform implications

No user settings/defaults, secondary series, Tick Replay requirements, subscriptions, OnMarketData/Calculate modes, chart rendering, trading logic, old provider or consumers changed. One small immutable coverage object is allocated per read batch; no additional per-tick allocation, persistence or historical rebuild. No deployment, NinjaTrader F5 or restart requested. Full_Suite untouched.

## Tests / compile / manual validation

Linked-source harness: 167 checks pass, including all prior storage/ownership checks, empty/unverified reads, invalid UTC intervals, illegal transitions, exclusive boundary enforcement, confirmed sequence prefix, immutable older coverage, overlap rejection, duplicate same-time live events, retention loss, terminal faults, empty confirmed history, and 20 append/confirmation concurrency trials. These trials exercise interleavings, not an exhaustive concurrency proof.

Standalone .NET Framework compilation of both core sources passes. No NinjaTrader runtime tests or performance benchmarks performed; manual platform validation remains pending integration. No end-to-end startup/live speedup claimed. Not eligible for Full_Suite.

## Next

Define adapter-owned coverage evidence and historical/live overlap reconciliation, then implement total subscriber/in-flight memory budgets and one opt-in ingestion/consumer path. Keep the existing provider opt-in and preserve local-series fallback until exact historical and chart-output parity is demonstrated.
