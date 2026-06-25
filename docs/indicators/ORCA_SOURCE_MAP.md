# Orca Source Map

Last updated: 2026-06-25

This source map is based on `Orca Trades/Working_Suite` code inspection only.

## Secondary And Hidden Series

| Component | Adds secondary series | Type/value | Bid/Ask/Last usage | Per-tick workload | Tick Replay implications | Shared cache usage | Render behavior |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `OrcaAbsorptionCandles` | Yes | 1 Tick | `OnMarketData` tracks Bid, Ask, Last | Maps hidden ticks to primary bars and computes absorption/delta state | High risk of large historical event volume | No shared provider found | Paints bars; no explicit SharpDX render found in first pass |
| `OrcaCumulativeDelta` | Yes | 1 Tick | Bid, Ask, Last; fallback classification | Per-tick delta and primary-bar carry-forward | High; may also use shared provider | Consumes `OrcaProfileDataCache` order-flow snapshots | Explicit `OnRender` |
| `OrcaCandleVolumeProfile` | Yes | 1 Tick | Bid, Ask, Last | Per-candle volume/delta maps | High; hidden tick hydration can be expensive | Local maps; no primary shared consumer role found | Explicit SharpDX `OnRender` |
| `OrcaLegtoLegProfile` | Yes | 1 Tick | Bid, Ask, Last | Per-tick leg volume/delta aggregation | High | Internal maps/locks | Explicit SharpDX `OnRender` |
| `OrcaProfileDataProvider` | Yes | 1 Tick | Bid, Ask, Last | Publishes order-flow buckets and optional chart profile maps | High but intended shared producer | Registers sources in `OrcaProfileDataCache`; optional persisted cache | `OnRender` refreshes registration only |
| `OrcaRollingProfiles` | Conditional | 1 Tick when local cache enabled and shared provider disabled | Bid, Ask; Last via tick series | Rolling tick buckets and profile state | High in local mode; shared-provider mode may reduce duplicate series | Consumes `OrcaProfileDataCache` order-flow snapshots | Explicit SharpDX `OnRender` |
| `OrcaStepProfile` | Yes | 1 Tick | Bid, Ask; tick series price/volume | Step block tick aggregation | High | No shared provider found | Explicit SharpDX `OnRender` |
| `OrcaTickDirectionIndex` | Yes | 1 Tick | Tick series direction; no `OnMarketData` found | Tick-direction delta per primary bar | High | No shared provider found | Explicit SharpDX `OnRender` |
| `OrcaVisibleRangeVolumeProfile` | Conditional | 1 Tick when local tick cache enabled | Bid, Ask, Last | Visible-range true VAP local maps; can recalc visible model | High in local mode; shared-provider mode lowers duplicate series | Consumes shared provider, shared chart VAP, or local cache | Explicit SharpDX `OnRender`; profile recalculation can occur from render path |
| `OrcaMGIDaily` | Yes | 1 Minute and 30 Second | No `OnMarketData` found in first pass | Secondary minute/second opening-range/session anchors | Tick Replay less central than secondary time series | No shared provider found | Explicit SharpDX `OnRender` |
| `OrcaSessionContextMap` | Yes | 30 Second | Bid, Ask, Last | Session state and delta updates | Moderate; depends on `UpdateMode` and second series | No shared provider found | Explicit SharpDX `OnRender` |
| `OrcaPrints` | No explicit `AddDataSeries` found | Uses primary and market data | Bid, Ask, Last in engine | Per-last print/cluster accumulation | Tick Replay/event-volume risk when on chart data | Publishes shared profile cache when enabled | Explicit SharpDX render partial |
| `OrcaAtrAdaptiveRangeBarsType` | BarsType, not indicator | Built from Tick; base Second | Last market data type by default | Custom bar construction | Can explain Bars type work even without a visible 1-tick chart | No shared provider found | BarsType, not renderer |

## Direct Evidence For 1 Tick Utilization Entry

Working_Suite files with explicit `AddDataSeries(BarsPeriodType.Tick, 1)`:

- `OrcaAbsorptionCandles.cs`
- `OrcaCumulativeDelta.cs`
- `OrcaCandleVolumeProfile.cs`
- `OrcaLegtoLegProfile.cs`
- `OrcaProfileDataProvider.cs`
- `OrcaRollingProfiles.cs`
- `OrcaStepProfile.cs`
- `OrcaTickDirectionIndex.cs`
- `OrcaVisibleRangeVolumeProfile.cs`

Additional relevant source:

- `OrcaAtrAdaptiveRangeBarsType.cs` sets `BuiltFrom = BarsPeriodType.Tick`.

Conclusion: the utilization monitor's 1 Tick row has multiple plausible Orca sources. It does not prove a visible 1-tick chart exists and does not identify one guilty indicator.

## Current Cache/Provider Architecture

`OrcaProfileDataCache` is the shared static registry in `OrcaVolumeProfileCore.cs`.

Shared producer:

- `OrcaProfileDataProvider`

Known shared consumers:

- `OrcaRollingProfiles`
- `OrcaVisibleRangeVolumeProfile`
- `OrcaCumulativeDelta`
- `OrcaTimeStatistics`

Potential publishers:

- `OrcaProfileDataProvider`
- `OrcaPrints`
- `OrcaVisibleRangeVolumeProfile` when local tick cache is active

## Risk Notes

- Multiple profile tools can hydrate separate hidden 1-tick series for the same instrument.
- Local caches classify Bid/Ask/Last independently.
- Shared provider registration depends on runtime state and timer refresh; startup ordering must be measured.
- `OrcaVisibleRangeVolumeProfile` currently allows profile recalculation inside `OnRender` when visible range/cache state changes.
- No structured startup session report exists yet.
