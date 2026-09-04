using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TaskbarDesktopSwitcher
{
    public sealed class WindowSwitcher
    {
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_CONTROL = 0x11, VK_MENU = 0x12, VK_TAB = 0x09, VK_SHIFT = 0x10, VK_RETURN = 0x0D;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, INPUT[] inputs, int inputSize);

        // INPUT contains an aligned union whose native size is determined by MOUSEINPUT (32 bytes).
        // The resulting INPUT is 40 bytes on x64, as required by user32!SendInput.
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT { public uint type; public INPUTUNION union; }
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        private struct INPUTUNION { [FieldOffset(0)] public KEYBDINPUT keyboard; }
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        public bool IsActive { get; private set; }

        public void Move(bool forward)
        {
            if (!IsActive)
            {
                // Ctrl down, Alt down, Tab down/up, Alt up, Ctrl up.
                if (!SendCombination(VK_CONTROL, VK_MENU, VK_TAB)) return;
                IsActive = true;

                // Windows selects the next item when Ctrl+Alt+Tab opens. Do not send a second Tab
                // for the initial forward wheel step, which would skip an application.
                if (!forward) SendCombination(VK_SHIFT, VK_TAB);
                return;
            }

            SendCombination(forward ? new[] { VK_TAB } : new[] { VK_SHIFT, VK_TAB });
        }

        public void Select()
        {
            if (IsActive) SendCombination(VK_RETURN);
            IsActive = false;
        }

        public void Cancel() => IsActive = false;

        private static bool SendCombination(params ushort[] keys)
        {
            var inputs = new INPUT[keys.Length * 2];
            for (var i = 0; i < keys.Length; i++) inputs[i] = CreateInput(keys[i], false);
            for (var i = 0; i < keys.Length; i++) inputs[keys.Length + i] = CreateInput(keys[keys.Length - 1 - i], true);

            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            if (sent == inputs.Length) return true;

            Debug.WriteLine($"Taskbar Desktop Switcher: SendInput sent {sent}/{inputs.Length}; error {Marshal.GetLastWin32Error()}");
            return false;
        }

        private static INPUT CreateInput(ushort key, bool keyUp) => new()
        {
            type = INPUT_KEYBOARD,
            union = new INPUTUNION { keyboard = new KEYBDINPUT { wVk = key, dwFlags = keyUp ? KEYEVENTF_KEYUP : 0 } }
        };
    }
}
