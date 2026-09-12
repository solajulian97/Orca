# CVP Full-Height Hollow Delta Refinement

Date: 2026-09-12

## Objective

Refine the opt-in `Hollow Body Delta` view from body-only labels into a continuous high-to-low delta read: body rows remain boxed, wick rows show free-floating delta, adjacent wick values receive faint separators, and the center column is narrowed around a signed four-digit value.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.Rendering.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaFootprintCore.cs`
- `tests/OrcaFootprint.Tests/Program.cs`
- `tests/OrcaFootprint.PlatformCheck/Program.cs`
- `docs/indicators/ORCA_CANDLE_VOLUME_PROFILE.md`
- This handoff

This is a narrow follow-up to commit `3b69912`. Unrelated dirty files were not edited or staged.

## Behavior Added, Changed, Or Removed

- `Hollow Body Delta` now displays strict signed delta for every populated display row from the candle's high-to-low footprint.
- Open-to-close body rows keep transparent delta-colored outlines.
- Wick rows have no box and no vertical wick line; their delta labels free-float in the same center column.
- A half-pixel neutral line beneath each wick row provides a faint visual separator without crossing the number.
- Delta opacity is normalized against the largest absolute strict delta across all populated rows in that candle, rather than body rows only.
- Full center-delta formatting omits thousands separators so a signed four-digit value such as `+1250` uses five glyphs rather than six.
- The automatic width changed from `max(32, font size * 4.5 + 6)` to `max(24, font size * 3 + 4)`. At the default 8-point Bid x Ask font this reduces the column minimum from 42 to 28 pixels.
- Values that exceed the prepared Enhanced width can still use the existing Auto compact fallback.

## User-Facing Settings

- No setting, default, serialized identity or generated factory argument was added or removed.
- `Candle Display = Hollow Body Delta` retains the same enum value and dropdown label.
- Candle Width remains an optional larger minimum; Candle/Profile Gap remains outside the delta column.
- Center delta remains independent of Show Bid x Ask Text and now also bypasses Minimum Total Row Volume for Text so every populated center row is eligible to display.
- Existing Bid x Ask positive, negative and neutral colors plus Min/Max Opacity remain the visual controls.

## Secondary Series Added Or Changed

- None.
- The existing optional hidden Last Tick 1 source is unchanged.
- No Bid, Ask, Last, Second, Volumetric or custom series was added.

## Tick Replay Implications

- None to ingestion, attribution or replay capability.
- The center continues to use existing strict same-row `Ask - Bid` evidence. Unavailable strict sides display `N/A`.

## Historical-Load Implications

- No historical request, bar attribution, classifier, aggregation or warm-up behavior changed.
- More already-prepared wick-row labels are visible; no historical rebuild path was added.

## Cache Implications

- No raw evidence or shared-cache contract changed.
- Enhanced prepared rows reuse the existing optional center TextLayout slot, now for every visible populated row while this display mode is active.
- The existing 7 MB label stop and 8 MB frame cap still bound prepared layouts.

## Rendering Implications

- Normal rendering performs the existing bounded center-delta pass across the aggregated row list, now normalizing all rows and drawing every row label.
- Enhanced preparation creates center TextLayouts outside `OnRender`; warmed rendering selects cached brushes, draws body rectangles or wick separators, and draws prepared layouts.
- The Enhanced render path adds no managed allocation, cache access, aggregation, lock or synchronization wait.
- RenderTarget-owned palettes provide both delta colors and the faint neutral separator.
- POC splitting and the absence of a center wick line are unchanged.

## Performance Implications

- Normal mode may draw additional center labels for wick rows; work remains proportional to the already-rendered aggregated rows.
- Enhanced mode may retain additional center TextLayouts, subject to the existing frame budget.
- No per-tick work, data collection, secondary series or full historical scan was added.

## Tests Performed In NinjaTrader

- Pending Julian's F5 compile and chart validation after deployment.

## Static Tests Performed

- `dotnet run --project tests/OrcaFootprint.Tests -c Release --no-restore`: passed 422 checks.
- `dotnet run --project tests/OrcaFootprint.PlatformCheck -c Release --no-restore`: passed with 0 authored-source semantic errors and all platform guards/13 baseline fixtures.
- `dotnet run --project tests/OrcaFootprint.PlatformCheck -c Release --no-restore -- --live-generated`: passed with 0 live-generated semantic errors and all platform guards/13 baseline fixtures.
- New/updated checks cover ungrouped signed four-digit formatting, all-row center preparation, body-only boxes, wick separators, threshold independence and no center fill.
- Normalized authored-region parity passed for all three deployed source files.

## Compile Status

- Offline authored-source semantic compile passed.
- Offline live-generated wrapper check passed against the deployed NinjaTrader files.
- NinjaTrader F5 compile pending.

## Deployment Status

- Target-deployed from `Working_Suite` to the live NinjaTrader Indicators folder:
  - `OrcaCandleVolumeProfile.cs`
  - `OrcaCandleVolumeProfile.Rendering.cs`
  - `OrcaFootprintCore.cs`
- No `Full_Suite`, mirror or unrelated live file was changed.
- NinjaTrader was running during deployment. Copy/parity does not prove that its currently loaded Custom assembly has recompiled.

## Manual-Validation Status

- Pending.
- Confirm every populated wick and body row shows its signed center delta.
- Confirm only body rows are boxed and wick rows remain free-floating with faint horizontal separators.
- Confirm the default 8-point center minimum is approximately 28 pixels and ordinary four-digit signed values fit without broad dark margins.
- Check positive/negative opacity scaling, `N/A`, doji candles, active bars, normal and Enhanced modes, Cluster and Histogram, side-text-off mode, compression, POC overlap and template reload.

## Known Issues, Risks, And Follow-Up Work

- Very dense vertical compression can still hide text when row height is physically too small; the renderer does not draw illegible overlapping labels.
- A user-set Candle Width larger than the automatic minimum intentionally keeps the wider lane.
- The faint wick separator uses the existing neutral color at the minimum Bid x Ask palette opacity; its exact appearance therefore depends on the configured neutral color and opacity.
- The view remains strict-evidence-only; unavailable rows show `N/A` rather than inferred direction.

## Promotion Eligibility

- Not eligible for `Full_Suite` until NinjaTrader F5 passes and Julian confirms the corrected full-height center read, width, separators, active-bar behavior and performance.
