# Known Issues And Fragile Areas

## NT8 Ghost Files

Issue: NinjaTrader compiles the whole `Documents/NinjaTrader 8/bin/Custom` tree, so stale `.cs` files can cause duplicate definitions, missing names, or compiler errors unrelated to current repo source.

Mitigation: Inspect live Custom folders directly when errors reference removed names. Do not assume repo source is wrong.

Status: Confirmed platform/workflow risk.

## Current Dirty Working_Suite Changes Are Not Documented As Validated

Issue: Nine core Working_Suite files differ from Full_Suite and are dirty in git. Their compile/runtime status is Unknown from this inspection.

Mitigation: Before continuing work, inspect `git diff` for target files, deploy only requested targets from Working_Suite, press F5 in NinjaTrader, and capture errors before promoting.

Status: Needs confirmation.

## SharpDX Resource Lifecycle

Issue: Many indicators allocate Direct2D/DirectWrite brushes, stroke styles, text formats, text layouts, and geometries. Render target changes can invalidate resources.

Mitigation: Dispose render-target resources in `OnRenderTargetChanged` and termination paths. Avoid unmanaged allocations inside tight render loops unless scoped and justified.

Status: Confirmed platform risk.

## Chart Zoom/Pan Performance

Issue: Profile rendering, text labels, visible-range recalculation, and dynamic aggregation can become expensive during chart pan/zoom.

Mitigation: Cache calculated state, use visible range checks, aggregate dynamically only when needed, and avoid unnecessary text measurement.

Status: Confirmed design concern.

## Dynamic Aggregation Flicker

Issue: Existing docs mention rolling profile delta text jitter when aggregation changes too eagerly during smooth zoom/pan.

Mitigation: Use hysteresis or stable thresholds before changing aggregation/text density.

Status: Confirmed by handoff docs; current behavior needs visual verification.

## Historical Versus Realtime Delta

Issue: Historical data, Tick Replay, and realtime bid/ask data can classify prints differently. Same-price prints and missing bid/ask state are common edge cases.

Mitigation: Use bid/ask when available, preserve last uptick/downtick direction where needed, and test both historical and realtime flows.

Status: Confirmed platform risk.

## WPF Brush Serialization

Issue: NinjaTrader XML serialization can break direct WPF brush properties.

Mitigation: Pair `[XmlIgnore]` brush properties with hidden serializable string properties using `Serialize.BrushToString`/`Serialize.StringToBrush`.

Status: Confirmed pattern.

## Generated NinjaScript Cache Sections

Issue: Hand-editing generated NinjaScript cache/MarketAnalyzer/Strategy sections can create duplicate or stale generated methods.

Mitigation: Avoid editing generated cache sections unless the change requires it and the effect is understood. Let NT8 regenerate when possible.

Status: Confirmed by workflow docs and session notes.

## Stale Or Unsafe Scripts

Issue: Some scripts predate the current source-of-truth workflow.

Known examples:

- `Orca Trades/Scripts/deploy_nt8.ps1` uses stale `C:\Users\Owner` paths.
- `Orca Trades/Scripts/promote_to_full_suite.ps1` copies from the `NinjaTrader` mirror into Full_Suite.

Mitigation: Use `deploy_orca.ps1` and `promote_working_to_full_suite.ps1` unless Julia explicitly asks otherwise.

Status: Confirmed from script contents.

## Deprecated OrcaExecutionLines2 Stub

Issue: `OrcaExecutionLines2.cs` is a deprecated stub stating all functionality was consolidated into `OrcaExecutionLines.cs`.

Mitigation: Do not delete it automatically. Confirm whether NT8 templates/workspaces or user setups still reference it.

Status: Needs confirmation before deletion.

## Orca Journal V1 Reversal Limitation

Issue: External `OrcaJournal/Core/TradeBuilder.cs` comments state V1 does not split reversal fills. A reversal that crosses flat closes the current trade and discards excess quantity.

Mitigation: Implement reversal splitting before treating journal trade capture as robust for reversal workflows.

Status: Confirmed from adjacent project source.

## Orca Journal Build Artifacts In Source Tree

Issue: The adjacent Orca Journal path contains `bin` and `obj` files.

Mitigation: When working there, distinguish source files from generated artifacts and avoid editing generated outputs.

Status: Confirmed from accessible path.

