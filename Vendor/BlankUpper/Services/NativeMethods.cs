using System.Runtime.InteropServices;
namespace BlankUpper.Services;
internal static class NativeMethods
{
    internal const int WH_MOUSE_LL = 14, WM_LBUTTONDOWN = 0x0201, SM_CXDOUBLECLK = 36, SM_CYDOUBLECLK = 37;
    internal const uint INPUT_KEYBOARD = 1, KEYEVENTF_EXTENDEDKEY = 1, KEYEVENTF_KEYUP = 2, VK_MENU = 0x12, VK_UP = 0x26;
    internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct MSLLHOOKSTRUCT { public Point pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }
    // INPUT is 40 bytes on x64: the union starts at byte 8, after alignment padding.
    // An incorrectly packed INPUT makes SendInput fail with ERROR_INVALID_PARAMETER (87).
    [StructLayout(LayoutKind.Explicit, Size = 40)] internal struct INPUT { [FieldOffset(0)] public uint type; [FieldOffset(8)] public InputUnion U; }
    [StructLayout(LayoutKind.Explicit, Size = 24)] internal struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] internal struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
    [DllImport("user32.dll")] internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hmod, uint threadId);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hhk, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] internal static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")] internal static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(IntPtr hWnd);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr hObject);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hWnd);
}
