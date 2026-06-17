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
- Added matching positive/negative delta text colors to `OrcaRollingProfiles.cs`, `OrcaLegtoLegProfile.cs`, and `OrcaStepProfile.cs`. Existing single text-color properties now act as the positive label color; new negative label color properties default to LightCoral. New positive defaults are LightGreen, matching the clean visible-range label style.
- The previous note about pre-existing uncommitted `OrcaRollingProfiles.cs` and `OrcaLegtoLegProfile.cs` edits is no longer current; `git status` was clean at the start of this pass.
- Standardized dynamic delta aggregation across Rolling, Leg-to-Leg, Step, and Visible Range profiles. Rolling, Leg-to-Leg, and Step now expose `Delta Dynamic Row Min Pixels` and use `ticksPerPixel * DeltaDynamicRowMinPixels * DynamicAggregationMultiplier`, instead of deriving row height from delta label font size. Defaults are 10 pixels to match visible range delta.

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
- On Rolling, Leg-to-Leg, and Step profiles, enable delta text and confirm positive delta labels use the positive text/label color while negative delta labels use the negative text/label color.
- On Rolling, Leg-to-Leg, Step, and Visible Range profiles, set dynamic multiplier and delta dynamic row-min-pixels to matching values and confirm delta row heights visually align at the same chart zoom.
- Visible-range delta labels now use trailing text alignment inside the actual delta bar rectangle, so labels line up on the right edge instead of relying on estimated text widths.
- Protected visible-range true volume-at-price maps with a shared lock while tick updates append data, then snapshot the visible maps before render-time recalculation. Profile rebuilds now enumerate immutable snapshots instead of live dictionaries, fixing live `OnRender` collection-modified errors across multi-chart layouts.
- Toggle every display/color property in the indicator dialog.
- Run the standard 6-chart MNQ/NQ/ES/MES layout and confirm no visible lag while panning and zooming.

## OrcaMGIDaily

- Changed the default prior RTH labels from the old `PRTH*` family to the clearer `RTH PDH`, `RTH PDL`, `RTH PDC`, `RTH PDO`, and `RTH PDM` acronyms, with legacy label normalization so old templates migrate to the new defaults.
- Added True Daily Open (`TDO`) as a current-day level. It captures the `Open[0]` of the first bar that crosses midnight Eastern and exposes show/color/label properties.
- Added Daily Open as an independent current-day level using the 18:00 Globex/ETH open. The default label is `Daily Open`.
- Split open-boundary detection from close-boundary detection. RTH Open, Daily Open, and TDO now seed from the first bar after the configured open boundary so minute bars use the actual opening bar `Open[0]` instead of the prior bar that closed at the boundary.
- Fixed ETH session bookkeeping so the 18:00 ETH open is assigned to the next RTH trading date. This keeps ETH-anchored rendering, including ETH Mid, connected after RTH begins.
- Tightened overnight tracking to only aggregate from ETH open through RTH open. The overnight range/value area now freezes during RTH instead of resetting at RTH open or including the post-RTH maintenance/post-close stretch.
- Routed dynamic mid labels through the same staggered label pass as normal levels so ON Mid and ETH Mid no longer overlap when they resolve to the same price.

## MGI Daily Test Plan

- Deploy `OrcaMGIDaily` from `Working_Suite`, press F5 in NinjaTrader, and confirm the indicator compiles cleanly.
- On minute and tick charts, verify `RTH PDC` appears where `PRTHC` used to appear and old templates migrate correctly.
- Verify `RTH PDO` matches the actual 09:30 RTH opening bar open on minute charts instead of the 09:29-09:30 closing bar.
- Load a chart across midnight and confirm `TDO` matches the opening print of the midnight candle.
- Confirm `Daily Open` appears at the 18:00 Globex open and can be toggled independently from current-day high/low.
- Verify ONH/ONL/ONM/ONVAH/ONVAL/ONPOC build only between ETH open and RTH open, then remain fixed after RTH begins.
- Confirm ETH Mid continues plotting after RTH open, reflects the full current day including overnight and RTH action, and staggers its label when overlapping ON Mid.
- Current RTH Open now renders as its own current-session level (`RTH Open`) when `Show Current RTH` is enabled. Legacy `RTHO` labels migrate to `RTH Open`, keeping `RTH PDO` reserved for the prior RTH open.

## Orca DPI-Safe Chart Coordinates

- Updated OrcaPrints hover tracking to store mouse positions in chart-control coordinates, matching the SharpDX render coordinates used for print dots.
- Updated OrcaVisualOrders drag/hover price conversion to use chart-control mouse coordinates instead of chart-panel-relative coordinates.
- Updated OrcaRiskManager drag overlays so temporary drag canvases are aligned to the chart control's grid slot, and drag price conversion now reads mouse coordinates from the aligned overlay canvas. This keeps preview lines, click prices, and chart-scale conversion in the same coordinate space on high-DPI laptop/external-monitor setups.

## DPI Coordinate Test Plan

- Deploy `OrcaPrints.Rendering.cs`, `OrcaVisualOrders`, and `OrcaRiskManagerAddOn` from `Working_Suite`, press F5, and restart NinjaTrader if the AddOn panel was already loaded.
- On the laptop display at native resolution, verify OrcaPrint hover tooltips trigger directly over dots.
- In SIM, use Orca Risk Manager drag order placement and confirm the preview line price matches the submitted limit/stop price.
- In SIM, drag visual TP/SL buttons and routed order overlays and confirm submitted/changed prices match the chart line.
