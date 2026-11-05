using System.Linq.Expressions;
using EthioFleet.Application.Interfaces;
using EthioFleet.Domain.Common;
using EthioFleet.Domain.Organizations;
using EthioFleet.Domain.Users;
using EthioFleet.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EthioFleet.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppIdentityUser, IdentityRole<Guid>, Guid>
{
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        ITenantContext tenantContext)
        : base(options)
    {
        _clock = clock;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> DomainUsers { get; set; }
    public DbSet<Role> DomainRoles { get; set; }
    public DbSet<UserRole> DomainUserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public Guid? CurrentOrganizationId => _tenantContext.OrganizationId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.FamilyId, token.RevokedAt, token.ExpiresAt });
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(token => token.CreatedByIp).HasMaxLength(64);
            entity.Property(token => token.RevokedByIp).HasMaxLength(64);
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppIdentityUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasFilter("normalized_email IS NOT NULL");
        modelBuilder.Entity<AppIdentityUser>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(user => user.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => !type.IsOwned()).ToArray())
        {
            ConfigureUtcColumns(entityType);
            ConfigureConcurrencyToken(modelBuilder, entityType);
            ConfigureQueryFilter(modelBuilder, entityType);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    internal void StampAuditFields()
    {
        AuditFieldStamper.Apply(ChangeTracker, _clock, _currentUser, _tenantContext);
    }

    private static void ConfigureUtcColumns(IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties().Where(property => property.ClrType == typeof(DateTimeOffset)
            || property.ClrType == typeof(DateTimeOffset?)))
        {
            property.SetColumnType("timestamp with time zone");
        }
    }

    private static void ConfigureConcurrencyToken(ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)
            || entityType.ClrType == typeof(AppIdentityUser)
            || entityType.ClrType == typeof(RefreshToken))
        {
            modelBuilder.Entity(entityType.ClrType).Property<uint>("xmin").IsRowVersion();
        }
    }

    private void ConfigureQueryFilter(ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType)
            && !typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
        {
            return;
        }

        var parameter = Expression.Parameter(entityType.ClrType, "entity");
        Expression? filter = null;

        if (typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
        {
            var tenantEntity = Expression.Convert(parameter, typeof(ITenantOwned));
            var entityOrganizationId = Expression.Property(tenantEntity, nameof(ITenantOwned.OrganizationId));
            var currentOrganizationId = Expression.Property(Expression.Constant(this), nameof(CurrentOrganizationId));
            var hasOrganization = Expression.Property(currentOrganizationId, nameof(Nullable<Guid>.HasValue));
            var organizationValue = Expression.Property(currentOrganizationId, nameof(Nullable<Guid>.Value));
            filter = Expression.AndAlso(
                hasOrganization,
                Expression.Equal(entityOrganizationId, organizationValue));
        }

        if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
        {
            var softDeletableEntity = Expression.Convert(parameter, typeof(ISoftDeletable));
            var isNotDeleted = Expression.Not(
                Expression.Property(softDeletableEntity, nameof(ISoftDeletable.IsDeleted)));
            filter = filter is null ? isNotDeleted : Expression.AndAlso(filter, isNotDeleted);
        }

        if (filter is not null)
        {
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(filter, parameter));
        }
    }
}
