using FileProcessing.Api.Endpoints;
using FileProcessing.Api.Middleware;
using FileProcessing.Application.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<JsonTransactionProcessor>();

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 2_097_152);

var app = builder.Build();

app.UseMiddleware<ApiKeyMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapFileEndpoints();

app.Run();