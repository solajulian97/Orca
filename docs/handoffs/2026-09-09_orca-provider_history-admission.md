# Required historical-range admission — 2026-09-09

## Objective / motivation

Continue the next source-contract slice after Julian's successful active-disconnect/fresh-live probe test. Two fresh instances received zero historical callbacks despite TickReplay=True and reported completion of an empty source-lifecycle pass. Prevent a future consumer from interpreting that state as sufficient historical coverage, without misclassifying a genuinely confirmed no-trade interval as missing data.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderHistoryAdmissionCore.cs` (new)
- `tests/OrcaProviderStream.Tests/ProviderHistoryAdmissionTests.cs` (new)
- `tests/OrcaProviderStream.Tests/OrcaProviderStream.Tests.csproj`
- `tests/OrcaProviderStream.Tests/Program.cs`
- `docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md`
- This handoff.

## Behavior / boundaries

A stateless, platform-independent evaluator accepts a raw immutable batch or a budgeted batch lease and a nonempty UTC half-open required interval. It returns an explicit admission status. Default status is ReadUnavailable, not Ready.

- Non-ready reads (closed/faulted, wrong generation, evicted or ahead cursor) are rejected.
- SourceLifecycle and Unverified coverage are UtcRangeUnverified regardless of event counts or passComplete/fullPassRetained flags. Populated lifecycle history is not more authoritative than an empty lifecycle pass.
- A declared requested range must be explicitly producer-confirmed; otherwise HistoricalPassPending.
- The declared interval must contain the full requirement, with no one-tick boundary tolerance.
- Confirmed history must remain fully retained. Prefix-evicted history is not salvaged through guessed timestamp overlap.
- An explicitly producer-confirmed empty requested interval may be Ready. Zero executions do not by themselves make a proven no-trade interval invalid.
- A disposed budgeted lease throws rather than presenting its surviving diagnostic metadata as an active read.

Ready means only that this snapshot's declared history is available. It does not mean the consumer has consumed it, that the producer's assertion is authenticated, that a source identity matches, that the interval can be reserved, or that the live continuation is complete. A caught-up empty read can report Ready while the required historical payload must still be read from the proper initial cursor. The consumer must validate generation/status on every subsequent bounded read and enforce its own live-gap/initialization rules. An older immutable snapshot remains old evidence after source closure; this pure evaluator does not refresh platform state.

No existing stream/coverage calculation or probe output was changed. No production consumer or registry calls this helper yet. It is the availability portion of future admission, not a shared-provider rollout.

## Settings / data / rendering / performance implications

- User-facing settings/defaults: none added, changed, deprecated or removed.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: none added or changed.
- AddDataSeries, OnMarketData, Calculate modes, Tick Replay flags, subscriptions and timers: unchanged.
- Historical loading: no platform request, reload, guessed range certification, data deletion or backfill introduced.
- Cache: no shared-cache or persistence change; evaluation does not mutate or copy history.
- Rendering: no OnRender use, chart object or UI dispatch. Do not move admission into rendering.
- Performance: constant metadata checks, no event scan, normal-path allocation or additional retained payload. Lease-open check uses the existing bounded lease Count accessor. Not a measured performance optimization.

## Verification / deployment / manual validation

357 linked-source core checks passed (35 new admission checks). Offline installed-platform C# 7.3 semantic compilation passed with zero errors across twelve sources and all existing structural guards. Existing linked-probe regression: 243 checks passed. Fixtures cover completed empty/populated lifecycle passes, live-only input, genuine confirmed empty intervals, pending confirmation, exact/contained/outside requirements, partially evicted history, all non-ready read statuses, caught-up snapshots, stale immutable evidence, invalid UTC/range inputs and disposed leases. Scoped diff hygiene passed.

Source-only. No live deployment, new NinjaTrader F5/load or runtime test of this helper. The existing validated probe was not changed. No user chart action is needed for this local contract slice. Full_Suite untouched and not eligible for promotion; unrelated dirty changes preserved.

## Remaining work

Establish platform historical source/merge/adjustment provenance and reliable requested-range confirmation before consumer integration. The current canonical probe intentionally cannot satisfy this gate because its coverage is SourceLifecycle. Do not make it pass by calling ConfirmHistory without evidence. Standalone snapshot/incremental semantics remain unresolved. First Rolling Profiles shadow integration must retain existing output and local series and independently compare calculations; hidden-series removal remains a separate gate.
