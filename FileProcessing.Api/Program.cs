using FileProcessing.Api.Endpoints;
using FileProcessing.Api.Middleware;
using FileProcessing.Api.Services;
using FileProcessing.Application.Interfaces;
using FileProcessing.Application.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<JsonTransactionProcessor>();
builder.Services.AddSingleton<ITrackingService, InMemoryTrackingService>(); // temp only, since there's no db yet

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 2_097_152);

var app = builder.Build();

app.UseMiddleware<ApiKeyMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapFileEndpoints();

app.Run();