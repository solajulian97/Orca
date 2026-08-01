# Orca Discipline Guard Background Monitoring And Runtime Health

Date: 2026-08-01

## Objective

Move Discipline Guard monitoring from window lifetime to AddOn lifetime, freeze non-active sessions, expose runtime trust signals, and improve the operational layout without promoting unvalidated code to `Full_Suite`.

## Files Changed

- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs`
- `docs/ORCA_PRODUCT_STATE.md`
- `docs/handoffs/2026-08-01_orca-discipline-guard_background-monitoring-health.md`

## Behavior Added, Changed, Or Removed

- Added one static AddOn-owned runtime engine created from the Control Center lifecycle and disposed only when the AddOn terminates.
- Changed the window and view model into UI attachments to the existing runtime; closing the window no longer disposes account subscriptions or the timer.
- Preserved runtime account, template, instrument, and session selection across window reopen.
- Added strict `Active` gating to order, position, account-item, timer, manual-rule, and violation mutation paths.
- Removed a duplicate position-rule evaluation after round-trip tracker synchronization.
- Added subscription state, connection state, heartbeat time, last account event time, and account event count.
- Added idempotent runtime disposal with dispatcher-safe cleanup.
- Replaced the internal `TabControl` with a Session/Summary segmented switcher to avoid NinjaTrader's multiple-tab close path.

## Design Changes

- Added an operational health band with `READY`, `ARMED`, `PAUSED`, `ENDED`, and `OFFLINE` states.
- Added heartbeat freshness, last account event, and event count without creating a separate diagnostics screen.
- Grouped session controls on the left and Rule Book controls on the right.
- Reduced title size and changed the header status from unconditional green to neutral.
- Kept the visual language quiet and dense for repeated trading use; no new icon or illustration dependency was introduced.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

None. Existing templates, rules, account selection, instrument filter, and mini/micro settings are unchanged.

## Secondary Series Added Or Changed

None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom data series were added or changed.

## Tick Replay Implications

None. Discipline Guard remains account-event driven and does not depend on Tick Replay.

## Historical-Load Implications

No chart historical-load behavior changed. Active-session restart recovery is still absent, so this change does not reconstruct events that occurred before the runtime was available.

## Cache Implications

None. No shared profile or indicator cache is used. Runtime session and health state remain in memory.

## Rendering Implications

The UI uses WPF only and no SharpDX path. Health text refreshes from the existing one-second engine timer. The segmented selector replaces a WPF `TabControl` but keeps both views instantiated for immediate switching.

## Performance Implications

- Account monitoring and the one-second timer now continue while the Guard window is closed.
- Health tracking adds timestamp and integer updates only and performs no per-event file I/O.
- Window subscribers are removed on close, so the closed UI does not receive property notifications.
- Lifecycle diagnostics are low frequency.
- The live counter showed frequent account events; bounded handler timing and duplicate/rejected-event counters remain future Phase 0 diagnostics.

## Tests Performed In NinjaTrader

- Deployed the targeted Working_Suite source to the live AddOns folder.
- Pressed F5 in the NinjaScript Editor; the compile completed with no error rows or error dialog.
- Opened the Guard from Control Center > Tools and observed `READY`, a healthy runtime heartbeat, last account event time, and event count.
- Switched from Session to Summary and verified summary/export controls rendered.
- Closed the repaired window using NinjaTrader's normal single-window confirmation; no error appeared.
- Reopened the Guard and observed the event count advance from 64 to 760 while the UI had been closed.
- Did not start a session, submit an order, or interact with account positions.

## Compile Status

Passed NinjaTrader F5 on 2026-08-01. Roslyn syntax parsing and `git diff --check` also passed.

## Deployment Status

Working_Suite and live `Documents/NinjaTrader 8/bin/Custom/AddOns/OrcaDisciplineGuardAddOn.cs` share SHA-256 `CD61E7789F9B8477BA1AE0054A486936B1D9F77CBC9E334364D94746B8A6500B`.

## Manual-Validation Status

Partially validated live. Window open, view switching, clean close, clean reopen, heartbeat, and continued event collection were observed in `NotStarted` state. An active Sim session surviving close/reopen is still pending Julian validation.

## Known Issues, Risks, And Follow-Up Work

- No periodic checkpoints, NinjaTrader restart recovery, or automatic finalization.
- No idempotent execution ledger or durable duplicate-event rejection.
- Completed trades and violations are not yet linked through durable IDs.
- Session trade/P&L totals are not reconciled against NinjaTrader with a confidence status.
- Default-rule deletion semantics and typed rule editors still need work.
- The penalty-only score should not be treated as a trusted growth metric until opportunity capture and reconciliation exist.

## Full_Suite Eligibility

Not eligible. Keep this change in `Working_Suite` until Julian validates an active Sim session and the remaining Phase 0 recovery/reconciliation gates are complete.
