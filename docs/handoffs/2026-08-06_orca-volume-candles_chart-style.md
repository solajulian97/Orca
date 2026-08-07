# Orca Volume Candles ChartStyle

## Objective

Add a production-oriented NinjaTrader 8 custom `ChartStyle` named `Orca Volume Candles` that preserves the selected BarsType and all OHLCV/timestamp data while varying only each centered candle body's visual width by volume.

## Files Changed

- `Orca Trades/Working_Suite/ChartStyles/OrcaVolumeCandles.cs`
- `docs/chartstyles/ORCA_VOLUME_CANDLES.md`
- `docs/ORCA_PRODUCT_STATE.md`
- `docs/ORCA_ARCHITECTURE.md`
- `docs/handoffs/2026-08-06_orca-volume-candles_chart-style.md`

No `Full_Suite`, native NinjaTrader, or stale mirror source was changed.

## Behavior Added, Changed, Or Removed

- Registered `Orca Volume Candles` as custom `ChartStyleType` `1331053361` (`0x4F564331`, mnemonic `OVC1`).
- Added ordinary OHLC candle bodies, tick-precision dojis, centered wicks, native x/y coordinate conversion, and minimum one-pixel body dimensions.
- Added Visible Range normalization across painted bars.
- Added per-bar Rolling Lookback normalization using coordinate-compressed volume ranks and a Fenwick count tree, avoiding `O(visible * lookback)` scans.
- Added exact interpolated upper-percentile clipping, Linear/Square Root/Logarithmic scaling, sensitivity, minimum/maximum width percentages, and local spacing clamps.
- Added Open vs. Close and Previous Close color assignment.
- Preserved native bar-body and candle-outline override brushes.
- Added safe handling for unavailable volume, zero volume, equal-volume windows, one visible bar, non-equidistant centers, and compressed charts.
- Removed no existing behavior.

## User-Facing Settings

Added under `Volume Width`:

- Normalization Mode: Visible Range (default) or Rolling Lookback
- Lookback Bars: 100
- Minimum Width Percent: 15
- Maximum Width Percent: 90
- Width Sensitivity: 1.0
- Scaling Method: Linear (default), Square Root, or Logarithmic
- Outlier Clipping: enabled
- Upper Percentile: 95
- Minimum Gap Pixels: 1

Added under `Appearance`:

- Color Assignment: Open vs. Close (default) or Previous Close
- Up body brush
- Down body brush
- Doji brush
- Outline brush
- Wick brush
- Body opacity: 100
- Outline width: 1
- Wick width: 1
- Show outlines: enabled
- Show wicks: enabled

The inherited bar-width control remains available as `Base Bar Width`.

## Secondary Series Added Or Changed

None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom series is added. The ChartStyle reads only the existing `ChartBars.Bars` OHLCV arrays.

## Tick Replay Implications

None. Tick Replay is not required or inspected. Width uses the volume already stored on each selected primary bar.

## Historical-Load Implications

- Visible Range reads only painted bars.
- Rolling Lookback reads painted bars plus at most `Lookback Bars - 1` earlier bars.
- No historical reconstruction, market-data request, or whole-chart scan is performed.

## Cache Implications

No shared or persistent cache is used. Reusable per-style scratch arrays expand only when a larger range is first encountered and are detached on clone.

## Rendering Implications

- Uses SharpDX in `ChartStyle.OnRender()` with absolute bar indices.
- Uses `chartControl.GetXByBarIndex()` and `chartScale.GetYByValue()`.
- Keeps every body centered and clamps total painted body width to local adjacent-center spacing minus the configured gap and to the normal bar-width limit.
- Up/down Direct2D brushes remain owned by the native ChartStyle base.
- The style owns only doji, outline, and wick Direct2D brushes. They are disposed/recreated when the render target changes and disposed at termination.
- Per-bar override resources are used but never disposed by this style.
- Temporary brush opacity and render-target antialias mode are restored in `finally` blocks.
- No `ForceRefresh()` call or mutable calculation model exists.

## Performance Implications

- Visible Range normalization is `O(visible log visible)` because the exact clipping percentile is sorted once per render.
- Rolling Lookback normalization is `O((visible + lookback) log n)` with `O(visible + lookback)` reusable scratch storage.
- There are no allocations in the candle draw loop and no work proportional to the entire loaded chart.
- The developing bar width is recomputed during normal chart renders as its stored volume changes.

## Tests Performed In NinjaTrader

- Copied the exact source file to `Documents\NinjaTrader 8\bin\Custom\ChartStyles\OrcaVolumeCandles.cs`; source and live-copy SHA-256 hashes matched after deployment.
- Triggered NinjaTrader `F5` compilation on August 6, 2026. The NinjaScript Editor error grid was empty after the compile.
- Confirmed `ChartStyles\OrcaVolumeCandles` was registered in the NinjaScript Explorer.
- Inspected NinjaScript Output after compilation. No `OrcaVolumeCandles` error was present; the visible messages were pre-existing provider notices from another component.
- Live chart rendering and interaction checks were intentionally left to Julian.

## Compile Status

- Local .NET 8 semantic compilation against the installed NinjaTrader, SharpDX, and current `NinjaTrader.Custom.dll` assemblies passed with zero C# errors. The harness reported only cross-runtime dependency-version warnings.
- NinjaTrader `F5` compile: passed on August 6, 2026 with an empty NinjaScript Editor error grid.

## Manual-Validation Status

Pending Julian's live-chart testing. Required checks include Chart style dropdown availability, NQ 5-second visual comparison, developing-bar updates, Visible Range pan/zoom behavior, Rolling Lookback stability, property persistence, per-bar override colors, multiple series/panels, reload determinism, and responsiveness review.

## Known Issues, Risks, And Follow-Up Work

- No screenshot was present in the supplied attachment directory; the implementation was matched to the written brief, TradingView description, and installed native Candlestick/Equivolume source.
- Exact rendering appearance and responsiveness still require Julian's live NinjaTrader validation with real chart data.
- A custom style installed only in a compiled third-party DLL would not be discoverable by source search. The high mnemonic identifier materially reduces collision risk, and NinjaTrader will log any registration collision.
- The current targeted deploy script does not include a ChartStyles branch and already contains unrelated uncommitted edits, so this change does not modify it. This validation pass used a direct, exact-file copy to the live `bin/Custom/ChartStyles` directory.

## Full_Suite Promotion

Not eligible. The `F5` compile passed, but promotion still requires Julian's manual live-chart validation.
