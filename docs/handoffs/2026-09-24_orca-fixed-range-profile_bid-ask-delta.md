# 2026-09-24 - Orca Fixed Range Profile Bid/Ask Delta And Outside Statistics

## Objective

Stop the fixed-range statistics line from reporting bar volume as delta, and move the statistics box outside the selected range so it does not cover the profile or the trend line.

## Files changed

- `Orca Trades/Working_Suite/DrawingTools/OrcaFixedRangeProfile.cs`
- `docs/handoffs/2026-09-24_orca-fixed-range-profile_bid-ask-delta.md`

## Behavior added, changed, or removed

- Total delta (`D`), finish delta (`FD`), and delta percent are taken only from classified bid/ask volume.
- Removed the estimated finish-delta path that used `close >= open ? bar volume : -bar volume`. On a 200-volume chart that path stepped by 200.
- The delta histogram is no longer built with that same up-bar/down-bar split. It is drawn only from classified bid/ask.
- Classified sources, in order:
  1. Existing per-bar up/down price maps (chart-local Tick Replay cache, candle VAP, prints, or master order-flow ask/bid).
  2. Volumetric bar ask/bid volume when the chart bars expose `Volumes`, `GetAskVolumeForPrice` / `GetBidVolumeForPrice`, or `TotalBuyingVolume` / `TotalSellingVolume`.
- If neither source has bid/ask volume, `D`, `FD`, and the delta percent are omitted. The delta histogram is cleared. Points and duration still render.
- Estimated volume shape can still come from chart bar volume when true volume-at-price data is missing and fallback is enabled. That estimate no longer keeps an up/down split, so those rows are not painted as directional delta.
- Statistics position still uses Top Left, Top Right, Bottom Left, and Bottom Right. Top positions sit above the selected range. Bottom positions sit below it. The box is not hidden.

## User-facing settings added, changed, deprecated, or removed

- No new settings.
- `Fallback To Chart Estimate` description now says delta is included only when bid/ask volume is available.
- `Statistics Position` choices are unchanged. Their placement is outside the selected range.

## Secondary series added or changed, including Tick, Second, Bid, Ask, Last, Volumetric, or custom series

None. The drawing tool still adds no series. Volumetric bid/ask is read from the chart's existing bars type when that type already stores it.

## Tick Replay implications

- Tick Replay price maps from `OrcaFixedRangeProfileDataCache`, candle volume profile, or prints remain the first delta source. This does not replace that August 29 path.
- A standard volume-bar series does not store bid/ask volume. Tick Replay delivers bid/ask prices to indicators through market-data events. Those events are available to this drawing tool only after a same-chart publisher has recorded them.
- When the bars themselves are volumetric and already contain ask/bid volume, the tool reads that volume directly and does not require the companion cache.

## Historical-load implications

- No new historical request. Profile rebuild still follows the existing range, bar-count, and last-bar-volume invalidation.
- Volumetric price levels are read only while rebuilding the selected range, not on every render.

## Cache implications

- No cache format change. Existing `OrcaProfileDataCache` snapshots are still preferred when they contain classified up or down volume.
- The removed estimate path no longer writes a fake delta into the statistics line when the cache is missing.

## Rendering implications

- The statistics box is placed outside the anchor rectangle: 6px above it for Top Left and Top Right, 6px below it for Bottom Left and Bottom Right. Left and right alignment still follow the selected corner.
- The box is shifted back into the chart panel only when that shift does not cover the selected range. If the range is already against the panel edge, the box stays outside the range.
- Unclassified estimated volume rows use the neutral delta color outside the value area instead of green/red bar-direction colors. Value-area and POC coloring are unchanged.
- The delta histogram is empty when no bid/ask delta exists.

## Performance implications

- Non-volumetric charts do one bars-type check and then skip volumetric reads.
- Volumetric charts walk tick prices inside the selected bars only when the profile cache rebuilds. That walk is not on the per-frame path after the range is cached.
- No new secondary series, full historical rebuild, or render-path allocation beyond the existing profile rebuild.

## Tests performed in NinjaTrader

None. This workspace cannot see Julian's live NinjaTrader folder, and the self-hosted worker is offline. Not deployed.

## Compile status

- `git diff --check` passed for the edited source.
- NinjaTrader F5 on the first push failed with CS0165: `finishDelta` was unassigned when `hasRealDelta` skipped `TryComputeFinishDelta`. It is now initialized to `0` before that call. A real finish delta still replaces `0` only when the bid/ask path returns one. Bar-direction delta was not restored.
- NinjaTrader F5 after that initialization: not run here.

## Manual-validation status

Pending Julian. Not validated.

Suggested checks on the 200-volume Tick Replay chart:

1. With a local Tick Replay publisher (cache, candle volume profile, or prints), `D` and `FD` should change by real ask-minus-bid size, not by exactly 200 per bar.
2. On volumetric bars, `D` should match ask volume minus bid volume inside the box. The source label should read `Source: volumetric bid/ask` when that is the source.
3. On a plain volume chart with no publisher and no volumetric bid/ask, the line should keep points and duration and should omit `D`, `FD`, and the delta percent.
4. Top statistics positions should sit above the range. Bottom positions should sit below it. The profile and trend line should remain visible.

## Known issues, risks, and follow-up work

- The 2026-09-15 live sync (`8573050`) recorded the installed NinjaTrader Custom tree as the source of truth and copied `OrcaFixedRangeProfile` from that live tree into `Working_Suite`. No later handoff says live NinjaTrader diverged again after that commit. This machine cannot compare the current live folder.
- A 200-volume chart with Tick Replay on still will not show delta until either a same-chart bid/ask publisher is loaded or the bars type itself stores ask/bid volume. The drawing tool cannot receive Tick Replay market-data events on its own.
- Volumetric access uses the public `Volumes` / ask-bid members. If a NinjaTrader build names those members differently, the tool skips them instead of falling back to bar volume.
- When per-price volumetric reads return nothing but bar `TotalBuyingVolume` / `TotalSellingVolume` exist, the bar totals are spread across the overlap with the selected price range. That preserves ask-minus-bid size and is not a bar-direction guess.
- Finish delta still needs more than one chronological bar, or master order-flow buckets. A single collapsed map does not invent finish delta from the flat total.
- Not eligible for `Full_Suite` until Julian compiles and confirms the chart.

## Full_Suite promotion eligibility

Not eligible. `Full_Suite` was not modified.
