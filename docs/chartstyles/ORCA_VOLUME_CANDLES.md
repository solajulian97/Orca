# Orca Volume Candles

Source: `Orca Trades/Working_Suite/ChartStyles/OrcaVolumeCandles.cs`

`Orca Volume Candles` is a NinjaTrader 8 `ChartStyle`. It renders the currently selected bar series as OHLC candlesticks whose body width varies with the existing bar volume. It does not aggregate market data, change timestamps, add a series, or require Tick Replay.

## Registration

- Display name: `Orca Volume Candles`
- Class: `OrcaVolumeCandles`
- `ChartStyleType`: `1331053361` (`0x4F564331`, mnemonic `OVC1`)
- The identifier is above NinjaTrader's reserved range and did not collide with any ChartStyle identifier found in installed or repository source on 2026-08-06.

## Normalization

For a volume `v`, reference minimum `vMin`, and upper reference `vUpper`:

`n = clamp((clamp(v, vMin, vUpper) - vMin) / (vUpper - vMin), 0, 1)`

If the reference values are equal, the width factor is `0.5`. If the current volume is unavailable, the factor is `0` so the bar fails to the minimum width. Zero is a valid volume.

When outlier clipping is enabled, `vUpper` is the configured linearly interpolated percentile:

- Sort the applicable volumes.
- `p = (count - 1) * UpperPercentile / 100`.
- Interpolate between the values at `floor(p)` and `ceil(p)`.

Scaling transforms are:

- Linear: `t = n`
- Square Root: `t = sqrt(n)`
- Logarithmic: `t = ln(1 + 9n) / ln(10)`
- Sensitivity: `s = t^(1 / WidthSensitivity)`

The percentage and final width are:

- `widthPercent = MinPercent + (MaxPercent - MinPercent) * s`
- `available = max(1, min(configuredPaintWidth, localCenterSpacing - MinimumGapPixels))`
- `paintWidth = max(1, min(available, round(available * widthPercent / 100)))`

The geometric body is reduced by its outline allowance so the complete painted body, including outline, remains inside `paintWidth`. The body stays centered at `chartControl.GetXByBarIndex()`.

### Visible Range

The reference population is `ChartBars.FromIndex` through `ChartBars.ToIndex`. Scrolling or zooming changes that population and recomputes widths.

### Rolling Lookback

Each bar uses its own trailing window including the current bar, limited by `Lookback Bars`. The implementation processes only the visible range plus the minimum leading lookback. Coordinate-compressed volumes and a Fenwick count tree provide exact rolling minima/percentiles in `O((visible + lookback) log n)` time instead of scanning the lookback for every painted bar.

## Defaults

| Setting | Default |
| --- | --- |
| Normalization Mode | Visible Range |
| Lookback Bars | 100 |
| Minimum Width Percent | 15 |
| Maximum Width Percent | 90 |
| Width Sensitivity | 1.0 |
| Scaling Method | Linear |
| Outlier Clipping | Enabled |
| Upper Percentile | 95 |
| Minimum Gap Pixels | 1 |
| Color Assignment | Open vs. Close |
| Body opacity | 100 |
| Outline width | 1 |
| Wick width | 1 |
| Show outlines | Enabled |
| Show wicks | Enabled |

Up/down body brushes use NinjaTrader's configured primary up/down colors. Doji, outline, and wick brushes default to the current chart-stroke color, with a dim-gray fallback.

## Rendering And Persistence

- Uses absolute bar indices and native x/y conversions.
- Reads only OHLCV from the supplied `ChartBars.Bars`.
- Honors native bar-body and candle-outline override brushes.
- Uses instrument price comparison for doji and direction precision.
- Reuses render scratch arrays and allocates only when a larger range first appears.
- Uses the native `UpBrushDX`/`DownBrushDX`; owns only doji, outline, and wick Direct2D brushes.
- Disposes and recreates owned brushes in `OnRenderTargetChanged()` and disposes them again at termination.
- Restores temporary body-brush opacity and render-target antialias mode after each use.
- Uses Orca/NinjaTrader brush string serialization for template/workspace persistence.
- Does not call `ForceRefresh()` and performs no calculation, cache access, synchronization, or historical reconstruction outside width normalization needed for the current render.

## Selection

After the source is deployed and NinjaScript compiles, open a chart's Data Series window and select `Orca Volume Candles` from the `Chart style` dropdown. The underlying `Type` remains the user's chosen Tick, Second, Minute, Range, Volume, or other bar type.
