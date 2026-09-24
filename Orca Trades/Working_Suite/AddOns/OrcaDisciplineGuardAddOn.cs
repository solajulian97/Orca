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
		private static readonly object RuntimeSync = new object();
		private static OrcaDisciplineGuardEngine runtimeEngine;
		private NTMenuItem guardMenuItem;
		private NTMenuItem hostMenu;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults) {
				Description = "Orca Rulebook session tracking and rule accountability panel";
				Name = "Orca Rulebook";
			} else if (State == State.Terminated) {
				DisposeRuntime();
			}
		}

		protected override void OnWindowCreated(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || guardMenuItem != null)
				return;
			GetOrCreateRuntime(controlCenter.Dispatcher);

			hostMenu = controlCenter.FindFirst("ControlCenterMenuItemTools") as NTMenuItem
				?? controlCenter.FindFirst("toolsMenuItem") as NTMenuItem
				?? controlCenter.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
			if (hostMenu == null) {
				OrcaDisciplineDiagnostics.Write("Control Center menu host was not found; Orca Rulebook menu was not injected.");
				return;
			}
			OrcaDisciplineDiagnostics.Write("Orca Rulebook menu host found: " + (hostMenu.Name ?? string.Empty) + " / " + (hostMenu.Header == null ? string.Empty : hostMenu.Header.ToString()));

			guardMenuItem = new NTMenuItem {
				Header = "Orca Rulebook",
				Style = Application.Current == null ? null : Application.Current.TryFindResource("MainMenuItem") as Style
			};
			guardMenuItem.Click += OnMenuItemClick;
			hostMenu.Items.Add(guardMenuItem);
			OrcaDisciplineDiagnostics.Write("Orca Rulebook menu injected.");
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
				OrcaDisciplineDiagnostics.Write("Orca Rulebook menu item clicked.");
				Dispatcher requestDispatcher = guardMenuItem == null ? Dispatcher.CurrentDispatcher : guardMenuItem.Dispatcher;
				OrcaDisciplineGuardEngine engine = GetOrCreateRuntime(requestDispatcher);
				engine.InvokeOnDispatcher(() => OrcaDisciplineGuardWindow.ShowOrActivate(engine));
			} catch (Exception ex) {
				string message = "Orca Rulebook click handler failed: " + ex.Message;
				OrcaDisciplineDiagnostics.Write(message + Environment.NewLine + ex);
				MessageBox.Show(message, "Orca Rulebook", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private static OrcaDisciplineGuardEngine GetOrCreateRuntime(Dispatcher dispatcher)
		{
			OrcaDisciplineGuardEngine engine;
			bool created = false;
			lock (RuntimeSync) {
				if (runtimeEngine == null) {
					runtimeEngine = new OrcaDisciplineGuardEngine(dispatcher);
					created = true;
				}
				engine = runtimeEngine;
			}
			if (created)
				OrcaDisciplineDiagnostics.Write("Orca Rulebook background runtime started.");
			return engine;
		}

		private static void DisposeRuntime()
		{
			OrcaDisciplineGuardEngine engine;
			lock (RuntimeSync) {
				engine = runtimeEngine;
				runtimeEngine = null;
			}
			if (engine != null) {
				engine.Dispose();
				OrcaDisciplineDiagnostics.Write("Orca Rulebook background runtime stopped.");
			}
		}
	}

	internal enum OrcaRulebookButtonKind
	{
		Primary,
		Secondary,
		Destructive
	}

	internal static class OrcaRulebookChrome
	{
		public const string Window = "#FF1C1C1C";
		public const string Panel = "#FF242424";
		public const string Hover = "#FF2C2C2C";
		public const string Hairline = "#FF363636";
		public const string Text = "#FFF0F0F0";
		public const string Label = "#FFB8B8B8";
		public const string Muted = "#FF858585";
		public const string Accent = "#FF90BFF9";
		public const string PrimaryFill = "#FFF2F2F2";
		public const string PrimaryHover = "#FFFFFFFF";
		public const string PrimaryLabel = "#FF1C1C1C";
		public const string AlertFill = "#FF5A1721";
		public const string AlertBorder = "#FFE23A52";
		public const string AlertText = "#FFFFD7DE";
		public const string Positive = "#FF3DDC97";

		public static readonly FontFamily UiFont = new FontFamily("Segoe UI");
		public static readonly FontFamily NumberFont = new FontFamily("Consolas");

		public static Brush Brush(string color)
		{
			Brush brush = (Brush)new BrushConverter().ConvertFrom(color);
			if (brush.CanFreeze)
				brush.Freeze();
			return brush;
		}

		public static void Clip(Border border, double radius)
		{
			if (border == null)
				return;
			border.SizeChanged += (sender, args) => {
				Border box = (Border)sender;
				box.Clip = new RectangleGeometry(new Rect(0, 0, Math.Max(0, box.ActualWidth), Math.Max(0, box.ActualHeight)), radius, radius);
			};
		}

		public static Button CreateButton(string label, OrcaRulebookButtonKind kind)
		{
			string fill = PrimaryFill;
			string hover = PrimaryHover;
			string foreground = PrimaryLabel;
			string border = PrimaryFill;
			Thickness thickness = new Thickness(1);
			if (kind == OrcaRulebookButtonKind.Secondary) {
				fill = "#00FFFFFF";
				hover = Hover;
				foreground = Text;
				border = Hairline;
			} else if (kind == OrcaRulebookButtonKind.Destructive) {
				fill = "#00FFFFFF";
				hover = Hover;
				foreground = AlertText;
				border = AlertBorder;
			}
			Button button = new Button {
				Content = label,
				Height = 28,
				MinWidth = 72,
				Padding = new Thickness(12, 0, 12, 0),
				Margin = new Thickness(0, 0, 8, 0),
				FontFamily = UiFont,
				FontSize = 12,
				FontWeight = FontWeights.SemiBold,
				Foreground = Brush(foreground),
				Background = Brush(fill),
				BorderBrush = Brush(border),
				BorderThickness = thickness,
				FocusVisualStyle = null,
				Style = ButtonStyle(hover, kind == OrcaRulebookButtonKind.Primary)
			};
			return button;
		}

		public static ToggleButton CreateSegment(string label, bool selected)
		{
			ToggleButton button = new ToggleButton {
				Content = label,
				Height = 28,
				MinWidth = 76,
				Padding = new Thickness(12, 0, 12, 0),
				Margin = new Thickness(2, 0, 2, 0),
				FontFamily = UiFont,
				FontSize = 12,
				FontWeight = FontWeights.SemiBold,
				FocusVisualStyle = null,
				Style = SegmentStyle()
			};
			ApplySegment(button, selected);
			return button;
		}

		public static void ApplySegment(ToggleButton button, bool selected)
		{
			if (button == null)
				return;
			button.IsChecked = selected;
			button.Foreground = Brush(selected ? Text : Label);
			button.Background = Brush(selected ? Hover : "#00FFFFFF");
			button.BorderBrush = Brush(selected ? Accent : "#00FFFFFF");
			button.BorderThickness = new Thickness(0, 0, 0, selected ? 2 : 0);
		}

		public static void StyleInput(Control control)
		{
			if (control == null)
				return;
			if (control.MinHeight < 28)
				control.MinHeight = 28;
			control.FontFamily = UiFont;
			control.FontSize = 12;
			control.Foreground = Brush(Text);
			control.Background = Brush(Panel);
			control.BorderBrush = Brush(Hairline);
			control.BorderThickness = new Thickness(1);
			control.Padding = new Thickness(8, 4, 8, 4);
			TextBox textBox = control as TextBox;
			if (textBox != null) {
				textBox.Template = TextBoxTemplate(8);
				textBox.CaretBrush = Brush(Text);
			}
		}

		public static Border WrapField(Control control)
		{
			StyleInput(control);
			control.BorderThickness = new Thickness(0);
			control.Background = Brushes.Transparent;
			Border shell = new Border {
				CornerRadius = new CornerRadius(8),
				Background = Brush(Panel),
				BorderBrush = Brush(Hairline),
				BorderThickness = new Thickness(1),
				Child = control
			};
			Clip(shell, 8);
			control.GotKeyboardFocus += delegate { shell.BorderBrush = Brush(Accent); };
			control.LostKeyboardFocus += delegate { shell.BorderBrush = Brush(Hairline); };
			return shell;
		}

		public static ControlTemplate TextBoxTemplate(double radius)
		{
			FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
			border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
			border.SetValue(Border.SnapsToDevicePixelsProperty, true);
			border.SetBinding(Border.BackgroundProperty, Templated("Background"));
			border.SetBinding(Border.BorderBrushProperty, Templated("BorderBrush"));
			border.SetBinding(Border.BorderThicknessProperty, Templated("BorderThickness"));
			border.SetBinding(Border.PaddingProperty, Templated("Padding"));
			FrameworkElementFactory host = new FrameworkElementFactory(typeof(ScrollViewer), "PART_ContentHost");
			host.SetValue(UIElement.FocusableProperty, false);
			border.AppendChild(host);
			return new ControlTemplate(typeof(TextBox)) { VisualTree = border };
		}

		private static Style ButtonStyle(string hoverFill, bool matchBorderOnHover)
		{
			Style style = new Style(typeof(Button));
			style.Setters.Add(new Setter(FrameworkElement.OverridesDefaultStyleProperty, true));
			style.Setters.Add(new Setter(Control.TemplateProperty, RoundTemplate(typeof(Button), 12)));
			style.Setters.Add(new Setter(Control.SnapsToDevicePixelsProperty, true));
			MultiTrigger hover = new MultiTrigger();
			hover.Conditions.Add(new System.Windows.Condition(UIElement.IsMouseOverProperty, true));
			hover.Conditions.Add(new System.Windows.Condition(UIElement.IsEnabledProperty, true));
			hover.Setters.Add(new Setter(Control.BackgroundProperty, Brush(hoverFill)));
			if (matchBorderOnHover)
				hover.Setters.Add(new Setter(Control.BorderBrushProperty, Brush(hoverFill)));
			style.Triggers.Add(hover);
			MultiTrigger focus = new MultiTrigger();
			focus.Conditions.Add(new System.Windows.Condition(UIElement.IsKeyboardFocusedProperty, true));
			focus.Conditions.Add(new System.Windows.Condition(UIElement.IsEnabledProperty, true));
			focus.Setters.Add(new Setter(Control.BorderBrushProperty, Brush(Accent)));
			style.Triggers.Add(focus);
			Trigger disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
			disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.4));
			style.Triggers.Add(disabled);
			return style;
		}

		private static Style SegmentStyle()
		{
			Style style = new Style(typeof(ToggleButton));
			style.Setters.Add(new Setter(FrameworkElement.OverridesDefaultStyleProperty, true));
			style.Setters.Add(new Setter(Control.TemplateProperty, RoundTemplate(typeof(ToggleButton), 8)));
			style.Setters.Add(new Setter(Control.SnapsToDevicePixelsProperty, true));
			MultiTrigger hover = new MultiTrigger();
			hover.Conditions.Add(new System.Windows.Condition(UIElement.IsMouseOverProperty, true));
			hover.Conditions.Add(new System.Windows.Condition(UIElement.IsEnabledProperty, true));
			hover.Setters.Add(new Setter(Control.BackgroundProperty, Brush(Hover)));
			style.Triggers.Add(hover);
			Trigger disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
			disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.4));
			style.Triggers.Add(disabled);
			return style;
		}

		private static Binding Templated(string path)
		{
			return new Binding(path) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) };
		}

		private static ControlTemplate RoundTemplate(Type controlType, double radius)
		{
			FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
			border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
			border.SetValue(Border.SnapsToDevicePixelsProperty, true);
			border.SetBinding(Border.BackgroundProperty, Templated("Background"));
			border.SetBinding(Border.BorderBrushProperty, Templated("BorderBrush"));
			border.SetBinding(Border.BorderThicknessProperty, Templated("BorderThickness"));
			border.SetBinding(Border.PaddingProperty, Templated("Padding"));
			FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
			presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
			presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
			presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
			border.AppendChild(presenter);
			return new ControlTemplate(controlType) { VisualTree = border };
		}
	}

	public sealed class OrcaDisciplineGuardWindow : NTWindow
	{
		private static OrcaDisciplineGuardWindow instance;
		private readonly OrcaDisciplineGuardViewModel viewModel;
		private ContentControl viewHost;
		private ToggleButton sessionViewButton;
		private ToggleButton summaryViewButton;
		private FrameworkElement sessionView;
		private FrameworkElement summaryView;
		private bool isClosing;

		private OrcaDisciplineGuardWindow(OrcaDisciplineGuardEngine engine)
		{
			if (engine == null)
				throw new ArgumentNullException("engine");
			if (!engine.CheckDispatcherAccess())
				throw new InvalidOperationException("Orca Rulebook must be created on its runtime dispatcher.");

			Caption = "Orca Rulebook";
			Title = "Orca Rulebook";
			Width = 1180;
			Height = 760;
			MinWidth = 960;
			MinHeight = 620;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Background = Brush(OrcaRulebookChrome.Window);
			Foreground = Brush(OrcaRulebookChrome.Text);
			FontFamily = OrcaRulebookChrome.UiFont;

			viewModel = new OrcaDisciplineGuardViewModel(Dispatcher, engine);
			DataContext = viewModel;

			Grid root = new Grid { Background = Brush(OrcaRulebookChrome.Window) };
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			FrameworkElement header = BuildHeader();
			Grid.SetRow(header, 0);
			root.Children.Add(header);

			FrameworkElement tabs = BuildTabs();
			Grid.SetRow(tabs, 1);
			root.Children.Add(tabs);

			Content = root;
			Closing += OnClosing;
			Closed += OnClosed;
		}

		public static void ShowOrActivate(OrcaDisciplineGuardEngine engine)
		{
			try {
				OrcaDisciplineDiagnostics.Write("ShowOrActivate requested.");
				if (instance != null && instance.isClosing) {
					OrcaDisciplineDiagnostics.Write("ShowOrActivate ignored while the current window is closing.");
					return;
				}
				if (instance == null)
					instance = new OrcaDisciplineGuardWindow(engine);

				if (!instance.IsVisible)
					instance.Show();
				instance.Activate();
				OrcaDisciplineDiagnostics.Write("Orca Rulebook window opened.");
			} catch (Exception ex) {
				instance = null;
				string message = "Orca Rulebook could not open: " + ex.Message;
				OrcaDisciplineDiagnostics.Write(message + Environment.NewLine + ex);
				MessageBox.Show(message, "Orca Rulebook", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private FrameworkElement BuildHeader()
		{
			Grid header = new Grid { Margin = new Thickness(12, 12, 12, 8) };
			header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			StackPanel titleStack = new StackPanel { Orientation = Orientation.Vertical };
			titleStack.Children.Add(new TextBlock {
				Text = "Orca Rulebook",
				FontFamily = OrcaRulebookChrome.UiFont,
				FontSize = 18,
				FontWeight = FontWeights.SemiBold,
				Foreground = Brush(OrcaRulebookChrome.Text)
			});
			titleStack.Children.Add(new TextBlock {
				Text = "Account-specific rule tracking, session discipline grade, and violation journal",
				FontFamily = OrcaRulebookChrome.UiFont,
				FontSize = 12,
				Foreground = Brush(OrcaRulebookChrome.Muted),
				Margin = new Thickness(0, 4, 0, 0)
			});
			Grid.SetColumn(titleStack, 0);
			header.Children.Add(titleStack);

			TextBlock status = new TextBlock {
				MinWidth = 220,
				TextAlignment = TextAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				FontFamily = OrcaRulebookChrome.UiFont,
				Foreground = Brush(OrcaRulebookChrome.Text),
				FontSize = 13,
				FontWeight = FontWeights.SemiBold
			};
			status.SetBinding(TextBlock.TextProperty, new Binding("HeaderStatus"));
			Grid.SetColumn(status, 1);
			header.Children.Add(status);

			Border alert = new Border {
				Margin = new Thickness(0, 8, 0, 0),
				Padding = new Thickness(12, 8, 12, 8),
				CornerRadius = new CornerRadius(10),
				Background = Brush(OrcaRulebookChrome.AlertFill),
				BorderBrush = Brush(OrcaRulebookChrome.AlertBorder),
				BorderThickness = new Thickness(1)
			};
			alert.SetBinding(UIElement.VisibilityProperty, new Binding("AlertText") { Converter = new OrcaDisciplineStringVisibilityConverter() });
			TextBlock alertText = new TextBlock {
				Foreground = Brush(OrcaRulebookChrome.AlertText),
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
			Grid views = new Grid {
				Margin = new Thickness(12, 0, 12, 12),
				Background = Brush(OrcaRulebookChrome.Window)
			};
			views.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			views.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			sessionView = BuildSessionTab();
			summaryView = BuildSummaryTab();
			viewHost = new ContentControl { Content = sessionView };
			Grid.SetRow(viewHost, 0);
			views.Children.Add(viewHost);

			Border selectorShell = new Border {
				Margin = new Thickness(0, 8, 0, 0),
				Padding = new Thickness(2),
				HorizontalAlignment = HorizontalAlignment.Left,
				Background = Brush(OrcaRulebookChrome.Panel),
				BorderBrush = Brush(OrcaRulebookChrome.Hairline),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(10)
			};
			StackPanel selector = new StackPanel { Orientation = Orientation.Horizontal };
			sessionViewButton = ViewButton("Session", true);
			summaryViewButton = ViewButton("Summary", false);
			sessionViewButton.Click += (sender, args) => SelectView(true);
			summaryViewButton.Click += (sender, args) => SelectView(false);
			selector.Children.Add(sessionViewButton);
			selector.Children.Add(summaryViewButton);
			selectorShell.Child = selector;
			Grid.SetRow(selectorShell, 1);
			views.Children.Add(selectorShell);
			return views;
		}

		private ToggleButton ViewButton(string label, bool selected)
		{
			return OrcaRulebookChrome.CreateSegment(label, selected);
		}

		private void SelectView(bool showSession)
		{
			if (viewHost == null)
				return;
			viewHost.Content = showSession ? sessionView : summaryView;
			UpdateViewButton(sessionViewButton, showSession);
			UpdateViewButton(summaryViewButton, !showSession);
		}

		private void UpdateViewButton(ToggleButton button, bool selected)
		{
			if (button == null)
				return;
			OrcaRulebookChrome.ApplySegment(button, selected);
		}

		private FrameworkElement BuildSessionTab()
		{
			Grid tab = new Grid { Background = Brush(OrcaRulebookChrome.Window) };
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			tab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(210) });

			FrameworkElement controls = BuildControls();
			Grid.SetRow(controls, 0);
			tab.Children.Add(controls);

			FrameworkElement monitoring = BuildMonitoringBand();
			Grid.SetRow(monitoring, 1);
			tab.Children.Add(monitoring);

			FrameworkElement dashboard = BuildDashboard();
			Grid.SetRow(dashboard, 2);
			tab.Children.Add(dashboard);

			Border rulesShell = new Border {
				Margin = new Thickness(0, 0, 0, 8),
				BorderThickness = new Thickness(1),
				BorderBrush = Brush(OrcaRulebookChrome.Hairline),
				Background = Brush(OrcaRulebookChrome.Panel),
				CornerRadius = new CornerRadius(10),
				Child = BuildRulesGrid()
			};
			OrcaRulebookChrome.Clip(rulesShell, 10);
			Grid.SetRow(rulesShell, 3);
			tab.Children.Add(rulesShell);

			Border violationsShell = new Border {
				BorderThickness = new Thickness(1),
				BorderBrush = Brush(OrcaRulebookChrome.Hairline),
				Background = Brush(OrcaRulebookChrome.Panel),
				CornerRadius = new CornerRadius(10),
				Child = BuildViolationsGrid()
			};
			OrcaRulebookChrome.Clip(violationsShell, 10);
			Grid.SetRow(violationsShell, 4);
			tab.Children.Add(violationsShell);
			return tab;
		}

		private FrameworkElement BuildControls()
		{
			Grid controls = new Grid { Margin = new Thickness(0, 0, 0, 8) };
			controls.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			controls.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			for (int i = 0; i < 3; i++)
				controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			FrameworkElement account = BuildLabeledCombo("Account", "AccountNames", "SelectedAccountName", 190);
			Grid.SetColumn(account, 0);
			controls.Children.Add(account);

			FrameworkElement instrument = BuildLabeledCombo("Instrument", "InstrumentOptions", "SelectedInstrumentFilter", 190);
			Grid.SetColumn(instrument, 1);
			instrument.Margin = new Thickness(8, 0, 0, 0);
			controls.Children.Add(instrument);

			FrameworkElement template = BuildLabeledCombo("Template", "TemplateNames", "SelectedTemplateName", 220);
			Grid.SetColumn(template, 2);
			template.Margin = new Thickness(8, 0, 8, 0);
			controls.Children.Add(template);

			Grid actions = new Grid { Margin = new Thickness(0, 8, 0, 0) };
			actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			WrapPanel sessionActions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Left };
			AddToolbarButton(sessionActions, "Refresh", "RefreshAccountsCommand", OrcaRulebookButtonKind.Secondary);
			AddToolbarButton(sessionActions, "Start Session", "StartCommand", OrcaRulebookButtonKind.Primary);
			AddToolbarButton(sessionActions, "Pause / Resume", "PauseCommand", OrcaRulebookButtonKind.Secondary);
			AddToolbarButton(sessionActions, "End Session", "EndCommand", OrcaRulebookButtonKind.Secondary);
			AddToolbarButton(sessionActions, "Reset", "ResetCommand", OrcaRulebookButtonKind.Secondary);
			Grid.SetColumn(sessionActions, 0);
			actions.Children.Add(sessionActions);

			WrapPanel ruleActions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
			AddToolbarButton(ruleActions, "Add Rule", "AddRuleCommand", OrcaRulebookButtonKind.Secondary);
			AddToolbarButton(ruleActions, "Delete Rule", "DeleteRuleCommand", OrcaRulebookButtonKind.Destructive);
			AddToolbarButton(ruleActions, "Save Template", "SaveTemplateCommand", OrcaRulebookButtonKind.Primary);
			AddToolbarButton(ruleActions, "Clone Template", "CloneTemplateCommand", OrcaRulebookButtonKind.Secondary);
			Grid.SetColumn(ruleActions, 1);
			actions.Children.Add(ruleActions);

			Grid.SetRow(actions, 1);
			Grid.SetColumnSpan(actions, 3);
			controls.Children.Add(actions);

			return controls;
		}

		private FrameworkElement BuildLabeledCombo(string label, string itemsPath, string selectedPath, double minWidth)
		{
			StackPanel stack = new StackPanel { Orientation = Orientation.Vertical };
			stack.Children.Add(new TextBlock {
				Text = label,
				FontFamily = OrcaRulebookChrome.UiFont,
				Foreground = Brush(OrcaRulebookChrome.Muted),
				FontSize = 11,
				Margin = new Thickness(0, 0, 0, 4)
			});
			ComboBox combo = new ComboBox {
				MinWidth = minWidth,
				Height = 28,
				IsEditable = false
			};
			combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemsPath));
			combo.SetBinding(Selector.SelectedItemProperty, new Binding(selectedPath) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
			stack.Children.Add(OrcaRulebookChrome.WrapField(combo));
			return stack;
		}

		private FrameworkElement BuildMonitoringBand()
		{
			Border band = new Border {
				Margin = new Thickness(0, 0, 0, 8),
				Padding = new Thickness(12),
				CornerRadius = new CornerRadius(10),
				Background = Brush(OrcaRulebookChrome.Panel),
				BorderBrush = Brush(OrcaRulebookChrome.Hairline),
				BorderThickness = new Thickness(1)
			};
			Grid grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			Border stateBadge = new Border {
				MinWidth = 76,
				Height = 28,
				Padding = new Thickness(12, 0, 12, 0),
				CornerRadius = new CornerRadius(8),
				Background = Brush(OrcaRulebookChrome.Window),
				BorderThickness = new Thickness(1)
			};
			Binding stateBrushBinding = new Binding("MonitoringStateText") { Converter = new OrcaDisciplineMonitoringBrushConverter(), ConverterParameter = "border" };
			stateBadge.SetBinding(Border.BorderBrushProperty, stateBrushBinding);
			TextBlock stateText = new TextBlock {
				TextAlignment = TextAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				FontFamily = OrcaRulebookChrome.UiFont,
				FontSize = 11,
				FontWeight = FontWeights.SemiBold
			};
			stateText.SetBinding(TextBlock.TextProperty, new Binding("MonitoringStateText"));
			stateText.SetBinding(TextBlock.ForegroundProperty, new Binding("MonitoringStateText") { Converter = new OrcaDisciplineMonitoringBrushConverter(), ConverterParameter = "text" });
			stateBadge.Child = stateText;
			Grid.SetColumn(stateBadge, 0);
			grid.Children.Add(stateBadge);

			TextBlock detail = new TextBlock {
				Margin = new Thickness(12, 0, 12, 0),
				VerticalAlignment = VerticalAlignment.Center,
				FontFamily = OrcaRulebookChrome.UiFont,
				Foreground = Brush(OrcaRulebookChrome.Label),
				FontSize = 12,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			detail.SetBinding(TextBlock.TextProperty, new Binding("MonitoringDetailText"));
			Grid.SetColumn(detail, 1);
			grid.Children.Add(detail);

			TextBlock eventCount = new TextBlock {
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = Brush(OrcaRulebookChrome.Muted),
				FontFamily = OrcaRulebookChrome.NumberFont,
				FontSize = 12
			};
			eventCount.SetBinding(TextBlock.TextProperty, new Binding("MonitoringEventCountText"));
			Grid.SetColumn(eventCount, 2);
			grid.Children.Add(eventCount);

			band.Child = grid;
			return band;
		}

		private FrameworkElement BuildDashboard()
		{
			UniformGrid grid = new UniformGrid { Columns = 8 };
			grid.Children.Add(MetricCard("Grade", "Grade", 18));
			grid.Children.Add(MetricCard("Score", "ScoreText", 16, "EnabledWeightText"));
			grid.Children.Add(MetricCard("Session P&L", "SessionPnlText", 16));
			grid.Children.Add(MetricCard("Trades", "TradeCountText", 16));
			grid.Children.Add(MetricCard("Violations", "ViolationCountText", 16));
			grid.Children.Add(MetricCard("Cooldown", "CooldownText", 16));
			grid.Children.Add(MetricCard("Position", "CurrentPositionSizeText", 16));
			grid.Children.Add(MetricCard("Loss Streak", "ConsecutiveLossesText", 16));
			TextBlock breakdown = new TextBlock {
				Margin = new Thickness(0, 4, 0, 0),
				FontFamily = OrcaRulebookChrome.UiFont,
				FontSize = 12,
				Foreground = Brush(OrcaRulebookChrome.Label),
				TextWrapping = TextWrapping.Wrap
			};
			breakdown.SetBinding(TextBlock.TextProperty, new Binding("ScoreBreakdown"));
			StackPanel panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
			panel.Children.Add(grid);
			panel.Children.Add(breakdown);
			return panel;
		}

		private FrameworkElement MetricCard(string label, string valuePath, double valueSize)
		{
			return MetricCard(label, valuePath, valueSize, null);
		}

		private FrameworkElement MetricCard(string label, string valuePath, double valueSize, string captionPath)
		{
			Border card = new Border {
				Margin = new Thickness(0, 0, 8, 0),
				Padding = new Thickness(12, 8, 12, 8),
				CornerRadius = new CornerRadius(10),
				BorderBrush = Brush(OrcaRulebookChrome.Hairline),
				BorderThickness = new Thickness(1),
				Background = Brush(OrcaRulebookChrome.Panel)
			};
			StackPanel stack = new StackPanel { Orientation = Orientation.Vertical };
			stack.Children.Add(new TextBlock {
				Text = label,
				FontFamily = OrcaRulebookChrome.UiFont,
				Foreground = Brush(OrcaRulebookChrome.Muted),
				FontSize = 11
			});
			TextBlock value = new TextBlock {
				Foreground = Brush(OrcaRulebookChrome.Text),
				FontFamily = OrcaRulebookChrome.NumberFont,
				FontSize = valueSize,
				FontWeight = FontWeights.SemiBold,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			value.SetBinding(TextBlock.TextProperty, new Binding(valuePath));
			if (valuePath == "Grade")
				value.SetBinding(TextBlock.ForegroundProperty, new Binding(valuePath) { Converter = new OrcaRulebookGradeBrushConverter() });
			stack.Children.Add(value);
			if (!string.IsNullOrEmpty(captionPath)) {
				TextBlock caption = new TextBlock {
					Margin = new Thickness(0, 2, 0, 0),
					FontFamily = OrcaRulebookChrome.UiFont,
					Foreground = Brush(OrcaRulebookChrome.Muted),
					FontSize = 11
				};
				caption.SetBinding(TextBlock.TextProperty, new Binding(captionPath));
				stack.Children.Add(caption);
			}
			card.Child = stack;
			return card;
		}

		private DataGrid BuildRulesGrid()
		{
			DataGrid grid = BuildBaseGrid();
			grid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Rules"));
			grid.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedRule") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
			grid.BeginningEdit += OnRulesGridBeginningEdit;
			grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Enabled", Binding = new Binding("Enabled") { Mode = BindingMode.TwoWay }, Width = new DataGridLength(70) });
			grid.Columns.Add(BuildRuleNameColumn());
			grid.Columns.Add(TextColumn("Mode", "Mode", 90));
			grid.Columns.Add(TextColumn("Status", "Status", 100));
			grid.Columns.Add(EditableTextColumn("Parameter / Limit", "ParameterText", 170));
			grid.Columns.Add(EditableTextColumn("Weight", "WeightText", 70));
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
				RowHeight = 28,
				BorderThickness = new Thickness(0),
				Background = Brush(OrcaRulebookChrome.Panel),
				Foreground = Brush(OrcaRulebookChrome.Text),
				BorderBrush = Brush(OrcaRulebookChrome.Hairline),
				HorizontalGridLinesBrush = Brush(OrcaRulebookChrome.Hairline),
				VerticalGridLinesBrush = Brush(OrcaRulebookChrome.Hairline),
				AlternatingRowBackground = Brush(OrcaRulebookChrome.Window),
				RowBackground = Brush(OrcaRulebookChrome.Panel),
				FontFamily = OrcaRulebookChrome.UiFont,
				FontSize = 12,
				ColumnHeaderStyle = BuildHeaderStyle(),
				RowStyle = BuildRowStyle(),
				CellStyle = BuildCellStyle()
			};
			return grid;
		}

		private DataGridTemplateColumn BuildManualActionColumn()
		{
			FrameworkElementFactory combo = new FrameworkElementFactory(typeof(ComboBox));
			combo.SetValue(FrameworkElement.MinWidthProperty, 112.0);
			combo.SetValue(FrameworkElement.HeightProperty, 24.0);
			combo.SetValue(Control.FontFamilyProperty, OrcaRulebookChrome.UiFont);
			combo.SetValue(Control.FontSizeProperty, 12.0);
			combo.SetValue(Control.ForegroundProperty, Brush(OrcaRulebookChrome.Text));
			combo.SetValue(Control.BackgroundProperty, Brush(OrcaRulebookChrome.Window));
			combo.SetValue(Control.BorderBrushProperty, Brush(OrcaRulebookChrome.Hairline));
			combo.SetValue(ComboBox.ItemsSourceProperty, OrcaManualActionValues.Items);
			combo.SetBinding(Selector.SelectedItemProperty, new Binding("ManualAction") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
			combo.SetBinding(UIElement.IsEnabledProperty, new Binding("IsManual"));
			return new DataGridTemplateColumn {
				Header = "Manual Action",
				Width = new DataGridLength(132),
				CellTemplate = new DataTemplate { VisualTree = combo }
			};
		}

		private DataGridTextColumn BuildRuleNameColumn()
		{
			return new DataGridTextColumn {
				Header = "Rule Name",
				Binding = new Binding("Name"),
				Width = new DataGridLength(190),
				ElementStyle = BuildCellTextStyle(),
				EditingElementStyle = BuildTextBoxStyle()
			};
		}

		private void OnRulesGridBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
		{
			if (e.Column == null || e.Row == null)
				return;
			if (!string.Equals(e.Column.Header as string, "Rule Name", StringComparison.Ordinal))
				return;
			OrcaDisciplineRule rule = e.Row.Item as OrcaDisciplineRule;
			if (rule == null || !rule.CanRename)
				e.Cancel = true;
		}

		private DataGridTextColumn TextColumn(string header, string path, double width)
		{
			return new DataGridTextColumn {
				Header = header,
				Binding = new Binding(path),
				IsReadOnly = true,
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

		private Style BuildRowStyle()
		{
			Style style = new Style(typeof(DataGridRow));
			MultiTrigger hover = new MultiTrigger();
			hover.Conditions.Add(new System.Windows.Condition(UIElement.IsMouseOverProperty, true));
			hover.Conditions.Add(new System.Windows.Condition(DataGridRow.IsSelectedProperty, false));
			hover.Setters.Add(new Setter(Control.BackgroundProperty, Brush(OrcaRulebookChrome.Hover)));
			style.Triggers.Add(hover);
			Trigger selected = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
			selected.Setters.Add(new Setter(Control.BackgroundProperty, Brush(OrcaRulebookChrome.Hover)));
			selected.Setters.Add(new Setter(Control.BorderBrushProperty, Brush(OrcaRulebookChrome.Accent)));
			selected.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(2, 0, 0, 0)));
			selected.Setters.Add(new Setter(Control.ForegroundProperty, Brush(OrcaRulebookChrome.Text)));
			style.Triggers.Add(selected);
			return style;
		}

		private Style BuildCellStyle()
		{
			Style style = new Style(typeof(DataGridCell));
			style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
			style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
			style.Setters.Add(new Setter(Control.ForegroundProperty, Brush(OrcaRulebookChrome.Text)));
			style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
			Trigger selected = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
			selected.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
			selected.Setters.Add(new Setter(Control.ForegroundProperty, Brush(OrcaRulebookChrome.Text)));
			selected.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
			style.Triggers.Add(selected);
			return style;
		}

		private Style BuildHeaderStyle()
		{
			Style style = new Style(typeof(DataGridColumnHeader));
			style.Setters.Add(new Setter(Control.BackgroundProperty, Brush(OrcaRulebookChrome.Panel)));
			style.Setters.Add(new Setter(Control.ForegroundProperty, Brush(OrcaRulebookChrome.Muted)));
			style.Setters.Add(new Setter(Control.FontFamilyProperty, OrcaRulebookChrome.UiFont));
			style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
			style.Setters.Add(new Setter(Control.FontSizeProperty, 11.0));
			style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 4, 10, 4)));
			style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush(OrcaRulebookChrome.Hairline)));
			style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
			style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
			return style;
		}

		private Style BuildCellTextStyle()
		{
			Style style = new Style(typeof(TextBlock));
			style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brush(OrcaRulebookChrome.Text)));
			style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, OrcaRulebookChrome.UiFont));
			style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 12.0));
			style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(10, 0, 10, 0)));
			style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
			style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
			return style;
		}

		private Style BuildTextBoxStyle()
		{
			Style style = new Style(typeof(TextBox));
			style.Setters.Add(new Setter(Control.ForegroundProperty, Brush(OrcaRulebookChrome.Text)));
			style.Setters.Add(new Setter(Control.BackgroundProperty, Brush(OrcaRulebookChrome.Window)));
			style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush(OrcaRulebookChrome.Hairline)));
			style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
			style.Setters.Add(new Setter(Control.FontFamilyProperty, OrcaRulebookChrome.UiFont));
			style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
			style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 2, 8, 2)));
			style.Setters.Add(new Setter(Control.TemplateProperty, OrcaRulebookChrome.TextBoxTemplate(8)));
			return style;
		}

		private FrameworkElement BuildSummaryTab()
		{
			Grid tab = new Grid { Background = Brush(OrcaRulebookChrome.Window) };
			tab.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			tab.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			StackPanel commands = new StackPanel {
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				Margin = new Thickness(0, 0, 0, 8)
			};
			commands.Children.Add(CommandButton("Copy Summary", "CopySummaryCommand", OrcaRulebookButtonKind.Secondary));
			commands.Children.Add(CommandButton("Export Session JSON", "ExportSessionCommand", OrcaRulebookButtonKind.Secondary));
			commands.Children.Add(CommandButton("Export Violations CSV", "ExportViolationsCommand", OrcaRulebookButtonKind.Secondary));
			Grid.SetRow(commands, 0);
			tab.Children.Add(commands);

			TextBox summary = new TextBox {
				AcceptsReturn = true,
				IsReadOnly = true,
				TextWrapping = TextWrapping.Wrap,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				Foreground = Brush(OrcaRulebookChrome.Text),
				Background = Brush(OrcaRulebookChrome.Panel),
				BorderBrush = Brush(OrcaRulebookChrome.Hairline),
				BorderThickness = new Thickness(1),
				Padding = new Thickness(12),
				FontFamily = OrcaRulebookChrome.NumberFont,
				FontSize = 12,
				Template = OrcaRulebookChrome.TextBoxTemplate(10)
			};
			summary.SetBinding(TextBox.TextProperty, new Binding("SessionSummary") { Mode = BindingMode.OneWay });
			Grid.SetRow(summary, 1);
			tab.Children.Add(summary);
			return tab;
		}

		private void AddToolbarButton(Panel panel, string label, string commandPath, OrcaRulebookButtonKind kind)
		{
			panel.Children.Add(CommandButton(label, commandPath, kind));
		}

		private Button CommandButton(string label, string commandPath, OrcaRulebookButtonKind kind)
		{
			Button button = OrcaRulebookChrome.CreateButton(label, kind);
			button.SetBinding(Button.CommandProperty, new Binding(commandPath));
			return button;
		}

		private void OnClosing(object sender, CancelEventArgs e)
		{
			isClosing = !e.Cancel;
		}

		private void OnClosed(object sender, EventArgs e)
		{
			viewModel.Dispose();
			instance = null;
			OrcaDisciplineDiagnostics.Write("Orca Rulebook window closed; background runtime remains active.");
		}

		private static Brush Brush(string color)
		{
			return OrcaRulebookChrome.Brush(color);
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
			Background = OrcaRulebookChrome.Brush(OrcaRulebookChrome.Window);
			Foreground = OrcaRulebookChrome.Brush(OrcaRulebookChrome.Text);
			FontFamily = OrcaRulebookChrome.UiFont;

			typeCombo = new ComboBox();
			nameBox = new TextBox();
			descriptionBox = new TextBox { MinHeight = 64, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
			parametersBox = new TextBox();
			severityCombo = new ComboBox();
			enabledBox = new CheckBox {
				Content = "Enabled",
				IsChecked = true,
				Margin = new Thickness(0, 4, 0, 8),
				Foreground = OrcaRulebookChrome.Brush(OrcaRulebookChrome.Text),
				FontFamily = OrcaRulebookChrome.UiFont,
				FontSize = 12
			};

			foreach (OrcaDisciplineRuleTypeChoice choice in OrcaDisciplineRuleTypeChoice.CreateDefaults())
				typeCombo.Items.Add(choice);
			foreach (object severity in Enum.GetValues(typeof(OrcaDisciplineSeverity)))
				severityCombo.Items.Add(severity);
			typeCombo.SelectionChanged += OnTypeSelectionChanged;

			Grid root = new Grid { Margin = new Thickness(12) };
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
			Button cancel = OrcaRulebookChrome.CreateButton("Cancel", OrcaRulebookButtonKind.Secondary);
			cancel.IsCancel = true;
			Button add = OrcaRulebookChrome.CreateButton("Add Rule", OrcaRulebookButtonKind.Primary);
			add.Margin = new Thickness(0);
			add.IsDefault = true;
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
			StackPanel stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 8) };
			stack.Children.Add(new TextBlock {
				Text = label,
				Foreground = OrcaRulebookChrome.Brush(OrcaRulebookChrome.Muted),
				FontFamily = OrcaRulebookChrome.UiFont,
				FontSize = 11,
				Margin = new Thickness(0, 0, 0, 4)
			});
			control.Margin = new Thickness(0);
			stack.Children.Add(OrcaRulebookChrome.WrapField(control));
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
				MessageBox.Show(this, "Give the rule a name first.", "Orca Rulebook", MessageBoxButton.OK, MessageBoxImage.Warning);
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
					MessageBox.Show(this, "Use Key=Value pairs for parameters, separated by semicolons.", "Orca Rulebook", MessageBoxButton.OK, MessageBoxImage.Warning);
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
			yield return Choice("TradeCooldown", "Automated - Trade cooldown", "Minimum time between new trades", "Minimum time between fresh flat-to-position trades.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MinimumMinutes", "5"));
			yield return Choice("MaxPositionSize", "Automated - Max position size", "Max position size", "Flags any instrument whose account position exceeds the mini-equivalent contract limit.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxContracts", "2", "MicroMultiplier", OrcaDisciplineConstants.DefaultMicroMultiplier, "MicroSymbols", OrcaDisciplineConstants.DefaultMicroSymbols));
			yield return Choice("MaxLossPerTrade", "Automated - Max loss per trade", "Max loss per trade", "Uses gross round-trip realized P&L after the trade closes.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxLoss", "300"));
			yield return Choice("MaxSessionLoss", "Automated - Max session loss", "Max session loss", "Uses selected account realized P&L from session start.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxLoss", "600"));
			yield return Choice("MaxTradesPerSession", "Automated - Max trades per session", "Max trades per session", "Counts completed round trips.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxTrades", "5"));
			yield return Choice("MaxConsecutiveLosses", "Automated - Max consecutive losses", "Max consecutive losses", "Flags losing streaks after completed round trips.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxLosses", "2"));
			yield return Choice("AllowedTradingWindow", "Automated - Allowed trading window", "Allowed trading window", "Flags fresh trades outside the configured local time window.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Warning, Dict("Start", "09:30", "End", "11:30"));
			yield return Choice("MaxRuleViolations", "Automated - Max rule violations", "Max rule violations", "Flags when the session breaks too many rules.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxViolations", "3"));
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

		public OrcaDisciplineGuardViewModel(Dispatcher dispatcher, OrcaDisciplineGuardEngine engine)
		{
			if (engine == null)
				throw new ArgumentNullException("engine");
			this.dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
			this.engine = engine;
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

			RefreshAccountsCore(false);
			if (engine.Session != null) {
				selectedAccountName = engine.Session.AccountName;
				selectedTemplateName = engine.Session.TemplateName;
				selectedInstrumentFilter = engine.Session.InstrumentFilter;
				if (!string.IsNullOrWhiteSpace(selectedAccountName) && !AccountNames.Contains(selectedAccountName))
					AccountNames.Add(selectedAccountName);
				if (!string.IsNullOrWhiteSpace(selectedTemplateName) && !TemplateNames.Contains(selectedTemplateName)) {
					OrcaDisciplineRuleTemplate runtimeTemplate = engine.Session.CreateTemplateSnapshot(selectedTemplateName);
					Templates.Add(runtimeTemplate);
					TemplateNames.Add(runtimeTemplate.Name);
				}
				if (!string.IsNullOrWhiteSpace(selectedInstrumentFilter) && !InstrumentOptions.Contains(selectedInstrumentFilter))
					InstrumentOptions.Add(selectedInstrumentFilter);
			} else {
				selectedTemplateName = ResolveInitialTemplate(settings.LastTemplateName);
				selectedAccountName = !string.IsNullOrWhiteSpace(settings.LastAccountName) && AccountNames.Contains(settings.LastAccountName)
					? settings.LastAccountName
					: AccountNames.FirstOrDefault();
				if (string.IsNullOrWhiteSpace(selectedTemplateName))
					selectedTemplateName = TemplateNames.FirstOrDefault();
				RebuildSessionForSelection();
			}
			Raise("SelectedAccountName");
			Raise("SelectedTemplateName");
			Raise("SelectedInstrumentFilter");
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

		public string MonitoringStateText
		{
			get {
				if (engine.Session == null || string.IsNullOrWhiteSpace(engine.Session.AccountName))
					return "OFFLINE";
				if (!engine.IsSubscribed || !engine.IsAccountConnected)
					return "OFFLINE";
				switch (engine.Session.Status) {
					case OrcaDisciplineSessionStatus.Active: return "ARMED";
					case OrcaDisciplineSessionStatus.Paused: return "PAUSED";
					case OrcaDisciplineSessionStatus.Ended: return "ENDED";
					default: return "READY";
				}
			}
		}

		public string MonitoringDetailText
		{
			get {
				if (engine.Session == null)
					return "Runtime available | No session loaded";
				if (string.IsNullOrWhiteSpace(engine.Session.AccountName))
					return "No account selected";
				if (!engine.IsSubscribed)
					return "Account feed is not attached";
				if (!engine.IsAccountConnected)
					return "Selected account is disconnected";
				string heartbeat = engine.LastHeartbeatTime == DateTime.MinValue
					? "Runtime starting"
					: (DateTime.Now - engine.LastHeartbeatTime).TotalSeconds <= 3
						? "Runtime heartbeat healthy"
						: "Runtime heartbeat delayed";
				string activity = engine.LastAccountEventTime == DateTime.MinValue
					? "Waiting for account activity"
					: "Last account event " + engine.LastAccountEventTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
				return heartbeat + " | " + activity;
			}
		}

		public string MonitoringEventCountText
		{
			get { return engine.AccountEventCount.ToString("N0", CultureInfo.InvariantCulture) + " events"; }
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
		public string ScoreBreakdown { get { return engine.Session == null ? string.Empty : engine.Session.ScoreBreakdown; } }
		public string EnabledWeightText { get { return engine.Session == null ? "Weight 0" : engine.Session.EnabledWeightText; } }
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
		}

		private void RefreshAccounts()
		{
			RefreshAccountsCore(true);
		}

		private void RefreshAccountsCore(bool rebuildSession)
		{
			try {
				string previous = selectedAccountName;
				string runtimeAccount = engine.Session == null ? string.Empty : engine.Session.AccountName;
				AccountNames.Clear();
				foreach (Account account in Account.All.Where(IsCurrentTradingAccount).OrderBy(a => a.Name))
					AccountNames.Add(account.Name);
				if (!string.IsNullOrWhiteSpace(previous) && AccountNames.Contains(previous))
					selectedAccountName = previous;
				else if (!string.IsNullOrWhiteSpace(runtimeAccount)) {
					selectedAccountName = runtimeAccount;
					if (!AccountNames.Contains(runtimeAccount))
						AccountNames.Add(runtimeAccount);
				} else
					selectedAccountName = AccountNames.FirstOrDefault();
				Raise("SelectedAccountName");
				bool selectionChanged = !string.Equals(previous, selectedAccountName, StringComparison.OrdinalIgnoreCase);
				if (rebuildSession && (engine.Session == null || selectionChanged))
					RebuildSessionForSelection();
				else
					RaiseDashboard();
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
				AlertText = "Orca Rulebook could not start: " + ex.Message;
			}
		}

		private bool CanStartSession()
		{
			return engine.Session != null
				&& !string.IsNullOrWhiteSpace(SelectedAccountName)
				&& (engine.Session.Status == OrcaDisciplineSessionStatus.NotStarted
					|| engine.Session.Status == OrcaDisciplineSessionStatus.Ended);
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
				AlertText = "Session JSON saved: " + path + SaveRulebookLedgerNote(engine.Session);
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
				MessageBoxResult result = MessageBox.Show("Delete rule '" + ruleName + "' from the current template draft?", "Orca Rulebook", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
			Raise("MonitoringStateText");
			Raise("MonitoringDetailText");
			Raise("MonitoringEventCountText");
			Raise("Grade");
			Raise("ScoreText");
			Raise("ScoreBreakdown");
			Raise("EnabledWeightText");
			Raise("SessionPnlText");
			Raise("TradeCountText");
			Raise("ViolationCountText");
			Raise("CooldownText");
			Raise("CurrentPositionSizeText");
			Raise("ConsecutiveLossesText");
			Raise("SessionSummary");
		}

		private static string SaveRulebookLedgerNote(OrcaDisciplineSession session)
		{
			try {
				if (session == null || session.Ledger == null)
					return string.Empty;
				string ledgerPath = OrcaDisciplineStore.SaveRulebookLedger(session.Ledger);
				return " Rulebook ledger: " + ledgerPath;
			} catch (Exception ex) {
				return " Rulebook ledger was not saved: " + ex.Message;
			}
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
				AlertText = "Archived active session before " + reason + ": " + path + SaveRulebookLedgerNote(engine.Session);
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
		private bool disposed;
		private DateTime lastAccountEventTime;
		private DateTime lastHeartbeatTime;
		private long accountEventCount;

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
		public bool IsSubscribed { get { return subscribed; } }
		public DateTime LastAccountEventTime { get { return lastAccountEventTime; } }
		public DateTime LastHeartbeatTime { get { return lastHeartbeatTime; } }
		public long AccountEventCount { get { return accountEventCount; } }
		public bool CheckDispatcherAccess() { return dispatcher.CheckAccess(); }

		public void InvokeOnDispatcher(Action action)
		{
			if (action == null || disposed)
				return;
			if (!dispatcher.CheckAccess())
				OrcaDisciplineDiagnostics.Write("Orca Rulebook window request marshaled to the runtime dispatcher.");
			RunOnUi(action);
		}

		public bool IsAccountConnected
		{
			get {
				try {
					return account != null
						&& (account.ConnectionStatus == ConnectionStatus.Connected
							|| (account.Connection != null && account.Connection.Status == ConnectionStatus.Connected));
				} catch {
					return false;
				}
			}
		}

		public void SelectAccount(Account selectedAccount, OrcaDisciplineRuleTemplate template, string instrumentFilter)
		{
			Unsubscribe();
			account = selectedAccount;
			lastAccountEventTime = DateTime.MinValue;
			accountEventCount = 0;
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
			if (disposed)
				return;
			if (!dispatcher.CheckAccess()) {
				try {
					dispatcher.Invoke(new Action(Dispose));
					return;
				} catch { }
			}
			disposed = true;
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
				if (Session == null || !IsSelectedAccount(e == null ? null : e.Order == null ? null : e.Order.Account))
					return;
				MarkAccountEvent();
				Session.OnOrderUpdate(e);
				RaiseSessionChanged();
			});
		}

		private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			RunOnUi(() => {
				if (Session == null || e == null || e.Execution == null || !IsSelectedAccount(e.Execution.Account))
					return;
				MarkAccountEvent();
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
				MarkAccountEvent();
				Session.OnPositionUpdate(e);
				RaiseSessionChanged();
			});
		}

		private void OnAccountItemUpdate(object sender, AccountItemEventArgs e)
		{
			RunOnUi(() => {
				if (Session == null || e == null || !IsSelectedAccount(e.Account))
					return;
				MarkAccountEvent();
				Session.OnAccountItemUpdate(e);
				RaiseSessionChanged();
			});
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			lastHeartbeatTime = DateTime.Now;
			if (Session != null)
				Session.OnTimerTick(account);
			RaiseSessionChanged();
		}

		private void MarkAccountEvent()
		{
			lastAccountEventTime = DateTime.Now;
			accountEventCount++;
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
		private readonly OrcaRulebookLedger ledger;
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
			ledger = OrcaRulebookLedger.Open(AccountName, this.instrumentFilter);
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
		public OrcaRulebookLedger Ledger { get { return ledger; } }
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
				if (ledger != null)
					ledger.SetInstrumentScope(instrumentFilter);
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
			if (ledger != null)
				ledger.Begin(StartTime);
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
			if (ledger != null)
				ledger.Pause();
		}

		public void Resume()
		{
			if (Status == OrcaDisciplineSessionStatus.Paused)
				Status = OrcaDisciplineSessionStatus.Active;
			if (ledger != null)
				ledger.Resume();
		}

		public void End()
		{
			if (Status == OrcaDisciplineSessionStatus.Ended)
				return;
			EndTime = DateTime.Now;
			Status = OrcaDisciplineSessionStatus.Ended;
			if (ledger != null)
				ledger.End(EndTime);
			RecalculateScore();
			RaiseAll();
		}

		public void OnOrderUpdate(OrderEventArgs e)
		{
			if (Status != OrcaDisciplineSessionStatus.Active || e == null || e.Order == null || e.Order.Instrument == null)
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
			if (ledger != null)
				ledger.BeginConsequenceWindow();
			try {
				OrcaRulebookIngestResult ingested = RecordLedgerExecution(e);
				if (ingested == OrcaRulebookIngestResult.Duplicate || ingested == OrcaRulebookIngestResult.Conflict)
					return;
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
			} finally {
				if (ledger != null)
					ledger.EndConsequenceWindow();
			}
		}

		public void OnPositionUpdate(PositionEventArgs e)
		{
			if (Status != OrcaDisciplineSessionStatus.Active || e == null || e.Position == null || e.Position.Instrument == null)
				return;
			ObserveInstrument(e.Position.Instrument);
			if (!MatchesInstrumentFilter(e.Position.Instrument))
				return;
			int signed = e.MarketPosition == MarketPosition.Short ? -Math.Abs(e.Quantity) : Math.Abs(e.Quantity);
			if (e.MarketPosition == MarketPosition.Flat || e.Quantity == 0)
				signed = 0;
			SyncPositionFromTracker(InstrumentName(e.Position.Instrument), signed);
			RefreshRulesCurrentValues();
			RaiseAll();
		}

		public void OnAccountItemUpdate(AccountItemEventArgs e)
		{
			if (Status != OrcaDisciplineSessionStatus.Active || e == null || e.AccountItem != AccountItem.RealizedProfitLoss || e.Currency != Currency.UsDollar)
				return;
			SessionRealizedPnl = e.Value - baselineRealizedPnl;
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnAccountItem(this, e);
			RecalculateScore();
			RaiseAll();
		}

		public void OnTimerTick(Account account)
		{
			if (Status != OrcaDisciplineSessionStatus.Active)
				return;
			SyncOpenPositions(account, true);
			if (account != null)
				SessionRealizedPnl = SafeAccountGet(account, AccountItem.RealizedProfitLoss) - baselineRealizedPnl;
			foreach (OrcaDisciplineRule rule in Rules)
				rule.OnTimerTick(this, DateTime.Now);
			RefreshRulesCurrentValues();
			RecalculateScore();
			RaiseAll();
		}

		public void AddViolation(OrcaDisciplineRule rule, string message, string instrument, string observed, string limit)
		{
			if (Status != OrcaDisciplineSessionStatus.Active || rule == null || !rule.Enabled)
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
			if (ledger != null)
				violation.TradeId = ledger.LinkViolation(rule.Id, rule.Name, rule.Severity.ToString(), message, instrument ?? string.Empty, violation.Timestamp);
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
			sb.AppendLine("Orca Rulebook Session");
			sb.AppendLine("Account: " + AccountName);
			if (ledger != null)
				sb.AppendLine("Ledger: " + ledger.EvidenceStatus + ". " + ledger.EvidenceReason);
			sb.AppendLine("Template: " + TemplateName);
			sb.AppendLine("Instrument Filter: " + InstrumentFilter);
			sb.AppendLine("Status: " + StatusText);
			if (StartTime != DateTime.MinValue)
				sb.AppendLine("Start: " + StartTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
			if (EndTime != DateTime.MinValue)
				sb.AppendLine("End: " + EndTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
			sb.AppendLine("Grade: " + Grade + " (" + Score.ToString("0", CultureInfo.InvariantCulture) + ")");
			sb.AppendLine(ScoreBreakdown);
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
			if (Status != OrcaDisciplineSessionStatus.Active || rule == null || !rule.Enabled)
				return;
			if (string.Equals(rule.ManualAction, OrcaManualActionValues.Followed, StringComparison.OrdinalIgnoreCase)) {
				rule.RegisterFollow();
				RecordManualOpportunity(rule, "Followed");
				rule.RefreshCurrentValue(this);
				return;
			}
			if (string.Equals(rule.ManualAction, OrcaManualActionValues.Broken, StringComparison.OrdinalIgnoreCase) && !rule.HasManualBrokenViolation) {
				rule.HasManualBrokenViolation = true;
				AddViolation(rule, "Manual rule marked broken" + (string.IsNullOrWhiteSpace(rule.Notes) ? string.Empty : ": " + rule.Notes), string.Empty, "Broken", "Followed");
				RecordManualOpportunity(rule, "Broken");
				rule.RefreshCurrentValue(this);
			}
			if (string.Equals(rule.ManualAction, OrcaManualActionValues.NotApplicable, StringComparison.OrdinalIgnoreCase)) {
				rule.Status = OrcaDisciplineRuleStatus.Disabled;
				RecordManualOpportunity(rule, "NotApplicable");
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
			RecordCompletedCycleOpportunities(trade);
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
					if (Status == OrcaDisciplineSessionStatus.Active) {
						tracker.SeedOpenPosition(position);
						if (ledger != null)
							ledger.SeedOpenPosition(InstrumentName(position.Instrument), signed, position.AveragePrice, DateTime.Now);
					}
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

		public string ScoreBreakdown
		{
			get { return OrcaDisciplineScoring.Breakdown(Rules); }
		}

		public string EnabledWeightText
		{
			get { return "Weight " + OrcaDisciplineScoring.EnabledWeight(Rules).ToString(CultureInfo.InvariantCulture); }
		}

		private void RecalculateScore()
		{
			Score = OrcaDisciplineScoring.WeightedScore(Rules);
			Grade = OrcaDisciplineScoring.Grade(Score);
			Raise("ScoreBreakdown");
			Raise("EnabledWeightText");
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

		private OrcaRulebookIngestResult RecordLedgerExecution(ExecutionEventArgs e)
		{
			if (ledger == null || e == null || e.Execution == null)
				return OrcaRulebookIngestResult.Ignored;
			try {
				Execution execution = e.Execution;
				string instrument = InstrumentName(execution.Instrument);
				int signed = SignedExecutionQuantity(execution);
				bool historical = e.IsSod || execution.Order == null;
				if (!historical && signed == 0) {
					ledger.MarkGap(instrument, "Execution quantity was not a buy or sell");
					return OrcaRulebookIngestResult.Ignored;
				}
				int? positionAfter = null;
				if (!historical) {
					try { positionAfter = execution.Position; } catch { positionAfter = null; }
				}
				return ledger.Ingest(new OrcaRulebookFill {
					Account = AccountName,
					Instrument = instrument,
					ExecutionId = execution.ExecutionId,
					SignedQuantity = signed,
					Price = execution.Price,
					PointValue = ExecutionPointValue(execution),
					Time = e.Time,
					PositionAfter = positionAfter,
					Historical = historical
				});
			} catch (Exception ex) {
				ledger.MarkGap(string.Empty, "Execution could not be read");
				OrcaDisciplineDiagnostics.Write("Orca Rulebook ledger skipped an execution: " + ex.Message);
				return OrcaRulebookIngestResult.Ignored;
			}
		}

		private static int SignedExecutionQuantity(Execution execution)
		{
			if (execution == null || execution.Order == null || execution.Quantity <= 0)
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

		private static double ExecutionPointValue(Execution execution)
		{
			try {
				if (execution != null && execution.Instrument != null && execution.Instrument.MasterInstrument != null)
					return execution.Instrument.MasterInstrument.PointValue;
			} catch { }
			return 0;
		}

		private void RecordManualOpportunity(OrcaDisciplineRule rule, string result)
		{
			if (ledger == null || rule == null)
				return;
			ledger.RecordOpportunity(rule.Id, rule.Name, "Manual", result, string.Empty, "Manual checklist is not a trade-opportunity denominator.", false);
		}

		private void RecordCompletedCycleOpportunities(OrcaRoundTripTrade trade)
		{
			if (ledger == null || trade == null)
				return;
			foreach (OrcaDisciplineRule rule in Rules) {
				if (rule == null || !rule.Enabled || rule.Mode == OrcaDisciplineRuleMode.Manual)
					continue;
				ledger.RecordOpportunity(rule.Id, rule.Name, "CompletedCycle", "PendingDefinition", trade.InstrumentName, "Denominator is not approved; this is not a grade.", true);
			}
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
		private string name;
		private int weight;
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
			name = config.Name ?? string.Empty;
			Description = config.Description;
			Enabled = config.Enabled;
			Mode = config.Mode;
			Severity = config.Severity;
			weight = config.Weight >= 1 ? config.Weight : OrcaDisciplineScoring.StarterWeight(Id, Type, name, Mode);
			Parameters = config.Parameters == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(config.Parameters, StringComparer.OrdinalIgnoreCase);
			Status = Enabled ? OrcaDisciplineRuleStatus.NotStarted : OrcaDisciplineRuleStatus.Disabled;
			ManualAction = string.Empty;
			CurrentValueText = string.Empty;
		}

		public string Id { get; private set; }
		public string Type { get; private set; }
		public string Description { get; private set; }

		public string Name
		{
			get { return name; }
			set {
				if (!IsManual) {
					Raise("Name");
					return;
				}
				string next = value == null ? string.Empty : value.Trim();
				if (next.Length == 0) {
					Raise("Name");
					return;
				}
				string previous = name;
				if (!Set(ref name, next, "Name"))
					return;
				if (string.Equals(Description, previous, StringComparison.Ordinal))
					Description = next;
			}
		}
		public int Weight
		{
			get { return weight; }
			set {
				int next = value < 1 ? 1 : value;
				if (!Set(ref weight, next, "Weight"))
					return;
				Raise("WeightText");
			}
		}

		public string WeightText
		{
			get { return weight.ToString(CultureInfo.InvariantCulture); }
			set {
				int parsed;
				string text = value == null ? string.Empty : value.Trim();
				if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) || parsed < 1) {
					Raise("WeightText");
					return;
				}
				Weight = parsed;
			}
		}

		public bool IsBroken
		{
			get { return Enabled && (Status == OrcaDisciplineRuleStatus.Violated || ViolationCount > 0); }
		}

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

		public bool CanRename
		{
			get { return IsManual; }
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
				Weight = Weight,
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
			template.Rules.Add(Config("cooldown", "TradeCooldown", "Minimum time between new trades", "Minimum time between fresh flat-to-position trades.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MinimumMinutes", "5")));
			template.Rules.Add(Config("max-position", "MaxPositionSize", "Max position size", "Flags any instrument whose account position exceeds the mini-equivalent contract limit.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxContracts", "2", "MicroMultiplier", OrcaDisciplineConstants.DefaultMicroMultiplier, "MicroSymbols", OrcaDisciplineConstants.DefaultMicroSymbols)));
			template.Rules.Add(Config("max-trade-loss", "MaxLossPerTrade", "Max loss per trade", "Uses gross round-trip realized P&L after the trade closes.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxLoss", "300")));
			template.Rules.Add(Config("max-session-loss", "MaxSessionLoss", "Max session loss", "Uses selected account realized P&L from session start.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxLoss", "600")));
			template.Rules.Add(Config("max-trades", "MaxTradesPerSession", "Max trades per session", "Counts completed round trips.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxTrades", "5")));
			template.Rules.Add(Config("max-loss-streak", "MaxConsecutiveLosses", "Max consecutive losses", "Flags losing streaks after completed round trips.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Major, Dict("MaxLosses", "2")));
			template.Rules.Add(Config("window", "AllowedTradingWindow", "Allowed trading window", "Flags fresh trades outside the configured local time window.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Warning, Dict("Start", "09:30", "End", "11:30")));
			template.Rules.Add(Config("max-violations", "MaxRuleViolations", "Max rule violations", "Flags when the session breaks too many rules.", OrcaDisciplineRuleMode.Automated, OrcaDisciplineSeverity.Critical, Dict("MaxViolations", "3")));
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
				Weight = OrcaDisciplineScoring.StarterWeight(id, type, name, mode),
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
		public int Weight { get; set; }
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
				Weight = Weight,
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

		public static string SaveRulebookLedger(OrcaRulebookLedger ledger)
		{
			lock (Sync) {
				EnsureSessions();
				return OrcaRulebookLedger.WriteAtomic(ledger, SessionsDirectory);
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
			bool changed = StripLegacyAutomatedNames(loaded);
			if (AssignMissingWeights(loaded))
				changed = true;
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

		private static readonly Dictionary<string, string> LegacyAutomatedNames = new Dictionary<string, string>(StringComparer.Ordinal) {
			{ "Minimum 5 minutes between new trades", "Minimum time between new trades" },
			{ "Max position size: 2 minis / 20 micros", "Max position size" },
			{ "Max loss per trade: $300", "Max loss per trade" },
			{ "Max session loss: $600", "Max session loss" },
			{ "Max trades per session: 5", "Max trades per session" },
			{ "Max consecutive losses: 2", "Max consecutive losses" },
			{ "Allowed trading window: 09:30 to 11:30", "Allowed trading window" },
			{ "Max rule violations: 3", "Max rule violations" }
		};

		private static bool StripLegacyAutomatedNames(List<OrcaDisciplineRuleTemplate> loaded)
		{
			if (loaded == null)
				return false;
			bool changed = false;
			foreach (OrcaDisciplineRuleTemplate template in loaded) {
				if (template == null || template.Rules == null)
					continue;
				foreach (OrcaDisciplineRuleConfig rule in template.Rules) {
					if (rule == null || rule.Mode == OrcaDisciplineRuleMode.Manual)
						continue;
					if (string.Equals(rule.Type, "ManualChecklist", StringComparison.OrdinalIgnoreCase))
						continue;
					string next;
					if (rule.Name == null || !LegacyAutomatedNames.TryGetValue(rule.Name, out next))
						continue;
					if (string.Equals(rule.Description, rule.Name, StringComparison.Ordinal))
						rule.Description = next;
					rule.Name = next;
					changed = true;
				}
			}
			return changed;
		}

		private static bool AssignMissingWeights(List<OrcaDisciplineRuleTemplate> loaded)
		{
			if (loaded == null)
				return false;
			bool changed = false;
			foreach (OrcaDisciplineRuleTemplate template in loaded) {
				if (template == null || template.Rules == null)
					continue;
				foreach (OrcaDisciplineRuleConfig rule in template.Rules) {
					if (rule == null || rule.Weight >= 1)
						continue;
					rule.Weight = OrcaDisciplineScoring.StarterWeight(rule.Id, rule.Type, rule.Name, rule.Mode);
					changed = true;
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
		public static int StarterWeight(string id, string type, string name, OrcaDisciplineRuleMode mode)
		{
			string idKey = id == null ? string.Empty : id.Trim();
			string typeKey = type == null ? string.Empty : type.Trim();
			string nameKey = name == null ? string.Empty : name.Trim();
			if (IsId(idKey, "max-session-loss") || IsName(nameKey, "Max session loss") || IsType(typeKey, "MaxSessionLoss"))
				return 12;
			if (IsId(idKey, "max-trade-loss") || IsName(nameKey, "Max loss per trade") || IsType(typeKey, "MaxLossPerTrade"))
				return 12;
			if (IsId(idKey, "no-add-loser") || IsName(nameKey, "No adding to losing trades") || IsType(typeKey, "NoAddToLosingTrade"))
				return 12;
			if (IsId(idKey, "max-position") || IsName(nameKey, "Max position size") || IsType(typeKey, "MaxPositionSize"))
				return 8;
			if (IsId(idKey, "max-loss-streak") || IsName(nameKey, "Max consecutive losses") || IsType(typeKey, "MaxConsecutiveLosses"))
				return 8;
			if (nameKey.IndexOf("revenge", StringComparison.OrdinalIgnoreCase) >= 0)
				return 8;
			if (nameKey.IndexOf("major rule", StringComparison.OrdinalIgnoreCase) >= 0)
				return 8;
			if (IsId(idKey, "window") || IsName(nameKey, "Allowed trading window") || IsType(typeKey, "AllowedTradingWindow"))
				return 5;
			if (IsId(idKey, "max-trades") || IsName(nameKey, "Max trades per session") || IsType(typeKey, "MaxTradesPerSession"))
				return 5;
			if (IsId(idKey, "max-violations") || IsName(nameKey, "Max rule violations") || IsType(typeKey, "MaxRuleViolations"))
				return 5;
			if (IsId(idKey, "cooldown") || IsName(nameKey, "Minimum time between new trades") || IsType(typeKey, "TradeCooldown"))
				return 4;
			if (IsId(idKey, "loss-reversal") || IsName(nameKey, "No immediate reversal after loss") || IsType(typeKey, "NoImmediateLossReversal"))
				return 4;
			if (mode == OrcaDisciplineRuleMode.Manual || IsType(typeKey, "ManualChecklist"))
				return 3;
			return 5;
		}

		public static int EnabledWeight(IEnumerable<OrcaDisciplineRule> rules)
		{
			int total = 0;
			if (rules == null)
				return 0;
			foreach (OrcaDisciplineRule rule in rules) {
				if (rule == null || !rule.Enabled || rule.Weight < 1)
					continue;
				total += rule.Weight;
			}
			return total;
		}

		public static int WeightedScore(IEnumerable<OrcaDisciplineRule> rules)
		{
			int keptWeight = 0;
			if (rules != null) {
				foreach (OrcaDisciplineRule rule in rules) {
					if (rule == null || !rule.Enabled || rule.Weight < 1 || rule.IsBroken)
						continue;
					keptWeight += rule.Weight;
				}
			}
			return ShareOfHundred(keptWeight, EnabledWeight(rules));
		}

		public static string Breakdown(IEnumerable<OrcaDisciplineRule> rules)
		{
			int enabledWeight = EnabledWeight(rules);
			List<OrcaDisciplineRule> broken = new List<OrcaDisciplineRule>();
			if (rules != null) {
				foreach (OrcaDisciplineRule rule in rules) {
					if (rule == null || !rule.Enabled || rule.Weight < 1 || !rule.IsBroken)
						continue;
					broken.Add(rule);
				}
			}
			if (enabledWeight <= 0)
				return "Why this score: no rules are enabled.";
			if (broken.Count == 0)
				return "Why this score: every enabled rule held.";
			StringBuilder text = new StringBuilder("Why this score:");
			foreach (OrcaDisciplineRule rule in broken) {
				int cost = ShareOfHundred(rule.Weight, enabledWeight);
				text.Append(" ");
				text.Append(string.IsNullOrWhiteSpace(rule.Name) ? rule.Id : rule.Name);
				text.Append(" cost ");
				text.Append(cost.ToString(CultureInfo.InvariantCulture));
				text.Append(".");
			}
			return text.ToString();
		}

		public static int ShareOfHundred(int weight, int enabledWeight)
		{
			if (enabledWeight <= 0)
				return 100;
			return (int)Math.Round(100.0 * weight / enabledWeight, MidpointRounding.AwayFromZero);
		}

		private static bool IsId(string id, string expected)
		{
			return string.Equals(id, expected, StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsName(string name, string expected)
		{
			return string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsType(string type, string expected)
		{
			return string.Equals(type, expected, StringComparison.OrdinalIgnoreCase);
		}

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

	public sealed class OrcaDisciplineMonitoringBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string state = value as string ?? string.Empty;
			bool border = string.Equals(parameter as string, "border", StringComparison.OrdinalIgnoreCase);
			if (border)
				return OrcaRulebookChrome.Brush(state == "ARMED" ? OrcaRulebookChrome.Accent : OrcaRulebookChrome.Hairline);
			if (state == "ARMED")
				return OrcaRulebookChrome.Brush(OrcaRulebookChrome.Positive);
			if (state == "PAUSED" || state == "ENDED" || state == "READY")
				return OrcaRulebookChrome.Brush(OrcaRulebookChrome.Label);
			return OrcaRulebookChrome.Brush(OrcaRulebookChrome.AlertText);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public sealed class OrcaRulebookGradeBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string grade = value as string ?? string.Empty;
			bool positive = grade == "A" || grade == "B";
			return OrcaRulebookChrome.Brush(positive ? OrcaRulebookChrome.Positive : OrcaRulebookChrome.Text);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
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
