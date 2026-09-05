# Provider platform probe — 2026-09-05

## Objective

Exercise the bounded provider ingestion core with actual NinjaTrader callbacks before registering shared production sources or migrating consumers. This is an opt-in test indicator, not the completed provider or a measured performance improvement.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderProbe.cs`
- `tests/OrcaProvider.PlatformCheck/OrcaProvider.PlatformCheck.csproj`
- `tests/OrcaProvider.PlatformCheck/Program.cs`
- This handoff.

The four previously committed provider core files are dependencies, unchanged in this slice.

## Behavior and settings

- Adding `OrcaProviderProbe` explicitly creates one private registry, publisher, reader and ingestion classifier. Existing indicators cannot discover or consume this registry.
- Only `OnMarketData` Last events enter ingestion. `OnBarUpdate` observes the native first-session-bar flag to reset classification; no hard-coded 18:00 boundary.
- Primary Tick Replay enables historical-pass ingestion. Realtime transition records the sequence boundary, not proof of complete requested UTC history. Without Tick Replay, the probe is live-only.
- Unspecified event timestamps use the platform timezone; ambiguous/invalid local timestamps fail closed. Quotes supplied by NinjaTrader are not proof of exchange-authenticated Bid/Ask provenance.
- Any observed price-connection loss/disconnect during realtime faults this private probe. No automatic recovery or inferred connection ownership.
- Termination disposes reader, publisher and registry and unregisters diagnostics. No manual subscriptions, timers or static registry references.
- No user-facing settings added beyond the explicit choice to add the indicator. Internal capacity is 100,000 events, one reader and one outstanding one-event status batch.

## Series, history, cache and rendering

- No `AddDataSeries`: no additional Tick, Second, Bid, Ask, Last, Volumetric or custom series.
- `Calculate.OnEachTick` and `OnMarketData` are used only by the opt-in probe; existing indicator defaults and Tick Replay settings are unchanged.
- Historical loading adds a bounded classification/append pass over the existing primary replay callbacks. It does not reduce NinjaTrader's native Tick Replay loading.
- No persistent cache or database access. Ring eviction is explicit; full-pass-retained becomes false when historical events leave retention. No implied complete-history availability.
- No `OnRender`, drawing output or trading functionality. No consumer migration or changes to Execution Lines.

## Performance and diagnostics

- Approximately 4 MB event payload capacity per probe, plus object/registry overhead; not a process-wide memory claim.
- Reports historical/live event counts, retained/evicted events, historical-pass completion and retention. Status reads/copies at most one event and releases its batch lease immediately.
- Non-print status updates are capped at once per second while diagnostics are enabled; startup and realtime summary lines go to NinjaScript Output. Optional sampled work attribution surrounds ingestion.
- Elapsed time starts at DataLoaded and includes interleaved/platform waiting. It is neither total platform startup time nor exclusive provider CPU time.
- No startup-speed improvement has been measured. The user's approximately 3:40 observation remains an uncontrolled baseline.

## Verification and deployment

- Core harness: **226 checks passed** (storage, ownership, coverage, budgets and ingestion contracts).
- Offline C# 7.3 semantic check against installed NinjaTrader references: **0 errors across five sources**. Structural guards passed: no renderer, no added series, no bar-path trade ingestion.
- Targeted deployment copied only `OrcaProviderStreamCore`, `OrcaProviderReadBudgetCore`, `OrcaProviderRegistryCore`, `OrcaProviderIngestionCore` and `OrcaProviderProbe` to NinjaTrader Custom Indicators. Normalized authored-source parity passed for all five.
- NinjaTrader tests performed: **none**. F5 compile, chart load and runtime behavior remain pending Julian's check; offline compilation and copy parity are not runtime validation.
- Full_Suite untouched; **not eligible for promotion**.

## Next validation and risks

1. Press F5 in NinjaScript Editor. If compilation fails, collect errors before adding the probe.
2. Add one `OrcaProviderProbe` to one existing Tick Replay chart without changing the other indicators or their settings. No full-platform restart required.
3. After chart loading completes, collect `OrcaProviderProbe` Output lines and any FAULT line. A historical event count and completed pass are expected; full retention may be false because capacity is intentionally bounded.
4. Verify removal unregisters its diagnostics row. Realtime ticks, session reset boundaries, callback ordering, disconnect/reconnect, repeated reload and disposal still need runtime testing.

Production sharing remains gated on explicit compatible connection/session/timezone identity, provenance and historical coverage policy. This isolated probe deliberately does not guess those identities or replace existing provider behavior. Historical replay testing can proceed while the market is closed; live continuity cannot be confirmed from a closed-market run.
