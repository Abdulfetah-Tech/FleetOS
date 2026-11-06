using EthioFleet.Domain.Common;
using EthioFleet.Domain.Drivers;
using EthioFleet.Domain.Trips;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Domain.Fuel;

public sealed class FuelRecord : BaseEntity, ITenantOwned, ISoftDeletable
{
    private FuelRecord()
    {
        Vehicle = null!;
        CurrencyCode = "ETB";
        Station = string.Empty;
        ReceiptReference = string.Empty;
        Notes = string.Empty;
    }

    public FuelRecord(
        Guid organizationId,
        Vehicle vehicle,
        Driver? driver,
        Trip? trip,
        DateTimeOffset occurredAt,
        decimal quantityLiters,
        decimal unitPrice,
        decimal odometer,
        string? station = null,
        string currencyCode = "ETB",
        string? receiptReference = null,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ValidateTenantRelationships(organizationId, vehicle, driver, trip);
        ValidatePositive(quantityLiters, nameof(quantityLiters));
        ValidateNonNegative(unitPrice, nameof(unitPrice));
        ValidateNonNegative(odometer, nameof(odometer));
        ValidateCurrency(currencyCode);

        if (odometer < vehicle.CurrentMileage)
        {
            throw new DomainException("Fuel odometer cannot be less than the vehicle's current mileage.");
        }

        OrganizationId = organizationId;
        VehicleId = vehicle.Id;
        Vehicle = vehicle;
        DriverId = driver?.Id;
        TripId = trip?.Id;
        OccurredAt = occurredAt.ToUniversalTime();
        QuantityLiters = quantityLiters;
        UnitPrice = unitPrice;
        TotalCost = decimal.Round(quantityLiters * unitPrice, 2, MidpointRounding.AwayFromZero);
        Odometer = odometer;
        Station = NormalizeOptional(station, nameof(station), 160);
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ReceiptReference = NormalizeOptional(receiptReference, nameof(receiptReference), 100);
        Notes = NormalizeOptional(notes, nameof(notes), 1000);
    }

    public Guid OrganizationId { get; set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; }
    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }
    public Guid? TripId { get; private set; }
    public Trip? Trip { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public decimal QuantityLiters { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal Odometer { get; private set; }
    public string Station { get; private set; }
    public string CurrencyCode { get; private set; }
    public string ReceiptReference { get; private set; }
    public string Notes { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public static decimal CalculateTotalCost(decimal quantityLiters, decimal unitPrice)
    {
        ValidatePositive(quantityLiters, nameof(quantityLiters));
        ValidateNonNegative(unitPrice, nameof(unitPrice));
        return decimal.Round(quantityLiters * unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidateTenantRelationships(Guid organizationId, Vehicle vehicle, Driver? driver, Trip? trip)
    {
        if (organizationId == Guid.Empty || vehicle.OrganizationId != organizationId ||
            (driver is not null && driver.OrganizationId != organizationId) ||
            (trip is not null && (trip.OrganizationId != organizationId || trip.VehicleId != vehicle.Id ||
                (driver is not null && trip.DriverId != driver.Id))))
        {
            throw new DomainException("Fuel record relationships must belong to the same organization and vehicle.");
        }
    }

    private static void ValidatePositive(decimal value, string name)
    {
        if (value <= 0 || decimal.Round(value, 3) != value)
        {
            throw new DomainException($"{name} must be positive and have at most three decimal places.");
        }
    }

    private static void ValidateNonNegative(decimal value, string name)
    {
        if (value < 0 || decimal.Round(value, 2) != value)
        {
            throw new DomainException($"{name} must be non-negative and have at most two decimal places.");
        }
    }

    private static void ValidateCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 3 ||
            value.Trim().Any(character => !char.IsAsciiLetter(character)))
        {
            throw new DomainException("Currency code must contain three letters.");
        }
    }

    private static string NormalizeOptional(string? value, string name, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
