using FileProcessing.Application.Interfaces;
using FileProcessing.Application.Models;
using FileProcessing.Application.Processing;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace FileProcessing.Api.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/files/process", ProcessFileAsync)
            .DisableAntiforgery();

        app.MapGet("/api/files/report", GetReport);

        return app;
    }

    private static async Task<IResult> ProcessFileAsync(
        IFormFile file,
        HttpRequest request,
        JsonTransactionProcessor processor,
        ITrackingService trackingService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (file.Length is 0 or > 1_048_576 || !string.Equals(Path.GetExtension(file.FileName), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                error = "Provide a nonempty .json file of at most 1 MB."
            });
        }

        decimal minimumAmount = 0m;
        string minimumInput = request.Query["minimumAmount"].ToString();

        if (!string.IsNullOrEmpty(minimumInput)
            && (!decimal.TryParse(minimumInput, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out minimumAmount)
            || minimumAmount < 0))
        {
            return Results.BadRequest(new
            {
                error = "minimumAmount must be a nonnegative decimal."
            });
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            await using var stream = file.OpenReadStream();

            ProcessingResult result = await processor.ProcessAsync(stream, minimumAmount, cancellationToken);

            stopwatch.Stop();

            string fileName = Path.GetFileName(file.FileName);

            trackingService.Record(new ProcessedFile(
                fileName,
                DateTimeOffset.UtcNow,
                stopwatch.ElapsedMilliseconds,
                result.InputCount,
                result.OutputCount));

            ILogger logger = loggerFactory.CreateLogger("FileProcessing.Api.Endpoints.FileEndpoints");

            logger.LogInformation(
                "Processed file {FileName}: {InputCount} input items, {OutputCount} output items in {ProcessingTimeMilliseconds} ms",
                fileName,
                result.InputCount,
                result.OutputCount,
                stopwatch.ElapsedMilliseconds);

            return Results.Ok(result);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "The file contains invalid JSON." });
        }
        catch (FormatException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
    }

    private static IResult GetReport(ITrackingService trackingService)
    {
        return Results.Ok(new
        {
            totalProcessed = trackingService.TotalProcessed,
            recentFiles = trackingService.GetRecent()
        });
    }
}