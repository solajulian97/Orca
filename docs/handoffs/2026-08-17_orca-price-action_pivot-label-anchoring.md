# Orca Price Action - Pivot Label Anchoring

Date: 2026-08-17

## Objective

Place HH/LH/HL/LL structure labels directly over the pivot candle they describe
instead of offsetting them to the later confirmation candle.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- Confirmed pivot labels now use the pivot bar as their horizontal render
  anchor rather than the confirmation bar.
- High labels are centered just above the pivot wick. Low labels are centered
  just below the pivot wick.
- Pivot detection remains causal. A label is not published until `Pivot
  Strength` right-side bars have closed; only its visual X position references
  the already-known origin candle.
- BOS, CHoCH, sweep, protection, order-block, and FVG behavior are unchanged.

## User-facing settings added, changed, deprecated, or removed

None. Direct pivot anchoring is the default structure-label presentation.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only and adds no Tick, Second,
Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. Pivot confirmation still uses completed
primary OHLC.

## Historical-load implications

Historical pivot decisions and the 3,000-bar discovery limit are unchanged.
Existing confirmed labels move horizontally from confirmation bars to their
origin pivots on reload.

## Cache implications

None. No cache, data provider, shared service, or background work was added or
changed.

## Rendering implications

Immutable pivot label items now carry the pivot bar and a centered-alignment
flag. A dedicated centered DirectWrite format is created with the render target,
recreated on target change, and disposed at termination. `OnRender` still does
not traverse or mutate detector collections.

## Performance implications

One additional fixed DirectWrite text-format resource is held per indicator
instance. Label count, detector work, snapshots, and draw-call counts are
unchanged.

## Tests performed in NinjaTrader

Pre-change screenshot review confirmed HH/LH/HL/LL labels rendered several bars
to the right of their corresponding wick because their X coordinate used the
confirmation bar.

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
- Static inspection confirmed the pivot-bar anchor, centered-format selection,
  and matching creation/disposal lifecycle.
- NinjaTrader `F5`: pending after targeted deployment.

## Deployment status

- `deploy_orca.ps1 -Target OrcaPriceAction -DryRun`: passed and resolved one
  live indicator target.
- `deploy_orca.ps1 -Target OrcaPriceAction`: completed successfully.
- Working_Suite versus live NinjaTrader authored-source parity: passed. The
  live file retains NinjaTrader's generated wrapper after the matching authored
  region.
- No other target was deployed.

## Manual-validation status

Pending. After F5, reload MNQ 1-minute and confirm each HH/LH label is centered
over its high wick and each HL/LL label is centered under its low wick. Confirm
the label still appears only after the right-side pivot confirmation window.

## Known issues, risks, and follow-up work

- Labels intentionally appear on a historical pivot candle after confirmation;
  this is visual back-placement, not early detection or rewritten structure.
- Dense neighboring pivots can still place labels near one another. This change
  centers each label accurately but does not add a collision-avoidance engine.
- The screenshot contains `Liquidity` labels from the previously loaded build;
  the latest separate zone-style build removes those after F5 and reload.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live alignment confirmation are required first.
