# Orca Per-Candle Volume Profile — NinjaTrader 8 Indicator

## Indicator Name
`OrcaCandleVolumeProfile`

## Overview
Build a NinjaTrader 8 indicator that functions as a **custom footprint chart**, where the indicator draws its own candles AND volume profiles as a single visual unit. For each primary-series bar, the indicator renders:

1. **A candlestick** (body + upper/lower wick) — green for bullish, red for bearish
2. **A volume profile histogram** to the right of the candle body, showing volume distribution across price levels

The indicator controls the entire layout — candle width, profile width, and spacing between candle+profile pairs — so the visual is always clean and proportional regardless of NinjaTrader's native chart settings.

> **Important**: The user should set NinjaTrader's chart type to a hidden/invisible style (e.g., transparent or "line on close" with transparent color) so the native candles don't render on top of the custom-drawn ones.

> **Reference implementation**: Reuse the same tick-level volume aggregation and SharpDX rendering patterns from
> `AutoLegProfileNT2.cs` (now renamed **Orca Leg-to-Leg Profiles**) and `LegToLegDeltaProfile.cs`.
> Specifically, the tick data series (`AddDataSeries(BarsPeriodType.Tick, 1)`), bid/ask delta classification via `OnMarketData`, and the `Dictionary<double, long>` per-price-level volume accumulation pattern should be carried over.

---

## Visual Specification (match the reference screenshot)

### Custom Candle Rendering
The indicator draws its **own candles** using SharpDX — it does NOT rely on NinjaTrader's native candlestick rendering. For each visible bar:

- **Body**: A filled rectangle between `Open` and `Close`.
  - **Bullish** (Close ≥ Open): filled with `BullishBodyBrush` (default: green/teal, matching the screenshot).
  - **Bearish** (Close < Open): filled with `BearishBodyBrush` (default: red).
  - Body width: `CandleWidthPx` (default: 14px) — a fixed pixel width so candles look consistent.
- **Wicks**: Thin vertical lines from the body top to `High`, and body bottom to `Low`.
  - Wick color matches the body color (bullish = green wick, bearish = red wick).
  - Wick thickness: 1–2px.
- **X positioning**: Each candle+profile unit is positioned at the X coordinate of the primary bar (`chartControl.GetXByBarIndex`). The candle body is drawn centered on this X, and the volume profile extends to the right from the candle body's right edge.

### Volume Profile Histogram (per candle)
- For each primary-series bar, a **horizontal bar chart** (volume profile) is rendered immediately to the **right** of the candle body.
- Each row of the histogram corresponds to a price level (or compressed group of price levels).
- Bar width is proportional to the volume at that price level relative to the **max volume within that candle**.
- The histogram uses a single color (default: **blue**, matching the reference screenshot) with configurable opacity.
- Profile left edge = candle body right edge + small gap (2px).
- Profile max width: `ProfileWidthPx` (default: 80px).

### POC (Point of Control) Highlight
- The price level with the highest volume within each candle should be rendered with a **slightly brighter or highlighted shade** of the profile color (e.g., a brighter blue or a distinct POC color) to mark the Point of Control.
- Expose a `ShowPOC` toggle (default: `true`) and a `POCBrush` color property.

### Spacing & Layout
- Each candle+profile unit occupies a total horizontal footprint of: `CandleWidthPx + GapPx + ProfileWidthPx`.
- NinjaTrader's bar spacing determines the X positions — the indicator just renders at those positions. The user adjusts NinjaTrader's bar width/spacing so the units don't overlap. This is natural since the indicator IS the chart visualization.
- The candle body is centered at the bar's X position; the profile extends rightward from there.

---

## Data Architecture

### Tick Data Series
- Add a 1-tick data series in `OnStateChange` → `State.Configure`:
  ```csharp
  AddDataSeries(BarsPeriodType.Tick, 1);
  ```

### Per-Bar Volume Map
- Maintain a `List<Dictionary<double, long>> barVolumeMaps` — one dictionary per primary bar index.
- Each dictionary maps `price → total volume` at that price level during that bar.

### Tick Processing (`OnBarUpdate`, `BarsInProgress == 1`)
- On each tick, determine the primary bar index via `BarsArray[0].GetBar(Time[0])`.
- Round the tick price to the instrument's tick size: `Instrument.MasterInstrument.RoundToTickSize(price)`.
- Apply **tick compression** (group N ticks into one bucket):
  ```
  double comp = TickCompression * TickSize;
  double bucketPrice = Math.Floor(price / comp + 0.000001) * comp;
  ```
- Accumulate volume into `barVolumeMaps[primaryBarIndex][bucketPrice]`.

### Optional Delta (future extension)
- Also maintain `barDeltaMaps` using the same bid/ask classification from `OnMarketData` as in the reference indicators. This allows a future toggle to color the profile by delta instead of pure volume.
- For now, delta rendering is **optional** — expose a `ShowDelta` toggle (default: `false`). When enabled, instead of a single blue color, each histogram row is colored green (positive delta) or red (negative delta).

---

## Rendering (`OnRender`)

### Candle Drawing
- For each visible bar (`ChartBars.FromIndex` to `ChartBars.ToIndex`):
  1. Get bar center X via `chartControl.GetXByBarIndex(ChartBars, barIndex)`.
  2. Get OHLC values: `BarsArray[0].GetOpen(barIndex)`, `GetClose`, `GetHigh`, `GetLow`.
  3. Convert to Y coordinates via `chartScale.GetYByValue(price)`.
  4. Draw **wick**: vertical line from High Y to Low Y, centered on bar X, 1-2px wide.
  5. Draw **body**: filled rectangle from Open Y to Close Y, width = `CandleWidthPx`, centered on bar X.
  6. Color based on bullish/bearish direction.

### Volume Profile Drawing
- Immediately after drawing each candle:
  1. Retrieve the bar's volume map from `barVolumeMaps[barIndex]`.
  2. Find `maxVol` for that bar.
  3. Calculate profile left edge: `barCenterX + (CandleWidthPx / 2) + GapPx`.
  4. For each price level in the map:
     - Calculate `yTop` and `yBot` from `chartScale.GetYByValue(price)` and `chartScale.GetYByValue(price - compressionHeight)`.
     - Calculate bar width: `ProfileWidthPx * (volume / maxVol)`.
     - Fill rectangle from profile left edge, extending rightward.
  5. If `ShowPOC`, render the POC row with `pocBrushDx` instead.

### Resource Management
- Use `SharpDX.Direct2D1.SolidColorBrush` for rendering (created in `EnsureDxResources()`).
- Properly dispose all DX resources in `OnRenderTargetChanged()` and `State.Terminated`.
- Follow the same pattern as `AutoLegProfileNT2.cs`:
  ```csharp
  private SolidColorBrush volBrushDx, pocBrushDx, bullBodyBrushDx, bearBodyBrushDx, bullWickBrushDx, bearWickBrushDx, posDeltaBrushDx, negDeltaBrushDx;
  ```

### Performance
- Only render for **visible bars** (`ChartBars.FromIndex` / `ChartBars.ToIndex`).
- Cull price rows outside the visible Y range (`ChartPanel.Y` to `ChartPanel.Y + ChartPanel.H`).
- Force refresh on every tick for real-time updates:
  ```csharp
  if (State == State.Realtime)
      ForceRefresh();
  ```

---

## Configurable Properties

### Data Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `TickCompression` | int | 4 | Number of ticks to group into each price bucket |

### Layout Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `CandleWidthPx` | int | 14 | Width of the custom-drawn candle body in pixels |
| `ProfileWidthPx` | int | 80 | Max width of the volume profile histogram in pixels |
| `CandleProfileGapPx` | int | 2 | Pixel gap between candle body right edge and profile left edge |
| `ProfileBarSpacingPx` | int | 0 | Pixel gap between histogram rows |

### Visibility Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `ShowPOC` | bool | true | Highlight the Point of Control row |
| `ShowDelta` | bool | false | Color rows by delta instead of solid volume color |

### Color Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `BullishBodyBrush` | Brush | MediumSeaGreen | Bullish candle body/wick color |
| `BearishBodyBrush` | Brush | Crimson | Bearish candle body/wick color |
| `VolumeBrush` | Brush | RoyalBlue | Color for volume profile bars |
| `VolumeOpacity` | float | 0.60 | Opacity of volume bars |
| `POCBrush` | Brush | DodgerBlue | Color for the POC row |
| `PositiveDeltaBrush` | Brush | Lime | Delta positive color (when ShowDelta is on) |
| `NegativeDeltaBrush` | Brush | Red | Delta negative color (when ShowDelta is on) |

---

## File Structure
- **File**: `NinjaTrader/Indicators/OrcaCandleVolumeProfile.cs`
- **Namespace**: `NinjaTrader.NinjaScript.Indicators`
- **Class**: `OrcaCandleVolumeProfile : Indicator`

## Key Implementation Notes

1. **The indicator draws its own candles.** It is a custom footprint-style chart. The user should hide NinjaTrader's native candlestick rendering (set chart type to transparent or "Line on Close" with transparent brush) so only the indicator's candles and profiles are visible.

2. **Histogram direction**: Profile bars extend **rightward** from the candle body's right edge (matching the reference screenshot).

3. **Compression**: Tick compression groups multiple price levels into one histogram row, crucial at lower zoom levels.

4. **Real-time updates**: The current (in-progress) bar's candle and profile should update on every tick in real-time mode.

5. **The `#region NinjaScript generated code` block** at the bottom of the file should follow the same pattern as `AutoLegProfileNT2.cs` — NinjaTrader auto-generates this when the indicator is compiled. Include a stub matching the property signature.

---

## Reference Files
- [AutoLegProfileNT2.cs](file:///c:/Users/Owner/.gemini/antigravity/scratch/Orca/NinjaTrader/Indicators/AutoLegProfileNT2.cs) — Volume/delta aggregation logic, SharpDX rendering, tick data series pattern
- [LegToLegDeltaProfile.cs](file:///c:/Users/Owner/.gemini/antigravity/scratch/Orca/NinjaTrader/Indicators/LegToLegDeltaProfile.cs) — Per-bar volume maps, session/leg rebuilds, delta classification
