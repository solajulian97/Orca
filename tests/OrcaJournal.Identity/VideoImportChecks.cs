using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using OrcaJournal.Core;
using OrcaJournal.Data;
using OrcaJournal.Data.Models;
internal static class VideoImportChecks
{
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
 static void Write(string root,TradeRecordingImporter.RecorderManifest m){using(var f=File.Create(Path.Combine(root,"capture.json")))new DataContractJsonSerializer(typeof(TradeRecordingImporter.RecorderManifest)).WriteObject(f,m);}
 public static void Run(){
  string root=Path.Combine(Path.GetTempPath(),"orca-video-import-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  using(var db=new DatabaseManager(Path.Combine(root,"fixture.db"))){db.Initialize();var repo=new TradeRepository(db);var media=new AttachmentRepository(db);
   var evidence=new ExecutionProvenance{Account="SIM",Instrument="MES SEP26",CompleteHistory=true,Allocations=new List<ExecutionAllocation>{new ExecutionAllocation{ExecutionId="entry",SignedQuantity=2},new ExecutionAllocation{ExecutionId="exit",SignedQuantity=-2}}};
   var trade=new Trade{Account=evidence.Account,Instrument="MES",InstrumentFullName=evidence.Instrument,Direction="Long",Quantity=2,EntryTime=DateTime.Today,ExitTime=DateTime.Today.AddMinutes(1),SessionDate=DateTime.Today.ToString("yyyy-MM-dd"),PnlDollars=50,Notes="preserve",ProvenanceJson=ExecutionIdentity.Serialize(evidence)};repo.Insert(trade);
   var video=Path.Combine(root,"clip.mp4");File.WriteAllText(video,"fixture");
   var ledger=new TradeRecordingImporter.RecorderTrade{Account=trade.Account,Instrument=trade.InstrumentFullName,Direction=trade.Direction,EntryQuantity=2,IsCompleteHistory=true,IdentityJson=trade.ProvenanceJson,TradeUid=trade.TradeUid,ExecutionIds=new List<string>{"entry","exit"}};
   var m=new TradeRecordingImporter.RecorderManifest{SchemaVersion=3,CaptureId="test",FinalizationStatus="Pending",StartedUtc=DateTime.UtcNow.AddMinutes(-2),StoppedUtc=DateTime.UtcNow,FinalVideoPath=video,Trades=new List<TradeRecordingImporter.RecorderTrade>{ledger}};
   Write(root,m);var report=new RecordingImportReport();Check(TradeRecordingImporter.Import(repo.GetAll(),media,root,report)==0 && report.Pending==1,"pending manifest reported, never linked");
   m.FinalizationStatus="Complete";Write(root,m);report=new RecordingImportReport();Check(TradeRecordingImporter.Import(repo.GetAll(),media,root,report)==1 && report.Linked==1,"exact schema3 video links");
   Check(TradeRecordingImporter.Import(repo.GetAll(),media,root)==0 && media.GetForTrade(trade.Id).Count==1,"schema3 repeat import idempotent");
   media.Detach(trade.Id,media.GetForTrade(trade.Id)[0].Id);Check(TradeRecordingImporter.Import(repo.GetAll(),media,root)==0 && media.GetForTrade(trade.Id).Count==0,"detached video never reattaches");
   var wrong=Path.Combine(root,"wrong.mp4");File.WriteAllText(wrong,"fixture");m.FinalVideoPath=wrong;ledger.TradeUid="tampered";Write(root,m);report=new RecordingImportReport();Check(TradeRecordingImporter.Import(repo.GetAll(),media,root,report)==0 && report.ReviewNeeded==1,"tampered UID blocked");
   ledger.TradeUid=trade.TradeUid;ledger.Account="OTHER";Write(root,m);Check(TradeRecordingImporter.Import(repo.GetAll(),media,root)==0,"cross-account never links");ledger.Account=trade.Account;
   ledger.EntryQuantity=3;Write(root,m);Check(TradeRecordingImporter.Import(repo.GetAll(),media,root)==0,"quantity conflict blocked");ledger.EntryQuantity=2;
   m.HasIncompleteTrades=true;Write(root,m);Check(TradeRecordingImporter.Import(repo.GetAll(),media,root)==0,"incomplete ledger blocked");m.HasIncompleteTrades=false;
   m.FinalVideoPath=Path.Combine(root,"missing.mp4");Write(root,m);report=new RecordingImportReport();Check(TradeRecordingImporter.Import(repo.GetAll(),media,root,report)==0 && report.MissingVideo==1,"missing file reported");m.FinalVideoPath=wrong;
   // Two rows sharing an execution UID cannot be silently chosen.
   trade.Id=0;repo.Insert(trade);Write(root,m);report=new RecordingImportReport();Check(TradeRecordingImporter.Import(repo.GetAll(),media,root,report)==0 && report.ReviewNeeded==1,"duplicate candidate blocked");
   Check(repo.GetAll().All(t=>t.Notes=="preserve"),"annotations preserved");
  }
  using(var db=new DatabaseManager(Path.Combine(root,"observed.db"))){db.Initialize();var repo=new TradeRepository(db);var media=new AttachmentRepository(db);
   var evidence=new ExecutionProvenance{Account="SIM",Instrument="MES SEP26",CompleteHistory=false,Allocations=new List<ExecutionAllocation>{new ExecutionAllocation{ExecutionId="o-entry",SignedQuantity=2},new ExecutionAllocation{ExecutionId="o-exit",SignedQuantity=-2}}};
   var trade=new Trade{Account="SIM",Instrument="MES",InstrumentFullName="MES SEP26",Direction="Long",Quantity=2,EntryTime=DateTime.Today,ExitTime=DateTime.Today.AddMinutes(1),SessionDate=DateTime.Today.ToString("yyyy-MM-dd"),ProvenanceJson=ExecutionIdentity.Serialize(evidence)};repo.Insert(trade);
   var ledger=new TradeRecordingImporter.RecorderTrade{Account=trade.Account,Instrument=trade.InstrumentFullName,Direction=trade.Direction,EntryQuantity=2,IsCompleteHistory=true,IdentityJson=trade.ProvenanceJson,ExecutionIds=new List<string>{"o-entry","o-exit"}};
   var m=new TradeRecordingImporter.RecorderManifest{SchemaVersion=3,CaptureId="observed",FinalizationStatus="Complete",StartedUtc=DateTime.UtcNow.AddMinutes(-2),StoppedUtc=DateTime.UtcNow,FinalVideoPath=Path.Combine(root,"clip.mp4"),Trades=new List<TradeRecordingImporter.RecorderTrade>{ledger}};
   Write(root,m);Check(TradeRecordingImporter.Import(repo.GetAll(),media,root)==1,"identical observed allocation evidence can associate media");
   Check(repo.GetById(trade.Id).TradeUid==null && media.GetForTrade(trade.Id).Single().Caption.Contains("history unverified"),"media association never upgrades trade identity");
   Check(TradeRecordingImporter.Import(repo.GetAll(),media,root)==0,"observed association repeat import idempotent");
   media.Detach(trade.Id,media.GetForTrade(trade.Id).Single().Id);Check(TradeRecordingImporter.Import(repo.GetAll(),media,root)==0,"observed detach preserved");
   ledger.TradeUid="tampered";Write(root,m);Check(TradeReconciliation.ObservedFillCandidates(ledger,repo.GetAll()).Count==0,"observed fallback cannot bypass conflicting UID");ledger.TradeUid=null;
   ledger.ExecutionIds[1]="wrong";Check(TradeReconciliation.ObservedFillCandidates(ledger,repo.GetAll()).Count==0,"ledger ID list must agree");ledger.ExecutionIds[1]="o-exit";
   trade.Id=0;repo.Insert(trade);m.FinalVideoPath=Path.Combine(root,"wrong.mp4");Write(root,m);Check(TradeRecordingImporter.Import(repo.GetAll(),media,root)==0,"duplicate observed candidates never auto link");
  }
  Console.WriteLine("PASS: "+checks+" video import checks");
 }
}
