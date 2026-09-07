# Step Profile before settings reorganization

SHA-256 verified snapshot of current on-disk Working_Suite Step Profile, diagnostics dependency (reference only), focused tests and Step Profile handoffs. Separate live-reference files preserve installed source without treating it as development source. Includes all current uncommitted changes at capture.

Restore:
1. Preserve newer Step Profile edits separately.
2. Extract the ZIP outside all NinjaTrader compile folders.
3. Verify the extracted files against manifest.json SHA-256 values.
4. Restore workspace/Orca Trades/Working_Suite/Indicators/OrcaStepProfile.cs to its matching repository path. Review the diff first. Do not restore the shared diagnostics dependency as part of a settings rollback; its copy is context only. Do not copy live-reference into Working_Suite.
5. If deployment is needed, use the normal exact-target Step Profile deployment, then NinjaTrader F5 and manual chart/template checks. Never place duplicate .cs copies inside NinjaTrader Custom.

This is a local source snapshot. It does not include chart templates, workspaces, market data, runtime state, compiled binaries or all platform dependencies. No off-machine backup/push was performed. Existing compile and manual-validation status is not upgraded.
