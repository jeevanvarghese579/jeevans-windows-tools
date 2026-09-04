using System.Runtime.InteropServices;
using MomentumScroll.Core;
using CoreMouseButtons = MomentumScroll.Core.MouseButtons;
namespace MomentumScroll.App.Services;
public sealed class MouseHookService : IDisposable
{
    public static readonly nuint InjectionMarker = 0x4D53434Cu;
    private readonly NativeMethods.HookProc _callback;
    private nint _hook;
    public MouseHookService() => _callback = HookCallback;
    public bool IsInstalled => _hook != 0;
    public nint Handle => _hook;
    public int LastWin32Error { get; private set; }
    public event Action<int, bool>? PhysicalWheel;
    public event Action<CoreMouseButtons, bool>? MouseButton;
    public bool Install()
    {
        if (IsInstalled) return true;
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, _callback, 0, 0);
        if (_hook != 0) { Logger.Info("Mouse hook installed."); return true; }
        LastWin32Error = Marshal.GetLastWin32Error(); Logger.Error($"Mouse hook installation failed: {LastWin32Error}"); return false;
    }
    public void Remove() { if (_hook == 0) return; if (!NativeMethods.UnhookWindowsHookEx(_hook)) { LastWin32Error = Marshal.GetLastWin32Error(); Logger.Error($"Mouse hook removal failed: {LastWin32Error}"); } else Logger.Info("Mouse hook removed."); _hook = 0; }
    private nint HookCallback(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && (wParam == NativeMethods.WmMouseWheel || wParam == NativeMethods.WmMouseHWheel))
        {
            var data = Marshal.PtrToStructure<NativeMethods.MsllHookStruct>(lParam);
            var injectedByUs = data.DwExtraInfo == unchecked((nint)InjectionMarker);
            if (!injectedByUs) PhysicalWheel?.Invoke(unchecked((short)(data.MouseData >> 16)), wParam == NativeMethods.WmMouseHWheel);
        }
        else if (code >= 0 && TryGetButton((int)wParam, lParam, out var button, out var isDown)) MouseButton?.Invoke(button, isDown);
        return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
    }
    private static bool TryGetButton(int message, nint lParam, out CoreMouseButtons button, out bool down)
    {
        down = message is NativeMethods.WmLButtonDown or NativeMethods.WmRButtonDown or NativeMethods.WmMButtonDown or NativeMethods.WmXButtonDown;
        button = message switch { NativeMethods.WmLButtonDown or NativeMethods.WmLButtonUp => CoreMouseButtons.Left, NativeMethods.WmRButtonDown or NativeMethods.WmRButtonUp => CoreMouseButtons.Right, NativeMethods.WmMButtonDown or NativeMethods.WmMButtonUp => CoreMouseButtons.Middle, NativeMethods.WmXButtonDown or NativeMethods.WmXButtonUp => ((unchecked((short)(Marshal.PtrToStructure<NativeMethods.MsllHookStruct>(lParam).MouseData >> 16)) & 0xFFFF) == 1 ? CoreMouseButtons.X1 : CoreMouseButtons.X2), _ => CoreMouseButtons.None };
        return button != CoreMouseButtons.None;
    }
    public void Dispose() => Remove();
}
