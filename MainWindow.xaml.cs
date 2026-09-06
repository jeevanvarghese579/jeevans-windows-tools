using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using JeevansWindowsTools.Models;
using JeevansWindowsTools.Services;
using TaskbarDesktopSwitcher;
using Forms = System.Windows.Forms;

namespace JeevansWindowsTools;

public partial class MainWindow : Window
{
    private readonly UnifiedSettingsStore _settingsStore = new();
    private readonly UnifiedSettings _settings;
    private readonly FeatureHost _host;
    private readonly Forms.NotifyIcon _tray;
    private readonly System.Drawing.Icon _trayIcon;
    private readonly System.Windows.Threading.DispatcherTimer _momentumUiTimer;
    private bool _loading = true;
    private bool _exiting;

    public bool StartMinimized => _settings.StartMinimized;

    public MainWindow()
    {
        _settings = _settingsStore.Load();
        _settings.StartWithWindows = StartupRegistration.IsEnabled();
        _host = new FeatureHost(_settings.MasterEnabled);
        InitializeComponent();
        SourceInitialized += (_, _) => _host.AttachWindow(new System.Windows.Interop.WindowInteropHelper(this).Handle);

        _momentumUiTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _momentumUiTimer.Tick += (_, _) => UpdateMomentumDiagnostics();
        _momentumUiTimer.Start();

        _trayIcon = LoadAppIcon();
        _tray = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "Jeevan's Windows Tools",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _tray.DoubleClick += (_, _) => ShowFromTray();
        _host.BlankActivated += OnBlankActivated;
        Closing += OnClosing;
        LoadControls();
        _loading = false;
        RefreshStatus();
    }

    private static System.Drawing.Icon LoadAppIcon()
    {
        var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/JeevansWindowsTools.ico"));
        if (resource is null) return (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
        using (resource.Stream)
        using (var source = new System.Drawing.Icon(resource.Stream))
            return (System.Drawing.Icon)source.Clone();
    }

    private Forms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Windows Tools", null, (_, _) => Dispatcher.Invoke(ShowFromTray));
        menu.Items.Add("Enable all", null, (_, _) => Dispatcher.Invoke(() => MasterToggle.IsChecked = true));
        menu.Items.Add("Disable all", null, (_, _) => Dispatcher.Invoke(() => MasterToggle.IsChecked = false));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(ExitApplication));
        return menu;
    }

    private void LoadControls()
    {
        MasterToggle.IsChecked = _settings.MasterEnabled;
        StartupToggle.IsChecked = _settings.StartWithWindows;
        MinimizedToggle.IsChecked = _settings.StartMinimized;
        BlankToggle.IsChecked = BlankDetailToggle.IsChecked = _host.BlankSettings.IsEnabled;
        BlankNotificationToggle.IsChecked = _host.BlankSettings.ShowNotification;
        MomentumToggle.IsChecked = MomentumDetailToggle.IsChecked = _host.MomentumSettings.Enabled;
        LoadMomentumControls();
        TaskbarToggle.IsChecked = TaskbarDetailToggle.IsChecked = _host.TaskbarSettings.IsSwitcherEnabled;
        SelectCombo(TopEdgeCombo, _host.TaskbarSettings.TopEdge.ToString());
        SelectCombo(BottomEdgeCombo, _host.TaskbarSettings.BottomEdge.ToString());
        SelectCombo(SelectButtonCombo, _host.TaskbarSettings.SelectButton.ToString());
        UpdateMomentumLabels();
        UpdateMomentumDiagnostics();
    }

    private void LoadMomentumControls()
    {
        SensitivitySlider.Value = _host.MomentumSettings.ActivationSensitivity;
        FlickNotchesSlider.Value = _host.MomentumSettings.MinimumFlickNotches;
        FlickWindowSlider.Value = _host.MomentumSettings.FlickDetectionWindowMs;
        StrengthSlider.Value = _host.MomentumSettings.MomentumStrength;
        FrictionSlider.Value = _host.MomentumSettings.FrictionPerTick;
        MaximumVelocitySlider.Value = _host.MomentumSettings.MaximumVelocity;
        MinimumVelocitySlider.Value = _host.MomentumSettings.MinimumStopVelocity;
        SameDirectionToggle.IsChecked = _host.MomentumSettings.SameDirectionBoosts;
        OppositeDirectionToggle.IsChecked = _host.MomentumSettings.OppositeDirectionStops;
        NaturalToggle.IsChecked = _host.MomentumSettings.NaturalScrolling;
        HorizontalToggle.IsChecked = _host.MomentumSettings.HorizontalScrolling;
    }

    private static void SelectCombo(System.Windows.Controls.ComboBox combo, string tag) =>
        combo.SelectedItem = combo.Items.OfType<System.Windows.Controls.ComboBoxItem>().FirstOrDefault(x => string.Equals(x.Tag?.ToString(), tag, StringComparison.Ordinal));

    private void RefreshStatus()
    {
        var master = _settings.MasterEnabled;
        MasterStatusText.Text = master ? "Your enabled tools are active" : "All tools are paused; individual choices are preserved";
        SetStatus(BlankStatus, master, _host.BlankSettings.IsEnabled);
        SetStatus(MomentumStatus, master, _host.MomentumSettings.Enabled);
        SetStatus(TaskbarStatus, master, _host.TaskbarSettings.IsSwitcherEnabled);
    }

    private static void SetStatus(TextBlock text, bool master, bool individual)
    {
        var active = master && individual;
        text.Text = active ? "● Active" : individual ? "● Paused by master switch" : "● Off";
        text.Foreground = new System.Windows.Media.SolidColorBrush(active
            ? System.Windows.Media.Color.FromRgb(86, 214, 169)
            : System.Windows.Media.Color.FromRgb(170, 179, 204));
    }

    private void MasterToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.MasterEnabled = MasterToggle.IsChecked == true;
        _host.SetMasterEnabled(_settings.MasterEnabled);
        SaveUnified();
        RefreshStatus();
    }

    private void BlankToggle_Changed(object sender, RoutedEventArgs e) => SetBlankEnabled(BlankToggle.IsChecked == true);
    private void BlankDetailToggle_Changed(object sender, RoutedEventArgs e) => SetBlankEnabled(BlankDetailToggle.IsChecked == true);
    private void SetBlankEnabled(bool enabled)
    {
        if (_loading) return;
        _loading = true;
        BlankToggle.IsChecked = BlankDetailToggle.IsChecked = enabled;
        _loading = false;
        _host.SetBlankEnabled(enabled);
        RefreshStatus();
    }

    private void BlankNotification_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _host.BlankSettings.ShowNotification = BlankNotificationToggle.IsChecked == true;
        _host.SaveBlankSettings();
    }

    private void MomentumToggle_Changed(object sender, RoutedEventArgs e) => SetMomentumEnabled(MomentumToggle.IsChecked == true);
    private void MomentumDetailToggle_Changed(object sender, RoutedEventArgs e) => SetMomentumEnabled(MomentumDetailToggle.IsChecked == true);
    private void SetMomentumEnabled(bool enabled)
    {
        if (_loading) return;
        _loading = true;
        MomentumToggle.IsChecked = MomentumDetailToggle.IsChecked = enabled;
        _loading = false;
        _host.SetMomentumEnabled(enabled);
        RefreshStatus();
    }

    private void MomentumTuning_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        _host.MomentumSettings.ActivationSensitivity = (int)Math.Round(SensitivitySlider.Value);
        _host.MomentumSettings.MinimumFlickNotches = (int)Math.Round(FlickNotchesSlider.Value);
        _host.MomentumSettings.FlickDetectionWindowMs = (int)Math.Round(FlickWindowSlider.Value);
        _host.MomentumSettings.MomentumStrength = StrengthSlider.Value;
        _host.MomentumSettings.FrictionPerTick = FrictionSlider.Value;
        _host.MomentumSettings.MaximumVelocity = MaximumVelocitySlider.Value;
        _host.MomentumSettings.MinimumStopVelocity = MinimumVelocitySlider.Value;
        _host.SaveMomentumSettings();
        UpdateMomentumLabels();
    }

    private void MomentumOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _host.MomentumSettings.SameDirectionBoosts = SameDirectionToggle.IsChecked == true;
        _host.MomentumSettings.OppositeDirectionStops = OppositeDirectionToggle.IsChecked == true;
        _host.MomentumSettings.NaturalScrolling = NaturalToggle.IsChecked == true;
        _host.MomentumSettings.HorizontalScrolling = HorizontalToggle.IsChecked == true;
        _host.SaveMomentumSettings();
    }

    private void UpdateMomentumLabels()
    {
        if (StrengthValue is null || FrictionValue is null || SensitivityValue is null) return;
        SensitivityValue.Text = $"{SensitivitySlider.Value:0}";
        FlickNotchesValue.Text = $"{FlickNotchesSlider.Value:0}";
        FlickWindowValue.Text = $"{FlickWindowSlider.Value:0} ms";
        StrengthValue.Text = $"{StrengthSlider.Value:0.00}×";
        FrictionValue.Text = $"{FrictionSlider.Value:0.000}";
        MaximumVelocityValue.Text = $"{MaximumVelocitySlider.Value:0}";
        MinimumVelocityValue.Text = $"{MinimumVelocitySlider.Value:0}";
    }

    private void RestoreMomentumDefaults_Click(object sender, RoutedEventArgs e)
    {
        _host.RestoreMomentumDefaults();
        _loading = true;
        LoadMomentumControls();
        _loading = false;
        UpdateMomentumLabels();
        UpdateMomentumDiagnostics();
    }

    private void UpdateMomentumDiagnostics()
    {
        if (MomentumLiveStatus is null) return;
        MomentumLiveStatus.Text = _host.MomentumIsActive ? "Momentum active" : "Momentum idle";
        MomentumLiveStatus.Foreground = new System.Windows.Media.SolidColorBrush(_host.MomentumIsActive
            ? System.Windows.Media.Color.FromRgb(86, 214, 169)
            : System.Windows.Media.Color.FromRgb(170, 179, 204));
        MomentumDiagnosticsText.Text =
            $"Physical mouse filter: {(_host.MomentumPhysicalMouseDetected ? "Active" : "Waiting for mouse wheel")}\n" +
            $"Last physical delta: {_host.MomentumLastPhysicalDelta}\n" +
            $"Flick count: {_host.MomentumFlickCount}\n" +
            $"Velocity: {_host.MomentumVelocity:0.0} units/s\n" +
            $"Last injected delta: {_host.MomentumLastInjectedDelta}\n" +
            $"Injection: {_host.MomentumInjectionStatus}\n" +
            $"Last stop reason: {_host.MomentumLastStopReason}";
    }

    private void RestartAsAdministrator_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                Environment.ProcessPath ?? throw new InvalidOperationException("Executable path unavailable"))
            {
                UseShellExecute = true,
                Verb = "runas"
            });
            ExitApplication();
        }
        catch (System.ComponentModel.Win32Exception) { }
    }

    private void TaskbarToggle_Changed(object sender, RoutedEventArgs e) => SetTaskbarEnabled(TaskbarToggle.IsChecked == true);
    private void TaskbarDetailToggle_Changed(object sender, RoutedEventArgs e) => SetTaskbarEnabled(TaskbarDetailToggle.IsChecked == true);
    private void SetTaskbarEnabled(bool enabled)
    {
        if (_loading) return;
        _loading = true;
        TaskbarToggle.IsChecked = TaskbarDetailToggle.IsChecked = enabled;
        _loading = false;
        _host.SetTaskbarEnabled(enabled);
        RefreshStatus();
    }

    private void EdgeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || TopEdgeCombo.SelectedItem is not System.Windows.Controls.ComboBoxItem top || BottomEdgeCombo.SelectedItem is not System.Windows.Controls.ComboBoxItem bottom) return;
        if (Enum.TryParse<EdgeFunction>(top.Tag?.ToString(), out var topValue) && Enum.TryParse<EdgeFunction>(bottom.Tag?.ToString(), out var bottomValue))
            _host.SaveTaskbarEdges(topValue, bottomValue);
    }

    private void SelectButtonCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || SelectButtonCombo.SelectedItem is not System.Windows.Controls.ComboBoxItem item) return;
        if (Enum.TryParse<WindowSwitcherSelectButton>(item.Tag?.ToString(), out var value)) _host.SaveTaskbarSelectButton(value);
    }

    private void StartupToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.StartWithWindows = StartupToggle.IsChecked == true;
        StartupRegistration.SetEnabled(_settings.StartWithWindows);
        SaveUnified();
    }

    private void MinimizedToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.StartMinimized = MinimizedToggle.IsChecked == true;
        SaveUnified();
    }

    private void SaveUnified() => _settingsStore.Save(_settings);
    private void OpenBlank_Click(object sender, RoutedEventArgs e) => ShowPage(BlankSettingsPage);
    private void OpenMomentum_Click(object sender, RoutedEventArgs e) => ShowPage(MomentumSettingsPage);
    private void OpenTaskbar_Click(object sender, RoutedEventArgs e) => ShowPage(TaskbarSettingsPage);
    private void Back_Click(object sender, RoutedEventArgs e) => ShowPage(DashboardPage);

    private void ShowPage(UIElement page)
    {
        DashboardPage.Visibility = BlankSettingsPage.Visibility = MomentumSettingsPage.Visibility = TaskbarSettingsPage.Visibility = Visibility.Collapsed;
        page.Visibility = Visibility.Visible;
    }

    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();
    private void OnBlankActivated(object? sender, EventArgs e)
    {
        if (_host.BlankSettings.ShowNotification)
            Dispatcher.BeginInvoke(() => _tray.ShowBalloonTip(900, "Blank Upper", "Navigated to the parent folder.", Forms.ToolTipIcon.Info));
    }
    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_exiting) return;
        e.Cancel = true;
        Hide();
    }

    private void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;
        _host.BlankActivated -= OnBlankActivated;
        _momentumUiTimer.Stop();
        _host.Dispose();
        _tray.Visible = false;
        _tray.ContextMenuStrip?.Dispose();
        _tray.Dispose();
        _trayIcon.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }
}
