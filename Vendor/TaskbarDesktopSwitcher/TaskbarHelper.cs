using System;
using System.Runtime.InteropServices;

namespace TaskbarDesktopSwitcher
{
    public enum EdgePosition
    {
        None,
        Top,
        Bottom
    }

    public static class TaskbarHelper
    {
        // Physical pixels are used throughout: the app is PerMonitorV2 DPI aware.
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT point, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO monitorInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        public static EdgePosition GetCursorEdge(int triggerDistance)
        {
            try
            {
                if (!GetCursorPos(out var cursor)) return EdgePosition.None;

                var monitor = MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST);
                var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return EdgePosition.None;

                var distance = Math.Clamp(triggerDistance, 5, 200);
                // Both tests use the identical inclusive edge range on the monitor under the cursor.
                if (cursor.Y <= info.rcMonitor.Top + distance) return EdgePosition.Top;
                if (cursor.Y >= info.rcMonitor.Bottom - distance) return EdgePosition.Bottom;
            }
            catch { }

            return EdgePosition.None;
        }
    }
}
