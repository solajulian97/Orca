using System;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Test-only transport observer. Caller serializes calls; no consumer calculations.
    public sealed class OrcaProviderReaderComparison : IDisposable
    {
        private OrcaProviderStreamReader left, right;
        private OrcaStreamCursor leftCursor, rightCursor;
        public long VerifiedEvents { get { return leftCursor.Sequence; } }
        public decimal Volume { get; private set; }
        public decimal SignedVolume { get; private set; }
        public ulong Digest { get; private set; }
        public bool Failed { get; private set; }
        private bool disposed;
        private const int BatchLimit = 256;

        private OrcaProviderReaderComparison() { Digest = 14695981039346656037UL; }
        public static OrcaProviderReaderComparison Create(OrcaProviderFeedLifetime owner, OrcaProviderSourceIdentity identity)
        {
            var comparison = new OrcaProviderReaderComparison();
            try
            {
                if (owner.OpenReader(identity, out comparison.left) != OrcaProviderReaderStatus.Opened
                    || owner.OpenReader(identity, out comparison.right) != OrcaProviderReaderStatus.Opened)
                    throw new InvalidOperationException("Comparison reader admission failed.");
                comparison.leftCursor = comparison.left.FirstAvailable;
                comparison.rightCursor = comparison.right.FirstAvailable;
                if (comparison.leftCursor.Generation != comparison.rightCursor.Generation
                    || comparison.leftCursor.Sequence != 0 || comparison.rightCursor.Sequence != 0)
                    throw new InvalidOperationException("Comparison must start before source prefix eviction.");
                return comparison;
            }
            catch { comparison.Dispose(); throw; }
        }

        // At most 256 events per reader per call; never rebuild the whole history here.
        public bool DrainThrough(long publishedExclusive)
        {
            if (disposed) throw new ObjectDisposedException("OrcaProviderReaderComparison");
            if (Failed) throw new InvalidOperationException("Comparison previously failed; recreate the probe.");
            try
            {
                if (publishedExclusive < VerifiedEvents) throw new ArgumentOutOfRangeException("publishedExclusive");
                int requested = (int)Math.Min(BatchLimit, publishedExclusive - VerifiedEvents);
                if (requested == 0) return true;
                using (var a = left.Read(leftCursor, requested))
                using (var b = right.Read(rightCursor, requested))
                {
                    if (a.Status != OrcaStreamReadStatus.Ready || b.Status != OrcaStreamReadStatus.Ready)
                        throw new InvalidOperationException("Comparison read failed: " + a.Status + " / " + b.Status);
                    if (a.Events.Count != requested || b.Events.Count != requested
                        || a.Next.Generation != b.Next.Generation || a.Next.Sequence != b.Next.Sequence)
                        throw new InvalidOperationException("Comparison interval mismatch or incomplete publication.");
                    decimal volume = Volume, signed = SignedVolume;
                    ulong digest = Digest;
                    for (int i = 0; i < requested; i++)
                    {
                        var x = a.Events[i]; var y = b.Events[i];
                        if (x.Time.Ticks != y.Time.Ticks || x.Time.Kind != y.Time.Kind
                            || BitConverter.DoubleToInt64Bits(x.Price) != BitConverter.DoubleToInt64Bits(y.Price)
                            || x.Volume != y.Volume || x.SignedVolume != y.SignedVolume || x.Classification != y.Classification)
                            throw new InvalidOperationException("Comparison payload mismatch.");
                        volume = checked(volume + x.Volume); signed = checked(signed + x.SignedVolume);
                        digest = Mix(digest, unchecked((ulong)x.Time.Ticks));
                        digest = Mix(digest, (ulong)x.Time.Kind);
                        digest = Mix(digest, unchecked((ulong)BitConverter.DoubleToInt64Bits(x.Price)));
                        digest = Mix(digest, unchecked((ulong)x.Volume));
                        digest = Mix(digest, unchecked((ulong)x.SignedVolume));
                        digest = Mix(digest, (ulong)x.Classification);
                    }
                    Volume = volume; SignedVolume = signed; Digest = digest;
                    leftCursor = a.Next; rightCursor = b.Next;
                }
                return VerifiedEvents == publishedExclusive;
            }
            catch { Failed = true; throw; }
        }
        private static ulong Mix(ulong digest, ulong value)
        { unchecked { for (int i = 0; i < 8; i++) { digest = (digest ^ (byte)value) * 1099511628211UL; value >>= 8; } return digest; } }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (left != null) left.Dispose(); if (right != null) right.Dispose();
            left = null; right = null;
        }
    }
}
