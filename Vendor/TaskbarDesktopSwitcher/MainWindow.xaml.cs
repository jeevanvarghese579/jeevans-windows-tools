using System.Windows;
using System.Windows.Media;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace TaskbarDesktopSwitcher
{
    public partial class MainWindow : Window
    {
        public StartupManager? StartupManager { get; set; }
        public bool IsExiting { get; set; } = false;
        public bool IsSwitcherEnabled { get; set; } = true;
        public MouseHook? MouseHook { get; set; }
        public NotifyIconManager? NotifyIconManager { get; set; }
        public EdgeSettings? EdgeSettings { get; set; }
        private bool _isLoading;

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            LoadIcon();
        }

        private void LoadIcon()
        {
            try
            {
                this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/icon.ico", UriKind.Absolute));
            }
            catch
            {
                // Silently handle icon loading errors
            }
        }

        #nullable disable
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsExiting)
            {
                // Allow exit
                return;
            }
            // Minimize to tray instead of closing
            e.Cancel = true;
            this.Hide();
        }
        #nullable restore

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Keep the complete settings view reachable even on highly scaled or short displays.
            MaxHeight = SystemParameters.WorkArea.Height;
            Height = Math.Min(Height, MaxHeight);
            // Initialize the toggle based on current startup state
            if (StartupManager != null)
            {
                StartWithWindowsToggle.IsChecked = StartupManager.IsStartWithWindowsEnabled();
                StartMinimizedToggle.IsChecked = StartupManager.IsStartMinimizedEnabled();
            }
            
            // Initialize the enable switcher toggle
            EnableSwitcherToggle.IsChecked = IsSwitcherEnabled;
            _isLoading = true;
            SelectEdgeFunction(TopEdgeComboBox, EdgeSettings?.TopEdge ?? EdgeFunction.None);
            SelectEdgeFunction(BottomEdgeComboBox, EdgeSettings?.BottomEdge ?? EdgeFunction.VirtualDesktops);
            SelectEnum(WindowSwitcherSelectButtonComboBox, EdgeSettings?.SelectButton ?? WindowSwitcherSelectButton.Right);
            _isLoading = false;
            UpdateStatus(IsSwitcherEnabled);
        }

        private void StartWithWindowsToggle_Checked(object sender, RoutedEventArgs e)
        {
            StartupManager?.EnableStartWithWindows();
        }

        private void StartWithWindowsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupManager?.DisableStartWithWindows();
        }

        private void EnableSwitcherToggle_Checked(object sender, RoutedEventArgs e)
        {
            IsSwitcherEnabled = true;
            EdgeSettings?.SaveEnabled(true);
            MouseHook?.Start();
            UpdateStatus(true);
            NotifyIconManager?.UpdateEnableSwitcherState(true);
        }

        private void EnableSwitcherToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            IsSwitcherEnabled = false;
            EdgeSettings?.SaveEnabled(false);
            MouseHook?.Stop();
            UpdateStatus(false);
            NotifyIconManager?.UpdateEnableSwitcherState(false);
        }

        private void StartMinimizedToggle_Checked(object sender, RoutedEventArgs e)
        {
            StartupManager?.EnableStartMinimized();
        }

        private void StartMinimizedToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupManager?.DisableStartMinimized();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void EdgeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isLoading || EdgeSettings == null) return;
            EdgeSettings.Save(GetEdgeFunction(TopEdgeComboBox), GetEdgeFunction(BottomEdgeComboBox));
            NotifyIconManager?.UpdateEdgeStates();
        }

        public void SetEdgeFunction(EdgePosition edge, EdgeFunction function)
        {
            _isLoading = true;
            SelectEdgeFunction(edge == EdgePosition.Top ? TopEdgeComboBox : BottomEdgeComboBox, function);
            _isLoading = false;
            EdgeSettings?.Save(GetEdgeFunction(TopEdgeComboBox), GetEdgeFunction(BottomEdgeComboBox));
            NotifyIconManager?.UpdateEdgeStates();
        }

        private void WindowSwitcherSelectButtonComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isLoading || EdgeSettings == null) return;
            EdgeSettings.SaveWindowSwitcherSelectButton(GetEnum<WindowSwitcherSelectButton>(WindowSwitcherSelectButtonComboBox, WindowSwitcherSelectButton.Right));
        }

        private static void SelectEdgeFunction(WpfComboBox comboBox, EdgeFunction edgeFunction)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is WpfComboBoxItem comboBoxItem && comboBoxItem.Tag?.ToString() == edgeFunction.ToString())
                {
                    comboBox.SelectedItem = comboBoxItem;
                    break;
                }
            }
        }

        private static EdgeFunction GetEdgeFunction(WpfComboBox comboBox) =>
            comboBox.SelectedItem is WpfComboBoxItem item && Enum.TryParse<EdgeFunction>(item.Tag?.ToString(), out var result)
                ? result : EdgeFunction.None;

        private static void SelectEnum<T>(WpfComboBox comboBox, T value) where T : struct, Enum
        {
            foreach (var item in comboBox.Items)
                if (item is WpfComboBoxItem comboBoxItem && comboBoxItem.Tag?.ToString() == value.ToString()) { comboBox.SelectedItem = comboBoxItem; break; }
        }

        private static T GetEnum<T>(WpfComboBox comboBox, T fallback) where T : struct, Enum =>
            comboBox.SelectedItem is WpfComboBoxItem item && Enum.TryParse<T>(item.Tag?.ToString(), out var value) ? value : fallback;

        public void UpdateStatus(bool isRunning)
        {
            if (isRunning)
            {
                StatusIndicator.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
                StatusText.Text = "Running";
            }
            else
            {
                StatusIndicator.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
                StatusText.Text = "Stopped";
            }
        }
    }
}
