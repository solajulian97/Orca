# Provider manual disconnect and fresh-run evidence — 2026-09-09

## Objective / input

Evaluate Julian's user-operated disconnect/reconnect test of the private probe and record exactly what passed. Input: `C:/Users/julia/.codex/attachments/402975a4-ae41-4e20-a6fc-97f7a9ba21e2/pasted-text.txt`. Julian reports manually disconnecting data sources, reconnecting, removing the probe and adding it again. Output contains three distinct probe IDs; do not infer that each corresponds to a separate deliberate user action or merge their counters.

## Probe-specific evidence

1. `e3def9229e6e4fe6bbb3e14643c4f805`: historical=2,997,715. Last printed PASS before interruption is live=1/verified=2,997,716. At interruption, script observations report live=637. The direct monitor records `phase=Active`, price/order Connected->Disconnecting at 2026-09-09T20:10:28.5554626Z, then reports successful handler detach and exactly one FAULT. Later observations remain historical=2,997,715/live=637 and alreadyFaulted=True. No subsequent PASS or restart for this ID appears. Do not claim a final two-reader comparison of all 637 live events: fault cleanup does not print a successfully drained final tail.
2. `f0b9a850450444fda0cdaac214e19a2c`: fresh ID, historical=0. Starts with NO_EVENTS; first live event and final live=1,707/verified=1,707 both PASS. Final elapsed=177.8s, volume=2,914, signed=-362, retained=1,707, evicted=0. Handler detach succeeds.
3. `361aaf484b68485eb1bb267d9b286ff0`: another fresh ID, historical=0. Starts with NO_EVENTS; first live event and final live=122/verified=122 both PASS. Final elapsed=64.1s, volume=144, signed=16, retained=122, evicted=0. Handler detach succeeds.

All three show TickReplay=True, two comparison readers, DirectPlatformEventsAfterAttach and four attachment notifications. Fresh runs show script startup Connecting/Connected pairs without fault. No inference about which connection produced which tick is justified by these lines.

## Accepted scope / remaining uncertainty

Accepted for the tested sequence: a genuine active direct-status notification terminally invalidated the old private owner; its handler detached; no old-ID PASS resumed in the supplied full output; distinct fresh instances accepted live callbacks, matched the reader transport and cleaned up. This is fresh-run replacement, not automatic reconnection or historical recovery inside the old epoch. The initial detach-before-FAULT log order follows cleanup/reporting code and is not a missed fault.

Both later runs have historical=0 despite TickReplay=True and enter Live at elapsed=0.0s. Their passComplete=True/fullPassRetained=True fields describe an ended, empty source-lifecycle pass, not confirmed historical coverage. Source review: Probe's historicalReplay comes from Bars.IsTickReplay; Realtime calls ingestion.CompleteHistoricalPassAndBeginLive; the stream's SourceLifecycle completion sets the pass-completed flag even for zero events while ProducerConfirmedHistory remains false. UTC-range-confirmed=false is correctly retained. The reason historical callbacks were absent is not established by this log. Do not diagnose missing cached data, erase/reload history, or treat fresh live PASS as a historical backfill.

## Other indicator messages — separate findings

- Time Statistics reports no legacy OrcaProfileDataProvider source for GC DEC26 and ES SEP26. The private probe intentionally registers no production source, so its PASS does not resolve those warnings or prove Time Statistics coverage.
- Step Profile and Candle Volume Profile each report one skipped render frame with an array-index exception. These are separate unresolved renderer observations during the test output, not evidence that the private probe caused them. Do not modify those components in this provider pass.
- Execution Lines startup/rebuild reports are not probe failures or proof of provider performance. No account/execution implementation changed.

## Files / behavior / rollout implications

Documentation only: this handoff, `docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md`, and a result follow-up in `2026-09-09_orca-provider_live-tail-runtime-evidence.md`.

No source behavior or user-facing settings added/changed/deprecated/removed. No secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series, AddDataSeries, OnMarketData, Calculate mode, subscriptions, rendering, cache access/persistence or historical-load policy changed. Tick Replay settings unchanged. No data/database/cache/workspace mutation, connection operation, deployment or performance claim by the agent.

## Tests / compile / manual validation / promotion

Julian performed the platform test; the agent inspected the full pasted Output and current probe/monitor/ingestion/coverage code. No new F5 or source compile was needed for documentation-only changes. Previously recorded 243 linked-probe, 322 core and 14 observer checks plus zero-error installed-platform semantic compile remain prior offline evidence, not newly executed tests. Revised runtime load and the tested interruption/fresh-live/cleanup sequence are evidenced by the supplied markers and output. All-feed/session/prolonged-load coverage remains unverified. Full_Suite untouched and not eligible for promotion.

## Next bounded implementation scope

Do not repeat the disconnect test or add a grace period. Proceed with the existing historical source/coverage contract before global sharing or consumer migration:

1. `OrcaProviderStreamCore.cs` and future consumer admission: preserve the distinction between source-lifecycle completion, actual historical events, producer-confirmed requested UTC coverage and retained history. Add consumer-facing admission tests that deny a required historical interval for a zero-event unverified lifecycle pass while preserving a genuinely confirmed empty requested interval (a valid no-trade interval is possible).
2. `OrcaProviderIdentityCore.cs` / platform capture: require explicit historical merge/rollover and adjustment semantics before cross-chart matching. A current route/connection epoch cannot certify cached history. Preserve unique classifier lineage and ordinal effective-session/timezone snapshots.
3. `OrcaProviderProbe.cs`: expose observed historical-input scope explicitly when the next code slice is integrated; keep NO_EVENTS for zero total input and retain live-only transport proof without labeling it recovered history. Do not automatically reload chart data to manufacture historical callbacks.
4. First Rolling Profiles shadow comparison remains downstream of these contracts. Preserve its existing calculations/output/local series; no public registry or hidden-series removal based merely on this lifecycle PASS. Standalone MarketData.Update ingestion remains gated on documented snapshot-versus-incremental semantics.
