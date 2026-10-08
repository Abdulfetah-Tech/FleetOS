namespace EthioFleet.Domain.Common;

public interface ITenantOwned
{
    Guid OrganizationId { get; set; }
}
