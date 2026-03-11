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
using System.Windows.Controls;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaRiskManagerNT : Indicator
	{
		private Grid MainGrid;
		private bool panelActive = false;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Orca Risk Manager NT order entry and risk management panel.";
				Name										= "Orca Risk Manager NT";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= false;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
			}
			else if (State == State.Historical)
			{
				// UI injection must happen here on the UI thread
				if (ChartControl != null)
				{
					ChartControl.Dispatcher.InvokeAsync(new Action(() => {
						CreateWPFControls();
					}));
				}
			}
			else if (State == State.Terminated)
			{
				// Cleanup UI
				if (ChartControl != null)
				{
					ChartControl.Dispatcher.InvokeAsync(new Action(() => {
						DisposeWPFControls();
					}));
				}
			}
		}

		protected override void OnBarUpdate()
		{
			// Logic here
		}
		
		#region UI Construction
		private Button btnLong;
		private Button btnShort;
		private Button btnMarket;
		private Button btnLimit;
		private Button btnBreakeven;
		private Button btnCloseAll;
		private TextBox txtContracts;
		private bool isLongSelected = true;
		
		private void CreateWPFControls()
		{
			if(panelActive) return;
			
			MainGrid = new Grid
			{
				Background = (Brush)new BrushConverter().ConvertFrom("#FF1B1B1B"),
				Width = 280,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Stretch,
				Margin = new Thickness(0, 0, 0, 0)
			};
			
			ScrollViewer scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
			StackPanel mainPanel = new StackPanel { Margin = new Thickness(5) };
			scrollViewer.Content = mainPanel;
			MainGrid.Children.Add(scrollViewer);

			Button CreateBtn(string text, Brush bg, RoutedEventHandler onClick = null) {
				var b = new Button { Content = text, Background = bg, Foreground = Brushes.White, Margin = new Thickness(2), Padding = new Thickness(5), BorderThickness = new Thickness(0) };
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

			mainPanel.Children.Add(new TextBlock { Text = "= ORCA RISK MANAGER NT =", Foreground = Brushes.White, FontFamily = new FontFamily("Arial"), FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 10)});

			// --- Quick Actions ---
			StackPanel quickActions = new StackPanel();
			var row1 = MakeGrid(2);
			AddToGrid(row1, CreateBtn("CLOSE", darkGray, (s, e) => ClosePosition(100)), 0); 
			AddToGrid(row1, CreateBtn("OPEN", darkGray), 1);
			
			var row2 = MakeGrid(2);
			btnLong = CreateBtn("/ LONG", greenBrush, (s, e) => { isLongSelected = true; UpdateDirectionButtons(); });
			btnShort = CreateBtn("\\ SHORT", darkGray, (s, e) => { isLongSelected = false; UpdateDirectionButtons(); });
			AddToGrid(row2, btnLong, 0); AddToGrid(row2, btnShort, 1);
			
			var row3 = MakeGrid(2);
			btnMarket = CreateBtn("MARKET", grayBrush, (s, e) => ExecuteTrade(OrderType.Market));
			btnLimit = CreateBtn("LIMIT", darkGray, (s, e) => ExecuteTrade(OrderType.Limit));
			AddToGrid(row3, btnMarket, 0); AddToGrid(row3, btnLimit, 1);
			
			var row4 = MakeGrid(3);
			AddToGrid(row4, CreateBtn("-1", darkGray), 0); AddToGrid(row4, CreateBtn("+1", darkGray), 1); AddToGrid(row4, CreateBtn("RESET", darkGray), 2);
			
			quickActions.Children.Add(row1); quickActions.Children.Add(row2); quickActions.Children.Add(row3); quickActions.Children.Add(row4);
			mainPanel.Children.Add(CreateSection("⚡ QUICK ACTIONS", quickActions));

			// --- Add to Position ---
			StackPanel addPos = new StackPanel();
			var atp1 = MakeGrid(2);
			AddToGrid(atp1, new TextBlock{Text="CONTRACTS", Foreground=Brushes.Gray, VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center}, 0);
			txtContracts = new TextBox{Text="1", Background=darkGray, Foreground=Brushes.White, TextAlignment=TextAlignment.Center, Margin=new Thickness(2), BorderThickness=new Thickness(0)};
			AddToGrid(atp1, txtContracts, 1);
			addPos.Children.Add(atp1);
			mainPanel.Children.Add(CreateSection("➕ POSITION SIZING", addPos));

			// --- Manage Position ---
			StackPanel managePos = new StackPanel();
			btnBreakeven = CreateBtn("MOVE TO BREAKEVEN", darkGray, (s, e) => MoveToBreakeven());
			managePos.Children.Add(btnBreakeven);
			mainPanel.Children.Add(CreateSection("⚙ MANAGE POSITION", managePos));

			// --- Close Position ---
			StackPanel closePos = new StackPanel();
			var cp1 = MakeGrid(3);
			AddToGrid(cp1, CreateBtn("25%", darkGray, (s,e) => ClosePosition(25)), 0); 
			AddToGrid(cp1, CreateBtn("50%", darkGray, (s,e) => ClosePosition(50)), 1); 
			AddToGrid(cp1, CreateBtn("75%", darkGray, (s,e) => ClosePosition(75)), 2);
			closePos.Children.Add(cp1);
			
			btnCloseAll = CreateBtn("CLOSE ALL", darkGray, (s, e) => ClosePosition(100));
			closePos.Children.Add(btnCloseAll);
			mainPanel.Children.Add(CreateSection("➖ CLOSE POSITION", closePos));
			
			if (ChartPanel != null && ChartPanel.Parent is Grid parentGrid)
			{
				if (parentGrid.ColumnDefinitions.Count == 0) parentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
				parentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
				Grid.SetColumn(MainGrid, parentGrid.ColumnDefinitions.Count - 1);
				parentGrid.Children.Add(MainGrid);
				panelActive = true;
			}
		}

		private void UpdateDirectionButtons()
		{
			Brush greenBrush = (Brush)new BrushConverter().ConvertFrom("#FF44CC44");
			Brush redBrush = (Brush)new BrushConverter().ConvertFrom("#FFCC4444");
			Brush darkGray = (Brush)new BrushConverter().ConvertFrom("#FF2A2A2A");

			if (btnLong != null) btnLong.Background = isLongSelected ? greenBrush : darkGray;
			if (btnShort != null) btnShort.Background = !isLongSelected ? redBrush : darkGray;
		}

		#region Core Trading Logic
		private Account GetActiveAccount()
		{
			// Try to get chart trader account if available
			if (ChartControl != null && ChartControl.OwnerChart != null && ChartControl.OwnerChart.ChartTrader != null)
			{
				return ChartControl.OwnerChart.ChartTrader.Account;
			}
			// Fallback to Sim101
			return Account.All.FirstOrDefault(a => a.Name == "Sim101");
		}

		private void ExecuteTrade(OrderType orderType)
		{
			Account activeAcc = GetActiveAccount();
			if (activeAcc == null || Instrument == null) return;

			int qty = 1;
			if (txtContracts != null && int.TryParse(txtContracts.Text, out int parsed))
				qty = parsed;

			OrderAction action = isLongSelected ? OrderAction.Buy : OrderAction.SellShort;
			
			// Simple Market Order Execution
			if (orderType == OrderType.Market)
			{
				Order order = activeAcc.CreateOrder(Instrument, action, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, qty, 0, 0, "OrcaRMId", "OrcaRM", DateTime.MaxValue, null);
				activeAcc.Submit(new[] { order });
			}
			else if (orderType == OrderType.Limit)
			{
				// Limit at current Ask/Bid
				double limitPrice = isLongSelected ? Close[0] : Close[0]; 
				Order order = activeAcc.CreateOrder(Instrument, action, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, qty, limitPrice, 0, "OrcaRMId", "OrcaRM", DateTime.MaxValue, null);
				activeAcc.Submit(new[] { order });
			}
		}

		private void MoveToBreakeven()
		{
			Account activeAcc = GetActiveAccount();
			if (activeAcc == null || Instrument == null) return;

			// Get current position
			Position pos = activeAcc.Positions.FirstOrDefault(p => p.Instrument == Instrument);
			if (pos != null && pos.MarketPosition != MarketPosition.Flat)
			{
				double bePrice = pos.AveragePrice;
				
				// Iterate over active orders for this account/instrument to find Stop Loss
				foreach (Order order in activeAcc.Orders)
				{
					if (order.Instrument == Instrument && (order.OrderState == OrderState.Accepted || order.OrderState == OrderState.Working))
					{
						if (order.OrderType == OrderType.StopMarket || order.OrderType == OrderType.StopLimit)
						{
							order.StopPriceChanged = bePrice;
							activeAcc.Change(new[] { order });
						}
					}
				}
			}
		}

		private void ClosePosition(double percent)
		{
			Account activeAcc = GetActiveAccount();
			if (activeAcc == null || Instrument == null) return;

			Position pos = activeAcc.Positions.FirstOrDefault(p => p.Instrument == Instrument);
			if (pos != null && pos.MarketPosition != MarketPosition.Flat)
			{
				int currentQty = pos.Quantity;
				int closeQty = (int)Math.Round(currentQty * (percent / 100.0));
				if (closeQty < 1) return;

				OrderAction action = pos.MarketPosition == MarketPosition.Long ? OrderAction.Sell : OrderAction.BuyToCover;
				Order order = activeAcc.CreateOrder(Instrument, action, OrderType.Market, OrderEntry.Manual, TimeInForce.Day, closeQty, 0, 0, "OrcRMClose", "OrcaRM", DateTime.MaxValue, null);
				activeAcc.Submit(new[] { order });
			}
		}
		#endregion
		
		private void DisposeWPFControls()
		{
			if (!panelActive) return;
			
			if (ChartPanel != null && ChartPanel.Parent is Grid parentGrid && MainGrid != null)
			{
				parentGrid.Children.Remove(MainGrid);
				// Clean up the column we added
				if (parentGrid.ColumnDefinitions.Count > 1)
				{
				   parentGrid.ColumnDefinitions.RemoveAt(parentGrid.ColumnDefinitions.Count - 1);
				}
			}
			panelActive = false;
			MainGrid = null;
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaRiskManagerNT[] cacheOrcaRiskManagerNT;
		public OrcaRiskManagerNT OrcaRiskManagerNT()
		{
			return OrcaRiskManagerNT(Input);
		}

		public OrcaRiskManagerNT OrcaRiskManagerNT(ISeries<double> input)
		{
			if (cacheOrcaRiskManagerNT != null)
				for (int idx = 0; idx < cacheOrcaRiskManagerNT.Length; idx++)
					if (cacheOrcaRiskManagerNT[idx] != null &&  cacheOrcaRiskManagerNT[idx].EqualsInput(input))
						return cacheOrcaRiskManagerNT[idx];
			return CacheIndicator<OrcaRiskManagerNT>(new OrcaRiskManagerNT(), input, ref cacheOrcaRiskManagerNT);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaRiskManagerNT OrcaRiskManagerNT()
		{
			return indicator.OrcaRiskManagerNT(Input);
		}

		public Indicators.OrcaRiskManagerNT OrcaRiskManagerNT(ISeries<double> input )
		{
			return indicator.OrcaRiskManagerNT(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaRiskManagerNT OrcaRiskManagerNT()
		{
			return indicator.OrcaRiskManagerNT(Input);
		}

		public Indicators.OrcaRiskManagerNT OrcaRiskManagerNT(ISeries<double> input )
		{
			return indicator.OrcaRiskManagerNT(input);
		}
	}
}

#endregion
