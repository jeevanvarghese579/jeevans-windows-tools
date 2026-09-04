using System.Diagnostics;

namespace MomentumScroll.Core;

public sealed class MomentumEngine
{
    private readonly Queue<(long Timestamp, int Direction)> _events = new();
    private long _lastTick;
    private double _fraction;
    public MomentumEngine(MomentumSettings settings) => Settings = settings;
    public MomentumSettings Settings { get; private set; }
    public double Velocity { get; private set; }
    public bool IsActive => Math.Abs(Velocity) >= Settings.MinimumStopVelocity;
    public int FlickCount => _events.Count;
    public string LastStopReason { get; private set; } = "None";
    public void ApplySettings(MomentumSettings settings) { Settings = settings; if (Math.Abs(Velocity) > settings.MaximumVelocity) Velocity = Math.Sign(Velocity) * settings.MaximumVelocity; }
    public void Stop(string reason = "Stopped") { Velocity = 0; _fraction = 0; _events.Clear(); LastStopReason = reason; }
    public void PhysicalWheel(int delta, long? timestamp = null)
    {
        if (delta == 0 || !Settings.Enabled) return;
        var now = timestamp ?? Stopwatch.GetTimestamp(); var dir = Math.Sign(delta);
        if (IsActive)
        {
            if (Math.Sign(Velocity) != dir && Settings.OppositeDirectionStops) { Stop("Opposite physical wheel"); return; }
            if (Math.Sign(Velocity) == dir && Settings.SameDirectionBoosts) { Velocity = Clamp(Velocity + dir * Math.Abs(delta) * 3 * Settings.MomentumStrength); _lastTick = now; return; }
        }
        _events.Enqueue((now, dir)); Trim(now);
        var same = _events.Count(e => e.Direction == dir);
        var requiredNotches = Math.Max(1, Settings.MinimumFlickNotches - (Settings.ActivationSensitivity - 1));
        if (same >= requiredNotches)
        {
            Velocity = Clamp(dir * Math.Max(Math.Abs(Velocity), 600 * Settings.MomentumStrength));
            _lastTick = now; LastStopReason = "Momentum started"; _events.Clear();
        }
    }
    public int Tick(long? timestamp = null)
    {
        if (!IsActive || !Settings.Enabled) return 0;
        var now = timestamp ?? Stopwatch.GetTimestamp(); var elapsed = _lastTick == 0 ? 0.016 : (now - _lastTick) / (double)Stopwatch.Frequency;
        _lastTick = now; elapsed = Math.Clamp(elapsed, 0.001, 0.10);
        _fraction += Velocity * elapsed;
        var output = (int)Math.Truncate(_fraction); _fraction -= output;
        var factor = Math.Pow(Settings.FrictionPerTick, elapsed / 0.016);
        Velocity *= factor;
        if (Math.Abs(Velocity) < Settings.MinimumStopVelocity) Stop("Velocity below threshold");
        return output;
    }
    private void Trim(long now) { var age = (long)(Settings.FlickDetectionWindowMs / 1000d * Stopwatch.Frequency); while (_events.Count > 0 && now - _events.Peek().Timestamp > age) _events.Dequeue(); }
    private double Clamp(double value) => Math.Clamp(value, -Settings.MaximumVelocity, Settings.MaximumVelocity);
}
