using System.Windows;
using BlankUpper.Models;
using BlankUpper.Services;

namespace BlankUpper;

public partial class App : System.Windows.Application
{
    private readonly SingleInstanceService _singleInstance = new();
    internal AppSettings Settings { get; private set; } = new();
    internal SettingsService SettingsStore { get; } = new();
    internal MouseHookService MouseHook { get; } = new(new ExplorerDetectionService(), new KeyboardInputService());
    internal TrayIconService? Tray { get; private set; }
    internal MainWindow? MainWindowInstance { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (!_singleInstance.TryAcquire(() => Dispatcher.BeginInvoke(() => MainWindowInstance?.ShowFromTray()))) { _singleInstance.ActivateExisting(); Shutdown(); return; }
        Settings = SettingsStore.Load();
        MouseHook.SetEnabled(Settings.IsEnabled);
        MainWindowInstance = new MainWindow();
        Tray = new TrayIconService(MainWindowInstance, this);
        Tray.Initialize();
        MainWindowInstance.Show();
        if (e.Args.Any(a => string.Equals(a, "--startup", StringComparison.OrdinalIgnoreCase)) || Settings.RunMinimized)
            MainWindowInstance.HideToTray();
    }

    internal void SaveSettings() { SettingsStore.Save(Settings); Tray?.Refresh(); }
    internal void ExitApplication()
    {
        Tray?.Dispose(); MouseHook.Dispose(); _singleInstance.Dispose(); Shutdown();
    }
    protected override void OnExit(ExitEventArgs e) { MouseHook.Dispose(); Tray?.Dispose(); _singleInstance.Dispose(); base.OnExit(e); }
}
