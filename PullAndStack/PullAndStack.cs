using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;

namespace Orca.PullAndStack
{
    /// <summary>
    /// Bottom-panel indicator showing:
    ///   Series 0 – Normalized Ratio histogram: (bids-asks)/(bids+asks)*100  [-100..+100]
    ///   Series 1 – Cumulative Vacuum line: running sum of raw (bids-asks)
    /// </summary>
    public class PullAndStackVacuum : Indicator
    {
        [InputParameter("Levels to Scan", 10, 1, 50)]
        public int LevelsToScan = 10;

        [InputParameter("Cumulative Reset on Session", 20)]
        public bool ResetOnSession = true;

        // Current snapshot values
        private double _normalizedRatio = 0;
        private double _cumulativeVacuum = 0;
        private DateTime _lastDomUpdate = DateTime.MinValue;
        private DateTime _sessionDate = DateTime.MinValue;

        public PullAndStackVacuum()
        {
            Name = "Orca Pull and Stack (Vacuum)";
            Description = "Plots Normalized Bid/Ask Ratio histogram and Cumulative Vacuum trend line.";

            // Series 0: Normalized ratio as a histogram bar (-100 to +100)
            AddLineSeries("Norm Ratio", Color.FromArgb(120, 100, 149, 237), 3, LineStyle.Histogramm);
            // Series 1: Cumulative vacuum as a smooth trend line
            AddLineSeries("Cumulative Vacuum", Color.FromArgb(255, 255, 215, 0), 2, LineStyle.Solid);

            SeparateWindow = true;
        }

        protected override void OnInit()
        {
            if (Symbol != null)
            {
                Symbol.NewQuote += OnNewQuote;
            }
        }

        protected override void OnClear()
        {
            if (Symbol != null)
            {
                Symbol.NewQuote -= OnNewQuote;
            }
            _cumulativeVacuum = 0;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            // Plot the two series
            SetValue(_normalizedRatio, 0);
            SetValue(_cumulativeVacuum, 1);
        }

        private void OnNewQuote(Symbol symbol, Quote quote)
        {
            if ((Core.TimeUtils.DateTimeUtcNow - _lastDomUpdate).TotalMilliseconds < 100) return;
            _lastDomUpdate = Core.TimeUtils.DateTimeUtcNow;

            if (symbol.DepthOfMarket == null) return;

            var aggregatedDOM = symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections();
            if (aggregatedDOM == null) return;

            var bids = aggregatedDOM.Bids;
            var asks = aggregatedDOM.Asks;

            if (bids == null || asks == null || bids.Length == 0 || asks.Length == 0) return;

            // Reset cumulative on new trading session
            DateTime now = Core.TimeUtils.DateTimeUtcNow;
            if (ResetOnSession && now.Date != _sessionDate)
            {
                _cumulativeVacuum = 0;
                _sessionDate = now.Date;
            }

            int bidLevels = Math.Min(LevelsToScan, bids.Length);
            int askLevels = Math.Min(LevelsToScan, asks.Length);

            double bidTotal = 0;
            double askTotal = 0;

            for (int i = 0; i < bidLevels; i++) bidTotal += bids[i]?.Size ?? 0;
            for (int i = 0; i < askLevels; i++) askTotal += asks[i]?.Size ?? 0;

            // --- Normalized Ratio: percentage of liquidity skew ---
            // +100 = all liquidity on bids (strong support), -100 = all on asks (strong resistance)
            double totalLiquidity = bidTotal + askTotal;
            _normalizedRatio = totalLiquidity > 0
                ? ((bidTotal - askTotal) / totalLiquidity) * 100.0
                : 0;

            // --- Cumulative Vacuum: running sum of raw delta ---
            // Trends down when bids are being pulled, trends up when asks are pulled
            double rawDelta = bidTotal - askTotal;
            _cumulativeVacuum += rawDelta;
        }
    }

    /// <summary>
    /// Main-chart overlay that draws horizontal lines at prices where
    /// large resting limit orders ("walls") are detected in the DOM.
    /// </summary>
    public class PullAndStackWalls : Indicator
    {
        [InputParameter("Minimum Wall Size", 10)]
        public int MinWallSize = 10;

        [InputParameter("Wall Size Multiplier", 20)]
        public double WallMultiplier = 1.5;

        [InputParameter("Levels to Scan", 30, 1, 50)]
        public int LevelsToScan = 10;

        [InputParameter("Bid Wall Color", 40)]
        public Color BidWallColor = Color.FromArgb(180, 0, 200, 0);

        [InputParameter("Ask Wall Color", 50)]
        public Color AskWallColor = Color.FromArgb(180, 255, 50, 50);

        [InputParameter("Wall Line Width", 60, 1, 10)]
        public int WallLineWidth = 2;

        [InputParameter("Wall Line Style", 65, variants: new object[] {
            "Solid", 0,
            "Dash", 1,
            "Dot", 2,
            "Dash-Dot", 3,
            "Dash-Dot-Dot", 4
        })]
        public int WallLineStyle = 1;

        [InputParameter("Show Size Labels", 70)]
        public bool ShowSizeLabels = true;

        private List<WallInfo> _bidWalls = new List<WallInfo>();
        private List<WallInfo> _askWalls = new List<WallInfo>();
        private DateTime _lastDomUpdate = DateTime.MinValue;

        private struct WallInfo
        {
            public double Price;
            public double Size;
        }

        public PullAndStackWalls()
        {
            Name = "Orca Pull and Stack (Walls)";
            Description = "Draws horizontal lines where large resting orders are detected.";

            // Dummy transparent line so OnPaintChart fires
            AddLineSeries("_hidden", Color.Transparent, 1, LineStyle.Solid);
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            if (Symbol != null)
            {
                Symbol.NewQuote += OnNewQuote;
            }
        }

        protected override void OnClear()
        {
            if (Symbol != null)
            {
                Symbol.NewQuote -= OnNewQuote;
            }
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            // Nothing to plot on the line series itself
        }

        private void OnNewQuote(Symbol symbol, Quote quote)
        {
            if ((Core.TimeUtils.DateTimeUtcNow - _lastDomUpdate).TotalMilliseconds < 200) return;
            _lastDomUpdate = Core.TimeUtils.DateTimeUtcNow;

            if (symbol.DepthOfMarket == null) return;

            var aggregatedDOM = symbol.DepthOfMarket.GetDepthOfMarketAggregatedCollections();
            if (aggregatedDOM == null) return;

            var bids = aggregatedDOM.Bids;
            var asks = aggregatedDOM.Asks;

            if (bids == null || asks == null || bids.Length == 0 || asks.Length == 0) return;

            var newBidWalls = new List<WallInfo>();
            var newAskWalls = new List<WallInfo>();

            // Analyze Bids
            int bidLevels = Math.Min(LevelsToScan, bids.Length);
            if (bidLevels > 0)
            {
                if (bidLevels <= 3)
                {
                    // Shallow depth (L1 data) — can't do relative comparison
                    // Just show any level that exceeds the absolute minimum
                    for (int i = 0; i < bidLevels; i++)
                    {
                        if (bids[i] != null && bids[i].Size >= MinWallSize)
                        {
                            newBidWalls.Add(new WallInfo { Price = bids[i].Price, Size = bids[i].Size });
                        }
                    }
                }
                else
                {
                    // Deep depth — use relative multiplier comparison
                    double avgBidSize = 0;
                    for (int i = 0; i < bidLevels; i++) avgBidSize += bids[i]?.Size ?? 0;
                    avgBidSize /= bidLevels;

                    double bidThreshold = Math.Max((double)MinWallSize, avgBidSize * WallMultiplier);

                    for (int i = 0; i < bidLevels; i++)
                    {
                        if (bids[i] != null && bids[i].Size >= bidThreshold)
                        {
                            newBidWalls.Add(new WallInfo { Price = bids[i].Price, Size = bids[i].Size });
                        }
                    }
                }
            }

            // Analyze Asks
            int askLevels = Math.Min(LevelsToScan, asks.Length);
            if (askLevels > 0)
            {
                if (askLevels <= 3)
                {
                    // Shallow depth (L1 data) — absolute threshold only
                    for (int i = 0; i < askLevels; i++)
                    {
                        if (asks[i] != null && asks[i].Size >= MinWallSize)
                        {
                            newAskWalls.Add(new WallInfo { Price = asks[i].Price, Size = asks[i].Size });
                        }
                    }
                }
                else
                {
                    // Deep depth — use relative multiplier comparison
                    double avgAskSize = 0;
                    for (int i = 0; i < askLevels; i++) avgAskSize += asks[i]?.Size ?? 0;
                    avgAskSize /= askLevels;

                    double askThreshold = Math.Max((double)MinWallSize, avgAskSize * WallMultiplier);

                    for (int i = 0; i < askLevels; i++)
                    {
                        if (asks[i] != null && asks[i].Size >= askThreshold)
                        {
                            newAskWalls.Add(new WallInfo { Price = asks[i].Price, Size = asks[i].Size });
                        }
                    }
                }
            }

            _bidWalls = newBidWalls;
            _askWalls = newAskWalls;
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);
            if (CurrentChart == null) return;

            var gr = args.Graphics;
            var window = CurrentChart.MainWindow;
            if (window == null) return;

            var clipState = gr.Save();
            try
            {
                gr.SetClip(window.ClientRectangle);

                using (Pen bidPen = new Pen(BidWallColor, WallLineWidth))
                using (Pen askPen = new Pen(AskWallColor, WallLineWidth))
                {
                    var dashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    switch (WallLineStyle)
                    {
                        case 0: dashStyle = System.Drawing.Drawing2D.DashStyle.Solid; break;
                        case 1: dashStyle = System.Drawing.Drawing2D.DashStyle.Dash; break;
                        case 2: dashStyle = System.Drawing.Drawing2D.DashStyle.Dot; break;
                        case 3: dashStyle = System.Drawing.Drawing2D.DashStyle.DashDot; break;
                        case 4: dashStyle = System.Drawing.Drawing2D.DashStyle.DashDotDot; break;
                    }
                    bidPen.DashStyle = dashStyle;
                    askPen.DashStyle = dashStyle;

                    foreach (var wall in _bidWalls)
                    {
                        int y = (int)window.CoordinatesConverter.GetChartY(wall.Price);
                        if (y >= window.ClientRectangle.Top && y <= window.ClientRectangle.Bottom)
                        {
                            gr.DrawLine(bidPen, window.ClientRectangle.Left, y, window.ClientRectangle.Right, y);

                            if (ShowSizeLabels)
                            {
                                string label = $"BID {wall.Size:F0}";
                                using (Font f = new Font("Arial", 8))
                                using (SolidBrush b = new SolidBrush(BidWallColor))
                                {
                                    gr.DrawString(label, f, b, window.ClientRectangle.Left + 5, y - 14);
                                }
                            }
                        }
                    }

                    foreach (var wall in _askWalls)
                    {
                        int y = (int)window.CoordinatesConverter.GetChartY(wall.Price);
                        if (y >= window.ClientRectangle.Top && y <= window.ClientRectangle.Bottom)
                        {
                            gr.DrawLine(askPen, window.ClientRectangle.Left, y, window.ClientRectangle.Right, y);

                            if (ShowSizeLabels)
                            {
                                string label = $"ASK {wall.Size:F0}";
                                using (Font f = new Font("Arial", 8))
                                using (SolidBrush b = new SolidBrush(AskWallColor))
                                {
                                    gr.DrawString(label, f, b, window.ClientRectangle.Left + 5, y + 2);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                gr.Restore(clipState);
            }
        }
    }
}
