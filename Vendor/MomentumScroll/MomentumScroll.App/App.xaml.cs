using System.Windows;
namespace MomentumScroll.App;
public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "Local\\MomentumScroll.JeevanVarghese", out var first);
        if (!first) { System.Windows.MessageBox.Show("Momentum Scroll is already running.", "Momentum Scroll"); Shutdown(); return; }
        DispatcherUnhandledException += (_, x) => { Logger.Error("Unhandled UI exception", x.Exception); x.Handled = true; };
        AppDomain.CurrentDomain.UnhandledException += (_, x) => Logger.Error("Unhandled exception", x.ExceptionObject as Exception);
        base.OnStartup(e);
    }
    protected override void OnExit(ExitEventArgs e) { _mutex?.ReleaseMutex(); _mutex?.Dispose(); base.OnExit(e); }
}
