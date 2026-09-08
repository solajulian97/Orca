# Risk Manager tab binding and entry contract guard

Date: 2026-09-08

Status: source patch, offline checks and targeted deployment complete; NinjaTrader F5 and manual validation pending.

## Objective

Start the coordinated inactive-tab reliability work with Risk Manager/Execution Router because Julian reports actual ES entries when MES routing is enabled. Julian confirmed all entries are submitted through Risk Manager: limit buttons, calculator mode or Spacebar. Delta/Time Statistics remain the next separate workstream.

## Evidence, facts and hypotheses

- Before editing, Risk Manager, Router, Cumulative Delta, Time Statistics, Execution Lines and Visual Orders installed authored sources matched this checkout. Persisted router settings enabled ES-to-MES and NQ-to-MNQ. Source parity does not identify the loaded assembly generation.
- The prior Risk Manager selector preferred SelectedContent to SelectedItem and refreshed synchronously in SelectionChanged. Ctrl+C independently rebinds the panel, fitting Julian's workaround.
- An isolated .NET Framework WPF fixture reproduced old SelectedContent in 100/100 tab transitions with `Switch.System.Windows.Controls.TabControl.SelectionPropertiesCanLagBehindSelectionChangedEvent=true`; the modern switch mode produced 0/100 stale callbacks. The new resolver is correct in both modes. NinjaTrader's effective runtime switch has not been inspected, so this is not a confirmed complete runtime root cause.
- The existing router resolver can fall back to the chart instrument or a root-only instrument. The new entry-only resolver removes those fallbacks for enabled mappings.
- Missing routed overlays may be related to tab binding, but the patch's live overlay behavior remains unverified.
- `C:/Users/julia/Documents/Orca Codex` is a junction to `C:/Users/julia/Documents/New project`; this was not a second independent source tree.

## Files changed

- `Orca Trades/Working_Suite/AddOns/OrcaRiskManagerAddOn.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaExecutionRouterAddOn.cs`
- `tests/OrcaRiskManager.EntryCheck/OrcaRiskManager.EntryCheck.csproj`
- `tests/OrcaRiskManager.EntryCheck/Program.cs`
- `tests/OrcaRiskManager.EntryCheck/Fixture.cs`
- `docs/indicators/ORCA_RISK_MANAGER.md`
- This handoff and its adjacent `2026-09-08_orca-risk-manager_tab-entry-context.patch`.

## Behavior added, changed or removed

- Resolve selected chart from SelectedItem rather than potentially old SelectedContent. Prefer the actual selection to an event-source/focus fallback.
- Coalesce tab refresh requests, execute after selection at Loaded dispatcher priority, filter child selector events, and retain detached/retired owner checks.
- Capture selected window/tab/account/chart contract/execution contract and tab-binding version for each of the five entry paths. Reject missing/incorrect ownership and unresolved routes. Revalidate after existing confirmation dialogs, before order creation. A binding change away and back invalidates the captured context.
- Entry routing requires an exact MES/MNQ contract derived from the chart suffix when its mapping is enabled. Failure displays an explanatory `Orca order not submitted` message. Explicitly disabled mappings and other instruments retain their chart contract.
- No automatic retry, automatic submission, global order interception or automatic account selection is added.
- Existing protection, execution-event, close, cancel, flatten and break-even methods were not changed. The legacy display/management resolver remains unchanged. Their end-to-end behavior across a tab switch is still part of manual regression testing.

## User-facing settings

None added, changed, removed or deprecated. Existing confirmation preferences, quantity/price/side calculations and serialization remain. New messages explain rejected entry context or failed mapping.

## Series, Tick Replay, history and caches

No secondary Tick, Second, Bid, Ask, Last, Volumetric or custom series changes. No AddDataSeries, BarsRequest, OnMarketData, Calculate.OnEachTick/OnPriceChange, Tick Replay, history loading or market-data cache changes. No provider migration. Delta and Time Statistics sources are untouched by this task.

## Rendering and performance

Panel/overlay tab rebinding is deferred and coalesced. Drawing geometry, price markers and OnRender paths are unchanged. Two bounded entry-context checks occur on user submission, with exact instrument lookup when mapping is enabled. No per-tick work or new polling timer. No performance improvement is claimed without measurement.

## Tests and compile status

Command:

```powershell
dotnet run --project tests/OrcaRiskManager.EntryCheck -- 'C:/Users/julia/Documents/New project' 'C:/Users/julia/Documents/New project/.codex-backups/risk-tab-routing-20260908'
```

- Complete Risk Manager, Router and sizing helper compile against installed platform references: zero errors, offline only.
- Actual selection/queue/resolver/context/five submission methods extracted into .NET Framework WPF fixture: 99 checks pass in each mode, 198 total.
- Tests include 100 repeated switches/mode; direct and wrapped tab items; no selection; queued refresh coalescing/latest tab; detached owner; ES/MES and NQ/MNQ; missing/wrong-expiry/exception lookup; disabled mapping; original quantity, side and price/type; all five successful entry paths; stale tab; missing account; cancelled confirmation; account/route/tab/same-symbol-tab/chart-contract changes during confirmation; away-and-back binding; replay lock and hidden panel.
- Comparison against pre-edit backup: 297 existing Risk Manager methods and all 21 existing Router methods remain syntax-equivalent. Modifications to existing methods are confined to the declared tab/entry scope.
- Scoped diff whitespace check passed.
- No tests performed inside NinjaTrader; no platform orders submitted. Explicit F5 and loaded assembly behavior are unverified.

## Backup, Git and deployment

Pre-edit copies: `.codex-backups/risk-tab-routing-20260908/`.

- Risk Manager SHA-256: `A6FE4F1DBADD0E512B6601DE274AA30C2B72991345FDA3BEDA19B32974EDBFF4`.
- Router SHA-256: `6C3C3750662C821863FCB871BAA36DC3052A4DEF47E44DAE7020B40A329DC2B8`.

Both production source files already contained substantial unrelated uncommitted work, including lifecycle/overlay prerequisites absent from HEAD. Preserve that work. The adjacent patch records only this task's delta against those exact backups; tests, component documentation and this handoff can be committed independently without staging the unrelated source work. Production files remain modified in Working_Suite; the source delta is retained in the committed patch rather than committing those pre-existing changes wholesale.

Julian confirmed he is flat, has no working orders and is ready for SIM validation. Deployment copied only Router first and Risk Manager second, using the existing targeted deploy script. Each live file matched its pre-edit backup before copying; both original live files were backed up as `.live-before.cs` beside the source backups and backup hashes verified. Both deployed files match Working_Suite after newline normalization.

- Deployed Router SHA-256: `D7473D288C5451E31F93B2C0484282CA8A8A741C04BA0DF7CA148D9EDCB004F3`.
- Deployed Risk Manager SHA-256: `89A9291FD1F84EB9FAE5D800771C98765EC6AF12A822E608B1341C7446D7277E`.
- Both source copies completed at 14:25:49 EDT. NinjaTrader regenerated `NinjaTrader.Custom.dll` at 14:25:55 EDT, after deployment. This is assembly-generation evidence, not explicit F5 or successful runtime load/behavior proof.
- Explicit F5, successful Custom assembly load, restart and SIM behavior remain pending Julian. No automated chart interaction, restart or platform order submission was performed.
- Next: F5, then restart once while flat so the AddOn lifecycle starts cleanly. No full-suite deployment or Full_Suite promotion.

## Manual validation and next steps

1. Confirm a SIM account and both actual tab configurations. Enable the intended ES/MES mapping and verify the same expiry.
2. Without Ctrl+C recovery toggles, switch away and back repeatedly. Verify the panel and routed overlay follow the selected tab; repeat with the panel hidden/opened.
3. On SIM, test Risk Manager Buy Limit, calculator entry and Spacebar separately after switching. Verify the actual Orders/Executions instrument is MES, account and quantity are correct, and each action submits at most once. Test Sell and NQ/MNQ as well.
4. Check working limit orders, fills while hidden, bracket stop/target creation, partial fills, cancel, close, flatten, and account changes. Verify protections for an earlier tab's trade remain attached to the original order/account.
5. Recompile/restart and repeat. Report stale overlays, blocked-entry messages or any incorrect order instrument as separate observations.
6. Only after routing/ownership passes, move to delta continuity tests with identical active/inactive charts, matched source/reset/bundle settings and background-load measurement.

## Known risks and promotion

This is a first bounded correction, not a complete account/protection-engine redesign. Simulated-account tests cannot establish platform callback order, real fills, account reconciliation, UI resource lifecycle, workspace startup or performance. A passing compile does not constitute validation.

Full_Suite was not changed. Not eligible for promotion until Julian confirms F5 and the focused NinjaTrader behavior checks.
