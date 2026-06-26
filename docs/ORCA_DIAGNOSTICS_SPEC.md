# Orca Diagnostics / Workspace Load Observatory

Last updated: 2026-06-25

## Purpose

Orca Diagnostics / Workspace Load Observatory is an internal developer-quality and reliability system for measuring startup, historical processing, cache behavior, calculation cost, data integrity, and rendering across all Orca chart instances in a multi-chart NinjaTrader workspace. It is not a customer-facing product feature in its first phase.

## Goals

- Identify which chart instance is slow.
- Identify which Orca component is slow.
- Attribute elapsed time to lifecycle phases.
- Verify whether a component received complete historical input.
- Detect incomplete local/shared tick cache responses.
- Distinguish data access, calculation, model-building, and rendering failures.
- Measure Tick Replay event volume and per-tick workload.
- Detect overlapping tick/Bid/Ask/Last requests and duplicate cache loads.
- Detect lock contention and shared-cache wait time.
- Produce a single startup report after workspace stabilization.

## Non-Goals

- Do not change indicator behavior in Phase 1.
- Do not optimize before measurements identify the bottleneck.
- Do not print on every tick, bar, or render call.
- Do not require broad historical-data reloads or destructive cache clears.
- Do not treat all modules on one chart as the primary benchmark.
- Do not claim root cause without reproducible evidence.

## Modes

Diagnostics must be disabled by default.

- `Off`: no allocation-heavy event capture; disabled path must be a cheap branch.
- `StartupSummary`: aggregate lifecycle, series, cache, profile/model, exception, and render summaries during startup/historical load and emit one summary.
- `Verbose`: include threshold breaches and bounded detail records, still sampled/aggregated and not per-tick logging.

## Event Model

Core event families:

- `StartupSessionStarted`
- `ModuleInstanceCreated`
- `StateChanged`
- `SeriesDeclared`
- `HistoricalProcessingStarted`
- `HistoricalProcessingProgress`
- `HistoricalProcessingCompleted`
- `RealtimeReached`
- `MarketDataSummary`
- `BarUpdateSummary`
- `CacheRequestStarted`
- `CacheRequestCompleted`
- `CacheGapDetected`
- `ProfileBuildStarted`
- `ProfileBuildCompleted`
- `RenderSample`
- `ThresholdBreach`
- `Exception`
- `StartupReportCompleted`

## Required Lifecycle Telemetry

For every practical Orca indicator/chart object, capture startup session ID, timestamp, module name, module version, source file/component ID, unique chart/indicator instance ID, instrument/contract, chart bar type/value, trading-hours template when accessible, Tick Replay state when accessible, days back/requested range when accessible, `State.DataLoaded` timestamp, historical start timestamp, first historical processing timestamp, transition/realtime timestamps, historical warm-up duration, historical bars processed, first/last processed historical timestamps, callback counts, and exceptions with phase, timestamp, last processed bar time, error text, and safe stack details.

## Required Added-Series Map

Each relevant Orca instance should report bars-in-progress index, instrument, bar type, bar value, market-data type, primary versus secondary, Tick/Second/Minute/Range/Volume/Volumetric/custom classification, why the series exists, and whether it was added by the component or inherited from the chart.

Initial candidate output shape:

```text
ORCA_SERIES_MAP
Session=<startup-session-id>
Instance=<module-instance-id>
Indicator=OrcaRollingProfiles
Chart=<instrument> <primary bar type/value> / <trading hours> / <days back>
BIP=0 <instrument> <primary period> Last source=Chart reason=Primary
BIP=1 <instrument> 1 Tick Last source=Component reason=Local tick cache
```

Do not finalize this exact structure until NinjaTrader-accessible metadata is confirmed in implementation.

## Required Cache Telemetry

For shared or local tick-cache systems, capture normalized cache key, instrument/contract, date/time range, session template where relevant, data source, hit/miss, acquisition duration, lock/synchronization wait time, concurrent request count for the same key, equivalent in-flight request detection, rows/ticks returned, first/last tick timestamp, largest timestamp gap, count of gaps over a configurable threshold, cache entry state, cache-read duration, cache-build duration, and cache-write duration where applicable.

Initial integration targets:

- `OrcaProfileDataCache.RegisterSource`
- `OrcaProfileDataCache.RegisterOrderFlowSource`
- `OrcaProfileDataCache.TrySnapshot`
- `OrcaProfileDataCache.TrySnapshotOrderFlow`
- `OrcaProfileDataCache.TrySnapshotOrderFlowSinceIndex`
- `OrcaProfileDataCache.TrySnapshotOrderFlowPriceMaps`
- `OrcaProfileDataProvider.LoadOrderFlowCache`
- `OrcaProfileDataProvider.SaveOrderFlowCache`
- `OrcaProfileDataProvider.ProcessTickIntoPrimaryBar`

## Required Profile And Model Telemetry

For profile-oriented tools, separately time/count tick parsing, price-level aggregation, delta aggregation, profile bucket creation/update, POC calculation, value-area calculation, gradient/label preparation, snapshot/model construction, profile rebuilds, incremental updates, active price buckets, and empty/invalid profile-model states.

The system must distinguish input ticks absent, ticks present but no profile buckets produced, profile model produced but render output absent, profile built successfully but performance degraded, and cache/synchronization wait dominating elapsed time.

## Required Rendering Telemetry

Rendering must be observed without creating a rendering bottleneck.

Capture render-call count, total render time, average sampled render time, max render time, render-resource rebuild count, snapshot age/stale-state detection, and invalidation request count where practical.

Constraints:

- No profile calculation, cache access, mutation, heavy allocation, or full historical scanning should occur in `OnRender`.
- Phase 1 can measure current violations or borderline patterns before refactoring.

## Live Workspace Observatory Requirements

The observatory must support live trading-session visibility, not only startup summaries. This is required because MNQ can lag during RTH open and news-volume spikes, especially when multiple charts and indicators process tick-level data.

Primary live questions:

- Which order-flow source is each Orca indicator using right now: primary chart series, Tick Replay Last events, hidden secondary tick series, shared `OrcaProfileDataProvider`, shared chart cache, or local cache?
- Is each indicator receiving live real data, stale data, fallback-estimated data, or no data?
- Which chart/indicator instances are behind realtime, and by how much?
- Which indicators own hidden Tick, Second, Bid, Ask, Last, Volumetric, or custom series?
- Which modules are processing the most `OnMarketData`, `OnBarUpdate`, profile rebuild, cache snapshot, and render work?
- Are multiple Orca modules independently requesting overlapping tick/order-flow streams for the same instrument/range?
- Is chart lag caused by data access, event processing, model/profile building, rendering, cache locks, or NinjaTrader chart/cache state?

Required live status fields per instance:

- Module name and instance ID
- Instrument and contract
- Primary bar type/value and trading-hours template
- Tick Replay state when accessible
- Data source mode and source health: live, replay, secondary-series, shared-provider, local-cache, estimated, stale, unavailable
- Last input timestamp received and wall-clock lag to current time
- `OnMarketData` counts by Last/Bid/Ask
- `OnBarUpdate` counts by BarsInProgress
- Hidden/secondary series map
- Cache hit/miss and source age where applicable
- Last profile/model update timestamp
- Render count, sampled render time, and max render time
- Warning flags: behind realtime, no live events, duplicate tick source, stale provider, excessive render time, cache wait, replay gap, fallback active

Initial operator surfaces:

- A lightweight internal AddOn window: `Tools > Orca Diagnostics`.
- A single workspace status table with rows grouped by chart/instrument/module.
- Optional per-chart compact overlay later, after the logging and aggregation layer is stable.
- JSONL/CSV startup output remains useful, but live status must be readable without opening log files.

Implementation constraints:

- Diagnostics remain Off by default.
- `StartupSummary` may log summaries; live observatory mode must aggregate in memory and refresh at a low fixed cadence, not per tick.
- Do not write files, allocate large objects, or block inside `OnMarketData`, `OnBarUpdate`, or `OnRender`.
- Prefer cheap counters and timestamps in hot paths, with snapshot publication to the diagnostics AddOn.
- Diagnostics failures must fail closed and never affect trading/chart behavior.

## Metrics

Lifecycle metrics: `data_loaded_ms`, `historical_warmup_ms`, `time_to_transition_ms`, `time_to_realtime_ms`, `historical_bars_processed`, `first_historical_time`, `last_historical_time`.

Event volume metrics: `on_bar_update_count`, `on_bar_update_bip_<n>_count`, `on_market_data_last_count`, `on_market_data_bid_count`, `on_market_data_ask_count`, `tick_replay_event_count`.

Cache metrics: `cache_request_count`, `cache_hit_count`, `cache_miss_count`, `cache_wait_ms`, `cache_read_ms`, `cache_build_ms`, `cache_write_ms`, `cache_rows_returned`, `cache_first_time`, `cache_last_time`, `cache_largest_gap_ms`.

Profile metrics: `profile_rebuild_count`, `profile_incremental_update_count`, `profile_build_ms`, `profile_bucket_count`, `value_area_ms`, `poc_ms`, `empty_profile_count`.

Render metrics: `render_count`, `render_sample_count`, `render_total_ms`, `render_avg_sample_ms`, `render_max_ms`, `render_resource_rebuild_count`, `invalidate_visual_count`.

## Output Files

Preferred first-phase output:

- JSONL summary/events: `Documents/NinjaTrader 8/orca-diagnostics/orca-diagnostics-YYYYMMDD-HHMMSS.jsonl`
- CSV rollups: `Documents/NinjaTrader 8/orca-diagnostics/orca-startup-summary-YYYYMMDD-HHMMSS.csv`

Output should be bounded and append-only per startup session.

No account numbers, order IDs, trade notes, custom tags, or personally identifying information should be logged. Instrument symbols and bar-series metadata are acceptable for diagnostics.

## Threading Considerations

- Disabled path must not allocate.
- Use lightweight counters and `Stopwatch.GetTimestamp()` where possible.
- Avoid blocking NinjaTrader UI/chart/render threads on file I/O.
- Aggregate in memory and flush in phase summaries or low-priority background work.
- Guard shared state with short locks.
- Avoid long global locks and global serial startup bottlenecks.
- Never write diagnostics from inside a high-frequency lock if the write can block.

## Performance Constraints

- `Off` mode: negligible overhead.
- `StartupSummary`: low overhead, aggregated counters/timings only.
- `Verbose`: bounded detail with rate limits.
- No per-tick `Print`.
- No per-render file writes.
- No unbounded dictionary/list growth.

## Failure Modes

Diagnostics must tolerate file write denial, unavailable paths, unavailable chart metadata, terminated instances, telemetry-formatting exceptions, disappearing cache sources, corrupt persisted provider caches, and unexpectedly large Tick Replay event volume.

Diagnostics must fail closed: if diagnostics fail, Orca trading/chart behavior should continue.

## Alert Categories

- Historical warm-up unusually slow
- Cache wait unusually slow
- Duplicate cache load detected
- Tick gap detected
- No profile buckets after valid input
- Render time unusually high
- Tick Replay event volume unusually high
- Excessive profile rebuild rate
- Realtime transition not reached after configurable time

Thresholds are placeholders until baseline measurements exist.

## Acceptance Criteria

Phase 1 is accepted when diagnostics are disabled by default; `StartupSummary` emits one startup report for a workspace; the report includes per-instance lifecycle timings, series map, event counts, cache summaries, profile/model summaries, render summaries, and exceptions; the report distinguishes primary chart series from component-added secondary series where NinjaTrader metadata allows; the report identifies which components added hidden 1-tick and 30-second series in a test workspace; the report captures `OrcaProfileDataProvider` registration and snapshot availability; disabled mode changes no user-facing behavior; and no per-tick or per-render `Print` spam is introduced.

## Candidate Source Files For Phase 1

New file:

- `Orca Trades/Working_Suite/Indicators/OrcaDiagnosticsCore.cs`

Primary instrumentation targets:

- `Orca Trades/Working_Suite/Indicators/OrcaProfileDataProvider.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaVolumeProfileCore.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaVisibleRangeVolumeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaStepProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaLegtoLegProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaPrints.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaPrints.Engine.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaPrints.Rendering.cs`

Secondary instrumentation targets:

- `Orca Trades/Working_Suite/Indicators/OrcaCumulativeDelta.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaAbsorptionCandles.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaTickDirectionIndex.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaSessionContextMap.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaMGIDaily.cs`
- `Orca Trades/Working_Suite/DrawingTools/OrcaFixedRangeProfile.cs`
- `Orca Trades/Working_Suite/DrawingTools/OrcaManualAnchoredVWAP.cs`
- `Orca Trades/Working_Suite/BarsTypes/OrcaAtrAdaptiveRangeBarsType.cs`

## Benchmark Matrix

Minimum benchmark plan:

- Clean NinjaTrader launch
- Normal workspace restart
- Safe abnormal/forced-close recovery test
- Single-chart baseline
- Full 10-12 chart workspace baseline
- Native indicator combinations on each chart
- Tick Replay off versus on
- One day versus five days of MNQ history
- Cold cache versus warmed cache
- Time bars, second bars, range bars, volume bars, and custom series actually used
- 1 Minute, 5 Minute, 15 Second, 30 Second, 30 Minute, 930 Minute, 16 Range, 40 Range, 120 Range, 260 Range
- Overlapping profile windows
- Multiple Orca modules requesting the same instrument/range
- Tick Replay historical calculation
- Safe data-gap/incomplete-cache simulation
- Render stress
- Realtime transition and steady-state performance

For each test capture total startup time, time to first usable chart, time to all charts usable, time to realtime, CPU, memory, allocation/GC observations where feasible, per-module elapsed time, per-bar-series elapsed time, cache waits, cache hit/miss behavior, tick counts, profile rebuild counts, render timings, errors/warnings, and whether price bars, indicator model, and rendering were each complete.

## Rollout Phases

Phase 0: Documentation and source audit.

Phase 1: Disabled-by-default diagnostics core, lifecycle counters, series maps, cache registration/snapshot timing, and startup summary output.

Phase 2: Profile/model timing and render sampling in high-priority profile/render modules.

Phase 3: Controlled benchmark runs and baseline thresholds.

Phase 4: Evidence-based optimization of duplicate tick series, shared-provider defaults, profile rebuilds, cache contention, or render snapshots.

Phase 5: Optional internal dashboard/overlay after logging is reliable.
