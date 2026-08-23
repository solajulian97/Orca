# Orca HTF Candles Eastern Session Options

## Objective

Extend `Orca HTF Candles` with a futures-week candle and two Eastern-time session aggregations while preserving the existing overlay, rendering, lookback, and active-candle behavior.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaHTFCandles.cs`
- `docs/indicators/ORCA_HTF_CANDLES.md`
- `docs/indicators/ORCA_SOURCE_MAP.md`
- `docs/ORCA_ARCHITECTURE.md`
- `docs/handoffs/2026-08-21_orca-htf-candles_initial-build.md` (corrected prior Smart App Control compile/load status)
- `docs/handoffs/2026-08-22_orca-htf-candles_eastern-session-options.md`

No `Full_Suite`, `Orca Trades/NinjaTrader`, `Stable_Release`, or `decompiled` file was changed.

## Behavior added, changed, or removed

- Changed the existing `Week1` display label from `1 Week` to `Weekly` while retaining the enum identity for template compatibility.
- Changed Weekly construction from a provider-native Week bar to a native 30-minute aggregation scheduled from Sunday 18:00 through Friday 17:00 Eastern.
- Added `ETH / RTH`:
  - ETH/Overnight: 18:00-09:30 Eastern.
  - RTH: 09:30-17:00 Eastern.
- Added `Asia / London / New York`:
  - Asia: 18:00-03:00 Eastern.
  - London: 03:00-09:30 Eastern.
  - New York/RTH: 09:30-17:00 Eastern.
- Excluded the 17:00-18:00 maintenance interval.
- Restricted overnight/Asia starts to Sunday through Thursday and daytime sessions to Monday through Friday.
- Added chart-timezone/Eastern conversion with daylight-saving handling through Windows `Eastern Standard Time` timezone rules.
- Preserved complete scheduled body projection, direction changes, doji classification, body-only transparency, and completed-candle stability.
- Removed no existing fixed-timeframe option or rendering behavior.

## User-facing settings added, changed, deprecated, or removed

Added to the existing `Timeframe` selector:

- `ETH / RTH`
- `Asia / London / New York`

Renamed the displayed `1 Week` option to `Weekly`; the underlying `Week1` enum value is retained. No other setting or default changed.

`Candle Lookback` counts individual rendered candles. Therefore 200 means 200 weekly candles in Weekly mode, 200 ETH/RTH session candles in the two-session mode, or 200 Asia/London/New York session candles in the three-session mode, plus the active candle.

## Secondary series added or changed

- Existing 5/15/30/60/120/240-minute and Day 1 selections are unchanged.
- Weekly now adds one same-instrument Minute 30 series instead of Week 1.
- `ETH / RTH` adds one same-instrument Minute 30 series.
- `Asia / London / New York` adds one same-instrument Minute 30 series.

All requested boundaries align exactly to 30 minutes. No Tick, Second, Bid, Ask, Last-specific, Volumetric, or custom BarsType series was added.

## Tick Replay implications

Tick Replay is not required. Custom OHLC is aggregated from native 30-minute open/high/low/close values under `Calculate.OnPriceChange`. No `OnMarketData` path was added.

## Historical-load implications

- Historical custom candles are built incrementally from loaded 30-minute bars.
- The first loaded custom window can be partial if loaded source history begins after its scheduled opening boundary.
- The chart's Trading Hours template controls which 30-minute bars exist. RTH-only data cannot supply overnight, Asia, or London OHLC.
- Holidays and missing source bars are not synthesized. Scheduled window geometry remains fixed at the requested Eastern boundary.
- Completed candles remain bounded by `Candle Lookback`, and historical render-array publication remains batched every 64 completed candles.

## Cache implications

- No shared or persistent cache was added.
- Existing bounded completed snapshots and one active snapshot remain the only render model.
- A last-processed 30-minute source index supports idempotent realtime updates and bounded catch-up after chart suspension.

## Rendering implications

- SharpDX rendering, Z-order, brush lifecycle, clipping, doji treatment, and primary-boundary mapping are unchanged.
- Custom candle start/end times are converted from Eastern to NinjaTrader's configured chart timezone before X mapping.
- Custom wick midpoint uses absolute time through the chart timezone conversion path.
- `OnRender` still performs no session aggregation, cache access, history reconstruction, LINQ, or collection mutation.

## Performance implications

- Custom model work is constant-time for a normal 30-minute source update.
- A resumed suspended chart processes only missed loaded 30-minute source indices once to restore the active aggregate.
- Weekly no longer requests provider Week bars; it uses the same bounded 30-minute aggregation path as the session modes.
- No controlled NinjaTrader callback or render benchmark has been run.

## Tests performed in NinjaTrader

- None. The updated source was not deployed because the previously diagnosed Windows Smart App Control policy blocks NinjaTrader from loading its unsigned generated temporary Custom assembly.
- No F5, historical chart, Market Replay, or live session test was performed.

## Static tests performed

- Isolated semantic compilation against installed NinjaTrader, WPF, and SharpDX assemblies passed with zero C# errors. The harness emitted cross-assembly version and old-live-type conflict warnings only.
- Fourteen boundary assertions extracted from the exact source helpers passed for Weekly, ETH/RTH, Asia, London, New York, the 17:00 maintenance boundary, and the Friday-night exclusion.
- Eastern timezone conversion was checked at UTC-5 in winter and UTC-4 in summer.
- Source inspection confirmed one secondary series per setting, no Tick Replay path, and no new work in `OnRender`.

## Compile status

- Isolated semantic compile: passed.
- NinjaTrader assembly generation/load: not attempted for this change.
- NinjaTrader F5: pending after the Smart App Control policy is resolved by Julian.

## Manual-validation status

Pending Julian validation for:

1. Weekly Sunday 18:00 through Friday 17:00 Eastern.
2. ETH 18:00-09:30 and RTH 09:30-17:00 transitions.
3. Asia 18:00-03:00, London 03:00-09:30, and New York 09:30-17:00 transitions.
4. The 17:00-18:00 maintenance gap.
5. A chart configured outside Eastern time.
6. Historical reload, Market Replay, and realtime transitions.
7. Holiday/early-close behavior and missing-source-data behavior.
8. Pan/zoom responsiveness with a 200-candle lookback.

## Known issues, risks, and follow-up work

- The user said EST; implementation interprets this as Eastern market time with daylight-saving transitions, not fixed UTC-5 year-round.
- An RTH-only Trading Hours template cannot produce overnight/Asia/London OHLC.
- The first custom candle can be partial when loaded history begins inside its scheduled window.
- Holiday early closes retain the scheduled window edge rather than shrinking geometry to the final source bar.
- Smart App Control currently prevents the required NinjaTrader Custom-assembly load gate.

## Promotion eligibility

Not eligible for `Full_Suite`. Eligibility requires successful NinjaTrader F5/assembly load and Julian's historical, Market Replay, timezone, and live-session validation.
