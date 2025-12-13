namespace Shared.Events;

/// <summary>
/// Öğrenci turnike ile kütüphaneye giriş yaptığında tetiklenen event
/// </summary>
public class StudentEnteredEvent
{
    public string StudentNumber { get; set; } = string.Empty;
    public DateTime EntryTime { get; set; }
    public string? TurnstileId { get; set; }
}
