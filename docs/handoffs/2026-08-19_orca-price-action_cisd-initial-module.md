# Orca Price Action - CISD Initial Module

Date: 2026-08-19

## Objective

Add an optional Change in State of Delivery engine that detects strict completed
closes through opposing candle-delivery opens, grades contextual quality, and
records later structural validation without redefining Orca's authoritative
BOS/CHoCH/protected-pivot engine.

## Existing architecture found

- One `Calculate.OnPriceChange` primary-series update loop processes each newly
  completed bar once.
- FVG, VI, rejection, structure, and typed Order Block engines share completed-
  bar models. Only developing-bar FVG remaining geometry mutates intrabar.
- Sweeps and Rejection Blocks are detected before structure breaks; the break
  engine returns explicit BOS/CHoCH events used by Order Blocks.
- Mutable models publish immutable zone/line/label arrays. `OnRender` reads only
  those arrays and owns no model calculation or collection traversal.
- Display presets control visibility only. Rejection and Order Block detector
  presets remain independent.
- The indicator has diagnostics integration but no existing alert subsystem or
  public event/plot API.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- Added a separate incremental CISD delivery-run/reference/event engine.
- Bullish CISD uses a strict completed close above an eligible bearish-run open;
  bearish CISD uses the mirrored close below a bullish-run open.
- Equal closes and wick-only violations do not confirm CISD.
- Delivery Run Origin and Last Opposing Candle reference modes are supported.
- Consecutive directional runs store start/end bars and times, first/last opens,
  high/low, candle count, net move, ATR-normalized move, reference, age, fired,
  expiry, and replacement state.
- A reversal candle finalizes the prior run before testing its own close, so it
  can confirm CISD on the same completed bar. A doji finalizes the run but does
  not become a reference or fire CISD.
- One reference produces no more than one event. Same-direction events inside
  the suppression window are consumed without another label.
- Raw and Qualified quality are stored at confirmation. Qualified filters reuse
  existing sweep events, ATR, standard same-direction FVGs, and confirmed Strong
  Rejection Blocks.
- When CISD's liquidity scope is broader than the visible Sweep Quality, the
  existing sweep price-action/timing predicates record context-only events with
  display disabled, preserving sweep-label density.
- Later same-direction CHoCH, or BOS when selected, validates the most recent
  eligible CISD inside the window. Same-bar breaks are excluded. A later marker
  is timestamped at validation while the original event retains its Raw or
  Qualified label.
- CISD reads structure context only. It does not assign trend direction,
  protected IDs, pivot roles, BOS/CHoCH, or Order Block confirmation.
- Historical CISD events are capped independently. The reference engine retains
  at most one completed reference per delivery direction plus the active run.
- Diagnostics status adds Raw, Qualified, Validated, bullish/bearish, External-
  sweep, CHoCH/BOS follow-through, expiry, and suppression counters.
- No alert was added because Orca Price Action has no existing alert subsystem;
  adding one only for CISD would create a separate lifecycle surface outside
  this V1 integration.

## User-facing settings added, changed, deprecated, or removed

Added `Show CISD` under Visibility and a dedicated `08A. CISD` group:

- Enable CISD Engine: Off default.
- Reference Mode: Delivery Run Origin default or Last Opposing Candle.
- Signal Mode: Raw/Research, Balanced default, Strict, or Custom.
- Minimum Delivery Run Candles: 1 default.
- Maximum Reference Age: 20 bars default.
- Break Buffer: 0 ticks default.
- Minimum Delivery Move: 0.5 ATR default.
- Liquidity Sweep Requirement and 8-bar lookback.
- Displacement requirement and 0.5 ATR minimum CISD candle body.
- Same-direction FVG and Strict Rejection Block requirements.
- Structural validation: CHoCH only default or CHoCH/BOS; 20-bar window.
- Same-direction suppression: 3 bars default.
- Active-reference, Raw, Qualified, and Validated visibility controls.
- Maximum Historical Events: 200 default.
- Raw/Qualified/Validated opacity: 30/75/100 defaults.
- Reference-line style and width: dashed, 1.5 defaults.

`CleanCore` and `BlocksFocused` hide CISD. `FullContext` makes CISD visible but
does not override the independent disabled-by-default engine switch. No existing
public property or enum value was renamed or reordered.

For a Blocks Focused chart, both `Show CISD` and `Enable CISD Engine` must be
turned on. Full Context already sets the visibility half, but the engine still
requires explicit activation.

## Secondary series added or changed

None. CISD uses only completed OHLC from the primary chart series and adds no
Tick, Second, Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay is not required. CISD confirmation is completed-bar close
based, and no replayed market-data callback is consumed.

## Historical-load implications

The existing 3,000-primary-bar discovery cutoff remains authoritative. CISD
reconstructs incremental run/reference state only inside that processed window.
At the default cap, at most 200 historical CISD events are retained. Enabling
CISD adds constant-time run/reference updates and bounded contextual scans over
existing capped collections.

## Cache implications

None. No cache, provider, database, shared service, or background work was added
or changed.

## Rendering implications

CISD adds immutable short-line and label items during snapshot publication.
Raw, Qualified, and Validated opacity travels in the snapshot. `OnRender`
temporarily applies that opacity to existing bullish/bearish brushes, restores
the brush value after each draw, and never reads CISD models. No large shaded
rectangle or NinjaTrader drawing object is created.

## Performance implications

The disabled engine returns immediately. When enabled, run tracking and
reference confirmation are constant-time. To preserve CISD's broader liquidity
scope without increasing visible sweep density, each completed bar can add two
filtered scans of the existing pruned pivot list for context-only high/low
sweeps. Qualification scans existing bounded sweep/FVG/Rejection collections
only when a raw CISD actually fires. Validation scans the capped CISD list only
when BOS/CHoCH events exist. No secondary series, historical full-chart rescan,
per-tick rebuild, render-model mutation, lock, wait, or asynchronous task was
added. Live diagnostics should compare enabled versus disabled model work before
the engine is promoted.

## Tests performed in NinjaTrader

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and current Working_Suite diagnostics source: passed with no
  errors. The 111 warnings are expected duplicate-type warnings from compiling
  source against the deployed `NinjaTrader.Custom.dll`.
- Deterministic CISD primitive/integration truth tables: 46/46 passed for strict
  bullish/bearish closes, equality, wick-only cases, tick-buffer boundaries,
  both reference modes, ATR movement, expiry, suppression, completed-bar event
  ordering, dojis, one-shot references, later-only validation, immutable
  rendering, disabled defaults, historical cap, structural non-mutation, hidden
  context-sweep rendering, as-of sweep importance/protection snapshots, CHoCH
  preference when BOS and CHoCH share a bar, and most-recent-prior-CISD
  validation selection.
- Deterministic delivery-sequence truth tables: 10/10 passed for single- and
  multi-candle runs, strict-versus-fast reference behavior, same-reversal-bar
  confirmation, doji preservation, one event per reference, and later
  validation timing.
- NinjaScript-property persistence assertions: 26/26 passed; default-value
  assertions: 7/7 passed. Actual NinjaTrader indicator-template round-tripping
  remains part of live validation.
- `git diff --check`: passed for the three owned files.
- NinjaTrader `F5` remains a separate pending gate.

## Deployment status

Targeted deployment completed with
`deploy_orca.ps1 -Target OrcaPriceAction`; no other target was deployed. The
normalized authored Working_Suite and live NinjaTrader regions both have
SHA-256 `27933f95cace3ca422386afb16c0a3d6d2ddc5a3814b8454dd56ae2c4065cd8b`,
so authored-source parity passed. Source implementation commit: `ff12f9f`.
NinjaTrader `F5` compilation remains pending and is not implied by deployment.

## Manual-validation status

Pending. MNQ two-minute is the primary CISD calibration chart, followed by MNQ
three-minute selectivity and MNQ one-minute noise/responsiveness checks. Existing
non-CISD engines also require regression review with CISD disabled and enabled.

## Known issues, risks, and follow-up work

- Balanced/Strict thresholds are starting points, not statistically proven MNQ
  optima. Measure CISD frequency, MSS follow-through, MFE, and MAE before product
  claims.
- Balanced requires a recent Standard-or-better Internal/External sweep. Strict
  requires External sweep context plus displacement; both can produce Raw events
  that remain hidden under defaults.
- Same-direction FVG qualification is intentionally confined to a standard FVG
  confirmed on the CISD bar to avoid attaching unrelated historical gaps.
- Strong Rejection context must already be confirmed; a same-bar follow-through
  candidate is not retroactively treated as confirmed.
- Session behavior follows the indicator's existing no-reset policy. References
  expire by bar age rather than being reset at a session boundary.
- Alerts, public plots/data series, scanners, strategies, multi-timeframe CISD,
  and statistical persistence remain outside V1.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live CISD behavior confirmation are required first.
