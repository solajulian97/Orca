# Orca Price Action - Immediate Structure And FVG Extension

Date: 2026-08-17

## Objective

Restrict BOS to the nearest current swing, classify older crossed swings as
liquidity, and give standard FVG geometry a configurable bar-length endpoint.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- The newest BOS-eligible pivot on the crossed side is the immediate structure
  level and is the only ordinary pivot that can publish BOS.
- A distinct close through the protected parent boundary still publishes
  CHoCH. A protected immediate pivot publishes CHoCH rather than duplicate BOS
  and CHoCH events.
- When immediate structure breaks, older unbroken same-side pivots lose future
  BOS eligibility but remain available as liquidity until price closes through
  them.
- A close through one or several older levels publishes one compact
  `Liquidity` label. It does not draw a line back to the old pivot and does not
  create an order block or continuation block.
- Every crossed pivot is still retired causally, so the same level cannot print
  again on a later bar.
- Standard FVG, iFVG, and first-RTH rectangles stop after the configured number
  of bars. Fill, completion, and inversion tracking continue after the visual
  endpoint.
- First-period FVGs preserve their explicit `Until Filled` or
  `Until Period End` behavior.

## User-facing settings added, changed, deprecated, or removed

- Added `Fair Value Gaps > Extension Bars`.
- Default: `30` bars.
- Allowed range: `1` through `10,000` bars.
- No structure settings were added or removed. The existing `Show BOS` control
  also controls the replacement `Liquidity` labels.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only and adds no Tick, Second,
Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. Structure decisions and FVG lifecycle
changes remain completed-primary-bar operations, apart from the existing
developing-bar FVG shrink behavior.

## Historical-load implications

Reloads intentionally produce fewer long structure lines. Older pivot records
remain in memory only until their normal terminal cleanup or until price takes
them as liquidity. The 3,000-bar discovery window is unchanged. The new FVG
bar cap changes visible geometry only and does not shorten historical model
discovery.

## Cache implications

None. No cache, shared service, background worker, or external data access was
added or changed.

## Rendering implications

Liquidity is published as one immutable label at the close-through bar. It has
no origin-to-break line. FVG render items use a precomputed capped end bar while
the underlying zone models retain their complete lifecycle. `OnRender` still
consumes immutable arrays and does no model mutation or collection traversal.

## Performance implications

The change reduces long BOS line geometry and limits the horizontal span of
most FVG rectangles. One boolean is stored per pivot, and break processing
updates older same-side eligibility only when a completed close actually breaks
structure. Active-zone lifecycle scanning is unchanged because visual expiry
does not delete or invalidate a live FVG.

## Tests performed in NinjaTrader

Pre-change MNQ 1-minute screenshot review confirmed BOS labels and lines tied
to older levels after immediate structure had already broken, plus standard FVG
rectangles extending across the chart.

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- `git diff --check`: passed for the three owned files.
- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
- Deterministic structure selection truth table: 4/4 passed, covering nearest
  BOS, separate protected CHoCH, protected-immediate deduplication, and a
  stale-liquidity-only close.
- Deterministic FVG endpoint truth table: 4/4 passed, covering the default cap,
  early terminal endpoints, minimum one-bar normalization, and negative-origin
  normalization.
- Static assertions confirmed the timed-period override, 30-bar default, no
  secondary series, and no `OnMarketData`.
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

Pending. Reload the same MNQ 1-minute chart and confirm one immediate BOS,
protected-boundary CHoCH when applicable, compact `Liquidity` labels for older
crossed levels, no old-level BOS lines, and a 30-bar default endpoint for
ordinary FVGs. Then change `Extension Bars` and verify the lifecycle still
updates after the rectangle endpoint.

## Known issues, risks, and follow-up work

- Immediate means the most recently formed confirmed pivot on that side. Live
  calibration may show that a reversal-magnitude filter should qualify that
  pivot before it becomes BOS-eligible.
- A break can show one immediate BOS and one protected CHoCH when they are two
  genuinely different levels; this preserves the requested two-level context.
- `Liquidity` is aggregated per close and direction. The label uses the newest
  older level as its price anchor rather than labeling every consumed level.
- Timed first-period FVG extension intentionally overrides `Extension Bars`.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live MNQ validation are still required.
