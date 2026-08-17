# Orca Price Action - Protected Label Anchoring

Date: 2026-08-17

## Objective

Place `Protected High` and `Protected Low` text at the actual protected pivot
candle while retaining the active protected-level line.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- The protected-level dotted line still begins when the pivot becomes protected
  and extends to the current bar.
- The line no longer places its label at the current endpoint.
- `Protected High` is centered over the actual protected-high pivot wick.
- `Protected Low` is centered under the actual protected-low pivot wick.
- The protected label is stacked one text row farther from the wick than the
  existing HH/LH/HL/LL relation label so both remain readable.
- Structure detection and protected-pivot selection are unchanged.
- In a bearish trend, a strict completed close above the active protected high
  is bullish CHoCH. In a bullish trend, a strict completed close below the
  active protected low is bearish CHoCH. A wick-only violation remains a sweep,
  and an equal close is not a break.

## User-facing settings added, changed, deprecated, or removed

None. `Show Protected Levels`, structure colors, text size, pivot strength, and
break-buffer settings are unchanged.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only and adds no Tick, Second,
Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. Structure transitions and CHoCH require
completed primary-series closes.

## Historical-load implications

Detection history and the 3,000-bar discovery limit are unchanged. Historical
protected text moves from the active line endpoint to the protected pivot that
the line represents.

## Cache implications

None. No cache, shared service, data provider, or background work was added or
changed.

## Rendering implications

The immutable render snapshot now publishes a separate centered label item for
each currently active protected side. The label contains its pivot bar, pivot
price, and a small immutable vertical pixel offset. `OnRender` only consumes
that geometry; it does not inspect or mutate detector collections. SharpDX
resource lifecycle behavior is unchanged.

## Performance implications

At most one separate protected label is published for each active side. The
same text was removed from the corresponding line item, so model work and draw
volume remain effectively unchanged.

## Tests performed in NinjaTrader

The pre-change chart screenshot confirmed that protected text appeared near the
current line endpoint instead of the originating protected pivot.

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
- Static assertions passed for protected text pivot-bar/pivot-price anchoring,
  removal of line-endpoint text, renderer pixel-offset consumption, retained
  strict-break handling, no secondary series, and no `OnMarketData`.
- `git diff --check`: passed for the three owned files.
- NinjaTrader `F5`: pending after targeted deployment.

## Deployment status

Pending targeted `OrcaPriceAction` deployment.

## Manual-validation status

Pending. After F5, verify each protected label is centered at the actual
protected pivot, the relation label remains readable, and the dotted active
line still extends from protection time to the current bar.

## Known issues, risks, and follow-up work

- When the originating pivot is outside the visible chart range, its protected
  text is also outside the viewport even though part of the active dotted line
  may remain visible. This is the expected result of choosing direct pivot
  anchoring instead of endpoint labeling.
- A protected label is drawn on its historical pivot only while that pivot is
  the currently active protected boundary.
- The ordinary relation label remains closest to the wick; the protected label
  occupies the next row away from price.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live label/CHoCH confirmation are required first.
