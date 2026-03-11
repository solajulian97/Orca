using System;
using System.Drawing;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;

namespace Orca.VolumeRPM
{
    public class VolumeRPM : Indicator
    {
        [InputParameter("Visual Type", 10, variants: new object[] {
            "Histogram", 0,
            "Line", 1
        })]
        public int VisualType = 0;

        [InputParameter("Histogram Width", 12, 1, 10)]
        public int HistogramWidth = 3;

        [InputParameter("Measurement Window (Seconds)", 15, 1, 60)]
        public int WindowSeconds = 3;

        [InputParameter("Smoothing (Periods)", 20, 1, 100)]
        public int SmoothingPeriods = 3;

        [InputParameter("Show Rev Meter Overlay", 30)]
        public bool ShowRevMeter = true;

        [InputParameter("Rev Meter X Offset", 40)]
        public int RevMeterXOffset = 20;

        [InputParameter("Rev Meter Y Offset", 50)]
        public int RevMeterYOffset = 50;

        [InputParameter("Rev Meter Width", 60)]
        public int RevMeterWidth = 20;

        [InputParameter("Rev Meter Height", 70)]
        public int RevMeterHeight = 200;

        [InputParameter("Max RPM Threshold", 80)]
        public double MaxRpmThreshold = 100.0;

        [InputParameter("Dynamic Auto-Scale RPM", 82)]
        public bool DynamicAutoScale = true;

        [InputParameter("Auto-Scale Multiplier", 84)]
        public double AutoScaleMultiplier = 2.0;

        [InputParameter("Idle Color", 90)]
        public Color IdleColor = Color.DarkGray;

        [InputParameter("Mid Rev Color", 100)]
        public Color MidRevColor = Color.Yellow;

        [InputParameter("Redline Color", 110)]
        public Color RedlineColor = Color.Red;

        [InputParameter("Historical Spike Cap (RPM)", 120)]
        public double HistoricalSpikeCap = 5000.0;

        private List<double> _rpmHistory;

        // Data structure for the rolling window
        private struct TradeData
        {
            public DateTime Time;
            public double Volume;
        }

        private readonly Queue<TradeData> _recentTrades = new Queue<TradeData>();
        private double _rollingVolume = 0;
        private double _currentRpm = 0;

        public VolumeRPM()
            : base()
        {
            Name = "Volume RPM";
            Description = "Measures volume velocity (volume per second) to gauge market momentum.";
            SeparateWindow = true;

            AddLineSeries("RPM", Color.DodgerBlue, 2, LineStyle.Histogramm);
            AddLineSeries("RPM Smoothed", Color.DarkOrange, 1, LineStyle.Solid);
        }

        protected override void OnInit()
        {
            // Set line style based on user preference
            LinesSeries[0].Style = VisualType == 0 ? LineStyle.Histogramm : LineStyle.Solid;
            LinesSeries[0].Width = HistogramWidth;

            // Subscribe to real-time ticks to build the rev meter
            if (Symbol != null)
            {
                Symbol.NewLast += OnNewTrade;
            }

            // Initialize history list for custom SMA calculation
            _rpmHistory = new List<double>();
        }

        protected override void OnClear()
        {
            if (Symbol != null)
            {
                Symbol.NewLast -= OnNewTrade;
            }
            _recentTrades.Clear();
            _rollingVolume = 0;
            if (_rpmHistory != null)
                _rpmHistory.Clear();
            base.OnClear();
        }

        private void OnNewTrade(Symbol symbol, Last tick)
        {
            DateTime now = Core.TimeUtils.DateTimeUtcNow;

            // Add new trade to the queue
            _recentTrades.Enqueue(new TradeData { Time = now, Volume = tick.Size });
            _rollingVolume += tick.Size;

            // Clean up old trades outside the window
            CleanOldTrades(now);

            // Calculate current RPM (Volume per Second)
            // If the market just opened, we divide by the time elapsed so far, up to the max window
            double oldestTradeElapsed = 0.001; 
            if (_recentTrades.Count > 0)
            {
                oldestTradeElapsed = (now - _recentTrades.Peek().Time).TotalSeconds;
            }

            // Normalization: Ensure we don't divide by near-zero, and cap the divisor at WindowSeconds
            double divisor = Math.Min(WindowSeconds, Math.Max(0.5, oldestTradeElapsed));
            _currentRpm = _rollingVolume / divisor;
        }

        private void CleanOldTrades(DateTime now)
        {
            while (_recentTrades.Count > 0)
            {
                if ((now - _recentTrades.Peek().Time).TotalSeconds > WindowSeconds)
                {
                    var oldTrade = _recentTrades.Dequeue();
                    _rollingVolume -= oldTrade.Volume;
                }
                else
                {
                    break;
                }
            }
            
            // Failsafe for floating point drift
            if (_rollingVolume < 0 || _recentTrades.Count == 0) _rollingVolume = 0;
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (Count < 1)
                return;

            int currentIndex = Count - 1;
            double currentVal = 0;

            if (args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.NewTick)
            {
                // In real-time, just push the live calculated RPM to the chart
                // Make sure we keep the queue clean even if no trades are happening (idle drain)
                CleanOldTrades(Core.TimeUtils.DateTimeUtcNow);
                
                if (_recentTrades.Count == 0) _currentRpm = 0;

                currentVal = _currentRpm;
            }
            else
            {
                // Historical Fallback
                // Since we don't have sub-millisecond historical tick data enqueued,
                // we map the standard Candle Volume / Candle Duration as a rough visual approximation.
                double volume = Volume();
                double elapsedSeconds = (Time(0).Add(Time(0) - Time(1)) - Time(0)).TotalSeconds;
                
                if (elapsedSeconds <= 0) elapsedSeconds = 1;
                
                currentVal = volume / elapsedSeconds;

                // Spikes of 15-20+ million volume during split-second gaps on market open
                // completely break the auto-scaling Y-axis of the indicator in Quantower.
                // We cap the historical approximation at a sensible maximum threshold.
                if (currentVal > HistoricalSpikeCap) 
                {
                    currentVal = HistoricalSpikeCap;
                }
            }

            SetValue(currentVal, 0);

            // Store for rolling average calculations
            if (_rpmHistory.Count <= currentIndex)
                _rpmHistory.Add(currentVal);
            else
                _rpmHistory[currentIndex] = currentVal;

            // Calculate moving average manually based on our history
            double smaValue = currentVal; // Fallback to current if not enough history
            
            // To ensure the SMA line renders immediately on the chart instead of 
            // waiting for 'SmoothingPeriods' to pass, we use a dynamic lookback
            // that uses whatever data is available until the period fills up.
            int lookback = Math.Min(Count, SmoothingPeriods);
            if (lookback > 0)
            {
                double sumRpm = 0;
                for (int i = 0; i < lookback; i++)
                {
                    if (currentIndex - i >= 0 && currentIndex - i < _rpmHistory.Count)
                    {
                        sumRpm += _rpmHistory[currentIndex - i];
                    }
                }
                smaValue = sumRpm / lookback;
            }

            SetValue(smaValue, 1); // Set SMA value to Series 1
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);
            
            if (!ShowRevMeter || CurrentChart == null) return;

            Graphics gr = args.Graphics;
            var mainWindow = CurrentChart.MainWindow;
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var state = gr.Save();
            try
            {
                gr.SetClip(mainWindow.ClientRectangle);

                // Calculate positions
                int rightEdge = mainWindow.ClientRectangle.Right;
                int bottomEdge = mainWindow.ClientRectangle.Bottom;

                int x = rightEdge - RevMeterXOffset - RevMeterWidth;
                int yBot = bottomEdge - RevMeterYOffset;
                int yTop = yBot - RevMeterHeight;

                // Draw Container
                using (Pen borderPen = new Pen(Color.Gray, 1))
                {
                    gr.DrawRectangle(borderPen, x, yTop, RevMeterWidth, RevMeterHeight);
                }

                // Dynamic Max RPM Handling
                double currentThreshold = MaxRpmThreshold;
                if (DynamicAutoScale && Count > 0)
                {
                    double currentSma = GetValue(1); // Get the smoothed moving average
                    if (!double.IsNaN(currentSma) && currentSma > 0)
                    {
                        // Set the redline to X times the recent average (e.g., 2.0x average volume = redline)
                        currentThreshold = Math.Max(10.0, currentSma * AutoScaleMultiplier);
                    }
                }

                // Calculate Fill
                double fillPercent = Math.Max(0, Math.Min(1.0, _currentRpm / currentThreshold));
                int fillHeight = (int)(RevMeterHeight * fillPercent);
                int yFillTop = yBot - fillHeight;

                // Determine Color
                Color fillColor = IdleColor;
                if (fillPercent > 0.85) fillColor = RedlineColor;
                else if (fillPercent > 0.50) fillColor = MidRevColor;

                // Draw Fill Bar
                if (fillHeight > 0)
                {
                    using (SolidBrush fillBrush = new SolidBrush(fillColor))
                    {
                        gr.FillRectangle(fillBrush, x + 1, yFillTop, RevMeterWidth - 1, fillHeight);
                    }
                }

                // Draw Text
                string rpmText = $"{_currentRpm:F0} RPM";
                using (Font font = new Font("Arial", 10, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    SizeF textSize = gr.MeasureString(rpmText, font);
                    // Center text above the gauge
                    float textX = x + (RevMeterWidth / 2f) - (textSize.Width / 2f);
                    float textY = yTop - textSize.Height - 5;
                    gr.DrawString(rpmText, font, textBrush, textX, textY);
                }
            }
            finally
            {
                gr.Restore(state);
            }
        }
    }
}
