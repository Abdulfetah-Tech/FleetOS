using EthioFleet.Application.Trips;
using EthioFleet.Domain.Assignments;
using EthioFleet.Domain.Trips;
using EthioFleet.Domain.Vehicles;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class TripService(AppDbContext dbContext, EthioFleet.Application.Interfaces.IClock clock) : ITripService
{
    public async Task<TripResult> CreateAsync(CreateTripRequest request, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.VehicleAssignments
            .Include(item => item.Vehicle)
            .Include(item => item.Driver).ThenInclude(item => item.License)
            .Where(item => item.VehicleId == request.VehicleId && item.DriverId == request.DriverId && item.IsActive)
            .SingleOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return TripResult.Failure("An active assignment for this vehicle and driver was not found.", notFound: true);
        }

        if (assignment.Driver.License.IsExpired(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)))
        {
            return TripResult.Failure("A driver with an expired license cannot start a trip.");
        }

        var trip = new Trip(assignment.OrganizationId, assignment.Vehicle, assignment.Driver, assignment,
            request.Origin, request.Destination, request.Purpose, request.PlannedStart, request.StartMileage, request.Notes);
        dbContext.Trips.Add(trip);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TripResult.Success(ToResponse(trip));
    }

    public async Task<TripResult> GetAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await QueryTrips().SingleOrDefaultAsync(item => item.Id == tripId, cancellationToken);
        return trip is null
            ? TripResult.Failure("Trip was not found.", notFound: true)
            : TripResult.Success(ToResponse(trip));
    }

    public async Task<TripPage> ListAsync(TripListQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        IQueryable<Trip> trips = QueryTrips();
        if (query.VehicleId is Guid vehicleId)
        {
            trips = trips.Where(trip => trip.VehicleId == vehicleId);
        }
        if (query.DriverId is Guid driverId)
        {
            trips = trips.Where(trip => trip.DriverId == driverId);
        }
        if (query.Status is TripStatus status)
        {
            trips = trips.Where(trip => trip.Status == status);
        }

        var count = await trips.CountAsync(cancellationToken);
        var items = await trips.OrderByDescending(trip => trip.PlannedStart)
            .ThenBy(trip => trip.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new TripPage(items.Select(ToResponse).ToArray(), page, pageSize, count);
    }

    public async Task<TripResult> StartAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await QueryTrips().SingleOrDefaultAsync(item => item.Id == tripId, cancellationToken);
        if (trip is null)
        {
            return TripResult.Failure("Trip was not found.", notFound: true);
        }

        trip.Start(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TripResult.Success(ToResponse(trip));
    }

    public async Task<TripResult> CompleteAsync(Guid tripId, CompleteTripRequest request, CancellationToken cancellationToken)
    {
        var trip = await QueryTrips().SingleOrDefaultAsync(item => item.Id == tripId, cancellationToken);
        if (trip is null)
        {
            return TripResult.Failure("Trip was not found.", notFound: true);
        }

        trip.Complete(request.ActualEnd, request.EndMileage);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TripResult.Success(ToResponse(trip));
    }

    public async Task<TripResult> CancelAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await QueryTrips().SingleOrDefaultAsync(item => item.Id == tripId, cancellationToken);
        if (trip is null)
        {
            return TripResult.Failure("Trip was not found.", notFound: true);
        }

        trip.Cancel();
        await dbContext.SaveChangesAsync(cancellationToken);
        return TripResult.Success(ToResponse(trip));
    }

    private IQueryable<Trip> QueryTrips() => dbContext.Trips
        .Include(trip => trip.Vehicle)
        .Include(trip => trip.Driver)
        .Include(trip => trip.VehicleAssignment);

    private static TripResponse ToResponse(Trip trip) => new(
        trip.Id,
        trip.OrganizationId,
        trip.VehicleId,
        trip.Vehicle.PlateNumber,
        trip.DriverId,
        trip.Driver.FullName,
        trip.VehicleAssignmentId,
        trip.Origin,
        trip.Destination,
        trip.Purpose,
        trip.PlannedStart,
        trip.ActualStart,
        trip.ActualEnd,
        trip.StartMileage,
        trip.EndMileage,
        trip.DistanceKm,
        trip.Status,
        string.IsNullOrEmpty(trip.Notes) ? null : trip.Notes);
}
