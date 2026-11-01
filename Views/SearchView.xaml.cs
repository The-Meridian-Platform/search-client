using System.Windows.Controls;
using System.Windows.Input;
using MeridianClient.ViewModels;

namespace MeridianClient.Views;

public partial class SearchView : UserControl
{
    public SearchView()
    {
        InitializeComponent();
    }

    private void TargetInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is SearchViewModel vm && vm.StartSearchCommand.CanExecute(null))
            {
                vm.StartSearchCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
