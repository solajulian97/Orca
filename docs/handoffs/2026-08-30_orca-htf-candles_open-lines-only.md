# Orca HTF Candles Open Lines Only

## Objective

Add an optional display mode that shows only the open of each higher-timeframe candle as a horizontal line spanning that candle's complete time window.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaHTFCandles.cs`
- `docs/indicators/ORCA_HTF_CANDLES.md`
- `docs/handoffs/2026-08-30_orca-htf-candles_open-lines-only.md`

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- Added `Open Lines Only`, disabled by default.
- When enabled, candle bodies, body borders, and wicks are suppressed.
- Each visible candle draws one horizontal line at its open from the candle's logical start through its logical end, which is also the next candle's scheduled start for contiguous periods.
- Bottom labels remain independently controlled by `Show Candle Labels`.
- Standard candle rendering is unchanged when the option is disabled.

## User-facing settings added, changed, deprecated, or removed

Added under `Appearance`:

- `Open Lines Only` (default disabled)

The line uses the existing `Border Width`. It uses `Border` when directional borders are disabled, or `Bull Border` / `Bear Border` when `Use Directional Borders` is enabled.

## Secondary series added or changed

No secondary series or aggregation logic changed.

## Tick Replay implications

No Tick Replay requirement, market-data subscription, or calculation mode changed.

## Historical-load implications

None. The mode renders the existing completed and developing candle snapshots.

## Cache implications

No cache or snapshot model changed. Existing render-target brushes are reused.

## Rendering implications

- Open-only rendering uses one Direct2D line per visible candle.
- Wick midpoint lookup and all body/wick geometry are skipped in this mode.
- Rendering remains clipped to the chart panel and behind primary bars.

## Performance implications

This mode performs less geometry and fewer Direct2D calls than full candle rendering. It adds no persistent drawing objects or per-frame collections.

## Tests performed in NinjaTrader

Targeted file copy completed to `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaHTFCandles.cs`. Authored-region comparison against `Working_Suite` passed after line-ending normalization. NinjaTrader F5/load and chart validation remain pending.

## Compile status

- Isolated semantic compilation against the installed NinjaTrader, WPF, and SharpDX assemblies: passed with zero C# errors; warnings were external-harness reference/version conflicts.
- NinjaTrader targeted file copy: complete.
- NinjaTrader F5 and Custom-assembly load: pending.

## Manual-validation status

Pending validation on a 1-minute chart with 30-minute HTF candles, including scrolling, zooming, the developing candle, session boundaries, and `Show Candle Labels` on/off.

## Known issues, risks, and follow-up work

- With directional borders enabled, the active open line can change bull/bear color while its candle develops. Disable directional borders for a fixed open-line color.
- On non-time charts, projection to the scheduled end remains subject to NinjaTrader's existing `GetXByTime()` behavior.

## Promotion eligibility

Not eligible for `Full_Suite` promotion until NinjaTrader F5/load and Julian's chart validation are complete.
