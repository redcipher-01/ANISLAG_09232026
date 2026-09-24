# File Processing API

A .NET 10 ASP.NET Core API for processing uploaded JSON transaction files. It filters transactions by a minimum amount and keeps a report of files processed during the current application run.

## What it does

- Accepts a JSON file through a multipart form upload.
- Validates each transaction and returns those whose amount meets the requested minimum.
- Requires an API key for the processing and reporting endpoints.
- Records the filename, processing time, timestamp, and transaction counts for successful uploads.
- Exposes a report containing the total processed count and the 100 most recent files.
- Includes a Dockerfile and a GitHub Actions workflow that builds and tests the container.

## Requirements

- .NET 10 SDK
- Visual Studio with ASP.NET Core development support, or the .NET CLI
- A container engine if you want to build and run the Docker image locally

## Solution layout

| Location | Responsibility |
| --- | --- |
| `FileProcessing.Api/Endpoints` | Routes, HTTP input validation, and responses |
| `FileProcessing.Api/Middleware` | API key validation |
| `FileProcessing.Api/Services` | In-memory tracking implementation |
| `FileProcessing.Application/Models` | Transaction and processing data |
| `FileProcessing.Application/Processing` | JSON parsing, validation, and filtering |
| `FileProcessing.Application/Interfaces` | Tracking service contract |
| `FileProcessing.Tests` | Tests for the transaction processor |
| `samples` | Example upload file |

The project reference runs from `FileProcessing.Api` to `FileProcessing.Application`. The Application project does not reference ASP.NET Core, so the transaction processing logic can be tested without an HTTP request.

This is a lightweight layered architecture for the size of the exercise. `FileEndpoints` handles HTTP concerns, `JsonTransactionProcessor` handles the processing rules, and `InMemoryTrackingService` stores the reporting data. A separate Domain project was unnecessary for the current model and rules. If the application grows, the existing boundary leaves room to introduce persistent storage or split out more complex business rules.

## Build and test

Open `ANISLAG_09232026.slnx` in Visual Studio and build the solution. Tests can be run through Test Explorer.

The equivalent commands from the repository root are:

```powershell
dotnet build .\ANISLAG_09232026.slnx
dotnet test .\FileProcessing.Tests\FileProcessing.Tests.csproj
```

## API key configuration

Both `/api` endpoints require an `X-Api-Key` request header. The API reads the expected value from the `ApiKey` configuration setting.

For local development in Visual Studio:

1. Right-click `FileProcessing.Api`.
2. Select **Manage User Secrets**.
3. Set `ApiKey` in the secrets file:

```json
{
  "ApiKey": "f47a16b0e79c465a8d2f36c097ea51b44c718d0e92f653ab1c8d495f760a2e35"
}
```

This is a public example key for trying the API. Use a different key for any other environment. The configured development secret is stored outside the repository.

The middleware applies to `/api` routes. It requires exactly one `X-Api-Key` header and compares the supplied value with the configured key. `/health` is public.

| Situation | Response |
| --- | --- |
| Valid configured key supplied | Request continues to the endpoint |
| Missing or incorrect request key | `401 Unauthorized` |
| `ApiKey` configuration missing | `503 Service Unavailable` |

## Run in Visual Studio

Set `FileProcessing.Api` as the startup project and select its HTTPS profile. Start the API and use the HTTPS address shown by Visual Studio.

The examples below use `https://localhost:7134`, which is the port used while developing this solution. If Visual Studio assigns a different port on your machine, replace `7134` in the commands.

### Health check

```powershell
curl.exe -i "https://localhost:7134/health"
```

Response body:

```json
{"status":"healthy"}
```

## Endpoints

| Method | Route | API key | Description |
| --- | --- | --- | --- |
| `GET` | `/health` | No | Indicates that the API is running |
| `POST` | `/api/files/process` | Yes | Processes an uploaded JSON file |
| `GET` | `/api/files/report` | Yes | Returns processed-file statistics |

### POST `/api/files/process`

Send a `multipart/form-data` request with a file field named `file`. The optional `minimumAmount` query parameter defaults to `0`. The comparison is inclusive, so an amount equal to the minimum is included.

The file must have a `.json` extension and must not exceed 1 MiB. Kestrel's request body limit is set to 2 MiB to allow for multipart form overhead.

From the repository root, process the included sample with a minimum amount of `75`:

```powershell
curl.exe -i `
  -H "X-Api-Key: f47a16b0e79c465a8d2f36c097ea51b44c718d0e92f653ab1c8d495f760a2e35" `
  -F "file=@.\samples\transactions.json" `
  "https://localhost:7134/api/files/process?minimumAmount=75"
```

The [sample file](samples/transactions.json) contains four transactions. The response includes the two with amounts of at least `75`:

```json
{
  "inputCount": 4,
  "outputCount": 2,
  "items": [
    {
      "id": "INV-1044",
      "amount": 75.00
    },
    {
      "id": "INV-1045",
      "amount": 150.25
    }
  ]
}
```

The JSON file must contain an array of objects. Each transaction must have:

- An `id` containing a nonblank string.
- An `amount` containing a nonnegative decimal number.

The processor rejects malformed JSON and invalid transactions instead of returning a partial result. The endpoint also rejects an invalid or negative `minimumAmount`. Only successfully processed uploads are added to the report.

### GET `/api/files/report`

```powershell
curl.exe -i `
  -H "X-Api-Key: f47a16b0e79c465a8d2f36c097ea51b44c718d0e92f653ab1c8d495f760a2e35" `
  "https://localhost:7134/api/files/report"
```

Example response after processing the sample once:

```json
{
  "totalProcessed": 1,
  "recentFiles": [
    {
      "fileName": "transactions.json",
      "processedAtUtc": "2026-09-24T12:00:00+00:00",
      "processingTimeMilliseconds": 5,
      "inputCount": 4,
      "outputCount": 2
    }
  ]
}
```

The timestamp and processing time above are examples. Actual values depend on when the file was processed and how long the request took.

`totalProcessed` counts all successful uploads during the current application run. `recentFiles` contains up to 100 records, newest first. Failed uploads do not increase the count.

Each successful upload is also logged with the filename, input and output counts, and processing time.

Tracking is stored in a singleton in-memory service. Access to its count and recent records is synchronized so concurrent requests can update it safely. The data is cleared when the application restarts. If several API instances are running, each instance keeps its own report; a shared database would be needed for a combined report.

To verify that the report is protected, call it without the header:

```powershell
curl.exe -i "https://localhost:7134/api/files/report"
```

With an API key configured, the expected status is `401 Unauthorized`.

## Processing and request flow

For a processing request:

1. `ApiKeyMiddleware` checks the `X-Api-Key` header before allowing the request to reach the endpoint.
2. `FileEndpoints` checks the uploaded file and parses `minimumAmount`.
3. `JsonTransactionProcessor` reads the JSON, validates each transaction, and filters by the minimum.
4. After successful processing, the endpoint records the filename, UTC timestamp, elapsed processing time, and input/output counts.
5. The endpoint returns the processing result as JSON.

The processing logic does not depend on the file tracking implementation. The endpoint coordinates the two operations after handling the HTTP request.

## Test scope

`FileProcessing.Tests` checks the main filtering behavior and representative invalid inputs. The suite does not attempt to cover every possible JSON value, boundary, or HTTP response. It establishes tests around the core rule while keeping the initial implementation focused on the requested API, security, reporting, and container support.

The next useful additions would be HTTP-level tests for authentication and upload validation, boundary tests for file size and amounts, and tests for concurrent reporting. Those become more valuable as the service gains new requirements.

## Docker

The [Dockerfile](FileProcessing.Api/Dockerfile) uses two stages:

1. The .NET 10 SDK image restores and publishes the API and its Application project reference.
2. The ASP.NET Core 10 runtime image runs the published output on port `8080`.

Build from the repository root. The final `.` in the command is the build context and allows Docker to access both projects:

```powershell
docker build -f .\FileProcessing.Api\Dockerfile -t fileprocessing-api:local .
```

With a local container engine available, start the image with an API key:

```powershell
docker run --rm -p 8080:8080 `
  -e "ApiKey=f47a16b0e79c465a8d2f36c097ea51b44c718d0e92f653ab1c8d495f760a2e35" `
  fileprocessing-api:local
```

Then check:

```powershell
curl.exe -i "http://localhost:8080/health"
```

The container example uses HTTP on the mapped local port. The Visual Studio development example uses HTTPS.

Docker Desktop is restricted on the development laptop, so local Docker Desktop testing was not possible. As an alternative, the [GitHub Actions workflow](.github/workflows/ci.yml) builds the image and starts it on a GitHub-hosted Ubuntu runner. It runs the .NET tests, checks `/health`, and verifies that the protected report rejects a request without an API key. The workflow generates a temporary key for the container test.

The image is built and smoke-tested by the workflow; it is not published to a container registry.

## Possible extensions

The current report is intentionally small and in memory. `ITrackingService` provides a boundary for a database-backed implementation if processing history needs to survive restarts or be shared across instances.

Other improvements would include broader automated test coverage, configurable report retention, and additional processing metrics.