# CVP settings organization — 2026-09-07

## Objective
Implement Julian's approved settings layout and concise description while preserving every existing option and its saved identity, defaults and functionality.

## Files changed
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`: Display metadata, indicator description and settings converter presentation only.
- `tests/OrcaFootprint.PlatformCheck/Program.cs`: explicitly allow and verify the approved description; continue enforcing all behavioral defaults.
- `tests/OrcaFootprint.PlatformCheck/VerifySettingsBackup.py`: compare to the exact pre-change ZIP, retaining all 93 settings and enforcing unchanged indicator logic, serialization, wrappers and dependencies.
- `docs/indicators/ORCA_CANDLE_VOLUME_PROFILE.md`: current grouping and visibility documentation.
- This handoff.

## Backup and existing work
Verified backup: `.codex-backups/cvp-pre-settings-2026-09-07_000755/CVP-pre-settings.zip`, committed in `b202af5`. It includes the current uncommitted CVP work at capture time. Source was verified unchanged from that ZIP immediately before this edit. Existing earlier CVP work remains intact; the committed current files include that pre-existing work. Rendering partial and footprint core remain byte-identical to the ZIP and are not part of this settings deployment.

## Behavior and user-facing settings
All 93 displayed properties remain. Eleven numbered groups: Display, Profile Layout, Candles, Rows & Scaling, Profile Colors, POC & Value Area, Text - General, Text - Volume, Text - Delta, Text - Bid x Ask, Advanced - Data. Each property has a unique group/order position. Labels and tooltips clarify value area, text thresholds, sizing and dominant-side emphasis. The approved indicator description is installed.

Text families are filtered by applicable profile mode. Bid x Ask style/width/enhancement appear in Bid x Ask; combined arrangement appears in Volume + Delta. Existing enhanced applicability and specialized volume-text toggle conditions remain. The enhanced converter uses the same groups rather than its older six-group override. No settings are deleted or reset; native platform settings are not modified. Shared appearance controls are conservatively retained wherever previously exposed, rather than hiding potentially cross-used colors or opacity controls.

## Secondary series and data paths
None added or changed. AddDataSeries, OnMarketData, Calculate mode, tick classification, profile calculation, Tick Replay, historical loading and shared cache access are unchanged. Public properties, ranges, enums, defaults, XML/brush serialization and generated wrappers are preserved.

## Rendering and performance
No rendering or calculation code changed. Filtering and metadata organization occur only during property-grid enumeration. No additional per-tick or OnRender work.

## Verification
- Backup comparison passes: 93 properties retained, 11 groups, no duplicate positions; all non-presentation indicator code and both dependencies match the backup.
- Model harness: 411 checks pass.
- Authored platform semantic check: 0 errors; 13 baseline behavior fixtures pass.
- The old description-default guard initially rejected the intentional description edit; it now checks that exact approved wording while preserving behavioral-default checks.
- One rerun encountered a held test executable after the rejected guard; the owned failed run was stopped, then a DLL-hosted build/run passed (0 build warnings/errors).
- Targeted deployment: only `OrcaCandleVolumeProfile.cs`, Working_Suite to NinjaTrader Custom; no mirror or Full_Suite copy.
- Installed source matched the captured live baseline before deployment. Post-deployment authored parity passes.
- Live-generated offline semantic check: 0 errors; baseline fixtures pass.
- Diff whitespace check passes.

## NinjaTrader compile and manual validation
No F5 or GUI interaction performed; native UI control is unavailable in this session. F5/assembly-load confirmation remains pending. No runtime or saved-template round-trip pass is claimed.

Julian should press F5, reopen CVP settings, and inspect Volume, Delta, Volume + Delta, Bid x Ask with enhancement off/on, and Off. Confirm group order, mode-dependent controls, and existing chart appearance. Save/reload a copy of an existing template and verify custom values persist when switching modes. In particular, confirm the runtime uses the contextual converter; the supplied screenshots showed properties together that the existing converter intends to filter.

## Risks and follow-up
Source identity/default checks support compatibility but do not replace NinjaTrader saved-template and property-grid validation. Prior runtime limitations remain unchanged. The backup provides scoped rollback; preserve newer edits before restoring. Do not restore shared dependencies indiscriminately.

## Full_Suite eligibility
No. Requires Julian's NinjaTrader compile and manual validation. Full_Suite untouched.
