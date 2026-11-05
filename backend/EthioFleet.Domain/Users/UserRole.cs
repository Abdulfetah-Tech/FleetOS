using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Users;

public sealed class UserRole : BaseEntity, ITenantOwned, ISoftDeletable
{
    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId, Guid organizationId)
    {
        UserId = userId;
        RoleId = roleId;
        OrganizationId = organizationId;
    }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid OrganizationId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
