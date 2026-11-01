using System.Windows;
using System.Windows.Controls;
using MeridianClient.ViewModels;

namespace MeridianClient.Views;

public partial class LogsView : UserControl
{
    public LogsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LogsViewModel vm)
        {
            vm.RequestScrollToEnd -= VmOnRequestScrollToEnd;
            vm.RequestScrollToEnd += VmOnRequestScrollToEnd;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LogsViewModel vm)
        {
            vm.RequestScrollToEnd -= VmOnRequestScrollToEnd;
        }
    }

    private void VmOnRequestScrollToEnd()
    {
        if (LogsScrollViewer != null)
        {
            LogsScrollViewer.ScrollToEnd();
        }
    }
}
