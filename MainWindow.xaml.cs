using MeridianClient.Services;
using MeridianClient.ViewModels;
using Wpf.Ui.Controls;

namespace MeridianClient;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel, IToastService? toastService = null)
    {
        DataContext = viewModel;
        InitializeComponent();
        if (toastService != null)
        {
            toastControl.Initialize(toastService);
        }
    }
}
