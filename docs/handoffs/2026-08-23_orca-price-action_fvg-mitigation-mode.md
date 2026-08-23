# Orca Price Action - FVG Mitigation Mode

## Objective

Add an optional close-confirmed Fair Value Gap lifecycle so wick-filled gaps can
remain visible for recurring support and resistance retests until price closes
strictly through the original far edge.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- `docs/handoffs/2026-08-23_orca-price-action_fvg-mitigation-mode.md`

`Orca Trades/Full_Suite` was not modified.

## Behavior added, changed, or removed

- Added `Wick Fill` and `Close Through` FVG mitigation modes.
- `Wick Fill` preserves the existing fully-filled terminal lifecycle.
- `Close Through` records wick fill normally but keeps the model active until a
  completed close is strictly beyond the original distal edge.
- A fully wick-filled zone awaiting close-through is rendered across its full
  original range with the filled/faded opacity.
- Standard FVG close-through can create one opposite iFVG when conversion is
  enabled. iFVG close-through terminates the iFVG without recursive inversion.
- First-RTH persistence, timed-FVG endpoints, and ordinary FVG extension limits
  are unchanged.

## User-facing settings

- Added `03. Fair Value Gaps > Mitigation Mode`:
  - `Wick Fill` (default and backward compatible)
  - `Close Through`

No setting was removed or deprecated.

## Secondary series and Tick Replay

- No secondary series were added or changed.
- No Tick, Second, Bid, Ask, Last, Volumetric, or custom series were added.
- No `OnMarketData` dependency was added.
- Tick Replay remains unnecessary.

## Historical-load implications

`Close Through` can retain more nonterminal FVG/iFVG records than `Wick Fill`
when gaps are wick-filled but never close-invalidated. The existing 3,000-bar
historical discovery limit and terminal caps are unchanged. Ordinary visible
geometry still obeys `Extension Bars`; increase it if an older active level
needs to remain drawn longer.

## Cache implications

No cache or shared-service behavior changed.

## Rendering implications

The calculation path publishes the existing immutable render snapshots. A
fully filled gap awaiting close-through publishes one faded full-range zone.
No calculation, collection mutation, cache read, or synchronization wait was
added to `OnRender`.

## Performance implications

The completed-bar path adds a constant-time close-edge test per active FVG.
There is no new background work or data series. Close-through mode may increase
the active-model count on long histories; the existing diagnostics warning for
large active collections remains applicable.

## Verification performed

- NinjaTrader F5 compile: pending Julian.
- NinjaTrader chart validation: pending Julian.
- Local .NET Framework semantic compilation against the installed NinjaTrader,
  WPF, and SharpDX assemblies passed with zero C# errors. Expected old-live-type
  conflict warnings were emitted because the installed custom assembly still
  contains the previously deployed Orca Price Action types.
- Seven deterministic close-boundary cases passed: bullish/bearish strict
  through, equality, inside-range, and neutral direction.
- Static lifecycle assertions passed for the new enum/property/default, Wick
  Fill terminal guard, Close Through faded fallback, iFVG terminal branch,
  preserved RTH endpoint/override, and absence of `AddDataSeries`/`OnMarketData`.
- `git diff --check` passed.
- Targeted deployment completed with `deploy_orca.ps1 -Target
  OrcaPriceAction`; its dry run and actual run named only the live
  `Indicators/OrcaPriceAction.cs` target.
- Normalized authored-source parity passed after deployment. Working and live
  authored regions both hashed to
  `da4d7290efd20d0d2edcca5f03f8edd98b4f6d04838a54623bf83eb9e4ffc6c9`.
- The live `NinjaTrader.Custom.dll` timestamp advanced to 7:34:10 PM EDT after
  the deployed source timestamp of 7:33:58 PM EDT. This records automatic
  assembly generation only; it is not proof of an explicit F5 compile, assembly
  load, or chart behavior.

## Manual-validation checklist

- Confirm `Wick Fill` matches the prior lifecycle.
- In `Close Through`, wick-fill a bullish and bearish FVG without closing
  through; confirm each remains as a faded full-range zone.
- Confirm an equal far-edge close does not invalidate the gap.
- Confirm a strict completed close beyond the far edge terminates the gap.
- Repeat for an iFVG and confirm no recursive inversion is created.
- Confirm first-RTH FVGs still extend through the configured RTH close even
  after fill or invalidation.
- Confirm ordinary FVGs still stop drawing at their configured extension cap.

## Known issues, risks, and follow-up work

- A standard FVG close-through may display an opposite iFVG when `Enable iFVG
  Conversion` is enabled; this is intentional and independently configurable.
- A 30-bar extension can visually hide an active recurring level before it is
  close-invalidated. The lifecycle continues in the model, and the user can
  increase `Extension Bars` when longer visible retention is desired.
- F5, reload, historical-to-realtime, and live chart behavior remain separate
  validation gates.

## Promotion eligibility

Not eligible for `Full_Suite` promotion until Julian completes F5 compilation
and manual NinjaTrader chart validation.
