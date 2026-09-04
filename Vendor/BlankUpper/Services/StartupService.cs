using Microsoft.Win32;
using System.IO;
namespace BlankUpper.Services;
public sealed class StartupService
{
    private const string KeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run"; private const string ValueName = "BlankUpper";
    public void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true) ?? Registry.CurrentUser.CreateSubKey(KeyPath, true);
            if (enabled) key.SetValue(ValueName, BuildCommandLine());
            else key.DeleteValue(ValueName, false);
        }
        catch (Exception ex) { AppLogger.Error("Could not update startup registration", ex); }
    }
    private static string BuildCommandLine()
    {
        var host = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "BlankUpper.exe");
        // `dotnet run` hosts the app in dotnet.exe; make that development scenario valid too.
        if (string.Equals(Path.GetFileName(host), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            return $"\"{host}\" \"{Path.Combine(AppContext.BaseDirectory, "BlankUpper.dll")}\" --startup";
        return $"\"{host}\" --startup";
    }
}
