using System;
using System.Diagnostics;
using NinjaTrader.Cbi;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Explicitly added test indicator; no production consumer can discover this registry.
    public class OrcaProviderProbe : Indicator
    {
        private readonly object probeSync = new object();
        private readonly string diagnosticsId = Guid.NewGuid().ToString("N");
        private OrcaProviderStreamRegistry registry;
        private OrcaProviderPublisherLease publisher;
        private OrcaProviderStreamReader reader;
        private OrcaProviderIngestion ingestion;
        private TimeZoneInfo eventTimeZone;
        private bool historicalReplay;
        private bool terminated;
        private bool faulted;
        private bool resetPending = true;
        private int lastSessionResetBar = -1;
        private long historicalCount;
        private long liveCount;
        private long loadStart;
        private long nextReport;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "OrcaProviderProbe";
                Description = "Experimental bounded exact-tick producer probe. Does not feed existing indicators.";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                IsAutoScale = false;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = false;
                BarsRequiredToPlot = 0;
                return;
            }
            lock (probeSync)
            {
                if (State == State.Terminated)
                {
                    terminated = true;
                    if (reader != null) reader.Dispose();
                    if (publisher != null) publisher.Dispose();
                    if (registry != null) registry.Dispose();
                    reader = null; publisher = null; registry = null; ingestion = null;
                    OrcaDiagnosticsCore.UnregisterInstance(diagnosticsId);
                    return;
                }
                if (terminated) return;
                try
                {
                    if (State == State.DataLoaded)
                    {
                        historicalReplay = Bars != null && Bars.IsTickReplay;
                        eventTimeZone = NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo;
                        if (eventTimeZone == null || Instrument == null || Bars == null || Bars.TradingHours == null)
                            throw new InvalidOperationException("Platform instrument/session/timezone metadata unavailable.");
                        loadStart = Stopwatch.GetTimestamp();
                        registry = new OrcaProviderStreamRegistry(1, 100000, 1, 1, 1);
                        // Deliberately unique probe environment: no inferred sharing across charts/connections.
                        var key = new OrcaProviderStreamKey(Instrument.FullName, "probe:" + diagnosticsId,
                            Bars.TradingHours.Name + ":reset-per-session", "UTC", "bidask-fallback:v1");
                        if (registry.TryAcquire(key, out publisher) != OrcaProviderAcquireStatus.Acquired)
                            throw new InvalidOperationException("Probe publisher acquisition failed.");
                        ingestion = new OrcaProviderIngestion(publisher,
                            OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection, historicalReplay);
                        if (!registry.TryOpenReader(key, out reader)) throw new InvalidOperationException("Probe reader acquisition failed.");
                        OrcaDiagnosticsCore.RegisterInstance(diagnosticsId, "OrcaProviderProbe", this);
                        OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsId, 0, "PrimaryChartSeries", "Chart", "Probe adds no secondary series");
                        Print("OrcaProviderProbe " + diagnosticsId + ": started; TickReplay=" + historicalReplay
                            + "; capacity=100000 events; platform timezone=" + eventTimeZone.Id + "; no consumers attached");
                    }
                    else if (State == State.Realtime && ingestion != null && !faulted)
                    {
                        if (historicalReplay) ingestion.CompleteHistoricalPassAndBeginLive();
                    }
                    if (reader != null && !faulted)
                    {
                        OrcaDiagnosticsCore.ReportState(diagnosticsId, State.ToString());
                        if (State == State.Realtime) ReportStatus(true);
                    }
                }
                catch (Exception ex) { Fault(ex.Message, OrcaStreamFault.IngestionFailed); }
            }
        }

        protected override void OnBarUpdate()
        {
            lock (probeSync)
            {
                if (terminated || faulted || ingestion == null || BarsInProgress != 0) return;
                // Native session flag, not a hard-coded exchange-clock cutoff.
                if (Bars.IsFirstBarOfSession && CurrentBar != lastSessionResetBar)
                {
                    resetPending = true;
                    lastSessionResetBar = CurrentBar;
                }
            }
        }

        protected override void OnMarketData(MarketDataEventArgs e)
        {
            if (e == null || e.MarketDataType != MarketDataType.Last) return;
            State callbackState = State;
            lock (probeSync)
            {
                if (terminated || faulted || ingestion == null) return;
                bool historical = callbackState == State.Historical;
                if ((!historical || !historicalReplay) && callbackState != State.Realtime) return;
                long workStart = 0;
                try
                {
                    if (OrcaDiagnosticsCore.IsEnabled)
                        workStart = OrcaDiagnosticsCore.BeginWorkSample(OrcaDiagnosticsCore.ReportMarketData(diagnosticsId, e.MarketDataType, e.Time));
                    DateTime utc = ToEventUtc(e.Time);
                    ingestion.OnTrade(historical, utc, e.Price, e.Volume, e.Bid, e.Ask, true, resetPending);
                    resetPending = false;
                    if (historical) historicalCount++; else liveCount++;
                }
                catch (Exception ex) { Fault(ex.Message, OrcaStreamFault.InvalidEvent); }
                finally
                {
                    if (workStart != 0) OrcaDiagnosticsCore.ReportWorkSample(diagnosticsId, OrcaDiagnosticsWorkKind.MarketData, -1, workStart);
                }
                if (!faulted) ReportStatus(false);
            }
        }

        protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs e)
        {
            if (e == null || (e.PriceStatus != ConnectionStatus.ConnectionLost && e.PriceStatus != ConnectionStatus.Disconnected)) return;
            lock (probeSync)
            {
                // Conservative probe policy: no guessed connection ownership or automatic recovery.
                if (!terminated && ingestion != null && State == State.Realtime)
                    Fault("Price connection changed; reload probe before resuming (any connection).", OrcaStreamFault.SourceDisconnected);
            }
        }

        private DateTime ToEventUtc(DateTime time)
        {
            if (time.Kind == DateTimeKind.Utc) return time;
            if (time.Kind == DateTimeKind.Local) return time.ToUniversalTime();
            if (eventTimeZone.IsInvalidTime(time) || eventTimeZone.IsAmbiguousTime(time))
                throw new InvalidOperationException("Ambiguous/invalid platform event time; refusing to guess UTC offset.");
            return TimeZoneInfo.ConvertTimeToUtc(time, eventTimeZone);
        }

        private void ReportStatus(bool print)
        {
            if (reader == null || terminated || (!print && !OrcaDiagnosticsCore.IsEnabled)) return;
            long now = Stopwatch.GetTimestamp();
            if (!print && now < nextReport) return;
            nextReport = now + Stopwatch.Frequency;
            using (var batch = reader.Read(reader.FirstAvailable, 1))
            {
                string status = "phase=" + batch.Coverage.Phase + " historical=" + historicalCount + " live=" + liveCount
                    + " retained=" + (batch.PublishedThroughExclusive - batch.FirstAvailable.Sequence)
                    + " evicted=" + batch.FirstAvailable.Sequence + " passComplete=" + batch.Coverage.HistoricalPassCompleted
                    + " fullPassRetained=" + batch.Coverage.FullHistoricalPassRetained + " UTC-range-confirmed=false"
                    + " elapsed=" + ((now - loadStart) / (double)Stopwatch.Frequency).ToString("F1") + "s";
                OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsId, "ExperimentalPrimaryLastProbe",
                    faulted ? "Unavailable" : historicalReplay && batch.Coverage.Phase == OrcaStreamPhase.Historical ? "HistoricalReplay" : "ProbeOnly", "PrivateProbeRegistry");
                OrcaDiagnosticsCore.ReportCacheStatus(diagnosticsId, "PrivateProbeRegistry", status);
                if (print) Print("OrcaProviderProbe " + diagnosticsId + ": " + status);
            }
        }

        private void Fault(string message, OrcaStreamFault reason)
        {
            if (faulted || terminated) return;
            faulted = true;
            if (ingestion != null) ingestion.MarkFault(reason);
            Print("OrcaProviderProbe " + diagnosticsId + ": FAULT " + message);
            OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsId, "ExperimentalPrimaryLastProbe", "Unavailable", "PrivateProbeRegistry");
        }
    }
}
