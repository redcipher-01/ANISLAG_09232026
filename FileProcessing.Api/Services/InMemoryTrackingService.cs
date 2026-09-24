using FileProcessing.Application.Interfaces;
using FileProcessing.Application.Models;

namespace FileProcessing.Api.Services;

public sealed class InMemoryTrackingService : ITrackingService
{
    private const int MaxRecentFiles = 100;

    private readonly Lock _sync = new();
    private readonly LinkedList<ProcessedFile> _recentFiles = new();
    private long _totalProcessed;

    public long TotalProcessed
    {
        get
        {
            lock (_sync)
            {
                return _totalProcessed;
            }
        }
    }

    public void Record(ProcessedFile file)
    {
        lock (_sync)
        {
            _totalProcessed++;
            _recentFiles.AddFirst(file);

            if (_recentFiles.Count > MaxRecentFiles)
            {
                _recentFiles.RemoveLast();
            }
        }
    }

    public IReadOnlyList<ProcessedFile> GetRecent()
    {
        lock (_sync)
        {
            return [.. _recentFiles];
        }
    }
}