using EthioFleet.Domain.Common;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Domain.Documents;

public sealed class VehicleDocument : BaseEntity, ITenantOwned, ISoftDeletable
{
    private VehicleDocument()
    {
        Vehicle = null!;
        Name = string.Empty;
        StorageKey = string.Empty;
        MediaType = string.Empty;
        FileName = string.Empty;
        ChecksumSha256 = string.Empty;
    }

    public VehicleDocument(
        Guid organizationId,
        Vehicle vehicle,
        VehicleDocumentType type,
        string name,
        string fileName,
        string mediaType,
        string storageKey,
        long fileSizeBytes,
        DateOnly? issuedOn = null,
        DateOnly? expiresOn = null,
        string? checksumSha256 = null)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        if (organizationId == Guid.Empty || vehicle.OrganizationId != organizationId)
        {
            throw new DomainException("Vehicle document must belong to the vehicle's organization.");
        }
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Vehicle document type is invalid.");
        }
        if (fileSizeBytes <= 0 || fileSizeBytes > 50L * 1024 * 1024)
        {
            throw new DomainException("Document file size must be between 1 byte and 50 MB.");
        }
        if (issuedOn is DateOnly issue && expiresOn is DateOnly expiry && expiry < issue)
        {
            throw new DomainException("Document expiry date cannot precede its issue date.");
        }

        OrganizationId = organizationId;
        VehicleId = vehicle.Id;
        Vehicle = vehicle;
        Type = type;
        Name = NormalizeRequired(name, nameof(name), 160);
        FileName = NormalizeFileName(fileName);
        MediaType = NormalizeMediaType(mediaType);
        StorageKey = NormalizeStorageKey(storageKey);
        FileSizeBytes = fileSizeBytes;
        IssuedOn = issuedOn;
        ExpiresOn = expiresOn;
        ChecksumSha256 = NormalizeChecksum(checksumSha256);
        Status = VehicleDocumentStatus.Active;
    }

    public Guid OrganizationId { get; set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; }
    public VehicleDocumentType Type { get; private set; }
    public string Name { get; private set; }
    public string FileName { get; private set; }
    public string MediaType { get; private set; }
    public string StorageKey { get; private set; }
    public long FileSizeBytes { get; private set; }
    public DateOnly? IssuedOn { get; private set; }
    public DateOnly? ExpiresOn { get; private set; }
    public VehicleDocumentStatus Status { get; private set; }
    public string ChecksumSha256 { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public bool IsExpired(DateOnly asOfDate) => ExpiresOn is DateOnly expiry && expiry < asOfDate;

    public void Archive()
    {
        if (Status == VehicleDocumentStatus.Archived)
        {
            throw new DomainException("Vehicle document is already archived.");
        }
        Status = VehicleDocumentStatus.Archived;
    }

    public void RefreshExpiryStatus(DateOnly asOfDate)
    {
        if (Status == VehicleDocumentStatus.Archived)
        {
            return;
        }
        Status = IsExpired(asOfDate) ? VehicleDocumentStatus.Expired : VehicleDocumentStatus.Active;
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

    private static string NormalizeFileName(string value)
    {
        var normalized = NormalizeRequired(value, nameof(FileName), 255).Replace('\\', '/');
        normalized = Path.GetFileName(normalized);
        if (normalized is "." or ".." || normalized.Any(char.IsControl))
        {
            throw new DomainException("Document filename is invalid.");
        }
        return normalized;
    }

    private static string NormalizeMediaType(string value)
    {
        var normalized = NormalizeRequired(value, nameof(MediaType), 127).ToLowerInvariant();
        if (!normalized.Contains('/') || normalized.Any(char.IsControl))
        {
            throw new DomainException("Document media type is invalid.");
        }
        return normalized;
    }

    private static string NormalizeStorageKey(string value)
    {
        var normalized = NormalizeRequired(value, nameof(StorageKey), 512);
        if (normalized.StartsWith('/') || normalized.Contains("..", StringComparison.Ordinal) ||
            normalized.Any(char.IsControl) || Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            throw new DomainException("Document storage key must be an opaque relative key.");
        }
        return normalized;
    }

    private static string NormalizeChecksum(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Length != 0 && (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character))))
        {
            throw new DomainException("SHA-256 checksum must contain exactly 64 hexadecimal characters.");
        }
        return normalized;
    }
}
