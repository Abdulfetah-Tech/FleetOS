using EthioFleet.Application.Interfaces;
using EthioFleet.Domain.Common;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Tests.UnitTests;

public sealed class AuditFieldStampingTests
{
    [Fact]
    public void SaveChangesStampsAuditAndTenantFieldsFromTrustedContext()
    {
        var now = new DateTimeOffset(2026, 10, 8, 7, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var entity = new AuditedEntity { Name = "foundation test" };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=not-used;Username=not-used;Password=not-used")
            .Options;

        using var context = new TestAppDbContext(
            options,
            new FixedClock(now),
            new TestCurrentUser(userId),
            new TestTenantContext(organizationId));
        context.Entities.Add(entity);
        context.StampAuditFields();

        Assert.Equal(now, entity.CreatedAt);
        Assert.Equal(now, entity.UpdatedAt);
        Assert.Equal(userId, entity.CreatedBy);
        Assert.Equal(userId, entity.UpdatedBy);
        Assert.Equal(organizationId, entity.OrganizationId);
        Assert.Equal(TimeSpan.Zero, entity.CreatedAt.Offset);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;
        public Guid? OrganizationId => null;
        public string? Email => null;
        public bool IsSystemAdmin => false;
    }

    private sealed class TestTenantContext(Guid organizationId) : ITenantContext
    {
        public Guid? OrganizationId { get; } = organizationId;
        public bool IsSystemAdmin => false;
    }

    private sealed class TestAppDbContext(
        DbContextOptions<AppDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        ITenantContext tenantContext)
        : AppDbContext(options, clock, currentUser, tenantContext)
    {
        public DbSet<AuditedEntity> Entities => Set<AuditedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditedEntity>();
            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class AuditedEntity : IAuditable, ITenantOwned, ISoftDeletable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid OrganizationId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
