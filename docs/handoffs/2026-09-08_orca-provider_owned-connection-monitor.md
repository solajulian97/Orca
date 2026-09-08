# Owned connection monitor — 2026-09-08

## Objective / evidence

Separate indicator connection notifications from notifications received during an explicitly owned subscription lifetime. Julian's latest ES Tick Replay capture compared 2,183,537 historical events, live=0, then showed two Disconnected->Connecting->Connected notification pairs immediately after Realtime while currentPrice was already Connected. Prior confirmation: the chart kept receiving price updates, including with the subscription observer removed. This supports initialization/delayed script notifications as a hypothesis, not a proven explanation of the platform internals.

The documented [Connection.ConnectionStatusUpdate](https://docs.ninjatrader.com/ninjascript/connectionstatusupdate) subscription is supported from any NinjaScript object and requires explicit unsubscribe. Use this direct, owned status lane for continuity; leave the indicator override as bounded diagnostic context. Do not interpret current connection status as proof an older notification/tick is safe. Read-only inspection of the installed reference assembly did not establish runtime dispatch ordering; no platform implementation was copied or used as a public API contract.

## Files changed

- Working_Suite/Indicators/OrcaProviderConnectionMonitor.cs (new)
- Working_Suite/Indicators/OrcaProviderProbe.cs
- tests/OrcaProviderProbe.Tests/{OrcaProviderProbe.Tests.csproj,PlatformStubs.cs,Program.cs}
- tests/OrcaProvider.PlatformCheck/Program.cs
- docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md
- This handoff.

## Behavior / ownership

The probe creates a private feed owner, stores a monitor and attaches its direct handler before publisher acquisition. A notification during attachment invalidates the owner too; it is not discarded as a presumed snapshot. Any direct notification, including unrelated, order-only or unclassified notifications, terminally invalidates the run and closes the private core immediately. Current connection-object status never overrides this decision. Duplicate notifications cannot reopen it.

The monitor callback atomically records invalidation plus at most one value-only notification snapshot and closes only its core owner. Core closure uses the existing bounded registry/buffer locks, with no probe lock, UI dispatch, Print, unsubscribe, account access or arbitrary consumer callback. Monitor lifetime operations use a separate lock; callback code never enters it. The monitor holds no chart/indicator/connection/account references, so the static event cannot retain a chart through this helper.

The probe checks monitor availability/invalidation before processing input, around initialization/handoff, after accepted input and before summary publication. Closed-buffer exceptions racing invalidation preserve the direct-notification cause. Output/Diagnostics reporting and exact-handler detachment occur on the next probe callback/status/removal; a quiet feed can have a closed core before a diagnostic line appears. Already delivered immutable batch leases retain their original consumer ownership.

Indicator OnConnectionStatusUpdate remains at most eight diagnostic lines, labeled script-connection-observation; it does not itself invalidate the epoch or reopen a stream. Global notifications remain authoritative for this experimental lifetime. This is not an exchange-origin authentication mechanism, an atomic cross-API sequence guarantee, or a production reconnect solution.

Normal and fault cleanup detach only the owned handler. Partial attachment failure preserves the stored helper for cleanup. Failed removal reports CONNECTION_MONITOR_DETACH_FAILED, disables admission and retains the helper for a later Dispose retry. Successful removal prints connection-monitor-detached=True. Replacement runs use new owners; queued callbacks against an old helper cannot affect the replacement.

## Settings / series / history / cache / rendering / performance

- Settings/defaults: no user-configurable properties added/changed/removed. Probe remains explicitly added.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: none added/changed; no AddDataSeries or BarsRequest.
- One connection-status handler per probe added; no extra market-data subscription. Existing primary OnMarketData, OnBarUpdate session flag, script OnConnectionStatusUpdate and Calculate.OnEachTick remain.
- Tick Replay, historical requests, classification, session configuration and identical-payload event handling unchanged. No guessed snapshot filter or timestamp deduplication.
- Private core closure only; production provider/cache/persistence and existing indicator consumers unchanged.
- No OnRender, timer, render work, account/order actions, automatic reconnect or platform connection changes.
- Per-input checks are atomic state reads; direct connection closure is low-frequency and bounded by the one-stream probe limit. No measured performance gain or hidden-series reduction.

## Verification / rollout

Final linked-probe suite: 162 checks passed. Offline C# 7.3 installed-platform semantic compile: zero errors across eleven provider sources; structural guards passed. Core regression: 322 checks passed. Subscription observer regression: 14 checks passed. Fixtures cover independent script/direct lanes, every fixture connection status (including Disconnecting) during four lifecycle phases, immediate quiet-feed core closure, partial add, failed remove/retry, concurrent attach/dispose and input/invalidation, bounded logs, no resurrection and owned-handler isolation. These fixtures do not establish actual NinjaTrader callback scheduling.

Source edit complete. Preflight confirmed pre-edit live probe authored parity against HEAD and absent live monitor target. Previous live probe backed up to `.codex-backups/provider-owned-connection-20260908/OrcaProviderProbe.live-before.cs`. Targeted deployment copied only the new monitor helper and probe, helper first; both authored source/live parity checks passed. NinjaTrader F5/load and revised runtime behavior pending Julian. No runtime disconnect or trading test performed by the agent. Full_Suite untouched and not eligible for promotion; unrelated dirty work preserved.

## Next gate / remaining work

F5, add only OrcaProviderProbe to the existing connected chart, allow historical load plus roughly ten seconds of live ticks, then remove it. Capture final summary and handler-removal line. Startup must show connectionGuard=DirectPlatformEvents; normal script notifications may remain visible without FAULT. Desired transport evidence: live>0, readerComparison=PASS, verified=historical+live, and connection-monitor-detached=True. A direct-platform FAULT is distinct evidence and must not be suppressed.

Even a passing run does not finish the shared provider. Historical/live source/coverage contracts, actual reconnection behavior, service discovery, first-consumer shadow parity, safe local-series removal and controlled performance measurements remain separate gates. Standalone MarketData.Update snapshot filtering remains unresolved and is not enabled by this change.
