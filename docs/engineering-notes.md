# Engineering Notes

## NinjaTrader Gotchas

- NinjaTrader compiles the whole `Documents/NinjaTrader 8/bin/Custom` tree. Stale source files can create duplicate definitions or missing-name errors that do not exist in the current repo source.
- The NinjaScript Editor delete operation is not always enough when the compiler is confused. Check the filesystem directly for ghost files.
- Historical, Tick Replay, and realtime behavior can differ, especially for bid/ask and order-flow calculations.
- Avoid assuming that bid/ask state is always available historically.

## SharpDX Rules

- Every render-target-bound brush must be disposed and recreated for a new render target.
- Avoid allocating expensive DirectWrite objects in tight render loops.
- If text measurement is cached, invalidate the cache when font size, font face, scale, or render conditions change.
- Watch for chart zoom/pan paths; they can trigger high-frequency rendering and expose expensive calculations.

## Serialization And Properties

NinjaTrader XML serialization does not safely serialize WPF brushes directly. Use this pattern:

```csharp
[XmlIgnore]
[Display(Name = "Delta Color", GroupName = "Visual")]
public System.Windows.Media.Brush DeltaColor { get; set; }

[Browsable(false)]
public string DeltaColorSerializable
{
    get { return Serialize.BrushToString(DeltaColor); }
    set { DeltaColor = Serialize.StringToBrush(value); }
}
```

## Order-Flow Data

For delta-style calculations, use bid/ask data when available. When historical bid/ask state is missing, the suite may fall back to uptick/downtick classification to avoid dropping data.

## Known Current Themes

- Dynamic aggregation is used to prevent dense profile/delta rendering from becoming unreadable when zoomed out.
- `FinishDelta` naming should replace older `DeltaEfficiency` naming in current source.
- Deployment scripts must not copy from stale `NinjaTrader` mirror files over newer `Working_Suite` or `Full_Suite` files.

## OrcaJournal Note

The handoff references OrcaJournal, SQLite, MVVM, `TradeCapture`, `TradeBuilder`, and LiveCharts2. Those files are not present in this checkout. Treat journal details as future/separate context until the source is added.
