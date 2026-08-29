# Orca HTF Candles Directional Borders, Daily Close, And Label Toggle

## Objective

Add optional bull/bear border colors, align `1 Day` candle geometry to a 17:00 Eastern close, and document the existing bottom-label visibility toggle so it can be used to avoid overlap with session-profile labels.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaHTFCandles.cs`
- `docs/indicators/ORCA_HTF_CANDLES.md`
- `docs/handoffs/2026-08-29_orca-htf-candles_directional-borders-daily-close-label-toggle.md`

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- Added `Use Directional Borders`. When enabled, bullish candles use `Bull Border` and bearish candles use `Bear Border`; when disabled, the existing common `Border` brush remains in use.
- Added separate serialized `Bull Border` and `Bear Border` brushes with green/red defaults.
- Changed `1 Day` to aggregate the same-instrument 30-minute secondary bars into an explicit 18:00-17:00 Eastern window. This registers the close at 17:00 and includes the final close-stamped 16:00-17:00 source bar.
- Kept the existing `Show Candle Labels` setting. It suppresses the bottom weekday/session labels without changing candle data or rendering.
- Doji direction remains bullish (`close >= open`) for body, wick, and directional border selection.

## User-facing settings added, changed, deprecated, or removed

Added under `Appearance`:

- `Use Directional Borders` (default disabled)
- `Bull Border`
- `Bear Border`

Existing `Show Candle Labels` remains under `Display Controls` and defaults enabled for backward compatibility. Turn it off when the session profile already supplies bottom labels.

## Secondary series added or changed

`1 Day` now uses the same-instrument Minute 30 secondary series so the explicit 18:00-17:00 Eastern session can be honored. Weekly and the Eastern session modes continue to use Minute 30 aggregation.

## Tick Replay implications

No Tick Replay or market-data event path was added. The feature remains compatible with `Calculate.OnPriceChange` and does not use `OnMarketData`.

## Historical-load implications

- Daily OHLC is aggregated from available 30-minute bars and remains provider/Trading Hours-template dependent.
- Daily rendering boundaries now use the chart-timezone/Eastern conversion path and the 17:00 Eastern wall-clock close.
- Historical candle availability and partial first windows are unchanged.

## Cache implications

No calculation or shared-cache model changed. Two additional render-target-bound Direct2D brushes are cached with the existing body, border, wick, and label resources.

## Rendering implications

- Directional border selection occurs during the existing candle render using the candle's bull/bear classification.
- The common `Border` brush remains the fallback when directional borders are disabled or a directional brush is unavailable.
- All new Direct2D brushes are rebuilt on render-target changes and disposed on termination.
- `Show Candle Labels` continues to gate bottom label drawing before any text allocation or draw call.

## Performance implications

The normal render path adds one branch and one cached brush selection per visible candle. No persistent drawing objects, per-tick allocations, LINQ, or model work were added.

## Tests performed in NinjaTrader

Targeted file copy completed to `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaHTFCandles.cs`; no NinjaTrader F5, chart, Market Replay, or live-session validation has been performed.

## Static tests performed

- Isolated semantic compilation against the live NinjaTrader, WPF, and SharpDX assemblies passed with zero C# errors; warnings were assembly-version/reference conflicts from the external harness.
- Source inspection confirmed standard `[XmlIgnore]` brush properties with hidden NinjaTrader serialization properties for both directional borders.
- Source inspection confirmed `Show Candle Labels` already gates the label render path and remains independent of candle calculations.
- `git diff --check` passed apart from the repository's existing LF/CRLF normalization warnings.

## Compile status

- Isolated C# compile: passed.
- NinjaTrader targeted file copy: complete; authored-region comparison against `Working_Suite` passed after line-ending normalization.
- NinjaTrader F5 and Custom-assembly load: pending.

## Manual-validation status

Pending validation of:

1. Directional borders with fill transparency at 100% and different bull/bear border brushes.
2. Common-border fallback when `Use Directional Borders` is disabled.
3. Day 1 candles ending at 17:00 Eastern on an ETH chart and on a non-Eastern chart timezone.
4. `Show Candle Labels` off with `Asia / London / New York` so session-profile labels remain unobstructed.
5. Dojis, reload, Market Replay, and live direction changes.

## Known issues, risks, and follow-up work

- Daily values are aggregated from available 30-minute bars; the selected Trading Hours template and provider history still determine whether the evening and final 16:00-17:00 bars are present.
- `Show Candle Labels` currently gates both Day 1 weekday labels and Asia/London/RTH labels together.
- Smart App Control/Code Integrity and NinjaTrader F5/load status remain separate runtime gates.

## Promotion eligibility

Not eligible for `Full_Suite` promotion until NinjaTrader F5/load and Julian's chart/runtime validation are complete.
