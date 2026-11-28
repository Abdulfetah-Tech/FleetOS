using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EthioFleet.Domain.Audit;
using EthioFleet.Domain.Organizations;
using EthioFleet.Domain.Vehicles;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace EthioFleet.Tests.IntegrationTests;

public sealed class AuditLogTests : IAsyncLifetime
{
    private const string SigningKey = "test-only-signing-key-for-ethiofleet-0000000000001";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("ethiofleet_audit_test")
        .WithUsername("ethiofleet_test")
        .WithPassword("local-test-only")
        .Build();
    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                    ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
                    ["Authentication:Jwt:Issuer"] = "EthioFleet.Tests",
                    ["Authentication:Jwt:Audience"] = "EthioFleet.Tests.Api",
                    ["Authentication:Jwt:SigningKey"] = SigningKey,
                    ["Authentication:Jwt:AccessTokenMinutes"] = "15",
                    ["Authentication:Jwt:RefreshTokenDays"] = "14"
                });
            });
        });

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task TenantMutationsRecordImmutableAndIsolatedAuditLogs()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await SeedOrganizationAsync(tenantA);
        await SeedOrganizationAsync(tenantB);

        using var clientA = _factory!.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), tenantA, "CompanyOwner"));

        var createResponse = await clientA.PostAsJsonAsync("/api/v1/vehicles", new
        {
            plateNumber = "AA-2-99001",
            vin = "1HGCM82633A099001",
            make = "Toyota",
            model = "Hilux",
            year = 2024,
            type = VehicleType.Pickup,
            fuelType = FuelType.Diesel,
            currentMileage = 120m
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var logsResponseA = await clientA.GetAsync("/api/v1/audit-logs?entityType=Vehicle");
        Assert.Equal(HttpStatusCode.OK, logsResponseA.StatusCode);
        using var logsJsonA = JsonDocument.Parse(await logsResponseA.Content.ReadAsStringAsync());
        var itemsA = logsJsonA.RootElement.GetProperty("items");
        Assert.True(itemsA.GetArrayLength() >= 1);
        Assert.Equal("VehicleCreated", itemsA[0].GetProperty("action").GetString());

        using var clientB = _factory!.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), tenantB, "CompanyOwner"));
        var logsResponseB = await clientB.GetAsync("/api/v1/audit-logs?entityType=Vehicle");
        Assert.Equal(HttpStatusCode.OK, logsResponseB.StatusCode);
        using var logsJsonB = JsonDocument.Parse(await logsResponseB.Content.ReadAsStringAsync());
        Assert.Equal(0, logsJsonB.RootElement.GetProperty("items").GetArrayLength());

        using var fleetManagerClient = _factory!.CreateClient();
        fleetManagerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), tenantA, "FleetManager"));
        Assert.Equal(HttpStatusCode.Forbidden, (await fleetManagerClient.GetAsync("/api/v1/audit-logs")).StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existingLog = await dbContext.AuditLogs.IgnoreQueryFilters().FirstAsync();
        dbContext.AuditLogs.Remove(existingLog);
        await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
    }

    private async Task SeedOrganizationAsync(Guid organizationId)
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Organizations.Add(new Organization(
            $"Audit {organizationId:N}"[..20],
            $"AUD{organizationId:N}"[..20],
            $"{organizationId:N}@example.test",
            string.Empty,
            string.Empty)
        {
            Id = organizationId
        });
        await dbContext.SaveChangesAsync();
    }

    private static string CreateTestToken(Guid userId, Guid organizationId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "audit-test@example.test"),
            new("organization_id", organizationId.ToString()),
            new(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken("EthioFleet.Tests", "EthioFleet.Tests.Api", claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
