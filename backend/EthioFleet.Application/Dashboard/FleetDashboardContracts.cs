namespace EthioFleet.Application.Dashboard;

public sealed record FleetDashboardRequest(DateOnly? AsOfDate = null, DateOnly? Month = null);
public sealed record DashboardCurrencyTotal(string CurrencyCode, decimal FuelCost, decimal MaintenanceCost, decimal OperatingCost);
public sealed record UpcomingDocumentExpiry(Guid DocumentId, Guid VehicleId, string PlateNumber, string Name, DateOnly ExpiresOn, int DaysUntilExpiry);
public sealed record DueMaintenanceSchedule(Guid ScheduleId, Guid VehicleId, string PlateNumber, string Name, DateOnly? DueDate, decimal? DueMileage, decimal CurrentMileage);
public sealed record FleetDashboardResponse(
    DateOnly AsOfDate,
    DateOnly Month,
    int TotalVehicles,
    int ActiveVehicles,
    int VehiclesInMaintenance,
    int AvailableVehicles,
    int TotalDrivers,
    int ActiveDrivers,
    int TripsToday,
    IReadOnlyCollection<DashboardCurrencyTotal> MonthlyCosts,
    IReadOnlyCollection<UpcomingDocumentExpiry> UpcomingDocumentExpiries,
    IReadOnlyCollection<DueMaintenanceSchedule> DueMaintenance,
    int OpenIncidents);

public interface IFleetDashboardService
{
    Task<FleetDashboardResponse> GetAsync(FleetDashboardRequest request, CancellationToken cancellationToken);
}
