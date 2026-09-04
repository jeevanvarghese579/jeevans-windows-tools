using System.Drawing;
using System.Drawing.Drawing2D;
using Forms = System.Windows.Forms;
namespace BlankUpper.Services;
public sealed class TrayIconService : IDisposable
{
    private readonly MainWindow _window; private readonly App _app; private Forms.NotifyIcon? _icon; private Icon? _appIcon;
    public TrayIconService(MainWindow window, App app) { _window = window; _app = app; }
    public void Initialize()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Blank Upper", null, (_, _) => _window.ShowFromTray());
        menu.Items.Add("Enable/Disable", null, (_, _) => { _app.Settings.IsEnabled = !_app.Settings.IsEnabled; _app.MouseHook.SetEnabled(_app.Settings.IsEnabled); _app.SaveSettings(); _window.RefreshFromSettings(); });
        menu.Items.Add("Start with Windows", null, (_, _) => { _app.Settings.StartWithWindows = !_app.Settings.StartWithWindows; new StartupService().SetEnabled(_app.Settings.StartWithWindows); _app.SaveSettings(); _window.RefreshFromSettings(); });
        menu.Items.Add(new Forms.ToolStripSeparator()); menu.Items.Add("Exit", null, (_, _) => _app.ExitApplication());
        _appIcon = CreateAppIcon();
        _icon = new Forms.NotifyIcon { Icon = _appIcon, Text = "Blank Upper", ContextMenuStrip = menu, Visible = true };
        _icon.DoubleClick += (_, _) => _window.ShowFromTray(); _app.MouseHook.Activated += OnActivated;
    }
    private void OnActivated(object? sender, EventArgs e) { if (_app.Settings.ShowNotification) _icon?.ShowBalloonTip(1000, "Blank Upper", "Navigated to the parent folder.", Forms.ToolTipIcon.Info); }
    public void Refresh() { }
    // A crisp, self-contained 32px tray icon: a purple folder with an upward navigation arrow.
    private static Icon CreateAppIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using var circle = new SolidBrush(Color.FromArgb(255, 89, 55, 180));
            graphics.FillEllipse(circle, 1, 1, 30, 30);
            using var folder = new SolidBrush(Color.FromArgb(255, 216, 203, 255));
            graphics.FillRoundedRectangle(folder, new Rectangle(6, 12, 20, 13), new Size(3, 3));
            graphics.FillRoundedRectangle(folder, new Rectangle(8, 9, 8, 5), new Size(2, 2));
            using var arrow = new SolidBrush(Color.FromArgb(255, 67, 42, 136));
            var points = new[] { new PointF(16, 14), new PointF(11.5f, 18.5f), new PointF(14.5f, 18.5f), new PointF(14.5f, 22), new PointF(17.5f, 22), new PointF(17.5f, 18.5f), new PointF(20.5f, 18.5f) };
            graphics.FillPolygon(arrow, points);
        }
        var handle = bitmap.GetHicon();
        try { using var source = Icon.FromHandle(handle); return (Icon)source.Clone(); }
        finally { NativeMethods.DeleteObject(handle); }
    }
    public void Dispose() { _app.MouseHook.Activated -= OnActivated; if (_icon is not null) { _icon.Visible = false; _icon.ContextMenuStrip?.Dispose(); _icon.Dispose(); _icon = null; } _appIcon?.Dispose(); _appIcon = null; }
}
