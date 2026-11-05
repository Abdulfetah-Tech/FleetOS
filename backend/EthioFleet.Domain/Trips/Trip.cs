using EthioFleet.Domain.Assignments;
using EthioFleet.Domain.Common;
using EthioFleet.Domain.Drivers;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Domain.Trips;

public sealed class Trip : BaseEntity, ITenantOwned, ISoftDeletable
{
    private Trip()
    {
        Origin = string.Empty;
        Destination = string.Empty;
        Purpose = string.Empty;
        Notes = string.Empty;
        Vehicle = null!;
        Driver = null!;
        VehicleAssignment = null!;
    }

    public Trip(
        Guid organizationId,
        Vehicle vehicle,
        Driver driver,
        VehicleAssignment assignment,
        string origin,
        string destination,
        string purpose,
        DateTimeOffset plannedStart,
        decimal startMileage,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(assignment);
        ValidateTenantAndAssignment(organizationId, vehicle, driver, assignment);
        Origin = NormalizeRequired(origin, nameof(origin), 200);
        Destination = NormalizeRequired(destination, nameof(destination), 200);
        Purpose = NormalizeRequired(purpose, nameof(purpose), 500);
        ValidateMileage(startMileage);
        if (startMileage < vehicle.CurrentMileage)
        {
            throw new DomainException("Trip starting mileage cannot be less than the vehicle's current mileage.");
        }

        OrganizationId = organizationId;
        VehicleId = vehicle.Id;
        DriverId = driver.Id;
        VehicleAssignmentId = assignment.Id;
        Vehicle = vehicle;
        Driver = driver;
        VehicleAssignment = assignment;
        PlannedStart = plannedStart.ToUniversalTime();
        StartMileage = startMileage;
        Notes = NormalizeOptional(notes, 2000);
        Status = TripStatus.Planned;
    }

    public Guid OrganizationId { get; set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; }
    public Guid VehicleAssignmentId { get; private set; }
    public VehicleAssignment VehicleAssignment { get; private set; }
    public string Origin { get; private set; }
    public string Destination { get; private set; }
    public string Purpose { get; private set; }
    public DateTimeOffset PlannedStart { get; private set; }
    public DateTimeOffset? ActualStart { get; private set; }
    public DateTimeOffset? ActualEnd { get; private set; }
    public decimal StartMileage { get; private set; }
    public decimal? EndMileage { get; private set; }
    public decimal? DistanceKm => EndMileage - StartMileage;
    public TripStatus Status { get; private set; }
    public string Notes { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public void Start(DateTimeOffset actualStart)
    {
        if (Status != TripStatus.Planned)
        {
            throw new DomainException("Only planned trips can start.");
        }

        if (!VehicleAssignment.IsActive || Vehicle.Status != VehicleStatus.Active || Driver.Status != DriverStatus.Active)
        {
            throw new DomainException("The assigned vehicle and driver must remain active to start a trip.");
        }

        var start = actualStart.ToUniversalTime();
        if (start < PlannedStart)
        {
            throw new DomainException("Actual trip start cannot precede the planned start.");
        }

        ActualStart = start;
        Status = TripStatus.InProgress;
    }

    public void Complete(DateTimeOffset actualEnd, decimal endMileage)
    {
        if (Status != TripStatus.InProgress)
        {
            throw new DomainException("Only in-progress trips can be completed.");
        }

        ValidateMileage(endMileage);
        if (endMileage < StartMileage)
        {
            throw new DomainException("Trip ending mileage cannot be less than starting mileage.");
        }

        var end = actualEnd.ToUniversalTime();
        if (ActualStart is null || end < ActualStart.Value)
        {
            throw new DomainException("Trip end cannot precede trip start.");
        }

        ActualEnd = end;
        EndMileage = endMileage;
        Vehicle.RecordOdometer(endMileage);
        Status = TripStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is TripStatus.Completed or TripStatus.Cancelled)
        {
            throw new DomainException("Completed or cancelled trips cannot be cancelled again.");
        }

        Status = TripStatus.Cancelled;
    }

    private static void ValidateTenantAndAssignment(
        Guid organizationId,
        Vehicle vehicle,
        Driver driver,
        VehicleAssignment assignment)
    {
        if (organizationId == Guid.Empty || vehicle.OrganizationId != organizationId ||
            driver.OrganizationId != organizationId || assignment.OrganizationId != organizationId ||
            assignment.VehicleId != vehicle.Id || assignment.DriverId != driver.Id || !assignment.IsActive)
        {
            throw new DomainException("Trip vehicle, driver, and active assignment must belong to the same organization.");
        }
    }

    private static void ValidateMileage(decimal mileage)
    {
        if (mileage < 0 || decimal.Round(mileage, 1) != mileage)
        {
            throw new DomainException("Trip mileage must be non-negative and have at most one decimal place.");
        }
    }

    private static string NormalizeRequired(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"Trip notes cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
