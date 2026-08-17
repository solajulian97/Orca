# Orca Price Action - Zone Styles And Liquidity Labels

Date: 2026-08-17

## Objective

Remove misleading stale-pivot `Liquidity` labels and make every FVG, imbalance,
and block type independently editable for bullish/bearish fill color,
bullish/bearish border color, and opacity.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- Removed the aggregated `Liquidity` text event introduced for stale pivot
  close-throughs. Its anchor combined the current break bar with an old pivot
  price, which looked arbitrary and did not show active liquidity clearly.
- The immediate-BOS improvement remains intact. Older same-side pivots still
  lose BOS eligibility and are causally retired when crossed, so removing the
  text does not restore stale BOS lines.
- Zone rendering now selects independent fill and border brushes for FVG,
  iFVG, volume imbalance, rejection block, S-OB, C-OB, and PB.
- Each type has one independent opacity. Completed records and the filled
  portion of Two-Tone FVGs retain their global fade caps.
- Timed FVG highlights retain their dedicated highlight-border override.

## User-facing settings added, changed, deprecated, or removed

Added seven style groups:

1. `FVG Style`
2. `iFVG Style`
3. `Volume Imbalance Style`
4. `Rejection Block Style`
5. `Structural OB Style`
6. `Continuation OB Style`
7. `Propulsion Block Style`

Each group exposes `Bullish Fill`, `Bearish Fill`, `Bullish Border`, `Bearish
Border`, and `Opacity`.

Removed the shared `Active Zone Opacity` and `Block Opacity` controls. Renamed
the retained global fade controls to `FVG Filled-Portion Max Opacity` and
`Completed-Zone Max Opacity`. Existing bullish/bearish event colors now appear
under `Structure and Text` and no longer color every zone type.

Every new brush has a `Serialize.BrushToString` / `Serialize.StringToBrush` XML
proxy for indicator-template persistence.

## Secondary series added or changed

None. The indicator remains primary-series-only and adds no Tick, Second, Bid,
Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. These changes affect structure-event
publication and render styling, not the completed-OHLC detector contract.

## Historical-load implications

The 3,000-bar discovery default and terminal-record caps are unchanged.
Historical reload intentionally emits no stale-pivot liquidity labels. Zone
models and lifecycle scans are unchanged by the style settings.

## Cache implications

None. No cache, data provider, shared service, or background work was added or
changed.

## Rendering implications

The render-target resource set grows from 9 to 35 SharpDX brushes so each of
the seven zone types has separate bullish/bearish fill and border resources.
Resources are still created only for the render target, recreated on target
change, and disposed at termination. Immutable render items carry type,
direction, and opacity; `OnRender` does not traverse mutable detector
collections.

Order-block open and midpoint lines use the selected block border brush.
Continuation blocks retain dashed treatment and PB retains its thicker line.

## Performance implications

The additional 26 small brush resources are fixed per indicator instance and
do not scale with bars or detected zones. Zone draw-call counts and model
processing are unchanged. Removing liquidity labels slightly reduces immutable
label geometry and text draw calls on dense histories.

## Tests performed in NinjaTrader

Pre-change MNQ 1-minute screenshot review confirmed `Liquidity` words appearing
at visually confusing break-bar/old-price intersections. The same screenshot
confirmed the improved BOS selection and FVG bar limiter.

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- `git diff --check`: passed for the indicator at implementation time.
- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
- Static assertions passed for all 28 fill/border brush properties and XML
  proxies, all 7 per-type opacity properties, the 35-resource ordering, removal
  of liquidity-event publication, no secondary series, and no `OnMarketData`.
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

Pending. After F5, reload MNQ 1-minute and confirm no `Liquidity` labels, the
same reduced BOS behavior, correct independent fill/border/opacity routing for
all seven types, timed-highlight border behavior, and template save/reload
round-tripping. Follow with 15-second and range-chart sanity checks.

## Known issues, risks, and follow-up work

- Existing templates contain the former shared zone colors/opacities, not the
  new per-type fields. New fields use SetDefaults values until the user saves an
  updated template.
- Border opacity remains intentionally stronger than fill opacity; the new
  setting controls zone fill opacity, while border color is fully independent.
- `I` and `E` remain compact pivot-scope suffixes: Internal and External.
- A future optional liquidity feature should mark active unswept highs/lows at
  their origin, with explicit lifecycle rules, rather than reuse stale
  close-through events.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
template round-trip validation, and Julian's live chart confirmation are still
required.
