using System.Text.Json;
using System.IO;

namespace JeevansWindowsTools.Models;

public sealed class UnifiedSettings
{
    public bool MasterEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; } = true;
}

public sealed class UnifiedSettingsStore
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JeevansWindowsTools", "settings.json");

    public UnifiedSettings Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<UnifiedSettings>(File.ReadAllText(_path)) ?? new()
                : new();
        }
        catch { return new(); }
    }

    public bool Save(UnifiedSettings value)
    {
        string? temp = null;
        try
        {
            var folder = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(folder);
            temp = Path.Combine(folder, $"settings-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(temp, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, _path, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Antivirus/endpoint protection may briefly lock the destination.
            // Keep the current in-memory choice and leave the application running.
            return false;
        }
        finally
        {
            if (temp is not null)
            {
                try { File.Delete(temp); } catch { }
            }
        }
    }
}
