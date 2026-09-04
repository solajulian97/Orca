# 2026-09-01 - Orca Journal Integrity, Setup Grades, And Trade Recordings

## Objective

Make the external Orca Journal a more cohesive owner of post-trade context by preserving Execution Lines setup grades and tag-library state, preventing new exact-key duplicate trades, discovering late-connected accounts, and linking finalized Orca Trade Recorder videos to the trades visible in each capture.

## Files changed

External Journal source at `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal`:

- `OrcaJournal.cs`
- `OrcaJournal.csproj`
- `Core/TradeCapture.cs`
- `Data/AttachmentRepository.cs`
- `Data/DatabaseManager.cs`
- `Data/ExecutionLineAnnotationRepository.cs`
- `Data/ExecutionLineNotesImporter.cs`
- `Data/TradeRecordingImporter.cs` (new)
- `Data/TradeRepository.cs`
- `Data/Models/Trade.cs`
- `Data/Models/TradeAttachment.cs`
- `UI/ViewModels/TradesViewModel.cs`
- `UI/Views/TradesView.xaml`

Repo documentation:

- `docs/handoffs/2026-09-01_orca-journal_integrity-grades-recordings.md`

`Orca Trades/Working_Suite` and `Orca Trades/Full_Suite` were not changed.

## Behavior added, changed, or removed

- Execution Lines TSV import now accepts optional setup grades, global known tags, and hidden tag options.
- Journal schema version 5 adds nullable setup-grade fields to completed trades and pending Execution Lines annotations, plus the hidden-tag table for fresh and migrated databases.
- Applying an Execution Lines annotation to a Journal trade now transfers notes, tags, and setup grade together. Grade-only annotations are retained.
- Setup grade appears in the selected trade detail panel.
- Finalized Orca Trade Recorder `capture.json` manifests are scanned at Journal startup and each Journal open. A completed video is linked to every Journal trade whose account, full instrument, and trade interval occur within that recording window.
- Recorder videos remain in their original bundle; Journal stores a file link and does not copy large media files.
- One recording may link to several trades when positions overlap or a new trade begins during the recorder tail, matching the recorder's global-capture design.
- Video attachments appear in the renamed Media section and open through the Windows-associated media player.
- Re-running recording import is idempotent by trade ID and file path.
- New completed trades with an existing exact composite trade key are rejected before insert, preventing further exact-key duplicates and duplicate session totals.
- Journal account capture now reconciles `Account.All` once per second and idempotently hooks accounts that connect after AddOn startup.
- Repeated delivery of the same non-empty NinjaTrader execution ID is ignored per account for the process lifetime.
- Existing duplicate or unmatched historical records are not deleted, merged, or rewritten by this change.

## User-facing settings added, changed, deprecated, or removed

- No Journal or NinjaScript setting was added.
- Recorder output discovery reads the existing `Documents/NinjaTrader 8/OrcaTradeRecorder.xml` `OutputDirectory` value and falls back to `Videos/Orca Trade Recordings`.
- The Trades detail section is renamed from Images to Media and shows Play for videos.

## Secondary series added or changed

- None.
- No Tick, Second, Bid, Ask, Last, Volumetric, or custom series was added or changed.

## Tick Replay implications

- None. Journal capture is account-execution driven and recorder linking is manifest/file driven.

## Historical-load implications

- Schema migrations are additive and run when the Journal AddOn configures.
- Existing exact duplicate groups and unmatched annotations remain unchanged pending a separate reconciliation workflow.
- Legacy keyless trade rows are not covered by the exact-key duplicate guard.
- The known Journal reversal limitation remains: a fill crossing through flat is not split into close and new-position portions.

## Cache implications

- No Orca market-data cache is used.
- Process-lifetime account subscriptions and processed execution IDs are retained in bounded-by-session hash sets.
- Recorder scans are idempotent against persisted attachment links.

## Rendering implications

- WPF trade details display setup grade and a distinct video placeholder.
- Image viewing remains unchanged; video playback is delegated to the Windows-associated player.
- No SharpDX or chart-render path changed.

## Performance implications

- Account discovery performs a small `Account.All` reconciliation once per second.
- Recorder import scans 63 current manifest files only at AddOn configure and Journal menu open, not on market-data or account callbacks.
- Video files are linked in place, avoiding copies and duplicate storage.
- Execution callbacks add one hash-set lookup when an execution ID exists.

## Tests performed in NinjaTrader

- No live NinjaTrader test has been performed yet. NinjaTrader was confirmed stopped for the 2026-09-03 deployment and remained stopped through post-copy verification.
- User previously reported routine Trade Recorder automatic start/stop behavior and usable OBS output quality; recorder failure-mode validation was not repeated in this pass.

## Compile status

- External Release build passed with 0 errors and 4 pre-existing assembly-version conflict warnings.
- An initial build failed because `System.Web.Extensions` pulled an unnecessary `System.Web` markup-compiler dependency. The implementation was changed to the built-in .NET data-contract JSON serializer; the final build passed.
- The rebuilt DLL was copied to `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\OrcaJournal.dll` on 2026-09-03 while NinjaTrader was stopped.
- Source and deployed SHA-256 hashes both equal `39C34CB93FAEDAF765E1D7A5AD19983BBA644FCF6AC4A0DCDABD6AF288FC1E39`.
- .NET Framework metadata inspection confirms the deployed DLL references `System.Data.SQLite, Version=2.0.2.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139`.

## Manual-validation status

- Deployment is complete; NinjaTrader restart and live validation are pending.
- Open Journal and confirm a graded Execution Lines trade shows its grade, notes, and tags.
- Select a trade covered by a finalized recorder capture and confirm Media shows an OBS trade recording and Play opens it.
- Reopen Journal and confirm media links are not duplicated.
- Connect an account after NinjaTrader startup, complete a SIM trade, and confirm Journal captures it once.

## Verification outside NinjaTrader

- Release build: 0 errors, 4 existing MSB3277 warnings.
- Guarded deployment backup: `C:\tmp\orca_journal_deploy_20260903_234600` contains the prior live DLL and the pre-migration Journal database.
- Post-copy source/live hash parity passed and NinjaTrader was confirmed not running.
- Deployed assembly reference verification passed for `System.Data.SQLite` version `2.0.2.0`.
- Disposable SQLite backup migrated from schema 4 to schema 5.
- TSV import produced 33 recognized rows: 21 trade rows, 10 known-tag rows, and 2 hidden-tag rows.
- All 14 TSV setup grades were retained in pending annotations; 2 currently matched existing Journal trades in the disposable database.
- Recorder import linked 29 trade/video pairs across 28 trades from 17 matching completed captures.
- A second import created 0 additional video links.
- Re-inserting an existing keyed trade returned false from the duplicate guard.

## Known issues, risks, and follow-up work

- The external Journal source is still outside Git and should be put under version control before larger UI work.
- Existing data contains 23 duplicate trade-key groups with 37 excess rows; no destructive cleanup was attempted.
- Most current pending Execution Lines annotations still do not match Journal trades. A versioned trade UID based on ordered execution IDs remains the preferred identity follow-up.
- Recorder manifests contain capture-wide account and instrument sets rather than per-position time segments. Matching intentionally links any account/instrument trade overlapping the capture window; unusually complex overlapping activity should be manually reviewed.
- Missing or moved video files remain visible as broken links until a future media maintenance workflow is added.
- Journal-side editing, image zoom/pan/gallery controls, recorder media management, and chart-to-Journal navigation remain future phases.

## Full_Suite promotion eligibility

- Not applicable to the external Journal DLL.
- No Working_Suite or Full_Suite source was changed.
- Live DLL deployment is complete. Julian's manual NinjaTrader validation remains required before treating this Journal slice as validated.
