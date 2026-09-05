using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NinjaTrader.Cbi;
using OrcaJournal.Core;
using OrcaJournal.Data;
using OrcaJournal.Data.Models;
using Orca.SharedIdentity;

static class SharedIdentityTests
{
    static int count;
    static void Check(bool value,string name) { if(!value) throw new Exception(name); count++; }
    public static int Run(string root)
    {
        DateTime now=new DateTime(2026,9,5,14,0,0,DateTimeKind.Utc);
        var builder=new TradeBuilder(); var trades=new List<Trade>(); builder.TradeCompleted+=trades.Add;
        var capture=new TradeCapture(builder); var account=new Account {Name="Shared"}; capture.Attach(account);
        var ledger=new RecorderFixture.OrcaRecorderTradeLedger(); var chart=new Tracker();
        var chartCycles=new List<Cycle>();
        Action<string,int,int,double> fill=(id,qty,post,price)=>{
            var execution=new Execution {ExecutionId=id,Quantity=Math.Abs(qty),Position=post,Price=price,Time=now,
                Instrument=new Instrument {FullName="MNQ SEP26",MasterInstrument=new MasterInstrument{Name="MNQ"}},
                Order=new Order {OrderAction=qty>0?OrderAction.Buy:OrderAction.Sell},MarketPosition=qty>0?MarketPosition.Long:MarketPosition.Short};
            account.Deliver(execution);
            ledger.Fill("Shared","MNQ SEP26",id,qty>0,Math.Abs(qty),price,2,now,post);
            var complete=chart.Fill("Shared","MNQ SEP26",id,qty,now,post); if(complete!=null) chartCycles.Add(complete);
        };
        fill("warm-entry",1,1,100); fill("warm-exit",-1,0,101);
        var recorded=ledger.DrainCompleted();
        Check(ExecutionIdentity.Build(ExecutionIdentity.Parse(trades[0].ProvenanceJson))==null && recorded[0].TradeUid==null && chartCycles[0].Uid==null,"first observed cycle unverified in all producers");
        fill("entry",2,2,100);fill("scale",1,3,110);fill("partial",-1,2,115);fill("reverse",-3,-1,120);fill("flat",1,0,110);
        recorded=ledger.DrainCompleted();
        Check(recorded.Count==2 && trades.Count==3 && chartCycles.Count==3,"three producers split same two cycles");
        for(int i=0;i<2;i++)
        {
            var provenance=ExecutionIdentity.Parse(trades[i+1].ProvenanceJson);
            string uid=ExecutionIdentity.Build(provenance);
            Check(uid!=null && uid==recorded[i].TradeUid && uid==chartCycles[i+1].Uid,"same complete UID across Journal recorder chart");
            Check(ExecutionIdentity.Serialize(ExecutionIdentity.Parse(recorded[i].IdentityJson))==trades[i+1].ProvenanceJson,"equivalent schema serialized across producers");
        }
        Check(chartCycles[1].Allocations.Last().SignedQuantity==-2 && chartCycles[2].Allocations.First().SignedQuantity==-1,"shared reversal allocation contract");
        // Invalidation during an open trade preserves calculations but removes identity certainty.
        fill("disconnect-entry",1,1,100); Connection.Changed(); ledger.InvalidateIdentity("connection");chart.Invalidate("connection");
        fill("disconnect-exit",-1,0,100);recorded=ledger.DrainCompleted();
        Check(recorded.Single().TradeUid==null && chartCycles.Last().Uid==null && ExecutionIdentity.Build(ExecutionIdentity.Parse(trades.Last().ProvenanceJson))==null,"connection event invalidates active cycles");
        fill("recovered-entry",1,1,100);fill("recovered-exit",-1,0,100);recorded=ledger.DrainCompleted();
        Check(recorded.Single().TradeUid!=null && recorded[0].TradeUid==chartCycles.Last().Uid && recorded[0].TradeUid==ExecutionIdentity.Build(ExecutionIdentity.Parse(trades.Last().ProvenanceJson)),"next cycle recovers after observed flat");
        int before=trades.Count;
        account.Deliver(new Execution {ExecutionId="recovered-exit"});
        Check(trades.Count==before,"repeat delivery remains deduplicated");
        // Position mismatch, missing IDs, timestamp regression and missing history are not proof.
        var t=new Tracker();t.Fill("A","MNQ", "w1",1,now,1);t.Fill("A","MNQ","w2",-1,now,0);
        t.Fill("A","MNQ","m1",1,now,4); var cycle=t.Fill("A","MNQ","m2",-1,now,0);
        Check(cycle.Uid==null,"position discontinuity fails closed");
        t.Fill("A","MNQ",null,1,now,1);cycle=t.Fill("A","MNQ","missing-exit",-1,now,0);
        Check(cycle.Uid==null,"missing execution ID fails closed");
        t.Fill("A","MNQ","time1",1,now.AddSeconds(1),1);cycle=t.Fill("A","MNQ","time2",-1,now,0);
        Check(cycle.Uid==null,"out-of-order timestamps fail closed");
        t.Fill("A","MNQ","legacy1",1,now,null);cycle=t.Fill("A","MNQ","legacy2",-1,now,null);
        Check(cycle.Uid==null,"historical arithmetic flat without execution position is not proof");
        var restart=new Tracker();restart.Fill("A","MNQ","restart1",1,now,1);cycle=restart.Fill("A","MNQ","restart2",-1,now,0);
        Check(cycle.Uid==null,"restart requires new flat boundary");
        var quote=new Cycle {Account="A\"\\\t\n😀",Instrument="MNQ SEP26",CompleteHistory=true};quote.Allocations.Add(new Allocation {ExecutionId="id\"",SignedQuantity=1});quote.Allocations.Add(new Allocation{ExecutionId="exit",SignedQuantity=-1});
        Check(ExecutionIdentity.Build(ExecutionIdentity.Parse(Contract.Json(quote)))==quote.Uid,"shared JSON escaping and unicode roundtrip");
        string video=Path.Combine(root,"shared.mp4");File.WriteAllText(video,"fixture");
        using(var db=new DatabaseManager(Path.Combine(root,"shared.db")))
        {
            db.Initialize();var repo=new TradeRepository(db);var trade=trades[1];repo.Insert(trade);
            Check(repo.GetById(trade.Id).TradeUid==chartCycles[1].Uid,"live-proven UID persists");
            var model=new TradeRecordingImporter.RecorderTrade {Account=trade.Account,Instrument=trade.InstrumentFullName,Direction=trade.Direction,
                EntryQuantity=trade.Quantity,IsCompleteHistory=true,IdentityJson=trade.ProvenanceJson,TradeUid=trade.TradeUid,
                ExecutionIds=ExecutionIdentity.Parse(trade.ProvenanceJson).Allocations.Select(a=>a.ExecutionId).ToList()};
            var manifest=new TradeRecordingImporter.RecorderManifest {SchemaVersion=3,FinalizationStatus="Complete",FinalVideoPath=video,Trades=new List<TradeRecordingImporter.RecorderTrade>{model}};
            var rows=TradeReconciliation.EvaluateManifest(manifest,repo.GetAll(),"shared",TimeZoneInfo.Utc);
            Check(rows.Single().Status=="Exact","schema3 full identity proposes exact trade");
            model.TradeUid="tampered";
            Check(TradeReconciliation.EvaluateManifest(manifest,repo.GetAll(),"shared",TimeZoneInfo.Utc).Single().Status=="Unmatched","schema3 supplied UID must verify");model.TradeUid=trade.TradeUid;
            model.EntryQuantity++;
            Check(TradeReconciliation.EvaluateManifest(manifest,repo.GetAll(),"shared",TimeZoneInfo.Utc).Single().Status=="Unmatched","schema3 quantity scope verifies");model.EntryQuantity--;
            manifest.HasIncompleteTrades=true;
            Check(TradeReconciliation.EvaluateManifest(manifest,repo.GetAll(),"shared",TimeZoneInfo.Utc).Single().Status=="Unmatched","schema3 incomplete manifest no fallback");manifest.HasIncompleteTrades=false;
            var tags=new TagRepository(db);var annotations=new ExecutionLineAnnotationRepository(db,repo,tags);
            annotations.SaveAnnotation(trade.TradeKey,trade.Account,trade.InstrumentFullName,trade.Direction,trade.EntryTime,trade.ExitTime,"keep",new[]{"Setup"},"A");
            annotations.SaveIdentity(trade.TradeKey,trade.ProvenanceJson);
            var alternate=ExecutionIdentity.Parse(trade.ProvenanceJson);alternate.Allocations[0].ExecutionId="different-real-fill";
            annotations.SaveIdentity(trade.TradeKey,ExecutionIdentity.Serialize(alternate));annotations.SaveIdentity(trade.TradeKey,trade.ProvenanceJson);
            Check(TradeReconciliation.Read(db,root).Any(r=>r.Source=="Annotation" && r.Reference==trade.TradeKey && r.Status=="Ambiguous"),"multiple chart identities conflict persistently instead of last-writer matching");
            Check(repo.GetById(trade.Id).Notes=="keep" && repo.GetById(trade.Id).SetupGrade=="A","conflicting identity evidence preserves annotations");
        }
        capture.DetachAll();
        return count;
    }
}
