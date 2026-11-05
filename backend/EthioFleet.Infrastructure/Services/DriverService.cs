using EthioFleet.Application.Drivers;
using EthioFleet.Domain.Drivers;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class DriverService(AppDbContext dbContext) : IDriverService
{
    public async Task<DriverResult> CreateAsync(CreateDriverRequest request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var license = new DriverLicense(id, request.LicenseNumber, request.LicenseCategory, request.LicenseExpiryDate);
        var driver = new Driver(id, request.FullName, request.Email, request.PhoneNumber, license,
            request.EmergencyContactName, request.EmergencyContactPhone);
        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DriverResult.Success(ToResponse(driver));
    }

    public async Task<DriverResult> GetAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var driver = await dbContext.Drivers.Include(item => item.License).AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == driverId, cancellationToken);
        return driver is null
            ? DriverResult.Failure("Driver was not found.", notFound: true)
            : DriverResult.Success(ToResponse(driver));
    }

    public async Task<DriverPage> ListAsync(DriverListQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        IQueryable<Driver> drivers = dbContext.Drivers.AsNoTracking().Include(driver => driver.License);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = EscapeLike(query.Search.Trim());
            drivers = drivers.Where(driver =>
                EF.Functions.ILike(driver.FullName, $"%{search}%") ||
                EF.Functions.ILike(driver.Email, $"%{search}%") ||
                EF.Functions.ILike(driver.PhoneNumber, $"%{search}%") ||
                EF.Functions.ILike(driver.License.Number, $"%{search}%"));
        }

        var count = await drivers.CountAsync(cancellationToken);
        var results = await drivers.OrderBy(driver => driver.FullName)
            .ThenBy(driver => driver.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new DriverPage(results.Select(ToResponse).ToArray(), page, pageSize, count);
    }

    public async Task<DriverResult> UpdateAsync(Guid driverId, UpdateDriverRequest request, CancellationToken cancellationToken)
    {
        var driver = await dbContext.Drivers.Include(item => item.License)
            .SingleOrDefaultAsync(item => item.Id == driverId, cancellationToken);
        if (driver is null)
        {
            return DriverResult.Failure("Driver was not found.", notFound: true);
        }

        driver.UpdateContactDetails(request.FullName, request.Email, request.PhoneNumber,
            request.EmergencyContactName, request.EmergencyContactPhone);
        driver.UpdateLicense(request.LicenseNumber, request.LicenseCategory, request.LicenseExpiryDate);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DriverResult.Success(ToResponse(driver));
    }

    public async Task<DriverResult> ChangeStatusAsync(Guid driverId, DriverStatusRequest request, CancellationToken cancellationToken)
    {
        var driver = await dbContext.Drivers.Include(item => item.License)
            .SingleOrDefaultAsync(item => item.Id == driverId, cancellationToken);
        if (driver is null)
        {
            return DriverResult.Failure("Driver was not found.", notFound: true);
        }

        driver.ChangeStatus(request.Status);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DriverResult.Success(ToResponse(driver));
    }

    public async Task<bool> DeleteAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var driver = await dbContext.Drivers.Include(item => item.License)
            .SingleOrDefaultAsync(item => item.Id == driverId, cancellationToken);
        if (driver is null)
        {
            return false;
        }

        dbContext.Drivers.Remove(driver);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static DriverResponse ToResponse(Driver driver) => new(
        driver.Id,
        driver.OrganizationId,
        driver.FullName,
        driver.Email,
        driver.PhoneNumber,
        string.IsNullOrEmpty(driver.EmergencyContactName) ? null : driver.EmergencyContactName,
        string.IsNullOrEmpty(driver.EmergencyContactPhone) ? null : driver.EmergencyContactPhone,
        driver.License.Number,
        driver.License.Category,
        driver.License.ExpiryDate,
        driver.License.IsExpired(DateOnly.FromDateTime(DateTime.UtcNow)),
        driver.Status,
        driver.CreatedAt,
        driver.UpdatedAt);

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
