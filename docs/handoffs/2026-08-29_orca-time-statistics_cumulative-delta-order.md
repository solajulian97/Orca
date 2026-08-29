# Orca Time Statistics Cumulative Delta Order

## Objective

Place Cumulative Delta after Finish Delta in the Time Statistics row layout so it is the last enabled delta-oriented row before price-action and time rows.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs`
- `docs/handoffs/2026-08-29_orca-time-statistics_cumulative-delta-order.md`

## Behavior added, changed, or removed

- Row construction now places Delta Percent, optional Max/Min Delta, and Finish Delta before Cumulative Delta.
- With the rows shown in Julian's chart, the visible order is Delta, Delta Percent, Finish Delta, and Cumulative Delta.
- Delta Per Second remains immediately after Delta when enabled.
- Cumulative-delta calculations, reset modes, averages, colors, and visibility settings are unchanged.
- Restored the prior close-stamped RTH boundary and selectable font-weight implementation that had been removed from the dirty working copy while preserving the existing diagnostics edits.

## User-facing settings added, changed, deprecated, or removed

- Reordered Rows settings to match the rendered delta sequence: Delta, Delta Per Second, Delta Percent, Max Delta, Min Delta, Finish Delta, Cumulative Delta, then Cumulative Delta Start.
- Restored Font Weight below Font Family in the Visual group. Available values are Light, Regular, Medium, SemiBold, Bold, and ExtraBold; Bold remains the default.

## Secondary series added or changed

- No secondary series were added or changed.

## Tick Replay implications

- No Tick Replay behavior changed.

## Historical-load implications

- No historical-load behavior changed.
- The restored RTH comparison preserves the close-stamped rule: a bar stamped exactly 9:30 AM belongs to the prior RTH anchor, and the first bar after 9:30 AM starts the new RTH cumulative period.

## Cache implications

- No cache behavior changed.

## Rendering implications

- Only row display order changes in rendering; no new rendering pass or resource is created.
- Existing DirectX render-target tracking and guarded Bar access helpers were preserved.
- The restored text font-weight resolver is applied when DirectWrite resources are created.

## Performance implications

- Row construction remains a small per-render list build with the same number of entries.
- No allocations, subscriptions, cache reads, polling, or synchronization were added.

## Tests performed in NinjaTrader

- Pending Julian's F5 compile and chart validation.

## Static tests performed

- `git diff --check` passed for the scoped source; only the existing line-ending conversion warning was reported.
- Confirmed one NinjaScript generated-code region remains.
- Confirmed the row list places Cumulative Delta immediately after Finish Delta.
- Confirmed the strict RTH boundary and Font Weight setting/resolver are present.
- Confirmed direct `Bars.Get*` access remains in the existing guarded helper methods rather than a new startup or render path.

## Compile status

- NinjaTrader F5 compile pending after deployment.

## Deployment status

- Targeted deployment completed with `deploy_orca.ps1 -Target OrcaTimeStatistics`.
- Live destination: `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaTimeStatistics.cs`.

## Manual-validation status

- Pending validation that Cumulative Delta appears below Finish Delta on the chart.
- Confirm the shown setup renders Delta, Delta Percent, Finish Delta, then Cumulative Delta.
- Recheck the 9:35 AM RTH reset on a five-minute chart and a Font Weight selection after F5.

## Known issues, risks, and follow-up work

- The dirty Working_Suite source contains pre-existing diagnostics edits from another task; they were preserved and not changed as part of the row-order logic.
- Non-time BarsTypes should continue to be checked separately for close-stamped RTH-boundary expectations.

## Promotion eligibility

- Not eligible for promotion to `Full_Suite` until NinjaTrader compiles and Julian confirms the live row order, RTH reset, and font-weight rendering.
