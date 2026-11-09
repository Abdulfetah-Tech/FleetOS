using System.Linq.Expressions;
using EthioFleet.Application.Interfaces;
using EthioFleet.Domain.Assignments;
using EthioFleet.Domain.Common;
using EthioFleet.Domain.Documents;
using EthioFleet.Domain.Drivers;
using EthioFleet.Domain.Fuel;
using EthioFleet.Domain.Maintenance;
using EthioFleet.Domain.Organizations;
using EthioFleet.Domain.Users;
using EthioFleet.Domain.Trips;
using EthioFleet.Domain.Vehicles;
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
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<VehicleAssignment> VehicleAssignments { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<FuelRecord> FuelRecords { get; set; }
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
    public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
    public DbSet<VehicleDocument> VehicleDocuments { get; set; }
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

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasAlternateKey(vehicle => new { vehicle.OrganizationId, vehicle.Id });
            entity.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.NormalizedPlateNumber })
                .IsUnique();
            entity.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.NormalizedVin })
                .IsUnique()
                .HasFilter("normalized_vin IS NOT NULL");
            entity.HasOne<Organization>()
                .WithMany()
                .HasForeignKey(vehicle => vehicle.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasAlternateKey(driver => new { driver.OrganizationId, driver.Id });
            entity.HasIndex(driver => new { driver.OrganizationId, driver.NormalizedEmail })
                .IsUnique();
            entity.HasOne<Organization>()
                .WithMany()
                .HasForeignKey(driver => driver.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<VehicleAssignment>(entity =>
        {
            entity.HasAlternateKey(assignment => new { assignment.OrganizationId, assignment.Id });
            entity.HasIndex(assignment => new { assignment.OrganizationId, assignment.VehicleId })
                .IsUnique()
                .HasFilter("is_active = true");
            entity.HasIndex(assignment => new { assignment.OrganizationId, assignment.DriverId })
                .IsUnique()
                .HasFilter("is_active = true");
            entity.HasOne(assignment => assignment.Vehicle)
                .WithMany()
                .HasForeignKey(assignment => new { assignment.OrganizationId, assignment.VehicleId })
                .HasPrincipalKey(vehicle => new { vehicle.OrganizationId, vehicle.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(assignment => assignment.Driver)
                .WithMany()
                .HasForeignKey(assignment => new { assignment.OrganizationId, assignment.DriverId })
                .HasPrincipalKey(driver => new { driver.OrganizationId, driver.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasAlternateKey(trip => new { trip.OrganizationId, trip.Id });
            entity.HasIndex(trip => new { trip.OrganizationId, trip.Status, trip.PlannedStart });
            entity.HasIndex(trip => new { trip.OrganizationId, trip.VehicleId, trip.PlannedStart });
            entity.Property(trip => trip.StartMileage).HasPrecision(12, 1);
            entity.Property(trip => trip.EndMileage).HasPrecision(12, 1);
            entity.Property(trip => trip.Origin).HasMaxLength(200).IsRequired();
            entity.Property(trip => trip.Destination).HasMaxLength(200).IsRequired();
            entity.Property(trip => trip.Purpose).HasMaxLength(500).IsRequired();
            entity.Property(trip => trip.Notes).HasMaxLength(2000).IsRequired();
            entity.HasOne(trip => trip.Vehicle)
                .WithMany()
                .HasForeignKey(trip => new { trip.OrganizationId, trip.VehicleId })
                .HasPrincipalKey(vehicle => new { vehicle.OrganizationId, vehicle.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(trip => trip.Driver)
                .WithMany()
                .HasForeignKey(trip => new { trip.OrganizationId, trip.DriverId })
                .HasPrincipalKey(driver => new { driver.OrganizationId, driver.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(trip => trip.VehicleAssignment)
                .WithMany()
                .HasForeignKey(trip => new { trip.OrganizationId, trip.VehicleAssignmentId })
                .HasPrincipalKey(assignment => new { assignment.OrganizationId, assignment.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasAlternateKey(trip => new { trip.OrganizationId, trip.Id });
            entity.HasIndex(trip => new { trip.OrganizationId, trip.Status, trip.PlannedStart });
            entity.HasIndex(trip => new { trip.OrganizationId, trip.VehicleId, trip.PlannedStart });
            entity.Property(trip => trip.StartMileage).HasPrecision(12, 1);
            entity.Property(trip => trip.EndMileage).HasPrecision(12, 1);
            entity.Property(trip => trip.Origin).HasMaxLength(200).IsRequired();
            entity.Property(trip => trip.Destination).HasMaxLength(200).IsRequired();
            entity.Property(trip => trip.Purpose).HasMaxLength(500).IsRequired();
            entity.Property(trip => trip.Notes).HasMaxLength(2000).IsRequired();
            entity.HasOne(trip => trip.Vehicle)
                .WithMany()
                .HasForeignKey(trip => new { trip.OrganizationId, trip.VehicleId })
                .HasPrincipalKey(vehicle => new { vehicle.OrganizationId, vehicle.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(trip => trip.Driver)
                .WithMany()
                .HasForeignKey(trip => new { trip.OrganizationId, trip.DriverId })
                .HasPrincipalKey(driver => new { driver.OrganizationId, driver.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(trip => trip.VehicleAssignment)
                .WithMany()
                .HasForeignKey(trip => new { trip.OrganizationId, trip.VehicleAssignmentId })
                .HasPrincipalKey(assignment => new { assignment.OrganizationId, assignment.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FuelRecord>(entity =>
        {
            entity.HasIndex(record => new { record.OrganizationId, record.VehicleId, record.OccurredAt });
            entity.HasIndex(record => new { record.OrganizationId, record.DriverId, record.OccurredAt });
            entity.Property(record => record.QuantityLiters).HasPrecision(12, 3);
            entity.Property(record => record.UnitPrice).HasPrecision(12, 2);
            entity.Property(record => record.TotalCost).HasPrecision(14, 2);
            entity.Property(record => record.Odometer).HasPrecision(12, 1);
            entity.Property(record => record.Station).HasMaxLength(160).IsRequired();
            entity.Property(record => record.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(record => record.ReceiptReference).HasMaxLength(100).IsRequired();
            entity.Property(record => record.Notes).HasMaxLength(1000).IsRequired();
            entity.HasOne(record => record.Vehicle)
                .WithMany()
                .HasForeignKey(record => new { record.OrganizationId, record.VehicleId })
                .HasPrincipalKey(vehicle => new { vehicle.OrganizationId, vehicle.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(record => record.Driver)
                .WithMany()
                .HasForeignKey(record => new { record.OrganizationId, record.DriverId })
                .HasPrincipalKey(driver => new { driver.OrganizationId, driver.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(record => record.Trip)
                .WithMany()
                .HasForeignKey(record => new { record.OrganizationId, record.TripId })
                .HasPrincipalKey(trip => new { trip.OrganizationId, trip.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasIndex(record => new { record.OrganizationId, record.VehicleId, record.PerformedOn });
            entity.Property(record => record.Description).HasMaxLength(500).IsRequired();
            entity.Property(record => record.Odometer).HasPrecision(12, 1);
            entity.Property(record => record.LaborCost).HasPrecision(14, 2);
            entity.Property(record => record.PartsCost).HasPrecision(14, 2);
            entity.Property(record => record.TotalCost).HasPrecision(14, 2);
            entity.Property(record => record.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(record => record.ServiceProvider).HasMaxLength(160).IsRequired();
            entity.Property(record => record.Reference).HasMaxLength(100).IsRequired();
            entity.Property(record => record.Notes).HasMaxLength(2000).IsRequired();
            entity.HasOne(record => record.Vehicle)
                .WithMany()
                .HasForeignKey(record => new { record.OrganizationId, record.VehicleId })
                .HasPrincipalKey(vehicle => new { vehicle.OrganizationId, vehicle.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<MaintenanceSchedule>(entity =>
        {
            entity.HasIndex(schedule => new { schedule.OrganizationId, schedule.Status, schedule.NextDueDate });
            entity.HasIndex(schedule => new { schedule.OrganizationId, schedule.VehicleId, schedule.NextDueMileage });
            entity.Property(schedule => schedule.NextDueMileage).HasPrecision(12, 1);
            entity.Property(schedule => schedule.IntervalMileage).HasPrecision(12, 1);
            entity.Property(schedule => schedule.Name).HasMaxLength(160).IsRequired();
            entity.Property(schedule => schedule.Description).HasMaxLength(500).IsRequired();
            entity.HasOne(schedule => schedule.Vehicle)
                .WithMany()
                .HasForeignKey(schedule => new { schedule.OrganizationId, schedule.VehicleId })
                .HasPrincipalKey(vehicle => new { vehicle.OrganizationId, vehicle.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VehicleDocument>(entity =>
        {
            entity.HasIndex(document => new { document.OrganizationId, document.VehicleId, document.ExpiresOn });
            entity.HasIndex(document => new { document.OrganizationId, document.VehicleId, document.Type, document.Status });
            entity.HasIndex(document => document.StorageKey).IsUnique();
            entity.Property(document => document.Name).HasMaxLength(160).IsRequired();
            entity.Property(document => document.FileName).HasMaxLength(255).IsRequired();
            entity.Property(document => document.MediaType).HasMaxLength(127).IsRequired();
            entity.Property(document => document.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(document => document.ChecksumSha256).HasMaxLength(64).IsRequired();
            entity.HasOne(document => document.Vehicle)
                .WithMany()
                .HasForeignKey(document => new { document.OrganizationId, document.VehicleId })
                .HasPrincipalKey(vehicle => new { vehicle.OrganizationId, vehicle.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VehicleDocument>(entity =>
        {
            entity.HasIndex(document => new { document.OrganizationId, document.VehicleId, document.ExpiresOn });
            entity.HasIndex(document => new { document.OrganizationId, document.VehicleId, document.Type, document.Status });
            entity.HasIndex(document => document.StorageKey).IsUnique();
            entity.Property(document => document.Name).HasMaxLength(160).IsRequired();
            entity.Property(document => document.FileName).HasMaxLength(255).IsRequired();
            entity.Property(document => document.MediaType).HasMaxLength(127).IsRequired();
            entity.Property(document => document.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(document => document.ChecksumSha256).HasMaxLength(64).IsRequired();
            entity.HasOne(document => document.Vehicle)
                .WithMany()
                .HasForeignKey(document => new { document.OrganizationId, document.VehicleId })
                .HasPrincipalKey(vehicle => new { vehicle.OrganizationId, vehicle.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DriverLicense>(entity =>
        {
            entity.HasIndex(license => new { license.OrganizationId, license.NormalizedNumber })
                .IsUnique();
            entity.HasOne<Driver>()
                .WithOne(driver => driver.License)
                .HasForeignKey<DriverLicense>(license => license.DriverId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Organization>()
                .WithMany()
                .HasForeignKey(license => license.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Organization>()
            .HasIndex(organization => organization.Code)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(user => new { user.OrganizationId, user.NormalizedEmail })
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(user => new { user.OrganizationId, user.NormalizedUserName })
            .IsUnique();
        modelBuilder.Entity<Role>()
            .HasIndex(role => role.NormalizedName)
            .IsUnique();
        modelBuilder.Entity<UserRole>()
            .HasIndex(userRole => new { userRole.OrganizationId, userRole.UserId, userRole.RoleId })
            .IsUnique();

        modelBuilder.Entity<AppIdentityUser>()
            .HasIndex(user => user.NormalizedUserName)
            .IsUnique(false);
        modelBuilder.Entity<AppIdentityUser>()
            .HasIndex(user => new { user.OrganizationId, user.NormalizedUserName })
            .IsUnique();
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
            filter = Expression.Equal(Expression.Convert(entityOrganizationId, typeof(Guid?)), currentOrganizationId);
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
