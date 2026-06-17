using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using NinjaTrader.Core;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using System.Collections.Concurrent;

namespace OrcaTradeCopier
{
    /// <summary>
    /// UI Helper structure representing the follower status bound directly to the WPF DataGrid.
    /// </summary>
    public class FollowerGridItem : DependencyObject
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled", typeof(bool), typeof(FollowerGridItem));
        public static readonly DependencyProperty AccountNameProperty = DependencyProperty.Register("AccountName", typeof(string), typeof(FollowerGridItem));
        public static readonly DependencyProperty MultiplierProperty = DependencyProperty.Register("Multiplier", typeof(double), typeof(FollowerGridItem));
        public static readonly DependencyProperty FixedQtyProperty = DependencyProperty.Register("FixedQty", typeof(int), typeof(FollowerGridItem));
        public static readonly DependencyProperty LatencyProperty = DependencyProperty.Register("Latency", typeof(string), typeof(FollowerGridItem));
        public static readonly DependencyProperty SlippageProperty = DependencyProperty.Register("Slippage", typeof(string), typeof(FollowerGridItem));
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(string), typeof(FollowerGridItem));
        public static readonly DependencyProperty StatusBrushProperty = DependencyProperty.Register("StatusBrush", typeof(Brush), typeof(FollowerGridItem));

        public bool IsEnabled { get { return (bool)GetValue(IsEnabledProperty); } set { SetValue(IsEnabledProperty, value); } }
        public string AccountName { get { return (string)GetValue(AccountNameProperty); } set { SetValue(AccountNameProperty, value); } }
        public double Multiplier { get { return (double)GetValue(MultiplierProperty); } set { SetValue(MultiplierProperty, value); } }
        public int FixedQty { get { return (int)GetValue(FixedQtyProperty); } set { SetValue(FixedQtyProperty, value); } }
        public string Latency { get { return (string)GetValue(LatencyProperty); } set { SetValue(LatencyProperty, value); } }
        public string Slippage { get { return (string)GetValue(SlippageProperty); } set { SetValue(SlippageProperty, value); } }
        public string Status { get { return (string)GetValue(StatusProperty); } set { SetValue(StatusProperty, value); } }
        public Brush StatusBrush { get { return (Brush)GetValue(StatusBrushProperty); } set { SetValue(StatusBrushProperty, value); } }
    }

    /// <summary>
    /// Premium WPF window class presenting the dashboard control center for OrcaTradeCopier.
    /// Natively inherits from NTWindow for full integration into NT8 workspace saving and docks.
    /// </summary>
    public class OrcaTradeCopierWindow : NTWindow
    {
        private OrcaTradeCopierEngine _engine;
        private OrcaTradeCopierNetwork _network;
        private DispatcherTimer _refreshTimer;

        // UI Controls
        private ComboBox _cmbLeaderAccount;
        private ComboBox _cmbCopyMethod;
        private Button _btnArmCopier;
        private Button _btnNetworkConnect;
        private TextBox _txtNetworkIp;
        private TextBox _txtNetworkPort;
        private ComboBox _cmbNetworkMode;
        private DataGrid _gridFollowers;
        private TextBox _txtLogConsole;

        private readonly ObservableCollection<FollowerGridItem> _gridItems = new ObservableCollection<FollowerGridItem>();

        public OrcaTradeCopierWindow()
        {
            Caption = "Orca Trade Copier Station";
            Width = 900;
            Height = 600;
            MinWidth = 800;
            MinHeight = 500;

            // Instantiate architecture backend
            _network = new OrcaTradeCopierNetwork();
            _engine = new OrcaTradeCopierEngine(_network);

            _engine.OnLogMessage += AppendLog;
            _engine.OnStateUpdated += UpdateUiState;
            _network.OnLogMessage += AppendLog;
            _network.OnConnectionStatusChanged += HandleNetworkStatusChange;

            BuildWindowLayout();
            PopulateAccounts();
            StartMonitoring();

            AppendLog("[Orca Trade Copier] System ready. Configure leader and follower accounts and press ARM.");
        }

        private void BuildWindowLayout()
        {
            // Dark premium palette
            Background = new SolidColorBrush(Color.FromRgb(24, 25, 29));
            Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));

            // Main layout Grid
            Grid mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) }); // Control Panel
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // View Area
            Content = mainGrid;

            // ==========================================
            // CONTROL PANEL (LEFT)
            // ==========================================
            Border controlBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 42, 48)),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Background = new SolidColorBrush(Color.FromRgb(30, 31, 36)),
                Padding = new Thickness(15)
            };
            Grid.SetColumn(controlBorder, 0);
            mainGrid.Children.Add(controlBorder);

            StackPanel ctrlPanel = new StackPanel();
            controlBorder.Child = ctrlPanel;

            // Heading
            TextBlock heading = new TextBlock
            {
                Text = "ORCA COPIER",
                FontFamily = new FontFamily("Impact"),
                FontSize = 26,
                Foreground = new LinearGradientBrush(Color.FromRgb(0, 200, 255), Color.FromRgb(0, 100, 255), 45),
                Margin = new Thickness(0, 0, 0, 15)
            };
            ctrlPanel.Children.Add(heading);

            // Leader Account Selector
            ctrlPanel.Children.Add(CreateLabel("LEADER ACCOUNT"));
            _cmbLeaderAccount = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 47, 54)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 63, 72)),
                Height = 30,
                Margin = new Thickness(0, 0, 0, 12)
            };
            ctrlPanel.Children.Add(_cmbLeaderAccount);

            // Copying Method
            ctrlPanel.Children.Add(CreateLabel("COPYING METHOD"));
            _cmbCopyMethod = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 47, 54)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 63, 72)),
                Height = 30,
                Margin = new Thickness(0, 0, 0, 15)
            };
            _cmbCopyMethod.Items.Add("Exact Quantity");
            _cmbCopyMethod.Items.Add("Size Multiplier");
            _cmbCopyMethod.Items.Add("Fixed Quantity");
            _cmbCopyMethod.SelectedIndex = 0;
            _cmbCopyMethod.SelectionChanged += (s, e) => ChangeCopyMethod();
            ctrlPanel.Children.Add(_cmbCopyMethod);

            // Action Buttons
            _btnArmCopier = new Button
            {
                Content = "ARM COPIER",
                FontWeight = FontWeights.Bold,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(0, 150, 255)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 10)
            };
            _btnArmCopier.Click += ArmCopier_Click;
            ctrlPanel.Children.Add(_btnArmCopier);

            Button btnFlattenAll = new Button
            {
                Content = "EMERGENCY FLATTEN ALL",
                FontWeight = FontWeights.Bold,
                Height = 35,
                Background = new LinearGradientBrush(Color.FromRgb(255, 60, 60), Color.FromRgb(200, 30, 30), 45),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 10)
            };
            btnFlattenAll.Click += (s, e) => _engine.FlattenAllFollowers();
            ctrlPanel.Children.Add(btnFlattenAll);

            Button btnRearm = new Button
            {
                Content = "REARM SYSTEM",
                Height = 28,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 120)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 200, 120)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 20)
            };
            btnRearm.Click += (s, e) => _engine.RearmAllFollowers();
            ctrlPanel.Children.Add(btnRearm);

            // Network Section Separator
            Border sep = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 42, 48)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 0, 0, 15)
            };
            ctrlPanel.Children.Add(sep);

            // Network Configurations
            ctrlPanel.Children.Add(CreateLabel("NETWORK NODE BROADCAST"));
            _cmbNetworkMode = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 47, 54)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 63, 72)),
                Height = 26,
                Margin = new Thickness(0, 0, 0, 10)
            };
            _cmbNetworkMode.Items.Add("Disable Network");
            _cmbNetworkMode.Items.Add("Server (LAN Broadcast)");
            _cmbNetworkMode.Items.Add("Client (WAN Follower)");
            _cmbNetworkMode.SelectedIndex = 0;
            _cmbNetworkMode.SelectionChanged += NetworkMode_SelectionChanged;
            ctrlPanel.Children.Add(_cmbNetworkMode);

            Grid netSettingsGrid = new Grid();
            netSettingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            netSettingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _txtNetworkIp = new TextBox
            {
                Text = "127.0.0.1",
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 24)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 63, 72)),
                Margin = new Thickness(0, 0, 5, 0),
                Height = 24
            };
            Grid.SetColumn(_txtNetworkIp, 0);
            netSettingsGrid.Children.Add(_txtNetworkIp);

            _txtNetworkPort = new TextBox
            {
                Text = "4547",
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 24)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 63, 72)),
                Height = 24
            };
            Grid.SetColumn(_txtNetworkPort, 1);
            netSettingsGrid.Children.Add(_txtNetworkPort);
            ctrlPanel.Children.Add(netSettingsGrid);

            _btnNetworkConnect = new Button
            {
                Content = "START NODE",
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(40, 42, 48)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 10, 0, 0),
                IsEnabled = false
            };
            _btnNetworkConnect.Click += NetworkConnect_Click;
            ctrlPanel.Children.Add(_btnNetworkConnect);

            // ==========================================
            // FOLLOWER VIEW AREA (RIGHT)
            // ==========================================
            Grid rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) }); // Follower List
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Console Logs
            Grid.SetColumn(rightGrid, 1);
            mainGrid.Children.Add(rightGrid);

            // Data Grid border and container
            Border gridBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 21, 25)),
                Padding = new Thickness(10)
            };
            Grid.SetRow(gridBorder, 0);
            rightGrid.Children.Add(gridBorder);

            _gridFollowers = new DataGrid
            {
                ItemsSource = _gridItems,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                RowBackground = Brushes.Transparent,
                AlternatingRowBackground = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255)),
                Foreground = Brushes.White,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(35, 37, 43))
            };

            // DataGrid Columns Setup
            var checkColumn = new DataGridCheckBoxColumn
            {
                Header = "Active",
                Binding = new Binding("IsEnabled") { Mode = BindingMode.TwoWay },
                Width = new DataGridLength(60)
            };
            _gridFollowers.Columns.Add(checkColumn);

            var accountColumn = new DataGridTextColumn
            {
                Header = "Account Name",
                Binding = new Binding("AccountName"),
                IsReadOnly = true,
                Width = new DataGridLength(150)
            };
            _gridFollowers.Columns.Add(accountColumn);

            var multColumn = new DataGridTextColumn
            {
                Header = "Multiplier",
                Binding = new Binding("Multiplier") { Mode = BindingMode.TwoWay },
                Width = new DataGridLength(80)
            };
            _gridFollowers.Columns.Add(multColumn);

            var fixedQtyColumn = new DataGridTextColumn
            {
                Header = "Fixed Qty",
                Binding = new Binding("FixedQty") { Mode = BindingMode.TwoWay },
                Width = new DataGridLength(80)
            };
            _gridFollowers.Columns.Add(fixedQtyColumn);

            var latencyColumn = new DataGridTextColumn
            {
                Header = "Latency (ms)",
                Binding = new Binding("Latency"),
                IsReadOnly = true,
                Width = new DataGridLength(100)
            };
            _gridFollowers.Columns.Add(latencyColumn);

            var slippageColumn = new DataGridTextColumn
            {
                Header = "Slippage (Ticks)",
                Binding = new Binding("Slippage"),
                IsReadOnly = true,
                Width = new DataGridLength(110)
            };
            _gridFollowers.Columns.Add(slippageColumn);

            // Colored Status Row
            var statusColumn = new DataGridTemplateColumn
            {
                Header = "Safety Status",
                Width = new DataGridLength(120)
            };
            var statusFactory = new FrameworkElementFactory(typeof(TextBlock));
            statusFactory.SetBinding(TextBlock.TextProperty, new Binding("Status"));
            statusFactory.SetBinding(TextBlock.ForegroundProperty, new Binding("StatusBrush"));
            statusFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            statusColumn.CellTemplate = new DataTemplate { VisualTree = statusFactory };
            _gridFollowers.Columns.Add(statusColumn);

            gridBorder.Child = _gridFollowers;

            // Console Log Panel (Bottom Right)
            Border consoleBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 42, 48)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(15, 16, 19)),
                Padding = new Thickness(10)
            };
            Grid.SetRow(consoleBorder, 1);
            rightGrid.Children.Add(consoleBorder);

            _txtLogConsole = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 190, 100)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                BorderThickness = new Thickness(0),
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap
            };
            consoleBorder.Child = _txtLogConsole;
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 135, 150)),
                Margin = new Thickness(0, 0, 0, 4)
            };
        }

        private void PopulateAccounts()
        {
            _cmbLeaderAccount.Items.Clear();
            _gridItems.Clear();

            lock (NinjaTrader.Cbi.Account.All)
            {
                foreach (var account in NinjaTrader.Cbi.Account.All)
                {
                    _cmbLeaderAccount.Items.Add(account.Name);

                    // Add everything else to follower collection as grid items
                    _gridItems.Add(new FollowerGridItem
                    {
                        IsEnabled = false,
                        AccountName = account.Name,
                        Multiplier = 1.0,
                        FixedQty = 1,
                        Latency = "--",
                        Slippage = "--",
                        Status = "Ready",
                        StatusBrush = new SolidColorBrush(Color.FromRgb(0, 180, 255))
                    });
                }
            }

            if (_cmbLeaderAccount.Items.Count > 0)
            {
                _cmbLeaderAccount.SelectedIndex = 0;
            }
        }

        private void ArmCopier_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.IsCopyingActive)
            {
                _engine.StopCopying();
            }
            else
            {
                string leaderName = _cmbLeaderAccount.SelectedItem as string;
                if (string.IsNullOrEmpty(leaderName)) return;

                var leaderAcc = NinjaTrader.Cbi.Account.All.FirstOrDefault(a => a.Name == leaderName);
                if (leaderAcc == null) return;

                var followersList = new List<FollowerAccountSettings>();
                foreach (var item in _gridItems)
                {
                    // Do not allow leader to follow itself
                    if (item.AccountName == leaderName) continue;

                    followersList.Add(new FollowerAccountSettings
                    {
                        AccountName = item.AccountName,
                        IsEnabled = item.IsEnabled,
                        Multiplier = item.Multiplier,
                        FixedQty = item.FixedQty,
                        CopyAtm = true
                    });
                }

                _engine.Initialize(leaderAcc, followersList);
                _engine.StartCopying();
            }
        }

        private void UpdateUiState()
        {
            Dispatcher.Invoke(() =>
            {
                if (_engine.IsCopyingActive)
                {
                    _btnArmCopier.Content = "DISARM COPIER";
                    _btnArmCopier.Background = new SolidColorBrush(Color.FromRgb(200, 30, 30));
                    _cmbLeaderAccount.IsEnabled = false;
                }
                else
                {
                    _btnArmCopier.Content = "ARM COPIER";
                    _btnArmCopier.Background = new SolidColorBrush(Color.FromRgb(0, 150, 255));
                    _cmbLeaderAccount.IsEnabled = true;
                }
            });
        }

        private void ChangeCopyMethod()
        {
            if (_cmbCopyMethod.SelectedIndex == 0) _engine.DefaultCopyMethod = CopyMethod.ExactQuantity;
            else if (_cmbCopyMethod.SelectedIndex == 1) _engine.DefaultCopyMethod = CopyMethod.Multiplier;
            else if (_cmbCopyMethod.SelectedIndex == 2) _engine.DefaultCopyMethod = CopyMethod.FixedQuantity;
        }

        private void NetworkMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cmbNetworkMode.SelectedIndex == 0)
            {
                _btnNetworkConnect.IsEnabled = false;
                _btnNetworkConnect.Content = "START NODE";
                _network.Stop();
            }
            else
            {
                _btnNetworkConnect.IsEnabled = true;
                _btnNetworkConnect.Content = _cmbNetworkMode.SelectedIndex == 1 ? "START SERVER" : "CONNECT TO LEADER";
            }
        }

        private void NetworkConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_network.IsRunning)
            {
                _network.Stop();
            }
            else
            {
                int port = int.Parse(_txtNetworkPort.Text);
                if (_cmbNetworkMode.SelectedIndex == 1)
                {
                    _network.StartServer(port);
                }
                else if (_cmbNetworkMode.SelectedIndex == 2)
                {
                    _network.StartClient(_txtNetworkIp.Text, port);
                }
            }
        }

        private void HandleNetworkStatusChange(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                if (connected)
                {
                    _btnNetworkConnect.Content = "DISCONNECT NODE";
                    _btnNetworkConnect.Background = new SolidColorBrush(Color.FromRgb(200, 30, 30));
                }
                else
                {
                    _btnNetworkConnect.Content = _cmbNetworkMode.SelectedIndex == 1 ? "START SERVER" : "CONNECT TO LEADER";
                    _btnNetworkConnect.Background = new SolidColorBrush(Color.FromRgb(40, 42, 48));
                }
            });
        }

        private void StartMonitoring()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _refreshTimer.Tick += (s, e) => RefreshFollowerGridData();
            _refreshTimer.Start();
        }

        private void RefreshFollowerGridData()
        {
            // Sync status data periodically to the WPF DataGrid
            foreach (var item in _gridItems)
            {
                var fAccount = NinjaTrader.Cbi.Account.All.FirstOrDefault(a => a.Name == item.AccountName);
                if (fAccount == null) continue;

                // Query stats from engine if copy session active
                if (_engine.IsCopyingActive)
                {
                    // Check engine states
                    var fields = _engine.GetType().GetField("_followers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fields != null)
                    {
                        var dict = fields.GetValue(_engine) as ConcurrentDictionary<string, FollowerAccountSettings>;
                        if (dict != null && dict.TryGetValue(item.AccountName, out var fSettings))
                        {
                            item.Latency = fSettings.LastLatencyMs > 0 ? $"{fSettings.LastLatencyMs} ms" : "0 ms";
                            item.Slippage = fSettings.LastSlippageTicks > 0 ? $"{fSettings.LastSlippageTicks} ticks" : "0 ticks";

                            if (fSettings.IsDisarmed)
                            {
                                item.Status = "DISARMED";
                                item.StatusBrush = new SolidColorBrush(Color.FromRgb(255, 60, 60));
                            }
                            else if (fSettings.IsEnabled)
                            {
                                item.Status = "ACTIVE";
                                item.StatusBrush = new SolidColorBrush(Color.FromRgb(0, 200, 120));
                            }
                            continue;
                        }
                    }
                }

                // If not active copying
                item.Latency = "--";
                item.Slippage = "--";
                item.Status = "Ready";
                item.StatusBrush = new SolidColorBrush(Color.FromRgb(0, 180, 255));
            }
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                _txtLogConsole.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                _txtLogConsole.ScrollToEnd();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer?.Stop();
            _engine?.Dispose();
            _network?.Dispose();
            base.OnClosed(e);
        }
    }
}
