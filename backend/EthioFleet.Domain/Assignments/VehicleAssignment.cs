using EthioFleet.Domain.Common;
using EthioFleet.Domain.Drivers;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Domain.Assignments;

public sealed class VehicleAssignment : BaseEntity, ITenantOwned
{
    private VehicleAssignment()
    {
        Vehicle = null!;
        Driver = null!;
        Notes = string.Empty;
    }

    public VehicleAssignment(
        Guid organizationId,
        Vehicle vehicle,
        Driver driver,
        DateTimeOffset assignedAt,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(driver);
        if (organizationId == Guid.Empty || vehicle.OrganizationId != organizationId || driver.OrganizationId != organizationId)
        {
            throw new DomainException("Vehicle and driver must belong to the assignment organization.");
        }

        if (vehicle.Status != VehicleStatus.Active)
        {
            throw new DomainException("Only active vehicles can be assigned.");
        }

        if (driver.Status != DriverStatus.Active)
        {
            throw new DomainException("Only active drivers can be assigned.");
        }

        OrganizationId = organizationId;
        VehicleId = vehicle.Id;
        DriverId = driver.Id;
        Vehicle = vehicle;
        Driver = driver;
        AssignedAt = assignedAt.ToUniversalTime();
        Notes = NormalizeNotes(notes);
        IsActive = true;
    }

    public Guid OrganizationId { get; set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? UnassignedAt { get; private set; }
    public bool IsActive { get; private set; }
    public string Notes { get; private set; }

    public void Unassign(DateTimeOffset unassignedAt)
    {
        if (!IsActive)
        {
            throw new DomainException("Assignment is already inactive.");
        }

        var normalizedTime = unassignedAt.ToUniversalTime();
        if (normalizedTime < AssignedAt)
        {
            throw new DomainException("Unassignment time cannot precede assignment time.");
        }

        UnassignedAt = normalizedTime;
        IsActive = false;
    }

    private static string NormalizeNotes(string? notes)
    {
        var normalized = notes?.Trim() ?? string.Empty;
        if (normalized.Length > 500)
        {
            throw new DomainException("Assignment notes cannot exceed 500 characters.");
        }

        return normalized;
    }
}
