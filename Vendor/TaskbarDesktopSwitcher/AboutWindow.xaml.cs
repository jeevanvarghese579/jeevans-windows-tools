using System.Windows;
using System.Windows.Input;

namespace TaskbarDesktopSwitcher
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Email_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Clipboard.SetText("jeevanvarghese579@gmail.com");
            System.Windows.MessageBox.Show("Email address copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}