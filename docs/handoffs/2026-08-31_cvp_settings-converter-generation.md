# CVP Settings Converter Generation Repair

Date: 2026-08-31

## Objective And Evidence

Fix Julian's NinjaScript Editor errors after footprint v1 deployment without changing footprint behavior. The live `OrcaCandleVolumeProfile.Rendering.cs` contained generated `cacheOrcaFootprintSettingsConverter`, `EqualsInput`, and `CacheIndicator<OrcaFootprintSettingsConverter>` calls at lines 773/775. The helper inherits `IndicatorBaseConverter`, not `IndicatorBase`, so those wrappers produce CS1061 and CS0311. An offline check of the live generated source reproduced exactly these two errors before deployment.

The renderer partial contained the settings converter but no concrete `: Indicator` declaration. Move that converter beside the actual indicator declaration, matching the existing anchored VWAP layout. Do not add indicator methods or change the helper's base class. Earlier authored-only checks removed generated regions and therefore missed this failure.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`: unchanged settings converter moved here, with an ownership comment.
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.Rendering.cs`: converter removed; rendering and enum converters unchanged.
- `tests/OrcaFootprint.PlatformCheck/Program.cs`: converter placement guard, invalid-wrapper regression, optional deployed/generated-source semantic check.
- `tests/OrcaFootprint.PlatformCheck/InvalidConverterWrapper.txt`: deliberately invalid wrapper fixture reproducing both reported diagnostic codes.
- `docs/indicators/ORCA_CANDLE_VOLUME_PROFILE.md`: ownership and verification boundary updated.
- This handoff.

## Behavior And Implications

- Behavior: source-layout repair only. Converter namespace, name, class body and references remain unchanged. No rendering, evidence, classifier, bar attribution or snapshot changes.
- Settings: none added, changed, deprecated or removed. Enhancement remains opt-in/default false; saved property identities and legacy defaults preserved.
- Secondary series: none added or changed. Existing hidden Last Tick 1 and TickReplayLastEvents modes unchanged; `AddDataSeries`, `OnMarketData` and `Calculate.OnPriceChange` untouched.
- Tick Replay/historical load: no processing or source changes. No cache clearing, historical reload or database operation performed.
- Shared cache: registration/publication unchanged; no new access or contract.
- Rendering/performance: no runtime work added, no render path change. This repair is not a new performance validation of v1.

## Checks And Deployment

- Pure model harness: 393 checks passed. Informational run: 200,000 observations 48 ms; warm preparation for 200 visible/600 cached bars p50 0.042 ms, p95 0.102 ms, estimated derived bytes 1,443,616. Not a NinjaTrader rendering/load benchmark.
- Authored offline platform semantic check: zero errors; legacy methods, enums, defaults, opt-in renderer and shared-publication token parity passed; 13 extracted baseline behavior fixtures passed.
- New deliberately invalid generated-wrapper fixture reproduced CS1061 and CS0311; converter placement guard passes corrected source.
- Before deployment, live authored regions matched checkpoint `541eb21`. No unrelated live edits were overwritten.
- Deployed only `OrcaCandleVolumeProfile.cs` and `OrcaCandleVolumeProfile.Rendering.cs` using exact-target Working_Suite deployment. This removes the stale invalid generated helper region. No mirror, shared dependencies or Full_Suite copy.
- After deployment, authored parity passed for both files and unchanged `OrcaFootprintCore.cs`. Invalid converter wrapper absent. Offline semantic check including deployed generated code: zero errors; all platform harness checks passed.
- The optional generated-source check supplies only private `indicator` fields normally provided by other NinjaTrader host partials. It compiles the deployed CVP wrappers verbatim but does not execute NinjaTrader's source generator.

## Outstanding Gates And Rollback

- NinjaTrader F5: pending. Computer Use access to NinjaTrader was not approved; no F5 input, chart interaction or restart was performed. Julian must press F5 to verify fresh wrapper generation.
- NinjaTrader tests/manual validation: none performed for this repair. Settings dialog, saved templates, enhanced/legacy chart behavior and all earlier v1 runtime/replay/resource gates remain pending Julian's confirmation.
- Known risk: offline success cannot establish NinjaTrader generator behavior. Send any remaining compiler errors after F5; do not treat the removed stale wrapper as proof of regeneration.
- Rollback: preserve baseline backup `34d7faf` and implementation checkpoint `541eb21`. Disabling Enhanced Footprint rolls back visuals only, not compile failures. Restoring pre-repair renderer source could regenerate the same invalid helper wrappers.
- Full_Suite eligibility: **No**. No promotion or Git push performed. Commit only the focused repair/tests/documentation, leaving unrelated dirty files untouched.
