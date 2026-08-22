# 2026-08-21 - Orca Trade Recorder OBS Prototype

## Objective

Add a standalone, production-shaped private prototype that automatically records a configured OBS scene while any NinjaTrader account has a non-flat position, with replay-buffer pre-roll, post-roll, microphone commentary, recoverable raw segments, and deferred MP4 finalization.

## Files changed

- `Orca Trades/Working_Suite/AddOns/OrcaTradeRecorderAddOn.cs`
- `docs/ORCA_TRADE_RECORDER.md`
- `docs/handoffs/2026-08-21_orca-trade-recorder_obs-prototype.md`

No existing dirty source file was edited. `Full_Suite` was not changed.

## Behavior added, changed, or removed

- Added `Tools > Orca Trade Recorder` with an AddOn-owned runtime that survives window close/reopen.
- Added manual Arm/Disarm, connection test, 10-second recording test, OBS launch, readiness/position/clip status, and focused recorder settings.
- Added idempotent monitoring for every account in `Account.All`, including once-per-second discovery of accounts created after arming.
- Added global position state transitions: first non-flat starts one recording; scaling, partial exits, reversals, and overlaps continue it; global flat starts a cancellable post-roll timer.
- Added OBS WebSocket 5 authentication/control, minimized dedicated-profile launch, scene/microphone/profile validation, replay-buffer and recording control, five-second health reconciliation, and continuation recovery.
- Added raw recording bundles, JSON manifests, deferred FFmpeg stream-copy MP4 finalization, ffprobe audio/video validation, and bounded asynchronous diagnostics.
- No existing indicator, AddOn, order-routing, risk-management, copier, execution-line, or discipline behavior changed.

## User-facing settings added, changed, deprecated, or removed

New settings are stored in `Documents/NinjaTrader 8/OrcaTradeRecorder.xml`:

- OBS executable, WebSocket host/port, protected password, profile, scene collection, capture scene, and microphone input.
- Recording directory, FFmpeg path, pre-roll seconds, post-roll seconds, minimum free GB, and automatic OBS launch.

Defaults are port 4455, dedicated `Orca Trade Recorder` profile/collection, `Trading Monitor` scene, `Mic/Aux`, 15-second pre/post-roll, 5 GB minimum free space, and auto-launch enabled. Armed state is not persisted.

## Secondary series added or changed

None. No Tick, Second, Minute, Bid, Ask, Last, Volumetric, or custom series was added or changed.

## Tick Replay implications

None. The AddOn is account-position driven and has no Tick Replay or chart-series dependency.

## Historical-load implications

None for charts or indicators. Arming snapshots current live account positions only. It does not reconstruct screen footage from executions before arming; an already-open position begins recording immediately and is marked as missing pre-arm footage.

## Cache implications

No Orca market-data or profile cache is used. The runtime retains only current account subscriptions, open-position summaries, pending capture manifests, and OBS connection state. OBS Replay Buffer owns encoded pre-roll memory.

## Rendering implications

The AddOn uses WPF only. It adds no chart overlay and no SharpDX path. The status window refreshes from one-second snapshots; closing it does not stop monitoring.

## Performance implications

- Account event handlers only coalesce a dispatcher reconciliation; they perform no WebSocket, file, process, or encoder work.
- Account discovery and authoritative position snapshots run once per second while armed.
- OBS commands are serialized on a background operation gate; health checks run every five seconds.
- FFmpeg finalization is deferred until Disarm so it does not compete with armed live trading.
- OBS Replay Buffer continuously encodes while armed and therefore has measurable GPU/CPU and memory cost that must be benchmarked on Julian's workstation.

## Tests performed in NinjaTrader

- No NinjaTrader F5 or live SIM recording test has been performed yet.
- Roslyn syntax parsing passed for the complete new source.
- A .NET Framework semantic compile against the installed NinjaTrader 8.1.8.1 `NinjaTrader.Core.dll` and `NinjaTrader.Gui.dll` passed with no compiler errors.
- Targeted deployment completed with `deploy_orca.ps1 -Target OrcaTradeRecorderAddOn`.
- Working_Suite and the live NinjaTrader AddOn match after newline normalization: SHA-256 `B7842A80052509E1984B3294F047AE29D86C17844E6F04592955040DCE76B018`.
- Cached `git diff --check`, NinjaTrader F5, OBS configuration, microphone verification, SIM state transitions, recovery, and performance testing are recorded separately as they occur.

## Compile status

- Static NinjaTrader-reference compile: passed.
- Targeted deployment and normalized source/live parity: passed.
- NinjaTrader F5 compile: pending Julian.

## Manual-validation status

Pending. OBS is not installed at its standard location, so the dedicated profile, scene, microphone-only audio, WebSocket authentication, replay buffer, recording, finalization, and SIM trade scenarios remain untested.

## Known issues, risks, and follow-up work

- OBS Studio 28 or later must be installed and a dedicated profile/scene collection must be configured before arming.
- The prototype favors preserving footage; the pre-roll/main join may briefly duplicate the entry moment.
- Raw segments are retained and no automatic storage retention is implemented.
- Automatic scene/source creation, an installer, multiple simultaneous per-account outputs, and seamless overlap trimming are deferred.
- OBS profile-parameter and `SetRecordDirectory` behavior must be confirmed against the installed OBS version.
- The reconnect path starts a continuation when OBS is no longer recording; a true OBS process crash can still lose the interval before restart.
- Runtime behavior and resource cost remain unvalidated until the prescribed SIM and controlled performance tests are completed.

## Full_Suite promotion eligibility

Not eligible. Promotion requires OBS setup, NinjaTrader F5, SIM behavior/recovery validation, microphone/video verification, and controlled performance evidence confirmed by Julian.
