# Leg-to-Leg before settings cleanup

Verified local snapshot of current Working_Suite Leg-to-Leg, including uncommitted work. Diagnostics/replay source copies are references only. Installed sources and saved templates are separate archive sections.

Restore procedure:
1. Preserve newer Leg-to-Leg source edits and templates separately.
2. Extract the ZIP outside NinjaTrader compile folders. Verify extracted files against manifest.json SHA-256 values.
3. Restore only workspace/Orca Trades/Working_Suite/Indicators/OrcaLegtoLegProfile.cs to the matching repository path after reviewing the diff.
4. Do not restore shared diagnostics/replay dependencies wholesale or copy live-reference into Working_Suite.
5. For deployment, use the exact-target Working_Suite process, NinjaTrader F5 and manual chart/template checks.
6. Saved templates are a separate recovery option, not required for source rollback. Restore only specifically selected files from saved-templates/ to their matching NinjaTrader templates paths after preserving newer versions. Chart templates may include other indicators and chart settings; do not restore them in bulk.

This is a local backup, with no remote push. It includes saved chart/indicator templates containing Leg-to-Leg, not unsaved chart state, workspaces, market data, compiled binaries or full transitive dependencies. Manual reset anchors are transient runtime state and are not preserved by templates. Existing compile/runtime validation status is unchanged.
