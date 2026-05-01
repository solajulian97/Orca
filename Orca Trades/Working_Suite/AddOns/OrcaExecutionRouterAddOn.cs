#region Using declarations
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	public sealed class OrcaExecutionRouterAddOn : AddOnBase
	{
		private NTMenuItem hostMenu;
		private NTMenuItem routerMenuItem;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Orca execution instrument routing controls";
				Name = "Orca Execution Router";
			}
		}

		protected override void OnWindowCreated(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || routerMenuItem != null)
				return;

			hostMenu = controlCenter.FindFirst("ControlCenterMenuItemTools") as NTMenuItem
				?? controlCenter.FindFirst("toolsMenuItem") as NTMenuItem
				?? controlCenter.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
			if (hostMenu == null)
				return;

			routerMenuItem = new NTMenuItem {
				Header = "Orca Execution Router",
				Style = Application.Current.TryFindResource("MainMenuItem") as Style
			};
			routerMenuItem.Click += OnMenuItemClick;
			hostMenu.Items.Add(routerMenuItem);
		}

		protected override void OnWindowDestroyed(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || routerMenuItem == null || hostMenu == null)
				return;

			routerMenuItem.Click -= OnMenuItemClick;
			hostMenu.Items.Remove(routerMenuItem);
			routerMenuItem = null;
			hostMenu = null;
		}

		private void OnMenuItemClick(object sender, RoutedEventArgs e)
		{
			Application.Current.Dispatcher.InvokeAsync(() => OrcaExecutionRouterWindow.ShowOrActivate());
		}
	}

	[Serializable]
	public class OrcaExecutionRouterSettings
	{
		public bool Enabled { get; set; }
		public bool RouteNqToMnq { get; set; } = true;
		public string PositionColor { get; set; } = "#FF1E90FF";
		public string ShortPositionColor { get; set; } = "#FFE03A52";
		public string BuyColor { get; set; } = "#FF32CD32";
		public string SellColor { get; set; } = "#FFFF7F7F";
		public string TextColor { get; set; } = "#FF000000";
		public string DragChangeColor { get; set; } = "#FFFFD700";
		public string FontFamily { get; set; } = "Segoe UI";
		public string FontWeight { get; set; } = "SemiBold";
		public string PnlBadgePosition { get; set; } = "Left";
		public double FontSize { get; set; } = 11;
		public double LineThickness { get; set; } = 1;
		public double LabelRightPadding { get; set; } = 6;
		public double LabelBackgroundOpacity { get; set; } = 0.95;
		public double CoverButtonBackgroundOpacity { get; set; } = 0.95;
	}

	public static class OrcaExecutionRouter
	{
		private static readonly object Sync = new object();
		private static OrcaExecutionRouterSettings settings;

		public static OrcaExecutionRouterSettings GetSettings()
		{
			EnsureLoaded();
			lock (Sync) {
				return new OrcaExecutionRouterSettings {
					Enabled = settings.Enabled,
					RouteNqToMnq = settings.RouteNqToMnq,
					PositionColor = settings.PositionColor,
					ShortPositionColor = settings.ShortPositionColor,
					BuyColor = settings.BuyColor,
					SellColor = settings.SellColor,
					TextColor = settings.TextColor,
					DragChangeColor = settings.DragChangeColor,
					FontFamily = settings.FontFamily,
					FontWeight = settings.FontWeight,
					PnlBadgePosition = settings.PnlBadgePosition,
					FontSize = settings.FontSize,
					LineThickness = settings.LineThickness,
					LabelRightPadding = settings.LabelRightPadding,
					LabelBackgroundOpacity = settings.LabelBackgroundOpacity,
					CoverButtonBackgroundOpacity = settings.CoverButtonBackgroundOpacity
				};
			}
		}

		public static void SaveSettings(OrcaExecutionRouterSettings newSettings)
		{
			if (newSettings == null)
				return;

			lock (Sync) {
				settings = new OrcaExecutionRouterSettings {
					Enabled = newSettings.Enabled,
					RouteNqToMnq = newSettings.RouteNqToMnq,
					PositionColor = string.IsNullOrWhiteSpace(newSettings.PositionColor) ? "#FF1E90FF" : newSettings.PositionColor,
					ShortPositionColor = string.IsNullOrWhiteSpace(newSettings.ShortPositionColor) ? "#FFE03A52" : newSettings.ShortPositionColor,
					BuyColor = string.IsNullOrWhiteSpace(newSettings.BuyColor) ? "#FF32CD32" : newSettings.BuyColor,
					SellColor = string.IsNullOrWhiteSpace(newSettings.SellColor) ? "#FFFF7F7F" : newSettings.SellColor,
					TextColor = string.IsNullOrWhiteSpace(newSettings.TextColor) ? "#FF000000" : newSettings.TextColor,
					DragChangeColor = string.IsNullOrWhiteSpace(newSettings.DragChangeColor) ? "#FFFFD700" : newSettings.DragChangeColor,
					FontFamily = string.IsNullOrWhiteSpace(newSettings.FontFamily) ? "Segoe UI" : newSettings.FontFamily,
					FontWeight = NormalizeFontWeight(newSettings.FontWeight),
					PnlBadgePosition = NormalizePnlBadgePosition(newSettings.PnlBadgePosition),
					FontSize = Math.Max(8, Math.Min(24, newSettings.FontSize)),
					LineThickness = Math.Max(1, Math.Min(6, newSettings.LineThickness)),
					LabelRightPadding = Math.Max(0, Math.Min(80, newSettings.LabelRightPadding)),
					LabelBackgroundOpacity = NormalizeOpacity(newSettings.LabelBackgroundOpacity, 0.95),
					CoverButtonBackgroundOpacity = NormalizeOpacity(newSettings.CoverButtonBackgroundOpacity, 0.95)
				};
				string directory = Path.GetDirectoryName(SettingsPath);
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);
				using (FileStream stream = File.Create(SettingsPath))
					new XmlSerializer(typeof(OrcaExecutionRouterSettings)).Serialize(stream, settings);
			}
		}

		private static string NormalizeFontWeight(string weight)
		{
			if (string.IsNullOrWhiteSpace(weight))
				return "SemiBold";

			string compact = weight.Replace(" ", "");
			if (string.Equals(compact, "Normal", StringComparison.OrdinalIgnoreCase)) return "Normal";
			if (string.Equals(compact, "Medium", StringComparison.OrdinalIgnoreCase)) return "Medium";
			if (string.Equals(compact, "SemiBold", StringComparison.OrdinalIgnoreCase)) return "SemiBold";
			if (string.Equals(compact, "Bold", StringComparison.OrdinalIgnoreCase)) return "Bold";
			return "SemiBold";
		}
		private static string NormalizePnlBadgePosition(string position)
		{
			return string.Equals(position, "Right", StringComparison.OrdinalIgnoreCase) ? "Right" : "Left";
		}

		private static double NormalizeOpacity(double opacity, double fallback)
		{
			if (double.IsNaN(opacity) || double.IsInfinity(opacity))
				return fallback;
			if (opacity > 1 && opacity <= 100)
				opacity /= 100.0;
			return Math.Max(0, Math.Min(1, opacity));
		}

		public static Instrument ResolveExecutionInstrument(Instrument chartInstrument, Instrument fallbackInstrument)
		{
			EnsureLoaded();
			if (chartInstrument == null)
				return fallbackInstrument;

			lock (Sync) {
				if (!settings.Enabled)
					return fallbackInstrument ?? chartInstrument;

				if (settings.RouteNqToMnq && string.Equals(GetRoot(chartInstrument), "NQ", StringComparison.OrdinalIgnoreCase)) {
					Instrument micro = GetMappedInstrument(chartInstrument, "MNQ");
					if (micro != null)
						return micro;
				}
			}

			return fallbackInstrument ?? chartInstrument;
		}

		private static string SettingsPath
		{
			get {
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					"NinjaTrader 8",
					"OrcaExecutionRouter.xml");
			}
		}

		private static void EnsureLoaded()
		{
			lock (Sync) {
				if (settings != null)
					return;

				settings = new OrcaExecutionRouterSettings();
				try {
					if (File.Exists(SettingsPath)) {
						using (FileStream stream = File.OpenRead(SettingsPath))
							settings = (OrcaExecutionRouterSettings)new XmlSerializer(typeof(OrcaExecutionRouterSettings)).Deserialize(stream);
					}
				} catch {
					settings = new OrcaExecutionRouterSettings();
				}
				if (string.IsNullOrWhiteSpace(settings.ShortPositionColor)
					|| string.Equals(settings.ShortPositionColor, "#FFCD5C5C", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(settings.ShortPositionColor, "#FFB11226", StringComparison.OrdinalIgnoreCase))
					settings.ShortPositionColor = "#FFE03A52";
				settings.PnlBadgePosition = NormalizePnlBadgePosition(settings.PnlBadgePosition);
			}
		}

		private static Instrument GetMappedInstrument(Instrument chartInstrument, string targetRoot)
		{
			string root = GetRoot(chartInstrument);
			string fullName = chartInstrument?.FullName;
			if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(fullName))
				return null;

			string targetFullName = fullName.StartsWith(root, StringComparison.OrdinalIgnoreCase)
				? targetRoot + fullName.Substring(root.Length)
				: targetRoot;
			try { return Instrument.GetInstrument(targetFullName, true); } catch { }
			try { return Instrument.GetInstrument(targetRoot, true); } catch { }
			return null;
		}

		private static string GetRoot(Instrument instrument)
		{
			string root = instrument?.MasterInstrument?.Name;
			if (!string.IsNullOrEmpty(root))
				return root.ToUpperInvariant();

			string fullName = instrument?.FullName;
			if (string.IsNullOrEmpty(fullName))
				return null;
			int space = fullName.IndexOf(' ');
			return (space > 0 ? fullName.Substring(0, space) : fullName).ToUpperInvariant();
		}
	}

	public class OrcaExecutionRouterWindow : Window
	{
		private static OrcaExecutionRouterWindow instance;
		private readonly CheckBox enabledBox;
		private readonly CheckBox nqToMnqBox;
		private readonly TextBlock statusText;
		private readonly TextBox positionColorBox;
		private readonly TextBox shortPositionColorBox;
		private readonly TextBox buyColorBox;
		private readonly TextBox sellColorBox;
		private readonly TextBox textColorBox;
		private readonly TextBox dragColorBox;
		private readonly TextBox fontFamilyBox;
		private readonly ComboBox fontWeightBox;
		private readonly ComboBox pnlBadgePositionBox;
		private readonly TextBox fontSizeBox;
		private readonly TextBox lineThicknessBox;
		private readonly TextBox labelPaddingBox;
		private readonly TextBox labelOpacityBox;
		private readonly TextBox coverOpacityBox;

		private OrcaExecutionRouterWindow()
		{
			Title = "Orca Execution Router";
			Width = 420;
			Height = 560;
			MinWidth = 320;
			MinHeight = 220;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Background = (Brush)new BrushConverter().ConvertFrom("#FF1B1B1B");
			Foreground = Brushes.GhostWhite;

			ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
			StackPanel root = new StackPanel { Margin = new Thickness(14) };
			scroll.Content = root;
			root.Children.Add(new TextBlock {
				Text = "Orca Execution Router",
				FontSize = 16,
				FontWeight = FontWeights.Bold,
				Margin = new Thickness(0, 0, 0, 12)
			});

			enabledBox = new CheckBox {
				Content = "Enable Orca routing",
				Margin = new Thickness(0, 0, 0, 8),
				Foreground = Brushes.GhostWhite
			};
			nqToMnqBox = new CheckBox {
				Content = "Route NQ chart orders to MNQ",
				Margin = new Thickness(0, 0, 0, 14),
				Foreground = Brushes.GhostWhite
			};
			root.Children.Add(enabledBox);
			root.Children.Add(nqToMnqBox);

			root.Children.Add(new TextBlock {
				Text = "Overlay Visuals",
				FontSize = 13,
				FontWeight = FontWeights.Bold,
				Foreground = Brushes.GhostWhite,
				Margin = new Thickness(0, 4, 0, 8)
			});
			positionColorBox = AddTextSetting(root, "Position color", "#FF1E90FF");
			shortPositionColorBox = AddTextSetting(root, "Short position color", "#FFE03A52");
			buyColorBox = AddTextSetting(root, "Buy / TP color", "#FF32CD32");
			sellColorBox = AddTextSetting(root, "Sell / SL color", "#FFFF7F7F");
			textColorBox = AddTextSetting(root, "Label text color", "#FF000000");
			dragColorBox = AddTextSetting(root, "Change-drag color", "#FFFFD700");
			fontFamilyBox = AddTextSetting(root, "Font family", "Segoe UI");
			fontWeightBox = AddComboSetting(root, "Font weight", new[] { "Normal", "Medium", "SemiBold", "Bold" });
			pnlBadgePositionBox = AddComboSetting(root, "PnL badge side", new[] { "Left", "Right" });
			fontSizeBox = AddTextSetting(root, "Font size", "11");
			lineThicknessBox = AddTextSetting(root, "Line thickness", "1");
			labelPaddingBox = AddTextSetting(root, "Right padding", "6");
			labelOpacityBox = AddTextSetting(root, "Label pill opacity", "0.95");
			coverOpacityBox = AddTextSetting(root, "TP/SL cover opacity", "0.95");

			statusText = new TextBlock {
				TextWrapping = TextWrapping.Wrap,
				Foreground = Brushes.LightGray,
				Margin = new Thickness(0, 0, 0, 16)
			};
			root.Children.Add(statusText);

			StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
			Button saveButton = new Button { Content = "Save", MinWidth = 70, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(8, 4, 8, 4) };
			Button closeButton = new Button { Content = "Close", MinWidth = 70, Padding = new Thickness(8, 4, 8, 4) };
			saveButton.Click += (s, e) => Save();
			closeButton.Click += (s, e) => Close();
			buttons.Children.Add(saveButton);
			buttons.Children.Add(closeButton);
			root.Children.Add(buttons);

			Content = scroll;
			LoadSettings();
			Closed += (s, e) => instance = null;
		}

		private TextBox AddTextSetting(Panel root, string label, string fallback)
		{
			root.Children.Add(new TextBlock { Text = label, Foreground = Brushes.LightGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
			TextBox box = new TextBox { Text = fallback, Margin = new Thickness(0, 0, 0, 8) };
			root.Children.Add(box);
			return box;
		}

		private ComboBox AddComboSetting(Panel root, string label, string[] choices)
		{
			root.Children.Add(new TextBlock { Text = label, Foreground = Brushes.LightGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
			ComboBox box = new ComboBox { IsEditable = true, Margin = new Thickness(0, 0, 0, 8) };
			foreach (string choice in choices)
				box.Items.Add(choice);
			root.Children.Add(box);
			return box;
		}

		public static void ShowOrActivate()
		{
			if (instance == null)
				instance = new OrcaExecutionRouterWindow();

			if (!instance.IsVisible)
				instance.Show();
			instance.Activate();
		}

		private void LoadSettings()
		{
			OrcaExecutionRouterSettings settings = OrcaExecutionRouter.GetSettings();
			enabledBox.IsChecked = settings.Enabled;
			nqToMnqBox.IsChecked = settings.RouteNqToMnq;
			positionColorBox.Text = settings.PositionColor;
			shortPositionColorBox.Text = settings.ShortPositionColor;
			buyColorBox.Text = settings.BuyColor;
			sellColorBox.Text = settings.SellColor;
			textColorBox.Text = settings.TextColor;
			dragColorBox.Text = settings.DragChangeColor;
			fontFamilyBox.Text = settings.FontFamily;
			fontWeightBox.Text = settings.FontWeight;
			pnlBadgePositionBox.Text = settings.PnlBadgePosition;
			fontSizeBox.Text = settings.FontSize.ToString("0.##");
			lineThicknessBox.Text = settings.LineThickness.ToString("0.##");
			labelPaddingBox.Text = settings.LabelRightPadding.ToString("0.##");
			labelOpacityBox.Text = settings.LabelBackgroundOpacity.ToString("0.##");
			coverOpacityBox.Text = settings.CoverButtonBackgroundOpacity.ToString("0.##");
			UpdateStatus();
		}

		private void Save()
		{
			OrcaExecutionRouter.SaveSettings(new OrcaExecutionRouterSettings {
				Enabled = enabledBox.IsChecked == true,
				RouteNqToMnq = nqToMnqBox.IsChecked == true,
				PositionColor = positionColorBox.Text,
				ShortPositionColor = shortPositionColorBox.Text,
				BuyColor = buyColorBox.Text,
				SellColor = sellColorBox.Text,
				TextColor = textColorBox.Text,
				DragChangeColor = dragColorBox.Text,
				FontFamily = fontFamilyBox.Text,
				FontWeight = fontWeightBox.Text,
				PnlBadgePosition = pnlBadgePositionBox.Text,
				FontSize = ParseDouble(fontSizeBox.Text, 11),
				LineThickness = ParseDouble(lineThicknessBox.Text, 1),
				LabelRightPadding = ParseDouble(labelPaddingBox.Text, 6),
				LabelBackgroundOpacity = ParseDouble(labelOpacityBox.Text, 0.95),
				CoverButtonBackgroundOpacity = ParseDouble(coverOpacityBox.Text, 0.95)
			});
			UpdateStatus();
		}

		private double ParseDouble(string text, double fallback)
		{
			double value;
			return double.TryParse(text, out value) ? value : fallback;
		}

		private void UpdateStatus()
		{
			statusText.Text = enabledBox.IsChecked == true && nqToMnqBox.IsChecked == true
				? "Enabled: Orca Risk Manager will use NQ chart prices but submit and size orders on the matching MNQ contract."
				: "Disabled: Orca Risk Manager uses its normal chart/Chart Trader instrument behavior.";
		}
	}
}
