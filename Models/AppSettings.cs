namespace MeridianClient.Models;

public class AppSettings
{
    public string BackendUrl { get; set; } = "http://localhost:5168";
    public bool AllowUnencryptedHttp2 { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 60;
}
