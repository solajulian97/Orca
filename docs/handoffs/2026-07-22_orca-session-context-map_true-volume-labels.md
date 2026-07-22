# OrcaSessionContextMap True Volume And Label Cleanup - 2026-07-22

## Objective

Replace the session context volume-profile feed with true traded-at-price volume and reduce misleading right-edge label clutter seen on higher-timeframe charts.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaSessionContextMap.cs`
- `docs/ORCA_ARCHITECTURE.md`
- `docs/indicators/ORCA_SOURCE_MAP.md`
- `docs/handoffs/2026-07-22_orca-session-context-map_true-volume-labels.md`

## Behavior Added, Changed, Or Removed

- Added a hidden 1-tick series when `Show Session Volume Profile` is enabled.
- Removed the prior profile fallback that spread a primary bar's volume evenly across every tick between that bar's high and low.
- Session profile rows now update only from tick-series trade price and volume.
- Session cumulative volume and session VWAP use tick-series trade price/volume when the true-volume series is enabled.
- Primary bars still maintain session OHLC, open-location classification, trend/balance classification, sweeps, reclaims, and acceptance logic.
- POC/VA profile lines still render, but profile POC labels no longer add extra right-edge text.
- Carry-forward and projection lines still render when enabled, but their labels are suppressed to avoid a crowded right margin.
- Remaining labels keep their price-level Y coordinate and use horizontal lanes for nearby collisions instead of being vertically shifted away from their lines.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

- No new settings were added.
- Existing `Show Session Volume Profile` now also controls whether the true-volume hidden 1-tick series is added.
- Existing `Show Labels` still controls session/event labels globally, but carry-forward/projection/profile-POC labels are intentionally not emitted by the renderer.

## Secondary Series Added Or Changed

- Existing: `AddDataSeries(BarsPeriodType.Second, 30)` remains for the 30-second opening range.
- Added: `AddDataSeries(BarsPeriodType.Tick, 1)` when `Show Session Volume Profile` is enabled.
- No Bid, Ask, Volumetric, or custom bars series were added.

## Tick Replay Implications

- The session volume profile no longer depends on Tick Replay to avoid bar-estimated volume; the hidden 1-tick series supplies historical trade price/volume for the profile.
- Delta classification still depends on available quote context for Bid/Ask-style classification; without quote replay it falls back to tick-direction style classification.
- This does not change the existing 30-second opening range behavior.

## Historical-Load Implications

- Historical profile loading now processes a hidden 1-tick series when the session profile is enabled, so load cost can increase compared with the previous primary-bar estimate.
- `Max Historical Days` continues to bound primary, 30-second, and true-volume tick processing.
- The visible profile should now reflect actual historical tick volume-at-price instead of synthetic bar-range distribution.

## Cache Implications

- No new shared cache was introduced.
- `OrcaProfileDataCache` is still not consumed by `OrcaSessionContextMap`; the new true-volume path is local to this indicator.
- No render-time cache reads were added.

## Rendering Implications

- Profile rows are still rendered from precomputed `ProfileRows` snapshots; the feed changed from primary-bar estimates to tick-price rows.
- Existing dynamic aggregation still compresses rows for dense/zoomed-out views.
- Carry-forward/projection labels are no longer added to `pendingLabels`.
- Pending labels preserve price-level alignment and fan horizontally for same/nearby Y buckets.

## Performance Implications

- The hidden 1-tick series is an intentional extra historical/live data stream for correctness: only true traded-at-price volume is used for session profiles.
- No full profile rebuilds were added to per-tick or render paths; tick updates mutate only the active session row and summary.
- OnRender still performs the pre-existing render-row aggregation from the current in-memory profile rows.

## Tests Performed In NinjaTrader

- Not yet performed. Julian needs to compile with NinjaTrader `F5` and inspect a live/historical chart.

## Compile Status

- Pending live NinjaTrader compile.
- Static/local checks will be recorded in the task response separately.

## Manual-Validation Status

- Pending Julian validation on the MNQ/ES chart.
- Suggested validation: apply the indicator on a 5- or 10-minute chart, confirm the session profile no longer has uniform bar-range blocks, confirm the 30-second opening range still appears, and confirm right-edge labels stay aligned to their price levels.

## Known Issues, Risks, And Follow-Up Work

- The new tick series increases load when session profiles are enabled.
- The indicator still does not use the shared `OrcaProfileDataProvider`; future work could route through the shared provider if duplicate 1-tick series become a performance issue.
- Bid/Ask delta precision still depends on quote availability; this change fixes true volume-at-price, not strict historical Bid/Ask classification.
- Existing chart templates may retain previous user settings such as high lookback days, carry-forward lines, or range projections.

## Promotion Eligibility

- Not eligible for promotion to `Full_Suite` until Julian completes NinjaTrader compile and manual chart validation.