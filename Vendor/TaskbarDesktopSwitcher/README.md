# Taskbar Desktop Switcher

A lightweight Windows 11 utility that allows you to switch between virtual desktops by scrolling the mouse wheel over the taskbar.

## Features

- **Mouse Wheel Detection**: Detects mouse wheel scrolling over the Windows taskbar
- **Virtual Desktop Switching**: 
  - Scroll up → Switch to next desktop (Win + Ctrl + Right Arrow)
  - Scroll down → Switch to previous desktop (Win + Ctrl + Left Arrow)
- **Modern UI**: Clean, modern interface with Windows 11 style
- **System Tray Integration**: Runs quietly in the background with tray icon
- **Start with Windows**: Option to automatically start the app on login
- **Multi-Monitor Support**: Works with taskbars on secondary monitors
- **Debounce Protection**: Prevents rapid firing of shortcuts (250ms debounce)
- **Safe & Efficient**: Low CPU and memory usage, proper cleanup on exit

## Requirements

- Windows 10/11
- .NET 8.0 Runtime (or SDK for building from source)
- Visual Studio 2022 (optional, for development)

## Building from Source

### Using .NET CLI

1. Ensure you have .NET 8.0 SDK installed
2. Open a terminal in the project directory
3. Run:
   ```bash
   dotnet build
   ```

### Using Visual Studio

1. Open `TaskbarDesktopSwitcher.csproj` in Visual Studio 2022
2. Select "Release" configuration
3. Build → Build Solution (Ctrl+Shift+B)

## Running the Application

### From CLI
```bash
dotnet run --project TaskbarDesktopSwitcher.csproj
```

### From Visual Studio
Press F5 to run in debug mode, or Ctrl+F5 to run without debugging.

### Executable
After building, find the executable at:
```
bin/Release/net8.0-windows10.0.17763.0/TaskbarDesktopSwitcher.exe
```

## Usage

1. Launch the application
2. The main window shows the status and settings
3. The app runs in the background with a system tray icon
4. Hover over the taskbar and scroll the mouse wheel to switch desktops
5. Use the tray icon context menu to:
   - Open the main window
   - Toggle "Start with Windows"
   - View About information
   - Exit the application

## Configuration

### Start with Windows
- Toggle the "Start with Windows" switch in the main window
- Or use the context menu from the tray icon
- This uses the Windows Registry (HKCU\Software\Microsoft\Windows\CurrentVersion\Run)

## Technical Details

- **Framework**: WPF with .NET 8.0
- **Mouse Hook**: Low-level mouse hook (WH_MOUSE_LL) to detect wheel events globally
- **Taskbar Detection**: Uses Windows API to find taskbar windows (Shell_TrayWnd and Shell_SecondaryTrayWnd)
- **Keyboard Simulation**: Uses SendInput API for reliable virtual desktop switching
- **Tray Icon**: Uses Hardcodet.NotifyIcon.Wpf library

## Files

- `App.xaml` / `App.xaml.cs` - Application entry point
- `MainWindow.xaml` / `MainWindow.xaml.cs` - Main UI window
- `AboutWindow.xaml` / `AboutWindow.xaml.cs` - About dialog
- `MouseHook.cs` - Low-level mouse hook implementation
- `TaskbarHelper.cs` - Taskbar detection using Windows APIs
- `VirtualDesktopSwitcher.cs` - Virtual desktop switching with SendInput
- `NotifyIconManager.cs` - System tray icon management
- `StartupManager.cs` - Start with Windows registry management
- `Styles.xaml` - Modern UI styles
- `app.manifest` - Windows compatibility and DPI awareness

## Developed by

Jeevan Varghese

## License

This project is open for personal use.