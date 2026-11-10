using EthioFleet.Application.Reports;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class FleetReportService(AppDbContext dbContext) : IFleetReportService
{
    public async Task<FleetReportResponse> GetAsync(FleetReportRequest request, CancellationToken cancellationToken)
    {
        if (request.From == default || request.To < request.From || request.To.DayNumber - request.From.DayNumber > 366)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Report date range must be valid and no longer than 367 days.");
        }

        var from = request.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var until = request.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fuel = await dbContext.FuelRecords.AsNoTracking()
            .Where(record => record.OccurredAt >= from && record.OccurredAt < until)
            .GroupBy(record => new { record.VehicleId, record.Vehicle.PlateNumber, record.CurrencyCode })
            .Select(group => new
            {
                group.Key.VehicleId,
                group.Key.PlateNumber,
                group.Key.CurrencyCode,
                Cost = group.Sum(record => record.TotalCost),
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);
        var maintenance = await dbContext.MaintenanceRecords.AsNoTracking()
            .Where(record => record.PerformedOn >= request.From && record.PerformedOn <= request.To)
            .GroupBy(record => new { record.VehicleId, record.Vehicle.PlateNumber, record.CurrencyCode })
            .Select(group => new
            {
                group.Key.VehicleId,
                group.Key.PlateNumber,
                group.Key.CurrencyCode,
                Cost = group.Sum(record => record.TotalCost),
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);
        var trips = await dbContext.Trips.AsNoTracking()
            .Where(trip => trip.Status == EthioFleet.Domain.Trips.TripStatus.Completed &&
                trip.ActualStart >= from && trip.ActualStart < until)
            .GroupBy(trip => new { trip.VehicleId, trip.Vehicle.PlateNumber })
            .Select(group => new
            {
                group.Key.VehicleId,
                group.Key.PlateNumber,
                CompletedTrips = group.Count(),
                DistanceKm = group.Sum(trip => trip.EndMileage!.Value - trip.StartMileage)
            })
            .ToListAsync(cancellationToken);

        var costs = fuel.Select(item => (item.VehicleId, item.PlateNumber, item.CurrencyCode, Fuel: item.Cost, Maintenance: 0m, FuelCount: item.Count, MaintenanceCount: 0))
            .Concat(maintenance.Select(item => (item.VehicleId, item.PlateNumber, item.CurrencyCode, Fuel: 0m, Maintenance: item.Cost, FuelCount: 0, MaintenanceCount: item.Count)))
            .GroupBy(item => new { item.VehicleId, item.PlateNumber, item.CurrencyCode })
            .Select(group => new VehicleCostRow(group.Key.VehicleId, group.Key.PlateNumber, group.Key.CurrencyCode,
                group.Sum(item => item.Fuel), group.Sum(item => item.Maintenance),
                group.Sum(item => item.FuelCount), group.Sum(item => item.MaintenanceCount)))
            .OrderBy(item => item.PlateNumber)
            .ToArray();
        var byCurrency = costs.GroupBy(item => item.CurrencyCode)
            .Select(group => new CurrencyCostSummary(group.Key, group.Sum(item => item.FuelCost),
                group.Sum(item => item.MaintenanceCost), group.Sum(item => item.FuelCost + item.MaintenanceCost)))
            .OrderBy(item => item.CurrencyCode)
            .ToArray();
        var utilization = trips.OrderBy(item => item.PlateNumber)
            .Select(item => new VehicleUtilizationRow(item.VehicleId, item.PlateNumber, item.CompletedTrips, item.DistanceKm))
            .ToArray();

        return new FleetReportResponse(request.From, request.To, byCurrency, costs, utilization);
    }
}
