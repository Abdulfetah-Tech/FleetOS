using EthioFleet.Application.Notifications;
using EthioFleet.Domain.Documents;
using EthioFleet.Domain.Maintenance;
using EthioFleet.Domain.Notifications;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class FleetReminderProcessor(AppDbContext dbContext) : IFleetReminderProcessor
{
    private const int ReminderLeadDays = 30;

    public async Task<int> GenerateDueRemindersAsync(DateOnly asOfDate, CancellationToken cancellationToken)
    {
        var expiryCutoff = asOfDate.AddDays(ReminderLeadDays);
        var expiringDocuments = await dbContext.VehicleDocuments.IgnoreQueryFilters().AsNoTracking()
            .Where(document => !document.IsDeleted && document.Status != VehicleDocumentStatus.Archived &&
                document.ExpiresOn != null && document.ExpiresOn <= expiryCutoff)
            .Select(document => new { document.OrganizationId, document.Id, document.Name, document.VehicleId, document.ExpiresOn })
            .ToListAsync(cancellationToken);

        var dueSchedules = await dbContext.MaintenanceSchedules.IgnoreQueryFilters().AsNoTracking()
            .Where(schedule => !schedule.IsDeleted && schedule.Status == MaintenanceScheduleStatus.Active &&
                ((schedule.NextDueDate != null && schedule.NextDueDate <= asOfDate) ||
                 (schedule.NextDueMileage != null && schedule.NextDueMileage <= schedule.Vehicle.CurrentMileage)))
            .Select(schedule => new
            {
                schedule.OrganizationId,
                schedule.Id,
                schedule.Name,
                schedule.NextDueDate,
                schedule.NextDueMileage,
                VehiclePlate = schedule.Vehicle.PlateNumber,
                VehicleMileage = schedule.Vehicle.CurrentMileage
            })
            .ToListAsync(cancellationToken);

        var created = 0;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var document in expiringDocuments)
        {
            var daysUntilExpiry = document.ExpiresOn!.Value.DayNumber - asOfDate.DayNumber;
            var idempotencyKey = $"document:{document.OrganizationId:N}:{document.Id:N}:{document.ExpiresOn:yyyyMMdd}";
            var title = daysUntilExpiry < 0 ? "Vehicle document expired" : "Vehicle document expiry approaching";
            var message = $"{document.Name} for vehicle {document.VehicleId:N} " +
                (daysUntilExpiry < 0 ? $"expired {-daysUntilExpiry} day(s) ago." : $"expires in {daysUntilExpiry} day(s).");
            created += await InsertIfMissingAsync(document.OrganizationId, NotificationType.VehicleDocumentExpiry,
                daysUntilExpiry <= 7 ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                title, message, idempotencyKey, cancellationToken);
        }

        foreach (var schedule in dueSchedules)
        {
            var idempotencyKey = $"maintenance:{schedule.OrganizationId:N}:{schedule.Id:N}:{asOfDate:yyyyMMdd}";
            var message = $"{schedule.Name} is due for vehicle {schedule.VehiclePlate} " +
                $"(current mileage {schedule.VehicleMileage:0.0} km).";
            created += await InsertIfMissingAsync(schedule.OrganizationId, NotificationType.MaintenanceDue,
                NotificationSeverity.Warning, "Maintenance due", message, idempotencyKey, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return created;
    }

    private Task<int> InsertIfMissingAsync(
        Guid organizationId,
        NotificationType type,
        NotificationSeverity severity,
        string title,
        string message,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO notifications
                (id, organization_id, type, severity, title, message, idempotency_key, read_at,
                 created_at, updated_at, created_by, updated_by)
            VALUES
                ({id}, {organizationId}, {(int)type}, {(int)severity}, {title}, {message},
                 {idempotencyKey}, NULL, {createdAt}, {createdAt}, NULL, NULL)
            ON CONFLICT (organization_id, idempotency_key) DO NOTHING
            """, cancellationToken);
    }
}
