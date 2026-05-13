# Roadmap

## Immediate Priorities

1. Validate the current dirty Working_Suite changes in NinjaTrader.

   Scope: deploy only intended targets from `Working_Suite`, press F5 in the NinjaScript Editor, capture compile output, and test chart behavior. Do not promote to Full_Suite until Julia approves.

2. Resolve Working_Suite versus Full_Suite drift deliberately.

   Scope: for each different file, decide whether the Working_Suite change is approved, still in test, or should remain unpromoted. Use `promote_working_to_full_suite.ps1 -Target <FileName>` only after approval.

3. Test DPI-safe coordinate changes across displays.

   Scope: `OrcaPrints.Rendering.cs`, `OrcaVisualOrders.cs`, and `OrcaRiskManagerAddOn.cs`. Verify hover, drag preview lines, submitted order prices, and routed order overlays on laptop and external monitors.

4. Validate MGI Daily session/open/PDC changes.

   Scope: `OrcaMGIDaily.cs`. Test minute and tick charts across ETH open, midnight, RTH open, IB end, RTH close/PDC capture, and overlapping labels.

5. Validate visible-range and candle profile aggregation under load.

   Scope: `OrcaVisibleRangeVolumeProfile.cs`, `OrcaCandleVolumeProfile.cs`, and `OrcaVolumeProfileCore.cs`. Test true volume-at-price, delta, labels, snapshot locking, panning, zooming, and multi-chart layouts.

## Near-Term Engineering Work

- Add or refine a standard NT8 compile handoff template: target files deployed, F5 result, error text, chart/timeframe tested, and promotion status.
- Audit SharpDX disposal and text layout creation in high-frequency render paths.
- Add hysteresis to dynamic aggregation paths that flicker during zoom/pan.
- Clarify whether MGI Weekly/Statistics and other mirror-only files are active modules, future modules, or legacy.
- Review `OrcaExecutionLines.cs` note/tag journaling integration against external Orca Journal so the two systems do not diverge accidentally.

## Workflow Cleanup

- Mark stale scripts clearly or archive them after Julia confirms.
- Update older docs that still say OrcaJournal is missing from this checkout; the adjacent path is accessible but outside the repo.
- Add a short "current module set" document once Julia confirms active versus legacy modules.
- Consider adding a dry-run-first habit to deployment prompts when source-of-truth is ambiguous.

## Product Priorities

- Stabilize visual order/risk workflows because price alignment errors can have direct trading impact.
- Stabilize session levels because MGI Daily is structurally important and time-boundary bugs can mislead chart context.
- Stabilize profile readability/performance because many indicators share profile/delta aggregation concepts.
- Keep journal/trade annotation work aligned with execution lines and external Orca Journal before adding more data capture features.

## Not Recommended Yet

- Broad refactors across the suite.
- Copying mirror files into Working_Suite or Full_Suite.
- Promoting all dirty files at once without per-file compile/behavior confirmation.
- Deleting deprecated stubs or old modules without checking NinjaTrader templates/workspaces.
- Adding new features before the current dirty state is validated.

