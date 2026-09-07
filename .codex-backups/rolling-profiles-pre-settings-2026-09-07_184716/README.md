# Rolling Profiles before settings cleanup

Verified local snapshot of current Working_Suite Rolling Profiles, including uncommitted work. Shared core, diagnostics and provider copies are reference only. Installed source is captured separately under live-reference.

Restore procedure:
1. Preserve any newer Rolling Profiles edits separately.
2. Extract the ZIP outside NinjaTrader compile folders; verify files against manifest.json SHA-256 values.
3. Restore only workspace/Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs to its matching repository path after reviewing the diff.
4. Do not restore shared core/provider/diagnostics dependencies as part of a settings rollback. Do not copy live-reference into development source.
5. If deployment is needed, use the exact-target Working_Suite deployment, compile with NinjaTrader F5, and manually check existing charts/templates.

This is a local source snapshot, not an off-machine backup. Templates, workspaces, compiled binaries, market data, runtime caches and full transitive dependencies are excluded. Existing validation status is unchanged. Never leave duplicate .cs files inside NinjaTrader Custom.
