# Provider historical/live tail and normal cleanup — 2026-09-09

## Objective

Record Julian's runtime evidence for the connection activation-boundary revision (`0b3e00e`), distinguish accepted-event transport from source completeness, and define the next lifecycle test without changing trading connections automatically.

## Evidence

User-provided screenshots:

- `C:/Users/julia/AppData/Local/Temp/codex-clipboard-188d6bc6-5157-4de5-a9b3-86416cb2d5c3.png`: first successful historical/live capture, 2,174,855 historical + 1 live = 2,174,856 verified events. No final-removal line in this capture.
- `C:/Users/julia/AppData/Local/Temp/codex-clipboard-ab2aab10-6c72-43dc-89a7-94de30d0651f.png`: separate run `6a1780225f5d43988357277c31c29c5c`, 2,175,885 historical + 75 live = 2,175,960 verified events, `readerComparison=PASS`, elapsed=25.4s, followed by `connection-monitor-detached=True`.

Both show TickReplay=True, Eastern Standard Time, comparisonReaders=2, attachmentNotifications=2, publisherAcquired=False on the activation line, and `connectionGuard=DirectPlatformEventsAfterAttach`. Script Disconnected->Connecting->Connected observations occur at Realtime without faulting the direct-monitor run. The complete run retains 100,000 events, evicts 2,075,960, and shows passComplete=True, fullPassRetained=False, UTC-range-confirmed=false.

These counts reconcile: 2,175,885 + 75 = 2,175,960 = 2,075,960 evicted + 100,000 retained. The separate run identifiers must not be combined into one continuous observation. Temporary screenshot paths may not remain available; these transcribed results preserve the bounded evidence.

## Accepted and unaccepted conclusions

Accepted for these observations: revised code loaded; setup notifications were counted before publisher admission; historical-to-live ingestion and independent-reader delivery matched; the complete run's live tail was drained and the owned connection handler removed normally. The source's previous terminal response to setup notifications no longer prevents this normal run.

Not established: the origin of those setup notifications, a universal synchronous snapshot rule, authentication of historical or live data, exchange completeness, correct merge/adjustment provenance, actual disconnect/reconnect behavior, all sessions/feed providers, cross-chart sharing, independent indicator-calculation parity, memory-leak absence, or performance improvement. A 25.4s probe duration is not a workspace startup benchmark. fullPassRetained=False is expected after consuming/evicting history; it must not be relabeled as complete retained coverage.

## Files changed / behavior implications

Documentation only: this handoff, the 2026-09-08 monitor-activation handoff follow-up, and `docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md`.

- Runtime behavior: no code added, changed or removed in this pass.
- User-facing settings: none added, changed, deprecated or removed.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: unchanged; none added.
- AddDataSeries, OnMarketData, Calculate mode and shared-cache usage: unchanged.
- Tick Replay / historical load: unchanged; evidence from existing Tick Replay run only.
- Cache/persistence: unchanged; no cache, history, database or workspace cleanup.
- Rendering/performance: unchanged; no new measurements or optimization claim.
- Deployment: none required or performed for documentation.

## Tests / compile / manual validation / promotion

Tests performed in NinjaTrader: Julian's two supplied captures; one contains final live-tail and normal-cleanup evidence. No platform UI/test performed by the agent. Loaded revision is evidenced by its distinct marker/behavior. A specific F5 compiler-pane result was not separately supplied; do not represent the agent as having pressed F5. Prior offline results remain 243 linked-probe, 322 core and 14 observer checks plus zero-error 11-source platform semantic compilation; no new code tests were needed for documentation-only changes.

Manual validation: normal setup, historical/live transport and cleanup passed for the complete observed run. Interruption/reconnection and production integration remain pending. Full_Suite untouched and not eligible for promotion.

## Next lifecycle gate — user-controlled, outside trading

Do not disconnect any account or data connection automatically. Julian must choose a safe non-trading session, with no positions or working orders affected and no automated trading/copying dependent on the connection. A simulation account selection alone does not prove a shared connection is safe to interrupt.

1. Add the current probe to one connected chart and establish a live PASS.
2. Julian manually disconnects the relevant connection. Capture Output. A market-data reset may win the race with the connection notification; either terminal cause is acceptable, but the specific `phase=Active` direct-monitor path is runtime-proven only if it appears. Quiet feeds may defer the fault report until a later callback/removal.
3. Julian reconnects, retaining the old probe instance. It must not resume successful ingestion or publish a new PASS after its terminal fault. Capture any bounded script observations and fault/cleanup output.
4. Remove the old probe, confirm owned-handler removal, then add a fresh instance. The fresh identifier must produce its own live PASS and normal cleanup. No automatic old-epoch reuse.

If an active notification is not observed, keep that exact path unverified; do not weaken the guard, inject fake platform events into live NinjaTrader, or modify connection settings to manufacture a PASS. No repeated full workspace restart is required.

After this lifecycle gate, continue the existing source-contract/subscription plan: resolve standalone snapshot-versus-incremental semantics and historical merge/adjustment/provenance before cross-chart discovery or Rolling Profiles shadow migration. Preserve all existing consumer output and local series until independent parity permits a separately authorized removal.
