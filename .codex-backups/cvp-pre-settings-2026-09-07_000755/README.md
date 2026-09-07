# CVP before settings reorganization

Verified snapshot of current on-disk Working_Suite CVP, its rendering partial and footprint core, CVP documentation and focused tests. Separate live-reference files preserve installed source without treating it as development source.

Restore procedure:
1. Preserve any newer CVP edits in a separate backup.
2. Extract the ZIP into a temporary directory outside NinjaTrader compile folders.
3. Verify extracted files against manifest.json SHA-256 values.
4. Restore only the intended files from workspace/ to their matching repository paths. Review changes to shared OrcaFootprintCore before restoring it. Do not bulk-restore other files or use live-reference to overwrite development source.
5. If rollback deployment is needed, deploy only the restored CVP files using the normal targeted process, compile with NinjaTrader F5, and manually check existing charts and saved templates.

The ZIP includes uncommitted source exactly as captured. No indicator logic or settings were changed. This is a local source rollback snapshot, not an off-machine backup, platform installer, compiled assembly backup, chart-template backup, workspace backup, or market-data backup. Existing validation status is not upgraded.
