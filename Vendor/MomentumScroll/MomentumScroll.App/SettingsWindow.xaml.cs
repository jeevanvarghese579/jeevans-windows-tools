using System.Windows;
using MomentumScroll.App.ViewModels;
namespace MomentumScroll.App;
public partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel;
    public SettingsWindow(MainViewModel viewModel) { _viewModel = viewModel; InitializeComponent(); DataContext = _viewModel; }
    private void Restore_Click(object sender, RoutedEventArgs e) => _viewModel.RestoreDefaults();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
