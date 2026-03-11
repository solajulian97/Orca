// Quantower custom indicator: Footprint Imbalance
// Draws stacked imbalance zones using diagonal bid/ask comparison.
// Zones extend horizontally until mitigated by price closing through them.
// Requires: Quantower Algo / ScriptBuilder, IVolumeAnalysisIndicator (volume analysis data loaded for the symbol).

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace QuantowerFootprintImbalance
{
    /// <summary>
    /// Represents a contiguous run of 3+ consecutive price levels
    /// that all share the same diagonal imbalance direction.
    /// </summary>
    internal class ImbalanceZone
    {
        public int BarIndex;           // bar where the zone originated
        public double TopPrice;        // highest price in the zone
        public double BottomPrice;     // lowest price in the zone
        public bool IsBuyZone;         // true = buy imbalance (bullish)
        public bool Mitigated;         // true once price closes through
        public int MitigatedBarIndex;  // bar index where mitigation happened
    }

    public class FootprintImbalanceIndicator : Indicator, IVolumeAnalysisIndicator
    {
        private bool _volumeAnalysisLoaded;
        private readonly List<ImbalanceZone> _zones = new List<ImbalanceZone>();
        private int _lastProcessedCount;

        // ── Parameters ───────────────────────────────────────────────

        [InputParameter("Imbalance ratio", 0, 1.5, 10.0, 0.1, 1)]
        public double ImbalanceRatio { get; set; } = 2.5;

        [InputParameter("Min volume at level", 1, 0, 10000, 1, 0)]
        public double MinVolumeAtLevel { get; set; } = 0;

        [InputParameter("Min consecutive levels", 2, 2, 10, 1, 0)]
        public int MinConsecutiveLevels { get; set; } = 3;

        [InputParameter("Buy zone color", 3)]
        public Color BuyZoneColor { get; set; } = Color.FromArgb(140, 0, 200, 0);

        [InputParameter("Sell zone color", 4)]
        public Color SellZoneColor { get; set; } = Color.FromArgb(140, 200, 0, 0);

        [InputParameter("Extension line width", 5, 1, 5, 1, 0)]
        public int ExtensionLineWidth { get; set; } = 1;

        [InputParameter("Show extension lines", 6)]
        public bool ShowExtensionLines { get; set; } = true;

        [InputParameter("Show zone boxes", 7)]
        public bool ShowZoneBoxes { get; set; } = true;

        [InputParameter("Fill extension zones", 8)]
        public bool FillExtensionZones { get; set; } = true;

        [InputParameter("Buy extension fill color", 9)]
        public Color BuyExtensionFillColor { get; set; } = Color.FromArgb(40, 0, 200, 0);

        [InputParameter("Sell extension fill color", 10)]
        public Color SellExtensionFillColor { get; set; } = Color.FromArgb(40, 200, 0, 0);

        // ── Constructor ──────────────────────────────────────────────

        public FootprintImbalanceIndicator()
            : base()
        {
            Name = "Footprint Imbalance";
            Description = "Diagonal bid/ask stacked imbalance zones with extension lines and mitigation.";
            SeparateWindow = false;
            AddLineSeries("dummy", Color.Gray, 1, LineStyle.Solid);
        }

        // ── IVolumeAnalysisIndicator ─────────────────────────────────

        public bool IsRequirePriceLevelsCalculation => true;

        public void VolumeAnalysisData_Loaded()
        {
            _volumeAnalysisLoaded = true;
            _lastProcessedCount = 0;
            _zones.Clear();
        }

        // ── OnUpdate ─────────────────────────────────────────────────

        protected override void OnUpdate(UpdateArgs args)
        {
            SetValue(0);

            if (!_volumeAnalysisLoaded)
                return;

            if (HistoricalData?.VolumeAnalysisCalculationProgress == null ||
                HistoricalData.VolumeAnalysisCalculationProgress.State != VolumeAnalysisCalculationState.Finished)
                return;

            int count = HistoricalData.Count;

            // On full recalc, rebuild everything
            if (args.Reason == UpdateReason.HistoricalBar && _lastProcessedCount == 0)
            {
                _zones.Clear();
                for (int i = 0; i < count; i++)
                {
                    ProcessBar(i);
                    CheckMitigation(i);
                }
                _lastProcessedCount = count;
                return;
            }

            // New bar appeared
            if (count > _lastProcessedCount)
            {
                for (int i = _lastProcessedCount; i < count; i++)
                {
                    ProcessBar(i);
                    CheckMitigation(i);
                }
                _lastProcessedCount = count;
            }
            else
            {
                // Current bar updated — reprocess the last bar's zones and mitigation
                int lastBar = count - 1;
                if (lastBar >= 0)
                {
                    // Remove zones from this bar (they'll be recalculated)
                    _zones.RemoveAll(z => z.BarIndex == lastBar);
                    ProcessBar(lastBar);

                    // Re-check mitigation for current bar close
                    CheckMitigation(lastBar);
                }
            }
        }

        // ── Zone Detection ───────────────────────────────────────────

        /// <summary>
        /// For the given bar, compute diagonal imbalances level-by-level,
        /// then find runs of MinConsecutiveLevels or more and create ImbalanceZones.
        /// </summary>
        private void ProcessBar(int barIndex)
        {
            if (barIndex < 0 || barIndex >= HistoricalData.Count)
                return;

            var item = HistoricalData[barIndex, SeekOriginHistory.Begin];
            if (!(item is HistoryItemBar bar) || bar.VolumeAnalysisData?.PriceLevels == null)
                return;

            var levels = bar.VolumeAnalysisData.PriceLevels;
            if (levels.Count < 2)
                return;

            // Sort price levels ascending
            var sortedPrices = levels.Keys.OrderBy(p => p).ToList();

            // Build per-level imbalance flags: +1 = buy, -1 = sell, 0 = none
            var flags = new int[sortedPrices.Count];

            for (int i = 0; i < sortedPrices.Count; i++)
            {
                double price = sortedPrices[i];
                var level = levels[price];
                double askVol = level.BuyVolume;   // aggressive buys (hitting the ask)
                double bidVol = level.SellVolume;  // aggressive sells (hitting the bid)

                // Buy imbalance: ask at this level vs bid one tick below
                if (i > 0)
                {
                    double priceBelowBid = levels[sortedPrices[i - 1]].SellVolume;
                    if (askVol >= MinVolumeAtLevel && priceBelowBid >= 0)
                    {
                        if (priceBelowBid <= 0)
                        {
                            if (askVol >= MinVolumeAtLevel)
                                flags[i] = 1;
                        }
                        else if (askVol >= ImbalanceRatio * priceBelowBid)
                        {
                            flags[i] = 1;
                        }
                    }
                }

                // Sell imbalance: bid at this level vs ask one tick above
                if (i < sortedPrices.Count - 1 && flags[i] == 0)
                {
                    double priceAboveAsk = levels[sortedPrices[i + 1]].BuyVolume;
                    if (bidVol >= MinVolumeAtLevel && priceAboveAsk >= 0)
                    {
                        if (priceAboveAsk <= 0)
                        {
                            if (bidVol >= MinVolumeAtLevel)
                                flags[i] = -1;
                        }
                        else if (bidVol >= ImbalanceRatio * priceAboveAsk)
                        {
                            flags[i] = -1;
                        }
                    }
                }
            }

            // Scan for consecutive runs of the same direction
            int runStart = 0;
            for (int i = 1; i <= sortedPrices.Count; i++)
            {
                bool sameRun = (i < sortedPrices.Count) && (flags[i] == flags[runStart]) && (flags[i] != 0);
                if (!sameRun)
                {
                    int runLen = i - runStart;
                    if (flags[runStart] != 0 && runLen >= MinConsecutiveLevels)
                    {
                        _zones.Add(new ImbalanceZone
                        {
                            BarIndex = barIndex,
                            BottomPrice = sortedPrices[runStart],
                            TopPrice = sortedPrices[i - 1],
                            IsBuyZone = flags[runStart] == 1,
                            Mitigated = false,
                            MitigatedBarIndex = -1
                        });
                    }
                    runStart = i;
                }
            }
        }

        /// <summary>
        /// Check if any existing unmitigated zones are mitigated by the close of the given bar.
        /// Buy zone mitigated when close &lt; zone bottom.
        /// Sell zone mitigated when close &gt; zone top.
        /// </summary>
        private void CheckMitigation(int barIndex)
        {
            if (barIndex < 0 || barIndex >= HistoricalData.Count)
                return;

            var item = HistoricalData[barIndex, SeekOriginHistory.Begin];
            if (!(item is HistoryItemBar bar))
                return;

            double close = bar.Close;

            foreach (var zone in _zones)
            {
                if (zone.Mitigated)
                    continue;

                // Don't mitigate on the same bar that created the zone
                if (zone.BarIndex >= barIndex)
                    continue;

                if (zone.IsBuyZone && close < zone.BottomPrice)
                {
                    zone.Mitigated = true;
                    zone.MitigatedBarIndex = barIndex;
                }
                else if (!zone.IsBuyZone && close > zone.TopPrice)
                {
                    zone.Mitigated = true;
                    zone.MitigatedBarIndex = barIndex;
                }
            }
        }

        // ── Drawing ──────────────────────────────────────────────────

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (!_volumeAnalysisLoaded || CurrentChart?.MainWindow == null)
                return;

            if (HistoricalData?.VolumeAnalysisCalculationProgress == null ||
                HistoricalData.VolumeAnalysisCalculationProgress.State != VolumeAnalysisCalculationState.Finished)
                return;

            if (_zones.Count == 0)
                return;

            var mainWindow = CurrentChart.MainWindow;
            var gr = args.Graphics;
            var prevClip = gr.ClipBounds;
            gr.SetClip(mainWindow.ClientRectangle);

            try
            {
                DateTime leftTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left);
                DateTime rightTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right);
                int leftIndex = (int)mainWindow.CoordinatesConverter.GetBarIndex(leftTime);
                int rightIndex = (int)Math.Ceiling(mainWindow.CoordinatesConverter.GetBarIndex(rightTime));

                int barWidth = Math.Max(1, CurrentChart.BarsWidth);
                int chartRight = mainWindow.ClientRectangle.Right;

                using (var buyBrush = new SolidBrush(BuyZoneColor))
                using (var sellBrush = new SolidBrush(SellZoneColor))
                using (var buyPen = new Pen(BuyZoneColor, ExtensionLineWidth))
                using (var sellPen = new Pen(SellZoneColor, ExtensionLineWidth))
                using (var buyFillBrush = new SolidBrush(BuyExtensionFillColor))
                using (var sellFillBrush = new SolidBrush(SellExtensionFillColor))
                {
                    buyPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    sellPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                    foreach (var zone in _zones)
                    {
                        // Determine horizontal extent of the zone
                        int endBarIndex;
                        if (zone.Mitigated)
                        {
                            endBarIndex = zone.MitigatedBarIndex;
                        }
                        else
                        {
                            endBarIndex = rightIndex + 5; // extend beyond visible area
                        }

                        // Skip zones entirely outside visible range
                        if (endBarIndex < leftIndex || zone.BarIndex > rightIndex)
                            continue;

                        // Vertical coordinates
                        int yTop = (int)mainWindow.CoordinatesConverter.GetChartY(zone.TopPrice);
                        int yBottom = (int)mainWindow.CoordinatesConverter.GetChartY(zone.BottomPrice);
                        if (yTop > yBottom)
                        {
                            int tmp = yTop; yTop = yBottom; yBottom = tmp;
                        }
                        int zoneHeight = Math.Max(1, yBottom - yTop);

                        // X coordinates
                        var originItem = HistoricalData[zone.BarIndex, SeekOriginHistory.Begin];
                        int barLeftX = (int)Math.Round(
                            mainWindow.CoordinatesConverter.GetChartX(
                                originItem is HistoryItemBar originBar ? originBar.TimeLeft : originItem.TimeLeft));

                        bool isBuy = zone.IsBuyZone;
                        var brush = isBuy ? buyBrush : sellBrush;
                        var pen = isBuy ? buyPen : sellPen;
                        var fillBrush = isBuy ? buyFillBrush : sellFillBrush;

                        // Draw the zone box on the originating bar
                        if (ShowZoneBoxes && zone.BarIndex >= leftIndex && zone.BarIndex <= rightIndex)
                        {
                            gr.FillRectangle(brush, barLeftX, yTop, barWidth, zoneHeight);
                        }

                        // Draw extension lines and fill
                        if (ShowExtensionLines)
                        {
                            int lineStartX = barLeftX + barWidth;
                            int lineEndX;

                            if (zone.Mitigated)
                            {
                                var mitItem = HistoricalData[zone.MitigatedBarIndex, SeekOriginHistory.Begin];
                                lineEndX = (int)Math.Round(
                                    mainWindow.CoordinatesConverter.GetChartX(
                                        mitItem is HistoryItemBar mitBar ? mitBar.TimeLeft : mitItem.TimeLeft));
                            }
                            else
                            {
                                lineEndX = chartRight;
                            }

                            if (lineEndX > lineStartX)
                            {
                                // Fill the extension zone between top and bottom lines
                                if (FillExtensionZones)
                                {
                                    gr.FillRectangle(fillBrush, lineStartX, yTop, lineEndX - lineStartX, zoneHeight);
                                }

                                // Draw border lines at top and bottom
                                gr.DrawLine(pen, lineStartX, yTop, lineEndX, yTop);
                                gr.DrawLine(pen, lineStartX, yBottom, lineEndX, yBottom);
                            }
                        }
                    }
                }
            }
            finally
            {
                gr.SetClip(prevClip);
            }
        }
    }
}
