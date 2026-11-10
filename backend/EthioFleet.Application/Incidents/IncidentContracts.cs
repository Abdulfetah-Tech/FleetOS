using EthioFleet.Domain.Incidents;

namespace EthioFleet.Application.Incidents;

public sealed record CreateIncidentRequest(
    Guid VehicleId,
    Guid? DriverId,
    IncidentType Type,
    IncidentSeverity Severity,
    DateTimeOffset OccurredAt,
    string Location,
    string Description,
    decimal? EstimatedCost = null,
    string CurrencyCode = "ETB");

public sealed record UpdateIncidentStatusRequest(IncidentStatus Status, string? ResolutionNotes, decimal? ActualCost);
public sealed record IncidentResponse(
    Guid Id,
    Guid OrganizationId,
    Guid VehicleId,
    string VehiclePlateNumber,
    Guid? DriverId,
    string? DriverName,
    IncidentType Type,
    IncidentSeverity Severity,
    DateTimeOffset OccurredAt,
    string Location,
    string Description,
    decimal? EstimatedCost,
    decimal? ActualCost,
    string CurrencyCode,
    IncidentStatus Status,
    string? ResolutionNotes,
    DateTimeOffset? ResolvedAt);
public sealed record IncidentPage(IReadOnlyCollection<IncidentResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record IncidentResult(bool Succeeded, IncidentResponse? Incident, string? Error, bool NotFound = false)
{
    public static IncidentResult Success(IncidentResponse incident) => new(true, incident, null);
    public static IncidentResult Failure(string error, bool notFound = false) => new(false, null, error, notFound);
}

public interface IIncidentService
{
    Task<IncidentResult> CreateAsync(CreateIncidentRequest request, CancellationToken cancellationToken);
    Task<IncidentResult> GetAsync(Guid incidentId, CancellationToken cancellationToken);
    Task<IncidentPage> ListAsync(Guid? vehicleId, IncidentStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<IncidentResult> UpdateStatusAsync(Guid incidentId, UpdateIncidentStatusRequest request, CancellationToken cancellationToken);
}
