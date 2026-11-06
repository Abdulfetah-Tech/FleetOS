using EthioFleet.Domain.Maintenance;

namespace EthioFleet.Application.Maintenance;

public sealed record CreateMaintenanceRecordRequest(
    Guid VehicleId,
    MaintenanceType Type,
    string Description,
    DateOnly PerformedOn,
    decimal Odometer,
    decimal LaborCost,
    decimal PartsCost,
    string CurrencyCode = "ETB",
    string? ServiceProvider = null,
    string? Reference = null,
    string? Notes = null);

public sealed record MaintenanceRecordResponse(
    Guid Id,
    Guid OrganizationId,
    Guid VehicleId,
    string VehiclePlateNumber,
    MaintenanceType Type,
    string Description,
    DateOnly PerformedOn,
    decimal Odometer,
    decimal LaborCost,
    decimal PartsCost,
    decimal TotalCost,
    string CurrencyCode,
    string? ServiceProvider,
    string? Reference,
    string? Notes);

public sealed record CreateMaintenanceScheduleRequest(
    Guid VehicleId,
    string Name,
    string? Description,
    DateOnly? NextDueDate,
    decimal? NextDueMileage,
    int? IntervalDays,
    decimal? IntervalMileage);

public sealed record CompleteMaintenanceScheduleRequest(DateOnly CompletedOn, decimal CompletedAtMileage);

public sealed record MaintenanceScheduleResponse(
    Guid Id,
    Guid OrganizationId,
    Guid VehicleId,
    string VehiclePlateNumber,
    string Name,
    string? Description,
    DateOnly? NextDueDate,
    decimal? NextDueMileage,
    int? IntervalDays,
    decimal? IntervalMileage,
    MaintenanceScheduleStatus Status,
    bool IsDue);

public sealed record MaintenanceRecordPage(IReadOnlyCollection<MaintenanceRecordResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record MaintenanceSchedulePage(IReadOnlyCollection<MaintenanceScheduleResponse> Items, int Page, int PageSize, int TotalCount);

public sealed record MaintenanceResult<T>(bool Succeeded, T? Item, string? Error, bool NotFound = false)
{
    public static MaintenanceResult<T> Success(T item) => new(true, item, null);
    public static MaintenanceResult<T> Failure(string error, bool notFound = false) => new(false, default, error, notFound);
}

public interface IMaintenanceService
{
    Task<MaintenanceRecordResponse> CreateRecordAsync(CreateMaintenanceRecordRequest request, CancellationToken cancellationToken);
    Task<MaintenanceRecordPage> ListRecordsAsync(Guid? vehicleId, int page, int pageSize, CancellationToken cancellationToken);
    Task<MaintenanceScheduleResponse> CreateScheduleAsync(CreateMaintenanceScheduleRequest request, CancellationToken cancellationToken);
    Task<MaintenanceSchedulePage> ListSchedulesAsync(Guid? vehicleId, bool dueOnly, int page, int pageSize, CancellationToken cancellationToken);
    Task<MaintenanceResult<MaintenanceScheduleResponse>> CompleteScheduleAsync(Guid scheduleId, CompleteMaintenanceScheduleRequest request, CancellationToken cancellationToken);
}
