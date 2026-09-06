using System;
using System.IO;
using System.Linq;
using OrcaJournal.Data;
using OrcaJournal.Data.Models;

static class ReviewChecks
{
    public static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "orca-review-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        string path = Path.Combine(root, "review.db");
        int count = 0;
        Action<bool,string> check = (ok, message) => { if (!ok) throw new Exception(message); count++; };
        using (var db = new DatabaseManager(path))
        {
            db.Initialize(); db.Initialize();
            var trades = new TradeRepository(db); var tags = new TagRepository(db); var reviews = new TradeReviewRepository(db);
            var annotations = new ExecutionLineAnnotationRepository(db, trades, tags);
            var t = new Trade { TradeKey="review", Account="Sim", Instrument="MES", InstrumentFullName="MES SEP26", Direction="Long", EntryTime=DateTime.Today, ExitTime=DateTime.Today.AddMinutes(1), Quantity=1, SessionDate="2026-09-05", Notes="original", SetupGrade="B" }; trades.Insert(t);
            var tag = tags.GetOrCreateTag("chart tag", "#123456"); tags.ApplyTagToTrade(t.Id, tag.Id, false, "execution_lines");
            var attachments = new AttachmentRepository(db); attachments.Insert(new TradeAttachment { TradeId=t.Id,FilePath="keep.png",Kind="image",Source="journal",CreatedAt=DateTime.Now });
            var baseline = reviews.Read(t.Id);
            check(baseline.Notes=="original" && baseline.Tags=="chart tag" && baseline.Revision==0, "Import seed");
            var saved = reviews.Save(baseline, "Journal note", "A+", "setup\nSetup\n  learning  ");
            check(saved.Revision==1 && saved.Tags=="learning\nsetup", "Normalized tags and version");
            check(trades.GetById(t.Id).Notes=="original", "Raw imported note unchanged");
            check(trades.GetAll().Single().Notes=="Journal note", "Effective list uses Journal note");
            check(tags.GetTagsForTrade(t.Id).Select(x=>x.Name).SequenceEqual(new[]{"learning","setup"}), "Effective tags");
            bool rejected=false; try { reviews.Save(baseline,"stale","A",""); } catch(InvalidOperationException) { rejected=true; }
            check(rejected && reviews.Read(t.Id).Notes=="Journal note", "Stale editor cannot overwrite");
            annotations.SaveAnnotation(t.TradeKey,t.Account,t.InstrumentFullName,t.Direction,t.EntryTime,t.ExitTime,"chart changed",new[]{"new chart tag"},"C");
            var conflict=reviews.Read(t.Id); check(conflict.HasConflict && conflict.ImportedNotes=="chart changed" && conflict.Notes=="Journal note", "Chart changes retained separately");
            rejected=false; try { reviews.Save(saved,"draft","A",""); } catch(InvalidOperationException) { rejected=true; }
            check(rejected,"Concurrent import blocks save");
            rejected=false; try { reviews.Save(conflict,"draft","A",""); } catch(InvalidOperationException) { rejected=true; }
            check(rejected,"Unacknowledged conflict blocks save");
            var resolved=reviews.Save(conflict,"merged","A-","keep",true); check(!resolved.HasConflict && resolved.Revision==2,"Explicit resolution rebases");
            annotations.SaveAnnotation(t.TradeKey,t.Account,t.InstrumentFullName,t.Direction,t.EntryTime,t.ExitTime,"chart changed",new[]{"new chart tag"},"C");
            check(!reviews.Read(t.Id).HasConflict,"Repeated identical import is not a conflict");
            var cleared=reviews.Save(reviews.Read(t.Id),"","",""); check(cleared.Notes=="" && trades.GetAll().Single().Notes=="" && tags.GetTagsForTrade(t.Id).Count==0,"Intentional empty review persists");
            using(var cmd=db.Connection.CreateCommand()) { cmd.CommandText="SELECT count(*) FROM journal_review_history"; check(Convert.ToInt32(cmd.ExecuteScalar())==3,"Saved history preserved"); }
            check(attachments.GetForTrade(t.Id).Single().FilePath=="keep.png","Attachment untouched");
            check(trades.GetById(t.Id).SetupGrade=="C", "Imported grade unchanged by review");
            annotations.SaveAnnotation(t.TradeKey,t.Account,t.InstrumentFullName,t.Direction,t.EntryTime,t.ExitTime,"",new string[0],"");
            check(reviews.Read(t.Id).HasConflict && reviews.Read(t.Id).ImportedNotes=="" && reviews.Read(t.Id).Revision==3, "Explicit chart clear becomes visible without deleting Journal review");
            rejected=false; try { reviews.Save(cleared,"bad","X",""); } catch(ArgumentException) { rejected=true; } check(rejected,"Invalid grade rejected");
        }
        using(var db=new DatabaseManager(path)) { db.Initialize(); var read=new TradeReviewRepository(db).Read(1); check(read.Revision==3 && read.Notes=="", "Reopen preserves review and clear intent"); }
        Console.WriteLine("PASS: " + count + " Journal review assertions (disposable database).");
    }
}
