# Security and input access

Jeevan's Windows Tools does not contain networking, downloading, telemetry, auto-update, driver installation, service installation, code injection, or elevation logic. It runs as the current user (`asInvoker`).

Its features require system-wide input observation:

- Blank Upper observes physical left-button clicks and uses Windows UI Automation to identify blank File Explorer space.
- Momentum Scroll observes physical wheel/button input and emits synthetic wheel input while momentum is active.
- Taskbar Switcher observes wheel input at configured screen edges and emits documented Windows keyboard shortcuts.

Version 1.1 uses one shared low-level mouse hook for all three features. Injected mouse input is ignored. A separate keyboard hook is active only when the user configures an edge for Window Switching, where Escape must cancel the switcher.

The shared hook replaces both the former per-feature mouse hooks and the former raw-input listener. It is stopped when the master switch is off. If only Taskbar Switcher is enabled with both edges set to Nothing, no global input hook is installed.

The master switch only starts or stops these hooks and saves `MasterEnabled` in `%LocalAppData%\JeevansWindowsTools\settings.json`. It performs no filesystem operation outside the settings file.

## Antivirus review

Global input hooks and `SendInput` can resemble automation malware to behavioral antivirus products. Release builds should be Authenticode-signed with a trusted code-signing certificate and submitted to antivirus vendors when a false positive occurs. Do not advise users to disable antivirus protection globally.

The recommended release is framework-dependent and multi-file. This avoids packing the .NET runtime and several assemblies into one unusually large executable. Users must keep the complete published folder together and install the .NET 10 Desktop Runtime.
