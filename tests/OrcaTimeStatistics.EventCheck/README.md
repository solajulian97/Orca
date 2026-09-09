# Time Statistics event assignment check

Run from the repository root on a Windows machine with the .NET 8 SDK and NinjaTrader installed in their standard locations:

```powershell
dotnet run --project tests/OrcaTimeStatistics.EventCheck
```

The runner compiles the complete authored indicator against installed platform references, excluding generated NinjaScript wrappers. It then extracts the actual OnMarketData, storage, extrema, and Finish Delta methods into a .NET Framework executable with modeled bar/event inputs. It does not load an indicator into NinjaTrader or reproduce platform scheduling.

Checks cover distinct assigned bars with identical timestamps, historical/realtime delivery states, time bars, avoiding future/invalid-bar writes, signed-volume conservation, extrema/Finish Delta, remembered quotes and tick-direction fallbacks, shared-only exclusion, hybrid local assignment, and preservation of the legacy timestamp path outside Calculate.OnEachTick.

An optional first argument selects a source file, allowing the pre-change backup to demonstrate the regression. An optional second argument supplies the baseline for a full syntax-preservation check outside the two declared edits:

```powershell
dotnet run --project tests/OrcaTimeStatistics.EventCheck -- 'Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs' '.codex-backups/time-statistics-inactive-20260909/OrcaTimeStatistics.before.cs'
```

F5, successful Custom assembly load, and paired visible/background chart testing remain separate manual gates. Volume split across multiple bars by one trade is not modeled by this check.
