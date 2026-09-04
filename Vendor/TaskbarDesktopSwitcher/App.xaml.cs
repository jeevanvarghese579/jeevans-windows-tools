using System.Windows;
using System.Windows.Threading;

namespace TaskbarDesktopSwitcher
{
    public partial class App : System.Windows.Application
    {
        private MainWindow? _mainWindow;
        private NotifyIconManager? _notifyIconManager;
        private MouseHook? _mouseHook;
        private KeyboardHook? _keyboardHook;
        private StartupManager? _startupManager;
        private EdgeSettings? _edgeSettings;
        private readonly WindowSwitcher _windowSwitcher = new();
        private int _wheelRemainder;
        private HookMouseButton? _suppressSelectButtonUntilUp;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Initialize startup manager
            _startupManager = new StartupManager();
            _edgeSettings = new EdgeSettings();
            _edgeSettings.Load();

            // Initialize the hook only when the persisted global setting is enabled.
            _mouseHook = new MouseHook();
            _mouseHook.WheelScrolled += OnWheelScrolled;
            _mouseHook.MouseButtonChanged += OnMouseButtonChanged;

            _keyboardHook = new KeyboardHook();
            _keyboardHook.EscapePressed += (_, _) => _windowSwitcher.Cancel();
            _keyboardHook.Start();

            // Create main window (but don't show it initially)
            _mainWindow = new MainWindow();
            _mainWindow.StartupManager = _startupManager;
            _mainWindow.MouseHook = _mouseHook;
            _mainWindow.EdgeSettings = _edgeSettings;
            _mainWindow.IsSwitcherEnabled = _edgeSettings.IsSwitcherEnabled;
            if (_edgeSettings.IsSwitcherEnabled) _mouseHook.Start();

            // Initialize notify icon manager
            _notifyIconManager = new NotifyIconManager(_mainWindow, _startupManager, _edgeSettings);
            
            // Pass notify icon manager reference to main window
            _mainWindow.NotifyIconManager = _notifyIconManager;

            // Show the main window only if "Start Minimized" is not enabled
            if (!_startupManager.IsStartMinimizedEnabled())
            {
                _mainWindow.Show();
            }
        }

        private void OnWheelScrolled(object? sender, WheelEventArgs e)
        {
            var edgeFunction = GetEdgeFunction(TaskbarHelper.GetCursorEdge(_edgeSettings?.TriggerDistance ?? 30));
            if (edgeFunction == EdgeFunction.None)
            {
                return;
            }

            // Consume every partial high-resolution wheel message so it never leaks into the app beneath it.
            e.Handled = true;
            if (Math.Sign(e.Delta) != Math.Sign(_wheelRemainder)) _wheelRemainder = 0;
            _wheelRemainder += e.Delta;

            while (Math.Abs(_wheelRemainder) >= 120)
            {
                var wheelUp = _wheelRemainder > 0;
                _wheelRemainder -= wheelUp ? 120 : -120;

                if (edgeFunction == EdgeFunction.VirtualDesktops)
                {
                    if (wheelUp) VirtualDesktopSwitcher.SwitchToPreviousDesktop();
                    else VirtualDesktopSwitcher.SwitchToNextDesktop();
                }
                else if (edgeFunction == EdgeFunction.WindowSwitching)
                {
                    _windowSwitcher.Move(forward: !wheelUp);
                }
            }
        }

        private EdgeFunction GetEdgeFunction(EdgePosition edge) => edge switch
        {
            EdgePosition.Top => _edgeSettings?.TopEdge ?? EdgeFunction.None,
            EdgePosition.Bottom => _edgeSettings?.BottomEdge ?? EdgeFunction.VirtualDesktops,
            _ => EdgeFunction.None
        };

        private void OnMouseButtonChanged(object? sender, HookMouseButtonEventArgs e)
        {
            // Keep consuming the matching Up after selection. Otherwise Windows receives an
            // unmatched button-up and can still act on the physical click.
            if (_suppressSelectButtonUntilUp == e.Button && !e.IsDown)
            {
                e.Handled = true;
                _suppressSelectButtonUntilUp = null;
                return;
            }

            if (!_windowSwitcher.IsActive || !e.IsDown) return;
            if (e.Button == ToHookMouseButton(_edgeSettings?.SelectButton ?? WindowSwitcherSelectButton.Right))
            {
                _suppressSelectButtonUntilUp = e.Button;
                e.Handled = true;
                _windowSwitcher.Select();
            }
            else
            {
                // A normal click lets Windows dismiss Alt+Tab and makes our state match it.
                _windowSwitcher.Cancel();
            }
        }

        private static HookMouseButton ToHookMouseButton(WindowSwitcherSelectButton button) => button switch
        {
            WindowSwitcherSelectButton.Left => HookMouseButton.Left,
            WindowSwitcherSelectButton.Middle => HookMouseButton.Middle,
            _ => HookMouseButton.Right
        };

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Clean up resources
            _mouseHook?.Stop();
            _mouseHook?.Dispose();
            _keyboardHook?.Dispose();
            _notifyIconManager?.Dispose();
        }

        public void ExitApplication()
        {
            if (_mainWindow != null)
            {
                _mainWindow.IsExiting = true;
            }
            System.Windows.Application.Current.Shutdown();
        }
    }
}
