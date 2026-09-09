using System;
using NinjaTrader.NinjaScript.Indicators;

static class ProviderHistoryAdmissionTests
{
    static readonly DateTime From = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);
    static readonly DateTime To = From.AddHours(1);
    static OrcaStreamTick Tick(DateTime time)
    { return new OrcaStreamTick(time, 100, 1, 1, OrcaStreamClassification.TickDirection); }

    public static void Run(Action<bool, string> check)
    {
        check(default(OrcaProviderHistoryAdmissionStatus) == OrcaProviderHistoryAdmissionStatus.ReadUnavailable,
            "default admission result fails closed");
        Action<OrcaStreamBatch, OrcaProviderHistoryAdmissionStatus> expect = (batch, status) =>
            check(OrcaProviderHistoryAdmission.Evaluate(batch, From, To) == status, "history admission: " + status);
        using (var buffer = new OrcaProviderStreamBuffer(4, 2))
        {
            var cursor = buffer.FirstAvailable;
            expect(buffer.Read(cursor, 2), OrcaProviderHistoryAdmissionStatus.UtcRangeUnverified);
            buffer.BeginHistoricalPass();
            expect(buffer.Read(cursor, 2), OrcaProviderHistoryAdmissionStatus.UtcRangeUnverified);
            buffer.CompleteHistoricalPassAndBeginLive();
            var emptyPass = buffer.Read(cursor, 2);
            check(emptyPass.Coverage.HistoricalPassCompleted && emptyPass.Coverage.FullHistoricalPassRetained,
                "reproduce zero-event completed lifecycle without changing its existing semantics");
            expect(emptyPass, OrcaProviderHistoryAdmissionStatus.UtcRangeUnverified);
            buffer.AppendLive(Tick(To));
            expect(buffer.Read(cursor, 2), OrcaProviderHistoryAdmissionStatus.UtcRangeUnverified);
        }
        using (var buffer = new OrcaProviderStreamBuffer(4, 2))
        {
            var cursor = buffer.FirstAvailable;
            buffer.BeginHistoricalPass(); buffer.AppendHistorical(Tick(From)); buffer.AppendHistorical(Tick(To));
            buffer.CompleteHistoricalPassAndBeginLive();
            expect(buffer.Read(cursor, 2), OrcaProviderHistoryAdmissionStatus.UtcRangeUnverified);
        }
        using (var buffer = new OrcaProviderStreamBuffer(4, 2))
        {
            buffer.BeginLiveOnly(); buffer.AppendLive(Tick(To));
            expect(buffer.Read(buffer.FirstAvailable, 2), OrcaProviderHistoryAdmissionStatus.UtcRangeUnverified);
        }
        using (var buffer = new OrcaProviderStreamBuffer(4, 2))
        {
            var cursor = buffer.FirstAvailable;
            buffer.BeginHistory(From, To);
            expect(buffer.Read(cursor, 2), OrcaProviderHistoryAdmissionStatus.HistoricalPassPending);
            buffer.ConfirmHistory();
            var emptyConfirmed = buffer.Read(cursor, 2);
            check(emptyConfirmed.Events.Count == 0, "confirmed no-trade fixture is empty");
            expect(emptyConfirmed, OrcaProviderHistoryAdmissionStatus.Ready);
            buffer.BeginLive(); buffer.AppendLive(Tick(To));
            expect(buffer.Read(cursor, 2), OrcaProviderHistoryAdmissionStatus.Ready);
        }
        using (var buffer = new OrcaProviderStreamBuffer(3, 2))
        {
            var cursor = buffer.FirstAvailable;
            buffer.BeginHistory(From, To); buffer.AppendHistorical(Tick(From)); buffer.AppendHistorical(Tick(From.AddMinutes(30)));
            buffer.ConfirmHistory(); buffer.BeginLive();
            var confirmed = buffer.Read(cursor, 2);
            expect(confirmed, OrcaProviderHistoryAdmissionStatus.Ready);
            check(OrcaProviderHistoryAdmission.Evaluate(confirmed, From.AddMinutes(1), To.AddMinutes(-1)) == OrcaProviderHistoryAdmissionStatus.Ready,
                "contained requested interval is available");
            check(OrcaProviderHistoryAdmission.Evaluate(confirmed, From.AddTicks(-1), To) == OrcaProviderHistoryAdmissionStatus.RequiredRangeNotCovered,
                "one tick before confirmed start denied");
            check(OrcaProviderHistoryAdmission.Evaluate(confirmed, From, To.AddTicks(1)) == OrcaProviderHistoryAdmissionStatus.RequiredRangeNotCovered,
                "one tick after exclusive confirmed end denied");
            var caughtUp = buffer.Read(confirmed.Next, 2);
            check(caughtUp.Events.Count == 0, "caught-up batch has no payload");
            expect(caughtUp, OrcaProviderHistoryAdmissionStatus.Ready); // Available, not already delivered to a new consumer.
            buffer.AppendLive(Tick(To)); buffer.AppendLive(Tick(To));
            expect(buffer.Read(cursor, 2), OrcaProviderHistoryAdmissionStatus.ReadUnavailable);
            expect(buffer.Read(buffer.FirstAvailable, 2), OrcaProviderHistoryAdmissionStatus.ConfirmedHistoryEvicted);
            check(OrcaProviderHistoryAdmission.Evaluate(buffer.Read(buffer.FirstAvailable, 2), From.AddMinutes(30), To)
                == OrcaProviderHistoryAdmissionStatus.ConfirmedHistoryEvicted, "no timestamp-based salvage of partially retained confirmed history");
            expect(buffer.Read(new OrcaStreamCursor(Guid.NewGuid(), 0), 2), OrcaProviderHistoryAdmissionStatus.ReadUnavailable);
            expect(buffer.Read(new OrcaStreamCursor(cursor.Generation, 100), 2), OrcaProviderHistoryAdmissionStatus.ReadUnavailable);
            buffer.MarkFault(OrcaStreamFault.SourceDisconnected);
            expect(buffer.Read(buffer.FirstAvailable, 2), OrcaProviderHistoryAdmissionStatus.ReadUnavailable);
            buffer.Dispose();
            expect(buffer.Read(buffer.FirstAvailable, 2), OrcaProviderHistoryAdmissionStatus.ReadUnavailable);
            // Immutable evidence is point-in-time; evaluating it must not claim it refreshes the source.
            expect(confirmed, OrcaProviderHistoryAdmissionStatus.Ready);
            Reject(check, () => OrcaProviderHistoryAdmission.Evaluate(confirmed, From, From));
            Reject(check, () => OrcaProviderHistoryAdmission.Evaluate(confirmed, To, From));
            Reject(check, () => OrcaProviderHistoryAdmission.Evaluate(confirmed, DateTime.SpecifyKind(From, DateTimeKind.Local), To));
            Reject(check, () => OrcaProviderHistoryAdmission.Evaluate(confirmed, From, DateTime.SpecifyKind(To, DateTimeKind.Unspecified)));
            Reject(check, () => OrcaProviderHistoryAdmission.Evaluate(confirmed, DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), To));
        }
        Reject(check, () => OrcaProviderHistoryAdmission.Evaluate((OrcaStreamBatch)null, From, To));
        Reject(check, () => OrcaProviderHistoryAdmission.Evaluate((OrcaProviderBatchLease)null, From, To));
        using (var registry = new OrcaProviderStreamRegistry(1, 4, 2))
        {
            var key = new OrcaProviderStreamKey("ES SEP26", "test", "session", "UTC", "tick");
            OrcaProviderPublisherLease publisher; registry.TryAcquire(key, out publisher);
            publisher.BeginHistory(From, To); publisher.ConfirmHistory(); publisher.BeginLive();
            OrcaProviderStreamReader reader; registry.TryOpenReader(key, out reader);
            using (reader)
            {
                var lease = reader.Read(reader.FirstAvailable, 2);
                check(OrcaProviderHistoryAdmission.Evaluate(lease, From, To) == OrcaProviderHistoryAdmissionStatus.Ready,
                    "budgeted reader evaluates the same confirmed-empty coverage");
                lease.Dispose();
                bool rejected = false;
                try { OrcaProviderHistoryAdmission.Evaluate(lease, From, To); } catch (ObjectDisposedException) { rejected = true; }
                check(rejected, "disposed lease cannot be used as an active admission read");
            }
        }
    }
    static void Reject(Action<bool, string> check, Action action)
    {
        bool rejected = false;
        try { action(); } catch (ArgumentException) { rejected = true; }
        check(rejected, "invalid history admission input rejected");
    }
}
