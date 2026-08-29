# 2026-08-29 - Orca Fixed Range Profile Local Tick Replay Cache

## Objective

Make `OrcaFixedRangeProfile` independent of the master provider by default on Tick Replay charts. Prefer existing same-chart true volume-at-price maps and provide a lightweight same-chart recorder for bare charts.

## Files changed

- `Orca Trades/Working_Suite/DrawingTools/OrcaFixedRangeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaFixedRangeProfileDataCache.cs`
- `docs/handoffs/2026-08-29_orca-fixed-range-profile_local-tick-replay-cache.md`

## Behavior added, changed, or removed

- Fixed Range now defaults `True Data Source` to `Chart Local Only`.
- In the default mode it reads same-chart Orca profile maps before any master path and never queries or waits for `OrcaProfileDataProvider`.
- Existing `OrcaCandleVolumeProfile` maps remain the first zero-extra-work local source when that indicator is already on the chart and publishes its shared profile cache.
- Added `OrcaFixedRangeProfileDataCache`, an invisible source indicator for charts without Candle Volume Profile, Orca Prints, or another local publisher.
- The cache indicator publishes volume, buy-side volume, and sell-side volume by primary bar and price to the existing `OrcaProfileDataCache`; Fixed Range continues to use the existing `OrcaVolumeProfileCore.BuildFixedRangeFromPriceMaps` engine.
- Source labels now identify local candle VAP, local Tick Replay cache, or local secondary tick cache. Missing local data is labeled as a local-cache condition rather than `no master`.

## User-facing settings

`OrcaFixedRangeProfile`:

- Added `True Data Source`: `Chart Local Only` (default), `Chart Local Then Master`, and `Master Then Chart Local`.

`OrcaFixedRangeProfileDataCache`:

- Added `Trade Source Mode`: `Tick Replay Last Events` (default) and `Secondary Tick Series`.

## Secondary series

- `OrcaFixedRangeProfile` remains a DrawingTool and adds no series.
- `OrcaFixedRangeProfileDataCache` uses no secondary series in its default `Tick Replay Last Events` mode.
- Its optional `Secondary Tick Series` mode adds one hidden `Tick 1 Last` series.

## Tick Replay implications

- With chart Tick Replay enabled, `Tick Replay Last Events` reads each replayed Last event and the Bid/Ask values attached to that event. It uses tick-direction only when the event cannot be classified at Bid or Ask.
- Tick Replay Last Events is the intended accurate historical path and avoids another hidden `Tick 1` series.
- Without Tick Replay, the cache warns that historical Last events are unavailable. It can still collect real-time Last events going forward.

## Historical-load implications

- The cache creates sparse per-primary-bar price maps while NinjaTrader replays historical Last events.
- It does not request a cross-chart backfill or persistent storage, so it cannot populate data before the chart's own loaded Tick Replay window.
- The optional secondary path uses NinjaTrader's hidden Tick 1 hydration instead.

## Cache implications

- Uses the existing static `OrcaProfileDataCache` chart keys and data-source arbitration.
- Does not register an order-flow master source, persist data, or modify `OrcaProfileDataProvider`.
- The recorder maintains only chart-local volume/up/down maps and a monotonic revision for Fixed Range cache invalidation.

## Rendering implications

- Fixed Range rendering style and profile engine are unchanged.
- The source choice rebuilds its cached profile rows only when source revision, region, row settings, or range bars change.
- No DirectX resources, render-target ownership, or row layout behavior changed.

## Performance implications

- Default local Tick Replay mode adds no hidden Tick 1 secondary series.
- One dictionary update per replayed/live Last event is performed off the DrawingTool render path.
- Cache coverage is maintained as an incrementing counter; source selection does not scan all bars to determine coverage.
- This removes master registration timing from the normal Fixed Range path but does not remove NinjaTrader's intrinsic Tick Replay replay cost.

## Tests performed in NinjaTrader

- Targeted deployment completed from `Working_Suite`:
  - `OrcaFixedRangeProfile.cs` to `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\DrawingTools\OrcaFixedRangeProfile.cs`.
  - `OrcaFixedRangeProfileDataCache.cs` to `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaFixedRangeProfileDataCache.cs`.
- Newline-normalized authored-region parity passed:
  - Fixed Range Profile: `1C139104DF81210899CFF4E1FBE0BD7A9C8575FA2ADBB53F7FBF31D56E99D3C2`.
  - Fixed Range Profile Data Cache: `65C788234F7213796C3D0AD88E5E6E994500E3A3C2583FA22BA9325E8EB5678C`.
- NinjaTrader regenerated the cache indicator's standard property wrapper after deployment. This is expected and its authored region matches `Working_Suite`.
- NinjaTrader F5 compile and chart behavior validation remain pending.

## Compile status

- `git diff --check` passed for the changed tracked source before handoff creation.
- Structural brace checks passed for both source files.
- NinjaTrader F5 compile: pending.

## Manual-validation status

Pending Julian validation on an ES Tick Replay chart:

1. With `OrcaCandleVolumeProfile` publishing its shared cache, Fixed Range should read `Source: local candle VAP` without adding the new cache indicator.
2. On a bare Tick Replay chart, add `OrcaFixedRangeProfileDataCache` with `Tick Replay Last Events`; Fixed Range should read `Source: local Tick Replay cache`.
3. Confirm `Chart Local Only` never emits a master wait/no-master label.
4. Compare an identical selected range against Candle Volume Profile/Step Profile volume and Delta rows.
5. Change the recorder to `Secondary Tick Series`, reload, and confirm `Source: local secondary tick cache`.

## Known issues, risks, and follow-up work

- A NinjaTrader DrawingTool cannot call `AddDataSeries` or receive `OnMarketData`; the companion cache is required only on charts that do not already publish local Orca VAP maps.
- Tick Replay fidelity remains dependent on the connected provider's historical Last/Bid/Ask data.
- Saved Fixed Range drawing templates may require an indicator reload to expose the new `True Data Source` property.
- The existing `OrcaProfileDataProvider.cs` has unrelated diagnostics work in the dirty tree and was intentionally not modified.

## Full_Suite promotion eligibility

Not eligible. Requires targeted deployment, NinjaTrader F5 compile, and Julian's chart validation before promotion.
