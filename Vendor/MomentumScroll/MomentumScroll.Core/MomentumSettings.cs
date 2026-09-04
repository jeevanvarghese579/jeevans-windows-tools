namespace MomentumScroll.Core;

public sealed class MomentumSettings
{
    public bool Enabled { get; set; } = true;
    public int ActivationSensitivity { get; set; } = 1;
    public int MinimumFlickNotches { get; set; } = 2;
    public int FlickDetectionWindowMs { get; set; } = 300;
    public double MomentumStrength { get; set; } = 1.0;
    public double FrictionPerTick { get; set; } = 0.96;
    public double MaximumVelocity { get; set; } = 1800;
    public double MinimumStopVelocity { get; set; } = 25;
    public bool OppositeDirectionStops { get; set; } = true;
    public bool SameDirectionBoosts { get; set; } = true;
    public bool NaturalScrolling { get; set; } = true;
    public bool HorizontalScrolling { get; set; }
    public bool StartMinimized { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public string Theme { get; set; } = "System";
    public static MomentumSettings Defaults() => new();
}
