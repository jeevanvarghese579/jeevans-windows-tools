namespace MomentumScroll.Core;

[Flags]
public enum MouseButtons { None = 0, Left = 1, Right = 2, Middle = 4, X1 = 8, X2 = 16 }

/// <summary>Routes verified physical mouse input without altering the momentum algorithm.</summary>
public sealed class MomentumInputRouter
{
    private readonly MomentumEngine _engine;
    private MouseButtons _down;
    private MouseButtons _buttonsAtMomentumStart;
    public MomentumInputRouter(MomentumEngine engine) => _engine = engine;
    public void Wheel(int delta, bool isPhysicalMouse)
    {
        if (!isPhysicalMouse) return;
        var wasActive = _engine.IsActive;
        _engine.PhysicalWheel(delta);
        if (!wasActive && _engine.IsActive) _buttonsAtMomentumStart = _down;
    }
    public void ButtonDown(MouseButtons button)
    {
        if ((_down & button) != 0) return;
        _down |= button;
        if (_engine.IsActive && (_buttonsAtMomentumStart & button) == 0) _engine.Stop("New mouse button press");
    }
    public void ButtonUp(MouseButtons button) => _down &= ~button;
}
