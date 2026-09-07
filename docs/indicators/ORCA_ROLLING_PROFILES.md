# Orca Rolling Profiles

Updated: 2026-09-07. Active source: `Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs`.

## Purpose

Displays volume and delta across a moving intraday or multi-day trading window. Includes full-session or RTH filtering, point of control, value area, adjustable rows, and customizable colors and delta labels.

## Settings organization

The settings follow CVP and Step Profile's order and vocabulary. All 52 existing editable settings remain available, with no visibility filter or settings converter added.

| Group | Controls in display order |
| --- | --- |
| 01 Display | Rolling Period, Operating Mode, Show Volume, Show Delta. |
| 02 Profile Layout | Volume Profile Width, Delta Profile Width, Delta Direction, Right Offset, Profile Row Spacing. |
| 03 Sessions | Minutes in Trading Day, RTH Start Time, RTH End Time. |
| 04 Rows & Scaling | Volume Row Size, fixed Order Flow Row Size, Dynamic Order Flow Aggregation, target row pixels, multiplier, minimum/maximum ticks. |
| 05 Profile Colors | Volume color/opacity, gradient toggle/minimum brightness/steps, delta colors/opacity/intensity controls. |
| 06 POC & Value Area | Show POC and color; Show Value Area and percentage; shading and color; boundary lines, color and width. |
| 07 Text - Delta | Show Delta Text, minimum absolute delta, font size, positive/negative colors, background toggle and color. |
| 08 Advanced - Data | Shared-provider toggle, historical backfill toggle/limit, provider records per update, local tick cache, data-source label and debug signatures. |

Six existing properties previously lacked Display metadata: `MinBrightness`, `ValueAreaPercent`, `VALineThickness`, `VolumeOpacity`, `DeltaTextMinThreshold` and `DeltaTextFontSize`. They now have readable names and explicit positions in the groups above; none is a new option.

Twenty-seven shared labels match CVP, including Volume/Order Flow Row Size, Minimum Profile Brightness, Shade Value Area, Value Area (%), Show Value Area Boundaries, Boundary Line Color/Width, Profile Row Spacing and Minimum Absolute Delta to Show Text. The old numbered property labels are removed; the group numbers determine the order. Provider controls use shared-provider wording. `SharedProviderMaxTicksPerRender` is displayed as Provider Records per Update because the existing processing occurs during provider updates; its internal name, range, default and effective 100–5,000 budget remain unchanged.

## Preserved contracts

- Internal property names, types, ranges, defaults, brush serialization, enum values and generated factory wrappers are unchanged. All settings remain available. No new migration logic.
- Rolling Period retains intraday and 1/2/5/10/20-day choices. Day windows use Minutes in Trading Day and the existing Trading Hours/session cutoff rules. Chart-session closures do not consume the rolling window. RTH mode retains its configured time filter.
- Source selection, provider backfill, local hidden 1-tick series, Tick Replay behavior, diagnostics and spike compaction are unchanged. This settings task does not migrate or harden the shared provider.
- Profile aggregation, POC/value-area calculations, delta classification, rolling expiry, profile geometry, text gating, rendering and SharpDX lifecycle are unchanged.
- Native NinjaTrader properties remain platform-managed. Template identity preservation is verified in source; actual saved-template round trips remain a NinjaTrader check.

## Backup and checks

Backup: `.codex-backups/rolling-profiles-pre-settings-2026-09-07_184716/RollingProfiles-pre-settings.zip`, with per-file SHA-256 manifest and restoration instructions. It preserves the current uncommitted source and separate installed-source references. Shared dependency copies are context only and should not be restored for a settings rollback.

Run `dotnet run --project tests/OrcaRollingProfiles.PlatformCheck --configuration Release` from the repository root. The check compares all non-presentation source tokens against the ZIP, verifies all 52 options, group/order uniqueness and 27 shared labels, then compiles against installed NinjaTrader metadata. `--live-generated` additionally checks deployed authored parity and compiles the installed generated wrappers with test-only host fields.

These checks do not execute NinjaTrader's generator or prove F5/assembly load, settings-grid layout or template behavior. See `docs/handoffs/2026-09-07_orca-rolling-profiles_settings-organization.md` for deployment and manual-validation status. Full_Suite promotion requires Julian's confirmation.
