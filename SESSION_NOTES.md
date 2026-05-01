# Session Notes

## OrcaVisibleRangeVolumeProfile

- Added `Orca Trades/Working_Suite/Indicators/OrcaVisibleRangeVolumeProfile.cs`.
- Added `Orca Trades/Working_Suite/Indicators/OrcaVolumeProfileCore.cs` as a shared calculation helper for visible-range row aggregation, POC detection, dictionary price-bin value-area calculation, and row-based value-area expansion.
- The helper follows the Orca profile value-area convention found in `OrcaRollingProfiles`, `OrcaLegtoLegProfile`, `OrcaCandleVolumeProfile`, and `OrcaStepProfile`: start from POC and expand toward the larger next-volume row until the configured VA percent is covered.
- Existing Orca profile source stores traded volume in price buckets from tick/price updates. `OrcaVisibleRangeVolumeProfile` now defaults to the same true traded volume-at-price approach by adding a 1-tick secondary series, storing per-primary-bar price maps, and summing only the maps for the bars currently visible on the chart. The previous OHLC high-low volume estimate remains available through `ProfileDataMode = EstimatedFromBars`.
- The profile cache is invalidated by visible `ChartBars.FromIndex`/`ChartBars.ToIndex`, bar count, row count, VA percent, tick size, new primary bars, and available `ChartControl.PropertyChanged` bar/canvas notifications. `OnRender` remains the final guard because it is the reliable place where the current viewport indices are available.
- Deployed `OrcaVolumeProfileCore.cs` and `OrcaVisibleRangeVolumeProfile.cs` to the live NinjaTrader Custom indicators folder with `deploy_orca.ps1`; NinjaTrader still needs F5 compile validation.
- Removed the hand-written NinjaScript generated-cache section from `OrcaVisibleRangeVolumeProfile.cs` after NT8 appended its own generated section and created duplicate cache/MarketAnalyzer/Strategy methods.
- Added visible-range profile controls matching the rest of the Orca profile family: profile bar spacing, value-area toggles, VA line style/thickness, gradient palettes/steps/min brightness, and row sizing by fixed row count, fixed ticks per row, or dynamic aggregation based on chart scale.
- Dynamic aggregation for this profile resolves to a tick row size at render time from the visible price scale, panel height, `DynamicRowMinPixels`, and `DynamicAggregationMultiplier`, then caches by the resolved tick size to avoid unnecessary recalculation.
- Removed the separate `UseDynamicAggregation` toggle from `OrcaVisibleRangeVolumeProfile`; `RowSizingMode` is now the single source of truth, so `TicksPerRow = 1` means one instrument tick per rendered row whenever `RowSizingMode = TicksPerRow`.
- True volume mode can require more historical tick data during initial chart load, but render-time recalculation stays scoped to visible bars and cached per data revision/viewport.
- The indicator now runs `Calculate.OnEachTick` so same-price prints are captured in true volume mode instead of being skipped by price-change-only updates.
- Added a visible-range delta profile using the same row aggregation as volume. `ShowVolume`, `ShowDelta`, `DeltaWidthPercent`, and `DeltaDirection` control whether volume/delta are shown and whether delta faces the profile's outer/price-scale side or the candle-facing side, matching the Rolling Profiles interaction pattern.
- Split delta into its own aggregation path. Delta now has independent `DeltaRowSizingMode`, `DeltaRowCount`, `DeltaTicksPerRow`, `DeltaDynamicAggregationMultiplier`, and `DeltaDynamicRowMinPixels` settings, so volume can stay one tick while delta can be compressed or dynamically aggregated for readability.
- Added independent delta colors through `DeltaPositiveColor` and `DeltaNegativeColor`; defaults are SteelBlue for positive delta and IndianRed for negative delta.
- Fixed the Tick Replay delta classifier in `OrcaVisibleRangeVolumeProfile`: `MarketDataType.Last` now refreshes bid/ask from `e.Ask`/`e.Bid`, and same-price prints now carry the last uptick/downtick direction instead of being forced positive. This matches the suite pattern used by Orca cumulative delta and absorption tools.
- Added delta value labels inside the visible-range delta profile. `ShowDeltaLabels`, `DeltaLabelFontSize`, `DeltaPositiveLabelColor`, and `DeltaNegativeLabelColor` control whether labels render, their font size, and separate positive/negative label colors. Defaults are LightGreen and LightCoral so the text stays visible while blending with the profile.
- `OrcaRollingProfiles.cs` and `OrcaLegtoLegProfile.cs` both had pre-existing uncommitted edits before this task. I did not modify those active files. Follow-up refactor opportunity: once those edits are validated, migrate their duplicate inlined value-area functions to `OrcaVolumeProfileCore`.

## Test Plan

- Deploy `OrcaVisibleRangeVolumeProfile` and `OrcaVolumeProfileCore` from `Working_Suite`.
- Press F5 in the NinjaScript Editor and verify the full NT8 compile has no errors or warnings.
- Add the indicator to minute and tick charts; pan and zoom to confirm the histogram, POC, VAH, VAL, and total volume recompute for only the visible bars.
- In default `TrueVolumeAtPrice` mode, verify that one-tick row sizing produces one price tick per row and that historical areas populate when NinjaTrader has historical tick data available.
- Toggle `ShowDelta`, `ShowVolume`, and `DeltaDirection` on both left- and right-side profile placement to confirm volume and delta switch sides cleanly without overlapping.
- Change `DeltaRowSizingMode` between `TicksPerRow` and `Dynamic` while leaving volume at one tick, then confirm only the delta histogram row height changes.
- Change `DeltaPositiveColor` and `DeltaNegativeColor` in the indicator dialog and confirm the delta histogram updates without changing volume profile colors.
- With Tick Replay enabled, confirm positive and negative delta rows both appear on sell/buy sequences, especially across repeated same-price prints.
- Toggle `ShowDeltaLabels`, adjust `DeltaLabelFontSize`, and change positive/negative label colors to confirm labels appear only on rows with enough height/width and do not overlap.
- Toggle every display/color property in the indicator dialog.
- Run the standard 6-chart MNQ/NQ/ES/MES layout and confirm no visible lag while panning and zooming.
