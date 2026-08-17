# Orca Price Action - Structure And Sweep Density

Date: 2026-08-17

## Objective

Make market structure readable by preventing one displacement close from
printing BOS events against many stale historical pivots and by rendering one
visible sweep label for repeated sweeps in the same nearby area.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- A close that crosses multiple same-direction pivots retires every crossed
  pivot but publishes at most two structure events: the most recent Internal
  pivot and the most recent External or Protected pivot.
- Protected pivots take priority within their scope so a valid CHoCH cannot be
  hidden by a newer ordinary pivot.
- Bullish sponsor promotion continues to choose the latest eligible confirmed
  low; bearish sponsor promotion continues to choose the latest eligible high.
- Same-direction sweep labels are clustered by time and price. A continuing
  cluster renders one `Sweep` word while all underlying sweep events remain
  available to rejection-block qualification.

Close-only break rules, equal-close behavior, pivot confirmation, CHoCH
protection requirements, and rejection-block predicates are unchanged.

## User-facing settings added, changed, deprecated, or removed

None. The two-scope structure limit and sweep-label clustering are default
readability rules rather than additional settings.

## Secondary series added or changed

None. The indicator remains primary-series-only and adds no Tick, Second, Bid,
Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. Structure and sweep decisions continue
to use completed primary OHLC.

## Historical-load implications

Historical reload will intentionally produce fewer BOS/CHoCH render events and
fewer visible sweep labels. Older crossed pivots remain causally retired, so
they cannot generate a delayed stale BOS later in the same reload.

## Cache implications

None. No cache or shared data service was added or changed.

## Rendering implications

Structure lines are limited at detection to one event per scope per impulse.
Sweep clustering occurs while publishing the immutable render snapshot, not in
`OnRender`. The renderer still consumes immutable arrays only.

## Performance implications

The change reduces structure line and label geometry on dense histories. Break
selection and sweep clustering scan existing in-memory pivot/event collections;
no background work, secondary data, or per-render model traversal was added.

## Tests performed in NinjaTrader

Pre-change screenshot review confirmed several BOS lines emitted by the same
close against old pivot levels and overlapping `Sweep` words around repeated
nearby lows/highs.

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- `git diff --check`: passed for the indicator.
- Local .NET Framework semantic compile against installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
  Duplicate-type warnings are expected because the deployed assembly contains
  the prior Orca Price Action version.
- Static assertions confirmed the two-scope selection, stale-pivot retirement,
  sweep cluster window/tolerance, no secondary series, and no `OnMarketData`.
- NinjaTrader `F5`: pending after targeted deployment.

## Manual-validation status

Pending. Reload the same MNQ chart and compare the exact areas shown in the
2026-08-17 screenshot. Confirm no more than one Internal and one External or
Protected break per impulse, nearest sponsor behavior, protected-pivot CHoCH,
and one visible sweep label per continuing nearby cluster.

## Known issues, risks, and follow-up work

- External/Protected breaks may still span a larger parent leg by design; the
  Internal event represents the nearer sub-leg.
- Sweep clustering uses a fixed rule of `max(4 ticks, 0.05 ATR)` and a bar
  window of `max(2, Pivot Strength)`. Live review may justify a user setting.
- The first historical-load optimization still requires a measured reload; this
  structure change does not substitute for that performance validation.

## Promotion eligibility

Not eligible for `Full_Suite`. NinjaTrader F5 compilation, measured reload, and
Julian's structure/sweep chart validation are required first.
