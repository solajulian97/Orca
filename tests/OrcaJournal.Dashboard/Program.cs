using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OrcaJournal.Analytics;
using OrcaJournal.Data.Models;
using OrcaJournal.UI.ViewModels;
using OrcaJournal.UI.Views;
class Program {
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
 static Trade T(DateTime close,double pnl,string account="SIM"){return new Trade{Account=account,SessionDate=close.ToString("yyyy-MM-dd"),ExitTime=close,EntryTime=close.AddMinutes(-5),PnlDollars=pnl};}
 [STAThread] static int Main(string[] args){try{
  var app=new Application();var day=DateTime.Today.AddDays(-1);
  var trades=new List<Trade>{T(day.AddHours(11),200),T(day.AddHours(11).AddMinutes(29).AddSeconds(59),100),T(day.AddHours(11.5),-100),T(day.AddHours(12.5),0),T(day.AddHours(13),600)};
  var periods=DashboardBreakdown.HalfHours(trades);
  Check(periods[0].Pnl==300 && periods[0].Count==2,"11:00-11:30 sum");
  Check(periods[1].Pnl==-100,"Exact 11:30 boundary");Check(periods[2].Pnl==0 && periods[2].Count==0,"Empty internal period");
  Check(periods.Sum(x=>x.Pnl)==800,"Period total equals trade total");
  Check(Math.Abs(KpiCalculator.Calculate(trades).RealizedRR-3)<0.00001,"RR excludes breakeven");
  Check(double.IsNaN(KpiCalculator.Calculate(new[]{T(day,10)}).RealizedRR),"RR unavailable without losses");
  Check(double.IsNaN(KpiCalculator.Calculate(new Trade[0]).RealizedRR),"RR unavailable with no trades");
  var leap=new DateTime(2024,2,29);var calendar=DashboardBreakdown.Calendar(new[]{T(leap,150)},leap,new DateTime(2024,2,1),leap);
  Check(calendar.Count==42 && calendar[0].Date.DayOfWeek==DayOfWeek.Sunday,"Calendar alignment");
  Check(calendar.Single(x=>x.Date==leap).Pnl==150,"Leap day preserved");Check(calendar.Sum(x=>x.Pnl)==150,"Calendar total");
  var vm=new DashboardViewModel();vm.Load(trades.Concat(new[]{T(DateTime.Today.AddDays(1),999),T(day,-50,"OTHER")}).ToList(),new[]{"All Accounts","SIM","OTHER"});vm.SelectedAccount="SIM";vm.SelectedPeriod="Yesterday";
  Check(vm.Kpis.NetPnlDollars==800 && vm.IsSingleDay,"Account and day filters");
  vm.Load(trades,new[]{"All Accounts","SIM"});Check(vm.SelectedPeriod=="Yesterday" && vm.SelectedAccount=="SIM","Refresh preserves selections");
  var view=new DashboardView{DataContext=vm};view.Measure(new Size(1400,800));view.Arrange(new Rect(0,0,1400,800));view.UpdateLayout();
  var bmp=new RenderTargetBitmap(1400,800,96,96,PixelFormats.Pbgra32);bmp.Render(view);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(Path.Combine(args[0],"dashboard.png")))encoder.Save(f);
  vm.NextMonthCommand.Execute(null);Check(vm.SelectedPeriod=="Yesterday" && vm.IsSingleDay && vm.Kpis.NetPnlDollars==800,"Month navigation preserves period");
  vm.SelectDayCommand.Execute(new PnlCalendarDay{Date=day});Check(vm.IsSingleDay && vm.Kpis.NetPnlDollars==800,"Day drilldown");
  vm.SelectedPeriod="Today";Check(vm.Kpis.TotalTrades==0,"Today excludes other dates");
  Check(DashboardViewModel.RollingWeekdayStart(new DateTime(2026,9,7),5)==new DateTime(2026,9,1),"Monday five weekdays inclusive");
  Check(DashboardViewModel.RollingWeekdayStart(new DateTime(2026,9,7),10)==new DateTime(2026,8,25),"Ten weekdays cross month");
  Check(DashboardViewModel.RollingWeekdayStart(new DateTime(2026,9,7),20)==new DateTime(2026,8,11),"Twenty weekdays");
  Check(DashboardViewModel.RollingWeekdayStart(new DateTime(2026,9,6),10)==new DateTime(2026,8,24),"Sunday does not count");
  Check(DashboardViewModel.RollingWeekdayStart(new DateTime(2024,3,1),2)==new DateTime(2024,2,29),"Leap-year boundary");
  Check(DashboardViewModel.RollingWeekdayStart(new DateTime(2026,1,1),2)==new DateTime(2025,12,31),"Year boundary and explicit weekday policy");
  var cutoff=DashboardViewModel.RollingWeekdayStart(DateTime.Today,10);
  var rolling=new DashboardViewModel();rolling.Load(new[]{T(cutoff.AddDays(-1),999),T(cutoff,100),T(DateTime.Today,50)},new[]{"All Accounts","SIM"});rolling.SelectedPeriod="Last 10 Trading Days";
  Check(rolling.Kpis.NetPnlDollars==150,"Inclusive rolling cutoff");
  string originalMonth=rolling.CalendarTitle;rolling.PreviousMonthCommand.Execute(null);string browsed=rolling.CalendarTitle;
  Check(rolling.SelectedPeriod=="Last 10 Trading Days" && rolling.Kpis.NetPnlDollars==150 && browsed!=originalMonth,"Browse keeps rolling KPI window");
  rolling.Load(new[]{T(cutoff,100),T(DateTime.Today,50)},new[]{"All Accounts","SIM"});Check(rolling.CalendarTitle==browsed,"Refresh preserves browsed month");
  rolling.NextMonthCommand.Execute(null);Check(rolling.CalendarTitle==originalMonth && rolling.Kpis.NetPnlDollars==150,"Return month preserves window");
  rolling.SelectedPeriod="Calendar month";rolling.PreviousMonthCommand.Execute(null);Check(rolling.SelectedPeriod=="Calendar month","Explicit calendar month still navigates");
  Console.WriteLine("PASS: "+checks+" dashboard checks and rendered view.");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
