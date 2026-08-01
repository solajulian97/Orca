# OrcaSessionContextMap Panel Hierarchy

## Objective

Make the Session Context Map stats panel easier to scan by visually separating Asia, London, and New York context without changing session calculations.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaSessionContextMap.cs`
- `docs/ORCA_ARCHITECTURE.md`
- `docs/handoffs/2026-08-01_orca-session-context-map_panel-hierarchy.md`

## Behavior Added, Changed, Or Removed

- Replaced the single white multiline text block with individually rendered title, session-heading, classification, metrics, relationship, and status rows.
- Added a larger semibold panel title.
- Added session-colored headings and vertical accents using the existing Asia, London, and New York colors.
- Added classification colors: bullish states use the bullish event color, bearish states use the bearish event color, balanced uses the neutral event color, and unknown uses the label color.
- Added a strong divider below the panel title and subtle dividers between session sections.
- Panel width and height now respond to font size, compact mode, and the number of displayed sessions.
- `Show Only Current Session` now produces a shorter one-section panel instead of retaining space for all sessions.

## User-Facing Settings

No settings were added, renamed, deprecated, or removed. Existing panel position, opacity, font size, compact mode, current-session-only, session-color, classification-color, and label-color settings control the updated design.

## Secondary Series

No secondary series changed. The existing 30-second opening-range series and conditional 1-tick true-volume series remain unchanged.

## Tick Replay Implications

No Tick Replay behavior changed. This is a rendering-only update.

## Historical-Load Implications

No historical calculation or hydration behavior changed.

## Cache Implications

No cache behavior changed.

## Rendering Implications

- The panel continues to read only the immutable `RenderStateSnapshot` introduced by the render-stability change.
- DirectWrite formats for title, session headings, right-aligned classifications, details, and status are created with the render target and disposed on render-target changes.
- Text uses fixed line rectangles and no wrapping so rows cannot resize or shift the panel.
- Narrow chart panels clip text within its assigned row; short chart panels stop before drawing a session section that cannot fit.
- Session open-location headers use the same dynamic stats-panel bounds when avoiding the panel.

## Performance Implications

- The panel now issues several small text and line draw calls instead of one multiline text call.
- The number of sections is bounded by the three configured sessions.
- No text formats, brushes, model snapshots, profile calculations, locks, or waits are created inside `OnRender`.

## Tests Performed In NinjaTrader

None in this task yet.

## Compile Status

A local sanitized C# compile against the installed NinjaTrader and SharpDX assemblies passed with no C# errors. NinjaTrader `F5` compile remains pending.

## Manual-Validation Status

Pending Julian's chart validation. Required checks are:

1. Confirm the panel renders after `F5` and NinjaScript reload without a `render skipped safely:` message.
2. Check that all three session sections are readable in compact mode.
3. Toggle `Compact Panel` off and verify the VWAP/EQ row fits.
4. Toggle `Show Only Current Session` and verify the panel height contracts.
5. Check top-left, top-right, bottom-left, and bottom-right panel positions.
6. Increase and decrease `Panel Font Size` and confirm text remains inside the panel.

## Known Issues, Risks, And Follow-Up Work

- Final width, spacing, and divider strength are visual choices and may need tuning from a live screenshot.
- Very short chart panels can omit lower session sections rather than draw them outside the panel.
- This change does not alter which context is selected or how classifications are calculated.

## Full_Suite Promotion

Not eligible. Promotion requires NinjaTrader `F5` success and Julian's manual runtime validation.
