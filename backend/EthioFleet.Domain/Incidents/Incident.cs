using EthioFleet.Domain.Common;
using EthioFleet.Domain.Drivers;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Domain.Incidents;

public sealed class Incident : BaseEntity, ITenantOwned, ISoftDeletable
{
    private Incident()
    {
        Vehicle = null!;
        Location = string.Empty;
        Description = string.Empty;
        CurrencyCode = "ETB";
        ResolutionNotes = string.Empty;
    }

    public Incident(
        Guid organizationId,
        Vehicle vehicle,
        Driver? driver,
        IncidentType type,
        IncidentSeverity severity,
        DateTimeOffset occurredAt,
        string location,
        string description,
        decimal? estimatedCost = null,
        string currencyCode = "ETB")
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        if (organizationId == Guid.Empty || vehicle.OrganizationId != organizationId ||
            (driver is not null && driver.OrganizationId != organizationId))
        {
            throw new DomainException("Incident vehicle and driver must belong to the incident organization.");
        }
        if (!Enum.IsDefined(type) || !Enum.IsDefined(severity))
        {
            throw new DomainException("Incident type or severity is invalid.");
        }
        ValidateCost(estimatedCost);

        OrganizationId = organizationId;
        VehicleId = vehicle.Id;
        Vehicle = vehicle;
        DriverId = driver?.Id;
        Type = type;
        Severity = severity;
        OccurredAt = occurredAt.ToUniversalTime();
        Location = NormalizeRequired(location, nameof(location), 240);
        Description = NormalizeRequired(description, nameof(description), 2000);
        EstimatedCost = estimatedCost;
        CurrencyCode = NormalizeCurrency(currencyCode);
        ResolutionNotes = string.Empty;
        Status = IncidentStatus.Open;
    }

    public Guid OrganizationId { get; set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; }
    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }
    public IncidentType Type { get; private set; }
    public IncidentSeverity Severity { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Location { get; private set; }
    public string Description { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? ActualCost { get; private set; }
    public string CurrencyCode { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string ResolutionNotes { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public void UpdateStatus(IncidentStatus status, string? resolutionNotes, decimal? actualCost, DateTimeOffset changedAt)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Incident status is invalid.");
        }
        if (Status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            throw new DomainException("Resolved or closed incidents cannot be reopened or changed.");
        }
        ValidateCost(actualCost);
        if (status is IncidentStatus.Resolved or IncidentStatus.Closed && string.IsNullOrWhiteSpace(resolutionNotes))
        {
            throw new DomainException("Resolution notes are required to resolve or close an incident.");
        }

        Status = status;
        if (status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            ResolutionNotes = NormalizeRequired(resolutionNotes!, nameof(resolutionNotes), 2000);
            ActualCost = actualCost;
            ResolvedAt = changedAt.ToUniversalTime();
        }
    }

    private static void ValidateCost(decimal? cost)
    {
        if (cost is decimal amount && (amount < 0 || decimal.Round(amount, 2) != amount))
        {
            throw new DomainException("Incident costs must be non-negative and have at most two decimal places.");
        }
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 3 || value.Trim().Any(character => !char.IsAsciiLetter(character)))
        {
            throw new DomainException("Currency code must contain three letters.");
        }
        return value.Trim().ToUpperInvariant();
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
}
