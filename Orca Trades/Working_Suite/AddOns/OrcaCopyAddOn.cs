#region Using declarations
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	public sealed class OrcaCopyAddOn : AddOnBase
	{
		private NTMenuItem copierMenuItem;
		private NTMenuItem hostMenu;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults) {
				Description = "Orca multi-account trade copier with follower guard and LAN/WAN remote nodes";
				Name = "Orca Trade Copier";
			}
		}

		protected override void OnWindowCreated(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || copierMenuItem != null)
				return;

			hostMenu = controlCenter.FindFirst("ControlCenterMenuItemTools") as NTMenuItem
				?? controlCenter.FindFirst("toolsMenuItem") as NTMenuItem
				?? controlCenter.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
			if (hostMenu == null)
				return;

			copierMenuItem = new NTMenuItem {
				Header = "Orca Trade Copier",
				Style = Application.Current.TryFindResource("MainMenuItem") as Style
			};
			copierMenuItem.Click += OnMenuItemClick;
			hostMenu.Items.Add(copierMenuItem);
		}

		protected override void OnWindowDestroyed(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || copierMenuItem == null || hostMenu == null)
				return;

			copierMenuItem.Click -= OnMenuItemClick;
			hostMenu.Items.Remove(copierMenuItem);
			copierMenuItem = null;
			hostMenu = null;
		}

		private void OnMenuItemClick(object sender, RoutedEventArgs e)
		{
			Application.Current.Dispatcher.InvokeAsync(() => OrcaTradeCopierWindow.ShowOrActivate());
		}
	}

	public sealed class OrcaTradeCopierWindow : NTWindow
	{
		private static OrcaTradeCopierWindow instance;
		private readonly OrcaCopyViewModel viewModel;
		private DataGrid followerGrid;
		private DataGrid healthGrid;
		private TextBlock statusText;

		private OrcaTradeCopierWindow()
		{
			Caption = "Orca Trade Copier";
			Title = "Orca Trade Copier";
			Width = 980;
			Height = 680;
			MinWidth = 760;
			MinHeight = 520;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Background = Brush("#FF0F141B");
			Foreground = Brush("#FFEAF0F6");

			viewModel = new OrcaCopyViewModel(Dispatcher);
			DataContext = viewModel;

			Grid root = new Grid { Margin = new Thickness(0), Background = Brush("#FF0F141B") };
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			FrameworkElement header = BuildHeader();
			Grid.SetRow(header, 0);
			root.Children.Add(header);

			FrameworkElement tabs = BuildTabs();
			Grid.SetRow(tabs, 1);
			root.Children.Add(tabs);

			Content = root;
			Closed += OnClosed;
		}

		public static void ShowOrActivate()
		{
			if (instance == null)
				instance = new OrcaTradeCopierWindow();

			if (!instance.IsVisible)
				instance.Show();
			instance.Activate();
		}

		private FrameworkElement BuildHeader()
		{
			Grid header = new Grid {
				Margin = new Thickness(14, 14, 14, 12)
			};
			header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			StackPanel titles = new StackPanel { Orientation = Orientation.Vertical };
			titles.Children.Add(new TextBlock {
				Text = "Orca Trade Copier",
				FontSize = 24,
				FontWeight = FontWeights.SemiBold,
				Foreground = Brush("#FFF5F8FB")
			});
			titles.Children.Add(new TextBlock {
				Text = "Leader, followers, guard state, and remote node transport",
				FontSize = 12,
				Foreground = Brush("#FF8EA0B5"),
				Margin = new Thickness(1, 3, 0, 0)
			});
			Grid.SetColumn(titles, 0);
			header.Children.Add(titles);

			statusText = new TextBlock {
				MinWidth = 170,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				TextAlignment = TextAlignment.Right,
				FontSize = 13,
				FontWeight = FontWeights.SemiBold,
				Foreground = Brush("#FF6EE7A8")
			};
			statusText.SetBinding(TextBlock.TextProperty, new Binding("StatusText"));
			Grid.SetColumn(statusText, 1);
			header.Children.Add(statusText);

			Border alert = new Border {
				Margin = new Thickness(0, 12, 0, 0),
				Padding = new Thickness(10, 7, 10, 7),
				CornerRadius = new CornerRadius(5),
				Background = Brush("#FF5A1721"),
				BorderBrush = Brush("#FFE23A52"),
				BorderThickness = new Thickness(1)
			};
			alert.SetBinding(UIElement.VisibilityProperty, new Binding("AlertText") { Converter = new OrcaStringVisibilityConverter() });
			TextBlock alertText = new TextBlock {
				Foreground = Brush("#FFFFD7DE"),
				FontSize = 12,
				FontWeight = FontWeights.SemiBold,
				TextWrapping = TextWrapping.Wrap
			};
			alertText.SetBinding(TextBlock.TextProperty, new Binding("AlertText"));
			alert.Child = alertText;
			Grid.SetColumnSpan(alert, 2);
			Grid.SetRow(alert, 1);
			header.Children.Add(alert);
			return header;
		}

		private FrameworkElement BuildTabs()
		{
			TabControl tabs = new TabControl {
				Margin = new Thickness(14, 0, 14, 14),
				Background = Brush("#FF0F141B"),
				BorderBrush = Brush("#FF273343"),
				Foreground = Brush("#FFEAF0F6")
			};
			tabs.Items.Add(new TabItem {
				Header = "Copier",
				Foreground = Brush("#FFEAF0F6"),
				Background = Brush("#FF141D27"),
				Content = BuildCopierTab()
			});
			tabs.Items.Add(new TabItem {
				Header = "Health",
				Foreground = Brush("#FFEAF0F6"),
				Background = Brush("#FF141D27"),
				Content = BuildHealthTab()
			});
			return tabs;
		}

		private FrameworkElement BuildCopierTab()
		{
			Grid tab = new Grid { Background = Brush("#FF0F141B") };
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			FrameworkElement controls = BuildControls();
			Grid.SetRow(controls, 0);
			tab.Children.Add(controls);

			followerGrid = BuildFollowerGrid();
			Border gridShell = new Border {
				Margin = new Thickness(0, 0, 0, 12),
				BorderThickness = new Thickness(1),
				BorderBrush = Brush("#FF2A3747"),
				Background = Brush("#FF121A23"),
				CornerRadius = new CornerRadius(6),
				Child = followerGrid
			};
			Grid.SetRow(gridShell, 1);
			tab.Children.Add(gridShell);

			FrameworkElement footer = BuildFooter();
			Grid.SetRow(footer, 2);
			tab.Children.Add(footer);
			return tab;
		}

		private FrameworkElement BuildHealthTab()
		{
			Grid tab = new Grid { Background = Brush("#FF0F141B") };
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			UniformGrid cards = new UniformGrid { Columns = 4, Margin = new Thickness(0, 14, 0, 14) };
			cards.Children.Add(BuildMetricCard("Total Net", "TotalNetPnl"));
			cards.Children.Add(BuildMetricCard("Unrealized", "TotalUnrealizedPnl"));
			cards.Children.Add(BuildMetricCard("Realized", "TotalRealizedPnl"));
			cards.Children.Add(BuildMetricCard("Accounts", "HealthAccountCount"));
			Grid.SetRow(cards, 0);
			tab.Children.Add(cards);

			healthGrid = BuildHealthGrid();
			Border healthShell = new Border {
				BorderThickness = new Thickness(1),
				BorderBrush = Brush("#FF2A3747"),
				Background = Brush("#FF121A23"),
				CornerRadius = new CornerRadius(6),
				Child = healthGrid
			};
			Grid.SetRow(healthShell, 1);
			tab.Children.Add(healthShell);

			TextBlock updated = new TextBlock {
				Margin = new Thickness(0, 10, 0, 0),
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 12,
				HorizontalAlignment = HorizontalAlignment.Right
			};
			updated.SetBinding(TextBlock.TextProperty, new Binding("LastHealthUpdate"));
			Grid.SetRow(updated, 2);
			tab.Children.Add(updated);
			return tab;
		}

		private FrameworkElement BuildControls()
		{
			Border shell = new Border {
				Margin = new Thickness(14, 0, 14, 14),
				Padding = new Thickness(12),
				BorderThickness = new Thickness(1),
				BorderBrush = Brush("#FF273343"),
				Background = Brush("#FF141D27"),
				CornerRadius = new CornerRadius(6)
			};

			Grid grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			for (int i = 0; i < 8; i++)
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = i == 7 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

			ComboBox leaderBox = Combo("Leader", 190);
			leaderBox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("AccountNames"));
			leaderBox.SetBinding(Selector.SelectedItemProperty, TwoWay("LeaderAccountName"));
			AddControl(grid, leaderBox, 0, 0);

			ComboBox methodBox = Combo("Method", 145);
			methodBox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("CopyMethods"));
			methodBox.SetBinding(Selector.SelectedItemProperty, TwoWay("CopyMethod"));
			AddControl(grid, methodBox, 0, 1);

			TextBox multiplierBox = InputBox("Multiplier", 80);
			multiplierBox.SetBinding(TextBox.TextProperty, TwoWay("Multiplier"));
			AddControl(grid, multiplierBox, 0, 2);

			TextBox fixedQtyBox = InputBox("Fixed Qty", 76);
			fixedQtyBox.SetBinding(TextBox.TextProperty, TwoWay("FixedQuantity"));
			AddControl(grid, fixedQtyBox, 0, 3);

			TextBox slipBox = InputBox("Warn Slip", 76);
			slipBox.SetBinding(TextBox.TextProperty, TwoWay("MaxSlippageTicks"));
			AddControl(grid, slipBox, 0, 4);

			TextBox hardSlipBox = InputBox("Hard Slip", 76);
			hardSlipBox.SetBinding(TextBox.TextProperty, TwoWay("HardSlippageTicks"));
			AddControl(grid, hardSlipBox, 0, 5);

			TextBox latencyBox = InputBox("Warn ms", 76);
			latencyBox.SetBinding(TextBox.TextProperty, TwoWay("WarningLatencyMs"));
			AddControl(grid, latencyBox, 0, 6);

			Button refresh = GhostButton("Refresh");
			refresh.SetBinding(Button.CommandProperty, new Binding("RefreshAccountsCommand"));
			AddControl(grid, refresh, 0, 7);

			ComboBox modeBox = Combo("Network", 150);
			modeBox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("NetworkModes"));
			modeBox.SetBinding(Selector.SelectedItemProperty, TwoWay("NetworkMode"));
			AddControl(grid, modeBox, 1, 0);

			TextBox hostBox = InputBox("Host", 150);
			hostBox.SetBinding(TextBox.TextProperty, TwoWay("NetworkHost"));
			AddControl(grid, hostBox, 1, 1);

			TextBox portBox = InputBox("Port", 80);
			portBox.SetBinding(TextBox.TextProperty, TwoWay("NetworkPort"));
			AddControl(grid, portBox, 1, 2);

			Button arm = PrimaryButton("ARM");
			arm.SetBinding(Button.CommandProperty, new Binding("ArmCommand"));
			AddControl(grid, arm, 1, 3);

			Button stop = GhostButton("STOP");
			stop.SetBinding(Button.CommandProperty, new Binding("StopCommand"));
			AddControl(grid, stop, 1, 4);

			Button rearm = GreenButton("REARM");
			rearm.SetBinding(Button.CommandProperty, new Binding("RearmCommand"));
			AddControl(grid, rearm, 1, 5);

			Button flatten = DangerButton("FLATTEN ALL");
			flatten.SetBinding(Button.CommandProperty, new Binding("FlattenAllCommand"));
			AddControl(grid, flatten, 1, 6);

			shell.Child = grid;
			return shell;
		}

		private DataGrid BuildFollowerGrid()
		{
			DataGrid grid = new DataGrid {
				AutoGenerateColumns = false,
				CanUserAddRows = false,
				CanUserDeleteRows = false,
				CanUserReorderColumns = false,
				CanUserResizeRows = false,
				GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
				HeadersVisibility = DataGridHeadersVisibility.Column,
				RowHeaderWidth = 0,
				SelectionMode = DataGridSelectionMode.Single,
				BorderThickness = new Thickness(0),
				Background = Brush("#FF121A23"),
				Foreground = Brush("#FFEAF0F6"),
				HorizontalGridLinesBrush = Brush("#FF213042"),
				VerticalGridLinesBrush = Brushes.Transparent,
				RowBackground = Brush("#FF121A23"),
				AlternatingRowBackground = Brush("#FF15202C"),
				ColumnHeaderStyle = BuildHeaderStyle(),
				RowStyle = BuildRowStyle()
			};
			grid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Followers"));

			grid.Columns.Add(BuildStatusColumn());
			grid.Columns.Add(CheckColumn("Enable", "Enabled", 70));
			grid.Columns.Add(TextColumn("Account Name", "DisplayName", 190));
			grid.Columns.Add(TextColumn("Connection Name", "ConnectionName", 170));
			grid.Columns.Add(CheckColumn("ATM Copy", "AtmCopy", 86));
			grid.Columns.Add(TextColumn("Latency (ms)", "LatencyMs", 96));
			DataGridTextColumn slip = TextColumn("Avg Slippage (Ticks)", "AverageSlippageTicks", 142);
			slip.Binding = new Binding("AverageSlippageTicks") { StringFormat = "0.##" };
			grid.Columns.Add(slip);
			grid.Columns.Add(TextColumn("Guard", "GuardMessage", 210));
			grid.Columns.Add(BuildFlattenColumn());
			return grid;
		}

		private DataGrid BuildHealthGrid()
		{
			DataGrid grid = new DataGrid {
				AutoGenerateColumns = false,
				CanUserAddRows = false,
				CanUserDeleteRows = false,
				CanUserReorderColumns = false,
				CanUserResizeRows = false,
				GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
				HeadersVisibility = DataGridHeadersVisibility.Column,
				RowHeaderWidth = 0,
				SelectionMode = DataGridSelectionMode.Single,
				BorderThickness = new Thickness(0),
				Background = Brush("#FF121A23"),
				Foreground = Brush("#FFEAF0F6"),
				HorizontalGridLinesBrush = Brush("#FF213042"),
				VerticalGridLinesBrush = Brushes.Transparent,
				RowBackground = Brush("#FF121A23"),
				AlternatingRowBackground = Brush("#FF15202C"),
				ColumnHeaderStyle = BuildHeaderStyle(),
				RowStyle = BuildRowStyle()
			};
			grid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("HealthRows"));
			grid.Columns.Add(TextColumn("Account", "DisplayName", 160));
			grid.Columns.Add(TextColumn("Role", "Role", 82));
			grid.Columns.Add(TextColumn("Sync", "SyncState", 96));
			grid.Columns.Add(TextColumn("Position", "PositionText", 210));
			grid.Columns.Add(TextColumn("Orders", "WorkingOrdersText", 82));
			grid.Columns.Add(PnlColumn("Unrealized", "UnrealizedPnl", 110));
			grid.Columns.Add(PnlColumn("Realized", "RealizedPnl", 110));
			grid.Columns.Add(PnlColumn("Net", "NetPnl", 110));
			grid.Columns.Add(TextColumn("Guard", "GuardText", 190));
			return grid;
		}

		private FrameworkElement BuildMetricCard(string title, string bindingPath)
		{
			Border card = new Border {
				Margin = new Thickness(0, 0, 10, 0),
				Padding = new Thickness(13, 10, 13, 10),
				CornerRadius = new CornerRadius(6),
				Background = Brush("#FF141D27"),
				BorderBrush = Brush("#FF273343"),
				BorderThickness = new Thickness(1)
			};
			StackPanel stack = new StackPanel();
			stack.Children.Add(new TextBlock {
				Text = title,
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 11,
				FontWeight = FontWeights.SemiBold
			});
			TextBlock value = new TextBlock {
				Margin = new Thickness(0, 4, 0, 0),
				FontSize = 20,
				FontWeight = FontWeights.SemiBold,
				Foreground = Brush("#FFF5F8FB")
			};
			Binding binding = new Binding(bindingPath);
			if (!string.Equals(bindingPath, "HealthAccountCount", StringComparison.Ordinal))
				binding.StringFormat = "{0:C2}";
			value.SetBinding(TextBlock.TextProperty, binding);
			if (!string.Equals(bindingPath, "HealthAccountCount", StringComparison.Ordinal))
				value.SetBinding(TextBlock.ForegroundProperty, new Binding(bindingPath) { Converter = new OrcaPnlBrushConverter() });
			stack.Children.Add(value);
			card.Child = stack;
			return card;
		}

		private FrameworkElement BuildFooter()
		{
			Grid footer = new Grid {
				Margin = new Thickness(14, 0, 14, 14)
			};
			footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			TextBlock network = new TextBlock {
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 12,
				VerticalAlignment = VerticalAlignment.Center
			};
			network.SetBinding(TextBlock.TextProperty, new Binding("NetworkStatus"));
			Grid.SetColumn(network, 0);
			footer.Children.Add(network);

			TextBlock lastRecord = new TextBlock {
				Foreground = Brush("#FFB8C6D8"),
				FontSize = 12,
				VerticalAlignment = VerticalAlignment.Center,
				TextAlignment = TextAlignment.Right
			};
			lastRecord.SetBinding(TextBlock.TextProperty, new Binding("LastLogLine"));
			Grid.SetColumn(lastRecord, 1);
			footer.Children.Add(lastRecord);
			return footer;
		}

		private DataGridTemplateColumn BuildStatusColumn()
		{
			FrameworkElementFactory ellipse = new FrameworkElementFactory(typeof(Ellipse));
			ellipse.SetValue(FrameworkElement.WidthProperty, 14.0);
			ellipse.SetValue(FrameworkElement.HeightProperty, 14.0);
			ellipse.SetValue(Shape.StrokeProperty, Brush("#882A3747"));
			ellipse.SetValue(Shape.StrokeThicknessProperty, 1.0);
			ellipse.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
			ellipse.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
			ellipse.SetBinding(Shape.FillProperty, new Binding("Status") { Converter = new OrcaStatusBrushConverter() });
			ellipse.SetValue(UIElement.EffectProperty, new DropShadowEffect {
				BlurRadius = 12,
				ShadowDepth = 0,
				Opacity = 0.75,
				Color = Color.FromRgb(94, 234, 171)
			});

			return new DataGridTemplateColumn {
				Header = "",
				Width = new DataGridLength(42),
				CellTemplate = new DataTemplate { VisualTree = ellipse }
			};
		}

		private DataGridTemplateColumn BuildFlattenColumn()
		{
			FrameworkElementFactory button = new FrameworkElementFactory(typeof(Button));
			button.SetValue(ContentControl.ContentProperty, "Flatten");
			button.SetValue(FrameworkElement.MinWidthProperty, 76.0);
			button.SetValue(Control.PaddingProperty, new Thickness(8, 3, 8, 3));
			button.SetValue(Control.MarginProperty, new Thickness(4, 2, 4, 2));
			button.SetValue(Control.ForegroundProperty, Brush("#FFE6ECF4"));
			button.SetValue(Control.BackgroundProperty, Brush("#FF2B3340"));
			button.SetValue(Control.BorderBrushProperty, Brush("#FF5D6978"));
			button.SetValue(Control.BorderThicknessProperty, new Thickness(1));
			button.SetBinding(FrameworkElement.TagProperty, new Binding());
			button.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnFollowerFlattenClick));

			return new DataGridTemplateColumn {
				Header = "Flatten",
				Width = new DataGridLength(98),
				CellTemplate = new DataTemplate { VisualTree = button }
			};
		}

		private DataGridTextColumn TextColumn(string header, string path, double width)
		{
			return new DataGridTextColumn {
				Header = header,
				Binding = new Binding(path),
				Width = new DataGridLength(width),
				ElementStyle = BuildCellTextStyle()
			};
		}

		private DataGridTextColumn PnlColumn(string header, string path, double width)
		{
			DataGridTextColumn column = TextColumn(header, path, width);
			column.Binding = new Binding(path) { StringFormat = "{0:C2}" };
			return column;
		}

		private DataGridCheckBoxColumn CheckColumn(string header, string path, double width)
		{
			return new DataGridCheckBoxColumn {
				Header = header,
				Binding = new Binding(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
				Width = new DataGridLength(width),
				ElementStyle = BuildCheckStyle(),
				EditingElementStyle = BuildCheckStyle()
			};
		}

		private Style BuildHeaderStyle()
		{
			Style style = new Style(typeof(DataGridColumnHeader));
			style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#FF182332")));
			style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#FF9FB0C4")));
			style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
			style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
			style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 9, 8, 9)));
			style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#FF263549")));
			style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
			return style;
		}

		private Style BuildRowStyle()
		{
			Style style = new Style(typeof(DataGridRow));
			style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#FFEAF0F6")));
			style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
			style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 34.0));
			style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#FF213042")));
			style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
			return style;
		}

		private Style BuildCellTextStyle()
		{
			Style style = new Style(typeof(TextBlock));
			style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
			style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 0, 8, 0)));
			style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
			return style;
		}

		private Style BuildCheckStyle()
		{
			Style style = new Style(typeof(CheckBox));
			style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));
			style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
			return style;
		}

		private ComboBox Combo(string label, double width)
		{
			ComboBox box = new ComboBox {
				Width = width,
				MinHeight = 29,
				Margin = new Thickness(0, 2, 10, 10),
				Background = Brush("#FF0F141B"),
				Foreground = Brush("#FFEAF0F6"),
				BorderBrush = Brush("#FF334256")
			};
			box.Tag = label;
			return box;
		}

		private TextBox InputBox(string label, double width)
		{
			TextBox box = new TextBox {
				Width = width,
				MinHeight = 29,
				Margin = new Thickness(0, 2, 10, 10),
				Padding = new Thickness(7, 4, 7, 4),
				Background = Brush("#FF0F141B"),
				Foreground = Brush("#FFEAF0F6"),
				BorderBrush = Brush("#FF334256"),
				CaretBrush = Brush("#FFFFFFFF")
			};
			box.Tag = label;
			return box;
		}

		private Button PrimaryButton(string text)
		{
			Button button = BaseButton(text);
			button.Background = Brush("#FF1D6A4A");
			button.BorderBrush = Brush("#FF58E19B");
			return button;
		}

		private Button GreenButton(string text)
		{
			Button button = BaseButton(text);
			button.Background = Brush("#FF151F1B");
			button.BorderBrush = Brush("#FF58E19B");
			return button;
		}

		private Button GhostButton(string text)
		{
			Button button = BaseButton(text);
			button.Background = Brush("#FF202B3A");
			button.BorderBrush = Brush("#FF425168");
			return button;
		}

		private Button DangerButton(string text)
		{
			Button button = BaseButton(text);
			LinearGradientBrush gradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
			gradient.GradientStops.Add(new GradientStop(Color.FromRgb(125, 24, 36), 0));
			gradient.GradientStops.Add(new GradientStop(Color.FromRgb(226, 58, 82), 1));
			button.Background = gradient;
			button.BorderBrush = Brush("#FFFF8294");
			button.FontWeight = FontWeights.SemiBold;
			button.MinWidth = 118;
			return button;
		}

		private Button BaseButton(string text)
		{
			return new Button {
				Content = text,
				MinWidth = 74,
				MinHeight = 29,
				Margin = new Thickness(0, 2, 10, 10),
				Padding = new Thickness(11, 4, 11, 4),
				Foreground = Brush("#FFF5F8FB"),
				BorderThickness = new Thickness(1)
			};
		}

		private void AddControl(Grid grid, FrameworkElement control, int row, int column)
		{
			StackPanel stack = new StackPanel { Orientation = Orientation.Vertical };
			string label = control.Tag as string;
			if (!string.IsNullOrWhiteSpace(label)) {
				stack.Children.Add(new TextBlock {
					Text = label,
					Foreground = Brush("#FF8EA0B5"),
					FontSize = 11,
					Margin = new Thickness(1, 0, 0, 0)
				});
			}
			stack.Children.Add(control);
			Grid.SetRow(stack, row);
			Grid.SetColumn(stack, column);
			grid.Children.Add(stack);
		}

		private Binding TwoWay(string path)
		{
			return new Binding(path) {
				Mode = BindingMode.TwoWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};
		}

		private void OnFollowerFlattenClick(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;
			OrcaFollowerAccountState follower = button == null ? null : button.Tag as OrcaFollowerAccountState;
			if (follower == null)
				return;

			MessageBoxResult result = MessageBox.Show(this, "Flatten " + follower.AccountName + "?", "Orca Trade Copier", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result == MessageBoxResult.Yes)
				viewModel.FlattenFollower(follower);
		}

		private void OnClosed(object sender, EventArgs e)
		{
			viewModel.Dispose();
			instance = null;
		}

		private static Brush Brush(string color)
		{
			return (Brush)new BrushConverter().ConvertFrom(color);
		}
	}

	public sealed class OrcaCopyViewModel : INotifyPropertyChanged, IDisposable
	{
		private readonly OrcaCopyEngine engine;
		private readonly Dispatcher dispatcher;
		private readonly DispatcherTimer healthTimer;
		private Dictionary<string, OrcaCopyFollowerSetting> loadedFollowerSettings = new Dictionary<string, OrcaCopyFollowerSetting>(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> alertedDisarmedAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private bool isRefreshing;
		private string leaderAccountName;
		private OrcaCopyMethod copyMethod;
		private double multiplier;
		private int fixedQuantity;
		private int maxSlippageTicks;
		private int hardSlippageTicks;
		private int warningLatencyMs;
		private OrcaCopyNetworkMode networkMode;
		private string networkHost;
		private int networkPort;
		private string statusText;
		private string alertText;
		private string networkStatus;
		private string lastLogLine;
		private string lastHealthUpdate;
		private double totalUnrealizedPnl;
		private double totalRealizedPnl;
		private double totalNetPnl;
		private int healthAccountCount;

		public OrcaCopyViewModel(Dispatcher dispatcher)
		{
			this.dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
			engine = new OrcaCopyEngine(this.dispatcher);
			engine.StatusChanged += OnEngineStatusChanged;
			OrcaCopyTradeLogger.RecordReady += OnTradeLogReady;

			AccountNames = new ObservableCollection<string>();
			Followers = new ObservableCollection<OrcaFollowerAccountState>();
			HealthRows = new ObservableCollection<OrcaAccountHealthState>();
			CopyMethods = new ObservableCollection<OrcaCopyMethod>((OrcaCopyMethod[])Enum.GetValues(typeof(OrcaCopyMethod)));
			NetworkModes = new ObservableCollection<OrcaCopyNetworkMode>((OrcaCopyNetworkMode[])Enum.GetValues(typeof(OrcaCopyNetworkMode)));

			ArmCommand = new OrcaRelayCommand(_ => Arm(), _ => !engine.IsRunning);
			StopCommand = new OrcaRelayCommand(_ => Stop(), _ => engine.IsRunning);
			RearmCommand = new OrcaRelayCommand(_ => Rearm(), _ => true);
			FlattenAllCommand = new OrcaRelayCommand(_ => FlattenAll(), _ => Followers.Count > 0);
			RefreshAccountsCommand = new OrcaRelayCommand(_ => RefreshAccounts(), _ => !isRefreshing);

			LoadSettings();
			RefreshAccounts();
			healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			healthTimer.Tick += (s, e) => UpdateHealth();
			healthTimer.Start();
			UpdateHealth();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public ObservableCollection<string> AccountNames { get; private set; }
		public ObservableCollection<OrcaFollowerAccountState> Followers { get; private set; }
		public ObservableCollection<OrcaAccountHealthState> HealthRows { get; private set; }
		public ObservableCollection<OrcaCopyMethod> CopyMethods { get; private set; }
		public ObservableCollection<OrcaCopyNetworkMode> NetworkModes { get; private set; }
		public ICommand ArmCommand { get; private set; }
		public ICommand StopCommand { get; private set; }
		public ICommand RearmCommand { get; private set; }
		public ICommand FlattenAllCommand { get; private set; }
		public ICommand RefreshAccountsCommand { get; private set; }

		public string LeaderAccountName
		{
			get { return leaderAccountName; }
			set { Set(ref leaderAccountName, value, "LeaderAccountName"); }
		}

		public OrcaCopyMethod CopyMethod
		{
			get { return copyMethod; }
			set { Set(ref copyMethod, value, "CopyMethod"); }
		}

		public double Multiplier
		{
			get { return multiplier; }
			set { Set(ref multiplier, value <= 0 ? 1 : value, "Multiplier"); }
		}

		public int FixedQuantity
		{
			get { return fixedQuantity; }
			set { Set(ref fixedQuantity, Math.Max(1, value), "FixedQuantity"); }
		}

		public int MaxSlippageTicks
		{
			get { return maxSlippageTicks; }
			set { Set(ref maxSlippageTicks, Math.Max(0, value), "MaxSlippageTicks"); }
		}

		public int HardSlippageTicks
		{
			get { return hardSlippageTicks; }
			set { Set(ref hardSlippageTicks, Math.Max(0, value), "HardSlippageTicks"); }
		}

		public int WarningLatencyMs
		{
			get { return warningLatencyMs; }
			set { Set(ref warningLatencyMs, Math.Max(0, value), "WarningLatencyMs"); }
		}

		public OrcaCopyNetworkMode NetworkMode
		{
			get { return networkMode; }
			set { Set(ref networkMode, value, "NetworkMode"); }
		}

		public string NetworkHost
		{
			get { return networkHost; }
			set { Set(ref networkHost, value, "NetworkHost"); }
		}

		public int NetworkPort
		{
			get { return networkPort; }
			set { Set(ref networkPort, Math.Max(1, Math.Min(65535, value)), "NetworkPort"); }
		}

		public string StatusText
		{
			get { return statusText; }
			set { Set(ref statusText, value, "StatusText"); }
		}

		public string AlertText
		{
			get { return alertText; }
			set { Set(ref alertText, value, "AlertText"); }
		}

		public string NetworkStatus
		{
			get { return networkStatus; }
			set { Set(ref networkStatus, value, "NetworkStatus"); }
		}

		public string LastLogLine
		{
			get { return lastLogLine; }
			set { Set(ref lastLogLine, value, "LastLogLine"); }
		}

		public string LastHealthUpdate
		{
			get { return lastHealthUpdate; }
			set { Set(ref lastHealthUpdate, value, "LastHealthUpdate"); }
		}

		public double TotalUnrealizedPnl
		{
			get { return totalUnrealizedPnl; }
			set { Set(ref totalUnrealizedPnl, value, "TotalUnrealizedPnl"); }
		}

		public double TotalRealizedPnl
		{
			get { return totalRealizedPnl; }
			set { Set(ref totalRealizedPnl, value, "TotalRealizedPnl"); }
		}

		public double TotalNetPnl
		{
			get { return totalNetPnl; }
			set { Set(ref totalNetPnl, value, "TotalNetPnl"); }
		}

		public int HealthAccountCount
		{
			get { return healthAccountCount; }
			set { Set(ref healthAccountCount, value, "HealthAccountCount"); }
		}

		public void FlattenFollower(OrcaFollowerAccountState follower)
		{
			engine.FlattenFollower(follower, "Manual follower flatten");
		}

		public void Dispose()
		{
			SaveSettings();
			if (healthTimer != null)
				healthTimer.Stop();
			OrcaCopyTradeLogger.RecordReady -= OnTradeLogReady;
			engine.StatusChanged -= OnEngineStatusChanged;
			engine.Dispose();
			foreach (OrcaFollowerAccountState follower in Followers)
				follower.PropertyChanged -= OnFollowerPropertyChanged;
		}

		private void Arm()
		{
			try {
				SaveSettings();
				engine.Start(BuildSettings(), Followers);
				UpdateGuardSummary();
				RaiseCommandStates();
			} catch (Exception ex) {
				StatusText = "ERROR";
				AlertText = "OrcaTradeCopier could not arm: " + ex.Message;
				NetworkStatus = ex.Message;
				OrcaCopyDiagnostics.Print("Arm failed: " + ex.Message, LogLevel.Error);
			}
		}

		private void Stop()
		{
			engine.Stop();
			StatusText = "STOPPED";
			AlertText = string.Empty;
			RaiseCommandStates();
		}

		private void Rearm()
		{
			alertedDisarmedAccounts.Clear();
			engine.RearmAll();
			engine.RefreshFollowers(Followers);
			SaveSettings();
			UpdateGuardSummary();
		}

		private void FlattenAll()
		{
			MessageBoxResult result = MessageBox.Show("Flatten all enabled follower accounts?", "Orca Trade Copier", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result == MessageBoxResult.Yes)
				engine.FlattenAll();
		}

		private void RefreshAccounts()
		{
			isRefreshing = true;
			try {
				IEnumerable<OrcaCopyFollowerSetting> sourceSettings = Followers.Count > 0 ? (IEnumerable<OrcaCopyFollowerSetting>)BuildSettings().Followers : loadedFollowerSettings.Values;
				Dictionary<string, OrcaCopyFollowerSetting> saved = sourceSettings
					.GroupBy(f => f.AccountName, StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

				foreach (OrcaFollowerAccountState follower in Followers)
					follower.PropertyChanged -= OnFollowerPropertyChanged;

				AccountNames.Clear();
				Followers.Clear();

				List<Account> accounts = Account.All
					.Where(IsCurrentTradingAccount)
					.OrderBy(a => a.Name)
					.ToList();
				foreach (Account account in accounts) {
					AccountNames.Add(account.Name);
					OrcaFollowerAccountState state = new OrcaFollowerAccountState(account);
					OrcaCopyFollowerSetting setting;
					if (saved.TryGetValue(account.Name, out setting))
						state.ApplyFollowerSetting(setting);
					state.PropertyChanged += OnFollowerPropertyChanged;
					Followers.Add(state);
				}

				if (string.IsNullOrWhiteSpace(LeaderAccountName) || !AccountNames.Contains(LeaderAccountName))
					LeaderAccountName = AccountNames.FirstOrDefault();
				engine.RefreshFollowers(Followers);
				UpdateGuardSummary();
				UpdateHealth();
			} catch (Exception ex) {
				NetworkStatus = "Account refresh failed: " + ex.Message;
				OrcaCopyDiagnostics.Print(NetworkStatus, LogLevel.Warning);
			} finally {
				isRefreshing = false;
				RaiseCommandStates();
			}
		}

		private void LoadSettings()
		{
			OrcaCopySettings settings = OrcaCopySettingsStore.Load();
			LeaderAccountName = settings.LeaderAccountName;
			CopyMethod = settings.CopyMethod;
			Multiplier = settings.Multiplier;
			FixedQuantity = settings.FixedQuantity;
			MaxSlippageTicks = settings.MaxSlippageTicks;
			HardSlippageTicks = settings.HardSlippageTicks;
			WarningLatencyMs = settings.WarningLatencyMs;
			NetworkMode = settings.NetworkMode;
			NetworkHost = settings.NetworkHost;
			NetworkPort = settings.NetworkPort;
			loadedFollowerSettings = settings.Followers == null
				? new Dictionary<string, OrcaCopyFollowerSetting>(StringComparer.OrdinalIgnoreCase)
				: settings.Followers
					.Where(f => f != null && !string.IsNullOrWhiteSpace(f.AccountName))
					.GroupBy(f => f.AccountName, StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
			StatusText = "STOPPED";
			AlertText = string.Empty;
			NetworkStatus = "Network idle";
			LastLogLine = string.Empty;
			LastHealthUpdate = string.Empty;
		}

		private static bool IsCurrentTradingAccount(Account account)
		{
			if (account == null)
				return false;

			try {
				if (account.Connection == null)
					return false;
				if (account.Connection.Options != null && !account.Connection.Options.CanManageOrders)
					return false;
				return account.ConnectionStatus == ConnectionStatus.Connected
					|| account.Connection.Status == ConnectionStatus.Connected;
			} catch {
				return false;
			}
		}

		private void SaveSettings()
		{
			OrcaCopySettingsStore.Save(BuildSettings());
		}

		private OrcaCopySettings BuildSettings()
		{
			return new OrcaCopySettings {
				LeaderAccountName = LeaderAccountName,
				CopyMethod = CopyMethod,
				Multiplier = Multiplier,
				FixedQuantity = FixedQuantity,
				MaxSlippageTicks = MaxSlippageTicks,
				HardSlippageTicks = HardSlippageTicks,
				WarningLatencyMs = WarningLatencyMs,
				NetworkMode = NetworkMode,
				NetworkHost = NetworkHost,
				NetworkPort = NetworkPort,
				Followers = Followers.Select(f => f.ToSetting()).ToList()
			};
		}

		private void OnFollowerPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (isRefreshing)
				return;
			if (e.PropertyName == "Enabled" || e.PropertyName == "AtmCopy") {
				SaveSettings();
				engine.RefreshFollowers(Followers);
			}
			if (e.PropertyName == "Status" || e.PropertyName == "GuardMessage" || e.PropertyName == "IsDisarmed" || e.PropertyName == "Enabled")
				UpdateGuardSummary();
			if (e.PropertyName == "Status" || e.PropertyName == "GuardMessage" || e.PropertyName == "IsDisarmed" || e.PropertyName == "Enabled" || e.PropertyName == "LatencyMs" || e.PropertyName == "AverageSlippageTicks")
				UpdateHealth();
		}

		private void OnEngineStatusChanged(object sender, string status)
		{
			NetworkStatus = status;
			if (string.Equals(status, "Armed", StringComparison.OrdinalIgnoreCase))
				UpdateGuardSummary();
			else if (string.Equals(status, "Stopped", StringComparison.OrdinalIgnoreCase))
				StatusText = "STOPPED";
			RaiseCommandStates();
		}

		private void UpdateGuardSummary()
		{
			int disarmed = Followers.Count(f => f.IsDisarmed);
			int warnings = Followers.Count(f => !f.IsDisarmed && f.Status == OrcaFollowerGuardStatus.Warning);
			if (engine.IsRunning) {
				if (disarmed > 0)
					StatusText = "ARMED - " + disarmed + " FOLLOWER OFF";
				else if (warnings > 0)
					StatusText = "ARMED - " + warnings + " WARNING";
				else
					StatusText = "ARMED";
			}

			OrcaFollowerAccountState firstDisarmed = Followers.FirstOrDefault(f => f.IsDisarmed);
			if (firstDisarmed != null) {
				AlertText = "FOLLOWER DISARMED: " + firstDisarmed.AccountName + " - " + firstDisarmed.GuardMessage;
				if (alertedDisarmedAccounts.Add(firstDisarmed.AccountName)) {
					try { SystemSounds.Exclamation.Play(); } catch { }
				}
				return;
			}

			OrcaFollowerAccountState firstWarning = Followers.FirstOrDefault(f => f.Status == OrcaFollowerGuardStatus.Warning);
			if (firstWarning != null) {
				AlertText = "Follower warning: " + firstWarning.AccountName + " - " + firstWarning.GuardMessage;
				return;
			}

			if (engine.IsRunning)
				AlertText = string.Empty;
		}

		private void UpdateHealth()
		{
			try {
				Account leader = ResolveAccount(LeaderAccountName);
				string leaderSignature = BuildPositionSignature(leader);
				List<OrcaAccountHealthState> desired = new List<OrcaAccountHealthState>();

				foreach (OrcaFollowerAccountState follower in Followers.OrderBy(f => f.AccountName)) {
					if (follower.Account != null)
						desired.Add(BuildHealthState(follower.Account, follower, leaderSignature));
				}

				if (leader != null && !desired.Any(r => string.Equals(r.AccountName, leader.Name, StringComparison.OrdinalIgnoreCase)))
					desired.Insert(0, BuildHealthState(leader, null, leaderSignature));

				SyncHealthRows(desired);
				TotalUnrealizedPnl = desired.Sum(r => r.UnrealizedPnl);
				TotalRealizedPnl = desired.Sum(r => r.RealizedPnl);
				TotalNetPnl = desired.Sum(r => r.NetPnl);
				HealthAccountCount = desired.Count;
				LastHealthUpdate = "Health updated " + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
			} catch (Exception ex) {
				LastHealthUpdate = "Health update failed: " + ex.Message;
			}
		}

		private OrcaAccountHealthState BuildHealthState(Account account, OrcaFollowerAccountState follower, string leaderSignature)
		{
			bool isLeader = account != null && string.Equals(account.Name, LeaderAccountName, StringComparison.OrdinalIgnoreCase);
			bool isEnabledFollower = follower != null && follower.Enabled && !follower.IsDisarmed;
			string positionSignature = BuildPositionSignature(account);
			double unrealized = SafeAccountGet(account, AccountItem.UnrealizedProfitLoss);
			double realized = SafeAccountGet(account, AccountItem.RealizedProfitLoss);
			return new OrcaAccountHealthState {
				AccountName = account == null ? string.Empty : account.Name,
				DisplayName = account == null ? string.Empty : (string.IsNullOrWhiteSpace(account.DisplayName) ? account.Name : account.DisplayName),
				Role = isLeader ? "Leader" : (follower != null && follower.IsDisarmed ? "Disarmed" : (isEnabledFollower ? "Follower" : "Off")),
				SyncState = BuildSyncState(isLeader, isEnabledFollower, leaderSignature, positionSignature),
				PositionText = BuildPositionText(account),
				WorkingOrdersText = BuildWorkingOrdersText(account),
				UnrealizedPnl = unrealized,
				RealizedPnl = realized,
				NetPnl = unrealized + realized,
				GuardText = isLeader ? "Leader" : (follower == null ? string.Empty : follower.GuardMessage)
			};
		}

		private void SyncHealthRows(List<OrcaAccountHealthState> desired)
		{
			foreach (OrcaAccountHealthState existing in HealthRows.ToList()) {
				if (!desired.Any(r => string.Equals(r.AccountName, existing.AccountName, StringComparison.OrdinalIgnoreCase)))
					HealthRows.Remove(existing);
			}

			for (int i = 0; i < desired.Count; i++) {
				OrcaAccountHealthState next = desired[i];
				OrcaAccountHealthState existing = HealthRows.FirstOrDefault(r => string.Equals(r.AccountName, next.AccountName, StringComparison.OrdinalIgnoreCase));
				if (existing == null) {
					HealthRows.Insert(Math.Min(i, HealthRows.Count), next);
					continue;
				}
				existing.CopyFrom(next);
				int currentIndex = HealthRows.IndexOf(existing);
				if (currentIndex >= 0 && currentIndex != i)
					HealthRows.Move(currentIndex, i);
			}
		}

		private static string BuildSyncState(bool isLeader, bool isEnabledFollower, string leaderSignature, string positionSignature)
		{
			if (isLeader)
				return "Leader";
			if (!isEnabledFollower)
				return "Off";
			if (string.Equals(leaderSignature, positionSignature, StringComparison.OrdinalIgnoreCase))
				return "Synced";
			return "Mismatch";
		}

		private static string BuildPositionSignature(Account account)
		{
			if (account == null)
				return "Flat";
			List<string> parts = new List<string>();
			try {
				foreach (Position position in account.Positions) {
					if (position == null || position.Instrument == null || position.MarketPosition == MarketPosition.Flat || position.Quantity == 0)
						continue;
					parts.Add((position.Instrument.FullName ?? string.Empty) + "|" + position.MarketPosition + "|" + Math.Abs(position.Quantity));
				}
			} catch { }
			if (parts.Count == 0)
				return "Flat";
			parts.Sort(StringComparer.OrdinalIgnoreCase);
			return string.Join(";", parts.ToArray());
		}

		private static string BuildPositionText(Account account)
		{
			if (account == null)
				return "Flat";
			List<string> parts = new List<string>();
			try {
				foreach (Position position in account.Positions) {
					if (position == null || position.Instrument == null || position.MarketPosition == MarketPosition.Flat || position.Quantity == 0)
						continue;
					string instrumentName = position.Instrument.MasterInstrument == null ? position.Instrument.FullName : position.Instrument.MasterInstrument.Name;
					parts.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} @ {3:0.00}", instrumentName, position.MarketPosition, Math.Abs(position.Quantity), position.AveragePrice));
				}
			} catch { }
			return parts.Count == 0 ? "Flat" : string.Join("; ", parts.ToArray());
		}

		private static string BuildWorkingOrdersText(Account account)
		{
			if (account == null)
				return "0";
			try {
				int count = account.Orders.Count(o => o != null && IsLiveWorkingState(o.OrderState));
				return count.ToString(CultureInfo.InvariantCulture);
			} catch {
				return "0";
			}
		}

		private static bool IsLiveWorkingState(OrderState state)
		{
			return state == OrderState.Accepted
				|| state == OrderState.AcceptedByRisk
				|| state == OrderState.Submitted
				|| state == OrderState.Working
				|| state == OrderState.PartFilled
				|| state == OrderState.ChangePending
				|| state == OrderState.ChangeSubmitted
				|| state == OrderState.CancelPending
				|| state == OrderState.CancelSubmitted
				|| state == OrderState.TriggerPending;
		}

		private static double SafeAccountGet(Account account, AccountItem item)
		{
			try {
				if (account == null)
					return 0;
				double value = account.Get(item, Currency.UsDollar);
				if (double.IsNaN(value) || double.IsInfinity(value))
					return 0;
				return value;
			} catch {
				return 0;
			}
		}

		private static Account ResolveAccount(string accountName)
		{
			if (string.IsNullOrWhiteSpace(accountName))
				return null;
			try {
				return Account.All.FirstOrDefault(a => a != null && string.Equals(a.Name, accountName, StringComparison.OrdinalIgnoreCase));
			} catch {
				return null;
			}
		}

		private void OnTradeLogReady(object sender, OrcaCopyLogRecord record)
		{
			if (record == null)
				return;
			dispatcher.BeginInvoke(new Action(() => {
				LastLogLine = string.Format(
					CultureInfo.InvariantCulture,
					"{0:HH:mm:ss} {1} {2} {3} {4:0.##}t",
					record.TimeUtc.ToLocalTime(),
					record.EventType,
					record.FollowerAccount,
					record.Instrument,
					record.SlippageTicks);
			}));
		}

		private bool Set<T>(ref T field, T value, string propertyName)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
				return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		private void RaiseCommandStates()
		{
			OrcaRelayCommand.RaiseCanExecuteChanged(ArmCommand);
			OrcaRelayCommand.RaiseCanExecuteChanged(StopCommand);
			OrcaRelayCommand.RaiseCanExecuteChanged(FlattenAllCommand);
			OrcaRelayCommand.RaiseCanExecuteChanged(RefreshAccountsCommand);
		}
	}

	public sealed class OrcaAccountHealthState : INotifyPropertyChanged
	{
		private string accountName;
		private string displayName;
		private string role;
		private string syncState;
		private string positionText;
		private string workingOrdersText;
		private double unrealizedPnl;
		private double realizedPnl;
		private double netPnl;
		private string guardText;

		public event PropertyChangedEventHandler PropertyChanged;

		public string AccountName
		{
			get { return accountName; }
			set { Set(ref accountName, value, "AccountName"); }
		}

		public string DisplayName
		{
			get { return displayName; }
			set { Set(ref displayName, value, "DisplayName"); }
		}

		public string Role
		{
			get { return role; }
			set { Set(ref role, value, "Role"); }
		}

		public string SyncState
		{
			get { return syncState; }
			set { Set(ref syncState, value, "SyncState"); }
		}

		public string PositionText
		{
			get { return positionText; }
			set { Set(ref positionText, value, "PositionText"); }
		}

		public string WorkingOrdersText
		{
			get { return workingOrdersText; }
			set { Set(ref workingOrdersText, value, "WorkingOrdersText"); }
		}

		public double UnrealizedPnl
		{
			get { return unrealizedPnl; }
			set { Set(ref unrealizedPnl, value, "UnrealizedPnl"); }
		}

		public double RealizedPnl
		{
			get { return realizedPnl; }
			set { Set(ref realizedPnl, value, "RealizedPnl"); }
		}

		public double NetPnl
		{
			get { return netPnl; }
			set { Set(ref netPnl, value, "NetPnl"); }
		}

		public string GuardText
		{
			get { return guardText; }
			set { Set(ref guardText, value, "GuardText"); }
		}

		public void CopyFrom(OrcaAccountHealthState source)
		{
			if (source == null)
				return;
			AccountName = source.AccountName;
			DisplayName = source.DisplayName;
			Role = source.Role;
			SyncState = source.SyncState;
			PositionText = source.PositionText;
			WorkingOrdersText = source.WorkingOrdersText;
			UnrealizedPnl = source.UnrealizedPnl;
			RealizedPnl = source.RealizedPnl;
			NetPnl = source.NetPnl;
			GuardText = source.GuardText;
		}

		private bool Set<T>(ref T field, T value, string propertyName)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
				return false;
			field = value;
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
			return true;
		}
	}

	public sealed class OrcaRelayCommand : ICommand
	{
		private readonly Action<object> execute;
		private readonly Predicate<object> canExecute;

		public OrcaRelayCommand(Action<object> execute, Predicate<object> canExecute)
		{
			this.execute = execute;
			this.canExecute = canExecute;
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			return canExecute == null || canExecute(parameter);
		}

		public void Execute(object parameter)
		{
			if (execute != null)
				execute(parameter);
		}

		public static void RaiseCanExecuteChanged(ICommand command)
		{
			OrcaRelayCommand relay = command as OrcaRelayCommand;
			if (relay != null) {
				EventHandler handler = relay.CanExecuteChanged;
				if (handler != null)
					handler(relay, EventArgs.Empty);
			}
		}
	}

	public sealed class OrcaStatusBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			OrcaFollowerGuardStatus status = value is OrcaFollowerGuardStatus ? (OrcaFollowerGuardStatus)value : OrcaFollowerGuardStatus.Off;
			switch (status) {
				case OrcaFollowerGuardStatus.Active:
					return Brush("#FF5EEAAB");
				case OrcaFollowerGuardStatus.Warning:
					return Brush("#FFF4D35E");
				case OrcaFollowerGuardStatus.Disarmed:
					return Brush("#FFE23A52");
				default:
					return Brush("#FF647386");
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}

		private static Brush Brush(string color)
		{
			return (Brush)new BrushConverter().ConvertFrom(color);
		}
	}

	public sealed class OrcaPnlBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double pnl;
			try { pnl = System.Convert.ToDouble(value, CultureInfo.InvariantCulture); }
			catch { pnl = 0; }
			if (pnl > 0.0001)
				return Brush("#FF5EEAAB");
			if (pnl < -0.0001)
				return Brush("#FFFF8294");
			return Brush("#FFEAF0F6");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}

		private static Brush Brush(string color)
		{
			return (Brush)new BrushConverter().ConvertFrom(color);
		}
	}

	public sealed class OrcaStringVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string text = value as string;
			return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}
