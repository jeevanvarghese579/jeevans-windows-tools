using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using BlankUpper.Services;
namespace BlankUpper;
public partial class MainWindow : Window
{
    private bool _loading;
    private App CurrentApp => (App)System.Windows.Application.Current;
    public MainWindow() { InitializeComponent(); Closing += OnClosing; RefreshFromSettings(); VersionText.Text = $"Version {GetType().Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}"; }
    public void RefreshFromSettings()
    {
        if (!IsInitialized) return; _loading = true;
        EnabledToggle.IsChecked = CurrentApp.Settings.IsEnabled; StartupToggle.IsChecked = CurrentApp.Settings.StartWithWindows; MinimizedToggle.IsChecked = CurrentApp.Settings.RunMinimized; NotificationToggle.IsChecked = CurrentApp.Settings.ShowNotification;
        StatusText.Text = CurrentApp.Settings.IsEnabled ? "Status: Active" : "Status: Paused"; StatusText.Foreground = CurrentApp.Settings.IsEnabled ? System.Windows.Media.Brushes.MediumPurple : System.Windows.Media.Brushes.Orange; _loading = false;
    }
    private void EnabledChanged(object sender, RoutedEventArgs e) { if (_loading) return; CurrentApp.Settings.IsEnabled = EnabledToggle.IsChecked == true; CurrentApp.MouseHook.SetEnabled(CurrentApp.Settings.IsEnabled); CurrentApp.SaveSettings(); RefreshFromSettings(); }
    private void StartupChanged(object sender, RoutedEventArgs e) { if (_loading) return; CurrentApp.Settings.StartWithWindows = StartupToggle.IsChecked == true; new StartupService().SetEnabled(CurrentApp.Settings.StartWithWindows); CurrentApp.SaveSettings(); }
    private void MinimizedChanged(object sender, RoutedEventArgs e) { if (!_loading) { CurrentApp.Settings.RunMinimized = MinimizedToggle.IsChecked == true; CurrentApp.SaveSettings(); } }
    private void NotificationChanged(object sender, RoutedEventArgs e) { if (!_loading) { CurrentApp.Settings.ShowNotification = NotificationToggle.IsChecked == true; CurrentApp.SaveSettings(); } }
    private void OnClosing(object? sender, CancelEventArgs e) { e.Cancel = true; HideToTray(); }
    public void HideToTray() { Hide(); }
    public void ShowFromTray() { Show(); WindowState = WindowState.Normal; Activate(); Topmost = true; Topmost = false; Focus(); }
    private void Home_Click(object sender, RoutedEventArgs e) => ShowPage(HomePage);
    private void Settings_Click(object sender, RoutedEventArgs e) => ShowPage(SettingsPage);
    private void Developer_Click(object sender, RoutedEventArgs e) => ShowPage(DeveloperPage);
    private void ShowPage(UIElement page) { HomePage.Visibility = Visibility.Collapsed; SettingsPage.Visibility = Visibility.Collapsed; DeveloperPage.Visibility = Visibility.Collapsed; page.Visibility = Visibility.Visible; }
    private void Website_RequestNavigate(object sender, RequestNavigateEventArgs e) { try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); } catch (Exception ex) { AppLogger.Error("Could not open website", ex); } e.Handled = true; }
}
