using System;
using System.Runtime.InteropServices;

namespace TaskbarDesktopSwitcher
{
    public enum HookMouseButton { Left, Right, Middle }

    public class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208;
        private const int LLMHF_INJECTED = 0x00000001;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc proc, IntPtr module, uint threadId);
        [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GetModuleHandle(string? moduleName);
        [StructLayout(LayoutKind.Sequential)] private struct MSLLHOOKSTRUCT { public POINT pt; public int mouseData; public int flags; public int time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }

        private readonly LowLevelMouseProc _proc;
        private IntPtr _hookId;
        public event EventHandler<WheelEventArgs>? WheelScrolled;
        public event EventHandler<HookMouseButtonEventArgs>? MouseButtonChanged;
        public MouseHook() => _proc = HookCallback;

        public void Start()
        {
            if (_hookId != IntPtr.Zero) return;
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            if (process.MainModule != null) _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(process.MainModule.ModuleName), 0);
        }
        public void Stop() { if (_hookId != IntPtr.Zero) { UnhookWindowsHookEx(_hookId); _hookId = IntPtr.Zero; } }

        private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code < 0) return CallNextHookEx(_hookId, code, wParam, lParam);
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            if ((data.flags & LLMHF_INJECTED) != 0) return CallNextHookEx(_hookId, code, wParam, lParam);

            if (wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                var args = new WheelEventArgs((short)(data.mouseData >> 16));
                WheelScrolled?.Invoke(this, args);
                if (args.Handled) return (IntPtr)1;
            }
            else if (TryGetButton(wParam, out var button, out var isDown))
            {
                var args = new HookMouseButtonEventArgs(button, isDown);
                MouseButtonChanged?.Invoke(this, args);
                if (args.Handled) return (IntPtr)1;
            }
            return CallNextHookEx(_hookId, code, wParam, lParam);
        }

        private static bool TryGetButton(IntPtr message, out HookMouseButton button, out bool isDown)
        {
            if (message == (IntPtr)WM_LBUTTONDOWN || message == (IntPtr)WM_LBUTTONUP) { button = HookMouseButton.Left; isDown = message == (IntPtr)WM_LBUTTONDOWN; return true; }
            if (message == (IntPtr)WM_RBUTTONDOWN || message == (IntPtr)WM_RBUTTONUP) { button = HookMouseButton.Right; isDown = message == (IntPtr)WM_RBUTTONDOWN; return true; }
            if (message == (IntPtr)WM_MBUTTONDOWN || message == (IntPtr)WM_MBUTTONUP) { button = HookMouseButton.Middle; isDown = message == (IntPtr)WM_MBUTTONDOWN; return true; }
            button = default; isDown = false; return false;
        }

        public void Dispose() { Stop(); GC.SuppressFinalize(this); }
        ~MouseHook() => Stop();
    }

    public class WheelEventArgs : EventArgs { public int Delta { get; } public bool Handled { get; set; } public WheelEventArgs(int delta) => Delta = delta; }
    public class HookMouseButtonEventArgs : EventArgs
    {
        public HookMouseButton Button { get; }
        public bool IsDown { get; }
        public bool Handled { get; set; }
        public HookMouseButtonEventArgs(HookMouseButton button, bool isDown) { Button = button; IsDown = isDown; }
    }
}
