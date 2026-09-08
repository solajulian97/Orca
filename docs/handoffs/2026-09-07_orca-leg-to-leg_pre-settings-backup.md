# Leg-to-Leg pre-settings backup — 2026-09-07

## Objective
Preserve current Leg-to-Leg source and saved templates before the requested settings cleanup.

## Files added
- .codex-backups/leg-to-leg-pre-settings-2026-09-07_233554/LegToLeg-pre-settings.zip
- Companion manifest.json and README.md restoration instructions.
- This handoff.

## Verification
Reopened and SHA-256 verified all 67 entries, including 55 saved chart/indicator template files containing Leg-to-Leg. Originals rehashed and unchanged. Includes current uncommitted source, diagnostics/replay references, separate live-source copies and component handoffs.
ZIP SHA-256: 7bf3cf07fe5cbf467f3df612a59582215834926999cf43193785907c61f04831

## Behavior and settings
None added, changed, hidden, deprecated or removed in this backup step. No source or template edited.

## Secondary series, Tick Replay, historical load, cache, rendering and performance
No changes. Offline backup only. No AddDataSeries, OnMarketData, Calculate, replay/reset state, cache, chart or historical-data changes.

## NinjaTrader tests, compile and manual validation
None performed. Backup integrity verified; no F5, load or runtime claim. No deployment in this step.

## Risks and follow-up
Local copy only. No workspace, unsaved chart state, transient manual-reset anchor, market data, compiled binaries or full dependency closure. Restore only scoped source/template files after preserving newer work; saved chart templates also contain unrelated chart settings. Settings cleanup follows the verified capture.

## Full_Suite eligibility
No new eligibility. Full_Suite untouched.
