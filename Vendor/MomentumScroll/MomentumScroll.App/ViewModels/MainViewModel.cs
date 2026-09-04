using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using MomentumScroll.App.Services;
using MomentumScroll.Core;
namespace MomentumScroll.App.ViewModels;
public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly SettingsStore _store = new(); private readonly MomentumController _controller; private MomentumSettings _settings; private bool _isEnabled; private bool _diagnosticsVisible;
    public MainViewModel()
    {
        _settings = _store.Load(); _isEnabled = _settings.Enabled; _controller = new MomentumController(_settings); _controller.Changed += () => System.Windows.Application.Current.Dispatcher.BeginInvoke(Refresh); Logger.Info("Application started.");
        if (_isEnabled && !_controller.Enable()) { _isEnabled = false; _settings.Enabled = false; Save(); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsEnabled { get => _isEnabled; set { if (_isEnabled == value) return; _isEnabled = value; if (value) { if (!_controller.Enable()) { _isEnabled = false; System.Windows.MessageBox.Show("The global mouse hook could not be installed. See the log for details.", "Momentum Scroll"); } } else _controller.Disable(); _settings.Enabled = _isEnabled; Save(); Refresh(); } }
    public bool DiagnosticsVisible { get => _diagnosticsVisible; set { _diagnosticsVisible = value; OnChanged(); } }
    public string Status => IsEnabled ? "Active" : "Disabled";
    public string HookStatus => _controller.Hook.IsInstalled ? "Installed" : "Not installed";
    public string Direction => Math.Sign(_controller.LastPhysicalDelta) switch { > 0 => "Up", < 0 => "Down", _ => "—" };
    public string Velocity => $"{_controller.Engine.Velocity:0.0} units/s";
    public string MomentumStatus => _controller.Engine.IsActive ? "Active" : "Idle";
    public string LastInjected => _controller.LastInjectedDelta.ToString();
    public void RecordPhysicalMouseWheel() => _controller.RecordRawMouseWheel();
    public string InjectionStatus => _controller.InjectionStatus;
    public int ActivationSensitivity { get => _settings.ActivationSensitivity; set => UpdateSetting(() => _settings.ActivationSensitivity = value); }
    public int MinimumFlickNotches { get => _settings.MinimumFlickNotches; set => UpdateSetting(() => _settings.MinimumFlickNotches = value); }
    public int FlickDetectionWindowMs { get => _settings.FlickDetectionWindowMs; set => UpdateSetting(() => _settings.FlickDetectionWindowMs = value); }
    public double MomentumStrength { get => _settings.MomentumStrength; set => UpdateSetting(() => _settings.MomentumStrength = value); }
    public double FrictionPerTick { get => _settings.FrictionPerTick; set => UpdateSetting(() => _settings.FrictionPerTick = value); }
    public double MaximumVelocity { get => _settings.MaximumVelocity; set => UpdateSetting(() => _settings.MaximumVelocity = value); }
    public double MinimumStopVelocity { get => _settings.MinimumStopVelocity; set => UpdateSetting(() => _settings.MinimumStopVelocity = value); }
    public bool SameDirectionBoosts { get => _settings.SameDirectionBoosts; set => UpdateSetting(() => _settings.SameDirectionBoosts = value); }
    public bool OppositeDirectionStops { get => _settings.OppositeDirectionStops; set => UpdateSetting(() => _settings.OppositeDirectionStops = value); }
    public bool NaturalScrolling { get => _settings.NaturalScrolling; set => UpdateSetting(() => _settings.NaturalScrolling = value); }
    public bool HorizontalScrolling { get => _settings.HorizontalScrolling; set => UpdateSetting(() => _settings.HorizontalScrolling = value); }
    public bool StartMinimized { get => _settings.StartMinimized; set => UpdateSetting(() => _settings.StartMinimized = value); }
    public bool MinimizeToTray { get => _settings.MinimizeToTray; set => UpdateSetting(() => _settings.MinimizeToTray = value); }
    public bool StartWithWindows { get => _settings.StartWithWindows; set { if (_settings.StartWithWindows == value) return; _settings.StartWithWindows = value; SetStartupEntry(); UpdateSetting(() => { }); } }
    public string Theme { get => _settings.Theme; set => UpdateSetting(() => _settings.Theme = value); }
    public string Diagnostics => $"Enabled: {IsEnabled}\nHook installed: {_controller.Hook.IsInstalled}\nHook handle: 0x{_controller.Hook.Handle.ToInt64():X}\nLast Win32 error: {_controller.Hook.LastWin32Error}\nLast physical delta: {_controller.LastPhysicalDelta}\nFlick count: {_controller.Engine.FlickCount}\nVelocity: {_controller.Engine.Velocity:0.0}\nMomentum active: {_controller.Engine.IsActive}\nLast injected delta: {_controller.LastInjectedDelta}\nLast SendInput result: {_controller.Injector.LastResult}\nLast stop reason: {_controller.Engine.LastStopReason}";
    public void RestoreDefaults() { var enabled = IsEnabled; _settings = MomentumSettings.Defaults(); _settings.Enabled = enabled; _controller.Apply(_settings); Save(); Refresh(); OnChanged(string.Empty); }
    private void UpdateSetting(Action update) { update(); _controller.Apply(_settings); Save(); OnChanged(string.Empty); }
    private void SetStartupEntry() { using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true); if (key is null) return; if (_settings.StartWithWindows) key.SetValue("MomentumScroll", $"\"{Environment.ProcessPath}\""); else key.DeleteValue("MomentumScroll", false); }
    private void Save() { try { _store.Save(_settings); Logger.Info("Settings saved."); } catch (Exception x) { Logger.Error("Could not save settings", x); } }
    private void Refresh() { OnChanged(nameof(Status)); OnChanged(nameof(HookStatus)); OnChanged(nameof(Direction)); OnChanged(nameof(Velocity)); OnChanged(nameof(MomentumStatus)); OnChanged(nameof(LastInjected)); OnChanged(nameof(InjectionStatus)); OnChanged(nameof(Diagnostics)); }
    private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public void Dispose() { _settings.Enabled = IsEnabled; Save(); _controller.Dispose(); Logger.Info("Application shutdown."); }
}
