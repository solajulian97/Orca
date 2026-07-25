# Orca Discipline Guard Product Review And Roadmap

Date: 2026-07-25

## Objective

Establish the verified current state, product direction, risks, and implementation order for the Orca Rule Book, Discipline Guard, and Report Card. This review is documentation-only and does not change NinjaTrader behavior.

The product north star is:

> Define the commitment before trading, protect it during trading, and turn the session into one evidence-backed improvement after trading.

## Files Changed

- `docs/ORCA_PRODUCT_STATE.md`
- `docs/handoffs/2026-07-25_orca-discipline-guard_product-review-roadmap.md`

No Working_Suite or Full_Suite source file was changed.

## Sources Reviewed

- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaRiskManagerAddOn.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaExecutionRouterAddOn.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs`
- `docs/ORCA_PRODUCT_STATE.md`
- `docs/ORCA_ARCHITECTURE.md`
- `docs/ORCA_DIAGNOSTICS_SPEC.md`
- `docs/collaboration-workflow.md`
- `docs/engineering-notes.md`
- `docs/indicators/ORCA_SOURCE_MAP.md`
- Recent relevant handoffs and Git history/diffs
- External Orca Journal source at `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal`
- Live Discipline Guard settings, template inventory, diagnostics, and session-directory state under `Documents/NinjaTrader 8/OrcaDisciplineGuard`

## Current Product Overview

### Rule Book

Current behavior:

- Three persisted templates.
- Ten automated rule types and manual checklist rules.
- Add, delete, save, and clone actions.
- Rule enabled state, severity, notes, and parameter editing.
- Mini/micro position normalization, including the requested 2 NQ / 20 MNQ default.

Current limitation:

- Rule editing is based on raw `Key=Value` parameter strings.
- Built-in deleted rules can be restored by default-template migration.
- There is no template rename/delete/version/assignment workflow.
- Manual rules are marked once per session instead of against a specific trade opportunity.

### Discipline Guard

Current behavior:

- Tracks one manually started account session.
- Subscribes to orders, executions, positions, and realized-P&L account events.
- Evaluates automated rules and manual checklist state.
- Shows score, grade, P&L, trade count, violation count, position, cooldown, and loss streak.

Current limitation:

- Monitoring stops when the window closes because the engine belongs to the window view model.
- Sessions do not auto-arm, checkpoint, recover, or finalize.
- Position and realized-P&L handlers can mutate a paused or ended session.
- Late starts and seeded open positions cannot reconstruct full trade history.
- There is no visible data-health or reconciliation state.

### Report Card

Current behavior:

- Creates a single-session score and summary.
- Saves JSON reports and CSV violations on explicit export/end paths.
- Stores rule and violation snapshots.

Current limitation:

- No completed-trade ledger is serialized.
- Violation `TradeId` exists in the data model but is not populated.
- Score is fixed penalty subtraction rather than opportunity-based adherence.
- There is no session browser, weekly trend, rule-cost analysis, coaching focus, or Journal context.

## Validation Evidence

- Working_Suite, Full_Suite, and live NinjaTrader Discipline Guard copies had the same SHA-256 on 2026-07-25: `8A0DCA0376A35CA4023770E5E8F2FDFD4B6D3E7B8D411455C61CD1E77ACAA751`.
- Live diagnostics show repeated successful Control Center menu injection through 2026-07-25.
- Julian previously confirmed the window opened and looked correct after the binding fix.
- Add/delete/template controls and 2 NQ / 20 MNQ normalization are present in deployed source, but explicit live manual validation was not recorded.
- Saved templates exist. The selected template is `Prop Firm Discipline`, the instrument filter is `All Instruments`, and no account is currently persisted.
- No `Documents/NinjaTrader 8/OrcaDisciplineGuard/Sessions` directory existed during this review. This is evidence that no report archive is currently available there; it does not by itself prove a capture defect.

## Behavior Added, Changed, Or Removed

None. This logical change records the product review and implementation roadmap only.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

None.

Recommended future settings:

- Account-to-rule-book assignment.
- Auto-arm mode and session schedule.
- Coach/guard intervention level.
- Typed rule parameters.
- Instrument-family risk units.
- Checkpoint/recovery and auto-finalization policy.

These settings are recommendations, not implemented behavior.

## Secondary Series Added Or Changed

None.

The current Discipline Guard adds no Tick, Second, Bid, Ask, Last, Volumetric, or custom data series.

## Tick Replay Implications

None for the current component and none introduced by this review. Discipline Guard uses account events rather than chart replay.

## Historical-Load Implications

No chart historical-load work is performed.

Trade-history completeness is still a product concern: starting the guard after executions have already occurred cannot reconstruct those executions, and seeded open positions have incomplete entry context. This is session recovery/reconciliation work, not NinjaTrader bar-history work.

## Cache Implications

No Orca profile or chart cache is used.

The component currently persists:

- `Templates.json`
- `Settings.json`
- Session JSON and violation CSV files when export paths run
- `Diagnostics.log`

Phase 0 should define versioned, atomic checkpoints and retention before adding longitudinal analytics.

## Rendering Implications

None. The component uses WPF controls and no SharpDX render path.

Future report-card views should query prepared summaries rather than recompute a full history on the UI thread.

## Performance Implications

The current implementation is low-frequency compared with order-flow indicators: account events plus a one-second UI-dispatcher timer. No controlled benchmark exists.

Phase 0 instrumentation should measure:

- Event counts by type.
- Duplicate or rejected event counts.
- Handler duration.
- Checkpoint duration.
- Reconciliation drift.
- Last-event age.

## Highest-Priority Risks

1. Window close stops monitoring.
2. Manual arming creates silent missing-session risk.
3. Active state cannot recover after shutdown or failure.
4. Paused/ended state can still receive mutations.
5. Built-in rule deletion can be undone during template loading.
6. Reports lack a durable trade-to-rule ledger.
7. Fill-derived trade P&L excludes commissions and can disagree with account data.
8. Penalty-only scoring can misrepresent actual adherence.
9. Fixed micro mappings are not general risk normalization.
10. Discipline Guard and Orca Journal can become competing sources of trade/session truth.

## File-Aware Implementation Plan

### Phase 0: Trustworthy Capture

Primary:

- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs`

Likely extraction targets, subject to NinjaTrader compile constraints:

- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineService.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineModels.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineStore.cs`

Work:

1. Own monitoring at add-on lifetime rather than window lifetime.
2. Gate all event mutation by explicit session state.
3. Add account assignment, auto-arm, checkpoints, restart recovery, and auto-finalization.
4. Store an idempotent execution/fill/round-trip ledger.
5. Link rule opportunities and violations to trade/session IDs.
6. Reconcile against NinjaTrader account data and expose data confidence.
7. Fix built-in deletion migration and add typed parameter validation.
8. Add focused lifecycle, execution-sequence, and risk-unit tests.

### Phase 1: Maintainable Rule Book

Primary:

- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs`
- Phase 0 support files if extracted

Work:

1. Typed editors and plain-language rule definitions.
2. Template rename/delete/version/duplicate/assignment.
3. Instrument-family risk units instead of a global 10x assumption.
4. Pre-session plan, allowed setups, risk, schedule, and stop conditions.
5. Per-trade, session-level, automated, and reflection rule scopes.

### Phase 2: In-Session Coaching

Integration points:

- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaRiskManagerAddOn.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaExecutionRouterAddOn.cs`

Work:

1. Always-visible armed/healthy/paused status.
2. Intervention ladder from record-only through explicit hard guard.
3. Shared policy decision API for every Orca-owned order-submission path.
4. Sim-first enforcement validation and defined stale-data behavior.

### Phase 3: Growth Report Card And Journal Integration

Integration points:

- Discipline Guard session/trade schema.
- External Orca Journal repositories, trade capture, sessions, attachments, tags, and KPI calculator.
- `Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs` annotations.

Work:

1. Choose one canonical trade/session identity and owner.
2. Add opportunity-based process scoring and separate outcome context.
3. Add daily/weekly/rolling adherence and behavior-cost analysis.
4. Add setup, screenshot, note, emotion, and reflection context.
5. Produce one measurable weekly focus and track its trend.

## Acceptance Criteria For The Next Code Change

The first implementation phase is complete only when:

1. A Sim session continues monitoring after the Discipline Guard window closes.
2. Reopening the window attaches to the same active session without duplicate subscriptions.
3. Pause and End prevent prohibited mutations.
4. A checkpointed session survives a controlled NinjaTrader restart.
5. One report contains a non-duplicated trade ledger and populated trade-to-violation links.
6. Completed trade count and realized P&L reconcile to the documented NinjaTrader source.
7. A deleted built-in rule remains deleted after save and restart.
8. 2 NQ and 20 MNQ produce the same configured mini-equivalent exposure result.
9. Observe-only mode cannot block or alter an order.

## Tests Performed In NinjaTrader

None during this documentation-only review.

Prior evidence:

- Window open/basic UI was user-confirmed after the WPF binding fix.
- Live diagnostics show the add-on menu continues to inject successfully.

Add/delete/template persistence, the 2 NQ / 20 MNQ rule, a full session archive, lifecycle recovery, and report reconciliation remain pending manual validation.

## Compile Status

Not run for this documentation-only change.

Source/deployed hash parity was verified, but parity is not a current `F5` compile result.

## Manual-Validation Status

- Window open/basic UI: user-confirmed.
- Add/delete/template persistence: pending.
- Mini/micro normalization: pending.
- Full-session capture/export: pending.
- Window-independent monitoring: not implemented.
- Restart recovery/reconciliation: not implemented.
- Longitudinal Report Card: not implemented.
- Guard enforcement: not implemented.

## Known Issues, Risks, And Follow-Up Work

- The current UI can appear healthy while monitoring is stopped after window close.
- No session archive exists at the expected path to evaluate report quality or real usage.
- Current JSON/CSV persistence may conflict with Orca Journal if both become separate sources of truth.
- Enforcement cannot be added safely until capture health and policy boundaries are explicit.
- All code work must remain in Working_Suite until Julian completes NinjaTrader compile and live manual validation.

## Eligibility For Promotion To Full_Suite

Not eligible based on this review.

Although the current Full_Suite copy is hash-identical to Working_Suite, this review does not establish validation of add/delete/template persistence, micro normalization, session capture, or report accuracy. No further Full_Suite changes should occur until the next Working_Suite implementation passes its compile and manual-validation gates.
