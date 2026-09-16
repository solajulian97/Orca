# Live Orca Source And Full Suite Sync

Date: 2026-09-15

## Objective

Use the installed NinjaTrader `bin/Custom` Orca source as Julian's current source-of-truth snapshot, bring its substantive authored differences into `Working_Suite`, and mirror the complete live Orca source set into `Full_Suite` for GitHub backup.

## Files changed

- Six `Working_Suite` files synchronized from live source: `Indicators/OrcaLegtoLegProfile.cs`, `Indicators/OrcaOpeningRanges.cs`, `Indicators/OrcaRollingProfiles.cs`, `Indicators/OrcaStepProfile.cs`, `DrawingTools/OrcaFixedRangeProfile.cs`, and `ChartStyles/OrcaVolumeCandles.cs`.
- The commit includes the complete 68-file `Working_Suite` source tree so previously untracked live modules and the repository-only history-admission helper are represented on GitHub.
- `Full_Suite`: all 67 live `Orca*.cs` files under `Indicators` (50), `AddOns` (13), `DrawingTools` (2), `BarsTypes` (1), and `ChartStyles` (1). The exact committed path manifest is available from `git show --name-only` for this handoff's commit.
- This handoff. Unrelated dirty documentation, tests, backup folders, and the separate `Orca Full Suite` folder were not included.

## Behavior and settings

- `OrcaLegtoLegProfile`, `OrcaRollingProfiles`, and `OrcaStepProfile` use the live statistics tokens `D` and `FD` in place of repository `T` and `F`; formulas and sources did not change in these two-line differences.
- Live `OrcaOpeningRanges` adds an optional CL open (default disabled), with CL time/color settings and an eighth session slot.
- Live `OrcaFixedRangeProfile` adds separate volume/delta track widths, a profile statistics box, and a trend line. Its new settings and default-on display choices replace the earlier repository drawing-tool snapshot.
- Live `OrcaVolumeCandles` is a different 253-line ChartStyle with `WidthExponent`, min/max body width, wick/body and border/wick matching, and wick-width settings. It replaces the 922-line repository ChartStyle with rolling-lookback normalization, clipping/scaling/color modes, and a different custom ChartStyle type ID. Saved style templates and visual parity must be checked before treating this as a validated product promotion.
- The other 61 live/working pairs had identical authored code after normalizing line endings, trailing whitespace, and NinjaTrader's generated region. Existing live behavior in those files was mirrored without new implementation.

## Series, Tick Replay, history, cache, render, performance

- This is a source synchronization, not a new data architecture. The `D`/`FD` changes add no secondary series or callback work. CL Opening Ranges uses the indicator's existing 30-second series.
- The installed Fixed Range and Volume Candles replacements add no Tick, Second, Bid, Ask, Last, Volumetric, or custom secondary series. Fixed Range retains its existing profile/cache sourcing; Volume Candles reads primary chart OHLCV.
- Tick Replay and historical-load behavior remain whatever the installed live sources currently implement. No historical data, NinjaTrader cache, database, or workspace was changed during this sync.
- Fixed Range and Volume Candles alter SharpDX rendering and settings. Their live performance has not been benchmarked against the prior repository versions, and the Volume Candles feature reduction is a material compatibility risk.
- `Working_Suite/Indicators/OrcaProviderHistoryAdmissionCore.cs` is absent from the live Custom tree and is used by repository tests; it remains in `Working_Suite` only and is not part of this live `Full_Suite` snapshot.

## Verification and promotion status

- Pre-sync inventory: 68 `Working_Suite` Orca sources, 37 `Full_Suite`, 67 installed live; no live-only Orca filename was found.
- Post-sync authored comparison: all 67 live files match both `Working_Suite` and `Full_Suite`; platform-generated regions were excluded from that comparison.
- `git diff --check` on suite paths passed. No new NinjaTrader F5 compile, chart/trading interaction, template reload, or busy-open measurement was performed for this snapshot.
- Offline `OrcaStepProfile.PlatformCheck` passed with zero semantic errors and 14 focused filter cases. `OrcaLegToLegProfile.PlatformCheck` and `OrcaRollingProfiles.PlatformCheck` exited at their prior metadata-only source-diff guards because the live `D`/`FD` text change is outside those fixtures' allowed scope; they did not establish a compile failure. Those guards were not altered for a source-backup task.
- Julian requested the broad live-source promotion to `Full_Suite` and GitHub. This commit records that explicit source snapshot; it does not assert that every prior development handoff's manual-validation gate was passed.
- Follow-up: verify live Volume Candles identity/template compatibility, Fixed Range settings/rendering, and representative workspace F5/load behavior; keep per-component validation status separate from this backup commit.
