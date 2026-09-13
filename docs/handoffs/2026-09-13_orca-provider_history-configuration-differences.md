# History configuration mismatch diagnostics — 2026-09-13

## Objective and runtime evidence

Julian reported the following output for `b433701f0cd94ec2be57130368dd6c98`:

```text
starting requested=1000 Last-Tick-1; instrument=ES SEP26; lookup=Repository; merge=DoNotMerge; timeout=30s; eventClock=Eastern Standard Time; requestClock=Eastern Standard Time; published=0; UTC-range-confirmed=false
OBSERVATION_FAILED Historical request configuration no longer matches its captured identity.
cleanup reason=ERROR requestReleased=True elapsed=1.66s; published=0; UTC-range-confirmed=false
```

The code path passed the owned-request and ErrorCode.NoError checks, then rejected the initial configuration comparison before reading Bars or sample rows. The old observer loaded/executed and reported successful request release for this run. This does not establish returned bar count, UTC coverage, provenance or an explicit F5 keystroke. Resolving count-back FromLocal/ToLocal values is a hypothesis, not a demonstrated cause: the old output does not identify any changed field.

Julian authorized continued implementation and asked to stop only when his input is needed. The completed old observer can remain installed while development proceeds because it does not automatically issue another request; a fresh instance is needed to collect the new evidence.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderHistoryConfigurationCapture.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaProviderHistoryProbe.cs`
- `tests/OrcaProviderStream.Tests/ProviderHistoryConfigurationTests.cs`
- `tests/OrcaProviderHistory.Tests/Program.cs`
- `docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md`
- This handoff.

## Behavior

An additive RequireUnchanged overload accepts a transient reporting callback. On mismatch it decodes the same two validated canonical snapshots that failed comparison and reports named changed fields with before/after values. It does not reread mutable platform fields for the differences, retain the callback, change the canonical encoding/key or weaken any rejection. The original overload remains available and silent.

Reports include contract/expiry/type/price metadata, series/merge/lookup/adjustment/reset settings, mode/BarsBack, FromLocal/ToLocal ticks and kinds, session/clock definitions and indexed rollover fields. Dates display round-trip values plus exact ticks; floating-point values include exact bits. Output is bounded to twelve changed-field lines and one total/omission summary; individual text values over 160 characters are explicitly truncated. Date kind is a separate field. A recapture that fails validation still fails closed through the original error path; it is not converted into a valid snapshot for diagnostic comparison.

The probe labels differences before versus after inspection. A mismatch still throws, publishes zero events and follows existing disposal behavior. No dates or other fields have been excluded on an unverified assumption. No new request, automatic retry or rerun was added.

## Settings, series, history, cache, rendering and performance

- User-facing settings added/changed/deprecated/removed: none.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: none added/changed; AddDataSeries absent.
- Existing one-shot repository-only 1,000 Last Tick-1 BarsRequest: unchanged. No Update, OnMarketData, OnBarUpdate or connection callback added; no Calculate mode change.
- Tick Replay and chart/session configuration: unchanged; no new dependency.
- Historical-load implications: unchanged request; mismatch still blocks sample inspection and coverage admission. No provider download, historical repair, broader reload or historical/live stitch.
- Cache/persistence: no shared/production cache access, mutation, deletion or persistence.
- Rendering: no changes; observer has no renderer.
- Performance: mismatch-only parsing of an already bounded configuration and bounded Output. No per-tick work; no measured speedup claim.

## Verification and deployment

- 480 provider-core checks and 154 linked observer checks passed. Added checks cover silent unchanged snapshots, exact date/kind field differences, bounded output, unchanged rejection and linked pre-inspection lookup-policy diagnostics.
- Offline C# 7.3 platform semantic check: zero errors across 14 sources; existing no-publication/no-series/no-rendering/one-request guards pass.
- Canonical-probe/subscription regressions: 243 and 14 checks passed, respectively.
- Targeted deployment: both changed sources copied after old authored live parity matched commit a0fcb07. Backups: `.codex-backups/provider-history-diff-20260913-183435/`.
- Normalized authored source/live SHA-256: capture `254DE6847618A5359C11E85435C1E170285581285AAC4219946E4468A34A402D`; probe `60C1064E36CAC9CC584E5B4AC3A0251E8D7B1DD5FEAB5498563036E4B4071FC1`.
- New F5/load and new runtime output: pending Julian. The supplied run validates only the previous observer's reported error/cleanup path.
- Existing canonical probe and production indicators: no edits/deployment. Unrelated dirty/staged work preserved. Full_Suite untouched and not eligible for promotion.

## Next gate

Remove the completed old history probe, F5, add one fresh history probe to the same chart without changing settings, then capture all lines for its new ID, including configuration-change and cleanup. No trading, disconnect, cache deletion or data download requested. Use those values to distinguish platform-resolved request fields from unexpected semantic configuration changes before deciding any correction. A diagnostic mismatch is not a reason to bypass the guard.
