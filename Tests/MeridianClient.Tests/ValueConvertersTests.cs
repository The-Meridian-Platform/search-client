using System.Windows;
using MeridianClient.Converters;
using Xunit;

namespace MeridianClient.Tests;

public class ValueConvertersTests
{
    [Theory]
    [InlineData(0, null, Visibility.Collapsed)]
    [InlineData(0, "Inverse", Visibility.Visible)]
    [InlineData(5, null, Visibility.Visible)]
    [InlineData(5, "Inverse", Visibility.Collapsed)]
    [InlineData(true, null, Visibility.Visible)]
    [InlineData(true, "Inverse", Visibility.Collapsed)]
    [InlineData(false, null, Visibility.Collapsed)]
    [InlineData(false, "Inverse", Visibility.Visible)]
    [InlineData("hello", null, Visibility.Visible)]
    [InlineData("hello", "Inverse", Visibility.Collapsed)]
    [InlineData("", null, Visibility.Collapsed)]
    [InlineData("", "Inverse", Visibility.Visible)]
    public void BooleanToVisibilityConverter_HandlesTypesAndInverseCorrectly(object value, string? param, Visibility expected)
    {
        var converter = new BooleanToVisibilityConverter();
        var result = converter.Convert(value, typeof(Visibility), param, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Whois", "Whois", true)]
    [InlineData("whois", "WHOIS", true)]
    [InlineData("All", "DNS", false)]
    [InlineData(null, "All", false)]
    [InlineData("All", null, false)]
    public void StringEqualsToBooleanConverter_ComparesCorrectly(string? value, string? param, bool expected)
    {
        var converter = new StringEqualsToBooleanConverter();
        var result = converter.Convert(value, typeof(bool), param, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }
}
