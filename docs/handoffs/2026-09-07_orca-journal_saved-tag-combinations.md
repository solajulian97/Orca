# Orca Journal — saved tag combinations — 2026-09-07

## Objective and behavior
Add named reusable tag combinations before deployment. Save new, explicitly update selected, load, and confirm-delete combinations. Restore selected tags (including currently unused tags), All/Any matching, outcome bucket and breakeven amount. Preserve current account, instrument and date scope. Duplicate names are protected case-insensitively. Empty tag selection represents all tags.

Breakeven threshold is now persisted and shared between Trades and Tag performance within the Journal window. Invalid negative/nonfinite values are rejected; missing/invalid stored preference defaults to $20.

## Files changed
External source: Data/DatabaseManager.cs, Data/SavedTagViewRepository.cs, UI/Views/TagPerformanceView.cs under C:/Users/julia/projects/OrcaTrading/OrcaJournal/OrcaJournal.
Coordination tests: tests/OrcaJournal.Identity/Program.cs, SavedViewChecks.cs; tests/OrcaJournal.ReviewUI/Program.cs.

## Storage and safety
Additive schema 11 tables journal_tag_views and journal_preferences. Existing reviews, annotation links, tags, attachments and trade data are not rewritten. Tests use disposable databases; live database untouched. Selected saved combination does not auto-load on startup.

## Platform implications
No secondary series, AddDataSeries, OnMarketData or calculation mode changes. No Tick Replay or historical-load changes. No data cache changes. Rendering adds one wrapping toolbar and status line; dark theme retained. Preset reads/writes occur on user actions; no per-tick work. No Working_Suite or Full_Suite changes.

## Validation
Release build: 0 errors, 4 existing MSB3277 assembly warnings. 8 persistence checks plus 23 tag-performance, 25 excursion/title, 13 risk/media, 22 review and 85 identity assertions passed. WPF harness verifies save/load restores tags and matching while preserving date scope; prior review/media and tag analytics UI checks passed. Render inspected; corrected themed ComboBox display to show saved name.
No NinjaTrader compile/load/runtime tests performed for this change. NinjaTrader PID 71880 still running. MAE/MFE revised live capture remains awaiting user runtime validation.

## Stage / deployment
Combined build includes pending expectancy/average winner/average loser/layout improvements.
Stage: .codex-backups/journal-saved-views-stage/OrcaJournal.dll
SHA256: 789CAB49F7E4AA30E1377B665997FA1D6600B5B12B0BF26FD7268685B3A8B449
Not deployed. Guarded DLL backup/copy required once NinjaTrader is closed. Existing live DLL remains unchanged.
Not eligible for Full_Suite promotion; manual NinjaTrader validation pending.
