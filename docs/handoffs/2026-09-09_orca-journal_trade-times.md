# Orca Journal — trade times — 2026-09-09

Objective: add Open time / Hold time / Close time in that order to Trades table; compact hold durations.
Files: external OrcaJournal/UI/Views/TradesView.xaml and Data/Models/Trade.cs.
Behavior: open/close show stored EntryTime/ExitTime in 24-hour HH:mm, sort by full underlying timestamps. Hold duration uses h/m/s with no spaces or commas (1m47s, 1h11m52s), including trade detail via shared property. Table hold width reduced to fit time columns. Stored values, calculations, timezone handling and exports unchanged. Missing/negative holds remain dash. No settings, schema or media changes.
No secondary series, Tick Replay, historical load, cache or per-tick changes. Rendering only formatting/columns. No Working_Suite/Full_Suite edits.
Validation: Release 0 errors / 4 existing assembly warnings. Existing WPF review, gallery, saved-combination and sorting harness passed. NinjaTrader startup/manual validation pending; not eligible for Full_Suite promotion.
Stage: .codex-backups/journal-trade-times-stage/OrcaJournal.dll
SHA256 F9894598E85B456A892F0598EA0C3338A5175EA60BF46E21BBE6E9042A1A383E
Not deployed. Prior live baseline 5BC7CA068EF3FE908E92E52596D30B9C3979D8D15BE6A7BCA0A5442A63C8ECE2.
Screenshot also shows MAE/MFE unavailable due to out-of-order price/execution timestamps; no new runtime diagnosis or capture change in this formatting slice.
