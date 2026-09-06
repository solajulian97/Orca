# Orca Journal simple review and grouped tags — 2026-09-06

## Objective and runtime feedback
Julian confirmed the dashboard changes look good and provided screenshots of the dashboard/calendar and dark screenshot window. He found the review version buttons confusing and the tag library absent from the editor. Simplify ordinary review to edit/save; expose conflict choices only when needed; offer existing chart tags grouped by risk/trade management versus analysis/execution. Typeless context was acknowledged conversationally, not persisted as personal memory.

## Findings and changes
The read-only chart TSV inspection found 10 TAG library rows (FVG, iFVG, MGI, node, OB, Passive Player, RB, Structure, Sweep, TAPER), 21 TRADE rows and 2 hidden tags. The old editor textbox only listed applied trade tags, not selectable library options. No missing data or lost link was established. The new picker offers the imported library without applying all tags to every trade.
External source C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal:
- Data/DatabaseManager.cs: additive schema 8 journal_tag_groups table, storing name/category. Existing review/history tables remain unchanged.
- Data/ReviewTagOption.cs: Risk / trade management, Analysis / execution, Other groups. Known chart technical terms default to Analysis; unfamiliar tags stay Other. User examples are unselected suggested choices: Risked proper amount, Took proper partials, Sweep of the low with a passive buyer, Taper in the volume profile, Displacement on the 2-minute.
- Data/TradeReviewRepository.cs: library options plus applied tags, hidden-library filtering without dropping applied tags, typed conflict exception, atomic save of deliberately changed groups. New tag names enter the existing library; category changes do not rewrite other tags. Saved category metadata is Journal-side, not written to ExecutionLines.
- UI/Windows/TradeReviewWindow.cs: grouped checkbox tags, add-tag field/category, right-click move group. Only Save review appears normally. Conflicting edits reveal both versions and Keep my changes / Use saved review / Use chart notes. Drafts remain intact. No explicit Check latest versions command needed; source refresh/conflict checks remain in save/open paths.
- Coordination tests/OrcaJournal.Identity/Program.cs, ReviewChecks.cs and tests/OrcaJournal.ReviewUI/Program.cs updated.

Chart annotations still import into Journal; this slice does not implement Journal-to-chart write-back. Saved review/history and imported annotations remain separately preserved. Field semantics and ownership are unchanged; only conflicting versions require user choices. Notes that were never applied to the selected trade are not inferred or attached by name alone.

## Verification
Release net48 x64 build passed, 0 errors, 4 existing MSB3277 warnings. 22 review database assertions and 85 existing identity/reconciliation assertions pass, including migration preservation, library availability without applying tags, categories and stored grouping. UI harness exercises actual compiled editor: note/grade/checkbox editing, save feedback, an external annotation change, draft preservation, automatic conflict choices and explicit resolution preserving source notes. An offscreen nonactivating WPF window and dispatcher render pass provide a stable preview; no NinjaTrader UI was automated. Preview inspected at C:\Users\julia\AppData\Local\Temp\orca-review-ui-23f6747155e44192af3c0d2d022fa57f\review.png. Earlier unshown-window renders omitted cached visual regions; harness corrected to render a shown offscreen window.
All databases used for tests were disposable. Live TSV was read only. No live data changes, source deployments or platform restart performed.

## Stage and gates
C:\Users\julia\Documents\New project\.codex-backups\journal-tag-groups-stage\OrcaJournal.dll
SHA256 BED0AF5D9461D2020687F771CAA327505C239A78D8AA266B2CA8667AFD4FC664.
NinjaTrader remains running (observed PID 64884). Not deployed. Expected installed baseline D02966A1B116E207F04C9969E41A676F082D67B476581DFF3A363C84F1830C32. Before authorized copy after closure: verify process absence, hashes/SQLite metadata; back up DLL/database/sidecars/annotation TSV; copy only staged DLL; verify parity. Schema 8 initializes at next startup.

## Implications and next validation
No persistent indicator settings, secondary series, AddDataSeries, OnMarketData, Calculate modes, Tick Replay, historical-load, cache or trading logic changes. WPF picker rebuilds only on edit/group/load actions; no per-tick/render calculations. No performance benchmark. Existing annotations/media/IDs/history retained. Full_Suite untouched and ineligible for promotion.
Manual validation pending for this new editor: normal screen shows only Save; chart library offered; applied tags selected; custom tags and group moves persist after Save/reopen; hidden library options stay hidden unless already applied; changed chart notes cause conflict choices without dropping the draft; dark styling and window sizing. Dashboard appearance and screenshot dark styling are user-confirmed; broader identity/SIM validation remains separate and pending. Future roadmap work remains after this usability checkpoint.
