using System.Windows.Threading;
using System.Diagnostics;
using CoreMouseButtons = MomentumScroll.Core.MouseButtons;
using MomentumScroll.Core;
namespace MomentumScroll.App.Services;
public sealed class MomentumController : IDisposable
{
    private readonly MouseHookService _hook = new(); private readonly InputInjector _injector = new(); private readonly DispatcherTimer _timer; private readonly MomentumInputRouter _router; private long _lastRawMouseWheel; private bool _rawInputObserved;
    public MomentumController(MomentumSettings settings) { Engine = new MomentumEngine(settings); _router = new MomentumInputRouter(Engine); _hook.PhysicalWheel += OnWheel; _hook.MouseButton += OnButton; _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(16) }; _timer.Tick += OnTick; }
    public MomentumEngine Engine { get; }
    public MouseHookService Hook => _hook; public InputInjector Injector => _injector;
    public int LastPhysicalDelta { get; private set; } public int LastInjectedDelta { get; private set; }
    public string InjectionStatus { get; private set; } = "Ready";
    public event Action? Changed;
    public bool Enable() { if (!_hook.Install()) return false; _timer.Start(); return true; }
    public void Disable() { Engine.Stop("Disabled"); _timer.Stop(); _hook.Remove(); Changed?.Invoke(); }
    public void Apply(MomentumSettings settings) => Engine.ApplySettings(settings);
    public void RecordRawMouseWheel() { _rawInputObserved = true; Interlocked.Exchange(ref _lastRawMouseWheel, Stopwatch.GetTimestamp()); }
    private void OnWheel(int delta, bool horizontal)
    {
        // Raw mouse input is authoritative once available.  Until then, retain the existing
        // low-level-hook behavior so a driver that delivers raw input after the hook cannot disable scrolling.
        var rawIsRecent = Stopwatch.GetTimestamp() - Interlocked.Read(ref _lastRawMouseWheel) <= Stopwatch.Frequency / 8;
        if (_rawInputObserved && !rawIsRecent) return;
        if (horizontal && !Engine.Settings.HorizontalScrolling) return; LastPhysicalDelta = delta;
        var adjusted = Engine.Settings.NaturalScrolling ? delta : -delta; _router.Wheel(adjusted, true); Changed?.Invoke();
    }
    private void OnButton(CoreMouseButtons button, bool down) { if (down) _router.ButtonDown(button); else _router.ButtonUp(button); Changed?.Invoke(); }
    private void OnTick(object? sender, EventArgs e) { var delta = Engine.Tick(); if (delta != 0) { if (ElevationService.IsForegroundElevated() && !ElevationService.IsCurrentProcessElevated()) { InjectionStatus = "Target application is elevated. Momentum injection is unavailable."; Logger.Info(InjectionStatus); Engine.Stop("Elevated target"); } else { InjectionStatus = "Ready"; LastInjectedDelta = delta; _injector.Inject(delta, false); } } Changed?.Invoke(); }
    public void Dispose() { _timer.Stop(); _timer.Tick -= OnTick; _hook.PhysicalWheel -= OnWheel; _hook.MouseButton -= OnButton; _hook.Dispose(); }
}
