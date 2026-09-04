using System.Runtime.InteropServices;
namespace BlankUpper.Services;
public sealed class KeyboardInputService
{
    public void SendAltUp()
    {
        // Arrow keys are extended keys. Without this flag Windows may treat VK_UP as keypad 8.
        var inputs = new[] { Key(NativeMethods.VK_MENU, 0), Key(NativeMethods.VK_UP, NativeMethods.KEYEVENTF_EXTENDEDKEY), Key(NativeMethods.VK_UP, NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP), Key(NativeMethods.VK_MENU, NativeMethods.KEYEVENTF_KEYUP) };
        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        if (sent != inputs.Length)
        {
            // A partial send must never leave Alt logically down.
            NativeMethods.SendInput(2, new[] { Key(NativeMethods.VK_UP, NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP), Key(NativeMethods.VK_MENU, NativeMethods.KEYEVENTF_KEYUP) }, Marshal.SizeOf<NativeMethods.INPUT>());
            AppLogger.Error($"SendInput sent {sent} of {inputs.Length} keys (Win32 error {Marshal.GetLastWin32Error()}).");
        }
    }
    private static NativeMethods.INPUT Key(uint key, uint flags) => new() { type = NativeMethods.INPUT_KEYBOARD, U = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wVk = (ushort)key, dwFlags = flags } } };
}
