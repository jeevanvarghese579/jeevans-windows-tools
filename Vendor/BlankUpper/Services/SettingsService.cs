using System.Text.Json;
using System.IO;
using BlankUpper.Models;
namespace BlankUpper.Services;
public sealed class SettingsService
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlankUpper");
    private static readonly string FilePath = Path.Combine(Folder, "settings.json");
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Could not load settings", ex);
            try { File.Copy(FilePath, FilePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json", true); } catch { }
            return new();
        }
    }
    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var temporaryPath = FilePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, FilePath, true);
        }
        catch (Exception ex) { AppLogger.Error("Could not save settings", ex); }
    }
}
