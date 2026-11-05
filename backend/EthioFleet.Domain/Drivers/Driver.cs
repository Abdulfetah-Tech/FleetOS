using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Drivers;

public sealed class Driver : BaseEntity, ITenantOwned, ISoftDeletable
{
    private Driver()
    {
        FullName = string.Empty;
        Email = string.Empty;
        NormalizedEmail = string.Empty;
        PhoneNumber = string.Empty;
        EmergencyContactName = string.Empty;
        EmergencyContactPhone = string.Empty;
    }

    public Driver(
        string fullName,
        string email,
        string phoneNumber,
        DriverLicense license,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null)
        : this(Guid.NewGuid(), fullName, email, phoneNumber, license, emergencyContactName, emergencyContactPhone)
    {
    }

    public Driver(
        Guid id,
        string fullName,
        string email,
        string phoneNumber,
        DriverLicense license,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null)
    {
        Id = id;
        FullName = NormalizeRequired(fullName, nameof(fullName), 160);
        Email = NormalizeEmail(email);
        NormalizedEmail = Email.ToUpperInvariant();
        PhoneNumber = NormalizeRequired(phoneNumber, nameof(phoneNumber), 32);
        License = license ?? throw new ArgumentNullException(nameof(license));
        EmergencyContactName = NormalizeOptional(emergencyContactName, nameof(emergencyContactName), 160);
        EmergencyContactPhone = NormalizeOptional(emergencyContactPhone, nameof(emergencyContactPhone), 32);
        Status = DriverStatus.Active;
    }

    public Guid OrganizationId { get; set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string NormalizedEmail { get; private set; }
    public string PhoneNumber { get; private set; }
    public string EmergencyContactName { get; private set; }
    public string EmergencyContactPhone { get; private set; }
    public DriverLicense License { get; private set; } = null!;
    public DriverStatus Status { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public void UpdateContactDetails(
        string fullName,
        string email,
        string phoneNumber,
        string? emergencyContactName,
        string? emergencyContactPhone)
    {
        var normalizedFullName = NormalizeRequired(fullName, nameof(fullName), 160);
        var normalizedEmail = NormalizeEmail(email);
        var normalizedPhone = NormalizeRequired(phoneNumber, nameof(phoneNumber), 32);
        var contactName = NormalizeOptional(emergencyContactName, nameof(emergencyContactName), 160);
        var contactPhone = NormalizeOptional(emergencyContactPhone, nameof(emergencyContactPhone), 32);

        FullName = normalizedFullName;
        Email = normalizedEmail;
        NormalizedEmail = normalizedEmail.ToUpperInvariant();
        PhoneNumber = normalizedPhone;
        EmergencyContactName = contactName;
        EmergencyContactPhone = contactPhone;
    }

    public void UpdateLicense(string number, string category, DateOnly expiryDate) =>
        License.Update(number, category, expiryDate);

    public void ChangeStatus(DriverStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Driver status is invalid.");
        }

        Status = status;
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = NormalizeRequired(email, nameof(email), 254).ToLowerInvariant();
        if (!System.Net.Mail.MailAddress.TryCreate(normalized, out var parsed) ||
            !string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Driver email address is invalid.");
        }

        return normalized;
    }

    private static string NormalizeOptional(string? value, string name, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeRequired(value, name, maxLength);

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
