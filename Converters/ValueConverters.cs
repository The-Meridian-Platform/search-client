using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MeridianClient.Converters;

public class BooleanToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush TrueBrush = new(Color.FromRgb(0, 229, 153));   // #00E599 Emerald
    private static readonly SolidColorBrush FalseBrush = new(Color.FromRgb(255, 51, 102));  // #FF3366 Crimson

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? TrueBrush : FalseBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolVal = value switch
        {
            null => false,
            bool b => b,
            int i => i > 0,
            long l => l > 0,
            double d => d > 0,
            string s => !string.IsNullOrWhiteSpace(s),
            _ => true
        };

        if (parameter is string paramStr && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
        {
            boolVal = !boolVal;
        }

        return boolVal ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush RunningBrush = new(Color.FromRgb(0, 210, 255));    // #00D2FF Electric Cyan
    private static readonly SolidColorBrush CompletedBrush = new(Color.FromRgb(0, 229, 153));  // #00E599 Cyber Emerald
    private static readonly SolidColorBrush FailedBrush = new(Color.FromRgb(255, 51, 102));    // #FF3366 Cyber Crimson
    private static readonly SolidColorBrush PendingBrush = new(Color.FromRgb(255, 184, 0));   // #FFB800 Cyber Gold
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(110, 118, 129)); // #6E7681 Muted Steel

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToUpperInvariant() ?? string.Empty;

        return status switch
        {
            "RUNNING" => RunningBrush,
            "COMPLETED" or "SUCCESS" => CompletedBrush,
            "FAILED" or "ERROR" => FailedBrush,
            "PENDING" => PendingBrush,
            _ => DefaultBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FindingTypeToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ABrush = new(Color.FromRgb(0, 210, 255));      // #00D2FF Neon Cyan
    private static readonly SolidColorBrush AaaaBrush = new(Color.FromRgb(168, 85, 247));  // #A855F7 Electric Purple
    private static readonly SolidColorBrush MxBrush = new(Color.FromRgb(255, 122, 0));     // #FF7A00 Cyber Orange
    private static readonly SolidColorBrush TxtBrush = new(Color.FromRgb(0, 229, 153));    // #00E599 Cyber Emerald
    private static readonly SolidColorBrush NsBrush = new(Color.FromRgb(0, 240, 255));     // #00F0FF Neon Aqua
    private static readonly SolidColorBrush WhoisBrush = new(Color.FromRgb(255, 0, 127));  // #FF007F Cyber Magenta
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(59, 130, 246));// #3B82F6 Sapphire

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var type = value?.ToString()?.ToUpperInvariant() ?? string.Empty;

        return type switch
        {
            "A" => ABrush,
            "AAAA" => AaaaBrush,
            "MX" => MxBrush,
            "TXT" => TxtBrush,
            "NS" => NsBrush,
            "WHOIS" => WhoisBrush,
            _ => DefaultBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringEqualsToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter != null)
        {
            return parameter.ToString()!;
        }
        return Binding.DoNothing;
    }
}

public class StringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasValue = !string.IsNullOrWhiteSpace(value as string);
        if (parameter is string paramStr && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
        {
            hasValue = !hasValue;
        }

        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
