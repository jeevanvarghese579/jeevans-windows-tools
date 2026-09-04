using System.Runtime.InteropServices;
namespace MomentumScroll.App.Services;
internal static class NativeMethods
{
    internal const int WhMouseLl = 14, WmMouseWheel = 0x020A, WmMouseHWheel = 0x020E, WmLButtonDown = 0x0201, WmLButtonUp = 0x0202, WmRButtonDown = 0x0204, WmRButtonUp = 0x0205, WmMButtonDown = 0x0207, WmMButtonUp = 0x0208, WmXButtonDown = 0x020B, WmXButtonUp = 0x020C, LlmhfInjected = 0x00000001;
    internal const uint InputMouse = 0, MouseeventfWheel = 0x0800, MouseeventfHWheel = 0x1000;
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct MsllHookStruct { public Point Pt; public uint MouseData; public uint Flags; public uint Time; public nint DwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] internal struct Input { public uint Type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion { [FieldOffset(0)] public MouseInput Mi; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseInput { public int Dx; public int Dy; public uint MouseData; public uint DwFlags; public uint Time; public nuint DwExtraInfo; }
    internal delegate nint HookProc(int code, nint wParam, nint lParam);
    [DllImport("user32.dll", SetLastError = true)] internal static extern nint SetWindowsHookEx(int idHook, HookProc callback, nint module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, Input[] inputs, int size);
}
