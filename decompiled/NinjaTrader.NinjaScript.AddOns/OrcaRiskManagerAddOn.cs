using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.AddOns;

public sealed class OrcaRiskManagerAddOn : AddOnBase
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Orca Risk Manager NT ChartTrader Injection AddOn";
			((NinjaScript)this).Name = "Orca Risk Manager NT AddOn";
		}
	}

	protected override void OnWindowCreated(Window window)
	{
		Chart chartWindow = (Chart)(object)((window is Chart) ? window : null);
		if (chartWindow == null)
		{
			return;
		}
		((DispatcherObject)(object)chartWindow).Dispatcher.InvokeAsync(delegate
		{
			if (((FrameworkElement)(object)chartWindow).IsLoaded)
			{
				InsertChartTraderControl(chartWindow);
			}
		});
		((FrameworkElement)(object)chartWindow).Loaded += delegate
		{
			((DispatcherObject)(object)chartWindow).Dispatcher.InvokeAsync(delegate
			{
				InsertChartTraderControl(chartWindow);
			});
		};
		DispatcherTimer retryTimer = new DispatcherTimer();
		retryTimer.Interval = TimeSpan.FromSeconds(2.0);
		retryTimer.Tick += delegate
		{
			retryTimer.Stop();
			InsertChartTraderControl(chartWindow);
		};
		retryTimer.Start();
		((UIElement)(object)chartWindow).PreviewKeyDown += delegate(object s, KeyEventArgs e)
		{
			if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
			{
				e.Handled = true;
				TogglePanelVisibility(chartWindow);
			}
		};
		((DispatcherObject)(object)chartWindow).Dispatcher.InvokeAsync(delegate
		{
			if (((NTWindow)chartWindow).MainTabControl != null)
			{
				((NTWindow)chartWindow).MainTabControl.SelectionChanged += delegate
				{
					InsertChartTraderControl(chartWindow);
				};
			}
		});
	}

	private void TogglePanelVisibility(Chart chartWindow)
	{
		if (((NTWindow)chartWindow).MainTabControl == null || ((NTWindow)chartWindow).MainTabControl.Items.Count == 0)
		{
			return;
		}
		foreach (object item in (IEnumerable)((NTWindow)chartWindow).MainTabControl.Items)
		{
			ChartTab val = (ChartTab)((item is ChartTab) ? item : null);
			if (val == null && item is TabItem tabItem)
			{
				object content = tabItem.Content;
				val = (ChartTab)((content is ChartTab) ? content : null);
			}
			if (val == null || !(((ContentControl)(object)val).Content is Grid grid))
			{
				continue;
			}
			bool flag = false;
			foreach (UIElement child in grid.Children)
			{
				if (!(child.GetType().Name == "OrcaRiskPanel"))
				{
					continue;
				}
				flag = true;
				int column = Grid.GetColumn(child);
				if (column >= 0 && column < grid.ColumnDefinitions.Count)
				{
					ColumnDefinition columnDefinition = grid.ColumnDefinitions[column];
					if (columnDefinition.Width.Value > 0.0)
					{
						columnDefinition.Width = new GridLength(0.0);
						child.Visibility = Visibility.Collapsed;
					}
					else
					{
						columnDefinition.Width = new GridLength(240.0);
						child.Visibility = Visibility.Visible;
					}
				}
			}
			if (!flag)
			{
				InsertChartTraderControl(chartWindow);
			}
		}
	}

	private void InsertChartTraderControl(Chart chartWindow)
	{
		try
		{
			if (((NTWindow)chartWindow).MainTabControl == null || ((NTWindow)chartWindow).MainTabControl.Items.Count == 0)
			{
				return;
			}
			foreach (object item in (IEnumerable)((NTWindow)chartWindow).MainTabControl.Items)
			{
				ChartTab val = (ChartTab)((item is ChartTab) ? item : null);
				if (val == null && item is TabItem tabItem)
				{
					object content = tabItem.Content;
					val = (ChartTab)((content is ChartTab) ? content : null);
				}
				if (val == null || !(((ContentControl)(object)val).Content is Grid grid))
				{
					continue;
				}
				List<UIElement> list = new List<UIElement>();
				int num = 0;
				foreach (UIElement child in grid.Children)
				{
					if (child.GetType().Name == "OrcaRiskPanel")
					{
						if (child is OrcaRiskPanel)
						{
							list.Clear();
							num = -1;
							break;
						}
						list.Add(child);
						num++;
					}
				}
				if (num == -1)
				{
					continue;
				}
				foreach (UIElement item2 in list)
				{
					try
					{
						item2.GetType().GetMethod("Cleanup")?.Invoke(item2, null);
					}
					catch
					{
					}
					grid.Children.Remove(item2);
				}
				for (int i = 0; i < num; i++)
				{
					if (grid.ColumnDefinitions.Count > 0)
					{
						grid.ColumnDefinitions.RemoveAt(grid.ColumnDefinitions.Count - 1);
					}
				}
				OrcaRiskPanel element = new OrcaRiskPanel(val);
				if (grid.ColumnDefinitions.Count == 0)
				{
					grid.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star)
					});
				}
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(240.0)
				});
				Grid.SetColumn(element, grid.ColumnDefinitions.Count - 1);
				grid.Children.Add(element);
			}
		}
		catch
		{
		}
	}
}
