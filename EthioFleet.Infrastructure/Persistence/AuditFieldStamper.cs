using EthioFleet.Application.Interfaces;
using EthioFleet.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EthioFleet.Infrastructure.Persistence;

internal static class AuditFieldStamper
{
    public static void Apply(
        ChangeTracker changeTracker,
        IClock clock,
        ICurrentUser currentUser,
        ITenantContext tenantContext)
    {
        var now = clock.UtcNow.ToUniversalTime();
        var userId = currentUser.UserId;

        foreach (var entry in changeTracker.Entries())
        {
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
                softDeletable.DeletedBy = userId;
            }

            if (entry.Entity is ITenantOwned tenantOwned)
            {
                if (entry.State == EntityState.Added)
                {
                    tenantOwned.OrganizationId = tenantContext.OrganizationId
                        ?? throw new UnauthorizedAccessException("An organization claim is required to create tenant-owned data.");
                }
                else if (tenantContext.OrganizationId is not Guid organizationId
                    || tenantOwned.OrganizationId != organizationId)
                {
                    throw new UnauthorizedAccessException("Tenant-owned data cannot be modified outside its organization.");
                }
            }

            if (entry.Entity is not IAuditable auditable)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAt = now;
                auditable.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                auditable.UpdatedAt = now;
                auditable.UpdatedBy = userId;
            }
        }
    }
}
