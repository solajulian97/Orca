$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '../../Orca Trades/Working_Suite/AddOns/OrcaTradeRecorderAddOn.cs'
$text = [IO.File]::ReadAllText($source)
$start = $text.IndexOf('public sealed class OrcaRecorderRoundTrip')
$end = $text.IndexOf('public sealed class OrcaRecorderCaptureManifest')
$ledger = $text.Substring($start, $end - $start)
$checks = @'
public static class RecorderTests {
 static int count;
 static void Check(bool value, string name) { if (!value) throw new Exception(name); count++; }
 static void Fill(OrcaRecorderTradeLedger l, string id, bool buy, int qty, double price) { l.Fill("Sim", "MNQ SEP26", id, buy, qty, price, 2, DateTime.Now); }
 public static int Run() {
  var l = new OrcaRecorderTradeLedger();
  Fill(l,"1",true,2,100); Fill(l,"2",true,1,110); Fill(l,"3",false,1,115);
  Check(l.DrainCompleted().Count==0,"partial exit must not complete");
  Fill(l,"4",false,2,120);
  var t=l.DrainCompleted();
  Check(t.Count==1 && t[0].GrossPnl==90 && t[0].EntryQuantity==3,"scale and partial PnL");
  Check(OrcaRecorderTradeLedger.BuildTitle(t,false).StartsWith("WIN__GROSS-+$90.00"),"win title");
  Fill(l,"4",false,2,120); Check(!l.HasOpenTrades,"duplicate close ignored");
  Fill(l,"5",false,1,100); Fill(l,"6",true,1,105);
  t=l.DrainCompleted(); Check(t[0].GrossPnl==-10,"short loss");
  Check(OrcaRecorderTradeLedger.BuildTitle(t,false).StartsWith("LOSS__"),"loss title");
  Fill(l,"7",true,2,100); Fill(l,"8",false,3,110);
  t=l.DrainCompleted(); Check(t.Count==1 && t[0].GrossPnl==40 && l.HasOpenTrades,"reversal closes original");
  Fill(l,"9",true,1,110); t.AddRange(l.DrainCompleted());
  Check(t.Count==2 && t[1].GrossPnl==0 && t[1].EntryQuantity==1,"reversal remainder");
  Check(OrcaRecorderTradeLedger.BuildTitle(t,false).StartsWith("MULTI__2-TRADES__WIN"),"multi title");
  Check(OrcaRecorderTradeLedger.BuildTitle(new List<OrcaRecorderRoundTrip>{t[1]},false).StartsWith("BREAKEVEN__"),"breakeven");
  l=new OrcaRecorderTradeLedger(); l.Seed("Sim","MNQ SEP26",1); Fill(l,"10",false,1,100);
  t=l.DrainCompleted(); Check(!t[0].IsCompleteHistory && OrcaRecorderTradeLedger.BuildTitle(t,false)=="PNL-UNKNOWN","pre-arm history");
  Fill(l,"11",true,1,100); Check(OrcaRecorderTradeLedger.BuildTitle(t,l.HasOpenTrades)=="PNL-UNKNOWN","disarm open");
  l=new OrcaRecorderTradeLedger(); Fill(l,"same",true,1,100);
  l.Fill("Other","MNQ SEP26","same",true,1,200,2,DateTime.Now);
  Fill(l,"exit",false,1,110); l.Fill("Other","MNQ SEP26","exit",false,1,190,2,DateTime.Now);
  t=l.DrainCompleted(); Check(t.Count==2 && t.Sum(x=>x.GrossPnl)==0,"account isolation");
  l=new OrcaRecorderTradeLedger(); Fill(l,null,true,1,100); Fill(l,"exit",false,1,100);
  Check(OrcaRecorderTradeLedger.BuildTitle(l.DrainCompleted(),l.HasOpenTrades)=="PNL-UNKNOWN","missing id");
  Check(OrcaRecorderTradeLedger.BuildTitle(null,false)=="PNL-UNKNOWN","legacy manifest");
  l=new OrcaRecorderTradeLedger(); Fill(l,"a",true,1,100);
  l.Fill("Sim","MES SEP26","b",false,1,100,5,DateTime.Now);
  Fill(l,"c",false,1,105); l.Fill("Sim","MES SEP26","d",true,1,98,5,DateTime.Now);
  t=l.DrainCompleted(); Check(t.Count==2 && t.Sum(x=>x.GrossPnl)==20,"instrument and point value isolation");
  Fill(l,"e",true,1,100); Fill(l,"f",false,1,101); Fill(l,"g",true,1,100); Fill(l,"h",false,1,99);
  t=l.DrainCompleted(); Check(t.Count==2 && t.Sum(x=>x.GrossPnl)==0,"new trade in tail remains separate round trip");
  t[0].Account="Bad:/Account"; t[0].Instrument="MNQ?*";
  Check(OrcaRecorderTradeLedger.BuildTitle(new List<OrcaRecorderRoundTrip>{t[0]},false).IndexOfAny(Path.GetInvalidFileNameChars())<0,"safe filename");
  return count;
 }
}
'@
Add-Type -TypeDefinition ("using System; using System.Collections.Generic; using System.Linq; using System.IO; using System.Globalization;" + $ledger + $checks)
Write-Output ('Passed {0} ledger assertions' -f [RecorderTests]::Run())

# Compile the entire AddOn against the installed NinjaTrader and WPF assemblies.
$compileDirectory = Join-Path ([IO.Path]::GetTempPath()) ('orca-recorder-check-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $compileDirectory | Out-Null
$framework = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$nt = 'C:\Program Files\NinjaTrader 8\bin'
$arguments = @('/nologo','/target:library',('/out:' + (Join-Path $compileDirectory 'Recorder.dll')),
 ('/r:' + $nt + '\NinjaTrader.Core.dll'),('/r:' + $nt + '\NinjaTrader.Gui.dll'),
 ('/r:' + $framework + '\WPF\PresentationCore.dll'),('/r:' + $framework + '\WPF\PresentationFramework.dll'),
 ('/r:' + $framework + '\WPF\WindowsBase.dll'),'/r:System.Xaml.dll','/r:System.Web.Extensions.dll','/r:System.ComponentModel.DataAnnotations.dll', $source)
& (Join-Path $framework 'csc.exe') @arguments
if ($LASTEXITCODE -ne 0) { throw 'NinjaTrader reference compilation failed' }
Write-Output 'Full AddOn semantic compile passed (not NinjaTrader F5/load validation).'
