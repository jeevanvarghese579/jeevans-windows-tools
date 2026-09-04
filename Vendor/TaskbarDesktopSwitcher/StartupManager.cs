using System;
using Microsoft.Win32;

namespace TaskbarDesktopSwitcher
{
    /// <summary>
    /// Manages the "Start with Windows" functionality using the Windows Registry.
    /// Uses the HKCU\Software\Microsoft\Windows\CurrentVersion\Run key for reliability.
    /// </summary>
    public class StartupManager
    {
        // Registry path for startup programs (Current User - doesn't require admin rights)
        private const string RunRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "TaskbarDesktopSwitcher";

        /// <summary>
        /// Checks if the app is configured to start with Windows.
        /// </summary>
        /// <returns>True if enabled, false otherwise.</returns>
        public bool IsStartWithWindowsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(AppName);
                        return value != null;
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle exceptions to prevent crashes
            }

            return false;
        }

        /// <summary>
        /// Enables the app to start with Windows by adding a registry entry.
        /// </summary>
        public void EnableStartWithWindows()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
                {
                    if (key != null)
                    {
                        // Get the path to the current executable
                        var exePath = GetExecutablePath();
                        key.SetValue(AppName, exePath);
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle exceptions to prevent crashes
            }
        }

        /// <summary>
        /// Disables the app from starting with Windows by removing the registry entry.
        /// </summary>
        public void DisableStartWithWindows()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle exceptions to prevent crashes
            }
        }

        /// <summary>
        /// Gets the full path to the current executable.
        /// </summary>
        private string GetExecutablePath()
        {
            return Environment.ProcessPath ?? 
                   System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? 
                   string.Empty;
        }

        /// <summary>
        /// Checks if the app is configured to start minimized to tray.
        /// </summary>
        public bool IsStartMinimizedEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(AppName + "_StartMinimized");
                        return value != null && value.ToString() == "true";
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle exceptions to prevent crashes
            }

            return false;
        }

        /// <summary>
        /// Enables starting the app minimized to tray.
        /// </summary>
        public void EnableStartMinimized()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue(AppName + "_StartMinimized", "true");
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle exceptions to prevent crashes
            }
        }

        /// <summary>
        /// Disables starting the app minimized to tray.
        /// </summary>
        public void DisableStartMinimized()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(AppName + "_StartMinimized", false);
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle exceptions to prevent crashes
            }
        }
    }
}
