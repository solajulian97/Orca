# CVP Dynamic Center Width And Single Body Outline

Date: 2026-09-12

## Objective

Reduce unused horizontal space in `Hollow Body Delta` and replace the stack of body-row boxes with one visually coherent hollow candle-body outline.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.Rendering.cs`
- `tests/OrcaFootprint.PlatformCheck/Program.cs`
- `docs/indicators/ORCA_CANDLE_VOLUME_PROFILE.md`
- This handoff

The change continues commits `3b69912` and `1a239e2`. Unrelated dirty workspace files were preserved.

## Behavior Added, Changed, Or Removed

- Each candle now measures the widest center-delta label it will display.
- Center width is the greater of Candle Width and measured text width plus four total pixels of horizontal padding.
- A small-value candle no longer reserves the previous fixed four-digit minimum; an exceptional larger value expands only its own candle.
- Auto number format compacts absolute deltas of 10,000 or more before width measurement. Full and Compact continue to honor their explicit selections.
- The stack of delta-colored body-row rectangles was removed.
- One continuous hollow rectangle now encloses all displayed rows intersecting open-to-close.
- The continuous body outline uses the active configured bullish or bearish candle-body brush, separating candle direction from per-row delta direction.
- Faint neutral separators are drawn beneath every center row, both body and wick.
- Center text remains colored and opacity-scaled independently by strict row delta.

## User-Facing Settings

- No setting, enum value, default, serialization identity or generated factory argument changed.
- `Candle Display = Hollow Body Delta` remains the opt-in choice.
- Candle Width is now a true per-candle minimum rather than being forced up to a fixed 28-pixel center width.
- Candle/Profile Gap remains outside the measured column.
- Bullish Body Color and Bearish Body Color control the single body outline.
- Bid x Ask Positive/Negative/Neutral colors and Min/Max Opacity continue to control center-delta labels.

## Secondary Series Added Or Changed

- None.
- No Tick, Second, Bid, Ask, Last, Volumetric or custom series changed.

## Tick Replay Implications

- None.
- Strict `Ask - Bid` evidence, classification and replay capability are unchanged.

## Historical-Load Implications

- None.
- No request, attribution, aggregation, rebuild or warm-up path changed.

## Cache Implications

- No market-data or shared-cache contract changed.
- Normal rendering reuses its existing text-width cache, keyed by center label and font size.
- Enhanced preparation measures text outside `OnRender`, reuses widths for repeated label/font combinations within the frame, stores one resolved CenterWidth per prepared candle, and remains within the existing frame budgets.

## Rendering Implications

- Normal Bid x Ask measures cached DirectWrite label widths while processing its already-aggregated row list, reserves the resulting per-candle center, draws row separators/text, then draws one body outline.
- Enhanced mode performs label measurement and TextLayout preparation off-render. `OnRender` consumes the stored width and prepared layouts, draws separators/text, and draws one body outline after the row loop.
- The Enhanced render path contains no managed allocation, data/cache access, aggregation, lock or synchronization wait.
- POC outlines use each candle's resolved center gutter.
- No vertical wick line is introduced.

## Performance Implications

- Normal mode can create a width measurement only for a previously unseen label/font-size pair; subsequent frames reuse the existing cache.
- Enhanced preparation creates bounded temporary measurement layouts outside `OnRender`, caching repeated label/font measurements for the preparation pass; final display layouts remain frame-owned and budgeted.
- Per-candle width adds one float to each Enhanced prepared bar.
- No per-tick work, series, market-data processing or historical scan was added.

## Tests Performed In NinjaTrader

- Targeted deployment completed; NinjaTrader F5 and Julian's chart validation remain pending.

## Static Tests Performed

- `dotnet run --project tests/OrcaFootprint.Tests -c Release --no-restore`: passed 422 checks; 200,000 observations in 45 ms, warm preparation p50 0.076 ms and p95 0.138 ms.
- `dotnet run --project tests/OrcaFootprint.PlatformCheck -c Release --no-restore`: passed with 0 authored-source semantic errors and all platform guards/13 baseline fixtures.
- `dotnet run --project tests/OrcaFootprint.PlatformCheck -c Release --no-restore -- --live-generated`: passed with 0 semantic errors against the deployed live source including generated code and all platform guards/13 baseline fixtures.
- Platform guards require measured per-candle width in both renderers, prohibit per-row center rectangles, require one continuous body outline, preserve separators, and retain the Enhanced no-allocation/no-lock render contract.
- `git diff --check` passed for the scoped change; only line-ending conversion warnings were emitted.

## Compile Status

- Offline authored-source semantic compile passed.
- NinjaTrader F5 compile pending.

## Deployment Status

- Deployed only `OrcaCandleVolumeProfile.cs` and `OrcaCandleVolumeProfile.Rendering.cs` from `Working_Suite` to the live NinjaTrader Custom Indicators folder.
- Normalized authored-source parity passed for both files. The rendering partial is also an exact normalized match; the main file differs only in NinjaTrader's generated region.
- `Full_Suite` was not touched.

## Manual-Validation Status

- Pending.
- Confirm small deltas produce visibly narrower center lanes and four-digit deltas expand only the candle that contains them.
- Confirm one bullish/bearish rectangle encloses the full candle body with no individual row boxes.
- Confirm faint separators and delta-colored text continue through body and wick rows.
- Check doji bodies, active-bar width changes, normal and Enhanced modes, Cluster and Histogram, POC splitting, chart compression, pan/zoom and template reload.

## Known Issues, Risks, And Follow-Up Work

- Per-candle width can change when a developing delta crosses a text-width boundary; only that active candle's side roots should move.
- A manually large Candle Width intentionally overrides the tighter measured result.
- Very compressed rows can still suppress text when vertical height is insufficient.
- Native DirectWrite memory and live render cost require NinjaTrader observation; offline checks do not prove visual or runtime behavior.

## Promotion Eligibility

- Not eligible for `Full_Suite` until NinjaTrader F5 passes and Julian confirms the new spacing, single body outline, active-bar stability and performance.
