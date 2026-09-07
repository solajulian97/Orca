# Orca Journal — Tag Performance spacing — 2026-09-07

## Objective / changes
User reported successful deployed UI cleanup and requested more spacing and capitalized Performance. Increased margins between filters, saved combinations, tag picker, metrics/help and result tabs; larger row minimum and checkbox spacing. Capitalized tab to Tag Performance.
Files: external OrcaJournal/UI/Views/TagPerformanceView.cs and UI/Windows/JournalWindow.cs.
No settings or database changes; annotation/media and unrelated work preserved.

## Validation and implications
Release build 0 errors / 4 existing assembly warnings. Existing WPF saved-combination, analytics, review/media checks passed. Render inspected at 1200x720. No secondary series, Tick Replay, historical-load, cache or per-tick work changes. Rendering/layout only; more vertical space consumed. No NinjaTrader tests for this new build; manual validation pending. User screenshot confirms previous deployed layout loads, not saved-view persistence or MAE/MFE behavior.
Stage .codex-backups/journal-tag-spacing-stage/OrcaJournal.dll SHA256 C02D55FABFAB603695F3E0223B15A679F054DE4C56E3C24B61E2564542E28447. Not deployed. Live saved-view DLL remains unchanged. Not eligible for Full_Suite promotion.

## Proposed next slice: Tag Library (not implemented)
Current TagManagerViewModel loads legacy TagRepository tags/rules; unlike review and performance UI, it does not consult TradeReviewRepository.TagOptions or journal_tag_groups. Performance picker intentionally shows tags assigned to snapshot trades plus selected tags. This explains the three used tags and category discrepancy.
Recommend grouped Tag Library using Risk & Trade Management and Analysis & Execution. Share review tag taxonomy and effective review-overridden tag membership. Selecting a tag shows attached screenshots from matching round trips; open image/review with date/instrument/outcome context and optional video launch. Use existing attachment repository/thumbnail decoder and respect detached/missing files. Page or virtualize media, decode bounded thumbnails, avoid loading full library images at once. Keep auto-tag rules in collapsed advanced section and preserve existing rules. Renaming/deleting tags needs a separate preservation policy for review text/history/imported annotations; do not silently rewrite them. User currently exploring this idea; no gallery implementation in this slice.
