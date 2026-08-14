# Orca Manual Anchored VWAP Start-Anchor Open Snap

Date: 2026-08-14

## Objective

Make the `OrcaManualAnchoredVWAP` start anchor select a candle by horizontal position and automatically place the anchor price at that candle's open, even when the initial click is above or below the candle.

## Files Changed

- `Orca Trades/Working_Suite/DrawingTools/OrcaManualAnchoredVWAP.cs`
- `docs/handoffs/2026-08-14_orca-manual-anchored-vwap_start-anchor-open-snap.md`

## Behavior Added, Changed, Or Removed

- Initial start-anchor placement now resolves the nearest loaded chart bar from the click time.
- The start anchor's time is normalized to the selected bar time and its price is set to `Bars.GetOpen(barIndex)`.
- Directly dragging the start handle applies the same bar-open snap, including the final mouse-up position.
- The endpoint remains freely positioned.
- Moving the entire drawing preserves the existing translation behavior and does not force a new candle-open snap.

## User-Facing Settings

- No settings were added, changed, deprecated, or removed.
- Start-anchor open snapping is the default interaction behavior.

## Secondary Series

- No Tick, Second, Bid, Ask, Last, Volumetric, or custom data series were added or changed.

## Tick Replay Implications

- None. The interaction reads the primary chart's already-loaded `Bars` collection.

## Historical-Load Implications

- The nearest loaded chart bar is used.
- Clicks outside the loaded range clamp to the first or last available chart bar through the existing nearest-bar resolver.

## Cache Implications

- No cache structure or rebuild policy changed.
- Existing `MarkVwapDirty()` calls still invalidate the calculated VWAP after start-anchor placement or editing.

## Rendering Implications

- No SharpDX rendering path or resource lifecycle changed.
- The first rendered VWAP point now begins at the selected candle's open because the existing calculation uses `StartAnchor.Price` for that point.

## Performance Implications

- Start-anchor placement or dragging performs one bounded nearest-bar lookup and one open-price read per interaction update.
- No work was added to `OnRender`, `OnBarUpdate`, or a per-tick path.

## Tests Performed In NinjaTrader

- Deployed only `OrcaManualAnchoredVWAP.cs` from `Working_Suite` to `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\DrawingTools`.
- Verified the deployed file matches the source after normalizing CRLF/LF line endings.
- F5 automation was attempted through the existing NinjaScript Editor, but Windows-control approval timed out before activation; no NinjaTrader compile result was produced.
- No live chart placement test was performed in this task.

## Compile Status

- `git diff --check` passed for the source change, with only Git's existing LF-to-CRLF warning.
- NinjaTrader F5 compile: not verified.

## Manual-Validation Status

- Pending Julian's live test: place the start above or below a candle, confirm the handle and VWAP begin at that candle's open, then drag the start handle to another candle and confirm it snaps to the new open.

## Known Issues, Risks, And Follow-Up Work

- Final NinjaTrader compile and live chart interaction remain unverified.
- Whole-object movement intentionally preserves relative geometry; only initial placement and direct start-handle editing snap to candle opens.

## Promotion Eligibility

- Not eligible for promotion to `Full_Suite` until NinjaTrader compiles successfully and Julian confirms the live drawing interaction.
