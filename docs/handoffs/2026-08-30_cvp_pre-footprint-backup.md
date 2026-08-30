# CVP Pre-Footprint Backup

## Objective

Preserve the current CVP before Julian supplies a new bid/ask footprint specification. No feature changes are authorized in this task.

## Files Added

- `.codex-backups/cvp-pre-footprint-2026-08-30_142958/CVP-current-version.zip`
- `.codex-backups/cvp-pre-footprint-2026-08-30_142958/manifest.json`
- `.codex-backups/cvp-pre-footprint-2026-08-30_142958/README.md`
- `docs/handoffs/2026-08-30_cvp_pre-footprint-backup.md`

No source file was edited. Working_Suite CVP has no diff from its existing Git checkpoint `c735f68` (2026-07-21, Add CVP bid ask footprint modes). Repository HEAD at capture was `827fbac9cdf06fded1d5d3bb0d061f7f8c093203`.

## Backup And Verification

The archive contains development and live copies of CVP, its direct shared-profile core and absorption-color dependencies, and the original footprint handoff. Dependency copies are context only, not permission for broad rollback.

- Reopened and SHA-256 verified all seven archive entries against the originals; all passed.
- Rehashed originals after capture; none changed during backup.
- Development CVP SHA-256: `3E3AC606E0BBF528C2DE544F86DA25A79B70B63FEB85DD370DB719CE83253C97`.
- Live CVP SHA-256: `B874B9CF798B232F83955B184FAC9DE1A033328B12CFC5BAC764E4D1B8C605A3`.
- Authored CVP regions match after newline normalization and trimming trailing whitespace at the generated-region boundary. Whole files differ in their generated wrappers.
- Archive SHA-256: `8509552416BFB9235AC57AF3F2CD29D1C1B23C6319E5C9B102BF3475A9D4CD08`.
- Restore instructions are beside the archive. It is outside all NinjaTrader compile folders.

## Behavior And User-Facing Settings

None added, changed, deprecated, or removed. The forthcoming footprint request remains unimplemented.

## Secondary Series

No Tick, Second, Bid, Ask, Last, Volumetric, or custom series changes. Existing AddDataSeries, OnMarketData, and Calculate paths are untouched.

## Tick Replay, Historical Load, And Cache Implications

None. No runtime load, shared-cache access, market-data mutation, or historical-data operation was performed. The archive is source-only and does not preserve in-memory profiles or chart configuration.

## Rendering And Performance Implications

None at runtime. Only an offline ZIP and documentation were created. No reload, compile, deployment, or chart interaction was performed.

## Tests Performed In NinjaTrader

None. Backup integrity was verified offline; this is not runtime proof.

## Compile Status

Not run for this backup-only task. Source remains unchanged. Prior handoff validation limits are preserved, not upgraded.

## Manual-Validation Status

No new manual validation. Julian can now provide the next specification with a verified rollback baseline available.

## Known Issues, Risks, And Follow-Up

Local snapshot only; no GitHub push or off-machine backup was performed. Templates, workspace settings, compiled binaries, market data, and full transitive dependency closure are excluded. Restoring must be scoped to CVP after preserving newer work; do not bulk-restore dependencies or place duplicate .cs files in compiled directories. Existing unrelated work is untouched.

## Full_Suite Promotion Eligibility

Not applicable to backup artifacts. Full_Suite is untouched, and the backup does not grant validation or promotion eligibility to the existing feature.
