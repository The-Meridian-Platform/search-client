using MeridianClient.ViewModels;
using Wpf.Ui.Controls;

namespace MeridianClient.Views;

public partial class WelcomeWindow : FluentWindow
{
    public WelcomeWindow(WelcomeViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
