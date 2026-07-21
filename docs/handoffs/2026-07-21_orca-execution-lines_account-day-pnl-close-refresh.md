# 2026-07-21 - OrcaExecutionLines Account Day P&L Close Refresh

## Objective

Fix completed-trade labels that captured the selected account's realized day P&L one trade behind because the closing execution callback could arrive before NinjaTrader published the corresponding `AccountItem.RealizedProfitLoss` update.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs`
- `docs/handoffs/2026-07-21_orca-execution-lines_account-day-pnl-close-refresh.md`

`Full_Suite` was not changed.

## Behavior added, changed, or removed

- The existing immediate `Account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar)` capture remains as a fallback when a round trip returns flat.
- The just-closed round trip is armed for a bounded two-second account-day P&L refresh.
- `OrcaExecutionLines` now listens for `AccountItemUpdate` events on the same accounts already used for execution events.
- The first valid USD `RealizedProfitLoss` publication for the same account refreshes that completed round trip and then disarms it.
- The refresh also verifies the completed round trip belongs to this indicator's instrument.
- Older completed trades cannot be rewritten by later account P&L updates.
- Existing FIFO execution matching, MAE/MFE, open-trade arrows, labels, notes, shot clock, and completed-trade rendering remain unchanged.

## User-facing settings added, changed, deprecated, or removed

- None.
- `Show Account Day P&L On Label` retains its existing name, default, and behavior.

## Secondary series added or changed

- None.
- No Tick, Second, Bid, Ask, Last, Volumetric, or custom series were added or changed.

## Tick Replay implications

- None.
- The refresh uses live account events and does not change Tick Replay processing.

## Historical-load implications

- Historical and SQLite execution reconstruction still pass no live `Account` reference and therefore do not arm account-day refreshes.
- Previously reconstructed trades still omit point-in-time account day P&L when a truthful historical snapshot is unavailable.

## Cache implications

- None.
- No shared cache, local cache, database, or persistent state was added.

## Rendering implications

- `OnRender` still reads the precomputed scalar stored on the completed round trip.
- No account access, polling, synchronization wait, or calculation was added to `OnRender`.
- A successful refresh requests a chart invalidation so an open hover label can repaint with the new value.

## Performance implications

- Adds one low-frequency `AccountItemUpdate` subscription per already-hooked account.
- The callback immediately ignores every account item except USD `RealizedProfitLoss`.
- Matching work is one dictionary lookup under the existing short `tradeLock`.
- There is no per-tick, per-bar, timer, or render polling.

## Tests performed in NinjaTrader

- Source inspection confirmed every `AccountItemUpdate` subscription has a matching unsubscribe.
- Source inspection confirmed pending refresh state is stored per `AccountState`, checks account and instrument identity, expires after two seconds, and is cleared after the first accepted update.
- Official NinjaTrader documentation was checked for `Account.AccountItemUpdate` and `AccountItemEventArgs` fields.
- Targeted deployment completed with `deploy_orca.ps1 -Target OrcaExecutionLines`.
- Working_Suite and live authored source matched after newline normalization: `393C1166CF30127A2C86FFFF03330042515D5D5FA772E8D4CAF2297872B398B4`.
- Working_Suite and live files each contained exactly one NinjaScript generated-code region after deployment.
- `git diff --check` passed for the edited source and handoff.
- NinjaTrader F5 compile is pending Julian.
- Live closed-trade validation is pending Julian.

## Compile status

- NinjaTrader F5 compile is pending.
- The standalone Roslyn compiler parser reported no targeted C# syntax diagnostics; semantic compilation against NinjaTrader remains an F5 test.

## Manual-validation status

- Pending.
- Close one trade and confirm the label matches Account Data realized P&L after that same trade, rather than the prior total.
- Close a second trade and confirm each completed label keeps its own post-trade account total.
- Confirm a later account P&L change does not rewrite an older label.
- Confirm account switching still isolates the visible round trips and labels.

## Known issues, risks, and follow-up work

- NinjaTrader does not document deterministic ordering between execution and account-item callbacks. The immediate account query remains the fallback, while the observed late-publication path is corrected by the bounded event correlation.
- `AccountItemEventArgs` identifies the account item and account but not the instrument. The first-event-only two-second window minimizes cross-instrument ambiguity, but nearly simultaneous realized-P&L changes on the same account across different instruments remain a theoretical edge case.
- If a provider does not publish a USD `RealizedProfitLoss` event within the bounded window, the immediate fallback value remains.
- Historical point-in-time account day P&L is still not reconstructed or persisted.

## Full_Suite promotion eligibility

- Not eligible.
- Promotion requires NinjaTrader F5 compile and Julian's live validation of at least two sequential closed trades.
