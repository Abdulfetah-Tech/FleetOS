# Build stage
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Copy solution and project files
COPY EthioFleet.slnx .
COPY backend/EthioFleet.Api/EthioFleet.Api.csproj backend/EthioFleet.Api/
COPY backend/EthioFleet.Application/EthioFleet.Application.csproj backend/EthioFleet.Application/
COPY backend/EthioFleet.Domain/EthioFleet.Domain.csproj backend/EthioFleet.Domain/
COPY backend/EthioFleet.Infrastructure/EthioFleet.Infrastructure.csproj backend/EthioFleet.Infrastructure/
COPY backend/EthioFleet.Tests/EthioFleet.Tests.csproj backend/EthioFleet.Tests/

# Restore dependencies
RUN dotnet restore EthioFleet.slnx

# Copy source code
COPY backend/ backend/

# Build and publish
WORKDIR /src/backend/EthioFleet.Api
RUN dotnet publish EthioFleet.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser:appuser /app
USER appuser

# Copy published app
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

# Entry point
ENTRYPOINT ["dotnet", "EthioFleet.Api.dll"]