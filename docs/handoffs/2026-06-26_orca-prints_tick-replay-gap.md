# 2026-06-26 Orca Prints Tick Replay Gap Incident

## Objective

Capture Julian's chart-specific Tick Replay load/gap report and define the next diagnostic sequence without changing production indicator logic.

## Files Changed

- `docs/ORCA_PRODUCT_STATE.md`
- `docs/handoffs/2026-06-26_orca-prints_tick-replay-gap.md`

## Behavior Added Changed Or Removed

Documentation-only change. No NinjaScript behavior changed.

## User-Facing Settings Added Changed Deprecated Or Removed

None.

## Secondary Series Added Or Changed

None.

Current chart stack has existing tick-level load sources:

- `OrcaPrints`: no `AddDataSeries`, but `Calculate.OnEachTick` and Tick Replay-driven `OnMarketData` Last/Bid/Ask processing.
- `OrcaStepProfile`: hidden `AddDataSeries(BarsPeriodType.Tick, 1)`.
- `OrcaAbsorptionCandles`: hidden `AddDataSeries(BarsPeriodType.Tick, 1)`.
- `OrcaLegtoLegProfile`: default `TradeSourceMode = SecondaryTickSeries`, which adds hidden `AddDataSeries(BarsPeriodType.Tick, 1)`.
- `Orca Time VWAPs`: no secondary series found; primary-series VWAP work only.

## Tick Replay Implications

Tick Replay is required for accurate historical Orca Prints because `OrcaPrints` depends on replayed market-data events. On this chart, enabling Tick Replay also runs alongside three existing hidden 1-tick consumers, so historical work can be multiplied.

`OrcaLegtoLegProfile` has a `TickReplayLastEvents` mode that may be useful for isolating whether one hidden 1-tick series can be removed from this chart stack without losing historical trade-event behavior.

## Historical-Load Implications

A ten-minute load for a one-minute, three-day chart is not acceptable as a product target. Current source evidence makes duplicate hidden tick-series hydration a high-probability performance hypothesis, but this is not confirmed until isolated chart tests or diagnostics identify which component dominates.

The 10:00-10:30 gap must be classified as one of:

- price bars missing from the primary chart series,
- Tick Replay/market-data events absent,
- Orca model state absent despite price bars existing,
- render output absent despite model state existing.

## Cache Implications

`OrcaPrints` can publish shared profile cache data, but this chart stack does not yet prove cache involvement in the gap. No broad cache clear or historical-data reload should be recommended without scoped evidence.

## Rendering Implications

The active `OrcaPrints` dirty diff is visual-only single-print size text. It may add render cost when enabled, but it does not by itself explain a 30-minute historical input gap.

`OrcaStepProfile` and `OrcaLegtoLegProfile` are render-heavy and should be included in render timing once diagnostics exist.

## Performance Implications

The most likely performance pressure is Tick Replay plus multiple hidden 1-tick series on the same instrument/range. Immediate measurement should isolate indicators one at a time before implementing optimizations.

## NinjaTrader Tests Performed

None by Codex in this pass. Runtime observation was reported by Julian.

## Compile Status

Not compiled. Documentation-only change.

## Manual-Validation Status

Not manually validated by Codex. Julian reported the runtime behavior on the active chart.

## Known Issues Risks And Follow-Up Work

- Need to verify whether the 10:00-10:30 gap exists on a clean chart with the same instrument/contract/trading-hours template/Tick Replay setting.
- Need to isolate load time and gap behavior by adding indicators one at a time.
- Need to test `OrcaLegtoLegProfile` with `Trade Source Mode = TickReplayLastEvents` if the deployed build exposes it.
- Need diagnostics instrumentation for lifecycle timings, series maps, market-data event counts, and render samples.

## Promotion Eligibility

Not applicable. No `Full_Suite` promotion should occur for documentation-only incident capture.

## Updated Evidence After Indicator Removal

Julian later removed all indicators from the same one-minute, three-day MNQ Tick Replay chart and reloaded it. The chart loaded quickly, but the approximately 10:00-10:30 gap remained. Julian also reloaded historical data and the gap still remained.

Julian also observed that enabling Tick Replay or reloading history on one MNQ chart appears to cause other open MNQ charts to reload or recalculate, which may explain why the original chart stack took much longer when other MNQ charts and Orca indicators were open.

Updated interpretation:

- The missing 10:00-10:30 bars are now unlikely to be caused by Orca indicator rendering or model state on that chart, because the gap persists with all indicators removed.
- Orca indicators remain a likely contributor to slow calculation when loaded, especially `OrcaStepProfile`, `OrcaAbsorptionCandles`, and `OrcaLegtoLegProfile` due to hidden 1-tick series plus Tick Replay event volume.
- The gap should now be triaged as a NinjaTrader chart instance, chart template, trading-hours template, historical data cache, instrument/contract, or provider/session issue until a brand-new chart proves otherwise.

Next diagnostic sequence:

1. Create a brand-new MNQ one-minute chart, same contract, same trading-hours template, same three-day range, Tick Replay on, no Orca indicators.
2. If the new chart has complete 10:00-10:30 bars, save a screenshot and treat the original chart/template instance as suspect. Rebuild that chart from a clean chart rather than continuing to debug Orca logic.
3. If the new chart has the same gap, test the same contract/time window with Tick Replay off and then a different minute range such as five days. If the gap persists, the issue is likely historical data/session/provider-side rather than Orca.
4. If complete bars return only after closing the other MNQ charts or restarting NinjaTrader, record that as workspace-level reload contention/state behavior.

## Updated Evidence With Tick Replay Off

Julian then loaded the original one-minute chart template with Tick Replay off so the chart would load quickly before restarting NinjaTrader. With Tick Replay off, the same one-minute data loaded complete and the visible 10:00-10:30 gap was no longer apparent.

Updated interpretation:

- Regular one-minute historical data for the window appears to exist.
- The missing region is now most consistent with Tick Replay/tick-level historical data, Tick Replay cache/state, or replay-specific chart construction rather than Orca indicator logic or missing minute bars.
- The next useful comparison is the same chart/template after a full NinjaTrader restart with other MNQ charts closed: Tick Replay off first, then Tick Replay on.

## Bare One-Chart Restart Reproduction

Julian restarted NinjaTrader and tested only one MNQ one-minute chart. With Tick Replay off, all one-minute data loaded correctly. With Tick Replay on, the large gap returned.

Updated conclusion:

- This is now a Tick Replay/tick-level historical replay issue for the affected MNQ contract/window until proven otherwise.
- Regular one-minute historical data is present.
- Orca indicators and the original chart template can still contribute to calculation time, but they are not required to reproduce the missing-bar gap.

Next diagnostic sequence:

1. Confirm the exact MNQ contract, trading-hours template, and date/time of the gap.
2. Test the same contract/window on a 1-tick chart or other Tick Replay-dependent view, if safe, to see whether tick history itself has the same hole.
3. Test a nearby date or wider lookback with Tick Replay on to see whether the problem is isolated to one replay segment.
4. If the replay gap persists, perform only a targeted NinjaTrader historical/tick cache repair for MNQ and the affected date range, not a broad workspace or database reset.

## Confirmed 1-Tick Chart Gap

Julian tested a 1-tick MNQ chart for the same contract/window. The 1-tick chart is also missing data, approximately 9:49 a.m. to 10:41 a.m.

Conclusion:

- The visible gap is now confirmed as a historical tick-data problem for the affected MNQ contract/time range.
- Tick Replay on the one-minute chart exposes that tick-history hole.
- Tick Replay off can still show complete one-minute bars because minute historical data exists separately.
- Orca indicators are not required to reproduce the missing window and should not be modified for this gap.

Recommended recovery path:

1. Record exact contract, date, trading-hours template, and missing window: roughly 9:49 to 10:41.
2. Attempt a targeted historical tick-data redownload/repair for that MNQ contract/date range.
3. Re-test a 1-tick chart first.
4. Only after the 1-tick chart is complete should Tick Replay be retested on the one-minute Orca chart.

## Mixed Bar-Type Result After Tick Download

Julian downloaded historical tick data for MNQ September using Ask, Bid, and Last from June 25 through June 26. After reloading, the missing window still did not appear on the 1-tick chart. The data did appear on 30-second and 5-second charts, but did not appear on the 1-minute chart.

Updated conclusion:

- NinjaTrader is showing a bar-type/data-store inconsistency: second-based historical data appears available, while tick and one-minute paths still show the affected hole.
- This remains outside Orca indicator logic.
- The safest next recovery path is targeted cleanup/redownload for MNQ September Tick and Minute data for June 26 only, leaving Second data and broader databases untouched.

Recommended next test:

1. In Historical Data > Edit, inspect MNQ September for June 26 under Last/Tick and Last/Minute if available.
2. If the 9:49-10:41 hole is visible there, delete only the affected MNQ September June 26 Tick and/or Minute day entries.
3. Redownload Last Tick and Last Minute for June 25 through June 26.
4. Re-test in this order: 1-tick chart, 1-minute Tick Replay off, 1-minute Tick Replay on.
