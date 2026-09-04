using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace TaskbarDesktopSwitcher
{
    public sealed class NotifyIconManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly MainWindow _mainWindow;
        private readonly StartupManager _startupManager;
        private readonly EdgeSettings _edgeSettings;
        private ToolStripMenuItem? _startWithWindowsMenuItem;
        private ToolStripMenuItem? _enableSwitcherMenuItem;
        private ToolStripMenuItem? _topEdgeMenuItem;
        private ToolStripMenuItem? _bottomEdgeMenuItem;

        public NotifyIconManager(MainWindow mainWindow, StartupManager startupManager, EdgeSettings edgeSettings)
        {
            _mainWindow = mainWindow;
            _startupManager = startupManager;
            _edgeSettings = edgeSettings;
            _notifyIcon = new NotifyIcon
            {
                Icon = AppIcon.LoadTrayIcon(),
                Text = "Taskbar Desktop Switcher",
                Visible = true,
                ContextMenuStrip = CreateContextMenu()
            };
            _notifyIcon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowMainWindow(); };
            _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("Open");
            openItem.Click += (_, _) => ShowMainWindow();
            menu.Items.Add(openItem);

            _enableSwitcherMenuItem = new ToolStripMenuItem("Enable Desktop Switcher") { CheckOnClick = true };
            _enableSwitcherMenuItem.Click += (_, _) => _mainWindow.EnableSwitcherToggle.IsChecked = _enableSwitcherMenuItem.Checked;
            menu.Items.Add(_enableSwitcherMenuItem);

            _topEdgeMenuItem = CreateEdgeMenu("Top Edge", EdgePosition.Top);
            _bottomEdgeMenuItem = CreateEdgeMenu("Bottom Edge", EdgePosition.Bottom);
            menu.Items.Add(_topEdgeMenuItem);
            menu.Items.Add(_bottomEdgeMenuItem);

            _startWithWindowsMenuItem = new ToolStripMenuItem("Start with Windows") { CheckOnClick = true };
            _startWithWindowsMenuItem.Click += (_, _) =>
            {
                if (_startWithWindowsMenuItem.Checked) _startupManager.EnableStartWithWindows();
                else _startupManager.DisableStartWithWindows();
                _mainWindow.StartWithWindowsToggle.IsChecked = _startWithWindowsMenuItem.Checked;
            };
            menu.Items.Add(_startWithWindowsMenuItem);
            menu.Items.Add(new ToolStripSeparator());

            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (_, _) => { var about = new AboutWindow { Owner = _mainWindow }; about.ShowDialog(); };
            menu.Items.Add(aboutItem);
            menu.Items.Add(new ToolStripSeparator());
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => (System.Windows.Application.Current as App)?.ExitApplication();
            menu.Items.Add(exitItem);
            UpdateStates();
            return menu;
        }

        private ToolStripMenuItem CreateEdgeMenu(string text, EdgePosition edge)
        {
            var parent = new ToolStripMenuItem(text);
            foreach (var function in Enum.GetValues<EdgeFunction>())
            {
                var item = new ToolStripMenuItem(GetEdgeText(function)) { Tag = function, CheckOnClick = true };
                item.Click += (_, _) => _mainWindow.SetEdgeFunction(edge, function);
                parent.DropDownItems.Add(item);
            }
            return parent;
        }

        public void UpdateStates()
        {
            if (_enableSwitcherMenuItem != null) _enableSwitcherMenuItem.Checked = _mainWindow.IsSwitcherEnabled;
            if (_startWithWindowsMenuItem != null) _startWithWindowsMenuItem.Checked = _startupManager.IsStartWithWindowsEnabled();
            UpdateEdgeStates();
        }

        public void UpdateEdgeStates()
        {
            UpdateEdgeMenu(_topEdgeMenuItem, _edgeSettings.TopEdge);
            UpdateEdgeMenu(_bottomEdgeMenuItem, _edgeSettings.BottomEdge);
        }

        private static void UpdateEdgeMenu(ToolStripMenuItem? menu, EdgeFunction selected)
        {
            if (menu == null) return;
            foreach (ToolStripMenuItem item in menu.DropDownItems)
                item.Checked = item.Tag is EdgeFunction function && function == selected;
        }

        private static string GetEdgeText(EdgeFunction function) => function switch
        {
            EdgeFunction.VirtualDesktops => "Virtual Desktops",
            EdgeFunction.WindowSwitching => "Window Switching",
            _ => "None"
        };

        public void UpdateStartWithWindowsState() => UpdateStates();
        public void UpdateEnableSwitcherState(bool isEnabled) => UpdateStates();

        private void ShowMainWindow()
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Focus();
        }

        public void Dispose() => _notifyIcon.Dispose();
    }
}
