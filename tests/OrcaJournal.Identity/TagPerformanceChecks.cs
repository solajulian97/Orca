using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OrcaJournal.Analytics;
using OrcaJournal.Data;
using OrcaJournal.Data.Models;
static class TagPerformanceChecks
{
 static int count;static void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;}
 static TaggedTrade Row(int id,double pnl,double? risk,params string[] tags)=>new TaggedTrade{Trade=new Trade{Id=id,PnlDollars=pnl,PlannedRisk=risk},Tags=new HashSet<string>(tags,StringComparer.OrdinalIgnoreCase)};
 public static void Run()
 {
  var rows=new[]{Row(1,200,100,"Sweep","FVG"),Row(2,-100,100,"Sweep"),Row(3,50,null,"FVG"),Row(4,0,null)};
  var all=TagPerformance.Calculate(rows,new[]{"sweep","FVG","SWEEP"},true);Check(all.Summary.Count==1&&all.Summary.Net==200,"all tag intersection with case-insensitive deduplication");Check(all.Summary.AverageR==2,"realized R uses planned risk");
  var any=TagPerformance.Calculate(rows,new[]{"Sweep","FVG"},false);Check(any.Summary.Count==3 && any.Summary.Net==150,"any tag union counted once");Check(any.Overlaps.Count==3 && any.Overlaps.Sum(x=>x.Count)==3,"observed overlap partitions are disjoint");Check(any.Summary.WinRate==2.0/3 && any.Summary.Average==50,"net average and win rate");Check(any.Summary.RCount==2 && any.Summary.AverageR==0.5,"missing risk excluded not zero");
  var none=TagPerformance.Calculate(rows,new string[0],true);Check(none.Summary.Count==4&&none.Summary.WinRate==0.5,"untagged and breakeven included in baseline");Check(none.Overlaps.Count==1,"no-selection single baseline group");
  var empty=TagPerformance.Calculate(rows,new[]{"missing"},true);Check(empty.Summary.Count==0&&!empty.Summary.WinRate.HasValue&&!empty.Summary.AverageR.HasValue,"empty selection result unknown stats");
  Check(TagPerformance.Calculate(rows,new[]{"Sweep"},true).Summary.Count==2,"extra tags allowed in intersection");
  var many=Enumerable.Range(0,70).Select(x=>"Tag "+x).ToArray();Check(TagPerformance.Calculate(new[]{Row(5,1,1,many)},many,true).Summary.Count==1,"more than64 selected tags supported");
  Check(!TagPerformance.Summarize(new[]{Row(8,1,0),Row(9,1,double.NaN)},"").AverageR.HasValue,"invalid risk excluded");
  string path=Path.Combine(Path.GetTempPath(),"orca-tag-performance-"+Guid.NewGuid().ToString("N"),"fixture.db");
  using(var db=new DatabaseManager(path))
  {
   db.Initialize();var repo=new TradeRepository(db);var tags=new TagRepository(db);
   var t=new Trade{TradeKey="tags",Account="A",Instrument="MES",InstrumentFullName="MES SEP26",Direction="Long",Quantity=1,EntryTime=DateTime.Today,ExitTime=DateTime.Today,SessionDate="2026-09-06",PnlDollars=200};repo.Insert(t);
   tags.ApplyTagToTrade(t.Id,tags.GetOrCreateTag("Sweep","#ffffff").Id,false);
   Check(new TagPerformanceRepository(db).Load().Single().Tags.Contains("Sweep"),"imported/manual tags loaded");
   using(var cmd=db.Connection.CreateCommand()){cmd.CommandText="INSERT INTO journal_reviews(trade_id,revision,notes,grade,tags,base_snapshot,updated_at,planned_risk) VALUES ("+t.Id+",1,'','','FVG','', 'now',100)";cmd.ExecuteNonQuery();}
   var snap=new TagPerformanceRepository(db).Load();Check(snap.Single().Tags.SetEquals(new[]{"FVG"}),"review tag override replaces base");Check(snap.Single().Trade.PlannedRisk==100,"review risk overlay included");
   using(var cmd=db.Connection.CreateCommand()){cmd.CommandText="UPDATE journal_reviews SET tags=''";cmd.ExecuteNonQuery();}
   Check(new TagPerformanceRepository(db).Load().Single().Tags.Count==0,"intentional empty override respected");
  }
  Console.WriteLine("PASS: "+count+" tag-performance assertions");
 }
}
