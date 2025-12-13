namespace Shared.Events;

/// <summary>
/// Yeni rezervasyon oluşturulduğunda tetiklenen event
/// </summary>
public class ReservationCreatedEvent
{
    public int ReservationId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public int TableId { get; set; }
    public string ReservationDate { get; set; } = string.Empty; // Format: yyyy-MM-dd
    public string StartTime { get; set; } = string.Empty; // Format: HH:mm
    public string EndTime { get; set; } = string.Empty; // Format: HH:mm
    public string StudentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
