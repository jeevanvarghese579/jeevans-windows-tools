using Microsoft.Win32;

namespace TaskbarDesktopSwitcher
{
    public enum EdgeFunction { None, VirtualDesktops, WindowSwitching }
    public enum WindowSwitcherSelectButton { Left, Right, Middle }

    public sealed class EdgeSettings
    {
        private const string SettingsPath = @"SOFTWARE\TaskbarDesktopSwitcher";
        public EdgeFunction TopEdge { get; private set; } = EdgeFunction.None;
        public EdgeFunction BottomEdge { get; private set; } = EdgeFunction.VirtualDesktops;
        public bool IsSwitcherEnabled { get; private set; } = true;
        public int TriggerDistance { get; private set; } = 30;
        public WindowSwitcherSelectButton SelectButton { get; private set; } = WindowSwitcherSelectButton.Right;

        public void Load()
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsPath, false);
            TopEdge = Read(key, "TopEdge", EdgeFunction.None);
            BottomEdge = Read(key, "BottomEdge", EdgeFunction.VirtualDesktops);
            IsSwitcherEnabled = ReadBool(key, "IsSwitcherEnabled", true);
            TriggerDistance = Math.Clamp(ReadInt(key, "TriggerDistance", 30), 5, 200);
            SelectButton = Read(key, "WindowSwitcherSelectButton", WindowSwitcherSelectButton.Right);
        }

        public void Save(EdgeFunction topEdge, EdgeFunction bottomEdge)
        {
            TopEdge = topEdge; BottomEdge = bottomEdge;
            using var key = Registry.CurrentUser.CreateSubKey(SettingsPath, true);
            key?.SetValue("TopEdge", topEdge.ToString());
            key?.SetValue("BottomEdge", bottomEdge.ToString());
        }

        public void SaveEnabled(bool enabled)
        {
            IsSwitcherEnabled = enabled;
            using var key = Registry.CurrentUser.CreateSubKey(SettingsPath, true);
            key?.SetValue("IsSwitcherEnabled", enabled ? "true" : "false");
        }

        public void SaveWindowSwitcherSelectButton(WindowSwitcherSelectButton button)
        {
            SelectButton = button;
            using var key = Registry.CurrentUser.CreateSubKey(SettingsPath, true);
            key?.SetValue("WindowSwitcherSelectButton", button.ToString());
        }

        private static EdgeFunction Read(RegistryKey? key, string name, EdgeFunction fallback) =>
            key?.GetValue(name) is string value && Enum.TryParse<EdgeFunction>(value, out var result) ? result : fallback;
        private static WindowSwitcherSelectButton Read(RegistryKey? key, string name, WindowSwitcherSelectButton fallback) =>
            key?.GetValue(name) is string value && Enum.TryParse<WindowSwitcherSelectButton>(value, out var result) ? result : fallback;
        private static bool ReadBool(RegistryKey? key, string name, bool fallback) =>
            key?.GetValue(name) is string value && bool.TryParse(value, out var result) ? result : fallback;
        private static int ReadInt(RegistryKey? key, string name, int fallback) =>
            key?.GetValue(name) is string value && int.TryParse(value, out var result) ? result : fallback;
    }
}
