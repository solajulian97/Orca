using System;
using System.Collections.Generic;
using System.IO;
using OrcaJournal.Core;
using OrcaJournal.Data;
using OrcaJournal.Data.Models;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
static class ExcursionChecks
{
 static int count; static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
 static DateTime t=new DateTime(2026,9,6,10,0,0);
 static Instrument instrument=new Instrument{FullName="MES SEP26",MasterInstrument=new MasterInstrument{Name="MES"}};
 static Execution Fill(string id,int q,double price,int pos,int sec) => new Execution{Instrument=instrument,Order=new Order{OrderAction=q>0?OrderAction.Buy:OrderAction.Sell},ExecutionId=id,Quantity=Math.Abs(q),Position=pos,Price=price,Time=t.AddSeconds(sec)};
 static void Price(TradeBuilder b,double p,int sec){b.OnPrice(instrument.FullName,p,t.AddSeconds(sec));}
 public static void Run()
 {
  var x=new RoundTripExcursion();x.Fill(2,100,t,5);x.Observe(90,t.AddSeconds(1),5);x.Fill(1,90,t.AddSeconds(2),5);x.Observe(110,t.AddSeconds(3),5);x.Fill(-2,110,t.AddSeconds(4),5);x.Observe(95,t.AddSeconds(5),5);x.Fill(-1,95,t.AddSeconds(6),5);
  Check(x.Mae==-100 && x.Mfe==200,"scale-in and partial exit extrema");Check(x.Samples==3,"only market observations counted");
  var b=new TradeBuilder();var trades=new List<Trade>();b.TradeCompleted+=trades.Add;
  b.OnFill(Fill("warm1",1,100,1,0),"A");Price(b,110,1);b.OnFill(Fill("warm2",-1,105,0,2),"A");
  Check(trades[0].Mfe==50 && trades[0].ExcursionQuality.StartsWith("Partial") && trades[0].MfeDisplay.StartsWith("Partial"),"unverified initial boundary preserves explicitly partial observations");
  b.OnFill(Fill("e1",2,100,2,3),"A");Price(b,90,4);b.OnFill(Fill("e2",1,90,3,5),"A");Price(b,110,6);b.OnFill(Fill("e3",-2,110,1,7),"A");Price(b,95,8);b.OnFill(Fill("e4",-2,95,-1,9),"A");
  Check(trades.Count==2 && trades[1].Mae==-100 && trades[1].Mfe==200,"builder full round trip");Check(trades[1].PnlDollars==125,"partial realized plus open equals gross result");
  Price(b,90,10);b.OnFill(Fill("e5",1,92,0,11),"A");Check(trades[2].Mfe==25 && trades[2].Mae==0,"reversal starts fresh short excursion");
  b.OnFill(Fill("gap1",1,100,1,12),"A");Price(b,110,13);b.InvalidateIdentity("disconnect");b.OnFill(Fill("gap2",-1,105,0,14),"A");Check(!trades[3].Mfe.HasValue,"connection gap fails closed");
  b.OnFill(Fill("none1",1,100,1,15),"A");b.OnFill(Fill("none2",-1,105,0,16),"A");Check(!trades[4].Mfe.HasValue,"fills alone are not price coverage");
  b.OnFill(Fill("late1",1,100,1,17),"A");Price(b,110,19);Price(b,90,18);b.OnFill(Fill("late2",-1,105,0,20),"A");Check(trades[5].Mfe==50 && trades[5].Mae==0 && trades[5].ExcursionQuality.Contains("1 late price samples skipped"),"late marks skipped without erasing accepted extrema");
  b.OnFill(Fill("scope1",1,100,1,21),"A");b.OnPrice("MES DEC26",999,t.AddSeconds(22));b.OnFill(Fill("scope2",-1,105,0,23),"A");Check(!trades[6].Mfe.HasValue,"wrong contract cannot supply coverage");
  string root=Path.Combine(Path.GetTempPath(),"orca-excursions-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  using(var db=new DatabaseManager(Path.Combine(root,"fixture.db"))){db.Initialize();var repo=new TradeRepository(db);repo.Insert(trades[1]);var saved=repo.GetById(trades[1].Id);Check(saved.Mfe==200 && saved.Mae==-100 && saved.ExcursionSamples==3,"excursion persistence");Check(saved.ExcursionStart==t.AddSeconds(4)&&saved.ExcursionEnd==t.AddSeconds(8),"coverage timestamps persisted");Check(saved.ExcursionQuality.Contains("sampled"),"quality survives reload");}
  var account=new Account{Name="A"};var live=new TradeBuilder();var captured=new List<Trade>();live.TradeCompleted+=captured.Add;var cap=new TradeCapture(live);cap.Attach(account);
  account.Deliver(Fill("cap1",1,100,1,0));Check(instrument.MarketData.Subscribers==1,"shared feed attaches");
  account.Deliver(Fill("cap2",1,100,2,1));Check(instrument.MarketData.Subscribers==1,"no repeated subscriptions");
  account.Deliver(Fill("cap3",-2,101,0,2));Check(instrument.MarketData.Subscribers==0,"flat releases feed");
  account.Deliver(Fill("cap4",1,100,1,3));
  instrument.MarketData.Deliver(new MarketDataEventArgs{Instrument=instrument,MarketDataType=MarketDataType.Bid,Price=1,Time=t.AddSeconds(4)});
  instrument.MarketData.Deliver(new MarketDataEventArgs{Instrument=instrument,MarketDataType=MarketDataType.Last,Price=110,Time=t.AddSeconds(4)});
  account.Deliver(Fill("cap5",-1,105,0,5));Check(captured[1].Mfe==50&&captured[1].Mae==0,"Last callback reaches builder; bid ignored");
  account.Deliver(Fill("cap6",1,100,1,6));cap.DetachAll();Check(instrument.MarketData.Subscribers==0,"shutdown releases feed");
  var mid=new TradeBuilder();Trade midTrade=null;mid.TradeCompleted+=value=>midTrade=value;
  mid.OnFill(Fill("mid1",1,100,4,0),"A");Price(mid,110,1);mid.OnFill(Fill("mid2",-1,105,3,2),"A");
  Check(midTrade!=null && !midTrade.Mfe.HasValue,"startup mid-position does not publish partial P&L for a false full cycle");
  var c2=new TradeCapture(new TradeBuilder());var a2=new Account{Name="A2"};var a3=new Account{Name="A3"};c2.Attach(a2);c2.Attach(a3);
  a2.Deliver(Fill("detach1",1,100,1,0));a3.Deliver(Fill("detach2",1,200,1,0));Check(instrument.MarketData.Subscribers==1,"accounts share one contract subscription");
  c2.Detach(a2);Check(instrument.MarketData.Subscribers==1,"other account retains feed");c2.Detach(a3);Check(instrument.MarketData.Subscribers==0,"last account detach releases feed");c2.DetachAll();
  Check(ReviewTagOption.Management=="Risk & Trade Management" && ReviewTagOption.Analysis=="Analysis & Execution","requested category labels");
  var media=new TradeAttachment{FilePath="file.png",Caption="Entry setup"};Check(media.DisplayTitle=="Entry setup","caption is image title");media.Caption=" ";Check(media.DisplayTitle=="file.png","old media title fallback");
  var jitter=new RoundTripExcursion();jitter.Fill(2,100,t,5);jitter.Observe(102,t.AddMilliseconds(100),5);jitter.Fill(-1,101,t.AddMilliseconds(90),5);jitter.Observe(103,t.AddMilliseconds(150),5);jitter.Fill(-1,102,t.AddMilliseconds(140),5);
  Check(jitter.Valid && jitter.Samples==2 && jitter.Mfe==20 && jitter.Mae==0,"cross-stream overlap retains callback-order estimate and fill inventory");
  Check(jitter.CrossStreamOverlaps==2 && jitter.Quality(true).StartsWith("Partial observed") && jitter.Quality(true).Contains("10 ms"),"cross-stream uncertainty is explicit");
  var beforeFill=new RoundTripExcursion();beforeFill.Fill(1,100,t.AddMilliseconds(100),5);beforeFill.Observe(101,t.AddMilliseconds(90),5);Check(beforeFill.Valid && beforeFill.Samples==0 && beforeFill.SkippedPrices==1,"pre-entry price cannot mark new inventory");
  var badFills=new RoundTripExcursion();badFills.Fill(1,100,t,5);badFills.Fill(1,101,t.AddMilliseconds(-1),5);Check(!badFills.Valid && badFills.FailureReason.Contains("execution timestamps"),"actual execution regression still invalidates");
  var badPrices=new RoundTripExcursion();badPrices.Fill(1,100,t,5);badPrices.Observe(101,t.AddSeconds(2),5);badPrices.Observe(102,t.AddSeconds(1),5);Check(badPrices.Valid && badPrices.SkippedPrices==1 && badPrices.Mfe==5,"price regression does not change accepted extrema"); badPrices.Observe(999,t.AddMilliseconds(1500),5); Check(badPrices.SkippedPrices==2 && badPrices.Mfe==5,"high-water survives repeated stale samples"); badPrices.Observe(103,t.AddSeconds(3),5); Check(badPrices.Mfe==15 && badPrices.Samples==2,"capture resumes once price time catches up");
  Check(new Trade{HoldSeconds=107}.HoldDuration=="1m 47s" && new Trade{HoldSeconds=4312}.HoldDuration=="1h 11m 52s","spaced compact duration");
  var mismatch=new TradeBuilder();var mismatchTrades=new List<Trade>();mismatch.TradeCompleted+=mismatchTrades.Add;
  mismatch.OnFill(Fill("p0",1,100,1,0),"A");Price(mismatch,101,1);mismatch.OnFill(Fill("p1",-1,101,0,2),"A");
  mismatch.OnFill(Fill("p2",1,100,2,3),"A");Price(mismatch,98,4);mismatch.OnFill(Fill("p3",-1,102,0,5),"A");
  Check(mismatchTrades[1].Mae==-10 && mismatchTrades[1].Mfe==10 && mismatchTrades[1].ExcursionQuality.Contains("1 execution-position disagreements"),"known-boundary mismatch retains explicitly partial observed-fill estimate");
  Check(mismatchTrades[1].TradeUid==null,"partial excursion never manufactures identity");
  Console.WriteLine("PASS: "+count+" excursion and title checks");
 }
}
