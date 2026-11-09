using EthioFleet.Domain.Inspections;

namespace EthioFleet.Application.Inspections;

public sealed record InspectionItemResponseRequest(string Key, EthioFleet.Domain.Inspections.InspectionResult Result, string? Notes = null);
public sealed record CreateInspectionRequest(
    Guid VehicleId,
    decimal Odometer,
    string ChecklistVersion,
    IReadOnlyCollection<InspectionChecklistItemDefinition> ChecklistItems,
    string? Notes = null);
public sealed record CompleteInspectionRequest(IReadOnlyCollection<InspectionItemResponseRequest> Responses);
public sealed record InspectionResponse(
    Guid Id,
    Guid OrganizationId,
    Guid VehicleId,
    string VehiclePlateNumber,
    Guid? InspectorUserId,
    DateTimeOffset InspectedAt,
    decimal Odometer,
    string ChecklistVersion,
    IReadOnlyCollection<InspectionChecklistItemDefinition> ChecklistItems,
    IReadOnlyCollection<EthioFleet.Domain.Inspections.InspectionItemResponse> Responses,
    InspectionStatus Status,
    bool RequiresVehicleOutOfService,
    DateTimeOffset? CompletedAt,
    string? Notes);
public sealed record InspectionPage(IReadOnlyCollection<InspectionResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record InspectionResult(bool Succeeded, InspectionResponse? Inspection, string? Error, bool NotFound = false)
{
    public static InspectionResult Success(InspectionResponse inspection) => new(true, inspection, null);
    public static InspectionResult Failure(string error, bool notFound = false) => new(false, null, error, notFound);
}

public interface IInspectionService
{
    Task<InspectionResult> CreateAsync(CreateInspectionRequest request, CancellationToken cancellationToken);
    Task<InspectionResult> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<InspectionPage> ListAsync(Guid? vehicleId, int page, int pageSize, CancellationToken cancellationToken);
    Task<InspectionResult> CompleteAsync(Guid id, CompleteInspectionRequest request, CancellationToken cancellationToken);
}
