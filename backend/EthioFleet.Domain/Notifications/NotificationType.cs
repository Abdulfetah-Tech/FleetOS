namespace EthioFleet.Domain.Notifications;

public enum NotificationType
{
    VehicleDocumentExpiry = 1,
    MaintenanceDue = 2
}

public enum NotificationSeverity
{
    Information = 1,
    Warning = 2,
    Critical = 3
}
