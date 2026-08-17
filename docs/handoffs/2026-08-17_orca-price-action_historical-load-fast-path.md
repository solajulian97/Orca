# Orca Price Action - Historical Load Fast Path

Date: 2026-08-17

## Objective

Reduce the unacceptable initial historical load reported during the first live
Orca Price Action chart test without changing any detector, lifecycle,
mitigation, invalidation, or display-setting semantics.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- Historical callbacks now perform completed-bar model work without updating
  developing-bar FVG geometry.
- Immutable render arrays are published at the final historical callback and
  again on the realtime state transition, rather than after every historical
  callback.
- Realtime snapshots are republished on a completed-bar model change or when an
  active FVG's remaining geometry actually changes intrabar.
- Terminal-record collections are counted first and sorted only when the
  configured removable-record cap is exceeded.

Detection predicates, structure timing, block selection, zone lifecycle, and
all visual geometry rules are unchanged.

## User-facing settings added, changed, deprecated, or removed

None. `Maximum Historical Bars` remains 3,000 by default so the first retest
measures the fast path before reducing discovery depth.

## Secondary series added or changed

None. The indicator remains primary-series-only and adds no Tick, Second, Bid,
Ask, Last, Volumetric, or custom series.

## Tick Replay implications

Tick Replay remains unnecessary. If it is enabled at the chart level, repeated
historical `OnPriceChange` callbacks no longer rebuild developing FVG geometry
or immutable render arrays. Completed primary bars remain the causal source.

## Historical-load implications

The 3,000-bar discovery cutoff still governs detector creation. Callbacks before
that cutoff now have only the minimum bar-transition bookkeeping. Model work
inside the window remains completed-bar-based, and one chart-ready snapshot is
published at the historical boundary.

## Cache implications

None. No shared or local market-data cache was added or changed.

## Rendering implications

`OnRender` remains snapshot-only. Snapshot content and SharpDX resource
lifecycle are unchanged; only publication frequency is reduced.

## Performance implications

The previous path allocated and rebuilt complete zone, line, and label arrays
on every historical callback, including callbacks before the discovery cutoff.
It also sorted each terminal collection every processed bar even when under its
cap. The fast path removes those unnecessary operations. Detector collection
scans within the final 3,000 bars remain and require measurement after reload.

## Tests performed in NinjaTrader

Pre-change live observation from Julian: the first indicator load remained in
`Calculating` for at least approximately five minutes. Exact interval, loaded
date range, and final completion time were not recorded, so this is a defect
signal rather than a controlled benchmark.

No post-change NinjaTrader test has been performed yet.

## Compile status

- `git diff --check`: passed for the indicator.
- Local .NET Framework semantic compile against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
  Expected duplicate-type warnings were produced because the deployed assembly
  still contains the prior Orca Price Action types.
- NinjaTrader `F5`: pending after targeted deployment.

## Manual-validation status

Pending. Retest on the same chart and record interval, loaded date range, Tick
Replay state, approximate start time, time to realtime, and whether the chart
remains responsive.

## Known issues, risks, and follow-up work

- The exact contribution of snapshot publication versus detector collection
  scans has not yet been measured in NinjaTrader.
- Confirm final snapshot publication on disconnected or historical-only charts.
- If the same 3,000-bar test remains slow, profile active FVG, pivot, rejection,
  and order-block collection sizes before changing detector semantics.
- Lowering `Maximum Historical Bars` remains a temporary calibration option,
  not the primary correction for work outside the discovery window.

## Promotion eligibility

Not eligible for `Full_Suite`. NinjaTrader F5 compilation, a measured historical
reload, historical-to-realtime transition, and chart behavior still require
Julian's confirmation.
