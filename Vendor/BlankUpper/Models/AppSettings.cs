namespace BlankUpper.Models;
public sealed class AppSettings
{
    public bool IsEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool RunMinimized { get; set; }
    public bool ShowNotification { get; set; } = true;
}
