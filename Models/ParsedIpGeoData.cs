using System;
using System.Globalization;

namespace MeridianClient.Models;

public sealed class ParsedIpGeoData
{
    public string Ip { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public string Isp { get; set; } = string.Empty;
    public string Org { get; set; } = string.Empty;
    public string Asn { get; set; } = string.Empty;
    public bool IsMobile { get; set; }
    public bool IsProxy { get; set; }
    public bool IsHosting { get; set; }

    public bool HasCoordinates => Math.Abs(Latitude) > 0.0001 || Math.Abs(Longitude) > 0.0001;

    public string Coordinates => HasCoordinates
        ? $"{Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {Longitude.ToString("F4", CultureInfo.InvariantCulture)}"
        : string.Empty;

    public string FlagEmoji => GetFlagEmoji(CountryCode);

    public string FormattedLocation
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(City) && !string.IsNullOrWhiteSpace(Country))
                return $"{City}, {Country}";
            if (!string.IsNullOrWhiteSpace(Country))
                return Country;
            if (!string.IsNullOrWhiteSpace(City))
                return City;
            return "Неизвестная локация";
        }
    }

    public string LocationBadgeText => !string.IsNullOrWhiteSpace(CountryCode)
        ? $"[{CountryCode.ToUpperInvariant()}] {FormattedLocation}"
        : FormattedLocation;

    public bool HasAsn => !string.IsNullOrWhiteSpace(Asn);
    public bool HasIsp => !string.IsNullOrWhiteSpace(Isp);
    public bool HasOrg => !string.IsNullOrWhiteSpace(Org);
    public bool HasTimezone => !string.IsNullOrWhiteSpace(Timezone);
    public bool HasNetworkFlags => IsHosting || IsProxy || IsMobile;

    private static string GetFlagEmoji(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 2) return string.Empty;
        var upper = code.ToUpperInvariant();
        if (upper[0] < 'A' || upper[0] > 'Z' || upper[1] < 'A' || upper[1] > 'Z') return string.Empty;
        return char.ConvertFromUtf32(0x1F1E6 + (upper[0] - 'A')) + char.ConvertFromUtf32(0x1F1E6 + (upper[1] - 'A'));
    }
}
