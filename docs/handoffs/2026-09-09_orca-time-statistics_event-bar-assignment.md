# Time Statistics: preserve the assigned bar for local trade events

Date: 2026-09-09

## Objective and recovered state

Continue the coordinated inactive-tab investigation after Risk Manager/Router and Cumulative Delta. Julian was unsure of Time Statistics' source setting and requested proceeding with Internal. This is the chosen investigation path, not confirmation of the affected instance's live setting.

Time Statistics already defaulted to IsSuspendedWhileInactive=false. Its OnMarketData path classified each Last trade, then used BarsArray[0].GetBar(e.Time) to select the delta array index. The shared modes also contain render-dependent refresh paths; these are outside this Internal-mode correction.

The platform documents [GetBar](https://docs.ninjatrader.com/ninjascript/getbar) as returning the first matching timestamp, and [OnMarketData](https://docs.ninjatrader.com/ninjascript/onmarketdata) as expected after OnBarUpdate. A timestamp alone loses bar identity when several range/volume bars share it. The existing OrcaInstantReplay capture path already prefers CurrentBar for the same reason.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs`
- `tests/OrcaTimeStatistics.EventCheck/{OrcaTimeStatistics.EventCheck.csproj,Program.cs,Fixture.cs,README.md}`
- `docs/indicators/ORCA_TIME_STATISTICS.md`
- This handoff.

Existing dirty diagnostics, provider-warning, and replay adapter work was preserved. Full_Suite and other indicators/AddOns were not changed in this task.

## Behavior and settings

- Under Calculate.OnEachTick, local Last events accumulate into CurrentBar, with a primary-series bounds check, instead of selecting the first timestamp match. Invalid/unavailable indexes cannot fall back to the first/last bar.
- Other Calculate modes retain their prior timestamp lookup; no new accuracy claim is made for them.
- Configure enforces the existing false suspension policy after settings are applied. Defaults already used false.
- Classification, raw signed volume, extrema formulas, Finish Delta, cumulative reset rules, rows, colors, serialization and user-facing controls are unchanged. No settings added, removed or renamed.
- The shared-only snapshot path is unchanged; the hybrid mode's existing local-event path also benefits from CurrentBar assignment.

## Series, history, cache, rendering, performance

- No secondary Tick, Second, Bid, Ask, Last, Volumetric or custom series added; no AddDataSeries introduced.
- Existing OnMarketData subscription and Calculate.OnEachTick default remain. OnPriceChange/OnBarClose are not enabled or forced by this edit.
- Tick Replay requirements are unchanged. Historical local events can now assign differently where timestamps were ambiguous; this is the intended correction. Recreate/reload the indicator to rebuild old in-memory values.
- No shared service/cache, provider, persistence, history download or historical-data deletion changed. Existing OrcaProfileDataCache modes remain separate.
- No OnRender, DirectWrite/brush, cumulative-scan, average calculation or replay-horizon changes. No per-tick historical rebuild, new loop, polling or locking was added.
- Default background CPU behavior was already continuous. The assignment is constant work and avoids GetBar lookup in OnEachTick; no measured speedup is claimed. Configure can override a previously suspended instance.

## Checks and deployment

- Source/live normalized authored regions matched before editing.
- Backups: `.codex-backups/time-statistics-inactive-20260909/OrcaTimeStatistics.before.cs` and `OrcaTimeStatistics.live-before.cs`.
- Pre-deploy live SHA256: `06CB88CF23EF502A2E5392D6B9B9F24F5F611FD278EF141C1EF93066EB2CC015`; verified again immediately before copying.
- Baseline regression: the actual pre-change OnMarketData method failed with `Historical second same-time bar: expected -7, got 0`.
- Corrected regression: 25 assertions passed using extracted actual OnMarketData, storage, extrema and Finish Delta methods with modeled event/bar inputs. Covers duplicate timestamps, Historical/Realtime states, time bars, assigned developing/future bars, invalid indexes, signed-volume conservation, quote/fallback classification, provider exclusion, hybrid local assignment and non-OnEachTick compatibility. No real platform scheduling is emulated.
- Whole authored indicator compiles offline against installed NinjaTrader/.NET Framework references; generated wrappers excluded. Full syntax comparison confirms only the declared configuration/index edits, with 73 other methods and all property declarations preserved.
- Reproduce: `dotnet run --project tests/OrcaTimeStatistics.EventCheck -- 'Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs' '.codex-backups/time-statistics-inactive-20260909/OrcaTimeStatistics.before.cs'`.
- Targeted deployment at 12:30:10 EDT via `deploy_orca.ps1 -Target OrcaTimeStatistics`; no mirror sync. Complete normalized source/live parity passed. Deployed SHA256: `9FBF5C2FE6F797497D0BC58A64B82BECFB67851FB2E29BED9170AF97BA78E2D6`.
- Custom DLL timestamp advanced to 12:30:17 EDT, providing regeneration evidence only; it does not prove successful load.
- NinjaTrader tests performed: none. Explicit F5 compile, successful Custom assembly load and Julian's manual acceptance remain pending.

## Manual validation and limits

1. F5 in NinjaScript Editor, then recreate/reload the affected Time Statistics instances. Verify Internal, OnEachTick and Tick Replay enabled on both comparison charts.
2. Use identical instrument, bar type/size, trading-hours template, loaded range and cumulative reset setting. Keep one chart visible in another window and the other in a background tab; compare completed bars after several new bars and repeated tab switches.
3. Check Delta, Max/Min Delta, Finish Delta and Cumulative Delta, including rapid same-timestamp range/volume bars and an ordinary time chart. Check row order and reset boundaries remain expected.
4. If the original symptom persists, capture matched bar timestamps and values. Inspect actual source setting, callback/bar sequencing, session-first-bar quote resets, render/model concurrency and provider-dependent paths rather than assuming suspension explains every component.

The same-time assignment defect is reproduced by controlled inputs, but its connection to the reported tab-switch incident is not yet proven. Single trades split across multiple volume bars and custom BarsType replacement behavior are outside fixture coverage. Existing shared-provider timestamp bucket mapping and render-time historical scans/refresh remain known follow-up work.

Full_Suite promotion: **not eligible** until F5/load and Julian's manual validation. Commit only the two focused source hunks with this task's tests/docs, preserving unrelated dirty source changes.
