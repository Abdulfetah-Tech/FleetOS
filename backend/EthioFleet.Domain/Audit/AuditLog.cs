using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Audit;

/// <summary>
/// Immutable audit trail entry for tenant-scoped domain mutations.
/// </summary>
public sealed class AuditLog : ITenantOwned
{
    private AuditLog()
    {
    }

    public AuditLog(
        Guid organizationId,
        Guid? userId,
        string action,
        string entityType,
        string entityId,
        DateTimeOffset occurredAt,
        bool succeeded = true,
        string? ipAddress = null,
        string? metadataJson = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainException("OrganizationId is required for audit log entries.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainException("Audit action is required.");
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new DomainException("Audit entityType is required.");
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new DomainException("Audit entityId is required.");
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        UserId = userId;
        Action = action.Trim();
        EntityType = entityType.Trim();
        EntityId = entityId.Trim();
        OccurredAt = occurredAt;
        Succeeded = succeeded;
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public bool Succeeded { get; private set; }
    public string? IpAddress { get; private set; }
    public string? MetadataJson { get; private set; }
}
