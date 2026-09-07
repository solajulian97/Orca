# Step Profile pre-settings backup — 2026-09-07

## Objective
Preserve the current Step Profile exactly before Julian's requested settings reorganization.

## Files added
- .codex-backups/step-profile-pre-settings-2026-09-07_131040/StepProfile-pre-settings.zip
- .codex-backups/step-profile-pre-settings-2026-09-07_131040/manifest.json
- .codex-backups/step-profile-pre-settings-2026-09-07_131040/README.md
- docs/handoffs/2026-09-07_orca-step-profile_pre-settings-backup.md

## Verification
Reopened the ZIP and SHA-256 verified all 22 entries. Rehashed originals after capture; all remained unchanged.
Archive SHA-256: 35042F83450F4243AA2AB0233E3B92D75C4096AC19BD7A744AEC46C8E11276EF
Includes uncommitted Working_Suite Step Profile, its diagnostics dependency as reference only, focused tests, recent component handoffs and separate installed-source references. Restore instructions are beside the ZIP.

## Behavior and user-facing settings
None added, changed, deprecated, or removed in this backup step.

## Secondary series, Tick Replay, historical load, cache, rendering, performance
No changes. No AddDataSeries, OnMarketData, Calculate, shared-cache, profile, statistics, rendering, historical-data or runtime-state changes. Offline archive only.

## NinjaTrader tests, compile, manual validation
None performed for backup. No deployment. ZIP integrity verified; existing compile/manual status unchanged.

## Risks and follow-up
Local source snapshot only; excludes templates, workspaces, binaries, market data and full transitive dependencies. Do not bulk-restore shared diagnostics; preserve newer edits before restoring Step Profile. Next step is presentation-only settings organization.

## Full_Suite eligibility
No new eligibility. Full_Suite untouched.
