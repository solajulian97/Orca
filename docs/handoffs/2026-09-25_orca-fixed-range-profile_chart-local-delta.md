# 2026-09-25 - Orca Fixed Range Profile Chart-Local Delta

## Objective

Keep chart-local true volume at price, and fill ask−bid delta from Tick Replay, live prints, or a secondary tick series without replacing that volume with a bar-range estimate.

## Files changed

- `Orca Trades/Working_Suite/DrawingTools/OrcaFixedRangeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaVolumeProfileCore.cs`
- `docs/handoffs/2026-09-25_orca-fixed-range-profile_chart-local-delta.md`

## Behavior added, changed, or removed

- `572e1cf` could paint `BuildFixedRangeFromBars` (even bar-range slices, the choppy flat histogram) and then keep it. `TrySnapshot` / `GetBestSource` ranks publishers by map coverage, so a source that is empty in the selected bars wins over the Tick Replay, live, or secondary-tick map that has them. The fallback cache key was `estimated-bars` at revision `-1`, so a later cache fill did not rebuild. Missing ask/bid also left `volumeOk` false and took the same fallback.
- Chart-local volume now comes from `TrySnapshotBestInRange`: the publisher with the most volume inside the selected bars. That profile is kept when some prices have no bid/ask split.
- Delta is ask minus bid only for prices that have a split. Those prices fill the histogram, `D`, `FD`, and the percent. A price with true volume and no split stays in the volume profile and adds nothing to delta.
- The bar-range estimate runs only when no chart-local true volume was captured. Its revision follows the publisher, so data that arrives after the first paint replaces the estimate.
- Bar-direction delta (`close >= open ? volume : -volume`) stays removed.
- Delta is now ask minus bid from the first source that has that split inside the selected price range:
  1. The chart-local volume snapshot, with bars that have no ask and no bid left empty.
  2. `OrcaProfileDataCache.TrySnapshotPreferDelta` on the chart key, then the instrument-and-period key. This picks the same-chart publisher with the most ask+bid in the selected bars (Tick Replay cache, candle VAP, or prints).
  3. Volumetric bar ask/bid volume, when the bars type stores it.
  4. Master order-flow buckets, only when the paths above have no ask or bid.
- Row volume on the delta build is ask+bid. Unclassified remainder is not assigned to a side.
- A bar or bucket with no ask, no bid, and no delta is skipped. It does not clear the rest of the profile and it does not freeze finish delta at 0.
- Bar-direction delta (`close >= open ? volume : -volume`) stays removed. Estimated volume may still draw when fallback is on, with the up/down split cleared.
- The statistics box stays outside the selected range.

## User-facing settings added, changed, deprecated, or removed

None. Data Mode, True Data Source, and the show-profile switches are unchanged.

## Secondary series added or changed, including Tick, Second, Bid, Ask, Last, Volumetric, or custom series

None. The drawing tool still adds no series. Volumetric bid/ask is read from the chart bars only when that bars type already stores it.

## Tick Replay implications

- Tick Replay still reaches this drawing tool only through a same-chart publisher. Ask is that publisher's up map. Bid is the down map.
- `TrySnapshotPreferDelta` is the read used when the coverage-ranked volume source has no ask/bid and another publisher on the same chart key does.
- A 200-volume series still does not store bid/ask volume on the bars themselves.

## Historical-load implications

- No new historical request. The profile still rebuilds on range, bar count, last-bar volume, and the selected source revision.
- Order-flow buckets are read only when no chart-local or volumetric snapshot has ask or bid in the range.

## Cache implications

- `OrcaProfileDataCache.TrySnapshot` and `GetBestSource` are unchanged, so other indicators still take the coverage-ranked volume source.
- `TrySnapshotBestInRange` and `TryGetMaxRevision` are new reads. They copy the source list under the cache lock, release it, then measure under each source lock. They do not register a source.
- `TrySnapshotPreferDelta` is still the ask/bid read when the volume winner has no split.

## Rendering implications

- The delta histogram is drawn from the ask−bid snapshot. Bars without a split add no row.
- Estimated volume rows stay on the neutral color outside the value area.
- Statistics placement is unchanged: 6px above the range for Top Left and Top Right, 6px below for Bottom Left and Bottom Right.

## Performance implications

- The extra snapshot runs only when the volume source has no in-range ask or bid, and only on profile rebuild, not on every render frame.
- No new secondary series and no full historical rebuild on the per-tick path.

## Tests performed in NinjaTrader

None. This workspace cannot see Julian's live NinjaTrader folder. Not deployed.

## Compile status

Not compiled in NinjaTrader here. `git diff --check` was run on the edited source.

## Manual-validation status

Pending Julian. Not validated.

Suggested check on NQ DEC26 200 Volume, Tick Replay on, Data Mode TrueVolumeAtPrice, True Data Source ChartLocalOnly, delta profile and statistics on:

1. With the local Tick Replay cache, candle VAP, or prints publishing ask/bid, the delta histogram and `D`, `FD`, and delta percent should appear. The step should be ask−bid, not 200 per bar.
2. A bar with no bid/ask should drop out. Neighboring bars that have a split should still draw.
3. The statistics box should stay outside the range.

## Known issues, risks, and follow-up work

- If no same-chart publisher has recorded ask/bid and the bars are not volumetric, there is still no real delta to draw. The tool does not invent one from bar direction.
- Order-flow buckets are the last resort, including when True Data Source is Chart Local Only and the local maps have no ask/bid. The source label switches to that delta source when the volume snapshot did not supply the split.
- Not eligible for `Full_Suite` until Julian compiles and confirms the chart.

## Full_Suite promotion eligibility

Not eligible. `Full_Suite` was not modified.
