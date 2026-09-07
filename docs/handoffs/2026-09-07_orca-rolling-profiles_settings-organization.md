# Rolling Profiles settings organization — 2026-09-07

## Objective

Apply the CVP/Step Profile settings cleanup to Rolling Profiles, preserving all functionality and saved settings. Back up the current source before editing.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs`: settings Display metadata and an indicator description only relative to the captured source.
- `tests/OrcaRollingProfiles.PlatformCheck/Program.cs` and `.csproj`: source preservation, inventory, shared-label and offline semantic checks.
- `docs/indicators/ORCA_ROLLING_PROFILES.md`: current settings reference.
- This handoff.

## Backup and prior work

Backup commit `c17def0` preserves `.codex-backups/rolling-profiles-pre-settings-2026-09-07_184716/RollingProfiles-pre-settings.zip`, manifest and restoration instructions. All nine archived entries and unchanged originals passed SHA-256 verification. Archive SHA-256: `45585720f95fa320b2cf6eae4162c328e3401280c141514f82ef6f78a2548cd5`.

Rolling Profiles had pre-existing uncommitted diagnostics, spike-compaction and other changes. They remain unchanged; the captured source matched the installed authored source before this cleanup. A current-source commit includes that preserved baseline in addition to metadata edits. Other source files, shared dependencies and the deployment script are untouched.

## Behavior and user-facing settings

All 52 editable settings retained in eight groups: Display, Profile Layout, Sessions, Rows & Scaling, Profile Colors, POC & Value Area, Text - Delta, Advanced - Data. Twenty-seven shared labels match CVP. Six existing unlabeled/ungrouped properties now have Display metadata: minimum brightness, value-area percentage, boundary width, volume opacity, delta text threshold and font size. POC and value-area controls now sit beside their colors. Text controls are ordered consistently, and provider/cache controls are together at the end.

No option added, removed, deprecated, hidden or reset. Property identities, ranges, defaults, enum values, brush serialization and generated wrappers unchanged. A concise indicator description was added. Master Ticks Per Render was relabeled Provider Records per Update to reflect the existing update path; internal budget behavior is unchanged.

## Secondary series, Tick Replay, historical loading and caches

No Tick/Second/Bid/Ask/Last/Volumetric/custom series changes. Existing conditional hidden 1-tick AddDataSeries, OnMarketData, Calculate.OnPriceChange, shared-provider selection/backfill, local cache and diagnostics are unchanged. No provider migration, data reload or cache operation performed. Trading-session window cutoffs and RTH filtering are preserved.

## Rendering and performance

All calculation, rolling expiry, aggregation, POC/value-area, delta classification, drawing, clipping, text gating and SharpDX lifecycle code matches the backup. No new per-tick, rendering or background work. Only property-grid metadata and the indicator description changed.

## Verification

- Full-source token comparison after removing Display metadata and the exact new description: passed. Includes all logic, defaults, serialization, enums and generated wrappers.
- Inventory: 52 settings retained; eight groups with unique positions; six existing Misc properties assigned labels/groups; 27 common CVP labels match.
- Authored C# 7.3/NinjaTrader semantic compilation: zero errors.
- Scoped git diff --check: passed.
- Exact-target `deploy_orca.ps1 -Target OrcaRollingProfiles.cs`: completed. No mirror, dependency or Full_Suite copy.
- Deployed authored source parity: passed.
- Live source including generated wrappers: zero offline semantic errors; all source-preservation/inventory/label checks also pass. This does not execute or certify NinjaTrader's generator.

## NinjaTrader tests, compile and manual validation

No NinjaTrader UI tests or F5 performed. Native app control is unavailable in this session. Offline compilation does not establish native generation or assembly load. Julian should press F5, inspect the new settings layout, reload a saved template and confirm custom values/appearance. Check full-session/RTH settings, day/intraday rolling periods, fixed/dynamic delta rows, colors/text/POC/value area, and existing source configuration without changing it merely for this cleanup.

## Known risks and follow-up

Source preservation supports template compatibility but does not replace a platform round trip. Existing provider/history coverage limitations remain. Restore only Rolling Profiles after preserving newer edits; shared dependency copies are reference only. No promise of new historical accuracy or performance is made.

## Full_Suite eligibility

No. Full_Suite untouched. NinjaTrader F5 and Julian's manual acceptance remain required.
