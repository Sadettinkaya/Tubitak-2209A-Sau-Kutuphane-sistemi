namespace TurnstileService.Models;

public class ReservationAccessResponse
{
    public bool Allowed { get; set; }
    public string Message { get; set; } = string.Empty;
}
