using System;
using System.Drawing;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;

namespace ControlIndexIndicator
{
    public class ControlIndexV2 : Indicator
    {
        [InputParameter("Lookback Period for Delta Average", 10, 1, 100)]
        public int LookbackPeriod = 20;

        [InputParameter("Minimum Delta Threshold (Contracts)", 20, 0, 100000)]
        public int MinDeltaThreshold = 100;

        private List<double> _absDeltaHistory;

        public ControlIndexV2()
        {
            Name = "Control Index V2 (Absorption)";
            Description = "Highlights when aggressive buyers or sellers are being absorbed, shifting control.";

            // Add an explicit separate window for the Histogram
            AddLineSeries("Absorption Signal", Color.Gray, 2, LineStyle.Histogramm);
            SeparateWindow = true;
        }

        protected override void OnInit()
        {
            _absDeltaHistory = new List<double>();
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (HistoricalData == null || HistoricalData.Count == 0)
                return;

            int currentIndex = Count - 1;

            // 1. FAST EXTRACTION: Use native Volume properties directly from the Bar. This avoids the 99% freeze.
            // Requires Volume Analysis / Footprint to be enabled on the chart.
            var vaData = HistoricalData[currentIndex]?.VolumeAnalysisData;
            
            double askVolume = vaData != null ? vaData.Total.BuyVolume : 0;
            double bidVolume = vaData != null ? vaData.Total.SellVolume : 0;

            if (double.IsNaN(askVolume)) askVolume = 0;
            if (double.IsNaN(bidVolume)) bidVolume = 0;

            // 2. Compute Effort (Delta)
            double barDelta = askVolume - bidVolume;
            double absDelta = Math.Abs(barDelta);

            // Store for rolling average calculations
            if (_absDeltaHistory.Count <= currentIndex)
                _absDeltaHistory.Add(absDelta);
            else
                _absDeltaHistory[currentIndex] = absDelta;

            if (Count < LookbackPeriod) return;

            // Calculate rolling SMA of absolute delta to know what "High Effort" looks like
            double sumDelta = 0;
            for (int i = 0; i < LookbackPeriod; i++)
            {
                sumDelta += _absDeltaHistory[currentIndex - i];
            }
            double avgAbsDelta = sumDelta / LookbackPeriod;

            // 3. Compute Result (Candlestick progression)
            double open = Open();
            double close = Close();
            
            bool isBullishCandle = close > open;
            bool isBearishCandle = close < open;
            bool isDoji = close == open;

            // 4. Determine Absorption Control
            double controlSignal = 0; // 0 = Agreement / Neutral
            Color signalColor = Color.Gray;

            // Rules for Absorption:
            // "Effort vs Result"
            // If Buyers show high effort (Positive Delta > Average) but Price goes down (Red Candle) = Sellers Abosrbed (Control = Bear)
            // If Sellers show high effort (Negative Delta < Average) but Price goes up (Green Candle) = Buyers Absorbed (Control = Bull)

            if (absDelta >= MinDeltaThreshold && absDelta >= avgAbsDelta)
            {
                if (barDelta > 0 && (isBearishCandle || isDoji))
                {
                    // Aggressive Buyers trapped/absorbed by Passive Sellers -> Bears in Control
                    // Plot the absolute Delta to show EXACTLY how many contracts were trapped
                    controlSignal = -absDelta; 
                    signalColor = Color.Red;
                }
                else if (barDelta < 0 && (isBullishCandle || isDoji))
                {
                    // Aggressive Sellers trapped/absorbed by Passive Buyers -> Bulls in Control
                    controlSignal = absDelta;
                    signalColor = Color.LimeGreen;
                }
            }

            // Plot final Control Index block
            if (controlSignal != 0)
            {
                SetValue(controlSignal, 0);
                LinesSeries[0].SetMarker(0, signalColor);
            }
            else 
            {
                // Base state: Plot the raw Delta so we can visually confirm data is flowing.
                SetValue(barDelta, 0);
                LinesSeries[0].SetMarker(0, Color.Gray);
            }
        }
    }
}
