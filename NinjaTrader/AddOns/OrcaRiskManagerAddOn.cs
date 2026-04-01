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

			// === TRIPLE INJECTION STRATEGY ===
			// Strategy 1: Immediate inject (works if chart is already loaded, e.g. after recompile)
			chartWindow.Dispatcher.InvokeAsync(() => {
				if (chartWindow.IsLoaded)
					InsertChartTraderControl(chartWindow);
			});

			// Strategy 2: Loaded event (works for newly opening charts)
			chartWindow.Loaded += (s, e) => 
			{
				chartWindow.Dispatcher.InvokeAsync(() => {
					InsertChartTraderControl(chartWindow);
				});
			};

			// Strategy 3: Delayed retry (2 second fallback for edge cases)
			var retryTimer = new System.Windows.Threading.DispatcherTimer();
			retryTimer.Interval = TimeSpan.FromSeconds(2);
			retryTimer.Tick += (s, e) => {
				retryTimer.Stop();
				InsertChartTraderControl(chartWindow);
			};
			retryTimer.Start();

			// Register Ctrl+C to toggle the Orca panel
			chartWindow.PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
				{
					e.Handled = true;
					TogglePanelVisibility(chartWindow);
				}
			};

			// Hook tab changes
			chartWindow.Dispatcher.InvokeAsync(() => 
			{
				if (chartWindow.MainTabControl != null)
				{
					chartWindow.MainTabControl.SelectionChanged += (s, e) => 
					{
						InsertChartTraderControl(chartWindow);
					};
				}
			});
		}

		private void TogglePanelVisibility(Chart chartWindow)
		{
			if (chartWindow.MainTabControl == null || chartWindow.MainTabControl.Items.Count == 0) return;
			foreach (object item in chartWindow.MainTabControl.Items)
			{
				ChartTab tab = item as ChartTab;
				if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
				if (tab == null) continue;
				if (tab.Content is Grid tabGrid)
				{
					bool foundPanel = false;
					// Use GetType().Name instead of 'is' to work across recompiles
					foreach (UIElement el in tabGrid.Children)
					{
						if (el.GetType().Name == "OrcaRiskPanel")
						{
							foundPanel = true;
							int col = Grid.GetColumn(el);
							if (col >= 0 && col < tabGrid.ColumnDefinitions.Count)
							{
								var colDef = tabGrid.ColumnDefinitions[col];
								if (colDef.Width.Value > 0)
								{
									colDef.Width = new GridLength(0);
									el.Visibility = Visibility.Collapsed;
								}
								else
								{
									colDef.Width = new GridLength(240);
									el.Visibility = Visibility.Visible;
								}
							}
						}
					}
					if (!foundPanel)
					{
						InsertChartTraderControl(chartWindow);
					}
				}
			}
		}

		private void InsertChartTraderControl(Chart chartWindow)
		{
			try {
				if (chartWindow.MainTabControl == null || chartWindow.MainTabControl.Items.Count == 0) return;

				foreach (object item in chartWindow.MainTabControl.Items)
				{
					ChartTab tab = item as ChartTab;
					if (tab == null && item is TabItem tabItem)
					{
						tab = tabItem.Content as ChartTab;
					}
					if (tab == null) continue;

					if (tab.Content is Grid tabGrid)
					{
						// STEP 1: Clean up ANY stale panels from previous compilations
						// After recompile, old panels are a different .NET type but same name
						var staleToRemove = new List<UIElement>();
						int staleCols = 0;
						foreach(UIElement el in tabGrid.Children)
						{
							if (el.GetType().Name == "OrcaRiskPanel")
							{
								if (el is OrcaRiskPanel currentPanel)
								{
									// This is our current-version panel — it's fine, skip this tab
									staleToRemove.Clear();
									staleCols = -1; // signal to skip
									break;
								}
								else
								{
									// Stale panel from old recompile — mark for removal
									staleToRemove.Add(el);
									staleCols++;
								}
							}
						}
						if (staleCols == -1) continue; // current panel exists, skip

						// Remove stale panels and their columns
						foreach(UIElement el in staleToRemove)
						{
							try { el.GetType().GetMethod("Cleanup")?.Invoke(el, null); } catch { }
							tabGrid.Children.Remove(el);
						}
						for (int i = 0; i < staleCols; i++)
						{
							if (tabGrid.ColumnDefinitions.Count > 0)
								tabGrid.ColumnDefinitions.RemoveAt(tabGrid.ColumnDefinitions.Count - 1);
						}

						// STEP 2: Insert the new panel
						OrcaRiskPanel orcaPanel = new OrcaRiskPanel(tab);

						if (tabGrid.ColumnDefinitions.Count == 0)
						{
							tabGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
						}

						tabGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(240) });
						
						Grid.SetColumn(orcaPanel, tabGrid.ColumnDefinitions.Count - 1);
						tabGrid.Children.Add(orcaPanel);
					}
				}
			} catch { }
		}
	}

	public class OrcaRiskPanel : UserControl
	{
		private ChartTab attachedTab;
		private System.Windows.Threading.DispatcherTimer pnlTimer;
		
		private Button btnLong;
		private Button btnShort;
		private Button btnMarket;
		private Button btnLimit;
		private Button btnStop;
		private Button btnOpen;
		private Button btnClose;
		private Button btnBuyMkt;
		private Button btnSellMkt;
		private Button btnBuyAsk;
		private Button btnSellBid;
		private Button btnBreakeven;
		private Button btnCloseAll;
		private Button btnFixedDollar;
		private Button btnFixedSize;
		private TextBox txtContracts;
		private TextBox txtRisk;
		private TextBlock txtPnL;
		
		private Button btnBuyLmt;
		private Button btnSellLmt;
		private Button btnBuyStop;
		private Button btnSellStop;

		// Drag-order state
		private bool isDragOrderActive = false;
		private string dragOrderType = null;
		private System.Windows.Shapes.Line dragLine = null;
		private Canvas dragCanvas = null;
		private Border dragLabelPill = null;
		private TextBlock dragLabelTxt = null;
		
		private bool isLongSelected = true;
		private bool isFixedDollar = true;
		private OrderType selectedOrderType = OrderType.Market;

		private string pendingEntryName = null;
		private double pendingStopPrice = 0;
		private double pendingTargetPrice = 0;
		private int pendingContracts = 0;
		private Account hookedAccount = null;
		
		private TextBlock txtUnrealR;
		private TextBlock txtRealR;
		private double baselineRealizedPnL = 0;
		private double currentTradeRealizedPnL = 0;

		private static double totalSessionR = 0;

		private bool isCalculatorActive = false;
		private NinjaScriptBase calcOwner = null;
		private NinjaTrader.NinjaScript.DrawingTools.HorizontalLine hEntry, hStop, hTarget;
		
		private Canvas calcCanvas;
		private Border cEntryPill, cStopPill, cTargetPill;
		private TextBlock cEntryTxt, cStopTxt, cTargetTxt;
		private EventHandler renderHandler;

		public OrcaRiskPanel(ChartTab tab)
		{
			attachedTab = tab;
			BuildUI();
			
			pnlTimer = new System.Windows.Threading.DispatcherTimer();
			pnlTimer.Interval = TimeSpan.FromSeconds(1);
			pnlTimer.Tick += UpdatePnL;
			pnlTimer.Start();
		}

		public void Cleanup()
		{
			if (pnlTimer != null) pnlTimer.Stop();
			RemoveCalculator();
		}

		private void BuildUI()
		{
			Grid MainGrid = new Grid
			{
				Background = (Brush)new BrushConverter().ConvertFrom("#FF1B1B1B"),
				Width = 240,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Stretch,
				Margin = new Thickness(0, 0, 0, 0)
			};
			
			ScrollViewer scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
			StackPanel mainPanel = new StackPanel { Margin = new Thickness(5) };
			scrollViewer.Content = mainPanel;
			MainGrid.Children.Add(scrollViewer);

			Button CreateBtn(string text, Brush bg, RoutedEventHandler onClick = null) {
				var gridContent = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
				gridContent.Children.Add(new TextBlock { Text = text, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center });
				var b = new Button { 
					Content = gridContent, 
					Background = bg, Foreground = Brushes.White, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(2), Padding = new Thickness(5), BorderThickness = new Thickness(0) 
				};
				if (onClick != null) b.Click += onClick;
				return b;
			}
			Border CreateSection(string title, UIElement content) {
				var b = new Border { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(0, 5, 0, 5) };
				var sp = new StackPanel();
				sp.Children.Add(new TextBlock { Text = title, Foreground = Brushes.Gray, FontSize = 10, Margin = new Thickness(5, 2, 0, 2) });
				sp.Children.Add(content);
				b.Child = sp;
				return b;
			}
			Grid MakeGrid(int cols) {
				var g = new Grid();
				for(int i=0; i<cols; i++) g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				return g;
			}
			void AddToGrid(Grid g, UIElement e, int col) { Grid.SetColumn(e, col); g.Children.Add(e); }

			Brush redBrush = (Brush)new BrushConverter().ConvertFrom("#FFCC4444");
			Brush greenBrush = (Brush)new BrushConverter().ConvertFrom("#FF44CC44");
			Brush grayBrush = (Brush)new BrushConverter().ConvertFrom("#FF444444");
			Brush darkGray = (Brush)new BrushConverter().ConvertFrom("#FF2A2A2A");
			Brush amberBrush = (Brush)new BrushConverter().ConvertFrom("#FFCC9944");
			Brush steelBlue = Brushes.SteelBlue;

			mainPanel.Children.Add(new TextBlock { Text = "Orca Risk Manager", Foreground = Brushes.White, FontFamily = new FontFamily("Arial"), FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 10)});

			// --- Quick Actions ---
			StackPanel quickActions = new StackPanel();
			
			var rowCalc = MakeGrid(2);
			AddToGrid(rowCalc, CreateBtn("Calc On", grayBrush, (s, e) => SpawnCalculator()), 0);
			AddToGrid(rowCalc, CreateBtn("Calc Off", grayBrush, (s, e) => RemoveCalculator()), 1);
			quickActions.Children.Add(rowCalc);

			var rowDir = MakeGrid(2);
			btnLong = CreateBtn("Long", greenBrush, (s, e) => { isLongSelected = true; UpdateDirectionButtons(); MirrorCalculatorLines(); });
			btnShort = CreateBtn("Short", darkGray, (s, e) => { isLongSelected = false; UpdateDirectionButtons(); MirrorCalculatorLines(); });
			AddToGrid(rowDir, btnLong, 0); AddToGrid(rowDir, btnShort, 1);
			quickActions.Children.Add(rowDir);

			var rowModes = MakeGrid(3);
			btnMarket = CreateBtn("Market", amberBrush, (s, e) => { selectedOrderType = OrderType.Market; UpdateOrderModeButtons(); });
			btnLimit = CreateBtn("Limit", darkGray, (s, e) => { selectedOrderType = OrderType.Limit; UpdateOrderModeButtons(); });
			btnStop = CreateBtn("Stop", darkGray, (s, e) => { selectedOrderType = OrderType.StopMarket; UpdateOrderModeButtons(); });
			AddToGrid(rowModes, btnMarket, 0); AddToGrid(rowModes, btnLimit, 1); AddToGrid(rowModes, btnStop, 2);
			quickActions.Children.Add(rowModes);

			var rowExecution = MakeGrid(2);
			btnOpen = CreateBtn("Open", steelBlue, (s, e) => ExecuteTrade(selectedOrderType));
			btnClose = CreateBtn("Close", darkGray, (s, e) => ClosePosition(100)); 
			AddToGrid(rowExecution, btnOpen, 0); AddToGrid(rowExecution, btnClose, 1);
			quickActions.Children.Add(rowExecution);
			
			mainPanel.Children.Add(CreateSection("⚡ Quick Actions", quickActions));

			// --- Position Sizing ---
			StackPanel addPos = new StackPanel();
			
			var rowSizeMode = MakeGrid(2);
			btnFixedDollar = CreateBtn("Fixed $", amberBrush, (s, e) => { isFixedDollar = true; UpdateSizeModeButtons(); });
			btnFixedSize = CreateBtn("Fixed Size", darkGray, (s, e) => { isFixedDollar = false; UpdateSizeModeButtons(); });
			AddToGrid(rowSizeMode, btnFixedDollar, 0); AddToGrid(rowSizeMode, btnFixedSize, 1);
			addPos.Children.Add(rowSizeMode);
			
			System.Windows.Input.KeyEventHandler filterKeys = (sender, e) => 
			{
				TextBox tb = sender as TextBox;
				if (tb == null) return;
				
				bool isDigit = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
				bool isControl = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Tab || e.Key == Key.Enter;
				bool isPeriod = e.Key == Key.Decimal || e.Key == Key.OemPeriod;

				if (isDigit || isControl || isPeriod) 
				{
					if (e.Key == Key.Enter) { Keyboard.ClearFocus(); e.Handled = true; }
					else if (!isControl) {
						e.Handled = true;
						string input = "";
						if (isDigit) {
							if (e.Key >= Key.D0 && e.Key <= Key.D9) input = (e.Key - Key.D0).ToString();
							else input = (e.Key - Key.NumPad0).ToString();
						} else if (isPeriod) input = ".";
						
						if (input != "") {
							int start = tb.SelectionStart;
							if (tb.SelectionLength > 0) tb.Text = tb.Text.Remove(start, tb.SelectionLength);
							tb.Text = tb.Text.Insert(start, input);
							tb.SelectionStart = start + 1;
						}
					}
				} else {
					e.Handled = true;
				}
			};

			var atp1 = MakeGrid(2);
			AddToGrid(atp1, new TextBlock{Text="Contracts", Foreground=Brushes.Gray, VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center}, 0);
			txtContracts = new TextBox{Text="1", Background=darkGray, Foreground=Brushes.White, TextAlignment=TextAlignment.Center, Margin=new Thickness(2), BorderThickness=new Thickness(0)};
			txtContracts.PreviewKeyDown += filterKeys;
			AddToGrid(atp1, txtContracts, 1);
			addPos.Children.Add(atp1);

			var atp2 = MakeGrid(2);
			AddToGrid(atp2, new TextBlock{Text="Risk $", Foreground=Brushes.Gray, VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center}, 0);
			txtRisk = new TextBox{Text="500", Background=darkGray, Foreground=Brushes.White, TextAlignment=TextAlignment.Center, Margin=new Thickness(2), BorderThickness=new Thickness(0)};
			txtRisk.PreviewKeyDown += filterKeys;
			AddToGrid(atp2, txtRisk, 1);
			addPos.Children.Add(atp2);

			var rowSizeAdj = MakeGrid(3);
			AddToGrid(rowSizeAdj, CreateBtn("-1", darkGray, (s, e) => AdjustContractSize(-1)), 0); 
			AddToGrid(rowSizeAdj, CreateBtn("+1", darkGray, (s, e) => AdjustContractSize(1)), 1); 
			AddToGrid(rowSizeAdj, CreateBtn("Reset", darkGray, (s, e) => txtContracts.Text = "1"), 2);
			addPos.Children.Add(rowSizeAdj);

			mainPanel.Children.Add(CreateSection("➕ Position Sizing", addPos));

			// --- Fast Execution ---
			StackPanel fastExec = new StackPanel();
			var fe1 = MakeGrid(2);
			btnBuyMkt = CreateBtn("Buy Mkt", greenBrush, (s, e) => ExecuteFastCommand("BuyMkt"));
			btnSellMkt = CreateBtn("Sell Mkt", redBrush, (s, e) => ExecuteFastCommand("SellMkt"));
			AddToGrid(fe1, btnBuyMkt, 0); AddToGrid(fe1, btnSellMkt, 1);
			var fe2 = MakeGrid(2);
			btnBuyAsk = CreateBtn("Buy Ask", darkGray, (s, e) => ExecuteFastCommand("BuyAsk"));
			btnSellBid = CreateBtn("Sell Bid", darkGray, (s, e) => ExecuteFastCommand("SellBid"));
			AddToGrid(fe2, btnBuyAsk, 0); AddToGrid(fe2, btnSellBid, 1);
			
			var fe3 = MakeGrid(2);
			Brush tealBrush = (Brush)new BrushConverter().ConvertFrom("#FF008080");
			btnBuyLmt = CreateBtn("Buy Limit", tealBrush, (s, e) => StartDragOrder("BuyLimit"));
			btnSellLmt = CreateBtn("Sell Limit", tealBrush, (s, e) => StartDragOrder("SellLimit"));
			AddToGrid(fe3, btnBuyLmt, 0); AddToGrid(fe3, btnSellLmt, 1);

			var fe4 = MakeGrid(2);
			btnBuyStop = CreateBtn("Buy Stop", amberBrush, (s, e) => StartDragOrder("BuyStop"));
			btnSellStop = CreateBtn("Sell Stop", amberBrush, (s, e) => StartDragOrder("SellStop"));
			AddToGrid(fe4, btnBuyStop, 0); AddToGrid(fe4, btnSellStop, 1);

			Brush dodgerBlue = (Brush)new BrushConverter().ConvertFrom("#FF1E90FF");
			btnBreakeven = CreateBtn("Move To Breakeven", dodgerBlue, (s, e) => MoveToBreakeven());
			fastExec.Children.Add(fe1); fastExec.Children.Add(fe2); fastExec.Children.Add(fe3); fastExec.Children.Add(fe4); fastExec.Children.Add(btnBreakeven);
			mainPanel.Children.Add(CreateSection("⚡ Fast Execution", fastExec));
			
			// --- Close Position ---
			StackPanel closePos = new StackPanel();
			var cp1 = MakeGrid(3);
			AddToGrid(cp1, CreateBtn("25%", darkGray, (s,e) => ClosePosition(25)), 0); 
			AddToGrid(cp1, CreateBtn("50%", darkGray, (s,e) => ClosePosition(50)), 1); 
			AddToGrid(cp1, CreateBtn("75%", darkGray, (s,e) => ClosePosition(75)), 2);
			closePos.Children.Add(cp1);
			Brush flattenRed = (Brush)new BrushConverter().ConvertFrom("#80DC143C"); // Crimson at 50% opacity
			btnCloseAll = CreateBtn("Flatten", flattenRed, (s, e) => Flatten());
			closePos.Children.Add(btnCloseAll);
			mainPanel.Children.Add(CreateSection("➖ Close Position", closePos));

			// --- Manage Position ---
			StackPanel managePos = new StackPanel();
			txtPnL = new TextBlock { Text = "$0.00", Foreground = Brushes.LightGray, HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeights.Bold, Margin = new Thickness(0,2,0,5), FontSize = 14 };
			managePos.Children.Add(txtPnL);
			var rPanel = MakeGrid(2);
			txtUnrealR = new TextBlock { Text = "Unreal: 0.0 R\n\n", Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,0,0,5), FontSize = 11 };
			txtRealR = new TextBlock { Text = "Real: 0.0 R\nTotal: 0.0 R\nSession: 0.0 R", Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,0,0,5), FontSize = 11 };
			AddToGrid(rPanel, txtUnrealR, 0); AddToGrid(rPanel, txtRealR, 1);
			managePos.Children.Add(rPanel);
			mainPanel.Children.Add(CreateSection("⚙ Manage Position", managePos));

			this.Content = MainGrid;
		}

		private void UpdateDirectionButtons()
		{
			Brush greenBrush = (Brush)new BrushConverter().ConvertFrom("#FF44CC44");
			Brush redBrush = (Brush)new BrushConverter().ConvertFrom("#FFCC4444");
			Brush darkGray = (Brush)new BrushConverter().ConvertFrom("#FF2A2A2A");
			if (btnLong != null) btnLong.Background = isLongSelected ? greenBrush : darkGray;
			if (btnShort != null) btnShort.Background = !isLongSelected ? redBrush : darkGray;
			UpdatePnL(null, null); // Immediate label update
		}

		private void UpdateOrderModeButtons()
		{
			Brush amberBrush = (Brush)new BrushConverter().ConvertFrom("#FFCC9944");
			Brush darkGray = (Brush)new BrushConverter().ConvertFrom("#FF2A2A2A");
			if (btnMarket != null) btnMarket.Background = selectedOrderType == OrderType.Market ? amberBrush : darkGray;
			if (btnLimit != null) btnLimit.Background = selectedOrderType == OrderType.Limit ? amberBrush : darkGray;
			if (btnStop != null) btnStop.Background = selectedOrderType == OrderType.StopMarket ? amberBrush : darkGray;
			UpdatePnL(null, null); // Immediate label update
		}

		private void UpdateSizeModeButtons()
		{
			Brush amberBrush = (Brush)new BrushConverter().ConvertFrom("#FFCC9944");
			Brush darkGray = (Brush)new BrushConverter().ConvertFrom("#FF2A2A2A");
			if (btnFixedDollar != null) btnFixedDollar.Background = isFixedDollar ? amberBrush : darkGray;
			if (btnFixedSize != null) btnFixedSize.Background = !isFixedDollar ? amberBrush : darkGray;
		}

		private void MirrorCalculatorLines()
		{
			try {
				if (hEntry == null || hStop == null || hTarget == null) return;
				double entry = hEntry.StartAnchor.Price;
				double stop = hStop.StartAnchor.Price;
				double target = hTarget.StartAnchor.Price;
				double sDist = Math.Abs(entry - stop);
				double tDist = Math.Abs(entry - target);
				
				if (isLongSelected) {
					hStop.StartAnchor.Price = hStop.EndAnchor.Price = entry - sDist;
					hTarget.StartAnchor.Price = hTarget.EndAnchor.Price = entry + tDist;
				} else {
					hStop.StartAnchor.Price = hStop.EndAnchor.Price = entry + sDist;
					hTarget.StartAnchor.Price = hTarget.EndAnchor.Price = entry - tDist;
				}
				attachedTab.ChartControl.InvalidateVisual();
			} catch { }
		}
		
		private void AdjustContractSize(int amount)
		{
			if (int.TryParse(txtContracts.Text, out int curr)) txtContracts.Text = Math.Max(1, curr + amount).ToString();
			else txtContracts.Text = "1";
		}

		#region Core Logic
		private Account GetActiveAccount()
		{
			if (attachedTab != null) {
				Chart cw = Window.GetWindow(attachedTab) as Chart;
				if (cw != null && cw.ChartTrader != null) return cw.ChartTrader.Account;
			}
			return Account.All.FirstOrDefault(a => a.Name == "Sim101");
		}
		private Instrument GetActiveInstrument()
		{
			if (attachedTab != null) {
				Chart cw = Window.GetWindow(attachedTab) as Chart;
				if (cw != null && cw.ChartTrader != null && cw.ChartTrader.Instrument != null) return cw.ChartTrader.Instrument;
				if (attachedTab.ChartControl != null) return attachedTab.ChartControl.Instrument;
			}
			return null;
		}

		private double GetActivePrice()
		{
			try {
				if (attachedTab != null && attachedTab.ChartControl != null && attachedTab.ChartControl.BarsArray.Count > 0)
				{
					Instrument inst = GetActiveInstrument();
					if (inst != null) {
						foreach (var cb in attachedTab.ChartControl.BarsArray) {
							if (cb != null && cb.Bars != null && cb.Bars.Count > 0 && cb.Bars.Instrument != null && cb.Bars.Instrument.FullName == inst.FullName)
								return cb.Bars.GetClose(cb.Bars.Count - 1);
						}
					}
					var cb0 = attachedTab.ChartControl.BarsArray[0];
					if (cb0 != null && cb0.Bars != null && cb0.Bars.Count > 0) return cb0.Bars.GetClose(cb0.Bars.Count - 1);
				}
			} catch { }
			return 0;
		}

		private DateTime GetActiveTime()
		{
			try {
				if (attachedTab != null && attachedTab.ChartControl != null && attachedTab.ChartControl.BarsArray.Count > 0)
				{
					Instrument inst = GetActiveInstrument();
					if (inst != null) {
						foreach (var cb in attachedTab.ChartControl.BarsArray) {
							if (cb != null && cb.Bars != null && cb.Bars.Count > 0 && cb.Bars.Instrument != null && cb.Bars.Instrument.FullName == inst.FullName)
								return cb.Bars.GetTime(cb.Bars.Count - 1);
						}
					}
					var cb0 = attachedTab.ChartControl.BarsArray[0];
					if (cb0 != null && cb0.Bars != null && cb0.Bars.Count > 0) return cb0.Bars.GetTime(cb0.Bars.Count - 1);
				}
			} catch { }
			return DateTime.Now;
		}

		private void UpdatePnL(object sender, EventArgs e)
		{
			try {
				Account acc = GetActiveAccount();
				HookExecutionEvent(acc);
				Instrument inst = GetActiveInstrument();
				if (acc != null && inst != null)
				{
					Position pos = acc.Positions.FirstOrDefault(p => p.Instrument == inst);
					if (pos != null && pos.MarketPosition != MarketPosition.Flat)
					{
						double pnl = pos.GetUnrealizedProfitLoss(PerformanceUnit.Currency, GetActivePrice());
						txtPnL.Text = pnl.ToString("C2");
						txtPnL.Foreground = pnl >= 0 ? Brushes.LightGreen : Brushes.Salmon;
						double risk = 500;
						if (txtRisk != null && double.TryParse(txtRisk.Text, out double r)) risk = r;
						double uR = risk > 0 ? pnl / risk : 0;
						txtUnrealR.Text = $"Unreal: {uR:N1} R";
						txtUnrealR.Foreground = uR >= 0 ? Brushes.LimeGreen : Brushes.Salmon;
						double rR = risk > 0 ? currentTradeRealizedPnL / risk : 0;
						txtRealR.Text = $"Real: {rR:N1} R\nTotal: {(uR + rR):N1} R\nSession: {totalSessionR:N2} R";
					}
					else
					{
						if (txtPnL != null) {
							txtPnL.Text = "$0.00"; txtPnL.Foreground = Brushes.LightGray;
							txtUnrealR.Text = "Unreal: 0.0 R"; txtUnrealR.Foreground = Brushes.Gray;
							txtRealR.Text = $"Real: 0.0 R\nTotal: 0.0 R\nSession: {totalSessionR:N2} R";
						}
					}
				}

				if (isCalculatorActive && attachedTab != null && attachedTab.ChartControl != null && hEntry != null && hStop != null)
				{
					if (inst == null) inst = GetActiveInstrument();
					if (inst == null) return;
					
					double ent = hEntry.StartAnchor.Price;
					double stp = hStop.StartAnchor.Price;
					double tar = hTarget != null ? hTarget.StartAnchor.Price : 0;

					if (selectedOrderType == OrderType.Market)
					{
						double cur = GetActivePrice();
						if (Math.Abs(ent - cur) > 0.0000001) {
							hEntry.StartAnchor.Price = hEntry.EndAnchor.Price = cur;
							ent = cur;
						}
					}

					double dist = Math.Abs(ent - stp);
					double tick = inst.MasterInstrument.TickSize;
					double val = inst.MasterInstrument.PointValue;
					
					if (isFixedDollar)
					{
						if (dist > 0 && tick > 0 && txtRisk != null && double.TryParse(txtRisk.Text, out double rDist))
						{
							double tickDist = dist / tick;
							double contractVal = tickDist * val * tick;
							if (contractVal > 0) {
								int qty = (int)Math.Max(1, Math.Floor(rDist / contractVal));
								if (txtContracts != null && !txtContracts.IsFocused) txtContracts.Text = qty.ToString();
							}
						}
					}
					else
					{
						int fixedQty = 1;
						if (txtContracts != null) int.TryParse(txtContracts.Text, out fixedQty);
						if (dist > 0 && tick > 0) {
							double stopTicks = dist / tick;
							double calcRisk = stopTicks * val * tick * fixedQty;
							if (txtRisk != null && !txtRisk.IsFocused) txtRisk.Text = calcRisk.ToString("N0");
						}
					}

					int cQty = 1;
					if (txtContracts != null) int.TryParse(txtContracts.Text, out cQty);
					double stopTicksFinal = Math.Abs(ent - stp) / tick;
					double riskAmt = stopTicksFinal * val * tick * cQty;
					double targetTicks = Math.Abs(tar - ent) / tick;
					double profitAmt = targetTicks * val * tick * cQty;
					double rMult = riskAmt > 0 ? profitAmt / riskAmt : 0;

					double riskPts = Math.Abs(ent - stp);
					double profitPts = Math.Abs(tar - ent);

					string modeTxt = (isLongSelected ? "BUY " : "SELL ") + selectedOrderType.ToString().ToUpper();
					if (selectedOrderType == OrderType.StopMarket) modeTxt = (isLongSelected ? "BUY " : "SELL ") + "STOP";

					if (cEntryPill != null && cEntryTxt != null) {
						cEntryTxt.Text = $"{modeTxt} {cQty} @ {ent:F2}";
					}
					if (cStopPill != null && cStopTxt != null) {
						cStopTxt.Text = $"RISK: ${riskAmt:N2} | {riskPts:N2} pts";
					}
					if (cTargetPill != null && cTargetTxt != null) {
						cTargetTxt.Text = $"PROFIT: ${profitAmt:N2} | {profitPts:N2} pts | {rMult:N1} R";
					}
					// Note: Y-positioning moved to smooth renderHandler
					attachedTab.ChartControl.InvalidateVisual();
				}
			} catch { }
		}

		private void OnRenderFrame(object sender, EventArgs e)
		{
			if (!isCalculatorActive || hEntry == null || hStop == null || hTarget == null) return;
			try {
				if (cEntryPill != null) {
					double y = GetYByPrice(hEntry.StartAnchor.Price);
					if (y > 0) Canvas.SetTop(cEntryPill, y - 12);
					cEntryPill.Visibility = y > 0 ? Visibility.Visible : Visibility.Collapsed;
				}
				if (cStopPill != null) {
					double y = GetYByPrice(hStop.StartAnchor.Price);
					if (y > 0) Canvas.SetTop(cStopPill, y - 12);
					cStopPill.Visibility = y > 0 ? Visibility.Visible : Visibility.Collapsed;
				}
				if (cTargetPill != null) {
					double y = GetYByPrice(hTarget.StartAnchor.Price);
					if (y > 0) Canvas.SetTop(cTargetPill, y - 12);
					cTargetPill.Visibility = y > 0 ? Visibility.Visible : Visibility.Collapsed;
				}
			} catch { }
		}

		private void ExecuteTrade(OrderType orderType)
		{
			try {
				Account acc = GetActiveAccount();
				Instrument inst = GetActiveInstrument();
				if (acc == null || inst == null) return;
				Position pos = acc.Positions.FirstOrDefault(p => p.Instrument == inst);
				if (pos == null || pos.MarketPosition == MarketPosition.Flat) {
					baselineRealizedPnL = acc.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar);
					currentTradeRealizedPnL = 0;
				}
				int qty = 1;
				if (txtContracts != null) int.TryParse(txtContracts.Text, out qty);
				OrderAction act = isLongSelected ? OrderAction.Buy : OrderAction.SellShort;
				double ent = hEntry != null ? hEntry.StartAnchor.Price : 0;
				double stp = hStop != null ? hStop.StartAnchor.Price : 0;
				double tar = hTarget != null ? hTarget.StartAnchor.Price : 0;
				if (stp != 0 && tar != 0) {
					pendingStopPrice = stp; pendingTargetPrice = tar; pendingContracts = qty;
				}
				string id = "Orca_" + Guid.NewGuid().ToString("N");
				if (orderType == OrderType.Market) acc.Submit(new[] { acc.CreateOrder(inst, act, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, qty, 0, 0, "", id, DateTime.MaxValue, null) });
				else if (orderType == OrderType.Limit) acc.Submit(new[] { acc.CreateOrder(inst, act, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, qty, ent != 0 ? ent : GetActivePrice(), 0, "", id, DateTime.MaxValue, null) });
				else if (orderType == OrderType.StopMarket) acc.Submit(new[] { acc.CreateOrder(inst, act, OrderType.StopMarket, OrderEntry.Manual, TimeInForce.Day, qty, 0, ent != 0 ? ent : GetActivePrice(), "", id, DateTime.MaxValue, null) });
				pendingEntryName = id;
			} catch { }
		}

		private void ExecuteFastCommand(string cmd)
		{
			try {
				Account acc = GetActiveAccount(); Instrument inst = GetActiveInstrument();
				if (acc == null || inst == null) return;
				int qty = 1; if (txtContracts != null) int.TryParse(txtContracts.Text, out qty);
				string id = "Fast_" + Guid.NewGuid().ToString("N");
				OrderAction act = cmd.StartsWith("Sell") ? OrderAction.Sell : OrderAction.Buy;
				if (cmd.EndsWith("Mkt")) acc.Submit(new[] { acc.CreateOrder(inst, act, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, qty, 0, 0, "", id, DateTime.MaxValue, null) });
				else {
					double off = (cmd == "BuyAsk") ? inst.MasterInstrument.TickSize : -inst.MasterInstrument.TickSize;
					acc.Submit(new[] { acc.CreateOrder(inst, act, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, qty, GetActivePrice() + off, 0, "", id, DateTime.MaxValue, null) });
				}
			} catch { }
		}

		private void StartDragOrder(string orderType)
		{
			if (attachedTab == null || attachedTab.ChartControl == null) return;
			
			// Directly inject into the parent Tab Content Grid instead of searching for an elusive canvas
			var rootGrid = attachedTab.Content as Grid;
			if (rootGrid == null) return;

			if (isDragOrderActive) CancelDragOrder();

			isDragOrderActive = true;
			dragOrderType = orderType;

			dragCanvas = new Canvas 
			{ 
				Background = Brushes.Transparent, 
				Cursor = Cursors.Cross,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch 
			};
			Panel.SetZIndex(dragCanvas, 9999);
			// Do not span columns, so it inherently bounds exactly to the ChartControl (Column 0)

			Brush lineBrush = orderType.Contains("Stop") ? Brushes.Orange : Brushes.Cyan;
			string labelTxt = orderType.Contains("Buy") ? "Buy" : "Sell";
			string lmtStpTxt = orderType.Contains("Stop") ? "STP" : "LMT";

			dragLine = new System.Windows.Shapes.Line
			{
				X1 = 0, X2 = attachedTab.ChartControl.ActualWidth, // Dynamically set to the full width of the viewable chart control
				Stroke = lineBrush, StrokeThickness = 2
			};

			dragLabelTxt = new TextBlock
			{
				Foreground = Brushes.Black,
				FontWeight = FontWeights.Bold,
				HorizontalAlignment = HorizontalAlignment.Center
			};

			dragLabelPill = new Border
			{
				Background = lineBrush,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(8), // Little pill shape
				Padding = new Thickness(8, 2, 8, 2)
			};
			dragLabelPill.Child = dragLabelTxt;

			dragCanvas.Children.Add(dragLine);
			dragCanvas.Children.Add(dragLabelPill);

			dragCanvas.MouseMove += (s, e) =>
			{
				if (!isDragOrderActive) return;
				// Mouse Y-position must be exactly relative to the inner ChartControl object to map properly to the price scale!
				Point pos = e.GetPosition(attachedTab.ChartControl);
				
				double price = GetPriceByY(pos.Y);
				Instrument inst = GetActiveInstrument();
				if (inst != null && inst.MasterInstrument != null && inst.MasterInstrument.TickSize > 0)
				{
					double tick = inst.MasterInstrument.TickSize;
					price = Math.Round(price / tick) * tick;
					
					double snappedY = GetYByPrice(price);
					if (snappedY != 0) pos.Y = snappedY;
				}

				// Optional: auto-recalculate line width dynamically
				dragLine.X2 = attachedTab.ChartControl.ActualWidth;
				dragLine.Y1 = dragLine.Y2 = pos.Y;
				
				int qty = 1; int.TryParse(txtContracts.Text, out qty);
				dragLabelTxt.Text = $"{qty} {labelTxt} {lmtStpTxt} {price:F2}";

				// Lock tracking label perfectly to the left edge of the price scale (standard NT scale width is ~65px)
				Canvas.SetRight(dragLabelPill, 65);
				Canvas.SetTop(dragLabelPill, pos.Y - 12);
			};

			dragCanvas.MouseLeftButtonDown += (s, e) =>
			{
				if (!isDragOrderActive) return;
				Point pos = e.GetPosition(attachedTab.ChartControl);
				double price = GetPriceByY(pos.Y);
				
				Instrument inst = GetActiveInstrument();
				if (inst != null && inst.MasterInstrument != null && inst.MasterInstrument.TickSize > 0)
				{
					double tick = inst.MasterInstrument.TickSize;
					price = Math.Round(price / tick) * tick;
				}
				
				PlaceDragOrderAt(price);
				CancelDragOrder();
			};

			rootGrid.Children.Add(dragCanvas);

			var window = Window.GetWindow(attachedTab.ChartControl);
			if (window != null)
			{
				window.PreviewKeyDown += Window_PreviewKeyDown_CancelDrag;
			}
		}

		private double GetPriceByY(double y)
		{
			if (attachedTab == null || attachedTab.ChartControl == null) return 0;
			try
			{
				var chartPanel = attachedTab.ChartControl.ChartPanels.FirstOrDefault();
				if (chartPanel != null)
				{
					var scale = chartPanel.Scales.FirstOrDefault();
					if (scale != null) return scale.GetValueByY((float)y);
				}
			}
			catch { }
			return 0;
		}

		private double GetYByPrice(double price)
		{
			if (attachedTab == null || attachedTab.ChartControl == null) return 0;
			try
			{
				var chartPanel = attachedTab.ChartControl.ChartPanels.FirstOrDefault();
				if (chartPanel != null)
				{
					var scale = chartPanel.Scales.FirstOrDefault();
					if (scale != null) return scale.GetYByValue(price);
				}
			}
			catch { }
			return 0;
		}

		private void PlaceDragOrderAt(double price)
		{
			try {
				Account acc = GetActiveAccount(); Instrument inst = GetActiveInstrument();
				if (acc == null || inst == null) return;
				int qty = 1; if (txtContracts != null) int.TryParse(txtContracts.Text, out qty);
				string id = "Drag_" + Guid.NewGuid().ToString("N");

				OrderAction act = dragOrderType.Contains("Buy") ? OrderAction.Buy : OrderAction.Sell;
				OrderType typ = dragOrderType.Contains("Stop") ? OrderType.StopMarket : OrderType.Limit;

				acc.Submit(new[] { acc.CreateOrder(inst, act, typ, OrderEntry.Manual, TimeInForce.Day, qty, typ == OrderType.Limit ? price : 0, typ == OrderType.StopMarket ? price : 0, "", id, DateTime.MaxValue, null) });
			} catch { }
		}

		private void CancelDragOrder()
		{
			isDragOrderActive = false;
			if (dragCanvas != null)
			{
				// Because the parent is the massive root Grid, clear it from there.
				var pnl = VisualTreeHelper.GetParent(dragCanvas) as Panel;
				if (pnl != null) pnl.Children.Remove(dragCanvas);
				dragCanvas = null;
			}
			if (attachedTab != null && attachedTab.ChartControl != null)
			{
				var window = Window.GetWindow(attachedTab.ChartControl);
				if (window != null) window.PreviewKeyDown -= Window_PreviewKeyDown_CancelDrag;
			}
		}

		private void Window_PreviewKeyDown_CancelDrag(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				CancelDragOrder();
				e.Handled = true;
			}
		}

		private Canvas FindCanvas(DependencyObject parent)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is Canvas canvas) return canvas;
				var result = FindCanvas(child);
				if (result != null) return result;
			}
			return null;
		}

		private void Flatten() { try { Account acc = GetActiveAccount(); Instrument inst = GetActiveInstrument(); if (acc != null && inst != null) { foreach (var o in acc.Orders) if (o.Instrument == inst && (o.OrderState == OrderState.Accepted || o.OrderState == OrderState.Working)) acc.Cancel(new[] { o }); ClosePosition(100); } } catch { } }
		private void MoveToBreakeven() { try { Account acc = GetActiveAccount(); Instrument inst = GetActiveInstrument(); if (acc != null && inst != null) { Position p = acc.Positions.FirstOrDefault(x => x.Instrument == inst); if (p != null && p.MarketPosition != MarketPosition.Flat) { foreach (var o in acc.Orders) if (o.Instrument == inst && (o.OrderType == OrderType.StopMarket || o.OrderType == OrderType.StopLimit)) { o.StopPriceChanged = p.AveragePrice; acc.Change(new[] { o }); } } } } catch { } }
		private void ClosePosition(double pct) { try { Account acc = GetActiveAccount(); Instrument inst = GetActiveInstrument(); if (acc != null && inst != null) { Position p = acc.Positions.FirstOrDefault(x => x.Instrument == inst); if (p != null && p.MarketPosition != MarketPosition.Flat) { int q = (int)Math.Max(1, Math.Round(p.Quantity * pct / 100.0)); acc.Submit(new[] { acc.CreateOrder(inst, p.MarketPosition == MarketPosition.Long ? OrderAction.Sell : OrderAction.BuyToCover, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, q, 0, 0, "", "Orca", DateTime.MaxValue, null) }); } } } catch { } }

		private void HookExecutionEvent(Account acc) { if (acc != null && hookedAccount != acc) { if (hookedAccount != null) hookedAccount.ExecutionUpdate -= OnExecutionUpdate; acc.ExecutionUpdate += OnExecutionUpdate; hookedAccount = acc; } }
		private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
		{
			try {
				if (e.Execution == null || e.Execution.Account == null) return;
				
				double risk = 500; 
				Dispatcher.Invoke(() => { double.TryParse(txtRisk.Text, out risk); });
				
				double curReal = e.Execution.Account.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar);
				double realizedDiff = curReal - baselineRealizedPnL;
				currentTradeRealizedPnL = realizedDiff;
				
				if (e.Execution.MarketPosition == MarketPosition.Flat && risk > 0) 
				{
					double tradeR = currentTradeRealizedPnL / risk;
					Dispatcher.InvokeAsync(() => { totalSessionR += tradeR; });
					baselineRealizedPnL = curReal;
					currentTradeRealizedPnL = 0;
				}

				if (pendingEntryName == e.Execution.Order.Name && pendingStopPrice != 0 && pendingTargetPrice != 0 && e.Execution.Quantity > 0) {
					string oco = "OrcaOCO_" + Guid.NewGuid().ToString("N");
					OrderAction act = e.Execution.Order.OrderAction == OrderAction.Buy ? OrderAction.Sell : OrderAction.BuyToCover;
					e.Execution.Account.Submit(new[] { e.Execution.Account.CreateOrder(e.Execution.Instrument, act, OrderType.StopMarket, OrderEntry.Manual, TimeInForce.Day, e.Execution.Quantity, 0, pendingStopPrice, oco, "Stop", DateTime.MaxValue, null), e.Execution.Account.CreateOrder(e.Execution.Instrument, act, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, e.Execution.Quantity, pendingTargetPrice, 0, oco, "Target", DateTime.MaxValue, null) });
					pendingEntryName = null;
				}
			} catch { }
		}

		private void OnCalculatorLineMoved(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "StartAnchor" || e.PropertyName == "EndAnchor") UpdatePnL(null, null);
		}

		private void SpawnCalculator()
		{
			RemoveCalculator(); 
			try {
				if (attachedTab == null || attachedTab.ChartControl == null) return;
				var inds = attachedTab.ChartControl.Indicators;
				NinjaScriptBase owner = (inds != null && inds.Count > 0) ? inds[0] as NinjaScriptBase : null;
				if (owner == null) return;
				calcOwner = owner;

				double cp = GetActivePrice(); Instrument inst = GetActiveInstrument(); if (inst == null) return;
				double tick = inst.MasterInstrument.TickSize;
				
				// Standard defaults: NQ/MNQ = 100 ticks (25 pts), ES/MES = 20 ticks (5 pts)
				int stopTicks = inst.FullName.Contains("ES") ? 20 : 100;
				int targetTicks = stopTicks * 2;
				
				double sY = isLongSelected ? cp - (stopTicks * tick) : cp + (stopTicks * tick);
				double tY = isLongSelected ? cp + (targetTicks * tick) : cp - (targetTicks * tick);
				
				var rootGrid = attachedTab.Content as Grid;
				if (rootGrid == null) return;

				hEntry = Draw.HorizontalLine(owner, "OrcaCalcEntry", cp, Brushes.WhiteSmoke, DashStyleHelper.Solid, 2);
				hTarget = Draw.HorizontalLine(owner, "OrcaCalcTarget", tY, Brushes.LimeGreen, DashStyleHelper.Solid, 2);
				hStop = Draw.HorizontalLine(owner, "OrcaCalcStop", sY, Brushes.Salmon, DashStyleHelper.Solid, 2);

				// Create a transparent canvas overlay to host the labels
				calcCanvas = new Canvas { IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
				Panel.SetZIndex(calcCanvas, 9998);
				
				// CRITICAL: Anchor the canvas EXACTLY to where the ChartControl is in the rootGrid
				Grid.SetRow(calcCanvas, Grid.GetRow(attachedTab.ChartControl));
				Grid.SetColumn(calcCanvas, Grid.GetColumn(attachedTab.ChartControl));
				Grid.SetRowSpan(calcCanvas, Grid.GetRowSpan(attachedTab.ChartControl));
				Grid.SetColumnSpan(calcCanvas, Grid.GetColumnSpan(attachedTab.ChartControl));
				
				rootGrid.Children.Add(calcCanvas);

				// Create the three custom Pills (Entry, Stop, Target)
				cEntryPill = CreatePill(Brushes.WhiteSmoke, out cEntryTxt);
				cStopPill = CreatePill(Brushes.Salmon, out cStopTxt);
				cTargetPill = CreatePill(Brushes.LimeGreen, out cTargetTxt);

				foreach (var pill in new Border[] { cEntryPill, cStopPill, cTargetPill }) {
					Canvas.SetRight(pill, 65);
					calcCanvas.Children.Add(pill);
				}

				if (renderHandler == null) {
					renderHandler = new EventHandler(OnRenderFrame);
					CompositionTarget.Rendering += renderHandler;
				}

				foreach (var obj in new DrawingTool[] { hEntry, hTarget, hStop }) {
					if (obj == null) continue;
					try { 
						typeof(DrawingTool).GetProperty("IsUserDrawn").SetValue(obj, true, null);
						typeof(DrawingTool).GetProperty("IsAttachedToNinjaScript").SetValue(obj, false, null); 
					} catch { }
					obj.IsLocked = false; 
					obj.IsAutoScale = false;
					if (obj is INotifyPropertyChanged inpc) inpc.PropertyChanged += OnCalculatorLineMoved;
				}
				isCalculatorActive = true; 
				UpdatePnL(null, null); // Force immediate text population
				attachedTab.ChartControl.InvalidateVisual();
			} catch { }
		}

		private Border CreatePill(Brush bg, out TextBlock txt)
		{
			txt = new TextBlock
			{
				Foreground = Brushes.Black,
				FontFamily = new FontFamily("Segoe UI"),
				FontSize = 11,
				FontWeight = FontWeights.Bold,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(4, 0, 4, 0)
			};

			LinearGradientBrush gradient = new LinearGradientBrush();
			gradient.StartPoint = new Point(0, 0);
			gradient.EndPoint = new Point(0, 1);
			gradient.GradientStops.Add(new GradientStop(((Color)ColorConverter.ConvertFromString(bg.ToString())), 0.0));
			gradient.GradientStops.Add(new GradientStop(Colors.White, 3.0)); // Slow fade to white for premium sheen

			Border b = new Border
			{
				Background = bg,
				BorderBrush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(4),
				Padding = new Thickness(5, 2, 5, 2),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.5 }
			};
			b.Child = txt;
			return b;
		}

		private void RemoveCalculator()
		{
			try {
				isCalculatorActive = false;
				if (attachedTab == null || attachedTab.ChartControl == null) return;
				
				var rootGrid = attachedTab.Content as Grid;
				if (rootGrid != null && calcCanvas != null) {
					if (rootGrid.Children.Contains(calcCanvas)) rootGrid.Children.Remove(calcCanvas);
					calcCanvas = null;
				}

				if (calcOwner != null) {
					try {
						Draw.HorizontalLine(calcOwner, "OrcaCalcEntry", 0, Brushes.Black, DashStyleHelper.Solid, 1);
						Draw.HorizontalLine(calcOwner, "OrcaCalcTarget", 0, Brushes.Black, DashStyleHelper.Solid, 1);
						Draw.HorizontalLine(calcOwner, "OrcaCalcStop", 0, Brushes.Black, DashStyleHelper.Solid, 1);
					} catch { }
				}
				
				foreach (var line in new[] { hEntry, hStop, hTarget }) {
					if (line == null) continue;
					try { line.GetType().GetProperty("IsPriceMarkerVisible").SetValue(line, false, null); } catch { }
					try { line.StartAnchor.Price = line.EndAnchor.Price = 0; } catch { }
				}
				
				// Also try reflection removal as backup
				string[] tags = new[] { "OrcaCalcEntry", "OrcaCalcTarget", "OrcaCalcStop", "OrcaCalcEntryText", "OrcaCalcTargetText", "OrcaCalcStopText" };
				var rm = typeof(NinjaScriptBase).GetMethod("RemoveDrawObject", 
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, 
					null, new Type[] { typeof(string) }, null);
				if (rm != null && calcOwner != null) {
					foreach (var tag in tags)
						try { rm.Invoke(calcOwner, new object[] { tag }); } catch { }
				}
				var inds = attachedTab.ChartControl.Indicators;
				if (rm != null && inds != null) {
					foreach (var ind in inds) {
						if (ind is NinjaScriptBase nsb) {
							foreach (var tag in tags)
								try { rm.Invoke(nsb, new object[] { tag }); } catch { }
						}
					}
				}
				
				hEntry = hStop = hTarget = null; 
				cEntryPill = cStopPill = cTargetPill = null;
				cEntryTxt = cStopTxt = cTargetTxt = null;
				
				if (renderHandler != null) {
					CompositionTarget.Rendering -= renderHandler;
					renderHandler = null;
				}
				
				calcOwner = null;
				attachedTab.ChartControl.InvalidateVisual();
			} catch { }
		}
		#endregion
	}
}
