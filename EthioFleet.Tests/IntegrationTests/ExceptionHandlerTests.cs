using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EthioFleet.Tests.IntegrationTests;

public sealed class ExceptionHandlerTests
{
    [Fact]
    public async Task ExceptionHandlerReturnsProblemDetailsWithoutInternalDetails()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Database=not_used;Username=not_used;Password=not_used",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:4200"
                });
            });
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/__test/exception");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"status\":500", body, StringComparison.Ordinal);
        Assert.DoesNotContain("private diagnostic detail", body, StringComparison.Ordinal);
    }
}
