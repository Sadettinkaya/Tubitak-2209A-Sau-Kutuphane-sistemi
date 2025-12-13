namespace Shared.Events;

/// <summary>
/// Rezervasyon iptal edildiğinde tetiklenen event
/// </summary>
public class ReservationCancelledEvent
{
    public int ReservationId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public int TableId { get; set; }
    public string ReservationDate { get; set; } = string.Empty; // Format: yyyy-MM-dd
    public DateTime CancelledAt { get; set; }
}
