using System.Text;
using FileProcessing.Application.Processing;
using Xunit;

namespace FileProcessing.Tests;

public sealed class JsonTransactionProcessorTests
{
    [Fact]
    public async Task ProcessAsync_IncludesAmountsAtOrAboveMinimum()
    {
        const string json = """
            [
                { "id": "INV-1042", "amount": 0 },
                { "id": "INV-1043", "amount": 49.99 },
                { "id": "INV-1044", "amount": 75.00 },
                { "id": "INV-1045", "amount": 150.25 }
            ]
            """;

        using var stream = ToStream(json);
        var processor = new JsonTransactionProcessor();

        var result = await processor.ProcessAsync(stream, 75m);

        Assert.Equal(4, result.InputCount);
        Assert.Equal(2, result.OutputCount);
        Assert.Equal(new[] { "INV-1044", "INV-1045" }, result.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData("""{"id":"INV-3001","amount":10}""")]
    [InlineData("""[{"id":"","amount":10}]""")]
    [InlineData("""[{"id":"INV-3002","amount":-1}]""")]
    [InlineData("""[{"id":"INV-3003"}]""")]
    public async Task ProcessAsync_RejectsInvalidStructureOrItem(string json)
    {
        using var stream = ToStream(json);
        var processor = new JsonTransactionProcessor();

        await Assert.ThrowsAsync<FormatException>(
            () => processor.ProcessAsync(stream, 0m));
    }

    private static MemoryStream ToStream(string json) =>
        new(Encoding.UTF8.GetBytes(json));
}