# Decisions Log

## D001: Working_Suite Is The Editable Source

Decision: Use `Orca Trades/Working_Suite/Indicators` and `Orca Trades/Working_Suite/AddOns` as the editable local source of truth.

Rationale: `AGENTS.md`, `CLAUDE.md`, `README.md`, and `docs/collaboration-workflow.md` all converge on this rule. It prevents agents from copying stale mirror or live NinjaTrader files over current work.

Status: Confirmed.

## D002: Full_Suite Is Validated Only

Decision: Treat `Orca Trades/Full_Suite` as the clean validated suite, not a work area.

Rationale: Full_Suite should only receive files after Julia confirms they compiled and behaved correctly in NinjaTrader.

Status: Confirmed.

## D003: Do Not Copy From Mirrors Or Decompiled Sources Without Explicit Approval

Decision: Do not copy from `Orca Trades/NinjaTrader`, `Stable_Release`, `decompiled`, or live NinjaTrader Custom folders into Working_Suite or Full_Suite unless Julia explicitly confirms that exact source should replace the current version.

Rationale: These areas can contain stale live mirrors, old release snapshots, generated code, decompiled platform references, or experiments.

Status: Confirmed.

## D004: Preferred Deployment Script Is deploy_orca.ps1

Decision: Use `Orca Trades/Scripts/deploy_orca.ps1` for deployment from Working_Suite by default.

Rationale: It is repo-relative, supports `-Target`, defaults to Working_Suite, supports DryRun, and tells Julia to press F5 in NinjaTrader after copying.

Status: Confirmed.

## D005: Promotion Uses promote_working_to_full_suite.ps1

Decision: Use `Orca Trades/Scripts/promote_working_to_full_suite.ps1 -Target <FileName>` only after Julia approves compile and behavior.

Rationale: It copies from current Working_Suite into Full_Suite and matches source-of-truth rules.

Status: Confirmed.

## D006: Older Promotion Script Is Unsafe For Current Rules

Decision: Avoid `Orca Trades/Scripts/promote_to_full_suite.ps1` unless Julia explicitly asks for it.

Rationale: It copies from `Orca Trades/NinjaTrader` mirror into `Full_Suite`, which can create retrograde changes under the current workflow.

Status: Confirmed from script contents.

## D007: Stale deploy_nt8.ps1 Should Not Be Used

Decision: Do not use `Orca Trades/Scripts/deploy_nt8.ps1` for current workflow.

Rationale: It has hard-coded `C:\Users\Owner\.gemini\antigravity\scratch` paths and deploys only `OrcaCandleVolumeProfile.cs`.

Status: Confirmed from script contents.

## D008: SharpDX Rendering Requires Explicit Lifecycle Discipline

Decision: Keep SharpDX render resources tied to render targets and dispose/recreate them on `OnRenderTargetChanged` and termination paths.

Rationale: Existing docs and source show heavy SharpDX usage. NT8 chart render-target changes can invalidate brushes and leak resources.

Status: Confirmed pattern and platform risk.

## D009: WPF Brushes Need Serializable Companions

Decision: For NinjaScript user properties, keep WPF brush properties `[XmlIgnore]` and expose string companion properties using `Serialize.BrushToString` and `Serialize.StringToBrush`.

Rationale: NinjaTrader XML serialization does not safely serialize WPF brushes directly.

Status: Confirmed pattern.

## D010: Historical And Realtime Order Flow Can Differ

Decision: Do not assume bid/ask state is always available historically. Fallbacks and Tick Replay behavior must be considered for delta/order-flow features.

Rationale: Existing docs and source use bid/ask when available and fallback tick-direction classification in several places.

Status: Confirmed platform constraint.

## D011: Orca Journal Is Adjacent, Not Part Of This Repo Tree

Decision: Document Orca Journal as an external adjacent project at `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal\`, not as current Orca Codex repo source.

Rationale: The path is accessible, but its files live outside `C:\Users\julia\Documents\Orca Codex`.

Status: Confirmed accessible. Integration scope needs Julia confirmation.

## D012: Orca Journal Uses SkiaSharp Instead Of LiveCharts2 WPF Controls

Decision: Treat SkiaSharp bitmap rendering as the current journal chart rendering approach.

Rationale: `OrcaJournal.csproj` and `Analytics/ChartRenderer.cs` state LiveCharts2 WPF controls were removed to avoid NT8 D2D visual-tree conflicts.

Status: Confirmed from Orca Journal source.

## D013: Current Dirty Working_Suite Changes Are Unapproved Until Julia Says Otherwise

Decision: Document dirty changes, but do not promote or assume validated behavior.

Rationale: Current state contains uncommitted changes in active Working_Suite files. Source docs require Julia approval before Full_Suite promotion.

Status: Needs confirmation from Julia for compile/behavior approval.

