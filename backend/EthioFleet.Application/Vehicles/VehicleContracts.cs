using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Application.Vehicles;

public sealed record CreateVehicleRequest(
    string PlateNumber,
    string Make,
    string Model,
    int Year,
    VehicleType Type,
    FuelType FuelType,
    decimal CurrentMileage,
    string? Vin,
    string CurrencyCode = "ETB",
    DateOnly? PurchaseDate = null);

public sealed record UpdateVehicleRequest(
    string PlateNumber,
    string Make,
    string Model,
    int Year,
    VehicleType Type,
    FuelType FuelType,
    string? Vin,
    DateOnly? PurchaseDate);

public sealed record VehicleResponse(
    Guid Id,
    Guid OrganizationId,
    string PlateNumber,
    string Make,
    string Model,
    int Year,
    VehicleType Type,
    FuelType FuelType,
    decimal CurrentMileage,
    string? Vin,
    VehicleStatus Status,
    DateOnly? PurchaseDate,
    string CurrencyCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record VehiclePage(IReadOnlyCollection<VehicleResponse> Items, int Page, int PageSize, int TotalCount);

public sealed record VehicleResult(bool Succeeded, VehicleResponse? Vehicle, string? Error, bool NotFound = false)
{
    public static VehicleResult Success(VehicleResponse vehicle) => new(true, vehicle, null);
    public static VehicleResult Failure(string error, bool notFound = false) => new(false, null, error, notFound);
}

public sealed record VehicleListQuery(int Page = 1, int PageSize = 20, string? Search = null);
public sealed record VehicleStatusRequest(VehicleStatus Status);

public interface IVehicleService
{
    Task<VehicleResult> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken);
    Task<VehicleResult> GetAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task<VehiclePage> ListAsync(VehicleListQuery query, CancellationToken cancellationToken);
    Task<VehicleResult> UpdateAsync(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken);
    Task<VehicleResult> ChangeStatusAsync(Guid vehicleId, VehicleStatusRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid vehicleId, CancellationToken cancellationToken);
}
