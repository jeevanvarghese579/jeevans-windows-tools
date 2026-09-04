using System.Windows.Interop;
using System.IO;
using BlankUpper.Models;
using BlankUpper.Services;
using Microsoft.Win32;
using MomentumScroll.App.Services;
using MomentumScroll.Core;
using TaskbarDesktopSwitcher;
using BlankMouseHook = BlankUpper.Services.MouseHookService;
using MomentumMouseMonitor = MomentumScroll.App.Services.RawInputMouseMonitor;
using TaskbarMouseHook = TaskbarDesktopSwitcher.MouseHook;

namespace JeevansWindowsTools.Services;

public sealed class FeatureHost : IDisposable
{
    private readonly SettingsService _blankStore = new();
    private readonly BlankMouseHook _blankHook = new(new ExplorerDetectionService(), new KeyboardInputService());
    private readonly SettingsStore _momentumStore = new();
    private readonly MomentumController _momentumController;
    private MomentumMouseMonitor? _rawInput;
    private readonly EdgeSettings _taskbarSettings = new();
    private readonly TaskbarMouseHook _taskbarHook = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly WindowSwitcher _windowSwitcher = new();
    private int _wheelRemainder;
    private HookMouseButton? _suppressButtonUntilUp;
    private bool _masterEnabled;

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
        _blankHook.Activated += OnBlankActivated;
        _taskbarHook.WheelScrolled += OnTaskbarWheel;
        _taskbarHook.MouseButtonChanged += OnTaskbarButton;
        _keyboardHook.EscapePressed += (_, _) => _windowSwitcher.Cancel();
        ApplyEffectiveStates();
    }

    public void AttachWindow(nint handle) => _rawInput ??= new MomentumMouseMonitor(handle, _momentumController.RecordRawMouseWheel);

    public void SetMasterEnabled(bool enabled)
    {
        _masterEnabled = enabled;
        ApplyEffectiveStates();
    }

    public void SetBlankEnabled(bool enabled)
    {
        BlankSettings.IsEnabled = enabled;
        _blankStore.Save(BlankSettings);
        _blankHook.SetEnabled(_masterEnabled && enabled);
    }

    public void SaveBlankSettings() => _blankStore.Save(BlankSettings);

    public void SetMomentumEnabled(bool enabled)
    {
        MomentumSettings.Enabled = enabled;
        _momentumStore.Save(MomentumSettings);
        if (_masterEnabled && enabled) _momentumController.Enable(); else _momentumController.Disable();
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
    }

    public void SaveTaskbarEdges(EdgeFunction top, EdgeFunction bottom) => _taskbarSettings.Save(top, bottom);
    public void SaveTaskbarSelectButton(WindowSwitcherSelectButton button) => _taskbarSettings.SaveWindowSwitcherSelectButton(button);

    private void OnBlankActivated(object? sender, EventArgs e) => BlankActivated?.Invoke(this, e);

    private void ApplyEffectiveStates()
    {
        _blankHook.SetEnabled(_masterEnabled && BlankSettings.IsEnabled);
        if (_masterEnabled && MomentumSettings.Enabled) _momentumController.Enable(); else _momentumController.Disable();
        ApplyTaskbarState();
    }

    private void ApplyTaskbarState()
    {
        var enabled = _masterEnabled && _taskbarSettings.IsSwitcherEnabled;
        if (enabled)
        {
            _taskbarHook.Start();
            _keyboardHook.Start();
        }
        else
        {
            _taskbarHook.Stop();
            _keyboardHook.Stop();
            _windowSwitcher.Cancel();
        }
    }

    private void OnTaskbarWheel(object? sender, WheelEventArgs e)
    {
        var function = TaskbarHelper.GetCursorEdge(_taskbarSettings.TriggerDistance) switch
        {
            EdgePosition.Top => _taskbarSettings.TopEdge,
            EdgePosition.Bottom => _taskbarSettings.BottomEdge,
            _ => EdgeFunction.None
        };
        if (function == EdgeFunction.None) return;

        e.Handled = true;
        if (Math.Sign(e.Delta) != Math.Sign(_wheelRemainder)) _wheelRemainder = 0;
        _wheelRemainder += e.Delta;
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
    }

    private void OnTaskbarButton(object? sender, HookMouseButtonEventArgs e)
    {
        if (_suppressButtonUntilUp == e.Button && !e.IsDown)
        {
            e.Handled = true;
            _suppressButtonUntilUp = null;
            return;
        }
        if (!_windowSwitcher.IsActive || !e.IsDown) return;
        var selectButton = _taskbarSettings.SelectButton switch
        {
            WindowSwitcherSelectButton.Left => HookMouseButton.Left,
            WindowSwitcherSelectButton.Middle => HookMouseButton.Middle,
            _ => HookMouseButton.Right
        };
        if (e.Button == selectButton)
        {
            _suppressButtonUntilUp = e.Button;
            e.Handled = true;
            _windowSwitcher.Select();
        }
        else _windowSwitcher.Cancel();
    }

    public void Dispose()
    {
        _rawInput?.Dispose();
        _blankHook.Activated -= OnBlankActivated;
        _blankHook.Dispose();
        _momentumController.Dispose();
        _taskbarHook.Dispose();
        _keyboardHook.Dispose();
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
