# Step Profile settings organization — 2026-09-07

## Objective

Align Step Profile settings with the approved CVP organization without changing functionality. Back up the current source before editing.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaStepProfile.cs`: Display names, descriptions, groups and ordering, plus the concise indicator description. No other code changes relative to the pre-settings ZIP.
- `tests/OrcaStepProfile.PlatformCheck/VerifySettingsBackup.py`: backup contract/inventory check and optional deployed parity check.
- `docs/indicators/ORCA_STEP_PROFILE.md`: settings map and retained behavior.
- This handoff.

## Backup and existing work

Pre-change ZIP: `.codex-backups/step-profile-pre-settings-2026-09-07_131040/StepProfile-pre-settings.zip`, committed with manifest and restoration instructions as `13bd5d4`. All 22 entries were re-opened and SHA-256 verified; originals were rehashed and unchanged. Installed authored Step Profile matched the captured Working_Suite source before deployment.

Step Profile contained substantial pre-existing uncommitted development. The current source preserves it exactly outside settings metadata; the backup captures it independently of the older HEAD version. Any current-source commit includes that previously uncommitted baseline, not just the settings diff. Other suite files and the diagnostics dependency are not modified.

## Behavior and settings

All 88 displayed properties remain available with unique group/order positions. Eleven groups: Display, Profile Layout, Sessions, Rows & Scaling, Profile Colors, POC & Value Area, Text - Delta, Text - Historical Delta, Profile Statistics, Display Controls, Advanced - Data. Shared labels match CVP. POC and value-area toggles, colors and style controls are together; text, statistics and active/historical width settings are explicitly distinguished.

No properties are added, deleted, deprecated, hidden or reset. No new settings converter. Existing enum/dropdown values, ranges, public property identities, serialized brush helpers, defaults and generated factory wrappers are unchanged. Only the user-facing indicator description changes in State.SetDefaults.

## Secondary series, Tick Replay, historical load and caches

No changes to secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series. Existing AddDataSeries, OnMarketData, Calculate.OnPriceChange, trade-source mode, tick attribution, session boundaries, historical ingestion and caches are byte-equivalent after excluding metadata/description. No shared data access or historical operation added.

## Rendering and performance

Rendering, active right-edge geometry, historical logical bounds/clipping, mirror layouts, dynamic widths, aggregation, delta-label filtering/hover, statistics and SharpDX lifecycle are unchanged. No per-tick, render or background work added; only property-grid metadata changed.

## Verification

- Metadata-only backup check: passed; all code outside Display attributes and the exact description matches the captured baseline, including defaults, serialization, converters, calculations, rendering and wrappers.
- Inventory: 88 settings retained in 11 groups, no duplicate positions; 14 shared CVP labels match.
- Existing offline C# 7.3/NinjaTrader semantic check: 0 errors.
- Existing authored-expression label tests: all 14 pass (11 general cases plus 3 zero-cutoff cases).
- Scoped git diff --check: passed.
- Deployment: exact-target `deploy_orca.ps1 -Target OrcaStepProfile.cs` succeeded. Only the Step Profile source was copied; no diagnostics dependency, mirror or Full_Suite copy.
- Post-deployment `VerifySettingsBackup.py --live`: passes authored source parity as well as all preservation/inventory checks. ZIP SHA-256 remains `35042F83450F4243AA2AB0233E3B92D75C4096AC19BD7A744AEC46C8E11276EF`.

## NinjaTrader tests, compile and manual validation

No NinjaTrader UI tests or F5 performed. Native application control is unavailable in this session. Offline checks are not runtime proof. Julian must compile with F5 and verify the property grid, existing charts and template save/reload before Full_Suite promotion. Check Time, Volume and Session basis; active/historical mirrors and widths; global/filtered/hover-only labels; statistics and hotkey custom values.

## Risks and follow-up

New group/label names change where users find controls. All existing values retain their property identities; saved-template behavior still needs platform confirmation. Preserve newer edits before restoring the backup, and do not restore shared diagnostics wholesale.

## Full_Suite eligibility

No. Full_Suite untouched; Julian's F5 and manual acceptance remain required.
