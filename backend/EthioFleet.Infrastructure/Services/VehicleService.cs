using EthioFleet.Application.Common;
using EthioFleet.Application.Vehicles;
using EthioFleet.Domain.Common;
using EthioFleet.Domain.Vehicles;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class VehicleService(AppDbContext dbContext) : IVehicleService
{
    public async Task<VehicleResult> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle(request.PlateNumber, request.Make, request.Model, request.Year,
            request.Type, request.FuelType, request.CurrentMileage, request.Vin, request.CurrencyCode, request.PurchaseDate);
        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return VehicleResult.Success(ToResponse(vehicle));
    }

    public async Task<VehicleResult> GetAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == vehicleId, cancellationToken);
        return vehicle is null
            ? VehicleResult.Failure("Vehicle was not found.", notFound: true)
            : VehicleResult.Success(ToResponse(vehicle));
    }

    public async Task<VehiclePage> ListAsync(VehicleListQuery query, CancellationToken cancellationToken)
    {
        var window = PageWindow.Create(query.Page, query.PageSize);
        var vehicles = dbContext.Vehicles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var normalizedSearch = Vehicle.NormalizeIdentifier(search);
            vehicles = vehicles.Where(vehicle =>
                EF.Functions.ILike(vehicle.PlateNumber, $"%{EscapeLike(search)}%") ||
                vehicle.NormalizedPlateNumber.Contains(normalizedSearch) ||
                EF.Functions.ILike(vehicle.Make, $"%{EscapeLike(search)}%") ||
                EF.Functions.ILike(vehicle.Model, $"%{EscapeLike(search)}%"));
        }

        var totalCount = await vehicles.CountAsync(cancellationToken);
        var items = await vehicles.OrderBy(vehicle => vehicle.NormalizedPlateNumber)
            .ThenBy(vehicle => vehicle.Id)
            .Skip(window.Offset)
            .Take(window.PageSize)
            .Select(vehicle => new VehicleResponse(
                vehicle.Id,
                vehicle.OrganizationId,
                vehicle.PlateNumber,
                vehicle.Make,
                vehicle.Model,
                vehicle.Year,
                vehicle.Type,
                vehicle.FuelType,
                vehicle.CurrentMileage,
                vehicle.Vin,
                vehicle.Status,
                vehicle.PurchaseDate,
                vehicle.CurrencyCode,
                vehicle.CreatedAt,
                vehicle.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new VehiclePage(items, window.Page, window.PageSize, totalCount);
    }

    public async Task<VehicleResult> UpdateAsync(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == vehicleId, cancellationToken);
        if (vehicle is null)
        {
            return VehicleResult.Failure("Vehicle was not found.", notFound: true);
        }

        vehicle.UpdateDetails(request.PlateNumber, request.Make, request.Model, request.Year,
            request.Type, request.FuelType, request.Vin, request.PurchaseDate);
        await dbContext.SaveChangesAsync(cancellationToken);
        return VehicleResult.Success(ToResponse(vehicle));
    }

    public async Task<VehicleResult> ChangeStatusAsync(Guid vehicleId, VehicleStatusRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == vehicleId, cancellationToken);
        if (vehicle is null)
        {
            return VehicleResult.Failure("Vehicle was not found.", notFound: true);
        }

        vehicle.ChangeStatus(request.Status);
        await dbContext.SaveChangesAsync(cancellationToken);
        return VehicleResult.Success(ToResponse(vehicle));
    }

    public async Task<bool> DeleteAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == vehicleId, cancellationToken);
        if (vehicle is null)
        {
            return false;
        }

        dbContext.Vehicles.Remove(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static VehicleResponse ToResponse(Vehicle vehicle) => new(
        vehicle.Id,
        vehicle.OrganizationId,
        vehicle.PlateNumber,
        vehicle.Make,
        vehicle.Model,
        vehicle.Year,
        vehicle.Type,
        vehicle.FuelType,
        vehicle.CurrentMileage,
        vehicle.Vin,
        vehicle.Status,
        vehicle.PurchaseDate,
        vehicle.CurrencyCode,
        vehicle.CreatedAt,
        vehicle.UpdatedAt);

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
