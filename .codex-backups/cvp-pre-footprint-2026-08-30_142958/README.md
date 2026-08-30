# CVP Baseline Before Footprint Changes

Created: 2026-08-30, America/New_York.

`CVP-current-version.zip` preserves the exact development and deployed source bytes before the next bid/ask footprint request. `manifest.json` records original paths, sizes, SHA-256 hashes, and the repository HEAD at capture.

## Contents

- `Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`: authoritative development baseline, unchanged from Git commit `c735f68`.
- `NinjaTrader-live/Indicators/OrcaCandleVolumeProfile.cs`: deployed source, including NinjaTrader's generated wrapper. Its authored region matches Working_Suite after newline normalization and trimming trailing whitespace at the region boundary.
- Both copies of `OrcaVolumeProfileCore.cs` and `OrcaAbsorptionCandles.cs`: direct shared-profile and absorption-color dependency context, not automatic rollback targets.
- `reference/2026-07-21_cvp_bid-ask-footprint.md`: original implementation handoff and its recorded validation limitations.

All seven ZIP entries were reopened and hashed against their original files. Originals were checked again after archiving to detect changes during capture.

Archive SHA-256: `8509552416BFB9235AC57AF3F2CD29D1C1B23C6319E5C9B102BF3475A9D4CD08`.

## Restore Procedure

1. Preserve any newer work before restoring. Verify the archive hash against the manifest.
2. Extract into a separate temporary directory outside NinjaTrader's Custom folder and outside Working_Suite. Verify the extracted CVP hash against the manifest.
3. For an approved source rollback, restore only `Working_Suite/Indicators/OrcaCandleVolumeProfile.cs` to its original repository path. Do not copy the live version into Working_Suite without Julian's explicit approval.
4. Deploy only the restored CVP through the normal targeted workflow, then compile in NinjaTrader and have Julian validate the chart. Do not restore shared dependencies unless their changes are independently implicated and approved.
5. Do not promote to Full_Suite until compile and manual validation are confirmed.

This is a local source snapshot, not a NinjaTrader import package, full application backup, or standalone compilable suite. It excludes chart templates, workspace settings, market data, binaries, and transitive dependencies. Do not extract duplicate scripts into a compile folder. No GitHub push was performed.
