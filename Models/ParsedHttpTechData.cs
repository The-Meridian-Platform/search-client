namespace MeridianClient.Models;

public class ParsedHttpTechData
{
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string Server { get; set; } = string.Empty;
    public string PoweredBy { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool HasHsts { get; set; }
    public bool HasCsp { get; set; }
    public bool HasXFrameOptions { get; set; }
    public string ContentType { get; set; } = string.Empty;

    public bool HasServer => !string.IsNullOrWhiteSpace(Server);
    public bool HasPoweredBy => !string.IsNullOrWhiteSpace(PoweredBy);
    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

    public string StatusBadgeText => StatusCode switch
    {
        200 => "200 OK",
        201 => "201 CREATED",
        204 => "204 NO CONTENT",
        301 => "301 MOVED",
        302 => "302 FOUND",
        400 => "400 BAD REQUEST",
        401 => "401 UNAUTHORIZED",
        403 => "403 FORBIDDEN",
        404 => "404 NOT FOUND",
        500 => "500 SERVER ERR",
        502 => "502 BAD GATEWAY",
        503 => "503 UNAVAIL",
        _ => StatusCode > 0 ? $"{StatusCode} HTTP" : "HTTP"
    };

    public string StatusColor => StatusCode switch
    {
        >= 200 and < 300 => "#00E599", // Emerald
        >= 300 and < 400 => "#38BDF8", // Sky Blue
        >= 400 and < 500 => "#FFB800", // Amber
        >= 500 => "#FF3366",           // Rose Red
        _ => "#94A3B8"
    };

    public string StatusBackground => StatusCode switch
    {
        >= 200 and < 300 => "#2000E599",
        >= 300 and < 400 => "#2038BDF8",
        >= 400 and < 500 => "#25FFB800",
        >= 500 => "#25FF3366",
        _ => "#1AFFFFFF"
    };
}
