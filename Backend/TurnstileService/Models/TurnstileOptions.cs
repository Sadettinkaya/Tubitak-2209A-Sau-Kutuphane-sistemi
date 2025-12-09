namespace TurnstileService.Models;

public class TurnstileOptions
{
    public string ReservationServiceBaseUrl { get; set; } = "http://localhost:5010/";
    public int EntryLogMaxItems { get; set; } = 200;
    public string IdentityBaseUrl { get; set; } = "http://localhost:5010/api/Auth";
    public ServiceAccountOptions ServiceAccount { get; set; } = new();
}

public class ServiceAccountOptions
{
    public string StudentNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
