# Orca Price Action - Sweep Label Episodes

Date: 2026-08-17

## Objective

Replace stacked `Sweep` words from repeated nearby sweeps with one visible
latest-wins label per same-side sweep episode.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaPriceAction.cs`
- `docs/indicators/ORCA_PRICE_ACTION.md`
- This handoff

No `Full_Suite` file was changed.

## Behavior added, changed, or removed

- High-side and low-side sweep labels are grouped independently.
- A same-side resweep within the rolling bar window replaces the pending label
  rather than adding another visible word.
- The final visible label is centered on the latest resweep candle at its actual
  high or low wick extreme, not at the swept pivot's older price.
- A gap beyond the configured window starts a new episode.
- A bullish close-confirmed BOS/CHoCH ends the pending high-side sweep episode;
  a bearish break ends the pending low-side episode.
- Every underlying sweep event remains stored and available to rejection-block
  qualification. Consolidation is render-publication behavior only.

## User-facing settings added, changed, deprecated, or removed

- Added `Market Structure > Sweep Label Window Bars`.
- Default: `5` bars.
- Allowed range: `1` through `100` bars.

No existing sweep, structure, or rejection setting was removed.

## Secondary series added or changed

None. Orca Price Action remains primary-series-only and adds no Tick, Second,
Bid, Ask, Last, Volumetric, or custom series.

## Tick Replay implications

None. Tick Replay remains unnecessary. Sweep detection still uses completed
primary OHLC.

## Historical-load implications

The detector replays the same sweep events over the same 3,000-bar discovery
window. Historical snapshots intentionally contain fewer visible sweep labels.

## Cache implications

None. No cache, data provider, shared service, or background work was added or
changed.

## Rendering implications

Snapshot publication holds at most one pending label per sweep direction while
walking the existing chronological structure-event list. Published labels use
immutable bar/price/text geometry and the existing centered DirectWrite format.
`OnRender` remains collection-free and mutation-free.

## Performance implications

The change removes the former ATR/price-distance calculation for every sweep
label and reduces text geometry/draw calls in resweep sequences. It adds two
temporary model references during snapshot publication; no persistent cache or
per-bar detector scan was added.

## Tests performed in NinjaTrader

Pre-change MNQ screenshot review confirmed several same-side `Sweep` words
stacked across consecutive candles and pivot prices.

No post-change NinjaTrader chart test has been performed yet.

## Compile status

- Local .NET Framework semantic compilation against the installed NinjaTrader
  assemblies and deployed `NinjaTrader.Custom.dll`: passed with no errors.
- Deterministic latest-wins truth table: 3/3 passed for a continuous resweep
  chain, a window-separated pair, and two rolling episodes.
- Static assertions passed for high/low wick anchoring, centered placement,
  bullish/bearish break reset, the public setting/default, no secondary series,
  and no `OnMarketData`.
- `git diff --check`: passed for the three owned files.
- NinjaTrader `F5`: pending after targeted deployment.

## Manual-validation status

Pending. After F5, reload the screenshot area and confirm one label follows the
latest same-side resweep inside five bars, separate high/low episodes can coexist,
and a new label appears after a window gap or close-confirmed break.

## Known issues, risks, and follow-up work

- Rolling grouping is intentionally time-based rather than price-distance-based.
  Distinct same-side pivots swept within five bars share one visible label for
  readability, while their detector events remain separate.
- A long resweep chain can continue beyond five total bars when every successive
  resweep arrives within five bars of the previous one; this is the intended
  rolling-window behavior.
- Live calibration may justify a different default window.

## Promotion eligibility

Not eligible for `Full_Suite`. Targeted deployment, NinjaTrader F5 compilation,
and Julian's live sweep-label confirmation are required first.
