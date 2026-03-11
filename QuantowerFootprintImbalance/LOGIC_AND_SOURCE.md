# Logic and data source (Quantower Footprint Imbalance)

This document describes **where the indicator gets its data** and **how it decides what counts as an imbalance**, for context and for anyone (e.g. agents) reading this project.

---

## Where the data comes from

- The indicator implements **`IVolumeAnalysisIndicator`** and sets **`IsRequirePriceLevelsCalculation = true`**. That tells Quantower to load **volume-by-price (footprint) data** for the chart’s symbol and timeframe.
- Quantower provides that data per bar in **`bar.VolumeAnalysisData.PriceLevels`**: a dictionary of **price → VolumeAnalysisItem**. Each item has:
  - **BuyVolume** – volume traded as buy (aggressive buy / at ask)
  - **SellVolume** – volume traded as sell (aggressive sell / at bid)
  - Plus other fields (Delta, Trades, etc.) that this indicator does not use.
- So the **source** is Quantower’s internal footprint/volume analysis for the same symbol and period as the chart. There is no separate feed; it’s the same data that powers the cluster/footprint chart.

---

## Imbalance logic

At **each price level** inside a bar we only use **BuyVolume** and **SellVolume** for that price.

- **Buy imbalance**  
  - One side dominates: **BuyVolume ≥ ImbalanceRatio × SellVolume**.  
  - Example: ratio **2.0** → we need buy volume at least 2× sell volume (e.g. 20 vs 8).  
  - If **SellVolume = 0** and BuyVolume &gt; 0, we still treat it as buy imbalance as long as total volume at that level is ≥ **Min volume at level** (otherwise we skip).

- **Sell imbalance**  
  - Same idea the other way: **SellVolume ≥ ImbalanceRatio × BuyVolume**.  
  - If **BuyVolume = 0** and SellVolume is above the min filter, that’s sell imbalance.

So the rule is: **at this price, did one side trade at least X times more than the other?**  
**X** = the **Imbalance ratio** parameter (default 2.0 = “2:1 or more”).

**Min volume at level** only filters out low-volume levels: we still use the same imbalance rule, but we don’t draw anything if `BuyVolume + SellVolume` at that price is below that threshold.

---

## Summary

- **Data:** Quantower’s footprint (buy/sell volume per price) for the chart’s symbol and period.  
- **Logic:** Mark a price as imbalance when one side’s volume is at least `Imbalance ratio` times the other side’s volume at that same price; optionally skip levels below `Min volume at level`.
