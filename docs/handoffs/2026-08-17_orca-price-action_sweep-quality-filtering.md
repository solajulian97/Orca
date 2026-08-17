# Orca Price Action - Sweep Quality Filtering

Date: 2026-08-17

## Objective

Reduce insignificant visible sweep labels by requiring configurable resting
liquidity quality, penetration, and age without changing the structure/BOS
pivot engine.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- Visible sweep detection now uses a dedicated `Sweep Quality` filter.
- `Qualified Liquidity`, the default, requires a Standard-or-better pivot that
  is External, Protected, Sponsor, or the active trend-side target.
- `Major Only` accepts Protected pivots, Sponsors, and External Major pivots.
- `All Confirmed Pivots` restores broad pivot eligibility while retaining the
  independent penetration and resting-bar controls.
- A visible sweep now requires a true wick violation of at least the configured
  penetration and a completed close back inside the actual pivot price.
- The structure break buffer no longer allows a sweep candle to close outside
  the swept level.
- A pivot must rest for the configured number of bars after confirmation before
  its first visible sweep.
- A swept level may resweep while its rolling label episode remains open. Once
  the episode window expires, that level is display-consumed and cannot emit a
  later visible sweep label.
- Existing same-side latest-wins label coalescing remains in place, providing a
  second density reduction when distinct qualified levels are swept close
  together.
- Rejection Blocks continue to apply their own liquidity preset. Extra sweep
  events found only for Rejection Block qualification are stored for block logic
  but no longer publish visible `Sweep` text.
- Pivot confirmation, HH/LH/HL/LL, BOS, CHoCH, protected levels, and rejection
  candle requirements are otherwise unchanged.

## User-facing settings added, changed, deprecated, or removed

Added under `06. Market Structure`:

- `Sweep Quality`: `Qualified Liquidity` default, `Major Only`, or
  `All Confirmed Pivots`.
- `Minimum Sweep Penetration (Ticks)`: default `1`, range `0` through `100`.
- `Minimum Resting Bars`: default `3`, range `0` through `100`.

`Sweep Label Window Bars` remains at its existing default of `5`. Setting both
new numeric controls to zero makes `All Confirmed Pivots` closest to the prior
permissive detector, while a strict wick violation and close back inside remain
mandatory.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only and adds no Tick, Second,
Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. Sweep qualification continues to use
completed primary-series OHLC only.

## Historical-load implications

The 3,000-bar discovery window is unchanged. With the new defaults, historical
reloads store and publish fewer generic sweep events because Weak, ordinary
Internal, too-new, and under-penetrated pivots are rejected. Rejection-qualified
hidden sweep events remain available to that block engine.

## Cache implications

None. No cache, provider, shared service, database, or background work was added
or changed.

## Rendering implications

Structure events now carry an immutable display-label flag. Snapshot publication
skips rejection-only sweep events and retains the existing latest-wins high-side
and low-side episode coalescing. `OnRender` remains collection-free and performs
no detector mutation or filtering.

## Performance implications

Each completed bar adds small constant-time eligibility, timing, and penetration
predicates to the existing pivot scan. The default filter reduces stored generic
sweep events and visible text geometry. No new historical rebuild, allocation-
heavy render work, secondary series, or asynchronous work was added.

## Tests performed in NinjaTrader

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
- Standalone deterministic sweep truth tables: 19/19 passed for quality modes,
  resting boundaries, rolling resweeps, episode consumption, penetration,
  close-back-inside, and zero-threshold strict violation.
- Static integration assertions passed for the public enum/properties/defaults,
  quality/timing/penetration wiring, visible-versus-rejection-only event flags,
  renderer gating, no secondary series, and no `OnMarketData`.
- `git diff --check`: passed for the three owned files.
- NinjaTrader `F5`: pending after targeted deployment.

## Deployment status

- `deploy_orca.ps1 -Target OrcaPriceAction -DryRun`: passed and resolved one
  live indicator target.
- `deploy_orca.ps1 -Target OrcaPriceAction`: completed successfully.
- Working_Suite versus live NinjaTrader authored-source parity: passed. The
  deployed copy contains the quality enum/default, one-tick and three-bar
  defaults, and visible-event flagging.
- No other target was deployed.

## Manual-validation status

Pending. On MNQ 1-minute, compare Qualified Liquidity against All Confirmed
Pivots, verify the default removes nearby Weak/Internal noise, and confirm valid
resweeps move one label only while rejection blocks still qualify normally.

## Known issues, risks, and follow-up work

- Sweep importance and role tests use the pivot's classification as of the
  sweep bar. A previously ordinary pivot can become eligible after a later
  Sponsor/Protected/Major promotion.
- The resting-bar count starts at pivot confirmation, not the original pivot
  candle, preserving causal availability.
- `All Confirmed Pivots` still honors the independent numeric controls; set them
  to zero for maximum sensitivity.
- Distinct same-side qualified levels swept within the label window still share
  one latest label for readability, although their detector events remain
  separate.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live sweep-density and Rejection Block confirmation are required
first.
