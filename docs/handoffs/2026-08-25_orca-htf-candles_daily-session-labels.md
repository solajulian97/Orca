# Orca HTF Candles Daily And Session Labels

## Objective

Add centered bottom labels to `Orca HTF Candles` for the `1 Day` and `Asia / London / New York` modes.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaHTFCandles.cs`
- `docs/indicators/ORCA_HTF_CANDLES.md`
- `docs/handoffs/2026-08-25_orca-htf-candles_daily-session-labels.md`

## Behavior added, changed, or removed

- `1 Day` candles display their trading-day name: Monday, Tuesday, Wednesday, Thursday, or Friday.
- `Asia / London / New York` candles display `Asia`, `London`, or `RTH` according to the existing Eastern session start.
- Labels are centered along the bottom of each candle's logical start/end span.
- Labels are omitted when their logical center is outside the visible chart panel.
- `ETH / RTH`, Weekly, and fixed intraday modes are unchanged.
- No existing behavior was removed.

## User-facing settings added, changed, deprecated, or removed

No settings were added, changed, deprecated, or removed. Labels are automatic in the two applicable timeframe modes.

## Secondary series added or changed

No secondary series were added or changed. `1 Day` continues to use Day 1. `Asia / London / New York` continues to use one Minute 30 series.

## Tick Replay implications

No Tick Replay behavior changed. Tick Replay remains unnecessary for this indicator.

## Historical-load implications

No historical aggregation or loading behavior changed. Labels are derived from the immutable candle snapshots already available to rendering.

## Cache implications

No shared or persistent cache behavior changed.

## Rendering implications

- Added one render-target-bound label brush and one persistent DirectWrite text format.
- Label X coordinates reuse each candle's existing logical start/end coordinates.
- The daily label uses the candle end/trading date so a Sunday 6:00 PM futures-session start correctly labels the candle `Monday`.
- Three-session labels use the existing DST-aware chart-time-to-Eastern conversion before matching 6:00 PM, 3:00 AM, or 9:30 AM.
- The existing chart-panel clip handles label clipping.

## Performance implications

The render path adds one constant-time label lookup and at most one DirectWrite draw per visible applicable candle. Resources are created once per render target and disposed on target change or termination. No model mutation, aggregation, cache access, LINQ, synchronization, or per-tick work was added.

## Tests performed in NinjaTrader

Pending F5 compilation and chart validation.

## Static tests performed

- Roslyn syntax parse passed with zero syntax errors.
- Opening and closing brace counts match.
- `git diff --check` passed with only repository line-ending warnings.
- Label truth-table checks passed for Monday, Tuesday, Asia, London, and RTH examples.
- Source audit confirmed label resources are disposed with existing SharpDX resources.

## Compile status

Targeted deployment completed with `deploy_orca.ps1 -Target OrcaHTFCandles`. NinjaTrader F5 compile remains pending.

The normalized authored region matches the live NinjaTrader copy with SHA-256 `6694DF0E06488EF0BED119A465CE06CAC0931A5E7E8E773B51EB1A8685674EBE`. The live copy contains one NinjaScript generated-code region.

## Manual-validation status

Pending. Validate weekday labels on `1 Day`, all three labels on `Asia / London / New York`, pan/zoom stability, and partially visible candles.

## Known issues, risks, and follow-up work

- At extreme horizontal compression, the fixed 12 px label can be constrained by a narrow candle span.
- Holiday and partial-window label availability follows the existing candle availability and scheduled geometry.
- `ETH / RTH` remains unlabeled in this pass because the request explicitly named daily weekdays and Asia/London/RTH.

## Promotion eligibility

Not eligible for promotion to `Full_Suite` until NinjaTrader compiles and Julian confirms the live labels.
