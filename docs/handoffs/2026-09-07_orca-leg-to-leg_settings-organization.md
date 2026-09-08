# Leg-to-Leg Profile settings organization — 2026-09-07

## Objective

Apply the same settings cleanup used for CVP, Step and Rolling Profiles, preserving functionality and template values, with a verified backup before editing.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaLegtoLegProfile.cs`: Display metadata and description only relative to backup.
- `tests/OrcaLegToLegProfile.PlatformCheck/Program.cs` and `.csproj`: preservation, inventory, common-label, saved XML and offline semantic checks.
- `docs/indicators/ORCA_LEG_TO_LEG_PROFILE.md`: settings/compatibility reference.
- This handoff.

## Backup and existing work

Backup commit `7f0b727` contains the ZIP, manifest, restoration README and pre-settings handoff. Archive: `.codex-backups/leg-to-leg-pre-settings-2026-09-07_233554/LegToLeg-pre-settings.zip`. SHA-256: `7bf3cf07fe5cbf467f3df612a59582215834926999cf43193785907c61f04831`. All 67 entries were reopened and verified, including 55 saved chart/indicator XML files. Live source was unchanged since capture and matched the Working_Suite authored baseline before cleanup.

The source had pre-existing uncommitted reset, statistics, ATR, replay and diagnostics work. This current-source commit includes that preserved baseline. The settings cleanup itself changes only metadata and description, verified against the archived baseline. Unrelated files and shared dependencies are untouched.

## Behavior and user-facing settings

All 77 editable settings retained in 11 groups: Display, Profile Layout, Leg Detection, Rows & Scaling, Profile Colors, POC & Value Area, Text - General, Text - Delta, Profile Statistics, Active Delta Reset, Advanced - Data. Twenty-two common labels match CVP. Active/historical sizing and reversal labels are clearer; full/reset statistics, reset aggregation and hotkeys are grouped together.

No option added, removed, deprecated, hidden or reset. Property names, ranges, defaults, enum values, brush serialization, hidden legacy UseSeparateResetAggregation and generated wrappers are unchanged. Reset anchor state remains transient. Previously unused MinimumBarsPerLeg/ProfileSeparationPx settings do not acquire runtime effects.

## Secondary series and Tick Replay

No Tick/Second/Bid/Ask/Last/Volumetric/custom series changes. Existing conditional `AddDataSeries(BarsPeriodType.Tick, 1)`, `Calculate.OnPriceChange`, `OnMarketData`, SecondaryTickSeries/TickReplayLastEvents choices and replay adapter are unchanged. No redundant source or shared-provider migration introduced.

## Historical loading and caches

Historical leg construction, ATR/tick reversal, delta classification, aggregation, local leg data and diagnostics match the backup. No historical reload, cache/database operation or shared-cache change performed.

## Rendering and performance

Full/reset calculations, accumulation, statistics, overlay/side-by-side geometry, value area, POC, brushes, clipping and lifecycle all match the backup. No new render/per-tick/background work. Only property-grid presentation and description changed.

## Verification and compile status

- Full-source Roslyn token comparison excluding only Display attributes and exact old/new descriptions: passed, including all logic, defaults, properties, serialization, enums and wrappers.
- Inventory: 77 settings, 11 groups, unique positions; 22 common CVP labels match.
- Saved XML: 55 files hash-verified and byte-identical; 55 instances and 3,019 source-owned values retain their property definitions. No templates edited.
- Authored C# 7.3 semantic check against installed metadata: zero errors.
- Exact-target `deploy_orca.ps1 -Target OrcaLegtoLegProfile.cs`: completed; deployed authored parity passed. No mirror, dependency or Full_Suite copy.
- Deployed source with existing generated wrappers: zero offline semantic errors. Initial isolated check lacked the ATR factory because the local Indicator partial shadows the installed host; the harness now reads the installed @ATR.cs generated factory. No indicator logic changed to accommodate the harness.
- Scoped `git diff --check`: passed.

## NinjaTrader tests and manual-validation status

No native NinjaTrader F5, assembly-load or UI tests performed; native app control is unavailable in this session. Offline compilation is not native generation/runtime proof. Julian should press F5, inspect the groups and reload a saved indicator/chart template, confirming custom widths, ATR/tick choices, colors, compression, POC/value area and reset configuration. Confirm full/reset statistics and all reset display modes in normal operation. Manual reset anchors are not saved template values.

## Known risks and follow-up

Source preservation and XML checks support compatibility but do not prove a platform round trip. Existing replay/history/data-quality limitations remain. Restore only intended source or individually selected templates after preserving newer work; chart templates contain other indicators too. Dependency copies are references only.

## Full_Suite eligibility

No. Full_Suite untouched. NinjaTrader F5 and Julian's manual confirmation are required before promotion.
