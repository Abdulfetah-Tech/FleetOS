using EthioFleet.Domain.Notifications;

namespace EthioFleet.Application.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    Guid OrganizationId,
    NotificationType Type,
    NotificationSeverity Severity,
    string Title,
    string Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    bool IsRead);

public sealed record NotificationPage(IReadOnlyCollection<NotificationResponse> Items, int Page, int PageSize, int TotalCount, int UnreadCount);

public interface INotificationService
{
    Task<NotificationPage> ListAsync(bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken);
    Task<bool> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken);
}

public interface IFleetReminderProcessor
{
    Task<int> GenerateDueRemindersAsync(DateOnly asOfDate, CancellationToken cancellationToken);
}
