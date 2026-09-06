using System.Runtime.InteropServices;
namespace MomentumScroll.App.Services;
public sealed class InputInjector
{
    public static readonly nuint InjectionMarker = 0x4D53434Cu;
    public int LastResult { get; private set; }
    public int LastWin32Error { get; private set; }
    public bool Inject(int delta, bool horizontal)
    {
        if (delta == 0) return true;
        var input = new NativeMethods.Input { Type = NativeMethods.InputMouse, U = new NativeMethods.InputUnion { Mi = new NativeMethods.MouseInput { MouseData = unchecked((uint)delta), DwFlags = horizontal ? NativeMethods.MouseeventfHWheel : NativeMethods.MouseeventfWheel, DwExtraInfo = InjectionMarker } } };
        LastResult = (int)NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.Input>());
        if (LastResult != 1) { LastWin32Error = Marshal.GetLastWin32Error(); Logger.Error($"SendInput failed: {LastWin32Error}"); return false; }
        return true;
    }
}
