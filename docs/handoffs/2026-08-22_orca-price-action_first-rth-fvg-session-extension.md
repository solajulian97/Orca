# Orca Price Action - First RTH FVG Session Extension

Date: 2026-08-22

## Objective

Keep the first qualifying RTH fair value gap visible through the complete New
York regular trading session, including after the gap fills.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- A claimed first-RTH FVG stores its configured New York RTH-close timestamp
  and the last eligible primary bar at that boundary.
- Its visual endpoint overrides the ordinary `Extension Bars` cap and continues
  through the stored RTH endpoint.
- A completed or invalidated first-RTH original range remains visible at the
  existing completed-zone opacity even when `Show Completed Zones` is off.
- A model that is both first-period and first-RTH uses the longer RTH endpoint
  without publishing a duplicate overlapping period-history zone.
- Terminal first-RTH models are protected from terminal-record pruning until
  their session endpoint has been observed.
- Ordinary FVG, iFVG, first-period, fill, inversion, and lifecycle behavior is
  unchanged.

## User-facing settings added, changed, deprecated, or removed

None. Existing `Show First RTH FVG`, `RTH Open`, `RTH Close`, FVG opacity, and
completed-zone opacity settings remain authoritative. `Extension Bars` no
longer limits first-RTH geometry.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only.

## Tick Replay implications

None. Tick Replay remains unnecessary.

## Historical-load implications

The existing 3,000-primary-bar discovery window is unchanged. Historical
reconstruction records the first post-boundary bar and resolves the RTH visual
endpoint without a forward data request.

## Cache implications

None. No cache, provider, database, or background work was added.

## Rendering implications

Immutable FVG snapshots now use the stored RTH endpoint for the highlighted
first-RTH model. Before the boundary is observed, the rectangle ends at the
latest available bar; afterward it ends at the last eligible RTH bar. No future
X coordinate is synthesized, including on non-time charts. `OnRender` remains
snapshot-only.

## Performance implications

Each first-RTH model adds two scalar fields and one constant-time boundary check
per completed bar until RTH close. No secondary series, historical rescan,
render-model collection access, synchronization, or allocation-heavy path was
added.

## Tests performed in NinjaTrader

No post-change NinjaTrader chart test has been performed yet.

## Compile status

Local .NET Framework semantic compilation against the installed NinjaTrader
assemblies and current Working_Suite diagnostics source passed with zero errors.
The 155 warnings are expected duplicate-type warnings from compiling against the
already deployed custom assembly. RTH endpoint truth tables passed 5/5 for
pre-boundary, exact time-bar boundary, later non-time boundary, confirmation
floor, and negative-index safety. Static integration assertions passed 8/8 for
endpoint storage, completed visibility, bar-cap override, duplicate suppression,
pruning protection, and primary-series-only behavior. `git diff --check` passed.
NinjaTrader F5 compilation remains pending.

## Manual-validation status

Pending. On an MNQ intraday chart, confirm the first qualifying post-09:30 FVG
extends through the configured RTH close after partial fill, completion, and
close-through inversion. Confirm ordinary FVGs still stop at their configured
bar limit.

## Deployment status

Targeted deployment completed with
`deploy_orca.ps1 -Target OrcaPriceAction`; no other target was deployed. The
normalized authored Working_Suite and live NinjaTrader regions both have
SHA-256 `5c6a5a31fed5618bc9eeb61957875e7f0d331b117a36f405a9e57f92d82bb862`,
so authored-source parity passed. Source implementation commit: `acb4478`.
NinjaTrader F5 compilation remains pending and is not implied by deployment.

## Known issues, risks, and follow-up work

- On non-time charts, a bar that crosses the RTH boundary is not split; the
  endpoint uses the last causally eligible bar rather than a synthetic future
  coordinate.
- Historical first-RTH records remain subject to the normal terminal cap after
  their RTH endpoint has been resolved.

## Promotion eligibility

Not eligible for `Full_Suite` until NinjaTrader F5 compilation and Julian's live
chart confirmation.
