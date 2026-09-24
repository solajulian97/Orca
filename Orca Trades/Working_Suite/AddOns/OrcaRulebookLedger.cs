// Orca Rulebook local-first execution ledger. No NinjaTrader, database, or market-data dependencies.
// Ingest is in-memory only. Callers write snapshots off account callbacks and off the WPF render path.
// Gross P&L is fill math times point value. Fees are not estimated. A shared trade id is published only
// when Orca.SharedIdentity accepts the cycle; otherwise the id stays unknown.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace NinjaTrader.NinjaScript.AddOns
{
	public enum OrcaRulebookIngestResult
	{
		Accepted,
		Duplicate,
		Conflict,
		Ignored
	}

	public sealed class OrcaRulebookFill
	{
		public string Account { get; set; }
		public string Instrument { get; set; }
		public string ExecutionId { get; set; }
		public int SignedQuantity { get; set; }
		public double Price { get; set; }
		public double PointValue { get; set; }
		public DateTime Time { get; set; }
		public int? PositionAfter { get; set; }
		public bool Historical { get; set; }
	}

	public sealed class OrcaRulebookAllocationView
	{
		public string ExecutionId { get; set; }
		public int SignedQuantity { get; set; }
	}

	public sealed class OrcaRulebookCycleView
	{
		public string LocalId { get; set; }
		public string Instrument { get; set; }
		public string Direction { get; set; }
		public string Observation { get; set; }
		public bool IdentityComplete { get; set; }
		public string TradeUid { get; set; }
		public string IdentityReason { get; set; }
		public bool PnlKnown { get; set; }
		public decimal GrossPnl { get; set; }
		public string PnlBasis { get; set; }
		public List<OrcaRulebookAllocationView> Allocations { get; set; }
	}

	public sealed class OrcaRulebookLedgerDocument
	{
		public int SchemaVersion { get; set; }
		public string Product { get; set; }
		public string SessionId { get; set; }
		public string Account { get; set; }
		public string InstrumentScope { get; set; }
		public string PnlBasis { get; set; }
		public string Status { get; set; }
		public string StartedLocal { get; set; }
		public string EndedLocal { get; set; }
		public List<OrcaRulebookLedgerEvent> Events { get; set; }
	}

	public sealed class OrcaRulebookLedgerEvent
	{
		public string Kind { get; set; }
		public string Account { get; set; }
		public string Instrument { get; set; }
		public string ExecutionId { get; set; }
		public int SignedQuantity { get; set; }
		public double Price { get; set; }
		public double PointValue { get; set; }
		public string Time { get; set; }
		public bool HasPositionAfter { get; set; }
		public int PositionAfter { get; set; }
		public bool Historical { get; set; }
		public string Reason { get; set; }
		public string RuleId { get; set; }
		public string RuleName { get; set; }
		public string Severity { get; set; }
		public string Message { get; set; }
		public string Result { get; set; }
		public string Note { get; set; }
		public string Scope { get; set; }
		public string CycleLocalId { get; set; }
		public string Link { get; set; }
	}

	public sealed class OrcaRulebookLedger
	{
		public const int SchemaVersion = 1;
		public const string ProductName = "Orca Rulebook";
		public const string GrossBasis = "Gross";
		private const int MaxEvents = 100000;

		private readonly Dictionary<string, Book> books = new Dictionary<string, Book>(StringComparer.Ordinal);
		private readonly Dictionary<string, OrcaRulebookLedgerEvent> rawFacts = new Dictionary<string, OrcaRulebookLedgerEvent>(StringComparer.Ordinal);
		private readonly List<OrcaRulebookLedgerEvent> events = new List<OrcaRulebookLedgerEvent>();
		private bool needsReview;
		private string reviewReason;
		private bool replaying;
		private bool consequenceWindow;
		private Dictionary<string, string> justClosed;
		private int nextCycleNumber = 1;

		private OrcaRulebookLedger()
		{
		}

		public string SessionId { get; private set; }
		public string Account { get; private set; }
		public string InstrumentScope { get; private set; }
		public string Status { get; private set; }
		public string StartedLocal { get; private set; }
		public string EndedLocal { get; private set; }
		public string PnlBasis { get { return GrossBasis; } }

		public string EvidenceStatus
		{
			get
			{
				if (needsReview)
					return "NeedsReview";
				if (Status == "NotStarted")
					return "Unknown";
				if (HasOpenCycle() || HasIncompleteCycle() || CompletedCycles().Count == 0)
					return "Partial";
				return "Observed";
			}
		}

		public string EvidenceReason
		{
			get
			{
				if (needsReview)
					return string.IsNullOrWhiteSpace(reviewReason) ? "Evidence needs review" : reviewReason;
				if (Status == "NotStarted")
					return "Session has not started";
				OrcaRulebookCycleView open = FirstOpenCycle();
				if (open != null)
					return "Open round trip on " + open.Instrument + " is still in progress";
				OrcaRulebookCycleView incomplete = FirstIncompleteCycle();
				if (incomplete != null)
					return incomplete.IdentityReason ?? incomplete.Observation;
				if (CompletedCycles().Count == 0)
					return "No completed round trip was observed";
				return "Observed fills form complete flat-to-flat cycles with shared trade ids. Gross P&L excludes fees.";
			}
		}

		public static OrcaRulebookLedger Open(string account, string instrumentScope)
		{
			OrcaRulebookLedger ledger = new OrcaRulebookLedger();
			ledger.SessionId = Guid.NewGuid().ToString("N");
			ledger.Account = account ?? string.Empty;
			ledger.InstrumentScope = string.IsNullOrWhiteSpace(instrumentScope) ? "All Instruments" : instrumentScope;
			ledger.Status = "NotStarted";
			return ledger;
		}

		public void Begin(DateTime startedLocal)
		{
			if (Status == "Active" || Status == "Paused" || Status == "Ended")
				return;
			Status = "Active";
			StartedLocal = FormatTime(startedLocal);
		}

		public void Pause()
		{
			if (Status == "Active")
				Status = "Paused";
		}

		public void Resume()
		{
			if (Status == "Paused")
				Status = "Active";
		}

		public void End(DateTime endedLocal)
		{
			if (Status == "Ended")
				return;
			Status = "Ended";
			EndedLocal = FormatTime(endedLocal);
		}

		public void SetInstrumentScope(string scope)
		{
			if (string.IsNullOrWhiteSpace(scope))
				scope = "All Instruments";
			if (string.Equals(scope, InstrumentScope, StringComparison.Ordinal))
				return;
			InstrumentScope = scope;
			if (Status == "Active" || Status == "Paused") {
				MarkReview("Instrument scope changed during the session");
				Append(new OrcaRulebookLedgerEvent { Kind = "Scope", Scope = scope, Reason = "Instrument scope changed during the session" });
			}
		}

		public void SeedOpenPosition(string instrument, int signedQuantity, double averagePrice, DateTime time)
		{
			if (!CanMutate() || string.IsNullOrWhiteSpace(instrument) || signedQuantity == 0 || signedQuantity == int.MinValue)
				return;
			Book book = GetBook(instrument);
			if (book.SawFill || book.Seeded) {
				if (book.Seeded && book.Net == signedQuantity)
					return;
				MarkReview("Seed disagreed with quantity already tracked for " + instrument);
				Append(new OrcaRulebookLedgerEvent {
					Kind = "Gap",
					Instrument = instrument,
					SignedQuantity = signedQuantity,
					Price = averagePrice,
					Time = FormatTime(time),
					Reason = "Seed disagreed with quantity already tracked"
				});
				return;
			}
			book.Seeded = true;
			book.AwaitingFlat = true;
			book.Net = signedQuantity;
			book.Identity = new Orca.SharedIdentity.Tracker();
			book.Open = StartCycle(instrument, signedQuantity > 0 ? "Long" : "Short", time, "Seeded");
			book.Open.IdentityReason = "Seeded open position; entry fills were not observed";
			Append(new OrcaRulebookLedgerEvent {
				Kind = "Seed",
				Instrument = instrument,
				SignedQuantity = signedQuantity,
				Price = averagePrice,
				Time = FormatTime(time),
				Reason = book.Open.IdentityReason
			});
		}

		public OrcaRulebookIngestResult Ingest(OrcaRulebookFill fill)
		{
			return Ingest(fill, true);
		}

		public void BeginConsequenceWindow()
		{
			consequenceWindow = true;
			justClosed = new Dictionary<string, string>(StringComparer.Ordinal);
		}

		public void EndConsequenceWindow()
		{
			consequenceWindow = false;
			justClosed = null;
		}

		public string LinkViolation(string ruleId, string ruleName, string severity, string message, string instrument, DateTime time)
		{
			string cycleLocalId = null;
			string tradeUid = null;
			Book book;
			if (!string.IsNullOrWhiteSpace(instrument) && books.TryGetValue(instrument, out book)) {
				if (book.Open != null) {
					cycleLocalId = book.Open.LocalId;
					tradeUid = book.Open.TradeUid;
				} else if (consequenceWindow && justClosed != null && justClosed.ContainsKey(instrument)) {
					cycleLocalId = justClosed[instrument];
					OrcaRulebookCycleView closed = FindCompleted(instrument, cycleLocalId);
					if (closed != null)
						tradeUid = closed.TradeUid;
				}
			}
			string link = !string.IsNullOrEmpty(tradeUid)
				? tradeUid
				: (cycleLocalId != null ? "orca.rulebook.cycle:" + cycleLocalId : "orca.rulebook.session:" + SessionId);
			Append(new OrcaRulebookLedgerEvent {
				Kind = "Violation",
				Instrument = instrument ?? string.Empty,
				Time = FormatTime(time),
				RuleId = ruleId ?? string.Empty,
				RuleName = ruleName ?? string.Empty,
				Severity = severity ?? string.Empty,
				Message = message ?? string.Empty,
				CycleLocalId = cycleLocalId ?? string.Empty,
				Link = link
			});
			return link;
		}

		public void RecordOpportunity(string ruleId, string ruleName, string scope, string result, string instrument, string note, bool attachToCompleted)
		{
			string cycleLocalId = string.Empty;
			Book book;
			if (!string.IsNullOrWhiteSpace(instrument) && books.TryGetValue(instrument, out book)) {
				if (attachToCompleted && book.Completed.Count > 0)
					cycleLocalId = book.Completed[book.Completed.Count - 1].LocalId;
				else if (book.Open != null)
					cycleLocalId = book.Open.LocalId;
			}
			Append(new OrcaRulebookLedgerEvent {
				Kind = "Opportunity",
				Instrument = instrument ?? string.Empty,
				RuleId = ruleId ?? string.Empty,
				RuleName = ruleName ?? string.Empty,
				Scope = scope ?? string.Empty,
				Result = result ?? string.Empty,
				Note = note ?? string.Empty,
				CycleLocalId = cycleLocalId
			});
		}

		public void MarkGap(string instrument, string reason)
		{
			MarkReview(reason);
			Append(new OrcaRulebookLedgerEvent {
				Kind = "Gap",
				Instrument = instrument ?? string.Empty,
				Reason = reason ?? string.Empty
			});
		}

		public void Invalidate(string reason)
		{
			if (string.IsNullOrWhiteSpace(reason))
				reason = "Identity continuity invalidated";
			foreach (KeyValuePair<string, Book> pair in books)
				pair.Value.Identity.Invalidate(reason);
			MarkReview(reason);
			Append(new OrcaRulebookLedgerEvent { Kind = "Invalidate", Reason = reason });
		}

		public int NetQuantity(string instrument)
		{
			Book book;
			if (string.IsNullOrWhiteSpace(instrument) || !books.TryGetValue(instrument, out book))
				return 0;
			return book.Net;
		}

		public int EventCount(string kind)
		{
			int count = 0;
			for (int i = 0; i < events.Count; i++) {
				if (string.Equals(events[i].Kind, kind, StringComparison.Ordinal))
					count++;
			}
			return count;
		}

		public OrcaRulebookCycleView OpenCycle(string instrument)
		{
			Book book;
			if (string.IsNullOrWhiteSpace(instrument) || !books.TryGetValue(instrument, out book) || book.Open == null)
				return null;
			return book.Open.ToView();
		}

		public List<OrcaRulebookCycleView> CompletedCycles()
		{
			List<OrcaRulebookCycleView> views = new List<OrcaRulebookCycleView>();
			foreach (KeyValuePair<string, Book> pair in books) {
				for (int i = 0; i < pair.Value.Completed.Count; i++)
					views.Add(pair.Value.Completed[i].ToView());
			}
			return views;
		}

		public string ToJson()
		{
			OrcaRulebookLedgerDocument document = new OrcaRulebookLedgerDocument {
				SchemaVersion = SchemaVersion,
				Product = ProductName,
				SessionId = SessionId,
				Account = Account,
				InstrumentScope = InstrumentScope,
				PnlBasis = GrossBasis,
				Status = Status,
				StartedLocal = StartedLocal,
				EndedLocal = EndedLocal,
				Events = events
			};
			return CreateSerializer().Serialize(document);
		}

		public static OrcaRulebookLedger FromJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				throw new InvalidDataException("Orca Rulebook ledger is empty.");
			OrcaRulebookLedgerDocument document;
			try {
				document = CreateSerializer().Deserialize<OrcaRulebookLedgerDocument>(json);
			} catch (Exception ex) {
				throw new InvalidDataException("Orca Rulebook ledger could not be read.", ex);
			}
			if (document == null || document.SchemaVersion != SchemaVersion)
				throw new InvalidDataException("Orca Rulebook ledger schema is missing or unsupported.");
			if (!string.Equals(document.Product, ProductName, StringComparison.Ordinal) || !string.Equals(document.PnlBasis, GrossBasis, StringComparison.Ordinal))
				throw new InvalidDataException("Orca Rulebook ledger product or P&L basis is not recognized.");
			OrcaRulebookLedger ledger = new OrcaRulebookLedger();
			ledger.SessionId = document.SessionId ?? string.Empty;
			ledger.Account = document.Account ?? string.Empty;
			ledger.InstrumentScope = string.IsNullOrWhiteSpace(document.InstrumentScope) ? "All Instruments" : document.InstrumentScope;
			ledger.Status = string.IsNullOrWhiteSpace(document.Status) ? "NotStarted" : document.Status;
			ledger.StartedLocal = document.StartedLocal;
			ledger.EndedLocal = document.EndedLocal;
			ledger.replaying = true;
			if (document.Events != null) {
				for (int i = 0; i < document.Events.Count; i++) {
					OrcaRulebookLedgerEvent stored = document.Events[i] ?? new OrcaRulebookLedgerEvent();
					ledger.events.Add(stored);
					ledger.ApplyStored(stored);
				}
			}
			ledger.replaying = false;
			ledger.Status = string.IsNullOrWhiteSpace(document.Status) ? "NotStarted" : document.Status;
			ledger.InstrumentScope = string.IsNullOrWhiteSpace(document.InstrumentScope) ? "All Instruments" : document.InstrumentScope;
			return ledger;
		}

		public static string WriteAtomic(OrcaRulebookLedger ledger, string directory)
		{
			if (ledger == null)
				throw new ArgumentNullException("ledger");
			if (string.IsNullOrWhiteSpace(directory))
				throw new ArgumentException("Ledger directory is required.", "directory");
			Directory.CreateDirectory(directory);
			string name = "rulebook-" + SafeFileToken(ledger.SessionId) + ".json";
			string finalPath = Path.Combine(directory, name);
			string tempPath = finalPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
			File.WriteAllText(tempPath, ledger.ToJson(), new UTF8Encoding(false));
			if (File.Exists(finalPath))
				File.Replace(tempPath, finalPath, null);
			else
				File.Move(tempPath, finalPath);
			return finalPath;
		}

		private OrcaRulebookIngestResult Ingest(OrcaRulebookFill fill, bool record)
		{
			if (fill == null)
				return Ignore(record, "Ignored", "Fill was missing");
			if (!CanMutate())
				return Ignore(record, "Gap", "Fill arrived while the session was not active", fill.Instrument);
			if (!string.Equals(fill.Account ?? string.Empty, Account, StringComparison.Ordinal))
				return Ignore(record, "Gap", "Fill account does not match the session account", fill.Instrument);
			if (string.IsNullOrWhiteSpace(fill.Instrument))
				return Ignore(record, "Gap", "Fill is missing a full contract name");
			if (fill.Historical)
				return ConsumeUnapplied(fill, record, "Historical", "Historical or start-of-day execution was not applied");
			if (string.IsNullOrWhiteSpace(fill.ExecutionId))
				return Ignore(record, "Unidentified", "Missing execution id; duplicate callbacks cannot be distinguished", fill.Instrument);
			if (fill.SignedQuantity == 0 || fill.SignedQuantity == int.MinValue || BadPrice(fill.Price))
				return ConsumeUnapplied(fill, record, "Invalid", "Execution price or quantity cannot be applied");

			string rawKey = RawKey(fill.ExecutionId);
			OrcaRulebookLedgerEvent prior;
			if (rawFacts.TryGetValue(rawKey, out prior)) {
				if (SameFill(prior, fill)) {
					if (record)
						Append(EventFromFill(fill, "Duplicate", "Duplicate execution callback"));
					return OrcaRulebookIngestResult.Duplicate;
				}
				MarkReview("Execution id was delivered again with different facts");
				if (record)
					Append(EventFromFill(fill, "Conflict", "Execution id was delivered again with different facts"));
				return OrcaRulebookIngestResult.Conflict;
			}

			Book book = GetBook(fill.Instrument);
			long next = (long)book.Net + fill.SignedQuantity;
			if (next > int.MaxValue || next < int.MinValue)
				return ConsumeUnapplied(fill, record, "Invalid", "Execution quantity overflowed the position");

			OrcaRulebookLedgerEvent stored = EventFromFill(fill, "Fill", null);
			rawFacts[rawKey] = stored;
			ApplyFill(book, fill);
			if (record)
				Append(stored);
			return OrcaRulebookIngestResult.Accepted;
		}

		private void ApplyStored(OrcaRulebookLedgerEvent stored)
		{
			if (stored == null || string.IsNullOrEmpty(stored.Kind))
				return;
			if (stored.Kind == "Fill" || stored.Kind == "Duplicate" || stored.Kind == "Conflict" || stored.Kind == "Invalid" || stored.Kind == "Historical") {
				Ingest(FillFromEvent(stored), false);
				return;
			}
			if (stored.Kind == "Unidentified" || stored.Kind == "Gap") {
				MarkReview(stored.Reason);
				return;
			}
			if (stored.Kind == "Seed") {
				ApplySeed(stored);
				return;
			}
			if (stored.Kind == "Scope") {
				if (!string.IsNullOrWhiteSpace(stored.Reason))
					MarkReview(stored.Reason);
				if (!string.IsNullOrWhiteSpace(stored.Scope))
					InstrumentScope = stored.Scope;
				return;
			}
			if (stored.Kind == "Invalidate") {
				string reason = string.IsNullOrWhiteSpace(stored.Reason) ? "Identity continuity invalidated" : stored.Reason;
				foreach (KeyValuePair<string, Book> pair in books)
					pair.Value.Identity.Invalidate(reason);
				MarkReview(reason);
			}
		}

		private void ApplySeed(OrcaRulebookLedgerEvent stored)
		{
			if (stored == null || string.IsNullOrWhiteSpace(stored.Instrument) || stored.SignedQuantity == 0)
				return;
			Book book = GetBook(stored.Instrument);
			if (book.SawFill || book.Seeded)
				return;
			book.Seeded = true;
			book.AwaitingFlat = true;
			book.Net = stored.SignedQuantity;
			book.Identity = new Orca.SharedIdentity.Tracker();
			book.Open = StartCycle(stored.Instrument, stored.SignedQuantity > 0 ? "Long" : "Short", ParseTime(stored.Time), "Seeded");
			book.Open.IdentityReason = string.IsNullOrWhiteSpace(stored.Reason) ? "Seeded open position; entry fills were not observed" : stored.Reason;
		}

		private void ApplyFill(Book book, OrcaRulebookFill fill)
		{
			int sign = Math.Sign(fill.SignedQuantity);
			int quantity = Math.Abs(fill.SignedQuantity);
			int closing = book.Net != 0 && Math.Sign(book.Net) != sign ? Math.Min(Math.Abs(book.Net), quantity) : 0;
			Orca.SharedIdentity.Cycle identityCompleted = null;
			if (!book.AwaitingFlat)
				identityCompleted = book.Identity.Fill(Account, fill.Instrument, fill.ExecutionId, fill.SignedQuantity, fill.Time, fill.PositionAfter);

			if (closing > 0) {
				if (book.Open == null)
					book.Open = StartCycle(fill.Instrument, sign > 0 ? "Short" : "Long", fill.Time, book.AwaitingFlat ? "Seeded" : "Observed");
				NotePointValue(book, fill);
				book.Open.Allocations.Add(new OrcaRulebookAllocationView { ExecutionId = fill.ExecutionId, SignedQuantity = sign * closing });
				book.Cash -= (decimal)sign * ToDecimal(fill.Price) * closing;
				book.Net += sign * closing;
				if (book.Net == 0) {
					CloseCycle(book, identityCompleted, fill.Time);
					if (book.AwaitingFlat) {
						book.AwaitingFlat = false;
						book.Seeded = false;
						book.Identity = new Orca.SharedIdentity.Tracker();
						if (quantity > closing)
							book.Identity.Fill(Account, fill.Instrument, fill.ExecutionId, sign * (quantity - closing), fill.Time, fill.PositionAfter);
					}
				}
			}

			int opening = quantity - closing;
			if (opening > 0) {
				if (book.Net == 0) {
					book.Cash = 0;
					book.PointValueSet = false;
					book.PointValueConflict = false;
					book.Open = StartCycle(fill.Instrument, sign > 0 ? "Long" : "Short", fill.Time, "Observed");
				}
				if (book.Open == null)
					book.Open = StartCycle(fill.Instrument, sign > 0 ? "Long" : "Short", fill.Time, book.Seeded ? "Seeded" : "Observed");
				NotePointValue(book, fill);
				book.Open.Allocations.Add(new OrcaRulebookAllocationView { ExecutionId = fill.ExecutionId, SignedQuantity = sign * opening });
				book.Cash -= (decimal)sign * ToDecimal(fill.Price) * opening;
				book.Net += sign * opening;
			}
			book.SawFill = true;
		}

		private void CloseCycle(Book book, Orca.SharedIdentity.Cycle identityCompleted, DateTime time)
		{
			CycleDraft cycle = book.Open;
			if (cycle == null)
				return;
			cycle.ExitTime = time;
			bool seeded = cycle.Observation == "Seeded";
			if (seeded || book.PointValueConflict || !book.PointValueSet) {
				cycle.PnlKnown = false;
				cycle.GrossPnl = 0;
				if (book.PointValueConflict) {
					cycle.Observation = "NeedsReview";
					cycle.IdentityReason = "Point value changed inside the round trip";
					MarkReview(cycle.IdentityReason);
				} else if (seeded) {
					cycle.IdentityReason = "Seeded open position; entry fills were not observed";
				} else {
					cycle.IdentityReason = "Point value was missing, so gross P&L is unknown";
					MarkReview(cycle.IdentityReason);
				}
			} else {
				cycle.PnlKnown = true;
				cycle.GrossPnl = book.Cash * ToDecimal(book.PointValue);
				cycle.PnlBasis = GrossBasis;
			}

			if (identityCompleted != null) {
				cycle.IdentityReason = identityCompleted.Reason;
				if (AllocationsMatch(cycle, identityCompleted) && identityCompleted.CompleteHistory && !string.IsNullOrEmpty(identityCompleted.Uid)) {
					cycle.TradeUid = identityCompleted.Uid;
					cycle.IdentityComplete = true;
					if (cycle.Observation == "Observed")
						cycle.Observation = "Observed";
				} else {
					cycle.TradeUid = null;
					cycle.IdentityComplete = false;
					if (cycle.Observation == "Observed")
						cycle.Observation = "Partial";
					if (IsReviewIdentityReason(identityCompleted.Reason) || !AllocationsMatch(cycle, identityCompleted))
						MarkReview(AllocationsMatch(cycle, identityCompleted) ? identityCompleted.Reason : "Round-trip allocations did not match the shared identity contract");
				}
			} else if (!seeded) {
				cycle.TradeUid = null;
				cycle.IdentityComplete = false;
				if (string.IsNullOrWhiteSpace(cycle.IdentityReason))
					cycle.IdentityReason = "Shared trade id is unknown";
				if (cycle.Observation == "Observed")
					cycle.Observation = "Partial";
			} else {
				cycle.TradeUid = null;
				cycle.IdentityComplete = false;
			}

			book.Completed.Add(cycle);
			if (consequenceWindow && justClosed != null)
				justClosed[cycle.Instrument] = cycle.LocalId;
			book.Open = null;
			book.Cash = 0;
			book.PointValueSet = false;
			book.PointValueConflict = false;
		}

		private OrcaRulebookIngestResult ConsumeUnapplied(OrcaRulebookFill fill, bool record, string kind, string reason)
		{
			if (!string.IsNullOrWhiteSpace(fill.ExecutionId)) {
				string rawKey = RawKey(fill.ExecutionId);
				OrcaRulebookLedgerEvent prior;
				if (rawFacts.TryGetValue(rawKey, out prior)) {
					if (SameFill(prior, fill)) {
						if (record)
							Append(EventFromFill(fill, "Duplicate", "Duplicate execution callback"));
						return OrcaRulebookIngestResult.Duplicate;
					}
					MarkReview("Execution id was delivered again with different facts");
					if (record)
						Append(EventFromFill(fill, "Conflict", "Execution id was delivered again with different facts"));
					return OrcaRulebookIngestResult.Conflict;
				}
				OrcaRulebookLedgerEvent stored = EventFromFill(fill, kind, reason);
				rawFacts[rawKey] = stored;
				if (record)
					Append(stored);
			} else if (record) {
				Append(EventFromFill(fill, kind, reason));
			}
			MarkReview(reason);
			return OrcaRulebookIngestResult.Ignored;
		}

		private OrcaRulebookIngestResult Ignore(bool record, string kind, string reason)
		{
			return Ignore(record, kind, reason, null);
		}

		private OrcaRulebookIngestResult Ignore(bool record, string kind, string reason, string instrument)
		{
			MarkReview(reason);
			if (record)
				Append(new OrcaRulebookLedgerEvent { Kind = kind, Instrument = instrument ?? string.Empty, Reason = reason ?? string.Empty });
			return OrcaRulebookIngestResult.Ignored;
		}

		private bool CanMutate()
		{
			return replaying || Status == "Active";
		}

		private void NotePointValue(Book book, OrcaRulebookFill fill)
		{
			if (fill.PointValue <= 0 || double.IsNaN(fill.PointValue) || double.IsInfinity(fill.PointValue)) {
				book.PointValueConflict = true;
				MarkReview("Point value was missing, so gross P&L is unknown");
				return;
			}
			if (!book.PointValueSet) {
				book.PointValue = fill.PointValue;
				book.PointValueSet = true;
				return;
			}
			if (book.PointValue != fill.PointValue) {
				book.PointValueConflict = true;
				MarkReview("Point value changed inside the round trip");
			}
		}

		private CycleDraft StartCycle(string instrument, string direction, DateTime time, string observation)
		{
			CycleDraft cycle = new CycleDraft();
			cycle.LocalId = "c" + nextCycleNumber.ToString(CultureInfo.InvariantCulture);
			nextCycleNumber++;
			cycle.Instrument = instrument;
			cycle.Direction = direction;
			cycle.Observation = observation;
			cycle.EntryTime = time;
			cycle.PnlBasis = GrossBasis;
			cycle.Allocations = new List<OrcaRulebookAllocationView>();
			return cycle;
		}

		private Book GetBook(string instrument)
		{
			Book book;
			if (!books.TryGetValue(instrument, out book)) {
				book = new Book();
				book.Identity = new Orca.SharedIdentity.Tracker();
				book.Completed = new List<CycleDraft>();
				books[instrument] = book;
			}
			return book;
		}

		private void MarkReview(string reason)
		{
			if (needsReview)
				return;
			needsReview = true;
			reviewReason = reason ?? "Evidence needs review";
		}

		private void Append(OrcaRulebookLedgerEvent ledgerEvent)
		{
			if (!replaying && events.Count >= MaxEvents) {
				if (!needsReview)
					MarkReview("Ledger event limit exceeded");
				return;
			}
			if (!replaying)
				events.Add(ledgerEvent);
		}

		private bool HasOpenCycle()
		{
			foreach (KeyValuePair<string, Book> pair in books) {
				if (pair.Value.Open != null)
					return true;
			}
			return false;
		}

		private bool HasIncompleteCycle()
		{
			return FirstIncompleteCycle() != null;
		}

		private OrcaRulebookCycleView FirstOpenCycle()
		{
			foreach (KeyValuePair<string, Book> pair in books) {
				if (pair.Value.Open != null)
					return pair.Value.Open.ToView();
			}
			return null;
		}

		private OrcaRulebookCycleView FirstIncompleteCycle()
		{
			foreach (KeyValuePair<string, Book> pair in books) {
				for (int i = 0; i < pair.Value.Completed.Count; i++) {
					CycleDraft cycle = pair.Value.Completed[i];
					if (!cycle.IdentityComplete || !cycle.PnlKnown || cycle.Observation != "Observed")
						return cycle.ToView();
				}
			}
			return null;
		}

		private OrcaRulebookCycleView FindCompleted(string instrument, string localId)
		{
			Book book;
			if (!books.TryGetValue(instrument, out book))
				return null;
			for (int i = 0; i < book.Completed.Count; i++) {
				if (book.Completed[i].LocalId == localId)
					return book.Completed[i].ToView();
			}
			return null;
		}

		private static bool AllocationsMatch(CycleDraft cycle, Orca.SharedIdentity.Cycle identity)
		{
			if (cycle == null || identity == null || cycle.Allocations.Count != identity.Allocations.Count)
				return false;
			for (int i = 0; i < cycle.Allocations.Count; i++) {
				OrcaRulebookAllocationView left = cycle.Allocations[i];
				Orca.SharedIdentity.Allocation right = identity.Allocations[i];
				if (left == null || right == null || !string.Equals(left.ExecutionId, right.ExecutionId, StringComparison.Ordinal) || left.SignedQuantity != right.SignedQuantity)
					return false;
			}
			return true;
		}

		private static bool IsReviewIdentityReason(string reason)
		{
			return reason == "Duplicate ID inside cycle"
				|| reason == "Invalid quantity"
				|| reason == "Identity allocation limit exceeded";
		}

		private static bool BadPrice(double price)
		{
			return double.IsNaN(price) || double.IsInfinity(price);
		}

		private static decimal ToDecimal(double value)
		{
			try {
				return (decimal)value;
			} catch (OverflowException) {
				return 0;
			}
		}

		private string RawKey(string executionId)
		{
			return Account + "\u001f" + executionId;
		}

		private static bool SameFill(OrcaRulebookLedgerEvent prior, OrcaRulebookFill fill)
		{
			if (prior == null || fill == null)
				return false;
			return string.Equals(prior.Instrument, fill.Instrument, StringComparison.Ordinal)
				&& prior.SignedQuantity == fill.SignedQuantity
				&& prior.Price == fill.Price
				&& prior.PointValue == fill.PointValue
				&& string.Equals(prior.Time, FormatTime(fill.Time), StringComparison.Ordinal)
				&& prior.HasPositionAfter == fill.PositionAfter.HasValue
				&& (!fill.PositionAfter.HasValue || prior.PositionAfter == fill.PositionAfter.Value)
				&& prior.Historical == fill.Historical;
		}

		private static OrcaRulebookLedgerEvent EventFromFill(OrcaRulebookFill fill, string kind, string reason)
		{
			OrcaRulebookLedgerEvent ledgerEvent = new OrcaRulebookLedgerEvent();
			ledgerEvent.Kind = kind;
			ledgerEvent.Account = fill.Account ?? string.Empty;
			ledgerEvent.Instrument = fill.Instrument ?? string.Empty;
			ledgerEvent.ExecutionId = fill.ExecutionId ?? string.Empty;
			ledgerEvent.SignedQuantity = fill.SignedQuantity;
			ledgerEvent.Price = fill.Price;
			ledgerEvent.PointValue = fill.PointValue;
			ledgerEvent.Time = FormatTime(fill.Time);
			ledgerEvent.HasPositionAfter = fill.PositionAfter.HasValue;
			ledgerEvent.PositionAfter = fill.PositionAfter.HasValue ? fill.PositionAfter.Value : 0;
			ledgerEvent.Historical = fill.Historical;
			ledgerEvent.Reason = reason ?? string.Empty;
			return ledgerEvent;
		}

		private static OrcaRulebookFill FillFromEvent(OrcaRulebookLedgerEvent stored)
		{
			OrcaRulebookFill fill = new OrcaRulebookFill();
			fill.Account = stored.Account;
			fill.Instrument = stored.Instrument;
			fill.ExecutionId = stored.ExecutionId;
			fill.SignedQuantity = stored.SignedQuantity;
			fill.Price = stored.Price;
			fill.PointValue = stored.PointValue;
			fill.Time = ParseTime(stored.Time);
			fill.PositionAfter = stored.HasPositionAfter ? (int?)stored.PositionAfter : null;
			fill.Historical = stored.Historical || stored.Kind == "Historical";
			return fill;
		}

		private static string FormatTime(DateTime time)
		{
			return time.ToString("o", CultureInfo.InvariantCulture);
		}

		private static DateTime ParseTime(string value)
		{
			DateTime parsed;
			if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
				return parsed;
			return DateTime.MinValue;
		}

		private static JavaScriptSerializer CreateSerializer()
		{
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			serializer.MaxJsonLength = int.MaxValue;
			return serializer;
		}

		private static string SafeFileToken(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "session";
			StringBuilder builder = new StringBuilder(value.Length);
			for (int i = 0; i < value.Length; i++) {
				char c = value[i];
				if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
					builder.Append(c);
				else
					builder.Append('_');
			}
			return builder.Length == 0 ? "session" : builder.ToString();
		}

		private sealed class Book
		{
			public int Net;
			public bool Seeded;
			public bool AwaitingFlat;
			public bool SawFill;
			public decimal Cash;
			public double PointValue;
			public bool PointValueSet;
			public bool PointValueConflict;
			public Orca.SharedIdentity.Tracker Identity;
			public CycleDraft Open;
			public List<CycleDraft> Completed;
		}

		private sealed class CycleDraft
		{
			public string LocalId;
			public string Instrument;
			public string Direction;
			public string Observation;
			public bool IdentityComplete;
			public string TradeUid;
			public string IdentityReason;
			public bool PnlKnown;
			public decimal GrossPnl;
			public string PnlBasis;
			public DateTime EntryTime;
			public DateTime ExitTime;
			public List<OrcaRulebookAllocationView> Allocations;

			public OrcaRulebookCycleView ToView()
			{
				List<OrcaRulebookAllocationView> copies = new List<OrcaRulebookAllocationView>();
				if (Allocations != null) {
					for (int i = 0; i < Allocations.Count; i++) {
						OrcaRulebookAllocationView source = Allocations[i];
						if (source == null)
							continue;
						copies.Add(new OrcaRulebookAllocationView { ExecutionId = source.ExecutionId, SignedQuantity = source.SignedQuantity });
					}
				}
				return new OrcaRulebookCycleView {
					LocalId = LocalId,
					Instrument = Instrument,
					Direction = Direction,
					Observation = Observation,
					IdentityComplete = IdentityComplete,
					TradeUid = TradeUid,
					IdentityReason = IdentityReason,
					PnlKnown = PnlKnown,
					GrossPnl = GrossPnl,
					PnlBasis = PnlBasis,
					Allocations = copies
				};
			}
		}
	}
}
