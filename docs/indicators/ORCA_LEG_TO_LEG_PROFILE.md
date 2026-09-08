# Orca Leg-to-Leg Profile

Settings reference updated 2026-09-07. Source: `Orca Trades/Working_Suite/Indicators/OrcaLegtoLegProfile.cs`.

## Settings organization

All 77 editable settings remain available in 11 numbered groups. Only Display labels, tooltips, groups, ordering and the indicator description changed. No property identity, default, range, enum, serialization field or visibility changed.

| Group | Controls |
| --- | --- |
| 01 Display | Volume/delta visibility, historical delta and leg count, active leg box |
| 02 Profile Layout | Active/historical widths, offsets, spacing, mirror, draw behind candles |
| 03 Leg Detection | ATR/tick reversals, historical reversal overrides, minimum filters |
| 04 Rows & Scaling | Volume/delta compression and dynamic delta rows |
| 05 Profile Colors | Volume gradient, positive/negative delta colors, opacity and intensity |
| 06 POC & Value Area | POC, percentage, fill and boundary styling |
| 07 Text - General | Font family and weight |
| 08 Text - Delta | Delta font size, signed colors and background |
| 09 Profile Statistics | Full-leg/reset total delta, finish delta, delta percent, font and color |
| 10 Active Delta Reset | Enable, display modes, aggregation, hotkeys and status |
| 11 Advanced - Data | Trade source |

## Preserved behavior and compatibility

- Active and historical profiles retain separate widths and reversal settings. Show Delta remains the master delta toggle, with Show Historical Delta subordinate to it.
- Leg detection retains fixed-tick and ATR reversal paths and existing fallback/minimum filters.
- Volume rows retain fixed compression; dynamic aggregation applies to delta rows only.
- Active-leg reset remains independent of the full-leg accumulator. Reset Only, Overlay and Side-by-Side modes, reset aggregation choices, hotkeys and statistics retain their implementation.
- Hidden legacy `UseSeparateResetAggregation` remains available for older templates. Its getter/setter are unchanged, including preservation of an already selected ratio mode when loading true.
- Saved settings include property values and serialized brushes. The manual reset anchor is transient runtime state and is not preserved by templates.
- Existing `MinimumBarsPerLeg` and `ProfileSeparationPx` settings remain; this cleanup does not add runtime behavior to them.

## Data and rendering

Existing `Calculate.OnPriceChange`, conditional one-tick `AddDataSeries`, `OnMarketData`, Tick Replay Last-event selection, replay participation, local leg accumulators and diagnostics are unchanged. No additional series, cache work, historical rebuilds, rendering work or performance change introduced.

## Verification and recovery

Backup commit `7f0b727`: `.codex-backups/leg-to-leg-pre-settings-2026-09-07_233554/LegToLeg-pre-settings.zip`, with manifest and scoped restoration instructions. Includes current source, separate dependency/live references and 55 saved XML template files.

`dotnet run --project tests/OrcaLegToLegProfile.PlatformCheck -- .` verifies non-presentation source preservation, settings, common labels and saved XML hashes, then performs an offline C# 7.3 semantic check against installed NinjaTrader metadata. Add `--live-generated` to check deployed authored parity and existing wrappers, including the installed ATR factory needed by isolated compilation.

Both offline modes passed with zero errors. All 55 templates remain byte-identical, covering 55 instances and 3,019 source-owned values. This supports source-level compatibility; no NinjaTrader template loading was executed. Native F5, chart inspection and Julian's template reload are pending. Full_Suite is not eligible for promotion.
