using EthioFleet.Domain.Common;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Domain.Maintenance;

public sealed class MaintenanceRecord : BaseEntity, ITenantOwned, ISoftDeletable
{
    private MaintenanceRecord()
    {
        Vehicle = null!;
        Description = string.Empty;
        CurrencyCode = "ETB";
        ServiceProvider = string.Empty;
        Reference = string.Empty;
        Notes = string.Empty;
    }

    public MaintenanceRecord(
        Guid organizationId,
        Vehicle vehicle,
        MaintenanceType type,
        string description,
        DateOnly performedOn,
        decimal odometer,
        decimal laborCost,
        decimal partsCost,
        string currencyCode = "ETB",
        string? serviceProvider = null,
        string? reference = null,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        if (organizationId == Guid.Empty || vehicle.OrganizationId != organizationId)
        {
            throw new DomainException("Maintenance vehicle must belong to the maintenance organization.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Maintenance type is invalid.");
        }

        Type = type;
        Description = NormalizeRequired(description, nameof(description), 500);
        ValidateDate(performedOn);
        ValidateOdometer(odometer);
        ValidateCost(laborCost, nameof(laborCost));
        ValidateCost(partsCost, nameof(partsCost));
        CurrencyCode = NormalizeCurrency(currencyCode);
        PerformedOn = performedOn;
        Odometer = odometer;
        LaborCost = laborCost;
        PartsCost = partsCost;
        TotalCost = decimal.Round(laborCost + partsCost, 2, MidpointRounding.AwayFromZero);
        VehicleId = vehicle.Id;
        Vehicle = vehicle;
        ServiceProvider = NormalizeOptional(serviceProvider, nameof(serviceProvider), 160);
        Reference = NormalizeOptional(reference, nameof(reference), 100);
        Notes = NormalizeOptional(notes, nameof(notes), 2000);
    }

    public Guid OrganizationId { get; set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; }
    public MaintenanceType Type { get; private set; }
    public string Description { get; private set; }
    public DateOnly PerformedOn { get; private set; }
    public decimal Odometer { get; private set; }
    public decimal LaborCost { get; private set; }
    public decimal PartsCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public string CurrencyCode { get; private set; }
    public string ServiceProvider { get; private set; }
    public string Reference { get; private set; }
    public string Notes { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private static void ValidateDate(DateOnly date)
    {
        if (date == default)
        {
            throw new DomainException("Maintenance date is required.");
        }
    }

    private static void ValidateOdometer(decimal value)
    {
        if (value < 0 || decimal.Round(value, 1) != value)
        {
            throw new DomainException("Maintenance odometer must be non-negative with at most one decimal place.");
        }
    }

    private static void ValidateCost(decimal value, string name)
    {
        if (value < 0 || decimal.Round(value, 2) != value)
        {
            throw new DomainException($"{name} must be non-negative with at most two decimal places.");
        }
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 3 ||
            value.Trim().Any(character => !char.IsAsciiLetter(character)))
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

        var result = value.Trim();
        if (result.Length > maxLength)
        {
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        }

        return result;
    }

    private static string NormalizeOptional(string? value, string name, int maxLength)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length > maxLength)
        {
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        }

        return result;
    }
}
