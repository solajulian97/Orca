# Repository-only history observer — 2026-09-10

## Objective

Continue from the history configuration/identity foundation (`18cdfa4`) to an opt-in platform observation. Inspect one bounded returned Last Tick-1 sample without publishing it, authenticating its provenance, stitching it to live events, or changing production consumers.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderHistoryProbe.cs` (new).
- `tests/OrcaProviderHistory.Tests/{OrcaProviderHistory.Tests.csproj,PlatformStubs.cs,Program.cs}` (new linked-source fixtures).
- `tests/OrcaProviderStream.Tests/ProviderHistoryConfigurationTests.cs`: two fixture classes made partial for reuse; no test behavior changed.
- `tests/OrcaProvider.PlatformCheck/Program.cs`: observer structural guards.
- `docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md` and this handoff.
- Local deployment helper `.codex-backups/deploy-provider-history-observer.ps1` (not product source).

Previously committed `OrcaProviderIdentityCore.cs` and `OrcaProviderHistoryConfigurationCapture.cs` are deployment dependencies, not source edits in this slice. Legacy identity constructor/key behavior remains unchanged.

## Behavior and ownership

Manually add `OrcaProviderHistoryProbe` to one chart. On its first Realtime transition it queues an independent BarsRequest on the instrument dispatcher: 1,000 Last Tick-1 bars, chart resolved TradingHours, explicit DoNotMerge, Repository-only lookup, no split/dividend adjustments, reset-on-new-trading-day enabled. It does not alter chart/global configuration. Futures-only configuration validation precedes Request.

Request is called once. Completion is always queued, even if synchronous. Inspection is capped at the first 1,000 returned rows; no Update subscription is added. Three sample rows plus counts, empty/capped status, volume, invalid rows, timestamp kinds, adjacent equal/backwards timestamps, positive ordered quote-pair count, first/last timestamps, count stability and configuration comparison are printed. Identical rows remain separate: deduplicated=0, published=0. Inspection exceptions remain errors.

A 30-second dispatcher timer disposes a pending request; it is not a hard wall-clock deadline if the dispatcher/platform blocks. Completion, request failure, removal and shutdown also dispose it. Removal/shutdown during Request defers disposal until Request returns; during inspection cancellation defers disposal until inspection unwinds. Late/duplicate callbacks are ignored. The owner retains no chart/indicator reference. Cleanup clears request/instrument/session/configuration references and timer/shutdown handlers. Dispose failure reports requestReleased=False and retains the shutdown hook/owner for a later removal/shutdown retry.

Final review added explicit nested-dispatcher guards: if a platform Request pumps queued work before returning, one completion is deferred and timeout marks cancellation without disposing inside Request. Deferred completion references are cleared on successful cleanup. Two additional linked scenarios prove that neither path inspects/disposes during the simulated Request call; actual platform scheduling remains unverified.

## Platform evidence and limitations

[BarsRequest documentation](https://docs.ninjatrader.com/ninjascript/barsrequest) documents count-back requests, independent chart-series timing, Repository versus Provider lookup and disposal. [Request callback documentation](https://ninjatrader.com/support/helpguides/nt8/request.htm) demonstrates inspection of returned Bars. Offline compilation confirms API binding, not runtime scheduling.

Repository-only intent avoids an explicit provider download request. NinjaTrader owns internal loading/cache behavior; the observer does not delete, repair or write historical rows or Orca caches. A count-back sample is not a fixed UTC interval. The final bar may develop; equal count/configuration before/after does not establish atomicity or detect all edits. Positive quote pairs do not establish authentic historical bid/ask. The displayed event clock is a captured setting, not timestamp-provenance proof. No UTC conversion/admission or history-aware provider identity is created. Empty results do not authorize migration.

## Settings / series / history / cache / rendering / performance

- User settings added/changed/deprecated/removed: none. New manually installed diagnostic indicator; fixed count/timeout.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: none (`AddDataSeries` absent). One independent repository Last Tick-1 request intentionally observes a path the existing chart-callback/cache services cannot prove. Install on one chart only.
- OnMarketData/OnBarUpdate/OnConnectionStatusUpdate: no overrides. No live market-data/connection subscription, trading, publication, or Calculate.OnEachTick/OnPriceChange setting.
- Tick Replay: not required or changed. Request starts at Realtime only. Playback-specific timing untested.
- Historical load: one 1,000-bar request; returned data may exceed this, inspection is capped. Platform internal load cost is unmeasured, not guaranteed bounded by this cap. No retry, automatic rerun, broad reload or historical/live merge.
- Cache: existing shared/production caches untouched; no Orca persistence or historical-store mutation.
- Rendering: no plots, drawing, renderer, invalidation or render-path work.
- Performance: initialization encoding, at most 1,000 row reads, bounded Output. No steady-state work after cleanup; no speedup/hidden-series-reduction claim.

## Tests and release gates

- 153 linked observer checks passed: request/configuration, sync/foreign-thread/duplicate/late/nested-dispatcher callbacks, empty/overreturned/identical rows, malformed values/clock kinds, wide volume sum, cancellation before/during Request/queued completion/inspection, timeout, shutdown, platform error, thrown request/read, mutated config, foreign result, failed completion queue and disposal retry.
- Regression: 473 core, 243 canonical-probe, 14 subscription-observer checks passed.
- Offline C# 7.3 semantic check: zero errors across 14 provider sources. Structural guards: one Request site, no Update subscription, added series, publication/coverage assertion or rendering.
- Source: complete. Guarded three-file deployment completed at approximately 09:43 ET, using the targeted Working_Suite deployment script. Preflight matched the old live identity to its pre-18cdfa4 version and all ten other live provider files to their current authored source. All ten non-target provider files remained unchanged after deployment.
- Backup: `.codex-backups/provider-history-observer-20260910-094321/` contains the old live identity and three source snapshots. An earlier sandbox-denied attempt left a separate backup at `provider-history-observer-20260910-094305/`; no live write succeeded in that attempt.
- Normalized authored source/live SHA-256 parity passed: identity `3353059D062271DCEE09B815571AE9E0C0D171ED91EA05A98390A97ED708EB06`; configuration capture `8D9706D95038DA35ED05BF41773D16B049230610FFCD9406A115743782339D6D`; final observer `0F290EDF23224380059B86BF9B4D57F992D8722B7F403A71D2D809A26CD122A7`. The nested-dispatcher revision was deployed only to the observer after backing up its initial live version in the same backup directory.
- NinjaTrader F5 / Custom-assembly load: not performed for this change.
- NinjaTrader tests/manual validation: not performed. Fixtures do not prove actual request/dispatch/disposal behavior or repository results.
- Full_Suite: untouched, not eligible. Existing canonical probe, local-series ownership and production consumer settings unchanged.

## First manual gate / follow-up

Press F5 in NinjaScript Editor. On one existing connected futures chart, add `OrcaProviderHistoryProbe` without changing Tick Replay/session settings. Open NinjaScript Output 1. Expect starting, samples/summary or explicit error, then cleanup with requestReleased=True; allow about 30 seconds for timeout. Remove the indicator and supply all lines for its ID. Empty/rejected results are useful evidence, not a reason to change settings or download data blindly. No trade/disconnect test requested.

Inspect this evidence before any UTC certification, historical/live stitching, cross-chart discovery or Rolling Profiles shadow integration. History admission remains source-only/unwired.
