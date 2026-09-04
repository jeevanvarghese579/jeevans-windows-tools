using System.Windows;

namespace JeevansWindowsTools;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "Local\\JeevansWindowsTools.JeevanVarghese", out var firstInstance);
        if (!firstInstance)
        {
            System.Windows.MessageBox.Show("Jeevan's Windows Tools is already running.", "Windows Tools");
            Shutdown();
            return;
        }

        base.OnStartup(e);
        var window = new MainWindow();
        MainWindow = window;
        if (!e.Args.Contains("--startup", StringComparer.OrdinalIgnoreCase) || !window.StartMinimized)
            window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mutex is not null)
        {
            try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
            _mutex.Dispose();
        }
        base.OnExit(e);
    }
}
