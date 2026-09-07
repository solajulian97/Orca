# Rolling Profiles pre-settings backup — 2026-09-07

## Objective
Preserve the current Rolling Profiles before the requested CVP/Step Profile settings cleanup.

## Files added
- .codex-backups/rolling-profiles-pre-settings-2026-09-07_184716/RollingProfiles-pre-settings.zip
- Companion manifest.json and README.md restoration instructions.
- This handoff.

## Verification
All 9 ZIP entries reopened and SHA-256 verified. Originals rehashed and unchanged. Includes current uncommitted source, dependency references, separate live sources and the rolling-session handoff.
Archive SHA-256: 45585720f95fa320b2cf6eae4162c328e3401280c141514f82ef6f78a2548cd5

## Behavior and settings
None added, changed, deprecated or removed in this backup step.

## Secondary series, Tick Replay, historical load, cache, rendering and performance
None changed. Offline archive only. No AddDataSeries, OnMarketData, Calculate, runtime cache, chart or historical-data operations.

## NinjaTrader tests, compile and manual validation
None performed. Backup integrity passes; prior validation status remains unchanged. No deployment in this step.

## Risks and follow-up
Local source snapshot only. Templates, workspaces, compiled binaries, market data, runtime state and full transitive dependencies excluded. Preserve newer work before restoring; do not restore shared dependencies indiscriminately. Settings cleanup follows this verified capture.

## Full_Suite eligibility
No new eligibility. Full_Suite untouched.
