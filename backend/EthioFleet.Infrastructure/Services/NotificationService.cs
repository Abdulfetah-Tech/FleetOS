using EthioFleet.Application.Common;
using EthioFleet.Application.Notifications;
using EthioFleet.Domain.Notifications;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class NotificationService(AppDbContext dbContext, EthioFleet.Application.Interfaces.IClock clock) : INotificationService
{
    public async Task<NotificationPage> ListAsync(bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var window = PageWindow.Create(page, pageSize);
        var query = dbContext.Notifications.AsNoTracking();
        var unreadCount = await query.CountAsync(notification => notification.ReadAt == null, cancellationToken);
        if (unreadOnly)
        {
            query = query.Where(notification => notification.ReadAt == null);
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(notification => notification.CreatedAt)
            .ThenBy(notification => notification.Id)
            .Skip(window.Offset)
            .Take(window.PageSize)
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.OrganizationId,
                notification.Type,
                notification.Severity,
                notification.Title,
                notification.Message,
                notification.CreatedAt,
                notification.ReadAt,
                notification.ReadAt != null))
            .ToListAsync(cancellationToken);
        return new NotificationPage(items, window.Page, window.PageSize, total, unreadCount);
    }

    public async Task<bool> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(item => item.Id == notificationId, cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.MarkRead(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
