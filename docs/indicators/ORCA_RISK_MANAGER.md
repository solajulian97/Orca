# Orca Risk Manager: tab binding and new-entry routing

Updated 2026-09-08. Working_Suite source and targeted deployment complete; NinjaTrader F5 and Julian's manual validation pending.

## Ownership and entry context

The window owns a visible/lazy Risk Manager panel and a separate routed-overlay controller. Both attach to the selected chart tab. Tab changes queue a coalesced refresh at the chart dispatcher's Loaded priority. Child selector events do not trigger tab rebinding. The selected item, either a ChartTab or a TabItem containing one, determines the selected chart; SelectedContent is not used because it can lag during WPF SelectionChanged.

Risk Manager new entries capture the selected tab, window, account, chart contract, execution contract and tab-binding version. The panel must be operational and attached to that selected tab; the account must come from that window's Chart Trader. Missing account selection does not fall back to Sim101 for new entries. A second check after existing confirmation dialogs rejects changes before order creation, including a tab binding that changes away and back.

The five guarded entry methods are ExecuteTrade (calculator), ExecuteFastCommand (fast buttons), PlaceDragOrderAt (Buy/Sell Limit/Stop placement), SubmitStagedBracket (Spacebar), and SubmitAltSpaceQuickEntryAt. Existing quantity, side, price, confirmation preferences, and order type calculations are retained.

## Router contract

OrcaExecutionRouter.TryResolveEntryInstrument is the entry-only resolver. When enabled for the selected root, ES maps to MES and NQ to MNQ using the exact chart-contract suffix. Missing contract information, lookup exceptions, missing results, wrong roots and wrong expiries reject the new entry with a visible explanation. There is no root-only or mini-contract fallback for an enabled mapping.

Explicitly disabling routing or an individual mapping retains entry on the chart instrument. An unmapped instrument also uses its chart instrument. No settings or serialization properties were added or changed.

ResolveExecutionInstrument remains the established display and existing-order management resolver. This patch does not refactor account execution processing, protections/OCO, close, cancel, flatten or break-even. Their broader lifecycle behavior still requires manual testing across tab changes. The Execution Router is not a platform-wide redirect for native NinjaTrader order controls.

## Evidence and limitations

Run `dotnet run --project tests/OrcaRiskManager.EntryCheck -- "C:/Users/julia/Documents/New project"` from this checkout. The runner compiles the complete two AddOns and sizing helper against installed NinjaTrader references, then extracts the actual selection, queue, routing, context and five entry methods into an isolated .NET Framework WPF executable with simulated accounts/instruments.

The 2026-09-08 run passed 99 checks in each of two WPF compatibility modes. With lag enabled, SelectedContent pointed to the previous tab in 100/100 immediate callbacks; with lag disabled, it was current. The selected-item resolver was correct in both modes. This establishes a code susceptibility and regression behavior, not the effective switch setting or full cause in Julian's running NinjaTrader process. Tests also verify coalescing, detached-owner suppression, exact mapping, submission parameters and blocked-entry cases. They never submit platform orders or open NinjaTrader UI.

No data series, AddDataSeries, BarsRequest, OnMarketData, Calculate setting, Tick Replay behavior, historical work, market-data cache or OnRender work changes. Additional work is limited to tab transitions and user entry actions. No measured performance gain is claimed.

See [handoff](../handoffs/2026-09-08_orca-risk-manager_tab-entry-context.md) for rollout gates and outstanding SIM checks. Full_Suite promotion is not eligible.
