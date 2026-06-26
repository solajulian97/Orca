# 2026-06-26 Workspace Observatory PM Plan

## Objective

Capture the product direction after the MNQ Tick Replay/cache incident: build a live Orca Diagnostics / Workspace Load Observatory that explains data-source ownership, live-data health, lag, and performance load across the trading workspace.

## Files Changed

- `docs/ORCA_PRODUCT_STATE.md`
- `docs/ORCA_DIAGNOSTICS_SPEC.md`
- `docs/handoffs/2026-06-26_workspace-observatory-pm-plan.md`

## Behavior Added Changed Or Removed

Documentation-only change. No NinjaScript behavior changed.

## User-Facing Settings Added Changed Deprecated Or Removed

None yet. Future implementation should add diagnostics settings that default to Off.

## Secondary Series Added Or Changed

None.

The plan specifically targets visibility into existing secondary-series usage, especially hidden 1-tick paths in profile/prints/order-flow tools.

## Tick Replay Implications

Tick Replay remains required for accurate historical Orca Prints, but the suite needs per-instance reporting of Tick Replay state, replay event volume, last replay/live input timestamp, and whether a module is using Tick Replay Last events versus hidden secondary tick series.

## Historical-Load Implications

The cache incident showed historical rows can exist while chart/bar cache state is stale. Diagnostics should distinguish historical database availability, NinjaTrader chart/bar cache state, Orca cache/model state, and render state.

## Cache Implications

Successful recovery was a reversible `db\cache` rename. Future diagnostics should flag suspected stale chart/cache conditions and guide users toward cache rebuild before destructive historical-data deletion.

## Rendering Implications

Rendering should be observed through sampled render timing and snapshot age. No profile calculation, cache access, or heavy allocation should be added to render paths.

## Performance Implications

RTH open and news spikes on MNQ are priority scenarios. The observatory must identify which modules are falling behind realtime and whether the bottleneck is data events, hidden series, cache contention, profile/model rebuilds, or rendering.

## NinjaTrader Tests Performed

Julian reported the real workspace loaded and functioned correctly after cache rebuild. No Codex runtime test was performed.

## Compile Status

Not compiled. Documentation-only PM plan.

## Manual-Validation Status

Runtime validation reported by Julian for the cache fix and workspace reload. The observatory is not yet implemented.

## Known Issues Risks And Follow-Up Work

- Need Phase 1 diagnostics core and a low-overhead live status registry.
- Need an internal AddOn window under Tools > Orca Diagnostics.
- Need per-indicator source declarations before optimization decisions.
- Need measured evidence before consolidating data-source paths.
- Need to evaluate shared provider strategy for Prints, Step Profile, Absorption Candles, Leg-to-Leg Profile, Rolling Profiles, Visible Range, Cumulative Delta, and Time Statistics.

## Promotion Eligibility

Not applicable. No `Full_Suite` promotion for documentation-only planning.
