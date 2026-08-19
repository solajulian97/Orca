# Orca Price Action - Timed FVG Displacement Anchor

Date: 2026-08-19

## Objective

Assign first-period FVG highlights to the New York clock bucket containing the
middle displacement candle itself, rather than the later third-candle
confirmation timestamp.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- First-period 15-minute, 30-minute, 1-hour, and 4-hour slot claims now use the
  opening timestamp of the three-candle pattern's middle displacement candle.
- On fixed-minute and fixed-second primary charts, the displacement opening time
  is derived by subtracting the primary interval from NinjaTrader's close-
  stamped displacement-bar time.
- On non-time primary charts, the preceding bar timestamp is used as the causal
  opening boundary because a synthetic duration is unavailable.
- The model's confirmation timestamp remains the third completed candle. FVG
  detection, rendering origin, lifecycle, fill, inversion, and filtering remain
  close-confirmed and unchanged.
- The separate first-RTH claim deliberately retains its confirmation-timestamp
  rule, including exact-09:30 exclusion.
- A displacement candle opening at 11:58 can no longer claim the 12:00 hourly
  slot merely because the third pattern candle is close-stamped 12:00. The first
  12:00-hour candidate must have a middle displacement candle whose opening time
  is 12:00 or later.

## User-facing settings added, changed, deprecated, or removed

None. Existing Timed FVG period and extension settings retain their names and
stored values; only the period-claim timestamp semantics changed.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only and adds no Tick, Second,
Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. The change uses completed primary-series
timestamps and configured primary bar-period metadata.

## Historical-load implications

The 3,000-bar discovery window and historical fast path are unchanged. Reloading
history can reassign only boundary-straddling first-period highlights to their
displacement candle's actual bucket; detector workload and record limits are
unchanged.

## Cache implications

None. No cache, provider, database, shared service, or background work was added
or changed.

## Rendering implications

None. Immutable zone snapshots, FVG geometry, labels, timed extension, and
SharpDX resource lifecycle are unchanged. Only the bucket key and stored period
end selected during completed-bar detection can differ.

## Performance implications

Each qualifying standard FVG performs constant-time timestamp arithmetic and a
primary bar-period type check. No per-tick historical rebuild, collection scan,
allocation-heavy render work, secondary series, or synchronization was added.

## Tests performed in NinjaTrader

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and current Working_Suite diagnostics source: passed with no
  errors. The 103 warnings are expected duplicate-type warnings from compiling
  source against the deployed `NinjaTrader.Custom.dll`.
- Deterministic timestamp/bucket truth tables: 11/11 passed for the reported
  one-minute 11:58 case, first 12:00-hour eligibility, 15-second and 30-minute
  opening boundaries, four-hour anchoring, non-time fallback, separate period
  and RTH clocks, and the primary-series-only contract.
- NinjaTrader `F5` remains a separate pending gate.

## Deployment status

Pending targeted `OrcaPriceAction` deployment and authored-source parity check.

## Manual-validation status

Pending. On MNQ 1-minute, confirm the reported 11:58 displacement no longer
claims the 12:00 first-hour slot and that the first qualifying pattern whose
middle candle opens at or after 12:00 claims it instead. Also sanity-check one
15-second boundary and confirm first-RTH behavior is unchanged.

## Known issues, risks, and follow-up work

- Non-time bars have no fixed duration, so their displacement opening boundary
  is the preceding bar timestamp rather than an extrapolated synthetic time.
- The change intentionally does not redefine the separate first-RTH clock. If
  Julian later wants RTH to use displacement opening time too, that should be a
  separate explicit behavior change.
- NinjaTrader F5 and live boundary validation are still required.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live timed-FVG confirmation are required first.
