using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Vehicles;

public sealed class Vehicle : BaseEntity, ITenantOwned, ISoftDeletable
{
    private Vehicle()
    {
        PlateNumber = string.Empty;
        NormalizedPlateNumber = string.Empty;
        Make = string.Empty;
        Model = string.Empty;
        Vin = string.Empty;
        NormalizedVin = string.Empty;
        CurrencyCode = "ETB";
    }

    public Vehicle(
        string plateNumber,
        string make,
        string model,
        int year,
        VehicleType type,
        FuelType fuelType,
        decimal currentMileage,
        string? vin = null,
        string currencyCode = "ETB",
        DateOnly? purchaseDate = null)
    {
        PlateNumber = NormalizeRequired(plateNumber, nameof(plateNumber), 20).ToUpperInvariant();
        NormalizedPlateNumber = NormalizeIdentifier(PlateNumber);
        Make = NormalizeRequired(make, nameof(make), 80);
        Model = NormalizeRequired(model, nameof(model), 80);
        ValidateYear(year);
        ValidateMileage(currentMileage);
        ValidateEnum(type, nameof(type));
        ValidateEnum(fuelType, nameof(fuelType));
        CurrencyCode = NormalizeCurrency(currencyCode);

        if (vin is not null && !string.IsNullOrWhiteSpace(vin))
        {
            Vin = NormalizeVin(vin);
            NormalizedVin = NormalizeIdentifier(Vin);
        }
        else
        {
            Vin = null;
            NormalizedVin = null;
        }

        Year = year;
        Type = type;
        FuelType = fuelType;
        CurrentMileage = currentMileage;
        PurchaseDate = purchaseDate;
        Status = VehicleStatus.Active;
    }

    public Guid OrganizationId { get; set; }
    public string PlateNumber { get; private set; }
    public string NormalizedPlateNumber { get; private set; }
    public string Make { get; private set; }
    public string Model { get; private set; }
    public int Year { get; private set; }
    public VehicleType Type { get; private set; }
    public FuelType FuelType { get; private set; }
    public decimal CurrentMileage { get; private set; }
    public string? Vin { get; private set; }
    public string? NormalizedVin { get; private set; }
    public VehicleStatus Status { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public string CurrencyCode { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public void UpdateDetails(
        string plateNumber,
        string make,
        string model,
        int year,
        VehicleType type,
        FuelType fuelType,
        string? vin,
        DateOnly? purchaseDate)
    {
        PlateNumber = NormalizeRequired(plateNumber, nameof(plateNumber), 20).ToUpperInvariant();
        NormalizedPlateNumber = NormalizeIdentifier(PlateNumber);
        Make = NormalizeRequired(make, nameof(make), 80);
        Model = NormalizeRequired(model, nameof(model), 80);
        ValidateYear(year);
        ValidateEnum(type, nameof(type));
        ValidateEnum(fuelType, nameof(fuelType));

        if (vin is not null && !string.IsNullOrWhiteSpace(vin))
        {
            Vin = NormalizeVin(vin);
            NormalizedVin = NormalizeIdentifier(Vin);
        }
        else
        {
            Vin = null;
            NormalizedVin = null;
        }

        Year = year;
        Type = type;
        FuelType = fuelType;
        PurchaseDate = purchaseDate;
    }

    public void RecordOdometer(decimal mileage)
    {
        ValidateMileage(mileage);
        if (mileage < CurrentMileage)
        {
            throw new DomainException("Vehicle mileage cannot decrease.");
        }

        CurrentMileage = mileage;
    }

    public void ChangeStatus(VehicleStatus status)
    {
        ValidateEnum(status, nameof(status));
        if (Status == VehicleStatus.Retired && status != VehicleStatus.Retired)
        {
            throw new DomainException("A retired vehicle cannot be reactivated.");
        }

        Status = status;
    }

    public static string NormalizeIdentifier(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character) && character != '-')).ToUpperInvariant();

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

    private static string NormalizeVin(string value)
    {
        var vin = NormalizeRequired(value, nameof(Vin), 17).ToUpperInvariant();
        if (vin.Length != 17 || vin.Any(character => !char.IsLetterOrDigit(character) || character is 'I' or 'O' or 'Q'))
        {
            throw new DomainException("VIN must contain exactly 17 letters or digits and cannot contain I, O, or Q.");
        }

        return vin;
    }

    private static string NormalizeCurrency(string value)
    {
        var currency = NormalizeRequired(value, nameof(CurrencyCode), 3).ToUpperInvariant();
        if (currency.Length != 3 || currency.Any(character => !char.IsAsciiLetter(character)))
        {
            throw new DomainException("Currency code must contain three letters.");
        }

        return currency;
    }

    private static void ValidateYear(int year)
    {
        var maximumYear = DateTime.UtcNow.Year + 1;
        if (year < 1886 || year > maximumYear)
        {
            throw new DomainException($"Vehicle year must be between 1886 and {maximumYear}.");
        }
    }

    private static void ValidateMileage(decimal mileage)
    {
        if (mileage < 0 || decimal.Round(mileage, 1) != mileage)
        {
            throw new DomainException("Vehicle mileage must be non-negative and have at most one decimal place.");
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string name) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException($"{name} is invalid.");
        }
    }
}
