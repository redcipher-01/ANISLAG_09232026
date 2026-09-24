using FileProcessing.Application.Models;
using FileProcessing.Application.Processing;
using System.Globalization;
using System.Text.Json;

namespace FileProcessing.Api.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/files/process", ProcessFileAsync)
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> ProcessFileAsync(
        IFormFile file,
        HttpRequest request,
        JsonTransactionProcessor processor,
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

        try
        {
            await using var stream = file.OpenReadStream();

            ProcessingResult result = await processor.ProcessAsync(stream, minimumAmount, cancellationToken);

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
}