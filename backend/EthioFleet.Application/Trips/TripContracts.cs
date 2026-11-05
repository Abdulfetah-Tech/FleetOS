using EthioFleet.Domain.Trips;

namespace EthioFleet.Application.Trips;

public sealed record CreateTripRequest(
    Guid VehicleId,
    Guid DriverId,
    string Origin,
    string Destination,
    string Purpose,
    DateTimeOffset PlannedStart,
    decimal StartMileage,
    string? Notes = null);

public sealed record CompleteTripRequest(DateTimeOffset ActualEnd, decimal EndMileage);
public sealed record TripResult(bool Succeeded, TripResponse? Trip, string? Error, bool NotFound = false)
{
    public static TripResult Success(TripResponse trip) => new(true, trip, null);
    public static TripResult Failure(string error, bool notFound = false) => new(false, null, error, notFound);
}

public sealed record TripResponse(
    Guid Id,
    Guid OrganizationId,
    Guid VehicleId,
    string VehiclePlateNumber,
    Guid DriverId,
    string DriverFullName,
    Guid VehicleAssignmentId,
    string Origin,
    string Destination,
    string Purpose,
    DateTimeOffset PlannedStart,
    DateTimeOffset? ActualStart,
    DateTimeOffset? ActualEnd,
    decimal StartMileage,
    decimal? EndMileage,
    decimal? DistanceKm,
    TripStatus Status,
    string? Notes);

public sealed record TripPage(IReadOnlyCollection<TripResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record TripListQuery(int Page = 1, int PageSize = 20, Guid? VehicleId = null, Guid? DriverId = null, TripStatus? Status = null);

public interface ITripService
{
    Task<TripResult> CreateAsync(CreateTripRequest request, CancellationToken cancellationToken);
    Task<TripResult> GetAsync(Guid tripId, CancellationToken cancellationToken);
    Task<TripPage> ListAsync(TripListQuery query, CancellationToken cancellationToken);
    Task<TripResult> StartAsync(Guid tripId, CancellationToken cancellationToken);
    Task<TripResult> CompleteAsync(Guid tripId, CompleteTripRequest request, CancellationToken cancellationToken);
    Task<TripResult> CancelAsync(Guid tripId, CancellationToken cancellationToken);
}
