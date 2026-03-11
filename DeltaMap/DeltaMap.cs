using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;

namespace Quantower.Indicators
{
    public class DeltaMap : Indicator, IVolumeAnalysisIndicator
    {
        [InputParameter("Delta Multiplier Threshold", 10)]
        public double DeltaMultiplier = 2.0;

        [InputParameter("Use Strict Absorption Logic", 15)]
        public bool UseAbsorptionLogic = true;

        [InputParameter("Average Period", 20)]
        public int MovingAveragePeriod = 20;

        [InputParameter("Buy Exhaustion / Top Color", 30)]
        public Color BuyExhaustionColor = Color.Cyan;

        [InputParameter("Sell Exhaustion / Bottom Color", 40)]
        public Color SellExhaustionColor = Color.Magenta;

        [InputParameter("Consecutive Bars Required", 50)]
        public int ConsecutiveBars = 1;

        [InputParameter("Min Bar Volume", 60)]
        public double MinBarVolume = 0;

        private class SignalData 
        {
            public Color Color { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
            public double Open { get; set; }
            public double Close { get; set; }
        }

        private Dictionary<DateTime, SignalData> signalCache = new Dictionary<DateTime, SignalData>();
        private Dictionary<DateTime, Color?> rawSignals = new Dictionary<DateTime, Color?>();
        private string debugMsg = "Waiting for data...";
        private bool historyLoaded = false;
        private bool _volumeAnalysisLoaded = false;

        public DeltaMap()
        {
            Name = "DeltaMap Overlay";
            Description = "Highlights specific candlesticks based on extreme Net Delta / Absorption.";
            SeparateWindow = false;
        }

        // ── IVolumeAnalysisIndicator ─────────────────────────────────

        public bool IsRequirePriceLevelsCalculation => true;

        public void VolumeAnalysisData_Loaded()
        {
            _volumeAnalysisLoaded = true;
            historyLoaded = false;
            signalCache.Clear();
            rawSignals.Clear();
        }

        protected override void OnInit()
        {
            signalCache.Clear();
            rawSignals.Clear();
            historyLoaded = false;
            _volumeAnalysisLoaded = false;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (Count == 0) return;

            // Wait until IVolumeAnalysisIndicator callback has fired
            if (!_volumeAnalysisLoaded) return;

            if (HistoricalData?.VolumeAnalysisCalculationProgress == null ||
                HistoricalData.VolumeAnalysisCalculationProgress.State != VolumeAnalysisCalculationState.Finished)
            {
                debugMsg = "Waiting for Volume Analysis...";
                return;
            }

            if (!historyLoaded)
            {
                debugMsg = $"Processing History... (0/{Count})";
                // Process all historical bars on the first pass
                for (int i = 0; i < Count; i++)
                {
                    ProcessBar(i);
                }
                historyLoaded = true;
            }
            else if (args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.NewTick)
            {
                // Only process the live bar
                ProcessBar(Count - 1);
            }
        }

        private void ProcessBar(int index)
        {
            var vaData = HistoricalData[index]?.VolumeAnalysisData;
            double askVolume = vaData != null ? vaData.Total.BuyVolume : 0;
            double bidVolume = vaData != null ? vaData.Total.SellVolume : 0;
            double totalVolume = askVolume + bidVolume;

            if (double.IsNaN(askVolume)) askVolume = 0;
            if (double.IsNaN(bidVolume)) bidVolume = 0;

            double barDelta = askVolume - bidVolume;
            double absDelta = Math.Abs(barDelta);
            DateTime barTime = Time(index);

            // 1. Initial filter: Min Volume & History length
            if (index < MovingAveragePeriod || totalVolume < MinBarVolume) 
            {
                rawSignals[barTime] = null;
                signalCache.Remove(barTime);
                return;
            }

            // 2. Compute Average
            double sumAbsDelta = 0;
            int validDataPoints = 0;
            for (int i = 1; i <= MovingAveragePeriod; i++)
            {
                var pastVa = HistoricalData[index - i]?.VolumeAnalysisData;
                if (pastVa != null)
                {
                    double pAsk = double.IsNaN(pastVa.Total.BuyVolume) ? 0 : pastVa.Total.BuyVolume;
                    double pBid = double.IsNaN(pastVa.Total.SellVolume) ? 0 : pastVa.Total.SellVolume;
                    sumAbsDelta += Math.Abs(pAsk - pBid);
                    if (Math.Abs(pAsk - pBid) > 0) validDataPoints++;
                }
            }

            double avgAbsDelta = (validDataPoints > 0) ? (sumAbsDelta / validDataPoints) : 1;
            if (avgAbsDelta == 0) avgAbsDelta = 1;

            double threshold = avgAbsDelta * DeltaMultiplier;
            debugMsg = $"Avg: {avgAbsDelta:F1} | Threshold: {threshold:F1} | Delta: {absDelta:F1} | V: {totalVolume:F0} | VAData: {(vaData != null ? "Yes" : "No")}";

            Color? currentCondition = null;
            double cClose = Close(index);
            double cOpen = Open(index);
            
            if (absDelta > 0 && absDelta >= threshold)
            {
                if (barDelta > 0) 
                {
                    if (!UseAbsorptionLogic || cClose <= cOpen)
                        currentCondition = BuyExhaustionColor; 
                }
                else if (barDelta < 0)
                {
                    if (!UseAbsorptionLogic || cClose >= cOpen)
                        currentCondition = SellExhaustionColor;
                }
            }

            // Update raw signals
            rawSignals[barTime] = currentCondition;

            // 3. Check Consecutive Confirmation
            bool isConfirmed = false;
            if (currentCondition.HasValue)
            {
                if (ConsecutiveBars <= 1)
                {
                    isConfirmed = true;
                }
                else
                {
                    // Check previous N-1 bars
                    int matchCount = 1;
                    for (int i = 1; i < ConsecutiveBars; i++)
                    {
                        if (index - i < 0) break;
                        DateTime prevTime = Time(index - i);
                        if (rawSignals.TryGetValue(prevTime, out Color? prevSig) && prevSig == currentCondition)
                            matchCount++;
                        else
                            break;
                    }
                    if (matchCount >= ConsecutiveBars)
                        isConfirmed = true;
                }
            }

            if (isConfirmed)
            {
                signalCache[barTime] = new SignalData 
                { 
                    Color = currentCondition.Value, 
                    High = High(index), 
                    Low = Low(index), 
                    Open = Open(index), 
                    Close = Close(index) 
                };
            }
            else
            {
                if (signalCache.ContainsKey(barTime))
                    signalCache.Remove(barTime);
            }
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);
            if (CurrentChart == null) return;

            Graphics gr = args.Graphics;
            var mainWindow = CurrentChart.MainWindow;
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var state = gr.Save();
            try 
            {
                gr.SetClip(mainWindow.ClientRectangle);

                // --- DEBUG INFO ---
                gr.DrawString($"DeltaMap Cached Signals: {signalCache.Count}", new Font("Arial", 10), Brushes.Yellow, 10, 30);
                gr.DrawString(debugMsg, new Font("Arial", 10), Brushes.LightGray, 10, 50);
                
                if (signalCache.Count == 0) return;

                DateTime leftTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left);
                DateTime rightTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right);

                // Buffer panning time by a lot to ensure edge candles paint nicely
                leftTime = leftTime.AddDays(-1);
                rightTime = rightTime.AddDays(1);

                var visibleSignals = signalCache.Where(k => k.Key >= leftTime && k.Key <= rightTime).ToList();
                if (visibleSignals.Count == 0) return;

                // Use Quantower's native bar width for consistency
                int barWidth = Math.Max(2, CurrentChart.BarsWidth); 
                // Alternatively, if BarsWidth is too small, use a percentage of the actual spacing
                if (barWidth < 4 && Count >= 2)
                {
                    int x1 = (int)mainWindow.CoordinatesConverter.GetChartX(Time(Count - 1));
                    int x2 = (int)mainWindow.CoordinatesConverter.GetChartX(Time(Count - 2));
                    barWidth = Math.Max(2, (int)(Math.Abs(x1 - x2) * 0.6));
                }

                foreach (var kvp in visibleSignals)
                {
                    DateTime t = kvp.Key;
                    var data = kvp.Value;

                    int x = (int)mainWindow.CoordinatesConverter.GetChartX(t);
                    
                    int yTop = (int)mainWindow.CoordinatesConverter.GetChartY(data.High);
                    int yBot = (int)mainWindow.CoordinatesConverter.GetChartY(data.Low);
                    int yBodyTop = (int)mainWindow.CoordinatesConverter.GetChartY(Math.Max(data.Open, data.Close));
                    int yBodyBot = (int)mainWindow.CoordinatesConverter.GetChartY(Math.Min(data.Open, data.Close));

                    int bodyHeight = Math.Max(1, yBot - yTop); 
                    int rectHeight = Math.Max(2, yBodyBot - yBodyTop);

                    // Re-draw the wick
                    using (Pen wickPen = new Pen(data.Color, 2))
                    {
                        gr.DrawLine(wickPen, x, yTop, x, yBot);
                    }

                    // Re-draw the body natively glowing over the previous candle
                    using (SolidBrush bodyBrush = new SolidBrush(data.Color))
                    using (Pen borderPen = new Pen(data.Color, 1))
                    {
                        int boxLeft = x - (barWidth / 2);
                        gr.FillRectangle(bodyBrush, boxLeft, yBodyTop, barWidth, rectHeight);
                        gr.DrawRectangle(borderPen, boxLeft, yBodyTop, barWidth, rectHeight);
                    }
                }
            } 
            finally 
            {
                gr.Restore(state);
            }
        }
    }
}
