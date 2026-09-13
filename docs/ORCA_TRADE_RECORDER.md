# Orca Trade Recorder

Last updated: 2026-09-05

## Purpose

Orca Trade Recorder is a standalone NinjaTrader Control Center AddOn that records the trader's OBS scene only while one or more NinjaTrader accounts have an open position. It is a production-shaped private prototype: the monitoring and recovery boundaries are intended to remain valid if the feature is later productized, while installation and OBS scene creation remain manual.

The active source is `Orca Trades/Working_Suite/AddOns/OrcaTradeRecorderAddOn.cs`. `Full_Suite` must not receive this AddOn until NinjaTrader compilation and live SIM behavior are validated by Julian.

## Operator Workflow

1. Install OBS Studio 28 or later.
2. Create an OBS profile and scene collection named `Orca Trade Recorder`.
3. Create a `Trading Monitor` scene. Use Display Capture for the complete trading monitor, or enter another trader-created window/region scene in Recorder settings.
4. In the dedicated OBS profile, disable Desktop Audio and enable only the microphone intended for commentary.
5. In OBS `Tools > WebSocket Server Settings`, enable authentication and copy its password into Recorder settings.
6. Open `Tools > Orca Trade Recorder`, confirm the paths/settings, then run `Test Connection` and `Test Recording`.
7. Press `Arm` before trading. Closing the Recorder window does not disarm its AddOn-owned runtime.
8. Press `Disarm` after the session. Disarming stops and preserves any active recording, stops Orca-owned replay buffering, and finalizes pending clips.

The OBS password is protected with Windows DPAPI for the current user before it is written to `Documents/NinjaTrader 8/OrcaTradeRecorder.xml`. The AddOn calls the native Windows DPAPI entry points directly so it does not require NinjaTrader to carry an additional managed cryptography reference. Armed state is never persisted.

## Capture State Machine

States are `Off`, `Starting`, `Armed`, `Recording`, `Tail`, `Finalizing`, and `Error`.

- Arming launches the configured OBS executable minimized when necessary, authenticates WebSocket 5, verifies the dedicated profile/collection/scene/microphone, rejects active streaming or recording, checks disk space, configures MKV plus the requested replay duration, and starts the replay buffer.
- The runtime idempotently hooks every object currently in `Account.All` and rescans once per second for late-connected or replaced account objects.
- `PositionUpdate` and `ExecutionUpdate` handlers only request a coalesced position reconciliation on the NinjaTrader dispatcher. They never perform WebSocket, process, or file work.
- The aggregate snapshot of all non-flat `Account.Positions` is authoritative. A global flat-to-nonflat transition starts OBS recording and then saves the replay buffer. Starting the main recording first deliberately favors a small overlap over a missing entry.
- Partial exits, scaling, reversals, multiple instruments, and overlapping accounts remain in one recording.
- When all accounts become flat, `Tail` runs for the configured post-roll period. A new position cancels the tail. If the snapshot remains flat, the recording stops and raw footage is preserved.
- Arming with an existing position starts immediately and explicitly reports that footage from before arming is unavailable.

OBS commands are serialized through one background operation gate. A five-second health reconciliation checks the actual replay and recording outputs. A WebSocket-only interruption reconnects without changing the recording. If OBS has stopped recording while a trade remains open, Orca starts a continuation and marks the manifest for raw-segment recovery.

## Settings

`Documents/NinjaTrader 8/OrcaTradeRecorder.xml` stores:

- OBS executable, host, port, DPAPI-protected password, profile, scene collection, and capture scene.
- Microphone input name. Arming fails while OBS global Desktop Audio is enabled.
- Recording root, FFmpeg executable, pre-roll seconds, post-roll seconds, minimum free disk space, and automatic OBS launch.

Defaults are localhost port `4455`, profile/collection `Orca Trade Recorder`, scene `Trading Monitor`, microphone `Mic/Aux`, 15 seconds of pre-roll, 15 seconds of post-roll, 5 GB minimum free space, and automatic OBS launch.

## Recording Artifacts

Each global open-position interval creates:

`Documents/NinjaTrader 8/OrcaTradeRecordings/YYYY-MM-DD/capture-<timestamp-id>/`

The bundle contains raw pre-roll/recording segments and `capture.json`. The manifest records UTC timestamps, accounts, instruments, maximum simultaneous positions, state/recovery events, raw paths, final path, and finalization status.

Stopped clips finalize automatically in a background worker while armed, with failed work retried after 60 seconds; Disarm also waits for finalization. FFmpeg concatenates every listed segment with stream copy, and ffprobe requires video, audio, and a positive finite duration before publication. Raw inputs are retained. If finalization fails, the manifest records the error and no raw footage is deleted.

Diagnostics are written asynchronously to a bounded, rotating `Documents/NinjaTrader 8/OrcaTradeRecorder.log`. Credentials are never included.

## Round-trip results and filenames (2026-09-05)

New captures contain a schema-2 execution ledger: account, full contract, direction, entry/exit times, total entry quantity, execution IDs, gross P&L, and history completeness for each completed round trip. Scaling and partial exits stay in one round trip; reversals close one and open another. Completed-cycle cash flow times the actual instrument point value equals Execution Lines' FIFO gross realized total. Commissions are not deducted and filenames explicitly say `GROSS`.

At recording stop, the manifest and recorder status receive a result title. During automatic finalization (or Disarm), the finalized MP4 uses that title plus the unique capture ID, for example `WIN__GROSS-+$90.00__MNQ SEP26__LONG__Sim101__<capture-id>.mp4`. Losses and zero-cent results use `LOSS` and `BREAKEVEN`. Multiple round trips within one capture use `MULTI__2-TRADES__WIN__GROSS-+$90.00__<instruments>__<capture-id>.mp4`; each constituent result remains in the manifest. Classification rounds the aggregate gross amount to cents, away from zero at midpoint.

Existing-position entries, unclosed ledger positions, missing execution IDs, reconnect/start-of-day events, or unreadable fills produce `PNL-UNKNOWN` rather than an asserted result. An uncertain session requires Disarm/re-arm to reset conservative uncertainty. Dedupe IDs are scoped by account and retained until the next arm. The recorder does not reconstruct pre-arm execution history. Existing schema-1 captures retain their legacy names; already-finalized recordings and Journal links are not renamed. New Journal imports continue to read the final path from `capture.json`.

The ledger uses a short runtime lock during execution callbacks, with no filesystem, OBS, market-data subscription, chart dependency, or historical database work. It sums signed fill cash flow and splits reversal quantities; FIFO lot allocation is unnecessary for the fully closed gross total. No settings or outcome-based quality grades are introduced. Test recordings retain their existing timestamp filenames.

### Shared identity (2026-09-05, staged only)

New finalized captures now use schema 3. Each ledger trade adds `IdentityJson` and a nullable `TradeUid` from the shared `AddOns/OrcaTradeIdentity.cs` contract; schema-2 common fields and P&L/naming behavior remain. The initial observed cycle has no trusted UID. A correctly observed execution ending flat enables the following cycle, with each later fill checked against execution-position quantity and ID/order evidence. Crossing-flat fills carry distinct closing/opening allocations with the same execution ID in adjacent trades. Connection changes invalidate identity, and existing recorder uncertainty/re-arm rules remain stricter where history is incomplete.

Journal verifies schema-3 envelopes and displays exact/ambiguous/unmatched proposals. Media linking remains read-only for schema 2/3; existing recordings/links are not renamed or rewritten. The helper must deploy with this recorder source and the coordinated Journal build. No new settings, data series, market-data cache access, callback disk writes or chart rendering were introduced. See `docs/handoffs/2026-09-05_orca-journal_shared-identity.md` for offline checks and pending deployment/runtime gates.

## Performance And Safety Boundaries

- No secondary chart series, Tick Replay, `OnMarketData`, `OnBarUpdate`, SharpDX rendering, or historical chart processing is used.
- Account callbacks perform only a coalesced dispatcher request.
- OBS/network/disk/process work runs off account and chart callback paths.
- FFmpeg finalizes stopped bundles in the background while armed. A per-runtime semaphore and per-bundle exclusive lease prevent duplicate work; raw inputs remain intact.
- OBS is never commandeered when it is already streaming, recording, or running a replay buffer outside Orca ownership.
- Shutdown recording stop is bounded. MKV raw output and manifests remain the recovery source of truth.
- The prototype performs no automatic deletion or retention cleanup.

## Validation Gates

Source compilation, targeted deployment, NinjaTrader F5, OBS connection/setup, SIM behavior, and trading-session performance are separate gates. Static compilation or source/live parity does not validate recording behavior.

SIM validation must cover a single round trip, scaling, partial exits, reversal, overlapping accounts/instruments, a new trade during post-roll, late account connection, arming with an existing position, window close/reopen, OBS disconnect/restart, missing microphone/scene, low disk, and NinjaTrader shutdown. Final video, raw segments, manifest transitions, microphone audio, OBS skipped frames, and NinjaTrader/chart responsiveness must all be checked.

## 2026-09-12 reliability and Journal integration

See [capture reliability and review queue](handoffs/2026-09-12_orca-journal_capture-reliability-review-queue.md) for automatic finalization, atomic manifests, stricter segment validation, Journal association evidence, test results, and pending NinjaTrader validation. Runtime recreation still requires manual Arm. Source changes are staged, not deployed.

Deployment update 2026-09-13: recorder source and Journal DLL are now deployed with verified backups and hashes. NinjaTrader F5/load and manual capture validation remain pending; see the September 12 handoff deployment record.

