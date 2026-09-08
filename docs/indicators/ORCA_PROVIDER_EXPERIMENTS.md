# Exact-event provider experiments

Updated 2026-09-08. Active source: `Orca Trades/Working_Suite/Indicators/OrcaProvider*.cs`.

These opt-in experiments are separate from the existing `OrcaProfileDataProvider` and `OrcaProfileDataCache`. No production indicator discovers their private registry. No hidden-series reduction or startup improvement is established.

## Current components

- Stream/registry/read-budget cores: bounded exact-event storage, stable sequence/generation cursors, exclusive publisher ownership, explicit eviction/coverage, bounded disposable reader batches.
- Identity/session/feed-lifetime cores: immutable effective session and timezone snapshots, isolated continuity epochs, terminal disposal; not authenticated broker/cache provenance.
- Ingestion core: one ordered Last callback lane with supplied trade-time quote classification and tick-direction fallback. Identical callback payloads remain separate events.
- Reader comparison: two independent bounded readers compare exact ordered payloads; this is transport verification, not an independent exchange tape or indicator-calculation comparison.
- `OrcaProviderProbe`: manually added chart indicator, primary `OnMarketData` Last source, no added series or renderer, private 100,000-event retention. Tick Replay supplies historical callbacks when enabled; no Tick Replay means live-only. Session resets use native bar flags. Diagnostics are optional.
- `OrcaProviderSubscriptionProbe`: manually added five-second observation of instrument-level subscription callbacks, bounded detail output and owned-handler cleanup. It publishes nothing and does not establish a snapshot filter.
- `OrcaProviderConnectionMonitor`: owns one direct `Connection.ConnectionStatusUpdate` handler before the probe acquires its publisher. Retains only the private core owner and bounded value-only notification state, not an indicator, chart, account or connection object.

## Continuity policy

The canonical probe faults terminally on a market-data reset, a notification received through its directly owned platform connection monitor, or a Last callback observed during Transition. The direct connection policy intentionally includes unrelated/order-only notifications because affected-feed attribution is unverified. It can stop a valid observation unnecessarily; reload creates a new isolated run. No automatic reconnect, callback buffering, timestamp-based deduplication or guessed handoff is allowed.

Indicator `OnConnectionStatusUpdate` notifications are now diagnostic only. They do not create/recover a feed or override the separately attached monitor. Any direct notification immediately closes the private core on the notifying thread using bounded core locks; it performs no chart callback, printing or unsubscribe there. The next probe callback/status/removal reports the terminal cause and detaches the exact handler. In a quiet feed the closed core precedes its deferred Output/Diagnostics report. Normal removal also reports `connection-monitor-detached=True`. A failed detach is explicit and retains a helper for retry, with no static chart reference.

Normal historical-to-Realtime transition preserves every accepted callback. The first live event and final removal print bounded reader-comparison summaries, even when Diagnostics is off. Summary PASS confirms delivery of accepted events only; it does not establish historical completeness, missing exchange trades, snapshot provenance or cross-chart compatibility.

## Evidence and next gate

Two revised canonical-probe runs stopped on price=Connecting after historical comparison PASS (latest 2,988,611 events, live=0). Julian confirmed prices continued updating on the chart, including with only the canonical probe installed. This establishes that the probe stopped while the chart continued, not that the notification was harmless or what emitted it. The 2026-09-08 update prints at most eight connection observations, including previous/new price/order status, current connection-object price status, UTC receipt time and lifecycle/counters. Observations may continue after the terminal fault; ingestion never resumes. Current status cannot authenticate older callbacks. The next capture is diagnostic, not an expected clean PASS run.

Julian supplied four historical comparison PASS runs (~2.95 million events each, live=0). A subsequent ES SEP26 subscription observation reported detached=True, published=0, 10 during-add and 200 after-add callbacks, 33 total Last callbacks, zero reset callbacks, elapsed 4.99 seconds. A during-add Last had reset=False. This validates that observation's execution/cleanup, not a universal synchronous snapshot contract.

The latest diagnostic capture showed two `Disconnected->Connecting->Connected` pairs immediately after Realtime while current connection price state was already Connected; historical comparison passed 2,183,537 events and live remained zero. This is consistent with initialization/delayed script notifications but does not prove their origin or authenticate a tick. The direct monitor replaces the script notification as the probe's continuity boundary without relying on current connection status to ignore events.

The revised canonical probe needs F5/load and one live run, then removal to capture the final live tail and handler-removal line. Expected startup marker: `connectionGuard=DirectPlatformEvents`. Transition/direct-connection/reset faults must remain visible and must not be bypassed to obtain PASS. A standalone market-data subscriber still needs a documented snapshot/incremental boundary; consumer shadow comparison, cross-chart sharing, historical provenance and measured performance remain later gates. Full_Suite is not eligible for promotion.
