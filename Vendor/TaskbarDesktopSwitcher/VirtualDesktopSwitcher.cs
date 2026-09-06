using System;
using System.Runtime.InteropServices;

namespace TaskbarDesktopSwitcher
{
    public static class VirtualDesktopSwitcher
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern ushort MapVirtualKey(ushort uCode, uint uMapType);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const ushort VK_LWIN = 0x5B;
        private const ushort VK_LCONTROL = 0xA2;
        private const ushort VK_LEFT = 0x25;
        private const ushort VK_RIGHT = 0x27;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public KEYBDINPUT ki;
            private uint pad1;
            private uint pad2;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public static void SwitchToNextDesktop()
        {
            try
            {
                SendKeysCombination(VK_LWIN, VK_LCONTROL, VK_RIGHT);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Taskbar Desktop Switcher: " + ex.Message);
            }
        }

        public static void SwitchToPreviousDesktop()
        {
            try
            {
                SendKeysCombination(VK_LWIN, VK_LCONTROL, VK_LEFT);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Taskbar Desktop Switcher: " + ex.Message);
            }
        }

        private static void SendKeysCombination(ushort winKey, ushort ctrlKey, ushort arrowKey)
        {
            INPUT[] inputs = new INPUT[6];

            // Press Win
            inputs[0] = CreateKeyboardInput(winKey, false);
            // Press Ctrl
            inputs[1] = CreateKeyboardInput(ctrlKey, false);
            // Press Arrow
            inputs[2] = CreateKeyboardInput(arrowKey, false);
            // Release Arrow
            inputs[3] = CreateKeyboardInput(arrowKey, true);
            // Release Ctrl
            inputs[4] = CreateKeyboardInput(ctrlKey, true);
            // Release Win
            inputs[5] = CreateKeyboardInput(winKey, true);

            uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (result != inputs.Length) System.Diagnostics.Debug.WriteLine($"Taskbar Desktop Switcher: SendInput sent {result}/{inputs.Length}");
        }

        private static INPUT CreateKeyboardInput(ushort keyCode, bool keyUp)
        {
            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.ki = new KEYBDINPUT();
            input.ki.wVk = keyCode;
            input.ki.wScan = MapVirtualKey(keyCode, 0);
            input.ki.dwFlags = keyUp ? KEYEVENTF_KEYUP : 0u;
            input.ki.time = 0;
            input.ki.dwExtraInfo = IntPtr.Zero;
            return input;
        }

    }
}
