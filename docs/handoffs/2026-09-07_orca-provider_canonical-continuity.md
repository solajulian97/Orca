# Canonical probe continuity gates — 2026-09-07

## Objective / evidence

Continue provider hardening after Julian's subscription observation. Screenshot: ES SEP26, detached=True, published=0, callbacksDuringAdd=10, callbacksAfterAdd=200, lastCallbacks=33, resetCallbacks=0, elapsed=4.99s. A Last callback during attachment had reset=False. Actual observer execution and reported detachment succeeded; explicit F5 keystroke was not separately observed. This single run does not establish a portable snapshot discriminator.

Official [MarketData](https://docs.ninjatrader.com/ninjascript/marketdata) supplies subscription snapshots; no reliable snapshot/incremental discriminator was established. Retain the existing primary [OnMarketData](https://docs.ninjatrader.com/ninjascript/onmarketdata) probe instead of introducing a guessed standalone filter. Its documented ordered callback path is not independent proof of exchange completeness. [OnConnectionStatusUpdate](https://docs.ninjatrader.com/ninjascript/onconnectionstatusupdate) provides connection notifications, with price and order status distinct; it does not authenticate another callback's source.

Source review identified silent Transition Last rejection and a connection guard limited to lost/disconnected notifications while Realtime. These are source-level gaps, not failures observed in Julian's screenshot.

## Files changed

- Working_Suite/Indicators/OrcaProviderProbe.cs
- tests/OrcaProviderProbe.Tests/{OrcaProviderProbe.Tests.csproj,PlatformStubs.cs,Program.cs}
- docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md
- This handoff.

## Behavior

- Reject any reset notification before the Last-only filter and close the private feed owner.
- Fault explicitly on Last callbacks during Transition rather than silently discarding an unassigned event.
- Close the owner on every connection notification after ingestion creation, including Historical/Transition and Connected/Connecting. Conservatively includes unrelated or order-only notifications; no guessed route provenance or automatic recovery. Notifications before owner creation are ignored.
- Print the first live comparison and drain/print the final accepted live tail before normal termination. Repeated termination remains idempotent; faulted runs do not print a successful terminal summary. Identical timestamp/price/volume callbacks remain distinct.

## Implications

- User-facing settings: none added/changed/removed; experiment remains manually opt-in.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: none added or changed; no AddDataSeries.
- Existing OnMarketData, OnBarUpdate session flag and OnConnectionStatusUpdate overrides retained; no manual subscription, timer or global handler added. Existing Calculate.OnEachTick unchanged.
- Tick Replay/history: accepted historical events unchanged; uncertain Transition events now fault. No history request, rebuild, cache/database deletion or range-completeness claim.
- Cache: private owner closes on uncertainty; no production registry, persistence or consumer change.
- Rendering: no OnRender or chart visuals.
- Performance: bounded checks plus at most one first-live and one normal-termination summary; terminal drain bounded by existing reader policy. No measured speedup or hidden-series reduction.
- Trading/accounts/orders: untouched.

## Verification / deployment

- 64 linked actual-probe checks passed against shape-only platform fixtures, including normal handoff, identical boundary payloads, first/final live reporting, all four fixture connection statuses across four lifecycle phases, reset types, terminal rejection and live-only mode.
- Existing 322 core checks passed. Offline C# 7.3 NinjaTrader-reference semantic check: zero errors across ten provider sources, structural guards pass.
- Subscription observer lifecycle regression suite: all 14 checks passed.
- Pre-edit authored live/source-HEAD parity passed; generated NinjaScript region excluded. No local mirror source exists for this probe.
- Targeted deployment: only OrcaProviderProbe.cs copied; post-deploy authored parity passed. Prior live file backed up to `.codex-backups/provider-continuity-20260907/OrcaProviderProbe.live-before.cs`. NinjaTrader F5/load and revised-probe runtime: pending. No disconnect, connection changes, account operations or restart performed by the agent.
- Full_Suite untouched; not eligible for promotion. Unrelated dirty work preserved.

## Next user gate / remaining risks

F5, add OrcaProviderProbe to one connected chart (non-Tick-Replay is sufficient for live-only), allow live ticks, then remove it and capture the final summary. Expected readerComparison=PASS, live>0, verified=historical+live, with no FAULT. This proves accepted live transport only. If a normal lifecycle emits an early connection notification or Transition Last, capture that fault; do not weaken the guard speculatively.

No forced disconnect test on a trading workspace. Standalone snapshot provenance, historical/cache provenance, cross-chart sharing, consumer calculation parity, hidden-series removal and controlled performance measurement remain unresolved. Do not present this checkpoint as the completed shared-provider rollout.
