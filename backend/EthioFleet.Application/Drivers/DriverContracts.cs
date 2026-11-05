using EthioFleet.Domain.Drivers;

namespace EthioFleet.Application.Drivers;

public sealed record CreateDriverRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string LicenseNumber,
    string LicenseCategory,
    DateOnly LicenseExpiryDate,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null);

public sealed record UpdateDriverRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string LicenseNumber,
    string LicenseCategory,
    DateOnly LicenseExpiryDate,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null);

public sealed record DriverStatusRequest(DriverStatus Status);

public sealed record DriverResponse(
    Guid Id,
    Guid OrganizationId,
    string FullName,
    string Email,
    string PhoneNumber,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string LicenseNumber,
    string LicenseCategory,
    DateOnly LicenseExpiryDate,
    bool LicenseExpired,
    DriverStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DriverPage(IReadOnlyCollection<DriverResponse> Items, int Page, int PageSize, int TotalCount);

public sealed record DriverResult(bool Succeeded, DriverResponse? Driver, string? Error, bool NotFound = false)
{
    public static DriverResult Success(DriverResponse driver) => new(true, driver, null);
    public static DriverResult Failure(string error, bool notFound = false) => new(false, null, error, notFound);
}

public sealed record DriverListQuery(int Page = 1, int PageSize = 20, string? Search = null);

public interface IDriverService
{
    Task<DriverResult> CreateAsync(CreateDriverRequest request, CancellationToken cancellationToken);
    Task<DriverResult> GetAsync(Guid driverId, CancellationToken cancellationToken);
    Task<DriverPage> ListAsync(DriverListQuery query, CancellationToken cancellationToken);
    Task<DriverResult> UpdateAsync(Guid driverId, UpdateDriverRequest request, CancellationToken cancellationToken);
    Task<DriverResult> ChangeStatusAsync(Guid driverId, DriverStatusRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid driverId, CancellationToken cancellationToken);
}
