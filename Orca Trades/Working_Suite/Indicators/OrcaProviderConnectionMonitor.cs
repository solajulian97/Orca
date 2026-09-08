using System;
using System.Threading;
using NinjaTrader.Cbi;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Owns one documented platform-level status subscription for one private feed lifetime.
    // Holds no chart, indicator, account, Connection object or consumer callback.
    public sealed class OrcaProviderConnectionMonitor : IDisposable
    {
        private readonly object lifecycleSync = new object();
        private readonly OrcaProviderFeedLifetime owner;
        private bool subscriptionAttempted;
        private int armed;
        private int disposed;
        private int invalidated;
        private int closeFailed;
        private Notification firstNotification;

        public OrcaProviderConnectionMonitor(OrcaProviderFeedLifetime owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            this.owner = owner;
        }

        public bool IsArmed { get { return Volatile.Read(ref armed) != 0 && Volatile.Read(ref disposed) == 0; } }
        public bool IsInvalidated { get { return Volatile.Read(ref invalidated) != 0; } }

        // Caller stores this object before Attach so a partial add/remove failure can be retried.
        public void Attach()
        {
            lock (lifecycleSync)
            {
                if (disposed != 0) throw new ObjectDisposedException("OrcaProviderConnectionMonitor");
                if (subscriptionAttempted) throw new InvalidOperationException("Connection monitor already attached or attachment failed.");
                subscriptionAttempted = true;
                try
                {
                    Connection.ConnectionStatusUpdate += OnPlatformStatus;
                    Volatile.Write(ref armed, 1);
                }
                catch
                {
                    Invalidate(null);
                    throw;
                }
            }
        }

        private void OnPlatformStatus(object sender, ConnectionStatusEventArgs e)
        {
            if (Volatile.Read(ref disposed) == 0) Invalidate(e);
        }

        private void Invalidate(ConnectionStatusEventArgs e)
        {
            if (Interlocked.CompareExchange(ref invalidated, 1, 0) != 0) return;
            try
            {
                if (e != null) Volatile.Write(ref firstNotification, new Notification(e));
            }
            finally
            {
                // Only bounded core registry/buffer locks; no probe lock, UI dispatch,
                // Print, platform unsubscribe or arbitrary consumer code on this callback.
                try { owner.Dispose(); }
                catch { Volatile.Write(ref closeFailed, 1); }
            }
        }

        public string DescribeInvalidation()
        {
            var notification = Volatile.Read(ref firstNotification);
            string detail = notification == null ? "notification/attachment invalidated; details unavailable"
                : "utc=" + notification.ReceiptUtc.ToString("O")
                    + " price=" + notification.PreviousPrice + "->" + notification.Price
                    + " order=" + notification.PreviousOrder + "->" + notification.Order;
            return detail + (Volatile.Read(ref closeFailed) == 0 ? "" : "; core closure needs retry");
        }

        public void Dispose()
        {
            lock (lifecycleSync)
            {
                Volatile.Write(ref disposed, 1);
                try { owner.Dispose(); }
                finally
                {
                    if (subscriptionAttempted)
                    {
                        // Leave attempted=true if removal throws; a later Dispose retries.
                        Connection.ConnectionStatusUpdate -= OnPlatformStatus;
                        subscriptionAttempted = false;
                    }
                    Volatile.Write(ref armed, 0);
                }
            }
        }

        private sealed class Notification
        {
            public readonly DateTime ReceiptUtc = DateTime.UtcNow;
            public readonly ConnectionStatus PreviousPrice, Price, PreviousOrder, Order;
            public Notification(ConnectionStatusEventArgs e)
            {
                PreviousPrice = e.PreviousPriceStatus; Price = e.PriceStatus;
                PreviousOrder = e.PreviousStatus; Order = e.Status;
            }
        }
    }
}
