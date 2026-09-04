using System.Diagnostics;
using System.Windows.Automation;

namespace BlankUpper.Services;

/// <summary>Conservative UI Automation hit testing for Explorer's folder contents control.</summary>
public sealed class ExplorerDetectionService
{
    internal bool IsConfidentExplorerBlankSpace(NativeMethods.Point point)
    {
        try
        {
            var foreground = NativeMethods.GetForegroundWindow();
            if (!IsExplorer(foreground)) return false;

            // Hook coordinates and UIA screen coordinates are physical pixels under PerMonitorV2 awareness.
            var element = AutomationElement.FromPoint(new System.Windows.Point(point.X, point.Y));
            if (element is null || IsExcluded(element)) return false;

            var hasListSurface = false;
            for (AutomationElement? current = element; current is not null; current = TreeWalker.RawViewWalker.GetParent(current))
            {
                // Stop at the contents boundary. Explorer chrome (including tab controls) can be above it.
                if (IsFolderContentsControl(current)) return true;
                if (IsExcluded(current)) return false;
                if (current.Current.ControlType == ControlType.List) hasListSurface = true;
                if (current.Current.NativeWindowHandle == foreground.ToInt32()) break;
            }
            // Some Windows 10 Explorer builds expose the folder view as an unnamed List.
            // A list surface is safe here because file/folder items have already been rejected above.
            if (!hasListSurface) AppLogger.Info($"Explorer UIA did not expose a folder view at {point.X},{point.Y}: {Describe(element)}");
            return hasListSurface;
        }
        catch (ElementNotAvailableException) { return false; }
        catch (Exception ex) { AppLogger.Error("UI Automation inspection failed", ex); return false; }
    }

    private static bool IsExplorer(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        try { NativeMethods.GetWindowThreadProcessId(hwnd, out var pid); using var process = Process.GetProcessById((int)pid); return string.Equals(process.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }

    // Explorer identifies the main content control by these stable UIA identifiers/names across its common views.
    private static bool IsFolderContentsControl(AutomationElement element)
    {
        var type = element.Current.ControlType;
        if (type != ControlType.List && type != ControlType.Pane && type != ControlType.Group) return false;
        var id = Safe(() => element.Current.AutomationId); var name = Safe(() => element.Current.Name);
        var cls = Safe(() => element.Current.ClassName);
        return ContainsAny(id, "FolderView", "ItemsView", "ShellFolderView") || ContainsAny(name, "Items View", "Folder View") || ContainsAny(cls, "FolderView", "ItemsView", "ShellFolderView");
    }

    // Anything interactive, item-like, or part of Explorer chrome is never blank space.
    private static bool IsExcluded(AutomationElement element)
    {
        try
        {
            var type = element.Current.ControlType;
            if (type == ControlType.ListItem || type == ControlType.TreeItem || type == ControlType.DataItem || type == ControlType.Button || type == ControlType.Edit || type == ControlType.MenuItem || type == ControlType.TabItem || type == ControlType.ScrollBar || type == ControlType.Hyperlink || type == ControlType.Menu || type == ControlType.ToolBar) return true;
            var id = element.Current.AutomationId; var name = element.Current.Name; var cls = element.Current.ClassName;
            return ContainsAny(id, "Address", "Search", "Breadcrumb", "Navigation", "Toolbar", "Tab", "CommandBar", "Scroll") ||
                   ContainsAny(name, "Address", "Search", "Navigation", "Command bar") ||
                   ContainsAny(cls, "Toolbar", "ReBar", "ScrollBar", "Menu");
        }
        catch (ElementNotAvailableException) { return true; }
    }

    private static string Safe(Func<string> getter) { try { return getter() ?? string.Empty; } catch { return string.Empty; } }
    private static string Describe(AutomationElement element) => $"type={Safe(() => element.Current.ControlType.ProgrammaticName)}, name='{Safe(() => element.Current.Name)}', id='{Safe(() => element.Current.AutomationId)}', class='{Safe(() => element.Current.ClassName)}'";
    private static bool ContainsAny(string? value, params string[] terms) => !string.IsNullOrWhiteSpace(value) && terms.Any(t => value.Contains(t, StringComparison.OrdinalIgnoreCase));
}
