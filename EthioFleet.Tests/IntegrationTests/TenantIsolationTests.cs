using EthioFleet.Application.Interfaces;
using EthioFleet.Domain.Users;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EthioFleet.Tests.IntegrationTests;

public sealed class TenantIsolationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ethiofleet_tenant_test")
        .WithUsername("ethiofleet_test")
        .WithPassword("local-test-only")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = CreateContext(Guid.NewGuid());
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task TenantQueryFilterOnlyReturnsRecordsForCurrentOrganization()
    {
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var userA = new User("a@example.test", "tenant-a-user", organizationA);
        var userB = new User("b@example.test", "tenant-b-user", organizationB);

        await using (var context = CreateContext(organizationA))
        {
            context.DomainUsers.Add(userA);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(organizationB))
        {
            context.DomainUsers.Add(userB);
            await context.SaveChangesAsync();
        }

        await using var tenantAContext = CreateContext(organizationA);
        await using var tenantBContext = CreateContext(organizationB);
        var tenantARecords = await tenantAContext.DomainUsers.AsNoTracking().ToListAsync();
        var tenantBRecords = await tenantBContext.DomainUsers.AsNoTracking().ToListAsync();

        Assert.Collection(tenantARecords, user => Assert.Equal("a@example.test", user.Email));
        Assert.Collection(tenantBRecords, user => Assert.Equal("b@example.test", user.Email));
        Assert.DoesNotContain(tenantARecords, user => user.OrganizationId != organizationA);
        Assert.DoesNotContain(tenantBRecords, user => user.OrganizationId != organizationB);
    }

    [Fact]
    public async Task TenantAssignmentIgnoresClientOrganizationAndUsesTrustedContext()
    {
        var clientSuppliedOrganization = Guid.NewGuid();
        var authenticatedOrganization = Guid.NewGuid();
        var user = new User("stamped@example.test", "stamped-user", clientSuppliedOrganization);

        await using var context = CreateContext(authenticatedOrganization);
        context.DomainUsers.Add(user);
        await context.SaveChangesAsync();

        Assert.Equal(authenticatedOrganization, user.OrganizationId);
        Assert.Null(await context.DomainUsers.SingleOrDefaultAsync(entity => entity.Id == user.Id &&
            entity.OrganizationId == clientSuppliedOrganization));
        Assert.NotNull(await context.DomainUsers.SingleOrDefaultAsync(entity => entity.Id == user.Id));
    }

    [Fact]
    public async Task MissingTenantContextCannotReadOrCreateTenantOwnedData()
    {
        var organizationId = Guid.NewGuid();
        var user = new User("no-tenant@example.test", "no-tenant-user", organizationId);
        await using (var tenantContext = CreateContext(organizationId))
        {
            tenantContext.DomainUsers.Add(user);
            await tenantContext.SaveChangesAsync();
        }

        await using var anonymousContext = CreateContext(null);
        Assert.Empty(await anonymousContext.DomainUsers.ToListAsync());
        var newUser = new User("blocked@example.test", "blocked-user", organizationId);
        anonymousContext.DomainUsers.Add(newUser);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => anonymousContext.SaveChangesAsync());
    }

    [Fact]
    public async Task CrossTenantModificationIsRejectedDuringSaveChanges()
    {
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var userA = new User("protected@example.test", "protected-user", organizationA);
        await using (var tenantAContext = CreateContext(organizationA))
        {
            tenantAContext.DomainUsers.Add(userA);
            await tenantAContext.SaveChangesAsync();
        }

        await using var tenantBContext = CreateContext(organizationB);
        tenantBContext.DomainUsers.Attach(userA);
        tenantBContext.Entry(userA).Property(entity => entity.Email).IsModified = true;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => tenantBContext.SaveChangesAsync());
    }

    private AppDbContext CreateContext(Guid? organizationId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        var tenant = new TestTenantContext(organizationId);
        return new AppDbContext(options, new TestClock(), tenant, tenant);
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class TestTenantContext(Guid? organizationId) : ITenantContext, ICurrentUser
    {
        public Guid? OrganizationId { get; } = organizationId;
        public Guid? UserId => null;
        public string? Email => null;
        public bool IsSystemAdmin => false;
    }
}
