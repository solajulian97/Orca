#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
		private readonly Dictionary<ChartTab, bool> panelVisibilityByTab = new Dictionary<ChartTab, bool>();
		private const string PanelVersion = "OrcaRiskPanel.Recovered.20260429.1";
		private const string ActiveVersionKey = "OrcaRiskManager.ActivePanelVersion";

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

			chartWindow.PreviewKeyDown += (s, e) => {
				if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control) {
					e.Handled = true;
					if (e.IsRepeat) return;
					if (!IsActivePanelVersion()) return;
					TogglePanelVisibility(chartWindow, e.OriginalSource);
				}
			};

			chartWindow.Dispatcher.InvokeAsync(() => {
				if (chartWindow.MainTabControl != null) {
					chartWindow.MainTabControl.SelectionChanged += (s, e) => { RefreshChartWindowPanels(chartWindow); };
				}
			});
		}

		private void TogglePanelVisibility(Chart chartWindow, object eventSource)
		{
			if (!IsActivePanelVersion()) return;
			if (chartWindow.MainTabControl == null || chartWindow.MainTabControl.Items.Count == 0) return;
			SweepStalePanels(chartWindow);
			ChartTab tab = GetActiveChartTab(chartWindow, eventSource);
			if (tab == null) return;
			CleanupInactivePanels(chartWindow, tab);
			if (tab.Content is System.Windows.Controls.Grid tabGrid) {
				OrcaRiskPanel currentPanel = GetCurrentPanel(tabGrid);
				if (currentPanel != null) {
					bool shouldShow = !(panelVisibilityByTab.TryGetValue(tab, out bool isVisible) && isVisible);
					panelVisibilityByTab[tab] = shouldShow;
					SetPanelVisibility(tabGrid, currentPanel, shouldShow);
					return;
				}

				RemoveRiskPanels(tabGrid);
				panelVisibilityByTab[tab] = true;
				InsertChartTraderControl(chartWindow, tab);
			}
		}

		private void InsertChartTraderControl(Chart chartWindow, ChartTab targetTab = null)
		{
			try {
				if (chartWindow.MainTabControl == null || chartWindow.MainTabControl.Items.Count == 0) return;
				ChartTab tab = targetTab ?? GetSelectedChartTab(chartWindow);
				if (tab == null) return;
				if (tab.Content is System.Windows.Controls.Grid tabGrid) {
					if (!ShouldShowPanel(tab)) {
						RemoveRiskPanels(tabGrid);
						return;
					}

					OrcaRiskPanel currentPanel = GetCurrentPanel(tabGrid);
					if (currentPanel != null) {
						SetPanelVisibility(tabGrid, currentPanel, true);
						return;
					}

					RemoveRiskPanels(tabGrid);
					OrcaRiskPanel orcaPanel = new OrcaRiskPanel(tab);
					orcaPanel.Tag = PanelVersion;
					orcaPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
					if (tabGrid.ColumnDefinitions.Count == 0) tabGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
					tabGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(235) });
					System.Windows.Controls.Grid.SetColumn(orcaPanel, tabGrid.ColumnDefinitions.Count - 1);
					tabGrid.Children.Add(orcaPanel);
				}
			} catch { }
		}

		private void RefreshChartWindowPanels(Chart chartWindow)
		{
			if (!IsActivePanelVersion()) return;
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
			CleanupInactivePanels(chartWindow, activeTab);
			if (activeTab.Content is System.Windows.Controls.Grid activeGrid && !ShouldShowPanel(activeTab)) {
				RemoveRiskPanels(activeGrid);
				return;
			}
			InsertChartTraderControl(chartWindow, activeTab);
		}

		private bool ShouldShowPanel(ChartTab tab)
		{
			if (panelVisibilityByTab.TryGetValue(tab, out bool isVisible)) return isVisible;
			panelVisibilityByTab[tab] = true;
			return true;
		}

		private void SweepStalePanels(Chart chartWindow)
		{
			foreach (object item in chartWindow.MainTabControl.Items) {
				ChartTab tab = item as ChartTab;
				if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
				if (tab?.Content is System.Windows.Controls.Grid tabGrid) RemoveRiskPanels(tabGrid, staleOnly: true);
			}
		}

		private void CleanupInactivePanels(Chart chartWindow, ChartTab activeTab)
		{
			foreach (object item in chartWindow.MainTabControl.Items) {
				ChartTab tab = item as ChartTab;
				if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
				if (tab == null || object.ReferenceEquals(tab, activeTab)) continue;
				if (tab.Content is System.Windows.Controls.Grid tabGrid) RemoveRiskPanels(tabGrid);
			}
		}

		private OrcaRiskPanel GetCurrentPanel(System.Windows.Controls.Grid tabGrid)
		{
			foreach (UIElement el in tabGrid.Children) {
				if (el is OrcaRiskPanel panel && string.Equals(panel.Tag as string, PanelVersion, StringComparison.Ordinal))
					return panel;
			}
			return null;
		}

		private void SetPanelVisibility(System.Windows.Controls.Grid tabGrid, OrcaRiskPanel panel, bool isVisible)
		{
			int col = System.Windows.Controls.Grid.GetColumn(panel);
			if (col < 0 || col >= tabGrid.ColumnDefinitions.Count) return;
			tabGrid.ColumnDefinitions[col].Width = isVisible ? new GridLength(235) : new GridLength(0);
			panel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
			panel.HorizontalAlignment = HorizontalAlignment.Stretch;
		}

		private void RemoveRiskPanels(System.Windows.Controls.Grid tabGrid, bool staleOnly = false)
		{
			var panels = new List<UIElement>();
			var columns = new List<int>();
			foreach (UIElement el in tabGrid.Children) {
				if (el.GetType().Name != "OrcaRiskPanel") continue;
				if (staleOnly && el is OrcaRiskPanel panel && string.Equals(panel.Tag as string, PanelVersion, StringComparison.Ordinal)) continue;
				try { el.GetType().GetMethod("Cleanup")?.Invoke(el, null); } catch { }
				panels.Add(el);
				columns.Add(System.Windows.Controls.Grid.GetColumn(el));
			}
			foreach (UIElement el in panels) tabGrid.Children.Remove(el);
			foreach (int col in columns.Distinct().OrderByDescending(x => x)) {
				if (col >= 0 && col < tabGrid.ColumnDefinitions.Count)
					tabGrid.ColumnDefinitions.RemoveAt(col);
			}
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
			ChartTab tab = GetChartTabFromWindowProperty(chartWindow);
			if (tab != null) return tab;

			tab = GetSelectedChartTab(chartWindow);
			if (tab != null) return tab;

			tab = FindOwningChartTab(eventSource as DependencyObject);
			if (tab != null && IsTabVisible(tab)) return tab;

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
	}

	public class OrcaRiskPanel : System.Windows.Controls.UserControl
	{
		private ChartTab attachedTab;
		private System.Windows.Threading.DispatcherTimer pnlTimer;
		private System.Windows.Threading.DispatcherTimer routedOverlayTimer;
		private System.Windows.Controls.Button btnLong, btnShort, btnMarket, btnLimit, btnStop, btnOpen, btnClose, btnBuyMkt, btnSellMkt, btnBuyAsk, btnSellBid, btnBreakeven, btnCloseAll, btnFixedDollar, btnFixedSize, btnBuyLmt, btnSellLmt, btnBuyStop, btnSellStop;
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
		private string lastProtectionSyncInstrument = "";
		private int lastProtectionSyncQuantity = -1;
		private bool isLongSelected = true, isFixedDollar = true, isCalculatorActive = false;
		private OrderType selectedOrderType = OrderType.Market;
		private string pendingEntryName = null;
		private double pendingStopPrice = 0, pendingTargetPrice = 0, baselineRealizedPnL = 0, currentTradeRealizedPnL = 0;
		private int pendingContracts = 0;
		private Account hookedAccount = null;
		private static double totalSessionR = 0;
		private NinjaScriptBase calcOwner = null;
		private NinjaTrader.NinjaScript.DrawingTools.HorizontalLine hEntry, hStop, hTarget;
		private System.Windows.Controls.Canvas calcCanvas, routedOrderCanvas;
		private System.Windows.Controls.Border cEntryPill, cStopPill, cTargetPill;
		private System.Windows.Controls.TextBlock cEntryTxt, cStopTxt, cTargetTxt;
		private EventHandler renderHandler;
		private readonly List<Rect> routedLabelSlots = new List<Rect>();

		public OrcaRiskPanel(ChartTab tab) { attachedTab = tab; BuildUI(); pnlTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; pnlTimer.Tick += UpdatePnL; pnlTimer.Start(); routedOverlayTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) }; routedOverlayTimer.Tick += UpdateRoutedOverlayFast; routedOverlayTimer.Start(); }
		public void Cleanup() { if (pnlTimer != null) pnlTimer.Stop(); if (routedOverlayTimer != null) routedOverlayTimer.Stop(); RemoveCalculator(); RemoveRoutedOrderOverlay(); }

		private void BuildUI() {
			var G = new System.Windows.Controls.Grid { Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF1B1B1B"), HorizontalAlignment = HorizontalAlignment.Stretch };
			var S = new System.Windows.Controls.ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalContentAlignment = HorizontalAlignment.Stretch };
			var P = new System.Windows.Controls.StackPanel { Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch };
			S.Content = P; G.Children.Add(S);
			System.Windows.Controls.Button CB(string t, System.Windows.Media.Brush b, RoutedEventHandler h = null) {
				var text = new System.Windows.Controls.TextBlock {
					Text = t,
					TextAlignment = TextAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
				var btn = new System.Windows.Controls.Button {
					Content = text,
					Background = b, Foreground = System.Windows.Media.Brushes.GhostWhite,
					FontWeight = FontWeights.Normal, Margin = new Thickness(1), Padding = new Thickness(0, 5, 0, 5),
					BorderThickness = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Stretch,
					HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center
				};
				if (h != null) btn.Click += h; return btn;
			}
			System.Windows.Controls.Border CS(string t, UIElement c) {
				var b = new System.Windows.Controls.Border { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Stretch };
				var sp = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; sp.Children.Add(new System.Windows.Controls.TextBlock { Text = t, Foreground = System.Windows.Media.Brushes.Gray, FontSize = 10, Margin = new Thickness(5, 2, 0, 2) });
				sp.Children.Add(c); b.Child = sp; return b;
			}
			System.Windows.Controls.Primitives.UniformGrid MG(int c) { return new System.Windows.Controls.Primitives.UniformGrid { Columns = c, Rows = 1, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 1, 0, 1) }; }
			System.Windows.Media.Brush red = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFCC4444"), green = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF44CC44"), gray = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF444444"), dark = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF2A2A2A"), amber = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFCC9944"), blue = System.Windows.Media.Brushes.SteelBlue;
			P.Children.Add(new System.Windows.Controls.TextBlock { Text = "Orca Risk Manager", Foreground = System.Windows.Media.Brushes.GhostWhite, FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 10)});
			var qS = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; var rC = MG(2); rC.Children.Add(CB("Calc On", gray, (s, e) => SpawnCalculator())); rC.Children.Add(CB("Calc Off", gray, (s, e) => RemoveCalculator())); qS.Children.Add(rC);
			var rD = MG(2); btnLong = CB("Long", green, (s, e) => { isLongSelected = true; UpdateDirectionButtons(); MirrorCalculatorLines(); }); btnShort = CB("Short", dark, (s, e) => { isLongSelected = false; UpdateDirectionButtons(); MirrorCalculatorLines(); }); rD.Children.Add(btnLong); rD.Children.Add(btnShort); qS.Children.Add(rD);
			var rM = MG(3); btnMarket = CB("Market", amber, (s, e) => { selectedOrderType = OrderType.Market; UpdateOrderModeButtons(); }); btnLimit = CB("Limit", dark, (s, e) => { selectedOrderType = OrderType.Limit; UpdateOrderModeButtons(); }); btnStop = CB("Stop", dark, (s, e) => { selectedOrderType = OrderType.StopMarket; UpdateOrderModeButtons(); }); rM.Children.Add(btnMarket); rM.Children.Add(btnLimit); rM.Children.Add(btnStop); qS.Children.Add(rM);
			var rE = MG(2); btnOpen = CB("Open", blue, (s, e) => ExecuteTrade(selectedOrderType)); btnClose = CB("Close", dark, (s, e) => ClosePosition(100)); rE.Children.Add(btnOpen); rE.Children.Add(btnClose); qS.Children.Add(rE);
			P.Children.Add(CS("\u26A1 Quick Actions", qS));
			var sP = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; var rSM = MG(2); btnFixedDollar = CB("Fixed $", amber, (s, e) => { isFixedDollar = true; UpdateSizeModeButtons(); }); btnFixedSize = CB("Fixed Size", dark, (s, e) => { isFixedDollar = false; UpdateSizeModeButtons(); }); rSM.Children.Add(btnFixedDollar); rSM.Children.Add(btnFixedSize); sP.Children.Add(rSM);
			System.Windows.Input.KeyEventHandler f = (s, e) => { var tb = s as System.Windows.Controls.TextBox; if (tb == null) return; bool iD = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9), iC = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab || e.Key == Key.Enter, iP = e.Key == Key.Decimal || e.Key == Key.OemPeriod; if (iD || iC || iP) { if (e.Key==Key.Enter) Keyboard.ClearFocus(); else if (!iC) { e.Handled=true; string inp = iD? (e.Key>=Key.D0?(e.Key-Key.D0).ToString():(e.Key-Key.NumPad0).ToString()) : "."; int st=tb.SelectionStart; if (tb.SelectionLength>0) tb.Text=tb.Text.Remove(st,tb.SelectionLength); tb.Text=tb.Text.Insert(st,inp); tb.SelectionStart=st+1; } } else e.Handled=true; };
			var at2 = MG(2); at2.Children.Add(new System.Windows.Controls.TextBlock{Text="Risk $", Foreground=System.Windows.Media.Brushes.Gray, VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center}); 
			txtRisk = new System.Windows.Controls.TextBox{Text="500", Margin=new Thickness(1), TextAlignment=TextAlignment.Center}; txtRisk.PreviewKeyDown += f; 
			at2.Children.Add(txtRisk); sP.Children.Add(at2);
			var at3 = MG(2); at3.Children.Add(new System.Windows.Controls.TextBlock{Text="Risk Pts", Foreground=System.Windows.Media.Brushes.Gray, VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center}); 
			txtPoints = new System.Windows.Controls.TextBox{Text="0", Margin=new Thickness(1), TextAlignment=TextAlignment.Center}; txtPoints.PreviewKeyDown += f; 
			txtPoints.MouseWheel += (s, ev) => { if (double.TryParse(txtPoints.Text, out double p) && hEntry != null && hStop != null) { double tk = (GetActiveInstrument()?.MasterInstrument.TickSize ?? 0.25); double nP = Math.Max(tk, p + (ev.Delta > 0 ? tk : -tk)); txtPoints.Text = nP.ToString("F2"); double eP = hEntry.StartAnchor.Price; hStop.StartAnchor.Price = hStop.EndAnchor.Price = isLongSelected ? eP - nP : eP + nP; UpdatePnL(null, null); } };
			at3.Children.Add(txtPoints); sP.Children.Add(at3);
			var at1 = MG(2); at1.Children.Add(new System.Windows.Controls.TextBlock{Text="Contracts", Foreground=System.Windows.Media.Brushes.Gray, VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center}); 
			txtContracts = new System.Windows.Controls.TextBox{Text="1", Margin=new Thickness(1), TextAlignment=TextAlignment.Center}; txtContracts.PreviewKeyDown += f; 
			txtContracts.MouseWheel += (s, ev) => { if (int.TryParse(txtContracts.Text, out int q)) { txtContracts.Text = Math.Max(1, q + (ev.Delta > 0 ? 1 : -1)).ToString(); UpdatePnL(null, null); } };
			at1.Children.Add(txtContracts); sP.Children.Add(at1);
			var rSA = MG(3); rSA.Children.Add(CB("-1", dark, (s, e) => AdjustContractSize(-1))); rSA.Children.Add(CB("+1", dark, (s, e) => AdjustContractSize(1))); rSA.Children.Add(CB("Reset", dark, (s, e) => { txtContracts.Text="1"; })); sP.Children.Add(rSA);
			P.Children.Add(CS("\u2795 Position Sizing", sP));
			var fs = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; var f1 = MG(2); btnBuyMkt = CB("Buy Mkt", green, (s, e) => ExecuteFastCommand("BuyMkt")); btnSellMkt = CB("Sell Mkt", red, (s, e) => ExecuteFastCommand("SellMkt")); f1.Children.Add(btnBuyMkt); f1.Children.Add(btnSellMkt); fs.Children.Add(f1);
			var f2 = MG(2); btnBuyAsk = CB("Buy Ask", dark, (s, e) => ExecuteFastCommand("BuyAsk")); btnSellBid = CB("Sell Bid", dark, (s, e) => ExecuteFastCommand("SellBid")); f2.Children.Add(btnBuyAsk); f2.Children.Add(btnSellBid); fs.Children.Add(f2);
			var f3 = MG(2); btnBuyLmt = CB("Buy Limit", System.Windows.Media.Brushes.LimeGreen, (s, e) => StartDragOrder("BuyLimit")); btnSellLmt = CB("Sell Limit", System.Windows.Media.Brushes.Salmon, (s, e) => StartDragOrder("SellLimit")); f3.Children.Add(btnBuyLmt); f3.Children.Add(btnSellLmt); fs.Children.Add(f3);
			var f4 = MG(2); btnBuyStop = CB("Buy Stop", System.Windows.Media.Brushes.LimeGreen, (s, e) => StartDragOrder("BuyStop")); btnSellStop = CB("Sell Stop", System.Windows.Media.Brushes.Salmon, (s, e) => StartDragOrder("SellStop")); f4.Children.Add(btnBuyStop); f4.Children.Add(btnSellStop); fs.Children.Add(f4);
			btnBreakeven = CB("Move To Breakeven", System.Windows.Media.Brushes.DodgerBlue, (s, e) => MoveToBreakeven()); fs.Children.Add(btnBreakeven);
			P.Children.Add(CS("\u26A1 Fast Execution", fs));
			var cl = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; var rPct = MG(3); var btn25 = CB("25%", dark, (s, e) => ClosePosition(25)); var btn50 = CB("50%", dark, (s, e) => ClosePosition(50)); var btn75 = CB("75%", dark, (s, e) => ClosePosition(75)); rPct.Children.Add(btn25); rPct.Children.Add(btn50); rPct.Children.Add(btn75); cl.Children.Add(rPct); btnCloseAll = CB("Flatten", (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#80DC143C"), (s, e) => Flatten()); cl.Children.Add(btnCloseAll); P.Children.Add(CS("\u2796 Close Position", cl));
			var mn = new System.Windows.Controls.StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch }; txtPnL = new System.Windows.Controls.TextBlock { Text = "$0.00", Foreground = System.Windows.Media.Brushes.LightGray, FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center }; mn.Children.Add(txtPnL);
			var mR = MG(2); mR.Margin = new Thickness(0, 8, 0, 5); txtUnrealR = new System.Windows.Controls.TextBlock{Text="Unrealized: 0.0R", FontSize=11, HorizontalAlignment=HorizontalAlignment.Center}; txtRealR = new System.Windows.Controls.TextBlock{Text="Realized: 0.0R", FontSize=11, HorizontalAlignment=HorizontalAlignment.Center}; mR.Children.Add(txtUnrealR); mR.Children.Add(txtRealR); mn.Children.Add(mR); P.Children.Add(CS("\u2699 Manage Position", mn));
			this.Content = G;
		}

		private void UpdateDirectionButtons() { var g = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF44CC44"); var r = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFCC4444"); var d = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF2A2A2A"); btnLong.Background = isLongSelected? g: d; btnShort.Background = !isLongSelected? r: d; UpdatePnL(null, null); }
		private void UpdateOrderModeButtons() { var a = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFCC9944"); var d = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF2A2A2A"); btnMarket.Background = selectedOrderType == OrderType.Market? a: d; btnLimit.Background = selectedOrderType == OrderType.Limit? a: d; btnStop.Background = selectedOrderType == OrderType.StopMarket? a: d; UpdatePnL(null, null); }
		private void UpdateSizeModeButtons() { var a = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFCC9944"); var d = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF2A2A2A"); btnFixedDollar.Background = isFixedDollar? a: d; btnFixedSize.Background = !isFixedDollar? a: d; }
		private void MirrorCalculatorLines() { if (hEntry == null) return; double e = hEntry.StartAnchor.Price, sD = Math.Abs(e - hStop.StartAnchor.Price), tD = Math.Abs(e - hTarget.StartAnchor.Price); if (isLongSelected) { hStop.StartAnchor.Price = hStop.EndAnchor.Price = e - sD; hTarget.StartAnchor.Price = hTarget.EndAnchor.Price = e + tD; } else { hStop.StartAnchor.Price = hStop.EndAnchor.Price = e + sD; hTarget.StartAnchor.Price = hTarget.EndAnchor.Price = e - tD; } attachedTab.ChartControl.InvalidateVisual(); }
		private void AdjustContractSize(int a) { if (int.TryParse(txtContracts.Text, out int c)) txtContracts.Text = Math.Max(1, c + a).ToString(); else txtContracts.Text = "1"; }
		private Account GetActiveAccount() { Chart cw = Window.GetWindow(attachedTab) as Chart; if (cw?.ChartTrader != null) return cw.ChartTrader.Account; return Account.All.FirstOrDefault(a => a.Name == "Sim101"); }
		private Instrument GetChartInstrument() { return attachedTab?.ChartControl?.Instrument ?? (Window.GetWindow(attachedTab) as Chart)?.ChartTrader?.Instrument; }
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
				Position pos = acc.Positions.FirstOrDefault(p => IsSameInstrument(p.Instrument, ins));
				if (pos != null && pos.MarketPosition != MarketPosition.Flat) {
					double pnl = pos.GetUnrealizedProfitLoss(PerformanceUnit.Currency, GetActivePrice()); txtPnL.Text = pnl.ToString("C2"); txtPnL.Foreground = pnl >= 0? System.Windows.Media.Brushes.LightGreen: System.Windows.Media.Brushes.Salmon;
					double risk = 500; if (double.TryParse(txtRisk.Text, out double r)) risk = r; double uR = risk > 0? pnl / risk: 0; txtUnrealR.Text = $"Unrealized: {uR:N1}R"; txtUnrealR.Foreground = uR >= 0? System.Windows.Media.Brushes.LimeGreen: System.Windows.Media.Brushes.Salmon;
					double rR = risk > 0? currentTradeRealizedPnL / risk: 0; txtRealR.Text = $"Realized: {rR:N1}R";
					SyncProtectionOrdersOnPositionChange(acc, ins, Math.Abs(pos.Quantity));
				} else { txtPnL.Text = "$0.00"; txtPnL.Foreground = System.Windows.Media.Brushes.LightGray; txtUnrealR.Text = "Unrealized: 0.0R"; txtRealR.Text = $"Realized: 0.0R"; SyncProtectionOrdersOnPositionChange(acc, ins, 0); }
				UpdateRoutedOrderOverlay(acc, ins);
				if (isCalculatorActive && hEntry != null && hStop != null && hEntry.StartAnchor != null && hStop.StartAnchor != null) {
					double ent = hEntry.StartAnchor.Price, stp = hStop.StartAnchor.Price, tar = (hTarget != null && hTarget.StartAnchor != null) ? hTarget.StartAnchor.Price : 0;
					if (selectedOrderType == OrderType.Market) { double cur = GetActivePrice(); if (Math.Abs(ent-cur)>0.000001) { hEntry.StartAnchor.Price=hEntry.EndAnchor.Price=cur; ent=cur; } }
					double dist = Math.Abs(ent-stp), tick = ins.MasterInstrument.TickSize, val = ins.MasterInstrument.PointValue;
					if (!txtPoints.IsFocused) txtPoints.Text = dist.ToString("F2");
					if (isFixedDollar) { if (dist > 0 && double.TryParse(txtRisk.Text, out double rd)) { int q = (int)Math.Max(1, Math.Floor(rd / (dist / tick * val * tick))); if (!txtContracts.IsFocused) txtContracts.Text = q.ToString(); } }
					else { int fq = 1; int.TryParse(txtContracts.Text, out fq); if (dist > 0) { double cr = dist / tick * val * tick * fq; if (!txtRisk.IsFocused) txtRisk.Text = cr.ToString("N0"); } }
					int cQ = 1; int.TryParse(txtContracts.Text, out cQ); double rAmt = Math.Abs(ent-stp)/tick*val*tick*cQ, pAmt = Math.Abs(tar-ent)/tick*val*tick*cQ; double riskDollar = 500; double.TryParse(txtRisk.Text, out riskDollar); double rR = riskDollar > 0 ? rAmt / riskDollar : 0, pR = rAmt > 0 ? pAmt / rAmt : 0;
					if (cEntryTxt!=null) cEntryTxt.Text = $"{(isLongSelected?"BUY":"SELL")} {cQ} @ {ent:F2}"; if (cStopTxt!=null) cStopTxt.Text = $"RISK: ${rAmt:N0} | {Math.Abs(ent-stp):F2}pts | {rR:F1}R"; if (cTargetTxt!=null) cTargetTxt.Text = $"PROFIT: ${pAmt:N0} | {Math.Abs(tar-ent):F2}pts | {pR:F1}R";
					attachedTab.ChartControl.InvalidateVisual();
				}
			} catch { }
		}
		private void OnRenderFrame(object s, EventArgs e) { 
			if (!isCalculatorActive) return; 
			try {
				if (hEntry == null || hStop == null || hTarget == null || hEntry.StartAnchor == null || hStop.StartAnchor == null || hTarget.StartAnchor == null) return;
				void SetP(System.Windows.Controls.Border b, double p) { double y = GetYByPrice(p); if (y > 0) { System.Windows.Controls.Canvas.SetTop(b, y - 12); b.Visibility = Visibility.Visible; } else b.Visibility = Visibility.Collapsed; } 
				if (cEntryPill != null) SetP(cEntryPill, hEntry.StartAnchor.Price); 
				if (cStopPill != null) SetP(cStopPill, hStop.StartAnchor.Price); 
				if (cTargetPill != null) SetP(cTargetPill, hTarget.StartAnchor.Price); 
			} catch { }
		}
		private void ExecuteTrade(OrderType t) { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc == null || ins == null) return; int q = 1; int.TryParse(txtContracts.Text, out q); OrderAction act = isLongSelected ? OrderAction.Buy : OrderAction.SellShort; double ent = hEntry?.StartAnchor.Price ?? 0, stp = hStop?.StartAnchor.Price ?? 0, tar = hTarget?.StartAnchor.Price ?? 0; if (stp != 0) { pendingStopPrice=stp; pendingTargetPrice=tar; } string id = "Orca_" + Guid.NewGuid().ToString("N"); if (t == OrderType.Market) acc.Submit(new[] { acc.CreateOrder(ins, act, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, q, 0, 0, "", id, DateTime.MaxValue, null) }); else acc.Submit(new[] { acc.CreateOrder(ins, act, t, OrderEntry.Manual, TimeInForce.Day, q, t==OrderType.Limit?ent:0, t==OrderType.StopMarket?ent:0, "", id, DateTime.MaxValue, null) }); pendingEntryName = id; } catch { } }
		private void ExecuteFastCommand(string c) { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc == null || ins == null) return; int q = 1; int.TryParse(txtContracts.Text, out q); string id = "Fast_" + Guid.NewGuid().ToString("N"); OrderAction act = c.StartsWith("Sell")? OrderAction.Sell : OrderAction.Buy; if (c.EndsWith("Mkt")) acc.Submit(new[] { acc.CreateOrder(ins, act, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, q, 0, 0, "", id, DateTime.MaxValue, null) }); else acc.Submit(new[] { acc.CreateOrder(ins, act, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, q, GetActivePrice() + (c=="BuyAsk"?ins.MasterInstrument.TickSize:-ins.MasterInstrument.TickSize), 0, "", id, DateTime.MaxValue, null) }); } catch { } }
		private void StartDragOrder(string t) { if (attachedTab?.Content is System.Windows.Controls.Grid r) { if (isDragOrderActive) { bool isSame = dragOrderType == t; CancelDragOrder(); if (isSame) return; } isDragOrderActive = true; dragOrderType = t; dragCanvas = new System.Windows.Controls.Canvas { Background = System.Windows.Media.Brushes.Transparent, Cursor = Cursors.Cross }; System.Windows.Controls.Panel.SetZIndex(dragCanvas, 9999); System.Windows.Media.Brush br = t.StartsWith("Sell") ? System.Windows.Media.Brushes.Salmon : System.Windows.Media.Brushes.LimeGreen; dragLine = new System.Windows.Shapes.Line { X1 = 0, X2 = attachedTab.ChartControl.ActualWidth, Stroke = br, StrokeThickness = 2 }; dragLabelTxt = new System.Windows.Controls.TextBlock { Foreground = System.Windows.Media.Brushes.Black, FontWeight = FontWeights.SemiBold }; dragLabelPill = new System.Windows.Controls.Border{Background=br, CornerRadius=new CornerRadius(4), Padding=new Thickness(4), Child=dragLabelTxt}; dragCanvas.Children.Add(dragLine); dragCanvas.Children.Add(dragLabelPill); dragCanvas.MouseMove += (s, e) => { if (isDragOrderActive) { Point p = e.GetPosition(attachedTab.ChartControl); double pr = GetPriceByY(p.Y); if (attachedTab?.ChartControl?.Instrument != null) { double tk = attachedTab.ChartControl.Instrument.MasterInstrument.TickSize; pr = Math.Round(pr/tk)*tk; double sY = GetYByPrice(pr); if(sY!=0) p.Y=sY; } dragLine.X2 = attachedTab.ChartControl.ActualWidth; dragLine.Y1 = dragLine.Y2 = p.Y; int q = 1; int.TryParse(txtContracts.Text, out q); string act = dragOrderType.StartsWith("Buy") ? "BUY" : "SELL"; string typ = dragOrderType.EndsWith("Limit") ? "LMT" : "STP"; dragLabelTxt.Text=$"{act} {q} {typ} @ {pr:F2}"; System.Windows.Controls.Canvas.SetRight(dragLabelPill, 65); System.Windows.Controls.Canvas.SetTop(dragLabelPill, p.Y-12); } }; dragCanvas.MouseLeftButtonDown += (s, e) => { PlaceDragOrderAt(GetPriceByY(e.GetPosition(attachedTab.ChartControl).Y)); CancelDragOrder(); }; r.Children.Add(dragCanvas); var w = Window.GetWindow(attachedTab.ChartControl); if(w!=null) w.PreviewKeyDown += Window_PreviewKeyDown_CancelDrag; } }
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
					string label = BuildPositionTradeLabel(pos);
					bool pnlIsPositive;
					string pnlBadge = BuildPositionPnlBadge(executionInstrument, pos, out pnlIsPositive);
					AddRoutedLine(pos.AveragePrice, GetRoutedPositionBrush(pos, visual), label, acc, null, visual, pnlBadge, pnlIsPositive);
					AddRoutedProtectionButtons(acc, executionInstrument, pos, hasLimit, hasStop, activeOco, visual);
				}
				foreach (Order order in workingOrders) {
					double price = GetOrderDisplayPrice(order);
					if (price <= 0) continue;
					System.Windows.Media.Brush brush = GetRoutedOrderBrush(pos, order, price, visual);
					string type = order.OrderType == OrderType.StopMarket ? "STP" : order.OrderType == OrderType.Limit ? "LMT" : order.OrderType.ToString();
					string label = BuildRoutedOrderLabel(executionInstrument, pos, order, price, type);
					AddRoutedLine(price, brush, label, acc, order, visual);
				}
			} catch { }
		}
		private void EnsureRoutedOrderCanvas() {
			if (routedOrderCanvas != null) return;
			if (!(attachedTab?.Content is System.Windows.Controls.Grid grid) || attachedTab.ChartControl == null) return;
			routedOrderCanvas = new System.Windows.Controls.Canvas { IsHitTestVisible = true, ClipToBounds = true };
			System.Windows.Controls.Panel.SetZIndex(routedOrderCanvas, 9996);
			System.Windows.Controls.Grid.SetRow(routedOrderCanvas, System.Windows.Controls.Grid.GetRow(attachedTab.ChartControl));
			System.Windows.Controls.Grid.SetColumn(routedOrderCanvas, System.Windows.Controls.Grid.GetColumn(attachedTab.ChartControl));
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
		private FontFamily GetRouterFont(OrcaExecutionRouterSettings visual) { try { return new FontFamily(string.IsNullOrWhiteSpace(visual.FontFamily) ? "Segoe UI" : visual.FontFamily); } catch { return new FontFamily("Segoe UI"); } }
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
		private string BuildPositionTradeLabel(Position pos) {
			return $"{pos.MarketPosition} {pos.Quantity} @ {pos.AveragePrice:F2}";
		}
		private string BuildPositionPnlBadge(Instrument instrument, Position pos, out bool pnlIsPositive) {
			double current = GetActivePrice();
			double direction = pos.MarketPosition == MarketPosition.Long ? 1 : -1;
			double points = (current - pos.AveragePrice) * direction;
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
			double direction = side == MarketPosition.Long ? 1 : -1;
			double points = (price - entryPrice) * direction;
			return points * instrument.MasterInstrument.PointValue * quantity;
		}
		private bool IsPositionReducingOrder(Position pos, Order order) {
			if (pos == null || order == null || pos.MarketPosition == MarketPosition.Flat) return false;
			if (pos.MarketPosition == MarketPosition.Long) return order.OrderAction == OrderAction.Sell;
			return order.OrderAction == OrderAction.Buy || order.OrderAction == OrderAction.BuyToCover;
		}
		private double ReserveRoutedLabelTop(double desiredTop, double elementHeight, double panelTop, double panelBottom) {
			const double gap = 2;
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
		private void AddRoutedLine(double price, System.Windows.Media.Brush brush, string label, Account acc, Order order, OrcaExecutionRouterSettings visual, string pnlBadgeText = "", bool pnlIsPositive = true) {
			double y, panelTop, panelBottom;
			if (!TryGetYByPriceInPrimaryPanel(price, out y, out panelTop, out panelBottom)) return;
			if (y <= 0 || routedOrderCanvas == null || attachedTab?.ChartControl == null) return;
			double plotRight = GetPlotRightX();
			var line = new System.Windows.Shapes.Line { X1 = 0, X2 = plotRight, Y1 = y, Y2 = y, Stroke = brush, StrokeThickness = visual.LineThickness, Opacity = 0.95, IsHitTestVisible = false };
			bool hasPnlBadge = !string.IsNullOrWhiteSpace(pnlBadgeText);
			var content = BuildRoutedLabelContent(label, brush, visual, pnlBadgeText, pnlIsPositive);
			var pill = new System.Windows.Controls.Border { Background = hasPnlBadge ? System.Windows.Media.Brushes.Transparent : GetRouterBackgroundBrush(brush, visual.LabelBackgroundOpacity), CornerRadius = new CornerRadius(4), Padding = hasPnlBadge ? new Thickness(0) : new Thickness(4, 2, 4, 2), Child = content, IsHitTestVisible = order != null, Cursor = order != null ? Cursors.SizeNS : Cursors.Arrow };
			var closeButton = acc != null ? BuildRoutedCloseButton(acc, order, visual) : null;
			routedOrderCanvas.Children.Add(line);
			if (order != null) {
				var hitLine = new System.Windows.Shapes.Line { X1 = 0, X2 = plotRight, Y1 = y, Y2 = y, Stroke = System.Windows.Media.Brushes.Transparent, StrokeThickness = 10, Cursor = Cursors.SizeNS };
				hitLine.MouseLeftButtonDown += (s, e) => { StartRoutedOrderChangeDrag(acc, order); e.Handled = true; };
				hitLine.ContextMenu = BuildRoutedOrderContextMenu(acc, order);
				routedOrderCanvas.Children.Add(hitLine);
				pill.MouseLeftButtonDown += (s, e) => { StartRoutedOrderChangeDrag(acc, order); e.Handled = true; };
				pill.ContextMenu = BuildRoutedOrderContextMenu(acc, order);
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
			System.Windows.Controls.Canvas.SetLeft(pill, Math.Max(0, (closeButton != null ? closeLeft - closeGap : plotRight - visual.LabelRightPadding) - pill.DesiredSize.Width));
			System.Windows.Controls.Canvas.SetTop(pill, labelTop);
			if (closeButton != null) {
				double buttonTop = slotTop + Math.Max(0, (slotHeight - closeButton.Height) / 2.0);
				System.Windows.Controls.Canvas.SetLeft(closeButton, closeLeft);
				System.Windows.Controls.Canvas.SetTop(closeButton, buttonTop);
			}
		}
		private System.Windows.Controls.Border BuildRoutedCloseButton(Account acc, Order order, OrcaExecutionRouterSettings visual) {
			var text = new System.Windows.Controls.TextBlock { Text = "X", Foreground = System.Windows.Media.Brushes.White, FontFamily = GetRouterFont(visual), FontWeight = FontWeights.Bold, FontSize = Math.Max(9, visual.FontSize - 1), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
			var button = new System.Windows.Controls.Border { Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#CC111111"), BorderBrush = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Width = 16, Height = 16, Child = text, Opacity = 0.95, Cursor = Cursors.Hand, ToolTip = order == null ? "Flatten position and cancel working orders" : "Cancel this order" };
			button.MouseLeftButtonDown += (s, e) => {
				if (order == null) Flatten();
				else CancelRoutedOrder(acc, order);
				e.Handled = true;
			};
			return button;
		}
		private void AddRoutedProtectionButtons(Account acc, Instrument instrument, Position pos, bool hasLimit, bool hasStop, string oco, OrcaExecutionRouterSettings visual) {
			double y, panelTop, panelBottom;
			if (!TryGetYByPriceInPrimaryPanel(pos.AveragePrice, out y, out panelTop, out panelBottom) || routedOrderCanvas == null) return;
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
			routedDragAccount = acc;
			routedDragInstrument = instrument;
			routedDragSide = pos.MarketPosition;
			routedDragEntryPrice = pos.AveragePrice;
			routedDragQuantity = Math.Abs(pos.Quantity);
			routedDragOco = oco;
			routedDragPrice = pos.AveragePrice;
			Mouse.Capture(routedOrderCanvas);
		}
		private void StartRoutedOrderChangeDrag(Account acc, Order order) {
			if (acc == null || order == null) return;
			isDraggingRoutedTP = false;
			isDraggingRoutedSL = false;
			isDraggingRoutedOrder = true;
			routedDragAccount = acc;
			routedDragOrder = order;
			routedDragInstrument = order.Instrument;
			routedDragPrice = GetOrderDisplayPrice(order);
			Position pos = acc.Positions.FirstOrDefault(p => IsSameInstrument(p.Instrument, order.Instrument));
			if (IsPositionReducingOrder(pos, order)) {
				routedDragSide = pos.MarketPosition;
				routedDragEntryPrice = pos.AveragePrice;
				routedDragQuantity = Math.Max(1, order.Quantity);
			} else {
				routedDragSide = MarketPosition.Flat;
				routedDragEntryPrice = 0;
				routedDragQuantity = 0;
			}
			Mouse.Capture(routedOrderCanvas);
			DrawRoutedDragPreview();
		}
		private void RoutedOrderCanvas_MouseMove(object sender, MouseEventArgs e) {
			if (!isDraggingRoutedTP && !isDraggingRoutedSL && !isDraggingRoutedOrder) return;
			Point point = e.GetPosition(attachedTab.ChartControl);
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
			string label = $"{GetOrderDragLabel(routedDragOrder)} @ {routedDragPrice:F2}";
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
				if (routedDragAccount == null || routedDragOrder == null || routedDragPrice <= 0) return;
				if (routedDragOrder.OrderType == OrderType.Limit) SetOrderDouble(routedDragOrder, "LimitPriceChanged", routedDragPrice);
				else if (routedDragOrder.OrderType == OrderType.StopMarket || routedDragOrder.OrderType == OrderType.StopLimit) SetOrderDouble(routedDragOrder, "StopPriceChanged", routedDragPrice);
				else return;
				routedDragAccount.Change(new[] { routedDragOrder });
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
		private void ClearRoutedProtectionDrag() {
			isDraggingRoutedTP = isDraggingRoutedSL = isDraggingRoutedOrder = false;
			routedDragAccount = null; routedDragInstrument = null; routedDragSide = MarketPosition.Flat; routedDragEntryPrice = routedDragPrice = 0; routedDragQuantity = 0; routedDragOco = "";
			routedDragLine = null; routedDragPill = null; routedDragTxt = null; routedDragOrder = null;
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
		private void PlaceDragOrderAt(double p) { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc==null || ins==null) return; int q = 1; int.TryParse(txtContracts.Text, out q); string id = "Drag_" + Guid.NewGuid().ToString("N"); OrderAction act = dragOrderType.Contains("Buy") ? OrderAction.Buy : OrderAction.Sell; OrderType typ = dragOrderType.Contains("Stop") ? OrderType.StopMarket : OrderType.Limit; acc.Submit(new[] { acc.CreateOrder(ins, act, typ, OrderEntry.Manual, TimeInForce.Day, q, typ==OrderType.Limit?p:0, typ==OrderType.StopMarket?p:0, "", id, DateTime.MaxValue, null) }); } catch { } }
		private void CancelDragOrder() { isDragOrderActive = false; if (dragCanvas != null) { (System.Windows.Media.VisualTreeHelper.GetParent(dragCanvas) as System.Windows.Controls.Panel)?.Children.Remove(dragCanvas); dragCanvas = null; } if (attachedTab?.ChartControl != null) { var w = Window.GetWindow(attachedTab.ChartControl); if (w != null) w.PreviewKeyDown -= Window_PreviewKeyDown_CancelDrag; } }
		private void Window_PreviewKeyDown_CancelDrag(object s, KeyEventArgs e) { if (e.Key == Key.Escape) { CancelDragOrder(); e.Handled = true; } }
		private void Flatten() { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc != null && ins != null) { foreach (var o in acc.Orders) if (IsSameInstrument(o.Instrument, ins) && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted)) acc.Cancel(new[] { o }); ClosePosition(100); } } catch { } }
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
		private void OnCalculatorLineMoved(object s, PropertyChangedEventArgs e) { if (e.PropertyName == "StartAnchor" || e.PropertyName == "EndAnchor") UpdatePnL(null, null); }
		private void SpawnCalculator() {
			RemoveCalculator(); try {
				if (attachedTab?.ChartControl == null) return; NinjaScriptBase o = attachedTab.ChartControl.Indicators.FirstOrDefault() as NinjaScriptBase; if (o == null) return; calcOwner = o; double cp = GetActivePrice(); Instrument ins = GetActiveInstrument(); if (ins == null) return; double tk = ins.MasterInstrument.TickSize; int sT = ins.FullName.Contains("ES") ? 20 : 100, tT = sT * 2; double sY = isLongSelected ? cp - (sT * tk) : cp + (sT * tk), tY = isLongSelected ? cp + (tT * tk) : cp - (tT * tk);
				hEntry = Draw.HorizontalLine(o,"OEnt",cp,System.Windows.Media.Brushes.WhiteSmoke,DashStyleHelper.Solid,2); hTarget = Draw.HorizontalLine(o,"OTar",tY,System.Windows.Media.Brushes.LimeGreen,DashStyleHelper.Solid,2); hStop = Draw.HorizontalLine(o,"OStp",sY,System.Windows.Media.Brushes.Salmon,DashStyleHelper.Solid,2);
				calcCanvas = new System.Windows.Controls.Canvas { IsHitTestVisible = false }; System.Windows.Controls.Panel.SetZIndex(calcCanvas, 9998); System.Windows.Controls.Grid.SetRow(calcCanvas, System.Windows.Controls.Grid.GetRow(attachedTab.ChartControl)); System.Windows.Controls.Grid.SetColumn(calcCanvas, System.Windows.Controls.Grid.GetColumn(attachedTab.ChartControl)); (attachedTab.Content as System.Windows.Controls.Grid).Children.Add(calcCanvas);
				void AddP(System.Windows.Media.Brush b, out System.Windows.Controls.TextBlock t, out System.Windows.Controls.Border p) { t = new System.Windows.Controls.TextBlock { Foreground = System.Windows.Media.Brushes.Black, FontWeight = FontWeights.Bold }; p = new System.Windows.Controls.Border { Background = b, CornerRadius = new CornerRadius(4), Padding = new Thickness(4), Child = t }; System.Windows.Controls.Canvas.SetRight(p, 65); calcCanvas.Children.Add(p); }
				AddP(System.Windows.Media.Brushes.WhiteSmoke, out cEntryTxt, out cEntryPill); AddP(System.Windows.Media.Brushes.Salmon, out cStopTxt, out cStopPill); AddP(System.Windows.Media.Brushes.LimeGreen, out cTargetTxt, out cTargetPill);
				if (renderHandler == null) { renderHandler = new EventHandler(OnRenderFrame); System.Windows.Media.CompositionTarget.Rendering += renderHandler; }
				foreach (var l in new[] { hEntry, hStop, hTarget }) if (l != null) { l.IsLocked = l.IsAutoScale = false; if (l is INotifyPropertyChanged i) i.PropertyChanged += OnCalculatorLineMoved; }
				isCalculatorActive = true; UpdatePnL(null, null); attachedTab.ChartControl.InvalidateVisual();
			} catch { }
		}
		private void RemoveCalculator() { try { isCalculatorActive = false; if (calcCanvas != null) { (attachedTab?.Content as System.Windows.Controls.Grid)?.Children.Remove(calcCanvas); calcCanvas = null; } if (calcOwner != null) { Draw.HorizontalLine(calcOwner,"OEnt",0,System.Windows.Media.Brushes.Black,DashStyleHelper.Solid,1); Draw.HorizontalLine(calcOwner,"OTar",0,System.Windows.Media.Brushes.Black,DashStyleHelper.Solid,1); Draw.HorizontalLine(calcOwner,"OStp",0,System.Windows.Media.Brushes.Black,DashStyleHelper.Solid,1); } foreach (var l in new[] { hEntry, hStop, hTarget }) if (l != null) l.StartAnchor.Price = l.EndAnchor.Price = 0; if (renderHandler != null) { System.Windows.Media.CompositionTarget.Rendering -= renderHandler; renderHandler = null; } calcOwner = null; attachedTab?.ChartControl?.InvalidateVisual(); } catch { } }
	}
}
