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
        private OrcaProviderFeedLifetime feedLifetime;
        private OrcaProviderReaderComparison comparison;
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
        private int connectionReports;

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
                    // Capture the live tail before releasing readers; no timer or extra subscription.
                    if (!terminated && !faulted && reader != null)
                    {
                        try { ReportStatus(true); }
                        catch (Exception ex) { Fault(ex.Message, OrcaStreamFault.IngestionFailed); }
                    }
                    terminated = true;
                    if (reader != null) reader.Dispose();
                    if (publisher != null) publisher.Dispose();
                    if (comparison != null) comparison.Dispose();
                    if (feedLifetime != null) feedLifetime.Dispose();
                    reader = null; publisher = null; feedLifetime = null; ingestion = null; comparison = null;
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
                        var sessionCapture = OrcaProviderSessionCapture.Capture(Bars.TradingHours, eventTimeZone);
                        feedLifetime = new OrcaProviderFeedLifetime(Bars.IsInReplayMode
                            ? OrcaProviderEnvironment.MarketReplay : OrcaProviderEnvironment.LiveFeed, 1, 100000, 256, 3, 2);
                        // Deliberately unique probe environment: no inferred sharing across charts/connections.
                        var identity = new OrcaProviderSourceIdentity(Instrument.FullName, feedLifetime.Environment,
                            feedLifetime.Epoch, sessionCapture.Definition, sessionCapture.EventTimezone, true,
                            Guid.Parse(diagnosticsId), OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection,
                            "platform-supplied-unverified:v1");
                        if (feedLifetime.TryAcquire(identity, out publisher) != OrcaProviderAcquireStatus.Acquired)
                            throw new InvalidOperationException("Probe publisher acquisition failed.");
                        ingestion = new OrcaProviderIngestion(publisher,
                            OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection, historicalReplay);
                        if (feedLifetime.OpenReader(identity, out reader) != OrcaProviderReaderStatus.Opened) throw new InvalidOperationException("Probe reader acquisition failed.");
                        comparison = OrcaProviderReaderComparison.Create(feedLifetime, identity);
                        OrcaDiagnosticsCore.RegisterInstance(diagnosticsId, "OrcaProviderProbe", this);
                        OrcaDiagnosticsCore.ReportSeriesDeclaration(diagnosticsId, 0, "PrimaryChartSeries", "Chart", "Probe adds no secondary series");
                        Print("OrcaProviderProbe " + diagnosticsId + ": started; TickReplay=" + historicalReplay
                            + "; capacity=100000 events; platform timezone=" + eventTimeZone.Id + "; comparisonReaders=2; no production consumers attached");
                    }
                    else if (State == State.Realtime && ingestion != null && !faulted)
                    {
                        if (!comparison.DrainThrough(historicalCount + liveCount)) throw new InvalidOperationException("Comparison fell behind at realtime handoff.");
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
            if (e == null) return;
            State callbackState = State;
            lock (probeSync)
            {
                if (terminated || faulted || ingestion == null) return;
                if (e.IsReset)
                {
                    Fault("Market-data reset; reload probe before resuming.", OrcaStreamFault.SourceDisconnected);
                    return;
                }
                if (e.MarketDataType != MarketDataType.Last) return;
                if (callbackState == State.Transition)
                {
                    Fault("Last callback during Transition; historical/live assignment is unverified.", OrcaStreamFault.HistoricalGap);
                    return;
                }
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
                    if (((historicalCount + liveCount) & 255) == 0)
                        if (!comparison.DrainThrough(historicalCount + liveCount)) throw new InvalidOperationException("Comparison fell behind producer.");
                }
                catch (Exception ex) { Fault(ex.Message, OrcaStreamFault.InvalidEvent); }
                finally
                {
                    if (workStart != 0) OrcaDiagnosticsCore.ReportWorkSample(diagnosticsId, OrcaDiagnosticsWorkKind.MarketData, -1, workStart);
                }
                if (!faulted)
                {
                    try { ReportStatus(!historical && liveCount == 1); }
                    catch (Exception ex) { Fault(ex.Message, OrcaStreamFault.IngestionFailed); }
                }
            }
        }

        protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs e)
        {
            if (e == null) return;
            lock (probeSync)
            {
                if (terminated || ingestion == null) return;
                // Bounded evidence continues after fault without reopening or publishing.
                // Current object state is context only, not proof that an older callback is safe.
                if (connectionReports++ < 8)
                    Print("OrcaProviderProbe " + diagnosticsId + ": connection-observation utc="
                        + DateTime.UtcNow.ToString("O") + " state=" + State
                        + " price=" + e.PreviousPriceStatus + "->" + e.PriceStatus
                        + " order=" + e.PreviousStatus + "->" + e.Status
                        + " currentPrice=" + (e.Connection == null ? "unavailable" : e.Connection.PriceStatus.ToString())
                        + " error=" + e.Error + " historical=" + historicalCount + " live=" + liveCount
                        + " alreadyFaulted=" + faulted);
                // Any notification can indicate changed routing. Even order-only notifications
                // conservatively invalidate this isolated experiment; no feed attribution is guessed.
                if (!terminated && ingestion != null)
                    Fault("Connection notification (price=" + e.PriceStatus
                        + "); continuity unverified; reload probe before resuming (any connection).", OrcaStreamFault.SourceDisconnected);
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
            if (!comparison.DrainThrough(historicalCount + liveCount)) throw new InvalidOperationException("Comparison status is not caught up.");
            using (var batch = reader.Read(reader.FirstAvailable, 1))
            {
                string status = "phase=" + batch.Coverage.Phase + " historical=" + historicalCount + " live=" + liveCount
                    + " retained=" + (batch.PublishedThroughExclusive - batch.FirstAvailable.Sequence)
                    + " evicted=" + batch.FirstAvailable.Sequence + " passComplete=" + batch.Coverage.HistoricalPassCompleted
                    + " fullPassRetained=" + batch.Coverage.FullHistoricalPassRetained + " UTC-range-confirmed=false"
                    + " readerComparison=" + (comparison.VerifiedEvents > 0 ? "PASS" : "NO_EVENTS")
                    + " verified=" + comparison.VerifiedEvents + " volume=" + comparison.Volume
                    + " signed=" + comparison.SignedVolume + " digest=" + comparison.Digest.ToString("X16")
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
            if (comparison != null) comparison.Dispose();
            if (feedLifetime != null) feedLifetime.Dispose();
            Print("OrcaProviderProbe " + diagnosticsId + ": FAULT " + message);
            OrcaDiagnosticsCore.ReportSourceDeclaration(diagnosticsId, "ExperimentalPrimaryLastProbe", "Unavailable", "PrivateProbeRegistry");
        }
    }
}
