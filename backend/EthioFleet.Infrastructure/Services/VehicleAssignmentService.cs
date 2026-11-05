using EthioFleet.Application.Assignments;
using EthioFleet.Domain.Assignments;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class VehicleAssignmentService(AppDbContext dbContext, EthioFleet.Application.Interfaces.IClock clock)
    : IVehicleAssignmentService
{
    public async Task<AssignmentResult> AssignAsync(CreateVehicleAssignmentRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == request.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return AssignmentResult.Failure("Vehicle was not found.", notFound: true);
        }

        var driver = await dbContext.Drivers.Include(item => item.License)
            .SingleOrDefaultAsync(item => item.Id == request.DriverId, cancellationToken);
        if (driver is null)
        {
            return AssignmentResult.Failure("Driver was not found.", notFound: true);
        }

        if (vehicle.OrganizationId != driver.OrganizationId)
        {
            return AssignmentResult.Failure("Vehicle and driver must belong to the same organization.");
        }

        if (driver.License.IsExpired(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)))
        {
            return AssignmentResult.Failure("A driver with an expired license cannot be assigned.");
        }

        var assignment = new VehicleAssignment(vehicle.OrganizationId, vehicle, driver, clock.UtcNow, request.Notes);
        dbContext.VehicleAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AssignmentResult.Success(ToResponse(assignment));
    }

    public async Task<AssignmentResult> GetActiveForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.VehicleAssignments.AsNoTracking()
            .Include(item => item.Vehicle)
            .Include(item => item.Driver)
            .Where(item => item.VehicleId == vehicleId && item.IsActive)
            .SingleOrDefaultAsync(cancellationToken);
        return assignment is null
            ? AssignmentResult.Failure("Active assignment was not found.", notFound: true)
            : AssignmentResult.Success(ToResponse(assignment));
    }

    public async Task<AssignmentResult> UnassignAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.VehicleAssignments
            .Include(item => item.Vehicle)
            .Include(item => item.Driver)
            .Where(item => item.VehicleId == vehicleId && item.IsActive)
            .SingleOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return AssignmentResult.Failure("Active assignment was not found.", notFound: true);
        }

        assignment.Unassign(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AssignmentResult.Success(ToResponse(assignment));
    }

    private static VehicleAssignmentResponse ToResponse(VehicleAssignment assignment) => new(
        assignment.Id,
        assignment.OrganizationId,
        assignment.VehicleId,
        assignment.DriverId,
        assignment.Vehicle.PlateNumber,
        assignment.Driver.FullName,
        assignment.AssignedAt,
        assignment.UnassignedAt,
        assignment.IsActive,
        string.IsNullOrEmpty(assignment.Notes) ? null : assignment.Notes);
}
