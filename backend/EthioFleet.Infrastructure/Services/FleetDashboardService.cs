using EthioFleet.Application.Dashboard;
using EthioFleet.Domain.Incidents;
using EthioFleet.Domain.Drivers;
using EthioFleet.Domain.Maintenance;
using EthioFleet.Domain.Trips;
using EthioFleet.Domain.Vehicles;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class FleetDashboardService(AppDbContext dbContext, EthioFleet.Application.Interfaces.IClock clock)
    : IFleetDashboardService
{
    private const int UpcomingWindowDays = 30;

    public async Task<FleetDashboardResponse> GetAsync(FleetDashboardRequest request, CancellationToken cancellationToken)
    {
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var month = request.Month is DateOnly requestedMonth
            ? new DateOnly(requestedMonth.Year, requestedMonth.Month, 1)
            : new DateOnly(asOf.Year, asOf.Month, 1);
        var monthEnd = month.AddMonths(1);
        var totalVehicles = await dbContext.Vehicles.CountAsync(cancellationToken);
        var activeVehicles = await dbContext.Vehicles.CountAsync(vehicle => vehicle.Status == VehicleStatus.Active, cancellationToken);
        var vehiclesInMaintenance = await dbContext.Vehicles.CountAsync(vehicle => vehicle.Status == VehicleStatus.Maintenance, cancellationToken);
        var totalDrivers = await dbContext.Drivers.CountAsync(cancellationToken);
        var activeDrivers = await dbContext.Drivers.CountAsync(driver => driver.Status == DriverStatus.Active, cancellationToken);
        var tripsToday = await dbContext.Trips.CountAsync(trip =>
            trip.PlannedStart >= asOf.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) &&
            trip.PlannedStart < asOf.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), cancellationToken);

        var fuelCosts = await dbContext.FuelRecords.AsNoTracking()
            .Where(record => record.OccurredAt >= month.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) &&
                record.OccurredAt < monthEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .GroupBy(record => record.CurrencyCode)
            .Select(group => new { CurrencyCode = group.Key, Cost = group.Sum(record => record.TotalCost) })
            .ToListAsync(cancellationToken);
        var maintenanceCosts = await dbContext.MaintenanceRecords.AsNoTracking()
            .Where(record => record.PerformedOn >= month && record.PerformedOn < monthEnd)
            .GroupBy(record => record.CurrencyCode)
            .Select(group => new { CurrencyCode = group.Key, Cost = group.Sum(record => record.TotalCost) })
            .ToListAsync(cancellationToken);
        var costs = fuelCosts.Select(item => (item.CurrencyCode, Fuel: item.Cost, Maintenance: 0m))
            .Concat(maintenanceCosts.Select(item => (item.CurrencyCode, Fuel: 0m, Maintenance: item.Cost)))
            .GroupBy(item => item.CurrencyCode)
            .Select(group => new DashboardCurrencyTotal(group.Key, group.Sum(item => item.Fuel),
                group.Sum(item => item.Maintenance), group.Sum(item => item.Fuel + item.Maintenance)))
            .OrderBy(item => item.CurrencyCode)
            .ToArray();

        var expiryEnd = asOf.AddDays(UpcomingWindowDays);
        var documents = await dbContext.VehicleDocuments.AsNoTracking()
            .Where(document => document.Status != EthioFleet.Domain.Documents.VehicleDocumentStatus.Archived &&
                document.ExpiresOn != null && document.ExpiresOn <= expiryEnd)
            .OrderBy(document => document.ExpiresOn)
            .Take(100)
            .Select(document => new UpcomingDocumentExpiry(document.Id, document.VehicleId,
                document.Vehicle.PlateNumber, document.Name, document.ExpiresOn!.Value,
                document.ExpiresOn.Value.DayNumber - asOf.DayNumber))
            .ToListAsync(cancellationToken);

        var dueSchedules = await dbContext.MaintenanceSchedules.AsNoTracking()
            .Where(schedule => schedule.Status == MaintenanceScheduleStatus.Active &&
                ((schedule.NextDueDate != null && schedule.NextDueDate <= asOf) ||
                 (schedule.NextDueMileage != null && schedule.NextDueMileage <= schedule.Vehicle.CurrentMileage)))
            .OrderBy(schedule => schedule.NextDueDate)
            .ThenBy(schedule => schedule.NextDueMileage)
            .Take(100)
            .Select(schedule => new DueMaintenanceSchedule(schedule.Id, schedule.VehicleId,
                schedule.Vehicle.PlateNumber, schedule.Name, schedule.NextDueDate, schedule.NextDueMileage,
                schedule.Vehicle.CurrentMileage))
            .ToListAsync(cancellationToken);

        var activeIncidents = await dbContext.Incidents.CountAsync(incident =>
            incident.Status == IncidentStatus.Open || incident.Status == IncidentStatus.UnderInvestigation,
            cancellationToken);

        return new FleetDashboardResponse(asOf, month, totalVehicles, activeVehicles, vehiclesInMaintenance,
            Math.Max(0, activeVehicles - vehiclesInMaintenance), totalDrivers, activeDrivers, tripsToday,
            costs, documents, dueSchedules, activeIncidents);
    }
}
