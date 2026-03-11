using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;

namespace Quantower.Indicators
{
    [Obfuscation(Exclude = true, ApplyToMembers = false)]
    public class AutoLegProfile : Indicator
    {
        // --- Licensing ---
        [InputParameter("Whop License Key", 0)]
        [Obfuscation(Exclude = true)]
        public string WhopLicenseKey = "";

        private bool isLicenseValid = false;
        private string licenseStatusMessage = "Checking License...";
        private static readonly HttpClient httpClient = new HttpClient();
        private const string ProxyUrl = "https://orcaprofiles-proxy.vercel.app/api/validate-license";

        // Input Parameters
        [InputParameter("Reversal Ticks", 10, 1, 9999, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int ReversalTicks = 200;

        [InputParameter("Minimum Leg Size (Ticks)", 20, 1, 9999, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int MinimumLegTicks = 200;

        [InputParameter("Volume Tick Compression", 30, 1, 9999, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int TickCompression = 4;

        [InputParameter("Delta Tick Compression", 35, 1, 9999, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int DeltaTickCompression = 10;

        [InputParameter("Number of Legs to Display", 40, 1, 50, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int LegsToDisplay = 3;

        [InputParameter("Volume Profile Width (pixels)", 50, 10, 500, 10, 0)]
        [Obfuscation(Exclude = true)]
        public int VolumeProfileWidth = 150;

        [InputParameter("Delta Profile Width (pixels)", 51, 10, 500, 10, 0)]
        [Obfuscation(Exclude = true)]
        public int DeltaProfileWidth = 100;

        [InputParameter("Past Volume Width (pixels)", 52, 10, 500, 10, 0)]
        [Obfuscation(Exclude = true)]
        public int PastVolumeWidth = 60;

        [InputParameter("Past Delta Width (pixels)", 53, 10, 500, 10, 0)]
        [Obfuscation(Exclude = true)]
        public int PastDeltaWidth = 40;

        [InputParameter("Distance From Right (pixels)", 55, 0, 500, 10, 0)]
        [Obfuscation(Exclude = true)]
        public int RightOffset = 60;

        [InputParameter("Profile Separation (pixels)", 56, 0, 200, 10, 0)]
        [Obfuscation(Exclude = true)]
        public int ProfileSeparation = 60;

        [InputParameter("Merge Overlap (%)", 57, 0, 100, 5, 0)]
        [Obfuscation(Exclude = true)]
        public int MergeOverlapPercent = 80;

        [InputParameter("Profile Bar Spacing (px)", 58, 0, 5, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int ProfileBarSpacing = 0;

        [InputParameter("Minimum Bars per Leg", 58, 1, 500, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int MinimumBarsPerLeg = 10;

        [InputParameter("Minimum Duration (Min)", 59, 0, 1440, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int MinimumDurationMinutes = 0;

        [InputParameter("Mirror Past Profiles", 60)]
        [Obfuscation(Exclude = true)]
        public bool MirrorPastProfiles = true;

        [InputParameter("Show Volume", 60)]
        [Obfuscation(Exclude = true)]
        public bool ShowVolume = true;

        [InputParameter("Show Delta", 70)]
        [Obfuscation(Exclude = true)]
        public bool ShowDelta = true;

        [InputParameter("Value Area (%)", 75, 1, 100, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int ValueAreaPercent = 70;

        [InputParameter("Show Current Leg Outline", 76)]
        [Obfuscation(Exclude = true)]
        public bool ShowCurrentLegBox = false;

        [InputParameter("Calculate VWAP", 77)]
        [Obfuscation(Exclude = true)]
        public bool ShowVWAP = true;
        
        [InputParameter("Show Delta Labels", 78)]
        [Obfuscation(Exclude = true)]
        public bool ShowDeltaLabels = true;

        [InputParameter("Delta Label Min Height", 79, 5, 50, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int DeltaLabelMinHeight = 12;

        [InputParameter("Hide Profiles During Load", 79)]
        [Obfuscation(Exclude = true)]
        public bool HideDuringLoad = true;

        [InputParameter("Delta Label Font Size", 80, 5, 24, 1, 0)]
        [Obfuscation(Exclude = true)]
        public int DeltaLabelFontSize = 8;

        [InputParameter("Delta Label Color", 81)]
        [Obfuscation(Exclude = true)]
        public Color DeltaLabelColor = Color.White;

        [InputParameter("Show Label Background", 82)]
        [Obfuscation(Exclude = true)]
        public bool ShowDeltaLabelBackground = true;

        [InputParameter("Label Background Color", 83)]
        [Obfuscation(Exclude = true)]
        public Color DeltaLabelBackgroundColor = Color.FromArgb(180, 0, 0, 0);

        // Profile colors
        [InputParameter("Positive Delta Color", 80)]
        [Obfuscation(Exclude = true)]
        public Color PositiveDeltaColor = Color.FromArgb(200, 0, 255, 0);

        [InputParameter("Negative Delta Color", 90)]
        [Obfuscation(Exclude = true)]
        public Color NegativeDeltaColor = Color.FromArgb(200, 255, 0, 0);

        [InputParameter("Volume Color", 100)]
        [Obfuscation(Exclude = true)]
        public Color VolumeColor = Color.FromArgb(150, 128, 128, 255);

        [InputParameter("Value Area Color", 105)]
        [Obfuscation(Exclude = true)]
        public Color ValueAreaColor = Color.FromArgb(200, 128, 128, 128);

        [InputParameter("VWAP Color", 110)]
        [Obfuscation(Exclude = true)]
        public Color VWAPColor = Color.Magenta;

        // --- Data Structures ---
        private class PriceLeg
        {
            public int StartIndex;
            public int EndIndex;
            public DateTime StartTime;
            public DateTime EndTime;
            public double StartPrice;
            public double EndPrice;
            public double HighPrice;
            public double LowPrice;
            public bool IsUpLeg;
            
            public Dictionary<double, LevelData> VolumeProfileData;
            public Dictionary<double, LevelData> DeltaProfileData;
            public List<Tuple<DateTime, double>> VwapPoints;
            public DateTime LastVwapBarTime;

            public double TotalVolume;
            public double TotalPV;
            public double CurrentVwapValue;    
            
            public double LegTotalVolume; 
            public double MaxVolume;
            public double MaxDeltaAbs;
            public bool IsRefined; // Flag to indicate if ticks have been processed

            public PriceLeg()
            {
                VolumeProfileData = new Dictionary<double, LevelData>();
                DeltaProfileData = new Dictionary<double, LevelData>();
                VwapPoints = new List<Tuple<DateTime, double>>();
                LastVwapBarTime = DateTime.MinValue;
                CurrentVwapValue = 0;
                IsRefined = false;
            }

            public double LegSizeInTicks(double tickSize)
            {
                return Math.Abs(HighPrice - LowPrice) / tickSize;
            }
            
             public void ResetData()
            {
                VolumeProfileData.Clear();
                DeltaProfileData.Clear();
                VwapPoints.Clear();
                LastVwapBarTime = DateTime.MinValue;
                CurrentVwapValue = 0;
                TotalVolume = 0;
                TotalPV = 0;
                LegTotalVolume = 0;
                MaxVolume = 0;
                MaxDeltaAbs = 0;
            }
        }

        private class LevelData
        {
            public double Volume;
            public double BuyVolume;
            public double SellVolume;
            public double Delta { get { return BuyVolume - SellVolume; } }
        }

        // --- Fields ---
        private List<PriceLeg> completedLegs;
        private PriceLeg currentLeg;
        
        private double currentExtremePrice;
        private int currentExtremeIndex; 
        private DateTime currentExtremeTime;

        private bool isUpLeg;
        private double tickSize;
        private bool isHistoricalLoaded;
        private readonly object syncLock = new object();

        public AutoLegProfile()
        {
            Name = "Auto Leg Profile";
            Description = "Automatically creates volume/delta profiles for each price leg";
            
            completedLegs = new List<PriceLeg>();
        }

        protected override void OnInit()
        {
            tickSize = Symbol.TickSize;
            currentLeg = null;
            currentExtremePrice = 0;
            currentExtremeIndex = 0;
            completedLegs = new List<PriceLeg>();
            isHistoricalLoaded = false;
            
            // Asynchronously validate license
            Task.Run(async () => await ValidateWhopLicense());

            // Subscribe to real-time trades for accurate delta
            if (Symbol != null)
            {
                Symbol.NewLast += OnNewLast;
            }
        }

        private async Task ValidateWhopLicense()
        {
            if (string.IsNullOrWhiteSpace(WhopLicenseKey))
            {
                isLicenseValid = false;
                licenseStatusMessage = "LICENSE KEY REQUIRED";
                return;
            }

            try
            {
                var payload = new { licenseKey = WhopLicenseKey };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(ProxyUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(resultJson))
                    {
                        if (doc.RootElement.TryGetProperty("valid", out JsonElement validElement))
                        {
                            isLicenseValid = validElement.GetBoolean();
                            licenseStatusMessage = isLicenseValid ? "LICENSE VALID" : "INVALID LICENSE KEY";
                        }
                    }
                }
                else
                {
                    licenseStatusMessage = "LICENSE SERVER ERROR";
                }
            }
            catch (Exception ex)
            {
                licenseStatusMessage = "CHECKING FAILED: " + ex.Message;
            }
        }

        private void OnNewLast(Symbol symbol, Last last)
        {
            if (!isLicenseValid) return;

            // Only process if we have a current leg and are in real-time mode
            if (currentLeg == null || !isHistoricalLoaded)
                return;
                
            // Accumulate this trade to the current leg for accurate delta
            AccumulateTickToLeg(currentLeg, last.Price, last.Size, last.AggressorFlag, last.Time);
            
            // Update max stats after each trade
            UpdateLegStats(currentLeg);

            // Force chart to repaint so the profile updates in real time
            this.CurrentChart?.InvalidateIndicator(this);
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (!isLicenseValid) return;

            if (Count < 2)
                return;

            DateTime time = Time();
            double high = High();
            double low = Low();
            double close = Close();
            double volume = Volume();
            double open = Open();

            // Handle transition to Realtime
            if (!isHistoricalLoaded && args.Reason == UpdateReason.NewTick)
            {
                isHistoricalLoaded = true;
                // The current leg accumulated so far is purely estimated.
                // We should refine it NOW.
                if (currentLeg != null)
                {
                    RefineLegProfile(currentLeg, currentLeg.StartTime, time);
                }
            }
            
            // NOTE: For Realtime updates, we could hook OnNewTrade, but for simplicity and consistency
            // we will continue to "Estimate" the live bar in this loop.
            // The "Refine" step happens on Reversal (Correction) or on Historical Load.
            // This prevents "smeared" history, but keeps live updates fast.

            // Initialize first leg
            if (currentLeg == null)
            {
                InitializeFirstLeg(time, close, high, low);
                return;
            }

            // Update current extreme
            bool newExtremeFound = false;
            
            if (isUpLeg)
            {
                if (high >= currentExtremePrice)
                {
                    currentExtremePrice = high;
                    currentExtremeIndex = Count - 1;
                    currentExtremeTime = time;
                    newExtremeFound = true;
                }
            }
            else
            {
                if (low <= currentExtremePrice)
                {
                    currentExtremePrice = low;
                    currentExtremeIndex = Count - 1;
                    currentExtremeTime = time;
                    newExtremeFound = true;
                }
            }

            // Reversal Check
            bool reversalDetected = false;
            double reversalThreshold = Math.Max(tickSize, ReversalTicks * tickSize);

            if (!newExtremeFound)
            {
                // Number of bars inclusive in the current leg
                int barsInLeg = Count - currentLeg.StartIndex;
                bool constraintsMet = barsInLeg >= MinimumBarsPerLeg;

                if (MinimumDurationMinutes > 0)
                    constraintsMet = constraintsMet && (time - currentLeg.StartTime).TotalMinutes >= MinimumDurationMinutes;

                if (constraintsMet)
                {
                    if (isUpLeg && (currentExtremePrice - low) >= reversalThreshold)
                    {
                        reversalDetected = true;
                        HandleReversal(false, low, high, time); // Switch to Down
                    }
                    else if (!isUpLeg && (high - currentExtremePrice) >= reversalThreshold)
                    {
                        reversalDetected = true;
                        HandleReversal(true, high, low, time); // Switch to Up
                    }
                }
            }

            // Accumulate Bar data (Estimated for Historical ONLY)
            // OnNewLast handles accurate real-time delta
            if (!reversalDetected)
            {
                // Only use bar estimation for historical data or until first real-time tick
                if (!isHistoricalLoaded)
                {
                    AccumulateBarToLeg(currentLeg, high, low, close, volume, open, time);
                }
                
                currentLeg.HighPrice = Math.Max(currentLeg.HighPrice, high);
                currentLeg.LowPrice = Math.Min(currentLeg.LowPrice, low);
                currentLeg.EndIndex = Count - 1;
                currentLeg.EndTime = time;
                currentLeg.EndPrice = close;
                
                // Add VWAP point using bar time (this aligns with chart)
                if (ShowVWAP && currentLeg.CurrentVwapValue > 0)
                {
                    if (time > currentLeg.LastVwapBarTime)
                    {
                        currentLeg.VwapPoints.Add(new Tuple<DateTime, double>(time, currentLeg.CurrentVwapValue));
                        currentLeg.LastVwapBarTime = time;
                    }
                    else if (currentLeg.VwapPoints.Count > 0)
                    {
                        // Update last point with current value
                        currentLeg.VwapPoints[currentLeg.VwapPoints.Count - 1] = new Tuple<DateTime, double>(time, currentLeg.CurrentVwapValue);
                    }
                }
            }
        }

        private void InitializeFirstLeg(DateTime time, double price, double high, double low)
        {
            isUpLeg = true;
            currentExtremePrice = high;
            currentExtremeIndex = Count - 1;
            currentExtremeTime = time;
            
            currentLeg = new PriceLeg
            {
                StartIndex = Count - 1,
                StartTime = time,
                StartPrice = price,
                HighPrice = high,
                LowPrice = low,
                IsUpLeg = true,
                EndPrice = price,
                EndTime = time,
                EndIndex = Count - 1
            };
            
            AccumulateBarToLeg(currentLeg, high, low, price, Volume(), Open(), time);
        }

        private void HandleReversal(bool toUpLeg, double high, double low, DateTime currentTime)
        {
            // 1. REBUILD THE OLD LEG WITH TICKS (Accurate)
            // The old leg ends at 'currentExtremeTime'.
            PriceLeg oldLeg = currentLeg;
            
            // We recreate it structurally
            PriceLeg refinedOldLeg = new PriceLeg
            {
                StartIndex = oldLeg.StartIndex,
                StartTime = oldLeg.StartTime,
                EndIndex = currentExtremeIndex,
                EndTime = currentExtremeTime,
                IsUpLeg = oldLeg.IsUpLeg,
                 // We rely on Refine to set Prices/Volume
                 HighPrice = oldLeg.HighPrice, // Will be updated by Refine
                 LowPrice = oldLeg.LowPrice
            };
            
            // Fetch Ticks and Build Profile
            RefineLegProfile(refinedOldLeg, oldLeg.StartTime, currentExtremeTime);
            
            // Store or Merge
            if (refinedOldLeg.LegSizeInTicks(tickSize) >= MinimumLegTicks)
            {
                bool merged = false;
                if (completedLegs.Count > 0 && MergeOverlapPercent > 0)
                {
                    PriceLeg lastLeg = completedLegs.Last();
                    
                    // Calculate Overlap
                    double overlapLow = Math.Max(lastLeg.LowPrice, refinedOldLeg.LowPrice);
                    double overlapHigh = Math.Min(lastLeg.HighPrice, refinedOldLeg.HighPrice);
                    double overlapHeight = Math.Max(0, overlapHigh - overlapLow);
                    
                    double lastHeight = lastLeg.HighPrice - lastLeg.LowPrice;
                    double currentHeight = refinedOldLeg.HighPrice - refinedOldLeg.LowPrice;
                    double minHeight = Math.Min(lastHeight, currentHeight);
                    
                    if (minHeight > 0)
                    {
                        double overlapPct = (overlapHeight / minHeight) * 100.0;
                        if (overlapPct >= MergeOverlapPercent)
                        {
                            MergeLegs(lastLeg, refinedOldLeg);
                            merged = true;
                        }
                    }
                }

                if (!merged)
                {
                    lock (syncLock)
                    {
                        completedLegs.Add(refinedOldLeg);
                        while (completedLegs.Count > LegsToDisplay)
                            completedLegs.RemoveAt(0);
                    }
                }
            }
            
            // 2. START NEW LEG (Accurate Start)
            // Starts from ExtremeTime to Now
            isUpLeg = toUpLeg;
            
            currentLeg = new PriceLeg
            {
                StartIndex = currentExtremeIndex,
                StartTime = currentExtremeTime, 
                // Note: StartIndex is approximate if we use Ticks. 
                // But generally correct for drawing anchors.
                EndIndex = Count - 1,
                EndTime = currentTime,
                IsUpLeg = toUpLeg,
                HighPrice = toUpLeg ? high : currentExtremePrice, // Init
                LowPrice = toUpLeg ? currentExtremePrice : low
            };
            
            // Reset Extreme Tracker
            currentExtremePrice = toUpLeg ? high : low;
            currentExtremeIndex = Count - 1;
            currentExtremeTime = currentTime;

            // Refine the "Stub" of the new leg (FROM Extreme TO Now)
            // This ensures the new leg starts with clean tick data too
            RefineLegProfile(currentLeg, currentLeg.StartTime, currentTime);
        }

        private void MergeLegs(PriceLeg destination, PriceLeg source)
        {
            // Update Boundaries
            destination.HighPrice = Math.Max(destination.HighPrice, source.HighPrice);
            destination.LowPrice = Math.Min(destination.LowPrice, source.LowPrice);
            destination.EndTime = source.EndTime;
            destination.EndIndex = source.EndIndex;
            destination.EndPrice = source.EndPrice;

            // Merge Volume Profile Data
            foreach (var kvp in source.VolumeProfileData)
            {
                if (!destination.VolumeProfileData.ContainsKey(kvp.Key))
                    destination.VolumeProfileData[kvp.Key] = new LevelData();
                
                destination.VolumeProfileData[kvp.Key].Volume += kvp.Value.Volume;
                destination.VolumeProfileData[kvp.Key].BuyVolume += kvp.Value.BuyVolume;
                destination.VolumeProfileData[kvp.Key].SellVolume += kvp.Value.SellVolume;
            }

            // Merge Delta Profile Data
            foreach (var kvp in source.DeltaProfileData)
            {
                if (!destination.DeltaProfileData.ContainsKey(kvp.Key))
                    destination.DeltaProfileData[kvp.Key] = new LevelData();
                
                destination.DeltaProfileData[kvp.Key].Volume += kvp.Value.Volume; // Though Volume is unused in Delta profile drawing
                destination.DeltaProfileData[kvp.Key].BuyVolume += kvp.Value.BuyVolume;
                destination.DeltaProfileData[kvp.Key].SellVolume += kvp.Value.SellVolume;
            }

            // Merge VWAP data (approximate for points)
            destination.VwapPoints.AddRange(source.VwapPoints);
            destination.TotalVolume += source.TotalVolume;
            destination.TotalPV += source.TotalPV;
            destination.CurrentVwapValue = source.CurrentVwapValue;
            destination.LastVwapBarTime = source.LastVwapBarTime;

            // Recalculate Stats
            UpdateLegStats(destination);
        }

        private void RefineLegProfile(PriceLeg leg, DateTime fromTime, DateTime toTime)
        {
            // Use HistoryAggregationTick for trade data
            var aggregation = new HistoryAggregationTick(HistoryType.Last);
            
            var history = this.Symbol.GetHistory(new HistoryRequestParameters 
            { 
               Symbol = this.Symbol,
               FromTime = fromTime,
               ToTime = toTime, 
               Aggregation = aggregation
            });

            if (history != null && history.Count > 0)
            {
                leg.ResetData();
                leg.HighPrice = -double.MaxValue;
                leg.LowPrice = double.MaxValue;

                for (int i = 0; i < history.Count; i++)
                {
                     // Use HistoryItemLast for Trades
                     if (history[i] is HistoryItemLast trade)
                     {
                         // trade[PriceType.Last] is usually the trade price
                         // trade[PriceType.Volume] for volume
                         AccumulateTickToLeg(leg, trade[PriceType.Last], trade[PriceType.Volume], trade.AggressorFlag, trade.TimeLeft);
                     }
                     else if (history[i] is HistoryItemTick tick)
                     {
                         // Fallback
                         AccumulateTickToLeg(leg, tick[PriceType.Last], tick[PriceType.Volume], AggressorFlag.None, tick.TimeLeft);
                     }
                     else 
                     {
                         // Generic fallback
                         AccumulateTickToLeg(leg, history[i][PriceType.Last], history[i][PriceType.Volume], AggressorFlag.None, history[i].TimeLeft);
                     }
                }
                leg.IsRefined = true;
            }
            
            // IMPORTANT: Update stats after accumulating all data
            UpdateLegStats(leg);
        }

        private void AccumulateTickToLeg(PriceLeg leg, double price, double volume, AggressorFlag aggressor, DateTime time)
        {
            // Update Stats
            leg.HighPrice = Math.Max(leg.HighPrice, price);
            leg.LowPrice = Math.Min(leg.LowPrice, price);
            
            // Profile
            double compressionSize = TickCompression * tickSize;
            double roundedPrice = Math.Floor(price / compressionSize + 0.000001) * compressionSize;

            if (!leg.VolumeProfileData.ContainsKey(roundedPrice))
                leg.VolumeProfileData[roundedPrice] = new LevelData();

            leg.VolumeProfileData[roundedPrice].Volume += volume;

            // Delta - use same compression as volume so both profiles align at matching price rows
            double deltaCompressionSize = TickCompression * tickSize;
            double deltaPrice = Math.Floor(price / deltaCompressionSize + 0.000001) * deltaCompressionSize;
            
            if (!leg.DeltaProfileData.ContainsKey(deltaPrice))
                leg.DeltaProfileData[deltaPrice] = new LevelData();
            
            // Logic for Delta
            if (aggressor == AggressorFlag.Buy)
            {
                leg.VolumeProfileData[roundedPrice].BuyVolume += volume;
                leg.DeltaProfileData[deltaPrice].BuyVolume += volume;
            }
            else if (aggressor == AggressorFlag.Sell)
            {
                 leg.VolumeProfileData[roundedPrice].SellVolume += volume;
                 leg.DeltaProfileData[deltaPrice].SellVolume += volume;
            }
            else
            {
                // Unknown aggression.
                // Could use UpTick/DownTick logic if previous price stored?
                // For now, neutral (split).
                 leg.VolumeProfileData[roundedPrice].BuyVolume += volume * 0.5;
                 leg.VolumeProfileData[roundedPrice].SellVolume += volume * 0.5;
                 leg.DeltaProfileData[deltaPrice].BuyVolume += volume * 0.5;
                 leg.DeltaProfileData[deltaPrice].SellVolume += volume * 0.5;
            }

            // VWAP - just update running totals, actual point added in OnUpdate
            if (ShowVWAP)
            {
                leg.TotalVolume += volume;
                leg.TotalPV += (price * volume);
                if (leg.TotalVolume > 0)
                {
                    leg.CurrentVwapValue = leg.TotalPV / leg.TotalVolume;
                }
            }
        }

        private void AccumulateBarToLeg(PriceLeg leg, double high, double low, double close, double volume, double open, DateTime time)
        {
            // Simple Estimation Logic (Original)
            // Used ONLY for loop estimation between refinements
            AccumulateProfile(leg.VolumeProfileData, TickCompression, high, low, close, volume);
            AccumulateProfile(leg.DeltaProfileData, TickCompression, high, low, close, volume);

            // Update Max Stats
            UpdateLegStats(leg);

            // VWAP Accumulation (Estimated) - just update running value
            if (ShowVWAP)
            {
                double typicalPrice = (high + low + close) / 3.0;
                double pv = typicalPrice * volume;
                leg.TotalVolume += volume;
                leg.TotalPV += pv;
                if (leg.TotalVolume > 0)
                {
                    leg.CurrentVwapValue = leg.TotalPV / leg.TotalVolume;
                }
            }
        }
        
        private void UpdateLegStats(PriceLeg leg)
        {
            leg.LegTotalVolume = 0;
            leg.MaxVolume = 0;
            leg.MaxDeltaAbs = 0;

            foreach (var kvp in leg.VolumeProfileData)
            {
                leg.LegTotalVolume += kvp.Value.Volume;
                if (kvp.Value.Volume > leg.MaxVolume)
                    leg.MaxVolume = kvp.Value.Volume;
            }

            foreach (var kvp in leg.DeltaProfileData)
            {
                double absDelta = Math.Abs(kvp.Value.Delta);
                if (absDelta > leg.MaxDeltaAbs)
                    leg.MaxDeltaAbs = absDelta;
            }
        }

        private void AccumulateProfile(Dictionary<double, LevelData> profile, int compression, double high, double low, double close, double volume)
        {
            double compressionSize = compression * tickSize;
            double rangeStart = Math.Floor(low / compressionSize) * compressionSize;
            double rangeEnd = Math.Ceiling(high / compressionSize) * compressionSize;

            for (double price = rangeStart; price <= rangeEnd; price += compressionSize)
            {
                double roundedPrice = Math.Floor(price / compressionSize + 0.000001) * compressionSize;

                if (!profile.ContainsKey(roundedPrice))
                {
                    profile[roundedPrice] = new LevelData();
                }

                double levelVolume = volume / ((rangeEnd - rangeStart) / compressionSize + 1);
                profile[roundedPrice].Volume += levelVolume;

                double barRange = high - low;
                if (barRange > 0)
                {
                    double closePosition = (close - low) / barRange;
                    profile[roundedPrice].BuyVolume += levelVolume * closePosition;
                    profile[roundedPrice].SellVolume += levelVolume * (1 - closePosition);
                }
                else
                {
                    profile[roundedPrice].BuyVolume += levelVolume * 0.5;
                    profile[roundedPrice].SellVolume += levelVolume * 0.5;
                }
            }
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (!isLicenseValid)
            {
                using (Font font = new Font("Arial", 20, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Color.Red))
                {
                    args.Graphics.DrawString(licenseStatusMessage, font, brush, 50, 50);
                }
                return;
            }

            if (HideDuringLoad && !isHistoricalLoaded)
                return;

            if (completedLegs.Count == 0 && currentLeg == null)
                return;

            Graphics gr = args.Graphics;
            var mainWindow = CurrentChart.MainWindow;
            
            // Save state and set clipping to prevent bleed into other panes
            var state = gr.Save();
            try
            {
                gr.SetClip(mainWindow.ClientRectangle);

                lock (syncLock)
                {
                    // Draw completed legs
                    foreach (var leg in completedLegs)
                    {
                        DrawLegProfile(gr, mainWindow, leg, false);
                        DrawVWAP(gr, mainWindow, leg);
                    }
                }

                // Draw current leg
                if (currentLeg != null)
                {
                    DrawLegProfile(gr, mainWindow, currentLeg, true);
                    DrawVWAP(gr, mainWindow, currentLeg);
                }
            }
            finally
            {
                gr.Restore(state);
            }
        }

        private void DrawVWAP(Graphics gr, IChartWindow mainWindow, PriceLeg leg)
        {
            if (!ShowVWAP || leg.VwapPoints.Count < 2) return;

            List<Point> screenPoints = new List<Point>();
            
            // Sample points to avoid drawing thousands of points
            int step = Math.Max(1, leg.VwapPoints.Count / 200);
            for (int i = 0; i < leg.VwapPoints.Count; i += step)
            {
                var point = leg.VwapPoints[i];
                int x = (int)mainWindow.CoordinatesConverter.GetChartX(point.Item1);
                int y = (int)mainWindow.CoordinatesConverter.GetChartY(point.Item2);
                screenPoints.Add(new Point(x, y));
            }
            
            // Always include the last point
            if (leg.VwapPoints.Count > 0)
            {
                var lastPoint = leg.VwapPoints[leg.VwapPoints.Count - 1];
                int lastX = (int)mainWindow.CoordinatesConverter.GetChartX(lastPoint.Item1);
                int lastY = (int)mainWindow.CoordinatesConverter.GetChartY(lastPoint.Item2);
                if (screenPoints.Count == 0 || screenPoints[screenPoints.Count - 1].X != lastX)
                    screenPoints.Add(new Point(lastX, lastY));
            }

            if (screenPoints.Count > 1)
            {
                using (Pen pen = new Pen(VWAPColor, 2))
                {
                    try { gr.DrawLines(pen, screenPoints.ToArray()); } catch {}
                }
            }
        }

        private void DrawLegProfile(Graphics gr, IChartWindow mainWindow, PriceLeg leg, bool isCurrent)
        {
            if (leg.VolumeProfileData.Count == 0 && leg.DeltaProfileData.Count == 0)
                return;

            int profileOriginX;
            int volumeOriginX, deltaOriginX;
            int volumeDir, deltaDir; // 1 for Right, -1 for Left

            int currentVolumeWidth = isCurrent ? VolumeProfileWidth : PastVolumeWidth;
            int currentDeltaWidth = isCurrent ? DeltaProfileWidth : PastDeltaWidth;

            if (isCurrent)
            {
                profileOriginX = mainWindow.ClientRectangle.Right - RightOffset;
                volumeOriginX = profileOriginX;
                deltaOriginX = profileOriginX - currentVolumeWidth - ProfileSeparation;
                volumeDir = -1;
                deltaDir = -1;
            }
            else
            {
                profileOriginX = (int)mainWindow.CoordinatesConverter.GetChartX(leg.StartTime);
                
                if (MirrorPastProfiles)
                {
                    volumeOriginX = profileOriginX;
                    deltaOriginX = profileOriginX;
                    volumeDir = 1;
                    deltaDir = -1;
                }
                else
                {
                    volumeOriginX = profileOriginX;
                    deltaOriginX = profileOriginX + currentVolumeWidth + ProfileSeparation;
                    volumeDir = 1;
                    deltaDir = 1;
                }
            }

            // 1. Draw Volume Profile
            if (ShowVolume && leg.VolumeProfileData.Count > 0)
            {
                HashSet<double> valueAreaLevels = CalculateValueArea(leg.VolumeProfileData, leg.LegTotalVolume);

                foreach (var kvp in leg.VolumeProfileData)
                {
                    double price = kvp.Key;
                    LevelData data = kvp.Value;
                    
                    int y = (int)mainWindow.CoordinatesConverter.GetChartY(price);
                    double tickHeight = Math.Abs(mainWindow.CoordinatesConverter.GetChartY(price + (TickCompression * tickSize)) - mainWindow.CoordinatesConverter.GetChartY(price));
                    int barHeight = Math.Max(1, (int)tickHeight - ProfileBarSpacing);
                   
                    if (leg.MaxVolume > 0)
                    {
                        int barWidth = (int)((data.Volume / leg.MaxVolume) * currentVolumeWidth);
                        int x = volumeDir == 1 ? volumeOriginX : volumeOriginX - barWidth;
                        Color color = valueAreaLevels.Contains(price) ? ValueAreaColor : VolumeColor;

                        using (SolidBrush brush = new SolidBrush(color))
                        {
                            gr.FillRectangle(brush, x, y, barWidth, barHeight);
                        }
                    }
                }
            }

            // 2. Draw Delta Profile
            if (ShowDelta && leg.DeltaProfileData.Count > 0 && leg.MaxDeltaAbs > 0)
            {
                foreach (var kvp in leg.DeltaProfileData)
                {
                    double price = kvp.Key;
                    LevelData data = kvp.Value;
                    double netDelta = data.Delta;

                    int y = (int)mainWindow.CoordinatesConverter.GetChartY(price);
                    double tickHeight = Math.Abs(mainWindow.CoordinatesConverter.GetChartY(price + (TickCompression * tickSize)) - mainWindow.CoordinatesConverter.GetChartY(price));
                    int barHeight = Math.Max(1, (int)tickHeight - ProfileBarSpacing);

                    int maxDeltaWidthStrulcture = currentDeltaWidth;
                    
                    // Scale Delta
                    int barWidth = (int)((Math.Abs(netDelta) / leg.MaxDeltaAbs) * maxDeltaWidthStrulcture);
                    if (Math.Abs(netDelta) > 0 && barWidth == 0) barWidth = 1;

                    int x = deltaDir == 1 ? deltaOriginX : deltaOriginX - barWidth;
                    Color deltaColor = netDelta >= 0 ? PositiveDeltaColor : NegativeDeltaColor;

                    using (SolidBrush brush = new SolidBrush(deltaColor))
                    {
                        gr.FillRectangle(brush, x, y, barWidth, barHeight);
                    }
                    
                    if (ShowDeltaLabels && Math.Abs(netDelta) > 0 && barHeight >= DeltaLabelMinHeight) 
                    {
                        string label = netDelta.ToString("+#;-#;0");
                        
                        using (Font labelFont = new Font("Arial", DeltaLabelFontSize))
                        using (SolidBrush labelBrush = new SolidBrush(DeltaLabelColor))
                        {
                            SizeF textSize = gr.MeasureString(label, labelFont);
                            int textX;
                            
                            if (isCurrent)
                            {
                                textX = deltaOriginX + 2;
                            }
                            else
                            {
                                textX = deltaDir == 1 ? (x + barWidth + 2) : (x - (int)textSize.Width - 2);
                            }
                            
                            int textY = y + (barHeight / 2) - ((int)textSize.Height / 2);

                            if (ShowDeltaLabelBackground)
                            {
                                using (SolidBrush bgBrush = new SolidBrush(DeltaLabelBackgroundColor))
                                {
                                    gr.FillRectangle(bgBrush, textX - 1, textY - 1, textSize.Width + 2, textSize.Height + 2);
                                }
                            }

                            gr.DrawString(label, labelFont, labelBrush, textX, textY);
                        }
                    }
                }
            }

            if (isCurrent && ShowCurrentLegBox)
            {
                using (Pen pen = new Pen(Color.Yellow, 2))
                {
                    int topY = (int)mainWindow.CoordinatesConverter.GetChartY(leg.HighPrice);
                    int bottomY = (int)mainWindow.CoordinatesConverter.GetChartY(leg.LowPrice);
                    int totalWidth = currentVolumeWidth + ProfileSeparation + currentDeltaWidth;
                    gr.DrawRectangle(pen, profileOriginX - totalWidth, topY, totalWidth, bottomY - topY);
                }
            }
        }

        private HashSet<double> CalculateValueArea(Dictionary<double, LevelData> profile, double totalVolume)
        {
            var valueAreaLevels = new HashSet<double>();
            if (profile.Count == 0) return valueAreaLevels;

            double pocPrice = 0;
            double maxVol = -1;
            List<double> sortedPrices = profile.Keys.OrderBy(p => p).ToList();

            int pocIndex = 0;
            for (int i = 0; i < sortedPrices.Count; i++)
            {
                double p = sortedPrices[i];
                if (profile[p].Volume > maxVol)
                {
                    maxVol = profile[p].Volume;
                    pocPrice = p;
                    pocIndex = i;
                }
            }

            double targetVolume = totalVolume * (ValueAreaPercent / 100.0);
            double currentVolume = maxVol;
            valueAreaLevels.Add(pocPrice);

            int upIndex = pocIndex + 1;
            int downIndex = pocIndex - 1;

            while (currentVolume < targetVolume && (upIndex < sortedPrices.Count || downIndex >= 0))
            {
                double upVol = (upIndex < sortedPrices.Count) ? profile[sortedPrices[upIndex]].Volume : 0;
                double downVol = (downIndex >= 0) ? profile[sortedPrices[downIndex]].Volume : 0;

                if (upVol >= downVol)
                {
                   if (upIndex < sortedPrices.Count)
                   {
                       currentVolume += upVol;
                       valueAreaLevels.Add(sortedPrices[upIndex]);
                       upIndex++;
                   }
                   else if (downIndex >= 0)
                   {
                        currentVolume += downVol;
                        valueAreaLevels.Add(sortedPrices[downIndex]);
                        downIndex--;
                   }
                }
                else
                {
                    if (downIndex >= 0)
                    {
                        currentVolume += downVol;
                        valueAreaLevels.Add(sortedPrices[downIndex]);
                        downIndex--;
                    }
                    else if (upIndex < sortedPrices.Count)
                    {
                        currentVolume += upVol;
                        valueAreaLevels.Add(sortedPrices[upIndex]);
                        upIndex++;
                    }
                }
            }

            return valueAreaLevels;
        }

        protected override void OnClear()
        {
            // Unsubscribe from trade events
            if (Symbol != null)
            {
                Symbol.NewLast -= OnNewLast;
            }
            
            completedLegs.Clear();
            currentLeg = null;
        }
    }
}
