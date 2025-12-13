namespace Shared.Events;

/// <summary>
/// Öğrenci profili güncellendiğinde (ceza, ban vb.) tetiklenen event
/// </summary>
public class StudentProfileUpdatedEvent
{
    public string StudentNumber { get; set; } = string.Empty;
    public string StudentType { get; set; } = string.Empty;
    public int PenaltyPoints { get; set; }
    public string? BanUntil { get; set; } // Format: yyyy-MM-dd veya null
    public string? BanReason { get; set; }
    public DateTime UpdatedAt { get; set; }
}
