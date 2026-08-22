# Orca HTF Candles Initial Build

## Objective

Create a native NinjaTrader 8 price-panel overlay named `Orca HTF Candles` that reproduces the supplied TradingView HTF body/wick appearance while using actual NinjaTrader secondary-series OHLC, close-stamp-aware X placement, bounded snapshots, and cached SharpDX resources.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaHTFCandles.cs` (new)
- `docs/indicators/ORCA_HTF_CANDLES.md` (new)
- `docs/indicators/ORCA_SOURCE_MAP.md`
- `docs/ORCA_ARCHITECTURE.md`
- `docs/ORCA_PRODUCT_STATE.md`
- `docs/handoffs/2026-08-21_orca-htf-candles_initial-build.md` (new)

No file in `Orca Trades/Full_Suite`, `Orca Trades/NinjaTrader`, `Stable_Release`, or `decompiled` was modified.

## Behavior added, changed, or removed

- Added translucent HTF candle bodies, opaque borders, and direction-selectable wicks behind the primary chart bars.
- Added a single Timeframe selector with 5m, 15m, 30m, 1h, 2h, 4h, 1d, and 1w presets.
- Added native developing-candle updates from the current secondary HTF bar.
- Added completed-candle retention equivalent to Pine's 200 completed objects plus one current object.
- Added bullish/bearish direction changes while the HTF candle forms; `close >= open` is bullish, including dojis.
- Added graceful no-render behavior when the selected period is equal to or below a comparable time-based primary chart period.
- Removed no existing behavior.

## User-facing settings added, changed, deprecated, or removed

Added under `General`:

- Timeframe (default 1 Hour)
- Candle Lookback (default 200, range 50-400; completed candles plus one active candle)

Added under `Appearance`:

- Bull Body (`#4CAF50`)
- Bear Body (`#FF5252`)
- Border (`#2E2E2E`)
- Bull Wick (`#2E2E2E`)
- Bear Wick (`#2E2E2E`)
- Transparency (default 85, range 0-100; body only)
- Border Width (default 1, range 1-5)
- Wick Width (default 1, range 1-5)

No setting was deprecated or removed.

## Secondary series added or changed

The new indicator adds exactly one same-instrument secondary series according to the Timeframe setting:

- Minute 5, 15, 30, 60, 120, or 240
- Day 1
- Week 1

No Tick, Second, Bid, Ask, Volumetric, or custom series is added. Existing indicators and their series declarations are unchanged.

## Tick Replay implications

Tick Replay is not required. The indicator uses native HTF OHLC and has no `OnMarketData` path. The calculation mode is `Calculate.OnPriceChange`; the active OHLC changes whenever the secondary bar price changes.

## Historical-load implications

- Historical HTF bars come directly from the selected secondary series.
- The completed queue is capped at Candle Lookback.
- Immutable render arrays are published in 64-HTF-bar historical batches and once at `State.Transition`, avoiding one full array allocation per historical HTF bar.
- A larger requested lookback does not force additional history beyond what NinjaTrader loads for the chart and secondary series.

## Cache implications

- No shared profile cache or persistent cache is used.
- The indicator owns a bounded queue of completed candle structs, one immutable completed render array, and one active candle struct.
- Completed primary boundary indices are cached when the HTF candle closes.

## Rendering implications

- `IsOverlay`, `DrawOnPricePanel`, and `IsAutoScale = false` are set; no plot or price marker is created.
- `SetZOrder(-1000)` places the overlay behind primary bars.
- `OnRender` reads only immutable completed data plus a lock-copied active struct.
- SharpDX brushes are cached per render target and disposed on target changes/termination.
- Bodies are panel-clipped, dojis remain visible at one pixel, and the wick uses the scheduled time midpoint.
- No model calculation, collection mutation, LINQ, drawing-object creation, Output logging, or full-history scan occurs inside `OnRender`.

## Performance implications

- Realtime model work is constant-time per secondary price change.
- Completed snapshot publication occurs only at HTF transitions in realtime.
- Rendering visits at most Candle Lookback completed candles plus the active candle and skips bodies outside the panel after X resolution.
- Completed candles reuse cached primary boundary indices; only the active/fallback boundaries require a binary search or time projection.

## Tests performed in NinjaTrader

- Targeted Working_Suite deployment copied only `OrcaHTFCandles.cs` to the live NinjaTrader Indicators folder.
- NinjaTrader's automatic source watcher includes `Indicators\OrcaHTFCandles.cs` in `NinjaTrader.Custom.csproj` and regenerated `NinjaTrader.Custom.dll`/`.pdb` at 23:16:03, after the final live-source write at 23:15:59. This is platform semantic-compile evidence.
- No chart was opened or modified, and no historical, Market Replay, or live visual behavior was tested.
- An explicit F5 keystroke was not sent because Windows app control was not approved for NinjaTrader.

## Static tests performed

- Isolated .NET semantic compilation against the installed NinjaTrader Core/Gui/Custom, WPF, SharpDX, and SharpDX.Direct2D1 assemblies passed with zero C# errors. The harness emitted only cross-assembly dependency-version warnings.
- `git diff --check` passed for the new source before documentation updates.
- Native NinjaTrader Minute, Day, and Week BarsType source was inspected to verify close-stamp behavior and calendar aggregation.
- The strict primary-boundary resolver follows the existing Step Profile convention: first primary timestamp strictly later than the logical boundary.
- Screenshot swatches were sampled read-only to establish `#4CAF50`, `#FF5252`, and `#2E2E2E` defaults.
- Normalized Working_Suite/live authored-region SHA-256 parity passed at `018F814ADA53F5AAF55A84B60E54245DC1182E869EC99D3E9B44A579334220D`; NinjaTrader's generated-code region exists only in the live copy.

## Compile status

- Isolated semantic compile: passed.
- NinjaTrader automatic source-watcher semantic compile: passed; Custom project/DLL/PDB regenerated after deployment.
- Explicit NinjaTrader F5: not performed because app control was not approved.

## Deployment status

- Targeted deployment: complete.
- Live destination: `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaHTFCandles.cs`.
- Normalized source/live parity: passed.

## Manual-validation status

Pending Julian validation for:

1. Five-minute primary / one-hour HTF comparison with the supplied TradingView screenshot.
2. One-minute / 15-minute.
3. Five-second / one-hour.
4. Range / one-hour.
5. Volume / one-hour.
6. Overnight ETH boundary and 5:00-6:00 PM maintenance break.
7. Historical reload stability.
8. Market Replay active-candle changes and rollover.
9. Bull/bear flips and dojis.
10. Pan/zoom responsiveness with 200 or more HTF candles.

## Known issues, risks, and follow-up work

- Native provider Day/Week OHLC may not exactly reproduce every custom Trading Hours template; this requires chart-specific validation.
- On non-time primary charts, future active-candle width depends on NinjaTrader's time-to-X projection until more primary bars exist.
- Pine uses an unconditional nominal-duration right edge. This implementation intentionally honors NinjaTrader's shortened intraday final-bar close stamp.
- Official TradingView documentation classifies unoffset developing HTF `request.security()` values as repainting between realtime and historical execution. The new NinjaTrader implementation permits only the active candle to develop and freezes completed native secondary candles.
- No controlled render-time or callback benchmark has been run.

## Promotion eligibility

Not eligible for `Full_Suite`. Eligibility requires a successful NinjaTrader F5 compile and Julian's historical, Market Replay, and live-chart validation.
