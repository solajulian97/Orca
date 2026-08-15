# Orca Risk Manager ATR Sizing And Bracket Automation Plan

Date: 2026-08-15

Status: design review only; no indicator or order-submission logic changed

## Objective

Define a clean, low-friction way to add chart-timeframe ATR position sizing, configurable protective OCO orders, multi-bracket exits, and automatic break-even behavior to Orca Risk Manager without weakening its current routing, calculator, staged-bracket, dormant-panel, or execution-safety behavior.

## Product Decision

ATR sizing belongs in Orca Risk Manager as a fourth sizing mode. The visible mode row should become:

`$ Risk | Qty | Pts | ATR`

The ATR mode should use the primary chart bars as its volatility source, use the resolved execution instrument's point value for dollar sizing, and produce one immutable trade-plan snapshot when an entry is submitted. ATR can continue updating the preview for the next trade, but it must not resize an open position or move a submitted trade's stop.

Sizing and order protection should be implemented as two layers:

1. A small calculation layer owns ATR, stop-distance, risk-per-contract, and quantity math.
2. An AddOn-lifetime protection engine owns entry plans, account subscriptions, OCO legs, partial fills, break-even state, and recovery. The WPF panel remains a view/coordinator and must not remain the sole owner of live protection.

## Confirmed Design Decisions

Julian confirmed the following on 2026-08-15:

- ATR updates only after the active primary chart bar completes.
- ATR mode keeps manual overrides. Dragging the stop changes the effective ATR multiplier and recalculates quantity; directly editing quantity switches the sizing mode to `Qty` so the panel does not mislabel a manual size as ATR risk sizing.
- Every new Risk Manager entry path receives protection when Auto Trade Management is armed.
- The default bracket is `2 Targets`; percentages and target R values are editable in settings. The initial proposed template is 50% at 1R and 50% at 2R.
- Auto break-even uses the proposed one-way 1R trigger, average-fill break-even price, zero-tick offset, and remaining-Orca-stop scope.
- Scale-ins remain supported. Adding contracts to an existing account/instrument position must increase the total protective stop and target quantities to the actual account position rather than blocking the entry.
- ATR sizing requires a maximum-quantity cap. The working proposed default remains 20 unless Julian selects another value.
- The panel keeps the existing Close Position/Flatten placement. A new `Auto Trade Management` section appears immediately below the Flatten area, before Manage Position/PnL.

## Current Risk Manager Behavior Reviewed

Primary source reviewed:

- `Orca Trades/Working_Suite/AddOns/OrcaRiskManagerAddOn.cs`

Related routing source reviewed:

- `Orca Trades/Working_Suite/AddOns/OrcaExecutionRouterAddOn.cs`

Current capability in source:

- One Risk Manager panel is injected into a chart window and follows the active chart tab.
- `Ctrl+C` lazily creates, shows, and hides the panel; panels are dormant by default unless `Open panels on startup` is enabled.
- Settings persist in `Documents/NinjaTrader 8/OrcaRiskManager.xml`.
- The settings window controls hotkeys, live-order confirmation, panel visibility and style, chart labels, and the Spacebar Bracket Builder.
- The panel exposes Quick Actions, Position Sizing, Fast Execution, Close Position, and Manage Position/PnL sections.
- Current sizing modes are fixed-dollar risk, fixed quantity, and fixed points. The current panel fields are Risk $, Risk Pts, and Contracts.
- The calculator creates draggable entry, stop, and target lines and labels.
- The Spacebar Bracket Builder can preview, stage, drag, place, or cancel a single entry/stop/target plan.
- Quick, fast, and drag-order entry paths submit through NinjaTrader's account API.
- `OrcaExecutionRouter.ResolveExecutionInstrument(...)` can keep the chart-price source on NQ/ES while sizing and submitting on MNQ/MES.
- Calculator and Spacebar entries already create a stop/target OCO pair after each entry execution when pending stop and target prices exist.
- A manual `Move To Breakeven` action already attempts to move stop-market orders to the account position average price.
- Routed position and working-order overlays support labels, drag-to-change, quantity changes, cancellation, and protection controls.
- PnL, points, unrealized R, realized R, partial closes, and flatten are already present.

## Current Architecture Findings

Facts from source inspection:

- Live plan state is panel-owned through one `pendingEntryName`, one pending stop, and one pending target.
- The panel subscribes to `Account.ExecutionUpdate`; it does not own a complete per-plan `OrderUpdate`/`ExecutionUpdate`/`PositionUpdate` state model.
- Each partial fill can generate a separate OCO stop/target pair because the handler uses the execution quantity.
- Protection synchronization searches working orders by instrument and generic names `Stop` and `Target`. This scope is too broad for multiple simultaneous Orca plans or multiple bracket legs.
- Automatic OCO protection is implicit rather than a visible setting, and the fast/drag entry paths do not all create a protected trade plan.
- Manual break-even scans stop-market orders for the instrument rather than only orders owned by one Orca trade plan.
- Hidden panels can retain their execution subscription when a pending entry or open position needs protection, but order management still belongs to the individual panel.
- Several order paths suppress exceptions. A multi-bracket engine needs bounded diagnostics for rejects, change failures, stale plans, and reconciliation instead of silent failure.

Conclusion:

- ATR preview and quantity calculation can be added narrowly to the existing panel.
- Multi-bracket and automatic break-even should not be layered onto the existing single pending-entry fields. They require a plan-aware protection engine first.

## ATR Sizing Specification

### Source

- Use the active tab's primary chart bars and trading-hours/session construction.
- Do not add a secondary series, `BarsRequest`, hidden indicator, Tick Replay dependency, or shared-cache dependency.
- Use NinjaTrader/Wilder ATR smoothing with a configurable length; default recommendation is 14.
- Use the latest completed primary bar by default so quantity does not flicker intrabar. A live-forming-bar option is deferred until the closed-bar behavior is validated.
- Rebuild the small ATR state when the attached tab, instrument, bars period, loaded bars, or ATR length changes; update it incrementally after that.
- Display the source beside the result, for example `ATR 14 | MNQ 5 Minute | closed`.

### Math

For an ATR value in chart-price points:

```text
raw stop points       = ATR * stop multiplier
stop ticks            = ceiling(raw stop points / execution tick size)
stop points           = stop ticks * execution tick size
risk per contract     = stop points * execution instrument point value
recommended quantity  = floor(risk budget / risk per contract)
planned dollar risk   = recommended quantity * risk per contract
```

The stop distance should round outward to a valid execution-instrument tick so rounding never makes the requested ATR stop tighter.

The chart instrument supplies the ATR price distance. The routed execution instrument supplies tick size and point value. Example: a 10-point NQ-chart ATR with a 2x multiplier produces a 20-point stop; routed MNQ risk is $40 per contract, so a $200 budget produces 5 contracts. Without routing, NQ risk is $400 per contract, so the same budget produces quantity zero.

Quantity zero must be a valid safety result. The existing `Math.Max(1, ...)` behavior must not be reused for ATR mode because that can knowingly exceed the risk budget. Show `No trade - 1 contract risks $400` and disable submission until the user changes risk, multiplier, routing, or mode.

Add a configurable `Max ATR quantity` safety cap, with a recommended initial default of 20. Show `CAP` in the preview whenever the cap, rather than risk math, determines quantity.

The displayed amount is planned stop risk and cannot include stop slippage, gaps, commissions, or fees. The UI should say `planned risk` where space permits.

### Snapshot Rule

At submission, create an immutable plan containing:

- Plan ID and timestamp
- Account
- Chart instrument and chart bars-period label
- Execution instrument
- Direction and entry order type/price
- ATR length, ATR value, and stop multiplier
- Rounded stop ticks/points
- Execution-instrument tick size and point value
- Risk budget, recommended quantity, and planned risk
- Target/bracket template
- Break-even rule

All protective prices should be based on the actual fill price plus/minus the frozen stop distance. Later ATR changes affect only the next preview.

## Proposed Panel Experience

Keep the existing four-section panel and add no new top-level window.

When ATR is selected, the Position Sizing section should be compact:

```text
$ Risk | Qty | Pts | ATR
Risk $       [ 200 ]
ATR          [ 14 ] x [ 2.00 ]
Stop         20.00 pts | $40/contract
Quantity     5 | planned risk $200
```

Interaction rules:

- Risk, ATR length, and ATR multiplier are editable session values seeded from settings.
- Stop distance, risk per contract, quantity, and planned risk are outputs.
- `-1`, `+1`, and direct quantity edits should either be disabled in ATR mode or explicitly switch to `Qty` mode; ATR mode must not silently claim fixed-risk sizing after a manual size override.
- `Calc On` and the Spacebar builder should show the same frozen-plan preview model rather than separate sizing formulas.
- Dragging an ATR stop can update the session multiplier and recompute quantity, but the UI must visibly show the changed multiplier.
- Moving the entry should preserve the stop/target distances until submission.
- Changing chart tab, chart instrument, bars period, or routing should invalidate and rebuild the preview.
- `Open` must be disabled if ATR is unavailable, there are insufficient bars, quantity is zero, the execution instrument cannot be resolved, or automatic protection cannot be armed.

The existing Close Position section and Flatten button remain in their current position. Immediately below Flatten, add a separate section:

```text
AUTO TRADE MANAGEMENT
Bracket      [ 2 Targets v ]
Auto BE      [ On ] at [ 1.0R ]
Protection   ARMED | 50% @ 1R / 50% @ 2R
```

This location keeps Julian's primary Flatten exit accessible while separating emergency/manual exit actions from the automation configuration. The current manual `Move To Breakeven` action should move into Manage Position; the Auto Trade Management row configures the automatic rule for new and active Orca plans.

## Proposed Settings Experience

Add three concise sections to the existing `Tools > Orca Risk Manager` settings window.

### Position Sizing

- Default sizing mode: `$ Risk`, `Qty`, `Pts`, or `ATR`
- Default risk amount
- ATR length, default 14
- ATR stop multiplier, default 2.0
- Max ATR quantity, recommended default 20

### Bracket Protection

- Attach stop and targets to Orca entries
- Bracket template: `Single`, `2 Targets`, or `3 Targets`; default `2 Targets`
- Target rows appear only for the selected template and contain allocation percentage and target R
- Recommended starting templates:
  - Single: 100% at 2R
  - 2 Targets: 50% at 1R, 50% at 2R
  - 3 Targets: 50% at 1R, 25% at 2R, 25% at 3R

No open-ended runner or trailing-stop mode is included in the first implementation. Those require separate exit rules and would make the first surface less predictable.

### Auto Break-Even

- Enable auto break-even, default off until live validation is complete
- Trigger at R, recommended default 1.0R
- Offset ticks, recommended default 0
- Apply to all remaining Orca bracket legs

The panel should summarize these values but not expose every bracket row during normal trading.

## OCO And Multi-Bracket Model

Each active bracket leg must own its own OCO pair:

```text
Leg 1: Stop qty 3 <OCO-A> Target 1 qty 3
Leg 2: Stop qty 1 <OCO-B> Target 2 qty 1
Leg 3: Stop qty 1 <OCO-C> Target 3 qty 1
```

Do not place one common stop and all targets under one OCO ID. The first filled target could cancel every sibling order and leave the remaining position unprotected.

Allocation rules:

- Percentages must total 100 in settings.
- Convert percentages to whole contracts with a deterministic largest-remainder allocation.
- Skip zero-quantity legs when total quantity is smaller than the selected template.
- Show the actual allocation before submission, for example `5 = 3 / 1 / 1`.
- Protect every partial fill immediately. Allocate each new execution quantity into the remaining planned legs and create OCO protection for that filled quantity; never wait for the full entry to fill before protecting it.

Order ownership rules:

- Use plan-specific order names and OCO IDs, not generic global `Stop` and `Target` names.
- Reconcile only Orca Risk Manager orders that belong to the same plan and leg.
- Do not change or cancel unrelated manual, ATM, strategy, or other Orca orders on the instrument.
- Track submitted, accepted, working, part-filled, filled, cancelled, and rejected states through account events.
- Make execution handling idempotent by execution ID so duplicate/amended callbacks cannot create duplicate protection.

## Scale-In And Position Reconciliation Model

An active account plus execution instrument is treated as one Orca position campaign. A second Risk Manager entry on the same campaign is allowed and joins the existing protected position.

Required behavior:

- Reconcile protection to the actual absolute account position after every relevant execution or position update.
- Example: a 10-contract position with a 50/50 template has desired target quantities 5 and 5. Adding 2 contracts produces a 12-contract position and desired quantities 6 and 6. Total working stop quantity must also equal 12.
- Preserve the existing campaign stop and target price levels when scaling in. Scale-in changes quantities; it does not automatically move the original stop farther away or re-anchor targets to the newest fill.
- Continue to calculate and display the account's weighted average fill price for PnL and break-even purposes.
- Apply deterministic whole-contract allocation to the new total position, adjusting only the quantity delta required to reach the desired leg totals.
- If a target leg has already completed, do not recreate it during a later scale-in. Allocate new contracts only across the remaining active target legs.
- If auto break-even has already triggered, later scale-in quantity inherits the current protective stop. The engine must never move that stop backward or reset the break-even latch.
- Manual partial exits reduce the desired remaining protection quantities. Flatten cancels the campaign's remaining Orca protection after the position reaches flat.
- Only Risk Manager-owned plan/leg orders participate. The reconciliation engine must not absorb, resize, or cancel unrelated manual, ATM, strategy, or other Orca orders.

This preserves the current user expectation that adding 2 contracts to a protected 10-contract position adjusts protection to 12 while giving multi-target plans deterministic allocation and order ownership.

## Auto Break-Even Model

- The trigger is based on the frozen initial stop distance: long trigger = average fill + stop distance * trigger R; short trigger is the inverse.
- Use a temporary AddOn `MarketData` subscription only while an eligible Orca plan is live.
- On the first qualifying Last-price event, latch the trigger and move only working Orca stops for the remaining legs to the tick-rounded average fill plus/minus the configured offset.
- Never move a stop backward. If a stop is already more protective than the requested break-even price, leave it unchanged.
- Stop and target OCO membership must remain intact after the change.
- Unsubscribe when no active plan needs price monitoring.
- No WPF dispatcher wait, rendering work, file I/O, or allocation-heavy work may occur in the market-data callback.
- Auto break-even should not be enabled for live accounts until change/reject behavior is proven on the actual connection technology.

## Proposed Source Boundaries

Planned source changes:

- Modify `Orca Trades/Working_Suite/AddOns/OrcaRiskManagerAddOn.cs`
  - Settings fields and settings UI
  - Fourth ATR mode and compact preview
  - One canonical submission path for Quick Actions, Spacebar, fast, and drag entries
  - View binding to sizing and protection snapshots
- Add `Orca Trades/Working_Suite/AddOns/OrcaRiskSizing.cs`
  - Pure sizing models
  - Incremental Wilder ATR state over primary chart bars
  - Tick rounding, quantity, cap, allocation, and validation results
- Add `Orca Trades/Working_Suite/AddOns/OrcaRiskProtectionEngine.cs`
  - AddOn-lifetime account subscriptions
  - Immutable trade plans and active-plan state
  - Partial-fill protection, plan/leg OCO ownership, order reconciliation, market-data break-even triggers, and bounded diagnostics

`OrcaExecutionRouterAddOn.cs` should remain the execution-instrument authority. ATR work should call its existing resolver rather than duplicating NQ/MNQ or ES/MES mappings.

The future Discipline Guard pre-submit policy should have one integration point in the canonical Risk Manager submission path; ATR sizing should not directly depend on Discipline Guard in the first implementation.

## Implementation Sequence

### Phase 1 - ATR Preview And Pure Sizing

- Add settings and the fourth mode.
- Calculate closed-bar Wilder ATR from the active primary chart bars.
- Add zero-quantity and max-cap safety states.
- Make calculator and Spacebar previews consume one sizing result.
- Do not change order submission or protection behavior in this phase.

Acceptance: ATR matches NinjaTrader's built-in ATR for the same chart/period within tick-display precision; NQ/MNQ and ES/MES calculations use the resolved execution point value; zero/cap states cannot submit.

### Phase 2 - Plan-Aware Single-Bracket Engine

- Introduce the immutable trade plan and AddOn-lifetime protection engine.
- Route existing single-stop/single-target submissions through it.
- Subscribe once per relevant account and use plan-specific order ownership.
- Handle partial fills, cancellations, rejects, panel hide, tab changes, scale-ins, manual partial exits, and multiple instruments/accounts.

Acceptance: one protected Sim trade remains managed when the panel is hidden or its chart tab changes; a 10-contract position scaled to 12 has exactly 12 protected contracts; manual reductions shrink protection; unrelated orders are untouched; each fill is protected once.

### Phase 3 - Multi-Bracket Templates

- Add two- and three-target settings.
- Allocate whole contracts deterministically.
- Create one OCO stop/target pair per active leg.
- Display actual leg allocation and state.

Acceptance: single, two-leg, and three-leg Sim trades handle full fills, partial fills, scale-ins before and after a target fill, stop fills, cancellations, and quantity changes without an uncovered remainder or resurrected completed target.

### Phase 4 - Automatic Break-Even And Recovery

- Add the temporary market-data subscription and one-way R trigger.
- Persist only state transitions for active plans; do not perform per-tick disk writes.
- Reconcile persisted Orca plan state with account orders/positions after startup before re-arming automation.
- Keep live-account auto break-even disabled until Sim and provider-specific validation pass.

Acceptance: remaining stops move once, never backward, retain their OCO behavior, survive panel closure, and recover or visibly fail safe after NinjaTrader restart.

## Files Changed

- `docs/ORCA_PRODUCT_STATE.md`
- `docs/handoffs/2026-08-15_orca-risk-manager_atr-sizing-bracket-automation-plan.md`

No `Working_Suite` or `Full_Suite` source file was changed by this design review.

## Behavior Added, Changed, Or Removed

None. This pass documents current behavior and a proposed implementation sequence only.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

None in source. Proposed settings are documented above.

## Secondary Series Added Or Changed

None. The design explicitly avoids new Tick, Second, Bid, Ask, Last, Volumetric, or custom bar series.

## Tick Replay Implications

None for the proposed ATR source. It reads the primary chart bars already loaded by the chart and does not require Tick Replay.

## Historical-Load Implications

Phase 1 requires one bounded ATR initialization over already-loaded primary chart bars when the panel/tab/period/length changes, then incremental updates. No new historical request is proposed.

## Cache Implications

No shared profile/order-flow cache use is proposed. ATR state is panel/chart-local and bounded. Active trade-plan recovery may use a small transition-based JSON state file in a later phase; it must not write on every price event.

## Rendering Implications

No SharpDX change is proposed. Existing WPF labels and overlays should consume immutable sizing/protection snapshots. No ATR calculation, order mutation, or account lookup should be added to `OnRender`.

## Performance Implications

- Phase 1 reuses the existing low-frequency panel refresh and performs bounded O(1) incremental ATR work after initialization.
- No `AddDataSeries`, `BarsRequest`, or persistent market-data subscription is needed for sizing.
- Auto break-even later adds one lightweight Last-price subscription per active execution instrument and removes it when no plan needs it.
- Account callbacks must update engine state without synchronous WPF dispatcher waits.

## Tests Performed In NinjaTrader

None. This was a source/documentation review.

## Compile Status

Not applicable. No NinjaScript source was changed or deployed.

## Manual-Validation Status

Planning only. Every implementation phase requires separate deployment, NinjaTrader F5 compilation, and Julian's Sim/live-behavior validation.

## Known Issues, Risks, And Follow-Up Work

- `OrcaRiskManagerAddOn.cs` already contains substantial unrelated uncommitted Risk Manager work. Future source changes must preserve and review that diff before editing.
- Recent Calc label dragging, execution-dispatch cleanup, dormant-panel lifecycle, and PnL/font changes have separate pending compile or live-validation gates in their existing handoffs.
- Planned stop risk cannot guarantee realized maximum loss because stop slippage, gaps, commissions, and fees remain outside the sizing formula.
- Broker/connection behavior for changing OCO-linked stops must be tested. Rejections cannot be silently ignored.
- Partial fills, simultaneous plans, routing, chart-tab changes, panel closure, and restart recovery are the critical correctness cases.
- Scale-ins make campaign ownership, price preservation, completed-leg handling, and exact protection-quantity reconciliation additional critical correctness cases.
- The supplied YouTube page did not expose a transcript during this review. The linked Jack Gleason page confirms the reference product's ATR Expansion Bar, live risk calculator, and NinjaTrader-ready positioning, but not its internal order-management rules.

## Eligibility For Promotion To Full_Suite

Not eligible. No implementation exists, and no source should be promoted before phased NinjaTrader compile and Julian's manual Sim/live validation.

## External References Reviewed

- https://www.youtube.com/watch?v=jf0P7-YTZUY
- https://jackgleason.com/atr-checkout
- https://jackgleason.com/
- https://ninjatrader.com/support/helpguides/nt8/average_true_range_atr.htm
- https://ninjatrader.com/support/helpGuides/nt8/createorder.htm
- https://ninjatrader.com/support/helpGuides/nt8/executionupdate.htm
- https://ninjatrader.com/support/helpguides/nt8/marketdata.htm
- https://ninjatrader.com/support/helpguides/nt8/using_onorderupdate_and_onexec.htm
