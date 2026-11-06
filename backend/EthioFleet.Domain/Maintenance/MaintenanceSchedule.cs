using EthioFleet.Domain.Common;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Domain.Maintenance;

public sealed class MaintenanceSchedule : BaseEntity, ITenantOwned, ISoftDeletable
{
    private MaintenanceSchedule()
    {
        Vehicle = null!;
        Name = string.Empty;
        Description = string.Empty;
    }

    public MaintenanceSchedule(
        Guid organizationId,
        Vehicle vehicle,
        string name,
        string? description,
        DateOnly? nextDueDate,
        decimal? nextDueMileage,
        int? intervalDays,
        decimal? intervalMileage)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        if (organizationId == Guid.Empty || vehicle.OrganizationId != organizationId)
        {
            throw new DomainException("Maintenance schedule vehicle must belong to its organization.");
        }

        Name = NormalizeRequired(name, nameof(name), 160);
        Description = NormalizeOptional(description, nameof(description), 500);
        ValidateDueAndIntervals(nextDueDate, nextDueMileage, intervalDays, intervalMileage);
        OrganizationId = organizationId;
        VehicleId = vehicle.Id;
        Vehicle = vehicle;
        NextDueDate = nextDueDate;
        NextDueMileage = nextDueMileage;
        IntervalDays = intervalDays;
        IntervalMileage = intervalMileage;
        Status = MaintenanceScheduleStatus.Active;
    }

    public Guid OrganizationId { get; set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateOnly? NextDueDate { get; private set; }
    public decimal? NextDueMileage { get; private set; }
    public int? IntervalDays { get; private set; }
    public decimal? IntervalMileage { get; private set; }
    public MaintenanceScheduleStatus Status { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public bool IsDue(DateOnly asOfDate, decimal currentMileage)
    {
        if (Status != MaintenanceScheduleStatus.Active)
        {
            return false;
        }

        return (NextDueDate is DateOnly dueDate && asOfDate >= dueDate) ||
            (NextDueMileage is decimal dueMileage && currentMileage >= dueMileage);
    }

    public void Complete(DateOnly completedOn, decimal completedAtMileage)
    {
        if (Status != MaintenanceScheduleStatus.Active)
        {
            throw new DomainException("Only active maintenance schedules can be completed.");
        }
        if (completedOn == default || completedAtMileage < 0 || decimal.Round(completedAtMileage, 1) != completedAtMileage)
        {
            throw new DomainException("Maintenance completion date and mileage are invalid.");
        }

        NextDueDate = IntervalDays is int days ? completedOn.AddDays(days) : null;
        NextDueMileage = IntervalMileage is decimal miles ? completedAtMileage + miles : null;
        if (NextDueDate is null && NextDueMileage is null)
        {
            Status = MaintenanceScheduleStatus.Completed;
        }
    }

    public void ChangeStatus(MaintenanceScheduleStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Maintenance schedule status is invalid.");
        }
        if (Status == MaintenanceScheduleStatus.Completed && status != MaintenanceScheduleStatus.Completed)
        {
            throw new DomainException("A completed maintenance schedule cannot be reactivated.");
        }

        Status = status;
    }

    private static void ValidateDueAndIntervals(DateOnly? dueDate, decimal? dueMileage, int? days, decimal? miles)
    {
        if (dueDate is null && dueMileage is null)
        {
            throw new DomainException("A maintenance schedule requires a due date, due mileage, or both.");
        }
        if (dueDate == default || days is <= 0 || miles is <= 0 ||
            (dueMileage is decimal due && (due < 0 || decimal.Round(due, 1) != due)) ||
            (miles is decimal interval && decimal.Round(interval, 1) != interval))
        {
            throw new DomainException("Maintenance due values and intervals are invalid.");
        }
    }

    private static string NormalizeRequired(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} is required.");
        }
        var result = value.Trim();
        if (result.Length > maxLength)
        {
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        }
        return result;
    }

    private static string NormalizeOptional(string? value, string name, int maxLength)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length > maxLength)
        {
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        }
        return result;
    }
}
