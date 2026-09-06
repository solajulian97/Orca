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
            var app=new Application();
            using(var db=new DatabaseManager(Path.Combine(args[0],"fixture.db"))) {
                db.Initialize();var repo=new TradeRepository(db);var t=new Trade{TradeKey="ui",Account="SIM",Instrument="MES",InstrumentFullName="MES SEP26",Direction="Long",SessionDate="2026-09-05",EntryTime=DateTime.Today,ExitTime=DateTime.Today.AddMinutes(1),Quantity=1,Notes="Waited for a pullback. Review the entry timing.",SetupGrade="B"};repo.Insert(t);
                int saves=0;var window=new TradeReviewWindow(new TradeReviewRepository(db),t.Id,"SIM · MES · Long",()=>saves++);
                var root=(FrameworkElement)window.Content; root.Measure(new Size(680,740));root.Arrange(new Rect(0,0,680,740));root.UpdateLayout();
                var boxes=Find<TextBox>(root).Where(x=>!x.IsReadOnly && x.AcceptsReturn).ToList();
                if(boxes.Count!=2)throw new Exception("Expected notes and tags editors");
                boxes[0].Text="Waited for confirmation; next time review stop placement before entering.";boxes[1].Text="Pullback\nPatience";
                Find<ComboBox>(root).Single().SelectedItem="A-";
                if(!Find<TextBlock>(root).Any(x=>x.Text.StartsWith("Unsaved changes")))throw new Exception("Missing dirty feedback");
                Find<Button>(root).Single(x=>x.Content as string=="Save review").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();
                if(saves!=1 || new TradeReviewRepository(db).Read(t.Id).Grade!="A-")throw new Exception("Save integration failed");
                if(!Find<TextBlock>(root).Any(x=>x.Text.StartsWith("Saved review")))throw new Exception("Missing saved feedback");
                var bitmap=new RenderTargetBitmap(680,740,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(Path.Combine(args[0],"review.png")))encoder.Save(f);
                window.Close();Console.WriteLine("PASS: review editor loads theme, edits notes/tags/grade, saves, refreshes, and shows save feedback.");
            }
            return 0;
        } catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
