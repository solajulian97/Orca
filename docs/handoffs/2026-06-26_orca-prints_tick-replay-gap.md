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