# Orca Cumulative Delta

Updated: 2026-09-09. Source inspection and offline compilation; inactive-tab correction awaits NinjaTrader F5/load and Julian's manual validation.

Source: `Orca Trades/Working_Suite/Indicators/OrcaCumulativeDelta.cs`.

## Background processing

`OnStateChange` sets `IsSuspendedWhileInactive=false` in both SetDefaults and Configure. The Configure assignment enforces continuous processing after any saved or caller-applied setting. This applies to all source modes; it is not an optional display setting.

Internal mode retains its hidden 1 Tick Last series. `OnMarketData` maintains bid/ask state, including quotes on Tick Replay Last events; secondary-series `OnBarUpdate` classifies volume and updates per-bar/cumulative arrays. Continuous processing avoids intentionally suspending the bar-update side of this stateful pipeline on inactive tabs. It does not establish a new quote/trade ordering guarantee or repair already accumulated bad values in an existing instance.

SharedProvider and SharedHistoricalInternalRealtime retain their existing `OrcaProfileDataCache` snapshot/backfill paths. No series, cache, formula, plot, reset, or rendering changes accompany the suspension policy. `Calculate.OnEachTick` remains the default; no OnPriceChange path was introduced. Background instances can consume more CPU than suspended instances; the actual cost has not been benchmarked.

## Display compatibility

The 2026-09-09 correction preserves all exposed property identities and display defaults, including BidAsk, ETHDaily, Cumulative, Mirrored histogram, and `BundleSameSignBars=false`. Bundles remain an opt-in BarByBar presentation. Plot indexes remain DeltaClose=0, DeltaHigh=1, DeltaLow=2.

## Validation

See [inactive-tab handoff](../handoffs/2026-09-09_orca-cumulative-delta_inactive-tab-processing.md) for evidence, deployment status, and the paired-chart manual check. Full_Suite promotion is not eligible until Julian confirms live behavior.
