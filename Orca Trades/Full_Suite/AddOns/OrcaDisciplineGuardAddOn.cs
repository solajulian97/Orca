#region Using declarations
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	public sealed class OrcaDisciplineGuardAddOn : AddOnBase
	{
		private NTMenuItem guardMenuItem;
		private NTMenuItem hostMenu;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults) {
				Description = "Orca account-specific discipline grading and rule accountability panel";
				Name = "Orca Discipline Guard";
			}
		}

		protected override void OnWindowCreated(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || guardMenuItem != null)
				return;

			hostMenu = controlCenter.FindFirst("ControlCenterMenuItemTools") as NTMenuItem
				?? controlCenter.FindFirst("toolsMenuItem") as NTMenuItem
				?? controlCenter.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
			if (hostMenu == null) {
				OrcaDisciplineDiagnostics.Write("Control Center menu host was not found; Orca Discipline Guard menu was not injected.");
				return;
			}
			OrcaDisciplineDiagnostics.Write("Orca Discipline Guard menu host found: " + (hostMenu.Name ?? string.Empty) + " / " + (hostMenu.Header == null ? string.Empty : hostMenu.Header.ToString()));

			guardMenuItem = new NTMenuItem {
				Header = "Orca Discipline Guard",
				Style = Application.Current == null ? null : Application.Current.TryFindResource("MainMenuItem") as Style
			};
			guardMenuItem.Click += OnMenuItemClick;
			hostMenu.Items.Add(guardMenuItem);
			OrcaDisciplineDiagnostics.Write("Orca Discipline Guard menu injected.");
		}

		protected override void OnWindowDestroyed(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || guardMenuItem == null || hostMenu == null)
				return;

			guardMenuItem.Click -= OnMenuItemClick;
			hostMenu.Items.Remove(guardMenuItem);
			guardMenuItem = null;
			hostMenu = null;
		}

		private void OnMenuItemClick(object sender, RoutedEventArgs e)
		{
			try {
				OrcaDisciplineDiagnostics.Write("Orca Discipline Guard menu item clicked.");
				Dispatcher dispatcher = Application.Current == null ? Dispatcher.CurrentDispatcher : Application.Current.Dispatcher;
				dispatcher.InvokeAsync(() => OrcaDisciplineGuardWindow.ShowOrActivate());
			} catch (Exception ex) {
				string message = "Orca Discipline Guard click handler failed: " + ex.Message;
				OrcaDisciplineDiagnostics.Write(message + Environment.NewLine + ex);
				MessageBox.Show(message, "Orca Discipline Guard", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	public sealed class OrcaDisciplineGuardWindow : NTWindow
	{
		private static OrcaDisciplineGuardWindow instance;
		private readonly OrcaDisciplineGuardViewModel viewModel;

		private OrcaDisciplineGuardWindow()
		{
			Caption = "Orca Discipline Guard";
			Title = "Orca Discipline Guard";
			Width = 1180;
			Height = 760;
			MinWidth = 960;
			MinHeight = 620;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Background = Brush("#FF0F141B");
			Foreground = Brush("#FFEAF0F6");

			viewModel = new OrcaDisciplineGuardViewModel(Dispatcher);
			DataContext = viewModel;

			Grid root = new Grid { Background = Brush("#FF0F141B") };
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
			try {
				OrcaDisciplineDiagnostics.Write("ShowOrActivate requested.");
				if (instance == null)
					instance = new OrcaDisciplineGuardWindow();

				if (!instance.IsVisible)
					instance.Show();
				instance.Activate();
				OrcaDisciplineDiagnostics.Write("Orca Discipline Guard window opened.");
			} catch (Exception ex) {
				instance = null;
				string message = "Orca Discipline Guard could not open: " + ex.Message;
				OrcaDisciplineDiagnostics.Write(message + Environment.NewLine + ex);
				MessageBox.Show(message, "Orca Discipline Guard", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private FrameworkElement BuildHeader()
		{
			Grid header = new Grid { Margin = new Thickness(14, 14, 14, 10) };
			header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			StackPanel titleStack = new StackPanel { Orientation = Orientation.Vertical };
			titleStack.Children.Add(new TextBlock {
				Text = "Orca Discipline Guard",
				FontSize = 24,
				FontWeight = FontWeights.SemiBold,
				Foreground = Brush("#FFF5F8FB")
			});
			titleStack.Children.Add(new TextBlock {
				Text = "Account-specific rule tracking, session discipline grade, and violation journal",
				FontSize = 12,
				Foreground = Brush("#FF8EA0B5"),
				Margin = new Thickness(1, 3, 0, 0)
			});
			Grid.SetColumn(titleStack, 0);
			header.Children.Add(titleStack);

			TextBlock status = new TextBlock {
				MinWidth = 220,
				TextAlignment = TextAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = Brush("#FF6EE7A8"),
				FontSize = 13,
				FontWeight = FontWeights.SemiBold
			};
			status.SetBinding(TextBlock.TextProperty, new Binding("HeaderStatus"));
			Grid.SetColumn(status, 1);
			header.Children.Add(status);

			Border alert = new Border {
				Margin = new Thickness(0, 12, 0, 0),
				Padding = new Thickness(10, 7, 10, 7),
				CornerRadius = new CornerRadius(5),
				Background = Brush("#FF5A1721"),
				BorderBrush = Brush("#FFE23A52"),
				BorderThickness = new Thickness(1)
			};
			alert.SetBinding(UIElement.VisibilityProperty, new Binding("AlertText") { Converter = new OrcaDisciplineStringVisibilityConverter() });
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
				Header = "Session",
				Foreground = Brush("#FFEAF0F6"),
				Background = Brush("#FF141D27"),
				Content = BuildSessionTab()
			});
			tabs.Items.Add(new TabItem {
				Header = "Summary",
				Foreground = Brush("#FFEAF0F6"),
				Background = Brush("#FF141D27"),
				Content = BuildSummaryTab()
			});
			return tabs;
		}

		private FrameworkElement BuildSessionTab()
		{
			Grid tab = new Grid { Background = Brush("#FF0F141B") };
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			tab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(210) });

			FrameworkElement controls = BuildControls();
			Grid.SetRow(controls, 0);
			tab.Children.Add(controls);

			FrameworkElement dashboard = BuildDashboard();
			Grid.SetRow(dashboard, 1);
			tab.Children.Add(dashboard);

			Border rulesShell = new Border {
				Margin = new Thickness(0, 0, 0, 12),
				BorderThickness = new Thickness(1),
				BorderBrush = Brush("#FF2A3747"),
				Background = Brush("#FF121A23"),
				CornerRadius = new CornerRadius(6),
				Child = BuildRulesGrid()
			};
			Grid.SetRow(rulesShell, 2);
			tab.Children.Add(rulesShell);

			Border violationsShell = new Border {
				BorderThickness = new Thickness(1),
				BorderBrush = Brush("#FF2A3747"),
				Background = Brush("#FF121A23"),
				CornerRadius = new CornerRadius(6),
				Child = BuildViolationsGrid()
			};
			Grid.SetRow(violationsShell, 3);
			tab.Children.Add(violationsShell);
			return tab;
		}

		private FrameworkElement BuildControls()
		{
			Grid controls = new Grid { Margin = new Thickness(0, 0, 0, 12) };
			controls.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			controls.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			for (int i = 0; i < 3; i++)
				controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			FrameworkElement account = BuildLabeledCombo("Account", "AccountNames", "SelectedAccountName", 190);
			Grid.SetColumn(account, 0);
			controls.Children.Add(account);

			FrameworkElement instrument = BuildLabeledCombo("Instrument", "InstrumentOptions", "SelectedInstrumentFilter", 190);
			Grid.SetColumn(instrument, 1);
			instrument.Margin = new Thickness(10, 0, 0, 0);
			controls.Children.Add(instrument);

			FrameworkElement template = BuildLabeledCombo("Template", "TemplateNames", "SelectedTemplateName", 220);
			Grid.SetColumn(template, 2);
			template.Margin = new Thickness(10, 0, 10, 0);
			controls.Children.Add(template);

			StackPanel buttons = new StackPanel {
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0, 8, 0, 0)
			};
			AddToolbarButton(buttons, "Refresh", "RefreshAccountsCommand", "#FF2B3340");
			AddToolbarButton(buttons, "Start Session", "StartCommand", "#FF146C43");
			AddToolbarButton(buttons, "Pause / Resume", "PauseCommand", "#FF274B73");
			AddToolbarButton(buttons, "End Session", "EndCommand", "#FF5A1721");
			AddToolbarButton(buttons, "Reset", "ResetCommand", "#FF3B4655");
			AddToolbarButton(buttons, "Add Rule", "AddRuleCommand", "#FF274B73");
			AddToolbarButton(buttons, "Delete Rule", "DeleteRuleCommand", "#FF5A1721");
			AddToolbarButton(buttons, "Save Template", "SaveTemplateCommand", "#FF2B3340");
			AddToolbarButton(buttons, "Clone Template", "CloneTemplateCommand", "#FF274B73");
			Grid.SetRow(buttons, 1);
			Grid.SetColumnSpan(buttons, 3);
			controls.Children.Add(buttons);

			return controls;
		}

		private FrameworkElement BuildLabeledCombo(string label, string itemsPath, string selectedPath, double minWidth)
		{
			StackPanel stack = new StackPanel { Orientation = Orientation.Vertical };
			stack.Children.Add(new TextBlock {
				Text = label,
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 11,
				Margin = new Thickness(0, 0, 0, 3)
			});
			ComboBox combo = new ComboBox {
				MinWidth = minWidth,
				Height = 28,
				Foreground = Brush("#FFEAF0F6"),
				Background = Brush("#FF18212C"),
				BorderBrush = Brush("#FF334255"),
				BorderThickness = new Thickness(1),
				IsEditable = false
			};
			combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemsPath));
			combo.SetBinding(Selector.SelectedItemProperty, new Binding(selectedPath) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
			stack.Children.Add(combo);
			return stack;
		}

		private FrameworkElement BuildDashboard()
		{
			UniformGrid grid = new UniformGrid {
				Columns = 8,
				Margin = new Thickness(0, 0, 0, 12)
			};
			grid.Children.Add(MetricCard("Grade", "Grade", 26));
			grid.Children.Add(MetricCard("Score", "ScoreText", 22));
			grid.Children.Add(MetricCard("Session P&L", "SessionPnlText", 20));
			grid.Children.Add(MetricCard("Trades", "TradeCountText", 20));
			grid.Children.Add(MetricCard("Violations", "ViolationCountText", 20));
			grid.Children.Add(MetricCard("Cooldown", "CooldownText", 20));
			grid.Children.Add(MetricCard("Position", "CurrentPositionSizeText", 20));
			grid.Children.Add(MetricCard("Loss Streak", "ConsecutiveLossesText", 20));
			return grid;
		}

		private FrameworkElement MetricCard(string label, string valuePath, double valueSize)
		{
			Border card = new Border {
				Margin = new Thickness(0, 0, 8, 0),
				Padding = new Thickness(10, 8, 10, 8),
				CornerRadius = new CornerRadius(6),
				BorderBrush = Brush("#FF2A3747"),
				BorderThickness = new Thickness(1),
				Background = Brush("#FF121A23")
			};
			StackPanel stack = new StackPanel { Orientation = Orientation.Vertical };
			stack.Children.Add(new TextBlock {
				Text = label,
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 11
			});
			TextBlock value = new TextBlock {
				Foreground = Brush("#FFF5F8FB"),
				FontSize = valueSize,
				FontWeight = FontWeights.SemiBold,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			value.SetBinding(TextBlock.TextProperty, new Binding(valuePath));
			stack.Children.Add(value);
			card.Child = stack;
			return card;
		}

		private DataGrid BuildRulesGrid()
		{
			DataGrid grid = BuildBaseGrid();
			grid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Rules"));
			grid.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedRule") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
			grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Enabled", Binding = new Binding("Enabled") { Mode = BindingMode.TwoWay }, Width = new DataGridLength(70) });
			grid.Columns.Add(TextColumn("Rule Name", "Name", 190));
			grid.Columns.Add(TextColumn("Mode", "Mode", 90));
			grid.Columns.Add(TextColumn("Status", "Status", 100));
			grid.Columns.Add(EditableTextColumn("Parameter / Limit", "ParameterText", 170));
			grid.Columns.Add(TextColumn("Current Value", "CurrentValueText", 140));
			grid.Columns.Add(TextColumn("Violations", "ViolationCount", 80));
			grid.Columns.Add(TextColumn("Last Violation", "LastViolationMessage", 230));
			grid.Columns.Add(BuildManualActionColumn());
			grid.Columns.Add(new DataGridTextColumn {
				Header = "Notes",
				Binding = new Binding("Notes") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
				Width = new DataGridLength(220),
				ElementStyle = BuildCellTextStyle(),
				EditingElementStyle = BuildTextBoxStyle()
			});
			return grid;
		}

		private DataGrid BuildViolationsGrid()
		{
			DataGrid grid = BuildBaseGrid();
			grid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Violations"));
			grid.Columns.Add(TextColumn("Time", "DisplayTime", 90));
			grid.Columns.Add(TextColumn("Rule", "RuleName", 180));
			grid.Columns.Add(TextColumn("Severity", "Severity", 90));
			grid.Columns.Add(TextColumn("Instrument", "Instrument", 120));
			grid.Columns.Add(TextColumn("Message", "Message", 360));
			grid.Columns.Add(TextColumn("Observed", "ValueObserved", 110));
			grid.Columns.Add(TextColumn("Limit", "LimitValue", 110));
			return grid;
		}

		private DataGrid BuildBaseGrid()
		{
			DataGrid grid = new DataGrid {
				AutoGenerateColumns = false,
				CanUserAddRows = false,
				CanUserDeleteRows = false,
				GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
				HeadersVisibility = DataGridHeadersVisibility.Column,
				RowHeaderWidth = 0,
				Background = Brush("#FF121A23"),
				Foreground = Brush("#FFEAF0F6"),
				BorderBrush = Brush("#FF2A3747"),
				HorizontalGridLinesBrush = Brush("#FF202A36"),
				VerticalGridLinesBrush = Brush("#FF202A36"),
				AlternatingRowBackground = Brush("#FF101820"),
				RowBackground = Brush("#FF121A23"),
				ColumnHeaderStyle = BuildHeaderStyle()
			};
			return grid;
		}

		private DataGridTemplateColumn BuildManualActionColumn()
		{
			FrameworkElementFactory combo = new FrameworkElementFactory(typeof(ComboBox));
			combo.SetValue(FrameworkElement.MinWidthProperty, 112.0);
			combo.SetValue(FrameworkElement.HeightProperty, 25.0);
			combo.SetValue(Control.ForegroundProperty, Brush("#FFEAF0F6"));
			combo.SetValue(Control.BackgroundProperty, Brush("#FF18212C"));
			combo.SetValue(ComboBox.ItemsSourceProperty, OrcaManualActionValues.Items);
			combo.SetBinding(Selector.SelectedItemProperty, new Binding("ManualAction") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
			combo.SetBinding(UIElement.IsEnabledProperty, new Binding("IsManual"));
			return new DataGridTemplateColumn {
				Header = "Manual Action",
				Width = new DataGridLength(132),
				CellTemplate = new DataTemplate { VisualTree = combo }
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

		private DataGridTextColumn EditableTextColumn(string header, string path, double width)
		{
			return new DataGridTextColumn {
				Header = header,
				Binding = new Binding(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus },
				Width = new DataGridLength(width),
				ElementStyle = BuildCellTextStyle(),
				EditingElementStyle = BuildTextBoxStyle()
			};
		}

		private Style BuildHeaderStyle()
		{
			Style style = new Style(typeof(DataGridColumnHeader));
			style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#FF18212C")));
			style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#FFB8C6D8")));
			style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
			style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
			style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 6, 8, 6)));
			style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#FF2A3747")));
			style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
			return style;
		}

		private Style BuildCellTextStyle()
		{
			Style style = new Style(typeof(TextBlock));
			style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brush("#FFEAF0F6")));
			style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 12.0));
			style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 4, 8, 4)));
			style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
			return style;
		}

		private Style BuildTextBoxStyle()
		{
			Style style = new Style(typeof(TextBox));
			style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#FFEAF0F6")));
			style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#FF18212C")));
			style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#FF334255")));
			return style;
		}

		private FrameworkElement BuildSummaryTab()
		{
			Grid tab = new Grid { Background = Brush("#FF0F141B") };
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			StackPanel commands = new StackPanel {
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0, 0, 0, 12)
			};
			commands.Children.Add(ControlButton("Copy Summary", "CopySummaryCommand", "#FF274B73"));
			commands.Children.Add(ControlButton("Export Session JSON", "ExportSessionCommand", "#FF146C43"));
			commands.Children.Add(ControlButton("Export Violations CSV", "ExportViolationsCommand", "#FF3B4655"));
			Grid.SetRow(commands, 0);
			tab.Children.Add(commands);

			TextBox summary = new TextBox {
				AcceptsReturn = true,
				IsReadOnly = true,
				TextWrapping = TextWrapping.Wrap,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				Foreground = Brush("#FFEAF0F6"),
				Background = Brush("#FF121A23"),
				BorderBrush = Brush("#FF2A3747"),
				BorderThickness = new Thickness(1),
				Padding = new Thickness(12),
				FontFamily = new FontFamily("Consolas"),
				FontSize = 13
			};
			summary.SetBinding(TextBox.TextProperty, new Binding("SessionSummary") { Mode = BindingMode.OneWay });
			Grid.SetRow(summary, 1);
			tab.Children.Add(summary);
			return tab;
		}

		private void AddToolbarButton(Panel panel, string label, string commandPath, string color)
		{
			Button button = ControlButton(label, commandPath, color);
			button.Margin = new Thickness(6, 0, 0, 0);
			panel.Children.Add(button);
		}

		private Button ControlButton(string label, string commandPath, string color)
		{
			Button button = new Button {
				Content = label,
				MinWidth = 92,
				Height = 29,
				Margin = new Thickness(6, 18, 0, 0),
				Padding = new Thickness(10, 4, 10, 4),
				Foreground = Brush("#FFEAF0F6"),
				Background = Brush(color),
				BorderBrush = Brush("#FF5D6978"),
				BorderThickness = new Thickness(1)
			};
			button.SetBinding(Button.CommandProperty, new Binding(commandPath));
			return button;
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

	public sealed class OrcaDisciplineRuleEditorDialog : Window
	{
		private readonly ComboBox typeCombo;
		private readonly TextBox nameBox;
		private readonly TextBox descriptionBox;
		private readonly TextBox parametersBox;
		private readonly ComboBox severityCombo;
		private readonly CheckBox enabledBox;

		private OrcaDisciplineRuleEditorDialog()
		{
			Title = "Add Discipline Rule";
			Width = 560;
			Height = 440;
			MinWidth = 520;
			MinHeight = 420;
			ResizeMode = ResizeMode.NoResize;
			Background = new SolidColorBrush(Color.FromRgb(15, 20, 27));
			Foreground = new SolidColorBrush(Color.FromRgb(234, 240, 246));

			typeCombo = new ComboBox { MinHeight = 26, Margin = new Thickness(0, 3, 0, 10) };
			nameBox = new TextBox { MinHeight = 26, Margin = new Thickness(0, 3, 0, 10) };
			descriptionBox = new TextBox { MinHeight = 64, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 10) };
			parametersBox = new TextBox { MinHeight = 26, Margin = new Thickness(0, 3, 0, 10) };
			severityCombo = new ComboBox { MinHeight = 26, Margin = new Thickness(0, 3, 0, 10) };
			enabledBox = new CheckBox { Content = "Enabled", IsChecked = true, Margin = new Thickness(0, 3, 0, 10), Foreground = Foreground };

			foreach (OrcaDisciplineRuleTypeChoice choice in OrcaDisciplineRuleTypeChoice.CreateDefaults())
				typeCombo.Items.Add(choice);
			foreach (object severity in Enum.GetValues(typeof(OrcaDisciplineSeverity)))
				severityCombo.Items.Add(severity);
			typeCombo.SelectionChanged += OnTypeSelectionChanged;

			Grid root = new Grid { Margin = new Thickness(16) };
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			AddLabeledControl(root, 0, "Rule type", typeCombo);
			AddLabeledControl(root, 1, "Rule name", nameBox);
			AddLabeledControl(root, 2, "Description", descriptionBox);
			AddLabeledControl(root, 3, "Parameters", parametersBox);
			AddLabeledControl(root, 4, "Severity", severityCombo);
			Grid.SetRow(enabledBox, 5);
			root.Children.Add(enabledBox);

			StackPanel buttons = new StackPanel {
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom
			};
			Button cancel = new Button { Content = "Cancel", MinWidth = 86, Height = 28, Margin = new Thickness(6, 0, 0, 0), IsCancel = true };
			Button add = new Button { Content = "Add Rule", MinWidth = 92, Height = 28, Margin = new Thickness(6, 0, 0, 0), IsDefault = true };
			add.Click += OnAddClicked;
			buttons.Children.Add(cancel);
			buttons.Children.Add(add);
			Grid.SetRow(buttons, 6);
			root.Children.Add(buttons);

			Content = root;
			if (typeCombo.Items.Count > 0)
				typeCombo.SelectedIndex = 0;
		}

		public OrcaDisciplineRuleConfig ResultConfig { get; private set; }

		public static OrcaDisciplineRuleConfig ShowDialogForRule(Window owner)
		{
			OrcaDisciplineRuleEditorDialog dialog = new OrcaDisciplineRuleEditorDialog();
			if (owner != null) {
				dialog.Owner = owner;
				dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			} else {
				dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			}
			bool? result = dialog.ShowDialog();
			return result == true ? dialog.ResultConfig : null;
		}

		private static void AddLabeledControl(Grid root, int row, string label, Control control)
		{
			StackPanel stack = new StackPanel { Orientation = Orientation.Vertical };
			stack.Children.Add(new TextBlock {
				Text = label,
				Foreground = new SolidColorBrush(Color.FromRgb(142, 160, 181)),
				FontSize = 11
			});
			stack.Children.Add(control);
			Grid.SetRow(stack, row);
			root.Children.Add(stack);
		}

		private void OnTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			OrcaDisciplineRuleTypeChoice choice = typeCombo.SelectedItem as OrcaDisciplineRuleTypeChoice;
			if (choice == null)
				return;
			nameBox.Text = choice.DefaultName;
			descriptionBox.Text = choice.Description;
			parametersBox.Text = FormatParameters(choice.Parameters);
			severityCombo.SelectedItem = choice.DefaultSeverity;
		}

		private void OnAddClicked(object sender, RoutedEventArgs e)
		{
			OrcaDisciplineRuleTypeChoice choice = typeCombo.SelectedItem as OrcaDisciplineRuleTypeChoice;
			if (choice == null)
				return;
			if (string.IsNullOrWhiteSpace(nameBox.Text)) {
				MessageBox.Show(this, "Give the rule a name first.", "Orca Discipline Guard", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Dictionary<string, string> parameters = ParseParameters(parametersBox.Text);
			if (parameters == null)
				return;
			object selectedSeverity = severityCombo.SelectedItem;
			OrcaDisciplineSeverity severity = selectedSeverity is OrcaDisciplineSeverity ? (OrcaDisciplineSeverity)selectedSeverity : choice.DefaultSeverity;
			ResultConfig = new OrcaDisciplineRuleConfig {
				Id = "custom-" + choice.Type.ToLowerInvariant() + "-" + Guid.NewGuid().ToString("N").Substring(0, 8),
				Type = choice.Type,
				Name = nameBox.Text.Trim(),
				Description = string.IsNullOrWhiteSpace(descriptionBox.Text) ? nameBox.Text.Trim() : descriptionBox.Text.Trim(),
				Enabled = enabledBox.IsChecked != false,
				Mode = choice.Mode,
				Severity = severity,
				Parameters = parameters
			};
			DialogResult = true;
			Close();
		}

		private Dictionary<string, string> ParseParameters(string text)
		{
			Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(text))
				return parameters;
			string[] parts = text.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string rawPart in parts) {
				string part = rawPart == null ? string.Empty : rawPart.Trim();
				int equalsIndex = part.IndexOf('=');
				if (equalsIndex <= 0 || equalsIndex >= part.Length - 1) {
					MessageBox.Show(this, "Use Key=Value pairs for parameters, separated by semicolons.", "Orca Discipline Guard", MessageBoxButton.OK, MessageBoxImage.Warning);
					return null;
				}
				string key = part.Substring(0, equalsIndex).Trim();
				string value = part.Substring(equalsIndex + 1).Trim();
				if (!string.IsNullOrWhiteSpace(key))
					parameters[key] = value;
			}
			return parameters;
		}

		private static string FormatParameters(Dictionary<string, string> parameters)
		{
			if (parameters == null || parameters.Count == 0)
				return string.Empty;
			return string.Join("; ", parameters.OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value).ToArray());
		}
	}

	public sealed class OrcaDisciplineRuleTypeChoice
	{
		public string Type { get; set; }
		public string DisplayName { get; set; }
		public string DefaultName { get; set; }
		public string Description { get; set; }
		public OrcaDisciplineRuleMode Mode { get; set; }
		public OrcaDisciplineSeverity DefaultSeverity { get; set; }
		public Dictionary<string, string> Parameters { get; set; }

		public override string ToString()
		{
			return DisplayName;
		}

		public static IEnumerable<OrcaDisciplineRuleTypeChoice> CreateDefaults()
		{
			yield return Choice("TradeCooldown", "Automated - Trade cooldown", "Minimum 5 minutes between new trades", "Minimum time between fresh flat-to-position trades.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MinimumMinutes", "5"));
			yield return Choice("MaxPositionSize", "Automated - Max position size", "Max position size: 2 minis / 20 micros", "Flags any instrument whose account position exceeds the mini-equivalent contract limit.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxContracts", "2", "MicroMultiplier", OrcaDisciplineConstants.DefaultMicroMultiplier, "MicroSymbols", OrcaDisciplineConstants.DefaultMicroSymbols));
			yield return Choice("MaxLossPerTrade", "Automated - Max loss per trade", "Max loss per trade: $300", "Uses gross round-trip realized P&L after the trade closes.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxLoss", "300"));
			yield return Choice("MaxSessionLoss", "Automated - Max session loss", "Max session loss: $600", "Uses selected account realized P&L from session start.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxLoss", "600"));
			yield return Choice("MaxTradesPerSession", "Automated - Max trades per session", "Max trades per session: 5", "Counts completed round trips.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxTrades", "5"));
			yield return Choice("MaxConsecutiveLosses", "Automated - Max consecutive losses", "Max consecutive losses: 2", "Flags losing streaks after completed round trips.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxLosses", "2"));
			yield return Choice("AllowedTradingWindow", "Automated - Allowed trading window", "Allowed trading window: 09:30 to 11:30", "Flags fresh trades outside the configured local time window.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Warning, Dict("Start", "09:30", "End", "11:30"));
			yield return Choice("MaxRuleViolations", "Automated - Max rule violations", "Max rule violations: 3", "Flags when the session breaks too many rules.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxViolations", "3"));
			yield return Choice("NoAddToLosingTrade", "Automated - No add to loser", "No adding to losing trades", "Flags scale-ins when the open trade is currently losing.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict());
			yield return Choice("NoImmediateLossReversal", "Automated - No immediate loss reversal", "No immediate reversal after loss", "Flags opposite-direction trades started too soon after a losing trade.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MinimumMinutes", "5"));
			yield return Choice("ManualChecklist", "Manual - Checklist item", "New manual checklist rule", "Manual rule that you mark Followed, Broken, or N/A during the session.", OrcaDisciplineRuleMode.Manual, OrcaDisciplineSeverity.Warning, Dict());
		}

		private static OrcaDisciplineRuleTypeChoice Choice(string type, string displayName, string defaultName, string description, OrcaDisciplineRuleMode mode, OrcaDisciplineSeverity severity, Dictionary<string, string> parameters)
		{
			return new OrcaDisciplineRuleTypeChoice {
				Type = type,
				DisplayName = displayName,
				DefaultName = defaultName,
				Description = description,
				Mode = mode,
				DefaultSeverity = severity,
				Parameters = parameters
			};
		}

		private static Dictionary<string, string> Dict(params string[] values)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i + 1 < values.Length; i += 2)
				dictionary[values[i]] = values[i + 1];
			return dictionary;
		}
	}

	public sealed class OrcaDisciplineGuardViewModel : OrcaDisciplineNotifyBase, IDisposable
	{
		private readonly Dispatcher dispatcher;
		private readonly OrcaDisciplineGuardEngine engine;
		private readonly OrcaDisciplineSettings settings;
		private string selectedAccountName;
		private string selectedTemplateName;
		private string selectedInstrumentFilter;
		private string alertText;
		private OrcaDisciplineRule selectedRule;

		public OrcaDisciplineGuardViewModel(Dispatcher dispatcher)
		{
			this.dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
			Templates = new ObservableCollection<OrcaDisciplineRuleTemplate>(OrcaDisciplineStore.LoadTemplates());
			settings = OrcaDisciplineStore.LoadSettings();
			AccountNames = new ObservableCollection<string>();
			TemplateNames = new ObservableCollection<string>();
			InstrumentOptions = new ObservableCollection<string>();
			foreach (OrcaDisciplineRuleTemplate template in Templates)
				TemplateNames.Add(template.Name);
			InstrumentOptions.Add(OrcaDisciplineConstants.AllInstruments);
			selectedInstrumentFilter = string.IsNullOrWhiteSpace(settings.LastInstrumentFilter) ? OrcaDisciplineConstants.AllInstruments : settings.LastInstrumentFilter;
			if (!InstrumentOptions.Contains(selectedInstrumentFilter))
				InstrumentOptions.Add(selectedInstrumentFilter);

			engine = new OrcaDisciplineGuardEngine(this.dispatcher);
			engine.SessionChanged += OnEngineSessionChanged;
			engine.AlertRaised += OnEngineAlertRaised;

			RefreshAccountsCommand = new OrcaDisciplineCommand(RefreshAccounts);
			StartCommand = new OrcaDisciplineCommand(StartSession, CanStartSession);
			PauseCommand = new OrcaDisciplineCommand(PauseSession, CanPauseSession);
			EndCommand = new OrcaDisciplineCommand(EndSession, CanEndSession);
			ResetCommand = new OrcaDisciplineCommand(ResetSession);
			CopySummaryCommand = new OrcaDisciplineCommand(CopySummary);
			ExportSessionCommand = new OrcaDisciplineCommand(ExportSession);
			ExportViolationsCommand = new OrcaDisciplineCommand(ExportViolations);
			SaveTemplateCommand = new OrcaDisciplineCommand(SaveCurrentTemplate, CanSaveCurrentTemplate);
			CloneTemplateCommand = new OrcaDisciplineCommand(CloneCurrentTemplate, CanSaveCurrentTemplate);
			AddRuleCommand = new OrcaDisciplineCommand(AddRule, CanEditRules);
			DeleteRuleCommand = new OrcaDisciplineCommand(DeleteSelectedRule, CanDeleteSelectedRule);

			RefreshAccounts();
			SelectedTemplateName = ResolveInitialTemplate(settings.LastTemplateName);
			if (!string.IsNullOrWhiteSpace(settings.LastAccountName) && AccountNames.Contains(settings.LastAccountName))
				SelectedAccountName = settings.LastAccountName;
			else
				SelectedAccountName = AccountNames.FirstOrDefault();
			if (string.IsNullOrWhiteSpace(SelectedTemplateName))
				SelectedTemplateName = TemplateNames.FirstOrDefault();
			RebuildSessionForSelection();
		}

		public ObservableCollection<string> AccountNames { get; private set; }
		public ObservableCollection<string> TemplateNames { get; private set; }
		public ObservableCollection<string> InstrumentOptions { get; private set; }
		public ObservableCollection<OrcaDisciplineRuleTemplate> Templates { get; private set; }

		public ObservableCollection<OrcaDisciplineRule> Rules
		{
			get { return engine.Session == null ? null : engine.Session.Rules; }
		}

		public ObservableCollection<OrcaDisciplineViolation> Violations
		{
			get { return engine.Session == null ? null : engine.Session.Violations; }
		}

		public ICommand RefreshAccountsCommand { get; private set; }
		public ICommand StartCommand { get; private set; }
		public ICommand PauseCommand { get; private set; }
		public ICommand EndCommand { get; private set; }
		public ICommand ResetCommand { get; private set; }
		public ICommand CopySummaryCommand { get; private set; }
		public ICommand ExportSessionCommand { get; private set; }
		public ICommand ExportViolationsCommand { get; private set; }
		public ICommand SaveTemplateCommand { get; private set; }
		public ICommand CloneTemplateCommand { get; private set; }
		public ICommand AddRuleCommand { get; private set; }
		public ICommand DeleteRuleCommand { get; private set; }

		public string SelectedAccountName
		{
			get { return selectedAccountName; }
			set {
				if (!Set(ref selectedAccountName, value, "SelectedAccountName"))
					return;
				ArchiveActiveSessionBeforeSelectionChange("account change");
				settings.LastAccountName = value;
				SaveSettings();
				RebuildSessionForSelection();
			}
		}

		public string SelectedTemplateName
		{
			get { return selectedTemplateName; }
			set {
				if (!Set(ref selectedTemplateName, value, "SelectedTemplateName"))
					return;
				ArchiveActiveSessionBeforeSelectionChange("template change");
				settings.LastTemplateName = value;
				SaveSettings();
				RebuildSessionForSelection();
			}
		}

		public string SelectedInstrumentFilter
		{
			get { return selectedInstrumentFilter; }
			set {
				if (string.IsNullOrWhiteSpace(value))
					value = OrcaDisciplineConstants.AllInstruments;
				if (!Set(ref selectedInstrumentFilter, value, "SelectedInstrumentFilter"))
					return;
				settings.LastInstrumentFilter = value;
				SaveSettings();
				if (engine.Session != null)
					engine.Session.InstrumentFilter = value;
				RaiseDashboard();
			}
		}

		public string HeaderStatus
		{
			get {
				if (engine.Session == null)
					return "No session";
				string account = string.IsNullOrWhiteSpace(engine.Session.AccountName) ? "No account" : engine.Session.AccountName;
				return account + " - " + engine.Session.StatusText;
			}
		}

		public string AlertText
		{
			get { return alertText; }
			set { Set(ref alertText, value, "AlertText"); }
		}

		public OrcaDisciplineRule SelectedRule
		{
			get { return selectedRule; }
			set {
				if (!Set(ref selectedRule, value, "SelectedRule"))
					return;
				OrcaDisciplineCommand.RaiseCanExecuteChanged(DeleteRuleCommand);
			}
		}

		public string Grade { get { return engine.Session == null ? "-" : engine.Session.Grade; } }
		public string ScoreText { get { return engine.Session == null ? "0" : engine.Session.Score.ToString("0", CultureInfo.InvariantCulture); } }
		public string SessionPnlText { get { return engine.Session == null ? "$0" : engine.Session.SessionRealizedPnl.ToString("C0", CultureInfo.CurrentCulture); } }
		public string TradeCountText { get { return engine.Session == null ? "0" : engine.Session.CompletedTradeCount.ToString(CultureInfo.InvariantCulture); } }
		public string ViolationCountText { get { return engine.Session == null ? "0" : engine.Session.TotalViolations.ToString(CultureInfo.InvariantCulture); } }
		public string CooldownText { get { return engine.Session == null ? "Ready" : engine.Session.CooldownText; } }
		public string CurrentPositionSizeText { get { return engine.Session == null ? "0" : engine.Session.CurrentPositionSizeText; } }
		public string ConsecutiveLossesText { get { return engine.Session == null ? "0" : engine.Session.ConsecutiveLosses.ToString(CultureInfo.InvariantCulture); } }
		public string SessionSummary { get { return engine.Session == null ? string.Empty : engine.Session.BuildSummary(); } }

		public void Dispose()
		{
			SaveSettings();
			engine.SessionChanged -= OnEngineSessionChanged;
			engine.AlertRaised -= OnEngineAlertRaised;
			engine.Dispose();
		}

		private void RefreshAccounts()
		{
			try {
				string previous = SelectedAccountName;
				AccountNames.Clear();
				foreach (Account account in Account.All.Where(IsCurrentTradingAccount).OrderBy(a => a.Name))
					AccountNames.Add(account.Name);
				if (!string.IsNullOrWhiteSpace(previous) && AccountNames.Contains(previous))
					selectedAccountName = previous;
				else
					selectedAccountName = AccountNames.FirstOrDefault();
				Raise("SelectedAccountName");
				RebuildSessionForSelection();
			} catch (Exception ex) {
				AlertText = "Account refresh failed: " + ex.Message;
			}
		}

		private void StartSession()
		{
			try {
				AlertText = string.Empty;
				engine.StartSession();
				RaiseDashboard();
				RaiseRuleCommandStates();
			} catch (Exception ex) {
				AlertText = "Orca Discipline Guard could not start: " + ex.Message;
			}
		}

		private bool CanStartSession()
		{
			return engine.Session != null
				&& !string.IsNullOrWhiteSpace(SelectedAccountName)
				&& engine.Session.Status != OrcaDisciplineSessionStatus.Active;
		}

		private void PauseSession()
		{
			engine.TogglePause();
			RaiseDashboard();
			RaiseRuleCommandStates();
		}

		private bool CanPauseSession()
		{
			return engine.Session != null
				&& (engine.Session.Status == OrcaDisciplineSessionStatus.Active || engine.Session.Status == OrcaDisciplineSessionStatus.Paused);
		}

		private void EndSession()
		{
			engine.EndSession();
			ExportSession();
			RaiseDashboard();
			RaiseRuleCommandStates();
		}

		private bool CanEndSession()
		{
			return engine.Session != null
				&& (engine.Session.Status == OrcaDisciplineSessionStatus.Active || engine.Session.Status == OrcaDisciplineSessionStatus.Paused);
		}

		private void ResetSession()
		{
			AlertText = string.Empty;
			RebuildSessionForSelection();
		}

		private void CopySummary()
		{
			try {
				Clipboard.SetText(SessionSummary ?? string.Empty);
				AlertText = "Session summary copied.";
			} catch (Exception ex) {
				AlertText = "Copy failed: " + ex.Message;
			}
		}

		private void ExportSession()
		{
			try {
				if (engine.Session == null)
					return;
				string path = OrcaDisciplineStore.SaveSessionReport(engine.Session.CreateReport());
				AlertText = "Session JSON saved: " + path;
			} catch (Exception ex) {
				AlertText = "Session export failed: " + ex.Message;
			}
		}

		private void ExportViolations()
		{
			try {
				if (engine.Session == null)
					return;
				string path = OrcaDisciplineStore.SaveViolationsCsv(engine.Session);
				AlertText = "Violations CSV saved: " + path;
			} catch (Exception ex) {
				AlertText = "CSV export failed: " + ex.Message;
			}
		}

		private bool CanEditRules()
		{
			return engine != null
				&& engine.Session != null
				&& Rules != null
				&& engine.Session.Status == OrcaDisciplineSessionStatus.NotStarted;
		}

		private bool CanDeleteSelectedRule()
		{
			return CanEditRules()
				&& SelectedRule != null
				&& Rules != null
				&& Rules.Count > 1;
		}

		private void AddRule()
		{
			try {
				if (!CanEditRules()) {
					AlertText = "Reset the session before editing rules.";
					return;
				}
				OrcaDisciplineRuleConfig config = OrcaDisciplineRuleEditorDialog.ShowDialogForRule(null);
				if (config == null)
					return;
				OrcaDisciplineRule rule = engine.Session.AddConfiguredRule(config);
				SelectedRule = rule;
				Raise("Rules");
				RaiseDashboard();
				RaiseRuleCommandStates();
				AlertText = "Rule added. Use Save Template to keep it.";
			} catch (Exception ex) {
				AlertText = "Rule add failed: " + ex.Message;
			}
		}

		private void DeleteSelectedRule()
		{
			try {
				if (!CanDeleteSelectedRule())
					return;
				string ruleName = SelectedRule.Name;
				MessageBoxResult result = MessageBox.Show("Delete rule '" + ruleName + "' from the current template draft?", "Orca Discipline Guard", MessageBoxButton.YesNo, MessageBoxImage.Warning);
				if (result != MessageBoxResult.Yes)
					return;
				if (engine.Session.RemoveRule(SelectedRule)) {
					SelectedRule = null;
					Raise("Rules");
					RaiseDashboard();
					RaiseRuleCommandStates();
					AlertText = "Rule deleted. Use Save Template to keep it.";
				}
			} catch (Exception ex) {
				AlertText = "Rule delete failed: " + ex.Message;
			}
		}

		private bool CanSaveCurrentTemplate()
		{
			return engine != null && engine.Session != null && Rules != null && Rules.Count > 0;
		}

		private void SaveCurrentTemplate()
		{
			try {
				if (engine.Session == null)
					return;
				string templateName = string.IsNullOrWhiteSpace(SelectedTemplateName) ? engine.Session.TemplateName : SelectedTemplateName;
				OrcaDisciplineRuleTemplate snapshot = engine.Session.CreateTemplateSnapshot(templateName);
				OrcaDisciplineRuleTemplate existing = Templates.FirstOrDefault(t => string.Equals(t.Name, templateName, StringComparison.OrdinalIgnoreCase));
				if (existing == null) {
					Templates.Add(snapshot);
					TemplateNames.Add(snapshot.Name);
					SelectedTemplateName = snapshot.Name;
				} else {
					existing.Rules = snapshot.Rules;
				}
				OrcaDisciplineStore.SaveTemplates(Templates);
				AlertText = "Template saved: " + snapshot.Name;
			} catch (Exception ex) {
				AlertText = "Template save failed: " + ex.Message;
			}
		}

		private void CloneCurrentTemplate()
		{
			try {
				if (engine.Session == null)
					return;
				string baseName = string.IsNullOrWhiteSpace(SelectedTemplateName) ? "Discipline Template" : SelectedTemplateName;
				string cloneName = UniqueTemplateName(baseName + " Copy");
				OrcaDisciplineRuleTemplate snapshot = engine.Session.CreateTemplateSnapshot(cloneName);
				Templates.Add(snapshot);
				TemplateNames.Add(snapshot.Name);
				OrcaDisciplineStore.SaveTemplates(Templates);
				SelectedTemplateName = snapshot.Name;
				AlertText = "Template cloned: " + snapshot.Name;
			} catch (Exception ex) {
				AlertText = "Template clone failed: " + ex.Message;
			}
		}

		private void RebuildSessionForSelection()
		{
			if (engine == null)
				return;
			OrcaDisciplineRuleTemplate template = Templates.FirstOrDefault(t => string.Equals(t.Name, SelectedTemplateName, StringComparison.OrdinalIgnoreCase))
				?? Templates.FirstOrDefault();
			Account account = ResolveAccount(SelectedAccountName);
			engine.SelectAccount(account, template, SelectedInstrumentFilter);
			SelectedRule = null;
			Raise("Rules");
			Raise("Violations");
			RaiseDashboard();
			OrcaDisciplineCommand.RaiseCanExecuteChanged(StartCommand);
			OrcaDisciplineCommand.RaiseCanExecuteChanged(PauseCommand);
			OrcaDisciplineCommand.RaiseCanExecuteChanged(EndCommand);
			RaiseRuleCommandStates();
		}

		private void OnEngineSessionChanged(object sender, EventArgs e)
		{
			if (!dispatcher.CheckAccess()) {
				dispatcher.BeginInvoke(new Action(() => OnEngineSessionChanged(sender, e)));
				return;
			}
			Raise("Rules");
			Raise("Violations");
			RaiseDashboard();
			RefreshInstrumentOptions();
			OrcaDisciplineCommand.RaiseCanExecuteChanged(StartCommand);
			OrcaDisciplineCommand.RaiseCanExecuteChanged(PauseCommand);
			OrcaDisciplineCommand.RaiseCanExecuteChanged(EndCommand);
			RaiseRuleCommandStates();
		}

		private void RaiseRuleCommandStates()
		{
			OrcaDisciplineCommand.RaiseCanExecuteChanged(SaveTemplateCommand);
			OrcaDisciplineCommand.RaiseCanExecuteChanged(CloneTemplateCommand);
			OrcaDisciplineCommand.RaiseCanExecuteChanged(AddRuleCommand);
			OrcaDisciplineCommand.RaiseCanExecuteChanged(DeleteRuleCommand);
		}

		private void OnEngineAlertRaised(object sender, string message)
		{
			AlertText = message;
		}

		private void RefreshInstrumentOptions()
		{
			if (engine.Session == null)
				return;
			foreach (string instrument in engine.Session.ObservedInstrumentNames.OrderBy(s => s)) {
				if (!InstrumentOptions.Contains(instrument))
					InstrumentOptions.Add(instrument);
			}
		}

		private void RaiseDashboard()
		{
			Raise("HeaderStatus");
			Raise("Grade");
			Raise("ScoreText");
			Raise("SessionPnlText");
			Raise("TradeCountText");
			Raise("ViolationCountText");
			Raise("CooldownText");
			Raise("CurrentPositionSizeText");
			Raise("ConsecutiveLossesText");
			Raise("SessionSummary");
		}

		private string ResolveInitialTemplate(string savedTemplateName)
		{
			if (!string.IsNullOrWhiteSpace(savedTemplateName) && TemplateNames.Contains(savedTemplateName))
				return savedTemplateName;
			return TemplateNames.FirstOrDefault();
		}

		private void SaveSettings()
		{
			OrcaDisciplineStore.SaveSettings(settings);
		}

		private void ArchiveActiveSessionBeforeSelectionChange(string reason)
		{
			if (engine == null || engine.Session == null)
				return;
			if (engine.Session.Status != OrcaDisciplineSessionStatus.Active && engine.Session.Status != OrcaDisciplineSessionStatus.Paused)
				return;
			try {
				string path = OrcaDisciplineStore.SaveSessionReport(engine.Session.CreateReport());
				AlertText = "Archived active session before " + reason + ": " + path;
			} catch (Exception ex) {
				AlertText = "Could not archive active session before " + reason + ": " + ex.Message;
			}
		}

		private string UniqueTemplateName(string requestedName)
		{
			if (string.IsNullOrWhiteSpace(requestedName))
				requestedName = "Discipline Template";
			string candidate = requestedName;
			int suffix = 2;
			while (TemplateNames.Contains(candidate)) {
				candidate = requestedName + " " + suffix.ToString(CultureInfo.InvariantCulture);
				suffix++;
			}
			return candidate;
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
	}

	public sealed class OrcaDisciplineGuardEngine : IDisposable
	{
		private readonly Dispatcher dispatcher;
		private readonly DispatcherTimer timer;
		private Account account;
		private bool subscribed;

		public OrcaDisciplineGuardEngine(Dispatcher dispatcher)
		{
			this.dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
			timer = new DispatcherTimer(DispatcherPriority.Background, this.dispatcher);
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Tick += OnTimerTick;
			timer.Start();
		}

		public event EventHandler SessionChanged;
		public event EventHandler<string> AlertRaised;

		public OrcaDisciplineSession Session { get; private set; }

		public void SelectAccount(Account selectedAccount, OrcaDisciplineRuleTemplate template, string instrumentFilter)
		{
			Unsubscribe();
			account = selectedAccount;
			Session = new OrcaDisciplineSession(account == null ? string.Empty : account.Name, template, instrumentFilter);
			Session.PropertyChanged += OnSessionPropertyChanged;
			if (account != null)
				Subscribe();
			RaiseSessionChanged();
		}

		public void StartSession()
		{
			if (account == null)
				throw new InvalidOperationException("Select a connected trading account.");
			if (Session == null)
				throw new InvalidOperationException("Select a rule template.");
			Session.Start(account);
			RaiseSessionChanged();
		}

		public void TogglePause()
		{
			if (Session == null)
				return;
			if (Session.Status == OrcaDisciplineSessionStatus.Active)
				Session.Pause();
			else if (Session.Status == OrcaDisciplineSessionStatus.Paused)
				Session.Resume();
			RaiseSessionChanged();
		}

		public void EndSession()
		{
			if (Session == null)
				return;
			Session.End();
			RaiseSessionChanged();
		}

		public void Dispose()
		{
			timer.Stop();
			timer.Tick -= OnTimerTick;
			Unsubscribe();
			if (Session != null)
				Session.PropertyChanged -= OnSessionPropertyChanged;
		}

		private void Subscribe()
		{
			if (account == null || subscribed)
				return;
			account.OrderUpdate += OnOrderUpdate;
			account.ExecutionUpdate += OnExecutionUpdate;
			account.PositionUpdate += OnPositionUpdate;
			account.AccountItemUpdate += OnAccountItemUpdate;
			subscribed = true;
		}

		private void Unsubscribe()
		{
			if (Session != null)
				Session.PropertyChanged -= OnSessionPropertyChanged;
			if (account == null || !subscribed)
				return;
			try {
				account.OrderUpdate -= OnOrderUpdate;
				account.ExecutionUpdate -= OnExecutionUpdate;
				account.PositionUpdate -= OnPositionUpdate;
				account.AccountItemUpdate -= OnAccountItemUpdate;
			} catch { }
			subscribed = false;
		}

		private void OnOrderUpdate(object sender, OrderEventArgs e)
		{
			RunOnUi(() => {
				if (!IsSelectedAccount(e == null ? null : e.Order == null ? null : e.Order.Account))
					return;
				Session.OnOrderUpdate(e);
				RaiseSessionChanged();
			});
		}

		private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			RunOnUi(() => {
				if (Session == null || e == null || e.Execution == null || !IsSelectedAccount(e.Execution.Account))
					return;
				Session.OnExecution(e);
				RaiseSessionChanged();
			});
		}

		private void OnPositionUpdate(object sender, PositionEventArgs e)
		{
			RunOnUi(() => {
				Account eventAccount = e == null || e.Position == null ? null : e.Position.Account;
				if (Session == null || !IsSelectedAccount(eventAccount))
					return;
				Session.OnPositionUpdate(e);
				RaiseSessionChanged();
			});
		}

		private void OnAccountItemUpdate(object sender, AccountItemEventArgs e)
		{
			RunOnUi(() => {
				if (Session == null || e == null || !IsSelectedAccount(e.Account))
					return;
				Session.OnAccountItemUpdate(e);
				RaiseSessionChanged();
			});
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			if (Session == null)
				return;
			Session.OnTimerTick(account);
			RaiseSessionChanged();
		}

		private void OnSessionPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			RaiseSessionChanged();
		}

		private bool IsSelectedAccount(Account candidate)
		{
			return account != null
				&& candidate != null
				&& string.Equals(candidate.Name, account.Name, StringComparison.OrdinalIgnoreCase);
		}

		private void RunOnUi(Action action)
		{
			if (dispatcher.CheckAccess())
				action();
			else
				dispatcher.BeginInvoke(action);
		}

		private void RaiseSessionChanged()
		{
			EventHandler handler = SessionChanged;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		private void RaiseAlert(string message)
		{
			EventHandler<string> handler = AlertRaised;
			if (handler != null)
				handler(this, message);
		}
	}

	public sealed class OrcaDisciplineSession : OrcaDisciplineNotifyBase
	{
		private readonly OrcaRoundTripTracker tracker = new OrcaRoundTripTracker();
		private readonly Dictionary<string, int> currentPositionsByInstrument = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> observedInstrumentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private readonly OrcaDisciplineRuleTemplate template;
		private DateTime startTime;
		private DateTime endTime;
		private OrcaDisciplineSessionStatus status;
		private double baselineRealizedPnl;
		private double sessionRealizedPnl;
		private int completedTradeCount;
		private int consecutiveLosses;
		private int winningTrades;
		private int losingTrades;
		private double score;
		private string grade;
		private string instrumentFilter;

		public OrcaDisciplineSession(string accountName, OrcaDisciplineRuleTemplate template, string instrumentFilter)
		{
			AccountName = accountName ?? string.Empty;
			this.template = template == null ? OrcaDisciplineRuleTemplate.CreatePropFirmDefault() : template.Clone();
			TemplateName = this.template.Name;
			this.instrumentFilter = string.IsNullOrWhiteSpace(instrumentFilter) ? OrcaDisciplineConstants.AllInstruments : instrumentFilter;
			Rules = new ObservableCollection<OrcaDisciplineRule>();
			Violations = new ObservableCollection<OrcaDisciplineViolation>();
			Status = OrcaDisciplineSessionStatus.NotStarted;
			Score = 100;
			Grade = "A";
			foreach (OrcaDisciplineRuleConfig config in this.template.Rules)
				AddRule(OrcaDisciplineRuleFactory.Create(config));
		}

		public string AccountName { get; private set; }
		public string TemplateName { get; private set; }
		public ObservableCollection<OrcaDisciplineRule> Rules { get; private set; }
		public ObservableCollection<OrcaDisciplineViolation> Violations { get; private set; }

		public IEnumerable<string> ObservedInstrumentNames
		{
			get { return observedInstrumentNames.ToArray(); }
		}

		public string InstrumentFilter
		{
			get { return instrumentFilter; }
			set {
				if (string.IsNullOrWhiteSpace(value))
					value = OrcaDisciplineConstants.AllInstruments;
				if (!Set(ref instrumentFilter, value, "InstrumentFilter"))
					return;
				currentPositionsByInstrument.Clear();
				RefreshRulesCurrentValues();
			}
		}

		public OrcaDisciplineSessionStatus Status
		{
			get { return status; }
			private set {
				if (Set(ref status, value, "Status"))
					Raise("StatusText");
			}
		}

		public string StatusText
		{
			get { return Status.ToString(); }
		}

		public DateTime StartTime
		{
			get { return startTime; }
			private set { Set(ref startTime, value, "StartTime"); }
		}

		public DateTime EndTime
		{
			get { return endTime; }
			private set { Set(ref endTime, value, "EndTime"); }
		}

		public double SessionRealizedPnl
		{
			get { return sessionRealizedPnl; }
			private set { Set(ref sessionRealizedPnl, value, "SessionRealizedPnl"); }
		}

		public int CompletedTradeCount
		{
			get { return completedTradeCount; }
			private set { Set(ref completedTradeCount, value, "CompletedTradeCount"); }
		}

		public int ConsecutiveLosses
		{
			get { return consecutiveLosses; }
			private set { Set(ref consecutiveLosses, value, "ConsecutiveLosses"); }
		}

		public int WinningTrades
		{
			get { return winningTrades; }
			private set { Set(ref winningTrades, value, "WinningTrades"); }
		}

		public int LosingTrades
		{
			get { return losingTrades; }
			private set { Set(ref losingTrades, value, "LosingTrades"); }
		}

		public double Score
		{
			get { return score; }
			private set { Set(ref score, Math.Max(0, Math.Min(100, value)), "Score"); }
		}

		public string Grade
		{
			get { return grade; }
			private set { Set(ref grade, value, "Grade"); }
		}

		public int TotalViolations
		{
			get { return Violations.Count; }
		}

		public int CriticalViolations
		{
			get { return Violations.Count(v => v.Severity == OrcaDisciplineSeverity.Critical); }
		}

		public int TotalRulesFollowed
		{
			get { return Rules.Sum(r => r.FollowCount); }
		}

		public string CooldownText
		{
			get {
				OrcaTradeCooldownRule cooldown = Rules.OfType<OrcaTradeCooldownRule>().FirstOrDefault();
				return cooldown == null ? "Ready" : cooldown.CooldownText;
			}
		}

		public string CurrentPositionSizeText
		{
			get {
				if (currentPositionsByInstrument.Count == 0)
					return "0";
				int max = currentPositionsByInstrument.Values.Select(Math.Abs).DefaultIfEmpty(0).Max();
				return max.ToString(CultureInfo.InvariantCulture);
			}
		}

		public void Start(Account account)
		{
			Status = OrcaDisciplineSessionStatus.Active;
			StartTime = DateTime.Now;
			EndTime = DateTime.MinValue;
			baselineRealizedPnl = SafeAccountGet(account, AccountItem.RealizedProfitLoss);
			SessionRealizedPnl = 0;
			CompletedTradeCount = 0;
			ConsecutiveLosses = 0;
			WinningTrades = 0;
			LosingTrades = 0;
			tracker.Reset();
			Violations.Clear();
			currentPositionsByInstrument.Clear();
			SyncOpenPositions(account, false);
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnSessionStart(this);
			RecalculateScore();
			RaiseAll();
		}

		public void Pause()
		{
			if (Status == OrcaDisciplineSessionStatus.Active)
				Status = OrcaDisciplineSessionStatus.Paused;
		}

		public void Resume()
		{
			if (Status == OrcaDisciplineSessionStatus.Paused)
				Status = OrcaDisciplineSessionStatus.Active;
		}

		public void End()
		{
			if (Status == OrcaDisciplineSessionStatus.Ended)
				return;
			EndTime = DateTime.Now;
			Status = OrcaDisciplineSessionStatus.Ended;
			RecalculateScore();
			RaiseAll();
		}

		public void OnOrderUpdate(OrderEventArgs e)
		{
			if (e == null || e.Order == null || e.Order.Instrument == null)
				return;
			ObserveInstrument(e.Order.Instrument);
		}

		public void OnExecution(ExecutionEventArgs e)
		{
			if (Status != OrcaDisciplineSessionStatus.Active || e == null || e.Execution == null)
				return;
			Execution execution = e.Execution;
			if (execution.Instrument == null || !MatchesInstrumentFilter(execution.Instrument))
				return;
			ObserveInstrument(execution.Instrument);
			OrcaTradeUpdate update = tracker.ProcessExecution(execution, e.Time);
			if (update == null)
				return;
			if (update.NewTradeStarted != null)
				ApplyNewTrade(update.NewTradeStarted);
			if (update.IncreasedTrade != null)
				ApplyTradeIncreased(update.IncreasedTrade);
			foreach (OrcaRoundTripTrade trade in update.CompletedTrades)
				ApplyCompletedTrade(trade);
			SyncPositionFromTracker(update.InstrumentName, update.CurrentSignedPosition);
			RefreshRulesCurrentValues();
			RecalculateScore();
			RaiseAll();
		}

		public void OnPositionUpdate(PositionEventArgs e)
		{
			if (e == null || e.Position == null || e.Position.Instrument == null)
				return;
			ObserveInstrument(e.Position.Instrument);
			if (!MatchesInstrumentFilter(e.Position.Instrument))
				return;
			int signed = e.MarketPosition == MarketPosition.Short ? -Math.Abs(e.Quantity) : Math.Abs(e.Quantity);
			if (e.MarketPosition == MarketPosition.Flat || e.Quantity == 0)
				signed = 0;
			SyncPositionFromTracker(InstrumentName(e.Position.Instrument), signed);
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnPositionSnapshot(this, currentPositionsByInstrument);
			RefreshRulesCurrentValues();
			RaiseAll();
		}

		public void OnAccountItemUpdate(AccountItemEventArgs e)
		{
			if (e == null || e.AccountItem != AccountItem.RealizedProfitLoss || e.Currency != Currency.UsDollar)
				return;
			SessionRealizedPnl = e.Value - baselineRealizedPnl;
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnAccountItem(this, e);
			RecalculateScore();
			RaiseAll();
		}

		public void OnTimerTick(Account account)
		{
			if (Status == OrcaDisciplineSessionStatus.NotStarted || Status == OrcaDisciplineSessionStatus.Ended)
				return;
			SyncOpenPositions(account, Status == OrcaDisciplineSessionStatus.Active);
			if (account != null)
				SessionRealizedPnl = SafeAccountGet(account, AccountItem.RealizedProfitLoss) - baselineRealizedPnl;
			if (Status != OrcaDisciplineSessionStatus.Active) {
				RefreshRulesCurrentValues();
				RecalculateScore();
				RaiseAll();
				return;
			}
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnTimerTick(this, DateTime.Now);
			RefreshRulesCurrentValues();
			RecalculateScore();
			RaiseAll();
		}

		public void AddViolation(OrcaDisciplineRule rule, string message, string instrument, string observed, string limit)
		{
			if (rule == null || !rule.Enabled)
				return;
			OrcaDisciplineViolation violation = new OrcaDisciplineViolation {
				Timestamp = DateTime.Now,
				RuleId = rule.Id,
				RuleName = rule.Name,
				Severity = rule.Severity,
				Message = message,
				Account = AccountName,
				Instrument = instrument ?? string.Empty,
				ValueObserved = observed ?? string.Empty,
				LimitValue = limit ?? string.Empty
			};
			Violations.Insert(0, violation);
			rule.RegisterViolation(violation);
			foreach (OrcaDisciplineRule candidate in Rules) {
				if (!object.ReferenceEquals(candidate, rule))
					candidate.OnViolationAdded(this, violation);
			}
			RecalculateScore();
			RaiseAll();
		}

		public OrcaDisciplineSessionReport CreateReport()
		{
			return new OrcaDisciplineSessionReport {
				AccountName = AccountName,
				TemplateName = TemplateName,
				InstrumentFilter = InstrumentFilter,
				StartTime = StartTime,
				EndTime = EndTime == DateTime.MinValue ? DateTime.Now : EndTime,
				Status = Status.ToString(),
				Score = Score,
				Grade = Grade,
				SessionRealizedPnl = SessionRealizedPnl,
				CompletedTradeCount = CompletedTradeCount,
				WinningTrades = WinningTrades,
				LosingTrades = LosingTrades,
				ConsecutiveLosses = ConsecutiveLosses,
				TotalRulesFollowed = TotalRulesFollowed,
				TotalViolations = TotalViolations,
				CriticalViolations = CriticalViolations,
				Summary = BuildSummary(),
				Violations = Violations.ToList(),
				Rules = Rules.Select(r => r.CreateSnapshot()).ToList()
			};
		}

		public OrcaDisciplineRuleTemplate CreateTemplateSnapshot(string templateName)
		{
			return new OrcaDisciplineRuleTemplate {
				Name = string.IsNullOrWhiteSpace(templateName) ? TemplateName : templateName,
				Rules = Rules.Select(r => r.CreateConfigSnapshot()).ToList()
			};
		}

		public OrcaDisciplineRule AddConfiguredRule(OrcaDisciplineRuleConfig config)
		{
			OrcaDisciplineRule rule = OrcaDisciplineRuleFactory.Create(config);
			AddRule(rule);
			RefreshRulesCurrentValues();
			RecalculateScore();
			RaiseAll();
			return rule;
		}

		public bool RemoveRule(OrcaDisciplineRule rule)
		{
			if (rule == null || Rules == null || Rules.Count <= 1 || !Rules.Contains(rule))
				return false;
			rule.PropertyChanged -= OnRulePropertyChanged;
			Rules.Remove(rule);
			RefreshRulesCurrentValues();
			RecalculateScore();
			RaiseAll();
			return true;
		}

		public string BuildSummary()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Orca Discipline Guard Session");
			sb.AppendLine("Account: " + AccountName);
			sb.AppendLine("Template: " + TemplateName);
			sb.AppendLine("Instrument Filter: " + InstrumentFilter);
			sb.AppendLine("Status: " + StatusText);
			if (StartTime != DateTime.MinValue)
				sb.AppendLine("Start: " + StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
			if (EndTime != DateTime.MinValue)
				sb.AppendLine("End: " + EndTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
			sb.AppendLine("Grade: " + Grade + " (" + Score.ToString("0", CultureInfo.InvariantCulture) + ")");
			sb.AppendLine("Session P&L: " + SessionRealizedPnl.ToString("C2", CultureInfo.CurrentCulture));
			sb.AppendLine("Completed Trades: " + CompletedTradeCount.ToString(CultureInfo.InvariantCulture));
			sb.AppendLine("Wins / Losses: " + WinningTrades.ToString(CultureInfo.InvariantCulture) + " / " + LosingTrades.ToString(CultureInfo.InvariantCulture));
			sb.AppendLine("Consecutive Losses: " + ConsecutiveLosses.ToString(CultureInfo.InvariantCulture));
			sb.AppendLine("Rules Followed: " + TotalRulesFollowed.ToString(CultureInfo.InvariantCulture));
			sb.AppendLine("Violations: " + TotalViolations.ToString(CultureInfo.InvariantCulture) + " (" + CriticalViolations.ToString(CultureInfo.InvariantCulture) + " critical)");
			sb.AppendLine();
			sb.AppendLine("Rules Broken:");
			foreach (IGrouping<string, OrcaDisciplineViolation> group in Violations.GroupBy(v => v.RuleName).OrderByDescending(g => g.Count()))
				sb.AppendLine("- " + group.Key + ": " + group.Count().ToString(CultureInfo.InvariantCulture));
			if (Violations.Count == 0)
				sb.AppendLine("- None");
			sb.AppendLine();
			sb.AppendLine("Most Recent Violations:");
			foreach (OrcaDisciplineViolation violation in Violations.Take(8))
				sb.AppendLine("- " + violation.DisplayTime + " " + violation.RuleName + ": " + violation.Message);
			if (Violations.Count == 0)
				sb.AppendLine("- None");
			return sb.ToString();
		}

		private void AddRule(OrcaDisciplineRule rule)
		{
			if (rule == null)
				return;
			rule.PropertyChanged += OnRulePropertyChanged;
			Rules.Add(rule);
		}

		private void OnRulePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OrcaDisciplineRule rule = sender as OrcaDisciplineRule;
			if (rule != null && rule.Mode == OrcaDisciplineRuleMode.Manual && e.PropertyName == "ManualAction")
				ApplyManualRule(rule);
			if (e.PropertyName == "ParameterText") {
				RefreshRulesCurrentValues();
				Raise("Rules");
			}
			RecalculateScore();
			RaiseAll();
		}

		private void ApplyManualRule(OrcaDisciplineRule rule)
		{
			if (rule == null || !rule.Enabled)
				return;
			if (string.Equals(rule.ManualAction, OrcaManualActionValues.Followed, StringComparison.OrdinalIgnoreCase)) {
				rule.RegisterFollow();
				rule.RefreshCurrentValue(this);
				return;
			}
			if (string.Equals(rule.ManualAction, OrcaManualActionValues.Broken, StringComparison.OrdinalIgnoreCase) && !rule.HasManualBrokenViolation) {
				rule.HasManualBrokenViolation = true;
				AddViolation(rule, "Manual rule marked broken" + (string.IsNullOrWhiteSpace(rule.Notes) ? string.Empty : ": " + rule.Notes), string.Empty, "Broken", "Followed");
				rule.RefreshCurrentValue(this);
			}
			if (string.Equals(rule.ManualAction, OrcaManualActionValues.NotApplicable, StringComparison.OrdinalIgnoreCase)) {
				rule.Status = OrcaDisciplineRuleStatus.Disabled;
				rule.RefreshCurrentValue(this);
			}
		}

		private void ApplyNewTrade(OrcaRoundTripTrade trade)
		{
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnTradeStarted(this, trade);
		}

		private void ApplyTradeIncreased(OrcaRoundTripTrade trade)
		{
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnTradeIncreased(this, trade);
		}

		private void ApplyCompletedTrade(OrcaRoundTripTrade trade)
		{
			CompletedTradeCount = CompletedTradeCount + 1;
			if (trade.RealizedPnl < 0) {
				LosingTrades = LosingTrades + 1;
				ConsecutiveLosses = ConsecutiveLosses + 1;
			} else {
				if (trade.RealizedPnl > 0)
					WinningTrades = WinningTrades + 1;
				ConsecutiveLosses = 0;
			}
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnTradeCompleted(this, trade);
		}

		private void SyncOpenPositions(Account account, bool notifyRules)
		{
			if (account == null)
				return;
			currentPositionsByInstrument.Clear();
			try {
				foreach (Position position in account.Positions) {
					if (position == null || position.Instrument == null)
						continue;
					ObserveInstrument(position.Instrument);
					if (!MatchesInstrumentFilter(position.Instrument))
						continue;
					int signed = position.MarketPosition == MarketPosition.Short ? -Math.Abs(position.Quantity) : Math.Abs(position.Quantity);
					if (position.MarketPosition == MarketPosition.Flat || position.Quantity == 0)
						signed = 0;
					if (signed != 0)
						currentPositionsByInstrument[InstrumentName(position.Instrument)] = signed;
					if (Status == OrcaDisciplineSessionStatus.Active)
						tracker.SeedOpenPosition(position);
				}
			} catch { }
			if (notifyRules) {
				foreach (OrcaDisciplineRule rule in Rules)
					rule.OnPositionSnapshot(this, currentPositionsByInstrument);
			}
		}

		private void SyncPositionSnapshot(Account account)
		{
			if (account != null)
				SyncOpenPositions(account, true);
		}

		private void SyncPositionFromTracker(string instrumentName, int signedPosition)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
				return;
			if (signedPosition == 0)
				currentPositionsByInstrument.Remove(instrumentName);
			else
				currentPositionsByInstrument[instrumentName] = signedPosition;
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnPositionSnapshot(this, currentPositionsByInstrument);
		}

		private void RefreshRulesCurrentValues()
		{
			foreach (OrcaDisciplineRule rule in Rules)
				rule.RefreshCurrentValue(this);
		}

		private void RecalculateScore()
		{
			int penalty = Violations.Sum(v => OrcaDisciplineScoring.Penalty(v.Severity));
			Score = 100 - penalty;
			Grade = OrcaDisciplineScoring.Grade(Score);
			Raise("TotalViolations");
			Raise("CriticalViolations");
			Raise("TotalRulesFollowed");
		}

		private void RaiseAll()
		{
			Raise("ObservedInstrumentNames");
			Raise("SessionRealizedPnl");
			Raise("CompletedTradeCount");
			Raise("ConsecutiveLosses");
			Raise("WinningTrades");
			Raise("LosingTrades");
			Raise("CooldownText");
			Raise("CurrentPositionSizeText");
			Raise("StatusText");
		}

		private bool MatchesInstrumentFilter(Instrument instrument)
		{
			if (instrument == null)
				return false;
			if (string.IsNullOrWhiteSpace(InstrumentFilter) || string.Equals(InstrumentFilter, OrcaDisciplineConstants.AllInstruments, StringComparison.OrdinalIgnoreCase))
				return true;
			return string.Equals(InstrumentName(instrument), InstrumentFilter, StringComparison.OrdinalIgnoreCase);
		}

		private void ObserveInstrument(Instrument instrument)
		{
			string name = InstrumentName(instrument);
			if (!string.IsNullOrWhiteSpace(name))
				observedInstrumentNames.Add(name);
		}

		private static string InstrumentName(Instrument instrument)
		{
			if (instrument == null)
				return string.Empty;
			return string.IsNullOrWhiteSpace(instrument.FullName) ? (instrument.MasterInstrument == null ? string.Empty : instrument.MasterInstrument.Name) : instrument.FullName;
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
	}

	public abstract class OrcaDisciplineRule : OrcaDisciplineNotifyBase
	{
		private bool enabled;
		private OrcaDisciplineRuleStatus status;
		private int violationCount;
		private int followCount;
		private DateTime lastViolationTime;
		private string lastViolationMessage;
		private string currentValueText;
		private string notes;
		private string manualAction;

		protected OrcaDisciplineRule(OrcaDisciplineRuleConfig config)
		{
			if (config == null)
				config = new OrcaDisciplineRuleConfig();
			Id = config.Id;
			Type = config.Type;
			Name = config.Name;
			Description = config.Description;
			Enabled = config.Enabled;
			Mode = config.Mode;
			Severity = config.Severity;
			Parameters = config.Parameters == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(config.Parameters, StringComparer.OrdinalIgnoreCase);
			Status = Enabled ? OrcaDisciplineRuleStatus.NotStarted : OrcaDisciplineRuleStatus.Disabled;
			ManualAction = string.Empty;
			CurrentValueText = string.Empty;
		}

		public string Id { get; private set; }
		public string Type { get; private set; }
		public string Name { get; private set; }
		public string Description { get; private set; }
		public OrcaDisciplineRuleMode Mode { get; private set; }
		public OrcaDisciplineSeverity Severity { get; private set; }
		public Dictionary<string, string> Parameters { get; private set; }
		public bool HasManualBrokenViolation { get; set; }

		public bool Enabled
		{
			get { return enabled; }
			set {
				if (!Set(ref enabled, value, "Enabled"))
					return;
				Status = value ? OrcaDisciplineRuleStatus.NotStarted : OrcaDisciplineRuleStatus.Disabled;
			}
		}

		public OrcaDisciplineRuleStatus Status
		{
			get { return status; }
			set { Set(ref status, value, "Status"); }
		}

		public int ViolationCount
		{
			get { return violationCount; }
			private set { Set(ref violationCount, value, "ViolationCount"); }
		}

		public int FollowCount
		{
			get { return followCount; }
			private set { Set(ref followCount, value, "FollowCount"); }
		}

		public DateTime LastViolationTime
		{
			get { return lastViolationTime; }
			private set { Set(ref lastViolationTime, value, "LastViolationTime"); }
		}

		public string LastViolationMessage
		{
			get { return lastViolationMessage; }
			private set { Set(ref lastViolationMessage, value, "LastViolationMessage"); }
		}

		public string CurrentValueText
		{
			get { return currentValueText; }
			protected set { Set(ref currentValueText, value, "CurrentValueText"); }
		}

		public string Notes
		{
			get { return notes; }
			set { Set(ref notes, value, "Notes"); }
		}

		public string ManualAction
		{
			get { return manualAction; }
			set { Set(ref manualAction, value, "ManualAction"); }
		}

		public string ParameterText
		{
			get { return FormatParameters(); }
			set {
				if (!ApplyParameterText(value))
					return;
				Raise("ParameterText");
				Raise("LimitText");
			}
		}

		public bool IsManual
		{
			get { return Mode == OrcaDisciplineRuleMode.Manual || Mode == OrcaDisciplineRuleMode.Hybrid; }
		}

		public virtual string LimitText
		{
			get { return string.Empty; }
		}

		public virtual void OnSessionStart(OrcaDisciplineSession session)
		{
			ViolationCount = 0;
			FollowCount = 0;
			LastViolationMessage = string.Empty;
			LastViolationTime = DateTime.MinValue;
			HasManualBrokenViolation = false;
			if (Enabled)
				Status = OrcaDisciplineRuleStatus.Passing;
		}

		public virtual void OnTradeStarted(OrcaDisciplineSession session, OrcaRoundTripTrade trade) { }
		public virtual void OnTradeIncreased(OrcaDisciplineSession session, OrcaRoundTripTrade trade) { }
		public virtual void OnTradeCompleted(OrcaDisciplineSession session, OrcaRoundTripTrade trade) { }
		public virtual void OnViolationAdded(OrcaDisciplineSession session, OrcaDisciplineViolation violation) { }
		public virtual void OnPositionSnapshot(OrcaDisciplineSession session, IDictionary<string, int> positionsByInstrument) { }
		public virtual void OnAccountItem(OrcaDisciplineSession session, AccountItemEventArgs e) { }
		public virtual void OnTimerTick(OrcaDisciplineSession session, DateTime now) { }
		public virtual void RefreshCurrentValue(OrcaDisciplineSession session) { }

		public void RegisterViolation(OrcaDisciplineViolation violation)
		{
			ViolationCount = ViolationCount + 1;
			LastViolationTime = violation == null ? DateTime.Now : violation.Timestamp;
			LastViolationMessage = violation == null ? string.Empty : violation.Message;
			Status = OrcaDisciplineRuleStatus.Violated;
		}

		public void RegisterFollow()
		{
			FollowCount = 1;
			if (Enabled)
				Status = OrcaDisciplineRuleStatus.Passing;
		}

		public OrcaDisciplineRuleSnapshot CreateSnapshot()
		{
			return new OrcaDisciplineRuleSnapshot {
				Id = Id,
				Name = Name,
				Description = Description,
				Enabled = Enabled,
				Mode = Mode.ToString(),
				Severity = Severity.ToString(),
				Status = Status.ToString(),
				ViolationCount = ViolationCount,
				FollowCount = FollowCount,
				LastViolationTime = LastViolationTime,
				LastViolationMessage = LastViolationMessage,
				CurrentValueText = CurrentValueText,
				LimitText = LimitText,
				ManualAction = ManualAction,
				Notes = Notes
			};
		}

		public OrcaDisciplineRuleConfig CreateConfigSnapshot()
		{
			return new OrcaDisciplineRuleConfig {
				Id = Id,
				Type = Type,
				Name = Name,
				Description = Description,
				Enabled = Enabled,
				Mode = Mode,
				Severity = Severity,
				Parameters = Parameters == null ? new Dictionary<string, string>() : new Dictionary<string, string>(Parameters, StringComparer.OrdinalIgnoreCase)
			};
		}

		protected int IntParameter(string key, int fallback)
		{
			string raw;
			if (Parameters != null && Parameters.TryGetValue(key, out raw)) {
				int parsed;
				if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
					return parsed;
			}
			return fallback;
		}

		protected double DoubleParameter(string key, double fallback)
		{
			string raw;
			if (Parameters != null && Parameters.TryGetValue(key, out raw)) {
				double parsed;
				if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
					return parsed;
			}
			return fallback;
		}

		protected string StringParameter(string key, string fallback)
		{
			string raw;
			if (Parameters != null && Parameters.TryGetValue(key, out raw) && !string.IsNullOrWhiteSpace(raw))
				return raw;
			return fallback;
		}

		protected TimeSpan TimeParameter(string key, TimeSpan fallback)
		{
			string raw;
			if (Parameters != null && Parameters.TryGetValue(key, out raw)) {
				TimeSpan parsed;
				if (TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out parsed))
					return parsed;
				DateTime time;
				if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out time))
					return time.TimeOfDay;
			}
			return fallback;
		}

		private string FormatParameters()
		{
			if (Parameters == null || Parameters.Count == 0)
				return LimitText;
			return string.Join("; ", Parameters.OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value).ToArray());
		}

		private bool ApplyParameterText(string value)
		{
			Dictionary<string, string> parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (!string.IsNullOrWhiteSpace(value)) {
				string[] parts = value.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string rawPart in parts) {
					string part = rawPart == null ? string.Empty : rawPart.Trim();
					int equalsIndex = part.IndexOf('=');
					if (equalsIndex <= 0 || equalsIndex >= part.Length - 1)
						continue;
					string key = part.Substring(0, equalsIndex).Trim();
					string parameterValue = part.Substring(equalsIndex + 1).Trim();
					if (!string.IsNullOrWhiteSpace(key))
						parsed[key] = parameterValue;
				}
				if (parsed.Count == 0)
					return false;
			}
			if (Parameters != null && parsed.Count == Parameters.Count) {
				bool same = true;
				foreach (KeyValuePair<string, string> pair in parsed) {
					string existing;
					if (!Parameters.TryGetValue(pair.Key, out existing) || !string.Equals(existing, pair.Value, StringComparison.Ordinal)) {
						same = false;
						break;
					}
				}
				if (same)
					return false;
			}
			Parameters = parsed;
			return true;
		}
	}

	public sealed class OrcaTradeCooldownRule : OrcaDisciplineRule
	{
		private DateTime lastTradeStartTime;
		private string cooldownText = "Ready";

		public OrcaTradeCooldownRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public int MinimumMinutes
		{
			get { return Math.Max(0, IntParameter("MinimumMinutes", 5)); }
		}

		public string CooldownText
		{
			get { return cooldownText; }
			private set { Set(ref cooldownText, value, "CooldownText"); }
		}

		public override string LimitText
		{
			get { return MinimumMinutes.ToString(CultureInfo.InvariantCulture) + " min"; }
		}

		public override void OnSessionStart(OrcaDisciplineSession session)
		{
			base.OnSessionStart(session);
			lastTradeStartTime = DateTime.MinValue;
			CooldownText = "Ready";
			CurrentValueText = "Ready";
		}

		public override void OnTradeStarted(OrcaDisciplineSession session, OrcaRoundTripTrade trade)
		{
			if (!Enabled || trade == null)
				return;
			if (lastTradeStartTime != DateTime.MinValue) {
				double elapsed = (trade.EntryTime - lastTradeStartTime).TotalMinutes;
				if (elapsed < MinimumMinutes)
					session.AddViolation(this, "New trade started before cooldown expired", trade.InstrumentName, elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " min", LimitText);
			}
			lastTradeStartTime = trade.EntryTime;
			Status = ViolationCount > 0 ? OrcaDisciplineRuleStatus.Violated : OrcaDisciplineRuleStatus.Passing;
			RefreshCurrentValue(session);
		}

		public override void OnTimerTick(OrcaDisciplineSession session, DateTime now)
		{
			RefreshCurrentValue(session);
		}

		public override void RefreshCurrentValue(OrcaDisciplineSession session)
		{
			if (lastTradeStartTime == DateTime.MinValue || MinimumMinutes <= 0) {
				CooldownText = "Ready";
				CurrentValueText = "Ready";
				return;
			}
			DateTime nextAllowed = lastTradeStartTime.AddMinutes(MinimumMinutes);
			TimeSpan remaining = nextAllowed - DateTime.Now;
			if (remaining <= TimeSpan.Zero) {
				CooldownText = "Ready";
				CurrentValueText = "Ready";
			} else {
				CooldownText = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", (int)remaining.TotalMinutes, remaining.Seconds);
				CurrentValueText = CooldownText;
			}
		}
	}

	public sealed class OrcaMaxPositionSizeRule : OrcaDisciplineRule
	{
		private readonly HashSet<string> breachedInstruments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public OrcaMaxPositionSizeRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public int MaxContracts
		{
			get { return Math.Max(1, IntParameter("MaxContracts", 2)); }
		}

		public int MicroMultiplier
		{
			get { return Math.Max(1, IntParameter("MicroMultiplier", int.Parse(OrcaDisciplineConstants.DefaultMicroMultiplier, CultureInfo.InvariantCulture))); }
		}

		public string MicroSymbolsText
		{
			get { return StringParameter("MicroSymbols", OrcaDisciplineConstants.DefaultMicroSymbols); }
		}

		public override string LimitText
		{
			get {
				int microLimit = MaxContracts * MicroMultiplier;
				return MaxContracts.ToString(CultureInfo.InvariantCulture) + " mini / " + microLimit.ToString(CultureInfo.InvariantCulture) + " micro";
			}
		}

		public override void OnSessionStart(OrcaDisciplineSession session)
		{
			base.OnSessionStart(session);
			breachedInstruments.Clear();
		}

		public override void OnPositionSnapshot(OrcaDisciplineSession session, IDictionary<string, int> positionsByInstrument)
		{
			if (!Enabled || positionsByInstrument == null)
				return;
			foreach (string instrument in breachedInstruments.ToArray()) {
				int signed;
				if (!positionsByInstrument.TryGetValue(instrument, out signed) || Math.Abs(signed) <= AllowedContractsFor(instrument))
					breachedInstruments.Remove(instrument);
			}
			double maxMiniEquivalent = 0;
			string maxObservedText = "0";
			foreach (KeyValuePair<string, int> pair in positionsByInstrument) {
				int abs = Math.Abs(pair.Value);
				int allowed = AllowedContractsFor(pair.Key);
				double miniEquivalent = MiniEquivalentContracts(pair.Key, abs);
				if (miniEquivalent >= maxMiniEquivalent) {
					maxMiniEquivalent = miniEquivalent;
					maxObservedText = FormatPositionValue(pair.Key, abs, allowed);
				}
				if (abs > allowed) {
					if (!breachedInstruments.Contains(pair.Key)) {
						breachedInstruments.Add(pair.Key);
						session.AddViolation(this, "Position size exceeded maximum mini-equivalent contracts", pair.Key, FormatObserved(abs, miniEquivalent), FormatLimit(pair.Key, allowed));
					}
				} else {
					breachedInstruments.Remove(pair.Key);
				}
			}
			CurrentValueText = maxObservedText;
			if (ViolationCount == 0)
				Status = OrcaDisciplineRuleStatus.Passing;
		}

		private int AllowedContractsFor(string instrumentName)
		{
			return IsMicroInstrument(instrumentName) ? MaxContracts * MicroMultiplier : MaxContracts;
		}

		private double MiniEquivalentContracts(string instrumentName, int contracts)
		{
			if (!IsMicroInstrument(instrumentName))
				return contracts;
			return contracts / (double)Math.Max(1, MicroMultiplier);
		}

		private string FormatPositionValue(string instrumentName, int contracts, int allowed)
		{
			return InstrumentRoot(instrumentName) + " " + contracts.ToString(CultureInfo.InvariantCulture)
				+ "/" + allowed.ToString(CultureInfo.InvariantCulture)
				+ " (" + MiniEquivalentContracts(instrumentName, contracts).ToString("0.##", CultureInfo.InvariantCulture) + " mini)";
		}

		private string FormatObserved(int contracts, double miniEquivalent)
		{
			return contracts.ToString(CultureInfo.InvariantCulture) + " contracts (" + miniEquivalent.ToString("0.##", CultureInfo.InvariantCulture) + " mini)";
		}

		private string FormatLimit(string instrumentName, int allowed)
		{
			return allowed.ToString(CultureInfo.InvariantCulture) + " contracts (" + MaxContracts.ToString(CultureInfo.InvariantCulture) + " mini)";
		}

		private bool IsMicroInstrument(string instrumentName)
		{
			string root = InstrumentRoot(instrumentName);
			if (string.IsNullOrWhiteSpace(root))
				return false;
			foreach (string symbol in MicroSymbols()) {
				if (string.Equals(root, symbol, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		private IEnumerable<string> MicroSymbols()
		{
			string text = MicroSymbolsText ?? string.Empty;
			string[] parts = text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string raw in parts) {
				string symbol = NormalizeSymbol(raw);
				if (!string.IsNullOrWhiteSpace(symbol))
					yield return symbol;
			}
		}

		private static string InstrumentRoot(string instrumentName)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
				return string.Empty;
			string trimmed = instrumentName.Trim();
			int space = trimmed.IndexOf(' ');
			if (space > 0)
				trimmed = trimmed.Substring(0, space);
			int dash = trimmed.IndexOf('-');
			if (dash > 0)
				trimmed = trimmed.Substring(0, dash);
			return NormalizeSymbol(trimmed);
		}

		private static string NormalizeSymbol(string symbol)
		{
			if (string.IsNullOrWhiteSpace(symbol))
				return string.Empty;
			symbol = symbol.Trim().TrimStart('@');
			return symbol.ToUpperInvariant();
		}
	}

	public sealed class OrcaMaxLossPerTradeRule : OrcaDisciplineRule
	{
		public OrcaMaxLossPerTradeRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public double MaxLoss
		{
			get { return Math.Max(0, DoubleParameter("MaxLoss", 300)); }
		}

		public override string LimitText
		{
			get { return MaxLoss.ToString("C0", CultureInfo.CurrentCulture); }
		}

		public override void OnTradeCompleted(OrcaDisciplineSession session, OrcaRoundTripTrade trade)
		{
			if (!Enabled || trade == null || MaxLoss <= 0)
				return;
			CurrentValueText = trade.RealizedPnl.ToString("C0", CultureInfo.CurrentCulture);
			if (trade.RealizedPnl <= -MaxLoss)
				session.AddViolation(this, "Completed trade loss exceeded limit", trade.InstrumentName, trade.RealizedPnl.ToString("C0", CultureInfo.CurrentCulture), "-" + LimitText);
			else if (ViolationCount == 0)
				Status = OrcaDisciplineRuleStatus.Passing;
		}
	}

	public sealed class OrcaMaxSessionLossRule : OrcaDisciplineRule
	{
		private bool breached;

		public OrcaMaxSessionLossRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public double MaxSessionLoss
		{
			get { return Math.Max(0, DoubleParameter("MaxLoss", 600)); }
		}

		public override string LimitText
		{
			get { return MaxSessionLoss.ToString("C0", CultureInfo.CurrentCulture); }
		}

		public override void OnSessionStart(OrcaDisciplineSession session)
		{
			base.OnSessionStart(session);
			breached = false;
		}

		public override void OnAccountItem(OrcaDisciplineSession session, AccountItemEventArgs e)
		{
			Check(session);
		}

		public override void OnTimerTick(OrcaDisciplineSession session, DateTime now)
		{
			Check(session);
		}

		public override void RefreshCurrentValue(OrcaDisciplineSession session)
		{
			if (session != null)
				CurrentValueText = session.SessionRealizedPnl.ToString("C0", CultureInfo.CurrentCulture);
		}

		private void Check(OrcaDisciplineSession session)
		{
			if (!Enabled || session == null || MaxSessionLoss <= 0)
				return;
			CurrentValueText = session.SessionRealizedPnl.ToString("C0", CultureInfo.CurrentCulture);
			if (!breached && session.SessionRealizedPnl <= -MaxSessionLoss) {
				breached = true;
				session.AddViolation(this, "Session realized loss limit breached", string.Empty, session.SessionRealizedPnl.ToString("C0", CultureInfo.CurrentCulture), "-" + LimitText);
			}
		}
	}

	public sealed class OrcaMaxTradesPerSessionRule : OrcaDisciplineRule
	{
		private int lastViolationTradeCount;

		public OrcaMaxTradesPerSessionRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public int MaxTrades
		{
			get { return Math.Max(1, IntParameter("MaxTrades", 5)); }
		}

		public override string LimitText
		{
			get { return MaxTrades.ToString(CultureInfo.InvariantCulture) + " trades"; }
		}

		public override void OnSessionStart(OrcaDisciplineSession session)
		{
			base.OnSessionStart(session);
			lastViolationTradeCount = 0;
		}

		public override void OnTradeCompleted(OrcaDisciplineSession session, OrcaRoundTripTrade trade)
		{
			if (!Enabled || session == null)
				return;
			CurrentValueText = session.CompletedTradeCount.ToString(CultureInfo.InvariantCulture);
			if (session.CompletedTradeCount > MaxTrades && session.CompletedTradeCount != lastViolationTradeCount) {
				lastViolationTradeCount = session.CompletedTradeCount;
				session.AddViolation(this, "Completed trade count exceeded session limit", trade == null ? string.Empty : trade.InstrumentName, session.CompletedTradeCount.ToString(CultureInfo.InvariantCulture), LimitText);
			}
		}
	}

	public sealed class OrcaMaxConsecutiveLossesRule : OrcaDisciplineRule
	{
		private int lastViolationLossCount;

		public OrcaMaxConsecutiveLossesRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public int MaxConsecutiveLosses
		{
			get { return Math.Max(1, IntParameter("MaxLosses", 2)); }
		}

		public override string LimitText
		{
			get { return MaxConsecutiveLosses.ToString(CultureInfo.InvariantCulture) + " losses"; }
		}

		public override void OnSessionStart(OrcaDisciplineSession session)
		{
			base.OnSessionStart(session);
			lastViolationLossCount = 0;
		}

		public override void OnTradeCompleted(OrcaDisciplineSession session, OrcaRoundTripTrade trade)
		{
			if (!Enabled || session == null)
				return;
			CurrentValueText = session.ConsecutiveLosses.ToString(CultureInfo.InvariantCulture);
			if (session.ConsecutiveLosses > MaxConsecutiveLosses && session.ConsecutiveLosses != lastViolationLossCount) {
				lastViolationLossCount = session.ConsecutiveLosses;
				session.AddViolation(this, "Consecutive losing trades exceeded limit", trade == null ? string.Empty : trade.InstrumentName, session.ConsecutiveLosses.ToString(CultureInfo.InvariantCulture), LimitText);
			}
		}
	}

	public sealed class OrcaAllowedTradingWindowRule : OrcaDisciplineRule
	{
		public OrcaAllowedTradingWindowRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public TimeSpan Start
		{
			get { return TimeParameter("Start", new TimeSpan(9, 30, 0)); }
		}

		public TimeSpan End
		{
			get { return TimeParameter("End", new TimeSpan(11, 30, 0)); }
		}

		public override string LimitText
		{
			get { return Start.ToString(@"hh\:mm", CultureInfo.InvariantCulture) + "-" + End.ToString(@"hh\:mm", CultureInfo.InvariantCulture); }
		}

		public override void OnTradeStarted(OrcaDisciplineSession session, OrcaRoundTripTrade trade)
		{
			if (!Enabled || trade == null)
				return;
			TimeSpan time = trade.EntryTime.TimeOfDay;
			CurrentValueText = time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
			bool allowed = Start <= End ? time >= Start && time <= End : time >= Start || time <= End;
			if (!allowed)
				session.AddViolation(this, "New trade started outside allowed trading window", trade.InstrumentName, CurrentValueText, LimitText);
			else if (ViolationCount == 0)
				Status = OrcaDisciplineRuleStatus.Passing;
		}
	}

	public sealed class OrcaMaxRuleViolationsRule : OrcaDisciplineRule
	{
		private bool breached;

		public OrcaMaxRuleViolationsRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public int MaxViolations
		{
			get { return Math.Max(1, IntParameter("MaxViolations", 3)); }
		}

		public override string LimitText
		{
			get { return MaxViolations.ToString(CultureInfo.InvariantCulture) + " violations"; }
		}

		public override void OnSessionStart(OrcaDisciplineSession session)
		{
			base.OnSessionStart(session);
			breached = false;
			CurrentValueText = "0";
		}

		public override void OnViolationAdded(OrcaDisciplineSession session, OrcaDisciplineViolation violation)
		{
			Check(session);
		}

		public override void RefreshCurrentValue(OrcaDisciplineSession session)
		{
			if (session != null)
				CurrentValueText = session.TotalViolations.ToString(CultureInfo.InvariantCulture);
		}

		private void Check(OrcaDisciplineSession session)
		{
			if (!Enabled || breached || session == null)
				return;
			CurrentValueText = session.TotalViolations.ToString(CultureInfo.InvariantCulture);
			if (session.TotalViolations > MaxViolations) {
				breached = true;
				session.AddViolation(this, "Rule violation count exceeded session limit", string.Empty, session.TotalViolations.ToString(CultureInfo.InvariantCulture), LimitText);
			}
		}
	}

	public sealed class OrcaNoAddToLosingTradeRule : OrcaDisciplineRule
	{
		public OrcaNoAddToLosingTradeRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public override string LimitText
		{
			get { return "No losing adds"; }
		}

		public override void OnTradeIncreased(OrcaDisciplineSession session, OrcaRoundTripTrade trade)
		{
			if (!Enabled || trade == null)
				return;
			CurrentValueText = trade.LastIncreaseUnrealizedPnl.ToString("C0", CultureInfo.CurrentCulture);
			if (trade.LastIncreaseWasLosing)
				session.AddViolation(this, "Position was increased while the open trade was losing", trade.InstrumentName, CurrentValueText, ">= $0 before add");
			else if (ViolationCount == 0)
				Status = OrcaDisciplineRuleStatus.Passing;
		}
	}

	public sealed class OrcaNoImmediateLossReversalRule : OrcaDisciplineRule
	{
		public OrcaNoImmediateLossReversalRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public int MinimumMinutesAfterLoss
		{
			get { return Math.Max(0, IntParameter("MinimumMinutes", 5)); }
		}

		public override string LimitText
		{
			get { return MinimumMinutesAfterLoss.ToString(CultureInfo.InvariantCulture) + " min after loss"; }
		}

		public override void OnTradeStarted(OrcaDisciplineSession session, OrcaRoundTripTrade trade)
		{
			if (!Enabled || trade == null || trade.PreviousTradeExitTime == DateTime.MinValue)
				return;
			bool oppositeDirection = !string.IsNullOrWhiteSpace(trade.PreviousTradeDirection)
				&& !string.Equals(trade.PreviousTradeDirection, trade.Direction, StringComparison.OrdinalIgnoreCase);
			if (!oppositeDirection || trade.PreviousTradeRealizedPnl >= 0)
				return;
			double minutes = trade.TimeSincePreviousTrade.TotalMinutes;
			CurrentValueText = minutes.ToString("0.0", CultureInfo.InvariantCulture) + " min";
			if (minutes <= MinimumMinutesAfterLoss)
				session.AddViolation(this, "New opposite-direction trade started too soon after a losing trade", trade.InstrumentName, CurrentValueText, LimitText);
			else if (ViolationCount == 0)
				Status = OrcaDisciplineRuleStatus.Passing;
		}
	}

	public sealed class OrcaManualChecklistRule : OrcaDisciplineRule
	{
		public OrcaManualChecklistRule(OrcaDisciplineRuleConfig config) : base(config) { }

		public override string LimitText
		{
			get { return "Manual"; }
		}

		public override void RefreshCurrentValue(OrcaDisciplineSession session)
		{
			CurrentValueText = string.IsNullOrWhiteSpace(ManualAction) ? "Unmarked" : ManualAction;
		}
	}

	public static class OrcaDisciplineRuleFactory
	{
		public static OrcaDisciplineRule Create(OrcaDisciplineRuleConfig config)
		{
			string type = config == null ? string.Empty : config.Type;
			if (string.Equals(type, "TradeCooldown", StringComparison.OrdinalIgnoreCase)) return new OrcaTradeCooldownRule(config);
			if (string.Equals(type, "MaxPositionSize", StringComparison.OrdinalIgnoreCase)) return new OrcaMaxPositionSizeRule(config);
			if (string.Equals(type, "MaxLossPerTrade", StringComparison.OrdinalIgnoreCase)) return new OrcaMaxLossPerTradeRule(config);
			if (string.Equals(type, "MaxSessionLoss", StringComparison.OrdinalIgnoreCase)) return new OrcaMaxSessionLossRule(config);
			if (string.Equals(type, "MaxTradesPerSession", StringComparison.OrdinalIgnoreCase)) return new OrcaMaxTradesPerSessionRule(config);
			if (string.Equals(type, "MaxConsecutiveLosses", StringComparison.OrdinalIgnoreCase)) return new OrcaMaxConsecutiveLossesRule(config);
			if (string.Equals(type, "AllowedTradingWindow", StringComparison.OrdinalIgnoreCase)) return new OrcaAllowedTradingWindowRule(config);
			if (string.Equals(type, "MaxRuleViolations", StringComparison.OrdinalIgnoreCase)) return new OrcaMaxRuleViolationsRule(config);
			if (string.Equals(type, "NoAddToLosingTrade", StringComparison.OrdinalIgnoreCase)) return new OrcaNoAddToLosingTradeRule(config);
			if (string.Equals(type, "NoImmediateLossReversal", StringComparison.OrdinalIgnoreCase)) return new OrcaNoImmediateLossReversalRule(config);
			return new OrcaManualChecklistRule(config);
		}
	}

	public sealed class OrcaRoundTripTracker
	{
		private readonly Dictionary<string, OrcaRoundTripTrade> openTrades = new Dictionary<string, OrcaRoundTripTrade>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, OrcaRoundTripTrade> lastCompletedTrades = new Dictionary<string, OrcaRoundTripTrade>(StringComparer.OrdinalIgnoreCase);
		private int nextTradeId = 1;

		public void Reset()
		{
			openTrades.Clear();
			lastCompletedTrades.Clear();
			nextTradeId = 1;
		}

		public void SeedOpenPosition(Position position)
		{
			if (position == null || position.Instrument == null || position.MarketPosition == MarketPosition.Flat || position.Quantity == 0)
				return;
			string key = InstrumentName(position.Instrument);
			if (openTrades.ContainsKey(key))
				return;
			int signed = position.MarketPosition == MarketPosition.Short ? -Math.Abs(position.Quantity) : Math.Abs(position.Quantity);
			openTrades[key] = new OrcaRoundTripTrade {
				TradeId = "seed-" + nextTradeId++.ToString(CultureInfo.InvariantCulture),
				InstrumentName = key,
				EntryTime = DateTime.Now,
				Direction = signed > 0 ? "Long" : "Short",
				Quantity = Math.Abs(signed),
				OpenQuantity = Math.Abs(signed),
				AverageEntry = position.AveragePrice,
				MaxPositionSize = Math.Abs(signed)
			};
		}

		public OrcaTradeUpdate ProcessExecution(Execution execution, DateTime eventTime)
		{
			if (execution == null || execution.Instrument == null || execution.Quantity <= 0)
				return null;
			int delta = SignedDelta(execution);
			if (delta == 0)
				return null;
			string instrumentName = InstrumentName(execution.Instrument);
			OrcaRoundTripTrade trade;
			openTrades.TryGetValue(instrumentName, out trade);
			int before = trade == null ? 0 : (string.Equals(trade.Direction, "Long", StringComparison.OrdinalIgnoreCase) ? trade.OpenQuantity : -trade.OpenQuantity);
			int after = before + delta;
			OrcaTradeUpdate update = new OrcaTradeUpdate { InstrumentName = instrumentName, CurrentSignedPosition = after };

			if (before == 0) {
				OrcaRoundTripTrade started = StartTrade(instrumentName, execution, eventTime, delta);
				openTrades[instrumentName] = started;
				update.NewTradeStarted = started;
				update.CurrentSignedPosition = delta;
				return update;
			}

			if (Math.Sign(before) == Math.Sign(delta)) {
				MarkScaleInContext(trade, execution, eventTime);
				ScaleIn(trade, execution, delta);
				update.IncreasedTrade = trade;
				update.CurrentSignedPosition = after;
				return update;
			}

			int closingQuantity = Math.Min(Math.Abs(before), Math.Abs(delta));
			CloseQuantity(trade, execution, eventTime, closingQuantity);
			if (Math.Abs(delta) < Math.Abs(before)) {
				trade.OpenQuantity = Math.Abs(after);
				openTrades[instrumentName] = trade;
				update.CurrentSignedPosition = after;
				return update;
			}

			trade.ExitTime = eventTime;
			trade.Quantity = Math.Max(trade.Quantity, trade.MaxPositionSize);
			update.CompletedTrades.Add(trade);
			openTrades.Remove(instrumentName);
			RememberCompletedTrade(instrumentName, trade);

			if (Math.Abs(delta) > closingQuantity) {
				int remainder = Math.Sign(delta) * (Math.Abs(delta) - closingQuantity);
				OrcaRoundTripTrade reversed = StartTrade(instrumentName, execution, eventTime, remainder);
				openTrades[instrumentName] = reversed;
				update.NewTradeStarted = reversed;
				update.CurrentSignedPosition = remainder;
			} else {
				update.CurrentSignedPosition = 0;
			}
			return update;
		}

		private OrcaRoundTripTrade StartTrade(string instrumentName, Execution execution, DateTime time, int signedQuantity)
		{
			OrcaRoundTripTrade trade = new OrcaRoundTripTrade {
				TradeId = nextTradeId++.ToString(CultureInfo.InvariantCulture),
				InstrumentName = instrumentName,
				EntryTime = time,
				Direction = signedQuantity > 0 ? "Long" : "Short",
				Quantity = Math.Abs(signedQuantity),
				OpenQuantity = Math.Abs(signedQuantity),
				AverageEntry = execution.Price,
				MaxPositionSize = Math.Abs(signedQuantity),
				RealizedPnl = 0
			};
			AttachPreviousTradeContext(trade, instrumentName, time);
			return trade;
		}

		private void MarkScaleInContext(OrcaRoundTripTrade trade, Execution execution, DateTime time)
		{
			if (trade == null || execution == null)
				return;
			double pointValue = PointValue(execution);
			int direction = string.Equals(trade.Direction, "Long", StringComparison.OrdinalIgnoreCase) ? 1 : -1;
			double unrealized = (execution.Price - trade.AverageEntry) * trade.OpenQuantity * pointValue * direction;
			trade.LastIncreaseTime = time;
			trade.LastIncreasePrice = execution.Price;
			trade.LastIncreaseUnrealizedPnl = unrealized;
			trade.LastIncreaseWasLosing = unrealized < 0;
		}

		private void ScaleIn(OrcaRoundTripTrade trade, Execution execution, int signedQuantity)
		{
			int addQuantity = Math.Abs(signedQuantity);
			double totalCost = trade.AverageEntry * trade.OpenQuantity + execution.Price * addQuantity;
			trade.OpenQuantity += addQuantity;
			trade.Quantity += addQuantity;
			trade.AverageEntry = totalCost / Math.Max(1, trade.OpenQuantity);
			trade.MaxPositionSize = Math.Max(trade.MaxPositionSize, trade.OpenQuantity);
			trade.WasIncreased = true;
		}

		private void CloseQuantity(OrcaRoundTripTrade trade, Execution execution, DateTime time, int closingQuantity)
		{
			double pointValue = PointValue(execution);
			int direction = string.Equals(trade.Direction, "Long", StringComparison.OrdinalIgnoreCase) ? 1 : -1;
			double pnl = (execution.Price - trade.AverageEntry) * closingQuantity * pointValue * direction;
			// MVP uses gross realized P&L from fills. Commissions/fees can be layered in once an Orca-wide commission source is standardized.
			trade.RealizedPnl += pnl;
			trade.ExitTime = time;
			double existingExitQty = Math.Max(0, trade.Quantity - trade.OpenQuantity);
			trade.AverageExit = existingExitQty <= 0
				? execution.Price
				: ((trade.AverageExit * existingExitQty) + execution.Price * closingQuantity) / (existingExitQty + closingQuantity);
			trade.OpenQuantity = Math.Max(0, trade.OpenQuantity - closingQuantity);
		}

		private void AttachPreviousTradeContext(OrcaRoundTripTrade trade, string instrumentName, DateTime entryTime)
		{
			if (trade == null || string.IsNullOrWhiteSpace(instrumentName))
				return;
			OrcaRoundTripTrade previous;
			if (!lastCompletedTrades.TryGetValue(instrumentName, out previous) || previous == null)
				return;
			trade.PreviousTradeExitTime = previous.ExitTime;
			trade.PreviousTradeDirection = previous.Direction;
			trade.PreviousTradeRealizedPnl = previous.RealizedPnl;
			trade.TimeSincePreviousTrade = previous.ExitTime == DateTime.MinValue ? TimeSpan.MaxValue : entryTime - previous.ExitTime;
			if (trade.TimeSincePreviousTrade < TimeSpan.Zero)
				trade.TimeSincePreviousTrade = TimeSpan.Zero;
		}

		private void RememberCompletedTrade(string instrumentName, OrcaRoundTripTrade trade)
		{
			if (string.IsNullOrWhiteSpace(instrumentName) || trade == null)
				return;
			lastCompletedTrades[instrumentName] = trade;
		}

		private static double PointValue(Execution execution)
		{
			try {
				if (execution != null && execution.Instrument != null && execution.Instrument.MasterInstrument != null)
					return execution.Instrument.MasterInstrument.PointValue;
			} catch { }
			return 1;
		}

		private static int SignedDelta(Execution execution)
		{
			if (execution == null || execution.Order == null)
				return 0;
			switch (execution.Order.OrderAction) {
				case OrderAction.Buy:
				case OrderAction.BuyToCover:
					return Math.Abs(execution.Quantity);
				case OrderAction.Sell:
				case OrderAction.SellShort:
					return -Math.Abs(execution.Quantity);
				default:
					return 0;
			}
		}

		private static string InstrumentName(Instrument instrument)
		{
			if (instrument == null)
				return string.Empty;
			return string.IsNullOrWhiteSpace(instrument.FullName) ? (instrument.MasterInstrument == null ? string.Empty : instrument.MasterInstrument.Name) : instrument.FullName;
		}
	}

	public sealed class OrcaTradeUpdate
	{
		public OrcaTradeUpdate()
		{
			CompletedTrades = new List<OrcaRoundTripTrade>();
		}

		public string InstrumentName { get; set; }
		public int CurrentSignedPosition { get; set; }
		public OrcaRoundTripTrade NewTradeStarted { get; set; }
		public OrcaRoundTripTrade IncreasedTrade { get; set; }
		public List<OrcaRoundTripTrade> CompletedTrades { get; private set; }
	}

	public sealed class OrcaRoundTripTrade
	{
		public string TradeId { get; set; }
		public string InstrumentName { get; set; }
		public DateTime EntryTime { get; set; }
		public DateTime ExitTime { get; set; }
		public string Direction { get; set; }
		public int Quantity { get; set; }
		public int OpenQuantity { get; set; }
		public double AverageEntry { get; set; }
		public double AverageExit { get; set; }
		public double RealizedPnl { get; set; }
		public int MaxPositionSize { get; set; }
		public bool WasIncreased { get; set; }
		public DateTime LastIncreaseTime { get; set; }
		public double LastIncreasePrice { get; set; }
		public double LastIncreaseUnrealizedPnl { get; set; }
		public bool LastIncreaseWasLosing { get; set; }
		public DateTime PreviousTradeExitTime { get; set; }
		public string PreviousTradeDirection { get; set; }
		public double PreviousTradeRealizedPnl { get; set; }
		public TimeSpan TimeSincePreviousTrade { get; set; }
	}

	[Serializable]
	public sealed class OrcaDisciplineRuleTemplate
	{
		public OrcaDisciplineRuleTemplate()
		{
			Rules = new List<OrcaDisciplineRuleConfig>();
		}

		public string Name { get; set; }
		public List<OrcaDisciplineRuleConfig> Rules { get; set; }

		public OrcaDisciplineRuleTemplate Clone()
		{
			return new OrcaDisciplineRuleTemplate {
				Name = Name,
				Rules = Rules == null ? new List<OrcaDisciplineRuleConfig>() : Rules.Select(r => r.Clone()).ToList()
			};
		}

		public static OrcaDisciplineRuleTemplate CreatePropFirmDefault()
		{
			OrcaDisciplineRuleTemplate template = new OrcaDisciplineRuleTemplate { Name = "Prop Firm Discipline" };
			template.Rules.Add(Config("cooldown", "TradeCooldown", "Minimum 5 minutes between new trades", "Minimum time between fresh flat-to-position trades.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MinimumMinutes", "5")));
			template.Rules.Add(Config("max-position", "MaxPositionSize", "Max position size: 2 minis / 20 micros", "Flags any instrument whose account position exceeds the mini-equivalent contract limit.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxContracts", "2", "MicroMultiplier", OrcaDisciplineConstants.DefaultMicroMultiplier, "MicroSymbols", OrcaDisciplineConstants.DefaultMicroSymbols)));
			template.Rules.Add(Config("max-trade-loss", "MaxLossPerTrade", "Max loss per trade: $300", "Uses gross round-trip realized P&L after the trade closes.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxLoss", "300")));
			template.Rules.Add(Config("max-session-loss", "MaxSessionLoss", "Max session loss: $600", "Uses selected account realized P&L from session start.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxLoss", "600")));
			template.Rules.Add(Config("max-trades", "MaxTradesPerSession", "Max trades per session: 5", "Counts completed round trips.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxTrades", "5")));
			template.Rules.Add(Config("max-loss-streak", "MaxConsecutiveLosses", "Max consecutive losses: 2", "Flags losing streaks after completed round trips.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxLosses", "2")));
			template.Rules.Add(Config("window", "AllowedTradingWindow", "Allowed trading window: 09:30 to 11:30", "Flags fresh trades outside the configured local time window.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Warning, Dict("Start", "09:30", "End", "11:30")));
			template.Rules.Add(Config("max-violations", "MaxRuleViolations", "Max rule violations: 3", "Flags when the session breaks too many rules.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxViolations", "3")));
			template.Rules.Add(Config("no-add-loser", "NoAddToLosingTrade", "No adding to losing trades", "Flags scale-ins when the open trade is currently losing.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict()));
			template.Rules.Add(Config("loss-reversal", "NoImmediateLossReversal", "No immediate reversal after loss", "Flags opposite-direction trades started too soon after a losing trade.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MinimumMinutes", "5")));
			template.Rules.Add(Manual("manual-setup", "Setup was valid."));
			template.Rules.Add(Manual("manual-chop", "I avoided chop."));
			template.Rules.Add(Manual("manual-sizing", "I followed position sizing."));
			template.Rules.Add(Manual("manual-revenge", "I was not revenge trading."));
			template.Rules.Add(Manual("manual-stop", "I stopped after breaking a major rule."));
			return template;
		}

		public static List<OrcaDisciplineRuleTemplate> CreateDefaults()
		{
			List<OrcaDisciplineRuleTemplate> templates = new List<OrcaDisciplineRuleTemplate>();
			templates.Add(CreatePropFirmDefault());

			OrcaDisciplineRuleTemplate manual = new OrcaDisciplineRuleTemplate { Name = "Manual Discipline" };
			manual.Rules.Add(Manual("manual-plan", "Was this trade part of my plan?"));
			manual.Rules.Add(Manual("manual-confirm", "Did I wait for confirmation?"));
			manual.Rules.Add(Manual("manual-tilt", "Was I emotionally tilted?"));
			manual.Rules.Add(Manual("manual-chop", "Did I trade through chop?"));
			manual.Rules.Add(Manual("manual-stop", "I stopped when I was supposed to stop."));
			templates.Add(manual);

			OrcaDisciplineRuleTemplate conservative = CreatePropFirmDefault();
			conservative.Name = "Conservative Scalping";
			SetParam(conservative, "cooldown", "MinimumMinutes", "5");
			SetParam(conservative, "max-position", "MaxContracts", "1");
			SetParam(conservative, "max-trades", "MaxTrades", "3");
			SetParam(conservative, "max-session-loss", "MaxLoss", "400");
			SetParam(conservative, "max-violations", "MaxViolations", "2");
			templates.Add(conservative);
			return templates;
		}

		private static OrcaDisciplineRuleConfig Manual(string id, string name)
		{
			return Config(id, "ManualChecklist", name, name, OrcaDisciplineRuleMode.Manual, OrcaDisciplineSeverity.Warning, new Dictionary<string, string>());
		}

		private static OrcaDisciplineRuleConfig Config(string id, string type, string name, string description, OrcaDisciplineRuleMode mode, OrcaDisciplineSeverity severity, Dictionary<string, string> parameters)
		{
			return new OrcaDisciplineRuleConfig {
				Id = id,
				Type = type,
				Name = name,
				Description = description,
				Enabled = true,
				Mode = mode,
				Severity = severity,
				Parameters = parameters
			};
		}

		private static Dictionary<string, string> Dict(params string[] values)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i + 1 < values.Length; i += 2)
				dictionary[values[i]] = values[i + 1];
			return dictionary;
		}

		private static void SetParam(OrcaDisciplineRuleTemplate template, string id, string key, string value)
		{
			OrcaDisciplineRuleConfig rule = template.Rules.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));
			if (rule == null)
				return;
			if (rule.Parameters == null)
				rule.Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			rule.Parameters[key] = value;
		}
	}

	[Serializable]
	public sealed class OrcaDisciplineRuleConfig
	{
		public OrcaDisciplineRuleConfig()
		{
			Enabled = true;
			Mode = OrcaDisciplineRuleMode.Automated;
			Severity = OrcaDisciplineSeverity.Warning;
			Parameters = new Dictionary<string, string>();
		}

		public string Id { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool Enabled { get; set; }
		public OrcaDisciplineRuleMode Mode { get; set; }
		public OrcaDisciplineSeverity Severity { get; set; }
		public Dictionary<string, string> Parameters { get; set; }

		public OrcaDisciplineRuleConfig Clone()
		{
			return new OrcaDisciplineRuleConfig {
				Id = Id,
				Type = Type,
				Name = Name,
				Description = Description,
				Enabled = Enabled,
				Mode = Mode,
				Severity = Severity,
				Parameters = Parameters == null ? new Dictionary<string, string>() : new Dictionary<string, string>(Parameters, StringComparer.OrdinalIgnoreCase)
			};
		}
	}

	[Serializable]
	public sealed class OrcaDisciplineSettings
	{
		public string LastAccountName { get; set; }
		public string LastTemplateName { get; set; }
		public string LastInstrumentFilter { get; set; }
	}

	public static class OrcaDisciplineStore
	{
		private static readonly object Sync = new object();
		private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

		public static List<OrcaDisciplineRuleTemplate> LoadTemplates()
		{
			lock (Sync) {
				try {
					EnsureRoot();
					if (File.Exists(TemplatesPath)) {
						string json = File.ReadAllText(TemplatesPath);
						List<OrcaDisciplineRuleTemplate> loaded = Serializer.Deserialize<List<OrcaDisciplineRuleTemplate>>(json);
						if (loaded != null && loaded.Count > 0) {
							if (MergeMissingDefaults(loaded))
								SaveTemplates(loaded);
							return loaded;
						}
					}
				} catch { }
				List<OrcaDisciplineRuleTemplate> defaults = OrcaDisciplineRuleTemplate.CreateDefaults();
				SaveTemplates(defaults);
				return defaults;
			}
		}

		public static void SaveTemplates(IEnumerable<OrcaDisciplineRuleTemplate> templates)
		{
			lock (Sync) {
				EnsureRoot();
				File.WriteAllText(TemplatesPath, Serializer.Serialize((templates ?? new OrcaDisciplineRuleTemplate[0]).ToList()));
			}
		}

		public static OrcaDisciplineSettings LoadSettings()
		{
			lock (Sync) {
				try {
					EnsureRoot();
					if (File.Exists(SettingsPath)) {
						OrcaDisciplineSettings loaded = Serializer.Deserialize<OrcaDisciplineSettings>(File.ReadAllText(SettingsPath));
						if (loaded != null)
							return loaded;
					}
				} catch { }
				return new OrcaDisciplineSettings { LastInstrumentFilter = OrcaDisciplineConstants.AllInstruments };
			}
		}

		public static void SaveSettings(OrcaDisciplineSettings settings)
		{
			lock (Sync) {
				EnsureRoot();
				File.WriteAllText(SettingsPath, Serializer.Serialize(settings ?? new OrcaDisciplineSettings()));
			}
		}

		public static string SaveSessionReport(OrcaDisciplineSessionReport report)
		{
			lock (Sync) {
				EnsureSessions();
				string account = SanitizeFileName(report == null ? "Unknown" : report.AccountName);
				string stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
				string path = Path.Combine(SessionsDirectory, stamp + "_" + account + ".json");
				File.WriteAllText(path, Serializer.Serialize(report));
				return path;
			}
		}

		public static string SaveViolationsCsv(OrcaDisciplineSession session)
		{
			lock (Sync) {
				EnsureSessions();
				string account = SanitizeFileName(session == null ? "Unknown" : session.AccountName);
				string stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
				string path = Path.Combine(SessionsDirectory, stamp + "_" + account + "_violations.csv");
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("Time,Rule,Severity,Account,Instrument,Message,Observed,Limit");
				if (session != null) {
					foreach (OrcaDisciplineViolation v in session.Violations.Reverse())
						sb.AppendLine(Csv(v.Timestamp.ToString("o", CultureInfo.InvariantCulture)) + "," + Csv(v.RuleName) + "," + Csv(v.Severity.ToString()) + "," + Csv(v.Account) + "," + Csv(v.Instrument) + "," + Csv(v.Message) + "," + Csv(v.ValueObserved) + "," + Csv(v.LimitValue));
				}
				File.WriteAllText(path, sb.ToString());
				return path;
			}
		}

		private static bool MergeMissingDefaults(List<OrcaDisciplineRuleTemplate> loaded)
		{
			if (loaded == null)
				return false;
			bool changed = false;
			foreach (OrcaDisciplineRuleTemplate defaultTemplate in OrcaDisciplineRuleTemplate.CreateDefaults()) {
				OrcaDisciplineRuleTemplate existing = loaded.FirstOrDefault(t => string.Equals(t.Name, defaultTemplate.Name, StringComparison.OrdinalIgnoreCase));
				if (existing == null) {
					loaded.Add(defaultTemplate.Clone());
					changed = true;
					continue;
				}
				if (existing.Rules == null)
					existing.Rules = new List<OrcaDisciplineRuleConfig>();
				foreach (OrcaDisciplineRuleConfig defaultRule in defaultTemplate.Rules) {
					OrcaDisciplineRuleConfig existingRule = existing.Rules.FirstOrDefault(r => string.Equals(r.Id, defaultRule.Id, StringComparison.OrdinalIgnoreCase));
					if (existingRule == null) {
						existing.Rules.Add(defaultRule.Clone());
						changed = true;
					} else if (MergeMissingParameters(existingRule, defaultRule)) {
						changed = true;
					}
				}
			}
			return changed;
		}

		private static bool MergeMissingParameters(OrcaDisciplineRuleConfig existingRule, OrcaDisciplineRuleConfig defaultRule)
		{
			if (existingRule == null || defaultRule == null || defaultRule.Parameters == null || defaultRule.Parameters.Count == 0)
				return false;
			if (existingRule.Parameters == null)
				existingRule.Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			bool changed = false;
			foreach (KeyValuePair<string, string> pair in defaultRule.Parameters) {
				if (!existingRule.Parameters.ContainsKey(pair.Key)) {
					existingRule.Parameters[pair.Key] = pair.Value;
					changed = true;
				}
			}
			return changed;
		}

		private static string Root
		{
			get {
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					"NinjaTrader 8",
					"OrcaDisciplineGuard");
			}
		}

		private static string TemplatesPath { get { return Path.Combine(Root, "Templates.json"); } }
		private static string SettingsPath { get { return Path.Combine(Root, "Settings.json"); } }
		private static string SessionsDirectory { get { return Path.Combine(Root, "Sessions"); } }

		private static void EnsureRoot()
		{
			if (!Directory.Exists(Root))
				Directory.CreateDirectory(Root);
		}

		private static void EnsureSessions()
		{
			EnsureRoot();
			if (!Directory.Exists(SessionsDirectory))
				Directory.CreateDirectory(SessionsDirectory);
		}

		private static string SanitizeFileName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				value = "Unknown";
			foreach (char c in Path.GetInvalidFileNameChars())
				value = value.Replace(c, '_');
			return value;
		}

		private static string Csv(string value)
		{
			if (value == null)
				value = string.Empty;
			return "\"" + value.Replace("\"", "\"\"") + "\"";
		}
	}

	[Serializable]
	public sealed class OrcaDisciplineSessionReport
	{
		public string AccountName { get; set; }
		public string TemplateName { get; set; }
		public string InstrumentFilter { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; }
		public double Score { get; set; }
		public string Grade { get; set; }
		public double SessionRealizedPnl { get; set; }
		public int CompletedTradeCount { get; set; }
		public int WinningTrades { get; set; }
		public int LosingTrades { get; set; }
		public int ConsecutiveLosses { get; set; }
		public int TotalRulesFollowed { get; set; }
		public int TotalViolations { get; set; }
		public int CriticalViolations { get; set; }
		public string Summary { get; set; }
		public List<OrcaDisciplineViolation> Violations { get; set; }
		public List<OrcaDisciplineRuleSnapshot> Rules { get; set; }
	}

	[Serializable]
	public sealed class OrcaDisciplineViolation
	{
		public DateTime Timestamp { get; set; }
		public string RuleId { get; set; }
		public string RuleName { get; set; }
		public OrcaDisciplineSeverity Severity { get; set; }
		public string Message { get; set; }
		public string Account { get; set; }
		public string Instrument { get; set; }
		public string TradeId { get; set; }
		public string ValueObserved { get; set; }
		public string LimitValue { get; set; }

		public string DisplayTime
		{
			get { return Timestamp == DateTime.MinValue ? string.Empty : Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture); }
		}
	}

	[Serializable]
	public sealed class OrcaDisciplineRuleSnapshot
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool Enabled { get; set; }
		public string Mode { get; set; }
		public string Severity { get; set; }
		public string Status { get; set; }
		public int ViolationCount { get; set; }
		public int FollowCount { get; set; }
		public DateTime LastViolationTime { get; set; }
		public string LastViolationMessage { get; set; }
		public string CurrentValueText { get; set; }
		public string LimitText { get; set; }
		public string ManualAction { get; set; }
		public string Notes { get; set; }
	}

	public enum OrcaDisciplineRuleMode
	{
		Manual,
		Automated,
		Hybrid
	}

	public enum OrcaDisciplineSeverity
	{
		Info,
		Warning,
		Major,
		Critical
	}

	public enum OrcaDisciplineRuleStatus
	{
		NotStarted,
		Passing,
		Warning,
		Violated,
		Disabled
	}

	public enum OrcaDisciplineSessionStatus
	{
		NotStarted,
		Active,
		Paused,
		Ended
	}

	public static class OrcaDisciplineScoring
	{
		public static int Penalty(OrcaDisciplineSeverity severity)
		{
			switch (severity) {
				case OrcaDisciplineSeverity.Info: return 1;
				case OrcaDisciplineSeverity.Warning: return 3;
				case OrcaDisciplineSeverity.Major: return 8;
				case OrcaDisciplineSeverity.Critical: return 15;
				default: return 0;
			}
		}

		public static string Grade(double score)
		{
			if (score >= 90) return "A";
			if (score >= 80) return "B";
			if (score >= 70) return "C";
			if (score >= 60) return "D";
			return "F";
		}
	}

	public static class OrcaDisciplineConstants
	{
		public const string AllInstruments = "All Instruments";
		public const string DefaultMicroMultiplier = "10";
		public const string DefaultMicroSymbols = "MNQ,MES,MYM,M2K,MGC,MCL";
	}

	public static class OrcaDisciplineDiagnostics
	{
		private static readonly object Sync = new object();

		public static void Write(string message)
		{
			try {
				lock (Sync) {
					string root = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
						"NinjaTrader 8",
						"OrcaDisciplineGuard");
					if (!Directory.Exists(root))
						Directory.CreateDirectory(root);
					File.AppendAllText(
						Path.Combine(root, "Diagnostics.log"),
						DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " " + (message ?? string.Empty) + Environment.NewLine);
				}
			} catch { }
		}
	}

	public static class OrcaManualActionValues
	{
		public const string Followed = "Followed";
		public const string Broken = "Broken";
		public const string NotApplicable = "N/A";
		public static readonly string[] Items = new[] { string.Empty, Followed, Broken, NotApplicable };
	}

	public abstract class OrcaDisciplineNotifyBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected bool Set<T>(ref T field, T value, string propertyName)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
				return false;
			field = value;
			Raise(propertyName);
			return true;
		}

		protected void Raise(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public sealed class OrcaDisciplineCommand : ICommand
	{
		private readonly Action execute;
		private readonly Func<bool> canExecute;

		public OrcaDisciplineCommand(Action execute, Func<bool> canExecute = null)
		{
			this.execute = execute;
			this.canExecute = canExecute;
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			return canExecute == null || canExecute();
		}

		public void Execute(object parameter)
		{
			if (execute != null)
				execute();
		}

		public static void RaiseCanExecuteChanged(ICommand command)
		{
			OrcaDisciplineCommand typed = command as OrcaDisciplineCommand;
			if (typed != null) {
				EventHandler handler = typed.CanExecuteChanged;
				if (handler != null)
					handler(typed, EventArgs.Empty);
			}
		}
	}

	public sealed class OrcaDisciplineStringVisibilityConverter : IValueConverter
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
