# Orca Journal — Tag Library and baseline comparison — 2026-09-07

## Objective and behavior
User approved grouped visual Tag Library and selected-combination vs all-trades comparison. Tags tab becomes Tag Library. Established groups retained: Risk & Trade Management; Analysis & Execution. Show suggested/stored/effectively used tags with trade counts and search. Select tag to browse dated full-instrument/direction/account/P&L cards, images, video actions, and full review. Clicking preview opens review; separate screenshot action opens zoom viewer. Trades without media and unavailable images have explicit placeholders. Gallery pages at 18 cards; only current-page thumbnails decode (existing bounded OnLoad decoder). Add tags and move selected tags between groups using shared review taxonomy.

Legacy tag manager/rule tools preserved inside collapsed Advanced. Source search of RuleJson/GetRulesForTag found storage/editor only, no automatic rule evaluator. UI explicitly says automatic application is not implemented. No new automatic tags or rule execution in this change.

Tag Performance gains Compared with all trades tab: selected combination and complete scope share dates, account, instrument, outcome and threshold. Baseline includes selected trades, explicitly stated. Counts, win rate, expectancy, average winner/loser, RR and risk coverage shown. Empty results retain unknown metrics. Includes previously staged spacing/capitalization.

## Files changed
External OrcaJournal: Data/AttachmentRepository.cs (active attachment metadata bulk read), Data/TradeReviewRepository.cs (transactional tag group save), UI/Views/TagLibraryView.cs (new), UI/Views/TagPerformanceView.cs, UI/Windows/JournalWindow.cs.
Coordination: tests/OrcaJournal.ReviewUI/Program.cs and this handoff.

## Safety/settings/implications
No schema migration. Tag group save updates category/catalog only, preserving existing reviews/history and media; explicit add/group can unhide that tag. Existing legacy delete semantics are unchanged in Advanced. Gallery uses TagPerformanceRepository effective tags, including saved review overrides. Detached attachments excluded; no file mutation. No secondary series, AddDataSeries, OnMarketData, Tick Replay or historical-load changes. No cache changes. No per-tick work; metadata loaded on refresh, bounded image decode on page selection. Metadata materializes library in memory; large-library benchmarks not performed. No Working_Suite/Full_Suite edits.

## Verification
Release 0 errors, 4 existing assembly warnings. WPF harness validates effective-tag media, category persistence without changing review notes, unused-tag empty state, collapsed Advanced, 18/5 paging for 23 active media, detached exclusion, selected/all comparison and empty scope. Existing saved-view/review/media UI checks pass. Render inspected and polished dark preview/selection backgrounds.
Identity suite passes: 8 saved views, 23 tag performance, 25 excursion/title, 13 risk/media, 22 review, 85 identity assertions.
No NinjaTrader runtime tests for new build; full review modal routing/video shell launch need manual validation. Prior MAE/MFE live capture validation still pending.

## Stage
.codex-backups/journal-tag-library-stage/OrcaJournal.dll
SHA256 6E4B98AAFB51FAAABE3357C786C37DB99AD269C4CD01AAA15E27BB78C4889C94
Not deployed. Current live expected baseline remains 789CAB49F7E4AA30E1377B665997FA1D6600B5B12B0BF26FD7268685B3A8B449. Guarded closed-platform deployment required. Not eligible for Full_Suite promotion.

## Future auto-tag work
Optional factual rules: session/time, duration, instrument/direction, realized R when planned risk exists; later scale/partial patterns and validated excursion thresholds. Require preview/dry run, reason and rule version, missing-data handling, manual overrides and undo before bulk application. Do not infer setup quality or proper risk from P&L alone. Existing rules preserved but dormant.
