using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Notifications;

public sealed class Notification : BaseEntity, ITenantOwned
{
    private Notification()
    {
        Title = string.Empty;
        Message = string.Empty;
        IdempotencyKey = string.Empty;
    }

    public Notification(
        Guid organizationId,
        NotificationType type,
        NotificationSeverity severity,
        string title,
        string message,
        string idempotencyKey,
        DateTimeOffset createdAt)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainException("Notification organization is required.");
        }
        if (!Enum.IsDefined(type) || !Enum.IsDefined(severity))
        {
            throw new DomainException("Notification type or severity is invalid.");
        }

        OrganizationId = organizationId;
        Type = type;
        Severity = severity;
        Title = NormalizeRequired(title, nameof(title), 160);
        Message = NormalizeRequired(message, nameof(message), 1000);
        IdempotencyKey = NormalizeRequired(idempotencyKey, nameof(idempotencyKey), 200);
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = CreatedAt;
    }

    public Guid OrganizationId { get; set; }
    public NotificationType Type { get; private set; }
    public NotificationSeverity Severity { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string IdempotencyKey { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public bool IsRead => ReadAt.HasValue;

    public void MarkRead(DateTimeOffset readAt)
    {
        if (IsRead)
        {
            return;
        }
        ReadAt = readAt.ToUniversalTime();
    }

    private static string NormalizeRequired(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} is required.");
        }
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        }
        return normalized;
    }
}
