# 2026-06-25 Suite Diagnostics First Pass

## Objective

Establish durable project coordination docs, inspect the current Working_Suite source, map hidden/secondary series and performance-sensitive patterns, and create a file-aware diagnostics plan for startup, historical-load, Tick Replay, cache, and render issues.

## Files Changed

- `AGENTS.md`
- `docs/ORCA_PRODUCT_STATE.md`
- `docs/ORCA_ARCHITECTURE.md`
- `docs/ORCA_DIAGNOSTICS_SPEC.md`
- `docs/ORCA_AGENT_HANDOFF_TEMPLATE.md`
- `docs/ORCA_DECISIONS.md`
- `docs/indicators/ORCA_SOURCE_MAP.md`
- `docs/handoffs/2026-06-25_suite_diagnostics-first-pass.md`

## Behavior Added Changed Or Removed

Documentation-only change. No production NinjaScript behavior changed.

## User-Facing Settings Added Changed Deprecated Or Removed

None.

## Secondary Series Added Or Changed

None.

Audit found existing hidden/secondary series in:

- 1 Tick: `OrcaAbsorptionCandles`, `OrcaCumulativeDelta`, `OrcaCandleVolumeProfile`, `OrcaLegtoLegProfile`, `OrcaProfileDataProvider`, `OrcaRollingProfiles`, `OrcaStepProfile`, `OrcaTickDirectionIndex`, `OrcaVisibleRangeVolumeProfile`.
- 1 Minute and 30 Second: `OrcaMGIDaily`.
- 30 Second: `OrcaSessionContextMap`.
- Built from Tick: `OrcaAtrAdaptiveRangeBarsType`.

## Tick Replay Implications

No code changed. Audit indicates Tick Replay must be measured per component, especially for tools using hidden 1-tick series or `Calculate.OnEachTick`.

## Historical-Load Implications

No code changed. Audit indicates historical load risk is concentrated in hidden tick-series hydration, shared provider startup/registration, local/shared cache availability, and render-triggered profile rebuilds.

Current root-cause hypotheses are documented in docs/ORCA_PRODUCT_STATE.md. They remain hypotheses until startup diagnostics or controlled benchmarks tie them to measured chart instances.

## Cache Implications

No code changed. Existing shared cache/service path is `OrcaProfileDataCache` in `OrcaVolumeProfileCore.cs`, with `OrcaProfileDataProvider` as the main shared producer.

## Rendering Implications

No code changed. Render-heavy files were mapped for future render sampling and snapshot-age diagnostics.

## Performance Implications

No runtime performance impact expected from docs. Future Phase 1 instrumentation must be disabled by default and use cheap branches in `Off` mode.

## NinjaTrader Tests Performed

None. This pass did not deploy or compile in NinjaTrader.

## Compile Status

Not compiled. Documentation-only change.

## Manual-Validation Status

Not manually validated in NinjaTrader. Not required for documentation-only first pass.

## Known Issues Risks And Follow-Up Work

- Dirty worktree existed before this pass in several Working_Suite files.
- No reproducible startup benchmark has been captured yet.
- No diagnostics core exists yet.
- The 1 Tick utilization entry has multiple plausible sources; root cause is not confirmed.
- The root-cause hypothesis matrix is documentation-only and must be validated with Phase 1 diagnostics or manual benchmark evidence.

## Promotion Eligibility

Not applicable. No `Full_Suite` promotion should occur for this documentation-only pass.
