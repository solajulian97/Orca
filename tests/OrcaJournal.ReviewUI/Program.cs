using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OrcaJournal.Data;
using OrcaJournal.Data.Models;
using OrcaJournal.UI.Windows;
class Program
{
    static IEnumerable<T> Find<T>(DependencyObject root) where T:DependencyObject {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++) {var c=VisualTreeHelper.GetChild(root,i);if(c is T)yield return (T)c;foreach(var v in Find<T>(c))yield return v;}
    }
    [STAThread] static int Main(string[] args) {
        try {
            var app=new Application { ShutdownMode=ShutdownMode.OnExplicitShutdown };
            using(var db=new DatabaseManager(Path.Combine(args[0],"fixture.db"))) {
                db.Initialize();var repo=new TradeRepository(db);var t=new Trade{TradeKey="ui",Account="SIM",Instrument="MES",InstrumentFullName="MES SEP26",Direction="Long",SessionDate="2026-09-05",EntryTime=DateTime.Today,ExitTime=DateTime.Today.AddMinutes(1),Quantity=1,PnlDollars=275,Notes="Waited for a pullback. Review the entry timing.",SetupGrade="B"};repo.Insert(t);
                var tagRepo=new TagRepository(db);
                foreach(var name in new[]{"FVG","iFVG","MGI","node","OB","Passive Player","RB","Structure","Sweep","TAPER"})tagRepo.GetOrCreateTag(name,"#4285F4");
                int saves=0;var window=new TradeReviewWindow(new TradeReviewRepository(db),t.Id,"SIM · MES · Long",()=>saves++);
                window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=-20000;window.Top=-20000;window.ShowActivated=false;window.ShowInTaskbar=false;window.Show();
                var root=(FrameworkElement)window.Content; root.Measure(new Size(680,740));root.Arrange(new Rect(0,0,680,740));root.UpdateLayout();
                var boxes=Find<TextBox>(root).Where(x=>!x.IsReadOnly && x.AcceptsReturn).ToList();
                if(boxes.Count!=1)throw new Exception("Expected notes editor and checkbox tags");
                Find<TextBox>(root).Single(x=>x.Name=="PlannedRisk").Text="200";
                boxes[0].Text="Waited for confirmation; next time review stop placement before entering.";Find<CheckBox>(root).Single(x=>x.Content as string=="Risked proper amount").IsChecked=true; Find<CheckBox>(root).Single(x=>x.Content as string=="Took proper partials").IsChecked=true;
                Find<ComboBox>(root).First().SelectedItem="A-";
                if(!Find<TextBlock>(root).Any(x=>x.Text.StartsWith("Unsaved changes")))throw new Exception("Missing dirty feedback");
                Find<Button>(root).Single(x=>x.Content as string=="Save review").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();
                if(new TradeReviewRepository(db).Read(t.Id).PlannedRisk!=200)throw new Exception("Risk save failed");
                if(saves!=1 || new TradeReviewRepository(db).Read(t.Id).Grade!="A-")throw new Exception("Save integration failed");
                if(!Find<TextBlock>(root).Any(x=>x.Text=="Saved."))throw new Exception("Missing saved feedback");
                var frame=new System.Windows.Threading.DispatcherFrame();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,new Action(()=>frame.Continue=false));
                System.Windows.Threading.Dispatcher.PushFrame(frame);
                var bitmap=new RenderTargetBitmap(680,740,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(Path.Combine(args[0],"review.png")))encoder.Save(f);
                boxes[0].Text="My unsaved draft";
                var raw=repo.GetById(t.Id);raw.Notes="A later chart edit";repo.Update(raw);
                Find<Button>(root).Single(x=>x.Content as string=="Save review").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();
                if(boxes[0].Text!="My unsaved draft")throw new Exception("Conflict lost draft");
                var keep=Find<Button>(root).Single(x=>x.Content as string=="Keep my changes");
                if(((FrameworkElement)((FrameworkElement)keep.Parent).Parent).Visibility!=Visibility.Visible)throw new Exception("Conflict choices not exposed");
                keep.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();
                if(saves!=2 || new TradeReviewRepository(db).Read(t.Id).Notes!="My unsaved draft" || repo.GetById(t.Id).Notes!="A later chart edit")throw new Exception("Explicit resolution failed");
                window.Close();
                var mediaRepo=new AttachmentRepository(db);var attachment=new TradeAttachment{TradeId=t.Id,FilePath=Path.Combine(args[0],"example.png"),Kind="image",CreatedAt=DateTime.Now};mediaRepo.Insert(attachment);
                var mediaWindow=new TradeMediaWindow(mediaRepo,t.Id,()=>{});
                mediaWindow.WindowStartupLocation=WindowStartupLocation.Manual;mediaWindow.Left=-20000;mediaWindow.Top=-20000;mediaWindow.ShowActivated=false;mediaWindow.ShowInTaskbar=false;mediaWindow.Show();
                var mediaRoot=(FrameworkElement)mediaWindow.Content;mediaRoot.UpdateLayout();
                Find<TextBox>(mediaRoot).Single().Text="Entry setup";mediaWindow.Close();
                if(mediaRepo.GetForTrade(t.Id).Single().Caption!="Entry setup")throw new Exception("Caption not saved on close");
                Console.WriteLine("PASS: review editor loads theme, edits notes/tags/grade, saves, refreshes, and shows save feedback.");
            }
            return 0;
        } catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
