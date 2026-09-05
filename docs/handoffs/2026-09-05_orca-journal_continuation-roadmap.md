# Orca Journal continuation handoff - 2026-09-05

## Objective and reading order

Continue a cohesive Orca journaling workflow: quick chart annotation after a trade, followed by visual review and learning in Orca Journal. This document is the entry point for a new chat; it records implementation, deployment, uncertainty, and the proposed next slice.

Read AGENTS.md, docs/ORCA_PRODUCT_STATE.md, docs/ORCA_ARCHITECTURE.md, docs/ORCA_DIAGNOSTICS_SPEC.md, docs/collaboration-workflow.md, and docs/engineering-notes.md. Then read this handoff and:
- docs/handoffs/2026-09-01_orca-journal_integrity-grades-recordings.md
- docs/handoffs/2026-09-05_orca-trade-recorder_result-filenames.md
- docs/ORCA_TRADE_RECORDER.md
- docs/handoffs/2026-09-05_orca-execution-lines_sqlite-parameter-binding.md
- Relevant August Execution Lines setup-grade, tag-library, note-editor, deduplication, and late-account-hook handoffs.
- docs/ai-context/CHATGPT_BRIEFING.md for orientation only: its dirty-file counts, name spelling, and integration status are stale. User is Julian; literal Windows paths still use julia.

## Purpose and agreed requirements

- Execution Lines is the quick capture surface: right-click a completed trade, add tags, setup grade A-F, and notes under relevant tags.
- Journal must show those notes, tags, and grade on the corresponding trade.
- Screenshots are central to explaining and reviewing a trade. Attach/paste is useful, but a tiny thumbnail is insufficient; a large viewer and comfortable writing environment are required.
- Automatically associate Orca Trade Recorder OBS footage with its corresponding trade(s).
- Future chart action should open that exact trade in Journal.
- Prefer a rich Journal within NinjaTrader; export to Notion or portable documents is a later option, not a decision to move the product out of NinjaTrader.
- Preserve existing annotations, attachments, and trade history. No automatic destructive deduplication or database/cache cleanup.
- Earlier plan-only instructions were followed by implementation authorization. Current request is a documentation handoff, not authorization to implement the entire roadmap.

## Exact environment and ownership

Verified 2026-09-05:
- Coordination repository and working directory: C:\Users\julia\Documents\New project
- Branch: main
- HEAD before this documentation commit: a8fe8f0 (Add isolated NinjaTrader provider ingestion probe)
- One worktree: C:/Users/julia/Documents/New project on main. No isolated Journal worktree.
- Previous Journal deployment handoff commit: b0d37a4.
- External solution: C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal.sln
- External source root: C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal
- External Journal root is NOT a Git repository, verified with git rev-parse. It has no branch, tracked baseline, or reliable uncommitted diff. The previous source changes are present there, not in b0d37a4.
- Suite source: C:\Users\julia\Documents\New project\Orca Trades\Working_Suite
- Validated promotion target: Orca Trades/Full_Suite; do not promote until Julian confirms NinjaTrader behavior.
- Full repository dirty inventory is in the sibling 2026-09-05_orca-journal_git-status.txt file. Index was empty at capture. Execution Lines alone has 1004 insertions and 155 deletions relative to HEAD, including other work. Do not whole-file stage it for a narrow Journal change.
- Recheck status before acting; other tasks continue changing this shared checkout.
- External source and live deployment writes may require tool sandbox escalation. Never substitute a mirror or decompiled copy for current source.

## Data and deployment paths

- Database: C:\Users\julia\Documents\OrcaJournal\orca_journal.db
- Annotation exchange: C:\Users\julia\Documents\OrcaJournal\execution_line_notes.tsv
- Recorder settings: C:\Users\julia\Documents\NinjaTrader 8\OrcaTradeRecorder.xml
- Recorder root: OutputDirectory in those settings, otherwise C:\Users\julia\Videos\Orca Trade Recordings
- Built Journal: C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal\bin\Release\net48\OrcaJournal.dll
- Live Journal: C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\OrcaJournal.dll
- Pre-deployment DLL/database backup: C:\tmp\orca_journal_deploy_20260903_234600
- September 3 source/live DLL SHA-256, rechecked September 5:
  39C34CB93FAEDAF765E1D7A5AD19983BBA644FCF6AC4A0DCDABD6AF288FC1E39
- Deployed assembly reference was verified as System.Data.SQLite 2.0.2.0, token db937bc2d44ff139.
- NinjaTrader was closed for September 3 deployment. Its current running state has not been checked in this documentation pass.

## Implemented and deployed, not yet live validated

- WPF Journal AddOn with Dashboard, Trades, and Tags surfaces; account execution capture and SQLite repositories.
- Annotation import accepts notes, tags, optional setup grade, TAG library entries, and HIDDEN_TAG entries.
- Schema 5 adds nullable setup grades to trades and pending annotations plus hidden-tag storage. Grade-only annotations survive.
- Matching annotations apply notes, tags, and grade together; grade is displayed in trade details.
- Finalized recorder manifests scanned during AddOn configuration and Journal open.
- Existing importer links only Complete manifests whose FinalVideoPath exists; links in place without duplicating large video files.
- Video entries in Media open through Windows-associated playback; repeated import is idempotent by trade ID/path.
- Exact composite-key duplicate trade insert guard also avoids duplicate session totals.
- Process-lifetime nonempty execution-ID dedupe per account.
- Account discovery once per second hooks accounts connected after startup.
- Earlier user confirmed pasted screenshots and, at one point, visible tags. Later reports included missing tags and inability to enlarge images. Do not interpret older successes as validation of this build.

## Architecture decisions and reasons

- Keep the external C# WPF/.NET Framework 4.8 DLL architecture for now. A .cs file is source; both deployment styles ultimately compile into assemblies. The separately built Journal DLL loaded in NinjaTrader requires process restart to replace loaded code. Suite NinjaScript source follows its own Custom compilation/load gate.
- WPF/NTWindow is the existing native Journal shell. Existing chart rendering uses SkiaSharp bitmap images; briefing/source comments attribute earlier LiveCharts2 removal to NinjaTrader D2D visual-tree conflicts. Reassess before introducing a new embedded renderer.
- Retain SQLite and additive migrations; do not invent a second competing annotation database.
- TSV is the existing chart-to-Journal compatibility bridge. Journal-side editing needs explicit field ownership, revision/conflict handling, and a writeback strategy before becoming bidirectional.
- Existing Core/TradeIdentity.cs is a composite key, NOT a durable execution UID: account, full contract, side, entry/exit ticks, entry/exit quantities, and round-trip price strings. Time conversion, aggregation, and floating-point differences can break equivalence.
- Proposed versioned trade_uid must share grouping semantics and execution provenance across producers. Account plus ordered IDs is a starting proposal, not a settled sufficient algorithm: define contract scope, ordering, partial fills, reversal allocation, missing IDs, and reconnect completeness.
- Current Journal recorder matching uses capture-wide account and instrument sets plus overlapping trade UTC interval with +/-5 seconds. Source also permits a short-instrument fallback. It does not yet read per-trade ledger IDs or schema-2 trade records.
- A clip can legitimately contain multiple trades. Treat trade/media association as many-to-many.
- Trade times with unspecified Kind are interpreted as local by the importer; manifest unspecified times as UTC. Include timezone/DST cases in identity and matching work.
- Market-data caches and additional Tick/Bid/Ask series are unnecessary for this account-execution/manifest workflow.

## Relevant source map

Paths below are relative to the external source root unless marked suite:
- OrcaJournal.cs: lifecycle, repositories, capture wiring, import/menu orchestration.
- OrcaJournal.csproj: framework and NinjaTrader/SQLite/rendering references.
- Core/TradeCapture.cs: account subscriptions, discovery, execution delivery dedupe.
- Core/TradeBuilder.cs: open-to-flat aggregation; reversal limitation.
- Core/TradeIdentity.cs: existing composite identity formatting.
- Data/DatabaseManager.cs: schema/migrations.
- Data/TradeRepository.cs: persistence, annotation application, duplicate guard.
- Data/ExecutionLineNotesImporter.cs: TSV compatibility and grade/library import.
- Data/ExecutionLineAnnotationRepository.cs: pending annotations and matching data.
- Data/TradeRecordingImporter.cs: manifest DTO, matching, import, output-directory discovery.
- Data/AttachmentRepository.cs and Data/Models/TradeAttachment.cs: media persistence.
- Data/Models/Trade.cs: grade and trade fields.
- Data/TagRepository.cs: tag repository.
- UI/ViewModels/TradesViewModel.cs: trade selection, media commands, display state.
- UI/Views/TradesView.xaml and .xaml.cs: trade list/detail/media/date-filter UI.
- UI/Windows/JournalWindow.cs, UI/OrcaResources.cs, UI/Themes/: window and styling.
- Analytics/ExportService.cs, KpiCalculator.cs, ChartRenderer.cs: existing export/analytics foundation.
- Suite Indicators/OrcaExecutionLines.cs: note/tag/grade editor, TSV writer, execution history, FIFO matching, chart context menu.
- Suite AddOns/OrcaTradeRecorderAddOn.cs: OBS runtime, capture manifest and OrcaRecorderTradeLedger.
- Repository tests/OrcaTradeRecorder/Verify.ps1: recorder ledger harness.
- Repository tests/OrcaExecutionLines.StartupCheck/: recent offline semantic/regression checks.

## New recorder work relevant to the next slice

September 5 recorder handoff and current source confirm schema-2 capture manifests include constituent trades and execution IDs, completeness, PnlBasis, and ResultTitle. Ledger groups by account/full contract, keeps scaling and partial exits together until flat, and splits reversals. Gross P&L excludes commissions. Unknown IDs/history/reconnects conservatively mark uncertainty.

Reuse or align with this ledger before creating another incompatible definition of a trade. Current Journal importer only consumes the older common fields. Keep legacy schema-1 captures readable. Existing finalized filenames were not renamed; new outputs gain result titles. FFmpeg finalization occurs on Disarm, so absence of a Journal video before finalization may be expected.

## Known bugs, risks, and failed approaches

- Prior disposable database inspection found 23 duplicate-key groups and 37 excess rows; these are historical measurements, not refreshed live counts.
- Most pending chart annotations did not match Journal rows in the prior snapshot. Exact-key dedupe does not solve reconciliation or keyless legacy records.
- Journal TradeBuilder does not split crossing-flat reversal fills; recorder now does. Resolve this discrepancy before promising shared durable identity.
- Capture-wide account/instrument sets can link unrelated combinations during overlaps. Prefer schema-2 per-trade evidence, retain conservative legacy fallback, and show ambiguous cases.
- Missing or moved media lacks a full maintenance/relink workflow.
- Prior UI complaints: Today absent inside calendar popup (toolbar Today was visible), low calendar contrast, mojibake in Export and empty-value placeholders, tiny/unopenable screenshot previews. Verify current behavior instead of asserting fixed.
- September 5 Execution Lines historical SQLite loading failed with AmbiguousMatchException on name-only Parameters reflection. Latest targeted fix uses Public | Instance | DeclaredOnly in two lookups; source/live parity and offline compile passed, F5/runtime still pending. Missing historical lines/annotations may depend on this producer-side issue.
- Previous SQLite 1.0.116.0 dependency caused OnStateChange FileNotFoundException. Current deployed reference is 2.0.2.0; do not blindly bundle an older SQLite provider.
- System.Web.Extensions introduced an unnecessary System.Web markup compilation dependency and failed build. Replaced with DataContractJsonSerializer.
- ReflectionOnlyLoadFrom failed in modern PowerShell/.NET. A temporary .NET Framework csc.exe helper successfully inspected metadata and was removed.
- Manifest scans currently enumerate recursively and compare trades on configure/open; evaluate growth before adding frequent polling.
- External source lacks version control. Establish a reviewed baseline before substantial edits; exclude generated bin/obj and private trading data.

## Verification evidence and remaining gates

Previously run for Journal:
- Release build: 0 errors, 4 existing MSB3277 reference warnings.
- Disposable database: schema 4 -> 5; 33 recognized TSV rows (21 trades, 10 tags, 2 hidden tags); 14 grades preserved; 2 grades matched existing trades.
- 29 trade/video links across 28 trades from 17 captures; second import added zero links.
- Duplicate keyed insert returned false.
- Guarded September 3 deployment backed up DLL/database, checked NinjaTrader stopped, verified DLL hashes and SQLite reference.
- Those tests were offline; temporary verification artifacts were removed. Do not claim a committed Journal regression test suite exists.

Build command:
    dotnet build "C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal.sln" --configuration Release --no-restore -v:minimal

September 5 recorder: 18 deterministic ledger assertions and full NinjaTrader-reference AddOn compilation passed; targeted deployment parity passed. Observed regenerated Custom.dll is not F5/load/manual proof.

Still required from Julian:
1. Start NinjaTrader and confirm no Journal SQLite/OnStateChange error.
2. Open Tools > Orca Journal; inspect a known chart-annotated trade for notes, tags, grade.
3. Disarm/finalize a recording and reopen Journal; verify correct video and Play.
4. Reopen again and verify no duplicate attachments.
5. Connect account after startup and complete a SIM trade; verify one Journal trade.
6. Check multiple accounts/full contracts, scaling, partial closes, reversals, and recorder tail overlaps.
7. Check calendar popup Today/contrast, placeholder text, and screenshot opening.
8. For latest Execution Lines fix: F5/load, no Parameters ambiguity, historical row counts and lines restored, account isolation/partial exits intact.

Julian said he would test the next morning; no result has been reported in this conversation. Treat runtime status as unconfirmed.

## Recommended roadmap

1. Validate deployed slice and repair reproducible defects. Record the exact build and user result.
2. Establish external Journal version control and implement Trade Identity and Reconciliation v1.
3. Trade Review Workspace: Journal-side notes/tags/grade editing with conflict handling; autosave feedback; large image viewer with zoom/pan/fit/fullscreen/next/previous; captions, ordering, removal.
4. Recorder experience: schema-2 precise linking, recording metadata, Play/Reveal/Relink/Unlink; later playback timestamps/bookmarks. Embedded playback is a product choice, not yet committed architecture.
5. Chart navigation: Open in Orca Journal selects an exact trade through a durable-ID request; later Show on Chart.
6. Review/learning: filters and saved views by tags, grade, session, account; screenshot-first daily/weekly reviews; reliable performance summaries.
7. Hardening/export: database integrity/backup and media maintenance, installation/dependency packaging; portable HTML/PDF/Notion export after native review is useful.

## Exact next task: Trade Identity and Reconciliation v1

Start by collecting validation results if available and inspecting current source/diffs. Independent design and disposable tests can proceed while live validation is pending.

First bounded deliverable:
- Document/test one shared flat-to-flat definition across chart, recorder, Journal, including reversal quantities.
- Define a versioned execution identity contract and completeness flags; do not generate confident IDs from insufficient history.
- Add additive provenance/UID storage and backward-compatible TSV/manifest handling.
- Build a read-only reconciliation report for exact, legacy, ambiguous, and unmatched annotations/media. Include explanations and counts.
- Keep existing links/history intact; manual review/merge UI is a later explicit step.
- Test repeated imports, same timestamps/prices, partial fills, scale-outs, reversals, duplicate delivery, missing IDs, account/contract separation, restarts, UTC/local/DST, and schema-1/schema-2 compatibility.

Likely files: Core/TradeIdentity.cs, TradeBuilder.cs, TradeCapture.cs; trade/annotation models and repositories; DatabaseManager.cs; ExecutionLineNotesImporter.cs; TradeRecordingImporter.cs; narrow authored regions of suite OrcaExecutionLines.cs and recorder ledger/manifest if needed. Inspect current code before deciding whether a shared source-linked helper or small contract fixture is the least disruptive way to keep producers consistent.

Acceptance: deterministic identity on complete equivalent executions; ambiguity visible rather than silently matched; no extra rows/links on reimport; all legacy notes, tags, grades, screenshots, and videos preserved. Keep migrations and reconciliation tests on disposable database copies.

## This documentation change

Files changed: this handoff and sibling Git status snapshot only. No application behavior/settings, secondary series, Tick Replay, historical loading, cache, rendering, or performance changes. No build rerun or NinjaTrader test in this pass; current DLL hash parity rechecked. No deployment or promotion. Full_Suite eligibility remains unvalidated for related suite work. Commit only these documentation files and preserve the existing dirty tree.
