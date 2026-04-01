using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.AddOns;

public class OrcaRiskPanel : UserControl
{
	private ChartTab attachedTab;

	private DispatcherTimer pnlTimer;

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

	private bool isLongSelected = true;

	private bool isFixedDollar = true;

	private OrderType selectedOrderType = (OrderType)1;

	private string pendingEntryName;

	private double pendingStopPrice;

	private double pendingTargetPrice;

	private int pendingContracts;

	private Account hookedAccount;

	private TextBlock txtUnrealR;

	private TextBlock txtRealR;

	private double baselineRealizedPnL;

	private double currentTradeRealizedPnL;

	private static double totalSessionR;

	private bool isCalculatorActive;

	private NinjaScriptBase calcOwner;

	private HorizontalLine hEntry;

	private HorizontalLine hStop;

	private HorizontalLine hTarget;

	private Text tStop;

	private Text tTarget;

	public OrcaRiskPanel(ChartTab tab)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		attachedTab = tab;
		BuildUI();
		pnlTimer = new DispatcherTimer();
		pnlTimer.Interval = TimeSpan.FromSeconds(1.0);
		pnlTimer.Tick += UpdatePnL;
		pnlTimer.Start();
	}

	public void Cleanup()
	{
		if (pnlTimer != null)
		{
			pnlTimer.Stop();
		}
		RemoveCalculator();
	}

	private void BuildUI()
	{
		Grid grid = new Grid
		{
			Background = (Brush)new BrushConverter().ConvertFrom("#FF1B1B1B"),
			Width = 240.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Stretch,
			Margin = new Thickness(0.0, 0.0, 0.0, 0.0)
		};
		ScrollViewer scrollViewer = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		StackPanel stackPanel = (StackPanel)(scrollViewer.Content = new StackPanel
		{
			Margin = new Thickness(5.0)
		});
		grid.Children.Add(scrollViewer);
		Brush bg = (Brush)new BrushConverter().ConvertFrom("#FFCC4444");
		Brush bg2 = (Brush)new BrushConverter().ConvertFrom("#FF44CC44");
		Brush bg3 = (Brush)new BrushConverter().ConvertFrom("#FF444444");
		Brush brush = (Brush)new BrushConverter().ConvertFrom("#FF2A2A2A");
		Brush bg4 = (Brush)new BrushConverter().ConvertFrom("#FFCC9944");
		Brush steelBlue = Brushes.SteelBlue;
		stackPanel.Children.Add(new TextBlock
		{
			Text = "= Orca Risk Manager NT =",
			Foreground = Brushes.White,
			FontFamily = new FontFamily("Arial"),
			FontSize = 14.0,
			FontWeight = FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 5.0, 0.0, 10.0)
		});
		StackPanel stackPanel2 = new StackPanel();
		Grid grid2 = MakeGrid(2);
		AddToGrid(grid2, CreateBtn("Calc On", bg3, delegate
		{
			SpawnCalculator();
		}), 0);
		AddToGrid(grid2, CreateBtn("Calc Off", bg3, delegate
		{
			RemoveCalculator();
		}), 1);
		stackPanel2.Children.Add(grid2);
		Grid grid3 = MakeGrid(2);
		btnLong = CreateBtn("Long", bg2, delegate
		{
			isLongSelected = true;
			UpdateDirectionButtons();
			MirrorCalculatorLines();
		});
		btnShort = CreateBtn("Short", brush, delegate
		{
			isLongSelected = false;
			UpdateDirectionButtons();
			MirrorCalculatorLines();
		});
		AddToGrid(grid3, btnLong, 0);
		AddToGrid(grid3, btnShort, 1);
		stackPanel2.Children.Add(grid3);
		Grid grid4 = MakeGrid(3);
		btnMarket = CreateBtn("Market", bg4, delegate
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			selectedOrderType = (OrderType)1;
			UpdateOrderModeButtons();
		});
		btnLimit = CreateBtn("Limit", brush, delegate
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			selectedOrderType = (OrderType)0;
			UpdateOrderModeButtons();
		});
		btnStop = CreateBtn("Stop", brush, delegate
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			selectedOrderType = (OrderType)4;
			UpdateOrderModeButtons();
		});
		AddToGrid(grid4, btnMarket, 0);
		AddToGrid(grid4, btnLimit, 1);
		AddToGrid(grid4, btnStop, 2);
		stackPanel2.Children.Add(grid4);
		Grid grid5 = MakeGrid(2);
		btnOpen = CreateBtn("Open", steelBlue, delegate
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			ExecuteTrade(selectedOrderType);
		});
		btnClose = CreateBtn("Close", brush, delegate
		{
			ClosePosition(100.0);
		});
		AddToGrid(grid5, btnOpen, 0);
		AddToGrid(grid5, btnClose, 1);
		stackPanel2.Children.Add(grid5);
		stackPanel.Children.Add(CreateSection("⚡ Quick Actions", stackPanel2));
		StackPanel stackPanel3 = new StackPanel();
		Grid grid6 = MakeGrid(2);
		btnFixedDollar = CreateBtn("Fixed $", bg4, delegate
		{
			isFixedDollar = true;
			UpdateSizeModeButtons();
		});
		btnFixedSize = CreateBtn("Fixed Size", brush, delegate
		{
			isFixedDollar = false;
			UpdateSizeModeButtons();
		});
		AddToGrid(grid6, btnFixedDollar, 0);
		AddToGrid(grid6, btnFixedSize, 1);
		stackPanel3.Children.Add(grid6);
		KeyEventHandler value = delegate(object sender, KeyEventArgs e)
		{
			if (sender is TextBox textBox)
			{
				bool flag = (e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
				bool flag2 = e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End || e.Key == Key.Tab || e.Key == Key.Return;
				bool flag3 = e.Key == Key.Decimal || e.Key == Key.OemPeriod;
				if (flag || flag2 || flag3)
				{
					if (e.Key == Key.Return)
					{
						Keyboard.ClearFocus();
						e.Handled = true;
					}
					else if (!flag2)
					{
						e.Handled = true;
						string text = "";
						if (flag)
						{
							text = ((e.Key < Key.D0 || e.Key > Key.D9) ? ((int)(e.Key - 74)).ToString() : ((int)(e.Key - 34)).ToString());
						}
						else if (flag3)
						{
							text = ".";
						}
						if (text != "")
						{
							int selectionStart = textBox.SelectionStart;
							if (textBox.SelectionLength > 0)
							{
								textBox.Text = textBox.Text.Remove(selectionStart, textBox.SelectionLength);
							}
							textBox.Text = textBox.Text.Insert(selectionStart, text);
							textBox.SelectionStart = selectionStart + 1;
						}
					}
				}
				else
				{
					e.Handled = true;
				}
			}
		};
		Grid grid7 = MakeGrid(2);
		AddToGrid(grid7, new TextBlock
		{
			Text = "Contracts",
			Foreground = Brushes.Gray,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		}, 0);
		txtContracts = new TextBox
		{
			Text = "1",
			Background = brush,
			Foreground = Brushes.White,
			TextAlignment = TextAlignment.Center,
			Margin = new Thickness(2.0),
			BorderThickness = new Thickness(0.0)
		};
		txtContracts.PreviewKeyDown += value;
		AddToGrid(grid7, txtContracts, 1);
		stackPanel3.Children.Add(grid7);
		Grid grid8 = MakeGrid(2);
		AddToGrid(grid8, new TextBlock
		{
			Text = "Risk $",
			Foreground = Brushes.Gray,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		}, 0);
		txtRisk = new TextBox
		{
			Text = "500",
			Background = brush,
			Foreground = Brushes.White,
			TextAlignment = TextAlignment.Center,
			Margin = new Thickness(2.0),
			BorderThickness = new Thickness(0.0)
		};
		txtRisk.PreviewKeyDown += value;
		AddToGrid(grid8, txtRisk, 1);
		stackPanel3.Children.Add(grid8);
		Grid grid9 = MakeGrid(3);
		AddToGrid(grid9, CreateBtn("-1", brush, delegate
		{
			AdjustContractSize(-1);
		}), 0);
		AddToGrid(grid9, CreateBtn("+1", brush, delegate
		{
			AdjustContractSize(1);
		}), 1);
		AddToGrid(grid9, CreateBtn("Reset", brush, delegate
		{
			txtContracts.Text = "1";
		}), 2);
		stackPanel3.Children.Add(grid9);
		stackPanel.Children.Add(CreateSection("➕ Position Sizing", stackPanel3));
		StackPanel stackPanel4 = new StackPanel();
		Grid grid10 = MakeGrid(2);
		btnBuyMkt = CreateBtn("Buy Mkt", bg2, delegate
		{
			ExecuteFastCommand("BuyMkt");
		});
		btnSellMkt = CreateBtn("Sell Mkt", bg, delegate
		{
			ExecuteFastCommand("SellMkt");
		});
		AddToGrid(grid10, btnBuyMkt, 0);
		AddToGrid(grid10, btnSellMkt, 1);
		Grid grid11 = MakeGrid(2);
		btnBuyAsk = CreateBtn("Buy Ask", brush, delegate
		{
			ExecuteFastCommand("BuyAsk");
		});
		btnSellBid = CreateBtn("Sell Bid", brush, delegate
		{
			ExecuteFastCommand("SellBid");
		});
		AddToGrid(grid11, btnBuyAsk, 0);
		AddToGrid(grid11, btnSellBid, 1);
		btnBreakeven = CreateBtn("Move To Breakeven", brush, delegate
		{
			MoveToBreakeven();
		});
		stackPanel4.Children.Add(grid10);
		stackPanel4.Children.Add(grid11);
		stackPanel4.Children.Add(btnBreakeven);
		stackPanel.Children.Add(CreateSection("⚡ Fast Execution", stackPanel4));
		StackPanel stackPanel5 = new StackPanel();
		Grid grid12 = MakeGrid(3);
		AddToGrid(grid12, CreateBtn("25%", brush, delegate
		{
			ClosePosition(25.0);
		}), 0);
		AddToGrid(grid12, CreateBtn("50%", brush, delegate
		{
			ClosePosition(50.0);
		}), 1);
		AddToGrid(grid12, CreateBtn("75%", brush, delegate
		{
			ClosePosition(75.0);
		}), 2);
		stackPanel5.Children.Add(grid12);
		btnCloseAll = CreateBtn("Flatten", brush, delegate
		{
			Flatten();
		});
		stackPanel5.Children.Add(btnCloseAll);
		stackPanel.Children.Add(CreateSection("➖ Close Position", stackPanel5));
		StackPanel stackPanel6 = new StackPanel();
		txtPnL = new TextBlock
		{
			Text = "$0.00",
			Foreground = Brushes.LightGray,
			HorizontalAlignment = HorizontalAlignment.Center,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 2.0, 0.0, 5.0),
			FontSize = 14.0
		};
		stackPanel6.Children.Add(txtPnL);
		Grid grid13 = MakeGrid(2);
		txtUnrealR = new TextBlock
		{
			Text = "Unreal: 0.0 R\n\n",
			Foreground = Brushes.Gray,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0),
			FontSize = 11.0
		};
		txtRealR = new TextBlock
		{
			Text = "Real: 0.0 R\nTotal: 0.0 R\nSession: 0.0 R",
			Foreground = Brushes.Gray,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0),
			FontSize = 11.0
		};
		AddToGrid(grid13, txtUnrealR, 0);
		AddToGrid(grid13, txtRealR, 1);
		stackPanel6.Children.Add(grid13);
		stackPanel.Children.Add(CreateSection("⚙ Manage Position", stackPanel6));
		base.Content = grid;
		static void AddToGrid(Grid g, UIElement e, int col)
		{
			Grid.SetColumn(e, col);
			g.Children.Add(e);
		}
		static Button CreateBtn(string text, Brush background, RoutedEventHandler onClick = null)
		{
			Button button = new Button
			{
				Content = text,
				Background = background,
				Foreground = Brushes.White,
				Margin = new Thickness(2.0),
				Padding = new Thickness(5.0),
				BorderThickness = new Thickness(0.0)
			};
			if (onClick != null)
			{
				button.Click += onClick;
			}
			return button;
		}
		static Border CreateSection(string title, UIElement content)
		{
			return new Border
			{
				BorderBrush = Brushes.Gray,
				BorderThickness = new Thickness(1.0),
				Margin = new Thickness(0.0, 5.0, 0.0, 5.0),
				Child = new StackPanel
				{
					Children = 
					{
						(UIElement)new TextBlock
						{
							Text = title,
							Foreground = Brushes.Gray,
							FontSize = 10.0,
							Margin = new Thickness(5.0, 2.0, 0.0, 2.0)
						},
						content
					}
				}
			};
		}
		static Grid MakeGrid(int cols)
		{
			Grid grid14 = new Grid();
			for (int i = 0; i < cols; i++)
			{
				grid14.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
			}
			return grid14;
		}
	}

	private void UpdateDirectionButtons()
	{
		Brush brush = (Brush)new BrushConverter().ConvertFrom("#FF44CC44");
		Brush brush2 = (Brush)new BrushConverter().ConvertFrom("#FFCC4444");
		Brush brush3 = (Brush)new BrushConverter().ConvertFrom("#FF2A2A2A");
		if (btnLong != null)
		{
			btnLong.Background = (isLongSelected ? brush : brush3);
		}
		if (btnShort != null)
		{
			btnShort.Background = ((!isLongSelected) ? brush2 : brush3);
		}
	}

	private void UpdateOrderModeButtons()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Invalid comparison between Unknown and I4
		Brush brush = (Brush)new BrushConverter().ConvertFrom("#FFCC9944");
		Brush brush2 = (Brush)new BrushConverter().ConvertFrom("#FF2A2A2A");
		if (btnMarket != null)
		{
			btnMarket.Background = (((int)selectedOrderType == 1) ? brush : brush2);
		}
		if (btnLimit != null)
		{
			btnLimit.Background = (((int)selectedOrderType == 0) ? brush : brush2);
		}
		if (btnStop != null)
		{
			btnStop.Background = (((int)selectedOrderType == 4) ? brush : brush2);
		}
	}

	private void UpdateSizeModeButtons()
	{
		Brush brush = (Brush)new BrushConverter().ConvertFrom("#FFCC9944");
		Brush brush2 = (Brush)new BrushConverter().ConvertFrom("#FF2A2A2A");
		if (btnFixedDollar != null)
		{
			btnFixedDollar.Background = (isFixedDollar ? brush : brush2);
		}
		if (btnFixedSize != null)
		{
			btnFixedSize.Background = ((!isFixedDollar) ? brush : brush2);
		}
	}

	private void MirrorCalculatorLines()
	{
		try
		{
			if (hEntry != null && hStop != null && hTarget != null)
			{
				double price = hEntry.StartAnchor.Price;
				double price2 = hStop.StartAnchor.Price;
				double price3 = hTarget.StartAnchor.Price;
				double num = Math.Abs(price - price2);
				double num2 = Math.Abs(price - price3);
				if (isLongSelected)
				{
					ChartAnchor startAnchor = hStop.StartAnchor;
					double price4 = (hStop.EndAnchor.Price = price - num);
					startAnchor.Price = price4;
					ChartAnchor startAnchor2 = hTarget.StartAnchor;
					price4 = (hTarget.EndAnchor.Price = price + num2);
					startAnchor2.Price = price4;
				}
				else
				{
					ChartAnchor startAnchor3 = hStop.StartAnchor;
					double price4 = (hStop.EndAnchor.Price = price + num);
					startAnchor3.Price = price4;
					ChartAnchor startAnchor4 = hTarget.StartAnchor;
					price4 = (hTarget.EndAnchor.Price = price - num2);
					startAnchor4.Price = price4;
				}
				attachedTab.ChartControl.InvalidateVisual();
			}
		}
		catch
		{
		}
	}

	private void AdjustContractSize(int amount)
	{
		if (int.TryParse(txtContracts.Text, out var result))
		{
			txtContracts.Text = Math.Max(1, result + amount).ToString();
		}
		else
		{
			txtContracts.Text = "1";
		}
	}

	private Account GetActiveAccount()
	{
		if (attachedTab != null)
		{
			Window window = Window.GetWindow((DependencyObject)(object)attachedTab);
			Chart val = (Chart)(object)((window is Chart) ? window : null);
			if (val != null && val.ChartTrader != null)
			{
				return val.ChartTrader.Account;
			}
		}
		return Account.All.FirstOrDefault((Account a) => a.Name == "Sim101");
	}

	private Instrument GetActiveInstrument()
	{
		if (attachedTab != null)
		{
			Window window = Window.GetWindow((DependencyObject)(object)attachedTab);
			Chart val = (Chart)(object)((window is Chart) ? window : null);
			if (val != null && val.ChartTrader != null && val.ChartTrader.Instrument != null)
			{
				return val.ChartTrader.Instrument;
			}
			if (attachedTab.ChartControl != null)
			{
				return attachedTab.ChartControl.Instrument;
			}
		}
		return null;
	}

	private double GetActivePrice()
	{
		try
		{
			if (attachedTab != null && attachedTab.ChartControl != null && attachedTab.ChartControl.BarsArray.Count > 0)
			{
				Instrument activeInstrument = GetActiveInstrument();
				if (activeInstrument != null)
				{
					foreach (ChartBars item in attachedTab.ChartControl.BarsArray)
					{
						if (item != null && item.Bars != null && item.Bars.Count > 0 && item.Bars.Instrument != null && item.Bars.Instrument.FullName == activeInstrument.FullName)
						{
							return item.Bars.GetClose(item.Bars.Count - 1);
						}
					}
				}
				ChartBars val = attachedTab.ChartControl.BarsArray[0];
				if (val != null && val.Bars != null && val.Bars.Count > 0)
				{
					return val.Bars.GetClose(val.Bars.Count - 1);
				}
			}
		}
		catch
		{
		}
		return 0.0;
	}

	private DateTime GetActiveTime()
	{
		try
		{
			if (attachedTab != null && attachedTab.ChartControl != null && attachedTab.ChartControl.BarsArray.Count > 0)
			{
				Instrument activeInstrument = GetActiveInstrument();
				if (activeInstrument != null)
				{
					foreach (ChartBars item in attachedTab.ChartControl.BarsArray)
					{
						if (item != null && item.Bars != null && item.Bars.Count > 0 && item.Bars.Instrument != null && item.Bars.Instrument.FullName == activeInstrument.FullName)
						{
							return item.Bars.GetTime(item.Bars.Count - 1);
						}
					}
				}
				ChartBars val = attachedTab.ChartControl.BarsArray[0];
				if (val != null && val.Bars != null && val.Bars.Count > 0)
				{
					return val.Bars.GetTime(val.Bars.Count - 1);
				}
			}
		}
		catch
		{
		}
		return DateTime.Now;
	}

	private void UpdatePnL(object sender, EventArgs e)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Invalid comparison between Unknown and I4
		try
		{
			Account activeAccount = GetActiveAccount();
			HookExecutionEvent(activeAccount);
			Instrument inst = GetActiveInstrument();
			if (activeAccount != null && inst != null)
			{
				Position val = activeAccount.Positions.FirstOrDefault((Position p) => p.Instrument == inst);
				if (val != null && (int)val.MarketPosition != 2)
				{
					double unrealizedProfitLoss = val.GetUnrealizedProfitLoss((PerformanceUnit)0, GetActivePrice());
					txtPnL.Text = unrealizedProfitLoss.ToString("C2");
					txtPnL.Foreground = ((unrealizedProfitLoss >= 0.0) ? Brushes.LightGreen : Brushes.Salmon);
					double num = 500.0;
					if (txtRisk != null && double.TryParse(txtRisk.Text, out var result))
					{
						num = result;
					}
					double num2 = ((num > 0.0) ? (unrealizedProfitLoss / num) : 0.0);
					txtUnrealR.Text = $"Unreal: {num2:N1} R";
					txtUnrealR.Foreground = ((num2 >= 0.0) ? Brushes.LimeGreen : Brushes.Salmon);
					double num3 = ((num > 0.0) ? (currentTradeRealizedPnL / num) : 0.0);
					txtRealR.Text = $"Real: {num3:N1} R\nTotal: {num2 + num3:N1} R\nSession: {totalSessionR:N2} R";
				}
				else if (txtPnL != null)
				{
					txtPnL.Text = "$0.00";
					txtPnL.Foreground = Brushes.LightGray;
					txtUnrealR.Text = "Unreal: 0.0 R";
					txtUnrealR.Foreground = Brushes.Gray;
					txtRealR.Text = $"Real: 0.0 R\nTotal: 0.0 R\nSession: {totalSessionR:N2} R";
				}
			}
			if (!isCalculatorActive || inst == null || hEntry == null || hStop == null)
			{
				return;
			}
			double num4 = hEntry.StartAnchor.Price;
			double price = hStop.StartAnchor.Price;
			double num5 = ((hTarget != null) ? hTarget.StartAnchor.Price : 0.0);
			if ((int)selectedOrderType == 1)
			{
				double activePrice = GetActivePrice();
				if (Math.Abs(num4 - activePrice) > 1E-07)
				{
					ChartAnchor startAnchor = hEntry.StartAnchor;
					double price2 = (hEntry.EndAnchor.Price = activePrice);
					startAnchor.Price = price2;
					num4 = activePrice;
					attachedTab.ChartControl.InvalidateVisual();
				}
			}
			double num7 = Math.Abs(num4 - price);
			double tickSize = inst.MasterInstrument.TickSize;
			double pointValue = inst.MasterInstrument.PointValue;
			if (isFixedDollar)
			{
				if (num7 > 0.0 && tickSize > 0.0 && txtRisk != null && double.TryParse(txtRisk.Text, out var result2))
				{
					double num8 = num7 / tickSize * pointValue * tickSize;
					if (num8 > 0.0)
					{
						int num9 = (int)Math.Max(1.0, Math.Floor(result2 / num8));
						if (txtContracts != null && !txtContracts.IsFocused)
						{
							txtContracts.Text = num9.ToString();
						}
					}
				}
			}
			else
			{
				int result3 = 1;
				if (txtContracts != null)
				{
					int.TryParse(txtContracts.Text, out result3);
				}
				if (num7 > 0.0 && tickSize > 0.0)
				{
					double num10 = num7 / tickSize * pointValue * tickSize * (double)result3;
					if (txtRisk != null && !txtRisk.IsFocused)
					{
						txtRisk.Text = num10.ToString("N0");
					}
				}
			}
			int result4 = 1;
			if (txtContracts != null)
			{
				int.TryParse(txtContracts.Text, out result4);
			}
			double num11 = Math.Abs(num4 - price) / tickSize * pointValue * tickSize * (double)result4;
			double num12 = Math.Abs(num5 - num4) / tickSize * pointValue * tickSize * (double)result4;
			double num13 = ((num11 > 0.0) ? (num12 / num11) : 0.0);
			if (tStop != null)
			{
				tStop.DisplayText = $"RISK: ${num11:N2}";
				foreach (ChartAnchor anchor in ((DrawingTool)tStop).Anchors)
				{
					anchor.Price = price;
				}
			}
			if (tTarget != null)
			{
				tTarget.DisplayText = $"PROFIT: ${num12:N2} | {num13:N1} R";
				foreach (ChartAnchor anchor2 in ((DrawingTool)tTarget).Anchors)
				{
					anchor2.Price = num5;
				}
			}
			attachedTab.ChartControl.InvalidateVisual();
		}
		catch
		{
		}
	}

	private void ExecuteTrade(OrderType orderType)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I4
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Invalid comparison between Unknown and I4
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Invalid comparison between Unknown and I4
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Account activeAccount = GetActiveAccount();
			Instrument inst = GetActiveInstrument();
			if (activeAccount != null && inst != null)
			{
				Position val = activeAccount.Positions.FirstOrDefault((Position p) => p.Instrument == inst);
				if (val == null || (int)val.MarketPosition == 2)
				{
					baselineRealizedPnL = activeAccount.Get((AccountItem)18, (Currency)7);
					currentTradeRealizedPnL = 0.0;
				}
				int result = 1;
				if (txtContracts != null)
				{
					int.TryParse(txtContracts.Text, out result);
				}
				OrderAction val2 = (OrderAction)((!isLongSelected) ? 3 : 0);
				double num = ((hEntry != null) ? hEntry.StartAnchor.Price : 0.0);
				double num2 = ((hStop != null) ? hStop.StartAnchor.Price : 0.0);
				double num3 = ((hTarget != null) ? hTarget.StartAnchor.Price : 0.0);
				if (num2 != 0.0 && num3 != 0.0)
				{
					pendingStopPrice = num2;
					pendingTargetPrice = num3;
					pendingContracts = result;
				}
				string text = "Orca_" + Guid.NewGuid().ToString("N");
				if ((int)orderType == 1)
				{
					activeAccount.Submit((IEnumerable<Order>)(object)new Order[1] { activeAccount.CreateOrder(inst, val2, (OrderType)1, (OrderEntry)1, (TimeInForce)0, result, 0.0, 0.0, "", text, DateTime.MaxValue, (CustomOrder)null) });
				}
				else if ((int)orderType == 0)
				{
					activeAccount.Submit((IEnumerable<Order>)(object)new Order[1] { activeAccount.CreateOrder(inst, val2, (OrderType)0, (OrderEntry)1, (TimeInForce)0, result, (num != 0.0) ? num : GetActivePrice(), 0.0, "", text, DateTime.MaxValue, (CustomOrder)null) });
				}
				else if ((int)orderType == 4)
				{
					activeAccount.Submit((IEnumerable<Order>)(object)new Order[1] { activeAccount.CreateOrder(inst, val2, (OrderType)4, (OrderEntry)1, (TimeInForce)0, result, 0.0, (num != 0.0) ? num : GetActivePrice(), "", text, DateTime.MaxValue, (CustomOrder)null) });
				}
				pendingEntryName = text;
			}
		}
		catch
		{
		}
	}

	private void ExecuteFastCommand(string cmd)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Account activeAccount = GetActiveAccount();
			Instrument activeInstrument = GetActiveInstrument();
			if (activeAccount != null && activeInstrument != null)
			{
				int result = 1;
				if (txtContracts != null)
				{
					int.TryParse(txtContracts.Text, out result);
				}
				string text = "Fast_" + Guid.NewGuid().ToString("N");
				OrderAction val = (OrderAction)(cmd.StartsWith("Sell") ? 2 : 0);
				if (cmd.EndsWith("Mkt"))
				{
					activeAccount.Submit((IEnumerable<Order>)(object)new Order[1] { activeAccount.CreateOrder(activeInstrument, val, (OrderType)1, (OrderEntry)1, (TimeInForce)0, result, 0.0, 0.0, "", text, DateTime.MaxValue, (CustomOrder)null) });
				}
				else
				{
					double num = ((cmd == "BuyAsk") ? activeInstrument.MasterInstrument.TickSize : (0.0 - activeInstrument.MasterInstrument.TickSize));
					activeAccount.Submit((IEnumerable<Order>)(object)new Order[1] { activeAccount.CreateOrder(activeInstrument, val, (OrderType)0, (OrderEntry)1, (TimeInForce)0, result, GetActivePrice() + num, 0.0, "", text, DateTime.MaxValue, (CustomOrder)null) });
				}
			}
		}
		catch
		{
		}
	}

	private void Flatten()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		try
		{
			Account activeAccount = GetActiveAccount();
			Instrument activeInstrument = GetActiveInstrument();
			if (activeAccount == null || activeInstrument == null)
			{
				return;
			}
			foreach (Order order in activeAccount.Orders)
			{
				if (order.Instrument == activeInstrument && ((int)order.OrderState == 0 || (int)order.OrderState == 10))
				{
					activeAccount.Cancel((IEnumerable<Order>)(object)new Order[1] { order });
				}
			}
			ClosePosition(100.0);
		}
		catch
		{
		}
	}

	private void MoveToBreakeven()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Invalid comparison between Unknown and I4
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Invalid comparison between Unknown and I4
		try
		{
			Account activeAccount = GetActiveAccount();
			Instrument inst = GetActiveInstrument();
			if (activeAccount == null || inst == null)
			{
				return;
			}
			Position val = activeAccount.Positions.FirstOrDefault((Position x) => x.Instrument == inst);
			if (val == null || (int)val.MarketPosition == 2)
			{
				return;
			}
			foreach (Order order in activeAccount.Orders)
			{
				if (order.Instrument == inst && ((int)order.OrderType == 4 || (int)order.OrderType == 3))
				{
					order.StopPriceChanged = val.AveragePrice;
					activeAccount.Change((IEnumerable<Order>)(object)new Order[1] { order });
				}
			}
		}
		catch
		{
		}
	}

	private void ClosePosition(double pct)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Account activeAccount = GetActiveAccount();
			Instrument inst = GetActiveInstrument();
			if (activeAccount != null && inst != null)
			{
				Position val = activeAccount.Positions.FirstOrDefault((Position x) => x.Instrument == inst);
				if (val != null && (int)val.MarketPosition != 2)
				{
					int num = (int)Math.Max(1.0, Math.Round((double)val.Quantity * pct / 100.0));
					activeAccount.Submit((IEnumerable<Order>)(object)new Order[1] { activeAccount.CreateOrder(inst, (OrderAction)(((int)val.MarketPosition != 0) ? 1 : 2), (OrderType)1, (OrderEntry)1, (TimeInForce)0, num, 0.0, 0.0, "", "Orca", DateTime.MaxValue, (CustomOrder)null) });
				}
			}
		}
		catch
		{
		}
	}

	private void HookExecutionEvent(Account acc)
	{
		if (acc != null && hookedAccount != acc)
		{
			if (hookedAccount != null)
			{
				hookedAccount.ExecutionUpdate -= OnExecutionUpdate;
			}
			acc.ExecutionUpdate += OnExecutionUpdate;
			hookedAccount = acc;
		}
	}

	private void OnExecutionUpdate(object sender, ExecutionEventArgs e)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Invalid comparison between Unknown and I4
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (e.Execution == null || e.Execution.Account == null)
			{
				return;
			}
			double risk = 500.0;
			base.Dispatcher.Invoke(delegate
			{
				double.TryParse(txtRisk.Text, out risk);
			});
			double num = e.Execution.Account.Get((AccountItem)18, (Currency)7);
			double num2 = num - baselineRealizedPnL;
			currentTradeRealizedPnL = num2;
			if ((int)e.Execution.MarketPosition == 2 && risk > 0.0)
			{
				double tradeR = currentTradeRealizedPnL / risk;
				base.Dispatcher.InvokeAsync(delegate
				{
					totalSessionR += tradeR;
				});
				baselineRealizedPnL = num;
				currentTradeRealizedPnL = 0.0;
			}
			if (pendingEntryName == e.Execution.Order.Name && pendingStopPrice != 0.0 && pendingTargetPrice != 0.0 && e.Execution.Quantity > 0)
			{
				string text = "OrcaOCO_" + Guid.NewGuid().ToString("N");
				OrderAction val = (OrderAction)(((int)e.Execution.Order.OrderAction != 0) ? 1 : 2);
				e.Execution.Account.Submit((IEnumerable<Order>)(object)new Order[2]
				{
					e.Execution.Account.CreateOrder(e.Execution.Instrument, val, (OrderType)4, (OrderEntry)1, (TimeInForce)0, e.Execution.Quantity, 0.0, pendingStopPrice, text, "Stop", DateTime.MaxValue, (CustomOrder)null),
					e.Execution.Account.CreateOrder(e.Execution.Instrument, val, (OrderType)0, (OrderEntry)1, (TimeInForce)0, e.Execution.Quantity, pendingTargetPrice, 0.0, text, "Target", DateTime.MaxValue, (CustomOrder)null)
				});
				pendingEntryName = null;
			}
		}
		catch
		{
		}
	}

	private void OnCalculatorLineMoved(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "StartAnchor" || e.PropertyName == "EndAnchor")
		{
			UpdatePnL(null, null);
		}
	}

	private void SpawnCalculator()
	{
		RemoveCalculator();
		try
		{
			if (attachedTab == null || attachedTab.ChartControl == null)
			{
				return;
			}
			ChartObjectCollection<IndicatorRenderBase> indicators = attachedTab.ChartControl.Indicators;
			NinjaScriptBase val = (NinjaScriptBase)(object)((indicators != null && ((Collection<IndicatorRenderBase>)(object)indicators).Count > 0) ? ((Collection<IndicatorRenderBase>)(object)indicators)[0] : null);
			if (val == null)
			{
				return;
			}
			calcOwner = val;
			double activePrice = GetActivePrice();
			Instrument activeInstrument = GetActiveInstrument();
			if (activeInstrument == null)
			{
				return;
			}
			double tickSize = activeInstrument.MasterInstrument.TickSize;
			double y = (isLongSelected ? (activePrice - 100.0 * tickSize) : (activePrice + 100.0 * tickSize));
			double y2 = (isLongSelected ? (activePrice + 200.0 * tickSize) : (activePrice - 200.0 * tickSize));
			hEntry = Draw.HorizontalLine(val, "OrcaCalcEntry", activePrice, Brushes.WhiteSmoke, (DashStyleHelper)0, 2);
			hTarget = Draw.HorizontalLine(val, "OrcaCalcTarget", y2, Brushes.LimeGreen, (DashStyleHelper)0, 2);
			hStop = Draw.HorizontalLine(val, "OrcaCalcStop", y, Brushes.Salmon, (DashStyleHelper)0, 2);
			DateTime time = GetActiveTime().AddMinutes(15.0);
			tTarget = Draw.Text(val, "OrcaCalcTargetText", "PROFIT", 0, y2, Brushes.LimeGreen);
			tStop = Draw.Text(val, "OrcaCalcStopText", "RISK", 0, y, Brushes.Salmon);
			if (tTarget != null)
			{
				tTarget.YPixelOffset = -15;
				foreach (ChartAnchor anchor in ((DrawingTool)tTarget).Anchors)
				{
					anchor.Time = time;
				}
			}
			if (tStop != null)
			{
				tStop.YPixelOffset = -15;
				foreach (ChartAnchor anchor2 in ((DrawingTool)tStop).Anchors)
				{
					anchor2.Time = time;
				}
			}
			DrawingTool[] array = (DrawingTool[])(object)new DrawingTool[5] { hEntry, hTarget, hStop, tTarget, tStop };
			foreach (DrawingTool val2 in array)
			{
				if (val2 != null)
				{
					try
					{
						typeof(DrawingTool).GetProperty("IsUserDrawn").SetValue(val2, true, null);
						typeof(DrawingTool).GetProperty("IsAttachedToNinjaScript").SetValue(val2, false, null);
					}
					catch
					{
					}
					val2.IsLocked = false;
					((ChartObject)val2).IsAutoScale = false;
					if (val2 is INotifyPropertyChanged notifyPropertyChanged)
					{
						notifyPropertyChanged.PropertyChanged += OnCalculatorLineMoved;
					}
				}
			}
			isCalculatorActive = true;
			attachedTab.ChartControl.InvalidateVisual();
		}
		catch
		{
		}
	}

	private void RemoveCalculator()
	{
		try
		{
			isCalculatorActive = false;
			if (attachedTab == null || attachedTab.ChartControl == null)
			{
				return;
			}
			if (calcOwner != null)
			{
				try
				{
					Draw.HorizontalLine(calcOwner, "OrcaCalcEntry", 0.0, Brushes.Black, (DashStyleHelper)0, 1);
					Draw.HorizontalLine(calcOwner, "OrcaCalcTarget", 0.0, Brushes.Black, (DashStyleHelper)0, 1);
					Draw.HorizontalLine(calcOwner, "OrcaCalcStop", 0.0, Brushes.Black, (DashStyleHelper)0, 1);
					Draw.Text(calcOwner, "OrcaCalcTargetText", "", 0, 0.0, Brushes.Black);
					Draw.Text(calcOwner, "OrcaCalcStopText", "", 0, 0.0, Brushes.Black);
				}
				catch
				{
				}
			}
			HorizontalLine[] array = new HorizontalLine[3] { hEntry, hStop, hTarget };
			foreach (HorizontalLine horizontalLine in array)
			{
				if (horizontalLine != null)
				{
					try
					{
						((object)horizontalLine).GetType().GetProperty("IsPriceMarkerVisible").SetValue(horizontalLine, false, null);
					}
					catch
					{
					}
					try
					{
						ChartAnchor startAnchor = horizontalLine.StartAnchor;
						double price = (horizontalLine.EndAnchor.Price = 0.0);
						startAnchor.Price = price;
					}
					catch
					{
					}
				}
			}
			Text[] array2 = new Text[2] { tStop, tTarget };
			foreach (Text text in array2)
			{
				if (text == null)
				{
					continue;
				}
				try
				{
					text.DisplayText = "";
					foreach (ChartAnchor anchor in ((DrawingTool)text).Anchors)
					{
						anchor.Price = 0.0;
					}
				}
				catch
				{
				}
			}
			string[] array3 = new string[5] { "OrcaCalcEntry", "OrcaCalcTarget", "OrcaCalcStop", "OrcaCalcTargetText", "OrcaCalcStopText" };
			MethodInfo method = typeof(NinjaScriptBase).GetMethod("RemoveDrawObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(string) }, null);
			if (method != null && calcOwner != null)
			{
				string[] array4 = array3;
				foreach (string text2 in array4)
				{
					try
					{
						method.Invoke(calcOwner, new object[1] { text2 });
					}
					catch
					{
					}
				}
			}
			ChartObjectCollection<IndicatorRenderBase> indicators = attachedTab.ChartControl.Indicators;
			if (method != null && indicators != null)
			{
				foreach (IndicatorRenderBase item in (Collection<IndicatorRenderBase>)(object)indicators)
				{
					if (item == null)
					{
						continue;
					}
					string[] array4 = array3;
					foreach (string text3 in array4)
					{
						try
						{
							method.Invoke(item, new object[1] { text3 });
						}
						catch
						{
						}
					}
				}
			}
			hEntry = (hStop = (hTarget = null));
			tStop = (tTarget = null);
			calcOwner = null;
			attachedTab.ChartControl.InvalidateVisual();
		}
		catch
		{
		}
	}
}
