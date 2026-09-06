using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using BlankUpper.Models;
using BlankUpper.Services;
using Microsoft.Win32;
using MomentumScroll.App.Services;
using MomentumScroll.Core;
using TaskbarDesktopSwitcher;
using CoreMouseButtons = MomentumScroll.Core.MouseButtons;

namespace JeevansWindowsTools.Services;

public sealed class FeatureHost : IDisposable
{
    private readonly SettingsService _blankStore = new();
    private readonly ExplorerDetectionService _blankDetector = new();
    private readonly KeyboardInputService _blankKeyboard = new();
    private readonly SettingsStore _momentumStore = new();
    private readonly MomentumController _momentumController;
    private readonly EdgeSettings _taskbarSettings = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly WindowSwitcher _windowSwitcher = new();
    private readonly SharedMouseHook _mouseHook;
    private int _wheelRemainder;
    private SharedMouseButton? _suppressButtonUntilUp;
    private bool _masterEnabled;
    private long _lastBlankClickTick;
    private long _lastBlankActivationTick;
    private int _lastBlankX, _lastBlankY, _checkingBlank;

    public AppSettings BlankSettings { get; }
    public MomentumSettings MomentumSettings { get; }
    public EdgeSettings TaskbarSettings => _taskbarSettings;
    public event EventHandler? BlankActivated;

    public FeatureHost(bool masterEnabled)
    {
        _masterEnabled = masterEnabled;
        BlankSettings = _blankStore.Load();
        MomentumSettings = _momentumStore.Load();
        _momentumController = new MomentumController(MomentumSettings);
        _taskbarSettings.Load();
        _keyboardHook.EscapePressed += OnEscapePressed;
        _mouseHook = new SharedMouseHook(HandleMouseEvent);
        ApplyEffectiveStates();
    }

    public void SetMasterEnabled(bool enabled)
    {
        _masterEnabled = enabled;
        ApplyEffectiveStates();
    }

    public void SetBlankEnabled(bool enabled)
    {
        BlankSettings.IsEnabled = enabled;
        _blankStore.Save(BlankSettings);
        ApplySharedMouseState();
    }

    public void SaveBlankSettings() => _blankStore.Save(BlankSettings);

    public void SetMomentumEnabled(bool enabled)
    {
        MomentumSettings.Enabled = enabled;
        _momentumStore.Save(MomentumSettings);
        ApplyMomentumState();
        ApplySharedMouseState();
    }

    public void SaveMomentumSettings()
    {
        _momentumController.Apply(MomentumSettings);
        _momentumStore.Save(MomentumSettings);
    }

    public void SetTaskbarEnabled(bool enabled)
    {
        _taskbarSettings.SaveEnabled(enabled);
        ApplyTaskbarState();
        ApplySharedMouseState();
    }

    public void SaveTaskbarEdges(EdgeFunction top, EdgeFunction bottom)
    {
        _taskbarSettings.Save(top, bottom);
        ApplyTaskbarState();
    }

    public void SaveTaskbarSelectButton(WindowSwitcherSelectButton button) =>
        _taskbarSettings.SaveWindowSwitcherSelectButton(button);

    private bool BlankActive => _masterEnabled && BlankSettings.IsEnabled;
    private bool MomentumActive => _masterEnabled && MomentumSettings.Enabled;
    private bool TaskbarActive => _masterEnabled && _taskbarSettings.IsSwitcherEnabled;
    private bool TaskbarNeedsMouse => TaskbarActive &&
        (_taskbarSettings.TopEdge != EdgeFunction.None || _taskbarSettings.BottomEdge != EdgeFunction.None);

    private void ApplyEffectiveStates()
    {
        ApplyMomentumState();
        ApplyTaskbarState();
        ApplySharedMouseState();
    }

    private void ApplyMomentumState()
    {
        // Mouse input arrives through SharedMouseHook; MomentumController only runs its timer.
        if (MomentumActive) _momentumController.Enable();
        else _momentumController.Disable();
    }

    private void ApplyTaskbarState()
    {
        // Escape is only needed while Ctrl+Alt+Tab window switching is configured.
        var needsKeyboard = TaskbarActive &&
            (_taskbarSettings.TopEdge == EdgeFunction.WindowSwitching ||
             _taskbarSettings.BottomEdge == EdgeFunction.WindowSwitching);
        if (needsKeyboard) _keyboardHook.Start();
        else
        {
            _keyboardHook.Stop();
            _windowSwitcher.Cancel();
        }
    }

    private void ApplySharedMouseState()
    {
        if (BlankActive || MomentumActive || TaskbarNeedsMouse) _mouseHook.Start();
        else _mouseHook.Stop();
    }

    private bool HandleMouseEvent(SharedMouseEvent e)
    {
        if (BlankActive && e.Kind == SharedMouseEventKind.ButtonDown && e.Button == SharedMouseButton.Left)
            ObserveBlankClick(e.X, e.Y);

        var handled = TaskbarNeedsMouse && e.Kind switch
        {
            SharedMouseEventKind.Wheel => HandleTaskbarWheel(e.WheelDelta),
            SharedMouseEventKind.ButtonDown => HandleTaskbarButton(e.Button, true),
            SharedMouseEventKind.ButtonUp => HandleTaskbarButton(e.Button, false),
            _ => false
        };

        if (MomentumActive && !handled)
        {
            if (e.Kind is SharedMouseEventKind.Wheel or SharedMouseEventKind.HorizontalWheel)
                _momentumController.HandlePhysicalWheel(e.WheelDelta, e.Kind == SharedMouseEventKind.HorizontalWheel);
            else if (TryMapButton(e.Button, out var button))
                _momentumController.HandleMouseButton(button, e.Kind == SharedMouseEventKind.ButtonDown);
        }
        return handled;
    }

    private void ObserveBlankClick(int x, int y)
    {
        var now = Stopwatch.GetTimestamp();
        var elapsedMs = (now - Interlocked.Read(ref _lastBlankClickTick)) * 1000 / Stopwatch.Frequency;
        var closeEnough = Math.Abs(x - _lastBlankX) <= NativeMethods.DoubleClickWidth &&
                          Math.Abs(y - _lastBlankY) <= NativeMethods.DoubleClickHeight;
        if (elapsedMs <= NativeMethods.DoubleClickTime && closeEnough &&
            now - Interlocked.Read(ref _lastBlankActivationTick) > Stopwatch.Frequency / 2 &&
            Interlocked.CompareExchange(ref _checkingBlank, 1, 0) == 0)
            _ = CheckBlankClickAsync(x, y);
        _lastBlankX = x;
        _lastBlankY = y;
        Interlocked.Exchange(ref _lastBlankClickTick, now);
    }

    private async Task CheckBlankClickAsync(int x, int y)
    {
        try
        {
            var blank = await Task.Run(() => BlankActive && _blankDetector.IsConfidentExplorerBlankSpace(x, y)).ConfigureAwait(false);
            if (!blank || !BlankActive) return;
            _blankKeyboard.SendAltUp();
            Interlocked.Exchange(ref _lastBlankActivationTick, Stopwatch.GetTimestamp());
            Interlocked.Exchange(ref _lastBlankClickTick, 0);
            BlankActivated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { AppLogger.Error("Blank-space detection failed", ex); }
        finally { Interlocked.Exchange(ref _checkingBlank, 0); }
    }

    private bool HandleTaskbarWheel(int delta)
    {
        var function = TaskbarHelper.GetCursorEdge(_taskbarSettings.TriggerDistance) switch
        {
            EdgePosition.Top => _taskbarSettings.TopEdge,
            EdgePosition.Bottom => _taskbarSettings.BottomEdge,
            _ => EdgeFunction.None
        };
        if (function == EdgeFunction.None) return false;

        if (Math.Sign(delta) != Math.Sign(_wheelRemainder)) _wheelRemainder = 0;
        _wheelRemainder += delta;
        while (Math.Abs(_wheelRemainder) >= 120)
        {
            var up = _wheelRemainder > 0;
            _wheelRemainder -= up ? 120 : -120;
            if (function == EdgeFunction.VirtualDesktops)
            {
                if (up) VirtualDesktopSwitcher.SwitchToPreviousDesktop();
                else VirtualDesktopSwitcher.SwitchToNextDesktop();
            }
            else _windowSwitcher.Move(forward: !up);
        }
        return true;
    }

    private bool HandleTaskbarButton(SharedMouseButton button, bool isDown)
    {
        if (_suppressButtonUntilUp == button && !isDown)
        {
            _suppressButtonUntilUp = null;
            return true;
        }
        if (!_windowSwitcher.IsActive || !isDown) return false;
        var selectButton = _taskbarSettings.SelectButton switch
        {
            WindowSwitcherSelectButton.Left => SharedMouseButton.Left,
            WindowSwitcherSelectButton.Middle => SharedMouseButton.Middle,
            _ => SharedMouseButton.Right
        };
        if (button == selectButton)
        {
            _suppressButtonUntilUp = button;
            _windowSwitcher.Select();
            return true;
        }
        _windowSwitcher.Cancel();
        return false;
    }

    private static bool TryMapButton(SharedMouseButton value, out CoreMouseButtons button)
    {
        button = value switch
        {
            SharedMouseButton.Left => CoreMouseButtons.Left,
            SharedMouseButton.Right => CoreMouseButtons.Right,
            SharedMouseButton.Middle => CoreMouseButtons.Middle,
            SharedMouseButton.X1 => CoreMouseButtons.X1,
            SharedMouseButton.X2 => CoreMouseButtons.X2,
            _ => CoreMouseButtons.None
        };
        return button != CoreMouseButtons.None;
    }

    private void OnEscapePressed(object? sender, EventArgs e) => _windowSwitcher.Cancel();

    public void Dispose()
    {
        _mouseHook.Dispose();
        _momentumController.Dispose();
        _keyboardHook.EscapePressed -= OnEscapePressed;
        _keyboardHook.Dispose();
    }

    private static class NativeMethods
    {
        public static int DoubleClickWidth => GetSystemMetrics(36);
        public static int DoubleClickHeight => GetSystemMetrics(37);
        public static uint DoubleClickTime => GetDoubleClickTime();
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] private static extern uint GetDoubleClickTime();
    }
}

public static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "JeevansWindowsTools";

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true) ?? Registry.CurrentUser.CreateSubKey(RunKey, true);
        if (!enabled) { key.DeleteValue(ValueName, false); return; }
        var executable = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "JeevansWindowsTools.exe");
        var command = string.Equals(Path.GetFileName(executable), "dotnet.exe", StringComparison.OrdinalIgnoreCase)
            ? $"\"{executable}\" \"{Path.Combine(AppContext.BaseDirectory, "JeevansWindowsTools.dll")}\" --startup"
            : $"\"{executable}\" --startup";
        key.SetValue(ValueName, command);
    }
}
