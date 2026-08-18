# Orca Price Action - Zone Label Right-Center Alignment

Date: 2026-08-17

## Objective

Place every zone label at the vertical middle of its zone and align the text to
the zone's right edge without changing structure-event label anchors.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- FVG, iFVG, VI, RB, S-OB, C-OB, and PB labels are now vertically centered on
  their rendered zone.
- Those labels are trailing/right-aligned inside a layout rectangle inset three
  pixels from the zone's right edge.
- The label layout uses up to 128 horizontal pixels and respects the zone's left
  edge when the zone is narrower.
- Thin zones receive a minimum text-height layout rectangle centered on the same
  vertical midpoint, preventing the text from being clipped away by a one- or
  two-pixel-high zone.
- Bullish and bearish zones now use identical label placement; direction no
  longer selects top-versus-bottom placement.
- HH/LH/HL/LL, Sweep, BOS, CHoCH, Protected High/Low, and other structure-event
  labels retain their existing event/pivot anchors.
- Zone geometry, lifecycle, colors, opacity, extension, and quadrant behavior
  are unchanged.

## User-facing settings added, changed, deprecated, or removed

None. Zone-label alignment is the new fixed rendering behavior.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only and adds no Tick, Second,
Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. This is a render-only alignment change and Tick Replay remains
unnecessary.

## Historical-load implications

None. Detector history, the 3,000-bar discovery limit, terminal records, and
snapshot publication timing are unchanged.

## Cache implications

None. No cache, provider, database, shared service, or background work was added
or changed.

## Rendering implications

A dedicated DirectWrite text format uses `TextAlignment.Trailing` and
`ParagraphAlignment.Center`. It is created with the other render-target-bound
resources, recreated after render-target changes, and disposed during resource
teardown. `OnRender` calculates only a small value-type layout rectangle from
the immutable zone geometry and does not access mutable models.

## Performance implications

One additional DirectWrite text-format resource exists per indicator render
target. Each visible zone label adds a few constant-time scalar calculations;
there are no additional draw calls, collection scans, allocations, secondary
series, or synchronization waits.

## Tests performed in NinjaTrader

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
- Deterministic layout truth table: 3/3 passed for normal, narrow, and thin
  zones, including exact vertical-center preservation.
- Static assertions passed for the dedicated format, trailing/center alignment,
  right inset, width constraint, thin-zone minimum height, vertical-center
  formula, render-target creation/disposal/nulling, removal of direction-based
  placement, no secondary series, and no `OnMarketData`.
- `git diff --check`: passed for the three owned files.
- NinjaTrader `F5`: pending after targeted deployment.

## Deployment status

- `deploy_orca.ps1 -Target OrcaPriceAction -DryRun`: passed and resolved one
  live indicator target.
- `deploy_orca.ps1 -Target OrcaPriceAction`: completed successfully.
- Working_Suite versus live NinjaTrader authored-source parity: passed. The
  deployed copy contains the dedicated format, trailing/center alignment,
  vertical-center formula, and format disposal path.
- No other target was deployed.

## Manual-validation status

Pending. On MNQ 1-minute, inspect bullish and bearish FVG/iFVG/VI/RB/S-OB/C-OB/PB
zones at several chart scales. Confirm labels remain vertically centered,
right-aligned, visible in thin zones, and clipped normally when the entire zone
is outside the viewport. Confirm structure labels did not move.

## Known issues, risks, and follow-up work

- A label wider than the available zone width remains constrained by the zone's
  visible layout width and can be clipped rather than overflowing left.
- For extremely thin zones, the text layout extends equally above and below the
  zone to preserve its vertical center and readability.
- Live chart validation is needed across custom font sizes and opacity/color
  combinations.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live alignment confirmation are required first.
