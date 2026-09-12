using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class OrcaCandleVolumeProfile
    {
        private FootprintBook footprintBook;
        private SessionIterator footprintSessions;
        private long firstFootprintSession;
        private DateTime footprintSessionBegin, footprintSessionEnd;
        private DateTime footprintBidQuoteTime, footprintAskQuoteTime;
        private long footprintSessionId;
        private volatile bool footprintStopped, footprintConnectionGap;
        private int footprintPending, footprintGeneration, footprintReaders;
        private long footprintAppliedRevision = -1, footprintRenderTicks;
        private int footprintObserveSamples;
        private string footprintViewportKey, footprintFailure;
        private DispatcherTimer footprintTimer;
        private ChartControl footprintChart;
        private ToolTip footprintTooltip;
        private FootprintFrame footprintFrame;
        private readonly List<FootprintFrame> footprintRetired = new List<FootprintFrame>();
        private SolidColorBrush footprintNeutralDx, footprintTextDx, footprintBullCandleDx, footprintBearCandleDx;
        private SolidColorBrush[] footprintAskDx, footprintBidDx, footprintUnknownDx;
        private string footprintDiagnosticsId;
        private string footprintNumeralFamily, footprintFontSignature;
        private volatile bool footprintDataFailed;
        private readonly CancellationTokenSource footprintCancellation = new CancellationTokenSource();

        private bool IsEnhancedFootprintActive
        { get { return EnhancedFootprint && ProfileDisplayMode == CandleProfileDisplayMode.BidAsk; } }

        private sealed class FootprintRequest
        {
            public int Generation, From, To, First, Last, Display, ActiveBar;
            public float Width, Height, PanelWidth, Dpi;
            public double Min, Max;
            public ChartScale Scale;
        }
        private sealed class FootprintPaintRow
        {
            public FootprintRow Evidence;
            public TextLayout Left, Right, Center, CenterDelta;
            public bool BodyDeltaRow;
        }
        private sealed class FootprintPaintBar
        {
            public FootprintBarSnapshot Evidence;
            public FootprintPaintRow[] Rows;
            public long SideDenominator, TotalDenominator, CenterMaxAbsDelta;
            public float CenterWidth;
            public double Open, High, Low, Close;
            public bool Developing;
        }
        private sealed class FootprintFrame : IDisposable
        {
            public FootprintRequest Request;
            public FootprintPaintBar[] Bars;
            public TextLayout Status;
            public string FontNote;
            public string StatusText;
            public float StatusHeight;
            public long EstimatedBytes;
            public readonly List<IDisposable> Resources = new List<IDisposable>();
            public void Dispose() { foreach (IDisposable item in Resources) item.Dispose(); Resources.Clear(); }
        }

        private void InitializeEnhancedFootprint()
        {
            if (!IsEnhancedFootprintActive) return;
            footprintBook = new FootprintBook();
            footprintSessions = new SessionIterator(BarsArray[0]);
            footprintDiagnosticsId = sharedSourceId.ToString("N") + ":footprint";
            OrcaDiagnosticsCore.RegisterInstance(footprintDiagnosticsId, "OrcaCandleVolumeProfile", this);
            OrcaDiagnosticsCore.ReportSourceDeclaration(footprintDiagnosticsId, TradeSourceMode.ToString(),
                "Quote provenance unverified", "Local strict evidence; shared publication remains inferred");
            OrcaDiagnosticsCore.ReportSeriesDeclaration(footprintDiagnosticsId, 0, BarsPeriod.ToString(), "Primary", "Existing chart");
            if (TradeSourceMode == CandleProfileTradeSourceMode.SecondaryTickSeries)
                OrcaDiagnosticsCore.ReportSeriesDeclaration(footprintDiagnosticsId, 1, "Tick 1 Last", "SecondaryTickSeries", "Existing series; no new series");
            OrcaDiagnosticsCore.ReportState(footprintDiagnosticsId, State.ToString());
        }

        private void ObserveEnhancedTrade(int index, DateTime time, double price, long volume, int side, FootprintDataQuality quality)
        {
            if (footprintDataFailed) return;
            try { ObserveEnhancedTradeCore(index, time, price, volume, side, quality); }
            catch (Exception error)
            {
                // An enhancement failure must not interrupt the legacy/shared accumulation path.
                footprintFailure = error.GetType().Name + ": " + error.Message;
                footprintDataFailed = true;
            }
        }

        private void ObserveEnhancedQuoteTime(MarketDataEventArgs e)
        {
            if (e.MarketDataType == MarketDataType.Bid || (e.MarketDataType == MarketDataType.Last && e.Bid > 0 && !double.IsInfinity(e.Bid)))
                footprintBidQuoteTime = e.Time;
            if (e.MarketDataType == MarketDataType.Ask || (e.MarketDataType == MarketDataType.Last && e.Ask > 0 && !double.IsInfinity(e.Ask)))
                footprintAskQuoteTime = e.Time;
        }

        private void ObserveEnhancedTradeCore(int index, DateTime time, double price, long volume, int side, FootprintDataQuality quality)
        {
            if (footprintBook == null || footprintStopped) return;
            long started = OrcaDiagnosticsCore.IsEnabled && (++footprintObserveSamples & 255) == 1 ? Stopwatch.GetTimestamp() : 0;
            if (footprintSessionId == 0 || time > footprintSessionEnd || time < footprintSessionBegin)
            {
                footprintSessions.GetNextSession(time, true);
                footprintSessionBegin = footprintSessions.ActualSessionBegin;
                footprintSessionEnd = footprintSessions.ActualSessionEnd;
                footprintSessionId = footprintSessions.ActualTradingDayExchange.Ticks;
                if (firstFootprintSession == 0) firstFootprintSession = footprintSessionId;
            }
            if (footprintConnectionGap) quality |= FootprintDataQuality.ConnectionGap;
            // Characterize the legacy attribution result without changing its price/time search.
            int native = BarsArray[0].GetBar(time);
            if (native != index || (TradeSourceMode == CandleProfileTradeSourceMode.TickReplayLastEvents
                && CurrentBars != null && CurrentBars.Length > 0 && index > CurrentBars[0]))
                quality |= FootprintDataQuality.AttributionUnverified;
            long quoteAge = -1;
            if ((quality & FootprintDataQuality.TradeQuoteUnverified) != 0) quoteAge = 0;
            else if (footprintBidQuoteTime != DateTime.MinValue && footprintAskQuoteTime != DateTime.MinValue
                && time >= footprintBidQuoteTime && time >= footprintAskQuoteTime)
                quoteAge = time.Ticks - Math.Min(footprintBidQuoteTime.Ticks, footprintAskQuoteTime.Ticks);
            footprintBook.Observe(index, FootprintBook.PriceTick(price, TickSize), volume, side,
                footprintSessionId, footprintSessionId == firstFootprintSession || footprintConnectionGap, time, quality, quoteAge);
            if (started > 0) OrcaDiagnosticsCore.ReportWorkPhaseSample(footprintDiagnosticsId, "FootprintObserve", Stopwatch.GetTimestamp() - started);
        }

        protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs e)
        {
            base.OnConnectionStatusUpdate(e);
            if (IsEnhancedFootprintActive && State == State.Realtime && e.PriceStatus != ConnectionStatus.Connected)
                footprintConnectionGap = true;
        }

        private void StartEnhancedFootprintObserver()
        {
			bool enhanced = IsEnhancedFootprintActive;
			if ((!enhanced && !IsDeltaColoredVolumeTextActive) || ChartControl == null) return;
            footprintChart = ChartControl;
            footprintChart.Dispatcher.BeginInvoke(new Action(() =>
            {
				if (footprintStopped || footprintTooltip != null) return;
                footprintTooltip = new ToolTip { PlacementTarget = footprintChart,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse, MaxWidth = 560 };
                footprintChart.MouseMove += FootprintMouseMove;
                footprintChart.MouseLeave += FootprintMouseLeave;
				if (!enhanced) return;
                footprintTimer = new DispatcherTimer(DispatcherPriority.Background, footprintChart.Dispatcher);
                footprintTimer.Interval = TimeSpan.FromMilliseconds(100);
                footprintTimer.Tick += FootprintTimerTick;
                footprintTimer.Start();
                if (footprintNeutralDx == null && RenderTarget != null) ResetEnhancedRenderTarget();
                FootprintTimerTick(null, EventArgs.Empty);
            }));
        }

        private void FlushEnhancedFootprint()
        {
            if (!IsEnhancedFootprintActive) return;
            if (footprintDiagnosticsId != null) OrcaDiagnosticsCore.ReportState(footprintDiagnosticsId, State.ToString());
            if (footprintChart != null && !footprintStopped)
                footprintChart.Dispatcher.BeginInvoke(new Action(() => FootprintTimerTick(null, EventArgs.Empty)));
        }

        private FootprintRequest ReadFootprintViewport()
        {
            if (ChartPanel == null || ChartBars == null || footprintChart == null || !footprintChart.IsVisible) return null;
            ChartScale scale = null;
            foreach (ChartScale candidate in ChartPanel.Scales)
            {
                foreach (var chartObject in candidate.ChartObjects)
                    if (ReferenceEquals(chartObject, this)) { scale = candidate; break; }
                if (scale != null) break;
            }
            if (scale == null)
                foreach (ChartScale candidate in ChartPanel.Scales)
                    if (candidate.ScaleJustification == ScaleJustification) { scale = candidate; break; }
            if (scale == null || scale.MaxValue <= scale.MinValue || ChartPanel.H <= 0) return null;
            int from = Math.Max(0, ChartBars.FromIndex), to = Math.Min(ChartBars.ToIndex, ChartBars.Count - 1);
            if (to < from) return null;
            float spacing = to > from ? Math.Abs(footprintChart.GetXByBarIndex(ChartBars, from + 1)
                - footprintChart.GetXByBarIndex(ChartBars, from)) : BidAskWidthPx + 2;
            var source = System.Windows.PresentationSource.FromVisual(footprintChart);
            float dpi = source == null || source.CompositionTarget == null ? 1f : (float)source.CompositionTarget.TransformToDevice.M11;
            int count = to - from + 1;
            return new FootprintRequest { From = from, To = to, First = Math.Max(0, from - count),
                Last = Math.Min(ChartBars.Count - 1, to + count), Display = ResolveDeltaCompressionTicks(scale),
                Width = Math.Max(.5f, Math.Min(BidAskWidthPx, spacing - 2)), Height = ChartPanel.H, PanelWidth = ChartPanel.W,
                Min = scale.MinValue, Max = scale.MaxValue, Scale = scale, Dpi = dpi,
                ActiveBar = CurrentBars != null && CurrentBars.Length > 0 ? CurrentBars[0] : -1 };
        }

        private void FootprintTimerTick(object sender, EventArgs e)
        {
            try { PrepareEnhancedViewport(); }
            catch (Exception error) { footprintFailure = error.GetType().Name + ": " + error.Message; }
        }

        private void PrepareEnhancedViewport()
        {
            if (footprintStopped || footprintBook == null) return;
            FootprintRequest request = ReadFootprintViewport();
            if (request == null) return;
            string key = string.Join("|", request.From, request.To, request.Display, request.Width, request.Height,
                request.Min, request.Max, request.Dpi, request.ActiveBar, request.PanelWidth, footprintDataFailed, footprintConnectionGap,
                (int)FootprintScaffold, CandleWidthPx, CandleProfileGapPx, BidAskTextFontSize, BidAskTextMinThreshold,
                UseDynamicTextSizing, DynamicTextMaxFontSize, (int)FootprintNumbers, TextFontFamily, (int)TextFontWeight);
            if (key != footprintViewportKey)
            { footprintViewportKey = key; footprintGeneration++; footprintAppliedRevision = -1; }
            request.Generation = footprintGeneration;
            long renderTicks = Interlocked.Exchange(ref footprintRenderTicks, 0);
            if (renderTicks > 0) OrcaDiagnosticsCore.ReportWorkPhaseSample(footprintDiagnosticsId, "EnhancedRender", renderTicks);
            DisposeRetiredFootprintFrames();
            if (footprintAppliedRevision == footprintBook.Revision || Interlocked.CompareExchange(ref footprintPending, 1, 0) != 0) return;
            if (footprintRetired.Count != 0)
            { Interlocked.Exchange(ref footprintPending, 0); return; }
            // One worker; viewport changes replace the request on the next timer turn, never enqueue more jobs.
            Task.Run(() =>
            {
                FootprintViewportSnapshot snapshot = null;
                string failure = null;
                long started = Stopwatch.GetTimestamp();
                try
                {
                    if (footprintDataFailed)
                        snapshot = new FootprintViewportSnapshot(footprintBook.Revision, new FootprintBarSnapshot[0], new long[0], new long[0], "N/A - evidence stopped: " + footprintFailure);
                    else if (!footprintStopped)
                        snapshot = footprintBook.Capture(request.First, request.Last, request.From, request.To,
                            request.Display, Math.Max(1, FootprintAnalysisTicks), FootprintScale, FootprintFixedVolume, footprintCancellation.Token);
                }
                catch (OperationCanceledException) { }
                catch (Exception error) { failure = error.GetType().Name + ": " + error.Message; }
                OrcaDiagnosticsCore.ReportWorkPhaseSample(footprintDiagnosticsId, "FootprintPrepare", Stopwatch.GetTimestamp() - started);
                if (footprintStopped || footprintChart.Dispatcher.HasShutdownStarted)
                { Interlocked.Exchange(ref footprintPending, 0); return; }
                footprintChart.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (footprintStopped || request.Generation != footprintGeneration) return;
                        if (failure != null)
                        {
                            footprintFailure = failure;
                            snapshot = new FootprintViewportSnapshot(footprintBook.Revision, new FootprintBarSnapshot[0], new long[0], new long[0], "N/A - preparation failed: " + failure);
                        }
                        if (snapshot == null) return;
                        FootprintFrame next;
                        try { next = PrepareFootprintFrame(request, snapshot); }
                        catch (Exception error)
                        {
                            footprintFailure = error.GetType().Name + ": " + error.Message;
                            next = PrepareFootprintFailureFrame(request, footprintFailure);
                        }
                        FootprintFrame old = Interlocked.Exchange(ref footprintFrame, next);
                        if (old != null) footprintRetired.Add(old);
                        footprintAppliedRevision = snapshot.Revision;
                        if (failure == null && !footprintDataFailed) footprintFailure = null;
                        DisposeRetiredFootprintFrames();
                        OrcaDiagnosticsCore.ReportModelUpdate(footprintDiagnosticsId, DateTime.UtcNow, snapshot.Status.Length == 0 ? "Quote provenance unverified" : snapshot.Status);
                        OrcaDiagnosticsCore.ReportCacheStatus(footprintDiagnosticsId, "CVP derived rows", footprintBook.CachedBytes.ToString(CultureInfo.InvariantCulture) + " bytes");
                        footprintChart.InvalidateVisual();
                    }
                    catch (Exception error) { footprintFailure = error.GetType().Name + ": " + error.Message; }
                    finally { Interlocked.Exchange(ref footprintPending, 0); }
                }));
            });
            // The existing primary OnBarUpdate owns shared registration; rendering never refreshes it.
        }

        private FootprintFrame PrepareFootprintFrame(FootprintRequest request, FootprintViewportSnapshot snapshot)
        {
            var frame = new FootprintFrame { Request = request };
            try
            {
                string family = GetProfileTextFontFamily();
                float rowHeight = (float)(TickSize * request.Display / (request.Max - request.Min) * request.Height);
                float size = UseDynamicTextSizing ? Math.Max(BidAskTextFontSize, Math.Min(DynamicTextMaxFontSize, rowHeight * 0.72f)) : BidAskTextFontSize;
                var format = new TextFormat(Core.Globals.DirectWriteFactory, family, ResolveProfileTextFontWeight(), FontStyle.Normal, size)
                { WordWrapping = WordWrapping.NoWrap, ParagraphAlignment = ParagraphAlignment.Center };
                frame.Resources.Add(format);
                var typography = new Typography(Core.Globals.DirectWriteFactory);
                typography.AddFontFeature(new FontFeature(FontFeatureTag.TabularFigures, 1));
                frame.Resources.Add(typography);
                string fontKey = family + "|" + TextFontWeight + "|" + request.Dpi;
                if (fontKey != footprintFontSignature)
                {
                    bool installed = false;
                    foreach (System.Windows.Media.FontFamily candidate in System.Windows.Media.Fonts.SystemFontFamilies)
                        if (string.Equals(candidate.Source, family, StringComparison.OrdinalIgnoreCase)) { installed = true; break; }
                    footprintNumeralFamily = installed && HasTabularFootprintDigits(format, typography) ? family : "Consolas";
                    footprintFontSignature = fontKey;
                }
                bool tabular = footprintNumeralFamily == family;
                if (!tabular)
                {
                    format = new TextFormat(Core.Globals.DirectWriteFactory, "Consolas", ResolveProfileTextFontWeight(), FontStyle.Normal, size)
                    { WordWrapping = WordWrapping.NoWrap, ParagraphAlignment = ParagraphAlignment.Center };
                    frame.Resources.Add(format);
                }
                frame.FontNote = tabular ? "Numerals: " + family + " (tabular)" : "Numerals: Consolas fallback; selected " + family;
                var cache = new Dictionary<string, TextLayout>();
                var measurementWidths = new Dictionary<string, float>();
                var formats = new Dictionary<float, TextFormat>();
                var winnerFormats = new Dictionary<float, TextFormat>();
                formats[size] = format;
                var bars = new List<FootprintPaintBar>();
                bool partial = false, unknown = false, attribution = false, clipped = false;
                int missing = request.To - request.From + 1;
                for (int i = 0; i < snapshot.Bars.Count; i++)
                {
                    FootprintBarSnapshot evidence = snapshot.Bars[i];
                    if (evidence.BarIndex < request.From || evidence.BarIndex > request.To) continue;
                    missing--;
                    partial |= evidence.PartialSession;
                    unknown |= (evidence.Quality & FootprintDataQuality.Unclassified) != 0;
                    attribution |= (evidence.Quality & (FootprintDataQuality.AttributionUnverified | FootprintDataQuality.OutOfOrder)) != 0;
                    var bar = new FootprintPaintBar { Evidence = evidence, SideDenominator = snapshot.SideDenominators[i],
                        TotalDenominator = snapshot.TotalDenominators[i], Developing = evidence.BarIndex == request.ActiveBar,
                        Open = BarsArray[0].GetOpen(evidence.BarIndex), High = BarsArray[0].GetHigh(evidence.BarIndex),
                        Low = BarsArray[0].GetLow(evidence.BarIndex), Close = BarsArray[0].GetClose(evidence.BarIndex) };
                    bool hollowBodyDelta = FootprintScaffold == FootprintScaffoldMode.HollowBodyDelta;
                    float widestCenterLabel = 0f;
                    if (hollowBodyDelta)
                    {
                        foreach (FootprintRow row in evidence.Rows)
                        {
                            if ((row.Tick + request.Display) * TickSize < request.Min || row.Tick * TickSize > request.Max) continue;
                            float cellHeight = Math.Max(0, request.Scale.GetYByValue((row.Tick - .5) * TickSize)
                                - request.Scale.GetYByValue((row.Tick + request.Display - .5) * TickSize) - Math.Max(0, ProfileBarSpacingPx));
                            float cellSize = UseDynamicTextSizing ? (float)Math.Round(Math.Max(BidAskTextFontSize,
                                Math.Min(DynamicTextMaxFontSize, cellHeight * .72f))) : BidAskTextFontSize;
                            TextFormat measureFormat = GetOrCreateFootprintTextFormat(frame, formats, cellSize);
                            bool unavailable = row.Bid == 0 && row.Ask == 0 && row.Unclassified > 0;
                            string label = ResolveHollowDeltaText(row.Delta, unavailable);
                            widestCenterLabel = Math.Max(widestCenterLabel, MeasureFootprintTextWidth(
                                label, measureFormat, typography, measurementWidths));
                        }
                    }
                    float bodyDeltaWidth = hollowBodyDelta
                        ? ResolveBodyDeltaColumnWidth(CandleWidthPx, widestCenterLabel)
                        : 0f;
                    bar.CenterWidth = bodyDeltaWidth;
                    float effectiveGutter = ResolveFootprintCenterGutter(request.Width, bodyDeltaWidth);
                    bodyDeltaWidth = hollowBodyDelta
                        ? Math.Min(bodyDeltaWidth, Math.Max(1f, effectiveGutter - 2f * Math.Max(0, CandleProfileGapPx)))
                        : 0f;
                    bar.CenterWidth = bodyDeltaWidth;
                    float available = Math.Max(0, request.Width / 2 - effectiveGutter / 2f - 2);
                    var rows = new List<FootprintPaintRow>();
                    foreach (FootprintRow row in evidence.Rows)
                    {
                        double rowLow = (row.Tick - .5) * TickSize;
                        double rowHigh = (row.Tick + request.Display - .5) * TickSize;
                        bool bodyDeltaRow = hollowBodyDelta && FootprintFormatting.IntersectsBody(rowLow, rowHigh, bar.Open, bar.Close);
                        if (hollowBodyDelta)
                            bar.CenterMaxAbsDelta = Math.Max(bar.CenterMaxAbsDelta, FootprintFormatting.Magnitude(row.Delta));
                        if ((row.Tick + request.Display) * TickSize < request.Min || row.Tick * TickSize > request.Max) continue;
                        var paint = new FootprintPaintRow { Evidence = row, BodyDeltaRow = bodyDeltaRow };
                        frame.EstimatedBytes += 96;
                        if (frame.EstimatedBytes > 8L * 1024 * 1024 - 65536)
                            throw new InvalidOperationException("Presentation budget exceeded; reduce visible bars or increase display row size. Evidence retained.");
                        float cellHeight = Math.Max(0, request.Scale.GetYByValue((row.Tick - .5) * TickSize)
                            - request.Scale.GetYByValue((row.Tick + request.Display - .5) * TickSize) - Math.Max(0, ProfileBarSpacingPx));
                        float cellSize = UseDynamicTextSizing ? (float)Math.Round(Math.Max(BidAskTextFontSize,
                            Math.Min(DynamicTextMaxFontSize, cellHeight * .72f))) : BidAskTextFontSize;
                        TextFormat cellFormat = GetOrCreateFootprintTextFormat(frame, formats, cellSize);
                        TextFormat winnerFormat = cellFormat;
                        if (FootprintEmphasizeWinner && FootprintValues == FootprintCellView.BidAsk)
                        {
                            if (!winnerFormats.TryGetValue(cellSize, out winnerFormat))
                            {
                                winnerFormat = new TextFormat(Core.Globals.DirectWriteFactory, footprintNumeralFamily, ResolveFootprintWinnerWeight(), FontStyle.Normal, cellSize)
                                { WordWrapping = WordWrapping.NoWrap, ParagraphAlignment = ParagraphAlignment.Center };
                                winnerFormats.Add(cellSize, winnerFormat); frame.Resources.Add(winnerFormat);
                            }
                        }
                        clipped |= BidAskStyle == CandleProfileBidAskStyle.Histogram ? Math.Max(row.Ask, row.Bid) > bar.SideDenominator : row.Total > bar.TotalDenominator;
                        if (hollowBodyDelta)
                        {
                            bool unavailable = row.Bid == 0 && row.Ask == 0 && row.Unclassified > 0;
                            paint.CenterDelta = PrepareFootprintText(frame, cache, cellFormat, typography,
                                unavailable ? "N/A" : FootprintFormatting.SignedNumber(row.Delta, false),
                                unavailable ? "N/A" : FootprintFormatting.SignedNumber(row.Delta, true),
                                Math.Max(0, bodyDeltaWidth - 2f), cellHeight + 2, TextAlignment.Center);
                        }
                        if (ShowBidAskText && row.Total >= BidAskTextMinThreshold)
                        {
                            if (FootprintValues == FootprintCellView.BidAsk)
                            {
                                // No classified evidence means N/A, not invented zero-side certainty.
                                bool unavailable = row.Bid == 0 && row.Ask == 0 && row.Unclassified > 0;
                                TextFormat bidFormat = FootprintEmphasizeWinner && FootprintFormatting.IsWinner(row.Bid, row.Ask, FootprintWinnerRatio) ? winnerFormat : cellFormat;
                                TextFormat askFormat = FootprintEmphasizeWinner && FootprintFormatting.IsWinner(row.Ask, row.Bid, FootprintWinnerRatio) ? winnerFormat : cellFormat;
                                paint.Left = PrepareFootprintText(frame, cache, bidFormat, typography, unavailable ? "N/A" : FootprintFormatting.Number(row.Bid, false),
                                    unavailable ? "N/A" : FootprintFormatting.Number(row.Bid, true), available, cellHeight + 2, TextAlignment.Trailing);
                                paint.Right = PrepareFootprintText(frame, cache, askFormat, typography, unavailable ? "N/A" : FootprintFormatting.Number(row.Ask, false),
                                    unavailable ? "N/A" : FootprintFormatting.Number(row.Ask, true), available, cellHeight + 2, TextAlignment.Leading);
                            }
                            else
                            {
                                bool unavailable = row.Bid == 0 && row.Ask == 0 && row.Unclassified > 0 && FootprintValues != FootprintCellView.Total;
                                paint.Center = PrepareFootprintText(frame, cache, cellFormat, typography, unavailable ? "N/A" : FootprintFormatting.Value(row, FootprintValues, false),
                                    unavailable ? "N/A" : FootprintFormatting.Value(row, FootprintValues, true), available, cellHeight + 2, TextAlignment.Leading);
                            }
                        }
                        rows.Add(paint);
                    }
                    bar.Rows = rows.ToArray(); bars.Add(bar);
                }
                frame.Bars = bars.ToArray();
                string status = BidAskStyle == CandleProfileBidAskStyle.Cluster ? "Cluster - Delta Heatmap" : "Bid x Ask - Histogram";
                status += " | " + FootprintScale + (FootprintScale == FootprintScaleMode.SessionGlobal ? " (developing)" : "");
                status += " | " + (TradeSourceMode == CandleProfileTradeSourceMode.SecondaryTickSeries ? "Secondary ticks: quote/sequence limits" : "Trade quotes: provenance unverified");
                if (partial) status += " | partial session";
                if (unknown) status += " | U: unclassified";
                if (attribution) status += " | attribution/order check";
                if (footprintConnectionGap) status += " | connection gap";
                if (missing > 0) status += " | N/A: " + missing + " bars";
                if (clipped) status += " | clipped (exact values on hover)";
                if (!tabular) status += " | Consolas numerals";
                if (!string.IsNullOrEmpty(snapshot.Status)) status += " | " + snapshot.Status;
                if (frame.EstimatedBytes >= 7L * 1024 * 1024) status += " | text budget: some labels hidden";
                var statusFormat = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", 10f) { WordWrapping = WordWrapping.Wrap };
                frame.Resources.Add(statusFormat);
                frame.Status = new TextLayout(Core.Globals.DirectWriteFactory, status, statusFormat, Math.Max(40, ChartPanel.W - 16), 60);
                frame.Resources.Add(frame.Status);
                frame.StatusText = status;
                frame.StatusHeight = Math.Min(60, frame.Status.Metrics.Height) + 12;
                return frame;
            }
            catch { frame.Dispose(); throw; }
        }

        private FootprintFrame PrepareFootprintFailureFrame(FootprintRequest request, string error)
        {
            var frame = new FootprintFrame { Request = request, Bars = new FootprintPaintBar[0], FontNote = "Numerals unavailable",
                StatusText = "N/A - enhanced footprint: " + error };
            try
            {
                var format = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", 10f);
                frame.Resources.Add(format);
                frame.Status = new TextLayout(Core.Globals.DirectWriteFactory, frame.StatusText, format, Math.Max(40, ChartPanel.W - 16), 60);
                frame.Resources.Add(frame.Status); frame.StatusHeight = 72;
                return frame;
            }
            catch { frame.Dispose(); throw; }
        }

        private bool HasTabularFootprintDigits(TextFormat format, Typography typography)
        {
            float width = -1;
            for (int i = 0; i < 10; i++)
                using (var layout = new TextLayout(Core.Globals.DirectWriteFactory, i.ToString(CultureInfo.InvariantCulture), format, 100, 100))
                {
                    layout.SetTypography(typography, new TextRange(0, 1));
                    float current = layout.Metrics.WidthIncludingTrailingWhitespace;
                    if (width >= 0 && Math.Abs(width - current) > 0.05f) return false;
                    width = current;
                }
            return true;
        }

        private TextFormat GetOrCreateFootprintTextFormat(FootprintFrame frame, Dictionary<float, TextFormat> formats, float size)
        {
            TextFormat format;
            if (formats.TryGetValue(size, out format)) return format;
            format = new TextFormat(Core.Globals.DirectWriteFactory, footprintNumeralFamily, ResolveProfileTextFontWeight(), FontStyle.Normal, size)
            { WordWrapping = WordWrapping.NoWrap, ParagraphAlignment = ParagraphAlignment.Center };
            formats.Add(size, format);
            frame.Resources.Add(format);
            return format;
        }

        private float MeasureFootprintTextWidth(string text, TextFormat format, Typography typography,
            Dictionary<string, float> measurementWidths)
        {
            string key = string.Join(":", text, format.FontSize, (int)format.FontWeight);
            float width;
            if (measurementWidths.TryGetValue(key, out width)) return width;
            using (var layout = new TextLayout(Core.Globals.DirectWriteFactory, text, format, 1000, 100))
            {
                layout.SetTypography(typography, new TextRange(0, text.Length));
                width = layout.Metrics.WidthIncludingTrailingWhitespace;
            }
            measurementWidths.Add(key, width);
            return width;
        }

        private TextLayout PrepareFootprintText(FootprintFrame frame, Dictionary<string, TextLayout> cache, TextFormat format,
            Typography typography, string full, string compact, float width, float height, TextAlignment alignment)
        {
            if (width < 2 || height < 2 || frame.EstimatedBytes >= 7L * 1024 * 1024) return null;
            string text = FootprintNumbers == FootprintNumberFormat.Compact ? compact : full;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string key = string.Join(":", text, (int)alignment, width, height, format.FontSize, (int)format.FontWeight);
                TextLayout layout;
                if (cache.TryGetValue(key, out layout)) return layout;
                layout = new TextLayout(Core.Globals.DirectWriteFactory, text, format, width, height);
                layout.TextAlignment = alignment;
                layout.SetTypography(typography, new TextRange(0, text.Length));
                if (FootprintFormatting.Fits(layout.Metrics.WidthIncludingTrailingWhitespace, layout.Metrics.Height, width, height))
                { cache.Add(key, layout); frame.Resources.Add(layout); frame.EstimatedBytes += 1024 + text.Length * 32L; return layout; }
                layout.Dispose();
                if (FootprintNumbers != FootprintNumberFormat.Auto || text == compact) break;
                text = compact;
            }
            return null;
        }

        private FontWeight ResolveFootprintWinnerWeight()
        {
            FontWeight current = ResolveProfileTextFontWeight();
            if (current >= FontWeight.ExtraBold) return FontWeight.ExtraBlack;
            return current >= FontWeight.SemiBold ? FontWeight.ExtraBold : FontWeight.Bold;
        }

        private float ResolveFootprintCenterGutter(float width, float hollowCenterWidth)
        {
            float desired = FootprintGutterPx;
            if (FootprintScaffold == FootprintScaffoldMode.OhlcSpineAndBody)
                desired = Math.Max(desired, CandleWidthPx + 2f * Math.Max(0, CandleProfileGapPx));
            else if (FootprintScaffold == FootprintScaffoldMode.HollowBodyDelta)
                desired = Math.Max(desired, hollowCenterWidth + 2f * Math.Max(0, CandleProfileGapPx));
            return Math.Max(0, Math.Min(desired, Math.Max(0, width - 2)));
        }

        private void ResetEnhancedRenderTarget()
        {
            DisposeBrushPalette(ref footprintAskDx); DisposeBrushPalette(ref footprintBidDx); DisposeBrushPalette(ref footprintUnknownDx);
            if (footprintNeutralDx != null) footprintNeutralDx.Dispose();
            if (footprintTextDx != null) footprintTextDx.Dispose();
            if (footprintBullCandleDx != null) footprintBullCandleDx.Dispose();
            if (footprintBearCandleDx != null) footprintBearCandleDx.Dispose();
            footprintNeutralDx = null; footprintTextDx = null; footprintBullCandleDx = null; footprintBearCandleDx = null;
            if (RenderTarget == null || !IsEnhancedFootprintActive || footprintStopped) return;
            footprintNeutralDx = new SolidColorBrush(RenderTarget, new Color4(.75f, .75f, .75f, .8f));
            footprintTextDx = new SolidColorBrush(RenderTarget, ToDxColor(BidAskTextBrush, 1));
            footprintBullCandleDx = new SolidColorBrush(RenderTarget, ToDxColor(BullishBodyBrush, 1));
            footprintBearCandleDx = new SolidColorBrush(RenderTarget, ToDxColor(BearishBodyBrush, 1));
            footprintAskDx = new SolidColorBrush[32]; footprintBidDx = new SolidColorBrush[32]; footprintUnknownDx = new SolidColorBrush[32];
            for (int i = 0; i < 32; i++)
            {
                float opacity = BidAskMinOpacity + (BidAskMaxOpacity - BidAskMinOpacity) * i / 31f;
                footprintAskDx[i] = new SolidColorBrush(RenderTarget, ToDxColor(BidAskPositiveBrush, opacity));
                footprintBidDx[i] = new SolidColorBrush(RenderTarget, ToDxColor(BidAskNegativeBrush, opacity));
                footprintUnknownDx[i] = new SolidColorBrush(RenderTarget, ToDxColor(BidAskNeutralBrush, opacity));
            }
        }

        private void RenderEnhancedFootprint(ChartControl chart, ChartScale scale)
        {
            if (RenderTarget == null || footprintNeutralDx == null || footprintStopped) return;
            long started = Stopwatch.GetTimestamp();
            bool clipped = false;
            Interlocked.Increment(ref footprintReaders);
            try
            {
                FootprintFrame frame = Volatile.Read(ref footprintFrame);
                if (frame == null || frame.Request.Generation != footprintGeneration || !ReferenceEquals(scale, frame.Request.Scale)
                    || scale.MinValue != frame.Request.Min || scale.MaxValue != frame.Request.Max
                    || ChartPanel.H != frame.Request.Height || ChartPanel.W != frame.Request.PanelWidth
                    || ChartBars.FromIndex != frame.Request.From || Math.Min(ChartBars.ToIndex, ChartBars.Count - 1) != frame.Request.To) return;
                float width = frame.Request.Width;
                bool fullCandle = FootprintScaffold == FootprintScaffoldMode.OhlcSpineAndBody;
                bool hollowBodyDelta = FootprintScaffold == FootprintScaffoldMode.HollowBodyDelta;
                bool reserveCenter = fullCandle || hollowBodyDelta;
                float healthHeight = FootprintShowHealth ? Math.Min(frame.StatusHeight, ChartPanel.H) : 0;
                RenderTarget.PushAxisAlignedClip(new RectangleF(ChartPanel.X, ChartPanel.Y + healthHeight, ChartPanel.W, Math.Max(0, ChartPanel.H - healthHeight)), AntialiasMode.Aliased);
                clipped = true;
                for (int b = 0; b < frame.Bars.Length; b++)
                {
                    FootprintPaintBar bar = frame.Bars[b];
                    float x = chart.GetXByBarIndex(ChartBars, bar.Evidence.BarIndex);
                    if (x + width / 2 < ChartPanel.X || x - width / 2 > ChartPanel.X + ChartPanel.W) continue;
                    float gutter = ResolveFootprintCenterGutter(width, bar.CenterWidth);
                    float half = Math.Max(0, (width - gutter) / 2);
                    float bodyDeltaWidth = hollowBodyDelta
                        ? Math.Min(bar.CenterWidth, Math.Max(1f, gutter - 2f * Math.Max(0, CandleProfileGapPx)))
                        : 0f;
                    float bodyOutlineTop = float.MaxValue, bodyOutlineBottom = float.MinValue;
                    for (int r = 0; r < bar.Rows.Length; r++)
                    {
                        FootprintPaintRow paint = bar.Rows[r]; FootprintRow row = paint.Evidence;
                        float top = scale.GetYByValue((row.Tick + frame.Request.Display - .5) * TickSize);
                        float bottom = scale.GetYByValue((row.Tick - .5) * TickSize);
                        if (bottom < ChartPanel.Y || top > ChartPanel.Y + ChartPanel.H) continue;
                        float height = Math.Max(0, bottom - top - Math.Max(0, ProfileBarSpacingPx));
                        if (height <= 0) continue;
                        bool cluster = BidAskStyle == CandleProfileBidAskStyle.Cluster;
                        int intensity = (int)Math.Round(31 * FootprintFormatting.Fraction(row.Total, bar.TotalDenominator));
                        if (cluster)
                        {
                            SolidColorBrush fill = row.Delta > 0 ? footprintAskDx[intensity] : row.Delta < 0 ? footprintBidDx[intensity] : footprintUnknownDx[intensity];
                            if (reserveCenter)
                            {
                                RenderTarget.FillRectangle(new RectangleF(x - gutter / 2 - half, top, half, height), fill);
                                RenderTarget.FillRectangle(new RectangleF(x + gutter / 2, top, half, height), fill);
                            }
                            else RenderTarget.FillRectangle(new RectangleF(x - width / 2, top, width, height), fill);
                        }
                        else
                        {
                            float bidWidth = half * (float)FootprintFormatting.Fraction(row.Bid, bar.SideDenominator);
                            float askWidth = half * (float)FootprintFormatting.Fraction(row.Ask, bar.SideDenominator);
                            if (bidWidth > 0) RenderTarget.FillRectangle(new RectangleF(x - gutter / 2 - bidWidth, top, bidWidth, height), footprintBidDx[31]);
                            if (askWidth > 0) RenderTarget.FillRectangle(new RectangleF(x + gutter / 2, top, askWidth, height), footprintAskDx[31]);
                        }
                        if (row.Unclassified > 0)
                            RenderTarget.DrawLine(new Vector2(x - 1, top + 1), new Vector2(x + 1, Math.Max(top + 1, bottom - 1)), footprintNeutralDx, 2);
                        if (FootprintScale == FootprintScaleMode.Fixed)
                        {
                            if ((cluster && row.Total > bar.TotalDenominator) || (!cluster && row.Bid > bar.SideDenominator))
                                RenderTarget.DrawLine(new Vector2(x - width / 2, top), new Vector2(x - width / 2 + 3, top + 3), footprintNeutralDx);
                            if ((cluster && row.Total > bar.TotalDenominator) || (!cluster && row.Ask > bar.SideDenominator))
                                RenderTarget.DrawLine(new Vector2(x + width / 2 - 3, top + 3), new Vector2(x + width / 2, top), footprintNeutralDx);
                        }
                        if (paint.Left != null) RenderTarget.DrawTextLayout(new Vector2(x - width / 2 + 1, top - 1), paint.Left, footprintTextDx, DrawTextOptions.Clip);
                        if (paint.Right != null) RenderTarget.DrawTextLayout(new Vector2(x + gutter / 2 + 1, top - 1), paint.Right, footprintTextDx, DrawTextOptions.Clip);
                        if (paint.Center != null) RenderTarget.DrawTextLayout(new Vector2(x + gutter / 2 + 1, top - 1), paint.Center, footprintTextDx, DrawTextOptions.Clip);
                        if (hollowBodyDelta && bodyDeltaWidth >= 2f)
                        {
                            int deltaIntensity = (int)Math.Round(31 * FootprintFormatting.Fraction(
                                FootprintFormatting.Magnitude(row.Delta), bar.CenterMaxAbsDelta));
                            SolidColorBrush deltaBrush = row.Delta > 0 ? footprintAskDx[deltaIntensity]
                                : row.Delta < 0 ? footprintBidDx[deltaIntensity] : footprintUnknownDx[deltaIntensity];
                            RectangleF centerRect = new RectangleF(x - bodyDeltaWidth / 2f, top, bodyDeltaWidth, height);
                            if (paint.BodyDeltaRow)
                            {
                                bodyOutlineTop = Math.Min(bodyOutlineTop, centerRect.Top);
                                bodyOutlineBottom = Math.Max(bodyOutlineBottom, centerRect.Bottom);
                            }
                            if (bodyDeltaWidth >= 6f)
                                RenderTarget.DrawLine(new Vector2(centerRect.Left + 1f, centerRect.Bottom - 0.5f),
                                    new Vector2(centerRect.Right - 1f, centerRect.Bottom - 0.5f), footprintUnknownDx[0], 0.5f);
                            if (paint.CenterDelta != null)
                                RenderTarget.DrawTextLayout(new Vector2(x - bodyDeltaWidth / 2f + 1f, top - 1f), paint.CenterDelta, deltaBrush, DrawTextOptions.Clip);
                        }
                    }
                    if (hollowBodyDelta && bodyDeltaWidth >= 2f && bodyOutlineTop != float.MaxValue && bodyOutlineBottom > bodyOutlineTop)
                    {
                        SolidColorBrush bodyOutlineBrush = bar.Close >= bar.Open ? footprintBullCandleDx : footprintBearCandleDx;
                        if (bodyOutlineBrush != null)
                            RenderTarget.DrawRectangle(new RectangleF(x - bodyDeltaWidth / 2f, bodyOutlineTop,
                                bodyDeltaWidth, bodyOutlineBottom - bodyOutlineTop), bodyOutlineBrush, 1f);
                    }
                    if (ShowPOC)
                    {
                        float top = scale.GetYByValue((bar.Evidence.PocTick + bar.Evidence.AnalysisTicks - .5) * TickSize);
                        float bottom = scale.GetYByValue((bar.Evidence.PocTick - .5) * TickSize);
                        if (reserveCenter)
                        {
                            RenderTarget.DrawRectangle(new RectangleF(x - gutter / 2 - half, top, half, Math.Max(1, bottom - top)), footprintNeutralDx, bar.Developing ? .5f : 1f);
                            RenderTarget.DrawRectangle(new RectangleF(x + gutter / 2, top, half, Math.Max(1, bottom - top)), footprintNeutralDx, bar.Developing ? .5f : 1f);
                        }
                        else RenderTarget.DrawRectangle(new RectangleF(x - width / 2, top, width, Math.Max(1, bottom - top)), footprintNeutralDx, bar.Developing ? .5f : 1f);
                    }
                    if (FootprintScaffold != FootprintScaffoldMode.Off && !hollowBodyDelta)
                    {
                        float open = scale.GetYByValue(bar.Open), close = scale.GetYByValue(bar.Close);
                        RenderTarget.DrawLine(new Vector2(x, scale.GetYByValue(bar.High)), new Vector2(x, scale.GetYByValue(bar.Low)), footprintNeutralDx, 1);
                        RenderTarget.DrawLine(new Vector2(x - Math.Min(4, gutter / 2), open), new Vector2(x, open), footprintNeutralDx, 1);
                        RenderTarget.DrawLine(new Vector2(x + (bar.Developing ? 2 : 0), close), new Vector2(x + Math.Min(4, gutter / 2), close), footprintNeutralDx, bar.Developing ? 2 : 1);
                        if (fullCandle)
                        {
                            SolidColorBrush candleBrush = bar.Close >= bar.Open ? footprintBullCandleDx : footprintBearCandleDx;
                            float candleWidth = Math.Max(1, Math.Min(CandleWidthPx, Math.Max(1, gutter - 2f * Math.Max(0, CandleProfileGapPx))));
                            float wickWidth = Math.Max(1, Math.Min(WickWidthPx, candleWidth));
                            if (candleBrush != null)
                            {
                                RenderTarget.FillRectangle(new RectangleF(x - wickWidth / 2f, scale.GetYByValue(bar.High), wickWidth,
                                    Math.Max(1, scale.GetYByValue(bar.Low) - scale.GetYByValue(bar.High))), candleBrush);
                                RenderTarget.FillRectangle(new RectangleF(x - candleWidth / 2f, Math.Min(open, close), candleWidth,
                                    Math.Max(1, Math.Abs(open - close))), candleBrush);
                            }
                        }
                    }
                }
                RenderTarget.PopAxisAlignedClip(); clipped = false;
                if (FootprintShowHealth && frame.Status != null)
                    RenderTarget.DrawTextLayout(new Vector2(ChartPanel.X + 8, ChartPanel.Y + 6), frame.Status, footprintTextDx, DrawTextOptions.Clip);
            }
            finally
            {
                if (clipped) RenderTarget.PopAxisAlignedClip();
                Interlocked.Decrement(ref footprintReaders);
                Interlocked.Exchange(ref footprintRenderTicks, Stopwatch.GetTimestamp() - started);
            }
        }

        private void FootprintMouseLeave(object sender, MouseEventArgs e)
        { if (footprintTooltip != null) footprintTooltip.IsOpen = false; }

        private void FootprintMouseMove(object sender, MouseEventArgs e)
        {
			if (TryShowQualifiedVolumeTextTooltip(e)) return;
			if (!IsEnhancedFootprintActive)
			{
				if (footprintTooltip != null) footprintTooltip.IsOpen = false;
				return;
			}

            FootprintFrame frame = footprintFrame;
            if (frame == null || footprintTooltip == null || footprintStopped || frame.Request.Generation != footprintGeneration) return;
            var point = e.GetPosition(footprintChart);
            float px = (float)point.X * frame.Request.Dpi, py = (float)point.Y * frame.Request.Dpi;
            if (py < ChartPanel.Y || py > ChartPanel.Y + ChartPanel.H) { footprintTooltip.IsOpen = false; return; }
            if (FootprintShowHealth && py < ChartPanel.Y + frame.StatusHeight)
            { footprintTooltip.Content = frame.StatusText + "\n" + frame.FontNote; footprintTooltip.IsOpen = true; return; }
            long tick = FootprintBook.PriceTick(frame.Request.Scale.GetValueByY(py), TickSize);
            long bucket = FootprintBook.Bucket(tick, frame.Request.Display);
            foreach (FootprintPaintBar bar in frame.Bars)
            {
                float x = footprintChart.GetXByBarIndex(ChartBars, bar.Evidence.BarIndex);
                if (Math.Abs(px - x) > frame.Request.Width / 2) continue;
                foreach (FootprintPaintRow paint in bar.Rows)
                {
                    FootprintRow row = paint.Evidence;
                    if (row.Tick != bucket) continue;
                    string details = string.Format(CultureInfo.InvariantCulture,
                        "Bar {0}{1} | {2} to {3}\nBid {4:N0} x Ask {5:N0}\nTotal {6:N0} | Strict delta {7:+#,0;-#,0;0} | {8:0.00}%\nPOC {9}{10}",
                        bar.Evidence.BarIndex, bar.Developing ? " (developing)" : "", row.Tick * TickSize,
                        (row.Tick + frame.Request.Display - 1) * TickSize, row.Bid, row.Ask,
                        row.Total, row.Delta, row.DeltaPercent, bar.Evidence.PocTick * TickSize,
                        bar.Developing ? " (developing)" : bar.Evidence.PocTies > 1 ? " (" + bar.Evidence.PocTies + " tied rows)" : "");
                    bool bidWinner = FootprintFormatting.IsWinner(row.Bid, row.Ask, FootprintWinnerRatio);
                    bool askWinner = FootprintFormatting.IsWinner(row.Ask, row.Bid, FootprintWinnerRatio);
                    if (FootprintEmphasizeWinner && (bidWinner || askWinner))
                    {
                        long winner = bidWinner ? row.Bid : row.Ask, loser = bidWinner ? row.Ask : row.Bid;
                        string multiple = loser == 0 ? "unopposed" : (winner / (double)loser).ToString("0.00", CultureInfo.InvariantCulture) + "x";
                        details += "\nWinner: " + (bidWinner ? "Bid" : "Ask") + " (" + multiple + "; threshold " + FootprintWinnerRatio.ToString("0.##", CultureInfo.InvariantCulture) + "x)";
                    }
                    if (row.Unclassified > 0) details += "\nUnclassified " + row.Unclassified.ToString("N0", CultureInfo.InvariantCulture) + "; strict delta excludes it.";
                    if (row.Bid == 0 && row.Ask == 0 && row.Unclassified > 0) details += "\nSides unavailable, not evidence of no trading.";
                    if (row.Total > bar.TotalDenominator || Math.Max(row.Ask, row.Bid) > bar.SideDenominator) details += "\nScale saturated; exact values shown above.";
                    if (footprintFailure != null) details += "\nPreparation error: " + footprintFailure;
                    footprintTooltip.Content = details; footprintTooltip.IsOpen = true; return;
                }
                footprintTooltip.Content = "N/A - no accepted trade evidence at this price row. This is not a confirmed zero-volume row.";
                footprintTooltip.IsOpen = true; return;
            }
            for (int index = frame.Request.From; index <= frame.Request.To; index++)
                if (Math.Abs(px - footprintChart.GetXByBarIndex(ChartBars, index)) <= frame.Request.Width / 2)
                {
                    footprintTooltip.Content = "N/A - no accepted footprint evidence for this candle. Check the selected source and historical coverage.";
                    footprintTooltip.IsOpen = true; return;
                }
            footprintTooltip.IsOpen = false;
        }

		private bool TryShowQualifiedVolumeTextTooltip(MouseEventArgs e)
		{
			if (!IsDeltaColoredVolumeTextActive || footprintTooltip == null || footprintChart == null
				|| ChartPanel == null || ChartBars == null)
				return false;

			ChartScale scale = null;
			foreach (ChartScale candidate in ChartPanel.Scales)
			{
				foreach (object chartObject in candidate.ChartObjects)
					if (ReferenceEquals(chartObject, this)) { scale = candidate; break; }
				if (scale != null) break;
			}
			if (scale == null)
				foreach (ChartScale candidate in ChartPanel.Scales)
					if (candidate.ScaleJustification == ScaleJustification) { scale = candidate; break; }
			if (scale == null)
				return false;

			System.Windows.Point point = e.GetPosition(footprintChart);
			System.Windows.PresentationSource source = System.Windows.PresentationSource.FromVisual(footprintChart);
			float dpi = source == null || source.CompositionTarget == null
				? 1f : (float)source.CompositionTarget.TransformToDevice.M11;
			float px = (float)point.X * dpi;
			float py = (float)point.Y * dpi;
			if (py < ChartPanel.Y || py > ChartPanel.Y + ChartPanel.H)
				return false;

			int from = Math.Max(0, ChartBars.FromIndex);
			int to = Math.Min(ChartBars.ToIndex, ChartBars.Count - 1);
			if (to < from)
				return false;

			int barIndex = -1;
			float nearestDistance = float.MaxValue;
			for (int index = from; index <= to; index++)
			{
				float distance = Math.Abs(px - footprintChart.GetXByBarIndex(ChartBars, index));
				if (distance < nearestDistance) { nearestDistance = distance; barIndex = index; }
			}
			if (barIndex < 0)
				return false;

			float spacing = GetAverageVisibleBarSpacing(footprintChart, from, to);
			bool profilesVisible = !AutoHideProfilesWhenCompressed || spacing <= 0 || spacing >= MinBarSpacingToShowProfilesPx;
			if (!profilesVisible)
				return false;

			float barCenterX = footprintChart.GetXByBarIndex(ChartBars, barIndex);
			float candleWidth = Math.Max(CandleWidthPx, UseAbsorptionColorsWhenCompressed ? AbsorptionCandleMinWidthPx : 1f);
			float halfCandle = candleWidth / 2f;
			bool flowsRight = ProfileArrangement == CandleProfileSideArrangement.DeltaLeft_VolumeRight;
			float availableWidth = ResolveAvailableProfileWidth(footprintChart, barIndex, barCenterX, halfCandle, candleWidth, flowsRight);
			float drawWidth = ResolveSideProfileWidth(availableWidth, ProfileWidthPx,
				(float)Math.Max(0.1, Math.Min(1.0, ProfileWidthScale)), false);
			float root = flowsRight ? barCenterX + halfCandle + CandleProfileGapPx : barCenterX - halfCandle - CandleProfileGapPx;
			float left = flowsRight ? root : root - drawWidth;
			if (px < left || px > left + drawWidth)
				return false;

			int compressionTicks = ResolveVolumeCompressionTicks(scale);
			double compression = Math.Max(1, compressionTicks) * TickSize;
			double bucketPrice = Math.Floor(scale.GetValueByY(py) / compression + 0.000001) * compression;
			long bid, ask, unclassified;
			if (!TryGetQualifiedVolumeTextEvidence(barIndex, bucketPrice, compressionTicks, out bid, out ask, out unclassified))
				return false;

			if (bid + ask > 0)
			{
				long strictDelta = ask - bid;
				footprintTooltip.Content = string.Format(CultureInfo.InvariantCulture,
					"{0:N0} x {1:N0}\n{2:+#,0;-#,0;0}", bid, ask, strictDelta);
			}
			else
				footprintTooltip.Content = "N/A x N/A\nN/A";
			footprintTooltip.IsOpen = true;
			return true;
		}

        private void DisposeRetiredFootprintFrames()
        {
            if (Volatile.Read(ref footprintReaders) != 0) return;
            foreach (FootprintFrame retired in footprintRetired) retired.Dispose();
            footprintRetired.Clear();
        }

        private void StopEnhancedFootprint()
        {
            if (footprintStopped) return;
            footprintStopped = true;
            footprintCancellation.Cancel();
            footprintCancellation.Dispose();
            Interlocked.Increment(ref footprintGeneration);
            if (footprintDiagnosticsId != null) OrcaDiagnosticsCore.UnregisterInstance(footprintDiagnosticsId);
            if (footprintChart == null) return;
            footprintChart.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (footprintTimer != null) { footprintTimer.Stop(); footprintTimer.Tick -= FootprintTimerTick; footprintTimer = null; }
                footprintChart.MouseMove -= FootprintMouseMove;
                footprintChart.MouseLeave -= FootprintMouseLeave;
                if (footprintTooltip != null) { footprintTooltip.IsOpen = false; footprintTooltip = null; }
                FootprintFrame old = Interlocked.Exchange(ref footprintFrame, null);
                if (old != null) footprintRetired.Add(old);
                ReleaseStoppedFootprintResources();
            }));
        }

        private void ReleaseStoppedFootprintResources()
        {
            DisposeRetiredFootprintFrames();
            if (footprintRetired.Count != 0 && !footprintChart.Dispatcher.HasShutdownStarted)
            {
                footprintChart.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ReleaseStoppedFootprintResources));
                return;
            }
            ResetEnhancedRenderTarget();
        }
    }

    public class FootprintNamedEnumConverter : EnumConverter
    {
        private readonly string[] labels;
        protected FootprintNamedEnumConverter(Type type, params string[] labels) : base(type) { this.labels = labels; }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value != null && value.GetType() == EnumType)
            {
                int index = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                if (index >= 0 && index < labels.Length) return labels[index];
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            for (int i = 0; i < labels.Length; i++)
                if (string.Equals(value as string, labels[i], StringComparison.OrdinalIgnoreCase)) return Enum.ToObject(EnumType, i);
            return base.ConvertFrom(context, culture, value);
        }
    }

    public class FootprintScaffoldModeConverter : FootprintNamedEnumConverter
    { public FootprintScaffoldModeConverter() : base(typeof(FootprintScaffoldMode), "Off", "OHLC Spine", "Full Candle", "Hollow Body Delta") { } }

    public class FootprintCellViewConverter : FootprintNamedEnumConverter
    { public FootprintCellViewConverter() : base(typeof(FootprintCellView), "Bid x Ask", "Total Volume", "Strict Delta", "Strict Delta %") { } }

    public class FootprintScaleModeConverter : FootprintNamedEnumConverter
    {
        public FootprintScaleModeConverter() : base(typeof(FootprintScaleMode), "Per Bar", "Visible Range", "Session Global", "Fixed") { }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var indicator = context == null ? null : context.Instance as OrcaCandleVolumeProfile;
            return indicator != null && indicator.FootprintFixedVolume <= 0
                ? new StandardValuesCollection(new[] { FootprintScaleMode.PerBar, FootprintScaleMode.VisibleRange, FootprintScaleMode.SessionGlobal })
                : base.GetStandardValues(context);
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }
    }

    public class CandleProfileBidAskStyleConverter : EnumConverter
    {
        public CandleProfileBidAskStyleConverter() : base(typeof(CandleProfileBidAskStyle)) { }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is CandleProfileBidAskStyle && (CandleProfileBidAskStyle)value == CandleProfileBidAskStyle.Cluster)
                return "Cluster - Delta Heatmap";
            return base.ConvertTo(context, culture, value, destinationType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value as string == "Cluster - Delta Heatmap") return CandleProfileBidAskStyle.Cluster;
            return base.ConvertFrom(context, culture, value);
        }
    }

}
