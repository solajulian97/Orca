# Explicit count-back endpoint — 2026-09-13

## Objective / evidence

Continue automatically from Julian's runtime output for `2b7bf78c186344aabc412026aae78047`. The new diagnostic reported exactly one difference before inspection: ToLocal.Ticks changed from `2099-12-01T00:00:00.0000000` (662353632000000000) to `2026-09-13T18:46:11.8765741` (639249219718765741). All other encoded fields matched. The guard rejected and cleanup reported requestReleased=True at 2.38s. This identifies the mismatch in this run, not a universal guarantee about all request environments. No bars/count/coverage evidence was inspected.

The prior constructor endpoint was unresolved until request execution. Correct the observer's request initialization, without ignoring returned endpoint changes or loosening the history configuration contract.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderHistoryProbe.cs`
- `tests/OrcaProviderHistory.Tests/PlatformStubs.cs`
- `tests/OrcaProviderHistory.Tests/Program.cs`
- `tests/OrcaProvider.PlatformCheck/Program.cs`
- `docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md`
- This handoff.

## Behavior

After constructing the count-back request and before capturing its configuration, the observer assigns ToLocal explicitly: current UTC converted through the already captured request-local TimeZoneInfo, with Unspecified kind to represent that local clock. BarsBack remains 1000. The endpoint is fixed at initialization, not left as a far-future placeholder. Output adds `request-end explicit=True ToLocal=... kind=Unspecified; BarsBack=1000; guard=STRICT`.

The canonical configuration encoding, strict comparison and named mismatch diagnostics are unchanged. Even a one-tick change in the explicit endpoint still rejects before inspection. No generic sentinel acceptance, date exclusion, post-load rebasing, retry or automatic rerun was introduced. A platform that still modifies the explicit end will produce diagnostic rejection rather than silently bypassing the guard.

Installed public metadata verifies ToLocal, FromLocal and BarsBack have public setters; the metadata-only diagnostic now shows accessor accessibility. [NinjaTrader's BarsRequest documentation](https://ninjatrader.com/support/helpguides/nt8/barsrequest.htm) distinguishes count-back from date-range requests; this remains a count-back request, not a switch to whole-trading-day loading. Runtime evidence that the explicit endpoint is honored is still required.

## Settings / series / history / cache / rendering / performance

- User-facing settings added/changed/deprecated/removed: none; one additional low-frequency output line.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: no additions or changes; no AddDataSeries.
- Historical request: still one repository-only futures Last Tick-1 request for 1000 bars, DoNotMerge, no split/dividend adjustments; now anchored to an explicit initialization-time local end. Chart/global settings remain unchanged. No provider download or repair requested.
- Tick Replay: unchanged and not required by this observer. No OnMarketData/OnBarUpdate/Update subscription or Calculate-mode changes. Playback-specific behavior remains untested.
- Cache/persistence: no shared/production cache changes or historical-store mutation; NinjaTrader owns its internal request/cache behavior.
- Rendering: none; no render-path work.
- Performance: one clock conversion and one bounded output line per observer instance. Existing inspection cap and cleanup remain unchanged; no performance improvement claim.
- UTC interval, quote provenance, exact tape completeness, historical/live stitching and consumer migration: still unproven and not enabled.

## Tests / release gates

- 170 linked observer checks passed, including constructor-placeholder simulation, explicit endpoint within the initialization interval, unchanged 1000-bar intent, successful modeled completion when the endpoint is honored, and rejection if that endpoint changes by one tick.
- Regressions: 480 core, 243 canonical-probe and 14 subscription-observer checks passed (907 total).
- Offline C# 7.3 installed-platform semantic check: zero errors across 14 sources; structural guards passed.
- Source edit complete. Targeted observer deployment passed old-live parity against fe68263. Backup: `.codex-backups/provider-explicit-end-20260913-185016/`. Normalized authored source/live SHA-256: `F0A526386DD2D753AD2484C75BA6B34BDF54CFFA00D759FF2169F2533F548D04`.
- NinjaTrader F5/load and new explicit-end runtime validation: pending. The supplied output proves the previous diagnostic mismatch/cleanup path only, not the new initialization fix.
- Full_Suite untouched and ineligible. Canonical probe, configuration helper, production indicators and unrelated dirty/staged work are unchanged by this slice.

## Next required user action

Remove the completed history probe, F5, add it back on the same unchanged chart and provide all output for the fresh ID, including request-end, samples or configuration-change, and cleanup. Do not change sessions, Tick Replay, contracts, cache or connection merely to make the guard pass. Continue from that evidence without requiring another authorization to investigate this scoped observer workflow.
