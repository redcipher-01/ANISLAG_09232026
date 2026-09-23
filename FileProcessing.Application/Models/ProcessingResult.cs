namespace FileProcessing.Application.Models;

public sealed record ProcessingResult(
    int InputCount,
    int OutputCount,
    IReadOnlyList<Transaction> Items);
