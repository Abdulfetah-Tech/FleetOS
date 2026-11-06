using EthioFleet.Application.Fuel;
using EthioFleet.Domain.Fuel;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class FuelRecordService(AppDbContext dbContext) : IFuelRecordService
{
    public async Task<FuelRecordResult> CreateAsync(CreateFuelRecordRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == request.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return FuelRecordResult.Failure("Vehicle was not found.", notFound: true);
        }

        var driver = request.DriverId is Guid driverId
            ? await dbContext.Drivers.SingleOrDefaultAsync(item => item.Id == driverId, cancellationToken)
            : null;
        if (request.DriverId.HasValue && driver is null)
        {
            return FuelRecordResult.Failure("Driver was not found.", notFound: true);
        }

        var trip = request.TripId is Guid tripId
            ? await dbContext.Trips.SingleOrDefaultAsync(item => item.Id == tripId, cancellationToken)
            : null;
        if (request.TripId.HasValue && trip is null)
        {
            return FuelRecordResult.Failure("Trip was not found.", notFound: true);
        }

        var record = new FuelRecord(vehicle.OrganizationId, vehicle, driver, trip, request.OccurredAt,
            request.QuantityLiters, request.UnitPrice, request.Odometer, request.Station,
            request.CurrencyCode, request.ReceiptReference, request.Notes);
        dbContext.FuelRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return FuelRecordResult.Success(ToResponse(record));
    }

    public async Task<FuelRecordResult> GetAsync(Guid fuelRecordId, CancellationToken cancellationToken)
    {
        var record = await QueryRecords().SingleOrDefaultAsync(item => item.Id == fuelRecordId, cancellationToken);
        return record is null
            ? FuelRecordResult.Failure("Fuel record was not found.", notFound: true)
            : FuelRecordResult.Success(ToResponse(record));
    }

    public async Task<FuelRecordPage> ListAsync(FuelRecordListQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        IQueryable<FuelRecord> records = QueryRecords();
        if (query.VehicleId is Guid vehicleId)
        {
            records = records.Where(item => item.VehicleId == vehicleId);
        }
        if (query.DriverId is Guid driverId)
        {
            records = records.Where(item => item.DriverId == driverId);
        }

        var count = await records.CountAsync(cancellationToken);
        var items = await records.OrderByDescending(item => item.OccurredAt)
            .ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new FuelRecordPage(items.Select(ToResponse).ToArray(), page, pageSize, count);
    }

    public async Task<bool> DeleteAsync(Guid fuelRecordId, CancellationToken cancellationToken)
    {
        var record = await dbContext.FuelRecords.SingleOrDefaultAsync(item => item.Id == fuelRecordId, cancellationToken);
        if (record is null)
        {
            return false;
        }

        dbContext.FuelRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<FuelRecord> QueryRecords() => dbContext.FuelRecords.AsNoTracking()
        .Include(record => record.Vehicle)
        .Include(record => record.Driver);

    private static FuelRecordResponse ToResponse(FuelRecord record) => new(
        record.Id,
        record.OrganizationId,
        record.VehicleId,
        record.Vehicle.PlateNumber,
        record.DriverId,
        record.Driver?.FullName,
        record.TripId,
        record.OccurredAt,
        record.QuantityLiters,
        record.UnitPrice,
        record.TotalCost,
        record.Odometer,
        string.IsNullOrEmpty(record.Station) ? null : record.Station,
        record.CurrencyCode,
        string.IsNullOrEmpty(record.ReceiptReference) ? null : record.ReceiptReference,
        string.IsNullOrEmpty(record.Notes) ? null : record.Notes);
}
