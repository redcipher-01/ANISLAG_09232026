using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;
using System.Text;

namespace FileProcessing.Api.Middleware;

public sealed class ApiKeyMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<ApiKeyMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        string? configuredKey = configuration["ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            logger.LogError("API key is not configured");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        StringValues supplied = context.Request.Headers["X-Api-Key"];
        byte[] expectedBytes = Encoding.UTF8.GetBytes(configuredKey);
        byte[] suppliedBytes = Encoding.UTF8.GetBytes(supplied.ToString());

        // Require one API key whose bytes match the configured key.
        if (supplied.Count != 1 
            || suppliedBytes.Length != expectedBytes.Length 
            || !CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}