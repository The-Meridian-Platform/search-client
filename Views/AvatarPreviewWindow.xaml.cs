using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using MeridianClient.Services;

namespace MeridianClient.Views;

public partial class AvatarPreviewWindow : Window
{
    private readonly string? _profileUrl;

    public AvatarPreviewWindow(
        string title,
        string? avatarUrl,
        string displayName,
        string? subtitle,
        string? profileUrl,
        Color accentColor)
    {
        InitializeComponent();

        _profileUrl = profileUrl;

        var loc = LocalizationService.Instance;
        txtCloseLabel.Text = loc.GetString("Avatar_Dialog_Close") ?? "Закрыть";
        txtOpenProfileLabel.Text = loc.GetString("Avatar_Dialog_OpenProfile") ?? "Открыть профиль";

        txtTitle.Text = title;
        txtDisplayName.Text = displayName;
        txtSubtitle.Text = subtitle ?? string.Empty;
        txtSubtitle.Visibility = string.IsNullOrWhiteSpace(subtitle) ? Visibility.Collapsed : Visibility.Visible;

        if (string.IsNullOrWhiteSpace(_profileUrl))
        {
            btnOpenProfile.Visibility = Visibility.Collapsed;
        }

        var accentBrush = new SolidColorBrush(accentColor);
        iconBadge.Background = new SolidColorBrush(Color.FromArgb(38, accentColor.R, accentColor.G, accentColor.B));
        iconBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(102, accentColor.R, accentColor.G, accentColor.B));
        dialogIcon.Foreground = accentBrush;

        avatarGlowBorder.BorderBrush = accentBrush;
        avatarGlowEffect.Color = accentColor;

        void ApplyButtonAccent()
        {
            var openProfileBorder = (Border)btnOpenProfile.Template?.FindName("b", btnOpenProfile)!;
            if (openProfileBorder != null)
            {
                openProfileBorder.Background = new SolidColorBrush(Color.FromArgb(224, accentColor.R, accentColor.G, accentColor.B));
                openProfileBorder.BorderBrush = accentBrush;
                if (openProfileBorder.Effect is DropShadowEffect dropShadow)
                {
                    dropShadow.Color = accentColor;
                }
            }
        }

        btnOpenProfile.Loaded += (_, _) => ApplyButtonAccent();
        btnOpenProfile.ApplyTemplate();
        ApplyButtonAccent();

        if (!string.IsNullOrWhiteSpace(avatarUrl) && Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                imgAvatar.Source = bitmap;
                imgAvatar.Visibility = Visibility.Visible;
                avatarFallbackIcon.Visibility = Visibility.Collapsed;
            }
            catch
            {
                imgAvatar.Visibility = Visibility.Collapsed;
                avatarFallbackIcon.Visibility = Visibility.Visible;
            }
        }
        else
        {
            imgAvatar.Visibility = Visibility.Collapsed;
            avatarFallbackIcon.Visibility = Visibility.Visible;
        }

        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
    }

    private void BtnOpenProfile_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_profileUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo(_profileUrl) { UseShellExecute = true });
            }
            catch { }
        }
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public static void Show(
        Window? owner,
        string title,
        string? avatarUrl,
        string displayName,
        string? subtitle,
        string? profileUrl,
        Color accentColor)
    {
        if (owner == null || !owner.IsVisible)
        {
            owner = Application.Current?.MainWindow;
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
        }

        var dialog = new AvatarPreviewWindow(title, avatarUrl, displayName, subtitle, profileUrl, accentColor);
        if (owner != null && owner.IsVisible)
        {
            dialog.Owner = owner;
        }

        dialog.ShowDialog();
    }
}