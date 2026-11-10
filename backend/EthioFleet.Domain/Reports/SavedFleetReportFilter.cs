using System.Text.Json;
using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Reports;

public enum FleetReportDatePreset
{
    Custom = 1,
    ThisMonth = 2,
    LastMonth = 3,
    Last30Days = 4,
    Last90Days = 5,
    YearToDate = 6
}

public sealed record FleetReportFilterConfiguration(
    FleetReportDatePreset DatePreset,
    DateOnly? CustomFrom = null,
    DateOnly? CustomTo = null,
    Guid? VehicleId = null,
    string? CurrencyCode = null);

public sealed class SavedFleetReportFilter : BaseEntity, ITenantOwned, ISoftDeletable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private SavedFleetReportFilter()
    {
        Name = string.Empty;
        ConfigurationJson = "{}";
    }

    public SavedFleetReportFilter(Guid organizationId, Guid ownerUserId, string name, FleetReportFilterConfiguration configuration)
    {
        if (organizationId == Guid.Empty || ownerUserId == Guid.Empty)
        {
            throw new DomainException("A tenant and filter owner are required.");
        }
        Name = NormalizeRequired(name, nameof(name), 100);
        ConfigurationJson = Serialize(configuration);
        OrganizationId = organizationId;
        OwnerUserId = ownerUserId;
    }

    public Guid OrganizationId { get; set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; }
    public string ConfigurationJson { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public FleetReportFilterConfiguration Configuration =>
        JsonSerializer.Deserialize<FleetReportFilterConfiguration>(ConfigurationJson, JsonOptions)
        ?? throw new DomainException("Saved report filter configuration is invalid.");

    public void Update(string name, FleetReportFilterConfiguration configuration)
    {
        Name = NormalizeRequired(name, nameof(name), 100);
        ConfigurationJson = Serialize(configuration);
    }

    private static string Serialize(FleetReportFilterConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (!Enum.IsDefined(configuration.DatePreset))
        {
            throw new DomainException("Saved report date preset is invalid.");
        }
        if (configuration.DatePreset == FleetReportDatePreset.Custom &&
            (configuration.CustomFrom is null || configuration.CustomTo is null || configuration.CustomTo < configuration.CustomFrom))
        {
            throw new DomainException("Custom report filters require a valid start and end date.");
        }
        if (configuration.DatePreset != FleetReportDatePreset.Custom &&
            (configuration.CustomFrom is not null || configuration.CustomTo is not null))
        {
            throw new DomainException("Date values can only be supplied for a custom date preset.");
        }
        if (configuration.CurrencyCode is not null &&
            (configuration.CurrencyCode.Trim().Length != 3 || configuration.CurrencyCode.Trim().Any(character => !char.IsAsciiLetter(character))))
        {
            throw new DomainException("Report currency code must contain three letters.");
        }

        return JsonSerializer.Serialize(configuration with
        {
            CurrencyCode = configuration.CurrencyCode?.Trim().ToUpperInvariant()
        }, JsonOptions);
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
}
