# Orca Step Profile

Updated: 2026-09-07. Active development source: `Orca Trades/Working_Suite/Indicators/OrcaStepProfile.cs`.

## Purpose

Displays volume and delta profiles by time interval, traded volume, or market session. Includes active and historical profiles, point of control, value area, delta labels, and profile statistics.

## Settings organization

The settings follow Candle Volume Profile's numbered flow: display choices, layout, rows, colors, POC/value area, text and advanced data. Step-specific controls retain their own meaning and scope. All 88 displayed properties remain available; this cleanup adds no conditional visibility or new settings converter.

| Group | Controls in display order |
| --- | --- |
| 01 Display | Step Basis; Step Interval; Step Volume Amount; Volume Step Start; Session Configuration; active Volume/Delta visibility; historical Volume/Delta visibility. |
| 02 Profile Layout | Mirror toggle and independent historical/active arrangements; active unmirrored layout; active volume/delta widths; right offset; dynamic/fixed historical volume width; dynamic/fixed historical delta width; non-RTH and RTH width overrides; row spacing; drawing behind candles. |
| 03 Sessions | RTH-only filter; RTH start/end times; session labels. |
| 04 Rows & Scaling | Volume row size and dynamic aggregation; delta row size and dynamic aggregation, with existing multipliers and minimum/maximum row sizes. |
| 05 Profile Colors | Volume color and active/historical opacity; gradient, minimum brightness and steps; delta colors and active/historical opacity; delta intensity and its minimum opacity. |
| 06 POC & Value Area | Show POC and color; Show Value Area and percentage; shading and color; boundary lines, color, width and style. |
| 07 Text - Delta | Show Delta Text; minimum absolute delta; font size; positive/negative text colors. |
| 08 Text - Historical Delta | Historical label mode; absolute-delta, row-height and bar-spacing filters; hover reveal. |
| 09 Profile Statistics | Master visibility; active/historical visibility; total delta, finish delta, delta percentage and total volume; font size and color. |
| 10 Display Controls | Block separators and color; visibility-hotkey enable and chord. |
| 11 Advanced - Data | Existing trade-source mode. |

Shared names match CVP, including Minimum Profile Brightness, Shade Value Area, Value Area (%), Show Value Area Boundaries, Boundary Line Color/Width/Style, Profile Row Spacing and Minimum Absolute Delta to Show Text. Historical abbreviations are spelled out. Width labels distinguish volume from delta and pixels from percentages. Every setting has a unique order within its group.

Internal property names, enum values, ranges, defaults, serialization and dropdown converters are unchanged. Native NinjaTrader properties remain platform-managed. Existing saved values should retain their identities; actual template round-trip validation remains a NinjaTrader gate.

## Existing behavior retained

- Step Basis selects Time, Volume Amount or Session. Time interval and volume amount/reset controls retain their mode-specific applicability, explained in their tooltips. RTH Only filters Time/Volume; Session uses its existing boundaries and configuration. The settings remain visible so no option is hidden or reset by this cleanup.
- Active profiles retain their right-edge anchor. Historical profiles retain their logical period bounds and clipping. Mirror arrangements remain independent for active and completed profiles. Session width overrides retain their existing native Volume/Minute-chart requirements.
- `FollowGlobal` historical labels use the Text - Delta settings. `Filtered` uses all three historical thresholds independently. `HoverOnly` reveals the hovered row, and `Hidden` suppresses historical labels and hover. A zero zoom threshold disables that cutoff; bar spacing is chart-candle spacing, not profile width.
- Statistics preserve their existing selected tokens, historical placement after each separator, and separate active row. No calculation or geometry changes were made.
- Trade Source Mode retains the secondary-tick and Tick Replay Last Events choices. Existing AddDataSeries, OnMarketData, Calculate.OnPriceChange, diagnostics, historical ingestion and cache behavior are unchanged.

## Backup and validation

Pre-change snapshot: `.codex-backups/step-profile-pre-settings-2026-09-07_131040/StepProfile-pre-settings.zip`. Its manifest contains per-file SHA-256 values, and its README explains scoped restoration. It includes uncommitted source as captured; dependency copies are reference only.

`tests/OrcaStepProfile.PlatformCheck/VerifySettingsBackup.py` compares all code outside Display metadata and the exact approved description against that ZIP. It also checks the full option inventory, group/order uniqueness and fourteen shared CVP labels. `--live` checks deployed authored parity.

See `docs/handoffs/2026-09-07_orca-step-profile_settings-organization.md` for implementation checks and deployment status. Source verification and offline semantic compilation do not establish NinjaTrader F5/load, property-grid layout or saved-template compatibility. Full_Suite promotion requires Julian's explicit manual validation.
