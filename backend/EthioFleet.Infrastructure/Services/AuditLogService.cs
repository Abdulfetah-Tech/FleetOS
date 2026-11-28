using EthioFleet.Application.Audit;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class AuditLogService(AppDbContext dbContext) : IAuditLogService
{
    public async Task<AuditLogPage> ListAsync(
        string? entityType = null,
        string? action = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var trimmedEntityType = entityType.Trim();
            query = query.Where(log => log.EntityType == trimmedEntityType);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var trimmedAction = action.Trim();
            query = query.Where(log => log.Action == trimmedAction);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(log => log.OccurredAt)
            .ThenByDescending(log => log.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(log => new AuditLogResponse(
                log.Id,
                log.OrganizationId,
                log.UserId,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.OccurredAt,
                log.Succeeded,
                log.IpAddress,
                log.MetadataJson))
            .ToListAsync(cancellationToken);

        return new AuditLogPage(items, normalizedPage, normalizedPageSize, totalCount);
    }
}
