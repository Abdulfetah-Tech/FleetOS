using System.Threading.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Asp.Versioning;
using EthioFleet.Api.Health;
using EthioFleet.Api.Middleware;
using EthioFleet.Application;
using EthioFleet.Application.Authentication;
using EthioFleet.Infrastructure;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration["Authentication:Jwt:Issuer"] = "EthioFleet.Tests";
    builder.Configuration["Authentication:Jwt:Audience"] = "EthioFleet.Tests.Api";
    builder.Configuration["Authentication:Jwt:SigningKey"] = "test-only-signing-key-for-ethiofleet-0000000000001";
    builder.Configuration["Authentication:Jwt:AccessTokenMinutes"] = "15";
    builder.Configuration["Authentication:Jwt:RefreshTokenDays"] = "14";
}

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "EthioFleet")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck("live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"]);
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Configure at least one CORS origin in Cors:AllowedOrigins.");
}

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configure Authentication:Jwt.");
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException("Authentication:Jwt:SigningKey must contain at least 32 UTF-8 bytes and must be provided through secret configuration.");
}
if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || jwtOptions.AccessTokenMinutes is < 1 or > 60 || jwtOptions.RefreshTokenDays is < 1 or > 90)
{
    throw new InvalidOperationException("Configure valid issuer, audience, access-token, and refresh-token lifetimes under Authentication:Jwt.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TenantMember", policy => policy.RequireAuthenticatedUser()
        .RequireClaim("organization_id"));
    options.AddPolicy("CompanyOwner", policy => policy.RequireAuthenticatedUser()
        .RequireClaim("organization_id")
        .RequireRole("CompanyOwner"));
    options.AddPolicy("FleetManager", policy => policy.RequireAuthenticatedUser()
        .RequireClaim("organization_id")
        .RequireRole("CompanyOwner", "FleetManager"));
    options.AddPolicy("MaintenanceManager", policy => policy.RequireAuthenticatedUser()
        .RequireClaim("organization_id")
        .RequireRole("CompanyOwner", "MaintenanceManager"));
    options.AddPolicy("InspectionViewer", policy => policy.RequireAuthenticatedUser()
        .RequireClaim("organization_id")
        .RequireRole("CompanyOwner", "FleetManager", "MaintenanceManager"));
    options.AddPolicy("InspectionSubmitter", policy => policy.RequireAuthenticatedUser()
        .RequireClaim("organization_id")
        .RequireRole("CompanyOwner", "FleetManager", "MaintenanceManager", "Driver"));
    options.AddPolicy("FinanceManager", policy => policy.RequireAuthenticatedUser()
        .RequireClaim("organization_id")
        .RequireRole("CompanyOwner", "FinanceManager"));
});

builder.Services.AddCors(options =>
    options.AddPolicy("ConfiguredOrigins", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return Task.CompletedTask;
    });
    context.Response.Headers["X-Request-Id"] = context.TraceIdentifier;
    using (Serilog.Context.LogContext.PushProperty("RequestId", context.TraceIdentifier))
    {
        await next();
    }
});
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("ConfiguredOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await IdentitySeeder.SeedRolesAsync(roleManager);
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthResponseWriter.WriteAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync
});

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapGet("/api/v{version:apiVersion}", () =>
        Results.Ok(new { apiVersion = "v1" }))
    .WithApiVersionSet(versionSet)
    .MapToApiVersion(new ApiVersion(1, 0))
    .WithName("GetApiVersion");

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/exception", (HttpContext _) =>
        throw new InvalidOperationException("private diagnostic detail"));
}

app.Run();

public partial class Program;
