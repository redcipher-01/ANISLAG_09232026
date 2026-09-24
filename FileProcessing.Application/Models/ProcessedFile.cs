namespace FileProcessing.Application.Models;

public sealed record ProcessedFile(
    string FileName,
    DateTimeOffset ProcessedAtUtc,
    long ProcessingTimeMilliseconds,
    int InputCount,
    int OutputCount);