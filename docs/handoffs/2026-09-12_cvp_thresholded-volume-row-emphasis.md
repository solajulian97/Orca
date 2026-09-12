# CVP Thresholded Volume Row Emphasis

Date: 2026-09-12

## Objective

Change the existing Volume-only delta emphasis so qualifying profile bars, rather than their volume labels, use the configured positive or negative delta color. Keep volume labels in the selected Volume Text Color.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`
- `tests/OrcaFootprint.PlatformCheck/Program.cs`
- `docs/indicators/ORCA_CANDLE_VOLUME_PROFILE.md`
- This handoff

The intentionally dirty `OrcaCandleVolumeProfile.Rendering.cs`, `OrcaFootprintCore.cs`, and isolated core fixtures were preserved. No unrelated CVP or repository changes were reverted or normalized.

## Behavior Added, Changed, Or Removed

- The existing thresholded Volume-only emphasis now colors a qualifying profile bar with Positive Delta or Negative Delta rather than coloring its volume number.
- Qualification is unchanged: the aggregated display row must meet both Minimum Absolute Delta and Minimum Delta %.
- The volume number keeps Volume Text Color and the selected ordinary font weight.
- The POC fill keeps its existing priority when the POC row also qualifies.
- `Color Volume By Delta` remains the separate setting that colors every volume row by delta.
- Qualified-row hover keeps the existing compact strict Bid x Ask and strict-delta evidence.

## User-Facing Settings

- The existing serialized `ColorVolumeTextByDelta` property is retained and relabeled `Emphasize Volume Rows by Delta`; saved templates keep their existing toggle value.
- Minimum Absolute Delta and Minimum Delta % retain their identities, defaults of 150 and 15%, ranges, and serialization.
- The legacy `BoldQualifiedVolumeText` value remains serialized with its default unchanged, but is hidden because qualified text is no longer reformatted.
- The older continuous percentage-intensity compatibility property remains serialized and hidden.
- No new setting or generated factory parameter was added.

## Secondary Series And Data Paths

- No Tick, Second, Bid, Ask, Last, Volumetric, or custom series was added or changed.
- Existing `AddDataSeries`, `OnMarketData`, `OnBarUpdate`, accepted-trade classification, primary-bar attribution, and shared publication are unchanged.
- Row emphasis reuses the existing display-compressed volume and delta maps.
- Because emphasis now belongs to the row, it can remain active when Show Volume Text is off.

## Tick Replay Implications

- No Tick Replay behavior changed.
- The feature continues to use the established inferred CVP delta and does not promote it to strict Bid/Ask evidence.

## Historical-Load Implications

- No historical request or rebuild path changed.
- As before, enabling strict hover evidence on an already-loaded historical chart requires the normal NinjaScript reload so the retained strict maps can be rebuilt from the selected source.

## Cache Implications

- No shared-cache contract, cache ownership, retention, or invalidation changed.

## Rendering Implications

- Qualifying non-POC rows select the existing RenderTarget-owned positive or negative delta brush before ordinary gradient/value-area fill selection.
- POC remains the first fill priority.
- Volume text now always selects its ordinary Volume Text Color in Volume mode; combined-mode brightness scaling remains unchanged.
- The two dedicated colored-volume-text Direct2D brushes were removed from allocation and disposal lifecycle because they are no longer used.

## Performance Implications

- Qualification remains one constant-time calculation per displayed volume row using already-aggregated values.
- No new render-time allocation, lock, cache read, profile build, or synchronization wait was added.
- Removing two dedicated text brushes slightly reduces RenderTarget-owned resources.

## Tests Performed In NinjaTrader

- Pending Julian's NinjaTrader F5 compile and chart validation.

## Static Tests Performed

- `dotnet run --project tests/OrcaFootprint.Tests -c Release --no-restore`: passed 411 checks.
- `dotnet run --project tests/OrcaFootprint.PlatformCheck -c Release --no-restore`: passed with 0 authored-source semantic errors, converter placement guard, render-allocation guard, invalid-wrapper regression, and 13 baseline fixtures.
- `dotnet run --project tests/OrcaFootprint.PlatformCheck -c Release --no-restore -- --live-generated`: passed with 0 semantic errors against the deployed source including NinjaTrader's generated wrapper.
- Focused guards confirm the threshold qualifier selects the row brush, the volume text path contains no delta brush selection, the legacy bold property is hidden, and row emphasis no longer depends on Show Volume Text.
- Scoped `git diff --check` passed; only the repository's existing LF-to-CRLF warnings were reported.
- Normalized authored-region parity between Working_Suite and the live NinjaTrader source passed exactly (119,417 characters in each authored region).

## Compile Status

- Offline authored-source semantic compile passed.
- NinjaTrader F5 compile is pending.

## Deployment Status

- Targeted deployment completed with `Orca Trades/Scripts/deploy_orca.ps1 -Target OrcaCandleVolumeProfile`.
- Only `OrcaCandleVolumeProfile.cs` was copied for this logical change.
- Live destination: `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaCandleVolumeProfile.cs`.
- The live source write time observed after deployment was 2026-09-12 1:28:15 PM local.
- No Custom DLL/PDB rebuild was confirmed or claimed in this pass.

## Manual-Validation Status

- Pending.
- In Volume mode with Show Volume Text on, confirm a row meeting both thresholds turns Positive Delta or Negative Delta while its number stays in Volume Text Color.
- Confirm a row failing either threshold keeps the ordinary volume gradient/value-area fill.
- Confirm the POC retains its POC color when it also qualifies.
- Turn Show Volume Text off and confirm qualifying bars can still be emphasized.
- Confirm the compact qualified-row hover and active-bar updates remain correct.

## Known Issues, Risks, And Follow-Up Work

- The internal serialized property name remains `ColorVolumeTextByDelta` for compatibility even though the displayed label now describes row emphasis.
- Positive and negative row fills use the existing delta colors and Delta Opacity.
- Visual contrast depends on the chosen Volume Text Color over the chosen delta fills and needs chart inspection.
- Property-grid visibility, template round-trip, F5/load, active-bar behavior, and live visual acceptance remain unverified.

## Promotion Eligibility

- Not eligible for promotion to `Full_Suite` until NinjaTrader compiles and Julian confirms the row colors, text color, settings behavior, hover, and active-bar rendering on a chart.
