namespace EthioFleet.Application.Interfaces;

public interface ITenantContext
{
    Guid? OrganizationId { get; }
    bool IsSystemAdmin { get; }
}
