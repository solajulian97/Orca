# 2026-08-30 - Orca Fixed Range Profile Delta Labels At Spine

## Objective

Keep Fixed Range delta numbers aligned to the delta histogram spine instead of placing them at each row's variable-width bar endpoint.

## Files changed

- `Orca Trades/Working_Suite/DrawingTools/OrcaFixedRangeProfile.cs`
- `docs/handoffs/2026-08-30_orca-fixed-range-profile_delta-label-spine.md`

## Behavior added, changed, or removed

- Left-facing delta rows now place labels in a fixed-width text rectangle at the left edge of the delta track.
- Right-facing delta rows now place labels in a fixed-width text rectangle at the right edge of the delta track.
- Delta label text now uses leading alignment, so two-, three-, and four-digit values share the same left edge within that spine column.
- Delta row geometry, normalization, colors, spacing, and profile calculations are unchanged.

## User-facing settings

None. `Show Delta Labels` and the existing delta font settings continue to control the labels.

## Secondary series

None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom series changed.

## Tick Replay implications

None. The change is render-only.

## Historical-load implications

None. No historical calculation or cache path changed.

## Cache implications

None. No profile or data cache changed.

## Rendering implications

- Delta labels are now anchored to the fixed track edge, which is the visual spine for the selected orientation.
- DirectWrite leading alignment keeps every delta value left-aligned within that anchored column regardless of label length.
- The existing minimum bar-width guard remains in place so labels are not forced into rows that cannot contain them.

## Performance implications

- No new allocations, brushes, text formats, data scans, or render passes were added.
- The label rectangle is calculated from the existing row width and cached font metrics.

## Tests performed in NinjaTrader

- Targeted deployment completed to `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\DrawingTools\OrcaFixedRangeProfile.cs`.
- Source and deployed file authored content matched after newline normalization.
- NinjaTrader F5 compile and chart validation remain pending.

## Compile status

- `git diff --check` passed for the focused change.
- NinjaTrader F5 compile: pending.
- NinjaTrader F5 compile: pending.

## Manual-validation status

Pending Julian validation with:

- Delta on the left, facing right.
- Delta on the right, facing left.
- Both Fixed Range profile arrangement options.
- Labels enabled and disabled at several zoom levels.

## Known issues, risks, and follow-up work

- Very narrow rows still suppress labels under the existing width guard.
- DirectWrite text alignment remains trailing, but the fixed-width spine-anchored rectangles keep the visible text column stable in both orientations.

## Full_Suite promotion eligibility

Not eligible until NinjaTrader F5 compile and Julian's manual chart validation pass.
