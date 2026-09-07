# Provider subscription integration checkpoint — 2026-09-07

## Objective and result

Proceed from isolated ownership helpers to a platform adapter without introducing duplicate ticks or claiming unsupported source provenance. This pass establishes an integration plan; no indicator logic or deployment changed.

## Verified API constraints

- The documented [MarketData subscription](https://docs.ninjatrader.com/ninjascript/marketdata) attaches to Instrument.MarketData.Update, using the instrument dispatcher for attachment. It supplies snapshot data immediately on subscription. Only a successfully attached handler should be removed. The documented attachment signature does not select a Connection.
- [Connection](https://docs.ninjatrader.com/ninjascript/connection_class) exposes global connection status events and distinguishes price-feed status from order-feed status.
- [ConnectionStatusEventArgs](https://docs.ninjatrader.com/ninjascript/connectionstatuseventargs) identifies the affected connection, not the connection responsible for another market-data callback.
- Installed metadata inspection found Instrument.GetMarketDataConnection(), but no documented guarantee was established that its result authenticates historical data or the origin of an already queued tick. It must not be used to label arbitrary historical data as belonging to that feed.

Consequently, merely moving subscription ownership out of a chart does not solve provenance. A new listener also must not append its initial Last snapshot as a new execution. Timestamp/price/volume equality cannot distinguish a repeated snapshot from two legitimate identical trades.

## Implementation sequence

### 1. Multi-reader comparison on the existing single-source probe

Files: OrcaProviderProbe.cs, OrcaProviderFeedLifetimeCore.cs, tests/OrcaProviderStream.Tests.

Use the existing canonical OnMarketData Last lane and private registry; do not add another market subscription. Wire the typed identity and feed owner into the probe. Add bounded independent readers with independent cursors and compare event counts, volume totals and an order-sensitive event digest over exactly the same sequence interval. Report eviction as an explicit failed comparison, never silently reset a reader to FirstAvailable. Keep comparison batches disposable and bounded; perform no reader processing in OnRender. Report summary counters without raw execution data or per-tick Print.

This proves one producer/multiple-reader transport, not indicator calculation parity, cross-chart compatibility or reduced platform data loading. It provides useful integration evidence without speculative feed attribution.

### 2. Live-only owned subscription experiment

New opt-in adapter, not a change to the existing production provider. Its lifetime owns exactly one successfully attached instrument handler and one connection-status handler. Attach on the instrument dispatcher, track pending/attached/closing/closed explicitly, and handle Dispose racing pending attachment. Detach on the owning dispatcher; report shutdown/attachment failures. Do not invoke consumer code under platform or global registry locks.

Before implementation, establish an evidence-backed rule for subscription snapshots versus incremental trades. Do not infer that IsReset, the first callback, timestamp equality or a subscribe-return boundary is sufficient without verifying actual platform semantics and synchronous/asynchronous callback behavior. Until then, leave this adapter disabled rather than quietly discarding or duplicating events.

Close the owner on any price-routing topology change until exact feed attribution is established. This conservative rule may interrupt an unaffected stream; it is preferable to silent cross-feed continuation. No account/order connection operations, reconnection, workspace manipulation or trading subscriptions are authorized by this experiment.

### 3. Historical coverage and first consumer

Keep historical and live sources distinct until a reliable handoff is demonstrated. A current routing connection cannot certify local cached history. Capture merge/rollover, split/dividend adjustment, session and classification initialization semantics before cross-chart matching. Preserve retained-range and generation checks.

Choose one consumer in shadow/comparison mode only after these contracts are verified; retain existing output and series for the comparison. Never suppress required local data based solely on a matching key. Actual hidden-series removal is a separate deployment and performance-measurement gate.

## Tests and user action

No new compile/runtime test was needed for this documentation-only pass. Prior 309 core checks and offline platform compilation remain prior evidence, not a test of subscription attachment or snapshot handling. No user action needed yet; ask for one-chart probe output when the multi-reader probe is implemented and deliberately deployed. Do not request repeated full workspace restarts for these development steps.

## Change implications and boundaries

Files changed: this handoff only. Settings, secondary series (Tick/Second/Bid/Ask/Last/Volumetric/custom), Tick Replay, historical loading, cache access/persistence, rendering and trading behavior: unchanged. No new AddDataSeries, OnMarketData, Calculate mode, subscription or timer. No measured performance improvement. No deployment or NinjaTrader F5/manual validation. Full_Suite untouched; not eligible for promotion. Existing dirty work preserved.
