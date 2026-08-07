# Orca Time Statistics Contextual Bar Metrics

## Objective

Add opt-in per-bar metrics that describe participation, speed, candle efficiency, and rejection without changing the existing default Time Statistics layout. Preserve the current startup guards, guarded bar reads, render-target resource handling, average-column placement, and far-right row-label alignment.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs`
- `docs/handoffs/2026-08-07_orca-time-statistics_contextual-bar-metrics.md`

## Behavior added, changed, or removed

- Added Volume / Tick: bar volume divided by the bar's high-low range in instrument ticks.
- Added Delta / Sec: signed bar delta divided by bar duration in seconds.
- Added Body: absolute close-open distance, formatted in the same price units as Range.
- Added Body %: absolute candle body divided by total high-low range.
- Added Close Location: close position within the range, where 0% is the low and 100% is the high.
- Added Upper Wick and Lower Wick: each wick divided by total range.
- Added lookback averages for every new metric. Delta / Sec follows the existing Delta average convention and averages absolute magnitude.
- Zero-range bars leave range-normalized metrics blank. Bars without a valid positive duration leave per-second metrics blank.
- No existing row was removed or enabled/disabled by default.

## User-facing settings

Added the following opt-in settings under `Rows`, all defaulting to `false`:

- `Show Volume / Range Tick`
- `Show Delta / Second`
- `Show Body`
- `Show Body %`
- `Show Close Location`
- `Show Upper Wick %`
- `Show Lower Wick %`

## Secondary series

No secondary series was added or changed. No Tick, Second, Bid, Ask, Last, Volumetric, or custom series was introduced.

## Tick Replay implications

No Tick Replay requirement was added. Delta-based rows retain the indicator's existing data-source behavior and limitations. The new candle-shape rows use primary-series OHLC data through guarded accessors.

## Historical-load implications

The new rows calculate from already available per-bar data during the bounded visible-bar render pass and average lookback. They do not initiate a historical rebuild. Per-second rows depend on valid neighboring bar timestamps and remain blank when duration cannot be established safely.

## Cache implications

No cache was added or changed. Context Map integration is intentionally deferred. A future integration should publish an immutable, calculation-thread snapshot keyed to the chart/instrument context; `OrcaSessionContextMap` should consume that snapshot without traversing mutable Time Statistics collections in `OnRender`.

## Rendering implications

- The new metrics use the existing SharpDX resource lifecycle and current render-target tracking.
- Average values remain beside the latest rendered bar; row labels remain independently aligned at the far-right edge.
- OHLC and timestamp reads remain inside fail-soft helpers. No unguarded `Bars.GetOpen`, `Bars.GetClose`, `Bars.GetHigh`, `Bars.GetLow`, or `Bars.GetTime` call was added to startup or render control flow.
- Percentage values are clamped to their expected 0-100 domain through the candle calculation.

## Performance implications

- All new rows default off.
- Work is limited to the existing visible-bar scan and configured average lookback.
- No new data series, synchronization wait, historical profile rebuild, or persistent per-tick allocation path was added.
- Multiple enabled candle-shape rows share one guarded OHLC calculation per bar.

## Chart-style interpretation

- Time bars: 30-second through 2-minute charts are the clearest execution/scalp use case for rate metrics; 3- to 5-minute charts can provide intraday swing confirmation. Larger bars emphasize candle efficiency and close location more than transient rate.
- Range bars: Body %, close location, wick percentages, and per-second speed remain informative. Volume / Tick has a mostly fixed denominator and therefore behaves primarily like rescaled volume.
- Volume bars: Body %, close location, wick percentages, Delta %, and Delta / Sec remain informative. Raw volume is constrained by construction, while duration and per-second values describe how quickly a bar completed.
- Tick bars: per-second values describe event speed; candle-shape metrics remain useful. Raw volume is partly constrained by the chart definition.
- Recommended use is multi-horizon: faster bars for a trigger and slower bars for regime or confirmation. Rate metrics decay faster than candle-shape and location metrics.

## Tests performed

- Verified exactly one NinjaScript generated-code region.
- Verified balanced braces and parentheses.
- Verified `git diff --check` passes.
- Audited every direct primary-series OHLC/time read and confirmed it remains inside a guarded helper.
- Verified all seven new rows have defaults, row registration, rendering, averages, formatting, and public settings.

## Compile status

Not compiled in NinjaTrader. Repository structural checks passed; NinjaTrader `F5` remains required after deployment.

## Manual-validation status

Pending Julian's NinjaTrader validation. Test each row individually and together, with averages on and off, on time, range, and volume charts. Confirm zero-range and developing-bar behavior, average placement, far-right labels, and chart reload/pan/zoom stability.

## Known issues, risks, and follow-up work

- Delta quality still depends on the existing provider/backfill/live-data path.
- Developing-bar duration can change while the bar is active, so per-second values are intentionally dynamic.
- Range and volume chart definitions constrain some denominators; interpretation matters more than raw cross-chart comparison.
- Future Context Map integration needs an explicit snapshot schema and thresholds before implementation. Useful candidates are participation, directional pressure, candle efficiency, rejection, and speed percentiles relative to the configured lookback.

## Promotion eligibility

Not eligible for promotion to `Full_Suite` until NinjaTrader `F5` compilation and live manual validation are confirmed by Julian.
