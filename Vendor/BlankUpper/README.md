# Blank Upper

Blank Upper is a native .NET 10 WPF tray application that conservatively detects a double-click on blank space in the File Explorer folder view and sends **Alt+Up** exactly once to navigate to the parent folder. It is per-monitor-DPI aware and supports Windows 10 and Windows 11.

## Prerequisites

Windows 10 or 11 and the .NET 10 SDK (10.0.301 or later) are required to build from source. No administrator privileges are required. The published self-contained executable does not need the .NET runtime.

## Open and run

Open `BlankUpper.csproj` in Visual Studio with .NET 10 support, restore packages, and press F5. From a terminal:

```powershell
dotnet run --project .\BlankUpper.csproj
```

Settings are stored at `%LocalAppData%\BlankUpper\settings.json`; non-fatal diagnostics are written to `%LocalAppData%\BlankUpper\Logs`. Settings are written atomically; an unreadable settings file is preserved with a `.corrupt-YYYYMMDDHHMMSS.json` suffix and defaults are used.

The application and notification-area icon is a local purple folder with an upward arrow; no external resources are loaded at runtime.

## Publish

```powershell
dotnet publish .\BlankUpper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
```

## Manual test checklist

- Double-click blank folder-view space in each Explorer view mode: navigates up once.
- Double-click files/folders: normal Explorer action only; no navigation.
- Click navigation pane, toolbar, address/search bar, scroll bar, tabs, or another app: no action.
- Disable the master switch: no action; restart and confirm settings persist.
- Toggle Windows startup and verify the per-user Run entry is added/removed.
- Close the window: it hides to the tray; use Exit from the tray menu and verify it terminates.
- Start a second copy: the current copy is brought forward where Windows permits it.

## Known limitations

Explorer’s UI Automation tree and class names vary by Windows build, localization, view type, and shell extensions. The detector requires an Explorer foreground window and a recognizable folder-contents UI Automation control, and rejects item, navigation, search, address, toolbar, tab, menu, and scrollbar controls. On an unfamiliar Explorer surface it does nothing rather than risk navigating after an item click. The low-level hook never consumes mouse input and UI Automation never runs on its callback thread.

The current user startup entry points to the actual executable and includes `--startup`; when launched through `dotnet run`, it instead registers the dotnet host plus the built DLL. Closing the main window hides it to the notification area. Use **Exit** in the tray menu for a clean shutdown.
