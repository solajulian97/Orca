using System;
using System.IO;
using System.Linq;
using OrcaJournal.Data;
using OrcaJournal.Data.Models;
static class RiskMediaChecks {
 public static void Run(){int n=0;Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);n++;};
 var root=Path.Combine(Path.GetTempPath(),"orca-risk-media-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
 using(var db=new DatabaseManager(Path.Combine(root,"fixture.db"))){db.Initialize();db.Initialize();var trades=new TradeRepository(db);var reviews=new TradeReviewRepository(db);
 var t=new Trade{TradeKey="risk-media",Account="SIM",Instrument="MES",InstrumentFullName="MES SEP26",Direction="Long",EntryTime=DateTime.Today,ExitTime=DateTime.Today,SessionDate="2026-09-06",Quantity=1,PnlDollars=275};trades.Insert(t);
 var saved=reviews.Save(reviews.Read(t.Id),"","","",false,null,200);
 var display=trades.GetAll().Single();check(display.PnlR==1.375 && display.RealizedRFormatted=="1.4R","275 / 200 exact and display");check(saved.PlannedRisk==200,"Risk persisted");
 foreach(var bad in new[]{0.0,-1.0,double.NaN,double.PositiveInfinity}){bool rejected=false;try{reviews.Save(saved,"","","",false,null,bad);}catch(ArgumentException){rejected=true;}check(rejected,"Invalid risk rejected");}
 saved=reviews.Save(saved,"","","",false,null,null);check(!trades.GetAll().Single().PnlR.HasValue,"Unknown risk stays unknown");
 var media=new AttachmentRepository(db);string file=Path.Combine(root,"keep.png");File.WriteAllText(file,"fixture");
 var a=new TradeAttachment{TradeId=t.Id,FilePath=file,Kind="image",CreatedAt=DateTime.Today};var b=new TradeAttachment{TradeId=t.Id,FilePath=Path.Combine(root,"other.png"),Kind="image",CreatedAt=DateTime.Today.AddMinutes(1)};media.Insert(a);media.Insert(b);
 media.SetCaption(t.Id,a.Id,"Entry setup");check(media.GetForTrade(t.Id).Single(x=>x.Id==a.Id).Caption=="Entry setup","Caption persists");
 check(media.GetForTrade(t.Id)[0].Id==b.Id,"Legacy newest-first order");media.Move(t.Id,a.Id,-1);check(media.GetForTrade(t.Id)[0].Id==a.Id,"Custom order persists");
 media.Detach(t.Id,a.Id);check(media.GetForTrade(t.Id).Count==1 && File.Exists(file),"Detach preserves file");check(media.Exists(t.Id,file),"Tombstone blocks recorder reimport");
 using(var cmd=db.Connection.CreateCommand()){cmd.CommandText="SELECT count(*) FROM trade_attachments";check(Convert.ToInt32(cmd.ExecuteScalar())==2,"Detached row retained");}
 string video=Path.Combine(root,"shared.mp4");File.WriteAllText(video,"video fixture");
 var second=new Trade{TradeKey="parallel",Account="SIM",Instrument="MNQ",Direction="Long",EntryTime=DateTime.Today,ExitTime=DateTime.Today,SessionDate="2026-09-06"};trades.Insert(second);
 media.AttachVideo(t.Id,video);media.AttachVideo(second.Id,video);media.AttachVideo(t.Id,video);
 check(media.GetForTrade(t.Id).Count(x=>x.FilePath==video)==1 && media.GetForTrade(second.Id).Count(x=>x.FilePath==video)==1,"Same original video shared without duplicate rows");
 var linked=media.GetForTrade(t.Id).Single(x=>x.FilePath==video);media.Detach(t.Id,linked.Id);media.AttachVideo(t.Id,video);
 check(media.GetForTrade(t.Id).Any(x=>x.Id==linked.Id) && File.ReadAllText(video)=="video fixture","Explicit reattach restores tombstone and preserves file");
 bool invalid=false;try{media.AttachVideo(t.Id,file);}catch(ArgumentException){invalid=true;}check(invalid,"Image cannot be attached as video");
 }
 Console.WriteLine("PASS: "+n+" risk/media checks on disposable database.");
 }
}
