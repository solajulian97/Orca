# Orca Step Profile — NinjaTrader 8 Indicator

## Indicator Name
`OrcaStepProfile`

## Overview
Build a NinjaTrader 8 indicator that renders **time-based volume profiles** at fixed intervals on a lower-timeframe chart. Each "step" covers a configurable time block (15 min, 30 min, 1 hour, or daily), and a full volume profile is rendered for each completed block — plus a live-updating profile for the current (in-progress) block.

**Example use cases:**
- 1-minute chart with 30-minute step profiles → see a new profile every half hour
- 1-minute chart with 15-minute step profiles → profiles anchored at 3:00, 3:15, 3:30, etc.
- 1-hour chart with daily step profiles → see today's, yesterday's, and previous days' profiles

Normal candlesticks remain visible — this indicator overlays profiles on top of them.

> **Reference implementations:**
> - `OrcaCandleVolumeProfile.cs` — Gradient logic, Value Area, POC highlight, SharpDX rendering, tick aggregation
> - `AutoLegProfileNT2.cs` — Dual tick compression (volume vs delta), profile anchored to right scale edge, spine positioning

---

## Visual Specification

### Historical (Completed) Profiles
Each completed time block renders a volume profile at the **start** of that block's time period:

- **Spine (zero line)** is placed at the **X position of the first bar** in that time block
- **Histogram bars extend to the right** from the spine
- **Width**: `HistoricalProfileWidthPx` (default: 100px) — fixed pixel width, normalized to the max volume within that block
- Profile is drawn from the block's high price to its low price

### Current (Active) Profile
The in-progress block renders differently:

- **Spine (zero line)** is placed on the **right side** — flush against the right edge of the chart / left of the price scale, similar to `AutoLegProfileNT2`
- **Histogram bars extend to the left** from the spine
- **Width**: `ActiveProfileWidthPx` (default: 150px)
- Updates on every tick in real-time

### Dual Profile: Volume + Delta
Each profile (both historical and active) can render two sub-profiles:

1. **Volume profile** — total volume at each price level
2. **Delta profile** — net delta (ask-hit minus bid-hit volume) at each price level, shown as green (positive) / red (negative) bars

These are independent and share the same spine, with delta drawn as a narrower profile on top of or beside the volume.

Each sub-profile has its own tick compression:
- `VolumeTickCompression` (default: 4) — groups for the volume histogram
- `DeltaTickCompression` (default: 10) — groups for the delta histogram

This matches the dual-compression pattern from `AutoLegProfileNT2`.

---

## Data Architecture

### Tick Data Series
```csharp
AddDataSeries(BarsPeriodType.Tick, 1);
```

### Time Block Management
Each time block is stored as a `StepBlock` object:
```csharp
private class StepBlock
{
    public DateTime StartTime;
    public DateTime EndTime;       // end of block (start + interval)
    public int StartBarIndex;      // primary bar index where block begins
    public int EndBarIndex;        // primary bar index where block ends (-1 if active)
    public double HighPrice;
    public double LowPrice;
    public Dictionary<double, long> VolByPrice;   // volume map (keyed by compressed price)
    public Dictionary<double, long> DeltaByPrice;  // delta map (keyed by compressed price)
}
```

Maintain a `List<StepBlock> stepBlocks` that grows as new time intervals start.

### Block Boundary Detection
On each primary bar update (`BarsInProgress == 0`):
- Determine if the current bar's time crosses a block boundary:
  - For N-minute intervals: `Time[0]` crosses the next N-minute mark from the previous bar
  - For daily: `Time[0].Date != previousDate`
- When a boundary is crossed: finalize the current block (`EndBarIndex = CurrentBar - 1`) and start a new one

### Tick Processing (`BarsInProgress == 1`)
Same pattern as `OrcaCandleVolumeProfile`:
- Determine the primary bar index via `BarsArray[0].GetBar(Time[0])`
- Find which `StepBlock` this tick belongs to (typically the last/active block)
- Round price and apply compression (separate compression for volume vs delta)
- Accumulate into `VolByPrice` and `DeltaByPrice` using bid/ask classification from `OnMarketData`

### Interval Options
Expose a `StepInterval` property as an enum:
```csharp
public enum StepIntervalType
{
    Minutes15 = 15,
    Minutes30 = 30,
    Hour1 = 60,
    Hours4 = 240,
    Daily = 1440
}
```

---

## Rendering (`OnRender`)

### Historical Profile Rendering
For each completed `StepBlock` whose bar range overlaps the visible chart area:
1. Find the X position of the block's `StartBarIndex`: `chartControl.GetXByBarIndex(ChartBars, block.StartBarIndex)`
2. This X becomes the **spine** (left edge of the profile)
3. For each price level in `VolByPrice`:
   - Calculate Y coords from `chartScale.GetYByValue(price)` and `GetYByValue(price + compHeight)`
   - Bar width = `HistoricalProfileWidthPx * (volume / maxVolInBlock)`
   - Fill rectangle from spine X extending **rightward**
4. Render delta overlay if `ShowDelta` is enabled, using delta compression and delta colors
5. Apply gradient, POC, and Value Area logic (same as `OrcaCandleVolumeProfile`)

### Active Profile Rendering
For the current (last) `StepBlock`:
1. Spine X = right edge of chart panel (or `ChartPanel.X + ChartPanel.W - RightOffsetPx`)
2. For each price level:
   - Bar width = `ActiveProfileWidthPx * (volume / maxVolInBlock)`
   - Fill rectangle from spine X extending **leftward** (i.e., `rect.X = spineX - barWidth`)
3. Same gradient/POC/VA logic applies

### POC (Point of Control)
- Highlight the price level with the highest volume in each block
- Use `POCBrush` (default: DodgerBlue)
- Expose `ShowPOC` toggle (default: `true`)

### Value Area
Reuse exact same logic from `OrcaCandleVolumeProfile`:
- Calculate VA by expanding outward from POC until `ValueAreaPercent%` of total volume is covered
- `ShowValueArea` master toggle
- `ShowVAColor` — different color for inside-VA rows (uses `VABrush`)
- `ShowVALines` — dashed/solid/dotted lines at VAH and VAL boundaries
- `VALineThickness`, `VALineStyle`, `VALineBrush` — all configurable
- Gradient applies independently inside VA (using `VABrush` as base) and outside (using `VolumeBrush`)

### Gradient
Same logic as `OrcaCandleVolumeProfile`:
- `UseGradient` toggle
- `GradientSteps` (configurable, default 16)
- `MinBrightness` (default 0.20)
- Higher volume rows = brighter, lower = darker
- Separate gradient palettes for VA and non-VA rows

### Block Separator Lines (Optional)
- Draw thin vertical lines at each block boundary to visually separate the step intervals
- `ShowBlockSeparators` toggle (default: `true`)
- `BlockSeparatorBrush` (default: DimGray with low opacity)

---

## Configurable Properties

### Data Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `StepInterval` | StepIntervalType | Minutes30 | Time interval per profile block |
| `VolumeTickCompression` | int | 4 | Tick grouping for volume histogram |
| `DeltaTickCompression` | int | 10 | Tick grouping for delta histogram |

### Layout Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `HistoricalProfileWidthPx` | int | 100 | Max width of completed profile histograms |
| `ActiveProfileWidthPx` | int | 150 | Max width of the current live profile |
| `DeltaProfileWidthPx` | int | 60 | Width of the delta sub-profile |
| `RightOffsetPx` | int | 60 | Offset from right chart edge for active profile spine |
| `ProfileBarSpacingPx` | int | 0 | Pixel gap between histogram rows |

### Visibility Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `ShowVolume` | bool | true | Show volume profile |
| `ShowDelta` | bool | true | Show delta profile overlay |
| `ShowPOC` | bool | true | Highlight Point of Control |
| `ShowBlockSeparators` | bool | true | Draw vertical lines at block boundaries |

### Gradient Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `UseGradient` | bool | true | Enable gradient coloring |
| `GradientSteps` | int | 16 | Number of gradient steps (2–64) |
| `MinBrightness` | float | 0.20 | Minimum brightness for lowest-volume rows |

### Value Area Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `ShowValueArea` | bool | true | Master toggle for Value Area |
| `ShowVAColor` | bool | true | Color inside-VA rows with `VABrush` |
| `ShowVALines` | bool | true | Draw lines at VAH/VAL boundaries |
| `ValueAreaPercent` | int | 70 | Percentage of volume the VA covers |
| `VALineThickness` | float | 1.5 | Thickness of VA boundary lines |
| `VALineStyle` | VALineStyleEnum | Dash | Style: Solid, Dash, Dot, DashDot |
| `VABrush` | Brush | CornflowerBlue | Color for inside-VA rows |
| `VALineBrush` | Brush | White | Color for VA boundary lines |

### Color Parameters
| Property | Type | Default | Description |
|---|---|---|---|
| `VolumeBrush` | Brush | RoyalBlue | Volume profile color |
| `VolumeOpacity` | float | 0.85 | Opacity for volume bars |
| `POCBrush` | Brush | DodgerBlue | POC row highlight color |
| `PositiveDeltaBrush` | Brush | Lime | Delta positive color |
| `NegativeDeltaBrush` | Brush | Red | Delta negative color |
| `DeltaOpacity` | float | 0.85 | Opacity for delta bars |
| `BlockSeparatorBrush` | Brush | DimGray | Block boundary line color |

---

## File Structure
- **File**: `NinjaTrader/Indicators/OrcaStepProfile.cs`
- **Namespace**: `NinjaTrader.NinjaScript.Indicators`
- **Class**: `OrcaStepProfile : Indicator`

## Key Implementation Notes

1. **Normal candles remain visible.** Unlike `OrcaCandleVolumeProfile`, this indicator does NOT draw its own candles — it overlays profiles on the standard NinjaTrader candlesticks.

2. **Historical profiles face right, active profile faces left.** This mirrors the `AutoLegProfileNT2` pattern: past profiles are anchored at the start of each time block with bars facing right; the active profile is anchored at the right chart edge with bars facing left toward the candles.

3. **Dual tick compression.** Volume and delta can use different compression levels (e.g., 4-tick for volume, 10-tick for delta) — same as `AutoLegProfileNT2`.

4. **Block boundary detection must handle gaps.** Market close/open creates time gaps. The block boundary logic should handle overnight gaps by checking actual bar timestamps, not just elapsed time.

5. **Performance.** Only render blocks whose bar range overlaps the visible chart area. Cull price rows outside the visible Y range. Only rebuild the active profile on each tick; historical blocks are static once closed.

6. **Real-time updates.** `ForceRefresh()` on every tick when `State == State.Realtime` to keep the active block's profile live.

7. **Do NOT include the `#region NinjaScript generated code` block.** NinjaTrader auto-generates this on compile; including it manually causes duplicate definition errors.

---

## Reference Files
- [OrcaCandleVolumeProfile.cs](file:///c:/Users/Owner/.gemini/antigravity/scratch/Orca/NinjaTrader/Indicators/OrcaCandleVolumeProfile.cs) — Gradient, Value Area, POC, SharpDX rendering, tick aggregation
- [AutoLegProfileNT2.cs](file:///c:/Users/Owner/.gemini/antigravity/scratch/Orca/NinjaTrader/Indicators/AutoLegProfileNT2.cs) — Dual compression, right-edge profile anchoring, spine direction, past/current profile pattern
- [LegToLegDeltaProfile.cs](file:///c:/Users/Owner/.gemini/antigravity/scratch/Orca/NinjaTrader/Indicators/LegToLegDeltaProfile.cs) — Per-bar volume/delta maps, delta classification, session rebuilds
