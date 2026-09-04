# Jeevan's Windows Tools

A unified WPF tray application containing:

- **Blank Upper** — double-click blank space in File Explorer to navigate up.
- **Momentum Scroll** — add momentum to mouse-wheel scrolling.
- **Taskbar Switcher** — scroll at the top or bottom screen edge to switch virtual desktops or windows.

The master switch pauses or resumes every tool without changing the three individual enable/disable choices. Each card has its own settings page inside the unified window. Existing Blank Upper, Momentum Scroll, and Taskbar Desktop Switcher preferences are reused.

## Build and run

The required feature source projects are vendored under `Vendor`, so a fresh clone builds without needing the original sibling folders:

```powershell
dotnet build .\JeevansWindowsTools.csproj
dotnet run --project .\JeevansWindowsTools.csproj
```

Publish a self-contained Windows executable and its required companion assemblies:

```powershell
dotnet publish .\JeevansWindowsTools.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
```

Unified preferences are stored in `%LocalAppData%\JeevansWindowsTools\settings.json`. Feature-specific preferences remain in their original locations, so existing user choices carry over.

For a clean migration, disable the old apps' **Start with Windows** options and enable startup once in this unified app.
Exit any separately running copies before launching the unified app so that two copies of the same mouse hook are not active.
