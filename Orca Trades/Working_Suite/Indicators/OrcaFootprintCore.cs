using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;

namespace NinjaTrader.NinjaScript.Indicators
{
    public enum FootprintScaleMode { PerBar, VisibleRange, SessionGlobal, Fixed }
    public enum FootprintScaffoldMode { Off, OhlcSpine, OhlcSpineAndBody }
    public enum FootprintNumberFormat { Full, Compact, Auto }
    public enum FootprintCellView { BidAsk, Total, Delta, DeltaPercent }

    [Flags]
    public enum FootprintDataQuality
    {
        None = 0, TradeQuoteUnverified = 1, CachedQuote = 2, SecondarySeries = 4,
        Unclassified = 8, OutOfOrder = 16, AttributionUnverified = 32, ConnectionGap = 64, QuoteAgeUnknown = 128
    }

    public struct FootprintRow
    {
        public readonly long Tick, Bid, Ask, Unclassified;
        public readonly FootprintDataQuality Quality;
        public readonly long MaxQuoteAgeTicks;
        public long Total { get { return checked(Bid + Ask + Unclassified); } }
        public long Delta { get { return Ask - Bid; } }
        public double DeltaPercent { get { return Total == 0 ? 0 : 100.0 * Delta / Total; } }
        public FootprintRow(long tick, long bid, long ask, long unclassified,
            FootprintDataQuality quality = FootprintDataQuality.None, long maxQuoteAgeTicks = -1)
        {
            if (bid < 0 || ask < 0 || unclassified < 0) throw new ArgumentOutOfRangeException("bid", "Evidence volumes cannot be negative.");
            Tick = tick; Bid = bid; Ask = ask; Unclassified = unclassified; Quality = quality; MaxQuoteAgeTicks = maxQuoteAgeTicks;
        }
    }

    public sealed class FootprintBarSnapshot
    {
        public readonly int BarIndex, DisplayTicks, AnalysisTicks, PocTies;
        public readonly long Revision, SessionId, Sequence, PocTick, Total, MaxSide, MaxTotal;
        public readonly DateTime LastEventTime;
        public readonly bool PartialSession;
        public readonly FootprintDataQuality Quality;
        public readonly ReadOnlyCollection<FootprintRow> Rows;
        internal FootprintBarSnapshot(int index, long revision, long session, bool partial,
            long sequence, DateTime time, FootprintDataQuality quality, FootprintRow[] rows,
            int displayTicks, int analysisTicks, long poc, int ties)
        {
            BarIndex = index; Revision = revision; SessionId = session; PartialSession = partial;
            Sequence = sequence; LastEventTime = time; Quality = quality;
            Rows = Array.AsReadOnly(rows); DisplayTicks = displayTicks; AnalysisTicks = analysisTicks;
            PocTick = poc; PocTies = ties;
            foreach (FootprintRow row in rows)
            { Total = checked(Total + row.Total); MaxSide = Math.Max(MaxSide, Math.Max(row.Bid, row.Ask)); MaxTotal = Math.Max(MaxTotal, row.Total); }
        }
    }

    public sealed class FootprintViewportSnapshot
    {
        public readonly long Revision;
        public readonly ReadOnlyCollection<FootprintBarSnapshot> Bars;
        public readonly ReadOnlyCollection<long> SideDenominators, TotalDenominators;
        public readonly string Status;
        public FootprintViewportSnapshot(long revision, FootprintBarSnapshot[] bars, long[] sides, long[] totals, string status)
        {
            Revision = revision;
            Bars = Array.AsReadOnly((FootprintBarSnapshot[])bars.Clone());
            SideDenominators = Array.AsReadOnly((long[])sides.Clone());
            TotalDenominators = Array.AsReadOnly((long[])totals.Clone());
            Status = status;
        }
    }

    // Short ingestion locks; one preparation worker owns all derived state.
    public sealed class FootprintBook
    {
        private sealed class Bar
        {
            public readonly Dictionary<long, FootprintRow> Rows = new Dictionary<long, FootprintRow>();
            public long Revision, Session, Sequence;
            public DateTime Time;
            public bool Partial;
            public FootprintDataQuality Quality;
        }
        private sealed class Summary
        {
            public long Revision, Session, Side, Total;
        }
        private sealed class SessionMax
        {
            public readonly SortedSet<Tuple<long, int>> Sides = new SortedSet<Tuple<long, int>>();
            public readonly SortedSet<Tuple<long, int>> Totals = new SortedSet<Tuple<long, int>>();
        }
        private sealed class Variant
        {
            public int Display, Analysis;
            public readonly Dictionary<int, Summary> Summaries = new Dictionary<int, Summary>();
            public readonly Dictionary<long, SessionMax> Sessions = new Dictionary<long, SessionMax>();
            public readonly Dictionary<int, FootprintBarSnapshot> Snapshots = new Dictionary<int, FootprintBarSnapshot>();
        }
        private readonly Dictionary<int, Bar> bars = new Dictionary<int, Bar>();
        private readonly Dictionary<int, Bar> liveBars = new Dictionary<int, Bar>();
        private readonly HashSet<int> liveDirty = new HashSet<int>();
        private readonly object sync = new object();
        private readonly HashSet<int> dirty = new HashSet<int>();
        private readonly List<Variant> variants = new List<Variant>(2);
        private long revision, sequence;
        private DateTime lastEventTime;
        public long Revision { get { return System.Threading.Interlocked.Read(ref revision); } }
        public long CachedBytes { get; private set; }
        public const long CacheBudgetBytes = 32L * 1024 * 1024;
        // Reserve half for prepared text layouts, projected rows, and retiring frames in the host.
        public const long ModelCacheBudgetBytes = CacheBudgetBytes / 2;
        private readonly long budget;
        public int VariantCount { get { return variants.Count; } }
        public FootprintBook(long derivedBudgetBytes = ModelCacheBudgetBytes)
        {
            if (derivedBudgetBytes < 1024 || derivedBudgetBytes > ModelCacheBudgetBytes) throw new ArgumentOutOfRangeException("derivedBudgetBytes");
            budget = derivedBudgetBytes;
        }

        public static long PriceTick(double price, double tickSize)
        {
            if (double.IsNaN(price) || double.IsInfinity(price) || tickSize <= 0 || double.IsNaN(tickSize) || double.IsInfinity(tickSize))
                throw new ArgumentOutOfRangeException("price");
            return checked((long)Math.Round(price / tickSize, MidpointRounding.AwayFromZero));
        }
        public static long Bucket(long tick, int size)
        {
            if (size < 1) throw new ArgumentOutOfRangeException("size");
            long quotient = tick / size;
            if (tick < 0 && tick % size != 0) quotient--;
            return checked(quotient * size);
        }

        public void Observe(int index, long tick, long volume, int strictSide, long session,
            bool partialSession, DateTime time, FootprintDataQuality quality, long quoteAgeTicks = -1)
        {
            lock (sync) ObserveCore(index, tick, volume, strictSide, session, partialSession, time, quality, quoteAgeTicks);
        }
        private void ObserveCore(int index, long tick, long volume, int strictSide, long session,
            bool partialSession, DateTime time, FootprintDataQuality quality, long quoteAgeTicks)
        {
            if (index < 0 || volume <= 0) return;
            if (lastEventTime != DateTime.MinValue && time < lastEventTime) quality |= FootprintDataQuality.OutOfOrder;
            lastEventTime = time;
            Bar bar;
            if (!liveBars.TryGetValue(index, out bar))
            { bar = new Bar { Session = session, Partial = partialSession }; liveBars.Add(index, bar); }
            if (bar.Time != DateTime.MinValue && time < bar.Time) quality |= FootprintDataQuality.OutOfOrder;
            if (bar.Session != session) quality |= FootprintDataQuality.AttributionUnverified;
            if (quoteAgeTicks < 0) quality |= FootprintDataQuality.QuoteAgeUnknown;
            if (strictSide == 0) quality |= FootprintDataQuality.Unclassified;
            FootprintRow old;
            bar.Rows.TryGetValue(tick, out old);
            bar.Rows[tick] = new FootprintRow(tick,
                checked(old.Bid + (strictSide < 0 ? volume : 0)),
                checked(old.Ask + (strictSide > 0 ? volume : 0)),
                checked(old.Unclassified + (strictSide == 0 ? volume : 0)), old.Quality | quality,
                Math.Max(old.Total == 0 ? -1 : old.MaxQuoteAgeTicks, quoteAgeTicks));
            bar.Quality |= quality | (strictSide == 0 ? FootprintDataQuality.Unclassified : FootprintDataQuality.None);
            bar.Time = time; bar.Sequence = ++sequence; bar.Revision = ++revision;
            liveDirty.Add(index);
        }

        private void FreezeDirtyBars(CancellationToken cancellation)
        {
            int[] indices;
            lock (sync) { indices = new int[liveDirty.Count]; liveDirty.CopyTo(indices); }
            foreach (int index in indices)
            {
                cancellation.ThrowIfCancellationRequested();
                // Copy only one dirty candle while ingestion is excluded; aggregate after releasing it.
                Bar copy;
                lock (sync)
                {
                    Bar source = liveBars[index];
                    copy = new Bar { Revision = source.Revision, Session = source.Session,
                        Sequence = source.Sequence, Time = source.Time, Partial = source.Partial, Quality = source.Quality };
                    foreach (var row in source.Rows) copy.Rows.Add(row.Key, row.Value);
                    liveDirty.Remove(index);
                }
                bars[index] = copy; dirty.Add(index);
            }
        }

        private static FootprintRow[] Aggregate(Bar bar, int ticks)
        {
            var grouped = new SortedDictionary<long, FootprintRow>();
            foreach (FootprintRow row in bar.Rows.Values)
            {
                long tick = Bucket(row.Tick, ticks);
                FootprintRow old;
                grouped.TryGetValue(tick, out old);
                grouped[tick] = new FootprintRow(tick, checked(old.Bid + row.Bid), checked(old.Ask + row.Ask), checked(old.Unclassified + row.Unclassified),
                    old.Quality | row.Quality, Math.Max(old.Total == 0 ? -1 : old.MaxQuoteAgeTicks, row.MaxQuoteAgeTicks));
            }
            var result = new FootprintRow[grouped.Count];
            grouped.Values.CopyTo(result, 0);
            return result;
        }
        private static FootprintBarSnapshot Build(int index, Bar bar, Variant variant)
        {
            FootprintRow[] rows = Aggregate(bar, variant.Display);
            FootprintRow[] analysis = variant.Display == variant.Analysis ? rows : Aggregate(bar, variant.Analysis);
            long max = -1, poc = 0;
            int ties = 0;
            foreach (FootprintRow row in analysis)
            {
                if (row.Total > max) { max = row.Total; poc = row.Tick; ties = 1; }
                else if (row.Total == max) ties++;
            }
            return new FootprintBarSnapshot(index, bar.Revision, bar.Session, bar.Partial, bar.Sequence,
                bar.Time, bar.Quality, rows, variant.Display, variant.Analysis, poc, ties);
        }
        private static void UpdateSummary(int index, Bar bar, Variant variant, FootprintRow[] rows)
        {
            Summary old;
            if (variant.Summaries.TryGetValue(index, out old))
            {
                SessionMax prior = variant.Sessions[old.Session];
                prior.Sides.Remove(Tuple.Create(old.Side, index)); prior.Totals.Remove(Tuple.Create(old.Total, index));
            }
            long side = 0, total = 0;
            foreach (FootprintRow row in rows) { side = Math.Max(side, Math.Max(row.Ask, row.Bid)); total = Math.Max(total, row.Total); }
            SessionMax session;
            if (!variant.Sessions.TryGetValue(bar.Session, out session))
            { session = new SessionMax(); variant.Sessions.Add(bar.Session, session); }
            session.Sides.Add(Tuple.Create(side, index)); session.Totals.Add(Tuple.Create(total, index));
            variant.Summaries[index] = new Summary { Revision = bar.Revision, Session = bar.Session, Side = side, Total = total };
        }
        private Variant GetVariant(int display, int analysis, CancellationToken cancellation)
        {
            foreach (Variant current in variants)
                foreach (int index in dirty)
                {
                    cancellation.ThrowIfCancellationRequested();
                    UpdateSummary(index, bars[index], current, Aggregate(bars[index], current.Display));
                }
            dirty.Clear();
            for (int i = 0; i < variants.Count; i++)
                if (variants[i].Display == display && variants[i].Analysis == analysis)
                {
                    Variant hit = variants[i]; variants.RemoveAt(i); variants.Add(hit); return hit;
                }
            var variant = new Variant { Display = display, Analysis = analysis };
            // Initial or compression-change preparation, never called from a trade or render callback.
            foreach (var pair in bars)
            {
                cancellation.ThrowIfCancellationRequested();
                UpdateSummary(pair.Key, pair.Value, variant, Aggregate(pair.Value, display));
            }
            if (variants.Count == 2) variants.RemoveAt(0);
            variants.Add(variant);
            return variant;
        }

        public FootprintViewportSnapshot Capture(int from, int to, int visibleFrom, int visibleTo,
            int displayTicks, int analysisTicks, FootprintScaleMode mode, long fixedVolume,
            CancellationToken cancellation = default(CancellationToken))
        {
            cancellation.ThrowIfCancellationRequested();
            if (displayTicks < 1 || analysisTicks < 1) throw new ArgumentOutOfRangeException("displayTicks");
            if (mode == FootprintScaleMode.Fixed && fixedVolume <= 0)
                return Empty("N/A - configure Fixed Scale Volume");
            long capturedRevision = Revision;
            FreezeDirtyBars(cancellation);
            Variant variant = GetVariant(displayTicks, analysisTicks, cancellation);
            if (variant.Summaries.Count * 320L > budget)
            { variants.Clear(); CachedBytes = 0; return Empty("N/A - derived summary budget exceeded; reduce loaded history"); }
            long visibleSide = 0, visibleTotal = 0;
            for (int i = visibleFrom; i <= visibleTo; i++)
            {
                Summary sum;
                if (!variant.Summaries.TryGetValue(i, out sum)) continue;
                visibleSide = Math.Max(visibleSide, sum.Side); visibleTotal = Math.Max(visibleTotal, sum.Total);
            }
            foreach (Variant cached in variants)
            {
                var remove = new List<int>();
                foreach (int key in cached.Snapshots.Keys) if (key < from || key > to) remove.Add(key);
                foreach (int key in remove) cached.Snapshots.Remove(key);
            }
            var output = new List<FootprintBarSnapshot>();
            var sides = new List<long>(); var totals = new List<long>();
            long outputBytes = 0;
            for (int i = from; i <= to; i++)
            {
                cancellation.ThrowIfCancellationRequested();
                Bar bar;
                if (!bars.TryGetValue(i, out bar)) continue;
                FootprintBarSnapshot snapshot;
                if (!variant.Snapshots.TryGetValue(i, out snapshot) || snapshot.Revision != bar.Revision)
                { snapshot = Build(i, bar, variant); variant.Snapshots[i] = snapshot; }
                outputBytes += 160 + snapshot.Rows.Count * 48L;
                if (outputBytes + variant.Summaries.Count * 320L > budget)
                { variants.Clear(); CachedBytes = 0; return Empty("N/A - derived row budget exceeded; reduce visible bars"); }
                long side = snapshot.MaxSide, total = snapshot.MaxTotal;
                if (mode == FootprintScaleMode.VisibleRange) { side = visibleSide; total = visibleTotal; }
                else if (mode == FootprintScaleMode.SessionGlobal)
                { SessionMax session = variant.Sessions[bar.Session]; side = session.Sides.Max.Item1; total = session.Totals.Max.Item1; }
                else if (mode == FootprintScaleMode.Fixed) side = total = fixedVolume;
                output.Add(snapshot); sides.Add(Math.Max(1, side)); totals.Add(Math.Max(1, total));
            }
            RecountCache();
            if (CachedBytes > budget && variants.Count == 2)
            { variants.RemoveAt(0); RecountCache(); }
            return new FootprintViewportSnapshot(capturedRevision, output.ToArray(), sides.ToArray(), totals.ToArray(), output.Count == 0 ? "N/A - no footprint trade evidence" : string.Empty);
        }
        private FootprintViewportSnapshot Empty(string status)
        { return new FootprintViewportSnapshot(revision, new FootprintBarSnapshot[0], new long[0], new long[0], status); }
        private void RecountCache()
        {
            CachedBytes = 0;
            foreach (Variant cached in variants)
            {
                CachedBytes += cached.Summaries.Count * 320L;
                foreach (FootprintBarSnapshot s in cached.Snapshots.Values) CachedBytes += 160 + s.Rows.Count * 48L;
            }
        }
    }

    public static class FootprintFormatting
    {
        public static double Fraction(long value, long denominator)
        { return denominator <= 0 ? 0 : Math.Max(0, Math.Min(1, value / (double)denominator)); }
        public static string Number(long value, bool compact)
        {
            if (!compact) return value.ToString("N0", CultureInfo.InvariantCulture);
            double v = value, magnitude = Math.Abs(v);
            if (magnitude < 1000) return value.ToString(CultureInfo.InvariantCulture);
            double divisor = magnitude >= 1000000000 ? 1000000000 : magnitude >= 1000000 ? 1000000 : 1000;
            string suffix = divisor == 1000000000 ? "B" : divisor == 1000000 ? "M" : "K";
            return (v / divisor).ToString("0.##", CultureInfo.InvariantCulture) + suffix;
        }
        public static string Value(FootprintRow row, FootprintCellView view, bool compact)
        {
            if (view == FootprintCellView.Total) return Number(row.Total, compact);
            if (view == FootprintCellView.DeltaPercent) return row.DeltaPercent.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "%";
            return (row.Delta > 0 ? "+" : "") + Number(row.Delta, compact);
        }
        public static bool Fits(float measuredWidth, float measuredHeight, float width, float height)
        { return measuredWidth <= width && measuredHeight <= height && width > 0 && height > 0; }
        public static bool IsWinner(long dominant, long opposing, double ratio)
        {
            if (dominant <= 0 || opposing < 0 || ratio < 1 || double.IsNaN(ratio) || double.IsInfinity(ratio)) return false;
            return dominant > opposing && (opposing == 0 || dominant >= opposing * ratio);
        }
    }
}
