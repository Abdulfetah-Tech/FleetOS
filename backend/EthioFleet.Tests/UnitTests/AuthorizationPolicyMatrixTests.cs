using EthioFleet.Application.Authentication;

namespace EthioFleet.Tests.UnitTests;

public sealed class AuthorizationPolicyMatrixTests
{
    [Fact]
    public void TenantRoleMatrixCoversAllOperationalPoliciesAndSeededRoles()
    {
        Assert.Equal(
            [
                AuthorizationPolicies.CompanyOwner,
                AuthorizationPolicies.FleetManager,
                AuthorizationPolicies.Dispatcher,
                AuthorizationPolicies.MaintenanceManager,
                AuthorizationPolicies.InspectionViewer,
                AuthorizationPolicies.InspectionSubmitter,
                AuthorizationPolicies.FinanceManager,
                AuthorizationPolicies.FleetReadOnly
            ],
            AuthorizationPolicies.TenantRoleMatrix.Keys);

        Assert.Contains("OperationsManager", AuthorizationPolicies.TenantRoleMatrix[AuthorizationPolicies.FleetManager]);
        Assert.Contains("OperationsManager", AuthorizationPolicies.TenantRoleMatrix[AuthorizationPolicies.MaintenanceManager]);
        Assert.Contains("Dispatcher", AuthorizationPolicies.TenantRoleMatrix[AuthorizationPolicies.Dispatcher]);
        Assert.Contains("ReadOnlyAnalyst", AuthorizationPolicies.TenantRoleMatrix[AuthorizationPolicies.FleetReadOnly]);
        Assert.Contains("ReadOnlyAnalyst", AuthorizationPolicies.TenantRoleMatrix[AuthorizationPolicies.InspectionViewer]);
        Assert.Contains("Driver", AuthorizationPolicies.TenantRoleMatrix[AuthorizationPolicies.InspectionSubmitter]);
        Assert.DoesNotContain("Employee", AuthorizationPolicies.TenantRoleMatrix[AuthorizationPolicies.FleetReadOnly]);
        Assert.DoesNotContain("Driver", AuthorizationPolicies.TenantRoleMatrix[AuthorizationPolicies.FleetManager]);
        Assert.Equal(["SuperAdministrator", "SystemAdmin"], AuthorizationPolicies.PlatformAdminRoles);
    }
}
