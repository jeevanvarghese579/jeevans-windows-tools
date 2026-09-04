using System.Diagnostics;
using System.Runtime.InteropServices;
namespace BlankUpper.Services;
public sealed class MouseHookService : IDisposable
{
    private readonly ExplorerDetectionService _detector; private readonly KeyboardInputService _keyboard;
    private readonly NativeMethods.HookProc _callback; private readonly object _hookLock = new();
    private IntPtr _hook; private long _lastTick; private long _lastActivationTick; private NativeMethods.Point _lastPoint; private int _checking;
    public bool Enabled { get; set; }
    public event EventHandler? Activated;
    public MouseHookService(ExplorerDetectionService detector, KeyboardInputService keyboard) { _detector = detector; _keyboard = keyboard; _callback = HookCallback; }
    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (enabled) Start(); else Stop();
    }
    public void Start()
    {
        lock (_hookLock) { if (_hook != IntPtr.Zero) return; _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _callback, IntPtr.Zero, 0); if (_hook == IntPtr.Zero) AppLogger.Error("Unable to install mouse hook.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())); }
    }
    // The hook does no UI Automation: it records a candidate and schedules work after returning.
    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && wParam.ToInt32() == NativeMethods.WM_LBUTTONDOWN && Enabled)
        {
            var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam); var now = Stopwatch.GetTimestamp();
            var elapsed = (now - Interlocked.Read(ref _lastTick)) * 1000 / Stopwatch.Frequency;
            var dpi = NativeMethods.GetDpiForWindow(NativeMethods.GetForegroundWindow());
            var width = dpi == 0 ? NativeMethods.GetSystemMetrics(NativeMethods.SM_CXDOUBLECLK) : NativeMethods.GetSystemMetricsForDpi(NativeMethods.SM_CXDOUBLECLK, dpi);
            var height = dpi == 0 ? NativeMethods.GetSystemMetrics(NativeMethods.SM_CYDOUBLECLK) : NativeMethods.GetSystemMetricsForDpi(NativeMethods.SM_CYDOUBLECLK, dpi);
            var dx = Math.Abs(data.pt.X - _lastPoint.X); var dy = Math.Abs(data.pt.Y - _lastPoint.Y);
            if (elapsed <= NativeMethods.GetDoubleClickTime() && dx <= width && dy <= height && now - Interlocked.Read(ref _lastActivationTick) > Stopwatch.Frequency / 2 && Interlocked.CompareExchange(ref _checking, 1, 0) == 0)
                _ = CheckCandidateAsync(data.pt);
            _lastPoint = data.pt; Interlocked.Exchange(ref _lastTick, now);
        }
        return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
    }
    private async Task CheckCandidateAsync(NativeMethods.Point point)
    {
        try
        {
            // UI Automation is deliberately off the low-level hook thread.
            var blank = await Task.Run(() => Enabled && _detector.IsConfidentExplorerBlankSpace(point)).ConfigureAwait(false);
            if (blank && Enabled) { _keyboard.SendAltUp(); Interlocked.Exchange(ref _lastActivationTick, Stopwatch.GetTimestamp()); Interlocked.Exchange(ref _lastTick, 0); Activated?.Invoke(this, EventArgs.Empty); }
        }
        catch (Exception ex) { AppLogger.Error("Blank-space detection failed", ex); }
        finally { Interlocked.Exchange(ref _checking, 0); }
    }
    public void Stop() { lock (_hookLock) { if (_hook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; } } }
    public void Dispose() { Enabled = false; Stop(); GC.SuppressFinalize(this); }
}
