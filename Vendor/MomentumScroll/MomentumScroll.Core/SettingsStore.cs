using System.Text.Json;

namespace MomentumScroll.Core;
public sealed class SettingsStore
{
    public string SettingsPath { get; }
    public Exception? LastSaveError { get; private set; }

    public SettingsStore(string? root = null)
    {
        root ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MomentumScroll");
        SettingsPath = Path.Combine(root, "settings.json");
    }
    public MomentumSettings Load()
    {
        try { if (!File.Exists(SettingsPath)) return MomentumSettings.Defaults(); return JsonSerializer.Deserialize<MomentumSettings>(File.ReadAllText(SettingsPath)) ?? MomentumSettings.Defaults(); }
        catch (Exception) { return MomentumSettings.Defaults(); }
    }
    public bool Save(MomentumSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            LastSaveError = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Security software can temporarily deny this write. Settings persistence
            // must never be allowed to terminate the input utility.
            LastSaveError = ex;
            return false;
        }
    }
}
