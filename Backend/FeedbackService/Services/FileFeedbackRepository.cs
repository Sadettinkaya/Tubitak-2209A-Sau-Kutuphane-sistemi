using System.Text.Json;
using FeedbackService.Models;

namespace FeedbackService.Services;

public interface IFeedbackRepository
{
    Task<IReadOnlyCollection<Feedback>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Feedback> AddAsync(Feedback feedback, CancellationToken cancellationToken = default);
}

public class FileFeedbackRepository : IFeedbackRepository
{
    private readonly string _storagePath;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private List<Feedback> _cache = new();
    private bool _loaded;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public FileFeedbackRepository(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _storagePath = Path.Combine(dataDirectory, "feedback.json");
    }

    public async Task<IReadOnlyCollection<Feedback>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        await _sync.WaitAsync(cancellationToken);
        try
        {
            return _cache
                .OrderByDescending(f => f.Date)
                .Select(f => new Feedback
                {
                    Id = f.Id,
                    StudentNumber = f.StudentNumber,
                    Message = f.Message,
                    Date = f.Date
                })
                .ToArray();
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<Feedback> AddAsync(Feedback feedback, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var nextId = _cache.Count == 0 ? 1 : _cache.Max(f => f.Id) + 1;
            var stored = new Feedback
            {
                Id = nextId,
                StudentNumber = feedback.StudentNumber.Trim(),
                Message = feedback.Message.Trim(),
                Date = DateTime.Now
            };

            _cache.Add(stored);
            await PersistAsync(cancellationToken);
            return stored;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return;
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_loaded)
            {
                return;
            }

            if (File.Exists(_storagePath))
            {
                var json = await File.ReadAllTextAsync(_storagePath, cancellationToken);
                var items = JsonSerializer.Deserialize<List<Feedback>>(json, _serializerOptions) ?? new List<Feedback>();
                _cache = items;
            }
            else
            {
                _cache = new List<Feedback>();
            }

            _loaded = true;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(_cache, _serializerOptions);
        await File.WriteAllTextAsync(_storagePath, json, cancellationToken);
    }
}
