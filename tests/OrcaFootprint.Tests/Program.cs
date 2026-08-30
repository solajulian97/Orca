using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using NinjaTrader.NinjaScript.Indicators;

internal static class Program
{
    private static int checks;
    private static void Equal<T>(T expected, T actual, string name)
    { checks++; if (!object.Equals(expected, actual)) throw new Exception(name + ": expected " + expected + ", got " + actual); }
    private static FootprintViewportSnapshot Capture(FootprintBook book, FootprintScaleMode mode = FootprintScaleMode.PerBar, int size = 1)
    { return book.Capture(0, 4, 1, 1, size, 1, mode, 100); }
    public static void Main()
    {
        var time = new DateTime(2026, 8, 28, 9, 30, 0);
        var book = new FootprintBook();
        book.Observe(0, 100, 1200, 1, 10, true, time, FootprintDataQuality.TradeQuoteUnverified);
        book.Observe(0, 100, 1200, 1, 10, true, time, FootprintDataQuality.TradeQuoteUnverified);
        book.Observe(0, 100, 200, -1, 10, true, time, FootprintDataQuality.CachedQuote);
        book.Observe(0, 100, 50, 0, 10, true, time, FootprintDataQuality.SecondarySeries);
        var first = Capture(book).Bars[0];
        Equal(2400L, first.Rows[0].Ask, "identical trades retained, large sizes intact");
        Equal(2650L, first.Total, "volume conservation");
        Equal(2200L, first.Rows[0].Delta, "strict delta excludes unknown");
        Equal(true, (first.Quality & FootprintDataQuality.Unclassified) != 0, "quality");
        book.Observe(0, 101, 2650, -1, 10, true, time, FootprintDataQuality.None);
        var tied = Capture(book).Bars[0];
        Equal(100L, tied.PocTick, "lowest tied POC"); Equal(2, tied.PocTies, "tie count");
        Equal(2650L, first.Total, "old snapshot immutable");
        var grouped = Capture(book, FootprintScaleMode.PerBar, 4).Bars[0];
        Equal(tied.PocTick, grouped.PocTick, "POC independent of display compression");
        Equal(tied.Total, grouped.Total, "grouped conservation");
        Equal(-4L, FootprintBook.Bucket(-1, 4), "negative floor");
        Equal(400L, FootprintBook.PriceTick(100, .25), "tick conversion");
        book.Observe(1, 100, 10, 1, 10, true, time, FootprintDataQuality.None);
        book.Observe(2, 100, 9999, 1, 11, false, time, FootprintDataQuality.None);
        Equal(10L, Capture(book, FootprintScaleMode.VisibleRange).SideDenominators[0], "horizontal viewport only");
        Equal(2650L, Capture(book, FootprintScaleMode.SessionGlobal).SideDenominators[1], "session common maximum");
        Equal(9999L, Capture(book, FootprintScaleMode.SessionGlobal).SideDenominators[2], "sessions isolated");
        Equal(100L, Capture(book, FootprintScaleMode.Fixed).SideDenominators[0], "fixed denominator");
        Equal(1.0, FootprintFormatting.Fraction(500, 100), "fixed saturation");
        Equal(0, book.Capture(0, 2, 0, 2, 1, 1, FootprintScaleMode.Fixed, 0).Bars.Count, "unconfigured fixed unavailable");
        book.Observe(1, 100, 5000, 1, 10, true, time.AddSeconds(-1), FootprintDataQuality.None);
        var updated = Capture(book, FootprintScaleMode.SessionGlobal);
        Equal(5010L, updated.SideDenominators[0], "developing session updates earlier bars");
        Equal(true, (updated.Bars[1].Quality & FootprintDataQuality.OutOfOrder) != 0, "out of order disclosed");
        Capture(book, FootprintScaleMode.PerBar, 2); Capture(book, FootprintScaleMode.PerBar, 8);
        Equal(2, book.VariantCount, "two compression variants");
        Equal("0", FootprintFormatting.Number(0, false), "zero");
        Equal("1.2K", FootprintFormatting.Number(1200, true), "compact");
        Equal("0.0%", FootprintFormatting.Value(new FootprintRow(0, 0, 0, 0), FootprintCellView.DeltaPercent, false), "zero denominator display");
        Equal(false, FootprintFormatting.Fits(30, 12, 29, 14), "width suppresses text");
        Equal(false, FootprintFormatting.Fits(20, 15, 30, 14), "height suppresses text");
        Equal(0L, new FootprintRow(1, 123, 123, 0).Delta, "known zero delta has traded volume");
        Equal(0.0, new FootprintRow(1, 123, 123, 0).DeltaPercent, "known zero percentage");
        Equal(50.0, new FootprintRow(1, 0, 100, 100).DeltaPercent, "unknown included in percent denominator");
        Equal("N/A - no footprint trade evidence", new FootprintBook().Capture(0, 1, 0, 1, 1, 1, FootprintScaleMode.PerBar, 0).Status, "missing is not zero");
        var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        bool cancelled = false;
        try { book.Capture(0, 3, 0, 3, 1, 1, FootprintScaleMode.PerBar, 0, cancellation.Token); }
        catch (OperationCanceledException) { cancelled = true; }
        Equal(true, cancelled, "cancelled preparation rejected");
        Equal(5010L, Capture(book, FootprintScaleMode.SessionGlobal).SideDenominators[0], "capture recovers after cancellation");
        var bounded = new FootprintBook(6144);
        for (int i = 0; i < 100; i++) bounded.Observe(0, i, 1, 1, 10, false, time, FootprintDataQuality.None);
        bounded.Capture(0, 0, 0, 0, 1, 1, FootprintScaleMode.PerBar, 0);
        bounded.Capture(0, 0, 0, 0, 2, 1, FootprintScaleMode.PerBar, 0);
        Equal(1, bounded.VariantCount, "LRU variant evicted by byte budget");
        Equal(true, bounded.CachedBytes <= 6144, "small cache remains bounded");
        var evicted = bounded.Capture(0, 0, 0, 0, 1, 1, FootprintScaleMode.PerBar, 0);
        Equal(100L, evicted.Bars[0].Total, "eviction never deletes evidence");
        bounded.Capture(1, 1, 1, 1, 1, 1, FootprintScaleMode.PerBar, 0);
        Equal(true, bounded.CachedBytes < 1024, "offscreen row snapshots evicted");
        var exhausted = new FootprintBook(1024);
        for (int i = 0; i < 100; i++) exhausted.Observe(0, i, 1, 1, 10, false, time, FootprintDataQuality.None);
        Equal(0, exhausted.Capture(0, 0, 0, 0, 1, 1, FootprintScaleMode.PerBar, 0).Bars.Count, "oversized viewport reported unavailable");
        Equal(100L, exhausted.Capture(0, 0, 0, 0, 100, 1, FootprintScaleMode.PerBar, 0).Bars[0].Total, "budget failure keeps underlying data");
        var age = new FootprintBook();
        age.Observe(0, 100, 1, 1, 10, false, time, FootprintDataQuality.CachedQuote, 10000);
        age.Observe(0, 101, 1, -1, 10, false, time, FootprintDataQuality.SecondarySeries);
        var qualityRow = age.Capture(0, 0, 0, 0, 4, 1, FootprintScaleMode.PerBar, 0).Bars[0].Rows[0];
        Equal(10000L, qualityRow.MaxQuoteAgeTicks, "maximum known age retained");
        Equal(true, (qualityRow.Quality & FootprintDataQuality.QuoteAgeUnknown) != 0, "mixed unknown age retained across aggregation");
        long[] denominators = { 123 };
        var frozenViewport = new FootprintViewportSnapshot(1, new[] { first }, denominators, denominators, "");
        denominators[0] = 999;
        Equal(123L, frozenViewport.SideDenominators[0], "public snapshot does not alias caller arrays");
        GoldenAcceptedStream(time);
        var random = new Random(7); var stress = new FootprintBook(); long total = 0;
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < 200000; i++)
        { int v = random.Next(1, 2000); total += v; stress.Observe(i / 100, random.Next(30000, 30100), v, i % 3 - 1, i / 100000, false, time, FootprintDataQuality.None); }
        watch.Stop(); Console.WriteLine("200000 observations: " + watch.ElapsedMilliseconds + " ms");
        var all = stress.Capture(0, 1999, 0, 1999, 4, 1, FootprintScaleMode.SessionGlobal, 0);
        long actual = 0; foreach (var bar in all.Bars) actual += bar.Total;
        Equal(total, actual, "stress conservation"); Equal(true, stress.CachedBytes <= FootprintBook.CacheBudgetBytes, "cache bound");
        var timings = new double[50];
        for (int i = 0; i < timings.Length; i++)
        {
            watch.Restart(); stress.Capture(1200, 1799, 1400, 1599, 4, 1, FootprintScaleMode.SessionGlobal, 0); watch.Stop();
            timings[i] = watch.Elapsed.TotalMilliseconds;
        }
        Array.Sort(timings);
        Console.WriteLine("Warm model preparation (200 visible, 600 cached): p50=" + timings[25].ToString("F3") + "ms p95=" + timings[47].ToString("F3") + "ms; derived bytes=" + stress.CachedBytes);
        Console.WriteLine("PASS: " + checks + " checks");
    }

    private static void GoldenAcceptedStream(DateTime time)
    {
        var model = new FootprintBook(); var expected = new Dictionary<long, long[]>(); var random = new Random(91);
        long total = 0;
        for (int i = 0; i < 5000; i++)
        {
            long tick = random.Next(100, 200), volume = random.Next(1, 100000);
            int side = random.Next(3) - 1;
            long[] row;
            if (!expected.TryGetValue(tick, out row)) expected[tick] = row = new long[3];
            row[side + 1] += volume; total += volume;
            model.Observe(0, tick, volume, side, 10, true, time, FootprintDataQuality.TradeQuoteUnverified);
            if (i % 100 == 0) Equal(total, model.Capture(0, 0, 0, 0, 1, 4, FootprintScaleMode.PerBar, 0).Bars[0].Total, "identical event prefix");
        }
        var bar = model.Capture(0, 0, 0, 0, 1, 4, FootprintScaleMode.PerBar, 0).Bars[0];
        foreach (var row in bar.Rows)
        {
            Equal(expected[row.Tick][0], row.Bid, "golden strict bid");
            Equal(expected[row.Tick][1], row.Unclassified, "golden unknown");
            Equal(expected[row.Tick][2], row.Ask, "golden strict ask");
        }
        Equal(5000L, bar.Sequence, "local sequence includes same-timestamp events");
    }
}
