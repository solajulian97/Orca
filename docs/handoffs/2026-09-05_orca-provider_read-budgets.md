# Provider reader and outstanding-batch budgets

## Objective / scope

Continue the approved core hardening. Bound registry reader handles and payload copies held by consumers, without wiring the legacy provider or chart callbacks yet.

## Files changed

- Working_Suite/Indicators/OrcaProviderReadBudgetCore.cs (new)
- Working_Suite/Indicators/OrcaProviderRegistryCore.cs
- tests/OrcaProviderStream.Tests/Program.cs and project
- This handoff.

## Behavior / contract

Registry construction now accepts maximum reader handles and maximum outstanding batches. The existing three-argument overload uses 128 readers and 256 outstanding batches; these are internal constructor defaults, not new NinjaScript user settings. Explicit OpenReader status distinguishes absent source, exhausted reader budget and closed registry. Existing TryOpenReader remains a convenience boolean wrapper.

Readers are IDisposable. Read reserves a service-wide batch slot before allocating the bounded payload; quota exhaustion throws OrcaProviderBudgetExceededException without advancing any cursor. Failures release the reservation. Each returned OrcaProviderBatchLease must be disposed, preferably in a using block. Disposal drops the private snapshot reference before releasing its slot, and is idempotent. An event-list wrapper retained after disposal cannot expose or retain that disposed event array through its owner; copied event structs belong to the consumer.

Payload arrays never escape the batch lease. Read-only metadata remains available after disposal, but accessing Events after disposal throws. Concurrent iteration/disposal may throw; consumers must not dispose a batch while another thread is using it. Reader disposal releases only the reader slot; outstanding batches remain valid and charged, including after publisher/registry shutdown. It is incorrect to automatically return their capacity merely because a reader closes.

There are deliberately no finalizers, weak-reference sweeps or forced disposal. Forgotten Dispose causes explicit capacity refusal, not unbounded allocation; this is an availability risk requiring adapter discipline. Existing broad contract fixtures do not stress disposal and use conservative defaults; the new budget-specific tests explicitly dispose all handles/batches.

For registry-managed storage, retained payload slots are bounded by maxStreams * eventsPerStream, and outstanding snapshot payload slots by maxOutstandingBatches * maxReadEvents. These counts exclude CLR/object overhead, garbage awaiting collection, consumer copies/models, direct low-level buffer usage, additional independently created registries, and metadata such as key strings. This is NOT a process-wide byte cap. No allocation-rate or GC performance claim is made.

## Platform handoff investigation

NinjaTrader's current official documentation confirms Tick Replay OnMarketData follows the OnBarUpdate events used to build the bar, and trade-time bid/ask should come from the MarketDataEventArgs. Source: https://docs.ninjatrader.com/ninjascript/developing_for_tick_replay . The previous help-guide URL redirected unsuccessfully; the current official documentation was found through search.

This supports canonical Last-event ingestion for the Tick Replay adapter, not pairing hidden-bar events with subsequently cached quotes. It does not establish a globally unique event identity or a safe timestamp-only historical/live deduplication rule. The existing strict half-open time handoff cannot simply be wired to all native transitions: historical and realtime events may share a timestamp. Next design work must use lifecycle/source ordering and explicit generation/sequence boundaries, with identical-time boundary fixtures, before enabling ingestion. No platform adapter or overlap reconciliation is implemented in this slice.

## Tests and validation

180 linked-source checks pass, including all prior storage/ownership/coverage fixtures. New tests cover reader quota refusal, batch refusal before copying, idempotent disposal, retained-view invalidation, invalid-request recovery, independent reader/batch reservations, shutdown with a live batch, and 32 simultaneous readers competing for one outstanding-batch slot. Standalone .NET Framework compilation of all three core files passes. No NinjaTrader tests, deployment, F5 or manual runtime validation performed. No measured startup or live performance improvement claimed.

## Settings / data / lifecycle implications

No existing indicator settings, defaults, secondary series, Tick Replay behavior, history loading, persistence/cache files, OnMarketData/Calculate mode, chart rendering, execution/trading logic or platform subscriptions changed. No chart/account references, timers or external callbacks added. All old provider/consumer code and unrelated dirty changes remain untouched. Full_Suite unchanged; not eligible for promotion.

Next: correct sequence-aware platform handoff contract, canonical ingestion adapter and one opt-in consumer parity test. No restart needed for this isolated core slice.
