namespace EthioFleet.Application.Fuel;

public sealed record CreateFuelRecordRequest(
    Guid VehicleId,
    Guid? DriverId,
    Guid? TripId,
    DateTimeOffset OccurredAt,
    decimal QuantityLiters,
    decimal UnitPrice,
    decimal Odometer,
    string? Station = null,
    string CurrencyCode = "ETB",
    string? ReceiptReference = null,
    string? Notes = null);

public sealed record FuelRecordResponse(
    Guid Id,
    Guid OrganizationId,
    Guid VehicleId,
    string VehiclePlateNumber,
    Guid? DriverId,
    string? DriverName,
    Guid? TripId,
    DateTimeOffset OccurredAt,
    decimal QuantityLiters,
    decimal UnitPrice,
    decimal TotalCost,
    decimal Odometer,
    string? Station,
    string CurrencyCode,
    string? ReceiptReference,
    string? Notes);

public sealed record FuelRecordResult(bool Succeeded, FuelRecordResponse? FuelRecord, string? Error, bool NotFound = false)
{
    public static FuelRecordResult Success(FuelRecordResponse record) => new(true, record, null);
    public static FuelRecordResult Failure(string error, bool notFound = false) => new(false, null, error, notFound);
}

public sealed record FuelRecordPage(IReadOnlyCollection<FuelRecordResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record FuelRecordListQuery(int Page = 1, int PageSize = 20, Guid? VehicleId = null, Guid? DriverId = null);

public interface IFuelRecordService
{
    Task<FuelRecordResult> CreateAsync(CreateFuelRecordRequest request, CancellationToken cancellationToken);
    Task<FuelRecordResult> GetAsync(Guid fuelRecordId, CancellationToken cancellationToken);
    Task<FuelRecordPage> ListAsync(FuelRecordListQuery query, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid fuelRecordId, CancellationToken cancellationToken);
}
