using Wpf.Ui.Controls;
using MeridianClient.ViewModels;

namespace MeridianClient;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
