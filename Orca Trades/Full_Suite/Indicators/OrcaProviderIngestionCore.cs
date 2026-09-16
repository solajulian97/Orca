using System;

namespace NinjaTrader.NinjaScript.Indicators
{
    public enum OrcaProviderClassificationPolicy { TickDirection, TradeQuoteThenTickDirection }

    // Pure adapter for one ordered callback source. The caller owns the publisher lease.
    // Quote availability means supplied trade-time quotes, NOT verified exchange provenance.
    public sealed class OrcaProviderIngestion
    {
        private readonly object sync = new object();
        private readonly OrcaProviderPublisherLease publisher;
        private readonly OrcaProviderClassificationPolicy policy;
        private double previousPrice = double.NaN;
        private int previousDirection;

        public OrcaProviderIngestion(OrcaProviderPublisherLease publisher,
            OrcaProviderClassificationPolicy policy, bool hasHistoricalPass)
        {
            if (publisher == null) throw new ArgumentNullException("publisher");
            if (policy != OrcaProviderClassificationPolicy.TickDirection
                && policy != OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection)
                throw new ArgumentOutOfRangeException("policy");
            string policyId = policy == OrcaProviderClassificationPolicy.TickDirection ? "tickdirection:v1" : "bidask-fallback:v1";
            if (publisher.Key.ClassificationPolicy != policyId || publisher.Key.TimeBasis != "UTC")
                throw new ArgumentException("Publisher identity must match the adapter's versioned classification policy and UTC time basis.");
            this.publisher = publisher; this.policy = policy;
            if (hasHistoricalPass) publisher.BeginHistoricalPass();
            else publisher.BeginLiveOnly();
        }

        public long OnTrade(bool historical, DateTime timeUtc, double price, long volume,
            double bid, double ask, bool tradeQuotesAvailable, bool resetClassification)
        {
            lock (sync)
            {
                double prior = resetClassification ? double.NaN : previousPrice;
                int direction = resetClassification ? 0 : previousDirection;
                OrcaStreamClassification classification = OrcaStreamClassification.Unknown;
                bool usableQuotes = policy == OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection
                    && tradeQuotesAvailable && Finite(bid) && Finite(ask) && ask > bid;
                if (usableQuotes && (price >= ask || price <= bid))
                {
                    direction = price >= ask ? 1 : -1;
                    classification = OrcaStreamClassification.BidAsk;
                }
                else
                {
                    if (!double.IsNaN(prior) && price != prior) direction = price > prior ? 1 : -1;
                    if (direction != 0) classification = OrcaStreamClassification.TickDirection;
                }
                var tick = new OrcaStreamTick(timeUtc, price, volume, direction * volume, classification);
                long sequence = historical ? publisher.AppendHistorical(tick) : publisher.AppendLive(tick);
                // Invalid input or rejected callback must not advance classifier state.
                previousPrice = price; previousDirection = direction;
                return sequence;
            }
        }

        public void CompleteHistoricalPassAndBeginLive()
        { lock (sync) publisher.CompleteHistoricalPassAndBeginLive(); }

        public void MarkFault(OrcaStreamFault reason)
        { lock (sync) publisher.MarkFault(reason); }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
