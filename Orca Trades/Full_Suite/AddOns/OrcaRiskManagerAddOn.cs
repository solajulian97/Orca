#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	public sealed class OrcaRiskManagerAddOn : NinjaTrader.NinjaScript.AddOnBase
	{
		private static readonly Dictionary<ChartTab, bool> PanelVisibilityByTab = new Dictionary<ChartTab, bool>();
		private static readonly Dictionary<Chart, bool> PanelVisibilityByChart = new Dictionary<Chart, bool>();
		private static readonly object ToggleSync = new object();
		private static DateTime lastPanelToggleUtc = DateTime.MinValue;
		private NTMenuItem hostMenu;
		private NTMenuItem riskMenuItem;
		private const string PanelVersion = "OrcaRiskPanel.BracketBuilder.20260528.3";
		private const string ActiveVersionKey = "OrcaRiskManager.ActivePanelVersion";
		private const string LastPanelToggleTicksKey = "OrcaRiskManager.LastPanelToggleTicks";
		private const string ControlCToggleIsDownKey = "OrcaRiskManager.ControlCToggleIsDown";
		private const string ControlCToggleDownTicksKey = "OrcaRiskManager.ControlCToggleDownTicks";
		private const string WindowHostTag = "OrcaRiskManager.WindowHost";

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Orca Risk Manager NT ChartTrader Injection AddOn";
				Name = "Orca Risk Manager NT AddOn";
			}
		}

		protected override void OnWindowCreated(Window window)
		{
			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter != null) {
				AddControlCenterMenu(controlCenter);
				return;
			}

			Chart chartWindow = window as Chart;
			if (chartWindow == null) return;
			try { Application.Current.Properties[ActiveVersionKey] = PanelVersion; } catch { }

			chartWindow.Dispatcher.InvokeAsync(() => {
				if (chartWindow.IsLoaded) RefreshChartWindowPanels(chartWindow);
			});

			chartWindow.Loaded += (s, e) => {
				chartWindow.Dispatcher.InvokeAsync(() => { RefreshChartWindowPanels(chartWindow); });
			};

			var retryTimer = new System.Windows.Threading.DispatcherTimer();
			retryTimer.Interval = TimeSpan.FromSeconds(2);
			retryTimer.Tick += (s, e) => { retryTimer.Stop(); RefreshChartWindowPanels(chartWindow); };
			retryTimer.Start();

			chartWindow.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler((s, e) => {
				if (IsControlZChartTraderHotkey(e))
					ScheduleChartTraderOpaqueRefresh(chartWindow);
				if (HandleChartToggleHotkey(chartWindow, e, e.OriginalSource))
					return;
				if (ForwardRiskPanelKeyDown(chartWindow, e))
					e.Handled = true;
			}), true);

			chartWindow.AddHandler(Keyboard.PreviewKeyUpEvent, new KeyEventHandler((s, e) => {
				ReleaseChartToggleHotkeyIfNeeded(e);
				if (ForwardRiskPanelKeyUp(chartWindow, e))
					e.Handled = true;
			}), true);

			chartWindow.Dispatcher.InvokeAsync(() => {
				if (chartWindow.MainTabControl != null) {
					chartWindow.MainTabControl.SelectionChanged += (s, e) => { RefreshChartWindowPanels(chartWindow); };
				}
			});
		}

		protected override void OnWindowDestroyed(Window window)
		{
			Chart chartWindow = window as Chart;
			if (chartWindow != null) {
				CleanupChartWindowPanels(chartWindow);
				return;
			}

			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || riskMenuItem == null || hostMenu == null)
				return;

			riskMenuItem.Click -= OnRiskMenuItemClick;
			hostMenu.Items.Remove(riskMenuItem);
			riskMenuItem = null;
			hostMenu = null;
		}

		private void AddControlCenterMenu(ControlCenter controlCenter)
		{
			if (riskMenuItem != null)
				return;

			hostMenu = controlCenter.FindFirst("ControlCenterMenuItemTools") as NTMenuItem
				?? controlCenter.FindFirst("toolsMenuItem") as NTMenuItem
				?? controlCenter.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
			if (hostMenu == null)
				return;

			riskMenuItem = new NTMenuItem {
				Header = "Orca Risk Manager",
				Style = Application.Current.TryFindResource("MainMenuItem") as Style
			};
			riskMenuItem.Click += OnRiskMenuItemClick;
			hostMenu.Items.Add(riskMenuItem);
		}

		private void OnRiskMenuItemClick(object sender, RoutedEventArgs e)
		{
			Application.Current.Dispatcher.InvokeAsync(() => OrcaRiskManagerWindow.ShowOrActivate());
		}

		private bool HandleChartToggleHotkey(Chart chartWindow, KeyEventArgs e, object eventSource)
		{
			if (!IsControlCToggleHotkey(e))
				return false;
			if (e.IsRepeat) {
				e.Handled = true;
				return true;
			}
			if (IsControlCToggleKeyDownLatched() || IsPanelToggleRecentlyHandled()) {
				e.Handled = true;
				return true;
			}
			if (TogglePanelVisibility(chartWindow, eventSource)) {
				MarkControlCToggleKeyDown();
				e.Handled = true;
				return true;
			}
			return false;
		}

		private void ReleaseChartToggleHotkeyIfNeeded(KeyEventArgs e)
		{
			if (e == null)
				return;
			Key key = e.Key == Key.System ? e.SystemKey : e.Key;
			if (key == Key.C || key == Key.LeftCtrl || key == Key.RightCtrl)
				ReleaseControlCToggleKeyDown();
		}

		internal static bool IsControlCToggleHotkey(KeyEventArgs e)
		{
			if (e == null)
				return false;
			Key key = e.Key == Key.System ? e.SystemKey : e.Key;
			return key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
		}

		private static bool IsControlZChartTraderHotkey(KeyEventArgs e)
		{
			if (e == null)
				return false;
			Key key = e.Key == Key.System ? e.SystemKey : e.Key;
			return key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
		}

		internal static bool IsPanelToggleRecentlyHandled()
		{
			lock (ToggleSync) {
				DateTime now = DateTime.UtcNow;
				try {
					object value = Application.Current.Properties[LastPanelToggleTicksKey];
					if (value is long ticks && ticks > 0 && (now - new DateTime(ticks, DateTimeKind.Utc)).TotalMilliseconds < 150)
						return true;
				} catch { }

				return (now - lastPanelToggleUtc).TotalMilliseconds < 150;
			}
		}

		internal static bool IsControlCToggleKeyDownLatched()
		{
			lock (ToggleSync) {
				DateTime now = DateTime.UtcNow;
				try {
					object isDown = Application.Current.Properties[ControlCToggleIsDownKey];
					object ticksValue = Application.Current.Properties[ControlCToggleDownTicksKey];
					if (isDown is bool down && down) {
						long ticks = ticksValue is long storedTicks ? storedTicks : 0;
						if (ticks <= 0 || (now - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds < 5)
							return true;
						Application.Current.Properties[ControlCToggleIsDownKey] = false;
					}
				} catch { }
				return false;
			}
		}

		internal static void MarkControlCToggleKeyDown()
		{
			lock (ToggleSync) {
				MarkPanelToggleHandled();
				try {
					Application.Current.Properties[ControlCToggleIsDownKey] = true;
					Application.Current.Properties[ControlCToggleDownTicksKey] = DateTime.UtcNow.Ticks;
				} catch { }
			}
		}

		internal static void ReleaseControlCToggleKeyDown()
		{
			lock (ToggleSync) {
				try { Application.Current.Properties[ControlCToggleIsDownKey] = false; } catch { }
			}
		}

		internal static void MarkPanelToggleHandled()
		{
			lock (ToggleSync) {
				lastPanelToggleUtc = DateTime.UtcNow;
				try { Application.Current.Properties[LastPanelToggleTicksKey] = lastPanelToggleUtc.Ticks; } catch { }
			}
		}

		private bool TogglePanelVisibility(Chart chartWindow, object eventSource)
		{
			if (chartWindow.MainTabControl == null || chartWindow.MainTabControl.Items.Count == 0) return false;
			SweepStalePanels(chartWindow);
			ChartTab tab = GetActiveChartTab(chartWindow, eventSource);
			if (tab == null) return false;
			OrcaRiskPanel currentPanel = EnsureWindowRiskPanel(chartWindow, tab);
			if (currentPanel != null) {
				System.Windows.Controls.Grid hostGrid = GetPanelHostGrid(currentPanel);
				bool shouldShow = !IsPanelCurrentlyVisible(hostGrid, currentPanel);
				SetPanelVisibility(chartWindow, hostGrid, currentPanel, shouldShow);
				return true;
			}
			return false;
		}

		private void InsertChartTraderControl(Chart chartWindow, ChartTab targetTab = null)
		{
			try {
				if (chartWindow.MainTabControl == null || chartWindow.MainTabControl.Items.Count == 0) return;
				ChartTab tab = targetTab ?? GetActiveChartTab(chartWindow, null);
				if (tab != null)
					EnsureWindowRiskPanel(chartWindow, tab);
			} catch { }
		}

		private void RefreshChartWindowPanels(Chart chartWindow)
		{
			if (chartWindow.MainTabControl == null || chartWindow.MainTabControl.Items.Count == 0) return;
			SweepStalePanels(chartWindow);
			SyncPanelsToActiveTab(chartWindow);
		}

		private bool IsActivePanelVersion()
		{
			try {
				object active = Application.Current.Properties[ActiveVersionKey];
				return active == null || string.Equals(active as string, PanelVersion, StringComparison.Ordinal);
			} catch { return true; }
		}

		private void SyncPanelsToActiveTab(Chart chartWindow)
		{
			ChartTab activeTab = GetActiveChartTab(chartWindow, null);
			if (activeTab == null) return;
			EnsureWindowRiskPanel(chartWindow, activeTab);
		}

		private bool ShouldShowPanel(Chart chartWindow)
		{
			if (chartWindow != null && PanelVisibilityByChart.TryGetValue(chartWindow, out bool isVisible)) return isVisible;
			SetPanelWindowVisible(chartWindow, true);
			return true;
		}

		internal static void SetPanelTabVisible(ChartTab tab, bool isVisible)
		{
			if (tab == null) return;
			PanelVisibilityByTab[tab] = isVisible;
		}

		internal static void SetPanelWindowVisible(Window window, bool isVisible)
		{
			Chart chartWindow = window as Chart;
			if (chartWindow != null)
				PanelVisibilityByChart[chartWindow] = isVisible;
		}

		private void SweepStalePanels(Chart chartWindow)
		{
			foreach (object item in chartWindow.MainTabControl.Items) {
				ChartTab tab = item as ChartTab;
				if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
				if (tab?.Content is System.Windows.Controls.Grid tabGrid) RemoveRiskPanels(tabGrid);
			}
		}

		private void CleanupInactivePanels(Chart chartWindow, ChartTab activeTab)
		{
			foreach (object item in chartWindow.MainTabControl.Items) {
				ChartTab tab = item as ChartTab;
				if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
				if (tab == null || object.ReferenceEquals(tab, activeTab)) continue;
				if (tab.Content is System.Windows.Controls.Grid tabGrid) RemoveRiskPanels(tabGrid, staleOnly: true);
			}
		}

		private OrcaRiskPanel EnsureWindowRiskPanel(Chart chartWindow, ChartTab tab)
		{
			if (chartWindow == null || tab == null)
				return null;

			System.Windows.Controls.Grid hostGrid = GetChartWindowHostGrid(chartWindow);
			if (hostGrid == null)
				return null;

			RemoveRiskPanels(hostGrid, staleOnly: true);
			OrcaRiskPanel panel = GetCurrentPanel(hostGrid);
			if (panel == null) {
				panel = new OrcaRiskPanel(tab);
				panel.Tag = PanelVersion;
				panel.HorizontalAlignment = HorizontalAlignment.Stretch;
				panel.VerticalAlignment = VerticalAlignment.Stretch;

				if (hostGrid.ColumnDefinitions.Count == 0)
					hostGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				if (hostGrid.ColumnDefinitions.Count == 1)
					hostGrid.ColumnDefinitions.Add(new ColumnDefinition());
				hostGrid.ColumnDefinitions[1].Width = ShouldShowPanel(chartWindow) ? new GridLength(GetConfiguredPanelWidth()) : new GridLength(0);

				int panelColumn = 1;
				System.Windows.Controls.Grid.SetColumn(panel, panelColumn);
				hostGrid.Children.Add(panel);
			} else {
				panel.AttachToTab(tab);
			}

			SetPanelVisibility(chartWindow, hostGrid, panel, ShouldShowPanel(chartWindow));
			return panel;
		}

		private System.Windows.Controls.Grid GetChartWindowHostGrid(Chart chartWindow, bool createIfMissing = true)
		{
			try {
				if (chartWindow == null)
					return null;

				System.Windows.Controls.Grid existingHost = chartWindow.Content as System.Windows.Controls.Grid;
				if (existingHost != null && string.Equals(existingHost.Tag as string, WindowHostTag, StringComparison.Ordinal))
					return existingHost;

				if (!createIfMissing)
					return null;

				UIElement originalContent = chartWindow.Content as UIElement;
				if (originalContent == null)
					return null;

				System.Windows.Controls.Grid hostGrid = new System.Windows.Controls.Grid {
					Tag = WindowHostTag,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				};
				hostGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				hostGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = ShouldShowPanel(chartWindow) ? new GridLength(GetConfiguredPanelWidth()) : new GridLength(0) });

				chartWindow.Content = null;
				System.Windows.Controls.Grid.SetRow(originalContent, 0);
				System.Windows.Controls.Grid.SetRowSpan(originalContent, 1);
				System.Windows.Controls.Grid.SetColumn(originalContent, 0);
				System.Windows.Controls.Grid.SetColumnSpan(originalContent, 1);
				hostGrid.Children.Add(originalContent);
				chartWindow.Content = hostGrid;
				return hostGrid;
			} catch { }
			return null;
		}

		private void CleanupChartWindowPanels(Chart chartWindow)
		{
			try {
				if (chartWindow?.MainTabControl == null)
					return;

				foreach (object item in chartWindow.MainTabControl.Items) {
					ChartTab tab = item as ChartTab;
					if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
					if (tab?.Content is System.Windows.Controls.Grid tabGrid)
						RemoveRiskPanels(tabGrid);
					if (tab != null)
						PanelVisibilityByTab.Remove(tab);
				}
				System.Windows.Controls.Grid hostGrid = GetChartWindowHostGrid(chartWindow, false);
				if (hostGrid != null)
					RemoveRiskPanels(hostGrid);
				PanelVisibilityByChart.Remove(chartWindow);
			} catch { }
		}

		private OrcaRiskPanel GetCurrentPanel(System.Windows.Controls.Grid tabGrid)
		{
			foreach (UIElement el in tabGrid.Children) {
				if (el is OrcaRiskPanel panel && string.Equals(panel.Tag as string, PanelVersion, StringComparison.Ordinal))
					return panel;
			}
			return null;
		}

		private void SetPanelVisibility(Chart chartWindow, System.Windows.Controls.Grid tabGrid, OrcaRiskPanel panel, bool isVisible)
		{
			if (tabGrid == null || panel == null)
				return;
			int col = System.Windows.Controls.Grid.GetColumn(panel);
			if (col < 0 || col >= tabGrid.ColumnDefinitions.Count) return;
			SetPanelWindowVisible(chartWindow, isVisible);
			tabGrid.ColumnDefinitions[col].Width = isVisible ? new GridLength(GetConfiguredPanelWidth()) : new GridLength(0);
			panel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
			panel.HorizontalAlignment = HorizontalAlignment.Stretch;
			if (isVisible)
				ScheduleChartTraderOpaqueRefresh(chartWindow);
			RefreshTabLayout(tabGrid, null);
		}

		private void ScheduleChartTraderOpaqueRefresh(Chart chartWindow)
		{
			try {
				if (chartWindow == null || chartWindow.Dispatcher == null)
					return;

				chartWindow.Dispatcher.InvokeAsync(() => {
					ApplyChartTraderOpaqueBackground(chartWindow);
					var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
					timer.Tick += (s, e) => {
						timer.Stop();
						ApplyChartTraderOpaqueBackground(chartWindow);
					};
					timer.Start();
				});
			} catch { }
		}

		private void ApplyChartTraderOpaqueBackground(Chart chartWindow)
		{
			try {
				DependencyObject chartTrader = chartWindow?.ChartTrader as DependencyObject;
				if (chartTrader == null)
					return;

				ApplyOpaqueBackground(chartTrader, 0);
			} catch { }
		}

		private void ApplyOpaqueBackground(DependencyObject element, int depth)
		{
			if (element == null || depth > 3)
				return;

			try {
				UIElement uiElement = element as UIElement;
				if (uiElement != null)
					uiElement.Opacity = 1;

				System.Windows.Controls.Panel panel = element as System.Windows.Controls.Panel;
				if (panel != null && IsTransparentOrTranslucent(panel.Background))
					panel.Background = CreateChartTraderBackgroundBrush();

				System.Windows.Controls.Border border = element as System.Windows.Controls.Border;
				if (border != null && IsTransparentOrTranslucent(border.Background))
					border.Background = CreateChartTraderBackgroundBrush();

				System.Windows.Controls.Control control = element as System.Windows.Controls.Control;
				if (control != null && IsTransparentOrTranslucent(control.Background))
					control.Background = CreateChartTraderBackgroundBrush();

				int count = VisualTreeHelper.GetChildrenCount(element);
				for (int i = 0; i < count; i++)
					ApplyOpaqueBackground(VisualTreeHelper.GetChild(element, i), depth + 1);
			} catch { }
		}

		private bool IsTransparentOrTranslucent(System.Windows.Media.Brush brush)
		{
			if (brush == null || brush.Opacity < 0.99)
				return true;
			System.Windows.Media.SolidColorBrush solid = brush as System.Windows.Media.SolidColorBrush;
			return solid != null && solid.Color.A < 255;
		}

		private System.Windows.Media.Brush CreateChartTraderBackgroundBrush()
		{
			return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 31, 31));
		}

		private System.Windows.Controls.Grid GetPanelHostGrid(OrcaRiskPanel panel)
		{
			try { return VisualTreeHelper.GetParent(panel) as System.Windows.Controls.Grid; } catch { }
			return null;
		}

		private bool IsPanelCurrentlyVisible(System.Windows.Controls.Grid tabGrid, OrcaRiskPanel panel)
		{
			try {
				if (tabGrid == null || panel == null || panel.Visibility != Visibility.Visible)
					return false;

				int col = System.Windows.Controls.Grid.GetColumn(panel);
				return col >= 0
					&& col < tabGrid.ColumnDefinitions.Count
					&& tabGrid.ColumnDefinitions[col].Width.Value > 0.5;
			} catch { return false; }
		}

		private double GetConfiguredPanelWidth()
		{
			try { return Math.Max(160, Math.Min(420, OrcaRiskManager.GetSettings().PanelWidth)); }
			catch { return 235; }
		}

		private void RemoveRiskPanels(System.Windows.Controls.Grid tabGrid, bool staleOnly = false)
		{
			var panels = new List<UIElement>();
			var columns = new List<int>();
			foreach (UIElement el in tabGrid.Children.OfType<UIElement>().ToList()) {
				if (el.GetType().Name != "OrcaRiskPanel") continue;
				if (staleOnly && el is OrcaRiskPanel panel && string.Equals(panel.Tag as string, PanelVersion, StringComparison.Ordinal)) continue;
				panels.Add(el);
				columns.Add(System.Windows.Controls.Grid.GetColumn(el));
			}
			foreach (UIElement el in panels) {
				try { el.GetType().GetMethod("Cleanup")?.Invoke(el, null); } catch { }
				if (tabGrid.Children.Contains(el))
					tabGrid.Children.Remove(el);
			}
			foreach (int col in columns.Distinct().OrderByDescending(x => x)) {
				if (col >= 0 && col < tabGrid.ColumnDefinitions.Count)
					tabGrid.ColumnDefinitions.RemoveAt(col);
			}
			tabGrid.InvalidateMeasure();
			tabGrid.InvalidateArrange();
		}

		private void RefreshTabLayout(System.Windows.Controls.Grid tabGrid, ChartTab tab)
		{
			try {
				tabGrid?.InvalidateMeasure();
				tabGrid?.InvalidateArrange();
				ChartTab refreshTab = tab ?? FindOwningChartTab(tabGrid);
				ChartControl chartControl = refreshTab?.ChartControl;
				if (chartControl != null)
					chartControl.Dispatcher.InvokeAsync(() => chartControl.InvalidateVisual());
			} catch { }
		}

		private ChartTab GetSelectedChartTab(Chart chartWindow)
		{
			object content = chartWindow.MainTabControl?.SelectedContent;
			ChartTab selectedContentTab = content as ChartTab;
			if (selectedContentTab != null) return selectedContentTab;

			object item = chartWindow.MainTabControl?.SelectedItem;
			ChartTab tab = item as ChartTab;
			if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
			return tab;
		}

		private ChartTab GetActiveChartTab(Chart chartWindow, object eventSource)
		{
			ChartTab tab = FindOwningChartTab(eventSource as DependencyObject);
			if (tab != null && IsTabVisible(tab)) return tab;

			tab = GetSelectedChartTab(chartWindow);
			if (tab != null) return tab;

			tab = GetChartTabFromWindowProperty(chartWindow);
			if (tab != null) return tab;

			tab = GetVisibleChartTab(chartWindow);
			if (tab != null) return tab;

			foreach (object item in chartWindow.MainTabControl.Items) {
				tab = item as ChartTab;
				if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
				if (tab == null) continue;
				try {
					if (tab is UIElement tabElement && tabElement.IsKeyboardFocusWithin) return tab;
					if (tab.Content is UIElement contentElement && contentElement.IsKeyboardFocusWithin) return tab;
					if (tab.ChartControl != null && tab.ChartControl.IsKeyboardFocusWithin) return tab;
				} catch { }
			}

			return GetSelectedChartTab(chartWindow);
		}

		private ChartTab GetChartTabFromWindowProperty(Chart chartWindow)
		{
			foreach (string propertyName in new[] { "ActiveChartTab", "SelectedChartTab", "ChartTab" }) {
				try {
					object value = chartWindow.GetType().GetProperty(propertyName)?.GetValue(chartWindow, null);
					if (value is ChartTab tab) return tab;
				} catch { }
			}
			return null;
		}

		private ChartTab GetVisibleChartTab(Chart chartWindow)
		{
			ChartTab best = null;
			foreach (object item in chartWindow.MainTabControl.Items) {
				ChartTab tab = item as ChartTab;
				if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
				if (tab == null) continue;
				if (IsTabVisible(tab)) return tab;
				if (best == null) best = tab;
			}
			return best;
		}

		private bool IsTabVisible(ChartTab tab)
		{
			try {
				if (tab.ChartControl != null && tab.ChartControl.IsVisible && tab.ChartControl.ActualWidth > 0) return true;
				if (tab.Content is FrameworkElement content && content.IsVisible && content.ActualWidth > 0) return true;
				if (tab is FrameworkElement tabElement && tabElement.IsVisible && tabElement.ActualWidth > 0) return true;
			} catch { }
			return false;
		}

		private ChartTab FindOwningChartTab(DependencyObject source)
		{
			while (source != null) {
				if (source is ChartTab tab) return tab;
				DependencyObject parent = null;
				try { parent = VisualTreeHelper.GetParent(source); } catch { }
				if (parent == null) {
					try { parent = LogicalTreeHelper.GetParent(source) as DependencyObject; } catch { }
				}
				source = parent;
			}
			return null;
		}

		private bool ForwardRiskPanelKeyDown(Chart chartWindow, KeyEventArgs e)
		{
			OrcaRiskPanel panel = GetActiveRiskPanel(chartWindow, e.OriginalSource);
			return panel != null && panel.HandleSpacebarBuilderKeyDown(e);
		}

		private bool ForwardRiskPanelKeyUp(Chart chartWindow, KeyEventArgs e)
		{
			OrcaRiskPanel panel = GetActiveRiskPanel(chartWindow, e.OriginalSource);
			return panel != null && panel.HandleSpacebarBuilderKeyUp(e);
		}

		private OrcaRiskPanel GetActiveRiskPanel(Chart chartWindow, object eventSource)
		{
			ChartTab tab = GetActiveChartTab(chartWindow, eventSource);
			OrcaRiskPanel panel = tab == null ? null : EnsureWindowRiskPanel(chartWindow, tab);
			System.Windows.Controls.Grid hostGrid = GetPanelHostGrid(panel);
			return IsPanelCurrentlyVisible(hostGrid, panel) ? panel : null;
		}
	}

	[Serializable]
	public class OrcaRiskManagerSettings
	{
		public bool EnableHotkeys { get; set; } = true;
		public bool ConfirmLiveOrders { get; set; } = true;
		public bool ConfirmFlatten { get; set; } = true;
		public bool EnableLiveTradingHotkeys { get; set; } = false;
		public double PanelWidth { get; set; } = 235;
		public string FontFamily { get; set; } = "Segoe UI";
		public double FontSize { get; set; } = 11;
		public bool ShowQuickActions { get; set; } = true;
		public bool ShowPositionSizing { get; set; } = true;
		public bool ShowFastExecution { get; set; } = true;
		public bool ShowClosePosition { get; set; } = true;
		public bool ShowManagePosition { get; set; } = true;
		public bool ShowPnlDisplay { get; set; } = true;
		public string ThemePreset { get; set; } = "Orca Dark";
		public string BuyColor { get; set; } = "#FF44CC44";
		public string SellColor { get; set; } = "#FFCC4444";
		public string ActiveSelectedColor { get; set; } = "#FFCC9944";
		public string NeutralButtonColor { get; set; } = "#FF2A2A2A";
		public string DangerFlattenColor { get; set; } = "#80DC143C";
		public string TextColor { get; set; } = "#FFF8F8FF";
		public string ChartLabelFontFamily { get; set; } = "";
		public double ChartLabelFontSize { get; set; } = 0;
		public string ChartLabelProfitColor { get; set; } = "";
		public string ChartLabelRiskColor { get; set; } = "";
		public string ChartLabelTextColor { get; set; } = "";
		public bool EnableSpacebarBracketBuilder { get; set; } = false;
		public string SpacebarDefaultMode { get; set; } = "Stage";
		public bool ShowBracketPreviewRiskLabels { get; set; } = true;
		public int DefaultStopDistanceTicks { get; set; } = 20;
		public double DefaultTargetMultipleR { get; set; } = 2.0;
		public bool KeepBracketAfterSpaceRelease { get; set; } = true;
		public string SubmitStagedBracketHotkey { get; set; } = "Enter";
		public string CancelStagedBracketHotkey { get; set; } = "Escape";
	}

	public static class OrcaRiskManager
	{
		private static readonly object Sync = new object();
		private static OrcaRiskManagerSettings settings;
		public static event EventHandler SettingsChanged;

		public static OrcaRiskManagerSettings GetSettings()
		{
			EnsureLoaded();
			lock (Sync) {
				return Clone(settings);
			}
		}

		public static void SaveSettings(OrcaRiskManagerSettings newSettings)
		{
			if (newSettings == null)
				return;

			lock (Sync) {
				settings = Normalize(newSettings);
				string directory = System.IO.Path.GetDirectoryName(SettingsPath);
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);
				using (FileStream stream = File.Create(SettingsPath))
					new XmlSerializer(typeof(OrcaRiskManagerSettings)).Serialize(stream, settings);
			}
			NotifySettingsChanged();
		}

		private static void NotifySettingsChanged()
		{
			EventHandler handler = SettingsChanged;
			if (handler == null)
				return;

			try {
				if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
					Application.Current.Dispatcher.InvokeAsync(() => handler(null, EventArgs.Empty));
				else
					handler(null, EventArgs.Empty);
			} catch {
				try { handler(null, EventArgs.Empty); } catch { }
			}
		}

		private static string SettingsPath
		{
			get {
				return System.IO.Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					"NinjaTrader 8",
					"OrcaRiskManager.xml");
			}
		}

		private static void EnsureLoaded()
		{
			lock (Sync) {
				if (settings != null)
					return;

				settings = new OrcaRiskManagerSettings();
				try {
					if (File.Exists(SettingsPath)) {
						using (FileStream stream = File.OpenRead(SettingsPath))
							settings = (OrcaRiskManagerSettings)new XmlSerializer(typeof(OrcaRiskManagerSettings)).Deserialize(stream);
					}
				} catch {
					settings = new OrcaRiskManagerSettings();
				}
				settings = Normalize(settings);
			}
		}

		private static OrcaRiskManagerSettings Clone(OrcaRiskManagerSettings source)
		{
			return new OrcaRiskManagerSettings {
				EnableHotkeys = source.EnableHotkeys,
				ConfirmLiveOrders = source.ConfirmLiveOrders,
				ConfirmFlatten = source.ConfirmFlatten,
				EnableLiveTradingHotkeys = source.EnableLiveTradingHotkeys,
				PanelWidth = source.PanelWidth,
				FontFamily = source.FontFamily,
				FontSize = source.FontSize,
				ShowQuickActions = source.ShowQuickActions,
				ShowPositionSizing = source.ShowPositionSizing,
				ShowFastExecution = source.ShowFastExecution,
				ShowClosePosition = source.ShowClosePosition,
				ShowManagePosition = source.ShowManagePosition,
				ShowPnlDisplay = source.ShowPnlDisplay,
				ThemePreset = source.ThemePreset,
				BuyColor = source.BuyColor,
				SellColor = source.SellColor,
				ActiveSelectedColor = source.ActiveSelectedColor,
				NeutralButtonColor = source.NeutralButtonColor,
				DangerFlattenColor = source.DangerFlattenColor,
				TextColor = source.TextColor,
				ChartLabelFontFamily = source.ChartLabelFontFamily,
				ChartLabelFontSize = source.ChartLabelFontSize,
				ChartLabelProfitColor = source.ChartLabelProfitColor,
				ChartLabelRiskColor = source.ChartLabelRiskColor,
				ChartLabelTextColor = source.ChartLabelTextColor,
				EnableSpacebarBracketBuilder = source.EnableSpacebarBracketBuilder,
				SpacebarDefaultMode = source.SpacebarDefaultMode,
				ShowBracketPreviewRiskLabels = source.ShowBracketPreviewRiskLabels,
				DefaultStopDistanceTicks = source.DefaultStopDistanceTicks,
				DefaultTargetMultipleR = source.DefaultTargetMultipleR,
				KeepBracketAfterSpaceRelease = source.KeepBracketAfterSpaceRelease,
				SubmitStagedBracketHotkey = source.SubmitStagedBracketHotkey,
				CancelStagedBracketHotkey = source.CancelStagedBracketHotkey
			};
		}

		private static OrcaRiskManagerSettings Normalize(OrcaRiskManagerSettings input)
		{
			OrcaRiskManagerSettings output = Clone(input ?? new OrcaRiskManagerSettings());
			output.PanelWidth = Clamp(output.PanelWidth, 160, 420, 235);
			output.FontFamily = string.IsNullOrWhiteSpace(output.FontFamily) ? "Segoe UI" : output.FontFamily.Trim();
			output.FontSize = Clamp(output.FontSize, 8, 20, 11);
			output.ThemePreset = NormalizeThemePreset(output.ThemePreset);
			output.SpacebarDefaultMode = string.Equals(output.SpacebarDefaultMode, "Live", StringComparison.OrdinalIgnoreCase) ? "Live" : "Stage";
			output.DefaultStopDistanceTicks = (int)Math.Max(1, Math.Min(500, output.DefaultStopDistanceTicks));
			output.DefaultTargetMultipleR = Clamp(output.DefaultTargetMultipleR, 0.25, 10, 2.0);
			output.SubmitStagedBracketHotkey = NormalizeHotkey(output.SubmitStagedBracketHotkey, "Enter");
			output.CancelStagedBracketHotkey = NormalizeHotkey(output.CancelStagedBracketHotkey, "Escape");
			ApplyThemeDefaults(output);
			NormalizeChartLabelSettings(output);
			return output;
		}

		private static double Clamp(double value, double min, double max, double fallback)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
				return fallback;
			return Math.Max(min, Math.Min(max, value));
		}

		private static string NormalizeHotkey(string value, string fallback)
		{
			if (string.IsNullOrWhiteSpace(value))
				return fallback;
			string compact = value.Trim().Replace(" ", "");
			Key parsed;
			return Enum.TryParse(compact, true, out parsed) ? parsed.ToString() : fallback;
		}

		private static string NormalizeThemePreset(string preset)
		{
			if (string.Equals(preset, "High Contrast", StringComparison.OrdinalIgnoreCase)) return "High Contrast";
			if (string.Equals(preset, "Minimal Gray", StringComparison.OrdinalIgnoreCase)) return "Minimal Gray";
			if (string.Equals(preset, "Custom", StringComparison.OrdinalIgnoreCase)) return "Custom";
			return "Orca Dark";
		}

		private static void ApplyThemeDefaults(OrcaRiskManagerSettings output)
		{
			if (output.ThemePreset == "High Contrast") {
				output.BuyColor = "#FF00D26A"; output.SellColor = "#FFFF4C4C"; output.ActiveSelectedColor = "#FFFFFF00"; output.NeutralButtonColor = "#FF000000"; output.DangerFlattenColor = "#FFFF0000"; output.TextColor = "#FFFFFFFF";
				return;
			}
			if (output.ThemePreset == "Minimal Gray") {
				output.BuyColor = "#FF5AA469"; output.SellColor = "#FFD66A6A"; output.ActiveSelectedColor = "#FF9CA3AF"; output.NeutralButtonColor = "#FF3A3A3A"; output.DangerFlattenColor = "#FF9F3A3A"; output.TextColor = "#FFF3F4F6";
				return;
			}
			if (output.ThemePreset == "Custom") {
				output.BuyColor = ValidColor(output.BuyColor, "#FF44CC44");
				output.SellColor = ValidColor(output.SellColor, "#FFCC4444");
				output.ActiveSelectedColor = ValidColor(output.ActiveSelectedColor, "#FFCC9944");
				output.NeutralButtonColor = ValidColor(output.NeutralButtonColor, "#FF2A2A2A");
				output.DangerFlattenColor = ValidColor(output.DangerFlattenColor, "#80DC143C");
				output.TextColor = ValidColor(output.TextColor, "#FFF8F8FF");
				return;
			}
			output.BuyColor = "#FF44CC44"; output.SellColor = "#FFCC4444"; output.ActiveSelectedColor = "#FFCC9944"; output.NeutralButtonColor = "#FF2A2A2A"; output.DangerFlattenColor = "#80DC143C"; output.TextColor = "#FFF8F8FF";
		}

		private static void NormalizeChartLabelSettings(OrcaRiskManagerSettings output)
		{
			output.ChartLabelFontFamily = string.IsNullOrWhiteSpace(output.ChartLabelFontFamily) ? output.FontFamily : output.ChartLabelFontFamily.Trim();
			output.ChartLabelFontSize = Clamp(output.ChartLabelFontSize, 8, 28, output.FontSize);
			output.ChartLabelProfitColor = ValidColor(output.ChartLabelProfitColor, output.BuyColor);
			output.ChartLabelRiskColor = ValidColor(output.ChartLabelRiskColor, output.SellColor);
			output.ChartLabelTextColor = ValidColor(output.ChartLabelTextColor, output.TextColor);
		}

		private static string ValidColor(string value, string fallback)
		{
			try {
				if (!string.IsNullOrWhiteSpace(value))
					new BrushConverter().ConvertFrom(value);
				return string.IsNullOrWhiteSpace(value) ? fallback : value;
			} catch { return fallback; }
		}
	}

	public class OrcaRiskManagerWindow : Window
	{
		private static OrcaRiskManagerWindow instance;
		private readonly CheckBox enableHotkeysBox;
		private readonly CheckBox confirmLiveOrdersBox;
		private readonly CheckBox confirmFlattenBox;
		private readonly CheckBox enableLiveHotkeysBox;
		private readonly TextBox panelWidthBox;
		private readonly ComboBox fontFamilyBox;
		private readonly TextBox fontSizeBox;
		private readonly CheckBox showQuickActionsBox;
		private readonly CheckBox showPositionSizingBox;
		private readonly CheckBox showFastExecutionBox;
		private readonly CheckBox showClosePositionBox;
		private readonly CheckBox showManagePositionBox;
		private readonly CheckBox showPnlDisplayBox;
		private readonly ComboBox themePresetBox;
		private readonly TextBox buyColorBox;
		private readonly TextBox sellColorBox;
		private readonly TextBox activeColorBox;
		private readonly TextBox neutralColorBox;
		private readonly TextBox dangerColorBox;
		private readonly TextBox textColorBox;
		private readonly ComboBox chartLabelFontFamilyBox;
		private readonly TextBox chartLabelFontSizeBox;
		private readonly TextBox chartLabelProfitColorBox;
		private readonly TextBox chartLabelRiskColorBox;
		private readonly TextBox chartLabelTextColorBox;
		private readonly CheckBox enableSpacebarBuilderBox;
		private readonly ComboBox spacebarModeBox;
		private readonly CheckBox showBracketLabelsBox;
		private readonly TextBox defaultStopTicksBox;
		private readonly TextBox defaultTargetRBox;
		private readonly CheckBox keepBracketAfterReleaseBox;
		private readonly TextBox submitHotkeyBox;
		private readonly TextBox cancelHotkeyBox;
		private readonly TextBlock statusText;

		private OrcaRiskManagerWindow()
		{
			Title = "Orca Risk Manager";
			Width = 430;
			Height = 680;
			MinWidth = 340;
			MinHeight = 360;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Background = (Brush)new BrushConverter().ConvertFrom("#FF1B1B1B");
			Foreground = Brushes.GhostWhite;

			ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
			StackPanel root = new StackPanel { Margin = new Thickness(14) };
			scroll.Content = root;
			root.Children.Add(new TextBlock { Text = "Orca Risk Manager", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) });

			AddSectionHeader(root, "General");
			enableHotkeysBox = AddCheck(root, "Enable hotkeys");
			confirmLiveOrdersBox = AddCheck(root, "Confirm live orders");
			confirmFlattenBox = AddCheck(root, "Confirm flatten");
			enableLiveHotkeysBox = AddCheck(root, "Enable live-trading hotkeys");

			AddSectionHeader(root, "Panel");
			panelWidthBox = AddTextSetting(root, "Panel width", "235");
			fontFamilyBox = AddFontFamilySetting(root, "Font family");
			fontSizeBox = AddTextSetting(root, "Font size", "11");
			showQuickActionsBox = AddCheck(root, "Show Quick Actions");
			showPositionSizingBox = AddCheck(root, "Show Position Sizing");
			showFastExecutionBox = AddCheck(root, "Show Fast Execution");
			showClosePositionBox = AddCheck(root, "Show Close Position");
			showManagePositionBox = AddCheck(root, "Show Manage Position");
			showPnlDisplayBox = AddCheck(root, "Show PnL / R display");

			AddSectionHeader(root, "Spacebar Bracket Builder");
			enableSpacebarBuilderBox = AddCheck(root, "Enable Spacebar Bracket Builder");
			spacebarModeBox = AddComboSetting(root, "Default mode", new[] { "Stage", "Live" });
			showBracketLabelsBox = AddCheck(root, "Show bracket preview risk labels");
			defaultStopTicksBox = AddTextSetting(root, "Default stop distance ticks", "20");
			defaultTargetRBox = AddTextSetting(root, "Default target multiple R", "2.0");
			keepBracketAfterReleaseBox = AddCheck(root, "Keep bracket after Space release");
			submitHotkeyBox = AddTextSetting(root, "Submit staged bracket hotkey", "Enter");
			cancelHotkeyBox = AddTextSetting(root, "Cancel staged bracket hotkey", "Escape");

			AddSectionHeader(root, "Simple Visuals");
			themePresetBox = AddComboSetting(root, "Theme preset", new[] { "Orca Dark", "High Contrast", "Minimal Gray", "Custom" });
			buyColorBox = AddTextSetting(root, "Buy color", "#FF44CC44");
			sellColorBox = AddTextSetting(root, "Sell color", "#FFCC4444");
			activeColorBox = AddTextSetting(root, "Active / selected color", "#FFCC9944");
			neutralColorBox = AddTextSetting(root, "Neutral button color", "#FF2A2A2A");
			dangerColorBox = AddTextSetting(root, "Danger / flatten color", "#80DC143C");
			textColorBox = AddTextSetting(root, "Text color", "#FFF8F8FF");

			AddSectionHeader(root, "Chart Labels");
			chartLabelFontFamilyBox = AddFontFamilySetting(root, "Label font family");
			chartLabelFontSizeBox = AddTextSetting(root, "Label font size", "11");
			chartLabelProfitColorBox = AddTextSetting(root, "Profit / long color", "#FF44CC44");
			chartLabelRiskColorBox = AddTextSetting(root, "Risk / short color", "#FFCC4444");
			chartLabelTextColorBox = AddTextSetting(root, "Label text color", "#FFF8F8FF");

			statusText = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightGray, Margin = new Thickness(0, 4, 0, 16) };
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

		private void AddSectionHeader(Panel root, string text)
		{
			root.Children.Add(new TextBlock { Text = text, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.GhostWhite, Margin = new Thickness(0, 10, 0, 8) });
		}

		private CheckBox AddCheck(Panel root, string label)
		{
			CheckBox box = new CheckBox { Content = label, Margin = new Thickness(0, 0, 0, 7), Foreground = Brushes.GhostWhite };
			root.Children.Add(box);
			return box;
		}

		private TextBox AddTextSetting(Panel root, string label, string fallback)
		{
			root.Children.Add(new TextBlock { Text = label, Foreground = Brushes.LightGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
			TextBox box = new TextBox { Text = fallback, Margin = new Thickness(0, 0, 0, 8) };
			root.Children.Add(box);
			return box;
		}

		private ComboBox AddFontFamilySetting(Panel root, string label)
		{
			root.Children.Add(new TextBlock { Text = label, Foreground = Brushes.LightGray, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
			ComboBox box = new ComboBox { IsEditable = true, IsTextSearchEnabled = true, Margin = new Thickness(0, 0, 0, 8), MaxDropDownHeight = 280 };
			var fontNames = Fonts.SystemFontFamilies
				.Select(f => f.Source)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(name => name)
				.ToList();
			if (!fontNames.Any(name => string.Equals(name, "Segoe UI", StringComparison.OrdinalIgnoreCase)))
				box.Items.Add("Segoe UI");
			foreach (string fontName in fontNames)
				box.Items.Add(fontName);
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
				instance = new OrcaRiskManagerWindow();

			if (!instance.IsVisible)
				instance.Show();
			instance.Activate();
		}

		private void LoadSettings()
		{
			OrcaRiskManagerSettings settings = OrcaRiskManager.GetSettings();
			enableHotkeysBox.IsChecked = settings.EnableHotkeys;
			confirmLiveOrdersBox.IsChecked = settings.ConfirmLiveOrders;
			confirmFlattenBox.IsChecked = settings.ConfirmFlatten;
			enableLiveHotkeysBox.IsChecked = settings.EnableLiveTradingHotkeys;
			panelWidthBox.Text = settings.PanelWidth.ToString("0.##");
			fontFamilyBox.Text = settings.FontFamily;
			fontSizeBox.Text = settings.FontSize.ToString("0.##");
			showQuickActionsBox.IsChecked = settings.ShowQuickActions;
			showPositionSizingBox.IsChecked = settings.ShowPositionSizing;
			showFastExecutionBox.IsChecked = settings.ShowFastExecution;
			showClosePositionBox.IsChecked = settings.ShowClosePosition;
			showManagePositionBox.IsChecked = settings.ShowManagePosition;
			showPnlDisplayBox.IsChecked = settings.ShowPnlDisplay;
			themePresetBox.Text = settings.ThemePreset;
			buyColorBox.Text = settings.BuyColor;
			sellColorBox.Text = settings.SellColor;
			activeColorBox.Text = settings.ActiveSelectedColor;
			neutralColorBox.Text = settings.NeutralButtonColor;
			dangerColorBox.Text = settings.DangerFlattenColor;
			textColorBox.Text = settings.TextColor;
			chartLabelFontFamilyBox.Text = settings.ChartLabelFontFamily;
			chartLabelFontSizeBox.Text = settings.ChartLabelFontSize.ToString("0.##");
			chartLabelProfitColorBox.Text = settings.ChartLabelProfitColor;
			chartLabelRiskColorBox.Text = settings.ChartLabelRiskColor;
			chartLabelTextColorBox.Text = settings.ChartLabelTextColor;
			enableSpacebarBuilderBox.IsChecked = settings.EnableSpacebarBracketBuilder;
			spacebarModeBox.Text = settings.SpacebarDefaultMode;
			showBracketLabelsBox.IsChecked = settings.ShowBracketPreviewRiskLabels;
			defaultStopTicksBox.Text = settings.DefaultStopDistanceTicks.ToString();
			defaultTargetRBox.Text = settings.DefaultTargetMultipleR.ToString("0.##");
			keepBracketAfterReleaseBox.IsChecked = settings.KeepBracketAfterSpaceRelease;
			submitHotkeyBox.Text = settings.SubmitStagedBracketHotkey;
			cancelHotkeyBox.Text = settings.CancelStagedBracketHotkey;
			UpdateStatus();
		}

		private void Save()
		{
			OrcaRiskManager.SaveSettings(new OrcaRiskManagerSettings {
				EnableHotkeys = enableHotkeysBox.IsChecked == true,
				ConfirmLiveOrders = confirmLiveOrdersBox.IsChecked == true,
				ConfirmFlatten = confirmFlattenBox.IsChecked == true,
				EnableLiveTradingHotkeys = enableLiveHotkeysBox.IsChecked == true,
				PanelWidth = ParseDouble(panelWidthBox.Text, 235),
				FontFamily = fontFamilyBox.Text,
				FontSize = ParseDouble(fontSizeBox.Text, 11),
				ShowQuickActions = showQuickActionsBox.IsChecked == true,
				ShowPositionSizing = showPositionSizingBox.IsChecked == true,
				ShowFastExecution = showFastExecutionBox.IsChecked == true,
				ShowClosePosition = showClosePositionBox.IsChecked == true,
				ShowManagePosition = showManagePositionBox.IsChecked == true,
				ShowPnlDisplay = showPnlDisplayBox.IsChecked == true,
				ThemePreset = themePresetBox.Text,
				BuyColor = buyColorBox.Text,
				SellColor = sellColorBox.Text,
				ActiveSelectedColor = activeColorBox.Text,
				NeutralButtonColor = neutralColorBox.Text,
				DangerFlattenColor = dangerColorBox.Text,
				TextColor = textColorBox.Text,
				ChartLabelFontFamily = chartLabelFontFamilyBox.Text,
				ChartLabelFontSize = ParseDouble(chartLabelFontSizeBox.Text, 11),
				ChartLabelProfitColor = chartLabelProfitColorBox.Text,
				ChartLabelRiskColor = chartLabelRiskColorBox.Text,
				ChartLabelTextColor = chartLabelTextColorBox.Text,
				EnableSpacebarBracketBuilder = enableSpacebarBuilderBox.IsChecked == true,
				SpacebarDefaultMode = spacebarModeBox.Text,
				ShowBracketPreviewRiskLabels = showBracketLabelsBox.IsChecked == true,
				DefaultStopDistanceTicks = ParseInt(defaultStopTicksBox.Text, 20),
				DefaultTargetMultipleR = ParseDouble(defaultTargetRBox.Text, 2.0),
				KeepBracketAfterSpaceRelease = keepBracketAfterReleaseBox.IsChecked == true,
				SubmitStagedBracketHotkey = submitHotkeyBox.Text,
				CancelStagedBracketHotkey = cancelHotkeyBox.Text
			});
			LoadSettings();
		}

		private int ParseInt(string text, int fallback)
		{
			int value;
			return int.TryParse(text, out value) ? value : fallback;
		}

		private double ParseDouble(string text, double fallback)
		{
			double value;
			return double.TryParse(text, out value) ? value : fallback;
		}

		private void UpdateStatus()
		{
			bool fontsInstalled = IsFontFamilyInstalled(fontFamilyBox.Text) && IsFontFamilyInstalled(chartLabelFontFamilyBox.Text);
			string fontNote = fontsInstalled
				? ""
				: " Font not found in Windows; NinjaTrader will fall back until it is installed.";
			statusText.Text = enableSpacebarBuilderBox.IsChecked == true
				? "Saved settings apply to open Risk Manager panels." + fontNote
				: "Spacebar Bracket Builder is off by default. Enable it here when you are ready to stage brackets from the chart." + fontNote;
		}

		private bool IsFontFamilyInstalled(string family)
		{
			if (string.IsNullOrWhiteSpace(family))
				return true;

			string requested = family.Trim();
			foreach (FontFamily systemFamily in Fonts.SystemFontFamilies) {
				if (string.Equals(systemFamily.Source, requested, StringComparison.OrdinalIgnoreCase))
					return true;
				foreach (string name in systemFamily.FamilyNames.Values)
					if (string.Equals(name, requested, StringComparison.OrdinalIgnoreCase))
						return true;
			}
			return false;
		}
	}

	public class OrcaRiskPanel : System.Windows.Controls.UserControl
	{
		private ChartTab attachedTab;
		private Window hostWindow;
		private OrcaRiskManagerSettings riskSettings;
		private FontFamily riskFontFamily, chartLabelFontFamily;
		private System.Windows.Media.Brush riskBuyBrush, riskSellBrush, riskActiveBrush, riskNeutralBrush, riskDangerBrush, riskTextBrush, riskPanelBrush;
		private System.Windows.Media.Brush chartLabelProfitBrush, chartLabelRiskBrush, chartLabelTextBrush;
		private System.Windows.Threading.DispatcherTimer pnlTimer;
		private System.Windows.Threading.DispatcherTimer routedOverlayTimer;
		private System.Windows.Controls.Button btnLong, btnShort, btnMarket, btnLimit, btnStop, btnOpen, btnClose, btnBuyMkt, btnSellMkt, btnBuyAsk, btnSellBid, btnBreakeven, btnCloseAll, btnFixedDollar, btnFixedSize, btnFixedPoints, btnBuyLmt, btnSellLmt, btnBuyStop, btnSellStop;
		private System.Windows.Controls.TextBox txtContracts, txtRisk, txtPoints;
		private System.Windows.Controls.TextBlock txtPnL, txtUnrealR, txtRealR;
		private bool isDragOrderActive = false;
		private string dragOrderType = null;
		private System.Windows.Shapes.Line dragLine = null;
		private System.Windows.Controls.Canvas dragCanvas = null;
		private System.Windows.Controls.Border dragLabelPill = null;
		private System.Windows.Controls.TextBlock dragLabelTxt = null;
		private bool isDraggingRoutedTP = false, isDraggingRoutedSL = false, isDraggingRoutedOrder = false;
		private System.Windows.Shapes.Line routedDragLine = null;
		private System.Windows.Controls.Border routedDragPill = null;
		private System.Windows.Controls.TextBlock routedDragTxt = null;
		private Order routedDragOrder = null;
		private Account routedDragAccount = null;
		private Instrument routedDragInstrument = null;
		private MarketPosition routedDragSide = MarketPosition.Flat;
		private double routedDragEntryPrice = 0, routedDragPrice = 0;
		private int routedDragQuantity = 0;
		private string routedDragOco = "";
		private List<Order> routedDragOrders = null;
		private string lastProtectionSyncInstrument = "";
		private int lastProtectionSyncQuantity = -1;
		private bool isLongSelected = true, isFixedDollar = true, isFixedPoints = false, isCalculatorActive = false;
		private bool isSyncingCalculatorLines = false;
		private OrderType selectedOrderType = OrderType.Market;
		private string pendingEntryName = null;
		private double pendingStopPrice = 0, pendingTargetPrice = 0, baselineRealizedPnL = 0, currentTradeRealizedPnL = 0;
		private int pendingContracts = 0;
		private double lastCalcEntryPrice = double.NaN, lastCalcStopPrice = double.NaN, lastCalcTargetPrice = double.NaN;
		private Account hookedAccount = null;
		private static double totalSessionR = 0;
		private NinjaScriptBase calcOwner = null;
		private NinjaTrader.NinjaScript.DrawingTools.HorizontalLine hEntry, hStop, hTarget;
		private System.Windows.Controls.Canvas calcCanvas, routedOrderCanvas;
		private System.Windows.Controls.Border cEntryPill, cStopPill, cTargetPill;
		private System.Windows.Controls.TextBlock cEntryTxt, cStopTxt, cTargetTxt;
		private EventHandler renderHandler;
		private readonly List<Rect> routedLabelSlots = new List<Rect>();
		private bool isSpacebarPreviewActive = false, isBracketStaged = false;
		private bool isDraggingStagedEntry = false, isDraggingStagedStop = false, isDraggingStagedTarget = false;
		private bool isSyncingStagedPanel = false;
		private double stagedEntryPrice = 0, stagedStopPrice = 0, stagedTargetPrice = 0;
		private OrderType stagedEntryOrderType = OrderType.Limit;
		private OrderAction stagedEntryAction = OrderAction.Buy;
		private int stagedQuantity = 1;
		private System.Windows.Controls.Canvas stagedBracketCanvas;
		private System.Windows.Shapes.Line stagedEntryLine, stagedStopLine, stagedTargetLine;
		private System.Windows.Controls.Border stagedEntryPill, stagedStopPill, stagedTargetPill, stagedPlacePill, stagedCancelPill;
		private System.Windows.Controls.TextBlock stagedEntryTxt, stagedStopTxt, stagedTargetTxt, stagedPlaceTxt, stagedCancelTxt;

		public OrcaRiskPanel(ChartTab tab) {
			attachedTab = tab;
			OrcaRiskManager.SettingsChanged += OnRiskManagerSettingsChanged;
			BuildUI();
			Loaded += (s, e) => AttachWindowHotkeys();
			Unloaded += (s, e) => DetachWindowHotkeys();
			pnlTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			pnlTimer.Tick += UpdatePnL;
			pnlTimer.Start();
			routedOverlayTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
			routedOverlayTimer.Tick += UpdateRoutedOverlayFast;
			routedOverlayTimer.Start();
		}
		public void AttachToTab(ChartTab tab) {
			if (tab == null || object.ReferenceEquals(attachedTab, tab)) return;
			CancelDragOrder();
			CancelStagedBracket();
			RemoveCalculator();
			RemoveRoutedOrderOverlay();
			attachedTab = tab;
			UpdatePnL(null, null);
		}
		public void Cleanup() {
			OrcaRiskManager.SettingsChanged -= OnRiskManagerSettingsChanged;
			if (pnlTimer != null) pnlTimer.Stop();
			if (routedOverlayTimer != null) routedOverlayTimer.Stop();
			DetachWindowHotkeys();
			CancelStagedBracket();
			RemoveCalculator();
			RemoveRoutedOrderOverlay();
		}

		private void OnRiskManagerSettingsChanged(object sender, EventArgs e) {
			try {
				if (Dispatcher == null || Dispatcher.CheckAccess())
					RefreshRiskPanelFromSettings();
				else
					Dispatcher.InvokeAsync(() => RefreshRiskPanelFromSettings());
			} catch { }
		}

		private void RefreshRiskPanelFromSettings() {
			try {
				string contracts = txtContracts?.Text;
				string risk = txtRisk?.Text;
				string points = txtPoints?.Text;
				CancelDragOrder();
				BuildUI();
				if (!string.IsNullOrWhiteSpace(risk) && txtRisk != null) txtRisk.Text = risk;
				if (!string.IsNullOrWhiteSpace(points) && txtPoints != null) txtPoints.Text = points;
				if (!string.IsNullOrWhiteSpace(contracts) && txtContracts != null) txtContracts.Text = contracts;
				UpdateHostColumnWidth();
				UpdateCalcLabelVisuals();
				UpdateDirectionButtons();
				UpdateOrderModeButtons();
				UpdateSizeModeButtons();
				UpdatePnL(null, null);
			} catch { }
		}

		private void UpdateHostColumnWidth() {
			try {
				System.Windows.Controls.Grid grid = VisualTreeHelper.GetParent(this) as System.Windows.Controls.Grid;
				if (grid == null)
					return;
				int column = System.Windows.Controls.Grid.GetColumn(this);
				if (column >= 0 && column < grid.ColumnDefinitions.Count) {
					bool isVisible = Visibility == Visibility.Visible && grid.ColumnDefinitions[column].Width.Value > 0;
					grid.ColumnDefinitions[column].Width = isVisible
						? new GridLength(Math.Max(160, Math.Min(420, riskSettings.PanelWidth)))
						: new GridLength(0);
				}
			} catch { }
		}

		private void BuildUI() {
			ReloadRiskSettings();
			var G = new System.Windows.Controls.Grid { Background = riskPanelBrush, HorizontalAlignment = HorizontalAlignment.Stretch };
			var S = new System.Windows.Controls.ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalContentAlignment = HorizontalAlignment.Stretch };
			var P = new System.Windows.Controls.StackPanel { Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch };
			S.Content = P; G.Children.Add(S);
			System.Windows.Controls.Button CB(string t, System.Windows.Media.Brush b, RoutedEventHandler h = null) {
				var text = new System.Windows.Controls.TextBlock {
					Text = t,
					Foreground = riskTextBrush,
					FontFamily = riskFontFamily,
					TextAlignment = TextAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
				var content = new System.Windows.Controls.Grid {
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				};
				content.Children.Add(text);
				var btn = new System.Windows.Controls.Button {
					Content = content,
					Background = b, Foreground = riskTextBrush,
					FontSize = riskSettings.FontSize,
					FontWeight = FontWeights.Normal, Margin = new Thickness(1), Padding = new Thickness(0, 5, 0, 5),
					BorderThickness = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 0,
					HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch
				};
				if (h != null) btn.Click += h; return btn;
			}
			System.Windows.Controls.Border CS(string t, UIElement c) {
				var b = new System.Windows.Controls.Border { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Stretch };
				var sp = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; sp.Children.Add(new System.Windows.Controls.TextBlock { Text = t, Foreground = System.Windows.Media.Brushes.Gray, FontFamily = riskFontFamily, FontSize = 10, Margin = new Thickness(5, 2, 0, 2) });
				sp.Children.Add(c); b.Child = sp; return b;
			}
			System.Windows.Controls.Primitives.UniformGrid MG(int c) { return new System.Windows.Controls.Primitives.UniformGrid { Columns = c, Rows = 1, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 1, 0, 1) }; }
			System.Windows.Media.Brush red = riskSellBrush, green = riskBuyBrush, gray = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF444444"), dark = riskNeutralBrush, amber = riskActiveBrush, blue = System.Windows.Media.Brushes.SteelBlue;
			var titleRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 5, 0, 10), HorizontalAlignment = HorizontalAlignment.Stretch };
			titleRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(18) });
			titleRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			titleRow.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(18) });
			var titleText = new System.Windows.Controls.TextBlock { Text = "Orca Risk Manager", Foreground = riskTextBrush, FontFamily = riskFontFamily, FontSize = Math.Max(12, riskSettings.FontSize + 3), FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			System.Windows.Controls.Grid.SetColumn(titleText, 1);
			var hideText = new System.Windows.Controls.TextBlock { Text = "X", Foreground = riskTextBrush, FontFamily = riskFontFamily, FontSize = Math.Max(10, riskSettings.FontSize - 1), FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			var hideButton = new System.Windows.Controls.Border { Width = 18, Height = 18, Background = dark, BorderBrush = riskTextBrush, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Child = hideText, Cursor = Cursors.Hand, ToolTip = "Hide Risk Manager", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
			hideButton.MouseLeftButtonDown += (s, e) => { ToggleAttachedPanelVisibility(); e.Handled = true; };
			System.Windows.Controls.Grid.SetColumn(hideButton, 2);
			titleRow.Children.Add(titleText);
			titleRow.Children.Add(hideButton);
			P.Children.Add(titleRow);
			var qS = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; var rC = MG(2); rC.Children.Add(CB("Calc On", gray, (s, e) => SpawnCalculator())); rC.Children.Add(CB("Calc Off", gray, (s, e) => RemoveCalculator())); qS.Children.Add(rC);
			var rD = MG(2); btnLong = CB("Long", green, (s, e) => { isLongSelected = true; UpdateDirectionButtons(); MirrorCalculatorLines(); }); btnShort = CB("Short", dark, (s, e) => { isLongSelected = false; UpdateDirectionButtons(); MirrorCalculatorLines(); }); rD.Children.Add(btnLong); rD.Children.Add(btnShort); qS.Children.Add(rD);
			var rM = MG(3); btnMarket = CB("Market", amber, (s, e) => { selectedOrderType = OrderType.Market; UpdateOrderModeButtons(); }); btnLimit = CB("Limit", dark, (s, e) => { selectedOrderType = OrderType.Limit; UpdateOrderModeButtons(); }); btnStop = CB("Stop", dark, (s, e) => { selectedOrderType = OrderType.StopMarket; UpdateOrderModeButtons(); }); rM.Children.Add(btnMarket); rM.Children.Add(btnLimit); rM.Children.Add(btnStop); qS.Children.Add(rM);
			var rE = MG(2); btnOpen = CB("Open", blue, (s, e) => ExecuteTrade(selectedOrderType)); btnClose = CB("Close", dark, (s, e) => ClosePosition(100)); rE.Children.Add(btnOpen); rE.Children.Add(btnClose); qS.Children.Add(rE);
			if (riskSettings.ShowQuickActions) P.Children.Add(CS("\u26A1 Quick Actions", qS));
			var sP = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; var rSM = MG(3); btnFixedDollar = CB("$", amber, (s, e) => { isFixedDollar = true; isFixedPoints = false; UpdateSizeModeButtons(); }); btnFixedSize = CB("Size", dark, (s, e) => { isFixedDollar = false; isFixedPoints = false; UpdateSizeModeButtons(); }); btnFixedPoints = CB("Pts", dark, (s, e) => { isFixedDollar = false; isFixedPoints = true; ApplyPanelRiskPointsToCalculatorStop(); UpdateSizeModeButtons(); }); rSM.Children.Add(btnFixedDollar); rSM.Children.Add(btnFixedSize); rSM.Children.Add(btnFixedPoints); sP.Children.Add(rSM);
			System.Windows.Input.KeyEventHandler f = (s, e) => { var tb = s as System.Windows.Controls.TextBox; if (tb == null) return; bool iD = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9), iC = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab || e.Key == Key.Enter, iP = e.Key == Key.Decimal || e.Key == Key.OemPeriod; if (iD || iC || iP) { if (e.Key==Key.Enter) Keyboard.ClearFocus(); else if (!iC) { e.Handled=true; string inp = iD? (e.Key>=Key.D0?(e.Key-Key.D0).ToString():(e.Key-Key.NumPad0).ToString()) : "."; int st=tb.SelectionStart; if (tb.SelectionLength>0) tb.Text=tb.Text.Remove(st,tb.SelectionLength); tb.Text=tb.Text.Insert(st,inp); tb.SelectionStart=st+1; } } else e.Handled=true; };
			var at2 = MG(2); at2.Children.Add(new System.Windows.Controls.TextBlock{Text="Risk $", Foreground=System.Windows.Media.Brushes.Gray, FontFamily = riskFontFamily, FontSize = riskSettings.FontSize, VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center});
			txtRisk = new System.Windows.Controls.TextBox{Text="500", Margin=new Thickness(1), TextAlignment=TextAlignment.Center, FontFamily = riskFontFamily, FontSize = riskSettings.FontSize}; txtRisk.PreviewKeyDown += f; txtRisk.TextChanged += OnRiskSizingTextChanged;
			at2.Children.Add(txtRisk); sP.Children.Add(at2);
			var at3 = MG(2); at3.Children.Add(new System.Windows.Controls.TextBlock{Text="Risk Pts", Foreground=System.Windows.Media.Brushes.Gray, FontFamily = riskFontFamily, FontSize = riskSettings.FontSize, VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center});
			txtPoints = new System.Windows.Controls.TextBox{Text="0", Margin=new Thickness(1), TextAlignment=TextAlignment.Center, FontFamily = riskFontFamily, FontSize = riskSettings.FontSize}; txtPoints.PreviewKeyDown += f; txtPoints.TextChanged += OnRiskSizingTextChanged;
			txtPoints.MouseWheel += (s, ev) => { if (double.TryParse(txtPoints.Text, out double p)) { double tk = (GetActiveInstrument()?.MasterInstrument.TickSize ?? 0.25); double nP = Math.Max(tk, p + (ev.Delta > 0 ? tk : -tk)); txtPoints.Text = nP.ToString("F2"); if (isBracketStaged) { ApplyPanelRiskPointsToStagedStop(); ev.Handled = true; } else if (hEntry != null && hStop != null) { double eP = hEntry.StartAnchor.Price; hStop.StartAnchor.Price = hStop.EndAnchor.Price = isLongSelected ? eP - nP : eP + nP; UpdatePnL(null, null); } } };
			at3.Children.Add(txtPoints); sP.Children.Add(at3);
			var at1 = MG(2); at1.Children.Add(new System.Windows.Controls.TextBlock{Text="Contracts", Foreground=System.Windows.Media.Brushes.Gray, FontFamily = riskFontFamily, FontSize = riskSettings.FontSize, VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center});
			txtContracts = new System.Windows.Controls.TextBox{Text="1", Margin=new Thickness(1), TextAlignment=TextAlignment.Center, FontFamily = riskFontFamily, FontSize = riskSettings.FontSize}; txtContracts.PreviewKeyDown += f; txtContracts.TextChanged += OnRiskSizingTextChanged;
			txtContracts.MouseWheel += (s, ev) => { if (int.TryParse(txtContracts.Text, out int q)) { txtContracts.Text = Math.Max(1, q + (ev.Delta > 0 ? 1 : -1)).ToString(); if (isBracketStaged) UpdateStagedBracketFromPanelInputs(txtContracts); UpdatePnL(null, null); } };
			at1.Children.Add(txtContracts); sP.Children.Add(at1);
			var rSA = MG(3); rSA.Children.Add(CB("-1", dark, (s, e) => AdjustContractSize(-1))); rSA.Children.Add(CB("+1", dark, (s, e) => AdjustContractSize(1))); rSA.Children.Add(CB("Reset", dark, (s, e) => { txtContracts.Text="1"; })); sP.Children.Add(rSA);
			if (riskSettings.ShowPositionSizing) P.Children.Add(CS("\u2795 Position Sizing", sP));
			var fs = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; var f1 = MG(2); btnBuyMkt = CB("Buy Mkt", green, (s, e) => ExecuteFastCommand("BuyMkt")); btnSellMkt = CB("Sell Mkt", red, (s, e) => ExecuteFastCommand("SellMkt")); f1.Children.Add(btnBuyMkt); f1.Children.Add(btnSellMkt); fs.Children.Add(f1);
			var f2 = MG(2); btnBuyAsk = CB("Buy Ask", dark, (s, e) => ExecuteFastCommand("BuyAsk")); btnSellBid = CB("Sell Bid", dark, (s, e) => ExecuteFastCommand("SellBid")); f2.Children.Add(btnBuyAsk); f2.Children.Add(btnSellBid); fs.Children.Add(f2);
			var f3 = MG(2); btnBuyLmt = CB("Buy Limit", green, (s, e) => StartDragOrder("BuyLimit")); btnSellLmt = CB("Sell Limit", red, (s, e) => StartDragOrder("SellLimit")); f3.Children.Add(btnBuyLmt); f3.Children.Add(btnSellLmt); fs.Children.Add(f3);
			var f4 = MG(2); btnBuyStop = CB("Buy Stop", green, (s, e) => StartDragOrder("BuyStop")); btnSellStop = CB("Sell Stop", red, (s, e) => StartDragOrder("SellStop")); f4.Children.Add(btnBuyStop); f4.Children.Add(btnSellStop); fs.Children.Add(f4);
			btnBreakeven = CB("Move To Breakeven", System.Windows.Media.Brushes.DodgerBlue, (s, e) => MoveToBreakeven()); fs.Children.Add(btnBreakeven);
			if (riskSettings.ShowFastExecution) P.Children.Add(CS("\u26A1 Fast Execution", fs));
			var cl = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; var rPct = MG(3); var btn25 = CB("25%", dark, (s, e) => ClosePosition(25)); var btn50 = CB("50%", dark, (s, e) => ClosePosition(50)); var btn75 = CB("75%", dark, (s, e) => ClosePosition(75)); rPct.Children.Add(btn25); rPct.Children.Add(btn50); rPct.Children.Add(btn75); cl.Children.Add(rPct); btnCloseAll = CB("Flatten", (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#80DC143C"), (s, e) => Flatten()); cl.Children.Add(btnCloseAll); if (riskSettings.ShowClosePosition) P.Children.Add(CS("\u2796 Close Position", cl));
			btnCloseAll.Background = riskDangerBrush;
			var mn = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; txtPnL = new System.Windows.Controls.TextBlock { Text = "$0.00", Foreground = System.Windows.Media.Brushes.LightGray, FontFamily = riskFontFamily, FontSize = Math.Max(12, riskSettings.FontSize + 3), FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center }; mn.Children.Add(txtPnL);
			var mR = MG(2); mR.Margin = new Thickness(0, 8, 0, 5); txtUnrealR = new System.Windows.Controls.TextBlock{Text="Unrealized: 0.0R", Foreground = riskTextBrush, FontFamily = riskFontFamily, FontSize=riskSettings.FontSize, HorizontalAlignment=HorizontalAlignment.Center}; txtRealR = new System.Windows.Controls.TextBlock{Text="Realized: 0.0R", Foreground = riskTextBrush, FontFamily = riskFontFamily, FontSize=riskSettings.FontSize, HorizontalAlignment=HorizontalAlignment.Center}; mR.Children.Add(txtUnrealR); mR.Children.Add(txtRealR); mn.Children.Add(mR); if (riskSettings.ShowManagePosition && riskSettings.ShowPnlDisplay) P.Children.Add(CS("\u2699 Manage Position", mn));
			this.Content = G;
		}

		private void ReloadRiskSettings() {
			riskSettings = OrcaRiskManager.GetSettings();
			riskFontFamily = GetRiskFontFamily(riskSettings.FontFamily);
			riskBuyBrush = GetRiskBrush(riskSettings.BuyColor, (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF44CC44"));
			riskSellBrush = GetRiskBrush(riskSettings.SellColor, (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFCC4444"));
			riskActiveBrush = GetRiskBrush(riskSettings.ActiveSelectedColor, (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFCC9944"));
			riskNeutralBrush = GetRiskBrush(riskSettings.NeutralButtonColor, (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF2A2A2A"));
			riskDangerBrush = GetRiskBrush(riskSettings.DangerFlattenColor, (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#80DC143C"));
			riskTextBrush = GetRiskBrush(riskSettings.TextColor, System.Windows.Media.Brushes.GhostWhite);
			riskPanelBrush = GetRiskBrush(GetRiskPanelBackgroundColor(riskSettings.ThemePreset), (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF1B1B1B"));
			chartLabelFontFamily = GetRiskFontFamily(riskSettings.ChartLabelFontFamily);
			chartLabelProfitBrush = GetRiskBrush(riskSettings.ChartLabelProfitColor, riskBuyBrush);
			chartLabelRiskBrush = GetRiskBrush(riskSettings.ChartLabelRiskColor, riskSellBrush);
			chartLabelTextBrush = GetRiskBrush(riskSettings.ChartLabelTextColor, riskTextBrush);
		}
		private FontFamily GetRiskFontFamily(string family) { try { return new FontFamily(string.IsNullOrWhiteSpace(family) ? "Segoe UI" : family); } catch { return new FontFamily("Segoe UI"); } }
		private string GetRiskPanelBackgroundColor(string preset) {
			if (string.Equals(preset, "High Contrast", StringComparison.OrdinalIgnoreCase)) return "#FF000000";
			if (string.Equals(preset, "Minimal Gray", StringComparison.OrdinalIgnoreCase)) return "#FF26282C";
			return "#FF1B1B1B";
		}
		private System.Windows.Media.Brush GetRiskBrush(string text, System.Windows.Media.Brush fallback) { try { return (System.Windows.Media.Brush)new BrushConverter().ConvertFrom(text); } catch { return fallback; } }
		private void OnRiskSizingTextChanged(object sender, TextChangedEventArgs e) {
			if (isSyncingStagedPanel) return;
			if (isCalculatorActive && isFixedPoints && sender == txtPoints && txtPoints != null && txtPoints.IsFocused)
				ApplyPanelRiskPointsToCalculatorStop();
			if (!isBracketStaged) return;
			UpdateStagedBracketFromPanelInputs(sender);
		}
		private void UpdateDirectionButtons() { if (btnLong != null) btnLong.Background = isLongSelected? riskBuyBrush: riskNeutralBrush; if (btnShort != null) btnShort.Background = !isLongSelected? riskSellBrush: riskNeutralBrush; UpdateCalcLabelVisuals(); UpdatePnL(null, null); }
		private void UpdateOrderModeButtons() { if (btnMarket != null) btnMarket.Background = selectedOrderType == OrderType.Market? riskActiveBrush: riskNeutralBrush; if (btnLimit != null) btnLimit.Background = selectedOrderType == OrderType.Limit? riskActiveBrush: riskNeutralBrush; if (btnStop != null) btnStop.Background = selectedOrderType == OrderType.StopMarket? riskActiveBrush: riskNeutralBrush; UpdateCalcLabelVisuals(); UpdatePnL(null, null); }
		private void UpdateSizeModeButtons() { if (btnFixedDollar != null) btnFixedDollar.Background = isFixedDollar? riskActiveBrush: riskNeutralBrush; if (btnFixedSize != null) btnFixedSize.Background = (!isFixedDollar && !isFixedPoints)? riskActiveBrush: riskNeutralBrush; if (btnFixedPoints != null) btnFixedPoints.Background = isFixedPoints? riskActiveBrush: riskNeutralBrush; if (isBracketStaged && !isSyncingStagedPanel) ApplyStagedBracketToRiskPanel(); UpdatePnL(null, null); }
		private void MirrorCalculatorLines() { if (hEntry == null) return; double e = hEntry.StartAnchor.Price, sD = Math.Abs(e - hStop.StartAnchor.Price), tD = Math.Abs(e - hTarget.StartAnchor.Price); if (isLongSelected) { hStop.StartAnchor.Price = hStop.EndAnchor.Price = e - sD; hTarget.StartAnchor.Price = hTarget.EndAnchor.Price = e + tD; } else { hStop.StartAnchor.Price = hStop.EndAnchor.Price = e + sD; hTarget.StartAnchor.Price = hTarget.EndAnchor.Price = e - tD; } attachedTab.ChartControl.InvalidateVisual(); }
		private void ApplyPanelRiskPointsToCalculatorStop() {
			try {
				double points;
				if (txtPoints == null || hEntry == null || hStop == null || !double.TryParse(txtPoints.Text, out points) || points <= 0) return;
				double entry = hEntry.StartAnchor.Price;
				double stop = RoundToTick(isLongSelected ? entry - points : entry + points);
				isSyncingCalculatorLines = true;
				hStop.StartAnchor.Price = hStop.EndAnchor.Price = stop;
				isSyncingCalculatorLines = false;
				SnapshotCalculatorPrices();
				UpdatePnL(null, null);
			} catch { isSyncingCalculatorLines = false; }
		}
		private double GetChartLabelFontSize() {
			double value = riskSettings != null && riskSettings.ChartLabelFontSize > 0 ? riskSettings.ChartLabelFontSize : (riskSettings?.FontSize ?? 11);
			return Math.Max(8, value);
		}
		private System.Windows.Media.Brush GetChartProfitBrush() { return chartLabelProfitBrush ?? riskBuyBrush; }
		private System.Windows.Media.Brush GetChartRiskBrush() { return chartLabelRiskBrush ?? riskSellBrush; }
		private System.Windows.Media.Brush GetChartEntryBrush(bool isLong) { return isLong ? GetChartProfitBrush() : GetChartRiskBrush(); }
		private void ApplyChartLabelTextStyle(System.Windows.Controls.TextBlock text, bool bold = true) {
			if (text == null) return;
			text.Foreground = chartLabelTextBrush ?? riskTextBrush;
			text.FontFamily = chartLabelFontFamily ?? riskFontFamily;
			text.FontWeight = bold ? FontWeights.Bold : FontWeights.Normal;
			text.FontSize = GetChartLabelFontSize();
			text.TextAlignment = TextAlignment.Center;
			text.VerticalAlignment = VerticalAlignment.Center;
		}
		private void UpdateCalcLabelVisuals() {
			try {
				if (cEntryTxt != null) ApplyChartLabelTextStyle(cEntryTxt);
				if (cStopTxt != null) ApplyChartLabelTextStyle(cStopTxt);
				if (cTargetTxt != null) ApplyChartLabelTextStyle(cTargetTxt);
				System.Windows.Media.Brush entryBrush = GetChartEntryBrush(isLongSelected);
				if (cEntryPill != null) cEntryPill.Background = GetRouterBackgroundBrush(entryBrush, 1);
				if (cStopPill != null) cStopPill.Background = GetRouterBackgroundBrush(GetChartRiskBrush(), 1);
				if (cTargetPill != null) cTargetPill.Background = GetRouterBackgroundBrush(GetChartProfitBrush(), 1);
				TrySetDrawingToolBrush(hEntry, entryBrush);
				TrySetDrawingToolBrush(hStop, GetChartRiskBrush());
				TrySetDrawingToolBrush(hTarget, GetChartProfitBrush());
			} catch { }
		}
		private void TrySetDrawingToolBrush(object drawingTool, System.Windows.Media.Brush brush) {
			try {
				if (drawingTool == null || brush == null) return;
				var brushProperty = drawingTool.GetType().GetProperty("Brush");
				if (brushProperty != null && brushProperty.CanWrite) {
					brushProperty.SetValue(drawingTool, brush, null);
					return;
				}
				var strokeProperty = drawingTool.GetType().GetProperty("Stroke");
				object stroke = strokeProperty?.GetValue(drawingTool, null);
				var strokeBrushProperty = stroke?.GetType().GetProperty("Brush");
				if (strokeBrushProperty != null && strokeBrushProperty.CanWrite)
					strokeBrushProperty.SetValue(stroke, brush, null);
			} catch { }
		}
		private void AdjustContractSize(int a) { if (int.TryParse(txtContracts.Text, out int c)) txtContracts.Text = Math.Max(1, c + a).ToString(); else txtContracts.Text = "1"; if (isBracketStaged) UpdateStagedBracketFromPanelInputs(txtContracts); }
		private Account GetActiveAccount() { Chart cw = Window.GetWindow(attachedTab) as Chart; if (cw?.ChartTrader != null) return cw.ChartTrader.Account; return Account.All.FirstOrDefault(a => a.Name == "Sim101"); }
		private Instrument GetChartInstrument() {
			try {
				if (attachedTab?.ChartControl?.Instrument != null)
					return attachedTab.ChartControl.Instrument;
				var chartBars = attachedTab?.ChartControl?.BarsArray?.FirstOrDefault(x => x.Bars?.Instrument != null);
				if (chartBars?.Bars?.Instrument != null)
					return chartBars.Bars.Instrument;
			} catch { }
			return (Window.GetWindow(attachedTab) as Chart)?.ChartTrader?.Instrument;
		}
		private Instrument GetActiveInstrument() { Chart cw = Window.GetWindow(attachedTab) as Chart; return OrcaExecutionRouter.ResolveExecutionInstrument(GetChartInstrument(), cw?.ChartTrader?.Instrument); }
		private bool IsSameInstrument(Instrument left, Instrument right) { if (left == null || right == null) return false; if (object.ReferenceEquals(left, right)) return true; return string.Equals(left.FullName, right.FullName, StringComparison.OrdinalIgnoreCase); }
		private double GetActivePrice() { Instrument chartInstrument = GetChartInstrument(); var cb = attachedTab?.ChartControl?.BarsArray.FirstOrDefault(x => x.Bars?.Instrument?.FullName == chartInstrument?.FullName) ?? attachedTab?.ChartControl?.BarsArray.FirstOrDefault(); return cb?.Bars?.GetClose(cb.Bars.Count-1) ?? 0; }
		private void UpdateRoutedOverlayFast(object s, EventArgs e) {
			try {
				Account acc = GetActiveAccount();
				Instrument ins = GetActiveInstrument();
				if (acc == null || ins == null) return;
				UpdateRoutedOrderOverlay(acc, ins);
			} catch { }
		}
		private void UpdatePnL(object s, EventArgs e) {
			try {
				Account acc = GetActiveAccount(); HookExecutionEvent(acc); Instrument ins = GetActiveInstrument(); if (acc == null || ins == null) return;
				CheckPendingEntryState(acc);
				Position pos = acc.Positions.FirstOrDefault(p => IsSameInstrument(p.Instrument, ins));
				if (pos != null && pos.MarketPosition != MarketPosition.Flat) {
					double pnl = pos.GetUnrealizedProfitLoss(PerformanceUnit.Currency, GetActivePrice()); txtPnL.Text = pnl.ToString("C2"); txtPnL.Foreground = pnl >= 0? System.Windows.Media.Brushes.LightGreen: System.Windows.Media.Brushes.Salmon;
					double risk = 500; if (double.TryParse(txtRisk.Text, out double r)) risk = r; double uR = risk > 0? pnl / risk: 0; txtUnrealR.Text = $"Unrealized: {uR:N1}R"; txtUnrealR.Foreground = uR >= 0? System.Windows.Media.Brushes.LimeGreen: System.Windows.Media.Brushes.Salmon;
					double rR = risk > 0? currentTradeRealizedPnL / risk: 0; txtRealR.Text = $"Realized: {rR:N1}R";
					SyncProtectionOrdersOnPositionChange(acc, ins, Math.Abs(pos.Quantity));
				} else { txtPnL.Text = "$0.00"; txtPnL.Foreground = System.Windows.Media.Brushes.LightGray; txtUnrealR.Text = "Unrealized: 0.0R"; txtRealR.Text = $"Realized: 0.0R"; SyncProtectionOrdersOnPositionChange(acc, ins, 0); }
				UpdateRoutedOrderOverlay(acc, ins);
				if (isCalculatorActive && hEntry != null && hStop != null && hEntry.StartAnchor != null && hStop.StartAnchor != null) {
					EnforceFixedPointCalculatorBracket(false);
					double ent = hEntry.StartAnchor.Price, stp = hStop.StartAnchor.Price, tar = (hTarget != null && hTarget.StartAnchor != null) ? hTarget.StartAnchor.Price : 0;
					if (selectedOrderType == OrderType.Market) {
							double cur = GetActivePrice();
							if (Math.Abs(ent-cur)>0.000001) {
								double delta = cur - ent;
								isSyncingCalculatorLines = true;
								try {
									hEntry.StartAnchor.Price=hEntry.EndAnchor.Price=cur;
									ent=cur;
									if (isFixedPoints) {
										hStop.StartAnchor.Price = hStop.EndAnchor.Price = RoundToTick(stp + delta);
										stp = hStop.StartAnchor.Price;
										if (hTarget != null && hTarget.StartAnchor != null) {
											hTarget.StartAnchor.Price = hTarget.EndAnchor.Price = RoundToTick(hTarget.StartAnchor.Price + delta);
											tar = hTarget.StartAnchor.Price;
										}
									}
								} finally {
									isSyncingCalculatorLines = false;
								}
								SnapshotCalculatorPrices();
							}
						}
					double dist = Math.Abs(ent-stp), tick = ins.MasterInstrument.TickSize, val = ins.MasterInstrument.PointValue;
					if (!txtPoints.IsFocused) txtPoints.Text = dist.ToString("F2");
					if (isFixedDollar) { if (dist > 0 && double.TryParse(txtRisk.Text, out double rd)) { int q = (int)Math.Max(1, Math.Floor(rd / (dist / tick * val * tick))); if (!txtContracts.IsFocused) txtContracts.Text = q.ToString(); } }
					else { int fq = 1; int.TryParse(txtContracts.Text, out fq); if (dist > 0) { double cr = dist / tick * val * tick * fq; if (!txtRisk.IsFocused) txtRisk.Text = cr.ToString("N0"); } }
					int cQ = 1; int.TryParse(txtContracts.Text, out cQ); double rAmt = Math.Abs(ent-stp)/tick*val*tick*cQ, pAmt = Math.Abs(tar-ent)/tick*val*tick*cQ; double riskDollar = 500; double.TryParse(txtRisk.Text, out riskDollar); double rR = riskDollar > 0 ? rAmt / riskDollar : 0, pR = rAmt > 0 ? pAmt / rAmt : 0;
					if (cEntryTxt!=null) cEntryTxt.Text = $"{(isLongSelected?"BUY":"SELL")} {cQ} @ {ent:F2}"; if (cStopTxt!=null) cStopTxt.Text = $"RISK: ${rAmt:N0} | {Math.Abs(ent-stp):F2}pts | {rR:F1}R"; if (cTargetTxt!=null) cTargetTxt.Text = $"PROFIT: ${pAmt:N0} | {Math.Abs(tar-ent):F2}pts | {pR:F1}R";
					attachedTab.ChartControl.InvalidateVisual();
				}
				if (isBracketStaged || isSpacebarPreviewActive) UpdateStagedBracketVisuals();
			} catch { }
		}
		private void OnRenderFrame(object s, EventArgs e) {
			if (!isCalculatorActive) return;
			try {
				if (hEntry == null || hStop == null || hTarget == null || hEntry.StartAnchor == null || hStop.StartAnchor == null || hTarget.StartAnchor == null) return;
				EnforceFixedPointCalculatorBracket(false);
				void SetP(System.Windows.Controls.Border b, double p) { double y = GetYByPrice(p); if (y > 0) { System.Windows.Controls.Canvas.SetTop(b, y - 12); b.Visibility = Visibility.Visible; } else b.Visibility = Visibility.Collapsed; }
				if (cEntryPill != null) SetP(cEntryPill, hEntry.StartAnchor.Price);
				if (cStopPill != null) SetP(cStopPill, hStop.StartAnchor.Price);
				if (cTargetPill != null) SetP(cTargetPill, hTarget.StartAnchor.Price);
			} catch { }
		}
		private void AttachWindowHotkeys() {
			if (hostWindow != null) return;
			DependencyObject source = attachedTab?.ChartControl as DependencyObject;
			if (source == null) source = this;
			hostWindow = Window.GetWindow(source);
			if (hostWindow == null) return;
			hostWindow.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(HostWindow_PreviewKeyDown), true);
			hostWindow.PreviewKeyUp += HostWindow_PreviewKeyUp;
		}
		private void DetachWindowHotkeys() {
			if (hostWindow == null) return;
			hostWindow.RemoveHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(HostWindow_PreviewKeyDown));
			hostWindow.PreviewKeyUp -= HostWindow_PreviewKeyUp;
			hostWindow = null;
		}
		public bool HandleSpacebarBuilderKeyDown(KeyEventArgs e) {
			try {
				riskSettings = OrcaRiskManager.GetSettings();
				if (IsTextInputFocused() || !riskSettings.EnableHotkeys) return false;
				if (e.Key == Key.Space && riskSettings.EnableSpacebarBracketBuilder) {
					if (!e.IsRepeat) StartSpacebarPreview();
					return true;
				}
				if (isBracketStaged && IsConfiguredHotkey(e, riskSettings.CancelStagedBracketHotkey, Key.Escape)) { CancelStagedBracket(); return true; }
				if (isBracketStaged && IsConfiguredHotkey(e, riskSettings.SubmitStagedBracketHotkey, Key.Enter)) { SubmitStagedBracket(); return true; }
			} catch { }
			return false;
		}
		public bool HandleSpacebarBuilderKeyUp(KeyEventArgs e) {
			try {
				if (e.Key == Key.Space && isSpacebarPreviewActive) {
					System.Windows.Threading.Dispatcher dispatcher = Dispatcher ?? attachedTab?.ChartControl?.Dispatcher;
					if (dispatcher != null) {
						dispatcher.InvokeAsync(() => {
							if (!Keyboard.IsKeyDown(Key.Space))
								StopSpacebarPreview();
						});
					} else if (!Keyboard.IsKeyDown(Key.Space)) {
						StopSpacebarPreview();
					}
					return true;
				}
			} catch { }
			return false;
		}
		private void HostWindow_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (OrcaRiskManagerAddOn.IsControlCToggleHotkey(e)) {
				return;
			}
			if (e.Handled || Visibility != Visibility.Visible) return;
			if (HandleSpacebarBuilderKeyDown(e))
				e.Handled = true;
		}
		private void HostWindow_PreviewKeyUp(object sender, KeyEventArgs e) {
			if (Visibility != Visibility.Visible) return;
			if (HandleSpacebarBuilderKeyUp(e))
				e.Handled = true;
		}
		private bool IsTextInputFocused() {
			object focused = Keyboard.FocusedElement;
			return focused is System.Windows.Controls.Primitives.TextBoxBase || focused is System.Windows.Controls.PasswordBox;
		}

		private bool ToggleAttachedPanelVisibility() {
			try {
				System.Windows.Controls.Grid grid = VisualTreeHelper.GetParent(this) as System.Windows.Controls.Grid;
				if (grid == null)
					return false;
				int column = System.Windows.Controls.Grid.GetColumn(this);
				if (column < 0 || column >= grid.ColumnDefinitions.Count)
					return false;
				bool isVisible = Visibility == Visibility.Visible && grid.ColumnDefinitions[column].Width.Value > 0;
				bool shouldShow = !isVisible;
				OrcaRiskManagerAddOn.SetPanelWindowVisible(Window.GetWindow(this), shouldShow);
				grid.ColumnDefinitions[column].Width = shouldShow
					? new GridLength(Math.Max(160, Math.Min(420, riskSettings?.PanelWidth ?? OrcaRiskManager.GetSettings().PanelWidth)))
					: new GridLength(0);
				Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
				HorizontalAlignment = HorizontalAlignment.Stretch;
				grid.InvalidateMeasure();
				grid.InvalidateArrange();
				return true;
			} catch { return false; }
		}

		private bool IsConfiguredHotkey(KeyEventArgs e, string setting, Key fallback) {
			Key parsed;
			if (string.IsNullOrWhiteSpace(setting) || !Enum.TryParse(setting.Replace(" ", ""), true, out parsed))
				parsed = fallback;
			return e.Key == parsed;
		}
		private void StartSpacebarPreview() {
			riskSettings = OrcaRiskManager.GetSettings();
			if (!riskSettings.EnableSpacebarBracketBuilder) return;
			if (isBracketStaged) {
				if (stagedBracketCanvas != null) stagedBracketCanvas.Background = null;
				return;
			}
			isSpacebarPreviewActive = true;
			EnsureStagedBracketCanvas();
			if (stagedBracketCanvas != null) {
				stagedBracketCanvas.Background = System.Windows.Media.Brushes.Transparent;
				stagedBracketCanvas.Cursor = Cursors.Cross;
				stagedBracketCanvas.UpdateLayout();
				Point point = Mouse.GetPosition(stagedBracketCanvas);
				if (point.X >= 0 && point.Y >= 0 && point.X <= Math.Max(stagedBracketCanvas.ActualWidth, attachedTab?.ChartControl?.ActualWidth ?? 0) && point.Y <= Math.Max(stagedBracketCanvas.ActualHeight, attachedTab?.ChartControl?.ActualHeight ?? 0)) {
					double price = RoundToTick(GetPriceByY(point.Y));
					if (price > 0) {
						ConfigureStagedEntry(price, MouseButton.Left);
						SetDefaultStagedBracketPrices();
						UpdateStagedBracketVisuals();
					}
				}
			}
		}
		private void StopSpacebarPreview() {
			isSpacebarPreviewActive = false;
			if (!isBracketStaged || !riskSettings.KeepBracketAfterSpaceRelease) {
				RemoveStagedBracketOverlay();
				return;
			}
			if (stagedBracketCanvas != null) {
				stagedBracketCanvas.Background = null;
				stagedBracketCanvas.Cursor = Cursors.Arrow;
			}
		}
		private void EnsureStagedBracketCanvas() {
			if (stagedBracketCanvas != null) return;
			if (!(attachedTab?.Content is System.Windows.Controls.Grid grid) || attachedTab.ChartControl == null) return;
			stagedBracketCanvas = new System.Windows.Controls.Canvas { ClipToBounds = true, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, Background = System.Windows.Media.Brushes.Transparent };
			System.Windows.Controls.Panel.SetZIndex(stagedBracketCanvas, 9997);
			AlignOverlayCanvasWithChart(stagedBracketCanvas);
			stagedBracketCanvas.MouseMove += StagedBracketCanvas_MouseMove;
			stagedBracketCanvas.PreviewMouseRightButtonDown += StagedBracketCanvas_PreviewMouseRightButtonDown;
			stagedBracketCanvas.MouseLeftButtonDown += StagedBracketCanvas_MouseLeftButtonDown;
			stagedBracketCanvas.MouseRightButtonDown += StagedBracketCanvas_MouseRightButtonDown;
			stagedBracketCanvas.MouseLeftButtonUp += StagedBracketCanvas_MouseButtonUp;
			stagedBracketCanvas.MouseRightButtonUp += StagedBracketCanvas_MouseButtonUp;
			stagedBracketCanvas.ContextMenuOpening += StagedBracketCanvas_ContextMenuOpening;
			grid.Children.Add(stagedBracketCanvas);
		}
		private void RemoveStagedBracketOverlay() {
			try {
				isSpacebarPreviewActive = false; isBracketStaged = false; isDraggingStagedEntry = isDraggingStagedStop = isDraggingStagedTarget = false;
				stagedEntryPrice = stagedStopPrice = stagedTargetPrice = 0;
				Mouse.Capture(null);
				if (stagedBracketCanvas != null) {
					stagedBracketCanvas.MouseMove -= StagedBracketCanvas_MouseMove;
					stagedBracketCanvas.PreviewMouseRightButtonDown -= StagedBracketCanvas_PreviewMouseRightButtonDown;
					stagedBracketCanvas.MouseLeftButtonDown -= StagedBracketCanvas_MouseLeftButtonDown;
					stagedBracketCanvas.MouseRightButtonDown -= StagedBracketCanvas_MouseRightButtonDown;
					stagedBracketCanvas.MouseLeftButtonUp -= StagedBracketCanvas_MouseButtonUp;
					stagedBracketCanvas.MouseRightButtonUp -= StagedBracketCanvas_MouseButtonUp;
					stagedBracketCanvas.ContextMenuOpening -= StagedBracketCanvas_ContextMenuOpening;
					(System.Windows.Media.VisualTreeHelper.GetParent(stagedBracketCanvas) as System.Windows.Controls.Panel)?.Children.Remove(stagedBracketCanvas);
				}
				stagedBracketCanvas = null; stagedEntryLine = stagedStopLine = stagedTargetLine = null; stagedEntryPill = stagedStopPill = stagedTargetPill = stagedPlacePill = stagedCancelPill = null; stagedEntryTxt = stagedStopTxt = stagedTargetTxt = stagedPlaceTxt = stagedCancelTxt = null;
			} catch { }
		}
		private void StagedBracketCanvas_MouseMove(object sender, MouseEventArgs e) {
			try {
				if (stagedBracketCanvas == null) return;
				Point point = e.GetPosition(stagedBracketCanvas);
				double price = RoundToTick(GetPriceByY(point.Y));
				if (price <= 0) return;
				if (isDraggingStagedEntry || isDraggingStagedStop || isDraggingStagedTarget) {
					DragStagedBracketLine(price);
					e.Handled = true;
					return;
				}
				if (isSpacebarPreviewActive && !isBracketStaged) {
					ConfigureStagedEntry(price, MouseButton.Left);
					SetDefaultStagedBracketPrices();
					UpdateStagedBracketVisuals();
				}
			} catch { }
		}
		private void StagedBracketCanvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			if (!isSpacebarPreviewActive) return;
			e.Handled = true;
			if (isBracketStaged) return;
			Point point = e.GetPosition(stagedBracketCanvas);
			StageBracketAt(GetPriceByY(point.Y), MouseButton.Right);
		}
		private void StagedBracketCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (!isSpacebarPreviewActive) return;
			e.Handled = true;
			if (isBracketStaged) return;
			Point point = e.GetPosition(stagedBracketCanvas);
			StageBracketAt(GetPriceByY(point.Y), MouseButton.Left);
		}
		private void StagedBracketCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			if (!isSpacebarPreviewActive) return;
			e.Handled = true;
			if (isBracketStaged) return;
			Point point = e.GetPosition(stagedBracketCanvas);
			StageBracketAt(GetPriceByY(point.Y), MouseButton.Right);
		}
		private void StagedBracketCanvas_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			if (isSpacebarPreviewActive || isBracketStaged)
				e.Handled = true;
		}
		private void StagedBracketCanvas_MouseButtonUp(object sender, MouseButtonEventArgs e) {
			if (!isDraggingStagedEntry && !isDraggingStagedStop && !isDraggingStagedTarget) return;
			isDraggingStagedEntry = isDraggingStagedStop = isDraggingStagedTarget = false;
			Mouse.Capture(null);
			ApplyStagedBracketToRiskPanel();
			e.Handled = true;
		}
		private bool StageBracketAt(double price, MouseButton button) {
			price = RoundToTick(price);
			if (price <= 0) return false;
			riskSettings = OrcaRiskManager.GetSettings();
			ConfigureStagedEntry(price, button);
			SetDefaultStagedBracketPrices();
			isBracketStaged = true;
			ApplyStagedBracketToRiskPanel();
			UpdateStagedBracketVisuals();
			if (string.Equals(riskSettings.SpacebarDefaultMode, "Live", StringComparison.OrdinalIgnoreCase))
				SubmitStagedBracket();
			return true;
		}
		private void ConfigureStagedEntry(double price, MouseButton button) {
			double market = GetActivePrice();
			bool belowMarket = market <= 0 || price < market;
			stagedEntryPrice = RoundToTick(price);
			if (button == MouseButton.Right) {
				stagedEntryAction = OrderAction.SellShort;
				stagedEntryOrderType = belowMarket ? OrderType.StopMarket : OrderType.Limit;
			} else {
				stagedEntryAction = OrderAction.Buy;
				stagedEntryOrderType = belowMarket ? OrderType.Limit : OrderType.StopMarket;
			}
		}
		private void SetDefaultStagedBracketPrices() {
			Instrument ins = GetActiveInstrument() ?? GetChartInstrument();
			double tick = ins?.MasterInstrument?.TickSize ?? 0.25;
			double stopDistance = GetDefaultSpacebarStopDistance(tick);
			double targetDistance = stopDistance * Math.Max(0.25, riskSettings.DefaultTargetMultipleR);
			if (IsStagedLong()) {
				stagedStopPrice = RoundToTick(stagedEntryPrice - stopDistance);
				stagedTargetPrice = RoundToTick(stagedEntryPrice + targetDistance);
			} else {
				stagedStopPrice = RoundToTick(stagedEntryPrice + stopDistance);
				stagedTargetPrice = RoundToTick(stagedEntryPrice - targetDistance);
			}
		}
		private double GetDefaultSpacebarStopDistance(double tick) {
			double panelPoints;
			if (txtPoints != null && double.TryParse(txtPoints.Text, out panelPoints) && panelPoints > 0)
				return Math.Max(tick, RoundToTick(panelPoints));
			return Math.Max(1, riskSettings.DefaultStopDistanceTicks) * tick;
		}
		private void BeginDragStagedEntry(object sender, MouseButtonEventArgs e) { if (!isBracketStaged) return; isDraggingStagedEntry = true; isDraggingStagedStop = isDraggingStagedTarget = false; Mouse.Capture(stagedBracketCanvas); e.Handled = true; }
		private void BeginDragStagedStop(object sender, MouseButtonEventArgs e) { if (!isBracketStaged) return; isDraggingStagedStop = true; isDraggingStagedEntry = isDraggingStagedTarget = false; Mouse.Capture(stagedBracketCanvas); e.Handled = true; }
		private void BeginDragStagedTarget(object sender, MouseButtonEventArgs e) { if (!isBracketStaged) return; isDraggingStagedTarget = true; isDraggingStagedEntry = isDraggingStagedStop = false; Mouse.Capture(stagedBracketCanvas); e.Handled = true; }
		private void DragStagedBracketLine(double price) {
			price = RoundToTick(price);
			if (price <= 0) return;
			if (isDraggingStagedEntry) {
				double stopOffset = stagedStopPrice - stagedEntryPrice;
				double targetOffset = stagedTargetPrice - stagedEntryPrice;
				stagedEntryPrice = price;
				stagedStopPrice = RoundToTick(stagedEntryPrice + stopOffset);
				stagedTargetPrice = RoundToTick(stagedEntryPrice + targetOffset);
			} else if (isDraggingStagedStop) stagedStopPrice = price;
			else if (isDraggingStagedTarget) stagedTargetPrice = price;
			if (isDraggingStagedEntry) RefreshStagedOrderTypeFromMarket();
			NormalizeStagedBracketSides();
			ApplyStagedBracketToRiskPanel();
			UpdateStagedBracketVisuals();
		}
		private void RefreshStagedOrderTypeFromMarket() {
			double market = GetActivePrice();
			if (market <= 0 || stagedEntryPrice <= 0) return;
			if (IsStagedLong()) stagedEntryOrderType = stagedEntryPrice < market ? OrderType.Limit : OrderType.StopMarket;
			else stagedEntryOrderType = stagedEntryPrice < market ? OrderType.StopMarket : OrderType.Limit;
		}
		private void NormalizeStagedBracketSides() {
			double tick = (GetActiveInstrument() ?? GetChartInstrument())?.MasterInstrument?.TickSize ?? 0.25;
			if (IsStagedLong()) {
				if (stagedStopPrice >= stagedEntryPrice) stagedStopPrice = RoundToTick(stagedEntryPrice - tick);
				if (stagedTargetPrice <= stagedEntryPrice) stagedTargetPrice = RoundToTick(stagedEntryPrice + tick);
			} else {
				if (stagedStopPrice <= stagedEntryPrice) stagedStopPrice = RoundToTick(stagedEntryPrice + tick);
				if (stagedTargetPrice >= stagedEntryPrice) stagedTargetPrice = RoundToTick(stagedEntryPrice - tick);
			}
		}
		private void ApplyStagedBracketToRiskPanel() {
			if (!isBracketStaged && !isSpacebarPreviewActive) return;
			isLongSelected = IsStagedLong();
			selectedOrderType = stagedEntryOrderType;
			isSyncingStagedPanel = true;
			try {
				if (txtPoints != null) txtPoints.Text = GetStagedRiskPoints().ToString("F2");
				if (isFixedDollar) RecalculateQuantityFromRisk();
				else RecalculateRiskFromQuantity();
				stagedQuantity = ParseQuantity();
			} finally { isSyncingStagedPanel = false; }
			if (btnLong != null) btnLong.Background = isLongSelected ? riskBuyBrush : riskNeutralBrush;
			if (btnShort != null) btnShort.Background = !isLongSelected ? riskSellBrush : riskNeutralBrush;
			if (btnMarket != null) btnMarket.Background = selectedOrderType == OrderType.Market ? riskActiveBrush : riskNeutralBrush;
			if (btnLimit != null) btnLimit.Background = selectedOrderType == OrderType.Limit ? riskActiveBrush : riskNeutralBrush;
			if (btnStop != null) btnStop.Background = selectedOrderType == OrderType.StopMarket ? riskActiveBrush : riskNeutralBrush;
			if (btnFixedDollar != null) btnFixedDollar.Background = isFixedDollar ? riskActiveBrush : riskNeutralBrush;
			if (btnFixedSize != null) btnFixedSize.Background = (!isFixedDollar && !isFixedPoints) ? riskActiveBrush : riskNeutralBrush;
			if (btnFixedPoints != null) btnFixedPoints.Background = isFixedPoints ? riskActiveBrush : riskNeutralBrush;
		}
		private void UpdateStagedBracketFromPanelInputs(object sender) {
			if (!isBracketStaged) return;
			if (sender == txtPoints) ApplyPanelRiskPointsToStagedStop();
			else if (sender == txtRisk && isFixedDollar) RecalculateQuantityFromRisk();
			else if (sender == txtContracts && !isFixedDollar) RecalculateRiskFromQuantity();
			stagedQuantity = ParseQuantity();
			UpdateStagedBracketVisuals();
		}
		private void ApplyPanelRiskPointsToStagedStop() {
			double points;
			if (txtPoints == null || !double.TryParse(txtPoints.Text, out points) || points <= 0) return;
			stagedStopPrice = RoundToTick(IsStagedLong() ? stagedEntryPrice - points : stagedEntryPrice + points);
			NormalizeStagedBracketSides();
			if (isFixedDollar) RecalculateQuantityFromRisk();
			else RecalculateRiskFromQuantity();
		}
		private void RecalculateQuantityFromRisk() {
			Instrument ins = GetActiveInstrument() ?? GetChartInstrument();
			double risk;
			if (ins == null || txtRisk == null || !double.TryParse(txtRisk.Text, out risk)) return;
			double dollarsPerContract = GetStagedRiskPoints() * ins.MasterInstrument.PointValue;
			if (dollarsPerContract <= 0) return;
			int q = (int)Math.Max(1, Math.Floor(risk / dollarsPerContract));
			if (txtContracts != null && !txtContracts.IsFocused) txtContracts.Text = q.ToString();
			stagedQuantity = txtContracts != null && txtContracts.IsFocused ? ParseQuantity() : q;
		}
		private void RecalculateRiskFromQuantity() {
			Instrument ins = GetActiveInstrument() ?? GetChartInstrument();
			if (ins == null || txtRisk == null) return;
			int q = ParseQuantity();
			double risk = GetStagedRiskPoints() * ins.MasterInstrument.PointValue * q;
			if (!txtRisk.IsFocused) txtRisk.Text = risk.ToString("N0");
			stagedQuantity = q;
		}
		private void EnsureStagedVisualElements() {
			EnsureStagedBracketCanvas();
			if (stagedBracketCanvas == null || stagedEntryLine != null) return;
			CreateStagedVisual(GetChartEntryBrush(IsStagedLong()), "Entry", BeginDragStagedEntry, out stagedEntryLine, out stagedEntryPill, out stagedEntryTxt);
			CreateStagedVisual(GetChartRiskBrush(), "Stop", BeginDragStagedStop, out stagedStopLine, out stagedStopPill, out stagedStopTxt);
			CreateStagedVisual(GetChartProfitBrush(), "Target", BeginDragStagedTarget, out stagedTargetLine, out stagedTargetPill, out stagedTargetTxt);
			CreateStagedActionVisuals();
		}
		private void CreateStagedVisual(System.Windows.Media.Brush brush, string tooltip, MouseButtonEventHandler dragHandler, out System.Windows.Shapes.Line line, out System.Windows.Controls.Border pill, out System.Windows.Controls.TextBlock text) {
			line = new System.Windows.Shapes.Line { X1 = 0, Stroke = brush, StrokeThickness = 2, Cursor = Cursors.SizeNS };
			text = new System.Windows.Controls.TextBlock();
			ApplyChartLabelTextStyle(text);
			pill = new System.Windows.Controls.Border { Background = GetRouterBackgroundBrush(brush, 1), CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 3, 7, 3), Child = text, Cursor = Cursors.SizeNS, ToolTip = tooltip };
			line.MouseLeftButtonDown += dragHandler;
			pill.MouseLeftButtonDown += dragHandler;
			stagedBracketCanvas.Children.Add(line);
			stagedBracketCanvas.Children.Add(pill);
		}
		private void CreateStagedActionVisuals() {
			stagedPlaceTxt = new System.Windows.Controls.TextBlock { Text = "Place", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center };
			ApplyChartLabelTextStyle(stagedPlaceTxt);
			stagedPlacePill = new System.Windows.Controls.Border { Background = GetRouterBackgroundBrush(riskActiveBrush, 1), BorderBrush = riskTextBrush, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Width = 48, Height = 22, Child = stagedPlaceTxt, Cursor = Cursors.Hand, ToolTip = "Place staged bracket", Visibility = Visibility.Collapsed };
			stagedPlacePill.MouseLeftButtonDown += (s, e) => { SubmitStagedBracket(); e.Handled = true; };
			stagedBracketCanvas.Children.Add(stagedPlacePill);

			stagedCancelTxt = new System.Windows.Controls.TextBlock { Text = "X", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center };
			ApplyChartLabelTextStyle(stagedCancelTxt);
			stagedCancelPill = new System.Windows.Controls.Border { Background = GetRouterBackgroundBrush(riskDangerBrush, 1), BorderBrush = riskTextBrush, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Width = 22, Height = 22, Child = stagedCancelTxt, Cursor = Cursors.Hand, ToolTip = "Cancel staged bracket", Visibility = Visibility.Collapsed };
			stagedCancelPill.MouseLeftButtonDown += (s, e) => { CancelStagedBracket(); e.Handled = true; };
			stagedBracketCanvas.Children.Add(stagedCancelPill);
		}
		private void UpdateStagedBracketVisuals() {
			if (stagedEntryPrice <= 0 || stagedStopPrice <= 0 || stagedTargetPrice <= 0) return;
			RefreshStagedOrderTypeFromMarket();
			EnsureStagedVisualElements();
			if (stagedBracketCanvas == null) return;
			if (!isSpacebarPreviewActive && isBracketStaged) stagedBracketCanvas.Background = null;
			ApplyStagedVisualBrushes();
			double plotRight = GetPlotRightX();
			SetStagedLineVisual(stagedEntryLine, stagedEntryPill, stagedEntryTxt, stagedEntryPrice, BuildStagedEntryLabel(), plotRight);
			SetStagedLineVisual(stagedStopLine, stagedStopPill, stagedStopTxt, stagedStopPrice, BuildStagedStopLabel(), plotRight);
			SetStagedLineVisual(stagedTargetLine, stagedTargetPill, stagedTargetTxt, stagedTargetPrice, BuildStagedTargetLabel(), plotRight);
			UpdateStagedActionButtons();
		}
		private void ApplyStagedVisualBrushes() {
			System.Windows.Media.Brush entryBrush = GetChartEntryBrush(IsStagedLong());
			if (stagedEntryLine != null) stagedEntryLine.Stroke = entryBrush;
			if (stagedEntryPill != null) stagedEntryPill.Background = GetRouterBackgroundBrush(entryBrush, 1);
			if (stagedStopLine != null) stagedStopLine.Stroke = GetChartRiskBrush();
			if (stagedStopPill != null) stagedStopPill.Background = GetRouterBackgroundBrush(GetChartRiskBrush(), 1);
			if (stagedTargetLine != null) stagedTargetLine.Stroke = GetChartProfitBrush();
			if (stagedTargetPill != null) stagedTargetPill.Background = GetRouterBackgroundBrush(GetChartProfitBrush(), 1);
			if (stagedPlacePill != null) { stagedPlacePill.Background = GetRouterBackgroundBrush(riskActiveBrush, 1); stagedPlacePill.BorderBrush = riskTextBrush; }
			if (stagedCancelPill != null) { stagedCancelPill.Background = GetRouterBackgroundBrush(riskDangerBrush, 1); stagedCancelPill.BorderBrush = riskTextBrush; }
			ApplyChartLabelTextStyle(stagedEntryTxt);
			ApplyChartLabelTextStyle(stagedStopTxt);
			ApplyChartLabelTextStyle(stagedTargetTxt);
			ApplyChartLabelTextStyle(stagedPlaceTxt);
			ApplyChartLabelTextStyle(stagedCancelTxt);
		}
		private void SetStagedLineVisual(System.Windows.Shapes.Line line, System.Windows.Controls.Border pill, System.Windows.Controls.TextBlock text, double price, string label, double plotRight) {
			if (line == null || pill == null || text == null) return;
			double y, panelTop, panelBottom;
			if (!TryGetYByPriceInPrimaryPanel(price, out y, out panelTop, out panelBottom)) {
				line.Visibility = Visibility.Collapsed; pill.Visibility = Visibility.Collapsed; return;
			}
			line.Visibility = Visibility.Visible; pill.Visibility = Visibility.Visible;
			line.X2 = plotRight; line.Y1 = line.Y2 = y;
			ApplyChartLabelTextStyle(text);
			text.Text = label;
			pill.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			System.Windows.Controls.Canvas.SetLeft(pill, Math.Max(0, plotRight - pill.DesiredSize.Width - 65));
			System.Windows.Controls.Canvas.SetTop(pill, ClampRoutedTop(y - (pill.DesiredSize.Height / 2.0), pill.DesiredSize.Height, panelTop, panelBottom));
		}
		private void UpdateStagedActionButtons() {
			if (stagedPlacePill == null || stagedCancelPill == null || stagedEntryPill == null || !isBracketStaged || stagedEntryPill.Visibility != Visibility.Visible) {
				if (stagedPlacePill != null) stagedPlacePill.Visibility = Visibility.Collapsed;
				if (stagedCancelPill != null) stagedCancelPill.Visibility = Visibility.Collapsed;
				return;
			}
			stagedPlacePill.Visibility = Visibility.Visible;
			stagedCancelPill.Visibility = Visibility.Visible;
			double entryLeft = System.Windows.Controls.Canvas.GetLeft(stagedEntryPill);
			double entryTop = System.Windows.Controls.Canvas.GetTop(stagedEntryPill);
			if (double.IsNaN(entryLeft)) entryLeft = 0;
			if (double.IsNaN(entryTop)) entryTop = 0;
			double entryWidth = stagedEntryPill.DesiredSize.Width > 0 ? stagedEntryPill.DesiredSize.Width : stagedEntryPill.ActualWidth;
			double placeLeft = Math.Max(0, entryLeft - stagedPlacePill.Width - 4);
			double cancelLeft = entryLeft + entryWidth + 4;
			System.Windows.Controls.Canvas.SetLeft(stagedPlacePill, placeLeft);
			System.Windows.Controls.Canvas.SetTop(stagedPlacePill, entryTop);
			System.Windows.Controls.Canvas.SetLeft(stagedCancelPill, cancelLeft);
			System.Windows.Controls.Canvas.SetTop(stagedCancelPill, entryTop);
		}
		private string BuildStagedEntryLabel() { return $"{GetStagedEntryTypeLabel()} {ParseQuantity()} @ {stagedEntryPrice:F2}"; }
		private string BuildStagedStopLabel() {
			if (!riskSettings.ShowBracketPreviewRiskLabels) return $"SL @ {stagedStopPrice:F2}";
			double points = GetStagedRiskPoints(), dollars = points * GetStagedPointValue() * ParseQuantity();
			return $"Risk {dollars:C0} | {points:F2} pts | Qty {ParseQuantity()}";
		}
		private string BuildStagedTargetLabel() {
			if (!riskSettings.ShowBracketPreviewRiskLabels) return $"TP @ {stagedTargetPrice:F2}";
			double riskPoints = GetStagedRiskPoints(), profitPoints = GetStagedProfitPoints(), dollars = profitPoints * GetStagedPointValue() * ParseQuantity(), rr = riskPoints > 0 ? profitPoints / riskPoints : 0;
			return $"Profit {dollars:C0} | {profitPoints:F2} pts | {rr:F1}R";
		}
		private string GetStagedEntryTypeLabel() {
			string side = IsStagedLong() ? "BUY" : "SELL";
			string type = stagedEntryOrderType == OrderType.Limit ? "LMT" : stagedEntryOrderType == OrderType.StopMarket ? "STP" : stagedEntryOrderType.ToString().ToUpperInvariant();
			return side + " " + type;
		}
		private bool IsStagedLong() { return stagedEntryAction == OrderAction.Buy || stagedEntryAction == OrderAction.BuyToCover; }
		private double GetStagedRiskPoints() { return Math.Abs(stagedEntryPrice - stagedStopPrice); }
		private double GetStagedProfitPoints() { return Math.Abs(stagedTargetPrice - stagedEntryPrice); }
		private double GetStagedPointValue() { return (GetActiveInstrument() ?? GetChartInstrument())?.MasterInstrument?.PointValue ?? 1; }
		private int ParseQuantity() { int q; return txtContracts != null && int.TryParse(txtContracts.Text, out q) ? Math.Max(1, q) : Math.Max(1, stagedQuantity); }
		private double RoundToTick(double price) {
			double tick = (GetActiveInstrument() ?? GetChartInstrument())?.MasterInstrument?.TickSize ?? 0.25;
			return tick > 0 ? Math.Round(price / tick) * tick : price;
		}
		private void SubmitStagedBracket() {
			try {
				Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument();
				stagedQuantity = ParseQuantity();
				if (acc == null || ins == null || stagedQuantity < 1 || stagedEntryPrice <= 0 || stagedStopPrice <= 0 || stagedTargetPrice <= 0) return;
				if (!CanSubmitHotkeyOrder(acc)) return;
				pendingStopPrice = stagedStopPrice; pendingTargetPrice = stagedTargetPrice; pendingContracts = stagedQuantity;
				string id = "OrcaBracket_" + Guid.NewGuid().ToString("N");
				double limitPrice = stagedEntryOrderType == OrderType.Limit ? stagedEntryPrice : 0;
				double stopPrice = stagedEntryOrderType == OrderType.StopMarket ? stagedEntryPrice : 0;
				acc.Submit(new[] { acc.CreateOrder(ins, stagedEntryAction, stagedEntryOrderType, OrderEntry.Manual, TimeInForce.Day, stagedQuantity, limitPrice, stopPrice, "", id, DateTime.MaxValue, null) });
				pendingEntryName = id;
				RemoveStagedBracketOverlay();
			} catch { }
		}
		private void CancelStagedBracket() { RemoveStagedBracketOverlay(); }
		private bool CanSubmitHotkeyOrder(Account account) {
			riskSettings = OrcaRiskManager.GetSettings();
			if (account == null) return false;
			if (IsLiveAccount(account) && !riskSettings.EnableLiveTradingHotkeys) {
				System.Windows.MessageBox.Show("Live account hotkey submission is disabled in Orca Risk Manager settings.", "Orca Risk Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}
			if (IsLiveAccount(account) && riskSettings.ConfirmLiveOrders) {
				return System.Windows.MessageBox.Show($"Submit staged bracket on {account.Name}?", "Confirm Orca Bracket", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
			}
			return true;
		}
		private bool ConfirmLiveOrderIfNeeded(Account account, string actionText) {
			riskSettings = OrcaRiskManager.GetSettings();
			if (account == null || !IsLiveAccount(account) || !riskSettings.ConfirmLiveOrders) return true;
			return System.Windows.MessageBox.Show($"{actionText} on {account.Name}?", "Confirm Orca Order", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
		}
		private bool ConfirmFlattenIfNeeded(Account account) {
			riskSettings = OrcaRiskManager.GetSettings();
			if (account == null || !riskSettings.ConfirmFlatten) return true;
			return System.Windows.MessageBox.Show($"Flatten active {account.Name} position and cancel working orders?", "Confirm Orca Flatten", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
		}
		private bool IsLiveAccount(Account account) {
			string name = account?.Name ?? "";
			return !(name.StartsWith("Sim", StringComparison.OrdinalIgnoreCase) || name.IndexOf("Playback", StringComparison.OrdinalIgnoreCase) >= 0);
		}
		private void CheckPendingEntryState(Account acc) {
			try {
				if (acc == null || string.IsNullOrEmpty(pendingEntryName)) return;
				Order entry = acc.Orders.FirstOrDefault(o => o.Name == pendingEntryName);
				if (entry != null && (entry.OrderState == OrderState.Cancelled || entry.OrderState == OrderState.Rejected)) {
					pendingEntryName = null; pendingStopPrice = 0; pendingTargetPrice = 0; pendingContracts = 0;
				}
			} catch { }
		}
		private void ExecuteTrade(OrderType t) { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc == null || ins == null) return; int q = 1; int.TryParse(txtContracts.Text, out q); if (q < 1 || !ConfirmLiveOrderIfNeeded(acc, "Submit Orca entry order")) return; OrderAction act = isLongSelected ? OrderAction.Buy : OrderAction.SellShort; double ent = hEntry?.StartAnchor.Price ?? 0, stp = hStop?.StartAnchor.Price ?? 0, tar = hTarget?.StartAnchor.Price ?? 0; if (stp != 0) { pendingStopPrice=stp; pendingTargetPrice=tar; } string id = "Orca_" + Guid.NewGuid().ToString("N"); if (t == OrderType.Market) acc.Submit(new[] { acc.CreateOrder(ins, act, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, q, 0, 0, "", id, DateTime.MaxValue, null) }); else acc.Submit(new[] { acc.CreateOrder(ins, act, t, OrderEntry.Manual, TimeInForce.Day, q, t==OrderType.Limit?ent:0, t==OrderType.StopMarket?ent:0, "", id, DateTime.MaxValue, null) }); pendingEntryName = id; } catch { } }
		private void ExecuteFastCommand(string c) { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc == null || ins == null) return; int q = 1; int.TryParse(txtContracts.Text, out q); if (q < 1 || !ConfirmLiveOrderIfNeeded(acc, "Submit Orca fast order")) return; string id = "Fast_" + Guid.NewGuid().ToString("N"); OrderAction act = c.StartsWith("Sell")? OrderAction.Sell : OrderAction.Buy; if (c.EndsWith("Mkt")) acc.Submit(new[] { acc.CreateOrder(ins, act, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, q, 0, 0, "", id, DateTime.MaxValue, null) }); else acc.Submit(new[] { acc.CreateOrder(ins, act, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, q, GetActivePrice() + (c=="BuyAsk"?ins.MasterInstrument.TickSize:-ins.MasterInstrument.TickSize), 0, "", id, DateTime.MaxValue, null) }); } catch { } }
		private void StartDragOrder(string t) {
			if (!(attachedTab?.Content is System.Windows.Controls.Grid r)) return;
			if (isDragOrderActive) {
				bool isSame = dragOrderType == t;
				CancelDragOrder();
				if (isSame) return;
			}
			isDragOrderActive = true;
			dragOrderType = t;
			dragCanvas = new System.Windows.Controls.Canvas { Background = System.Windows.Media.Brushes.Transparent, Cursor = Cursors.Cross, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			AlignOverlayCanvasWithChart(dragCanvas);
			System.Windows.Controls.Panel.SetZIndex(dragCanvas, 9999);
			System.Windows.Media.Brush br = GetChartEntryBrush(!t.StartsWith("Sell"));
			dragLine = new System.Windows.Shapes.Line { X1 = 0, X2 = attachedTab.ChartControl.ActualWidth, Stroke = br, StrokeThickness = 2 };
			dragLabelTxt = new System.Windows.Controls.TextBlock();
			ApplyChartLabelTextStyle(dragLabelTxt);
			dragLabelPill = new System.Windows.Controls.Border { Background = GetRouterBackgroundBrush(br, 1), CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 3, 7, 3), Child = dragLabelTxt };
			dragCanvas.Children.Add(dragLine);
			dragCanvas.Children.Add(dragLabelPill);
			dragCanvas.MouseMove += (s, e) => {
				if (!isDragOrderActive) return;
				Point p = e.GetPosition(dragCanvas);
				double pr = GetPriceByY(p.Y);
				if (attachedTab?.ChartControl?.Instrument != null) {
					double tk = attachedTab.ChartControl.Instrument.MasterInstrument.TickSize;
					pr = Math.Round(pr / tk) * tk;
					double sY = GetYByPrice(pr);
					if (sY != 0) p.Y = sY;
				}
				dragLine.Stroke = GetChartEntryBrush(!dragOrderType.StartsWith("Sell"));
				dragLine.X2 = Math.Max(dragCanvas.ActualWidth, attachedTab.ChartControl.ActualWidth);
				dragLine.Y1 = dragLine.Y2 = p.Y;
				if (dragLabelPill != null) dragLabelPill.Background = GetRouterBackgroundBrush((System.Windows.Media.Brush)dragLine.Stroke, 1);
				ApplyChartLabelTextStyle(dragLabelTxt);
				int q = 1;
				int.TryParse(txtContracts.Text, out q);
				string act = dragOrderType.StartsWith("Buy") ? "BUY" : "SELL";
				string typ = dragOrderType.EndsWith("Limit") ? "LMT" : "STP";
				dragLabelTxt.Text = $"{act} {q} {typ} @ {pr:F2}";
				System.Windows.Controls.Canvas.SetRight(dragLabelPill, 65);
				System.Windows.Controls.Canvas.SetTop(dragLabelPill, p.Y - 12);
			};
			dragCanvas.MouseLeftButtonDown += (s, e) => { PlaceDragOrderAt(GetPriceByY(e.GetPosition(dragCanvas).Y)); CancelDragOrder(); };
			r.Children.Add(dragCanvas);
			var w = Window.GetWindow(attachedTab.ChartControl);
			if (w != null) w.PreviewKeyDown += Window_PreviewKeyDown_CancelDrag;
		}
		private double GetPriceByY(double y) { try { var s = attachedTab?.ChartControl?.ChartPanels.FirstOrDefault()?.Scales.FirstOrDefault(); if (s != null) return s.GetValueByY((float)y); } catch { } return 0; }
		private double GetYByPrice(double p) { try { var s = attachedTab?.ChartControl?.ChartPanels.FirstOrDefault()?.Scales.FirstOrDefault(); if (s != null) return s.GetYByValue(p); } catch { } return 0; }
		private bool TryGetPrimaryPricePanelBounds(out double top, out double bottom) {
			top = 0;
			bottom = attachedTab?.ChartControl?.ActualHeight ?? 0;
			try {
				ChartPanel panel = attachedTab?.ChartControl?.ChartPanels.FirstOrDefault();
				if (panel == null || panel.H <= 0) return bottom > top;
				top = panel.Y;
				bottom = panel.Y + panel.H;
				return bottom > top;
			} catch { return bottom > top; }
		}
		private void AlignOverlayCanvasWithChart(System.Windows.Controls.Canvas canvas) {
			if (canvas == null || attachedTab?.ChartControl == null) return;
			System.Windows.Controls.Grid.SetRow(canvas, System.Windows.Controls.Grid.GetRow(attachedTab.ChartControl));
			System.Windows.Controls.Grid.SetColumn(canvas, System.Windows.Controls.Grid.GetColumn(attachedTab.ChartControl));
			System.Windows.Controls.Grid.SetRowSpan(canvas, System.Windows.Controls.Grid.GetRowSpan(attachedTab.ChartControl));
			System.Windows.Controls.Grid.SetColumnSpan(canvas, System.Windows.Controls.Grid.GetColumnSpan(attachedTab.ChartControl));
		}
		private bool TryGetYByPriceInPrimaryPanel(double price, out double y, out double panelTop, out double panelBottom) {
			y = GetYByPrice(price);
			TryGetPrimaryPricePanelBounds(out panelTop, out panelBottom);
			if (y <= 0 || panelBottom <= panelTop) return false;
			return y >= panelTop && y <= panelBottom;
		}
		private double ClampRoutedTop(double desiredTop, double elementHeight, double panelTop, double panelBottom) {
			if (elementHeight <= 0 || double.IsNaN(elementHeight) || double.IsInfinity(elementHeight)) elementHeight = 22;
			if (panelBottom <= panelTop) return desiredTop;
			return Math.Max(panelTop, Math.Min(panelBottom - elementHeight, desiredTop));
		}
		private void UpdateRoutedOrderOverlay(Account acc, Instrument executionInstrument) {
			try {
				if (isDraggingRoutedTP || isDraggingRoutedSL || isDraggingRoutedOrder) return;
				Instrument chartInstrument = GetChartInstrument();
				if (acc == null || executionInstrument == null || IsSameInstrument(chartInstrument, executionInstrument)) { RemoveRoutedOrderOverlay(); return; }
				EnsureRoutedOrderCanvas();
				if (routedOrderCanvas == null) return;
				OrcaExecutionRouterSettings visual = OrcaExecutionRouter.GetSettings();
				routedOrderCanvas.Children.Clear();
				routedLabelSlots.Clear();
				Position pos = acc.Positions.FirstOrDefault(p => IsSameInstrument(p.Instrument, executionInstrument));
				bool hasStop = false, hasLimit = false;
				string activeOco = "";
				var workingOrders = acc.Orders.Where(o => IsSameInstrument(o.Instrument, executionInstrument) && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted)).ToList();
				foreach (Order order in workingOrders) {
					if (order.OrderType == OrderType.StopMarket || order.OrderType == OrderType.StopLimit) hasStop = true;
					if (order.OrderType == OrderType.Limit) hasLimit = true;
					if (!string.IsNullOrEmpty(order.Oco) && order.Oco.StartsWith("OrcaOCO_")) activeOco = order.Oco;
				}
				if (pos != null && pos.MarketPosition != MarketPosition.Flat && pos.AveragePrice > 0) {
					double displayAveragePrice = RoundPriceToInstrumentTick(executionInstrument, pos.AveragePrice);
					string label = BuildPositionTradeLabel(executionInstrument, pos);
					bool pnlIsPositive;
					string pnlBadge = BuildPositionPnlBadge(executionInstrument, pos, out pnlIsPositive);
					AddRoutedLine(displayAveragePrice, GetRoutedPositionBrush(pos, visual), label, acc, null, visual, pnlBadge, pnlIsPositive);
					AddRoutedProtectionButtons(acc, executionInstrument, pos, hasLimit, hasStop, activeOco, visual);
				}
				foreach (RoutedOrderGroup group in BuildRoutedOrderGroups(workingOrders, executionInstrument)) {
					System.Windows.Media.Brush brush = GetRoutedOrderBrush(pos, group.Representative, group.Price, visual);
					string label = BuildRoutedOrderLabel(executionInstrument, pos, group);
					AddRoutedLine(group.Price, brush, label, acc, group.Representative, visual, "", true, group.Orders);
				}
			} catch { }
		}
		private class RoutedOrderGroup {
			public readonly List<Order> Orders = new List<Order>();
			public Order Representative;
			public OrderAction Action;
			public OrderType Type;
			public double Price;
			public int Quantity;
		}
		private List<RoutedOrderGroup> BuildRoutedOrderGroups(IEnumerable<Order> orders, Instrument instrument) {
			var groups = new Dictionary<string, RoutedOrderGroup>();
			double tick = instrument?.MasterInstrument?.TickSize ?? 0.25;
			foreach (Order order in orders) {
				double price = GetOrderDisplayPrice(order);
				if (price <= 0) continue;
				if (tick > 0) price = Math.Round(price / tick) * tick;
				string key = order.OrderAction + "|" + order.OrderType + "|" + price.ToString("F8");
				RoutedOrderGroup group;
				if (!groups.TryGetValue(key, out group)) {
					group = new RoutedOrderGroup { Representative = order, Action = order.OrderAction, Type = order.OrderType, Price = price };
					groups[key] = group;
				}
				group.Orders.Add(order);
				group.Quantity += Math.Max(1, order.Quantity);
			}
			return groups.Values.OrderBy(g => g.Price).ThenBy(g => g.Type.ToString()).ToList();
		}
		private void EnsureRoutedOrderCanvas() {
			if (routedOrderCanvas != null) return;
			if (!(attachedTab?.Content is System.Windows.Controls.Grid grid) || attachedTab.ChartControl == null) return;
			routedOrderCanvas = new System.Windows.Controls.Canvas { IsHitTestVisible = true, ClipToBounds = true, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			System.Windows.Controls.Panel.SetZIndex(routedOrderCanvas, 9996);
			AlignOverlayCanvasWithChart(routedOrderCanvas);
			routedOrderCanvas.MouseMove += RoutedOrderCanvas_MouseMove;
			routedOrderCanvas.MouseLeftButtonUp += RoutedOrderCanvas_MouseLeftButtonUp;
			grid.Children.Add(routedOrderCanvas);
		}
		private void RemoveRoutedOrderOverlay() { try { routedLabelSlots.Clear(); if (routedOrderCanvas != null) { routedOrderCanvas.MouseMove -= RoutedOrderCanvas_MouseMove; routedOrderCanvas.MouseLeftButtonUp -= RoutedOrderCanvas_MouseLeftButtonUp; (System.Windows.Media.VisualTreeHelper.GetParent(routedOrderCanvas) as System.Windows.Controls.Panel)?.Children.Remove(routedOrderCanvas); routedOrderCanvas = null; } } catch { } }
		private double GetPlotRightX() {
			try { if (attachedTab?.ChartControl != null && attachedTab.ChartControl.CanvasRight > 0) return attachedTab.ChartControl.CanvasRight; } catch { }
			return Math.Max(100, (attachedTab?.ChartControl?.ActualWidth ?? 100) - 65);
		}
		private string ShortOrderAction(OrderAction action) { return action == OrderAction.BuyToCover ? "Buy" : action == OrderAction.SellShort ? "Short" : action.ToString(); }
		private System.Windows.Media.Brush GetRouterBrush(string text, System.Windows.Media.Brush fallback) { try { return (System.Windows.Media.Brush)new BrushConverter().ConvertFrom(text); } catch { return fallback; } }
		private System.Windows.Media.Brush GetRouterBackgroundBrush(System.Windows.Media.Brush brush, double opacity) {
			try {
				System.Windows.Media.Brush clone = brush.Clone();
				clone.Opacity = GetRouterOpacity(opacity, 0.95);
				return clone;
			} catch { return brush; }
		}
		private double GetRouterOpacity(double opacity, double fallback) {
			if (double.IsNaN(opacity) || double.IsInfinity(opacity)) return fallback;
			if (opacity > 1 && opacity <= 100) opacity /= 100.0;
			return Math.Max(0, Math.Min(1, opacity));
		}
		private FontFamily GetRouterFont(OrcaExecutionRouterSettings visual) {
			try {
				string family = riskSettings != null && !string.IsNullOrWhiteSpace(riskSettings.FontFamily) ? riskSettings.FontFamily : visual.FontFamily;
				return new FontFamily(string.IsNullOrWhiteSpace(family) ? "Segoe UI" : family);
			} catch { return new FontFamily("Segoe UI"); }
		}
		private FontWeight GetRouterFontWeight(OrcaExecutionRouterSettings visual) {
			string weight = string.IsNullOrWhiteSpace(visual.FontWeight) ? "SemiBold" : visual.FontWeight.Replace(" ", "");
			if (string.Equals(weight, "Normal", StringComparison.OrdinalIgnoreCase)) return FontWeights.Normal;
			if (string.Equals(weight, "Medium", StringComparison.OrdinalIgnoreCase)) return FontWeights.Medium;
			if (string.Equals(weight, "Bold", StringComparison.OrdinalIgnoreCase)) return FontWeights.Bold;
			return FontWeights.SemiBold;
		}
		private System.Windows.Media.Brush GetRoutedPositionBrush(Position pos, OrcaExecutionRouterSettings visual) {
			if (pos != null && pos.MarketPosition == MarketPosition.Short)
				return GetRouterBrush(visual.ShortPositionColor, (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFE03A52"));
			return GetRouterBrush(visual.PositionColor, System.Windows.Media.Brushes.DodgerBlue);
		}
		private System.Windows.Media.Brush GetRoutedOrderBrush(Position pos, Order order, double price, OrcaExecutionRouterSettings visual) {
			if (IsPositionReducingOrder(pos, order)) {
				double dollars = GetProtectionDollarAmount(order.Instrument, pos.MarketPosition, pos.AveragePrice, price, Math.Max(1, order.Quantity));
				if (dollars > 0) return GetRouterBrush(visual.BuyColor, System.Windows.Media.Brushes.LimeGreen);
				if (dollars < 0) return GetRouterBrush(visual.SellColor, System.Windows.Media.Brushes.Salmon);
			}
			return (order.OrderAction == OrderAction.Buy || order.OrderAction == OrderAction.BuyToCover)
				? GetRouterBrush(visual.BuyColor, System.Windows.Media.Brushes.LimeGreen)
				: GetRouterBrush(visual.SellColor, System.Windows.Media.Brushes.Salmon);
		}
		private string BuildPositionTradeLabel(Instrument instrument, Position pos) {
			double averagePrice = RoundPriceToInstrumentTick(instrument, pos.AveragePrice);
			return $"{pos.MarketPosition} {pos.Quantity} @ {averagePrice:F2}";
		}
		private string BuildPositionPnlBadge(Instrument instrument, Position pos, out bool pnlIsPositive) {
			double current = RoundPriceToInstrumentTick(instrument, GetActivePrice());
			double averagePrice = RoundPriceToInstrumentTick(instrument, pos.AveragePrice);
			double direction = pos.MarketPosition == MarketPosition.Long ? 1 : -1;
			double points = (current - averagePrice) * direction;
			double dollars = points * instrument.MasterInstrument.PointValue * Math.Abs(pos.Quantity);
			pnlIsPositive = dollars >= 0;
			string dollarText = (dollars >= 0 ? "+" : "-") + Math.Abs(dollars).ToString("C2");
			string pointText = (points >= 0 ? "+" : "-") + Math.Abs(points).ToString("F2");
			return $"{dollarText} | {pointText}";
		}
		private string BuildRoutedOrderLabel(Instrument instrument, Position pos, Order order, double price, string type) {
			string label = $"{ShortOrderAction(order.OrderAction)} {order.Quantity} {type} @ {price:F2}";
			string amount = BuildProtectionAmountText(instrument, pos, order, price);
			return string.IsNullOrEmpty(amount) ? label : label + " | " + amount;
		}
		private string BuildRoutedOrderLabel(Instrument instrument, Position pos, RoutedOrderGroup group) {
			string type = group.Type == OrderType.StopMarket ? "STP" : group.Type == OrderType.Limit ? "LMT" : group.Type.ToString();
			string label = $"{ShortOrderAction(group.Action)} {group.Quantity} {type} @ {group.Price:F2}";
			string amount = (pos != null && IsPositionReducingOrder(pos, group.Representative))
				? BuildProtectionAmountText(instrument, pos.MarketPosition, pos.AveragePrice, group.Price, group.Quantity)
				: "";
			return string.IsNullOrEmpty(amount) ? label : label + " | " + amount;
		}
		private string BuildProtectionAmountText(Instrument instrument, Position pos, Order order, double price) {
			if (order == null || !IsPositionReducingOrder(pos, order)) return "";
			return BuildProtectionAmountText(instrument, pos.MarketPosition, pos.AveragePrice, price, Math.Max(1, order.Quantity));
		}
		private string BuildProtectionAmountText(Instrument instrument, MarketPosition side, double entryPrice, double price, int quantity) {
			if (instrument == null || side == MarketPosition.Flat || entryPrice <= 0 || price <= 0 || quantity <= 0) return "";
			double dollars = GetProtectionDollarAmount(instrument, side, entryPrice, price, quantity);
			if (dollars > 0) return "Profit " + Math.Abs(dollars).ToString("C0");
			if (dollars < 0) return "Risk " + Math.Abs(dollars).ToString("C0");
			return "B/E";
		}
		private double GetProtectionDollarAmount(Instrument instrument, MarketPosition side, double entryPrice, double price, int quantity) {
			if (instrument == null || side == MarketPosition.Flat || entryPrice <= 0 || price <= 0 || quantity <= 0) return 0;
			entryPrice = RoundPriceToInstrumentTick(instrument, entryPrice);
			price = RoundPriceToInstrumentTick(instrument, price);
			double direction = side == MarketPosition.Long ? 1 : -1;
			double points = (price - entryPrice) * direction;
			return points * instrument.MasterInstrument.PointValue * quantity;
		}
		private double RoundPriceToInstrumentTick(Instrument instrument, double price) {
			if (instrument == null || price <= 0 || double.IsNaN(price) || double.IsInfinity(price))
				return price;
			double tick = instrument.MasterInstrument?.TickSize ?? 0;
			if (tick <= 0 || double.IsNaN(tick) || double.IsInfinity(tick))
				return price;
			return Math.Round(price / tick, 0, MidpointRounding.AwayFromZero) * tick;
		}
		private bool IsPositionReducingOrder(Position pos, Order order) {
			if (pos == null || order == null || pos.MarketPosition == MarketPosition.Flat) return false;
			if (pos.MarketPosition == MarketPosition.Long) return order.OrderAction == OrderAction.Sell;
			return order.OrderAction == OrderAction.Buy || order.OrderAction == OrderAction.BuyToCover;
		}
		private double ReserveRoutedLabelTop(double desiredTop, double elementHeight, double panelTop, double panelBottom) {
			const double gap = 0;
			double height = elementHeight <= 0 || double.IsNaN(elementHeight) || double.IsInfinity(elementHeight) ? 22 : elementHeight;
			double clampedDesired = ClampRoutedTop(desiredTop, height, panelTop, panelBottom);
			if (IsRoutedLabelSlotFree(clampedDesired, height, gap)) {
				AddRoutedLabelSlot(clampedDesired, height, gap);
				return clampedDesired;
			}
			double step = height + gap;
			for (int i = 1; i <= 10; i++) {
				double lower = ClampRoutedTop(clampedDesired + step * i, height, panelTop, panelBottom);
				if (Math.Abs(lower - clampedDesired) > 0.5 && IsRoutedLabelSlotFree(lower, height, gap)) {
					AddRoutedLabelSlot(lower, height, gap);
					return lower;
				}
				double upper = ClampRoutedTop(clampedDesired - step * i, height, panelTop, panelBottom);
				if (Math.Abs(upper - clampedDesired) > 0.5 && IsRoutedLabelSlotFree(upper, height, gap)) {
					AddRoutedLabelSlot(upper, height, gap);
					return upper;
				}
			}
			AddRoutedLabelSlot(clampedDesired, height, gap);
			return clampedDesired;
		}
		private bool IsRoutedLabelSlotFree(double top, double height, double gap) {
			Rect candidate = new Rect(0, top - gap, 1, height + gap * 2);
			return !routedLabelSlots.Any(slot => slot.IntersectsWith(candidate));
		}
		private void AddRoutedLabelSlot(double top, double height, double gap) {
			routedLabelSlots.Add(new Rect(0, top - gap, 1, height + gap * 2));
		}
		private FrameworkElement BuildRoutedLabelContent(string label, System.Windows.Media.Brush mainBrush, OrcaExecutionRouterSettings visual, string pnlBadgeText, bool pnlIsPositive) {
			System.Windows.Media.Brush textBrush = GetRouterBrush(visual.TextColor, System.Windows.Media.Brushes.Black);
			var text = new System.Windows.Controls.TextBlock { Text = label, Foreground = textBrush, FontFamily = GetRouterFont(visual), FontWeight = GetRouterFontWeight(visual), FontSize = visual.FontSize, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
			if (string.IsNullOrWhiteSpace(pnlBadgeText))
				return text;

			var pnlBrush = pnlIsPositive ? GetRouterBrush(visual.BuyColor, System.Windows.Media.Brushes.LimeGreen) : GetRouterBrush(visual.SellColor, System.Windows.Media.Brushes.Salmon);
			var badgeText = new System.Windows.Controls.TextBlock { Text = pnlBadgeText, Foreground = textBrush, FontFamily = GetRouterFont(visual), FontWeight = FontWeights.Normal, FontSize = visual.FontSize, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
			bool badgeLeft = !string.Equals(visual.PnlBadgePosition, "Right", StringComparison.OrdinalIgnoreCase);
			var badge = new System.Windows.Controls.Border { Background = GetRouterBackgroundBrush(pnlBrush, 1), CornerRadius = badgeLeft ? new CornerRadius(4, 0, 0, 4) : new CornerRadius(0, 4, 4, 0), Padding = new Thickness(5, 2, 5, 2), Child = badgeText, IsHitTestVisible = false };
			var tradeSegment = new System.Windows.Controls.Border { Background = GetRouterBackgroundBrush(mainBrush, visual.LabelBackgroundOpacity), CornerRadius = badgeLeft ? new CornerRadius(0, 4, 4, 0) : new CornerRadius(4, 0, 0, 4), Padding = new Thickness(5, 2, 5, 2), Child = text, IsHitTestVisible = false };
			var stack = new System.Windows.Controls.StackPanel { Orientation = Orientation.Horizontal, IsHitTestVisible = false };
			if (badgeLeft) {
				stack.Children.Add(badge);
				stack.Children.Add(tradeSegment);
			} else {
				stack.Children.Add(tradeSegment);
				stack.Children.Add(badge);
			}
			return stack;
		}
		private void AddRoutedLine(double price, System.Windows.Media.Brush brush, string label, Account acc, Order order, OrcaExecutionRouterSettings visual, string pnlBadgeText = "", bool pnlIsPositive = true, List<Order> groupedOrders = null) {
			double y, panelTop, panelBottom;
			if (!TryGetYByPriceInPrimaryPanel(price, out y, out panelTop, out panelBottom)) return;
			if (y <= 0 || routedOrderCanvas == null || attachedTab?.ChartControl == null) return;
			double plotRight = GetPlotRightX();
			var line = new System.Windows.Shapes.Line { X1 = 0, X2 = plotRight, Y1 = y, Y2 = y, Stroke = brush, StrokeThickness = visual.LineThickness, Opacity = 0.95, IsHitTestVisible = false };
			bool hasPnlBadge = !string.IsNullOrWhiteSpace(pnlBadgeText);
			var content = BuildRoutedLabelContent(label, brush, visual, pnlBadgeText, pnlIsPositive);
			var pill = new System.Windows.Controls.Border { Background = hasPnlBadge ? System.Windows.Media.Brushes.Transparent : GetRouterBackgroundBrush(brush, visual.LabelBackgroundOpacity), CornerRadius = new CornerRadius(4), Padding = hasPnlBadge ? new Thickness(0) : new Thickness(4, 2, 4, 2), Child = content, IsHitTestVisible = order != null, Cursor = order != null ? Cursors.SizeNS : Cursors.Arrow };
			List<Order> actionOrders = groupedOrders ?? (order != null ? new List<Order> { order } : null);
			var closeButton = acc != null ? BuildRoutedCloseButton(acc, order, visual, actionOrders) : null;
			routedOrderCanvas.Children.Add(line);
			System.Windows.Shapes.Line hitLine = null;
			if (order != null) {
				hitLine = new System.Windows.Shapes.Line { X1 = 0, X2 = plotRight, Y1 = y, Y2 = y, Stroke = System.Windows.Media.Brushes.Transparent, StrokeThickness = 10, Cursor = Cursors.SizeNS };
				hitLine.MouseLeftButtonDown += (s, e) => { StartRoutedOrderChangeDrag(acc, actionOrders); e.Handled = true; };
				if (actionOrders == null || actionOrders.Count == 1) hitLine.ContextMenu = BuildRoutedOrderContextMenu(acc, order);
				routedOrderCanvas.Children.Add(hitLine);
				pill.MouseLeftButtonDown += (s, e) => { StartRoutedOrderChangeDrag(acc, actionOrders); e.Handled = true; };
				if (actionOrders == null || actionOrders.Count == 1) pill.ContextMenu = BuildRoutedOrderContextMenu(acc, order);
			}
			routedOrderCanvas.Children.Add(pill);
			if (closeButton != null) routedOrderCanvas.Children.Add(closeButton);
			pill.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			double closeWidth = closeButton != null ? 16 : 0;
			double closeGap = closeButton != null ? 3 : 0;
			double closeLeft = Math.Max(0, plotRight - closeWidth - visual.LabelRightPadding);
			double slotHeight = Math.Max(pill.DesiredSize.Height, closeButton != null ? closeButton.Height : 0);
			double slotTop = ReserveRoutedLabelTop(y - (slotHeight / 2.0), slotHeight, panelTop, panelBottom);
			double labelTop = slotTop + Math.Max(0, (slotHeight - pill.DesiredSize.Height) / 2.0);
			double labelLeft = Math.Max(0, (closeButton != null ? closeLeft - closeGap : plotRight - visual.LabelRightPadding) - pill.DesiredSize.Width);
			line.X2 = Math.Max(0, labelLeft - 4);
			if (hitLine != null) hitLine.X2 = line.X2;
			System.Windows.Controls.Canvas.SetLeft(pill, labelLeft);
			System.Windows.Controls.Canvas.SetTop(pill, labelTop);
			if (closeButton != null) {
				double buttonTop = slotTop + Math.Max(0, (slotHeight - closeButton.Height) / 2.0);
				System.Windows.Controls.Canvas.SetLeft(closeButton, closeLeft);
				System.Windows.Controls.Canvas.SetTop(closeButton, buttonTop);
			}
		}
		private System.Windows.Controls.Border BuildRoutedCloseButton(Account acc, Order order, OrcaExecutionRouterSettings visual, List<Order> orders = null) {
			var text = new System.Windows.Controls.TextBlock { Text = "X", Foreground = System.Windows.Media.Brushes.White, FontFamily = GetRouterFont(visual), FontWeight = FontWeights.Bold, FontSize = Math.Max(9, visual.FontSize - 1), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
			var button = new System.Windows.Controls.Border { Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#CC111111"), BorderBrush = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Width = 16, Height = 16, Child = text, Opacity = 0.95, Cursor = Cursors.Hand, ToolTip = order == null ? "Flatten position and cancel working orders" : orders != null && orders.Count > 1 ? "Cancel these orders" : "Cancel this order" };
			button.MouseLeftButtonDown += (s, e) => {
				if (order == null) Flatten();
				else CancelRoutedOrders(acc, orders ?? new List<Order> { order });
				e.Handled = true;
			};
			return button;
		}
		private void AddRoutedProtectionButtons(Account acc, Instrument instrument, Position pos, bool hasLimit, bool hasStop, string oco, OrcaExecutionRouterSettings visual) {
			double y, panelTop, panelBottom;
			double displayAveragePrice = RoundPriceToInstrumentTick(instrument, pos.AveragePrice);
			if (!TryGetYByPriceInPrimaryPanel(displayAveragePrice, out y, out panelTop, out panelBottom) || routedOrderCanvas == null) return;
			double plotRight = GetPlotRightX();
			double width = 32;
			double height = 18;
			double gap = 4;
			double startX = Math.Max(0, plotRight - (width * 2) - gap - 8);
			double buttonTop = ClampRoutedTop(y - 31, height, panelTop, panelBottom);
			if (!hasLimit) AddRoutedProtectionButton("TP", startX, buttonTop, width, height, GetRouterBrush(visual.BuyColor, System.Windows.Media.Brushes.LimeGreen), acc, instrument, pos, true, oco, visual);
			if (!hasStop) AddRoutedProtectionButton("SL", startX + width + gap, buttonTop, width, height, GetRouterBrush(visual.SellColor, System.Windows.Media.Brushes.Salmon), acc, instrument, pos, false, oco, visual);
		}
		private void AddRoutedProtectionButton(string text, double x, double y, double width, double height, System.Windows.Media.Brush brush, Account acc, Instrument instrument, Position pos, bool isTp, string oco, OrcaExecutionRouterSettings visual) {
			var label = new System.Windows.Controls.TextBlock { Text = text, Foreground = System.Windows.Media.Brushes.White, FontFamily = GetRouterFont(visual), FontWeight = GetRouterFontWeight(visual), FontSize = visual.FontSize, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			var button = new System.Windows.Controls.Border { Background = GetRouterBackgroundBrush(brush, visual.CoverButtonBackgroundOpacity), CornerRadius = new CornerRadius(4), Width = width, Height = height, Child = label, Cursor = Cursors.SizeNS };
			button.MouseLeftButtonDown += (s, e) => {
				StartRoutedProtectionDrag(isTp, acc, instrument, pos, string.IsNullOrEmpty(oco) ? "OrcaOCO_" + Guid.NewGuid().ToString("N") : oco);
				e.Handled = true;
			};
			routedOrderCanvas.Children.Add(button);
			System.Windows.Controls.Canvas.SetLeft(button, x);
			System.Windows.Controls.Canvas.SetTop(button, y);
		}
		private void StartRoutedProtectionDrag(bool isTp, Account acc, Instrument instrument, Position pos, string oco) {
			isDraggingRoutedTP = isTp;
			isDraggingRoutedSL = !isTp;
			isDraggingRoutedOrder = false;
			routedDragOrder = null;
			routedDragOrders = null;
			routedDragAccount = acc;
			routedDragInstrument = instrument;
			routedDragSide = pos.MarketPosition;
			routedDragEntryPrice = RoundPriceToInstrumentTick(instrument, pos.AveragePrice);
			routedDragQuantity = Math.Abs(pos.Quantity);
			routedDragOco = oco;
			routedDragPrice = routedDragEntryPrice;
			Mouse.Capture(routedOrderCanvas);
		}
		private void StartRoutedOrderChangeDrag(Account acc, Order order) {
			StartRoutedOrderChangeDrag(acc, order == null ? null : new List<Order> { order });
		}
		private void StartRoutedOrderChangeDrag(Account acc, List<Order> orders) {
			if (acc == null || orders == null || orders.Count == 0) return;
			Order order = orders.FirstOrDefault(o => o != null);
			if (order == null) return;
			isDraggingRoutedTP = false;
			isDraggingRoutedSL = false;
			isDraggingRoutedOrder = true;
			routedDragAccount = acc;
			routedDragOrder = order;
			routedDragOrders = orders.Where(o => o != null).ToList();
			routedDragInstrument = order.Instrument;
			routedDragPrice = GetOrderDisplayPrice(order);
			routedDragQuantity = routedDragOrders.Sum(o => Math.Max(1, o.Quantity));
			Position pos = acc.Positions.FirstOrDefault(p => IsSameInstrument(p.Instrument, order.Instrument));
			if (IsPositionReducingOrder(pos, order)) {
				routedDragSide = pos.MarketPosition;
				routedDragEntryPrice = RoundPriceToInstrumentTick(order.Instrument, pos.AveragePrice);
			} else {
				routedDragSide = MarketPosition.Flat;
				routedDragEntryPrice = 0;
			}
			Mouse.Capture(routedOrderCanvas);
			DrawRoutedDragPreview();
		}
		private void RoutedOrderCanvas_MouseMove(object sender, MouseEventArgs e) {
			if (!isDraggingRoutedTP && !isDraggingRoutedSL && !isDraggingRoutedOrder) return;
			Point point = e.GetPosition(routedOrderCanvas);
			double price = GetPriceByY(point.Y);
			double tick = GetChartInstrument()?.MasterInstrument?.TickSize ?? 0.25;
			routedDragPrice = Math.Round(price / tick) * tick;
			DrawRoutedDragPreview();
			e.Handled = true;
		}
		private void RoutedOrderCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (!isDraggingRoutedTP && !isDraggingRoutedSL && !isDraggingRoutedOrder) return;
			double y, panelTop, panelBottom;
			if (!TryGetYByPriceInPrimaryPanel(routedDragPrice, out y, out panelTop, out panelBottom)) {
				ClearRoutedProtectionDrag();
				UpdatePnL(null, null);
				e.Handled = true;
				return;
			}
			if (isDraggingRoutedOrder) SubmitRoutedOrderChangeDrag();
			else SubmitRoutedProtectionDrag();
			ClearRoutedProtectionDrag();
			UpdatePnL(null, null);
			e.Handled = true;
		}
		private void DrawRoutedDragPreview() {
			if (routedOrderCanvas == null || routedDragPrice <= 0) return;
			OrcaExecutionRouterSettings visual = OrcaExecutionRouter.GetSettings();
			if (routedDragLine == null) {
				System.Windows.Media.Brush brush = isDraggingRoutedTP ? GetRouterBrush(visual.BuyColor, System.Windows.Media.Brushes.LimeGreen) : isDraggingRoutedSL ? GetRouterBrush(visual.SellColor, System.Windows.Media.Brushes.Salmon) : GetRouterBrush(visual.DragChangeColor, System.Windows.Media.Brushes.Gold);
				routedDragLine = new System.Windows.Shapes.Line { X1 = 0, StrokeThickness = Math.Max(1.5, visual.LineThickness), Stroke = brush, IsHitTestVisible = false };
				routedDragTxt = new System.Windows.Controls.TextBlock { Foreground = GetRouterBrush(visual.TextColor, System.Windows.Media.Brushes.Black), FontFamily = GetRouterFont(visual), FontWeight = GetRouterFontWeight(visual), FontSize = visual.FontSize, IsHitTestVisible = false };
				routedDragPill = new System.Windows.Controls.Border { Background = GetRouterBackgroundBrush(routedDragLine.Stroke, visual.LabelBackgroundOpacity), CornerRadius = new CornerRadius(4), Padding = new Thickness(4, 2, 4, 2), Child = routedDragTxt, IsHitTestVisible = false };
				routedOrderCanvas.Children.Add(routedDragLine);
				routedOrderCanvas.Children.Add(routedDragPill);
			}
			double y, panelTop, panelBottom;
			if (!TryGetYByPriceInPrimaryPanel(routedDragPrice, out y, out panelTop, out panelBottom)) {
				routedDragLine.Visibility = Visibility.Collapsed;
				routedDragPill.Visibility = Visibility.Collapsed;
				return;
			}
			routedDragLine.Visibility = Visibility.Visible;
			routedDragPill.Visibility = Visibility.Visible;
			double plotRight = GetPlotRightX();
			routedDragLine.X2 = plotRight; routedDragLine.Y1 = routedDragLine.Y2 = y;
			string label = isDraggingRoutedOrder ? BuildRoutedOrderDragLabel() : BuildRoutedProtectionDragLabel();
			routedDragTxt.Text = label;
			routedDragPill.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			System.Windows.Controls.Canvas.SetLeft(routedDragPill, Math.Max(0, plotRight - routedDragPill.DesiredSize.Width - visual.LabelRightPadding));
			System.Windows.Controls.Canvas.SetTop(routedDragPill, ClampRoutedTop(y - 11, routedDragPill.DesiredSize.Height, panelTop, panelBottom));
		}
		private string BuildRoutedProtectionDragLabel() {
			string label = $"{(isDraggingRoutedTP ? "TP" : "SL")} @ {routedDragPrice:F2}";
			string amount = BuildProtectionAmountText(routedDragInstrument, routedDragSide, routedDragEntryPrice, routedDragPrice, routedDragQuantity);
			return string.IsNullOrEmpty(amount) ? label : label + " | " + amount;
		}
		private string BuildRoutedOrderDragLabel() {
			int quantity = routedDragOrders != null && routedDragOrders.Count > 0 ? routedDragOrders.Sum(o => Math.Max(1, o.Quantity)) : Math.Max(1, routedDragOrder?.Quantity ?? 1);
			string label = routedDragOrder == null
				? $"Order @ {routedDragPrice:F2}"
				: $"{ShortOrderAction(routedDragOrder.OrderAction)} {quantity} {GetOrderDragLabel(routedDragOrder)} @ {routedDragPrice:F2}";
			string amount = BuildProtectionAmountText(routedDragInstrument, routedDragSide, routedDragEntryPrice, routedDragPrice, routedDragQuantity);
			return string.IsNullOrEmpty(amount) ? label : label + " | " + amount;
		}
		private void SubmitRoutedProtectionDrag() {
			try {
				if (routedDragAccount == null || routedDragInstrument == null || routedDragQuantity <= 0 || routedDragSide == MarketPosition.Flat || routedDragPrice <= 0) return;
				OrderAction action = routedDragSide == MarketPosition.Long ? OrderAction.Sell : OrderAction.BuyToCover;
				if (isDraggingRoutedTP) {
					if (routedDragSide == MarketPosition.Long ? routedDragPrice > routedDragEntryPrice : routedDragPrice < routedDragEntryPrice)
						routedDragAccount.Submit(new[] { routedDragAccount.CreateOrder(routedDragInstrument, action, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, routedDragQuantity, routedDragPrice, 0, routedDragOco, "Target", DateTime.MaxValue, null) });
				} else if (routedDragSide == MarketPosition.Long ? routedDragPrice < routedDragEntryPrice : routedDragPrice > routedDragEntryPrice) {
					routedDragAccount.Submit(new[] { routedDragAccount.CreateOrder(routedDragInstrument, action, OrderType.StopMarket, OrderEntry.Manual, TimeInForce.Day, routedDragQuantity, 0, routedDragPrice, routedDragOco, "Stop", DateTime.MaxValue, null) });
				}
			} catch { }
		}
		private void SubmitRoutedOrderChangeDrag() {
			try {
				List<Order> orders = routedDragOrders != null && routedDragOrders.Count > 0 ? routedDragOrders : (routedDragOrder != null ? new List<Order> { routedDragOrder } : null);
				if (routedDragAccount == null || orders == null || orders.Count == 0 || routedDragPrice <= 0) return;
				var changeOrders = new List<Order>();
				foreach (Order order in orders) {
					if (order == null) continue;
					if (order.OrderType == OrderType.Limit) SetOrderDouble(order, "LimitPriceChanged", routedDragPrice);
					else if (order.OrderType == OrderType.StopMarket || order.OrderType == OrderType.StopLimit) SetOrderDouble(order, "StopPriceChanged", routedDragPrice);
					else continue;
					changeOrders.Add(order);
				}
				if (changeOrders.Count > 0)
					routedDragAccount.Change(changeOrders.ToArray());
			} catch { }
		}
		private string GetOrderDragLabel(Order order) {
			if (order == null) return "Order";
			return order.OrderType == OrderType.Limit ? "LMT" : order.OrderType == OrderType.StopMarket ? "STP" : order.OrderType.ToString();
		}
		private void SetOrderDouble(Order order, string propertyName, double value) {
			try { order.GetType().GetProperty(propertyName)?.SetValue(order, value, null); } catch { }
		}
		private System.Windows.Controls.ContextMenu BuildRoutedOrderContextMenu(Account acc, Order order) {
			var menu = new System.Windows.Controls.ContextMenu();
			if (acc == null || order == null) return menu;
			int current = Math.Max(1, order.Quantity);
			menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Qty " + current, IsEnabled = false });
			AddRoutedQuantityMenuItem(menu, "Qty +1", acc, order, current + 1);
			if (current > 1) AddRoutedQuantityMenuItem(menu, "Qty -1", acc, order, current - 1);
			int positionQty = GetPositionQuantity(acc, order.Instrument);
			if (positionQty > 0 && positionQty != current) AddRoutedQuantityMenuItem(menu, "Match position (" + positionQty + ")", acc, order, positionQty);
			return menu;
		}
		private void AddRoutedQuantityMenuItem(System.Windows.Controls.ContextMenu menu, string header, Account acc, Order order, int quantity) {
			var item = new System.Windows.Controls.MenuItem { Header = header };
			item.Click += (s, e) => ChangeRoutedOrderQuantity(acc, order, quantity);
			menu.Items.Add(item);
		}
		private int GetPositionQuantity(Account acc, Instrument instrument) {
			try {
				var pos = acc?.Positions.FirstOrDefault(p => IsSameInstrument(p.Instrument, instrument));
				return pos != null && pos.MarketPosition != MarketPosition.Flat ? Math.Abs(pos.Quantity) : 0;
			} catch { return 0; }
		}
		private void ChangeRoutedOrderQuantity(Account acc, Order order, int quantity) {
			try {
				if (acc == null || order == null || quantity < 1) return;
				order.QuantityChanged = quantity;
				acc.Change(new[] { order });
				lastProtectionSyncInstrument = order.Instrument?.FullName ?? "";
				lastProtectionSyncQuantity = GetPositionQuantity(acc, order.Instrument);
				UpdatePnL(null, null);
			} catch { }
		}
		private void CancelRoutedOrder(Account acc, Order order) {
			try {
				if (acc == null || order == null) return;
				acc.Cancel(new[] { order });
				UpdatePnL(null, null);
			} catch { }
		}
		private void CancelRoutedOrders(Account acc, List<Order> orders) {
			try {
				if (acc == null || orders == null || orders.Count == 0) return;
				Order[] cancelOrders = orders.Where(o => o != null).ToArray();
				if (cancelOrders.Length == 0) return;
				acc.Cancel(cancelOrders);
				UpdatePnL(null, null);
			} catch { }
		}
		private void ClearRoutedProtectionDrag() {
			isDraggingRoutedTP = isDraggingRoutedSL = isDraggingRoutedOrder = false;
			routedDragAccount = null; routedDragInstrument = null; routedDragSide = MarketPosition.Flat; routedDragEntryPrice = routedDragPrice = 0; routedDragQuantity = 0; routedDragOco = "";
			routedDragLine = null; routedDragPill = null; routedDragTxt = null; routedDragOrder = null; routedDragOrders = null;
			Mouse.Capture(null);
		}
		private double GetOrderDisplayPrice(Order order) {
			if (order == null) return 0;
			if (order.OrderType == OrderType.Limit) return GetOrderDouble(order, "LimitPrice");
			if (order.OrderType == OrderType.StopMarket) return GetOrderDouble(order, "StopPrice");
			double stop = GetOrderDouble(order, "StopPrice");
			if (stop > 0) return stop;
			return GetOrderDouble(order, "LimitPrice");
		}
		private double GetOrderDouble(Order order, string propertyName) {
			try {
				object value = order.GetType().GetProperty(propertyName)?.GetValue(order, null);
				if (value == null) return 0;
				return Convert.ToDouble(value);
			} catch { return 0; }
		}
		private void PlaceDragOrderAt(double p) { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc==null || ins==null) return; int q = 1; int.TryParse(txtContracts.Text, out q); if (q < 1 || !ConfirmLiveOrderIfNeeded(acc, "Submit Orca drag order")) return; string id = "Drag_" + Guid.NewGuid().ToString("N"); OrderAction act = dragOrderType.Contains("Buy") ? OrderAction.Buy : OrderAction.Sell; OrderType typ = dragOrderType.Contains("Stop") ? OrderType.StopMarket : OrderType.Limit; acc.Submit(new[] { acc.CreateOrder(ins, act, typ, OrderEntry.Manual, TimeInForce.Day, q, typ==OrderType.Limit?p:0, typ==OrderType.StopMarket?p:0, "", id, DateTime.MaxValue, null) }); } catch { } }
		private void CancelDragOrder() { isDragOrderActive = false; if (dragCanvas != null) { (System.Windows.Media.VisualTreeHelper.GetParent(dragCanvas) as System.Windows.Controls.Panel)?.Children.Remove(dragCanvas); dragCanvas = null; } if (attachedTab?.ChartControl != null) { var w = Window.GetWindow(attachedTab.ChartControl); if (w != null) w.PreviewKeyDown -= Window_PreviewKeyDown_CancelDrag; } }
		private void Window_PreviewKeyDown_CancelDrag(object s, KeyEventArgs e) { if (e.Key == Key.Escape) { CancelDragOrder(); e.Handled = true; } }
		private void Flatten() { try { CancelStagedBracket(); Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc != null && ins != null && ConfirmFlattenIfNeeded(acc)) { foreach (var o in acc.Orders) if (IsSameInstrument(o.Instrument, ins) && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted)) acc.Cancel(new[] { o }); ClosePosition(100); } } catch { } }
		private void MoveToBreakeven() { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc != null && ins != null) { var p = acc.Positions.FirstOrDefault(x => IsSameInstrument(x.Instrument, ins)); if (p != null && p.MarketPosition != MarketPosition.Flat) { foreach (var o in acc.Orders) if (IsSameInstrument(o.Instrument, ins) && o.OrderType == OrderType.StopMarket) { o.StopPriceChanged = p.AveragePrice; acc.Change(new[] { o }); } } } } catch { } }
		private void ClosePosition(double pct) { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc != null && ins != null) { var p = acc.Positions.FirstOrDefault(x => IsSameInstrument(x.Instrument, ins)); if (p != null && p.MarketPosition != MarketPosition.Flat) { int q = (int)Math.Max(1, Math.Round(p.Quantity * pct / 100.0)); acc.Submit(new[] { acc.CreateOrder(ins, p.MarketPosition == MarketPosition.Long ? OrderAction.Sell : OrderAction.BuyToCover, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, q, 0, 0, "", "Orca", DateTime.MaxValue, null) }); } } } catch { } }
		private void HookExecutionEvent(Account acc) { if (acc != null && hookedAccount != acc) { if (hookedAccount != null) hookedAccount.ExecutionUpdate -= OnExecutionUpdate; acc.ExecutionUpdate += OnExecutionUpdate; hookedAccount = acc; } }
		private void OnExecutionUpdate(object s, ExecutionEventArgs e) {
			try {
				if (e.Execution?.Account == null) return;
				double risk = 500; Dispatcher.Invoke(() => { double.TryParse(txtRisk.Text, out risk); });
				double cur = e.Execution.Account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar), diff = cur - baselineRealizedPnL;
				currentTradeRealizedPnL = diff;
				if (e.Execution.MarketPosition == MarketPosition.Flat && risk > 0) {
					Dispatcher.InvokeAsync(() => { totalSessionR += currentTradeRealizedPnL / risk; });
					baselineRealizedPnL = cur; currentTradeRealizedPnL = 0;
				}
				if (pendingEntryName == e.Execution.Order.Name && pendingStopPrice != 0 && e.Execution.Quantity > 0) {
					string oco = "OrcaOCO_" + Guid.NewGuid().ToString("N");
					OrderAction act = e.Execution.Order.OrderAction == OrderAction.Buy ? OrderAction.Sell : OrderAction.BuyToCover;
					e.Execution.Account.Submit(new[] {
						e.Execution.Account.CreateOrder(e.Execution.Instrument, act, OrderType.StopMarket, OrderEntry.Manual, TimeInForce.Day, e.Execution.Quantity, 0, pendingStopPrice, oco, "Stop", DateTime.MaxValue, null),
						e.Execution.Account.CreateOrder(e.Execution.Instrument, act, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, e.Execution.Quantity, pendingTargetPrice, 0, oco, "Target", DateTime.MaxValue, null)
					});
					if (e.Execution.Order.OrderState == OrderState.Filled || e.Execution.Order.OrderState == OrderState.Cancelled || e.Execution.Order.OrderState == OrderState.Rejected) {
						pendingEntryName = null;
					}
				}

				if (e.Execution.Order.Name != "Stop" && e.Execution.Order.Name != "Target" && e.Execution.Quantity > 0) {
					var acc = e.Execution.Account; var ins = e.Execution.Instrument;
					Dispatcher.InvokeAsync(() => { SyncProtectionOrders(acc, ins); }, System.Windows.Threading.DispatcherPriority.Background);
				}
			} catch { }
		}

		private void SyncProtectionOrders(Account acc, Instrument ins) {
			try {
				var pos = acc.Positions.FirstOrDefault(p => IsSameInstrument(p.Instrument, ins));
				int absPos = (pos != null) ? Math.Abs(pos.Quantity) : 0;
				void SyncType(string name) {
					var orders = acc.Orders.Where(o => IsSameInstrument(o.Instrument, ins) && o.Name == name && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted)).OrderByDescending(o => o.Time).ToList();
					int total = orders.Sum(o => o.Quantity);
					if (total < absPos && absPos > 0 && orders.Count > 0) {
						Order newest = orders[0];
						newest.QuantityChanged = newest.Quantity + (absPos - total);
						acc.Change(new[] { newest });
					} else if (total > absPos) {
						int toReduce = total - absPos;
						foreach (var o in orders) {
							if (toReduce <= 0) break;
							int canReduce = o.Quantity;
							if (toReduce >= canReduce) { acc.Cancel(new[] { o }); toReduce -= canReduce; }
							else { o.QuantityChanged = o.Quantity - toReduce; acc.Change(new[] { o }); toReduce = 0; }
						}
					}
				}
				SyncType("Stop"); SyncType("Target");
			} catch { }
		}
		private void SyncProtectionOrdersOnPositionChange(Account acc, Instrument ins, int absPosition) {
			string instrumentName = ins?.FullName ?? "";
			if (instrumentName == lastProtectionSyncInstrument && absPosition == lastProtectionSyncQuantity) return;
			lastProtectionSyncInstrument = instrumentName;
			lastProtectionSyncQuantity = absPosition;
			if (acc != null && ins != null) SyncProtectionOrders(acc, ins);
		}
		private void OnCalculatorLineMoved(object s, PropertyChangedEventArgs e) {
			if (e.PropertyName != "StartAnchor" && e.PropertyName != "EndAnchor") return;
			if (isSyncingCalculatorLines) return;
			try {
				if (isCalculatorActive && isFixedPoints && object.ReferenceEquals(s, hEntry))
					ShiftFixedPointCalculatorBracketFromEntryMove();
				else
					SnapshotCalculatorPrices();
			} catch { isSyncingCalculatorLines = false; }
			UpdatePnL(null, null);
		}
		private void ShiftFixedPointCalculatorBracketFromEntryMove() {
			if (hEntry == null || hStop == null || hEntry.StartAnchor == null || hStop.StartAnchor == null) return;
			double entry = hEntry.StartAnchor.Price;
			if (double.IsNaN(lastCalcEntryPrice)) {
				SnapshotCalculatorPrices();
				return;
			}
			double delta = entry - lastCalcEntryPrice;
			if (Math.Abs(delta) <= 0.0000001) {
				SnapshotCalculatorPrices();
				return;
			}
			isSyncingCalculatorLines = true;
			try {
				double stop = double.IsNaN(lastCalcStopPrice) ? hStop.StartAnchor.Price + delta : lastCalcStopPrice + delta;
				hStop.StartAnchor.Price = hStop.EndAnchor.Price = RoundToTick(stop);
				if (hTarget != null && hTarget.StartAnchor != null) {
					double target = double.IsNaN(lastCalcTargetPrice) ? hTarget.StartAnchor.Price + delta : lastCalcTargetPrice + delta;
					hTarget.StartAnchor.Price = hTarget.EndAnchor.Price = RoundToTick(target);
				}
			} finally {
				isSyncingCalculatorLines = false;
			}
			SnapshotCalculatorPrices();
		}
		private void EnforceFixedPointCalculatorBracket(bool forceFromPanelPoints) {
			if (!isCalculatorActive || !isFixedPoints || isSyncingCalculatorLines) return;
			if (hEntry == null || hStop == null || hEntry.StartAnchor == null || hStop.StartAnchor == null) return;

			try {
				double entry = hEntry.StartAnchor.Price;
				double stop = hStop.StartAnchor.Price;
				double target = hTarget?.StartAnchor != null ? hTarget.StartAnchor.Price : double.NaN;
				double tick = GetActiveInstrument()?.MasterInstrument?.TickSize ?? 0.25;
				if (tick <= 0) tick = 0.25;
				double epsilon = Math.Max(0.0000001, tick * 0.1);

				if (double.IsNaN(lastCalcEntryPrice)) {
					SnapshotCalculatorPrices();
					return;
				}

				bool entryMoved = Math.Abs(entry - lastCalcEntryPrice) > epsilon;
				bool stopMoved = !double.IsNaN(lastCalcStopPrice) && Math.Abs(stop - lastCalcStopPrice) > epsilon;
				bool targetMoved = !double.IsNaN(target) && !double.IsNaN(lastCalcTargetPrice) && Math.Abs(target - lastCalcTargetPrice) > epsilon;

				if (!forceFromPanelPoints && !entryMoved) {
					if (stopMoved || targetMoved)
						SnapshotCalculatorPrices();
					return;
				}

				double fallbackPoints = Math.Abs(entry - stop);
				double points = fallbackPoints;
				if (txtPoints != null && double.TryParse(txtPoints.Text, out double configuredPoints) && configuredPoints > 0)
					points = configuredPoints;
				if (points <= 0 || double.IsNaN(points) || double.IsInfinity(points)) {
					SnapshotCalculatorPrices();
					return;
				}

				double delta = entry - lastCalcEntryPrice;
				double newStop = RoundToTick(isLongSelected ? entry - points : entry + points);
				isSyncingCalculatorLines = true;
				try {
					hStop.StartAnchor.Price = hStop.EndAnchor.Price = newStop;
					if (entryMoved && hTarget != null && hTarget.StartAnchor != null) {
						double baseTarget = double.IsNaN(lastCalcTargetPrice) ? hTarget.StartAnchor.Price : lastCalcTargetPrice;
						hTarget.StartAnchor.Price = hTarget.EndAnchor.Price = RoundToTick(baseTarget + delta);
					}
				} finally {
					isSyncingCalculatorLines = false;
				}

				SnapshotCalculatorPrices();
				attachedTab?.ChartControl?.InvalidateVisual();
			} catch {
				isSyncingCalculatorLines = false;
			}
		}
		private void SnapshotCalculatorPrices() {
			try {
				lastCalcEntryPrice = hEntry?.StartAnchor != null ? hEntry.StartAnchor.Price : double.NaN;
				lastCalcStopPrice = hStop?.StartAnchor != null ? hStop.StartAnchor.Price : double.NaN;
				lastCalcTargetPrice = hTarget?.StartAnchor != null ? hTarget.StartAnchor.Price : double.NaN;
			} catch {
				lastCalcEntryPrice = lastCalcStopPrice = lastCalcTargetPrice = double.NaN;
			}
		}
		private void SpawnCalculator() {
			RemoveCalculator(); try {
				if (attachedTab?.ChartControl == null) return; NinjaScriptBase o = attachedTab.ChartControl.Indicators.FirstOrDefault() as NinjaScriptBase; if (o == null) return; calcOwner = o; double cp = GetActivePrice(); Instrument ins = GetActiveInstrument(); if (ins == null) return; double tk = ins.MasterInstrument.TickSize; int sT = ins.FullName.Contains("ES") ? 20 : 100; double stopDistance = sT * tk; if (isFixedPoints && txtPoints != null && double.TryParse(txtPoints.Text, out double configuredPoints) && configuredPoints > 0) stopDistance = configuredPoints; double targetDistance = stopDistance * 2; double sY = isLongSelected ? cp - stopDistance : cp + stopDistance, tY = isLongSelected ? cp + targetDistance : cp - targetDistance;
				System.Windows.Media.Brush entryBrush = GetChartEntryBrush(isLongSelected);
				hEntry = Draw.HorizontalLine(o,"OEnt",cp,entryBrush,DashStyleHelper.Solid,2); hTarget = Draw.HorizontalLine(o,"OTar",tY,GetChartProfitBrush(),DashStyleHelper.Solid,2); hStop = Draw.HorizontalLine(o,"OStp",sY,GetChartRiskBrush(),DashStyleHelper.Solid,2);
				calcCanvas = new System.Windows.Controls.Canvas { IsHitTestVisible = false }; System.Windows.Controls.Panel.SetZIndex(calcCanvas, 9998); System.Windows.Controls.Grid.SetRow(calcCanvas, System.Windows.Controls.Grid.GetRow(attachedTab.ChartControl)); System.Windows.Controls.Grid.SetColumn(calcCanvas, System.Windows.Controls.Grid.GetColumn(attachedTab.ChartControl)); (attachedTab.Content as System.Windows.Controls.Grid).Children.Add(calcCanvas);
				void AddP(System.Windows.Media.Brush b, out System.Windows.Controls.TextBlock t, out System.Windows.Controls.Border p) { t = new System.Windows.Controls.TextBlock(); ApplyChartLabelTextStyle(t); p = new System.Windows.Controls.Border { Background = GetRouterBackgroundBrush(b, 1), CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 3, 7, 3), Child = t }; System.Windows.Controls.Canvas.SetRight(p, 65); calcCanvas.Children.Add(p); }
				AddP(entryBrush, out cEntryTxt, out cEntryPill); AddP(GetChartRiskBrush(), out cStopTxt, out cStopPill); AddP(GetChartProfitBrush(), out cTargetTxt, out cTargetPill);
				if (renderHandler == null) { renderHandler = new EventHandler(OnRenderFrame); System.Windows.Media.CompositionTarget.Rendering += renderHandler; }
				foreach (var l in new[] { hEntry, hStop, hTarget }) if (l != null) { l.IsLocked = l.IsAutoScale = false; if (l is INotifyPropertyChanged i) i.PropertyChanged += OnCalculatorLineMoved; }
				SnapshotCalculatorPrices();
				isCalculatorActive = true; UpdateCalcLabelVisuals(); UpdatePnL(null, null); attachedTab.ChartControl.InvalidateVisual();
			} catch { }
		}
		private void RemoveCalculator() { try { isCalculatorActive = false; isSyncingCalculatorLines = true; if (calcCanvas != null) { (attachedTab?.Content as System.Windows.Controls.Grid)?.Children.Remove(calcCanvas); calcCanvas = null; } if (calcOwner != null) { Draw.HorizontalLine(calcOwner,"OEnt",0,System.Windows.Media.Brushes.Black,DashStyleHelper.Solid,1); Draw.HorizontalLine(calcOwner,"OTar",0,System.Windows.Media.Brushes.Black,DashStyleHelper.Solid,1); Draw.HorizontalLine(calcOwner,"OStp",0,System.Windows.Media.Brushes.Black,DashStyleHelper.Solid,1); } foreach (var l in new[] { hEntry, hStop, hTarget }) if (l != null) l.StartAnchor.Price = l.EndAnchor.Price = 0; isSyncingCalculatorLines = false; SnapshotCalculatorPrices(); if (renderHandler != null) { System.Windows.Media.CompositionTarget.Rendering -= renderHandler; renderHandler = null; } calcOwner = null; attachedTab?.ChartControl?.InvalidateVisual(); } catch { isSyncingCalculatorLines = false; } }
	}
}
