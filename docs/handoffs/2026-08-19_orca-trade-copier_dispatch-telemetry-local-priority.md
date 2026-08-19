# Orca Trade Copier Dispatch Telemetry And Local Priority

## Objective

Measure Orca's internal leader-event-to-follower-submit latency separately from market fill latency, and make low-risk hot-path improvements that prioritize local follower submissions without changing order semantics.

## Files Changed

- `Orca Trades/Working_Suite/AddOns/OrcaCopyAddOn.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaCopyEngine.cs`
- `docs/handoffs/2026-08-19_orca-trade-copier_dispatch-telemetry-local-priority.md`

## Behavior Added, Changed, Or Removed

- Added per-follower dispatch timing from entry into the leader order callback through return from the follower `Account.Submit()` or `Account.Change()` call.
- Added last and running-average dispatch milliseconds to each follower state.
- Added a `Dispatch Last / Avg (ms)` column to the Copier tab.
- Local follower replication now occurs before leader-server network broadcast for the same order update.
- Replaced per-order conversion of the follower dictionary to an array with a cached follower snapshot rebuilt only when follower configuration refreshes.
- Existing order types, quantities, OCO mapping, guard behavior, fill-latency calculation, and slippage calculation remain unchanged.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

- No settings were added, changed, deprecated, or removed.
- One read-only telemetry column was added.

## Secondary Series Added Or Changed

- None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom series were added or changed.

## Tick Replay Implications

- None. The copier uses account order/execution events rather than chart Tick Replay.

## Historical-Load Implications

- None.

## Cache Implications

- Added an in-memory array snapshot of current follower state references.
- The snapshot is rebuilt when follower configuration is applied; it does not persist and does not contain market data.

## Rendering Implications

- Added one read-only DataGrid text column. No SharpDX or chart rendering changed.

## Performance Implications

- Removes one `ToArray()` allocation and follower-dictionary enumeration from each replicated order update.
- Prioritizes local follower submission ahead of synchronous remote network broadcast.
- Adds one `Stopwatch.GetTimestamp()` at leader order receipt and one after each follower submit/change, plus a deferred UI property update after submission.
- Dispatch telemetry measures Orca/NT submission-path latency; it does not measure broker, prop-provider, exchange, queue-position, or fill latency.
- Account submissions remain sequential by design. Parallel submission was not introduced because NinjaTrader/provider thread-safety and event ordering have not been proven safe.

## Tests Performed In NinjaTrader

- Deployed `OrcaCopyAddOn.cs` and `OrcaCopyEngine.cs` from `Working_Suite` to the live NinjaTrader `bin\\Custom\\AddOns` folder.
- Pending NinjaTrader F5 compile and Sim-account runtime test.

## Compile Status

- Local C# compile passed against the installed NinjaTrader and WPF assemblies.
- NinjaTrader F5 compile remains pending.

## Manual-Validation Status

- Pending Julian's Sim-account confirmation.

## Known Issues, Risks, And Follow-Up Work

- `Account.Submit()` return time indicates local submission-path completion, not confirmed broker receipt.
- LAN socket writes remain synchronous after local submissions and could still delay processing of a later leader event if a remote socket stalls. An ordered background network-send queue is a possible follow-up after local dispatch measurements establish need.
- Validation should compare dispatch time against fill latency across one follower and then multiple followers.

## Full_Suite Promotion Eligibility

- Not eligible until NinjaTrader F5 compile and Julian's manual Sim-account validation are confirmed.
