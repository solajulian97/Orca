using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NinjaTrader.NinjaScript.AddOns;

internal static class Program
{
	private const string GoldenUid = "orca.trade.v1:65428ff6137f4582a84ef92b2747bf6ed061344f0b87aff81a7efdab697e1e3a";
	private static int failures;

	private static int Main()
	{
		GoldenIdentity();
		ReplayDoesNotDoubleCount();
		RoundTripThenReplay();
		ScaleInAndPartialExit();
		ReversalSplitStaysIdempotent();
		SeedStaysPartial();
		ConflictAndMissingEvidence();
		ViolationAndOpportunityLinks();
		AtomicSnapshot();
		RejectsForeignSchema();
		SeparateInstruments();
		if (failures != 0) {
			Console.WriteLine("FAILED " + failures.ToString(CultureInfo.InvariantCulture));
			return 1;
		}
		Console.WriteLine("PASS: Orca Rulebook ledger");
		return 0;
	}

	private static void GoldenIdentity()
	{
		OrcaRulebookLedger ledger = Started("Sim", "MNQ SEP26");
		DateTime t = new DateTime(2026, 9, 21, 9, 30, 0, DateTimeKind.Local);
		Fill(ledger, "p", 1, 1, 1, 2, t);
		Fill(ledger, "q", -1, 2, 0, 2, t.AddSeconds(1));
		Fill(ledger, "e0", 2, 20000, 2, 2, t.AddSeconds(2));
		Fill(ledger, "e1", -2, 20010, 0, 2, t.AddSeconds(3));
		var cycles = ledger.CompletedCycles();
		Check(cycles.Count == 2, "primer plus golden cycle");
		Check(cycles[0].TradeUid == null && !cycles[0].IdentityComplete, "first cycle stays unknown: " + cycles[0].IdentityReason);
		Check(cycles[1].TradeUid == GoldenUid, "golden uid was " + cycles[1].TradeUid + " reason " + cycles[1].IdentityReason);
		Check(cycles[1].PnlKnown && cycles[1].GrossPnl == 40m && cycles[1].PnlBasis == "Gross", "golden gross " + cycles[1].GrossPnl);
		Check(ledger.EvidenceStatus == "Partial", "session stays partial while an earlier cycle has no shared id, was " + ledger.EvidenceStatus);
		Check(ledger.PnlBasis == "Gross", "basis");
	}

	private static void ReplayDoesNotDoubleCount()
	{
		OrcaRulebookLedger ledger = Started("Sim", "MNQ SEP26");
		DateTime t = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Local);
		var first = Fill(ledger, "e0", 2, 100, 2, 5, t);
		var again = Fill(ledger, "e0", 2, 100, 2, 5, t);
		Check(first == OrcaRulebookIngestResult.Accepted && again == OrcaRulebookIngestResult.Duplicate, "replay is duplicate");
		Check(ledger.NetQuantity("MNQ SEP26") == 2, "net stays 2");
		Check(ledger.CompletedCycles().Count == 0 && ledger.EventCount("Fill") == 1 && ledger.EventCount("Duplicate") == 1, "one fill event");
	}

	private static void RoundTripThenReplay()
	{
		OrcaRulebookLedger ledger = Started("Sim", "MNQ SEP26");
		DateTime t = new DateTime(2026, 9, 21, 9, 30, 0, DateTimeKind.Local);
		Fill(ledger, "p", 1, 1, 1, 2, t);
		Fill(ledger, "q", -1, 2, 0, 2, t.AddSeconds(1));
		Fill(ledger, "e0", 2, 20000, 2, 2, t.AddSeconds(2));
		Fill(ledger, "e1", -2, 20010, 0, 2, t.AddSeconds(3));
		int fills = ledger.EventCount("Fill");
		OrcaRulebookLedger restored = OrcaRulebookLedger.FromJson(ledger.ToJson());
		Check(restored.SessionId == ledger.SessionId, "session id");
		Check(restored.EventCount("Fill") == fills, "fill count survived " + restored.EventCount("Fill"));
		var cycles = restored.CompletedCycles();
		Check(cycles.Count == 2 && cycles[1].TradeUid == GoldenUid && cycles[1].GrossPnl == 40m, "restored golden");
		var replay = Fill(restored, "e1", -2, 20010, 0, 2, t.AddSeconds(3));
		Check(replay == OrcaRulebookIngestResult.Duplicate, "replay after restore was " + replay + " status " + restored.EvidenceStatus);
		Check(restored.CompletedCycles().Count == 2 && restored.NetQuantity("MNQ SEP26") == 0, "restore did not double count");
	}

	private static void ScaleInAndPartialExit()
	{
		OrcaRulebookLedger scale = FlatBoundary("Scale");
		DateTime t = new DateTime(2026, 9, 21, 11, 0, 0, DateTimeKind.Utc);
		Fill(scale, "a", 1, 100, 1, 2, t);
		Fill(scale, "b", 1, 110, 2, 2, t.AddSeconds(1));
		Fill(scale, "c", -2, 120, 0, 2, t.AddSeconds(2));
		var cycle = scale.CompletedCycles().Last();
		Check(cycle.Allocations.Count == 3 && cycle.GrossPnl == 60m && cycle.PnlKnown, "scale-in gross " + cycle.GrossPnl);

		OrcaRulebookLedger partial = FlatBoundary("Partial");
		Fill(partial, "a", 2, 100, 2, 5, t);
		Fill(partial, "b", -1, 102, 1, 5, t.AddSeconds(1));
		Check(partial.CompletedCycles().Count == 1 && partial.NetQuantity("MNQ SEP26") == 1, "partial exit stays open");
		Fill(partial, "c", -1, 104, 0, 5, t.AddSeconds(2));
		var closed = partial.CompletedCycles().Last();
		Check(closed.Allocations.Count == 3 && closed.GrossPnl == 30m, "partial gross " + closed.GrossPnl);
		Check(Fill(partial, "b", -1, 102, 1, 5, t.AddSeconds(1)) == OrcaRulebookIngestResult.Duplicate, "partial replay");
		Check(partial.CompletedCycles().Count(c => c.Instrument == "MNQ SEP26" && c.Allocations.Count == 3) == 1, "partial replay did not add a cycle");
	}

	private static void ReversalSplitStaysIdempotent()
	{
		OrcaRulebookLedger ledger = FlatBoundary("Reversal");
		DateTime t = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
		Fill(ledger, "e0", 2, 100, 2, 1, t);
		Fill(ledger, "e1", -4, 110, -2, 1, t.AddSeconds(1));
		var cycles = ledger.CompletedCycles();
		Check(cycles.Count == 2, "reversal closes the old cycle, count " + cycles.Count);
		var closed = cycles.Last();
		Check(closed.GrossPnl == 20m && closed.Allocations.Count == 2, "reversal gross");
		Check(closed.Allocations[0].ExecutionId == "e0" && closed.Allocations[0].SignedQuantity == 2, "open leg");
		Check(closed.Allocations[1].ExecutionId == "e1" && closed.Allocations[1].SignedQuantity == -2, "close leg");
		var opened = ledger.OpenCycle("MNQ SEP26");
		Check(opened != null && opened.Allocations.Count == 1 && opened.Allocations[0].ExecutionId == "e1" && opened.Allocations[0].SignedQuantity == -2, "reversal remainder");
		Check(ledger.NetQuantity("MNQ SEP26") == -2, "short remainder");
		Check(Fill(ledger, "e1", -4, 110, -2, 1, t.AddSeconds(1)) == OrcaRulebookIngestResult.Duplicate, "reversal replay");
		Check(ledger.CompletedCycles().Count == 2 && ledger.OpenCycle("MNQ SEP26").Allocations.Count == 1, "reversal replay kept both equal legs once");
	}

	private static void SeedStaysPartial()
	{
		OrcaRulebookLedger ledger = Started("Sim", "MNQ SEP26");
		DateTime t = new DateTime(2026, 9, 21, 13, 0, 0, DateTimeKind.Utc);
		ledger.SeedOpenPosition("MNQ SEP26", -2, 50, t);
		ledger.SeedOpenPosition("MNQ SEP26", -2, 50, t);
		Check(ledger.EventCount("Seed") == 1 && ledger.NetQuantity("MNQ SEP26") == -2, "seed once");
		Fill(ledger, "cover", 2, 40, 0, 2, t.AddSeconds(1));
		var seeded = ledger.CompletedCycles().Single();
		Check(seeded.Observation == "Seeded" && seeded.TradeUid == null && !seeded.PnlKnown, "seeded close is not a shared id and has no invented gross");
		Fill(ledger, "a", 1, 10, 1, 2, t.AddSeconds(2));
		Fill(ledger, "b", -1, 12, 0, 2, t.AddSeconds(3));
		Fill(ledger, "c", 1, 10, 1, 2, t.AddSeconds(4));
		Fill(ledger, "d", -1, 12, 0, 2, t.AddSeconds(5));
		var done = ledger.CompletedCycles();
		Check(done.Count == 3, "seed plus two observed cycles, count " + done.Count);
		Check(done[1].TradeUid == null, "first cycle after a seed still has no shared id");
		Check(done[2].TradeUid != null && done[2].TradeUid.StartsWith("orca.trade.v1:", StringComparison.Ordinal) && done[2].GrossPnl == 4m, "second observed cycle " + done[2].TradeUid + " pnl " + done[2].GrossPnl);
	}

	private static void ConflictAndMissingEvidence()
	{
		OrcaRulebookLedger ledger = Started("Sim", "MES SEP26");
		DateTime t = new DateTime(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc);
		Fill(ledger, "e0", 2, 100, 2, 5, t, "MES SEP26");
		var conflict = Fill(ledger, "e0", 3, 100, 3, 5, t.AddSeconds(1), "MES SEP26");
		Check(conflict == OrcaRulebookIngestResult.Conflict, "changed facts conflict");
		Check(ledger.NetQuantity("MES SEP26") == 2 && ledger.EvidenceStatus == "NeedsReview", "conflict does not move net, net " + ledger.NetQuantity("MES SEP26") + " status " + ledger.EvidenceStatus);

		OrcaRulebookLedger missing = Started("Sim", "MES SEP26");
		var noId = missing.Ingest(new OrcaRulebookFill {
			Account = "Sim", Instrument = "MES SEP26", ExecutionId = " ", SignedQuantity = 1, Price = 10, PointValue = 5, Time = t, PositionAfter = 1
		});
		Check(noId == OrcaRulebookIngestResult.Ignored && missing.NetQuantity("MES SEP26") == 0, "missing id is not applied");
		Check(missing.EvidenceStatus == "NeedsReview", "missing id needs review");

		OrcaRulebookLedger historical = Started("Sim", "MES SEP26");
		var sod = historical.Ingest(new OrcaRulebookFill {
			Account = "Sim", Instrument = "MES SEP26", ExecutionId = "sod", SignedQuantity = 1, Price = 10, PointValue = 5, Time = t, PositionAfter = 1, Historical = true
		});
		Check(sod == OrcaRulebookIngestResult.Ignored && historical.NetQuantity("MES SEP26") == 0, "historical fill is evidence only");

		OrcaRulebookLedger other = Started("Sim", "MES SEP26");
		var wrong = other.Ingest(new OrcaRulebookFill {
			Account = "sim", Instrument = "MES SEP26", ExecutionId = "e", SignedQuantity = 1, Price = 10, PointValue = 5, Time = t, PositionAfter = 1
		});
		Check(wrong == OrcaRulebookIngestResult.Ignored && other.NetQuantity("MES SEP26") == 0, "account match is ordinal");

		OrcaRulebookLedger missingPoint = Started("Sim", "MES SEP26");
		Check(Fill(missingPoint, "pv", 1, 10, 1, 0, t, "MES SEP26") == OrcaRulebookIngestResult.Accepted, "missing point value still records the fill");
		Check(missingPoint.NetQuantity("MES SEP26") == 1 && missingPoint.EvidenceStatus == "NeedsReview", "missing point value is not scored as known gross");
	}

	private static void ViolationAndOpportunityLinks()
	{
		OrcaRulebookLedger ledger = FlatBoundary("Link");
		DateTime t = new DateTime(2026, 9, 21, 15, 0, 0, DateTimeKind.Utc);
		Fill(ledger, "e0", 1, 100, 1, 2, t);
		string openLink = ledger.LinkViolation("max-size", "Max size", "Critical", "too big", "MNQ SEP26", t);
		Check(openLink.StartsWith("orca.rulebook.cycle:", StringComparison.Ordinal), "open violation uses a local cycle id");
		string sessionLink = ledger.LinkViolation("checklist", "Checklist", "Warning", "broken", "", t);
		Check(sessionLink.StartsWith("orca.rulebook.session:", StringComparison.Ordinal), "manual violation stays on the session");
		ledger.BeginConsequenceWindow();
		Fill(ledger, "e1", -1, 110, 0, 2, t.AddSeconds(1));
		var cycle = ledger.CompletedCycles().Last();
		string closedLink = ledger.LinkViolation("max-loss", "Max loss", "Major", "loss", "MNQ SEP26", t.AddSeconds(1));
		ledger.EndConsequenceWindow();
		Check(cycle.TradeUid != null && closedLink == cycle.TradeUid, "completed violation uses the shared id when it exists, link " + closedLink);
		ledger.RecordOpportunity("cooldown", "Cooldown", "CompletedCycle", "PendingDefinition", "MNQ SEP26", "Denominator is not approved; this is not a grade.", true);
		Check(ledger.EventCount("Opportunity") == 1, "opportunity recorded without a grade");
		string later = ledger.LinkViolation("session-loss", "Session loss", "Critical", "down", "MNQ SEP26", t.AddSeconds(2));
		Check(later.StartsWith("orca.rulebook.session:", StringComparison.Ordinal), "later flat violation is not pinned to the previous trade");
	}

	private static void AtomicSnapshot()
	{
		OrcaRulebookLedger ledger = FlatBoundary("File");
		DateTime t = new DateTime(2026, 9, 21, 16, 0, 0, DateTimeKind.Utc);
		Fill(ledger, "e0", 1, 5, 1, 2, t);
		Fill(ledger, "e1", -1, 7, 0, 2, t.AddSeconds(1));
		string directory = Path.Combine(Path.GetTempPath(), "orca-rulebook-ledger-" + Guid.NewGuid().ToString("N"));
		try {
			string path = OrcaRulebookLedger.WriteAtomic(ledger, directory);
			string again = OrcaRulebookLedger.WriteAtomic(ledger, directory);
			Check(path == again && File.Exists(path), "stable ledger path");
			Check(Directory.GetFiles(directory).Length == 1, "tmp file was not left behind");
			var restored = OrcaRulebookLedger.FromJson(File.ReadAllText(path));
			Check(restored.CompletedCycles().Last().GrossPnl == 4m, "snapshot gross");
		} finally {
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
		}
	}

	private static void RejectsForeignSchema()
	{
		ExpectInvalid(delegate { OrcaRulebookLedger.FromJson(""); });
		ExpectInvalid(delegate { OrcaRulebookLedger.FromJson("{\"SchemaVersion\":2,\"Product\":\"Orca Rulebook\",\"PnlBasis\":\"Gross\"}"); });
		ExpectInvalid(delegate { OrcaRulebookLedger.FromJson("{\"SchemaVersion\":1,\"Product\":\"Orca Rulebook\",\"PnlBasis\":\"Net\"}"); });
	}

	private static void SeparateInstruments()
	{
		OrcaRulebookLedger ledger = Started("Sim", "All Instruments");
		DateTime t = new DateTime(2026, 9, 21, 17, 0, 0, DateTimeKind.Utc);
		Fill(ledger, "nq", 2, 100, 2, 20, t, "NQ SEP26");
		Fill(ledger, "mnq", -1, 100, -1, 2, t, "MNQ SEP26");
		Check(ledger.NetQuantity("NQ SEP26") == 2 && ledger.NetQuantity("MNQ SEP26") == -1, "books stay split");
	}

	private static OrcaRulebookLedger FlatBoundary(string name)
	{
		OrcaRulebookLedger ledger = Started("Sim", "MNQ SEP26");
		DateTime t = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc);
		Fill(ledger, name + "-p", 1, 1, 1, 2, t);
		Fill(ledger, name + "-q", -1, 1, 0, 2, t.AddSeconds(1));
		return ledger;
	}

	private static OrcaRulebookLedger Started(string account, string scope)
	{
		OrcaRulebookLedger ledger = OrcaRulebookLedger.Open(account, scope);
		ledger.Begin(new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc));
		return ledger;
	}

	private static OrcaRulebookIngestResult Fill(OrcaRulebookLedger ledger, string id, int signed, double price, int positionAfter, double pointValue, DateTime time)
	{
		return Fill(ledger, id, signed, price, positionAfter, pointValue, time, "MNQ SEP26");
	}

	private static OrcaRulebookIngestResult Fill(OrcaRulebookLedger ledger, string id, int signed, double price, int positionAfter, double pointValue, DateTime time, string instrument)
	{
		return ledger.Ingest(new OrcaRulebookFill {
			Account = "Sim",
			Instrument = instrument,
			ExecutionId = id,
			SignedQuantity = signed,
			Price = price,
			PointValue = pointValue,
			Time = time,
			PositionAfter = positionAfter
		});
	}

	private static void ExpectInvalid(Action action)
	{
		try {
			action();
			Check(false, "expected an unreadable ledger to be rejected");
		} catch (InvalidDataException) {
			Check(true, "rejected");
		}
	}

	private static void Check(bool condition, string message)
	{
		if (condition)
			return;
		failures++;
		Console.WriteLine("FAIL: " + message);
	}
}
