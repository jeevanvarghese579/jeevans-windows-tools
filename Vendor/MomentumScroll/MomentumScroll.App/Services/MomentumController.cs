using System.Windows.Threading;
using CoreMouseButtons = MomentumScroll.Core.MouseButtons;
using MomentumScroll.Core;
namespace MomentumScroll.App.Services;
public sealed class MomentumController : IDisposable
{
    private readonly InputInjector _injector = new(); private readonly DispatcherTimer _timer; private readonly MomentumInputRouter _router;
    public MomentumController(MomentumSettings settings) { Engine = new MomentumEngine(settings); _router = new MomentumInputRouter(Engine); _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(16) }; _timer.Tick += OnTick; }
    public MomentumEngine Engine { get; }
    public InputInjector Injector => _injector;
    public int LastPhysicalDelta { get; private set; } public int LastInjectedDelta { get; private set; }
    public string InjectionStatus { get; private set; } = "Ready";
    public event Action? Changed;
    public bool Enable() { _timer.Start(); return true; }
    public void Disable() { Engine.Stop("Disabled"); _timer.Stop(); Changed?.Invoke(); }
    public void Apply(MomentumSettings settings) => Engine.ApplySettings(settings);
    public void HandlePhysicalWheel(int delta, bool horizontal) => OnWheel(delta, horizontal);
    public void HandleMouseButton(CoreMouseButtons button, bool down) => OnButton(button, down);
    private void OnWheel(int delta, bool horizontal)
    {
        if (horizontal && !Engine.Settings.HorizontalScrolling) return; LastPhysicalDelta = delta;
        var adjusted = Engine.Settings.NaturalScrolling ? delta : -delta; _router.Wheel(adjusted, true); Changed?.Invoke();
    }
    private void OnButton(CoreMouseButtons button, bool down) { if (down) _router.ButtonDown(button); else _router.ButtonUp(button); Changed?.Invoke(); }
    private void OnTick(object? sender, EventArgs e) { var delta = Engine.Tick(); if (delta != 0) { if (ElevationService.IsForegroundElevated() && !ElevationService.IsCurrentProcessElevated()) { InjectionStatus = "Target application is elevated. Momentum injection is unavailable."; Logger.Info(InjectionStatus); Engine.Stop("Elevated target"); } else { InjectionStatus = "Ready"; LastInjectedDelta = delta; _injector.Inject(delta, false); } } Changed?.Invoke(); }
    public void Dispose() { _timer.Stop(); _timer.Tick -= OnTick; }
}
