# Orca Price Action - Initial V1 Build

Date: 2026-08-15

## Objective

Implement the complete first version of Orca Price Action as one primary-series
overlay containing FVG/iFVG, volume imbalance, market structure, rejection
blocks, and structural, continuation, and propulsion order blocks.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- `docs/indicators/ORCA_SOURCE_MAP.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

Added one new indicator with completed-bar causal state, intrabar FVG remaining
geometry, classic/advanced VI, confirmed pivots and protected structure,
pivot-qualified rejection blocks, and typed S-OB/C-OB/PB detection. Added
stable IDs, origin/confirmation/effective event bars, lifecycle state, typed
quality, FVG confluence, parent linkage, and immutable render snapshots.

No existing indicator behavior was changed.

## User-facing settings added, changed, deprecated, or removed

Added:

- Clean Core, Blocks Focused, Full Context, and Custom display presets;
- FVG size, directional, displacement, fill, iFVG, timed-period, and RTH
  settings;
- VI Classic/Advanced mode;
- structure pivot strength, close buffer, BOS/CHoCH/sweep/protected-level, and
  role-badge settings;
- rejection Strict/Balanced/Aggressive/Custom settings;
- order-block Standard/Strict/Broad/Custom thresholds, windows, per-type FVG
  modes, invalidation, display, open, midpoint, and mean-hold settings;
- lifecycle visibility, opacity, font, color, retention, and diagnostics-panel
  settings.

No setting was changed, deprecated, or removed from another component.

## Secondary series added or changed

None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom secondary series
was added. There is no `AddDataSeries` call.

## Tick Replay implications

Tick Replay is not required. There is no `OnMarketData`. Structural decisions
use completed primary OHLC. Realtime `OnPriceChange` callbacks only update the
remaining geometry of already-confirmed FVGs.

## Historical-load implications

The default discovery window is the final 3,000 loaded primary bars. Pivot and
event storage is pruned outside the configured window when it is no longer an
active protected/target dependency. Active zones are not age-expired; terminal
records are capped per type.

## Cache implications

None. The indicator does not access `OrcaProfileDataCache` or introduce a local
market-data cache. Its lists are instance-owned calculation state.

## Rendering implications

`OnRender` consumes immutable zone, line, and label arrays. It performs no
detector work, historical scan, mutable model traversal, cache access, or
synchronization wait. SharpDX resources are render-target scoped and disposed.

## Performance implications

One completed-bar pass updates bounded or terminal-pruned model collections.
Open-bar callbacks scan active FVGs for geometry updates and publish the copied
render snapshot. Diagnostics warn above 500 active records without silently
evicting active zones. Dense-history runtime timing still requires live chart
measurement.

## Tests performed in NinjaTrader

None. NinjaTrader `F5`, chart loading, MNQ 1-minute behavior, reload parity,
pan/zoom behavior, 15-second behavior, and range-chart behavior remain pending.

Outside NinjaTrader, 17 deterministic compiled truth cases passed for bullish,
bearish, overlapping, and equal-edge FVGs; Classic/Advanced bullish and bearish
VI including true-gap exclusion; 15-minute, 1-hour, and 4-hour New York bucket
anchoring; and strict bullish/bearish break equality and buffer behavior.

Source invariants also passed for no secondary series/`OnMarketData`, separate
intrabar-geometry and completed-bar FVG state paths, protected-pivot CHoCH,
typed block engines, preset threshold coverage, immutable snapshot publication,
and no calculation-model collection access from `OnRender`.

## Compile status

- Local .NET Framework semantic compile against installed NinjaTrader and the
  currently deployed custom assembly: passed with no errors or warnings.
- Local semantic compile including the current Working_Suite
  `OrcaDiagnosticsCore.cs`: passed. Expected duplicate-type warnings were
  emitted because the same diagnostics types also exist in the referenced
  deployed `NinjaTrader.Custom.dll`.
- Targeted deployment to
  `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaPriceAction.cs`:
  passed. Normalized Working_Suite/live SHA-256 values both equal
  `2b86132cf84026229671fd10a19ed3c2c44b6d7738e5ed2cee8949b30d18aa30`.
- NinjaTrader `F5`: pending.

## Manual-validation status

Pending Julian's live NinjaTrader validation. Static compilation is not manual
validation.

## Known issues, risks, and follow-up work

- Visual density and MNQ 1-minute numeric calibration require chart review.
- The New York conversion follows the existing Orca session-clock convention;
  confirm behavior when NinjaTrader and Windows use different display zones.
- The current VI Advanced predicate broadens candle-color eligibility while
  preserving body-gap direction and true-gap exclusion; compare it against the
  requested reference on live examples.
- No alerts, MTF inputs, volume/delta filters, or separate internal/external
  pivot engines are included in V1.

## Promotion eligibility

Not eligible for `Full_Suite`. Eligibility requires successful NinjaTrader F5
compilation and Julian's manual MNQ/time/range-chart validation.
