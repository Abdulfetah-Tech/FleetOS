namespace EthioFleet.Application.Audit;

public sealed record AuditLogResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? UserId,
    string Action,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    bool Succeeded,
    string? IpAddress,
    string? MetadataJson);

public sealed record AuditLogPage(
    IReadOnlyList<AuditLogResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public interface IAuditLogService
{
    Task<AuditLogPage> ListAsync(
        string? entityType = null,
        string? action = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
