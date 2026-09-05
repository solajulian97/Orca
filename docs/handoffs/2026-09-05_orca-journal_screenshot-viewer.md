# Orca Journal manual screenshots and viewer — 2026-09-05

## Objective and ownership

Make manual screenshot review useful before adding automatic capture. Julian approved continuing this slice while NinjaTrader was closed. Authoritative source: `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal`, versioned in its parent repository. No reported NinjaTrader runtime defect was outstanding; the earlier shared-identity build still has no reported platform validation.

## Files and behavior

- External `UI/Windows/TradeImageViewer.cs`: new image gallery with fit, 100%, zoom buttons/wheel, drag panning, previous/next, arrow keys, F to fit, 0 for 100%, Escape to close. Window can be maximized normally. Gallery snapshots the selected trade's attachments and excludes videos; existing captions display without editing. Corrupt/missing images encountered during navigation show an error and allow continued navigation. Images load on demand with released file handles.
- External `UI/ViewModels/TradesViewModel.cs`: opens the owned gallery with account/instrument/direction/time context. Existing video playback stays external. Validates attached images before copying, uses GUID filenames and non-overwriting writes, and catches clipboard access errors. Existing attachment records/files and annotations are preserved.
- External `UI/Views/TradesView.xaml`: explains Win+Shift+S → Paste or Attach → Open.
- Coordination `tests/OrcaJournal.Screenshots/Program.cs` and `Verify.ps1`: disposable STA WPF decoding/navigation/render checks against actual viewer source.
- This handoff is copied to the external repository's `docs/handoffs`.

No new persistent settings, schema changes, dependencies, annotation editing, cloud upload, or automatic screen capture. Attach/Paste remain explicit buttons. Screenshots remain local under `Documents\OrcaJournal\attachments` with the existing trade association. The gallery is fixed at opening; close/reopen to include newly attached images. GIF images display their first frame. 100% maps source pixels to WPF device-independent units; physical size follows Windows display scaling.

## Platform and performance implications

Secondary series, AddDataSeries, OnMarketData, Calculate modes, Tick Replay, historical loads, and trading caches: unchanged. No indicator or Full_Suite files changed. Rendering uses ordinary WPF images in a separate resizable window; one full-resolution image is loaded on demand per viewer. Very large images can cause temporary UI decoding delays and memory use; decode is synchronous. No per-tick work or historical rebuild was introduced.

## Verification and deployment

- Release net48 x64 build: passed, 0 errors, 4 existing MSB3277 dependency conflict warnings.
- Actual viewer compiled and exercised by the standalone STA WPF harness: 11 checks passed, including decoding/freeze, released file handle, video exclusion, boundaries, fit/100%/zoom, corrupt and missing image handling, fixed gallery snapshot, and navigation recovery. Rendered synthetic-chart preview visually inspected. Harness artifacts: `C:\Users\julia\AppData\Local\Temp\orca-screenshots-3ecca6dcf6e249a680a7ac6b31545464`.
- DLL metadata still references installed `System.Data.SQLite, Version=2.0.2.0, PublicKeyToken=db937bc2d44ff139`.
- Deployed only `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\OrcaJournal.dll`; source/live SHA256: `E9E88E716DEE73B4153BAEBCE0459BEAF6D392645B97E78CDA0E20F1CB782C00`.
- Backup: `C:\Users\julia\Documents\New project\.codex-backups\journal-screenshots-deploy-20260905-182115`. Previous DLL, database, annotation TSV and available WAL/SHM sidecars copied with data-backup hash checks. Process checks before/after deployment found NinjaTrader closed. Live database and TSV hashes unchanged after deployment. Existing media was not changed.
- NinjaTrader F5/load and manual/runtime validation: **pending**, including prior shared identity. Offline WPF success is not platform validation. No NinjaTrader tests performed this slice.

## Julian's validation checklist and follow-up

1. Restart NinjaTrader, compile/F5, open Journal, and confirm existing trades, notes, grades and media remain present.
2. Select a trade, use Win+Shift+S, then click Paste; also try Attach with a saved image. Confirm both stay with that trade after reopening Journal.
3. Open an image. Test wheel/buttons, drag while zoomed, Fit after resize, 100%, previous/next and Escape. Change selected trade behind the viewer and verify its gallery stays on the original trade. Check monitor scaling and readability.
4. Confirm existing recording Play still opens the video. Then perform the shared-identity/reconciliation SIM validation in the prior handoff.

Address any reported runtime defects before advancing. Further annotation ownership/editing and optional screenshot capture automation remain separate slices. Unrelated dirty coordination work and external `OrcaJournal/Lib/` remain untouched. Full_Suite promotion is not eligible without Julian's NinjaTrader validation.
