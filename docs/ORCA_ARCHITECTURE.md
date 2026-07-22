# Orca Architecture

Last updated: 2026-06-25

## Scope

This document maps the current `Orca Trades/Working_Suite` source for startup, historical-load, Tick Replay, cache, and rendering diagnostics. It is based on code inspection only; no NinjaTrader runtime benchmark was run in this pass.

## Repository Shape

- `Orca Trades/Working_Suite/Indicators`: active indicator source.
- `Orca Trades/Working_Suite/AddOns`: active add-on source.
- `Orca Trades/Working_Suite/DrawingTools`: active drawing-tool source.
- `Orca Trades/Working_Suite/BarsTypes`: active custom BarsType source.
- `Orca Trades/Full_Suite`: validated promotion target after Julian's manual NinjaTrader validation.
- `Orca Trades/NinjaTrader`, `Stable_Release`, and `decompiled`: references only unless Julian explicitly approves copying.

## Indicator And Tool Inventory

Order-flow/profile: `OrcaAbsorptionCandles`, `OrcaCandleVolumeProfile`, `OrcaCumulativeDelta`, `OrcaFixedRangeProfile`, `OrcaLegtoLegProfile`, `OrcaProfileDataProvider`, `OrcaRollingProfiles`, `OrcaStepProfile`, `OrcaTickDirectionIndex`, `OrcaVisibleRangeVolumeProfile`, `OrcaVolumeProfileCore`.

Prints/execution: `OrcaPrints`, `OrcaPrints.Engine`, `OrcaPrints.Models`, `OrcaPrints.Rendering`, `OrcaPrints.Scoring`, `OrcaExecutionLines`, `OrcaExecutionLines2`, `OrcaVisualOrders`.

VWAP/session context: `OrcaAnchoredVWAPs`, `OrcaManualAnchoredVWAP`, `OrcaTimeVWAPs`, `OrcaTimeStatistics`, `OrcaSessionContextMap`, `OrcaMGIDaily`, `OrcaMGIWeekly`, `OrcaMGIStatistics`.

Add-ons: `OrcaCopyAddOn`, `OrcaCopyEngine`, `OrcaCopyNetwork`, `OrcaTradeCopierAddOn`, `OrcaTradeCopierEngine`, `OrcaTradeCopierNetwork`, `OrcaRiskManagerAddOn`, `OrcaExecutionRouterAddOn`, `OrcaDisciplineGuardAddOn`.

BarsType: `OrcaAtrAdaptiveRangeBarsType`.

## Shared Services And Cache Layers

`OrcaVolumeProfileCore.cs` contains the main shared profile/order-flow cache: `OrcaProfileDataCache`, `RegisterSource`, `RegisterOrderFlowSource`, `TrySnapshot`, `TrySnapshotOrderFlow`, `TrySnapshotOrderFlowSinceIndex`, `TrySnapshotOrderFlowPriceMaps`, `BuildKey(Bars)`, `BuildKey(Bars, ChartControl)`, and `BuildInstrumentKey(Bars)`.

Known cache characteristics from inspection:

- The registry is in-memory and static.
- Source registration is guarded by `CacheSync`.
- Snapshot reads copy dictionaries or order-flow bucket clones while holding source sync locks.
- Source keys include instrument/bars-period for chart profile data and instrument-only for order-flow data.
- Chart-specific keys exist through `BuildKey(Bars, ChartControl)`.
- Current code exposes source descriptions but not duration, lock-wait, duplicate-load, partial-data, or gap telemetry.

`OrcaProfileDataProvider.cs` is the main shared producer:

- Adds a hidden 1-tick series.
- Publishes order-flow buckets by instrument key.
- Optionally publishes chart profile maps when `PublishChartProfileCache` is enabled.
- Can persist order-flow cache when `PersistOrderFlowCache` is enabled.
- Uses a `DispatcherTimer` to refresh registration.

Existing shared consumers include `OrcaRollingProfiles`, `OrcaVisibleRangeVolumeProfile`, `OrcaCumulativeDelta`, and `OrcaTimeStatistics`.

## Data-Series Ownership

| Component | Adds secondary series | Type/value | Notes |
| --- | --- | --- | --- |
| `OrcaAbsorptionCandles` | Yes | 1 Tick | Hidden tick processing for candle absorption/delta. |
| `OrcaCumulativeDelta` | Yes | 1 Tick | Used when not relying on shared provider. |
| `OrcaCandleVolumeProfile` | Yes | 1 Tick | Per-candle true volume/delta maps. |
| `OrcaLegtoLegProfile` | Yes | 1 Tick | Leg profile tick volume/delta. |
| `OrcaProfileDataProvider` | Yes | 1 Tick | Shared order-flow/profile producer. |
| `OrcaRollingProfiles` | Conditional | 1 Tick | Adds local tick series when `UseLocalTickSeriesCache` is true and shared provider is false. |
| `OrcaStepProfile` | Yes | 1 Tick | Step blocks built from tick events. |
| `OrcaTickDirectionIndex` | Yes | 1 Tick | Tick-direction delta index. |
| `OrcaVisibleRangeVolumeProfile` | Conditional | 1 Tick | Adds local tick series when true VAP and local tick cache are enabled. |
| `OrcaMGIDaily` | Yes | 1 Minute and 30 Second | Daily/session opening-range and anchor behavior. |
| `OrcaSessionContextMap` | Yes | 30 Second; 1 Tick when session volume profile is enabled | 30-second opening range plus true traded-at-price session profile/VWAP; no bar-range volume estimate. |
| `OrcaAtrAdaptiveRangeBarsType` | BarsType | Built from Tick; base period Second | Custom BarsType work may appear in utilization. |

No `AddVolumetric` usage was found in `Working_Suite`.

## Bid Ask Last Dependencies

`OnMarketData` consumers: `OrcaAbsorptionCandles`, `OrcaCandleVolumeProfile`, `OrcaCumulativeDelta`, `OrcaExecutionLines`, `OrcaLegtoLegProfile`, `OrcaPrints.Engine`, `OrcaProfileDataProvider`, `OrcaRollingProfiles`, `OrcaSessionContextMap`, `OrcaStepProfile`, `OrcaTimeStatistics`, `OrcaVisibleRangeVolumeProfile`.

Typical pattern:

- Bid/ask values update from `MarketDataType.Bid` and `MarketDataType.Ask`.
- Last events are classified with bid/ask when available, then fall back to tick direction in several tools.
- Historical bid/ask availability is a risk; Tick Replay and provider state must be measured per component.

## Lifecycle And Rendering Patterns

Common calculation modes:

- `Calculate.OnEachTick`: `OrcaAbsorptionCandles`, `OrcaCumulativeDelta`, `OrcaPrints`, `OrcaProfileDataProvider`, `OrcaTickDirectionIndex`, `OrcaTimeStatistics`, `OrcaVisibleRangeVolumeProfile`, `OrcaVisualOrders`, and conditional `OrcaSessionContextMap`.
- `Calculate.OnPriceChange`: `OrcaCandleVolumeProfile`, `OrcaExecutionLines`, `OrcaLegtoLegProfile`, `OrcaMGIDaily`, `OrcaMGIWeekly`, `OrcaRollingProfiles`, `OrcaStepProfile`, `OrcaTimeVWAPs`, default `OrcaSessionContextMap`.
- `Calculate.OnBarClose`: `OrcaAnchoredVWAPs`.

Render-heavy components with explicit `OnRender`: `OrcaCandleVolumeProfile`, `OrcaExecutionLines`, `OrcaFixedRangeProfile`, `OrcaLegtoLegProfile`, `OrcaManualAnchoredVWAP`, `OrcaMGIDaily`, `OrcaMGIStatistics`, `OrcaMGIWeekly`, `OrcaPrints.Rendering`, `OrcaProfileDataProvider`, `OrcaRollingProfiles`, `OrcaSessionContextMap`, `OrcaStepProfile`, `OrcaTickDirectionIndex`, `OrcaTimeStatistics`, `OrcaVisibleRangeVolumeProfile`, and `OrcaVisualOrders`.

Render risk areas:

- `OrcaVisibleRangeVolumeProfile` may recalculate profiles inside `OnRender` when cache keys are dirty.
- `OrcaExecutionLines` snapshots under lock in `OnRender` and creates some DirectWrite/geometry objects during rendering.
- Profile renderers generally use SharpDX resource caches but still need measured render timings and rebuild counters.

## Cross-Module Dependencies

- `OrcaProfileDataProvider` can feed profile/order-flow snapshots to profile consumers through `OrcaProfileDataCache`.
- `OrcaPrints` publishes shared profile cache when `PublishSharedProfileCache` is true.
- `OrcaVisibleRangeVolumeProfile` can use shared provider, local tick cache, shared chart true VAP cache, or estimated chart fallback.
- `OrcaRollingProfiles` can use local hidden tick series or shared provider historical backfill.
- `OrcaCumulativeDelta` and `OrcaTimeStatistics` can consume shared order-flow snapshots.

## Known Duplication Or Risk Areas

- Multiple components can independently add hidden 1-tick series for the same instrument/range.
- Multiple local caches can classify Bid/Ask/Last separately.
- The shared provider exists but is not universally default.
- Cache telemetry does not currently measure lock wait, duplicate requests, gaps, partial data, or source hydration time.
- Some profile/model work still happens near render paths and needs timing before optimization.
- Printed warnings are throttled but not structured startup reports.

## Proposed Diagnostic Integration Points

- New shared diagnostics core in `Orca Trades/Working_Suite/Indicators/OrcaDiagnosticsCore.cs`.
- Lifecycle hooks in each high-priority indicator: `SetDefaults`, `Configure`, `DataLoaded`, `Historical`, `Transition`, `Realtime`, `Terminated`.
- Series-map reporting immediately after `Configure`.
- Shared cache hooks in `OrcaProfileDataCache` and `OrcaProfileDataProvider`.
- Profile/model timing around profile build methods in `OrcaVolumeProfileCore`, `OrcaRollingProfiles`, `OrcaVisibleRangeVolumeProfile`, `OrcaStepProfile`, `OrcaLegtoLegProfile`, and `OrcaCandleVolumeProfile`.
- Render sampling around `OnRender` in high-cost visual tools.
