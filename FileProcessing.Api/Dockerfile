FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FileProcessing.Application/FileProcessing.Application.csproj FileProcessing.Application/
COPY FileProcessing.Api/FileProcessing.Api.csproj FileProcessing.Api/
RUN dotnet restore FileProcessing.Api/FileProcessing.Api.csproj

COPY FileProcessing.Application/ FileProcessing.Application/
COPY FileProcessing.Api/ FileProcessing.Api/
RUN dotnet publish FileProcessing.Api/FileProcessing.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileProcessing.Api.dll"]