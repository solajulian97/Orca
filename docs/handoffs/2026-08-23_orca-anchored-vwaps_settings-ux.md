# Orca Anchored VWAPs Settings UX

Date: 2026-08-23

## Objective

Make the automated `OrcaAnchoredVWAPs` property panel easier to scan and configure while preserving its three VWAP calculations, plot indexes, existing colors, and default chart appearance.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaAnchoredVWAPs.cs`
- `docs/handoffs/2026-08-23_orca-anchored-vwaps_settings-ux.md`

## Behavior Added, Changed, Or Removed

- Reorganized custom settings into numbered `VWAPs`, `Anchor Detection`, `Deviation Bands`, `VWAP 2 Fill`, and `VWAP 3 Fill` sections.
- Added an `IndicatorBaseConverter` that hides fixed-tick fields while ATR reversals are enabled and hides ATR fields while fixed-tick reversals are active.
- Band controls now appear only for visible VWAPs, and the shared deviation section is hidden when no visible VWAP has bands enabled.
- Disabled bands hide their corresponding multiplier and unavailable fill zones.
- Added `Enable VWAP 2 Fill` and `Enable VWAP 3 Fill`, both defaulting to enabled to preserve the existing appearance.
- Fill color and opacity controls are hidden while their fill, VWAP, bands, or required adjacent deviation levels are disabled.
- Disabling a fill or deviation band now removes any previously drawn region immediately instead of allowing stale shading to remain.
- Existing `AddPlot` declarations and `Values` indexes were not reordered or renamed.

## User-Facing Settings

Added:

- `Enable VWAP 2 Fill`, default `true`.
- `Enable VWAP 3 Fill`, default `true`.

Renamed or regrouped for display only:

- `Parameters` became `2. Anchor Detection`.
- `Standard Deviation` became `3. Deviation Bands`.
- `Show All Bands (Override Filter)` became `Show Both Band Sides`.
- `Show Std Dev N` became `Show Band N`.
- `Std Dev Multiplier N` became `Band N Multiplier`.
- Region labels now use `VWAP to Band 1`, `Band 1 to Band 2`, and `Band 2 to Band 3` language.

No existing property names or NinjaScript-generated method signatures were changed.

## Secondary Series

- No Tick, Second, Bid, Ask, Last, Volumetric, or custom series were added or changed.

## Tick Replay Implications

- None. The indicator remains primary-series-only and `Calculate.OnBarClose` by default.

## Historical-Load Implications

- No calculation, anchor-tracker, or historical-loop behavior changed.

## Cache Implications

- No shared or local data cache was added or changed.
- Existing NinjaScript cache-signature properties remain unchanged.
- The two fill-enable controls are visual instance properties and are not added to generated programmatic indicator overloads.

## Rendering Implications

- VWAP and deviation plot rendering is unchanged.
- Region fills can now be disabled independently for VWAP 2 and VWAP 3.
- Region cleanup now removes all inactive or disabled zones, including zones made unavailable by band toggles.

## Performance Implications

- The property converter runs only when NinjaTrader builds or refreshes the settings grid.
- Region cleanup adds only bounded `RemoveDrawObject` calls in the existing bar-close region path when zones are inactive.
- No per-tick work, secondary-series work, SharpDX allocation, or render-path calculation was added.

## Tests Performed In NinjaTrader

- Deployed only `OrcaAnchoredVWAPs.cs` from `Working_Suite` to `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\Indicators`.
- Verified normalized source/live content parity.
- NinjaTrader F5 automation was attempted, but Windows app control was not approved for NinjaTrader; no platform compile or settings-panel interaction was performed.

## Compile Status

- Roslyn syntax parse: zero errors.
- `git diff --check`: passed with only the repository's existing LF-to-CRLF warning.
- Standalone semantic comparison: the edited source and untouched Git baseline each produced the same five errors caused by compiling one NinjaScript file without NinjaTrader's generated partial-class context; no additional semantic errors were introduced by this change.
- NinjaTrader F5 compile: pending.

## Manual-Validation Status

Pending Julian's live validation:

- Open the indicator properties and confirm the five numbered custom sections are easy to scan.
- Toggle `Use ATR-Based Reversals` and confirm fixed-tick versus ATR fields switch immediately.
- Toggle each VWAP and its bands and confirm irrelevant sections appear or disappear without losing stored values.
- Disable VWAP 2 and VWAP 3 fills and confirm existing shading disappears while lines remain.
- Toggle Bands 1, 2, and 3 and confirm unavailable zone fills disappear immediately.
- Save and reload a chart or template and confirm existing values and the new fill toggles persist.

## Known Issues, Risks, And Follow-Up Work

- NinjaTrader compile and property-grid behavior remain unverified until F5 and live panel testing are completed.
- The built-in `Plots` section still contains all 21 plot entries. Its underlying order was intentionally preserved to avoid template and index migration risk.
- Existing charts should retain their appearance because both new fill toggles default to enabled, but workspace/template persistence must be confirmed live.

## Promotion Eligibility

- Not eligible for promotion to `Full_Suite` until NinjaTrader compiles successfully and Julian confirms the settings workflow and chart rendering.
