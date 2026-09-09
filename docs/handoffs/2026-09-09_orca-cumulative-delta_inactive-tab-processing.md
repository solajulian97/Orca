# Orca Cumulative Delta: continuous inactive-tab processing

Date: 2026-09-09

## Objective and evidence

Julian reports distorted delta after returning to inactive chart tabs. Supplied configuration: NQ SEP26, 500 Volume, Tick Replay enabled, Internal source, BidAsk classification, ETHDaily reset, Cumulative display; mirrored histogram, bundles off, wicks on. Authorized implementation with "proceed".

Source fact: the indicator set `IsSuspendedWhileInactive=true`. Its Internal path maintains bid/ask in OnMarketData and consumes the hidden tick series in OnBarUpdate. NinjaTrader documents that suspension stops OnBarUpdate and catches it up historically when the indicator resumes: [platform reference](https://ninjatrader-live.ninjatrader.com/support/helpguides/nt8/issuspendedwhileinactive.htm).

Hypothesis: suspending this stateful calculation disrupts continuity and contributes to the inactive-tab distortion. This session did not reproduce NinjaTrader's exact callback delivery or establish quote ordering during suspension/catch-up. Do not label this a proven root cause until live comparison passes.

## Files changed and behavior

- `Orca Trades/Working_Suite/Indicators/OrcaCumulativeDelta.cs`: sets IsSuspendedWhileInactive=false in SetDefaults and Configure, enforcing continuous processing for new and configured instances in all source modes.
- `docs/indicators/ORCA_CUMULATIVE_DELTA.md`: component behavior and validation documentation.
- This handoff.

No user-facing controls added or removed. Existing calculation/display identities and defaults are preserved; the engine suspension policy changes from true to false. No changes to Time Statistics, Risk Manager, Router, or Full_Suite were made for this correction.

## Data, replay, cache, rendering, and performance

- Secondary series: unchanged; Internal alone adds 1 Tick Last. No new Tick, Second, Bid, Ask, Volumetric, or custom series.
- OnMarketData: existing bid/ask tracking and hybrid realtime classification remain unchanged. Calculate.OnEachTick remains the default; no Calculate.OnPriceChange added.
- Tick Replay: existing historical Last-event quote usage and requirements unchanged. Tick Replay enabled is part of the requested validation configuration.
- Historical load: existing build/reset paths unchanged. Recreating/reloading the indicator is required to rebuild any corrupted in-memory accumulation and adopt the lifecycle correction; no stored history/cache deletion is proposed.
- Shared cache: existing OrcaProfileDataCache snapshot/backfill access unchanged; no additional cache or data provider introduced. Shared modes also remain active while hidden.
- Rendering: no OnRender, autoscale, brushes, bundling, plot order, Values indexes, or render allocation changes. Existing diagnostics preserved.
- Performance: hidden/minimized instances continue normal event processing, with potentially greater background CPU use than suspension. No measured CPU result or speedup is claimed. No full historical rebuild was added to a per-tick path.

## Verification and deployment

- Backups: `.codex-backups/cumulative-delta-inactive-20260909/OrcaCumulativeDelta.before.cs` and `OrcaCumulativeDelta.live-before.cs`.
- Before editing, normalized authored source/live regions matched. Live pre-deploy SHA256: `9A772224ECE1756719C74ADE799C17CA2820CA47A56DD27286F3A6A5C1C8A359`; guarded deployment required this hash still to match.
- `git diff --check` passed for the indicator.
- Temporary Roslyn verification: `dotnet run --project .codex-backups/cumulative-delta-inactive-20260909/Check.csproj`. All 54 methods outside OnStateChange unchanged; entire source syntax matches the backup after reverting exactly the two policy assignments. Both SetDefaults and Configure enforce false.
- Offline compilation of the complete authored indicator against installed NinjaTrader/.NET Framework references passed with zero errors. Generated NinjaScript wrappers excluded from this isolated check. An initial check-project inclusion error was corrected by excluding backup .cs files from the runner project; it was not a product-source error.
- Targeted deployment via `Orca Trades/Scripts/deploy_orca.ps1 -Target OrcaCumulativeDelta` completed at 09:33:08 EDT. Complete normalized source/live parity passed immediately after copy; no mirror sync.
- NinjaTrader tests performed: none. NinjaTrader F5 compile and Custom assembly load: pending. Custom DLL timestamp advanced to 2026-09-09 09:33:22 EDT after deployment, which is assembly-generation evidence only; successful load and F5 confirmation remain unverified.
- Manual validation: pending Julian. No live event-order reproduction or fixture result is being substituted for it.

## Manual check and follow-up

1. Press F5 in NinjaScript Editor, then reload/recreate the affected Cumulative Delta instances so they initialize from the new code and rebuild their data. Keep the supplied settings and Tick Replay enabled.
2. Compare two otherwise identical NQ 500 Volume charts with the same loaded range/session/template, one visible in a separate window and the other in a background tab. Allow several bars to form while hidden, return, and compare the completed bars' cumulative close/high/low values. Repeat several tab switches; note background CPU/responsiveness.
3. If distortion persists, capture matching timestamps/bar values and investigate quote/trade callback ordering and timestamp-to-volume-bar mapping separately. This change intentionally does not rewrite those existing paths.

Full_Suite promotion: **not eligible**; F5/load and Julian's live validation are outstanding. Existing dirty source work was preserved; the commit should include only this correction's two source hunks and its new documentation.
