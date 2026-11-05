using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EthioFleet.Domain.Drivers;
using EthioFleet.Domain.Common;
using EthioFleet.Domain.Assignments;
using EthioFleet.Domain.Vehicles;
using EthioFleet.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace EthioFleet.Tests.IntegrationTests;

public sealed class AuthenticationTests : IAsyncLifetime
{
    private const string SigningKey = "test-only-signing-key-for-ethiofleet-0000000000001";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ethiofleet_auth_test")
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
    public async Task VehicleAssignmentPersistsHistoryAndEnforcesSingleActiveAssignment()
    {
        using var client = _factory!.CreateClient();
        var organizationId = Guid.NewGuid();
        await SeedOrganizationAsync(organizationId);
        var vehicle = CreateTestVehicle("ASSIGN-1");
        var driver = CreateTestDriver("assign-one@example.test", "LICENSE-ASSIGN-1");
        await using (var seedContext = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres.GetConnectionString()).UseSnakeCaseNamingConvention().Options,
            new TestClock(), new TestTenantContext(organizationId), new TestTenantContext(organizationId)))
        {
            seedContext.Vehicles.Add(vehicle);
            seedContext.Drivers.Add(driver);
            await seedContext.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), organizationId, "CompanyOwner"));
        var assignmentResponse = await client.PostAsJsonAsync($"/api/v1/vehicles/{vehicle.Id}/assignment", new
        {
            vehicleId = vehicle.Id,
            driverId = driver.Id,
            notes = "Day shift"
        });
        Assert.Equal(HttpStatusCode.Created, assignmentResponse.StatusCode);
        using var assignmentJson = JsonDocument.Parse(await assignmentResponse.Content.ReadAsStringAsync());
        var assignmentId = assignmentJson.RootElement.GetProperty("id").GetGuid();
        Assert.True(assignmentJson.RootElement.GetProperty("isActive").GetBoolean());

        var anotherVehicle = CreateTestVehicle("ASSIGN-2");
        await using (var seedVehicleContext = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres.GetConnectionString()).UseSnakeCaseNamingConvention().Options,
            new TestClock(), new TestTenantContext(organizationId), new TestTenantContext(organizationId)))
        {
            seedVehicleContext.Vehicles.Add(anotherVehicle);
            await seedVehicleContext.SaveChangesAsync();
        }
        var driverAlreadyAssigned = await client.PostAsJsonAsync($"/api/v1/vehicles/{anotherVehicle.Id}/assignment", new
        {
            vehicleId = anotherVehicle.Id,
            driverId = driver.Id
        });
        Assert.Equal(HttpStatusCode.Conflict, driverAlreadyAssigned.StatusCode);

        var unassigned = await client.DeleteAsync($"/api/v1/vehicles/{vehicle.Id}/assignment");
        Assert.Equal(HttpStatusCode.OK, unassigned.StatusCode);
        using var unassignedJson = JsonDocument.Parse(await unassigned.Content.ReadAsStringAsync());
        Assert.False(unassignedJson.RootElement.GetProperty("isActive").GetBoolean());
        Assert.True(unassignedJson.RootElement.GetProperty("unassignedAt").GetDateTimeOffset() >=
            unassignedJson.RootElement.GetProperty("assignedAt").GetDateTimeOffset());

        var activeForVehicle = await client.GetAsync($"/api/v1/vehicles/{vehicle.Id}/assignment");
        Assert.Equal(HttpStatusCode.NotFound, activeForVehicle.StatusCode);
        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var history = await verifyDb.VehicleAssignments.IgnoreQueryFilters()
            .SingleAsync(item => item.Id == assignmentId);
        Assert.False(history.IsActive);
    }

    [Fact]
    public async Task AssignmentCannotReferenceResourcesFromAnotherOrganization()
    {
        using var client = _factory!.CreateClient();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await SeedOrganizationAsync(tenantA);
        await SeedOrganizationAsync(tenantB);
        var vehicleA = CreateTestVehicle("CROSS-A");
        await using (var tenantAContext = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres.GetConnectionString()).UseSnakeCaseNamingConvention().Options,
            new TestClock(), new TestTenantContext(tenantA), new TestTenantContext(tenantA)))
        {
            tenantAContext.Vehicles.Add(vehicleA);
            await tenantAContext.SaveChangesAsync();
        }

        var driverB = CreateTestDriver("cross-b@example.test", "CROSS-B-LICENSE");
        await using (var tenantBContext = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres.GetConnectionString()).UseSnakeCaseNamingConvention().Options,
            new TestClock(), new TestTenantContext(tenantB), new TestTenantContext(tenantB)))
        {
            tenantBContext.Drivers.Add(driverB);
            await tenantBContext.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), tenantA, "CompanyOwner"));
        var response = await client.PostAsJsonAsync($"/api/v1/vehicles/{vehicleA.Id}/assignment", new
        {
            vehicleId = vehicleA.Id,
            driverId = driverB.Id
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await Assert.ThrowsAsync<DomainException>(() => Task.Run(() => new VehicleAssignment(
            tenantA, vehicleA, driverB, DateTimeOffset.UtcNow)));
    }

    [Fact]
    public async Task DriverApiSupportsTenantScopedCrudAndLicenseExpiry()
    {
        using var client = _factory!.CreateClient();
        var organizationId = Guid.NewGuid();
        await SeedOrganizationAsync(organizationId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), organizationId, "CompanyOwner"));

        var response = await client.PostAsJsonAsync("/api/v1/drivers", new
        {
            fullName = "Hana Bekele",
            email = "hana@example.test",
            phoneNumber = "+251911123456",
            licenseNumber = "ab-1234",
            licenseCategory = "B",
            licenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            emergencyContactName = "Abel Bekele",
            emergencyContactPhone = "+251922123456"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var driverId = json.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(organizationId, json.RootElement.GetProperty("organizationId").GetGuid());
        Assert.Equal("AB-1234", json.RootElement.GetProperty("licenseNumber").GetString());

        var list = await client.GetAsync("/api/v1/drivers?search=hana");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Equal(1, listJson.RootElement.GetProperty("totalCount").GetInt32());

        var inactive = await client.PatchAsJsonAsync($"/api/v1/drivers/{driverId}/status", new
        {
            status = DriverStatus.Inactive
        });
        Assert.Equal(HttpStatusCode.OK, inactive.StatusCode);
        var deleted = await client.DeleteAsync($"/api/v1/drivers/{driverId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/drivers/{driverId}")).StatusCode);
    }

    [Fact]
    public async Task DriverApiRejectsDuplicateTenantLicenseAndIsolatesRecords()
    {
        using var client = _factory!.CreateClient();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await SeedOrganizationAsync(tenantA);
        await SeedOrganizationAsync(tenantB);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), tenantA, "CompanyOwner"));

        var create = new
        {
            fullName = "Driver One",
            email = "driver.one@example.test",
            phoneNumber = "+251911000001",
            licenseNumber = "LIC-12345",
            licenseCategory = "B",
            licenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/drivers", create)).StatusCode);
        var duplicateLicense = await client.PostAsJsonAsync("/api/v1/drivers", create with
        {
            fullName = "Driver Two",
            email = "driver.two@example.test",
            phoneNumber = "+251911000002"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateLicense.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), tenantB, "FleetManager"));
        Assert.Equal(0, JsonDocument.Parse(await (await client.GetAsync("/api/v1/drivers")).Content.ReadAsStringAsync())
            .RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task VehicleApiSupportsCreateListGetUpdateStatusDeleteWithinTenant()
    {
        using var client = _factory!.CreateClient();
        var organizationId = Guid.NewGuid();
        await SeedOrganizationAsync(organizationId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), organizationId, "CompanyOwner"));

        var createdResponse = await client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            plateNumber = "ab-555 xy",
            make = "Toyota",
            model = "Hilux",
            year = 2022,
            type = VehicleType.Pickup,
            fuelType = FuelType.Diesel,
            currentMileage = 12345.5m,
            vin = "1HGCM82633A004352",
            currencyCode = "ETB"
        });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        using var createdJson = JsonDocument.Parse(await createdResponse.Content.ReadAsStringAsync());
        var vehicleId = createdJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(organizationId, createdJson.RootElement.GetProperty("organizationId").GetGuid());
        Assert.Equal("AB-555 XY", createdJson.RootElement.GetProperty("plateNumber").GetString());

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            plateNumber = "AB555XY",
            make = "Toyota",
            model = "Duplicate",
            year = 2022,
            type = VehicleType.Pickup,
            fuelType = FuelType.Diesel,
            currentMileage = 0m
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/vehicles?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using var listJson = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, listJson.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(organizationId, listJson.RootElement.GetProperty("items")[0].GetProperty("organizationId").GetGuid());

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/vehicles/{vehicleId}", new
        {
            plateNumber = "ab-555 xy",
            make = "Toyota",
            model = "Hilux SR5",
            year = 2022,
            type = VehicleType.Pickup,
            fuelType = FuelType.Diesel,
            vin = "1HGCM82633A004352",
            purchaseDate = (DateOnly?)null
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var statusResponse = await client.PatchAsJsonAsync($"/api/v1/vehicles/{vehicleId}/status", new
        {
            status = VehicleStatus.Maintenance
        });
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        using var statusJson = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync());
        Assert.Equal((int)VehicleStatus.Maintenance, statusJson.RootElement.GetProperty("status").GetInt32());

        var deleteResponse = await client.DeleteAsync($"/api/v1/vehicles/{vehicleId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/vehicles/{vehicleId}")).StatusCode);
    }

    [Fact]
    public async Task VehicleApiEnforcesTenantIsolationAndCompanyOwnerWritePolicy()
    {
        using var client = _factory!.CreateClient();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await SeedOrganizationAsync(tenantA);
        await SeedOrganizationAsync(tenantB);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), tenantA, "CompanyOwner"));

        var createdResponse = await client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            plateNumber = "TENANT-A-1",
            make = "Toyota",
            model = "Hilux",
            year = 2020,
            type = VehicleType.Pickup,
            fuelType = FuelType.Diesel,
            currentMileage = 0m
        });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        using var createdJson = JsonDocument.Parse(await createdResponse.Content.ReadAsStringAsync());
        var vehicleId = createdJson.RootElement.GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(Guid.NewGuid(), tenantB, "FleetManager"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/vehicles/{vehicleId}")).StatusCode);
        var forbiddenCreate = await client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            plateNumber = "TENANT-B-1",
            make = "Toyota",
            model = "Hilux",
            year = 2020,
            type = VehicleType.Pickup,
            fuelType = FuelType.Diesel,
            currentMileage = 0m
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreate.StatusCode);
    }

    [Fact]
    public async Task RegistrationLoginRefreshRotationAndLogoutWork()
    {
        using var client = _factory!.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.test";
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            userName = "fleet-owner",
            password = "ValidPass123!Secure",
            organizationName = "Auth Integration Test"
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        using var registrationJson = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        var tokens = registrationJson.RootElement.GetProperty("tokens");
        var accessToken = tokens.GetProperty("accessToken").GetString()!;
        var firstRefresh = tokens.GetProperty("refreshToken").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "ValidPass123!Secure" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var rotated = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = firstRefresh });
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        using var rotatedJson = JsonDocument.Parse(await rotated.Content.ReadAsStringAsync());
        var secondRefresh = rotatedJson.RootElement.GetProperty("tokens").GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(firstRefresh, secondRefresh);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = secondRefresh });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var replay = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = firstRefresh });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        var revokedSession = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = secondRefresh });
        Assert.Equal(HttpStatusCode.Unauthorized, revokedSession.StatusCode);
    }

    [Fact]
    public async Task OrganizationEndpointRequiresTenantClaimAndFleetRole()
    {
        using var client = _factory!.CreateClient();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateTestToken(userId, null));
        var missingTenant = await client.GetAsync("/api/v1/organizations/current");
        Assert.Equal(HttpStatusCode.Forbidden, missingTenant.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateTestToken(userId, organizationId));
        var currentOrganization = await client.GetAsync("/api/v1/organizations/current");
        Assert.Equal(HttpStatusCode.OK, currentOrganization.StatusCode);
        using var currentJson = JsonDocument.Parse(await currentOrganization.Content.ReadAsStringAsync());
        Assert.Equal(organizationId, currentJson.RootElement.GetProperty("organizationId").GetGuid());

        var deniedFleet = await client.GetAsync("/api/v1/organizations/current/fleet");
        Assert.Equal(HttpStatusCode.Forbidden, deniedFleet.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateTestToken(userId, organizationId, "FleetManager"));
        var fleetWorkspace = await client.GetAsync("/api/v1/organizations/current/fleet");
        Assert.Equal(HttpStatusCode.OK, fleetWorkspace.StatusCode);
    }

    private sealed class TestClock : EthioFleet.Application.Interfaces.IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class TestTenantContext(Guid organizationId) : EthioFleet.Application.Interfaces.ITenantContext, EthioFleet.Application.Interfaces.ICurrentUser
    {
        public Guid? OrganizationId { get; } = organizationId;
        public Guid? UserId => null;
        public string? Email => null;
        public bool IsSystemAdmin => false;
    }

    private static Vehicle CreateTestVehicle(string plate) => new(
        plate, "Toyota", "Hilux", 2022, VehicleType.Pickup, FuelType.Diesel, 100m);

    private static Driver CreateTestDriver(string email, string licenseNumber)
    {
        var driverId = Guid.NewGuid();
        var license = new DriverLicense(driverId, licenseNumber, "B", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        return new Driver(driverId, "Assignment Driver", email, "+251911000000", license);
    }

    private async Task SeedOrganizationAsync(Guid organizationId)
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Organizations.Add(new EthioFleet.Domain.Organizations.Organization(
            $"Tenant {organizationId:N}"[..20], $"ORG{organizationId:N}"[..20],
            $"{organizationId:N}@example.test", string.Empty, string.Empty)
        {
            Id = organizationId
        });
        await dbContext.SaveChangesAsync();
    }

    private static string CreateTestToken(Guid userId, Guid? organizationId, string? role = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "policy-test@example.test")
        };
        if (organizationId is Guid tenantId)
        {
            claims.Add(new Claim("organization_id", tenantId.ToString()));
        }
        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken("EthioFleet.Tests", "EthioFleet.Tests.Api", claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task AuthenticatedEndpointRejectsMissingBearerToken()
    {
        using var client = _factory!.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = "invalid" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
