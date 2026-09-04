using System;
using System.Runtime.InteropServices;

namespace TaskbarDesktopSwitcher
{
    public sealed class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x0100, VK_ESCAPE = 0x1B, LLKHF_INJECTED = 0x10;
        private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc proc, IntPtr module, uint thread);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string? name);
        [StructLayout(LayoutKind.Sequential)] private struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr dwExtraInfo; }
        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hook;
        public event EventHandler? EscapePressed;
        public KeyboardHook() => _proc = Callback;
        public void Start() { if (_hook != IntPtr.Zero) return; using var process = System.Diagnostics.Process.GetCurrentProcess(); _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(process.MainModule?.ModuleName), 0); }
        public void Stop() { if (_hook != IntPtr.Zero) { UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; } }
        private IntPtr Callback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                if (data.vkCode == VK_ESCAPE && (data.flags & LLKHF_INJECTED) == 0) EscapePressed?.Invoke(this, EventArgs.Empty);
            }
            return CallNextHookEx(_hook, code, wParam, lParam);
        }
        public void Dispose() { Stop(); GC.SuppressFinalize(this); }
    }
}
