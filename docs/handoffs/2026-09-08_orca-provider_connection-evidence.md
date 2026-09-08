# Provider progress and connection evidence — 2026-09-08

## Objective / evidence

Answer Julian's progress question and investigate the repeated Connecting fault without bypassing continuity safety. Two canonical-probe runs stopped after historical comparison PASS with live=0; latest historical/verified count 2,988,611. Julian repeated with the subscription observer removed and confirmed chart prices continued updating. Thus the probe stopped independently of continued chart updates. A benign initialization notification is a hypothesis, not a proven cause. Read-only platform logs reviewed in the previous investigation showed connections established at 18:26/18:33, with no later logged transition; absence of a matching log entry does not establish callback provenance.

## Progress / remaining implementation sequence

1. Bounded exact-event storage, ownership, sequence/generation cursors, retention/read budgets, classifier and isolated identities: implemented and tested offline.
2. Historical two-reader transport: user-run PASS around three million events per run. Not an independent exchange-completeness or consumer-calculation comparison.
3. Live adapter continuity: blocked by overbroad connection-notification guard. Add evidence now; use it to define/test an admission policy before changing guard behavior. Standalone subscription snapshot discrimination remains unresolved.
4. Historical/live coverage and recovery: verify boundary accounting, merge/rollover/adjustments, startup/reconnect behavior and source compatibility. Core closure is not automatic recovery.
5. Shared service discovery and first consumer shadow integration: not implemented. Keep local data/output until numerical parity is proved. Do not infer compatible history from instrument name or current connection.
6. Production adoption and performance: migrate one consumer, validate chart/reload/session behavior, then remove only redundant local series and measure the same workload. Expand consumers after evidence. No measured speedup and no hidden-series reduction yet.

There is no defensible percentage or time-to-finish estimate while platform semantics remain unresolved. A useful probe and a production shared provider are different completion gates.

## Files / behavior

- Working_Suite/Indicators/OrcaProviderProbe.cs: first eight connection callbacks after owner creation print UTC receipt time, lifecycle, previous/new price/order status, current connection object's price status (or unavailable), error enum and event counts. No connection/account names or native error text. Observations continue after fault solely to see following notifications; no stream resurrection.
- tests/OrcaProviderProbe.Tests/PlatformStubs.cs and Program.cs: additional platform shapes and bounded-after-fault assertions.
- docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md and this handoff: evidence and roadmap.

## Implications

Settings/defaults: unchanged. Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: none added/changed. Existing OnMarketData/OnBarUpdate/OnConnectionStatusUpdate overrides and Calculate.OnEachTick retained. No new subscription/timer, history request, Tick Replay change, cache read/write/persistence or OnRender work. Private fault/closure behavior unchanged. At most eight extra diagnostic lines per probe run; no per-trade output or performance claim. Accounts/orders, production provider/consumers and Full_Suite untouched.

## Verification / rollout

68 linked-probe checks passed. Installed-platform offline semantic compile: zero errors across ten sources; structural guards passed. Earlier 322 core and 14 observer checks are prior evidence, not rerun in this diagnostic-only change. Pre-edit authored live/HEAD parity passed; previous live source backed up to `.codex-backups/provider-connection-evidence-20260908/OrcaProviderProbe.live-before.cs`. Targeted deployment copied only OrcaProviderProbe.cs; post-deploy authored parity passed. F5/load and diagnostic runtime capture pending Julian. No actual connections changed by the agent. Not eligible for Full_Suite promotion.

## Next user gate

F5; add only OrcaProviderProbe to the same chart, allow loading plus about ten seconds, then remove. Send connection-observation and FAULT lines, including observations after the fault. A fault remains expected until the admission policy is resolved; this is not another unchanged pass/fail test. No restart, disconnect, trade or setting change required.
