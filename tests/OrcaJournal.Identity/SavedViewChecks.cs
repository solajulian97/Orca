using System;
using System.IO;
using OrcaJournal.Data;
internal static class SavedViewChecks
{
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static void Run(){
 var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");
 using(var db=new DatabaseManager(path)){db.Initialize();var r=new SavedTagViewRepository(db);
 Check(r.LoadThreshold()==20,"default threshold");r.SaveThreshold(50);
 r.Save(new SavedTagView{Name=" Sweep + Structure ",Tags="Sweep\nStructure",MatchAll=true,Outcome="Exclude breakevens",Threshold=50});
 bool rejected=false;try{r.Save(new SavedTagView{Name="sweep + structure",Tags="",Outcome="All trades",Threshold=20});}catch{rejected=true;}Check(rejected,"duplicate protected");
 rejected=false;try{r.SaveThreshold(double.NaN);}catch{rejected=true;}Check(rejected,"invalid rejected");
 }
 using(var db=new DatabaseManager(path)){db.Initialize();var r=new SavedTagViewRepository(db);var v=r.Load()[0];
 Check(r.LoadThreshold()==50 && v.Name=="Sweep + Structure" && v.Tags=="Sweep\nStructure" && v.MatchAll && v.Threshold==50 && v.Outcome=="Exclude breakevens","restart roundtrip");
 v.MatchAll=false;v.Tags="Missing tag";v.Threshold=200;r.Save(v,true);Check(r.Load().Count==1 && !r.Load()[0].MatchAll && r.Load()[0].Tags=="Missing tag","update preserves unknown tag");
 r.Delete(v.Name);Check(r.Load().Count==0 && r.LoadThreshold()==50,"delete only preset");
 using(var c=db.Connection.CreateCommand()){c.CommandText="SELECT COUNT(*) FROM trades";Check(Convert.ToInt32(c.ExecuteScalar())==0,"no trades created");}
 }Console.WriteLine("PASS: 8 saved-view persistence checks");
 }
}
