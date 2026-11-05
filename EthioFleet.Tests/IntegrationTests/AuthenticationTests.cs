using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
