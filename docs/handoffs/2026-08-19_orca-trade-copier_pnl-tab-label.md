# Orca Trade Copier P&L Tab Label

## Objective

Rename the Orca Trade Copier account-health tab from `Health` to `P&L` so the navigation label matches the tab's primary user-facing purpose.

## Files Changed

- `Orca Trades/Working_Suite/AddOns/OrcaCopyAddOn.cs`
- `docs/handoffs/2026-08-19_orca-trade-copier_pnl-tab-label.md`

## Behavior Added, Changed, Or Removed

- Changed the second Orca Trade Copier tab header from `Health` to `P&L`.
- No copier routing, follower guard, account-health calculation, synchronization, or order behavior changed.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

- None.

## Secondary Series Added Or Changed

- None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom series were added or changed.

## Tick Replay Implications

- None.

## Historical-Load Implications

- None.

## Cache Implications

- None.

## Rendering Implications

- Only the visible WPF tab header text changes. Layout and account-health rendering remain unchanged.

## Performance Implications

- None expected. This is a static label change outside the order-routing path.

## Tests Performed In NinjaTrader

- `OrcaCopyAddOn.cs` was deployed from `Working_Suite` to the live NinjaTrader Custom AddOns folder.
- NinjaTrader F5 compile and visual confirmation remain pending.

## Compile Status

- Local C# compile passed against the installed NinjaTrader and WPF assemblies.
- NinjaTrader F5 compile remains pending.

## Manual-Validation Status

- Pending Julian's confirmation that the tab displays as `P&L` in NinjaTrader.

## Known Issues, Risks, And Follow-Up Work

- Trade-copy latency has been reviewed separately at source level. Any speed optimization should begin with submit-latency instrumentation before changing thread or submission behavior.

## Full_Suite Promotion Eligibility

- Not eligible until NinjaTrader F5 compile and Julian's manual validation are confirmed.
