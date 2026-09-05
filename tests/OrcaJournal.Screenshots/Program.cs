using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OrcaJournal.Data.Models;
using OrcaJournal.UI.Windows;

class Program
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    static IEnumerable<T> Children<T>(DependencyObject obj) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T) yield return (T)child;
            foreach (var item in Children<T>(child)) yield return item;
        }
    }
    static void Click(FrameworkElement root, string text)
    {
        Children<Button>(root).Single(b => (string)b.Content == text).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        root.UpdateLayout();
    }
    [STAThread]
    static void Main(string[] args)
    {
        var app = new Application();
        Exception workerFailure = null;
        var worker = new System.Threading.Thread(() => {
            try {
                bool oldScanFailed = false;
                try { var windows = Application.Current.Windows; }
                catch (InvalidOperationException) { oldScanFailed = true; }
                Check(oldScanFailed, "Old application-window scan reproduces cross-thread failure");
                var button = new Button();
                var owner = new Window { Content = button, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -20000, Top = -20000, Width = 1, Height = 1 };
                owner.Show();
                var viewer = new TradeImageViewer(new TradeAttachment[0], null, "Dispatcher regression");
                viewer.SetOwnerFrom(button);
                Check(viewer.Owner == owner, "Viewer ownership works on secondary UI dispatcher");
                viewer.Close(); owner.Close();
            } catch (Exception ex) { workerFailure = ex; }
        });
        worker.SetApartmentState(System.Threading.ApartmentState.STA); worker.Start(); worker.Join();
        if (workerFailure != null) throw workerFailure;
        int?[] seconds = { null, -1, 0, 1, 59, 60, 61, 3599, 3600, 4330, 90061 };
        string[] expected = { "-", "-", "0 seconds", "1 second", "59 seconds", "1 minute, 0 seconds", "1 minute, 1 second", "59 minutes, 59 seconds", "1 hour, 0 minutes, 0 seconds", "1 hour, 12 minutes, 10 seconds", "25 hours, 1 minute, 1 second" };
        for (int i = 0; i < seconds.Length; i++) Check(new Trade { HoldSeconds = seconds[i] }.HoldDuration == expected[i], "Hold duration " + seconds[i]);
        string path = Path.Combine(args[0], "chart.png");
        var drawing = new DrawingVisual();
        using (var dc = drawing.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(18, 24, 30)), null, new Rect(0, 0, 1800, 900));
            for (int x = 0; x < 1800; x += 60) dc.DrawLine(new Pen(Brushes.DimGray, 1), new Point(x, 0), new Point(x, 900));
            for (int y = 0; y < 900; y += 60) dc.DrawLine(new Pen(Brushes.DimGray, 1), new Point(0, y), new Point(1800, y));
            for (int x = 100; x < 1700; x += 30)
            {
                double y = 450 + Math.Sin(x / 150.0) * 160;
                dc.DrawLine(new Pen(Brushes.LightGreen, 2), new Point(x, y - 35), new Point(x, y + 45));
                dc.DrawRectangle(Brushes.MediumSeaGreen, null, new Rect(x - 8, y - 20, 16, 40));
            }
        }
        var fixture = new RenderTargetBitmap(1800, 900, 96, 96, PixelFormats.Pbgra32); fixture.Render(drawing);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(fixture));
        using (var file = File.Create(path)) encoder.Save(file);
        var loaded = TradeImageViewer.LoadBitmap(path);
        Check(loaded.IsFrozen && loaded.PixelWidth == 1800, "Image decoding and freeze");
        using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { Check(true, "File unlocked after decode"); }
        string corrupt = Path.Combine(args[0], "broken.png"); File.WriteAllText(corrupt, "not an image");
        var first = new TradeAttachment { FilePath = path, Caption = "Synthetic chart fixture — no live trade data" };
        var attachments = new List<TradeAttachment> { first, new TradeAttachment { Kind = "video", FilePath = "video.mp4" }, new TradeAttachment { FilePath = corrupt }, new TradeAttachment { FilePath = path + ".missing" } };
        var window = new TradeImageViewer(attachments, first, "SIM · MNQ · Long · Screenshot check");
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(1180, 720)); root.Arrange(new Rect(0, 0, 1180, 720));
        window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent)); root.UpdateLayout();
        Check(Children<TextBlock>(root).Any(t => t.Text.StartsWith("1 / 3")), "Video excluded from gallery");
        Check(!Children<Button>(root).Single(b => (string)b.Content == "Previous").IsEnabled, "First boundary");
        var img = Children<Image>(root).Single();
        Check(img.Width <= 1180 && img.Height <= 720, "Fit within viewport");
        Click(root, "100%"); Check(img.Width == 1800, "100 percent");
        Click(root, "+"); Check(img.Width > 1800, "Zoom in");
        Click(root, "Fit");
        var screenshot = new RenderTargetBitmap(1180, 720, 96, 96, PixelFormats.Pbgra32); screenshot.Render(root);
        encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(screenshot));
        using (var file = File.Create(Path.Combine(args[0], "viewer.png"))) encoder.Save(file);
        attachments.Clear(); Click(root, "Next");
        Check(Children<TextBlock>(root).Any(t => t.Text.Contains("Unable to open this image:")), "Corrupt image handled and gallery snapshot retained");
        Click(root, "Next"); Check(img.Source == null, "Missing file clears previous image");
        Check(!Children<Button>(root).Single(b => (string)b.Content == "Next").IsEnabled, "Last boundary");
        Click(root, "Previous"); Click(root, "Previous"); Check(img.Source != null, "Navigation recovers after error");
        window.Close();
        Console.WriteLine("PASS: " + checks + " screenshot checks. Offline WPF only; NinjaTrader validation pending.");
    }
}
