using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace EthioFleet.Tests.IntegrationTests;

public sealed class ApiFoundationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ethiofleet_test")
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
                    ["Cors:AllowedOrigins:0"] = "http://localhost:4200"
                });
            });
        });
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
    public async Task HealthEndpointsReportLiveAndPostgresReady()
    {
        using var client = _factory!.CreateClient();

        using var liveResponse = await client.GetAsync("/health/live");
        using var readyResponse = await client.GetAsync("/health/ready");
        var readyBody = await readyResponse.Content.ReadAsStringAsync();
        using var readyJson = JsonDocument.Parse(readyBody);

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        Assert.Equal("healthy", readyJson.RootElement.GetProperty("status").GetString());
        Assert.Contains("postgresql", readyJson.RootElement.GetProperty("checks").EnumerateObject()
            .Select(property => property.Name));
    }

}
