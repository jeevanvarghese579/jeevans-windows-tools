using System.IO;
namespace MomentumScroll.App;
public static class Logger
{
    private static readonly object Gate = new();
    private static readonly string PathName;
    static Logger() { var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MomentumScroll", "Logs"); Directory.CreateDirectory(folder); PathName = Path.Combine(folder, $"MomentumScroll-{DateTime.UtcNow:yyyyMMdd}.log"); }
    public static void Info(string message) => Write("INFO", message, null);
    public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);
    private static void Write(string level, string message, Exception? exception) { lock (Gate) File.AppendAllText(PathName, $"{DateTime.UtcNow:O} [{level}] {message}{(exception is null ? "" : Environment.NewLine + exception)}{Environment.NewLine}"); }
}
