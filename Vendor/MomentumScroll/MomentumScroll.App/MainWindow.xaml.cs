using System.Diagnostics;
using System.Windows;
using MomentumScroll.App.ViewModels;
using MomentumScroll.App.Services;
using Forms = System.Windows.Forms;
namespace MomentumScroll.App;
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new(); private readonly Forms.NotifyIcon _tray; private RawInputMouseMonitor? _rawInput; private bool _exiting;
    public MainWindow()
    {
        InitializeComponent(); DataContext = _viewModel;
        _tray = new Forms.NotifyIcon { Icon = new System.Drawing.Icon(GetType().Assembly.GetManifestResourceStream("MomentumScroll.App.Resources.MomentumScroll.ico") ?? throw new InvalidOperationException("Icon unavailable")), Text = "Momentum Scroll", Visible = true };
        var menu = new Forms.ContextMenuStrip(); menu.Items.Add("Open", null, (_, _) => Restore()); menu.Items.Add("Enable", null, (_, _) => _viewModel.IsEnabled = true); menu.Items.Add("Disable", null, (_, _) => _viewModel.IsEnabled = false); menu.Items.Add("Settings", null, (_, _) => OpenSettings()); menu.Items.Add("About", null, (_, _) => ShowAbout()); menu.Items.Add("Restart as Administrator", null, (_, _) => RestartAsAdministrator()); menu.Items.Add("Exit", null, (_, _) => Exit()); _tray.ContextMenuStrip = menu; _tray.DoubleClick += (_, _) => Restore();
        Loaded += (_, _) => { if (_viewModel.StartMinimized) Hide(); else Restore(); };
        SourceInitialized += (_, _) => _rawInput = new RawInputMouseMonitor(new System.Windows.Interop.WindowInteropHelper(this).Handle, _viewModel.RecordPhysicalMouseWheel);
    }
    private void RestoreDefaults_Click(object sender, RoutedEventArgs e) => _viewModel.RestoreDefaults();
    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private void About_Click(object sender, RoutedEventArgs e) => ShowAbout();
    private void Exit_Click(object sender, RoutedEventArgs e) => Exit();
    private void Restore() { Show(); WindowState = WindowState.Normal; Activate(); Topmost = true; Topmost = false; Focus(); }
    private void OpenSettings() { Restore(); new SettingsWindow(_viewModel) { Owner = this }.ShowDialog(); }
    private void RestartAsAdministrator()
    {
        try { Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? throw new InvalidOperationException("Executable path unavailable")) { UseShellExecute = true, Verb = "runas" }); Exit(); }
        catch (System.ComponentModel.Win32Exception x) { Logger.Info($"Administrator restart cancelled or failed: {x.NativeErrorCode}"); }
    }
    private void ShowAbout()
    {
        var link = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("https://itsjeevanvarghese.web.app")) { NavigateUri = new Uri("https://itsjeevanvarghese.web.app") };
        link.RequestNavigate += (_, _) => Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri) { UseShellExecute = true });
        var text = new System.Windows.Controls.TextBlock { Margin = new Thickness(24), TextWrapping = TextWrapping.Wrap };
        text.Inlines.Add("Momentum Scroll\n\nDeveloped by Jeevan Varghese\n\nVisit itsjeevanvarghese.web.app for more softwares\n"); text.Inlines.Add(link); text.Inlines.Add($"\n\nVersion: {GetType().Assembly.GetName().Version}\n.NET runtime: {Environment.Version}\nWindows: {Environment.OSVersion.Version}\nPortable application: Yes");
        new Window { Title = "About Momentum Scroll", Content = text, Width = 410, Height = 310, ResizeMode = ResizeMode.NoResize, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
    }
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e) { if (!_exiting && _viewModel.MinimizeToTray) { e.Cancel = true; Hide(); _tray.ShowBalloonTip(1000, "Momentum Scroll", "Still running in the system tray.", Forms.ToolTipIcon.Info); } else if (!_exiting) Exit(); else base.OnClosing(e); }
    private void Exit() { if (_exiting) return; _exiting = true; Logger.Info("Exit requested from tray/UI."); _rawInput?.Dispose(); _viewModel.Dispose(); _tray.Visible = false; _tray.Dispose(); System.Windows.Application.Current.Shutdown(); }
}
