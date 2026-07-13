# 2026-07-13 - OrcaExecutionLines Account Day P&L Label

## Objective

Add a toggleable completed-trade label line that records the selected account's realized P&L for the current trading session immediately after the round trip returns flat. The purpose is to preserve the account-level P&L context after each trade for later tilt review.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs`
- `docs/handoffs/2026-07-13_orca-execution-lines_account-day-pnl-label.md`

The live NinjaTrader deployment copy was updated at `Documents/NinjaTrader 8/bin/Custom/Indicators/OrcaExecutionLines.cs`. `Full_Suite` was not changed.

## Behavior added, changed, or removed

- Each live completed round trip now captures `AccountItem.RealizedProfitLoss` from the execution's account at the flat transition.
- The captured scalar is stored on that round trip, so later trades do not change the value shown for an earlier trade.
- The aggregate completed-trade hover label can display `Day P&L after trade: +$X.XX`.
- Account day P&L is NinjaTrader's overall realized P&L for the current trading session across instruments and is net of configured commissions.
- Individual fill labels do not display the account-day line.
- Existing execution matching, MAE/MFE, open-trade rendering, notes, tags, diagnostics, and session-total behavior were not changed.

## User-facing settings added, changed, deprecated, or removed

- Added `Show Account Day P&L On Label` under `1. Visibility`.
- Default is `false` to preserve existing label behavior.
- No settings were changed, deprecated, or removed.

## Secondary series added or changed

- None.
- No Tick, Second, Bid, Ask, Last, Volumetric, or custom series were added or changed.
- The indicator still uses account execution events plus its existing primary-chart callbacks.

## Tick Replay implications

- None.
- The feature does not require Tick Replay and does not change replay behavior.

## Historical-load implications

- The account-day snapshot is captured only from a live `ExecutionUpdate` carrying an account reference.
- Round trips reconstructed by the existing SQLite or `Account.Executions` history loaders intentionally do not receive a snapshot, because the current account total cannot truthfully reconstruct the point-in-time account P&L after every older trade.
- After a chart/indicator reload, previously reconstructed trades therefore omit this label line unless a future persistence/reconstruction feature is added.

## Cache implications

- None.
- No cache keys, shared providers, databases, note stores, or persistent files were added or changed.

## Rendering implications

- The aggregate hover label reads one precomputed scalar and conditionally appends one text line.
- No account access, mutation, synchronization wait, cache read, or historical calculation was added to `OnRender`.
- Existing snapshot-based SharpDX rendering remains intact.

## Performance implications

- One `Account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar)` call is made only when a live round trip fully closes.
- There is no per-tick or per-render account polling.
- Expected overhead is negligible relative to the existing execution-close processing.

## Tests performed in NinjaTrader

- Targeted deployment completed with `deploy_orca.ps1 -Target OrcaExecutionLines`.
- NinjaTrader regenerated only its generated wrapper section after deployment.
- Normalized authored-source SHA-256 matched between Working_Suite and the live deployment: `B7AC72553279DF10D07B555FA8BFD2FD0BA37E1D793D1511EEAA38B4C871075A`.
- NinjaTrader F5 compile has not yet been performed by Julian.
- No live trade was placed as part of this implementation pass.

## Compile status

- `git diff --check` passed with only the existing LF-to-CRLF warning.
- NinjaTrader F5 compile is pending.

## Manual-validation status

- Pending.
- Enable `Show Account Day P&L On Label`, close a trade, and hover the completed aggregate execution line.
- Confirm the label matches the selected account's Control Center realized P&L after commissions.
- Close a second trade and confirm the first label keeps its earlier value while the second label shows the new cumulative account total.
- Confirm switching Chart Trader accounts does not show another account's stored round trips.

## Known issues, risks, and follow-up work

- Historical point-in-time account day P&L is not reconstructed or persisted in this first pass.
- Account providers can differ in when they publish the final realized-P&L update relative to the execution callback; live validation should confirm the captured value includes the closing execution on Julian's configured accounts.
- If callback ordering proves late, follow-up should use a bounded account-item update correlation rather than per-tick polling.
- Raw whole-file source/live hashes differ because NinjaTrader regenerated the generated wrapper section; the authored portion matches.

## Full_Suite promotion eligibility

- Not eligible.
- Promotion requires NinjaTrader F5 compile and Julian's live manual validation.
