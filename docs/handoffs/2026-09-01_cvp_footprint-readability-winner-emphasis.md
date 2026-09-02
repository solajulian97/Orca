# CVP Footprint Readability And Winner Emphasis

Date: 2026-09-01

## Objective

Refine the opt-in enhanced Bid x Ask footprint from Julian's first chart review: show Bid/Ask values at a practical zoom, remove diagnostic clutter from row hover, and make a clear same-row auction winner visually scannable. Preserve legacy Volume/Delta and non-enhanced Bid x Ask rendering, strict evidence, normalization and saved widths.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`: winner settings/defaults, technical-status default and settings version 2.
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.Rendering.cs`: tighter text fit, separate winner weight/layouts and concise hover.
- `Orca Trades/Working_Suite/Indicators/OrcaFootprintCore.cs`: pure same-row ratio qualifier.
- `tests/OrcaFootprint.Tests/Program.cs`: winner boundary/zero/invalid-ratio fixtures.
- `tests/OrcaFootprint.PlatformCheck/Program.cs`: new-setting default guards.
- `docs/indicators/ORCA_CANDLE_VOLUME_PROFILE.md`: current visual/text contract.
- This handoff.

## Behavior And Settings

- Enhanced Bid x Ask text keeps exact horizontal measurement and Auto compact fallback. Its layout and draw origin now receive the established two-pixel vertical allowance used by the legacy footprint, allowing 8-point text in tight rows without waiting for another zoom step.
- New `Emphasize Row Winner`, default true, selects only the winning Bid or Ask label for a heavier DirectWrite weight. It is rendering-only and applies only to the Bid x Ask cell-value view.
- New `Winner Ratio`, default 1.5 and range 1.0-10.0. A strictly larger side qualifies when `dominant >= opposing * ratio`; a positive side against zero qualifies as unopposed; ties and 0 x 0 do not. Strict Bid/Ask values are used. Unclassified volume neither qualifies a side nor enters the ratio.
- If the selected base text is Regular/Medium, the winner uses Bold; SemiBold/Bold uses ExtraBold; ExtraBold uses ExtraBlack. Text-layout cache identity now includes font weight.
- Row hover now contains bar/price, Bid x Ask, total, strict delta/percentage, POC and a qualifying winner. It conditionally retains unclassified/unavailable, clipping and preparation-failure warnings. It removes denominator/row size, quality enum, session, numeral fallback, event/sequence/revision, quote age, bar quality and hidden-text narration.
- `Show Data Health` defaults false for new instances. Existing saved true/false values are not migrated. `FootprintSettingsVersion` advances to 2.
- Existing `Bid x Ask Width (px)` remains the sole enhanced-width control. No automatic width migration was made; Julian can lower 96 toward 80 for a slimmer silhouette without losing his saved choice.

## Data, Replay, Cache And Performance Implications

- Secondary series added/changed: none. Tick 1, Tick Replay Last events, `OnMarketData`, `OnBarUpdate` and `Calculate.OnPriceChange` are unchanged.
- Tick Replay/historical load: no attribution, classification, accepted-event or history behavior changed.
- Cache/shared publication: unchanged. No new shared cache access or contract.
- Rendering: one additional cached `TextFormat` per visible font size only when winner emphasis is active. Winner checks occur during prepared-frame construction and hover, not evidence ingestion. `OnRender` remains snapshot-only and draws the already selected layout.
- Legacy Volume, Delta, Volume and Delta, Delta-only and non-enhanced Bid x Ask paths are unchanged. Existing factory signatures and legacy defaults remain token-checked.

## Validation And Status

- Pure model harness: 399 checks passed, including exact 1.5 boundary, below-boundary rejection, unopposed winner, tied-side rejection, 0 x 0 rejection and invalid-ratio rejection.
- Offline installed-platform semantic check: zero errors. Converter placement, legacy token/default/enums/shared-publication parity, enhanced render allocation guard and 13 extracted baseline fixtures passed.
- `git diff --check`: passed for touched code before documentation.
- Targeted deployment: complete for exactly `OrcaCandleVolumeProfile.cs`, `OrcaCandleVolumeProfile.Rendering.cs` and `OrcaFootprintCore.cs`. Post-deploy authored parity passed for all three. No mirror, unrelated source or Full_Suite file was copied.
- NinjaTrader compile evidence: the platform automatically rebuilt `NinjaTrader.Custom.dll` six seconds after the deployed source timestamps. Offline semantic compilation of the live files, including generated wrappers, reported zero errors and passed every platform guard. An explicit NinjaScript Editor F5 action was not sent, so F5 remains a separate user confirmation.
- NinjaTrader chart tests/manual validation: pending. Required checks are label visibility at Julian's screenshot zoom, no vertical collisions, both winner sides at 1.5 and 2.0, zero-side rows, Figtree/base-weight combinations, hover contents, status toggle and width near 80.
- Full_Suite eligibility: **No** until Julian confirms compile and chart behavior.

## Risks And Rollback

- The two-pixel allowance intentionally matches the legacy footprint's tight-row behavior, but actual glyph metrics vary by font/DPI. If labels collide, disable text, increase Display Row Size, reduce font size or revert this focused rendering change after capturing the failing font/DPI/zoom.
- Winner emphasis is not diagonal imbalance, stacked imbalance, absorption or an order-flow signal. It expresses only the strict same-row Bid/Ask ratio and does not persist an observation.
- Disable `Emphasize Row Winner` for the previous uniform text weight. Disable Enhanced Footprint for the complete legacy visual rollback. Checkpoints `34d7faf`, `541eb21` and `b29fdcb` remain intact.
