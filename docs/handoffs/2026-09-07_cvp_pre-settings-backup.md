# CVP pre-settings backup — 2026-09-07

## Objective
Preserve the current CVP before settings reorganization. Backup only; implementation remains pending.

## Files added
- .codex-backups/cvp-pre-settings-2026-09-07_000755/CVP-pre-settings.zip
- .codex-backups/cvp-pre-settings-2026-09-07_000755/manifest.json
- .codex-backups/cvp-pre-settings-2026-09-07_000755/README.md
- docs/handoffs/2026-09-07_cvp_pre-settings-backup.md

## Verification
Reopened ZIP and SHA-256 verified all 24 entries. Rehashed originals after capture; all remained unchanged.
Archive SHA-256: BE920F2F83DAFA79EBAB8A23E871966BD5681C2F0E6F27F9586B536D9C7B5A6D
Includes current uncommitted Working_Suite source, rendering partial, footprint core, documentation, focused tests, and separate installed-source references. Restore instructions are in README.md.

## Behavior and user-facing settings
None added, changed, deprecated, or removed.

## Secondary series, Tick Replay, historical load, cache, rendering, performance
No changes. No data access paths, AddDataSeries, OnMarketData, Calculate settings, runtime caches, chart rendering, or historical data were modified. Offline archive only.

## NinjaTrader tests, compile and manual validation
None performed. No deployment or runtime changes. Backup integrity passes; existing compile/manual-validation status remains unchanged.

## Risks and follow-up
Local backup only; no remote push. Templates, workspaces, compiled binaries, market data and full transitive dependencies are excluded. Restore only scoped files after preserving newer work; shared core needs diff review. Settings implementation remains pending.

## Full_Suite eligibility
No new eligibility; Full_Suite untouched.
