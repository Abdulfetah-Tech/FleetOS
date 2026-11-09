namespace EthioFleet.Domain.Inspections;

public sealed record InspectionChecklistItemDefinition(
    string Key,
    string Label,
    bool IsCritical);

public sealed record InspectionChecklistDefinition(
    string Version,
    IReadOnlyCollection<InspectionChecklistItemDefinition> Items);

public sealed record InspectionItemResponse(
    string Key,
    InspectionResult Result,
    string? Notes);
