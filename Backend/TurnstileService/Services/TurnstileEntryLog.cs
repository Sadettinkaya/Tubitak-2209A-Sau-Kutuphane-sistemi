using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TurnstileService.Models;

namespace TurnstileService.Services;

public interface ITurnstileEntryLog
{
    void Record(string studentNumber, bool allowed, string message);
    IReadOnlyCollection<TurnstileEntryLogRecord> GetLatest(int take = 20);
}

public class InMemoryTurnstileEntryLog : ITurnstileEntryLog
{
    private readonly ConcurrentQueue<TurnstileEntryLogRecord> _entries = new();
    private readonly int _maxItems;

    public InMemoryTurnstileEntryLog(IOptions<TurnstileOptions> options)
    {
        _maxItems = Math.Max(50, options.Value.EntryLogMaxItems);
    }

    public void Record(string studentNumber, bool allowed, string message)
    {
        _entries.Enqueue(new TurnstileEntryLogRecord
        {
            StudentNumber = studentNumber,
            Allowed = allowed,
            Message = message,
            TimestampUtc = DateTime.UtcNow
        });

        while (_entries.Count > _maxItems && _entries.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyCollection<TurnstileEntryLogRecord> GetLatest(int take = 20)
    {
        return _entries.Reverse().Take(Math.Max(1, take)).ToArray();
    }
}
