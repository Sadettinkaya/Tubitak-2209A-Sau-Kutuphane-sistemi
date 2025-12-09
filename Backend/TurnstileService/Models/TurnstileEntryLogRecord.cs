namespace TurnstileService.Models;

public class TurnstileEntryLogRecord
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string StudentNumber { get; init; } = string.Empty;
    public bool Allowed { get; init; }
    public string Message { get; init; } = string.Empty;
}
