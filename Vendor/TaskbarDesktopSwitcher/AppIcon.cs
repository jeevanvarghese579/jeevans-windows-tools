using System;
using System.Drawing;

namespace TaskbarDesktopSwitcher
{
    internal static class AppIcon
    {
        public static Icon LoadTrayIcon()
        {
            try
            {
                var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico", UriKind.Absolute));
                if (resource?.Stream != null)
                {
                    using (resource.Stream)
                    using (var icon = new Icon(resource.Stream)) return (Icon)icon.Clone();
                }
            }
            catch { }
            return (Icon)SystemIcons.Application.Clone();
        }
    }
}
