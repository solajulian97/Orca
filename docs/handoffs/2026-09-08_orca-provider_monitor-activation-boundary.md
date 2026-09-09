# Connection monitor activation boundary — 2026-09-08

## Objective / evidence

Julian's latest capture contains `connection-monitor-detached=True` followed by a direct-platform Disconnected->Connecting fault at 18:09:59.6127379Z, with no startup line visible. The screenshot confirms cleanup and notification receipt, not callback timing inside the event-add accessor. The previous monitor treated even attachment-time callbacks as loss of continuity before there was a publisher. This change defines the start of admission explicitly and supplies the missing boundary evidence. It does not declare the platform notification harmless or prove a snapshot/replay mechanism.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderConnectionMonitor.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaProviderProbe.cs`
- `tests/OrcaProviderProbe.Tests/Program.cs`
- `tests/OrcaProvider.PlatformCheck/Program.cs`
- `docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md`
- This handoff.

## Behavior / admission contract

The caller creates a fresh private feed owner, stores its monitor and attaches before acquiring any publisher or admitting input. Empty-owner guards reject late attachment to existing streams. The atomic activation gate starts as a saturating nonnegative setup-notification count. Immediately after successful event-add return and the empty-owner check, one exchange sets the gate to -1 and freezes the setup count. Only then is the monitor armed; publisher acquisition remains afterward.

A callback uses compare/exchange to count itself in setup or, if activation wins the race, invalidate the active run. There is no lost gap and no waiting on the probe/lifecycle lock from the callback. This is a local callback-admission interval, not an atomic broker connection or exchange-tape boundary. Setup status does not authenticate a feed; history/provenance remains explicitly unverified.

Every post-activation direct notification still terminally closes the private owner, including Connected, Connecting, unrelated/order-only and null notifications. A callback queued during setup but delivered after activation is not excused. No grace timer, current-status override, inferred snapshot filter, automatic reconnect or rearming is introduced. Existing reset and Transition faults remain unchanged. Attachment exceptions still fail closed and detach the exact stored handler; their count/phase is recorded.

Startup adds one bounded `connection-monitor-active; attachmentNotifications=N; publisherAcquired=False` line. Startup/status version marker is `connectionGuard=DirectPlatformEventsAfterAttach`. Fault details now distinguish `phase=Active` and `phase=AttachmentFailure`. Count appears in normal summaries too. Existing eight-line script-observation limit and successful/failed cleanup reporting remain.

## Settings / series / Tick Replay / load / cache / rendering / performance

- User settings/defaults: none added, changed, deprecated or removed. Explicit opt-in probe only.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: none added or changed. No AddDataSeries or BarsRequest.
- Existing primary OnMarketData Last ingestion, OnBarUpdate session flags, script connection observations and Calculate.OnEachTick remain; no new market-data subscription.
- Tick Replay/history: no request, classification, reset, event deduplication or historical calculation change. Setup must finish before ingestion exists. Historical/live source provenance and completeness remain unverified.
- Cache: private owner only; production provider/cache/persistence and all existing consumers unchanged. No data/cache/workspace deletion.
- Rendering: no renderer, chart/UI dispatcher, render-time calculation or mutation added.
- Performance: constant retained monitor state, atomic setup counter only during attachment, unchanged active invalidation/core closure. One extra startup line; no per-tick extra log. No measured startup gain or hidden-series reduction.

## Tests / compile / deployment / manual validation

Final linked-probe suite: 243 checks passed. Core regression: 322 checks passed. Subscription observer regression: 14 checks passed. Offline installed-platform C# 7.3 semantic compile: zero errors across eleven sources; structural guards, including activation-before-publisher ordering, passed. Fixtures cover all status values during attachment and after activation, concurrent attachment callbacks, no publisher/input during setup, delayed callback delivery after activation, fifty activation races, attachment failures and existing lifecycle/input regressions. Fixtures do not prove NinjaTrader scheduling. An overlapping build briefly encountered an output-file lock; the completed standalone rerun passed.

Source edit and targeted deployment complete. Preflight verified both live authored bodies matched pre-edit HEAD; NinjaTrader was running (PID 24444). Backups: `.codex-backups/provider-monitor-activation-20260908/{OrcaProviderConnectionMonitor,OrcaProviderProbe}.live-before.cs`. Copied only monitor then probe using the targeted deployment helper. Both authored source/live parity checks and scoped diff hygiene passed. No broad deployment, mirror sync, restart or production-indicator change.

NinjaTrader tests performed by agent: none. F5/Custom-assembly load and revised live behavior require Julian's confirmation. Prior screenshot is evidence for the previous revision only. Full_Suite untouched and not eligible for promotion.

## Risks / next gate

F5, add only OrcaProviderProbe to one connected chart, wait for historical load and about ten seconds of live input, then remove it. Capture from the activation line through final summary/removal. A positive setup count establishes callbacks before this local activation boundary on that run, not a universal synchronous snapshot guarantee. An Active fault is new evidence and stays terminal. No restart is requested for the prior run because its screenshot reports successful detachment.

Shared service discovery, first-consumer shadow parity, historical/live source contracts, recovery, safe local-series removal and controlled performance measurements remain later work. This bounded probe update does not finish the production provider.

## 2026-09-09 runtime follow-up

Julian supplied revised-marker output with two attachment notifications, a historical/live comparison PASS and normal removal. The complete run verified 2,175,885 historical + 75 live = 2,175,960 events at 25.4 seconds, then reported `connection-monitor-detached=True`. This supersedes the pending revised-code load and normal historical/live/removal check above for this one observed run only. It does not verify deliberate interruption/reconnection, independent historical completeness, standalone subscriber snapshot semantics or production consumer parity. Full evidence and next safe gate: `2026-09-09_orca-provider_live-tail-runtime-evidence.md`. No Full_Suite promotion.
