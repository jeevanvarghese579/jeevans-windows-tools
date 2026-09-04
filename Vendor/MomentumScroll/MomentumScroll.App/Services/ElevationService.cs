using System.Diagnostics;
using System.Runtime.InteropServices;
namespace MomentumScroll.App.Services;
public static class ElevationService
{
    private const uint ProcessQueryLimitedInformation = 0x1000, TokenQuery = 0x0008;
    private enum TokenInformationClass { TokenElevation = 20 }
    [StructLayout(LayoutKind.Sequential)] private struct TokenElevation { public int Elevated; }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern nint OpenProcess(uint access, bool inherit, uint processId);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(nint process, uint access, out nint token);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool GetTokenInformation(nint token, TokenInformationClass type, out TokenElevation info, int length, out int returned);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    public static bool IsCurrentProcessElevated() => IsProcessElevated((uint)Environment.ProcessId);
    public static bool IsForegroundElevated()
    {
        var hwnd = GetForegroundWindow(); if (hwnd == 0) return false; GetWindowThreadProcessId(hwnd, out var pid); return pid != 0 && IsProcessElevated(pid);
    }
    private static bool IsProcessElevated(uint pid)
    {
        var process = OpenProcess(ProcessQueryLimitedInformation, false, pid); if (process == 0) return false;
        try { if (!OpenProcessToken(process, TokenQuery, out var token)) return false; try { return GetTokenInformation(token, TokenInformationClass.TokenElevation, out var elevation, Marshal.SizeOf<TokenElevation>(), out _) && elevation.Elevated != 0; } finally { CloseHandle(token); } } finally { CloseHandle(process); }
    }
}
