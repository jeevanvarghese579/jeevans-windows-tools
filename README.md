# Jeevan's Windows Tools

A unified WPF tray application containing:

- **Blank Upper** — double-click blank space in File Explorer to navigate up.
- **Momentum Scroll** — add configurable momentum to physical mouse-wheel scrolling while leaving precision-touchpad gestures alone.
- **Taskbar Switcher** — scroll at the top or bottom screen edge to switch virtual desktops or windows.

The master switch pauses or resumes every tool without changing the three individual enable/disable choices. Each card has its own settings page inside the unified window. Existing Blank Upper, Momentum Scroll, and Taskbar Desktop Switcher preferences are reused.

Version 1.2 shares one global mouse hook across the three tools and restores Momentum Scroll V2's physical-mouse detection, complete tuning controls, diagnostics, horizontal momentum, defaults, and elevated-app restart. The keyboard hook is enabled only when Window Switching needs it. See [SECURITY.md](SECURITY.md) for a transparent description of the input access.

## Build and run

The required feature source projects are vendored under `Vendor`, so a fresh clone builds without needing the original sibling folders:

```powershell
dotnet build .\JeevansWindowsTools.csproj
dotnet run --project .\JeevansWindowsTools.csproj
```

Publish the recommended framework-dependent, multi-file release:

```powershell
dotnet publish .\JeevansWindowsTools.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o .\publish\win-x64-safe
```

Unified preferences are stored in `%LocalAppData%\JeevansWindowsTools\settings.json`. Feature-specific preferences remain in their original locations, so existing user choices carry over.

The release requires the .NET 10 Desktop Runtime. Keep every file in the published folder together; do not copy only the `.exe`.

For a clean migration, disable the old apps' **Start with Windows** options and enable startup once in this unified app.
Exit any separately running copies before launching the unified app so that two copies of the same mouse hook are not active.
