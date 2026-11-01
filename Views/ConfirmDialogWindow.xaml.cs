using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace MeridianClient.Views;

public partial class ConfirmDialogWindow : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmDialogWindow(
        string title,
        string message,
        string subMessage = "Все сохраненные связи и промежуточные сессии будут безвозвратно удалены.",
        string confirmText = "Удалить",
        string cancelText = "Отмена",
        bool isDanger = true,
        SymbolRegular icon = SymbolRegular.Delete24)
    {
        InitializeComponent();

        txtTitle.Text = title;
        txtMessage.Text = message;
        txtSubMessage.Text = subMessage;
        txtConfirmLabel.Text = confirmText;
        txtCancelLabel.Text = cancelText;
        dialogIcon.Symbol = icon;

        if (!isDanger)
        {
            var accentBrush = Application.Current.FindResource("AccentBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(6, 182, 212));
            iconBadge.Background = new SolidColorBrush(Color.FromArgb(35, accentBrush.Color.R, accentBrush.Color.G, accentBrush.Color.B));
            iconBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(90, accentBrush.Color.R, accentBrush.Color.G, accentBrush.Color.B));
            dialogIcon.Foreground = accentBrush;

            var confirmBorder = (Border)btnConfirm.Template.FindName("b", btnConfirm);
            if (confirmBorder != null)
            {
                confirmBorder.Background = accentBrush;
                confirmBorder.BorderBrush = accentBrush;
            }
        }

        MouseLeftButtonDown += (s, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }

    public static bool Show(
        string title,
        string message,
        string subMessage = "Все сохраненные связи и промежуточные сессии будут безвозвратно удалены.",
        string confirmText = "Удалить",
        string cancelText = "Отмена",
        bool isDanger = true,
        SymbolRegular icon = SymbolRegular.Delete24)
    {
        Window? owner = Application.Current?.MainWindow;
        if (owner == null || !owner.IsVisible)
        {
            foreach (Window win in Application.Current?.Windows ?? (System.Collections.IEnumerable)new Window[0])
            {
                if (win.IsVisible)
                {
                    owner = win;
                    break;
                }
            }
        }

        var dialog = new ConfirmDialogWindow(title, message, subMessage, confirmText, cancelText, isDanger, icon);
        if (owner != null && owner.IsVisible)
        {
            dialog.Owner = owner;
        }

        dialog.ShowDialog();
        return dialog.Confirmed;
    }
}
