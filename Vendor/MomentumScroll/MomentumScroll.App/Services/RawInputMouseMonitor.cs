using System.Runtime.InteropServices;
using System.Windows.Interop;
namespace MomentumScroll.App.Services;
/// <summary>Records real HID mouse-wheel packets; precision touchpads normally do not emit these.</summary>
public sealed class RawInputMouseMonitor : IDisposable
{
    private const int WmInput = 0x00FF, RidInput = 0x10000003, RimTypeMouse = 0;
    private const ushort RiMouseWheel = 0x0400, RiMouseHWheel = 0x0800;
    private readonly Action _onWheel; private HwndSource? _source;
    [StructLayout(LayoutKind.Sequential)] private struct RawInputDevice { public ushort UsagePage; public ushort Usage; public uint Flags; public nint Target; }
    [StructLayout(LayoutKind.Explicit)] private struct RawMouseButtons
    {
        [FieldOffset(0)] public uint Buttons;
        [FieldOffset(0)] public ushort ButtonFlags;
        [FieldOffset(2)] public ushort ButtonData;
    }
    [StructLayout(LayoutKind.Sequential)] private struct RawMouse { public ushort Flags; public RawMouseButtons Buttons; public uint RawButtons; public int LastX; public int LastY; public uint ExtraInformation; }
    [StructLayout(LayoutKind.Sequential)] private struct RawInput { public Header Header; public RawMouse Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct Header { public uint Type; public uint Size; public nint Device; public nint WParam; }
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterRawInputDevices(RawInputDevice[] devices, uint number, uint size);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint GetRawInputData(nint input, uint command, nint data, ref uint size, uint headerSize);
    public RawInputMouseMonitor(nint hwnd, Action onWheel) { _onWheel = onWheel; _source = HwndSource.FromHwnd(hwnd); if (_source is null) return; RegisterRawInputDevices([new RawInputDevice { UsagePage = 1, Usage = 2, Flags = 0x100, Target = hwnd }], 1, (uint)Marshal.SizeOf<RawInputDevice>()); _source.AddHook(WndProc); }
    private nint WndProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != WmInput) return 0; uint size = 0; GetRawInputData(lParam, RidInput, 0, ref size, (uint)Marshal.SizeOf<Header>()); if (size == 0) return 0; var memory = Marshal.AllocHGlobal((int)size); try { if (GetRawInputData(lParam, RidInput, memory, ref size, (uint)Marshal.SizeOf<Header>()) == size) { var input = Marshal.PtrToStructure<RawInput>(memory); if (input.Header.Type == RimTypeMouse && (input.Mouse.Buttons.ButtonFlags & (RiMouseWheel | RiMouseHWheel)) != 0) _onWheel(); } } finally { Marshal.FreeHGlobal(memory); } return 0;
    }
    public void Dispose() { if (_source is not null) _source.RemoveHook(WndProc); _source = null; }
}
