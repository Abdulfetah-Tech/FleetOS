using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Drivers;

public sealed class DriverLicense : BaseEntity, ITenantOwned
{
    private DriverLicense()
    {
        Number = string.Empty;
        NormalizedNumber = string.Empty;
        Category = string.Empty;
    }

    public DriverLicense(Guid driverId, string number, string category, DateOnly expiryDate)
    {
        DriverId = driverId;
        Number = NormalizeRequired(number, nameof(number), 40).ToUpperInvariant();
        NormalizedNumber = NormalizeIdentifier(Number);
        Category = NormalizeRequired(category, nameof(category), 20).ToUpperInvariant();
        ExpiryDate = expiryDate;
        ValidateExpiry(expiryDate);
    }

    public Guid DriverId { get; private set; }
    public Guid OrganizationId { get; set; }
    public string Number { get; private set; }
    public string NormalizedNumber { get; private set; }
    public string Category { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public bool IsExpired(DateOnly asOfDate) => ExpiryDate < asOfDate;

    public void Update(string number, string category, DateOnly expiryDate)
    {
        var normalizedNumber = NormalizeRequired(number, nameof(number), 40).ToUpperInvariant();
        var normalizedCategory = NormalizeRequired(category, nameof(category), 20).ToUpperInvariant();
        ValidateExpiry(expiryDate);
        Number = normalizedNumber;
        NormalizedNumber = NormalizeIdentifier(normalizedNumber);
        Category = normalizedCategory;
        ExpiryDate = expiryDate;
    }

    private static string NormalizeIdentifier(string value) =>
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

    private static void ValidateExpiry(DateOnly expiryDate)
    {
        if (expiryDate == default)
        {
            throw new DomainException("License expiry date is required.");
        }
    }
}
