using System.IO;
namespace BlankUpper.Services;
public static class AppLogger
{
    public static void Info(string message) => Write("INFO " + message);
    public static void Error(string message, Exception? exception = null)
    {
        Write($"ERROR {message} {exception}");
    }
    private static void Write(string message) { try { var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlankUpper", "Logs"); Directory.CreateDirectory(dir); File.AppendAllText(Path.Combine(dir, "BlankUpper.log"), $"{DateTime.Now:O} {message}\r\n"); } catch { } }
}
