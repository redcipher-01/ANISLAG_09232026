using FileProcessing.Application.Models;

namespace FileProcessing.Application.Interfaces;

public interface ITrackingService
{
    void Record(ProcessedFile file);

    long TotalProcessed { get; }

    IReadOnlyList<ProcessedFile> GetRecent();
}