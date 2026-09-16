#region Using declarations
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	public sealed class OrcaDiagnosticsAddOn : AddOnBase
	{
		private static readonly object ChartRegistrySync = new object();
		private static readonly HashSet<Chart> OpenChartWindows = new HashSet<Chart>();
		private NTMenuItem diagnosticsMenuItem;
		private NTMenuItem hostMenu;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Internal Orca workspace diagnostics and source observability";
				Name = "Orca Diagnostics";
			}
		}

		protected override void OnWindowCreated(Window window)
		{
			Chart chartWindow = window as Chart;
			if (chartWindow != null)
			{
				lock (ChartRegistrySync)
					OpenChartWindows.Add(chartWindow);
			}

			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || diagnosticsMenuItem != null)
				return;

			hostMenu = controlCenter.FindFirst("ControlCenterMenuItemTools") as NTMenuItem
				?? controlCenter.FindFirst("toolsMenuItem") as NTMenuItem
				?? controlCenter.FindFirst("ControlCenterMenuItemNew") as NTMenuItem;
			if (hostMenu == null)
				return;

			diagnosticsMenuItem = new NTMenuItem
			{
				Header = "Orca Diagnostics",
				Style = Application.Current == null ? null : Application.Current.TryFindResource("MainMenuItem") as Style
			};
			diagnosticsMenuItem.Click += OnMenuItemClick;
			hostMenu.Items.Add(diagnosticsMenuItem);
		}

		protected override void OnWindowDestroyed(Window window)
		{
			Chart chartWindow = window as Chart;
			if (chartWindow != null)
			{
				lock (ChartRegistrySync)
					OpenChartWindows.Remove(chartWindow);
				return;
			}

			ControlCenter controlCenter = window as ControlCenter;
			if (controlCenter == null || diagnosticsMenuItem == null || hostMenu == null)
				return;

			diagnosticsMenuItem.Click -= OnMenuItemClick;
			hostMenu.Items.Remove(diagnosticsMenuItem);
			diagnosticsMenuItem = null;
			hostMenu = null;
		}

		private void OnMenuItemClick(object sender, RoutedEventArgs e)
		{
			Dispatcher dispatcher = Application.Current == null ? Dispatcher.CurrentDispatcher : Application.Current.Dispatcher;
			dispatcher.InvokeAsync(() => OrcaDiagnosticsWindow.ShowOrActivate());
		}

		internal static int GetOpenChartTabCount()
		{
			int count = 0;
			try
			{
				lock (ChartRegistrySync)
				{
					foreach (Chart chartWindow in OpenChartWindows)
					{
						if (chartWindow == null)
							continue;
						int tabCount = chartWindow.MainTabControl == null ? 0 : chartWindow.MainTabControl.Items.Count;
						count += Math.Max(1, tabCount);
					}
				}
				if (count == 0 && Application.Current != null)
				{
					foreach (Window window in Application.Current.Windows)
					{
						Chart chartWindow = window as Chart;
						if (chartWindow == null)
							continue;
						int tabCount = chartWindow.MainTabControl == null ? 0 : chartWindow.MainTabControl.Items.Count;
						count += Math.Max(1, tabCount);
					}
				}
			}
			catch { }
			return count;
		}
	}

	public sealed class OrcaDiagnosticsWindow : NTWindow
	{
		private static OrcaDiagnosticsWindow instance;
		private readonly ObservableCollection<OrcaDiagnosticsRow> rows = new ObservableCollection<OrcaDiagnosticsRow>();
        private readonly Dictionary<string, SnapshotBaseline> previousSnapshots = new Dictionary<string, SnapshotBaseline>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CapturePeak> capturePeaks = new Dictionary<string, CapturePeak>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, WorkWindow> workWindows = new Dictionary<string, WorkWindow>(StringComparer.OrdinalIgnoreCase);
		private readonly WorkWindow workspaceWorkWindow = new WorkWindow();
		private readonly CheckBox enabledBox;
		private readonly CheckBox pauseBox;
		private readonly TextBlock statusText;
		private readonly DispatcherTimer refreshTimer;
		private Border healthDot;
		private TextBlock healthStateText;
		private TextBlock healthReasonText;
		private ProgressBar lagHeadroomBar;
		private TextBlock lagHeadroomText;
		private TextBlock lagValueText;
		private TextBlock lagDetailText;
		private TextBlock workValueText;
		private TextBlock workDetailText;
		private TextBlock workspaceValueText;
		private TextBlock workspaceDetailText;
		private TextBlock topPressureText;
		private int elevatedLagRefreshes;
		private int severeLagRefreshes;
		private int risingLagRefreshes;
		private double previousWorstLiveLag = double.NaN;

        private sealed class SnapshotBaseline
        {
            public OrcaDiagnosticsSnapshot Snapshot;
            public DateTime CapturedUtc;
        }

        private sealed class CapturePeak
        {
            public double WorkMsPerSecond;
            public DateTime WorkUtc;
            public double LoadScore;
            public DateTime LoadUtc;
        }

		private sealed class WorkSample
		{
			public DateTime EndUtc;
			public double DurationSeconds;
			public double WorkMsPerSecond;
		}

		private sealed class WorkWindow
		{
			private readonly List<WorkSample> samples = new List<WorkSample>();

			public void Add(DateTime endUtc, double durationSeconds, double workMsPerSecond)
			{
				if (endUtc == DateTime.MinValue || durationSeconds <= 0
					|| double.IsNaN(workMsPerSecond) || double.IsInfinity(workMsPerSecond))
					return;

				samples.Add(new WorkSample
				{
					EndUtc = endUtc,
					DurationSeconds = durationSeconds,
					WorkMsPerSecond = Math.Max(0, workMsPerSecond)
				});
				Trim(endUtc);
			}

			public double Average(DateTime endUtc, double windowSeconds)
			{
				if (samples.Count == 0 || windowSeconds <= 0)
					return 0;

				DateTime windowStart = endUtc.AddSeconds(-windowSeconds);
				double weightedWork = 0;
				double observedSeconds = 0;
				foreach (WorkSample sample in samples)
				{
					DateTime sampleStart = sample.EndUtc.AddSeconds(-sample.DurationSeconds);
					DateTime overlapStart = sampleStart > windowStart ? sampleStart : windowStart;
					DateTime overlapEnd = sample.EndUtc < endUtc ? sample.EndUtc : endUtc;
					double overlapSeconds = (overlapEnd - overlapStart).TotalSeconds;
					if (overlapSeconds <= 0)
						continue;
					weightedWork += sample.WorkMsPerSecond * overlapSeconds;
					observedSeconds += overlapSeconds;
				}
				return observedSeconds <= 0 ? 0 : weightedWork / observedSeconds;
			}

			public double Peak(DateTime endUtc, double windowSeconds)
			{
				if (samples.Count == 0 || windowSeconds <= 0)
					return 0;

				DateTime windowStart = endUtc.AddSeconds(-windowSeconds);
				double peak = 0;
				foreach (WorkSample sample in samples)
				{
					DateTime sampleStart = sample.EndUtc.AddSeconds(-sample.DurationSeconds);
					if (sample.EndUtc <= windowStart || sampleStart >= endUtc)
						continue;
					peak = Math.Max(peak, sample.WorkMsPerSecond);
				}
				return peak;
			}

			public void Clear()
			{
				samples.Clear();
			}

			private void Trim(DateTime endUtc)
			{
				DateTime cutoff = endUtc.AddSeconds(-60);
				int removeCount = 0;
				while (removeCount < samples.Count && samples[removeCount].EndUtc <= cutoff)
					removeCount++;
				if (removeCount > 0)
					samples.RemoveRange(0, removeCount);
			}
		}

		private sealed class ChartHealthMetric
		{
			public string Chart;
			public string Instrument;
			public double WorstLagSeconds;
			public double FeedAgeSeconds;
		}

		private OrcaDiagnosticsWindow()
		{
			Caption = "Orca Diagnostics";
			Title = "Orca Diagnostics";
			Width = 1420;
			Height = 760;
			MinWidth = 980;
			MinHeight = 520;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Background = Brush("#FF0F141B");
			Foreground = Brush("#FFEAF0F6");

			Grid root = new Grid { Background = Brush("#FF0F141B") };
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			StackPanel toolbar = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(12, 12, 12, 8)
			};

			enabledBox = new CheckBox
			{
				Content = "Diagnostics",
				IsChecked = OrcaDiagnosticsCore.IsEnabled,
				Foreground = Brush("#FFEAF0F6"),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 14, 0)
			};
			enabledBox.Checked += OnEnabledChanged;
			enabledBox.Unchecked += OnEnabledChanged;
			toolbar.Children.Add(enabledBox);

			pauseBox = new CheckBox
			{
				Content = "Pause",
				Foreground = Brush("#FFEAF0F6"),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 14, 0)
			};
			toolbar.Children.Add(pauseBox);

			Button refreshButton = ToolbarButton("Refresh");
			refreshButton.Click += (s, e) => RefreshRows(true);
			toolbar.Children.Add(refreshButton);

			Button clearButton = ToolbarButton("Clear Counters");
			clearButton.ToolTip = "Start a fresh diagnostics capture and reset rolling Work/s windows.";
			clearButton.Click += (s, e) =>
			{
				OrcaDiagnosticsCore.ClearCounters();
                previousSnapshots.Clear();
				capturePeaks.Clear();
				ResetRollingWork();
				RefreshRows(true);
			};
			toolbar.Children.Add(clearButton);

			Grid.SetRow(toolbar, 0);
			root.Children.Add(toolbar);

			Border dashboard = BuildHealthDashboard();
			Grid.SetRow(dashboard, 1);
			root.Children.Add(dashboard);

			statusText = new TextBlock
			{
				Margin = new Thickness(12, 0, 12, 8),
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 12
			};
			Grid.SetRow(statusText, 2);
			root.Children.Add(statusText);

			DataGrid grid = BuildGrid();
			Grid.SetRow(grid, 3);
			root.Children.Add(grid);

			Content = root;

			refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			refreshTimer.Tick += (s, e) => RefreshRows(false);
			refreshTimer.Start();
			Closed += OnClosed;
			RefreshRows(true);
		}

		public static void ShowOrActivate()
		{
			try
			{
				if (instance == null)
					instance = new OrcaDiagnosticsWindow();

				if (!instance.IsVisible)
					instance.Show();
				instance.Activate();
			}
			catch
			{
				instance = null;
			}
		}

		private void OnClosed(object sender, EventArgs e)
		{
			if (refreshTimer != null)
				refreshTimer.Stop();
			instance = null;
		}

		private void OnEnabledChanged(object sender, RoutedEventArgs e)
		{
			previousSnapshots.Clear();
			ResetRollingWork();
			OrcaDiagnosticsCore.SetEnabled(enabledBox.IsChecked == true);
			RefreshRows(true);
		}

        private void RefreshRows(bool force)
        {
            if (!force && pauseBox.IsChecked == true)
                return;

            DateTime capturedUtc = DateTime.UtcNow;
            List<OrcaDiagnosticsRow> nextRows = new List<OrcaDiagnosticsRow>();
			HashSet<string> activeWorkKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			double workspaceSampleDurationSeconds = 0;
            foreach (OrcaDiagnosticsSnapshot snapshot in OrcaDiagnosticsCore.GetSnapshot())
            {
                SnapshotBaseline baseline = null;
                string key = snapshot == null ? string.Empty : snapshot.InstanceId ?? string.Empty;
                if (!string.IsNullOrEmpty(key))
                    previousSnapshots.TryGetValue(key, out baseline);

                OrcaDiagnosticsRow row = new OrcaDiagnosticsRow(snapshot, baseline == null ? null : baseline.Snapshot, baseline == null ? DateTime.MinValue : baseline.CapturedUtc, capturedUtc);
                if (!string.IsNullOrEmpty(key))
                {
					activeWorkKeys.Add(key);
                    CapturePeak peak;
                    if (!capturePeaks.TryGetValue(key, out peak))
                    {
                        peak = new CapturePeak();
                        capturePeaks[key] = peak;
                    }
                    if (row.WorkMsPerSecond > peak.WorkMsPerSecond)
                    {
                        peak.WorkMsPerSecond = row.WorkMsPerSecond;
                        peak.WorkUtc = capturedUtc;
                    }
                    if (row.LoadScore > peak.LoadScore)
                    {
                        peak.LoadScore = row.LoadScore;
                        peak.LoadUtc = capturedUtc;
                    }
                    row.CapturePeakWorkMsPerSecond = peak.WorkMsPerSecond;
                    row.CapturePeakWorkUtc = peak.WorkUtc;
                    row.CapturePeakLoadScore = peak.LoadScore;
                    row.CapturePeakLoadUtc = peak.LoadUtc;

					WorkWindow workWindow;
					if (!workWindows.TryGetValue(key, out workWindow))
					{
						workWindow = new WorkWindow();
						workWindows[key] = workWindow;
					}
					workWindow.Add(capturedUtc, row.RateIntervalSeconds, row.WorkMsPerSecond);
					row.Work15SecondAverage = workWindow.Average(capturedUtc, 15);
					row.Work60SecondAverage = workWindow.Average(capturedUtc, 60);
					row.Work60SecondPeak = workWindow.Peak(capturedUtc, 60);
					workspaceSampleDurationSeconds = Math.Max(workspaceSampleDurationSeconds, row.RateIntervalSeconds);
                }
                nextRows.Add(row);

                if (!string.IsNullOrEmpty(key))
                    previousSnapshots[key] = new SnapshotBaseline { Snapshot = snapshot, CapturedUtc = capturedUtc };
            }

			List<string> staleWorkKeys = new List<string>();
			foreach (string workKey in workWindows.Keys)
				if (!activeWorkKeys.Contains(workKey))
					staleWorkKeys.Add(workKey);
			foreach (string workKey in staleWorkKeys)
				workWindows.Remove(workKey);

            Dictionary<string, double> instrumentWork = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (OrcaDiagnosticsRow row in nextRows)
            {
                string instrument = string.IsNullOrWhiteSpace(row.Instrument) ? "Unknown" : row.Instrument;
                double total;
                instrumentWork.TryGetValue(instrument, out total);
                instrumentWork[instrument] = total + row.WorkMsPerSecond;
            }
            foreach (OrcaDiagnosticsRow row in nextRows)
            {
                double total;
                if (instrumentWork.TryGetValue(string.IsNullOrWhiteSpace(row.Instrument) ? "Unknown" : row.Instrument, out total))
                    row.InstrumentWorkMsPerSecond = total;
            }

			double workspaceCurrentWork = 0;
			foreach (OrcaDiagnosticsRow row in nextRows)
				workspaceCurrentWork += Math.Max(0, row.WorkMsPerSecond);
			workspaceWorkWindow.Add(capturedUtc, workspaceSampleDurationSeconds, workspaceCurrentWork);

            double captureElapsedSeconds = 0;
            foreach (OrcaDiagnosticsRow row in nextRows)
                if (row.CaptureElapsedSeconds > captureElapsedSeconds)
                    captureElapsedSeconds = row.CaptureElapsedSeconds;

            UpdateHealthDashboard(nextRows, capturedUtc);
            nextRows.Sort(CompareDiagnosticRows);
            rows.Clear();
            foreach (OrcaDiagnosticsRow row in nextRows)
                rows.Add(row);

            statusText.Text = OrcaDiagnosticsCore.Mode.ToString()
                + " | " + rows.Count.ToString(CultureInfo.InvariantCulture) + " instances"
                + " | capture " + FormatCaptureDuration(captureElapsedSeconds)
                + " | " + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            if (enabledBox.IsChecked != OrcaDiagnosticsCore.IsEnabled)
                enabledBox.IsChecked = OrcaDiagnosticsCore.IsEnabled;
        }

		private Border BuildHealthDashboard()
		{
			Border band = new Border
			{
				Margin = new Thickness(12, 0, 12, 8),
				Padding = new Thickness(14, 12, 14, 10),
				Background = Brush("#FF121A23"),
				BorderBrush = Brush("#FF2A3747"),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(4)
			};

			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			Grid metrics = new Grid();
			metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
			metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(175) });
			metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(205) });
			metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(245) });
			metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			StackPanel healthPanel = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
			TextBlock healthLabel = MetricLabel("WORKSPACE HEALTH");
			healthPanel.Children.Add(healthLabel);
			StackPanel healthLine = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 7) };
			healthDot = new Border
			{
				Width = 10,
				Height = 10,
				CornerRadius = new CornerRadius(5),
				Background = Brush("#FF8EA0B5"),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 8, 0)
			};
			healthStateText = new TextBlock
			{
				Text = "OFF",
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 20,
				FontWeight = FontWeights.SemiBold,
				VerticalAlignment = VerticalAlignment.Center
			};
			healthLine.Children.Add(healthDot);
			healthLine.Children.Add(healthStateText);
			healthPanel.Children.Add(healthLine);
			lagHeadroomBar = new ProgressBar
			{
				Minimum = 0,
				Maximum = 100,
				Value = 0,
				Height = 6,
				Foreground = Brush("#FF8EA0B5"),
				Background = Brush("#FF263240"),
				BorderThickness = new Thickness(0),
				ToolTip = "Remaining lag headroom to the 10-second red line. This is not an opaque health percentage."
			};
			healthPanel.Children.Add(lagHeadroomBar);
			lagHeadroomText = new TextBlock
			{
				Text = "Enable Diagnostics",
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 11,
				Margin = new Thickness(0, 5, 0, 0)
			};
			healthPanel.Children.Add(lagHeadroomText);
			Grid.SetColumn(healthPanel, 0);
			metrics.Children.Add(healthPanel);

			StackPanel lagPanel = MetricPanel("LIVE LAG", out lagValueText, out lagDetailText);
			Grid.SetColumn(lagPanel, 1);
			metrics.Children.Add(lagPanel);

			StackPanel workPanel = MetricPanel("ORCA WORK", out workValueText, out workDetailText);
			Grid.SetColumn(workPanel, 2);
			metrics.Children.Add(workPanel);

			StackPanel workspacePanel = MetricPanel("WORKSPACE", out workspaceValueText, out workspaceDetailText);
			Grid.SetColumn(workspacePanel, 3);
			metrics.Children.Add(workspacePanel);

			StackPanel pressurePanel = new StackPanel { Margin = new Thickness(18, 0, 0, 0) };
			pressurePanel.Children.Add(MetricLabel("TOP PRESSURE"));
			topPressureText = new TextBlock
			{
				Text = "No sampled work yet",
				Foreground = Brush("#FFD7E0EA"),
				FontSize = 12,
				LineHeight = 18,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 5, 0, 0)
			};
			pressurePanel.Children.Add(topPressureText);
			Grid.SetColumn(pressurePanel, 4);
			metrics.Children.Add(pressurePanel);

			Grid.SetRow(metrics, 0);
			layout.Children.Add(metrics);

			healthReasonText = new TextBlock
			{
				Text = "Enable Diagnostics to measure workspace health.",
				Foreground = Brush("#FFB8C6D8"),
				FontSize = 12,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 10, 0, 0),
				Padding = new Thickness(0, 8, 0, 0)
			};
			Grid.SetRow(healthReasonText, 1);
			layout.Children.Add(healthReasonText);

			band.Child = layout;
			return band;
		}

		private StackPanel MetricPanel(string label, out TextBlock valueText, out TextBlock detailText)
		{
			StackPanel panel = new StackPanel
			{
				Margin = new Thickness(18, 0, 0, 0)
			};
			panel.Children.Add(MetricLabel(label));
			valueText = new TextBlock
			{
				Text = "--",
				Foreground = Brush("#FFEAF0F6"),
				FontSize = 20,
				FontWeight = FontWeights.SemiBold,
				Margin = new Thickness(0, 5, 0, 0)
			};
			detailText = new TextBlock
			{
				Text = string.Empty,
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 11,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 4, 0, 0)
			};
			panel.Children.Add(valueText);
			panel.Children.Add(detailText);
			return panel;
		}

		private TextBlock MetricLabel(string text)
		{
			return new TextBlock
			{
				Text = text,
				Foreground = Brush("#FF8EA0B5"),
				FontSize = 11,
				FontWeight = FontWeights.SemiBold
			};
		}

		private void ResetRollingWork()
		{
			workWindows.Clear();
			workspaceWorkWindow.Clear();
		}

		private void UpdateHealthDashboard(List<OrcaDiagnosticsRow> currentRows, DateTime capturedUtc)
		{
			if (healthStateText == null)
				return;

			if (!OrcaDiagnosticsCore.IsEnabled)
			{
				ResetLagHealthState();
				ApplyHealthState("OFF", "#FF8EA0B5", 0, "Enable Diagnostics", "Enable Diagnostics to measure workspace health.");
				SetDashboardValues("--", "No live rows", "--", "Budget uncalibrated", "--", "Diagnostics are off", "No sampled work yet");
				return;
			}

			List<OrcaDiagnosticsRow> liveRows = new List<OrcaDiagnosticsRow>();
			Dictionary<string, ChartHealthMetric> chartMetrics = new Dictionary<string, ChartHealthMetric>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> chartNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> instruments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int hiddenTickConsumers = 0;
			int warningRows = 0;
			int actionableWarningRows = 0;
			double totalCurrentWork = 0;
			int openChartTabs = OrcaDiagnosticsAddOn.GetOpenChartTabCount();

			foreach (OrcaDiagnosticsRow row in currentRows)
			{
				if (row == null)
					continue;
				totalCurrentWork += Math.Max(0, row.WorkMsPerSecond);
				if (!string.IsNullOrWhiteSpace(row.ChartName) && !string.Equals(row.ChartName, "Unknown", StringComparison.OrdinalIgnoreCase))
					chartNames.Add(row.ChartName);
				if (!string.IsNullOrWhiteSpace(row.Instrument) && !string.Equals(row.Instrument, "Unknown", StringComparison.OrdinalIgnoreCase))
					instruments.Add(row.Instrument);
				if (!string.IsNullOrWhiteSpace(row.SecondarySeries)
					&& row.SecondarySeries.IndexOf("Tick 1", StringComparison.OrdinalIgnoreCase) >= 0
					&& row.SecondarySeries.IndexOf("source=Component", StringComparison.OrdinalIgnoreCase) >= 0)
					hiddenTickConsumers++;
				if (!string.IsNullOrWhiteSpace(row.Warnings))
					warningRows++;

				if (!string.Equals(row.SourceHealth, "Live", StringComparison.OrdinalIgnoreCase))
					continue;
				liveRows.Add(row);
				if (HasActionableHealthWarning(row.Warnings))
					actionableWarningRows++;

				string chart = string.IsNullOrWhiteSpace(row.ChartName) || string.Equals(row.ChartName, "Unknown", StringComparison.OrdinalIgnoreCase)
					? row.Instrument + " " + row.PrimarySeries
					: row.ChartName;
				string key = chart + "|" + row.Instrument;
				ChartHealthMetric metric;
				if (!chartMetrics.TryGetValue(key, out metric))
				{
					metric = new ChartHealthMetric { Chart = chart, Instrument = row.Instrument };
					chartMetrics[key] = metric;
				}
				if (row.SortLagSeconds > metric.WorstLagSeconds)
					metric.WorstLagSeconds = row.SortLagSeconds;
				if (row.FeedAgeSeconds > metric.FeedAgeSeconds)
					metric.FeedAgeSeconds = row.FeedAgeSeconds;
			}

			int advisoryWarningRows = Math.Max(0, warningRows - actionableWarningRows);
			string workspaceValue = openChartTabs.ToString(CultureInfo.InvariantCulture) + " open / "
				+ chartNames.Count.ToString(CultureInfo.InvariantCulture) + " reporting";
			string workspaceDetail = currentRows.Count.ToString(CultureInfo.InvariantCulture) + " reporting instances | "
				+ instruments.Count.ToString(CultureInfo.InvariantCulture) + " reporting instruments | "
				+ hiddenTickConsumers.ToString(CultureInfo.InvariantCulture) + " hidden Tick 1 | "
				+ actionableWarningRows.ToString(CultureInfo.InvariantCulture) + " actionable | "
				+ advisoryWarningRows.ToString(CultureInfo.InvariantCulture) + " advisory";
			double work15SecondAverage = workspaceWorkWindow.Average(capturedUtc, 15);
			double work60SecondAverage = workspaceWorkWindow.Average(capturedUtc, 60);
			double work60SecondPeak = workspaceWorkWindow.Peak(capturedUtc, 60);
			string workValue = totalCurrentWork.ToString("0.0", CultureInfo.InvariantCulture) + " ms/s now";
			string workDetail = "15s " + work15SecondAverage.ToString("0.0", CultureInfo.InvariantCulture)
				+ " | 60s " + work60SecondAverage.ToString("0.0", CultureInfo.InvariantCulture)
				+ " | peak " + work60SecondPeak.ToString("0.0", CultureInfo.InvariantCulture) + " ms/s";
			string pressure = BuildTopPressureText(currentRows);

			if (liveRows.Count == 0 || chartMetrics.Count == 0)
			{
				ResetLagHealthState();
				string waitingReason = openChartTabs > 0
					? openChartTabs.ToString(CultureInfo.InvariantCulture) + " chart tabs are open, but no Live diagnostics rows are reporting yet."
					: "Diagnostics are on, but no open chart tabs or Live source rows are available yet.";
				ApplyHealthState("WAITING", "#FF8EA0B5", 0, "No live chart lag", waitingReason);
				SetDashboardValues("--", "Waiting for live data", workValue, workDetail, workspaceValue, workspaceDetail, pressure);
				return;
			}

			List<double> chartLags = new List<double>();
			ChartHealthMetric worstChart = null;
			int chartsOverTwoSeconds = 0;
			foreach (ChartHealthMetric metric in chartMetrics.Values)
			{
				chartLags.Add(metric.WorstLagSeconds);
				if (metric.WorstLagSeconds > 2)
					chartsOverTwoSeconds++;
				if (worstChart == null || metric.WorstLagSeconds > worstChart.WorstLagSeconds)
					worstChart = metric;
			}
			chartLags.Sort();
			int p95Index = Math.Max(0, Math.Min(chartLags.Count - 1, (int)Math.Ceiling(chartLags.Count * 0.95) - 1));
			double p95Lag = chartLags[p95Index];
			double worstLag = worstChart == null ? 0 : worstChart.WorstLagSeconds;

			if (worstLag > 2)
				elevatedLagRefreshes++;
			else
				elevatedLagRefreshes = 0;
			if (worstLag > 10)
				severeLagRefreshes++;
			else
				severeLagRefreshes = 0;
			if (!double.IsNaN(previousWorstLiveLag) && worstLag > 2 && worstLag > previousWorstLiveLag + 0.75)
				risingLagRefreshes++;
			else
				risingLagRefreshes = 0;
			previousWorstLiveLag = worstLag;

			bool behind = worstLag > 30 || severeLagRefreshes >= 3 || risingLagRefreshes >= 3;
			bool pressured = !behind && (elevatedLagRefreshes >= 3 || actionableWarningRows > 0);
			bool unknownCoverage = openChartTabs <= 0 || chartNames.Count > openChartTabs;
			bool partialCoverage = unknownCoverage || chartNames.Count < openChartTabs;
			double headroom = Math.Max(0, Math.Min(100, (1.0 - (worstLag / 10.0)) * 100.0));
			string lagValue = FormatLag(worstLag) + " backlog";
			string lagDetail = worstChart.Instrument + " " + worstChart.Chart + " | p95 " + FormatLag(p95Lag)
				+ " | feed age " + FormatLag(worstChart.FeedAgeSeconds)
				+ " | " + chartsOverTwoSeconds.ToString(CultureInfo.InvariantCulture) + " charts >2s";

			string state;
			string color;
			string reason;
			if (behind)
			{
				state = "BEHIND";
				color = "#FFFF6577";
				if (worstLag > 30)
					reason = worstChart.Instrument + " " + worstChart.Chart + " is " + FormatLag(worstLag) + " behind, above the 30-second critical line.";
				else if (severeLagRefreshes >= 3)
					reason = worstChart.Instrument + " " + worstChart.Chart + " has remained above the 10-second red line for " + severeLagRefreshes.ToString(CultureInfo.InvariantCulture) + " refreshes.";
				else
					reason = worstChart.Instrument + " " + worstChart.Chart + " lag has increased for " + risingLagRefreshes.ToString(CultureInfo.InvariantCulture) + " consecutive refreshes.";
			}
			else if (pressured)
			{
				state = "PRESSURED";
				color = "#FFF4C95D";
				if (elevatedLagRefreshes >= 3)
					reason = worstChart.Instrument + " " + worstChart.Chart + " has remained above the 2-second warning line for " + elevatedLagRefreshes.ToString(CultureInfo.InvariantCulture) + " refreshes.";
				else
					reason = actionableWarningRows.ToString(CultureInfo.InvariantCulture) + " live rows report stale data, cache wait, fallback, or excessive render time.";
			}
			else if (partialCoverage)
			{
				state = "PARTIAL";
				color = "#FF67B7DC";
				reason = unknownCoverage
					? "Open-chart coverage is unavailable or inconsistent. Health applies only to reporting charts; workspace health is not confirmed."
					: chartNames.Count.ToString(CultureInfo.InvariantCulture) + " of "
					+ openChartTabs.ToString(CultureInfo.InvariantCulture)
					+ " open chart tabs are represented by diagnostics. Health applies only to reporting charts; reload NinjaScript or restart NinjaTrader to refresh registrations.";
			}
			else
			{
				state = "HEALTHY";
				color = "#FF35C78A";
				if (worstLag > 2)
					reason = "Brief " + FormatLag(worstLag) + " lag excursion on " + worstChart.Instrument + " " + worstChart.Chart + "; confirming " + elevatedLagRefreshes.ToString(CultureInfo.InvariantCulture) + "/3 refreshes.";
				else
					reason = "All " + chartMetrics.Count.ToString(CultureInfo.InvariantCulture) + " live charts are within the 2-second healthy line.";
			}

			ApplyHealthState(state, color, headroom, headroom.ToString("0", CultureInfo.InvariantCulture) + "% lag headroom to 10s", reason);
			lagHeadroomBar.Foreground = Brush(worstLag > 10 ? "#FFFF6577" : worstLag > 2 ? "#FFF4C95D" : color);
			SetDashboardValues(lagValue, lagDetail, workValue, workDetail, workspaceValue, workspaceDetail, pressure);
		}

		private void SetDashboardValues(string lagValue, string lagDetail, string workValue, string workDetail, string workspaceValue, string workspaceDetail, string pressure)
		{
			lagValueText.Text = lagValue;
			lagDetailText.Text = lagDetail;
			workValueText.Text = workValue;
			workDetailText.Text = workDetail;
			workspaceValueText.Text = workspaceValue;
			workspaceDetailText.Text = workspaceDetail;
			topPressureText.Text = pressure;
		}

		private void ApplyHealthState(string state, string color, double headroom, string headroomText, string reason)
		{
			Brush stateBrush = Brush(color);
			healthDot.Background = stateBrush;
			healthStateText.Foreground = stateBrush;
			healthStateText.Text = state;
			lagHeadroomBar.Foreground = stateBrush;
			lagHeadroomBar.Value = headroom;
			lagHeadroomText.Text = headroomText;
			healthReasonText.Text = reason;
		}

		private void ResetLagHealthState()
		{
			elevatedLagRefreshes = 0;
			severeLagRefreshes = 0;
			risingLagRefreshes = 0;
			previousWorstLiveLag = double.NaN;
		}

		private static bool HasActionableHealthWarning(string warnings)
		{
			if (string.IsNullOrWhiteSpace(warnings))
				return false;
			return warnings.IndexOf("StaleData", StringComparison.OrdinalIgnoreCase) >= 0
				|| warnings.IndexOf("CacheWait", StringComparison.OrdinalIgnoreCase) >= 0
				|| warnings.IndexOf("FallbackActive", StringComparison.OrdinalIgnoreCase) >= 0
				|| warnings.IndexOf("ExcessiveRenderTime", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static string BuildTopPressureText(List<OrcaDiagnosticsRow> currentRows)
		{
			List<OrcaDiagnosticsRow> liveRows = new List<OrcaDiagnosticsRow>();
			foreach (OrcaDiagnosticsRow row in currentRows)
				if (row != null
					&& string.Equals(row.SourceHealth, "Live", StringComparison.OrdinalIgnoreCase)
					&& (row.WorkMsPerSecond > 0 || row.Work15SecondAverage > 0 || row.Work60SecondAverage > 0))
					liveRows.Add(row);
			if (liveRows.Count == 0)
				return "No sampled work yet";

			List<OrcaDiagnosticsRow> nowRanked = new List<OrcaDiagnosticsRow>(liveRows);
			nowRanked.Sort(CompareTopNowRows);
			List<OrcaDiagnosticsRow> sustainedRanked = new List<OrcaDiagnosticsRow>(liveRows);
			sustainedRanked.Sort((left, right) => right.Work60SecondAverage.CompareTo(left.Work60SecondAverage));

			List<string> lines = new List<string>();
			lines.Add("NOW (1s / 15s)");
			for (int index = 0; index < Math.Min(2, nowRanked.Count); index++)
			{
				OrcaDiagnosticsRow row = nowRanked[index];
				lines.Add((index + 1).ToString(CultureInfo.InvariantCulture) + ". " + row.ModuleName + " | "
					+ row.Instrument + " " + row.PrimarySeries + " | "
					+ row.WorkMsPerSecond.ToString("0.0", CultureInfo.InvariantCulture) + " / "
					+ row.Work15SecondAverage.ToString("0.0", CultureInfo.InvariantCulture) + " ms/s");
			}
			lines.Add("SUSTAINED (60s / peak)");
			for (int index = 0; index < Math.Min(2, sustainedRanked.Count); index++)
			{
				OrcaDiagnosticsRow row = sustainedRanked[index];
				lines.Add((index + 1).ToString(CultureInfo.InvariantCulture) + ". " + row.ModuleName + " | "
					+ row.Instrument + " " + row.PrimarySeries + " | "
					+ row.Work60SecondAverage.ToString("0.0", CultureInfo.InvariantCulture) + " / "
					+ row.Work60SecondPeak.ToString("0.0", CultureInfo.InvariantCulture) + " ms/s");
			}
			return string.Join("\n", lines.ToArray());
		}

		private static int CompareTopNowRows(OrcaDiagnosticsRow left, OrcaDiagnosticsRow right)
		{
			if (left == null)
				return right == null ? 0 : 1;
			if (right == null)
				return -1;
			int currentCompare = right.WorkMsPerSecond.CompareTo(left.WorkMsPerSecond);
			if (currentCompare != 0)
				return currentCompare;
			int rollingCompare = right.Work15SecondAverage.CompareTo(left.Work15SecondAverage);
			if (rollingCompare != 0)
				return rollingCompare;
			return string.Compare(left.ModuleName, right.ModuleName, StringComparison.OrdinalIgnoreCase);
		}

		private static string FormatLag(double seconds)
		{
			if (seconds >= 3600)
				return (seconds / 3600.0).ToString("0.0", CultureInfo.InvariantCulture) + "h";
			if (seconds >= 60)
				return (seconds / 60.0).ToString("0.0", CultureInfo.InvariantCulture) + "m";
			return seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
		}

        private static int CompareDiagnosticRows(OrcaDiagnosticsRow left, OrcaDiagnosticsRow right)
        {
            if (left == null && right == null)
                return 0;
            if (left == null)
                return 1;
            if (right == null)
                return -1;

            bool lagging = left.SortLagSeconds > 30 || right.SortLagSeconds > 30;
            if (lagging)
            {
                int lagCompare = right.SortLagSeconds.CompareTo(left.SortLagSeconds);
                if (lagCompare != 0)
                    return lagCompare;
            }

            int workCompare = right.WorkMsPerSecond.CompareTo(left.WorkMsPerSecond);
            int captureCompare = right.CaptureAverageWorkMsPerSecond.CompareTo(left.CaptureAverageWorkMsPerSecond);
            if (captureCompare != 0)
                return captureCompare;

            if (workCompare != 0)
                return workCompare;

            int loadCompare = right.LoadScore.CompareTo(left.LoadScore);
            if (loadCompare != 0)
                return loadCompare;

            int renderCompare = right.RenderMaxMs.CompareTo(left.RenderMaxMs);
            if (renderCompare != 0)
                return renderCompare;

            int instrumentCompare = string.Compare(left.Instrument, right.Instrument, StringComparison.OrdinalIgnoreCase);
            if (instrumentCompare != 0)
                return instrumentCompare;
            return string.Compare(left.ModuleName, right.ModuleName, StringComparison.OrdinalIgnoreCase);
        }

		private DataGrid BuildGrid()
		{
			DataGrid grid = new DataGrid
			{
				AutoGenerateColumns = false,
				CanUserAddRows = false,
				CanUserDeleteRows = false,
				IsReadOnly = true,
				ItemsSource = rows,
				GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
				HeadersVisibility = DataGridHeadersVisibility.Column,
				RowHeaderWidth = 0,
				Margin = new Thickness(12, 0, 12, 12),
				Background = Brush("#FF121A23"),
				Foreground = Brush("#FFEAF0F6"),
				BorderBrush = Brush("#FF2A3747"),
				HorizontalGridLinesBrush = Brush("#FF202A36"),
				VerticalGridLinesBrush = Brush("#FF202A36"),
				AlternatingRowBackground = Brush("#FF101820"),
				RowBackground = Brush("#FF121A23"),
				ColumnHeaderStyle = BuildHeaderStyle(),
				EnableRowVirtualization = true,
				EnableColumnVirtualization = true
			};

			grid.Columns.Add(TextColumn("Health", "SourceHealth", 120));
			grid.Columns.Add(TextColumn("Warnings", "Warnings", 210));
			grid.Columns.Add(TextColumn("Module", "ModuleName", 170));
			grid.Columns.Add(TextColumn("Instrument", "Instrument", 120));
			grid.Columns.Add(TextColumn("Chart", "ChartName", 110));
			grid.Columns.Add(TextColumn("Primary", "PrimarySeries", 110));
			grid.Columns.Add(TextColumn("Hours", "TradingHours", 150));
			grid.Columns.Add(TextColumn("Replay", "TickReplayState", 90));
			grid.Columns.Add(TextColumn("Source", "SourceMode", 190));
			grid.Columns.Add(TextColumn("Series", "SecondarySeries", 270));
			grid.Columns.Add(TextColumn("Last Input", "LastInputText", 110));
			grid.Columns.Add(TextColumn("Backlog / feed age", "LagText", 170));
            grid.Columns.Add(TextColumn("Cluster", "LagCluster", 280));
            grid.Columns.Add(TextColumn("Load/s", "LoadText", 170));
            grid.Columns.Add(TextColumn("Work/s", "WorkText", 300));
            grid.Columns.Add(TextColumn("Phases", "PhasesText", 360));
			grid.Columns.Add(TextColumn("MD L/B/A", "MarketDataText", 140));
			grid.Columns.Add(TextColumn("Bars", "BarUpdateCounts", 150));
            grid.Columns.Add(TextColumn("Capture", "CaptureText", 360));
			grid.Columns.Add(TextColumn("Cache", "CacheText", 180));
			grid.Columns.Add(TextColumn("Model", "ModelText", 110));
			grid.Columns.Add(TextColumn("Render", "RenderText", 140));
			grid.Columns.Add(TextColumn("ID", "ShortInstanceId", 90));
			return grid;
		}

		private static string FormatCaptureDuration(double seconds)
		{
			if (seconds <= 0)
				return "0s";

			TimeSpan duration = TimeSpan.FromSeconds(seconds);
			if (duration.TotalHours >= 1)
				return ((int)duration.TotalHours).ToString(CultureInfo.InvariantCulture) + "h " + duration.Minutes.ToString(CultureInfo.InvariantCulture) + "m";
			if (duration.TotalMinutes >= 1)
				return ((int)duration.TotalMinutes).ToString(CultureInfo.InvariantCulture) + "m " + duration.Seconds.ToString(CultureInfo.InvariantCulture) + "s";
			return Math.Max(0, duration.Seconds).ToString(CultureInfo.InvariantCulture) + "s";
		}

		private Button ToolbarButton(string text)
		{
			return new Button
			{
				Content = text,
				MinWidth = 86,
				Height = 28,
				Margin = new Thickness(0, 0, 8, 0),
				Padding = new Thickness(10, 3, 10, 3)
			};
		}

		private DataGridTextColumn TextColumn(string header, string path, double width)
		{
			return new DataGridTextColumn
			{
				Header = header,
				Binding = new Binding(path),
				Width = new DataGridLength(width),
				ElementStyle = BuildCellTextStyle()
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

		private static Brush Brush(string hex)
		{
			return (Brush)new BrushConverter().ConvertFrom(hex);
		}
	}

    public sealed class OrcaDiagnosticsRow
    {
        private readonly OrcaDiagnosticsSnapshot snapshot;
        private readonly double eventRate;
        private readonly double marketDataRate;
        private readonly double barUpdateRate;
        private readonly double workMsPerSecond;
        private readonly double renderRate;
        private readonly double loadScore;
		private readonly double rateIntervalSeconds;

        public OrcaDiagnosticsRow(OrcaDiagnosticsSnapshot snapshot, OrcaDiagnosticsSnapshot previousSnapshot, DateTime previousCapturedUtc, DateTime capturedUtc)
        {
            this.snapshot = snapshot ?? new OrcaDiagnosticsSnapshot();
			rateIntervalSeconds = previousSnapshot == null || previousCapturedUtc == DateTime.MinValue || capturedUtc <= previousCapturedUtc
				? 0
				: Math.Max(0, (capturedUtc - previousCapturedUtc).TotalSeconds);
            CalculateRates(previousSnapshot, previousCapturedUtc, capturedUtc, out eventRate, out marketDataRate, out barUpdateRate, out renderRate, out loadScore);
            workMsPerSecond = (marketDataRate * this.snapshot.WorkMarketDataAverageMs) + (barUpdateRate * this.snapshot.WorkBarUpdateAverageMs);
        }

        public string ShortInstanceId
        {
            get
            {
                string id = snapshot.InstanceId ?? string.Empty;
                return id.Length <= 8 ? id : id.Substring(0, 8);
            }
        }

        public string ModuleName { get { return snapshot.ModuleName; } }
        public string SourceHealth { get { return snapshot.SourceHealth; } }
        public string Warnings { get { return snapshot.Warnings; } }
        public string Instrument { get { return snapshot.Instrument; } }
        public string ChartName { get { return snapshot.ChartName; } }
        public string PrimarySeries { get { return snapshot.PrimarySeries; } }
        public string TradingHours { get { return snapshot.TradingHours; } }
        public string TickReplayState { get { return snapshot.TickReplayState; } }
        public string SourceMode { get { return snapshot.SourceMode; } }
        public string SecondarySeries { get { return snapshot.SecondarySeries; } }
        public string BarUpdateCounts { get { return snapshot.BarUpdateCounts; } }
        public string LagCluster { get { return snapshot.LagCluster; } }
        public double SortLagSeconds { get { return snapshot.LagSeconds; } }
		public double FeedAgeSeconds { get { return snapshot.FeedAgeSeconds; } }
        public double LoadScore { get { return loadScore; } }
        public double RenderMaxMs { get { return snapshot.RenderMaxMs; } }
        public double WorkMsPerSecond { get { return workMsPerSecond; } }
		public double RateIntervalSeconds { get { return rateIntervalSeconds <= 0.1 ? 0 : rateIntervalSeconds; } }
		public double Work15SecondAverage { get; set; }
		public double Work60SecondAverage { get; set; }
		public double Work60SecondPeak { get; set; }
        public double InstrumentWorkMsPerSecond { get; set; }
        public double CaptureElapsedSeconds { get { return snapshot.CaptureElapsedSeconds; } }
        public double CaptureAverageWorkMsPerSecond { get { return snapshot.CaptureAverageWorkMsPerSecond; } }
        public double CaptureAverageLoadScore { get { return snapshot.CaptureAverageLoadScore; } }
        public double CapturePeakWorkMsPerSecond { get; set; }
        public DateTime CapturePeakWorkUtc { get; set; }
        public double CapturePeakLoadScore { get; set; }
        public DateTime CapturePeakLoadUtc { get; set; }


        public string LastInputText
        {
            get { return FormatTime(snapshot.LastInputEventTime); }
        }

        public string LagText
        {
            get
            {
                if (snapshot.LastInputEventTime == DateTime.MinValue && snapshot.LastInputUtc == DateTime.MinValue)
                    return string.Empty;
				return "backlog " + FormatDuration(snapshot.LagSeconds) + " | feed " + FormatDuration(snapshot.FeedAgeSeconds);
			}
		}

		private static string FormatDuration(double seconds)
		{
			if (seconds >= 3600)
				return (seconds / 3600.0).ToString("0.0", CultureInfo.InvariantCulture) + "h";
			if (seconds >= 60)
				return (seconds / 60.0).ToString("0.0", CultureInfo.InvariantCulture) + "m";
			return seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
		}

        public string LoadText
        {
            get
            {
                if (loadScore <= 0)
                    return string.Empty;
                return "score " + loadScore.ToString("0", CultureInfo.InvariantCulture)
                    + " ev/s " + eventRate.ToString("0", CultureInfo.InvariantCulture)
                    + " rnd/s " + renderRate.ToString("0.0", CultureInfo.InvariantCulture);
            }
        }

        public string WorkText
        {
            get
            {
                if (snapshot.WorkMarketDataSampleCount <= 0 && snapshot.WorkBarUpdateSampleCount <= 0)
                    return string.Empty;

                double maxMs = Math.Max(snapshot.WorkMarketDataMaxMs, snapshot.WorkBarUpdateMaxMs);
                string text = "est " + workMsPerSecond.ToString("0.0", CultureInfo.InvariantCulture) + " ms/s";
                if (snapshot.WorkMarketDataSampleCount > 0)
                    text += " md " + snapshot.WorkMarketDataAverageMs.ToString("0.000", CultureInfo.InvariantCulture);
                if (snapshot.WorkBarUpdateSampleCount > 0)
                    text += " bar " + snapshot.WorkBarUpdateAverageMs.ToString("0.000", CultureInfo.InvariantCulture);
                text += " max " + maxMs.ToString("0.00", CultureInfo.InvariantCulture);
                if (InstrumentWorkMsPerSecond > 0)
                    text += " inst " + InstrumentWorkMsPerSecond.ToString("0.0", CultureInfo.InvariantCulture);
                text += " n " + (snapshot.WorkMarketDataSampleCount + snapshot.WorkBarUpdateSampleCount).ToString(CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(snapshot.WorkBarUpdateByBip))
                    text += " " + snapshot.WorkBarUpdateByBip;
                return text;
            }
        }

        public string PhasesText { get { return snapshot.WorkPhases; } }

        public string CaptureText
        {
            get
            {
                if (snapshot.CaptureElapsedSeconds < 1)
                    return string.Empty;

                string text = FormatCaptureDuration(snapshot.CaptureElapsedSeconds)
                    + " work avg " + snapshot.CaptureAverageWorkMsPerSecond.ToString("0.0", CultureInfo.InvariantCulture)
                    + " obs peak " + CapturePeakWorkMsPerSecond.ToString("0.0", CultureInfo.InvariantCulture);
                if (CapturePeakWorkUtc != DateTime.MinValue)
                    text += "@" + FormatTime(CapturePeakWorkUtc);
                text += " load avg " + snapshot.CaptureAverageLoadScore.ToString("0", CultureInfo.InvariantCulture)
                    + " obs peak " + CapturePeakLoadScore.ToString("0", CultureInfo.InvariantCulture);
                if (CapturePeakLoadUtc != DateTime.MinValue)
                    text += "@" + FormatTime(CapturePeakLoadUtc);
                text += " ev/s " + snapshot.CaptureAverageEventsPerSecond.ToString("0", CultureInfo.InvariantCulture)
                    + " cb max " + snapshot.CaptureWorkMaxMs.ToString("0.00", CultureInfo.InvariantCulture)
                    + " n " + snapshot.CaptureWorkSampleCount.ToString(CultureInfo.InvariantCulture);
                return text;
            }
        }

        public string MarketDataText
        {
            get
            {
                return snapshot.MarketDataLastCount.ToString(CultureInfo.InvariantCulture)
                    + "/"
                    + snapshot.MarketDataBidCount.ToString(CultureInfo.InvariantCulture)
                    + "/"
                    + snapshot.MarketDataAskCount.ToString(CultureInfo.InvariantCulture);
            }
        }

        public string CacheText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(snapshot.CacheProvider))
                    return snapshot.CacheStatus;
                if (string.IsNullOrWhiteSpace(snapshot.CacheStatus))
                    return snapshot.CacheProvider;
                return snapshot.CacheProvider + " " + snapshot.CacheStatus;
            }
        }

        public string ModelText
        {
            get { return FormatTime(snapshot.LastModelUpdateUtc); }
        }

        public string RenderText
        {
            get
            {
                if (snapshot.RenderSampleCount <= 0)
                    return string.Empty;
                return "5s avg " + snapshot.RenderAverageMs.ToString("0.00", CultureInfo.InvariantCulture)
                    + " max " + snapshot.RenderMaxMs.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        private void CalculateRates(OrcaDiagnosticsSnapshot previousSnapshot, DateTime previousCapturedUtc, DateTime capturedUtc, out double eventsPerSecond, out double marketEventsPerSecond, out double barEventsPerSecond, out double rendersPerSecond, out double score)
        {
            eventsPerSecond = 0;
            marketEventsPerSecond = 0;
            barEventsPerSecond = 0;
            rendersPerSecond = 0;
            score = 0;
            if (previousSnapshot == null || previousCapturedUtc == DateTime.MinValue || capturedUtc <= previousCapturedUtc)
                return;

            double seconds = (capturedUtc - previousCapturedUtc).TotalSeconds;
            if (seconds <= 0.1)
                return;

            long marketDelta = Math.Max(0, TotalMarketDataCount(snapshot) - TotalMarketDataCount(previousSnapshot));
            long barDelta = Math.Max(0, TotalBarUpdateCount(snapshot.BarUpdateCounts) - TotalBarUpdateCount(previousSnapshot.BarUpdateCounts));
            long renderDelta = Math.Max(0, snapshot.RenderCount - previousSnapshot.RenderCount);

            marketEventsPerSecond = marketDelta / seconds;
            barEventsPerSecond = barDelta / seconds;
            eventsPerSecond = marketEventsPerSecond + barEventsPerSecond;
            rendersPerSecond = renderDelta / seconds;
            score = eventsPerSecond + (rendersPerSecond * 5.0) + (snapshot.RenderAverageMs * rendersPerSecond);
        }

        private static long TotalMarketDataCount(OrcaDiagnosticsSnapshot value)
        {
            if (value == null)
                return 0;
            return value.MarketDataLastCount + value.MarketDataBidCount + value.MarketDataAskCount;
        }

        private static long TotalBarUpdateCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            long total = 0;
            string[] parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                int eq = part.IndexOf('=');
                if (eq < 0 || eq >= part.Length - 1)
                    continue;
                long value;
                if (long.TryParse(part.Substring(eq + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                    total += value;
            }
            return total;
        }

        private static string FormatCaptureDuration(double seconds)
        {
            if (seconds <= 0)
                return "0s";

            TimeSpan duration = TimeSpan.FromSeconds(seconds);
            if (duration.TotalHours >= 1)
                return ((int)duration.TotalHours).ToString(CultureInfo.InvariantCulture) + "h" + duration.Minutes.ToString("00", CultureInfo.InvariantCulture) + "m";
            if (duration.TotalMinutes >= 1)
                return ((int)duration.TotalMinutes).ToString(CultureInfo.InvariantCulture) + "m" + duration.Seconds.ToString("00", CultureInfo.InvariantCulture) + "s";
            return duration.Seconds.ToString(CultureInfo.InvariantCulture) + "s";
        }

        private static string FormatTime(DateTime value)
        {
            if (value == DateTime.MinValue)
                return string.Empty;
            return value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }
    }
}
