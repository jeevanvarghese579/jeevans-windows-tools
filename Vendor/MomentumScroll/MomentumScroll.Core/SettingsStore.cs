using System.Text.Json;

namespace MomentumScroll.Core;
public sealed class SettingsStore
{
    public string SettingsPath { get; }
    public SettingsStore(string? root = null) { root ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MomentumScroll"); Directory.CreateDirectory(root); SettingsPath = Path.Combine(root, "settings.json"); }
    public MomentumSettings Load()
    {
        try { if (!File.Exists(SettingsPath)) return MomentumSettings.Defaults(); return JsonSerializer.Deserialize<MomentumSettings>(File.ReadAllText(SettingsPath)) ?? MomentumSettings.Defaults(); }
        catch (Exception) { return MomentumSettings.Defaults(); }
    }
    public void Save(MomentumSettings settings) => File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
}
