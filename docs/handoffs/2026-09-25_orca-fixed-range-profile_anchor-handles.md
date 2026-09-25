# 2026-09-25 - Orca Fixed Range Profile Anchor Handles

## Objective

Make the fixed-range drawing easier to place and read: visible drag circles on the line ends, a stats-line `est` tag when volume is the bar-range estimate, a draggable statistics box whose offset survives reload, and bar snapping while a handle is dragged.

## Files changed

- `Orca Trades/Working_Suite/DrawingTools/OrcaFixedRangeProfile.cs`
- `docs/handoffs/2026-09-25_orca-fixed-range-profile_anchor-handles.md`

## Behavior added, changed, or removed

- A circle is drawn at `StartAnchor` and `EndAnchor` on every render, including while the drawing is being placed.
- Resting size is a 7px radius. Light fill (`0.93, 0.95, 0.97` at 82% opacity) with a dark edge. When the drawing is selected the radius is 9px, the fill is solid white, and the edge is slightly thicker.
- The hit radius is 14px. A press inside that radius drags that anchor. If both circles overlap, the nearer one wins.
- While a handle is dragged, and while the range is first placed, that anchor's time snaps to the bar under the cursor (`ChartBars.GetBarIdxByX`, then `Bars.GetTime` and `ChartControl.GetSlotIndexByTime`). Price stays at the cursor. Corner resize and body drag are not snapped.
- Dragging the box body still moves both anchors together. Corner resize of the rectangle is unchanged outside the handle radius.
- When volume came from `BuildFixedRangeFromBars` (each bar spread from low to high), the statistics line appends `est`. True volume does not show `est`, including when some prices have no ask/bid split.
- The statistics box still defaults outside the range: Top Left and Top Right sit 6px above, Bottom Left and Bottom Right sit 6px below. A stored pixel offset is applied after that placement and is not clamped back over the profile.
- Dragging the statistics box changes only `StatisticsOffsetX` and `StatisticsOffsetY`. It does not move the range anchors and does not rebuild the profile. Hit testing includes the box so it can be grabbed when it sits off the candles.
- Volume, delta, and the bar-direction (`close >= open`) path were not changed.

## User-facing settings added, changed, deprecated, or removed

- `StatisticsOffsetX` and `StatisticsOffsetY` are stored on the drawing (default `0, 0`). They are `[Browsable(false)]` and `[NinjaScriptProperty]` so a workspace reload keeps the dragged position and they stay out of the property grid.
- No new property-grid setting. `est` is a label token, not a switch.

## Secondary series added or changed, including Tick, Second, Bid, Ask, Last, Volumetric, or custom series

None.

## Tick Replay implications

None. Handle drag, the estimate tag, the statistics offset, and bar snap do not read or publish Tick Replay data.

## Historical-load implications

None. Bar snap uses the bars already attached to the chart. It does not request history.

## Cache implications

None.

## Rendering implications

- Handles are drawn after the profile and statistics so they stay on top of the box.
- Hit-test rendering fills a 14px circle at each anchor and the last statistics rectangle, in addition to the existing box mask, so a click on a circle or on the label outside the range still reaches the tool.
- Three extra solid brushes are created with the other Direct2D resources and disposed with them.
- The statistics rectangle is the default outside-range placement plus the saved pixel offset. The offset is not re-clamped into the panel.

## Performance implications

- Two ellipse draws per frame, plus the existing statistics text layout.
- Handle and initial-placement drags still mark the profile dirty because the anchors moved.
- Statistics drags update the offset and invalidate the chart. They do not mark the profile dirty.

## Tests performed in NinjaTrader

None. This workspace cannot see Julian's live NinjaTrader folder. Not deployed.

## Compile status

Not compiled in NinjaTrader here. `git diff --check` was run on the edited source. Brace depth of the drawing tool is balanced.

## Manual-validation status

Pending Julian. Not validated.

Suggested check on NQ DEC26 200 Volume:

- Start and end circles are visible on the black chart, grow slightly when the drawing is selected, and dragging one circle moves only that end. The anchor time lands on a bar. Price follows the cursor.
- Dragging the box body still moves the whole range.
- With true volume, the stats line has no `est`. With the bar-range estimate, the line ends in `est`.
- Dragging the stats box moves only the label. Reload the workspace and the label stays where it was dropped. A new drawing still starts 6px outside the range.

## Known issues, risks, and follow-up work

- NinjaTrader's own selection squares can still appear on the rectangle corners when the drawing is selected. The circles are the line-end handles.
- Corner resize and body drag do not snap time to bars. Only handle drags and the initial click-drag placement do.
- `StatisticsOffsetX` / `StatisticsOffsetY` persist only if NinjaTrader saves `[NinjaScriptProperty]` values on this drawing. That has not been confirmed in a live workspace reload.
- Not eligible for `Full_Suite` until Julian compiles and confirms the chart.

## Full_Suite promotion eligibility

Not eligible. `Full_Suite` was not modified.
