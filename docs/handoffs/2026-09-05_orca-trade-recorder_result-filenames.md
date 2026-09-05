# Orca Trade Recorder result filenames — 2026-09-05

## Objective

Identify completed trade recordings by WIN, LOSS, or BREAKEVEN and whole-round-trip gross P&L, with explicit multi-trade and unknown-history handling.

## Files changed

- `Orca Trades/Working_Suite/AddOns/OrcaTradeRecorderAddOn.cs`
- `tests/OrcaTradeRecorder/Verify.ps1`
- `docs/ORCA_TRADE_RECORDER.md`
- This handoff.

## Behavior and settings

Added an account/full-contract execution ledger independent of chart instances. Scaling and partial exits accumulate until flat; crossing-flat fills split into a completed trade and reversal entry. Account-scoped execution-ID dedupe preserves distinct fills. The signed cash-flow total on a completed round trip matches the inspected Execution Lines FIFO gross total and uses the execution contract's point value. It does not subtract commissions.

Capture manifests now store constituent trades, history completeness, P&L basis, and the result title. The status reports the title when raw capture preservation finishes. Final MP4 naming at Disarm includes the outcome, gross amount, instrument, direction/account for a single trade, and unique capture ID. Multi-trade titles explicitly identify aggregation. Existing final recordings/paths are untouched, keeping Journal attachment links valid. Raw segment and bundle names remain stable. No settings were added/changed/removed; Test Recording is unchanged.

## Secondary series, Tick Replay, historical load, cache, rendering

No series or market-data callbacks added. No Tick Replay or historical chart loading implications. No shared market-data cache or indicator renderer changes. Execution IDs and ledger buckets live until re-arm; completed results transfer to manifests on capture stop. WPF status displays the saved result. Existing dirty Execution Lines source was inspected but not modified.

## Performance

One account/instrument lookup and ID hash lookup per execution; constant-time cash-flow/quantity updates plus per-trade ID-list checks. Work runs under the runtime's short lock; callbacks perform no file/network/process work. FFmpeg remains deferred until Disarm. No measured live performance claim is made.

## Verification and deployment

- Deterministic ledger harness: 18 assertions covering scaling, partial close, duplicate delivery, short loss, reversal allocation, breakeven, overlapping accounts/contracts, point values, sequential trades within one clip, unknown histories, legacy manifests, and safe names.
- Full AddOn .NET Framework compile against installed NinjaTrader/WPF assemblies: passed.
- Targeted diff whitespace check: passed (existing LF/CRLF warning only).
- Targeted live deployment: completed; normalized authored/live parity passed. The live pre-change source matched HEAD before deployment.
- NinjaTrader regenerated Custom.dll at 00:02:54 local, nine seconds after the source copy. Assembly generation is observed; successful load is not established by this timestamp.
- NinjaTrader F5/load: pending Julian; reference compile is not F5 validation.
- Live SIM/manual validation and trading-session performance: pending Julian.

## Risks and follow-up

PnL is explicitly gross, not account net-after-fees. Unknown execution IDs or reconnect uncertainty conservatively label subsequent captures PNL-UNKNOWN until re-arm. Existing open positions cannot provide truthful pre-arm full trade P&L. Existing aggregate position-trigger behavior is unchanged, including one clip across overlaps and cancelled tails; an extremely short round trip missed entirely by position reconciliation is not fixed by this naming feature. Trades opening while a prior recording is being stopped can make that clip incomplete; it is labeled unknown. No backfill, existing-file renaming, quality tags/grades, per-round-trip video splitting, or Journal schema changes are included.

SIM acceptance: compare filenames and manifest trades against Execution Lines for a scaled trade, loss, scratch, reversal, overlapping accounts/contracts, and a new trade during tail. Disarm and confirm final filename, audible footage, and Journal playback. Verify arming with an open position/disarming mid-trade yields unknown.

## Full_Suite eligibility

Not eligible until Julian confirms F5 and manual behavior. Full_Suite untouched.
