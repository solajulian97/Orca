using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using OrcaJournal.Core;
using OrcaJournal.Data;
using OrcaJournal.Data.Models;
using NinjaTrader.Cbi;

class Program
{
    static int assertions;
    static DateTime time = new DateTime(2026, 9, 5, 14, 0, 0, DateTimeKind.Utc);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); assertions++; }
    static ExecutionProvenance P(params int[] quantities)
    {
        return new ExecutionProvenance { Account = "Sim", Instrument = "MNQ SEP26", CompleteHistory = true,
            Allocations = quantities.Select((q, i) => new ExecutionAllocation { ExecutionId = "e" + i, SignedQuantity = q }).ToList() };
    }
    static Trade T(string key, ExecutionProvenance p = null)
    {
        return new Trade { TradeKey = key, Account = "Sim", Instrument = "MNQ", InstrumentFullName = "MNQ SEP26",
            Direction = "Long", Quantity = 2, EntryTime = time, ExitTime = time.AddMinutes(1), EntryPrice = 100, ExitPrice = 101,
            PnlDollars = 4, SessionDate = "2026-09-05", Notes = "keep notes", SetupGrade = "A", ProvenanceJson = ExecutionIdentity.Serialize(p) };
    }
    static Execution Fill(string id, bool buy, int q, double price, string contract = "MNQ SEP26")
    { return new Execution { ExecutionId = id, Quantity = q, Price = price, Time = time,
        MarketPosition = buy ? MarketPosition.Long : MarketPosition.Short, Order = new Order { OrderAction = buy ? OrderAction.Buy : OrderAction.Sell },
        Instrument = new Instrument { FullName = contract, MasterInstrument = new MasterInstrument { Name = "MNQ" } } }; }
    static object Scalar(DatabaseManager db, string sql)
    { using (var cmd = db.Connection.CreateCommand()) { cmd.CommandText = sql; return cmd.ExecuteScalar(); } }
    static string B64(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s)); }

    static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "orca-identity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { TagPerformanceChecks.Run(); ExcursionChecks.Run(); RiskMediaChecks.Run(); ReviewChecks.Run(); Run(root); assertions += SharedIdentityTests.Run(root); Console.WriteLine("PASS: " + assertions + " assertions; disposable artifacts: " + root); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); Console.Error.WriteLine("Fixtures: " + root); return 1; }
    }
    static void Run(string root)
    {
        var p = P(2, 1, -1, -2); string uid = ExecutionIdentity.Build(p);
        Check(ExecutionIdentity.Build(P(2,-2)) == "orca.trade.v1:65428ff6137f4582a84ef92b2747bf6ed061344f0b87aff81a7efdab697e1e3a", "independent Python binary contract golden vector");
        Check(uid != null, "scaled complete cycle has UID");
        Check(uid == ExecutionIdentity.Build(ExecutionIdentity.Parse(ExecutionIdentity.Serialize(p))), "serialization and restart deterministic");
        var culture = CultureInfo.CurrentCulture; CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        Check(uid == ExecutionIdentity.Build(p), "culture invariant UID"); CultureInfo.CurrentCulture = culture;
        p.Account = "Other"; Check(uid != ExecutionIdentity.Build(p), "account isolation"); p.Account = "Sim";
        p.Instrument = "MNQ DEC26"; Check(uid != ExecutionIdentity.Build(p), "full contract isolation"); p.Instrument = "MNQ SEP26";
        p.Allocations[0].ExecutionId = "different"; Check(uid != ExecutionIdentity.Build(p), "same timestamp prices distinct IDs");
        p = P(2, -2); p.CompleteHistory = false; Check(ExecutionIdentity.Build(p) == null, "unknown history no UID");
        p.CompleteHistory = true; p.Allocations[0].ExecutionId = null; Check(ExecutionIdentity.Build(p) == null, "missing ID no UID");
        p = P(2, -2); p.Allocations[1].ExecutionId = "e0"; Check(ExecutionIdentity.Build(p) == null, "duplicate ID rejected");
        Check(ExecutionIdentity.Build(P(2, -3, 1)) == null, "unsplit reversal rejected");
        Check(ExecutionIdentity.Build(P(1, -1, 1, -1)) == null, "two cycles not one UID");
        Check(ExecutionIdentity.Build(P(1, 1)) == null, "open cycle rejected");
        Check(ExecutionIdentity.Build(P(2, -2)) != ExecutionIdentity.Build(P(1, -1)), "allocated quantities affect UID");
        var builder = new TradeBuilder(); var completed = new List<Trade>(); builder.TradeCompleted += completed.Add;
        var capture = new TradeCapture(builder); var account = new Account { Name = "Sim" }; capture.Attach(account); capture.Attach(account);
        account.Deliver(Fill("1", true, 2, 100)); account.Deliver(Fill("2", true, 1, 110)); account.Deliver(Fill("3", false, 1, 115));
        Check(completed.Count == 0, "partial exit remains open");
        account.Deliver(Fill("4", false, 3, 120)); account.Deliver(Fill("4", false, 3, 120));
        Check(completed.Count == 1 && completed[0].Quantity == 3 && completed[0].PnlDollars == 90, "scaled reversal closes once");
        account.Deliver(Fill("5", true, 1, 110));
        Check(completed.Count == 2 && completed[1].Direction == "Short" && completed[1].Quantity == 1 && completed[1].PnlDollars == 20, "reversal remainder preserved");
        var close = ExecutionIdentity.Parse(completed[0].ProvenanceJson); var reverse = ExecutionIdentity.Parse(completed[1].ProvenanceJson);
        Check(close.Allocations.Last().SignedQuantity == -2 && reverse.Allocations.First().SignedQuantity == -1, "reversal allocated portions");
        var recorder = new RecorderFixture.OrcaRecorderTradeLedger();
        recorder.Fill("Sim","MNQ SEP26","1",true,2,100,2,time);
        recorder.Fill("Sim","MNQ SEP26","2",true,1,110,2,time);
        recorder.Fill("Sim","MNQ SEP26","3",false,1,115,2,time);
        recorder.Fill("Sim","MNQ SEP26","4",false,3,120,2,time);
        recorder.Fill("Sim","MNQ SEP26","4",false,3,120,2,time);
        recorder.Fill("Sim","MNQ SEP26","5",true,1,110,2,time);
        var recorded = recorder.DrainCompleted();
        Check(recorded.Count == 2 && (double)recorded[0].GrossPnl == completed[0].PnlDollars && (double)recorded[1].GrossPnl == completed[1].PnlDollars, "actual recorder and Journal grouped PnL parity");
        Check(recorded[0].ExecutionIds.SequenceEqual(close.Allocations.Select(a=>a.ExecutionId)) && recorded[1].ExecutionIds.SequenceEqual(reverse.Allocations.Select(a=>a.ExecutionId)), "actual recorder order and reversal ID parity");
        Check(ExecutionIdentity.Build(close) == null && ExecutionIdentity.Build(reverse) == null, "live continuity never assumed");
        var other = new Account { Name = "Other" }; capture.Attach(other);
        other.Deliver(Fill("1", true, 1, 100)); other.Deliver(Fill("2", false, 1, 100));
        Check(completed.Count == 3 && completed[2].Account == "Other", "same IDs other account survive dedupe");
        account.Deliver(Fill("6", true, 1, 100, "MNQ DEC26")); account.Deliver(Fill("7", false, 1, 101, "MNQ DEC26"));
        Check(completed.Last().InstrumentFullName == "MNQ DEC26", "capture full-contract bucket");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); DateTime utc;
        Check(!TradeReconciliation.TryUtc(new DateTime(2026,11,1,1,30,0), zone, out utc), "DST overlap rejected");
        Check(!TradeReconciliation.TryUtc(new DateTime(2026,3,8,2,30,0), zone, out utc), "DST gap rejected");
        Check(TradeReconciliation.TryUtc(new DateTime(2026,9,5,10,0,0), zone, out utc) && utc == time, "explicit local zone conversion");
        Check(TradeReconciliation.TryUtc(time, zone, out utc) && utc == time, "UTC unchanged");
        string video = Path.Combine(root,"clip.mp4"); File.WriteAllText(video,"fixture");
        string dbPath = Path.Combine(root, "test.db");
        // Baseline schema 5: use exact pre-change manager linked by Verify.ps1.
        using (var old = new Baseline.DatabaseManager(dbPath))
        {
            old.Initialize();
            using (var cmd = old.Connection.CreateCommand())
            { cmd.CommandText = @"INSERT INTO trades (trade_key,instrument,instrument_full_name,direction,entry_time,exit_time,entry_price,exit_price,quantity,pnl_dollars,notes,setup_grade,session_date,account)
 VALUES ('old','MNQ','MNQ SEP26','Long','2026-09-05T14:00:00Z','2026-09-05T14:01:00Z',100,101,2,4,'old note','A','2026-09-05','Sim');
 INSERT INTO tags (name,color) VALUES ('Keep','#fff'); INSERT INTO trade_tags (trade_id,tag_id,source) VALUES (1,1,'manual');
 INSERT INTO trade_attachments (trade_id,file_path,kind,created_at) VALUES (1,'missing.png','image','2026-09-05');
 INSERT INTO execution_line_annotations (trade_key,notes,setup_grade,updated_at) VALUES ('pending','pending note','B','2026-09-05');
 INSERT INTO execution_line_hidden_tags VALUES ('Hidden');"; cmd.ExecuteNonQuery(); }
        }
        using (var db = new DatabaseManager(dbPath))
        {
            db.Initialize(); db.Initialize();
            Check(Convert.ToInt32(Scalar(db,"SELECT version FROM schema_version")) == 10, "schema5 to10 idempotent migration");
            Check((string)Scalar(db,"SELECT notes FROM trades WHERE id=1") == "old note" && (string)Scalar(db,"SELECT setup_grade FROM trades WHERE id=1") == "A", "legacy notes grade retained");
            Check(Convert.ToInt32(Scalar(db,"SELECT COUNT(*) FROM trade_tags")) == 1 && Convert.ToInt32(Scalar(db,"SELECT COUNT(*) FROM trade_attachments")) == 1, "tags images retained");
            Check(Convert.ToInt32(Scalar(db,"SELECT COUNT(*) FROM execution_line_hidden_tags")) == 1, "hidden library retained");
            var repo = new TradeRepository(db); var t = T("new",P(2,-2)); Check(repo.Insert(t), "new provenance trade inserted");
            Check(repo.GetById(t.Id).TradeUid == ExecutionIdentity.Build(P(2,-2)), "UID persisted");
            Check(!repo.Insert(T("new",P(2,-2))), "duplicate import no row");
            var saved = repo.GetById(t.Id); saved.Notes = "edited"; repo.Update(saved);
            Check(repo.GetById(t.Id).TradeUid == t.TradeUid, "ordinary annotation update retains identity");
            var tags = new TagRepository(db); var annotations = new ExecutionLineAnnotationRepository(db,repo,tags);
            string key = TradeIdentity.Build("Sim","MNQ SEP26",true,time,time.AddMinutes(1),2,2,100,101);
            annotations.SaveAnnotation(key,"Sim","MNQ SEP26","Long",time,time.AddMinutes(1),"proposal",new[]{"Setup"},"B");
            string tsv = Path.Combine(root,"notes.tsv");
            File.WriteAllText(tsv,"IDENTITY_V1\t" + B64(key) + "\t" + B64(ExecutionIdentity.Serialize(P(2,-2))) + "\n");
            ExecutionLineNotesImporter.Import(tsv,annotations); ExecutionLineNotesImporter.Import(tsv,annotations);
            Check(Convert.ToInt32(Scalar(db,"SELECT COUNT(*) FROM annotation_identity")) == 1, "identity TSV repeated idempotent");
            Check(repo.GetById(t.Id).Notes == "edited", "proposed identity match never applies notes");
            string legacyTsv = Path.Combine(root,"legacy.tsv");
            File.WriteAllText(legacyTsv,"TRADE\t"+B64("unmatched")+"\t"+B64("legacy note")+"\t"+B64("tag")+"\t"+B64("C")+"\nTAG\t"+B64("Library")+"\nHIDDEN_TAG\t"+B64("Hidden2"));
            Check(ExecutionLineNotesImporter.Import(legacyTsv,annotations) == 3, "legacy TSV still readable");
            var ledger = new TradeRecordingImporter.RecorderTrade { Account="Sim",Instrument="MNQ SEP26",Direction="LONG",EntryQuantity=2,EntryTime=time,ExitTime=time.AddMinutes(1),IsCompleteHistory=true,ExecutionIds=new List<string>{"e0","e1"} };
            var manifest = new TradeRecordingImporter.RecorderManifest { SchemaVersion=2,FinalizationStatus="Complete",FinalVideoPath=video,StartedUtc=time,StoppedUtc=time.AddMinutes(1),Trades=new List<TradeRecordingImporter.RecorderTrade>{ledger} };
            var candidates = new List<Trade>{repo.GetById(t.Id)};
            Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"fixture",zone).Single().Status == "Legacy", "schema2 IDs not fabricated exact UID");
            ledger.ExecutionIds=new List<string>{"conflicting","exit"};
            Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"fixture",zone).Single().Status == "Ambiguous", "conflicting IDs cannot become time-only match");
            ledger.ExecutionIds=new List<string>{"e0","e1"};
            var wrong = T("wrong",P(2,-2)); wrong.Id=99; wrong.Account="Other"; candidates.Add(wrong);
            Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"fixture",zone).Single().Candidates == t.Id.ToString(), "schema2 account-instrument pairing");
            wrong.Account="Sim"; wrong.InstrumentFullName="MNQ DEC26";
            Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"fixture",zone).Single().Candidates == t.Id.ToString(), "no short contract fallback");
            wrong.InstrumentFullName="MNQ SEP26";
            Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"fixture",zone).Single().Status == "Ambiguous", "duplicate candidates explicit");
            candidates.Remove(wrong); manifest.HasIncompleteTrades=true;
            Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"fixture",zone).Single().Status == "Ambiguous", "incomplete manifest fails closed"); manifest.HasIncompleteTrades=false;
            manifest.Trades.Clear(); Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"fixture",zone).Single().Status == "Unmatched", "schema2 no ledger no broad fallback"); manifest.Trades.Add(ledger);
            manifest.SchemaVersion=1; manifest.Accounts=new List<string>{"Sim"}; manifest.Instruments=new List<string>{"MNQ SEP26"};
            Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"fixture",zone).Single().Status == "Legacy", "schema1 compatible review"); manifest.SchemaVersion=2;
            // Serialize with the recorder's actual serializer, deserialize through the report reader.
            File.WriteAllText(Path.Combine(root,"capture.json"),new JavaScriptSerializer().Serialize(manifest));
            Scalar(db,"PRAGMA query_only=ON");
            long changes = Convert.ToInt64(Scalar(db,"SELECT total_changes()"));
            var rows = TradeReconciliation.Read(db,root); var again = TradeReconciliation.Read(db,root);
            Check(rows.Any(r=>r.Source=="Annotation" && r.Status=="Exact"), "exact annotation proposal visible");
            Check(rows.Any(r=>r.Source=="Recorder" && r.Status=="Legacy"), "real recorder JSON compatible");
            Check(rows.Any(r=>r.Source=="Existing media" && r.Status=="Unmatched"), "missing screenshot link visible and retained");
            Check(rows.Count==again.Count && changes==Convert.ToInt64(Scalar(db,"SELECT total_changes()")), "repeated report query-only no data mutations");
            Scalar(db,"PRAGMA query_only=OFF");
            var attachments = new AttachmentRepository(db);
            long countBefore = Convert.ToInt64(Scalar(db,"SELECT COUNT(*) FROM trade_attachments"));
            Check(TradeRecordingImporter.Import(repo.GetAll(),attachments,root) == 0 && TradeRecordingImporter.Import(repo.GetAll(),attachments,root) == 0, "schema2 import remains read-only repeatedly");
            Check(Convert.ToInt64(Scalar(db,"SELECT COUNT(*) FROM trade_attachments")) == countBefore, "schema2 preserves attachment rows");
            manifest.SchemaVersion=1;
            File.WriteAllText(Path.Combine(root,"capture.json"),new JavaScriptSerializer().Serialize(manifest));
            Check(TradeRecordingImporter.Import(repo.GetAll(),attachments,root) > 0, "schema1 legacy import remains available");
            Check(TradeRecordingImporter.Import(repo.GetAll(),attachments,root) == 0, "schema1 import idempotent");
            manifest.SchemaVersion=99;
            Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"future",zone).Single().Status=="Unmatched", "future schema fails closed");
            manifest.SchemaVersion=2; manifest.FinalVideoPath=Path.Combine(root,"moved.mp4");
            Check(TradeReconciliation.EvaluateManifest(manifest,candidates,"moved",zone).Single().Status=="Unmatched", "missing video fails closed");
            var bad=P(2,-2); bad.Account="Other";
            annotations.SaveIdentity(key,ExecutionIdentity.Serialize(bad));
            Check((string)Scalar(db,"SELECT trade_uid FROM annotation_identity WHERE trade_key='"+key+"'") == ExecutionIdentity.Build(P(2,-2)), "mismatched weaker annotation scope cannot overwrite trusted UID");
            annotations.SaveIdentity(key,ExecutionIdentity.Serialize(P(2,-2)));
            var dup=T(null,P(2,-2)); repo.Insert(dup);
            Check(TradeReconciliation.Read(db,root).Any(r=>r.Source=="Annotation" && r.Reference==key && r.Status=="Ambiguous"), "duplicate UID candidates not silently chosen");
            repo.Delete(dup.Id);
            Check(repo.GetById(dup.Id)==null, "existing explicit delete not blocked by additive provenance FK");
        }
        using (var db = new DatabaseManager(dbPath)) { db.Initialize(); Check(new TradeRepository(db).GetAll().Any(t=>t.TradeUid!=null),"identity survives reopen"); }
        using (var db = new DatabaseManager(dbPath,true))
        {
            Check(TradeReconciliation.Read(db,root).Count>0,"dedicated read-only connection works");
            bool rejected=false; try { Scalar(db,"UPDATE trades SET notes='bad'"); } catch(System.Data.SQLite.SQLiteException) { rejected=true; }
            Check(rejected,"dedicated report connection rejects writes");
        }
    }
}
