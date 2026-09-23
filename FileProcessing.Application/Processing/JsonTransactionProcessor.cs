using FileProcessing.Application.Models;
using System.Text.Json;

namespace FileProcessing.Application.Processing;

public sealed class JsonTransactionProcessor
{
    public async Task<ProcessingResult> ProcessAsync(
        Stream input,
        decimal minimumAmount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumAmount);

        using var document = await JsonDocument.ParseAsync(input, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new FormatException("The file must contain a JSON array.");

        var selected = new List<Transaction>();
        var inputCount = 0;

        foreach (var item in document.RootElement.EnumerateArray())
        {
            inputCount++;
            var transaction = ParseTransaction(item, inputCount);

            if (transaction.Amount >= minimumAmount)
                selected.Add(transaction);
        }

        return new ProcessingResult(inputCount, selected.Count, selected);
    }

    private static Transaction ParseTransaction(JsonElement item, int index)
    {
        // Item should have the expected shape: an object with a nonempty string id and a nonnegative decimal amount.
        if (item.ValueKind != JsonValueKind.Object ||
            !item.TryGetProperty("id", out var id) ||
            id.ValueKind != JsonValueKind.String)
        {
            throw InvalidItem(index);
        }

        var transactionId = id.GetString();

        // Transactions should have valid identifiers and amounts: amount must be a nonnegative decimal number.
        if (string.IsNullOrWhiteSpace(transactionId) ||
            !item.TryGetProperty("amount", out var amount) ||
            amount.ValueKind != JsonValueKind.Number ||
            !amount.TryGetDecimal(out var value) ||
            value < 0)
        {
            throw InvalidItem(index);
        }

        return new Transaction(transactionId, value);
    }

    private static FormatException InvalidItem(int index) =>
        new($"Item {index} must have a nonempty string id and a nonnegative decimal amount.");

}