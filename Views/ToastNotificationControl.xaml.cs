using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MeridianClient.Models;
using MeridianClient.Services;

namespace MeridianClient.Views;

public partial class ToastNotificationControl : UserControl
{
    private IToastService? _toastService;
    private AnimationClock? _progressClock;
    private bool _isExiting;

    public ToastNotificationControl()
    {
        InitializeComponent();
    }

    public void Initialize(IToastService toastService)
    {
        _toastService = toastService;
        _toastService.ToastRequested += OnToastRequested;
        _toastService.DismissRequested += OnDismissRequested;
    }

    private void OnToastRequested(ToastMessage toast)
    {
        Dispatcher.Invoke(() => ShowToast(toast));
    }

    private void OnDismissRequested()
    {
        Dispatcher.Invoke(DismissToast);
    }

    private void ShowToast(ToastMessage toast)
    {
        StopAllAnimations();
        _isExiting = false;

        txtTitle.Text = toast.Title;
        txtMessage.Text = toast.Message;

        var brushConverter = new BrushConverter();
        var accentBrush = (Brush?)brushConverter.ConvertFromString(toast.AccentColor) ?? Brushes.Cyan;
        var badgeBg = (Brush?)brushConverter.ConvertFromString(toast.BadgeBackground) ?? Brushes.Transparent;
        var badgeBorder = (Brush?)brushConverter.ConvertFromString(toast.BadgeBorder) ?? Brushes.Cyan;

        toastContainer.BorderBrush = accentBrush;
        timeProgressBar.Foreground = accentBrush;
        iconBadge.Background = badgeBg;
        iconBadge.BorderBrush = badgeBorder;
        symbolIcon.Symbol = toast.Symbol;
        symbolIcon.Foreground = accentBrush;
        txtTitle.Foreground = accentBrush;

        Visibility = Visibility.Visible;
        toastContainer.Opacity = 0;
        toastTranslate.X = 50;
        toastScale.ScaleX = 0.94;
        toastScale.ScaleY = 0.94;
        timeProgressBar.Value = 1.0;

        var opacityAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var translateXAnim = new DoubleAnimation(50, 0, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
        };
        var scaleXAnim = new DoubleAnimation(0.94, 1.0, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
        };
        var scaleYAnim = new DoubleAnimation(0.94, 1.0, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
        };

        toastContainer.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        toastTranslate.BeginAnimation(TranslateTransform.XProperty, translateXAnim);
        toastScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
        toastScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);

        var progressAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(toast.DurationSeconds))
        {
            FillBehavior = FillBehavior.HoldEnd
        };
        _progressClock = progressAnim.CreateClock();
        _progressClock.Completed += (_, _) =>
        {
            if (!_isExiting && IsMouseOver != true)
            {
                DismissToast();
            }
        };
        timeProgressBar.ApplyAnimationClock(RangeBase.ValueProperty, _progressClock);
    }

    private void DismissToast()
    {
        if (_isExiting || Visibility != Visibility.Visible) return;
        _isExiting = true;

        if (_progressClock?.Controller != null)
        {
            _progressClock.Controller.Stop();
        }

        var exitOpacity = new DoubleAnimation(toastContainer.Opacity, 0.0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var exitTranslate = new DoubleAnimation(toastTranslate.X, 40, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var exitScaleX = new DoubleAnimation(toastScale.ScaleX, 0.94, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var exitScaleY = new DoubleAnimation(toastScale.ScaleY, 0.94, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        exitOpacity.Completed += (_, _) =>
        {
            Visibility = Visibility.Collapsed;
            _isExiting = false;
            StopAllAnimations();
        };

        toastContainer.BeginAnimation(UIElement.OpacityProperty, exitOpacity);
        toastTranslate.BeginAnimation(TranslateTransform.XProperty, exitTranslate);
        toastScale.BeginAnimation(ScaleTransform.ScaleXProperty, exitScaleX);
        toastScale.BeginAnimation(ScaleTransform.ScaleYProperty, exitScaleY);
    }

    private void StopAllAnimations()
    {
        if (_progressClock?.Controller != null)
        {
            _progressClock.Controller.Stop();
        }
        timeProgressBar.ApplyAnimationClock(RangeBase.ValueProperty, null);
        timeProgressBar.Value = 1.0;

        toastContainer.BeginAnimation(UIElement.OpacityProperty, null);
        toastTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        toastScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        toastScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    }

    private void BtnDismiss_Click(object sender, RoutedEventArgs e)
    {
        DismissToast();
    }

    private void Toast_MouseEnter(object sender, MouseEventArgs e)
    {
        _progressClock?.Controller?.Pause();
    }

    private void Toast_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!_isExiting && Visibility == Visibility.Visible)
        {
            if (_progressClock != null && _progressClock.CurrentProgress == 1.0)
            {
                DismissToast();
            }
            else
            {
                _progressClock?.Controller?.Resume();
            }
        }
    }
}
