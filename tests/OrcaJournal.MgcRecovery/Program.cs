using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Data.SQLite;
using OrcaJournal.Core;
using OrcaJournal.Data;
using OrcaJournal.Data.Models;

// Explicit, additive recovery of the demonstrated September 14 MGC omission.
// Operates on supplied SQLite backups by default; production application is a separate guarded step.
class Program {
 static int Main(string[] args) {try {
  if(args.Length!=3)throw new ArgumentException("NT snapshot, Journal COPY, report path required");
  if(!Path.GetFileName(args[1]).StartsWith("recovery-copy-",StringComparison.Ordinal))throw new ArgumentException("Only recovery-copy-* databases accepted");
  var trades=new List<Trade>();var builder=new TradeBuilder();builder.TradeCompleted+=t=>trades.Add(t);
  var zone=TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
  var start=TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026,9,14),zone);
  var end=start.AddDays(1);
  using(var nt=new SQLiteConnection("Data Source="+args[0]+";Read Only=True;")) {
   nt.Open();using(var cmd=nt.CreateCommand()) {
    cmd.CommandText="SELECT e.ExecutionId,e.Quantity,e.MarketPosition,e.Price,e.Time,e.Position,i.Expiry,m.PointValue,m.TickSize FROM Executions e JOIN Accounts a ON e.Account=a.Id JOIN Instruments i ON e.Instrument=i.Id JOIN MasterInstruments m ON i.MasterInstrument=m.Id WHERE a.Name=@account AND m.Name='MGC' AND e.Time>=@start AND e.Time<@end ORDER BY e.Time,e.Id";
    cmd.Parameters.AddWithValue("@account","TAKEPROFIT262460251");cmd.Parameters.AddWithValue("@start",start.Ticks);cmd.Parameters.AddWithValue("@end",end.Ticks);
    var netByContract=new Dictionary<string,int>();var seen=new HashSet<string>();
    using(var reader=cmd.ExecuteReader())while(reader.Read()) {
     string id=Convert.ToString(reader[0]);if(string.IsNullOrWhiteSpace(id)||!seen.Add(id))throw new Exception("Missing/duplicate execution identity");
     if(Convert.ToDouble(reader[7])!=10 || Math.Abs(Convert.ToDouble(reader[8])-.1)>1e-9)throw new Exception("Unexpected MGC contract specification");
     string contract="MGC "+new DateTime(Convert.ToInt64(reader[6])).ToString("MMMyy",System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
     int side=Convert.ToInt32(reader[2]);if(side!=0 && side!=1)throw new Exception("Unexpected execution side");
     int quantity=Convert.ToInt32(reader[1]);int net;netByContract.TryGetValue(contract,out net);net+=side==0?quantity:-quantity;netByContract[contract]=net;
     if(Math.Abs(net)!=Math.Abs(Convert.ToInt32(reader[5])))throw new Exception("History starts mid-position or has a position gap");
     builder.OnFill(new TradeBuilder.FillSnapshot{Account="TAKEPROFIT262460251",Instrument="MGC",FullName=contract,Id=id,Quantity=quantity,IsBuy=side==0,Price=Convert.ToDouble(reader[3]),Time=TimeZoneInfo.ConvertTimeFromUtc(new DateTime(Convert.ToInt64(reader[4]),DateTimeKind.Utc),zone),Position=net,HasOrder=true});
    }
    if(netByContract.Values.Any(n=>n!=0))throw new Exception("MGC position remains open in snapshot");
   }
  }
  using(var db=new DatabaseManager(args[1])) {db.Initialize();var repo=new TradeRepository(db);int added=0;
   var lines=new List<string>{"# September 14 MGC recovery on a COPY","","Existing trades and media are preserved. No MAE/MFE or commissions reconstructed.",""};
   foreach(var t in trades) {
    var provenance=ExecutionIdentity.Parse(t.ProvenanceJson);provenance.CompleteHistory=false;provenance.Reason="Recovered from NinjaTrader execution database; historical continuity not independently verified";
    t.ProvenanceJson=ExecutionIdentity.Serialize(provenance);t.Mae=null;t.Mfe=null;t.ExcursionSamples=0;t.ExcursionStart=null;t.ExcursionEnd=null;t.ExcursionQuality="Not captured — recovered historical executions";
    t.Notes="Recovered omitted MGC fills from NinjaTrader execution database on 2026-09-14. Gross P&L before fees.";
    var ids=new HashSet<string>(provenance.Allocations.Select(a=>a.ExecutionId));
    var overlaps=repo.GetAll().Where(x=>x.Account==t.Account && x.InstrumentFullName==t.InstrumentFullName && x.TradeKey!=t.TradeKey).Any(x=>{var p=ExecutionIdentity.Parse(x.ProvenanceJson);return p!=null && p.Allocations.Any(a=>ids.Contains(a.ExecutionId));});
    if(overlaps)throw new Exception("Overlapping historical evidence requires review");
    if(repo.Insert(t))added++;
    lines.Add("- "+t.InstrumentFullName+" "+t.Direction+" "+t.EntryTime.ToString("HH:mm:ss")+"–"+t.ExitTime.ToString("HH:mm:ss")+" qty "+t.Quantity+" gross "+t.PnlDollars.ToString("F2"));
   }
   double beforeNoon=trades.Where(t=>t.ExitTime.Hour<12).Sum(t=>t.PnlDollars);
   if(Math.Abs(beforeNoon+60)>.001)throw new Exception("Screenshot-window MGC reconciliation changed");
   lines.Add("\nAdded: "+added+". MGC before noon: "+beforeNoon.ToString("F2")+". All recovered MGC: "+trades.Sum(t=>t.PnlDollars).ToString("F2"));
   File.WriteAllLines(args[2],lines);Console.WriteLine(lines.Last());
  }
  return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
