# 2026-07-10 - Orca Volume-Spike Lean Mode

## Objective

Reduce RTH/news-volume backlog amplification without re-enabling the shared provider default or changing profile totals. Target the diagnostics hot path shared by duplicate hidden Tick 1 consumers and the allocation-heavy expiring history used by `OrcaRollingProfiles`.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaDiagnosticsCore.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaCumulativeDelta.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaMGIDaily.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs`
- `docs/handoffs/2026-07-10_orca-volume-spike-lean-mode.md`

## Behavior added, changed, or removed

- Diagnostics event counters now update without taking the single global diagnostics lock on every `OnBarUpdate` and `OnMarketData` callback.
- Bars-in-progress and market-data counters remain exact. Input/model timestamps are sampled on the first event and every 32 hot-path events after that.
- Render timing uses an instance-local lock, so unrelated chart instances no longer serialize on the diagnostics global lock.
- `OrcaRollingProfiles` automatically detects sustained local hidden-tick rates at or above 1,000 events per second.
- During a detected spike, Rolling Profiles preserves exact volume and classified delta totals while merging equivalent expiring-history records into 250 ms price buckets.
- Spike compaction remains active for three seconds after the threshold is crossed, then returns to normal tick-history storage.
- Diagnostics cache status reports `lean active`, observed rate, bucket size, and cumulative compacted-record count. No chart label was added.
- `OrcaRollingProfiles`, `OrcaCumulativeDelta`, `OrcaTimeStatistics`, `OrcaMGIDaily`, and `OrcaExecutionLines` now avoid diagnostics timestamp/state work in callback paths while Diagnostics is Off.

## User-facing settings

- No user-facing settings were added, changed, deprecated, or removed.
- The threshold, 250 ms history bucket, and three-second hold are internal conservative defaults for this first benchmark pass.

## Secondary series

- No `AddDataSeries` calls were added, removed, or changed.
- Existing hidden Tick 1 series remain declared by local-source indicators.
- This slice reduces duplicated downstream diagnostics and Rolling Profiles history work; it does not yet consolidate the underlying NinjaTrader hidden series.

## Tick Replay implications

- No Tick Replay requirement or source mode changed.
- Tick Replay and historical hidden-series event totals remain counted exactly.
- Sampled diagnostics timestamps may be up to 31 hot-path events behind the latest callback, normally a few milliseconds during a spike.

## Historical-load implications

- Fast historical hydration can activate Rolling Profiles spike compaction because the gate measures processing throughput.
- Historical volume and classified delta totals remain exact.
- Rolling-window expiration can retain a compacted record for up to 250 ms beyond its exact tick timestamp; this is bounded and small relative to the configured rolling periods.

## Cache implications

- No provider, persistent cache, or shared-cache default changed.
- `OrcaProfileDataProvider` remains opt-in and the provider-default rollback remains in force.
- Rolling Profiles stores fewer expiring history objects during sustained high-rate periods; its total profile maps continue to update per trade.

## Rendering implications

- Profile drawing behavior and source labels are unchanged.
- Diagnostics render sampling remains active when diagnostics are Live, but render samples no longer contend with every indicator's event counters on one global lock.

## Performance implications

- Removes a global per-event lock convoy that was multiplied across every instrument/chart/module row while Orca Diagnostics was Live.
- Restores a cheap Diagnostics-Off path in the Phase 2 modules touched by this pass.
- Reduces Rolling Profiles allocations and timestamp-key growth during high-volume bursts, particularly when multiple Rolling Profiles instances are loaded.
- This change does not yet remove duplicate hidden Tick 1 series from `OrcaStepProfile`, `OrcaAbsorptionCandles`, `OrcaLegtoLegProfile`, or local `OrcaRollingProfiles` instances.
- The next benchmark should compare pre-spike lag, peak lag, recovery time, event rates, compacted count, model time, and render maxima with Diagnostics Live.

## Tests performed in NinjaTrader

- Deployed all six edited indicator files from `Working_Suite` to the live NinjaTrader Custom Indicators folder.
- NinjaTrader immediately regenerated wrapper sections in four live files; normalized pre-generated logic matches `Working_Suite` for all six deployed files.
- NinjaTrader F5 compile: pending Julian.
- Live RTH/news-volume validation: pending Julian.

## Compile status

- Source consistency checks and `git diff --check` passed for the edited source.
- NinjaTrader F5 compile is pending.

## Manual-validation status

- Pending.
- Validate that Orca Diagnostics rows continue counting, lag stays plausible, Rolling Profiles visuals/totals remain stable, and the Cache column shows `lean active` during a sufficiently large spike.

## Known issues, risks, and follow-up work

- The 1,000 events/second threshold is based on the observed MNQ spike screenshot and needs live calibration.
- Timestamp sampling intentionally trades sub-second precision for much lower diagnostic overhead; counters remain exact.
- True hidden-series consolidation still requires a lifecycle-safe shared feed or chart-local ownership design. Do not restore the previous provider default as part of this benchmark.
- After this slice is validated, the next candidates are `OrcaStepProfile` and `OrcaAbsorptionCandles`, followed by a bounded chart-local shared-feed design.

## Full_Suite promotion eligibility

- Not eligible.
- Promotion requires NinjaTrader F5 compile and Julian's live manual validation.
