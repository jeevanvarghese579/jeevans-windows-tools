using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JeevansWindowsTools.Services;

public enum SharedMouseEventKind { Wheel, HorizontalWheel, ButtonDown, ButtonUp }
public enum SharedMouseButton { None, Left, Right, Middle, X1, X2 }

public readonly record struct SharedMouseEvent(
    SharedMouseEventKind Kind,
    SharedMouseButton Button,
    int WheelDelta,
    int X,
    int Y);

/// <summary>
/// One process-wide low-level mouse hook shared by all three features. Keeping a
/// single hook avoids stacking multiple global observers and makes its lifetime explicit.
/// Injected mouse input is ignored so the app never reacts to its own output.
/// </summary>
public sealed class SharedMouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmMouseWheel = 0x020A, WmMouseHWheel = 0x020E;
    private const int WmLButtonDown = 0x0201, WmLButtonUp = 0x0202;
    private const int WmRButtonDown = 0x0204, WmRButtonUp = 0x0205;
    private const int WmMButtonDown = 0x0207, WmMButtonUp = 0x0208;
    private const int WmXButtonDown = 0x020B, WmXButtonUp = 0x020C;
    private const uint LlmhfInjected = 0x00000001;

    private delegate nint HookProc(int code, nint wParam, nint lParam);
    private readonly HookProc _callback;
    private readonly Func<SharedMouseEvent, bool> _handler;
    private nint _hook;

    public SharedMouseHook(Func<SharedMouseEvent, bool> handler)
    {
        _handler = handler;
        _callback = Callback;
    }

    public bool IsInstalled => _hook != 0;

    public bool Start()
    {
        if (_hook != 0) return true;
        using var process = Process.GetCurrentProcess();
        _hook = SetWindowsHookEx(WhMouseLl, _callback, GetModuleHandle(process.MainModule?.ModuleName), 0);
        return _hook != 0;
    }

    public void Stop()
    {
        if (_hook == 0) return;
        UnhookWindowsHookEx(_hook);
        _hook = 0;
    }

    private nint Callback(int code, nint wParam, nint lParam)
    {
        if (code < 0) return CallNextHookEx(_hook, code, wParam, lParam);
        var data = Marshal.PtrToStructure<MsllHookStruct>(lParam);
        if ((data.Flags & LlmhfInjected) != 0) return CallNextHookEx(_hook, code, wParam, lParam);

        SharedMouseEvent? item = (int)wParam switch
        {
            WmMouseWheel => new(SharedMouseEventKind.Wheel, SharedMouseButton.None, unchecked((short)(data.MouseData >> 16)), data.Point.X, data.Point.Y),
            WmMouseHWheel => new(SharedMouseEventKind.HorizontalWheel, SharedMouseButton.None, unchecked((short)(data.MouseData >> 16)), data.Point.X, data.Point.Y),
            WmLButtonDown => Button(SharedMouseEventKind.ButtonDown, SharedMouseButton.Left, data),
            WmLButtonUp => Button(SharedMouseEventKind.ButtonUp, SharedMouseButton.Left, data),
            WmRButtonDown => Button(SharedMouseEventKind.ButtonDown, SharedMouseButton.Right, data),
            WmRButtonUp => Button(SharedMouseEventKind.ButtonUp, SharedMouseButton.Right, data),
            WmMButtonDown => Button(SharedMouseEventKind.ButtonDown, SharedMouseButton.Middle, data),
            WmMButtonUp => Button(SharedMouseEventKind.ButtonUp, SharedMouseButton.Middle, data),
            WmXButtonDown => Button(SharedMouseEventKind.ButtonDown, XButton(data), data),
            WmXButtonUp => Button(SharedMouseEventKind.ButtonUp, XButton(data), data),
            _ => null
        };

        if (item is { } mouseEvent && _handler(mouseEvent)) return 1;
        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    private static SharedMouseEvent Button(SharedMouseEventKind kind, SharedMouseButton button, MsllHookStruct data) =>
        new(kind, button, 0, data.Point.X, data.Point.Y);

    private static SharedMouseButton XButton(MsllHookStruct data) =>
        unchecked((short)(data.MouseData >> 16)) == 1 ? SharedMouseButton.X1 : SharedMouseButton.X2;

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct MsllHookStruct { public Point Point; public uint MouseData, Flags, Time; public nint ExtraInfo; }
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SetWindowsHookEx(int id, HookProc callback, nint module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] private static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandle(string? moduleName);
}
