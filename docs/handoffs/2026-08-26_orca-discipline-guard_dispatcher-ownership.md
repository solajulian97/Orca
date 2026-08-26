# Orca Discipline Guard Dispatcher Ownership Fix

Date: 2026-08-26

## Objective

Prevent the Discipline Guard `CollectionView` cross-thread exception reported while account events were updating the active session.

## Evidence And Root Cause

The reported WPF error was:

`This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread.`

Source inspection found two dispatcher owners:

- The add-on-lifetime `OrcaDisciplineGuardEngine` was created on the Control Center dispatcher.
- The menu handler opened `OrcaDisciplineGuardWindow` on `Application.Current.Dispatcher`.

The engine correctly marshaled account callbacks to its own dispatcher, but the window could create WPF collection views for `Rules` and `Violations` on a different dispatcher. A later `Violations.Insert`, `Violations.Clear`, `Rules.Add`, or `Rules.Remove` on the engine dispatcher could therefore trigger the reported exception.

NinjaTrader trace evidence confirms the failing mutation rather than merely inferring it from the screenshot. `trace.20260825.00000.txt` records repeated exceptions beginning at 09:47:46 and continuing through 10:10:14. The stacks terminate at `OrcaDisciplineSession.AddViolation` and arrive through `OnExecutionUpdate` or `OnAccountItemUpdate`, including cooldown, no-add-to-loser, max-trades, and max-session-loss rule paths.

## Files Changed

- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs`
- `docs/ORCA_PRODUCT_STATE.md`
- `docs/handoffs/2026-08-26_orca-discipline-guard_dispatcher-ownership.md`

## Behavior Added, Changed, Or Removed

- The menu handler now asks the existing engine to open or activate the window on the engine's dispatcher.
- The window constructor rejects creation from any dispatcher other than the engine dispatcher.
- A low-frequency diagnostic records when a window request is marshaled across dispatchers.
- Account callback handling and collection mutation behavior are otherwise unchanged.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

None.

## Secondary Series Added Or Changed

None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom series were added or changed.

## Tick Replay Implications

None. Discipline Guard remains account-event driven.

## Historical-Load Implications

None.

## Cache Implications

None.

## Rendering Implications

The Guard remains a WPF-only window. The fix aligns WPF view creation and bound collection mutation on one dispatcher; no SharpDX or chart rendering path changed.

## Performance Implications

No per-event work was added. Dispatcher marshaling already existed for account callbacks. The only new diagnostic occurs when a window-open request originates from a different dispatcher.

## Tests Performed In NinjaTrader

- Target-deployed only `OrcaDisciplineGuardAddOn.cs` from `Working_Suite` to the live NinjaTrader AddOns folder.
- Compared source and live files by line content: 3,962 lines in each and zero differing lines. Raw hashes differ because deployment normalizes line endings.
- Pressed F5 in the NinjaScript Editor. Compilation completed with no error panel or error dialog, and `NinjaTrader.Custom.dll`/`.pdb` regenerated at 10:54:48.
- Opened the Guard from Control Center > Tools after compilation. It displayed `READY`, a healthy heartbeat, and the account-event count advanced from 44 to 60 without an exception.
- Searched the current NinjaTrader log for `CollectionView`, `SourceCollection`, the dispatcher error text, and Discipline Guard failures; no current-day matching error was present after deployment.
- Did not start a live-account session or submit an order. A violation-producing active Sim session remains the decisive runtime reproduction.

## Compile Status

Passed NinjaTrader F5 on 2026-08-26. Source diff, whole-file syntax screen, and dispatcher-ownership structural checks also passed.

## Manual-Validation Status

Partially validated. The deployed window opens and receives account events without error in `NotStarted` state. Active Sim-session violation creation and close/reopen validation remain pending.

## Known Issues, Risks, And Follow-Up Work

- This fixes the proven window/engine dispatcher mismatch. Runtime validation is still required to rule out an independent off-dispatcher mutation path.
- Restart recovery, idempotent execution persistence, reconciliation, and durable report-card work remain separate Phase 0 priorities.

## Full_Suite Eligibility

Not eligible until NinjaTrader compiles the deployed source and Julian confirms the active-session reproduction no longer raises the error.
