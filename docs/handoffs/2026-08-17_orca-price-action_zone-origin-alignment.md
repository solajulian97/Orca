# Orca Price Action - FVG And VI Origin Alignment

Date: 2026-08-17

## Objective

Extend standard FVG rectangles through the complete three-candle pattern and
start volume-imbalance rectangles on the prior candle whose close forms the
first price boundary.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- A confirmed standard FVG now renders from candle one (`confirmation - 2`)
  instead of candle three.
- Original/remaining price bounds, partial fills, completion, timed badges, and
  invalidation are unchanged.
- `Extension Bars` is measured from the standard FVG's new visual origin.
- iFVG rectangles continue to begin at their later conversion confirmation bar
  so the inverted state is not drawn before it exists.
- A confirmed volume imbalance now stores the prior candle as its origin instead
  of the second candle. Its zone still spans prior close to current open.
- FVG and VI detection still waits for the completed confirming candle; the
  earlier origin is visual back-placement after confirmation.

## User-facing settings added, changed, deprecated, or removed

None. Existing FVG extension, fill display, VI mode, style, and visibility
settings are unchanged.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only and adds no Tick, Second,
Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. Standard FVG and VI qualification still
uses completed primary OHLC.

## Historical-load implications

Historical detector results and the 3,000-bar discovery limit are unchanged.
On reload, standard FVG geometry starts two bars earlier and VI geometry starts
one bar earlier. FVGs with a finite bar extension also end two bars earlier
because the configured length is measured from the new visual start.

## Cache implications

None. No cache, data provider, shared service, or background work was added or
changed.

## Rendering implications

Immutable FVG render items use `OriginBar` for standard FVGs and
`ConfirmationBar` for iFVGs. VI render items already consume `OriginBar`; VI
detection now records the prior candle there. `OnRender` remains geometry-only
and performs no model mutation or collection traversal.

## Performance implications

No detector scans, model counts, snapshot counts, or draw calls were added. The
change substitutes existing integer bar indices while publishing geometry.

## Tests performed in NinjaTrader

Pre-change screenshot review confirmed standard FVG rectangles beginning on the
third candle wick and VI rectangles beginning on the second candle.

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
- FVG origin truth table: 2/2 passed for standard first-candle placement and
  causal iFVG conversion placement.
- Static assertions passed for all four FVG render paths, VI prior-candle
  origin, unchanged VI price bounds, no secondary series, and no `OnMarketData`.
- `git diff --check`: passed for the three owned files.
- NinjaTrader `F5`: pending after targeted deployment.

## Manual-validation status

Pending. After F5, reload the screenshot area and confirm standard FVGs begin on
candle one, VI rectangles begin on the prior-close candle, iFVGs still begin at
conversion, and all vertical price bounds/fill behavior remain unchanged.

## Known issues, risks, and follow-up work

- A standard FVG is visually back-placed two bars after confirmation. This does
  not mean it was detectable on candle one.
- A first-RTH FVG confirmed just after the session open can visually begin on a
  pre-open pattern candle; slot qualification still uses the confirmation time.
- Timed `Until Period End` remains governed by its explicit clock boundary.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live origin/fill confirmation are required first.
