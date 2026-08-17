# Orca Price Action - Order Block Extension And Quadrants

Date: 2026-08-17

## Objective

Limit Order Block geometry to a configurable number of bars and add a full-
range display mode with dashed quarter-range reference lines.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- S-OB, C-OB, and PB visual geometry now ends at the configured Order Block bar
  cap instead of extending indefinitely to the current bar.
- The default cap is 30 bars from the source candle and uses the same endpoint
  semantics as the existing FVG extension control.
- If an Order Block reaches a terminal state before the visual cap, its geometry
  ends at that earlier terminal bar.
- The cap is visual only. Touch, mitigation, invalidation, quality, parent
  linkage, and active-model retention continue after the displayed geometry
  stops.
- Added `Full Range With Quadrants` to the Order Block display choices.
- Existing enum values retain their prior numeric assignments so saved Body,
  Full Range, and Open And Midpoint selections remain backward-compatible.
- The mode uses the source candle's full low-to-high range and draws thin dashed
  lines at 25%, 50%, and 75% of that range.
- S-OB, C-OB, and PB share the quadrant geometry while retaining their existing
  type-specific border treatment and opacity.
- Rejection Blocks retain their separate wick-to-body geometry and are not
  capped or given quadrants by these Order Block settings.
- Existing Body, Full Range, and Open And Midpoint modes remain unchanged.
- Optional Open and Body Midpoint overlays remain available in quadrant mode.

## User-facing settings added, changed, deprecated, or removed

Added under `08. Order Blocks`:

- `Extension Bars`: default `30`, range `1` through `10,000`.
- `Display > Full Range With Quadrants`.

No existing setting was removed or deprecated. Existing NinjaScript enum and
integer properties remain template-persisted through NinjaTrader's normal
indicator serialization.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only and adds no Tick, Second,
Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. Order Block detection and lifecycle
continue to use completed primary-series OHLC.

## Historical-load implications

The 3,000-bar discovery window, block detector history, and terminal-record cap
are unchanged. Historical reloads publish shorter S-OB/C-OB/PB rectangles at
the default setting, but the same block records and lifecycle transitions are
calculated.

## Cache implications

None. No cache, provider, database, shared service, or background work was added
or changed.

## Rendering implications

Immutable Order Block render items now carry the capped endpoint and a quadrant
flag. In quadrant mode, `OnRender` derives the three Y coordinates from the
snapshot's full lower/upper bounds and draws them with the existing dashed
stroke resource. `OnRender` does not inspect or mutate block collections.

## Performance implications

The visual cap adds one constant-time endpoint clamp during snapshot
publication. Quadrant mode adds three thin line draw calls per visible S-OB,
C-OB, or PB; other modes add no draw calls. No historical rebuild, secondary
series, allocation-heavy render model, or asynchronous work was added.

## Tests performed in NinjaTrader

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
- Deterministic block-geometry truth tables: 10/10 passed for active and terminal
  endpoint caps, safe bounds/overflow, and 25%/50%/75% price calculations.
- Static integration assertions passed for the public enum/property/default,
  full-range geometry selection, visual-only endpoint cap, Rejection Block
  exclusion, immutable quadrant flag, three thin dashed lines, retained Body
  Midpoint overlay, no secondary series, and no `OnMarketData`.
- `git diff --check`: passed for the three owned files.
- NinjaTrader `F5`: pending after targeted deployment.

## Deployment status

- `deploy_orca.ps1 -Target OrcaPriceAction -DryRun`: passed and resolved one
  live indicator target.
- `deploy_orca.ps1 -Target OrcaPriceAction`: completed successfully.
- Working_Suite versus live NinjaTrader authored-source parity: passed. The
  deployed copy contains the preserved/new display enum values, 30-bar default,
  endpoint cap, immutable quadrant flag, and quadrant-price helper.
- No other target was deployed.

## Manual-validation status

Pending. On MNQ 1-minute, confirm S-OB/C-OB/PB rectangles stop at 30 bars,
post-cap lifecycle state still changes correctly, and Full Range With Quadrants
draws three thin dashed lines at the expected prices for bullish and bearish
blocks. Confirm Rejection Blocks are unchanged.

## Known issues, risks, and follow-up work

- The extension value controls geometry, not model eviction. An active block can
  remain diagnostically active after it is no longer drawn.
- In Full Range With Quadrants, an enabled Body Midpoint overlay may not coincide
  with the full-range 50% line; both are intentionally preserved.
- Very short chart windows can clip part of a capped block or its quadrant lines
  at the viewport boundary; ordinary render clipping still applies.
- Live chart validation is needed to calibrate dash visibility across chart
  scales and custom color/opacity combinations.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live endpoint/lifecycle/quadrant confirmation are required first.
