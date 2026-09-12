# CVP Hollow Body Delta

Date: 2026-09-12

## Objective

Add an opt-in Bid x Ask center-column view that replaces the filled candle with hollow open-to-close row cells containing strict row delta. Preserve an empty center through the wick region so no line crosses the delta labels.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.Rendering.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaFootprintCore.cs`
- `tests/OrcaFootprint.Tests/Program.cs`
- `tests/OrcaFootprint.PlatformCheck/Program.cs`
- `docs/indicators/ORCA_CANDLE_VOLUME_PROFILE.md`
- This handoff

The rendering partial, footprint core and isolated fixtures already contained intentional uncommitted CVP work described by `2026-09-12_cvp_continuation-context.md`. This change was layered narrowly onto that work; no pre-existing hunks were reverted or normalized.

## Behavior Added, Changed, Or Removed

- Added `Hollow Body Delta` as a fourth `Candle Display` choice after Off, OHLC Spine and Full Candle.
- Only display rows intersecting the candle's open-to-close body receive center cells.
- The high-to-open and close-to-low center regions remain empty. No wick line, open tick or close tick runs through the center labels.
- Each body cell is transparent and receives a one-pixel delta-colored outline plus centered signed strict delta (`Ask - Bid`).
- Positive, negative and unavailable/zero rows reuse the existing Bid x Ask Positive, Negative and Neutral colors.
- Opacity is normalized to the greatest absolute strict delta among that candle's body rows, using Bid x Ask Min Opacity and Max Opacity.
- Normalization excludes wick rows so an extreme wick delta cannot suppress body contrast.
- The center column automatically expands to a readable signed-number minimum. Candle Width remains a larger user-controlled minimum and Candle/Profile Gap remains outside the column.
- POC outlines split around the reserved center column.
- Normal and Enhanced Bid x Ask both implement the option.
- Existing Off, OHLC Spine and Full Candle behavior remains selected by the prior enum values.

## User-Facing Settings

- `Candle Display` now includes `Hollow Body Delta`.
- The option defaults off because the existing default remains OHLC Spine.
- No new serialized property or generated factory argument was added.
- Existing Candle Width, Candle/Profile Gap, Bid x Ask font/size, Number Format, Minimum Total Row Volume for Text, Positive/Negative/Neutral colors, and Min/Max Opacity are reused.
- The center delta does not depend on `Show Bid x Ask Text`. Users can hide the left/right Bid and Ask numbers while retaining the center quick read.

## Secondary Series Added Or Changed

- None.
- Existing primary series and optional hidden Tick 1 trade source are unchanged.
- No Bid, Ask, Last, Second, Volumetric or custom series was added.

## Tick Replay Implications

- No Tick Replay ingestion or replay-participant behavior changed.
- Center values use strict same-row Ask minus Bid evidence already retained by Bid x Ask mode.
- Unavailable strict sides display `N/A`; inferred CVP delta is not substituted.

## Historical-Load Implications

- No historical request, bar attribution, classifier, rebuild, or warm-up path changed.
- Historical accuracy remains governed by the selected existing Trade Source Mode and its documented limitations.

## Cache Implications

- No shared-cache contract or raw evidence retention changed.
- Enhanced prepared rows now retain one body-row flag and an optional precomputed center TextLayout. The frame remains under the existing presentation budgets and lifecycle.
- Enhanced viewport identity includes the relevant center mode, width, spacing, font, threshold and number-format settings so a setting change triggers fresh preparation.

## Rendering Implications

- Normal Bid x Ask suppresses its original filled body and wick only while Hollow Body Delta is active and renderable profile evidence exists.
- Normal center labels use existing cached DirectWrite formats and RenderTarget-owned Bid x Ask palettes.
- Enhanced mode prepares center TextLayouts outside `OnRender`; render only selects a cached palette brush, draws an outline and draws the prepared layout.
- No center cell is filled.
- Enhanced cluster fills and POC outlines split around the same reserved gutter used by the body column.
- Existing RenderTarget disposal/rebuild ownership is unchanged.

## Performance Implications

- Normal mode performs one additional bounded scan of the candle's already-aggregated displayed rows to find the body-only maximum delta.
- Enhanced mode calculates body membership, maximum and text layouts during its existing off-render preparation worker.
- No data access, dictionary aggregation, locks, profile construction, managed object creation or synchronization wait was added inside Enhanced `OnRender`.
- Normal mode follows the existing legacy renderer's string-label path; no new per-tick work was added.

## Tests Performed In NinjaTrader

- Pending Julian's NinjaTrader F5 compile and chart validation.

## Static Tests Performed

- `dotnet run --project tests/OrcaFootprint.Tests -c Release --no-restore`: passed 421 checks.
- New fixtures cover positive/negative signed formatting, compact formatting, zero formatting, safe magnitude, bullish and bearish body-row inclusion, wick exclusion, and doji handling.
- `dotnet run --project tests/OrcaFootprint.PlatformCheck -c Release --no-restore`: passed with 0 authored-source semantic errors, converter placement, render-allocation guard, invalid-wrapper regression and 13 baseline fixtures.
- `dotnet run --project tests/OrcaFootprint.PlatformCheck -c Release --no-restore -- --live-generated`: passed with 0 live-generated semantic errors and all platform guards/13 baseline fixtures.
- Semantic guards cover enum append order, option label, normal filled-candle suppression, open/close body bounds, hollow outlines without fills, enhanced prepared text, automatic center reservation, palette opacity and wick-line suppression.
- Authored-region parity after deployment: exact match for `OrcaCandleVolumeProfile.cs`, `OrcaCandleVolumeProfile.Rendering.cs` and `OrcaFootprintCore.cs` after newline normalization.
- Scoped `git diff --check` passed; only existing LF-to-CRLF warnings were reported.

## Compile Status

- Offline authored-source semantic compile passed.
- Offline live-generated wrapper check passed against the deployed NinjaTrader files.
- NinjaTrader F5 compile pending.

## Deployment Status

- Target-deployed from `Working_Suite` to the live NinjaTrader Indicators folder:
  - `OrcaCandleVolumeProfile.cs`
  - `OrcaCandleVolumeProfile.Rendering.cs`
  - `OrcaFootprintCore.cs`
- No `Full_Suite`, local mirror or unrelated NinjaTrader file was changed.
- NinjaTrader was running during the copy; deployment does not prove that the running assembly has recompiled or loaded the change.

## Manual-Validation Status

- Pending.
- Select Profile Display = Bid x Ask and Candle Display = Hollow Body Delta.
- Confirm body rows alone show signed deltas and the center remains empty throughout both wick regions.
- Confirm the largest absolute body-row delta reaches the strongest configured opacity and weaker values fade in both positive and negative colors.
- Confirm Candle Width can make the column wider, while the automatic minimum prevents ordinary signed values from starting too narrow.
- Toggle Show Bid x Ask Text off and confirm only the center body-delta read remains.
- Check Cluster and Histogram, normal and Enhanced modes, POC overlap, doji candles, active-bar updates, compression, and template reload.

## Known Issues, Risks, And Follow-Up Work

- The hollow view intentionally omits an explicit wick line and open/close ticks. The footprint's populated high/low rows imply the wick extent, while the outlined center cells identify the body.
- Very tight horizontal compression can still leave insufficient width for both side profiles and the auto-sized center column; Auto Hide Profiles or wider chart spacing remains the fallback.
- The center uses strict Bid/Ask delta. Rows with only unclassified evidence show `N/A` rather than a directional estimate.
- A future side-delta mode remains a possible alternative if live use shows the center column compresses Bid/Ask more than desired. It was not added in this change.

## Promotion Eligibility

- Not eligible for promotion to `Full_Suite` until NinjaTrader compiles and Julian confirms the normal and Enhanced visuals, wick readability, opacity scaling, settings behavior, template reload, active-bar updates and performance on a chart.
