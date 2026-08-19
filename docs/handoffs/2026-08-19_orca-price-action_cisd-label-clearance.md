# Orca Price Action - CISD Label Clearance

Date: 2026-08-19

## Objective

Keep CISD confirmation and validation labels readable when their event candle
has a large body or wick.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- Bullish CISD labels now anchor below the event candle's low.
- Bearish CISD labels now anchor above the event candle's high.
- Both directions receive a four-pixel outward gap in addition to the renderer's
  normal text clearance.
- The original Raw/Qualified label and the later validated marker use the same
  placement helper.
- Event timestamps, direction, qualification, validation, and reference-line
  geometry are unchanged.

## User-facing settings added, changed, deprecated, or removed

None. Placement is automatic and preserves all existing CISD templates.

## Secondary series added or changed

None. The indicator remains primary-series-only.

## Tick Replay implications

None. Tick Replay is still not required.

## Historical-load implications

None. Existing historical discovery and event caps are unchanged.

## Cache implications

None.

## Rendering implications

Snapshot publication now resolves each visible CISD label against the stored
event bar's high or low and publishes the existing immutable label item with a
small pixel offset. `OnRender` remains snapshot-only and performs no model
lookup or mutation.

## Performance implications

Negligible: two existing primary-series value lookups replace the stored close
anchor for each visible CISD label during snapshot publication. No allocation
loop, secondary series, cache access, lock, or background work was added.

## Tests performed in NinjaTrader

Julian's pre-change MNQ two-minute screenshot confirmed that CISD labels were
drawn over candle bodies. Post-change NinjaTrader chart validation is pending.

## Compile status

Local .NET Framework semantic compilation against the installed NinjaTrader
assemblies and current Working_Suite diagnostics source passed with zero errors.
The 155 warnings are expected duplicate-type warnings from compiling against the
already deployed custom assembly. Deterministic label-placement assertions
passed 7/7 for shared placement, high/low anchoring, outward pixel clearance,
confirmation/validation consistency, and removal of close-price label anchors.
`git diff --check` passed. NinjaTrader F5 compilation remains pending.

## Manual-validation status

Pending. Confirm bullish labels below lows and bearish labels above highs for
Raw, Qualified, and later Validated events while zooming and panning.

## Deployment status

Targeted deployment completed with
`deploy_orca.ps1 -Target OrcaPriceAction`; no other target was deployed. The
normalized authored Working_Suite and live NinjaTrader regions both have
SHA-256 `3ac69449ee0216a3bd2f5352833bf52ec02b1c0eb40a71e584ae6f551728b7a5`,
so authored-source parity passed. Source implementation commit: `8ff3375`.
NinjaTrader F5 compilation remains pending and is not implied by deployment.

## Known issues, risks, and follow-up work

- A label can still share space with an unrelated neighboring candle or another
  indicator's drawing in an exceptionally dense chart; this change guarantees
  clearance from its own event candle.
- If Julian wants wider spacing after live review, a configurable pixel-gap
  property can be added later without changing CISD detection.

## Promotion eligibility

Not eligible for `Full_Suite` until NinjaTrader F5 compilation and Julian's live
behavior confirmation.
