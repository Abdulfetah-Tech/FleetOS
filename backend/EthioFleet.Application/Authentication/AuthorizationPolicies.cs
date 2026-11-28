namespace EthioFleet.Application.Authentication;

/// <summary>
/// Centralizes authorization policy names and role-to-policy mappings across the API.
/// </summary>
public static class AuthorizationPolicies
{
    public const string TenantMember = nameof(TenantMember);
    public const string CompanyOwner = nameof(CompanyOwner);
    public const string FleetManager = nameof(FleetManager);
    public const string Dispatcher = nameof(Dispatcher);
    public const string MaintenanceManager = nameof(MaintenanceManager);
    public const string InspectionViewer = nameof(InspectionViewer);
    public const string InspectionSubmitter = nameof(InspectionSubmitter);
    public const string FinanceManager = nameof(FinanceManager);
    public const string FleetReadOnly = nameof(FleetReadOnly);
    public const string PlatformAdmin = nameof(PlatformAdmin);

    public static readonly IReadOnlyDictionary<string, string[]> TenantRoleMatrix =
        new Dictionary<string, string[]>
        {
            [CompanyOwner] = ["CompanyOwner", "CompanyAdministrator"],
            [FleetManager] = ["CompanyOwner", "CompanyAdministrator", "OperationsManager", "FleetManager"],
            [Dispatcher] = ["CompanyOwner", "CompanyAdministrator", "OperationsManager", "FleetManager", "Dispatcher"],
            [MaintenanceManager] = ["CompanyOwner", "CompanyAdministrator", "OperationsManager", "MaintenanceManager"],
            [InspectionViewer] = ["CompanyOwner", "CompanyAdministrator", "OperationsManager", "FleetManager", "MaintenanceManager", "ReadOnlyAnalyst"],
            [InspectionSubmitter] = ["CompanyOwner", "CompanyAdministrator", "OperationsManager", "FleetManager", "MaintenanceManager", "Driver"],
            [FinanceManager] = ["CompanyOwner", "CompanyAdministrator", "FinanceManager", "Accountant"],
            [FleetReadOnly] = ["CompanyOwner", "CompanyAdministrator", "OperationsManager", "FleetManager", "MaintenanceManager", "FinanceManager", "Accountant", "ReadOnlyAnalyst"]
        };

    public static readonly string[] PlatformAdminRoles = ["SuperAdministrator", "SystemAdmin"];
}
