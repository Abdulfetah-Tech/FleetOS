# EthioFleet API Documentation

## OpenAPI Specification

The OpenAPI 3.0 specification is automatically generated and published via GitHub Actions CI pipeline.

### Accessing the Spec

1. **CI Artifact**: After each successful build, the `api-spec.json` artifact is uploaded and available for download from the GitHub Actions workflow run.

2. **Swagger UI**: When the API is running locally or deployed, the interactive Swagger UI is available at:
   - Local: `http://localhost:8080/swagger`
   - Production: `https://api.ethiofleet.com/swagger`

### Generating Locally

If you need to generate the spec locally, ensure you have .NET 9.0 SDK installed, then run:

```bash
cd backend/EthioFleet.Api
dotnet tool restore
dotnet tool run swagger tofile --output ./api-spec.json ./bin/Release/net11.0/EthioFleet.Api.dll v1
```

### Converting to Markdown

To convert the JSON spec to Markdown documentation:

```bash
npx widdershins ./api-spec.json -o ./API.md
```

### API Versioning

The API uses URL versioning with the format `v1` (e.g., `/api/v1/vehicles`).

### Authentication

The API uses JWT Bearer token authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### Key Endpoints

- **Authentication**: `/api/v1/auth/*`
- **Vehicles**: `/api/v1/vehicles`
- **Trips**: `/api/v1/trips`
- **Fuel Management**: `/api/v1/fuel`
- **Maintenance**: `/api/v1/maintenance`
- **SignalR Hub**: `/hub/trip`

### Health Checks

- Liveness: `/health/live`
- Readiness: `/health/ready`