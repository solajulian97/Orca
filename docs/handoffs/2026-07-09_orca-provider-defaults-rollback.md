# 2026-07-09 - Orca Provider Defaults Rollback

## Objective

Restore conservative, self-contained defaults for `OrcaRollingProfiles` and `OrcaCumulativeDelta` after the experimental shared-provider rollout coincided with a live MNQ workspace stall. Preserve diagnostics coverage and use the 2026-07-09 RTH-open screenshot as benchmark evidence for later optimization.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaCumulativeDelta.cs`
- `docs/handoffs/2026-07-09_orca-provider-defaults-rollback.md`

## Behavior added, changed, or removed

- New/default `OrcaRollingProfiles` instances use the local tick-series cache instead of requiring `OrcaProfileDataProvider`.
- New/default `OrcaCumulativeDelta` instances use the internal source mode instead of shared historical provider data with internal realtime updates.
- No diagnostics behavior or instrumentation was removed.
- No chart source label was restored; `ShowDataSourceLabel` remains off.

## User-facing settings

- `OrcaRollingProfiles`: `UseSharedProfileDataProvider = false`, `EnableSharedProviderHistoricalBackfill = false`, and `UseLocalTickSeriesCache = true`.
- `OrcaCumulativeDelta`: `OrderFlowSourceMode = Internal`.
- Existing saved chart/template instances can retain serialized provider-backed values and may require manual setting changes.

## Secondary series

- No new `AddDataSeries` calls were added.
- New/default Rolling Profiles instances again declare their existing hidden 1 Tick local-cache series.
- New/default Cumulative Delta instances again declare their existing hidden 1 Tick internal series.
- This rollback favors operational independence over reducing duplicate hidden tick consumers until the shared-provider lifecycle is hardened.

## Tick Replay implications

- No Tick Replay requirement changed.
- The restored internal/local modes retain the existing historical tick-volume and Tick Replay load implications documented for these modules.

## Historical-load implications

- Rolling Profiles no longer waits for provider historical backfill by default.
- Cumulative Delta no longer depends on provider snapshots for historical data by default.
- Local hidden-tick hydration may increase historical workload relative to a healthy shared provider.

## Cache implications

- Default Rolling Profiles returns to its local tick cache; default Cumulative Delta returns to internal state.
- The provider remains available as an explicit opt-in, but the dedicated dummy provider chart should remain out of the live workspace while provider lifecycle and memory bounds are investigated.
- Incident facts: after the MNQ provider experiment, MNQ charts stalled and a stale NinjaTrader process held `Vendor.dll`. Renaming the NinjaTrader chart cache and ending the remaining process restored the workspace. This recovery does not by itself prove the provider was the root cause.

## Rendering implications

- No rendering path changed.
- Existing diagnostics render timing remains active when diagnostics are on.

## Performance implications

- This rollback can restore duplicate hidden 1 Tick consumers for compatible modules, so it is a stability rollback rather than the final optimization.
- The 2026-07-09 RTH-open screenshot showed 59 live diagnostic instances near realtime at about 0.2 seconds lag.
- MNQ hidden-tick consumers were processing roughly 1,500 events per second in several rows. `OrcaTimeStatistics` showed notable sampled render maxima around 19-23 ms; `OrcaMGIDaily` and `OrcaLegtoLegProfile` also showed double-digit sampled render maxima in some rows.
- These are observational rankings from one screenshot. They do not establish a single root cause for the earlier multi-minute MNQ backlog.
- Next optimization should target bounded RTH-open benchmarks and the repeated hidden-tick consumers (`OrcaStepProfile`, `OrcaAbsorptionCandles`, `OrcaRollingProfiles`, and `OrcaLegtoLegProfile`) before retrying a shared-provider default.

## Tests performed in NinjaTrader

- Julian confirmed the workspace recovered after renaming the cache and ending the stale NinjaTrader process.
- Deployed `OrcaRollingProfiles.cs` and `OrcaCumulativeDelta.cs` from Working_Suite to the live NinjaTrader Custom tree.
- Source/live SHA-256 hashes match: Rolling Profiles `01F0214795A9D4353568BF395E40C21C1DD37DFE2826FDB6EB7E7F8A56D9D3D9`; Cumulative Delta `C2F375C1B64366F1118273058E40B5C6368CC02495D722AB20692CCDBDD4FCEB`.
- The rollback itself has not yet been compiled or manually validated.

## Compile status

- Local source checks: `git diff --check` passed for both source files and this handoff.
- NinjaTrader F5 compile: pending Julian validation after deployment.

## Manual-validation status

- Pending.
- After deployment, existing saved instances should be checked explicitly: Rolling Profiles provider off/local cache on, and Cumulative Delta source `Internal`.

## Known issues, risks, and follow-up work

- Saved workspaces/templates may override these defaults.
- Duplicate hidden tick-series pressure remains.
- Provider lifecycle, registration ownership, shutdown behavior, retention bounds, and failure isolation need investigation before another default-on rollout.
- A controlled benchmark should capture pre-open baseline, RTH-open peak, recovery slope, lag, event rates, model time, and render maxima by chart and module.

## Full_Suite promotion eligibility

- Not eligible.
- Promotion requires NinjaTrader F5 compile and Julian's live manual validation.
