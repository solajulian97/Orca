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

			chartWindow.Dispatcher.InvokeAsync(() => {
				if (chartWindow.IsLoaded) InsertChartTraderControl(chartWindow);
			});

			chartWindow.Loaded += (s, e) => {
				chartWindow.Dispatcher.InvokeAsync(() => { InsertChartTraderControl(chartWindow); });
			};

			var retryTimer = new System.Windows.Threading.DispatcherTimer();
			retryTimer.Interval = TimeSpan.FromSeconds(2);
			retryTimer.Tick += (s, e) => { retryTimer.Stop(); InsertChartTraderControl(chartWindow); };
			retryTimer.Start();

			chartWindow.PreviewKeyDown += (s, e) => {
				if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control) {
					e.Handled = true;
					TogglePanelVisibility(chartWindow);
				}
			};

			chartWindow.Dispatcher.InvokeAsync(() => {
				if (chartWindow.MainTabControl != null) {
					chartWindow.MainTabControl.SelectionChanged += (s, e) => { InsertChartTraderControl(chartWindow); };
				}
			});
		}

		private void TogglePanelVisibility(Chart chartWindow)
		{
			if (chartWindow.MainTabControl == null || chartWindow.MainTabControl.Items.Count == 0) return;
			foreach (object item in chartWindow.MainTabControl.Items) {
				ChartTab tab = item as ChartTab;
				if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
				if (tab == null) continue;
				if (tab.Content is System.Windows.Controls.Grid tabGrid) {
					bool foundPanel = false;
					foreach (UIElement el in tabGrid.Children) {
						if (el.GetType().Name == "OrcaRiskPanel") {
							foundPanel = true;
							int col = System.Windows.Controls.Grid.GetColumn(el);
							if (col >= 0 && col < tabGrid.ColumnDefinitions.Count) {
								var colDef = tabGrid.ColumnDefinitions[col];
								if (colDef.Width.Value > 0) { colDef.Width = new GridLength(0); el.Visibility = Visibility.Collapsed; }
								else { colDef.Width = new GridLength(235); el.Visibility = Visibility.Visible; ((FrameworkElement)el).HorizontalAlignment = HorizontalAlignment.Stretch; }
							}
						}
					}
					if (!foundPanel) InsertChartTraderControl(chartWindow);
				}
			}
		}

		private void InsertChartTraderControl(Chart chartWindow)
		{
			try {
				if (chartWindow.MainTabControl == null || chartWindow.MainTabControl.Items.Count == 0) return;
				foreach (object item in chartWindow.MainTabControl.Items) {
					ChartTab tab = item as ChartTab;
					if (tab == null && item is TabItem tabItem) tab = tabItem.Content as ChartTab;
					if (tab == null) continue;
					if (tab.Content is System.Windows.Controls.Grid tabGrid) {
						var staleToRemove = new List<UIElement>();
						int staleCols = 0;
						foreach(UIElement el in tabGrid.Children) {
							if (el.GetType().Name == "OrcaRiskPanel") {
								if (el is OrcaRiskPanel) { staleToRemove.Clear(); staleCols = -1; break; }
								else { staleToRemove.Add(el); staleCols++; }
							}
						}
						if (staleCols == -1) continue;
						foreach(UIElement el in staleToRemove) {
							try { el.GetType().GetMethod("Cleanup")?.Invoke(el, null); } catch { }
							tabGrid.Children.Remove(el);
						}
						for (int i = 0; i < staleCols; i++) if (tabGrid.ColumnDefinitions.Count > 0) tabGrid.ColumnDefinitions.RemoveAt(tabGrid.ColumnDefinitions.Count - 1);
						OrcaRiskPanel orcaPanel = new OrcaRiskPanel(tab);
						orcaPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
						if (tabGrid.ColumnDefinitions.Count == 0) tabGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
						tabGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(235) });
						System.Windows.Controls.Grid.SetColumn(orcaPanel, tabGrid.ColumnDefinitions.Count - 1);
						tabGrid.Children.Add(orcaPanel);
					}
				}
			} catch { }
		}
	}

	public class OrcaRiskPanel : System.Windows.Controls.UserControl
	{
		private ChartTab attachedTab;
		private System.Windows.Threading.DispatcherTimer pnlTimer;
		private System.Windows.Controls.Button btnLong, btnShort, btnMarket, btnLimit, btnStop, btnOpen, btnClose, btnBuyMkt, btnSellMkt, btnBuyAsk, btnSellBid, btnBreakeven, btnCloseAll, btnFixedDollar, btnFixedSize, btnBuyLmt, btnSellLmt, btnBuyStop, btnSellStop;
		private System.Windows.Controls.TextBox txtContracts, txtRisk, txtPoints;
		private System.Windows.Controls.TextBlock txtPnL, txtUnrealR, txtRealR;
		private bool isDragOrderActive = false;
		private string dragOrderType = null;
		private System.Windows.Shapes.Line dragLine = null;
		private System.Windows.Controls.Canvas dragCanvas = null;
		private System.Windows.Controls.Border dragLabelPill = null;
		private System.Windows.Controls.TextBlock dragLabelTxt = null;
		private bool isLongSelected = true, isFixedDollar = true, isCalculatorActive = false;
		private OrderType selectedOrderType = OrderType.Market;
		private string pendingEntryName = null;
		private double pendingStopPrice = 0, pendingTargetPrice = 0, baselineRealizedPnL = 0, currentTradeRealizedPnL = 0;
		private int pendingContracts = 0;
		private Account hookedAccount = null;
		private static double totalSessionR = 0;
		private NinjaScriptBase calcOwner = null;
		private NinjaTrader.NinjaScript.DrawingTools.HorizontalLine hEntry, hStop, hTarget;
		private System.Windows.Controls.Canvas calcCanvas;
		private System.Windows.Controls.Border cEntryPill, cStopPill, cTargetPill;
		private System.Windows.Controls.TextBlock cEntryTxt, cStopTxt, cTargetTxt;
		private EventHandler renderHandler;

		public OrcaRiskPanel(ChartTab tab) { attachedTab = tab; BuildUI(); pnlTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; pnlTimer.Tick += UpdatePnL; pnlTimer.Start(); }
		public void Cleanup() { if (pnlTimer != null) pnlTimer.Stop(); RemoveCalculator(); }

		private void BuildUI() {
			var G = new System.Windows.Controls.Grid { Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF1B1B1B"), HorizontalAlignment = HorizontalAlignment.Stretch };
			var S = new System.Windows.Controls.ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalContentAlignment = HorizontalAlignment.Stretch };
			var P = new System.Windows.Controls.StackPanel { Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch };
			S.Content = P; G.Children.Add(S);
			System.Windows.Controls.Button CB(string t, System.Windows.Media.Brush b, RoutedEventHandler h = null) {
				var btn = new System.Windows.Controls.Button { 
					Content = new System.Windows.Controls.TextBlock { Text = t, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(-4, 0, 0, 0) }, 
					Background = b, Foreground = System.Windows.Media.Brushes.GhostWhite, 
					FontWeight = FontWeights.Normal, Margin = new Thickness(1), Padding = new Thickness(5), 
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
		private Instrument GetActiveInstrument() { Chart cw = Window.GetWindow(attachedTab) as Chart; if (cw?.ChartTrader?.Instrument != null) return cw.ChartTrader.Instrument; return attachedTab?.ChartControl?.Instrument; }
		private double GetActivePrice() { var cb = attachedTab?.ChartControl?.BarsArray.FirstOrDefault(x => x.Bars?.Instrument?.FullName == GetActiveInstrument()?.FullName) ?? attachedTab?.ChartControl?.BarsArray.FirstOrDefault(); return cb?.Bars?.GetClose(cb.Bars.Count-1) ?? 0; }
		private void UpdatePnL(object s, EventArgs e) {
			try {
				Account acc = GetActiveAccount(); HookExecutionEvent(acc); Instrument ins = GetActiveInstrument(); if (acc == null || ins == null) return;
				Position pos = acc.Positions.FirstOrDefault(p => p.Instrument == ins);
				if (pos != null && pos.MarketPosition != MarketPosition.Flat) {
					double pnl = pos.GetUnrealizedProfitLoss(PerformanceUnit.Currency, GetActivePrice()); txtPnL.Text = pnl.ToString("C2"); txtPnL.Foreground = pnl >= 0? System.Windows.Media.Brushes.LightGreen: System.Windows.Media.Brushes.Salmon;
					double risk = 500; if (double.TryParse(txtRisk.Text, out double r)) risk = r; double uR = risk > 0? pnl / risk: 0; txtUnrealR.Text = $"Unrealized: {uR:N1}R"; txtUnrealR.Foreground = uR >= 0? System.Windows.Media.Brushes.LimeGreen: System.Windows.Media.Brushes.Salmon;
					double rR = risk > 0? currentTradeRealizedPnL / risk: 0; txtRealR.Text = $"Realized: {rR:N1}R";
				} else { txtPnL.Text = "$0.00"; txtPnL.Foreground = System.Windows.Media.Brushes.LightGray; txtUnrealR.Text = "Unrealized: 0.0R"; txtRealR.Text = $"Realized: 0.0R"; }
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
		private void PlaceDragOrderAt(double p) { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc==null || ins==null) return; int q = 1; int.TryParse(txtContracts.Text, out q); string id = "Drag_" + Guid.NewGuid().ToString("N"); OrderAction act = dragOrderType.Contains("Buy") ? OrderAction.Buy : OrderAction.Sell; OrderType typ = dragOrderType.Contains("Stop") ? OrderType.StopMarket : OrderType.Limit; acc.Submit(new[] { acc.CreateOrder(ins, act, typ, OrderEntry.Manual, TimeInForce.Day, q, typ==OrderType.Limit?p:0, typ==OrderType.StopMarket?p:0, "", id, DateTime.MaxValue, null) }); } catch { } }
		private void CancelDragOrder() { isDragOrderActive = false; if (dragCanvas != null) { (System.Windows.Media.VisualTreeHelper.GetParent(dragCanvas) as System.Windows.Controls.Panel)?.Children.Remove(dragCanvas); dragCanvas = null; } if (attachedTab?.ChartControl != null) { var w = Window.GetWindow(attachedTab.ChartControl); if (w != null) w.PreviewKeyDown -= Window_PreviewKeyDown_CancelDrag; } }
		private void Window_PreviewKeyDown_CancelDrag(object s, KeyEventArgs e) { if (e.Key == Key.Escape) { CancelDragOrder(); e.Handled = true; } }
		private void Flatten() { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc != null && ins != null) { foreach (var o in acc.Orders) if (o.Instrument == ins && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted)) acc.Cancel(new[] { o }); ClosePosition(100); } } catch { } }
		private void MoveToBreakeven() { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc != null && ins != null) { var p = acc.Positions.FirstOrDefault(x => x.Instrument == ins); if (p != null && p.MarketPosition != MarketPosition.Flat) { foreach (var o in acc.Orders) if (o.Instrument == ins && o.OrderType == OrderType.StopMarket) { o.StopPriceChanged = p.AveragePrice; acc.Change(new[] { o }); } } } } catch { } }
		private void ClosePosition(double pct) { try { Account acc = GetActiveAccount(); Instrument ins = GetActiveInstrument(); if (acc != null && ins != null) { var p = acc.Positions.FirstOrDefault(x => x.Instrument == ins); if (p != null && p.MarketPosition != MarketPosition.Flat) { int q = (int)Math.Max(1, Math.Round(p.Quantity * pct / 100.0)); acc.Submit(new[] { acc.CreateOrder(ins, p.MarketPosition == MarketPosition.Long ? OrderAction.Sell : OrderAction.BuyToCover, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, q, 0, 0, "", "Orca", DateTime.MaxValue, null) }); } } } catch { } }
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
				var pos = acc.Positions.FirstOrDefault(p => p.Instrument == ins);
				int absPos = (pos != null) ? Math.Abs(pos.Quantity) : 0;
				void SyncType(string name) {
					var orders = acc.Orders.Where(o => o.Instrument == ins && o.Name == name && (o.OrderState == OrderState.Working || o.OrderState == OrderState.Accepted)).OrderByDescending(o => o.Time).ToList();
					int total = orders.Sum(o => o.Quantity);
					if (total > absPos) {
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
