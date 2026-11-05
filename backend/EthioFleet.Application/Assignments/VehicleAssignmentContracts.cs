using EthioFleet.Domain.Assignments;

namespace EthioFleet.Application.Assignments;

public sealed record CreateVehicleAssignmentRequest(Guid VehicleId, Guid DriverId, string? Notes = null);
public sealed record VehicleAssignmentResponse(
    Guid Id,
    Guid OrganizationId,
    Guid VehicleId,
    Guid DriverId,
    string VehiclePlateNumber,
    string DriverFullName,
    DateTimeOffset AssignedAt,
    DateTimeOffset? UnassignedAt,
    bool IsActive,
    string? Notes);
public sealed record AssignmentResult(bool Succeeded, VehicleAssignmentResponse? Assignment, string? Error, bool NotFound = false)
{
    public static AssignmentResult Success(VehicleAssignmentResponse assignment) => new(true, assignment, null);
    public static AssignmentResult Failure(string error, bool notFound = false) => new(false, null, error, notFound);
}

public interface IVehicleAssignmentService
{
    Task<AssignmentResult> AssignAsync(CreateVehicleAssignmentRequest request, CancellationToken cancellationToken);
    Task<AssignmentResult> GetActiveForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task<AssignmentResult> UnassignAsync(Guid vehicleId, CancellationToken cancellationToken);
}
