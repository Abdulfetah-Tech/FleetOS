using System.Text.Json;
using EthioFleet.Domain.Common;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Domain.Inspections;

public sealed class VehicleInspection : BaseEntity, ITenantOwned, ISoftDeletable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private VehicleInspection()
    {
        Vehicle = null!;
        ChecklistVersion = string.Empty;
        ChecklistItemsJson = "[]";
        ItemsJson = "[]";
        Notes = string.Empty;
    }

    public VehicleInspection(
        Guid organizationId,
        Vehicle vehicle,
        DateTimeOffset inspectedAt,
        decimal odometer,
        Guid? inspectorUserId,
        InspectionChecklistDefinition checklist,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(checklist);
        if (organizationId == Guid.Empty || vehicle.OrganizationId != organizationId)
        {
            throw new DomainException("Inspection vehicle must belong to the inspection organization.");
        }
        if (odometer < vehicle.CurrentMileage || odometer < 0 || decimal.Round(odometer, 1) != odometer)
        {
            throw new DomainException("Inspection odometer cannot be less than current vehicle mileage and must be non-negative with at most one decimal place.");
        }

        ValidateChecklist(checklist);
        OrganizationId = organizationId;
        VehicleId = vehicle.Id;
        Vehicle = vehicle;
        InspectedAt = inspectedAt.ToUniversalTime();
        Odometer = odometer;
        InspectorUserId = inspectorUserId;
        ChecklistVersion = checklist.Version.Trim();
        ChecklistItemsJson = JsonSerializer.Serialize(checklist.Items, JsonOptions);
        Notes = NormalizeOptional(notes, 2000);
        ItemsJson = "[]";
        Status = InspectionStatus.InProgress;
    }

    public Guid OrganizationId { get; set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; }
    public Guid? InspectorUserId { get; private set; }
    public DateTimeOffset InspectedAt { get; private set; }
    public decimal Odometer { get; private set; }
    public string ChecklistVersion { get; private set; }
    public string ChecklistItemsJson { get; private set; }
    public string ItemsJson { get; private set; }
    public string Notes { get; private set; }
    public InspectionStatus Status { get; private set; }
    public bool RequiresVehicleOutOfService { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public IReadOnlyCollection<InspectionChecklistItemDefinition> ChecklistItems =>
        JsonSerializer.Deserialize<InspectionChecklistItemDefinition[]>(ChecklistItemsJson, JsonOptions) ?? [];

    public IReadOnlyCollection<InspectionItemResponse> Responses =>
        JsonSerializer.Deserialize<InspectionItemResponse[]>(ItemsJson, JsonOptions) ?? [];

    public void Complete(IReadOnlyCollection<InspectionItemResponse> responses, DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(responses);
        if (Status != InspectionStatus.InProgress)
        {
            throw new DomainException("Only in-progress inspections can be completed.");
        }

        var definitions = ChecklistItems.ToDictionary(item => item.Key, StringComparer.Ordinal);
        if (responses.Count != definitions.Count || responses.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() != definitions.Count)
        {
            throw new DomainException("Every inspection checklist item must have exactly one result.");
        }

        foreach (var response in responses)
        {
            if (!definitions.ContainsKey(response.Key) || !Enum.IsDefined(response.Result))
            {
                throw new DomainException("Inspection result contains an unknown checklist item or result.");
            }
            if (response.Notes?.Length > 1000)
            {
                throw new DomainException("Inspection item notes cannot exceed 1000 characters.");
            }
        }

        var materialized = responses.ToArray();
        var passed = materialized.All(item => item.Result != InspectionResult.Fail);
        var hasCriticalFailure = materialized.Any(item => item.Result == InspectionResult.Fail && definitions[item.Key].IsCritical);
        ItemsJson = JsonSerializer.Serialize(materialized, JsonOptions);
        RequiresVehicleOutOfService = hasCriticalFailure;
        Status = passed ? InspectionStatus.Passed : InspectionStatus.Failed;
        CompletedAt = completedAt.ToUniversalTime();
    }

    private static void ValidateChecklist(InspectionChecklistDefinition checklist)
    {
        if (string.IsNullOrWhiteSpace(checklist.Version) || checklist.Version.Length > 40)
        {
            throw new DomainException("Inspection checklist version is required and cannot exceed 40 characters.");
        }
        if (checklist.Items is null || checklist.Items.Count is < 1 or > 100)
        {
            throw new DomainException("Inspection checklist must contain between 1 and 100 items.");
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in checklist.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > 80 ||
                string.IsNullOrWhiteSpace(item.Label) || item.Label.Length > 200 || !keys.Add(item.Key))
            {
                throw new DomainException("Inspection checklist item keys and labels must be valid and keys unique.");
            }
        }
    }

    private static string NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"Inspection notes cannot exceed {maxLength} characters.");
        }
        return normalized;
    }
}
